using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FiniteChoiceTests
{
    [Fact]
    public void SelectionRetainsWholeProposalContextChooserAndSamplingEvidence()
    {
        var f = new Fixture();
        var proposalEvidence = new Placeholder();
        var proposal = f.Propose(StructuralConstraint.Closed(f.Left, f.Right), proposalEvidence);
        var chooser = new Placeholder();
        var samplingEvidence = new Relation(new Placeholder(), f.Right);
        var receipt = FiniteChoice.Select(proposal, chooser, f.Right, samplingEvidence);
        Assert.True(FiniteChoice.TryReadSelection(receipt, out var original, out var actualChooser,
            out var candidate, out var sampling, out var result, out var accepted, out var blocked, out var unresolved));
        Assert.Same(proposal, original);
        Assert.Same(chooser, actualChooser);
        Assert.Same(f.Right, candidate);
        Assert.Same(samplingEvidence, sampling);
        Assert.Single(Entries(accepted));
        Assert.Empty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
        Assert.True(FiniteChoice.TryReadSelected(receipt, out var command));
        Assert.Same(result, command);
        Assert.Same(proposal.Left, command.Left);
        Assert.True(QualitativePublication.TryRead(command, out var source, out var subject,
            out var occurrence, out var basis, out var domain, out var support, out var evidence));
        Assert.Same(f.Source, source); Assert.Same(f.Subject, subject); Assert.Same(f.Event, occurrence);
        Assert.Same(f.Basis, basis); Assert.Same(f.Domain, domain);
        Assert.Same(f.Right, Assert.Single(Entries(support)));
        Assert.True(Contains(evidence, proposal));
        Assert.True(Contains(evidence, chooser));
        Assert.True(Contains(evidence, samplingEvidence));
        Assert.True(Contains(evidence, proposalEvidence));
        Assert.True(QualitativePublication.TryRead(proposal, out _, out _, out _, out _, out _, out var oldSupport, out _));
        Assert.Equal(new IElement[] { f.Left, f.Right }, Entries(oldSupport));
        AssertNoNumbers(receipt);
    }

    [Fact]
    public void CallerSuppliesOneSampleAndOnlyOneContinuationIsOffered()
    {
        var f = new Fixture();
        var proposal = f.Propose(f.Domain);
        var samplerCalls = 0;
        IElement ExternalSample()
        {
            samplerCalls++;
            return f.Left;
        }
        var chosen = ExternalSample();
        var receipt = FiniteChoice.Select(proposal, new Placeholder(), chosen, new Relation(new Placeholder(), chosen));
        Assert.Equal(1, samplerCalls);
        Assert.True(FiniteChoice.TryReadSelected(receipt, out var command));
        Assert.True(QualitativePublication.TryRead(command, out _, out _, out _, out _, out _, out var support, out _));
        Assert.Same(f.Left, Assert.Single(Entries(support)));
        var context = new Placeholder();
        var outcomeLeft = new Placeholder(); var outcomeRight = new Placeholder();
        var continuation = FiniteLaw.Create(f.Domain, StructuralConstraint.Closed(context),
            StructuralConstraint.Closed(outcomeLeft, outcomeRight),
            FiniteLaw.Row(f.Left, context, StructuralConstraint.Closed(outcomeLeft)),
            FiniteLaw.Row(f.Right, context, StructuralConstraint.Closed(outcomeRight)));
        var read = FiniteLaw.Evaluate(continuation, support, StructuralConstraint.Closed(context));
        Assert.True(FiniteLaw.TryReadResult(read, out var outcomes, out var unresolved, out var rows));
        Assert.Same(outcomeLeft, Assert.Single(Entries(outcomes)));
        Assert.Empty(Entries(unresolved));
        Assert.Single(Entries(rows));
        Assert.Equal(1, samplerCalls);
        // The unselected proposal remains available for another explicitly
        // authored choice; it is not silently expanded into another execution.
        Assert.Equal(2, Entries(f.Domain).Length);
    }

    [Fact]
    public void OpenProposalRemainsPendingEvenWhenTheSuppliedCandidateIsKnown()
    {
        var f = new Fixture();
        foreach (var support in new[] { StructuralConstraint.Open(), StructuralConstraint.Open(f.Left) })
        {
            var proposal = f.Propose(support);
            var receipt = FiniteChoice.Select(proposal, new Placeholder(), f.Left, new Placeholder());
            Refused(receipt, proposal, pending: true);
            Assert.True(Contains(receipt, support));
        }
    }

    [Fact]
    public void EmptyForeignAndExcludedChoicesAreBlockedWithoutInventingAnAction()
    {
        var f = new Fixture();
        var proposals = new[]
        {
            f.Propose(StructuralConstraint.Closed()),
            f.Propose(StructuralConstraint.Closed(f.Left, new Placeholder())),
            f.Propose(StructuralConstraint.Closed(f.Right))
        };
        foreach (var proposal in proposals)
            Refused(FiniteChoice.Select(proposal, new Placeholder(), f.Left, new Placeholder()), proposal, pending: false);
        var admitted = f.Propose(f.Domain);
        Refused(FiniteChoice.Select(admitted, new Placeholder(), new Placeholder(), new Placeholder()), admitted, pending: false);
    }

    [Fact]
    public void EqualLookingActionRelationsDoNotAcquireTheOriginalActionIdentity()
    {
        var f = new Fixture();
        var proposal = f.Propose(f.Domain);
        var copy = new Relation(f.Left.Left, f.Left.Right);
        Refused(FiniteChoice.Select(proposal, new Placeholder(), copy, new Placeholder()), proposal, pending: false);
    }

    [Fact]
    public void DuplicateSupportEntriesRemainOriginalEvidenceButProduceOneSelectedCommand()
    {
        var f = new Fixture();
        var support = StructuralConstraint.Closed(f.Left, f.Left);
        var proposal = f.Propose(support);
        var receipt = FiniteChoice.Select(proposal, new Placeholder(), f.Left, new Placeholder());
        Assert.True(FiniteChoice.TryReadSelected(receipt, out var command));
        Assert.True(QualitativePublication.TryRead(command, out _, out _, out _, out _, out _, out var selected, out _));
        Assert.Same(f.Left, Assert.Single(Entries(selected)));
        Assert.Equal(2, Entries(support).Length);
    }

    [Fact]
    public void DifferentChoosersRemainDifferentOperationsEvenWhenTheyChooseTheSameAction()
    {
        var f = new Fixture();
        var proposal = f.Propose(f.Domain);
        var firstChooser = new Placeholder(); var secondChooser = new Placeholder();
        var samplingEvidence = new Placeholder();
        var first = FiniteChoice.Select(proposal, firstChooser, f.Left, samplingEvidence);
        var second = FiniteChoice.Select(proposal, secondChooser, f.Left, samplingEvidence);
        Assert.True(FiniteChoice.TryReadSelected(first, out var firstCommand));
        Assert.True(FiniteChoice.TryReadSelected(second, out var secondCommand));
        Assert.NotSame(firstCommand, secondCommand);
        Assert.True(Contains(firstCommand, firstChooser));
        Assert.False(Contains(firstCommand, secondChooser));
        Assert.True(Contains(secondCommand, secondChooser));
        Assert.True(Contains(firstCommand, proposal));
        Assert.True(Contains(secondCommand, proposal));
    }

    [Fact]
    public void ChangedOwnerSubjectEventBasisOrDomainCannotReuseCopiedSuccessFields()
    {
        var f = new Fixture();
        var proposal = f.Propose(f.Domain);
        var receipt = FiniteChoice.Select(proposal, new Placeholder(), f.Left, new Placeholder());
        Assert.True(FiniteChoice.TryReadSelected(receipt, out var command));
        Assert.True(QualitativePublication.TryRead(command, out _, out _, out _, out _, out _, out var support, out var evidence));
        var replacements = new[]
        {
            QualitativePublication.Create(new Placeholder(), f.Subject, f.Event, f.Basis, f.Domain, support, evidence),
            QualitativePublication.Create(f.Source, new Placeholder(), f.Event, f.Basis, f.Domain, support, evidence),
            QualitativePublication.Create(f.Source, f.Subject, new Placeholder(), f.Basis, f.Domain, support, evidence),
            QualitativePublication.Create(f.Source, f.Subject, f.Event, new Placeholder(), f.Domain, support, evidence),
            QualitativePublication.Create(f.Source, f.Subject, f.Event, f.Basis,
                StructuralConstraint.Closed(f.Left, f.Right), support, evidence)
        };
        foreach (var replacement in replacements)
            Assert.False(FiniteChoice.TryReadSelected(ReplaceCommand(receipt, replacement), out _));
    }

    [Fact]
    public void ChangedOutputAndMissingOperationEvidenceCannotReuseCopiedSuccessFields()
    {
        var f = new Fixture();
        var proposal = f.Propose(f.Domain);
        var receipt = FiniteChoice.Select(proposal, new Placeholder(), f.Left, new Placeholder());
        Assert.True(FiniteChoice.TryReadSelected(receipt, out var command));
        Assert.True(QualitativePublication.TryRead(command, out _, out _, out _, out _, out _, out _, out var evidence));
        foreach (var support in new[] { f.Domain, StructuralConstraint.Closed(), StructuralConstraint.Closed(f.Right), StructuralConstraint.Open(f.Left) })
        {
            var replacement = new Relation(command.Left, new Relation(support, evidence));
            Assert.False(FiniteChoice.TryReadSelected(ReplaceCommand(receipt, replacement), out _));
        }
        var lostEvidence = new Relation(command.Left, new Relation(StructuralConstraint.Closed(f.Left), new Placeholder()));
        Assert.False(FiniteChoice.TryReadSelected(ReplaceCommand(receipt, lostEvidence), out _));
    }

    [Fact]
    public void ForgedAcceptedScopeCannotAdmitACandidateExcludedByTheOriginalProposal()
    {
        var f = new Fixture();
        var proposal = f.Propose(StructuralConstraint.Closed(f.Right));
        var refused = FiniteChoice.Select(proposal, new Placeholder(), f.Left, new Placeholder());
        var selection = Assert.IsType<Relation>(refused.Left);
        var checks = StructuralConstraint.Closed(new Placeholder(), new Placeholder(), new Placeholder());
        var command = new Relation(proposal.Left,
            new Relation(StructuralConstraint.Closed(f.Left), new Relation(selection, checks)));
        var forged = new Relation(selection, new Relation(new Relation(command, StructuralConstraint.Closed(selection)),
            new Relation(StructuralConstraint.Closed(), new Relation(StructuralConstraint.Closed(), checks))));
        Assert.False(FiniteChoice.TryReadSelected(forged, out _));
    }

    [Fact]
    public void MarkerOnlyChecksCannotReplaceNativeMembershipEvidence()
    {
        var f = new Fixture();
        var proposal = f.Propose(f.Domain);
        var receipt = FiniteChoice.Select(proposal, new Placeholder(), f.Left, new Placeholder());
        var selection = Assert.IsType<Relation>(receipt.Left);
        var checks = StructuralConstraint.Closed(new Placeholder(), new Placeholder(), new Placeholder(),
            new Placeholder(), new Placeholder());
        var command = new Relation(proposal.Left,
            new Relation(StructuralConstraint.Closed(f.Left), new Relation(selection, checks)));
        var forged = new Relation(selection, new Relation(new Relation(command, StructuralConstraint.Closed(selection)),
            new Relation(StructuralConstraint.Closed(), new Relation(StructuralConstraint.Closed(), checks))));
        Assert.False(FiniteChoice.TryReadSelected(forged, out _));
    }

    [Fact]
    public void MalformedProposalAndMalformedReceiptRemainUnreadableOrPending()
    {
        var malformed = new Relation(new Placeholder(), new Placeholder());
        var receipt = FiniteChoice.Select(malformed, new Placeholder(), new Placeholder(), new Placeholder());
        Refused(receipt, malformed, pending: true);
        Assert.False(FiniteChoice.TryReadSelection(malformed, out _, out _, out _, out _, out _, out _, out _, out _));
        Assert.False(FiniteChoice.TryReadSelected(malformed, out _));
    }

    private static Relation ReplaceCommand(Relation receipt, Relation command)
    {
        var result = (Relation)receipt.Right;
        var oldResult = (Relation)result.Left;
        return new Relation(receipt.Left, new Relation(new Relation(command, oldResult.Right), result.Right));
    }

    private static void Refused(Relation receipt, Relation original, bool pending)
    {
        Assert.True(FiniteChoice.TryReadSelection(receipt, out var proposal, out _, out _, out _,
            out var command, out var accepted, out var blocked, out var unresolved));
        Assert.Same(original, proposal);
        Assert.Same(original, command);
        Assert.Empty(Entries(accepted));
        if (pending) { Assert.Empty(Entries(blocked)); Assert.NotEmpty(Entries(unresolved)); }
        else { Assert.NotEmpty(Entries(blocked)); Assert.Empty(Entries(unresolved)); }
        Assert.False(FiniteChoice.TryReadSelected(receipt, out _));
    }

    private sealed class Fixture
    {
        public IElement Source { get; } = new Placeholder();
        public IElement Subject { get; } = new Placeholder();
        public IElement Event { get; } = new Placeholder();
        public IElement Basis { get; } = new Placeholder();
        public Relation Left { get; }
        public Relation Right { get; }
        public Relation Domain { get; }

        public Fixture()
        {
            var origin = new Placeholder();
            Left = new Relation(origin, new Placeholder());
            Right = new Relation(origin, new Placeholder());
            Domain = StructuralConstraint.Closed(Left, Right);
        }

        public Relation Propose(Relation support, IElement? evidence = null) =>
            QualitativePublication.Create(Source, Subject, Event, Basis, Domain, support, evidence);
    }

    private static IElement[] Entries(Relation scope) => FiniteLaw.Entries(scope);

    private static bool Contains(IElement root, IElement target) =>
        Traverse(root).Any(element => ReferenceEquals(element, target));

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var element in Traverse(root))
        {
            Assert.IsNotType<Carrier>(element);
            Assert.IsNotType<CarrierLocation>(element);
        }
    }

    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<IElement>();
        stack.Push(root);
        while (stack.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is Relation relation)
            {
                stack.Push(relation.Left);
                stack.Push(relation.Right);
            }
        }
    }
}
