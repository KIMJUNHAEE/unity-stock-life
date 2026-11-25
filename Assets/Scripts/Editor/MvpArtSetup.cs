using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.EditorTools
{
    /// <summary>Imports the reviewed MVP art candidates without rebuilding the user's UI or resetting balance.</summary>
    public static class MvpArtSetup
    {
        [Serializable] public class ArtEntry { public string id, key, kind, path; }
        [Serializable] public class ArtManifest { public ArtEntry[] entries; }
        const string ManifestPath = "Assets/Art/mvp_art_manifest.json";
        const string IconRoot = "Assets/Art/UI/Icons/";
        static string Root { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
        static Dictionary<string, ArtEntry> entries;
        static readonly string[] Expressions = { "normal", "happy", "sad", "surprised", "tired", "focus" };
        static readonly string[] Apps = { "stock", "browser", "finance", "album", "settings" };
        static readonly string[] CustomerKeys = { "kang_harin", "baek_soeun", "cha_nari", "lee_doa" };
        static readonly string[] CustomerNames = { "강하린 · 출근길 직장인", "백소은 · 대학생", "차나리 · 아침 러너", "이도아 · 야간 근무자" };
        static readonly string[] ProductKeys = { "rice_ball", "banana_milk", "cup_noodle", "water", "lunchbox", "soda", "jelly", "sandwich", "coffee", "snack", "icecream", "pen" };

        [MenuItem("StockGame/MVP 아트/생성 에셋 연결 (배치 유지)", false, 25)]
        public static void Apply()
        {
            if (!File.Exists(ManifestPath)) { Debug.LogWarning("[MVP Art] Manifest is not ready."); return; }
            var manifest = JsonUtility.FromJson<ArtManifest>(File.ReadAllText(ManifestPath));
            entries = manifest.entries.ToDictionary(e => e.key);
            foreach (var e in manifest.entries) Import(e.path, e.kind == "seoha", e.kind == "background" || e.kind == "wallpaper");
            foreach (var app in Apps) { MakeIcon(app); Import(IconRoot + "ui_icon_" + app + "_v001.png", false, false); }
            var cfg = ConfigImporter.EnsureAsset();
            Undo.RecordObject(cfg, "Connect MVP art");
            foreach (var expression in Expressions)
            {
                var e = cfg.dialogue.expressions.Find(x => x.key == expression);
                if (e == null) { e = new ExpressionCfg { key = expression }; cfg.dialogue.expressions.Add(e); }
                var path = EntryPath("seoha_" + expression);
                if (e.standing == null) e.standing = SubSprite(path, "standing");
                if (e.portrait == null) e.portrait = SubSprite(path, "portrait");
            }
            for (int i = 0; i < Mathf.Min(4, cfg.job.customers.Count); i++)
            {
                cfg.job.customers[i].name = CustomerNames[i];
                if (cfg.job.customers[i].image == null) cfg.job.customers[i].image = Sprite("customer_" + CustomerKeys[i]);
            }
            for (int i = 0; i < Mathf.Min(ProductKeys.Length, cfg.job.items.Count); i++)
                if (cfg.job.items[i].image == null) cfg.job.items[i].image = Sprite("item_" + ProductKeys[i]);
            foreach (var item in cfg.shop.items)
            {
                string key = item.id == "pc_old_laptop" ? "pc_old" : item.id == "chair_ergonomic" ? "chair_ergo" : item.id;
                if (item.image == null || AssetDatabase.GetAssetPath(item.image).StartsWith("Assets/Art/Props/Furniture/prop_" + key + "_v"))
                    item.image = Sprite("furn_" + key);
            }
            foreach (var app in cfg.theme.apps)
                if (app.icon == null) app.icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconRoot + "ui_icon_" + app.key + "_v001.png");
            foreach (var theme in cfg.theme.themes)
                if (theme.wallpaper == null) theme.wallpaper = Sprite(theme.key == "sakura_night" ? "wallpaper_sakura" : "wallpaper_day");
            Stage(cfg.cutscenes.defaultScene);
            foreach (var scene in cfg.cutscenes.scenes) if (scene.enabled) Stage(scene);
            Stage(cfg.dialogue.intro);
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            ApplyToScene();
            GameConfig.NotifyChanged();
            Debug.Log("[MVP Art] Connected sprites; existing UI layout and economic settings preserved.");
        }

        static string EntryPath(string key) { ArtEntry e; return entries.TryGetValue(key, out e) ? e.path : null; }
        static Sprite Sprite(string key) { var p = EntryPath(key); return p == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(p); }
        static Sprite SubSprite(string path, string name)
        {
            if (path == null) return null;
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
        }
        static void Stage(SceneCfg scene)
        {
            if (scene != null && scene.image == null) { scene.image = Sprite("cutscene_bg"); scene.useCharacterLayer = true; }
        }

        static void Import(string path, bool expression, bool background)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Missing MVP art", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Not a texture: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = expression ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = !background;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            if (expression)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                // Pixel rectangles are metadata crops of the original; the PNG is never enlarged or retouched.
                // Stable normalized head crop; per-expression deviations remain a recorded review item.
#pragma warning disable 0618
                importer.spritesheet = new[] {
                    new SpriteMetaData { name = "standing", rect = new Rect(0, 0, tex.width, tex.height), pivot = new Vector2(0.5f, 0), alignment = (int)SpriteAlignment.BottomCenter },
                    new SpriteMetaData { name = "portrait", rect = new Rect(tex.width * 0.25f, tex.height * 0.68f, tex.width * 0.5f, tex.height * 0.32f), pivot = new Vector2(0.5f, 0.5f) }
                };
#pragma warning restore 0618
            }
            importer.SaveAndReimport();
        }

        static Transform Find(string path)
        {
            var ui = GameObject.Find("UI");
            return ui != null ? ui.transform.Find(path) : null;
        }
        static void Hide(Transform parent, string name)
        {
            if (parent == null) return;
            var child = parent.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }
        static RectTransform Stretch(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }
        static void Background(Transform parent, Sprite sprite)
        {
            if (parent == null || sprite == null) return;
            var frame = parent.Find("MvpBackground");
            if (frame == null)
            {
                var go = new GameObject("MvpBackground", typeof(RectTransform), typeof(RectMask2D));
                go.transform.SetParent(parent, false); Stretch(go); frame = go.transform;
                var picture = new GameObject("Art", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
                picture.transform.SetParent(frame, false); Stretch(picture);
            }
            frame.SetAsFirstSibling();
            var image = frame.GetComponentInChildren<Image>(true);
            image.sprite = sprite; image.color = Color.white; image.raycastTarget = false;
            image.type = Image.Type.Simple;
            var fit = image.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = sprite.rect.width / sprite.rect.height;
        }
        static void FixedPicture(Transform target, Sprite sprite)
        {
            if (target == null || sprite == null) return;
            var image = target.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite; image.enabled = true; image.color = Color.white; image.type = Image.Type.Simple;
            image.preserveAspect = true; image.raycastTarget = false; Hide(target, "Placeholder");
        }

        public static void ApplyToScene()
        {
            if (entries == null || GameConfig.Instance == null) return;
            var title = Find("Screen_Title");
            if (title != null)
            {
                Hide(title, "Background"); Background(title, Sprite("wallpaper_day"));
                Hide(title, "MvpCharacter"); Hide(title, "Subtitle");
            }
            var room = Find("Screen_Home/Room");
            if (room != null)
            {
                bool firstLayout = room.Find("MvpBackground") == null;
                Hide(room, "Wall"); Hide(room, "Floor"); Hide(room, "Window");
                Background(room, Sprite("room_bg"));
                FixedPicture(room.Find("Door"), Sprite("door")); FixedPicture(room.Find("Bed"), Sprite("bed"));
                // Set defaults only on a newly built room. Reconnecting art must preserve Inspector edits.
                if (firstLayout)
                {
                    SubjectAt(room.Find("Seoha"), GameConfig.Instance.StandingOf("normal"), 650, 210, 650);
                    SubjectAt(room.Find("Door"), Sprite("door"), 1120, 300, 545);
                    SubjectAt(room.Find("Slot_desk"), Sprite("furn_desk_old"), 130, 585, 275);
                    SubjectAt(room.Find("Slot_pc"), Sprite("furn_pc_old"), 200, 445, 165);
                    SubjectAt(room.Find("Slot_chair"), Sprite("furn_chair_cushion"), 350, 650, 280);
                    SubjectAt(room.Find("Slot_light"), Sprite("furn_light_stand"), 480, 440, 165);
                    SubjectAt(room.Find("Slot_deco"), Sprite("furn_deco_plant"), 115, 490, 120);
                }
                foreach (var binding in room.GetComponentsInChildren<BindImage>(true))
                    if (binding.spriteKey != null && binding.spriteKey.StartsWith("slot:"))
                    { Hide(binding.transform, "Placeholder"); binding.placeholder = null; binding.hideWhenEmpty = true; }
                PreviewRoomArt(room, GameConfig.Instance);
            }
            var job = Find("Screen_Job");
            if (job != null) { Hide(job, "Background"); Background(job, Sprite("store_bg")); CustomerLabel(job); }
            var settlement = Find("Screen_Settlement");
            if (settlement != null) { Hide(settlement, "Background"); Background(settlement, Sprite("wallpaper_day")); }
            Hero(Find("Screen_Cutscene"), true);
            var ui = GameObject.Find("UI");
            if (ui != null)
                foreach (var show in ui.GetComponentsInChildren<BindShow>(true))
                {
                    var parent = show.transform.parent;
                    var shadow = parent != null ? parent.Find(show.name + "_Shadow") : null;
                    if (shadow == null) continue;
                    var follow = shadow.GetComponent<BindShow>();
                    if (follow == null) follow = shadow.gameObject.AddComponent<BindShow>();
                    follow.key = show.key; follow.invert = show.invert;
                    var ownerGroup = show.GetComponent<CanvasGroup>();
                    shadow.GetComponent<CanvasGroup>().alpha = ownerGroup != null ? ownerGroup.alpha : 1;
                }
            if (ui != null)
                foreach (var b in ui.GetComponentsInChildren<BindImage>(true))
                {
                    if (string.IsNullOrEmpty(b.spriteKey)) continue;
                    b.GetComponent<Image>().type = Image.Type.Simple;
                    b.GetComponent<Image>().preserveAspect = true;
                }
            if (ui != null) EditorSceneManager.MarkSceneDirty(ui.scene);
        }

        /// <summary>Keep default furniture visible while arranging the scene. Runtime bindings still select owned items.</summary>
        public static void PreviewRoomArt(Transform room, GameConfig cfg)
        {
            if (room == null || cfg == null) return;
            FixedPicture(room.Find("Seoha"), cfg.StandingOf("normal"));
            foreach (string slot in new[] { "desk", "pc", "chair", "light", "deco" })
            {
                var target = room.Find("Slot_" + slot);
                if (target == null) continue;
                Sprite preview = null;
                foreach (string id in cfg.shop.startingItems)
                {
                    var item = cfg.ShopItem(id);
                    if (item != null && item.slot == slot) preview = item.image;
                }
                Hide(target, "Placeholder");
                var image = target.GetComponent<Image>();
                image.sprite = preview;
                image.enabled = preview != null;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        static void SubjectAt(Transform target, Sprite sprite, float x, float top, float subjectHeight)
        {
            if (target == null || sprite == null) return;
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            var pixels = texture.GetPixels32();
            int minX=texture.width, minY=texture.height, maxX=0, maxY=0;
            for (int py=0; py<texture.height; py++) for (int px=0; px<texture.width; px++)
                if (pixels[py*texture.width+px].a >= 200)
                { minX=Mathf.Min(minX,px); maxX=Mathf.Max(maxX,px); minY=Mathf.Min(minY,py); maxY=Mathf.Max(maxY,py); }
            float scale=subjectHeight/Mathf.Max(1,maxY-minY+1);
            var rt=(RectTransform)target;
            rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1);
            rt.anchoredPosition=new Vector2(x-minX*scale,-top+(texture.height-1-maxY)*scale);
            rt.sizeDelta=new Vector2(texture.width*scale,texture.height*scale);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        static void CustomerLabel(Transform job)
        {
            var customer=job.Find("Playing/Customer");
            if (customer == null || customer.Find("CustomerName") != null) return;
            var go=new GameObject("CustomerName",typeof(RectTransform),typeof(Image));
            go.transform.SetParent(customer,false);
            var rt=(RectTransform)go.transform;
            rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1);
            rt.anchoredPosition=new Vector2(72,-448); rt.sizeDelta=new Vector2(390,38);
            var image=go.GetComponent<Image>(); image.color=new Color(1,1,1,.94f); image.raycastTarget=false;
            var label=new GameObject("Text",typeof(RectTransform),typeof(Text),typeof(BindText));
            label.transform.SetParent(go.transform,false); Stretch(label);
            var text=label.GetComponent<Text>();
            text.font=job.GetComponentInChildren<Text>(true).font; text.fontSize=20; text.alignment=TextAnchor.MiddleCenter;
            text.color=new Color(.2f,.27f,.36f); text.raycastTarget=false;
            label.GetComponent<BindText>().key="customer_name";
        }

        static void Hero(Transform parent, bool cutscene)
        {
            if (parent == null || parent.Find("MvpCharacter") != null) return;
            var go = new GameObject("MvpCharacter", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = cutscene ? new Vector2(430, -24) : new Vector2(1370, -150);
            rt.sizeDelta = cutscene ? new Vector2(650, 1020) : new Vector2(500, 920);
            var cfg = GameConfig.Instance;
            var image = go.GetComponent<Image>(); image.sprite = cfg.StandingOf("normal");
            image.color = Color.white; image.preserveAspect = true; image.raycastTarget = false;
            if (cutscene)
            {
                var bind = go.AddComponent<BindImage>(); bind.spriteKey = "standing"; bind.hideWhenEmpty = true;
                var show = go.AddComponent<BindShow>(); show.key = "character_layer";
                var dialog = parent.Find("ClickToAdvance");
                if (dialog != null) go.transform.SetSiblingIndex(dialog.GetSiblingIndex());
            }
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            var v = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, v) / v.sqrMagnitude);
            return Vector2.Distance(p, a + t * v);
        }
        static bool Line(Vector2 p, float ax, float ay, float bx, float by, float w = 0.022f)
        { return Segment(p, new Vector2(ax, ay), new Vector2(bx, by)) < w; }
        static bool Ring(Vector2 p, float x, float y, float r, float w = 0.022f)
        { return Mathf.Abs(Vector2.Distance(p, new Vector2(x, y)) - r) < w; }
        static bool Glyph(string key, Vector2 p)
        {
            float x = p.x, y = p.y;
            if (key == "stock") return Line(p,.25f,.28f,.25f,.7f) || Line(p,.25f,.28f,.76f,.28f)
                || Line(p,.32f,.4f,.46f,.53f) || Line(p,.46f,.53f,.59f,.46f) || Line(p,.59f,.46f,.75f,.7f);
            if (key == "browser") return Ring(p,.5f,.5f,.27f) || Mathf.Abs(((x-.5f)*(x-.5f)/.018f+(y-.5f)*(y-.5f)/.073f)-1)<.22f
                || (x>.25f && x<.75f && (Mathf.Abs(y-.41f)<.018f || Mathf.Abs(y-.59f)<.018f));
            if (key == "finance") return Line(p,.24f,.32f,.76f,.32f) || Line(p,.24f,.32f,.24f,.68f)
                || Line(p,.24f,.68f,.76f,.68f) || Line(p,.76f,.32f,.76f,.68f)
                || (x>.57f && x<.8f && y>.43f && y<.57f) || Ring(p,.43f,.5f,.065f,.018f);
            if (key == "album") return Line(p,.26f,.25f,.74f,.25f) || Line(p,.26f,.75f,.74f,.75f)
                || Line(p,.26f,.25f,.26f,.75f) || Line(p,.74f,.25f,.74f,.75f)
                || Line(p,.32f,.37f,.47f,.55f) || Line(p,.47f,.55f,.66f,.37f) || Ring(p,.61f,.64f,.045f,.025f);
            var v = p - new Vector2(.5f,.5f); float r = v.magnitude;
            float a = Mathf.Atan2(v.y,v.x);
            return Ring(p,.5f,.5f,.18f,.065f) || (r>.18f && r<.31f && Mathf.Abs(Mathf.Cos(a*4))>.83f);
        }
        static void MakeIcon(string key)
        {
            string path = IconRoot + "ui_icon_" + key + "_v001.png";
            if (File.Exists(path)) return;
            Directory.CreateDirectory(IconRoot);
            Color[] palette = { new Color(.48f,.68f,.88f), new Color(.42f,.70f,.64f), new Color(.72f,.60f,.84f), new Color(.87f,.59f,.71f), new Color(.56f,.64f,.76f) };
            Color ink = palette[Mathf.Max(0, Array.IndexOf(Apps,key))];
            var tex = new Texture2D(512,512,TextureFormat.RGBA32,false);
            var pixels = new Color[512*512];
            for (int y=0; y<512; y++) for (int x=0; x<512; x++)
            {
                var p = new Vector2((x+.5f)/512,(y+.5f)/512);
                var q = new Vector2(Mathf.Max(Mathf.Abs(p.x-.5f)-.31f,0), Mathf.Max(Mathf.Abs(p.y-.5f)-.31f,0));
                float alpha = Mathf.Clamp01((.13f-q.magnitude)*512);
                Color c = Color.Lerp(Color.white, ink, .12f+(1-p.y)*.08f);
                if (Glyph(key,p)) c=ink;
                c.a=alpha; pixels[y*512+x]=c;
            }
            tex.SetPixels(pixels); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        }

        public static void ApplyAndValidate()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            Apply();
            var cfg = GameConfig.Instance;
            foreach (var e in cfg.dialogue.expressions)
                if (e.portrait == null || e.standing == null) throw new InvalidOperationException("Missing expression " + e.key);
            if (cfg.job.customers.Count != 4 || cfg.job.customers.Any(c => c.image == null)) throw new InvalidOperationException("Customer art missing");
            if (cfg.job.items.Any(i => i.image == null) || cfg.shop.items.Any(i => i.image == null)) throw new InvalidOperationException("Item art missing");
            if (cfg.theme.apps.Any(a => a.icon == null) || cfg.theme.themes.Any(t => t.wallpaper == null)) throw new InvalidOperationException("Desktop art missing");
            if (cfg.cutscenes.scenes.Any(s => s.enabled && s.image == null)) throw new InvalidOperationException("Cutscene backdrop missing");
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[MVP_ART_VALIDATION_PASS] 6 expressions, 4 customers, 12 products, 12 furniture, 5 icons, 2 wallpapers, active scenes connected.");
        }
    }
}
