using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StockGame
{
    /// <summary>증권 앱 차트 상태 (StockApp 이 바꾼다).</summary>
    public static class ChartState
    {
        public static string Symbol;
        public static int Timeframe = 1;
        public static int Visible = -1;
        public static int ScrollBack;
    }

    /// <summary>결산 복기 차트 상태 (SettlementScreen 이 바꾼다).</summary>
    public static class ReviewChartState
    {
        public static string Symbol;
        public static int Day;
    }

    /// <summary>
    /// 캔들·거래량 차트. 크기·위치는 씬에서 RectTransform 으로, 색은 테마 색(up, down, grid...)으로 정한다.
    /// 편집 중에도 예시 캔들을 그려 배치를 확인할 수 있다.
    /// 마우스 휠 = 확대/축소, 드래그 = 과거 보기.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class CandleChart : MaskableGraphic, IScrollHandler, IBeginDragHandler, IDragHandler, IPointerMoveHandler, IPointerExitHandler
    {
        [Tooltip("켜면 결산 복기용: 그날 진행한 캔들 전체와 내 매매·공개 글 위치를 보여 준다 (확대·드래그 없음)")]
        public bool reviewMode;
        [Header("배치")]
        [Tooltip("오른쪽 가격 눈금 폭")] public float axisWidth = 92;
        [Tooltip("아래 시각 눈금 높이")] public float timeHeight = 28;
        [Range(0.4f, 0.95f), Tooltip("가격 영역 비율 (나머지는 거래량)")] public float priceRatio = 0.76f;
        [Range(0.2f, 1f)] public float bodyWidth = 0.68f;
        [Header("글자")]
        [Tooltip("눈금 글자 원본 (복제해서 사용)")] public Text labelTemplate;
        [Tooltip("마우스 위치 봉 정보")] public Text hoverText;
        [Tooltip("기사 제목 툴팁")] public Text tooltipText;
        [Header("눈 감김 (컨디션)")]
        public RectTransform eyelidTop;
        public RectTransform eyelidBottom;
        public Text eyelidText;

        struct Quad { public Rect R; public Color C; }
        struct Label { public string T; public Vector2 P; public TextAnchor A; public Color C; public bool Tag; public Color TagColor; }

        readonly List<Quad> quads = new List<Quad>();
        readonly List<Label> labels = new List<Label>();
        readonly List<Text> pool = new List<Text>();
        readonly List<Image> tagPool = new List<Image>();
        bool hovering;
        Vector2 hoverLocal;
        float dragAccum;

        Color Pal(string key, Color def) { Color c; return Palette.TryGet(key, out c) ? c : def; }

        // ───────────────────────── 입력 ─────────────────────────

        public void OnScroll(PointerEventData e)
        {
            if (reviewMode) return;
            int min = Cfg.I("theme", "chart.min_visible_candles", 20), max = Cfg.I("theme", "chart.max_visible_candles", 300);
            if (ChartState.Visible < 0) ChartState.Visible = Cfg.I("theme", "chart.default_visible_candles", 60);
            ChartState.Visible = Mathf.Clamp((int)(ChartState.Visible * (e.scrollDelta.y < 0 ? 1.15f : 1 / 1.15f)), min, max);
        }

        public void OnBeginDrag(PointerEventData e) { dragAccum = 0; }

        public void OnDrag(PointerEventData e)
        {
            if (reviewMode) return;
            float scale = canvas != null ? canvas.scaleFactor : 1;
            dragAccum += e.delta.x / Mathf.Max(0.01f, scale);
            var plot = PlotRect();
            float cw = plot.width / Mathf.Max(1, ChartState.Visible);
            int steps = (int)(dragAccum / Mathf.Max(1, cw));
            if (steps != 0) { ChartState.ScrollBack = Mathf.Max(0, ChartState.ScrollBack + steps); dragAccum -= steps * cw; }
        }

        public void OnPointerMove(PointerEventData e)
        {
            hovering = RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.enterEventCamera, out hoverLocal);
        }

        public void OnPointerExit(PointerEventData e) { hovering = false; }

        // ───────────────────────── 계산 ─────────────────────────

        Rect PlotRect()
        {
            var r = GetPixelAdjustedRect();
            return new Rect(r.xMin, r.yMin + timeHeight, r.width - axisWidth, r.height - timeHeight);
        }

        List<Candle> PreviewCandles()
        {
            var list = new List<Candle>();
            double p = 1000;
            for (int i = 0; i < 70; i++)
            {
                double o = p;
                p = p * (1 + Math.Sin(i * 0.35) * 0.01 + Math.Cos(i * 1.7) * 0.006);
                list.Add(new Candle { Tick = i, O = o, C = p, H = Math.Max(o, p) * 1.004, L = Math.Min(o, p) * 0.996, V = 2000 + (long)(Math.Abs(Math.Sin(i)) * 6000) });
            }
            return list;
        }

        void LateUpdate()
        {
            Compute();
            LayoutLabels();
            UpdateEyelids();
            SetVerticesDirty();
        }

        void Add(float x, float y, float w, float h, Color c) { quads.Add(new Quad { R = new Rect(x, y, w, h), C = c }); }

        void Compute()
        {
            quads.Clear();
            labels.Clear();
            var g = Game.I;
            if (reviewMode) { ComputeReview(g); return; }
            bool live = Application.isPlaying && g != null && g.S != null && ChartState.Symbol != null && g.S.Stocks.ContainsKey(ChartState.Symbol);
            var cs = live ? g.ChartCandles(ChartState.Symbol, ChartState.Timeframe) : PreviewCandles();
            if (ChartState.Visible < 0) ChartState.Visible = live ? Cfg.I("theme", "chart.default_visible_candles", 60) : 60;

            var area = PlotRect();
            var pricePlot = new Rect(area.xMin, area.yMin + area.height * (1 - priceRatio), area.width, area.height * priceRatio);
            var volPlot = new Rect(area.xMin, area.yMin, area.width, area.height * (1 - priceRatio) - 8);
            Color grid = Pal("grid", new Color(0.89f, 0.92f, 0.95f)), dim = Pal("text_dim", Color.gray);
            Color up = Pal("up", new Color(0.92f, 0.42f, 0.49f)), down = Pal("down", new Color(0.3f, 0.56f, 0.88f));
            if (hoverText != null) hoverText.text = "";
            if (tooltipText != null) tooltipText.text = "";
            if (cs.Count == 0) return;

            int count = Mathf.Clamp(ChartState.Visible, 5, 1000);
            ChartState.ScrollBack = Mathf.Clamp(ChartState.ScrollBack, 0, Math.Max(0, cs.Count - 5));
            int end = cs.Count - 1 - ChartState.ScrollBack;
            int start = Math.Max(0, end - count + 1);
            int shown = end - start + 1;

            double lo = double.MaxValue, hi = double.MinValue;
            long vmax = 1;
            for (int i = start; i <= end; i++) { lo = Math.Min(lo, cs[i].L); hi = Math.Max(hi, cs[i].H); vmax = Math.Max(vmax, cs[i].V); }
            Holding h = live ? g.HoldingOf(ChartState.Symbol) : null;
            if (h != null) { lo = Math.Min(lo, h.AvgCost); hi = Math.Max(hi, h.AvgCost); }
            double price = live ? g.PriceOf(ChartState.Symbol) : cs[cs.Count - 1].C;
            double pad = Math.Max((hi - lo) * 0.08, live ? g.TickSize(price) * 2 : 2);
            lo -= pad; hi += pad;
            Func<double, float> Y = p => pricePlot.yMin + (float)((p - lo) / (hi - lo)) * pricePlot.height;

            for (int k = 0; k <= 4; k++)
            {
                double p = lo + (hi - lo) * k / 4.0;
                float y = Y(p);
                Add(pricePlot.xMin, y, pricePlot.width, 1, grid);
                labels.Add(new Label { T = Fmt.N(live ? g.Quantize(p) : p), P = new Vector2(area.xMax + 8, y), A = TextAnchor.MiddleLeft, C = dim });
            }

            float cw = pricePlot.width / count;
            float bw = Mathf.Max(1, cw * bodyWidth);
            float offset = (count - shown) * cw;
            int labelEvery = Math.Max(1, count / 6);
            int prevDay = -1;
            for (int i = start; i <= end; i++)
            {
                var cd = cs[i];
                float x = pricePlot.xMin + offset + (i - start) * cw;
                if (live)
                {
                    int day = g.DayOfTick(cd.Tick);
                    if (prevDay >= 0 && day != prevDay)
                    {
                        Add(x, area.yMin, 1, area.height, new Color(dim.r, dim.g, dim.b, 0.4f));
                        labels.Add(new Label { T = day + "일차", P = new Vector2(x + 4, pricePlot.yMax - 10), A = TextAnchor.MiddleLeft, C = dim });
                    }
                    prevDay = day;
                }
                Color cc = cd.C >= cd.O ? up : down;
                float yO = Y(cd.O), yC = Y(cd.C), yH = Y(cd.H), yL = Y(cd.L);
                Add(x + cw * 0.5f - 0.75f, yL, 1.5f, Mathf.Max(1, yH - yL), cc);
                Add(x + (cw - bw) * 0.5f, Mathf.Min(yO, yC), bw, Mathf.Max(1.5f, Mathf.Abs(yC - yO)), cc);
                float vh = (float)cd.V / vmax * volPlot.height;
                Add(x + (cw - bw) * 0.5f, volPlot.yMin, bw, vh, new Color(cc.r, cc.g, cc.b, 0.55f));
                if ((i - start) % labelEvery == 0)
                    labels.Add(new Label { T = live ? g.ClockOfTick(cd.Tick) : Fmt.Clock(1260 + cd.Tick), P = new Vector2(x + cw * 0.5f, area.yMin - timeHeight * 0.5f), A = TextAnchor.MiddleCenter, C = dim });
            }

            // 평균 매입가선, 현재가선
            if (h != null)
            {
                float ya = Y(h.AvgCost);
                Color ac = Pal("avg_line", new Color(0.55f, 0.48f, 0.79f));
                for (float px = 0; px < pricePlot.width; px += 10) Add(pricePlot.xMin + px, ya, Mathf.Min(6, pricePlot.width - px), 1.2f, ac);
                labels.Add(new Label { T = "평균 " + Fmt.N(h.AvgCost), P = new Vector2(pricePlot.xMin + 8, ya), A = TextAnchor.MiddleLeft, C = Pal("text", Color.black), Tag = true, TagColor = Pal("panel", Color.white) });
            }
            Color accent = Pal("accent_dark", new Color(0.31f, 0.53f, 0.76f));
            float yp = Y(price);
            for (float px = 0; px < pricePlot.width; px += 10) Add(pricePlot.xMin + px, yp, Mathf.Min(6, pricePlot.width - px), 1.2f, new Color(accent.r, accent.g, accent.b, 0.8f));
            labels.Add(new Label { T = Fmt.N(price), P = new Vector2(area.xMax + 6, yp), A = TextAnchor.MiddleLeft, C = Color.white, Tag = true, TagColor = accent });

            // 기사 게시 시점
            if (live && g.Settings.ShowNewsMarkers)
            {
                Color nm = Pal("news_marker", new Color(0.72f, 0.61f, 0.91f));
                int tickLo = cs[start].Tick, tickHi = cs[end].Tick + ChartState.Timeframe;
                foreach (var f in g.S.Feed)
                {
                    if (f.Kind != "news" || f.CompanyId != ChartState.Symbol || f.Tick > g.S.CurrentTick || f.Tick < tickLo || f.Tick >= tickHi) continue;
                    int idx = start;
                    for (int i = start; i <= end; i++) if (cs[i].Tick <= f.Tick) idx = i;
                    float x = pricePlot.xMin + offset + (idx - start) * cw + cw * 0.5f;
                    Add(x - 0.5f, pricePlot.yMin, 1, pricePlot.height - 30, new Color(nm.r, nm.g, nm.b, 0.35f));
                    labels.Add(new Label { T = "N", P = new Vector2(x, pricePlot.yMax - 16), A = TextAnchor.MiddleCenter, C = Color.white, Tag = true, TagColor = nm });
                    if (hovering && Mathf.Abs(hoverLocal.x - x) < 12 && hoverLocal.y > pricePlot.yMax - 30 && tooltipText != null) tooltipText.text = f.Title;
                }
            }

            if (hovering && hoverText != null)
            {
                int idx = start + (int)((hoverLocal.x - pricePlot.xMin - offset) / cw);
                if (idx >= start && idx <= end && hoverLocal.x < area.xMax)
                {
                    var cd = cs[idx];
                    string when = live ? g.DayOfTick(cd.Tick) + "일차 " + g.ClockOfTick(cd.Tick) : "";
                    hoverText.text = when + "  시 " + Fmt.N(cd.O) + "  고 " + Fmt.N(cd.H) + "  저 " + Fmt.N(cd.L) + "  종 " + Fmt.N(cd.C) + "  거래량 " + Fmt.N(cd.V);
                    float x = pricePlot.xMin + offset + (idx - start) * cw;
                    Add(x, area.yMin, cw, area.height, new Color(dim.r, dim.g, dim.b, 0.08f));
                }
            }
        }

        /// <summary>복기: 그날 캔들 전부 + 매수·매도 위치 + 그 종목의 공개 글 시각. 미래 글은 표시하지 않는다.</summary>
        void ComputeReview(Game g)
        {
            bool live = Application.isPlaying && g != null && g.S != null && ReviewChartState.Symbol != null && g.S.Stocks.ContainsKey(ReviewChartState.Symbol);
            string sym = ReviewChartState.Symbol;
            var cs = live ? g.ReviewCandles(sym, ReviewChartState.Day) : PreviewCandles();
            if (hoverText != null) hoverText.text = "";
            if (tooltipText != null) tooltipText.text = "";
            var area = PlotRect();
            var pricePlot = new Rect(area.xMin, area.yMin + area.height * (1 - priceRatio), area.width, area.height * priceRatio);
            var volPlot = new Rect(area.xMin, area.yMin, area.width, area.height * (1 - priceRatio) - 8);
            Color grid = Pal("grid", new Color(0.89f, 0.92f, 0.95f)), dim = Pal("text_dim", Color.gray);
            Color up = Pal("up", new Color(0.92f, 0.42f, 0.49f)), down = Pal("down", new Color(0.3f, 0.56f, 0.88f));
            if (cs.Count == 0)
            {
                labels.Add(new Label { T = "이날 진행한 시장 기록이 없어요.", P = area.center, A = TextAnchor.MiddleCenter, C = dim });
                return;
            }
            double lo = double.MaxValue, hi = double.MinValue;
            long vmax = 1;
            foreach (var c in cs) { lo = Math.Min(lo, c.L); hi = Math.Max(hi, c.H); vmax = Math.Max(vmax, c.V); }
            var trades = live ? g.ReviewTrades(sym, ReviewChartState.Day) : new List<TradeRecord>();
            foreach (var t in trades) { lo = Math.Min(lo, t.Price); hi = Math.Max(hi, t.Price); }
            double pad = Math.Max((hi - lo) * 0.1, 2);
            lo -= pad; hi += pad;
            Func<double, float> Y = p => pricePlot.yMin + (float)((p - lo) / (hi - lo)) * pricePlot.height;
            for (int k = 0; k <= 4; k++)
            {
                double p = lo + (hi - lo) * k / 4.0;
                float y = Y(p);
                Add(pricePlot.xMin, y, pricePlot.width, 1, grid);
                labels.Add(new Label { T = Fmt.N(live ? g.Quantize(p) : p), P = new Vector2(area.xMax + 8, y), A = TextAnchor.MiddleLeft, C = dim });
            }
            int count = Math.Max(cs.Count, 30);
            float cw = pricePlot.width / count;
            float bw = Mathf.Max(1, cw * bodyWidth);
            int labelEvery = Math.Max(1, count / 6);
            Func<int, float> XOfTick = tick =>
            {
                int idx = 0;
                for (int i = 0; i < cs.Count; i++) if (cs[i].Tick <= tick) idx = i;
                return pricePlot.xMin + idx * cw + cw * 0.5f;
            };
            for (int i = 0; i < cs.Count; i++)
            {
                var cd = cs[i];
                float x = pricePlot.xMin + i * cw;
                Color cc = cd.C >= cd.O ? up : down;
                float yO = Y(cd.O), yC = Y(cd.C), yH = Y(cd.H), yL = Y(cd.L);
                Add(x + cw * 0.5f - 0.6f, yL, 1.2f, Mathf.Max(1, yH - yL), cc);
                Add(x + (cw - bw) * 0.5f, Mathf.Min(yO, yC), bw, Mathf.Max(1.2f, Mathf.Abs(yC - yO)), cc);
                Add(x + (cw - bw) * 0.5f, volPlot.yMin, bw, (float)cd.V / vmax * volPlot.height, new Color(cc.r, cc.g, cc.b, 0.5f));
                if (i % labelEvery == 0)
                    labels.Add(new Label { T = live ? g.ClockOfTick(cd.Tick) : Fmt.Clock(1260 + cd.Tick), P = new Vector2(x + cw * 0.5f, area.yMin - timeHeight * 0.5f), A = TextAnchor.MiddleCenter, C = dim });
            }
            if (!live) return;

            // 공개 글 시각 (그 종목, 그날, 이미 공개된 것만)
            int start, end;
            g.DayTickRange(ReviewChartState.Day, out start, out end);
            Color nm = Pal("news_marker", new Color(0.72f, 0.61f, 0.91f));
            Color pm = Pal("warning", new Color(0.95f, 0.66f, 0.36f));
            foreach (var f in g.S.Feed)
            {
                if (f.CompanyId != sym || f.Tick < start || f.Tick >= end || f.Tick > g.S.CurrentTick) continue;
                bool news = f.Kind == "news";
                float x = XOfTick(f.Tick);
                Color mc = news ? nm : pm;
                Add(x - 0.5f, pricePlot.yMin, 1, pricePlot.height - 30, new Color(mc.r, mc.g, mc.b, 0.35f));
                labels.Add(new Label { T = news ? "N" : "C", P = new Vector2(x, pricePlot.yMax - 16), A = TextAnchor.MiddleCenter, C = Color.white, Tag = true, TagColor = mc });
                if (hovering && Mathf.Abs(hoverLocal.x - x) < 12 && hoverLocal.y > pricePlot.yMax - 30 && tooltipText != null)
                    tooltipText.text = (news ? "[뉴스] " : "[커뮤니티] ") + f.Title + (f.Read ? " · 읽음" : " · 읽지 않음");
            }
            // 내 매매 위치
            foreach (var t in trades)
            {
                bool buy = t.Side == "buy";
                float x = XOfTick(t.Tick), y = Y(t.Price);
                Color tc = buy ? up : down;
                Add(x - 4, y - 4, 8, 8, tc);
                labels.Add(new Label { T = (buy ? "매수 " : "매도 ") + Fmt.N(t.Qty), P = new Vector2(x, y + (buy ? -18 : 18)), A = TextAnchor.MiddleCenter, C = Color.white, Tag = true, TagColor = tc });
            }
            if (hovering && hoverText != null)
            {
                int idx = (int)((hoverLocal.x - pricePlot.xMin) / cw);
                if (idx >= 0 && idx < cs.Count && hoverLocal.x < area.xMax)
                {
                    var cd = cs[idx];
                    hoverText.text = g.ClockOfTick(cd.Tick) + "  시 " + Fmt.N(cd.O) + "  고 " + Fmt.N(cd.H) + "  저 " + Fmt.N(cd.L) + "  종 " + Fmt.N(cd.C) + "  거래량 " + Fmt.N(cd.V);
                    Add(pricePlot.xMin + idx * cw, area.yMin, cw, area.height, new Color(dim.r, dim.g, dim.b, 0.08f));
                }
            }
        }

        void LayoutLabels()
        {
            if (labelTemplate == null) return;
            labelTemplate.gameObject.SetActive(false);
            while (pool.Count < labels.Count)
            {
                var t = Instantiate(labelTemplate, transform, false);
                t.gameObject.name = "__chart_label";
                t.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInHierarchy;
                t.raycastTarget = false;
                var tag = new GameObject("__chart_tag", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                tag.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInHierarchy;
                tag.transform.SetParent(transform, false);
                tag.raycastTarget = false;
                tag.transform.SetSiblingIndex(0);
                pool.Add(t);
                tagPool.Add(tag);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                bool on = i < labels.Count;
                if (pool[i].gameObject.activeSelf != on) pool[i].gameObject.SetActive(on);
                if (!on) { if (tagPool[i].gameObject.activeSelf) tagPool[i].gameObject.SetActive(false); continue; }
                var l = labels[i];
                var t = pool[i];
                t.text = l.T;
                t.color = l.C;
                t.alignment = l.A;
                var rt = t.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float w = Mathf.Max(40, t.preferredWidth + 4), hgt = labelTemplate.rectTransform.rect.height;
                rt.sizeDelta = new Vector2(w, hgt);
                float px = l.A == TextAnchor.MiddleCenter ? 0.5f : (l.A == TextAnchor.MiddleRight ? 1 : 0);
                rt.pivot = new Vector2(px, 0.5f);
                rt.anchoredPosition = l.P - rectTransform.rect.center;
                var tag = tagPool[i];
                if (tag.gameObject.activeSelf != l.Tag) tag.gameObject.SetActive(l.Tag);
                if (l.Tag)
                {
                    tag.color = l.TagColor;
                    var trt = tag.rectTransform;
                    trt.anchorMin = trt.anchorMax = rt.anchorMin;
                    trt.pivot = rt.pivot;
                    trt.sizeDelta = new Vector2(w + 8, hgt + 2);
                    trt.anchoredPosition = rt.anchoredPosition + new Vector2(px == 0 ? -4 : (px == 1 ? 4 : 0), 0);
                }
            }
        }

        void UpdateEyelids()
        {
            if (eyelidTop == null || eyelidBottom == null) return;
            float lid = 0;
            var root = GameRoot.I;
            if (Application.isPlaying && root != null && root.BlinkTimer > 0 && root.BlinkDuration > 0)
            {
                float t = 1 - root.BlinkTimer / root.BlinkDuration;
                float edge = Cfg.F("balance", "condition.blink_close_fraction", 0.18f);
                lid = Mathf.Clamp01(Mathf.Min(t / edge, (1 - t) / edge));
            }
            float half = rectTransform.rect.height * 0.5f * lid;
            eyelidTop.SetAsLastSibling();
            eyelidBottom.SetAsLastSibling();
            if (eyelidText != null) eyelidText.transform.SetAsLastSibling();
            eyelidTop.sizeDelta = new Vector2(eyelidTop.sizeDelta.x, half);
            eyelidBottom.sizeDelta = new Vector2(eyelidBottom.sizeDelta.x, half);
            if (eyelidText != null) eyelidText.enabled = lid > 0.6f;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var q in quads)
            {
                int i = vh.currentVertCount;
                var c = q.C * color;
                vh.AddVert(new Vector3(q.R.xMin, q.R.yMin), c, Vector2.zero);
                vh.AddVert(new Vector3(q.R.xMin, q.R.yMax), c, Vector2.zero);
                vh.AddVert(new Vector3(q.R.xMax, q.R.yMax), c, Vector2.zero);
                vh.AddVert(new Vector3(q.R.xMax, q.R.yMin), c, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i + 2, i + 3, i);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // 이전 실행에서 남은 눈금 글자 정리
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var ch = transform.GetChild(i).gameObject;
                if (!ch.name.StartsWith("__chart_")) continue;
                if (Application.isPlaying) Destroy(ch); else DestroyImmediate(ch);
            }
            pool.Clear();
            tagPool.Clear();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!Application.isPlaying)
            {
                foreach (var t in pool) if (t != null) DestroyImmediate(t.gameObject);
                foreach (var t in tagPool) if (t != null) DestroyImmediate(t.gameObject);
                pool.Clear();
                tagPool.Clear();
            }
        }
    }
}
