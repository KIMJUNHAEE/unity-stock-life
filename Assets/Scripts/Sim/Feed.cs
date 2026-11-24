using System;
using System.Collections.Generic;

namespace StockGame
{
    /// <summary>
    /// 뉴스·게시글·댓글 공개. 사전 작성된 data/*.json 에서
    /// 종목·사건 확인 → 공개 단계 확인 → 맥락 후보 → 최근 중복 제외 → 가중 선택 → 이력 저장 순으로 고른다.
    /// 후보가 없으면 억지로 채우지 않고 개수를 줄인다.
    /// </summary>
    public partial class Game
    {
        void ProcessPending(int tick)
        {
            int guard = 0;
            while (S.Pending.Count > 0 && S.Pending[0].Tick <= tick && guard++ < 500)
            {
                var p = S.Pending[0];
                S.Pending.RemoveAt(0);
                try { ProcessPub(p, tick); }
                catch (Exception ex) { UnityEngine.Debug.LogWarning("[StockGame] 피드 처리 오류: " + ex.Message); }
            }
            SyncRngCounters();
        }

        void ProcessPub(PendingPub p, int tick)
        {
            var e = EventById(p.EventId);
            switch (p.Type)
            {
                case "rumor_post":
                    if (e != null) PublishRumorPost(e, tick);
                    break;
                case "official_news":
                    if (e != null) PublishNews(e, "official", tick);
                    break;
                case "follow_news":
                    if (e != null) PublishNews(e, "follow_up", tick);
                    break;
                case "fact_comments":
                    if (e != null) PublishFactComments(e, tick);
                    break;
                case "general_post":
                    PublishGeneralPost(tick);
                    break;
                case "general_comment":
                    var parent = FeedById(p.ParentId);
                    if (parent != null) AddGeneralComment(parent, tick);
                    break;
            }
        }

        public FeedItem FeedById(string id)
        {
            if (id == null) return null;
            for (int i = S.Feed.Count - 1; i >= 0; i--) if (S.Feed[i].Id == id) return S.Feed[i];
            return null;
        }

        bool OnCooldown(Dictionary<string, object> t)
        {
            string id = J.S(t, "id", "");
            int last;
            if (!S.TemplateLastUsed.TryGetValue(id, out last)) return false;
            return S.Day - last < J.I(t, "cooldown_sessions", 0);
        }

        Dictionary<string, object> PickTemplate(List<Dictionary<string, object>> candidates)
        {
            if (candidates.Count == 0) return null;
            var weights = new List<double>();
            foreach (var c in candidates) weights.Add(J.D(c, "selection_weight", 1));
            int idx = contentRng.PickWeighted(weights);
            if (idx < 0) return null;
            var chosen = candidates[idx];
            S.TemplateLastUsed[J.S(chosen, "id", "")] = S.Day;
            return chosen;
        }

        static bool ListContains(object root, string key, string value)
        {
            foreach (var o in J.A(root, key)) if (J.ToS(o, "") == value) return true;
            return false;
        }

        public static string Substitute(string text, string companyId)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var c = Company(companyId);
            return text
                .Replace("{company_name}", J.S(c, "company_name", "어느 회사"))
                .Replace("{stock_name}", J.S(c, "stock_name", "어느 종목"))
                .Replace("{product_name}", J.S(c, "product_name", "신제품"));
        }

        string RandomNick()
        {
            var names = Cfg.A("names", "nicknames");
            if (names.Count == 0) return "익명";
            return J.ToS(contentRng.Pick(names), "익명");
        }

        string RandomOutlet()
        {
            var names = Cfg.A("names", "news_outlets");
            if (names.Count == 0) return "가상경제";
            return J.ToS(contentRng.Pick(names), "가상경제");
        }

        int RandomViews(string key)
        {
            double a, b;
            Cfg.Range("balance", "feed." + key, 30, 900, out a, out b);
            return (int)Math.Round(Math.Exp(contentRng.Range(Math.Log(Math.Max(1, a)), Math.Log(Math.Max(1, b)))));
        }

        FeedItem NewItem(string kind, Dictionary<string, object> t, string companyId, int tick)
        {
            var it = new FeedItem
            {
                Id = "F" + (++S.FeedSerial).ToString("00000"),
                Kind = kind,
                TemplateId = J.S(t, "id", ""),
                Title = Substitute(J.S(t, "title", ""), companyId),
                Body = Substitute(J.S(t, "body", ""), companyId),
                CompanyId = companyId,
                Tick = tick,
                Category = J.S(t, "category", null)
            };
            it.Views = RandomViews(kind == "news" ? "news_views" : "post_views");
            it.Likes = (int)Math.Round(it.Views * contentRng.Range(0.01, 0.12));
            S.Feed.Add(it);
            return it;
        }

        void ScheduleCommentsFor(FeedItem post, int tick)
        {
            int n = contentRng.RangeInt(Cfg.I("balance", "feed.general_comments_per_post_min", 1), Cfg.I("balance", "feed.general_comments_per_post_max", 3));
            for (int i = 0; i < n; i++)
                AddPending(new PendingPub { Tick = tick + contentRng.RangeInt(0, 40), Type = "general_comment", ParentId = post.Id });
        }

        void PublishRumorPost(EventInstance e, int tick)
        {
            var cands = new List<Dictionary<string, object>>();
            foreach (var o in Cfg.Items("posts"))
            {
                var t = o as Dictionary<string, object>;
                if (t == null) continue;
                if (J.S(t, "kind", "") != "rumor_or_discussion") continue;
                if (J.S(t, "topic", "") != e.Topic) continue;
                if (!ListContains(t, "allowed_phases", "rumor")) continue;
                bool usedHere = false;
                foreach (var pid in e.RumorPostIds) { var f = FeedById(pid); if (f != null && f.TemplateId == J.S(t, "id", "")) usedHere = true; }
                if (usedHere || OnCooldown(t)) continue;
                cands.Add(t);
            }
            var chosen = PickTemplate(cands);
            if (chosen == null) return;
            var it = NewItem("post", chosen, e.CompanyId, tick);
            it.EventId = e.Id;
            it.Phase = "rumor";
            it.Author = RandomNick();
            it.Category = "종목";
            e.RumorPostIds.Add(it.Id);
            ScheduleCommentsFor(it, tick);
        }

        void PublishNews(EventInstance e, string phase, int tick)
        {
            var cands = new List<Dictionary<string, object>>();
            foreach (var o in Cfg.Items("news"))
            {
                var t = o as Dictionary<string, object>;
                if (t == null) continue;
                if (J.S(t, "fact_tag", "") != e.OutcomeId) continue;
                if (J.S(t, "publish_phase", "") != phase) continue;
                if (OnCooldown(t)) continue;
                cands.Add(t);
            }
            if (phase == "official") e.OfficialPublished = true; else e.FollowPublished = true;
            var chosen = PickTemplate(cands);
            if (chosen == null) return;
            var it = NewItem("news", chosen, e.CompanyId, tick);
            it.EventId = e.Id;
            it.Phase = phase;
            it.Author = RandomOutlet();
            e.NewsIds.Add(it.Id);
        }

        void PublishFactComments(EventInstance e, int tick)
        {
            string phase = e.FollowPublished ? "follow_up" : "official";
            foreach (var pid in e.RumorPostIds)
            {
                var post = FeedById(pid);
                if (post == null) continue;
                var cands = new List<Dictionary<string, object>>();
                foreach (var o in Cfg.Items("comments"))
                {
                    var t = o as Dictionary<string, object>;
                    if (t == null) continue;
                    if (J.S(t, "required_public_fact", null) != e.OutcomeId) continue;
                    if (!ListContains(t, "allowed_phases", phase) && !ListContains(t, "allowed_phases", "any")) continue;
                    if (!ListContains(t, "allowed_parent_ids", post.TemplateId)) continue;
                    if (HasComment(post, J.S(t, "id", ""))) continue;
                    if (OnCooldown(t)) continue;
                    cands.Add(t);
                }
                var chosen = PickTemplate(cands);
                if (chosen == null) continue;
                AddComment(post, chosen, tick);
            }
        }

        void PublishGeneralPost(int tick)
        {
            var cands = new List<Dictionary<string, object>>();
            foreach (var o in Cfg.Items("posts"))
            {
                var t = o as Dictionary<string, object>;
                if (t == null) continue;
                if (J.S(t, "kind", "") != "daily_or_general") continue;
                if (OnCooldown(t)) continue;
                cands.Add(t);
            }
            var chosen = PickTemplate(cands);
            if (chosen == null) return;
            var it = NewItem("post", chosen, null, tick);
            it.Phase = "any";
            it.Author = RandomNick();
            if (string.IsNullOrEmpty(it.Category)) it.Category = "일반";
            ScheduleCommentsFor(it, tick);
        }

        void AddGeneralComment(FeedItem post, int tick)
        {
            var cands = new List<Dictionary<string, object>>();
            foreach (var o in Cfg.Items("comments"))
            {
                var t = o as Dictionary<string, object>;
                if (t == null) continue;
                if (J.S(t, "required_public_fact", null) != null) continue;
                if (!ListContains(t, "allowed_parent_ids", post.TemplateId)) continue;
                if (HasComment(post, J.S(t, "id", ""))) continue;
                if (OnCooldown(t)) continue;
                cands.Add(t);
            }
            var chosen = PickTemplate(cands);
            if (chosen == null) return;
            AddComment(post, chosen, tick);
        }

        static bool HasComment(FeedItem post, string templateId)
        {
            foreach (var c in post.Comments) if (c.TemplateId == templateId) return true;
            return false;
        }

        void AddComment(FeedItem post, Dictionary<string, object> t, int tick)
        {
            post.Comments.Add(new FeedComment
            {
                TemplateId = J.S(t, "id", ""),
                Text = Substitute(J.S(t, "body", ""), post.CompanyId),
                Nick = RandomNick(),
                Tick = tick,
                Likes = contentRng.RangeInt(0, 30)
            });
        }

        void ScheduleGeneralPosts(int startTick, int endTick)
        {
            int n = contentRng.RangeInt(Cfg.I("balance", "feed.general_posts_per_day_min", 4), Cfg.I("balance", "feed.general_posts_per_day_max", 7));
            for (int i = 0; i < n; i++)
                AddPending(new PendingPub { Tick = contentRng.RangeInt(startTick, Math.Max(startTick, endTick - 1)), Type = "general_post" });
            SyncRngCounters();
        }

        /// <summary>글을 열면 읽음 처리. 컷신 판정용으로 사건별 첫 열람 시각을 남긴다.</summary>
        public void MarkRead(FeedItem it)
        {
            if (it == null || it.Read) return;
            it.Read = true;
            if (it.Kind == "news")
            {
                S.Today.ArticlesRead++;
                if (it.EventId != null && !S.ReadNewsTick.ContainsKey(it.EventId)) S.ReadNewsTick[it.EventId] = S.CurrentTick;
            }
            else
            {
                S.Today.PostsRead++;
                if (it.EventId != null && it.Phase == "rumor" && !S.ReadRumorTick.ContainsKey(it.EventId)) S.ReadRumorTick[it.EventId] = S.CurrentTick;
            }
        }

        /// <summary>현재 시각까지 공개된 글 (최신순).</summary>
        public List<FeedItem> VisibleFeed(string kind)
        {
            var res = new List<FeedItem>();
            for (int i = S.Feed.Count - 1; i >= 0; i--)
            {
                var f = S.Feed[i];
                if (f.Kind == kind && f.Tick <= S.CurrentTick) res.Add(f);
            }
            res.Sort((a, b) => b.Tick != a.Tick ? b.Tick.CompareTo(a.Tick) : string.CompareOrdinal(b.Id, a.Id));
            return res;
        }
    }
}
