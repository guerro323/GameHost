using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenToolkit.Windowing.Common.Input;

namespace GameHost.Input
{
    public static class InputManagerNaming
    {
        private static Dictionary<Key, string> _keyToId;
        
        static InputManagerNaming()
        {
            _keyToId = new Dictionary<Key, string>(131);
            foreach (var key in Enum.GetValues(typeof(Key)).Cast<Key>())
            {
                _keyToId[key] = string.Format("keyboard/{0}", CamelCase(key.ToString()));
            }
        }
        
        static string CamelCase(string s)
        {
            var x = s.Replace("_", "");
            if (x.Length == 0) return "Null";
            x = Regex.Replace(x, "([A-Z])([A-Z]+)($|[A-Z])",
                m => m.Groups[1].Value + m.Groups[2].Value.ToLower() + m.Groups[3].Value);
            return char.ToUpper(x[0]) + x.Substring(1);
        }

        public static string GetKeyId(Key key)
        {
            return _keyToId[key];
        }
    }
}
