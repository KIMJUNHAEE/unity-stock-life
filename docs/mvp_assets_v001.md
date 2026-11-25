# MVP 에셋 v001

제작일 2026-11-25. 채택 후보 PNG 45장(캐릭터 시트 4장 + 게임용 원본 41장), 별도 코드 기반 앱 아이콘 5종. 의자 2종의 첫 출력은 교체 이력으로 보관해 총 생성 원본은 47장이다.

현재 MVP에 필요한 이미지 슬롯을 기준으로 제작했다. 모든 이미지의 원본·요청 크기·실제 크기·프롬프트·참조·해시는 각 ThirdParty/Generated/ART-xxxx/provenance.json에 보관한다. 원본을 늘려 목표 해상도로 표시하지 않는다.

## 인물

| 인물 | 나이·역할 | 시트 |
| --- | --- | --- |
| 윤서하 | 24세 제안·주인공 | [기존 시트](characters/yoon_seoha_character_sheet_v001.md) |
| 강하린 | 26세·출근길 직장인 | [시트](characters/kang_harin_character_sheet_v001.md) |
| 백소은 | 21세·등교하는 대학생 | [시트](characters/baek_soeun_character_sheet_v001.md) |
| 차나리 | 23세·아침 운동을 마친 러너 | [시트](characters/cha_nari_character_sheet_v001.md) |
| 이도아 | 25세·야간 근무를 마친 방송 스태프 | [시트](characters/lee_doa_character_sheet_v001.md) |

스토리 확장 점장은 문정숙(49)에서 문채린(29·성인 여성)으로 조정하고 동료 한유진(25·성인 여성)은 유지했다. 두 인물의 아침 만남은 현재 MVP에 추가 구현하지 않았으며 이번 게임용 이미지 대상에도 포함하지 않았다.

## 연결 방식

- 실행 설정은 Assets/Resources/GameConfig.asset이다. config JSON은 초기 가져오기 원본이므로 실제 에셋과 함께 이름을 맞춘다.
- 메뉴 StockGame > MVP 아트 > 생성 에셋 연결 (배치 유지)로 다시 연결한다. 기존 이미지가 있는 칸은 덮어쓰지 않으며 수치와 사용자 UI 배치를 초기화하지 않는다.
- 서하 6개 전신 PNG에 전신·얼굴 Sprite 영역을 지정한다. 얼굴 영역은 원본 기준 [0.25, 0.68, 0.5, 0.32]의 정규화 좌표(왼쪽 아래 원점)다. PNG 바이트는 변경하지 않는다.
- 컷신은 공용 밤 방 배경과 별도 서하 레이어로 구성한다. 대사에 표정이 없으면 앞선 표정을 유지한다. 전용 CG와 가구 성장 상태 반영은 후속 제작 범위다.
- 방·편의점·바탕화면, 가구 12종, 상품 12종, 손님 4종을 서로 분리한다. 가구·상품 이름과 가격·대사는 Unity UI 텍스트다.
- 방 배경은 비율을 유지해 중앙 기준으로 뷰포트에 맞춰 자른다. 다른 비율로 늘려 찌그러뜨리지 않는다.
- 제작 후보를 MVP에 연결하는 것이 최종 디자인 승인이나 상업적 출시 권리 확인을 뜻하지 않는다.

## 원본 목록

| ID | 키 | 파일 | 실제 크기 |
| --- | --- | --- | --- |
| ART-0003 | sheet_kang_harin | [chr_kang_harin_sheet_v001.png](../docs/concepts/chr_kang_harin_sheet_v001.png) | 1672×941 |
| ART-0004 | sheet_baek_soeun | [chr_baek_soeun_sheet_v001.png](../docs/concepts/chr_baek_soeun_sheet_v001.png) | 1672×941 |
| ART-0005 | sheet_cha_nari | [chr_cha_nari_sheet_v001.png](../docs/concepts/chr_cha_nari_sheet_v001.png) | 1672×941 |
| ART-0006 | sheet_lee_doa | [chr_lee_doa_sheet_v001.png](../docs/concepts/chr_lee_doa_sheet_v001.png) | 1672×941 |
| ART-0007 | seoha_normal | [chr_yoon_seoha_default_normal_v001.png](../Assets/Art/Characters/Seoha/chr_yoon_seoha_default_normal_v001.png) | 1024×1536 |
| ART-0008 | room_bg | [bg_home_stage01_day_v001.png](../Assets/Art/Backgrounds/bg_home_stage01_day_v001.png) | 1672×941 |
| ART-0009 | store_bg | [bg_store_morning_v001.png](../Assets/Art/Backgrounds/bg_store_morning_v001.png) | 1672×941 |
| ART-0010 | wallpaper_day | [ui_wallpaper_day_v001.png](../Assets/Art/UI/ui_wallpaper_day_v001.png) | 1672×941 |
| ART-0011 | wallpaper_sakura | [ui_wallpaper_sakura_v001.png](../Assets/Art/UI/ui_wallpaper_sakura_v001.png) | 1672×941 |
| ART-0012 | cutscene_bg | [bg_story_room_night_v001.png](../Assets/Art/Backgrounds/bg_story_room_night_v001.png) | 1672×941 |
| ART-0013 | bed | [prop_bed_basic_v001.png](../Assets/Art/Props/Room/prop_bed_basic_v001.png) | 1835×857 |
| ART-0014 | door | [prop_door_basic_v001.png](../Assets/Art/Props/Room/prop_door_basic_v001.png) | 887×1774 |
| ART-0015 | customer_kang_harin | [chr_kang_harin_customer_v001.png](../Assets/Art/Characters/Customers/chr_kang_harin_customer_v001.png) | 1086×1448 |
| ART-0016 | customer_baek_soeun | [chr_baek_soeun_customer_v001.png](../Assets/Art/Characters/Customers/chr_baek_soeun_customer_v001.png) | 1086×1448 |
| ART-0017 | customer_cha_nari | [chr_cha_nari_customer_v001.png](../Assets/Art/Characters/Customers/chr_cha_nari_customer_v001.png) | 1086×1448 |
| ART-0018 | customer_lee_doa | [chr_lee_doa_customer_v001.png](../Assets/Art/Characters/Customers/chr_lee_doa_customer_v001.png) | 1086×1448 |
| ART-0019 | seoha_happy | [chr_yoon_seoha_default_happy_v001.png](../Assets/Art/Characters/Seoha/chr_yoon_seoha_default_happy_v001.png) | 1024×1536 |
| ART-0020 | seoha_sad | [chr_yoon_seoha_default_sad_v001.png](../Assets/Art/Characters/Seoha/chr_yoon_seoha_default_sad_v001.png) | 1024×1536 |
| ART-0021 | seoha_surprised | [chr_yoon_seoha_default_surprised_v001.png](../Assets/Art/Characters/Seoha/chr_yoon_seoha_default_surprised_v001.png) | 1024×1536 |
| ART-0022 | seoha_tired | [chr_yoon_seoha_default_tired_v001.png](../Assets/Art/Characters/Seoha/chr_yoon_seoha_default_tired_v001.png) | 1024×1536 |
| ART-0023 | seoha_focus | [chr_yoon_seoha_default_focus_v001.png](../Assets/Art/Characters/Seoha/chr_yoon_seoha_default_focus_v001.png) | 1024×1536 |
| ART-0024 | furn_desk_old | [prop_desk_old_v001.png](../Assets/Art/Props/Furniture/prop_desk_old_v001.png) | 1536×1024 |
| ART-0025 | furn_desk_wood | [prop_desk_wood_v001.png](../Assets/Art/Props/Furniture/prop_desk_wood_v001.png) | 1536×1024 |
| ART-0026 | furn_desk_l | [prop_desk_l_v001.png](../Assets/Art/Props/Furniture/prop_desk_l_v001.png) | 1536×1024 |
| ART-0027 | furn_pc_old | [prop_pc_old_v001.png](../Assets/Art/Props/Furniture/prop_pc_old_v001.png) | 1536×1024 |
| ART-0028 | furn_pc_monitor | [prop_pc_monitor_v001.png](../Assets/Art/Props/Furniture/prop_pc_monitor_v001.png) | 1536×1024 |
| ART-0029 | furn_pc_dual | [prop_pc_dual_v001.png](../Assets/Art/Props/Furniture/prop_pc_dual_v001.png) | 1536×1024 |
| ART-0030 | furn_chair_cushion | [prop_chair_cushion_v001.png](../Assets/Art/Props/Furniture/prop_chair_cushion_v001.png) | 1536×1024 |
| ART-0031 | furn_chair_ergo | [prop_chair_ergo_v001.png](../Assets/Art/Props/Furniture/prop_chair_ergo_v001.png) | 1536×1024 |
| ART-0032 | furn_light_stand | [prop_light_stand_v001.png](../Assets/Art/Props/Furniture/prop_light_stand_v001.png) | 1024×1536 |
| ART-0033 | furn_light_mood | [prop_light_mood_v001.png](../Assets/Art/Props/Furniture/prop_light_mood_v001.png) | 1312×1199 |
| ART-0034 | furn_deco_plant | [prop_deco_plant_v001.png](../Assets/Art/Props/Furniture/prop_deco_plant_v001.png) | 1254×1254 |
| ART-0035 | furn_deco_plush | [prop_deco_plush_v001.png](../Assets/Art/Props/Furniture/prop_deco_plush_v001.png) | 1254×1254 |
| ART-0036 | item_rice_ball | [prop_rice_ball_v001.png](../Assets/Art/Props/Store/prop_rice_ball_v001.png) | 1254×1254 |
| ART-0037 | item_banana_milk | [prop_banana_milk_v001.png](../Assets/Art/Props/Store/prop_banana_milk_v001.png) | 1254×1254 |
| ART-0038 | item_cup_noodle | [prop_cup_noodle_v001.png](../Assets/Art/Props/Store/prop_cup_noodle_v001.png) | 1254×1254 |
| ART-0039 | item_water | [prop_water_v001.png](../Assets/Art/Props/Store/prop_water_v001.png) | 1254×1254 |
| ART-0040 | item_lunchbox | [prop_lunchbox_v001.png](../Assets/Art/Props/Store/prop_lunchbox_v001.png) | 1254×1254 |
| ART-0041 | item_soda | [prop_soda_v001.png](../Assets/Art/Props/Store/prop_soda_v001.png) | 1254×1254 |
| ART-0042 | item_jelly | [prop_jelly_v001.png](../Assets/Art/Props/Store/prop_jelly_v001.png) | 1254×1254 |
| ART-0043 | item_sandwich | [prop_sandwich_v001.png](../Assets/Art/Props/Store/prop_sandwich_v001.png) | 1254×1254 |
| ART-0044 | item_coffee | [prop_coffee_v001.png](../Assets/Art/Props/Store/prop_coffee_v001.png) | 1254×1254 |
| ART-0045 | item_snack | [prop_snack_v001.png](../Assets/Art/Props/Store/prop_snack_v001.png) | 1254×1254 |
| ART-0046 | item_icecream | [prop_icecream_v001.png](../Assets/Art/Props/Store/prop_icecream_v001.png) | 1254×1254 |
| ART-0047 | item_pen | [prop_pen_v001.png](../Assets/Art/Props/Store/prop_pen_v001.png) | 1254×1254 |
| ART-0048 | furn_chair_cushion | [prop_chair_cushion_v002.png](../Assets/Art/Props/Furniture/prop_chair_cushion_v002.png) | 1024×1536 |
| ART-0049 | furn_chair_ergo | [prop_chair_ergo_v002.png](../Assets/Art/Props/Furniture/prop_chair_ergo_v002.png) | 1024×1536 |

## 검증 기록

이미지 원본·프로젝트 사본·참조·프롬프트·약관 증빙의 파일 해시와 PNG 크기·RGBA 채널을 확인했다. Unity 컴파일·연결 검증 및 화면 검토 결과는 docs/previews/mvp_art_validation.json과 Unity 로그로 별도 기록한다. 첫 7일 게임 밸런스 검증을 완료했다고 표시하지 않는다.

## 검토용 모아보기

- [캐릭터와 표정](previews/mvp_characters_contact_v001.png)
- [가구·상품·앱 아이콘](previews/mvp_objects_contact_v001.png)
- [방·편의점·바탕화면](previews/mvp_backgrounds_contact_v001.png)
- [정확한 생성 요청 목록](../tools/mvp_art_jobs.json)

## 이번 범위에서 제외한 것

기존 assets.json의 site_news_logo·site_community_logo는 현재 uGUI에서 이미지 슬롯으로 사용하지 않아 사이트 이름을 실제 UI 텍스트로 유지한다. 비활성 선물 장면 CS_liquidation도 이번 제작 대상에 포함하지 않았다. 점장·동료 만남, 투자 복기, 거래 연장, 생활 진척 통계는 이번 아트 작업으로 구현 완료 처리하지 않는다.

## Unity 화면 확인 결과

Unity 6000.3.29f1 컴파일·Sprite 연결 검증과 플레이 모드 화면 점검을 통과했다. 런타임 오류 0건, 사용자 진행 저장을 호출하지 않은 메모리 상태로 점검했다. 첫 7일 경제·밸런스 검증은 수행하지 않았다.

서하·가구의 실제 불투명 영역을 기준으로 집의 발끝·책상 높이를 맞추고 빈 가구 슬롯을 숨겼다. 숨은 패널의 그림자도 같은 조건으로 숨기며, 앱 이름이 비었을 때 빈 창이 표시되지 않게 했다.

- [타이틀](previews/mvp_title_v001.png)
- [집](previews/mvp_home_v001.png)
- [편의점](previews/mvp_store_v001.png)
- [바탕화면](previews/mvp_desktop_v001.png)
- [공용 무대 컷신](previews/mvp_cutscene_v001.png)
- [검증 수치](previews/mvp_art_validation.json)
