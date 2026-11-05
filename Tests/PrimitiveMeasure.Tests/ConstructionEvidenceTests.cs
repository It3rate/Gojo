using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ConstructionEvidenceTests
{
    [Fact]
    public void NamesAndIllustrativePixelsCannotChangeEvidenceOrCandidateMatches()
    {
        var originals = ConstructionAtlas.All;
        var renamed = originals.Select(e => e with
        {
            Pattern = "Unrelated caption", Method = "Another label", Description = "No geometric claim",
            Points = e.Points.Select((_, i) => new PenPoint(i % 2, 0)).ToArray()
        }).ToArray();
        for (var i = 0; i < originals.Count; i++)
        {
            var clue = ConstructionEvidence.Read(originals[i].Surface);
            Assert.Equal(clue.Values, ConstructionEvidence.Read(renamed[i].Surface).Values);
            Assert.Equal(ConstructionEvidence.Match(clue, originals), ConstructionEvidence.Match(clue, renamed));
        }
    }

    [Fact]
    public void HidingCluesOnlyAddsCandidatesAndRetainsSelfInEitherGlobalFrame()
    {
        var atlas = ConstructionAtlas.All;
        for (var index = 0; index < atlas.Count; index++)
            foreach (var reverse in new[] { false, true })
            {
                var source = ConstructionEvidence.Read(atlas[index].Surface, reverse);
                var full = ConstructionEvidence.Match(source, atlas, reverse);
                Assert.Contains(index, full);
                foreach (var visibility in Enum.GetValues<ConstructionVisibility>())
                {
                    var observed = source.Publish(visibility);
                    var candidates = ConstructionEvidence.Match(observed, atlas, reverse);
                    Assert.Contains(index, candidates);
                    Assert.All(full, candidate => Assert.Contains(candidate, candidates));
                    for (var slot = 0; slot < source.Values.Count; slot++)
                        Assert.True(observed.Values[slot] is null || observed.Values[slot] == source.Values[slot]);
                }
                var directions = source.Publish(ConstructionVisibility.DirectionsHidden);
                var less = directions.Publish(ConstructionVisibility.GeometryHidden);
                Assert.All(ConstructionEvidence.Match(directions, atlas, reverse), candidate =>
                    Assert.Contains(candidate, ConstructionEvidence.Match(less, atlas, reverse)));
            }

        var path = Index("Square", "Repeated directed path");
        var radial = Index("Square", "Radial parts and chords");
        var pathClues = ConstructionEvidence.Read(atlas[path].Surface);
        Assert.DoesNotContain(radial, ConstructionEvidence.Match(pathClues, atlas));
        Assert.Contains(radial, ConstructionEvidence.Match(pathClues.Publish(ConstructionVisibility.GeometryHidden), atlas));
    }

    [Fact]
    public void DirectionHidingRemovesBranchVersusMergePreferenceWithoutInventingFlow()
    {
        var branch = Example("Y", "Branch reading").Surface;
        var merge = Example("Y", "Merge reading").Surface;
        var first = ConstructionEvidence.Read(branch);
        var second = ConstructionEvidence.Read(merge);
        Assert.Equal(1, first.Values[10]);
        Assert.Equal(0, first.Values[11]);
        Assert.Equal(0, second.Values[10]);
        Assert.Equal(1, second.Values[11]);
        Assert.Equal(first.Values, ConstructionEvidence.Read(merge, reverse: true).Values);
        Assert.True(first.Publish(ConstructionVisibility.DirectionsHidden).CompatibleWith(second));
        Assert.NotEmpty(first.ConflictsWith(second));
    }

    [Fact]
    public void SharedValueRoleIsNotInferredFromEqualParticipatingBits()
    {
        var shared = Square(0, 1, independent: false);
        var independent = Square(0, 1, independent: true);
        var first = ConstructionEvidence.Read(shared);
        var second = ConstructionEvidence.Read(independent);
        Assert.Equal(first.Values.Take(12), second.Values.Take(12));
        Assert.Equal(1, first.Values[12]);
        Assert.Equal(4, second.Values[12]);
        Assert.Equal(1, first.Values[13]);
        Assert.Equal(0, second.Values[13]);
        Assert.Equal(4, first.Values[19]);
        Assert.Equal(4, second.Values[19]);
        Assert.Equal(1, first.Values[17]);
        Assert.Equal(1, first.Values[18]);
        Assert.Null(second.Values[17]);
        Assert.Null(second.Values[18]);
        for (var field = 0; field < 4; field++) Assert.Equal((byte)1, independent.Table.ReadCoarse(independent.Entry, field));
    }

    [Fact]
    public void OpposedSeamEvidenceRequiresSharedLinkIdentity()
    {
        var shared = ConstructionEvidence.Read(Example("House", "Roof and room share seam").Surface);
        var duplicate = ConstructionEvidence.Read(Example("House", "Coincident independent seams").Surface);
        Assert.Equal(1, shared.Values[8]);
        Assert.Equal(1, shared.Values[9]);
        Assert.Equal(0, duplicate.Values[8]);
        Assert.Equal(0, duplicate.Values[9]);
        Assert.Equal(2, shared.Values[7]);
        Assert.Equal(2, duplicate.Values[7]);

        var table = new FamilyTable(new([new(0, 0)], [new(0, 1, 0), new(0, 1, 0)]));
        var entry = table.Append(1, 1);
        var uses = new FamilyUseTable(table.Template, [0], [3]);
        var equalFieldCopy = ConstructionEvidence.Read(new(table, entry, uses));
        Assert.Equal(1, equalFieldCopy.Values[13]);
        Assert.Equal(0, equalFieldCopy.Values[8]);
        Assert.Equal(0, equalFieldCopy.Values[9]);
    }

    [Fact]
    public void AProjectedCrossingDoesNotSupplyASharedJunction()
    {
        var joined = Example("Cross", "Joined crossing");
        var separate = Example("Cross", "Separate projected crossing");
        Assert.Equal(separate.Points[1], separate.Points[4]);
        var first = ConstructionEvidence.Read(joined.Surface);
        var second = ConstructionEvidence.Read(separate.Surface);
        Assert.Equal(4, first.Values[1]);
        Assert.Equal(4, second.Values[1]);
        Assert.Equal(1, first.Values[2]);
        Assert.Equal(2, second.Values[2]);
        Assert.Equal(1, first.Values[4]);
        Assert.Equal(0, second.Values[4]);
        Assert.Equal(5, first.Values[0]);
        Assert.Equal(6, second.Values[0]);
    }

    [Fact]
    public void GeometryNeedsCompleteOneToOneCorrespondenceNotThePresenceOfASourceObject()
    {
        var square = Square(1, 1);
        Assert.Equal((byte)2, square.Basis!.Read(square).Square);
        Assert.Null(ConstructionEvidence.Read(square with { Basis = null }).Values[18]);
        foreach (var (sites, links) in new (int[], int[])[]
        {
            ([0, 1, 2], [0, 1, 2, 3]),
            ([0, 1, 2, 3], [0, 1, 2]),
            ([0, 1, 1, 3], [0, 1, 2, 3]),
            ([0, 1, 2, 3], [0, 1, 2, 2]),
            ([0, 1, 2, 3], [1, 0, 2, 3]),
            ([0, 1, 2, 999], [0, 1, 2, 3])
        })
        {
            var basis = ConstructionBasis.Rails(square.Table, square.Entry, Rails(), sites, links, [0, 0]);
            var incomplete = square with { Basis = basis };
            Assert.False(basis.Corresponds(incomplete));
            Assert.Equal(((byte)3, (byte)3), basis.Read(incomplete));
            var clues = ConstructionEvidence.Read(incomplete);
            Assert.Equal(0, clues.Values[14]);
            Assert.Null(clues.Values[17]);
            Assert.Null(clues.Values[18]);
        }
    }

    [Fact]
    public void GeometryCorrespondenceIsBoundToTheActualOwnerAndEntry()
    {
        var source = Square(1, 1);
        var other = new FamilyTable(source.Table.Template);
        var otherEntry = other.Append(1, 1);
        var otherSurface = new ConstructionSurface(other, otherEntry, source.Uses, source.Basis);
        Assert.False(source.Basis!.Corresponds(otherSurface));
        Assert.Null(ConstructionEvidence.Read(otherSurface).Values[18]);
        var later = source.Table.Append(1, 1);
        var laterSurface = source with { Entry = later };
        Assert.False(source.Basis.Corresponds(laterSurface));
        Assert.Null(ConstructionEvidence.Read(laterSurface).Values[18]);
    }

    [Fact]
    public void ZeroFamilyParticipationCannotOverrideIndependentPositiveRailOrRadialPremises()
    {
        foreach (var construction in new[] { 1, 2 })
        {
            var surface = Square(construction, 0);
            Assert.True(surface.Basis!.Corresponds(surface));
            Assert.Equal(((byte)3, (byte)3), surface.Basis.Read(surface));
            var clues = ConstructionEvidence.Read(surface);
            Assert.Equal(0, clues.Values[19]);
            Assert.Null(clues.Values[17]);
            Assert.Null(clues.Values[18]);
        }
        var path = Square(0, 0);
        Assert.Equal(((byte)1, (byte)1), path.Basis!.Read(path));
        Assert.Equal(0, ConstructionEvidence.Read(path).Values[17]);
        Assert.Equal(0, ConstructionEvidence.Read(path).Values[18]);

        var independent = Square(0, 0, independent: true);
        Assert.Equal(((byte)3, (byte)3), independent.Basis!.Read(independent));
        var table = new FamilyTable(new([new(0, 0)],
            [new(0, 1, 0), new(1, 2, 0), new(2, 3, 0), new(0, 3, 0)]));
        var entry = table.Append(1, 0);
        var broken = new ConstructionSurface(table, entry, new FamilyUseTable(table.Template, [0, 2, 4, 6]),
            ConstructionBasis.Path(table, entry, [0, 1, 2, 3]));
        Assert.True(broken.Basis!.Corresponds(broken));
        Assert.Equal(((byte)3, (byte)3), broken.Basis.Read(broken));
    }

    [Fact]
    public void EmptyOrPhantomRadialCorrespondencesStayIncomplete()
    {
        var bare = new FamilyTable(new([new(0, 0)], []));
        var entry = bare.Append(1, 1);
        var contour = new ContourPattern(new Present(), new Present());
        var empty = new ConstructionSurface(bare, entry, new FamilyUseTable(bare.Template),
            ConstructionBasis.Radial(bare, entry, contour, [], []));
        Assert.False(empty.Basis!.Corresponds(empty));
        Assert.Equal(0, ConstructionEvidence.Read(empty).Values[15]);

        var table = new FamilyTable(new([new(0, 0)],
            [new(1, 2, 0), new(2, 3, 0), new(3, 4, 0), new(4, 1, 0)]));
        var row = table.Append(1, 1);
        var phantom = new ConstructionSurface(table, row, new FamilyUseTable(table.Template, [0, 2, 4, 6]),
            ConstructionBasis.Radial(table, row, RepetitionExamples.Contour(4, true, true),
                [999, 1, 5, 6, 7], [0, 1, 2, 3]));
        Assert.False(phantom.Basis!.Corresponds(phantom));
        Assert.Null(ConstructionEvidence.Read(phantom).Values[18]);
    }

    [Fact]
    public void EmptyUseViewsStillValidateInstanceAndTemplateIdentity()
    {
        var table = new FamilyTable(new([new(0, 0)], []));
        var entry = table.Append(1, 1);
        var sameShape = new FamilyTemplate([new(0, 0)], []);
        var wrong = new ConstructionSurface(table, entry, new FamilyUseTable(sameShape));
        Assert.Throws<ArgumentException>(() => ConstructionEvidence.Read(wrong));
        var missing = new ConstructionSurface(table, new(1, 1), new FamilyUseTable(table.Template));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConstructionEvidence.Read(missing));
        var valid = new ConstructionSurface(table, entry, new FamilyUseTable(table.Template));
        Assert.Equal(0, ConstructionEvidence.Read(valid).Values[0]);
    }

    [Fact]
    public void MaskingChangesOnlyObservationAvailabilityAndLeavesEverySourceByteIntact()
    {
        foreach (var example in ConstructionAtlas.All)
        {
            var surface = example.Surface;
            var before = SourceBytes(surface);
            var clues = ConstructionEvidence.Read(surface);
            var known = clues.KnownBytes();
            var values = clues.PackedBytes();
            foreach (var visibility in Enum.GetValues<ConstructionVisibility>())
            {
                var masked = clues.Publish(visibility);
                _ = masked.KnownBytes();
                _ = masked.PackedBytes();
                _ = ConstructionEvidence.Match(masked, ConstructionAtlas.All);
            }
            Assert.Equal(before, SourceBytes(surface));
            Assert.Equal(known, clues.KnownBytes());
            Assert.Equal(values, clues.PackedBytes());
        }
    }

    private static ConstructionSurface Square(int construction, ulong value, bool independent = false)
    {
        var fields = Enumerable.Repeat(new FamilyField(0, 0), independent ? 4 : 1).ToArray();
        var links = Enumerable.Range(0, 4).Select(i => new FamilyLink(i, (i + 1) % 4, independent ? i : 0)).ToArray();
        var table = new FamilyTable(new(fields, links));
        var entry = table.Append(1, Enumerable.Repeat(value, fields.Length).ToArray());
        var basis = construction switch
        {
            0 => ConstructionBasis.Path(table, entry, [0, 1, 2, 3]),
            1 => ConstructionBasis.Rails(table, entry, Rails(), [0, 1, 2, 3], [0, 1, 2, 3], [0, 0]),
            2 => ConstructionBasis.Radial(table, entry, RepetitionExamples.Contour(4, true, true), [1, 5, 6, 7], [0, 1, 2, 3]),
            _ => throw new ArgumentOutOfRangeException(nameof(construction))
        };
        return new(table, entry, new FamilyUseTable(table.Template, [0, 2, 4, 6]), basis);
    }

    private static OrdinalSketch Rails() => new(new LandmarkRun(1, [0, 1]), new LandmarkRun(1, [0, 1]),
        [[0, 0], [1, 0], [1, 1], [0, 1]], true);

    private static ConstructionExample Example(string pattern, string method) => ConstructionAtlas.All[Index(pattern, method)];
    private static int Index(string pattern, string method) => Enumerable.Range(0, ConstructionAtlas.All.Count)
        .Single(i => ConstructionAtlas.All[i].Pattern == pattern && ConstructionAtlas.All[i].Method == method);

    private static byte[] SourceBytes(ConstructionSurface surface)
    {
        var bytes = new List<byte>(FamilyUseCodec.Encode(surface.Table, surface.Entry, surface.Uses));
        if (surface.Basis is { } basis)
        {
            bytes.AddRange(basis.SiteBytes()); bytes.AddRange(basis.LinkBytes()); bytes.AddRange(basis.GapBytes());
            if (basis.RailSource is { } rails)
            {
                bytes.AddRange(rails.SiteBytes()); bytes.AddRange(rails.AxisBytes());
                bytes.AddRange(rails.Path.EdgeBytes()); bytes.AddRange(rails.Path.UseBytes());
            }
            if (basis.RadialSource is { } radial)
            {
                bytes.AddRange(radial.ArcBytes()); bytes.AddRange(radial.RadialBytes()); bytes.AddRange(radial.WholeBytes());
                bytes.AddRange(radial.ClosureBytes()); bytes.AddRange(radial.ChordBytes());
            }
            if (basis.PathSource is { } path)
                for (var lane = 0; lane < 4; lane++) bytes.AddRange(path.PackedLane(lane));
        }
        return bytes.ToArray();
    }

    private sealed class Present : IPrimitiveReadPort
    {
        public byte ReadSupport() => 2;
    }
}
