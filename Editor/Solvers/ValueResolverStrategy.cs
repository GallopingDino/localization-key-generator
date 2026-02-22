using System.Reflection;

#if ODIN_SUPPORT
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
#endif

namespace Dino.LocalizationKeyGenerator.Editor.Solvers {
    internal static class ValueResolverStrategy {
        private const BindingFlags MemberLookupFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        public static bool TryResolve(PropertyContext context, string expression, out object result) {
            result = null;

            if (string.IsNullOrEmpty(expression)) return false;

            // @expressions are Odin-only
            if (expression.StartsWith("@")) {
#if ODIN_SUPPORT
                return TryResolveWithOdin(context, expression, out result);
#else
                return false;
#endif
            }

            // Try reflection: field or property by name on the parent object
            if (TryResolveByReflection(context, expression, out result)) return true;

#if ODIN_SUPPORT
            return TryResolveWithOdin(context, expression, out result);
#else
            return false;
#endif
        }

        private static bool TryResolveByReflection(PropertyContext context, string memberName, out object result) {
            result = null;

            var parent = context.Parent;
            if (parent == null) return false;

            var parentValue = parent.GetValue();
            if (parentValue == null) return false;

            var parentType = parentValue.GetType();

            var field = parentType.GetField(memberName, MemberLookupFlags);
            if (field != null) {
                result = field.GetValue(parentValue);
                return true;
            }

            var prop = parentType.GetProperty(memberName, MemberLookupFlags);
            if (prop != null && prop.CanRead) {
                try {
                    result = prop.GetValue(parentValue);
                    return true;
                } catch {
                    return false;
                }
            }

            return false;
        }

#if ODIN_SUPPORT
        private static bool TryResolveWithOdin(PropertyContext context, string expression, out object result) {
            try {
                var so = context.SerializedProperty.serializedObject;
                using (var tree = PropertyTree.Create(so)) {
                    var odinProp = tree.GetPropertyAtUnityPath(context.Path);
                    if (odinProp == null) {
                        result = null;
                        return false;
                    }

                    var resolver = ValueResolver.Get<object>(odinProp, expression);
                    if (resolver.HasError) {
                        result = null;
                        return false;
                    }

                    result = resolver.GetValue();
                    return true;
                }
            } catch {
                result = null;
                return false;
            }
        }
#endif
    }
}
