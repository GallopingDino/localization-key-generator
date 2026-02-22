using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class RootNameMonoBehaviourProcessor : ParameterProcessor {
        public override string ParameterName => "rootName";

        public override bool CanProcess(PropertyContext context) {
            return context.GetValue() is MonoBehaviour;
        }

        public override object Process(PropertyContext context) {
            var behaviour = (MonoBehaviour) context.GetValue();
            return behaviour.name;
        }
    }
}
