"""Exercise actual async desktop generation, CLI parity, output protection and review states."""
import hashlib
import json
import math
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

from acceptance import DOTNET, ROOT, command

VIEWER = ROOT/"src/Viewer/bin/Release/net10.0-windows/Viewer.dll"
FILES = ("source-run.json", "run.json", "cad-plan.json", "overlay.svg")


def snapshot(folder):
    return {p.relative_to(folder).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in folder.rglob("*") if p.is_file()}


def main():
    with tempfile.TemporaryDirectory(prefix="portfolio workflow ") as temp:
        root = Path(temp)
        operation = 0
        def run(mode, manifest, output, code=0):
            nonlocal operation
            operation += 1
            png = root/(str(operation)+"-"+output.name+".png")
            result = subprocess.run([DOTNET, str(VIEWER), mode, str(manifest), str(output), str(png)],
                                    capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
            assert result.returncode == code, (result.returncode, result.stdout, result.stderr)
            if code == 1:
                assert result.stderr.startswith("Cannot build draft:") and not png.exists(), result
            else:
                assert png.read_bytes().startswith(b"\x89PNG\r\n\x1a\n")
                assert int(re.search(r"UI work ticks: (\d+)", result.stdout)[1]) > 0, result
            return result

        workspace = root/"demo"
        run("--demo-smoke", "unused", workspace)
        source, gui = workspace/"input", workspace/"output"
        receipt = json.loads((gui/"workflow-run.json").read_text())
        assert receipt["state"] == "draft-created" and receipt["numericStatus"] == "passed"
        assert receipt["cadSides"] == 3 and math.isfinite(receipt["automatedElapsedSeconds"]) and receipt["automatedElapsedSeconds"] > 0
        assert not receipt["nativeCadExecuted"] and not receipt["engineerTimeSavingMeasured"]
        original = snapshot(source)
        cli = root/"cli"
        command("source", "run", source/"case.json", cli)
        again = root/"second-gui"
        run("--build-smoke", source/"case.json", again)
        for name in FILES:
            assert (gui/name).read_bytes() == (cli/name).read_bytes() == (again/name).read_bytes(), name
        assert snapshot(source) == original
        before = snapshot(cli)
        run("--build-smoke", source/"case.json", cli, 1)
        assert snapshot(cli) == before
        nested = source/"unsafe-output"
        run("--build-smoke", source/"case.json", nested, 1)
        assert not nested.exists() and snapshot(source) == original
        run("--demo-smoke", "unused", workspace, 1)
        assert snapshot(source) == original

        bad = root/"bad-input"
        shutil.copytree(source, bad)
        manifest = bad/"case.json"
        data = json.loads(manifest.read_text()); data["mesh"]["sha256"] = "0"*64
        manifest.write_text(json.dumps(data), encoding="utf-8")
        bad_before = snapshot(bad)
        failed = root/"failed"
        run("--build-smoke", manifest, failed, 1)
        assert snapshot(bad) == bad_before and not (failed/"cad-plan.json").exists()
        assert json.loads((failed/"workflow-run.json").read_text())["state"] == "failed"

        # A non-centimeter straight span cannot round its length while fixing both ends.
        # Existing Core retains explicit unrounded geometry; desktop must expose that stage.
        unrounded = root/"unrounded-input"
        shutil.copytree(source, unrounded)
        data = json.loads((unrounded/"case.json").read_text())
        data["references"][0]["endMm"] = 8.1234
        (unrounded/"case.json").write_text(json.dumps(data), encoding="utf-8")
        reviewed = root/"unrounded-output"
        run("--build-smoke", unrounded/"case.json", reviewed)
        state = json.loads((reviewed/"workflow-run.json").read_text())
        report = json.loads((reviewed/"source-verification.json").read_text())
        assert state["unroundedSides"] == 1 and report["sides"][0]["roundingState"] == "withheld"
        assert json.loads((reviewed/"run.json").read_text())["sides"][0]["geometryStage"] == "unrounded-engineering-draft"
    print("Desktop workflow: demo/build responsive; four CLI files byte-identical; source immutable; four bad operations rejected; unrounded stage explicit.")


if __name__ == "__main__":
    main()
