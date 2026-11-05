"""Run portable acceptance against the built CLI using a new temporary workspace."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[1]
DOTNET = os.environ.get("PORTFOLIO_DOTNET", "dotnet")
CLI = ROOT / "src/Cli/bin/Release/net10.0/Cli.dll"

def command(*args, code=0):
    process = subprocess.run([DOTNET, str(CLI), *map(str, args)], text=True,
                             capture_output=True, encoding="utf-8", errors="replace")
    assert process.returncode == code, (args, process.returncode, process.stdout, process.stderr)
    return process.stdout, process.stderr

def read(path):
    return json.loads(path.read_text(encoding="utf-8"))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def check_vsta_evidence():
    evidence_dir = ROOT / "docs/evidence"
    receipt = read(evidence_dir / "vsta-editor.json")
    plan_path = evidence_dir / "vsta-synthetic-plan.json"
    plan = read(plan_path)
    assert receipt["planSha256"] == sha(plan_path)
    workflow = (ROOT / "macro/MacroWorkflow.cs").read_text(encoding="utf-8-sig").rstrip()
    assert receipt["workflowRevision"] == "233ab7a36eace2b6ab33187c348ea266e915335f"
    assert receipt["workflowSha256"] == "b18efbac8b9e4a7208856d08b569218940c1ca525cd3cfa1d924105f036e6eea"
    rows, surfaces = receipt["sketchReadbacks"], receipt["surfaces"]
    assert len(rows) == surfaces["sketchCount"] == surfaces["surfaceCount"] == len(plan["sides"]) == 3
    assert sorted(row["curves"] for row in rows) == sorted(len(side["curves"]) for side in plan["sides"])
    assert receipt["readbackLimitMm"] == 1e-7 and all(0 <= row["maxResidual"] <= 1e-7 for row in rows)
    assert sum(row["activeConstraints"] for row in rows) == 15
    assert surfaces["zMin"] == plan["zMin"] == plan["planeZ"] - surfaces["negativeOffset"]
    assert surfaces["zMax"] == plan["zMax"] == plan["planeZ"] + surfaces["positiveOffset"]
    for key in ("a", "b"):
        receipt = read(evidence_dir / f"arm-vsta-{key}.json")
        plan_path = evidence_dir / f"arm-plan-{key}.json"
        plan = read(plan_path)
        assert receipt["planSha256"] == sha(plan_path)
        assert receipt["workflowSha256"] == hashlib.sha256(workflow.encode()).hexdigest()
        assert receipt["host"]["entry"] == "CATMain" and receipt["host"]["process"] == "DSYVstaDebugHost"
        rows = receipt["sketchReadbacks"]
        assert [row["side"] for row in rows] == [side["id"] for side in plan["sides"]]
        assert len(rows) == receipt["surfaces"]["sketchCount"] == receipt["surfaces"]["surfaceCount"] == 3
        assert all(row["curves"] == len(side["curves"]) == 5 for row, side in zip(rows, plan["sides"], strict=True))
        assert sum(row["activeConstraints"] for row in rows) == 30
        assert receipt["readbackLimitMm"] == 1e-7 and all(0 <= row["maxResidual"] <= 1e-7 for row in rows)

def main():
    check_vsta_evidence()
    with tempfile.TemporaryDirectory(prefix="portfolio cold run ") as temp:
        root = Path(temp)
        all_cases = {}
        for case in ("normal", "gap", "height", "reversed", "competing", "workpiece", "workpiece-height"):
            source, first, second = [root / f"{case} {step}" for step in ("input", "first", "replay")]
            command("generate", case, source)
            command("run", source, first)
            command("run", source, second)
            report, _ = command("verify", source, first)
            all_cases[case] = json.loads(report)["sides"]
            if case == "competing":
                rows = read(first / "run.json")["sides"][1]["stations"]
                assert any(row["state"] == "ambiguous" for row in rows)
            for name in ("run.json", "cad-plan.json", "verification.json", "overlay.svg"):
                assert sha(first / name) == sha(second / name), (case, name)
        assert all(side["reducedCount"] == 3 and side["baselineCount"] == 6 for side in all_cases["normal"])
        assert all(side["status"] == "checked" for side in all_cases["reversed"])
        assert all_cases["gap"][1]["status"] == "withheld" and all_cases["gap"][1]["unresolved"] >= 2
        assert sum(s["status"] == "checked" for s in all_cases["gap"]) == 2
        assert all(s["roundedXYError"] <= .3 and s["contactSampleMaximum"] >= .7
                   and s["contactState"] == "exceeds-sampled-limit" for s in all_cases["height"])

        workpiece, shortened = root / "workpiece input", root / "workpiece-height input"
        for file in ("guide.asc", "scan.stl"):
            assert sha(workpiece / file) == sha(shortened / file)
        workpiece_job, shortened_job = read(workpiece / "job.json"), read(shortened / "job.json")
        assert shortened_job == dict(workpiece_job, case="workpiece-height", zMin=2.7)
        original_cad = read(root / "workpiece first" / "cad-plan.json")
        shortened_cad = read(root / "workpiece-height first" / "cad-plan.json")
        assert shortened_cad["sides"] == original_cad["sides"]
        assert [side["id"] for side in original_cad["sides"]] == ["edge-a", "edge-c"]
        a, b, c = all_cases["workpiece"]
        assert (a["observed"], b["observed"], c["observed"]) == (104, 57, 66)
        assert b["unresolved"] == 13 and b["status"] == "withheld" and c["inferred"] == 1
        assert all(row["roundedXYError"] <= .3 and row["contactState"] == "within-sampled-limit" for row in (a, c))
        assert all_cases["workpiece-height"][1]["status"] == "withheld"
        for index in (0, 2):
            row = all_cases["workpiece-height"][index]
            assert row["contactSampleMaximum"] >= .7 and row["contactState"] == "exceeds-sampled-limit"
        stations = read(root / "workpiece first" / "run.json")["sides"]
        assert sum(s["state"] == "gap" for s in stations[1]["stations"]) == 4
        assert sum(s["state"] == "ambiguous" for s in stations[1]["stations"]) == 9
        section_text = command("section", workpiece, "edge-b", 29)[0]
        section = json.loads(section_text)
        assert len(section["components"]) == 2 and len(section["rawSupport"]["triangles"]) == 4
        assert section["rawSupport"] == stations[1]["stations"][29]
        section = json.loads(command("section", workpiece, "edge-c", 9)[0])
        assert section["rawSupport"]["state"] == "gap" and stations[2]["stations"][9]["state"] == "inferred"
        _, reason = command("section", workpiece, "edge-b", 70, code=1)
        assert "section index" in reason
        section_file, rendered = root / "b29.json", root / "workpiece.svg"
        section_file.write_text(section_text, encoding="utf-8")
        subprocess.run([sys.executable, str(ROOT / "tools/render_workpiece.py"), str(workpiece),
                        str(root / "workpiece first"), str(root / "workpiece-height first"),
                        str(section_file), str(rendered)], check=True)
        labels = " ".join(ElementTree.parse(rendered).getroot().itertext())
        assert "2,914 STL triangles" in labels and "241 section stations" in labels and "0.7001 mm" in labels
        source, output = root / "normal input", root / "normal first"
        receipt = output / "cad-plan.json"
        cad = read(receipt)
        original = receipt.read_text(encoding="utf-8")
        cad["sides"][1]["curves"][1]["sweep"] *= -1
        receipt.write_text(json.dumps(cad), encoding="utf-8")
        _, reason = command("verify", source, output, code=1)
        assert "CAD geometry differ" in reason
        receipt.write_text(original, encoding="utf-8")
        run_file = output / "run.json"
        original_run = run_file.read_text(encoding="utf-8")
        run = read(run_file)
        run["sides"][0]["stations"][0]["point"]["x"] += .2
        run_file.write_text(json.dumps(run), encoding="utf-8")
        _, reason = command("verify", source, output, code=1)
        assert "section plane" in reason

        run = json.loads(original_run)
        source_ids = run["sides"][0]["stations"][0]["triangles"]
        source_ids.append(source_ids[0])
        run_file.write_text(json.dumps(run), encoding="utf-8")
        _, reason = command("verify", source, output, code=1)
        assert "source identities differ" in reason

        # Change all three reports together. Agreement among edited artifacts is not source evidence.
        report_file = output / "verification.json"
        original_report = report_file.read_text(encoding="utf-8")
        cad = json.loads(original)
        cad["sides"].pop(0)
        receipt.write_text(json.dumps(cad), encoding="utf-8")
        for fabricated_gap in (True, False):
            run = json.loads(original_run)
            side = run["sides"][0]
            side.update(fit=None, rounded=None, issue="withheld")
            report = json.loads(original_report)
            row = report["sides"][0]
            row.update(baselineCount=0, reducedCount=0, originalError=None, uniformError=None,
                       roundedXYError=None, movementBound=None, endpointResidual=None,
                       jointResidual=None, tangentResidual=None, contactSampleMaximum=None,
                       contactState="withheld", status="withheld")
            if fabricated_gap:
                side["stations"][0].update(state="gap", point=None, triangles=[], cover=None)
                row.update(observed=row["observed"] - 1, unresolved=1)
            run_file.write_text(json.dumps(run), encoding="utf-8")
            report_file.write_text(json.dumps(report), encoding="utf-8")
            _, reason = command("verify", source, output, code=1)
            expected = "state or source identities differ" if fabricated_gap else "Withheld side or its reason differs"
            assert expected in reason
        receipt.write_text(original, encoding="utf-8")
        run_file.write_text(original_run, encoding="utf-8")
        report_file.write_text(original_report, encoding="utf-8")

        job = source / "job.json"
        original_job = job.read_bytes()
        for bad_guides in (None, [None]):
            config = json.loads(original_job)
            config["guides"] = bad_guides
            job.write_text(json.dumps(config), encoding="utf-8")
            bad_output = root / "invalid job"
            _, reason = command("run", source, bad_output, code=1)
            assert "guide" in reason.lower() or "synthetic job contract" in reason
            assert "Unhandled exception" not in reason and not bad_output.exists()
        job.write_bytes(original_job)

        _, reason = command("generate", "normal", source, code=1)
        assert "fresh directory" in reason

        preview = output / "overlay.svg"
        svg = preview.read_text(encoding="utf-8")
        preview.write_text(svg, encoding="utf-8", newline="\r\n")
        command("verify", source, output)
        preview.write_text("<svg/>", encoding="utf-8")
        _, reason = command("verify", source, output, code=1)
        assert "Preview overlay differs" in reason
        preview.unlink()
        _, reason = command("verify", source, output, code=1)
        assert "overlay.svg" in reason
        preview.write_text(svg, encoding="utf-8", newline="\n")

        results = json.loads(command("experiment", "check-all")[0])
        listed = {row.split("\t")[0] for row in command("experiment", "list")[0].splitlines()}
        assert all(r["reproduced"] for r in results) and len(listed) == len(results)
        for result in results:
            individual = json.loads(command("experiment", "run", result["id"])[0])
            assert individual == result
        print(f"{len(all_cases)} input cases and deterministic replays, 11 negative controls, {len(results)} experiments passed.")

if __name__ == "__main__":
    main()
