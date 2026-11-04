using Portfolio;

static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args is ["--smoke", var input, var png, var sideIndex])
        {
            using var form = new ReviewWindow(Data.Read<RunResult>(input));
            form.SelectSide(int.Parse(sideIndex, System.Globalization.CultureInfo.InvariantCulture));
            form.Show();
            Application.DoEvents();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            if (form.SelectedCount < 1 || form.PaintCount < 1) Environment.ExitCode = 1;
            form.Close();
            return;
        }

        RunResult? initial = args is [var path] ? Data.Read<RunResult>(path) : null;
        Application.Run(new ReviewWindow(initial));
    }
}

sealed class ReviewWindow : Form
{
    private readonly ComboBox sides = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly Label summary = new() { AutoSize = true, Padding = new Padding(12, 7, 0, 0) };
    private readonly Plot plot = new() { Dock = DockStyle.Fill };
    private RunResult? run;

    public ReviewWindow(RunResult? initial)
    {
        Text = "CAD Portfolio — synthetic section review";
        ClientSize = new Size(1000, 580);
        MinimumSize = new Size(720, 440);
        Font = new Font("Segoe UI", 10);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8) };
        var open = new Button { Text = "Open run.json", AutoSize = true };
        open.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "Run report|run.json|JSON|*.json" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { LoadRun(Data.Read<RunResult>(dialog.FileName)); }
            catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException)
            {
                MessageBox.Show(this, e.Message, "Invalid run", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        toolbar.Controls.AddRange([open, sides, summary]);
        Controls.Add(plot);
        Controls.Add(toolbar);
        sides.SelectedIndexChanged += (_, _) => UpdateSelection();
        if (initial is not null) LoadRun(initial);
    }

    public void SelectSide(int index) => sides.SelectedIndex = index;
    public int SelectedCount => sides.Items.Count;
    public int PaintCount => plot.PaintCount;

    private void LoadRun(RunResult value)
    {
        if (value.Schema != "portfolio-run/1" || value.Sides.Length == 0)
            throw new InvalidDataException("Expected a portfolio run report.");
        run = value;
        sides.Items.Clear();
        sides.Items.AddRange(value.Sides.Select(s => (object)s.Id).ToArray());
        sides.SelectedIndex = 0;
    }

    private void UpdateSelection()
    {
        if (run is null || sides.SelectedIndex < 0) return;
        var side = run.Sides[sides.SelectedIndex];
        plot.Side = side;
        int observed = side.Stations.Count(p => p.State == "observed"), inferred = side.Stations.Count(p => p.State == "inferred");
        summary.Text = $"{side.Id}   |   {observed} observed  ·  {inferred} inferred  ·  {side.Stations.Length - observed - inferred} unresolved";
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
            e.Graphics.DrawString("Open a synthetic run.json to inspect its section evidence.", Font, text, 35, 35);
            return;
        }

        XY[] points = side.Stations.Where(s => s.Point.HasValue).Select(s => s.Point!.Value).ToArray();
        e.Graphics.DrawString("Observed: blue   ·   Inferred: orange   ·   Worst XY: red ring   ·   Line: teal   ·   Arc: violet",
            Font, text, 26, 16);
        e.Graphics.DrawString(side.Issue ?? $"{side.Fit!.Baseline.Length} → {side.Fit.Curves.Length} primitives  ·  max fit XY {Math.Max(side.Fit.OriginalError, side.Fit.UniformError):G4} mm",
            Font, text, 26, 47);
        if (points.Length == 0) return;
        double minX = points.Min(p => p.X), minY = points.Min(p => p.Y);
        double scale = Math.Min((Width - 94) / Math.Max(1, points.Max(p => p.X) - minX),
            (Height - 165) / Math.Max(1, points.Max(p => p.Y) - minY));
        PointF DrawPoint(XY p) => new((float)(45 + (p.X - minX) * scale), (float)(Height - 83 - (p.Y - minY) * scale));
        using var line = new Pen(Color.Teal, 4);
        using var arc = new Pen(Color.MediumPurple, 4);
        using var worst = new Pen(Color.Firebrick, 2);
        using var observed = new SolidBrush(Color.RoyalBlue);
        using var inferred = new SolidBrush(Color.DarkOrange);
        foreach (var c in side.Rounded?.Curves ?? [])
            e.Graphics.DrawLines(c.IsArc ? arc : line, Enumerable.Range(0, 33).Select(i => DrawPoint(c.At(i / 32.0))).ToArray());
        foreach (var row in side.Stations.Where(r => r.Point.HasValue))
        {
            PointF p = DrawPoint(row.Point!.Value);
            e.Graphics.FillEllipse(row.State == "inferred" ? inferred : observed, p.X - 2, p.Y - 2, 4, 4);
        }
        if (side.Fit is FitResult fit)
        {
            PointF p = DrawPoint(fit.WorstPoint);
            e.Graphics.DrawEllipse(worst, p.X - 8, p.Y - 8, 16, 16);
        }

        float stride = (Width - 52f) / Math.Max(1, side.Stations.Length);
        using var missing = new SolidBrush(Color.Firebrick);
        e.Graphics.DrawString("Station coverage   ·   red: gap or ambiguous", Font, text, 26, Height - 55);
        for (int i = 0; i < side.Stations.Length; i++)
            e.Graphics.FillRectangle(side.Stations[i].State switch
            {
                "observed" => observed, "inferred" => inferred, _ => missing
            }, 26 + i * stride, Height - 30, Math.Max(1, stride - 1), 8);
    }
}
