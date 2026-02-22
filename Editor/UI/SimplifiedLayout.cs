using System;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.UI {
    internal class SimplifiedLayout : ILayout {
        private readonly Action<GUIContent> _defaultDrawer;
        private readonly Styles _styles;
        private readonly AutoCommentUi _autoCommentUi;

        public SimplifiedLayout(PropertyContext context, AutoCommentAttribute comment,
                                             PropertyEditor editor, Styles styles, Action<GUIContent> defaultDrawer) {
            _defaultDrawer = defaultDrawer;
            _styles = styles;

            if (comment != null) {
                _autoCommentUi = new AutoCommentUi(context, comment, editor, styles);
                editor.EntryAdded += _autoCommentUi.GenerateComment;
            }
        }

        public void Draw(GUIContent label) {
            Update();

            BeginBox();
            _defaultDrawer.Invoke(label);
            _autoCommentUi?.DrawErrors();
            _autoCommentUi?.DrawComment();
            EndBox();
        }

        private void Update() {
            _styles.Update();
            _autoCommentUi?.Update();
        }

        private void BeginBox() {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        }

        private void EndBox() {
            EditorGUILayout.EndVertical();
        }
    }
}
