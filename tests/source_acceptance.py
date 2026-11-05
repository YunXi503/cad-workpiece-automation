"""Clean-directory reviewed-source replay and adversarial source/geometry controls.

All inputs here are independently generated mathematical fixtures. No private Case A
file is needed, nor does this test certify a physical wall or manufacturing contact.
"""
import copy
import hashlib
import json
from pathlib import Path
import tempfile

from acceptance import command


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    rejected = 0
    with tempfile.TemporaryDirectory(prefix="reviewed source replay ") as temp:
        root = Path(temp)
        source, first, second = root / "input", root / "first", root / "second"
        command("source", "generate", source)
        manifest = source / "case.json"
        original = json.loads(manifest.read_text())
        command("source", "run", manifest, first)
        report = json.loads(command("source", "verify", manifest, first)[0])
        assert report["originalFrames"] == 66 and report["reviewExcludedFrames"] == 3
        assert report["sourceSupportEndpoints"] == 126 and not report["physicalContactAccepted"]
        assert len(report["sides"]) == 3
        for side in report["sides"]:
            assert side["reducedCount"] < side["baselineCount"]
            assert side["cadXYMaximum"] <= .3 and side["roundingMovementBoundMm"] <= .05
            assert max(side[k] for k in ("endpointResidualMm", "jointResidualMm", "unitTangentDifference")) <= 1e-7
        command("source", "run", manifest, second)
        for path in first.iterdir():
            assert digest(path) == digest(second / path.name), path.name
        summary = root / "public.json"
        command("source", "public-report", manifest, first, summary)
        public = summary.read_text()
        assert str(root) not in public and '"point"' not in public and '"frames"' not in public
        assert json.loads(public)["cadPlanSha256"] == digest(first / "cad-plan.json")

        mutations = []
        def mutation(fn):
            data = copy.deepcopy(original)
            fn(data)
            mutations.append(data)
        mutation(lambda d: d["mesh"].update(sha256="0" * 64))
        mutation(lambda d: d["references"][0]["points"].update(sha256="0" * 64))
        mutation(lambda d: d["frames"][0].update(offset=d["frames"][0]["offset"] + 1))
        mutation(lambda d: d["frames"][0]["corners"][0]["cap"]["faces"].__setitem__(0, 0))
        mutation(lambda d: d["frames"][0]["corners"][0]["wall"]["first"].update(z=40))
        mutation(lambda d: d["frames"][0]["corners"][0]["wall"]["anchor"].update(x=40))
        mutation(lambda d: d["references"][0].update(endMm=-1))
        mutation(lambda d: d["references"].__setitem__(0, None))
        mutation(lambda d: d["frames"].__setitem__(0, None))
        mutation(lambda d: d["frames"][0]["corners"].__setitem__(0, None))
        mutation(lambda d: d.update(frames=None))
        for index, value in enumerate(mutations):
            manifest.write_text(json.dumps(value), encoding="utf-8")
            command("source", "audit", manifest, root / f"bad-{index}", code=1)
            rejected += 1
        # Reused cap identity must become unresolved, rather than silently choose a larger width.
        reused = copy.deepcopy(original)
        reused["frames"][0]["corners"][1]["cap"] = reused["frames"][0]["corners"][0]["cap"]
        manifest.write_text(json.dumps(reused), encoding="utf-8")
        command("source", "audit", manifest, root / "reuse")
        audit = json.loads((root / "reuse/source-audit.json").read_text())
        assert audit["frames"][0]["state"] == "unresolved"
        manifest.write_text(json.dumps(original, indent=2) + "\n", encoding="utf-8")
        # Restore identical generated manifest bytes before replay; fingerprint binds exact input.
        regenerated = root / "regenerated"
        command("source", "generate", regenerated)
        manifest.write_bytes((regenerated / "case.json").read_bytes())
        for filename in ("cad-plan.json", "run.json", "source-run.json", "overlay.svg"):
            target = first / filename
            old = target.read_bytes()
            if filename.endswith("json"):
                data = json.loads(old)
                data["schema"] = "tampered"
                target.write_text(json.dumps(data), encoding="utf-8")
            else:
                target.write_text("<svg/>", encoding="utf-8")
            command("source", "verify", manifest, first, code=1)
            target.write_bytes(old)
            rejected += 1
        command("source", "verify", manifest, first)
    print(f"Reviewed source: deterministic replay, 3 rounded sides, provenance guard, {rejected} rejected controls.")


if __name__ == "__main__":
    main()
