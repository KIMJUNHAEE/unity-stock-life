using System.Collections.Generic;

namespace StockGame
{
    public class GuideStep
    {
        public string Id;
        public string Label;
        public bool Done;
    }

    /// <summary>
    /// 첫날 단계 안내 (첫 15분 진행안). 정답 종목을 알려 주지 않고, 지금 해 볼 수 있는 일만 순서대로 보여 준다.
    /// 거래하지 않고 지켜보기만 해도 단계는 완료된다.
    /// </summary>
    public partial class Game
    {
        const int GuideWatchTicks = 15;

        public bool GuideVisible { get { return S != null && S.Day == 1 && !S.HasFlag("guide_hidden"); } }

        public List<GuideStep> GuideSteps()
        {
            var list = new List<GuideStep>();
            if (S == null) return list;
            var t = S.Today;
            bool day1 = S.Day == 1;
            list.Add(new GuideStep { Id = "job", Label = "편의점에 출근하거나, 오늘은 쉬기", Done = !day1 || t.JobDone || t.JobSkipped });
            list.Add(new GuideStep { Id = "computer", Label = "컴퓨터를 켜서 바탕화면 보기", Done = !day1 || S.HasFlag("guide_computer") });
            list.Add(new GuideStep { Id = "read", Label = "인터넷에서 기사 하나, 게시글 하나 읽기", Done = !day1 || (t.ArticlesRead >= 1 && t.PostsRead >= 1) });
            list.Add(new GuideStep { Id = "stock", Label = "증권 앱에서 직접 사 보거나, 지켜보기만 하기", Done = !day1 || t.Trades > 0 || t.TicksWatched >= GuideWatchTicks });
            list.Add(new GuideStep { Id = "settle", Label = "결산하고 오늘의 결과 확인하기", Done = !day1 || t.Settled });
            list.Add(new GuideStep { Id = "sleep", Label = "가구를 살지 아껴 둘지 정하고 잠자기", Done = !day1 });
            return list;
        }

        public GuideStep CurrentGuideStep()
        {
            foreach (var s in GuideSteps()) if (!s.Done) return s;
            return null;
        }

        /// <summary>새로 끝난 단계를 한 번만 알려 준다. UI 가 매 프레임 호출해도 된다.</summary>
        public void CheckGuideProgress()
        {
            if (!GuideVisible || HiddenSim) return;
            foreach (var st in GuideSteps())
            {
                if (!st.Done || S.HasFlag("guide_done_" + st.Id)) continue;
                S.SetFlag("guide_done_" + st.Id);
                var next = CurrentGuideStep();
                Toast("할 일 완료: " + st.Label + (next != null ? "\n다음: " + next.Label : ""));
            }
        }

        public void HideGuide() { if (S != null) { S.SetFlag("guide_hidden"); Save(); } }

        public int GuideDoneCount()
        {
            int n = 0;
            foreach (var s in GuideSteps()) if (s.Done) n++;
            return n;
        }
    }
}
