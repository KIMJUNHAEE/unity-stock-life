using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StockGame.EditorTools
{
    /// <summary>
    /// 프로젝트를 열면 자동으로 실행되는 준비 작업. 직접 누를 필요는 없다.
    /// 1) Assets/Scenes/Main.unity 가 없으면 만들고 빌드 목록에 넣는다.
    /// 2) Assets/Resources/GameConfig.asset 이 없으면 config 폴더 JSON 값으로 만든다.
    /// 3) Main 씬에 UI 가 없으면 만든다 (StockGame ▸ UI 만들기와 같음).
    /// 메뉴: StockGame ▸ ...
    /// </summary>
    [InitializeOnLoad]
    public static class StockGameEditorSetup
    {
        const string FontRoot = "Assets/Art/Fonts";
        const string LibraryPath = "Assets/Resources/FontLibrary.asset";
        const string ScenePath = "Assets/Scenes/Main.unity";

        static StockGameEditorSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoSetup;
                return;
            }
            RebuildFontLibrary(false);
            EnsureMainScene(false);
            ConfigImporter.EnsureAsset();
            AutoBuildUI();
            Auto2DOnce();
        }

        /// <summary>이 프로젝트를 처음 연 PC 에서 한 번: 2D 기본값과 Scene 뷰 2D 모드를 켠다.</summary>
        static void Auto2DOnce()
        {
            const string key = "StockGame.Auto2D.v2";
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) return;
            if (UIBuilder.FixCanvasRenderers() > 0) EditorSceneManager.MarkSceneDirty(scene);
            if (EditorPrefs.GetBool(key, false)) { if (scene.isDirty) EditorSceneManager.SaveScene(scene); return; }
            UIBuilder.Make2DScene();
            if (SceneView.lastActiveSceneView != null) UIBuilder.Focus2D(GameObject.Find(UIBuilder.RootName));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorPrefs.SetBool(key, true);
        }

        /// <summary>Main 씬이 열려 있고 UI 가 없으면 한 번 만들어 저장한다.</summary>
        static void AutoBuildUI()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) return;
            if (Object.FindAnyObjectByType<GameRoot>() != null) return;
            UIBuilder.Build();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[StockGame] Main 씬에 UI 를 자동으로 만들고 저장했습니다.");
        }

        [MenuItem("StockGame/고급/폰트 목록 다시 만들기")]
        public static void RebuildFontLibraryMenu() { RebuildFontLibrary(true); }

        public static void RebuildFontLibrary(bool verbose)
        {
            if (!AssetDatabase.IsValidFolder(FontRoot)) return;
            var fonts = new List<Font>();
            foreach (var guid in AssetDatabase.FindAssets("t:Font", new[] { FontRoot }))
            {
                var f = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
                if (f != null) fonts.Add(f);
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var lib = AssetDatabase.LoadAssetAtPath<FontLibrary>(LibraryPath);
            bool created = false;
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<FontLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
                created = true;
            }
            bool same = lib.fonts.Count == fonts.Count;
            if (same) for (int i = 0; i < fonts.Count; i++) if (lib.fonts[i] != fonts[i]) { same = false; break; }
            if (!same || created)
            {
                lib.fonts = fonts;
                EditorUtility.SetDirty(lib);
                AssetDatabase.SaveAssets();
            }
            if (verbose) Debug.Log("[StockGame] 폰트 목록: " + fonts.Count + "개 → " + LibraryPath);
        }

        [MenuItem("StockGame/고급/메인 씬 만들기·빌드 목록 등록")]
        public static void EnsureMainSceneMenu() { EnsureMainScene(true); }

        static void EnsureMainScene(bool verbose)
        {
            if (!File.Exists(ScenePath))
            {
                var active = SceneManager.GetActiveScene();
                // 사용자가 작업 중인 씬은 건드리지 않는다.
                if (!string.IsNullOrEmpty(active.path) || active.isDirty)
                {
                    if (verbose) Debug.Log("[StockGame] 현재 씬을 저장/닫은 뒤 다시 실행하세요.");
                    return;
                }
                if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[StockGame] " + ScenePath + " 를 만들었습니다. 플레이를 누르면 게임이 자동으로 시작됩니다.");
            }
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list) if (s.path == ScenePath) return;
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            if (verbose) Debug.Log("[StockGame] 빌드 목록에 " + ScenePath + " 를 추가했습니다.");
        }

        [MenuItem("StockGame/고급/저장 폴더 열기")]
        public static void OpenSaveFolder()
        {
            Directory.CreateDirectory(Game.SaveDir);
            EditorUtility.RevealInFinder(Game.SaveDir);
        }
    }

    /// <summary>
    /// 빌드 전: 폰트 목록 갱신. 빌드 후: data/, config/, art/ 를 빌드의 StreamingAssets 로 복사.
    /// 빌드된 게임에서도 그 폴더의 JSON·이미지를 고치면 바로 반영된다.
    /// </summary>
    public class StockGameBuildHooks : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            StockGameEditorSetup.RebuildFontLibrary(false);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            string output = report.summary.outputPath;
            string streaming = null;
            var target = report.summary.platform;
            if (target == BuildTarget.StandaloneOSX)
                streaming = Path.Combine(output, "Contents/Resources/Data/StreamingAssets");
            else if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneLinux64)
                streaming = Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_Data/StreamingAssets");
            if (streaming == null)
            {
                Debug.LogWarning("[StockGame] 이 플랫폼에서는 편집 가능 데이터 자동 복사를 지원하지 않습니다: " + target);
                return;
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (var folder in new[] { "data", "config", "art" })
            {
                string src = Path.Combine(root, folder);
                if (Directory.Exists(src)) CopyDir(src, Path.Combine(streaming, folder));
            }
            Debug.Log("[StockGame] 편집 가능한 data/config/art 를 복사했습니다 → " + streaming);
        }

        static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
            {
                if (f.EndsWith(".meta")) continue;
                File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            }
            foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }
    }
}
