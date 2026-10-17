using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeFieldAnalysisTests
{
    [Fact]
    public void RoleRelativeAbsenceKeepsBothSourceRolesAsActualParticipants()
    {
        var f = new Fixture(true, false, false, false, true); var goal = new Placeholder();
        var read = Read(QualitativeFieldAnalysis.FromLabels(f.Source, f.Ink, f.BothWays, goal));
        Assert.Equal(3, E(read.Empty).Length); Assert.Equal(2, E(read.Single).Length); Assert.Empty(E(read.Many)); Assert.Empty(E(read.Pending));
        Assert.Single(E(read.Centers)); Assert.True(Contains(read.Analysis.Surface, f.Source.Surface)); Assert.True(Contains(read.Analysis.Surface, goal));
        Assert.True(Contains(read.Analysis.Evaluate(), f.Source.Labels)); AssertNoNumbers(read.Analysis.Evaluate());
    }

    [Fact]
    public void QuietPlateauIsAnActualConnectedFarthestShellRatherThanInventedMidpoint()
    {
        var f = new Fixture(true, false, false, false, false, true);
        var read = Read(QualitativeFieldAnalysis.FromLabels(f.Source, f.Ink, f.BothWays));
        var center = Assert.IsType<Relation>(Assert.Single(E(read.Centers)));
        Assert.True(read.Analysis.Coverage.TryReadCenter(center, out var layer, out var cells));
        Assert.Equal(new[] { f.Cells[2], f.Cells[3] }, E(cells));
        Assert.True(read.Analysis.Coverage.TryReadLayer(layer, out var predecessor, out var members));
        Assert.IsType<Relation>(predecessor); Assert.Equal(E(cells), E(members));
        Assert.True(Contains(read.Analysis.Coverage.Surface, f.BothWays)); Assert.NotSame(f.Source.Adjacency, f.BothWays);
    }

    [Fact]
    public void ExplicitObserverEdgesChangeClearanceWithoutChangingSourceTopology()
    {
        var f = new Fixture(false, false, true); var outgoing = Read(QualitativeFieldAnalysis.FromLabels(f.Source, f.Ink));
        var both = Read(QualitativeFieldAnalysis.FromLabels(f.Source, f.Ink, f.BothWays));
        Assert.Empty(E(outgoing.Centers)); Assert.NotEmpty(E(outgoing.Pending));
        Assert.Single(E(both.Centers)); Assert.Empty(E(both.Pending));
        Assert.Same(f.Source.Adjacency, outgoing.Analysis.Adjacency); Assert.Same(f.BothWays, both.Analysis.Adjacency);
    }

    [Fact]
    public void MultipleDeclaredSupportsAreCrowdedWithoutClaimingIndependentSamples()
    {
        var f = new Fixture(true, false); var first = new Placeholder(); var second = new Placeholder();
        var supports = S(new Relation(f.Cells[0], S(first, second)), new Relation(f.Cells[1], S()));
        var read = Read(new QualitativeFieldAnalysis(f.Source, f.Ink, supports, f.BothWays));
        Assert.Single(E(read.Many)); Assert.Empty(E(read.Single)); Assert.True(Contains(read.Many, first)); Assert.True(Contains(read.Many, second));
        Assert.Empty(E(read.Pending));
        var repeated = Read(new QualitativeFieldAnalysis(f.Source, f.Ink,
            S(new Relation(f.Cells[0], S(first, first)), new Relation(f.Cells[1], S())), f.BothWays));
        Assert.Single(E(repeated.Single)); Assert.Empty(E(repeated.Many));
    }

    [Fact]
    public void PassSupportsExposeCrowdingForActualOverlappingContributions()
    {
        var f = new Fixture(true, false, false);
        var tool = new QualitativePartition(f.Source.Cells, f.Source.Adjacency, f.Source.RolePair,
            S(new Relation(f.Cells[0], f.Ink), new Relation(f.Cells[1], f.Ink), new Relation(f.Cells[2], f.Other)));
        var pass = new QualitativeFieldPass(f.Source, tool, QualitativeFieldPass.UnionLaw(f.Source.RolePair, f.Ink));
        var analysis = Read(new QualitativeFieldAnalysis(pass.Result, f.Ink, pass.Supports(f.Ink), f.BothWays));
        Assert.Single(E(analysis.Many)); Assert.Single(E(analysis.Single)); Assert.Single(E(analysis.Empty));
        Assert.True(Contains(analysis.Many, E(f.Source.Labels)[0])); Assert.True(Contains(analysis.Many, E(tool.Labels)[0]));
    }

    [Fact]
    public void MissingOrOpenSupportsCannotConvertAnOccupiedRoleToAbsence()
    {
        var f = new Fixture(true, false);
        foreach (var supports in new[] { S(new Relation(f.Cells[1], S())),
            S(new Relation(f.Cells[0], StructuralConstraint.Open(new Placeholder())), new Relation(f.Cells[1], S())),
            S(new Relation(f.Cells[0], S()), new Relation(f.Cells[1], S())) })
        {
            var read = Read(new QualitativeFieldAnalysis(f.Source, f.Ink, supports, f.BothWays));
            Assert.Single(E(read.Empty)); Assert.Empty(E(read.Single)); Assert.Empty(E(read.Many)); Assert.NotEmpty(E(read.Pending));
            Assert.DoesNotContain(E(read.Empty).OfType<Relation>(), row => ReferenceEquals(row.Left, f.Cells[0]));
        }
    }

    [Fact]
    public void OtherRoleSupportConflictStaysUnknownInsteadOfOverridingPartition()
    {
        var f = new Fixture(true, false); var witness = new Placeholder();
        var read = Read(new QualitativeFieldAnalysis(f.Source, f.Ink,
            S(new Relation(f.Cells[0], S(witness)), new Relation(f.Cells[1], S(witness))), f.BothWays));
        Assert.Empty(E(read.Empty)); Assert.Single(E(read.Single)); Assert.NotEmpty(E(read.Pending));
    }

    [Fact]
    public void OpenAndMissingSourceLabelsRemainUnknownForEitherSelectedRole()
    {
        var f = new Fixture(true, false); var source = new QualitativePartition(f.Source.Cells, f.Source.Adjacency,
            f.Source.RolePair, S(E(f.Source.Labels)[0]));
        var read = Read(QualitativeFieldAnalysis.FromLabels(source, f.Ink, f.BothWays));
        Assert.Single(E(read.Single)); Assert.Empty(E(read.Empty)); Assert.NotEmpty(E(read.Pending));
        var open = new QualitativePartition(f.Source.Cells, f.Source.Adjacency, f.Source.RolePair, StructuralConstraint.Open(E(f.Source.Labels)));
        var unresolved = Read(QualitativeFieldAnalysis.FromLabels(open, f.Other, f.BothWays));
        Assert.Empty(E(unresolved.Empty)); Assert.Empty(E(unresolved.Single)); Assert.NotEmpty(E(unresolved.Pending));
    }

    [Fact]
    public void AllEmptySelectedRoleHasNoInventedClearanceOriginOrCenter()
    {
        var f = new Fixture(false, false, false); var read = Read(QualitativeFieldAnalysis.FromLabels(f.Source, f.Ink, f.BothWays));
        Assert.Equal(3, E(read.Empty).Length); Assert.Empty(E(read.Layers)); Assert.Empty(E(read.Centers)); Assert.NotEmpty(E(read.Pending));
    }

    [Fact]
    public void ForeignRoleOpenOuterSupportAndDuplicateRowsDoNotCertifyOccupancy()
    {
        var f = new Fixture(true, false); var rows = new[] { new Relation(f.Cells[0], S(new Placeholder())), new Relation(f.Cells[1], S()) };
        var open = Read(new QualitativeFieldAnalysis(f.Source, f.Ink, StructuralConstraint.Open(rows), f.BothWays));
        Assert.Empty(E(open.Empty)); Assert.Empty(E(open.Single)); Assert.NotEmpty(E(open.Pending));
        var duplicate = Read(new QualitativeFieldAnalysis(f.Source, f.Ink, S(rows[0], rows[0], rows[1]), f.BothWays));
        Assert.Empty(E(duplicate.Single)); Assert.NotEmpty(E(duplicate.Pending));
        var foreign = Read(QualitativeFieldAnalysis.FromLabels(f.Source, new Placeholder(), f.BothWays));
        Assert.Empty(E(foreign.Empty)); Assert.Empty(E(foreign.Single)); Assert.NotEmpty(E(foreign.Pending));
    }

    [Fact]
    public void ReadersRejectChangedProofAndOtherAnalysisFrames()
    {
        var f = new Fixture(true, false); var analysis = QualitativeFieldAnalysis.FromLabels(f.Source, f.Ink, f.BothWays);
        var other = QualitativeFieldAnalysis.FromLabels(f.Source, f.Ink, f.BothWays);
        Assert.False(analysis.TryReadResult(other.Evaluate(), out _, out _, out _, out _, out _, out _));
        var forged = new Relation(analysis.Surface, new Relation(new Relation(analysis.SourceRead, analysis.CoverageRead), S()));
        Assert.False(analysis.TryReadResult(forged, out _, out _, out _, out _, out _, out _));
        Assert.True(analysis.TryReadResult(new Relation(analysis.Surface, analysis.Evaluate().Right), out _, out _, out _, out _, out _, out _));
    }

    private static (QualitativeFieldAnalysis Analysis, Relation Empty, Relation Single, Relation Many, Relation Layers, Relation Centers, Relation Pending) Read(QualitativeFieldAnalysis analysis)
    {
        Assert.True(analysis.TryReadResult(analysis.Evaluate(), out var empty, out var one, out var many, out var layers, out var centers, out var pending));
        return (analysis, empty, one, many, layers, centers, pending);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries; }
    private static bool Contains(IElement element, IElement item) => ReferenceEquals(element, item) ||
        element is Relation relation && (Contains(relation.Left, item) || Contains(relation.Right, item));
    private static void AssertNoNumbers(IElement element)
    { Assert.False(element is Carrier or CarrierLocation); if (element is Relation relation) { AssertNoNumbers(relation.Left); AssertNoNumbers(relation.Right); } }
    private sealed class Fixture
    {
        public IElement Ink { get; } = new Placeholder(); public IElement Other { get; } = new Placeholder();
        public IElement[] Cells { get; } public QualitativePartition Source { get; } public Relation BothWays { get; }
        public Fixture(params bool[] ink)
        {
            Cells = ink.Select(_ => (IElement)new Placeholder()).ToArray();
            var edges = Cells.Zip(Cells.Skip(1), (a, b) => new Relation(a, b)).ToArray();
            var adjacency = S(edges); BothWays = S(edges.SelectMany(edge => new IElement[] { edge, edge.Reverse() }).ToArray());
            Source = new QualitativePartition(S(Cells), adjacency, new Relation(Ink, Other),
                S(Cells.Select((cell, i) => (IElement)new Relation(cell, ink[i] ? Ink : Other)).ToArray()));
        }
    }
}
