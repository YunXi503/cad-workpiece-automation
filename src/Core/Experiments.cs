namespace Portfolio;

public sealed record Observation(string Key, string Value);
public sealed record ExperimentResult(string Id, string Title, string Mechanism, Observation[] Observations, bool Reproduced);
public sealed record Experiment(string Id, string Title, Func<ExperimentResult> Run);

public static class Experiments
{
    private static Experiment Make(string id, string title, string mechanism, Func<(bool, Observation[])> body) =>
        new(id, title, () =>
        {
            var (ok, observations) = body();
            return new(id, title, mechanism, observations, ok);
        });

    private static Observation O(string key, object value) => new(key,
        value is double d ? Data.Number(d) : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "");

    public static readonly Experiment[] All =
    [
        Make("closed-route", "Closed guide has two paths", "Enumerate both endpoint routes and check which reaches the expected boundary.",
            () =>
            {
                XY[] loop = [new(0, 0), new(3, 0), new(3, 2), new(0, 2)];
                int[] forward = [0, 1, 2], reverse = [0, 3, 2];
                bool Crosses(int[] path) => path.Zip(path.Skip(1)).Any(pair =>
                    Math.Min(loop[pair.First].Y, loop[pair.Second].Y) <= 1
                    && Math.Max(loop[pair.First].Y, loop[pair.Second].Y) >= 1
                    && loop[pair.First].X == 0 && loop[pair.Second].X == 0);
                return (!Crosses(forward) && Crosses(reverse), [O("forwardIntersects", Crosses(forward)), O("reverseIntersects", Crosses(reverse))]);
            }),
        Make("bend-density", "Guide density versus section spacing", "Bisect spans by chord sag, then compare with independently fixed section stations.",
            () =>
            {
                var arc = new Curve(new(4, 0), new(0, 4), new XY(0, 0), Math.PI / 2);
                int Count(double a, double b)
                {
                    double sag = (arc.At((a + b) / 2) - (arc.At(a) + arc.At(b)) * .5).Norm();
                    return sag <= .03 ? 1 : Count(a, (a + b) / 2) + Count((a + b) / 2, b);
                }
                int chordSpans = Count(0, 1), stations = (int)Math.Ceiling(arc.Length / 2) + 1;
                return (chordSpans > stations, [O("chordSpans", chordSpans), O("sectionStations", stations)]);
            }),
        Make("angle-sort", "Polar ordering bridges disconnected strips", "Compare a global angle sort with endpoint-connected components.",
            () =>
            {
                Edge[] edges = [new(new(-2, -.2, 0), new(-2, .2, 0), 0), new(new(2, -.2, 0), new(2, .2, 0), 1)];
                XYZ[] sorted = edges.SelectMany(e => new[] { e.A, e.B }).OrderBy(p => Math.Atan2(p.Y, p.X)).ToArray();
                double longest = sorted.Zip(sorted.Skip(1)).Max(p => (p.First - p.Second).Norm());
                int components = Slicing.Components(edges).Length;
                return (longest > 3 && components == 2, [O("inventedChord", longest), O("connectedComponents", components)]);
            }),
        Make("longest-chain", "Longest component loses the intended wall", "Compare longest length and component membership near the intended boundary.",
            () =>
            {
                Edge[] edges = [new(new(0, 0, 0), new(4, 0, 0), 10), new(new(0, 2, 0), new(1, 2, 0), 20)];
                Edge[][] groups = Slicing.Components(edges);
                int lengthPick = groups.MaxBy(g => g.Sum(e => (e.A - e.B).Norm()))![0].Triangle;
                int boundaryPick = groups.MinBy(g => g.Min(e => Math.Abs(e.A.Y - 2)))![0].Triangle;
                return (lengthPick != boundaryPick && boundaryPick == 20, [O("lengthPick", lengthPick), O("boundaryPick", boundaryPick)]);
            }),
        Make("short-window", "Locally straight is not the intended wall", "Measure free-direction straightness and alignment with the fixed wall direction.",
            () =>
            {
                XY[] samples = [new(0, 0), new(1, .5), new(2, 1)];
                double free = Fitting.Maximum([new(samples[0], samples[^1])], samples);
                double fixedBand = samples.Max(p => p.Y) - samples.Min(p => p.Y);
                XY[] anotherWall = [new(0, 3), new(1, 3), new(2, 3)];
                double wrongOffset = anotherWall.Min(p => p.Y);
                return (free < 1e-10 && fixedBand > .1 && wrongOffset > .1,
                    [O("freeLineError", free), O("fixedDirectionBand", fixedBand), O("parallelButWrongOffset", wrongOffset)]);
            }),
        Make("free-circle", "A fixed point set can contradict a circle hypothesis", "Use equal-power equations: three non-collinear points fix the circle, then test a fourth.",
            () =>
            {
                XY a = new(0, 0), b = new(4, 0), c = new(0, 4), d = new(4, 3);
                XY u = (b - a) * 2, v = (c - a) * 2;
                double r1 = b.Dot(b) - a.Dot(a), r2 = c.Dot(c) - a.Dot(a);
                XY center = new((r1 * v.Y - r2 * u.Y) / u.Cross(v), (u.X * r2 - v.X * r1) / u.Cross(v));
                double residual = Math.Abs((d - center).Norm() - (a - center).Norm());
                return (residual > .1, [O("exactFirstThree", true), O("fourthPointRadialResidual", residual)]);
            }),
        Make("arc-families", "Single, double and triple tangent-arc families", "Build arc families with unequal radii from shared endpoint tangents.",
            () =>
            {
                Curve[] Family(params double[] radii)
                {
                    XY p = new(0, 0), tangent = new(1, 0);
                    var chain = new List<Curve>();
                    foreach (double radius in radii)
                    {
                        double sweep = radius > 4 ? -.31 : .42;
                        XY center = p + tangent.Left() * (radius * Math.Sign(sweep));
                        XY end = center + (p - center).Rotate(sweep);
                        chain.Add(new(p, end, center, sweep));
                        p = end;
                        tangent = tangent.Rotate(sweep);
                    }
                    return [.. chain];
                }
                Curve[][] families = [Family(3), Family(3, 5), Family(3, 5, 4)];
                double g1 = families.Skip(1).Max(f => f.Zip(f.Skip(1)).Max(pair =>
                    (pair.First.Tangent(1) - pair.Second.Tangent(0)).Norm()));
                XY sample = families[^1][^1].End;
                double singleError = families[0][0].Distance(sample);
                return (g1 <= 1e-10 && singleError > .1,
                    [O("families", "1,2,3"), O("maxJoinTangentResidual", g1), O("singleArcErrorAtTriarcEnd", singleError)]);
            }),
        Make("arc-ordering", "A short candidate budget can miss a feasible solution", "Compare fixed-budget ranking and a full evaluation of the same candidates.",
            () =>
            {
                double[] radii = [1, 2, 3, 4, 5, 6];
                XY witness = new(3, 3);
                double Error(double r) => new Curve(new(r, 0), new(0, r), new XY(0, 0), Math.PI / 2).Distance(witness);
                double budgeted = radii.Take(2).Min(Error), exhaustive = radii.Min(Error);
                return (budgeted > .3 && exhaustive <= .3,
                    [O("shortlistBest", budgeted), O("fullListBest", exhaustive), O("candidateCount", radii.Length)]);
            }),
        Make("necessary-bound", "A necessary condition prunes infeasible radius intervals", "Apply the triangle inequality to chord length and radial tolerance.",
            () =>
            {
                double chord = (new XY(10, 0) - new XY(0, 0)).Norm(), error = .05;
                double lowerRadius = chord / 2 - error;
                double[] candidates = [2, 4, 4.96, 5.2];
                int discarded = candidates.Count(r => r < lowerRadius);
                return (discarded == 2, [O("lowerRadius", lowerRadius), O("discarded", discarded), O("survivingIsOnlyNecessary", true)]);
            }),
        Make("wide-cover", "Independently widest covers can switch identity", "Select widest per section and detect incompatible source IDs across neighboring sections.",
            () =>
            {
                (string Cover, double Width)[][] sections = [[("A", 12), ("B", 11)], [("A", 10), ("B", 13)]];
                string[] picks = sections.Select(s => s.MaxBy(c => c.Width).Cover).ToArray();
                return (!picks[0].Equals(picks[1], StringComparison.Ordinal), [O("firstPick", picks[0]), O("nextPick", picks[1])]);
            }),
        Make("mixed-cover", "Conflicting cover support creates a kink", "Guard a repeated edge identity before choosing an anchor.",
            () =>
            {
                XY left = new(0, 0), right = new(2, 0), wrong = new(1, .6);
                int[] firstCover = [3, 7, 9], purportedOther = [9, 3, 7];
                bool conflict = firstCover.Order().SequenceEqual(purportedOther.Order());
                XY guarded = conflict ? (left + right) * .5 : wrong;
                double Deflection(XY p) => ((p - left).Unit() - (right - p).Unit()).Norm();
                return (conflict && Deflection(wrong) > .5 && Deflection(guarded) < 1e-12,
                    [O("supportConflict", conflict), O("beforeTangentJump", Deflection(wrong)), O("afterTangentJump", Deflection(guarded))]);
            }),
        Make("per-side", "An invalid side need not suppress the other", "Report separate side decisions and preserve the rejected side as unresolved.",
            () =>
            {
                (bool Branch, bool Cover)[] evidence = [(true, true), (false, true)];
                int coupled = evidence.All(x => x.Branch && x.Cover) ? 2 : 0;
                int separate = evidence.Count(x => x.Branch && x.Cover);
                return (coupled == 0 && separate == 1, [O("acceptedCoupled", coupled), O("acceptedSeparate", separate)]);
            }),
        Make("projection", "Projection can yield one hit or an ambiguity", "Intersect a transverse ray with local finite supports and require a unique hit.",
            () =>
            {
                XY ray = new(1, 0);
                int Hits(Curve[] lines) => lines.Count(line =>
                {
                    XY edge = line.End - line.Start;
                    double determinant = ray.Cross(edge);
                    if (Math.Abs(determinant) < 1e-10) return false;
                    double t = line.Start.Cross(edge) / determinant;
                    double s = line.Start.Cross(ray) / determinant;
                    return t >= 0 && s >= 0 && s <= 1;
                });
                Curve near = new(new(2, -1), new(2, 1)), far = new(new(4, -1), new(4, 1));
                return (Hits([near]) == 1 && Hits([near, far]) == 2,
                    [O("uniqueHits", Hits([near])), O("ambiguousHits", Hits([near, far])), O("role", "inferred")]);
            }),
        Make("weld-apex", "Weld apex cue depends on support on both sides", "Require a positive local peak and points to either side.",
            () =>
            {
                bool Apex(XY[] p) => Enumerable.Range(1, Math.Max(0, p.Length - 2))
                    .Any(i => p[i].Y - Math.Max(p[i - 1].Y, p[i + 1].Y) > .2
                        && p[i - 1].X < p[i].X && p[i].X < p[i + 1].X);
                XY[] full = [new(-1, 0), new(0, .6), new(1, 0)];
                return (Apex(full) && !Apex(full[..2]), [O("twoSidedCue", Apex(full)), O("truncatedCue", Apex(full[..2]))]);
            }),
        Make("outer-extension", "Local continuation needs one compatible boundary per side", "Check endpoint alignment and matching branch identity before proposing a bridge.",
            () =>
            {
                Curve left = new(new(0, 0), new(1, 0)), right = new(new(2, 0), new(3, 0));
                bool Bridge(Curve a, Curve b, string idA, string idB) => idA == idB
                    && (a.Tangent(1) - b.Tangent(0)).Norm() <= 1e-9
                    && Math.Abs((b.Start - a.End).Cross(a.Tangent(1))) <= 1e-9;
                bool same = Bridge(left, right, "branch-A", "branch-A");
                bool other = Bridge(left, right, "branch-A", "branch-B");
                return (same && !other, [O("sameBranchProposal", same), O("crossBranchProposal", other), O("role", "inferred bridge")]);
            }),
        Make("neighbor-interpolation", "A source-bound gap can be interpolated selectively", "Only infer a single gap between matching observed cover IDs.",
            () =>
            {
                Station a = new(0, "observed", new XY(0, 0), [3], "A"), gap = new(1, "gap", null, [], null),
                    b = new(2, "observed", new XY(2, 0), [5], "A");
                Station yes = Slicing.InferSingleGap([a, gap, b])[1];
                Station no = Slicing.InferSingleGap([a, gap, b with { Cover = "B" }])[1];
                return (yes.State == "inferred" && no.State == "gap", [O("sameCover", yes.State), O("differentCover", no.State)]);
            }),
        Make("branch-stop", "A short branch can end before the global station limit", "Compare global stations, central stop, fixed station rotation and last valid interval.",
            () =>
            {
                bool SameBranch(double x, double angle) => x + 1.4 * Math.Abs(Math.Sin(angle)) <= 6.4;
                double lo = 6, hi = 7;
                for (int i = 0; i < 24; i++)
                {
                    double mid = (lo + hi) / 2;
                    if (SameBranch(mid, 0)) lo = mid;
                    else hi = mid;
                }
                bool fixedBad = SameBranch(6, .6), rotated = SameBranch(6, 0);
                return (!SameBranch(7, 0) && !fixedBad && rotated && Math.Abs(lo - 6.4) < 1e-6,
                    [O("globalStationValid", SameBranch(7, 0)), O("earlyStop", 5), O("fixedDirectionValid", fixedBad),
                    O("reorientedValid", rotated), O("searchedLastStation", lo)]);
            }),
        Make("cloud-raster", "Raster closing can connect separate silhouette regions", "Close a binary row by dilation and erosion and check a deliberate gap.",
            () =>
            {
                bool[] raster = [false, false, true, true, false, true, true, false, false];
                bool In(bool[] a, int i) => i >= 0 && i < a.Length && a[i];
                bool[] dilated = raster.Select((_, i) => In(raster, i - 1) || In(raster, i) || In(raster, i + 1)).ToArray();
                bool[] closed = raster.Select((_, i) => In(dilated, i - 1) && In(dilated, i) && In(dilated, i + 1)).ToArray();
                return (!raster[4] && closed[4], [O("originalGap", !raster[4]), O("afterClosing", closed[4])]);
            }),
        Make("axis-bins", "Axis-bin envelopes hide a second branch", "Group projected points by X and keep one envelope value per bin.",
            () =>
            {
                XY[] points = [new(0, 1), new(0, 3), new(1, 1), new(1, 3), new(2, 1), new(2, 3)];
                XY[] upper = points.GroupBy(p => Math.Floor(p.X)).Select(bin => bin.MaxBy(p => p.Y)).ToArray();
                return (upper.Length == 3 && upper.All(p => p.Y == 3),
                    [O("points", points.Length), O("bins", upper.Length), O("lowerBranchRetained", upper.Any(p => p.Y == 1))]);
            }),
        Make("paired-center", "Index-based centerline assumes matching stations", "Compare row-index midpoint with a midpoint after aligning one boundary by arc length.",
            () =>
            {
                XY[] a = [new(0, 0), new(2, 0), new(4, 0)];
                XY[] b = [new(0, 2), new(1, 2), new(2, 2), new(3, 2), new(4, 2)];
                XY indexMid = (a[2] + b[2]) * .5;
                XY alignedMid = (a[2] + Sampling.Uniform(b, 2)[2]) * .5;
                return (indexMid.X == 3 && alignedMid.X == 4,
                    [O("indexCenterEndpoint", indexMid.X), O("matchedCenterEndpoint", alignedMid.X)]);
            }),
        Make("arc-length", "Uniform stations use arc length instead of source row index", "Reparameterize a nonuniform polyline with cumulative lengths.",
            () =>
            {
                XY[] points = [new(0, 0), new(.25, 0), new(1, 0), new(4, 0)];
                XY[] uniform = Sampling.Uniform(points, 1);
                double stepError = uniform.Zip(uniform.Skip(1)).Max(p => Math.Abs((p.First - p.Second).Norm() - 1));
                return (uniform.Length == 5 && stepError <= 1e-12 && uniform[^1] == points[^1],
                    [O("originalPoints", points.Length), O("newStations", uniform.Length), O("stepError", stepError)]);
            }),
        Make("adaptive-tolerance", "Selecting a draft tolerance changes the segment trade-off", "Evaluate each trial using its actual finite-geometry deviation.",
            () =>
            {
                (int Count, double Error)[] candidates = [(7, .08), (5, .14), (3, .32)];
                int Choose(double limit) => candidates.Where(x => x.Error <= limit).Min(x => x.Count);
                return (Choose(.1) == 7 && Choose(.2) == 5 && Choose(.3) == 5,
                    [O("strictCount", Choose(.1)), O("draftCount", Choose(.2)), O("limitPointThreeCount", Choose(.3))]);
            }),
        Make("fit-reduction", "Reducing editable entities preserves checked XY fit", "Fit a tangent chain, then coalesce compatible pieces and remeasure both point sets.",
            () =>
            {
                Curve[] reference = [new(new(0, 0), new(6, 0)),
                    new(new(6, 0), new(9, 3), new XY(6, 3), Math.PI / 2), new(new(9, 3), new(9, 9))];
                XY[] points = reference.SelectMany((curve, j) => Enumerable.Range(j == 0 ? 0 : 1, j == 0 ? 13 : 12)
                    .Select(i => curve.At(i / 12.0))).ToArray();
                FitResult fit = Fitting.Fit(points);
                return (fit.Baseline.Length == 6 && fit.Curves.Length == 3 && fit.UniformError <= .3,
                    [O("baseline", fit.Baseline.Length), O("reduced", fit.Curves.Length), O("maxUniformXY", fit.UniformError)]);
            }),
        Make("finite-surface", "A finite extension has a separate height failure mode", "Clamp Z to the extrusion range before combining the planar and vertical components.",
            () =>
            {
                double xy = .08, probeZ = 2, low = 2.7, high = 5;
                double dz = Math.Max(0, Math.Max(low - probeZ, probeZ - high));
                double distance = Math.Sqrt(xy * xy + dz * dz);
                return (xy <= .3 && distance > .3, [O("xy", xy), O("zGap", dz), O("finiteDistance", distance)]);
            }),

        Make("resume-integrity", "Resume needs source identity, including after relocation", "Hash equal content under two names and reject changed content.",
            () =>
            {
                static string Identity(string s) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s)));
                var original = ("first/path", "synthetic payload");
                var relocated = ("second/path", original.Item2);
                bool matches = Identity(original.Item2) == Identity(relocated.Item2);
                bool changed = Identity(original.Item2) != Identity(relocated.Item2 + "!");
                return (matches && changed, [O("relocatedMatch", matches), O("contentChangeRejected", changed)]);
            })
    ];

    public static ExperimentResult Run(string id) => (All.SingleOrDefault(e => e.Id == id)
        ?? throw new InvalidDataException("Unknown experiment: " + id)).Run();
}
