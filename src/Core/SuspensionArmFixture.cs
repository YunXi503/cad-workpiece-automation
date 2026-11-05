namespace Portfolio;

internal static class SuspensionArmFixture
{
    private sealed record Sample(XY Point, XY Tangent, double Station);

    public static void Generate(string folder, string caseId)
    {
        bool second = caseId == "arm-b";
        XY[] polygon = second
            ? [new(0, 0), new(130, -35), new(265, 38), new(242, 105), new(70, 155), new(-28, 108)]
            : [new(0, 0), new(150, -22), new(290, 55), new(230, 95), new(55, 170), new(-32, 125)];
        double[] radii = second ? [16, 23, 18, 15, 26, 12] : [18, 28, 16, 20, 24, 14];
        var starts = new XY[polygon.Length];
        var ends = new XY[polygon.Length];
        var arcs = new Curve[polygon.Length];
        XY centroid = polygon.Aggregate(new XY(), (sum, p) => sum + p) * (1.0 / polygon.Length);
        for (int i = 0; i < polygon.Length; i++)
        {
            XY u = (polygon[i] - polygon[(i + polygon.Length - 1) % polygon.Length]).Unit();
            XY v = (polygon[(i + 1) % polygon.Length] - polygon[i]).Unit();
            double turn = Math.Atan2(u.Cross(v), u.Dot(v));
            double trim = radii[i] * Math.Tan(turn / 2);
            starts[i] = polygon[i] - u * trim;
            ends[i] = polygon[i] + v * trim;
            arcs[i] = new(starts[i], ends[i], starts[i] + u.Left() * radii[i], turn);
        }

        var mesh = new List<Triangle>();
        var groups = new List<string>();
        var asc = new List<XYZ>();
        var guides = new List<Guide>();
        var nominal = new List<Curve[]>();
        double station = 0;
        for (int side = 0; side < 3; side++)
        {
            int first = 2 * side, last = first + 1;
            XY before = (ends[(first + 5) % 6] + starts[first]) * .5;
            XY after = (ends[last] + starts[(last + 1) % 6]) * .5;
            Curve[] chain = [new(before, starts[first]), arcs[first], new(ends[first], starts[last]), arcs[last], new(ends[last], after)];
            nominal.Add(chain);
            var path = new List<Sample>();
            foreach (Curve curve in chain)
            {
                int count = (int)Math.Ceiling(curve.Length / 1.1);
                for (int i = path.Count == 0 ? 0 : 1; i <= count; i++)
                {
                    double t = (double)i / count;
                    path.Add(new(curve.At(t), curve.Tangent(t), station + curve.Length * t));
                }
                station += curve.Length;
            }
            guides.Add(new("arm-" + (side + 1), asc.Count, path.Count, "auto"));
            asc.AddRange(path.Select(p => new XYZ(p.Point.X + p.Tangent.Left().X * .2,
                p.Point.Y + p.Tangent.Left().Y * .2, 30)));
            for (int i = 0; i + 1 < path.Count; i++)
            {
                var a = Profile(path[i]);
                var b = Profile(path[i + 1]);
                for (int j = 0; j < a.Length; j++)
                {
                    if (caseId == "arm-gap" && side == 1 && i is >= 40 and <= 44 && j is >= 5 and <= 7) continue;
                    int k = (j + 1) % a.Length;
                    string role = j == 0 ? "upper-shell" : j is >= 1 and <= 5 ? "upper-bend" : j == 6 ? "upper-wall"
                        : j is >= 7 and <= 10 ? "weld" : j == 11 ? "lower-wall" : j is >= 12 and <= 16 ? "lower-bend"
                        : j == 17 ? "lower-shell" : "inner-wall";
                    Quad(a[j], b[j], b[k], a[k], role);
                }
            }
        }

        for (int mount = 0; mount < 3; mount++)
        {
            XY vertex = polygon[2 * mount];
            XY inward = (centroid - vertex).Unit();
            XY center = vertex + inward * 10;
            XY axis = inward.Left();
            const int facets = 48;
            for (int i = 0; i < facets; i++)
            {
                double a = i * 2 * Math.PI / facets, b = (i + 1) * 2 * Math.PI / facets;
                Quad(Tube(a, -11, 11), Tube(b, -11, 11), Tube(b, 11, 11), Tube(a, 11, 11), "mount-sleeve");
                Quad(Tube(a, 11, 7), Tube(b, 11, 7), Tube(b, -11, 7), Tube(a, -11, 7), "mount-bore");
                Quad(Tube(a, -11, 7), Tube(b, -11, 7), Tube(b, -11, 11), Tube(a, -11, 11), "mount-rim");
                Quad(Tube(a, 11, 11), Tube(b, 11, 11), Tube(b, 11, 7), Tube(a, 11, 7), "mount-rim");
            }
            XYZ Tube(double theta, double along, double radius)
            {
                XY p = center + axis * along + inward * (radius * Math.Cos(theta));
                return new(p.X, p.Y, 7 + radius * Math.Sin(theta));
            }
        }

        if (caseId == "arm-reversed")
            foreach (Guide guide in guides) asc.Reverse(guide.Start, guide.Count);
        Directory.CreateDirectory(folder);
        Data.WriteStl(Path.Combine(folder, "scan.stl"), mesh);
        File.WriteAllText(Path.Combine(folder, "guide.asc"), string.Join('\n', asc.Select(Data.Coordinates)) + "\n");
        Data.Write(Path.Combine(folder, "job.json"), new Job("synthetic-job/1", caseId, [.. guides],
            0, 0, caseId == "arm-height" ? 15 : -12, 24, "formed-shell"));
        Data.Write(Path.Combine(folder, "design.json"), new
        {
            schema = "synthetic-arm-design/1", units = "mm", polygon, radii, nominal,
            triangleRoles = groups, purpose = "Authoring and visualization only. The solver reads scan.stl, guide.asc and job.json."
        });

        XYZ[] Profile(Sample sample)
        {
            XY n = sample.Tangent.Left();
            double top = 11 + 1.4 * Math.Sin(sample.Station / 61);
            double bottom = -7 + .9 * Math.Sin(sample.Station / 73);
            double seam = 1 + .6 * Math.Sin(sample.Station / 38);
            double amplitude = 1.6 + .45 * Math.Sin(sample.Station * .43);
            XYZ At(double u, double z) => new(sample.Point.X + n.X * u, sample.Point.Y + n.Y * u, z);
            double upperScale = caseId == "arm-switch"
                ? .30 + .27 * Math.Exp(-Math.Pow((sample.Station - 355) / 42, 2)) : .30;
            XY upperInner = centroid + (sample.Point - centroid) * upperScale;
            XY lowerInner = centroid + (sample.Point - centroid) * .42;
            var profile = new List<XYZ> { new(upperInner.X, upperInner.Y, top), At(1.6, top) };
            for (int i = 1; i <= 5; i++)
            {
                double a = Math.PI / 2 + Math.PI / 2 * i / 5;
                profile.Add(At(1.6 + 1.6 * Math.Cos(a), top - 1.6 + 1.6 * Math.Sin(a)));
            }
            profile.Add(At(0, seam + 1.1));
            profile.Add(At(-amplitude * .55, seam + .72));
            profile.Add(At(-amplitude, seam));
            profile.Add(At(-amplitude * .6, seam - .65));
            profile.Add(At(.3, seam - 1.1));
            profile.Add(At(.3, bottom + 1.8));
            for (int i = 1; i <= 5; i++)
            {
                double a = Math.PI + Math.PI / 2 * i / 5;
                profile.Add(At(2.1 + 1.8 * Math.Cos(a), bottom + 1.8 + 1.8 * Math.Sin(a)));
            }
            profile.Add(new(lowerInner.X, lowerInner.Y, bottom));
            return [.. profile];
        }

        void Quad(XYZ a, XYZ b, XYZ c, XYZ d, string group)
        {
            mesh.Add(new(a, b, c)); groups.Add(group);
            mesh.Add(new(a, c, d)); groups.Add(group);
        }
    }
}
