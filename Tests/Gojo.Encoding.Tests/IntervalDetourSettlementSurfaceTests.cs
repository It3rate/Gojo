using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class IntervalDetourSettlementSurfaceTests
{
    [Fact]
    public void ObstacleWithoutBranchLeavesBlockedDirectTension()
    {
        var sample = CreateSample(includeBranch: false);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(sample.Ledger, out var graph));
        Assert.True(IntervalDetourSettlementSurface.TryCreateSettlement(
            graph,
            sample.Focus,
            sample.Context,
            new Carrier(),
            out var settlement));

        Assert.True(IntervalDetourSettlementSurface.TryReadBlocked(settlement, out var read));
        Assert.Equal(0, read.CurrentTicks);
        Assert.Equal(12, read.GoalTicks);
        Assert.Equal(5, read.ObstacleStartTicks);
        Assert.Equal(7, read.ObstacleEndTicks);
        Assert.True(read.PrimaryCarrier.SameIdentityAs(sample.Primary));
        Assert.Same(sample.Obstacle, read.Obstacle);
        Assert.False(IntervalDetourSettlementSurface.TryReadDetour(settlement, out _));
    }

    [Fact]
    public void BlockedPrimaryMotionCanSettleThroughBranchCarrier()
    {
        var sample = CreateSample(includeBranch: true);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(sample.Ledger, out var graph));
        Assert.True(IntervalDetourSettlementSurface.TryCreateSettlement(
            graph,
            sample.Focus,
            sample.Context,
            new Carrier(),
            out var settlement));

        Assert.True(IntervalDetourSettlementSurface.TryReadDetour(settlement, out var read));
        Assert.Equal(0, read.CurrentTicks);
        Assert.Equal(12, read.GoalTicks);
        Assert.Equal(4, read.EntryTicks);
        Assert.Equal(8, read.ExitTicks);
        Assert.Equal(0, read.BranchEntryTicks);
        Assert.Equal(10, read.BranchExitTicks);
        Assert.Equal(5, read.ObstacleStartTicks);
        Assert.Equal(7, read.ObstacleEndTicks);
        Assert.True(read.PrimaryCarrier.SameIdentityAs(sample.Primary));
        Assert.True(read.BranchCarrier.SameIdentityAs(sample.Branch));
        Assert.Same(sample.BranchCandidate, read.BranchCandidate);
        AssertDetourPath(read.Path, sample.Primary, sample.Branch);
    }

    [Fact]
    public void BlockedPrimaryMotionCanSettleThroughPositiveOutletBranch()
    {
        var sample = CreatePositiveOutletSample();

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(sample.Ledger, out var graph));
        Assert.True(IntervalDetourSettlementSurface.TryCreateSettlement(
            graph,
            sample.Focus,
            sample.Context,
            new Carrier(),
            out var settlement));

        Assert.True(IntervalDetourSettlementSurface.TryReadDetour(settlement, out var read));
        Assert.Equal(0, read.CurrentTicks);
        Assert.Equal(12, read.GoalTicks);
        Assert.Equal(4, read.EntryTicks);
        Assert.Equal(8, read.ExitTicks);
        Assert.Equal(0, read.BranchEntryTicks);
        Assert.Equal(10, read.BranchExitTicks);
        Assert.True(read.PrimaryCarrier.SameIdentityAs(sample.Primary));
        Assert.True(read.BranchCarrier.SameIdentityAs(sample.Branch));
        Assert.Same(sample.BranchCandidate, read.BranchCandidate);
        AssertPositiveOutletDetourPath(read.Path, sample.Primary, sample.Branch);
    }

    [Fact]
    public void DetourSettlementCanReloadFromCompactJson()
    {
        var generated = ConceptDefinitionInterchangeJson.Serialize(CompactDetourDocument());
        var fixture = File.ReadAllText(IntervalDetourSettlementCompactFixturePath());
        var reread = ConceptDefinitionInterchangeJson.Deserialize(fixture);
        var rewritten = ConceptDefinitionInterchangeJson.Serialize(reread);

        AssertJsonEquivalent(fixture, generated);
        AssertJsonEquivalent(generated, rewritten);

        var loaded = ConceptDefinitionInterchangeJson.ToLabeledLedger(reread);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(loaded.Ledger, out var graph));
        Assert.True(IntervalDetourSettlementSurface.TryCreateSettlement(
            graph,
            loaded.SingleDefinition("focus"),
            loaded.SingleDefinition("blocked route context"),
            new Carrier(),
            out var settlement));
        Assert.True(IntervalDetourSettlementSurface.TryReadDetour(settlement, out var read));

        Assert.Equal(0, read.CurrentTicks);
        Assert.Equal(12, read.GoalTicks);
        Assert.Equal(4, read.EntryTicks);
        Assert.Equal(8, read.ExitTicks);
        Assert.Equal(5, read.ObstacleStartTicks);
        Assert.Equal(7, read.ObstacleEndTicks);
        AssertContainsNoEncodedLabelWords(loaded.Ledger, reread);
    }

    [Fact]
    public void PositiveOutletDetourSettlementCanReloadFromCompactJson()
    {
        var generated = ConceptDefinitionInterchangeJson.Serialize(CompactPositiveOutletDocument());
        var fixture = File.ReadAllText(IntervalDetourPositiveOutletCompactFixturePath());
        var reread = ConceptDefinitionInterchangeJson.Deserialize(fixture);
        var rewritten = ConceptDefinitionInterchangeJson.Serialize(reread);

        AssertJsonEquivalent(fixture, generated);
        AssertJsonEquivalent(generated, rewritten);

        var loaded = ConceptDefinitionInterchangeJson.ToLabeledLedger(reread);

        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(loaded.Ledger, out var graph));
        Assert.True(IntervalDetourSettlementSurface.TryCreateSettlement(
            graph,
            loaded.SingleDefinition("focus"),
            loaded.SingleDefinition("positive outlet route context"),
            new Carrier(),
            out var settlement));
        Assert.True(IntervalDetourSettlementSurface.TryReadDetour(settlement, out var read));

        Assert.Equal(0, read.CurrentTicks);
        Assert.Equal(12, read.GoalTicks);
        Assert.Equal(4, read.EntryTicks);
        Assert.Equal(8, read.ExitTicks);
        Assert.True(IntervalDetourSettlementSurface.TryReadPositiveOutlet(
            MiddlePathElement(read.Path),
            read.PrimaryCarrier,
            out var sourceStart,
            out var sourceEnd,
            out var branchCarrier,
            out var branchEntry,
            out var branchExit));
        Assert.Equal(4, sourceStart);
        Assert.Equal(8, sourceEnd);
        Assert.True(branchCarrier.SameIdentityAs(read.BranchCarrier));
        Assert.Equal(0, branchEntry);
        Assert.Equal(10, branchExit);
        AssertContainsNoEncodedLabelWords(loaded.Ledger, reread);
    }

    private static DetourSample CreateSample(bool includeBranch)
    {
        var primary = new Carrier();
        var branch = new Carrier();
        var definitionFrame = new Carrier();
        var definitions = new List<Relation>();

        Relation Add(string name, IElement body, IReadOnlyList<IElement>? dependencies = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                definitionFrame,
                name,
                body,
                dependencies);
            definitions.Add(definition);
            return definition;
        }

        var focus = Add("focus", Relation.Point(primary, 0));
        var obstacle = Relation.Segment(primary, 5, 7);
        var branchCandidate = IntervalDetourSettlementSurface.BranchCandidate(
            new Carrier(),
            primary,
            entryTicks: 4,
            branch,
            branchEntryTicks: 0,
            branchExitTicks: 10,
            exitTicks: 8);
        var contextElements = includeBranch
            ? new IElement[] { Relation.Point(primary, 12), obstacle, branchCandidate }
            : [Relation.Point(primary, 12), obstacle];
        var context = Add(
            "blocked route context",
            Relation.OrderedGroup(new Carrier(), contextElements),
            [focus]);
        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        return new DetourSample(ledger, primary, branch, obstacle, branchCandidate, context, focus);
    }

    private static DetourSample CreatePositiveOutletSample()
    {
        var primary = new Carrier();
        var branch = new Carrier();
        var definitionFrame = new Carrier();
        var definitions = new List<Relation>();

        Relation Add(string name, IElement body, IReadOnlyList<IElement>? dependencies = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                definitionFrame,
                name,
                body,
                dependencies);
            definitions.Add(definition);
            return definition;
        }

        var focus = Add("focus", Relation.Point(primary, 0));
        var obstacle = Relation.Segment(primary, 5, 7);
        var branchCandidate = IntervalDetourSettlementSurface.PositiveOutletBranchCandidate(
            new Carrier(),
            primary,
            entryTicks: 4,
            branch,
            branchEntryTicks: 0,
            branchExitTicks: 10,
            exitTicks: 8);
        var context = Add(
            "positive outlet route context",
            Relation.OrderedGroup(new Carrier(), Relation.Point(primary, 12), obstacle, branchCandidate),
            [focus]);
        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), definitions);

        return new DetourSample(ledger, primary, branch, obstacle, branchCandidate, context, focus);
    }

    private static ConceptDefinitionInterchangeDocument CompactDetourDocument() =>
        new()
        {
            SourceId = "grammar/interval-detour-settlement",
            Carriers = ["primary-route", "detour-branch"],
            Definitions =
            [
                new()
                {
                    Id = "blocked route context",
                    Body = new()
                    {
                        Kind = "group",
                        Elements =
                        [
                            new()
                            {
                                Kind = "point",
                                Carrier = "primary-route",
                                Ticks = 12
                            },
                            new()
                            {
                                Kind = "segment",
                                Carrier = "primary-route",
                                Start = 5,
                                End = 7
                            },
                            new()
                            {
                                Kind = "group",
                                Elements =
                                [
                                    new()
                                    {
                                        Kind = "relation",
                                        Left = new()
                                        {
                                            Kind = "point",
                                            Carrier = "primary-route",
                                            Ticks = 4
                                        },
                                        Right = new()
                                        {
                                            Kind = "point",
                                            Carrier = "detour-branch",
                                            Ticks = 0
                                        }
                                    },
                                    new()
                                    {
                                        Kind = "segment",
                                        Carrier = "detour-branch",
                                        Start = 0,
                                        End = 10
                                    },
                                    new()
                                    {
                                        Kind = "relation",
                                        Left = new()
                                        {
                                            Kind = "point",
                                            Carrier = "detour-branch",
                                            Ticks = 10
                                        },
                                        Right = new()
                                        {
                                            Kind = "point",
                                            Carrier = "primary-route",
                                            Ticks = 8
                                        }
                                    }
                                ]
                            }
                        ]
                    },
                    DependsOn = []
                },
                new()
                {
                    Id = "focus",
                    Body = new()
                    {
                        Kind = "point",
                        Carrier = "primary-route",
                        Ticks = 0
                    },
                    DependsOn = ["blocked route context"]
                }
            ]
        };

    private static ConceptDefinitionInterchangeDocument CompactPositiveOutletDocument() =>
        new()
        {
            SourceId = "grammar/interval-detour-positive-outlet",
            Carriers = ["primary-route", "detour-branch"],
            Definitions =
            [
                new()
                {
                    Id = "positive outlet route context",
                    Body = new()
                    {
                        Kind = "group",
                        Elements =
                        [
                            new()
                            {
                                Kind = "point",
                                Carrier = "primary-route",
                                Ticks = 12
                            },
                            new()
                            {
                                Kind = "segment",
                                Carrier = "primary-route",
                                Start = 5,
                                End = 7
                            },
                            new()
                            {
                                Kind = "group",
                                Elements =
                                [
                                    new()
                                    {
                                        Kind = "relation",
                                        Left = new()
                                        {
                                            Kind = "point",
                                            Carrier = "primary-route",
                                            Ticks = 4
                                        },
                                        Right = new()
                                        {
                                            Kind = "point",
                                            Carrier = "detour-branch",
                                            Face = "inverse",
                                            Ticks = 0
                                        }
                                    },
                                    new()
                                    {
                                        Kind = "relation",
                                        Left = new()
                                        {
                                            Kind = "segment",
                                            Carrier = "primary-route",
                                            Start = 4,
                                            End = 8
                                        },
                                        Right = new()
                                        {
                                            Kind = "directedSegment",
                                            Carrier = "detour-branch",
                                            Start = 0,
                                            End = 10
                                        }
                                    },
                                    new()
                                    {
                                        Kind = "relation",
                                        Left = new()
                                        {
                                            Kind = "point",
                                            Carrier = "detour-branch",
                                            Ticks = 10
                                        },
                                        Right = new()
                                        {
                                            Kind = "point",
                                            Carrier = "primary-route",
                                            Ticks = 8
                                        }
                                    }
                                ]
                            }
                        ]
                    },
                    DependsOn = []
                },
                new()
                {
                    Id = "focus",
                    Body = new()
                    {
                        Kind = "point",
                        Carrier = "primary-route",
                        Ticks = 0
                    },
                    DependsOn = ["positive outlet route context"]
                }
            ]
        };

    private static void AssertDetourPath(IElement element, Carrier primary, Carrier branch)
    {
        var path = Assert.IsType<Relation>(element);
        var holder = Assert.IsType<Carrier>(path.Right);
        Assert.True(path.TryReadOrderedElementsOn(holder, out var elements));
        Assert.Equal(5, elements.Length);
        AssertSegment(elements[0], primary, 0, 4);
        AssertLink(elements[1], primary, 4, branch, 0);
        AssertSegment(elements[2], branch, 0, 10);
        AssertLink(elements[3], branch, 10, primary, 8);
        AssertSegment(elements[4], primary, 8, 12);
    }

    private static void AssertPositiveOutletDetourPath(IElement element, Carrier primary, Carrier branch)
    {
        var path = Assert.IsType<Relation>(element);
        var holder = Assert.IsType<Carrier>(path.Right);
        Assert.True(path.TryReadOrderedElementsOn(holder, out var elements));
        Assert.Equal(5, elements.Length);
        AssertSegment(elements[0], primary, 0, 4);
        AssertLink(elements[1], primary, 4, branch.Inverse(), 0);
        AssertPositiveOutlet(elements[2], primary, branch);
        AssertLink(elements[3], branch, 10, primary, 8);
        AssertSegment(elements[4], primary, 8, 12);
    }

    private static IElement MiddlePathElement(IElement element)
    {
        var path = Assert.IsType<Relation>(element);
        var holder = Assert.IsType<Carrier>(path.Right);
        Assert.True(path.TryReadOrderedElementsOn(holder, out var elements));
        Assert.Equal(5, elements.Length);
        return elements[2];
    }

    private static void AssertPositiveOutlet(IElement element, Carrier primary, Carrier branch)
    {
        var outlet = Assert.IsType<Relation>(element);
        AssertSegment(outlet.Left, primary, 4, 8);
        AssertDirectedSegment(outlet.Right, branch, 0, 10);
    }

    private static void AssertDirectedSegment(IElement element, Carrier branch, long inverseStart, long positiveEnd)
    {
        var segment = Assert.IsType<Relation>(element);
        Assert.True(segment.TryReadDirectedSegment(out var start, out var end));
        Assert.True(start.Carrier.SameIdentityAs(branch.Inverse()));
        Assert.Equal(inverseStart, start.Ticks);
        Assert.True(end.Carrier.SameIdentityAs(branch));
        Assert.Equal(positiveEnd, end.Ticks);
    }

    private static void AssertLink(
        IElement element,
        Carrier leftCarrier,
        long leftTicks,
        Carrier rightCarrier,
        long rightTicks)
    {
        var link = Assert.IsType<Relation>(element);
        AssertPoint(link.Left, leftCarrier, leftTicks);
        AssertPoint(link.Right, rightCarrier, rightTicks);
    }

    private static void AssertSegment(IElement element, Carrier carrier, long start, long end)
    {
        var segment = Assert.IsType<Relation>(element);
        AssertPoint(segment.Left, carrier, start);
        AssertPoint(segment.Right, carrier, end);
    }

    private static void AssertPoint(IElement element, Carrier carrier, long ticks)
    {
        var point = Assert.IsType<Relation>(element);
        var location = Assert.IsType<CarrierLocation>(point.Left);
        var pointCarrier = Assert.IsType<Carrier>(point.Right);
        Assert.True(location.Carrier.SameIdentityAs(carrier));
        Assert.True(pointCarrier.SameIdentityAs(carrier));
        Assert.Equal(ticks, location.Ticks);
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

        if (body.Left is not null)
        {
            foreach (var label in CollectLabels(body.Left))
            {
                yield return label;
            }
        }

        if (body.Right is not null)
        {
            foreach (var label in CollectLabels(body.Right))
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

    private static string IntervalDetourSettlementCompactFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "grammar",
            "interval-detour-settlement.compact.json");

    private static string IntervalDetourPositiveOutletCompactFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "grammar",
            "interval-detour-positive-outlet.compact.json");

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }

    private readonly record struct DetourSample(
        Relation Ledger,
        Carrier Primary,
        Carrier Branch,
        Relation Obstacle,
        Relation BranchCandidate,
        Relation Context,
        Relation Focus);
}
