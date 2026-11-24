using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// Assets/Art/Fonts 아래 폰트들을 빌드에 포함시키기 위한 목록.
    /// Assets/Resources/FontLibrary.asset 은 에디터 스크립트(FontLibraryBuilder)가 자동 생성·갱신한다.
    /// 직접 만들거나 연결할 필요는 없다.
    /// </summary>
    public class FontLibrary : ScriptableObject
    {
        public List<Font> fonts = new List<Font>();

        public Font Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var f in fonts) if (f != null && f.name == name) return f;
            return null;
        }
    }

    public enum FontWeight { Light, Regular, Bold }

    /// <summary>theme.json 의 fonts 설정에 따라 폰트를 고른다. 없으면 OS 한글 폰트로 대체.</summary>
    public static class GameFonts
    {
        static readonly Dictionary<FontWeight, Font> cache = new Dictionary<FontWeight, Font>();
        static FontLibrary library;
        static bool libraryTried;
        public static readonly List<string> Report = new List<string>();

        public static void Clear() { cache.Clear(); Report.Clear(); }

        public static Font Get(FontWeight w)
        {
            Font f;
            if (cache.TryGetValue(w, out f) && f != null) return f;
            f = Load(w);
            cache[w] = f;
            return f;
        }

        static Font Load(FontWeight w)
        {
            string key = w == FontWeight.Light ? "light" : (w == FontWeight.Bold ? "bold" : "regular");
            string libName = Cfg.S("theme", "fonts." + key + ".library_name", null);
            Font f = null;

            if (!libraryTried)
            {
                libraryTried = true;
                library = Resources.Load<FontLibrary>("FontLibrary");
            }
            if (library != null) f = library.Find(libName);

#if UNITY_EDITOR
            if (f == null)
            {
                string editorPath = Cfg.S("theme", "fonts." + key + ".editor_path", null);
                if (!string.IsNullOrEmpty(editorPath)) f = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(editorPath);
            }
#endif
            if (f != null) { Report.Add(key + ": " + f.name); return f; }

            var fallback = Cfg.A("theme", "fonts." + key + ".os_fallback");
            var names = new List<string>();
            foreach (var o in fallback) names.Add(J.ToS(o, ""));
            if (names.Count == 0) { names.Add("Malgun Gothic"); names.Add("Apple SD Gothic Neo"); names.Add("Noto Sans CJK KR"); }
            try
            {
                f = Font.CreateDynamicFontFromOSFont(names.ToArray(), 24);
                Report.Add(key + ": OS 폰트 대체 (" + string.Join(", ", names.ToArray()) + ")");
            }
            catch (System.Exception)
            {
                f = null;
                Report.Add(key + ": 폰트 없음 (기본 폰트)");
            }
            return f;
        }
    }
}
