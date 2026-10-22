using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// An explicitly selected aggregate compatibility channel. One report is a
/// uniform mixture over the unique checks exposed by a source port. Its
/// likelihood is not the probability of observing every check jointly, nor an
/// acquired sample count. Native checks, calibration and deduplication remain
/// connected to the resulting unmeasured partition.
/// </summary>
public static class CompatibilityEvidence
{
    // calibration = frame -> (hit partition -> (miss partition -> unknown partition))
    // assessment = (source port -> model) -> (supported -> (contradicted -> unresolved))
    // normalized = supported -> (contradicted -> unresolved)
    // receipt = (assessment -> calibration) -> (normalized -> result partition)
    // q = (s*hit + c*miss + u/2)/(s+c+u). These host counts and exact
    // fractions are reads over native participation, not canonical evidence.

    public static Relation Calibrate(StatisticalEvidence.Frame frame, int hit, int denominator) =>
        new(frame.Surface, new Relation(frame.Weight(hit, denominator),
            new Relation(frame.Weight(denominator - hit, denominator), frame.Weight(1, 2))));

    public static Relation Assess(IElement sourcePort, IElement model, Relation supported,
        Relation contradicted, Relation unresolved) =>
        new(new Relation(sourcePort, model), new Relation(supported, new Relation(contradicted, unresolved)));

    public static bool TryReadLikelihood(Relation assessment, Relation calibration,
        out Relation partition, out Relation receipt)
    {
        var premise = new Relation(assessment, calibration);
        receipt = new(premise, StructuralConstraint.Open(premise));
        partition = StructuralConstraint.Open(receipt);
        if (calibration is not { Left: Relation frameSurface,
                Right: Relation { Left: Relation hitPartition,
                    Right: Relation { Left: Relation missPartition, Right: Relation unknownPartition } } } ||
            !StatisticalEvidence.Frame.TryRead(frameSurface, out var frame) ||
            !StatisticalEvidence.TryReadPartition(hitPartition, frame.Unit, out var hit) ||
            !StatisticalEvidence.TryReadPartition(missPartition, frame.Unit, out var miss) ||
            !StatisticalEvidence.TryReadPartition(unknownPartition, frame.Unit, out var unknown) ||
            hit + miss != StatisticalEvidence.Ratio.One || unknown != new StatisticalEvidence.Ratio(1, 2) ||
            assessment is not { Left: Relation { Right: var model },
                Right: Relation { Left: Relation supported,
                    Right: Relation { Left: Relation contradicted, Right: Relation unresolved } } } ||
            !frame.Models.Contains(model, Participants.Instance) ||
            !TryClosed(supported, out var yes) || !TryClosed(contradicted, out var no) ||
            !TryClosed(unresolved, out var open)) return false;

        // Classification is a read of native identity. A dual-dual wrapper is
        // the same placeholder participant; the opposed face is distinct.
        var states = new Dictionary<IElement, int>(Participants.Instance);
        Add(yes, 1); Add(no, 2); Add(open, 4);
        var selectedYes = states.Where(row => row.Value == 1).Select(row => row.Key).ToArray();
        var selectedNo = states.Where(row => row.Value == 2).Select(row => row.Key).ToArray();
        var selectedOpen = states.Where(row => row.Value is not (1 or 2)).Select(row => row.Key).ToArray();
        var normalized = new Relation(S(selectedYes), new Relation(S(selectedNo), S(selectedOpen)));
        var basis = new Relation(premise, normalized);
        if (states.Count == 0)
        {
            receipt = new(premise, new Relation(normalized, StructuralConstraint.Open(basis)));
            partition = StructuralConstraint.Open(receipt); return false;
        }

        var probability = (new StatisticalEvidence.Ratio(selectedYes.Length, 1) * hit +
            new StatisticalEvidence.Ratio(selectedNo.Length, 1) * miss +
            new StatisticalEvidence.Ratio(selectedOpen.Length, 1) * unknown) /
            new StatisticalEvidence.Ratio(states.Count, 1);
        if (probability.Denominator > 65536 || probability.Numerator > int.MaxValue)
        {
            // The exact read cannot be reified at this fixture's declared
            // partition capacity. The source still permits a later reader.
            receipt = new(premise, new Relation(normalized, StructuralConstraint.Open(basis)));
            partition = StructuralConstraint.Open(receipt); return false;
        }
        partition = frame.Weight((int)probability.Numerator, (int)probability.Denominator, basis);
        receipt = new(premise, new Relation(normalized, partition)); return true;

        void Add(IEnumerable<IElement> items, int classification)
        {
            foreach (var item in items) states[item] = states.GetValueOrDefault(item) | classification;
        }
    }

    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static bool TryClosed(Relation scope, out IElement[] items) =>
        StructuralConstraint.TryReadScope(scope, out items, out var start, out var end) && Same(start, end);
    private static bool Same(IElement left, IElement right) => ReferenceEquals(left, right) ||
        left is Placeholder a && right is Placeholder b && a.Equals(b);
    private sealed class Participants : IEqualityComparer<IElement>
    {
        internal static readonly Participants Instance = new();
        public bool Equals(IElement? left, IElement? right) =>
            left is null ? right is null : right is not null && Same(left, right);
        public int GetHashCode(IElement item) => item is Placeholder placeholder ? placeholder.GetHashCode()
            : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(item);
    }
}
