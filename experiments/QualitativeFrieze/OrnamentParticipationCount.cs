using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An exact finite participation question. Cardinality is a bijection
/// between fresh expected roles and distinct offered pattern matches; neither
/// labels nor stored numeric counts determine its answer.</summary>
public static class OrnamentParticipationCount
{
    // question = (pattern -> Closed(selected variables)) -> Closed(expected roles)
    // witness = question -> (actual support -> (native pattern read ->
    //           (Closed(role -> offered participant) ->
    //            (Closed(unmatched roles) -> Closed(unmatched participants)))))

    public sealed class Question
    {
        public Relation Surface { get; }
        public IElement Pattern { get; } = null!;
        public Relation Variables { get; } = S();
        public Relation ExpectedRoles { get; } = S();
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => Empty(Blocked) && Empty(Open);

        public Question(IElement pattern, Relation variables, Relation expectedRoles)
            : this(new(new Relation(pattern, variables), expectedRoles)) { }

        public Question(Relation surface)
        {
            Surface = surface; var blocked = new List<IElement>(); var open = new List<IElement>();
            if (surface is not { Left: Relation { Right: Relation variables } definition, Right: Relation expected }) blocked.Add(surface);
            else
            {
                Pattern = definition.Left; Variables = variables; ExpectedRoles = expected;
                Scope(variables, blocked, open); Scope(expected, blocked, open);
                var slots = E(variables); var roles = E(expected); var participants = OrnamentReconstruction.Participants(Pattern);
                if (slots.Concat(roles).Any(item => item is not Placeholder { IsInverse: false }) ||
                    !FieldNative.Distinct(slots) || !FieldNative.Distinct(roles) ||
                    roles.Any(role => participants.Any(actual => FieldNative.Same(actual, role)) ||
                        slots.Any(slot => FieldNative.Same(slot, role)))) blocked.Add(surface);
            }
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }
    }

    public sealed class Receipt
    {
        public Question Question { get; }
        public Relation Support { get; }
        public Relation Match { get; }
        public Relation DistinctParticipants { get; }
        public Relation Correspondence { get; }
        public Relation UnmatchedExpected { get; }
        public Relation UnmatchedActual { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        public string Answer => !Empty(Blocked) ? "Blocked" : !Empty(Open) ? "Open" : !Empty(Refuted) ? "No" : "Yes";

        public Receipt(Question question, Relation support)
        {
            Question = question; Support = support;
            var blocked = E(question.Blocked).ToList(); var open = E(question.Open).ToList();
            Scope(support, blocked, open);
            Match = StructuralConstraint.Match(question.Pattern ?? question.Surface, question.Variables, support);
            var participants = new List<IElement>();
            if (!StructuralConstraint.TryRead(Match, out _, out var candidates, out _, out var unresolved)) open.Add(Match);
            else
            {
                open.AddRange(E(unresolved));
                foreach (var item in E(candidates))
                    if (item is not Relation candidate || !StructuralConstraint.TryReadCandidate(candidate, out var offered, out _, out _)) open.Add(item);
                    else if (!participants.Any(actual => FieldNative.Same(actual, offered))) participants.Add(offered);
            }
            DistinctParticipants = S(participants.ToArray());
            var roles = E(question.ExpectedRoles); var common = Math.Min(roles.Length, participants.Count);
            Correspondence = S(roles.Take(common).Select((role, index) => (IElement)new Relation(role, participants[index])).ToArray());
            UnmatchedExpected = S(roles.Skip(common).ToArray()); UnmatchedActual = S(participants.Skip(common).ToArray());
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
            var basis = new Relation(question.Surface, new Relation(support, new Relation(Match,
                new Relation(Correspondence, new Relation(UnmatchedExpected, UnmatchedActual)))));
            Supported = Empty(Blocked) && Empty(Open) && Empty(UnmatchedExpected) && Empty(UnmatchedActual) ? S(basis) : S();
            Refuted = Empty(Blocked) && Empty(Open) && (!Empty(UnmatchedExpected) || !Empty(UnmatchedActual)) ? S(basis) : S();
            Surface = new(basis, new Relation(Supported, new Relation(Refuted, new Relation(Open, Blocked))));
        }
    }

    /// <summary>Count exposed mechanical group rows, retaining their internal
    /// paths and closure information outside this particular promise.</summary>
    public static Question AllGroups(Relation referenceMechanicalGroups, out Relation sourceWitness) =>
        FromReference(referenceMechanicalGroups, closedOnly: false, out sourceWitness);

    /// <summary>Count rows publishing exactly one closed end-to-start site
    /// promise. The common closure seam is selected, so the question contains
    /// no identity borrowed from the reference's concrete closure scope.</summary>
    public static Question ClosedGroups(Relation referenceMechanicalGroups, out Relation sourceWitness) =>
        FromReference(referenceMechanicalGroups, closedOnly: true, out sourceWitness);

    private static Question FromReference(Relation reference, bool closedOnly, out Relation sourceWitness)
    {
        var id = new Placeholder(); var segments = new Placeholder(); var closure = new Placeholder();
        IElement pattern; Relation variables;
        if (!closedOnly)
        {
            pattern = new Relation(id, new Relation(segments, closure)); variables = S(id, segments, closure);
        }
        else
        {
            var site = new Placeholder(); var seam = new Placeholder();
            pattern = new Relation(id, new Relation(segments,
                new Relation(seam, new Relation(new Relation(site, site), seam))));
            variables = S(id, segments, site, seam);
        }
        var read = StructuralConstraint.Match(pattern, variables, reference); var participants = new List<IElement>();
        var complete = StructuralConstraint.TryRead(read, out _, out var candidates, out _, out var unresolved) && Empty(unresolved);
        if (complete || StructuralConstraint.TryRead(read, out _, out candidates, out _, out _))
            foreach (var item in E(candidates))
                if (item is Relation candidate && StructuralConstraint.TryReadCandidate(candidate, out var offered, out _, out _) &&
                    !participants.Any(actual => FieldNative.Same(actual, offered))) participants.Add(offered);
        var roles = participants.Select(_ => (IElement)new Placeholder()).ToArray();
        var question = new Question(pattern, variables, complete ? S(roles) : StructuralConstraint.Open(roles));
        var pairing = S(roles.Select((role, index) => (IElement)new Relation(role, participants[index])).ToArray());
        sourceWitness = new(reference, new Relation(question.Surface, new Relation(read, pairing)));
        return question;
    }

    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Empty(Relation scope) => E(scope).Length == 0;
    private static void Scope(Relation scope, List<IElement> blocked, List<IElement> open)
    {
        if (!StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)) blocked.Add(scope);
        else if (!FieldNative.Same(seam, tail)) open.Add(scope);
    }
}
