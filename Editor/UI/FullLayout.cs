using System;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.UI {
    internal class FullLayout : ILayout {
        private readonly InspectorProperty _property;
        private readonly Action<GUIContent> _defaultDrawer;
        private readonly Styles _styles;
        private readonly AutoKeyUi _autoKeyUi;
        private readonly AutoCommentUi _autoCommentUi;
        
        public FullLayout(InspectorProperty property, AutoKeyAttribute key, AutoCommentAttribute comment, 
                                       EditorFacade editor, Styles styles, Action<GUIContent> defaultDrawer) {
            _property = property;
            _defaultDrawer = defaultDrawer;
            _styles = styles;

            if (key != null) {
                _autoKeyUi = new AutoKeyUi(property, key, editor, styles);
            }

            if (comment != null) {
                _autoCommentUi = new AutoCommentUi(property, comment, editor, styles);
                editor.EntryAdded += _autoCommentUi.GenerateComment;
            }
        }
        
        public void Draw(GUIContent label) {
            if (BeginFoldout(label) == false) {
                EndFoldout();
                return;
            }
            
            Update();
            
            _autoKeyUi.DrawModeSelector(out var mode);
            BeginBox();

            switch (mode) {
                case AutoKeyUiMode.Auto:
                    _autoKeyUi.DrawErrors();
                    _autoCommentUi?.DrawErrors();
                    _autoKeyUi.DrawKeySelector();
                    _autoCommentUi?.DrawComment();
                    _autoKeyUi.DrawText();
                    break;
                case AutoKeyUiMode.Manual:
                    _defaultDrawer?.Invoke(GUIContent.none);
                    break;
            }
            EndBox();
            EndFoldout();
        }

        private void Update() {
            _styles.Update();
            _autoKeyUi?.Update();
            _autoCommentUi?.Update();
        }

        private bool BeginFoldout(GUIContent label) {
            var showLabel = label != null;

            if (showLabel) {
                _property.State.Expanded = SirenixEditorGUI.Foldout(_property.State.Expanded, label);
            }

            var isVisible = _property.State.Expanded || !showLabel;
            return SirenixEditorGUI.BeginFadeGroup(this, isVisible);
        }

        private void EndFoldout() {
            SirenixEditorGUI.EndFadeGroup();
        }

        private void BeginBox() {
            SirenixEditorGUI.BeginBox();
        }

        private void EndBox() {
            SirenixEditorGUI.EndBox();
        }
    }
}