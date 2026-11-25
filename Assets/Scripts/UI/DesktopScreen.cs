using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 가상 OS 바탕화면. 앱 창은 Window/Content 아래 App_* 오브젝트 (한 번에 하나 전면).
    /// 글자: topleft, condition_chip, market_status, pause_label, window_title, appname:&lt;앱&gt;, bubble_text,
    ///       cp_value, cp_now, cp_next, cp_cash, tut_text, tut_step, tut_next_label, guide_title, guide_list
    /// 참거짓: window_open, can_pause, bubble, ov_condition, ov_menu, ov_tutorial, can_end_day, guide_visible
    /// 값: cp_frac / 색: status_dot / 스프라이트: portrait, window_icon, icon:&lt;앱&gt;, tut_portrait
    /// 목록 running: run_name, run_active / 행동 run · 목록 items: it_name, it_desc, it_can / 행동 it_use
    /// 행동: open:&lt;앱&gt;, minimize, close, menu, settings, home, condition, pause, cp_close, end_day, go_home, menu_close, tut_next, tut_skip, guide_hide
    /// </summary>
    public class DesktopScreen : UISource
    {
        [Header("앱 창 (자동 연결)")]
        public GameObject appStock;
        public GameObject appBrowser;
        public GameObject appFinance;
        public GameObject appAlbum;
        public GameObject appSettings;
        public StockApp stock;
        [Tooltip("피곤할 때 가장자리 흐림")] public CanvasGroup vignette;

        public readonly List<string> Running = new List<string>();
        public string Active;
        public bool Minimized;
        public string Overlay;   // condition / menu / tutorial
        public float Speed = 1;

        int tutorialStep;
        string tutorialKey;
        int conditionPromptDay = -1, sessionEndPromptDay = -1, extensionPromptDay = -1;
        readonly List<string> itemKeys = new List<string>();

        public bool StockInFront { get { return Active == "stock" && !Minimized && Running.Contains("stock"); } }

        static string AppName(string id)
        {
            var a = GameConfig.Instance != null ? GameConfig.Instance.App(id) : null;
            return a != null ? a.name : id;
        }

        public void OnEnter()
        {
            Overlay = null;
            var s = Game.I.S;
            if (s != null && !s.HasFlag("guide_computer")) s.SetFlag("guide_computer");
        }

        public void Open(string app)
        {
            if (!Running.Contains(app)) Running.Add(app);
            Active = app;
            Minimized = false;
            if (app == "stock" || app == "browser") MaybeTutorial(app);
        }

        void Close(string app)
        {
            Running.Remove(app);
            if (Active == app) { Active = Running.Count > 0 ? Running[Running.Count - 1] : null; Minimized = Active == null; }
        }

        List<LineCfg> Tutorial(string key)
        {
            var cfg = GameConfig.Instance;
            if (cfg != null) foreach (var t in cfg.dialogue.tutorial) if (t.key == key) return t.lines;
            return null;
        }

        void MaybeTutorial(string key)
        {
            var s = Game.I.S;
            if (s == null || s.HasFlag("tutorial_" + key)) return;
            var lines = Tutorial(key);
            if (lines == null || lines.Count == 0) return;
            tutorialKey = key;
            tutorialStep = 0;
            Overlay = "tutorial";
        }

        void Update()
        {
            var g = Game.I;
            var s = g.S;
            if (s == null) { GameRoot.I.Go(ScreenId.Title); return; }
            var root = GameRoot.I;

            SetApp(appStock, "stock");
            SetApp(appBrowser, "browser");
            SetApp(appFinance, "finance");
            SetApp(appAlbum, "album");
            SetApp(appSettings, "settings");
            if (vignette != null) vignette.alpha = s.Phase == "evening" ? (float)J.D(g.Stage(), "vignette", 0) : 0;

            itemKeys.Clear();
            foreach (var kv in Cfg.O("balance", "condition.items")) itemKeys.Add(kv.Key);

            if (s.Phase == "evening" && !s.Today.Settled && s.Condition <= 0 && conditionPromptDay != s.Day && !root.HasModal)
            {
                conditionPromptDay = s.Day;
                g.UserPaused = true;
                root.ShowModal("컨디션 소진", "더 이상 집중할 수 없어요. 시장은 멈춰 있습니다.\n회복 수단을 쓰고 이어가거나, 오늘 거래를 마칠 수 있어요.",
                    new ModalButton("회복하기", false, () => { Overlay = "condition"; conditionPromptDay = -1; }),
                    new ModalButton("오늘 거래 종료", true, () => root.settlement.Open()));
            }
            if (g.ExtensionOfferDue() && extensionPromptDay != s.Day && !root.HasModal && Overlay == null)
            {
                extensionPromptDay = s.Day;
                bool wasPaused = g.UserPaused;
                int ext = s.ExtensionTicks;
                string endClock = Fmt.Clock(s.SessionStartMinute + s.TicksPerDay + ext);
                double drain = Cfg.D("balance", "condition.extension_drain_per_tick", 0.45);
                root.ShowModal("연장 거래",
                    "곧 오늘 장이 마감돼요 (" + (s.SessionStartMinute + s.TicksPerDay == 1440 ? "24:00" : Fmt.Clock(s.SessionStartMinute + s.TicksPerDay)) + ").\n" +
                    "원하면 " + endClock + "까지 " + ext + "분 더 거래할 수 있어요. 연장 구간은 컨디션이 분당 " + drain.ToString("0.##") + "씩 줄어요.\n" +
                    "연장하지 않아도 보유 주식은 다음 날로 넘어가고, 연장 중에도 언제든 결산할 수 있어요.",
                    new ModalButton("기본 마감대로", false, () => { g.DeclineExtension(); g.UserPaused = wasPaused; }),
                    new ModalButton("연장 신청", true, () => { g.AcceptExtension(); g.UserPaused = wasPaused; root.Toast(endClock + "까지 연장 거래"); }));
            }
            if (s.Phase == "evening" && !s.Today.Settled && s.CurrentTick >= s.SessionEnd && sessionEndPromptDay != s.Day && !root.HasModal)
            {
                sessionEndPromptDay = s.Day;
                g.React("session_end");
                root.ShowModal(s.Today.Extended ? "연장 거래 마감" : "장 마감", "오늘의 거래 세션이 끝났습니다. 보유 주식은 다음 날로 넘어갑니다.",
                    new ModalButton("조금 더 둘러보기", false, null),
                    new ModalButton("결산하기", true, () => root.settlement.Open()));
            }
            if (Overlay == "tutorial")
            {
                var lines = Tutorial(tutorialKey);
                if (lines == null || tutorialStep >= lines.Count)
                {
                    s.SetFlag("tutorial_" + tutorialKey);
                    Overlay = null;
                    // 처음 보는 거래 안내가 끝나면 바로 시장을 돌리지 않고, 플레이어가 직접 재개하게 한다.
                    if (tutorialKey == "stock" && g.MarketOpenNow())
                    {
                        g.UserPaused = true;
                        root.Toast("준비되면 위쪽 ▶ 재개 (또는 Space)로 시장을 움직이세요.");
                    }
                }
            }
        }

        void SetApp(GameObject go, string id)
        {
            if (go == null) return;
            bool on = Active == id && !Minimized && Running.Contains(id);
            if (go.activeSelf != on) go.SetActive(on);
        }

        // ───────────────────────── 바인딩 ─────────────────────────

        public override string Text(string key, Ctx c)
        {
            var g = Game.I;
            var s = g.S;
            if (s == null) return null;
            if (key.StartsWith("appname:")) return AppName(key.Substring(8));
            switch (key)
            {
                case "topleft": return s.Day + "일차 · " + (s.Phase == "morning" ? "아침" : (s.Phase == "evening" ? "밤" : "밤 (결산 완료)")) + "   |   " + g.NowClock();
                case "condition_chip": return "컨디션 " + Mathf.RoundToInt((float)s.Condition) + " · " + g.StageName();
                case "market_status":
                    if (s.Phase == "morning") return "장 시작 전";
                    if (s.Today.Settled || s.Phase == "night") return "오늘 거래 종료";
                    if (s.CurrentTick >= s.SessionEnd) return s.Today.Extended ? "연장 거래 마감" : "장 마감";
                    if (s.InExtension && GameRoot.I.MarketShouldRun()) return "연장 거래 중";
                    if (GameRoot.I.MarketShouldRun()) return "거래 진행 중";
                    if (g.UserPaused) return "일시정지 (Space)";
                    return "시장 정지 (증권 앱 밖)";
                case "pause_label": return g.UserPaused ? "▶ 재개" : "❚❚ 정지";
                case "window_title": return Active != null ? AppName(Active) : "";
                case "bubble_text": return GameRoot.I.BubbleText ?? "";
                case "run_name": return c.Index < Running.Count ? AppName(Running[c.Index]) : "";
                case "cp_value": return Mathf.RoundToInt((float)s.Condition) + " / " + Cfg.I("balance", "condition.max_value", 100) + " · " + g.StageName();
                case "cp_now": return "지금: " + J.S(g.Stage(), "effect_text", "");
                case "cp_next":
                    {
                        var stages = g.ConditionStages();
                        int idx = g.StageIndex();
                        if (idx + 1 >= stages.Count) return "";
                        var next = stages[idx + 1];
                        return "다음 단계(" + J.S(next, "name", "") + ", " + J.I(next, "min", 0) + " 미만): " + J.S(next, "effect_text", "");
                    }
                case "cp_cash": return "회복 (현금 " + Fmt.Won(s.Cash) + ")";
                case "it_name": { var it = ItemAt(c); return it != null ? J.S(it, "name", "") + " · " + Fmt.Won(J.L(it, "price", 0)) : ""; }
                case "it_desc":
                    {
                        var it = ItemAt(c);
                        if (it == null) return "";
                        return J.S(it, "description", "") + "  (오늘 " + g.ItemUsesToday(itemKeys[c.Index]) + "/" + J.I(it, "limit_per_night", 99) + ")";
                    }
                case "tut_text": { var l = TutLine(); return l != null ? l.text : ""; }
                case "tut_step": { var lines = Tutorial(tutorialKey); return lines != null ? "안내 " + (tutorialStep + 1) + " / " + lines.Count + "  (시장 정지 중)" : ""; }
                case "tut_next_label": { var lines = Tutorial(tutorialKey); return lines != null && tutorialStep + 1 >= lines.Count ? "시작하기" : "다음"; }
                case "guide_title": return "오늘 할 일  " + g.GuideDoneCount() + "/" + g.GuideSteps().Count;
                case "guide_list":
                    {
                        var sb = new System.Text.StringBuilder();
                        bool current = false;
                        foreach (var st in g.GuideSteps())
                        {
                            if (sb.Length > 0) sb.Append('\n');
                            if (st.Done) sb.Append("<color=#9AA3B2>●  " + st.Label + "</color>");
                            else if (!current) { current = true; sb.Append("<b>▶  " + st.Label + "</b>"); }
                            else sb.Append("○  " + st.Label);
                        }
                        return sb.ToString();
                    }
            }
            return null;
        }

        LineCfg TutLine()
        {
            var lines = Tutorial(tutorialKey);
            return lines != null && tutorialStep < lines.Count ? lines[tutorialStep] : null;
        }

        Dictionary<string, object> ItemAt(Ctx c)
        {
            if (c.Index < 0 || c.Index >= itemKeys.Count) return null;
            return Game.I.RecoveryItem(itemKeys[c.Index]);
        }

        public override bool Flag(string key, Ctx c)
        {
            var g = Game.I;
            var s = g.S;
            if (s == null) return false;
            switch (key)
            {
                case "window_open": return !string.IsNullOrEmpty(Active) && !Minimized && Running.Contains(Active);
                case "can_pause": return g.MarketOpenNow();
                case "bubble": return GameRoot.I.BubbleVisible;
                case "ov_condition": return Overlay == "condition";
                case "ov_menu": return Overlay == "menu";
                case "ov_tutorial": return Overlay == "tutorial";
                case "can_end_day": return s.Phase != "night" && !s.Today.Settled;
                case "guide_visible": return g.GuideVisible;
                case "run_active": return c.Index < Running.Count && Running[c.Index] == Active && !Minimized;
                case "it_can":
                    {
                        var it = ItemAt(c);
                        return it != null && s.Phase != "night" && g.ItemUsesToday(itemKeys[c.Index]) < J.I(it, "limit_per_night", 99) && s.Cash >= J.L(it, "price", 0);
                    }
            }
            return false;
        }

        public override float Value(string key, Ctx c)
        {
            if (key == "cp_frac" && Game.I.S != null) return (float)(Game.I.S.Condition / Cfg.D("balance", "condition.max_value", 100));
            return 0;
        }

        public override string ColorKey(string key, Ctx c)
        {
            if (key != "status_dot") return null;
            var s = Game.I.S;
            if (s == null || s.Phase != "evening" || s.Today.Settled || s.CurrentTick >= s.SessionEnd) return "text_dim";
            return GameRoot.I.MarketShouldRun() ? "mint_text" : "warning";
        }

        public override Sprite SpriteOf(string key, Ctx c)
        {
            var cfg = GameConfig.Instance;
            if (cfg == null) return null;
            if (key.StartsWith("icon:")) { var a = cfg.App(key.Substring(5)); return a != null ? a.icon : null; }
            switch (key)
            {
                case "portrait": return cfg.PortraitOf(GameRoot.I.CurrentExpression());
                case "window_icon": { var a = Active != null ? cfg.App(Active) : null; return a != null ? a.icon : null; }
                case "tut_portrait": { var l = TutLine(); return cfg.PortraitOf(l != null ? l.expression : "normal"); }
                case "wallpaper": { var t = cfg.Theme(Palette.ActiveThemeKey()); return t != null ? t.wallpaper : null; }
            }
            return null;
        }

        public override int Count(string list)
        {
            if (list == "running") return Running.Count;
            if (list == "items") return itemKeys.Count;
            return 0;
        }

        public override void Act(string action, Ctx c)
        {
            var g = Game.I;
            var root = GameRoot.I;
            if (action.StartsWith("open:")) { Open(action.Substring(5)); return; }
            switch (action)
            {
                case "minimize": Minimized = true; break;
                case "close": if (Active != null) Close(Active); break;
                case "menu": Overlay = Overlay == "menu" ? null : "menu"; break;
                case "menu_close": Overlay = null; break;
                case "settings": Open("settings"); break;
                case "home": root.Go(ScreenId.Home); break;
                case "condition": Overlay = "condition"; break;
                case "cp_close": Overlay = null; break;
                case "pause": if (g.MarketOpenNow()) g.UserPaused = !g.UserPaused; break;
                case "run":
                    if (c.Index < Running.Count)
                    {
                        string id = Running[c.Index];
                        if (id == Active && !Minimized) Minimized = true; else { Active = id; Minimized = false; }
                    }
                    break;
                case "it_use":
                    if (c.Index < itemKeys.Count)
                    {
                        string err = g.UseRecoveryItem(itemKeys[c.Index]);
                        if (err != null) root.Toast(err);
                    }
                    break;
                case "end_day":
                    Overlay = null;
                    root.Confirm("오늘 거래 종료", "보유 주식과 부채를 확인하고 결산·상환·컷신으로 넘어갑니다. 창을 닫는 것과는 다릅니다.", "결산하기", () => root.settlement.Open());
                    break;
                case "go_home": Overlay = null; root.Go(ScreenId.Home); break;
                case "tut_next": tutorialStep++; break;
                case "guide_hide": g.HideGuide(); break;
                case "tut_skip": { var lines = Tutorial(tutorialKey); tutorialStep = lines != null ? lines.Count : 0; break; }
            }
        }
    }
}
