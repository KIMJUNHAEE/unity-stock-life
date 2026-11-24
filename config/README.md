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

전체 목록은 드롭다운에 나옵니다. 새 값이 필요하면 `Assets/Scripts/Sim/Progression.cs` 의 `ComputeStats()` 와 `StatNames` 에 함께 추가합니다.

## 스토리 구간

`data/story_milestones.json` 의 `requirements` 가 하나라도 `null` 이면 그 구간은 열리지 않습니다 (기획 원칙). 시험하려면 콘텐츠 편집기에서 숫자를 넣으세요. 시작 부채 10억을 포함하므로 `net_financial_assets_min` 은 큰 음수입니다(예: `-999800000`).

## 단축키 (플레이 중)

F1 편집 방법·데이터 상태 · F5 다시 읽기 · F12 디버그 패널 · Space 시장 일시정지 · Esc 창 닫기
