using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace StockGame
{
    /// <summary>List 를 JSON 객체(사전)로 바꿔 저장. 원소의 key 필드가 사전 키가 된다. Value 가 있으면 그 필드만 값으로 쓴다.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AsDictAttribute : Attribute
    {
        public string Value;
        public AsDictAttribute() { }
        public AsDictAttribute(string valueField) { Value = valueField; }
    }

    /// <summary>Inspector 에 보여줄 한국어 이름과 설명.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LabelAttribute : Attribute
    {
        public string Text;
        public string Help;
        public LabelAttribute(string text, string help = null) { Text = text; Help = help; }
    }

    /// <summary>컷신·테마 조건의 stat 이름 칸에 드롭다운을 붙인다.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class StatNameAttribute : Attribute { }

    /// <summary>직접 트리를 만드는 타입.</summary>
    public interface IConfigTree
    {
        object ToTree();
        void FromTree(object tree);
    }

    /// <summary>
    /// GameConfig(인스펙터 값) ↔ JSON 형태 트리 변환.
    /// 필드 이름 camelCase → snake_case, Vector2 → [x, y], Rect → [x, y, w, h], Color → "#RRGGBBAA".
    /// 게임 규칙 코드는 이 트리를 Cfg 로 읽으므로 인스펙터 값이 바뀌면 그대로 반영된다.
    /// </summary>
    public static class ConfigTree
    {
        static readonly Dictionary<string, string> snakeCache = new Dictionary<string, string>();

        public static string Snake(string name)
        {
            string s;
            if (snakeCache.TryGetValue(name, out s)) return s;
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsUpper(c)) { if (sb.Length > 0) sb.Append('_'); sb.Append(char.ToLowerInvariant(c)); }
                else sb.Append(c);
            }
            s = sb.ToString();
            snakeCache[name] = s;
            return s;
        }

        static double F(float f) { return double.Parse(f.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture); }

        public static object ToTree(object o)
        {
            if (o == null) return null;
            if (o is UnityEngine.Object) return null;
            var custom = o as IConfigTree;
            if (custom != null) return custom.ToTree();
            if (o is string || o is bool || o is double) return o;
            if (o is float) return F((float)o);
            if (o is int || o is long) return Convert.ToDouble(o);
            if (o is Enum) return o.ToString();
            if (o is Vector2) { var v = (Vector2)o; return new List<object> { F(v.x), F(v.y) }; }
            if (o is Rect) { var r = (Rect)o; return new List<object> { F(r.x), F(r.y), F(r.width), F(r.height) }; }
            if (o is Color)
            {
                var c = (Color)o;
                return "#" + (Mathf.Approximately(c.a, 1f) ? ColorUtility.ToHtmlStringRGB(c) : ColorUtility.ToHtmlStringRGBA(c));
            }
            var list = o as IList;
            if (list != null)
            {
                var l = new List<object>(list.Count);
                foreach (var x in list) l.Add(ToTree(x));
                return l;
            }
            var d = new Dictionary<string, object>();
            foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType)) continue;
                object v = f.GetValue(o);
                var asDict = (AsDictAttribute)Attribute.GetCustomAttribute(f, typeof(AsDictAttribute));
                if (asDict != null && v is IList) d[Snake(f.Name)] = ListToDict((IList)v, asDict.Value);
                else d[Snake(f.Name)] = ToTree(v);
            }
            return d;
        }

        static Dictionary<string, object> ListToDict(IList list, string valueField)
        {
            var d = new Dictionary<string, object>();
            foreach (var item in list)
            {
                if (item == null) continue;
                var keyField = item.GetType().GetField("key");
                if (keyField == null) continue;
                string key = keyField.GetValue(item) as string;
                if (string.IsNullOrEmpty(key)) continue;
                if (valueField != null)
                {
                    var vf = item.GetType().GetField(valueField);
                    d[key] = vf != null ? ToTree(vf.GetValue(item)) : null;
                }
                else
                {
                    var tree = ToTree(item) as Dictionary<string, object>;
                    if (tree != null) tree.Remove("key");
                    d[key] = tree;
                }
            }
            return d;
        }

        /// <summary>JSON 트리 값을 기존 객체에 채운다 (가져오기용). 맞지 않는 값은 건너뛴다.</summary>
        public static void Fill(object target, object tree)
        {
            if (target == null) return;
            var custom = target as IConfigTree;
            if (custom != null) { custom.FromTree(tree); return; }
            var src = tree as Dictionary<string, object>;
            if (src == null) return;
            foreach (var f in target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                object v;
                if (!src.TryGetValue(Snake(f.Name), out v)) continue;
                var asDict = (AsDictAttribute)Attribute.GetCustomAttribute(f, typeof(AsDictAttribute));
                try
                {
                    object nv = asDict != null ? DictToList(v as Dictionary<string, object>, f.FieldType, asDict.Value, f.GetValue(target) as IList)
                                               : ConvertValue(v, f.FieldType, f.GetValue(target));
                    if (nv != NoValue) f.SetValue(target, nv);
                }
                catch (Exception) { }
            }
        }

        static readonly object NoValue = new object();

        static object DictToList(Dictionary<string, object> d, Type listType, string valueField, IList existing)
        {
            if (d == null || !listType.IsGenericType) return NoValue;
            Type et = listType.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(listType);
            foreach (var kv in d)
            {
                object item = null;
                // 이미 있는 같은 키 항목(스프라이트 등)을 살린다
                if (existing != null)
                    foreach (var ex in existing)
                    {
                        var kf = ex != null ? ex.GetType().GetField("key") : null;
                        if (kf != null && (kf.GetValue(ex) as string) == kv.Key) { item = ex; break; }
                    }
                if (item == null) item = Activator.CreateInstance(et);
                var keyField = et.GetField("key");
                if (keyField != null) keyField.SetValue(item, kv.Key);
                if (valueField != null)
                {
                    var vf = et.GetField(valueField);
                    if (vf != null)
                    {
                        object nv = ConvertValue(kv.Value, vf.FieldType, vf.GetValue(item));
                        if (nv != NoValue) vf.SetValue(item, nv);
                    }
                }
                else Fill(item, kv.Value);
                list.Add(item);
            }
            return list;
        }

        static object ConvertValue(object v, Type t, object existing)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return NoValue;
            if (v == null) return t.IsValueType ? NoValue : null;
            if (t == typeof(string)) return J.ToS(v, "");
            if (t == typeof(bool)) return J.ToB(v, false);
            if (t == typeof(float)) return (float)J.ToD(v, 0);
            if (t == typeof(double)) return J.ToD(v, 0);
            if (t == typeof(int)) return (int)Math.Round(J.ToD(v, 0));
            if (t == typeof(long)) return (long)Math.Round(J.ToD(v, 0));
            if (t.IsEnum)
            {
                try { return Enum.Parse(t, J.ToS(v, ""), true); } catch (Exception) { return NoValue; }
            }
            if (t == typeof(Vector2))
            {
                var l = v as List<object>;
                if (l != null && l.Count >= 2) return new Vector2((float)J.ToD(l[0], 0), (float)J.ToD(l[1], 0));
                if (J.IsNumber(v)) { float f = (float)J.ToD(v, 0); return new Vector2(f, f); }
                return NoValue;
            }
            if (t == typeof(Rect))
            {
                var l = v as List<object>;
                if (l != null && l.Count >= 4) return new Rect((float)J.ToD(l[0], 0), (float)J.ToD(l[1], 0), (float)J.ToD(l[2], 0), (float)J.ToD(l[3], 0));
                return NoValue;
            }
            if (t == typeof(Color))
            {
                Color c;
                if (ColorUtility.TryParseHtmlString(J.ToS(v, ""), out c)) return c;
                return NoValue;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                var src = v as List<object>;
                if (src == null) return NoValue;
                Type et = t.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(t);
                var old = existing as IList;
                for (int i = 0; i < src.Count; i++)
                {
                    object prev = old != null && i < old.Count ? old[i] : null;
                    object item;
                    if (et.IsClass && et != typeof(string))
                    {
                        item = prev ?? Activator.CreateInstance(et);
                        Fill(item, src[i]);
                    }
                    else
                    {
                        item = ConvertValue(src[i], et, prev);
                        if (item == NoValue) continue;
                    }
                    list.Add(item);
                }
                return list;
            }
            if (t.IsClass)
            {
                object obj = existing ?? Activator.CreateInstance(t);
                Fill(obj, v);
                return obj;
            }
            return NoValue;
        }
    }
}
