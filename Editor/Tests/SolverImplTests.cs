using System;
using NUnit.Framework;
using Sirenix.OdinInspector.Editor;
using UnityEngine.Localization;
using Dino.LocalizationKeyGenerator.Editor.Solvers;

namespace Dino.LocalizationKeyGenerator.Editor.Tests {
    public class SolverImplTests {
        [Serializable]
        [AutoKeyParams("parameter", nameof(ContainingTypeField))]
        private class LocalizedStringContainer {
            public LocalizedString TargetString;
            public string ContainingTypeField = "ABC";
        }

        private static InspectorProperty FindTargetStringProperty(PropertyTree<LocalizedStringContainer> containerTree) {
            return containerTree.RootProperty.FindChild(p => p.Name == nameof(LocalizedStringContainer.TargetString), includeSelf: false);
        }
        
        [Test]
        public void TryResolveLine_ContainerFieldName_ReturnsFieldValue() {
            var stringContainer = new LocalizedStringContainer();
            using (var containerTree = new PropertyTree<LocalizedStringContainer>(new[] { stringContainer })) {
                var stringProperty = FindTargetStringProperty(containerTree);
                var solver = new SolverImpl();
                var line = $"{{{nameof(LocalizedStringContainer.ContainingTypeField)}}}";

                var isFormatted = solver.TryResolveLine(stringProperty, line, out var formattedLine);
                var errorReport = solver.GetErrors();

                Assert.IsTrue(isFormatted);
                Assert.AreEqual(stringContainer.ContainingTypeField, formattedLine);
                Assert.IsEmpty(errorReport);
            }
        }
        
        [Test]
        public void TryResolveLine_ContainerParameterName_ReturnsParameterValue() {
            var stringContainer = new LocalizedStringContainer();
            using (var containerTree = new PropertyTree<LocalizedStringContainer>(new[] { stringContainer })) {
                var stringProperty = FindTargetStringProperty(containerTree);
                var solver = new SolverImpl();
                var line = "{parameter}";

                solver.CollectParameters(stringProperty);
                var isFormatted = solver.TryResolveLine(stringProperty, line, out var formattedLine);
                var errorReport = solver.GetErrors();

                Assert.IsTrue(isFormatted);
                Assert.AreEqual(stringContainer.ContainingTypeField, formattedLine);
                Assert.IsEmpty(errorReport);
            }
        }
        
        [Test]
        public void TryResolveLine_Expression_ReturnsSolvedExpression() {
            var localizedString = new LocalizedString(default, default);
            using (var containerTree = new PropertyTree<LocalizedString>(new[] { localizedString })) {
                var stringProperty = containerTree.RootProperty;
                var solver = new SolverImpl();
                var line = "{@1 + 2}";
                
                var isFormatted = solver.TryResolveLine(stringProperty, line, out var formattedLine);
                var errorReport = solver.GetErrors();
                
                Assert.IsTrue(isFormatted);
                Assert.AreEqual("3", formattedLine);
                Assert.IsEmpty(errorReport);
            }
        }
    }
}
