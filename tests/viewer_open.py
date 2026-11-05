"""Exercise the Windows viewer's shared report-opening path and real painting."""
import json
from pathlib import Path
import subprocess
import tempfile

from acceptance import DOTNET, ROOT, command

VIEWER = ROOT / "src/Viewer/bin/Release/net10.0-windows/Viewer.dll"


def main():
    with tempfile.TemporaryDirectory(prefix="portfolio viewer ") as temp:
        root = Path(temp)
        png = root / "preview.png"

        def open_report(report, index=0):
            return subprocess.run([DOTNET, str(VIEWER), "--smoke", str(report), str(png), str(index)],
                                  capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=30)

        missing = root / "missing.json"
        broken = root / "truncated.json"
        broken.write_text('{"schema":', encoding="utf-8")
        null_side = root / "null-side.json"
        null_side.write_text(json.dumps({"schema": "portfolio-run/1", "case": "normal",
                                         "inputFingerprint": "synthetic", "sides": [None]}), encoding="utf-8")
        for report in (missing, broken, null_side):
            result = open_report(report)
            assert result.returncode == 1 and result.stderr.startswith("Cannot open run report:"), result
            assert "Unhandled exception" not in result.stderr and not png.exists(), result

        for case, index in (("normal", 0), ("gap", 1), ("workpiece", 1), ("arm", 0)):
            source, output = root / f"{case} input", root / f"{case} output"
            command("generate", case, source)
            command("run", source, output)
            result = open_report(output / "run.json", index)
            assert result.returncode == 0 and png.read_bytes().startswith(b"\x89PNG\r\n\x1a\n"), result
            png.unlink()

        source, output = root / "reviewed input", root / "reviewed output"
        command("source", "generate", source)
        command("source", "run", source / "case.json", output)
        valid = json.loads((output/"run.json").read_text())
        # Exercise nested JSON elements that nullable annotations cannot reject on their own.
        invalid = [
            ("source-null", lambda d: d["sides"][0].update(sourceSupports=[None])),
            ("frame-null", lambda d: d["sides"][0].update(sourceFrames=[None])),
            ("fit-null", lambda d: d["sides"][0]["fit"].update(curves=[None])),
            ("baseline-null", lambda d: d["sides"][0]["fit"].update(baseline=[None])),
            ("fit-empty", lambda d: d["sides"][0]["fit"].update(curves=[])),
            ("rounded-empty", lambda d: d["sides"][0]["rounded"].update(curves=[])),
        ]
        for name, mutate in invalid:
            data = json.loads(json.dumps(valid))
            mutate(data)
            report = root/f"{name}.json"
            report.write_text(json.dumps(data), encoding="utf-8")
            result = open_report(report)
            assert result.returncode == 1 and result.stderr.startswith("Cannot open run report:"), result
            assert "Unhandled exception" not in result.stderr and not png.exists(), result
        for index in range(3):
            result = open_report(output / "run.json", index)
            assert result.returncode == 0 and png.read_bytes().startswith(b"\x89PNG\r\n\x1a\n"), result
            assert "pre-round fit XY" in result.stdout and "display: rounded" in result.stdout, result
            png.unlink()

    print("Windows viewer: 9 invalid reports rejected, 7 valid sides painted; source views explicitly identify pre-round errors and displayed rounded geometry.")


if __name__ == "__main__":
    main()
