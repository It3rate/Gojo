using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Conservative process features at a selected emitted-word resolution.
/// Equal words share a read class, never sample, application or phase identity.
/// These features describe structure and do not establish aesthetic utility.</summary>
public sealed class FriezeWordFeatures
{
    // Surface = (fixture.Surface -> trace) -> explicit-complexity-slots
    // read = Surface -> ((drive-read -> rule-read) ->
    //                    ((reuse-groups -> transitions) ->
    //                     (continuations -> ((violations -> legality-unresolved) -> unresolved))))
    // word = actual-sample -> actual-ordered-applications
    // group = representative-word -> Closed(word -> Closed(actual-move -> actual-move))
    // transition = (actual-before-word -> actual-after-word) -> (before-group -> after-group)
    // continuation = actual-group -> Closed(actual-transitions-from-that-group)
    // The current word alone is the prediction context. Multiple successors
    // mean insufficient context in this read, not random source phase dynamics.
    public FriezeFixture Fixture { get; }
    public Relation Trace { get; }
    public Relation Surface { get; }
    public Relation DriveRead { get; }
    public Relation RuleRead { get; }
    public Relation Groups { get; }
    public Relation Transitions { get; }
    public Relation Continuations { get; }
    public Relation Violations { get; }
    public Relation LegalityUnresolved { get; }
    public Relation Unresolved { get; }
    private readonly Relation read;
    public sealed record Summary(int Samples, int DistinctWords, int ChangedWordTransitions,
        int CurrentWordContextsWithAlternatives, int Unresolved, int Violations = 0, int LegalityUnresolved = 0);

    public FriezeWordFeatures(FriezeFixture fixture, Relation trace, Relation complexitySlots)
    {
        Fixture = fixture; Trace = trace;
        Surface = new Relation(new Relation(fixture.Surface, trace), complexitySlots);
        DriveRead = FriezeDriveAnalysis.Evaluate(fixture, trace);
        var rules = new FriezeStructuralRules(fixture, complexitySlots); RuleRead = rules.Evaluate(trace);
        var pending = new List<IElement>(); var words = new List<Relation>();
        Relation driveSamples = S();
        if (!FriezeDriveAnalysis.TryReadResult(fixture, DriveRead, out driveSamples, out _))
        { pending.Add(DriveRead); driveSamples = S(); }
        if (!rules.TryReadResult(RuleRead, out _, out var violations, out var rulePending))
        { Violations = S(); LegalityUnresolved = S(RuleRead); }
        else { Violations = violations; LegalityUnresolved = rulePending; }
        if (!rules.TryReadPeriod(RuleRead, out var samples, out _)) pending.Add(RuleRead);
        else
            foreach (var item in E(samples))
                if (item is Relation sample && FriezeFixture.TryReadSample(sample, out _, out _, out _, out _, out var apps, out _) &&
                    E(driveSamples).OfType<Relation>().Any(clause => clause.Left is Relation { Left: Relation owner } &&
                        ReferenceEquals(owner.Left, trace) && ReferenceEquals(owner.Right, sample)) &&
                    Closed(apps) && E(apps).All(app => app is Relation { Right: Relation move } && fixture.Frame.Direction(move, out _, out _, out _)))
                    words.Add(new Relation(sample, apps));
                else pending.Add(item);

        var representatives = new List<Relation>(); var members = new List<List<Relation>>();
        var groupIndex = new List<int>();
        foreach (var word in words)
        {
            var found = representatives.FindIndex(other => Match(other, word, out _));
            if (found < 0) { found = representatives.Count; representatives.Add(word); members.Add([]); }
            Match(representatives[found], word, out var correspondence);
            members[found].Add(new Relation(word, correspondence)); groupIndex.Add(found);
        }
        var groups = representatives.Select((word, index) => new Relation(word, S(members[index].Cast<IElement>().ToArray()))).ToArray();
        var transitions = new List<IElement>();
        // Cyclic closure needs an actual completed period and readable causal
        // words. Unknown net displacement remains in DriveRead/RuleRead; it
        // does not make a fully known emitted-word sequence unknowable.
        if (words.Count != 0 && pending.Count == 0)
            for (var i = 0; i < words.Count; i++)
            {
                var next = (i + 1) % words.Count;
                transitions.Add(new Relation(new Relation(words[i], words[next]), new Relation(groups[groupIndex[i]], groups[groupIndex[next]])));
            }
        Groups = S(groups); Transitions = S(transitions.ToArray());
        Continuations = S(groups.Select(group => (IElement)new Relation(group, S(transitions.OfType<Relation>()
            .Where(edge => edge.Right is Relation pair && ReferenceEquals(pair.Left, group)).Cast<IElement>().ToArray()))).ToArray());
        Unresolved = S(pending.ToArray());
        read = new Relation(Surface, new Relation(new Relation(DriveRead, RuleRead),
            new Relation(new Relation(Groups, Transitions), new Relation(Continuations,
                new Relation(new Relation(Violations, LegalityUnresolved), Unresolved)))));
    }

    public Relation Evaluate() => read;
    public bool TryReadResult(Relation candidate, out Relation groups, out Relation transitions,
        out Relation continuations, out Relation unresolved)
    {
        groups = transitions = continuations = unresolved = null!;
        if (!ReferenceEquals(candidate.Left, Surface) || candidate.Right is not Relation { Left: Relation evidence, Right: Relation result } ||
            !ReferenceEquals(evidence.Left, DriveRead) || !ReferenceEquals(evidence.Right, RuleRead) ||
            result.Left is not Relation reuse || !ReferenceEquals(reuse.Left, Groups) || !ReferenceEquals(reuse.Right, Transitions) ||
            result.Right is not Relation prediction || !ReferenceEquals(prediction.Left, Continuations) ||
            prediction.Right is not Relation status || status.Left is not Relation legality ||
            !ReferenceEquals(legality.Left, Violations) || !ReferenceEquals(legality.Right, LegalityUnresolved) ||
            !ReferenceEquals(status.Right, Unresolved)) return false;
        groups = Groups; transitions = Transitions; continuations = Continuations; unresolved = Unresolved; return true;
    }
    public bool TryReadGroup(Relation group, out Relation representative, out Relation members)
    {
        representative = members = null!;
        if (!E(Groups).Any(value => ReferenceEquals(value, group)) || group.Left is not Relation word || group.Right is not Relation scope) return false;
        representative = word; members = scope; return true;
    }
    public Summary Describe()
    {
        var samples = E(Groups).Cast<Relation>().Sum(group => E((Relation)group.Right).Length);
        var changed = E(Transitions).Cast<Relation>().Count(edge => edge.Right is Relation pair && !ReferenceEquals(pair.Left, pair.Right));
        var ambiguous = E(Continuations).Cast<Relation>().Count(row => E((Relation)row.Right).Cast<Relation>()
            .Select(edge => ((Relation)edge.Right).Right).Distinct(ReferenceEqualityComparer.Instance).Skip(1).Any());
        return new(samples, E(Groups).Length, changed, ambiguous, E(Unresolved).Length, E(Violations).Length, E(LegalityUnresolved).Length);
    }

    private bool Match(Relation first, Relation second, out Relation correspondence)
    {
        correspondence = S();
        var a = E((Relation)first.Right); var b = E((Relation)second.Right);
        if (a.Length != b.Length) return false;
        var matches = new List<IElement>();
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] is not Relation { Right: Relation x } || b[i] is not Relation { Right: Relation y } ||
                !Fixture.Frame.Direction(x, out var ax, out var fx, out var rx) ||
                !Fixture.Frame.Direction(y, out var ay, out var fy, out var ry) ||
                !ReferenceEquals(ax, ay) || fx != fy || !Same(rx, ry)) return false;
            matches.Add(new Relation(x, y));
        }
        correspondence = S(matches.ToArray()); return true;
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation p && b is CarrierLocation q && p.Carrier.SameIdentityAs(q.Carrier) && p.SameTickRefAs(q);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
