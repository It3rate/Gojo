namespace ScratchMill.KiCad;

public class KcPosition : IKiCadElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Orientation { get; set; }
    public static KcPosition Empty => new KcPosition(0, 0, 0);
    public KcPosition()
    {
    }
    public KcPosition(double x, double y)
    {
        X = x;
        Y = y;
        Orientation = 0;
    }
    public KcPosition(double x, double y, double orientation)
    {
        X = x;
        Y = y;
        Orientation = orientation;
    }

    public static double MAX_DIFF = 0.05; // TODO: This needs to be based on how large circuit elements are.
    public bool IsNear(KcPosition other)
    {
        return Math.Abs(X - other.X) < MAX_DIFF && Math.Abs(Y - other.Y) < MAX_DIFF;
    }
    public KcPosition Nearest(KcPosition a, KcPosition b)
    {
        var distA = Math.Abs(X - a.X) + Math.Abs(Y - a.Y);
        var distB = Math.Abs(X - b.X) + Math.Abs(Y - b.Y);
        return distA < distB ? a : b;
    }

    public static KcPosition Parse(List<object> items)
    {
        var result = new KcPosition();
        result.X = Convert.ToDouble(items[1]);
        result.Y = Convert.ToDouble(items[2]);
        if (items.Count > 3)
        {
            result.Orientation = Convert.ToDouble(items[3]);
        }
        return result;
    }
    public override string ToString() => $"pos[{X} {Y} o{Orientation}]";


    public static List<KcPosition> ParsePoints(List<object> items)
    {
        var result = new List<KcPosition>();
        var kind = (string)items[0];
        if (kind == "pts" && items[1] is List<object>)
        {
            for (int i = 1; i < items.Count; i++)
            {
                if (items[i] is List<object> ptList)
                {
                    result.Add(Parse(ptList));
                }
            }
        }
        return result;
    }
    public static bool operator ==(KcPosition? a, KcPosition? b)
    {
        if (a is null && b is null)
        {
            return true;
        }
        if (a is null || b is null)
        {
            return false;
        }
        return a.Equals(b);
    }
    public static bool operator !=(KcPosition? a, KcPosition? b)
    {
        return !(a == b);
    }
    public override bool Equals(object? obj)
    {
        return obj is KcPosition other && Equals(other);
    }
    public bool Equals(KcPosition? other)
    {
        if (other is null) { return false; }
        return ReferenceEquals(this, other) || (X == other.X && Y == other.Y && Orientation == other.Orientation);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y) + (int)(Orientation * 19);
    }
}
