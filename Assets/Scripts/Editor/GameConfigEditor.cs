using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StockGame.EditorTools
{
    /// <summary>
    /// GameConfig 에셋 Inspector: 한국어 이름, 슬라이더, stat 드롭다운, 목록 추가·복제·삭제·순서 변경.
    /// 값을 바꾸면 플레이 중인 게임에도 바로 반영된다.
    /// </summary>
    [CustomEditor(typeof(GameConfig))]
    public class GameConfigEditor : Editor
    {
        GUIStyle wrapArea, help;
        string search = "";

        public override void OnInspectorGUI()
        {
            if (wrapArea == null)
            {
                wrapArea = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                help = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            }
            serializedObject.Update();

            EditorGUILayout.HelpBox("게임 수치·문구·이미지 설정입니다. 값을 바꾸면 플레이 중에도 바로 반영됩니다.\n" +
                                    "시작 현금·종목·장 길이처럼 회차 구조에 걸린 값은 새 게임부터 적용됩니다. 항목에 마우스를 올리면 설명이 나옵니다.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("config JSON 에서 다시 가져오기")) { if (EditorUtility.DisplayDialog("가져오기", "config 폴더의 JSON 값으로 덮어씁니다. 넣어 둔 이미지는 유지됩니다.", "가져오기", "취소")) ConfigImporter.ImportInto((GameConfig)target); }
            if (GUILayout.Button("JSON 으로 내보내기 (백업)")) ConfigImporter.Export((GameConfig)target);
            EditorGUILayout.EndHorizontal();
            search = EditorGUILayout.TextField("찾기", search);
            EditorGUILayout.Space(4);

            var it = serializedObject.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.name == "m_Script") continue;
                Draw(it.Copy(), true);
            }
            serializedObject.ApplyModifiedProperties();
        }

        // ───────────────────────── 리플렉션 ─────────────────────────

        FieldInfo FieldOf(SerializedProperty p)
        {
            Type t = target.GetType();
            FieldInfo fi = null;
            string path = p.propertyPath.Replace(".Array.data[", "[");
            foreach (var part in path.Split('.'))
            {
                string name = part;
                bool elem = false;
                int br = part.IndexOf('[');
                if (br >= 0) { name = part.Substring(0, br); elem = true; }
                fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi == null) return null;
                t = fi.FieldType;
                if (elem) t = t.IsArray ? t.GetElementType() : (t.IsGenericType ? t.GetGenericArguments()[0] : t);
            }
            return fi;
        }

        static T Attr<T>(FieldInfo f) where T : Attribute { return f == null ? null : (T)Attribute.GetCustomAttribute(f, typeof(T)); }

        static bool IsElement(SerializedProperty p) { return p.propertyPath.EndsWith("]"); }

        GUIContent LabelOf(SerializedProperty p, FieldInfo f)
        {
            if (IsElement(p)) return new GUIContent(ElementName(p));
            var l = Attr<LabelAttribute>(f);
            var tip = Attr<TooltipAttribute>(f);
            string tooltip = l != null && l.Help != null ? l.Help : (tip != null ? tip.tooltip : null);
            return new GUIContent(l != null ? l.Text : p.displayName, (tooltip != null ? tooltip + "\n" : "") + ConfigTree.Snake(p.name));
        }

        static string ElementName(SerializedProperty p)
        {
            int idx = 0;
            int lb = p.propertyPath.LastIndexOf('['), rb = p.propertyPath.LastIndexOf(']');
            if (lb >= 0 && rb > lb) int.TryParse(p.propertyPath.Substring(lb + 1, rb - lb - 1), out idx);
            if (p.propertyType == SerializedPropertyType.Generic)
            {
                foreach (var k in new[] { "title", "name", "label", "key", "id", "stat", "text" })
                {
                    var c = p.FindPropertyRelative(k);
                    if (c != null && c.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(c.stringValue))
                        return "#" + (idx + 1) + "  " + Short(c.stringValue);
                }
            }
            return "#" + (idx + 1);
        }

        static string Short(string s) { s = s.Replace("\n", " "); return s.Length > 40 ? s.Substring(0, 39) + "…" : s; }

        bool Matches(SerializedProperty p)
        {
            if (string.IsNullOrEmpty(search)) return true;
            string s = search.ToLowerInvariant();
            var f = FieldOf(p);
            var l = Attr<LabelAttribute>(f);
            if (p.name.ToLowerInvariant().Contains(s) || (l != null && l.Text.ToLowerInvariant().Contains(s))) return true;
            if (p.propertyType == SerializedPropertyType.String && p.stringValue != null && p.stringValue.ToLowerInvariant().Contains(s)) return true;
            if (p.hasVisibleChildren)
            {
                var c = p.Copy();
                var end = p.GetEndProperty();
                bool enter = true;
                while (c.NextVisible(enter) && !SerializedProperty.EqualContents(c, end))
                {
                    enter = true;
                    var cf = FieldOf(c);
                    var cl = Attr<LabelAttribute>(cf);
                    if (c.name.ToLowerInvariant().Contains(s) || (cl != null && cl.Text.ToLowerInvariant().Contains(s))) return true;
                    if (c.propertyType == SerializedPropertyType.String && c.stringValue != null && c.stringValue.ToLowerInvariant().Contains(s)) return true;
                }
            }
            return false;
        }

        // ───────────────────────── 그리기 ─────────────────────────

        void Draw(SerializedProperty p, bool topLevel = false)
        {
            if (!Matches(p)) return;
            var f = FieldOf(p);
            var label = LabelOf(p, f);
            bool searching = !string.IsNullOrEmpty(search);

            if (p.isArray && p.propertyType != SerializedPropertyType.String)
            {
                DrawList(p, label, f);
                return;
            }
            if (p.propertyType == SerializedPropertyType.Generic)
            {
                if (topLevel) { EditorGUILayout.Space(6); }
                p.isExpanded = EditorGUILayout.Foldout(p.isExpanded || searching, label, true, topLevel ? EditorStyles.foldoutHeader : EditorStyles.foldout);
                if (!p.isExpanded && !searching) return;
                EditorGUI.indentLevel++;
                var c = p.Copy();
                var end = p.GetEndProperty();
                bool enter = true;
                while (c.NextVisible(enter) && !SerializedProperty.EqualContents(c, end))
                {
                    enter = false;
                    Draw(c.Copy());
                }
                EditorGUI.indentLevel--;
                return;
            }
            DrawLeaf(p, label, f);
        }

        void DrawLeaf(SerializedProperty p, GUIContent label, FieldInfo f)
        {
            bool elem = IsElement(p);
            var range = elem ? null : Attr<RangeAttribute>(f);
            var area = elem ? null : Attr<TextAreaAttribute>(f);
            if (Attr<StatNameAttribute>(f) != null && p.propertyType == SerializedPropertyType.String)
            {
                var names = new List<string>(Game.StatNames);
                int idx = names.IndexOf(p.stringValue);
                if (idx < 0) { names.Insert(0, p.stringValue); idx = 0; }
                int ni = EditorGUILayout.Popup(label.text, idx, names.ToArray());
                if (ni != idx) p.stringValue = names[ni];
                return;
            }
            if (range != null && p.propertyType == SerializedPropertyType.Float) { p.floatValue = EditorGUILayout.Slider(label, p.floatValue, range.min, range.max); return; }
            if (range != null && p.propertyType == SerializedPropertyType.Integer) { p.intValue = EditorGUILayout.IntSlider(label, p.intValue, (int)range.min, (int)range.max); return; }
            if (p.propertyType == SerializedPropertyType.String && (area != null || (p.stringValue != null && p.stringValue.Length > 50)))
            {
                EditorGUILayout.LabelField(label);
                var r = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, Mathf.Max(36, wrapArea.CalcHeight(new GUIContent(p.stringValue), EditorGUIUtility.currentViewWidth - 80))));
                p.stringValue = EditorGUI.TextArea(r, p.stringValue, wrapArea);
                return;
            }
            if (p.propertyType == SerializedPropertyType.Vector2)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                int ind = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                var v = p.vector2Value;
                float a = EditorGUILayout.FloatField(v.x);
                GUILayout.Label("~", GUILayout.Width(14));
                float b = EditorGUILayout.FloatField(v.y);
                EditorGUI.indentLevel = ind;
                EditorGUILayout.EndHorizontal();
                p.vector2Value = new Vector2(a, b);
                return;
            }
            EditorGUILayout.PropertyField(p, label, false);
        }

        void DrawList(SerializedProperty p, GUIContent label, FieldInfo f)
        {
            bool searching = !string.IsNullOrEmpty(search);
            EditorGUILayout.BeginHorizontal();
            p.isExpanded = EditorGUILayout.Foldout(p.isExpanded || searching, new GUIContent(label.text + "  [" + p.arraySize + "]", label.tooltip), true);
            EditorGUILayout.EndHorizontal();
            if (!p.isExpanded && !searching) return;
            EditorGUI.indentLevel++;
            int remove = -1, up = -1, down = -1, dup = -1;
            for (int i = 0; i < p.arraySize; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                if (searching && !Matches(e) && !Matches(p)) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                Draw(e.Copy());
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(22))) up = i;
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(22))) down = i;
                if (GUILayout.Button("복제", EditorStyles.miniButtonMid, GUILayout.Width(36))) dup = i;
                if (GUILayout.Button("삭제", EditorStyles.miniButtonRight, GUILayout.Width(36))) remove = i;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            if (GUILayout.Button("+ 추가", GUILayout.Width(80)))
            {
                p.InsertArrayElementAtIndex(p.arraySize > 0 ? p.arraySize - 1 : 0);
                if (p.arraySize == 1) { }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
            if (up > 0) p.MoveArrayElement(up, up - 1);
            if (down >= 0 && down < p.arraySize - 1) p.MoveArrayElement(down, down + 1);
            if (dup >= 0) p.InsertArrayElementAtIndex(dup);
            if (remove >= 0) p.DeleteArrayElementAtIndex(remove);
        }
    }

    /// <summary>config/*.json ↔ GameConfig 에셋.</summary>
    public static class ConfigImporter
    {
        public const string AssetPath = "Assets/Resources/GameConfig.asset";
        static string Root { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }

        static object ReadJson(string rel)
        {
            string full = Path.Combine(Root, rel);
            if (!File.Exists(full)) return null;
            try { return MiniJson.Parse(File.ReadAllText(full)); }
            catch (Exception ex) { Debug.LogWarning("[StockGame] " + rel + " 읽기 실패: " + ex.Message); return null; }
        }

        [MenuItem("StockGame/설정 에셋 열기 (GameConfig)", false, 1)]
        public static void SelectAsset()
        {
            var cfg = EnsureAsset();
            Selection.activeObject = cfg;
            EditorGUIUtility.PingObject(cfg);
        }

        /// <summary>에셋이 없으면 만들고 config JSON 값을 채운다.</summary>
        public static GameConfig EnsureAsset()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetPath);
            if (cfg != null) return cfg;
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            cfg = ScriptableObject.CreateInstance<GameConfig>();
            ImportValues(cfg);
            AssetDatabase.CreateAsset(cfg, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[StockGame] " + AssetPath + " 를 만들었습니다 (config 폴더 JSON 값 사용). 클릭하면 Inspector 에서 고칠 수 있습니다.");
            return cfg;
        }

        public static void ImportInto(GameConfig cfg)
        {
            Undo.RecordObject(cfg, "config JSON 가져오기");
            ImportValues(cfg);
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            GameConfig.NotifyChanged();
            if (File.Exists("Assets/Art/mvp_art_manifest.json")) MvpArtSetup.Apply();
            if (MvpPlayUpdate.EnsureContent(cfg, new System.Collections.Generic.List<string>())) { EditorUtility.SetDirty(cfg); AssetDatabase.SaveAssets(); }
        }

        static void ImportValues(GameConfig cfg)
        {
            Fill(cfg.balance, "config/balance.json");
            Fill(cfg.job, "config/job.json");
            Fill(cfg.cutscenes, "config/cutscenes.json");
            Fill(cfg.shop, "config/shop.json");
            Fill(cfg.theme, "config/theme.json");
            Fill(cfg.names, "config/names.json");
            var dlg = ReadJson("config/dialogue.json");
            if (dlg != null)
            {
                ConfigTree.Fill(cfg.dialogue, dlg);
                // 표정: JSON 은 이미지 키 문자열이라 키 목록만 가져온다
                var keys = new List<string>();
                foreach (var kv in J.O(dlg, "expressions")) keys.Add(kv.Key);
                foreach (var k in keys)
                {
                    bool has = false;
                    foreach (var e in cfg.dialogue.expressions) if (e.key == k) has = true;
                    if (!has) cfg.dialogue.expressions.Add(new ExpressionCfg { key = k });
                }
            }
            if (cfg.dialogue.expressions.Count == 0)
                foreach (var k in new[] { "normal", "happy", "sad", "surprised", "tired", "focus" }) cfg.dialogue.expressions.Add(new ExpressionCfg { key = k });
            if (cfg.theme.apps.Count == 0)
                foreach (var a in new[] { "stock:증권", "browser:인터넷", "finance:금융", "album:밤의 기록", "settings:설정" })
                    cfg.theme.apps.Add(new AppCfg { key = a.Split(':')[0], name = a.Split(':')[1] });
        }

        static void Fill(object section, string rel)
        {
            var tree = ReadJson(rel);
            if (tree != null) ConfigTree.Fill(section, tree);
        }

        public static void Export(GameConfig cfg)
        {
            string dir = Path.Combine(Root, "config/exported");
            Directory.CreateDirectory(dir);
            foreach (var kv in cfg.Trees())
                File.WriteAllText(Path.Combine(dir, kv.Key + ".json"), MiniJson.Serialize(kv.Value, true) + "\n", new UTF8Encoding(false));
            EditorUtility.RevealInFinder(Path.Combine(dir, "balance.json"));
            Debug.Log("[StockGame] 현재 설정을 config/exported 에 내보냈습니다. (이미지는 포함되지 않음)");
        }
    }
}
