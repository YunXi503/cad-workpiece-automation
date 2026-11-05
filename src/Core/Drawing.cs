using System.Globalization;
using System.Net;
using System.Text;

namespace Portfolio;

public static class Drawing
{
    public static string Svg(RunResult run)
    {
        var s = new StringBuilder();
        bool source=run.Sides.Any(side=>side.SourceSupports is not null);
        s.AppendLine(source?"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1000 350\" role=\"img\" aria-label=\"Reviewed source endpoints, engineering-derived draft and CAD paths\">"
            :"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1000 350\" role=\"img\" aria-label=\"Synthetic sectional supports and rounded CAD paths\">");
        s.AppendLine("<rect width=\"1000\" height=\"350\" fill=\"#f5f8fc\"/>");
        s.AppendLine("<g font-family=\"system-ui,sans-serif\" fill=\"#15283d\">");
        s.AppendLine("<text x=\"32\" y=\"39\" font-size=\"26\" font-weight=\"bold\">From scan sections to editable CAD curves</text>");
        s.AppendLine(source?"<text x=\"32\" y=\"66\" font-size=\"15\">Reviewed source endpoints / Derived engineering draft / Explicit CAD stage</text>"
            :"<text x=\"32\" y=\"66\" font-size=\"15\">Synthetic scan  /  Section evidence  /  Rounded line–arc chains</text>");
        for (int i = 0; i < run.Sides.Length; i++)
        {
            var side = run.Sides[i];
            double x0 = 28 + i * 318;
            var p = side.Stations.Where(r => r.Point.HasValue).Select(r => r.Point!.Value)
                .Concat(side.SourceSupports?.Select(a=>a.Point.Planar())??[]).ToArray();
            s.AppendLine(F($"<rect x=\"{x0}\" y=\"87\" width=\"302\" height=\"218\" rx=\"12\" fill=\"white\" stroke=\"#cbd8e7\"/>"));
            if (p.Length == 0) continue;
            double minX = p.Min(q => q.X), minY = p.Min(q => q.Y);
            double scale = Math.Min(260 / Math.Max(p.Max(q => q.X) - minX, 1), 129 / Math.Max(p.Max(q => q.Y) - minY, 1));
            XY P(XY q) => new(x0 + 21 + (q.X - minX) * scale, 256 - (q.Y - minY) * scale);
            s.AppendLine(F($"<text x=\"{x0 + 20}\" y=\"115\" font-size=\"15\" font-weight=\"bold\">{WebUtility.HtmlEncode(side.Id)}</text>"));
            foreach (var c in side.Rounded?.Curves ?? (side.GeometryStage is not null?side.Fit?.Curves:[]) ?? [])
            {
                string path = string.Join(' ', Enumerable.Range(0, 25).Select(n =>
                {
                    XY q = P(c.At(n / 24.0));
                    return F($"{q.X:F2},{q.Y:F2}");
                }));
                s.AppendLine(F($"<polyline points=\"{path}\" fill=\"none\" stroke=\"{(c.IsArc ? "#7453bc" : "#176b74")}\" stroke-width=\"3.8\" stroke-linecap=\"round\"/>"));
            }
            foreach (var row in side.Stations.Where(r => r.Point.HasValue))
            {
                XY q = P(row.Point!.Value);
                s.AppendLine(F($"<circle cx=\"{q.X:F2}\" cy=\"{q.Y:F2}\" r=\"2.15\" fill=\"{(row.State == "inferred" ? "#eb8b2f" : "#437ed8")}\" opacity=\".73\"/>"));
            }
            foreach(var anchor in side.SourceSupports??[])
            {
                XY q=P(anchor.Point.Planar());
                s.AppendLine(F($"<circle cx=\"{q.X:F2}\" cy=\"{q.Y:F2}\" r=\"2.6\" fill=\"#437ed8\"/>"));
            }
            if (side.Fit is FitResult fit)
            {
                XY q = P(fit.WorstPoint);
                s.AppendLine(F($"<circle cx=\"{q.X:F2}\" cy=\"{q.Y:F2}\" r=\"6.2\" fill=\"none\" stroke=\"#b73528\" stroke-width=\"1.7\"/>"));
            }
            string label = side.Fit is FitResult f
                ? F($"{f.Baseline.Length} → {f.Curves.Length} primitives  ·  max fit XY {Math.Max(f.OriginalError, f.UniformError):F4} mm")
                : $"{side.Stations.Count(r => !r.Point.HasValue)} unresolved  ·  CAD withheld";
            s.AppendLine(F($"<text x=\"{x0 + 20}\" y=\"285\" font-size=\"13\">{label}</text>"));
        }
        s.AppendLine(source?"<text x=\"32\" y=\"331\" font-size=\"13\">Blue · source endpoints   Orange · derived draft   Red ring · worst draft XY   Teal · line   Violet · arc</text>"
            :"<text x=\"32\" y=\"331\" font-size=\"13\">Blue · observed   Orange · inferred   Red ring · worst original XY sample   Teal · line   Violet · arc</text>");
        s.AppendLine("</g></svg>");
        return s.ToString().ReplaceLineEndings("\n");
    }

    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);
}
