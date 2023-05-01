using Sirenix.OdinInspector.Editor;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class ListIndexProcessor : ParameterProcessor {
        public override string ParameterName => "listIndex";
        
        public override bool CanProcess(InspectorProperty property) {
            return property.Parent?.ChildResolver is ICollectionResolver;
        }

        public override object Process(InspectorProperty property) {
            return property.Index;
        }
    }
}