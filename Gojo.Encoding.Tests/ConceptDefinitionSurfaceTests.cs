using Gojo.Core;
using Gojo.Encoding;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class ConceptDefinitionSurfaceTests
{
    [Fact]
    public void PrimitiveDefinitionCrystallizesBodyWithProvenance()
    {
        var site = Relation.Point(new Carrier(), 0);
        var definition = ConceptDefinitionSurface.Create(new Carrier(), "position", site);

        Assert.True(ConceptDefinitionSurface.TryRead(definition, out var read));
        Assert.True(ConceptAddressSurface.TryReadAtom(read.Name, out var name, out _));
        Assert.Equal("position", name);
        Assert.Same(site, read.Body);
        Assert.Same(site, read.Provenance);
        Assert.Empty(read.Dependencies);
        Assert.True(read.ResidueOrTension is Relation);
    }

    [Fact]
    public void DefinitionLedgerPreservesCreationOrderAndEarlierDependencies()
    {
        var axis = new Carrier();
        var source = Relation.Segment(axis, 0, 10);
        var offset = Relation.OrthogonalOutlet(source);
        var span = ConceptDefinitionSurface.Create(new Carrier(), "span", source);
        var offsetRead = ConceptDefinitionSurface.Create(
            new Carrier(),
            "offset-read",
            offset,
            [span],
            provenance: Relation.OrderedGroup(new Carrier(), source, offset));
        var ledger = ConceptDefinitionSurface.Ledger(new Carrier(), [span, offsetRead]);

        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));
        Assert.Equal([span, offsetRead], definitions);
        Assert.True(ConceptDefinitionSurface.TryRead(definitions[1], out var offsetDefinition));
        Assert.Equal([span], offsetDefinition.Dependencies);
        Assert.Same(offset, offsetDefinition.Body);
    }

    [Fact]
    public void DependencyMayPointAtAspirationalStructureWithoutBreakingRead()
    {
        var areaNeed = ConceptAddressSurface.Atom(new Carrier(), "future-area-definition");
        var boundary = ConceptAddressSurface.Atom(new Carrier(), "boundary");
        var residue = ConceptAddressSurface.Atom(new Carrier(), "missing-area-footing");
        var support = ConceptDefinitionSurface.Create(
            new Carrier(),
            "area-support",
            boundary,
            [areaNeed],
            residueOrTension: residue);

        Assert.True(ConceptDefinitionSurface.TryRead(support, out var read));
        var dependency = Assert.Single(read.Dependencies);
        Assert.True(ConceptAddressSurface.TryReadAtom(dependency, out var dependencyName, out _));
        Assert.Equal("future-area-definition", dependencyName);
        Assert.Same(residue, read.ResidueOrTension);
    }

    [Fact]
    public void CrystallizedReferenceReusesDefinitionWithoutExpandingBody()
    {
        var width = Relation.Segment(new Carrier(), 0, 3);
        var height = Relation.Segment(new Carrier(), 0, 5);
        var areaBody = Relation.OrderedGroup(new Carrier(), width, height);
        var areaDefinition = ConceptDefinitionSurface.Create(
            new Carrier(),
            "area",
            areaBody,
            provenance: areaBody);
        var circleContext = ConceptAddressSurface.Atom(new Carrier(), "circle-inside-support");
        var areaReference = ConceptDefinitionSurface.Reference(new Carrier(), areaDefinition, circleContext);

        Assert.True(ConceptDefinitionSurface.TryReadReference(areaReference, out var referenceRead));
        Assert.Same(areaDefinition, referenceRead.DefinitionOrAddress);
        Assert.Same(circleContext, referenceRead.Context);
        Assert.True(referenceRead.ResidueOrTension is Relation);
        Assert.True(ConceptDefinitionSurface.TryRead(referenceRead.DefinitionOrAddress, out var definitionRead));
        Assert.Same(areaBody, definitionRead.Body);
    }

    [Fact]
    public void DefinitionProjectsToCommonReadWitness()
    {
        var source = Relation.Segment(new Carrier(), 0, 1);
        var residue = ConceptAddressSurface.Atom(new Carrier(), "low-resolution-residue");
        var definition = ConceptDefinitionSurface.Create(
            new Carrier(),
            "unit-span",
            source,
            residueOrTension: residue);

        Assert.True(ConceptDefinitionSurface.TryCreateWitness(new Carrier(), definition, out var witness));
        Assert.True(ReadWitness.TryRead(
            witness,
            out var selected,
            out var readSurface,
            out var result,
            out var witnessResidue,
            out var provenance));
        Assert.Same(source, selected);
        Assert.Same(definition, readSurface);
        Assert.True(ConceptAddressSurface.TryReadAtom(result, out var name, out _));
        Assert.Equal("unit-span", name);
        Assert.Same(residue, witnessResidue);
        Assert.Same(source, provenance);
    }
}
