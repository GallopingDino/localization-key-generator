using System.Text.RegularExpressions;
using Dino.LocalizationKeyGenerator.Editor.Settings;
using UnityEngine.Localization.Tables;

namespace Dino.LocalizationKeyGenerator.Editor.Solvers {
    internal class KeySolver {
        private const string IndexParameterName = "index";
        private const string DefaultIndexParameterPostfix = "-{index:D3}";

        private readonly SolverImpl _solver = new SolverImpl();
        private readonly Regex _indexParameterFilter = new Regex(@"\{\s*index[\s\:\}]");

        public KeySolver() {
            UpdateSolverSettings();
            LocalizationKeyGeneratorSettings.Instance.Changed += UpdateSolverSettings;
        }

        private void UpdateSolverSettings() {
            _solver.DefaultStringFormat = LocalizationKeyGeneratorSettings.Instance.DefaultKeyStringFormat;
        }

        public bool TryCreateKey(PropertyContext context, string format, out string key) {
            return TryCreateKeyCore(context, format, sharedData: null, oldKey: null, out key);
        }

        public bool TryCreateUniqueKey(PropertyContext context, string format, SharedTableData sharedData, string oldKey, out string key) {
            return TryCreateKeyCore(context, format, sharedData, oldKey, out key);
        }

        public void CheckForErrors(PropertyContext context, string format) {
            TryCreateKeyCore(context, format, sharedData: null, oldKey: string.Empty, key: out _);
        }

        public string GetErrors() => _solver.GetErrors();

        private bool TryCreateKeyCore(PropertyContext context, string format, SharedTableData sharedData, string oldKey, out string key) {
            key = null;
            _solver.ClearErrors();

            if (_solver.TryResolveFormat(context, format, out var resolvedFormat) == false) {
                return false;
            }

            _solver.CollectParameters(context);
            return TryBruteForceKeyIndex(context, resolvedFormat, sharedData, oldKey, out key);
        }

        private bool TryBruteForceKeyIndex(PropertyContext context, string format, SharedTableData sharedData, string oldKey, out string key) {
            var index = 0;
            do {
                _solver.OverrideParameter(IndexParameterName, index);
                if (index == 1) {
                    format = AppendFormatWithIndexIfNone(format);
                }
                if (_solver.TryResolveLine(context, format, out key) == false) {
                    return false;
                }
                index++;
            } while (key != oldKey && sharedData != null && sharedData.Contains(key));

            return true;
        }

        private string AppendFormatWithIndexIfNone(string format) {
            if (_indexParameterFilter.IsMatch(format)) {
                return format;
            }
            return format + DefaultIndexParameterPostfix;
        }
    }
}
