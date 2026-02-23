using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Utility {
    internal class UnityDrawerProxy {
        private static readonly FieldInfo _drawerFieldInfoField = typeof(PropertyDrawer).GetField("m_FieldInfo", BindingFlags.NonPublic | BindingFlags.Instance);

        private static Type _drawerType;
        private static bool _drawerTypeResolved;

        private readonly PropertyDrawer _drawer;
        private readonly SerializedProperty _property;

        public UnityDrawerProxy(SerializedProperty property, FieldInfo fieldInfo) {
            _property = property;

            if (!_drawerTypeResolved) {
                _drawerTypeResolved = true;
                var assembly = Assembly.Load("Unity.Localization.Editor");
                _drawerType = assembly?.GetType("UnityEditor.Localization.UI.LocalizedStringPropertyDrawer");
            }

            if (_drawerType == null) {
                return;
            }

            _drawer = (PropertyDrawer) Activator.CreateInstance(_drawerType);
            _drawerFieldInfoField?.SetValue(_drawer, fieldInfo);
        }

        public void Draw(GUIContent label) {
            if (_drawer != null) {
                var height = _drawer.GetPropertyHeight(_property, label);
                var rect = EditorGUILayout.GetControlRect(false, height);
                _drawer.OnGUI(rect, _property, label);
            }
            else {
                EditorGUILayout.PropertyField(_property, label, true);
            }
        }
    }
}
