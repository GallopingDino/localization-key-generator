using System.Collections.Generic;
using Dino.LocalizationKeyGenerator.Editor.UI;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

namespace Dino.LocalizationKeyGenerator.Editor {
    [CustomPropertyDrawer(typeof(AutoKeyAttribute))]
    [CustomPropertyDrawer(typeof(AutoCommentAttribute))]
    internal class Drawer : PropertyDrawer {
        private class DrawerState {
            public ILayout Layout;
            public Styles Styles;
        }

        private readonly Dictionary<string, DrawerState> _states = new Dictionary<string, DrawerState>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return 0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (fieldInfo != null && fieldInfo.FieldType != typeof(LocalizedString)) {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var state = GetOrCreateState(property);
            state.Styles.TrackWidth(position);
            state.Layout.Draw(label);
        }

        private DrawerState GetOrCreateState(SerializedProperty property) {
            var key = property.serializedObject.targetObject.GetInstanceID() + ":" + property.propertyPath;

            if (_states.TryGetValue(key, out var state)) {
                return state;
            }

            state = new DrawerState();
            var context = new PropertyContext(property);
            var keyAttr = GetAutoKeyAttribute(context);
            var commentAttr = GetAutoCommentAttribute(context);
            var editor = new PropertyEditor(context);
            var defaultDrawer = new UnityDrawerProxy(property, fieldInfo);
            var styles = new Styles();

            state.Styles = styles;
            state.Layout = keyAttr != null
                ? (ILayout) new FullLayout(context, keyAttr, commentAttr, editor, styles, defaultDrawer)
                : new SimplifiedLayout(context, commentAttr, editor, styles, defaultDrawer);

            _states[key] = state;
            return state;
        }

        private AutoKeyAttribute GetAutoKeyAttribute(PropertyContext context) {
            return context.GetAttribute<AutoKeyAttribute>() ?? attribute as AutoKeyAttribute;
        }

        private AutoCommentAttribute GetAutoCommentAttribute(PropertyContext context) {
            return context.GetAttribute<AutoCommentAttribute>() ?? attribute as AutoCommentAttribute;
        }
    }
}
