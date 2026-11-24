using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StockGame
{
    /// <summary>JSON 문법 오류. 줄/열 정보를 담아 화면에 그대로 보여준다.</summary>
    public class JsonParseException : Exception
    {
        public int Line;
        public int Column;
        public JsonParseException(string message, int line, int column)
            : base(message + " (" + line + "줄 " + column + "열)")
        {
            Line = line;
            Column = column;
        }
    }

    /// <summary>
    /// 외부 패키지 없이 쓰는 작은 JSON 파서/작성기.
    /// 객체 → Dictionary&lt;string, object&gt;, 배열 → List&lt;object&gt;, 숫자 → double, null → null.
    /// JsonUtility와 달리 null을 0으로 바꾸지 않는다(기획상 null = 미정).
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text)
        {
            var p = new Parser(text);
            p.SkipWs();
            object v = p.ReadValue();
            p.SkipWs();
            if (!p.End) p.Fail("JSON 끝에 불필요한 문자가 있습니다");
            return v;
        }

        public static string Serialize(object value, bool pretty)
        {
            var sb = new StringBuilder(1024);
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        class Parser
        {
            readonly string s;
            int i;
            public Parser(string text) { s = text ?? ""; i = 0; if (s.Length > 0 && s[0] == '﻿') i = 1; }
            public bool End { get { return i >= s.Length; } }

            public void Fail(string msg)
            {
                int line = 1, col = 1;
                for (int k = 0; k < i && k < s.Length; k++)
                {
                    if (s[k] == '\n') { line++; col = 1; } else col++;
                }
                throw new JsonParseException(msg, line, col);
            }

            public void SkipWs()
            {
                while (i < s.Length)
                {
                    char c = s[i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') { i++; continue; }
                    // 편집 편의를 위해 // 주석과 /* */ 주석을 허용한다.
                    if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
                    {
                        while (i < s.Length && s[i] != '\n') i++;
                        continue;
                    }
                    if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
                    {
                        i += 2;
                        while (i + 1 < s.Length && !(s[i] == '*' && s[i + 1] == '/')) i++;
                        i += 2;
                        continue;
                    }
                    break;
                }
            }

            public object ReadValue()
            {
                if (End) Fail("값이 필요합니다");
                char c = s[i];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                        Fail("알 수 없는 문자 '" + c + "'");
                        return null;
                }
            }

            void Expect(string word)
            {
                if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) Fail(word + " 이(가) 필요합니다");
                i += word.Length;
            }

            Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>();
                i++; // {
                SkipWs();
                if (!End && s[i] == '}') { i++; return d; }
                while (true)
                {
                    SkipWs();
                    if (End || s[i] != '"') Fail("객체 키는 \"따옴표\"로 감싸야 합니다");
                    string key = ReadString();
                    SkipWs();
                    if (End || s[i] != ':') Fail("키 뒤에 ':' 가 필요합니다");
                    i++;
                    SkipWs();
                    d[key] = ReadValue();
                    SkipWs();
                    if (End) Fail("객체가 '}' 로 닫히지 않았습니다");
                    if (s[i] == ',')
                    {
                        i++;
                        SkipWs();
                        if (!End && s[i] == '}') { i++; return d; } // 끝의 쉼표 허용
                        continue;
                    }
                    if (s[i] == '}') { i++; return d; }
                    Fail("',' 또는 '}' 가 필요합니다");
                }
            }

            List<object> ReadArray()
            {
                var list = new List<object>();
                i++; // [
                SkipWs();
                if (!End && s[i] == ']') { i++; return list; }
                while (true)
                {
                    SkipWs();
                    list.Add(ReadValue());
                    SkipWs();
                    if (End) Fail("배열이 ']' 로 닫히지 않았습니다");
                    if (s[i] == ',')
                    {
                        i++;
                        SkipWs();
                        if (!End && s[i] == ']') { i++; return list; }
                        continue;
                    }
                    if (s[i] == ']') { i++; return list; }
                    Fail("',' 또는 ']' 가 필요합니다");
                }
            }

            string ReadString()
            {
                var sb = new StringBuilder();
                i++; // "
                while (true)
                {
                    if (End) Fail("문자열이 닫히지 않았습니다");
                    char c = s[i++];
                    if (c == '"') break;
                    if (c == '\\')
                    {
                        if (End) Fail("잘못된 이스케이프");
                        char e = s[i++];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (i + 4 > s.Length) Fail("잘못된 \\u 이스케이프");
                                sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                                i += 4;
                                break;
                            default: Fail("잘못된 이스케이프 \\" + e); break;
                        }
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            object ReadNumber()
            {
                int start = i;
                if (s[i] == '-') i++;
                while (i < s.Length)
                {
                    char c = s[i];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') i++;
                    else break;
                }
                double d;
                string num = s.Substring(start, i - start);
                if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    Fail("숫자를 읽을 수 없습니다: " + num);
                return d;
            }
        }

        static void Indent(StringBuilder sb, bool pretty, int depth)
        {
            if (!pretty) return;
            sb.Append('\n');
            sb.Append(' ', depth * 2);
        }

        static void Write(StringBuilder sb, object v, bool pretty, int depth)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is string) { WriteString(sb, (string)v); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is double)
            {
                double d = (double)v;
                if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append("null"); return; }
                if (Math.Abs(d - Math.Round(d)) < 1e-9 && Math.Abs(d) < 9e15) sb.Append(((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture));
                else sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            if (v is float) { Write(sb, (double)(float)v, pretty, depth); return; }
            if (v is int || v is long || v is short || v is byte || v is uint)
            {
                sb.Append(Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture));
                return;
            }
            var dict = v as IDictionary;
            if (dict != null)
            {
                sb.Append('{');
                bool first = true;
                foreach (DictionaryEntry kv in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Indent(sb, pretty, depth + 1);
                    WriteString(sb, kv.Key.ToString());
                    sb.Append(pretty ? ": " : ":");
                    Write(sb, kv.Value, pretty, depth + 1);
                }
                if (!first) Indent(sb, pretty, depth);
                sb.Append('}');
                return;
            }
            var list = v as IList;
            if (list != null)
            {
                sb.Append('[');
                bool allScalar = true;
                foreach (var item in list)
                {
                    if (item is IDictionary || item is IList) { allScalar = false; break; }
                }
                for (int k = 0; k < list.Count; k++)
                {
                    if (k > 0) sb.Append(allScalar && pretty ? ", " : ",");
                    if (!allScalar) Indent(sb, pretty, depth + 1);
                    Write(sb, list[k], pretty, depth + 1);
                }
                if (!allScalar && list.Count > 0) Indent(sb, pretty, depth);
                sb.Append(']');
                return;
            }
            WriteString(sb, v.ToString());
        }

        static void WriteString(StringBuilder sb, string str)
        {
            sb.Append('"');
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
