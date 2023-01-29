using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class RootNameScriptableObjectProcessor : ParameterProcessor {
        public override string ParameterName => "rootName";
        
        public override bool CanProcess(InspectorProperty property) {
            return property.ValueEntry?.WeakSmartValue is ScriptableObject;
        }
        
        public override object Process(InspectorProperty property) {
            var scriptable = (ScriptableObject) property.ValueEntry.WeakSmartValue;
            return scriptable.name;
        }
    }
}