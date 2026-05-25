namespace Gojo.Core;

public sealed partial class Relation
{
    #region Identity and Duality

    /// <summary>Duals a point by negating its location and other relations recursively.</summary>
    public Relation Dual() => IsPoint(out var location, out var carrier)
        ? new(location.Dual(), carrier)
        : new(Left.Dual(), Right.Dual());

    /// <summary>Swaps traversal order without changing either side's duality.</summary>
    public Relation Reverse() => new(Right, Left);

    /// <summary>Returns the structural dual as an element.</summary>
    IElement IElement.Dual() => Dual();

    /// <summary>Compares physical relation identity rather than structural equality.</summary>
    public bool SameIdentityAs(Relation other) => ReferenceEquals(this, other);

    #endregion

    #region Structure Inspection

    /// <summary>Returns all carrier locations contained by this relation.</summary>
    public CarrierLocation[] CarrierLocations()
    {
        return [.. LeftCarrierLocations(), .. RightCarrierLocations()];
    }

    /// <summary>Returns all carriers referenced by this relation or nested elements.</summary>
    public Carrier[] Carriers() => [.. CarriersOf(Left), .. CarriersOf(Right)];

    /// <summary>Returns carrier locations from the left element.</summary>
    public CarrierLocation[] LeftCarrierLocations() => CarrierLocationsOf(Left);

    /// <summary>Returns carrier locations from the right element.</summary>
    public CarrierLocation[] RightCarrierLocations() => CarrierLocationsOf(Right);

    /// <summary>Reads this relation as two direct carrier-location slots.</summary>
    public bool TryGetCarrierLocations(out CarrierLocation left, out CarrierLocation right)
    {
        if (Left is CarrierLocation leftLocation && Right is CarrierLocation rightLocation)
        {
            left = leftLocation;
            right = rightLocation;
            return true;
        }

        left = default;
        right = default;
        return false;
    }

    /// <summary>Reads ordered additive participants attached to a holder origin.</summary>
    public bool TryReadAdditiveParticipantsOn(Carrier holder, out IElement[] participants)
    {
        if (TryReadAdditiveAttachment(this, holder, out var participant))
        {
            participants = [participant];
            return true;
        }

        if (Right is Carrier groupHolder && groupHolder.SameIdentityAs(holder))
        {
            var collected = new List<IElement>();
            CollectAdditiveParticipants(Left, holder, collected);
            participants = [.. collected];
            return true;
        }

        participants = [];
        return false;
    }

    /// <summary>Reads ordered elements attached to a holder origin.</summary>
    public bool TryReadOrderedElementsOn(Carrier holder, out IElement[] elements) =>
        TryReadAdditiveParticipantsOn(holder, out elements);

    /// <summary>Reads the first and last ordered elements when they close through shared coordinate refs.</summary>
    public bool TryReadOrderedClosureOn(Carrier holder, out Relation closure)
    {
        if (!TryReadOrderedElementsOn(holder, out var elements) ||
            elements.Length < 2 ||
            !TryReadClosedOrderedEndpoints(elements[0], elements[^1]))
        {
            closure = null!;
            return false;
        }

        closure = new Relation(elements[0], elements[^1]);
        return true;
    }

    /// <summary>Creates a fill basis only when this ordered group closes through shared endpoint refs.</summary>
    public bool TryCreateContourFillBasisOn(Carrier holder, out Carrier fill)
    {
        if (!TryReadOrderedClosureOn(holder, out _))
        {
            fill = null!;
            return false;
        }

        fill = CreateFillBasis();
        return true;
    }

    /// <summary>Reads the additive unit participation of holder participants.</summary>
    public bool TryReadAdditiveUnitShapeOn(Carrier holder, out UnitShape unitShape)
    {
        if (!TryReadAdditiveParticipantsOn(holder, out var participants) ||
            participants.Length == 0)
        {
            unitShape = null!;
            return false;
        }

        var shapes = new List<UnitShape>();
        foreach (var participant in participants)
        {
            if (!TryReadElementUnitShape(participant, out var shape))
            {
                unitShape = null!;
                return false;
            }

            shapes.Add(shape);
        }

        unitShape = UnitShape.Additive(shapes);
        return true;
    }

    /// <summary>Returns the direct left carrier-location slot or throws.</summary>
    public CarrierLocation LeftLocation => Left is CarrierLocation location
        ? location
        : throw new InvalidOperationException("The left relation slot is not a carrier location.");

    /// <summary>Returns the direct right carrier-location slot or throws.</summary>
    public CarrierLocation RightLocation => Right is CarrierLocation location
        ? location
        : throw new InvalidOperationException("The right relation slot is not a carrier location.");

    #endregion

    #region Equality and Formatting

    /// <summary>Compares relation shape and element values, not physical identity.</summary>
    public bool Equals(Relation? other) =>
        other is not null &&
        Equals(Left, other.Left) &&
        Equals(Right, other.Right);

    public override bool Equals(object? obj) => obj is Relation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Left, Right);

    public override string ToString() => $"{Left} -> {Right}";

    #endregion

    #region Element Traversal Helpers

    /// <summary>Collects carrier locations from an element tree.</summary>
    private static CarrierLocation[] CarrierLocationsOf(IElement element) => element switch
    {
        CarrierLocation location => [location],
        Relation relation => relation.CarrierLocations(),
        _ => []
    };

    /// <summary>Collects carriers from an element tree.</summary>
    private static Carrier[] CarriersOf(IElement element) => element switch
    {
        Carrier carrier => [carrier],
        CarrierLocation location => [location.Carrier],
        Relation relation => relation.Carriers(),
        _ => []
    };

    /// <summary>Builds an ordered chain of additive holder attachments.</summary>
    private static IElement ChainAdditiveAttachments(
        Carrier holder,
        IReadOnlyList<IElement> participants)
    {
        if (participants.Count == 0)
        {
            // Non-origin marker keeps the empty group from reading as holder -> holder.
            return Point(holder, 1);
        }

        IElement chain = AdditiveAttachment(holder, participants[0]);
        for (var index = 1; index < participants.Count; index++)
        {
            chain = new Relation(chain, AdditiveAttachment(holder, participants[index]));
        }

        return chain;
    }

    /// <summary>Collects ordered participants from a nested additive group chain.</summary>
    private static void CollectAdditiveParticipants(
        IElement element,
        Carrier holder,
        List<IElement> participants)
    {
        if (TryReadAdditiveAttachment(element, holder, out var participant))
        {
            participants.Add(participant);
            return;
        }

        if (element is Relation relation)
        {
            CollectAdditiveParticipants(relation.Left, holder, participants);
            CollectAdditiveParticipants(relation.Right, holder, participants);
        }
    }

    /// <summary>Reads one holder-origin additive attachment.</summary>
    private static bool TryReadAdditiveAttachment(
        IElement element,
        Carrier holder,
        out IElement participant)
    {
        if (element is Relation relation &&
            relation.Left is Relation holderOrigin &&
            holderOrigin.IsPoint(out var origin, out var originCarrier) &&
            originCarrier.SameIdentityAs(holder) &&
            origin.Ticks == 0)
        {
            participant = relation.Right;
            return true;
        }

        participant = default!;
        return false;
    }

    /// <summary>Returns true when two ordered endpoint elements expose matching shared coordinates.</summary>
    private static bool TryReadClosedOrderedEndpoints(IElement first, IElement last)
    {
        if (!TryReadOrderedCarrierLocations(first, out var firstLocations) ||
            !TryReadOrderedCarrierLocations(last, out var lastLocations))
        {
            return false;
        }

        return firstLocations.Length != 0 &&
            firstLocations.Length == lastLocations.Length &&
            firstLocations.Zip(lastLocations).All(pair =>
                pair.First.Carrier.SameIdentityAs(pair.Second.Carrier) &&
                pair.First.SameTickRefAs(pair.Second));
    }

    /// <summary>Reads an ordered position's coordinate locations without counting its holder origin.</summary>
    private static bool TryReadOrderedCarrierLocations(IElement element, out CarrierLocation[] locations)
    {
        if (element is CarrierLocation location)
        {
            locations = [location];
            return true;
        }

        if (element is Relation relation &&
            relation.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out var elements) &&
            elements.All(item => item is CarrierLocation))
        {
            locations = [.. elements.Cast<CarrierLocation>()];
            return true;
        }

        locations = [];
        return false;
    }

    /// <summary>Reads the unit shape carried by a participant element.</summary>
    private static bool TryReadElementUnitShape(IElement element, out UnitShape unitShape)
    {
        switch (element)
        {
            case Carrier carrier:
                unitShape = carrier.UnitShape;
                return true;
            case CarrierLocation location:
                unitShape = location.Carrier.UnitShape;
                return true;
            case Relation relation:
                return relation.TryReadUnitShape(out unitShape);
            default:
                unitShape = null!;
                return false;
        }
    }

    #endregion
}
