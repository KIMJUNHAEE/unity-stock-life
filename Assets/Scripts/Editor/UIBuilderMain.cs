using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StockGame.EditorTools
{
    /// <summary>
    /// 메뉴 StockGame ▸ UI 만들기: 현재 씬에 모든 화면을 GameObject 로 만든다.
    /// 만든 뒤에는 Scene 뷰에서 옮기고 Inspector 에서 색·글자·이미지를 바꾸면 된다.
    /// 다시 만들면 직접 고친 배치는 사라지므로 처음 한 번만 쓰는 것을 권장한다.
    /// </summary>
    public static partial class UIBuilder
    {
        public const string RootName = "UI";

        [MenuItem("StockGame/UI 만들기 (씬에 생성)", false, 20)]
        public static void BuildMenu()
        {
            var existing = Object.FindAnyObjectByType<GameRoot>();
            if (existing != null && !EditorUtility.DisplayDialog("UI 다시 만들기",
                    "씬에 이미 UI 가 있습니다. 지우고 새로 만들면 직접 바꾼 배치·색·이미지가 사라집니다.\n(먼저 씬을 복사해 두면 안전합니다)", "지우고 새로 만들기", "취소"))
                return;
            Build();
        }

        public static void Build()
        {
            LoadResources();
            var oldRoot = Object.FindAnyObjectByType<GameRoot>();
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot.gameObject);
            var oldUi = GameObject.Find(RootName);
            if (oldUi != null && oldUi.GetComponent<Canvas>() != null) Undo.DestroyObjectImmediate(oldUi);

            var canvasGo = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var ct = canvasGo.transform;
            Make2DScene();

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "EventSystem");
            }

            var title = BuildTitle(ct);
            var home = BuildHome(ct);
            var job = BuildJob(ct);
            var desktop = BuildDesktop(ct);
            var settlement = BuildSettlement(ct);
            var story = BuildStory(ct);
            var cutscene = BuildCutscene(ct);
            var debug = BuildDebug(ct);
            var info = BuildInfo(ct);
            var modal = BuildModal(ct);
            var toasts = BuildToasts(ct);
            modal.gameObject.SetActive(false);

            var rootGo = new GameObject("GameRoot");
            var gr = rootGo.AddComponent<GameRoot>();
            gr.title = title; gr.home = home; gr.job = job; gr.desktop = desktop;
            gr.settlement = settlement; gr.story = story; gr.cutscene = cutscene;
            gr.modalLayer = modal; gr.toastLayer = toasts; gr.debugPanel = debug; gr.infoOverlay = info;

            AddHomeProgress(home.transform);
            AddDesktopGuide(desktop.transform);
            AddSettlementReview(settlement.transform);
            StyleJobCounter(job.transform);
            if (System.IO.File.Exists("Assets/Art/mvp_art_manifest.json")) MvpArtSetup.Apply();
            ShowOnly(ScreenId.Title);
            Undo.RegisterCreatedObjectUndo(canvasGo, "StockGame UI");
            Undo.RegisterCreatedObjectUndo(rootGo, "StockGame GameRoot");
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Focus2D(title.gameObject);
            Debug.Log("[StockGame] UI 를 만들었습니다. Hierarchy 의 UI 아래 Screen_* 를 켜고 끄면서 Scene 뷰에서 편집하세요. (메뉴 StockGame ▸ 화면 보기)");
        }

        // ───────────────────────── 편집 도우미 ─────────────────────────

        public static void ShowOnly(ScreenId id)
        {
            var gr = Object.FindAnyObjectByType<GameRoot>();
            if (gr == null) { Debug.LogWarning("[StockGame] 씬에 GameRoot 가 없습니다. StockGame ▸ UI 만들기를 먼저 실행하세요."); return; }
            Set(gr.title, id == ScreenId.Title);
            Set(gr.home, id == ScreenId.Home);
            Set(gr.job, id == ScreenId.Job);
            Set(gr.desktop, id == ScreenId.Desktop);
            Set(gr.settlement, id == ScreenId.Settlement);
            Set(gr.story, id == ScreenId.Story);
            Set(gr.cutscene, id == ScreenId.Cutscene);
            Component target = null;
            switch (id)
            {
                case ScreenId.Title: target = gr.title; break;
                case ScreenId.Home: target = gr.home; break;
                case ScreenId.Job: target = gr.job; break;
                case ScreenId.Desktop: target = gr.desktop; break;
                case ScreenId.Settlement: target = gr.settlement; break;
                case ScreenId.Story: target = gr.story; break;
                case ScreenId.Cutscene: target = gr.cutscene; break;
            }
            if (target != null) Focus2D(target.gameObject);
        }

        // ───────────────────────── 2D 편집 ─────────────────────────

        /// <summary>Scene 뷰를 2D(정면·평면)로 바꾸고 대상을 화면에 꽉 차게 보여 준다. 사각형 툴(T)을 고른다.</summary>
        public static void Focus2D(GameObject target)
        {
            if (target != null) Selection.activeGameObject = target;
            var sv = SceneView.lastActiveSceneView;
            if (sv == null && SceneView.sceneViews != null && SceneView.sceneViews.Count > 0) sv = SceneView.sceneViews[0] as SceneView;
            if (sv == null) return;
            sv.in2DMode = true;
            sv.sceneLighting = false;
            Tools.current = Tool.Rect;
            if (target != null) sv.FrameSelected();
            sv.Repaint();
        }

        [MenuItem("StockGame/2D 편집 보기 (선택한 것 맞추기) %#f", false, 30)]
        static void Focus2DMenu()
        {
            var go = Selection.activeGameObject;
            if (go == null || go.GetComponent<RectTransform>() == null)
            {
                var gr = Object.FindAnyObjectByType<GameRoot>();
                var ui = GameObject.Find(RootName);
                go = ui != null ? ui : (gr != null ? gr.gameObject : null);
            }
            Focus2D(go);
        }

        /// <summary>2D 게임용 기본값: 새 그림은 Sprite 로 임포트, 3D 조명 제거, 카메라 정사영.</summary>
        public static void Make2DScene()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l != null && l.type == LightType.Directional && l.gameObject.name == "Directional Light" && l.transform.childCount == 0)
                    Undo.DestroyObjectImmediate(l.gameObject);
            var cam = Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(cam, "2D 카메라");
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.93f, 0.92f, 0.95f);
            }
            FixCanvasRenderers();
        }

        /// <summary>그라데이션·차트처럼 직접 만든 그래픽에 CanvasRenderer 가 빠져 안 보이던 문제를 고친다.</summary>
        public static int FixCanvasRenderers()
        {
            int n = 0;
            foreach (var g in Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g == null || g.GetComponent<CanvasRenderer>() != null) continue;
                Undo.AddComponent<CanvasRenderer>(g.gameObject);
                g.SetAllDirty();
                n++;
            }
            if (n > 0) Debug.Log("[StockGame] 빠져 있던 CanvasRenderer " + n + "개를 추가했습니다.");
            return n;
        }

        static void Set(Component c, bool on)
        {
            if (c == null || c.gameObject.activeSelf == on) return;
            Undo.RecordObject(c.gameObject, "화면 보기");
            c.gameObject.SetActive(on);
        }

        [MenuItem("StockGame/화면 보기/타이틀", false, 40)] static void V1() { ShowOnly(ScreenId.Title); }
        [MenuItem("StockGame/화면 보기/집", false, 41)] static void V2() { ShowOnly(ScreenId.Home); }
        [MenuItem("StockGame/화면 보기/편의점 알바", false, 42)] static void V3() { ShowOnly(ScreenId.Job); }
        [MenuItem("StockGame/화면 보기/컴퓨터 바탕화면", false, 43)] static void V4() { ShowOnly(ScreenId.Desktop); }
        [MenuItem("StockGame/화면 보기/결산", false, 44)] static void V5() { ShowOnly(ScreenId.Settlement); }
        [MenuItem("StockGame/화면 보기/스토리 선택", false, 45)] static void V6() { ShowOnly(ScreenId.Story); }
        [MenuItem("StockGame/화면 보기/컷신", false, 46)] static void V7() { ShowOnly(ScreenId.Cutscene); }

        static void ShowApp(string app)
        {
            ShowOnly(ScreenId.Desktop);
            var gr = Object.FindAnyObjectByType<GameRoot>();
            if (gr == null || gr.desktop == null) return;
            var d = gr.desktop;
            GameObject[] apps = { d.appStock, d.appBrowser, d.appFinance, d.appAlbum, d.appSettings };
            string[] ids = { "stock", "browser", "finance", "album", "settings" };
            for (int i = 0; i < apps.Length; i++)
            {
                if (apps[i] == null) continue;
                Undo.RecordObject(apps[i], "앱 보기");
                apps[i].SetActive(ids[i] == app);
                if (ids[i] == app) Selection.activeGameObject = apps[i];
            }
        }

        [MenuItem("StockGame/화면 보기/앱 - 증권", false, 60)] static void A1() { ShowApp("stock"); }
        [MenuItem("StockGame/화면 보기/앱 - 인터넷", false, 61)] static void A2() { ShowApp("browser"); }
        [MenuItem("StockGame/화면 보기/앱 - 금융", false, 62)] static void A3() { ShowApp("finance"); }
        [MenuItem("StockGame/화면 보기/앱 - 밤의 기록", false, 63)] static void A4() { ShowApp("album"); }
        [MenuItem("StockGame/화면 보기/앱 - 설정", false, 64)] static void A5() { ShowApp("settings"); }

        /// <summary>선택한 오브젝트의 CanvasGroup 을 보이기/숨기기 (편집 중 겹친 패널 정리용, 실행 중엔 자동).</summary>
        [MenuItem("StockGame/선택한 패널 보이기·숨기기 %#h", false, 80)]
        static void ToggleSelected()
        {
            foreach (var go in Selection.gameObjects)
            {
                var g = go.GetComponent<CanvasGroup>();
                if (g == null) continue;
                Undo.RecordObject(g, "패널 보이기·숨기기");
                bool show = g.alpha < 0.5f;
                g.alpha = show ? 1 : 0;
                g.blocksRaycasts = show;
                g.interactable = show;
            }
        }
    }
}
