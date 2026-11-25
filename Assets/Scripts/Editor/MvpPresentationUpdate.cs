using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.EditorTools
{
    /// <summary>Apply the requested presentation revision to the open scene without rebuilding its layout.</summary>
    public static class MvpPresentationUpdate
    {
        [Serializable] class Report
        {
            public string status, title, unity_version;
            public bool title_character_hidden, subtitle_hidden, dialog_portrait_hidden;
            public bool room_layout_preserved, scene_saved;
            public string product_alignment;
            public float dialog_text_left, dialog_text_width;
        }

        [MenuItem("StockGame/화면 수정/제목·대사창·상품 정렬 적용", false, 90)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before editing the saved scene.");
            var ui = GameObject.Find("UI");
            if (ui == null || ui.scene.path != "Assets/Scenes/Main.unity")
                throw new InvalidOperationException("Open Assets/Scenes/Main.unity first.");

            var room = ui.transform.Find("Screen_Home/Room");
            string roomBefore = RoomLayout(room);
            Undo.RegisterFullObjectHierarchyUndo(ui, "Update game presentation");
            var cfg = ConfigImporter.EnsureAsset();
            Undo.RecordObject(cfg, "Change game title");
            cfg.theme.gameTitle = "두근두근 트레이딩";
            cfg.theme.gameSubtitle = "";
            cfg.dialogue.intro.title = cfg.theme.gameTitle;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssetIfDirty(cfg);
            MvpArtSetup.PreviewRoomArt(room, cfg);

            var title = ui.transform.Find("Screen_Title");
            Hide(title.Find("MvpCharacter"));
            var subtitle = title.Find("Subtitle");
            if (subtitle != null)
            {
                Hide(subtitle);
                SetText(subtitle.GetComponent<Text>(), "");
            }
            SetText(title.Find("Title").GetComponent<Text>(), cfg.theme.gameTitle);

            var dialog = ui.transform.Find("Screen_Cutscene/Dialog");
            Hide(dialog.Find("Portrait"));
            SetRect((RectTransform)dialog.Find("Speaker"), 40, 24, 1520, 36);
            var text = (RectTransform)dialog.Find("Text");
            SetRect(text, 40, 70, 1520, 150);

            var products = ui.transform.Find("Screen_Job/Playing/CounterItems").GetComponent<HorizontalLayoutGroup>();
            Undo.RecordObject(products, "Center convenience-store products");
            products.childAlignment = TextAnchor.UpperCenter;
            EditorUtility.SetDirty(products);

            GameConfig.NotifyChanged();
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(ui.scene);
            bool saved = EditorSceneManager.SaveScene(ui.scene);
            var report = new Report
            {
                title = cfg.theme.gameTitle,
                unity_version = Application.unityVersion,
                title_character_hidden = Hidden(title.Find("MvpCharacter")),
                subtitle_hidden = Hidden(subtitle),
                dialog_portrait_hidden = Hidden(dialog.Find("Portrait")),
                room_layout_preserved = roomBefore == RoomLayout(room),
                scene_saved = saved,
                product_alignment = products.childAlignment.ToString(),
                dialog_text_left = text.anchoredPosition.x,
                dialog_text_width = text.sizeDelta.x
            };
            bool passed = report.title == "두근두근 트레이딩" && report.title_character_hidden
                && report.subtitle_hidden && report.dialog_portrait_hidden && report.room_layout_preserved
                && saved && report.product_alignment == "UpperCenter"
                && Mathf.Approximately(report.dialog_text_left, 40) && Mathf.Approximately(report.dialog_text_width, 1520);
            report.status = passed ? "PASS" : "FAIL";
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/previews/ui_revision_v002_validation.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            if (!passed) throw new InvalidOperationException("Presentation revision validation failed: " + output);
            Debug.Log("[MVP_PRESENTATION_V002] PASS — title, dialog, centered products; room layout preserved.");
        }

        static bool Hidden(Transform t) { return t == null || !t.gameObject.activeSelf; }
        static void Hide(Transform t)
        {
            if (t == null) return;
            Undo.RecordObject(t.gameObject, "Hide presentation element");
            t.gameObject.SetActive(false);
        }
        static void SetText(Text text, string value)
        {
            Undo.RecordObject(text, "Change title text");
            text.text = value;
            EditorUtility.SetDirty(text);
        }
        static void SetRect(RectTransform rt, float x, float y, float width, float height)
        {
            Undo.RecordObject(rt, "Expand dialog text");
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }
        static string RoomLayout(Transform room)
        {
            string result = "";
            foreach (var rt in room.GetComponentsInChildren<RectTransform>(true))
                result += rt.GetInstanceID() + ":" + EditorJsonUtility.ToJson(rt) + "\n";
            return result;
        }
    }
}
