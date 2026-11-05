namespace Portfolio;

public sealed record LocalFile(string Path, string Sha256);
public sealed record LocalReference(string Id, LocalFile Points, double StartMm, double? EndMm);
public sealed record SupportDomain(int[] EdgeIds, int[] Faces, XY Anchor, XY Direction,
    double Width, XYZ First, XYZ Last);
public sealed record ReviewedCorner(string Side, string Entry, SupportDomain Cap, SupportDomain Wall, double WallUShift);
public sealed record ReviewedFrame(string Id, XY P1, XY P2, XY Origin, XY Horizontal, XY Normal,
    double Offset, double UMin, double UMax, int[] SourceFaceOrder, ReviewedCorner[] Corners,
    bool ReviewExcluded, string? ReviewReason);
public sealed record LocalCase(string Schema, string Id, string DataKind, LocalFile Mesh,
    LocalReference[] References, ReviewedFrame[] Frames, double PlaneZ, double ZMin, double ZMax);
public sealed record SourceAnchor(string Id, string Frame, string Side, XYZ Point, XYZ ReferenceFoot,
    double ReferenceStation, int[] Faces, string Role);
public sealed record FrameAudit(string Id, int Intersections, int Components, string State,
    string? SelectedCover, double? Width, string Reason, int[] SourceFaces);
public sealed record SourceAudit(string Schema, string Case, string DataKind, string InputFingerprint,
    int Triangles, FrameAudit[] Frames, SourceAnchor[] Anchors, double MaximumSourceResidualMm,
    string InputScope, bool PhysicalWallAccepted = false);

public static class SourceEvidence
{
    private const double Eps=1e-7;
    public static string Resolve(string manifest, LocalFile input)
    {
        string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(manifest))!,input.Path));
        if (Data.Sha(path)!=input.Sha256) throw new InvalidDataException("Local source fingerprint mismatch.");
        return path;
    }

    public static LocalCase Load(string manifest)
    {
        LocalCase job=Data.Read<LocalCase>(manifest);
        static bool Finite(XY p)=>double.IsFinite(p.X)&&double.IsFinite(p.Y);
        static bool Finite3(XYZ p)=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&double.IsFinite(p.Z);
        static bool FileValid(LocalFile? f)=>f is not null&&!string.IsNullOrWhiteSpace(f.Path)
            &&f.Sha256 is not null&&System.Text.RegularExpressions.Regex.IsMatch(f.Sha256,"^[0-9a-f]{64}$");
        bool DomainValid(SupportDomain? d)=>d is not null&&d.EdgeIds is not null&&d.Faces is not null
            &&Finite(d.Anchor)&&Finite(d.Direction)&&Finite3(d.First)&&Finite3(d.Last)&&double.IsFinite(d.Width);
        if (job.Schema!="reviewed-source-case/1" || job.DataKind is not ("private-original" or "analytic-fixture")
            || string.IsNullOrWhiteSpace(job.Id) || !FileValid(job.Mesh)
            || job.References is null || job.References.Length!=3
            || job.References.Any(r=>r is null||string.IsNullOrWhiteSpace(r.Id)||!FileValid(r.Points)
                ||!double.IsFinite(r.StartMm)||r.StartMm<0||r.EndMm is double end&&(!double.IsFinite(end)||end<=r.StartMm))
            || job.References.Select(r=>r.Id).Distinct().Count()!=3 || job.Frames is null || job.Frames.Length==0
            || job.Frames.Any(f=>f is null||string.IsNullOrWhiteSpace(f.Id)||!Finite(f.P1)||!Finite(f.P2)
                ||!Finite(f.Origin)||!Finite(f.Normal)||!Finite(f.Horizontal)
                ||!new[]{f.Offset,f.UMin,f.UMax}.All(double.IsFinite)||f.SourceFaceOrder is null
                ||f.SourceFaceOrder.Distinct().Count()!=f.SourceFaceOrder.Length||f.Corners is null
                ||f.Corners.Any(c=>c is null||c.Side is not ("P1" or "P2")||string.IsNullOrWhiteSpace(c.Entry)
                    ||!DomainValid(c.Cap)||!DomainValid(c.Wall)||!double.IsFinite(c.WallUShift))
                ||f.Corners.Select(c=>(c.Side,c.Entry)).Distinct().Count()!=f.Corners.Length)
            || job.Frames.Select(f=>f.Id).Distinct().Count()!=job.Frames.Length
            || !new[]{job.PlaneZ,job.ZMin,job.ZMax}.All(double.IsFinite)
            || job.ZMin>=job.ZMax || job.PlaneZ<job.ZMin || job.PlaneZ>job.ZMax)
            throw new InvalidDataException("Invalid reviewed local-case contract.");
        return job;
    }

    public static (LocalCase Job, SourceAudit Audit) Inspect(string manifest)
    {
        LocalCase job=Load(manifest);
        Triangle[] mesh=Data.LoadStl(Resolve(manifest,job.Mesh));
        var refs=job.References.Select(r=>(Info:r,Points:ReferenceGeometry.Read(Resolve(manifest,r.Points)))).ToArray();
        if (refs.Any(r=>r.Points.Length<3)) throw new InvalidDataException("Reference needs three points.");
        var audits=new List<FrameAudit>(); var anchors=new List<SourceAnchor>(); double residual=0;
        foreach (ReviewedFrame frame in job.Frames)
        {
            if (Math.Abs(frame.Normal.Norm()-1)>Eps || Math.Abs(frame.Horizontal.Norm()-1)>Eps
                || Math.Abs(frame.Horizontal.Dot(frame.Normal))>Eps
                || Math.Abs(frame.Origin.Dot(frame.Normal)+frame.Offset)>Eps || frame.UMin>=frame.UMax
                || frame.SourceFaceOrder.Any(i=>i<0||i>=mesh.Length))
                throw new InvalidDataException("Invalid source frame.");
            // Full plane first, followed by the supplied original bounded cross-frame interval.
            Edge[] intersections=Slicing.Intersect(mesh,frame.Origin,frame.Normal,100_000);
            var byFace=intersections.ToDictionary(e=>e.Triangle);
            XY Across(XYZ p)=>new((p.Planar()-frame.Origin).Dot(frame.Horizontal),p.Z);
            Edge Clip(Edge e)
            {
                double a=Across(e.A).X,b=Across(e.B).X;
                if (Math.Max(a,b)<frame.UMin-Eps||Math.Min(a,b)>frame.UMax+Eps)
                    throw new InvalidDataException("Source support lies outside its supplied section range.");
                if (Math.Abs(a-b)<1e-12) return e;
                double t0=Math.Clamp((frame.UMin-a)/(b-a),0,1),t1=Math.Clamp((frame.UMax-a)/(b-a),0,1);
                return new(e.A+(e.B-e.A)*Math.Min(t0,t1),e.A+(e.B-e.A)*Math.Max(t0,t1),e.Triangle);
            }
            double Distance(XYZ p,Edge e)
            {
                XYZ d=e.B-e.A; double denominator=d.X*d.X+d.Y*d.Y+d.Z*d.Z;
                if (denominator<=0) return (p-e.A).Norm();
                XYZ q=p-e.A; double t=Math.Clamp((q.X*d.X+q.Y*d.Y+q.Z*d.Z)/denominator,0,1);
                return (p-e.A-d*t).Norm();
            }
            Edge[] Validate(SupportDomain domain,double shift)
            {
                if (domain.EdgeIds.Length==0 || domain.EdgeIds.Length!=domain.Faces.Length
                    || domain.EdgeIds.Distinct().Count()!=domain.EdgeIds.Length
                    || !double.IsFinite(domain.Width) || domain.Width<0 || domain.Width>.1
                    || Math.Abs(domain.Direction.Norm()-1)>Eps)
                    throw new InvalidDataException("Invalid reviewed support domain.");
                var edges=new List<Edge>();
                foreach (var (edge,face) in domain.EdgeIds.Zip(domain.Faces))
                {
                    if (edge<0||edge>=frame.SourceFaceOrder.Length||frame.SourceFaceOrder[edge]!=face||!byFace.TryGetValue(face,out Edge? found))
                        throw new InvalidDataException("Support edge/triangle identity differs from the original section.");
                    Edge clipped=Clip(found);edges.Add(clipped);
                    foreach (XYZ p in new[]{clipped.A,clipped.B})
                    {
                        double band=Math.Abs((Across(p)-domain.Anchor-new XY(shift,0)).Cross(domain.Direction));
                        if (band>domain.Width/2+Eps) throw new InvalidDataException("Source point lies outside its reviewed supporting strip.");
                    }
                }
                double error=Math.Max(edges.Min(e=>Distance(domain.First,e)),edges.Min(e=>Distance(domain.Last,e)));
                residual=Math.Max(residual,error);
                if (error>Eps) throw new InvalidDataException("Reviewed endpoint is not on its actual finite source edge.");
                return [..edges];
            }
            foreach (ReviewedCorner corner in frame.Corners)
            {Validate(corner.Cap,0);Validate(corner.Wall,corner.WallUShift);}
            FrameAudit Audit(string state,string reason,string? cover=null,double? width=null)=>new(frame.Id,
                intersections.Length,Slicing.Components(intersections).Length,state,cover,width,reason,
                frame.Corners.SelectMany(c=>c.Cap.Faces.Concat(c.Wall.Faces)).Distinct().Order().ToArray());
            ReviewedCorner[] left=frame.Corners.Where(c=>c.Side=="P1").ToArray(), right=frame.Corners.Where(c=>c.Side=="P2").ToArray();
            if (left.Length!=2||right.Length!=2)
            {audits.Add(Audit("unresolved","Insufficient archived source support domains; original failure retained."));continue;}
            bool Reused(ReviewedCorner[] entries)=>entries[0].Cap.EdgeIds.Order().SequenceEqual(entries[1].Cap.EdgeIds.Order());
            if (Reused(left)||Reused(right))
            {audits.Add(Audit("unresolved","The same complete source cap domain was reused for two entries."));continue;}
            ExactCorner Corner(ReviewedCorner c)=>SupportGeometry.Intersect(new(c.Cap.Anchor,c.Cap.Direction,c.Cap.Width),
                new(c.Wall.Anchor,c.Wall.Direction,c.Wall.Width),c.WallUShift);
            var a=left.Select(c=>(Source:c,Point:Corner(c))).OrderBy(c=>c.Point.Z).ToArray();
            var b=right.Select(c=>(Source:c,Point:Corner(c))).OrderBy(c=>c.Point.Z).ToArray();
            if (a[0].Point.Z.CompareTo(a[1].Point.Z)==0||b[0].Point.Z.CompareTo(b[1].Point.Z)==0)
            {audits.Add(Audit("unresolved","Exact relative-height order is tied."));continue;}
            Rational w0=Rational.Abs(b[0].Point.U-a[0].Point.U),w1=Rational.Abs(b[1].Point.U-a[1].Point.U);
            if (w0.CompareTo(w1)==0)
            {audits.Add(Audit("unresolved","Exact engineering width is tied."));continue;}
            int selected=w0.CompareTo(w1)>0?0:1;
            ReviewedCorner[] chosen=[a[selected].Source,b[selected].Source];
            bool contradiction=left.Any(x=>right.Any(y=>x.Cap.EdgeIds.Order().SequenceEqual(y.Cap.EdgeIds.Order())
                && chosen.Contains(x)!=chosen.Contains(y)));
            if (contradiction||frame.ReviewExcluded)
            {audits.Add(Audit("review-excluded",contradiction?"Shared source cap identity contradicts the pair.":frame.ReviewReason??"Source review exclusion."));continue;}
            foreach (ReviewedCorner c in chosen)
            {
                double CapDistance(XYZ p)=>Math.Abs((Across(p)-c.Cap.Anchor).Cross(c.Cap.Direction));
                XYZ point=CapDistance(c.Wall.First)<=CapDistance(c.Wall.Last)?c.Wall.First:c.Wall.Last;
                XY location=c.Side=="P1"?frame.P1:frame.P2;
                var feet=refs.Select(r=>ReferenceGeometry.Foot(r.Info.Id,r.Points,location)).OrderBy(f=>f.Distance).ToArray();
                var foot=feet[0]; LocalReference range=job.References.Single(r=>r.Id==foot.Side);
                if (foot.Station<range.StartMm || range.EndMm is double end && foot.Station>end) continue;
                Edge[] wall=c.Wall.Faces.Select(face=>Clip(byFace[face])).ToArray();
                int[] faces=wall.Where(e=>Distance(point,e)<=Eps).Select(e=>e.Triangle).Distinct().Order().ToArray();
                anchors.Add(new(frame.Id+"/"+c.Side,frame.Id,foot.Side,point,foot.Point,foot.Station,faces,
                    "original support endpoint used as approximate engineering anchor; not a certified fillet-end point"));
            }
            audits.Add(Audit("engineering-width-candidate","Reviewed source domains; nominal width only, physical cover identity not certified.",
                chosen[0].Entry+"/"+chosen[1].Entry,(selected==0?w0:w1).Value));
        }
        string fingerprint=Data.Sha(manifest)+":"+job.Mesh.Sha256+":"+string.Join(":",job.References.Select(r=>r.Points.Sha256));
        return (job,new("source-audit/1",job.Id,job.DataKind,fingerprint,mesh.Length,[..audits],[..anchors],residual,
            "Archived support domains and review exclusions supplied as explicit input; original STL independently re-sliced. No automatic physical wall/contact certification."));
    }
}
