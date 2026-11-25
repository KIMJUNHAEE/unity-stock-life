using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 밤 결산. 글자: title, subtitle, repaid_today, footnote / 참거짓: has_loan, has_repaid
    /// 목록 left, right: r_label, r_value, 색 r_color
    /// 목록 loan_repay: label / 행동 loan_repay · 목록 debt_repay: label / 행동 debt_repay
    /// 행동: loan_all, confirm, back
    /// 복기: 참거짓 review_open · 글자 rv_title, rv_summary, rv_note, rv_empty · 행동 review_open, review_close
    ///       목록 rv_tabs: rv_tab_label, 참거짓 rv_tab_selected / 행동 rv_tab · 목록 rv_events: rv_time, rv_text, 색 rv_color
    /// </summary>
    public class SettlementScreen : UISource
    {
        struct Row { public string L, V, C; public Row(string l, string v, string c) { L = l; V = v; C = c; } }
        readonly List<Row> left = new List<Row>();
        readonly List<Row> right = new List<Row>();
        readonly List<ReviewEntry> reviewEntries = new List<ReviewEntry>();
        bool reviewOpen;
        GameState S { get { return Game.I.S; } }

        public void Open()
        {
            reviewOpen = false;
            Game.I.PrepareSettlement();
            var root = GameRoot.I;
            root.CloseAllModals();
            if (root.desktop != null) root.desktop.Overlay = null;
            root.Go(ScreenId.Settlement);
        }

        void Update()
        {
            left.Clear(); right.Clear();
            if (S == null) return;
            var g = Game.I;
            var t = S.Today;
            left.Add(new Row("알바 수입", t.JobDone ? Fmt.Won(t.JobPay) : (t.JobSkipped ? "쉼" : "-"), "text"));
            if (t.JobGarnish > 0) left.Add(new Row("  └ 연체 대출 자동 상환", "−" + Fmt.Won(t.JobGarnish), "text_dim"));
            left.Add(new Row("거래 실현 순손익 (수수료 반영)", Fmt.SignedWon(t.Realized), Palette.UpDown(t.Realized) == "text_dim" ? "text" : Palette.UpDown(t.Realized)));
            left.Add(new Row("  └ 오늘 낸 거래 수수료", Fmt.Won(t.FeesPaid), "text_dim"));
            if (t.StartCapital > 0) left.Add(new Row("  └ 거래 시작 자금 대비", Fmt.Pct((double)t.Realized / t.StartCapital), "text_dim"));
            left.Add(new Row("보유 주식 평가액", Fmt.Won(g.HoldingsValue()), "text"));
            left.Add(new Row("  └ 미실현 평가손익", Fmt.SignedWon(g.UnrealizedPnl()), "text_dim"));
            if (t.LoanAmount > 0) left.Add(new Row("오늘 받은 대출 (수익 아님)", Fmt.Won(t.LoanAmount), "text"));
            if (t.InterestCharged > 0) left.Add(new Row("대출 이자 (오늘 부과)", "−" + Fmt.Won(t.InterestCharged), "danger"));
            if (t.AutoRepaid > 0) left.Add(new Row("만기 자동 상환", Fmt.Won(t.AutoRepaid), "text"));
            if (t.FurnitureSpent > 0) left.Add(new Row("생활 구매 (거래 손실 아님)", Fmt.Won(t.FurnitureSpent), "text"));
            long items = 0;
            foreach (var kv in t.ItemUses) items += kv.Value * J.L(g.RecoveryItem(kv.Key), "price", 0);
            if (items > 0) left.Add(new Row("식사·간식·음료", Fmt.Won(items), "text"));
            if (t.TradingStarted && S.CurrentTick < S.SessionEnd) left.Add(new Row("장 마감 전 거래 종료", "예 (" + t.TicksWatched + "분 지켜봄)", "text_dim"));

            right.Add(new Row("현금", Fmt.Won(S.Cash), "text"));
            right.Add(new Row("투자 가능 자산", Fmt.Won(g.InvestableAssets()), "text"));
            right.Add(new Row("추가 대출 잔액", S.Loan != null ? Fmt.Won(S.Loan.Owed) + (S.Loan.Overdue ? " (연체)" : " · " + S.Loan.DueDay + "일차 만기") : "없음", S.Loan != null && S.Loan.Overdue ? "danger" : "text"));
            right.Add(new Row("남은 시작 부채", Fmt.Won(S.StartingDebt), "text_dim"));
            right.Add(new Row("생활 진척 (구매·상환 포함)", Fmt.SignedWon(g.LifeProgress()), "text_dim"));

            reviewEntries.Clear();
            if (reviewOpen && ReviewChartState.Symbol != null) reviewEntries.AddRange(g.ReviewEntries(ReviewChartState.Symbol, S.Day));
        }

        void OpenReview()
        {
            if (S == null) return;
            reviewOpen = true;
            S.Today.ReviewOpened = true;
            ReviewChartState.Day = S.Day;
            // 오늘 매매한 종목이 있으면 그 종목부터
            string first = S.Tradable.Count > 0 ? S.Tradable[0] : null;
            foreach (var t in S.Trades) if (t.Day == S.Day) { first = t.CompanyId; break; }
            if (ReviewChartState.Symbol == null || !S.Tradable.Contains(ReviewChartState.Symbol) || !HasTradeToday(ReviewChartState.Symbol)) ReviewChartState.Symbol = first;
        }

        bool HasTradeToday(string id)
        {
            foreach (var t in S.Trades) if (t.Day == S.Day && t.CompanyId == id) return true;
            return false;
        }

        static List<long> LoanOpts { get { return GameConfig.Instance != null ? GameConfig.Instance.balance.loan.repayOptions : new List<long>(); } }
        static List<long> DebtOpts { get { return GameConfig.Instance != null ? GameConfig.Instance.balance.debt.repayOptions : new List<long>(); } }

        public override string Text(string key, Ctx c)
        {
            if (S == null) return null;
            var t = S.Today;
            switch (key)
            {
                case "title": return S.Day + "일차 결산";
                case "subtitle": return t.Trades > 0 ? "오늘 체결 " + t.Trades + "건" : "오늘은 매매하지 않았어요";
                case "repaid_today": return "오늘 상환: 대출 " + Fmt.Won(t.RepaidLoan) + " · 시작 부채 " + Fmt.Won(t.RepaidStartingDebt);
                case "footnote": return "확정 후에는 오늘 거래를 다시 열 수 없습니다. 보유 주식은 다음 날로 넘어갑니다.";
                case "rv_title": return S.Day + "일차 복기 · " + (ReviewChartState.Symbol != null ? Game.CompanyName(ReviewChartState.Symbol) : "");
                case "rv_summary": return ReviewChartState.Symbol != null ? Game.I.ReviewSummary(ReviewChartState.Symbol, S.Day) : "";
                case "rv_note": return "그날 실제로 진행한 차트와 이미 공개됐던 글, 내 매매만 보여 줘요. 결과만으로 판단을 채점하지 않아요.";
                case "rv_empty": return reviewEntries.Count == 0 ? "이 종목은 오늘 매매도, 공개된 관련 글도 없었어요." : "";
                case "rv_tab_label": return c.Index < S.Tradable.Count ? Game.CompanyName(S.Tradable[c.Index]) + (HasTradeToday(S.Tradable[c.Index]) ? " ●" : "") : "";
                case "rv_time": return c.Index < reviewEntries.Count ? Game.I.ClockOfTick(reviewEntries[c.Index].Tick) : "";
                case "rv_text": return c.Index < reviewEntries.Count ? reviewEntries[c.Index].Text : "";
                case "r_label": { var r = RowAt(c); return r.HasValue ? r.Value.L : ""; }
                case "r_value": { var r = RowAt(c); return r.HasValue ? r.Value.V : ""; }
                case "label":
                    if (c.List == "loan_repay") return c.Index < LoanOpts.Count ? Fmt.N(LoanOpts[c.Index]) : "";
                    if (c.List == "debt_repay") return c.Index < DebtOpts.Count ? Fmt.N(DebtOpts[c.Index]) : "";
                    return "";
            }
            return null;
        }

        Row? RowAt(Ctx c)
        {
            var list = c.List == "right" ? right : left;
            if (c.Index < 0 || c.Index >= list.Count) return null;
            return list[c.Index];
        }

        public override string ColorKey(string key, Ctx c)
        {
            if (key == "r_color") { var r = RowAt(c); return r.HasValue ? r.Value.C : null; }
            if (key == "rv_color")
            {
                if (c.Index >= reviewEntries.Count) return null;
                var e = reviewEntries[c.Index];
                if (e.Kind == "buy") return "up";
                if (e.Kind == "sell") return "down";
                return e.Read ? "text" : "text_dim";
            }
            return null;
        }

        public override bool Flag(string key, Ctx c)
        {
            if (S == null) return false;
            switch (key)
            {
                case "has_loan": return S.Loan != null;
                case "has_repaid": return S.Today.RepaidLoan + S.Today.RepaidStartingDebt > 0;
                case "can_repay": return S.Cash > 0;
                case "review_open": return reviewOpen;
                case "rv_tab_selected": return c.Index < S.Tradable.Count && S.Tradable[c.Index] == ReviewChartState.Symbol;
            }
            return false;
        }

        public override int Count(string list)
        {
            switch (list)
            {
                case "left": return left.Count;
                case "right": return right.Count;
                case "loan_repay": return S != null && S.Loan != null ? LoanOpts.Count : 0;
                case "debt_repay": return DebtOpts.Count;
                case "rv_tabs": return S != null && reviewOpen ? S.Tradable.Count : 0;
                case "rv_events": return reviewOpen ? reviewEntries.Count : 0;
            }
            return 0;
        }

        public override void Act(string action, Ctx c)
        {
            var g = Game.I;
            switch (action)
            {
                case "loan_repay": if (c.Index < LoanOpts.Count) Pay(g.RepayLoan(LoanOpts[c.Index])); break;
                case "loan_all": Pay(g.RepayLoan(g.LoanOwed())); break;
                case "debt_repay": if (c.Index < DebtOpts.Count) Pay(g.RepayStartingDebt(DebtOpts[c.Index])); break;
                case "confirm": Finish(); break;
                case "review_open": OpenReview(); break;
                case "review_close": reviewOpen = false; break;
                case "rv_tab": if (S != null && c.Index < S.Tradable.Count) ReviewChartState.Symbol = S.Tradable[c.Index]; break;
                case "back": GameRoot.I.Go(ScreenId.Home); break;
            }
        }

        static void Pay(long p) { if (p > 0) GameRoot.I.Toast(Fmt.Won(p) + " 상환"); }

        void Finish()
        {
            var g = Game.I;
            var root = GameRoot.I;
            var cfg = GameConfig.Instance;
            var res = g.FinalizeSettlement();
            foreach (var id in res.NewUnlocks)
            {
                if (id == res.CutsceneId) continue;
                var sc = cfg != null ? cfg.Scene(id) : null;
                root.Toast("새 장면 해금: " + (sc != null ? sc.title : id) + " (밤의 기록에서 다시 보기)");
            }
            foreach (var th in res.NewThemes)
            {
                var t = cfg != null ? cfg.Theme(th) : null;
                root.Toast("새 컴퓨터 테마 해금: " + (t != null ? t.name : th));
            }
            Action afterStory = () => PlayNightScene(res);
            if (res.StoryMilestoneId != null)
            {
                root.Go(ScreenId.Story);
                root.story.Open(res.StoryMilestoneId, afterStory);
            }
            else afterStory();
        }

        static void PlayNightScene(SettleResult res)
        {
            var g = Game.I;
            var root = GameRoot.I;
            var cfg = GameConfig.Instance;
            var scene = cfg != null ? cfg.Scene(res.CutsceneId) : null;
            bool isDefault = cfg != null && cfg.cutscenes.defaultScene != null && cfg.cutscenes.defaultScene.id == res.CutsceneId;
            GalleryEntry entry;
            bool seen = res.CutsceneId != null && g.Gallery.Entries.TryGetValue(res.CutsceneId, out entry) && entry.Seen;
            if (scene == null || (isDefault && g.Settings.SkipDefaultBedtime) || (!res.CutsceneIsNew && seen && g.Settings.AutoSkipSeenCutscenes))
            {
                root.Go(ScreenId.Home);
                return;
            }
            root.Go(ScreenId.Cutscene);
            root.cutscene.Play(scene, false, () => root.Go(ScreenId.Home));
        }
    }
}
