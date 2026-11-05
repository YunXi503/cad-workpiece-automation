"""Analytic controls for surface figure feet, plus public aggregate identity checks.

No private source files, plotting packages or licensed CAD required.
"""
import json
import hashlib
import math
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/"tools"))
from contact_geometry import closest_surface


def line(a, b):
    return dict(start=dict(zip(("x", "y"), a)), end=dict(zip(("x", "y"), b)), center=None, sweep=0)


def arc(sign):
    return dict(start=dict(x=2., y=0.), end=dict(x=0., y=sign*2.), center=dict(x=0., y=0.), sweep=sign*math.pi/2)


def near(actual, expected):
    assert math.dist(actual, expected) <= 1e-12, (actual, expected)


def main():
    finite = line((0., 0.), (2., 0.))
    controls = [
        ((1., 3., 0.), [finite], (1., 0., 0.), 3.),
        ((-1., 0., 0.), [finite], (0., 0., 0.), 1.),
        ((3., 0., 0.), [finite], (2., 0., 0.), 1.),
        ((1., 3., 5.), [finite], (1., 0., 1.), 5.),
        ((1., 0., -3.), [finite], (1., 0., -1.), 2.),
        ((3., 3., 0.), [arc(1)], (math.sqrt(2), math.sqrt(2), 0.), math.sqrt(18)-2),
        ((3., -3., 0.), [arc(-1)], (math.sqrt(2), -math.sqrt(2), 0.), math.sqrt(18)-2),
        ((0., -2., 0.), [arc(1)], (2., 0., 0.), math.sqrt(8)),
        ((0., 2., 0.), [arc(-1)], (2., 0., 0.), math.sqrt(8)),
        ((0., 0., 0.), [arc(1)], (2., 0., 0.), 2.),
        ((1., 0., 0.), [line((10., 0.), (12., 0.)), finite], (1., 0., 0.), 0.),
    ]
    for p, curves, foot, distance in controls:
        q, _, xy, dz, d = closest_surface(p, curves, -1., 1.)
        near(q, foot)
        assert abs(d-distance) <= 1e-12
        assert abs(d-math.dist(p, q)) <= 1e-12 and abs(d-math.hypot(xy, dz)) <= 1e-12
    for p, curves, low, high in [((0., 0., 0.), [], -1., 1.), ((math.nan, 0., 0.), [finite], -1., 1.), ((0., 0., 0.), [finite], 1., -1.)]:
        try:
            closest_surface(p, curves, low, high)
        except ValueError:
            pass
        else:
            raise AssertionError("Invalid surface input accepted")
    read = lambda name: json.loads((ROOT/"docs/evidence"/name).read_text())
    display, geometry, native = read("case-a-surface-contact.json"), read("case-a-geometry.json"), read("case-a-vsta.json")
    assert display["cadPlanSha256"] == geometry["cadPlanSha256"] == native["planSha256"]
    assert display["meshSha256"] in geometry["inputFingerprint"].split(":")
    assert display["originalTriangles"] == geometry["originalTriangles"]
    assert display["zMin"] == -50 and display["zMax"] == 50 and display["thresholdMm"] == .3
    assert display["zMin"] == native["surfaces"]["zMin"] and display["zMax"] == native["surfaces"]["zMax"]
    assert not display["physicalContactAccepted"]
    for d, g in zip(display["sides"], geometry["sides"], strict=True):
        assert d["id"] == g["id"] and d["probes"] == g["sourceSupportEndpoints"]
        assert d["withinLimit"]+d["overLimit"] == d["probes"]
        assert abs(d["maximumMm"]-g["sourceAnchorFiniteSurfaceMaximumMm"]) <= 1e-7
    assert sum(s["probes"] for s in display["sides"]) == 119
    assert [s["overLimit"] for s in display["sides"]] == [0, 3, 3]
    assert len(display["figures"]) == 3
    for figure in display["figures"]:
        image = (ROOT/figure["path"]).read_bytes()
        assert image.startswith(b"\x89PNG\r\n\x1a\n")
        assert hashlib.sha256(image).hexdigest() == figure["sha256"]
    print("Surface display: 11 finite-foot controls, 3 invalid inputs, exact plan bindings and 119-probe aggregates passed.")


if __name__ == "__main__":
    main()
