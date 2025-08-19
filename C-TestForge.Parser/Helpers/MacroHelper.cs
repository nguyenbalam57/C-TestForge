using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Parser.Helpers
{
    public static class MacroHelper
    {
        /// <summary>
        /// Chuyển ObservableCollection<string> sang Dictionary<string, string>.
        /// Nếu có nhiều macro cùng key, chỉ lấy giá trị cuối cùng.
        /// </summary>
        public static Dictionary<string, string> ToDictionary(List<string>? macros)
        {
            var dict = new Dictionary<string, string>();
            if (macros == null) return dict;

            foreach (var macro in macros)
            {
                if (string.IsNullOrWhiteSpace(macro)) continue;
                var parts = macro.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                if (!string.IsNullOrEmpty(key))
                {
                    dict[key] = value; // Nếu trùng key, giá trị sau sẽ ghi đè
                }
            }
            return dict;
        }

        /// <summary>
        /// Chuyển Dictionary<string, string> sang ObservableCollection<string>.
        /// Nếu value rỗng, chỉ lấy key.
        /// </summary>
        public static ObservableCollection<string> ToObservableCollection(Dictionary<string, string>? dict)
        {
            var macros = new ObservableCollection<string>();
            if (dict == null) return macros;

            foreach (var kvp in dict)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
                if (!string.IsNullOrEmpty(kvp.Value))
                    macros.Add($"{kvp.Key}={kvp.Value}");
                else
                    macros.Add(kvp.Key);
            }
            return macros;
        }
    }
}
