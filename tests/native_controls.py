"""Licensed Windows-only pre-mutation controls against the current Macro executable.

Run after a successful VSTA demo, with that same demo active. Private plans and raw
diagnostics stay under ignored artifacts; this is external STA diagnostic evidence.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("plan", type=Path)
    p.add_argument("receipt", type=Path)
    p.add_argument("output", type=Path)
    args = p.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to(ROOT / "artifacts") or output.exists():
        p.error("choose a fresh ignored artifacts directory")
    output.mkdir(parents=True)
    plan = json.loads(args.plan.read_text(encoding="utf-8-sig"))
    receipt = json.loads(args.receipt.read_text(encoding="utf-8-sig"))
    macro = ROOT / "macro/bin/Release/PortfolioMacro.exe"
    rows = []

    def reject(name, command, message):
        destination = output / (name + "-receipt.json")
        process = subprocess.run([str(macro), *map(str, command), str(destination)],
                                 capture_output=True, text=True, encoding="utf-8", errors="replace")
        (output / (name + ".log")).write_text(process.stdout + process.stderr, encoding="utf-8")
        assert process.returncode == 1 and message in process.stderr and not destination.exists(), (name, process.stderr)
        rows.append({"control": name, "exitCode": process.returncode, "expectedMessageObserved": message,
                     "successReceiptCreated": False})

    reversed_arc = copy.deepcopy(plan)
    arc = next(c for side in reversed_arc["sides"] for c in side["curves"] if c.get("center"))
    arc["sweep"] *= -1
    reverse_path = output / "reversed.json"
    reverse_path.write_text(json.dumps(reversed_arc), encoding="utf-8")
    reject("signed-arc", ["sketches", reverse_path], "Invalid signed arc")
    wrong = copy.deepcopy(plan)
    wrong["source"] += ":tampered"
    wrong_path = output / "fingerprint.json"
    wrong_path.write_text(json.dumps(wrong), encoding="utf-8")
    reject("fingerprint", ["surfaces", wrong_path, args.receipt.resolve()], "Sketch receipt and CAD plan do not match")
    wrong = copy.deepcopy(receipt)
    wrong["root"] = "PortfolioSketch_missing_control"
    wrong_path = output / "wrong-root.json"
    wrong_path.write_text(json.dumps(wrong), encoding="utf-8")
    reject("owning-sketch", ["surfaces", args.plan.resolve(), wrong_path], "does not own the validated sketch marker")
    reject("duplicate-surfaces", ["surfaces", args.plan.resolve(), args.receipt.resolve()], "already contains an extension attempt")
    capacity = copy.deepcopy(plan)
    capacity["sides"][0]["curves"] = [capacity["sides"][0]["curves"][0]] * 257
    capacity_path = output / "capacity.json"
    capacity_path.write_text(json.dumps(capacity), encoding="utf-8")
    reject("curve-capacity", ["sketches", capacity_path], "Invalid curve list (maximum 256 per side)")
    workflow = (ROOT / "macro/MacroWorkflow.cs").read_text(encoding="utf-8-sig").rstrip()
    summary = {"schema": "native-rejection-controls/1", "invocation": "external STA diagnostic; not editor F5",
               "planSha256": hashlib.sha256(args.plan.read_bytes()).hexdigest(),
               "workflowSha256": hashlib.sha256(workflow.encode()).hexdigest(), "controls": rows,
               "scope": "pre-mutation rejection paths on the already verified current demo; private inputs/logs retained locally"}
    (output / "summary.json").write_text(json.dumps(summary, indent=2)+"\n", encoding="utf-8")
    print("Current macro: five native pre-mutation controls rejected with no success receipts.")


if __name__ == "__main__": main()
