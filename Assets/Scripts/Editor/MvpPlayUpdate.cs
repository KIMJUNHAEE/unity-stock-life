using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StockGame.EditorTools
{
    /// <summary>
    /// 기획서 0.14 의 다음 적용 항목을 지금 씬과 설정 에셋에 더한다 (배치는 유지, 이미 있으면 건너뜀).
    /// 첫날 할 일 · 첫 결산 반응 · 첫 일상 장면 · 아침 만남 · 결산 복기 · 연장 거래 설정.
    /// </summary>
    public static class MvpPlayUpdate
    {
        public const string DailySceneId = "CS_daily_first_night";
        public const string MeetingSceneId = "MT_manager_first_visit";
        public const string MeetingId = "manager_first_visit";

        [MenuItem("StockGame/화면 수정/진행 기능 추가 (첫날 안내·복기·만남·연장)", false, 91)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("플레이를 멈춘 뒤 실행하세요.");
            var ui = GameObject.Find("UI");
            if (ui == null) throw new System.InvalidOperationException("Assets/Scenes/Main.unity 를 열고 실행하세요.");

            var cfg = ConfigImporter.EnsureAsset();
            Undo.RecordObject(cfg, "Progress content");
            var log = new List<string>();
            if (EnsureContent(cfg, log)) { EditorUtility.SetDirty(cfg); AssetDatabase.SaveAssetIfDirty(cfg); }

            var t = ui.transform;
            UIBuilder.PrepareForUpdate();
            if (UIBuilder.AddHomeProgress(t.Find("Screen_Home"))) log.Add("집: 만남 버튼·휴식 문구·오늘 할 일");
            if (UIBuilder.AddDesktopGuide(t.Find("Screen_Desktop"))) log.Add("바탕화면: 오늘 할 일");
            if (UIBuilder.AddSettlementReview(t.Find("Screen_Settlement"))) log.Add("결산: 복기 버튼·복기 화면");
            if (UIBuilder.StyleJobCounter(t.Find("Screen_Job"))) log.Add("편의점: 카운터 판 정리");

            GameConfig.NotifyChanged();
            EditorSceneManager.MarkSceneDirty(ui.scene);
            EditorSceneManager.SaveScene(ui.scene);
            Debug.Log("[StockGame] 진행 기능 추가 완료" + (log.Count > 0 ? ": " + string.Join(" / ", log.ToArray()) : " (이미 모두 있음)"));
        }

        static LineCfg L(string speaker, string text, string expression = null) { return new LineCfg { speaker = speaker, text = text, expression = expression }; }

        static StatCond C(string stat, CompareOp op, double value) { return new StatCond { stat = stat, op = op, value = value }; }

        /// <summary>설정 에셋에 새 반응·장면·만남이 없으면 넣는다. 사용자가 고친 기존 항목은 건드리지 않는다.</summary>
        public static bool EnsureContent(GameConfig cfg, List<string> log)
        {
            bool changed = false;
            var d = cfg.dialogue;
            changed |= Reaction(d, "first_settle_no_trade", log,
                L("서하", "오늘은 지켜보기만 했어. 그것도 내가 고른 거야.", "normal"),
                L("서하", "사지 않은 날도 기록은 남아. 내일은 조금 더 보일 거야.", "focus"));
            changed |= Reaction(d, "first_settle_loss", log,
                L("서하", "첫날부터 줄었네… 그래도 내일 아침 근무로 다시 시작할 수 있어.", "sad"),
                L("서하", "아프지만, 어디서 샀는지는 기억해 두자. 내일 또 출근하고.", "sad"));
            changed |= Reaction(d, "first_settle_profit", log,
                L("서하", "벌긴 했는데… 운이었을지도 몰라. 왜 샀는지 적어 두자.", "happy"),
                L("서하", "첫날 플러스. 들뜨지 말고, 수수료까지 다시 보기.", "happy"));
            changed |= Reaction(d, "first_settle_flat", log,
                L("서하", "본전이네. 수수료가 생각보다 크구나.", "focus"));

            var cs = cfg.cutscenes;
            Sprite nightBg = null;
            foreach (var s in cs.scenes) if (s != null && s.useCharacterLayer && s.image != null) { nightBg = s.image; break; }
            if (nightBg == null) nightBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/bg_story_room_night_v001.png");
            var storeBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/bg_store_morning_v001.png");

            if (cfg.Scene(DailySceneId) == null)
            {
                var sc = new SceneCfg
                {
                    id = DailySceneId, title = "첫날의 끝", category = "일상", priority = 40, repeatable = false,
                    image = nightBg, useCharacterLayer = true,
                    hint = "처음으로 하루를 마친 밤", conditionText = "첫 결산을 마친 밤 (투자 결과와 상관없음)",
                    lines = new List<LineCfg>
                    {
                        L("서하", "첫날이 끝났다. 알바하고, 차트 보고… 생각보다 하루가 길었어.", "tired"),
                        L("", "책상 위에는 오늘 받은 영수증과, 아직 그대로인 10억이라는 숫자."),
                        L("서하", "숫자는 그대로인데, 오늘 한 일은 하나씩 남아 있네.", "normal"),
                        L("서하", "무엇을 봤는지, 왜 그렇게 했는지. 내일은 그걸 기억하면서 해 보자.", "focus"),
                        L("서하", "…일단 씻고 자자. 내일도, 다녀오겠습니다.", "happy")
                    }
                };
                sc.conditions.mode = MatchMode.All;
                sc.conditions.items.Add(C("nights_played", CompareOp.Equal, 1));
                cs.scenes.Add(sc);
                log.Add("장면: " + sc.title);
                changed = true;
            }
            if (cfg.Scene(MeetingSceneId) == null)
            {
                var sc = new SceneCfg
                {
                    id = MeetingSceneId, title = "점장님의 아침", category = "만남", priority = 0, repeatable = false,
                    image = storeBg, useCharacterLayer = false,
                    hint = "근무 없는 아침, 편의점 앞에서", conditionText = "3일차 이후 아침에 출근 대신 만남을 고르면",
                    lines = new List<LineCfg>
                    {
                        L("", "근무가 없는 아침. 편의점 앞을 지나는데 유리문이 열린다."),
                        L("문채린", "서하 씨, 오늘 쉬는 날이죠? 얼굴 보러 잠깐 나왔어요."),
                        L("서하", "점장님… 네, 오늘은 출근 안 하는 날이라서요.", "surprised"),
                        L("문채린", "요즘 밤에 뭘 그렇게 열심히 봐요? 출근할 때마다 눈이 빨개요."),
                        L("서하", "아, 그게… 공부 비슷한 거예요.", "tired"),
                        L("문채린", "뭘 하든 밥은 챙겨 먹어요. 끼니 거르면 계산도 틀려요. 가게에서도, 다른 데서도."),
                        L("서하", "…네. 명심할게요.", "happy"),
                        L("문채린", "다음 근무 때 봐요. 늦지 말고.")
                    }
                };
                cs.scenes.Add(sc);
                log.Add("장면: " + sc.title);
                changed = true;
            }
            // config JSON 에서 다시 가져온 뒤에는 그림 칸이 비어 밤 방 배경이 들어갈 수 있으니 만남 장면 배경을 바로잡는다.
            var meetScene = cfg.Scene(MeetingSceneId);
            if (meetScene != null && storeBg != null && (meetScene.image != storeBg || meetScene.useCharacterLayer))
            {
                meetScene.image = storeBg;
                meetScene.useCharacterLayer = false;
                changed = true;
            }
            bool hasMeeting = false;
            foreach (var m in cs.meetings) if (m != null && m.id == MeetingId) hasMeeting = true;
            if (!hasMeeting)
            {
                cs.meetings.Add(new MeetingCfg
                {
                    id = MeetingId, enabled = true, buttonLabel = "점장님 만나러 가기 (근무 없음)",
                    sceneId = MeetingSceneId, minDay = 3, requiresSeenScene = DailySceneId
                });
                log.Add("아침 만남: 점장님");
                changed = true;
            }
            return changed;
        }

        static bool Reaction(DialogueConfig d, string key, List<string> log, params LineCfg[] lines)
        {
            foreach (var r in d.reactions) if (r != null && r.key == key) return false;
            d.reactions.Add(new ReactionCfg { key = key, lines = new List<LineCfg>(lines) });
            log.Add("반응: " + key);
            return true;
        }
    }
}
