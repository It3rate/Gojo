using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Tiny scan-hint surface for concept addresses. The hint itself is ordinary
/// Gojo relation structure over small numeric basis carriers; packed tokens are
/// only derived query reads over that structure.
/// </summary>
public static class ConceptHintSurface
{
    public const long AxisOrder = 1;
    public const long AxisBoundary = 2;
    public const long AxisEdge = 3;
    public const long AxisContainment = 4;
    public const long AxisSupport = 5;
    public const long AxisSourceTarget = 6;
    public const long AxisPath = 7;
    public const long AxisOpposition = 8;
    public const long AxisSimilarity = 9;
    public const long AxisSeparation = 10;
    public const long AxisIntensity = 11;
    public const long AxisRate = 12;
    public const long AxisAttention = 13;
    public const long AxisAgency = 14;
    public const long AxisUncertainty = 15;

    public const long ContextGeneral = 0;
    public const long ContextSpatial = 1;
    public const long ContextTemporal = 2;
    public const long ContextLogical = 3;
    public const long ContextSocial = 4;
    public const long ContextProcedural = 5;
    public const long ContextPerceptual = 6;
    public const long ContextDomainLocal = 7;

    public const long SideNegative = -1;
    public const long SideReference = 0;
    public const long SidePositive = 1;

    public const long StrengthWeak = 1;
    public const long StrengthOrdinary = 2;
    public const long StrengthStrong = 3;

    private const long AxisPartitionCount = 15;
    private const long ContextPartitionCount = 7;
    private const long SidePartitionRadius = 8;
    private const long StrengthPartitionCount = 3;

    private static readonly Carrier AxisCarrier = new();
    private static readonly Carrier ContextCarrier = new();
    private static readonly Carrier SideCarrier = new();
    private static readonly Carrier StrengthCarrier = new();

    public readonly record struct HintRead(
        CarrierLocation Axis,
        CarrierLocation Side,
        CarrierLocation Context,
        CarrierLocation Strength,
        IElement OpenedStructure,
        IElement ResidueOrTension);

    public static Relation Create(
        Carrier root,
        long axis,
        long side = SideReference,
        long context = ContextGeneral,
        long strength = StrengthOrdinary,
        IElement? openedStructure = null,
        IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            AxisPoint(axis),
            SidePoint(side),
            ContextPoint(context),
            StrengthPoint(strength),
            openedStructure ?? EncodedTextSurface.UnknownPoint(),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static Relation Signature(Carrier root, params IElement[] hints) =>
        Relation.OrderedGroup(EncodedTextSurface.RootOrNew(root), hints ?? []);

    public static Relation AxisBasis() => Relation.Segment(AxisCarrier, 0, AxisPartitionCount);

    public static Relation SideBasis() => Relation.Segment(SideCarrier, -SidePartitionRadius, SidePartitionRadius);

    public static Relation ContextBasis() => Relation.Segment(ContextCarrier, 0, ContextPartitionCount);

    public static Relation StrengthBasis() => Relation.Segment(StrengthCarrier, 0, StrengthPartitionCount);

    public static bool TryRead(IElement element, out HintRead read)
    {
        if (element is Relation hint &&
            EncodedTextSurface.TryReadRootElements(hint, out var elements) &&
            elements.Length >= 6 &&
            TryReadPointOn(elements[0], AxisCarrier, out var axis) &&
            TryReadPointOn(elements[1], SideCarrier, out var side) &&
            TryReadPointOn(elements[2], ContextCarrier, out var context) &&
            TryReadPointOn(elements[3], StrengthCarrier, out var strength))
        {
            read = new HintRead(
                axis,
                side,
                context,
                strength,
                elements[4],
                elements[5]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadSignature(IElement element, out IElement[] hints)
    {
        if (element is Relation signature &&
            EncodedTextSurface.TryReadRootElements(signature, out var elements) &&
            elements.All(item => TryRead(item, out _)))
        {
            hints = elements;
            return true;
        }

        hints = [];
        return false;
    }

    public static bool TryPackQueryToken(IElement hint, out uint token)
    {
        if (!TryRead(hint, out var read) ||
            read.Axis.Ticks is < 0 or > byte.MaxValue ||
            read.Context.Ticks is < 0 or > byte.MaxValue ||
            read.Side.Ticks is < -8 or > 7 ||
            read.Strength.Ticks is < 0 or > 15)
        {
            token = 0;
            return false;
        }

        token =
            (uint)read.Axis.Ticks |
            ((uint)read.Context.Ticks << 8) |
            ((uint)(read.Side.Ticks + 8) << 16) |
            ((uint)read.Strength.Ticks << 20);
        return true;
    }

    public static bool SharesAxis(IElement left, IElement right) =>
        TryRead(left, out var leftRead) &&
        TryRead(right, out var rightRead) &&
        leftRead.Axis.Ticks == rightRead.Axis.Ticks;

    public static Relation OpposedSide(IElement hint)
    {
        if (!TryRead(hint, out var read))
        {
            return Create(new Carrier(), AxisUncertainty, residueOrTension: hint);
        }

        return Create(
            new Carrier(),
            read.Axis.Ticks,
            checked(-read.Side.Ticks),
            read.Context.Ticks,
            read.Strength.Ticks,
            read.OpenedStructure,
            read.ResidueOrTension);
    }

    private static Relation AxisPoint(long ticks) => Relation.Point(AxisCarrier, ticks);

    private static Relation SidePoint(long ticks) => Relation.Point(SideCarrier, ticks);

    private static Relation ContextPoint(long ticks) => Relation.Point(ContextCarrier, ticks);

    private static Relation StrengthPoint(long ticks) => Relation.Point(StrengthCarrier, ticks);

    private static bool TryReadPointOn(
        IElement element,
        Carrier expectedCarrier,
        out CarrierLocation location)
    {
        if (EncodedTextSurface.TryReadPoint(element, out location) &&
            location.Carrier.SameIdentityAs(expectedCarrier))
        {
            return true;
        }

        location = default;
        return false;
    }
}
