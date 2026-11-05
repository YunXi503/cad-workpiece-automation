namespace Portfolio;

public sealed record KnotTrial(int RemovedSourceIndex,int BeforeCount,int CandidateCount,
    bool Accepted,double? MaximumXY,string Reason);
public sealed record GeneralFitResult(FitResult Fit,int[] BaselineKnots,int[] SelectedKnots,KnotTrial[] Trials,
    bool BudgetReached=false,bool GlobalMinimumProved=false);

/// <summary>Ordered line supports, tangent circular fillets and verified support-removal reduction.</summary>
public static class GeneralFitting
{
    private const double SupportBudget=.1;
    private const double CornerBudget=.17;
    private const int TrialBudget=2000;

    public static GeneralFitResult Fit(XY[] points)
    {
        if(points.Length<5||points.Any(p=>!double.IsFinite(p.X)||!double.IsFinite(p.Y)))
            throw new InvalidDataException("General fitting needs finite ordered points.");
        XY[] uniform=Sampling.Uniform(points,.5);
        var knots=new List<int>{0};
        while(knots[^1]<points.Length-1)
        {
            int start=knots[^1],end=start+1;
            for(int next=start+2;next<points.Length;next++)
            {
                var chord=new Curve(points[start],points[next]);
                if(chord.Length<=1e-9||Enumerable.Range(start,next-start+1).Any(i=>chord.Distance(points[i])>SupportBudget))break;
                end=next;
            }
            knots.Add(end);
        }
        // A numerically collinear knot adds no new geometric support.
        for(int i=1;i+1<knots.Count;)
        {
            XY a=(points[knots[i]]-points[knots[i-1]]).Unit(),b=(points[knots[i+1]]-points[knots[i]]).Unit();
            if(a.Dot(b)>0&&Math.Abs(a.Cross(b))<1e-10)knots.RemoveAt(i);else i++;
        }
        Curve[] baseline=Build(points,knots);
        Check(baseline,points,uniform);
        int[] baselineKnots=[..knots];var trials=new List<KnotTrial>();
        Curve[] selected=baseline;
        for(int pass=0;pass<8&&trials.Count<TrialBudget;pass++)
        {
            bool changed=false;
            int[] order=Enumerable.Range(1,Math.Max(0,knots.Count-2)).ToArray();
            if(pass%2==1)Array.Reverse(order);
            // Record source indices, so later removals do not change the identity of a trial.
            int[] candidates=order.Select(i=>knots[i]).ToArray();
            foreach(int sourceIndex in candidates)
            {
                if(trials.Count>=TrialBudget)break;
                int at=knots.IndexOf(sourceIndex);if(at<=0||at>=knots.Count-1)continue;
                var proposed=knots.Where((_,i)=>i!=at).ToList();
                try
                {
                    Curve[] geometry=Build(points,proposed);
                    double error=Check(geometry,points,uniform);
                    bool accept=geometry.Length<selected.Length;
                    trials.Add(new(sourceIndex,selected.Length,geometry.Length,accept,error,
                        accept?"All finite original/uniform distances, endpoints and G1 passed.":"No primitive reduction."));
                    if(accept){knots=proposed;selected=geometry;changed=true;}
                }
                catch(InvalidDataException error)
                {trials.Add(new(sourceIndex,selected.Length,0,false,null,error.Message));}
            }
            if(!changed)break;
        }
        double raw=Fitting.Maximum(selected,points),equal=Fitting.Maximum(selected,uniform);
        XY worst=points.MaxBy(p=>selected.Min(c=>c.Distance(p)));
        var fit=new FitResult(baseline,selected,raw,equal,baseline.Length-selected.Length,trials.Count(t=>!t.Accepted),worst);
        return new(fit,baselineKnots,[..knots],[..trials],trials.Count>=TrialBudget);
    }

    private static Curve[] Build(XY[] points,List<int> knots)
    {
        if(knots.Count<2)throw new InvalidDataException("Insufficient line supports.");
        XY[] nodes=knots.Select(i=>points[i]).ToArray();
        var arcs=new Curve?[nodes.Length];
        for(int i=1;i+1<nodes.Length;i++)
        {
            XY u=(nodes[i]-nodes[i-1]).Unit(),v=(nodes[i+1]-nodes[i]).Unit();
            double turn=Math.Atan2(u.Cross(v),u.Dot(v));
            if(Math.Abs(turn)<1e-10||Math.Abs(turn)>=Math.PI-1e-7)
                throw new InvalidDataException("Parallel or reversed corner needs a different ordered support partition.");
            double trim=Math.Min(.45*Math.Min((nodes[i]-nodes[i-1]).Norm(),(nodes[i+1]-nodes[i]).Norm()),
                CornerBudget/Math.Tan(Math.Abs(turn)/4));
            double radius=trim/Math.Tan(Math.Abs(turn)/2);
            if(!double.IsFinite(radius)||radius<=1e-7||trim<=1e-7)throw new InvalidDataException("Fillet collapsed.");
            XY start=nodes[i]-u*trim,end=nodes[i]+v*trim,center=start+u.Left()*(Math.Sign(turn)*radius);
            arcs[i]=new(start,end,center,turn);
        }
        var chain=new List<Curve>();
        for(int i=0;i+1<nodes.Length;i++)
        {
            XY start=i==0?nodes[0]:arcs[i]!.End;
            XY end=i+1==nodes.Length-1?nodes[^1]:arcs[i+1]!.Start;
            if((end-start).Norm()<=1e-7)throw new InvalidDataException("Finite straight support vanished.");
            chain.Add(new(start,end));
            if(i+1<nodes.Length-1)chain.Add(arcs[i+1]!);
        }
        return [..chain];
    }

    private static double Check(Curve[] curves,XY[] original,XY[] uniform)
    {
        if((curves[0].Start-original[0]).Norm()>1e-7||(curves[^1].End-original[^1]).Norm()>1e-7)
            throw new InvalidDataException("Fitting changed original draft endpoints.");
        for(int i=0;i<curves.Length;i++)
        {
            Curve c=curves[i];
            if(!double.IsFinite(c.Length)||c.Length<=1e-7||(c.At(1)-c.End).Norm()>1e-7)
                throw new InvalidDataException("Invalid finite primitive.");
            if(i>0&&((curves[i-1].End-c.Start).Norm()>1e-7||(curves[i-1].Tangent(1)-c.Tangent(0)).Norm()>1e-7))
                throw new InvalidDataException("Finite chain lost endpoint connectivity or G1.");
        }
        double error=Math.Max(Fitting.Maximum(curves,original),Fitting.Maximum(curves,uniform));
        if(error>Fitting.FitLimit)throw new InvalidDataException("Finite point distance exceeds unchanged 0.3 mm draft limit.");
        return error;
    }
}
