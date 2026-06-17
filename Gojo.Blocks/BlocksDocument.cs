using Gojo.Core;
using GojoCAD;
using GojoDraw;
using SkiaSharp;

namespace Gojo.Blocks;

internal sealed record BlocksDocument(
    IReadOnlyList<BlocksPage> TutorialPages,
    IReadOnlyList<BlocksPage> GlossaryPages,
    int PageIndex,
    bool GlossaryVisible = false,
    int ThemeIndex = 0,
    bool NotesVisible = false,
    bool SidePanelVisible = true,
    bool AudioEnabled = false,
    bool PhysicalPointInteractive = false,
    double PhysicalPointT = 0.5d)
{
    public const string PhysicalStructurePageId = "physical-structure";

    public IReadOnlyList<BlocksPage> Pages => GlossaryVisible ? GlossaryPages : TutorialPages;

    public BlocksPage CurrentPage => Pages[Math.Clamp(PageIndex, 0, Math.Max(0, Pages.Count - 1))];

    public BlocksTheme CurrentTheme => BlocksTheme.At(ThemeIndex);

    public IElement? CurrentStructure =>
        !GlossaryVisible && CurrentPage.Id == PhysicalStructurePageId
            ? CreatePhysicalPointLineStructure(PhysicalPointT)
            : CurrentPage.Structure;

    public BlocksDocument NextPage() =>
        this with { PageIndex = Pages.Count == 0 ? 0 : (PageIndex + 1) % Pages.Count };

    public BlocksDocument PreviousPage() =>
        this with { PageIndex = Pages.Count == 0 ? 0 : (PageIndex - 1 + Pages.Count) % Pages.Count };

    public BlocksDocument SelectPage(int index) =>
        this with { PageIndex = Math.Clamp(index, 0, Math.Max(0, Pages.Count - 1)) };

    public BlocksDocument FirstPage() =>
        this with { PageIndex = 0 };

    public BlocksDocument LastPage() =>
        this with { PageIndex = Math.Max(0, Pages.Count - 1) };

    public BlocksDocument ToggleNotes() =>
        this with { NotesVisible = !NotesVisible };

    public BlocksDocument ToggleSidePanel() =>
        this with { SidePanelVisible = !SidePanelVisible };

    public BlocksDocument ToggleAudio() =>
        this with { AudioEnabled = !AudioEnabled };

    public BlocksDocument ToggleGlossary() =>
        this with
        {
            GlossaryVisible = !GlossaryVisible,
            PageIndex = 0,
            NotesVisible = false,
            PhysicalPointInteractive = false,
            PhysicalPointT = 0.5d
        };

    public BlocksDocument ToggleTheme() =>
        this with { ThemeIndex = BlocksTheme.NextIndex(ThemeIndex) };

    public BlocksDocument HideNotes() =>
        this with { NotesVisible = false };

    public BlocksDocument StartPhysicalPointInteraction(double pointT) =>
        !GlossaryVisible && CurrentPage.Id == PhysicalStructurePageId
            ? this with { PhysicalPointInteractive = true, PhysicalPointT = ClampPointT(pointT) }
            : this;

    public BlocksDocument DragPhysicalPoint(double pointT) =>
        !GlossaryVisible && CurrentPage.Id == PhysicalStructurePageId && PhysicalPointInteractive
            ? this with { PhysicalPointT = ClampPointT(pointT) }
            : this;

    public BlocksDocument ResetCurrentPage() =>
        !GlossaryVisible && CurrentPage.Id == PhysicalStructurePageId
            ? this with { PhysicalPointInteractive = false, PhysicalPointT = 0.5d }
            : this;

    public static BlocksDocument CreateDefault() =>
        new(BlocksTutorialContent.LoadPages(), BlocksGlossaryContent.CreatePages(), 0, GlossaryVisible: true);

    private static IElement CreatePhysicalPointLineStructure(double pointT)
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();

        Relation Point(long px, long py) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, px), new CarrierLocation(y, py));

        var start = Point(-50, 0);
        var end = Point(50, 0);
        var active = Point(PointTicks(pointT), 0);

        return Relation.OrderedGroup(
            new Carrier(),
            GraphicStructure.FrameSegment(start, end),
            active,
            GraphicStructure.RadialFlow(active));
    }

    private static long PointTicks(double pointT) =>
        (long)Math.Round(-50 + ClampPointT(pointT) * 100);

    private static double ClampPointT(double pointT) =>
        Math.Clamp(pointT, 0d, 1d);
}

internal sealed record BlocksPage(
    string Id,
    string Title,
    string Subtitle,
    string Body,
    IElement? Structure,
    SKRect? ModelBounds,
    int ProjectionSeed,
    IReadOnlyList<string> Concepts,
    GojoDrawAudioCue? NarrationAudio = null,
    BlocksTutorialScene? TutorialScene = null,
    IReadOnlyList<BlocksGlossaryItem>? GlossaryItems = null,
    BlocksGlossarySheetKind GlossarySheet = BlocksGlossarySheetKind.None,
    IReadOnlyDictionary<string, string>? GlossaryDescriptions = null,
    IReadOnlyDictionary<string, BlocksGlossaryStructure>? GlossaryStructures = null)
{
    public bool HasAnimation => TutorialScene?.HasAnimation == true;

    public bool IsGlossary => GlossaryItems is { Count: > 0 } ||
        GlossarySheet != BlocksGlossarySheetKind.None;

    public string? GlossaryDescriptionFor(string id) =>
        GlossaryDescriptions is not null &&
        GlossaryDescriptions.TryGetValue(id, out var description)
            ? description
            : null;

    public BlocksGlossaryStructure? GlossaryStructureFor(string id) =>
        GlossaryStructures is not null &&
        GlossaryStructures.TryGetValue(id, out var structure)
            ? structure
            : null;
}

internal sealed record BlocksGlossaryItem(
    string Id,
    string Title,
    IElement Structure,
    SKRect? ModelBounds,
    int ProjectionSeed = 0);

internal sealed record BlocksGlossaryStructure(
    IElement Structure,
    SKRect? ModelBounds,
    int ProjectionSeed,
    string Expression,
    string? EncodingNote = null);

internal enum BlocksGlossarySheetKind
{
    None,
    ResolutionTension,
    SiteFlowBoundary,
    ActionsRepeat,
    UnitsTicks,
    BasisFrames,
    SourceToSupport,
    GradesDimensions,
    DirectionToArea,
    FaceNormalSupport,
    Curves,
    Branches,
    BooleanParticipation,
    ConnectedPaths,
    PathReadBases,
    DerivedPathStructures,
    BooleanOffsetReads,
    CrystallizationProvenance,
    DroneClipOffsetLadder,
    RadialCrescentLadder,
    DiamondCuspRounding,
    ConstraintsSettlement,
    TopologyParticipation,
    RepetitionTimeline,
    PcbDataAttachment
}
