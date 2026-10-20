using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Explicitly captures a certified occurrence and asks later questions
/// in that retained context. The unrestricted read is a comparison, never a
/// substitute for a failed or unfinished retained read.</summary>
public sealed class RetainedMatchStudy
{
    // episode = actual execution -> (initial question/read ->
    //           (explicit witness capture -> (follow-up question ->
    //           (unrestricted comparison -> (retained read -> original-read recheck)))))
    public sealed record Episode(RetainedMatchCases.Case Case, C.Assessment Initial,
        RetainedMatch Memory, C.Assessment Fresh, C.Assessment Followup,
        C.Assessment Recheck, Relation SelectedSegments, Relation Surface);
    public sealed record Control(string Name, string Purpose, C.Assessment Read);

    public RetainedMatchCases Cases { get; }
    public IReadOnlyList<Episode> Episodes { get; }
    public IReadOnlyList<Control> Controls { get; }
    public Relation Surface { get; }

    private RetainedMatchStudy(RetainedMatchCases cases, IReadOnlyList<Episode> episodes,
        IReadOnlyList<Control> controls)
    {
        Cases = cases; Episodes = episodes; Controls = controls;
        Surface = new(cases.Surface, new Relation(S(episodes.Select(episode => (IElement)episode.Surface).ToArray()),
            S(controls.Select(control => (IElement)control.Read.Surface).ToArray())));
    }

    public static RetainedMatchStudy Build()
    {
        var cases = RetainedMatchCases.Build(); var episodes = new List<Episode>();
        foreach (var example in cases.Cases)
        {
            var initial = new C.Assessment(example.Source.Induction, example.InitialQuestion);
            // The fixture explicitly retains this actual certified witness.
            // The reader's first successful witness is not a universal rule for
            // choosing which interpretation an application should remember.
            var witness = initial.Witnesses.Single();
            var memory = RetainedMatch.Capture(initial, witness, example.RetainedRoles);
            if (!memory.IsValid) throw new InvalidOperationException("The selected fixture witness must be retainable.");
            var fresh = new C.Assessment(example.Source.Induction, example.FollowupQuestion);
            var followup = new C.Assessment(example.Source.Induction, example.FollowupQuestion, retained: memory);
            var recheck = new C.Assessment(example.Source.Induction, example.InitialQuestion, retained: memory);
            var selected = example.InitialQuestion.Roles.Select((role, index) => (role, index))
                .Where(pair => E(example.RetainedRoles).Any(slot => ReferenceEquals(slot, pair.role.Slot)))
                .SelectMany(pair => E(witness.Matches[pair.index].Occurrence.Segments))
                .Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
            var surface = new Relation(example.Source.Surface, new Relation(initial.Surface,
                new Relation(memory.Surface, new Relation(example.FollowupQuestion.Surface,
                    new Relation(fresh.Surface, new Relation(followup.Surface, recheck.Surface))))));
            episodes.Add(new(example, initial, memory, fresh, followup, recheck, S(selected), surface));
        }
        var first = episodes[0]; var sample = first.Case.Source;
        var otherEvent = RetainedMatchCases.ContactScene(false);
        var again = new QualitativeMotifInduction(sample.Observation, maxCycleEdges: 4,
            proposalCapacity: 4096, maxWalkEdges: 3, retainedCapacity: 1024);
        var incomplete = RetainedMatch.Capture(first.Initial, first.Initial.Witnesses.Single(),
            StructuralConstraint.Open(E(first.Case.RetainedRoles)));
        var unretainedRole = first.Case.InitialQuestion.Roles.First(role =>
            !E(first.Case.RetainedRoles).Any(slot => ReferenceEquals(slot, role.Slot)));
        var missingRole = new C.Question("Read a contour without admitting the retained path role.", [unretainedRole]);
        Control[] controls = [
            new("Rebuild the reader, retain the observation", "A new induction wrapper over the same observation can use the same retained actual sites and edges.",
                new(again, first.Case.FollowupQuestion, retained: first.Memory)),
            new("A new observation event", "A lookalike event does not establish identity with the retained observation. The reference stays unsettled.",
                new(otherEvent.Induction, first.Case.FollowupQuestion, retained: first.Memory)),
            new("Unfinished capture", "An open selection is not a complete retained context and does not authorize replacement by another matching path.",
                new(sample.Induction, first.Case.FollowupQuestion, retained: incomplete)),
            new("No follow-up search capacity", "Remembering a match does not manufacture an answer when the required follow-up search is unfinished.",
                new(sample.Induction, first.Case.FollowupQuestion, assignmentCapacity: 0, retained: first.Memory)),
            new("Retained role absent from the question", "The supplied context and question must agree about the retained role slots. An unadmitted slot is blocked.",
                new(sample.Induction, missingRole, retained: first.Memory))
        ];
        return new(cases, episodes, controls);
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
