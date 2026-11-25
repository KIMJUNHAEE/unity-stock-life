"""config/*.json 검증: 문법, 참조, 조건식의 stat 이름.

프로젝트 루트에서 실행한다. 별도 패키지가 필요 없다.

    python tools/validate_config.py

게임은 문법 오류가 있는 파일을 만나면 이전 내용을 유지하고 화면에 오류를 띄우지만,
참조 오류(없는 이미지 키, 오타 난 stat 이름 등)는 조용히 무시되므로 이 스크립트로 확인한다.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

# Assets/Scripts/Sim/Progression.cs 의 ComputeStats() 와 같은 목록을 유지한다.
KNOWN_STATS = {
    "day", "nights_played", "cash", "realized_today", "fees_today", "trades_today", "buys_today",
    "sells_today", "start_capital", "return_today", "took_loan_today", "loan_amount_today",
    "had_loan_before_today", "loan_balance", "loan_overdue", "loan_cleared_today", "repaid_loan_today",
    "repaid_starting_debt_today", "interest_today", "starting_debt_remaining", "ever_took_loan",
    "investable_assets", "holdings_value", "unrealized_pnl", "net_financial_assets",
    "net_financial_assets_excluding_starting_debt", "cumulative_realized_profit",
    "consecutive_profit_nights", "condition", "condition_stage", "min_condition_today",
    "meal_used_today", "snack_used_today", "drinks_used_today", "ended_early", "session_ticks_watched",
    "session_length", "rumor_early_profit_today", "false_rumor_loss_today", "news_chase_loss_today",
    "articles_read_today", "posts_read_today", "job_done_today", "job_pay_today", "furniture_owned",
    "furniture_spent_today", "forced_liquidations_today", "story_milestones_unlocked",
    "life_net_funds", "life_progress", "cumulative_furniture_spent", "cumulative_starting_debt_repaid",
    "extended_today", "rested_today", "met_today", "meetings_done", "review_opened_today",
}
OPS = {">", ">=", "<", "<=", "==", "!="}

errors: list[str] = []
warnings: list[str] = []


def load(rel: str):
    path = ROOT / rel
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except FileNotFoundError:
        errors.append(f"{rel}: 파일 없음")
    except json.JSONDecodeError as e:
        errors.append(f"{rel}: JSON 문법 오류 {e.lineno}줄 {e.colno}열 — {e.msg}")
    return None


def check_condition(cond, where: str):
    if cond is None:
        errors.append(f"{where}: 조건이 비어 있음")
        return
    if isinstance(cond, list):
        for c in cond:
            check_condition(c, where)
        return
    if not isinstance(cond, dict):
        errors.append(f"{where}: 조건은 객체여야 함")
        return
    if "all" in cond or "any" in cond:
        for c in cond.get("all", []) + cond.get("any", []):
            check_condition(c, where)
        return
    if "not" in cond:
        check_condition(cond["not"], where)
        return
    if "flag" in cond:
        return
    stat = cond.get("stat")
    if stat not in KNOWN_STATS:
        errors.append(f"{where}: 알 수 없는 stat '{stat}'")
    if cond.get("op", ">=") not in OPS:
        errors.append(f"{where}: 알 수 없는 op '{cond.get('op')}'")
    if not isinstance(cond.get("value"), (int, float)):
        errors.append(f"{where}: value 는 숫자여야 함 (null 은 항상 거짓)")


def main() -> int:
    cfg = {name: load(f"config/{name}.json") for name in
           ["balance", "job", "cutscenes", "dialogue", "shop", "theme", "assets", "names"]}
    companies = load("data/companies.json")
    events = load("data/events.json")
    if errors:
        for e in errors:
            print("오류:", e)
        return 1

    images = set(cfg["assets"].get("images", {}).keys())

    def need_image(key, where):
        if key and key not in images:
            warnings.append(f"{where}: assets.json 에 이미지 키 '{key}' 없음 (자리표시로 표시됨)")

    company_ids = {c["id"] for c in companies["items"]}
    for cid in cfg["balance"].get("market", {}).get("tradable_company_ids", []):
        if cid not in company_ids:
            errors.append(f"balance.market.tradable_company_ids: 없는 회사 '{cid}'")

    outcomes = cfg["balance"].get("event_outcomes", {})
    for ev in events["items"]:
        t = outcomes.get(ev["id"])
        if t is None:
            warnings.append(f"balance.event_outcomes: '{ev['id']}' 항목 없음 → 이 결과는 배정되지 않음")
            continue
        imp = t.get("impact")
        if not (isinstance(imp, list) and len(imp) == 2 and all(isinstance(v, (int, float)) for v in imp)):
            errors.append(f"balance.event_outcomes.{ev['id']}.impact 는 [최소, 최대] 숫자 배열")
        if t.get("tone") not in {"positive", "neutral", "negative"}:
            errors.append(f"balance.event_outcomes.{ev['id']}.tone 은 positive/neutral/negative")
    for key in outcomes:
        if key not in {e["id"] for e in events["items"]}:
            warnings.append(f"balance.event_outcomes: data/events.json 에 없는 결과 '{key}'")

    for i, st in enumerate(cfg["balance"].get("condition", {}).get("stages", [])):
        if "min" not in st:
            errors.append(f"balance.condition.stages[{i}]: min 없음")

    ids = set()
    cs = cfg["cutscenes"]
    meeting_scenes = {m.get("scene_id") for m in cs.get("meetings", []) or []}
    for sc in [cs.get("default_scene", {})] + cs.get("scenes", []):
        sid = sc.get("id")
        if sid in ids:
            errors.append(f"cutscenes: 중복 id '{sid}'")
        ids.add(sid)
        need_image(sc.get("image"), f"cutscenes.{sid}")
        if sc is not cs.get("default_scene") and sid not in meeting_scenes:
            check_condition(sc.get("conditions"), f"cutscenes.{sid}.conditions")
        for line in sc.get("lines", []):
            if "expression" in line and line["expression"] not in cfg["dialogue"].get("expressions", {}):
                warnings.append(f"cutscenes.{sid}: 표정 '{line['expression']}' 이 dialogue.expressions 에 없음")

    for m in cs.get("meetings", []) or []:
        if m.get("scene_id") not in ids:
            errors.append(f"cutscenes.meetings.{m.get('id')}: 장면 '{m.get('scene_id')}' 이 없음")
        if m.get("requires_seen_scene") and m["requires_seen_scene"] not in ids:
            errors.append(f"cutscenes.meetings.{m.get('id')}: 먼저 볼 장면 '{m['requires_seen_scene']}' 이 없음")

    for name, th in cfg["theme"].get("themes", {}).items():
        if "unlock" in th:
            check_condition(th["unlock"], f"theme.themes.{name}.unlock")
    if cfg["theme"].get("default_theme") not in cfg["theme"].get("themes", {}):
        errors.append("theme.default_theme 이 themes 에 없음")

    layout_room = cfg["theme"].get("layout", {}).get("room", {})
    slot_ids = set()
    for slot in cfg["shop"].get("slots", []):
        slot_ids.add(slot["id"])
        if slot["id"] not in layout_room:
            warnings.append(f"shop.slots '{slot['id']}': theme.layout.room 에 위치 없음 (기본 위치 사용)")
    item_ids = set()
    for it in cfg["shop"].get("items", []):
        if it["id"] in item_ids:
            errors.append(f"shop.items: 중복 id '{it['id']}'")
        item_ids.add(it["id"])
        if it.get("slot") not in slot_ids:
            errors.append(f"shop.items.{it['id']}: 없는 slot '{it.get('slot')}'")
        need_image(it.get("image"), f"shop.items.{it['id']}")
    for sid in cfg["shop"].get("starting_items", []):
        if sid not in item_ids:
            errors.append(f"shop.starting_items: 없는 상품 '{sid}'")

    for key, img in cfg["dialogue"].get("expressions", {}).items():
        need_image(img, f"dialogue.expressions.{key}")
    for it in cfg["job"].get("items", []):
        need_image(it.get("image"), f"job.items.{it.get('name')}")
    pay_ids = {p["id"] for p in cfg["job"].get("payments", [])}
    if "cash" not in pay_ids:
        warnings.append("job.payments: cash 가 없으면 거스름돈 단계가 나오지 않음")

    missing_files = [k for k, v in cfg["assets"]["images"].items() if not (ROOT / v.get("path", "")).exists()]

    for w in warnings:
        print("주의:", w)
    for e in errors:
        print("오류:", e)
    print(f"이미지 {len(images)}개 중 파일 있음 {len(images) - len(missing_files)}개 (나머지는 자리표시)")
    if errors:
        print(f"\n오류 {len(errors)}건")
        return 1
    print("config 검증 통과")
    return 0


if __name__ == "__main__":
    sys.exit(main())
