using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dino.LocalizationKeyGenerator.Editor.Processors;
using Dino.LocalizationKeyGenerator.Editor.Settings;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using UnityEditor;
using RH = Dino.LocalizationKeyGenerator.Editor.Utility.RegexHelper;

namespace Dino.LocalizationKeyGenerator.Editor.Solvers {
    internal class SolverImpl {
        private const int MaxAnalysisDepth = 5;
        
        private readonly StringBuilder _errorBuilder = new StringBuilder();
        private readonly List<ParameterProcessor> _parameterProcessors = new List<ParameterProcessor>();
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private readonly Stack<InspectorProperty> _propertyHierarchy = new Stack<InspectorProperty>();
        private readonly TextFormatter _textFormatter = new TextFormatter();
        private static string _parameterParserPattern;
        
        public string DefaultStringFormat { get; set; }
        public IDictionary<string, object> Parameters => _parameters;

        #region Parameters processing

        public void CollectParameters(InspectorProperty property) {
            ClearParameters();
            FillGlobalParameters();
            BuildPropertyHierarchy(property);
            TraversePropertyHierarchy();
        }

        private void ClearParameters() {
            _parameters.Clear();
        }

        private void FillGlobalParameters() {
            foreach (var defaultParameterPair in LocalizationKeyGeneratorSettings.Instance.Parameters) {
                _parameters[defaultParameterPair.Key] = defaultParameterPair.Value;
            }
        }

        private void BuildPropertyHierarchy(InspectorProperty property) {
            _propertyHierarchy.Clear();
            var baseProperty = property;
            while (baseProperty != null) {
                _propertyHierarchy.Push(baseProperty);
                baseProperty = baseProperty.Parent;
            }
        }

        private void TraversePropertyHierarchy() {
            InitializeParameterProcessors();
            while (_propertyHierarchy.Count > 0) {
                var property = _propertyHierarchy.Pop();
                FillAttributeProvidedParameters(property);
                FillProcessorProvidedParameters(property);
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

        private void FillAttributeProvidedParameters(InspectorProperty property) {
            foreach (var parameter in GetParameterAttributes(property)) {
                var value = parameter.Value;
                if (TryResolveLine(property, value, out var resolvedObject) && !(resolvedObject is string)) {
                    _parameters[parameter.Key] = resolvedObject;
                    continue;
                }

                var resolvedStr = resolvedObject as string ?? value;

                if (TryFormatLine(property, resolvedStr, out var formattedStr)) {
                    _parameters[parameter.Key] = formattedStr;
                }
            }
        }

        private void FillProcessorProvidedParameters(InspectorProperty property) {
            foreach (var processor in _parameterProcessors) {
                if (processor.CanProcess(property) == false) {
                    continue;
                }

                var processorResolvedObject = processor.Process(property);
                if (processorResolvedObject == null) {
                    continue;
                }

                var processorResolvedString = processorResolvedObject as string;
                if (processorResolvedString == null) {
                    _parameters[processor.ParameterName] = processorResolvedObject;
                    continue;
                }

                if (TryFormatLine(property, processorResolvedString, out var processorFormattedStr)) {
                    _parameters[processor.ParameterName] = processorFormattedStr;
                }
            }
        }

        private IEnumerable<AutoKeyParamsAttribute> GetParameterAttributes(InspectorProperty property) {
            var propertyAttributes = property.GetAttributes<AutoKeyParamsAttribute>();
            if (property.ValueEntry != null) {
                var propertyClassAttributes = property.ValueEntry.TypeOfValue.GetAttributes<AutoKeyParamsAttribute>();
                propertyAttributes = propertyAttributes.Except(propertyClassAttributes);
            }
            if (property.Parent != null && property.ParentType != null) {
                var parentClassAttributes = property.ParentType.GetAttributes<AutoKeyParamsAttribute>();
                propertyAttributes = propertyAttributes.Concat(parentClassAttributes);
            }
            return propertyAttributes;
        }

        #endregion

        #region Resolve tools

        public bool TryResolveFormat(InspectorProperty property, string format, out string resolved) {
            resolved = TryResolveLine(property, format, out var resolvedFormatObj) ? resolvedFormatObj?.ToString() : format;
            
            if (string.IsNullOrEmpty(resolved)) {
                SetError(GenerateFormatError(format));
                return false;
            }

            return true;
        }

        private bool TryResolveLine(InspectorProperty property, string str, out object resolved) {
            try {
                var resolver = ValueResolver.Get<object>(property, str);
                if (resolver.HasError) {
                    resolved = null;
                    return false;
                }
                resolved = resolver.GetValue();
                return true;
            }
            catch {
                resolved = null;
                return false;
            }
        }

        #endregion

        #region Format tools

        public bool TryFormatLine(InspectorProperty property, string str, out string result) {
            result = null;

            if (string.IsNullOrEmpty(str)) {
                result = str;
                return true;
            }

            var depth = 0;
            var parameterParserPattern = BuildParameterParserPattern();
            while (Regex.Matches(str, parameterParserPattern, 0) is var parsedParameters && parsedParameters.Count > 0) {
                if (depth++ > MaxAnalysisDepth) {
                    SetError(GenerateAnalysisDepthExceeded(str));
                    break;
                }
                foreach (var match in parsedParameters.Cast<Match>().Reverse()) {
                    var parameterName = match.Groups[1].Value;
                    var parameterFormat = match.Groups.Count > 2 ? match.Groups[2].Value : string.Empty;
                    if (_parameters.TryGetValue(parameterName, out var parameterValue) == false
                        && TryResolveLine(property, parameterName, out parameterValue) == false) {
                        SetError(GenerateParameterNotFoundError(parameterName));
                        return false;
                    }
                    
                    try {
                        string formattedParameter;
                        switch (parameterValue) {
                            case var _ when _textFormatter.CanFormat(parameterValue.GetType()):
                                if (string.IsNullOrEmpty(parameterFormat)) parameterFormat = DefaultStringFormat;
                                formattedParameter = _textFormatter.Format(parameterValue, parameterFormat);
                                break;
                            case IFormattable formattableValue:
                                formattedParameter = formattableValue.ToString(parameterFormat, CultureInfo.InvariantCulture);
                                break;
                            case var _ when string.IsNullOrEmpty(parameterFormat):
                                formattedParameter = parameterValue.ToString();
                                break;
                            default:
                                throw new FormatException();
                        }
                        str = str.Substring(0, match.Index) + formattedParameter + str.Substring(match.Index + match.Length);
                    }
                    catch {
                        SetError(GenerateParameterFormatError(parameterName, parameterFormat));
                        return false;
                    }
                }
            }

            result = str;
            return true;
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

        private string GenerateParameterNotFoundError(string parameterName) {
            return $"Please provide parameter <b>{parameterName}</b>";
        }

        private string GenerateParameterFormatError(string parameterName, string format) {
            return $"Invalid parameter <b>{parameterName}</b> format <b>'{format}'</b>";
        }

        private string GenerateFormatError(string format) {
            return $"Invalid format <b>'{format}</b>";
        }

        private string GenerateAnalysisDepthExceeded(string str) {
            return $"Invalid format <b>'{str}'</b>. Analysis depth exceeded";
        }

        #endregion

    }
}