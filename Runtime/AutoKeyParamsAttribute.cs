using System;
using UnityEngine;

namespace Dino.LocalizationKeyGenerator {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class AutoKeyParamsAttribute : PropertyAttribute, IEquatable<AutoKeyParamsAttribute> {
        public readonly string Key;
        public readonly string Value;
        
        public AutoKeyParamsAttribute(string key, string value) {
            Key = key;
            Value = value;
        }

        public bool Equals(AutoKeyParamsAttribute other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Key == other.Key && Value == other.Value;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AutoKeyParamsAttribute)obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = Key != null ? Key.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}