using Gojo.Core;
using GojoCAD;
using GojoDraw;
using SkiaSharp;

namespace Gojo.Blocks;

internal sealed record BlocksDocument(
    IReadOnlyList<BlocksPage> Pages,
    int PageIndex,
    bool NotesVisible = false,
    bool SidePanelVisible = true,
    bool AudioEnabled = false)
{
    public BlocksPage CurrentPage => Pages[Math.Clamp(PageIndex, 0, Math.Max(0, Pages.Count - 1))];

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

    public BlocksDocument HideNotes() =>
        this with { NotesVisible = false };

    public static BlocksDocument CreateDefault() =>
        new(
            [
                CreatePhysicalStructureTutorialPage(),
                new BlocksPage(
                    "Structure Notes",
                    "Use this page for the next small Gojo-native drawing ladder.",
                    "Page state stays tiny: page index, optional GojoCAD structure, and render bounds.",
                    null,
                    null,
                    17),
                new BlocksPage(
                    "Boolean Workbench",
                    "Placeholder for the next offset/fill/outline synthesis pass.",
                    "The old visualizers taught us what to avoid: hidden area fallback and stale rung residue.",
                    null,
                    null,
                    23)
            ],
            0);

    private static BlocksPage CreatePhysicalStructureTutorialPage()
    {
        var structure = CreatePhysicalStructurePage();
        var bounds = new SKRect(-70, -42, 96, 48);
        return new BlocksPage(
            "Physical Structure",
            "Values as inspectable participation.",
            "Most math libraries start with numbers and operations. Gojo starts with participation in local structure. A value can settle, route, or remain unresolved. The unresolved part stays visible because it may be the next piece of structure.",
            structure,
            bounds,
            11,
            new BlocksTutorialScene(
                structure,
                bounds,
                11,
                TimeSpan.FromSeconds(4),
                [
                    new GojoDrawVisualCue(
                        "incoming-pulse",
                        GojoDrawVisualCueKind.Pulse,
                        "incoming participation",
                        Track: new GojoDrawVisualTrack(
                            new GojoDrawScenePoint(0.08f, 0.52f),
                            new GojoDrawScenePoint(0.66f, 0.52f),
                            TimeSpan.Zero,
                            TimeSpan.FromSeconds(2.6))),
                    new GojoDrawVisualCue(
                        "unsettled-pressure",
                        GojoDrawVisualCueKind.TensionGlow,
                        "unresolved tension",
                        Track: new GojoDrawVisualTrack(
                            new GojoDrawScenePoint(0.66f, 0.52f),
                            new GojoDrawScenePoint(0.82f, 0.34f),
                            TimeSpan.FromSeconds(2.2),
                            TimeSpan.FromSeconds(1.4)))
                ]));
    }

    private static IElement CreatePhysicalStructurePage()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();

        Relation Point(long px, long py) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, px), new CarrierLocation(y, py));

        var source = Point(-58, 0);
        var junction = Point(-20, 0);
        var settled = Point(24, 0);
        var boundary = Point(58, 0);
        var tension = Point(48, 24);
        var returnSink = Point(24, -24);
        var center = Point(-20, 0);
        var radius = Point(-2, 0);

        return Relation.OrderedGroup(
            new Carrier(),
            GraphicStructure.FrameSegment(source, junction),
            GraphicStructure.FrameSegment(junction, settled),
            GraphicStructure.FrameSegment(settled, boundary),
            GraphicStructure.FrameSegment(boundary, tension),
            GraphicStructure.FrameSegment(junction, returnSink),
            GraphicStructure.FrameCircle(center, radius));
    }
}

internal sealed record BlocksPage(
    string Title,
    string Subtitle,
    string Body,
    IElement? Structure,
    SKRect? ModelBounds,
    int ProjectionSeed,
    BlocksTutorialScene? TutorialScene = null)
{
    public bool HasAnimation => TutorialScene?.HasAnimation == true;
}
