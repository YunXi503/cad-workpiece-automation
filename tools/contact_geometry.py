"""Closest points on finite extruded line/arc chains, independent of display tessellation.

Standard library only. This supports figure measurements, not contact-band selection.
"""
import math


def closest_curve(point, curve):
    """Return the closest XY point on a finite line or signed circular arc."""
    p = (point[0], point[1])
    a = (curve["start"]["x"], curve["start"]["y"])
    b = (curve["end"]["x"], curve["end"]["y"])
    if curve.get("center") is None:
        d = (b[0]-a[0], b[1]-a[1])
        length2 = d[0]*d[0]+d[1]*d[1]
        if length2 == 0:
            raise ValueError("Degenerate finite line")
        t = max(0., min(1., ((p[0]-a[0])*d[0]+(p[1]-a[1])*d[1])/length2))
        return (a[0]+t*d[0], a[1]+t*d[1])
    o = (curve["center"]["x"], curve["center"]["y"])
    radius = math.dist(a, o)
    sweep = curve["sweep"]
    if radius == 0 or not 0 < abs(sweep) < 2*math.pi:
        raise ValueError("Degenerate or unsupported signed finite arc")
    candidates = [a, b]
    r = math.dist(p, o)
    if r > 0:
        angle = math.atan2(p[1]-o[1], p[0]-o[0])-math.atan2(a[1]-o[1], a[0]-o[0])
        progress = (angle*(1 if sweep > 0 else -1)) % (2*math.pi)
        if progress <= abs(sweep):
            candidates.append((o[0]+radius*(p[0]-o[0])/r, o[1]+radius*(p[1]-o[1])/r))
    return min(candidates, key=lambda q: math.dist(p, q))


def closest_surface(point, curves, z_min, z_max):
    """Return (XYZ foot, primitive index, XY gap, height gap, 3D distance)."""
    if not curves or z_min >= z_max or not all(math.isfinite(v) for v in (*point, z_min, z_max)):
        raise ValueError("Invalid finite surface input")
    choices = [(closest_curve(point, c), i) for i, c in enumerate(curves)]
    q, index = min(choices, key=lambda v: math.dist(point[:2], v[0]))
    z = max(z_min, min(z_max, point[2]))
    xy, height = math.dist(point[:2], q), abs(point[2]-z)
    return (*q, z), index, xy, height, math.hypot(xy, height)
