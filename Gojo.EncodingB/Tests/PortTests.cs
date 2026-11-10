using System.Reflection;
using Xunit;

namespace Gojo.EncodingB.Tests;

public sealed class PortTests
{
    private static readonly Domain Command = new("command", "closed", "open", "maintenance");

    private static readonly SiteGrammar Controller = SiteGrammar.Create("controller")
        .Plane("command0", "control", "command bit 0 clear", "command bit 0 set")
        .Plane("command1", "control", "command bit 1 clear", "command bit 1 set")
        .Code("command", Command, "command0", "command1")
        .Build();

    // An inlet reads its field's own values: "gate reads closed", "gate reads open".
    private static readonly Domain GateReading = Passage.Grammar.DomainOf("open");

    // Maintenance has no gate reading: a lane commanding it is blocked, not closed.
    private static readonly RelationLaw CommandToGate = RelationLaw.FromPredicate(
        "command to gate",
        [new LawRole("from", Command), new LawRole("to", GateReading)],
        t => t[0] == t[1]);

    private static readonly PortBasis CommandBasis = new(Command, "valve command");
    private static readonly PortBasis GateBasis = new(GateReading, "valve command");

    private static ulong Word(string name) => Bits.Of(OdvWord.Parse(name).Code);

    private static Tile Valve(string name, int sites = 4)
    {
        var valve = new Tile(name, Passage.Grammar, sites);
        valve.Seed("occupied", "1010");
        return valve;
    }

    private static Tile Commands(params string[] commands)
    {
        var controller = new Tile("controller", Controller, commands.Length);
        for (var site = 0; site < commands.Length; site++) controller.SeedCode("command", site, Command.CodeOf(commands[site]));
        return controller;
    }

    [Fact]
    public void APortPublishesItsContractAndACoarseReadNotItsOwner()
    {
        var tank = new Tile("tank", TensionField.Grammar(4), 3);
        tank.SeedMagnitude("M", 2, 3);
        var epoch = tank.Epoch;
        var presence = Rungs.Presence(4);
        var level = tank.Publish("level", "M", [2, 0], new PortBasis(presence.Coarse, "litres", presence));
        Assert.Equal(epoch + 1, tank.Epoch);
        Assert.Same(level, Assert.Single(tank.Ports));

        var version = tank.Version;
        var publication = level.Read();
        Assert.Equal(new int?[] { 1, 0 }, publication.Values);
        Assert.Equal("detectable", publication.NameOf(0));
        Assert.Equal(version, tank.Version);

        // γ bounds the hidden magnitude (1 to 4 litres); nothing in the port recovers the 3.
        Assert.Equal(0b11110UL, level.Basis.Rung!.Concretize(publication.Values[0]!.Value));
        var members = typeof(Port).GetMembers(BindingFlags.Public | BindingFlags.Instance);
        Assert.DoesNotContain(members, m => m is PropertyInfo p && p.PropertyType == typeof(Tile) || m is MethodInfo f && f.ReturnType == typeof(Tile));
        Assert.Contains("litres", level.Describe());
        Assert.Contains(level.Basis.Fingerprint, level.Describe());
    }

    [Fact]
    public void WithoutADeclaredCorrespondenceAConnectionIsAnOpenSlotWithResidue()
    {
        var sensor = new Tile("sensor", Odv.Grammar, 3);
        sensor.SeedCode("word", 0, OdvWord.Parse("R+0").Code);
        sensor.SeedCode("word", 1, OdvWord.Parse("R+?").Code);
        sensor.SeedCode("word", 2, OdvWord.Parse("R-0").Code);
        var detection = Odv.Basis(2, "steps");
        var exact = Odv.Basis(1, "steps");
        Assert.Equal(detection.Domain.Values, exact.Domain.Values);
        Assert.False(detection.SameAs(exact));

        var port = sensor.Publish("reading", "word", [0, 1, 2], detection);
        var model = new Tile("model", Odv.Grammar, 3);
        var inlet = model.AddInlet("reading", "word", [0, 1, 2], exact);
        var connected = port.Connect(inlet);
        var connection = connected.Connection!;
        Assert.True(connection.IsOpen);
        Assert.StartsWith("open", connected.Read);

        var open = connection.Read();
        Assert.Equal(port.Lanes, open.Open);
        Assert.Equal(port.Lanes, open.Residue);
        Assert.All(open.Supports, support => Assert.Equal(OdvWord.Domain.Full, support));
        Assert.Contains("R+0: open", open.Describe(0));
        var view = new TrialWorkspace("model view", model, SelectionMachines.All(SelectionScope.Sites(model)));
        Assert.Equal(0UL, open.DeliverTo(view).Narrowed);
        Assert.Equal(0UL, view.Sup);

        // An identity is declared for one basis; it cannot read detection words as exact ones.
        var refused = connection.Bind(Correspondence.Identity(detection));
        Assert.False(refused.Connected);
        Assert.Contains("reads into basis", refused.Read);
        Assert.True(connection.IsOpen);

        Assert.True(connection.Bind(Correspondence.Read(Odv.ThresholdCorrespondence(2, 1), detection, exact)).Connected);
        var read = connection.Read();
        Assert.Equal([Word("R+0") | Word("R+?"), Word("R+?"), Word("R-0") | Word("R-?")], read.Supports);
        Assert.Equal(0b101UL, read.Competing);
        Assert.Equal(0b010UL, read.Settled);
        Assert.Contains("(competing candidates)", read.Describe(0));
        Assert.Equal(0b111UL, read.DeliverTo(view).Narrowed);
        Assert.Equal(Word("R+0") | Word("R+?"), view.Support("word").Values(0));
        Assert.Equal(0b111UL, view.Sup);
    }

    [Fact]
    public void ThresholdCorrespondencesKeepSignsAndOnlyWiden()
    {
        ulong Read(RelationLaw law, string word) => law.Project(1, [Word(word), OdvWord.Domain.Full]);
        var toDetection = Odv.ThresholdCorrespondence(1, 2);
        Assert.Equal(Word("R+0"), Read(toDetection, "R+0"));
        Assert.Equal(Word("R+0") | Word("R+?"), Read(toDetection, "R+?"));
        Assert.Equal(Word("L-0") | Word("L-?"), Read(toDetection, "L-?"));
        var same = Odv.ThresholdCorrespondence(2, 2);
        for (var code = 0; code < 8; code++) Assert.Equal(Bits.Of(code), same.Project(1, [Bits.Of(code), OdvWord.Domain.Full]));
    }

    [Fact]
    public void AReorientedSourceReadsTheSameThroughItsCorrespondence()
    {
        var arm = new Tile("arm", Odv.Grammar, 2);
        arm.SeedCode("word", 0, OdvWord.Parse("R+?").Code);
        arm.SeedCode("word", 1, OdvWord.Parse("R-0").Code);
        var basis = Odv.Basis(1, "steps");
        var port = arm.Publish("displacement", "word", [0, 1], basis);
        var observer = new Tile("observer", Odv.Grammar, 2);
        observer.SetBasis("O", true);
        var inlet = observer.AddInlet("displacement", "word", [0, 1], basis);
        var connection = port.Connect(inlet, Correspondence.Read(Odv.ReadIn(positiveLeft: true), basis, basis)).Connection!;

        var before = connection.Read();
        Assert.Equal([Word("L-?"), Word("L+0")], before.Supports);

        // The arm's reader turns around: every published word changes, the thing does not move.
        Odv.ReorientTile(arm);
        var published = port.Read();
        Assert.Equal(("L-?", "L+0"), (published.NameOf(0), published.NameOf(1)));
        var after = connection.Read();
        Assert.Equal(before.Supports, after.Supports);
        Assert.NotEqual(before.Publication.OwnerEpoch, after.Publication.OwnerEpoch);
    }

    [Fact]
    public void ObservationPortsFanOutWithoutConsumingTheSource()
    {
        var controller = Commands("open", "closed");
        var port = controller.Publish("commands", "command", [0, 1], CommandBasis);
        var correspondence = Correspondence.Read(CommandToGate, CommandBasis, GateBasis);
        var version = controller.Version;
        foreach (var valve in new[] { Valve("east"), Valve("west") })
        {
            var connection = port.Connect(valve.AddInlet("gates", "open", [1, 3], GateBasis), correspondence).Connection!;
            var read = connection.Read();
            Assert.Equal(0b11UL, read.Settled);
            Assert.True(connection.Commit(read).Committed);
            Assert.Equal(Bits.Parse("0100"), valve.Read("open"));

            // The supplied gates now drive the valve's own passage rows.
            Passage.Rows("handoff", [[0, 1, 1], [2, 3, 3]]).Apply(valve, 0b11);
            Assert.Equal(Bits.Parse("0110"), valve.Read("occupied"));
        }
        Assert.Equal(2, port.Connections.Count);
        Assert.Equal(version, controller.Version);
    }

    [Fact]
    public void AReadCoversSixtyFourLanesInOneNarrowing()
    {
        var controller = Commands(Enumerable.Range(0, 64).Select(site => site % 2 == 0 ? "open" : "closed").ToArray());
        var lanes = Enumerable.Range(0, 64).ToArray();
        var port = controller.Publish("commands", "command", lanes, CommandBasis);
        var valve = new Tile("bank", Passage.Grammar, 64);
        var connection = port.Connect(valve.AddInlet("gates", "open", lanes, GateBasis),
            Correspondence.Read(CommandToGate, CommandBasis, GateBasis)).Connection!;
        var read = connection.Read();
        Assert.Equal(ulong.MaxValue, read.Settled);
        Assert.True(connection.Commit(read).Committed);
        Assert.Equal(0x5555555555555555UL, valve.Read("open"));
    }

    [Fact]
    public void AGroupedCommitSuppliesEveryLaneOrNone()
    {
        var controller = Commands("open", "maintenance");
        var port = controller.Publish("commands", "command", [0, 1], CommandBasis);
        var valve = Valve("north");
        var connection = port.Connect(valve.AddInlet("gates", "open", [1, 3], GateBasis),
            Correspondence.Read(CommandToGate, CommandBasis, GateBasis)).Connection!;

        var read = connection.Read();
        Assert.Equal((0b01UL, 0b10UL, 0b10UL), (read.Settled, read.Blocked, read.Residue));
        Assert.Equal(0UL, read.Supports[1]);
        Assert.Contains("maintenance: blocked", read.Describe(1));

        var version = valve.Version;
        var refused = connection.Commit(read);
        Assert.False(refused.Committed);
        Assert.Equal(0b10UL, refused.UnsettledLanes);
        Assert.Contains("every lane or none", refused.Read);
        Assert.Equal(version, valve.Version);

        // The controller moves on; the old read is stale even though nothing it says changed shape.
        controller.Supply("command1", 1, false);
        Assert.StartsWith("stale", connection.Commit(read).Read);
        var fresh = connection.Read();
        Assert.True(connection.Commit(fresh).Committed);
        Assert.Equal(Bits.Parse("0100"), valve.Read("open"));
    }

    [Fact]
    public void ConnectionsKeepThePortsAccountingClass()
    {
        var tank = new Tile("tank", TensionField.Grammar(2), 2);
        var tension = new TensionField(tank);
        var outflow = tension.PublishMaterial("outflow", [1], "water");
        var basin = new TensionField(new Tile("basin", TensionField.Grammar(2), 1));
        var inflow = basin.MaterialInlet("inflow", [0], "water");
        var monitor = new Tile("monitor", Passage.Grammar, 1);
        var watch = monitor.AddInlet("pending", "occupied", [0], outflow.Basis);

        var read = outflow.Connect(watch, Correspondence.Identity(outflow.Basis));
        Assert.False(read.Connected);
        Assert.Contains("publish it as coupled", read.Read);

        var gauge = tank.Publish("gauge", "P", [1], outflow.Basis);
        var drain = gauge.Connect(inflow, Correspondence.Material(Passage.Law, outflow.Basis, inflow.Basis, "S", "T", "C"));
        Assert.False(drain.Connected);
        Assert.Contains("would drain it", drain.Read);

        var oil = Assert.Throws<ArgumentException>(() =>
            Correspondence.Material(Passage.Law, outflow.Basis, new PortBasis(Passage.Presence, "oil"), "S", "T", "C"));
        Assert.Contains("calibration", oil.Message);
        var copy = TransitionLaw.FromFunction("copy",
            [new LawRole("S", Passage.Presence), new LawRole("T", Passage.Presence)],
            state => state is [1, 0] ? [1, 1] : state,
            new AccountingSignature(functional: true, new NeverIncreases("S")),
            ConfluenceClass.ScheduledDescent);
        var copying = Assert.Throws<ArgumentException>(() => Correspondence.Material(copy, outflow.Basis, inflow.Basis, "S", "T"));
        Assert.Contains("not a material promise", copying.Message);
    }

    [Fact]
    public void MaterialLeavesAsTensionAndEveryUnitIsAccounted()
    {
        var tank = new Tile("tank", TensionField.Grammar(2), 2);
        var tankTension = new TensionField(tank);
        var basinTile = new Tile("basin", TensionField.Grammar(2), 1);
        var basin = new TensionField(basinTile);
        tank.SeedMagnitude("M", 1, 2);
        tankTension.Ingest(1, 2);
        var outflow = tankTension.PublishMaterial("outflow", [1], "water");
        var inflow = basin.MaterialInlet("inflow", [0], "water");
        int Total() => tankTension.Magnitude(1) + tankTension.Positive(1) + basin.Magnitude(0) + basin.Positive(0);
        Assert.Equal(4, Total());

        var connection = outflow.Connect(inflow).Connection!;
        var open = connection.Transfer();
        Assert.Equal((1UL, 0UL, 1UL), (open.Pending, open.Offered, open.Remainder));
        Assert.Equal(2, tankTension.Positive(1));
        // The receiver sees that a unit is pending, never how many.
        Assert.Equal("occupied", outflow.Read().NameOf(0));

        Assert.True(connection.Bind(Correspondence.Material(Passage.Law, outflow.Basis, inflow.Basis, "S", "T", "C")).Connected);
        var first = connection.Transfer();
        Assert.Equal((1UL, 1UL, 0UL), (first.Offered, first.Accepted, first.Blocked));
        Assert.Empty(first.HardViolations);
        Assert.Equal((1, 1), (tankTension.Positive(1), basin.Positive(0)));
        Assert.Equal(4, Total());

        // The basin absorbs on its own tick.
        basin.Absorb(basinTile.Allocated);
        connection.Transfer();
        basin.Absorb(basinTile.Allocated);
        Assert.Equal((0, 2), (tankTension.Positive(1), basin.Magnitude(0)));

        // The basin is full: the offered unit is blocked and stays with the tank as tension.
        tankTension.Ingest(1, 1);
        var blocked = connection.Transfer();
        Assert.Equal((1UL, 0UL, 1UL), (blocked.Offered, blocked.Accepted, blocked.Blocked));
        Assert.Empty(blocked.HardViolations);
        Assert.Equal(1, tankTension.Positive(1));
        Assert.Equal(5, Total());
    }

    [Fact]
    public void AMaterialPortCannotFanOutBecauseTheJointSignatureBreaks()
    {
        var tank = new Tile("tank", TensionField.Grammar(2), 2);
        var tension = new TensionField(tank);
        tension.Ingest(1, 1);
        var outflow = tension.PublishMaterial("outflow", [1], "water", PortAccounting.Coupled);
        var east = new TensionField(new Tile("east", TensionField.Grammar(2), 1)).MaterialInlet("inflow", [0], "water");
        var west = new TensionField(new Tile("west", TensionField.Grammar(2), 1)).MaterialInlet("inflow", [0], "water");
        Correspondence Flow(Inlet inlet) => Correspondence.Material(Passage.Law, outflow.Basis, inlet.Basis, "S", "T", "C");

        Assert.True(outflow.Connect(east, Flow(east)).Connected);
        var second = outflow.Connect(west, Flow(west));
        Assert.False(second.Connected);
        Assert.Contains("cannot fan out", second.Read);
        Assert.Contains("[outflow=occupied, C1=open, T1=empty, C2=open, T2=empty]", second.Read);
        Assert.Contains("sum of outflow + T1 + T2 preserved", second.Read);
        Assert.Single(outflow.Connections);

        // An open slot moves nothing, so it may exist; binding it is refused for the same reason.
        var slot = outflow.Connect(west).Connection!;
        Assert.False(slot.Bind(Flow(west)).Connected);
        Assert.True(slot.IsOpen);

        // The coupled port may still be read by any number of observers.
        for (var i = 0; i < 2; i++)
        {
            var monitor = new Tile($"monitor {i}", Passage.Grammar, 1);
            var watch = monitor.AddInlet("pending", "occupied", [0], outflow.Basis);
            var observed = outflow.Connect(watch, Correspondence.Identity(outflow.Basis)).Connection!;
            Assert.Equal(Passage.Presence.Full & Bits.Of(1), observed.Read().Supports[0]);
        }
    }
}
