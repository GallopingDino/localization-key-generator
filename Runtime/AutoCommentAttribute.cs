using UnityEngine;

namespace Dino.LocalizationKeyGenerator {
    public class AutoCommentAttribute : PropertyAttribute {
        public readonly string Format;
        
        public AutoCommentAttribute(string format) {
            Format = format;
        }
    }
}