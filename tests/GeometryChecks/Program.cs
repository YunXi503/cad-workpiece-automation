using Portfolio;

static void Need(bool condition,string reason)
{if(!condition)throw new InvalidDataException(reason);}
double[] numbers=[0,-0.0,double.Epsilon,-double.Epsilon,Math.ScaleB(1,-1022),1,-1,
    Math.BitIncrement(1),Math.BitDecrement(1),1e-200,-1e200,double.MaxValue,-double.MaxValue];
foreach(double value in numbers)Need(Rational.From(value).Value==value,"Exact input display conversion");
Need(Rational.From(Math.BitIncrement(1)).CompareTo(Rational.From(1))>0,"Adjacent binary values must not tie");
Need((Rational.From(1e-200)/Rational.From(1e-200)).Value==1,"Large exact denominator");
var intersection=SupportGeometry.Intersect(new(new(0,1),new(1,1),0),new(new(0,3),new(1,-1),0));
Need(intersection.U.Value==1&&intersection.Z.Value==2,"Exact line intersection");
XY[] rectangle=[new(0,0),new(8,0),new(8,.07),new(0,.07),new(3,.02)];
XY[] rotated=rectangle.Select(p=>p.Rotate(.73)+new XY(31,-17)).ToArray();
var strip=SupportGeometry.Strip(rotated.Reverse());
Need(Math.Abs(strip.Width-.07)<1e-12&&rotated.All(p=>Math.Abs((p-strip.Anchor).Cross(strip.Direction))<=strip.Width/2+1e-12),"Minimax strip and containment");
Curve arc=new(new XY(1,0),new XY(0,1),new XY(0,0),Math.PI/2);
Need(Math.Abs(Verification.PointDistance(new(-1,0),arc)-Math.Sqrt(2))<1e-12,"Finite arc must clamp to an endpoint");
Curve reverse=new(arc.End,arc.Start,arc.Center,-arc.Sweep);
foreach(XY p in new XY[]{new(-1,0),new(1.3,.8),new(0,0)})
    Need(Math.Abs(Verification.PointDistance(p,arc)-Verification.PointDistance(p,reverse))<1e-12,"Signed finite arc distance");
Curve[] line=[new(new XY(0,0),new XY(10,0))];
double xy=Verification.PointDistance(new XY(5,.2),line[0]);
double tall=SourceVerification.FiniteSurfaceDistance(new XYZ(5,.2,2),line,-3,3);
double shortHeight=SourceVerification.FiniteSurfaceDistance(new XYZ(5,.2,2),line,-1,1);
Need(xy==tall&&Math.Abs(shortHeight-Math.Sqrt(1.04))<1e-12,"Fixed XY with insufficient finite surface height");
Console.WriteLine("Geometry math: exact binary predicates, subnormal values, support strip, finite signed arc and 3D height control passed.");
