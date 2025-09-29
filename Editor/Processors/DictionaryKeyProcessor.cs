using System.Collections;
using Sirenix.OdinInspector.Editor;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    /// <summary>
    /// Returns the key corresponding to the current dictionary element.
    /// Works with all collections implementing non-generic IDictionary interface.
    /// This processor iterates over every element in every parent dictionary to find the corresponding key.
    /// Therefore, using it with large dictionaries might have performance implications in the editor.
    /// </summary>
    internal sealed class DictionaryKeyProcessor : ParameterProcessor {
        public override string ParameterName => "dictionaryKey";
        
        public override bool CanProcess(InspectorProperty property) {
            var parentDictionary = GetFirstParentDictionary(property);
            if (parentDictionary == null) {
                return false;
            }
            return GetKeyByValue(parentDictionary, property.ValueEntry.WeakSmartValue) != null;
        }

        public override object Process(InspectorProperty property) {
            var parentDictionary = GetFirstParentDictionary(property);
            return GetKeyByValue(parentDictionary, property.ValueEntry.WeakSmartValue);
        }
        
        private IDictionary GetFirstParentDictionary(InspectorProperty property) {
            while (property != null) {
                if (property.ValueEntry.WeakSmartValue is IDictionary dictionary) {
                    return dictionary;
                }
                property = property.Parent;
            }

            return null;
        }
        
        private object GetKeyByValue(IDictionary dictionary, object value) {
            foreach (var key in dictionary.Keys) {
                if (dictionary[key].Equals(value)) {
                    return key;
                }
            }

            return null;
        }
    }
}