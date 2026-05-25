using Gojo.Core;

namespace Gojo.Equations;

/// <summary>
/// Helpers for one input feeding several equation surfaces. A fanout is an
/// ordered Gojo group of branch equations; a read produces tuple-shaped output
/// so later operation surfaces can merge the branch results.
/// </summary>
public static class EquationFanout
{
    /// <summary>Creates an ordered group of branch equation surfaces.</summary>
    public static Relation Create(Carrier root, params Relation[] branches)
    {
        var holder = RootOrNew(root);
        var count = CountOf(branches);
        var elements = new IElement[count];

        for (var index = 0; index < count; index++)
        {
            elements[index] = branches[index] ?? Relation.Point(Carrier.Unknown, 0);
        }

        return new Relation(Relation.Point(holder, 0), Relation.OrderedGroup(holder, elements));
    }

    /// <summary>Reads the ordered branch equation surfaces in a fanout.</summary>
    public static bool TryReadBranches(Relation fanout, out Relation[] branches)
    {
        if (TryReadFanoutRoot(fanout, out var holder) &&
            fanout.Right is Relation branchGroup &&
            branchGroup.TryReadOrderedElementsOn(holder, out var elements))
        {
            branches = [.. elements.OfType<Relation>()];
            return true;
        }

        branches = [];
        return false;
    }

    /// <summary>Reads one scalar input sample through all branches.</summary>
    public static Relation Predict(
        Relation fanout,
        CarrierLocation input,
        out Relation[] branchReads) =>
        Predict(fanout, (IElement)input, out branchReads);

    /// <summary>Reads one scalar or relation-shaped input sample through all branches.</summary>
    public static Relation Predict(
        Relation fanout,
        IElement input,
        out Relation[] branchReads)
    {
        if (!TryReadBranches(fanout, out var branches) || branches.Length == 0)
        {
            branchReads = [];
            return new Relation(Relation.Point(Carrier.Unknown, 0), input ?? Relation.Point(Carrier.Unknown, 0));
        }

        var supplied = input ?? Relation.Point(Carrier.Unknown, 0);
        var outputs = new IElement[branches.Length];
        var residues = new IElement[branches.Length];
        branchReads = new Relation[branches.Length];

        for (var index = 0; index < branches.Length; index++)
        {
            var branch = branches[index];
            var target = TryReadOutputCarrier(branch, out var outputCarrier)
                ? outputCarrier
                : Carrier.Unknown;
            var read = Equation.Predict(branch, supplied, target);
            branchReads[index] = read;

            if (Equation.TryReadPrediction(read, out var predicted, out var residue))
            {
                outputs[index] = predicted;
                residues[index] = residue;
            }
            else
            {
                outputs[index] = read;
                residues[index] = read;
            }
        }

        return new Relation(TupleOf(outputs), Relation.OrderedGroup(new Carrier(), residues));
    }

    /// <summary>Reads a fanout prediction into tuple output and residue elements.</summary>
    public static bool TryReadPrediction(
        Relation prediction,
        out IElement[] outputs,
        out IElement[] residues)
    {
        if (prediction is not null &&
            TryReadTuple(prediction.Left, out outputs) &&
            TryReadOrderedGroup(prediction.Right, out residues))
        {
            return true;
        }

        outputs = [];
        residues = [];
        return false;
    }

    private static IElement TupleOf(IReadOnlyList<IElement> elements)
    {
        if (CountOf(elements) == 0)
        {
            return Relation.Point(Carrier.Unknown, 0);
        }

        var tuple = elements[0] ?? Relation.Point(Carrier.Unknown, 0);
        for (var index = 1; index < elements.Count; index++)
        {
            tuple = new Relation(tuple, elements[index] ?? Relation.Point(Carrier.Unknown, 0));
        }

        return tuple;
    }

    private static bool TryReadTuple(IElement element, out IElement[] elements)
    {
        var collected = new List<IElement>();
        CollectTuple(element, collected);
        elements = [.. collected];
        return elements.Length != 0;
    }

    private static void CollectTuple(IElement element, List<IElement> elements)
    {
        if (element is Relation relation)
        {
            CollectTuple(relation.Left, elements);
            CollectTuple(relation.Right, elements);
            return;
        }

        elements.Add(element);
    }

    private static bool TryReadOrderedGroup(IElement element, out IElement[] elements)
    {
        if (element is Relation relation &&
            relation.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static bool TryReadFanoutRoot(Relation fanout, out Carrier holder)
    {
        if (fanout?.Left is Relation rootPoint &&
            rootPoint.Left is CarrierLocation origin &&
            rootPoint.Right is Carrier rootCarrier &&
            origin.Carrier.SameIdentityAs(rootCarrier) &&
            origin.Ticks == 0)
        {
            holder = rootCarrier;
            return true;
        }

        holder = null!;
        return false;
    }

    private static bool TryReadOutputCarrier(Relation equation, out Carrier carrier)
    {
        if (EquationSequence.TryReadSteps(equation, out var steps) && steps.Length != 0)
        {
            return TryReadOutputCarrier(steps[^1], out carrier);
        }

        foreach (var candidate in CarriersOf(equation?.Right))
        {
            if (!candidate.IsUnknown)
            {
                carrier = candidate;
                return true;
            }
        }

        carrier = null!;
        return false;
    }

    private static IEnumerable<Carrier> CarriersOf(IElement? element) => element switch
    {
        Carrier carrier => [carrier],
        CarrierLocation location => [location.Carrier],
        Relation relation => relation.Carriers(),
        _ => []
    };

    private static int CountOf<T>(IReadOnlyList<T> values) =>
        values?.Count ?? 0;

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
