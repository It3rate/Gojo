using GojoDraw;
using SkiaSharp;
using System.Reflection;
using System.Text.Json;

namespace Gojo.Blocks;

internal static class BlocksTutorialContent
{
    private const string DefaultLocale = "en";
    private const string CatalogFileName = "tutorial-pages.json";

    public static IReadOnlyList<BlocksPage> LoadPages(string locale = DefaultLocale)
    {
        var contentRoot = ResolveContentRoot(locale);
        var json = contentRoot.ReadAllText(CatalogFileName);
        var catalog = JsonSerializer.Deserialize<BlocksTutorialCatalog>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
            throw new InvalidOperationException($"Tutorial catalog is empty: {CatalogFileName}");

        return [.. catalog.Pages.Select(page => CreatePage(page, contentRoot))];
    }

    private static BlocksPage CreatePage(BlocksTutorialCatalogPage page, BlocksContentRoot contentRoot)
    {
        var body = contentRoot.ReadAllText(page.NotesFile).Trim();
        var isPhysicalPage = page.Id == BlocksDocument.PhysicalStructurePageId;
        SKRect? bounds = isPhysicalPage ? new SKRect(-60, -30, 60, 30) : null;
        var seed = isPhysicalPage ? 11 : 0;
        var scene = isPhysicalPage
            ? new BlocksTutorialScene(
                null,
                bounds,
                seed,
                TimeSpan.FromSeconds(4),
                [],
                Loops: false)
            : null;
        var audio = string.IsNullOrWhiteSpace(page.VoiceFile)
            ? null
            : new GojoDrawAudioCue(
                $"{page.Id}:narration",
                contentRoot.AssetPath(page.VoiceFile),
                GojoDrawAudioCueKind.Narration);

        return new BlocksPage(
            page.Id,
            page.Title,
            page.Subtitle,
            body,
            null,
            bounds,
            seed,
            page.Concepts,
            audio,
            scene);
    }

    private static BlocksContentRoot ResolveContentRoot(string locale) =>
        BlocksContentRoot.Resolve(
            Path.Combine("Tutorial", "content", locale),
            CatalogFileName,
            typeof(BlocksTutorialContent).GetTypeInfo().Assembly,
            $"Could not find tutorial content for locale '{locale}'.");

    private sealed record BlocksTutorialCatalog(IReadOnlyList<BlocksTutorialCatalogPage> Pages);

    private sealed record BlocksTutorialCatalogPage(
        string Id,
        string Title,
        string Subtitle,
        string NotesFile,
        string VoiceFile,
        IReadOnlyList<string> Concepts);
}
