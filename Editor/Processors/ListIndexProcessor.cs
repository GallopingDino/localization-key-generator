namespace Dino.LocalizationKeyGenerator.Editor.Processors {
    internal sealed class ListIndexProcessor : ParameterProcessor {
        public override string ParameterName => "listIndex";

        public override bool CanProcess(PropertyContext context) {
            return context.IsArrayElement;
        }

        public override object Process(PropertyContext context) {
            return context.ArrayIndex;
        }
    }
}
