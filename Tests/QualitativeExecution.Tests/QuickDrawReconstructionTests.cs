using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QuickDraw;
using Gojo.Patterns;
using static Gojo.Experiments.QuickDraw.SequentialEvidence;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QuickDrawReconstructionTests
{
    [Fact]
    public void AcquisitionOrderSharesOnlyActualContiguousSitesAndRetainsPenLiftsAndReturns()
    {
        var packet = Create(1024, [new([Input(0, 0, 20, 0), Input(20, 0, 0, 0)]), new([Input(0, 0, 0, 20)])]);
        Assert.True(TryRead(packet, out var read)); var sections = read.Sections.ToArray();
        Assert.Same(read.Origin, sections[0].Anchor); Assert.NotSame(read.Origin, sections[0].From);
        Assert.Same(sections[0].To, sections[1].From); Assert.Same(sections[0].To, sections[1].Anchor);
        Assert.NotSame(sections[0].From, sections[1].To); // A return is not asserted closure.
        Assert.Same(sections[1].To, sections[2].Anchor); Assert.NotSame(sections[1].To, sections[2].From);
        Assert.Equal(new[] { 0, 1, 2 }, sections.Select(section => section.Sequence));
        Assert.Equal(new[] { 0, 0, 1 }, sections.Select(section => section.Stroke));
    }

    [Fact]
    public void GapsWithinAStrokeAreExplicitOffsetsWithIndependentSites()
    {
        var packet = Create(1024, [new([Input(0, 0, 20, 0), Input(23, 2, 40, 1)])]);
        Assert.True(TryRead(packet, out var read)); var sections = read.Sections.ToArray();
        Assert.NotSame(sections[0].To, sections[1].From); Assert.Same(sections[0].To, sections[1].Anchor);
        Assert.Equal(new Point(23, 2), sections[1].Start); Assert.Equal(new Point(40, 1), sections[1].End);
    }

    [Fact]
    public void ForwardReferenceCannotAcquireMeaningFromEqualCoordinatesOrAValidLaterSite()
    {
        var packet = Create(1024, [new([Input(0, 0, 20, 0), Input(20, 0, 40, 0)])]);
        Assert.True(TryRead(packet, out var read)); var sections = read.Sections.ToArray();
        var first = sections[0].Surface; var body = Assert.IsType<Relation>(first.Right);
        var fields = Assert.IsType<Relation>(body.Right); var anchoring = Assert.IsType<Relation>(fields.Left);
        var malformed = new Relation(first.Left, new Relation(body.Left,
            new Relation(new Relation(sections[1].To, anchoring.Right), fields.Right)));
        Assert.False(TryRead(ReplaceFirstSection(packet, malformed), out _));
        var lookalike = new Placeholder();
        Assert.False(TryRead(ReplaceFirstSection(packet, new Relation(first.Left, new Relation(body.Left,
            new Relation(new Relation(lookalike, anchoring.Right), fields.Right)))), out _));
    }

    [Fact]
    public void RowReorderingAndOpposedSiteFacesAreNotValidChronology()
    {
        var packet = Create(1024, [new([Input(0, 0, 20, 0), Input(20, 0, 40, 0)])]);
        var stroke = Assert.IsType<Relation>(Entries(Assert.IsType<Relation>(packet.Right))[0]);
        var rows = Entries(Assert.IsType<Relation>(stroke.Right));
        var reordered = new Relation(packet.Left, Scope(new Relation(stroke.Left, Scope(rows.Reverse().ToArray()))));
        Assert.False(TryRead(reordered, out _));
        var first = Assert.IsType<Relation>(rows[0]); var eventOrdinal = Assert.IsType<Relation>(first.Left);
        Assert.False(TryRead(ReplaceFirstSection(packet, new Relation(new Relation(eventOrdinal.Left.Dual(), eventOrdinal.Right), first.Right)), out _));
    }

    [Fact]
    public void EmptyStrokeAndPointLikeSectionRemainVisibleWithoutInventedMovement()
    {
        var packet = Create(1024, [new([]), new([Input(7, 9, 7, 9)])]);
        Assert.True(TryRead(packet, out var read)); Assert.Empty(read.Strokes[0]);
        var section = Assert.Single(read.Strokes[1]); Assert.Equal(section.Start, section.End); Assert.NotSame(section.From, section.To);
    }

    [Fact]
    public void MeasuredFeatureContractRejectsDifferentChordOrCoordinateUnits()
    {
        var bad = Input(0, 0, 20, 0); bad.Features[1] = 19;
        Assert.Throws<InvalidDataException>(() => Create(1024, [new([bad])]));
        var packet = Create(1024, []); var header = Assert.IsType<Relation>(packet.Left); var basis = Assert.IsType<Relation>(header.Left);
        var parts = Entries(basis); var features = Entries(Assert.IsType<Relation>(parts[3]));
        var incompatible = new Carrier(UnitShape.OrthogonalOf(Relation.Point(new Carrier(), 1)));
        features[0] = Relation.Segment(incompatible, 0, 1); parts[3] = Scope(features);
        Assert.False(TryRead(new Relation(new Relation(Scope(parts), header.Right), packet.Right), out _));
    }

    [Fact]
    public void NativePriorRoundsTheWholeSumAndRetainsSignedProjectionRemainder()
    {
        var weights = Zeros(); weights[0][1] = 6; weights[0][2] = 6;
        var prior = NativePrior.Create(10, weights, 1); Assert.True(NativePrior.TryRead(prior, out var model));
        foreach (var sign in new[] { -1, 1 })
        {
            var packet = Create(1024, [new([Input(0, 0, sign, sign)])]); Assert.True(TryRead(packet, out var read));
            var prediction = NativePrior.Predict(model, Assert.Single(read.Sections));
            Assert.Equal(sign, prediction.Outputs[0].Ticks);
            Assert.Contains(Reach(prediction.Surface), element => element is CarrierLocation location &&
                ReferenceEquals(location.Carrier, model.Rows[0].Accumulator) && location.Ticks == sign * 2);
        }
    }

    [Fact]
    public void DetachedNativeEvidenceAndLearnedEquationsGenerateTheSameControlsAfterRestore()
    {
        var packet = Create(1024, [new([Input(2, 3, 62, 93), Input(62, 93, 122, 3)]), new([Input(200, 100, 100, 100)])]);
        var prior = ChordPrior(); var generated = NativeReconstruction.Generate(packet, prior);
        Assert.True(NativeReconstruction.TryRead(generated, out var before));
        var restoredPacket = Restore(packet); var restoredPrior = Restore(prior);
        var rebuilt = Restore(NativeReconstruction.Generate(restoredPacket, restoredPrior));
        Assert.True(NativeReconstruction.TryRead(rebuilt, out var after));
        Assert.Equal(before.Strokes.SelectMany(stroke => stroke).SelectMany(curve => curve.Controls),
            after.Strokes.SelectMany(stroke => stroke).SelectMany(curve => curve.Controls));
        Assert.Equal(new[] { new Point(2, 3), new Point(22, 33), new Point(42, 63), new Point(62, 93) }, after.Strokes[0][0].Controls);
        Assert.NotSame(before.Strokes[0][0].From, after.Strokes[0][0].From);
        Assert.Same(after.Strokes[0][0].To, after.Strokes[0][1].From);
        Assert.NotSame(after.Strokes[0][1].To, after.Strokes[1][0].From);
        foreach (var carrier in Reach(rebuilt).OfType<Carrier>()) { Assert.Equal(0UL, carrier.Value); Assert.Equal(0L, carrier.Tension); }
    }

    [Fact]
    public void CoarseProfilesCanChangeGeneratedHandlesAndReflectionIsRetainedWithoutCategoryKnowledge()
    {
        var weights = Zeros(); weights[0][3] = 1; weights[1][4] = 1; weights[2][7] = 1; weights[3][8] = 1;
        var prior = NativePrior.Create(1, weights, 10);
        Point[] Produce(long sign)
        {
            var source = Input(100, 100, 100, 300); source.Features[3] = sign * 100; source.Features[4] = 50;
            source.Features[7] = sign * 100; source.Features[8] = 150;
            var result = Restore(NativeReconstruction.Generate(Restore(Create(1024, [new([source])])), Restore(prior)));
            Assert.True(NativeReconstruction.TryRead(result, out var read)); return Assert.Single(Assert.Single(read.Strokes)).Controls;
        }
        var left = Produce(-1); var right = Produce(1);
        Assert.Equal(left.Select(point => 200 - point.X), right.Select(point => point.X));
        Assert.Equal(left.Select(point => point.Y), right.Select(point => point.Y));
    }

    [Fact]
    public void CurveReaderRequiresActualInterpolationDependenciesNotJustFourCoordinates()
    {
        var packet = Create(1024, [new([Input(0, 0, 90, 0)])]); Assert.True(TryRead(packet, out var read));
        var section = Assert.Single(read.Sections);
        var curve = NativeReconstruction.BuildCurve(read.Frame, section.From, section.To,
            [new(0, 0), new(30, -20), new(60, -20), new(90, 0)]);
        Assert.True(NativeReconstruction.TryReadCurve(read.Frame, curve, out _));
        var body = Assert.IsType<Relation>(curve.Right); var words = Entries(Assert.IsType<Relation>(body.Left));
        (words[1], words[2]) = (words[2], words[1]);
        Assert.False(NativeReconstruction.TryReadCurve(read.Frame, new Relation(curve.Left, new Relation(Scope(words), body.Right)), out _));
    }

    [Fact]
    public void AppendingLaterSectionsDoesNotChangeAnEarlierConstructionAtTheChosenBasis()
    {
        var prefix = Create(1024, [new([Input(10, 20, 70, 110)])]);
        var complete = Create(1024, [new([Input(10, 20, 70, 110), Input(70, 110, 200, 20)]), new([Input(0, 0, 20, 20)])]);
        Assert.True(NativeReconstruction.TryRead(Restore(NativeReconstruction.Generate(Restore(prefix), Restore(ChordPrior()))), out var a));
        Assert.True(NativeReconstruction.TryRead(Restore(NativeReconstruction.Generate(Restore(complete), Restore(ChordPrior()))), out var b));
        Assert.Equal(a.Strokes[0][0].Controls, b.Strokes[0][0].Controls);
    }

    [Fact]
    public void EqualLookingTranslationOutputDoesNotEstablishControlDependency()
    {
        var packet = Create(1024, [new([Input(0, 0, 90, 0)])]); var program = NativeReconstruction.Generate(packet, ChordPrior());
        Assert.True(NativeReconstruction.TryRead(program, out _));
        var provenance = Assert.IsType<Relation>(program.Right); var modelAndWitnesses = Assert.IsType<Relation>(provenance.Right);
        var witnesses = Entries(Assert.IsType<Relation>(modelAndWitnesses.Right));
        var witness = Assert.IsType<Relation>(witnesses[0]); var translationsAndCurve = Assert.IsType<Relation>(witness.Right);
        var translations = Entries(Assert.IsType<Relation>(translationsAndCurve.Left));
        var translation = Assert.IsType<Relation>(translations[0]); var prediction = Assert.IsType<Relation>(translation.Right);
        var actualOutput = Assert.IsType<CarrierLocation>(prediction.Left);
        translations[0] = new Relation(translation.Left, new Relation(new CarrierLocation(actualOutput.Carrier, actualOutput.Ticks), prediction.Right));
        witnesses[0] = new Relation(witness.Left, new Relation(Scope(translations), translationsAndCurve.Right));
        var altered = new Relation(program.Left, new Relation(provenance.Left, new Relation(modelAndWitnesses.Left, Scope(witnesses))));
        Assert.False(NativeReconstruction.TryRead(altered, out _));
    }

    [Fact]
    public void PriorCannotReplaceItsOutputWithAnIncompatibleUnit()
    {
        var prior = ChordPrior(); var definition = Assert.IsType<Relation>(prior.Left);
        var rows = Entries(Assert.IsType<Relation>(definition.Right)); var row = Assert.IsType<Relation>(rows[0]);
        var projection = Assert.IsType<Relation>(row.Right);
        var incompatible = new Carrier(UnitShape.OrthogonalOf(Relation.Point(new Carrier(), 1)));
        rows[0] = new Relation(row.Left, new Relation(projection.Left, Relation.Segment(incompatible, 0, 1)));
        var altered = new Relation(new Relation(definition.Left, Scope(rows)), prior.Right);
        Assert.False(NativePrior.TryRead(altered, out _));
    }

    [Fact]
    public void PacketDoesNotTraverseIntoPrivateAcquisitionOrNativeGeneratedControls()
    {
        var source = new NativeAcquisition(QuickDrawRecord.Parse("{\"key_id\":\"private\",\"word\":\"moon\",\"drawing\":[[[0,90],[0,0]]]}"));
        var packet = Create(1024, [new([Input(0, 0, 90, 0)])]); var prior = ChordPrior();
        var program = NativeReconstruction.Generate(packet, prior);
        var publicNodes = Reach(packet); var acquisitionNodes = Reach(source.Surface);
        Assert.DoesNotContain(publicNodes, element => acquisitionNodes.Contains(element, ReferenceEqualityComparer.Instance));
        Assert.DoesNotContain(publicNodes, element => ReferenceEquals(element, prior) || ReferenceEquals(element, program));
        Assert.Contains(Reach(program), element => ReferenceEquals(element, packet));
    }

    private static SectionInput Input(long x, long y, long ex, long ey) => new([x, y], [ex, ey], [1024, ex - x, ey - y, 0, 0, 0, 0, 0, 0]);
    private static long[][] Zeros() => Enumerable.Range(0, 4).Select(_ => new long[9]).ToArray();
    private static Relation ChordPrior()
    {
        var weights = Zeros(); weights[0][1] = 1; weights[1][2] = 1; weights[2][1] = 2; weights[3][2] = 2;
        return NativePrior.Create(3, weights, 5);
    }
    private static Relation ReplaceFirstSection(Relation packet, Relation replacement)
    {
        var strokes = Entries(Assert.IsType<Relation>(packet.Right)); var first = Assert.IsType<Relation>(strokes[0]);
        var sections = Entries(Assert.IsType<Relation>(first.Right)); sections[0] = replacement;
        strokes[0] = new Relation(first.Left, Scope(sections)); return new Relation(packet.Left, Scope(strokes));
    }
    private static IElement[] Entries(Relation scope) { Assert.True(StructuralConstraint.TryReadScope(scope, out var rows, out _, out _)); return rows; }
    private static Relation Scope(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static Relation Restore(Relation source)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("anonymous", source));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        return Assert.IsType<Relation>(restored.Root);
    }
    private static IElement[] Reach(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element)) if (seen.Add(element) && element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen.ToArray();
    }
}
