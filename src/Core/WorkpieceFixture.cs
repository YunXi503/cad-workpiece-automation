namespace Portfolio;

// Independently dimensioned open sheet, with three guide intervals on one scan.
internal static class WorkpieceFixture
{
    private readonly record struct Sample(XY Point, XY Direction);

    public static void Create(List<XYZ> points, List<Triangle> mesh, List<Guide> guides)
    {
        (double Offset, double Z)[] profile =
            [(6, -1.2), (1.3, -.9), (0, 0), (0, 4), (-1, 4.35), (.3, 5), (7.5, 5.55)];
        (string Id, double Entry, double Radius, double Turn, double Exit)[] sides =
            [("edge-a", 72.173, 12.217, 1.20, 30.286), ("edge-b", 25.213, 11.327, 1.27, 38.145),
             ("edge-c", 33.186, 10.517, 1.12, 29.232)];
        XY start = new(0, 0), direction = new(1, 0);
        foreach (var side in sides)
        {
            XY p = start + direction * side.Entry, center = p + direction.Left() * side.Radius;
            XY q = center + (p - center).Rotate(side.Turn), exit = direction.Rotate(side.Turn);
            XY end = q + exit * side.Exit;
            Curve[] curves = [new(start, p), new(p, q, center, side.Turn), new(q, end)];
            var path = new List<Sample>();
            foreach (Curve curve in curves)
            {
                int count = (int)Math.Ceiling(curve.Length / 1.15);
                for (int i = path.Count == 0 ? 0 : 1; i <= count; i++)
                {
                    double t = (double)i / count;
                    path.Add(new(curve.At(t), curve.Tangent(t)));
                }
            }

            guides.Add(new(side.Id, points.Count, path.Count, "cover-A"));
            points.AddRange(path.Select(s => At(s, .18, 8)));
            for (int i = 0; i < path.Count - 1; i++)
            {
                // Scan dropouts interrupt the wall, while the upper cap remains visible.
                bool dropout = (side.Id == "edge-b" && i is >= 8 and <= 12)
                    || (side.Id == "edge-c" && i is 8 or 9);
                Strip(path[i], path[i + 1], dropout ? 3 : 0);
                if (side.Id == "edge-b" && i is >= 25 and <= 32)
                    Quad(At(path[i], -1.45, .15), At(path[i + 1], -1.45, .15),
                        At(path[i + 1], -1.45, 3.85), At(path[i], -1.45, 3.85));
            }

            start = end + exit * 4;
            direction = exit;
            Strip(path[^1], new(start, direction), 0);
        }

        // A tube appears in the scan beyond the final guide interval.
        const int facets = 24;
        for (int i = 0; i < facets; i++)
        {
            double a = i * 2 * Math.PI / facets, b = (i + 1) * 2 * Math.PI / facets;
            Quad(Tube(a, 4), Tube(b, 4), Tube(b, 22), Tube(a, 22));
        }

        XYZ Tube(double angle, double length) => At(new(start + direction * length, direction),
            3 + 4.3 * Math.Cos(angle), 2.4 + 4.3 * Math.Sin(angle));

        void Strip(Sample a, Sample b, int first)
        {
            for (int j = first; j < profile.Length - 1; j++)
            {
                var (u, za) = profile[j];
                var (v, zb) = profile[j + 1];
                Quad(At(a, u, za), At(b, u, za), At(b, v, zb), At(a, v, zb));
            }
        }

        void Quad(XYZ a, XYZ b, XYZ c, XYZ d)
        {
            mesh.Add(new(a, b, c));
            mesh.Add(new(a, c, d));
        }
    }

    private static XYZ At(Sample sample, double offset, double z)
    {
        XY p = sample.Point + sample.Direction.Left() * offset;
        return new(p.X, p.Y, z);
    }
}
