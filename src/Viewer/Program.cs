using Portfolio;

static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args is ["--smoke", var input, var png, var sideIndex])
        {
            using var form = new ReviewWindow();
            string? error = form.TryOpenRun(input);
            if (error is not null)
            {
                Console.Error.WriteLine(error);
                return 1;
            }
            if (!int.TryParse(sideIndex, out int index) || index < 0 || index >= form.SelectedCount)
            {
                Console.Error.WriteLine("Side index is outside the report.");
                return 1;
            }
            form.SelectSide(index);
            form.Show();
            Application.DoEvents();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            int exitCode = form.PaintCount > 0 ? 0 : 1;
            form.Close();
            return exitCode;
        }

        using var window = new ReviewWindow();
        if (args is [var path]) window.Shown += (_, _) => window.OpenRunOrWarn(path);
        Application.Run(window);
        return 0;
    }
}

sealed class ReviewWindow : Form
{
    private readonly ComboBox sides = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly Label summary = new() { AutoSize = true, Padding = new Padding(12, 7, 0, 0) };
    private readonly Plot plot = new() { Dock = DockStyle.Fill };
    private RunResult? run;

    public ReviewWindow()
    {
        Text = "CAD Portfolio — geometry and source review";
        ClientSize = new Size(1000, 580);
        MinimumSize = new Size(720, 440);
        Font = new Font("Segoe UI", 10);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8) };
        var open = new Button { Text = "Open run.json", AutoSize = true };
        open.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "Run report|run.json|JSON|*.json" };
            if (dialog.ShowDialog(this) == DialogResult.OK) OpenRunOrWarn(dialog.FileName);
        };
        toolbar.Controls.AddRange([open, sides, summary]);
        Controls.Add(plot);
        Controls.Add(toolbar);
        sides.SelectedIndexChanged += (_, _) => UpdateSelection();
    }

    public void SelectSide(int index) => sides.SelectedIndex = index;
    public int SelectedCount => sides.Items.Count;
    public int PaintCount => plot.PaintCount;

    public void OpenRunOrWarn(string path)
    {
        if (TryOpenRun(path) is string error)
            MessageBox.Show(this, error, "Invalid run", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public string? TryOpenRun(string path)
    {
        RunResult value;
        try
        {
            value = Data.Read<RunResult>(path);
            if (value.Schema != "portfolio-run/1" || value.Sides.Length == 0)
                throw new InvalidDataException("Expected a portfolio run report with at least one side.");
            foreach (SideResult? side in value.Sides)
            {
                if (side is null || string.IsNullOrWhiteSpace(side.Id) || side.Stations.Any(station => station is null))
                    throw new InvalidDataException("Each side needs an ID and valid station records.");
                if ((side.Fit is null && string.IsNullOrWhiteSpace(side.Issue)) ||
                    (side.Rounded is RoundResult rounded && rounded.Curves.Any(curve => curve is null)))
                    throw new InvalidDataException($"Side '{side.Id}' has an incomplete geometry report.");
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException
                                      or NotSupportedException or InvalidDataException or System.Text.Json.JsonException)
        {
            return "Cannot open run report: " + error.Message;
        }

        run = value;
        sides.Items.Clear();
        sides.Items.AddRange(value.Sides.Select(s => (object)s.Id).ToArray());
        sides.SelectedIndex = 0;
        return null;
    }

    private void UpdateSelection()
    {
        if (run is null || sides.SelectedIndex < 0) return;
        var side = run.Sides[sides.SelectedIndex];
        plot.Side = side;
        int observed = side.Stations.Count(p => p.State == "observed"), inferred = side.Stations.Count(p => p.State == "inferred");
        summary.Text = $"{side.Id}   |   {observed} observed  ·  {inferred} inferred  ·  {side.Stations.Length - observed - inferred} unresolved";
        if(side.SourceSupports is not null)
            summary.Text=$"{side.Id} | {side.SourceSupports.Length} source endpoints · {inferred} derived · {side.GeometryStage}";
        plot.Invalidate();
    }
}

sealed class Plot : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public SideResult? Side { get; set; }
    public int PaintCount { get; private set; }

    public Plot() { DoubleBuffered = true; BackColor = Color.White; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PaintCount++;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var text = new SolidBrush(Color.FromArgb(23, 45, 63));
        if (Side is not SideResult side)
        {
            e.Graphics.DrawString("Open run.json to inspect geometry and source evidence.", Font, text, 35, 35);
            return;
        }

        XY[] points = side.Stations.Where(s => s.Point.HasValue).Select(s => s.Point!.Value).ToArray();
        e.Graphics.DrawString(side.SourceSupports is null?"Observed: blue   ·   Inferred: orange   ·   Worst XY: red ring   ·   Line: teal   ·   Arc: violet"
            :"Source endpoints: blue (approximate anchors) · Derived draft: orange · Worst draft XY: red ring",
            Font, text, 26, 16);
        e.Graphics.DrawString(side.Issue ?? $"{side.Fit!.Baseline.Length} → {side.Fit.Curves.Length} primitives  ·  max fit XY {Math.Max(side.Fit.OriginalError, side.Fit.UniformError):G4} mm",
            Font, text, 26, 47);
        if (points.Length == 0) return;
        XY[] bounds=points.Concat(side.SourceSupports?.Select(a=>a.Point.Planar())??[]).ToArray();
        double minX = bounds.Min(p => p.X), minY = bounds.Min(p => p.Y);
        double scale = Math.Min((Width - 94) / Math.Max(1, bounds.Max(p => p.X) - minX),
            (Height - 165) / Math.Max(1, bounds.Max(p => p.Y) - minY));
        PointF DrawPoint(XY p) => new((float)(45 + (p.X - minX) * scale), (float)(Height - 83 - (p.Y - minY) * scale));
        using var line = new Pen(Color.Teal, 4);
        using var arc = new Pen(Color.MediumPurple, 4);
        using var worst = new Pen(Color.Firebrick, 2);
        using var observed = new SolidBrush(Color.RoyalBlue);
        using var inferred = new SolidBrush(Color.DarkOrange);
        foreach (var c in side.Rounded?.Curves ?? (side.GeometryStage is not null?side.Fit?.Curves:[]) ?? [])
            e.Graphics.DrawLines(c.IsArc ? arc : line, Enumerable.Range(0, 33).Select(i => DrawPoint(c.At(i / 32.0))).ToArray());
        foreach (var row in side.Stations.Where(r => r.Point.HasValue))
        {
            PointF p = DrawPoint(row.Point!.Value);
            e.Graphics.FillEllipse(row.State == "inferred" ? inferred : observed, p.X - 2, p.Y - 2, 4, 4);
        }
        foreach(var anchor in side.SourceSupports??[])
        {
            PointF p=DrawPoint(anchor.Point.Planar());
            e.Graphics.FillEllipse(observed,p.X-3,p.Y-3,6,6);
        }
        if (side.Fit is FitResult fit)
        {
            PointF p = DrawPoint(fit.WorstPoint);
            e.Graphics.DrawEllipse(worst, p.X - 8, p.Y - 8, 16, 16);
        }

        float stride = (Width - 52f) / Math.Max(1, side.SourceFrames?.Length??side.Stations.Length);
        using var missing = new SolidBrush(Color.Firebrick);
        e.Graphics.DrawString(side.SourceFrames is null?"Station coverage   ·   red: gap or ambiguous"
            :"Original case frames (all sides) · red: unresolved · orange: review-excluded · blue: nominal candidate",Font,text,26,Height-55);
        if(side.SourceFrames is not null)
        {
            for(int i=0;i<side.SourceFrames.Length;i++)e.Graphics.FillRectangle(side.SourceFrames[i].State switch
            {"engineering-width-candidate"=>observed,"review-excluded"=>inferred,_=>missing},26+i*stride,Height-30,Math.Max(1,stride-1),8);
            return;
        }
        for (int i = 0; i < side.Stations.Length; i++)
            e.Graphics.FillRectangle(side.Stations[i].State switch
            {
                "observed" => observed, "inferred" => inferred, _ => missing
            }, 26 + i * stride, Height - 30, Math.Max(1, stride - 1), 8);
    }
}
