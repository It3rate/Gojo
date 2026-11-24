using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Slow-clock structural adaptation driven by tension: recruiting
/// spare routes on persistent material tension, retiring idle rows via
/// <see cref="Fork"/> and admissibility, resolving contradictions inside a
/// <see cref="Closure"/> cone by firmness, and splitting/fusing using
/// <see cref="Closure.Common"/> and <see cref="Engine.Fuse"/>.</summary>
public sealed class TensionAdaptationTests
{
    /// <summary>A primary target saturates while <c>T+</c> persists on the
    /// source. A slow-clock <see cref="TensionLaws.Streak"/> counts consecutive
    /// steps with <c>T+ &gt; 0</c> and <see cref="TensionLaws.RecruitAt"/>
    /// recruits a dormant alternate route via <c>Rows.Life</c>. Independently,
    /// <see cref="Closure.Of"/> localizes the tensioned cone for a safe
    /// <see cref="Fork"/> trial before committing.</summary>
    [Fact]
    public void PersistentMaterialTensionRecruitsSpareRouteAndLocalizesViaClosure()
    {
        // Positions: 0 source, 1 primary target (cap 1), 2 spare target (cap 2), 3 unrelated site.
        // Planes: 0..1 value (cap 2), 2..3 T+ (cap 2), 4..5 persistence streak (cap 2).
        var store = new Store(6, 4);
        var value = new Field(Shape.Unary(2), 0, 1);
        var plus = new Field(Shape.Unary(2), 2, 3);
        var persist = new Field(Shape.Unary(2), 4, 5);
        value.Write(store, 0, 1);
        value.Write(store, 1, 1); // primary target already full (plane 0 = 1)
        plus.Write(store, 0, 2);  // T+ = 2 waiting on source

        var routes = new Wiring(Flows.Move(2, 1), new Binding(store, 0, 1), new Binding(store, 0));
        routes.Add(0, 1);       // row 0: blocked primary route
        routes.AddSpare(0, 2);  // row 1: dormant alternate route

        var absorb = Tension.Absorb(store, plus, value);
        absorb.Add(0, 0);

        // Unrelated wiring touching only position 3 (must stay outside the tension cone).
        var unrelated = new Wiring(Flows.Toggle, new Binding(store, 0));
        unrelated.Add(3);

        // First: verify Closure.Of seeded at the tensioned source excludes unrelated position 3.
        var probeEngine = new Engine().Add(routes).Add(absorb).Add(unrelated);
        var cone = Closure.Of(probeEngine, new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance) { [store] = Plane.Of(4, 0) }, reach: 1);
        Assert.True(cone.Of(store)[0] && cone.Of(store)[1]);
        Assert.False(cone.Of(store)[3]);

        // Trial recruiting row 1 inside a Fork and check strict admissibility before changing parent.
        var fork = probeEngine.Fork();
        var forkSite = new TensionSite(fork.Of(store), value, plus, null);
        var beforeFork = Tension.Measure(fork.Engine, sites: [forkSite]);
        fork.Of(routes).Rows.Recruit(1);
        fork.Engine.Settle(8);
        var afterFork = Tension.Measure(fork.Engine, sites: [forkSite]);
        Assert.Equal(2, beforeFork.MaterialPlus);
        Assert.Equal(0, afterFork.MaterialPlus);          // fork absorbed T+ cleanly
        Assert.Equal(2, plus.Read(store, 0));             // parent untouched
        Assert.True(Tension.AdmissibleStrict(beforeFork, afterFork).Admitted);

        // Now wire the self-running slow clock: streak watches T+ bit 0 (plane 2), RecruitAt(2, 2) wakes row 1.
        var countPersist = new Wiring(TensionLaws.Streak(2, countWhenSet: true), new Binding(store, 2), new Binding(store, 4, 5));
        countPersist.Add(0, 0);
        var recruit = new Wiring(TensionLaws.RecruitAt(2, 2), new Binding(store, 4, 5), new Binding(routes.Rows.Life, 0));
        recruit.Add(0, 1);
        probeEngine.Add(countPersist).Add(recruit);

        // Step 1: value absorbs 1 from T+ (value 1->2, T+ 2->1), persist -> 1 (still dormant).
        probeEngine.Step();
        Assert.False(routes.Live[1]);
        Assert.Equal((2, 1, 1), (value.Read(store, 0), plus.Read(store, 0), persist.Read(store, 0)));

        // Step 2: value full (2), T+ still 1 -> persist reaches 2 -> RecruitAt wakes row 1!
        probeEngine.Step();
        Assert.True(routes.Live[1]);

        // Step 3: alternate route drains source into pos 2 and T+ absorbs to 0!
        probeEngine.Step();
        Assert.Equal(0, plus.Read(store, 0));
        Assert.Equal(1, value.Read(store, 2));
    }

    /// <summary>A row that never changes anything saturates its idle streak and
    /// is retired on the slow clock—and verified admissible in a
    /// <see cref="Fork"/> (fewer active bits, zero rise in any tension tier).</summary>
    [Fact]
    public void IdleRowSaturatesAndIsRetiredSafely()
    {
        var store = new Store(1, 4);
        store.Write(0, 0, true); // active unit on 0 -> 1; positions 2 -> 3 are permanently empty
        var move = new Wiring(Flows.Move(1, 1), new Binding(store, 0), new Binding(store, 0));
        move.Add(0, 1);
        move.Add(2, 3);
        var ev = move.Record();

        // Keep row 0 busy by bouncing the unit back from 1 -> 0 on each step.
        var back = new Wiring(Flows.Move(1, 1), new Binding(store, 0), new Binding(store, 0));
        back.Add(1, 0);

        var idleStore = new Store(3, 2); // unary idle streak cap 3 per row of move
        var idleField = new Field(Shape.Unary(3), 0, 1, 2);
        var trackIdle = new Wiring(TensionLaws.Streak(3, countWhenSet: false), new Binding(move.Rows, ev.Changed), new Binding(idleStore, 0, 1, 2));
        trackIdle.Add(0, 0);
        trackIdle.Add(1, 1);

        var engine = new Engine().Add(move).Add(back).Add(trackIdle);
        engine.Run(3);

        Assert.Equal(0, idleField.Read(idleStore, 0)); // row 0 stayed busy
        Assert.Equal(3, idleField.Read(idleStore, 1)); // row 1 saturated idle

        // Verify retiring row 1 in a Fork is strictly admissible on the same ActiveBits scale.
        var fork = engine.Fork();
        var forkIdle = new StreakWatch(fork.Of(idleStore), idleField, 3);
        var beforeFork = Tension.Measure(fork.Engine, idle: [forkIdle]);
        fork.Of(move).Retire(1);
        var afterFork = Tension.Measure(fork.Engine, idle: [forkIdle]);
        Assert.True(afterFork.DescriptionBits < beforeFork.DescriptionBits);
        Assert.True(Tension.AdmissibleStrict(beforeFork, afterFork).Admitted);

        // And self-running RetireAt retires row 1 directly via Rows.Life.
        var prune = new Wiring(TensionLaws.RetireAt(3, 3), new Binding(idleStore, 0, 1, 2), new Binding(move.Rows.Life, 0));
        prune.Add(0, 0);
        prune.Add(1, 1);
        engine.Add(prune);
        engine.Step();

        Assert.True(move.Live[0]);
        Assert.True(move.Rows.IsRetired(1));
    }

    /// <summary>AGM-style firmness resolution inside a contradiction cone: a
    /// tentative assumption (firmness 0) conflicts with a hard observation
    /// (firmness 3). Backward <see cref="Closure.Of"/> isolates the conflict
    /// cone, and <see cref="TensionLaws.YieldByFirmness"/> retires only the
    /// weaker assumption so re-narrowing settles cleanly. At equal firmness
    /// neither yields and the contradiction remains visible.</summary>
    [Fact]
    public void FirmnessResolvesContradictionWithoutTouchingHardObservation()
    {
        // Value store: pos 0 target bit with supports; pos 1 unrelated bit.
        var world = new Store(1, 2);
        var sup = Supports.On(world, Field.Bit(0));

        // Two constant claim samples: pos 0 = 1 (observation), pos 1 = 0 (tentative assumption).
        var claims = new Store(1, 2);
        claims.Write(0, 0, true);
        claims.Write(0, 1, false);

        // Firmness store (unary cap 3): pos 0 firmness 3 (hard observation), pos 1 firmness 0 (tentative).
        var firmness = new Store(3, 2);
        var firmField = new Field(Shape.Unary(3), 0, 1, 2);
        firmField.Write(firmness, 0, 3);
        firmField.Write(firmness, 1, 0);

        var observe = new Wiring(Settling.Same(Shape.Bit), new Binding(claims, 0), sup.Variable);
        observe.Add(0, 0); // row 0: hard observation (1)
        var assume = new Wiring(Settling.Same(Shape.Bit), new Binding(claims, 0), sup.Variable);
        assume.Add(1, 0);  // row 0: tentative assumption (0)

        var status = new Store(5, 2); // 0 demand, 1 seen1, 2 seen2, 3 deficit, 4 excess (contradiction)
        status.Write(0, 0, true);

        var engine = new Engine().Add(observe).Add(assume);
        Tension.ObserveSupports(engine, sup, status, 0, status, 1, 2, 3, 4, positions: 0);

        // Localize conflict cone from pos 0: includes observe & assume, excludes unrelated pos 1.
        var cone = Closure.Of(engine, new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance) { [world] = Plane.Of(2, 0) });
        Assert.True(cone.Rows(observe)[0] && cone.Rows(assume)[0]);
        Assert.False(cone.Of(world)[1]);

        // Step 1: contradiction arises and lights status.excess (plane 4).
        engine.Step();
        Assert.True(status.Read(4, 0));
        Assert.Equal(0UL, sup.Values(0));

        // Wire symmetric YieldByFirmness to both rows' Life surfaces, then Forget + re-narrow.
        var yieldObs = new Wiring(TensionLaws.YieldByFirmness(3),
            new Binding(status, 4), new Binding(firmness, 0, 1, 2), new Binding(firmness, 0, 1, 2), new Binding(observe.Rows.Life, 0));
        yieldObs.Add(0, 0, 1, 0);
        var yieldAsm = new Wiring(TensionLaws.YieldByFirmness(3),
            new Binding(status, 4), new Binding(firmness, 0, 1, 2), new Binding(firmness, 0, 1, 2), new Binding(assume.Rows.Life, 0));
        yieldAsm.Add(0, 1, 0, 0);
        engine.Add(yieldObs).Add(yieldAsm);

        engine.Step();
        Assert.True(observe.Live[0]);            // hard observation survived
        Assert.True(assume.Rows.IsRetired(0));   // weaker assumption yielded

        // Reopen and re-settle: value settles cleanly to 1 with zero contradiction.
        sup.Forget(0);
        engine.Step();
        Assert.Equal(0b10UL, sup.Values(0));
        Assert.False(status.Read(4, 0));

        // Equal firmness negative test: neither side yields.
        var tied = new Store(3, 2);
        firmField.Write(tied, 0, 2);
        firmField.Write(tied, 1, 2);
        var active = new Store(1, 2);
        active.Write(0, 0, true);
        active.Write(0, 1, true);
        var contra = new Store(1, 1);
        contra.Write(0, 0, true);
        var tieCheck = new Wiring(TensionLaws.YieldByFirmness(3),
            new Binding(contra, 0), new Binding(tied, 0, 1, 2), new Binding(tied, 0, 1, 2), new Binding(active, 0));
        tieCheck.Add(0, 0, 1, 0);
        tieCheck.Add(0, 1, 0, 1);
        tieCheck.Apply();
        Assert.True(active.Read(0, 0) && active.Read(0, 1));
    }

    /// <summary><see cref="Closure.Common"/> scores drop off at the boundary
    /// between a shared referent and private structure (a candidate split cut),
    /// while <see cref="Engine.Fuse"/> combines tightly coupled structures with
    /// lower combined description bits and zero tension rise.</summary>
    [Fact]
    public void ClosureCommonFindsCutBoundaryAndFuseReducesDescriptionBits()
    {
        // Shared referent store owned by structure A, exposed through a port to structure B.
        var shared = new Store(1, 3); // pos 0 shared referent, pos 1 private to A
        var supA = Supports.On(shared, Field.Bit(0));
        var linkA = new Wiring(Settling.Same(Shape.Bit), supA.Variable, supA.Variable);
        linkA.Add(0, 1);
        var engineA = new Engine().Add(linkA);
        var port = shared.Publish([0], [.. supA.Planes]);

        var storeB = new Store(1, 2); // pos 0 private to B
        var supB = Supports.On(storeB, Field.Bit(0));
        var socket = Socket.For(port);
        socket.Plug(port);
        var linkB = new Wiring(Settling.Same(Shape.Bit), Binding.Open(socket, 0, 1), supB.Variable);
        linkB.Add(0, 0);
        var engineB = new Engine().Add(linkB);

        // Closure.Common across both uses: shared pos 0 has score 2, private positions score 1.
        var common = Closure.Common([engineA, engineB], new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance) { [shared] = Plane.Of(3, 0) });
        Assert.Equal(2, common.Score(shared, 0));
        Assert.Equal(1, common.Score(shared, 1));
        Assert.Equal(1, common.Score(storeB, 0));

        // Compare combined saved bits when separate vs fused (shared Same law saved once instead of twice).
        var separateBits = Transport.Measure(engineA).Bits + Transport.Measure(engineB).Bits;
        engineA.Fuse(engineB);
        var fusedProfile = Tension.Measure(engineA);

        Assert.True(fusedProfile.DescriptionBits < separateBits);
        Assert.True(fusedProfile.IsQuiet);
    }
}
