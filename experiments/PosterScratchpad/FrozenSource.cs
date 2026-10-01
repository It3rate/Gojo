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
                    carrier.TryAdd("AttachedRelationIndexes", null);
                }
            if (frozen["UnitShapes"] is JsonArray units)
                foreach (var unit in units.OfType<JsonObject>())
                    unit.TryAdd("Descriptors", new JsonArray());
        }
        var current = JsonSerializer.SerializeToNode(GojoEncodedSnapshot.Capture(document));
        if (frozen is null || !JsonNode.DeepEquals(current, frozen))
            throw new InvalidOperationException("Reconstructed source differs from the frozen Gojo artifact.");
    }
}
