namespace Gojo.Experiments.PrimitiveMeasure;

public readonly record struct DottedStemRead(bool? Dotted, LandmarkRead FootAgainstLine, bool? Bends)
{
    public bool Fits(DottedStemRead model) => (!Dotted.HasValue || Dotted==model.Dotted) &&
        (FootAgainstLine==LandmarkRead.Unresolved || FootAgainstLine==model.FootAgainstLine) &&
        (!Bends.HasValue || Bends==model.Bends);
}

/// <summary>Two stroke components and two point marks in one page-local
/// basis. Membership and height projection are separate from ink incidence.</summary>
public sealed class WritingLine
{
    private readonly EndpointRows frame = new();
    public int[] UprightFrame => frame.Read(new(2,2,0));
    public LandmarkRun Levels { get; } = new(2,[3,1,0,2]); // descender, baseline, mid-height, dot
    public CurveInk Ink { get; } = new([
        new(0,1,PenHeading.Down,PenHeading.Down,0),
        new(2,3,PenHeading.Down,PenHeading.Left,1)],2,(1,2));
    public OrderedBoundary Stems => Ink.Catalogue;
    private readonly EndpointRows dots = new(), groups = new(), projections = new();

    public WritingLine(bool includeRightDot = true, bool swapMembership = false)
    {
        frame.Append(2,1,0); // baseline -> body height on Levels
        dots.Append(3,4); dots.Append(3,5);
        groups.Append(3,0,swapMembership ? 5 : 4);
        if (includeRightDot) groups.Append(3,2,swapMembership ? 4 : 5);
        // Each row's position is an ink site. Its field is a page level.
        foreach (int level in new[]{0,1,0,3,2,2}) projections.Append(2,level);
    }
    public int[] DotSites() => Enumerable.Range(0,dots.Count(1,3)).Select(i=>dots.Read(new(1,3,i))[0]).ToArray();
    public int[][] Membership() => Enumerable.Range(0,groups.Count(2,3)).Select(i=>groups.Read(new(2,3,i))).ToArray();
    public byte[] DotBytes() => dots.PackedBytes(1,3);
    public byte[] MembershipBytes() => groups.PackedBytes(2,3);
    public byte[] LevelBytes() => projections.PackedBytes(1,2);
    public int Level(int site) => projections.Read(new(1,2,site))[0];
    public int InkComponents => Stems.Read().VisitedComponents + DotSites().Length;
    public DottedStemRead Read(int stem, bool showMembership = true, bool showBaseline = true, bool showBend = false)
    {
        var edge = Stems.Edge(stem);
        var marks = DotSites();
        bool? dotted = showMembership ? Membership().Any(row => row[0] == edge[0] &&
            marks.Contains(row[1]) && Levels.Compare(Level(edge[0]),Level(row[1])) == LandmarkRead.Before) : null;
        bool? bends = showBend ? (Ink.DriveLinks().Single(row=>row[0]==stem)[1] & 1) != 0 : null;
        return new(dotted, showBaseline ? Levels.Compare(Level(edge[1]),UprightFrame[0]) : LandmarkRead.Unresolved,bends);
    }

    private static readonly WritingLine Reference = new();
    public static string[] Candidates(DottedStemRead read) => new[]{"i","j"}
        .Where((_,i)=>read.Fits(Reference.Read(i,showBend:true))).ToArray();
}
