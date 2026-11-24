using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 게임 수치·문구·이미지 설정 에셋. Assets/Resources/GameConfig.asset 을 클릭하면 Inspector 에서 고칠 수 있다.
    /// 플레이 중에 바꿔도 바로 반영된다. (플레이 중 바꾼 값도 에셋에 남는다)
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "StockGame/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Label("경제·시장·대출·컨디션")] public BalanceConfig balance = new BalanceConfig();
        [Label("편의점 알바")] public JobConfig job = new JobConfig();
        [Label("밤 컷신")] public CutsceneConfig cutscenes = new CutsceneConfig();
        [Label("서하 대사·튜토리얼")] public DialogueConfig dialogue = new DialogueConfig();
        [Label("가구 카탈로그")] public ShopConfig shop = new ShopConfig();
        [Label("테마·색·앱")] public ThemeConfig theme = new ThemeConfig();
        [Label("가상 사이트·닉네임")] public NamesConfig names = new NamesConfig();

        public static event Action Changed;
        static GameConfig instance;

        public static GameConfig Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameConfig>("GameConfig");
                return instance;
            }
        }

        void OnValidate()
        {
            if (Changed != null) Changed();
        }

        public static void NotifyChanged() { if (Changed != null) Changed(); }

        /// <summary>규칙 코드(Cfg)가 읽는 트리로 섹션별 변환.</summary>
        public Dictionary<string, object> Trees()
        {
            return new Dictionary<string, object>
            {
                { "balance", ConfigTree.ToTree(balance) },
                { "job", ConfigTree.ToTree(job) },
                { "cutscenes", ConfigTree.ToTree(cutscenes) },
                { "dialogue", ConfigTree.ToTree(dialogue) },
                { "shop", ConfigTree.ToTree(shop) },
                { "theme", ConfigTree.ToTree(theme) },
                { "names", ConfigTree.ToTree(names) },
            };
        }

        // ── UI 편의 조회 ──
        public Sprite PortraitOf(string expression)
        {
            foreach (var e in dialogue.expressions) if (e.key == expression && e.portrait != null) return e.portrait;
            foreach (var e in dialogue.expressions) if (e.key == "normal") return e.portrait;
            return null;
        }

        public Sprite StandingOf(string expression)
        {
            foreach (var e in dialogue.expressions) if (e.key == expression && e.standing != null) return e.standing;
            foreach (var e in dialogue.expressions) if (e.key == "normal") return e.standing;
            return null;
        }

        public ShopItemCfg ShopItem(string id)
        {
            foreach (var i in shop.items) if (i.id == id) return i;
            return null;
        }

        public SceneCfg Scene(string id)
        {
            if (id == null) return null;
            if (cutscenes.defaultScene != null && cutscenes.defaultScene.id == id) return cutscenes.defaultScene;
            foreach (var s in cutscenes.scenes) if (s.id == id) return s;
            return null;
        }

        public ThemeCfg Theme(string key)
        {
            foreach (var t in theme.themes) if (t.key == key) return t;
            return null;
        }

        public AppCfg App(string key)
        {
            foreach (var a in theme.apps) if (a.key == key) return a;
            return null;
        }
    }

    // ───────────────────────── 조건식 ─────────────────────────

    public enum CompareOp { GreaterOrEqual, Greater, LessOrEqual, Less, Equal, NotEqual }
    public enum MatchMode { All, Any }

    [Serializable]
    public class StatCond
    {
        [StatName, Label("값 이름 (stat)")] public string stat = "trades_today";
        [Label("비교")] public CompareOp op = CompareOp.GreaterOrEqual;
        [Label("기준값")] public double value = 1;

        public static string OpText(CompareOp op)
        {
            switch (op)
            {
                case CompareOp.Greater: return ">";
                case CompareOp.LessOrEqual: return "<=";
                case CompareOp.Less: return "<";
                case CompareOp.Equal: return "==";
                case CompareOp.NotEqual: return "!=";
            }
            return ">=";
        }

        public static CompareOp ParseOp(string s)
        {
            switch (s)
            {
                case ">": return CompareOp.Greater;
                case "<=": return CompareOp.LessOrEqual;
                case "<": return CompareOp.Less;
                case "==": return CompareOp.Equal;
                case "!=": return CompareOp.NotEqual;
            }
            return CompareOp.GreaterOrEqual;
        }
    }

    /// <summary>조건 목록. 비어 있으면 "조건 없음"(null)으로 변환된다.</summary>
    [Serializable]
    public class ConditionSet : IConfigTree
    {
        [Label("모두 만족 / 하나라도")] public MatchMode mode = MatchMode.All;
        [Label("조건")] public List<StatCond> items = new List<StatCond>();

        public object ToTree()
        {
            if (items == null || items.Count == 0) return null;
            var list = new List<object>();
            foreach (var c in items)
                list.Add(new Dictionary<string, object> { { "stat", c.stat }, { "op", StatCond.OpText(c.op) }, { "value", c.value } });
            return new Dictionary<string, object> { { mode == MatchMode.Any ? "any" : "all", list } };
        }

        public void FromTree(object tree)
        {
            items = new List<StatCond>();
            mode = MatchMode.All;
            var d = tree as Dictionary<string, object>;
            if (d == null) return;
            List<object> list = null;
            if (d.ContainsKey("all")) list = J.A(d, "all");
            else if (d.ContainsKey("any")) { list = J.A(d, "any"); mode = MatchMode.Any; }
            else if (d.ContainsKey("stat")) list = new List<object> { d };
            if (list == null) return;
            foreach (var o in list)
                items.Add(new StatCond { stat = J.S(o, "stat", ""), op = StatCond.ParseOp(J.S(o, "op", ">=")), value = J.D(o, "value", 0) });
        }
    }

    // ───────────────────────── 경제·시장 ─────────────────────────

    [Serializable]
    public class BalanceConfig
    {
        public RunCfg run = new RunCfg();
        [Label("경제")] public EconomyCfg economy = new EconomyCfg();
        [Label("시작 부채")] public DebtCfg debt = new DebtCfg();
        [Label("매매")] public TradingCfg trading = new TradingCfg();
        [Label("시장")] public MarketCfg market = new MarketCfg();
        [Label("사건 배정")] public EventsCfg events = new EventsCfg();
        [AsDict, Label("사건 결과별 확률·영향", "weight 0 = 나오지 않음. impact 는 로그 수익 (0.1 ≈ +10%)")] public List<EventOutcomeCfg> eventOutcomes = new List<EventOutcomeCfg>();
        [Label("뉴스·커뮤니티 글 수")] public FeedCfg feed = new FeedCfg();
        [Label("추가 투자 대출")] public LoanCfg loan = new LoanCfg();
        [Label("컨디션")] public ConditionCfg condition = new ConditionCfg();
        [Label("개발 옵션")] public DevCfg dev = new DevCfg();
    }

    [Serializable] public class RunCfg { [Label("고정 시드", "0 = 매번 다른 시장")] public long fixedSeed; }
    [Serializable] public class EconomyCfg { [Label("시작 현금", "새 게임부터")] public long startingCash = 300000; }
    [Serializable] public class DebtCfg
    {
        [Label("시작 부채 금액", "새 게임부터")] public long startingDebt = 1000000000;
        [Label("상환 버튼 금액")] public List<long> repayOptions = new List<long> { 10000, 50000, 100000 };
    }
    [Serializable] public class TradingCfg
    {
        [Label("수수료율", "0.001 = 0.1%")] public float feeRate = 0.001f;
        [Label("최소 수수료")] public long minFee;
        [Label("매도 세금률")] public float sellTaxRate;
    }

    [Serializable] public class CompanyOverride
    {
        [Label("회사 id")] public string key;
        [Label("시작가 (0 = 무작위)")] public float initialPrice;
        [Label("변동성 배율")] public float volatilityMult = 1;
    }

    [Serializable] public class MarketCfg
    {
        [Label("거래 가능 종목 id", "data/companies.json 의 id. 새 게임부터")] public List<string> tradableCompanyIds = new List<string>();
        [Label("종목 수 (목록이 비었을 때)")] public int tradableCompanyCount = 3;
        [Label("장 시작 시각 (분)", "1260 = 21:00")] public int sessionStartMinute = 1260;
        [Label("하루 장 길이 (분)", "새 게임부터")] public int sessionLengthTicks = 180;
        [Label("캔들 내부 단계 수", "새 게임부터")] public int substepsPerTick = 5;
        [Label("시장 1분 = 실제 몇 초", "작을수록 빠름"), Range(0.05f, 10f)] public float secondsPerTick = 1.2f;
        [Label("배속 버튼")] public List<float> speedOptions = new List<float> { 1, 2, 4 };
        [Label("시작가 범위")] public Vector2 initialPriceRange = new Vector2(1000, 8000);
        [AsDict, Label("종목별 설정")] public List<CompanyOverride> companyOverrides = new List<CompanyOverride>();
        [Label("회차별 변동성 차이")] public float volatilityRunSpread = 0.25f;
        [Label("가격 출렁임 (1분)"), Range(0f, 0.03f)] public float baseVolatilityPerTick = 0.004f;
        [Label("시장 전체 흐름 세기"), Range(0f, 0.01f)] public float marketFactorVolPerTick = 0.0015f;
        [Label("제자리로 돌아오는 힘"), Range(0f, 0.1f)] public float meanReversionPerTick = 0.01f;
        [Label("기본 상승/하락 경향")] public float driftPerTick;
        [Label("관심 집중 시 출렁임 증가")] public float attentionVolBoost = 0.35f;
        [Label("기본 거래량")] public float baseVolumePerTick = 4000;
        [Label("가격 변동 → 거래량")] public float volumeReturnSensitivity = 60;
        [Label("최저 가격")] public float minPrice = 100;
        [Label("호가 단위표", "x = 이 가격 미만, y = 단위")] public List<Vector2> tickSizes = new List<Vector2>();
    }

    [Serializable] public class EventsCfg
    {
        [Label("하루 사건 수 최소")] public int perDayMin = 1;
        [Label("하루 사건 수 최대")] public int perDayMax = 2;
        [Label("공식 발표 시점 (장 시작 후 분)")] public Vector2 officialOffsetTicks = new Vector2(20, 165);
        [Label("소문이 먼저 돌 확률"), Range(0, 1)] public float rumorChance = 0.65f;
        [Label("소문 → 발표 간격 (분)")] public Vector2 rumorLeadTicks = new Vector2(25, 200);
        [Label("소문 없을 때 미리 반영 구간 (분)")] public Vector2 leakWindowTicks = new Vector2(5, 25);
        [Label("발표 → 후속 기사 (분)")] public Vector2 followUpDelayTicks = new Vector2(20, 90);
        [Label("소문 있을 때 선반영 비율")] public Vector2 pricedInWithRumor = new Vector2(0.35f, 0.85f);
        [Label("소문 없을 때 선반영 비율")] public Vector2 pricedInWithoutRumor = new Vector2(0f, 0.4f);
        [Label("소문 기대감 확률"), Range(0, 1)] public float rumorHypeChance = 0.6f;
        [Label("소문 기대감 크기")] public Vector2 rumorHype = new Vector2(0.01f, 0.06f);
        [Label("호재 후 차익 실현 되돌림")] public Vector2 sellTheNewsRatio = new Vector2(0f, 0.6f);
        [Label("발표 순간 반영 비율")] public Vector2 officialJumpShare = new Vector2(0.4f, 0.8f);
        [Label("사건 크기 → 관심도")] public float attentionPerImpact = 12;
        [Label("같은 회사 사건 최소 간격 (분)")] public int minGapBetweenCompanyEvents = 10;
    }

    public enum Tone { positive, neutral, negative }

    [Serializable] public class EventOutcomeCfg
    {
        [Label("결과 id")] public string key;
        [Label("성격")] public Tone tone = Tone.neutral;
        [Label("배정 가중치"), Range(0, 3)] public float weight = 1;
        [Label("가격 영향 범위")] public Vector2 impact = new Vector2(-0.03f, 0.03f);
    }

    [Serializable] public class FeedCfg
    {
        [Label("사건당 소문 글 최소")] public int rumorPostsPerEventMin = 1;
        [Label("사건당 소문 글 최대")] public int rumorPostsPerEventMax = 2;
        [Label("하루 일반 글 최소")] public int generalPostsPerDayMin = 4;
        [Label("하루 일반 글 최대")] public int generalPostsPerDayMax = 7;
        [Label("글당 일반 댓글 최소")] public int generalCommentsPerPostMin = 1;
        [Label("글당 일반 댓글 최대")] public int generalCommentsPerPostMax = 3;
        [Label("게시글 조회수 범위")] public Vector2 postViews = new Vector2(30, 1500);
        [Label("기사 조회수 범위")] public Vector2 newsViews = new Vector2(300, 8000);
    }

    [Serializable] public class LoanCfg
    {
        [Label("대출 사용")] public bool enabled = true;
        [Label("대출 한도")] public long limit = 500000;
        [Label("빌릴 금액 버튼")] public List<long> amountOptions = new List<long> { 100000, 300000, 500000 };
        [Label("상환 버튼 금액")] public List<long> repayOptions = new List<long> { 10000, 50000, 100000 };
        [Label("하루 이자율", "0.01 = 1%")] public float dailyInterestRate = 0.01f;
        [Label("하루 이자 상한")] public long dailyInterestCap = 10000;
        [Label("누적 이자 상한")] public long maxAccumulatedInterest = 100000;
        [Label("만기 (일)")] public int termDays = 5;
        [Label("거래 시작 전에만 대출")] public bool onlyBeforeTradingStarts = true;
        [Label("이 현금 미만일 때만 대출", "0 = 제한 없음")] public long cashThreshold = 150000;
        [Label("연체 시 알바 수입 상환 비율"), Range(0, 1)] public float overdueJobIncomeRatio = 0.3f;
    }

    [Serializable] public class StageCfg : IConfigTree
    {
        [Label("단계 이름")] public string name = "양호";
        [Label("이 값 이상이면 이 단계")] public float min = 70;
        [Label("가장자리 흐림"), Range(0, 1)] public float vignette;
        [Label("눈 감김 있음")] public bool hasBlink;
        [Label("눈 감김 간격 (분)")] public Vector2 blinkIntervalTicks = new Vector2(18, 30);
        [Label("눈 감김 길이 (초)")] public Vector2 blinkDurationSeconds = new Vector2(0.5f, 1f);
        [Label("안내 문구"), TextArea] public string effectText = "";

        public object ToTree()
        {
            var d = new Dictionary<string, object>
            {
                { "name", name }, { "min", (double)min }, { "vignette", (double)vignette }, { "effect_text", effectText }
            };
            if (hasBlink)
                d["blink"] = new Dictionary<string, object>
                {
                    { "interval_ticks", ConfigTree.ToTree(blinkIntervalTicks) },
                    { "duration_seconds", ConfigTree.ToTree(blinkDurationSeconds) }
                };
            return d;
        }

        public void FromTree(object tree)
        {
            name = J.S(tree, "name", name);
            min = (float)J.D(tree, "min", min);
            vignette = (float)J.D(tree, "vignette", 0);
            effectText = J.S(tree, "effect_text", "");
            var b = J.Get(tree, "blink");
            hasBlink = b != null;
            if (hasBlink)
            {
                double a, c;
                J.Range(b, "interval_ticks", 18, 30, out a, out c); blinkIntervalTicks = new Vector2((float)a, (float)c);
                J.Range(b, "duration_seconds", 0.5, 1, out a, out c); blinkDurationSeconds = new Vector2((float)a, (float)c);
            }
        }
    }

    [Serializable] public class RecoveryItemCfg
    {
        [Label("항목 id", "snack / meal / energy_drink")] public string key;
        [Label("이름")] public string name;
        [Label("가격")] public long price;
        [Label("즉시 회복량")] public float restore;
        [Label("하룻밤 사용 제한")] public int limitPerNight = 3;
        [Label("설명")] public string description;
        [Label("소모 감소 지속 (분)")] public int reduceDrainTicks;
        [Label("소모 배율 (감소 중)")] public float reduceDrainMult = 1;
        [Label("소모 정지 (분)")] public int suppressDrainTicks;
        [Label("연속 사용 효과 감소", "1 = 감소 없음")] public float repeatDecay = 1;
    }

    [Serializable] public class ConditionCfg
    {
        [Label("시작 컨디션")] public float startValue = 100;
        [Label("최대 컨디션")] public float maxValue = 100;
        [Label("시장 1분당 소모"), Range(0, 2)] public float drainPerTick = 0.3f;
        [Label("수면 후 컨디션")] public float sleepRestore = 100;
        [Label("첫 눈 감김까지 (분)")] public int firstBlinkDelayTicks = 6;
        [Label("눈 감고 뜨는 속도")] public float blinkCloseFraction = 0.18f;
        [Label("컨디션 단계", "위에서부터 큰 값 순서")] public List<StageCfg> stages = new List<StageCfg>();
        [AsDict, Label("회복 수단")] public List<RecoveryItemCfg> items = new List<RecoveryItemCfg>();
    }

    [Serializable] public class DevCfg
    {
        [Label("data 파일 변경 확인 간격 (초)")] public float hotReloadPollSeconds = 1;
        [Label("F12 디버그 패널 허용", "출시 빌드에서는 끄기")] public bool debugPanel = true;
    }

    // ───────────────────────── 알바 ─────────────────────────

    [Serializable] public class JobItemCfg
    {
        [Label("상품 이름")] public string name;
        [Label("가격")] public long price = 1000;
        [Label("카드 색")] public Color color = Color.white;
        [Label("이미지")] public Sprite image;
        [Label("바코드 표시")] public string barcode;
    }

    [Serializable] public class PaymentCfg
    {
        [Label("id", "cash 이면 거스름돈 단계")] public string id = "card";
        [Label("버튼 이름")] public string label = "카드";
        [Label("손님 대사")] public List<string> lines = new List<string>();
    }

    [Serializable] public class CustomerCfg
    {
        [Label("손님 이름")] public string name;
        [Label("이미지")] public Sprite image;
    }

    [Serializable] public class JobLinesCfg
    {
        [Label("손님 인사")] public List<string> greetings = new List<string>();
        [Label("응대 성공")] public List<string> thanks = new List<string>();
        [Label("결제 방법 틀림")] public List<string> wrongPayment = new List<string>();
        [Label("거스름돈 틀림")] public List<string> wrongChange = new List<string>();
        [Label("손님이 떠남")] public List<string> customerLeft = new List<string>();
    }

    [Serializable] public class JobConfig
    {
        [Label("설명 문구"), TextArea(2, 5)] public string howto = "";
        [Label("근무 시간 (초)"), Range(10, 300)] public float durationSeconds = 75;
        [Label("기본급")] public long basePay = 60000;
        [Label("손님당 보너스")] public long bonusPerCustomer = 3000;
        [Label("실수당 보너스 감소")] public long mistakePenalty = 1500;
        [Label("무실수 보너스")] public long perfectBonus = 5000;
        [Label("무실수 보너스 최소 손님")] public int perfectMinCustomers = 3;
        [Label("알바 컨디션 소모")] public float conditionCost = 10;
        [Label("손님 인내심 (초)")] public Vector2 customerPatienceSeconds = new Vector2(14, 20);
        [Label("다음 손님까지 (초)")] public float nextCustomerDelay = 0.6f;
        [Label("손님당 상품 수")] public Vector2 itemsPerCustomer = new Vector2(1, 4);
        [Label("손님이 내는 지폐")] public List<long> cashBills = new List<long> { 1000, 5000, 10000, 50000 };
        [Label("상품")] public List<JobItemCfg> items = new List<JobItemCfg>();
        [Label("결제 방법")] public List<PaymentCfg> payments = new List<PaymentCfg>();
        [Label("손님 종류")] public List<CustomerCfg> customers = new List<CustomerCfg>();
        [Label("대사")] public JobLinesCfg lines = new JobLinesCfg();
    }

    // ───────────────────────── 컷신·대사 ─────────────────────────

    [Serializable] public class LineCfg
    {
        [Label("화자")] public string speaker;
        [Label("대사"), TextArea(1, 4)] public string text;
        [Label("표정", "normal happy sad surprised tired focus")] public string expression;
        [Label("이 줄의 이미지 (선택)")] public Sprite image;
    }

    [Serializable] public class SceneCfg
    {
        [Label("id")] public string id;
        [Label("제목")] public string title;
        [Label("분류")] public string category = "기타";
        [Label("우선순위", "높을수록 대표 재생")] public int priority;
        [Label("수집 후 다시 대표로 나옴")] public bool repeatable = true;
        [Label("사용")] public bool enabled = true;
        [Label("수집률에 포함")] public bool countsForCollection = true;
        [Label("비밀 장면")] public bool secret;
        [Label("일러스트")] public Sprite image;
        [Label("미해금 힌트")] public string hint;
        [Label("조건 설명 문구")] public string conditionText;
        [Label("해금 조건")] public ConditionSet conditions = new ConditionSet();
        [Label("대사")] public List<LineCfg> lines = new List<LineCfg>();
    }

    [Serializable] public class CutsceneSettingsCfg
    {
        [Label("글자 속도 (초당)")] public float charsPerSecond = 28;
        [Label("일러스트 확대 속도")] public float cameraZoomPerSecond = 0.004f;
    }

    [Serializable] public class CutsceneConfig
    {
        [Label("재생 설정")] public CutsceneSettingsCfg settings = new CutsceneSettingsCfg();
        [Label("기본 취침 장면")] public SceneCfg defaultScene = new SceneCfg();
        [Label("컷신 목록")] public List<SceneCfg> scenes = new List<SceneCfg>();
    }

    [Serializable] public class ExpressionCfg
    {
        [Label("표정 키")] public string key;
        [Label("초상화 (작은 얼굴)")] public Sprite portrait;
        [Label("입상 (집 화면)")] public Sprite standing;
    }

    [Serializable] public class ReactionCfg
    {
        [Label("상황 키")] public string key;
        [Label("대사 (무작위 하나)")] public List<LineCfg> lines = new List<LineCfg>();
    }

    [Serializable] public class DialogueSettingsCfg
    {
        [Label("말풍선 표시 시간 (초)")] public float bubbleSeconds = 3.5f;
        [Label("말풍선 최소 간격 (초)")] public float minIntervalSeconds = 5;
        [Label("간격 무시 상황 키")] public List<string> priorityTriggers = new List<string>();
    }

    [Serializable] public class ThresholdsCfg
    {
        [Label("큰 이익 기준")] public long bigProfit = 50000;
        [Label("큰 손실 기준")] public long bigLoss = 50000;
    }

    [Serializable] public class SystemTextsCfg
    {
        [TextArea] public string marketNote = "";
        [TextArea] public string blinkWarning = "";
        [TextArea] public string browserHint = "";
        public string startingDebtTitle = "부모님이 남긴 빚";
        [TextArea] public string startingDebtNote = "";
        [TextArea] public string storyPlaceholder = "";
    }

    [Serializable] public class DialogueConfig
    {
        [Label("말풍선 설정")] public DialogueSettingsCfg settings = new DialogueSettingsCfg();
        [Label("표정 이미지")] public List<ExpressionCfg> expressions = new List<ExpressionCfg>();
        [Label("반응 기준 금액")] public ThresholdsCfg thresholds = new ThresholdsCfg();
        [AsDict("lines"), Label("상황별 반응 대사")] public List<ReactionCfg> reactions = new List<ReactionCfg>();
        [AsDict("lines"), Label("튜토리얼 (stock / browser)")] public List<ReactionCfg> tutorial = new List<ReactionCfg>();
        [Label("새 게임 인트로")] public SceneCfg intro = new SceneCfg();
        [Label("시스템 문구")] public SystemTextsCfg system = new SystemTextsCfg();
    }

    // ───────────────────────── 가구 ─────────────────────────

    [Serializable] public class SlotCfg
    {
        [Label("슬롯 id", "씬의 Room/Slot_<id> 오브젝트와 연결")] public string id;
        [Label("이름")] public string name;
        [Label("비었을 때 테두리 표시")] public bool showEmpty;
    }

    [Serializable] public class ShopItemCfg
    {
        [Label("id")] public string id;
        [Label("슬롯")] public string slot;
        [Label("이름")] public string name;
        [Label("가격")] public long price;
        [Label("이미지")] public Sprite image;
        [Label("설명"), TextArea] public string description;
        [Label("구매 대사")] public string purchaseLine;
    }

    [Serializable] public class ShopConfig
    {
        [Label("슬롯")] public List<SlotCfg> slots = new List<SlotCfg>();
        [Label("시작 가구 id")] public List<string> startingItems = new List<string>();
        [Label("상품")] public List<ShopItemCfg> items = new List<ShopItemCfg>();
    }

    // ───────────────────────── 테마 ─────────────────────────

    [Serializable] public class NamedColor
    {
        public string key;
        public Color value = Color.white;
    }

    [Serializable] public class ThemeCfg
    {
        [Label("테마 키")] public string key;
        [Label("이름")] public string name;
        [Label("설명")] public string description;
        [Label("바탕화면 이미지")] public Sprite wallpaper;
        [Label("해금 조건 (비우면 처음부터)")] public ConditionSet unlock = new ConditionSet();
        [Label("해금 힌트")] public string unlockHint;
        [AsDict("value"), Label("색 (ThemeColor 가 붙은 오브젝트와 강조색에 적용)")] public List<NamedColor> colors = new List<NamedColor>();
    }

    [Serializable] public class ChartCfg
    {
        [Label("기본 보이는 캔들 수")] public int defaultVisibleCandles = 60;
        [Label("최소")] public int minVisibleCandles = 20;
        [Label("최대")] public int maxVisibleCandles = 300;
    }

    [Serializable] public class AppCfg
    {
        [Label("앱 키")] public string key;
        [Label("이름")] public string name;
        [Label("아이콘")] public Sprite icon;
    }

    [Serializable] public class ThemeConfig
    {
        [Label("게임 제목")] public string gameTitle = "내일도, 다녀오겠습니다";
        [Label("부제")] public string gameSubtitle = "편의점 알바와 밤의 투자";
        [Label("알림 표시 시간 (초)")] public float toastSeconds = 3.5f;
        [Label("기본 테마 키")] public string defaultTheme = "pastel_day";
        [AsDict, Label("테마")] public List<ThemeCfg> themes = new List<ThemeCfg>();
        [Label("차트")] public ChartCfg chart = new ChartCfg();
        [AsDict, Label("앱 이름·아이콘")] public List<AppCfg> apps = new List<AppCfg>();
    }

    // ───────────────────────── 이름 ─────────────────────────

    [Serializable] public class SiteCfg
    {
        public string name;
        public string url;
        public string tagline;
    }

    [Serializable] public class SitesCfg
    {
        [Label("뉴스 사이트")] public SiteCfg news = new SiteCfg();
        [Label("커뮤니티 사이트")] public SiteCfg community = new SiteCfg();
    }

    [Serializable] public class NamesConfig
    {
        [Label("사이트")] public SitesCfg sites = new SitesCfg();
        [Label("뉴스 매체명")] public List<string> newsOutlets = new List<string>();
        [Label("커뮤니티 닉네임")] public List<string> nicknames = new List<string>();
    }
}
