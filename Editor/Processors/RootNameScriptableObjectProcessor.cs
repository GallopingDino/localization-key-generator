using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class RootNameScriptableObjectProcessor : ParameterProcessor {
        public override string ParameterName => "rootName";

        public override bool CanProcess(PropertyContext context) {
            return context.GetValue() is ScriptableObject;
        }

        public override object Process(PropertyContext context) {
            var scriptable = (ScriptableObject) context.GetValue();
            return scriptable.name;
        }
    }
}
