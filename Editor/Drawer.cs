using Dino.LocalizationKeyGenerator.Editor.UI;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.Localization;

namespace Dino.LocalizationKeyGenerator.Editor {
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    internal class Drawer : OdinDrawer {
        private ILayout _layout;

        protected override void Initialize() {
            var keyAttr = GetAutoKeyAttribute(Property);
            var commentAttr = GetAutoCommentAttribute(Property);
            var editor = new EditorFacade(Property);
            var styles = new Styles();
            _layout = keyAttr != null ? (ILayout) new FullLayout(Property, keyAttr, commentAttr, editor, styles, DrawDefaultInspector)
                                      : new SimplifiedLayout(Property, commentAttr, editor, styles, DrawDefaultInspector);
        }

        public override bool CanDrawProperty(InspectorProperty property) {
            return property.ValueEntry?.TypeOfValue == typeof(LocalizedString) 
                   && (GetAutoKeyAttribute(property) != null || GetAutoCommentAttribute(property) != null);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            _layout.Draw(label);
        }

        private void DrawDefaultInspector(GUIContent label) {
            CallNextDrawer(label);
        }

        private static AutoKeyAttribute GetAutoKeyAttribute(InspectorProperty property) {
            return property.GetAttribute<AutoKeyAttribute>() ?? property.Parent?.GetAttribute<AutoKeyAttribute>();
        }

        private static AutoCommentAttribute GetAutoCommentAttribute(InspectorProperty property) {
            return property.GetAttribute<AutoCommentAttribute>() ?? property.Parent?.GetAttribute<AutoCommentAttribute>();
        }
    }
}