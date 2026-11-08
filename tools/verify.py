"""Run the public build and acceptance suite, preserving a local result for every step."""
import datetime
import json
import os
from pathlib import Path
import platform
import subprocess
import sys
import time
import uuid

ROOT = Path(__file__).resolve().parents[1]


def main():
    dotnet = os.environ.get("PORTFOLIO_DOTNET", "dotnet")
    out = ROOT / "artifacts" / ("verification-" + uuid.uuid4().hex[:10])
    out.mkdir(parents=True)
    steps = [("cli-build", [dotnet, "build", "src/Cli", "-c", "Release", "--nologo"]),
             ("geometry-math", [dotnet, "run", "--project", "tests/GeometryChecks", "-c", "Release"])]
    for name in ["stl_input_acceptance", "source_acceptance", "case_a_evidence", "portfolio_consistency", "surface_display", "acceptance", "arm_acceptance", "check_docs"]:
        steps.append((name, [sys.executable, f"tests/{name}.py"]))
    steps.append(("public-tree", [sys.executable, "tools/audit_publication.py"]))
    if platform.system() == "Windows":
        steps += [("viewer-build", [dotnet, "build", "src/Viewer", "-c", "Release", "--nologo"]),
                  ("viewer-open", [sys.executable, "tests/viewer_open.py"]),
                  ("viewer-workflow", [sys.executable, "tests/viewer_workflow.py"])]
    rows = []
    for name, command in steps:
        print(f"Running {name}", flush=True)
        start = time.monotonic()
        with (out / (name + ".log")).open("w", encoding="utf-8") as log:
            result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
        rows.append({"step": name, "exitCode": result.returncode, "elapsedSeconds": round(time.monotonic()-start, 3)})
        if result.returncode:
            print((out / (name + ".log")).read_text(encoding="utf-8"))
            break
    report = {"schema": "public-acceptance-run/1", "measuredUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
              "os": platform.system(), "python": platform.python_version(), "steps": rows,
              "complete": len(rows) == len(steps), "passed": len(rows) == len(steps) and all(r["exitCode"] == 0 for r in rows),
              "scope": "public fixtures, aggregate receipt consistency and file audit; no native host or manufacturing claims"}
    (out / "summary.json").write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
    print(out)
    return 0 if report["passed"] else 1


if __name__ == "__main__": sys.exit(main())
