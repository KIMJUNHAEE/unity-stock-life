using System;
using System.Collections.Generic;

namespace StockGame
{
    /// <summary>시장가 매수·매도, 추가 투자 대출과 상환.</summary>
    public partial class Game
    {
        // ───────────────────────── 매매 ─────────────────────────

        public bool CanTrade(out string reason)
        {
            reason = null;
            if (S.Phase == "morning") { reason = "저녁 거래 시간이 아닙니다. 먼저 알바를 마치거나 쉬기를 선택하세요."; return false; }
            if (S.Phase == "night" || S.Today.Settled) { reason = "오늘 거래는 종료되었습니다."; return false; }
            if (S.CurrentTick >= S.SessionEnd) { reason = "장이 마감되었습니다."; return false; }
            if (S.Condition <= 0) { reason = "컨디션이 바닥났습니다. 회복하거나 거래를 마치세요."; return false; }
            return true;
        }

        public long FeeFor(long amount, bool sell)
        {
            double rate = Cfg.D("balance", "trading.fee_rate", 0.001);
            long fee = (long)Math.Round(amount * rate);
            long minFee = Cfg.L("balance", "trading.min_fee", 0);
            if (amount > 0 && fee < minFee) fee = minFee;
            if (sell) fee += (long)Math.Round(amount * Cfg.D("balance", "trading.sell_tax_rate", 0.0));
            return fee;
        }

        public long MaxBuyQty(string id)
        {
            double p = PriceOf(id);
            if (p <= 0) return 0;
            long q = (long)Math.Floor(S.Cash / (p * (1 + Cfg.D("balance", "trading.fee_rate", 0.001))));
            while (q > 0 && (long)Math.Round(p * q) + FeeFor((long)Math.Round(p * q), false) > S.Cash) q--;
            return Math.Max(0, q);
        }

        public Holding HoldingOf(string id)
        {
            Holding h;
            return S.Holdings.TryGetValue(id, out h) ? h : null;
        }

        /// <summary>성공하면 null, 실패하면 사유.</summary>
        public string Buy(string id, long qty)
        {
            string reason;
            if (!CanTrade(out reason)) return reason;
            if (qty <= 0) return "수량을 정하세요.";
            EnsureTradingStarted();
            double price = PriceOf(id);
            long amount = (long)Math.Round(price * qty);
            long fee = FeeFor(amount, false);
            if (amount + fee > S.Cash) return "거래 가능 현금이 부족합니다.";
            S.Cash -= amount + fee;
            var h = HoldingOf(id);
            if (h == null) { h = new Holding(); S.Holdings[id] = h; }
            h.AvgCost = (h.AvgCost * h.Qty + amount + fee) / (h.Qty + qty);
            h.Qty += qty;

            // 컷신 판정용 기록: 이 매수 전에 읽은 소문/기사
            foreach (var e in S.Events)
            {
                if (e.CompanyId != id) continue;
                if (S.ReadRumorTick.ContainsKey(e.Id) && S.CurrentTick < e.OfficialTick) h.EarlyRumorEvent = e.Id;
                if (S.ReadNewsTick.ContainsKey(e.Id) && e.OfficialPublished && e.Tone == "positive") h.NewsChaseEvent = e.Id;
            }

            S.Trades.Add(new TradeRecord { Day = S.Day, Tick = S.CurrentTick, CompanyId = id, Side = "buy", Qty = qty, Price = price, Amount = amount, Fee = fee });
            S.Today.Trades++;
            S.Today.Buys++;
            S.Today.FeesPaid += fee;
            React(S.Today.TookLoan ? "buy_with_loan" : "buy");
            return null;
        }

        public string Sell(string id, long qty)
        {
            string reason;
            if (!CanTrade(out reason)) return reason;
            var h = HoldingOf(id);
            if (h == null || h.Qty <= 0) return "보유 수량이 없습니다.";
            if (qty <= 0) return "수량을 정하세요.";
            if (qty > h.Qty) qty = h.Qty;
            EnsureTradingStarted();
            double price = PriceOf(id);
            long amount = (long)Math.Round(price * qty);
            long fee = FeeFor(amount, true);
            long proceeds = amount - fee;
            long realized = (long)Math.Round(proceeds - h.AvgCost * qty);
            S.Cash += proceeds;
            h.Qty -= qty;

            var early = EventById(h.EarlyRumorEvent);
            if (early != null && early.OfficialPublished)
            {
                if (early.Tone == "positive" && realized > 0) S.Today.RumorEarlyProfit += realized;
                if (early.Tone == "negative" && realized < 0) S.Today.FalseRumorLoss += -realized;
            }
            if (EventById(h.NewsChaseEvent) != null && realized < 0) S.Today.NewsChaseLoss += -realized;
            if (h.Qty <= 0) S.Holdings.Remove(id);

            S.Trades.Add(new TradeRecord { Day = S.Day, Tick = S.CurrentTick, CompanyId = id, Side = "sell", Qty = qty, Price = price, Amount = amount, Fee = fee, Realized = realized });
            S.Today.Trades++;
            S.Today.Sells++;
            S.Today.FeesPaid += fee;
            S.Today.Realized += realized;
            if (realized > 0) React(realized >= Cfg.L("dialogue", "thresholds.big_profit", 50000) ? "sell_big_profit" : "sell_profit");
            else if (realized < 0) React(-realized >= Cfg.L("dialogue", "thresholds.big_loss", 50000) ? "sell_big_loss" : "sell_loss");
            else React("sell_flat");
            return null;
        }

        // ───────────────────────── 대출 ─────────────────────────

        public long LoanLimit() { return Cfg.L("balance", "loan.limit", 500000); }
        public long LoanRemainingLimit() { return Math.Max(0, LoanLimit() - (S.Loan == null ? 0 : S.Loan.Principal)); }

        public bool CanBorrow(out string reason)
        {
            reason = null;
            if (!Cfg.B("balance", "loan.enabled", true)) { reason = "대출 기능이 꺼져 있습니다."; return false; }
            if (S.Loan != null && S.Loan.Overdue) { reason = "연체 중에는 새로 빌릴 수 없습니다."; return false; }
            if (S.Phase == "night" || S.Today.Settled) { reason = "오늘 거래가 끝난 뒤에는 빌릴 수 없습니다."; return false; }
            if (Cfg.B("balance", "loan.only_before_trading_starts", true) && S.Today.TradingStarted) { reason = "대출은 저녁 거래 시작 전에만 받을 수 있습니다."; return false; }
            long threshold = Cfg.L("balance", "loan.cash_threshold", 0);
            if (threshold > 0 && S.Cash >= threshold) { reason = "현금이 " + Fmt.Won(threshold) + " 이상이면 대출을 받을 수 없습니다."; return false; }
            if (LoanRemainingLimit() <= 0) { reason = "남은 대출 한도가 없습니다."; return false; }
            return true;
        }

        public string Borrow(long amount)
        {
            string reason;
            if (!CanBorrow(out reason)) return reason;
            amount = Math.Min(amount, LoanRemainingLimit());
            if (amount <= 0) return "금액을 정하세요.";
            if (S.Loan == null) S.Loan = new Loan { TakenDay = S.Day, DueDay = S.Day + Cfg.I("balance", "loan.term_days", 5) };
            S.Loan.Principal += amount;
            S.Cash += amount;
            S.Today.TookLoan = true;
            S.Today.LoanAmount += amount;
            S.EverTookLoan = true;
            React("loan_taken");
            Save();
            return null;
        }

        /// <summary>이자부터 갚고 원금을 갚는다. 실제 상환액을 돌려준다.</summary>
        public long RepayLoan(long amount)
        {
            if (S.Loan == null) return 0;
            amount = Math.Min(amount, Math.Min(S.Cash, S.Loan.Owed));
            if (amount <= 0) return 0;
            long left = amount;
            long toInterest = Math.Min(left, S.Loan.Interest);
            S.Loan.Interest -= toInterest;
            left -= toInterest;
            S.Loan.Principal -= Math.Min(left, S.Loan.Principal);
            S.Cash -= amount;
            S.Today.RepaidLoan += amount;
            if (S.Loan.Owed <= 0) { S.Loan = null; React("loan_cleared"); }
            else React("repay");
            return amount;
        }

        public long RepayStartingDebt(long amount)
        {
            amount = Math.Min(amount, Math.Min(S.Cash, S.StartingDebt));
            if (amount <= 0) return 0;
            S.StartingDebt -= amount;
            S.Cash -= amount;
            S.Today.RepaidStartingDebt += amount;
            React("repay_starting_debt");
            return amount;
        }

        /// <summary>결산 시 1회: 지난날 대출에 하루 이자, 만기 자동 상환과 연체 판정.</summary>
        void AccrueInterestAndMaturity()
        {
            if (S.Loan == null) return;
            if (S.Loan.TakenDay < S.Day)
            {
                long interest = (long)Math.Round(S.Loan.Principal * Cfg.D("balance", "loan.daily_interest_rate", 0.01));
                long cap = Cfg.L("balance", "loan.daily_interest_cap", 10000);
                if (cap > 0) interest = Math.Min(interest, cap);
                long maxTotal = Cfg.L("balance", "loan.max_accumulated_interest", 100000);
                if (maxTotal > 0) interest = Math.Min(interest, Math.Max(0, maxTotal - S.Loan.Interest));
                S.Loan.Interest += interest;
                S.Today.InterestCharged += interest;
            }
            if (S.Day >= S.Loan.DueDay)
            {
                long paid = RepayLoan(S.Loan.Owed);
                S.Today.AutoRepaid += paid;
                if (S.Loan != null && S.Loan.Owed > 0)
                {
                    if (!S.Loan.Overdue) Toast("대출 만기: 남은 금액이 연체되었습니다. 알바 수입 일부가 상환에 쓰입니다.");
                    S.Loan.Overdue = true;
                }
            }
        }

        /// <summary>연체 중 알바 수입 일부 자동 상환. 남는 금액은 다음 투자금으로 남는다.</summary>
        long GarnishJobPay(long pay)
        {
            if (S.Loan == null || !S.Loan.Overdue) return 0;
            long g = (long)Math.Round(pay * Cfg.D("balance", "loan.overdue_job_income_ratio", 0.3));
            g = Math.Min(g, S.Loan.Owed);
            if (g <= 0) return 0;
            S.Cash += g;          // RepayLoan 이 현금에서 빼므로 먼저 넣는다
            long paid = RepayLoan(g);
            S.Cash -= g - paid;
            if (S.Loan != null && S.Loan.Owed <= 0) S.Loan = null;
            return paid;
        }
    }
}
