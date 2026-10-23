using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// Qualitative appearance intentions over selected native participants. An
/// ordinal comparison is supplied by an exposed observer; paint, RGB and
/// perceptual distance do not belong to this contract. Desired order, observed
/// support and a binary question's orientation remain separate structure.
/// </summary>
public static class OrnamentColourIntent
{
    // selection = source -> Closed(actual selected participants)
    // comparison = (subject -> reference) -> ordinal frame
    // intent = selection -> (comparison -> (desired support -> question))
    // question claim/contrary = comparison -> desired/complement support
    // judgement = intent -> (ordinal publication ->
    //              (supported -> (contradicted -> unresolved)))
    // Every check in those three scopes is the actual intent identity. Reusing
    // it does not buy another independent vote in the compatibility mixture.

    public sealed record IntentRead(IElement Source, Relation Selected, IElement Subject,
        IElement Reference, Relation Desired, Relation Question, Relation Surface);

    public sealed record Judgement(Relation Intent, Relation Observation, bool? Supported,
        Relation Surface, OrdinalReferenceFrame Frame);

    public sealed record AggregateRead(Relation Assessment, Relation Evidence,
        int Supported, int Contradicted, int Unresolved);

    /// <summary>Declares an intended order, never an observation that the order
    /// already holds. Subject/reference may themselves be exposed compound
    /// ports, for example two contrasts against one actual ground.</summary>
    public static Relation Declare(IElement source, Relation selected, IElement subject,
        IElement reference, OrdinalReferenceFrame frame, Relation desired,
        IElement? context = null)
    {
        var selection = new Relation(source, selected);
        var comparison = new Relation(new Relation(subject, reference), frame.Surface);
        var complement = Entries(desired, out var allowed) && InDomain(frame, allowed)
            ? S(Domain(frame).Where(item => !allowed.Any(a => Same(a, item))).ToArray())
            : StructuralConstraint.Open(desired);
        var claim = new Relation(comparison, desired);
        var contrary = new Relation(comparison, complement);
        var question = new StatisticalEvidence.Question(claim, contrary,
            new Relation(selection, context ?? StructuralConstraint.Open()));
        return new(selection, new Relation(comparison, new Relation(desired, question.Surface)));
    }

    /// <summary>Requires the exact declared selection and comparison. Coordinate
    /// similarity, a copied colour role or a foreign order cannot substitute for
    /// the actual exposed participant.</summary>
    public static bool TryRead(OrdinalReferenceFrame frame, Relation surface, out IntentRead read)
    {
        read = null!;
        if (surface is not { Left: Relation { Left: var source, Right: Relation selected } selection,
                Right: Relation { Left: Relation { Left: Relation pair, Right: var basis } comparison,
                    Right: Relation { Left: Relation desired, Right: Relation question } } } ||
            !Same(basis, frame.Surface) || !Entries(selected, out var participants) ||
            !participants.Any(item => Same(item, pair.Left)) ||
            !participants.Any(item => Same(item, pair.Right)) || Same(pair.Left, pair.Right) ||
            !Entries(desired, out var allowed) || allowed.Length == 0 || !InDomain(frame, allowed) ||
            question is not { Left: Relation { Left: Relation order, Right: Relation choices },
                Right: Relation { Left: var selectionContext } } || !Same(selectionContext, selection) ||
            order.Left is not Relation { Left: var claimComparison, Right: var claimDesired } ||
            !Same(claimComparison, comparison) || !Same(claimDesired, desired) ||
            order.Right is not Relation { Left: var contraryComparison, Right: Relation contrarySupport } ||
            !Same(contraryComparison, comparison) || !Entries(contrarySupport, out var contrary) ||
            !SetEqual(contrary, Domain(frame).Where(item => !allowed.Any(a => Same(a, item))).ToArray()) ||
            !Entries(choices, out var alternatives) || alternatives.Length != 2 ||
            !Same(alternatives[0], order.Left) || !Same(alternatives[1], order.Right)) return false;
        read = new(source, selected, pair.Left, pair.Right, desired, question, surface);
        return true;
    }

    /// <summary>Tests an exposed ordinal support against the desired support.
    /// A partially overlapping support remains unresolved; an absent or foreign
    /// observation never silently becomes a contrary answer.</summary>
    public static Judgement Assess(OrdinalReferenceFrame frame, Relation intent, Relation observation)
    {
        var state = Evaluate(frame, intent, observation);
        var result = new Relation(state == true ? S(intent) : S(),
            new Relation(state == false ? S(intent) : S(), state is null ? S(intent) : S()));
        return new(intent, observation, state, new(intent, new Relation(observation, result)), frame);
    }

    private static bool? Evaluate(OrdinalReferenceFrame frame, Relation intent, Relation observation)
    {
        bool? state = null;
        if (TryRead(frame, intent, out var wanted) &&
            frame.TryRead(observation, out var source, out var subject, out var reference,
                out _, out var support, out _) && Same(source, wanted.Source) &&
            Same(subject, wanted.Subject) && Same(reference, wanted.Reference) &&
            Entries(support, out var actual) && actual.Length > 0 && InDomain(frame, actual) &&
            Entries(wanted.Desired, out var allowed))
        {
            if (actual.All(a => allowed.Any(b => Same(a, b)))) state = true;
            else if (!actual.Any(a => allowed.Any(b => Same(a, b)))) state = false;
        }
        return state;
    }

    /// <summary>Publishes a resolved answer using the existing statistical
    /// observation grammar. Asking the contrary and denying it preserves the
    /// same positive claim while retaining the different orientation.</summary>
    public static Relation ObserveAnswer(OrdinalReferenceFrame frame, Judgement judgement,
        IElement eventId, bool askOpposed = false)
    {
        if (!TryRead(frame, judgement.Intent, out var read) ||
            !State(judgement, out var state) || state is null)
            return new(new Relation(eventId, judgement.Surface), StructuralConstraint.Open(judgement.Surface));
        var order = (Relation)((Relation)read.Question.Left).Left;
        var orientation = askOpposed ? order.Reverse() : order;
        var response = state.Value ? order : order.Reverse();
        return new(new Relation(eventId, read.Question), new Relation(orientation, response));
    }

    /// <summary>One selected aggregate compatibility channel. Ordinal cue checks
    /// are not treated as independent likelihoods; contradictory publications
    /// of one intent are normalized to unresolved by CompatibilityEvidence.</summary>
    public static AggregateRead Aggregate(IElement sourcePort, IElement model,
        IEnumerable<Judgement> judgements)
    {
        var rows = judgements.ToArray();
        var yes = new List<IElement>(); var no = new List<IElement>(); var open = new List<IElement>();
        foreach (var row in rows)
        {
            if (!State(row, out var state) || state is null) open.Add(row.Intent);
            else if (state.Value) yes.Add(row.Intent);
            else no.Add(row.Intent);
        }
        var assessment = CompatibilityEvidence.Assess(sourcePort, model, S(yes.ToArray()), S(no.ToArray()), S(open.ToArray()));
        // The supplied observations and their calibration/provenance stay
        // reachable even though the check identity is the declared intent.
        var evidence = new Relation(S(rows.Select(row => (IElement)row.Surface).ToArray()), assessment);
        var distinct = rows.Select(row => row.Intent).Distinct(Participants.Instance).ToArray();
        int Count(int category) => distinct.Count(item =>
        {
            var y = yes.Any(check => Same(check, item)); var n = no.Any(check => Same(check, item));
            var u = open.Any(check => Same(check, item));
            return category == 1 ? y && !n && !u : category == 2 ? n && !y && !u : u || y && n;
        });
        return new(assessment, evidence, Count(1), Count(2), Count(3));
    }

    public static bool TryLikelihood(AggregateRead aggregate, Relation calibration,
        out Relation partition, out Relation receipt)
    {
        var success = CompatibilityEvidence.TryReadLikelihood(aggregate.Assessment, calibration,
            out partition, out var channel);
        receipt = new(aggregate.Evidence, channel);
        return success;
    }

    private static bool State(Judgement judgement, out bool? state)
    {
        state = null;
        if (judgement.Surface is not { Left: var intent,
                Right: Relation { Left: var observation, Right: Relation { Left: Relation yes,
                    Right: Relation { Left: Relation no, Right: Relation open } } } } ||
            !Same(intent, judgement.Intent) || !Same(observation, judgement.Observation) ||
            !Entries(yes, out var y) || !Entries(no, out var n) || !Entries(open, out var u) ||
            y.Length + n.Length + u.Length != 1 ||
            !y.Concat(n).Concat(u).All(check => Same(check, intent))) return false;
        var recorded = y.Length == 1 ? true : n.Length == 1 ? false : (bool?)null;
        var actual = Evaluate(judgement.Frame, judgement.Intent, judgement.Observation);
        // Result scopes are derived evidence. Even a correctly shaped result
        // cannot override the desired order or the actual published support.
        if (recorded != actual) return false;
        state = actual;
        return true;
    }

    private static IElement[] Domain(OrdinalReferenceFrame frame) => [frame.Lower, frame.Equal, frame.Higher];
    private static bool InDomain(OrdinalReferenceFrame frame, IElement[] values) =>
        values.All(item => Domain(frame).Any(candidate => Same(item, candidate)));
    private static bool SetEqual(IElement[] a, IElement[] b) =>
        a.Distinct(Participants.Instance).Count() == b.Distinct(Participants.Instance).Count() &&
        a.All(item => b.Any(candidate => Same(item, candidate)));
    private static bool Entries(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var start, out var end) && Same(start, end);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder p && b is Placeholder q && p.Equals(q);
    private sealed class Participants : IEqualityComparer<IElement>
    {
        internal static readonly Participants Instance = new();
        public bool Equals(IElement? a, IElement? b) => a is null ? b is null : b is not null && Same(a, b);
        public int GetHashCode(IElement item) => item is Placeholder p ? p.GetHashCode() :
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(item);
    }
}
