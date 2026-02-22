using System.Collections;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class DictionaryKeyProcessor : ParameterProcessor {
        public override string ParameterName => "dictionaryKey";

        public override bool CanProcess(PropertyContext context) {
            var parentDictionary = GetFirstParentDictionary(context);
            if (parentDictionary == null) return false;
            return GetKeyByValue(parentDictionary, context.GetValue()) != null;
        }

        public override object Process(PropertyContext context) {
            var parentDictionary = GetFirstParentDictionary(context);
            return GetKeyByValue(parentDictionary, context.GetValue());
        }

        private IDictionary GetFirstParentDictionary(PropertyContext context) {
            var current = context.Parent;
            while (current != null) {
                var value = current.GetValue();
                if (value is IDictionary dictionary) return dictionary;
                current = current.Parent;
            }
            return null;
        }

        private object GetKeyByValue(IDictionary dictionary, object value) {
            foreach (var key in dictionary.Keys) {
                if (dictionary[key].Equals(value)) return key;
            }
            return null;
        }
    }
}
