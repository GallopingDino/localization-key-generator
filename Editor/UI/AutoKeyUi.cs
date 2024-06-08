using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dino.LocalizationKeyGenerator.Editor.Settings;
using Dino.LocalizationKeyGenerator.Editor.Solvers;
using Dino.LocalizationKeyGenerator.Editor.Utility;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Dino.LocalizationKeyGenerator.Editor.UI {
    internal class AutoKeyUi {
        private const string TextControlNamePrefix = "LocalizationText";

        private static string[] _tabLabels;

        private readonly KeySolver _keySolver;
        private readonly InspectorProperty _property;
        private readonly AutoKeyAttribute _attribute;
        private readonly Styles _styles;
        private readonly PropertyEditor _editor;

        private ReadOnlyCollection<StringTableCollection> _tableCollections;
        private string[] _collectionLabels;
        private long _settingsVersionOnPrevKeySolverRun = -1;
        private AutoKeyUiMode _mode;

        #region Initialization

        public AutoKeyUi(InspectorProperty property, AutoKeyAttribute attr, PropertyEditor editor, Styles styles) {
            _keySolver = new KeySolver();
            _property = property;
            _attribute = attr;
            _editor = editor;
            _styles = styles;
            InitializeModeTabs();
            BindTableUpdates();
        }

        private void InitializeModeTabs() {
            _tabLabels = _tabLabels ?? Enum.GetNames(typeof(AutoKeyUiMode));
            _mode = _attribute.IsDefaultTabAuto ? AutoKeyUiMode.Auto : AutoKeyUiMode.Manual;
        }

        private void BindTableUpdates() {
            if (_tableCollections != null) {
                return;
            }
            UpdateTablesList();
            LocalizationEditorSettings.EditorEvents.CollectionAdded += _ => UpdateTablesList();
            LocalizationEditorSettings.EditorEvents.CollectionRemoved += _ => UpdateTablesList();
        }

        private void UpdateTablesList() {
            _tableCollections = LocalizationEditorSettings.GetStringTableCollections();
            _collectionLabels = _tableCollections.Select(c => c.TableCollectionName).Prepend("None").ToArray();
        }

        #endregion
        
        #region Draw

        public void DrawModeSelector(out AutoKeyUiMode mode) {
            BeginIndentedGroup();

            var selectedTab = (int) _mode;
            selectedTab = GUILayout.Toolbar(selectedTab, _tabLabels, EditorStyles.miniButton);
            _mode = (AutoKeyUiMode) selectedTab;
            mode = _mode;

            EndIndentedGroup();
        }

        public void DrawErrors() {
            var errors = _keySolver.GetErrors();
            if (string.IsNullOrEmpty(errors) == false) {
                EditorGUILayout.LabelField(new GUIContent(errors, _styles.WarningIcon), _styles.ErrorStyle);
            }
        }

        public void DrawKeySelector() {
            DrawTablePopup();
            DrawKey();
        }

        public void DrawText() {
            var sharedEntry = _editor.GetSharedEntry();
            
            foreach (var locale in LocalizationKeyGeneratorSettings.Instance.PreviewLocales) {
                if (locale == default || _editor.IsLocalizationTableAvailable(locale) == false) {
                    continue;
                }
                
                DrawLocale(locale, ref sharedEntry);
            }
        }

        private void DrawTablePopup() {
            var prevCollectionIndex = 0;
            var collection = _editor.GetTableCollection();
            if (collection != null) {
                for (var i = 0; i < _tableCollections.Count; i++) {
                    if (_tableCollections[i] != collection) continue;
                    prevCollectionIndex = i + 1;
                    break;
                }
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Table", _styles.LabelStyle, _styles.LabelOptions);
            
            BeginIgnoreIndent();
            var newCollectionIndex = EditorGUILayout.Popup(prevCollectionIndex, _collectionLabels, _styles.FlexibleContentOptions);
            EndIgnoreIndent();
            
            var newCollection = newCollectionIndex <= 0 ? default : _tableCollections[newCollectionIndex - 1];
            if (newCollectionIndex != prevCollectionIndex) {
                _editor.SetTableCollection(newCollection);
                GUIUtility.ExitGUI();
            }

            if (newCollection != null && GUILayout.Button(_styles.EditTableButton, _styles.ButtonStyle, _styles.SquareContentOptions)) {
                LocalizationTablesWindow.ShowWindow(newCollection);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawKey() {
            var sharedData = _editor.GetSharedData();

            if (sharedData == null) {
                return;
            }
            var sharedEntry = _editor.GetSharedEntry();
            var hasEntry = sharedEntry != null;
            var keyText = hasEntry ? sharedEntry.Key : "None";

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent($"Key: {keyText}", tooltip: keyText), _styles.LabelStyle, _styles.LabelOptions);

            if (hasEntry == false && GUILayout.Button("Generate", _styles.ButtonStyle, _styles.FlexibleContentOptions)) {
                if (TryCreateUniqueLocalizationKey(sharedData, _attribute.Format, oldKey: null, out var key)) {
                    sharedEntry = _editor.CreateSharedEntry(key);
                }
            }

            var findButtonContent = new GUIContent("Find", "Find existing key in table");
            if (hasEntry == false && GUILayout.Button(findButtonContent, _styles.ButtonStyle, _styles.FlexibleContentOptions)) {
                if (TryCreateLocalizationKey(_attribute.Format, out var key)) {
                    if (sharedData.Contains(key)) {
                        sharedEntry = sharedData.GetEntry(key);
                        _editor.SetSharedEntryReference(sharedEntry);
                    }
                    else {
                        Debug.Log($"Unable to find an existing entry with the key '{key}'");
                    }
                }
            }

            if (hasEntry && GUILayout.Button("Regenerate", _styles.ButtonStyle, _styles.FlexibleContentOptions)) {
                if (TryCreateUniqueLocalizationKey(sharedData, _attribute.Format, sharedEntry.Key, out var key)) {
                    _editor.RenameSharedEntry(key);
                }
            }

            if (hasEntry) {
                if (GUILayout.Button(new GUIContent("❐", "Duplicate table entry"), _styles.SquareContentOptions)) {
                    if (TryCreateUniqueLocalizationKey(sharedData, _attribute.Format, sharedEntry.Key, out var key)) {
                        _editor.CreateSharedEntry(key);
                        _editor.CopySharedEntryValuesFrom(sharedEntry);
                    }
                    
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    GUIUtility.ExitGUI();
                }
                
                if (GUILayout.Button(new GUIContent("○", "Set reference empty"), _styles.SquareContentOptions)) {
                    _editor.SetSharedEntryReferenceEmpty();
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(new GUIContent("✕", "Remove table entry"), _styles.SquareContentOptions)) {
                    _editor.RemoveSharedEntry();
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    GUIUtility.ExitGUI();
                }
            }
            else {
                SkipButtonControl();
                SkipButtonControl();
                SkipButtonControl();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLocale(LocaleIdentifier locale, ref SharedTableData.SharedTableEntry sharedEntry) {
            var table = _editor.GetLocalizationTable(locale);
            var entry = sharedEntry != null ? _editor.GetLocalizationTableEntry(table) : null;

            if (table == null) {
                return;
            }

            EditorGUI.BeginChangeCheck();
            BeginVerticalContentSizeFitter();

            var textControlName = GetTextControlName(locale);
            GUI.SetNextControlName(textControlName);
            var oldText = entry?.Value ?? string.Empty;
            var newText = GUILayout.TextArea(oldText, _styles.TextStyle, _styles.TextOptions);

            EndVerticalContentSizeFitter();

            var isTextSelected = GUI.GetNameOfFocusedControl() == textControlName;
            var textRect = GUILayoutUtility.GetLastRect();
            EditorGUI.LabelField(textRect, locale.Code, isTextSelected ? _styles.SelectedLocaleMarkerStyle : _styles.NormalLocaleMarkerStyle);

            if (EditorGUI.EndChangeCheck() == false) {
                return;
            }

            if (entry != null) {
                _editor.SetLocalizationTableEntryValue(entry, newText);
                return;
            }

            if (sharedEntry != null) {
                entry = _editor.CreateLocalizationTableEntry(table, sharedEntry.Key);
            }
            else {
                if (TryCreateUniqueLocalizationKey(table.SharedData, _attribute.Format, oldKey: null, key: out var key) == false) {
                    return;
                }

                entry = _editor.CreateLocalizationTableEntry(table, key);
            }

            _editor.SetLocalizationTableEntryValue(entry, newText);
            
            GUIUtility.ExitGUI();
        }

        private string GetTextControlName(LocaleIdentifier locale) {
            return $"{TextControlNamePrefix}@{_property.Path}-{locale.Code}";
        }

        #endregion
        
        #region Update

        public void Update() {
            CheckForErrors();
        }

        private void CheckForErrors() {
            if (_settingsVersionOnPrevKeySolverRun == LocalizationKeyGeneratorSettings.Instance.Version)
                return;

            _settingsVersionOnPrevKeySolverRun = LocalizationKeyGeneratorSettings.Instance.Version;
            _keySolver.CheckForErrors(_property, _attribute.Format);
        }
        
        #endregion
        
        #region Localization tools

        private bool TryCreateLocalizationKey(string keyFormat, out string key) {
            _settingsVersionOnPrevKeySolverRun = LocalizationKeyGeneratorSettings.Instance.Version;
            return _keySolver.TryCreateKey(_property, keyFormat, out key);
        }

        private bool TryCreateUniqueLocalizationKey(SharedTableData sharedData, string keyFormat, string oldKey, out string key) {
            if (sharedData == null) {
                key = null;
                return false;
            }
            
            _settingsVersionOnPrevKeySolverRun = LocalizationKeyGeneratorSettings.Instance.Version;
            return _keySolver.TryCreateUniqueKey(_property, keyFormat, sharedData, oldKey, out key);
        }

        #endregion

        #region Layout

        private void SkipButtonControl() {
            GUI.Button(new Rect(), GUIContent.none);
        }

        private void BeginIndentedGroup() {
            SirenixEditorGUI.BeginIndentedVertical();
        }

        private void EndIndentedGroup() {
            SirenixEditorGUI.EndIndentedVertical();
        }
        
        private void BeginVerticalContentSizeFitter() {
            GUILayout.BeginVertical(_styles.ContentSizeFitterOptions);
        }

        private void EndVerticalContentSizeFitter() {
            GUILayout.EndVertical();
        }

        private void BeginIgnoreIndent() {
            GUIHelper.PushIndentLevel(EditorGUI.indentLevel);
            EditorGUI.indentLevel = 0;
        }

        private void EndIgnoreIndent() {
            GUIHelper.PopIndentLevel();
        }
        
        #endregion
    }
}