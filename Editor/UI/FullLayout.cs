using System;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.UI {
    internal class FullLayout : ILayout {
        private readonly Action<GUIContent> _defaultDrawer;
        private readonly Styles _styles;
        private readonly AutoKeyUi _autoKeyUi;
        private readonly AutoCommentUi _autoCommentUi;

        private bool _expanded = true;

        public FullLayout(PropertyContext context, AutoKeyAttribute key, AutoCommentAttribute comment,
                                       PropertyEditor editor, Styles styles, Action<GUIContent> defaultDrawer) {
            _defaultDrawer = defaultDrawer;
            _styles = styles;

            if (key != null) {
                _autoKeyUi = new AutoKeyUi(context, key, editor, styles);
            }

            if (comment != null) {
                _autoCommentUi = new AutoCommentUi(context, comment, editor, styles);
                editor.EntryAdded += _autoCommentUi.GenerateComment;
            }
        }

        public void Draw(GUIContent label) {
            var showLabel = label != null;

            if (showLabel) {
                _expanded = EditorGUILayout.Foldout(_expanded, label, true);
            }

            if (!_expanded && showLabel) {
                return;
            }

            Update();

            _autoKeyUi.DrawModeSelector(out var mode);
            GuiHelper.BeginBox();

            switch (mode) {
                case AutoKeyUiMode.Auto:
                    if (GUI.enabled) {
                        _autoKeyUi.DrawErrors();
                        _autoCommentUi?.DrawErrors();
                    }
                    _autoKeyUi.DrawKeySelector();
                    _autoCommentUi?.DrawComment();
                    _autoKeyUi.DrawText();
                    break;
                case AutoKeyUiMode.Manual:
                    _defaultDrawer?.Invoke(GUIContent.none);
                    break;
            }
            GuiHelper.EndBox();
        }

        private void Update() {
            _styles.Update();
            _autoKeyUi?.Update();
            _autoCommentUi?.Update();
        }

    }
}
