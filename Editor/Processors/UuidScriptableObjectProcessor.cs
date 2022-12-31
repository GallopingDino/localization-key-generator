using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class UuidScriptableObjectProcessor : ParameterProcessor {
        public override string ParameterName => "uuid";
        
        public override bool CanProcess(InspectorProperty property) {
            return property.ValueEntry?.WeakSmartValue is ScriptableObject;
        }
        
        public override object Process(InspectorProperty property) {
            var scriptable = (ScriptableObject) property.ValueEntry.WeakSmartValue;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scriptable, out var guid, out long _)) {
                return guid;
            }
            return string.Empty;
        }
    }
}