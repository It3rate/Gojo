using System.Text.Json;
using System.Text.Json.Nodes;
using Gojo.Core;

namespace PosterScratchpad;

internal static class FrozenSource
{
    internal static void RequireMatch(Relation document, string path)
    {
        // This is diagnostic-schema compatibility, not executable rehydration.
        // Missing newly added fields take their empty diagnostic defaults. Any
        // non-default new evidence, changed old field, or unknown field fails.
        // Frozen files and generation hashes are never rewritten here.
        var frozen = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        if (frozen is not null)
        {
            frozen.TryAdd("Placeholders", new JsonArray());
            if (frozen["Carriers"] is JsonArray carriers)
                foreach (var carrier in carriers.OfType<JsonObject>())
                {
                    carrier.TryAdd("ValueTickRefIndex", JsonValue.Create(-1));
                    carrier.TryAdd("TensionTickRefIndex", JsonValue.Create(-1));
                    carrier.TryAdd("AttachedRelationIndexes", null);
                }
            if (frozen["UnitShapes"] is JsonArray units)
                foreach (var unit in units.OfType<JsonObject>())
                    unit.TryAdd("Descriptors", new JsonArray());
        }
        var current = JsonSerializer.SerializeToNode(GojoEncodedSnapshot.Capture(document));
        // State coordinates allocated by newer runtimes shift absolute TickRef
        // ids even when the selected drawing is identical. Keep creation order,
        // row identity, coordinate values and every reference; normalize only
        // session allocation gaps. Frozen files are never rewritten.
        NormalizeTickAllocationGaps(frozen);
        NormalizeTickAllocationGaps(current);
        if (frozen is null || !JsonNode.DeepEquals(current, frozen))
            throw new InvalidOperationException("Reconstructed source differs from the frozen Gojo artifact.");
    }

    private static void NormalizeTickAllocationGaps(JsonNode? document)
    {
        if (document?["TickRefs"] is not JsonArray ticks) return;
        var ordered = ticks.OfType<JsonObject>().OrderBy(tick => tick["Id"]!.GetValue<ulong>()).ToArray();
        if (ordered.Any(tick => tick["Id"]!.GetValue<ulong>() == 0) ||
            ordered.Select(tick => tick["Id"]!.GetValue<ulong>()).Distinct().Count() != ordered.Length)
            throw new InvalidOperationException("Frozen tick identities must be positive and distinct.");
        for (var index = 0; index < ordered.Length; index++)
            ordered[index]["Id"] = JsonValue.Create((ulong)index + 1);
    }
}
