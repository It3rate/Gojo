using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class SceneMechanicReadFrameSurfaceTests
{
    [Fact]
    public void ReadFrameLedgerDiscoversSuggestedProofWords()
    {
        var sample = CreateSample();

        Assert.True(SceneMechanicReadFrameSurface.TryReadFrames(sample.Ledger, out var reads));
        Assert.Equal(
            [
                "near-read",
                "cross-read",
                "deliver-read",
                "avoid-read",
                "conduct-read",
                "cross-question-read",
                "red-trace-modifier-read"
            ],
            reads.Select(read => read.Definition.Name).ToArray());

        Assert.All(reads, read =>
        {
            Assert.False(IsUnknownPoint(read.Frame.SelectedSourceFrame));
            Assert.False(IsUnknownPoint(read.Frame.FocusWitness));
            Assert.False(IsUnknownPoint(read.Frame.BasisWitness));
            Assert.NotEmpty(read.LexicalDebugLabels);
        });

        Assert.Contains("word:near", Labels(reads, "near-read"));
        Assert.Contains("word:cross", Labels(reads, "cross-read"));
        Assert.Contains("word:deliver", Labels(reads, "deliver-read"));
        Assert.Contains("word:avoid", Labels(reads, "avoid-read"));
        Assert.Contains("word:conduct", Labels(reads, "conduct-read"));
    }

    [Fact]
    public void ReadFramesUseExistingFocusBasisTransformAndGoalWitnesses()
    {
        var sample = CreateSample();

        Assert.True(SceneMechanicReadFrameSurface.TryReadFrames(sample.Ledger, out var reads));
        var near = Frame(reads, "near-read");
        var deliver = Frame(reads, "deliver-read");
        var avoid = Frame(reads, "avoid-read");
        var conduct = Frame(reads, "conduct-read");

        Assert.True(FocusSurface.TryRead(
            Assert.IsType<Relation>(near.FocusWitness),
            out _,
            out var nearFocusBasis,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
        Assert.Same(sample.NearBand, nearFocusBasis);
        Assert.True(BasisReadSurface.TryRead(Assert.IsType<Relation>(near.BasisWitness), out _, out _, out _, out _, out _, out _));
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(near.GoalOrResidueWitness), out _, out _, out var nearResult, out _, out _));
        Assert.Equal("settled inside near band", Text(nearResult));

        Assert.True(TransformSurface.TryRead(Assert.IsType<Relation>(deliver.TransformWitness), out _, out var deliverOperation, out _, out _, out _));
        Assert.Same(sample.DeliveryFlow, deliverOperation);
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(deliver.GoalOrResidueWitness), out _, out _, out var deliverResult, out _, out _));
        Assert.Same(sample.DeliveryTarget, deliverResult);

        Assert.True(TransformSurface.TryRead(Assert.IsType<Relation>(avoid.TransformWitness), out _, out var avoidOperation, out _, out _, out _));
        Assert.Same(sample.DetourPath, avoidOperation);
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(avoid.GoalOrResidueWitness), out _, out _, out var avoidResult, out var avoidResidue, out _));
        Assert.Same(sample.DetourPath, avoidResult);
        Assert.Equal("direct span remains blocked as visible tension", Text(avoidResidue));

        Assert.True(TransformSurface.TryRead(Assert.IsType<Relation>(conduct.TransformWitness), out _, out var conductOperation, out _, out _, out _));
        Assert.Same(sample.BridgeFlow, conductOperation);
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(conduct.GoalOrResidueWitness), out _, out _, out var conductResult, out _, out _));
        Assert.Equal("signal reaches right support", Text(conductResult));
    }

    [Fact]
    public void ModeAndModifierStayAsHintsOnTheSixSlotFrame()
    {
        var sample = CreateSample();

        Assert.True(SceneMechanicReadFrameSurface.TryReadFrames(sample.Ledger, out var reads));
        var question = Frame(reads, "cross-question-read");
        var modifier = Frame(reads, "red-trace-modifier-read");

        Assert.Contains("mode:question", Labels(reads, "cross-question-read"));
        Assert.Contains("missing-slot:goal/result", Labels(reads, "cross-question-read"));
        Assert.Contains("modifier:adjective", Labels(reads, "red-trace-modifier-read"));
        Assert.Contains("surface:red trace", Labels(reads, "red-trace-modifier-read"));

        Assert.True(SceneMechanicReadFrameSurface.TryRead(sample.CrossFrameBody, out var cross));
        Assert.Same(cross.SelectedSourceFrame, question.SelectedSourceFrame);
        Assert.Same(cross.FocusWitness, question.FocusWitness);
        Assert.Same(cross.BasisWitness, question.BasisWitness);
        Assert.Same(cross.TransformWitness, question.TransformWitness);
        Assert.False(IsUnknownPoint(question.GoalOrResidueWitness));

        Assert.True(BasisReadSurface.TryRead(Assert.IsType<Relation>(modifier.BasisWitness), out _, out _, out _, out _, out _, out _));
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(modifier.GoalOrResidueWitness), out _, out _, out var modifierResult, out _, out _));
        Assert.Equal("trace selected with red quality unresolved as surface evidence", Text(modifierResult));
    }

    [Fact]
    public void ReadFrameShapeDoesNotMistakeEverySixSlotSurfaceForAFrame()
    {
        var axis = new Carrier();
        var basis = BasisReadSurface.Create(
            new Carrier(),
            Relation.Segment(axis, 0, 10),
            Relation.Point(axis, 0),
            Relation.Point(axis, 10),
            Relation.Segment(axis, 0, 10));
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), Relation.Point(axis, 0)),
            Relation.DirectedSegment(axis, 0, 10),
            TransformSurface.Ports(new Carrier(), Relation.Point(axis, 10)),
            UnknownPoint(),
            Relation.Segment(axis, 0, 10));

        Assert.False(SceneMechanicReadFrameSurface.TryRead(basis, out _));
        Assert.False(SceneMechanicReadFrameSurface.TryRead(transform, out _));
    }

    [Fact]
    public void DefinitionBackedReadFrameReloadsFromCompactJson()
    {
        var generated = ConceptDefinitionInterchangeJson.Serialize(CompactReadFrameDocument());
        var fixture = File.ReadAllText(ReadFrameCompactFixturePath());
        var reread = ConceptDefinitionInterchangeJson.Deserialize(fixture);
        var rewritten = ConceptDefinitionInterchangeJson.Serialize(reread);

        AssertJsonEquivalent(fixture, generated);
        AssertJsonEquivalent(generated, rewritten);

        var loaded = ConceptDefinitionInterchangeJson.ToLabeledLedger(reread);

        Assert.True(SceneMechanicReadFrameSurface.TryReadFrames(loaded.Ledger, out var reads));
        var read = Assert.Single(reads);

        Assert.Same(loaded.SingleDefinition("near-read"), read.Definition.Definition);
        Assert.Contains("word:near", read.LexicalDebugLabels);
        Assert.Contains("part:preposition/adjective", read.LexicalDebugLabels);
        Assert.Same(loaded.SingleDefinition("near-source"), read.Frame.SelectedSourceFrame);
        Assert.Same(loaded.SingleDefinition("near-focus"), read.Frame.FocusWitness);
        Assert.Same(loaded.SingleDefinition("near-basis"), read.Frame.BasisWitness);
        Assert.Same(loaded.SingleDefinition("near-transform-open"), read.Frame.TransformWitness);
        Assert.Same(loaded.SingleDefinition("near-goal"), read.Frame.GoalOrResidueWitness);
        Assert.Same(loaded.SingleDefinition("near-lexical"), read.Frame.LexicalDebugAttachments);
    }

    private static Sample CreateSample()
    {
        var definitionRoot = new Carrier();
        var definitions = new List<Relation>();

        Relation Add(string name, IElement body)
        {
            var definition = ConceptDefinitionSurface.Create(definitionRoot, name, body);
            definitions.Add(definition);
            return definition;
        }

        var interval = new Carrier();
        var distanceScale = Relation.Segment(interval, 0, 10);
        var nearBand = Relation.Segment(interval, 0, 3);
        var nearFigure = Relation.Point(interval, 2);
        var nearReference = Relation.Point(interval, 0);
        var nearSelected = Frame(distanceScale, nearFigure, nearReference);
        var nearFocus = FocusSurface.Create(new Carrier(), distanceScale, nearBand, nearFigure);
        var nearBasis = BasisReadSurface.Create(new Carrier(), nearSelected, distanceScale, nearBand, distanceScale);
        var nearGoal = ReadWitness.Create(
            new Carrier(),
            nearSelected,
            nearBasis,
            Text("settled inside near band"),
            UnknownPoint(),
            nearFocus);
        Add(
            "near-read",
            SceneMechanicReadFrameSurface.Create(
                new Carrier(),
                nearSelected,
                nearFocus,
                nearBasis,
                UnknownPoint(),
                nearGoal,
                Labels("word:near", "language:en", "part:preposition/adjective", "hint:comparison")));

        var horizontal = new Carrier();
        var vertical = new Carrier();
        var horizontalSpan = Relation.Segment(horizontal, 0, 10);
        var verticalSpan = Relation.Segment(vertical, 0, 10);
        var contact = new Relation(Relation.Point(horizontal, 5), Relation.Point(vertical, 5));
        var crossSelected = Frame(horizontalSpan, verticalSpan, contact);
        var crossFocus = FocusSurface.Create(new Carrier(), crossSelected, contact, contact);
        var crossBasis = BasisReadSurface.Create(new Carrier(), crossSelected, horizontalSpan, verticalSpan, contact);
        var crossFlow = Relation.DirectedSegment(horizontal, 0, 10);
        var crossTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), Relation.Point(horizontal, 0)),
            crossFlow,
            TransformSurface.Ports(new Carrier(), Relation.Point(horizontal, 10)),
            UnknownPoint(),
            contact);
        var crossGoal = ReadWitness.Create(
            new Carrier(),
            crossSelected,
            crossTransform,
            Relation.Point(horizontal, 10),
            UnknownPoint(),
            crossFocus);
        var crossFrameBody = SceneMechanicReadFrameSurface.Create(
            new Carrier(),
            crossSelected,
            crossFocus,
            crossBasis,
            crossTransform,
            crossGoal,
            Labels("word:cross", "language:en", "part:verb/preposition", "hint:intersection"));
        Add("cross-read", crossFrameBody);

        var route = new Carrier();
        var cargo = new Carrier();
        var deliverySource = Relation.Point(route, 0);
        var deliveryTarget = Relation.Point(route, 10);
        var deliveryFlow = Relation.DirectedSegment(route, 0, 10);
        var deliverySelected = Frame(deliverySource, deliveryFlow, deliveryTarget, Relation.Point(cargo, 0));
        var deliveryFocus = FocusSurface.Create(new Carrier(), deliverySelected, deliveryTarget, deliveryFlow);
        var deliveryBasis = BasisReadSurface.Create(new Carrier(), deliverySelected, Relation.Segment(route, 0, 10), deliveryTarget, deliveryFlow);
        var deliveryTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), deliverySource, Relation.Point(cargo, 0)),
            deliveryFlow,
            TransformSurface.Ports(new Carrier(), deliveryTarget, Relation.Point(cargo, 10)),
            UnknownPoint(),
            deliverySelected);
        var deliveryGoal = ReadWitness.Create(
            new Carrier(),
            deliverySelected,
            deliveryTransform,
            deliveryTarget,
            UnknownPoint(),
            deliveryFocus);
        Add(
            "deliver-read",
            SceneMechanicReadFrameSurface.Create(
                new Carrier(),
                deliverySelected,
                deliveryFocus,
                deliveryBasis,
                deliveryTransform,
                deliveryGoal,
                Labels("word:deliver", "language:en", "part:verb", "hint:target-settlement")));

        var primary = new Carrier();
        var branch = new Carrier();
        var routeIntent = Relation.Segment(primary, 0, 12);
        var obstacle = Relation.Segment(primary, 5, 7);
        var detourPath = Frame(
            Relation.Segment(primary, 0, 4),
            new Relation(Relation.Point(primary, 4), Relation.Point(branch, 0)),
            Relation.DirectedSegment(branch, 0, 10),
            new Relation(Relation.Point(branch, 10), Relation.Point(primary, 8)),
            Relation.Segment(primary, 8, 12));
        var avoidSelected = Frame(routeIntent, obstacle, detourPath);
        var avoidFocus = FocusSurface.Create(new Carrier(), avoidSelected, obstacle, detourPath);
        var avoidBasis = BasisReadSurface.Create(new Carrier(), avoidSelected, routeIntent, obstacle, detourPath);
        var avoidTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), routeIntent),
            detourPath,
            TransformSurface.Ports(new Carrier(), Relation.Point(primary, 12)),
            Text("preserve goal while rejecting blocked span"),
            avoidSelected);
        var avoidGoal = ReadWitness.Create(
            new Carrier(),
            avoidSelected,
            avoidTransform,
            detourPath,
            Text("direct span remains blocked as visible tension"),
            avoidFocus);
        Add(
            "avoid-read",
            SceneMechanicReadFrameSurface.Create(
                new Carrier(),
                avoidSelected,
                avoidFocus,
                avoidBasis,
                avoidTransform,
                avoidGoal,
                Labels("word:avoid", "language:en", "part:verb/preposition", "hint:goal-preserving-detour")));

        var leftSupport = new Carrier();
        var rightSupport = new Carrier();
        var bridge = new Carrier();
        var leftSpan = Relation.Segment(leftSupport, 0, 10);
        var rightSpan = Relation.Segment(rightSupport, 0, 10);
        var bridgeFlow = Relation.DirectedSegment(bridge, 0, 10);
        var leftAttach = new Relation(Relation.Point(leftSupport, 5), Relation.Point(bridge, 0));
        var rightAttach = new Relation(Relation.Point(bridge, 10), Relation.Point(rightSupport, 5));
        var conductSelected = Frame(leftSpan, rightSpan, bridgeFlow, leftAttach, rightAttach);
        var conductFocus = FocusSurface.Create(new Carrier(), conductSelected, bridgeFlow, bridgeFlow);
        var conductBasis = BasisReadSurface.Create(new Carrier(), conductSelected, bridgeFlow, Frame(leftAttach, rightAttach), conductSelected);
        var conductTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), leftAttach),
            bridgeFlow,
            TransformSurface.Ports(new Carrier(), rightAttach),
            UnknownPoint(),
            conductSelected);
        var conductGoal = ReadWitness.Create(
            new Carrier(),
            conductSelected,
            conductTransform,
            Text("signal reaches right support"),
            UnknownPoint(),
            conductFocus);
        Add(
            "conduct-read",
            SceneMechanicReadFrameSurface.Create(
                new Carrier(),
                conductSelected,
                conductFocus,
                conductBasis,
                conductTransform,
                conductGoal,
                Labels("word:conduct", "language:en", "part:verb", "hint:bridge-flow")));

        var questionGoal = ReadWitness.Create(
            new Carrier(),
            crossSelected,
            crossBasis,
            UnknownPoint(),
            Text("goal/result slot is requested, not settled"),
            crossFocus);
        Add(
            "cross-question-read",
            SceneMechanicReadFrameSurface.Create(
                new Carrier(),
                crossSelected,
                crossFocus,
                crossBasis,
                crossTransform,
                questionGoal,
                Labels("surface:does it cross?", "mode:question", "missing-slot:goal/result")));

        var trace = new Carrier();
        var traceSpan = Relation.Segment(trace, 0, 10);
        var redEvidence = Text("red surface evidence");
        var modifierSelected = Frame(traceSpan, redEvidence);
        var modifierFocus = FocusSurface.Create(new Carrier(), modifierSelected, traceSpan, traceSpan);
        var modifierBasis = BasisReadSurface.Create(new Carrier(), modifierSelected, redEvidence, traceSpan, modifierSelected);
        var modifierGoal = ReadWitness.Create(
            new Carrier(),
            modifierSelected,
            modifierBasis,
            Text("trace selected with red quality unresolved as surface evidence"),
            UnknownPoint(),
            modifierFocus);
        Add(
            "red-trace-modifier-read",
            SceneMechanicReadFrameSurface.Create(
                new Carrier(),
                modifierSelected,
                modifierFocus,
                modifierBasis,
                UnknownPoint(),
                modifierGoal,
                Labels("surface:red trace", "modifier:adjective", "quality:red", "attachment:trace")));

        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);
        return new Sample(ledger, nearBand, deliveryFlow, deliveryTarget, detourPath, bridgeFlow, crossFrameBody);
    }

    private static SceneMechanicReadFrameSurface.ReadFrameRead Frame(
        SceneMechanicReadFrameSurface.DefinitionRead[] reads,
        string name)
    {
        var read = Assert.Single(reads, item => item.Definition.Name == name);
        return read.Frame;
    }

    private static string[] Labels(
        SceneMechanicReadFrameSurface.DefinitionRead[] reads,
        string name)
    {
        var read = Assert.Single(reads, item => item.Definition.Name == name);
        return read.LexicalDebugLabels;
    }

    private static Relation Frame(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements);

    private static Relation Labels(params string[] labels) =>
        SceneMechanicReadFrameSurface.LexicalDebugFrame(new Carrier(), labels);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);

    private static string Text(IElement element)
    {
        Assert.True(EncodedTextSurface.TryRead(element, out var text));
        return text;
    }

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static bool IsUnknownPoint(IElement element) =>
        element is Relation relation &&
        relation.Left is CarrierLocation location &&
        relation.Right is Carrier carrier &&
        carrier.IsUnknown &&
        location.Carrier.IsUnknown &&
        location.Ticks == 0;

    private static ConceptDefinitionInterchangeDocument CompactReadFrameDocument() =>
        new()
        {
            SourceId = "grammar/scene-mechanic-read-frames",
            Carriers = ["distance-axis", "read-frame-slot-axis"],
            Definitions =
            [
                new()
                {
                    Id = "near-source",
                    Body = new()
                    {
                        Kind = "group",
                        Elements =
                        [
                            new() { Kind = "segment", Carrier = "distance-axis", Start = 0, End = 10 },
                            new() { Kind = "point", Carrier = "distance-axis", Ticks = 2 },
                            new() { Kind = "point", Carrier = "distance-axis", Ticks = 0 }
                        ]
                    },
                    DependsOn = []
                },
                new()
                {
                    Id = "near-focus",
                    Body = new()
                    {
                        Kind = "witness",
                        Selected = "near-source",
                        ReadSurface = new() { Kind = "segment", Carrier = "distance-axis", Start = 0, End = 3 },
                        Result = new() { Kind = "point", Carrier = "distance-axis", Ticks = 2 },
                        ResidueOrTension = "focus on low-distance band",
                        Provenance = "near-source"
                    },
                    DependsOn = ["near-source"],
                    ResidueOrTension = "focus is a read over source structure"
                },
                new()
                {
                    Id = "near-basis",
                    Body = new()
                    {
                        Kind = "group",
                        Elements =
                        [
                            new() { Kind = "segment", Carrier = "distance-axis", Start = 0, End = 10 },
                            new() { Kind = "segment", Carrier = "distance-axis", Start = 0, End = 3 },
                            new() { Kind = "text", Text = "comparison basis" }
                        ]
                    },
                    DependsOn = ["near-source"],
                    ResidueOrTension = "basis stays separate from lexical near"
                },
                new()
                {
                    Id = "near-transform-open",
                    Body = new() { Kind = "point", Carrier = "read-frame-slot-axis", Ticks = 0 },
                    DependsOn = [],
                    ResidueOrTension = "static read has no transform witness yet"
                },
                new()
                {
                    Id = "near-goal",
                    Body = new()
                    {
                        Kind = "witness",
                        Selected = "near-source",
                        ReadSurface = new() { Kind = "definitionRef", Selected = "near-basis" },
                        Result = new() { Kind = "text", Text = "settled inside near band" },
                        ResidueOrTension = "low resolution comparison result",
                        Provenance = "near-focus"
                    },
                    DependsOn = ["near-source", "near-basis", "near-focus"],
                    ResidueOrTension = "goal/result is a read witness"
                },
                new()
                {
                    Id = "near-lexical",
                    Body = new()
                    {
                        Kind = "group",
                        Elements =
                        [
                            new() { Kind = "text", Text = "word:near" },
                            new() { Kind = "text", Text = "language:en" },
                            new() { Kind = "text", Text = "part:preposition/adjective" },
                            new() { Kind = "text", Text = "hint:comparison" }
                        ]
                    },
                    DependsOn = [],
                    ResidueOrTension = "lexical surface is an attachment, not the lower meaning"
                },
                new()
                {
                    Id = "near-read",
                    Body = new()
                    {
                        Kind = "group",
                        Elements =
                        [
                            ReadFrameSlot(1, "near-source"),
                            ReadFrameSlot(2, "near-focus"),
                            ReadFrameSlot(3, "near-basis"),
                            ReadFrameSlot(4, "near-transform-open"),
                            ReadFrameSlot(5, "near-goal"),
                            ReadFrameSlot(6, "near-lexical")
                        ]
                    },
                    DependsOn =
                    [
                        "near-source",
                        "near-focus",
                        "near-basis",
                        "near-transform-open",
                        "near-goal",
                        "near-lexical"
                    ],
                    ResidueOrTension = "six-slot read frame over a bounded carrier mechanic"
                }
            ]
        };

    private static ConceptDefinitionInterchangeBody ReadFrameSlot(long slot, string definition) =>
        new()
        {
            Kind = "relation",
            Left = new() { Kind = "point", Carrier = "read-frame-slot-axis", Ticks = slot },
            Right = new() { Kind = "definitionRef", Selected = definition }
        };

    private static void AssertJsonEquivalent(string expected, string actual) =>
        Assert.True(
            JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)),
            $"Expected JSON equivalent to:\n{expected}\nActual:\n{actual}");

    private static string ReadFrameCompactFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "grammar",
            "scene-mechanic-read-frames.compact.json");

    private sealed record Sample(
        Relation Ledger,
        Relation NearBand,
        Relation DeliveryFlow,
        Relation DeliveryTarget,
        Relation DetourPath,
        Relation BridgeFlow,
        Relation CrossFrameBody);
}
