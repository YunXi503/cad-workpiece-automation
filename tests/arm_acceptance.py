"""Exercise shell evidence, engineering inference and multi-bend CAD delivery."""
import json
import math
from pathlib import Path
import tempfile

from acceptance import command, read, sha


def nominal_distance(point, curves):
    # Independent finite-primitive evaluator, using authored truth only after solving.
    px, py = point["x"], point["y"]
    distances = []
    for c in curves:
        a, b, o = c["start"], c["end"], c["center"]
        if o is None:
            dx, dy = b["x"] - a["x"], b["y"] - a["y"]
            t = max(0, min(1, ((px - a["x"]) * dx + (py - a["y"]) * dy) / (dx * dx + dy * dy)))
            distances.append(math.hypot(px - a["x"] - t * dx, py - a["y"] - t * dy))
        else:
            angle = (math.atan2(py - o["y"], px - o["x"]) - math.atan2(a["y"] - o["y"], a["x"] - o["x"])) % (2 * math.pi)
            r = math.hypot(a["x"] - o["x"], a["y"] - o["y"])
            distances.append(abs(math.hypot(px - o["x"], py - o["y"]) - r) if angle <= c["sweep"] else
                             min(math.hypot(px - a["x"], py - a["y"]), math.hypot(px - b["x"], py - b["y"])))
    return min(distances)


def main():
    with tempfile.TemporaryDirectory(prefix="suspension arm ") as temp:
        root = Path(temp)
        results = {}
        for case in ("arm", "arm-b", "arm-gap", "arm-height", "arm-switch", "arm-reversed"):
            source, output = root / (case + " input"), root / (case + " output")
            command("generate", case, source)
            design = read(source / "design.json")
            (source / "design.json").unlink()
            # The run must succeed with generator roles and nominal curves physically absent.
            command("run", source, output)
            report = json.loads(command("verify", source, output)[0])
            run = read(output / "run.json")
            results[case] = report, run, read(output / "cad-plan.json")
            if case in ("arm", "arm-b", "arm-gap", "arm-height", "arm-reversed"):
                assert all(s["status"] == "checked" and s["baselineCount"] == 6 and s["reducedCount"] == 5
                           and s["roundedXYError"] <= .3 and s["movementBound"] <= .05 for s in report["sides"])
                assert all(c["cover"] == "upper" for side in run["sides"] for c in side["stations"])
            if case in ("arm", "arm-b", "arm-gap"):
                for side, truth in zip(run["sides"], design["nominal"], strict=True):
                    assert max(nominal_distance(row["point"], truth) for row in side["stations"]) < .1
                    assert all(row["triangles"] for row in side["stations"] if row["state"] == "observed")
                    assert all(not row["triangles"] and row["sourceState"] in ("gap", "ambiguous")
                               for row in side["stations"] if row["state"] == "inferred")
            if case == "arm":
                assert sum(s["inferred"] for s in report["sides"]) > 0
                replay = root / "arm replay"
                command("run", source, replay)
                assert all(sha(output / name) == sha(replay / name) for name in
                           ("run.json", "cad-plan.json", "verification.json", "overlay.svg"))
                section = json.loads(command("section", source, "arm-1", 68)[0])
                assert section["rawSupport"]["state"] == "ambiguous" and len(section["components"]) > 1
                assert len(section["shell"]["candidates"]) > 2
                section = json.loads(command("section", source, "arm-2", 40)[0])
                assert len(section["shell"]["candidates"]) == 2
                assert section["rawSupport"]["cover"] == "upper"
                assert section["rawSupport"]["height"] != 0
                run_file = output / "run.json"
                saved = run_file.read_bytes()
                tampered = json.loads(saved)
                tampered["sides"][1]["stations"][40]["height"] += .2
                run_file.write_text(json.dumps(tampered), encoding="utf-8")
                _, reason = command("verify", source, output, code=1)
                assert "source identities" in reason
                run_file.write_bytes(saved)

        assert results["arm"][2]["sides"] == results["arm-height"][2]["sides"]
        assert all(s["contactState"] == "exceeds-sampled-limit" for s in results["arm-height"][0]["sides"])
        assert all(s["contactState"] == "within-sampled-limit" for s in results["arm"][0]["sides"])
        assert all(c["sweep"] < 0 for side in results["arm-reversed"][2]["sides"] for c in side["curves"] if c["center"])
        switched = results["arm-switch"][1]
        assert {row["cover"] for row in switched["sides"][1]["stations"]} == {"upper", "lower"}
        assert any(s["status"] == "withheld" for s in results["arm-switch"][0]["sides"])

    print("6 arm cases passed: no design labels, source-height tamper, negative sweeps, inference, multi-bend rounding and height control.")


if __name__ == "__main__":
    main()
