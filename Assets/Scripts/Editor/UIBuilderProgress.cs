using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.EditorTools
{
    /// <summary>
    /// 진행 기능 UI (첫날 할 일, 아침 만남, 결산 복기, 편의점 카운터 정리).
    /// 새로 UI 를 만들 때와, 이미 꾸민 씬에 빠진 부분만 더할 때 같은 코드를 쓴다. 이미 있으면 건드리지 않는다.
    /// </summary>
    public static partial class UIBuilder
    {
        /// <summary>이미 있는 씬에 더할 때: 폰트·둥근 스프라이트·색 설정을 불러 둔다.</summary>
        public static void PrepareForUpdate() { LoadResources(); }

        static Transform FindDeep(Transform root, string path) { return root != null ? root.Find(path) : null; }

        /// <summary>집: 아침 만남 버튼, 휴식 문구, 오늘 할 일 카드.</summary>
        public static bool AddHomeProgress(Transform home)
        {
            bool changed = false;
            var panel = FindDeep(home, "InfoPanel");
            if (panel == null) return false;
            var morning = FindDeep(panel, "Buttons_Morning");
            if (morning != null)
            {
                var skipLabel = FindDeep(morning, "Btn_Skip/Label");
                var st = skipLabel != null ? skipLabel.GetComponent<Text>() : null;
                if (st != null && st.text != "오늘은 쉬기 (휴식)") { Undo.RecordObject(st, "Rest label"); st.text = "오늘은 쉬기 (휴식)"; changed = true; }
                if (FindDeep(morning, "Btn_Meet") == null)
                {
                    var mrt = (RectTransform)morning;
                    Undo.RecordObject(mrt, "Morning buttons");
                    mrt.sizeDelta = new Vector2(mrt.sizeDelta.x, Mathf.Max(mrt.sizeDelta.y, 270));
                    var meet = Btn(morning, "Btn_Meet", 0, 210, 424, 58, "점장님 만나러 가기", "meet", Kind.Normal, 22, null, "meet_label");
                    Show(meet.gameObject, "has_meeting", false, false);
                    Undo.RegisterCreatedObjectUndo(meet.gameObject, "Meeting button");
                    var shop = FindDeep(panel, "Btn_Shop") as RectTransform;
                    if (shop != null && Mathf.Approximately(shop.anchoredPosition.y, -640))
                    {
                        Undo.RecordObject(shop, "Move shop button");
                        shop.anchoredPosition = new Vector2(shop.anchoredPosition.x, -710);
                    }
                    changed = true;
                }
            }
            if (FindDeep(panel, "Guide") == null)
            {
                var guide = Box(panel, "Guide", 28, 782, 424, 130, P("panel_alt", new Color(0.95f, 0.96f, 0.99f)));
                var g = guide.transform;
                Txt(g, "Title", 16, 10, 300, 26, "오늘 할 일  0/6", 18, Dim, TextAnchor.MiddleLeft, true, "guide_title");
                Btn(g, "Btn_Hide", 330, 8, 80, 28, "숨기기", "guide_hide", Kind.Ghost, 15);
                Txt(g, "Now", 16, 42, 392, 34, "▶ 편의점에 출근하거나, 오늘은 쉬기", 19, TextC, TextAnchor.MiddleLeft, true, "guide_now");
                Txt(g, "Next", 16, 80, 392, 40, "다음: 컴퓨터를 켜서 바탕화면 보기", 16, Dim, TextAnchor.UpperLeft, false, "guide_next");
                Show(guide.gameObject, "guide_visible");
                Undo.RegisterCreatedObjectUndo(guide.gameObject, "Guide card");
                changed = true;
            }
            return changed;
        }

        /// <summary>바탕화면: 오른쪽 위 오늘 할 일 (앱 창보다 뒤).</summary>
        public static bool AddDesktopGuide(Transform desktop)
        {
            if (desktop == null || FindDeep(desktop, "Guide") != null) return false;
            var window = FindDeep(desktop, "Window");
            var panel = Panel(desktop, "Guide", 1480, 74, 420, 262);
            var shadow = FindDeep(desktop, "Guide_Shadow");
            var p = panel.transform;
            Txt(p, "Title", 24, 16, 280, 30, "오늘 할 일  0/6", 21, TextC, TextAnchor.MiddleLeft, true, "guide_title");
            Btn(p, "Btn_Hide", 316, 14, 84, 30, "숨기기", "guide_hide", Kind.Ghost, 15);
            var list = Txt(p, "List", 24, 58, 380, 190, "▶  편의점에 출근하거나, 오늘은 쉬기\n○  컴퓨터를 켜서 바탕화면 보기", 17, TextC, TextAnchor.UpperLeft, false, "guide_list");
            list.lineSpacing = 1.6f;
            Show(panel.gameObject, "guide_visible");
            if (window != null)
            {
                int idx = window.GetSiblingIndex();
                if (shadow != null) shadow.SetSiblingIndex(idx);
                panel.transform.SetSiblingIndex(idx + (shadow != null ? 1 : 0));
            }
            if (shadow != null) Undo.RegisterCreatedObjectUndo(shadow.gameObject, "Guide shadow");
            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Desktop guide");
            return true;
        }

        /// <summary>결산: 복기 버튼과 복기 화면.</summary>
        public static bool AddSettlementReview(Transform settlement)
        {
            if (settlement == null) return false;
            bool changed = false;
            var panel = FindDeep(settlement, "Panel");
            if (panel != null && FindDeep(panel, "Btn_Review") == null)
            {
                var b = Btn(panel, "Btn_Review", 720, 790, 640, 56, "오늘 매매 복기 보기", "review_open", Kind.Normal, 20);
                Undo.RegisterCreatedObjectUndo(b.gameObject, "Review button");
                changed = true;
            }
            if (FindDeep(settlement, "Review") == null)
            {
                var ov = Full(settlement, "Review");
                var dim = ov.AddComponent<Image>();
                dim.color = new Color(0.12f, 0.14f, 0.22f, 0.35f);
                dim.raycastTarget = true;
                var rp = Panel(ov.transform, "ReviewPanel", 120, 60, 1680, 960);
                var rpc = rp.color; rpc.a = 1f; rp.color = rpc;
                var t = rp.transform;
                Txt(t, "Legend", 1100, 96, 540, 30, "N 뉴스 · C 커뮤니티 · 네모 = 내 매수(빨강)·매도(파랑)", 15, Dim, TextAnchor.MiddleRight);
                Txt(t, "Title", 40, 24, 1200, 46, "1일차 복기", 32, TextC, TextAnchor.MiddleLeft, true, "rv_title");
                Btn(t, "Btn_Close", 1480, 24, 160, 48, "닫기", "review_close", Kind.Primary, 20);
                var tab = RowList(t, "Tabs", 40, 86, 1400, 46, "rv_tabs", 230, 10);
                MakeTab(tab.gameObject, "종목", "rv_tab", "rv_tab_selected", 19, "rv_tab_label");

                var chartBg = Box(t, "ChartBg", 40, 150, 1030, 620, P("panel_alt", new Color(0.97f, 0.98f, 1f)), true, 1.6f);
                chartBg.raycastTarget = false;
                var chartGo = Node(t, "Chart", 50, 160, 1010, 600);
                var chart = chartGo.AddComponent<CandleChart>();
                chart.reviewMode = true;
                chart.raycastTarget = true;
                var lt = Txt(chartGo.transform, "LabelTemplate", 0, 0, 90, 22, "1,000", 15, Dim);
                lt.gameObject.SetActive(false);
                chart.labelTemplate = lt;
                chart.hoverText = Txt(chartGo.transform, "HoverInfo", 6, 2, 900, 22, "", 15, TextC);
                chart.tooltipText = Txt(chartGo.transform, "Tooltip", 6, 30, 990, 26, "", 17, P("accent_dark", TextC), TextAnchor.MiddleLeft, true);

                Txt(t, "EventsTitle", 1100, 150, 540, 30, "시간순 기록 (매매 · 공개된 글)", 19, Dim, TextAnchor.MiddleLeft, true);
                var row = ScrollList(t, "Events", 1100, 188, 540, 582, "rv_events", 54, 4);
                Txt(row, "Time", 0, 0, 64, 54, "21:14", 16, Dim, TextAnchor.MiddleLeft, false, "rv_time");
                Txt(row, "Text", 70, 0, 456, 54, "매수 10주 · 2,340원", 16, TextC, TextAnchor.MiddleLeft, false, "rv_text", "rv_color");
                Txt(t, "Empty", 1100, 200, 540, 60, "", 17, Dim, TextAnchor.UpperLeft, false, "rv_empty");

                Txt(t, "Summary", 40, 790, 1600, 70, "체결 0건", 21, TextC, TextAnchor.UpperLeft, false, "rv_summary");
                Txt(t, "Note", 40, 880, 1600, 50, "", 17, Dim, TextAnchor.UpperLeft, false, "rv_note");
                Show(ov, "review_open", false, false);
                Undo.RegisterCreatedObjectUndo(ov, "Review overlay");
                changed = true;
            }
            return changed;
        }

        /// <summary>편의점 카운터: 단색 상자 대신 배경이 비치는 둥근 카운터 판.</summary>
        public static bool StyleJobCounter(Transform job)
        {
            var counter = FindDeep(job, "Counter");
            var img = counter != null ? counter.GetComponent<Image>() : null;
            if (img == null || img.sprite != null) return false;
            Undo.RecordObject(img, "Counter style");
            img.sprite = rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.6f;
            img.color = new Color(1f, 0.98f, 0.95f, 0.72f);
            var tc = counter.GetComponent<ThemeColor>();
            if (tc != null) Undo.DestroyObjectImmediate(tc);
            return true;
        }
    }
}
