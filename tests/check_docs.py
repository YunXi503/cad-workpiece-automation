"""Check repository-relative Markdown targets and their GitHub-style headings."""
from pathlib import Path
import re
import sys
import urllib.parse

ROOT = Path(__file__).resolve().parents[1]
LINK = re.compile(r"\]\(([^()\s]+)\)")
HEADING = re.compile(r"^#{1,6}\s+(.*)$", re.M)

def slug(value):
    value = value.lower().strip()
    value = re.sub(r"[^\w\-\s]", "", value)
    return re.sub(r"\s", "-", value)

def main():
    pages = [ROOT / "README.md", *sorted((ROOT / "docs").rglob("*.md"))]
    errors = []
    count = 0
    for page in pages:
        for raw in LINK.findall(page.read_text(encoding="utf-8")):
            target = urllib.parse.urlsplit(raw)
            if target.scheme or target.netloc:
                continue
            path = (page.parent / urllib.parse.unquote(target.path)).resolve() if target.path else page
            label = f"{page.relative_to(ROOT)} → {raw}"
            count += 1
            if not path.is_relative_to(ROOT) or not path.exists():
                errors.append(label)
                continue
            anchor = urllib.parse.unquote(target.fragment)
            if anchor and path.suffix == ".md":
                headings = HEADING.findall(path.read_text(encoding="utf-8"))
                if anchor not in {slug(h) for h in headings}:
                    errors.append(label)
            elif anchor:
                match = re.fullmatch(r"L(\d+)(?:-L(\d+))?", anchor)
                if match:
                    a = int(match.group(1))
                    b = int(match.group(2) or a)
                    if not (1 <= a <= b <= len(path.read_text(encoding="utf-8").splitlines())):
                        errors.append(label)
    print(f"{len(pages)} pages, {count} repository-relative targets, {len(errors)} link errors.")
    if errors:
        print("\n".join(errors))
    return int(bool(errors))

if __name__ == "__main__":
    sys.exit(main())
