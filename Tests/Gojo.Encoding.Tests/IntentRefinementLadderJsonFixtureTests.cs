using System.Text.Json.Nodes;

namespace Gojo.Encoding.Tests;

public sealed class IntentRefinementLadderJsonFixtureTests
{
    [Theory]
    [InlineData("intent-refinement-ladder-pcb-route.json", "grammar/intent-refinement-ladder/pcb-route")]
    [InlineData("intent-refinement-ladder-message-relay.json", "grammar/intent-refinement-ladder/message-relay")]
    public void IntentRefinementLadderJsonFixturesPreserveThreeSectionBasisChain(
        string fixtureName,
        string sourceId)
    {
        var root = ReadFixture(fixtureName);
        var sections = SectionObjects(root);

        Assert.Equal(sourceId, root["sourceId"]?.GetValue<string>());
        Assert.Equal(3, sections.Length);
        Assert.Equal("base-intent", sections[0]["id"]?.GetValue<string>());
        Assert.Equal([0, 1, 2], sections.Select(section => section["rung"]?.GetValue<int>()).ToArray());
        Assert.Equal("base-intent", sections[1]["basis"]?.GetValue<string>());
        Assert.Equal(sections[1]["id"]?.GetValue<string>(), sections[2]["basis"]?.GetValue<string>());
        Assert.Equal("base-intent", sections[0]["role"]?.GetValue<string>());
        Assert.Equal("constraint-elaboration", sections[1]["role"]?.GetValue<string>());
        Assert.Equal("settlement-refinement", sections[2]["role"]?.GetValue<string>());
        Assert.True(ContainsUse(sections[1], "base-intent"));
        Assert.True(ContainsUse(sections[2], sections[1]["id"]?.GetValue<string>() ?? string.Empty));
    }

    [Fact]
    public void PcbRouteFixtureKeepsPositiveOutletRepairInspectable()
    {
        var root = ReadFixture("intent-refinement-ladder-pcb-route.json");
        var repair = Section(root, "positive-outlet-repair");

        Assert.True(ContainsKind(repair, "directedSegment"));
        Assert.True(ContainsFace(repair, "inverse"));
        Assert.Contains("through", StringArray(root["languageHints"]?["prepositions"]));
        Assert.Contains("around", StringArray(root["languageHints"]?["prepositions"]));

        var stalenessRules = Assert.IsType<JsonArray>(root["stalenessRules"]);
        Assert.Contains(stalenessRules.OfType<JsonObject>(), rule =>
            rule["when"]?.GetValue<string>() == "blocked-span disappears" &&
            rule["staleBelow"]?.GetValue<string>() == "clearance-elaboration");
    }

    [Fact]
    public void MessageRelayFixtureShowsExactAndPartialLanguageFits()
    {
        var root = ReadFixture("intent-refinement-ladder-message-relay.json");
        var hints = Assert.IsType<JsonObject>(root["languageHints"]);

        Assert.Contains("message", StringArray(hints["nouns"]));
        Assert.Contains("send", StringArray(hints["verbs"]));
        Assert.Contains("through", StringArray(hints["prepositions"]));

        var fits = Assert.IsType<JsonArray>(root["mappingFits"])
            .OfType<JsonObject>()
            .ToDictionary(fit => fit["wordHint"]?.GetValue<string>() ?? string.Empty, StringComparer.Ordinal);

        Assert.Equal("exact-structure-with-context", fits["deliver"]["fit"]?.GetValue<string>());
        Assert.Equal("partial-source-and-conveyance", fits["send"]["fit"]?.GetValue<string>());
        Assert.Equal("partial-transform-only", fits["move"]["fit"]?.GetValue<string>());
        Assert.Equal("preposition-carrier-fit", fits["through"]["fit"]?.GetValue<string>());
        Assert.NotEmpty(Assert.IsType<JsonArray>(fits["send"]["unmatched"]));
        Assert.NotEmpty(Assert.IsType<JsonArray>(fits["move"]["unmatched"]));
    }

    private static JsonObject ReadFixture(string fixtureName) =>
        Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(FixturePath(fixtureName))));

    private static JsonArray Sections(JsonObject root) =>
        Assert.IsType<JsonArray>(root["sections"]);

    private static JsonObject[] SectionObjects(JsonObject root) =>
        [.. Sections(root).Select(section => Assert.IsType<JsonObject>(section))];

    private static JsonObject Section(JsonObject root, string id)
    {
        var section = SectionObjects(root)
            .SingleOrDefault(item => item["id"]?.GetValue<string>() == id);
        return Assert.IsType<JsonObject>(section);
    }

    private static string[] StringArray(JsonNode? node) =>
        Assert.IsType<JsonArray>(node).Select(item => item?.GetValue<string>() ?? string.Empty).ToArray();

    private static bool ContainsUse(JsonNode? node, string id)
    {
        if (node is JsonObject obj)
        {
            if (obj["use"]?.GetValue<string>() == id)
            {
                return true;
            }

            return obj.Any(pair => ContainsUse(pair.Value, id));
        }

        return node is JsonArray array && array.Any(item => ContainsUse(item, id));
    }

    private static bool ContainsKind(JsonNode? node, string kind)
    {
        if (node is JsonObject obj)
        {
            if (obj["kind"]?.GetValue<string>() == kind)
            {
                return true;
            }

            return obj.Any(pair => ContainsKind(pair.Value, kind));
        }

        return node is JsonArray array && array.Any(item => ContainsKind(item, kind));
    }

    private static bool ContainsFace(JsonNode? node, string face)
    {
        if (node is JsonObject obj)
        {
            if (obj["face"]?.GetValue<string>() == face)
            {
                return true;
            }

            return obj.Any(pair => ContainsFace(pair.Value, face));
        }

        return node is JsonArray array && array.Any(item => ContainsFace(item, face));
    }

    private static string FixturePath(string fixtureName) =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "grammar",
            fixtureName);
}
