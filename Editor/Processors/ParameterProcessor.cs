namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    public abstract class ParameterProcessor {
        public abstract string ParameterName { get; }
        public abstract bool CanProcess(PropertyContext context);
        public abstract object Process(PropertyContext context);
    }
}
