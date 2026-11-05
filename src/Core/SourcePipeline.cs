namespace Portfolio;

public sealed record SourceSideResult(string Id,GeneralFitResult? Geometry,string? FitFailure,
    bool RoundingPassed,string? RoundingFailure);
public sealed record SourceRun(string Schema,SourceAudit Source,DraftData Draft,SourceSideResult[] Fitting,
    RunResult Run,CadPlan Cad,string CadScope);

public static class SourcePipeline
{
    public static SourceRun Run(string manifest)
    {
        var(job,audit)=SourceEvidence.Inspect(manifest);
        DraftData draft=EngineeringDraft.Build(manifest,job,audit);
        var results=new List<SideResult>();var records=new List<SourceSideResult>();
        foreach(DraftSide side in draft.Sides)
        {
            Console.Error.WriteLine("Fitting reviewed engineering draft: "+side.Id);
            Station[] stations=side.Points.Select((p,i)=>new Station(i,"inferred",p.Planar(),[],null,p.Z,"engineering-draft")).ToArray();
            GeneralFitResult? fitted=null;RoundResult? rounded=null;string? fitIssue=null,roundIssue=null;
            try
            {
                fitted=GeneralFitting.Fit(side.Points.Select(p=>p.Planar()).ToArray());
                try
                {
                    rounded=Fitting.Round(fitted.Fit.Curves);
                    XY[] points=side.Points.Select(p=>p.Planar()).ToArray();
                    if(Math.Max(Fitting.Maximum(rounded.Curves,points),Fitting.Maximum(rounded.Curves,Sampling.Uniform(points,.5)))>.3)
                        throw new InvalidDataException("Rounded draft exceeds unchanged 0.3 mm XY limit.");
                }
                catch(InvalidDataException e){rounded=null;roundIssue=e.Message;}
            }
            catch(InvalidDataException e){fitIssue=e.Message;}
            string stage=fitted is null?"withheld":rounded is null?"unrounded-engineering-draft":"rounded-engineering-draft";
            results.Add(new(side.Id,stations,fitted?.Fit,rounded,fitIssue??(roundIssue is null?null:"Rounding withheld: "+roundIssue),side.SourceSupports,stage,
                audit.Frames.Select(f=>new SourceFrameState(f.Id,f.State)).ToArray()));
            records.Add(new(side.Id,fitted,fitIssue,rounded is not null,roundIssue));
        }
        var run=new RunResult("portfolio-run/1",job.Id,audit.InputFingerprint,[..results]);
        var plan=new CadPlan("portfolio-cad/1","mm",audit.InputFingerprint,job.PlaneZ,job.ZMin,job.ZMax,
            results.Where(s=>s.Fit is not null).Select(s=>new CadSide(s.Id,s.Rounded?.Curves??s.Fit!.Curves)).ToArray());
        return new("reviewed-source-run/1",audit,draft,[..records],run,plan,
            "Engineer-editable draft. CAD exports fitted finite G1 geometry; rounding stage is explicit per side. No physical fillet-end or manufacturing-contact acceptance.");
    }
}
