using System.Collections.Generic;

namespace Conference.Common {
    /// <summary>
    /// 字典扩展
    /// </summary>
    public static class DictionaryExtensions {
        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
            return dictionary.TryGetValue(key, default(TValue));
        }

        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            TValue defaultValue) {
            TValue result;
            if (!dictionary.TryGetValue(key, out result)) {
                return defaultValue;
            }

            return result;
        }
    }
}