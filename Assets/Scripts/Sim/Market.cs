using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 가상 시장. 가격 = 시장 공통 흐름 + 종목 잡음 + 평균 회귀 + 사건 효과.
    /// 사건이 먼저 정해지고 소문·기사·가격이 그 사건에 연결된다.
    /// 플레이어의 매수/열람은 가격 난수에 영향을 주지 않는다(시장 난수와 문구 난수 분리).
    /// </summary>
    public partial class Game
    {
        static readonly ulong SaltMarket = Hash.Str("market-factor");
        static readonly ulong SaltVolume = Hash.Str("volume");
        readonly Dictionary<string, ulong> idHash = new Dictionary<string, ulong>();

        ulong IdHash(string id)
        {
            ulong h;
            if (!idHash.TryGetValue(id, out h)) { h = Hash.Str(id); idHash[id] = h; }
            return h;
        }

        void InitMarket()
        {
            S.Tradable.Clear();
            var wanted = Cfg.A("balance", "market.tradable_company_ids");
            foreach (var o in wanted)
            {
                string id = J.ToS(o, null);
                if (id != null && Company(id) != null && !S.Tradable.Contains(id)) S.Tradable.Add(id);
            }
            if (S.Tradable.Count == 0)
            {
                int n = Cfg.I("balance", "market.tradable_company_count", 3);
                foreach (var c in Cfg.Items("companies"))
                {
                    if (S.Tradable.Count >= n) break;
                    S.Tradable.Add(J.S(c, "id", ""));
                }
            }
            S.Watchlist = new List<string>(S.Tradable);

            var rng = new StreamRng(Hash.H(S.Seed, Hash.Str("init")), 0);
            double pMin, pMax;
            Cfg.Range("balance", "market.initial_price_range", 1000, 30000, out pMin, out pMax);
            foreach (var id in S.Tradable)
            {
                double price = Cfg.D("balance", "market.company_overrides." + id + ".initial_price", 0);
                if (price <= 0) price = Math.Exp(rng.Range(Math.Log(pMin), Math.Log(pMax)));
                price = Quantize(price);
                double volSpread = Cfg.D("balance", "market.volatility_run_spread", 0.25);
                var st = new StockState
                {
                    Id = id,
                    Price = price,
                    Anchor = price,
                    PrevClose = price,
                    SessionOpen = price,
                    VolMult = Math.Exp(rng.Normal() * volSpread),
                    VolumeMult = Math.Exp(rng.Normal() * 0.3),
                    Beta = rng.Range(0.6, 1.4)
                };
                S.Stocks[id] = st;
            }
        }

        // ───────────────────────── 호가 단위 ─────────────────────────

        public double TickSize(double price)
        {
            var table = Cfg.A("balance", "market.tick_sizes");
            foreach (var row in table)
            {
                var r = row as List<object>;
                if (r == null || r.Count < 2) continue;
                if (price < J.ToD(r[0], 0)) return Math.Max(1, J.ToD(r[1], 1));
            }
            return 1;
        }

        public double Quantize(double price)
        {
            double ts = TickSize(price);
            double q = Math.Round(price / ts) * ts;
            double min = Cfg.D("balance", "market.min_price", 100);
            return Math.Max(q, min);
        }

        // ───────────────────────── 진행 ─────────────────────────

        public bool MarketOpenNow()
        {
            return S != null && S.Phase == "evening" && !S.Today.Settled && S.CurrentTick < S.SessionEnd && S.Condition > 0;
        }

        // ───────────────────────── 연장 거래 ─────────────────────────

        /// <summary>기본 마감 직전에 연장을 제안할 때인지. 이미 고른 날은 다시 묻지 않는다.</summary>
        public bool ExtensionOfferDue()
        {
            if (S == null || S.ExtensionTicks <= 0 || S.Phase != "evening" || S.Today.Settled) return false;
            if (S.Today.Extended || S.Today.ExtensionDeclined || !S.Today.TradingStarted) return false;
            int before = Math.Max(0, Cfg.I("balance", "market.extension_offer_before_ticks", 10));
            return S.CurrentTick >= S.BaseSessionEnd - before && S.Condition > 0;
        }

        /// <summary>연장 신청. 이미 배정된 사건과 가격 흐름을 그대로 이어서 보여준다 (새로 추첨하지 않음).</summary>
        public void AcceptExtension()
        {
            if (S == null || S.Today.Extended || S.ExtensionTicks <= 0) return;
            S.Today.Extended = true;
            S.Today.ExtensionDeclined = false;
            Save();
        }

        public void DeclineExtension()
        {
            if (S == null || S.Today.Extended) return;
            S.Today.ExtensionDeclined = true;
            Save();
        }

        /// <summary>실시간 진행. UI 가 '시장 진행 가능' 조건을 판단해 호출한다.</summary>
        public void Advance(float dt, float speed)
        {
            if (!MarketOpenNow()) { subAccum = 0; return; }
            EnsureTradingStarted();
            double secPerTick = Math.Max(0.05, Cfg.D("balance", "market.seconds_per_tick", 1.2));
            double subSec = secPerTick / S.Substeps;
            subAccum += dt * speed;
            int guard = 0;
            while (subAccum >= subSec && guard++ < 400)
            {
                subAccum -= (float)subSec;
                StepSub();
                if (!MarketOpenNow()) { subAccum = 0; break; }
            }
        }

        /// <summary>디버그: 지정 틱 수만큼 즉시 진행.</summary>
        public void AdvanceTicks(int ticks)
        {
            EnsureTradingStarted();
            int target = Math.Min(S.SessionEnd, S.CurrentTick + ticks);
            int guard = 0;
            while (S.CurrentTick < target && MarketOpenNow() && guard++ < 100000) StepSub();
        }

        public void EnsureTradingStarted()
        {
            if (S.Today.TradingStarted || S.Phase != "evening") return;
            S.Today.TradingStarted = true;
            S.Today.StartCapital = InvestableAssets();
        }

        void StepSub()
        {
            int tick = S.CurrentTick;
            int sub = S.SubIndex;
            if (sub == 0)
            {
                foreach (var st in S.Stocks.Values)
                    st.Live = new Candle { Tick = tick, O = st.Price, H = st.Price, L = st.Price, C = st.Price, V = 0 };
                ProcessPending(tick);
            }

            int subs = S.Substeps;
            double sqrtS = Math.Sqrt(subs);
            double mktVol = Cfg.D("balance", "market.market_factor_vol_per_tick", 0.0015);
            double baseVol = Cfg.D("balance", "market.base_volatility_per_tick", 0.004);
            double meanRev = Cfg.D("balance", "market.mean_reversion_per_tick", 0.01);
            double drift = Cfg.D("balance", "market.drift_per_tick", 0.0);
            double attnVolBoost = Cfg.D("balance", "market.attention_vol_boost", 0.35);
            double baseVolume = Cfg.D("balance", "market.base_volume_per_tick", 4000);
            double volRetK = Cfg.D("balance", "market.volume_return_sensitivity", 60);
            double mkt = Hash.Normal(Hash.H(S.Seed, SaltMarket, (ulong)tick, (ulong)sub)) * mktVol / sqrtS;

            foreach (var id in S.Tradable)
            {
                StockState st;
                if (!S.Stocks.TryGetValue(id, out st)) continue;
                double eff = 0, attention = 1;
                foreach (var e in S.Events)
                {
                    if (e.CompanyId != id || tick < e.StartTick || tick > e.FollowTick) continue;
                    double jump, grad;
                    EventEffect(e, tick, out jump, out grad);
                    eff += grad / subs;
                    if (sub == 0) eff += jump;
                    attention = Math.Max(attention, EventAttention(e, tick));
                }
                double vol = baseVol * st.VolMult * Cfg.D("balance", "market.company_overrides." + id + ".volatility_mult", 1.0);
                ulong h = Hash.H(S.Seed, IdHash(id), (ulong)tick, (ulong)sub);
                double noise = Hash.Normal(h) * vol / sqrtS * (1 + (attention - 1) * attnVolBoost);
                st.Anchor *= Math.Exp(eff);
                double rev = meanRev / subs * (Math.Log(st.Anchor) - Math.Log(st.Price));
                double ret = st.Beta * mkt + noise + eff + rev + drift / subs;
                double np = Quantize(st.Price * Math.Exp(ret));
                st.Price = np;
                if (st.Live == null) st.Live = new Candle { Tick = tick, O = np, H = np, L = np, C = np };
                st.Live.H = Math.Max(st.Live.H, np);
                st.Live.L = Math.Min(st.Live.L, np);
                st.Live.C = np;
                double vn = Hash.Normal(Hash.H(S.Seed, SaltVolume ^ IdHash(id), (ulong)tick, (ulong)sub));
                double v = baseVolume * st.VolumeMult / subs * attention * (1 + Math.Abs(ret) * volRetK) * Math.Exp(0.45 * vn);
                st.Live.V += (long)Math.Max(1, Math.Round(v));
            }

            S.SubIndex++;
            if (S.SubIndex >= subs)
            {
                foreach (var st in S.Stocks.Values)
                {
                    if (st.Live != null) st.Candles.Add(st.Live);
                    st.Live = null;
                }
                S.SubIndex = 0;
                S.CurrentTick++;
                if (!HiddenSim) OnTickEnd(tick);
            }
        }

        /// <summary>
        /// 사건 하나가 틱 t 에 주는 효과.
        /// 발표 전: (선반영 + 기대감)을 소문/누출 구간에 나눠 반영.
        /// 발표 순간: 남은 효과 일부를 한 번에 반영하고 기대감을 되돌린다.
        /// 발표 후: 나머지와 차익 실현 되돌림을 후속 기사까지 나눠 반영.
        /// </summary>
        void EventEffect(EventInstance e, int t, out double jump, out double grad)
        {
            jump = 0; grad = 0;
            int start = e.StartTick;
            if (t >= start && t < e.OfficialTick)
            {
                int span = Math.Max(1, e.OfficialTick - start);
                grad = (e.PricedIn * e.Impact + e.Hype) / span;
            }
            else if (t == e.OfficialTick)
            {
                jump = (1 - e.PricedIn) * e.Impact * e.JumpShare - e.Hype;
            }
            else if (t > e.OfficialTick && t <= e.FollowTick)
            {
                int span = Math.Max(1, e.FollowTick - e.OfficialTick);
                grad = ((1 - e.PricedIn) * e.Impact * (1 - e.JumpShare) + e.Reversal) / span;
            }
        }

        double EventAttention(EventInstance e, int t)
        {
            if (t < e.StartTick || t > e.FollowTick) return 1;
            if (t < e.OfficialTick)
            {
                double p = (t - e.StartTick) / (double)Math.Max(1, e.OfficialTick - e.StartTick);
                return 1 + e.Attention * 0.5 * p;
            }
            if (t == e.OfficialTick) return 1 + e.Attention * 2.5;
            double q = (t - e.OfficialTick) / (double)Math.Max(1, e.FollowTick - e.OfficialTick);
            return 1 + e.Attention * 1.5 * (1 - q);
        }

        // ───────────────────────── 사건 배정 ─────────────────────────

        static Dictionary<string, object> OutcomeTuning(string outcomeId)
        {
            return Cfg.Get("balance", "event_outcomes." + outcomeId) as Dictionary<string, object>;
        }

        /// <summary>
        /// [windowStart, windowEnd) 에 공식 발표가 오는 사건을 만든다. 소문은 minRumorTick 이후에만 둔다.
        /// balance.json 에 가중치가 없는 결과는 배정하지 않는다 (data 의 null 을 0이나 균등으로 취급하지 않음).
        /// </summary>
        void GenerateEvents(int windowStart, int windowEnd, int minRumorTick)
        {
            const string B = "balance";
            int count = eventRng.RangeInt(Cfg.I(B, "events.per_day_min", 1), Cfg.I(B, "events.per_day_max", 2));
            var companies = new List<string>(S.Tradable);
            for (int i = companies.Count - 1; i > 0; i--)
            {
                int j = eventRng.RangeInt(0, i);
                string tmp = companies[i]; companies[i] = companies[j]; companies[j] = tmp;
            }

            double offMin, offMax, leadMin, leadMax, leakMin, leakMax, folMin, folMax;
            Cfg.Range(B, "events.official_offset_ticks", 20, 160, out offMin, out offMax);
            Cfg.Range(B, "events.rumor_lead_ticks", 30, 200, out leadMin, out leadMax);
            Cfg.Range(B, "events.leak_window_ticks", 5, 25, out leakMin, out leakMax);
            Cfg.Range(B, "events.follow_up_delay_ticks", 20, 80, out folMin, out folMax);
            int gap = Cfg.I(B, "events.min_gap_between_company_events", 10);
            int span = Math.Max(1, windowEnd - windowStart);

            int made = 0;
            foreach (var cid in companies)
            {
                if (made >= count) break;
                int official = windowStart + eventRng.RangeInt((int)Math.Min(offMin, span - 1), (int)Math.Min(offMax, span - 1));
                bool hasRumor = eventRng.Next() < Cfg.D(B, "events.rumor_chance", 0.7);
                int rumor = official - eventRng.RangeInt((int)leadMin, (int)leadMax);
                if (rumor < minRumorTick) rumor = minRumorTick;
                if (official - rumor < 3) hasRumor = false;
                int leak = official - eventRng.RangeInt((int)leakMin, (int)leakMax);
                if (leak < minRumorTick) leak = Math.Min(official, minRumorTick);
                int follow = official + eventRng.RangeInt((int)folMin, (int)folMax);
                int start = hasRumor ? rumor : leak;

                bool overlap = false;
                foreach (var ex in S.Events)
                {
                    if (ex.CompanyId != cid) continue;
                    if (start <= ex.FollowTick + gap && follow + gap >= ex.StartTick) { overlap = true; break; }
                }
                if (overlap) continue;

                var outcomes = new List<Dictionary<string, object>>();
                var weights = new List<double>();
                foreach (var o in Cfg.Items("events"))
                {
                    var ev = o as Dictionary<string, object>;
                    if (ev == null) continue;
                    bool allowed = false;
                    foreach (var c in J.A(ev, "company_ids")) if (J.ToS(c, "") == cid) { allowed = true; break; }
                    if (!allowed) continue;
                    var tune = OutcomeTuning(J.S(ev, "id", ""));
                    double w = J.D(tune, "weight", 0);
                    if (w <= 0) continue;
                    outcomes.Add(ev);
                    weights.Add(w);
                }
                int pick = eventRng.PickWeighted(weights);
                if (pick < 0) continue;
                var chosen = outcomes[pick];
                string outcomeId = J.S(chosen, "id", "");
                var t = OutcomeTuning(outcomeId);
                double iMin, iMax;
                J.Range(t, "impact", -0.05, 0.05, out iMin, out iMax);
                double impact = eventRng.Range(iMin, iMax);

                double piMin, piMax;
                if (hasRumor) Cfg.Range(B, "events.priced_in_with_rumor", 0.3, 0.8, out piMin, out piMax);
                else Cfg.Range(B, "events.priced_in_without_rumor", 0.0, 0.35, out piMin, out piMax);
                double pricedIn = eventRng.Range(piMin, piMax);

                double hype = 0;
                if (hasRumor && eventRng.Next() < Cfg.D(B, "events.rumor_hype_chance", 0.6))
                {
                    double hMin, hMax;
                    Cfg.Range(B, "events.rumor_hype", 0.01, 0.06, out hMin, out hMax);
                    hype = eventRng.Range(hMin, hMax);
                }
                double reversal = 0;
                if (impact > 0)
                {
                    double rMin, rMax;
                    Cfg.Range(B, "events.sell_the_news_ratio", 0.0, 0.5, out rMin, out rMax);
                    reversal = -impact * pricedIn * eventRng.Range(rMin, rMax);
                }
                double jsMin, jsMax;
                Cfg.Range(B, "events.official_jump_share", 0.4, 0.8, out jsMin, out jsMax);

                var inst = new EventInstance
                {
                    Id = "EVT" + (++S.EventSerial).ToString("0000"),
                    CompanyId = cid,
                    OutcomeId = outcomeId,
                    Topic = J.S(chosen, "topic", ""),
                    Tone = J.S(t, "tone", "neutral"),
                    HasRumor = hasRumor,
                    RumorTick = rumor,
                    LeakStartTick = leak,
                    OfficialTick = official,
                    FollowTick = follow,
                    Impact = impact,
                    PricedIn = pricedIn,
                    Hype = hype,
                    Reversal = reversal,
                    JumpShare = eventRng.Range(jsMin, jsMax),
                    Attention = Math.Abs(impact) * Cfg.D(B, "events.attention_per_impact", 12) + (hasRumor ? 0.3 : 0)
                };
                S.Events.Add(inst);
                made++;

                if (hasRumor)
                {
                    int n = eventRng.RangeInt(Cfg.I(B, "feed.rumor_posts_per_event_min", 1), Cfg.I(B, "feed.rumor_posts_per_event_max", 2));
                    int tk = rumor;
                    for (int k = 0; k < n && tk < official; k++)
                    {
                        AddPending(new PendingPub { Tick = tk, Type = "rumor_post", EventId = inst.Id });
                        tk += eventRng.RangeInt(5, Math.Max(6, (official - rumor) / Math.Max(1, n)));
                    }
                }
                AddPending(new PendingPub { Tick = official, Type = "official_news", EventId = inst.Id });
                AddPending(new PendingPub { Tick = follow, Type = "follow_news", EventId = inst.Id });
                if (hasRumor)
                {
                    AddPending(new PendingPub { Tick = official + eventRng.RangeInt(2, 12), Type = "fact_comments", EventId = inst.Id });
                    AddPending(new PendingPub { Tick = follow + eventRng.RangeInt(1, 10), Type = "fact_comments", EventId = inst.Id });
                }
            }
            SyncRngCounters();
        }

        void AddPending(PendingPub p)
        {
            int idx = S.Pending.Count;
            while (idx > 0 && S.Pending[idx - 1].Tick > p.Tick) idx--;
            S.Pending.Insert(idx, p);
        }

        public EventInstance EventById(string id)
        {
            if (id == null) return null;
            foreach (var e in S.Events) if (e.Id == id) return e;
            return null;
        }

        /// <summary>차트에 그릴 봉 목록 (완성 봉 + 진행 중 봉).</summary>
        public List<Candle> ChartCandles(string id, int timeframe)
        {
            var res = new List<Candle>();
            StockState st;
            if (!S.Stocks.TryGetValue(id, out st)) return res;
            var src = new List<Candle>(st.Candles);
            if (st.Live != null) src.Add(st.Live);
            if (timeframe <= 1) return src;
            Candle cur = null;
            int curBucket = int.MinValue;
            foreach (var c in src)
            {
                int bucket = (int)Math.Floor(c.Tick / (double)timeframe);
                if (cur == null || bucket != curBucket)
                {
                    if (cur != null) res.Add(cur);
                    cur = c.Clone();
                    curBucket = bucket;
                }
                else
                {
                    cur.H = Math.Max(cur.H, c.H);
                    cur.L = Math.Min(cur.L, c.L);
                    cur.C = c.C;
                    cur.V += c.V;
                }
            }
            if (cur != null) res.Add(cur);
            return res;
        }
    }
}
