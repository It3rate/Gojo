namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One owner-local family instance with selected ordered uses and optional
/// correspondences to declared construction surfaces. Names and pixels are absent.</summary>
public sealed record ConstructionSurface(FamilyTable Table, FamilyEntry Entry,
    FamilyUseTable Uses, ConstructionBasis? Basis = null);

/// <summary>Authored presentation only. The evidence reader accepts Surface,
/// never these labels or illustrative coordinates.</summary>
public sealed record ConstructionExample(string Pattern, string Method, string Description,
    ConstructionSurface Surface, PenPoint[] Points);

/// <summary>Explicit correspondence to existing narrow construction readers.
/// Site and link rows attach their source to this family; no geometric result
/// follows merely from the presence of one of the source objects.</summary>
public sealed class ConstructionBasis
{
    private readonly FamilyTable table;
    private readonly FamilyEntry entry;
    private readonly EndpointRows sites = new(), links = new(), gaps = new();
    private readonly OrdinalSketch? rails;
    private readonly ContourPattern? radial;
    private readonly OrthogonalPathFixture? path;
    public int Bits { get; }
    public OrdinalSketch? RailSource => rails;
    public ContourPattern? RadialSource => radial;
    public OrthogonalPathFixture? PathSource => path;
    public int SiteBindings => sites.Count(2, Bits);
    public int LinkBindings => links.Count(2, Bits);
    public int GapBindings => gaps.Count(2, Bits);
    public int[] SiteRow(int i) => sites.Read(new(2, Bits, i));
    public int[] LinkRow(int i) => links.Read(new(2, Bits, i));
    public int[] GapRow(int i) => gaps.Read(new(2, Bits, i));
    public byte[] SiteBytes() => sites.PackedBytes(2, Bits);
    public byte[] LinkBytes() => links.PackedBytes(2, Bits);
    public byte[] GapBytes() => gaps.PackedBytes(2, Bits);

    private ConstructionBasis(FamilyTable table, FamilyEntry entry, int[] siteMap, int[] linkMap,
        OrdinalSketch? rails = null, ContourPattern? radial = null, OrthogonalPathFixture? path = null,
        int[]? gapFields = null)
    {
        this.table = table; this.entry = entry; this.rails = rails; this.radial = radial; this.path = path;
        var maximum = new[] { table.Template.SiteCount, table.Template.LinkCount, table.Template.FieldCount,
            siteMap.DefaultIfEmpty(0).Max() + 1, linkMap.DefaultIfEmpty(0).Max() + 1 }.Max();
        Bits = OrderedBoundary.Bits(maximum);
        if (Bits > 16 || siteMap.Any(x => x < 0) || linkMap.Any(x => x < 0))
            throw new ArgumentException("Use local nonnegative references within sixteen bits.");
        for (var i = 0; i < siteMap.Length; i++) sites.Append(Bits, i, siteMap[i]);
        for (var i = 0; i < linkMap.Length; i++) links.Append(Bits, i, linkMap[i]);
        if (gapFields is not null)
        {
            if (gapFields.Length != 2 || gapFields.Any(f => f < 0 || f >= table.Template.FieldCount))
                throw new ArgumentException("Bind both whole rail gaps to existing value roles.", nameof(gapFields));
            gaps.Append(Bits, 0, gapFields[0]); gaps.Append(Bits, 1, gapFields[1]);
        }
    }

    public static ConstructionBasis Rails(FamilyTable table, FamilyEntry entry, OrdinalSketch sketch,
        int[] siteMap, int[] linkMap, int[]? gapFields = null) =>
        new(table, entry, siteMap, linkMap, rails: sketch, gapFields: gapFields);

    public static ConstructionBasis Radial(FamilyTable table, FamilyEntry entry, ContourPattern contour,
        int[] siteMap, int[] linkMap) => new(table, entry, siteMap, linkMap, radial: contour);

    public static ConstructionBasis Path(FamilyTable table, FamilyEntry entry, int[] familyLinksInLaneOrder)
    {
        if (familyLinksInLaneOrder.Length != 4 || table.Template.LinkCount != 4 || table.Template.FieldCount > 4 || table.Template.SiteCount != 4 ||
            familyLinksInLaneOrder.Any(i => i < 0 || i >= table.Template.LinkCount) || familyLinksInLaneOrder.Distinct().Count() != 4)
            throw new ArgumentException("The selected quarter-lane fixture requires four local corners and links.");
        var ports = Enumerable.Range(0, table.Template.FieldCount).Select(f => (IPrimitiveReadPort)new FamilyReach(table, entry, f)).ToArray();
        var fixture = new OrthogonalPathFixture(ports);
        var linkMap = new int[table.Template.LinkCount];
        for (var lane = 0; lane < 4; lane++)
        {
            var link = table.Template.Link(familyLinksInLaneOrder[lane]);
            fixture.Connect(lane, link.Start, link.End, 4 + link.Field);
            linkMap[familyLinksInLaneOrder[lane]] = lane;
        }
        return new(table, entry, [0, 1, 2, 3], linkMap, path: fixture);
    }

    private sealed class FamilyReach(FamilyTable table, FamilyEntry entry, int field) : IPrimitiveReadPort
    {
        public byte ReadSupport()
        {
            var word = table.ReadCoarse(entry, field);
            if ((word & ~1) != 0) throw new InvalidOperationException("A reach needs an explicitly positive local basis.");
            return Gojo.Experiments.PrimitiveMeasure.ReadSupport.Of(word);
        }
    }

    /// <summary>Checks the supplied correspondence, including shared sites and
    /// the source edge identity. Missing rows leave the geometric read open.</summary>
    public bool Corresponds(ConstructionSurface surface)
    {
        if (!ReferenceEquals(surface.Table, table) || surface.Entry != entry ||
            SiteBindings != table.Template.SiteCount || LinkBindings != table.Template.LinkCount) return false;
        var mappedSites = Enumerable.Range(0, SiteBindings).Select(i => SiteRow(i)[1]).ToArray();
        var mappedLinks = Enumerable.Range(0, LinkBindings).Select(i => LinkRow(i)[1]).ToArray();
        if (mappedSites.Distinct().Count() != mappedSites.Length || mappedLinks.Distinct().Count() != mappedLinks.Length) return false;
        if (rails is not null && (mappedLinks.Length != rails.Path.EdgeCount || mappedSites.Length != rails.SiteCount)) return false;
        if (radial is not null)
        {
            if (mappedLinks.Length != radial.PartCount || !radial.Read().JoinedChords) return false;
            var radialSites = Enumerable.Range(0, radial.PartCount).SelectMany(i =>
            {
                var arc = radial.ArcRow(i);
                return new[] { arc[0], arc[1] == ContourPattern.End ? ContourPattern.Start : arc[1] };
            }).ToHashSet();
            if (!radialSites.SetEquals(mappedSites)) return false;
        }
        for (var i = 0; i < mappedLinks.Length; i++)
        {
            var link = table.Template.Link(i);
            int[] source;
            if (rails is not null)
            {
                if (mappedSites.Any(s => s >= rails.SiteCount) || mappedLinks[i] >= rails.Path.EdgeCount) return false;
                source = rails.Path.Edge(mappedLinks[i]);
            }
            else if (radial is not null)
            {
                if (mappedLinks[i] >= radial.PartCount) return false;
                var arc = radial.ArcRow(mappedLinks[i]);
                source = [arc[0], arc[1] == ContourPattern.End ? ContourPattern.Start : arc[1]];
            }
            else if (path is not null)
            {
                if (mappedLinks[i] >= 4) return false;
                source = path.Wiring(mappedLinks[i]);
                if (source[2] != link.Field + 4) return false;
            }
            else return false;
            var a = mappedSites[link.Start]; var b = mappedSites[link.End];
            if (!((a == source[0] && b == source[1]) || (a == source[1] && b == source[0]))) return false;
        }
        return true;
    }

    /// <summary>Positive-context support: 2 proves the selected shape, 1 proves
    /// degeneracy through a complete directly bound path, 3 leaves the claim
    /// open. A missing or contradictory source correspondence does not prove
    /// geometric impossibility.</summary>
    public (byte Rectangle, byte Square) Read(ConstructionSurface surface)
    {
        if (!Corresponds(surface)) return (3, 3);
        var used = Enumerable.Range(0, table.Template.LinkCount).Select(i => table.Template.Link(i).Field).Distinct().ToArray();
        if (used.Any(f => (table.ReadCoarse(entry, f) & ~1) != 0)) return (3, 3);
        if (path is not null)
        {
            var read = path.Read();
            if (read.NondegeneracySupport == 1) return (1, 1);
            var rectangle = read.GuaranteesRectangle;
            return (rectangle ? (byte)2 : (byte)3, rectangle && used.Length == 1 ? (byte)2 : (byte)3);
        }
        // Rails and radial contours have independently supplied positive
        // construction premises. Absent family participation does not override
        // those sources or establish that they have settled to a zero extent.
        if (used.Any(f => (table.ReadCoarse(entry, f) & 1) == 0)) return (3, 3);
        if (radial is not null)
        {
            var read = radial.Read();
            return (read.Rectangle ? (byte)2 : (byte)3, read.Square ? (byte)2 : (byte)3);
        }
        var rail = rails!.Read();
        var sharedGap = GapBindings == 2 && GapRow(0)[1] == GapRow(1)[1] && used.Contains(GapRow(0)[1]) &&
            rails.Across.Sites().Length == 2 && rails.Rise.Sites().Length == 2;
        return (rail.Rectangle ? (byte)2 : (byte)3, rail.Rectangle && sharedGap ? (byte)2 : (byte)3);
    }
}
