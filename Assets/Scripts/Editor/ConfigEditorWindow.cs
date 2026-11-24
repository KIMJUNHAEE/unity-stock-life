using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StockGame.EditorTools
{
    /// <summary>
    /// 메뉴 StockGame ▸ 콘텐츠 편집기.
    /// data/ JSON(회사·사건·뉴스·게시글·댓글·스토리)을 입력칸으로 고치고, 플레이 중 게임 상태를 조절한다.
    /// 수치·문구·이미지는 GameConfig 에셋(Inspector)에서 고친다.
    /// JSON 구조를 그대로 읽어 화면을 만들기 때문에 새 키를 추가해도 자동으로 보인다.
    /// </summary>
    public class ConfigEditorWindow : EditorWindow
    {
        class FileDoc
        {
            public string Name;
            public string Rel;
            public object Root;
            public bool Dirty;
            public string Error;
            public DateTime LastWrite;
            public double DirtySince;
        }

        static readonly string[,] Files =
        {
            { "companies", "data/companies.json" },
            { "events", "data/events.json" },
            { "news", "data/news.json" },
            { "posts", "data/community_posts.json" },
            { "comments", "data/community_comments.json" },
            { "story", "data/story_milestones.json" },
        };

        readonly List<FileDoc> docs = new List<FileDoc>();
        string page = "play";
        Vector2 scroll, sideScroll;
        string search = "";
        bool autoSave = true;
        readonly Dictionary<string, bool> folds = new Dictionary<string, bool>();
        readonly Dictionary<string, Texture2D> thumbs = new Dictionary<string, Texture2D>();
        readonly Dictionary<string, string> newKeyNames = new Dictionary<string, string>();
        Action pending;
        double lastPoll;
        GUIStyle wrapArea, helpLabel, sectionLabel;
        FileDoc cur;

        static string Root { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }

        [MenuItem("StockGame/콘텐츠 편집기 (뉴스·게시글 data) · 플레이 중 제어", false, 2)]
        public static void Open()
        {
            var w = GetWindow<ConfigEditorWindow>();
            w.titleContent = new GUIContent("StockGame 콘텐츠");
            w.minSize = new Vector2(560, 400);
            w.Show();
        }

        void OnEnable()
        {
            LoadAll();
            EditorApplication.update += Tick;
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            foreach (var d in docs) if (d.Dirty) Save(d);
            foreach (var t in thumbs.Values) if (t != null) DestroyImmediate(t);
            thumbs.Clear();
        }

        void LoadAll()
        {
            docs.Clear();
            for (int i = 0; i < Files.GetLength(0); i++)
            {
                var d = new FileDoc { Name = Files[i, 0], Rel = Files[i, 1] };
                Load(d);
                docs.Add(d);
            }
        }

        void Load(FileDoc d)
        {
            string full = Path.Combine(Root, d.Rel);
            try
            {
                d.Root = MiniJson.Parse(File.ReadAllText(full));
                d.LastWrite = File.GetLastWriteTimeUtc(full);
                d.Error = null;
                d.Dirty = false;
            }
            catch (Exception ex)
            {
                d.Error = ex.Message;
            }
        }

        void Save(FileDoc d)
        {
            if (d.Root == null) return;
            string full = Path.Combine(Root, d.Rel);
            try
            {
                File.WriteAllText(full, MiniJson.Serialize(d.Root, true) + "\n", new UTF8Encoding(false));
                d.LastWrite = File.GetLastWriteTimeUtc(full);
                d.Dirty = false;
                // 플레이 중이면 게임에 즉시 반영
                if (Application.isPlaying && Cfg.Store != null) Cfg.Store.Poll(999f, 0f);
            }
            catch (Exception ex)
            {
                d.Error = "저장 실패: " + ex.Message;
            }
        }

        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (autoSave)
                foreach (var d in docs)
                    if (d.Dirty && now - d.DirtySince > 0.4) { Save(d); Repaint(); }

            if (now - lastPoll > 1.0)
            {
                lastPoll = now;
                // 다른 프로그램(메모장 등)에서 저장하면 다시 읽기
                foreach (var d in docs)
                {
                    if (d.Dirty) continue;
                    string full = Path.Combine(Root, d.Rel);
                    try
                    {
                        if (File.Exists(full) && File.GetLastWriteTimeUtc(full) != d.LastWrite) { Load(d); Repaint(); }
                    }
                    catch (Exception) { }
                }
                if (Application.isPlaying && page == "play") Repaint();
            }
        }

        void MarkDirty()
        {
            if (cur == null) return;
            cur.Dirty = true;
            cur.DirtySince = EditorApplication.timeSinceStartup;
        }

        void InitStyles()
        {
            if (wrapArea != null) return;
            wrapArea = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            helpLabel = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            helpLabel.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            sectionLabel = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        }

        FileDoc Doc(string name)
        {
            foreach (var d in docs) if (d.Name == name) return d;
            return null;
        }

        // ───────────────────────── 화면 ─────────────────────────

        void OnGUI()
        {
            InitStyles();
            EditorGUILayout.BeginHorizontal();

            // 왼쪽 목록
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            sideScroll = EditorGUILayout.BeginScrollView(sideScroll);
            if (GUILayout.Button("★ 수치·문구 설정 (GameConfig) 열기")) ConfigImporter.SelectAsset();
            SideButton("play", Application.isPlaying ? "▶ 플레이 중 제어" : "▶ 플레이 중 제어 (꺼짐)");
            GUILayout.Space(8);
            GUILayout.Label("data (콘텐츠 원본)", EditorStyles.miniBoldLabel);
            foreach (var d in docs)
            {
                if (!d.Rel.StartsWith("data/")) continue;
                SideButton(d.Name, Title(d));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
            autoSave = EditorGUILayout.ToggleLeft("자동 저장", autoSave);
            if (GUILayout.Button("데이터 폴더 열기")) EditorUtility.RevealInFinder(Path.Combine(Root, "config/balance.json"));
            if (GUILayout.Button("파일에서 다시 읽기")) { LoadAll(); thumbs.Clear(); }
            EditorGUILayout.EndVertical();

            // 오른쪽 내용
            EditorGUILayout.BeginVertical();
            if (page == "quick") DrawQuick();
            else if (page == "play") DrawPlay();
            else DrawDoc(Doc(page));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (pending != null)
            {
                var p = pending;
                pending = null;
                p();
                MarkDirty();
                Repaint();
            }
        }

        string Title(FileDoc d)
        {
            string n;
            ConfigEditorLabels.FileNames.TryGetValue(d.Name, out n);
            return (d.Dirty ? "● " : "") + (d.Error != null ? "⚠ " : "") + (n ?? d.Name);
        }

        void SideButton(string id, string label)
        {
            var style = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleLeft, fixedHeight = 24 };
            bool on = page == id;
            if (GUILayout.Toggle(on, label, style) && !on) { page = id; scroll = Vector2.zero; GUI.FocusControl(null); }
        }

        void DrawDoc(FileDoc d)
        {
            if (d == null) return;
            cur = d;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(d.Rel + (d.Dirty ? "  (저장 안 됨)" : ""), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(220));
            if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(50))) Save(d);
            if (GUILayout.Button("되돌리기", EditorStyles.toolbarButton, GUILayout.Width(64))) Load(d);
            EditorGUILayout.EndHorizontal();

            if (d.Error != null) EditorGUILayout.HelpBox("JSON 오류: " + d.Error + "\n파일을 직접 고친 뒤 '파일에서 다시 읽기'를 누르세요.", MessageType.Error);
            if (Application.isPlaying) EditorGUILayout.HelpBox("플레이 중: 값을 바꾸면 바로 게임에 반영됩니다. (일부 값은 새 게임부터)", MessageType.Info);
            if (d.Root == null) return;

            scroll = EditorGUILayout.BeginScrollView(scroll);
            var obj = d.Root as Dictionary<string, object>;
            if (obj != null) DrawObjectBody(obj, "");
            EditorGUILayout.EndScrollView();
        }

        // ───────────────────────── 일반 JSON 그리기 ─────────────────────────

        GUIContent Label(string key, string path)
        {
            var info = ConfigEditorLabels.Find(cur.Name, path);
            if (info == null) return new GUIContent(key, path);
            return new GUIContent(info.Label + "  (" + key + ")", info.Help ?? path);
        }

        bool Matches(string path, object v)
        {
            if (string.IsNullOrEmpty(search)) return true;
            string s = search.ToLowerInvariant();
            if (path.ToLowerInvariant().Contains(s)) return true;
            var info = ConfigEditorLabels.Find(cur.Name, path);
            if (info != null && info.Label.ToLowerInvariant().Contains(s)) return true;
            var str = v as string;
            if (str != null) return str.ToLowerInvariant().Contains(s);
            var d = v as Dictionary<string, object>;
            if (d != null) { foreach (var kv in d) if (Matches(path + "." + kv.Key, kv.Value)) return true; return false; }
            var l = v as List<object>;
            if (l != null) { for (int i = 0; i < l.Count; i++) if (Matches(path + "[" + i + "]", l[i])) return true; return false; }
            return false;
        }

        bool Fold(string path, string label, bool def = false)
        {
            bool open;
            if (!folds.TryGetValue(path, out open)) open = def;
            if (!string.IsNullOrEmpty(search)) open = true;
            bool n = EditorGUILayout.Foldout(open, label, true);
            folds[path] = n;
            return n;
        }

        void DrawObjectBody(Dictionary<string, object> obj, string basePath)
        {
            var keys = new List<string>(obj.Keys);
            bool allChildrenObjects = keys.Count > 0;
            foreach (var k in keys) if (!k.StartsWith("_") && !(obj[k] is Dictionary<string, object>)) allChildrenObjects = false;

            foreach (var key in keys)
            {
                string path = basePath.Length == 0 ? key : basePath + "." + key;
                object v = obj[key];
                if (key.StartsWith("_"))
                {
                    if (v is string && string.IsNullOrEmpty(search)) EditorGUILayout.HelpBox((string)v, MessageType.None);
                    continue;
                }
                if (!Matches(path, v)) continue;
                object nv = DrawValue(key, path, v, allChildrenObjects ? obj : null);
                if (!ReferenceEquals(nv, v) && !Equals(nv, v)) { obj[key] = nv; MarkDirty(); }
            }

            // 자식이 모두 객체인 사전: 새 키 추가 (이미지 키, 사건 결과, 테마 등)
            if (allChildrenObjects && string.IsNullOrEmpty(search))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15);
                string nk;
                newKeyNames.TryGetValue(basePath, out nk);
                nk = EditorGUILayout.TextField(nk ?? "", GUILayout.Width(180));
                newKeyNames[basePath] = nk;
                GUI.enabled = !string.IsNullOrEmpty(nk) && !obj.ContainsKey(nk);
                if (GUILayout.Button("+ 항목 추가 (마지막 항목 복사)", GUILayout.Width(220)))
                {
                    string newKey = nk;
                    pending = () =>
                    {
                        object last = null;
                        foreach (var kv in obj) if (!kv.Key.StartsWith("_")) last = kv.Value;
                        obj[newKey] = last != null ? Clone(last) : new Dictionary<string, object>();
                        newKeyNames[basePath] = "";
                    };
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
        }

        static object Clone(object v) { return MiniJson.Parse(MiniJson.Serialize(v, false)); }

        static bool IsHexColor(string s)
        {
            if (string.IsNullOrEmpty(s) || s[0] != '#' || (s.Length != 7 && s.Length != 9)) return false;
            for (int i = 1; i < s.Length; i++) if (Uri.IsHexDigit(s[i]) == false) return false;
            return true;
        }

        object DrawValue(string key, string path, object v, Dictionary<string, object> removableParent)
        {
            var label = Label(key, path);

            if (v == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, new GUIContent("미정 (null)"));
                bool set = GUILayout.Button("숫자 넣기", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
                return set ? (object)0.0 : null;
            }
            if (v is bool) return EditorGUILayout.Toggle(label, (bool)v);
            if (v is double)
            {
                EditorGUILayout.BeginHorizontal();
                double nd = EditorGUILayout.DoubleField(label, (double)v);
                bool toNull = cur.Rel.StartsWith("data/") && GUILayout.Button("null", GUILayout.Width(44));
                EditorGUILayout.EndHorizontal();
                if (toNull) return null;
                return nd;
            }
            var str = v as string;
            if (str != null)
            {
                if (IsHexColor(str))
                {
                    Color c;
                    ColorUtility.TryParseHtmlString(str, out c);
                    Color nc = EditorGUILayout.ColorField(label, c);
                    if (nc != c) return "#" + (Mathf.Approximately(nc.a, 1f) ? ColorUtility.ToHtmlStringRGB(nc) : ColorUtility.ToHtmlStringRGBA(nc));
                    return str;
                }
                if (cur.Name == "assets" && key == "path") return DrawImagePath(label, str);
                if (key == "tone") { int idx = Array.IndexOf(Tones, str); int ni = EditorGUILayout.Popup(label.text, Math.Max(0, idx), Tones); return idx < 0 && ni == 0 ? str : Tones[ni]; }
                if (key == "op") { int idx = Array.IndexOf(Ops, str); int ni = EditorGUILayout.Popup(label.text, Math.Max(0, idx), Ops); return idx < 0 && ni == 0 ? str : Ops[ni]; }
                if (str.Length > 45 || str.Contains("\n"))
                {
                    EditorGUILayout.LabelField(label);
                    EditorGUI.indentLevel++;
                    var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, Mathf.Max(36, wrapArea.CalcHeight(new GUIContent(str), position.width - 260))));
                    string ns = EditorGUI.TextArea(rect, str, wrapArea);
                    EditorGUI.indentLevel--;
                    return ns;
                }
                return EditorGUILayout.TextField(label, str);
            }
            var list = v as List<object>;
            if (list != null) { DrawList(key, path, list, label); return list; }
            var obj = v as Dictionary<string, object>;
            if (obj != null)
            {
                EditorGUILayout.BeginHorizontal();
                bool open = Fold(path, label.text + ObjSummary(obj));
                if (removableParent != null && GUILayout.Button("삭제", EditorStyles.miniButton, GUILayout.Width(44)))
                {
                    string k = key;
                    pending = () => removableParent.Remove(k);
                }
                EditorGUILayout.EndHorizontal();
                if (open)
                {
                    EditorGUI.indentLevel++;
                    if (!string.IsNullOrEmpty(label.tooltip) && label.tooltip != path) EditorGUILayout.LabelField(label.tooltip, helpLabel);
                    DrawObjectBody(obj, path);
                    EditorGUI.indentLevel--;
                }
                return obj;
            }
            EditorGUILayout.LabelField(label, new GUIContent(v.ToString()));
            return v;
        }

        static readonly string[] Tones = { "positive", "neutral", "negative" };
        static readonly string[] Ops = { ">=", ">", "<=", "<", "==", "!=" };

        static string ObjSummary(Dictionary<string, object> o)
        {
            foreach (var k in new[] { "title", "name", "label", "text", "stat", "id" })
            {
                object v;
                if (o.TryGetValue(k, out v) && v is string) return "  —  " + Short((string)v);
            }
            return "";
        }

        static string Short(string s) { return s.Length > 40 ? s.Substring(0, 39) + "…" : s; }

        void DrawList(string key, string path, List<object> list, GUIContent label)
        {
            bool allNum = list.Count > 0, allScalar = true;
            foreach (var x in list)
            {
                if (!(x is double)) allNum = false;
                if (x is Dictionary<string, object> || x is List<object>) allScalar = false;
            }

            // [최소, 최대]
            if (allNum && list.Count == 2 && !path.Contains("layout"))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                int ind = EditorGUI.indentLevel; EditorGUI.indentLevel = 0;
                double a = EditorGUILayout.DoubleField((double)list[0]);
                GUILayout.Label("~", GUILayout.Width(14));
                double b = EditorGUILayout.DoubleField((double)list[1]);
                EditorGUI.indentLevel = ind;
                EditorGUILayout.EndHorizontal();
                if (a != (double)list[0] || b != (double)list[1]) { list[0] = a; list[1] = b; MarkDirty(); }
                return;
            }
            // 화면 좌표 [x, y, w, h]
            if (allNum && list.Count == 4 && path.Contains("layout"))
            {
                var r = new Rect((float)(double)list[0], (float)(double)list[1], (float)(double)list[2], (float)(double)list[3]);
                var nr = EditorGUILayout.RectField(label, r);
                if (nr != r) { list[0] = (double)nr.x; list[1] = (double)nr.y; list[2] = (double)nr.width; list[3] = (double)nr.height; MarkDirty(); }
                return;
            }

            EditorGUILayout.BeginHorizontal();
            bool open = Fold(path, label.text + "  [" + list.Count + "]", allScalar && list.Count <= 6);
            EditorGUILayout.EndHorizontal();
            if (!open) return;
            EditorGUI.indentLevel++;
            if (!string.IsNullOrEmpty(label.tooltip) && label.tooltip != path) EditorGUILayout.LabelField(label.tooltip, helpLabel);
            for (int i = 0; i < list.Count; i++)
            {
                string ip = path + "[" + i + "]";
                if (!Matches(ip, list[i]) && !Matches(path, null)) continue;
                int idx = i;
                var item = list[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                object nv;
                if (item is Dictionary<string, object>)
                {
                    var o = (Dictionary<string, object>)item;
                    bool io = Fold(ip, "#" + (i + 1) + ObjSummary(o));
                    if (io)
                    {
                        EditorGUI.indentLevel++;
                        DrawObjectBody(o, ip);
                        EditorGUI.indentLevel--;
                    }
                    nv = item;
                }
                else nv = DrawValue("#" + (i + 1), ip, item, null);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(22)) && idx > 0) pending = () => { var t = list[idx]; list[idx] = list[idx - 1]; list[idx - 1] = t; };
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(22)) && idx < list.Count - 1) pending = () => { var t = list[idx]; list[idx] = list[idx + 1]; list[idx + 1] = t; };
                if (GUILayout.Button("복제", EditorStyles.miniButtonMid, GUILayout.Width(36))) pending = () => list.Insert(idx + 1, Clone(list[idx]));
                if (GUILayout.Button("삭제", EditorStyles.miniButtonRight, GUILayout.Width(36))) pending = () => list.RemoveAt(idx);
                EditorGUILayout.EndHorizontal();
                if (!ReferenceEquals(nv, item) && !Equals(nv, item)) { list[i] = nv; MarkDirty(); }
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            if (GUILayout.Button("+ 추가", GUILayout.Width(80)))
                pending = () => list.Add(list.Count > 0 ? Clone(list[list.Count - 1]) : (object)"");
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        string DrawImagePath(GUIContent label, string rel)
        {
            EditorGUILayout.BeginHorizontal();
            string ns = EditorGUILayout.TextField(label, rel);
            if (GUILayout.Button("PNG 선택", GUILayout.Width(72)))
            {
                string start = Path.Combine(Root, "art");
                string picked = EditorUtility.OpenFilePanel("이미지 선택", Directory.Exists(start) ? start : Root, "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(picked)) ns = ToContentPath(picked, rel);
            }
            EditorGUILayout.EndHorizontal();
            var tex = Thumb(ns);
            EditorGUI.indentLevel++;
            if (tex != null)
            {
                var r = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, 72));
                r.width = 72f * tex.width / Mathf.Max(1, tex.height);
                GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            }
            else EditorGUILayout.LabelField("파일 없음 → 게임에서는 자리표시 상자로 보임", helpLabel);
            EditorGUI.indentLevel--;
            return ns;
        }

        /// <summary>데이터 폴더 밖의 파일이면 원래 경로 위치(없으면 art/)로 복사하고 상대 경로를 돌려준다.</summary>
        static string ToContentPath(string picked, string currentRel)
        {
            string root = Root.Replace('\\', '/').TrimEnd('/') + "/";
            string p = picked.Replace('\\', '/');
            if (p.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return p.Substring(root.Length);
            string relDir = string.IsNullOrEmpty(currentRel) ? "art" : Path.GetDirectoryName(currentRel).Replace('\\', '/');
            if (string.IsNullOrEmpty(relDir)) relDir = "art";
            string ext = Path.GetExtension(p);
            string name = string.IsNullOrEmpty(currentRel) ? Path.GetFileName(p) : Path.GetFileNameWithoutExtension(currentRel) + ext;
            string destRel = relDir + "/" + name;
            Directory.CreateDirectory(Path.Combine(Root, relDir));
            File.Copy(picked, Path.Combine(Root, destRel), true);
            return destRel;
        }

        Texture2D Thumb(string rel)
        {
            if (string.IsNullOrEmpty(rel)) return null;
            string full = Path.Combine(Root, rel);
            Texture2D t;
            string key = rel + "|" + (File.Exists(full) ? File.GetLastWriteTimeUtc(full).Ticks : 0);
            if (thumbs.TryGetValue(key, out t)) return t;
            t = null;
            if (File.Exists(full))
            {
                t = new Texture2D(2, 2);
                if (!t.LoadImage(File.ReadAllBytes(full))) { DestroyImmediate(t); t = null; }
            }
            thumbs[key] = t;
            return t;
        }

        // ───────────────────────── 빠른 조정 ─────────────────────────

        class Quick
        {
            public string File, Path, Label, Help;
            public float Min, Max;
            public bool Int;
            public Quick(string file, string path, string label, float min, float max, bool isInt, string help = null)
            { File = file; Path = path; Label = label; Min = min; Max = max; Int = isInt; Help = help; }
        }

        static readonly Quick[] QuickMarket =
        {
            new Quick("balance", "market.seconds_per_tick", "차트 속도: 시장 1분 = 실제 몇 초", 0.1f, 5f, false, "작을수록 빠름"),
            new Quick("balance", "market.base_volatility_per_tick", "가격 출렁임", 0.0005f, 0.02f, false),
            new Quick("balance", "market.mean_reversion_per_tick", "제자리로 돌아오는 힘", 0f, 0.05f, false),
            new Quick("balance", "market.market_factor_vol_per_tick", "시장 전체 흐름 세기", 0f, 0.01f, false),
            new Quick("balance", "trading.fee_rate", "수수료율", 0f, 0.01f, false, "0.001 = 0.1%"),
            new Quick("balance", "events.per_day_min", "하루 사건 수 (최소)", 0, 5, true),
            new Quick("balance", "events.per_day_max", "하루 사건 수 (최대)", 0, 6, true),
            new Quick("balance", "events.rumor_chance", "소문이 먼저 돌 확률", 0f, 1f, false),
        };
        static readonly Quick[] QuickMoney =
        {
            new Quick("balance", "economy.starting_cash", "시작 현금 (새 게임부터)", 0, 3000000, true),
            new Quick("balance", "loan.limit", "대출 한도", 0, 3000000, true),
            new Quick("balance", "loan.daily_interest_rate", "대출 하루 이자율", 0f, 0.05f, false),
            new Quick("balance", "loan.cash_threshold", "이 현금 미만일 때만 대출 (0=제한 없음)", 0, 1000000, true),
            new Quick("balance", "loan.term_days", "대출 만기 (일)", 1, 30, true),
        };
        static readonly Quick[] QuickLife =
        {
            new Quick("job", "duration_seconds", "알바 시간 (초)", 15, 240, true),
            new Quick("job", "base_pay", "알바 기본급", 0, 200000, true),
            new Quick("job", "bonus_per_customer", "손님당 보너스", 0, 20000, true),
            new Quick("job", "mistake_penalty", "실수당 감점", 0, 10000, true),
            new Quick("balance", "condition.drain_per_tick", "컨디션 소모 (시장 1분당)", 0f, 1.5f, false, "180분 × 0.3 = 54"),
            new Quick("job", "condition_cost", "알바 컨디션 소모", 0, 50, true),
        };
        static readonly Quick[] QuickUi =
        {
            new Quick("theme", "font_sizes.small", "작은 글자", 10, 40, true),
            new Quick("theme", "font_sizes.body", "본문 글자", 12, 44, true),
            new Quick("theme", "font_sizes.title", "제목 글자", 14, 56, true),
            new Quick("dialogue", "settings.bubble_seconds", "말풍선 표시 시간 (초)", 0.5f, 10f, false),
            new Quick("cutscenes", "settings.chars_per_second", "컷신 글자 속도", 5f, 120f, false),
        };

        void DrawQuick()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("자주 바꾸는 값", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("슬라이더를 움직이면 자동 저장되고, 플레이 중이면 즉시 반영됩니다. 더 많은 항목은 왼쪽 파일을 고르세요.", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            QuickGroup("시장", QuickMarket);
            QuickGroup("돈과 대출", QuickMoney);
            QuickGroup("알바와 컨디션", QuickLife);
            QuickGroup("화면", QuickUi);

            GUILayout.Space(10);
            GUILayout.Label("테마 색 (기본 파스텔)", sectionLabel);
            cur = Doc("theme");
            var colors = J.Path(cur != null ? cur.Root : null, "themes.pastel_day.colors") as Dictionary<string, object>;
            if (colors != null)
            {
                foreach (var k in new[] { "accent", "up", "down", "text", "panel", "desktop_top", "desktop_bottom", "room_wall", "room_floor" })
                {
                    object v;
                    if (!colors.TryGetValue(k, out v) || !(v is string)) continue;
                    object nv = DrawValue(k, "themes.pastel_day.colors." + k, v, null);
                    if (!Equals(nv, v)) { colors[k] = nv; MarkDirty(); }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void QuickGroup(string title, Quick[] items)
        {
            GUILayout.Space(8);
            GUILayout.Label(title, sectionLabel);
            foreach (var q in items)
            {
                var d = Doc(q.File);
                if (d == null || d.Root == null) continue;
                cur = d;
                double v = J.ToD(J.Path(d.Root, q.Path), double.NaN);
                if (double.IsNaN(v)) { EditorGUILayout.LabelField(q.Label, "(" + q.File + ".json 에 " + q.Path + " 없음)"); continue; }
                var lbl = new GUIContent(q.Label, (q.Help != null ? q.Help + "\n" : "") + q.File + ".json → " + q.Path);
                double nv;
                if (q.Int) nv = EditorGUILayout.IntSlider(lbl, (int)Math.Round(v), (int)q.Min, (int)q.Max);
                else nv = EditorGUILayout.Slider(lbl, (float)v, q.Min, q.Max);
                if (Math.Abs(nv - v) > 1e-9) { SetPath(d.Root, q.Path, q.Int ? Math.Round(nv) : Math.Round(nv, 5)); MarkDirty(); }
            }
        }

        static void SetPath(object root, string path, object value)
        {
            var parts = path.Split('.');
            var d = root as Dictionary<string, object>;
            for (int i = 0; i < parts.Length - 1 && d != null; i++)
            {
                object next;
                if (!d.TryGetValue(parts[i], out next) || !(next is Dictionary<string, object>))
                {
                    next = new Dictionary<string, object>();
                    d[parts[i]] = next;
                }
                d = (Dictionary<string, object>)next;
            }
            if (d != null) d[parts[parts.Length - 1]] = value;
        }

        // ───────────────────────── 플레이 중 제어 ─────────────────────────

        void DrawPlay()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("플레이 중 게임 상태", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (!Application.isPlaying) { EditorGUILayout.HelpBox("▶ 플레이를 누르면 여기서 현금·컨디션·시장 진행을 조절할 수 있습니다.", MessageType.Info); return; }
            var g = Game.I;
            if (g == null || g.S == null) { EditorGUILayout.HelpBox("타이틀 화면입니다. 새 게임이나 이어하기를 누르세요.", MessageType.Info); return; }
            var s = g.S;
            EditorGUILayout.LabelField("날짜", s.Day + "일차 · " + s.Phase + " · " + g.NowClock() + "  (틱 " + s.CurrentTick + " / 마감 " + s.SessionEnd + ")");
            EditorGUILayout.LabelField("자산", "투자 가능 " + Fmt.Won(g.InvestableAssets()) + " · 보유 평가 " + Fmt.Won(g.HoldingsValue()) + " · 대출 " + Fmt.Won(g.LoanOwed()));
            GUILayout.Space(6);
            long cash = EditorGUILayout.LongField("현금", s.Cash);
            if (cash != s.Cash) s.Cash = Math.Max(0, cash);
            float cond = EditorGUILayout.Slider("컨디션", (float)s.Condition, 0, (float)Cfg.D("balance", "condition.max_value", 100));
            if (Math.Abs(cond - s.Condition) > 0.01) s.Condition = cond;
            long debt = EditorGUILayout.LongField("남은 시작 부채", s.StartingDebt);
            if (debt != s.StartingDebt) s.StartingDebt = Math.Max(0, debt);
            g.UserPaused = EditorGUILayout.Toggle("시장 일시정지", g.UserPaused);
            if (GameRoot.I != null) GameRoot.I.desktop.Speed = EditorGUILayout.Slider("배속", GameRoot.I.desktop.Speed, 0.25f, 8f);

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = s.Phase == "morning";
            if (GUILayout.Button("알바 건너뛰고 저녁으로")) g.SkipJob();
            GUI.enabled = g.MarketOpenNow();
            if (GUILayout.Button("시장 30분 진행")) g.AdvanceTicks(30);
            if (GUILayout.Button("장 끝까지 진행")) g.AdvanceTicks(100000);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = s.Phase != "night" && GameRoot.I != null;
            if (GUILayout.Button("결산 화면 열기")) GameRoot.I.settlement.Open();
            GUI.enabled = s.Phase == "night";
            if (GUILayout.Button("잠자기 (다음 날)")) { g.Sleep(); if (GameRoot.I != null) GameRoot.I.Go(ScreenId.Home); }
            GUI.enabled = true;
            if (GUILayout.Button("저장")) g.Save();
            if (GUILayout.Button("설정 전체 다시 읽기") && GameRoot.I != null) GameRoot.I.ReloadAll();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("종목", sectionLabel);
            foreach (var id in s.Tradable)
            {
                StockState st;
                if (!s.Stocks.TryGetValue(id, out st)) continue;
                var h = g.HoldingOf(id);
                EditorGUILayout.LabelField(Game.CompanyName(id), Fmt.Won(st.Price) + " (전일 " + Fmt.Won(st.PrevClose) + ")" + (h != null ? " · 보유 " + h.Qty + "주" : ""));
            }
            GUILayout.Space(10);
            GUILayout.Label("배정된 사건 (스포일러)", sectionLabel);
            for (int i = s.Events.Count - 1; i >= 0 && i >= s.Events.Count - 12; i--)
            {
                var e = s.Events[i];
                string when = (e.OfficialTick <= s.CurrentTick ? "공개됨 " : "예정 ") + g.DayOfTick(e.OfficialTick) + "일 " + g.ClockOfTick(e.OfficialTick);
                EditorGUILayout.LabelField(Game.CompanyName(e.CompanyId) + " · " + e.OutcomeId, when + " · 영향 " + Fmt.Pct(Math.Exp(e.Impact) - 1) + (e.HasRumor ? " · 소문 있음" : ""), EditorStyles.miniLabel);
            }
        }
    }
}
