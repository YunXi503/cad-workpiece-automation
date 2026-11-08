"""Check public aggregate bindings without requesting or exporting private source geometry.

This is receipt consistency checking, not a replacement for licensed native execution.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    read = lambda name: json.loads((ROOT / "docs/evidence" / name).read_text())
    geometry, native = read("case-a-geometry.json"), read("case-a-vsta.json")
    workflow = (ROOT / "macro/MacroWorkflow.cs").read_text(encoding="utf-8-sig").rstrip()
    assert native["workflowSha256"] == hashlib.sha256(workflow.encode()).hexdigest()
    assert native["planSha256"] == geometry["cadPlanSha256"]
    assert native["host"]["entry"] == "CATMain" and native["host"]["process"] == "DSYVstaDebugHost"
    assert native["host"]["apartment"] == "MTA"
    assert not native["originalFilesPublished"] and not native["physicalContactAccepted"]
    assert not geometry["physicalContactAccepted"] and geometry["originalFrames"] == 83
    assert geometry["engineeringWidthFrames"] + geometry["unresolvedFrames"] + geometry["reviewExcludedFrames"] == 83
    assert sum(s["sourceSupportEndpoints"] for s in geometry["sides"]) == geometry["sourceSupportEndpoints"] == 119
    for g, n in zip(geometry["sides"], native["sketchReadbacks"], strict=True):
        assert g["id"] == n["side"] and g["reducedCount"] == n["curves"] == n["lines"] + n["arcs"]
        assert n["activeConstraints"] == 2*(n["curves"]-1) + n["arcs"]
        assert n["arcs"] == n["positiveSweepArcs"] + n["negativeSweepArcs"]
        assert n["maxResidual"] <= native["readbackLimitMm"] == 1e-7
        assert g["cadXYMaximum"] <= .3 and g["roundingMovementBoundMm"] <= .05
    assert sum(s["curves"] for s in native["sketchReadbacks"]) == 165
    assert sum(s["activeConstraints"] for s in native["sketchReadbacks"]) == 405
    assert native["surfaces"]["surfaceCount"] == native["surfaces"]["sketchCount"] == 3
    viewport = native["nativeViewport"]
    assert hashlib.sha256((ROOT / viewport["path"]).read_bytes()).hexdigest() == viewport["sha256"]
    # New run is kept separate from the historical receipt. Check cross-version
    # bindings and actual exported bytes, not just independently copied totals.
    codisplay = read("case-a-native-codisplay-20261108.json")
    assert codisplay["measuredDate"] == "2026-11-08" > native["measuredDate"]
    for key in ("planSha256", "workflowSha256", "editorSourceSha256", "surfaces", "readbackLimitMm"):
        assert codisplay[key] == native[key], key
    assert codisplay["host"]["process"] == "DSYVstaDebugHost" and codisplay["host"]["apartment"] == "MTA"
    assert codisplay["build"]["succeeded"] and not codisplay["build"]["sourceChangedForThisRun"]
    assert codisplay["postScanCopyReadbackRepeated"] and codisplay["postScanCopyReadbacksMatchF5"]
    for before, after in zip(native["sketchReadbacks"], codisplay["sketchReadbacks"], strict=True):
        assert before["side"] == after["name"]
        assert all(before[key] == after[key] for key in ("curves", "activeConstraints", "maxResidual"))
    scan = codisplay["scan"]
    assert scan["meshSha256"] == geometry["inputFingerprint"].split(":")[1]
    assert scan["triangleCount"] == geometry["originalTriangles"]
    assert scan["sourceAndCopyNativeStatisticsMatch"] and scan["localMeshBoundsMatchNativeDisplay"]
    assert not scan["geometryTransformApplied"] and not scan["fullNativeTriangleCoordinateComparisonPerformed"]
    assert codisplay["nativeCoDisplayEstablished"]
    assert len(codisplay["nativeViewports"]) == 2
    assert {v["view"] for v in codisplay["nativeViewports"]} == {"oblique", "reverse"}
    for view in codisplay["nativeViewports"]:
        blob = (ROOT / view["path"]).read_bytes()
        assert blob.startswith(b"\xff\xd8") and hashlib.sha256(blob).hexdigest() == view["sha256"]
        assert view["sha256"] != viewport["sha256"], "Old surface-only image cannot substitute for co-display"
    assert len({v["sha256"] for v in codisplay["nativeViewports"]}) == 2
    assert all(not codisplay[key] for key in ("originalFilesPublished", "plmSaveConfirmed", "reopenVerified",
        "physicalContactAccepted", "engineeringWeldAcceptanceConfirmed", "laborSavingMeasured"))
    print("Nov 8 receipt/image bindings agree; native co-display recorded, full mesh comparison and engineering acceptance remain unverified.")
    controls = read("case-a-native-controls.json")
    assert controls["planSha256"] == native["planSha256"] and controls["workflowSha256"] == native["workflowSha256"]
    assert len(controls["controls"]) == 5 and all(c["exitCode"] == 1 and not c["successReceiptCreated"] for c in controls["controls"])
    linux, windows = read("case-a-linux.json"), read("case-a-windows.json")
    assert linux["originalCaseExitCode"] == 0 and linux["matchesWindowsNumericalSources"]
    hashes = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_text(encoding="utf-8-sig").encode()).hexdigest()
              for folder in ("src/Core", "src/Cli") for p in sorted((ROOT / folder).glob("*.cs"))}
    assert hashes == linux["sourceKernelSha256"] == windows["sourceKernelSha256"]
    assert linux["originalCaseVerification"]["inputFingerprint"] == geometry["inputFingerprint"]
    assert not linux["nativeCadExecutedOnLinux"] and not linux["physicalContactAccepted"]
    for g, l in zip(geometry["sides"], linux["originalCaseVerification"]["sides"], strict=True):
        assert g["id"] == l["id"] and g["reducedCount"] == l["reducedCount"]
        for field in ("cadXYMaximum", "draftXYMaximum", "uniformXYMaximum", "roundingMovementBoundMm", "sourceAnchorFiniteSurfaceMaximumMm"):
            assert abs(g[field] - l[field]) <= 1e-7
    print("Case A aggregate evidence: geometry, current macro, native counts and viewport hashes agree; contact remains unaccepted.")
    # Historical private-log transcription, not a new geometry/native validation.
    history = read("historical-rounding.json")
    assert history["totalSides"] == 9 and history["totalPrimitives"] == 217
    assert not history["nativeExecutionInThisUpdate"] and not history["nativeRoundedRevisionVerified"]
    assert history["historicalNativeAttempt"]["status"] == "blocked-before-geometry"
    rows = [row for case in history["cases"] for row in case["sides"]]
    assert len(rows) == 9 and sum(r["lines"] + r["arcs"] for r in rows) == 217
    assert max(r["movementBoundMmRecorded"] for r in rows) <= .05
    for r in rows:
        assert max(r[k] for k in ("dimensionResidualMmRecorded", "endpointResidualMmRecorded", "jointResidualMmRecorded", "unitTangentDifferenceRecorded", "radialResidualMmRecorded")) <= 1e-7
    print("Historical rounding aggregate: 3 cases / 9 sides / 217 elements; saved-log checks only, no new native claim.")


if __name__ == "__main__": main()
