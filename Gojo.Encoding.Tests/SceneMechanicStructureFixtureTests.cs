using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class SceneMechanicStructureFixtureTests
{
    private static readonly string[] ExpectedFixtureNames =
    [
        "001-transform.json",
        "002-directed-transfer.json",
        "003-contact.json",
        "004-pressure-along-carrier.json",
        "005-boundary-crossing.json",
        "006-containment.json",
        "007-surface-coverage.json",
        "008-separation.json",
        "009-joining.json",
        "010-obstruction.json",
        "011-conduit.json",
        "012-avoidance.json",
        "013-pursuit.json",
        "014-support-placement.json",
        "015-perception.json",
        "016-social-exchange.json"
    ];

    [Fact]
    public void SceneMechanicFixturesAreOnePureStructureFilePerMechanic()
    {
        var actual = Directory
                .GetFiles(FixtureDirectory(), "*.json")
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal)
                .ToArray();

        Assert.True(
            ExpectedFixtureNames.SequenceEqual(actual, StringComparer.Ordinal),
            $"Expected scene mechanic fixtures: {string.Join(", ", ExpectedFixtureNames)}. Actual: {string.Join(", ", actual)}.");
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void SceneMechanicFixtureReloadsAsPureStructure(string fixtureName)
    {
        var document = ConceptDefinitionInterchangeJson.Deserialize(File.ReadAllText(FixturePath(fixtureName)));
        var sourceId = ExpectedSourceId(fixtureName);
        var definition = Assert.Single(document.Definitions);

        Assert.Equal(sourceId, document.SourceId);
        Assert.NotEmpty(document.Carriers);
        Assert.EndsWith(" structure", definition.Id, StringComparison.Ordinal);
        Assert.NotNull(definition.DependsOn);
        Assert.Empty(definition.DependsOn);
        Assert.Null(definition.ResidueOrTension);
        AssertPureBody(definition.Body);
        Assert.Contains(BodyKinds(definition.Body), kind => kind is "point" or "segment" or "directedSegment");

        var loaded = ConceptDefinitionInterchangeJson.ToLabeledLedger(document);
        Assert.True(ConceptDefinitionDiscoverySurface.TryReadLedger(loaded.Ledger, out var graph));
        var labeledDefinition = loaded.SingleDefinition(definition.Id);

        Assert.True(graph.TryFind(labeledDefinition, out var node));
        Assert.Empty(graph.DependenciesOf(node));
    }

    public static IEnumerable<object[]> FixtureNames() =>
        ExpectedFixtureNames.Select(name => new object[] { name });

    private static void AssertPureBody(ConceptDefinitionInterchangeBody body)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "group",
            "relation",
            "point",
            "segment",
            "directedSegment"
        };

        foreach (var kind in BodyKinds(body))
        {
            Assert.Contains(kind, allowed);
        }
    }

    private static IEnumerable<string> BodyKinds(ConceptDefinitionInterchangeBody body)
    {
        yield return body.Kind;

        if (body.Left is not null)
        {
            foreach (var kind in BodyKinds(body.Left))
            {
                yield return kind;
            }
        }

        if (body.Right is not null)
        {
            foreach (var kind in BodyKinds(body.Right))
            {
                yield return kind;
            }
        }

        foreach (var element in body.Elements ?? [])
        {
            foreach (var kind in BodyKinds(element))
            {
                yield return kind;
            }
        }
    }

    private static string ExpectedSourceId(string fixtureName)
    {
        var fileStem = Path.GetFileNameWithoutExtension(fixtureName);
        var slug = fileStem[(fileStem.IndexOf('-', StringComparison.Ordinal) + 1)..];
        return $"scene-mechanics/{slug}";
    }

    private static string FixturePath(string fixtureName) =>
        Path.Combine(FixtureDirectory(), fixtureName);

    private static string FixtureDirectory() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "scene-mechanics");
}
