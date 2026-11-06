using System.Diagnostics;
using System.Text.Json;
using Portfolio;

sealed record DraftWorkflowStatus(string Schema, string State, string NumericStatus, int CadSides,
    double AutomatedElapsedSeconds, string TimeScope, string? Detail,
    bool NativeCadExecuted = false, bool EngineerTimeSavingMeasured = false, int UnroundedSides = 0);

sealed record DraftWorkflowResult(string Output, DraftWorkflowStatus Status);

/// <summary>Desktop orchestration only; all geometry and verification use the existing Core.</summary>
static class DraftWorkflow
{
    public static string NewFolder(string parent, string prefix) =>
        Path.Combine(parent, prefix + "-" + Guid.NewGuid().ToString("N")[..8]);

    private static void Fresh(string folder)
    {
        if (File.Exists(folder) || Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new InvalidDataException("Choose a new or empty output folder; previous results are preserved.");
    }

    public static DraftWorkflowResult Demo(string workspace, IProgress<string>? progress = null)
    {
        Fresh(workspace);
        progress?.Report("Creating an independent demo input...");
        string input = Path.Combine(workspace, "input");
        ReviewedFixture.Generate(input);
        return Build(Path.Combine(input, "case.json"), Path.Combine(workspace, "output"), progress);
    }

    public static DraftWorkflowResult Build(string manifest, string output, IProgress<string>? progress = null)
    {
        manifest = Path.GetFullPath(manifest);
        output = Path.GetFullPath(output);
        string input = Path.GetDirectoryName(manifest)!;
        string relative = Path.GetRelativePath(input, output);
        if (!Path.IsPathRooted(relative) && relative != ".."
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Choose an output folder outside the input folder.");
        Fresh(output);
        Directory.CreateDirectory(output);
        var watch = Stopwatch.StartNew();
        const string timeScope = "geometry generation plus numerical replay; excludes app startup, demo input creation and engineer/CAD work";
        string statusPath = Path.Combine(output, "workflow-run.json");
        Data.Write(statusPath, new DraftWorkflowStatus("desktop-draft-workflow/1", "running", "not-run", 0, 0, timeScope, null));
        try
        {
            progress?.Report("Checking sources and generating editable line-arc geometry...");
            SourceRun result = SourcePipeline.Run(manifest);
            // Same four deterministic files as `source run`; no new geometry transformation.
            Data.Write(Path.Combine(output, "source-run.json"), result);
            Data.Write(Path.Combine(output, "run.json"), result.Run);
            Data.Write(Path.Combine(output, "cad-plan.json"), result.Cad);
            File.WriteAllText(Path.Combine(output, "overlay.svg"), Drawing.Svg(result.Run));
            progress?.Report("Replaying sources and checking numerical results...");
            string numeric = "passed"; string? detail = null;
            try
            {
                SourceReport report = SourceVerification.Verify(manifest, output);
                Data.Write(Path.Combine(output, "source-verification.json"), report);
            }
            catch (Exception error) when (error is InvalidDataException or IOException or ArgumentException or JsonException)
            {
                numeric = "needs-review"; detail = error.Message;
                File.WriteAllText(Path.Combine(output, "numeric-verification-error.txt"), error.ToString());
            }
            var status = new DraftWorkflowStatus("desktop-draft-workflow/1",
                result.Cad.Sides.Length == 0 ? "diagnostics-only" : "draft-created", numeric, result.Cad.Sides.Length,
                watch.Elapsed.TotalSeconds, timeScope, detail, UnroundedSides: result.Run.Sides.Count(s => s.Fit is not null && s.Rounded is null));
            Data.Write(statusPath, status);
            return new(output, status);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(output, "workflow-error.txt"), error.ToString());
            Data.Write(statusPath, new DraftWorkflowStatus("desktop-draft-workflow/1", "failed", "not-completed", 0,
                watch.Elapsed.TotalSeconds, timeScope, error.Message));
            throw new InvalidDataException("Draft generation stopped; logs in " + output + ". " + error.Message, error);
        }
    }
}
