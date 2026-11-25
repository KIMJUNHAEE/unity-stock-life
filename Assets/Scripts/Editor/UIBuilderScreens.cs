using UnityEngine;
using UnityEngine.UI;

namespace StockGame.EditorTools
{
    /// <summary>화면별 기본 배치. 만든 뒤에는 씬에서 자유롭게 옮기고 꾸미면 된다.</summary>
    public static partial class UIBuilder
    {
        static Color TextC { get { return P("text"); } }
        static Color Dim { get { return P("text_dim"); } }

        // ───────────────────────── 타이틀 ─────────────────────────

        static TitleScreen BuildTitle(Transform canvas)
        {
            var root = Full(canvas, "Screen_Title");
            var view = root.AddComponent<TitleScreen>();
            Grad(root.transform, "Background", P("desktop_top"), P("desktop_bottom"), "desktop_top", "desktop_bottom");
            Txt(root.transform, "Title", 0, 230, 1920, 100, "두근두근 트레이딩", 64, TextC, TextAnchor.MiddleCenter, true, "title");
            Btn(root.transform, "Btn_New", 770, 470, 380, 64, "새 게임", "new", Kind.Primary, 22, null, "new_label");
            Btn(root.transform, "Btn_Continue", 770, 550, 380, 64, "이어하기", "continue", Kind.Normal, 22, "has_save");
            Btn(root.transform, "Btn_Gallery", 770, 630, 380, 64, "밤의 기록 (컷신 갤러리)", "gallery");
            Btn(root.transform, "Btn_Quit", 770, 710, 380, 64, "종료", "quit");
            Txt(root.transform, "Footer", 40, 1020, 1840, 30, "MVP 시제품", 16, Dim, TextAnchor.MiddleLeft, false, "root");

            var gal = Full(root.transform, "Gallery");
            Show(gal, "gallery_open", false, false);
            var dim = AddImage(gal, P("dim", new Color(0.17f, 0.23f, 0.32f, 0.4f)), false);
            dim.raycastTarget = true;
            var panel = Panel(gal.transform, "Panel", 160, 90, 1600, 900);
            Txt(panel.transform, "Title", 36, 24, 800, 44, "밤의 기록", 28, TextC, TextAnchor.MiddleLeft, true);
            Btn(panel.transform, "Btn_Close", 1440, 22, 124, 48, "닫기", "gallery_close");
            BuildAlbum(panel.transform, 24, 90, 1552, 790);
            return view;
        }

        static AlbumView BuildAlbum(Transform parent, float x, float y, float w, float h)
        {
            var root = Node(parent, "Album", x, y, w, h);
            var view = root.AddComponent<AlbumView>();
            var cats = RowList(root.transform, "Categories", 0, 0, w - 320, 42, "cats", 120, 8);
            MakeTab(cats.gameObject, "전체", "cat", "cat_selected", 19, "cat_name");
            Txt(root.transform, "Progress", w - 300, 0, 300, 42, "수집 0 / 15", 22, TextC, TextAnchor.MiddleRight, true, "progress");

            var card = ScrollList(root.transform, "Scenes", 0, 56, w - 400, h - 56, "scenes", 270, 16, new Vector2(280, 270));
            RowButton(card, "s_play", P("panel"), true);
            var thumb = Pic(card, "Thumb", 12, 12, 256, 140, "s_image", "일러스트");
            var locked = Box(card, "Locked", 12, 12, 256, 140, P("locked", new Color(0.72f, 0.77f, 0.83f)));
            Txt(locked.transform, "Q", 0, 0, 256, 140, "?", 48, Color.white, TextAnchor.MiddleCenter, true);
            Show(locked.gameObject, "s_locked");
            var badge = Box(card, "New", 200, 18, 56, 26, P("up"));
            Txt(badge.transform, "T", 0, 0, 56, 26, "NEW", 14, Color.white, TextAnchor.MiddleCenter, true);
            Show(badge.gameObject, "s_new");
            Txt(card, "Title", 14, 158, 252, 30, "장면 제목", 21, TextC, TextAnchor.MiddleLeft, true, "s_title");
            Txt(card, "Sub", 14, 190, 252, 72, "힌트", 15, Dim, TextAnchor.UpperLeft, false, "s_sub");

            var story = Box(root.transform, "Story", w - 380, 56, 380, h - 56, P("panel_alt"));
            Show(story.gameObject, "has_run");
            Txt(story.transform, "Title", 20, 14, 340, 34, "이번 회차 이야기", 22, TextC, TextAnchor.MiddleLeft, true);
            var row = ScrollList(story.transform, "List", 16, 60, 348, h - 140, "story", 92, 12);
            AddImage(row.gameObject, P("button"));
            var openBg = New("OpenBg", row); Stretch(openBg); AddImage(openBg, P("panel")); Show(openBg, "st_open");
            Txt(row, "Title", 16, 8, 316, 30, "1. 처음 생긴 여유", 21, TextC, TextAnchor.MiddleLeft, true, "st_title");
            Txt(row, "State", 16, 42, 316, 44, "조건 미정", 15, Dim, TextAnchor.UpperLeft, false, "st_state");
            return view;
        }

        // ───────────────────────── 집 ─────────────────────────

        static HomeScreen BuildHome(Transform canvas)
        {
            var root = Full(canvas, "Screen_Home");
            var view = root.AddComponent<HomeScreen>();

            var room = Node(root.transform, "Room", 0, 0, 1380, 1080);
            var wall = Node(room.transform, "Wall", 0, 0, 1380, 734).AddComponent<GradientGraphic>();
            wall.top = P("room_wall", new Color(0.96f, 0.95f, 0.92f)); wall.bottom = P("room_wall2", new Color(0.92f, 0.94f, 0.97f)); wall.raycastTarget = false;
            Box(room.transform, "Floor", 0, 734, 1380, 346, P("room_floor", new Color(0.89f, 0.83f, 0.76f)), false);
            var frame = Box(room.transform, "Window", 150, 110, 380, 290, Color.white);
            var sky = Node(frame.transform, "Sky", 10, 10, 360, 270).AddComponent<GradientGraphic>();
            sky.top = P("sky_day_top", new Color(0.66f, 0.83f, 0.96f)); sky.bottom = P("sky_day_bottom", new Color(0.91f, 0.96f, 0.99f)); sky.raycastTarget = false;
            var nightSky = Node(frame.transform, "NightSky", 10, 10, 360, 270);
            var ns = nightSky.AddComponent<GradientGraphic>();
            ns.top = P("sky_night_top", new Color(0.37f, 0.44f, 0.66f)); ns.bottom = P("sky_night_bottom", new Color(0.78f, 0.66f, 0.81f)); ns.raycastTarget = false;
            Show(nightSky, "not_morning", false, false);

            var door = Pic(room.transform, "Door", 1170, 250, 170, 520, null, "문", null, new Color(0.91f, 0.86f, 0.80f));
            var bed = Pic(room.transform, "Bed", 720, 640, 580, 230, null, "침대", null, new Color(0.95f, 0.90f, 0.94f));
            door.raycastTarget = bed.raycastTarget = false;

            float[][] slots = {
                new float[] { 100, 560, 440, 220 }, new float[] { 170, 420, 270, 150 }, new float[] { 300, 700, 170, 200 },
                new float[] { 450, 400, 90, 170 }, new float[] { 40, 470, 110, 100 } };
            string[] ids = { "desk", "pc", "chair", "light", "deco" };
            for (int i = 0; i < ids.Length; i++)
            {
                var r = slots[i];
                var s = Pic(room.transform, "Slot_" + ids[i], r[0], r[1], r[2], r[3], "slot:" + ids[i], ids[i], "slotname:" + ids[i], new Color(1f, 0.96f, 0.92f));
                Show(s.gameObject, "slot_visible:" + ids[i]);
            }
            var night = Box(room.transform, "NightTint", 0, 0, 1380, 1080, new Color(0.25f, 0.3f, 0.5f, 0.18f), false);
            Show(night.gameObject, "not_morning", false, false);
            Pic(room.transform, "Seoha", 600, 250, 300, 600, "standing", "윤서하 입상", "standing_label", new Color(0.92f, 0.95f, 0.98f, 0.8f));

            var bubble = Box(room.transform, "Bubble", 500, 150, 500, 90, P("bubble", Color.white));
            Txt(bubble.transform, "Text", 18, 10, 464, 70, "좋은 아침. 오늘도 출근.", 22, TextC, TextAnchor.MiddleCenter, false, "bubble_text");
            Show(bubble.gameObject, "bubble", false, false);

            Hotspot(room.transform, "Hot_Computer", 100, 400, 450, 380, "컴퓨터", "hot_computer", null);
            Hotspot(room.transform, "Hot_Door", 1170, 250, 170, 520, "문", "hot_door", "door_label");
            Hotspot(room.transform, "Hot_Bed", 720, 640, 580, 230, "침대", "hot_bed", "bed_label");

            // 정보 패널
            var p = Panel(root.transform, "InfoPanel", 1400, 40, 480, 1000);
            var t = p.transform;
            Txt(t, "DayPhase", 28, 24, 424, 44, "1일차 · 아침", 34, TextC, TextAnchor.MiddleLeft, true, "day_phase");
            Pair(t, "Cash", 28, 84, 424, 30, "현금", "cash", 22, null, true);
            Pair(t, "Holdings", 28, 120, 424, 30, "보유 주식 평가액", "holdings");
            Pair(t, "Investable", 28, 156, 424, 30, "투자 가능 자산", "investable");
            var loan = Node(t, "LoanRow", 28, 192, 424, 30);
            Txt(loan.transform, "Label", 0, 0, 424, 30, "추가 대출", 21, Dim, TextAnchor.MiddleLeft, false, "loan_label");
            Txt(loan.transform, "Value", 0, 0, 424, 30, "0원", 21, TextC, TextAnchor.MiddleRight, false, "loan", "loan_color");
            Show(loan, "has_loan");
            Pair(t, "Debt", 28, 228, 424, 30, "남은 시작 부채", "starting_debt");
            Txt(t, "Condition", 28, 274, 424, 28, "컨디션 100 · 양호", 22, TextC, TextAnchor.MiddleLeft, false, "condition_text");
            Bar(t, "ConditionBar", 28, 306, 424, 14, "condition", P("mint"));
            var goal = Box(t, "Goal", 28, 336, 424, 76, P("panel_alt"));
            Txt(goal.transform, "Label", 16, 8, 392, 26, "다음 생활 목표", 18, Dim);
            Txt(goal.transform, "Value", 16, 36, 392, 30, "스탠드 조명 · 25,000원", 21, TextC, TextAnchor.MiddleLeft, true, "goal");
            Show(goal.gameObject, "has_goal");

            var morning = Node(t, "Buttons_Morning", 28, 430, 424, 200);
            Show(morning, "morning");
            Btn(morning.transform, "Btn_Job", 0, 0, 424, 58, "편의점 출근 (아침 알바)", "go_job", Kind.Primary);
            Btn(morning.transform, "Btn_Skip", 0, 70, 424, 58, "오늘은 알바 쉬기", "skip_job");
            Btn(morning.transform, "Btn_Computer", 0, 140, 424, 58, "컴퓨터 켜기 (장 시작 전)", "computer");
            var evening = Node(t, "Buttons_Evening", 28, 430, 424, 200);
            Show(evening, "evening", false, false);
            Btn(evening.transform, "Btn_Computer", 0, 0, 424, 58, "컴퓨터 켜기 (저녁 거래)", "computer", Kind.Primary);
            Btn(evening.transform, "Btn_End", 0, 70, 424, 58, "오늘 거래 마치고 결산", "end_day");
            var nightB = Node(t, "Buttons_Night", 28, 430, 424, 200);
            Show(nightB, "night", false, false);
            Btn(nightB.transform, "Btn_Sleep", 0, 0, 424, 58, "잠자기 → 다음 날", "sleep", Kind.Primary, 22, null, "sleep_label");
            Btn(nightB.transform, "Btn_Computer", 0, 70, 424, 58, "컴퓨터 켜기 (기록 보기)", "computer");
            Btn(t, "Btn_Shop", 28, 640, 424, 58, "가구 카탈로그", "open_shop");
            Btn(t, "Btn_SaveTitle", 28, 924, 424, 52, "저장하고 타이틀로", "save_title", Kind.Ghost);

            view.vignette = Vignette(root.transform);

            // 가구 카탈로그
            var shop = Full(root.transform, "Shop");
            Show(shop, "shop_open", false, false);
            var dim = AddImage(shop, P("dim", new Color(0.17f, 0.23f, 0.32f, 0.4f)), false);
            dim.raycastTarget = true;
            var sp = Panel(shop.transform, "Panel", 260, 90, 1400, 900);
            Txt(sp.transform, "Title", 36, 24, 700, 44, "가구 카탈로그", 28, TextC, TextAnchor.MiddleLeft, true);
            Txt(sp.transform, "Cash", 36, 24, 1180, 44, "현금 0원", 22, TextC, TextAnchor.MiddleRight, true, "shop_cash");
            Btn(sp.transform, "Btn_Close", 1240, 22, 124, 48, "닫기", "close_shop");
            var tabs = RowList(sp.transform, "SlotTabs", 36, 88, 1200, 46, "slots", 140, 10);
            MakeTab(tabs.gameObject, "책상", "slot_tab", "slot_selected", 20, "slot_name");
            var row = ScrollList(sp.transform, "Items", 24, 150, 1352, 726, "items", 150, 12);
            AddImage(row.gameObject, P("panel_alt"));
            Pic(row, "Image", 14, 14, 180, 122, "i_image", "가구", "i_name");
            Txt(row, "Name", 214, 16, 700, 34, "스탠드 조명", 26, TextC, TextAnchor.MiddleLeft, true, "i_name");
            Txt(row, "Desc", 214, 54, 760, 54, "설명", 19, Dim, TextAnchor.UpperLeft, false, "i_desc");
            Txt(row, "Price", 214, 108, 760, 30, "25,000원", 19, TextC, TextAnchor.MiddleLeft, false, "i_price");
            var buy = Btn(row, "Btn_Buy", 1112, 47, 200, 56, "구매", "i_buy", Kind.Primary, 22, "i_can_buy");
            Show(buy.gameObject, "i_buyable");
            var eq = Btn(row, "Btn_Equip", 1112, 47, 200, 56, "배치하기", "i_equip");
            Show(eq.gameObject, "i_owned", false, false);
            var on = Txt(row, "Equipped", 1112, 47, 200, 56, "배치 중", 22, Dim, TextAnchor.MiddleCenter, true);
            Show(on.gameObject, "i_equipped", false, false);
            return view;
        }

        static void Hotspot(Transform parent, string name, float x, float y, float w, float h, string label, string action, string labelBind)
        {
            var go = Node(parent, name, x, y, w, h);
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.25f);
            img.raycastTarget = true;
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = new Color(1, 1, 1, 0);
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(1, 1, 1, 0.6f);
            cb.selectedColor = new Color(1, 1, 1, 0);
            b.colors = cb;
            go.AddComponent<BindButton>().action = action;
            var chip = Box(go.transform, "Chip", w / 2 - 80, -40, 160, 34, P("toast_bg", new Color(0.23f, 0.3f, 0.39f, 0.9f)));
            Txt(chip.transform, "Text", 0, 0, 160, 34, label, 18, Color.white, TextAnchor.MiddleCenter, true, labelBind);
        }

        // ───────────────────────── 알바 ─────────────────────────

        static JobScreen BuildJob(Transform canvas)
        {
            var root = Full(canvas, "Screen_Job");
            var view = root.AddComponent<JobScreen>();
            Grad(root.transform, "Background", P("store_top", new Color(0.92f, 0.96f, 0.95f)), P("store_bottom", new Color(0.97f, 0.95f, 0.91f)));
            Box(root.transform, "Counter", 0, 640, 1260, 440, P("store_counter", new Color(0.89f, 0.84f, 0.78f)), false);

            var intro = Panel(root.transform, "Intro", 560, 220, 800, 600);
            Show(intro.gameObject, "intro");
            var it = intro.transform;
            Txt(it, "Title", 40, 30, 720, 50, "아침 편의점 근무", 30, TextC, TextAnchor.MiddleLeft, true);
            Txt(it, "Howto", 40, 96, 720, 190, "설명", 22, TextC, TextAnchor.UpperLeft, false, "howto");
            Txt(it, "Info1", 40, 300, 720, 30, "근무 시간", 22, TextC, TextAnchor.MiddleLeft, false, "intro_info1");
            Txt(it, "Info2", 40, 336, 720, 30, "보너스", 22, Dim, TextAnchor.MiddleLeft, false, "intro_info2");
            var od = Txt(it, "Overdue", 40, 372, 720, 30, "연체", 19, P("danger"), TextAnchor.MiddleLeft, false, "overdue_note");
            Show(od.gameObject, "overdue");
            Btn(it, "Btn_Start", 40, 510, 340, 60, "근무 시작", "start", Kind.Primary);
            Btn(it, "Btn_Back", 420, 510, 340, 60, "집으로 돌아가기", "back");

            var play = Full(root.transform, "Playing");
            Show(play, "playing", false, false);
            var pt = play.transform;
            var top = Panel(pt, "TopBar", 40, 24, 1840, 70);
            Txt(top.transform, "Time", 24, 0, 300, 70, "남은 시간 75초", 26, TextC, TextAnchor.MiddleLeft, true, "time");
            Bar(top.transform, "TimeBar", 300, 28, 700, 14, "time_frac", P("accent"));
            Txt(top.transform, "Stats", 1040, 0, 776, 70, "응대 0명 · 실수 0회", 22, TextC, TextAnchor.MiddleRight, false, "stats");

            var cust = Node(pt, "Customer", 120, 140, 1100, 480);
            Show(cust, "has_customer");
            Bar(cust.transform, "Patience", 110, 30, 300, 12, "patience", P("mint"), "patience_color");
            Pic(cust.transform, "Image", 110, 60, 300, 400, "customer", "손님", "customer_name");
            var speech = Box(cust.transform, "Speech", 530, 80, 560, 110, P("bubble", Color.white));
            Txt(speech.transform, "Text", 24, 10, 512, 90, "이거 계산해 주세요.", 28, TextC, TextAnchor.MiddleLeft, true, "speech");
            Show(speech.gameObject, "speaking");

            var flash = Box(pt, "Flash", 660, 330, 560, 60, P("toast_bg", new Color(0.23f, 0.3f, 0.39f, 0.9f)));
            Txt(flash.transform, "Text", 0, 0, 560, 60, "감사합니다!", 22, Color.white, TextAnchor.MiddleCenter, false, "flash_text");
            Show(flash.gameObject, "flash", false, false);

            var hint = Txt(pt, "ScanHint", 110, 650, 1000, 30, "상품을 눌러 바코드를 스캔하세요", 19, Dim);
            Show(hint.gameObject, "scanning");
            var card = RowList(pt, "CounterItems", 110, 700, 1060, 230, "counter", 210, 20, true, TextAnchor.UpperCenter);
            RowButton(card, "scan", Color.white);
            card.gameObject.AddComponent<BindImage>().colorKey = "c_color";
            var ci = Node(card, "Image", 20, 16, 170, 120);
            var cimg = ci.AddComponent<Image>(); cimg.color = Color.white; cimg.preserveAspect = true; cimg.raycastTarget = false;
            var cb = ci.AddComponent<BindImage>(); cb.spriteKey = "c_image"; cb.hideWhenEmpty = true;
            Txt(card, "Name", 10, 140, 190, 36, "삼각김밥", 22, TextC, TextAnchor.MiddleCenter, true, "c_name");
            Txt(card, "Barcode", 10, 176, 190, 30, "|||| 8801043", 15, Dim, TextAnchor.MiddleCenter, false, "c_barcode");

            var pos = Panel(pt, "POS", 1300, 130, 580, 900);
            var ps = pos.transform;
            Txt(ps, "Title", 28, 20, 524, 40, "POS", 26, TextC, TextAnchor.MiddleLeft, true);
            var prow = RowList(ps, "Scanned", 28, 80, 524, 330, "pos", 38, 0, false);
            Txt(prow, "Name", 0, 0, 360, 38, "삼각김밥", 22, TextC, TextAnchor.MiddleLeft, false, "p_name");
            Txt(prow, "Price", 0, 0, 524, 38, "1,300원", 22, TextC, TextAnchor.MiddleRight, false, "p_price");
            Box(ps, "Line", 28, 420, 524, 1, P("grid"), false);
            Txt(ps, "TotalLabel", 28, 430, 524, 44, "합계", 26, TextC);
            Txt(ps, "Total", 28, 430, 524, 44, "0원", 26, TextC, TextAnchor.MiddleRight, true, "pos_total");
            var payLabel = Txt(ps, "PayLabel", 28, 500, 524, 30, "결제 방법", 22, Dim);
            Show(payLabel.gameObject, "paying");
            var pay = RowList(ps, "Payments", 28, 540, 524, 240, "payments", 64, 12, false);
            MakeButton(pay.gameObject, "카드", "pay", Kind.Normal, 22, null, "pay_label");
            var chgLabel = Txt(ps, "ChangePrompt", 28, 500, 524, 30, "거스름돈은?", 22, TextC, TextAnchor.MiddleLeft, false, "change_prompt");
            Show(chgLabel.gameObject, "change", false, false);
            var chg = RowList(ps, "Changes", 28, 540, 524, 240, "changes", 64, 12, false);
            MakeButton(chg.gameObject, "1,000원", "chg", Kind.Normal, 22, null, "chg_label");

            var res = Panel(root.transform, "Result", 560, 200, 800, 640);
            Show(res.gameObject, "result", false, false);
            var rt = res.transform;
            Txt(rt, "Title", 40, 30, 720, 50, "근무 종료", 30, TextC, TextAnchor.MiddleLeft, true);
            Pair(rt, "Served", 40, 110, 720, 36, "응대한 손님", "r_served", 22, null, true);
            Pair(rt, "Mistakes", 40, 154, 720, 36, "실수 (돌아간 손님 포함)", "r_mistakes", 22, null, true);
            Pair(rt, "Base", 40, 198, 720, 36, "기본급", "r_base", 22, null, true);
            Pair(rt, "Bonus", 40, 242, 720, 36, "근무 보너스", "r_bonus", 22, null, true);
            var gar = Node(rt, "GarnishRow", 40, 286, 720, 36);
            Pair(gar.transform, "Garnish", 0, 0, 720, 36, "연체 대출 자동 상환", "r_garnish", 22, null, true);
            Show(gar, "r_has_garnish");
            Box(rt, "Line", 40, 332, 720, 1, P("grid"), false);
            Pair(rt, "Total", 40, 344, 720, 36, "받은 금액", "r_total", 24, null, true);
            Txt(rt, "Note", 40, 400, 720, 30, "이제 저녁입니다. 컴퓨터에서 거래를 시작할 수 있어요.", 19, Dim);
            Btn(rt, "Btn_Done", 40, 550, 720, 60, "집으로", "done", Kind.Primary);
            return view;
        }

        // ───────────────────────── 결산·스토리·컷신 ─────────────────────────

        static SettlementScreen BuildSettlement(Transform canvas)
        {
            var root = Full(canvas, "Screen_Settlement");
            var view = root.AddComponent<SettlementScreen>();
            Grad(root.transform, "Background", P("room_wall_night", new Color(0.79f, 0.81f, 0.9f)), P("room_wall_night2", new Color(0.9f, 0.84f, 0.9f)));
            var p = Panel(root.transform, "Panel", 260, 60, 1400, 960);
            var t = p.transform;
            Txt(t, "Title", 40, 26, 800, 48, "1일차 결산", 36, TextC, TextAnchor.MiddleLeft, true, "title");
            Txt(t, "Subtitle", 40, 26, 1320, 48, "오늘 체결 0건", 22, Dim, TextAnchor.MiddleRight, false, "subtitle");
            Txt(t, "LeftTitle", 40, 100, 640, 34, "오늘의 수입과 거래", 24, TextC, TextAnchor.MiddleLeft, true);
            var lrow = ScrollList(t, "Left", 40, 150, 640, 700, "left", 40, 0);
            Txt(lrow, "Label", 0, 0, 630, 40, "알바 수입", 21, Dim, TextAnchor.MiddleLeft, false, "r_label");
            Txt(lrow, "Value", 0, 0, 620, 40, "60,000원", 21, TextC, TextAnchor.MiddleRight, false, "r_value", "r_color");
            Txt(t, "RightTitle", 720, 100, 640, 34, "지금의 자산과 부채", 24, TextC, TextAnchor.MiddleLeft, true);
            var rrow = RowList(t, "Right", 720, 150, 640, 170, "right", 40, 0, false);
            Txt(rrow, "Label", 0, 0, 640, 40, "현금", 21, Dim, TextAnchor.MiddleLeft, false, "r_label");
            Txt(rrow, "Value", 0, 0, 640, 40, "0원", 21, TextC, TextAnchor.MiddleRight, false, "r_value", "r_color");

            var loan = Node(t, "LoanRepay", 720, 340, 640, 100);
            Show(loan, "has_loan");
            Txt(loan.transform, "Title", 0, 0, 640, 34, "추가 대출 상환", 24, TextC, TextAnchor.MiddleLeft, true);
            var lr = RowList(loan.transform, "Buttons", 0, 44, 500, 48, "loan_repay", 156, 10);
            MakeButton(lr.gameObject, "10,000", "loan_repay", Kind.Normal, 18, "can_repay", "label");
            Btn(loan.transform, "Btn_All", 510, 44, 130, 48, "전액", "loan_all", Kind.Primary, 18, "can_repay");
            Txt(t, "DebtTitle", 720, 460, 640, 34, "시작 부채 상환 (선택)", 24, TextC, TextAnchor.MiddleLeft, true);
            var dr = RowList(t, "DebtButtons", 720, 504, 640, 48, "debt_repay", 156, 10);
            MakeButton(dr.gameObject, "10,000", "debt_repay", Kind.Normal, 18, "can_repay", "label");
            var rep = Txt(t, "Repaid", 720, 570, 640, 30, "오늘 상환", 18, P("mint_text", new Color(0.18f, 0.61f, 0.47f)), TextAnchor.MiddleLeft, false, "repaid_today");
            Show(rep.gameObject, "has_repaid");

            Txt(t, "Footnote", 40, 868, 620, 64, "확정 후에는 오늘 거래를 다시 열 수 없습니다.", 18, Dim, TextAnchor.MiddleLeft, false, "footnote");
            Btn(t, "Btn_Back", 700, 868, 320, 64, "아직 아니에요 (돌아가기)", "back");
            Btn(t, "Btn_Confirm", 1040, 868, 320, 64, "결산 확정", "confirm", Kind.Primary);
            return view;
        }

        static StoryScreen BuildStory(Transform canvas)
        {
            var root = Full(canvas, "Screen_Story");
            var view = root.AddComponent<StoryScreen>();
            Grad(root.transform, "Background", P("cutscene_top", new Color(0.79f, 0.87f, 0.95f)), P("cutscene_bottom", new Color(0.95f, 0.85f, 0.9f)));
            Txt(root.transform, "Subtitle", 0, 160, 1920, 40, "새 이야기 구간", 24, Dim, TextAnchor.MiddleCenter, false, "subtitle");
            Txt(root.transform, "Title", 0, 210, 1920, 90, "처음 생긴 여유", 60, TextC, TextAnchor.MiddleCenter, true, "title");
            Txt(root.transform, "Placeholder", 360, 330, 1200, 80, "본편 장면은 아직 작성 전입니다.", 22, Dim, TextAnchor.UpperCenter, false, "placeholder");
            var ch = RowList(root.transform, "Choices", 540, 480, 840, 420, "choices", 90, 20, false);
            MakeButton(ch.gameObject, "작은 생활 개선", "ch", Kind.Normal, 28, null, "ch_label");
            return view;
        }

        static CutscenePlayer BuildCutscene(Transform canvas)
        {
            var root = Full(canvas, "Screen_Cutscene");
            var view = root.AddComponent<CutscenePlayer>();
            AddImage(root, P("cutscene_bg", new Color(0.12f, 0.15f, 0.2f)), false);
            var img = Full(root.transform, "Illustration");
            var im = img.AddComponent<Image>();
            im.raycastTarget = false;
            var bi = img.AddComponent<BindImage>(); bi.spriteKey = "image"; bi.hideWhenEmpty = true;
            view.zoomTarget = (RectTransform)img.transform;
            var none = Full(root.transform, "NoIllustration");
            Show(none, "no_image");
            Grad(none.transform, "Gradient", P("cutscene_top", new Color(0.79f, 0.87f, 0.95f)), P("cutscene_bottom", new Color(0.95f, 0.85f, 0.9f)));
            Txt(none.transform, "Title", 0, 300, 1920, 80, "장면 제목", 56, TextC, TextAnchor.MiddleCenter, true, "title");
            Txt(none.transform, "Hint", 0, 390, 1920, 40, "[일러스트 자리]", 18, Dim, TextAnchor.MiddleCenter, false, "image_hint");

            var adv = Full(root.transform, "ClickToAdvance");
            var ai = adv.AddComponent<Image>(); ai.color = new Color(0, 0, 0, 0); ai.raycastTarget = true;
            adv.AddComponent<Button>().targetGraphic = ai;
            adv.AddComponent<BindButton>().action = "advance";

            Txt(root.transform, "Replay", 40, 24, 600, 36, "다시 보기", 19, Dim, TextAnchor.MiddleLeft, false, "replay_label");
            var box = Box(root.transform, "Dialog", 160, 780, 1600, 250, new Color(1, 1, 1, 0.94f), true, 1.4f);
            box.raycastTarget = false;
            Txt(box.transform, "Speaker", 40, 24, 1520, 36, "서하", 26, P("accent_dark", new Color(0.31f, 0.53f, 0.76f)), TextAnchor.MiddleLeft, true, "speaker");
            Txt(box.transform, "Text", 40, 70, 1520, 150, "대사", 28, TextC, TextAnchor.UpperLeft, false, "text");
            Txt(box.transform, "Progress", 0, 210, 1570, 30, "1 / 3  ▶ 클릭", 15, Dim, TextAnchor.MiddleRight, false, "progress");
            Btn(root.transform, "Btn_Skip", 1720, 24, 170, 48, "건너뛰기", "skip");
            return view;
        }

        // ───────────────────────── 겹치는 층 ─────────────────────────

        static ModalLayer BuildModal(Transform canvas)
        {
            var root = Full(canvas, "Layer_Modal");
            var view = root.AddComponent<ModalLayer>();
            var dim = AddImage(root, P("dim", new Color(0.17f, 0.23f, 0.32f, 0.4f)), false);
            dim.raycastTarget = true;
            var p = Panel(root.transform, "Panel", 600, 360, 720, 340);
            Txt(p.transform, "Title", 40, 28, 640, 40, "확인", 28, TextC, TextAnchor.MiddleLeft, true, "title");
            Txt(p.transform, "Body", 40, 82, 640, 160, "내용", 22, TextC, TextAnchor.UpperLeft, false, "body");
            var b = RowList(p.transform, "Buttons", 40, 262, 640, 52, "buttons", 200, 14, true, TextAnchor.MiddleRight);
            MakeButton(b.gameObject, "확인", "b", Kind.Normal, 22, null, "b_label");
            b.gameObject.AddComponent<BindImage>().colorKey = "b_color";
            b.Find("Label").GetComponent<BindText>().colorKey = "b_text_color";
            return view;
        }

        static ToastLayer BuildToasts(Transform canvas)
        {
            var root = Full(canvas, "Layer_Toast");
            var g = root.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false;
            g.interactable = false;
            var view = root.AddComponent<ToastLayer>();
            var row = RowList(root.transform, "Toasts", 460, 70, 1000, 300, "toasts", 44, 8, false, TextAnchor.UpperCenter);
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = P("toast_bg", new Color(0.23f, 0.3f, 0.39f, 0.9f));
            bg.sprite = rounded; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1.5f; bg.raycastTarget = false;
            Txt(row, "Text", 20, 0, 960, 44, "알림", 19, Color.white, TextAnchor.MiddleCenter, false, "toast");
            row.gameObject.SetActive(false);
            return view;
        }

        static DebugPanel BuildDebug(Transform canvas)
        {
            var root = Node(canvas, "Layer_Debug", 1450, 60, 450, 960);
            var view = root.AddComponent<DebugPanel>();
            var bg = AddImage(root, new Color(1, 1, 1, 0.97f), true, 1.6f);
            bg.raycastTarget = true;
            var t = root.transform;
            Txt(t, "Title", 20, 12, 300, 36, "디버그 (F12)", 24, P("danger"), TextAnchor.MiddleLeft, true);
            Btn(t, "Btn_Close", 340, 12, 90, 36, "닫기", "close", Kind.Normal, 16);
            string[,] btns = {
                { "설정·데이터 다시 읽기", "reload", "" }, { "현금 +100,000", "cash_100k", "has_run" }, { "현금 +1,000,000", "cash_1m", "has_run" },
                { "컨디션 100", "cond_100", "has_run" }, { "컨디션 15", "cond_15", "has_run" }, { "저녁으로 (알바 생략)", "skip_job", "morning" },
                { "시장 30분 진행", "adv_30", "market_open" }, { "장 끝까지 진행", "adv_end", "market_open" }, { "결산 화면", "settle", "not_night" },
                { "저장", "save", "has_run" }, { "갤러리 초기화", "reset_gallery", "has_run" }, { "사건 목록", "toggle_events", "has_run" } };
            for (int i = 0; i < btns.GetLength(0); i++)
            {
                float x = 20 + (i % 2) * 209, y = 60 + (i / 2) * 54;
                string en = btns[i, 2].Length > 0 ? btns[i, 2] : null;
                Btn(t, "Btn_" + btns[i, 1], x, y, 201, 46, btns[i, 0], btns[i, 1], i == 10 ? Kind.Danger : Kind.Normal, 16, en, i == 11 ? "events_label" : null);
            }
            Txt(t, "Info", 20, 390, 410, 40, "정보", 15, Dim, TextAnchor.UpperLeft, false, "info");
            var ev = ScrollList(t, "Events", 20, 440, 410, 500, "events", 78, 6);
            AddImage(ev.gameObject, P("panel_alt"));
            Txt(ev, "L1", 10, 4, 380, 24, "EVT0001", 15, TextC, TextAnchor.MiddleLeft, true, "e_line1");
            Txt(ev, "L2", 10, 28, 380, 22, "소문", 14, Dim, TextAnchor.MiddleLeft, false, "e_line2");
            Txt(ev, "L3", 10, 50, 380, 22, "효과", 14, Dim, TextAnchor.MiddleLeft, false, "e_line3");
            root.SetActive(false);
            return view;
        }

        static InfoOverlay BuildInfo(Transform canvas)
        {
            var root = Node(canvas, "Layer_Info", 60, 60, 1000, 900);
            var view = root.AddComponent<InfoOverlay>();
            var bg = AddImage(root, new Color(1, 1, 1, 0.97f), true, 1.6f);
            bg.raycastTarget = true;
            Txt(root.transform, "Title", 28, 20, 800, 40, "편집 방법과 데이터 상태 (F1)", 28, TextC, TextAnchor.MiddleLeft, true);
            Btn(root.transform, "Btn_Close", 860, 20, 110, 40, "닫기", "close", Kind.Normal, 18);
            Txt(root.transform, "Info", 28, 80, 944, 800, "정보", 18, TextC, TextAnchor.UpperLeft, false, "info");
            root.SetActive(false);
            return view;
        }
    }
}
