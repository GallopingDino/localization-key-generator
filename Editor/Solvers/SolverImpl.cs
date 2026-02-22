using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dino.LocalizationKeyGenerator.Editor.Processors;
using Dino.LocalizationKeyGenerator.Editor.Settings;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using UnityEditor;
using RH = Dino.LocalizationKeyGenerator.Editor.Utility.RegexHelper;

namespace Dino.LocalizationKeyGenerator.Editor.Solvers {
    internal class SolverImpl {
        private const int MaxAnalysisDepth = 5;

        private readonly StringBuilder _errorBuilder = new StringBuilder();
        private readonly List<ParameterProcessor> _parameterProcessors = new List<ParameterProcessor>();
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private readonly Stack<PropertyContext> _propertyHierarchy = new Stack<PropertyContext>();
        private readonly TextFormatter _textFormatter = new TextFormatter();
        private static string _parameterParserPattern;

        public string DefaultStringFormat { get; set; }

        #region Line resolution

        public bool TryResolveFormat(PropertyContext context, string input, out string resolved) {
            resolved = ValueResolverStrategy.TryResolve(context, input, out var resolvedFormatObj) ? resolvedFormatObj?.ToString() : input;

            if (string.IsNullOrEmpty(resolved)) {
                SetError(CreateFormatError(input));
                return false;
            }

            return true;
        }

        public bool TryResolveLine(PropertyContext context, string format, out string result) {
            result = null;

            if (string.IsNullOrEmpty(format)) {
                result = format;
                return true;
            }

            var depth = 0;
            var parameterParserPattern = BuildParameterParserPattern();
            while (Regex.Matches(format, parameterParserPattern, 0) is var parsedParameters && parsedParameters.Count > 0) {
                if (depth++ > MaxAnalysisDepth) {
                    SetError(CreateAnalysisDepthExceededError(format));
                    break;
                }
                foreach (var match in parsedParameters.Cast<Match>().Reverse()) {
                    var parameterName = match.Groups[1].Value;
                    var parameterFormat = match.Groups.Count > 2 ? match.Groups[2].Value : string.Empty;
                    if (TryResolveParameter(context, parameterName, parameterFormat, out var formattedParameter) == false) {
                        return false;
                    }

                    format = format.Substring(0, match.Index) + formattedParameter + format.Substring(match.Index + match.Length);
                }
            }

            result = format;
            return true;
        }

        private bool TryResolveParameter(PropertyContext context, string parameterName, string parameterFormat, out string formattedParameter) {
            if (_parameters.TryGetValue(parameterName, out var parameterValue) == false && ValueResolverStrategy.TryResolve(context, parameterName, out parameterValue) == false) {
                SetError(CreateParameterNotFoundError(parameterName));
                formattedParameter = null;
                return false;
            }

            try {
                formattedParameter = ApplyParameterFormatting(parameterValue, ref parameterFormat);
                return true;
            }
            catch {
                SetError(CreateParameterFormatError(parameterName, parameterFormat));
                formattedParameter = null;
                return false;
            }
        }

        private static string BuildParameterParserPattern() {
            if (string.IsNullOrEmpty(_parameterParserPattern) == false) {
                return _parameterParserPattern;
            }
            var parameterNamePattern = $@"@?{RH.AnyOf(RH.Letter, RH.Digit, RH.Space, @"_\+\-\*\%\(\)\.")}+";
            var formatPattern = $@"{RH.Not(Regex.Escape("}"))}+";
            _parameterParserPattern = $@"\{{{RH.Space}*({parameterNamePattern}){RH.Space}*\:?{RH.Space}*({formatPattern})?{RH.Space}*\}}";
            return _parameterParserPattern;
        }

        private string ApplyParameterFormatting(object parameterValue, ref string parameterFormat) {
            switch (parameterValue) {
                case var _ when _textFormatter.CanFormat(parameterValue.GetType()):
                    if (string.IsNullOrEmpty(parameterFormat)) parameterFormat = DefaultStringFormat;
                    return _textFormatter.Format(parameterValue, parameterFormat);
                case IFormattable formattableValue:
                    return formattableValue.ToString(parameterFormat, CultureInfo.InvariantCulture);
                case var _ when string.IsNullOrEmpty(parameterFormat):
                    return parameterValue.ToString();
                default:
                    throw new FormatException();
            }
        }

        #endregion

        #region Parameters processing

        public void CollectParameters(PropertyContext context) {
            ClearParameters();
            FillGlobalParameters();
            BuildPropertyHierarchy(context);
            TraversePropertyHierarchy();
        }

        public void OverrideParameter(string name, object value) {
            _parameters[name] = value;
        }

        private void ClearParameters() {
            _parameters.Clear();
        }

        private void FillGlobalParameters() {
            foreach (var defaultParameterPair in LocalizationKeyGeneratorSettings.Instance.Parameters) {
                _parameters[defaultParameterPair.Key] = defaultParameterPair.Value;
            }
        }

        private void BuildPropertyHierarchy(PropertyContext context) {
            _propertyHierarchy.Clear();
            var current = context;
            while (current != null) {
                _propertyHierarchy.Push(current);
                current = current.Parent;
            }
        }

        private void TraversePropertyHierarchy() {
            InitializeParameterProcessors();
            while (_propertyHierarchy.Count > 0) {
                var context = _propertyHierarchy.Pop();
                FillAttributeProvidedParameters(context);
                FillProcessorProvidedParameters(context);
            }
        }

        private void InitializeParameterProcessors() {
            if (_parameterProcessors.Count > 0) {
                return;
            }
            _parameterProcessors.AddRange(TypeCache.GetTypesDerivedFrom<ParameterProcessor>()
                .Where(t => t.IsAbstract == false)
                .Select(Activator.CreateInstance)
                .Cast<ParameterProcessor>());
        }

        private void FillAttributeProvidedParameters(PropertyContext context) {
            foreach (var parameter in GetParameterAttributes(context)) {
                var value = parameter.Value;
                if (ValueResolverStrategy.TryResolve(context, value, out var resolvedObject) && !(resolvedObject is string)) {
                    _parameters[parameter.Key] = resolvedObject;
                    continue;
                }

                var resolvedStr = resolvedObject as string ?? value;

                if (TryResolveLine(context, resolvedStr, out var formattedStr)) {
                    _parameters[parameter.Key] = formattedStr;
                }
            }
        }

        private void FillProcessorProvidedParameters(PropertyContext context) {
            foreach (var processor in _parameterProcessors) {
                if (processor.CanProcess(context) == false) {
                    continue;
                }

                var processorResolvedObject = processor.Process(context);
                if (processorResolvedObject == null) {
                    continue;
                }

                var processorResolvedString = processorResolvedObject as string;
                if (processorResolvedString == null) {
                    _parameters[processor.ParameterName] = processorResolvedObject;
                    continue;
                }

                if (TryResolveLine(context, processorResolvedString, out var processorFormattedStr)) {
                    _parameters[processor.ParameterName] = processorFormattedStr;
                }
            }
        }

        private IEnumerable<AutoKeyParamsAttribute> GetParameterAttributes(PropertyContext context) {
            var propertyAttributes = context.GetAttributes<AutoKeyParamsAttribute>();
            if (context.ValueType != null) {
                var propertyClassAttributes = context.ValueType.GetCustomAttributes<AutoKeyParamsAttribute>(true);
                propertyAttributes = propertyAttributes.Except(propertyClassAttributes);
            }
            if (context.Parent != null && context.ParentType != null) {
                var parentClassAttributes = context.ParentType.GetCustomAttributes<AutoKeyParamsAttribute>(true);
                propertyAttributes = propertyAttributes.Concat(parentClassAttributes);
            }
            return propertyAttributes;
        }

        #endregion

        #region Errors

        public string GetErrors() {
            return _errorBuilder.ToString();
        }

        public void ClearErrors() {
            _errorBuilder.Clear();
        }

        private void SetError(string message) {
            _errorBuilder.AppendLine(message);
        }

        private string CreateParameterNotFoundError(string parameterName) {
            return $"Please provide parameter <b>{parameterName}</b>";
        }

        private string CreateParameterFormatError(string parameterName, string format) {
            return $"Invalid parameter <b>{parameterName}</b> format <b>'{format}'</b>";
        }

        private string CreateFormatError(string format) {
            return $"Invalid format <b>'{format}'</b>";
        }

        private string CreateAnalysisDepthExceededError(string str) {
            return $"Invalid format <b>'{str}'</b>. Analysis depth exceeded";
        }

        #endregion

    }
}
