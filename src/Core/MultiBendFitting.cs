namespace Portfolio;

public static class MultiBendFitting
{
    public static int[] Cuts(XY[] points)
    {
        var spans = new List<(int Start, int End)>();
        int start = -1;
        for (int i = 2; i + 2 < points.Length; i++)
        {
            XY a = points[i - 2], b = points[i + 2];
            double length = (b - a).Norm();
            bool straight = length > 1 && Enumerable.Range(i - 1, 3)
                .All(j => Math.Abs((points[j] - a).Cross(b - a)) / length <= .003);
            if (straight && start < 0) start = i;
            if ((!straight || i == points.Length - 3) && start >= 0)
            {
                int end = straight ? i : i - 1;
                if (end - start >= 5) spans.Add((start, end));
                start = -1;
            }
        }
        return spans.Skip(1).SkipLast(1).Select(s => (s.Start + s.End) / 2).ToArray();
    }

    public static FitResult Fit(XY[] points, int[] cuts)
    {
        int[] boundaries = [0, .. cuts, points.Length - 1];
        var fits = new List<FitResult>();
        for (int i = 0; i + 1 < boundaries.Length; i++)
            fits.Add(Fitting.Fit(points[boundaries[i]..(boundaries[i + 1] + 1)]));
        var chain = new List<Curve>();
        foreach (FitResult fit in fits)
        {
            if (chain.Count == 0) { chain.AddRange(fit.Curves); continue; }
            Curve a = chain[^1], b = fit.Curves[0];
            if (a.IsArc || b.IsArc || (a.End - b.Start).Norm() > 1e-7
                || (a.Tangent(1) - b.Tangent(0)).Norm() > 1e-7)
                throw new InvalidDataException("Multi-bend split does not share a straight tangent support.");
            chain[^1] = new(a.Start, b.End);
            chain.AddRange(fit.Curves.Skip(1));
        }
        Curve[] curves = [.. chain], baseline = fits.SelectMany(f => f.Curves).ToArray();
        XY[] uniform = Sampling.Uniform(points, .5);
        double original = Fitting.Maximum(curves, points), equal = Fitting.Maximum(curves, uniform);
        if (Math.Max(original, equal) > Fitting.FitLimit)
            throw new InvalidDataException("Multi-bend candidate exceeds the XY fit limit.");
        return new(baseline, curves, original, equal, baseline.Length - curves.Length,
            fits.Sum(f => f.RejectedMerges), points.MaxBy(p => curves.Min(c => c.Distance(p))));
    }

    public static RoundResult Round(Curve[] input)
    {
        if (input[0].IsArc || input[^1].IsArc || input.Select((c, i) => c.IsArc == (i % 2 == 1)).Any(ok => !ok))
            throw new InvalidDataException("Multi-bend rounding requires alternating lines and finite arcs.");
        double[] dimensions = input.Select(c => Math.Round(c.IsArc ? c.Radius : c.Length, 2,
            MidpointRounding.AwayFromZero)).ToArray();
        if (dimensions.Any(d => d <= 0)) throw new InvalidDataException("Rounded dimension collapsed.");
        XY start = input[0].Start, chord = input[^1].End - start;
        RoundResult? best = null;
        foreach (int adjustable in Enumerable.Range(0, input.Length).Where(i => input[i].IsArc))
        {
            Curve[] Build(double sweep)
            {
                XY cursor = new(), direction = new(1, 0);
                var chain = new List<Curve>();
                for (int i = 0; i < input.Length; i++)
                {
                    double angle = i == adjustable ? sweep : input[i].Sweep;
                    if (!input[i].IsArc)
                    {
                        XY end = cursor + direction * dimensions[i];
                        chain.Add(new(cursor, end)); cursor = end;
                    }
                    else
                    {
                        XY center = cursor + direction.Left() * (Math.Sign(angle) * dimensions[i]);
                        XY end = center + (cursor - center).Rotate(angle);
                        chain.Add(new(cursor, end, center, angle));
                        cursor = end; direction = direction.Rotate(angle);
                    }
                }
                double rotation = Math.Atan2(cursor.Cross(chord), cursor.Dot(chord));
                XY Place(XY p) => start + p.Rotate(rotation);
                return chain.Select(c => new Curve(Place(c.Start), Place(c.End), c.Center is XY p ? Place(p) : null, c.Sweep)).ToArray();
            }
            double theta = input[adjustable].Sweep, sign = Math.Sign(theta);
            double left = sign * Math.Max(.0001, Math.Abs(theta) - .08);
            double right = sign * Math.Min(Math.PI - .0001, Math.Abs(theta) + .08);
            double Residual(double angle) => (Build(angle)[^1].End - start).Norm() - chord.Norm();
            double f = Residual(left);
            if (Math.Sign(f) == Math.Sign(Residual(right))) continue;
            for (int k = 0; k < 56; k++)
            {
                double mid = (left + right) / 2;
                if (Math.Sign(Residual(mid)) == Math.Sign(f)) left = mid;
                else right = mid;
            }
            Curve[] candidate = Build((left + right) / 2);
            double bound = Fitting.MovementBound(input, candidate);
            if ((candidate[^1].End - input[^1].End).Norm() <= 1e-7 && bound <= Fitting.MoveLimit
                && (best is null || bound < best.MovementBound)) best = new(candidate, bound);
        }
        return best ?? throw new InvalidDataException("Rounded multi-bend chain has no closure within the movement limit.");
    }
}
