using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A captured categorical feasibility read for future search or
/// training. Material accounting, declared junction obligations and unresolved
/// local choices remain separate criteria. This neither ranks beauty nor
/// claims that a missing obligation was satisfied.</summary>
public sealed class AdaptiveWallpaperAssessment
{
    // feature = selected-source -> (requirement -> retained-evidence)
    // confirmed outcome = actual-feature -> actual-feature
    // failed outcome = actual-feature -> Open(actual-feature-witness)
    // A known failure has CLOSED outcome support. An unavailable assessment
    // has OPEN support over its alternatives; neither becomes a favorable zero.
    // junction check = actual required link -> actual current agreement, or
    //                 actual required link -> Open(missing/stale evidence)
    public sealed record Feature(string Name, Relation Surface, Relation Witness,
        Relation Criterion, Relation Observation);

    public AdaptiveWallpaperMachine Machine { get; }
    public Relation Snapshot { get; }
    public Relation TransportState { get; }
    public Relation CurrentSeams { get; }
    public Relation CurrentUnknown { get; }
    public Relation NodePending { get; }
    public Relation Account { get; }
    public Relation RequiredSeams { get; }
    public Relation SeamChecks { get; }
    public Relation AcceptedSeams { get; }
    public Relation BlockedSeams { get; }
    public Relation UnresolvedSeams { get; }
    public IReadOnlyList<Feature> Features { get; }
    public PatternAestheticProfile Profile { get; }
    public Relation Read { get; }
    public PatternAestheticProfile.Summary Summary { get; }
    public Relation Surface { get; }

    public AdaptiveWallpaperAssessment(AdaptiveWallpaperMachine machine)
    {
        Machine = machine;
        Snapshot = machine.Snapshot(); TransportState = machine.Transport.State;
        CurrentSeams = machine.CurrentSeams; CurrentUnknown = machine.CurrentUnknown;
        Account = machine.Transport.Account();
        var heads = machine.Nodes.Select(node => (Node: node, Revision: node.Current, Pending: node.Pending)).ToArray();
        NodePending = S(heads.Select(head => (IElement)new Relation(head.Node.Cell, head.Pending)).ToArray());
        RequiredSeams = S(machine.Links.Select(link => (IElement)link.Surface).ToArray());
        var checks = new List<IElement>(); var accepted = new List<IElement>();
        var blocked = new List<IElement>(); var unresolved = new List<IElement>();
        if (!Closed(CurrentSeams)) unresolved.Add(CurrentSeams);
        foreach (var link in machine.Links)
        {
            var candidates = E(CurrentSeams).OfType<Relation>().Where(read => read.Left is Relation { Left: Relation context } &&
                ReferenceEquals(context.Left, machine.Surface) && ReferenceEquals(context.Right, link.Surface)).ToArray();
            if (candidates is not [var seam])
            { Missing(link.Surface, S(candidates)); continue; }
            if (heads.Where(head => ReferenceEquals(head.Node, link.From)).ToArray() is not [var from] ||
                heads.Where(head => ReferenceEquals(head.Node, link.To)).ToArray() is not [var to] ||
                seam.Left is not Relation { Right: Relation owners } ||
                !ReferenceEquals(owners.Left, from.Revision.Surface) || !ReferenceEquals(owners.Right, to.Revision.Surface))
            { Missing(link.Surface, seam); continue; }
            var check = new Relation(link.Surface, seam); checks.Add(check);
            if (machine.TryReadAccepted(seam, out var passage) && ReferenceEquals(passage, link))
            { accepted.Add(seam); continue; }
            if (!from.Revision.TryReadPort(link.FromRole, out var firstPublication, out var firstBinding) ||
                !to.Revision.TryReadPort(link.ToRole, out var secondPublication, out var secondBinding) ||
                seam.Right is not Relation { Left: Relation publications, Right: Relation { Left: Relation bindings, Right: Relation equality } } ||
                !ReferenceEquals(publications.Left, firstPublication) || !ReferenceEquals(publications.Right, secondPublication) ||
                !ReferenceEquals(bindings.Left, firstBinding) || !ReferenceEquals(bindings.Right, secondBinding) ||
                equality.Left is not Relation { Left: var frame, Right: Relation operands } || !ReferenceEquals(frame, machine.Grammar.Frame.Surface) ||
                !ReferenceEquals(operands.Left, firstBinding.Right) || operands.Right is not Relation inverse ||
                secondBinding.Right is not Relation secondWord || !IsInverse(machine.Grammar.Frame, inverse, secondWord) ||
                !machine.Grammar.Frame.TryReadResult(equality, out var remaining, out _, out var pending) ||
                !Closed(remaining) || !Closed(pending) || E(pending).Length != 0 || E(remaining).Length == 0)
            { unresolved.Add(check); continue; }
            blocked.Add(seam);
        }
        SeamChecks = S(checks.ToArray()); AcceptedSeams = S(accepted.ToArray());
        BlockedSeams = S(blocked.ToArray()); UnresolvedSeams = S(unresolved.ToArray());
        var ownershipEvidence = new Relation(new Relation(machine.Transport.Surface, TransportState), new Relation(Account, NodePending));
        var ownershipSurface = new Relation(Snapshot, new Relation(machine.DefinitionProblems, ownershipEvidence));
        var seamEvidence = new Relation(CurrentSeams, new Relation(SeamChecks,
            new Relation(AcceptedSeams, new Relation(BlockedSeams, UnresolvedSeams))));
        var seamSurface = new Relation(Snapshot, new Relation(RequiredSeams, seamEvidence));
        var choicesSurface = new Relation(Snapshot, new Relation(machine.Observations, CurrentUnknown));
        bool? seamsPassed = E(BlockedSeams).Length != 0 ? false :
            E(RequiredSeams).Length == 0 || E(UnresolvedSeams).Length != 0 ? null : true;
        var features = new[]
        {
            Observe("Accounted, settled execution boundary", ownershipSurface, ownershipEvidence, OwnershipPassed()),
            Observe("Declared junction obligations", seamSurface, seamEvidence, seamsPassed),
            Observe("Resolved local choices", choicesSurface, new Relation(machine.Observations, CurrentUnknown),
                Closed(CurrentUnknown) ? E(CurrentUnknown).Length == 0 : null)
        };
        Features = Array.AsReadOnly(features);
        Profile = new(S(features.Select(feature => (IElement)feature.Criterion).ToArray()));
        Read = Profile.Evaluate(S(features.Select(feature => (IElement)feature.Observation).ToArray()));
        Summary = Profile.Describe(Read);
        Surface = new(new Relation(machine.Surface, new Relation(Snapshot,
                new Relation(TransportState, new Relation(CurrentSeams, new Relation(Account, new Relation(NodePending, CurrentUnknown)))))),
            new Relation(Profile.Surface, Read));

        void Missing(IElement link, IElement evidence)
        {
            var check = new Relation(link, StructuralConstraint.Open(evidence));
            checks.Add(check); unresolved.Add(check);
        }

        bool? OwnershipPassed()
        {
            if (!machine.IsValid) return false;
            if (heads.Any(head => !Closed(head.Pending)) ||
                !machine.Transport.TryReadState(TransportState, out _, out var placements, out var imported, out _) ||
                Account.Left is not Relation accountSource || !ReferenceEquals(accountSource.Left, machine.Transport.Surface) ||
                !ReferenceEquals(accountSource.Right, imported) || Account.Right is not Relation { Left: var held, Right: Relation pending } ||
                !ReferenceEquals(held, placements) || !Closed(pending)) return null;
            return E(pending).Length == 0 && heads.All(head => E(head.Pending).Length == 0);
        }
    }

    private static Feature Observe(string name, Relation surface, Relation witness, bool? passed)
    {
        var confirmed = new Relation(surface, surface);
        var failed = new Relation(surface, StructuralConstraint.Open(witness));
        var criterion = PatternAestheticProfile.Criterion(surface, S(failed, confirmed), S(confirmed), S());
        var support = passed is null ? StructuralConstraint.Open(failed, confirmed) : S(passed.Value ? confirmed : failed);
        return new(name, surface, witness, criterion, PatternAestheticProfile.Observation(criterion, witness, support));
    }

    private static bool IsInverse(QualitativeMotionFrame frame, Relation inverse, Relation word)
    {
        if (!Closed(inverse) || !Closed(word) || E(inverse).Length != E(word).Length) return false;
        return E(inverse).Zip(E(word).Reverse()).All(pair => pair.First is Relation a && pair.Second is Relation b &&
            ReferenceEquals(a.Right, b.Right) && E(frame.Opposites).OfType<Relation>().Any(opposite =>
                ReferenceEquals(opposite.Left, b.Left) && ReferenceEquals(opposite.Right, a.Left)));
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
