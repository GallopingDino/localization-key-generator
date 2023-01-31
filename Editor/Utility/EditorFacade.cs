using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace Dino.LocalizationKeyGenerator.Editor.Utility {
    internal class EditorFacade {
        private static event Action<StringTableCollection, SharedTableData.SharedTableEntry> EntryAddedSystemEvent;
        private static event Action<SharedTableData.SharedTableEntry> EntryModifiedSystemEvent;
        
        public event Action EntryAdded;
        public event Action EntryModified;
        public event Action EntryRemoved;

        private readonly LocalizedString _localizedString;
        private readonly Undo _undo;

        #region Initialization

        public EditorFacade(InspectorProperty property) {
            _localizedString = property.ValueEntry.WeakSmartValue as LocalizedString;
            _undo = new Undo(property);
            InitializeSystemEvents();
        }

        private void InitializeSystemEvents() {
            if (EntryAddedSystemEvent == null) {
                EntryAddedSystemEvent += (c, e) =>
                    typeof(LocalizationEditorEvents)
                        .GetMethod("RaiseTableEntryAdded", BindingFlags.Instance | BindingFlags.NonPublic)?
                        .Invoke(LocalizationEditorSettings.EditorEvents, new[] {(object) c, e});
            }

            if (EntryModifiedSystemEvent == null) {
                EntryModifiedSystemEvent += e => 
                    typeof(LocalizationEditorEvents)
                        .GetMethod("RaiseTableEntryModified", BindingFlags.Instance | BindingFlags.NonPublic)?
                        .Invoke(LocalizationEditorSettings.EditorEvents, new[] { e });
            }
        }
        
        #endregion
        
        #region Public API

        public StringTableCollection GetTableCollection() {
            var tableReference = _localizedString.TableReference;
            if (tableReference.ReferenceType == TableReference.Type.Empty) return null;
            return LocalizationEditorSettings.GetStringTableCollections().FirstOrDefault(c => 
                tableReference.ReferenceType switch {
                    TableReference.Type.Name => tableReference == c.TableCollectionName,
                    TableReference.Type.Guid => tableReference == c.SharedData.TableCollectionNameGuid,
                    _ => throw new ArgumentOutOfRangeException()
                });
        }

        public SharedTableData GetSharedData() {
            return GetTableCollection()?.SharedData;
        }

        public SharedTableData.SharedTableEntry GetSharedEntry() {
            var sharedData = GetSharedData();
            if (sharedData == null || _localizedString.TableEntryReference.ReferenceType == TableEntryReference.Type.Empty) {
                return null;
            }
            return sharedData.GetEntryFromReference(_localizedString.TableEntryReference);
        }

        public StringTable GetLocalizationTable(LocaleIdentifier locale) {
            var tableCollection = GetTableCollection();
            if (tableCollection == null) return null;
            return (tableCollection.GetTable(locale) ?? tableCollection.Tables.FirstOrDefault().asset) as StringTable;
        }

        public StringTableEntry GetLocalizationTableEntry(StringTable table) {
            if (table == null || _localizedString.TableEntryReference.ReferenceType == TableEntryReference.Type.Empty) {
                return null;
            }

            return table.GetEntryFromReference(_localizedString.TableEntryReference);
        }

        public SharedTableData.SharedTableEntry CreateSharedEntry(string key) {
            var sharedData = GetSharedData();
            if (sharedData == null) {
                return null;
            }
            
            var existingEntry = sharedData.GetEntry(key);
            if (existingEntry != null) {
                return existingEntry;
            }

            _undo.RegisterSelfChanges("Create entry");
            _undo.RegisterSharedDataChanges(sharedData, "Create entry");
            
            var newEntry = sharedData.AddKey(key);
            _localizedString.TableEntryReference = (TableEntryReference) key;
            RaiseTableEntryAddedEvent(newEntry);
            return newEntry;
        }

        public StringTableEntry CreateLocalizationTableEntry(StringTable table, string key) {
            if (table == null) {
                return null;
            }
            
            var existingEntry = table.GetEntry(key);
            if (existingEntry != null) {
                return existingEntry;
            }

            _undo.RegisterSelfChanges("Create entry");
            _undo.RegisterLocalizationTableChanges(table, "Create entry");
            
            var sharedEntry = CreateSharedEntry(key);
            var tableEntry = table.AddEntry(sharedEntry.Id, "");
            return tableEntry;
        }

        public void SetLocalizationTableEntryValue(StringTableEntry entry, string newText) {
            _undo.RegisterLocalizationTableChanges(entry.Table, "Set smart format");
            entry.Value = newText;
        }

        public void RenameSharedEntry(string key) {
            var sharedData = GetSharedData();
            var sharedEntry = GetSharedEntry();
            _undo.RegisterSelfChanges("Rename entry");
            _undo.RegisterSharedDataChanges(sharedData, "Rename entry");
            switch (_localizedString.TableEntryReference.ReferenceType) {
                case TableEntryReference.Type.Name:
                    sharedData.RenameKey(_localizedString.TableEntryReference.Key, key);
                    break;
                case TableEntryReference.Type.Id:
                    sharedData.RenameKey(_localizedString.TableEntryReference.KeyId, key);
                    break;
               default:
                   return;
            }
           _localizedString.TableEntryReference = (TableEntryReference) key;
            RaiseTableEntryModifiedEvent(sharedEntry);
        }

        public void RemoveSharedEntry() {
            _undo.RegisterSelfChanges("Remove key");
            var collection = GetTableCollection();
            _undo.RegisterCollectionChanges(collection, "Remove key");
            collection.RemoveEntry(_localizedString.TableEntryReference);
            _localizedString.TableEntryReference = default;
            RaiseTableEntryRemovedEvent();
        }

        public void SetTableCollection(StringTableCollection collection) {
            _undo.RegisterSelfChanges("Select localization table");
            _localizedString.TableReference = collection != null ? collection.TableCollectionNameReference : default;
        }

        public void SetSharedEntryReferenceEmpty() {
            _undo.RegisterSelfChanges("Set empty");
            _localizedString.SetReference(_localizedString.TableReference, null);
        }

        public void SetComment(string comment) {
            var sharedData = GetSharedData();
            var sharedEntry = GetSharedEntry();
            if (sharedData == null || sharedEntry == null) {
                return;
            }
            _undo.RegisterSharedDataChanges(sharedData, "Set comment");
            var existingCommentMeta = sharedEntry.Metadata.GetMetadata<Comment>();
            if (existingCommentMeta != null) {
                existingCommentMeta.CommentText = comment;
                return;
            }
            sharedEntry.Metadata.AddMetadata(new Comment { CommentText = comment });
        }

        #endregion

        #region Events

        private void RaiseTableEntryAddedEvent(SharedTableData.SharedTableEntry sharedEntry) {
            var collection = GetTableCollection();
            EntryAddedSystemEvent?.Invoke(collection, sharedEntry);
            EntryAdded?.Invoke();
        }

        private void RaiseTableEntryModifiedEvent(SharedTableData.SharedTableEntry sharedEntry) {
            EntryModifiedSystemEvent?.Invoke(sharedEntry);
            EntryModified?.Invoke();
        }

        private void RaiseTableEntryRemovedEvent() {
            EntryRemoved?.Invoke();
        }

        #endregion
    }
}