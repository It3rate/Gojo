using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QuickDraw;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QuickDrawAnnotationEvidenceTests
{
    [Fact]
    public void TransportRetainsActualSampleAliasesAndFullUnaccountedIntervals()
    {
        var surface = Build([[[0, 0], [5, 2], [10, 0]], [[10, 0], [10, 20]]]);
        var read = Read(Restore(surface));
        Assert.Equal(2, read.Profiles.Length);
        Assert.Equal(2, read.Lines.Length);
        foreach (var profile in read.Profiles)
        {
            Assert.True(ReadWitness.TryRead(profile.Witness, out var selected, out var basis, out _, out var residue, out var provenance));
            Assert.Same(profile.Source.Selection, selected);
            Assert.Same(selected, residue);
            Assert.Same(profile.Source.Witness, provenance);
            Assert.Same(Entries(read.Surface)[2], basis);
            var samples = Entries(profile.Source.Selection.Right).Cast<Relation>().ToArray();
            Assert.Same(samples[0].Left, profile.Curve.From);
            Assert.Same(samples[^1].Left, profile.Curve.To);
        }
        Assert.Equal(3, Entries(read.Profiles[0].Source.Selection.Right).Length);
        Assert.NotSame(read.Profiles[0].Curve.To, read.Profiles[1].Curve.From);
        Assert.Equal(read.Profiles[0].Curve.Controls[^1], read.Profiles[1].Curve.Controls[0]);
        Assert.True(ConstructionLadder.TryExecute(Restore(read.Interpretation.Program), out var executed));
        Assert.Equal(read.Interpretation.Construction.Sites.Select(site => site.Position), executed.Sites.Select(site => site.Position));
    }

    [Fact]
    public void WiderAngularReadChangesYToTWithoutMovingTheProposalOrJoiningPenLifts()
    {
        var profiles = Arms(0, 165, 90);
        var strict = Read(Build(profiles, angle: 10, joint: Joint(profiles.Length)));
        var loose = Read(Build(profiles, angle: 20, joint: Joint(profiles.Length)));
        Assert.Equal("Y", Assert.Single(strict.Joints).Kind);
        Assert.Equal("T", Assert.Single(loose.Joints).Kind);
        Assert.False(loose.Joints[0].ActualSharedSite);
        Assert.Equal(0, loose.Joints[0].Gap);
        Assert.Equal(strict.Interpretation.Construction.Sites.Select(site => site.Position), loose.Interpretation.Construction.Sites.Select(site => site.Position));
        Assert.Equal(strict.Profiles.SelectMany(profile => profile.Curve.Controls), loose.Profiles.SelectMany(profile => profile.Curve.Controls));
    }

    [Fact]
    public void DisplayLabelsCannotSupplyAJointKind()
    {
        var profiles = Arms(0, 90, 180);
        var read = Read(Build(profiles, joint: Joint(profiles.Length, label: "O fabricated closed circle")));
        Assert.Equal("T", Assert.Single(read.Joints).Kind);
        Assert.All(read.Lines, line => Assert.True(line.Straight));
    }

    [Theory]
    [InlineData(0, 0, 180)]
    [InlineData(0, 0.5, 180)]
    public void DuplicateRayDirectionsRemainAmbiguousWithAllActualParticipants(double first, double second, double third)
    {
        var read = Read(Build(Arms(first, second, third), angle: 32, joint: Joint(3)));
        var joint = Assert.Single(read.Joints);
        Assert.Equal("coincident rays", joint.Kind);
        Assert.Equal(3, joint.Rays.Length);
        Assert.Equal(3, joint.Profiles.Select(profile => profile.Source.Selection).Distinct().Count());
        Assert.Equal(1000, read.Policy.CoincidentAllowance);
    }

    [Fact]
    public void NarrowDistinctRaysDoNotUseTheBroadInterpretationToleranceAsCoincidence()
    {
        var joint = Assert.Single(Read(Build(Arms(0, 10, 180), angle: 32, joint: Joint(3))).Joints);
        Assert.Equal("T", joint.Kind);
        Assert.Equal(2, joint.OpposedPairs);
    }

    [Fact]
    public void TwoOpposedPairsSharingOneRayAreKWhileDisjointPairsAreX()
    {
        var k = Assert.Single(Read(Build(Arms(0, 150, 200, 290), angle: 32, joint: Joint(4))).Joints);
        var x = Assert.Single(Read(Build(Arms(0, 90, 180, 270), angle: 32, joint: Joint(4))).Joints);
        Assert.Equal(2, k.OpposedPairs);
        Assert.Equal("K", k.Kind);
        Assert.Equal("X", x.Kind);
    }

    [Fact]
    public void JointGapMeasuresParticipantDiameterInsteadOfDistanceToAnInventedCenter()
    {
        double[][][] profiles = [[[-6, 0], [-26, 0]], [[6, 0], [26, 0]]];
        var joint = Assert.Single(Read(Build(profiles, proximity: 10, joint: Joint(2))).Joints);
        Assert.Equal(12000, joint.Gap);
        Assert.Equal("unresolved", joint.Kind);
        Assert.False(joint.ActualSharedSite);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InteriorParticipantsRoundedToEndpointCoordinatesDoNotAcquireSourceEventIdentity(bool claimEndpoint)
    {
        object? claimed = claimEndpoint ? new { strokeIndex = 0, sampleIndex = 0 } : null;
        var candidate = new {
            spans = new[] { "s0" }, point = new[] { 0, 0 },
            rays = new[] { new { spanId = "s0", t = 0.00004, toward = "end" } },
            participants = new[] {
                new { spanId = "s0", t = 0.00004, sourceSite = claimed },
                new { spanId = "s0", t = 0.00004, sourceSite = claimed }
            }
        };
        var read = Read(Restore(Build([[[0, 0], [50, 0]]], joint: candidate)));
        var joint = Assert.Single(read.Joints);
        Assert.Equal(2, joint.Participants.Length);
        Assert.All(joint.Participants, participant => {
            Assert.Equal(0, participant.At); // Finite parameter projection reaches zero.
            Assert.Equal(read.Profiles[0].Curve.Controls[0], participant.Position);
            Assert.Null(participant.Endpoint); // The original focus was still interior.
        });
        Assert.Equal(joint.Participants[0].Position, joint.Participants[1].Position);
        Assert.False(joint.ActualSharedSite);
    }

    [Fact]
    public void ExplicitExactSourceSiteAttestationRetainsItsActualAcquiredEventAfterTransport()
    {
        var candidate = new {
            spans = new[] { "s0" }, point = new[] { 0, 0 },
            rays = new[] { new { spanId = "s0", t = 0, toward = "end" } },
            participants = new[] {
                new { spanId = "s0", t = 0, sourceSite = new { strokeIndex = 0, sampleIndex = 0 } },
                new { spanId = "s0", t = 0, sourceSite = new { strokeIndex = 0, sampleIndex = 0 } }
            }
        };
        var read = Read(Restore(Build([[[0, 0], [50, 0]]], joint: candidate)));
        var joint = Assert.Single(read.Joints);
        Assert.True(joint.ActualSharedSite);
        Assert.All(joint.Participants, participant => Assert.Same(read.Profiles[0].Curve.From, participant.Endpoint));
    }

    [Fact]
    public void CurveRayUsesLocalDerivativeRatherThanItsDiagonalChord()
    {
        double[][][] profiles = [[[0, 0], [0, 10], [10, 10]], [[0, 0], [20, 0]]];
        var joint = Assert.Single(Read(Build(profiles, joint: Joint(2))).Joints);
        Assert.Equal("L", joint.Kind);
        Assert.Equal(1, joint.PerpendicularPairs);
        Assert.Equal(0, joint.Rays[0].Direction.X);
        Assert.True(joint.Rays[0].Direction.Y > 0);
    }

    [Fact]
    public void MirrorReadKeepsReversalLocalScopeAndUnmatchedWholeScopeParticipation()
    {
        double[][][] profiles = [[[-10, -20], [-10, 20]], [[10, 20], [10, -20]], [[30, 30], [40, 30]]];
        var local = Mirror(["s0", "s1"], []);
        var whole = Mirror(["s0", "s1"], ["s2"]);
        var read = Read(Restore(Build(profiles, mirrors: [local, whole])));
        Assert.Equal(0, read.Symmetries[0].MaximumResidual);
        Assert.Equal(0, read.Symmetries[1].MaximumResidual);
        Assert.Equal(0, read.Symmetries[0].Unmatched);
        Assert.Equal(1, read.Symmetries[1].Unmatched);
        ReadWitness.TryRead(read.Symmetries[0].Witness, out var localScope, out _, out _, out _, out _);
        ReadWitness.TryRead(read.Symmetries[1].Witness, out var wholeScope, out _, out var pairs, out var unmatched, out _);
        Assert.Equal(2, Entries(localScope).Length);
        Assert.Equal(3, Entries(wholeScope).Length);
        Assert.Same(read.Profiles[2].Witness, Assert.Single(Entries(unmatched)));
        ReadWitness.TryRead(Assert.IsType<Relation>(Assert.Single(Entries(pairs))), out var picked, out _, out _, out _, out _);
        var order = Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(picked).Right).Right);
        Assert.Same(read.Profiles[1].Curve.To, order.Left);
        Assert.Same(read.Profiles[1].Curve.From, order.Right);
    }

    [Fact]
    public void MirrorCannotDropDeclaredUnmatchedParticipation()
    {
        double[][][] profiles = [[[-10, -20], [-10, 20]], [[10, 20], [10, -20]], [[30, 30], [40, 30]]];
        var fields = Entries(Build(profiles, mirrors: [Mirror(["s0", "s1"], ["s2"])]));
        var rows = Entries(fields[7]);
        ReadWitness.TryRead((Relation)rows[0], out var selected, out var basis, out var result, out _, out var provenance);
        rows[0] = ReadWitness.Create(new Carrier(), selected, basis, result, G(), provenance);
        fields[7] = G(rows);
        Assert.False(QuickDrawAnnotationEvidence.TryRead(G(fields), out _));
    }

    [Fact]
    public void EqualLookingSourceSelectionCannotBorrowAnActualIntervalAlias()
    {
        var fields = Entries(Build(Arms(0, 90)));
        var profiles = Entries(fields[3]);
        ReadWitness.TryRead((Relation)profiles[0], out var selected, out var basis, out var result, out _, out var provenance);
        var selection = (Relation)selected;
        var copied = new Relation(selection.Left, selection.Right);
        profiles[0] = ReadWitness.Create(new Carrier(), copied, basis, result, copied, provenance);
        fields[3] = G(profiles);
        Assert.False(QuickDrawAnnotationEvidence.TryRead(G(fields), out _));
    }

    [Fact]
    public void MeasuredPairResidueCannotBeRelabelledAsAnExactRightAngle()
    {
        var fields = Entries(Build(Arms(0, 80), joint: Joint(2)));
        var joints = Entries(fields[6]);
        ReadWitness.TryRead((Relation)joints[0], out var selected, out var basis, out var result, out var residue, out var provenance);
        var body = Entries(result); var pairs = Entries(body[1]);
        ReadWitness.TryRead((Relation)pairs[0], out var ps, out var pb, out var pr, out var prem, out var pp);
        var measures = Entries(pr); var actual = Assert.IsType<CarrierLocation>(measures[2]);
        Assert.True(actual.Ticks > 0);
        measures[2] = new CarrierLocation(actual.Carrier, 0);
        pairs[0] = ReadWitness.Create(new Carrier(), ps, pb, G(measures), prem, pp);
        body[1] = G(pairs);
        joints[0] = ReadWitness.Create(new Carrier(), selected, basis, G(body), residue, provenance);
        fields[6] = G(joints);
        Assert.False(QuickDrawAnnotationEvidence.TryRead(G(fields), out _));
    }

    private static QuickDrawAnnotationEvidence.Read Read(Relation surface)
    { Assert.True(QuickDrawAnnotationEvidence.TryRead(surface, out var read)); return read; }

    private static double[][][] Arms(params double[] angles) => angles.Select(angle => new[] {
        new[] { 0.0, 0.0 }, new[] { Math.Round(50 * Math.Cos(angle * Math.PI / 180), 3), Math.Round(50 * Math.Sin(angle * Math.PI / 180), 3) }
    }).ToArray();

    private static object Joint(int count, string label = "intentionally ignored") => new {
        label, spans = Enumerable.Range(0, count).Select(i => $"s{i}").ToArray(), point = new[] { 0, 0 },
        rays = Enumerable.Range(0, count).Select(i => new { spanId = $"s{i}", t = 0, toward = "end" }).ToArray(),
        participants = Enumerable.Range(0, count).Select(i => new { spanId = $"s{i}", t = 0 }).ToArray()
    };

    private static object Mirror(string[] spans, string[] unmatched) => new {
        spans, unmatchedSpanIds = unmatched, axis = new { from = new[] { 0, -50 }, to = new[] { 0, 50 } },
        correspondences = new[] { new { source = "s0", target = "s1", reversed = true } }
    };

    private static Relation Build(double[][][] profiles, double angle = 10, double proximity = 4, object? joint = null, object[]? mirrors = null)
    {
        object Coordinate(double value) => new { origin = "envelope-a", from = "envelope-a", to = "envelope-b", numerator = (long)Math.Round(value * 1000), denominator = 512000 };
        var sites = new List<object> { new { id = "envelope-a", at = 0, x = 0, y = 0 }, new { id = "envelope-b", at = 0, x = 512, y = 512 } };
        var edges = new List<object> { new { id = "rough", at = 0, controls = new[] { "envelope-a", "envelope-b" } } };
        var rungs = new List<object> {
            new { id = "rough", visible = new[] { "rough" }, guides = Array.Empty<string>(), selected = Array.Empty<string>(), introduced = new[] { "rough" } },
            new { id = "outline", visible = new[] { "rough" }, guides = Array.Empty<string>(), selected = Array.Empty<string>(), introduced = Array.Empty<string>() }
        };
        for (var i = 0; i < profiles.Length; i++)
        {
            sites.Add(new { id = $"a{i}", at = i + 2, x = Coordinate(profiles[i][0][0]), y = Coordinate(profiles[i][0][1]) });
            sites.Add(new { id = $"b{i}", at = i + 2, x = Coordinate(profiles[i][^1][0]), y = Coordinate(profiles[i][^1][1]) });
            edges.Add(new { id = $"edge{i}", at = i + 2, controls = new[] { $"a{i}", $"b{i}" } });
            rungs.Add(new { id = $"stroke{i}", visible = Enumerable.Range(0, i + 1).Select(k => $"edge{k}").ToArray(), guides = new[] { "rough" }, selected = i == 0 ? new[] { "rough" } : [], introduced = new[] { $"edge{i}" } });
        }
        var program = new {
            id = "fixture", family = "fixture", viewBox = new[] { 0, 0, 512, 512 }, parameters = Array.Empty<object>(), sites, edges, rungs,
            constraints = Array.Empty<object>(), repeats = Array.Empty<object>(), circular = Array.Empty<object>(),
            sourceMapping = new { sourceId = "fixture", envelopeSiteIds = new[] { "envelope-a", "envelope-b" }, spanMap = Enumerable.Range(0, profiles.Length).ToDictionary(i => $"s{i}", i => $"edge{i}") }
        };
        var points = profiles.SelectMany(p => p).ToArray();
        var packet = new {
            source = new { key_id = "fixture", drawing = profiles.Select(p => new[] { p.Select(v => v[0]).ToArray(), p.Select(v => v[1]).ToArray(), p.Select((_, i) => (double)(p.Length - i)).ToArray() }).ToArray() },
            observation = new { sourceId = "fixture", bounds = new[] { points.Min(p => p[0]), points.Min(p => p[1]), points.Max(p => p[0]), points.Max(p => p[1]) },
                spans = profiles.Select((p, i) => new { id = $"s{i}", strokeIndex = i, startIndex = 0, endIndex = p.Length - 1, controls = p }).ToArray() },
            program,
            annotations = new {
                policy = new { angleDegrees = angle, proximity, linearity = 4, reflectionDistance = 6, repetitionDistance = 5, minSpanLength = 12, coincidentDegrees = 1 },
                lines = profiles.Select((_, i) => new { spans = new[] { $"s{i}" }, label = "false display label" }).ToArray(),
                joints = joint is null ? Array.Empty<object>() : new[] { joint }, symmetries = mirrors ?? [], relations = Array.Empty<object>()
            }
        };
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(packet));
        return QuickDrawAnnotationEvidence.Import(document.RootElement);
    }

    private static Relation Restore(Relation surface)
    {
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("anonymous", surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored));
        return Assert.IsType<Relation>(restored.Root);
    }
    private static IElement[] Entries(IElement element)
    { Assert.True(Assert.IsType<Relation>(element).TryReadOrderedGroup(out var entries)); return entries; }
    private static Relation G(params IElement[] entries) => Relation.OrderedGroup(new Carrier(), entries);
}
