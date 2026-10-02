using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public class GojoSyntaxReplayTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void DependenciesFollowPublishedOutputsRatherThanEqualCoordinatesOrSharedInputs(int restores)
    {
        var world = ReadFixture(restores);
        var applications = ObserveInitial(world);
        AssertPoint(Result(applications[7]), 6, 7, 2);
        var edges = Dependencies(applications);
        Assert.Equal(new[] { (0, 1), (0, 2), (1, 5), (2, 5), (2, 6), (4, 6), (5, 7), (6, 7) },
            edges.Select(edge => (Array.IndexOf(applications, R(F(edge)[0])), Array.IndexOf(applications, R(F(edge)[1])))));
        AssertPoint(Result(applications[2]), 6, 4, 1);
        AssertPoint(Result(applications[3]), 6, 4, 1);
        Assert.NotSame(Result(applications[2]), Result(applications[3]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EditingOneControlCanPauseAndResumeWithoutPublishingStaleSamplesOrLosingHistory(int restores)
    {
        var world = ReadFixture(restores);
        var applications = ObserveInitial(world);
        var dependencies = Dependencies(applications);
        var previous = Group(applications);
        var beforePacket = GojoNumericFormat.Encode(previous);
        var edit = R(Assert.Single(F(world[2])));
        var bindings = F(world[1]).Select(R).Where(binding => !ReferenceEquals(binding.Left, edit.Left)).Append(edit).ToList();
        // Only unaffected observations may be carried into the new application.
        foreach (var index in new[] { 3, 4 }) bindings.Add(new Relation(OutputSlot(R(F(world[0])[index])), Result(applications[index])));
        var publication = Replay(0, world, applications, bindings);
        var changedFocus = R(F(publication)[1]);
        Assert.True(TransformSurface.TryCreateFocusChange(new Carrier(), R(F(applications[0])[1]), changedFocus,
            world[3], Unknown(), Unknown(), out var change));
        Assert.True(TransformSurface.TryCreateDependencyAffectedSlice(new Carrier(), previous, Group(dependencies),
            change, Unknown(), Unknown(), out var affected));
        Assert.Equal(new[] { applications[3], applications[4] }, F(F(affected)[3]));
        Assert.Equal(new[] { applications[0], applications[1], applications[2], applications[5], applications[6], applications[7] },
            F(F(affected)[4]));
        var unrelatedResidue = new Placeholder();
        Assert.True(TransformSurface.TryCreatePartialReplayFromDependencyAffectedSlice(new Carrier(), affected,
            [publication], unrelatedResidue, Unknown(), out var pending));
        AssertStale(pending, applications, 1, 2);
        Assert.Equal(3, F(F(pending)[3]).Length);
        Assert.DoesNotContain(applications[7], F(F(pending)[3]));
        AssertPoint(Result(applications[7]), 6, 7, 2);
        var phases = new List<Relation> { pending };
        var transitions = new List<Relation> { publication };

        var ab = Replay(1, world, applications, bindings);
        Assert.True(TransformSurface.TryResumeDependencyReplay(new Carrier(), affected, pending, [ab], Unknown(), Unknown(), out var afterAb));
        AssertStale(afterAb, applications, 2, 5);
        // AB is new but BC is still old. A mathematically valid mixed-version
        // calculation must not be admitted as the refreshed ABC observation.
        var mixedBindings = bindings.Append(new Relation(OutputSlot(R(F(world[0])[2])), Result(applications[2]))).ToList();
        var prematureAbc = Replay(5, world, applications, mixedBindings);
        Assert.False(TransformSurface.TryResumeDependencyReplay(new Carrier(), affected, afterAb, [prematureAbc], Unknown(), Unknown(), out _));
        phases.Add(afterAb); transitions.Add(ab);

        var bc = Replay(2, world, applications, bindings);
        Assert.True(TransformSurface.TryResumeDependencyReplay(new Carrier(), affected, afterAb, [bc], Unknown(), Unknown(), out var afterBc));
        AssertStale(afterBc, applications, 5, 5, 6); // two changed inputs to ABC
        phases.Add(afterBc); transitions.Add(bc);

        var abc = Replay(5, world, applications, bindings);
        // The older residue-only helper can discharge both incoming edges, but
        // has no graph from which to discover the next consumer.
        Assert.True(TransformSurface.TryCreatePartialReplayFromStaleDependencyResidue(new Carrier(), afterBc,
            [abc], Unknown(), Unknown(), out var residueOnly));
        AssertStale(residueOnly, applications, 6);
        Assert.True(TransformSurface.TryResumeDependencyReplay(new Carrier(), affected, afterBc, [abc], Unknown(), Unknown(), out var afterAbc));
        AssertStale(afterAbc, applications, 6, 7);
        phases.Add(afterAbc); transitions.Add(abc);

        var bcd = Replay(6, world, applications, bindings);
        Assert.True(TransformSurface.TryResumeDependencyReplay(new Carrier(), affected, afterAbc, [bcd], Unknown(), Unknown(), out var afterBcd));
        AssertStale(afterBcd, applications, 7, 7);
        phases.Add(afterBcd); transitions.Add(bcd);
        var final = Replay(7, world, applications, bindings);
        Assert.True(TransformSurface.TryResumeDependencyReplay(new Carrier(), affected, afterBcd, [final], Unknown(), Unknown(), out var complete));
        AssertStale(complete, applications);
        AssertPoint(Result(R(F(final)[2])), 6, 13, 2);
        var current = F(F(complete)[3]);
        Assert.Equal(8, current.Length);
        Assert.Same(applications[3], current[3]); Assert.Same(applications[4], current[4]);
        Assert.Same(unrelatedResidue, F(complete)[4]); // unrelated work remains open
        Assert.False(TransformSurface.TryResumeDependencyReplay(new Carrier(), affected, afterBcd, [bcd], Unknown(), Unknown(), out _));
        phases.Add(complete); transitions.Add(final);

        var timeline = TimeStepSequence.Create(new Carrier(), (Carrier)world[5], previous,
            [.. phases.Select((phase, index) => TimeStepSequence.Step(new Carrier(), (Carrier)world[5], index, index + 1,
                index == 0 ? previous : phases[index - 1], transitions[index], phase, F(phase)[4]))]);
        var outline = F(world[8]);
        var claim = F(outline[7]);
        var published = StructuralPattern.Apply(world[8],
            new Relation(outline[0], previous), new Relation(outline[1], change),
            new Relation(outline[2], F(affected)[2]), new Relation(outline[3], affected),
            new Relation(outline[4], pending), new Relation(outline[5], complete), new Relation(outline[6], timeline),
            new Relation(claim[0], F(R(F(final)[2]))[5]),
            new Relation(claim[1], F(F(R(F(world[0])[7]))[0])[2]),
            new Relation(claim[2], Result(R(F(final)[2]))));
        Assert.Same(pending, F(published)[4]); Assert.Same(complete, F(published)[5]);
        AssertPoint(F(F(published)[7])[2], 6, 13, 2);
        Assert.Equal(beforePacket, GojoNumericFormat.Encode(previous));
        // Restore the paused state together with its dependency context and then
        // resume it: context and aliases cannot be recovered from scalars alone.
        var restoredPaused = Restore(Group(affected, afterBcd, final), 2);
        Assert.True(TransformSurface.TryResumeDependencyReplay(new Carrier(), R(F(restoredPaused)[0]), R(F(restoredPaused)[1]),
            [R(F(restoredPaused)[2])], Unknown(), Unknown(), out var restoredComplete));
        AssertPoint(Result(F(F(restoredComplete)[3])[^1]), 6, 13, 2);
        var history = Restore(Group(previous, Group([.. phases]), timeline, world[3], published), 2);
        var restoredPrevious = F(F(history)[0]);
        AssertPoint(Result(restoredPrevious[7]), 6, 7, 2);
        AssertPoint(R(F(history)[3]).Left, 4, 8, 1);
        AssertPoint(R(F(history)[3]).Right, 4, 16, 1);
        Assert.True(TimeStepSequence.TryRead(R(F(history)[2]), out _, out var initial, out var steps));
        Assert.Same(F(history)[0], initial); Assert.Equal(phases.Count, steps.Length);
        for (var i = 0; i < steps.Length; i++)
        {
            Assert.True(TimeStepSequence.TryReadStep(steps[i], out _, out var start, out var end,
                out var before, out _, out var after, out _));
            Assert.Equal(i, start.Ticks); Assert.Equal(i + 1, end.Ticks);
            Assert.Same(i == 0 ? initial : F(F(history)[1])[i - 1], before);
            Assert.Same(F(F(history)[1])[i], after);
        }
    }

    [Fact]
    public void AStoredLiveReferenceIsNotACapturedHistoricalInput()
    {
        var world = ReadFixture(0);
        var applications = ObserveInitial(world);
        var packet = GojoNumericFormat.Encode(Group(world[3], Group(applications)));
        var oldControl = R(world[3]).Left;
        var location = (CarrierLocation)F(oldControl)[1];
        // Demonstrate the failure mode through an explicit structural port:
        // the source reference drifts while the old arithmetic result stays put.
        var input = new Carrier();
        var port = new Relation(Relation.Point(input, 8), new Relation(location, location.Carrier));
        Assert.True(port.TrySetStructureFrom(new CarrierLocation(input, 16)));
        AssertPoint(oldControl, 4, 16, 1);
        AssertPoint(Result(applications[7]), 6, 7, 2);
        var captured = GojoNumericFormat.Decode(packet)[0];
        AssertPoint(R(F(captured)[0]).Left, 4, 8, 1);
        AssertPoint(Result(F(F(captured)[1])[7]), 6, 7, 2);
    }

    private static Relation Replay(int index, IElement[] world, Relation[] applications, List<Relation> bindings)
    {
        var transform = R(F(world[0])[index]);
        var (focus, localBindings, result, witness) = Observe(transform, bindings, index == 0);
        Assert.True(TransformSurface.TryReplayApplication(new Carrier(), applications[index], focus, localBindings,
            result, Unknown(), witness, out var replay));
        var output = OutputSlot(transform);
        bindings.RemoveAll(binding => ReferenceEquals(binding.Left, output));
        bindings.Add(new Relation(output, result));
        return replay;
    }

    private static void AssertStale(Relation partial, Relation[] applications, params int[] expected)
    {
        TransformSurface.TryReadStaleDependencyResidue(F(partial)[4], out var stale);
        Assert.Equal(expected, stale.Select(dependency => Array.IndexOf(applications, R(F(dependency)[2]))).Order());
    }

    private static IElement Restore(IElement root, int times)
    {
        for (var i = 0; i < times; i++) root = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        return root;
    }

    private static IElement[] ReadFixture(int restores)
    {
        var roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "Syntax", "bezier-replay.gojo"))).Roots.ToArray();
        for (var i = 0; i < restores; i++) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
        return [.. F(roots[0]), roots[1]];
    }

    private static Relation[] ObserveInitial(IElement[] world)
    {
        var bindings = F(world[1]).Select(R).ToList();
        var applications = new List<Relation>();
        foreach (var transform in F(world[0]).Select(R))
        {
            var (focus, localBindings, result, witness) = Observe(transform, bindings, applications.Count == 0);
            Assert.True(TransformSurface.TryCreateApplication(new Carrier(), transform, focus, localBindings,
                result, Unknown(), witness, out var application));
            applications.Add(application);
            bindings.Add(new Relation(OutputSlot(transform), result));
        }
        return [.. applications];
    }

    private static (Relation Focus, Relation Bindings, IElement Result, Relation Witness) Observe(
        Relation transform, IReadOnlyList<Relation> bindings, bool publish)
    {
        var fields = F(transform);
        var operation = R(StructuralPattern.Apply(fields[1], bindings));
        IElement result;
        Relation witness;
        if (publish)
        {
            result = operation.Left;
            witness = ReadWitness.Create(new Carrier(), operation.Left, transform, result, Group(), operation);
        }
        else
        {
            Assert.True(AffineFrame.TryInterpolate(operation, out witness));
            result = F(witness)[2];
        }
        var source = StructuralPattern.Apply(fields[0], bindings);
        var focus = FocusSurface.Create(new Carrier(), source, fields[0], source, Unknown(), result,
            Unknown(), Unknown(), witness);
        var localBindings = F(fields[0]).OfType<Placeholder>()
            .Select(slot => new Relation(slot, StructuralPattern.Apply(slot, bindings))).ToList();
        localBindings.Add(new Relation(OutputSlot(transform), result));
        return (focus, Group([.. localBindings]), result, witness);
    }

    private static Relation[] Dependencies(Relation[] applications)
    {
        var dependencies = new List<Relation>();
        for (var i = 0; i < applications.Length; i++)
            for (var j = 0; j < applications.Length; j++)
                if (i != j && TransformSurface.TryCreateApplicationDependency(new Carrier(), applications[i],
                        applications[j], Unknown(), Unknown(), out var dependency)) dependencies.Add(dependency);
        return [.. dependencies];
    }

    private static IElement OutputSlot(Relation transform) => Assert.Single(F(F(transform)[2]));
    private static IElement Result(IElement application) => F(application)[3];
    private static Relation Unknown() => Relation.Point(Carrier.Unknown, 0);
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static Relation R(IElement element) => Assert.IsType<Relation>(element);
    private static IElement[] F(IElement element)
    { Assert.True(R(element).TryReadOrderedGroup(out var fields)); return fields; }
    private static void AssertPoint(IElement point, long x, long yn, long yd)
    {
        var coordinates = F(point);
        Assert.True(ProjectionResidue.TryRead(coordinates[0], out _, out var actualX, out var xd));
        Assert.True(ProjectionResidue.TryRead(coordinates[1], out _, out var actualY, out var actualD));
        Assert.Equal(x, actualX); Assert.Equal(1, xd); Assert.Equal(yn, actualY); Assert.Equal(yd, actualD);
    }
}
