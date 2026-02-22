using System;
using UnityEditor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class UuidScriptableObjectProcessor : ParameterProcessor {
        public override string ParameterName => "uuid";

        public override bool CanProcess(PropertyContext context) {
            return context.GetValue() is ScriptableObject;
        }

        public override object Process(PropertyContext context) {
            var scriptable = (ScriptableObject) context.GetValue();
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scriptable, out var guid, out long _)) {
                return Guid.Parse(guid);
            }
            return string.Empty;
        }
    }
}
