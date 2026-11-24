using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 금융 앱: 시작 부채와 추가 투자 대출을 분리.
    /// 글자: loan_rules, loan_principal, loan_interest, loan_due, loan_owed, borrow_preview, borrow_reason, cash_label,
    ///       debt_title, debt_amount, debt_progress, debt_note, investable, total_debt, net
    /// 참거짓: has_loan, no_loan, can_borrow, has_reason / 값: debt_frac / 색: loan_due_color, net_color
    /// 목록 amounts: am_label, am_selected / 행동 am · 목록 loan_repay: lr_label / 행동 lr · 목록 debt_repay: dr_label / 행동 dr
    /// 행동: borrow, repay_loan_all, repay_debt_all
    /// </summary>
    public class FinanceApp : UISource
    {
        int amountIndex;
        GameState S { get { return Game.I.S; } }
        static LoanCfg L { get { return GameConfig.Instance != null ? GameConfig.Instance.balance.loan : new LoanCfg(); } }
        static List<long> DebtOpts { get { return GameConfig.Instance != null ? GameConfig.Instance.balance.debt.repayOptions : new List<long>(); } }

        long SelectedAmount()
        {
            var a = L.amountOptions;
            if (a.Count == 0) return 0;
            amountIndex = Mathf.Clamp(amountIndex, 0, a.Count - 1);
            return Math.Min(a[amountIndex], Game.I.LoanRemainingLimit());
        }

        public override string Text(string key, Ctx c)
        {
            var g = Game.I;
            if (S == null) return null;
            var l = L;
            switch (key)
            {
                case "loan_rules":
                    return "한도 " + Fmt.Won(l.limit) + " · 남은 원금에 하루 " + (l.dailyInterestRate * 100).ToString("0.##") + "% 단리 (하루 최대 " + Fmt.Won(l.dailyInterestCap) + ")\n" +
                           "받은 다음 날부터 이자 · 만기 " + l.termDays + "일 후 결산에서 가능한 만큼 자동 상환 · 연체 시 신규 대출 제한, 알바 수입 " + Mathf.RoundToInt(l.overdueJobIncomeRatio * 100) + "%로 상환";
                case "loan_principal": return S.Loan != null ? Fmt.Won(S.Loan.Principal) : "-";
                case "loan_interest": return S.Loan != null ? Fmt.Won(S.Loan.Interest) : "-";
                case "loan_due": return S.Loan != null ? S.Loan.DueDay + "일차 결산" + (S.Loan.Overdue ? " (연체)" : "") : "-";
                case "loan_owed": return S.Loan != null ? Fmt.Won(S.Loan.Owed) : "-";
                case "borrow_preview":
                    {
                        long amt = SelectedAmount();
                        long principalAfter = (S.Loan != null ? S.Loan.Principal : 0) + amt;
                        long daily = Math.Min((long)Math.Round(principalAfter * l.dailyInterestRate), l.dailyInterestCap);
                        int due = S.Loan != null ? S.Loan.DueDay : S.Day + l.termDays;
                        long est = principalAfter + (S.Loan != null ? S.Loan.Interest : 0) + daily * Math.Max(0, due - S.Day);
                        return "받을 금액 " + Fmt.Won(amt) + " · 대출 후 원금 " + Fmt.Won(principalAfter) + "\n상환일 " + due + "일차 · 그때까지 갚지 않으면 예상 상환액 약 " + Fmt.Won(est);
                    }
                case "borrow_reason": { string r; g.CanBorrow(out r); return r ?? ""; }
                case "cash_label": return "상환 (현금 " + Fmt.Won(S.Cash) + ")";
                case "debt_title": var cfg = GameConfig.Instance; return cfg != null ? cfg.dialogue.system.startingDebtTitle : "시작 부채";
                case "debt_amount": return Fmt.Won(S.StartingDebt);
                case "debt_progress":
                    {
                        long initial = GameConfig.Instance != null ? GameConfig.Instance.balance.debt.startingDebt : S.StartingDebt;
                        double p = initial > 0 ? 1 - (double)S.StartingDebt / initial : 0;
                        return "상환 진척 " + (p * 100).ToString("0.0000") + "% · 지금까지 " + Fmt.Won(initial - S.StartingDebt);
                    }
                case "debt_note": return GameConfig.Instance != null ? GameConfig.Instance.dialogue.system.startingDebtNote : "";
                case "investable": return Fmt.Won(g.InvestableAssets());
                case "total_debt": return Fmt.Won(g.TotalDebt());
                case "net": return Fmt.SignedWon(g.NetFinancialAssets());
                case "am_label": return c.Index < l.amountOptions.Count ? Fmt.Won(l.amountOptions[c.Index]) : "";
                case "lr_label": return c.Index < l.repayOptions.Count ? Fmt.N(l.repayOptions[c.Index]) : "";
                case "dr_label": return c.Index < DebtOpts.Count ? Fmt.N(DebtOpts[c.Index]) : "";
            }
            return null;
        }

        public override bool Flag(string key, Ctx c)
        {
            if (S == null) return false;
            string r;
            switch (key)
            {
                case "has_loan": return S.Loan != null;
                case "no_loan": return S.Loan == null;
                case "can_borrow": return Game.I.CanBorrow(out r) && SelectedAmount() > 0;
                case "has_reason": return !Game.I.CanBorrow(out r);
                case "am_selected": return c.Index == amountIndex;
                case "can_repay_loan": return S.Cash > 0 && Game.I.LoanOwed() > 0;
                case "can_repay_debt": return S.Cash > 0 && S.StartingDebt > 0;
            }
            return false;
        }

        public override float Value(string key, Ctx c)
        {
            if (key == "debt_frac" && S != null)
            {
                long initial = GameConfig.Instance != null ? GameConfig.Instance.balance.debt.startingDebt : S.StartingDebt;
                return initial > 0 ? 1f - (float)((double)S.StartingDebt / initial) : 0;
            }
            return 0;
        }

        public override string ColorKey(string key, Ctx c)
        {
            if (S == null) return null;
            if (key == "loan_due_color") return S.Loan != null && S.Loan.Overdue ? "danger" : "text";
            if (key == "net_color") return Game.I.NetFinancialAssets() >= 0 ? "text" : "text_dim";
            return null;
        }

        public override int Count(string list)
        {
            switch (list)
            {
                case "amounts": return L.amountOptions.Count;
                case "loan_repay": return L.repayOptions.Count;
                case "debt_repay": return DebtOpts.Count;
            }
            return 0;
        }

        public override void Act(string action, Ctx c)
        {
            var g = Game.I;
            var root = GameRoot.I;
            if (S == null) return;
            switch (action)
            {
                case "am": amountIndex = c.Index; break;
                case "borrow":
                    {
                        long amt = SelectedAmount();
                        root.Confirm("대출 확인", Fmt.Won(amt) + "을(를) 빌립니다.\n대출금은 수익으로 집계되지 않으며, 거래 손실과 별개로 상환해야 합니다.", "빌리기", () =>
                        {
                            string err = g.Borrow(amt);
                            root.Toast(err ?? Fmt.Won(amt) + " 대출 입금");
                        });
                        break;
                    }
                case "lr": if (c.Index < L.repayOptions.Count) Repay(true, L.repayOptions[c.Index]); break;
                case "repay_loan_all": Repay(true, g.LoanOwed()); break;
                case "dr": if (c.Index < DebtOpts.Count) Repay(false, DebtOpts[c.Index]); break;
                case "repay_debt_all":
                    root.Confirm("현금 전부 상환", "가진 현금 " + Fmt.Won(S.Cash) + "을(를) 모두 시작 부채 상환에 씁니다. 다음 거래에 쓸 돈이 남지 않습니다.", "상환", () => Repay(false, S.Cash));
                    break;
            }
        }

        public static void Repay(bool loan, long amount)
        {
            var g = Game.I;
            long paid = loan ? g.RepayLoan(amount) : g.RepayStartingDebt(amount);
            if (paid > 0) { GameRoot.I.Toast(Fmt.Won(paid) + " 상환"); g.Save(); }
            else GameRoot.I.Toast("상환할 수 있는 금액이 없습니다.");
        }
    }
}
