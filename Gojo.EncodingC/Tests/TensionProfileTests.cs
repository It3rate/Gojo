using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Uniform tension profiles, structural reads (duplicates, co-moving
/// positions, streaks) and strict vs Ashby viability-threshold admissibility.</summary>
public sealed class TensionProfileTests
{
    /// <summary><see cref="Tension.Measure"/> reads every tier from ordinary
    /// store planes and evidence without netting <c>T+</c> and <c>T−</c>.</summary>
    [Fact]
    public void TensionProfileReadsEveryTierFromPlanesWithoutNettingMaterial()
    {
        var site = new Store(9, 2);
        var value = new Field(Shape.Unary(3), 0, 1, 2);
        var plus = new Field(Shape.Unary(3), 3, 4, 5);
        var minus = new Field(Shape.Unary(3), 6, 7, 8);
        value.Write(site, 0, 1);
        plus.Write(site, 0, 2);
        minus.Write(site, 0, 1);
        site.Write(1, 1, true); // broken unary 010 at pos 1 -> Hard = 1

        var info = new Store(1, 2);
        var sup = Supports.On(info, Field.Bit(0));
        sup.Restrict(1, 0UL); // pos 0 open, pos 1 contradicted

        // Contested pair on a small source.
        var race = new Store(1, 3);
        race.Write(0, 0, true);
        var move = new Wiring(Flows.Move(1, 1), new Binding(race, 0), new Binding(race, 0));
        move.Add(0, 1);
        move.Add(0, 2);
        move.Record();

        var engine = new Engine().Hold(site, info).Add(move);
        engine.StepTogether();

        var profile = Tension.Measure(
            engine,
            sites: [new TensionSite(site, value, plus, minus)],
            demands: [new TensionDemand(sup, Plane.Ones(2))]);

        Assert.Equal(1, profile.Hard);
        Assert.Equal(1, profile.Obligation);     // pos 1 empty under demand
        Assert.Equal(2, profile.MaterialPlus);   // never netted with MaterialMinus
        Assert.Equal(1, profile.MaterialMinus);
        Assert.Equal(3, profile.MaterialTotal);
        Assert.Equal(1, profile.Goal);           // pos 0 open under demand
        Assert.Equal(2, profile.Contention);     // both racing rows contested
        Assert.True(profile.DescriptionBits > 0);
        Assert.False(profile.IsQuiet);
    }

    /// <summary>Concrete comparison of strict Optimality Theory ranking vs
    /// Ashby viability thresholds: lossy crystallization that trims structural
    /// redundancy and description bits while raising coarse openness by 1
    /// inside its viability bound is refused by <see cref="Tension.AdmissibleStrict"/>
    /// and admitted by <see cref="Tension.AdmissibleViable"/>. Breaking
    /// <c>Hard</c> is refused by both.</summary>
    [Fact]
    public void ViabilityThresholdsAdmitLossyCrystallizationThatStrictRankingRefuses()
    {
        var verbose = new TensionProfile(Hard: 0, Obligation: 0, MaterialPlus: 0, MaterialMinus: 0, Goal: 0, Contention: 0, StructuralDeficit: 0, StructuralExcess: 2, DescriptionBits: 320);
        var crystallized = new TensionProfile(Hard: 0, Obligation: 0, MaterialPlus: 0, MaterialMinus: 0, Goal: 1, Contention: 0, StructuralDeficit: 0, StructuralExcess: 0, DescriptionBits: 140);
        var broken = crystallized with { Hard = 1 };

        var bounds = new TensionBounds(Goal: 1);

        Assert.False(Tension.AdmissibleStrict(verbose, crystallized).Admitted);
        var viable = Tension.AdmissibleViable(verbose, crystallized, bounds);
        Assert.True(viable.Admitted);
        Assert.True(viable.StrictlyLowered);
        Assert.Equal(0, viable.LoanUsed);

        // Hard accounting faults are never admissible under either rule.
        Assert.False(Tension.AdmissibleStrict(verbose, broken).Admitted);
        Assert.False(Tension.AdmissibleViable(verbose, broken, bounds, loanBudget: 10).Admitted);
    }

    /// <summary>A temporary rise above a viability bound is refused with no
    /// loan budget and admitted with an explicit loan budget that accounts for
    /// the exact overshoot used.</summary>
    [Fact]
    public void ExplicitTensionLoanAccountsForTemporaryOvershoot()
    {
        var before = new TensionProfile(0, 0, MaterialPlus: 1, MaterialMinus: 0, 0, 0, 0, 0, 100);
        var after = new TensionProfile(0, 0, MaterialPlus: 2, MaterialMinus: 0, 0, 0, 0, 0, 100);
        var bounds = new TensionBounds(MaterialPlus: 1);

        Assert.False(Tension.AdmissibleViable(before, after, bounds, loanBudget: 0).Admitted);
        var loaned = Tension.AdmissibleViable(before, after, bounds, loanBudget: 1);
        Assert.True(loaned.Admitted);
        Assert.Equal(1, loaned.LoanUsed);
        Assert.False(loaned.StrictlyLowered);
    }

    /// <summary>Structural reads detect duplicate rows and positions whose
    /// bits move together across a window of snapshots.</summary>
    [Fact]
    public void StructuralReadsDetectDuplicateRowsAndCoMovingPositions()
    {
        var store = new Store(1, 3);
        var toggle = new Wiring(Flows.Toggle, new Binding(store, 0));
        toggle.Add(0);
        toggle.Add(1);

        var same = new Wiring(Settling.Same(Shape.Bit), new Binding(store, 0), new Binding(store, 0));
        same.Add(0, 1);
        same.Add(0, 1); // duplicate of row 0

        var engine = new Engine().Add(toggle).Add(same);
        var dups = Tension.Duplicates(engine);
        var dup = Assert.Single(dups);
        Assert.Equal((0, 1), (dup.FirstRow, dup.DuplicateRowIndex));

        // Capture 4 snapshots: positions 0 and 1 always toggle identically, position 2 stays 0.
        var history = new List<Plane[]>();
        for (var i = 0; i < 4; i++)
        {
            history.Add(Tension.Snapshot(store));
            engine.Step();
        }
        var joined = Tension.Joined(store, history);
        Assert.Equal([(0, 1)], joined);

        var profile = Tension.Measure(engine, histories: [(store, history)]);
        Assert.Equal(2, profile.StructuralExcess); // 1 duplicate row + 1 joined pair
    }
}
