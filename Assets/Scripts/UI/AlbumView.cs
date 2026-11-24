using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 밤의 기록 (컷신 갤러리 + 이번 회차 스토리). 타이틀과 컴퓨터 앨범 앱에서 같이 쓴다.
    /// 글자: progress / 참거짓: has_run
    /// 목록 cats: cat_name, cat_selected / 행동 cat
    /// 목록 scenes: s_title, s_sub, s_unlocked, s_locked, s_new, 스프라이트 s_image / 행동 s_play
    /// 목록 story: st_title, st_state, st_open
    /// </summary>
    public class AlbumView : UISource
    {
        string category = "전체";
        readonly List<string> cats = new List<string>();
        readonly List<SceneCfg> scenes = new List<SceneCfg>();
        readonly List<Dictionary<string, object>> story = new List<Dictionary<string, object>>();
        int builtFrame = -1;

        void Build()
        {
            if (builtFrame == Time.frameCount) return;
            builtFrame = Time.frameCount;
            cats.Clear(); scenes.Clear(); story.Clear();
            cats.Add("전체");
            var cfg = GameConfig.Instance;
            if (cfg != null)
                foreach (var s in cfg.cutscenes.scenes)
                {
                    if (!s.countsForCollection) continue;
                    if (!cats.Contains(s.category)) cats.Add(s.category);
                    if (category == "전체" || category == s.category) scenes.Add(s);
                }
            foreach (var o in Cfg.Items("story")) { var d = o as Dictionary<string, object>; if (d != null) story.Add(d); }
            story.Sort((a, b) => J.I(a, "order", 0).CompareTo(J.I(b, "order", 0)));
        }

        public override int Count(string list)
        {
            Build();
            switch (list)
            {
                case "cats": return cats.Count;
                case "scenes": return scenes.Count;
                case "story": return Game.I.S != null ? story.Count : 0;
            }
            return 0;
        }

        GalleryEntry Entry(SceneCfg s)
        {
            GalleryEntry e;
            return s != null && Game.I.Gallery.Entries.TryGetValue(s.id, out e) ? e : null;
        }

        SceneCfg SceneAt(Ctx c) { Build(); return c.Index >= 0 && c.Index < scenes.Count ? scenes[c.Index] : null; }

        public override string Text(string key, Ctx c)
        {
            Build();
            var g = Game.I;
            switch (key)
            {
                case "progress":
                    {
                        int total = 0, got = 0;
                        var cfg = GameConfig.Instance;
                        if (cfg != null) foreach (var s in cfg.cutscenes.scenes) if (s.countsForCollection) { total++; if (g.Gallery.Entries.ContainsKey(s.id)) got++; }
                        return "수집 " + got + " / " + total;
                    }
                case "cat_name": return c.Index >= 0 && c.Index < cats.Count ? cats[c.Index] : "";
                case "s_title":
                    {
                        var s = SceneAt(c); var e = Entry(s);
                        if (s == null) return "";
                        return e != null || !s.secret ? s.title : "비밀 장면";
                    }
                case "s_sub":
                    {
                        var s = SceneAt(c); var e = Entry(s);
                        if (s == null) return "";
                        if (e != null) return s.conditionText + "\n최초 " + e.FirstDay + "일차 · " + e.FirstUnlockedAt;
                        return g.Settings.ShowExactConditions ? s.conditionText : "힌트: " + s.hint;
                    }
                case "st_title": return c.Index < story.Count ? J.I(story[c.Index], "order", 0) + ". " + J.S(story[c.Index], "title", "") : "";
                case "st_state":
                    {
                        if (g.S == null || c.Index >= story.Count) return "";
                        var m = story[c.Index];
                        string id = J.S(m, "id", "");
                        string choice;
                        g.S.StoryChoices.TryGetValue(id, out choice);
                        if (g.S.StoryUnlocked.Contains(id)) return choice != null ? "선택: " + ChoiceLabel(m, choice) : "열림";
                        return g.StoryRequirementsConfigured(m) ? "잠김" : "조건 미정 (data/story_milestones.json)";
                    }
            }
            return null;
        }

        static string ChoiceLabel(Dictionary<string, object> m, string choiceId)
        {
            foreach (var ch in J.A(m, "choices")) if (J.S(ch, "id", "") == choiceId) return J.S(ch, "label", choiceId);
            return choiceId;
        }

        public override bool Flag(string key, Ctx c)
        {
            Build();
            switch (key)
            {
                case "has_run": return Game.I.S != null;
                case "cat_selected": return c.Index >= 0 && c.Index < cats.Count && cats[c.Index] == category;
                case "s_unlocked": return Entry(SceneAt(c)) != null;
                case "s_locked": return Entry(SceneAt(c)) == null;
                case "s_new": { var e = Entry(SceneAt(c)); return e != null && !e.Seen; }
                case "st_open": return Game.I.S != null && c.Index < story.Count && Game.I.S.StoryUnlocked.Contains(J.S(story[c.Index], "id", ""));
            }
            return false;
        }

        public override Sprite SpriteOf(string key, Ctx c)
        {
            if (key == "s_image") { var s = SceneAt(c); return Entry(s) != null ? s.image : null; }
            return null;
        }

        public override void Act(string action, Ctx c)
        {
            switch (action)
            {
                case "cat": if (c.Index >= 0 && c.Index < cats.Count) category = cats[c.Index]; builtFrame = -1; break;
                case "s_play":
                    {
                        var s = SceneAt(c);
                        if (s == null || Entry(s) == null) return;
                        var root = GameRoot.I;
                        var prev = root.Screen;
                        root.Go(ScreenId.Cutscene);
                        root.cutscene.Play(s, true, () => root.Go(prev));
                        break;
                    }
            }
        }
    }
}
