using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Information tension in the planes: folding any <see cref="Supports"/>
/// width into two presence bits <c>(seen1, seen2)</c> and masking by a demand
/// plane so openness without demand creates no tension.</summary>
public sealed class TensionInformationTests
{
    /// <summary><see cref="Tension.ObserveSupports"/> scales cleanly across
    /// 2-value, 8-value (ODV) and 16-value supports without hitting
    /// <see cref="Law.MaxBits"/>, distinguishing contradiction <c>(0,0)</c>,
    /// settled <c>(1,0)</c> and open <c>(1,1)</c>.</summary>
    [Fact]
    public void SupportFoldScalesAcrossBinaryOdvAndWideSupports()
    {
        foreach (var size in new[] { 2, 8, 16 })
        {
            var shape = Shape.Binary(size);
            var store = new Store(shape.Bits, 3);
            var field = new Field(shape, Enumerable.Range(0, shape.Bits).ToArray());
            var supports = Supports.On(store, field);
            // Pos 0: open (all possible); Pos 1: settled (only value 1); Pos 2: empty (contradiction).
            supports.Restrict(1, 1UL << 1);
            supports.Restrict(2, 0UL);

            var status = new Store(5, 3); // 0 demand, 1 seen1, 2 seen2, 3 deficit, 4 excess
            for (var p = 0; p < 3; p++) status.Write(0, p, true);
            var engine = new Engine();
            Tension.ObserveSupports(engine, supports, status, 0, status, seen1: 1, seen2: 2, deficit: 3, excess: 4);

            engine.Step();

            // Open -> (seen1=1, seen2=1) -> deficit=1, excess=0.
            Assert.Equal((true, true, true, false), (status.Read(1, 0), status.Read(2, 0), status.Read(3, 0), status.Read(4, 0)));
            // Settled -> (seen1=1, seen2=0) -> deficit=0, excess=0.
            Assert.Equal((true, false, false, false), (status.Read(1, 1), status.Read(2, 1), status.Read(3, 1), status.Read(4, 1)));
            // Contradiction -> (seen1=0, seen2=0) -> deficit=0, excess=1.
            Assert.Equal((false, false, false, true), (status.Read(1, 2), status.Read(2, 2), status.Read(3, 2), status.Read(4, 2)));
        }
    }

    /// <summary>Openness is room, not strain: an undemanded open or empty
    /// position raises zero tension. Setting the demand bit lights deficit or
    /// excess, and another law in the same engine reacts to it. The whole
    /// machine saves and restores via <see cref="Transport"/>.</summary>
    [Fact]
    public void OpennessCreatesNoTensionUntilDemandedAndLawsReactToIt()
    {
        var store = new Store(2, 2);
        var field = new Field(Shape.Binary(4), 0, 1);
        var supports = Supports.On(store, field);
        supports.Restrict(1, 0UL); // pos 0 open, pos 1 contradicted

        // status: 0 demand, 1 seen1, 2 seen2, 3 deficit, 4 excess, 5 alarm
        var status = new Store(6, 2);
        var engine = new Engine();
        Tension.ObserveSupports(engine, supports, status, 0, status, 1, 2, 3, 4);

        var alarmLaw = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Bit, Shape.Bit],
            s => [s[0], s[1], s[0] | s[1]],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var alarm = new Wiring(alarmLaw, new Binding(status, 3), new Binding(status, 4), new Binding(status, 5));
        alarm.Add(0, 0, 0);
        alarm.Add(1, 1, 1);
        engine.Add(alarm);

        // Undemanded: neither deficit nor excess fires, and alarm stays clear.
        engine.Step();
        Assert.True(status.Read(3).None && status.Read(4).None && status.Read(5).None);

        // Now demand both positions and verify both original and restored machines react.
        status.Write(0, 0, true);
        status.Write(0, 1, true);
        var (restored, copies) = Transport.Load(Transport.Save(engine, store, status));

        engine.Step();
        restored.Step();

        Assert.Equal(Plane.Of(2, 0), status.Read(3)); // pos 0 deficit
        Assert.Equal(Plane.Of(2, 1), status.Read(4)); // pos 1 excess
        Assert.Equal(Plane.Of(2, 0, 1), status.Read(5));
        Assert.Equal(status.Read(5), copies[1].Read(5));
    }

    /// <summary>Granularity belongs to the read: a coarse question ("is count
    /// above 2?") settles to one answer when the fine value is still open on
    /// {3, 4, 5}, so information tension on the demanded coarse read is zero
    /// while the undemanded fine value stays open.</summary>
    [Fact]
    public void CoarseDemandedReadSettlesWithZeroTensionWhileFineValueStaysOpen()
    {
        var store = new Store(6, 1);
        var fine = new Field(Shape.Unary(5), 0, 1, 2, 3, 4);
        var coarse = Field.Bit(5);
        var fineSup = Supports.On(store, fine);
        var coarseSup = Supports.On(store, coarse);
        fineSup.Restrict(0, (1UL << 3) | (1UL << 4) | (1UL << 5));

        var aboveTwo = RelationLaw.FromPredicate([fine.Shape, coarse.Shape], t => t[1] == (t[0] > 2 ? 1 : 0));
        var project = new Wiring(aboveTwo, fineSup.Variable, coarseSup.Variable);
        project.Add(0, 0);

        var status = new Store(5, 1);
        status.Write(0, 0, true); // demand sits on the coarse read only
        var engine = new Engine().Add(project);
        Tension.ObserveSupports(engine, coarseSup, status, 0, status, 1, 2, 3, 4);

        engine.Step();

        Assert.Equal((1UL << 3) | (1UL << 4) | (1UL << 5), fineSup.Values(0)); // fine stays open
        Assert.Equal(1UL << 1, coarseSup.Values(0));                           // coarse is settled
        Assert.False(status.Read(3, 0));                                       // zero deficit
        Assert.False(status.Read(4, 0));                                       // zero excess
    }

    /// <summary>Regression test for <c>Wiring.SameReference</c>: two open roles
    /// sharing only plane 0 while differing on plane 1 are not the same value
    /// and must not be forced equal.</summary>
    [Fact]
    public void OpenRolesSharingOnlyFirstPlaneAreNotTreatedAsSameReference()
    {
        var store = new Store(3, 1);
        // Planes 0, 1, 2 all start filled (1 = possible).
        for (var p = 0; p < 3; p++) store.Write(p, 0, true);
        var xor = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit, Shape.Bit], t => t[2] == (t[0] ^ t[1]));
        var outStore = new Store(1, 1);
        var outSup = Supports.On(outStore, Field.Bit(0));

        // Role 0 binds planes (0, 1); role 1 binds planes (0, 2) -> share plane 0 only, distinct plane 1 vs 2.
        var wiring = new Wiring(xor, Binding.Open(store, 0, 1), Binding.Open(store, 0, 2), outSup.Variable);
        wiring.Add(0, 0, 0);
        wiring.Apply();

        // Because they are distinct bindings, XOR output stays {0, 1} rather than collapsing to {0}.
        Assert.Equal(0b11UL, outSup.Values(0));
    }
}
