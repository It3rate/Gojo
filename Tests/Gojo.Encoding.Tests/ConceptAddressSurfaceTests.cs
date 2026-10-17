using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class ConceptAddressSurfaceTests
{
    [Fact]
    public void TextSurfaceRoundTripsInternationalCharactersAsIntegerStructure()
    {
        var text = EncodedTextSurface.Create(new Carrier(), "速度 fast スピード");

        Assert.True(EncodedTextSurface.TryRead(text, out var readText));
        Assert.Equal("速度 fast スピード", readText);

        var bytes = EncodedTextSurface.ToUtf8Integers(readText);
        Assert.All(bytes, value => Assert.InRange(value, 0, 255));
        Assert.True(EncodedTextSurface.TryReadUtf8Integers(bytes, out var decoded));
        Assert.Equal(readText, decoded);
    }

    [Fact]
    public void AtomCarriesShortNamedSemanticAddress()
    {
        var distance = ConceptAddressSurface.Atom(new Carrier(), "distance");

        Assert.True(ConceptAddressSurface.TryReadAtom(distance, out var name, out var residue));
        Assert.Equal("distance", name);
        Assert.True(residue is Relation);
        Assert.Equal("distance", ConceptAddressSurface.ToDebugString(distance));
    }

    [Fact]
    public void CompoundAddressCarriesPartsContextAndDebugRead()
    {
        var distance = ConceptAddressSurface.Atom(new Carrier(), "distance");
        var time = ConceptAddressSurface.Atom(new Carrier(), "time");
        var motion = ConceptAddressSurface.Atom(new Carrier(), "motion");
        var vehicle = ConceptAddressSurface.Atom(new Carrier(), "vehicle");

        var speed = ConceptAddressSurface.Compound(
            new Carrier(),
            "speed:motion-rate",
            [distance, time],
            [motion, vehicle]);

        Assert.True(ConceptAddressSurface.TryReadCompound(
            speed,
            out var lexical,
            out var parts,
            out var contexts,
            out var residue));
        Assert.Equal("speed:motion-rate", lexical);
        Assert.Equal([distance, time], parts);
        Assert.Equal([motion, vehicle], contexts);
        Assert.True(residue is Relation);
        Assert.Equal(
            "speed:motion-rate(distance,time;context=motion,vehicle)",
            ConceptAddressSurface.ToDebugString(speed));
    }

    [Fact]
    public void SameWordCanCarryDifferentSemanticAddress()
    {
        var distance = ConceptAddressSurface.Atom(new Carrier(), "distance");
        var time = ConceptAddressSurface.Atom(new Carrier(), "time");
        var stimulant = ConceptAddressSurface.Atom(new Carrier(), "stimulant");
        var slang = ConceptAddressSurface.Atom(new Carrier(), "slang");

        var motionSpeed = ConceptAddressSurface.Compound(
            new Carrier(),
            "speed",
            [distance, time],
            [ConceptAddressSurface.Atom(new Carrier(), "motion")]);
        var drugSpeed = ConceptAddressSurface.Compound(
            new Carrier(),
            "speed",
            [stimulant],
            [slang]);

        Assert.NotEqual(
            ConceptAddressSurface.ToDebugString(motionSpeed),
            ConceptAddressSurface.ToDebugString(drugSpeed));
    }

    [Fact]
    public void NewMergedConceptCanCarryResidueInsteadOfPretendingToBeSettled()
    {
        var baseball = ConceptAddressSurface.Atom(new Carrier(), "baseball");
        var horseRiding = ConceptAddressSurface.Atom(new Carrier(), "horse-riding");
        var unresolved = ConceptAddressSurface.Atom(new Carrier(), "unsettled-composition");

        var strangeSport = ConceptAddressSurface.Compound(
            new Carrier(),
            "baseball-on-horses",
            [baseball, horseRiding],
            [ConceptAddressSurface.Atom(new Carrier(), "game")],
            unresolved);

        Assert.Equal(
            "baseball-on-horses(baseball,horse-riding;context=game;residue=unsettled-composition)",
            ConceptAddressSurface.ToDebugString(strangeSport));
    }
}
