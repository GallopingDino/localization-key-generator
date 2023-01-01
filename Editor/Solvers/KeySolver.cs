using System.Text.RegularExpressions;
using Sirenix.OdinInspector.Editor;
using UnityEngine.Localization.Tables;

namespace Dino.LocalizationKeyGenerator.Editor.Solvers {
    internal class KeySolver {
        private const string IndexParameterName = "index";
        private const string DefaultIndexParameterPostfix = "-{index:D3}";
        
        private readonly SolverImpl _solver = new SolverImpl();
        private readonly Regex _indexParameterFilter = new Regex(@"\{\s*index[\s\:\}]");

        public bool TryCreateKey(InspectorProperty property, string format, SharedTableData sharedData, string oldKey, out string key) {
            key = null;
            _solver.ClearErrors();

            if (_solver.TryResolveFormat(property, format, out var resolvedFormat) == false) {
                return false;
            }

            _solver.CollectParameters(property);
            return TryBruteForceKeyIndex(property, resolvedFormat, sharedData, oldKey, out key);
        }

        public void CheckForErrors(InspectorProperty property, string format) => TryCreateKey(property, format, sharedData: null, oldKey: string.Empty, key: out _);

        public string GetErrors() => _solver.GetErrors();

        private bool TryBruteForceKeyIndex(InspectorProperty property, string format, SharedTableData sharedData, string oldKey, out string key) {
            var index = 0;
            do {
                _solver.Parameters[IndexParameterName] = index;
                if (index == 1) {
                    format = AppendFormatWithIndexIfNone(format);
                }
                if (_solver.TryFormatLine(property, format, out key) == false) {
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