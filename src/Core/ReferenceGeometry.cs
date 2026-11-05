namespace Portfolio;

public sealed record ReferenceFoot(string Side, XYZ Point, double Station, double Distance);

public static class ReferenceGeometry
{
    public static XYZ[] Read(string path) => File.ReadLines(path).Where(l=>!string.IsNullOrWhiteSpace(l)).Select(line=>
    {
        string[] tokens=line.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
        if(tokens is ["X",var x,"Y",var y,"Z",var z])return Data.Point(x+" "+y+" "+z);
        return Data.Point(line);
    }).ToArray();

    public static double[] Stations(XYZ[] points,bool planar=false)
    {
        var result=new double[points.Length];
        for(int i=1;i<points.Length;i++) result[i]=result[i-1]+(planar?(points[i]-points[i-1]).Planar().Norm():(points[i]-points[i-1]).Norm());
        return result;
    }
    public static double Interpolate(double s,double[] stations,double[] values)
    {
        if(s<=stations[0])return values[0];
        if(s>=stations[^1])return values[^1];
        int index=Array.BinarySearch(stations,s);
        if(index>=0)return values[index];
        index=~index;
        double t=(s-stations[index-1])/(stations[index]-stations[index-1]);
        return values[index-1]+t*(values[index]-values[index-1]);
    }
    public static XYZ At(double s,double[] stations,XYZ[] values)=>new(
        Interpolate(s,stations,values.Select(p=>p.X).ToArray()),
        Interpolate(s,stations,values.Select(p=>p.Y).ToArray()),
        Interpolate(s,stations,values.Select(p=>p.Z).ToArray()));
    public static double[] Grid(double start,double end,int count)
    {
        if(count<2||start>=end)throw new InvalidDataException("Invalid sampling interval.");
        double[] grid=Enumerable.Range(0,count).Select(i=>start+(end-start)*i/(count-1)).ToArray();
        grid[^1]=end;return grid;
    }
    public static XYZ[] Uniform(XYZ[] points,double spacing)
    {
        if(!(spacing>0)||points.Length<2)throw new InvalidDataException("Invalid spatial resampling input.");
        double[] raw=Stations(points);
        int[] ids=Enumerable.Range(0,points.Length).Where(i=>i==0||raw[i]-raw[i-1]>1e-9).ToArray();
        if(ids.Length<2)throw new InvalidDataException("Degenerate spatial polyline.");
        double[] s=ids.Select(i=>raw[i]).ToArray();XYZ[] p=ids.Select(i=>points[i]).ToArray();
        return Grid(0,s[^1],Math.Max(2,(int)Math.Ceiling(s[^1]/spacing)+1)).Select(t=>At(t,s,p)).ToArray();
    }
    public static ReferenceFoot Foot(string side,XYZ[] points,XY location)
    {
        double[] stations=Stations(points,true);ReferenceFoot? best=null;
        for(int i=1;i<points.Length;i++)
        {
            XY d=(points[i]-points[i-1]).Planar(); double den=d.Dot(d);
            if(den==0)continue;
            double t=Math.Clamp((location-points[i-1].Planar()).Dot(d)/den,0,1);
            XYZ point=points[i-1]+(points[i]-points[i-1])*t;double distance=(point.Planar()-location).Norm();
            if(best is null||distance<best.Distance)best=new(side,point,stations[i-1]+t*(stations[i]-stations[i-1]),distance);
        }
        return best??throw new InvalidDataException("Degenerate reference polyline.");
    }
}

/// <summary>A natural cubic through explicit engineering knots, with no smoothing term.</summary>
public sealed class NaturalSpline
{
    private readonly double[] station;
    private readonly XYZ[] point,second;
    public NaturalSpline(double[] stations,XYZ[] points)
    {
        if(stations.Length<2||stations.Length!=points.Length
            ||stations.Zip(stations.Skip(1)).Any(p=>p.Second<=p.First))
            throw new InvalidDataException("Natural-spline stations must strictly increase.");
        station=[..stations];point=[..points];int n=points.Length;
        double[] diagonal=new double[n],upper=new double[n],lower=new double[n];
        XYZ[] rhs=new XYZ[n];diagonal[0]=diagonal[^1]=1;
        for(int i=1;i+1<n;i++)
        {
            double left=station[i]-station[i-1],right=station[i+1]-station[i];
            lower[i]=left;upper[i]=right;diagonal[i]=2*(left+right);
            rhs[i]=((point[i+1]-point[i])*(1/right)-(point[i]-point[i-1])*(1/left))*6;
        }
        for(int i=1;i<n;i++)
        {
            double factor=lower[i]/diagonal[i-1];diagonal[i]-=factor*upper[i-1];rhs[i]-=rhs[i-1]*factor;
        }
        second=new XYZ[n];second[^1]=rhs[^1]*(1/diagonal[^1]);
        for(int i=n-2;i>=0;i--)second[i]=(rhs[i]-second[i+1]*upper[i])*(1/diagonal[i]);
    }
    public XYZ At(double s)
    {
        int i=Array.BinarySearch(station,s);if(i<0)i=~i-1;i=Math.Clamp(i,0,station.Length-2);
        double h=station[i+1]-station[i],a=(station[i+1]-s)/h,b=(s-station[i])/h;
        return point[i]*a+point[i+1]*b+(second[i]*(a*a*a-a)+second[i+1]*(b*b*b-b))*(h*h/6);
    }
}
