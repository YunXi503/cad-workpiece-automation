namespace Portfolio;

public sealed record DraftSide(string Id, XYZ[] ReferencePoints, double[] ReferenceStations,
    SourceAnchor[] SourceSupports, XYZ[] EngineeringKnots, XYZ[] Points,
    int StartClampedKnots, int EndClampedKnots);
public sealed record DraftData(string Schema,string Case,string DataKind,string InputFingerprint,
    DraftSide[] Sides,string PointRole,bool PhysicalContactAccepted=false);

public static class EngineeringDraft
{
    public static DraftData Build(string manifest,LocalCase job,SourceAudit audit)
    {
        var sides=new List<DraftSide>();
        foreach(LocalReference reference in job.References)
        {
            XYZ[] original=ReferenceGeometry.Read(SourceEvidence.Resolve(manifest,reference.Points));
            double[] originalStations=ReferenceGeometry.Stations(original,true);
            double start=reference.StartMm,end=reference.EndMm??originalStations[^1];
            if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||start>=end||end>originalStations[^1])
                throw new InvalidDataException("Active reference range lies outside the original guide.");
            double[] stations=[start,..originalStations.Where(s=>s>start&&s<end),end];
            if(stations.Length<3)throw new InvalidDataException("Active reference range needs three knots.");
            XYZ[] guide=stations.Select(s=>ReferenceGeometry.At(s,originalStations,original)).ToArray();
            SourceAnchor[] supports=audit.Anchors.Where(a=>a.Side==reference.Id).ToArray();
            if(supports.Length<2)throw new InvalidDataException("Insufficient reviewed source anchors for engineering interpolation.");
            var groups=supports.GroupBy(a=>a.ReferenceStation).OrderBy(g=>g.Key).ToArray();
            if(groups.Length<2)throw new InvalidDataException("Engineering offsets need two distinct reference stations.");
            double Median(IEnumerable<double> values)
            {
                double[] sorted=values.Order().ToArray();int m=sorted.Length/2;
                return sorted.Length%2==1?sorted[m]:(sorted[m-1]+sorted[m])/2;
            }
            double[] anchorStations=groups.Select(g=>g.Key).ToArray();
            double[] dx=groups.Select(g=>Median(g.Select(a=>a.Point.X-a.ReferenceFoot.X))).ToArray();
            double[] dy=groups.Select(g=>Median(g.Select(a=>a.Point.Y-a.ReferenceFoot.Y))).ToArray();
            double[] z=groups.Select(g=>Median(g.Select(a=>a.Point.Z))).ToArray();
            XYZ[] knots=guide.Select((p,i)=>new XYZ(p.X+ReferenceGeometry.Interpolate(stations[i],anchorStations,dx),
                p.Y+ReferenceGeometry.Interpolate(stations[i],anchorStations,dy),
                ReferenceGeometry.Interpolate(stations[i],anchorStations,z))).ToArray();
            var spline=new NaturalSpline(stations,knots);
            double[] samples=ReferenceGeometry.Grid(start,end,Math.Max(2,(int)Math.Ceiling((end-start)/.5)+1));
            XYZ[] points=ReferenceGeometry.Uniform(samples.Select(spline.At).ToArray(),.5);
            if(points.Any(p=>!double.IsFinite(p.Norm())))throw new InvalidDataException("Nonfinite engineering draft.");
            sides.Add(new(reference.Id,guide,stations,supports,knots,points,
                stations.Count(s=>s<anchorStations[0]),stations.Count(s=>s>anchorStations[^1])));
        }
        return new("engineering-draft/1",job.Id,job.DataKind,audit.InputFingerprint,[..sides],
            "All output points are engineering-derived. Source support endpoints are separate; no derived point is certified as an original STL fillet endpoint.");
    }
}
