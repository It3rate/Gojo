using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace GojoCAD.Structures;

/// <summary>
/// Relation-shaped parameter surfaces for graphic structures. These helpers
/// create ordinary Gojo structure: normalized basis reads, resolution evidence,
/// template bindings, and concrete instance results.
/// </summary>
public static class GraphicParameterSurface
{
    /// <summary>Creates an ordered parameter/evidence frame.</summary>
    public static Relation Frame(Carrier root, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), elements ?? []);

    /// <summary>
    /// Creates a non-rendering normalized value read:
    /// basis, 0..scale span, coarse parameter, refined parameter, coarse
    /// result, refined result, resolution ladder, provenance.
    /// </summary>
    public static Relation NormalizedValue(
        Carrier root,
        IElement basis,
        Carrier unit,
        long scaleTicks,
        long coarseTicks,
        long refinedTicks,
        IElement coarseResult,
        IElement refinedResult,
        IElement resolutionLadder,
        IElement provenance)
    {
        var parameterCarrier = RootOrNew(unit);
        return Relation.OrderedGroup(
            RootOrNew(root),
            basis ?? UnknownPoint(),
            Relation.Segment(parameterCarrier, 0, scaleTicks),
            Relation.Point(parameterCarrier, coarseTicks),
            Relation.Point(parameterCarrier, refinedTicks),
            coarseResult ?? UnknownPoint(),
            refinedResult ?? UnknownPoint(),
            resolutionLadder ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates resolution-ladder evidence for a scalar parameter. Level scales
    /// need not be decimal; they are read exactly as supplied.
    /// </summary>
    public static Relation ResolutionValue(
        Carrier root,
        IReadOnlyList<long> levelScales,
        IReadOnlyList<long> levelTicks,
        out Carrier[] levels,
        out long compositeTicks)
    {
        var holder = RootOrNew(root);
        levels = ResolutionLadder.CreateLevels(holder, Math.Max(0, CountOf(levelScales)));
        var steps = CreateScaleSteps(levels, levelScales);
        var structure = ResolutionLadder.Attach(holder, levels, steps);
        ResolutionLadder.SetDigitsLowToHigh(levels, [.. levelTicks]);
        compositeTicks = ResolutionLadder.ReadComposite(levels, levelScales);
        return structure;
    }

    /// <summary>
    /// Records that a concrete result is an instance of a reusable template
    /// under a relation-shaped binding frame.
    /// </summary>
    public static Relation TemplateInstance(
        Carrier root,
        IElement template,
        IElement bindings,
        IElement result,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            template ?? UnknownPoint(),
            bindings ?? Frame(new Carrier()),
            result ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>Reads a normalized value surface.</summary>
    public static bool TryReadNormalizedValue(
        Relation value,
        out IElement basis,
        out Relation parameterSpan,
        out CarrierLocation coarseParameter,
        out CarrierLocation refinedParameter,
        out IElement coarseResult,
        out IElement refinedResult,
        out IElement resolutionLadder,
        out IElement provenance)
    {
        if (TryReadRootElements(value, out var elements) &&
            elements.Length == 8 &&
            elements[1] is Relation span &&
            TryReadParameterPoint(elements[2], out var coarse) &&
            TryReadParameterPoint(elements[3], out var refined))
        {
            basis = elements[0];
            parameterSpan = span;
            coarseParameter = coarse;
            refinedParameter = refined;
            coarseResult = elements[4];
            refinedResult = elements[5];
            resolutionLadder = elements[6];
            provenance = elements[7];
            return true;
        }

        basis = default!;
        parameterSpan = null!;
        coarseParameter = default;
        refinedParameter = default;
        coarseResult = default!;
        refinedResult = default!;
        resolutionLadder = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads a template instance surface.</summary>
    public static bool TryReadTemplateInstance(
        Relation instance,
        out IElement template,
        out IElement bindings,
        out IElement result,
        out IElement provenance)
    {
        if (TryReadRootElements(instance, out var elements) &&
            elements.Length == 4 &&
            StructuralPattern.ReadSlots(elements[0]) is { Length: > 0 } slots &&
            elements[1] is Relation bindingFrame &&
            FocusSurface.TryReadBindingFrame(bindingFrame, out var bindingClauses) &&
            bindingClauses.Any(binding => slots.Any(slot => Equals(slot, binding.Left))))
        {
            template = elements[0];
            bindings = elements[1];
            result = elements[2];
            provenance = elements[3];
            return true;
        }

        template = default!;
        bindings = default!;
        result = default!;
        provenance = default!;
        return false;
    }

    private static Relation[] CreateScaleSteps(IReadOnlyList<Carrier> levels, IReadOnlyList<long> levelScales)
    {
        var stepCount = Math.Max(0, CountOf(levels) - 1);
        var steps = new Relation[stepCount];
        for (var index = 0; index < stepCount; index++)
        {
            var lowerScale = ReadScale(levelScales, index);
            var upperScale = ReadScale(levelScales, index + 1);
            steps[index] = new Relation(
                Relation.Point(levels[index], upperScale),
                Relation.Point(levels[index + 1], lowerScale));
        }

        return steps;
    }

    private static long ReadScale(IReadOnlyList<long> scales, int index)
    {
        if (CountOf(scales) == 0)
        {
            return 0;
        }

        return scales[Math.Clamp(index, 0, scales.Count - 1)];
    }

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool TryReadParameterPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static Relation UnknownPoint() => Relation.Point(Carrier.Unknown, 0);

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier is null || carrier.IsUnknown ? new Carrier() : carrier;

    private static int CountOf<T>(IReadOnlyList<T> values) => values?.Count ?? 0;
}
