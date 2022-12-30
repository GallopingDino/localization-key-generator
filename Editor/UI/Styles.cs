using System.Reflection;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator.Editor.UI {
    internal class Styles {
        public Texture WarningIcon { private set; get; }
        public GUIStyle ErrorStyle { private set; get; }
        public GUIStyle LabelStyle { private set; get; }
        public GUIStyle ButtonStyle { private set; get; }
        public GUIStyle TextStyle { private set; get; }
        public GUIStyle NormalLocaleMarkerStyle { private set; get; }
        public GUIStyle SelectedLocaleMarkerStyle { private set; get; }
        public GUILayoutOption[] LabelOptions { private set; get; }
        public GUILayoutOption[] FlexibleContentOptions { private set; get; }
        public GUILayoutOption[] SquareContentOptions { private set; get; }
        public GUILayoutOption[] ContentSizeFitterOptions { private set; get; }
        public GUILayoutOption[] TextOptions { private set; get; }
        public GUIContent EditTableButton { private set; get; }
        
        #region Initialization

        public Styles() {
            InitializeStyles();
            InitializeLayoutOptions();
        }

        private void InitializeStyles() {
            ButtonStyle ??= new GUIStyle(EditorStyles.miniButton) {
                font = EditorStyles.miniFont, 
                richText = true
            };

            ErrorStyle ??= new GUIStyle(EditorStyles.helpBox) {
                richText = true
            };
            
            LabelStyle ??= new GUIStyle(EditorStyles.label) {
                alignment = TextAnchor.MiddleLeft
            };
            
            TextStyle ??= new GUIStyle(EditorStyles.textArea) {
                wordWrap = true,
                padding = {
                    right = 20
                }
            };

            if (NormalLocaleMarkerStyle == null || SelectedLocaleMarkerStyle == null) {
                var clearTexture = new Texture2D(1, 1);
                clearTexture.SetPixel(0, 0, Color.clear);
                clearTexture.Apply();
                NormalLocaleMarkerStyle = new GUIStyle(EditorStyles.miniLabel) {
                    normal = {
                        background = clearTexture
                    },
                    padding = new RectOffset(0, 7, 2, 0),
                    alignment = TextAnchor.UpperRight
                };
                SelectedLocaleMarkerStyle = new GUIStyle(NormalLocaleMarkerStyle) {
                    normal = {
                        textColor = GUI.skin.settings.selectionColor
                    }
                };
            }

            var iconsType = Assembly.Load("Unity.Localization.Editor")?.GetType("UnityEditor.Localization.EditorIcons");
            
            // Depends on localization package version
            EditTableButton ??= iconsType?.GetProperty("StringTable", BindingFlags.Static | BindingFlags.Public)?
                                    .GetValue(null) as GUIContent;
            
            EditTableButton ??= new GUIContent(iconsType?.GetProperty("TableWindow", BindingFlags.Static | BindingFlags.Public)?
                              .GetValue(null) as Texture, "Open table");
            
            EditTableButton ??= new GUIContent("T", "Open table");

            WarningIcon ??= typeof(EditorGUIUtility)
                                .GetMethod("GetHelpIcon", BindingFlags.NonPublic | BindingFlags.Static)?
                                .Invoke(null, new object[] { MessageType.Warning }) as Texture;
        }

        private void InitializeLayoutOptions() {
            const float SquareButtonWidth = 30;
            LabelOptions ??= new[] { GUILayout.Width(100), GUILayout.ExpandWidth(true) };
            TextOptions ??= new[] { GUILayout.ExpandHeight(true), GUILayout.MaxHeight(200) };            
            ContentSizeFitterOptions ??= new [] { GUILayout.MinHeight(10) };

            FlexibleContentOptions ??= new[] { GUILayout.ExpandWidth(true), GUILayout.MaxWidth(int.MaxValue), GUILayout.Height(EditorGUIUtility.singleLineHeight) };
            SquareContentOptions ??= new[] { GUILayout.Width(SquareButtonWidth), GUILayout.ExpandWidth(false), GUILayout.Height(EditorGUIUtility.singleLineHeight) };
        }
        
        #endregion
        
        #region Update

        public void Update() {
            UpdateLayoutOptions();
        }

        private void UpdateLayoutOptions() {
            var isRepaintingSelf = Event.current.type == EventType.Repaint && GUIHelper.CurrentWindowHasFocus;
            var isDragging = Event.current.type == EventType.DragUpdated;

            if (!isRepaintingSelf && !isDragging) {
                return;
            }

            const float LabelContentRatio = 0.35f;
            const float MinLabelWidth = 30;

            var positionWidth = GUIHelper.GetCurrentLayoutRect().width;
            var labelWidth = Mathf.Max(MinLabelWidth, positionWidth * LabelContentRatio);

            LabelOptions[0] = GUILayout.Width(labelWidth);
        }

        #endregion
    }
}