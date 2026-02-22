using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dino.LocalizationKeyGenerator.Editor {
    public class PropertyContext {
        private static readonly Dictionary<(Type, string), FieldInfo> FieldInfoCache = new Dictionary<(Type, string), FieldInfo>();
        private const BindingFlags AllInstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly SerializedProperty _serializedProperty;
        
        private PropertyContext _parent;
        private bool _parentResolved;
        private Type _valueType;
        private bool _valueTypeResolved;
        private FieldInfo _fieldInfo;
        private bool _fieldInfoResolved;
        private string _resolvedPath;
        private bool _resolvedPathInitialized;

        // Hold SO reference to prevent garbage collection when SerializedProperty was resolved from a different SerializedObject
        // This happens when Odin emits wrapper ScriptableObjects for properties
        private SerializedObject _actualSerializedObject;

        public string Path => _serializedProperty.propertyPath;
        public string Name => _serializedProperty.name;
        public Object RootObject => _serializedProperty.serializedObject.targetObject;

        private string ResolvedPath {
            get {
                if (_resolvedPathInitialized) {
                    return _resolvedPath;
                }

                _resolvedPathInitialized = true;
                _resolvedPath = FindFullPropertyPath();
                return _resolvedPath;
            }
        }

        public virtual PropertyContext Parent {
            get {
                if (_parentResolved) {
                    return _parent;
                }

                _parentResolved = true;
                _parent = ResolveParent();
                return _parent;
            }
        }

        public virtual Type ValueType {
            get {
                if (_valueTypeResolved) {
                    return _valueType;
                }

                _valueTypeResolved = true;
                var value = GetValue();
                if (value != null) {
                    _valueType = value.GetType();
                    return _valueType;
                }
                var fi = GetFieldInfo();
                _valueType = fi?.FieldType;
                return _valueType;
            }
        }

        public Type ParentType {
            get {
                var fi = GetFieldInfo();
                return fi?.DeclaringType;
            }
        }

        public bool IsArrayElement {
            get {
                var path = ResolvedPath;
                return path.Contains(".Array.data[");
            }
        }

        public int ArrayIndex {
            get {
                if (!IsArrayElement) {
                    return -1;
                }

                var path = ResolvedPath;
                var lastBracket = path.LastIndexOf('[');
                if (lastBracket < 0) {
                    return -1;
                }

                var closeBracket = path.IndexOf(']', lastBracket);
                if (closeBracket < 0) {
                    return -1;
                }

                var indexStr = path.Substring(lastBracket + 1, closeBracket - lastBracket - 1);
                return int.TryParse(indexStr, out var idx) ? idx : -1;
            }
        }

        public PropertyContext(SerializedProperty serializedProperty) {
            var actualProperty = TryResolveFromActualObject(serializedProperty);
            if (actualProperty != null) {
                _actualSerializedObject = actualProperty.serializedObject;
                _serializedProperty = actualProperty;
            }
            else {
                _serializedProperty = serializedProperty;
            }
        }

        public bool IsParentCollection() {
            var parent = Parent;
            if (parent == null) return false;
            var parentValue = parent.GetValue();
            return parentValue is IList;
        }

        public virtual object GetValue() {
            return ResolveValue(RootObject, ResolvedPath);
        }

        public T GetAttribute<T>() where T : Attribute {
            var fi = GetFieldInfo();
            return fi?.GetCustomAttribute<T>();
        }

        public IEnumerable<T> GetAttributes<T>() where T : Attribute {
            var fi = GetFieldInfo();
            if (fi == null) return Array.Empty<T>();
            return fi.GetCustomAttributes<T>(true);
        }

        private FieldInfo GetFieldInfo() {
            if (_fieldInfoResolved) return _fieldInfo;
            _fieldInfoResolved = true;
            _fieldInfo = ResolveFieldInfo(RootObject.GetType(), ResolvedPath);
            return _fieldInfo;
        }

        private PropertyContext ResolveParent() {
            var path = ResolvedPath;

            // Handle array element: strip .Array.data[N]
            if (path.EndsWith("]")) {
                var arrayDataIdx = path.LastIndexOf(".Array.data[", StringComparison.Ordinal);
                if (arrayDataIdx >= 0) {
                    var parentPath = path.Substring(0, arrayDataIdx);
                    var parentProp = _serializedProperty.serializedObject.FindProperty(parentPath);
                    return parentProp != null ? new PropertyContext(parentProp) : null;
                }
            }

            // Handle regular nested: strip last .fieldName
            var lastDot = path.LastIndexOf('.');
            if (lastDot < 0) {
                return CreateRootContext();
            }

            var parentFieldPath = path.Substring(0, lastDot);
            var parentSerializedProp = _serializedProperty.serializedObject.FindProperty(parentFieldPath);
            return parentSerializedProp != null ? new PropertyContext(parentSerializedProp) : null;
        }

        private PropertyContext CreateRootContext() {
            var so = _serializedProperty.serializedObject;
            // Use a known property that always exists as a sentinel for root
            var scriptProp = so.FindProperty("m_Script");
            if (scriptProp != null) {
                return new RootPropertyContext(so);
            }
            return null;
        }

        private string FindFullPropertyPath() {
            var path = _serializedProperty.propertyPath;

            if (ResolveFieldInfo(RootObject.GetType(), path) != null) {
                return path;
            }

            // Path can't be resolved from root (e.g., Odin passed a relative path).
            // Search the SerializedObject for a property with a matching path suffix.
            var so = _serializedProperty.serializedObject;
            var suffix = "." + path;
            var iter = so.GetIterator();
            while (iter.Next(true)) {
                var iterPath = iter.propertyPath;
                if (!iterPath.EndsWith(suffix)) {
                    continue;
                }

                if (SerializedProperty.DataEquals(_serializedProperty, iter)) {
                    return iterPath;
                }
            }

            return path;
        }

        /// <summary>
        /// Necessary for Odin compatibility.
        /// Odin emits wrapper ScriptableObjects for properties.
        /// This method detects Odin emitted ScriptableObjects and resolves the property
        /// from the actual inspected object to restore the full property hierarchy.
        /// </summary>
        private static SerializedProperty TryResolveFromActualObject(SerializedProperty property) {
            var rootType = property.serializedObject.targetObject.GetType();
            if (rootType.FullName == null || !rootType.FullName.Contains("EmittedUnityProperties")) {
                return null;
            }

            var actualObject = Selection.activeObject;
            if (actualObject == null) {
                return null;
            }

            var actualSerializedObject = new SerializedObject(actualObject);
            var path = property.propertyPath;
            var suffix = "." + path;
            var iter = actualSerializedObject.GetIterator();
            while (iter.Next(true)) {
                if (!iter.propertyPath.EndsWith(suffix)) {
                    continue;
                }

                if (SerializedProperty.DataEquals(property, iter)) {
                    return iter.Copy();
                }
            }

            return null;
        }

        private static object ResolveValue(object current, string propertyPath) {
            if (current == null || string.IsNullOrEmpty(propertyPath)) {
                return current;
            }

            var segments = propertyPath.Split('.');
            for (var i = 0; i < segments.Length; i++) {
                if (current == null) {
                    return null;
                }

                var segment = segments[i];

                // Handle Array.data[N]
                if (segment == "Array" && i + 1 < segments.Length && segments[i + 1].StartsWith("data[")) {
                    var dataSegment = segments[i + 1];
                    var idxStr = dataSegment.Substring(5, dataSegment.Length - 6); // strip "data[" and "]"
                    if (int.TryParse(idxStr, out var idx) && current is IList list && idx < list.Count) {
                        current = list[idx];
                    } else {
                        return null;
                    }
                    i++; // skip data[N] segment
                    continue;
                }

                var fi = FindFieldInHierarchy(current.GetType(), segment);
                if (fi == null) {
                    return null;
                }

                current = fi.GetValue(current);
            }

            return current;
        }

        private static FieldInfo ResolveFieldInfo(Type rootType, string propertyPath) {
            if (rootType == null || string.IsNullOrEmpty(propertyPath)) {
                return null;
            }

            var segments = propertyPath.Split('.');
            var currentType = rootType;
            FieldInfo lastField = null;

            for (var i = 0; i < segments.Length; i++) {
                if (currentType == null) {
                    return null;
                }

                var segment = segments[i];

                // Handle Array.data[N] — skip both segments, drill into element type
                if (segment == "Array" && i + 1 < segments.Length && segments[i + 1].StartsWith("data[")) {
                    currentType = GetElementType(currentType);
                    i++; // skip data[N]
                    lastField = null;
                    continue;
                }

                var fi = FindFieldInHierarchy(currentType, segment);
                if (fi == null) {
                    return null;
                }
                lastField = fi;
                currentType = fi.FieldType;
            }

            return lastField;
        }

        private static Type GetElementType(Type collectionType) {
            if (collectionType.IsArray) {
                return collectionType.GetElementType();
            }

            if (collectionType.IsGenericType) {
                var args = collectionType.GetGenericArguments();
                if (args.Length > 0) {
                    return args[args.Length - 1]; // Last arg (for Dictionary it's value type)
                }
            }
            return typeof(object);
        }

        private static FieldInfo FindFieldInHierarchy(Type type, string fieldName) {
            var key = (type, fieldName);
            if (FieldInfoCache.TryGetValue(key, out var cached)) {
                return cached;
            }

            var current = type;
            while (current != null) {
                var fi = current.GetField(fieldName, AllInstanceFields | BindingFlags.DeclaredOnly);
                if (fi != null) {
                    FieldInfoCache[key] = fi;
                    return fi;
                }
                current = current.BaseType;
            }

            FieldInfoCache[key] = null;
            return null;
        }

        /// <summary>
        /// Represents the root serialized object itself (parent of top-level properties).
        /// </summary>
        private class RootPropertyContext : PropertyContext {
            private readonly SerializedObject _serializedObject;

            public RootPropertyContext(SerializedObject serializedObject)
                : base(serializedObject.FindProperty("m_Script")) {
                _serializedObject = serializedObject;
            }

            public override object GetValue() => _serializedObject.targetObject;
            public override Type ValueType => _serializedObject.targetObject.GetType();
            public override PropertyContext Parent => null;
        }
    }
}
