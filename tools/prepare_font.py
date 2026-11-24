"""Extract the downloaded official GyeonggiTitle fonts without modifying bytes.

Run from the project root. Pillow is only used to render the local preview.
"""

from pathlib import Path
from html.parser import HTMLParser
import hashlib
import json
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ThirdParty/Fonts/Gyeonggi"
DEST = ROOT / "Assets/Art/Fonts/GyeonggiTitle"
PAGE_URL = "https://www.gg.go.kr/contents/contents.do?ciIdx=679&menuId=2457"
ZIP_URL = "https://www.gg.go.kr/uploads/CONTENTS/site/gg/경기천년체_260917.zip"


def tables(data):
    count = struct.unpack_from(">H", data, 4)[0]
    return {
        tag.decode("ascii"): (offset, length)
        for tag, _, offset, length in (
            struct.unpack_from(">4sIII", data, 12 + i * 16) for i in range(count)
        )
    }


def font_names(data):
    offset, _ = tables(data)["name"]
    _, count, strings = struct.unpack_from(">HHH", data, offset)
    result = {}
    for i in range(count):
        platform, encoding, language, name_id, length, start = struct.unpack_from(
            ">HHHHHH", data, offset + 6 + i * 12
        )
        if platform == 3 and language == 0x409:
            raw = data[offset + strings + start:offset + strings + start + length]
            result[name_id] = raw.decode("utf-16-be")
    return result


def supported_codepoints(data):
    base, _ = tables(data)["cmap"]
    count = struct.unpack_from(">H", data, base + 2)[0]
    supported = set()
    for i in range(count):
        platform, encoding, relative = struct.unpack_from(">HHI", data, base + 4 + i * 8)
        if platform != 0 and not (platform == 3 and encoding in (1, 10)):
            continue
        at = base + relative
        fmt = struct.unpack_from(">H", data, at)[0]
        if fmt == 12:
            groups = struct.unpack_from(">I", data, at + 12)[0]
            for g in range(groups):
                start, end, first = struct.unpack_from(">III", data, at + 16 + g * 12)
                supported.update(c for c in range(start, end + 1) if first + c - start)
        elif fmt == 4:
            segments = struct.unpack_from(">H", data, at + 6)[0] // 2
            ends = at + 14
            starts = ends + segments * 2 + 2
            deltas = starts + segments * 2
            ranges = deltas + segments * 2
            for s in range(segments):
                end = struct.unpack_from(">H", data, ends + 2 * s)[0]
                start = struct.unpack_from(">H", data, starts + 2 * s)[0]
                delta = struct.unpack_from(">h", data, deltas + 2 * s)[0]
                shift = struct.unpack_from(">H", data, ranges + 2 * s)[0]
                for code in range(start, min(end, 0xFFFE) + 1):
                    if shift:
                        glyph = struct.unpack_from(">H", data, ranges + 2 * s + shift + 2 * (code - start))[0]
                        glyph = (glyph + delta) & 0xFFFF if glyph else 0
                    else:
                        glyph = (code + delta) & 0xFFFF
                    if glyph:
                        supported.add(code)
    return supported


class TextParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.parts = []

    def handle_data(self, data):
        if data.strip():
            self.parts.append(data.strip())


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    records = []
    with zipfile.ZipFile(SOURCE / "Gyeonggi-official-260917.zip") as archive:
        assert archive.testzip() is None, "Archive CRC verification failed"
        for entry in archive.infolist():
            if not entry.filename.lower().endswith(".ttf"):
                continue
            data = archive.read(entry)
            names = font_names(data)
            postscript = names.get(6, "")
            if postscript not in ("GyeonggiTitleL", "GyeonggiTitleM", "GyeonggiTitleB"):
                continue
            weight = {"GyeonggiTitleL": "Light", "GyeonggiTitleM": "Medium", "GyeonggiTitleB": "Bold"}[postscript]
            filename = f"GyeonggiTitle-{weight}.ttf"
            target = DEST / filename
            if target.exists() and target.read_bytes() != data:
                raise RuntimeError(f"Refusing to overwrite a different font: {target}")
            target.write_bytes(data)
            codes = supported_codepoints(data)
            hangul = sum(c in codes for c in range(0xAC00, 0xD7A4))
            required = "매수매도보유주문가능금액컨디션편의점뉴스커뮤니티₩%+-0123456789.,"
            missing = [c for c in required if ord(c) not in codes]
            records.append({
                "path": target.relative_to(ROOT).as_posix(),
                "sha256": hashlib.sha256(data).hexdigest(),
                "bytes": len(data), "names": names,
                "hangul_syllables": hangul, "missing_sample_characters": missing,
                "unmodified_archive_bytes": True,
            })
    if len(records) != 3:
        raise RuntimeError(f"Expected three horizontal GyeonggiTitle weights, got {records}")
    parser = TextParser()
    parser.feed((SOURCE / "official-source-page.html").read_text(encoding="utf-8"))
    text = "\n".join(parser.parts)
    start = text.index("경기도 서체 저작권")
    end = text.index("담당부서", start)
    license_text = text[start:end].strip()
    assert "프로그램에 탑재하여 배포" in license_text
    assert "공공누리 제 1유형" in text
    evidence = f"출처: {PAGE_URL}\n확인일: 2026-11-25\n\n{license_text}\n\n페이지 표시: 공공누리 제 1유형(출처표시)\n"
    (SOURCE / "LICENSE-source-extract.txt").write_text(evidence, encoding="utf-8")
    (DEST / "LICENSE.txt").write_text(evidence, encoding="utf-8")
    files = []
    for p in sorted(SOURCE.glob("*")):
        if p.is_file() and p.name != "manifest.json":
            files.append({"path": p.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(p.read_bytes()).hexdigest(), "bytes": p.stat().st_size})
    manifest = {"retrieved_date": "2026-11-25", "source_page": PAGE_URL, "download_url": ZIP_URL, "fonts": records, "evidence_files": files}
    (SOURCE / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(records, ensure_ascii=True, indent=2))
    render_preview()


def render_preview():
    from PIL import Image, ImageDraw, ImageFont
    img = Image.new("RGB", (1440, 1040), "#eff6fb")
    draw = ImageDraw.Draw(img)
    def font(size, weight="Medium"):
        return ImageFont.truetype(str(DEST / f"GyeonggiTitle-{weight}.ttf"), size)
    def text(x, y, content, size=24, fill="#263e56", weight="Medium"):
        draw.text((x, y), content, font=font(size, weight), fill=fill)
    draw.rectangle((0, 0, 1440, 10), fill="#249be0")
    text(56, 34, "경기천년제목 · 게임 UI 폰트 미리보기", 38, weight="Bold")
    text(56, 94, "공식 배포 원본 TTF  |  Light · Medium · Bold  |  후보 시안", 21, "#607c93")
    for label, weight, y in [("LIGHT · 보조 문구", "Light", 162), ("MEDIUM · 본문과 뉴스", "Medium", 246), ("BOLD · 제목과 주요 숫자", "Bold", 330)]:
        text(56, y, label, 18, "#648096")
        text(365, y - 5, "오늘의 수익으로 내일의 방을 바꿔요. 0123456789", 28, weight=weight)
    draw.rounded_rectangle((48, 425, 864, 818), radius=18, fill="white")
    text(78, 450, "모모바이오", 34, weight="Bold")
    text(78, 510, "12,450", 62, weight="Bold")
    text(350, 546, "+450 (+3.75%)", 28, "#d9556b", "Bold")
    text(78, 610, "주문 가능 금액   1,250,000원", 25)
    text(78, 657, "보유 수량   12주     평균 매입가   11,900원", 22)
    draw.rounded_rectangle((78, 720, 422, 785), radius=12, fill="#259de1")
    draw.rounded_rectangle((442, 720, 834, 785), radius=12, fill="#eaf2f8")
    text(209, 732, "매수", 28, "white", "Bold")
    text(594, 732, "매도", 28, "#315775", "Bold")
    draw.rounded_rectangle((888, 425, 1392, 818), radius=18, fill="white")
    text(920, 452, "오늘의 뉴스", 30, weight="Bold")
    text(920, 518, "신제품 개발 성과 공식 발표", 25, weight="Bold")
    text(920, 564, "호재는 확인됐다. 지금 가격에도", 22)
    text(920, 601, "투자할 만할지는 직접 판단하자.", 22)
    text(920, 682, "컨디션  양호", 24, "#1788bb", "Bold")
    text(920, 735, "편의점 알바 · 밤의 기록", 22)
    text(56, 854, "숫자·기호 확인   0O 1Il  1,234,567원  -12.50%  +0.01%  /  :  ( )", 25)
    text(56, 914, "작은 본문 예시: 소문의 출처와 게시 시간을 확인하고 차트의 움직임을 살펴보세요.", 20)
    text(56, 975, "서체 제공: 경기도 · 경기천년제목  |  레이아웃은 본 프로젝트의 독자 시안", 18, "#70899a")
    target = ROOT / "docs/previews/font-preview.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    img.save(target)
    print(f"Preview: {target}")


if __name__ == "__main__":
    main()
