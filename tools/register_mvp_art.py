"""Register generated MVP art and verify original bytes using Python standard library only."""
from __future__ import annotations
import hashlib
import json
import pathlib
import struct
from collections import Counter

ROOT = pathlib.Path(__file__).resolve().parents[1]
DATE = "2026-11-25"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def section(path, marker, text):
    old = path.read_text(encoding="utf-8-sig")
    start, end = f"<!-- {marker}_BEGIN -->", f"<!-- {marker}_END -->"
    block = start + "\n" + text.rstrip() + "\n" + end
    if start in old:
        a, b = old.index(start), old.index(end) + len(end)
        old = old[:a] + block + old[b:]
    else:
        old = old.rstrip() + "\n\n" + block + "\n"
    path.write_text(old, encoding="utf-8", newline="\n")

def main():
    jobs = json.loads((ROOT / "tools/mvp_art_jobs.json").read_text(encoding="utf-8"))
    missing = [j["key"] for j in jobs if not (ROOT / j["path"]).is_file()]
    if missing:
        raise SystemExit("Missing art: " + ", ".join(missing))
    source_file = ROOT / "tools/mvp_art_sources.json"
    sources = {e["id"]: e["source"] for e in json.loads(source_file.read_text(encoding="utf-8"))} if source_file.exists() else {}
    terms = ROOT / "ThirdParty/Generated/ART-0001/terms"
    term_files = ["openai_terms_of_use_20261125.html", "openai_service_terms_20261125.html"]
    terms_records = [{"file": (terms / n).relative_to(ROOT).as_posix(), "sha256": sha(terms / n)} for n in term_files]
    records = []
    for j in jobs:
        target = ROOT / j["path"]
        directory = ROOT / "ThirdParty/Generated" / j["id"]
        original, prompt = directory / "generated_original.png", directory / "prompt.txt"
        assert original.is_file() and prompt.is_file(), j["id"]
        assert sha(original) == sha(target), j["key"]
        blob = original.read_bytes()
        assert blob[:8] == b"\x89PNG\r\n\x1a\n", j["key"]
        size = list(struct.unpack(">II", blob[16:24]))
        color_type = blob[25]
        if j["alpha"]:
            assert color_type == 6, (j["key"], "Missing RGBA alpha channel")
        refs = [{"file": f, "sha256": sha(ROOT / f)} for f in j["references"]]
        record = {
            "asset_id": j["id"], "key": j["key"], "status": "superseded" if j.get("active") is False else "mvp_review_candidate",
            "superseded_by": j.get("superseded_by"),
            "created_on": DATE, "purpose": j["kind"], "target_size_px": j["target"],
            "actual_generated_size_px": size, "transparent_background_requested": j["alpha"],
            "png_color_type": color_type, "alpha_channel_present": color_type == 6,
            "tool": "Codex built-in image_gen", "model": None, "seed": None,
            "reference_files_and_hashes": refs, "prompt_file": "prompt.txt", "prompt_sha256": sha(prompt),
            "original_file": "generated_original.png", "original_sha256": sha(original),
            "generator_saved_output": sources.get(j["id"]),
            "project_file": j["path"], "project_sha256": sha(target),
            "transformations": [], "terms_evidence": terms_records,
            "terms_note": "Reuses the same tool terms checked and archived on 2026-11-25 for ART-0001; no new legal clearance claimed.",
            "release_clearance": "not established; development MVP candidate",
            "review_notes": [
                "Native output retained; no enlargement, retouch or color-profile conversion.",
                "Appearance is a generated proposal, not automatic user approval.",
                "UI labels, prices, dialogue and charts remain real game UI."
            ]
        }
        if j["kind"] == "seoha":
            record["sprite_regions_normalized_bottom_left"] = {
                "standing": [0, 0, 1, 1], "portrait": [0.25, 0.68, 0.5, 0.32]
            }
            record["review_notes"].append("Shared pose and face crop; minor generated pixel differences are not claimed to be absent.")
        if refs:
            record["review_notes"].append("Reference lineage is recorded; REF-001 release-use permissions remain unverified.")
        write_json(directory / "provenance.json", record)
        records.append({**j, "actual_size": size, "sha256": sha(target)})
    runtime = [{"id": r["id"], "key": r["key"], "path": r["path"], "kind": r["kind"]} for r in records if r["kind"] != "sheet" and r.get("active") is not False]
    write_json(ROOT / "Assets/Art/mvp_art_manifest.json", {"version": 1, "status": "mvp_review_candidate", "entries": runtime})
    ledger = ["## MVP 아트 v001", "", "생성 후보이며 외형 승인·출시 권리 확인과 구분한다. 최초 출력·프롬프트·참조·약관 연결·해시는 각 ID의 provenance.json에 보관한다.", "", "| ID | 키 | 프로젝트 파일 | 실제 원본 | 상태 |", "| --- | --- | --- | --- | --- |"]
    for r in records:
        ledger.append(f'| {r["id"]} | {r["key"]} | `{r["path"]}` | {r["actual_size"][0]}×{r["actual_size"][1]} | {"보관·후속 버전으로 교체" if r.get("active") is False else "MVP 검토 후보"} |')
    ledger += ["", "UI-MVP-001~005는 기존 uGUI와 맞춘 코드 기반 앱 아이콘이다. 자체 C# 도형으로 제작하며 외부 이미지·서체를 포함하지 않는다. 제작 원본은 Assets/Scripts/Editor/MvpArtSetup.cs, 결과는 Assets/Art/UI/Icons에 보관한다.",
               "", "전용 컷신 CG 대신 공용 야간 방 배경과 서하의 입상·표정을 별도 레이어로 연결한다. 이는 17개의 서로 다른 전용 CG 제작 완료를 뜻하지 않는다. 공용 스토리 방은 고정 무대 시안이며 구매한 가구의 실시간 복제는 아니다."]
    section(ROOT / "docs/리소스 관리대장.md", "MVP_ART_V001", "\n".join(ledger))
    notices = ROOT / "THIRD_PARTY_NOTICES.txt"
    old = notices.read_text(encoding="utf-8-sig")
    marker = "\nMVP_ART_V001_BEGIN\n"
    if marker in old:
        a, b = old.index(marker), old.index("\nMVP_ART_V001_END", old.index(marker)) + len("\nMVP_ART_V001_END")
        old = old[:a] + old[b:]
    block = "\nMVP_ART_V001_BEGIN\nDevelopment MVP art candidates ART-0003 through ART-0049.\nGenerated with Codex built-in image_gen on 2026-11-25. No CLI fallback.\n"
    block += "\n".join(f'{r["id"]}: {r["path"]}' for r in records)
    block += "\nOriginal output, exact prompt, references, hashes and archived tool terms:\nThirdParty/Generated/<AssetID>/provenance.json\nSource references remain development references; release clearance is not established.\n"
    block += "UI-MVP-001 through 005: original code-drawn OS icons, no third-party artwork.\nSource: Assets/Scripts/Editor/MvpArtSetup.cs; PNGs: Assets/Art/UI/Icons/.\nMVP_ART_V001_END\n"
    notices.write_text(old.rstrip() + "\n" + block, encoding="utf-8", newline="\n")
    people = [j["person"] for j in jobs if j["kind"] == "sheet"]
    for p in people:
        sheet = next(r for r in records if r["key"] == "sheet_" + p["key"])
        sprite = next(r for r in records if r["key"] == "customer_" + p["key"])
        text = f'# {p["name"]} 캐릭터 시트 v001\n\n작성일 {DATE} · 성인 여성 {p["age"]}세 · {p["role"]} · 외형 검토 후보\n\n'
        text += f'![{p["name"]} 시트](../concepts/chr_{p["key"]}_sheet_v001.png)\n\n'
        text += f'현재 MVP 편의점의 손님으로 등장한다. 윤서하 시트는 선화·셀 채색·시트 구성 참고이며, 새 인물의 얼굴·헤어·의상을 별도로 설계했다. 나이·이름·세부 외형은 이번 사용자 방향에 맞춘 제작안이다.\n\n'
        text += f'외형 요청 원문: {p["design"]}\n\n'
        text += '정면·측면·후면 전신, 기본·웃음·놀람·피곤 표정, 의상 구조와 색상표를 포함한다. 뒷모습과 소품 세부는 새 디자인 제안이다.\n\n'
        text += f'- 시트: {sheet["id"]}, 실제 {sheet["actual_size"][0]}×{sheet["actual_size"][1]} 불투명 PNG.\n'
        text += f'- 게임용 이미지: [{p["key"]}](../../{sprite["path"]}), {sprite["id"]}, 실제 {sprite["actual_size"][0]}×{sprite["actual_size"][1]} 투명 PNG.\n'
        text += f'- 기록: [시트 프롬프트](../../ThirdParty/Generated/{sheet["id"]}/prompt.txt), [게임용 제작 기록](../../ThirdParty/Generated/{sprite["id"]}/provenance.json).\n\n'
        text += '최초 출력은 확대·크롭 없이 보관한다. 게임용 파일은 시트의 작은 그림을 확대한 결과가 아닌 별도 참조 생성물이다. 최종 외형 승인과 Steam 출시 권리 확인은 별도다.\n'
        (ROOT / "docs/characters" / f'{p["key"]}_character_sheet_v001.md').write_text(text, encoding="utf-8")
    counts = Counter(r["kind"] for r in records if r.get("active") is not False)
    report = ["# MVP 에셋 v001", "", f"제작일 {DATE}. 채택 후보 PNG {len(runtime)+4}장(캐릭터 시트 4장 + 게임용 원본 {len(runtime)}장), 별도 코드 기반 앱 아이콘 5종. 의자 2종의 첫 출력은 교체 이력으로 보관해 총 생성 원본은 {len(records)}장이다.", "",
              "현재 MVP에 필요한 이미지 슬롯을 기준으로 제작했다. 모든 이미지의 원본·요청 크기·실제 크기·프롬프트·참조·해시는 각 ThirdParty/Generated/ART-xxxx/provenance.json에 보관한다. 원본을 늘려 목표 해상도로 표시하지 않는다.", "",
              "## 인물", "", "| 인물 | 나이·역할 | 시트 |", "| --- | --- | --- |",
              "| 윤서하 | 24세 제안·주인공 | [기존 시트](characters/yoon_seoha_character_sheet_v001.md) |"]
    for p in people:
        report.append(f'| {p["name"]} | {p["age"]}세·{p["role"]} | [시트](characters/{p["key"]}_character_sheet_v001.md) |')
    report += ["", "스토리 확장 점장은 문정숙(49)에서 문채린(29·성인 여성)으로 조정하고 동료 한유진(25·성인 여성)은 유지했다. 두 인물의 아침 만남은 현재 MVP에 추가 구현하지 않았으며 이번 게임용 이미지 대상에도 포함하지 않았다.", "",
               "## 연결 방식", "", "- 실행 설정은 Assets/Resources/GameConfig.asset이다. config JSON은 초기 가져오기 원본이므로 실제 에셋과 함께 이름을 맞춘다.",
               "- 메뉴 StockGame > MVP 아트 > 생성 에셋 연결 (배치 유지)로 다시 연결한다. 기존 이미지가 있는 칸은 덮어쓰지 않으며 수치와 사용자 UI 배치를 초기화하지 않는다.",
               "- 서하 6개 전신 PNG에 전신·얼굴 Sprite 영역을 지정한다. 얼굴 영역은 원본 기준 [0.25, 0.68, 0.5, 0.32]의 정규화 좌표(왼쪽 아래 원점)다. PNG 바이트는 변경하지 않는다.",
               "- 컷신은 공용 밤 방 배경과 별도 서하 레이어로 구성한다. 대사에 표정이 없으면 앞선 표정을 유지한다. 전용 CG와 가구 성장 상태 반영은 후속 제작 범위다.",
               "- 방·편의점·바탕화면, 가구 12종, 상품 12종, 손님 4종을 서로 분리한다. 가구·상품 이름과 가격·대사는 Unity UI 텍스트다.",
               "- 방 배경은 비율을 유지해 중앙 기준으로 뷰포트에 맞춰 자른다. 다른 비율로 늘려 찌그러뜨리지 않는다.",
               "- 제작 후보를 MVP에 연결하는 것이 최종 디자인 승인이나 상업적 출시 권리 확인을 뜻하지 않는다.", "", "## 원본 목록", "",
               "| ID | 키 | 파일 | 실제 크기 |", "| --- | --- | --- | --- |"]
    for r in records:
        report.append(f'| {r["id"]} | {r["key"]} | [{r["path"].rsplit("/",1)[-1]}](../{r["path"]}) | {r["actual_size"][0]}×{r["actual_size"][1]} |')
    report += ["", "## 검증 기록", "", "이미지 원본·프로젝트 사본·참조·프롬프트·약관 증빙의 파일 해시와 PNG 크기·RGBA 채널을 확인했다. Unity 컴파일·연결 검증 및 화면 검토 결과는 docs/previews/mvp_art_validation.json과 Unity 로그로 별도 기록한다. 첫 7일 게임 밸런스 검증을 완료했다고 표시하지 않는다.", ""]
    report += ["## 검토용 모아보기", "",
               "- [캐릭터와 표정](previews/mvp_characters_contact_v001.png)",
               "- [가구·상품·앱 아이콘](previews/mvp_objects_contact_v001.png)",
               "- [방·편의점·바탕화면](previews/mvp_backgrounds_contact_v001.png)",
               "- [정확한 생성 요청 목록](../tools/mvp_art_jobs.json)", "",
               "## 이번 범위에서 제외한 것", "",
               "기존 assets.json의 site_news_logo·site_community_logo는 현재 uGUI에서 이미지 슬롯으로 사용하지 않아 사이트 이름을 실제 UI 텍스트로 유지한다. 비활성 선물 장면 CS_liquidation도 이번 제작 대상에 포함하지 않았다. 점장·동료 만남, 투자 복기, 거래 연장, 생활 진척 통계는 이번 아트 작업으로 구현 완료 처리하지 않는다.", ""]
    validation = ROOT / "docs/previews/mvp_art_validation.json"
    if validation.exists():
        qa = json.loads(validation.read_text(encoding="utf-8"))
        if qa.get("status") == "PASS":
            report += ["## Unity 화면 확인 결과", "",
                       "Unity 6000.3.29f1 컴파일·Sprite 연결 검증과 플레이 모드 화면 점검을 통과했다. 런타임 오류 0건, 사용자 진행 저장을 호출하지 않은 메모리 상태로 점검했다. 첫 7일 경제·밸런스 검증은 수행하지 않았다.",
                       "", "서하·가구의 실제 불투명 영역을 기준으로 집의 발끝·책상 높이를 맞추고 빈 가구 슬롯을 숨겼다. 숨은 패널의 그림자도 같은 조건으로 숨기며, 앱 이름이 비었을 때 빈 창이 표시되지 않게 했다.",
                       "", "- [타이틀](previews/mvp_title_v001.png)",
                       "- [집](previews/mvp_home_v001.png)",
                       "- [편의점](previews/mvp_store_v001.png)",
                       "- [바탕화면](previews/mvp_desktop_v001.png)",
                       "- [공용 무대 컷신](previews/mvp_cutscene_v001.png)",
                       "- [검증 수치](previews/mvp_art_validation.json)", ""]
    (ROOT / "docs/mvp_assets_v001.md").write_text("\n".join(report), encoding="utf-8")
    write_json(ROOT / "docs/previews/mvp_art_file_validation.json", {"status": "PASS", "files": len(records), "runtime_files": len(runtime), "kinds": dict(counts), "original_copy_hashes_equal": True})
    print(json.dumps({"status": "PASS", "files": len(records), "kinds": dict(counts)}))

if __name__ == "__main__":
    main()
