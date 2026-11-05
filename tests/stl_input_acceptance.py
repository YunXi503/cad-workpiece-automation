"""Exercise STL parsing through the real CLI, including ambiguous binary headers."""
import json
import os
from pathlib import Path
import struct
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DOTNET = os.environ.get("PORTFOLIO_DOTNET", "dotnet")
CLI = ROOT / "src/Cli/bin/Release/net10.0/Cli.dll"
TRIANGLES = [((-1, 0, 0), (5, 0, 0), (5, 0, 4)), ((-1, 0, 0), (5, 0, 4), (-1, 0, 4))]


def binary(triangles=TRIANGLES, header=b"binary fixture"):
    raw = header.ljust(80, b" ") + struct.pack("<I", len(triangles))
    for triangle in triangles:
        raw += struct.pack("<12fH", 0, 0, 0, *(x for p in triangle for x in p), 0)
    return raw


def ascii_stl():
    rows = ["solid authored_fixture"]
    for triangle in TRIANGLES:
        rows += ["facet normal 0 0 0", "outer loop"]
        rows += ["vertex " + " ".join(str(x) for x in p) for p in triangle]
        rows += ["endloop", "endfacet"]
    return "\n".join(rows + ["endsolid authored_fixture", ""])


with tempfile.TemporaryDirectory(prefix="portfolio STL input ") as temp:
    folder = Path(temp)
    (folder / "guide.asc").write_text("".join(f"{i} 0 0\n" for i in range(5)), encoding="utf-8")
    (folder / "job.json").write_text(json.dumps({"schema": "synthetic-job/1", "case": "input-controls",
        "guides": [{"id": "sample", "start": 0, "count": 5, "cover": "cover-A"}],
        "probeZ": 2, "planeZ": 2, "zMin": 0, "zMax": 4}), encoding="utf-8")

    def section(content, expected=0):
        (folder / "scan.stl").write_bytes(content)
        result = subprocess.run([DOTNET, str(CLI), "section", str(folder), "sample", "2"],
                                capture_output=True, text=True, encoding="utf-8", errors="replace")
        assert result.returncode == expected, (result.returncode, result.stdout, result.stderr)
        return result.stdout

    text = ascii_stl()
    baseline = section(text.encode())
    assert json.loads(baseline)["rawSupport"]["state"] == "observed"
    assert section(binary()) == baseline
    assert section(binary(header=b"solid misleading binary header")) == baseline
    assert section(("\n\n" + text.replace("\n", "\n  \n")).encode()) == baseline
    controls = [binary()[:-1], binary() + b"x", binary([]),
                binary().replace(struct.pack("<I", 2), struct.pack("<I", 3), 1),
                text.replace("outer loop", "outer bad", 1).encode(),
                text.replace("vertex -1 0 0", "vertex NaN 0 0", 1).encode(),
                text.replace("vertex -1 0 0", "vertex 0 0", 1).encode(),
                text.replace("endsolid authored_fixture\n", "").encode(),
                (text + "trailing payload\n").encode()]
    for value in (float("nan"), float("inf"), 100001):
        bad = [[list(p) for p in t] for t in TRIANGLES]
        bad[0][0][0] = value
        controls.append(binary(bad))
    for content in controls:
        section(content, expected=1)
    print(f"STL input passed: 4 equivalent formats / {len(controls)} rejected malformed inputs")
