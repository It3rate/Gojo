namespace ScratchMill.KiCad;

public interface IKiCadElement { }
public interface IKcLayerElement : IKiCadElement
{
    bool IsOnLayer(KcLayer layer);
}
public interface IKcNetElement : IKiCadElement
{
    int NetId { get; }
    bool IsOnNet(KcNet layer);
}
public interface IKcCopperElement : IKcLayerElement, IKcNetElement
{
    KcPosition At { get; }
}
public interface IKcDrillable : IKcCopperElement
{
    double DrillSize { get; }
    bool IsThroughHole { get; }
}
public interface IKcInnerSegment : IKcCopperElement
{
    KcPosition End { get; }

    void Flip();
    bool IsFlipped { get; }

    /// <summary>
    /// Zero if no touch, +1 if other start touches this end, -1 if other end touches this start.
    int IsContiguous(IKcInnerSegment other, bool useNear);
    /// <summary>
    /// Zero if no match, -1 if other start points touch, +1 if end points touch.
    int HasCommonEndPoint(IKcInnerSegment other, bool useNear);
    /// <summary>
    /// True if any terminal points are common.
    bool HasSharedPoint(IKcInnerSegment other, bool useNear);
}

public abstract class KcInnerSegment : IKcInnerSegment
{
    public KcPosition At { get; set; } = new KcPosition();
    public KcPosition End { get; set; } = new KcPosition();
    public string Layer { get; set; }
    public int NetId { get; set; }
    public string UUID { get; set; }

    public bool IsFlipped { get; private set; } = false;
    public void Flip()
    {
        var temp = At;
        At = End;
        End = temp;
        IsFlipped = !IsFlipped;
    }
    public bool HasSharedPoint(IKcInnerSegment other, bool useNear)
    {
        bool result;
        if (useNear)
        {
            result = At.IsNear(other.At) || End.IsNear(other.End) || At.IsNear(other.End) || End.IsNear(other.At);
        }
        else
        {
            result = At.Equals(other.At) || End.Equals(other.End) || At.Equals(other.End) || End.Equals(other.At);
        }

        return result;
    }
    public int HasCommonEndPoint(IKcInnerSegment other, bool useNear)
    {
        int result;
        if (useNear)
        {
            result = At.IsNear(other.At) ? -1 : End.IsNear(other.End) ? 1 : 0;
        }
        else
        {
            result = At.Equals(other.At) ? -1 : End.Equals(other.End) ? 1 : 0;
        }

        return result;
    }
    public int IsContiguous(IKcInnerSegment other, bool useNear)
    {
        int result;
        if (useNear)
        {
            result = End.IsNear(other.At) ? 1 : At.IsNear(other.End) ? -1 : 0;
        }
        else
        {
            result = End.Equals(other.At) ? 1 : At.Equals(other.End) ? -1 : 0;
        }
        return result;
    }

    public bool IsOnLayer(KcLayer layer) => Layer == layer.Name;
    public bool IsOnNet(KcNet net) => NetId == net.Id;


}