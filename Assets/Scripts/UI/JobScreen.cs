using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 아침 편의점 알바: 상품 스캔 → 결제 방법 → (현금) 거스름돈. 수치·상품·대사는 GameConfig ▸ 편의점 알바.
    /// 참거짓: intro, playing, result, has_customer, speaking, scanning, paying, change, flash, overdue, r_has_garnish
    /// 글자: howto, intro_info1, intro_info2, overdue_note, time, stats, speech, flash_text, customer_name, pos_total, change_prompt,
    ///       r_served, r_mistakes, r_base, r_bonus, r_garnish, r_total
    /// 값: time_frac, patience / 스프라이트: customer / 색: patience_color
    /// 목록 counter: c_name, c_barcode, c_image, 색 c_color / 행동 scan
    /// 목록 pos: p_name, p_price · 목록 payments: pay_label / 행동 pay · 목록 changes: chg_label / 행동 chg
    /// 행동: start, back, done
    /// </summary>
    public class JobScreen : UISource
    {
        enum Phase { Intro, Playing, Result }
        enum Step { Scanning, Paying, Change, Leaving }

        class Customer
        {
            public CustomerCfg Who;
            public List<int> Items = new List<int>();
            public List<bool> Scanned = new List<bool>();
            public PaymentCfg Payment;
            public string PaymentLine, Greeting;
            public float Patience, PatienceMax;
            public Step Step;
            public long Total, Paid;
            public List<long> ChangeOptions = new List<long>();
            public float LeaveTimer;
        }

        Phase phase;
        float timeLeft, duration;
        int served, mistakes;
        Customer cur;
        float nextCustomerDelay;
        readonly System.Random rng = new System.Random();
        string flash;
        float flashTimer;
        long resultPay, resultGarnish, resultBase, resultBonus;
        readonly List<int> counter = new List<int>();
        readonly List<int> pos = new List<int>();

        static JobConfig C { get { return GameConfig.Instance != null ? GameConfig.Instance.job : new JobConfig(); } }

        public void Begin()
        {
            phase = Phase.Intro;
            served = mistakes = 0;
            cur = null;
        }

        void Update()
        {
            counter.Clear(); pos.Clear();
            if (cur != null)
                for (int i = 0; i < cur.Items.Count; i++) { if (cur.Scanned[i]) pos.Add(i); else counter.Add(i); }

            if (phase != Phase.Playing || GameRoot.I.HasModal) return;
            float dt = Time.unscaledDeltaTime;
            timeLeft -= dt;
            if (flashTimer > 0) flashTimer -= dt;
            if (timeLeft <= 0) { Finish(); return; }
            if (cur == null)
            {
                nextCustomerDelay -= dt;
                if (nextCustomerDelay <= 0) cur = NewCustomer();
                return;
            }
            if (cur.Step == Step.Leaving)
            {
                cur.LeaveTimer -= dt;
                if (cur.LeaveTimer <= 0) { cur = null; nextCustomerDelay = C.nextCustomerDelay; }
                return;
            }
            cur.Patience -= dt;
            if (cur.Patience <= 0)
            {
                mistakes++;
                Flash(Pick(C.lines.customerLeft, "손님이 기다리다 돌아갔어요…"));
                cur.Step = Step.Leaving;
                cur.LeaveTimer = 0.8f;
            }
        }

        string Pick(List<string> list, string def) { return list == null || list.Count == 0 ? def : list[rng.Next(list.Count)]; }
        void Flash(string t) { flash = t; flashTimer = 1.6f; }

        Customer NewCustomer()
        {
            var c = new Customer();
            var cfg = C;
            int n = rng.Next((int)cfg.itemsPerCustomer.x, (int)cfg.itemsPerCustomer.y + 1);
            for (int i = 0; i < n && cfg.items.Count > 0; i++)
            {
                int idx = rng.Next(cfg.items.Count);
                c.Items.Add(idx);
                c.Scanned.Add(false);
                c.Total += cfg.items[idx].price;
            }
            if (cfg.customers.Count > 0) c.Who = cfg.customers[rng.Next(cfg.customers.Count)];
            c.Payment = cfg.payments.Count > 0 ? cfg.payments[rng.Next(cfg.payments.Count)] : new PaymentCfg();
            c.PaymentLine = Pick(c.Payment.lines, "카드로 할게요.");
            c.Greeting = Pick(cfg.lines.greetings, "이거 계산해 주세요.");
            c.PatienceMax = c.Patience = Mathf.Lerp(cfg.customerPatienceSeconds.x, cfg.customerPatienceSeconds.y, (float)rng.NextDouble());
            c.Step = c.Items.Count > 0 ? Step.Scanning : Step.Paying;
            return c;
        }

        void OnPayment(PaymentCfg p)
        {
            if (cur == null || cur.Step != Step.Paying) return;
            if (p.id != cur.Payment.id)
            {
                mistakes++;
                Flash(Pick(C.lines.wrongPayment, "앗, 결제 방법이 달라요."));
                return;
            }
            if (p.id == "cash")
            {
                cur.Paid = PaidAmount(cur.Total);
                long change = cur.Paid - cur.Total;
                var set = new HashSet<long> { change };
                long[] deltas = { 100, 500, 1000, 200, 300 };
                int guard = 0;
                while (set.Count < 3 && guard++ < 50)
                {
                    long d = deltas[rng.Next(deltas.Length)] * (rng.Next(2) == 0 ? 1 : -1);
                    if (change + d >= 0) set.Add(change + d);
                }
                cur.ChangeOptions = new List<long>(set);
                for (int i = cur.ChangeOptions.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); long t = cur.ChangeOptions[i]; cur.ChangeOptions[i] = cur.ChangeOptions[j]; cur.ChangeOptions[j] = t; }
                cur.Step = Step.Change;
                return;
            }
            Served();
        }

        long PaidAmount(long total)
        {
            var list = new List<long>(C.cashBills);
            list.Sort();
            foreach (var b in list) if (b >= total && rng.Next(3) != 0) return b;
            return ((total + 9999) / 10000) * 10000;
        }

        void OnChange(long amount)
        {
            if (cur == null || cur.Step != Step.Change) return;
            if (amount != cur.Paid - cur.Total)
            {
                mistakes++;
                Flash(Pick(C.lines.wrongChange, "거스름돈이 맞지 않아요."));
                return;
            }
            Served();
        }

        void Served()
        {
            served++;
            cur.Step = Step.Leaving;
            cur.LeaveTimer = 0.7f;
            Flash(Pick(C.lines.thanks, "감사합니다!"));
        }

        void Finish()
        {
            var cfg = C;
            phase = Phase.Result;
            resultBase = cfg.basePay;
            long bonus = served * cfg.bonusPerCustomer - mistakes * cfg.mistakePenalty;
            if (mistakes == 0 && served >= cfg.perfectMinCustomers) bonus += cfg.perfectBonus;
            resultBonus = Math.Max(0, bonus);
            resultPay = resultBase + resultBonus;
            resultGarnish = Game.I.CompleteJob(resultPay);
            Game.I.React("job_end");
        }

        JobItemCfg ItemOf(int custIndex) { var items = C.items; int idx = cur.Items[custIndex]; return idx < items.Count ? items[idx] : null; }

        // ───────────────────────── 바인딩 ─────────────────────────

        public override bool Flag(string key, Ctx c)
        {
            switch (key)
            {
                case "intro": return phase == Phase.Intro;
                case "playing": return phase == Phase.Playing;
                case "result": return phase == Phase.Result;
                case "has_customer": return phase == Phase.Playing && cur != null;
                case "speaking": return phase == Phase.Playing && cur != null && cur.Step != Step.Leaving;
                case "scanning": return phase == Phase.Playing && cur != null && cur.Step == Step.Scanning;
                case "paying": return phase == Phase.Playing && cur != null && cur.Step == Step.Paying;
                case "change": return phase == Phase.Playing && cur != null && cur.Step == Step.Change;
                case "flash": return flashTimer > 0 && !string.IsNullOrEmpty(flash);
                case "overdue": return Game.I.S != null && Game.I.S.Loan != null && Game.I.S.Loan.Overdue;
                case "r_has_garnish": return resultGarnish > 0;
            }
            return false;
        }

        public override string Text(string key, Ctx c)
        {
            var cfg = C;
            switch (key)
            {
                case "howto": return cfg.howto;
                case "intro_info1": return "근무 시간 " + Mathf.RoundToInt(cfg.durationSeconds) + "초 · 기본급 " + Fmt.Won(cfg.basePay);
                case "intro_info2": return "손님당 보너스 " + Fmt.Won(cfg.bonusPerCustomer) + " · 실수당 보너스 −" + Fmt.Won(cfg.mistakePenalty);
                case "overdue_note": return "대출 연체 중: 수입의 " + Mathf.RoundToInt(Cfg.F("balance", "loan.overdue_job_income_ratio", 0.3f) * 100) + "%가 상환에 쓰입니다.";
                case "time": return "남은 시간 " + Mathf.CeilToInt(Mathf.Max(0, timeLeft)) + "초";
                case "stats": return "응대 " + served + "명 · 실수 " + mistakes + "회";
                case "speech":
                    if (cur == null) return "";
                    return cur.Step == Step.Scanning ? cur.Greeting : (cur.Step == Step.Paying ? cur.PaymentLine : "여기요. (" + Fmt.Won(cur.Paid) + " 냄)");
                case "flash_text": return flash ?? "";
                case "customer_name": return cur != null && cur.Who != null ? cur.Who.name : "손님";
                case "pos_total": { long t = 0; foreach (var i in pos) { var it = ItemOf(i); if (it != null) t += it.price; } return Fmt.Won(t); }
                case "change_prompt": return cur != null ? "받은 돈 " + Fmt.Won(cur.Paid) + " · 거스름돈은?" : "";
                case "c_name": { var it = c.Index < counter.Count ? ItemOf(counter[c.Index]) : null; return it != null ? it.name : ""; }
                case "c_barcode": { var it = c.Index < counter.Count ? ItemOf(counter[c.Index]) : null; return it != null ? "|||| " + it.barcode : ""; }
                case "p_name": { var it = c.Index < pos.Count ? ItemOf(pos[c.Index]) : null; return it != null ? it.name : ""; }
                case "p_price": { var it = c.Index < pos.Count ? ItemOf(pos[c.Index]) : null; return it != null ? Fmt.Won(it.price) : ""; }
                case "pay_label": return c.Index < cfg.payments.Count ? cfg.payments[c.Index].label : "";
                case "chg_label": return cur != null && c.Index < cur.ChangeOptions.Count ? Fmt.Won(cur.ChangeOptions[c.Index]) : "";
                case "r_served": return served + "명";
                case "r_mistakes": return mistakes + "회";
                case "r_base": return Fmt.Won(resultBase);
                case "r_bonus": return Fmt.Won(resultBonus);
                case "r_garnish": return "−" + Fmt.Won(resultGarnish);
                case "r_total": return Fmt.Won(resultPay - resultGarnish);
            }
            return null;
        }

        public override float Value(string key, Ctx c)
        {
            if (key == "time_frac") return duration > 0 ? timeLeft / duration : 0;
            if (key == "patience") return cur != null && cur.PatienceMax > 0 ? cur.Patience / cur.PatienceMax : 0;
            return 0;
        }

        public override string ColorKey(string key, Ctx c)
        {
            if (key == "patience_color") return Value("patience", c) > 0.35f ? "mint" : "warning";
            if (key == "c_color")
            {
                var it = c.Index < counter.Count ? ItemOf(counter[c.Index]) : null;
                return it != null ? "#" + ColorUtility.ToHtmlStringRGBA(it.color) : null;
            }
            return null;
        }

        public override Sprite SpriteOf(string key, Ctx c)
        {
            if (key == "customer") return cur != null && cur.Who != null ? cur.Who.image : null;
            if (key == "c_image") { var it = c.Index < counter.Count ? ItemOf(counter[c.Index]) : null; return it != null ? it.image : null; }
            return null;
        }

        public override int Count(string list)
        {
            switch (list)
            {
                case "counter": return Flag("scanning", Ctx.None) ? counter.Count : 0;
                case "pos": return cur != null ? pos.Count : 0;
                case "payments": return Flag("paying", Ctx.None) ? C.payments.Count : 0;
                case "changes": return Flag("change", Ctx.None) ? cur.ChangeOptions.Count : 0;
            }
            return 0;
        }

        public override void Act(string action, Ctx c)
        {
            switch (action)
            {
                case "start":
                    phase = Phase.Playing;
                    duration = C.durationSeconds;
                    timeLeft = duration;
                    nextCustomerDelay = 0.5f;
                    Game.I.React("job_start");
                    break;
                case "back": GameRoot.I.Go(ScreenId.Home); break;
                case "done": GameRoot.I.Go(ScreenId.Home); break;
                case "scan":
                    if (cur != null && cur.Step == Step.Scanning && c.Index < counter.Count)
                    {
                        cur.Scanned[counter[c.Index]] = true;
                        if (!cur.Scanned.Contains(false)) cur.Step = Step.Paying;
                    }
                    break;
                case "pay": if (c.Index < C.payments.Count) OnPayment(C.payments[c.Index]); break;
                case "chg": if (cur != null && c.Index < cur.ChangeOptions.Count) OnChange(cur.ChangeOptions[c.Index]); break;
            }
        }
    }
}
