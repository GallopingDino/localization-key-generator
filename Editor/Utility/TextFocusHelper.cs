using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Utility {
    internal class TextFocusHelper {
        private readonly string _controlName;
        
        private bool _isFocused = false;
        private bool _forceFocus = false;
        private int _framesToHoldFocus = -1;
        private int _prevCursorPosition = -1;
        private TextEditor _textEditor;

        public TextFocusHelper(string controlName) {
            _controlName = controlName;
        }

        public void PreserveFocus() {
            _framesToHoldFocus = 3;
        }

        public void Tick() {
            if (Event.current.type == EventType.Layout && _framesToHoldFocus >= 0) {
                _framesToHoldFocus--;
            }
            _forceFocus = _framesToHoldFocus >= 0;
        }

        public void BeginFocusedArea() {
            var nextControlId = GetNextControlId();
            if (Event.current.type == EventType.Layout) {
                _isFocused = GUI.GetNameOfFocusedControl() == _controlName;
                _textEditor = GetTextEditor(nextControlId);
                return;
            }

            if (_forceFocus) {
                if (_isFocused == false) {
                    GUI.FocusControl(_controlName);
                }

                if (_prevCursorPosition >= 0) {
                    _textEditor.cursorIndex = _prevCursorPosition;
                    _textEditor.selectIndex = _prevCursorPosition;
                }
            }
        }

        public void EndFocusedArea() {
            if (Event.current.type == EventType.Layout) {
                return;
            }
            
            // Do not record cursor position on focus change frame as it gets reset by TextEditor
            if (_forceFocus && _isFocused == false) {
                _isFocused = true;
                return;
            }

            _prevCursorPosition = _isFocused ? _textEditor.cursorIndex : -1;
        }

        private int GetNextControlId() {
            return GUIUtility.GetControlID(FocusType.Passive) + 1;
        }

        private TextEditor GetTextEditor(int controlId) {
            return (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), controlId);
        }
    }
}