using UnityEngine;

namespace Dino.LocalizationKeyGenerator {
    public class AutoKeyAttribute : PropertyAttribute {
        public readonly string Format;
        public readonly bool IsDefaultTabAuto;
        
        public AutoKeyAttribute(string format, bool isDefaultTabAuto = true) {
            Format = format;
            IsDefaultTabAuto = isDefaultTabAuto;
        }
    }
}