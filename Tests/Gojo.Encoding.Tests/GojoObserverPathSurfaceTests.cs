using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoObserverPathSurfaceTests
{
    [Fact]
    public void PathRecordsObserverStepsAndResultAsReadableStructure()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var observer = GojoObserverPathSurface.ObserverSurface(
            new Carrier(),
            source,
            Label("left endpoint read"),
            Relation.Segment(new Carrier(), 0, 1));
        var result = source.Left;
        var step = GojoObserverPathSurface.Step(
            new Carrier(),
            source,
            Label("left"),
            result,
            source);

        var path = GojoObserverPathSurface.Create(
            new Carrier(),
            source,
            observer,
            GojoObserverPathSurface.StepFrame(new Carrier(), step),
            result);

        Assert.True(GojoObserverPathSurface.TryRead(path, out var read));
        Assert.Same(source, read.SelectedSource);
        Assert.Same(observer, read.ObserverSurface);
        Assert.Same(result, read.Result);
        var readStep = Assert.Single(read.Steps);
        Assert.True(GojoObserverPathSurface.TryReadStep(readStep, out var stepRead));
        Assert.Same(source, stepRead.From);
        Assert.Same(result, stepRead.To);
        Assert.True(EncodedTextSurface.TryRead(stepRead.Through, out var through));
        Assert.Equal("left", through);
    }

    [Fact]
    public void PathProjectsToCommonReadWitness()
    {
        var source = Relation.Segment(new Carrier(), 0, 8);
        var result = source.Right;
        var path = GojoObserverPathSurface.Create(
            new Carrier(),
            source,
            GojoObserverPathSurface.ObserverSurface(new Carrier(), source, Label("right endpoint read")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(new Carrier(), source, Label("right"), result)),
            result,
            provenance: Label("test path"));

        Assert.True(GojoObserverPathSurface.TryCreateWitness(new Carrier(), path, out var witness));
        Assert.True(ReadWitness.TryRead(
            witness,
            out var selected,
            out var readSurface,
            out var witnessResult,
            out _,
            out var provenance));
        Assert.Same(source, selected);
        Assert.Same(path, readSurface);
        Assert.Same(result, witnessResult);
        Assert.True(EncodedTextSurface.TryRead(provenance, out var provenanceText));
        Assert.Equal("test path", provenanceText);
    }

    [Fact]
    public void TensionCanBeStoredAsObservedResidueWithoutBecomingCanonicalState()
    {
        var phase = new Carrier();
        var wave = Relation.Segment(phase, 0, 360);
        var expectedSurface = GojoObserverPathSurface.ObserverSurface(
            new Carrier(),
            wave,
            Label("expected sine sample"),
            Relation.Segment(phase, 0, 360),
            Label("compare one sample"));
        var observedResult = Relation.Point(new Carrier(), 0);
        var observedTension = Relation.OrderedGroup(
            new Carrier(),
            Label("observed sample delta"),
            Relation.Segment(new Carrier(), -2, 2));

        var observedPath = GojoObserverPathSurface.Create(
            new Carrier(),
            wave,
            expectedSurface,
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(new Carrier(), wave, Label("sample"), observedResult)),
            observedResult,
            observedTension);
        var liveRecomputedPath = GojoObserverPathSurface.Create(
            new Carrier(),
            wave,
            expectedSurface,
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(new Carrier(), wave, Label("sample"), observedResult)),
            observedResult);

        Assert.True(GojoObserverPathSurface.TryRead(observedPath, out var observedRead));
        Assert.True(GojoObserverPathSurface.TryRead(liveRecomputedPath, out var recomputedRead));
        Assert.Same(observedTension, observedRead.ResidueOrTension);
        Assert.NotSame(observedTension, recomputedRead.ResidueOrTension);
    }

    [Fact]
    public void PathCanWalkFromDiagnosticSnapshotToSpecificRow()
    {
        var source = Relation.Segment(new Carrier(), 0, 4);
        var encoded = GojoStructureEncodingSurface.CreateDiagnosticSnapshot(new Carrier(), "segment block", source);

        Assert.True(GojoStructureEncodingSurface.TryRead(encoded, out var encodedRead));
        var relationRow = encodedRead.Relations[0];
        Assert.True(GojoStructureEncodingSurface.TryReadRelationRow(relationRow, out var rowRead));

        var path = GojoObserverPathSurface.Create(
            new Carrier(),
            encoded,
            GojoObserverPathSurface.ObserverSurface(new Carrier(), encoded, Label("relation table scan")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    encoded,
                    Label("relation row 0"),
                    relationRow,
                    encodedRead.OpenedStructure)),
            relationRow);

        Assert.True(GojoObserverPathSurface.TryRead(path, out var pathRead));
        Assert.Same(relationRow, pathRead.Result);
        var step = Assert.Single(pathRead.Steps);
        Assert.True(GojoObserverPathSurface.TryReadStep(step, out var stepRead));
        Assert.Same(encoded, stepRead.From);
        Assert.Same(relationRow, stepRead.To);
        Assert.Same(source, stepRead.Evidence);
        Assert.True(GojoStructureEncodingSurface.TryReadRelationRow(stepRead.To, out var walkedRow));
        Assert.Equal(rowRead.Id, walkedRow.Id);
    }

    private static Relation Label(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
