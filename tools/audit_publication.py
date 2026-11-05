"""Audit the public tree and optionally write a content-addressed publication inventory.

Original geometry files are generated/copied only under ignored local artifacts. This
check does not claim that old GitHub caches or unreachable historical blobs are purged.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
BLOCKED = {".stl", ".asc", ".dll", ".exe", ".catpart", ".catproduct", ".3dxml", ".zip", ".gz"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write-inventory", action="store_true")
    parser.add_argument("--check-inventory", action="store_true")
    args = parser.parse_args()
    result = subprocess.run(["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"],
                            cwd=ROOT, capture_output=True, check=True)
    names = sorted(set(n for n in result.stdout.decode("utf-8").split("\0") if n))
    errors, rows = [], []
    # Only report the category and filename; never echo a matching identifier.
    private_tokens = [str(Path.home()).replace("\\", "/"), str(Path.home()).replace("/", "\\")]
    for name in names:
        p = ROOT / name
        if not p.is_file() or name == "docs/evidence/public-files.json":
            continue
        if p.suffix.lower() in BLOCKED or "artifacts" in p.parts or {"bin", "obj"} & set(p.parts):
            errors.append((name, "raw/generated source or compiled file"))
        blob = p.read_bytes()
        if p.suffix.lower() in {".cs", ".py", ".md", ".json", ".yml", ".props", ".csproj"}:
            text = blob.decode("utf-8-sig")
            if any(token and token.casefold() in text.casefold() for token in private_tokens):
                errors.append((name, "personal absolute path"))
            if re.search(r"vst-\d{8}-\d{8}|3D Shape000\d+", text):
                errors.append((name, "raw native identity"))
        if args.write_inventory or args.check_inventory:
            staged = subprocess.run(["git", "show", ":" + name], cwd=ROOT, capture_output=True)
            if staged.returncode:
                raise SystemExit("Stage all reviewed public files before writing/checking the inventory: " + name)
            blob = staged.stdout  # Exact published blob, including -text evidence-plan exceptions.
        rows.append({"path": name, "bytes": len(blob), "sha256": hashlib.sha256(blob).hexdigest()})
    if errors:
        raise SystemExit(json.dumps(errors, ensure_ascii=False, indent=2))
    if args.write_inventory:
        receipt = {"schema": "portfolio-public-inventory/1", "scope": "exact staged Git blobs; inventory excludes itself; checkout newline conversion does not change these hashes",
                   "originalFilesIncluded": False, "files": rows,
                   "limitations": "Source-coordinate plans and raw native logs remain local. This inventory does not prove removal of pre-existing remote caches."}
        (ROOT / "docs/evidence/public-files.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    if args.check_inventory:
        inventory = json.loads((ROOT / "docs/evidence/public-files.json").read_text(encoding="utf-8"))
        if inventory["files"] != rows:
            raise SystemExit("Publication inventory differs from the exact staged Git blobs")
    print(f"Public tree audit passed: {len(rows)} files, no raw mesh/reference/CAD/binary archives or private path/native identities.")


if __name__ == "__main__": main()
