using Gojo.Core;

namespace Gojo.Blocks;

/// <summary>
/// Public, non-rendering export of glossary backing structures. This keeps the
/// UI/catalog internals private while letting encoding and analysis layers read
/// the actual Gojo structures behind glossary blocks.
/// </summary>
public static class BlocksGlossaryExport
{
    public const string ResolutionTensionPrefix = "resolution-tension";

    public static IReadOnlyList<BlocksGlossaryStructureExport> Structures(string? idPrefix = null)
    {
        var structures = BlocksGlossaryStructureCatalog.Create();
        var entries = idPrefix is null
            ? structures
            : structures.Where(pair =>
                pair.Key.StartsWith($"{idPrefix}-", StringComparison.Ordinal));

        return [.. entries.Select(pair => ToExport(GroupId(pair.Key), pair.Key, pair.Value))];
    }

    public static IReadOnlyList<BlocksGlossaryStructureExport> ResolutionTension() =>
        Structures(ResolutionTensionPrefix);

    private static BlocksGlossaryStructureExport ToExport(
        string groupId,
        string id,
        BlocksGlossaryStructure structure) =>
        new(
            groupId,
            id,
            structure.Structure,
            structure.Expression,
            structure.EncodingNote);

    private static string GroupId(string id)
    {
        if (id.StartsWith($"{ResolutionTensionPrefix}-", StringComparison.Ordinal))
        {
            return ResolutionTensionPrefix;
        }

        var lastDash = id.LastIndexOf('-');
        return lastDash <= 0 ? id : id[..lastDash];
    }
}

public sealed record BlocksGlossaryStructureExport(
    string GroupId,
    string Id,
    IElement Structure,
    string Expression,
    string? EncodingNote);
