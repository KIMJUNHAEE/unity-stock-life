# 편집 방법 (uGUI · Inspector 방식)

게임은 이제 일반적인 Unity 방식으로 고칩니다. **플레이를 누르지 않아도** Scene 뷰와 Inspector에서 바로 보이고 바뀝니다.

| 고칠 것 | 어디서 |
| --- | --- |
| 버튼·창·글자·가구 자리의 **위치와 크기** | Hierarchy ▸ `UI` 아래 오브젝트 선택 → Scene 뷰에서 Rect 툴(T)로 드래그 |
| **색, 글자 내용·크기, 고정 이미지** | 같은 오브젝트의 Inspector (Image ▸ Color / Source Image, Text ▸ Font Size 등) |
| **수치·문구·바뀌는 이미지** (시장 속도, 시작 현금, 알바 급여, 컷신 대사·일러스트, 가구 그림, 서하 표정) | `Assets/Resources/GameConfig.asset` 클릭 → Inspector (메뉴 **StockGame ▸ 설정 에셋 열기**) |
| **뉴스·게시글·댓글·회사·사건·스토리 구간** | 프로젝트 루트 `data/` JSON — 메뉴 **StockGame ▸ 콘텐츠 편집기** |

## 화면 편집 순서

1. 메뉴 **StockGame ▸ 화면 보기 ▸ 집** (또는 알바·컴퓨터·결산·컷신, 앱 - 증권 등)을 고르면 그 화면만 켜집니다.
2. Scene 뷰 상단의 **2D** 버튼을 켜고, Hierarchy에서 오브젝트를 골라 옮기거나 크기를 바꿉니다.
3. 겹쳐 있는 패널(가구 카탈로그, 컨디션 창, 알바 결과 등)은 처음에 숨겨져 있습니다. 골라서 **Ctrl+Shift+H** (메뉴 **StockGame ▸ 선택한 패널 보이기·숨기기**) 로 보이게 한 뒤 편집하세요. 플레이 중에는 자동으로 상황에 맞게 켜지고 꺼집니다.
4. 목록(관심 종목, 기사, 상품 등)은 `Template` 오브젝트가 한 줄의 모양입니다. Template 을 꾸미면 플레이 때 모든 줄이 그 모양으로 복제됩니다.
5. 저장은 Ctrl+S (씬 저장).

### 집의 침대·책상·컴퓨터 크기와 위치 조절

**먼저 ▶ 플레이를 중지하세요.** 플레이 중 씬 오브젝트를 옮기면 중지할 때 원래 배치로 돌아갑니다.

1. **StockGame ▸ 화면 보기 ▸ 집**을 고르고 **Scene** 탭을 엽니다. **2D**를 켭니다.
2. Hierarchy에서 `UI ▸ Screen_Home ▸ Room`을 펼치고 아래 오브젝트를 고릅니다.

| 화면 요소 | Room 아래 이름 |
| --- | --- |
| 침대 | `Bed` |
| 책상 | `Slot_desk` |
| 노트북·컴퓨터 | `Slot_pc` |
| 의자 | `Slot_chair` |
| 조명 | `Slot_light` |
| 화분·장식 | `Slot_deco` |
| 문 | `Door` |
| 서하 | `Seoha` |

3. **T**를 누르면 사각형 편집 도구가 됩니다. 가운데를 드래그하면 이동, 가장자리·모서리를 드래그하면 크기가 바뀝니다. **F**는 선택한 물체에 화면을 맞춥니다.
4. 정확한 수치는 Inspector의 **Rect Transform ▸ Pos X / Pos Y / Width / Height**에서 바꿉니다. 현재 배치는 왼쪽 위 기준이므로 X를 늘리면 오른쪽, Y를 줄이면 아래로 이동합니다. **Scale은 1, 1, 1**로 두고 Width/Height를 조절하는 것이 편합니다. **Image ▸ Preserve Aspect**는 켜 둡니다. PNG의 투명 여백 때문에 선택 사각형이 실제 그림보다 클 수 있습니다.
5. 책상과 노트북은 별개이므로 함께 이동하려면 `Slot_desk`와 `Slot_pc`를 Ctrl로 여러 개 선택합니다. 의자·조명도 필요하면 함께 고릅니다.
6. 클릭 위치는 별도 오브젝트입니다. 침대를 옮겼으면 `Hot_Bed`, 문은 `Hot_Door`, 컴퓨터는 `Hot_Computer`의 Rect Transform도 맞춰 주세요. 위쪽 작은 이름표는 각 Hot_* 아래의 `Chip`입니다.
7. **Ctrl+S**로 씬을 저장하고 ▶로 확인합니다. 편집 중에는 기본 시작 가구를 미리 보여 주며, 실제 플레이에서는 현재 구매·장착 상태로 표시됩니다. 구매 전 가구를 편집할 때는 해당 Slot_*의 **Image ▸ Enabled**를 켜고 **Source Image**에 원하는 가구 Sprite를 넣어 미리 볼 수 있습니다. 플레이할 때는 자동 교체됩니다.

**StockGame ▸ MVP 아트 ▸ 생성 에셋 연결 (배치 유지)**는 처음 연결할 때만 기본 가구 위치를 잡습니다. 이후 다시 연결해도 직접 바꾼 배치는 유지합니다. **UI 만들기 (씬에 생성)**는 전체를 기본 배치로 재생성하므로 배치 조절에 사용하지 마세요.

### 편의점 상품 카드와 안쪽 그림 조절

▶를 중지한 뒤 **StockGame ▸ 화면 보기 ▸ 편의점 알바**를 고릅니다. Hierarchy의 `UI ▸ Screen_Job ▸ Playing`을 선택하고 **Ctrl+Shift+H**로 근무 중 패널을 표시합니다. `Intro`가 가리면 같은 단축키로 숨깁니다.

- **상품 묶음 전체 위치·영역:** `Playing ▸ CounterItems`의 **Rect Transform**. 현재 X=110, Y=-700, Width=1060, Height=230입니다.
- **카드 정렬·간격:** 같은 `CounterItems`의 **Horizontal Layout Group**. **Child Alignment = Upper Center**면 가운데, **Upper Left**면 왼쪽, **Upper Right**면 오른쪽입니다. **Spacing**이 카드 사이 간격이며 기본은 20입니다. 기본 정렬을 Upper Center로 바꿨으므로 상품 수가 적어도 가운데에 모입니다.
- **카드 자체 크기:** `CounterItems ▸ Template`의 **Layout Element ▸ Preferred Width / Preferred Height**. 기본은 210 / 230입니다. 카드 위치는 부모 정렬 컴포넌트가 정하므로 Template의 Pos X를 바꾸는 방식으로 조절하지 않습니다.
- **카드 안 상품 그림 크기·위치:** `Template ▸ Image`의 **Rect Transform**. 기본 X=20, Y=-16, Width=170, Height=120입니다. X를 늘리면 그림이 오른쪽으로, Y를 줄이면 아래로 갑니다. 예를 들어 그림이 왼쪽으로 치우쳐 보이면 **Pos X를 20 → 40**으로 바꿔 보고 조절하세요. **Image ▸ Preserve Aspect**는 켜 두세요. 이 조절은 모든 상품 카드에 공통 적용됩니다.
- **상품명·바코드:** `Template ▸ Name`, `Template ▸ Barcode`의 Rect Transform과 Text를 조절합니다.

편집 중 그림이 비어 있으면 `Template ▸ Image`의 Image ▸ Source Image에 `Assets/Art/Props/Store/`의 상품 Sprite를 잠깐 넣어 배치를 확인할 수 있습니다. 플레이할 때는 상품별 그림으로 자동 교체됩니다. 그림마다 투명 여백이 달라 일부 상품만 치우쳐 보이면, 공통 Template 위치를 바꾸는 것만으로 각 상품을 다르게 보정할 수는 없습니다. 해당 PNG의 Unity **Sprite Editor**에서 Sprite 사각형을 그림에 맞게 조절하는 방법이 있습니다. 원본 PNG는 보관하세요.

플레이 중 생기는 `Item_0`, `Item_1` 대신 **Template을 수정하고 Ctrl+S**로 저장하세요. Template은 플레이 시 숨겨지고 복제됩니다. F5는 데이터 다시 읽기이며 씬 배치 저장이 아닙니다.

### 이미지 넣기

- 그림 파일을 `Assets/Art/` 아래에 넣고, Inspector 에서 **Texture Type 을 Sprite (2D and UI)** 로 바꿉니다.
- 고정 그림(방 배경, 문, 침대, 타이틀 배경 등): 해당 오브젝트 Image 의 **Source Image** 에 끌어다 놓고, 안쪽 `Placeholder` 글자는 지웁니다.
- 바뀌는 그림(가구 단계, 서하 표정, 손님, 상품, 컷신 일러스트, 앱 아이콘, 바탕화면): GameConfig 의 해당 항목 이미지 칸에 끌어다 놓습니다.
- 넣기 전에 `docs/에셋 생성 규칙.md` 와 리소스 관리대장 기준을 따릅니다.

### 컴포넌트가 하는 일

UI 오브젝트에 붙은 작은 컴포넌트들이 화면과 게임 규칙을 연결합니다. 키 이름만 맞으면 오브젝트를 옮기거나 복제해도 그대로 동작합니다.

| 컴포넌트 | 역할 |
| --- | --- |
| BindText | 글자를 게임 값으로 채움 (`key`). 씬에 적힌 글자는 편집용 예시 |
| BindButton | 누르면 `action` 실행, `enableKey` 가 거짓이면 비활성 |
| BindShow | `key` 가 참일 때만 보임 (CanvasGroup) |
| BindImage | 바뀌는 그림(`spriteKey`)·색(`colorKey`) |
| BindFill | 막대 길이 (0~1) |
| UIList | `Template` 을 항목 수만큼 복제 |
| ThemeColor | 테마 색을 따름 (해금 테마 적용용). 색을 직접 정하려면 이 컴포넌트를 지우세요 |
| CandleChart | 차트. 편집 중에도 예시 캔들이 보입니다. 색은 테마 색 up/down/grid |

각 화면 컨트롤러(TitleScreen, HomeScreen, StockApp …) 스크립트 맨 위 주석에 쓸 수 있는 키 목록이 있습니다.

### UI 를 처음부터 다시 만들기

메뉴 **StockGame ▸ UI 만들기** 는 기본 배치로 새로 만듭니다. 직접 고친 배치가 사라지므로, 필요하면 씬을 복사해 두고 실행하세요. 프로젝트를 처음 열 때 한 번 자동으로 실행됩니다.

## GameConfig (수치·문구)

- 항목에 마우스를 올리면 설명이 나옵니다. 위쪽 **찾기** 칸으로 검색할 수 있습니다.
- 플레이 중에 바꿔도 바로 반영됩니다. Unity 특성상 **플레이 중 바꾼 값도 에셋에 남습니다.**
- 새 게임부터 적용되는 값: 장 길이·시작 시각·캔들 단계 수, 거래 종목, 시작가, 시작 현금, 시작 부채, 시작 가구.
- 이 폴더(`config/*.json`)는 GameConfig 를 처음 만들 때 쓴 원본입니다. GameConfig Inspector 의 **config JSON 에서 다시 가져오기**로 되돌리거나, **JSON 으로 내보내기**로 백업(`config/exported/`)할 수 있습니다. 게임은 실행 중에 이 JSON 을 읽지 않습니다.

### 조건식 (컷신 해금 조건 · 테마 해금 조건)

GameConfig ▸ 밤 컷신 ▸ 각 장면 ▸ 해금 조건 에서 **모두 만족 / 하나라도** 와 조건 줄(값 이름 · 비교 · 기준값)을 고릅니다. 값 이름은 드롭다운입니다. 계산할 수 없는 값(예: 시작 자금 0일 때 수익률)은 항상 거짓입니다.

| 값 이름 | 뜻 |
| --- | --- |
| `realized_today` | 오늘 실현 순손익 (매수·매도 수수료 반영, 이전 날 산 주식 포함) |
| `trades_today` / `buys_today` / `sells_today` | 오늘 체결 수 |
| `return_today` | 실현 순손익 ÷ 거래 시작 직전 자금 (현금+보유 평가, 대출 포함) |
| `took_loan_today`, `loan_cleared_today`, `loan_balance`, `loan_overdue` | 추가 대출 |
| `repaid_starting_debt_today`, `starting_debt_remaining` | 시작 부채 |
| `investable_assets`, `net_financial_assets`, `net_financial_assets_excluding_starting_debt` | 자산 |
| `cumulative_realized_profit`, `consecutive_profit_nights` | 누적 실현 순손익, 거래한 밤 연속 이익 |
| `condition`, `min_condition_today`, `meal_used_today`, `ended_early`, `session_ticks_watched` | 컨디션·생활 |
| `rumor_early_profit_today` | 소문을 읽고 공식 발표 전 매수 → 호재 확인 후 매도한 이익 |
| `false_rumor_loss_today` | 소문을 읽고 발표 전 매수 → 악재 확인 후 매도한 손실 |
| `news_chase_loss_today` | 호재 기사를 읽고 진입 → 손실 매도 |
| `forced_liquidations_today` | 선물 도입 전까지 항상 0 |
| `life_progress`, `life_net_funds` | 생활 진척액 (대출 입금만으로 늘지 않고 가구 구매·시작 부채 상환은 진척을 지우지 않음), 시작 부채를 뺀 생활 순자금 |
| `extended_today`, `rested_today`, `met_today` | 오늘 연장 거래 / 휴식 / 아침 만남을 골랐는지 |

전체 목록은 드롭다운에 나옵니다. 새 값이 필요하면 `Assets/Scripts/Sim/Progression.cs` 의 `ComputeStats()` 와 `StatNames` 에 함께 추가합니다.

## 스토리 구간

`data/story_milestones.json` 의 `requirements` 가 하나라도 `null` 이면 그 구간은 열리지 않습니다 (기획 원칙). 시험하려면 콘텐츠 편집기에서 숫자를 넣으세요. 시작 부채 10억을 포함하므로 `net_financial_assets_min` 은 큰 음수입니다(예: `-999800000`).

## 아침 만남

GameConfig ▸ 밤 컷신 ▸ 아침 만남에 항목을 더하면 집 화면에 버튼이 나타납니다. 대사는 컷신 목록에 장면으로 넣고(해금 조건은 비움) `재생할 장면 id`로 연결합니다. 최소 일차·먼저 본 장면·추가 조건이 모두 맞을 때만 보이고, 고르면 그날은 출근하지 않은 것으로 처리됩니다.

## 단축키 (플레이 중)

F1 편집 방법·데이터 상태 · F5 다시 읽기 · F12 디버그 패널 · Space 시장 일시정지 · Esc 창 닫기
