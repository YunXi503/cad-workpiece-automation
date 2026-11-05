namespace Portfolio;

public sealed record CapSupport(double Width, double Height, string Role, Edge[] CapEdges, Edge[] WallEdges, Station? Anchor);
public sealed record ShellSection(CapSupport[] Candidates, Station Support);

public static class ShellSections
{
    // Engineering support policy, in mm. All rejected candidates remain in section output.
    private const double MinimumCapSpan = 8;
    private const double MinimumWallSpan = 2;
    private const double GuideReach = 4;
    private const double DirectionRatio = .2;
    private const double EndpointTolerance = 1e-7;

    public static ShellSection Inspect(Edge[][] components, XY origin, XY normal, int index)
    {
        XY transverse = normal.Left();
        double U(XYZ p) => (p.Planar() - origin).Dot(transverse);
        var caps = new List<(Edge[] Edges, Edge[] Component, XYZ End, double Width, double Height)>();
        foreach (Edge[] component in components)
        {
            Edge[] horizontal = component.Where(e => Math.Abs(e.A.Z - e.B.Z)
                <= DirectionRatio * (e.A.Planar() - e.B.Planar()).Norm()).ToArray();
            foreach (Edge[] cap in Slicing.Components(horizontal))
            {
                XYZ[] ends = cap.SelectMany(e => new[] { e.A, e.B }).ToArray();
                XYZ outer = ends.MinBy(p => Math.Abs(U(p)));
                double width = ends.Max(U) - ends.Min(U);
                if (width >= MinimumCapSpan && Math.Abs(U(outer)) <= GuideReach)
                    caps.Add((cap, component, outer, width, ends.Average(p => p.Z)));
            }
        }
        if (caps.Count != 2)
            return new(caps.Select(c => new CapSupport(c.Width, c.Height, "unpaired", c.Edges, [], null)).ToArray(),
                new(index, caps.Count == 0 ? "gap" : "ambiguous", null, [], null));

        double middle = caps.Average(c => c.Height);
        var candidates = new List<CapSupport>();
        foreach (var cap in caps)
        {
            string role = cap.Height > middle ? "upper" : "lower";
            XYZ cursor = cap.End;
            var remaining = cap.Component.Except(cap.Edges).ToList();
            var wall = new List<Edge>();
            XYZ? anchor = null;
            for (int step = 0; step < cap.Component.Length; step++)
            {
                Edge[] next = remaining.Where(e => (e.A - cursor).Norm() <= EndpointTolerance
                    || (e.B - cursor).Norm() <= EndpointTolerance).ToArray();
                if (next.Length == 0) break;
                Edge edge = next[0];
                XYZ end = (edge.A - cursor).Norm() <= EndpointTolerance ? edge.B : edge.A;
                // Adjacent STL faces can report the same section edge twice.
                if (next.Any(e => ((e.A - cursor).Norm() <= EndpointTolerance ? e.B - end : e.A - end)
                    .Norm() > EndpointTolerance)) break;
                foreach (Edge duplicate in next) remaining.Remove(duplicate);
                bool vertical = (end.Planar() - cursor.Planar()).Norm() <= DirectionRatio * Math.Abs(end.Z - cursor.Z);
                if (vertical)
                {
                    if (anchor is null) anchor = cursor;
                    wall.AddRange(next);
                }
                else if (wall.Count > 0) break;
                cursor = end;
            }
            Station? support = null;
            if (anchor is XYZ p && wall.Count > 0 && wall.Max(e => Math.Max(e.A.Z, e.B.Z))
                - wall.Min(e => Math.Min(e.A.Z, e.B.Z)) >= MinimumWallSpan
                && (role == "upper" ? p.Z > middle : p.Z < middle))
                support = new(index, "observed", p.Planar(), [wall[0].Triangle], role, p.Z);
            candidates.Add(new(cap.Width, cap.Height, role, cap.Edges, [.. wall], support));
        }
        CapSupport[] ordered = candidates.OrderByDescending(c => c.Width).ToArray();
        if (Math.Abs(ordered[0].Width - ordered[1].Width) <= EndpointTolerance)
            return new([.. candidates], new(index, "ambiguous", null, [], null));
        return new([.. candidates], ordered[0].Anchor ?? new(index, "gap", null, [], null));
    }

    public static Station[] Trace(Triangle[] mesh, XYZ[] guide, Guide side)
    {
        if (side.Start < 0 || side.Count < 5 || side.Count > guide.Length - side.Start)
            throw new InvalidDataException("Invalid shell guide interval.");
        var rows = new List<Station>();
        for (int i = 0; i < side.Count; i++)
        {
            var (origin, normal) = Slicing.Frame(guide, side, i);
            rows.Add(Inspect(Slicing.Components(Slicing.Intersect(mesh, origin, normal, 110)), origin, normal, i).Support);
        }
        Station[] observed = [.. rows], result = [.. rows];
        for (int i = 0; i < observed.Length; i++)
        {
            if (observed[i].Point is not null) continue;
            int left = i - 1, right = i + 1;
            while (left >= 0 && observed[left].Point is null) left--;
            while (right < observed.Length && observed[right].Point is null) right++;
            if (left < 0 || right >= observed.Length || observed[left].Cover != observed[right].Cover) continue;
            var (a, na) = Slicing.Frame(guide, side, left);
            var (b, nb) = Slicing.Frame(guide, side, right);
            var (p, n) = Slicing.Frame(guide, side, i);
            double t = (double)(i - left) / (right - left);
            double da = (observed[left].Point!.Value - a).Dot(na.Left());
            double db = (observed[right].Point!.Value - b).Dot(nb.Left());
            result[i] = new(i, "inferred", p + n.Left() * (da + (db - da) * t), [], observed[left].Cover,
                observed[left].Height + (observed[right].Height - observed[left].Height) * t, observed[i].State);
        }
        return result;
    }
}
