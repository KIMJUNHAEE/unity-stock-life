using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace StockGame
{
    /// <summary>배열 등으로 짧게 저장하고 싶은 타입이 구현한다.</summary>
    public interface ICompactJson
    {
        object ToCompact();
        void FromCompact(object data);
    }

    /// <summary>
    /// 저장 파일용 리플렉션 매퍼. public 필드만 대상으로 하며
    /// 기본형, string, enum, List&lt;T&gt;, Dictionary&lt;string,T&gt;, 중첩 클래스를 지원한다.
    /// 저장 파일에 없는 필드는 기본값을 유지해 버전이 바뀌어도 불러올 수 있다.
    /// </summary>
    public static class JsonMapper
    {
        public static object ToTree(object obj)
        {
            if (obj == null) return null;
            Type t = obj.GetType();
            if (obj is string || obj is bool || obj is double) return obj;
            if (obj is int || obj is long || obj is float || obj is short || obj is byte) return Convert.ToDouble(obj);
            if (obj is ulong) return ((ulong)obj).ToString();
            if (t.IsEnum) return obj.ToString();
            var compact = obj as ICompactJson;
            if (compact != null) return compact.ToCompact();
            var dict = obj as IDictionary;
            if (dict != null)
            {
                var d = new Dictionary<string, object>();
                foreach (DictionaryEntry kv in dict) d[kv.Key.ToString()] = ToTree(kv.Value);
                return d;
            }
            var list = obj as IList;
            if (list != null)
            {
                var l = new List<object>(list.Count);
                foreach (var item in list) l.Add(ToTree(item));
                return l;
            }
            var res = new Dictionary<string, object>();
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                res[f.Name] = ToTree(f.GetValue(obj));
            }
            return res;
        }

        public static T FromTree<T>(object tree) where T : class
        {
            return FromTree(tree, typeof(T)) as T;
        }

        public static object FromTree(object tree, Type t)
        {
            if (tree == null)
            {
                if (t.IsValueType) return Activator.CreateInstance(t);
                return null;
            }
            if (t == typeof(string)) return J.ToS(tree, null);
            if (t == typeof(bool)) return J.ToB(tree, false);
            if (t == typeof(double)) return J.ToD(tree, 0);
            if (t == typeof(float)) return (float)J.ToD(tree, 0);
            if (t == typeof(int)) return (int)Math.Round(J.ToD(tree, 0));
            if (t == typeof(long)) return (long)Math.Round(J.ToD(tree, 0));
            if (t == typeof(ulong))
            {
                ulong u;
                string s = J.ToS(tree, "0");
                return ulong.TryParse(s, out u) ? u : 0UL;
            }
            if (t.IsEnum)
            {
                try { return Enum.Parse(t, J.ToS(tree, ""), true); }
                catch (Exception) { return Activator.CreateInstance(t); }
            }
            if (typeof(ICompactJson).IsAssignableFrom(t))
            {
                var inst = (ICompactJson)Activator.CreateInstance(t);
                inst.FromCompact(tree);
                return inst;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type et = t.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(t);
                var src = tree as List<object>;
                if (src != null) foreach (var item in src) list.Add(FromTree(item, et));
                return list;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type vt = t.GetGenericArguments()[1];
                var dict = (IDictionary)Activator.CreateInstance(t);
                var src = tree as Dictionary<string, object>;
                if (src != null) foreach (var kv in src) dict[kv.Key] = FromTree(kv.Value, vt);
                return dict;
            }
            var obj = Activator.CreateInstance(t);
            var srcObj = tree as Dictionary<string, object>;
            if (srcObj == null) return obj;
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                object v;
                if (!srcObj.TryGetValue(f.Name, out v)) continue;
                f.SetValue(obj, FromTree(v, f.FieldType));
            }
            return obj;
        }

    }
}
