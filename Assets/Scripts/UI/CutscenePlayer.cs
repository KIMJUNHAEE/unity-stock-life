using System;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 컷신·인트로 재생. 일러스트·대사는 GameConfig ▸ 밤 컷신. 화면 배치는 씬에서.
    /// 참거짓: has_image, no_image, replay, has_portrait, has_speaker / 스프라이트: image, portrait
    /// 글자: title, image_hint, speaker, text, progress / 행동: skip, advance
    /// </summary>
    public class CutscenePlayer : UISource
    {
        [Tooltip("천천히 확대할 일러스트 오브젝트")] public RectTransform zoomTarget;
        SceneCfg scene;
        int index;
        float chars, camT;
        bool replay;
        Action onDone;

        public void Play(SceneCfg s, bool isReplay, Action done)
        {
            scene = s;
            index = 0;
            chars = 0;
            camT = 0;
            replay = isReplay;
            onDone = done;
            if (scene == null || scene.lines.Count == 0) { }
        }

        LineCfg Line { get { return scene != null && index < scene.lines.Count ? scene.lines[index] : null; } }

        void Update()
        {
            if (scene == null) return;
            var cfg = GameConfig.Instance;
            float cps = (cfg != null ? cfg.cutscenes.settings.charsPerSecond : 28) * Game.I.Settings.TextSpeed;
            float dt = Time.unscaledDeltaTime;
            chars += dt * cps;
            camT += dt;
            if (zoomTarget != null)
            {
                float zoom = 1 + Mathf.Min(0.06f, camT * (cfg != null ? cfg.cutscenes.settings.cameraZoomPerSecond : 0.004f));
                zoomTarget.localScale = new Vector3(zoom, zoom, 1);
            }
        }

        public void Advance()
        {
            if (scene == null) return;
            var l = Line;
            if (l != null && chars < (l.text ?? "").Length) { chars = l.text.Length; return; }
            index++;
            chars = 0;
            if (index >= scene.lines.Count) Finish();
        }

        void Finish()
        {
            var g = Game.I;
            if (scene != null)
            {
                GalleryEntry e;
                if (!string.IsNullOrEmpty(scene.id) && g.Gallery.Entries.TryGetValue(scene.id, out e)) { e.Seen = true; e.TimesPlayed++; g.SaveMeta(); }
            }
            scene = null;
            var cb = onDone;
            onDone = null;
            if (cb != null) cb();
            else GameRoot.I.Go(g.S != null ? ScreenId.Home : ScreenId.Title);
        }

        Sprite CurrentImage() { var l = Line; return l != null && l.image != null ? l.image : (scene != null ? scene.image : null); }

        public override bool Flag(string key, Ctx c)
        {
            switch (key)
            {
                case "has_image": return CurrentImage() != null;
                case "no_image": return CurrentImage() == null;
                case "replay": return replay;
                case "has_portrait": { var l = Line; return l != null && !string.IsNullOrEmpty(l.expression); }
                case "has_speaker": { var l = Line; return l != null && !string.IsNullOrEmpty(l.speaker); }
                case "has_lines": return scene != null && scene.lines.Count > 0;
            }
            return false;
        }

        public override Sprite SpriteOf(string key, Ctx c)
        {
            if (key == "image") return CurrentImage();
            if (key == "portrait") { var l = Line; var cfg = GameConfig.Instance; return l != null && cfg != null ? cfg.PortraitOf(l.expression) : null; }
            return null;
        }

        public override string Text(string key, Ctx c)
        {
            var l = Line;
            switch (key)
            {
                case "title": return scene != null ? scene.title : "";
                case "image_hint": return "[일러스트 자리] GameConfig ▸ 밤 컷신 ▸ " + (scene != null ? scene.id : "") + " ▸ 일러스트에 스프라이트를 넣으세요";
                case "speaker": return l != null ? l.speaker : "";
                case "text":
                    {
                        string t = l != null ? (l.text ?? "") : "";
                        int n = Mathf.Clamp((int)chars, 0, t.Length);
                        return t.Substring(0, n);
                    }
                case "progress": return scene != null && scene.lines.Count > 0 ? (index + 1) + " / " + scene.lines.Count + "  ▶ 클릭" : "";
                case "replay_label": return replay ? "다시 보기" : "";
            }
            return null;
        }

        public override void Act(string action, Ctx c)
        {
            if (action == "skip") Finish();
            else if (action == "advance") { if (scene != null && scene.lines.Count == 0) Finish(); else Advance(); }
        }
    }
}
