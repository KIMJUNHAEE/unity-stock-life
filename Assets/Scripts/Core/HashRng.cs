using System;
using System.Collections.Generic;

namespace StockGame
{
    /// <summary>
    /// 상태 없는 결정적 난수. (시드, 종목, 틱, 하위 단계) 를 섞어 값을 만든다.
    /// 저장 파일에 난수 상태를 넣지 않아도 같은 시드면 같은 시장이 나온다.
    /// </summary>
    public static class Hash
    {
        public static ulong Mix(ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }

        public static ulong H(ulong a, ulong b) { return Mix(a ^ Mix(b + 0x632BE59BD9B4E019UL)); }
        public static ulong H(ulong a, ulong b, ulong c) { return H(H(a, b), c); }
        public static ulong H(ulong a, ulong b, ulong c, ulong d) { return H(H(H(a, b), c), d); }

        public static ulong Str(string s)
        {
            ulong h = 14695981039346656037UL;
            if (s != null)
                foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            return Mix(h);
        }

        public static double U01(ulong h) { return (h >> 11) * (1.0 / 9007199254740992.0); }

        /// <summary>표준정규분포 값 (Box–Muller).</summary>
        public static double Normal(ulong h)
        {
            double u1 = U01(h);
            double u2 = U01(Mix(h ^ 0xD1B54A32D192ED03UL));
            if (u1 < 1e-12) u1 = 1e-12;
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }

    /// <summary>시드 + 카운터 방식의 난수열. 카운터만 저장하면 이어서 재현된다.</summary>
    public class StreamRng
    {
        public ulong Seed;
        public long Counter;

        public StreamRng(ulong seed, long counter) { Seed = seed; Counter = counter; }

        public double Next() { return Hash.U01(Hash.H(Seed, (ulong)Counter++)); }
        public double Range(double min, double max) { return min + (max - min) * Next(); }

        /// <summary>정수 [min, max] 양끝 포함.</summary>
        public int RangeInt(int min, int max)
        {
            if (max <= min) return min;
            int v = min + (int)Math.Floor(Next() * (max - min + 1));
            return v > max ? max : v;
        }

        public double Normal() { return Hash.Normal(Hash.H(Seed, (ulong)Counter++)); }

        public T Pick<T>(IList<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[RangeInt(0, list.Count - 1)];
        }

        /// <summary>가중치 선택. 가중치 0 이하 항목은 제외. 후보가 없으면 -1.</summary>
        public int PickWeighted(IList<double> weights)
        {
            double total = 0;
            for (int i = 0; i < weights.Count; i++) if (weights[i] > 0) total += weights[i];
            if (total <= 0) return -1;
            double r = Next() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0) continue;
                r -= weights[i];
                if (r <= 0) return i;
            }
            for (int i = weights.Count - 1; i >= 0; i--) if (weights[i] > 0) return i;
            return -1;
        }
    }
}
