using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using Dino.LocalizationKeyGenerator.Editor.Solvers;

namespace Dino.LocalizationKeyGenerator.Editor.Tests {
    public class SolverImplTests {
        [AutoKeyParams("container-parameter", nameof(ContainingTypeField))]
        private class LocalizedStringContainer : ScriptableObject {
            [AutoKeyParams("field-parameter", "Field parameter")]
            public LocalizedString TargetString;
            public string ContainingTypeField = "Container field";
        }

        private LocalizedStringContainer _container;

        [SetUp]
        public void SetUp() {
            _container = ScriptableObject.CreateInstance<LocalizedStringContainer>();
        }

        [TearDown]
        public void TearDown() {
            UnityEngine.Object.DestroyImmediate(_container);
        }

        private PropertyContext CreateTargetStringContext() {
            var so = new SerializedObject(_container);
            var prop = so.FindProperty(nameof(LocalizedStringContainer.TargetString));
            return new PropertyContext(prop);
        }

        [Test]
        public void TryResolveLine_FieldParameter_ReturnsParameterValue() {
            var context = CreateTargetStringContext();
            var solver = new SolverImpl();
            var line = "{field-parameter}";

            solver.CollectParameters(context);
            var isResolved = solver.TryResolveLine(context, line, out var resolvedLine);
            var errorReport = solver.GetErrors();

            Assert.IsTrue(isResolved);
            Assert.AreEqual("Field parameter", resolvedLine);
            Assert.IsEmpty(errorReport);
        }

        [Test]
        public void TryResolveLine_ContainerParameter_ReturnsParameterValue() {
            var context = CreateTargetStringContext();
            var solver = new SolverImpl();
            var line = "{container-parameter}";

            solver.CollectParameters(context);
            var isResolved = solver.TryResolveLine(context, line, out var resolvedLine);
            var errorReport = solver.GetErrors();

            Assert.IsTrue(isResolved);
            Assert.AreEqual(_container.ContainingTypeField, resolvedLine);
            Assert.IsEmpty(errorReport);
        }

        [Test]
        public void TryResolveLine_ContainerFieldName_ReturnsFieldValue() {
            var context = CreateTargetStringContext();
            var solver = new SolverImpl();
            var line = $"{{{nameof(LocalizedStringContainer.ContainingTypeField)}}}";

            var isResolved = solver.TryResolveLine(context, line, out var resolvedLine);
            var errorReport = solver.GetErrors();

            Assert.IsTrue(isResolved);
            Assert.AreEqual(_container.ContainingTypeField, resolvedLine);
            Assert.IsEmpty(errorReport);
        }

#if ODIN_SUPPORT
        [Test]
        public void TryResolveLine_Expression_ReturnsSolvedExpression() {
            var context = CreateTargetStringContext();
            var solver = new SolverImpl();
            var line = "{@1 + 2}";

            var isResolved = solver.TryResolveLine(context, line, out var resolvedLine);
            var errorReport = solver.GetErrors();

            Assert.IsTrue(isResolved);
            Assert.AreEqual("3", resolvedLine);
            Assert.IsEmpty(errorReport);
        }
#else
        [Test]
        public void TryResolveLine_Expression_FailsWithoutOdin() {
            var context = CreateTargetStringContext();
            var solver = new SolverImpl();
            var line = "{@1 + 2}";

            var isResolved = solver.TryResolveLine(context, line, out var resolvedLine);

            Assert.IsFalse(isResolved);
        }
#endif
    }
}
