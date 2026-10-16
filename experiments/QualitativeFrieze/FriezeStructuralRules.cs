using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// Experimental certificates for a selected sampled path: bounded generator
/// phase participation, positive horizontal/neutral vertical period displacement,
/// and absence of immediate retracing. This local rule is not a certificate
/// against all older or translated segment overlaps. Reach positivity is an
/// explicit design premise, not a discovered observation or a numeric length.
/// </summary>
public sealed class FriezeStructuralRules
{
    // positive-premise = reach -> (frame-origin -> distinct-positive-site)
    // complexity = Closed(component-surface -> phase) -> Closed(capacity-slots)
    // period-rule = (horizontal-axis -> forward-face) -> (vertical-axis -> Closed())
    // local-rule = opposites -> (exchange -> positive-premises)
    // Surface = (fixture -> (frame -> positive-premises)) ->
    //           (complexity -> (period-rule -> local-rule))
    // period = ((initial -> returned) -> Closed(root-prefix)) ->
    //          (Closed(selected-samples) -> displacement-read)
    // report = (Surface -> trace) -> (period -> (passed -> (violations -> unresolved)))
    // obligation-witness = selected-rule -> (subject -> proof)
    private readonly FriezeFixture fixture;
    public Relation PositiveReaches { get; }
    public Relation ComplexitySlots { get; }
    public Relation ComplexityRule { get; }
    public Relation PeriodRule { get; }
    public Relation LocalRetraceRule { get; }
    public Relation Surface { get; }

    public FriezeStructuralRules(FriezeFixture fixture, Relation complexitySlots)
    {
        this.fixture = fixture;
        ComplexitySlots = complexitySlots;
        PositiveReaches = S(fixture.Reaches.Select(reach => (IElement)new Relation(reach,
            new Relation(fixture.Frame.Origin, new Placeholder()))).ToArray());
        var phases = PhaseParticipation();
        ComplexityRule = new Relation(phases, complexitySlots);
        PeriodRule = new Relation(new Relation(fixture.Frame.Horizontal, fixture.Frame.Right),
            new Relation(fixture.Frame.Vertical, S()));
        LocalRetraceRule = new Relation(fixture.Frame.Opposites,
            new Relation(fixture.Frame.Exchange, PositiveReaches));
        Surface = new Relation(new Relation(fixture.Surface, new Relation(fixture.Frame.Surface, PositiveReaches)),
            new Relation(ComplexityRule, new Relation(PeriodRule, LocalRetraceRule)));
    }

    public Relation Evaluate(Relation trace)
    {
        var passed = new List<IElement>(); var violations = new List<IElement>(); var unresolved = new List<IElement>();
        var complexityProof = new Relation(ComplexityRule.Left, ComplexitySlots);
        if (!Closed(ComplexityRule.Left as Relation, out var parts) || !Closed(ComplexitySlots, out var slots) ||
            slots.Distinct(ReferenceEqualityComparer.Instance).Count() != slots.Length)
            unresolved.Add(Witness(ComplexityRule, fixture.Surface, complexityProof));
        else (parts.Length <= slots.Length ? passed : violations).Add(Witness(ComplexityRule, fixture.Surface, complexityProof));

        var period = new Relation(new Relation(new Relation(trace, trace), StructuralConstraint.Open()),
            new Relation(StructuralConstraint.Open(), fixture.Frame.Reduce(S(), StructuralConstraint.Open())));
        if (!SelectPeriod(trace, out var selected, out var displacement, out var selection, out var failure))
        {
            unresolved.Add(Witness(PeriodRule, trace, failure));
            unresolved.Add(Witness(LocalRetraceRule, trace, failure));
            return Report();
        }
        period = new Relation(selection, new Relation(selected, displacement));
        var periodDelta = ReadDelta(displacement, trace);
        var periodProof = new Relation(displacement, fixture.ReachOrder);
        if (!periodDelta.Complete)
            unresolved.Add(Witness(PeriodRule, selected, periodProof));
        else if (periodDelta.X is -1 or 0 || periodDelta.Y is -1 or 1)
            violations.Add(Witness(PeriodRule, selected, periodProof));
        else if (periodDelta.X == 1 && periodDelta.Y == 0)
            passed.Add(Witness(PeriodRule, selected, periodProof));
        else unresolved.Add(Witness(PeriodRule, selected, periodProof));

        var nonzero = new List<Delta>();
        foreach (var sample in E(selected).Cast<Relation>())
        {
            if (!FriezeFixture.TryReadSampleDelta(sample, out var delta) ||
                !ValidDelta(sample, delta))
            { unresolved.Add(Witness(LocalRetraceRule, sample, sample)); continue; }
            var read = ReadDelta(delta, sample);
            if (!read.Complete)
            { unresolved.Add(Witness(LocalRetraceRule, sample, delta)); nonzero.Add(read); }
            else if (read.X != 0 || read.Y != 0) nonzero.Add(read);
        }
        if (nonzero.Count == 0)
            passed.Add(Witness(LocalRetraceRule, selected, S()));
        else
        {
            for (var i = 1; i < nonzero.Count; i++) CheckPair(nonzero[i - 1], nonzero[i]);
            CheckPair(nonzero[^1], nonzero[0]); // Complete control return establishes the period seam.
        }
        return Report();

        void CheckPair(Delta first, Delta second)
        {
            var pair = new Relation(first.Subject, second.Subject);
            var proof = new Relation(new Relation(first.Read, second.Read), fixture.ReachOrder);
            if (!first.Complete || !second.Complete)
            { unresolved.Add(Witness(LocalRetraceRule, pair, proof)); return; }
            // Equal formal vectors are the same displacement promise, even if
            // their net sign is unknown; they cannot point against themselves.
            if (SameVector(first.Moves, second.Moves) || DifferentRay(first.X, second.X) || DifferentRay(first.Y, second.Y))
            { passed.Add(Witness(LocalRetraceRule, pair, proof)); return; }
            var axisOpposed = first.X == 0 && second.X == 0 && Opposed(first.Y, second.Y) ||
                first.Y == 0 && second.Y == 0 && Opposed(first.X, second.X);
            var combined = fixture.Frame.Reduce(S(first.Moves), S(second.Moves));
            var exactOpposite = fixture.Frame.TryReadResult(combined, out var remaining, out _, out _) &&
                Closed(remaining, out var survivors) && survivors.Length == 0 &&
                (first.X is -1 or 1 || first.Y is -1 or 1);
            proof = new Relation(proof, combined);
            if (axisOpposed || exactOpposite) violations.Add(Witness(LocalRetraceRule, pair, proof));
            else unresolved.Add(Witness(LocalRetraceRule, pair,
                new Relation(proof, new Relation(first.Read, second.Read))));
        }

        Relation Report() => new(new Relation(Surface, trace), new Relation(period,
            new Relation(S(passed.ToArray()), new Relation(S(violations.ToArray()), S(unresolved.ToArray())))));
    }

    public bool TryReadResult(Relation read, out Relation passed, out Relation violations, out Relation unresolved)
    {
        passed = violations = unresolved = null!;
        if (read.Left is not Relation context || !ReferenceEquals(context.Left, Surface) ||
            read.Right is not Relation { Left: Relation, Right: Relation results } ||
            results.Left is not Relation yes || results.Right is not Relation { Left: Relation no, Right: Relation pending } ||
            !Closed(yes, out _) || !Closed(no, out _) || !Closed(pending, out _)) return false;
        passed = yes; violations = no; unresolved = pending; return true;
    }

    /// <summary>Reads the first complete control-return period and its sampled
    /// displacement. A complete event period can still have open/unknown motion.</summary>
    public bool TryReadPeriod(Relation read, out Relation samples, out Relation displacement)
    {
        samples = displacement = null!;
        if (!TryReadResult(read, out _, out _, out _) ||
            read.Right is not Relation { Left: Relation { Left: Relation selection, Right: Relation result } } ||
            selection.Left is not Relation endpoints || endpoints.Left is not Relation initial || endpoints.Right is not Relation returned ||
            selection.Right is not Relation prefix || !Closed(prefix, out var steps) || steps.Length == 0 ||
            !fixture.SamePhaseConfiguration(initial, returned) ||
            result.Left is not Relation chosen || !Closed(chosen, out var entries) || entries.Length == 0 ||
            result.Right is not Relation movement || !fixture.Frame.TryReadResult(movement, out _, out _, out _)) return false;
        samples = chosen; displacement = movement; return true;
    }

    private Relation PhaseParticipation()
    {
        var selected = new List<IElement>();
        var seen = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        foreach (var component in fixture.Components)
        {
            if (!seen.Add(component.Surface)) continue;
            if (!Closed(component.Domain, out var phases)) return StructuralConstraint.Open(selected.ToArray());
            selected.AddRange(phases.Select(phase => (IElement)new Relation(component.Surface, phase)));
        }
        return S(selected.ToArray());
    }

    private bool SelectPeriod(Relation trace, out Relation samples, out Relation displacement,
        out Relation selection, out IElement failure)
    {
        samples = StructuralConstraint.Open(); displacement = fixture.Frame.Reduce(S(), StructuralConstraint.Open());
        selection = new Relation(trace, trace); failure = trace;
        if (!fixture.TryReadTrace(trace, out var initial, out _, out var steps, out var applications, out var allSamples, out _)) return false;
        if (E(applications).Any(entry => entry is not Relation { Left: Relation { Left: Relation } })) return false;
        var allowed = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var prefix = new List<IElement>();
        var cursor = initial;
        foreach (var entry in E(steps))
        {
            if (entry is not Relation step || !fixture.Network.TryReadStep(step, out var before, out _, out var after,
                    out var accepted, out var blocked, out var pending) || !ReferenceEquals(before, cursor) ||
                E(blocked).Length != 0 || E(pending).Length != 0) return false;
            prefix.Add(step); allowed.Add(step);
            foreach (var receipt in E(accepted))
            {
                if (receipt is not Relation phaseRead || !fixture.Components.Any(component => component.TryReadAccepted(phaseRead, out _, out _, out _)))
                    return false;
                allowed.Add(receipt);
            }
            cursor = after;
            if (!fixture.SamePhaseConfiguration(initial, after)) continue;
            var expected = E(applications).OfType<Relation>().Where(app => app.Left is Relation source && allowed.Contains(source.Left)).ToList();
            var incoming = expected.Select(app => app.Right).ToArray();
            var chosen = new List<IElement>();
            Relation? previousPosition = null;
            foreach (var candidate in E(allSamples))
            {
                if (candidate is not Relation sample || !FriezeFixture.TryReadSample(sample, out var cause, out _,
                        out var previous, out var current, out var used, out _)) return false;
                if (!allowed.Contains(cause)) continue;
                if (previousPosition is null ? !Closed(previous, out var start) || start.Length != 0 : !ReferenceEquals(previousPosition, previous))
                    return false;
                foreach (var usedApplication in E(used))
                {
                    var index = expected.FindIndex(app => ReferenceEquals(app, usedApplication));
                    if (index < 0) { failure = new Relation(sample, applications); return false; }
                    expected.RemoveAt(index);
                }
                if (!FriezeFixture.TryReadSampleDelta(sample, out var delta) || !ValidDelta(sample, delta))
                { failure = sample; return false; }
                chosen.Add(sample); previousPosition = current;
            }
            if (expected.Count != 0 || chosen.Count == 0)
            { failure = new Relation(S(expected.Cast<IElement>().ToArray()), fixture.Sampling); return false; }
            samples = S(chosen.ToArray());
            displacement = fixture.Frame.Reduce(S(), S(incoming));
            selection = new Relation(new Relation(initial, after), S(prefix.ToArray()));
            return true;
        }
        failure = new Relation(steps, fixture.Network.Surface);
        return false;
    }

    private bool ValidDelta(Relation sample, Relation delta)
    {
        if (!fixture.Frame.TryReadResult(delta, out _, out _, out _) ||
            !FriezeFixture.TryReadSample(sample, out _, out _, out _, out _, out var applications, out _) ||
            delta.Left is not Relation { Right: Relation inputs } || inputs.Left is not Relation before ||
            inputs.Right is not Relation incoming || !Closed(before, out var prior) || prior.Length != 0 ||
            !Closed(incoming, out var moves)) return false;
        var expected = E(applications).Select(application => application is Relation app ? app.Right : application).ToArray();
        return expected.Length == moves.Length && expected.Zip(moves).All(pair => ReferenceEquals(pair.First, pair.Second));
    }

    private Delta ReadDelta(Relation read, IElement subject)
    {
        if (!fixture.Frame.TryReadResult(read, out var support, out _, out _) || !Closed(support, out var entries))
            return new(subject, read, [], null, null, false);
        var x = new List<bool>(); var y = new List<bool>();
        foreach (var entry in entries)
        {
            if (entry is not Relation move || !fixture.Frame.Direction(move, out var axis, out var forward, out var reach) ||
                !E(PositiveReaches).OfType<Relation>().Any(premise => Same(premise.Left, reach) &&
                    premise.Right is Relation ordered && !Same(ordered.Left, ordered.Right)))
                return new(subject, read, entries, null, null, false);
            (ReferenceEquals(axis, fixture.Frame.Horizontal) ? x : y).Add(forward);
        }
        return new(subject, read, entries, Sign(x), Sign(y), true);
    }

    private bool SameVector(IElement[] first, IElement[] second)
    {
        var pending = second.OfType<Relation>().ToList();
        if (pending.Count != second.Length || first.Length != second.Length) return false;
        foreach (var entry in first)
        {
            if (entry is not Relation move) return false;
            var index = pending.FindIndex(other => ReferenceEquals(move.Left, other.Left) && Same(move.Right, other.Right));
            if (index < 0) return false;
            pending.RemoveAt(index);
        }
        return true;
    }

    // These signs are calculated adapter reads only. Native directions,
    // extents, sampling and every unresolved comparison stay in the witness.
    private readonly record struct Delta(IElement Subject, Relation Read, IElement[] Moves, int? X, int? Y, bool Complete);
    private static int? Sign(List<bool> faces) => faces.Count == 0 ? 0 : faces.All(face => face) ? 1 : faces.All(face => !face) ? -1 : null;
    private static bool Opposed(int? a, int? b) => a is -1 or 1 && b is -1 or 1 && a == -b;
    private static bool DifferentRay(int? a, int? b) => a.HasValue && b.HasValue &&
        ((a == 0) != (b == 0) || a != 0 && b != 0 && a == b);
    private static Relation Witness(Relation rule, IElement subject, IElement proof) => new(rule, new Relation(subject, proof));
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation? scope, out IElement[] entries)
    {
        entries = [];
        return scope is not null && StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
}
