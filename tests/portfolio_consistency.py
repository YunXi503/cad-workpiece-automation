"""Check historical method/document bindings and the complete historical image gallery.

This checks publication consistency, not native execution or engineering acceptance.
acceptance.py also supplies the actual current CLI results to verify_history_results.
"""
import hashlib
import json
import math
from pathlib import Path
import re
import struct

ROOT = Path(__file__).resolve().parents[1]


def read(name):
    return json.loads((ROOT / "docs/evidence" / name).read_text(encoding="utf-8"))


def display(value):
    try:
        return format(float(value), ".6g")
    except ValueError:
        return value


def verify_history_results(results):
    recorded = read("history-replay-20261108.json")
    expected = {r["id"]: r for r in recorded["results"]}
    assert len(results) == len(expected) == 25
    assert {r["id"] for r in results} == set(expected)
    tolerance = recorded["numericDisplayComparison"]
    assert tolerance["relativeTolerance"] == 1e-9 and tolerance["absoluteTolerance"] == 1e-12
    # Log comparison allows platform rounding; the C# example's own pass/fail
    # predicate and all geometry acceptance thresholds remain untouched.
    for actual in results:
        saved = expected[actual["id"]]
        assert actual["reproduced"] and saved["reproduced"]
        assert actual["title"] == saved["title"] and actual["mechanism"] == saved["mechanism"]
        values = {r["key"]: r["value"] for r in actual["observations"]}
        reference = {r["key"]: r["value"] for r in saved["observations"]}
        assert len(values) == len(actual["observations"]) and values.keys() == reference.keys()
        for key, value in values.items():
            try:
                a, b = float(value), float(reference[key])
            except ValueError:
                assert value == reference[key], (actual["id"], key)
            else:
                assert math.isfinite(a) and math.isfinite(b)
                assert math.isclose(a, b, rel_tol=tolerance["relativeTolerance"],
                                    abs_tol=tolerance["absoluteTolerance"]), (actual["id"], key)


def check_history_documents():
    receipt = read("history-replay-20261108.json")
    source = (ROOT / "src/Core/Experiments.cs").read_text(encoding="utf-8")
    entries = {m[1]: source[:m.start()].count("\n") + 1
               for m in re.finditer(r'Make\("([^\"]+)"', source)}
    results = {r["id"]: r for r in receipt["results"]}
    assert len(results) == 25 and set(results) == set(entries) == set(receipt["historicalStatuses"])
    assert {p.stem for p in (ROOT / "docs/history").glob("*.md")} == set(results)
    hashes = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_text(encoding="utf-8-sig").encode()).hexdigest()
              for folder in ("src/Core", "src/Cli") for p in sorted((ROOT / folder).glob("*.cs"))}
    assert hashes == receipt["sourceFilesSha256"], "Historical replay must bind the current numerical source"
    index = (ROOT / "docs/engineering-decisions.md").read_text(encoding="utf-8")
    for method, row in results.items():
        text = (ROOT / "docs/history" / (method + ".md")).read_text(encoding="utf-8")
        assert f"experiment run {method}" in text
        assert f"Experiments.cs#L{entries[method]})" in text, method
        status = re.search(r"\*\*歷史狀態\*\*\s*([^\n]+)", text)[1].strip()
        assert status == receipt["historicalStatuses"][method]
        assert f"| [{method}](history/{method}.md) | {status} |" in index
        assert "## 2026-11-08 公開最小重現的實測" in text
        for item in row["observations"]:
            assert f"| `{item['key']}` | `{display(item['value'])}` |" in text, (method, item["key"])
    verify_history_results(receipt["results"])


def check_gallery():
    gallery = read("historical-surfaces.json")
    rows = gallery["images"]
    assert len(rows) == gallery["imageCount"] == 59
    groups = {r["group"] for r in rows}
    assert len(groups) == gallery["groupCount"] == 18
    assert set(range(1, 20)) - groups == set(gallery["missingSourceGroupNumbers"]) == {10}
    assert len({r["path"] for r in rows}) == 59
    assert sum(r["bytes"] for r in rows) == gallery["publicImageBytes"]
    page = (ROOT / "docs/surface-gallery.md").read_text(encoding="utf-8")
    published = {p.relative_to(ROOT).as_posix() for p in (ROOT / "assets/historical-surfaces").glob("*.png")}
    assert published == {r["path"] for r in rows}
    for row in rows:
        blob = (ROOT / row["path"]).read_bytes()
        assert len(blob) == row["bytes"] and hashlib.sha256(blob).hexdigest() == row["sha256"]
        assert blob[:8] == b"\x89PNG\r\n\x1a\n" and blob[12:16] == b"IHDR"
        assert struct.unpack_from(">II", blob, 16) == (row["width"], row["height"])
        assert not row["pixelsEdited"] and "../" + row["path"] in page
    assert not gallery["exactHistoricalSolverConfirmed"] and not gallery["currentCaseANativePlanIdentityEstablished"]


def main():
    check_history_documents()
    check_gallery()
    print("Portfolio consistency: 25 method IDs/statuses/code entries/output tables and all 59 historical image bindings passed.")


if __name__ == "__main__":
    main()
