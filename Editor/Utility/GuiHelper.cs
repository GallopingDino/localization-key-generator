using System.Collections.Generic;
using UnityEditor;

namespace Dino.LocalizationKeyGenerator.Editor.Utility {
    internal static class GuiHelper {
        private static readonly Stack<int> _indentStack = new ();

        public static bool HasFocusedWindow() {
            return EditorWindow.focusedWindow != null;
        }

        public static void BeginBox() {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
        }

        public static void EndBox() {
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        public static void PushIndentLevel(int level) {
            _indentStack.Push(EditorGUI.indentLevel);
            EditorGUI.indentLevel = level;
        }

        public static void PopIndentLevel() {
            EditorGUI.indentLevel = _indentStack.Pop();
        }
    }
}
