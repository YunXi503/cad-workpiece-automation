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

        for case, index in (("normal", 0), ("gap", 1)):
            source, output = root / f"{case} input", root / f"{case} output"
            command("generate", case, source)
            command("run", source, output)
            result = open_report(output / "run.json", index)
            assert result.returncode == 0 and png.read_bytes().startswith(b"\x89PNG\r\n\x1a\n"), result
            png.unlink()

    print("Windows viewer: 3 invalid reports rejected, 2 valid reports painted.")


if __name__ == "__main__":
    main()
