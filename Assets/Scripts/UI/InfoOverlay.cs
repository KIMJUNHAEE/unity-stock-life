using System.Text;

namespace StockGame
{
    /// <summary>F1 정보 화면. 키 info</summary>
    public class InfoOverlay : UISource
    {
        public override string Text(string key, Ctx c)
        {
            if (key != "info") return null;
            var sb = new StringBuilder();
            sb.AppendLine("<b>편집 방법</b>");
            sb.AppendLine("· 화면 배치·색·이미지: 플레이를 멈추고 Hierarchy 의 UI 오브젝트를 골라 Scene 뷰에서 옮기고 Inspector 에서 수정");
            sb.AppendLine("· 수치·문구: Assets/Resources/GameConfig.asset 을 클릭해 Inspector 에서 수정 (플레이 중에도 바로 반영)");
            sb.AppendLine("· 뉴스·게시글 같은 콘텐츠: data 폴더 JSON (StockGame ▸ 콘텐츠 편집기)");
            sb.AppendLine("· F5 다시 읽기 · F12 디버그 · Space 시장 일시정지 · Esc 창 닫기");
            sb.AppendLine();
            sb.AppendLine("data 폴더: " + ContentPaths.Root);
            sb.AppendLine("저장 위치: " + Game.SaveDir);
            sb.AppendLine();
            var store = GameRoot.I.Store;
            foreach (var e in store.Entries.Values)
                sb.AppendLine((e.Error == null ? "● " : "▲ ") + e.RelPath + (e.Error != null ? "  —  " + e.Error : ""));
            sb.AppendLine();
            for (int i = System.Math.Max(0, store.Log.Count - 8); i < store.Log.Count; i++) sb.AppendLine(store.Log[i]);
            return sb.ToString();
        }

        public override void Act(string action, Ctx c) { if (action == "close") gameObject.SetActive(false); }
    }
}
