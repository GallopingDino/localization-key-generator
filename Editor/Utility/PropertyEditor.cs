using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace Dino.LocalizationKeyGenerator.Editor.Utility {
    internal class PropertyEditor {
        private static event Action<StringTableCollection, SharedTableData.SharedTableEntry> EntryAddedSystemEvent;
        private static event Action<SharedTableData.SharedTableEntry> EntryModifiedSystemEvent;
        
        public event Action EntryAdded;
        public event Action EntryModified;
        public event Action EntryRemoved;

        private readonly InspectorProperty _property;
        private readonly Undo _undo;

        #region Initialization

        public PropertyEditor(InspectorProperty property) {
            _property = property;
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

        public LocalizedString GetLocalizedString() {
            return (LocalizedString) _property.ValueEntry.WeakSmartValue;
        }

        public StringTableCollection GetTableCollection() {
            var localizedString = GetLocalizedString();
            var tableReference = localizedString.TableReference;
            if (tableReference.ReferenceType == TableReference.Type.Empty) return null;
            return LocalizationEditorSettings.GetStringTableCollections().FirstOrDefault(c => {
                switch (tableReference.ReferenceType) {
                    case TableReference.Type.Name: return tableReference == c.TableCollectionName;
                    case TableReference.Type.Guid: return tableReference == c.SharedData.TableCollectionNameGuid;
                    default: throw new ArgumentOutOfRangeException();
                }
            });
    }

        public SharedTableData GetSharedData() {
            return GetTableCollection()?.SharedData;
        }

        public SharedTableData.SharedTableEntry GetSharedEntry() {
            var localizedString = GetLocalizedString();
            var sharedData = GetSharedData();
            if (sharedData == null || localizedString.TableEntryReference.ReferenceType == TableEntryReference.Type.Empty) {
                return null;
            }
            return sharedData.GetEntryFromReference(localizedString.TableEntryReference);
        }

        public bool IsLocalizationTableAvailable(LocaleIdentifier locale) {
            var tableCollection = GetTableCollection();
            return tableCollection != null && tableCollection.GetTable(locale) != null;
        }

        public StringTable GetLocalizationTable(LocaleIdentifier locale) {
            var tableCollection = GetTableCollection();
            if (tableCollection == null) {
                return null;
            }
            return (tableCollection.GetTable(locale) ?? tableCollection.Tables.FirstOrDefault().asset) as StringTable;
        }

        public StringTableEntry GetLocalizationTableEntry(StringTable table) {
            if (table == null) {
                return null;
            }
            var localizedString = GetLocalizedString();
            return GetLocalizationTableEntryByReference(table, localizedString.TableEntryReference);
        }

        private StringTableEntry GetLocalizationTableEntryByReference(StringTable table, TableEntryReference entryReference) {
            var localizedString = GetLocalizedString();
            if (localizedString.TableEntryReference.ReferenceType == TableEntryReference.Type.Empty) {
                return null;
            }
            return table.GetEntryFromReference(entryReference);
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
            var localizedString = GetLocalizedString();
            localizedString.SetReference(localizedString.TableReference, (TableEntryReference) newEntry.Id);
            RaiseTableEntryAddedEvent(newEntry);
            return newEntry;
        }

        public void CopySharedEntryValuesFrom(SharedTableData.SharedTableEntry from) {
            if (from == null) {
                return;
            }

            var sharedEntry = GetSharedEntry();
            if (sharedEntry == null) {
                return;
            }
            
            var sourceReference = from.Id != 0 ? (TableEntryReference) from.Id : (TableEntryReference) from.Key;
            var tableCollection = GetTableCollection();
            if (tableCollection == null) {
                return;
            }
            
            foreach (var table in tableCollection.StringTables) {
                var sourceEntry = GetLocalizationTableEntryByReference(table, sourceReference);
                if (sourceEntry == null) {
                    continue;
                }
                
                var tableEntry = CreateLocalizationTableEntry(table, sharedEntry.Key);
                SetLocalizationTableEntryValue(tableEntry, sourceEntry.Value);
            }
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
            _undo.RegisterLocalizationTableChanges(entry.Table, "Set value");
            entry.Value = newText;
        }

        public void RenameSharedEntry(string key) {
            var localizedString = GetLocalizedString();
            var sharedData = GetSharedData();
            var sharedEntry = GetSharedEntry();
            _undo.RegisterSelfChanges("Rename entry");
            _undo.RegisterSharedDataChanges(sharedData, "Rename entry");
            switch (localizedString.TableEntryReference.ReferenceType) {
                case TableEntryReference.Type.Name:
                    sharedData.RenameKey(localizedString.TableEntryReference.Key, key);
                    break;
                case TableEntryReference.Type.Id:
                    sharedData.RenameKey(localizedString.TableEntryReference.KeyId, key);
                    break;
               default:
                   return;
            }
            localizedString.SetReference(localizedString.TableReference, (TableEntryReference) sharedEntry.Id);
            RaiseTableEntryModifiedEvent(sharedEntry);
        }

        public void RemoveSharedEntry() {
            var localizedString = GetLocalizedString();
            _undo.RegisterSelfChanges("Remove key");
            var collection = GetTableCollection();
            _undo.RegisterCollectionChanges(collection, "Remove key");
            collection.RemoveEntry(localizedString.TableEntryReference);
            localizedString.SetReference(localizedString.TableReference, default);
            RaiseTableEntryRemovedEvent();
        }

        public void SetSharedEntryReference(SharedTableData.SharedTableEntry entry) {
            var localizedString = GetLocalizedString();
            _undo.RegisterSelfChanges("Set entry");
            localizedString.SetReference(localizedString.TableReference, (TableEntryReference) entry.Id);
        }

        public void SetSharedEntryReferenceEmpty() {
            var localizedString = GetLocalizedString();
            _undo.RegisterSelfChanges("Set empty");
            localizedString.SetReference(localizedString.TableReference, null);
        }

        public void SetTableCollection(StringTableCollection collection) {
            var localizedString = GetLocalizedString();
            _undo.RegisterSelfChanges("Select localization table");
            localizedString.TableReference = collection != null ? collection.TableCollectionNameReference : default;
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

        public Comment GetComment() {
            var sharedData = GetSharedData();
            var sharedEntry = GetSharedEntry();
            
            if (sharedData == null || sharedEntry == null) {
                return null;
            }
            return sharedEntry.Metadata.GetMetadata<Comment>();
        }

        public void RemoveComment() {
            var sharedData = GetSharedData();
            var sharedEntry = GetSharedEntry();
            
            if (sharedData == null || sharedEntry == null) {
                return;
            }
            _undo.RegisterSharedDataChanges(sharedData, "Remove comment");
            var comment = sharedEntry.Metadata.GetMetadata<Comment>();
            if (comment == null) {
                return;
            }
            sharedEntry.Metadata.RemoveMetadata(comment);
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