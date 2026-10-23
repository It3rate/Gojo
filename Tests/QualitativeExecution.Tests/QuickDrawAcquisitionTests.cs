using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QuickDraw;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QuickDrawAcquisitionTests
{
    [Fact]
    public void RawRecordRetainsExactDecimalLexemesLargeStringIdentityAndUnchangedSource()
    {
        const string json = "{\"key_id\":\"90071992547409931234\",\"word\":\"moon\",\"recognized\":false,\"drawing\":[[[1.250,1e-2,-2],[0,2.500,3],[0,11.25,10]]]}";
        var record = QuickDrawRecord.Parse(json);
        Assert.Equal(json, record.OriginalJson); Assert.Equal("90071992547409931234", record.KeyId);
        Assert.True(record.HasTiming); Assert.False(record.Recognized);
        var samples = Assert.Single(record.Strokes).Samples;
        Assert.Equal("1.250", samples[0].X.Lexeme); Assert.Equal(new BigInteger(5), samples[0].X.Numerator);
        Assert.Equal(new BigInteger(4), samples[0].X.Denominator);
        Assert.Equal("1e-2", samples[1].X.Lexeme); Assert.Equal(new BigInteger(100), samples[1].X.Denominator);
        Assert.Equal("10", samples[2].Time!.Lexeme); // Retains the backward recorded time.
    }

    [Theory]
    [InlineData("{\"key_id\":12,\"drawing\":[]}")]
    [InlineData("{\"key_id\":\"a\",\"key_id\":\"b\",\"drawing\":[]}")]
    [InlineData("{\"key_id\":\"a\",\"drawing\":[[[0],[0,1]]]}")]
    [InlineData("{\"key_id\":\"a\",\"drawing\":[[[0],[0],[0]],[[1],[1]]]}")]
    [InlineData("{\"key_id\":\"a\",\"drawing\":[[[\"0\"],[0]]]}")]
    [InlineData("{\"key_id\":\"a\",\"drawing\":[[[0],[0],[0],[0]]]}")]
    public void MalformedOrAmbiguousRecordsAreRejectedBeforeConversion(string json) =>
        Assert.Throws<InvalidDataException>(() => QuickDrawRecord.Parse(json));

    [Fact]
    public void StreamingReaderReportsSourceLineAndDoesNotLoseTheValidPrefix()
    {
        using var reader = new StringReader("\n{\"key_id\":\"a\",\"drawing\":[]}\n{\"key_id\":\"b\",\"drawing\":[[[0],[0,1]]]}\n");
        using var rows = QuickDrawRecord.ReadNdjson(reader).GetEnumerator();
        Assert.True(rows.MoveNext()); Assert.Equal(2, rows.Current.Line);
        var error = Assert.Throws<InvalidDataException>(() => rows.MoveNext()); Assert.Contains("line 3", error.Message);
    }

    [Fact]
    public void OrderedEventsRemainIndependentAtRepeatedAndCrossStrokeCoordinates()
    {
        var acquisition = Acquire("[[[0,1,0],[0,1,0],[0,1,2]],[[0,0],[0,0],[3,4]]]");
        Assert.True(NativeAcquisition.TryRead(acquisition.Surface, out var read));
        Assert.Equal(2, read.StrokeCount); Assert.Equal(5, read.Samples.Count);
        Assert.Equal(5, read.Samples.Select(sample => sample.Event).Distinct().Count());
        Assert.NotSame(read.Samples[0].Event, read.Samples[2].Event);
        Assert.NotSame(read.Samples[0].Event, read.Samples[3].Event);
        Assert.Equal(new[] { 0, 1, 2, 0, 1 }, read.Samples.Select(sample => sample.SampleIndex));
        Assert.Equal(2, Entries(acquisition.StrokeReads[0].Spans).Length);
        Assert.Single(Entries(acquisition.StrokeReads[1].Spans));
        foreach (var carrier in new[] { acquisition.X, acquisition.Y, acquisition.Time, acquisition.Index })
        { Assert.Equal(0UL, carrier.Value); Assert.Equal(0L, carrier.Tension); Assert.Empty(carrier.AttachedRelations); }
    }

    [Fact]
    public void SourceSpanUsesActualOrderedRowsInsteadOfCoordinateLookalikes()
    {
        var acquisition = Acquire("[[[0,1,2],[0,0,0]]]"); var stroke = Assert.Single(acquisition.StrokeReads);
        var span = Assert.IsType<Relation>(Entries(stroke.Spans)[0]); Assert.Same(stroke.Event, span.Left);
        var content = Assert.IsType<Relation>(span.Right); var pair = Assert.IsType<Relation>(content.Left);
        Assert.Same(stroke.Samples[0].Event, pair.Left); Assert.Same(stroke.Samples[1].Event, pair.Right);
        var covered = Entries(Assert.IsType<Relation>(content.Right));
        Assert.Same(stroke.Samples[0].Surface, covered[0]); Assert.Same(stroke.Samples[1].Surface, covered[1]);
    }

    [Fact]
    public void MissingTimingIsOpenAndRecordedZeroTimingIsAValue()
    {
        var simplified = Acquire("[[[0],[0]]]"); var raw = Acquire("[[[0],[0],[0]]]");
        Assert.True(NativeAcquisition.TryRead(simplified.Surface, out var withoutTime));
        Assert.True(NativeAcquisition.TryRead(raw.Surface, out var withTime));
        Assert.Null(Assert.Single(withoutTime.Samples).TimeNumerator);
        Assert.Equal(BigInteger.Zero, Assert.Single(withTime.Samples).TimeNumerator);
        Assert.Equal(BigInteger.One, Assert.Single(withTime.Samples).TimeDenominator);
        Assert.False(Closed(simplified.StrokeReads[0].Samples[0].Time)); Assert.True(Closed(raw.StrokeReads[0].Samples[0].Time));
    }

    [Fact]
    public void EmptyAndSinglePointStrokesStayVisibleWithoutInventingEdges()
    {
        var acquisition = Acquire("[[[],[]],[[3],[4]]]");
        var publication = new AcquisitionPublication(acquisition);
        Assert.Equal(2, acquisition.StrokeReads.Count); Assert.Empty(acquisition.StrokeReads[0].Samples);
        Assert.Single(Entries(publication.Observation.Sites)); Assert.Empty(Entries(publication.Observation.Segments));
        Assert.True(NativeAcquisition.TryRead(acquisition.Surface, out var read)); Assert.Equal(2, read.StrokeCount);
    }

    [Fact]
    public void DetachedPublicationKeepsSamplingIncidenceButHidesCoordinatesLabelsAndTiming()
    {
        var acquisition = Acquire("[[[0,2,0],[0,2,0],[0,12,24]],[[0,0],[0,0],[25,26]]]");
        var publication = new AcquisitionPublication(acquisition); var observation = publication.Observation;
        Assert.Equal(5, Entries(observation.Sites).Length); Assert.Equal(3, Entries(observation.Segments).Length);
        Assert.Empty(Entries(observation.Continuations)); Assert.False(Closed(observation.Continuations));
        Assert.Empty(Entries(observation.Crossings)); Assert.False(Closed(observation.Crossings));
        var first = Assert.IsType<Relation>(Entries(observation.Segments)[0]);
        Assert.True(observation.TryReadSegment(first, out _, out var start, out var end, out var heading));
        Assert.Contains(observation.Frame.Right, Entries(heading)); Assert.Contains(observation.Frame.Down, Entries(heading));
        var second = Assert.IsType<Relation>(Entries(observation.Segments)[1]);
        Assert.True(observation.TryReadSegment(second, out _, out var nextStart, out var nextEnd, out _));
        Assert.Same(end, nextStart); Assert.NotSame(start, nextEnd); // Numerical return is not a topological closure.
        var duplicate = Assert.IsType<Relation>(Entries(observation.Segments)[2]);
        Assert.True(observation.TryReadSegment(duplicate, out _, out _, out _, out var openHeading)); Assert.False(Closed(openHeading));
        var publicNodes = Reach(observation.Surface); var privateNodes = Reach(acquisition.Surface);
        Assert.DoesNotContain(publicNodes, element => element is Carrier or CarrierLocation or TickRef);
        Assert.DoesNotContain(publicNodes, element => privateNodes.Any(hidden => ReferenceEquals(hidden, element)));
        Assert.Contains(Reach(publication.Audit), element => ReferenceEquals(element, acquisition.Surface));
    }

    [Fact]
    public void NativeTransportPreservesExactFractionsTimingAndSampleAliasesInAFreshWorld()
    {
        var acquisition = Acquire("[[[1.25,-2,1e-2],[3.5,4,5],[0,11.25,10]]]");
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("source", acquisition.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        Assert.True(NativeAcquisition.TryRead(Assert.IsType<Relation>(restored.Root), out var read));
        Assert.NotSame(acquisition.Event, read.Event); Assert.Equal(3, read.Samples.Count);
        Assert.Equal(new BigInteger(5), read.Samples[0].XNumerator); Assert.Equal(new BigInteger(4), read.Samples[0].XDenominator);
        Assert.Equal(new BigInteger(1), read.Samples[2].XNumerator); Assert.Equal(new BigInteger(100), read.Samples[2].XDenominator);
        Assert.Equal(new BigInteger(45), read.Samples[1].TimeNumerator); Assert.Equal(new BigInteger(4), read.Samples[1].TimeDenominator);
        Assert.Equal(new BigInteger(10), read.Samples[2].TimeNumerator);
    }

    [Fact]
    public void ScalarReadRequiresItsActualBasisReducedFractionAndPositiveSubdivision()
    {
        var c = new Carrier(); var other = new Carrier(c.UnitShape);
        var scalar = new Relation(new CarrierLocation(c, 5), Relation.Point(c, 4));
        Assert.True(NativeAcquisition.TryReadScalar(scalar, c, out _, out _));
        Assert.False(NativeAcquisition.TryReadScalar(scalar, other, out _, out _));
        Assert.False(NativeAcquisition.TryReadScalar(new Relation(new CarrierLocation(c, 2), Relation.Point(c, 4)), c, out _, out _));
        Assert.False(NativeAcquisition.TryReadScalar(new Relation(new CarrierLocation(c, 1), Relation.Point(c, 0)), c, out _, out _));
    }

    [Fact]
    public void UnrepresentableNativeRangeFailsExplicitlyWhileExactInputStillExists()
    {
        var record = QuickDrawRecord.Parse("{\"key_id\":\"a\",\"drawing\":[[[1e30],[0]]]}");
        Assert.Equal(BigInteger.Pow(10, 30), record.Strokes[0].Samples[0].X.Numerator);
        var error = Assert.Throws<InvalidDataException>(() => new NativeAcquisition(record));
        Assert.Contains("no rounding", error.Message);
    }

    [Fact]
    public void AcquisitionReaderRejectsUnrelatedShapeOrSwappedBasisRatherThanSearchingInside()
    {
        var acquisition = Acquire("[[[0,1],[0,1]]]");
        Assert.False(NativeAcquisition.TryRead(new Relation(new Placeholder(), acquisition.Surface), out _));
        var wrongBasis = new Relation(new Relation(Relation.Segment(acquisition.Y, 0, 1), Relation.Segment(acquisition.X, 0, 1)),
            new Relation(Relation.Segment(acquisition.Time, 0, 1), Relation.Segment(acquisition.Index, 0, 1)));
        var wrong = new Relation(new Relation(acquisition.Event, wrongBasis), acquisition.Strokes);
        Assert.False(NativeAcquisition.TryRead(wrong, out _));
    }

    [Theory]
    [InlineData("source")]
    [InlineData("stroke")]
    [InlineData("sample")]
    public void OpposedFacesCannotMasqueradeAsIndependentAcquisitionEvents(string role)
    {
        var acquisition = Acquire("[[[0],[0]]]"); var stroke = Assert.Single(acquisition.StrokeReads);
        Relation malformed;
        if (role == "source")
            malformed = new(new Relation(acquisition.Event.Dual(), acquisition.Basis), acquisition.Strokes);
        else if (role == "stroke")
        {
            // This has the source's slot identity on the other face. Counting
            // placeholder faces separately would falsely accept another event.
            var strokeRow = new Relation(acquisition.Event.Dual(), stroke.Surface.Right);
            malformed = new(new Relation(acquisition.Event, acquisition.Basis), StructuralConstraint.Closed(strokeRow));
        }
        else
        {
            var sample = Assert.Single(stroke.Samples);
            var sampleRow = new Relation(stroke.Event.Dual(), sample.Surface.Right);
            var strokeRow = new Relation(stroke.Event, new Relation(new CarrierLocation(acquisition.Index, 0),
                new Relation(StructuralConstraint.Closed(sampleRow), StructuralConstraint.Closed())));
            malformed = new(new Relation(acquisition.Event, acquisition.Basis), StructuralConstraint.Closed(strokeRow));
        }
        Assert.False(NativeAcquisition.TryRead(malformed, out _));
        Assert.True(NativeAcquisition.TryRead(acquisition.Surface, out _));
    }

    [Fact]
    public void SourceSpanCannotReplaceAnActualSampleRowWithAnEqualLookingCopy()
    {
        var acquisition = Acquire("[[[0,1],[0,1]]]"); var stroke = Assert.Single(acquisition.StrokeReads);
        var copiedRow = new Relation(stroke.Samples[0].Surface.Left, stroke.Samples[0].Surface.Right);
        var span = new Relation(stroke.Event, new Relation(new Relation(stroke.Samples[0].Event, stroke.Samples[1].Event),
            StructuralConstraint.Closed(copiedRow, stroke.Samples[1].Surface)));
        var strokeRow = new Relation(stroke.Event, new Relation(new CarrierLocation(acquisition.Index, 0),
            new Relation(StructuralConstraint.Closed(stroke.Samples.Select(sample => (IElement)sample.Surface).ToArray()),
                StructuralConstraint.Closed(span))));
        var malformed = new Relation(new Relation(acquisition.Event, acquisition.Basis), StructuralConstraint.Closed(strokeRow));
        Assert.False(NativeAcquisition.TryRead(malformed, out _));
        Assert.True(NativeAcquisition.TryRead(acquisition.Surface, out _));
    }

    private static NativeAcquisition Acquire(string drawing) => new(QuickDrawRecord.Parse("{\"key_id\":\"pilot\",\"word\":\"private prompt\",\"drawing\":" + drawing + "}"));
    private static IElement[] Entries(Relation scope) { Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries; }
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] Reach(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) if (seen.Add(item) && item is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); }
        return seen.ToArray();
    }
}
