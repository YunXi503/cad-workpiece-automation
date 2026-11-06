using Portfolio;

static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args is [var workflowMode, var manifest, var output, var capture]
            && (workflowMode == "--build-smoke" || workflowMode == "--demo-smoke"))
        {
            using var form = new ReviewWindow();
            int result = 1;
            form.Shown += async (_, _) =>
            {
                try
                {
                    DraftWorkflowResult built = args[0] == "--demo-smoke"
                        ? await form.CreateDemoAsync(output) : await form.BuildReviewedAsync(manifest, output);
                    Application.DoEvents();
                    using var bitmap = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                    bitmap.Save(capture, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"{built.Status.State}; numeric: {built.Status.NumericStatus}; UI work ticks: {form.WorkTicks}");
                    result = built.Status.NumericStatus == "passed" && form.PaintCount > 0 ? 0 : 2;
                }
                catch (Exception error) { Console.Error.WriteLine("Cannot build draft: " + error.Message); }
                finally { form.Close(); }
            };
            Application.Run(form);
            return result;
        }
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
            Console.WriteLine(form.GeometryDescription);
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
    private readonly Button open = new() { Text = "Open run.json", AutoSize = true };
    private readonly Button demo = new() { Text = "Create demo", AutoSize = true };
    private readonly Button build = new() { Text = "Build reviewed case", AutoSize = true };
    private readonly Button showOutput = new() { Text = "Open output folder", AutoSize = true, Enabled = false };
    private readonly ToolStripStatusLabel activity = new("Open a report or create a draft. Engineer suitability is assessed separately.") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripProgressBar progressBar = new() { Style = ProgressBarStyle.Marquee, Visible = false, Width = 100 };
    private readonly System.Windows.Forms.Timer workTimer = new() { Interval = 100 };
    private bool busy;
    private string? outputFolder;
    private RunResult? run;

    public ReviewWindow()
    {
        Text = "CAD Portfolio — surface draft workflow";
        ClientSize = new Size(1000, 580);
        MinimumSize = new Size(720, 440);
        Font = new Font("Segoe UI", 10);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 48), Padding = new Padding(8) };
        open.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "Run report|run.json|JSON|*.json" };
            if (dialog.ShowDialog(this) == DialogResult.OK) OpenRunOrWarn(dialog.FileName);
        };
        toolbar.Controls.AddRange([open, sides, summary]);
        var commands = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 43, Padding = new Padding(8, 4, 8, 4) };
        commands.Controls.AddRange([demo, build, showOutput]);
        var status = new StatusStrip(); status.Items.AddRange([activity, progressBar]);
        Controls.Add(plot);
        Controls.Add(toolbar);
        Controls.Add(commands);
        Controls.Add(status);
        sides.SelectedIndexChanged += (_, _) => UpdateSelection();
        workTimer.Tick += (_, _) => WorkTicks++;
        demo.Click += async (_, _) =>
        {
            using var picker = new FolderBrowserDialog { Description = "Choose a location for a new demo workspace." };
            if (picker.ShowDialog(this) == DialogResult.OK)
                await RunOrWarn(() => CreateDemoAsync(DraftWorkflow.NewFolder(picker.SelectedPath, "demo")));
        };
        build.Click += async (_, _) =>
        {
            using var input = new OpenFileDialog { Filter = "Reviewed case|case.json|JSON|*.json", Title = "Select reviewed case.json (mesh, references and support domains)" };
            if (input.ShowDialog(this) != DialogResult.OK) return;
            using var picker = new FolderBrowserDialog { Description = "Choose an output location outside the input folder. A new draft folder is created." };
            if (picker.ShowDialog(this) == DialogResult.OK)
                await RunOrWarn(() => BuildReviewedAsync(input.FileName, DraftWorkflow.NewFolder(picker.SelectedPath, "draft")));
        };
        showOutput.Click += (_, _) =>
        {
            if (outputFolder is not null)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(outputFolder) { UseShellExecute = true });
        };
    }

    public void SelectSide(int index) => sides.SelectedIndex = index;
    public int SelectedCount => sides.Items.Count;
    public int PaintCount => plot.PaintCount;
    public string GeometryDescription => plot.Description;
    public int WorkTicks { get; private set; }

    public Task<DraftWorkflowResult> CreateDemoAsync(string workspace) =>
        RunWorkflowAsync(p => DraftWorkflow.Demo(workspace, p));
    public Task<DraftWorkflowResult> BuildReviewedAsync(string manifest, string output) =>
        RunWorkflowAsync(p => DraftWorkflow.Build(manifest, output, p));

    private async Task RunOrWarn(Func<Task<DraftWorkflowResult>> action)
    {
        try { await action(); }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Draft generation stopped", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task<DraftWorkflowResult> RunWorkflowAsync(Func<IProgress<string>, DraftWorkflowResult> action)
    {
        if (busy) throw new InvalidOperationException("A draft operation is already running.");
        busy = true; outputFolder = null; WorkTicks = 0; workTimer.Start(); progressBar.Visible = true;
        open.Enabled = demo.Enabled = build.Enabled = sides.Enabled = showOutput.Enabled = false;
        try
        {
            var progress = new Progress<string>(message => activity.Text = message);
            DraftWorkflowResult result = await Task.Run(() => action(progress));
            if (TryOpenRun(Path.Combine(result.Output, "run.json")) is string error) throw new InvalidDataException(error);
            outputFolder = result.Output;
            activity.Text = $"{result.Status.CadSides} CAD sides · checks: {result.Status.NumericStatus} · {result.Status.UnroundedSides} unrounded · {result.Status.AutomatedElapsedSeconds:F1}s automatic · engineer review remains";
            return result;
        }
        catch { activity.Text = "Draft generation stopped. Source files are unchanged; review the new output log."; throw; }
        finally
        {
            workTimer.Stop(); busy = false; progressBar.Visible = false;
            open.Enabled = demo.Enabled = build.Enabled = sides.Enabled = true;
            showOutput.Enabled = outputFolder is not null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) workTimer.Dispose();
        base.Dispose(disposing);
    }

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
                bool Finite(XY p) => double.IsFinite(p.X) && double.IsFinite(p.Y);
                bool Valid(Curve? c) => c is not null && Finite(c.Start) && Finite(c.End)
                    && (c.Center is not XY center || Finite(center))
                    && double.IsFinite(c.Sweep) && double.IsFinite(c.Length) && c.Length > 0;
                if (side.Fit is FitResult fit && (fit.Baseline.Length == 0 || fit.Curves.Length == 0
                    || !fit.Baseline.All(Valid) || !fit.Curves.All(Valid) || !Finite(fit.WorstPoint)))
                    throw new InvalidDataException($"Side '{side.Id}' needs nonempty finite fitting curves.");
                if (side.Rounded is RoundResult cad && (side.Fit is null || cad.Curves.Length == 0 || !cad.Curves.All(Valid)))
                    throw new InvalidDataException($"Side '{side.Id}' needs nonempty finite rounded curves.");
                if (side.Stations.Any(s => s.Point is XY p && !Finite(p))
                    || side.SourceSupports?.Any(a => a is null || !Finite(a.Point.Planar())) == true
                    || side.SourceFrames?.Any(f => f is null || string.IsNullOrWhiteSpace(f.Id) || string.IsNullOrWhiteSpace(f.State)) == true)
                    throw new InvalidDataException($"Side '{side.Id}' has invalid source or station records.");
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
    public string Description => Side is not SideResult side ? "No report selected"
        : (side.Issue ?? $"{side.Fit!.Baseline.Length} → {side.Fit.Curves.Length} primitives · max pre-round fit XY {Math.Max(side.Fit.OriginalError, side.Fit.UniformError):G4} mm")
          + $" · display: {(side.Rounded is not null ? "rounded" : side.GeometryStage is not null && side.Fit is not null ? "unrounded draft" : "withheld")}";

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
        e.Graphics.DrawString(side.SourceSupports is null?"Observed: blue · Inferred: orange · Red ring: pre-round worst point · Line: teal · Arc: violet"
            :"Source endpoints: blue (approximate) · Draft: orange · Red ring: pre-round worst draft point",
            Font, text, 26, 16);
        e.Graphics.DrawString(Description, Font, text, new RectangleF(26, 47, Width-52, 48));
        if (points.Length == 0) return;
        XY[] bounds=points.Concat(side.SourceSupports?.Select(a=>a.Point.Planar())??[]).ToArray();
        double minX = bounds.Min(p => p.X), minY = bounds.Min(p => p.Y);
        double scale = Math.Min((Width - 94) / Math.Max(1, bounds.Max(p => p.X) - minX),
            (Height - 200) / Math.Max(1, bounds.Max(p => p.Y) - minY));
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
