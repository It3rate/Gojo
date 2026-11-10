using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Core;
using Gojo.Encoding.Core.Composition;
using Gojo.Encoding.Core.Native;

namespace Gojo.Encoding.Core.Tests;

public sealed class CompositionTests
{
    [Fact]
    public void One_site_can_be_exposed_without_exposing_every_internal_landmark()
    {
        var f = Passage();
        Assert.True(RoutedPassage.TryRead(f.Machine.Surface, out var definition));
        Assert.Equal(2, E(definition.Ports).Length);
        Assert.Equal(2, E(definition.Edges).Length); // Three actual sites, only two boundary ports.
        Assert.DoesNotContain(E(definition.Ports).Cast<Relation>(), p => ParticipantIdentity.Same(p.Left, f.Middle));
        var packet = new Placeholder(); f.Machine.Import(f.Input, packet);
        var read = f.Machine.Apply(S(f.Input), S(f.Go));
        Assert.Single(E(read.Accepted)); Assert.True(read.OwnershipPreserved);
        Assert.Same(packet, Assert.Single(E(f.Machine.Transport.PortPackets(f.Output))));
    }

    [Fact]
    public void Go_or_stop_keeps_material_while_two_controls_with_the_same_action_can_move()
    {
        var f = Passage(); f.Machine.Import(f.Input, new Placeholder());
        var before = f.Machine.Transport.State;
        var uncertain = f.Machine.Apply(S(f.Input), S(f.Go, f.Stop));
        Assert.Equal("choice", uncertain.Outcome); Assert.Same(before, f.Machine.Transport.State);
        Assert.Single(E(uncertain.Remainder)); Assert.Empty(E(uncertain.Accepted));
        var agreed = f.Machine.Apply(S(f.Input), S(f.Go, f.AlsoGo));
        Assert.Single(E(agreed.Accepted)); Assert.True(agreed.OwnershipPreserved);
    }

    [Fact]
    public void Serialized_definition_can_supply_the_same_passage_in_a_fresh_local_world()
    {
        var original = Passage();
        var bytes = GojoNumericFormat.Encode(original.Machine.Surface, original.Input, original.Output, original.Go);
        var restored = GojoNumericFormat.Decode(bytes);
        Assert.True(RoutedPassage.TryRead((Relation)restored[0], out var definition));
        var capacities = (Relation)((Relation)definition.Transport.Right).Left;
        var machine = new RoutedPassage(definition.Edges, definition.Ports, definition.Paths,
            definition.History, (Relation)capacities.Left, definition.Law);
        var input = (Relation)restored[1]; var output = (Relation)restored[2];
        Assert.NotSame(original.Input, input);
        var packet = new Placeholder(); machine.Import(input, packet);
        var read = machine.Apply(S(input), S(restored[3]));
        Assert.Single(E(read.Accepted)); Assert.True(read.OwnershipPreserved);
        Assert.Same(packet, Assert.Single(E(machine.Transport.PortPackets(output))));
        Assert.Empty(E(original.Machine.Transport.ImportedPackets));
    }

    [Fact]
    public void Coordinate_coincidence_cannot_repair_a_disconnected_path_but_shared_tick_identity_can()
    {
        var carrier = new Carrier(); var tick = new TickRef(1);
        var shared = Passage(new CarrierLocation(carrier, tick), new CarrierLocation(carrier, tick));
        Assert.True(RoutedPassage.TryRead(shared.Machine.Surface, out _));
        var coincident = Passage(new CarrierLocation(carrier, 1), new CarrierLocation(carrier, 1));
        Assert.False(RoutedPassage.TryRead(coincident.Machine.Surface, out _));
    }

    [Fact]
    public void Duplicate_capacity_sites_are_invalid_but_distinct_ports_can_share_capacity()
    {
        var f = Passage(); var carrier = new Carrier(); var tick = new TickRef(1);
        IElement slot = new CarrierLocation(carrier, tick), alias = new CarrierLocation(carrier, tick);
        RoutedPassage WithCapacity(Relation capacity) => new(f.Machine.Edges, f.Machine.Ports,
            f.Machine.Paths, f.Machine.History, capacity, f.Machine.Law);
        var duplicate = WithCapacity(S(new Relation(f.Input, S(slot, alias)), new Relation(f.Output, S(new Placeholder()))));
        Assert.False(RoutedPassage.TryRead(duplicate.Surface, out _));
        var shared = WithCapacity(S(new Relation(f.Input, S(slot)), new Relation(f.Output, S(alias))));
        Assert.True(RoutedPassage.TryRead(shared.Surface, out _));
        var packet = new Placeholder(); shared.Import(f.Input, packet);
        var read = shared.Apply(S(f.Input), S(f.Go));
        Assert.Single(E(read.Accepted)); Assert.True(read.OwnershipPreserved);
        Assert.Same(packet, Assert.Single(E(shared.Transport.PortPackets(f.Output))));
    }

    [Fact]
    public void Participant_hashes_follow_tick_identity_when_coordinates_move()
    {
        var carrier = new Carrier(); var tick = new TickRef(2);
        IElement a = new CarrierLocation(carrier, tick), alias = new CarrierLocation(carrier, tick);
        IElement coincident = new CarrierLocation(carrier, 2);
        var set = new HashSet<IElement>(ParticipantIdentity.Comparer) { a };
        Assert.Contains(alias, set, ParticipantIdentity.Comparer);
        Assert.DoesNotContain(coincident, set, ParticipantIdentity.Comparer);
        tick.Move(1);
        Assert.Contains(alias, set, ParticipantIdentity.Comparer);
        Assert.DoesNotContain(coincident, set, ParticipantIdentity.Comparer);
    }

    [Fact]
    public void Construction_reuse_distinguishes_continuous_turn_attachment_branch_and_arriving_stop()
    {
        var frame = new QualitativeCurve.Frame(new QualitativeMotionFrame());
        var top = new Placeholder(); var mid = new Placeholder(); var bottom = new Placeholder();
        var upperEnd = new Placeholder(); var middleEnd = new Placeholder();
        var a = frame.Line(top, mid, frame.Word(0, 2), frame.Word(0, 1));
        var b = frame.Line(mid, bottom, frame.Word(0, 1), frame.Word(0, 0));
        var upper = frame.Line(top, upperEnd, frame.Word(0, 2), frame.Word(1, 2));
        var middle = frame.Line(mid, middleEnd, frame.Word(0, 1), frame.Word(1, 1));
        var source = new Relation(new Relation(frame.Surface, S(a, b, upper, middle)),
            S(S(Visit(frame, a), Visit(frame, b)), S(Visit(frame, upper)), S(Visit(frame, middle))));
        var read = ConstructionHistory.Read(source);
        Assert.True(read.Complete);
        Assert.Contains(read.Strokes[1].Events, e => e.Kind == "endpoint-attachment");
        Assert.Contains(read.Strokes[2].Events, e => e.Kind == "outward-branch");
        var incoming = new Relation(source.Left,
            S(S(Visit(frame, a), Visit(frame, b)), S(Visit(frame, upper)), S(Visit(frame, middle, true))));
        Assert.Equal("stop-at-interior", ConstructionHistory.Read(incoming).Strokes[2].EndKind);
        var turning = new Relation(source.Left,
            S(S(Visit(frame, upper, true), Visit(frame, a), Visit(frame, b)), S(Visit(frame, middle))));
        Assert.Contains(ConstructionHistory.Read(turning).Strokes[0].Events, e => e.Kind == "continuous-turn");
        var restored = (Relation)Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(read.Surface)));
        var input = (Relation)restored.Left;
        var reread = ConstructionHistory.Read((Relation)input.Left, (Relation)input.Right);
        Assert.True(reread.Complete);
        Assert.Equal(read.Strokes.Select(s => (s.StartKind, s.EndKind)), reread.Strokes.Select(s => (s.StartKind, s.EndKind)));
    }

    [Fact]
    public void Construction_placement_can_use_an_earlier_landmark_but_not_a_future_one()
    {
        var frame = new QualitativeCurve.Frame(new QualitativeMotionFrame());
        var top = new Placeholder(); var bottom = new Placeholder();
        var nextTop = new Placeholder(); var nextBottom = new Placeholder();
        var a = frame.Line(top, bottom, frame.Word(0, 2), frame.Word(0, 0));
        var b = frame.Line(nextTop, nextBottom, frame.Word(1, 2), frame.Word(1, 0));
        var first = S(Visit(frame, a)); var second = S(Visit(frame, b));
        var source = new Relation(new Relation(frame.Surface, S(a, b)), S(first, second));
        var plan = ConstructionHistory.Placement(second, first, top, nextTop, frame.Word(1, 0));
        var read = ConstructionHistory.Read(source, S(plan));
        Assert.True(read.Complete); Assert.True(read.Strokes[1].ParallelToSource);
        Assert.Equal("offset-start", read.Strokes[1].StartKind);
        var invalid = ConstructionHistory.Placement(first, second, nextTop, top, frame.Word(-1, 0));
        Assert.False(ConstructionHistory.Read(source, S(invalid)).Complete);
    }

    private sealed record Fixture(RoutedPassage Machine, Relation Input, Relation Output,
        IElement Middle, IElement Go, IElement AlsoGo, IElement Stop);

    private static Fixture Passage(IElement? middleEnd = null, IElement? middleStart = null)
    {
        var first = new Placeholder(); var last = new Placeholder();
        middleEnd ??= new Placeholder(); middleStart ??= middleEnd;
        var a = new Relation(first, middleEnd); var b = new Relation(middleStart, last);
        var input = RoutedPassage.Port(first); var output = RoutedPassage.Port(last);
        var ports = S(input, output); var route = new Relation(input, output);
        var uses = S(new Relation(a, new Relation(first, middleEnd)), new Relation(b, new Relation(middleStart, last)));
        var paths = S(new Relation(route, uses));
        var go = new Placeholder(); var alsoGo = new Placeholder(); var stop = new Placeholder();
        var law = FiniteLaw.Create(ports, S(go, alsoGo, stop), S(route),
            FiniteLaw.Row(input, go, S(route)), FiniteLaw.Row(input, alsoGo, S(route)), FiniteLaw.Row(input, stop, S()));
        var capacity = S(new Relation(input, S(new Placeholder())), new Relation(output, S(new Placeholder())));
        return new(new RoutedPassage(S(a, b), ports, paths, S(uses), capacity, law), input, output, middleEnd, go, alsoGo, stop);
    }

    private static Relation Visit(QualitativeCurve.Frame frame, Relation section, bool reverse = false)
    {
        var read = frame.Read(section); var words = E(read.ControlWords);
        return new(section, new Relation(new Relation(reverse ? read.End : read.Start, reverse ? read.Start : read.End),
            S(reverse ? words.Reverse().ToArray() : words)));
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
