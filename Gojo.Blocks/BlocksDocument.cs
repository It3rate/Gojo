using Gojo.Core;
using GojoCAD;
using SkiaSharp;

namespace Gojo.Blocks;

internal sealed record BlocksDocument(IReadOnlyList<BlocksPage> Pages, int PageIndex, bool NotesVisible = false)
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

    public BlocksDocument HideNotes() =>
        this with { NotesVisible = false };

    public static BlocksDocument CreateDefault() =>
        new(
            [
                new BlocksPage(
                    "Blocks Surface",
                    "A clean page shell for rebuilding Gojo graphic experiments.",
                    "This page is intentionally quiet. The interaction surface is the primary object; narration stays available as notes.",
                    CreateStarterStructure(),
                    new SKRect(-40, -34, 96, 60),
                    11),
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

    private static IElement CreateStarterStructure()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();

        Relation Point(long px, long py) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, px), new CarrierLocation(y, py));

        var left = Point(-26, 0);
        var center = Point(0, 0);
        var right = Point(34, 0);
        var top = Point(0, 28);
        var lower = Point(0, -22);
        var radius = Point(18, 0);

        return Relation.OrderedGroup(
            new Carrier(),
            GraphicStructure.FrameSegment(left, right),
            GraphicStructure.FrameSegment(lower, top),
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
