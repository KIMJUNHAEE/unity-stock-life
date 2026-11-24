using System;
using System.Collections.Generic;

namespace StockGame
{
    public class SettleResult
    {
        public string StoryMilestoneId;
        public string CutsceneId;
        public bool CutsceneIsNew;
        public List<string> NewUnlocks = new List<string>();
        public List<string> NewThemes = new List<string>();
    }

    /// <summary>알바 정산, 밤 결산, 컷신·스토리·테마 판정, 가구 구매.</summary>
    public partial class Game
    {
        /// <summary>조건식에서 쓸 수 있는 값 이름 (Inspector 드롭다운용). ComputeStats 와 같게 유지.</summary>
        public static readonly string[] StatNames =
        {
            "realized_today", "trades_today", "buys_today", "sells_today", "return_today", "fees_today", "start_capital",
            "took_loan_today", "loan_amount_today", "had_loan_before_today", "loan_cleared_today", "loan_balance", "loan_overdue",
            "interest_today", "repaid_loan_today", "repaid_starting_debt_today", "starting_debt_remaining", "ever_took_loan",
            "investable_assets", "holdings_value", "unrealized_pnl", "cash", "net_financial_assets", "net_financial_assets_excluding_starting_debt",
            "cumulative_realized_profit", "consecutive_profit_nights", "condition", "condition_stage", "min_condition_today",
            "meal_used_today", "snack_used_today", "drinks_used_today", "ended_early", "session_ticks_watched", "session_length",
            "rumor_early_profit_today", "false_rumor_loss_today", "news_chase_loss_today", "articles_read_today", "posts_read_today",
            "job_done_today", "job_pay_today", "furniture_owned", "furniture_spent_today", "day", "nights_played",
            "story_milestones_unlocked", "forced_liquidations_today"
        };

        // ───────────────────────── 알바 ─────────────────────────

        /// <summary>알바 완료. 연체 중이면 일부 자동 상환 후 남은 금액을 현금으로.</summary>
        public long CompleteJob(long pay)
        {
            long garnish = GarnishJobPay(pay);
            S.Cash += pay - garnish;
            S.Today.JobDone = true;
            S.Today.JobPay = pay;
            S.Today.JobGarnish = garnish;
            S.Condition = Math.Max(0, S.Condition - Cfg.D("job", "condition_cost", 8));
            StartEvening();
            return garnish;
        }

        public void SkipJob()
        {
            S.Today.JobSkipped = true;
            StartEvening();
        }

        // ───────────────────────── 결산 ─────────────────────────

        /// <summary>결산 화면 진입 시 하루 1회: 이자와 만기 처리. 저장 후 다시 열어도 중복 부과하지 않는다.</summary>
        public void PrepareSettlement()
        {
            if (S.Today.SettlementPrepared || S.Today.Settled) return;
            S.Today.SettlementPrepared = true;
            if (S.Phase == "morning") S.Phase = "evening";
            AccrueInterestAndMaturity();
            Save();
        }

        public SettleResult FinalizeSettlement()
        {
            var res = new SettleResult();
            PrepareSettlement();
            S.Today.Settled = true;
            S.Today.EndedEarly = S.Today.TradingStarted && S.CurrentTick < S.SessionEnd;
            S.Today.EndInvestable = InvestableAssets();
            S.Today.EndNetFinancial = NetFinancialAssets();
            S.CumulativeRealized += S.Today.Realized;
            if (S.Today.Trades > 0)
                S.ConsecutiveProfitNights = S.Today.Realized > 0 ? S.ConsecutiveProfitNights + 1 : 0;

            var stats = ComputeStats();

            // 1) 스토리 구간 (이자·상환 반영 후, 가구 구매 전)
            res.StoryMilestoneId = CheckStoryMilestone(stats);

            // 2) 컷신: 조건 충족 장면을 모두 해금, 대표 1개 재생
            var matched = new List<Dictionary<string, object>>();
            foreach (var o in Cfg.A("cutscenes", "scenes"))
            {
                var cs = o as Dictionary<string, object>;
                if (cs == null || !J.B(cs, "enabled", true)) continue;
                if (!J.B(cs, "repeatable", true) && Gallery.Entries.ContainsKey(J.S(cs, "id", ""))) continue;
                if (EvalCondition(J.Get(cs, "conditions"), stats)) matched.Add(cs);
            }
            Dictionary<string, object> best = null;
            bool bestNew = false;
            int bestPri = int.MinValue;
            foreach (var cs in matched)
            {
                string id = J.S(cs, "id", "");
                bool isNew = !Gallery.Entries.ContainsKey(id);
                if (isNew)
                {
                    Gallery.Entries[id] = new GalleryEntry { Id = id, FirstDay = S.Day, FirstUnlockedAt = DateTime.Now.ToString("yyyy-MM-dd"), Seen = false };
                    res.NewUnlocks.Add(id);
                    S.Today.CutscenesUnlocked.Add(id);
                }
                int pri = J.I(cs, "priority", 0);
                // 미감상 신규 장면 우선 → 우선순위 → 목록 순서
                if (best == null || (isNew && !bestNew) || (isNew == bestNew && pri > bestPri))
                {
                    best = cs; bestNew = isNew; bestPri = pri;
                }
            }
            if (best != null)
            {
                res.CutsceneId = J.S(best, "id", null);
                res.CutsceneIsNew = bestNew;
            }
            else res.CutsceneId = J.S(Cfg.O("cutscenes", "default_scene"), "id", null);
            S.Today.CutscenePlayed = res.CutsceneId;

            // 3) UI 테마 해금
            foreach (var kv in Cfg.O("theme", "themes"))
            {
                if (S.UnlockedThemes.Contains(kv.Key)) continue;
                var unlock = J.Get(kv.Value, "unlock");
                if (unlock == null) { S.UnlockedThemes.Add(kv.Key); continue; }
                if (EvalCondition(unlock, stats))
                {
                    S.UnlockedThemes.Add(kv.Key);
                    res.NewThemes.Add(kv.Key);
                }
            }

            long r = S.Today.Realized;
            if (S.Today.Trades == 0) React("settle_no_trade");
            else if (r > 0) React("settle_profit");
            else if (r < 0) React("settle_loss");
            else React("settle_flat");

            S.Phase = "night";
            Save();
            SaveMeta();
            return res;
        }

        /// <summary>컷신·스토리·테마 조건에서 쓸 수 있는 값 목록. config/README.md 에 정리되어 있다.</summary>
        public Dictionary<string, double> ComputeStats()
        {
            var t = S.Today;
            var d = new Dictionary<string, double>();
            d["day"] = S.Day;
            d["nights_played"] = S.History.Count + 1;
            d["cash"] = S.Cash;
            d["realized_today"] = t.Realized;
            d["fees_today"] = t.FeesPaid;
            d["trades_today"] = t.Trades;
            d["buys_today"] = t.Buys;
            d["sells_today"] = t.Sells;
            d["start_capital"] = t.StartCapital;
            d["return_today"] = t.StartCapital > 0 ? (double)t.Realized / t.StartCapital : double.NaN;
            d["took_loan_today"] = t.TookLoan ? 1 : 0;
            d["loan_amount_today"] = t.LoanAmount;
            d["had_loan_before_today"] = t.HadLoanBefore ? 1 : 0;
            d["loan_balance"] = LoanOwed();
            d["loan_overdue"] = S.Loan != null && S.Loan.Overdue ? 1 : 0;
            d["loan_cleared_today"] = t.HadLoanBefore && S.Loan == null ? 1 : 0;
            d["repaid_loan_today"] = t.RepaidLoan;
            d["repaid_starting_debt_today"] = t.RepaidStartingDebt;
            d["interest_today"] = t.InterestCharged;
            d["starting_debt_remaining"] = S.StartingDebt;
            d["ever_took_loan"] = S.EverTookLoan ? 1 : 0;
            d["investable_assets"] = InvestableAssets();
            d["holdings_value"] = HoldingsValue();
            d["unrealized_pnl"] = UnrealizedPnl();
            d["net_financial_assets"] = NetFinancialAssets();
            d["net_financial_assets_excluding_starting_debt"] = InvestableAssets() - LoanOwed();
            d["cumulative_realized_profit"] = S.CumulativeRealized;
            d["consecutive_profit_nights"] = S.ConsecutiveProfitNights;
            d["condition"] = S.Condition;
            d["condition_stage"] = StageIndex();
            d["min_condition_today"] = t.MinCondition;
            d["meal_used_today"] = ItemUsesToday("meal");
            d["snack_used_today"] = ItemUsesToday("snack");
            d["drinks_used_today"] = ItemUsesToday("energy_drink");
            d["ended_early"] = t.EndedEarly ? 1 : 0;
            d["session_ticks_watched"] = t.TicksWatched;
            d["session_length"] = S.TicksPerDay;
            d["rumor_early_profit_today"] = t.RumorEarlyProfit;
            d["false_rumor_loss_today"] = t.FalseRumorLoss;
            d["news_chase_loss_today"] = t.NewsChaseLoss;
            d["articles_read_today"] = t.ArticlesRead;
            d["posts_read_today"] = t.PostsRead;
            d["job_done_today"] = t.JobDone ? 1 : 0;
            d["job_pay_today"] = t.JobPay;
            d["furniture_owned"] = S.OwnedItems.Count;
            d["furniture_spent_today"] = t.FurnitureSpent;
            d["forced_liquidations_today"] = 0; // 선물 도입 전까지 항상 0
            d["story_milestones_unlocked"] = S.StoryUnlocked.Count;
            return d;
        }

        /// <summary>
        /// 조건식: {"all":[...]}, {"any":[...]}, {"not":{...}}, {"stat":"이름","op":">=","value":숫자}
        /// 모르는 stat, 계산할 수 없는 값(NaN)은 거짓으로 처리한다. null 값도 거짓 (미정 = 충족 아님).
        /// </summary>
        public bool EvalCondition(object cond, Dictionary<string, double> stats)
        {
            if (cond == null) return false;
            var d = cond as Dictionary<string, object>;
            var list = cond as List<object>;
            if (list != null)
            {
                foreach (var c in list) if (!EvalCondition(c, stats)) return false;
                return list.Count > 0;
            }
            if (d == null) return false;
            if (d.ContainsKey("all"))
            {
                var l = J.A(d, "all");
                foreach (var c in l) if (!EvalCondition(c, stats)) return false;
                return l.Count > 0;
            }
            if (d.ContainsKey("any"))
            {
                foreach (var c in J.A(d, "any")) if (EvalCondition(c, stats)) return true;
                return false;
            }
            if (d.ContainsKey("not")) return !EvalCondition(J.Get(d, "not"), stats);
            if (d.ContainsKey("flag")) return S.HasFlag(J.S(d, "flag", ""));
            string stat = J.S(d, "stat", null);
            object rawVal = J.Get(d, "value");
            double left;
            if (stat == null || rawVal == null || !stats.TryGetValue(stat, out left) || double.IsNaN(left)) return false;
            double right = J.ToD(rawVal, double.NaN);
            if (double.IsNaN(right)) return false;
            switch (J.S(d, "op", ">="))
            {
                case ">": return left > right;
                case ">=": return left >= right;
                case "<": return left < right;
                case "<=": return left <= right;
                case "==": return Math.Abs(left - right) < 1e-9;
                case "!=": return Math.Abs(left - right) >= 1e-9;
            }
            return false;
        }

        // ───────────────────────── 스토리 구간 ─────────────────────────

        /// <summary>
        /// data/story_milestones.json: 앞 구간을 마친 다음 구간만 확인.
        /// requirements 중 하나라도 null(미정)이면 열리지 않는다.
        /// </summary>
        string CheckStoryMilestone(Dictionary<string, double> stats)
        {
            var items = new List<Dictionary<string, object>>();
            foreach (var o in Cfg.Items("story")) { var d = o as Dictionary<string, object>; if (d != null) items.Add(d); }
            items.Sort((a, b) => J.I(a, "order", 0).CompareTo(J.I(b, "order", 0)));
            foreach (var m in items)
            {
                string id = J.S(m, "id", "");
                if (S.StoryUnlocked.Contains(id)) continue;
                string prev = J.S(m, "requires_previous_milestone", null);
                if (prev != null && !S.StoryUnlocked.Contains(prev)) return null;
                if (!StoryRequirementsMet(m, stats)) return null;
                S.StoryUnlocked.Add(id);
                return id;
            }
            return null;
        }

        public bool StoryRequirementsConfigured(Dictionary<string, object> m)
        {
            var req = J.O(m, "requirements");
            if (req.Count == 0) return false;
            foreach (var kv in req) if (kv.Value == null) return false;
            return true;
        }

        bool StoryRequirementsMet(Dictionary<string, object> m, Dictionary<string, double> stats)
        {
            if (!StoryRequirementsConfigured(m)) return false;
            var req = J.O(m, "requirements");
            foreach (var kv in req)
            {
                double need = J.ToD(kv.Value, double.NaN);
                if (double.IsNaN(need)) return false;
                string key = kv.Key;
                string stat = key;
                if (key.EndsWith("_min")) stat = key.Substring(0, key.Length - 4);
                if (stat == "cumulative_realized_net_profit") stat = "cumulative_realized_profit";
                double have;
                if (!stats.TryGetValue(stat, out have)) return false;
                if (key.EndsWith("_max") ? have > need : have < need) return false;
            }
            return true;
        }

        public void ChooseStory(string milestoneId, string choiceId)
        {
            S.StoryChoices[milestoneId] = choiceId;
            Save();
        }

        // ───────────────────────── 가구 ─────────────────────────

        public static Dictionary<string, object> ShopItem(string id)
        {
            foreach (var o in Cfg.A("shop", "items"))
            {
                var d = o as Dictionary<string, object>;
                if (d != null && J.S(d, "id", null) == id) return d;
            }
            return null;
        }

        public string BuyItem(string id)
        {
            var item = ShopItem(id);
            if (item == null) return "없는 상품입니다.";
            if (S.OwnedItems.Contains(id)) return "이미 가지고 있습니다.";
            long price = J.L(item, "price", 0);
            if (S.Cash < price) return "현금이 부족합니다.";
            S.Cash -= price;
            S.OwnedItems.Add(id);
            S.Equipped[J.S(item, "slot", "")] = id;
            S.Today.FurnitureSpent += price;
            React("buy_furniture");
            Save();
            return null;
        }

        public void EquipItem(string id)
        {
            var item = ShopItem(id);
            if (item == null || !S.OwnedItems.Contains(id)) return;
            S.Equipped[J.S(item, "slot", "")] = id;
            Save();
        }

        /// <summary>집 화면에 보여줄 다음 목표: 아직 없는 가장 싼 가구.</summary>
        public Dictionary<string, object> NextGoalItem()
        {
            Dictionary<string, object> best = null;
            foreach (var o in Cfg.A("shop", "items"))
            {
                var d = o as Dictionary<string, object>;
                if (d == null || S.OwnedItems.Contains(J.S(d, "id", ""))) continue;
                if (best == null || J.L(d, "price", 0) < J.L(best, "price", 0)) best = d;
            }
            return best;
        }
    }
}
