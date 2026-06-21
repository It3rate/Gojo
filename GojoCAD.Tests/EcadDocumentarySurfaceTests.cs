using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class EcadDocumentarySurfaceTests
{
    [Fact]
    public void TextSurfaceStoresSourceTextAsGojoCodePointStructure()
    {
        var text = EcadTextSurface.Create(new Carrier(), "Net-(D1-Pad2)");

        Assert.True(EcadTextSurface.TryRead(text, out var readText));
        Assert.Equal("Net-(D1-Pad2)", readText);
    }

    [Fact]
    public void NetSurfaceCarriesKiCadIdNameWitnessParticipantsAndResidue()
    {
        var netAxis = new Carrier();
        var participants = Relation.OrderedGroup(new Carrier(), Relation.Point(netAxis, 7));
        var residue = Relation.Point(new Carrier(), 1);
        var provenance = EcadTextSurface.Create(new Carrier(), "sample.kicad_pcb");

        var net = EcadNetSurface.Create(
            new Carrier(),
            netAxis,
            7,
            "/SDA",
            provenance,
            participants,
            residue);

        Assert.True(EcadNetSurface.TryRead(
            net,
            out var netId,
            out var name,
            out var witness,
            out var readParticipants,
            out var readResidue));
        Assert.True(netAxis.SameIdentityAs(netId.Carrier));
        Assert.Equal(7, netId.Ticks);
        Assert.Equal("/SDA", name);
        Assert.Same(participants, readParticipants);
        Assert.Same(residue, readResidue);

        Assert.True(ReadWitness.TryRead(
            witness,
            out var selected,
            out var readSurface,
            out var result,
            out var witnessResidue,
            out var witnessProvenance));
        Assert.Same(residue, witnessResidue);
        Assert.Same(provenance, witnessProvenance);
        Assert.True(EcadNetSurface.TryReadIdentity((Relation)result, out var resultId, out var resultName));
        Assert.True(netAxis.SameIdentityAs(resultId.Carrier));
        Assert.Equal(7, resultId.Ticks);
        Assert.Equal("/SDA", resultName);

        Assert.True(EcadNetSurface.TryReadSourceRecord(
            selected,
            out var format,
            out var recordKind,
            out var sourceId,
            out var sourceName));
        Assert.Equal("kicad-pcb", format);
        Assert.Equal("net", recordKind);
        Assert.True(netAxis.SameIdentityAs(sourceId.Carrier));
        Assert.Equal(7, sourceId.Ticks);
        Assert.Equal("/SDA", sourceName);

        Assert.True(EcadNetSurface.TryReadDialectReadSurface(
            readSurface,
            out var dialectFormat,
            out var dialectKind));
        Assert.Equal("kicad-pcb", dialectFormat);
        Assert.Equal("net-list-entry", dialectKind);
    }
}
