using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.EditorTools
{
    /// <summary>Visual smoke review. Uses an in-memory state; never calls NewGame/Save or completes a scene.</summary>
    [InitializeOnLoad]
    public static class MvpArtPlayReview
    {
        const string Pending = "StockGame.MvpArtReview.Pending";
        const string Stage = "StockGame.MvpArtReview.Stage";
        static double next;
        static readonly List<string> errors = new List<string>();
        static string Output { get { return Path.Combine(Application.dataPath, "../docs/previews"); } }
        static MvpArtPlayReview()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending,false) && SessionState.GetInt(Stage,0) == 99)
                {
                    SessionState.SetBool(Pending,false);
                    if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt("StockGame.MvpArtReview.Exit",0));
                }
            };
            Application.logMessageReceived += (message, stack, type) => {
                if (SessionState.GetBool(Pending,false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    errors.Add(message);
            };
        }

        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("This review entry point is for batch mode only.");
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            StartReview();
        }

        [MenuItem("StockGame/MVP 아트/현재 씬 화면 검토 (저장 데이터 유지)", false, 27)]
        public static void RunOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before starting the review.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Main.unity" || scene.isDirty)
                throw new InvalidOperationException("Open and save Main.unity before starting the review.");
            StartReview();
        }

        static void StartReview()
        {
            errors.Clear();
            next = 0;
            SessionState.SetBool(Pending,true);
            SessionState.SetInt(Stage,0);
            SessionState.SetFloat("StockGame.MvpArtReview.Start",(float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Pending,false)) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat("StockGame.MvpArtReview.Start",0) > 180)
            { Fail(new TimeoutException("MVP visual review timed out.")); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            var root = GameRoot.I;
            if (root == null || Game.I == null) return;
            try
            {
                int stage = SessionState.GetInt(Stage,0);
                switch (stage)
                {
                    case 0:
                        Game.I.S = new GameState { Day=1, Phase="morning", Cash=300000, StartingDebt=1000000000, Condition=100, TicksPerDay=180, SessionStartMinute=1260, ActiveTheme="pastel_day" };
                        foreach (string id in GameConfig.Instance.shop.startingItems)
                        {
                            var item = GameConfig.Instance.ShopItem(id);
                            if (item != null) { Game.I.S.OwnedItems.Add(id); Game.I.S.Equipped[item.slot]=id; }
                        }
                        Game.I.UserPaused=true;
                        root.Go(ScreenId.Title);
                        break;
                    case 1:
                        Capture("mvp_title_v001");
                        root.Go(ScreenId.Home); root.BubbleTimer=0;
                        break;
                    case 2:
                        Capture("mvp_home_v001");
                        root.Go(ScreenId.Job); root.job.Begin(); root.job.Act("start",Ctx.None);
                        // Fix the preview customer and basket without changing production content or random rules.
                        var method=typeof(JobScreen).GetMethod("NewCustomer",BindingFlags.Instance|BindingFlags.NonPublic);
                        object customer=method.Invoke(root.job,null);
                        var ct=customer.GetType();
                        ct.GetField("Who").SetValue(customer,GameConfig.Instance.job.customers[1]);
                        var items=(List<int>)ct.GetField("Items").GetValue(customer); items.Clear(); items.AddRange(new[]{0,1,5,8});
                        var scanned=(List<bool>)ct.GetField("Scanned").GetValue(customer); scanned.Clear(); scanned.AddRange(new[]{false,false,false,false});
                        typeof(JobScreen).GetField("cur",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(root.job,customer);
                        break;
                    case 3:
                        Capture("mvp_store_v001");
                        Game.I.S.Phase="evening"; root.Go(ScreenId.Desktop); root.BubbleTimer=0;
                        root.desktop.Active=""; root.desktop.Minimized=false; root.desktop.Running.Clear();
                        if (root.desktop.Flag("window_open",Ctx.None)) throw new InvalidOperationException("An empty desktop must not show a blank application window.");
                        break;
                    case 4:
                        Capture("mvp_desktop_v001");
                        root.desktop.Open("finance");
                        if (!root.desktop.Flag("window_open",Ctx.None)) throw new InvalidOperationException("Launched application window must be visible.");
                        root.Go(ScreenId.Cutscene);
                        root.cutscene.Play(GameConfig.Instance.Scene("CS_first_profit"),false,()=>{});
                        root.cutscene.Advance(); // Reveal the current line only; no scene completion or gallery write.
                        break;
                    case 5:
                        Capture("mvp_cutscene_v001");
                        var cfg=GameConfig.Instance;
                        string report="{\n  \"status\": \"" + (errors.Count==0?"PASS":"FAIL") + "\",\n  \"unity_version\": \"" + Application.unityVersion
                            + "\",\n  \"play_mode_smoke_review\": true,\n  \"preview_size_px\": [1920,1080],\n  \"expressions\": " + cfg.dialogue.expressions.Count
                            + ",\n  \"customers\": " + cfg.job.customers.Count + ",\n  \"products\": " + cfg.job.items.Count + ",\n  \"furniture\": " + cfg.shop.items.Count
                            + ",\n  \"runtime_errors\": " + errors.Count + ",\n  \"save_methods_called\": false,\n  \"seven_day_balance_test\": false\n}\n";
                        File.WriteAllText(Path.Combine(Output,Application.isBatchMode ? "mvp_art_validation.json" : "mvp_art_validation_v002.json"),report);
                        foreach(var error in errors) Debug.LogWarning("[MVP_REVIEW_CAPTURED_ERROR] "+error);
                        Debug.Log("[MVP_ART_PLAY_REVIEW] "+(errors.Count==0?"PASS":"FAIL"));
                        SessionState.SetInt("StockGame.MvpArtReview.Exit",errors.Count==0?0:1);
                        SessionState.SetInt(Stage,99);
                        EditorApplication.isPlaying=false;
                        return;
                    default: return;
                }
                SessionState.SetInt(Stage,stage+1);
                next=EditorApplication.timeSinceStartup+1.2;
            }
            catch(Exception ex) { Fail(ex); }
        }

        static void Fail(Exception ex)
        {
            SessionState.SetBool(Pending,false); Debug.LogException(ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else EditorApplication.isPlaying = false;
        }
        static void Capture(string name)
        {
            Directory.CreateDirectory(Output);
            var canvas=GameObject.Find("UI").GetComponent<Canvas>();
            var scaler=canvas.GetComponent<CanvasScaler>();
            var originalMode=canvas.renderMode; var originalCamera=canvas.worldCamera;
            bool originalScaler=scaler.enabled;
            var go=new GameObject("MvpReviewCamera");
            var camera=go.AddComponent<Camera>();
            camera.orthographic=true; camera.orthographicSize=540; camera.aspect=1920f/1080;
            camera.transform.position=new Vector3(0,0,-10); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.white;
            var target=new RenderTexture(1920,1080,24);
            camera.targetTexture=target;
            var previous=RenderTexture.active;
            try
            {
                scaler.enabled=false; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=10; canvas.scaleFactor=1;
                Canvas.ForceUpdateCanvases();
                if (name == "mvp_desktop_v001")
                    foreach (var graphic in GameRoot.I.desktop.GetComponentsInChildren<Graphic>(true))
                        if (graphic.name == "Name" || graphic.name == "Left" || graphic.name == "Image")
                            Debug.Log("[MVP_DESKTOP_RENDER] "+graphic.transform.parent.name+"/"+graphic.name
                                +" active="+graphic.gameObject.activeInHierarchy+" alpha="+graphic.canvasRenderer.GetInheritedAlpha()
                                +" culled="+graphic.canvasRenderer.cull+" position="+graphic.rectTransform.position+" size="+graphic.rectTransform.rect.size);
                camera.Render();
                RenderTexture.active=target;
                var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                tex.ReadPixels(new Rect(0,0,1920,1080),0,0); tex.Apply();
                string outputName = Application.isBatchMode ? name : name.Replace("_v001", "_v002");
                File.WriteAllBytes(Path.Combine(Output,outputName+".png"),tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active=previous; canvas.renderMode=originalMode; canvas.worldCamera=originalCamera; scaler.enabled=originalScaler;
                camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
