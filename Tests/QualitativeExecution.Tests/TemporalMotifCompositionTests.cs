using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class TemporalMotifCompositionTests
{
    [Fact]
    public void PhaseCycleReusesTemplatesWhileGeneratedOccurrencesAndLocalSitesStayFresh()
    {
        var (phases, a, b, landmark) = Fixtures();
        var source = new Placeholder();
        var initialEvent = new Placeholder();
        var state = phases.Initial(source, initialEvent, StructuralConstraint.Closed(b.Surface));
        var generated = new List<Relation>();
        var causes = new List<IElement>();
        foreach (var expected in new[] { a, b, a })
        {
            Assert.True(phases.TryReadState(state, out _, out var previous, out _));
            var drive = phases.Drive(source, new Placeholder(), previous);
            var receipt = phases.Step(state, drive);
            Assert.True(TryGenerate(phases, receipt, [a, b], out var occurrence));
            Assert.True(RepeatedMotif.TryReadOccurrence(occurrence, out var identity, out var template,
                out _, out _, out _));
            Assert.True(phases.TryReadAccepted(receipt, out _, out var actualEvent, out _));
            Assert.Same(expected.Surface, template);
            Assert.Same(actualEvent, identity);
            causes.Add(new Relation(receipt, occurrence));
            generated.Add(occurrence);
            state = After(phases, receipt);
        }

        var strip = Joined(StructuralConstraint.Closed(generated.Cast<IElement>().ToArray()));
        var evidence = new Relation(new Relation(state, StructuralConstraint.Closed(causes.ToArray())), strip);
        Settled(RepeatedMotif.Read(strip, StructuralConstraint.Closed(a.Surface, b.Surface, a.Surface)));
        var identities = new List<IElement>();
        var instances = new List<IElement>();
        var sites = new List<IElement>();
        foreach (var occurrence in generated)
        {
            Assert.True(RepeatedMotif.TryReadOccurrence(occurrence, out var identity, out _, out var instance,
                out var entry, out var exit));
            identities.Add(identity); instances.Add(instance); sites.Add(entry); sites.Add(exit);
            Assert.True(Contains(instance, landmark));
            Assert.True(Contains(evidence, occurrence));
        }
        Assert.Equal(3, identities.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(3, instances.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(6, sites.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.True(phases.TryReadHistory(state, out var history));
        Assert.Equal(4, Entries(history).Length);
        Assert.Same(initialEvent, Entries(history)[0]);
        Assert.Equal(identities.ToArray(), Entries(history).Skip(1).ToArray());
        Assert.Equal(4, Entries(history).Distinct(ReferenceEqualityComparer.Instance).Count());
        AssertNoNumbers(evidence);
    }

    [Fact]
    public void ReturningToATemplateDoesNotCloseTheOccurrenceStripBackOntoItsBeginning()
    {
        var (phases, a, b, _) = Fixtures();
        var state = phases.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(b.Surface));
        var generated = new List<Relation>();
        for (var i = 0; i < 3; i++)
        {
            Assert.True(phases.TryReadState(state, out var source, out var previous, out _));
            var receipt = phases.Step(state, phases.Drive(source, new Placeholder(), previous));
            Assert.True(TryGenerate(phases, receipt, [a, b], out var occurrence));
            generated.Add(occurrence);
            state = After(phases, receipt);
        }
        var strip = Joined(StructuralConstraint.Closed(generated.Cast<IElement>().ToArray()));
        var successors = Entries((Relation)((Relation)strip.Right).Left).Cast<Relation>().ToArray();
        var seams = Entries((Relation)((Relation)strip.Right).Right).Cast<Relation>().ToArray();
        Assert.Equal(2, successors.Length);
        Assert.Equal(2, seams.Length);
        for (var i = 0; i < successors.Length; i++)
        {
            Assert.Same(generated[i], successors[i].Left);
            Assert.Same(generated[i + 1], successors[i].Right);
            Assert.Same(successors[i], seams[i].Left);
            Assert.True(RepeatedMotif.TryReadOccurrence(generated[i], out _, out _, out _, out _, out var exit));
            Assert.True(RepeatedMotif.TryReadOccurrence(generated[i + 1], out _, out _, out _, out var entry, out _));
            var joinedPorts = Assert.IsType<Relation>(seams[i].Right);
            Assert.Same(exit, joinedPorts.Left);
            Assert.Same(entry, joinedPorts.Right);
            Assert.NotSame(exit, entry);
        }
        Assert.DoesNotContain(successors, edge => ReferenceEquals(edge.Left, generated[^1]) &&
            ReferenceEquals(edge.Right, generated[0]));
        Assert.True(RepeatedMotif.TryReadOccurrence(generated[0], out _, out var firstTemplate, out _, out _, out _));
        Assert.True(RepeatedMotif.TryReadOccurrence(generated[^1], out _, out var lastTemplate, out _, out _, out _));
        Assert.Same(firstTemplate, lastTemplate);
        Settled(RepeatedMotif.Read(strip));
    }

    [Fact]
    public void TemporalSuccessionDoesNotManufactureMissingMotifPortConnections()
    {
        var (phases, a, b, _) = Fixtures();
        var state = phases.Initial(new Placeholder(), new Placeholder(), StructuralConstraint.Closed(b.Surface));
        var generated = new List<IElement>();
        for (var i = 0; i < 2; i++)
        {
            Assert.True(phases.TryReadState(state, out var source, out var previous, out _));
            var receipt = phases.Step(state, phases.Drive(source, new Placeholder(), previous));
            Assert.True(TryGenerate(phases, receipt, [a, b], out var occurrence));
            generated.Add(occurrence);
            state = After(phases, receipt);
        }
        var ordered = StructuralConstraint.Closed(generated.ToArray());
        var missingSeams = RepeatedMotif.Sequence(ordered);
        Assert.True(RepeatedMotif.TryReadResult(RepeatedMotif.Read(missingSeams), out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
        Settled(RepeatedMotif.Read(Joined(ordered)));
    }

    [Fact]
    public void ReplayedDriveDoesNotInvokeTheOccurrenceSupplierAgain()
    {
        var (phases, a, b, _) = Fixtures();
        var source = new Placeholder();
        var previous = new Placeholder();
        var state = phases.Initial(source, previous, StructuralConstraint.Closed(b.Surface));
        var drive = phases.Drive(source, new Placeholder(), previous);
        var receipt = phases.Step(state, drive);
        var calls = 0;
        Assert.True(TryGenerate(phases, receipt, [a, b], out var occurrence, () => calls++));
        var after = After(phases, receipt);
        var replay = phases.Step(after, drive);
        Assert.False(TryGenerate(phases, replay, [a, b], out _,
            () => throw new InvalidOperationException("A blocked phase receipt must not query generation.")));
        Assert.Equal(1, calls);
        Assert.True(phases.TryReadStep(replay, out _, out _, out var retained, out var accepted, out var blocked, out var unresolved));
        Assert.Same(after, retained);
        Assert.Empty(Entries(accepted));
        Assert.Same(drive, Assert.Single(Entries(blocked)));
        Assert.Empty(Entries(unresolved));
        Settled(RepeatedMotif.Read(Joined(StructuralConstraint.Closed(occurrence))));
    }

    [Fact]
    public void MissingTransitionLeavesGenerationUnqueriedAndRetainsTheAttemptedDrive()
    {
        var (_, a, b, _) = Fixtures();
        var phases = new PhaseRecurrence(StructuralConstraint.Closed(a.Surface, b.Surface), StructuralConstraint.Closed());
        var source = new Placeholder();
        var previous = new Placeholder();
        var state = phases.Initial(source, previous, StructuralConstraint.Closed(a.Surface));
        var drive = phases.Drive(source, new Placeholder(), previous);
        var receipt = phases.Step(state, drive);
        Assert.False(TryGenerate(phases, receipt, [a, b], out _,
            () => throw new InvalidOperationException("An unresolved phase receipt cannot authorize generation.")));
        Assert.True(phases.TryReadStep(receipt, out _, out _, out var retained,
            out var accepted, out var blocked, out var unresolved));
        Assert.Same(state, retained);
        Assert.Empty(Entries(accepted));
        Assert.Empty(Entries(blocked));
        Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(receipt, drive));
    }

    [Fact]
    public void RereadingPermissionDoesNotAuthorizeTwoOccurrencesWithTheSameEventIdentityInOneStrip()
    {
        var (phases, a, b, _) = Fixtures();
        var source = new Placeholder();
        var previous = new Placeholder();
        var state = phases.Initial(source, previous, StructuralConstraint.Closed(b.Surface));
        var receipt = phases.Step(state, phases.Drive(source, new Placeholder(), previous));
        Assert.True(TryGenerate(phases, receipt, [a, b], out var first));
        Assert.True(TryGenerate(phases, receipt, [a, b], out var second));
        Assert.True(RepeatedMotif.TryReadOccurrence(first, out var firstEvent, out _, out var firstInstance, out _, out _));
        Assert.True(RepeatedMotif.TryReadOccurrence(second, out var secondEvent, out _, out var secondInstance, out _, out _));
        Assert.Same(firstEvent, secondEvent);
        Assert.NotSame(firstInstance, secondInstance);
        var strip = Joined(StructuralConstraint.Closed(first, second));
        Assert.True(RepeatedMotif.TryReadResult(RepeatedMotif.Read(strip), out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
        // Permission is observable repeatedly. An effect needs a consuming
        // history or, here, the strip's explicit occurrence-identity obligation.
    }

    [Fact]
    public void UnequalMeasuredIntervalsKeepTheSameEventsAndTemplatesWithoutClaimingEqualSpacing()
    {
        var (phases, a, b, _) = Fixtures();
        var source = new Placeholder();
        IElement[] events = [new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder()];
        long[] ticks = [0, 2, 17, 18];
        var clock = new Carrier();
        var state = phases.Initial(source, events[0], StructuralConstraint.Closed(b.Surface));
        var generated = new List<IElement>();
        var calibrations = new List<IElement>();
        var causes = new List<IElement>();
        for (var i = 1; i < events.Length; i++)
        {
            var calibration = new Relation(new Relation(events[i - 1], events[i]), Relation.Segment(clock, ticks[i - 1], ticks[i]));
            calibrations.Add(calibration);
            var receipt = phases.Step(state, phases.Drive(source, events[i], events[i - 1], calibration));
            Assert.True(TryGenerate(phases, receipt, [a, b], out var occurrence));
            Assert.True(RepeatedMotif.TryReadOccurrence(occurrence, out var actualEvent, out _, out _, out _, out _));
            Assert.Same(events[i], actualEvent);
            generated.Add(occurrence);
            causes.Add(new Relation(receipt, occurrence));
            state = After(phases, receipt);
        }
        var strip = Joined(StructuralConstraint.Closed(generated.ToArray()));
        var composed = new Relation(new Relation(state, StructuralConstraint.Closed(causes.ToArray())), strip);
        Settled(RepeatedMotif.Read(strip, StructuralConstraint.Closed(a.Surface, b.Surface, a.Surface)));
        Assert.All(calibrations, calibration => Assert.True(Contains(composed, calibration)));
        Assert.True(phases.TryReadHistory(state, out var history));
        Assert.Equal(events, Entries(history));
        Assert.Equal(0UL, clock.Value);
        Assert.Equal(0, clock.Tension);
        AssertNoNumbers(strip);
        // These are topological motifs joined in sequence. Time calibration
        // establishes neither geometric translation nor a metric frieze.
    }

    private static (PhaseRecurrence Phases, RepeatedMotif A, RepeatedMotif B, IElement Landmark) Fixtures()
    {
        var entry = new Placeholder();
        var exit = new Placeholder();
        var landmark = new Placeholder();
        var variables = StructuralConstraint.Closed(entry, exit);
        var a = new RepeatedMotif(new Relation(new Relation(entry, landmark), new Relation(landmark, exit)),
            variables, entry, exit);
        var b = new RepeatedMotif(new Relation(new Relation(entry, landmark), new Relation(entry, exit)),
            variables, entry, exit);
        var phases = new PhaseRecurrence(StructuralConstraint.Closed(a.Surface, b.Surface),
            StructuralConstraint.Closed(new Relation(a.Surface, b.Surface), new Relation(b.Surface, a.Surface)));
        return (phases, a, b, landmark);
    }

    private static bool TryGenerate(PhaseRecurrence phases, Relation receipt, RepeatedMotif[] templates,
        out Relation occurrence, Action? onGenerate = null)
    {
        occurrence = null!;
        if (!phases.TryReadAccepted(receipt, out _, out var eventIdentity, out var selectedTemplate)) return false;
        var supplier = Assert.Single(templates, template => ReferenceEquals(template.Surface, selectedTemplate));
        onGenerate?.Invoke();
        occurrence = supplier.Generate(eventIdentity);
        return true;
    }

    private static Relation After(PhaseRecurrence phases, Relation receipt)
    {
        Assert.True(phases.TryReadStep(receipt, out _, out _, out var after, out var accepted, out var blocked, out var unresolved));
        Assert.Single(Entries(accepted));
        Assert.Empty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
        return after;
    }

    private static Relation Joined(Relation occurrences)
    {
        var strip = RepeatedMotif.Sequence(occurrences);
        var successors = (Relation)((Relation)strip.Right).Left;
        var seams = Entries(successors).Cast<Relation>().Select(RepeatedMotif.Join).Cast<IElement>().ToArray();
        return new Relation(occurrences, new Relation(successors, StructuralConstraint.Closed(seams)));
    }

    private static void Settled(Relation read)
    {
        Assert.True(RepeatedMotif.TryReadResult(read, out var passed, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(passed));
        Assert.Empty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
    }

    private static IElement[] Entries(Relation scope) => FiniteLaw.Entries(scope);

    private static bool Contains(IElement root, IElement target) =>
        Traverse(root).Any(element => ReferenceEquals(element, target));

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root))
        {
            Assert.IsNotType<Carrier>(element);
            Assert.IsNotType<CarrierLocation>(element);
        }
    }

    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<IElement>();
        stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is Relation relation)
            {
                stack.Push(relation.Left);
                stack.Push(relation.Right);
            }
        }
    }
}
