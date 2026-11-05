"""Convert a local archived support export to the independently validated input schema.

This is a schema adapter, not an automatic physical-wall classifier. It copies private
inputs only to a git-ignored directory (or outside this public checkout). Never commit
the resulting manifest, mesh, references, or native plan.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checked(path, expected):
    if sha(path) != expected:
        raise ValueError("Archived input fingerprint mismatch")
    return path


def prepare(config_path, output, mesh_override=None, case_id="case-a-original"):
    config_path, output = config_path.resolve(), output.resolve()
    if output == ROOT or ROOT in output.parents:
        ignored = subprocess.run(["git", "check-ignore", "--quiet", str(output / "case.json")], cwd=ROOT)
        if ignored.returncode != 0:
            raise ValueError("Private output inside this checkout must be git-ignored before importing")
    if output.exists() and any(output.iterdir()):
        raise ValueError("Use a fresh private output directory")
    config = read(config_path)
    base = config_path.parent
    mesh_name = config["stl"].get("path", config["stl"].get("file"))
    if mesh_override is None and not mesh_name:
        raise ValueError("This export has no portable mesh path; specify --mesh with the unchanged local STL")
    mesh_path = mesh_override or Path(mesh_name)
    if not mesh_path.is_absolute():
        mesh_path = base / mesh_path
    mesh = checked(mesh_path, config["stl"]["sha256"])
    output.mkdir(parents=True, exist_ok=True)
    shutil.copy2(mesh, output / "scan.stl")
    xy = lambda p: {"x": float(p[0]), "y": float(p[1])}
    xyz = lambda p: dict(xy(p), z=float(p[2]))

    def domain(d):
        return {"edgeIds": d["source_edges"], "faces": d["source_faces"],
                "anchor": xy(d["anchor_uz"]), "direction": xy(d["direction_uz"]),
                "width": float(d["full_width_mm"]), "first": xyz(d["first_xyz"]), "last": xyz(d["last_xyz"])}

    ranges = {r["key"]: r for r in config.get("active_reference_ranges", [])}
    references = []
    # Export filenames are aliases, not copied source basenames.
    for index, row in enumerate(config["references"]):
        p = checked(base / row["file"], row["sha256"])
        alias = row["key"] if row["key"] in ("upper", "lower", "left") else f"side-{index + 1}"
        filename = alias + ".asc"
        shutil.copy2(p, output / filename)
        scope = ranges.get(row["key"], {})
        references.append({"id": alias, "points": {"path": filename, "sha256": sha(p)},
                           "startMm": scope.get("start_mm", 0.), "endMm": scope.get("end_mm")})
    frames = []
    for row in config["frames"]:
        frozen = read(checked(base / row["file"], row["sha256"]))
        raw = read(checked(Path(frozen["source_file"]), frozen["source_sha256"]))
        order = [e["triangle_index"] for e in raw["source_triangle_segments"]]
        corners = []
        for e in frozen["extensions"]:
            for d in (e["frozen_cap_support"], e["frozen_wall_support"]):
                if len(d["source_edges"]) != len(d["source_faces"]) or any(
                        i < 0 or i >= len(order) or order[i] != f for i, f in zip(d["source_edges"], d["source_faces"])):
                    raise ValueError("Archived edge/triangle identity mismatch")
            corners.append({"side": e["side"], "entry": e["entry"], "cap": domain(e["frozen_cap_support"]),
                            "wall": domain(e["frozen_wall_support"]), "wallUShift": float(e["wall_reference_center_u_g17"])})
        excluded = row["frame"] in config.get("excluded_frames", [])
        frames.append({"id": row["frame"], "p1": xy(row["P1"]), "p2": xy(row["P2"]),
                       "origin": xy(raw["origin"]), "horizontal": xy(raw["horizontal"]),
                       "normal": xy(raw["plane"]["normal"]), "offset": float(raw["plane"]["offset"]),
                       "uMin": raw["original_local_u_bounds_mm"][0], "uMax": raw["original_local_u_bounds_mm"][1],
                       "sourceFaceOrder": order, "corners": corners, "reviewExcluded": excluded,
                       "reviewReason": "Historical source-identity review exclusion; not a new automatic physical rule." if excluded else None})
    manifest = {"schema": "reviewed-source-case/1", "id": case_id, "dataKind": "private-original",
                "mesh": {"path": "scan.stl", "sha256": sha(mesh)}, "references": references, "frames": frames,
                "planeZ": 0., "zMin": -50., "zMax": 50.}
    (output / "case.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Imported {len(frames)} reviewed source frames into a private directory. Original inputs remain local.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("config", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--mesh", type=Path, help="Relocated local mesh; SHA must match the export")
    args = parser.parse_args()
    try:
        prepare(args.config, args.output, args.mesh)
    except (OSError, ValueError, KeyError, TypeError) as error:
        parser.exit(1, str(error) + "\n")
