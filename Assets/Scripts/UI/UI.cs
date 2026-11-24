using UnityEngine;

namespace StockGame
{
    /// <summary>UI 공용 도우미: 테마 색 조회.</summary>
    public static class Palette
    {
        public static string ActiveThemeKey()
        {
            var cfg = GameConfig.Instance;
            string k = Game.I != null && Game.I.S != null ? Game.I.S.ActiveTheme : null;
            if (cfg == null) return k;
            if (string.IsNullOrEmpty(k) || cfg.Theme(k) == null) k = cfg.theme.defaultTheme;
            return k;
        }

        /// <summary>현재 테마의 색. 해금 테마에 없는 색은 기본 테마 값을 쓴다.</summary>
        public static bool TryGet(string key, out Color c)
        {
            c = Color.magenta;
            var cfg = GameConfig.Instance;
            if (cfg == null || string.IsNullOrEmpty(key)) return false;
            var t = cfg.Theme(ActiveThemeKey());
            if (t != null) foreach (var nc in t.colors) if (nc.key == key) { c = nc.value; return true; }
            var d = cfg.Theme(cfg.theme.defaultTheme);
            if (d != null) foreach (var nc in d.colors) if (nc.key == key) { c = nc.value; return true; }
            return false;
        }

        public static Color Get(string key)
        {
            Color c;
            TryGet(key, out c);
            return c;
        }

        public static string UpDown(double diff) { return diff > 0 ? "up" : (diff < 0 ? "down" : "text_dim"); }
    }
}
