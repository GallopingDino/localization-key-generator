using Sirenix.OdinInspector.Editor;

namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    public abstract class ParameterProcessor {
        public abstract string ParameterName { get; }
        public abstract bool CanProcess(InspectorProperty property);
        public abstract object Process(InspectorProperty property);
    }
}