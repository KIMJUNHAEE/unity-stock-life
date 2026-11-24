using System;
using System.Collections.Generic;

namespace StockGame
{
    /// <summary>1분봉 하나. 저장할 때는 [tick, o, h, l, c, v] 배열로 압축한다.</summary>
    public class Candle : ICompactJson
    {
        public int Tick;
        public double O, H, L, C;
        public long V;

        public object ToCompact()
        {
            return new List<object> { (double)Tick, O, H, L, C, (double)V };
        }

        public void FromCompact(object data)
        {
            var l = data as List<object>;
            if (l == null || l.Count < 6) return;
            Tick = (int)J.ToD(l[0], 0);
            O = J.ToD(l[1], 0); H = J.ToD(l[2], 0); L = J.ToD(l[3], 0); C = J.ToD(l[4], 0);
            V = (long)J.ToD(l[5], 0);
        }

        public Candle Clone() { return new Candle { Tick = Tick, O = O, H = H, L = L, C = C, V = V }; }
    }

    public class StockState
    {
        public string Id;
        public double Price;
        public double Anchor;          // 사건 영향이 누적된 기준 가격 (평균 회귀 대상)
        public double VolMult = 1;     // 회차별 변동성 배율 (설정값에 곱함)
        public double VolumeMult = 1;  // 회차별 거래량 배율
        public double Beta = 1;        // 시장 전체 흐름 민감도
        public double PrevClose;       // 직전 세션 종가
        public double SessionOpen;
        public List<Candle> Candles = new List<Candle>();
        public Candle Live;
    }

    /// <summary>회차 내 사건 인스턴스. 결과 유형(events.json)과 별개로 회사·시각이 붙는다.</summary>
    public class EventInstance
    {
        public string Id;
        public string CompanyId;
        public string OutcomeId;
        public string Topic;
        public string Tone;          // positive / neutral / negative (balance.json)
        public bool HasRumor;
        public int RumorTick;
        public int LeakStartTick;
        public int OfficialTick;
        public int FollowTick;
        public double Impact;        // 최종 로그 수익 효과
        public double PricedIn;      // 공식 발표 전 반영 비율
        public double Hype;          // 소문 기대감 (발표 시 되돌림)
        public double Reversal;      // 발표 후 차익 실현 되돌림
        public double JumpShare;     // 발표 순간 반영 비율
        public double Attention;     // 거래량 관심도
        public bool OfficialPublished;
        public bool FollowPublished;
        public List<string> RumorPostIds = new List<string>();
        public List<string> NewsIds = new List<string>();

        public int StartTick { get { return HasRumor ? RumorTick : LeakStartTick; } }
    }

    public class FeedComment
    {
        public string TemplateId;
        public string Text;
        public string Nick;
        public int Tick;
        public int Likes;
    }

    public class FeedItem
    {
        public string Id;
        public string Kind;          // news / post
        public string TemplateId;
        public string Title;
        public string Body;
        public string CompanyId;     // 관련 종목 (일반 글은 null)
        public string EventId;       // 내부 연결용, 화면에 표시하지 않음
        public string Phase;         // rumor / official / follow_up / any
        public string Category;
        public string Author;        // 매체명 또는 닉네임
        public int Tick;
        public int Views;
        public int Likes;
        public bool Read;
        public List<FeedComment> Comments = new List<FeedComment>();
    }

    public class PendingPub
    {
        public int Tick;
        public string Type;          // rumor_post, official_news, follow_news, fact_comments, general_post, general_comment
        public string EventId;
        public string ParentId;
    }

    public class Holding
    {
        public long Qty;
        public double AvgCost;       // 매수 수수료 포함 주당 원가
        public string EarlyRumorEvent;
        public string NewsChaseEvent;
    }

    public class TradeRecord
    {
        public int Day;
        public int Tick;
        public string CompanyId;
        public string Side;          // buy / sell
        public long Qty;
        public double Price;
        public long Amount;
        public long Fee;
        public long Realized;        // 매도 시 실현 순손익
    }

    public class Loan
    {
        public long Principal;
        public long Interest;
        public int TakenDay;
        public int DueDay;
        public bool Overdue;
        public long Owed { get { return Principal + Interest; } }
    }

    /// <summary>하루 기록. 컷신·결산 판정에 쓰인다.</summary>
    public class DayRecord
    {
        public int Day;
        public bool JobDone;
        public bool JobSkipped;
        public long JobPay;
        public long JobGarnish;
        public bool TradingStarted;
        public long StartCapital;
        public long Realized;
        public long FeesPaid;
        public int Trades;
        public int Buys;
        public int Sells;
        public bool TookLoan;
        public long LoanAmount;
        public bool HadLoanBefore;
        public long RepaidLoan;
        public long RepaidStartingDebt;
        public long InterestCharged;
        public long AutoRepaid;
        public double MinCondition = 100;
        public int TicksWatched;
        public bool EndedEarly;
        public long RumorEarlyProfit;
        public long FalseRumorLoss;
        public long NewsChaseLoss;
        public int ArticlesRead;
        public int PostsRead;
        public long FurnitureSpent;
        public bool SettlementPrepared;
        public bool Settled;
        public long EndInvestable;
        public long EndNetFinancial;
        public Dictionary<string, int> ItemUses = new Dictionary<string, int>();
        public List<string> CutscenesUnlocked = new List<string>();
        public string CutscenePlayed;
    }

    /// <summary>한 회차의 전체 진행 상태. 그대로 저장 파일이 된다.</summary>
    public class GameState
    {
        public int SaveVersion = 1;
        public ulong Seed;
        public int Day;
        public string Phase = "morning";   // morning / evening / night
        public int TicksPerDay;
        public int Substeps;
        public int SessionStartMinute;
        public int CurrentTick;
        public int SubIndex;
        public List<int> SessionStarts = new List<int>();

        public long Cash;
        public long StartingDebt;
        public Loan Loan;
        public Dictionary<string, Holding> Holdings = new Dictionary<string, Holding>();
        public List<TradeRecord> Trades = new List<TradeRecord>();

        public List<string> Tradable = new List<string>();
        public Dictionary<string, StockState> Stocks = new Dictionary<string, StockState>();
        public List<EventInstance> Events = new List<EventInstance>();
        public int EventSerial;
        public long EventRngCounter;

        public List<FeedItem> Feed = new List<FeedItem>();
        public List<PendingPub> Pending = new List<PendingPub>();
        public Dictionary<string, int> TemplateLastUsed = new Dictionary<string, int>();
        public long ContentRngCounter;
        public int FeedSerial;
        public Dictionary<string, int> ReadRumorTick = new Dictionary<string, int>();
        public Dictionary<string, int> ReadNewsTick = new Dictionary<string, int>();

        public double Condition = 100;
        public int DrainSuppressUntil;
        public int DrainReduceUntil;
        public double DrainReduceMult = 1;
        public int NextBlinkTick;
        public bool WarnedSleepy;
        public bool WarnedTired;

        public DayRecord Today = new DayRecord();
        public List<DayRecord> History = new List<DayRecord>();
        public long CumulativeRealized;
        public int ConsecutiveProfitNights;
        public bool EverTookLoan;

        public List<string> OwnedItems = new List<string>();
        public Dictionary<string, string> Equipped = new Dictionary<string, string>();
        public List<string> StoryUnlocked = new List<string>();
        public Dictionary<string, string> StoryChoices = new Dictionary<string, string>();
        public List<string> UnlockedThemes = new List<string>();
        public string ActiveTheme;
        public List<string> Flags = new List<string>();
        public List<string> Watchlist = new List<string>();

        public bool HasFlag(string f) { return Flags.Contains(f); }
        public void SetFlag(string f) { if (!Flags.Contains(f)) Flags.Add(f); }

        public int SessionStart { get { return SessionStarts.Count > Day ? SessionStarts[Day] : CurrentTick; } }
        public int SessionEnd { get { return SessionStart + TicksPerDay; } }
    }

    /// <summary>회차와 무관하게 유지되는 컷신 수집 기록.</summary>
    public class GalleryData
    {
        public Dictionary<string, GalleryEntry> Entries = new Dictionary<string, GalleryEntry>();
    }

    public class GalleryEntry
    {
        public string Id;
        public string FirstUnlockedAt;   // 실제 날짜
        public int FirstDay;
        public bool Seen;
        public int TimesPlayed;
    }

    public class SettingsData
    {
        public bool Reactions = true;
        public bool AutoSkipSeenCutscenes = false;
        public bool SkipDefaultBedtime = false;
        public bool ShowExactConditions = false;
        public float TextSpeed = 1f;
        public bool ShowNewsMarkers = true;
        public bool OrderConfirm = false;
    }
}
