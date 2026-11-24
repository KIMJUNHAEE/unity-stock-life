using System;
using System.Collections.Generic;

namespace StockGame
{
    /// <summary>자산 구간 스토리. 글자: subtitle, title, placeholder / 목록 choices: ch_label / 행동 ch</summary>
    public class StoryScreen : UISource
    {
        string milestoneId;
        Action onDone;

        public void Open(string id, Action done) { milestoneId = id; onDone = done; }

        Dictionary<string, object> M { get { return Cfg.ItemById("story", milestoneId); } }

        public override string Text(string key, Ctx c)
        {
            var m = M;
            switch (key)
            {
                case "subtitle": return "새 이야기 구간";
                case "title": return m != null ? J.S(m, "title", milestoneId) : "";
                case "placeholder": var cfg = GameConfig.Instance; return cfg != null ? cfg.dialogue.system.storyPlaceholder : "";
                case "ch_label": { var ch = J.A(m, "choices"); return c.Index < ch.Count ? J.S(ch[c.Index], "label", "") : ""; }
            }
            return null;
        }

        public override int Count(string list) { return list == "choices" ? J.A(M, "choices").Count : 0; }

        public override void Act(string action, Ctx c)
        {
            if (action != "ch") return;
            var ch = J.A(M, "choices");
            if (c.Index >= ch.Count) return;
            Game.I.ChooseStory(milestoneId, J.S(ch[c.Index], "id", ""));
            var cfg = GameConfig.Instance;
            var sc = cfg != null ? cfg.Scene(J.S(ch[c.Index], "scene_id", null)) : null;
            var cb = onDone;
            onDone = null;
            var root = GameRoot.I;
            if (sc != null)
            {
                root.Go(ScreenId.Cutscene);
                root.cutscene.Play(sc, false, () => { if (cb != null) cb(); else root.Go(ScreenId.Home); });
                return;
            }
            if (cb != null) cb(); else root.Go(ScreenId.Home);
        }
    }
}
