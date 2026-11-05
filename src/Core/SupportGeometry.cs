using System.Numerics;

namespace Portfolio;

/// <summary>Exact arithmetic on the binary values actually supplied in a reviewed model.</summary>
public readonly struct Rational : IComparable<Rational>
{
    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }
    public Rational(BigInteger n, BigInteger d)
    {
        if (d.IsZero) throw new InvalidDataException("Zero exact denominator.");
        if (d.Sign < 0) { n = -n; d = -d; }
        BigInteger gcd = BigInteger.GreatestCommonDivisor(n, d);
        Numerator = n / gcd; Denominator = d / gcd;
    }
    public static Rational From(double value)
    {
        if (!double.IsFinite(value)) throw new InvalidDataException("Nonfinite exact input.");
        long bits = BitConverter.DoubleToInt64Bits(value);
        int exponent = (int)((bits >> 52) & 2047);
        BigInteger mantissa = bits & 0x000fffffffffffffL;
        if (exponent != 0) mantissa += BigInteger.One << 52;
        int power = exponent == 0 ? -1074 : exponent - 1075;
        if (bits < 0) mantissa = -mantissa;
        return power >= 0 ? new(mantissa << power, 1) : new(mantissa, BigInteger.One << -power);
    }
    public int Sign => Numerator.Sign;
    public double Value
    {
        get
        {
            int a=(int)Math.Max(0,BigInteger.Abs(Numerator).GetBitLength()-54);
            int b=(int)Math.Max(0,Denominator.GetBitLength()-54);
            return Math.ScaleB((double)(Numerator>>a)/(double)(Denominator>>b),a-b);
        }
    }
    public static Rational operator +(Rational a, Rational b) => new(a.Numerator*b.Denominator+b.Numerator*a.Denominator,a.Denominator*b.Denominator);
    public static Rational operator -(Rational a, Rational b) => new(a.Numerator*b.Denominator-b.Numerator*a.Denominator,a.Denominator*b.Denominator);
    public static Rational operator *(Rational a, Rational b) => new(a.Numerator*b.Numerator,a.Denominator*b.Denominator);
    public static Rational operator /(Rational a, Rational b) => new(a.Numerator*b.Denominator,a.Denominator*b.Numerator);
    public static Rational Abs(Rational a) => new(BigInteger.Abs(a.Numerator),a.Denominator);
    public int CompareTo(Rational b) => (Numerator*b.Denominator).CompareTo(b.Numerator*Denominator);
}

public sealed record SupportLine(XY Anchor, XY Direction, double Width);
public readonly record struct ExactCorner(Rational U, Rational Z);

public static class SupportGeometry
{
    /// <summary>Minimum-width strip from all hull-edge directions; no least-squares acceptance shortcut.</summary>
    public static SupportLine Strip(IEnumerable<XY> input)
    {
        XY[] points = input.Distinct().OrderBy(p=>p.X).ThenBy(p=>p.Y).ToArray();
        if (points.Length < 2) throw new InvalidDataException("Support needs two distinct points.");
        var hull = new List<XY>();
        foreach (XY p in points)
        {
            while (hull.Count >= 2 && (hull[^1]-hull[^2]).Cross(p-hull[^1]) <= 0) hull.RemoveAt(hull.Count-1);
            hull.Add(p);
        }
        int lower = hull.Count;
        foreach (XY p in points.Reverse().Skip(1))
        {
            while (hull.Count > lower && (hull[^1]-hull[^2]).Cross(p-hull[^1]) <= 0) hull.RemoveAt(hull.Count-1);
            hull.Add(p);
        }
        hull.RemoveAt(hull.Count-1);
        if (hull.Count == 1) hull.Add(points[^1]);
        SupportLine? best = null;
        for (int i=0;i<hull.Count;i++)
        {
            XY direction=(hull[(i+1)%hull.Count]-hull[i]).Unit(), normal=direction.Left();
            double low=points.Min(p=>p.Dot(normal)),high=points.Max(p=>p.Dot(normal));
            double width=high-low;
            if (best is null || width < best.Width)
                best=new(normal*((low+high)/2),direction,width);
        }
        return best!;
    }

    public static ExactCorner Intersect(SupportLine a, SupportLine b, double bShift = 0)
    {
        Rational ax=Rational.From(a.Anchor.X), az=Rational.From(a.Anchor.Y);
        Rational bx=Rational.From(b.Anchor.X)+Rational.From(bShift), bz=Rational.From(b.Anchor.Y);
        Rational ux=Rational.From(a.Direction.X), uz=Rational.From(a.Direction.Y);
        Rational vx=Rational.From(b.Direction.X), vz=Rational.From(b.Direction.Y);
        Rational determinant=ux*vz-uz*vx;
        if (determinant.Sign==0) throw new InvalidDataException("Parallel source support models.");
        Rational t=((bx-ax)*vz-(bz-az)*vx)/determinant;
        return new(ax+ux*t,az+uz*t);
    }
}
