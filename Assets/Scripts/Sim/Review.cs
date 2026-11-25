using System;
using System.Collections.Generic;

namespace StockGame
{
    public class ReviewEntry
    {
        public int Tick;
        public string Kind;      // buy / sell / news / post
        public string Text;
        public bool Read;
    }

    /// <summary>
    /// 투자 복기. 그날 실제로 진행된 캔들, 내 매매, 그 시점까지 공개된 글만 모은다.
    /// 아직 공개되지 않은 글·사건의 실제 결과·내부 확률은 다루지 않는다.
    /// </summary>
    public partial class Game
    {
        /// <summary>그날 실제로 진행한 틱 구간 [start, end). 오늘이면 지금 시각까지.</summary>
        public void DayTickRange(int day, out int start, out int end)
        {
            start = day < S.SessionStarts.Count ? S.SessionStarts[day] : S.CurrentTick;
            end = day + 1 < S.SessionStarts.Count && day < S.Day ? S.SessionStarts[day + 1] : S.CurrentTick;
            if (end < start) end = start;
        }

        public List<Candle> ReviewCandles(string id, int day)
        {
            int start, end;
            DayTickRange(day, out start, out end);
            var res = new List<Candle>();
            foreach (var c in ChartCandles(id, 1)) if (c.Tick >= start && c.Tick < end) res.Add(c);
            return res;
        }

        public List<TradeRecord> ReviewTrades(string id, int day)
        {
            var res = new List<TradeRecord>();
            foreach (var t in S.Trades) if (t.Day == day && t.CompanyId == id) res.Add(t);
            return res;
        }

        /// <summary>시간순 기록: 매매와 그 종목 관련 공개 글 (그날 구간, 지금 시각까지 공개된 것만).</summary>
        public List<ReviewEntry> ReviewEntries(string id, int day)
        {
            int start, end;
            DayTickRange(day, out start, out end);
            var res = new List<ReviewEntry>();
            foreach (var t in ReviewTrades(id, day))
            {
                bool buy = t.Side == "buy";
                string txt = (buy ? "매수 " : "매도 ") + Fmt.N(t.Qty) + "주 · " + Fmt.N(t.Price) + "원 · 수수료 " + Fmt.Won(t.Fee);
                if (!buy) txt += " · 실현 " + Fmt.SignedWon(t.Realized);
                res.Add(new ReviewEntry { Tick = t.Tick, Kind = buy ? "buy" : "sell", Text = txt, Read = true });
            }
            foreach (var f in S.Feed)
            {
                if (f.CompanyId != id || f.Tick < start || f.Tick >= end || f.Tick > S.CurrentTick) continue;
                bool news = f.Kind == "news";
                res.Add(new ReviewEntry
                {
                    Tick = f.Tick,
                    Kind = news ? "news" : "post",
                    Text = (news ? "[뉴스] " : "[커뮤니티] ") + f.Title + (f.Read ? "  · 읽음" : "  · 읽지 않음"),
                    Read = f.Read
                });
            }
            res.Sort((a, b) => a.Tick != b.Tick ? a.Tick.CompareTo(b.Tick) : string.CompareOrdinal(a.Kind, b.Kind));
            return res;
        }

        public string ReviewSummary(string id, int day)
        {
            long realized = 0, fees = 0;
            int n = 0;
            foreach (var t in ReviewTrades(id, day)) { realized += t.Realized; fees += t.Fee; n++; }
            var h = HoldingOf(id);
            long unreal = h != null && h.Qty > 0 ? (long)Math.Round(h.Qty * (PriceOf(id) - h.AvgCost)) : 0;
            string s = n == 0 ? "이 종목은 매매하지 않았어요." : "체결 " + n + "건 · 실현 순손익 " + Fmt.SignedWon(realized) + " · 낸 수수료 " + Fmt.Won(fees);
            if (h != null && h.Qty > 0) s += "\n남은 보유 " + Fmt.N(h.Qty) + "주 · 평가손익 " + Fmt.SignedWon(unreal) + " (아직 확정되지 않음)";
            return s;
        }
    }
}
