using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// config/assets.json 의 이미지 키 → PNG/JPG 파일을 실행 중에 읽는다.
    /// Unity 임포트 없이 파일만 바꾸면 되고, 파일을 덮어쓰면 자동으로 다시 읽는다.
    /// 파일이 없으면 null 을 돌려주며 UI 가 자리표시 상자를 그린다.
    /// </summary>
    public static class TextureCache
    {
        class Item
        {
            public string Path;
            public Texture2D Tex;
            public DateTime LastWrite;
            public bool Missing;
        }

        static readonly Dictionary<string, Item> items = new Dictionary<string, Item>();
        static float pollTimer;

        public static string PathOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return Cfg.S("assets", "images." + key + ".path", null);
        }

        public static Texture2D Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            string rel = PathOf(key);
            if (string.IsNullOrEmpty(rel)) return null;
            Item it;
            if (items.TryGetValue(key, out it) && it.Path == rel)
                return it.Tex;
            if (it != null && it.Tex != null) UnityEngine.Object.Destroy(it.Tex);
            it = new Item { Path = rel };
            items[key] = it;
            LoadInto(it);
            return it.Tex;
        }

        static void LoadInto(Item it)
        {
            string full = ContentPaths.Full(it.Path);
            try
            {
                if (!File.Exists(full)) { it.Missing = true; it.Tex = null; return; }
                it.LastWrite = File.GetLastWriteTimeUtc(full);
                byte[] bytes = File.ReadAllBytes(full);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes))
                {
                    UnityEngine.Object.Destroy(tex);
                    it.Tex = null;
                    it.Missing = true;
                    return;
                }
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                it.Tex = tex;
                it.Missing = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[StockGame] 이미지 로드 실패 " + it.Path + ": " + ex.Message);
                it.Tex = null;
                it.Missing = true;
            }
        }

        /// <summary>설정 재로드 시 호출 — 경로가 바뀐 항목은 다음 Get 에서 새로 읽는다.</summary>
        public static void ClearAll()
        {
            foreach (var it in items.Values) if (it.Tex != null) UnityEngine.Object.Destroy(it.Tex);
            items.Clear();
        }

        /// <summary>파일이 새로 생기거나 바뀌었는지 주기적으로 확인.</summary>
        public static void Poll(float dt, float interval)
        {
            pollTimer += dt;
            if (pollTimer < interval) return;
            pollTimer = 0;
            foreach (var it in items.Values)
            {
                string full = ContentPaths.Full(it.Path);
                try
                {
                    bool exists = File.Exists(full);
                    if (!exists) continue;
                    if (it.Missing || File.GetLastWriteTimeUtc(full) != it.LastWrite)
                    {
                        if (it.Tex != null) UnityEngine.Object.Destroy(it.Tex);
                        LoadInto(it);
                    }
                }
                catch (Exception) { }
            }
        }

        public static int LoadedCount()
        {
            int n = 0;
            foreach (var it in items.Values) if (it.Tex != null) n++;
            return n;
        }
    }
}
