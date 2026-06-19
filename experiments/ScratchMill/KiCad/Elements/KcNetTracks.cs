using SkiaSharp;
using System.Text;

namespace ScratchMill.KiCad.Elements;

public interface ICarvable
{
    public SKPath GetOutlinePath(SKMatrix transform, float contourOffset = 0);
    public List<SKPath[]> GetFillPath(SKMatrix transform, float contourOffset = 0);
    public string ToSvg(bool writeToDisk = true);
}
public class KcNetTracks
{
    public List<IKcCopperElement> Elements { get; private set; }
    public List<List<IKcCopperElement>> ClosedShapes { get; private set; } = new List<List<IKcCopperElement>>();
    public bool HasWire { get; private set; }
    public KcPad? LargestPad { get; }
    public KcNetTracks(IEnumerable<IKcCopperElement> elements)
    {
        Elements = new List<IKcCopperElement>(elements);
        LargestPad = GetLargestPad();
        SortTipToTail();
    }
    private KcPad? GetLargestPad()
    {
        return Elements.Select(e => e as KcPad)
                .Where(p => p != null)
                .OrderByDescending(p => p.RectArea + (p.IsThroughHole ? -100 : 0))
                .FirstOrDefault();
    }

    public void SortTipToTail()
    {
        HasWire = false;
        if (Elements.Count < 1)
        {
            return;
        }
        ClosedShapes.Clear();

        var endElements = new List<IKcCopperElement>();
        var wireSegments = new List<IKcInnerSegment>();
        foreach (var element in Elements)
        {
            if (element is IKcInnerSegment inner)
            {
                wireSegments.Add(inner);
                HasWire = true;
            }
            else
            {
                endElements.Add(element);
            }
        }

        while (wireSegments.Count > 0)
        {
            var sortedElements = new List<IKcCopperElement>();
            var currentElement = wireSegments.First();
            sortedElements.Add(currentElement); // start every chain with one element
            wireSegments.Remove(currentElement);
            while (wireSegments.Count > 0)
            {
                var nextElement = wireSegments.FirstOrDefault(e => e.HasSharedPoint(currentElement, true));
                if (nextElement == null) // if this direction is endpoint, move to the other end. If still no match, give up.
                {
                    currentElement = (currentElement == sortedElements[0]) ? (IKcInnerSegment)sortedElements[sortedElements.Count - 1] : (IKcInnerSegment)sortedElements[0];
                    nextElement = wireSegments.FirstOrDefault(e => e.HasSharedPoint(currentElement, true));
                }

                if (nextElement != null)
                {
                    if (nextElement.HasCommonEndPoint(currentElement, true) != 0)
                    {
                        nextElement.Flip();
                    }
                    int offset = nextElement.IsContiguous(currentElement, true) == 1 ? 0 : 1;

                    sortedElements.Insert(sortedElements.IndexOf(currentElement) + offset, nextElement);
                    wireSegments.Remove(nextElement);
                    currentElement = nextElement;
                }
                else
                {
                    // there are no endpoint matches, so there is a new wire. Nets like gnd can have many wires.
                    break;
                }
            }
            // todo: ensure clockwise
            ClosedShapes.Add(sortedElements);
        }

        // Add non path shapes (Pads, Vias, teardrops etc)
        var endStops = new List<KcPosition>();
        foreach (var chain in ClosedShapes)
        {
            endStops.Add(chain.First().At);
            endStops.Add(chain.Last() is IKcInnerSegment inner ? inner.End : chain.Last().At); // will always be inner element
        }

        // match terminal elements to their segments (place pad at end of wire)
        foreach (var element in endElements)
        {
            var nearest = element.At;
            var nearestIndex = 0;
            for (var i = 0; i < endStops.Count; i++)
            {
                if (element.At.Nearest(nearest, endStops[i]) == endStops[i])
                {
                    nearest = endStops[i];
                    nearestIndex = i;
                }
            }
            var shapeIndex = (int)(nearestIndex / 2);
            var isStart = shapeIndex * 2 == nearestIndex;
            if (ClosedShapes.Count > 0)
            {
                if (isStart)
                {
                    ClosedShapes[shapeIndex].Insert(0, element);
                }
                else
                {
                    ClosedShapes[shapeIndex].Add(element);
                }
            }
            else
            {
                ClosedShapes.Add([element]);
            }
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var track in Elements)
        {
            sb.Append(track.ToString());
            sb.Append(System.Environment.NewLine);
        }
        return sb.ToString();
    }
}
