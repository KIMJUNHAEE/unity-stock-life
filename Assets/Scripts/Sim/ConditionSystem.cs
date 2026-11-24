using System;
using System.Collections.Generic;

namespace StockGame
{
    /// <summary>
    /// 컨디션: 거래 시간(시장 틱) 기준으로만 줄어든다. 메뉴·브라우저에서 시장이 멈추면 함께 멈춘다.
    /// 효과는 관찰 방해(가장자리 흐림, 눈 감김)뿐이며 가격·체결·주문에는 영향이 없다.
    /// </summary>
    public partial class Game
    {
        public List<object> ConditionStages() { return Cfg.A("balance", "condition.stages"); }

        public int StageIndex()
        {
            var stages = ConditionStages();
            if (S == null) return 0;
            if (S.Condition <= 0) return Math.Max(0, stages.Count - 1);
            for (int i = 0; i < stages.Count; i++)
                if (S.Condition >= J.D(stages[i], "min", 0)) return i;
            return Math.Max(0, stages.Count - 1);
        }

        public Dictionary<string, object> Stage() { var s = ConditionStages(); int i = StageIndex(); return i < s.Count ? s[i] as Dictionary<string, object> : null; }
        public string StageName() { return J.S(Stage(), "name", "양호"); }

        public double CurrentDrainPerTick()
        {
            if (S.CurrentTick < S.DrainSuppressUntil) return 0;
            double d = Cfg.D("balance", "condition.drain_per_tick", 0.25);
            if (S.CurrentTick < S.DrainReduceUntil) d *= S.DrainReduceMult;
            return d;
        }

        /// <summary>틱 하나가 끝날 때: 컨디션 소모, 눈 감김 일정, 단계 안내.</summary>
        void OnTickEnd(int finishedTick)
        {
            S.Today.TicksWatched++;
            int before = StageIndex();
            S.Condition = Math.Max(0, S.Condition - CurrentDrainPerTick());
            S.Today.MinCondition = Math.Min(S.Today.MinCondition, S.Condition);
            int after = StageIndex();
            if (after > before)
            {
                if (J.Has(Stage(), "blink") && !S.WarnedSleepy)
                {
                    S.WarnedSleepy = true;
                    React("condition_blink_warning");
                    Toast(Cfg.S("dialogue", "system.blink_warning", "졸음 때문에 차트를 잠깐 놓칠 수 있어요."));
                    S.NextBlinkTick = S.CurrentTick + Cfg.I("balance", "condition.first_blink_delay_ticks", 6);
                }
                else if (!S.WarnedTired)
                {
                    S.WarnedTired = true;
                    React("condition_tired");
                }
                else React("condition_down");
            }
            if (S.Condition <= 0) { React("condition_empty"); return; }

            var blink = J.Get(Stage(), "blink");
            if (blink != null)
            {
                if (S.NextBlinkTick <= 0) S.NextBlinkTick = S.CurrentTick + NextBlinkInterval(blink);
                else if (S.CurrentTick >= S.NextBlinkTick)
                {
                    double dMin, dMax;
                    J.Range(blink, "duration_seconds", 0.5, 1.0, out dMin, out dMax);
                    float dur = (float)(dMin + (dMax - dMin) * Hash.U01(Hash.H(S.Seed, Hash.Str("blink"), (ulong)S.CurrentTick)));
                    if (OnBlink != null) OnBlink(dur);
                    S.NextBlinkTick = S.CurrentTick + NextBlinkInterval(blink);
                }
            }
            else S.NextBlinkTick = 0;
        }

        int NextBlinkInterval(object blink)
        {
            double a, b;
            J.Range(blink, "interval_ticks", 15, 30, out a, out b);
            double u = Hash.U01(Hash.H(S.Seed, Hash.Str("blink-int"), (ulong)S.CurrentTick));
            return Math.Max(2, (int)Math.Round(a + (b - a) * u));
        }

        public Dictionary<string, object> RecoveryItem(string key) { return Cfg.Get("balance", "condition.items." + key) as Dictionary<string, object>; }

        public int ItemUsesToday(string key)
        {
            int n;
            return S.Today.ItemUses.TryGetValue(key, out n) ? n : 0;
        }

        /// <summary>식사·간식·음료 사용. 성공 시 null.</summary>
        public string UseRecoveryItem(string key)
        {
            var it = RecoveryItem(key);
            if (it == null) return "알 수 없는 항목입니다.";
            long price = J.L(it, "price", 0);
            int limit = J.I(it, "limit_per_night", 99);
            int used = ItemUsesToday(key);
            if (used >= limit) return J.S(it, "name", key) + "은(는) 오늘 더 쓸 수 없습니다.";
            if (S.Cash < price) return "현금이 부족합니다.";
            double max = Cfg.D("balance", "condition.max_value", 100);
            S.Cash -= price;
            S.Today.ItemUses[key] = used + 1;
            double decay = Math.Pow(J.D(it, "repeat_decay", 1.0), used);
            S.Condition = Math.Min(max, S.Condition + J.D(it, "restore", 0) * decay);
            int suppress = (int)Math.Round(J.D(it, "suppress_drain_ticks", 0) * decay);
            if (suppress > 0) S.DrainSuppressUntil = Math.Max(S.DrainSuppressUntil, S.CurrentTick) + suppress;
            int reduce = J.I(it, "reduce_drain_ticks", 0);
            if (reduce > 0)
            {
                S.DrainReduceUntil = S.CurrentTick + reduce;
                S.DrainReduceMult = J.D(it, "reduce_drain_mult", 0.6);
            }
            if (key == "meal") S.SetFlag("meal_today_" + S.Day);
            React("use_" + key);
            return null;
        }
    }
}
