using System;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 증권 앱. 차트는 CandleChart 컴포넌트, 나머지는 키로 연결.
    /// 탭: 행동 tab:trade / tab:holdings / tab:history, 참거짓 tab_is:&lt;탭&gt;
    /// 글자: cash, name, price, change, markers_label, hold_qty, hold_avg, hold_pnl, qty, cur_price, amount, fees, need, receive,
    ///       max_buy, reason, hold_summary, today_summary, market_note
    /// 색: change_color, pnl_color / 참거짓: has_reason, can_buy, can_sell, has_holding, chart_scrolled, markers_on, no_holdings, no_history
    /// 목록 watch: w_name, w_price, w_change, w_qty, 색 w_color, w_selected / 행동 w_select
    /// 목록 speeds: sp_label, sp_selected / 행동 sp · 목록 tfs: tf_label, tf_selected / 행동 tf
    /// 목록 holdings: h_name, h_qty, h_avg, h_price, h_value, h_pnl, h_rate, 색 h_color / 행동 h_chart
    /// 목록 history: t_time, t_name, t_side, t_qty, t_price, t_amount, t_fee, t_realized, 색 t_side_color, t_real_color
    /// 행동: zoom_in, zoom_out, markers, chart_latest, qty_minus, qty_plus, qty_10, qty_100, qty_max, qty_hold, buy, sell
    /// </summary>
    public class StockApp : UISource
    {
        string tab = "trade";
        long qty = 1;
        static readonly int[] Timeframes = { 1, 5, 15 };
        readonly List<string> holdIds = new List<string>();

        GameState S { get { return Game.I.S; } }
        string Sel { get { return ChartState.Symbol; } }

        public void Select(string id) { ChartState.Symbol = id; ChartState.ScrollBack = 0; tab = "trade"; }

        void Update()
        {
            if (S == null) return;
            if (Sel == null || !S.Stocks.ContainsKey(Sel)) ChartState.Symbol = S.Watchlist.Count > 0 ? S.Watchlist[0] : (S.Tradable.Count > 0 ? S.Tradable[0] : null);
            holdIds.Clear();
            holdIds.AddRange(S.Holdings.Keys);
        }

        StockState St(string id) { StockState st; return id != null && S != null && S.Stocks.TryGetValue(id, out st) ? st : null; }

        string WatchId(Ctx c) { return S != null && c.Index >= 0 && c.Index < S.Watchlist.Count ? S.Watchlist[c.Index] : null; }
        TradeRecord TradeAt(Ctx c) { int i = S.Trades.Count - 1 - c.Index; return i >= 0 && i < S.Trades.Count ? S.Trades[i] : null; }

        static List<float> Speeds()
        {
            var list = new List<float>();
            var cfg = GameConfig.Instance;
            if (cfg != null) list.AddRange(cfg.balance.market.speedOptions);
            if (list.Count == 0) list.Add(1);
            return list;
        }

        public override string Text(string key, Ctx c)
        {
            var g = Game.I;
            if (S == null) return null;
            var st = St(Sel);
            var h = Sel != null ? g.HoldingOf(Sel) : null;
            double price = st != null ? st.Price : 0;
            long amount = (long)Math.Round(price * qty);
            switch (key)
            {
                case "cash": return "거래 가능 현금  " + Fmt.Won(S.Cash);
                case "name": return Sel != null ? Game.CompanyName(Sel) : "";
                case "price": return Fmt.Won(price);
                case "change":
                    {
                        if (st == null) return "";
                        double d = st.Price - st.PrevClose;
                        return Fmt.Arrow(d) + " " + Fmt.N(Math.Abs(d)) + " (" + Fmt.Pct(st.PrevClose > 0 ? d / st.PrevClose : 0) + ")";
                    }
                case "markers_label": return g.Settings.ShowNewsMarkers ? "기사 표시 ✓" : "기사 표시";
                case "hold_qty": return h != null ? h.Qty + "주" : "0주";
                case "hold_avg": return h != null ? Fmt.Won(h.AvgCost) : "-";
                case "hold_pnl":
                    if (h == null) return "-";
                    return Fmt.SignedWon((long)Math.Round((price - h.AvgCost) * h.Qty)) + " (" + Fmt.Pct(h.AvgCost > 0 ? price / h.AvgCost - 1 : 0) + ")";
                case "qty": return qty + "주";
                case "cur_price": return Fmt.Won(price);
                case "amount": return Fmt.Won(amount);
                case "fees": return Fmt.N(g.FeeFor(amount, false)) + " / " + Fmt.N(g.FeeFor(amount, true)) + "원";
                case "need": return Fmt.Won(amount + g.FeeFor(amount, false));
                case "receive": return Fmt.Won(Math.Max(0, amount - g.FeeFor(amount, true)));
                case "max_buy": return "최대 매수 가능 " + Fmt.N(Sel != null ? g.MaxBuyQty(Sel) : 0) + "주";
                case "reason": { string r; g.CanTrade(out r); return r ?? ""; }
                case "hold_summary": return "보유 평가액 " + Fmt.Won(g.HoldingsValue()) + "   ·   평가손익 " + Fmt.SignedWon(g.UnrealizedPnl());
                case "today_summary": return "오늘 실현 순손익 " + Fmt.SignedWon(S.Today.Realized) + " (수수료 반영) · 오늘 체결 " + S.Today.Trades + "건";
                case "market_note": var cfg = GameConfig.Instance; return cfg != null ? cfg.dialogue.system.marketNote : "";
                case "w_name": { var id = WatchId(c); return id != null ? Game.CompanyName(id) : ""; }
                case "w_price": { var w = St(WatchId(c)); return w != null ? Fmt.Won(w.Price) : ""; }
                case "w_change": { var w = St(WatchId(c)); if (w == null) return ""; double d = w.Price - w.PrevClose; return Fmt.Arrow(d) + " " + Fmt.Pct(w.PrevClose > 0 ? d / w.PrevClose : 0); }
                case "w_qty": { var id = WatchId(c); var hh = id != null ? g.HoldingOf(id) : null; return hh != null ? hh.Qty + "주" : ""; }
                case "sp_label": { var sp = Speeds(); return c.Index < sp.Count ? "×" + sp[c.Index] : ""; }
                case "tf_label": return c.Index < Timeframes.Length ? Timeframes[c.Index] + "분" : "";
            }
            if (key.StartsWith("h_"))
            {
                if (c.Index < 0 || c.Index >= holdIds.Count) return "";
                string id = holdIds[c.Index];
                var hh = S.Holdings[id];
                double p = g.PriceOf(id);
                switch (key)
                {
                    case "h_name": return Game.CompanyName(id);
                    case "h_qty": return hh.Qty + "주";
                    case "h_avg": return Fmt.Won(hh.AvgCost);
                    case "h_price": return Fmt.Won(p);
                    case "h_value": return Fmt.Won(p * hh.Qty);
                    case "h_pnl": return Fmt.SignedWon((long)Math.Round((p - hh.AvgCost) * hh.Qty));
                    case "h_rate": return Fmt.Pct(hh.AvgCost > 0 ? p / hh.AvgCost - 1 : 0);
                }
            }
            if (key.StartsWith("t_"))
            {
                var t = TradeAt(c);
                if (t == null) return "";
                bool sell = t.Side == "sell";
                switch (key)
                {
                    case "t_time": return t.Day + "일 " + g.ClockOfTick(t.Tick);
                    case "t_name": return Game.CompanyName(t.CompanyId);
                    case "t_side": return sell ? "▼ 매도" : "▲ 매수";
                    case "t_qty": return t.Qty + "주";
                    case "t_price": return Fmt.Won(t.Price);
                    case "t_amount": return Fmt.Won(t.Amount);
                    case "t_fee": return Fmt.Won(t.Fee);
                    case "t_realized": return sell ? Fmt.SignedWon(t.Realized) : "-";
                }
            }
            return null;
        }

        public override bool Flag(string key, Ctx c)
        {
            var g = Game.I;
            if (S == null) return false;
            if (key.StartsWith("tab_is:")) return tab == key.Substring(7);
            string r;
            switch (key)
            {
                case "has_reason": return !g.CanTrade(out r);
                case "can_buy":
                    {
                        if (Sel == null || !g.CanTrade(out r) || qty <= 0) return false;
                        long amt = (long)Math.Round(g.PriceOf(Sel) * qty);
                        return amt + g.FeeFor(amt, false) <= S.Cash;
                    }
                case "can_sell": return Sel != null && g.CanTrade(out r) && qty > 0 && g.HoldingOf(Sel) != null;
                case "has_holding": return Sel != null && g.HoldingOf(Sel) != null;
                case "chart_scrolled": return ChartState.ScrollBack > 0;
                case "markers_on": return g.Settings.ShowNewsMarkers;
                case "no_holdings": return S.Holdings.Count == 0;
                case "no_history": return S.Trades.Count == 0;
                case "w_selected": return WatchId(c) == Sel;
                case "sp_selected": { var sp = Speeds(); return c.Index < sp.Count && GameRoot.I.desktop != null && Mathf.Approximately(GameRoot.I.desktop.Speed, sp[c.Index]); }
                case "tf_selected": return c.Index < Timeframes.Length && ChartState.Timeframe == Timeframes[c.Index];
            }
            return false;
        }

        public override string ColorKey(string key, Ctx c)
        {
            var g = Game.I;
            if (S == null) return null;
            switch (key)
            {
                case "change_color": { var st = St(Sel); return st != null ? Palette.UpDown(st.Price - st.PrevClose) : null; }
                case "pnl_color": { var h = Sel != null ? g.HoldingOf(Sel) : null; return h != null ? Palette.UpDown(g.PriceOf(Sel) - h.AvgCost) : "text"; }
                case "w_color": { var w = St(WatchId(c)); return w != null ? Palette.UpDown(w.Price - w.PrevClose) : null; }
                case "h_color": { if (c.Index >= holdIds.Count) return null; var id = holdIds[c.Index]; return Palette.UpDown(g.PriceOf(id) - S.Holdings[id].AvgCost); }
                case "t_side_color": { var t = TradeAt(c); return t == null ? null : (t.Side == "sell" ? "down" : "up"); }
                case "t_real_color": { var t = TradeAt(c); return t == null || t.Side != "sell" ? "text" : Palette.UpDown(t.Realized); }
            }
            return null;
        }

        public override int Count(string list)
        {
            if (S == null) return 0;
            switch (list)
            {
                case "watch": return S.Watchlist.Count;
                case "speeds": return Speeds().Count;
                case "tfs": return Timeframes.Length;
                case "holdings": return holdIds.Count;
                case "history": return S.Trades.Count;
            }
            return 0;
        }

        public override void Act(string action, Ctx c)
        {
            var g = Game.I;
            if (S == null) return;
            if (action.StartsWith("tab:")) { tab = action.Substring(4); return; }
            long maxBuy = Sel != null ? g.MaxBuyQty(Sel) : 0;
            var h = Sel != null ? g.HoldingOf(Sel) : null;
            switch (action)
            {
                case "w_select": { var id = WatchId(c); if (id != null) { ChartState.Symbol = id; ChartState.ScrollBack = 0; } break; }
                case "h_chart": if (c.Index < holdIds.Count) Select(holdIds[c.Index]); break;
                case "sp": { var sp = Speeds(); if (c.Index < sp.Count && GameRoot.I.desktop != null) GameRoot.I.desktop.Speed = sp[c.Index]; break; }
                case "tf": if (c.Index < Timeframes.Length) { ChartState.Timeframe = Timeframes[c.Index]; ChartState.ScrollBack = 0; } break;
                case "zoom_in": ChartState.Visible = Mathf.Max(Cfg.I("theme", "chart.min_visible_candles", 20), (int)(ChartState.Visible / 1.3f)); break;
                case "zoom_out": ChartState.Visible = Mathf.Min(Cfg.I("theme", "chart.max_visible_candles", 300), (int)(ChartState.Visible * 1.3f) + 1); break;
                case "markers": g.Settings.ShowNewsMarkers = !g.Settings.ShowNewsMarkers; g.SaveMeta(); break;
                case "chart_latest": ChartState.ScrollBack = 0; break;
                case "qty_minus": qty = Math.Max(0, qty - 1); break;
                case "qty_plus": qty++; break;
                case "qty_10": qty += 10; break;
                case "qty_100": qty += 100; break;
                case "qty_max": qty = Math.Max(maxBuy, h != null ? h.Qty : 0); break;
                case "qty_hold": qty = h != null ? h.Qty : 0; break;
                case "buy": DoOrder(true); break;
                case "sell": DoOrder(false); break;
            }
        }

        void DoOrder(bool buy)
        {
            var g = Game.I;
            var root = GameRoot.I;
            long q = qty;
            string id = Sel;
            if (id == null) return;
            Action exec = () =>
            {
                string err = buy ? g.Buy(id, q) : g.Sell(id, q);
                if (err != null) root.Toast(err);
                else root.Toast((buy ? "매수 체결: " : "매도 체결: ") + Game.CompanyName(id) + " " + q + "주 @ " + Fmt.Won(S.Trades[S.Trades.Count - 1].Price));
            };
            if (g.Settings.OrderConfirm)
            {
                double p = g.PriceOf(id);
                long amt = (long)Math.Round(p * q);
                root.Confirm(buy ? "매수 확인" : "매도 확인",
                    Game.CompanyName(id) + " " + q + "주\n현재가 " + Fmt.Won(p) + " · 금액 " + Fmt.Won(amt) + " · 수수료 " + Fmt.Won(g.FeeFor(amt, !buy)) + "\n확인 창이 열린 동안 시장은 멈춰 있습니다.",
                    buy ? "매수" : "매도", exec);
            }
            else exec();
        }
    }
}
