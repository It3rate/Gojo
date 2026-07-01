using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class IntervalFocusSettlementSurfaceTests
{
    [Fact]
    public void IntervalRoleReadCanProposeLocalFocusCorrection()
    {
        var sample = CreateSample(focusTicks: 10, roleTicks: 1, roleStart: 0, roleEnd: 2);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(sample.Ledger, out var graph));
        Assert.True(IntervalBasisReadSurface.TryRead(graph, out var intervals));
        Assert.Equal("start", Assert.Single(Assert.Single(intervals).Bands).Role);

        Assert.True(IntervalFocusSettlementSurface.TryCreateSettlement(
            graph,
            sample.Focus,
            sample.RoleRead,
            new Carrier(),
            out var settlement));
        Assert.True(IntervalFocusSettlementSurface.TryRead(settlement, out var read));

        Assert.Equal(10, read.CurrentTicks);
        Assert.Equal(0, read.DesiredStartTicks);
        Assert.Equal(2, read.DesiredEndTicks);
        Assert.Equal(2, read.ProposedTicks);
        Assert.True(read.Carrier.SameIdentityAs(sample.Axis));

        Assert.True(ReadWitness.TryRead(
            settlement,
            out var selected,
            out var readSurface,
            out var result,
            out var residueOrTension,
            out var provenance));
        Assert.Same(sample.Focus, selected);
        Assert.Same(sample.RoleRead, readSurface);
        Assert.Same(sample.Interval, provenance);
        AssertPoint(result, sample.Axis, 2);
        AssertCorrection(residueOrTension, sample.Axis, fromTicks: 10, toTicks: 2);
    }

    [Fact]
    public void SameSettlementShapeCanMoveFocusUpwardTowardEndRole()
    {
        var sample = CreateSample(focusTicks: 1, roleTicks: 3, roleStart: 9, roleEnd: 12);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(sample.Ledger, out var graph));
        Assert.True(IntervalFocusSettlementSurface.TryCreateSettlement(
            graph,
            sample.Focus,
            sample.RoleRead,
            new Carrier(),
            out var settlement));
        Assert.True(IntervalFocusSettlementSurface.TryRead(settlement, out var read));

        Assert.Equal(1, read.CurrentTicks);
        Assert.Equal(9, read.DesiredStartTicks);
        Assert.Equal(12, read.DesiredEndTicks);
        Assert.Equal(9, read.ProposedTicks);
        AssertCorrection(read.Correction, sample.Axis, fromTicks: 1, toTicks: 9);
    }

    [Fact]
    public void SalientSettlementChoosesNearestContinuousRegion()
    {
        var sample = CreateSalientSample(focusTicks: 4);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(sample.Ledger, out var graph));
        Assert.True(IntervalFocusSettlementSurface.TryCreateSalientSettlement(
            graph,
            sample.Focus,
            sample.SalientSet,
            new Carrier(),
            out var settlement));
        Assert.True(IntervalFocusSettlementSurface.TryReadSalient(settlement, out var read));

        Assert.Same(sample.CenterBand, read.Candidate);
        Assert.Equal(4, read.CurrentTicks);
        Assert.Equal(5, read.ProposedTicks);
        Assert.True(IntervalFocusSettlementSurface.TryReadContinuousCorrection(
            read.Correction,
            out var carrier,
            out var currentTicks,
            out var proposedTicks,
            out _));
        Assert.True(carrier.SameIdentityAs(sample.Axis));
        Assert.Equal(4, currentTicks);
        Assert.Equal(5, proposedTicks);
        AssertContinuousCorrection(read.Correction, sample.Axis, fromTicks: 4, toTicks: 5);
    }

    [Fact]
    public void SalientSettlementChoosesNearestDiscretePoint()
    {
        var sample = CreateSalientSample(focusTicks: 11);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(sample.Ledger, out var graph));
        Assert.True(IntervalFocusSettlementSurface.TryCreateSalientSettlement(
            graph,
            sample.Focus,
            sample.SalientSet,
            new Carrier(),
            out var settlement));
        Assert.True(IntervalFocusSettlementSurface.TryReadSalient(settlement, out var read));

        Assert.Same(sample.EndPoint, read.Candidate);
        Assert.Equal(11, read.CurrentTicks);
        Assert.Equal(12, read.ProposedTicks);
        Assert.True(IntervalFocusSettlementSurface.TryReadDiscreteCorrection(
            read.Correction,
            out var carrier,
            out var currentTicks,
            out var proposedTicks,
            out _));
        Assert.True(carrier.SameIdentityAs(sample.Axis));
        Assert.Equal(11, currentTicks);
        Assert.Equal(12, proposedTicks);
        AssertCorrection(read.Correction, sample.Axis, fromTicks: 11, toTicks: 12);
    }

    [Fact]
    public void SalientSettlementCanReloadFromCompactJson()
    {
        var generated = ConceptDefinitionInterchangeJson.Serialize(CompactSettlementDocument());
        var fixture = File.ReadAllText(IntervalFocusSettlementCompactFixturePath());
        var reread = ConceptDefinitionInterchangeJson.Deserialize(fixture);
        var rewritten = ConceptDefinitionInterchangeJson.Serialize(reread);

        AssertJsonEquivalent(fixture, generated);
        AssertJsonEquivalent(generated, rewritten);

        var loaded = ConceptDefinitionInterchangeJson.ToLabeledLedger(reread);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(loaded.Ledger, out var graph));
        Assert.True(IntervalFocusSettlementSurface.TryCreateSalientSettlement(
            graph,
            loaded.SingleDefinition("focus"),
            loaded.SingleDefinition("salient interval values"),
            new Carrier(),
            out var settlement));
        Assert.True(IntervalFocusSettlementSurface.TryReadSalient(settlement, out var read));

        Assert.Equal(4, read.CurrentTicks);
        Assert.Equal(5, read.ProposedTicks);
        Assert.True(IntervalFocusSettlementSurface.TryReadContinuousCorrection(
            read.Correction,
            out var carrier,
            out var currentTicks,
            out var proposedTicks,
            out _));
        Assert.True(carrier.SameIdentityAs(read.Carrier));
        Assert.Equal(4, currentTicks);
        Assert.Equal(5, proposedTicks);
        AssertContainsNoEncodedLabelWords(loaded.Ledger, reread);
    }

    [Fact]
    public void InteriorReadExcludesEndpoints()
    {
        var axis = new Carrier();
        var interval = Relation.Segment(axis, 0, 12);

        Assert.True(IntervalBasisReadSurface.TryReadInterior(interval, axis, 6, out var inside));
        Assert.True(inside);
        Assert.True(IntervalBasisReadSurface.TryReadInterior(interval, axis, 0, out inside));
        Assert.False(inside);
        Assert.True(IntervalBasisReadSurface.TryReadInterior(interval, axis, 12, out inside));
        Assert.False(inside);
    }

    private static SettlementSample CreateSample(
        long focusTicks,
        long roleTicks,
        long roleStart,
        long roleEnd)
    {
        var axis = new Carrier();
        var roleAxis = new Carrier();
        var definitionFrame = new Carrier();
        var definitions = new List<Relation>();

        Relation Add(
            string name,
            IElement body,
            IReadOnlyList<IElement>? dependencies = null,
            IElement? residueOrTension = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                definitionFrame,
                name,
                body,
                dependencies,
                residueOrTension: residueOrTension);
            definitions.Add(definition);
            return definition;
        }

        var interval = Add("shared span", Relation.Segment(axis, 0, 12));
        var roleRead = Add(
            "target interval role",
            ReadWitness.Create(
                new Carrier(),
                interval,
                Relation.Point(roleAxis, roleTicks),
                Relation.Segment(axis, roleStart, roleEnd),
                Relation.Point(Carrier.Unknown, 0),
                interval),
            [interval]);
        var focus = Add("focus", Relation.Point(axis, focusTicks), [interval]);
        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        return new SettlementSample(ledger, axis, interval, roleRead, focus);
    }

    private static SalientSettlementSample CreateSalientSample(long focusTicks)
    {
        var axis = new Carrier();
        var unrelatedAxis = new Carrier();
        var definitionFrame = new Carrier();
        var definitions = new List<Relation>();

        Relation Add(
            string name,
            IElement body,
            IReadOnlyList<IElement>? dependencies = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                definitionFrame,
                name,
                body,
                dependencies);
            definitions.Add(definition);
            return definition;
        }

        var interval = Add("shared span", Relation.Segment(axis, 0, 12));
        var startPoint = Relation.Point(axis, 0);
        var centerBand = Relation.Segment(axis, 5, 7);
        var endPoint = Relation.Point(axis, 12);
        var ignoredOtherCarrierPoint = Relation.Point(unrelatedAxis, 4);
        var salientBody = Relation.OrderedGroup(
            new Carrier(),
            startPoint,
            centerBand,
            endPoint,
            ignoredOtherCarrierPoint);
        var salientSet = Add("salient interval values", salientBody, [interval]);
        var focus = Add("focus", Relation.Point(axis, focusTicks), [interval]);
        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        return new SalientSettlementSample(
            ledger,
            axis,
            interval,
            salientSet,
            focus,
            centerBand,
            endPoint);
    }

    private static ConceptDefinitionInterchangeDocument CompactSettlementDocument() =>
        new()
        {
            SourceId = "grammar/interval-focus-settlement",
            Carriers = ["interval-axis", "unrelated-axis"],
            Definitions =
            [
                new()
                {
                    Id = "shared span",
                    Body = new()
                    {
                        Kind = "segment",
                        Carrier = "interval-axis",
                        Start = 0,
                        End = 12
                    },
                    DependsOn = []
                },
                new()
                {
                    Id = "salient interval values",
                    Body = new()
                    {
                        Kind = "group",
                        Elements =
                        [
                            new()
                            {
                                Kind = "point",
                                Carrier = "interval-axis",
                                Ticks = 0
                            },
                            new()
                            {
                                Kind = "segment",
                                Carrier = "interval-axis",
                                Start = 5,
                                End = 7
                            },
                            new()
                            {
                                Kind = "point",
                                Carrier = "interval-axis",
                                Ticks = 12
                            },
                            new()
                            {
                                Kind = "point",
                                Carrier = "unrelated-axis",
                                Ticks = 4
                            }
                        ]
                    },
                    DependsOn = ["shared span"]
                },
                new()
                {
                    Id = "focus",
                    Body = new()
                    {
                        Kind = "point",
                        Carrier = "interval-axis",
                        Ticks = 4
                    },
                    DependsOn = ["shared span"]
                }
            ]
        };

    private static void AssertPoint(IElement element, Carrier carrier, long ticks)
    {
        Assert.True(TryReadPoint(element, out var point));
        Assert.True(point.Carrier.SameIdentityAs(carrier));
        Assert.Equal(ticks, point.Ticks);
    }

    private static void AssertCorrection(IElement element, Carrier carrier, long fromTicks, long toTicks)
    {
        var correction = Assert.IsType<Relation>(element);
        var holder = Assert.IsType<Carrier>(correction.Right);
        Assert.True(correction.TryReadOrderedElementsOn(holder, out var elements));
        Assert.Equal(2, elements.Length);
        AssertPoint(elements[0], carrier, fromTicks);
        AssertPoint(elements[1], carrier, toTicks);
    }

    private static void AssertContinuousCorrection(IElement element, Carrier carrier, long fromTicks, long toTicks)
    {
        var correction = Assert.IsType<Relation>(element);
        var holder = Assert.IsType<Carrier>(correction.Right);
        Assert.True(correction.TryReadOrderedElementsOn(holder, out var elements));
        Assert.Equal(3, elements.Length);
        AssertPoint(elements[0], carrier, fromTicks);
        AssertPoint(elements[1], carrier, toTicks);
        Assert.True(TryReadSegment(elements[2], out var segmentCarrier, out var start, out var end));
        Assert.True(segmentCarrier.SameIdentityAs(carrier));
        Assert.Equal(Math.Min(fromTicks, toTicks), start);
        Assert.Equal(Math.Max(fromTicks, toTicks), end);
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static bool TryReadSegment(IElement element, out Carrier carrier, out long start, out long end)
    {
        if (element is Relation segment &&
            TryReadPoint(segment.Left, out var left) &&
            TryReadPoint(segment.Right, out var right) &&
            left.Carrier.SameIdentityAs(right.Carrier))
        {
            carrier = left.Carrier;
            start = Math.Min(left.Ticks, right.Ticks);
            end = Math.Max(left.Ticks, right.Ticks);
            return true;
        }

        carrier = Carrier.Unknown;
        start = 0;
        end = 0;
        return false;
    }

    private static void AssertContainsNoEncodedLabelWords(
        IElement element,
        ConceptDefinitionInterchangeDocument document)
    {
        var labels = document.Definitions
            .SelectMany(definition => CollectLabels(definition))
            .Where(label => !string.IsNullOrEmpty(label))
            .ToHashSet(StringComparer.Ordinal);
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        Visit(element);

        void Visit(IElement item)
        {
            if (!visited.Add(item))
            {
                return;
            }

            if (EncodedTextSurface.TryRead(item, out var text) && labels.Contains(text))
            {
                Assert.Fail($"Compact reload should keep label word '{text}' outside Gojo structure.");
            }

            if (item is Relation relation)
            {
                Visit(relation.Left);
                Visit(relation.Right);
            }
        }
    }

    private static IEnumerable<string?> CollectLabels(ConceptDefinitionInterchangeDefinition definition)
    {
        yield return definition.Id;
        yield return definition.ResidueOrTension;
        foreach (var label in CollectLabels(definition.Body))
        {
            yield return label;
        }
    }

    private static IEnumerable<string?> CollectLabels(ConceptDefinitionInterchangeBody body)
    {
        yield return body.Text;
        yield return body.Selected;
        yield return body.Context;
        yield return body.ResultText;
        yield return body.ResidueOrTension;
        yield return body.Provenance;
        foreach (var element in body.Elements ?? [])
        {
            foreach (var label in CollectLabels(element))
            {
                yield return label;
            }
        }

        if (body.ReadSurface is not null)
        {
            foreach (var label in CollectLabels(body.ReadSurface))
            {
                yield return label;
            }
        }

        if (body.Result is not null)
        {
            foreach (var label in CollectLabels(body.Result))
            {
                yield return label;
            }
        }
    }

    private static string IntervalFocusSettlementCompactFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "grammar",
            "interval-focus-settlement.compact.json");

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }

    private readonly record struct SettlementSample(
        Relation Ledger,
        Carrier Axis,
        Relation Interval,
        Relation RoleRead,
        Relation Focus);

    private readonly record struct SalientSettlementSample(
        Relation Ledger,
        Carrier Axis,
        Relation Interval,
        Relation SalientSet,
        Relation Focus,
        Relation CenterBand,
        Relation EndPoint);
}
