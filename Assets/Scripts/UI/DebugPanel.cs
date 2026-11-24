using System;

namespace StockGame
{
    /// <summary>F12 디버그. 행동: reload, cash_100k, cash_1m, cond_100, cond_15, skip_job, adv_30, adv_end, settle, save, reset_gallery, toggle_events
    /// 글자: info / 참거짓: has_run, show_events, morning, market_open, not_night / 목록 events: e_line1, e_line2, e_line3</summary>
    public class DebugPanel : UISource
    {
        bool showEvents;
        GameState S { get { return Game.I.S; } }

        EventInstance EventAt(Ctx c)
        {
            if (S == null) return null;
            int i = S.Events.Count - 1 - c.Index;
            return i >= 0 && i < S.Events.Count ? S.Events[i] : null;
        }

        public override string Text(string key, Ctx c)
        {
            var g = Game.I;
            switch (key)
            {
                case "info": return S == null ? "게임을 시작하면 더 많은 항목이 보입니다." : "시드 " + S.Seed + " · 틱 " + S.CurrentTick + "/" + S.SessionEnd + " · 대기 피드 " + S.Pending.Count;
                case "events_label": return showEvents ? "사건 목록 숨기기" : "사건 목록 보기 (스포일러)";
                case "e_line1": { var e = EventAt(c); return e != null ? e.Id + " " + Game.CompanyName(e.CompanyId) + " · " + e.OutcomeId + " (" + e.Tone + ")" : ""; }
                case "e_line2": { var e = EventAt(c); return e != null ? "소문 " + (e.HasRumor ? g.DayOfTick(e.RumorTick) + "일 " + g.ClockOfTick(e.RumorTick) : "없음") + " · 공식 " + g.DayOfTick(e.OfficialTick) + "일 " + g.ClockOfTick(e.OfficialTick) : ""; }
                case "e_line3": { var e = EventAt(c); return e != null ? "효과 " + Fmt.Pct(Math.Exp(e.Impact) - 1) + " · 선반영 " + (e.PricedIn * 100).ToString("0") + "% · 기대감 " + Fmt.Pct(e.Hype) : ""; }
            }
            return null;
        }

        public override bool Flag(string key, Ctx c)
        {
            var g = Game.I;
            switch (key)
            {
                case "has_run": return S != null;
                case "show_events": return showEvents && S != null;
                case "morning": return S != null && S.Phase == "morning";
                case "market_open": return S != null && g.MarketOpenNow();
                case "not_night": return S != null && S.Phase != "night";
            }
            return false;
        }

        public override int Count(string list) { return list == "events" && showEvents && S != null ? S.Events.Count : 0; }

        public override void Act(string action, Ctx c)
        {
            var g = Game.I;
            var root = GameRoot.I;
            if (action == "reload") { root.ReloadAll(); return; }
            if (action == "close") { gameObject.SetActive(false); return; }
            if (S == null) return;
            switch (action)
            {
                case "cash_100k": S.Cash += 100000; break;
                case "cash_1m": S.Cash += 1000000; break;
                case "cond_100": S.Condition = 100; break;
                case "cond_15": S.Condition = 15; break;
                case "skip_job": if (S.Phase == "morning") g.SkipJob(); break;
                case "adv_30": g.AdvanceTicks(30); break;
                case "adv_end": g.AdvanceTicks(100000); break;
                case "settle": if (S.Phase != "night") root.settlement.Open(); break;
                case "save": g.Save(); root.Toast("저장했습니다"); break;
                case "reset_gallery": root.Confirm("갤러리 초기화", "회차와 무관한 컷신 수집 기록을 모두 지웁니다.", "지우기", () => { g.Gallery = new GalleryData(); g.SaveMeta(); }); break;
                case "toggle_events": showEvents = !showEvents; break;
            }
        }
    }
}
