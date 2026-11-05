namespace Portfolio;

/// <summary>Independent open-ended shell strips for mathematical regression, not a replacement for Case A.</summary>
public static class ReviewedFixture
{
    public static void Generate(string output)
    {
        Directory.CreateDirectory(output);
        XY[] profile=[new(-12,5),new(12,5),new(12,1),new(10,1),new(10,-5),new(-10,-5),new(-10,1),new(-12,1)];
        var triangles=new List<Triangle>();var tracks=new List<(string Id,XYZ[] Centers,XYZ[][] Rings,int Base)>();
        for(int side=0;side<3;side++)
        {
            double sign=side==1?-1:1,angle=.7*sign;
            XY p=new(18.17,0),center=p+new XY(0,sign*40.24);
            XY q=center+(p-center).Rotate(angle),end=q+new XY(19.38,0).Rotate(angle);
            Curve[] route=[new(new XY(0,0),p),new(p,q,center,angle),new(q,end)];
            var samples=new List<(XY Point,XY Normal)>();
            foreach(Curve curve in route)
            {
                int n=(int)Math.Ceiling(curve.Length);
                for(int k=samples.Count==0?0:1;k<=n;k++)samples.Add((curve.At((double)k/n),curve.Tangent((double)k/n).Left()));
            }
            XY origin=new(side*250,side*90);
            XYZ[] centers=samples.Select(s=>new XYZ(s.Point.X+origin.X,s.Point.Y+origin.Y,0)).ToArray();
            XYZ[][] rings=samples.Select(s=>profile.Select(p=>
            {
                XY point=origin+s.Point+s.Normal*p.X;return new XYZ(point.X,point.Y,p.Y);
            }).ToArray()).ToArray();
            int start=triangles.Count;
            for(int i=0;i+1<rings.Length;i++)for(int j=0;j<profile.Length;j++)
            {
                int next=(j+1)%profile.Length;
                triangles.Add(new(rings[i][j],rings[i+1][j],rings[i+1][next]));
                triangles.Add(new(rings[i][j],rings[i+1][next],rings[i][next]));
            }
            tracks.Add((new[]{"upper","lower","left"}[side],centers,rings,start));
        }
        string meshPath=Path.Combine(output,"scan.stl");Data.WriteStl(meshPath,triangles);
        Triangle[] mesh=[..triangles];var references=new List<LocalReference>();var frames=new List<ReviewedFrame>();
        foreach(var track in tracks)
        {
            string referencePath=Path.Combine(output,track.Id+".asc");
            File.WriteAllLines(referencePath,track.Centers.Select(Data.Coordinates));
            references.Add(new(track.Id,new(Path.GetFileName(referencePath),Data.Sha(referencePath)),0,null));
            for(int i=2;i+3<track.Centers.Length;i+=3)
            {
                XY origin=((track.Centers[i]+track.Centers[i+1])*.5).Planar();
                XY normal=(track.Centers[i+1]-track.Centers[i]).Planar().Unit(),horizontal=normal.Left();
                Edge[] all=Slicing.Intersect(mesh,origin,normal,100_000);
                int[] sourceOrder=all.Select(e=>e.Triangle).ToArray();
                var index=sourceOrder.Select((face,k)=>(face,k)).ToDictionary(p=>p.face,p=>p.k);
                var byFace=all.ToDictionary(e=>e.Triangle);
                SupportDomain Domain(int profileEdge)
                {
                    int first=track.Base+i*16+profileEdge*2;
                    Edge[] edges=new[]{first,first+1}.Where(byFace.ContainsKey).Select(id=>byFace[id]).ToArray();
                    if(edges.Length==0)throw new InvalidDataException("Analytic support did not intersect.");
                    XYZ[] points=edges.SelectMany(e=>new[]{e.A,e.B}).ToArray();
                    XY UZ(XYZ p)=>new((p.Planar()-origin).Dot(horizontal),p.Z);
                    SupportLine line=SupportGeometry.Strip(points.Select(UZ));
                    XYZ a=points.MinBy(p=>UZ(p).Dot(line.Direction)),b=points.MaxBy(p=>UZ(p).Dot(line.Direction));
                    return new(edges.Select(e=>index[e.Triangle]).ToArray(),edges.Select(e=>e.Triangle).ToArray(),
                        line.Anchor,line.Direction,line.Width,a,b);
                }
                SupportDomain top=Domain(0),bottom=Domain(4);
                ReviewedCorner[] corners=[new("P1","A",top,Domain(7),0),new("P1","B",bottom,Domain(5),0),
                    new("P2","A",top,Domain(1),0),new("P2","B",bottom,Domain(3),0)];
                frames.Add(new(track.Id+"-"+i,origin,origin,origin,horizontal,normal,-origin.Dot(normal),-15,15,
                    sourceOrder,corners,i==8,i==8?"Analytic review-exclusion control; raw section remains present.":null));
            }
        }
        var job=new LocalCase("reviewed-source-case/1","analytic-reviewed-shell","analytic-fixture",
            new("scan.stl",Data.Sha(meshPath)),[..references],[..frames],0,-8,8);
        Data.Write(Path.Combine(output,"case.json"),job);
    }
}
