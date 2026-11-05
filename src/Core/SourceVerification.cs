using System.Text.Json;

namespace Portfolio;

public sealed record SourceSideCheck(string Id,int SourceSupportEndpoints,int DerivedPoints,int BaselineCount,int ReducedCount,
    double DraftXYMaximum,double UniformXYMaximum,double CadXYMaximum,double EndpointResidualMm,
    double JointResidualMm,double UnitTangentDifference,string RoundingState,double? RoundingMovementBoundMm,
    double SourceAnchorFiniteSurfaceMaximumMm,string SourceAnchorContactState,string CadStage);
public sealed record SourceReport(string Schema,string Case,string DataKind,string InputFingerprint,int OriginalTriangles,
    int OriginalFrames,int EngineeringWidthFrames,int UnresolvedFrames,int ReviewExcludedFrames,int SourceSupportEndpoints,
    double SourceIdentityResidualMaxMm,SourceSideCheck[] Sides,string CadPlanSha256,string Scope,bool PhysicalContactAccepted=false);

/// <summary>Independent finite-distance/derivative checks plus a deterministic source/algorithm replay.</summary>
public static class SourceVerification
{
    private const double Eps=1e-7;
    private static void Need(bool condition,string message)
    {if(!condition)throw new InvalidDataException(message);}
    public static SourceReport Verify(string manifest,string output)
    {
        SourceRun stored=Data.Read<SourceRun>(Path.Combine(output,"source-run.json"));
        SourceRun replay=SourcePipeline.Run(manifest);
        string Json<T>(T value)=>JsonSerializer.Serialize(value,Data.Json);
        Need(Json(stored)==Json(replay),"Stored source geometry differs from source/algorithm replay.");
        Need(Json(stored.Run)==Json(Data.Read<RunResult>(Path.Combine(output,"run.json")))
            && Json(stored.Cad)==Json(Data.Read<CadPlan>(Path.Combine(output,"cad-plan.json"))),
            "Standalone run or CAD plan differs from the source run.");
        Need(File.ReadAllText(Path.Combine(output,"overlay.svg")).ReplaceLineEndings("\n")==Drawing.Svg(stored.Run),
            "Displayed SVG differs from the stored geometry.");
        var checks=new List<SourceSideCheck>();
        foreach(var(side,draft) in stored.Run.Sides.Zip(stored.Draft.Sides))
        {
            Need(side.Fit is not null,"A source side remains withheld; no complete CAD verification.");
            FitResult fit=side.Fit!;XY[] points=draft.Points.Select(p=>p.Planar()).ToArray();
            XY[] uniform=Sampling.Uniform(points,.5);
            double Maximum(Curve[] chain,XY[] samples)=>samples.Max(p=>chain.Min(c=>Verification.PointDistance(p,c)));
            Curve[] exported=stored.Cad.Sides.Single(s=>s.Id==side.Id).Curves;
            var baseline=Chain(fit.Baseline);var selected=Chain(fit.Curves);var cad=Chain(exported);
            double raw=Maximum(fit.Curves,points),equal=Maximum(fit.Curves,uniform),xy=Math.Max(Maximum(exported,points),Maximum(exported,uniform));
            Need(raw<=.3&&equal<=.3&&xy<=.3&&Maximum(fit.Baseline,points)<=.3&&Maximum(fit.Baseline,uniform)<=.3,
                "An original/uniform finite XY distance exceeds unchanged 0.3 mm.");
            Need(Math.Abs(raw-fit.OriginalError)<=Eps&&Math.Abs(equal-fit.UniformError)<=Eps
                && points.Contains(fit.WorstPoint)&&Math.Abs(fit.Curves.Min(c=>Verification.PointDistance(fit.WorstPoint,c))-raw)<=Eps,
                "Fitting metrics or worst original point differ.");
            double endpoint=new[]{fit.Baseline,fit.Curves,exported}.Max(chain=>Math.Max((chain[0].Start-points[0]).Norm(),(chain[^1].End-points[^1]).Norm()));
            double joint=Math.Max(baseline.Joint,Math.Max(selected.Joint,cad.Joint));
            double tangent=Math.Max(baseline.Tangent,Math.Max(selected.Tangent,cad.Tangent));
            Need(endpoint<=Eps&&joint<=Eps&&tangent<=Eps,"Endpoint, joint or G1 residual exceeds 1e-7.");
            double? movement=null;
            if(side.Rounded is RoundResult round)
            {
                Need(round.Curves.SequenceEqual(exported)&&round.Curves.Length==fit.Curves.Length,"Rounded CAD geometry differs.");
                for(int i=0;i<exported.Length;i++)
                {
                    Curve a=fit.Curves[i],b=exported[i];
                    Need(a.IsArc==b.IsArc&&Math.Sign(a.Sweep)==Math.Sign(b.Sweep),"Rounding changed primitive type or direction.");
                    double old=a.IsArc?a.Radius:a.Length,next=b.IsArc?b.Radius:b.Length;
                    Need(Math.Abs(Math.Round(old,2,MidpointRounding.AwayFromZero)-next)<=Eps,"Rounded dimension is not the requested 0.01 mm value.");
                }
                movement=Movement(fit.Curves,exported);
                Need(movement<=.05&&Math.Abs(movement.Value-round.MovementBound)<=Eps,"Independent rounding movement bound exceeds 0.05 mm or differs.");
            }
            else Need(exported.SequenceEqual(fit.Curves)&&side.GeometryStage=="unrounded-engineering-draft",
                "Unrounded export stage is not explicit.");
            double contact=draft.SourceSupports.Max(anchor=>FiniteSurfaceDistance(anchor.Point,exported,stored.Cad.ZMin,stored.Cad.ZMax));
            checks.Add(new(side.Id,draft.SourceSupports.Length,points.Length,fit.Baseline.Length,fit.Curves.Length,
                raw,equal,xy,endpoint,joint,tangent,side.Rounded is null?"withheld":"checked",movement,
                contact,contact<=.3?"within-source-anchor-sample-limit":"exceeds-source-anchor-sample-limit",side.GeometryStage!));
        }
        SourceAudit audit=stored.Source;
        return new("reviewed-source-verification/1",stored.Run.Case,audit.DataKind,audit.InputFingerprint,audit.Triangles,
            audit.Frames.Length,audit.Frames.Count(f=>f.State=="engineering-width-candidate"),audit.Frames.Count(f=>f.State=="unresolved"),
            audit.Frames.Count(f=>f.State=="review-excluded"),audit.Anchors.Length,audit.MaximumSourceResidualMm,[..checks],Data.Sha(Path.Combine(output,"cad-plan.json")),
            "Original local source/support domains revalidated; draft, rounding, native CAD and physical manufacturing acceptance are separate. Contact sample covers source anchor endpoints only, not the full STL or a physically accepted contact band.");
    }

    public static double FiniteSurfaceDistance(XYZ point,Curve[] curves,double zMin,double zMax)
    {
        double xy=curves.Min(c=>Verification.PointDistance(point.Planar(),c));
        double height=Math.Max(0,Math.Max(zMin-point.Z,point.Z-zMax));
        return Math.Sqrt(xy*xy+height*height);
    }
    private static (double Joint,double Tangent) Chain(Curve[] curves)
    {
        Need(curves.Length>0,"Empty finite chain.");double joint=0,tangent=0;
        XY Derivative(Curve c,XY p)=>c.Center is XY o?(p-o).Left().Unit()*Math.Sign(c.Sweep):(c.End-c.Start).Unit();
        for(int i=0;i<curves.Length;i++)
        {
            Curve c=curves[i];
            Need(double.IsFinite(c.Length)&&c.Length>Eps&&double.IsFinite(c.Sweep),"Degenerate finite primitive.");
            if(c.Center is XY center)
            {
                Need(c.Radius>Eps&&Math.Abs(c.Sweep)>Eps&&Math.Abs(c.Sweep)<Math.PI
                    &&Math.Abs((c.End-center).Norm()-c.Radius)<=Eps,"Inconsistent circle or finite sweep.");
                double a=Math.Atan2(c.Start.Y-center.Y,c.Start.X-center.X)+c.Sweep;
                XY finish=center+new XY(Math.Cos(a),Math.Sin(a))*c.Radius;
                Need((finish-c.End).Norm()<=Eps,"Finite arc endpoint does not match signed sweep.");
            }
            else Need(c.Sweep==0,"Line has a circular sweep.");
            if(i==0)continue;
            joint=Math.Max(joint,(curves[i-1].End-c.Start).Norm());
            tangent=Math.Max(tangent,(Derivative(curves[i-1],curves[i-1].End)-Derivative(c,c.Start)).Norm());
        }
        return (joint,tangent);
    }
    private static double Movement(Curve[] first,Curve[] second)
    {
        const int divisions=64;double bound=0;
        for(int i=0;i<first.Length;i++)
        {
            Curve a=first[i],b=second[i];
            double high=Math.Max((a.Start-b.Start).Norm(),(a.End-b.End).Norm());
            if(a.IsArc)
            {
                XY At(Curve c,double t)
                {
                    XY o=c.Center!.Value;double angle=Math.Atan2(c.Start.Y-o.Y,c.Start.X-o.X)+c.Sweep*t;
                    return o+new XY(Math.Cos(angle),Math.Sin(angle))*c.Radius;
                }
                for(int j=1;j<divisions;j++)high=Math.Max(high,(At(a,(double)j/divisions)-At(b,(double)j/divisions)).Norm());
                high+=(a.Length*Math.Abs(a.Sweep)+b.Length*Math.Abs(b.Sweep))/(8*divisions*divisions);
            }
            bound=Math.Max(bound,high);
        }
        return bound;
    }
}
