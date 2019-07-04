using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd {
    public static class Extensions {
        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, V value) {
            V current = dictionary[key];
            if(current == null)
                return dictionary[key] = value;
            else
                return current;
        }
    }
}
