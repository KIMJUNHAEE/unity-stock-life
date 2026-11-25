using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 집. 방 오브젝트·버튼 위치는 씬에서 바꾼다. 가구 슬롯은 Room/Slot_&lt;id&gt; 에 BindImage(slot:&lt;id&gt;).
    /// 글자: day_phase, cash, holdings, investable, loan_label, loan, starting_debt, condition_text, goal, standing_label, bubble_text,
    ///       shop_cash, slotname:&lt;id&gt;, meet_label, guide_title, guide_now, guide_next, life_progress
    /// 참거짓: morning, evening, night, has_loan, has_goal, bubble, shop_open, slot_visible:&lt;id&gt;, has_meeting, guide_visible
    /// 값: condition / 스프라이트: standing, slot:&lt;id&gt;
    /// 행동: go_job, skip_job, meet, computer, end_day, sleep, open_shop, close_shop, save_title, hot_computer, hot_door, hot_bed, guide_hide
    /// 목록 slots: slot_name, slot_selected / 행동 slot_tab
    /// 목록 items: i_name, i_desc, i_price, i_image, i_buyable, i_can_buy, i_owned, i_equipped / 행동 i_buy, i_equip
    /// </summary>
    public class HomeScreen : UISource
    {
        [Tooltip("피곤할 때 가장자리 흐림 (CanvasGroup)")] public CanvasGroup vignette;
        bool shopOpen;
        string shopSlot;
        readonly List<ShopItemCfg> shopItems = new List<ShopItemCfg>();

        public void OnEnter() { shopOpen = false; }

        GameState S { get { return Game.I.S; } }
        static GameConfig Cfgx { get { return GameConfig.Instance; } }

        void Update()
        {
            if (S == null) { GameRoot.I.Go(ScreenId.Title); return; }
            if (vignette != null) vignette.alpha = (float)J.D(Game.I.Stage(), "vignette", 0) * 0.6f;
            shopItems.Clear();
            if (Cfgx != null)
            {
                if (shopSlot == null && Cfgx.shop.slots.Count > 0) shopSlot = Cfgx.shop.slots[0].id;
                foreach (var it in Cfgx.shop.items) if (it.slot == shopSlot) shopItems.Add(it);
                shopItems.Sort((a, b) => a.price.CompareTo(b.price));
            }
        }

        ShopItemCfg Equipped(string slot)
        {
            string id;
            if (S == null || !S.Equipped.TryGetValue(slot, out id) || Cfgx == null) return null;
            return Cfgx.ShopItem(id);
        }

        ShopItemCfg ItemAt(Ctx c) { return c.Index >= 0 && c.Index < shopItems.Count ? shopItems[c.Index] : null; }

        public override string Text(string key, Ctx c)
        {
            var g = Game.I;
            if (S == null) return null;
            if (key.StartsWith("slotname:")) { var it = Equipped(key.Substring(9)); return it != null ? it.name : ""; }
            switch (key)
            {
                case "day_phase": return S.Day + "일차 · " + (S.Phase == "morning" ? "아침" : (S.Phase == "evening" ? "저녁" : "밤"));
                case "cash": return Fmt.Won(S.Cash);
                case "holdings": return Fmt.Won(g.HoldingsValue());
                case "investable": return Fmt.Won(g.InvestableAssets());
                case "loan_label": return S.Loan != null && S.Loan.Overdue ? "추가 대출 (연체)" : "추가 대출";
                case "loan": return Fmt.Won(g.LoanOwed());
                case "starting_debt": return Fmt.Won(S.StartingDebt);
                case "condition_text": return "컨디션  " + Mathf.RoundToInt((float)S.Condition) + " · " + g.StageName();
                case "goal":
                    {
                        var goal = g.NextGoalItem();
                        if (goal == null) return "";
                        long price = J.L(goal, "price", 0);
                        return J.S(goal, "name", "") + " · " + Fmt.Won(price) + (S.Cash >= price ? "  (구매 가능)" : "");
                    }
                case "standing_label": return "윤서하 입상 (" + GameRoot.I.CurrentExpression() + ")";
                case "bubble_text": return GameRoot.I.BubbleText ?? "";
                case "shop_cash": return "현금 " + Fmt.Won(S.Cash);
                case "door_label": return S.Phase == "morning" ? "편의점 출근" : "문";
                case "bed_label": return S.Phase == "night" ? "잠자기" : "침대";
                case "sleep_label": return "잠자기 → " + (S.Day + 1) + "일차";
                case "meet_label": { var m = g.AvailableMeeting(); return m != null ? m.buttonLabel : ""; }
                case "guide_title": return "오늘 할 일  " + g.GuideDoneCount() + "/" + g.GuideSteps().Count;
                case "guide_now": { var st = g.CurrentGuideStep(); return st != null ? "▶ " + st.Label : "오늘 할 일을 모두 마쳤어요."; }
                case "guide_next":
                    {
                        bool found = false;
                        foreach (var st in g.GuideSteps())
                        {
                            if (st.Done) continue;
                            if (found) return "다음: " + st.Label;
                            found = true;
                        }
                        return "";
                    }
                case "life_progress": return "생활 진척 " + Fmt.SignedWon(g.LifeProgress());
                case "slot_name": return Cfgx != null && c.Index < Cfgx.shop.slots.Count ? Cfgx.shop.slots[c.Index].name : "";
                case "i_name": { var it = ItemAt(c); return it != null ? it.name : ""; }
                case "i_desc": { var it = ItemAt(c); return it != null ? it.description : ""; }
                case "i_price":
                    {
                        var it = ItemAt(c);
                        if (it == null) return "";
                        return S.OwnedItems.Contains(it.id) ? "보유 중" : Fmt.Won(it.price) + "   ·   구매 후 남는 현금 " + Fmt.Won(S.Cash - it.price);
                    }
            }
            return null;
        }

        public override bool Flag(string key, Ctx c)
        {
            if (S == null) return false;
            if (key.StartsWith("slot_visible:"))
            {
                string slot = key.Substring(13);
                if (S.Equipped.ContainsKey(slot)) return true;
                if (Cfgx != null) foreach (var s in Cfgx.shop.slots) if (s.id == slot) return s.showEmpty;
                return false;
            }
            switch (key)
            {
                case "morning": return S.Phase == "morning";
                case "evening": return S.Phase == "evening";
                case "night": return S.Phase == "night";
                case "not_morning": return S.Phase != "morning";
                case "has_meeting": return Game.I.AvailableMeeting() != null;
                case "guide_visible": return Game.I.GuideVisible;
                case "has_loan": return S.Loan != null;
                case "has_goal": return Game.I.NextGoalItem() != null;
                case "bubble": return GameRoot.I.BubbleVisible;
                case "shop_open": return shopOpen;
                case "slot_selected": return Cfgx != null && c.Index < Cfgx.shop.slots.Count && Cfgx.shop.slots[c.Index].id == shopSlot;
                case "i_equipped": { var it = ItemAt(c); string e; return it != null && S.Equipped.TryGetValue(it.slot, out e) && e == it.id; }
                case "i_owned": { var it = ItemAt(c); return it != null && S.OwnedItems.Contains(it.id) && !Flag("i_equipped", c); }
                case "i_buyable": { var it = ItemAt(c); return it != null && !S.OwnedItems.Contains(it.id); }
                case "i_can_buy": { var it = ItemAt(c); return it != null && !S.OwnedItems.Contains(it.id) && S.Cash >= it.price; }
            }
            return false;
        }

        public override float Value(string key, Ctx c)
        {
            if (key == "condition" && S != null) return (float)(S.Condition / Cfg.D("balance", "condition.max_value", 100));
            return 0;
        }

        public override string ColorKey(string key, Ctx c)
        {
            if (key == "loan_color") return S != null && S.Loan != null && S.Loan.Overdue ? "danger" : "text";
            return null;
        }

        public override Sprite SpriteOf(string key, Ctx c)
        {
            if (Cfgx == null) return null;
            if (key.StartsWith("slot:")) { var it = Equipped(key.Substring(5)); return it != null ? it.image : null; }
            if (key == "standing") return Cfgx.StandingOf(GameRoot.I.CurrentExpression());
            if (key == "i_image") { var it = ItemAt(c); return it != null ? it.image : null; }
            return null;
        }

        public override int Count(string list)
        {
            if (Cfgx == null) return 0;
            if (list == "slots") return Cfgx.shop.slots.Count;
            if (list == "items") return shopItems.Count;
            return 0;
        }

        public override void Act(string action, Ctx c)
        {
            var g = Game.I;
            var root = GameRoot.I;
            if (S == null) return;
            switch (action)
            {
                case "go_job": GoJob(); break;
                case "hot_door":
                    if (S.Phase == "morning") GoJob(); else root.Toast("오늘 알바는 이미 지나갔어요.");
                    break;
                case "skip_job": root.Confirm("오늘은 쉬기", "알바 수입 없이 저녁으로 넘어갑니다.\n근무 피로가 없어 수면으로 회복한 컨디션을 그대로 유지해요.", "쉬기", () => g.SkipJob()); break;
                case "meet": Meet(); break;
                case "guide_hide": g.HideGuide(); break;
                case "computer":
                case "hot_computer": root.Go(ScreenId.Desktop); break;
                case "end_day":
                case "sleep":
                case "hot_bed": OnBed(); break;
                case "open_shop": shopOpen = true; break;
                case "close_shop": shopOpen = false; break;
                case "save_title": g.Save(); root.Go(ScreenId.Title); break;
                case "slot_tab": if (Cfgx != null && c.Index < Cfgx.shop.slots.Count) shopSlot = Cfgx.shop.slots[c.Index].id; break;
                case "i_buy":
                    {
                        var it = ItemAt(c);
                        if (it == null) return;
                        string err = g.BuyItem(it.id);
                        if (err != null) root.Toast(err);
                        else if (!string.IsNullOrEmpty(it.purchaseLine)) root.Say(it.purchaseLine, "happy");
                        break;
                    }
                case "i_equip": { var it = ItemAt(c); if (it != null) g.EquipItem(it.id); break; }
            }
        }

        void Meet()
        {
            var g = Game.I;
            var root = GameRoot.I;
            var m = g.AvailableMeeting();
            var scene = m != null && Cfgx != null ? Cfgx.Scene(m.sceneId) : null;
            if (scene == null) return;
            root.Confirm(m.buttonLabel, "오늘은 출근하지 않고 짧게 만나고 옵니다.\n수입은 없고 근무 피로도 없어요.", "다녀오기", () =>
            {
                g.ChooseMeeting(m);
                root.Go(ScreenId.Cutscene);
                root.cutscene.Play(scene, false, () => root.Go(ScreenId.Home));
            });
        }

        void GoJob()
        {
            var root = GameRoot.I;
            root.Go(ScreenId.Job);
            root.job.Begin();
        }

        void OnBed()
        {
            var g = Game.I;
            var root = GameRoot.I;
            if (S.Phase == "night") { g.Sleep(); return; }
            string body = S.Phase == "morning"
                ? "알바와 거래 없이 오늘을 마칩니다. 결산 후 잠자리에 듭니다."
                : (S.CurrentTick < S.SessionEnd && S.Today.TradingStarted ? "장이 아직 열려 있습니다. 보유 주식은 다음 날로 넘어가며, 세션 사이 가격은 고정됩니다." : "오늘 거래를 마치고 결산합니다.");
            root.Confirm("오늘 거래 종료", body, "결산하기", () => root.settlement.Open());
        }
    }
}
