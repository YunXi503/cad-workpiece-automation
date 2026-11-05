using System;
using System.Collections.Generic;
using System.IO;
using File = System.IO.File;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using INFITF;
using MECMOD;
using HybridShapeTypeLib;
using CATPLMEnvBuild;

namespace CadPortfolio.Vsta
{
    public sealed class Point
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public sealed class Primitive
    {
        public Point start { get; set; }
        public Point end { get; set; }
        public Point center { get; set; }
        public double sweep { get; set; }
    }

    public sealed class Side
    {
        public string id { get; set; }
        public Primitive[] curves { get; set; }
    }

    public sealed class Plan
    {
        public string schema { get; set; }
        public string units { get; set; }
        public string source { get; set; }
        public double planeZ { get; set; }
        public double zMin { get; set; }
        public double zMax { get; set; }
        public Side[] sides { get; set; }
    }

    public sealed class SketchCheck
    {
        public string name { get; set; }
        public int curves { get; set; }
        public int activeConstraints { get; set; }
        public double maxResidual { get; set; }
    }

    public sealed class Receipt
    {
        public string schema { get; set; }
        public string planHash { get; set; }
        public string root { get; set; }
        public SketchCheck[] sketches { get; set; }
    }

    public static class MacroWorkflow
    {
        private const double Tolerance = 1e-7;
        private const string Marker = "PortfolioSketch_";
        private const string FaceMarker = "PortfolioSurface_";

        public static Receipt CreateSketches(Application app, string planPath, string receiptPath)
        {
            string hash;
            Plan plan = ReadPlan(planPath, out hash);
            Need(!File.Exists(receiptPath), "Choose a new sketch receipt.");
            Part reference = ActivePart(app);
            Need(reference != null, "Activate a reference Part before creating a fresh demo Part.");
            Part target = NewPart(app);
            Need(!SameObject(target, reference) && target.HybridBodies.Count == 0,
                "The target must be a distinct, empty Part.");

            var root = target.HybridBodies.Add();
            root.set_Name(Marker + Guid.NewGuid().ToString("N"));
            var factory = (HybridShapeFactory)target.HybridShapeFactory;
            var plane = factory.AddNewPlaneOffset(Ref(target, target.OriginElements.PlaneXY), plan.planeZ, false);
            root.AppendHybridShape(plane);
            target.UpdateObject(plane);
            foreach (Side side in plan.sides)
                BuildSketch(target, root, plane, plan.planeZ, side);

            target.Update();
            SketchCheck[] checks = plan.sides.Select(side => ReadBack(FindSketch(root, side.id), plan.planeZ, side)).ToArray();
            var receipt = new Receipt { schema = "portfolio-sketch-receipt/1", planHash = hash,
                root = root.get_Name(), sketches = checks };
            WriteNew(receiptPath, receipt);
            return receipt;
        }

        public static int CreateSurfaces(Application app, string planPath, string receiptPath, string surfaceReportPath)
        {
            string hash;
            Plan plan = ReadPlan(planPath, out hash);
            Need(!File.Exists(surfaceReportPath), "Choose a new surface report.");
            Receipt receipt = new JavaScriptSerializer().Deserialize<Receipt>(File.ReadAllText(receiptPath));
            Need(receipt != null && receipt.schema == "portfolio-sketch-receipt/1" && receipt.planHash == hash
                && receipt.root != null && receipt.root.StartsWith(Marker, StringComparison.Ordinal)
                && receipt.sketches != null && receipt.sketches.Select(s => s.name).SequenceEqual(plan.sides.Select(s => s.id)),
                "Sketch receipt and CAD plan do not match.");
            Part part = ActivePart(app);
            Need(part != null, "Activate the demo Part that owns the validated sketches.");
            HybridBody root = FindBody(part, receipt.root);
            Need(root != null, "The active Part does not own the validated sketch marker.");
            string suffix = receipt.root.Substring(Marker.Length);
            Need(FindBody(part, FaceMarker + suffix) == null, "This demo already contains an extension attempt.");
            var sketches = plan.sides.Select(s => FindSketch(root, s.id)).ToArray();
            for (int i = 0; i < sketches.Length; i++)
                ReadBack(sketches[i], plan.planeZ, plan.sides[i]);

            double before = plan.planeZ - plan.zMin, after = plan.zMax - plan.planeZ;
            Need(before >= 0 && after >= 0, "This macro requires the sketch plane within the finite Z interval.");
            var output = part.HybridBodies.Add();
            output.set_Name(FaceMarker + suffix);
            var factory = (HybridShapeFactory)part.HybridShapeFactory;
            var direction = factory.AddNewDirectionByCoord(0, 0, 1);
            foreach (Sketch sketch in sketches)
            {
                var face = factory.AddNewExtrude(Ref(part, sketch), after, before, direction);
                face.Context = 0;
                face.SymmetricalExtension = false;
                output.AppendHybridShape(face);
                part.UpdateObject(face);
                Need(part.IsUpToDate(face)
                    && Math.Abs(face.BeginOffset.Value - after) <= Tolerance
                    && Math.Abs(face.EndOffset.Value - before) <= Tolerance
                    && Math.Abs(face.Direction.GetXVal()) <= Tolerance
                    && Math.Abs(face.Direction.GetYVal()) <= Tolerance
                    && Math.Abs(face.Direction.GetZVal() - 1) <= Tolerance, "Extension direction or limits did not read back.");
            }
            Need(output.HybridShapes.Count == sketches.Length, "Unexpected surface feature count.");
            for (int i = 0; i < sketches.Length; i++)
                ReadBack(sketches[i], plan.planeZ, plan.sides[i]);

            WriteNew(surfaceReportPath, new { schema = "portfolio-surface-receipt/1", planHash = hash,
                sketchCount = sketches.Length, surfaceCount = output.HybridShapes.Count, positiveOffset = after,
                negativeOffset = before, zMin = plan.zMin, zMax = plan.zMax });
            return output.HybridShapes.Count;
        }

        public static Part ActivePart(Application app)
        {
            var editor = app.ActiveEditor;
            return editor == null ? null : editor.ActiveObject as Part;
        }

        public static Part NewPart(Application app)
        {
            string serviceName = "PLMNewService";
            string type = "3DShape";
            var service = (PLMNewService)app.GetSessionService(ref serviceName);
            Editor editor;
            service.PLMCreate(ref type, out editor);
            Part part = editor.ActiveObject as Part;
            Need(part != null, "PLMNewService did not return a 3D Shape Part.");
            return part;
        }

        private static void BuildSketch(Part part, HybridBody body, HybridShapePlaneOffset plane, double z, Side side)
        {
            Sketch sketch = body.HybridSketches.Add(Ref(part, plane));
            sketch.set_Name(side.id);
            sketch.SetAbsoluteAxisData(new object[] { 0d, 0d, z, 1d, 0d, 0d, 0d, 1d, 0d });
            var prior = part.InWorkObject;
            bool editing = false;
            try
            {
                part.InWorkObject = sketch;
                Factory2D factory = sketch.OpenEdition();
                editing = true;
                var curves = new List<Curve2D>();
                foreach (Primitive spec in side.curves)
                {
                    Curve2D curve;
                    if (spec.center == null)
                        curve = factory.CreateLine(spec.start.x, spec.start.y, spec.end.x, spec.end.y);
                    else
                    {
                        double start = Math.Atan2(spec.start.y - spec.center.y, spec.start.x - spec.center.x);
                        double low = spec.sweep > 0 ? start : start + spec.sweep;
                        curve = factory.CreateCircle(spec.center.x, spec.center.y, Distance(spec.start, spec.center),
                            low, low + Math.Abs(spec.sweep));
                    }
                    curve.set_Name("segment-" + (curves.Count + 1));
                    curves.Add(curve);
                }

                for (int i = 0; i < curves.Count; i++)
                {
                    Primitive spec = side.curves[i];
                    if (spec.center != null)
                    {
                        var radius = sketch.Constraints.AddMonoEltCst(CatConstraintType.catCstTypeRadius, Ref(part, curves[i]));
                        radius.Mode = CatConstraintMode.catCstModeDrivingDimension;
                        radius.Dimension.Value = Distance(spec.start, spec.center);
                    }
                    if (i == 0) continue;
                    Point2D priorEnd = side.curves[i - 1].sweep < 0 ? curves[i - 1].StartPoint : curves[i - 1].EndPoint;
                    Point2D nextStart = spec.sweep < 0 ? curves[i].EndPoint : curves[i].StartPoint;
                    sketch.Constraints.AddBiEltCst(CatConstraintType.catCstTypeOn, Ref(part, priorEnd), Ref(part, nextStart));
                    sketch.Constraints.AddBiEltCst(CatConstraintType.catCstTypeTangency, Ref(part, curves[i - 1]), Ref(part, curves[i]));
                }
            }
            finally
            {
                try { if (editing) sketch.CloseEdition(); }
                finally { part.InWorkObject = prior; }
            }

            part.UpdateObject(sketch);
        }

        private static SketchCheck ReadBack(Sketch sketch, double z, Side side)
        {
            object[] axis = new object[9];
            sketch.GetAbsoluteAxisData(axis);
            double[] expectedAxis = { 0, 0, z, 1, 0, 0, 0, 1, 0 };
            double maximum = axis.Select(Convert.ToDouble).Zip(expectedAxis, (a, b) => Math.Abs(a - b)).Max();
            var curves = new Dictionary<string, Curve2D>(StringComparer.Ordinal);
            var elements = sketch.GeometricElements;
            for (int i = 1; i <= elements.Count; i++)
            {
                object key = i;
                var curve = elements.Item(ref key) as Curve2D;
                if (curve != null)
                {
                    string name = curve.get_Name();
                    if (!name.StartsWith("segment-", StringComparison.Ordinal))
                    {
                        Need(curve.Construction, "Sketch " + side.id + " contains an unexpected non-construction curve: " + name);
                        continue;
                    }

                    Need(!curve.Construction, "Sketch " + side.id + " has a required curve marked as construction: " + name);
                    Need(!curves.ContainsKey(name), "Sketch " + side.id + " has a duplicate curve identity: " + name);
                    curves.Add(name, curve);
                }
            }

            Need(curves.Count == side.curves.Length, "Sketch " + side.id + " has an unexpected finite-curve count.");
            for (int i = 0; i < side.curves.Length; i++)
            {
                string name = "segment-" + (i + 1);
                Curve2D curve;
                Need(curves.TryGetValue(name, out curve), "Sketch " + side.id + " lacks the required curve: " + name);
                Primitive spec = side.curves[i];
                bool reverse = spec.sweep < 0;
                double[] parameters = ReadArray(curve.GetParamExtents);
                double length = curve.GetLengthAtParam(parameters[0], parameters[1]);
                maximum = Max(maximum, Distance(ReadPoint(curve.StartPoint.GetCoordinates), reverse ? spec.end : spec.start),
                    Distance(ReadPoint(curve.EndPoint.GetCoordinates), reverse ? spec.start : spec.end));
                if (spec.center != null)
                {
                    var circle = curve as Circle2D;
                    Need(circle != null, "Native primitive is not an arc.");
                    double radius = Distance(spec.start, spec.center);
                    maximum = Max(maximum, Math.Abs(radius - circle.Radius),
                        Distance(ReadPoint(circle.GetCenter), spec.center),
                        Math.Abs(length - radius * Math.Abs(spec.sweep)),
                        Distance(ReadPoint(v => curve.GetPointAtParam((parameters[0] + parameters[1]) / 2, v)), At(spec, .5)));
                }
                else
                {
                    Need(curve is Line2D, "Native primitive is not a line.");
                    maximum = Math.Max(maximum, Math.Abs(length - Distance(spec.start, spec.end)));
                }
            }

            int expectedConstraints = 2 * (side.curves.Length - 1) + side.curves.Count(c => c.center != null);
            Need(sketch.Constraints.Count == expectedConstraints, "Native constraint count differs.");
            for (int i = 1; i <= expectedConstraints; i++)
            {
                object key = i;
                var constraint = sketch.Constraints.Item(ref key);
                Need(!constraint.IsInactive() && constraint.Status == CatConstraintStatus.catCstStatusOK,
                    "Native sketch constraint is inactive or unsatisfied.");
            }
            Need(maximum <= Tolerance, "Native geometry residual exceeds 1e-7 mm.");
            return new SketchCheck { name = side.id, curves = curves.Count,
                activeConstraints = expectedConstraints, maxResidual = maximum };
        }

        private static Plan ReadPlan(string file, out string hash)
        {
            byte[] bytes = File.ReadAllBytes(file);
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            Plan plan = new JavaScriptSerializer().Deserialize<Plan>(Encoding.UTF8.GetString(bytes));
            Need(plan != null && plan.schema == "portfolio-cad/1" && plan.units == "mm"
                && !string.IsNullOrWhiteSpace(plan.source) && plan.sides != null && plan.sides.Length > 0 && plan.sides.Length <= 3
                && new[] { plan.planeZ, plan.zMin, plan.zMax }.All(Finite) && plan.zMin < plan.zMax,
                "Invalid synthetic CAD plan.");
            Need(plan.sides.All(s => s != null) && plan.sides.Select(s => s.id).Distinct().Count() == plan.sides.Length,
                "Missing side or duplicate side ID.");
            foreach (Side side in plan.sides)
            {
                Need(!string.IsNullOrWhiteSpace(side.id) && side.id.Length <= 64
                    && side.id.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'), "Invalid sketch side ID.");
                Need(side.curves != null && side.curves.Length > 0 && side.curves.Length <= 24, "Invalid curve list.");
                for (int i = 0; i < side.curves.Length; i++)
                {
                    Primitive p = side.curves[i];
                    Need(ValidPoint(p.start) && ValidPoint(p.end) && Distance(p.start, p.end) > Tolerance, "Invalid finite endpoints.");
                    if (p.center != null)
                        Need(ValidPoint(p.center) && Math.Abs(p.sweep) > Tolerance && Math.Abs(p.sweep) < Math.PI
                            && Distance(At(p, 1), p.end) <= Tolerance, "Invalid signed arc.");
                    else Need(p.sweep == 0, "A line must have zero sweep.");
                    if (i > 0)
                    {
                        Primitive prior = side.curves[i - 1];
                        Need(Distance(prior.end, p.start) <= Tolerance
                            && Distance(Tangent(prior, prior.end), Tangent(p, p.start)) <= Tolerance,
                            "Plan fails endpoint or G1 preflight.");
                    }
                }
            }
            return plan;
        }

        private static Sketch FindSketch(HybridBody root, string name)
        {
            for (int i = 1; i <= root.HybridSketches.Count; i++)
            {
                object key = i;
                Sketch sketch = root.HybridSketches.Item(ref key);
                if (sketch.get_Name() == name) return sketch;
            }
            throw new InvalidOperationException("Expected sketch identity is missing.");
        }

        private static HybridBody FindBody(Part part, string name)
        {
            for (int i = 1; i <= part.HybridBodies.Count; i++)
            {
                object key = i;
                HybridBody body = part.HybridBodies.Item(ref key);
                if (body.get_Name() == name) return body;
            }
            return null;
        }

        private static bool SameObject(object a, object b)
        {
            IntPtr left = Marshal.GetIUnknownForObject(a);
            try
            {
                IntPtr right = Marshal.GetIUnknownForObject(b);
                try { return left == right; }
                finally { Marshal.Release(right); }
            }
            finally { Marshal.Release(left); }
        }

        private static Reference Ref(Part part, object geometry)
        {
            return part.CreateReferenceFromObject((AnyObject)geometry);
        }
        private static double[] ReadArray(Action<object[]> getter)
        {
            var values = new object[2];
            getter(values);
            return values.Select(Convert.ToDouble).ToArray();
        }
        private static Point ReadPoint(Action<object[]> getter)
        {
            double[] pair = ReadArray(getter);
            return new Point { x = pair[0], y = pair[1] };
        }

        private static Point At(Primitive spec, double fraction)
        {
            double x = spec.start.x - spec.center.x, y = spec.start.y - spec.center.y;
            double a = spec.sweep * fraction;
            return new Point { x = spec.center.x + x * Math.Cos(a) - y * Math.Sin(a),
                y = spec.center.y + x * Math.Sin(a) + y * Math.Cos(a) };
        }
        private static Point Tangent(Primitive spec, Point point)
        {
            double x, y;
            if (spec.center == null) { x = spec.end.x - spec.start.x; y = spec.end.y - spec.start.y; }
            else { x = (spec.center.y - point.y) * Math.Sign(spec.sweep); y = (point.x - spec.center.x) * Math.Sign(spec.sweep); }
            double length = Math.Sqrt(x * x + y * y);
            Need(length > Tolerance, "Undefined tangent.");
            return new Point { x = x / length, y = y / length };
        }
        private static double Distance(Point a, Point b)
        {
            return Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        }

        private static double Max(params double[] values)
        {
            return values.Max();
        }

        private static bool Finite(double x)
        {
            return !double.IsInfinity(x) && !double.IsNaN(x) && Math.Abs(x) <= 100000;
        }

        private static bool ValidPoint(Point p)
        {
            return p != null && Finite(p.x) && Finite(p.y);
        }
        private static void Need(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private static void WriteNew(string path, object value)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
                writer.WriteLine(new JavaScriptSerializer().Serialize(value));
        }
    }
}
