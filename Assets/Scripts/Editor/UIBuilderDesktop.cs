using UnityEngine;
using UnityEngine.UI;

namespace StockGame.EditorTools
{
    /// <summary>컴퓨터 바탕화면과 앱들.</summary>
    public static partial class UIBuilder
    {
        static DesktopScreen BuildDesktop(Transform canvas)
        {
            var root = Full(canvas, "Screen_Desktop");
            var view = root.AddComponent<DesktopScreen>();
            Grad(root.transform, "Wallpaper_Gradient", P("desktop_top"), P("desktop_bottom"), "desktop_top", "desktop_bottom");
            var wp = Full(root.transform, "Wallpaper_Image");
            var wpi = wp.AddComponent<Image>(); wpi.raycastTarget = false;
            var wpb = wp.AddComponent<BindImage>(); wpb.spriteKey = "wallpaper"; wpb.hideWhenEmpty = true;

            string[] apps = { "stock", "browser", "finance", "album", "settings" };
            string[] names = { "증권", "인터넷", "금융", "밤의 기록", "설정" };
            var icons = Node(root.transform, "Icons", 14, 76, 104, 600);
            for (int i = 0; i < apps.Length; i++)
            {
                var ic = Node(icons.transform, "Icon_" + apps[i], 0, i * 118, 104, 108);
                var img = ic.AddComponent<Image>();
                img.color = new Color(1, 1, 1, 0.45f);
                img.sprite = rounded; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 2f;
                var b = ic.AddComponent<Button>();
                b.targetGraphic = img;
                var cb = ColorBlock.defaultColorBlock; cb.normalColor = new Color(1, 1, 1, 0); cb.highlightedColor = Color.white; cb.selectedColor = new Color(1, 1, 1, 0); b.colors = cb;
                ic.AddComponent<BindButton>().action = "open:" + apps[i];
                Pic(ic.transform, "Image", 22, 8, 60, 60, "icon:" + apps[i], names[i].Substring(0, 1), null, Color.white);
                Txt(ic.transform, "Name", 0, 70, 104, 30, names[i], 18, P("desktop_icon_text", P("text")), TextAnchor.MiddleCenter, true, "appname:" + apps[i]);
            }

            // 창
            var win = Node(root.transform, "Window", 136, 64, 1764, 952);
            Show(win, "window_open");
            Box(win.transform, "Shadow", 0, 6, 1764, 952, P("shadow", new Color(0, 0, 0, 0.1f)), true, 1.6f);
            var wbg = Box(win.transform, "Background", 0, 0, 1764, 952, P("window_bg", Color.white), true, 1.6f);
            wbg.raycastTarget = true;
            var tb = Box(win.transform, "TitleBar", 0, 0, 1764, 52, P("window_title"), true, 1.6f);
            tb.gameObject.AddComponent<ThemeColor>().colorKey = "window_title";
            var wi = Node(tb.transform, "Icon", 16, 10, 32, 32);
            var wii = wi.AddComponent<Image>(); wii.raycastTarget = false; wii.preserveAspect = true;
            var wib = wi.AddComponent<BindImage>(); wib.spriteKey = "window_icon"; wib.hideWhenEmpty = true;
            Txt(tb.transform, "Title", 58, 0, 600, 52, "증권", 24, TextC, TextAnchor.MiddleLeft, true, "window_title");
            Btn(tb.transform, "Btn_Minimize", 1644, 8, 50, 36, "—", "minimize", Kind.Ghost);
            Btn(tb.transform, "Btn_Close", 1702, 8, 50, 36, "✕", "close", Kind.Ghost);
            var content = Node(win.transform, "Content", 16, 64, 1732, 872);

            view.stock = BuildStock(content.transform);
            view.appStock = view.stock.gameObject;
            view.appBrowser = BuildBrowser(content.transform).gameObject;
            view.appFinance = BuildFinance(content.transform).gameObject;
            var album = Node(content.transform, "App_album", 0, 0, 1732, 872);
            BuildAlbum(album.transform, 0, 0, 1732, 872);
            view.appAlbum = album;
            view.appSettings = BuildSettings(content.transform).gameObject;
            view.appBrowser.SetActive(false);
            view.appFinance.SetActive(false);
            view.appAlbum.SetActive(false);
            view.appSettings.SetActive(false);

            // 위 막대
            var top = Box(root.transform, "TopBar", 0, 0, 1920, 50, P("topbar", new Color(1, 1, 1, 0.72f)), false);
            Txt(top.transform, "Left", 24, 0, 700, 50, "1일차 · 밤   |   21:00", 22, TextC, TextAnchor.MiddleLeft, true, "topleft");
            var chip = Btn(top.transform, "Btn_Condition", 1080, 8, 320, 34, "컨디션 100 · 양호", "condition", Kind.Normal, 19, null, "condition_chip");
            var dot = Box(top.transform, "StatusDot", 1440, 19, 12, 12, P("mint_text", Color.green));
            dot.sprite = circle;
            dot.gameObject.AddComponent<BindImage>().colorKey = "status_dot";
            Txt(top.transform, "Status", 1460, 0, 300, 50, "거래 진행 중", 19, TextC, TextAnchor.MiddleLeft, false, "market_status");
            Btn(top.transform, "Btn_Pause", 1770, 7, 130, 36, "❚❚ 정지", "pause", Kind.Normal, 18, "can_pause", "pause_label");

            // 작업 표시줄
            var task = Box(root.transform, "Taskbar", 0, 1024, 1920, 56, P("taskbar", new Color(1, 1, 1, 0.78f)), false);
            Btn(task.transform, "Btn_Menu", 12, 8, 120, 40, "메뉴", "menu", Kind.Primary, 19);
            var run = RowList(task.transform, "Running", 150, 8, 1300, 40, "running", 170, 10);
            MakeTab(run.gameObject, "증권", "run", "run_active", 19, "run_name");
            Btn(task.transform, "Btn_Settings", 1620, 8, 130, 40, "설정", "settings", Kind.Ghost, 19);
            Btn(task.transform, "Btn_Home", 1760, 8, 148, 40, "집으로", "home", Kind.Ghost, 19);

            // 초상화와 말풍선
            var por = Node(root.transform, "Portrait", 22, 880, 96, 96);
            var pbg = por.AddComponent<Image>(); pbg.sprite = circle; pbg.color = new Color(1, 1, 1, 0.9f); pbg.raycastTarget = false;
            var pimg = Pic(por.transform, "Face", 4, 4, 88, 88, "portrait", "서하", null, new Color(0.92f, 0.95f, 0.98f));
            pimg.sprite = circle; pimg.type = Image.Type.Simple;
            var bub = Box(root.transform, "Bubble", 130, 890, 460, 80, P("bubble", Color.white));
            Txt(bub.transform, "Text", 16, 8, 428, 64, "체결 확인.", 19, TextC, TextAnchor.MiddleLeft, false, "bubble_text");
            Show(bub.gameObject, "bubble", false, false);

            view.vignette = Vignette(root.transform);

            // 컨디션 패널
            var cond = Full(root.transform, "Overlay_Condition");
            Show(cond, "ov_condition", false, false);
            var cdim = AddImage(cond, P("dim", new Color(0.17f, 0.23f, 0.32f, 0.4f)), false); cdim.raycastTarget = true;
            var cp = Panel(cond.transform, "Panel", 1200, 70, 680, 760);
            var c = cp.transform;
            Txt(c, "Title", 32, 24, 400, 40, "컨디션", 28, TextC, TextAnchor.MiddleLeft, true);
            Txt(c, "Paused", 32, 24, 616, 40, "시장 정지 중", 18, Dim, TextAnchor.MiddleRight);
            Txt(c, "Value", 32, 80, 616, 30, "100 / 100 · 양호", 22, TextC, TextAnchor.MiddleLeft, true, "cp_value");
            Bar(c, "Bar", 32, 116, 616, 16, "cp_frac", P("mint"));
            Txt(c, "Now", 32, 146, 616, 56, "지금", 19, TextC, TextAnchor.UpperLeft, false, "cp_now");
            Txt(c, "Next", 32, 202, 616, 64, "다음 단계", 19, Dim, TextAnchor.UpperLeft, false, "cp_next");
            Txt(c, "Cash", 32, 272, 616, 30, "회복 (현금)", 22, TextC, TextAnchor.MiddleLeft, true, "cp_cash");
            var irow = RowList(c, "Items", 32, 312, 616, 320, "items", 92, 12, false);
            AddImage(irow.gameObject, P("panel_alt"));
            Txt(irow, "Name", 16, 8, 420, 32, "간식 · 2,000원", 22, TextC, TextAnchor.MiddleLeft, true, "it_name");
            Txt(irow, "Desc", 16, 42, 440, 44, "설명", 17, Dim, TextAnchor.UpperLeft, false, "it_desc");
            Btn(irow, "Btn_Use", 466, 20, 134, 52, "사용", "it_use", Kind.Primary, 22, "it_can");
            Txt(c, "Note", 32, 640, 616, 50, "수면은 오늘 거래를 마치고 잠자리에 들면 회복됩니다. 컨디션은 가격·체결에 영향을 주지 않습니다.", 17, Dim, TextAnchor.UpperLeft);
            Btn(c, "Btn_Close", 32, 692, 616, 52, "닫기", "cp_close");

            // OS 메뉴
            var menu = Full(root.transform, "Overlay_Menu");
            Show(menu, "ov_menu", false, false);
            var outside = menu.AddComponent<Image>(); outside.color = new Color(0, 0, 0, 0); outside.raycastTarget = true;
            menu.AddComponent<Button>().targetGraphic = outside;
            menu.AddComponent<BindButton>().action = "menu_close";
            var mp = Panel(menu.transform, "Panel", 12, 764, 380, 250);
            Btn(mp.transform, "Btn_EndDay", 20, 20, 340, 56, "오늘 거래 종료 (결산)", "end_day", Kind.Primary, 20, "can_end_day");
            Btn(mp.transform, "Btn_Home", 20, 88, 340, 56, "집으로 돌아가기", "go_home");
            Btn(mp.transform, "Btn_Close", 20, 156, 340, 56, "닫기", "menu_close", Kind.Ghost);

            // 튜토리얼
            var tut = Full(root.transform, "Overlay_Tutorial");
            Show(tut, "ov_tutorial", false, false);
            var tdim = AddImage(tut, new Color(0.17f, 0.23f, 0.32f, 0.25f), false); tdim.raycastTarget = true;
            var tp = Panel(tut.transform, "Panel", 360, 720, 1200, 220);
            Pic(tp.transform, "Portrait", 24, 24, 172, 172, "tut_portrait", "서하");
            Txt(tp.transform, "Step", 220, 24, 900, 30, "안내 1 / 5", 18, Dim, TextAnchor.MiddleLeft, false, "tut_step");
            Txt(tp.transform, "Text", 220, 60, 950, 90, "안내 문장", 23, TextC, TextAnchor.UpperLeft, false, "tut_text");
            Btn(tp.transform, "Btn_Next", 1000, 150, 176, 50, "다음", "tut_next", Kind.Primary, 20, null, "tut_next_label");
            Btn(tp.transform, "Btn_Skip", 810, 150, 176, 50, "건너뛰기", "tut_skip", Kind.Ghost, 20);
            return view;
        }

        // ───────────────────────── 증권 ─────────────────────────

        static StockApp BuildStock(Transform content)
        {
            var root = Node(content, "App_stock", 0, 0, 1732, 872);
            var view = root.AddComponent<StockApp>();
            var t = root.transform;
            TabBtn(t, "Tab_Trade", 0, 0, 120, 44, "거래", "tab:trade", "tab_is:trade");
            TabBtn(t, "Tab_Holdings", 128, 0, 120, 44, "보유", "tab:holdings", "tab_is:holdings");
            TabBtn(t, "Tab_History", 256, 0, 120, 44, "내역", "tab:history", "tab_is:history");
            Txt(t, "Cash", 400, 0, 1332, 44, "거래 가능 현금  300,000원", 26, TextC, TextAnchor.MiddleRight, true, "cash");

            // 거래 탭
            var trade = Node(t, "Page_Trade", 0, 58, 1732, 814);
            Show(trade, "tab_is:trade");
            var tr = trade.transform;
            var wl = Box(tr, "Watchlist", 0, 0, 290, 780, P("panel_alt"));
            Txt(wl.transform, "Title", 16, 8, 258, 34, "관심 종목", 22, TextC, TextAnchor.MiddleLeft, true);
            var w = ScrollList(wl.transform, "List", 6, 48, 278, 726, "watch", 96, 6);
            RowButton(w, "w_select", new Color(1, 1, 1, 0));
            var wsel = New("Selected", w); Stretch(wsel); AddImage(wsel, P("selected")); Show(wsel, "w_selected");
            Txt(w, "Name", 12, 8, 244, 28, "모모바이오", 21, TextC, TextAnchor.MiddleLeft, true, "w_name");
            Txt(w, "Qty", 12, 8, 244, 28, "", 15, Dim, TextAnchor.MiddleRight, false, "w_qty");
            Txt(w, "Price", 12, 36, 244, 26, "1,000원", 19, TextC, TextAnchor.MiddleLeft, false, "w_price");
            Txt(w, "Change", 12, 62, 244, 24, "▲ +0.00%", 18, TextC, TextAnchor.MiddleLeft, false, "w_change", "w_color");

            var cp = Box(tr, "ChartPanel", 304, 0, 1044, 780, P("panel", Color.white));
            var c = cp.transform;
            Txt(c, "Name", 20, 10, 300, 44, "모모바이오", 28, TextC, TextAnchor.MiddleLeft, true, "name");
            Txt(c, "Price", 300, 10, 220, 44, "1,000원", 28, TextC, TextAnchor.MiddleLeft, true, "price");
            Txt(c, "Change", 520, 10, 300, 44, "▲ 0 (+0.00%)", 21, TextC, TextAnchor.MiddleLeft, false, "change", "change_color");
            var sp = RowList(c, "Speeds", 832, 14, 196, 36, "speeds", 60, 6);
            MakeTab(sp.gameObject, "×1", "sp", "sp_selected", 17, "sp_label");
            var tf = RowList(c, "Timeframes", 20, 58, 210, 34, "tfs", 64, 6);
            MakeTab(tf.gameObject, "1분", "tf", "tf_selected", 17, "tf_label");
            Btn(c, "Btn_ZoomOut", 240, 58, 36, 34, "−", "zoom_out", Kind.Tab, 20);
            Btn(c, "Btn_ZoomIn", 282, 58, 36, 34, "+", "zoom_in", Kind.Tab, 20);
            TabBtn(c, "Btn_Markers", 336, 58, 130, 34, "기사 표시", "markers", "markers_on", 16, "markers_label");
            Txt(c, "Hint", 480, 58, 544, 34, "휠: 확대/축소 · 드래그: 과거 보기", 15, Dim, TextAnchor.MiddleRight);

            var chartGo = Node(c, "Chart", 16, 104, 1012, 662);
            var chart = chartGo.AddComponent<CandleChart>();
            chart.raycastTarget = true;
            var lt = Txt(chartGo.transform, "LabelTemplate", 0, 0, 90, 22, "1,000", 15, Dim);
            lt.gameObject.SetActive(false);
            chart.labelTemplate = lt;
            chart.hoverText = Txt(chartGo.transform, "HoverInfo", 6, 2, 900, 22, "", 15, TextC);
            chart.tooltipText = Txt(chartGo.transform, "Tooltip", 6, 60, 900, 26, "", 17, P("accent_dark", TextC), TextAnchor.MiddleLeft, true);
            var lidTop = New("EyelidTop", chartGo.transform);
            var lrt = (RectTransform)lidTop.transform; lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1); lrt.pivot = new Vector2(0.5f, 1); lrt.sizeDelta = new Vector2(0, 0); lrt.anchoredPosition = Vector2.zero;
            var lti = lidTop.AddComponent<Image>(); lti.color = P("eyelid", new Color(0.18f, 0.2f, 0.27f, 0.95f)); lti.raycastTarget = false;
            var lidBot = New("EyelidBottom", chartGo.transform);
            var lbt = (RectTransform)lidBot.transform; lbt.anchorMin = new Vector2(0, 0); lbt.anchorMax = new Vector2(1, 0); lbt.pivot = new Vector2(0.5f, 0); lbt.sizeDelta = new Vector2(0, 0); lbt.anchoredPosition = Vector2.zero;
            var lbi = lidBot.AddComponent<Image>(); lbi.color = lti.color; lbi.raycastTarget = false;
            chart.eyelidTop = lrt;
            chart.eyelidBottom = lbt;
            chart.eyelidText = Txt(chartGo.transform, "EyelidText", 0, 311, 1012, 40, "…졸려.", 22, Color.white, TextAnchor.MiddleCenter);
            var latest = Btn(c, "Btn_Latest", 898, 722, 120, 36, "최신으로 ▶", "chart_latest", Kind.Normal, 16);
            Show(latest.gameObject, "chart_scrolled", false, false);

            // 주문
            var op = Box(tr, "OrderPanel", 1362, 0, 370, 780, P("panel", Color.white));
            var o = op.transform;
            Txt(o, "Title", 20, 14, 330, 36, "시장가 주문", 26, TextC, TextAnchor.MiddleLeft, true);
            var info = Box(o, "Holding", 20, 62, 330, 120, P("panel_alt"));
            Pair(info.transform, "Qty", 14, 8, 302, 30, "보유", "hold_qty", 19);
            Pair(info.transform, "Avg", 14, 44, 302, 30, "평균 매입가", "hold_avg", 19);
            Pair(info.transform, "Pnl", 14, 80, 302, 30, "평가손익", "hold_pnl", 19, "pnl_color");
            Txt(o, "QtyLabel", 20, 198, 330, 28, "수량", 19, Dim);
            Btn(o, "Btn_Minus", 20, 230, 56, 52, "−", "qty_minus");
            var qb = Box(o, "QtyBox", 84, 230, 202, 52, P("panel_alt"));
            Txt(qb.transform, "Text", 0, 0, 202, 52, "1주", 26, TextC, TextAnchor.MiddleCenter, true, "qty");
            Btn(o, "Btn_Plus", 294, 230, 56, 52, "+", "qty_plus");
            Btn(o, "Btn_10", 20, 292, 76, 42, "+10", "qty_10", Kind.Normal, 18);
            Btn(o, "Btn_100", 104, 292, 76, 42, "+100", "qty_100", Kind.Normal, 18);
            Btn(o, "Btn_Max", 188, 292, 76, 42, "최대", "qty_max", Kind.Normal, 18);
            Btn(o, "Btn_Hold", 272, 292, 78, 42, "보유", "qty_hold", Kind.Normal, 18, "has_holding");
            Pair(o, "CurPrice", 20, 346, 330, 30, "현재가", "cur_price", 19);
            Pair(o, "Amount", 20, 380, 330, 30, "주문 금액", "amount", 19);
            Pair(o, "Fees", 20, 414, 330, 30, "수수료 (매수/매도)", "fees", 19);
            Box(o, "Line", 20, 452, 330, 1, P("grid"), false);
            Pair(o, "Need", 20, 462, 330, 30, "매수 시 필요", "need", 19, null, true);
            Pair(o, "Receive", 20, 498, 330, 30, "매도 시 수령", "receive", 19, null, true);
            Txt(o, "MaxBuy", 20, 534, 330, 26, "최대 매수 가능 0주", 16, Dim, TextAnchor.MiddleLeft, false, "max_buy");
            var reason = Txt(o, "Reason", 20, 628, 330, 62, "사유", 17, P("warning", new Color(0.85f, 0.58f, 0.23f)), TextAnchor.LowerLeft, false, "reason");
            Show(reason.gameObject, "has_reason");
            Btn(o, "Btn_Buy", 20, 696, 159, 64, "▲ 매수", "buy", Kind.Up, 24, "can_buy");
            Btn(o, "Btn_Sell", 191, 696, 159, 64, "▼ 매도", "sell", Kind.Down, 24, "can_sell");
            Txt(tr, "Note", 0, 786, 1732, 26, "모든 종목의 시장 시간이 함께 흐릅니다.", 16, Dim, TextAnchor.MiddleLeft, false, "market_note");

            // 보유 탭
            var hold = Node(t, "Page_Holdings", 0, 58, 1732, 814);
            Show(hold, "tab_is:holdings", false, false);
            AddImage(hold, P("panel", Color.white));
            string[] hh = { "종목", "수량", "평균 매입가", "현재가", "평가금액", "평가손익", "수익률" };
            float[] hc = { 0.03f, 0.2f, 0.32f, 0.46f, 0.58f, 0.71f, 0.84f };
            for (int i = 0; i < hh.Length; i++) Txt(hold.transform, "Head_" + i, 1732 * hc[i], 16, 220, 30, hh[i], 19, Dim);
            var hr = ScrollList(hold.transform, "List", 0, 60, 1732, 600, "holdings", 48, 0);
            string[] hk = { "h_name", "h_qty", "h_avg", "h_price", "h_value", "h_pnl", "h_rate" };
            for (int i = 0; i < hk.Length; i++) Txt(hr, "Col_" + hk[i], 1732 * hc[i], 0, 230, 48, "-", 21, TextC, TextAnchor.MiddleLeft, i == 0, hk[i], i >= 5 ? "h_color" : null);
            Btn(hr, "Btn_Chart", 1732 * 0.92f, 6, 110, 36, "차트", "h_chart", Kind.Normal, 17);
            var none = Txt(hold.transform, "Empty", 0, 90, 1732, 40, "보유 종목이 없습니다.", 22, Dim, TextAnchor.MiddleCenter);
            Show(none.gameObject, "no_holdings");
            Txt(hold.transform, "Summary", 30, 690, 1600, 32, "보유 평가액", 22, TextC, TextAnchor.MiddleLeft, true, "hold_summary");
            Txt(hold.transform, "Today", 30, 730, 1600, 32, "오늘 실현 순손익", 22, TextC, TextAnchor.MiddleLeft, false, "today_summary");

            // 내역 탭
            var hist = Node(t, "Page_History", 0, 58, 1732, 814);
            Show(hist, "tab_is:history", false, false);
            AddImage(hist, P("panel", Color.white));
            string[] th = { "일차·시각", "종목", "구분", "수량", "체결가", "금액", "수수료", "실현 손익" };
            float[] tc = { 0.03f, 0.15f, 0.31f, 0.39f, 0.49f, 0.61f, 0.74f, 0.84f };
            string[] tk = { "t_time", "t_name", "t_side", "t_qty", "t_price", "t_amount", "t_fee", "t_realized" };
            for (int i = 0; i < th.Length; i++) Txt(hist.transform, "Head_" + i, 1732 * tc[i], 16, 220, 30, th[i], 19, Dim);
            var hl = ScrollList(hist.transform, "List", 0, 64, 1732, 740, "history", 42, 0);
            for (int i = 0; i < tk.Length; i++) Txt(hl, "Col_" + tk[i], 1732 * tc[i], 0, 230, 42, "-", 19, TextC, TextAnchor.MiddleLeft, false, tk[i], i == 2 ? "t_side_color" : (i == 7 ? "t_real_color" : null));
            var hnone = Txt(hist.transform, "Empty", 0, 90, 1732, 40, "아직 체결 내역이 없습니다.", 22, Dim, TextAnchor.MiddleCenter);
            Show(hnone.gameObject, "no_history");
            return view;
        }

        // ───────────────────────── 브라우저 ─────────────────────────

        static BrowserApp BuildBrowser(Transform content)
        {
            var root = Node(content, "App_browser", 0, 0, 1732, 872);
            var view = root.AddComponent<BrowserApp>();
            var t = root.transform;
            TabBtn(t, "Tab_Home", 0, 0, 140, 44, "홈", "site:home", "site_is:home");
            TabBtn(t, "Tab_News", 148, 0, 220, 44, "파스텔경제", "site:news", "site_is:news", 20, "tab_news");
            TabBtn(t, "Tab_Community", 376, 0, 220, 44, "개미광장", "site:community", "site_is:community", 20, "tab_community");
            var addr = Box(t, "Address", 0, 54, 1732, 40, P("panel_alt"));
            Txt(addr.transform, "Url", 20, 0, 900, 40, "about:home", 18, Dim, TextAnchor.MiddleLeft, false, "url");
            Txt(addr.transform, "Clock", 20, 0, 1692, 40, "게임 내 시각", 17, Dim, TextAnchor.MiddleRight, false, "clock");

            var home = Node(t, "Page_Home", 0, 106, 1732, 766);
            Show(home, "site_is:home");
            Txt(home.transform, "Title", 0, 40, 1732, 50, "즐겨찾기", 28, TextC, TextAnchor.MiddleCenter, true);
            Tile(home.transform, "Tile_News", 386, 130, "site:news", "news_name", "news_tagline");
            Tile(home.transform, "Tile_Community", 926, 130, "site:community", "comm_name", "comm_tagline");
            Txt(home.transform, "Hint", 80, 450, 1572, 80, "뉴스는 확인된 사실이지만 가격에 먼저 반영됐을 수 있어요.", 21, Dim, TextAnchor.UpperCenter, false, "hint");

            // 뉴스
            var news = Node(t, "Page_News", 0, 106, 1732, 766);
            Show(news, "site_is:news", false, false);
            var nl = Box(news.transform, "List", 0, 0, 660, 766, P("panel_alt"));
            TabBtn(nl.transform, "Toggle_Watch", 16, 8, 200, 36, "관심 종목만", "toggle_watch", "only_watch", 18);
            var nr = ScrollList(nl.transform, "Items", 6, 52, 648, 708, "news", 92, 6);
            RowButton(nr, "n_open", new Color(1, 1, 1, 0));
            var nsel = New("Selected", nr); Stretch(nsel); AddImage(nsel, P("selected")); Show(nsel, "n_selected");
            var ndot = Box(nr, "Unread", 8, 18, 8, 8, P("accent")); ndot.sprite = circle; Show(ndot.gameObject, "n_unread");
            Txt(nr, "Title", 24, 8, 600, 34, "기사 제목", 21, TextC, TextAnchor.MiddleLeft, true, "n_title", "n_title_color");
            Txt(nr, "Meta", 24, 46, 600, 30, "매체 · 1일차 21:30", 17, Dim, TextAnchor.MiddleLeft, false, "n_meta");
            var nempty = Txt(nl.transform, "Empty", 0, 90, 660, 40, "아직 공개된 기사가 없습니다.", 21, Dim, TextAnchor.MiddleCenter);
            Show(nempty.gameObject, "news_empty");
            var nd = Box(news.transform, "Detail", 676, 0, 1056, 766, P("panel", Color.white));
            var nph = Txt(nd.transform, "Placeholder", 0, 0, 1056, 766, "왼쪽 목록에서 기사를 선택하세요.", 22, Dim, TextAnchor.MiddleCenter);
            Show(nph.gameObject, "news_no_sel");
            var ndc = DetailScroll(nd.transform, "Article", 1056, 766, "news_has_sel");
            LayoutText(ndc, "Title", "기사 제목", 32, TextC, true, "nd_title");
            LayoutText(ndc, "Meta", "매체 · 시각", 18, Dim, false, "nd_meta");
            LayoutText(ndc, "Body", "본문", 23, TextC, false, "nd_body");
            LayoutText(ndc, "Company", "관련 기업", 19, Dim, false, "nd_company");
            var ndb = LayoutButton(ndc, "Btn_Chart", 300, 54, "관련 종목 차트 보기", "nd_chart", Kind.Primary);
            Show(ndb, "nd_has_stock");

            // 커뮤니티
            var com = Node(t, "Page_Community", 0, 106, 1732, 766);
            Show(com, "site_is:community", false, false);
            var cl = Box(com.transform, "List", 0, 0, 820, 766, P("panel_alt"));
            TabBtn(cl.transform, "Toggle_Watch", 16, 8, 260, 36, "종목 글만 (관심 종목)", "toggle_watch", "only_watch", 18);
            var cr = ScrollList(cl.transform, "Items", 6, 52, 808, 708, "posts", 80, 4);
            RowButton(cr, "p_open", new Color(1, 1, 1, 0));
            var csel = New("Selected", cr); Stretch(csel); AddImage(csel, P("selected")); Show(csel, "p_selected");
            var cat = Box(cr, "Category", 14, 12, 64, 24, P("mint"));
            cat.gameObject.AddComponent<BindImage>().colorKey = "p_cat_color";
            Txt(cat.transform, "Text", 0, 0, 64, 24, "일반", 15, TextC, TextAnchor.MiddleCenter, false, "p_cat");
            Txt(cr, "Title", 90, 6, 690, 36, "글 제목", 21, TextC, TextAnchor.MiddleLeft, true, "p_title", "p_title_color");
            Txt(cr, "Meta", 14, 44, 500, 28, "닉네임 · 시각", 16, Dim, TextAnchor.MiddleLeft, false, "p_meta");
            Txt(cr, "Stats", 14, 44, 766, 28, "조회 · 추천", 16, Dim, TextAnchor.MiddleRight, false, "p_stats");
            var cempty = Txt(cl.transform, "Empty", 0, 90, 820, 40, "아직 글이 없습니다.", 21, Dim, TextAnchor.MiddleCenter);
            Show(cempty.gameObject, "posts_empty");
            var cd = Box(com.transform, "Detail", 836, 0, 896, 766, P("panel", Color.white));
            var cph = Txt(cd.transform, "Placeholder", 0, 0, 896, 766, "왼쪽 목록에서 글을 선택하세요.", 22, Dim, TextAnchor.MiddleCenter);
            Show(cph.gameObject, "post_no_sel");
            var cdc = DetailScroll(cd.transform, "Post", 896, 766, "post_has_sel");
            LayoutText(cdc, "Title", "글 제목", 30, TextC, true, "pd_title");
            LayoutText(cdc, "Meta", "닉네임 · 시각", 18, Dim, false, "pd_meta");
            LayoutText(cdc, "Body", "본문", 23, TextC, false, "pd_body");
            var cdb = LayoutButton(cdc, "Btn_Chart", 260, 48, "차트", "pd_chart", Kind.Normal, "pd_chart_label");
            Show(cdb, "pd_has_stock");
            LayoutText(cdc, "CommentCount", "댓글 0", 21, TextC, true, "pd_comment_count");
            var comments = New("Comments", cdc);
            var cvl = comments.AddComponent<VerticalLayoutGroup>();
            cvl.spacing = 10; cvl.childControlWidth = true; cvl.childControlHeight = true; cvl.childForceExpandWidth = true; cvl.childForceExpandHeight = false;
            var ctm = New("Template", comments.transform);
            var cimg = ctm.AddComponent<Image>(); cimg.color = P("panel_alt"); cimg.sprite = rounded; cimg.type = Image.Type.Sliced; cimg.pixelsPerUnitMultiplier = 2f; cimg.raycastTarget = false;
            var ctl = ctm.AddComponent<VerticalLayoutGroup>();
            ctl.padding = new RectOffset(20, 20, 10, 12); ctl.spacing = 4; ctl.childControlWidth = true; ctl.childControlHeight = true; ctl.childForceExpandWidth = true; ctl.childForceExpandHeight = false;
            LayoutText((RectTransform)ctm.transform, "Meta", "닉네임 · 시각", 16, Dim, false, "cm_meta");
            LayoutText((RectTransform)ctm.transform, "Text", "댓글", 21, TextC, false, "cm_text");
            var clist = comments.AddComponent<UIList>();
            clist.listKey = "comments";
            clist.template = (RectTransform)ctm.transform;
            clist.content = (RectTransform)comments.transform;
            return view;
        }

        static void Tile(Transform parent, string name, float x, float y, string action, string nameBind, string tagBind)
        {
            var go = Node(parent, name, x, y, 420, 260);
            var b = MakeButton(go, "", action, Kind.Normal, 20, null, null);
            Object.DestroyImmediate(go.transform.Find("Label").gameObject);
            Pic(go.transform, "Logo", 150, 30, 120, 100, null, "로고");
            Txt(go.transform, "Name", 0, 145, 420, 40, "사이트", 28, TextC, TextAnchor.MiddleCenter, true, nameBind);
            Txt(go.transform, "Tagline", 20, 190, 380, 50, "설명", 18, Dim, TextAnchor.UpperCenter, false, tagBind);
        }

        /// <summary>내용 길이에 맞춰 늘어나는 세로 스크롤 영역. 반환값 = 내용 부모.</summary>
        static RectTransform DetailScroll(Transform parent, string name, float w, float h, string showKey)
        {
            var root = Node(parent, name, 0, 0, w, h);
            Show(root, showKey, false, true);
            var img = root.AddComponent<Image>(); img.color = new Color(1, 1, 1, 0); img.raycastTarget = true;
            var sr = root.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 40; sr.movementType = ScrollRect.MovementType.Clamped;
            var vp = Full(root.transform, "Viewport");
            vp.AddComponent<RectMask2D>();
            var c = New("Content", vp.transform);
            var crt = (RectTransform)c.transform;
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = new Vector2(0, -h); crt.offsetMax = Vector2.zero;
            var vl = c.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(40, 40, 32, 40); vl.spacing = 16;
            vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            c.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = (RectTransform)vp.transform;
            sr.content = crt;
            return crt;
        }

        static Text LayoutText(RectTransform parent, string name, string text, int size, Color c, bool bold, string bind)
        {
            var go = New(name, parent);
            var t = AddText(go, text, size, c, TextAnchor.UpperLeft, bold, bind, null);
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static GameObject LayoutButton(RectTransform parent, string name, float w, float h, string label, string action, Kind kind, string labelBind = null)
        {
            var holder = New(name + "_Row", parent);
            var le = holder.AddComponent<LayoutElement>();
            le.preferredHeight = h; le.minHeight = h;
            var go = Node(holder.transform, name, 0, 0, w, h);
            MakeButton(go, label, action, kind, 20, null, labelBind);
            return holder;
        }

        // ───────────────────────── 금융 ─────────────────────────

        static FinanceApp BuildFinance(Transform content)
        {
            var root = Node(content, "App_finance", 0, 0, 1732, 872);
            var view = root.AddComponent<FinanceApp>();
            var l = Box(root.transform, "Loan", 0, 0, 854, 872, P("panel", Color.white)).transform;
            Txt(l, "Title", 32, 26, 790, 40, "추가 투자 대출", 28, TextC, TextAnchor.MiddleLeft, true);
            Txt(l, "Rules", 32, 80, 790, 90, "규칙", 18, Dim, TextAnchor.UpperLeft, false, "loan_rules");
            var st = Box(l, "Status", 32, 180, 790, 170, P("panel_alt"));
            var none = Txt(st.transform, "None", 0, 0, 790, 170, "현재 추가 대출이 없습니다.", 22, Dim, TextAnchor.MiddleCenter);
            Show(none.gameObject, "no_loan");
            var rows = Node(st.transform, "Rows", 20, 14, 750, 150);
            Show(rows, "has_loan", false, false);
            Pair(rows.transform, "Principal", 0, 0, 750, 30, "원금", "loan_principal");
            Pair(rows.transform, "Interest", 0, 36, 750, 30, "발생 이자", "loan_interest");
            Pair(rows.transform, "Due", 0, 72, 750, 30, "상환일", "loan_due", 21, "loan_due_color");
            Pair(rows.transform, "Owed", 0, 108, 750, 30, "갚을 금액", "loan_owed", 21, null, true);
            Txt(l, "AmountLabel", 32, 370, 790, 30, "빌릴 금액", 22, TextC, TextAnchor.MiddleLeft, true);
            var am = RowList(l, "Amounts", 32, 408, 790, 48, "amounts", 255, 10);
            MakeTab(am.gameObject, "100,000원", "am", "am_selected", 18, "am_label");
            Txt(l, "Preview", 32, 470, 790, 60, "받을 금액", 18, TextC, TextAnchor.UpperLeft, false, "borrow_preview");
            var reason = Txt(l, "Reason", 32, 540, 790, 30, "사유", 18, P("warning", new Color(0.85f, 0.58f, 0.23f)), TextAnchor.MiddleLeft, false, "borrow_reason");
            Show(reason.gameObject, "has_reason");
            Btn(l, "Btn_Borrow", 32, 576, 790, 58, "대출 받기", "borrow", Kind.Primary, 22, "can_borrow");
            var rep = Node(l, "Repay", 32, 650, 790, 120);
            Show(rep, "has_loan", false, false);
            Txt(rep.transform, "Label", 0, 0, 790, 30, "상환 (현금)", 22, TextC, TextAnchor.MiddleLeft, true, "cash_label");
            var lr = RowList(rep.transform, "Buttons", 0, 40, 600, 52, "loan_repay", 140, 10);
            MakeButton(lr.gameObject, "10,000", "lr", Kind.Normal, 18, "can_repay_loan", "lr_label");
            Btn(rep.transform, "Btn_All", 610, 40, 180, 52, "전액", "repay_loan_all", Kind.Primary, 18, "can_repay_loan");

            var r = Box(root.transform, "StartingDebt", 878, 0, 854, 872, P("panel", Color.white)).transform;
            Txt(r, "Title", 32, 26, 790, 40, "부모님이 남긴 빚", 28, TextC, TextAnchor.MiddleLeft, true, "debt_title");
            Txt(r, "Amount", 32, 86, 790, 60, "1,000,000,000원", 48, TextC, TextAnchor.MiddleLeft, true, "debt_amount");
            Bar(r, "Progress", 32, 160, 790, 16, "debt_frac", P("mint"));
            Txt(r, "ProgressText", 32, 186, 790, 30, "상환 진척", 18, Dim, TextAnchor.MiddleLeft, false, "debt_progress");
            Txt(r, "Note", 32, 230, 790, 90, "시작 부채는 장기 목표입니다.", 18, Dim, TextAnchor.UpperLeft, false, "debt_note");
            Txt(r, "CashLabel", 32, 330, 790, 30, "상환 (현금)", 22, TextC, TextAnchor.MiddleLeft, true, "cash_label");
            var dr = RowList(r, "Buttons", 32, 370, 600, 52, "debt_repay", 140, 10);
            MakeButton(dr.gameObject, "10,000", "dr", Kind.Normal, 18, "can_repay_debt", "dr_label");
            Btn(r, "Btn_All", 642, 370, 180, 52, "현금 전부", "repay_debt_all", Kind.Normal, 18, "can_repay_debt");
            Box(r, "Line", 32, 500, 790, 1, P("grid"), false);
            Pair(r, "Investable", 32, 520, 790, 30, "투자 가능 자산 (현금+보유 평가)", "investable");
            Pair(r, "TotalDebt", 32, 556, 790, 30, "총부채", "total_debt");
            Pair(r, "Net", 32, 592, 790, 30, "순금융자산", "net", 21, "net_color", true);
            return view;
        }

        // ───────────────────────── 설정 ─────────────────────────

        static SettingsApp BuildSettings(Transform content)
        {
            var root = Node(content, "App_settings", 0, 0, 1732, 872);
            var view = root.AddComponent<SettingsApp>();
            var l = Box(root.transform, "Options", 0, 0, 854, 872, P("panel", Color.white)).transform;
            Txt(l, "Title", 32, 26, 790, 40, "표시와 연출", 28, TextC, TextAnchor.MiddleLeft, true);
            string[,] opts = {
                { "opt:reactions", "캐릭터 반응 말풍선" }, { "opt:skip_seen", "이미 본 컷신 자동 넘기기" }, { "opt:skip_default", "기본 취침 연출 생략" },
                { "opt:exact", "미해금 컷신의 정확한 조건 공개" }, { "opt:markers", "차트에 기사 게시 시점 표시" }, { "opt:confirm", "주문 전 확인 창" } };
            for (int i = 0; i < opts.GetLength(0); i++) Switch(l, "Opt_" + i, 32, 90 + i * 56, 790, 44, opts[i, 1], opts[i, 0]);
            Txt(l, "SpeedLabel", 32, 440, 790, 30, "대사 속도", 22, TextC);
            var ts = RowList(l, "Speeds", 32, 478, 790, 44, "speeds", 150, 10);
            MakeTab(ts.gameObject, "×1", "ts", "ts_selected", 18, "ts_label");
            Txt(l, "Note", 32, 550, 790, 120, "수치·문구·이미지는 Assets/Resources/GameConfig.asset 에서, 화면 배치·색은 씬의 UI 오브젝트에서 고칩니다.", 18, Dim, TextAnchor.UpperLeft);

            var r = Box(root.transform, "Themes", 878, 0, 854, 872, P("panel", Color.white)).transform;
            Txt(r, "Title", 32, 26, 790, 40, "컴퓨터 테마", 28, TextC, TextAnchor.MiddleLeft, true);
            var row = ScrollList(r, "List", 32, 86, 790, 660, "themes", 110, 14);
            AddImage(row.gameObject, P("panel_alt"));
            var sw1 = Box(row, "SwatchTop", 16, 16, 120, 39, P("desktop_top"), false); sw1.gameObject.AddComponent<BindImage>().colorKey = "th_top";
            var sw2 = Box(row, "SwatchBottom", 16, 55, 120, 39, P("desktop_bottom"), false); sw2.gameObject.AddComponent<BindImage>().colorKey = "th_bottom";
            Txt(row, "Name", 156, 14, 450, 34, "기본 파스텔", 22, TextC, TextAnchor.MiddleLeft, true, "th_name");
            Txt(row, "Desc", 156, 52, 450, 50, "설명", 17, Dim, TextAnchor.UpperLeft, false, "th_desc");
            Btn(row, "Btn_Apply", 620, 30, 150, 50, "적용", "th_apply", Kind.Primary, 20, "th_can_apply", "th_button");
            Txt(r, "Note", 32, 780, 790, 60, "테마는 외형만 바꾸며 거래 기능·정보량은 모두 같습니다.", 17, Dim, TextAnchor.UpperLeft);
            return view;
        }

        static void Switch(Transform parent, string name, float x, float y, float w, float h, string label, string key)
        {
            var go = Node(parent, name, x, y, w, h);
            var img = go.AddComponent<Image>(); img.color = new Color(1, 1, 1, 0); img.raycastTarget = true;
            go.AddComponent<Button>().targetGraphic = img;
            go.AddComponent<BindButton>().action = key;
            var off = Box(go.transform, "Off", 0, (h - 26) / 2, 48, 26, P("button"), true, 1f);
            Box(off.transform, "Knob", 2, 2, 22, 22, Color.white, true, 1f).sprite = circle;
            Show(off.gameObject, key, true);
            var on = Box(go.transform, "On", 0, (h - 26) / 2, 48, 26, P("accent"), true, 1f);
            on.gameObject.AddComponent<ThemeColor>().colorKey = "accent";
            Box(on.transform, "Knob", 24, 2, 22, 22, Color.white, true, 1f).sprite = circle;
            Show(on.gameObject, key, false, false);
            Txt(go.transform, "Label", 60, 0, w - 60, h, label, 22, TextC);
        }
    }
}
