using System.Text.Json;

namespace Portfolio;

public sealed record SideCheck(string Id, int Observed, int Inferred, int Unresolved,
    int BaselineCount, int ReducedCount, double? OriginalError, double? UniformError, double? RoundedXYError,
    double? MovementBound, double? EndpointResidual, double? JointResidual, double? TangentResidual,
    double? ContactSampleMaximum, string ContactState, string Status);
public sealed record Report(string Schema, string Case, string Scope, SideCheck[] Sides);

public static class Verification
{
    private const double Eps = 1e-7;

    public static Report Inspect(string input, RunResult result, CadPlan plan)
    {
        Job job = Pipeline.ReadJob(Path.Combine(input, "job.json"));
        Triangle[] triangles = Data.LoadStl(Path.Combine(input, "scan.stl"));
        XYZ[] guide = File.ReadLines(Path.Combine(input, "guide.asc")).Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(Data.Point).ToArray();
        string fingerprint = string.Join(':', new[] { "job.json", "guide.asc", "scan.stl" }
            .Select(name => Data.Sha(Path.Combine(input, name))));
        Require(result.Schema == "portfolio-run/1" && result.Case == job.Case
            && fingerprint == result.InputFingerprint, "Source identity mismatch.");
        Require(plan.Schema == "portfolio-cad/1" && plan.Units == "mm" && plan.Source == fingerprint
            && plan.ZMin == job.ZMin && plan.ZMax == job.ZMax && plan.PlaneZ == job.PlaneZ
            && plan.Sides.All(s => s is not null) && result.Sides.All(s => s is not null)
            && result.Sides.Select(s => s.Id).SequenceEqual(job.Guides.Select(g => g.Id)),
            "CAD contract, sides or heights differ from the job.");
        Require(plan.Sides.Select(s => s.Id).SequenceEqual(result.Sides.Where(s => s.Rounded is not null)
            .Select(s => s.Id)), "CAD side list differs from accepted results.");

        var checks = new List<SideCheck>();
        foreach (var (side, range) in result.Sides.Zip(job.Guides))
        {
            Station[] replay = Slicing.Trace(triangles, guide, range, job.ProbeZ);
            Require(side.Stations.Length == replay.Length && side.Stations.All(s => s is not null),
                "Station count or contents mismatch.");
            foreach (var (s, index) in side.Stations.Select((s, i) => (s, i)))
            {
                Station expected = replay[index];
                Require(s.Index == index && s.State == expected.State && s.Cover == expected.Cover
                    && s.Triangles.SequenceEqual(expected.Triangles), "Station state or source identities differ from scan replay.");
                if (s.State == "observed")
                {
                    Require(s.Point.HasValue && s.Cover == range.Cover && s.Triangles.Length > 0,
                        "Observed station lacks evidence.");
                    XY point = s.Point!.Value;
                    XYZ spatial = new(point.X, point.Y, job.ProbeZ);
                    XY left = guide[range.Start + Math.Max(0, index - 1)].Planar();
                    XY right = guide[range.Start + Math.Min(range.Count - 1, index + 1)].Planar();
                    Require(Math.Abs((point - guide[range.Start + index].Planar()).Dot((right - left).Unit())) <= Eps
                        && s.Triangles.All(i => OnTriangle(spatial, triangles[i])),
                        "Observed point does not lie on its source triangle and section plane.");
                    Require(expected.Point is XY measured && (point - measured).Norm() <= Eps,
                        "Observed point differs from scan replay.");
                }
                else if (s.State == "inferred")
                {
                    Require(index > 0 && index + 1 < side.Stations.Length && s.Triangles.Length == 0,
                        "Inferred station must be an interior row without source triangles.");
                    Station a = side.Stations[index - 1], b = side.Stations[index + 1];
                    Require(a.State == "observed" && b.State == "observed" && a.Cover == b.Cover && s.Cover == a.Cover
                        && a.Point is XY p && b.Point is XY q && s.Point is XY m && (m - (p + q) * .5).Norm() <= Eps,
                        "Unbound inference.");
                }
                else
                    Require(s.State is "gap" or "ambiguous" && s.Point is null && s.Cover is null, "Invalid unresolved station.");
            }

            int observed = side.Stations.Count(s => s.State == "observed"), inferred = side.Stations.Count(s => s.State == "inferred");
            int unresolved = side.Stations.Length - observed - inferred;
            if (side.Rounded is null)
            {
                SideResult expected = Pipeline.FitSide(side.Id, replay);
                Require(expected.Rounded is null && side.Issue == expected.Issue
                    && JsonSerializer.Serialize(side.Fit, Data.Json) == JsonSerializer.Serialize(expected.Fit, Data.Json),
                    "Withheld side or its reason differs from replay.");
                checks.Add(new(side.Id, observed, inferred, unresolved, side.Fit?.Baseline.Length ?? 0,
                    side.Fit?.Curves.Length ?? 0, null, null, null, null, null, null, null, null, "withheld", "withheld"));
                continue;
            }

            Require(unresolved == 0 && side.Fit is not null && side.Issue is null, "Accepted side lacks fit or has unresolved points.");
            FitResult fit = side.Fit!;
            Curve[] baseline = fit.Baseline, reduced = fit.Curves, rounded = side.Rounded.Curves;
            Require(baseline.Length >= reduced.Length && reduced.Length > 0 && reduced.Length == rounded.Length
                && rounded.SequenceEqual(plan.Sides.Single(s => s.Id == side.Id).Curves), "Curve counts or CAD geometry differ.");
            XY[] points = side.Stations.Select(s => s.Point!.Value).ToArray();
            XY[] uniform = Sampling.Uniform(points, .5);
            double Max(Curve[] curves, XY[] samples) => samples.Max(p => curves.Min(c => PointDistance(p, c)));
            var baseChain = Chain(baseline);
            var reducedChain = Chain(reduced);
            var roundedChain = Chain(rounded);
            double endpoint = new[] { baseline, reduced, rounded }.Max(chain =>
                Math.Max((chain[0].Start - points[0]).Norm(), (chain[^1].End - points[^1]).Norm()));
            double joint = Math.Max(baseChain.Joint, Math.Max(reducedChain.Joint, roundedChain.Joint));
            double tangent = Math.Max(baseChain.Tangent, Math.Max(reducedChain.Tangent, roundedChain.Tangent));
            Require(endpoint <= Eps && joint <= Eps && tangent <= Eps, "Chain continuity, endpoints or G1 failed.");

            double original = Max(reduced, points), equal = Max(reduced, uniform);
            Require(Math.Abs(original - fit.OriginalError) <= Eps && Math.Abs(equal - fit.UniformError) <= Eps
                && Math.Abs(reduced.Min(c => PointDistance(fit.WorstPoint, c)) - original) <= Eps
                && points.Contains(fit.WorstPoint) && fit.AcceptedMerges == baseline.Length - reduced.Length,
                "Stored fitting metrics differ.");
            Require(original <= .3 && equal <= .3 && Max(baseline, points) <= .3 && Max(baseline, uniform) <= .3,
                "At least one original or uniform fit distance exceeds 0.3 mm.");

            for (int i = 0; i < rounded.Length; i++)
            {
                Curve old = reduced[i], next = rounded[i];
                Require(old.IsArc == next.IsArc && Math.Sign(old.Sweep) == Math.Sign(next.Sweep), "Primitive direction changed.");
                double a = old.IsArc ? old.Radius : old.Length, b = next.IsArc ? next.Radius : next.Length;
                Require(Math.Abs(Math.Round(a, 2, MidpointRounding.AwayFromZero) - b) <= Eps, "CAD dimension differs from rounding.");
            }
            double movement = Bound(reduced, rounded);
            Require(movement <= .05 && Math.Abs(movement - side.Rounded.MovementBound) <= Eps, "Rounding displacement bound failed.");

            double roundedXY = Math.Max(Max(rounded, points), Max(rounded, uniform));
            Require(roundedXY <= .3, "Rounded point distance exceeds 0.3 mm.");
            double height = Math.Max(0, Math.Max(plan.ZMin - job.ProbeZ, job.ProbeZ - plan.ZMax));
            double sampled = side.Stations.Where(s => s.State == "observed").Max(s =>
            {
                double horizontal = rounded.Min(c => PointDistance(s.Point!.Value, c));
                return Math.Sqrt(horizontal * horizontal + height * height);
            });
            checks.Add(new(side.Id, observed, inferred, unresolved, baseline.Length, reduced.Length, original, equal,
                roundedXY, movement, endpoint, joint, tangent, sampled,
                sampled <= .3 ? "within-sampled-limit" : "exceeds-sampled-limit", "checked"));
        }

        return new("portfolio-verification/1", result.Case,
            "Synthetic observations only. Geometry verification and sampled 3D contact are distinct.", [.. checks]);
    }

    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidDataException(message);
    }

    private static bool OnTriangle(XYZ p, Triangle t)
    {
        XYZ u = t.B - t.A, v = t.C - t.A, w = p - t.A;
        static double Dot(XYZ a, XYZ b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        double uu = Dot(u, u), uv = Dot(u, v), vv = Dot(v, v), det = uu * vv - uv * uv;
        if (det <= 0) return false;
        double a = (Dot(w, u) * vv - Dot(w, v) * uv) / det;
        double b = (Dot(w, v) * uu - Dot(w, u) * uv) / det;
        return a >= -Eps && b >= -Eps && a + b <= 1 + Eps && (w - u * a - v * b).Norm() <= Eps;
    }

    public static double PointDistance(XY p, Curve c)
    {
        if (c.Center is not XY origin)
        {
            XY direction = c.End - c.Start;
            double t = (p - c.Start).Dot(direction) / direction.Dot(direction);
            if (t < 0 || t > 1) return Math.Min((p - c.Start).Norm(), (p - c.End).Norm());
            return Math.Abs((p - c.Start).Cross(direction)) / direction.Norm();
        }
        XY a = c.Start - origin, q = p - origin;
        double directed = Math.Sign(c.Sweep) * (Math.Atan2(q.Y, q.X) - Math.Atan2(a.Y, a.X));
        double wrapped = (directed % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);
        return wrapped <= Math.Abs(c.Sweep) ? Math.Abs(q.Norm() - a.Norm())
            : Math.Min((p - c.Start).Norm(), (p - c.End).Norm());
    }

    private static (double Joint, double Tangent) Chain(Curve[] curves)
    {
        Require(curves.Length > 0 && curves.All(c => c is not null), "Empty chain or missing primitive.");
        double joint = 0, tangent = 0;
        XY Derivative(Curve c, XY point) => c.Center is XY o
            ? (point - o).Left().Unit() * Math.Sign(c.Sweep)
            : (c.End - c.Start).Unit();
        for (int i = 0; i < curves.Length; i++)
        {
            Curve c = curves[i];
            Require(double.IsFinite(c.Length) && c.Length > Eps && double.IsFinite(c.Sweep), "Degenerate primitive.");
            if (c.IsArc)
                Require(Math.Abs(c.Sweep) > Eps && Math.Abs(c.Sweep) < Math.PI && (c.At(1) - c.End).Norm() <= Eps,
                    "Inconsistent finite arc.");
            else Require(c.Sweep == 0, "Line has nonzero sweep.");
            if (i == 0) continue;
            Curve p = curves[i - 1];
            joint = Math.Max(joint, (p.End - c.Start).Norm());
            tangent = Math.Max(tangent, (Derivative(p, p.End) - Derivative(c, c.Start)).Norm());
        }
        return (joint, tangent);
    }

    private static double Bound(Curve[] a, Curve[] b)
    {
        const int n = 64;
        double bound = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double maximum = Math.Max((a[i].Start - b[i].Start).Norm(), (a[i].End - b[i].End).Norm());
            if (a[i].IsArc)
            {
                for (int j = 1; j < n; j++)
                    maximum = Math.Max(maximum, (a[i].At((double)j / n) - b[i].At((double)j / n)).Norm());
                maximum += (a[i].Length * Math.Abs(a[i].Sweep) + b[i].Length * Math.Abs(b[i].Sweep)) / (8 * n * n);
            }
            bound = Math.Max(bound, maximum);
        }
        return bound;
    }
}
