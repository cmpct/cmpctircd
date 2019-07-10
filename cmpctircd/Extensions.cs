using System;
using System.Collections.Generic;

namespace cmpctircd {
    public static class Extensions {
        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, V value) {
            V current = dictionary[key];
            if(current == null)
                return dictionary[key] = value;
            else
                return current;
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
            foreach(T item in enumerable) action(item);
        }
    }
}
