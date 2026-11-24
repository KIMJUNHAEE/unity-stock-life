using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 타이틀. 키 — 글자: title, subtitle, new_label, root / 참거짓: has_save, gallery_open
    /// 행동: new, continue, gallery, gallery_close, quit
    /// </summary>
    public class TitleScreen : UISource
    {
        bool galleryOpen;

        void OnEnable() { galleryOpen = false; }

        public override string Text(string key, Ctx c)
        {
            var cfg = GameConfig.Instance;
            switch (key)
            {
                case "title": return cfg != null ? cfg.theme.gameTitle : "";
                case "subtitle": return cfg != null ? cfg.theme.gameSubtitle : "";
                case "new_label": return Game.I.HasSave() ? "새 게임 (기존 진행 덮어쓰기)" : "새 게임";
                case "root": return "MVP 시제품 · data 폴더: " + ContentPaths.Root + "   ·   F1 상태  F5 다시 읽기  F12 디버그";
            }
            return null;
        }

        public override bool Flag(string key, Ctx c)
        {
            switch (key)
            {
                case "has_save": return Game.I.HasSave();
                case "gallery_open": return galleryOpen;
            }
            return false;
        }

        public override void Act(string action, Ctx c)
        {
            var root = GameRoot.I;
            switch (action)
            {
                case "new":
                    if (Game.I.HasSave()) root.Confirm("새 게임", "기존 회차 진행은 사라집니다. 컷신 수집 기록은 유지됩니다.", "새로 시작", StartNew);
                    else StartNew();
                    break;
                case "continue":
                    if (Game.I.Load()) root.Go(ScreenId.Home);
                    break;
                case "gallery": galleryOpen = true; break;
                case "gallery_close": galleryOpen = false; break;
                case "quit": Application.Quit(); break;
            }
        }

        void StartNew()
        {
            var g = Game.I;
            g.NewGame();
            var root = GameRoot.I;
            root.CloseAllModals();
            var cfg = GameConfig.Instance;
            if (cfg != null && cfg.dialogue.intro != null && cfg.dialogue.intro.lines.Count > 0)
            {
                root.Go(ScreenId.Cutscene);
                root.cutscene.Play(cfg.dialogue.intro, false, () => { g.S.Flags.Remove("intro_pending"); root.Go(ScreenId.Home); });
            }
            else root.Go(ScreenId.Home);
        }
    }
}
