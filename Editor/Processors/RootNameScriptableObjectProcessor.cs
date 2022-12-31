using System.Text.RegularExpressions;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class RootNameScriptableObjectProcessor : ParameterProcessor {
        public override string ParameterName => "rootName";
        
        public override bool CanProcess(InspectorProperty property) {
            return property.ValueEntry?.WeakSmartValue is ScriptableObject;
        }
        
        //TODO: extract formatter logic into a separate class, make it optional
        public override object Process(InspectorProperty property) {
            var scriptable = (ScriptableObject) property.ValueEntry.WeakSmartValue;
            return Regex.Replace(scriptable.name, @"([a-z])([A-Z,\d])", "$1_$2")
                .ToLowerInvariant()
                .Trim()
                .Replace(' ', '_');
        }
    }
}