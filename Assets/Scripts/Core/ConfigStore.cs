using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 편집 가능한 데이터 폴더(data/, config/, art/)가 어디 있는지 정한다.
    /// 1) 실행 인자 -contentRoot "경로"
    /// 2) StreamingAssets 안에 config/balance.json 이 있으면 그곳 (빌드한 게임)
    /// 3) 프로젝트 루트(Assets 폴더의 상위) — 에디터
    /// </summary>
    public static class ContentPaths
    {
        public static string Root { get; private set; }

        public static void Resolve()
        {
            string fromArgs = null;
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == "-contentRoot") fromArgs = args[i + 1];
            }
            catch (Exception) { }

            if (!string.IsNullOrEmpty(fromArgs) && Directory.Exists(fromArgs)) { Root = Path.GetFullPath(fromArgs); return; }

            string sa = Application.streamingAssetsPath;
            if (!string.IsNullOrEmpty(sa) && File.Exists(Path.Combine(sa, "config/balance.json"))) { Root = sa; return; }

            Root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        public static string Full(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return Root;
            if (Path.IsPathRooted(relative)) return relative;
            return Path.Combine(Root, relative.Replace('\\', '/'));
        }
    }

    /// <summary>
    /// JSON 파일 묶음을 읽고, 저장(수정 시각 변경)을 감지해 다시 읽는다.
    /// 문법 오류가 있으면 이전 내용을 유지하고 오류를 보고한다.
    /// </summary>
    public class ConfigStore
    {
        public class Entry
        {
            public string Name;
            public string RelPath;
            public object Root;
            public DateTime LastWrite;
            public string Error;
            public bool Loaded;
        }

        public readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        public readonly List<string> Log = new List<string>();
        public int Version { get; private set; }
        public event Action<List<string>> Reloaded;

        float pollTimer;

        public void Register(string name, string relPath)
        {
            Entries[name] = new Entry { Name = name, RelPath = relPath };
        }

        /// <summary>파일이 아닌 곳(GameConfig 에셋)에서 만든 트리를 등록한다.</summary>
        public void Inject(string name, object root)
        {
            Entry e;
            if (!Entries.TryGetValue(name, out e)) { e = new Entry { Name = name }; Entries[name] = e; }
            e.RelPath = "(GameConfig) " + name;
            e.Root = root;
            e.Loaded = true;
            e.Error = null;
        }

        public void BumpVersion(List<string> changed)
        {
            Version++;
            if (Reloaded != null) Reloaded(changed);
        }

        public object Get(string name)
        {
            Entry e;
            return Entries.TryGetValue(name, out e) ? e.Root : null;
        }

        public void LoadAll()
        {
            var changed = new List<string>();
            foreach (var e in Entries.Values) if (!e.RelPath.StartsWith("(") && Load(e)) changed.Add(e.Name);
            Version++;
            if (Reloaded != null) Reloaded(changed);
        }

        bool Load(Entry e)
        {
            string full = ContentPaths.Full(e.RelPath);
            try
            {
                if (!File.Exists(full))
                {
                    e.Error = "파일 없음: " + e.RelPath;
                    AddLog(e.Error);
                    return false;
                }
                e.LastWrite = File.GetLastWriteTimeUtc(full);
                string text = File.ReadAllText(full);
                object root = MiniJson.Parse(text);
                e.Root = root;
                e.Error = null;
                e.Loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                e.Error = e.RelPath + ": " + ex.Message;
                AddLog("[오류] " + e.Error + (e.Loaded ? " — 이전 내용을 계속 사용합니다" : ""));
                Debug.LogWarning("[StockGame] " + e.Error);
                return false;
            }
        }

        /// <summary>Update 에서 호출. 주기적으로 파일 수정 시각을 확인한다.</summary>
        public void Poll(float dt, float interval)
        {
            pollTimer += dt;
            if (pollTimer < interval) return;
            pollTimer = 0;
            var changed = new List<string>();
            foreach (var e in Entries.Values)
            {
                if (e.RelPath.StartsWith("(")) continue;
                string full = ContentPaths.Full(e.RelPath);
                try
                {
                    if (!File.Exists(full)) continue;
                    DateTime t = File.GetLastWriteTimeUtc(full);
                    if (t != e.LastWrite)
                    {
                        e.LastWrite = t;
                        if (Load(e)) { changed.Add(e.Name); AddLog("다시 불러옴: " + e.RelPath); }
                    }
                }
                catch (Exception) { }
            }
            if (changed.Count > 0)
            {
                Version++;
                if (Reloaded != null) Reloaded(changed);
            }
        }

        public void AddLog(string line)
        {
            Log.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + line);
            if (Log.Count > 40) Log.RemoveAt(0);
        }

        public List<string> Errors()
        {
            var list = new List<string>();
            foreach (var e in Entries.Values) if (e.Error != null) list.Add(e.Error);
            return list;
        }
    }
}
