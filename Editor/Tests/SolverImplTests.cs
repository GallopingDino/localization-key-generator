using System;
using NUnit.Framework;
using Sirenix.OdinInspector.Editor;
using UnityEngine.Localization;
using Dino.LocalizationKeyGenerator.Editor.Solvers;

namespace Dino.LocalizationKeyGenerator.Editor.Tests {
    public class SolverImplTests {
        [Serializable]
        [AutoKeyParams("container-parameter", nameof(ContainingTypeField))]
        private class LocalizedStringContainer {
            [AutoKeyParams("field-parameter", "Field parameter")]
            public LocalizedString TargetString;
            public string ContainingTypeField = "Container field";
        }

        private static InspectorProperty FindTargetStringProperty(PropertyTree<LocalizedStringContainer> containerTree) {
            return containerTree.RootProperty.FindChild(p => p.Name == nameof(LocalizedStringContainer.TargetString), includeSelf: false);
        }

        [Test]
        public void TryResolveLine_FieldParameter_ReturnsParameterValue() {
            var stringContainer = new LocalizedStringContainer();
            using (var containerTree = new PropertyTree<LocalizedStringContainer>(new[] { stringContainer })) {
                var stringProperty = FindTargetStringProperty(containerTree);
                var solver = new SolverImpl();
                var line = "{field-parameter}";

                solver.CollectParameters(stringProperty);
                var isResolved = solver.TryResolveLine(stringProperty, line, out var resolvedLine);
                var errorReport = solver.GetErrors();

                Assert.IsTrue(isResolved);
                Assert.AreEqual("Field parameter", resolvedLine);
                Assert.IsEmpty(errorReport);
            }
        }

        [Test]
        public void TryResolveLine_ContainerParameter_ReturnsParameterValue() {
            var stringContainer = new LocalizedStringContainer();
            using (var containerTree = new PropertyTree<LocalizedStringContainer>(new[] { stringContainer })) {
                var stringProperty = FindTargetStringProperty(containerTree);
                var solver = new SolverImpl();
                var line = "{container-parameter}";

                solver.CollectParameters(stringProperty);
                var isResolved = solver.TryResolveLine(stringProperty, line, out var resolvedLine);
                var errorReport = solver.GetErrors();

                Assert.IsTrue(isResolved);
                Assert.AreEqual(stringContainer.ContainingTypeField, resolvedLine);
                Assert.IsEmpty(errorReport);
            }
        }

        [Test]
        public void TryResolveLine_ContainerFieldName_ReturnsFieldValue() {
            var stringContainer = new LocalizedStringContainer();
            using (var containerTree = new PropertyTree<LocalizedStringContainer>(new[] { stringContainer })) {
                var stringProperty = FindTargetStringProperty(containerTree);
                var solver = new SolverImpl();
                var line = $"{{{nameof(LocalizedStringContainer.ContainingTypeField)}}}";

                var isResolved = solver.TryResolveLine(stringProperty, line, out var resolvedLine);
                var errorReport = solver.GetErrors();

                Assert.IsTrue(isResolved);
                Assert.AreEqual(stringContainer.ContainingTypeField, resolvedLine);
                Assert.IsEmpty(errorReport);
            }
        }

        [Test]
        public void TryResolveLine_Expression_ReturnsSolvedExpression() {
            var localizedString = new LocalizedString(default, default);
            using (var stringTree = new PropertyTree<LocalizedString>(new[] { localizedString })) {
                var stringProperty = stringTree.RootProperty;
                var solver = new SolverImpl();
                var line = "{@1 + 2}";
                
                var isResolved = solver.TryResolveLine(stringProperty, line, out var resolvedLine);
                var errorReport = solver.GetErrors();
                
                Assert.IsTrue(isResolved);
                Assert.AreEqual("3", resolvedLine);
                Assert.IsEmpty(errorReport);
            }
        }
    }
}
