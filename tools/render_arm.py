"""Render the independently designed suspension arm and actual STL sections."""
import argparse
import json
from pathlib import Path
from xml.sax.saxutils import escape

COLORS = {"upper-shell": "#9ab7ce", "lower-shell": "#738da5", "upper-bend": "#b8cbd9",
          "lower-bend": "#849bb0", "upper-wall": "#6d95af", "lower-wall": "#657f96",
          "inner-wall": "#61778a", "weld": "#db943f", "mount-sleeve": "#9aa6b3",
          "mount-bore": "#4c5d70", "mount-rim": "#c5cdd6"}


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def render(folder, section_files, sections_only, weld_zoom=False):
    design = read(folder / "design.json")
    vertices = [tuple(map(float, s.split()[1:])) for s in (folder / "scan.stl").read_text().splitlines()
                if s.strip().startswith("vertex ")]
    triangles = list(zip(vertices[::3], vertices[1::3], vertices[2::3], strict=True))
    roles = design["triangleRoles"]
    if len(roles) != len(triangles):
        raise ValueError("Design role index does not match the scan.")
    sections = [read(path) for path in section_files]
    width, height = (1440, 990) if not sections_only else (1440, 880)
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" role="img" aria-label="Synthetic stamped and welded suspension arm, with true scan sections">',
           '<g font-family="Segoe UI,Arial,sans-serif">']

    def rect(x, y, w, h, fill, rx=0):
        out.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" fill="{fill}"/>')

    def text(x, y, value, size=17, color="#20364b", bold=False):
        out.append(f'<text x="{x}" y="{y}" font-size="{size}" fill="{color}" font-weight="{700 if bold else 400}">{escape(str(value))}</text>')

    def path(points, color, w=1.3, fill="none", closed=False):
        tag = "polygon" if closed else "polyline"
        coords = " ".join(f"{x:.2f},{y:.2f}" for x, y in points)
        out.append(f'<{tag} points="{coords}" fill="{fill}" stroke="{color}" stroke-width="{w}" stroke-linejoin="round"/>')

    rect(0, 0, width, height, "#ecf1f6")
    rect(0, 0, width, 134, "#142c42")
    text(35, 36, "C# COMPUTATIONAL GEOMETRY   /   3DEXPERIENCE CATIA VSTA", 17, "#8dd7df", True)
    text(35, 80, "Stamped shells. Weld beads. Difficult scan sections.", 34, "white", True)
    text(36, 111, "Independently dimensioned suspension arm  |  Every section below is cut from the generated STL", 18, "#d8e6ef")

    def section_card(section, x, y, w, h):
        rect(x, y, w, h, "white", 14)
        raw = section["rawSupport"]
        text(x + 20, y + 32, f"{section['side'].upper()} / section {section['index']}", 21, bold=True)
        text(x + 20, y + 58, f"{len(section['components'])} connected components  /  {raw['state']}", 16)
        o, n = section["origin"], section["normal"]

        def local(p):
            return -(p["x"] - o["x"]) * n["y"] + (p["y"] - o["y"]) * n["x"], p["z"]

        edges = [edge for c in section["components"] for edge in c]
        coords = [local(e[k]) for e in edges for k in ("a", "b")]
        if not coords:
            text(x + 20, y + 110, "No source intersection in the frame.")
            return
        min_u, max_u = (-5, 7) if weld_zoom else (min(p[0] for p in coords), max(p[0] for p in coords))
        visible = [p for p in coords if min_u <= p[0] <= max_u] or coords
        min_z, max_z = min(p[1] for p in visible), max(p[1] for p in visible)
        scale = min((w - 70) / max(1, max_u - min_u), (h - 147) / max(1, max_z - min_z))

        def point(p):
            u, z = local(p)
            return x + 35 + (w - 70 - (max_u - min_u) * scale) / 2 + (u - min_u) * scale, y + 91 + (max_z - z) * scale

        clip = f"frame-{section['side']}-{section['index']}"
        out.append(f'<clipPath id="{clip}"><rect x="{x+20}" y="{y+75}" width="{w-40}" height="{h-126}"/></clipPath>')
        out.append(f'<g clip-path="url(#{clip})">')
        for e in edges:
            path([point(e["a"]), point(e["b"])], COLORS[roles[e["triangle"]]], 2.3)
        for cap in (section.get("shell") or {}).get("candidates", []):
            for e in cap["wallEdges"]:
                path([point(e["a"]), point(e["b"])], "#047e70", 4)
        out.append('</g>')
        text(x + 20, y + h - 30, "u = frame transverse coordinate   /   Z in mm", 14)

    if sections_only:
        for i, section in enumerate(sections):
            section_card(section, 24 + i % 2 * 708, 156 + i // 2 * 352, 684, 332)
    else:
        rect(24, 154, 938, 740, "white", 16)
        text(46, 189, "Assembly design and weld placement", 23, bold=True)
        text(46, 217, f"{len(triangles):,} STL triangles  /  upper and lower formed shells  /  three hollow mounts", 17)
        extents = [max(p[i] for p in vertices) - min(p[i] for p in vertices) for i in range(3)]
        text(46, 242, f"Scan extents {extents[0]:.1f} × {extents[1]:.1f} × {extents[2]:.1f} mm  /  varying bead width and height", 16)

        def projection(p):
            x, y, z = p
            return .91 * x + .38 * y, .24 * x - .67 * y - 1.5 * z

        coords = [projection(v) for v in vertices]
        min_x, max_x = min(x for x, _ in coords), max(x for x, _ in coords)
        min_y, max_y = min(y for _, y in coords), max(y for _, y in coords)
        scale = min(842 / (max_x - min_x), 558 / (max_y - min_y))
        x0 = 68 + (842 - (max_x - min_x) * scale) / 2

        def point(v):
            x, y = projection(v)
            return x0 + (x - min_x) * scale, 266 + (y - min_y) * scale

        def depth(item):
            return sum(.3 * x - .6 * y + z for x, y, z in item[1])

        for index, triangle in sorted(enumerate(triangles), key=depth):
            color = COLORS[roles[index]]
            path(map(point, triangle), color, .3, color, True)
        for section in sections:
            o, n = section["origin"], section["normal"]
            v = (o["x"], o["y"], 11)
            target = point(v)
            path([point((v[0] + n["y"] * 18, v[1] - n["x"] * 18, v[2])),
                  point((v[0] - n["y"] * 30, v[1] + n["x"] * 30, v[2]))], "#b23e67", 1.8)
            rect(target[0] + 7, target[1] - 23, 92, 26, "#fff5e9", 5)
            text(target[0] + 13, target[1] - 5, f"{section['side']}:{section['index']}", 14, "#793251", True)
        text(48, 867, "Orange weld mesh   |   Blue formed sheet   |   Grey sleeves   |   Magenta section frames", 16)
        for i, section in enumerate(sections[:3]):
            section_card(section, 984, 154 + i * 247, 432, 230)
        text(33, 931, "Design dimensions and nominal curves are authoring data. Geometric decisions use the STL intersections.", 17, bold=True)
        text(33, 960, "Public fixture has independently generated geometry. Source triangle identities remain available in the section JSON.", 16)

    out.append("</g></svg>")
    return "\n".join(out) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("svg", type=Path)
    parser.add_argument("sections", nargs="+", type=Path)
    parser.add_argument("--sections-only", action="store_true")
    parser.add_argument("--weld-zoom", action="store_true")
    args = parser.parse_args()
    args.svg.write_text(render(args.source, args.sections, args.sections_only, args.weld_zoom), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
