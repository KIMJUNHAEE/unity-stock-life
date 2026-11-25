using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    public enum ScreenId { Title, Home, Job, Desktop, Settlement, Story, Cutscene }

    public class ModalButton
    {
        public string Label;
        public Action OnClick;
        public bool Primary;
        public ModalButton(string label, bool primary, Action onClick) { Label = label; Primary = primary; OnClick = onClick; }
    }

    public class Modal
    {
        public string Title;
        public string Body;
        public List<ModalButton> Buttons = new List<ModalButton>();
    }

    /// <summary>
    /// 씬의 "GameRoot" 오브젝트. 화면 전환, 모달·알림, 캐릭터 반응, 단축키, 설정 반영을 맡는다.
    /// 화면 오브젝트 연결은 StockGame ▸ UI 만들기가 자동으로 해 준다.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot I;

        [Header("화면 (자동 연결)")]
        public TitleScreen title;
        public HomeScreen home;
        public JobScreen job;
        public DesktopScreen desktop;
        public SettlementScreen settlement;
        public StoryScreen story;
        public CutscenePlayer cutscene;
        [Header("겹치는 층")]
        public ModalLayer modalLayer;
        public ToastLayer toastLayer;
        public DebugPanel debugPanel;
        public InfoOverlay infoOverlay;

        public ScreenId Screen = ScreenId.Title;
        readonly List<Modal> modals = new List<Modal>();
        public readonly List<KeyValuePair<string, float>> Toasts = new List<KeyValuePair<string, float>>();

        public string BubbleText;
        public string BubbleExpression = "normal";
        public float BubbleTimer;
        float lastBubbleTime = -999;
        readonly System.Random uiRng = new System.Random();

        public float BlinkTimer;
        public float BlinkDuration;

        ConfigStore store;
        bool configReady;
        bool configDirty;

        public ConfigStore Store { get { return store; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CheckScene()
        {
            if (FindAnyObjectByType<GameRoot>() == null)
                Debug.LogError("[StockGame] 씬에 GameRoot 가 없습니다. Assets/Scenes/Main.unity 를 열거나 메뉴 StockGame ▸ UI 만들기를 실행하세요.");
        }

        void Awake()
        {
            I = this;
            useGUILayout = false;
            Application.targetFrameRate = 60;
            ContentPaths.Resolve();

            store = new ConfigStore();
            store.Register("companies", "data/companies.json");
            store.Register("events", "data/events.json");
            store.Register("news", "data/news.json");
            store.Register("posts", "data/community_posts.json");
            store.Register("comments", "data/community_comments.json");
            store.Register("story", "data/story_milestones.json");
            Cfg.Store = store;
            store.Reloaded += OnConfigReloaded;
            store.LoadAll();
            InjectGameConfig();
            configReady = true;
            GameConfig.Changed += OnGameConfigChanged;

            var game = new Game();
            Game.I = game;
            game.LoadMeta();
            game.OnToast += Toast;
            game.OnReaction += OnReaction;
            game.OnBlink += d => { BlinkDuration = d; BlinkTimer = d; };

            if (GameConfig.Instance == null) Toast("Assets/Resources/GameConfig.asset 이 없습니다. StockGame ▸ 설정 에셋 만들기를 실행하세요.");
            foreach (var err in store.Errors()) Toast("[데이터 오류] " + err);
        }

        void Start()
        {
            if (debugPanel != null) debugPanel.gameObject.SetActive(false);
            if (infoOverlay != null) infoOverlay.gameObject.SetActive(false);
            Go(ScreenId.Title);
        }

        void OnDestroy() { GameConfig.Changed -= OnGameConfigChanged; }

        void OnGameConfigChanged() { configDirty = true; }

        void InjectGameConfig()
        {
            var cfg = GameConfig.Instance;
            if (cfg == null) return;
            foreach (var kv in cfg.Trees()) store.Inject(kv.Key, kv.Value);
            store.BumpVersion(new List<string>());
        }

        void OnConfigReloaded(List<string> changed)
        {
            if (changed.Count > 0 && configReady) Toast("데이터 다시 불러옴: " + string.Join(", ", changed.ToArray()));
        }

        public void ReloadAll()
        {
            store.LoadAll();
            InjectGameConfig();
            var errs = store.Errors();
            if (errs.Count == 0) Toast("설정과 데이터를 다시 불러왔습니다 (F5)");
            else foreach (var e in errs) Toast("[데이터 오류] " + e);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (configDirty) { configDirty = false; InjectGameConfig(); }
            float poll = Cfg.F("balance", "dev.hot_reload_poll_seconds", 1f);
            if (poll > 0) store.Poll(dt, poll);

            for (int i = Toasts.Count - 1; i >= 0; i--)
            {
                var t = Toasts[i];
                Toasts[i] = new KeyValuePair<string, float>(t.Key, t.Value - dt);
                if (Toasts[i].Value <= 0) Toasts.RemoveAt(i);
            }
            if (BubbleTimer > 0) BubbleTimer -= dt;
            if (BlinkTimer > 0) BlinkTimer -= dt;

            var g = Game.I;
            if (g.S != null && MarketShouldRun()) g.Advance(dt, desktop != null ? desktop.Speed : 1);
            if (g.S != null && Screen != ScreenId.Title) g.CheckGuideProgress();

            if (modalLayer != null && modalLayer.gameObject.activeSelf != (modals.Count > 0)) modalLayer.gameObject.SetActive(modals.Count > 0);
        }

        /// <summary>시장 시계가 흐르는 조건: 증권 앱이 앞에 있고, 모달·안내가 없고, 일시정지가 아닐 때.</summary>
        public bool MarketShouldRun()
        {
            var g = Game.I;
            if (g == null || g.S == null || desktop == null) return false;
            if (Screen != ScreenId.Desktop || modals.Count > 0) return false;
            if (desktop.Overlay != null || !desktop.StockInFront) return false;
            if (g.UserPaused) return false;
            return g.MarketOpenNow();
        }

        // ───────────────────────── 화면 ─────────────────────────

        public void Go(ScreenId id)
        {
            Screen = id;
            Set(title, id == ScreenId.Title);
            Set(home, id == ScreenId.Home);
            Set(job, id == ScreenId.Job);
            Set(desktop, id == ScreenId.Desktop);
            Set(settlement, id == ScreenId.Settlement);
            Set(story, id == ScreenId.Story);
            Set(cutscene, id == ScreenId.Cutscene);
            if (id == ScreenId.Desktop && desktop != null) desktop.OnEnter();
            if (id == ScreenId.Home && home != null) home.OnEnter();
        }

        static void Set(Component c, bool on) { if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on); }

        void OnGUI()
        {
            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;
            switch (e.keyCode)
            {
                case KeyCode.F5: ReloadAll(); e.Use(); break;
                case KeyCode.F1: if (infoOverlay != null) infoOverlay.gameObject.SetActive(!infoOverlay.gameObject.activeSelf); e.Use(); break;
                case KeyCode.F12:
                    if (debugPanel != null && Cfg.B("balance", "dev.debug_panel", true)) debugPanel.gameObject.SetActive(!debugPanel.gameObject.activeSelf);
                    e.Use();
                    break;
                case KeyCode.Escape:
                    if (modals.Count > 0) { modals.RemoveAt(modals.Count - 1); e.Use(); }
                    else if (infoOverlay != null && infoOverlay.gameObject.activeSelf) { infoOverlay.gameObject.SetActive(false); e.Use(); }
                    break;
                case KeyCode.Space:
                    if (Screen == ScreenId.Desktop && modals.Count == 0 && Game.I.S != null) { Game.I.UserPaused = !Game.I.UserPaused; e.Use(); }
                    else if (Screen == ScreenId.Cutscene && cutscene != null) { cutscene.Advance(); e.Use(); }
                    break;
                case KeyCode.Return:
                    if (Screen == ScreenId.Cutscene && cutscene != null) { cutscene.Advance(); e.Use(); }
                    break;
            }
        }

        // ───────────────────────── 모달 ─────────────────────────

        public bool HasModal { get { return modals.Count > 0; } }
        public Modal TopModal { get { return modals.Count > 0 ? modals[modals.Count - 1] : null; } }

        public Modal ShowModal(string titleText, string body, params ModalButton[] buttons)
        {
            var m = new Modal { Title = titleText, Body = body };
            m.Buttons.AddRange(buttons);
            if (m.Buttons.Count == 0) m.Buttons.Add(new ModalButton("확인", true, null));
            modals.Add(m);
            if (modalLayer != null) modalLayer.gameObject.SetActive(true);
            return m;
        }

        public void Confirm(string titleText, string body, string yes, Action onYes, string no = "취소")
        {
            ShowModal(titleText, body, new ModalButton(no, false, null), new ModalButton(yes, true, onYes));
        }

        public void ClickModal(int index)
        {
            var m = TopModal;
            if (m == null || index < 0 || index >= m.Buttons.Count) return;
            modals.Remove(m);
            var b = m.Buttons[index];
            if (b.OnClick != null) b.OnClick();
        }

        public void CloseAllModals() { modals.Clear(); }

        // ───────────────────────── 알림·반응 ─────────────────────────

        public void Toast(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            var cfg = GameConfig.Instance;
            Toasts.Add(new KeyValuePair<string, float>(msg, cfg != null ? cfg.theme.toastSeconds : 3.5f));
            if (Toasts.Count > 5) Toasts.RemoveAt(0);
        }

        void OnReaction(string trigger)
        {
            if (!Game.I.Settings.Reactions) return;
            var cfg = GameConfig.Instance;
            if (cfg == null) return;
            ReactionCfg r = null;
            foreach (var x in cfg.dialogue.reactions) if (x.key == trigger) r = x;
            if (r == null || r.lines.Count == 0) return;
            bool priority = cfg.dialogue.settings.priorityTriggers.Contains(trigger);
            if (!priority && Time.realtimeSinceStartup - lastBubbleTime < cfg.dialogue.settings.minIntervalSeconds) return;
            var line = r.lines[uiRng.Next(r.lines.Count)];
            Say(line.text, line.expression);
        }

        public void Say(string text, string expression)
        {
            BubbleText = text;
            BubbleExpression = string.IsNullOrEmpty(expression) ? "normal" : expression;
            var cfg = GameConfig.Instance;
            BubbleTimer = cfg != null ? cfg.dialogue.settings.bubbleSeconds : 3.5f;
            lastBubbleTime = Time.realtimeSinceStartup;
        }

        public bool BubbleVisible { get { return BubbleTimer > 0 && !string.IsNullOrEmpty(BubbleText); } }

        public string CurrentExpression()
        {
            if (BubbleTimer > 0) return BubbleExpression;
            return Game.I != null && Game.I.S != null && Game.I.StageIndex() >= 2 ? "tired" : "normal";
        }
    }
}
