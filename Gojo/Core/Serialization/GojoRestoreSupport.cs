namespace Gojo.Core;

// These one-use construction hooks are private to graph restoration. Loading
// captured state is neither a new input delta nor attachment activation. No
// partly linked shell leaves the decoder, and public unit promises stay fixed.
public sealed partial class Carrier
{
    private bool awaitingRestoration;

    internal static Carrier CreateRestorationShell(bool isInverse) =>
        new(isInverse, UnitShape.Atom(Unknown)) { awaitingRestoration = true };

    internal void CompleteRestoration(UnitShape unit, TickRef value, TickRef pressure, Carrier? opposite, Relation[] attached)
    {
        if (!awaitingRestoration) throw new InvalidOperationException("Carrier is not a restoration shell.");
        UnitShape = unit;
        valueTick = value;
        tensionTick = pressure;
        inverse = opposite;
        relations.AddRange(attached);
        awaitingRestoration = false;
    }
}

public sealed partial class Relation
{
    private bool awaitingRestoration;

    internal static Relation CreateRestorationShell() =>
        new(Carrier.Unknown, Carrier.Unknown) { awaitingRestoration = true };

    internal void CompleteRestoration(IElement left, IElement right)
    {
        if (!awaitingRestoration) throw new InvalidOperationException("Relation is not a restoration shell.");
        Left = left;
        Right = right;
        awaitingRestoration = false;
    }
}

public sealed partial class UnitShape
{
    private bool awaitingRestoration;

    internal static UnitShape CreateRestorationShell() =>
        new(Carrier.Unknown) { awaitingRestoration = true };

    internal void CompleteRestoration(IElement nextSurface, IElement[] nextDescriptors)
    {
        if (!awaitingRestoration) throw new InvalidOperationException("Unit is not a restoration shell.");
        surface = nextSurface;
        descriptors = nextDescriptors;
        awaitingRestoration = false;
    }
}
