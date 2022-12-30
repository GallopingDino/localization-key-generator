using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.Utility {
    internal class TextFieldFocusController {
        private bool _forceCharactersSelectionSuppression = false;
        private int _framesToHoldTextFocus = -1;
        private string _focusedControlName;
        private Color _prevCursorColor;

        public void PreserveTextFocus(string controlName) {
            _framesToHoldTextFocus = 3;
            _focusedControlName = controlName;
        }

        public void TickTextFocusController() {
            if (Event.current.type == EventType.Layout && _framesToHoldTextFocus >= 0) {
                _framesToHoldTextFocus--;
            }
            _forceCharactersSelectionSuppression = _framesToHoldTextFocus >= 0;
        }

        public void BeginCharacterSelectionSuppression() {
            if (_forceCharactersSelectionSuppression == false) {
                return;
            }

            // A hack used to prevent selection of all text on click or manual focus change
            _prevCursorColor = GUI.skin.settings.cursorColor;
            GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);
            if (GUI.GetNameOfFocusedControl() != _focusedControlName) {
                GUI.FocusControl(_focusedControlName);
            }
        }

        public void EndCharacterSelectionSuppression() {
            if (_forceCharactersSelectionSuppression == false) {
                return;
            }
            
            GUI.skin.settings.cursorColor = _prevCursorColor;
        }
    }
}