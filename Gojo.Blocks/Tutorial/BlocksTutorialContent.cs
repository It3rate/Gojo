using GojoDraw;
using SkiaSharp;
using System.Text.Json;

namespace Gojo.Blocks;

internal static class BlocksTutorialContent
{
    private const string DefaultLocale = "en";
    private const string CatalogFileName = "tutorial-pages.json";

    public static IReadOnlyList<BlocksPage> LoadPages(string locale = DefaultLocale)
    {
        var contentRoot = ResolveContentRoot(locale);
        var catalogPath = Path.Combine(contentRoot, CatalogFileName);
        var json = File.ReadAllText(catalogPath);
        var catalog = JsonSerializer.Deserialize<BlocksTutorialCatalog>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
            throw new InvalidOperationException($"Tutorial catalog is empty: {catalogPath}");

        return [.. catalog.Pages.Select(page => CreatePage(page, contentRoot))];
    }

    private static BlocksPage CreatePage(BlocksTutorialCatalogPage page, string contentRoot)
    {
        var notesPath = Path.Combine(contentRoot, page.NotesFile);
        var body = File.ReadAllText(notesPath).Trim();
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
                Path.Combine(contentRoot, page.VoiceFile),
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

    private static string ResolveContentRoot(string locale)
    {
        var relative = Path.Combine("Tutorial", "content", locale);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, relative),
            Path.Combine(Environment.CurrentDirectory, "Gojo.Blocks", relative),
            Path.Combine(Environment.CurrentDirectory, relative)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, CatalogFileName)))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not find tutorial content for locale '{locale}'. Looked under: {string.Join("; ", candidates)}");
    }

    private sealed record BlocksTutorialCatalog(IReadOnlyList<BlocksTutorialCatalogPage> Pages);

    private sealed record BlocksTutorialCatalogPage(
        string Id,
        string Title,
        string Subtitle,
        string NotesFile,
        string VoiceFile,
        IReadOnlyList<string> Concepts);
}
