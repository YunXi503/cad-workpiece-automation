using System.Text.Json;
using Portfolio;

try
{
    switch (args)
    {
        case ["source", "generate", var sourceInput]:
            Empty(sourceInput);
            ReviewedFixture.Generate(sourceInput);
            Console.WriteLine("Generated independent analytic source-review fixture; this is not the private original Case A.");
            return 0;
        case ["source", "verify", var manifest, var sourceOutput]:
        {
            SourceReport report=SourceVerification.Verify(manifest,sourceOutput);
            Console.WriteLine(JsonSerializer.Serialize(report,Data.Json));
            return 0;
        }
        case ["source", "public-report", var manifest, var sourceOutput, var publicReport]:
        {
            if(File.Exists(publicReport))throw new InvalidDataException("Choose a new aggregate evidence file.");
            SourceReport report=SourceVerification.Verify(manifest,sourceOutput);
            // The whitelist is an aggregate record: no source paths, coordinates, raw frames or original files.
            string Alias(string id,int index)=>id is "upper" or "lower" or "left"?id:$"side-{index+1}";
            SourceReport sanitized=report with {Case=report.DataKind=="private-original"?"case-a-original":"analytic-case",
                Sides=report.Sides.Select((s,i)=>s with {Id=Alias(s.Id,i)}).ToArray()};
            Data.Write(publicReport,sanitized);
            Console.WriteLine("Wrote aggregate source-case evidence; original files and coordinates remain local.");
            return 0;
        }
        case ["source", "run", var manifest, var sourceOutput]:
        {
            Empty(sourceOutput);
            SourceRun result=SourcePipeline.Run(manifest);
            Directory.CreateDirectory(sourceOutput);
            Data.Write(Path.Combine(sourceOutput,"source-run.json"),result);
            Data.Write(Path.Combine(sourceOutput,"run.json"),result.Run);
            Data.Write(Path.Combine(sourceOutput,"cad-plan.json"),result.Cad);
            File.WriteAllText(Path.Combine(sourceOutput,"overlay.svg"),Drawing.Svg(result.Run));
            Console.WriteLine($"Fitted {result.Cad.Sides.Length}/{result.Run.Sides.Length} engineering-draft sides; rounding and physical contact are separate states.");
            return 0;
        }
        case ["source", "draft", var manifest, var draftOutput]:
        {
            Empty(draftOutput);
            var (job,audit)=SourceEvidence.Inspect(manifest);
            var draft=EngineeringDraft.Build(manifest,job,audit);
            Directory.CreateDirectory(draftOutput);
            Data.Write(Path.Combine(draftOutput,"source-audit.json"),audit);
            Data.Write(Path.Combine(draftOutput,"draft.json"),draft);
            Console.WriteLine(string.Join(", ",draft.Sides.Select(s=>$"{s.Id}: {s.SourceSupports.Length} source supports, {s.Points.Length} derived points")));
            return 0;
        }
        case ["source", "audit", var manifest, var auditOutput]:
        {
            Empty(auditOutput);
            var (_, audit)=SourceEvidence.Inspect(manifest);
            Directory.CreateDirectory(auditOutput);
            Data.Write(Path.Combine(auditOutput,"source-audit.json"),audit);
            Console.WriteLine($"Validated {audit.Frames.Length} original source frames; {audit.Anchors.Length} engineering support endpoints.");
            return 0;
        }
        case ["generate", var caseId, var folder]:
            Empty(folder);
            Fixtures.Generate(folder, caseId);
            Console.WriteLine($"Synthetic input: {Path.GetFullPath(folder)}");
            return 0;
        case ["run", var input, var output]:
        {
            Empty(output);
            var (job, run) = Pipeline.Run(input);
            CadPlan cad = Pipeline.Cad(job, run);
            Report report = Verification.Inspect(input, run, cad);
            Directory.CreateDirectory(output);
            Data.Write(Path.Combine(output, "run.json"), run);
            Data.Write(Path.Combine(output, "cad-plan.json"), cad);
            Data.Write(Path.Combine(output, "verification.json"), report);
            File.WriteAllText(Path.Combine(output, "overlay.svg"), Drawing.Svg(run));
            Console.WriteLine($"Checked {cad.Sides.Length}/{run.Sides.Length} CAD sides. See verification.json in {Path.GetFullPath(output)}");
            return 0;
        }
        case ["verify", var input, var output]:
        {
            var run = Data.Read<RunResult>(Path.Combine(output, "run.json"));
            var cad = Data.Read<CadPlan>(Path.Combine(output, "cad-plan.json"));
            var stored = Data.Read<Report>(Path.Combine(output, "verification.json"));
            var recomputed = Verification.Inspect(input, run, cad);
            if (JsonSerializer.Serialize(stored, Data.Json) != JsonSerializer.Serialize(recomputed, Data.Json))
                throw new InvalidDataException("Stored verification report differs from recomputed evidence.");
            string preview = File.ReadAllText(Path.Combine(output, "overlay.svg"));
            if (preview.ReplaceLineEndings("\n") != Drawing.Svg(run))
                throw new InvalidDataException("Preview overlay differs from the verified run. Re-run into a new output directory.");
            Console.WriteLine(JsonSerializer.Serialize(recomputed, Data.Json));
            return 0;
        }
        case ["section", var folder, var side, var index]:
            if (!int.TryParse(index, out int station)) throw new InvalidDataException("Section index must be an integer.");
            Console.WriteLine(JsonSerializer.Serialize(Pipeline.InspectSection(folder, side, station), Data.Json));
            return 0;
        case ["experiment", "list"]:
            foreach (Experiment experiment in Experiments.All) Console.WriteLine($"{experiment.Id}\t{experiment.Title}");
            return 0;
        case ["experiment", "run", var id]:
        {
            var result = Experiments.Run(id);
            Console.WriteLine(JsonSerializer.Serialize(result, Data.Json));
            return result.Reproduced ? 0 : 1;
        }
        case ["experiment", "check-all"]:
        {
            ExperimentResult[] results = Experiments.All.Select(e => e.Run()).ToArray();
            Console.WriteLine(JsonSerializer.Serialize(results, Data.Json));
            return results.All(r => r.Reproduced) ? 0 : 1;
        }
        case []:
        case ["--help"]:
            Console.WriteLine("CAD workpiece portfolio\n"
                + $"  generate <{string.Join('|', Fixtures.Cases)}> <new-input-directory>\n"
                + "  run <input-directory> <new-output-directory>\n"
                + "  verify <input-directory> <output-directory>\n"
                + "  section <input-directory> <side-id> <zero-based-index>\n"
                + "  source audit <local-manifest.json> <new-private-output-directory>\n"
                + "  source generate <new-analytic-input-directory>\n"
                + "  source draft <local-manifest.json> <new-private-output-directory>\n"
                + "  source run <local-manifest.json> <new-private-output-directory>\n"
                + "  source verify <local-manifest.json> <private-output-directory>\n"
                + "  source public-report <local-manifest.json> <private-output-directory> <new-summary.json>\n"
                + "  experiment <list|check-all>\n  experiment run <id>");
            return 0;
        default:
            Console.Error.WriteLine("Unknown arguments. Run --help.");
            return 2;
    }
}
catch (Exception ex) when (ex is InvalidDataException or IOException or ArgumentException or UnauthorizedAccessException or JsonException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static void Empty(string path)
{
    if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
        throw new InvalidDataException("Choose a fresh directory so previous evidence remains intact.");
}
