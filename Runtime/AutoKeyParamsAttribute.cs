using System;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class AutoKeyParamsAttribute : PropertyAttribute {
        public readonly string Key;
        public readonly string Value;
        
        public AutoKeyParamsAttribute(string key, string value) {
            Key = key;
            Value = value;
        }
    }
}