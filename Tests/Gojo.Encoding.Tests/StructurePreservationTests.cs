using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class StructurePreservationTests
{
    private static IElement Reload(IElement source) => GojoStructureCompactJson.ToElement(
        GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("preservation", source)))).Root;
    private static IElement[] Parts(Relation group)
    { Assert.True(group.TryReadOrderedElementsOn((Carrier)group.Right, out var elements)); return elements; }

    [Fact]
    public void SavedNegativeWeightReplaysTheSameAnswer()
    {
        var axis = new Carrier();
        var equation = Equation.Difference(axis, axis, axis);
        var witness = Equation.PredictWitness(new Carrier(), equation,
            new Relation(new CarrierLocation(axis, 2), new CarrierLocation(axis, 10)), axis);
        var restored = Assert.IsType<Relation>(Reload(witness));
        Assert.True(Equation.TryReadPredictionWitness(restored, out var input, out var surface, out var answer, out _, out _));
        Assert.True(Equation.TryReadPrediction(Equation.Predict(surface, input, answer.Carrier), out var replay, out _));
        Assert.Equal(8, answer.Ticks);
        Assert.Equal(answer, replay);
    }

    [Fact]
    public void ArchivePreservesInverseZeroSharedTicksAndEndpointIdentity()
    {
        var axis = new Carrier();
        var tick = new TickRef(0);
        var faces = new Relation(new CarrierLocation(axis.Inverse(), tick), new CarrierLocation(axis, tick));
        var point = Relation.Point(axis, 3);
        var root = Relation.OrderedGroup(new Carrier(), faces, new Relation(point, Relation.Point(axis, 2)), point);
        var restored = Parts((Relation)Reload(root));
        var pair = (Relation)restored[0];
        Assert.True(pair.TryReadFusedCarrierFaces(out var inverse, out var positive));
        Assert.Same(positive.Carrier.Inverse(), inverse.Carrier);
        Assert.True(inverse.SameTickRefAs(positive));
        inverse.Tick.Set(7);
        Assert.Equal(7, positive.Ticks);
        var segment = (Relation)restored[1];
        Assert.Same(segment.Left, restored[2]);
        Assert.Equal(-1, segment.FlowOn(positive.Carrier).Ticks);
    }

    [Fact]
    public void ArchiveRestoresResolutionDescriptorsAccountingAndLiveAttachments()
    {
        var descriptor = Relation.Point(new Carrier(), 12345);
        var unit = UnitShape.ResolutionOf(UnitShape.DescribedAtom(descriptor), 100);
        var capacity = new Carrier(unit);
        capacity.Attach(Relation.Segment(capacity, 10, 10));
        capacity.Add(13);
        var root = new Relation(capacity, capacity.ValueSlot);
        var restored = (Relation)Reload(root);
        var loaded = (Carrier)restored.Left;
        Assert.True(loaded.UnitShape.TryReadResolution(out _, out _));
        Assert.Contains(GojoEncodedSnapshot.Capture(loaded).TickRefs, t => t.Ticks == 12345);
        Assert.Equal(10UL, loaded.Value);
        Assert.Equal(3, loaded.Tension);
        Assert.True(loaded.ValueSlot.SameTickRefAs(restored.RightLocation));
        capacity.Add(1);
        loaded.Add(1);
        Assert.Equal(capacity.Value, loaded.Value);
        Assert.Equal(capacity.Tension, loaded.Tension);
        Assert.Equal(loaded.Value, (ulong)restored.RightLocation.Ticks);
    }

    [Fact]
    public void DefinitionArchiveRetainsOrderResidueAndSharedSources()
    {
        var axis = new Carrier();
        var directed = Relation.DirectedSegment(axis, 2, 3);
        var residue = Relation.Segment(axis, 0, 4);
        var first = ConceptDefinitionSurface.Create(new Carrier(), "first", directed, residueOrTension: residue);
        var second = ConceptDefinitionSurface.Create(new Carrier(), "second", directed.Reverse(), [first], residueOrTension: residue);
        var document = ConceptDefinitionInterchangeJson.FromLedger("definitions", ConceptDefinitionSurface.Ledger(new Carrier(), [first, second]));
        var ledger = ConceptDefinitionInterchangeJson.ToLabeledLedger(
            ConceptDefinitionInterchangeJson.Deserialize(ConceptDefinitionInterchangeJson.Serialize(document)));
        Assert.True(ConceptDefinitionSurface.TryRead(ledger.SingleDefinition("first"), out var a));
        Assert.True(ConceptDefinitionSurface.TryRead(ledger.SingleDefinition("second"), out var b));
        Assert.Same(a.ResidueOrTension, b.ResidueOrTension);
        var aBody = (Relation)a.Body;
        var bBody = (Relation)b.Body;
        Assert.Same(aBody.Left, bBody.Right);
        Assert.Same(aBody.Right, bBody.Left);
        Assert.Same(ledger.SingleDefinition("first"), b.Dependencies[0]);
    }

    [Fact]
    public void ArchivePreservesSettlementPriorityDespiteReverseTraversal()
    {
        var source = new Carrier();
        var first = new Carrier();
        var second = new Carrier();
        var firstRoute = source.Attach(new Relation(Relation.Point(source, 1), Relation.Point(first, 0)));
        var secondRoute = source.Attach(new Relation(Relation.Point(source, 1), Relation.Point(second, 0)));
        var root = Relation.OrderedGroup(new Carrier(), secondRoute, firstRoute, second, first, source);
        var restored = Parts((Relation)Reload(root));
        var packed = Parts((Relation)GojoEncodedSnapshot.CaptureArchive(root).RestoreRoots()[0]);
        Assert.True(((Relation)restored[1]).Id < ((Relation)restored[0]).Id);
        Assert.True(((Carrier)restored[3]).Id < ((Carrier)restored[2]).Id);
        source.Add(1);
        foreach (var parts in new[] { restored, packed })
        {
            ((Carrier)parts[4]).Add(1);
            Assert.Equal(first.Value, ((Carrier)parts[3]).Value);
            Assert.Equal(second.Value, ((Carrier)parts[2]).Value);
            Assert.Equal(source.Tension, ((Carrier)parts[4]).Tension);
        }
        Assert.Equal(1UL, first.Value);
        Assert.Equal(0UL, second.Value);
    }

    [Fact]
    public void ArchiveRetainsAnUnboundSlotAndItsOpposedFace()
    {
        var slot = new Placeholder();
        var root = Relation.OrderedGroup(new Carrier(), slot.Dual(), slot, new Placeholder());
        var restored = Parts((Relation)Reload(root));
        var forward = Assert.IsType<Placeholder>(restored[1]);
        Assert.Equal(forward.Dual(), restored[0]);
        Assert.NotEqual(forward, restored[2]);
    }

    [Fact]
    public void LegacyArchiveRestoresRecordedStateAndReportsMissingEvidence()
    {
        var legacy = new GojoCompactStructureDocument
        {
            Root = "capacity",
            Carriers = [new GojoCompactCarrierNode { Id = "capacity", Value = 10, Tension = 3 }]
        };
        var restored = GojoStructureCompactJson.ToElement(legacy);
        var capacity = Assert.IsType<Carrier>(restored.Root);
        Assert.Equal(10UL, capacity.Value);
        Assert.Equal(3, capacity.Tension);
        Assert.IsType<Relation>(restored.ResidueOrTension);
        Assert.NotEqual(Relation.Point(Carrier.Unknown, 0), restored.ResidueOrTension);
    }

    [Fact]
    public void ReloadedCalculationRespondsToItsRestoredSourceWithoutRebuilding()
    {
        var axis = new Carrier(); axis.Set(2);
        Relation Observe(CarrierLocation input) => QualifiedReadSurface.Create(new Carrier(), input,
            Relation.Segment(axis, 0, 12), input, new Relation(input, input), new Placeholder(),
            new Placeholder(), new Placeholder(), input);
        var a = Observe(axis.ValueSlot);
        var b = Observe(new CarrierLocation(axis, 3));
        var sum = QualifiedReadSurface.Add(new Carrier(), a, b);
        var difference = QualifiedReadSurface.Subtract(new Carrier(), a, b);
        var restored = Parts((Relation)Reload(Relation.OrderedGroup(new Carrier(), axis, sum, difference)));
        var live = (Carrier)restored[0];
        Assert.True(QualifiedReadSurface.TryReadValue((Relation)restored[1], out var first)); Assert.Equal(5, first.Ticks);
        live.Set(9);
        Assert.True(QualifiedReadSurface.TryReadValue((Relation)restored[1], out var current)); Assert.Equal(12, current.Ticks);
        Assert.True(QualifiedReadSurface.TryReadValue((Relation)restored[2], out var delta)); Assert.Equal(6, delta.Ticks);
        Assert.True(QualifiedReadSurface.TryReadBounds((Relation)restored[1], out var low, out var high));
        Assert.Equal(12, low.Ticks); Assert.Equal(12, high.Ticks);
    }

    [Theory]
    [InlineData("value")]
    [InlineData("tension")]
    [InlineData("valueTick")]
    [InlineData("attachments")]
    [InlineData("ticks")]
    [InlineData("descriptors")]
    public void MissingArchiveFieldsRemainFailedReadsInsteadOfDefaultState(string field)
    {
        var capacity = new Carrier();
        capacity.Attach(Relation.Segment(capacity, 10, 10)); capacity.Add(13);
        var complete = GojoStructureCompactJson.FromElement("missing-fields", capacity);
        var document = complete with
        {
            Carriers = complete.Carriers.Select(c => c.Id != complete.Root ? c : field switch
            {
                "value" => c with { Value = null }, "tension" => c with { Tension = null },
                "valueTick" => c with { ValueTick = null }, "attachments" => c with { Attachments = null }, _ => c
            }).ToList(),
            Nodes = complete.Nodes.Select(n => field switch
            {
                "ticks" when n.Kind == "tick" => n with { Ticks = null },
                "descriptors" when n.Kind == "unitShape" => n with { Descriptors = null }, _ => n
            }).ToList()
        };
        var json = GojoStructureCompactJson.Serialize(document);
        Assert.False(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var loaded));
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(loaded.Root), out var selected, out _, out var result, out var residue, out _));
        Assert.IsType<Placeholder>(result); Assert.Same(residue, loaded.ResidueOrTension);
        Assert.True(EncodedTextSurface.TryRead(selected, out var retained)); Assert.Equal(json, retained);
        Assert.True(GojoStructureCompactJson.TryToElement(complete, out var restored));
        var known = Assert.IsType<Carrier>(restored.Root);
        Assert.Equal(10UL, known.Value); Assert.Equal(3, known.Tension);
    }

    [Fact]
    public void UnalignedOperationIdentitySurvivesReloadWhileZeroStateRemainsKnown()
    {
        var axis = new Carrier(); var other = new Carrier(axis.UnitShape);
        Relation Observe(Carrier carrier) => QualifiedReadSurface.Create(new Carrier(), carrier,
            Relation.Segment(carrier, 0, 12), new CarrierLocation(carrier, 2), Relation.Segment(carrier, 1, 3),
            new Placeholder(), new Placeholder(), new Placeholder(), carrier);
        var left = Observe(axis); var right = Observe(other);
        var root = Relation.OrderedGroup(new Carrier(), QualifiedReadSurface.Add(new Carrier(), left, right),
            QualifiedReadSurface.Subtract(new Carrier(), left, right), axis);
        var document = GojoStructureCompactJson.Deserialize(GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("pending", root)));
        Assert.True(GojoStructureCompactJson.TryToElement(document, out var loaded));
        var parts = Parts((Relation)loaded.Root);
        var answers = new long[2];
        for (var index = 0; index < 2; index++)
        {
            var pending = (Relation)parts[index];
            Assert.False(QualifiedReadSurface.TryReadValue(pending, out _));
            Assert.True(ReadWitness.TryRead(pending, out _, out var surface, out var result, out _, out _));
            Assert.IsType<Placeholder>(result);
            var equation = (Relation)surface;
            var target = ((Relation)equation.Right).LeftLocation.Carrier;
            var prediction = Equation.Predict(equation,
                new Relation(new CarrierLocation(target, 2), new CarrierLocation(target, 3)), target);
            Assert.True(Equation.TryReadPrediction(prediction, out var value, out _));
            answers[index] = value.Ticks;
        }
        Assert.Equal(new long[] { 5, -1 }, answers);
        Assert.Equal(0UL, ((Carrier)parts[2]).Value); Assert.Equal(0, ((Carrier)parts[2]).Tension);
    }

    [Fact]
    public void ContinuousCorrectionDoesNotAcceptOppositeFlow()
    {
        var axis = new Carrier();
        var current = Relation.Point(axis, 3);
        var proposed = Relation.Point(axis, 2);
        Assert.False(IntervalFocusSettlementSurface.TryReadContinuousCorrection(
            Relation.OrderedGroup(new Carrier(), current, proposed, Relation.Segment(axis, 2, 3)), out _, out _, out _, out _));
        Assert.True(IntervalFocusSettlementSurface.TryReadContinuousCorrection(
            Relation.OrderedGroup(new Carrier(), current, proposed, Relation.Segment(axis, 3, 2)), out _, out _, out _, out _));
    }
}
