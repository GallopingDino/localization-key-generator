using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Dino.LocalizationKeyGenerator.Editor.Utility {
    internal class Undo {
        private readonly InspectorProperty _property;

        public Undo(InspectorProperty property) {
            _property = property;
        }

        public void RegisterSelfChanges(string desc) {
            if (_property.SerializationRoot.ValueEntry.WeakSmartValue is Object root) {
                UnityEditor.Undo.RecordObject(root, desc);
                TriggerDefaultDrawerUpdate();
            }
        }

        public void RegisterLocalizationTableChanges(LocalizationTable table, string desc) {
            if (table == null) return;

            UnityEditor.Undo.RecordObject(table, desc);
            EditorUtility.SetDirty(table);

            if (table.SharedData != null) {
                EditorUtility.SetDirty(table.SharedData);
            }

            TriggerDefaultDrawerUpdate();
        }

        public void RegisterSharedDataChanges(SharedTableData sharedData, string desc) {
            if (sharedData == null) return;

            UnityEditor.Undo.RecordObject(sharedData, desc);
            EditorUtility.SetDirty(sharedData);

            TriggerDefaultDrawerUpdate();
        }

        public void RegisterCollectionChanges(StringTableCollection collection, string desc) {
            if (collection == null) return;
            
            var objects = new Object[collection.Tables.Count + 1];
            for (var i = 0; i < collection.Tables.Count; ++i) {
                objects[i] = collection.Tables[i].asset;
            }
            objects[collection.Tables.Count] = collection.SharedData;
            UnityEditor.Undo.RecordObjects(objects, desc);

            foreach (var o in objects) {
                EditorUtility.SetDirty(o);
            }

            TriggerDefaultDrawerUpdate();
        }

        private void TriggerDefaultDrawerUpdate() {
            if (_property.SerializationRoot.ValueEntry.WeakSmartValue is Object root) {
                EditorUtility.SetDirty(root); // This is the only way to trigger LocalizedStringPropertyDrawer update
            }
        }
    }
}