using System.Text.Json;
using Portfolio;

try
{
    switch (args)
    {
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
                + "  generate <normal|gap|height|reversed|competing|workpiece|workpiece-height> <new-input-directory>\n"
                + "  run <input-directory> <new-output-directory>\n"
                + "  verify <input-directory> <output-directory>\n"
                + "  section <input-directory> <side-id> <zero-based-index>\n"
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
