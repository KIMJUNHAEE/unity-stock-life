using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 설정·테마. 참거짓/행동 opt:reactions, opt:skip_seen, opt:skip_default, opt:exact, opt:markers, opt:confirm (토글)
    /// 목록 speeds: ts_label, ts_selected / 행동 ts
    /// 목록 themes: th_name, th_desc, th_unlocked, th_active, th_can_apply, 색 th_top, th_bottom / 행동 th_apply
    /// </summary>
    public class SettingsApp : UISource
    {
        static readonly float[] TextSpeeds = { 0.5f, 1f, 1.5f, 2f, 4f };

        ThemeCfg ThemeAt(Ctx c)
        {
            var cfg = GameConfig.Instance;
            return cfg != null && c.Index >= 0 && c.Index < cfg.theme.themes.Count ? cfg.theme.themes[c.Index] : null;
        }

        public override bool Flag(string key, Ctx c)
        {
            var st = Game.I.Settings;
            var s = Game.I.S;
            switch (key)
            {
                case "opt:reactions": return st.Reactions;
                case "opt:skip_seen": return st.AutoSkipSeenCutscenes;
                case "opt:skip_default": return st.SkipDefaultBedtime;
                case "opt:exact": return st.ShowExactConditions;
                case "opt:markers": return st.ShowNewsMarkers;
                case "opt:confirm": return st.OrderConfirm;
                case "ts_selected": return c.Index < TextSpeeds.Length && Mathf.Approximately(st.TextSpeed, TextSpeeds[c.Index]);
                case "th_unlocked": { var t = ThemeAt(c); return t != null && s != null && s.UnlockedThemes.Contains(t.key); }
                case "th_locked": return !Flag("th_unlocked", c);
                case "th_active": { var t = ThemeAt(c); return t != null && Palette.ActiveThemeKey() == t.key; }
                case "th_can_apply": return Flag("th_unlocked", c) && !Flag("th_active", c);
            }
            return false;
        }

        public override string Text(string key, Ctx c)
        {
            switch (key)
            {
                case "ts_label": return c.Index < TextSpeeds.Length ? "×" + TextSpeeds[c.Index] : "";
                case "th_name": { var t = ThemeAt(c); return t != null ? t.name : ""; }
                case "th_desc":
                    {
                        var t = ThemeAt(c);
                        if (t == null) return "";
                        return Flag("th_unlocked", c) ? t.description : "잠김 · " + (string.IsNullOrEmpty(t.unlockHint) ? "조건 달성 시 해금" : t.unlockHint);
                    }
                case "th_button": return Flag("th_active", c) ? "사용 중" : "적용";
            }
            return null;
        }

        public override string ColorKey(string key, Ctx c)
        {
            var t = ThemeAt(c);
            if (t == null) return null;
            string want = key == "th_top" ? "desktop_top" : (key == "th_bottom" ? "desktop_bottom" : null);
            if (want == null) return null;
            foreach (var nc in t.colors) if (nc.key == want) return "#" + ColorUtility.ToHtmlStringRGBA(nc.value);
            return null;
        }

        public override int Count(string list)
        {
            if (list == "speeds") return TextSpeeds.Length;
            if (list == "themes") return GameConfig.Instance != null ? GameConfig.Instance.theme.themes.Count : 0;
            return 0;
        }

        public override void Act(string action, Ctx c)
        {
            var g = Game.I;
            var st = g.Settings;
            switch (action)
            {
                case "opt:reactions": st.Reactions = !st.Reactions; break;
                case "opt:skip_seen": st.AutoSkipSeenCutscenes = !st.AutoSkipSeenCutscenes; break;
                case "opt:skip_default": st.SkipDefaultBedtime = !st.SkipDefaultBedtime; break;
                case "opt:exact": st.ShowExactConditions = !st.ShowExactConditions; break;
                case "opt:markers": st.ShowNewsMarkers = !st.ShowNewsMarkers; break;
                case "opt:confirm": st.OrderConfirm = !st.OrderConfirm; break;
                case "ts": if (c.Index < TextSpeeds.Length) st.TextSpeed = TextSpeeds[c.Index]; break;
                case "th_apply":
                    {
                        var t = ThemeAt(c);
                        if (t != null && g.S != null && g.S.UnlockedThemes.Contains(t.key)) { g.S.ActiveTheme = t.key; g.Save(); }
                        return;
                    }
                default: return;
            }
            g.SaveMeta();
        }
    }
}
