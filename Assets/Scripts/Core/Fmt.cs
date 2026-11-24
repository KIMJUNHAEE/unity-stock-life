using System;
using System.Globalization;

namespace StockGame
{
    /// <summary>금액·시각 표기. 폰트에 ₩ 기호가 없어 "1,250,000원" 형식을 쓴다.</summary>
    public static class Fmt
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Won(long v) { return v.ToString("N0", Inv) + "원"; }
        public static string Won(double v) { return Won((long)Math.Round(v)); }
        public static string N(long v) { return v.ToString("N0", Inv); }
        public static string N(double v) { return ((long)Math.Round(v)).ToString("N0", Inv); }

        public static string Signed(long v) { return (v > 0 ? "+" : "") + v.ToString("N0", Inv); }
        public static string SignedWon(long v) { return Signed(v) + "원"; }

        public static string Pct(double ratio, int digits = 2)
        {
            if (double.IsNaN(ratio) || double.IsInfinity(ratio)) return "-";
            string s = (ratio * 100).ToString("F" + digits, Inv);
            return (ratio > 0 ? "+" : "") + s + "%";
        }

        /// <summary>▲/▼ 기호 포함 변동 표기. 색만으로 구분하지 않는다는 기획 기준.</summary>
        public static string Arrow(double diff)
        {
            if (diff > 0) return "▲";
            if (diff < 0) return "▼";
            return "–";
        }

        /// <summary>하루 기준 분(0~1439+) → "21:30".</summary>
        public static string Clock(int minuteOfDay)
        {
            int m = ((minuteOfDay % 1440) + 1440) % 1440;
            return (m / 60).ToString("00") + ":" + (m % 60).ToString("00");
        }
    }
}
