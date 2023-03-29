using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DFunc.Exts {
    public static class Ext {
        public static string PrettyToString(this IDictionary dictionary) {
            string result = "{";
            foreach (var Key in dictionary.Keys) {
                result += string.Format("({0}, {1}) ", PrettyToString(Key), PrettyToString(dictionary[Key]));
            }
            result += "}";
            return result;
        }

        public static string PrettyToString(this IEnumerable list) {
            string result = "[";
            foreach (var element in list) {
                result += string.Format("{0},", PrettyToString(element));
            }
            result = result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static string PrettyToString(this object O) {
            if (O is string S) return S;
            if (O is IDictionary D) return D.PrettyToString();
            if (O is IEnumerable L) return L.PrettyToString();
            return O.ToString();
        }

        public static bool EqualLists(this List<object> list, List<object> other) {
            if (list.Count != other.Count) return false;

            for (int i = 0; i < list.Count; i++) {
                var li = list[i];
                var oi = other[i];

                if (li.GetType() != oi.GetType()) return false;

                if (li is List<object>) {
                    if (!EqualLists((List<object>)li, (List<object>)oi)) {
                        return false;
                    }
                }

                if (!li.Equals(oi)) {
                    return false;
                }
            }

            return true;
        }
    }
}
