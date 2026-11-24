using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 게임 규칙의 중심. UI 와 분리되어 있으며 상태는 GameState 하나에 모인다.
    /// 시장·피드·매매·컨디션·결산은 partial 파일로 나뉘어 있다.
    /// </summary>
    public partial class Game
    {
        public static Game I;

        public GameState S;
        public GalleryData Gallery = new GalleryData();
        public SettingsData Settings = new SettingsData();

        /// <summary>플레이어가 누른 일시정지 (저장하지 않음).</summary>
        public bool UserPaused;
        /// <summary>숨김 시뮬레이션(새 게임의 과거 차트 생성) 중에는 컨디션·반응을 처리하지 않는다.</summary>
        public bool HiddenSim;

        public event Action<string> OnToast;
        public event Action<string> OnReaction;     // dialogue.json reactions 의 트리거 키
        public event Action<float> OnBlink;          // 눈 감김 지속 시간(초)

        StreamRng eventRng;
        StreamRng contentRng;
        float subAccum;

        public static string SaveDir { get { return Path.Combine(Application.persistentDataPath, "saves"); } }
        static string RunPath { get { return Path.Combine(SaveDir, "run.json"); } }
        static string GalleryPath { get { return Path.Combine(SaveDir, "gallery.json"); } }
        static string SettingsPath { get { return Path.Combine(SaveDir, "settings.json"); } }

        public void Toast(string msg) { if (OnToast != null && !HiddenSim) OnToast(msg); }
        public void React(string trigger) { if (OnReaction != null && !HiddenSim) OnReaction(trigger); }

        // ───────────────────────── 새 게임 / 저장 ─────────────────────────

        public void NewGame()
        {
            S = new GameState();
            long fixedSeed = Cfg.L("balance", "run.fixed_seed", 0);
            S.Seed = fixedSeed != 0 ? (ulong)fixedSeed : Hash.Mix((ulong)DateTime.UtcNow.Ticks ^ (ulong)Guid.NewGuid().GetHashCode());
            S.TicksPerDay = Math.Max(30, Cfg.I("balance", "market.session_length_ticks", 180));
            S.Substeps = Mathf.Clamp(Cfg.I("balance", "market.substeps_per_tick", 5), 1, 20);
            S.SessionStartMinute = Cfg.I("balance", "market.session_start_minute", 1260);
            S.Cash = Cfg.L("balance", "economy.starting_cash", 300000);
            S.StartingDebt = Cfg.L("balance", "debt.starting_debt", 1000000000);
            S.Condition = Cfg.D("balance", "condition.start_value", 100);
            S.ActiveTheme = Cfg.S("theme", "default_theme", "pastel_day");
            S.UnlockedThemes.Add(S.ActiveTheme);

            foreach (var o in Cfg.A("shop", "starting_items"))
            {
                string id = J.ToS(o, null);
                var item = ShopItem(id);
                if (item == null) continue;
                S.OwnedItems.Add(id);
                S.Equipped[J.S(item, "slot", "")] = id;
            }

            AttachRng();
            InitMarket();

            // 0일차: 시작 전 과거 차트·글을 숨김 상태로 생성한다.
            S.Day = 0;
            S.SessionStarts.Add(0);
            S.CurrentTick = 0;
            S.Today = new DayRecord { Day = 0 };
            GenerateEvents(0, S.TicksPerDay, 0);
            GenerateEvents(S.TicksPerDay, S.TicksPerDay * 2, 0);
            ScheduleGeneralPosts(0, S.TicksPerDay);
            HiddenSim = true;
            int guard = 0;
            while (S.CurrentTick < S.TicksPerDay && guard++ < 100000) StepSub();
            HiddenSim = false;
            foreach (var it in S.Feed) it.Read = false;

            BeginDay(1);
            S.SetFlag("intro_pending");
            Save();
        }

        void AttachRng()
        {
            eventRng = new StreamRng(Hash.H(S.Seed, Hash.Str("events")), S.EventRngCounter);
            contentRng = new StreamRng(Hash.H(S.Seed, Hash.Str("content")), S.ContentRngCounter);
        }

        void SyncRngCounters()
        {
            if (eventRng != null) S.EventRngCounter = eventRng.Counter;
            if (contentRng != null) S.ContentRngCounter = contentRng.Counter;
        }

        /// <summary>새 날의 시작. 세션 시작 틱을 기록하고 다음 날 사건을 미리 배정한다.</summary>
        void BeginDay(int day)
        {
            if (S.Today != null && S.Today.Day > 0) S.History.Add(S.Today);
            S.Day = day;
            while (S.SessionStarts.Count <= day) S.SessionStarts.Add(S.CurrentTick);
            S.SessionStarts[day] = S.CurrentTick;
            foreach (var st in S.Stocks.Values) { st.PrevClose = st.Price; st.SessionOpen = st.Price; }
            S.Today = new DayRecord { Day = day, HadLoanBefore = S.Loan != null && S.Loan.Owed > 0 };
            int start = S.CurrentTick;
            ScheduleGeneralPosts(start, start + S.TicksPerDay);
            GenerateEvents(start + S.TicksPerDay, start + S.TicksPerDay * 2, start);
            S.Phase = "morning";
            S.NextBlinkTick = 0;
            UserPaused = false;
            subAccum = 0;
        }

        public void StartEvening()
        {
            if (S.Phase == "morning") S.Phase = "evening";
            Save();
        }

        /// <summary>잠자기: 결산이 끝난 밤에만 다음 날로 넘어간다.</summary>
        public void Sleep()
        {
            if (S.Phase != "night") return;
            S.Condition = Math.Min(Cfg.D("balance", "condition.max_value", 100), Cfg.D("balance", "condition.sleep_restore", 100));
            S.DrainSuppressUntil = 0;
            S.DrainReduceUntil = 0;
            BeginDay(S.Day + 1);
            React("morning");
            Save();
        }

        public bool HasSave() { return File.Exists(RunPath); }

        public void Save()
        {
            if (S == null) return;
            try
            {
                SyncRngCounters();
                Directory.CreateDirectory(SaveDir);
                string json = MiniJson.Serialize(JsonMapper.ToTree(S), false);
                string tmp = RunPath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(RunPath)) File.Delete(RunPath);
                File.Move(tmp, RunPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[StockGame] 저장 실패: " + ex.Message);
                Toast("저장 실패: " + ex.Message);
            }
        }

        public bool Load()
        {
            try
            {
                if (!File.Exists(RunPath)) return false;
                var tree = MiniJson.Parse(File.ReadAllText(RunPath));
                S = JsonMapper.FromTree<GameState>(tree);
                if (S == null) return false;
                if (S.Today == null) S.Today = new DayRecord { Day = S.Day };
                AttachRng();
                UserPaused = true;
                subAccum = 0;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[StockGame] 불러오기 실패: " + ex.Message);
                Toast("불러오기 실패: " + ex.Message);
                return false;
            }
        }

        public void DeleteRun()
        {
            try { if (File.Exists(RunPath)) File.Delete(RunPath); } catch (Exception) { }
        }

        public void LoadMeta()
        {
            try
            {
                if (File.Exists(GalleryPath)) Gallery = JsonMapper.FromTree<GalleryData>(MiniJson.Parse(File.ReadAllText(GalleryPath))) ?? new GalleryData();
                if (File.Exists(SettingsPath)) Settings = JsonMapper.FromTree<SettingsData>(MiniJson.Parse(File.ReadAllText(SettingsPath))) ?? new SettingsData();
            }
            catch (Exception ex) { Debug.LogWarning("[StockGame] 기록 불러오기 실패: " + ex.Message); }
        }

        public void SaveMeta()
        {
            try
            {
                Directory.CreateDirectory(SaveDir);
                File.WriteAllText(GalleryPath, MiniJson.Serialize(JsonMapper.ToTree(Gallery), true));
                File.WriteAllText(SettingsPath, MiniJson.Serialize(JsonMapper.ToTree(Settings), true));
            }
            catch (Exception ex) { Debug.LogWarning("[StockGame] 기록 저장 실패: " + ex.Message); }
        }

        // ───────────────────────── 자산 계산 ─────────────────────────

        public double PriceOf(string id)
        {
            StockState st;
            return S.Stocks.TryGetValue(id, out st) ? st.Price : 0;
        }

        public long HoldingsValue()
        {
            double v = 0;
            foreach (var kv in S.Holdings) v += kv.Value.Qty * PriceOf(kv.Key);
            return (long)Math.Round(v);
        }

        public long UnrealizedPnl()
        {
            double v = 0;
            foreach (var kv in S.Holdings) v += kv.Value.Qty * (PriceOf(kv.Key) - kv.Value.AvgCost);
            return (long)Math.Round(v);
        }

        public long InvestableAssets() { return S.Cash + HoldingsValue(); }
        public long LoanOwed() { return S.Loan == null ? 0 : S.Loan.Owed; }
        public long TotalDebt() { return LoanOwed() + S.StartingDebt; }
        public long NetFinancialAssets() { return InvestableAssets() - TotalDebt(); }

        public int MinuteOfTick(int tick)
        {
            int day = DayOfTick(tick);
            int start = day < S.SessionStarts.Count ? S.SessionStarts[day] : 0;
            return S.SessionStartMinute + (tick - start);
        }

        public int DayOfTick(int tick)
        {
            for (int d = S.SessionStarts.Count - 1; d >= 0; d--)
                if (tick >= S.SessionStarts[d]) return d;
            return 0;
        }

        public string ClockOfTick(int tick) { return Fmt.Clock(MinuteOfTick(tick)); }
        public string NowClock() { return ClockOfTick(S.CurrentTick); }

        public static Dictionary<string, object> Company(string id) { return Cfg.ItemById("companies", id); }
        public static string CompanyName(string id) { return J.S(Company(id), "stock_name", id); }
    }
}
