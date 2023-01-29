using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class RootNameMonoBehaviourProcessor : ParameterProcessor {
        public override string ParameterName => "rootName";
        
        public override bool CanProcess(InspectorProperty property) {
            return property.ValueEntry?.WeakSmartValue is MonoBehaviour;
        }
        
        public override object Process(InspectorProperty property) {
            var behaviour = (MonoBehaviour) property.ValueEntry.WeakSmartValue;
            return behaviour.name;
        }
    }
}