using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.EditorTools
{
    public enum Kind { Normal, Primary, Up, Down, Danger, Ghost, Tab }

    /// <summary>UI 생성기 공용 도구. 좌표는 부모 기준 왼쪽 위 (x, y, 너비, 높이), 1920×1080 설계 기준.</summary>
    public static partial class UIBuilder
    {
        static Font fontRegular, fontBold;
        static Sprite rounded, circle;
        static GameConfig cfg;

        static Color P(string key, Color def)
        {
            if (cfg != null)
            {
                var t = cfg.Theme(cfg.theme.defaultTheme);
                if (t != null) foreach (var nc in t.colors) if (nc.key == key) return nc.value;
            }
            return def;
        }
        static Color P(string key) { return P(key, Color.magenta); }

        static void LoadResources()
        {
            cfg = ConfigImporter.EnsureAsset();
            fontRegular = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/GyeonggiTitle/GyeonggiTitle-Medium.ttf");
            fontBold = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/GyeonggiTitle/GyeonggiTitle-Bold.ttf");
            var fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fontRegular == null) fontRegular = fallback;
            if (fontBold == null) fontBold = fontRegular;
            rounded = MakeSprite("Assets/Art/UI/Rounded.png", 64, 24, 28);
            circle = MakeSprite("Assets/Art/UI/Circle.png", 128, 64, 0);
        }

        /// <summary>둥근 모서리 흰색 스프라이트를 만들어 둔다 (9-slice).</summary>
        static Sprite MakeSprite(string path, int size, int radius, int border)
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = radius <= 0 ? 1 : Mathf.Clamp01(radius - d + 0.5f);
                        px[y * size + x] = new Color(1, 1, 1, a);
                    }
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteBorder.x != border))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.spriteBorder = new Vector4(border, border, border, border);
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ───────────────────────── 배치 ─────────────────────────

        static GameObject New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5; // UI
            return go;
        }

        static RectTransform TL(GameObject go, float x, float y, float w, float h)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        static RectTransform Stretch(GameObject go, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        static GameObject Node(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = New(name, parent);
            TL(go, x, y, w, h);
            return go;
        }

        static GameObject Full(Transform parent, string name)
        {
            var go = New(name, parent);
            Stretch(go);
            return go;
        }

        // ───────────────────────── 요소 ─────────────────────────

        static Image Box(Transform parent, string name, float x, float y, float w, float h, Color c, bool round = true, float radiusScale = 2f)
        {
            var go = Node(parent, name, x, y, w, h);
            return AddImage(go, c, round, radiusScale);
        }

        static Image AddImage(GameObject go, Color c, bool round = true, float radiusScale = 2f)
        {
            var img = go.AddComponent<Image>();
            img.color = c;
            if (round && rounded != null) { img.sprite = rounded; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = radiusScale; }
            img.raycastTarget = false;
            return img;
        }

        static Image Panel(Transform parent, string name, float x, float y, float w, float h, string colorKey = "panel")
        {
            var shadow = Box(parent, name + "_Shadow", x, y + 5, w, h, P("shadow", new Color(0, 0, 0, 0.1f)), true, 1.6f);
            shadow.raycastTarget = false;
            var img = Box(parent, name, x, y, w, h, P(colorKey, Color.white), true, 1.6f);
            img.raycastTarget = true;
            return img;
        }

        static Text Txt(Transform parent, string name, float x, float y, float w, float h, string text, int size, Color c,
                        TextAnchor a = TextAnchor.MiddleLeft, bool bold = false, string bind = null, string colorBind = null)
        {
            var go = Node(parent, name, x, y, w, h);
            return AddText(go, text, size, c, a, bold, bind, colorBind);
        }

        static Text AddText(GameObject go, string text, int size, Color c, TextAnchor a, bool bold, string bind, string colorBind)
        {
            var t = go.AddComponent<Text>();
            t.font = bold ? fontBold : fontRegular;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = a;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            if (bind != null || colorBind != null)
            {
                var b = go.AddComponent<BindText>();
                b.key = bind;
                b.colorKey = colorBind;
            }
            return t;
        }

        static Color KindColor(Kind k)
        {
            switch (k)
            {
                case Kind.Primary: return P("accent");
                case Kind.Up: return P("up");
                case Kind.Down: return P("down");
                case Kind.Danger: return P("danger");
                case Kind.Ghost: return new Color(1, 1, 1, 0);
                case Kind.Tab: return P("tab");
            }
            return P("button");
        }

        static Button Btn(Transform parent, string name, float x, float y, float w, float h, string label, string action,
                          Kind kind = Kind.Normal, int size = 22, string enableKey = null, string labelBind = null)
        {
            var go = Node(parent, name, x, y, w, h);
            return MakeButton(go, label, action, kind, size, enableKey, labelBind);
        }

        static Button MakeButton(GameObject go, string label, string action, Kind kind, int size, string enableKey, string labelBind)
        {
            var img = AddImage(go, KindColor(kind), true, 2f);
            img.raycastTarget = true;
            if (kind == Kind.Primary) go.AddComponent<ThemeColor>().colorKey = "accent";
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = ColorBlock.defaultColorBlock;
            cb.highlightedColor = new Color(0.93f, 0.95f, 1f, 1f);
            cb.pressedColor = new Color(0.82f, 0.86f, 0.92f, 1f);
            cb.disabledColor = new Color(1, 1, 1, 0.45f);
            if (kind == Kind.Ghost)
            {
                img.color = P("button_hover", new Color(0.84f, 0.91f, 0.97f));
                cb.normalColor = new Color(1, 1, 1, 0);
                cb.highlightedColor = Color.white;
                cb.selectedColor = new Color(1, 1, 1, 0);
            }
            b.colors = cb;
            var bb = go.AddComponent<BindButton>();
            bb.action = action;
            bb.enableKey = enableKey;
            bool light = kind == Kind.Primary || kind == Kind.Up || kind == Kind.Down || kind == Kind.Danger;
            var lt = New("Label", go.transform);
            Stretch(lt, 6, 0, 6, 0);
            AddText(lt, label, size, light ? P("text_on_accent", Color.white) : P("text"), TextAnchor.MiddleCenter, true, labelBind, null);
            return b;
        }

        /// <summary>선택 표시가 있는 탭/칩 버튼. selectedKey 가 참이면 강조 배경이 보인다.</summary>
        static Button TabBtn(Transform parent, string name, float x, float y, float w, float h, string label, string action, string selectedKey, int size = 20, string labelBind = null)
        {
            var go = Node(parent, name, x, y, w, h);
            return MakeTab(go, label, action, selectedKey, size, labelBind);
        }

        static Button MakeTab(GameObject go, string label, string action, string selectedKey, int size, string labelBind)
        {
            var img = AddImage(go, P("tab"), true, 2f);
            img.raycastTarget = true;
            var sel = New("Selected", go.transform);
            Stretch(sel);
            AddImage(sel, P("tab_active"), true, 2f).gameObject.AddComponent<ThemeColor>().colorKey = "tab_active";
            Show(sel, selectedKey);
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            go.AddComponent<BindButton>().action = action;
            var lt = New("Label", go.transform);
            Stretch(lt, 4, 0, 4, 0);
            AddText(lt, label, size, P("text"), TextAnchor.MiddleCenter, true, labelBind, null);
            return b;
        }

        /// <summary>CanvasGroup + BindShow. editVisible=false 면 편집 화면에서 기본으로 숨긴다 (Inspector 의 CanvasGroup Alpha 로 보이게 해서 편집).</summary>
        static BindShow Show(GameObject go, string key, bool invert = false, bool editVisible = true)
        {
            var g = go.GetComponent<CanvasGroup>();
            if (g == null) g = go.AddComponent<CanvasGroup>();
            g.alpha = editVisible ? 1 : 0;
            g.blocksRaycasts = editVisible;
            g.interactable = editVisible;
            var s = go.AddComponent<BindShow>();
            s.key = key;
            s.invert = invert;
            return s;
        }

        static Image Pic(Transform parent, string name, float x, float y, float w, float h, string spriteKey, string placeholder, string placeholderBind = null, Color? bg = null)
        {
            var go = Node(parent, name, x, y, w, h);
            var img = AddImage(go, bg ?? P("panel_alt", new Color(0.95f, 0.97f, 0.99f)), true, 2f);
            var ph = New("Placeholder", go.transform);
            Stretch(ph, 6, 6, 6, 6);
            AddText(ph, placeholder, 18, P("text_dim"), TextAnchor.MiddleCenter, false, placeholderBind, null);
            if (spriteKey != null)
            {
                var b = go.AddComponent<BindImage>();
                b.spriteKey = spriteKey;
                b.placeholder = ph;
            }
            return img;
        }

        static void Bar(Transform parent, string name, float x, float y, float w, float h, string valueKey, Color fg, string fgColorBind = null)
        {
            var bg = Box(parent, name, x, y, w, h, P("button"), true, 4f);
            var fill = New("Fill", bg.transform);
            Stretch(fill);
            var img = fill.AddComponent<Image>();
            img.color = fg;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 0.7f;
            img.raycastTarget = false;
            fill.AddComponent<BindFill>().key = valueKey;
            if (fgColorBind != null) fill.AddComponent<BindImage>().colorKey = fgColorBind;
        }

        static GradientGraphic Grad(Transform parent, string name, Color top, Color bottom, string topKey = null, string bottomKey = null)
        {
            var go = Full(parent, name);
            var g = go.AddComponent<GradientGraphic>();
            g.top = top;
            g.bottom = bottom;
            g.themeTopKey = topKey;
            g.themeBottomKey = bottomKey;
            g.raycastTarget = false;
            return g;
        }

        /// <summary>가장자리 흐림 4면 (컨디션). CanvasGroup 을 돌려준다.</summary>
        static CanvasGroup Vignette(Transform parent)
        {
            var go = Full(parent, "Vignette (컨디션 흐림)");
            var g = go.AddComponent<CanvasGroup>();
            g.alpha = 0;
            g.blocksRaycasts = false;
            g.interactable = false;
            Color v = P("vignette", new Color(0.3f, 0.36f, 0.48f));
            Color clear = new Color(v.r, v.g, v.b, 0);
            var l = Node(go.transform, "Left", 0, 0, 240, 1080).AddComponent<GradientGraphic>(); l.horizontal = true; l.top = v; l.bottom = clear; l.raycastTarget = false;
            var r = Node(go.transform, "Right", 1680, 0, 240, 1080).AddComponent<GradientGraphic>(); r.horizontal = true; r.top = clear; r.bottom = v; r.raycastTarget = false;
            var t = Node(go.transform, "Top", 0, 0, 1920, 150).AddComponent<GradientGraphic>(); t.top = v; t.bottom = clear; t.raycastTarget = false;
            var b = Node(go.transform, "Bottom", 0, 930, 1920, 150).AddComponent<GradientGraphic>(); b.top = clear; b.bottom = v; b.raycastTarget = false;
            return g;
        }

        /// <summary>
        /// 스크롤 목록. 반환한 template 아래에 항목 모양을 만든다.
        /// grid 가 있으면 격자, 없으면 세로 목록.
        /// </summary>
        static RectTransform ScrollList(Transform parent, string name, float x, float y, float w, float h, string listKey, float rowH, float spacing, Vector2? grid = null)
        {
            var root = Node(parent, name, x, y, w, h);
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(1, 1, 1, 0);
            rootImg.raycastTarget = true;
            var sr = root.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 40;
            sr.movementType = ScrollRect.MovementType.Clamped;
            var vp = Full(root.transform, "Viewport");
            vp.AddComponent<RectMask2D>();
            var content = New("Content", vp.transform);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = new Vector2(0, -h);
            crt.offsetMax = new Vector2(-10, 0);
            if (grid.HasValue)
            {
                var gl = content.AddComponent<GridLayoutGroup>();
                gl.cellSize = grid.Value;
                gl.spacing = new Vector2(spacing, spacing);
            }
            else
            {
                var vl = content.AddComponent<VerticalLayoutGroup>();
                vl.spacing = spacing;
                vl.childControlWidth = true;
                vl.childControlHeight = true;
                vl.childForceExpandWidth = true;
                vl.childForceExpandHeight = false;
            }
            var fit = content.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = (RectTransform)vp.transform;
            sr.content = crt;
            var tmpl = New("Template", content.transform);
            var le = tmpl.AddComponent<LayoutElement>();
            le.preferredHeight = rowH;
            le.minHeight = rowH;
            var list = root.AddComponent<UIList>();
            list.listKey = listKey;
            list.template = (RectTransform)tmpl.transform;
            list.content = crt;
            return (RectTransform)tmpl.transform;
        }

        /// <summary>스크롤 없는 가로/세로 줄 목록 (탭·버튼 묶음).</summary>
        static RectTransform RowList(Transform parent, string name, float x, float y, float w, float h, string listKey, float cell, float spacing, bool horizontal = true, TextAnchor align = TextAnchor.UpperLeft)
        {
            var root = Node(parent, name, x, y, w, h);
            HorizontalOrVerticalLayoutGroup lg;
            if (horizontal) lg = root.AddComponent<HorizontalLayoutGroup>(); else lg = root.AddComponent<VerticalLayoutGroup>();
            lg.spacing = spacing;
            lg.childAlignment = align;
            lg.childControlWidth = true;
            lg.childControlHeight = true;
            lg.childForceExpandWidth = false;
            lg.childForceExpandHeight = false;
            var tmpl = New("Template", root.transform);
            var le = tmpl.AddComponent<LayoutElement>();
            if (horizontal) { le.preferredWidth = cell; le.preferredHeight = h; }
            else { le.preferredHeight = cell; le.preferredWidth = w; }
            var list = root.AddComponent<UIList>();
            list.listKey = listKey;
            list.template = (RectTransform)tmpl.transform;
            list.content = (RectTransform)root.transform;
            return (RectTransform)tmpl.transform;
        }

        /// <summary>목록 항목 전체를 버튼으로 만든다.</summary>
        static void RowButton(RectTransform tmpl, string action, Color bg, bool hoverTint = true)
        {
            var img = tmpl.gameObject.AddComponent<Image>();
            img.color = bg;
            if (rounded != null) { img.sprite = rounded; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 2f; }
            img.raycastTarget = true;
            var b = tmpl.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = ColorBlock.defaultColorBlock;
            cb.highlightedColor = hoverTint ? new Color(0.9f, 0.94f, 1f, 1f) : Color.white;
            b.colors = cb;
            tmpl.gameObject.AddComponent<BindButton>().action = action;
        }

        /// <summary>왼쪽 라벨 + 오른쪽 값 한 줄.</summary>
        static void Pair(Transform parent, string name, float x, float y, float w, float h, string label, string valueBind, int size = 21, string colorBind = null, bool bold = false)
        {
            Txt(parent, name + "_Label", x, y, w, h, label, size, P("text_dim"));
            Txt(parent, name + "_Value", x, y, w, h, "0원", size, P("text"), TextAnchor.MiddleRight, bold, valueBind, colorBind);
        }
    }
}
