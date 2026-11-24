using System.Collections.Generic;
using UnityEngine;

namespace StockGame
{
    /// <summary>
    /// 인터넷 브라우저 (가상 뉴스·커뮤니티). 내부 사건 결과는 화면에 나오지 않는다.
    /// 탭: 행동 site:home / site:news / site:community, 참거짓 site_is:&lt;사이트&gt;
    /// 글자: tab_news, tab_community, url, clock, news_name, news_tagline, comm_name, comm_tagline, hint
    /// 참거짓: only_watch / 행동 toggle_watch
    /// 목록 news: n_title, n_meta, n_unread, n_selected / 행동 n_open
    /// 기사: news_has_sel, nd_title, nd_meta, nd_body, nd_company, nd_has_stock / 행동 nd_chart
    /// 목록 posts: p_title, p_meta, p_stats, p_cat, p_unread, p_selected, 색 p_cat_color / 행동 p_open
    /// 글: post_has_sel, pd_title, pd_meta, pd_body, pd_comment_count, pd_has_stock, pd_chart_label / 행동 pd_chart
    /// 목록 comments: cm_meta, cm_text
    /// </summary>
    public class BrowserApp : UISource
    {
        string site = "home";
        string newsSel, postSel;
        bool onlyWatch;
        readonly List<FeedItem> news = new List<FeedItem>();
        readonly List<FeedItem> posts = new List<FeedItem>();
        readonly List<FeedComment> comments = new List<FeedComment>();

        GameState S { get { return Game.I.S; } }
        static NamesConfig N { get { return GameConfig.Instance != null ? GameConfig.Instance.names : new NamesConfig(); } }

        void Update()
        {
            news.Clear(); posts.Clear(); comments.Clear();
            if (S == null) return;
            Filter(Game.I.VisibleFeed("news"), news);
            Filter(Game.I.VisibleFeed("post"), posts);
            var p = Post();
            if (p != null)
            {
                foreach (var cm in p.Comments) if (cm.Tick <= S.CurrentTick) comments.Add(cm);
                comments.Sort((a, b) => a.Tick.CompareTo(b.Tick));
            }
        }

        void Filter(List<FeedItem> src, List<FeedItem> dst)
        {
            foreach (var f in src)
                if (!onlyWatch || (f.CompanyId != null && S.Watchlist.Contains(f.CompanyId))) dst.Add(f);
        }

        FeedItem Article() { var f = Game.I.FeedById(newsSel); return f != null && f.Tick <= S.CurrentTick ? f : null; }
        FeedItem Post() { var f = Game.I.FeedById(postSel); return f != null && f.Tick <= S.CurrentTick ? f : null; }
        static FeedItem At(List<FeedItem> l, Ctx c) { return c.Index >= 0 && c.Index < l.Count ? l[c.Index] : null; }

        string Meta(FeedItem f)
        {
            var g = Game.I;
            return f.Author + " · " + g.DayOfTick(f.Tick) + "일차 " + g.ClockOfTick(f.Tick) + (f.CompanyId != null ? " · " + Game.CompanyName(f.CompanyId) : "");
        }

        int Unread(string kind)
        {
            int n = 0;
            foreach (var f in Game.I.VisibleFeed(kind)) if (!f.Read) n++;
            return n;
        }

        public override string Text(string key, Ctx c)
        {
            var g = Game.I;
            if (S == null) return null;
            switch (key)
            {
                case "tab_news": { int u = Unread("news"); return N.sites.news.name + (u > 0 ? "  •" + u : ""); }
                case "tab_community": { int u = Unread("post"); return N.sites.community.name + (u > 0 ? "  •" + u : ""); }
                case "url": return site == "home" ? "about:home" : (site == "news" ? N.sites.news.url : N.sites.community.url);
                case "clock": return "게임 내 시각 " + S.Day + "일차 " + g.NowClock() + " · 시장 정지 중";
                case "news_name": return N.sites.news.name;
                case "news_tagline": return N.sites.news.tagline;
                case "comm_name": return N.sites.community.name;
                case "comm_tagline": return N.sites.community.tagline;
                case "hint": var cfg = GameConfig.Instance; return cfg != null ? cfg.dialogue.system.browserHint : "";
                case "n_title": { var f = At(news, c); return f != null ? f.Title : ""; }
                case "n_meta": { var f = At(news, c); return f != null ? Meta(f) : ""; }
                case "nd_title": { var f = Article(); return f != null ? f.Title : ""; }
                case "nd_meta": { var f = Article(); return f != null ? f.Author + " · " + g.DayOfTick(f.Tick) + "일차 " + g.ClockOfTick(f.Tick) + " · 조회 " + Fmt.N(f.Views) : ""; }
                case "nd_body": { var f = Article(); return f != null ? f.Body : ""; }
                case "nd_company":
                    {
                        var f = Article();
                        if (f == null || f.CompanyId == null) return "";
                        return "관련 기업: " + Game.CompanyName(f.CompanyId) + " (" + J.S(Game.Company(f.CompanyId), "business", "") + ")";
                    }
                case "p_title":
                    {
                        var f = At(posts, c);
                        if (f == null) return "";
                        int n = 0;
                        foreach (var cm in f.Comments) if (cm.Tick <= S.CurrentTick) n++;
                        return f.Title + (n > 0 ? "  [" + n + "]" : "");
                    }
                case "p_meta": { var f = At(posts, c); return f != null ? Meta(f) : ""; }
                case "p_stats": { var f = At(posts, c); return f != null ? "조회 " + Fmt.N(f.Views) + " · 추천 " + Fmt.N(f.Likes) : ""; }
                case "p_cat": { var f = At(posts, c); return f == null ? "" : (string.IsNullOrEmpty(f.Category) ? "일반" : f.Category); }
                case "pd_title": { var f = Post(); return f != null ? f.Title : ""; }
                case "pd_meta": { var f = Post(); return f != null ? f.Author + " · " + g.DayOfTick(f.Tick) + "일차 " + g.ClockOfTick(f.Tick) + " · 조회 " + Fmt.N(f.Views) + " · 추천 " + Fmt.N(f.Likes) : ""; }
                case "pd_body": { var f = Post(); return f != null ? f.Body : ""; }
                case "pd_comment_count": return "댓글 " + comments.Count;
                case "pd_chart_label": { var f = Post(); return f != null && f.CompanyId != null ? Game.CompanyName(f.CompanyId) + " 차트" : ""; }
                case "cm_meta": { var cm = c.Index < comments.Count ? comments[c.Index] : null; return cm != null ? cm.Nick + " · " + g.ClockOfTick(cm.Tick) + " · 추천 " + cm.Likes : ""; }
                case "cm_text": { var cm = c.Index < comments.Count ? comments[c.Index] : null; return cm != null ? cm.Text : ""; }
            }
            return null;
        }

        public override bool Flag(string key, Ctx c)
        {
            if (S == null) return false;
            if (key.StartsWith("site_is:")) return site == key.Substring(8);
            switch (key)
            {
                case "only_watch": return onlyWatch;
                case "n_unread": { var f = At(news, c); return f != null && !f.Read; }
                case "n_selected": { var f = At(news, c); return f != null && f.Id == newsSel; }
                case "news_has_sel": return Article() != null;
                case "news_no_sel": return Article() == null;
                case "nd_has_stock": { var f = Article(); return f != null && f.CompanyId != null && S.Stocks.ContainsKey(f.CompanyId); }
                case "p_unread": { var f = At(posts, c); return f != null && !f.Read; }
                case "p_selected": { var f = At(posts, c); return f != null && f.Id == postSel; }
                case "post_has_sel": return Post() != null;
                case "post_no_sel": return Post() == null;
                case "pd_has_stock": { var f = Post(); return f != null && f.CompanyId != null && S.Stocks.ContainsKey(f.CompanyId); }
                case "news_empty": return news.Count == 0;
                case "posts_empty": return posts.Count == 0;
            }
            return false;
        }

        public override string ColorKey(string key, Ctx c)
        {
            switch (key)
            {
                case "n_title_color": { var f = At(news, c); return f != null && f.Read ? "text_dim" : "text"; }
                case "p_title_color": { var f = At(posts, c); return f != null && f.Read ? "text_dim" : "text"; }
                case "p_cat_color": { var f = At(posts, c); return f != null && f.CompanyId != null ? "pink" : "mint"; }
            }
            return null;
        }

        public override int Count(string list)
        {
            switch (list)
            {
                case "news": return news.Count;
                case "posts": return posts.Count;
                case "comments": return comments.Count;
            }
            return 0;
        }

        public override void Act(string action, Ctx c)
        {
            var g = Game.I;
            if (S == null) return;
            if (action.StartsWith("site:")) { site = action.Substring(5); return; }
            switch (action)
            {
                case "toggle_watch": onlyWatch = !onlyWatch; break;
                case "n_open": { var f = At(news, c); if (f != null) { newsSel = f.Id; g.MarkRead(f); } break; }
                case "p_open": { var f = At(posts, c); if (f != null) { postSel = f.Id; g.MarkRead(f); } break; }
                case "nd_chart": { var f = Article(); if (f != null) OpenChart(f.CompanyId); break; }
                case "pd_chart": { var f = Post(); if (f != null) OpenChart(f.CompanyId); break; }
            }
        }

        static void OpenChart(string company)
        {
            if (company == null) return;
            var desk = GameRoot.I.desktop;
            if (desk.stock != null) desk.stock.Select(company); else { ChartState.Symbol = company; ChartState.ScrollBack = 0; }
            desk.Open("stock");
        }
    }
}
