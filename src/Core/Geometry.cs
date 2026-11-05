using System.Text.Json.Serialization;

namespace Portfolio;

public readonly record struct XY(double X, double Y)
{
    public static XY operator +(XY a, XY b) => new(a.X + b.X, a.Y + b.Y);
    public static XY operator -(XY a, XY b) => new(a.X - b.X, a.Y - b.Y);
    public static XY operator *(XY a, double k) => new(a.X * k, a.Y * k);
    public double Dot(XY b) => X * b.X + Y * b.Y;
    public double Cross(XY b) => X * b.Y - Y * b.X;
    public double Norm() => Math.Sqrt(Dot(this));
    public XY Unit()
    {
        double norm = Norm();
        if (!double.IsFinite(norm) || norm <= 1e-12)
            throw new InvalidDataException("Degenerate direction.");
        return this * (1 / norm);
    }
    public XY Left() => new(-Y, X);
    public XY Rotate(double angle) =>
        new(X * Math.Cos(angle) - Y * Math.Sin(angle), X * Math.Sin(angle) + Y * Math.Cos(angle));
}

public readonly record struct XYZ(double X, double Y, double Z)
{
    public static XYZ operator +(XYZ a, XYZ b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static XYZ operator -(XYZ a, XYZ b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static XYZ operator *(XYZ a, double k) => new(a.X * k, a.Y * k, a.Z * k);
    public XY Planar() => new(X, Y);
    public double Norm() => Math.Sqrt(X * X + Y * Y + Z * Z);
}

public sealed record Curve(XY Start, XY End, XY? Center = null, double Sweep = 0)
{
    [JsonIgnore] public bool IsArc => Center.HasValue;
    [JsonIgnore] public double Radius => Center is XY c ? (Start - c).Norm() : 0;
    [JsonIgnore] public double Length => IsArc ? Radius * Math.Abs(Sweep) : (End - Start).Norm();

    public XY At(double t) => Center is XY c
        ? c + (Start - c).Rotate(Sweep * t)
        : Start + (End - Start) * t;

    public XY Tangent(double t) => Center is XY c
        ? (At(t) - c).Unit().Left() * Math.Sign(Sweep)
        : (End - Start).Unit();

    public double Distance(XY q)
    {
        if (Center is not XY c)
        {
            XY d = End - Start;
            return (q - At(Math.Clamp((q - Start).Dot(d) / d.Dot(d), 0, 1))).Norm();
        }

        XY r = Start - c;
        XY v = q - c;
        double angle = Math.Atan2(r.Cross(v), r.Dot(v)) * Math.Sign(Sweep);
        if (angle < 0) angle += 2 * Math.PI;
        return angle <= Math.Abs(Sweep)
            ? Math.Abs(v.Norm() - Radius)
            : Math.Min((q - Start).Norm(), (q - End).Norm());
    }

    public Curve Subcurve(double a, double b) =>
        new(At(a), At(b), Center, Sweep * (b - a));
}

public sealed record FitResult(Curve[] Baseline, Curve[] Curves, double OriginalError,
    double UniformError, int AcceptedMerges, int RejectedMerges, XY WorstPoint);
public sealed record RoundResult(Curve[] Curves, double MovementBound);

public static class Fitting
{
    public const double FitLimit = 0.3;
    public const double MoveLimit = 0.05;

    public static FitResult Fit(XY[] points)
    {
        if (points.Length < 5) throw new InvalidDataException("Five ordered points are required.");
        int[] cuts = MultiBendFitting.Cuts(points);
        if (cuts.Length > 0) return MultiBendFitting.Fit(points, cuts);
        var uniform = Sampling.Uniform(points, 0.5);
        XY a = points[0], b = points[^1];
        XY u = (points[2] - a).Unit(), v = (b - points[^3]).Unit();
        var direct = new Curve(a, b);
        Curve[] initial;
        if (Maximum([direct], points) <= FitLimit && Maximum([direct], uniform) <= FitLimit)
            initial = [direct];
        else
        {
            double turn = Math.Atan2(u.Cross(v), u.Dot(v));
            if (Math.Abs(turn) < 0.03 || Math.Abs(turn) >= Math.PI - 0.03)
                throw new InvalidDataException("A supported line–arc–line candidate was not found.");
            double along = (b - a).Cross(v) / u.Cross(v);
            XY corner = a + u * along;
            double tangentFactor = Math.Tan(Math.Abs(turn) / 2);
            double maxRadius = Math.Min(along, (b - corner).Dot(v)) / tangentFactor;
            if (maxRadius <= 0.1) throw new InvalidDataException("The inferred corner lies outside the end supports.");

            Curve[] Candidate(double radius)
            {
                XY p = corner - u * (radius * tangentFactor);
                XY q = corner + v * (radius * tangentFactor);
                return [new(a, p), new(p, q, p + u.Left() * (Math.Sign(turn) * radius), turn), new(q, b)];
            }

            double best = double.PositiveInfinity, chosen = 0;
            double step = maxRadius / 192;
            for (int i = 1; i < 192; i++)
            {
                double radius = i * step;
                double error = Maximum(Candidate(radius), points);
                if (error < best) (best, chosen) = (error, radius);
            }
            for (int pass = 0; pass < 4; pass++)
            {
                double center = chosen;
                step /= 8;
                for (int offset = -8; offset <= 8; offset++)
                {
                    double radius = center + offset * step;
                    if (radius <= 0 || radius >= maxRadius) continue;
                    double error = Maximum(Candidate(radius), points);
                    if (error < best) (best, chosen) = (error, radius);
                }
            }
            initial = Candidate(chosen);
        }

        if (Maximum(initial, points) > FitLimit || Maximum(initial, uniform) > FitLimit)
            throw new InvalidDataException("The candidate exceeds the XY fit limit.");

        // Window boundaries give the editable baseline. Coalescing is subject to both point sets.
        var baseline = initial.SelectMany(p => new[] { p.Subcurve(0, .5), p.Subcurve(.5, 1) }).ToArray();
        var reduced = baseline.ToList();
        int accepted = 0, rejected = 0;
        for (int i = 0; i + 1 < reduced.Count;)
        {
            Curve left = reduced[i], right = reduced[i + 1];
            Curve? merged = left.Center is XY c && right.Center is XY d && (c - d).Norm() <= 1e-9
                && Math.Sign(left.Sweep) == Math.Sign(right.Sweep)
                ? new(left.Start, right.End, c, left.Sweep + right.Sweep)
                : !left.IsArc && !right.IsArc && (left.Tangent(1) - right.Tangent(0)).Norm() <= 1e-9
                    ? new(left.Start, right.End) : null;
            if (merged is not null)
            {
                Curve[] candidate = reduced.Take(i).Append(merged).Concat(reduced.Skip(i + 2)).ToArray();
                if (Maximum(candidate, points) <= FitLimit && Maximum(candidate, uniform) <= FitLimit)
                {
                    reduced[i] = merged;
                    reduced.RemoveAt(i + 1);
                    accepted++;
                    continue;
                }
            }
            rejected++;
            i++;
        }

        XY worst = points.MaxBy(q => reduced.Min(p => p.Distance(q)));
        return new(baseline, [.. reduced], Maximum(reduced, points), Maximum(reduced, uniform),
            accepted, rejected, worst);
    }

    public static double Maximum(IEnumerable<Curve> curves, IEnumerable<XY> points) =>
        points.Max(q => curves.Min(p => p.Distance(q)));

    public static RoundResult Round(Curve[] input)
    {
        if (input.Length > 3) return MultiBendFitting.Round(input);
        static double Rounded(double x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);
        if (input.Length == 1 && !input[0].IsArc)
        {
            if (Math.Abs(Rounded(input[0].Length) - input[0].Length) > 1e-7)
                throw new InvalidDataException("A single rounded line cannot preserve both ends.");
            return new(input, 0);
        }
        if (input is not [ { IsArc: false }, { IsArc: true }, { IsArc: false } ])
            throw new InvalidDataException("This demo rounds one line–arc–line chain.");

        XY start = input[0].Start;
        XY chord = input[^1].End - start;
        double l0 = Rounded(input[0].Length), r = Rounded(input[1].Radius), l1 = Rounded(input[2].Length);
        if (Math.Min(l0, Math.Min(r, l1)) <= 0)
            throw new InvalidDataException("Rounding collapsed a primitive.");
        double theta0 = input[1].Sweep;

        Curve[] Build(double theta)
        {
            XY p = new(l0, 0);
            XY c = p + new XY(0, Math.Sign(theta) * r);
            XY q = c + (p - c).Rotate(theta);
            XY b = q + new XY(l1, 0).Rotate(theta);
            double rotation = Math.Atan2(b.Cross(chord), b.Dot(chord));
            XY Place(XY x) => start + x.Rotate(rotation);
            return [new(start, Place(p)), new(Place(p), Place(q), Place(c), theta), new(Place(q), Place(b))];
        }

        double Residual(double theta) => (Build(theta)[^1].End - start).Norm() - chord.Norm();
        double sign = Math.Sign(theta0), abs = Math.Abs(theta0);
        double lower = Math.Max(1e-4, abs - .08), upper = Math.Min(Math.PI - 1e-4, abs + .08);
        double left = lower * sign, right = upper * sign;
        double f = Residual(left);
        if (Math.Sign(f) == Math.Sign(Residual(right)))
            throw new InvalidDataException("Rounded chain has no bracketed closure near the original sweep.");
        for (int i = 0; i < 56; i++)
        {
            double mid = (left + right) / 2;
            if (Math.Sign(Residual(mid)) == Math.Sign(f)) left = mid;
            else right = mid;
        }
        Curve[] output = Build((left + right) / 2);
        if ((output[^1].End - input[^1].End).Norm() > 1e-7)
            throw new InvalidDataException("Rounded chain failed endpoint closure.");
        double bound = MovementBound(input, output);
        if (bound > MoveLimit) throw new InvalidDataException("Rounded chain exceeds the movement bound.");
        return new(output, bound);
    }

    public static double MovementBound(Curve[] a, Curve[] b)
    {
        const int divisions = 64;
        return a.Zip(b).Max(pair =>
        {
            var (x, y) = pair;
            double observed = Enumerable.Range(0, divisions + 1)
                .Max(i => (x.At((double)i / divisions) - y.At((double)i / divisions)).Norm());
            if (!x.IsArc)
                return Math.Max((x.Start - y.Start).Norm(), (x.End - y.End).Norm());
            // Bound the interpolation error of the displacement function, not the curve length.
            double acceleration = x.Radius * x.Sweep * x.Sweep + y.Radius * y.Sweep * y.Sweep;
            return observed + acceleration / (8 * divisions * divisions);
        });
    }
}

public static class Sampling
{
    public static XY[] Uniform(XY[] points, double spacing)
    {
        var stations = new double[points.Length];
        for (int i = 1; i < points.Length; i++)
            stations[i] = stations[i - 1] + (points[i] - points[i - 1]).Norm();
        if (stations[^1] <= 1e-12) throw new InvalidDataException("Zero-length guide.");
        int intervals = (int)Math.Ceiling(stations[^1] / spacing);
        var result = new XY[intervals + 1];
        int edge = 1;
        for (int k = 0; k <= intervals; k++)
        {
            double s = stations[^1] * k / intervals;
            while (edge < points.Length - 1 && stations[edge] <= s) edge++;
            double length = stations[edge] - stations[edge - 1];
            result[k] = length <= 1e-12 ? points[edge]
                : points[edge - 1] + (points[edge] - points[edge - 1]) * ((s - stations[edge - 1]) / length);
        }
        return result;
    }
}
