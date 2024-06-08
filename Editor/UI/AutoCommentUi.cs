using Dino.LocalizationKeyGenerator.Editor.Settings;
using Dino.LocalizationKeyGenerator.Editor.Solvers;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace Dino.LocalizationKeyGenerator.Editor.UI {
    internal class AutoCommentUi {
        private readonly CommentSolver _commentSolver;
        private readonly InspectorProperty _property;
        private readonly AutoCommentAttribute _attribute;
        private readonly PropertyEditor _editor;
        private readonly Styles _styles;

        private long _settingsVersionOnPrevCommentSolverRun = -1;

        public AutoCommentUi(InspectorProperty property, AutoCommentAttribute attr, PropertyEditor editor, Styles styles) {
            _commentSolver = new CommentSolver();
            _property = property;
            _attribute = attr;
            _editor = editor;
            _styles = styles;
        }

        public void Update() {
            CheckForErrors();
        }

        public void DrawErrors() {
            var errors = _commentSolver.GetErrors();
            if (string.IsNullOrEmpty(errors) == false) {
                EditorGUILayout.LabelField(new GUIContent(errors, _styles.WarningIcon), _styles.ErrorStyle);
            }
        }

        public void DrawComment() {
            if (string.IsNullOrEmpty(_attribute.Format)) {
                return;
            }
            
            var sharedEntry = _editor.GetSharedEntry();

            if (sharedEntry == null) {
                SkipButtonControl();
                SkipButtonControl();
                return;
            }
            
            var existingComment = _editor.GetComment();
            var hasComment = existingComment != null;
            var commentText = existingComment?.CommentText ?? "none";
            
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label(new GUIContent($"Comment: {commentText}", tooltip: commentText), _styles.LabelStyle, _styles.LabelOptions);

            if (GUILayout.Button(hasComment ? "Regenerate" : "Generate", _styles.ButtonStyle, _styles.FlexibleContentOptions)) {
                GenerateComment();
            }

            if (hasComment) {
                if (GUILayout.Button(new GUIContent("✕", "Remove comment metadata"), _styles.SquareContentOptions)) {
                    _editor.RemoveComment();
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    GUIUtility.ExitGUI();
                }
            }
            else {
                SkipButtonControl();
            }

            EditorGUILayout.EndHorizontal();
        }

        public void GenerateComment() {
            if (TryCreateComment(_attribute.Format, out var comment) == false) {
                return;
            }

            _editor.SetComment(comment);
        }

        private bool TryCreateComment(string commentFormat, out string comment) {
            _settingsVersionOnPrevCommentSolverRun = LocalizationKeyGeneratorSettings.Instance.Version;
            return _commentSolver.TryCreateComment(_property, commentFormat, out comment);
        }

        private void CheckForErrors() {
            if (_settingsVersionOnPrevCommentSolverRun == LocalizationKeyGeneratorSettings.Instance.Version)
                return;

            _settingsVersionOnPrevCommentSolverRun = LocalizationKeyGeneratorSettings.Instance.Version;
            _commentSolver.CheckForErrors(_property, _attribute.Format);
        }

        private void SkipButtonControl() {
            GUI.Button(new Rect(), GUIContent.none);
        }
    }
}