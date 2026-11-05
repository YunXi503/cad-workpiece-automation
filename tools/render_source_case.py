"""Redraw a verified private case as raster portfolio figures; never export source files.

Reads the private binary STL used for Case A; the CLI SVG renderer supports public fixtures.
Optional dependencies: requirements-render.txt. Units/proportions and mesh are unchanged.
The drawings are presentations, not a physical weld/fillet classifier.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.collections import LineCollection, PolyCollection
from matplotlib.font_manager import FontProperties
import numpy as np


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def point(p):
    return np.array([p["x"], p["y"]])


def sample(c, n=81):
    a, b = point(c["start"]), point(c["end"])
    if c.get("center") is None:
        return np.array([a, b])
    o = point(c["center"])
    angles = np.arctan2(*(a-o)[::-1]) + np.linspace(0, c["sweep"], n)
    return o + np.linalg.norm(a-o)*np.c_[np.cos(angles), np.sin(angles)]


def finite_errors(points, curves):
    best=np.full(len(points),np.inf)
    for c in curves:
        a,b=point(c["start"]),point(c["end"])
        if c.get("center") is None:
            d=b-a
            t=np.clip((points-a)@d/(d@d),0,1)
            distance=np.linalg.norm(points-a-t[:,None]*d,axis=1)
        else:
            o=point(c["center"]);radius=np.linalg.norm(a-o)
            angle=np.arctan2(points[:,1]-o[1],points[:,0]-o[0])-np.arctan2(a[1]-o[1],a[0]-o[0])
            delta=(angle*np.sign(c["sweep"]))%(2*np.pi)
            ends=np.minimum(np.linalg.norm(points-a,axis=1),np.linalg.norm(points-b,axis=1))
            distance=np.where(delta<=abs(c["sweep"])+1e-12,np.abs(np.linalg.norm(points-o,axis=1)-radius),ends)
        best=np.minimum(best,distance)
    return best


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("run", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--report", type=Path, help="Optional independently verified aggregate report to cross-check plot metrics")
    args = parser.parse_args()
    job, data = read(args.manifest), read(args.run / "source-run.json")
    verified=read(args.report) if args.report else None
    if verified and verified["inputFingerprint"]!=data["source"]["inputFingerprint"]:
        raise ValueError("Plot report belongs to another input")
    blob = (args.manifest.parent / job["mesh"]["path"]).read_bytes()
    if hashlib.sha256(blob).hexdigest() != job["mesh"]["sha256"]:
        raise ValueError("Source fingerprint mismatch")
    dtype = np.dtype([("n", "<f4", (3,)), ("v", "<f4", (3, 3)), ("a", "<u2")])
    world = np.frombuffer(blob, dtype=dtype, offset=84, count=struct.unpack_from("<I", blob, 80)[0])["v"].astype(float)
    if Path("C:/Windows/Fonts/msjh.ttc").exists():
        plt.rcParams["font.family"] = FontProperties(fname="C:/Windows/Fonts/msjh.ttc").get_name()
    plt.rcParams.update({"font.size": 11, "axes.unicode_minus": False})
    args.output.mkdir(parents=True, exist_ok=True)
    center = world.reshape(-1, 3).mean(0)
    triangles = world-center

    def draw_mesh(ax, depth, right=None):
        depth = np.array(depth, float); depth /= np.linalg.norm(depth)
        right = np.cross([0., 0., 1.], depth) if right is None else np.array(right, float)
        right /= np.linalg.norm(right)
        projection = np.vstack([right, np.cross(depth, right)])
        projected = triangles @ projection.T
        normals = np.cross(triangles[:, 1]-triangles[:, 0], triangles[:, 2]-triangles[:, 0])
        normals /= np.maximum(np.linalg.norm(normals, axis=1), 1e-15)[:, None]
        visible = np.flatnonzero(normals @ depth >= -1e-12)
        order = visible[np.argsort(triangles.mean(1)[visible] @ depth)]
        light = np.array([.25, -.35, .9]); light /= np.linalg.norm(light)
        colors = (.48+.47*np.abs(normals@light))[:, None]*np.array([.74, .82, .88])
        ax.add_collection(PolyCollection(projected[order], facecolors=colors[order], edgecolors="none", antialiased=False, rasterized=True))
        low, high = projected.reshape(-1, 2).min(0)-12, projected.reshape(-1, 2).max(0)+12
        ax.set_xlim(low[0], high[0]); ax.set_ylim(low[1], high[1]); ax.set_aspect("equal"); ax.axis("off")
        return projection

    fig, axes = plt.subplots(1, 2, figsize=(16, 8), layout="constrained", facecolor="#f1f5f9")
    for ax, depth, right, title in zip(axes, [[.35,-.65,.68], [0,0,1]], [None,[1,0,0]],
                                      ["原始 Case A｜等尺度斜視", "原始 Case A｜俯視與 CAD 草稿"]):
        projection = draw_mesh(ax, depth, right)
        if right is not None:
            for side, color in zip(data["cad"]["sides"], ["#007c91", "#b6539a", "#c87215"]):
                for curve in side["curves"]:
                    p = sample(curve)-center[:2]
                    ax.plot(p[:,0], p[:,1], color=color, lw=1.8)
        ax.set_title(title, fontsize=16)
    fig.suptitle("原始焊接鈑金掃描 → 保來源的工程草稿 → 可編輯 CAD 線弧", fontsize=21, color="#244153")
    fig.text(.5, .025, "外形／孔洞／短肢／板金不變形；342,369 三角面。彩線為衍生草稿，尚未通過製造貼合驗收。", ha="center", fontsize=12)
    fig.savefig(args.output / "case-a-overview.png", dpi=135); plt.close(fig)

    fig, axes=plt.subplots(2,2,figsize=(14,10),layout="constrained",facecolor="#f1f5f9")
    regions=[(-550,-440,-50,80,"大孔座與板金接合"),(-240,-65,45,220,"中央壓形與短肢過渡"),
             (-195,-45,235,390,"上端孔座與表面起伏"),(-85,12,-25,115,"短端套筒與板金肩部")]
    for ax,(xmin,xmax,ymin,ymax,title) in zip(axes.flat,regions):
        projection=draw_mesh(ax,[.35,-.65,.68])
        mean=world.mean(1)
        mask=(mean[:,0]>=xmin)&(mean[:,0]<=xmax)&(mean[:,1]>=ymin)&(mean[:,1]<=ymax)
        local=(triangles[mask]@projection.T).reshape(-1,2)
        lo,hi=local.min(0)-4,local.max(0)+4
        ax.set_xlim(lo[0],hi[0]);ax.set_ylim(lo[1],hi[1]);ax.set_title(title,fontsize=15)
    fig.suptitle("原件接合與焊道脈絡｜等尺度局部觀察，不改形、不誇大 Z",fontsize=20,color="#244153")
    fig.text(.5,.015,"區域為人工展示窗口，未宣稱已自動分割焊道、辨識材料或認證圓角結束點。",ha="center",fontsize=11)
    fig.savefig(args.output/"case-a-joints.png",dpi=145);plt.close(fig)

    frame_ids = ["F10", "F37", "F54", "F75"]
    chosen = [f for key in frame_ids for f in job["frames"] if f["id"] == key]
    if len(chosen) == 4:
        fig = plt.figure(figsize=(16, 12), layout="constrained", facecolor="#f8fafc")
        grid = fig.add_gridspec(3, 2, height_ratios=[1.15, 1, 1])
        context = fig.add_subplot(grid[0,:]); draw_mesh(context, [0,0,1], [1,0,0])
        for frame, color in zip(chosen, ["#007c91", "#b6539a", "#c87215", "#6348b2"]):
            endpoints = np.array([point(frame["p1"]), point(frame["p2"])])-center[:2]
            context.plot(endpoints[:,0], endpoints[:,1], color=color, lw=2)
            context.annotate(frame["id"], endpoints.mean(0), color=color, fontsize=13, xytext=(5,7), textcoords="offset points")
        context.set_title("實際剖框位置｜原孔洞與接合區完整保留")
        for index, frame in enumerate(chosen):
            ax = fig.add_subplot(grid[1+index//2,index%2])
            normal, origin, h = point(frame["normal"]), point(frame["origin"]), point(frame["horizontal"])
            d = world[:,:,:2] @ normal+frame["offset"]
            active = np.flatnonzero((d.min(1)<0)&(d.max(1)>0))
            all_edges = {}
            for a,b in [(0,1),(1,2),(2,0)]:
                mask = active[d[active,a]*d[active,b]<0]
                t = d[mask,a]/(d[mask,a]-d[mask,b])
                points = world[mask,a]+(world[mask,b]-world[mask,a])*t[:,None]
                for face,p in zip(mask,points): all_edges.setdefault(int(face),[]).append(p)
            edges = {}
            for face,p in all_edges.items():
                if len(p)!=2: continue
                p=np.array(p); uz=np.c_[(p[:,:2]-origin)@h,p[:,2]]
                if uz[:,0].max()<frame["uMin"] or uz[:,0].min()>frame["uMax"]: continue
                du=uz[1,0]-uz[0,0]
                if abs(du)>1e-12:
                    ts=sorted(np.clip([(frame["uMin"]-uz[0,0])/du,(frame["uMax"]-uz[0,0])/du],0,1))
                    uz=uz[0]+np.array(ts)[:,None]*(uz[1]-uz[0])
                edges[face]=uz
            ax.add_collection(LineCollection(list(edges.values()),colors="#a4afbc",linewidths=.9))
            for role,color in [("cap","#1684a4"),("wall","#d17a16")]:
                faces={f for c in frame["corners"] for f in c[role]["faces"]}
                ax.add_collection(LineCollection([p for f,p in edges.items() if f in faces],colors=color,linewidths=1.8,label=role+" 支持域"))
            p=np.array(list(edges.values())).reshape(-1,2)
            lo,hi=p.min(0)-3,p.max(0)+3
            ax.set_xlim(lo[0],hi[0]); ax.set_ylim(lo[1],hi[1]); ax.set_aspect("equal"); ax.grid(alpha=.2)
            audit=next(a for a in data["source"]["frames"] if a["id"]==frame["id"])
            descriptions={"F10":"一般候選：工程寬度比較", "F37":"完整蓋支持重用：保留未定", "F54":"相鄰框蓋身分切換：審查排除", "F75":"多重蓋／壁與短窄帶：審查排除"}
            ax.set_title(frame["id"]+"｜"+descriptions[frame["id"]]+"\n"+audit["state"],fontsize=12)
            ax.set_xlabel("剖面 u (mm)");ax.set_ylabel("Z (mm)")
            ax.legend(fontsize=9)
        fig.suptitle("困難剖框對照｜每條原面交線獨立繪製，缺口不偷偷連接",fontsize=20)
        fig.savefig(args.output / "case-a-sections.png",dpi=145);plt.close(fig)

    fig,axes=plt.subplots(2,3,figsize=(16,9),layout="constrained",facecolor="#f8fafc")
    for index,(side,draft) in enumerate(zip(data["run"]["sides"],data["draft"]["sides"])):
        raw=np.array([[p["x"],p["y"]] for p in draft["points"]])
        supports=np.array([[p["point"]["x"],p["point"]["y"]] for p in draft["sourceSupports"]])
        errors=finite_errors(raw,side["rounded"]["curves"])
        station=np.r_[0,np.linalg.norm(np.diff(raw,axis=0),axis=1).cumsum()]
        equal=np.linspace(0,station[-1],int(np.ceil(station[-1]/.5))+1)
        uniform=np.c_[np.interp(equal,station,raw[:,0]),np.interp(equal,station,raw[:,1])]
        rounded_max=max(errors.max(),finite_errors(uniform,side["rounded"]["curves"]).max())
        if verified and abs(verified["sides"][index]["cadXYMaximum"]-rounded_max)>1e-7:
            raise ValueError("Independent plot distance differs from the verified aggregate")
        print(f"{side['id']} rounded plot maximum XY: {rounded_max:.12f} mm",flush=True)
        worst=raw[errors.argmax()]
        for row in range(2):
            ax=axes[row,index]; ax.plot(raw[:,0],raw[:,1],color="#db8a21",lw=2,label="衍生工程草稿")
            ax.scatter(supports[:,0],supports[:,1],s=13,color="#1684a4",label="原支持端點")
            for j,c in enumerate(side["rounded"]["curves"]):
                    p=sample(c);ax.plot(p[:,0],p[:,1],color="#704ab7" if c.get("center") else "#047c73",lw=1.2,label="取整直線" if j==0 else "取整圓弧" if j==1 else None)
            ax.scatter(*worst,s=90,facecolors="none",edgecolors="#c73545",label="取整後最差原草稿點")
            ax.set_aspect("equal");ax.grid(alpha=.2);ax.set_xlabel("X (mm)");ax.set_ylabel("Y (mm)")
            if row==1:
                ax.set_xlim(worst[0]-8,worst[0]+8);ax.set_ylim(worst[1]-8,worst[1]+8)
                ax.set_title("最差點局部｜16 × 16 mm；無變形放大")
            else:
                ax.set_title(f"{side['id']}｜{len(side['fit']['baseline'])} → {len(side['fit']['curves'])} 段\n取整後最大 XY {rounded_max:.6f} mm")
    fig.legend(*axes[0,0].get_legend_handles_labels(),loc="outside lower center",ncol=5,fontsize=10,frameon=False)
    fig.suptitle("保公差減段與 0.01 mm 尺寸重建｜原支持、推估與最差點分開顯示",fontsize=20)
    fig.savefig(args.output / "case-a-fitting.png",dpi=145);plt.close(fig)
    print("Redrawn unchanged source geometry, section evidence and actual fitting overlays.")


if __name__=="__main__": main()
