using System;
using System.IO;
using File = System.IO.File;
using System.Reflection;
using System.Runtime.InteropServices;
using INFITF;

namespace CadPortfolio.Vsta
{
    internal static class StaRunner
    {
        [STAThread]
        private static int Main(string[] args)
        {
            string interop = Environment.GetEnvironmentVariable("PORTFOLIO_CAD_INTEROP");
            AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
            {
                var name = new AssemblyName(request.Name).Name;
                if (string.IsNullOrEmpty(interop) || !name.StartsWith("DassaultSystemes.", StringComparison.Ordinal))
                    return null;
                string path = Path.Combine(interop, name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
            return Run(args);
        }

        private static int Run(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("probe | reference | sketches <plan> <receipt> | surfaces <plan> <receipt> <report>");
                    return 0;
                }
                var app = (Application)Marshal.GetActiveObject("CATIA.Application");
                if (args.Length == 1 && args[0] == "probe")
                    Console.WriteLine(MacroWorkflow.ActivePart(app) == null ? "COM reachable; no active Part." : "COM reachable; active Part available.");
                else if (args.Length == 1 && args[0] == "reference")
                {
                    if (MacroWorkflow.ActivePart(app) != null)
                        throw new InvalidOperationException("Use the current reference Part or a dedicated demo session.");
                    MacroWorkflow.NewPart(app);
                    Console.WriteLine("Created an isolated, unsaved reference Part.");
                }
                else if (args.Length == 3 && args[0] == "sketches")
                {
                    var receipt = MacroWorkflow.CreateSketches(app, args[1], args[2]);
                    Console.WriteLine("Verified native sketches: " + receipt.sketches.Length);
                }
                else if (args.Length == 4 && args[0] == "surfaces")
                    Console.WriteLine("Verified native surfaces: " + MacroWorkflow.CreateSurfaces(app, args[1], args[2], args[3]));
                else throw new ArgumentException("Unsupported arguments. Run with no arguments for help.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
                return 1;
            }
        }
    }
}
