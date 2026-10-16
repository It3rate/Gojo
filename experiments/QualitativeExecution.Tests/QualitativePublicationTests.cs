using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativePublicationTests
{
    [Fact]
    public void ARepeatedDomainIdentityIsNotANewResolutionContract()
    {
        var f = new Fixture();
        var malformed = QualitativePublication.Create(f.Source, f.Subject, f.Event,
            f.Fine, StructuralConstraint.Closed(f.A, f.A), StructuralConstraint.Closed(f.A));
        Assert.False(QualitativePublication.TryRead(malformed, out _, out _, out _, out _, out _, out _, out _));
        Assert.False(QualitativePublication.TryRefine(f.Publish(f.FineDomain), malformed, out _, out _));
    }

    [Fact]
    public void ImpossibleInputDoesNotBecomePossibleBecauseTheCorrespondenceIsUnfinished()
    {
        var f = new Fixture();
        var unfinished = new ResolutionCorrespondence(f.Fine, f.Coarse, f.FineDomain, f.CoarseDomain,
            StructuralConstraint.Open(new Relation(f.A, f.Low)));
        Assert.True(QualitativePublication.TryForward(unfinished, f.Publish(StructuralConstraint.Closed()),
            out var result, out _));
        Assert.True(QualitativePublication.TryRead(result, out _, out _, out _, out _, out _, out var support, out _));
        Assert.True(StructuralConstraint.TryReadScope(support, out var entries, out var seam, out var tail));
        Assert.Empty(entries); Assert.True(seam.Equals(tail));
    }

    [Fact]
    public void LossyRoundTripKeepsTheObservationButDoesNotRecoverItsPrivateDetail()
    {
        var f = new Fixture();
        var original = f.Publish(StructuralConstraint.Closed(f.A));
        Assert.True(QualitativePublication.TryForward(f.Map, original, out var coarse, out var forward));
        Assert.True(QualitativePublication.TryBackward(f.Map, coarse, out var expanded, out var backward));
        Assert.True(QualitativePublication.TryRead(expanded, out var source, out var subject, out var eventIdentity,
            out var basis, out var domain, out var support, out _));
        Assert.Same(f.Source, source); Assert.Same(f.Subject, subject); Assert.Same(f.Event, eventIdentity);
        Assert.Same(f.Fine, basis); Assert.Same(f.FineDomain, domain);
        Assert.Equal(new[] { f.A, f.B }, FiniteLaw.Entries(support));
        Assert.True(Contains(backward, original)); Assert.True(Contains(forward, f.Map.Surface));
        AssertNoNumbers(backward);
    }

    [Fact]
    public void FreshEvidenceNarrowsAnInverseImageWithoutReplacingTheEarlierClaim()
    {
        var f = new Fixture();
        var coarse = QualitativePublication.Create(f.Source, f.Subject, f.Event,
            f.Coarse, f.CoarseDomain, StructuralConstraint.Closed(f.Low));
        Assert.True(QualitativePublication.TryBackward(f.Map, coarse, out var prior, out _));
        var measured = f.Publish(StructuralConstraint.Closed(f.B));
        Assert.True(QualitativePublication.TryRefine(prior, measured, out var accepted, out var witness));
        Assert.True(QualitativePublication.TryRead(accepted, out _, out _, out _, out _, out _, out var support, out _));
        Assert.Same(f.B, Assert.Single(FiniteLaw.Entries(support)));
        Assert.True(Contains(witness, coarse)); Assert.True(Contains(witness, prior));
        Assert.True(Contains(witness, measured));
        var conflicting = f.Publish(StructuralConstraint.Closed(f.C));
        Assert.False(QualitativePublication.TryRefine(prior, conflicting, out var unchanged, out var failed));
        Assert.Same(prior, unchanged); Assert.True(Contains(failed, conflicting));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void MatchingCategoriesCannotSubstituteAnotherObservationOrContract(int changed)
    {
        var f = new Fixture(); var prior = f.Publish(f.FineDomain);
        var offered = QualitativePublication.Create(changed == 0 ? new Placeholder() : f.Source,
            changed == 1 ? new Placeholder() : f.Subject, changed == 2 ? new Placeholder() : f.Event,
            changed == 3 ? new Placeholder() : f.Fine,
            changed == 4 ? StructuralConstraint.Closed(f.A, f.B, f.C) : f.FineDomain,
            StructuralConstraint.Closed(f.A));
        Assert.False(QualitativePublication.TryRefine(prior, offered, out var unchanged, out var witness));
        Assert.Same(prior, unchanged); Assert.True(Contains(witness, offered));
    }

    [Fact]
    public void TransportRequiresTheActualExposedBasisAndDomain()
    {
        var f = new Fixture();
        var wrong = QualitativePublication.Create(f.Source, f.Subject, f.Event,
            f.Fine, StructuralConstraint.Closed(f.A, f.B, f.C), StructuralConstraint.Closed(f.A));
        Assert.False(QualitativePublication.TryForward(f.Map, wrong, out var result, out var witness));
        Assert.Same(wrong, result); Assert.True(Contains(witness, wrong));
        Assert.False(QualitativePublication.TryBackward(f.Map, f.Publish(f.FineDomain), out _, out _));
    }

    [Fact]
    public void ARejectedBranchCannotMasqueradeAsACompleteMappedSingleton()
    {
        var f = new Fixture();
        var mixed = f.Publish(StructuralConstraint.Closed(f.A, new Placeholder()));
        Assert.True(QualitativePublication.TryForward(f.Map, mixed, out var result, out _));
        Assert.True(QualitativePublication.TryRead(result, out _, out _, out _, out _, out _, out var support, out _));
        Assert.True(StructuralConstraint.TryReadScope(support, out var known, out var seam, out var tail));
        Assert.Same(f.Low, Assert.Single(known)); Assert.False(seam.Equals(tail));
        Assert.True(Contains(result, mixed));
    }

    [Fact]
    public void UnknownContradictionAndRefinementRemainDifferentReads()
    {
        var f = new Fixture();
        var unknown = f.Publish(StructuralConstraint.Open());
        Assert.True(QualitativePublication.TryRefine(unknown, f.Publish(StructuralConstraint.Closed(f.A)), out _, out _));
        var impossible = f.Publish(StructuralConstraint.Closed());
        Assert.False(QualitativePublication.TryRefine(impossible, f.Publish(StructuralConstraint.Closed(f.A)), out _, out _));
        Assert.False(QualitativePublication.TryRefine(unknown, impossible, out _, out _));
        foreach (var publication in new[] { unknown, impossible })
        {
            Assert.True(QualitativePublication.TryForward(f.Map, publication, out var result, out _));
            Assert.True(QualitativePublication.TryRead(result, out _, out _, out _, out _, out _, out var support, out _));
            Assert.True(StructuralConstraint.TryReadScope(support, out var entries, out var seam, out var tail));
            Assert.Empty(entries); Assert.Equal(ReferenceEquals(publication, impossible), seam.Equals(tail));
        }
    }

    private sealed class Fixture
    {
        public IElement Source { get; } = new Placeholder();
        public IElement Subject { get; } = new Placeholder();
        public IElement Event { get; } = new Placeholder();
        public IElement Fine { get; } = new Placeholder();
        public IElement Coarse { get; } = new Placeholder();
        public IElement A { get; } = new Placeholder();
        public IElement B { get; } = new Placeholder();
        public IElement C { get; } = new Placeholder();
        public IElement Low { get; } = new Placeholder();
        public IElement High { get; } = new Placeholder();
        public Relation FineDomain { get; }
        public Relation CoarseDomain { get; }
        public ResolutionCorrespondence Map { get; }
        public Fixture()
        {
            FineDomain = StructuralConstraint.Closed(A, B, C);
            CoarseDomain = StructuralConstraint.Closed(Low, High);
            Map = ResolutionCorrespondence.FromRows(Fine, Coarse, FineDomain, CoarseDomain,
                new Relation(A, Low), new Relation(B, Low), new Relation(C, High));
        }
        public Relation Publish(Relation support) =>
            QualitativePublication.Create(Source, Subject, Event, Fine, FineDomain, support);
    }

    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(e => ReferenceEquals(e, target));
    private static void AssertNoNumbers(IElement root)
    {
        foreach (var e in Traverse(root)) { Assert.IsNotType<Carrier>(e); Assert.IsNotType<CarrierLocation>(e); }
    }
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
