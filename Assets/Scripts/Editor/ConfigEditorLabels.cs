using System.Collections.Generic;

namespace StockGame.EditorTools
{
    /// <summary>설정 편집기에 보여줄 한국어 이름과 설명. 키 경로(파일 기준)로 찾는다. 없으면 원래 키를 보여준다.</summary>
    public static class ConfigEditorLabels
    {
        public class Info
        {
            public string Label;
            public string Help;
            public Info(string label, string help) { Label = label; Help = help; }
        }

        public static readonly Dictionary<string, string> FileNames = new Dictionary<string, string>
        {
            { "balance", "경제·시장·대출·컨디션" },
            { "job", "편의점 알바" },
            { "cutscenes", "밤 컷신" },
            { "dialogue", "서하 대사·튜토리얼" },
            { "shop", "가구 카탈로그" },
            { "theme", "색·글자·화면 배치" },
            { "assets", "이미지 연결" },
            { "names", "사이트·닉네임" },
            { "companies", "회사 목록 (data)" },
            { "events", "사건 결과 유형 (data)" },
            { "news", "뉴스 문구 (data)" },
            { "posts", "커뮤니티 글 (data)" },
            { "comments", "커뮤니티 댓글 (data)" },
            { "story", "스토리 구간 (data)" },
        };

        // 키: "파일:경로". 배열 원소는 경로에서 생략한다 (예: shop:items.price).
        static readonly Dictionary<string, Info> map = new Dictionary<string, Info>
        {
            { "balance:run", new Info("회차", null) },
            { "balance:run.fixed_seed", new Info("고정 시드", "0이면 매번 다른 시장. 숫자를 넣으면 새 게임마다 같은 시장이 나온다 (테스트용).") },
            { "balance:economy", new Info("경제", null) },
            { "balance:economy.starting_cash", new Info("시작 현금", "새 게임부터 적용") },
            { "balance:debt", new Info("시작 부채", null) },
            { "balance:debt.starting_debt", new Info("시작 부채 금액", "부모님이 남긴 빚. 새 게임부터 적용") },
            { "balance:debt.repay_options", new Info("상환 버튼 금액들", null) },
            { "balance:trading", new Info("매매", null) },
            { "balance:trading.fee_rate", new Info("수수료율", "0.001 = 0.1%") },
            { "balance:trading.min_fee", new Info("최소 수수료", null) },
            { "balance:trading.sell_tax_rate", new Info("매도 세금률", null) },
            { "balance:market", new Info("시장", null) },
            { "balance:market.tradable_company_ids", new Info("거래 가능 종목", "data/companies.json 의 id. 새 게임부터 적용") },
            { "balance:market.tradable_company_count", new Info("종목 수 (목록이 비었을 때)", null) },
            { "balance:market.session_start_minute", new Info("장 시작 시각 (분)", "1260 = 21:00. 새 게임부터") },
            { "balance:market.session_length_ticks", new Info("하루 장 길이 (분)", "캔들 개수. 새 게임부터") },
            { "balance:market.substeps_per_tick", new Info("캔들 하나를 몇 번 나눠 움직일지", "새 게임부터") },
            { "balance:market.seconds_per_tick", new Info("시장 1분 = 실제 몇 초", "작을수록 차트가 빨리 흐른다") },
            { "balance:market.speed_options", new Info("배속 버튼", null) },
            { "balance:market.initial_price_range", new Info("시작가 범위", "회사별 시작가가 없을 때") },
            { "balance:market.company_overrides", new Info("종목별 설정", "initial_price 시작가, volatility_mult 변동성 배율") },
            { "balance:market.volatility_run_spread", new Info("회차별 변동성 차이", null) },
            { "balance:market.base_volatility_per_tick", new Info("가격 출렁임 (1분)", "클수록 차트가 많이 흔들린다") },
            { "balance:market.market_factor_vol_per_tick", new Info("시장 전체 흐름 세기", null) },
            { "balance:market.mean_reversion_per_tick", new Info("제자리로 돌아오는 힘", "사건이 아닌 출렁임이 원래 가격으로 돌아오는 정도") },
            { "balance:market.drift_per_tick", new Info("기본 상승/하락 경향", "0 = 없음") },
            { "balance:market.attention_vol_boost", new Info("관심 집중 시 출렁임 증가", null) },
            { "balance:market.base_volume_per_tick", new Info("기본 거래량", null) },
            { "balance:market.volume_return_sensitivity", new Info("가격 변동 → 거래량 증가", null) },
            { "balance:market.min_price", new Info("최저 가격", null) },
            { "balance:market.tick_sizes", new Info("호가 단위표", "[이 가격 미만, 단위]") },
            { "balance:events", new Info("사건 배정", "다음에 배정되는 사건부터 적용") },
            { "balance:events.per_day_min", new Info("하루 사건 수 (최소)", null) },
            { "balance:events.per_day_max", new Info("하루 사건 수 (최대)", null) },
            { "balance:events.official_offset_ticks", new Info("공식 발표 시점 범위 (장 시작 후 분)", null) },
            { "balance:events.rumor_chance", new Info("소문이 먼저 돌 확률", "0~1") },
            { "balance:events.rumor_lead_ticks", new Info("소문 → 발표 간격 (분)", null) },
            { "balance:events.leak_window_ticks", new Info("소문 없을 때 미리 반영되는 구간 (분)", null) },
            { "balance:events.follow_up_delay_ticks", new Info("발표 → 후속 기사 간격 (분)", null) },
            { "balance:events.priced_in_with_rumor", new Info("소문 있을 때 선반영 비율", "0~1. 높을수록 뉴스 뜰 때 이미 올라 있음") },
            { "balance:events.priced_in_without_rumor", new Info("소문 없을 때 선반영 비율", null) },
            { "balance:events.rumor_hype_chance", new Info("소문 기대감이 붙을 확률", null) },
            { "balance:events.rumor_hype", new Info("소문 기대감 크기", "발표 때 되돌려진다 (거짓 소문 급등)") },
            { "balance:events.sell_the_news_ratio", new Info("호재 발표 후 차익 실현 되돌림", null) },
            { "balance:events.official_jump_share", new Info("발표 순간 한 번에 반영되는 비율", null) },
            { "balance:events.attention_per_impact", new Info("사건 크기 → 거래량 관심도", null) },
            { "balance:events.min_gap_between_company_events", new Info("같은 회사 사건 최소 간격 (분)", null) },
            { "balance:event_outcomes", new Info("사건 결과별 확률·영향", "weight 배정 가중치(0=안 나옴), impact 최종 가격 영향 [최소~최대] (0.1 ≈ +10%), tone 성격") },
            { "balance:event_outcomes.weight", new Info("배정 가중치", "0이면 이 결과는 나오지 않음") },
            { "balance:event_outcomes.impact", new Info("가격 영향 범위", "로그 수익. 0.1 ≈ +10.5%, -0.1 ≈ -9.5%") },
            { "balance:event_outcomes.tone", new Info("성격", "positive / neutral / negative (컷신 판정용)") },
            { "balance:feed", new Info("뉴스·커뮤니티 글 수", null) },
            { "balance:feed.rumor_posts_per_event_min", new Info("사건당 소문 글 (최소)", null) },
            { "balance:feed.rumor_posts_per_event_max", new Info("사건당 소문 글 (최대)", null) },
            { "balance:feed.general_posts_per_day_min", new Info("하루 일반 글 (최소)", null) },
            { "balance:feed.general_posts_per_day_max", new Info("하루 일반 글 (최대)", null) },
            { "balance:feed.general_comments_per_post_min", new Info("글당 일반 댓글 (최소)", null) },
            { "balance:feed.general_comments_per_post_max", new Info("글당 일반 댓글 (최대)", null) },
            { "balance:feed.post_views", new Info("게시글 조회수 범위", null) },
            { "balance:feed.news_views", new Info("기사 조회수 범위", null) },
            { "balance:loan", new Info("추가 투자 대출", null) },
            { "balance:loan.enabled", new Info("대출 사용", null) },
            { "balance:loan.limit", new Info("대출 한도", null) },
            { "balance:loan.amount_options", new Info("빌릴 금액 버튼", null) },
            { "balance:loan.repay_options", new Info("상환 버튼 금액들", null) },
            { "balance:loan.daily_interest_rate", new Info("하루 이자율", "0.01 = 1%") },
            { "balance:loan.daily_interest_cap", new Info("하루 이자 상한", null) },
            { "balance:loan.max_accumulated_interest", new Info("누적 이자 상한", null) },
            { "balance:loan.term_days", new Info("만기 (일)", null) },
            { "balance:loan.only_before_trading_starts", new Info("거래 시작 전에만 대출", null) },
            { "balance:loan.cash_threshold", new Info("이 현금 미만일 때만 대출 가능", "0이면 제한 없음") },
            { "balance:loan.overdue_job_income_ratio", new Info("연체 시 알바 수입 상환 비율", null) },
            { "balance:condition", new Info("컨디션", null) },
            { "balance:condition.start_value", new Info("시작 컨디션", null) },
            { "balance:condition.max_value", new Info("최대 컨디션", null) },
            { "balance:condition.drain_per_tick", new Info("시장 1분당 소모", "180분 × 0.3 = 54") },
            { "balance:condition.sleep_restore", new Info("수면 후 컨디션", null) },
            { "balance:condition.first_blink_delay_ticks", new Info("첫 눈 감김까지 (분)", null) },
            { "balance:condition.blink_close_fraction", new Info("눈 감고 뜨는 속도", null) },
            { "balance:condition.stages", new Info("컨디션 단계", "min 이상이면 그 단계. vignette 가장자리 흐림, blink 눈 감김") },
            { "balance:condition.items", new Info("회복 수단", null) },
            { "balance:dev", new Info("개발 옵션", null) },
            { "balance:dev.hot_reload_poll_seconds", new Info("파일 변경 확인 간격 (초)", null) },
            { "balance:dev.debug_panel", new Info("F12 디버그 패널 허용", "출시 빌드에서는 끄기") },

            { "job:duration_seconds", new Info("근무 시간 (초)", null) },
            { "job:base_pay", new Info("기본급", null) },
            { "job:bonus_per_customer", new Info("손님당 보너스", null) },
            { "job:mistake_penalty", new Info("실수당 보너스 감소", null) },
            { "job:perfect_bonus", new Info("무실수 보너스", null) },
            { "job:perfect_min_customers", new Info("무실수 보너스 최소 손님 수", null) },
            { "job:condition_cost", new Info("알바 컨디션 소모", null) },
            { "job:customer_patience_seconds", new Info("손님 인내심 (초)", null) },
            { "job:next_customer_delay", new Info("다음 손님까지 (초)", null) },
            { "job:items_per_customer", new Info("손님당 상품 수", null) },
            { "job:cash_bills", new Info("손님이 내는 지폐", null) },
            { "job:items", new Info("상품", null) },
            { "job:payments", new Info("결제 방법", null) },
            { "job:customers", new Info("손님 종류", null) },
            { "job:lines", new Info("알바 대사", null) },
            { "job:howto", new Info("설명 문구", null) },

            { "cutscenes:settings", new Info("재생 설정", null) },
            { "cutscenes:settings.chars_per_second", new Info("글자 속도 (초당)", null) },
            { "cutscenes:default_scene", new Info("기본 취침 장면", "조건 맞는 장면이 없을 때") },
            { "cutscenes:scenes", new Info("컷신 목록", "조건을 만족하면 해금. 새 장면 → priority 높은 순으로 하나 재생") },
            { "cutscenes:scenes.priority", new Info("우선순위", "높을수록 대표 재생") },
            { "cutscenes:scenes.repeatable", new Info("수집 후 다시 대표로 나옴", null) },
            { "cutscenes:scenes.conditions", new Info("해금 조건", "all=모두, any=하나라도. stat 목록은 config/README.md") },
            { "cutscenes:scenes.lines", new Info("대사", "speaker 화자, text 문장, expression 표정") },

            { "dialogue:settings.bubble_seconds", new Info("말풍선 표시 시간", null) },
            { "dialogue:settings.min_interval_seconds", new Info("말풍선 최소 간격", null) },
            { "dialogue:reactions", new Info("상황별 반응 대사", "여러 줄이면 무작위로 하나") },
            { "dialogue:tutorial", new Info("튜토리얼", null) },
            { "dialogue:intro", new Info("새 게임 인트로", null) },
            { "dialogue:expressions", new Info("표정 → 초상화 이미지 키", null) },
            { "dialogue:standing", new Info("표정 → 입상 이미지 키", null) },

            { "shop:items", new Info("상품", "slot 위치, price 가격, image 이미지 키") },
            { "shop:items.price", new Info("가격", null) },
            { "shop:slots", new Info("가구 슬롯", null) },
            { "shop:starting_items", new Info("시작 가구", null) },

            { "theme:font_sizes", new Info("글자 크기", null) },
            { "theme:themes", new Info("테마와 색", null) },
            { "theme:layout", new Info("화면 배치", "1920×1080 기준 x, y, 너비, 높이") },
            { "theme:layout.room", new Info("방 가구 위치", null) },
            { "theme:fonts", new Info("폰트", null) },
            { "theme:chart", new Info("차트", null) },
            { "theme:apps", new Info("앱 이름·아이콘", null) },

            { "assets:images", new Info("이미지 목록", "PNG를 고르면 art 폴더 경로로 연결된다") },
        };

        static string Strip(string path)
        {
            // 배열 인덱스 [3] 제거
            var sb = new System.Text.StringBuilder();
            bool skip = false;
            foreach (char c in path)
            {
                if (c == '[') { skip = true; continue; }
                if (c == ']') { skip = false; continue; }
                if (!skip) sb.Append(c);
            }
            return sb.ToString().Replace("..", ".").Trim('.');
        }

        public static Info Find(string file, string path)
        {
            Info i;
            string p = Strip(path);
            if (map.TryGetValue(file + ":" + p, out i)) return i;
            // event_outcomes.<id>.weight 같은 동적 키 → event_outcomes.weight
            var parts = p.Split('.');
            if (parts.Length >= 3)
            {
                string alt = parts[0] + "." + parts[parts.Length - 1];
                if (map.TryGetValue(file + ":" + alt, out i)) return i;
            }
            return null;
        }
    }
}
