"""Draw the portfolio case from the generated STL, run, section and height reports."""
import argparse
import json
from math import cos, sin
from pathlib import Path
from xml.sax.saxutils import escape


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def xyz(row):
    return row["x"], row["y"], row["z"]


def curve_points(curve, z):
    a, b, c = (curve[k] for k in ("start", "end", "center"))
    if c is None:
        return [(a["x"], a["y"], z), (b["x"], b["y"], z)]
    dx, dy = a["x"] - c["x"], a["y"] - c["y"]
    return [(c["x"] + dx * cos(t) - dy * sin(t), c["y"] + dx * sin(t) + dy * cos(t), z)
            for t in (curve["sweep"] * i / 24 for i in range(25))]


def render(source, output, height_output, section):
    job, run = read(source / "job.json"), read(output / "run.json")
    report = read(output / "verification.json")
    height = read(height_output / "verification.json")
    if (run["case"], report["case"], height["case"], section["side"], section["index"], section["rawSupport"]["state"]) != (
        "workpiece", "workpiece", "workpiece-height", "edge-b", 29, "ambiguous"
    ):
        raise ValueError("Expected the workpiece run, its height control and edge-b section 29.")
    guides = [tuple(map(float, row.split())) for row in (source / "guide.asc").read_text().splitlines() if row]
    vertices = [tuple(map(float, row.split()[1:])) for row in (source / "scan.stl").read_text().splitlines()
                if row.strip().startswith("vertex ")]
    triangles = list(zip(vertices[::3], vertices[1::3], vertices[2::3], strict=True))
    svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="806" viewBox="0 0 1200 806" role="img" aria-labelledby="title desc">',
           '<title id="title">Scan-to-CATIA engineering case</title>',
           '<desc id="desc">Generated sheet scan with three inspected sides, a withheld region and a zoomed cross-section with competing wall supports.</desc>',
           '<g font-family="Segoe UI,system-ui,sans-serif">']

    def rect(x, y, w, h, fill, rx=0):
        svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" rx="{rx}"/>')

    def text(x, y, label, size=16, fill="#183248", bold=False):
        weight = 700 if bold else 400
        svg.append(f'<text x="{x}" y="{y}" fill="{fill}" font-size="{size}" font-weight="{weight}">{escape(label)}</text>')

    def line(points, color, width=1, extra=""):
        coords = " ".join(f"{x:.2f},{y:.2f}" for x, y in points)
        svg.append(f'<polyline points="{coords}" fill="none" stroke="{color}" stroke-width="{width}" stroke-linecap="round" {extra}/>')

    def polygon(points, fill, stroke, width):
        coords = " ".join(f"{x:.2f},{y:.2f}" for x, y in points)
        svg.append(f'<polygon points="{coords}" fill="{fill}" stroke="{stroke}" stroke-width="{width}"/>')

    def dot(p, radius, fill, stroke="none"):
        svg.append(f'<circle cx="{p[0]:.2f}" cy="{p[1]:.2f}" r="{radius}" fill="{fill}" stroke="{stroke}"/>')

    rect(0, 0, 1200, 806, "#eaf0f6")
    rect(0, 0, 1200, 132, "#132b41")
    text(32, 36, "C# COMPUTATIONAL GEOMETRY   /   CATIA VSTA", 15, "#91d5e8", True)
    text(32, 79, "From one sheet scan to a CAD handoff", 35, "white", True)
    text(34, 107, "Synthetic workpiece  •  Section provenance  •  Editable tangent lines and arcs", 17, "#d5e5ef")
    rect(22, 151, 730, 632, "white", 16)
    rect(772, 151, 406, 346, "white", 16)
    rect(772, 515, 406, 268, "white", 16)
    text(45, 184, "01   Whole scan and CAD coverage", 22, bold=True)
    text(45, 210, f"{len(triangles):,} STL triangles   /   {len(guides)} section stations   /   one world coordinate frame", 15)

    def oblique(v):
        x, y, z = v
        return .93 * x + .38 * y, .30 * x - .63 * y - 1.3 * z

    projected = [oblique(v) for v in vertices]
    min_x, max_x = min(x for x, _ in projected), max(x for x, _ in projected)
    min_y, max_y = min(y for _, y in projected), max(y for _, y in projected)
    scale = min(640 / (max_x - min_x), 365 / (max_y - min_y))
    offset_x = 51 + (640 - (max_x - min_x) * scale) / 2

    def view(v):
        x, y = oblique(v)
        return offset_x + (x - min_x) * scale, 247 + (y - min_y) * scale

    def depth(item):
        return sum(.24 * x - .43 * y + z for x, y, z in item[1])

    for index, triangle in sorted(enumerate(triangles), key=depth):
        z = sum(v[2] for v in triangle) / 3
        fill = "#a0acbd" if index >= len(triangles) - 48 else "#b8c9d9" if z > 4.7 else "#879eb4"
        polygon(map(view, triangle), fill, "#dce7f0", .38)
    for ring in ([triangle[0] for triangle in triangles[-48::2]], [triangle[2] for triangle in triangles[-47::2]]):
        line(map(view, ring + ring[:1]), "#566d82", .85)

    origin, normal = section["origin"], section["normal"]
    transverse = -normal["y"], normal["x"]

    def plane(u, z):
        return origin["x"] + u * transverse[0], origin["y"] + u * transverse[1], z

    plane_outline = list(map(view, (plane(-2.5, -.9), plane(2.5, -.9), plane(2.5, 5.6), plane(-2.5, 5.6))))
    polygon(plane_outline, "#f9d8a9", "#b45a06", 1.2)

    status_colors = {"observed": "#245da8", "inferred": "#ba6709", "gap": "#c23755", "ambiguous": "#c23755"}
    for guide, side in zip(job["guides"], run["sides"], strict=True):
        for row in side["stations"]:
            original = guides[guide["start"] + row["index"]]
            point = row["point"]
            v = (point["x"], point["y"], job["probeZ"]) if point else (*original[:2], job["probeZ"])
            dot(view(v), 2.55 if row["state"] != "observed" else 1.45, status_colors[row["state"]])
        if side["rounded"]:
            for curve in side["rounded"]["curves"]:
                line(map(view, curve_points(curve, job["planeZ"])), "#7444a4" if curve["center"] else "#08756d", 2.8)

    def label(label, target, x, y, color="#183248"):
        width = round(len(label) * 8.1 + 14)
        line([target, (x + min(max(target[0] - x, 5), width - 5), y - 6)], color, 1.05)
        dot(target, 4.7, "white", color)
        rect(x, y - 16, width, 22, "#f4f7fb", 4)
        text(x + 6, y, label, 15, color, True)

    sleeve = [v for triangle in triangles[-48:] for v in triangle]
    label("Tube outside guide range", view(tuple(sum(axis) / len(axis) for axis in zip(*sleeve))), 52, 245)
    label("B29", view(plane(-1.45, 2)), 645, 452, "#a24012")
    b10 = guides[job["guides"][1]["start"] + 10]
    label("B10 gap", view((*b10[:2], 2)), 525, 564, "#a24012")
    c = run["sides"][2]["stations"][9]["point"]
    label("C09 inferred", view((c["x"], c["y"], 2)), 469, 248, "#915309")

    for i, row in enumerate(report["sides"]):
        x = 45 + 229 * i
        rect(x, 630, 217, 82, "#f1f5f9", 9)
        state = "CAD ready" if row["status"] == "checked" else "CAD withheld"
        text(x + 12, 657, f"{row['id'].upper()}   {state}", 16, "#183248", True)
        text(x + 12, 679, f"{row['observed']} observed  /  {row['inferred']} inferred", 14)
        text(x + 12, 698, f"{row['reducedCount']} CAD curves  /  {row['unresolved']} unresolved", 14)

    for x, color, caption in [(52, "#245da8", "Observed"), (164, "#ba6709", "Inferred"), (265, "#c23755", "Unresolved"),
                              (392, "#08756d", "CAD line"), (502, "#7444a4", "CAD arc")]:
        dot((x, 750), 4.5, color)
        text(x + 11, 755, caption, 14)

    text(794, 184, "02   Section B29", 22, bold=True)
    text(794, 209, "A second wall crosses the same probe height.", 15)
    rect(795, 224, 359, 192, "#f4f7fb", 8)

    def cross(u, z):
        return 976 + 60 * u, 361 - 23 * z

    def local(row):
        u = (row["x"] - origin["x"]) * transverse[0] + (row["y"] - origin["y"]) * transverse[1]
        return u, row["z"]

    z_probe = section["probeZ"]
    line([cross(-2.55, z_probe), cross(2.55, z_probe)], "#ac6831", 1.2, 'stroke-dasharray="5 4"')
    text(1025, 309, f"Probe Z = {z_probe:g} mm", 12, "#814a20")
    marked = set(section["rawSupport"]["triangles"])
    for i, component in enumerate(section["components"]):
        color = ("#245da8", "#a43a64")[i % 2]
        for edge in component:
            a, b = local(edge["a"]), local(edge["b"])
            line([cross(*a), cross(*b)], color, 3)
            if edge["triangle"] in marked:
                t = (z_probe - a[1]) / (b[1] - a[1])
                dot(cross(a[0] + (b[0] - a[0]) * t, z_probe), 5, "white", color)

    text(808, 407, "−2.5 mm", 12)
    text(954, 407, "local u", 12)
    text(1090, 407, "+2.5 mm", 12)
    text(795, 449, f"{len(section['components'])} components  /  {len(marked)} supporting triangle IDs", 16, bold=True)
    text(795, 473, "Ambiguous at the probe. Edge B is withheld.", 15)

    checked = [s for s in report["sides"] if s["status"] == "checked"]
    short = [s for s in height["sides"] if s["status"] == "checked"]
    xy = max(s["roundedXYError"] for s in checked)
    normal_z = max(s["contactSampleMaximum"] for s in checked)
    short_z = max(s["contactSampleMaximum"] for s in short)
    text(794, 549, "03   Finite surface check", 22, bold=True)
    text(794, 576, "Same XY chains. Only the lower Z bound moves.", 15)
    for y, title, value, color in [(616, "Rounded XY", xy, "#08756d"), (658, "Baseline 3D", normal_z, "#08756d"),
                                    (700, "Shortened 3D", short_z, "#a73352")]:
        text(795, y, title, 15)
        rect(927, y - 12, 118, 13, "#e9eef4", 3)
        rect(927, y - 12, round(value / .75 * 118, 1), 13, color, 3)
        line([(974.2, y - 14), (974.2, y + 3)], "#8293a5", 1)
        text(1061, y, f"{value:.4f} mm", 15, color, True)
    text(795, 751, "Distance limit 0.300 mm   /   synthetic samples", 14)
    svg.extend(["</g>", "</svg>"])
    return "\n".join(svg) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("source", "output", "height_output", "section", "svg"):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    args.svg.write_text(render(args.source, args.output, args.height_output, read(args.section)), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
