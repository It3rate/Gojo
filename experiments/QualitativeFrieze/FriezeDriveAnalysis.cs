using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An observational decomposition of actual causal applications at
/// their selected sampling boundary. It assigns no shape class or new motion.
/// Axis reads, delta cancellation and accumulated position remain distinct.</summary>
public static class FriezeDriveAnalysis
{
    // analysis = (fixture-surface -> trace) -> (Closed(sample-reads) -> unresolved)
    // sample-read = ((trace -> actual-sample) -> actual-sampling-route) ->
    //               ((actual-applications -> Closed(axis-contributions)) ->
    //                ((actual-delta-read -> actual-position-read) -> unresolved))
    // axis-contribution = (actual-axis -> Closed(actual-applications)) ->
    //                     displacement-read
    // Each axis read reduces only the attributable actual moves. Unattributed
    // motion leaves an open incoming tail on both axis reads, without assigning
    // that motion or claiming a concrete zero on either axis.
    public static Relation Evaluate(FriezeFixture fixture, Relation trace)
    {
        var samples = new List<IElement>(); var pending = new List<IElement>();
        if (!ReadContext(fixture, trace, out var context)) pending.Add(trace);
        else
        {
            pending.AddRange(E(context.Unresolved));
            foreach (var basis in context.Samples)
            {
                var contributions = S(Contribution(fixture, basis, fixture.Frame.Horizontal),
                    Contribution(fixture, basis, fixture.Frame.Vertical));
                var unresolved = S(basis.Pending);
                samples.Add(new Relation(new Relation(new Relation(trace, basis.Sample), basis.Route),
                    new Relation(new Relation(basis.Applications, contributions),
                        new Relation(new Relation(basis.Delta, basis.Position), unresolved))));
                pending.AddRange(basis.Pending);
            }
        }
        return new Relation(new Relation(fixture.Surface, trace), new Relation(S(samples.ToArray()), S(pending.ToArray())));
    }

    /// <summary>Revalidates membership, causes, input accounting and sampling
    /// against the connected trace. It is not global authentication of a caller's
    /// independently fabricated historical ledger.</summary>
    public static bool TryReadResult(FriezeFixture fixture, Relation read,
        out Relation samples, out Relation unresolved)
    {
        samples = unresolved = null!;
        if (read.Left is not Relation { Right: Relation trace } owner || !ReferenceEquals(owner.Left, fixture.Surface) ||
            read.Right is not Relation { Left: Relation clauses, Right: Relation pending } ||
            !Closed(clauses) || !Closed(pending)) return false;
        if (!ReadContext(fixture, trace, out var context))
        {
            if (E(clauses).Length != 0 || !SameRows(E(pending), [trace])) return false;
        }
        else
        {
            var rows = E(clauses); if (rows.Length != context.Samples.Length) return false;
            var expectedPending = E(context.Unresolved).ToList();
            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] is not Relation clause || !ReadSample(fixture, trace, clause, context.Samples[i],
                    out _, out _, out _, out _, out _, out _, out _)) return false;
                expectedPending.AddRange(context.Samples[i].Pending);
            }
            if (!SameRows(E(pending), expectedPending.ToArray())) return false;
        }
        samples = clauses; unresolved = pending; return true;
    }

    public static bool TryReadSample(FriezeFixture fixture, Relation read, out Relation sample,
        out Relation samplingRoute, out Relation applications, out Relation axisContributions,
        out Relation deltaRead, out Relation positionRead, out Relation unresolved)
    {
        sample = samplingRoute = applications = axisContributions = deltaRead = positionRead = unresolved = null!;
        if (read.Left is not Relation { Left: Relation { Left: Relation trace, Right: Relation actual } } ||
            !ReadContext(fixture, trace, out var context)) return false;
        var basis = context.Samples.SingleOrDefault(item => ReferenceEquals(item.Sample, actual));
        return basis is not null && ReadSample(fixture, trace, read, basis, out sample, out samplingRoute,
            out applications, out axisContributions, out deltaRead, out positionRead, out unresolved);
    }

    public static bool TryReadContribution(FriezeFixture fixture, Relation contribution,
        out Relation axis, out Relation applications, out Relation displacementRead)
    {
        axis = applications = displacementRead = null!;
        if (contribution.Left is not Relation { Left: Relation selected, Right: Relation apps } ||
            !ReferenceEquals(selected, fixture.Frame.Horizontal) && !ReferenceEquals(selected, fixture.Frame.Vertical) ||
            contribution.Right is not Relation reduction || !Closed(apps) ||
            !fixture.Frame.TryReadResult(reduction, out _, out _, out _) ||
            !ReductionInputs(fixture, reduction, out var before, out var incoming) || !Closed(before) || E(before).Length != 0)
            return false;
        var moves = new List<IElement>();
        foreach (var item in E(apps))
        {
            if (item is not Relation { Right: Relation move } ||
                !fixture.Frame.Direction(move, out var actualAxis, out _, out _) || !ReferenceEquals(selected, actualAxis)) return false;
            moves.Add(move);
        }
        if (!SameRows(E(incoming), moves.ToArray())) return false;
        axis = selected; applications = apps; displacementRead = reduction; return true;
    }

    private static Relation Contribution(FriezeFixture fixture, SampleBasis sample, Relation axis)
    {
        var applications = Attributable(fixture, sample, axis);
        var incoming = applications.Select(app => app.Right).ToArray();
        var support = sample.Unknown ? StructuralConstraint.Open(incoming) : S(incoming);
        return new Relation(new Relation(axis, S(applications)), fixture.Frame.Reduce(S(), support));
    }

    private static bool ReadSample(FriezeFixture fixture, Relation trace, Relation read, SampleBasis basis,
        out Relation sample, out Relation route, out Relation applications, out Relation contributions,
        out Relation delta, out Relation position, out Relation unresolved)
    {
        sample = route = applications = contributions = delta = position = unresolved = null!;
        if (read.Left is not Relation { Left: Relation owner, Right: Relation selectedRoute } ||
            !ReferenceEquals(owner.Left, trace) || !ReferenceEquals(owner.Right, basis.Sample) || !ReferenceEquals(selectedRoute, basis.Route) ||
            read.Right is not Relation { Left: Relation inputs, Right: Relation result } ||
            !ReferenceEquals(inputs.Left, basis.Applications) || inputs.Right is not Relation axes || !Closed(axes) ||
            result.Left is not Relation reads || !ReferenceEquals(reads.Left, basis.Delta) || !ReferenceEquals(reads.Right, basis.Position) ||
            result.Right is not Relation pending || !Closed(pending) || !SameRows(E(pending), basis.Pending) || E(axes).Length != 2)
            return false;
        var expectedAxes = new[] { fixture.Frame.Horizontal, fixture.Frame.Vertical };
        for (var i = 0; i < expectedAxes.Length; i++)
        {
            if (E(axes)[i] is not Relation row || !TryReadContribution(fixture, row, out var axis, out var apps, out var reduction) ||
                !ReferenceEquals(axis, expectedAxes[i]) ||
                !SameRows(E(apps), Attributable(fixture, basis, axis).Cast<IElement>().ToArray()) ||
                !ReductionInputs(fixture, reduction, out _, out var incoming) || Closed(incoming) == basis.Unknown) return false;
        }
        sample = basis.Sample; route = selectedRoute; applications = basis.Applications; contributions = axes;
        delta = basis.Delta; position = basis.Position; unresolved = pending; return true;
    }

    private static Relation[] Attributable(FriezeFixture fixture, SampleBasis basis, Relation axis) =>
        E(basis.Applications).Cast<Relation>().Where(app => app.Right is Relation move &&
            fixture.Frame.Direction(move, out var actualAxis, out _, out _) && ReferenceEquals(axis, actualAxis)).ToArray();

    private sealed record PhaseBasis(Relation Receipt, Relation Port, IElement Event, IElement Phase);
    private sealed record EventBoundary(PhaseBasis? Phase, Relation? Root, IElement Event);
    private sealed record SampleBoundary(Relation Route, IElement Cause, IElement Event, int ApplicationEnd);
    private sealed record SampleBasis(Relation Sample, Relation Route, Relation Applications, Relation Delta,
        Relation Position, bool Unknown, IElement[] Pending);
    private sealed record Context(SampleBasis[] Samples, Relation Unresolved);

    private static bool ReadContext(FriezeFixture fixture, Relation trace, out Context context)
    {
        context = null!;
        if (!fixture.TryReadTrace(trace, out var initial, out var final, out var steps, out var applications,
                out var samples, out var unresolved) ||
            !fixture.Network.TryReadState(initial, out _, out _, out _, out _) ||
            !fixture.Network.TryReadState(final, out _, out _, out _, out _) ||
            E(applications).Any(item => item is not Relation) || E(samples).Any(item => item is not Relation)) return false;
        var phases = new List<PhaseBasis>(); var roots = new Dictionary<Relation, IElement>(ReferenceEqualityComparer.Instance);
        var boundaries = new List<EventBoundary>();
        var nativePending = E(unresolved).ToList();
        var previous = initial;
        foreach (var item in E(steps))
        {
            if (item is not Relation step || !fixture.Network.TryReadStep(step, out var before, out _, out var after,
                    out var accepted, out var blocked, out var pending) || !ReferenceEquals(before, previous) ||
                !fixture.Network.TryReadState(after, out var rootEvent, out _, out _, out _)) return false;
            previous = after;
            foreach (var failure in E(blocked).Concat(E(pending)))
                if (!nativePending.Any(existing => ReferenceEquals(existing, failure))) nativePending.Add(failure);
            if (E(blocked).Length == 0 && E(pending).Length == 0 && !roots.TryAdd(step, rootEvent)) return false;
            foreach (var receipt in E(accepted))
            {
                if (receipt is not Relation phaseReceipt || phaseReceipt.Left is not Relation { Right: var surface }) return false;
                var clocks = fixture.Components.Where(clock => ReferenceEquals(clock.Surface, surface)).ToArray();
                if (clocks.Length != 1 || !clocks[0].TryReadAccepted(phaseReceipt, out var source, out var occurrence, out var phase)) return false;
                var ports = E(fixture.Network.Ports).OfType<Relation>().Where(port => ReferenceEquals(port.Right, surface) && Same(port.Left, source)).ToArray();
                if (ports.Length != 1 || phases.Any(row => ReferenceEquals(row.Receipt, phaseReceipt))) return false;
                var phaseBasis = new PhaseBasis(phaseReceipt, ports[0], occurrence, phase);
                phases.Add(phaseBasis); boundaries.Add(new(phaseBasis, null, occurrence));
            }
            if (E(blocked).Length == 0 && E(pending).Length == 0) boundaries.Add(new(null, step, rootEvent));
        }
        if (!ReferenceEquals(previous, final)) return false;
        var all = E(applications).Cast<Relation>().ToArray(); var cursor = 0;
        var emissions = new Dictionary<Relation, (int Count, bool Known)>(ReferenceEqualityComparer.Instance);
        foreach (var phase in phases)
        {
            var expected = FiniteLaw.Evaluate(fixture.OutputLaw, S(phase.Phase), S(phase.Port));
            if (!FiniteLaw.TryReadResult(expected, out var support, out var residue, out _)) return false;
            var known = Closed(support) && E(support) is [Relation word] && Closed(word) && E(residue).Length == 0;
            var moves = known ? E((Relation)E(support)[0]) : new IElement[] { new Placeholder() };
            emissions.Add(phase.Receipt, (moves.Length, known));
            foreach (var move in moves)
            {
                if (cursor >= all.Length || all[cursor].Left is not Relation source || !ReferenceEquals(source.Left, phase.Receipt) ||
                    source.Right is not Relation output || !Output(fixture, output, expected, phase) ||
                    (known ? !Same(all[cursor].Right, move) : all[cursor].Right is not Placeholder)) return false;
                if (!known && !nativePending.Any(existing => ReferenceEquals(existing, output))) nativePending.Add(output);
                cursor++;
            }
        }
        if (cursor != all.Length) return false;

        // Reconstruct only the advertised commit boundaries in event order.
        // This prevents assigning future applications to an earlier root cause.
        var expectedSamples = new List<SampleBoundary>(); var emitted = 0;
        foreach (var boundary in boundaries)
        {
            IElement source; IElement cause;
            if (boundary.Phase is { } phase)
            {
                var emission = emissions[phase.Receipt]; emitted += emission.Count;
                if (!emission.Known) continue; // Run retains a failed emission until a later valid commit.
                source = phase.Port; cause = phase.Receipt;
            }
            else { source = fixture.Network.Surface; cause = boundary.Root!; }
            foreach (var route in E(fixture.Sampling).OfType<Relation>().Where(route => ReferenceEquals(route.Left, source)))
            {
                if (E(fixture.Sampling).OfType<Relation>().Count(other => ReferenceEquals(other.Left, source) &&
                    ReferenceEquals(other.Right, route.Right)) != 1) return false;
                expectedSamples.Add(new(route, cause, boundary.Event, emitted));
            }
        }
        if (expectedSamples.Count != E(samples).Length) return false;

        var clauses = new List<SampleBasis>(); var appCursor = 0; Relation? lastPosition = null;
        foreach (var item in E(samples).Cast<Relation>())
        {
            var boundary = expectedSamples[clauses.Count];
            if (clauses.Any(clause => ReferenceEquals(clause.Sample, item)) ||
                !FriezeFixture.TryReadSample(item, out var cause, out var occurrence, out var before, out var after, out var apps, out var position) ||
                occurrence is not Relation eventIdentity ||
                !FriezeFixture.TryReadSampleDelta(item, out var delta) ||
                !fixture.Frame.TryReadResult(delta, out _, out _, out var deltaPending) ||
                !fixture.Frame.TryReadResult(position, out var result, out _, out var positionPending) || !ReferenceEquals(result, after) ||
                !ReductionInputs(fixture, delta, out var empty, out var deltaInput) || !Closed(empty) || E(empty).Length != 0 ||
                !ReductionInputs(fixture, position, out var prior, out var positionInput) || !ReferenceEquals(prior, before) ||
                !Closed(deltaInput) || !Closed(positionInput) ||
                (lastPosition is null ? !Closed(before) || E(before).Length != 0 : !ReferenceEquals(lastPosition, before))) return false;
            var actual = E(apps); var incoming = new List<IElement>(); var local = new List<IElement>(); var unknown = false;
            if (actual.Length != boundary.ApplicationEnd - appCursor || !ReferenceEquals(cause, boundary.Cause) ||
                !Same(eventIdentity.Left, boundary.Event) || !ReferenceEquals(eventIdentity.Right, boundary.Route.Right)) return false;
            foreach (var app in actual)
            {
                if (appCursor >= all.Length || !ReferenceEquals(app, all[appCursor++])) return false;
                var motion = ((Relation)app).Right; incoming.Add(motion);
                if (motion is not Relation move || !fixture.Frame.Direction(move, out _, out _, out _)) { local.Add(app); unknown = true; }
            }
            if (!SameRows(E(deltaInput), incoming.ToArray()) || !SameRows(E(positionInput), incoming.ToArray())) return false;
            local.AddRange(E(deltaPending)); local.AddRange(E(positionPending));
            clauses.Add(new(item, boundary.Route, apps, delta, position, unknown, local.ToArray())); lastPosition = after;
        }
        if (appCursor != all.Length && !E(unresolved).OfType<Relation>().Any(row => ReferenceEquals(row.Right, fixture.Sampling) &&
            row.Left is Relation offered && Closed(offered) && SameRows(E(offered), all.Skip(appCursor).Cast<IElement>().ToArray()))) return false;
        context = new(clauses.ToArray(), S(nativePending.ToArray())); return true;
    }

    private static bool Output(FriezeFixture fixture, Relation read, Relation expected, PhaseBasis phase)
    {
        if (read.Left is not Relation { Left: Relation source, Right: var domain } ||
            !ReferenceEquals(source.Left, fixture.OutputLaw) || source.Right is not Relation { Left: Relation a, Right: Relation b } ||
            !Closed(a) || !Closed(b) || !SameRows(E(a), [phase.Phase]) || !SameRows(E(b), [phase.Port]) ||
            !FiniteLaw.TryRead(fixture.OutputLaw, out _, out _, out var actualDomain, out _) || !ReferenceEquals(domain, actualDomain) ||
            !FiniteLaw.TryReadResult(read, out var support, out var pending, out var rows) ||
            !FiniteLaw.TryReadResult(expected, out var expectedSupport, out var expectedPending, out var expectedRows)) return false;
        return Closed(support) == Closed(expectedSupport) && SameRows(E(support), E(expectedSupport)) &&
            (E(pending).Length == 0) == (E(expectedPending).Length == 0) && SameRows(E(rows), E(expectedRows));
    }

    private static bool ReductionInputs(FriezeFixture fixture, Relation read, out Relation before, out Relation incoming)
    {
        before = incoming = null!;
        if (read.Left is not Relation { Left: var frame, Right: Relation { Left: Relation a, Right: Relation b } } ||
            !ReferenceEquals(frame, fixture.Frame.Surface) || !Scope(a) || !Scope(b)) return false;
        before = a; incoming = b; return true;
    }

    private static bool SameRows(IElement[] a, IElement[] b) => a.Length == b.Length && a.Zip(b).All(pair => Same(pair.First, pair.Second));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Scope(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out _, out _);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
