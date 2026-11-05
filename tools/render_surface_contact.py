"""Render unchanged private mesh + verified finite CAD surfaces; publish raster/aggregates only.

Usage: python tools/render_surface_contact.py MANIFEST RUN_DIR OUTPUT_DIR --report REPORT
Uses requirements-render.txt. All measurements use exact curves, never display triangles.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import struct

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection, LineCollection
from matplotlib.font_manager import FontProperties
from matplotlib.lines import Line2D
from matplotlib.colors import to_rgba
import numpy as np

from contact_geometry import closest_surface
from render_source_case import read, sample

COLORS = {"upper": "#007e91", "lower": "#b74a98", "left": "#ce7716"}
BAD = "#c63244"


def xyz(p):
    return np.array([p[k] for k in ("x", "y", "z")])


def section(world, origin, direction):
    """Display-only original STL section in the normal vertical plane through a probe."""
    normal = np.array([-direction[1], direction[0]])
    d = (world[:, :, :2]-origin[:2]) @ normal
    active = np.flatnonzero((d.min(1) <= 0) & (d.max(1) >= 0))
    points = {}
    for a, b in ((0, 1), (1, 2), (2, 0)):
        ids = active[d[active, a]*d[active, b] < 0]
        t = d[ids, a]/(d[ids, a]-d[ids, b])
        hit = world[ids, a]+t[:, None]*(world[ids, b]-world[ids, a])
        for face, p in zip(ids, hit):
            points.setdefault(int(face), []).append(p)
    for vertex in range(3):
        ids = active[np.abs(d[active, vertex]) <= 1e-10]
        for face in ids:
            points.setdefault(int(face), []).append(world[face, vertex])
    lines = []
    for hits in points.values():
        unique = []
        for p in hits:
            if not any(np.linalg.norm(p-q) < 1e-10 for q in unique):
                unique.append(p)
        if len(unique) == 2:
            p = np.array(unique)
            lines.append(np.c_[(p[:, :2]-origin[:2]) @ direction, p[:, 2]-origin[2]])
    return lines


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("run", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    manifest, data, verified = read(args.manifest), read(args.run/"source-run.json"), read(args.report)
    plan_path = args.run/"cad-plan.json"
    plan = read(plan_path)
    if hashlib.sha256(plan_path.read_bytes()).hexdigest() != verified["cadPlanSha256"]:
        raise ValueError("CAD plan differs from verified native plan")
    if data["source"]["inputFingerprint"] != verified["inputFingerprint"] or plan != data["cad"]:
        raise ValueError("Source, verification and CAD plan do not share identity")
    blob = (args.manifest.parent/manifest["mesh"]["path"]).read_bytes()
    if hashlib.sha256(blob).hexdigest() != manifest["mesh"]["sha256"]:
        raise ValueError("Original mesh fingerprint mismatch")
    count = struct.unpack_from("<I", blob, 80)[0]
    if len(blob) != 84+50*count:
        raise ValueError("Figure reader requires exact-length binary STL")
    dtype = np.dtype([("n", "<f4", (3,)), ("v", "<f4", (3, 3)), ("a", "<u2")])
    world = np.frombuffer(blob, dtype=dtype, offset=84, count=count)["v"].astype(float)
    font = Path("C:/Windows/Fonts/msjh.ttc")
    if font.exists():
        plt.rcParams["font.family"] = FontProperties(fname=str(font)).get_name()
    plt.rcParams.update({"font.size": 11, "axes.unicode_minus": False})
    args.output.mkdir(parents=True, exist_ok=True)
    center = (world.reshape(-1, 3).min(0)+world.reshape(-1, 3).max(0))/2
    normals = np.cross(world[:, 1]-world[:, 0], world[:, 2]-world[:, 0])
    normals /= np.maximum(np.linalg.norm(normals, axis=1), 1e-15)[:, None]
    light = np.array([.25, -.35, .9]); light /= np.linalg.norm(light)
    mesh_colors = np.c_[(.55+.4*np.abs(normals@light))[:, None]*np.array([.73, .79, .83]), np.ones(count)]
    panels, panel_colors = [], []
    for side in plan["sides"]:
        for curve in side["curves"]:
            # <= 0.02 mm circular chord error, <= 5 mm vertical patches for painter sorting.
            steps = 2
            if curve.get("center"):
                o, a = curve["center"], curve["start"]
                radius = math.hypot(a["x"]-o["x"], a["y"]-o["y"])
                da = 2*math.acos(max(-1., 1-min(.02/radius, 1.)))
                steps = max(2, math.ceil(abs(curve["sweep"])/da)+1)
            xy = sample(curve, steps)
            for z0, z1 in zip(np.linspace(plan["zMin"], plan["zMax"], 21)[:-1], np.linspace(plan["zMin"], plan["zMax"], 21)[1:]):
                for a, b in zip(xy[:-1], xy[1:]):
                    panels.extend([[[*a, z0], [*b, z0], [*b, z1]], [[*a, z0], [*b, z1], [*a, z1]]])
                    panel_colors.extend([to_rgba(COLORS[side["id"]], 1.)]*2)
    panels, panel_colors = np.array(panels), np.array(panel_colors)

    probes, summaries = {}, []
    for side, draft, check in zip(plan["sides"], data["draft"]["sides"], verified["sides"], strict=True):
        assert side["id"] == draft["id"] == check["id"]
        values = []
        for anchor in draft["sourceSupports"]:
            p = xyz(anchor["point"])
            q, index, xy, dz, distance = closest_surface(p, side["curves"], plan["zMin"], plan["zMax"])
            if abs(np.linalg.norm(p-np.array(q))-distance) > 1e-10:
                raise ValueError("Closest foot does not reproduce scalar gap")
            values.append(dict(anchor=anchor, p=p, q=np.array(q), primitive=index, xy=xy, dz=dz, distance=distance))
        worst = max(values, key=lambda v: v["distance"])
        if abs(worst["distance"]-check["sourceAnchorFiniteSurfaceMaximumMm"]) > 1e-7:
            raise ValueError("Independent surface distance differs from C# verification")
        probes[side["id"]] = values
        summaries.append(dict(id=side["id"], probes=len(values), withinLimit=sum(v["distance"] <= .3 for v in values),
                              overLimit=sum(v["distance"] > .3 for v in values),
                              maximumMm=worst["distance"], maximumXYMm=max(v["xy"] for v in values),
                              maximumHeightGapMm=max(v["dz"] for v in values), worstFrame=worst["anchor"]["frame"]))
        print(side["id"], summaries[-1], flush=True)

    def scene(ax, depth, focus=None, local_span=None, show_probes=False):
        depth = np.array(depth, float); depth /= np.linalg.norm(depth)
        right = np.cross([0., 0., 1.], depth); right /= np.linalg.norm(right)
        projection = np.array([right, np.cross(depth, right)])
        mesh = world
        colors = mesh_colors
        visible = normals @ depth >= -1e-12
        # Local views crop the projected viewport only, never cut away source triangles.
        mesh, colors = mesh[visible], colors[visible]
        all_tri = np.concatenate([mesh, panels])
        colors = np.concatenate([colors, panel_colors])
        order = np.argsort(all_tri.mean(1) @ depth, kind="stable")
        projected = (all_tri-center) @ projection.T
        ax.add_collection(PolyCollection(projected[order], facecolors=colors[order], edgecolors="none", antialiased=False, rasterized=True))
        for side in plan["sides"]:
            for curve in side["curves"]:
                p = sample(curve)
                for z in (plan["zMin"], plan["zMax"]):
                    q = (np.c_[p, np.full(len(p), z)]-center) @ projection.T
                    ax.plot(q[:, 0], q[:, 1], color=COLORS[side["id"]], lw=.9, alpha=.8)
            for c in (side["curves"][0]["start"], side["curves"][-1]["end"]):
                p = np.array([[c["x"], c["y"], z] for z in (plan["zMin"], plan["zMax"])])
                p = (p-center) @ projection.T
                ax.plot(p[:, 0], p[:, 1], color=COLORS[side["id"]], lw=1.1)
        if show_probes:
            for values in probes.values():
                p = (np.array([v["p"] for v in values])-center) @ projection.T
                ax.scatter(p[:, 0], p[:, 1], c=[BAD if v["distance"] > .3 else "#15546a" for v in values], s=12, edgecolors="white", linewidths=.35, zorder=5)
        if focus is None:
            low, high = projected.reshape(-1, 2).min(0)-12, projected.reshape(-1, 2).max(0)+12
            ax.set_xlim(low[0], high[0]); ax.set_ylim(low[1], high[1])
        else:
            p = (focus-center) @ projection.T
            ax.set_xlim(p[0]-local_span, p[0]+local_span); ax.set_ylim(p[1]-local_span, p[1]+local_span)
        ax.set_aspect("equal"); ax.axis("off")
        return projection

    fig, axes = plt.subplots(1, 2, figsize=(16, 9), facecolor="#f1f5f9")
    fig.subplots_adjust(left=.015, right=.985, top=.82, bottom=.13, wspace=.04)
    for ax, depth, title in zip(axes, [[.35, -.65, .68], [-.25, -.65, .72]], ["斜視｜面沿三側外緣延伸", "換向斜視｜同一工件、同一 CAD 計畫"]):
        scene(ax, depth, show_probes=True); ax.set_title(title, fontsize=14)
    handles = [Line2D([0], [0], color=c, lw=9, alpha=.9, label=f"{s} 有限延伸面") for s, c in COLORS.items()]
    handles += [Line2D([0], [0], marker="o", color="none", markerfacecolor="#15546a", label="原支持點 <=0.3 mm"),
                Line2D([0], [0], marker="o", color="none", markerfacecolor=BAD, label="原支持點 >0.3 mm")]
    fig.legend(handles=handles, loc="lower center", bbox_to_anchor=(.5, .065), ncol=5, frameon=False, fontsize=10)
    fig.suptitle("從掃描側線到貼面草稿｜原件與三側有限面疊合", fontsize=23, color="#244153", y=.96)
    fig.text(.5, .885, "原始孔洞／板金／焊道保持；彩面為同一 VSTA 已建面的計畫，Z = -50 … +50 mm。", ha="center", fontsize=12)
    fig.text(.5, .025, "數據重繪，非 CATIA 螢幕擷取；同一座標、等尺度、無 Z 誇大。119 點僅為支持子域，製造貼合尚未通過。", ha="center", fontsize=11)
    fig.savefig(args.output/"case-a-surfaces.png", dpi=150); plt.close(fig)

    fig, axes = plt.subplots(2, 3, figsize=(16, 10), facecolor="#f8fafc")
    fig.subplots_adjust(left=.055, right=.98, top=.86, bottom=.15, wspace=.28, hspace=.32)
    for col, summary in enumerate(summaries):
        key = summary["id"]; worst = max(probes[key], key=lambda v: v["distance"])
        p, q = worst["p"], worst["q"]
        ax = axes[0, col]; projection = scene(ax, [.35, -.65, .68], p, 12)
        ends = (np.array([p, q])-center) @ projection.T
        ax.plot(ends[:, 0], ends[:, 1], color=BAD, lw=2)
        ax.scatter(*ends[0], s=40, c="#15546a", edgecolors="white", zorder=5)
        ax.scatter(*ends[1], s=40, c=COLORS[key], marker="s", edgecolors="white", zorder=5)
        ax.set_title(f"{key}｜最差取樣框 {summary['worstFrame']}\n實距 {summary['maximumMm']:.6f} mm", fontsize=13)
        direction = q[:2]-p[:2]
        direction = direction/np.linalg.norm(direction) if np.linalg.norm(direction) > 1e-12 else np.array([1., 0.])
        ax = axes[1, col]
        ax.add_collection(LineCollection(section(world, p, direction), colors="#7e8b99", linewidths=1.3, label="原 STL 真實截線"))
        span = max(1.5, worst["distance"]*1.8)
        x = worst["xy"]
        ax.plot([x, x], [plan["zMin"]-p[2], plan["zMax"]-p[2]], color=COLORS[key], lw=2.3, label="有限面截線")
        ax.plot([0, x], [0, q[2]-p[2]], color=BAD, lw=2, label="最短距離")
        ax.scatter(0, 0, c="#15546a", s=45, zorder=5, label="原支持點 P")
        ax.scatter(x, q[2]-p[2], c=COLORS[key], marker="s", s=45, zorder=5, label="面上垂足 Q")
        ax.plot([-.7*span, -.7*span+.3], [-.72*span]*2, color=BAD, lw=3)
        ax.text(-.7*span, -.72*span+.13*span, "0.3 mm 判準尺", fontsize=9, color=BAD)
        ax.set_xlim(-span*.75, span*1.25); ax.set_ylim(-span, span)
        ax.set_aspect("equal"); ax.grid(alpha=.2)
        ax.set_xlabel("沿 P→Q 水平方向 (mm)"); ax.set_ylabel("相對 P 的 Z (mm)")
        ax.set_title(f"等尺度法向剖面｜ΔXY={worst['xy']:.6f} / ΔZ={worst['dz']:.6f}", fontsize=10)
    fig.legend(*axes[1, 0].get_legend_handles_labels(), loc="lower center", bbox_to_anchor=(.5, .065), ncol=5, fontsize=10, frameon=False)
    fig.suptitle("貼面差在哪裡？｜三側最差原支持點與有限面垂足", fontsize=22, y=.97, color="#244153")
    fig.text(.5, .915, "上排：原件與面局部 24 × 24 mm；下排：通過 P、Q 的真實 STL 法向剖面。距離由解析線弧計算。", ha="center", fontsize=11)
    fig.text(.5, .025, "不移動原件、不放大縫隙。這三個點解釋當前樣點最大值；不代表完整焊道或實際接觸帶已被辨識。", ha="center", fontsize=11)
    fig.savefig(args.output/"case-a-contact-detail.png", dpi=150); plt.close(fig)

    fig, axes = plt.subplots(1, 3, figsize=(16, 5.5), facecolor="#f8fafc")
    fig.subplots_adjust(left=.055, right=.98, top=.75, bottom=.2, wspace=.25)
    for ax, summary in zip(axes, summaries):
        key = summary["id"]; values = probes[key]
        ax.axhline(.3, color=BAD, ls="--", lw=1.3, label="0.3 mm 判準")
        ax.scatter([v["anchor"]["referenceStation"] for v in values], [v["distance"] for v in values],
                   c=[BAD if v["distance"] > .3 else COLORS[key] for v in values], s=26)
        ax.set_ylim(0, 2.0); ax.set_xlim(left=0); ax.grid(alpha=.2)
        ax.set_title(f"{key}｜{summary['probes']} 點；{summary['overLimit']} 點超標\n最大 {summary['maximumMm']:.6f} mm", fontsize=12)
        ax.set_xlabel("該側參考線站距 (mm)"); ax.set_ylabel("到有限面的 3D 距離 (mm)")
    fig.suptitle("全部 119 個支持端點的貼面距離｜超標點完整保留", fontsize=21, color="#244153", y=.97)
    fig.text(.5, .855, "三圖共用距離軸；散點不連線，不把未採樣區域塗成合格接觸帶。", ha="center", fontsize=11)
    max_height = max(s["maximumHeightGapMm"] for s in summaries)
    height_note = "本次樣點 dZ=0，超差來自橫向位置" if max_height == 0 else f"本次樣點最大 dZ={max_height:.6f} mm"
    fig.text(.5, .055, f"d3D = sqrt(dXY² + dZ²)；dZ 是超出有限高度的部分。{height_note}；不代表整片 STL 高度覆蓋。", ha="center", fontsize=10)
    fig.savefig(args.output/"case-a-contact-probes.png", dpi=150); plt.close(fig)
    figures = [dict(path="assets/"+name, sha256=hashlib.sha256((args.output/name).read_bytes()).hexdigest())
               for name in ("case-a-surfaces.png", "case-a-contact-detail.png", "case-a-contact-probes.png")]
    receipt = dict(schema="surface-contact-display/1", case=verified["case"], cadPlanSha256=verified["cadPlanSha256"],
                   meshSha256=manifest["mesh"]["sha256"], originalTriangles=count, zMin=plan["zMin"], zMax=plan["zMax"],
                   thresholdMm=.3, comparisonToleranceMm=1e-7, measurement="analytic finite curves, independent closest points; display tessellation is not measured",
                   scope="119 original approximate engineering-anchor endpoints only; not a physically established contact band or full-STL Hausdorff distance",
                   imageKind="redrawn from unchanged private mesh and exact native-verified CAD plan; not a native viewport",
                   physicalContactAccepted=False, sides=summaries, figures=figures)
    (args.output/"case-a-surface-contact.json").write_text(json.dumps(receipt, indent=2)+"\n", encoding="utf-8")


if __name__ == "__main__":
    main()
