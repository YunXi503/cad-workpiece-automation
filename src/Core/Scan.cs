using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Portfolio;

public sealed record Guide(string Id, int Start, int Count, string Cover);
public sealed record Job(string Schema, string Case, Guide[] Guides, double ProbeZ,
    double PlaneZ, double ZMin, double ZMax);
public sealed record Triangle(XYZ A, XYZ B, XYZ C);
public sealed record Edge(XYZ A, XYZ B, int Triangle);
public sealed record Station(int Index, string State, XY? Point, int[] Triangles, string? Cover);
public sealed record SideResult(string Id, Station[] Stations, FitResult? Fit, RoundResult? Rounded, string? Issue);
public sealed record RunResult(string Schema, string Case, string InputFingerprint, SideResult[] Sides);
public sealed record CadSide(string Id, Curve[] Curves);
public sealed record CadPlan(string Schema, string Units, string Source, double PlaneZ,
    double ZMin, double ZMax, CadSide[] Sides);

public static class Data
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    public static T Read<T>(string file) => JsonSerializer.Deserialize<T>(File.ReadAllText(file), Json)
        ?? throw new InvalidDataException("Empty JSON: " + Path.GetFileName(file));

    public static void Write<T>(string file, T value) =>
        File.WriteAllText(file, JsonSerializer.Serialize(value, Json) + "\n");

    public static string Sha(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();

    public static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    public static string Coordinates(XYZ p) => $"{Number(p.X)} {Number(p.Y)} {Number(p.Z)}";
    public static XYZ Point(string line)
    {
        string[] fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 3) throw new InvalidDataException("Expected three XYZ coordinates.");
        double Parse(string s)
        {
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                || !double.IsFinite(value) || Math.Abs(value) > 100_000)
                throw new InvalidDataException("Nonfinite or out-of-range coordinate.");
            return value;
        }
        return new(Parse(fields[0]), Parse(fields[1]), Parse(fields[2]));
    }

    public static Triangle[] LoadStl(string path)
    {
        string[] lines = File.ReadLines(path).Select(line => line.Trim())
            .Where(line => line.Length > 0).ToArray();
        if (lines.Length < 9 || !lines[0].StartsWith("solid ", StringComparison.Ordinal)
            || !lines[^1].StartsWith("endsolid", StringComparison.Ordinal)
            || (lines.Length - 2) % 7 != 0)
            throw new InvalidDataException("Expected an ASCII STL solid with seven lines per facet.");
        var triangles = new List<Triangle>();
        for (int i = 1; i < lines.Length - 1; i += 7)
        {
            if (!lines[i].StartsWith("facet normal ", StringComparison.Ordinal) || lines[i + 1] != "outer loop"
                || lines[i + 5] != "endloop" || lines[i + 6] != "endfacet")
                throw new InvalidDataException("Malformed ASCII STL facet.");
            XYZ Vertex(int offset) => lines[i + offset].StartsWith("vertex ", StringComparison.Ordinal)
                ? Point(lines[i + offset][7..]) : throw new InvalidDataException("Missing STL vertex.");
            triangles.Add(new(Vertex(2), Vertex(3), Vertex(4)));
        }
        return [.. triangles];
    }

    public static void WriteStl(string path, IEnumerable<Triangle> triangles)
    {
        using var writer = new StreamWriter(path) { NewLine = "\n" };
        writer.WriteLine("solid synthetic");
        foreach (var t in triangles)
        {
            writer.WriteLine("facet normal 0 0 0\nouter loop");
            foreach (var p in new[] { t.A, t.B, t.C }) writer.WriteLine("vertex " + Coordinates(p));
            writer.WriteLine("endloop\nendfacet");
        }
        writer.WriteLine("endsolid synthetic");
    }
}

public static class Fixtures
{
    public static readonly string[] Cases = ["normal", "gap", "height", "reversed", "competing"];

    public static void Generate(string folder, string caseId)
    {
        if (!Cases.Contains(caseId)) throw new InvalidDataException("Unknown synthetic case: " + caseId);
        Directory.CreateDirectory(folder);
        var points = new List<XYZ>();
        var mesh = new List<Triangle>();
        var guides = new List<Guide>();
        for (int side = 0; side < 3; side++)
        {
            double theta = (side == 1 ? -1 : 1) * (.9 + .13 * side);
            XY p = new(11.173 + side, 0);
            XY center = p + new XY(0, Math.Sign(theta) * (4.217 + .21 * side));
            XY q = center + (p - center).Rotate(theta);
            XY end = q + new XY(8.286 + side, 0).Rotate(theta);
            Curve[] curves = [new(new XY(0, 0), p), new(p, q, center, theta), new(q, end)];
            var path = new List<(XY P, XY N)>();
            foreach (Curve curve in curves)
            {
                int steps = (int)Math.Ceiling(curve.Length / .45);
                for (int k = path.Count == 0 ? 0 : 1; k <= steps; k++)
                {
                    double t = (double)k / steps;
                    path.Add((curve.At(t), curve.Tangent(t).Left()));
                }
            }

            XY origin = new(3.25 + side * 45, 1.7 + 8 * side);
            double rotation = .24 * side;
            XYZ World((XY P, XY N) sample, double offset, double z)
            {
                XY p = origin + (sample.P + sample.N * offset).Rotate(rotation);
                return new(p.X, p.Y, z);
            }
            var samples = path.Select(t => World(t, 0, 8)).ToArray();
            if (caseId == "reversed" && side == 1) Array.Reverse(samples);
            guides.Add(new("side-" + (side + 1), points.Count, path.Count, "cover-A"));
            points.AddRange(samples);

            (double Offset, double Z)[] profile = [(-1.2, -1), (0, 0), (0, 4), (.23, 4.4), (-1.1, 5.1)];
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (caseId == "gap" && side == 1 && i >= 7 && i <= 11) continue;
                if (caseId == "competing" && side == 1 && i >= 14 && i <= 20)
                {
                    XYZ a = World(path[i], .82, .2), b = World(path[i + 1], .82, .2);
                    XYZ c = World(path[i + 1], .82, 3.8), d = World(path[i], .82, 3.8);
                    mesh.Add(new(a, b, c));
                    mesh.Add(new(a, c, d));
                }
                for (int j = 0; j < profile.Length - 1; j++)
                {
                    var (a, za) = profile[j];
                    var (b, zb) = profile[j + 1];
                    XYZ p0 = World(path[i], a, za), p1 = World(path[i + 1], a, za);
                    XYZ q0 = World(path[i], b, zb), q1 = World(path[i + 1], b, zb);
                    mesh.Add(new(p0, p1, q1));
                    mesh.Add(new(p0, q1, q0));
                }
            }
        }

        File.WriteAllText(Path.Combine(folder, "guide.asc"), string.Join('\n', points.Select(Data.Coordinates)) + "\n");
        Data.WriteStl(Path.Combine(folder, "scan.stl"), mesh);
        Data.Write(Path.Combine(folder, "job.json"),
            new Job("synthetic-job/1", caseId, [.. guides], 2, 2, caseId == "height" ? 2.7 : 0, 5));
    }
}

public static class Slicing
{
    private const double Eps = 1e-8;

    public static Edge[] Intersect(Triangle[] mesh, XY station, XY unitNormal, double halfWidth)
    {
        var edges = new List<Edge>();
        for (int i = 0; i < mesh.Length; i++)
        {
            Triangle t = mesh[i];
            XYZ[] vertices = [t.A, t.B, t.C];
            double[] d = vertices.Select(p => (p.Planar() - station).Dot(unitNormal)).ToArray();
            if (d.All(x => x > Eps) || d.All(x => x < -Eps) || d.All(x => Math.Abs(x) <= Eps))
                continue;

            var hits = new List<XYZ>(3);
            for (int j = 0; j < 3; j++)
            {
                int k = (j + 1) % 3;
                if (Math.Abs(d[j]) <= Eps) hits.Add(vertices[j]);
                if ((d[j] < -Eps && d[k] > Eps) || (d[j] > Eps && d[k] < -Eps))
                    hits.Add(vertices[j] + (vertices[k] - vertices[j]) * (d[j] / (d[j] - d[k])));
            }
            if (hits.Count != 2 || (hits[0] - hits[1]).Norm() <= Eps) continue;

            XY transverse = unitNormal.Left();
            double u = (hits[0].Planar() - station).Dot(transverse);
            double v = (hits[1].Planar() - station).Dot(transverse);
            if (Math.Max(u, v) < -halfWidth || Math.Min(u, v) > halfWidth) continue;
            if (Math.Abs(u - v) <= Eps)
            {
                edges.Add(new(hits[0], hits[1], i));
                continue;
            }
            double t0 = Math.Clamp((-halfWidth - u) / (v - u), 0, 1);
            double t1 = Math.Clamp((halfWidth - u) / (v - u), 0, 1);
            XYZ a = hits[0] + (hits[1] - hits[0]) * Math.Min(t0, t1);
            XYZ b = hits[0] + (hits[1] - hits[0]) * Math.Max(t0, t1);
            if ((a - b).Norm() > Eps) edges.Add(new(a, b, i));
        }
        return [.. edges];
    }

    public static Edge[][] Components(IEnumerable<Edge> edges)
    {
        var remaining = edges.ToList();
        var components = new List<Edge[]>();
        while (remaining.Count > 0)
        {
            var chain = new List<Edge> { remaining[0] };
            remaining.RemoveAt(0);
            for (int k = 0; k < chain.Count; k++)
            {
                Edge edge = chain[k];
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    Edge next = remaining[i];
                    if (!new[] { edge.A, edge.B }.Any(p => (p - next.A).Norm() <= Eps || (p - next.B).Norm() <= Eps))
                        continue;
                    chain.Add(next);
                    remaining.RemoveAt(i);
                }
            }
            components.Add([.. chain]);
        }
        return [.. components];
    }

    public static Station Observe(int index, Edge[][] components, double z, string cover)
    {
        var hits = new List<(XY Point, int Triangle)>();
        foreach (Edge[] component in components)
        {
            // Keep contiguous near-vertical runs on both sides of the probe height.
            // Short bevels and transverse cap edges remain section evidence without becoming wall anchors.
            Edge[] vertical = component.Where(e => (e.A.Planar() - e.B.Planar()).Norm()
                <= .2 * Math.Abs(e.A.Z - e.B.Z) + Eps).ToArray();
            foreach (Edge[] wall in Components(vertical))
            {
                if (wall.Min(e => Math.Min(e.A.Z, e.B.Z)) > z - .45
                    || wall.Max(e => Math.Max(e.A.Z, e.B.Z)) < z + .45) continue;
                foreach (Edge e in wall)
                {
                    if (z < Math.Min(e.A.Z, e.B.Z) - Eps || z > Math.Max(e.A.Z, e.B.Z) + Eps
                        || Math.Abs(e.B.Z - e.A.Z) < Eps) continue;
                    XY p = (e.A + (e.B - e.A) * Math.Clamp((z - e.A.Z) / (e.B.Z - e.A.Z), 0, 1)).Planar();
                    hits.Add((p, e.Triangle));
                }
            }
        }
        if (hits.Count == 0) return new(index, "gap", null, [], null);
        XY first = hits[0].Point;
        if (hits.Any(h => (h.Point - first).Norm() > Eps))
            return new(index, "ambiguous", null, hits.Select(h => h.Triangle).Distinct().Order().ToArray(), null);
        return new(index, "observed", first, hits.Select(h => h.Triangle).Distinct().Order().ToArray(), cover);
    }

    public static Station[] Trace(Triangle[] mesh, XYZ[] guide, Guide side, double probeZ)
    {
        if (side.Start < 0 || side.Count < 5 || side.Count > guide.Length - side.Start
            || string.IsNullOrWhiteSpace(side.Id) || string.IsNullOrWhiteSpace(side.Cover))
            throw new InvalidDataException("Invalid guide interval.");

        var rows = new Station[side.Count];
        for (int i = 0; i < rows.Length; i++)
        {
            XY station = guide[side.Start + i].Planar();
            XY left = guide[side.Start + Math.Max(0, i - 1)].Planar();
            XY right = guide[side.Start + Math.Min(side.Count - 1, i + 1)].Planar();
            var edges = Intersect(mesh, station, (right - left).Unit(), 2.5);
            rows[i] = Observe(i, Components(edges), probeZ, side.Cover);
        }
        return InferSingleGap(rows);
    }

    public static Station[] InferSingleGap(Station[] rows)
    {
        var copy = (Station[])rows.Clone();
        for (int i = 1; i < rows.Length - 1; i++)
        {
            Station a = rows[i - 1], b = rows[i + 1];
            if (rows[i].State == "gap" && a.State == "observed" && b.State == "observed"
                && a.Cover is not null && a.Cover == b.Cover && a.Point is XY p && b.Point is XY q)
                copy[i] = new(i, "inferred", (p + q) * .5, [], a.Cover);
        }
        return copy;
    }
}

public static class Pipeline
{
    public static (Job Job, RunResult Result) Run(string folder)
    {
        string config = Path.Combine(folder, "job.json"), asc = Path.Combine(folder, "guide.asc"),
            stl = Path.Combine(folder, "scan.stl");
        Job job = ReadJob(config);
        XYZ[] guide = File.ReadLines(asc).Where(line => !string.IsNullOrWhiteSpace(line)).Select(Data.Point).ToArray();
        Triangle[] mesh = Data.LoadStl(stl);
        SideResult[] results = job.Guides.Select(side =>
            FitSide(side.Id, Slicing.Trace(mesh, guide, side, job.ProbeZ))).ToArray();
        return (job, new("portfolio-run/1", job.Case, Data.Sha(config) + ":" + Data.Sha(asc) + ":" + Data.Sha(stl), results));
    }

    internal static Job ReadJob(string file)
    {
        Job job = Data.Read<Job>(file);
        if (job.Schema != "synthetic-job/1" || job.ZMin >= job.ZMax
            || !new[] { job.ZMin, job.ZMax, job.PlaneZ, job.ProbeZ }.All(double.IsFinite)
            || job.Guides.Length == 0 || job.Guides.Any(g => g is null)
            || job.Guides.Select(g => g.Id).Distinct().Count() != job.Guides.Length)
            throw new InvalidDataException("Invalid synthetic job contract.");
        return job;
    }

    internal static SideResult FitSide(string id, Station[] stations)
    {
        if (stations.Any(s => s.Point is null))
            return new(id, stations, null, null, "Unresolved support. This side is withheld from CAD.");

        XY[] points = stations.Select(s => s.Point!.Value).ToArray();
        FitResult? fit = null;
        try
        {
            fit = Fitting.Fit(points);
            RoundResult rounded = Fitting.Round(fit.Curves);
            if (Fitting.Maximum(rounded.Curves, points) > Fitting.FitLimit
                || Fitting.Maximum(rounded.Curves, Sampling.Uniform(points, .5)) > Fitting.FitLimit)
                throw new InvalidDataException("Rounded point distance exceeds 0.3 mm.");
            return new(id, stations, fit, rounded, null);
        }
        catch (InvalidDataException e)
        {
            return new(id, stations, fit, null, e.Message);
        }
    }

    public static CadPlan Cad(Job job, RunResult result) => new("portfolio-cad/1", "mm", result.InputFingerprint,
        job.PlaneZ, job.ZMin, job.ZMax,
        result.Sides.Where(s => s.Rounded is not null).Select(s => new CadSide(s.Id, s.Rounded!.Curves)).ToArray());
}
