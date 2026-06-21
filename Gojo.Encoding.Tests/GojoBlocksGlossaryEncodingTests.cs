using Gojo.Blocks;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoBlocksGlossaryEncodingTests
{
    private static readonly string[] ResolutionTensionIds =
    [
        "resolution-tension-resolution-read",
        "resolution-tension-unspecified-site",
        "resolution-tension-closure-tension",
        "resolution-tension-failed-read",
        "resolution-tension-residue-witness",
        "resolution-tension-settled-read"
    ];

    [Fact]
    public void ResolutionTensionGlossaryExportExposesRealGojoStructures()
    {
        var exports = BlocksGlossaryExport.ResolutionTension();

        Assert.Equal(ResolutionTensionIds, exports.Select(item => item.Id).ToArray());
        Assert.All(exports, item =>
        {
            Assert.Equal(BlocksGlossaryExport.ResolutionTensionPrefix, item.GroupId);
            Assert.False(string.IsNullOrWhiteSpace(item.Expression));
            Assert.NotNull(item.Structure);
        });
        Assert.Contains(exports, item => item.EncodingNote?.Contains("Resolution belongs", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ResolutionTensionGlossaryCanBeEncodedAsBundleWithObserverPaths()
    {
        var exports = BlocksGlossaryExport.ResolutionTension();
        var entries = exports
            .Select(EncodeEntry)
            .Cast<IElement>()
            .ToArray();

        var bundle = GojoStructureEncodingBundleSurface.Create(
            new Carrier(),
            "gojo.blocks/glossary/resolution-tension",
            entries);

        Assert.True(GojoStructureEncodingBundleSurface.TryRead(bundle, out var bundleRead));
        Assert.Equal("gojo.blocks/glossary/resolution-tension", bundleRead.SourceId);
        Assert.Equal(6, bundleRead.Entries.Length);

        foreach (var entry in bundleRead.Entries)
        {
            Assert.True(GojoStructureEncodingBundleSurface.TryReadEntry(entry, out var entryRead));
            Assert.Contains(entryRead.EntryId, ResolutionTensionIds);
            Assert.False(string.IsNullOrWhiteSpace(entryRead.Expression));
            Assert.True(GojoStructureEncodingSurface.TryRead(entryRead.StructureEncoding, out var rawRead));
            Assert.Equal(entryRead.EntryId, rawRead.SourceId);
            Assert.Equal(2, entryRead.ObserverPaths.Length);
            Assert.All(entryRead.ObserverPaths, path =>
            {
                Assert.True(GojoObserverPathSurface.TryRead(path, out var pathRead));
                Assert.Same(entryRead.StructureEncoding, pathRead.SelectedSource);
                Assert.True(GojoObserverPathSurface.TryCreateWitness(new Carrier(), path, out var witness));
                Assert.True(ReadWitness.TryRead(witness, out _, out _, out _, out _, out _));
            });
        }
    }

    [Fact]
    public void EncodedGlossaryEntryCanTraceBackToOpenedBlockStructure()
    {
        var export = BlocksGlossaryExport.ResolutionTension()
            .Single(item => item.Id == "resolution-tension-closure-tension");
        var entry = EncodeEntry(export);

        Assert.True(GojoStructureEncodingBundleSurface.TryReadEntry(entry, out var read));
        Assert.True(GojoStructureEncodingSurface.TryRead(read.StructureEncoding, out var rawRead));

        var openedPath = read.ObserverPaths[0];
        Assert.True(GojoObserverPathSurface.TryRead(openedPath, out var pathRead));
        Assert.Same(rawRead.OpenedStructure, pathRead.Result);
        Assert.Same(export.Structure, pathRead.Result);
        Assert.True(EncodedTextSurface.TryRead(pathRead.Provenance, out var provenance));
        Assert.Equal(export.Expression, provenance);
    }

    private static Relation EncodeEntry(BlocksGlossaryStructureExport export)
    {
        var raw = GojoStructureEncodingSurface.Create(new Carrier(), export.Id, export.Structure);
        Assert.True(GojoStructureEncodingSurface.TryRead(raw, out var rawRead));

        return GojoStructureEncodingBundleSurface.Entry(
            new Carrier(),
            export.Id,
            export.Expression,
            export.EncodingNote,
            raw,
            [
                OpenedStructurePath(export, raw, rawRead),
                FirstRelationRowPath(export, raw, rawRead)
            ]);
    }

    private static Relation OpenedStructurePath(
        BlocksGlossaryStructureExport export,
        Relation rawEncoding,
        GojoStructureEncodingSurface.StructureRead rawRead) =>
        GojoObserverPathSurface.Create(
            new Carrier(),
            rawEncoding,
            GojoObserverPathSurface.ObserverSurface(
                new Carrier(),
                rawEncoding,
                Label("opened glossary block")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    rawEncoding,
                    Label("opened structure"),
                    rawRead.OpenedStructure,
                    export.Structure)),
            rawRead.OpenedStructure,
            provenance: Label(export.Expression));

    private static Relation FirstRelationRowPath(
        BlocksGlossaryStructureExport export,
        Relation rawEncoding,
        GojoStructureEncodingSurface.StructureRead rawRead)
    {
        var row = rawRead.Relations.Length == 0
            ? rawRead.OpenedStructure
            : rawRead.Relations[0];

        return GojoObserverPathSurface.Create(
            new Carrier(),
            rawEncoding,
            GojoObserverPathSurface.ObserverSurface(
                new Carrier(),
                rawEncoding,
                Label("relation table scan")),
            GojoObserverPathSurface.StepFrame(
                new Carrier(),
                GojoObserverPathSurface.Step(
                    new Carrier(),
                    rawEncoding,
                    Label("first relation row"),
                    row,
                    rawRead.OpenedStructure)),
            row,
            provenance: Label(export.Id));
    }

    private static Relation Label(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
