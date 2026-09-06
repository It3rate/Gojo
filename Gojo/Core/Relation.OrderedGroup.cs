namespace Gojo.Core;

public sealed partial class Relation
{
    /// <summary>
    /// Reads exactly one complete ordered group. Unlike a discovery traversal,
    /// every branch must be an attachment to the same holder. A bare point or
    /// a skipped malformed branch is not an empty group.
    /// </summary>
    public bool TryReadOrderedGroup(out IElement[] elements)
    {
        if (Right is Carrier holder)
        {
            if (IsGroupPoint(Left, holder, 1))
            {
                elements = [];
                return true;
            }

            var collected = new List<IElement>();
            if (TryCollectGroupAttachments(Left, holder, collected))
            {
                elements = [.. collected];
                return true;
            }
        }

        elements = [];
        return false;
    }

    private static bool TryCollectGroupAttachments(IElement element, Carrier holder, List<IElement> elements)
    {
        if (element is not Relation relation) return false;
        if (IsGroupPoint(relation.Left, holder, 0))
        {
            elements.Add(relation.Right);
            return true;
        }

        return TryCollectGroupAttachments(relation.Left, holder, elements) &&
            TryCollectGroupAttachments(relation.Right, holder, elements);
    }

    private static bool IsGroupPoint(IElement element, Carrier holder, long ticks) =>
        element is Relation { Left: CarrierLocation location, Right: Carrier carrier } &&
        carrier.SameIdentityAs(holder) && location.Carrier.SameIdentityAs(holder) && location.Ticks == ticks;
}
