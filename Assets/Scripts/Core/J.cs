using System;
using System.Collections.Generic;
using System.Globalization;

namespace StockGame
{
    /// <summary>MiniJson 결과(Dictionary/List/double)를 안전하게 읽는 도우미.</summary>
    public static class J
    {
        static readonly Dictionary<string, string[]> pathCache = new Dictionary<string, string[]>();

        public static Dictionary<string, object> AsObj(object o) { return o as Dictionary<string, object>; }
        public static List<object> AsList(object o) { return o as List<object>; }

        public static object Get(object root, string key)
        {
            var d = root as Dictionary<string, object>;
            if (d == null || key == null) return null;
            object v;
            return d.TryGetValue(key, out v) ? v : null;
        }

        public static bool Has(object root, string key)
        {
            var d = root as Dictionary<string, object>;
            return d != null && d.ContainsKey(key);
        }

        /// <summary>"market.session.length" 같은 점 경로로 값을 찾는다.</summary>
        public static object Path(object root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            string[] parts;
            if (!pathCache.TryGetValue(path, out parts))
            {
                parts = path.Split('.');
                pathCache[path] = parts;
            }
            object cur = root;
            for (int i = 0; i < parts.Length; i++)
            {
                var d = cur as Dictionary<string, object>;
                if (d == null) return null;
                if (!d.TryGetValue(parts[i], out cur)) return null;
            }
            return cur;
        }

        public static bool IsNumber(object v) { return v is double || v is int || v is long || v is float; }

        public static double ToD(object v, double def)
        {
            if (v == null) return def;
            if (v is double) return (double)v;
            if (v is int) return (int)v;
            if (v is long) return (long)v;
            if (v is float) return (float)v;
            if (v is bool) return (bool)v ? 1 : 0;
            var s = v as string;
            double d;
            if (s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
            return def;
        }

        public static string ToS(object v, string def)
        {
            if (v == null) return def;
            var s = v as string;
            if (s != null) return s;
            if (v is double) return ((double)v).ToString(CultureInfo.InvariantCulture);
            return v.ToString();
        }

        public static bool ToB(object v, bool def)
        {
            if (v == null) return def;
            if (v is bool) return (bool)v;
            if (IsNumber(v)) return ToD(v, 0) != 0;
            var s = v as string;
            if (s != null) return s == "true" || s == "1" || s == "yes";
            return def;
        }

        public static double D(object root, string path, double def) { return ToD(Path(root, path), def); }
        public static int I(object root, string path, int def) { return (int)Math.Round(ToD(Path(root, path), def)); }
        public static long L(object root, string path, long def) { return (long)Math.Round(ToD(Path(root, path), def)); }
        public static string S(object root, string path, string def) { return ToS(Path(root, path), def); }
        public static bool B(object root, string path, bool def) { return ToB(Path(root, path), def); }
        public static List<object> A(object root, string path) { return Path(root, path) as List<object> ?? new List<object>(); }
        public static Dictionary<string, object> O(object root, string path) { return Path(root, path) as Dictionary<string, object> ?? new Dictionary<string, object>(); }

        /// <summary>[min, max] 배열을 읽는다. 숫자 하나면 min=max.</summary>
        public static void Range(object root, string path, double defMin, double defMax, out double min, out double max)
        {
            object v = Path(root, path);
            var l = v as List<object>;
            if (l != null && l.Count >= 2) { min = ToD(l[0], defMin); max = ToD(l[1], defMax); }
            else if (IsNumber(v)) { min = max = ToD(v, defMin); }
            else { min = defMin; max = defMax; }
            if (max < min) { double t = min; min = max; max = t; }
        }

        public static List<string> StrList(object root, string path)
        {
            var res = new List<string>();
            foreach (var o in A(root, path)) if (o != null) res.Add(ToS(o, ""));
            return res;
        }

        /// <summary>items 배열을 꺼낸다. 콘텐츠 JSON 공통 구조.</summary>
        public static List<object> Items(object root) { return A(root, "items"); }
    }

    /// <summary>
    /// 설정값 읽기의 단일 창구. 매번 현재 로드된 JSON을 읽으므로 파일을 고치면 즉시 반영된다.
    /// file 은 ConfigStore 에 등록된 이름(balance, job, theme ...)이다.
    /// </summary>
    public static class Cfg
    {
        public static ConfigStore Store;

        public static int Version { get { return Store == null ? 0 : Store.Version; } }

        public static object Root(string file) { return Store == null ? null : Store.Get(file); }
        public static object Get(string file, string path) { return J.Path(Root(file), path); }
        public static double D(string file, string path, double def) { return J.D(Root(file), path, def); }
        public static float F(string file, string path, float def) { return (float)J.D(Root(file), path, def); }
        public static int I(string file, string path, int def) { return J.I(Root(file), path, def); }
        public static long L(string file, string path, long def) { return J.L(Root(file), path, def); }
        public static bool B(string file, string path, bool def) { return J.B(Root(file), path, def); }
        public static string S(string file, string path, string def) { return J.S(Root(file), path, def); }
        public static List<object> A(string file, string path) { return J.A(Root(file), path); }
        public static Dictionary<string, object> O(string file, string path) { return J.O(Root(file), path); }
        public static void Range(string file, string path, double dMin, double dMax, out double min, out double max)
        {
            J.Range(Root(file), path, dMin, dMax, out min, out max);
        }
        public static List<object> Items(string file) { return J.Items(Root(file)); }

        /// <summary>items 배열에서 id 로 항목 찾기.</summary>
        public static Dictionary<string, object> ItemById(string file, string id)
        {
            foreach (var o in Items(file))
            {
                var d = o as Dictionary<string, object>;
                if (d != null && J.S(d, "id", null) == id) return d;
            }
            return null;
        }
    }
}
