using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ResolutionCorrespondenceTests
{
    [Fact]
    public void ForwardReadPreservesTheNativeBasesDomainsAndConsumedRows()
    {
        var f = new Partition();
        var sourceEvent = new Relation(new Placeholder(), new Placeholder());
        var input = S(f.Fine[1], f.Fine[2]);
        var read = f.Map.ReadForward(input, sourceEvent);
        var result = Read(f.Map, read);

        AssertSet(result.Support, f.Coarse[0], f.Coarse[1]);
        Assert.Empty(E(result.Blocked)); Assert.Empty(E(result.Unresolved));
        Assert.Contains(f.Rows[1], E(result.Provenance));
        Assert.Contains(f.Rows[2], E(result.Provenance));
        Assert.DoesNotContain(f.Rows[0], E(result.Provenance));
        Assert.True(ContainsReference(result.Provenance, sourceEvent));
        var header = Assert.IsType<Relation>(Assert.IsType<Relation>(read.Left).Left);
        Assert.Same(f.Map.Surface, header.Left);
        var direction = Assert.IsType<Relation>(header.Right);
        Assert.Same(f.Map.SourcePort, direction.Left);
        Assert.Same(f.Map.TargetPort, direction.Right);
        Assert.Same(f.Map.SourceDomain, f.Map.SourcePort.Right);
        Assert.Same(f.Map.TargetDomain, f.Map.TargetPort.Right);
        AssertNoCarriers(read);
    }

    [Fact]
    public void InverseReadReturnsEveryPreimageWithoutRecreatingAFineObservation()
    {
        var f = new Partition();
        var forward = Read(f.Map, f.Map.ReadForward(S(f.Fine[0])));
        var backward = Read(f.Map, f.Map.ReadBackward(forward.Support));

        AssertSet(forward.Support, f.Coarse[0]);
        AssertSet(backward.Support, f.Fine[0], f.Fine[1]);
        Assert.Equal(2, E(backward.Provenance).Length);
        Assert.Empty(E(backward.Blocked)); Assert.Empty(E(backward.Unresolved));
        Assert.All(E(backward.Support), value => Assert.Contains(value, f.Fine));
        AssertNoCarriers(backward.Support);
    }

    [Fact]
    public void CrossResolutionCompositionKeepsTheIntermediateAndOriginalMappingRows()
    {
        var f = new Partition();
        var absent = new Placeholder(); var present = new Placeholder();
        var next = ResolutionCorrespondence.FromRows(f.Map.TargetBasis, new Placeholder(),
            f.Map.TargetDomain, S(absent, present), new Relation(f.Coarse[0], absent),
            new Relation(f.Coarse[1], present), new Relation(f.Coarse[2], present));
        var first = f.Map.ReadForward(S(f.Fine[2], f.Fine[4]));
        var middle = Read(f.Map, first);
        var second = next.ReadForward(middle.Support, first);

        AssertSet(Read(next, second).Support, present);
        Assert.True(ContainsReference(second, first));
        Assert.True(ContainsReference(second, f.Rows[2]));
        Assert.True(ContainsReference(second, f.Rows[4]));
        var inverseMiddle = Read(next, next.ReadBackward(S(present)));
        var inverseFine = Read(f.Map, f.Map.ReadBackward(inverseMiddle.Support));
        AssertSet(inverseFine.Support, f.Fine[2], f.Fine[3], f.Fine[4]);
    }

    [Fact]
    public void ClosedEmptyInputStaysImpossibleWhileOpenInputRetainsKnownImages()
    {
        var f = new Partition();
        var empty = Read(f.Map, f.Map.ReadForward(S()));
        var open = Read(f.Map, f.Map.ReadForward(StructuralConstraint.Open(f.Fine[2])));

        AssertClosed(empty.Support); Assert.Empty(E(empty.Support));
        Assert.Empty(E(empty.Blocked)); Assert.Empty(E(empty.Unresolved));
        AssertOpen(open.Support); AssertSet(open.Support, f.Coarse[1]);
        Assert.NotEmpty(E(open.Unresolved));
        Assert.Empty(E(open.Blocked));
    }

    [Fact]
    public void ForeignInputsAreBlockedWithoutInventingAnImageOrErasingKnownImages()
    {
        var f = new Partition(); var foreign = new Placeholder();
        var result = Read(f.Map, f.Map.ReadForward(S(f.Fine[0], foreign)));

        AssertClosed(result.Support); AssertSet(result.Support, f.Coarse[0]);
        Assert.Single(E(result.Blocked)); Assert.Empty(E(result.Unresolved));
        Assert.True(ContainsReference(result.Blocked, foreign));
        Assert.True(ContainsReference(result.Blocked, f.Map.SourceDomain));
        Assert.False(ContainsReference(result.Support, foreign));
    }

    [Fact]
    public void MissingClosedRowsDifferFromAnUnfinishedCorrespondence()
    {
        var f = new Partition();
        var partial = new ResolutionCorrespondence(f.Map.SourceBasis, f.Map.TargetBasis,
            f.Map.SourceDomain, f.Map.TargetDomain, S(f.Rows[0]));
        var unfinished = new ResolutionCorrespondence(f.Map.SourceBasis, f.Map.TargetBasis,
            f.Map.SourceDomain, f.Map.TargetDomain, StructuralConstraint.Open(f.Rows[0]));
        var missing = Read(partial, partial.ReadForward(S(f.Fine[1])));
        var unknown = Read(unfinished, unfinished.ReadForward(S(f.Fine[1])));
        var knownAndOpen = Read(unfinished, unfinished.ReadForward(S(f.Fine[0])));

        AssertClosed(missing.Support); Assert.Empty(E(missing.Support));
        Assert.Single(E(missing.Blocked)); Assert.Empty(E(missing.Unresolved));
        AssertOpen(unknown.Support); Assert.Empty(E(unknown.Support));
        Assert.NotEmpty(E(unknown.Unresolved)); Assert.Empty(E(unknown.Blocked));
        AssertOpen(knownAndOpen.Support); AssertSet(knownAndOpen.Support, f.Coarse[0]);
        Assert.NotEmpty(E(knownAndOpen.Unresolved));
        var impossible = Read(unfinished, unfinished.ReadForward(S()));
        AssertClosed(impossible.Support); Assert.Empty(E(impossible.Support));
        Assert.NotEmpty(E(impossible.Unresolved));
    }

    [Fact]
    public void MalformedAndForeignMappingRowsRemainInspectable()
    {
        var f = new Partition(); var foreign = new Placeholder();
        var badRow = new Relation(f.Fine[0], foreign);
        var incomplete = new Placeholder();
        var map = new ResolutionCorrespondence(f.Map.SourceBasis, f.Map.TargetBasis,
            f.Map.SourceDomain, f.Map.TargetDomain, S(f.Rows[0], badRow, incomplete));
        var read = Read(map, map.ReadForward(S(f.Fine[0])));

        AssertOpen(read.Support); AssertSet(read.Support, f.Coarse[0]);
        Assert.True(ContainsReference(read.Blocked, badRow));
        Assert.True(ContainsReference(read.Unresolved, incomplete));
        Assert.DoesNotContain(foreign, E(read.Support));
        Assert.Contains(f.Rows[0], E(read.Provenance));
        Assert.DoesNotContain(badRow, E(read.Provenance));
        var check = Check(map);
        Assert.NotEmpty(check.Unresolved); Assert.Empty(check.TotalityPassed);
        Assert.Empty(check.FunctionalityPassed);
    }

    [Fact]
    public void MalformedInputStillHasAReadableUnresolvedWitness()
    {
        var f = new Partition();
        var malformed = new Relation(new Relation(new Placeholder(), new Placeholder()), new Placeholder());
        var read = Read(f.Map, f.Map.ReadForward(malformed));

        AssertOpen(read.Support); Assert.Empty(E(read.Support));
        Assert.NotEmpty(E(read.Unresolved));
        Assert.True(ContainsReference(read.Unresolved, malformed));
        Assert.True(ContainsReference(read.Unresolved, f.Map.SourceDomain));
        Assert.Empty(E(read.Blocked));
    }

    [Fact]
    public void OwnershipAndOrderedNativePortsAreCheckedEvenWithIdenticalDomains()
    {
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
        var domain = S(a, b, c); var basis = new Placeholder();
        var map = ResolutionCorrespondence.FromRows(basis, basis, domain, domain,
            new Relation(a, b), new Relation(b, c));
        var forward = map.ReadForward(S(b));
        var backward = map.ReadBackward(S(b));
        AssertSet(Read(map, forward).Support, c);
        AssertSet(Read(map, backward).Support, a);
        var other = new ResolutionCorrespondence(basis, basis, domain, domain, map.Mappings);
        Assert.False(other.TryReadResult(forward, out _, out _, out _, out _));

        var source = Assert.IsType<Relation>(forward.Left);
        var header = Assert.IsType<Relation>(source.Left);
        var counterfeitPort = new Relation(basis, domain);
        var counterfeit = new Relation(new Relation(new Relation(map.Surface,
            new Relation(counterfeitPort, map.TargetPort)), source.Right), forward.Right);
        Assert.False(map.TryReadResult(counterfeit, out _, out _, out _, out _));
        Assert.Same(map.Surface, header.Left);
    }

    [Fact]
    public void TotalityAndFunctionalityAreSeparateNativeObligations()
    {
        var f = new Partition(); var complete = Check(f.Map);
        Assert.Equal(5, complete.TotalityPassed.Length);
        Assert.Equal(5, complete.FunctionalityPassed.Length);
        Assert.Empty(complete.TotalityFailed); Assert.Empty(complete.FunctionalityFailed);
        Assert.Empty(complete.Unresolved);

        var relation = ResolutionCorrespondence.FromRows(f.Map.SourceBasis, f.Map.TargetBasis,
            f.Map.SourceDomain, f.Map.TargetDomain, f.Rows[0],
            new Relation(f.Fine[0], f.Coarse[1]), f.Rows[1], f.Rows[2], f.Rows[3]);
        var check = Check(relation);
        Assert.Equal(4, check.TotalityPassed.Length); Assert.Single(check.TotalityFailed);
        Assert.Equal(4, check.FunctionalityPassed.Length); Assert.Single(check.FunctionalityFailed);
        Assert.Empty(check.Unresolved);
        Assert.True(ContainsReference(check.TotalityFailed[0], f.Fine[4]));
        Assert.True(ContainsReference(check.FunctionalityFailed[0], f.Fine[0]));
        Assert.True(ContainsReference(check.FunctionalityFailed[0], f.Coarse[0]));
        Assert.True(ContainsReference(check.FunctionalityFailed[0], f.Coarse[1]));
    }

    [Fact]
    public void DuplicateRowsDoNotCreateNewAlternativesOrAFalseNonfunctionalResult()
    {
        var f = new Partition();
        var duplicate = new Relation(f.Fine[0], f.Coarse[0]);
        var map = ResolutionCorrespondence.FromRows(f.Map.SourceBasis, f.Map.TargetBasis,
            f.Map.SourceDomain, f.Map.TargetDomain, [.. f.Rows, duplicate]);
        var result = Read(map, map.ReadForward(S(f.Fine[0], f.Fine[0])));
        AssertSet(result.Support, f.Coarse[0]);
        Assert.Equal(2, E(result.Provenance).Length);
        var check = Check(map);
        Assert.Equal(5, check.FunctionalityPassed.Length);
        Assert.Empty(check.FunctionalityFailed);
    }

    [Fact]
    public void MonotonePartitionCommutesWithAllSuppliedMaximumPairs()
    {
        var f = new Partition();
        var fineLaw = Maximum(f.Fine); var coarseLaw = Maximum(f.Coarse);
        var proofs = new List<IElement>(); var obligations = new List<IElement>();
        foreach (var a in f.Fine)
            foreach (var b in f.Fine)
            {
                var fine = FiniteLaw.Evaluate(fineLaw, S(a), S(b));
                Assert.True(FiniteLaw.TryReadResult(fine, out var fineOutput, out _, out _));
                var left = f.Map.ReadForward(fineOutput, fine);
                var mappedA = f.Map.ReadForward(S(a)); var mappedB = f.Map.ReadForward(S(b));
                obligations.Add(FiniteLawConformance.Equation(Read(f.Map, left).Support,
                    FiniteLawConformance.Apply(coarseLaw, Read(f.Map, mappedA).Support,
                        Read(f.Map, mappedB).Support)));
                proofs.Add(new Relation(fine, new Relation(left, new Relation(mappedA, mappedB))));
            }
        var report = FiniteLawConformance.Check(coarseLaw, S(obligations.ToArray()));
        var commutingWitness = new Relation(new Relation(f.Map.Surface, S(proofs.ToArray())), report);
        Assert.True(FiniteLawConformance.TryReadReport(report, out _, out _, out var passed,
            out var violated, out var unresolved));
        Assert.Equal(25, E(passed).Length); Assert.Empty(E(violated)); Assert.Empty(E(unresolved));
        Assert.True(ContainsReference(commutingWitness, fineLaw));
        Assert.True(ContainsReference(commutingWitness, coarseLaw));
        Assert.True(ContainsReference(commutingWitness, f.Rows[4]));
        AssertNoCarriers(commutingWitness);
    }

    [Fact]
    public void TotalFunctionalMappingCanStillViolateACommutingLaw()
    {
        var f = new Partition();
        var alteredRow = new Relation(f.Fine[1], f.Coarse[2]);
        var map = ResolutionCorrespondence.FromRows(f.Map.SourceBasis, f.Map.TargetBasis,
            f.Map.SourceDomain, f.Map.TargetDomain,
            f.Rows.Select((row, index) => index == 1 ? alteredRow : row).ToArray());
        var certificate = Check(map);
        Assert.Equal(5, certificate.TotalityPassed.Length);
        Assert.Equal(5, certificate.FunctionalityPassed.Length);
        var fineLaw = Maximum(f.Fine); var coarseLaw = Maximum(f.Coarse);
        var fine = FiniteLaw.Evaluate(fineLaw, S(f.Fine[1]), S(f.Fine[2]));
        Assert.True(FiniteLaw.TryReadResult(fine, out var fineOutput, out _, out _));
        var mapped = map.ReadForward(fineOutput, fine);
        var left = map.ReadForward(S(f.Fine[1])); var right = map.ReadForward(S(f.Fine[2]));
        var obligation = FiniteLawConformance.Equation(Read(map, mapped).Support,
            FiniteLawConformance.Apply(coarseLaw, Read(map, left).Support, Read(map, right).Support));
        var report = FiniteLawConformance.Check(coarseLaw, S(obligation));
        var counterexample = new Relation(new Relation(map.Surface,
            S(fine, mapped, left, right)), report);

        Assert.True(FiniteLawConformance.TryReadReport(report, out _, out _, out var passed,
            out var violated, out var unresolved));
        Assert.Empty(E(passed)); Assert.Single(E(violated)); Assert.Empty(E(unresolved));
        var failed = Assert.IsType<Relation>(Assert.Single(E(violated)));
        Assert.True(FiniteLawConformance.TryReadObligation(failed, out _, out var actual,
            out var expected, out _));
        AssertSet(actual, f.Coarse[1]); AssertSet(expected, f.Coarse[2]);
        Assert.True(ContainsReference(counterexample, alteredRow));
        AssertNoCarriers(counterexample);
    }

    private sealed class Partition
    {
        public IElement[] Fine { get; } = Enumerable.Range(0, 5).Select(_ => (IElement)new Placeholder()).ToArray();
        public IElement[] Coarse { get; } = Enumerable.Range(0, 3).Select(_ => (IElement)new Placeholder()).ToArray();
        public Relation[] Rows { get; }
        public ResolutionCorrespondence Map { get; }
        public Partition()
        {
            Rows = [new(Fine[0], Coarse[0]), new(Fine[1], Coarse[0]), new(Fine[2], Coarse[1]),
                new(Fine[3], Coarse[2]), new(Fine[4], Coarse[2])];
            Map = new ResolutionCorrespondence(new Placeholder(), new Placeholder(), S(Fine), S(Coarse), S(Rows));
        }
    }

    private static Relation Maximum(IElement[] participants)
    {
        var domain = S(participants); var rows = new List<Relation>();
        for (var a = 0; a < participants.Length; a++)
            for (var b = 0; b < participants.Length; b++)
                rows.Add(FiniteLaw.Row(participants[a], participants[b], S(participants[Math.Max(a, b)])));
        return FiniteLaw.Create(domain, domain, domain, rows.ToArray());
    }

    private static (Relation Support, Relation Blocked, Relation Unresolved, Relation Provenance)
        Read(ResolutionCorrespondence map, Relation read)
    {
        Assert.True(map.TryReadResult(read, out var result, out var blocked, out var unresolved, out var provenance));
        return (result, blocked, unresolved, provenance);
    }

    private static (IElement[] TotalityPassed, IElement[] TotalityFailed, IElement[] FunctionalityPassed,
        IElement[] FunctionalityFailed, IElement[] Unresolved) Check(ResolutionCorrespondence map)
    {
        Assert.True(map.TryReadCheck(map.CheckForward(), out var total, out var missing,
            out var functional, out var multiple, out var unresolved));
        return (E(total), E(missing), E(functional), E(multiple), E(unresolved));
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static void AssertSet(Relation support, params IElement[] expected)
    {
        var actual = E(support);
        Assert.Equal(expected.Length, actual.Length);
        Assert.All(expected, participant => Assert.Contains(participant, actual));
    }
    private static void AssertClosed(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail));
        Assert.Equal(seam, tail);
    }
    private static void AssertOpen(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
    }
    private static bool ContainsReference(IElement root, IElement target)
    {
        var pending = new Stack<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (ReferenceEquals(element, target)) return true;
            if (!seen.Add(element) || element is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
        return false;
    }
    private static void AssertNoCarriers(IElement root)
    {
        var pending = new Stack<IElement>(); var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            Assert.IsNotType<Carrier>(element); Assert.IsNotType<CarrierLocation>(element);
            if (element is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
    }
}
