using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class ReturnEvidenceTests
{
    [Fact]
    public void UpThenDownLeavesReturnOpenUntilASeparateComparisonSuppliesEquality()
    {
        var frame = new OrdinalReferenceFrame();
        var source = new Placeholder();
        var start = new Placeholder();
        var raised = new Placeholder();
        var end = new Placeholder();
        var upEvent = new Placeholder();
        var downEvent = new Placeholder();
        var returnEvent = new Placeholder();
        var up = frame.Observe(source, raised, start, upEvent,
            StructuralConstraint.Closed(frame.Higher));
        var down = frame.Observe(source, end, raised, downEvent,
            StructuralConstraint.Closed(frame.Lower));
        var composed = frame.Compose(down, up);
        Assert.True(frame.TryRead(composed, out var composedSource, out var subject,
            out var reference, out _, out var alternatives, out var compositionProof));
        Assert.Same(source, composedSource);
        Assert.Same(end, subject);
        Assert.Same(start, reference);
        Assert.Equal(Entries(frame.Domain), Entries(alternatives));
        Assert.True(Contains(compositionProof, up));
        Assert.True(Contains(compositionProof, down));

        // The return is a new detector publication, rather than a consequence
        // silently inserted into the mixed-direction composition table.
        var observedReturn = frame.Observe(source, end, start, returnEvent,
            StructuralConstraint.Closed(frame.Equal), new Relation(up, down));
        Assert.True(frame.TryRead(observedReturn, out _, out var returnedSubject,
            out var returnedReference, out var observedEvent, out var returnedSupport, out _));
        Assert.Same(end, returnedSubject);
        Assert.Same(start, returnedReference);
        Assert.Same(returnEvent, observedEvent);
        var selected = new Placeholder();
        var match = StructuralConstraint.Match(selected, StructuralConstraint.Closed(selected),
            alternatives, new Relation(selected, Assert.Single(Entries(returnedSupport))));
        Assert.True(StructuralConstraint.TryReadUnique(match, out var resolvedRelation, out _));
        Assert.Same(frame.Equal, resolvedRelation);
        var witnessedSelection = new Relation(new Relation(composed, observedReturn), match);
        Assert.True(Contains(witnessedSelection, compositionProof));
        Assert.True(Contains(witnessedSelection, observedReturn));
        Assert.NotSame(start, end); // Same resolved position is not sample identity.
        AssertNoNumbers(witnessedSelection);
    }

    [Fact]
    public void SameActualUpAndDownOperationsCanReturnAtOneSiteWithoutBeingGlobalInverses()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var c = new Placeholder();
        var d = new Placeholder();
        var e = new Placeholder();
        var order = StructuralConstraint.Closed(new Relation(a, b), new Relation(b, c),
            new Relation(c, d), new Relation(d, e));
        var sites = StructuralConstraint.Closed(a, b, c, d, e);
        var context = new Placeholder();
        var contextSupport = StructuralConstraint.Closed(context);
        var fAtA = FiniteLaw.Row(a, context, StructuralConstraint.Closed(c));
        var fAtB = FiniteLaw.Row(b, context, StructuralConstraint.Closed(e));
        var gAtC = FiniteLaw.Row(c, context, StructuralConstraint.Closed(a));
        var gAtE = FiniteLaw.Row(e, context, StructuralConstraint.Closed(d));
        var forward = FiniteLaw.Create(sites, contextSupport, sites, fAtA, fAtB);
        var backward = FiniteLaw.Create(sites, contextSupport, sites, gAtC, gAtE);
        var operationPair = new Relation(new Relation(forward, backward), new Relation(order, context));

        var firstForward = FiniteLaw.Evaluate(forward, StructuralConstraint.Closed(a), contextSupport);
        var firstReturn = Continue(backward, firstForward, contextSupport);
        Assert.Same(c, SingleResult(firstForward));
        Assert.Same(a, SingleResult(firstReturn));
        Assert.True(Contains(firstReturn, fAtA));
        Assert.True(Contains(firstReturn, gAtC));

        // Exactly the same law roots, receiver context and resolution are used.
        // b -> e goes up and e -> d goes down in the declared opaque order, but
        // d is above b. No calibrated distances or per-site operation tags exist.
        var secondForward = FiniteLaw.Evaluate(forward, StructuralConstraint.Closed(b), contextSupport);
        var secondReturn = Continue(backward, secondForward, contextSupport);
        Assert.Same(e, SingleResult(secondForward));
        Assert.Same(d, SingleResult(secondReturn));
        Assert.NotSame(b, SingleResult(secondReturn));
        Assert.True(Contains(secondReturn, fAtB));
        Assert.True(Contains(secondReturn, gAtE));
        Assert.True(Contains(firstReturn, forward));
        Assert.True(Contains(firstReturn, backward));
        Assert.True(Contains(secondReturn, forward));
        Assert.True(Contains(secondReturn, backward));

        var frame = new OrdinalReferenceFrame(new Relation(a, e));
        var source = new Placeholder();
        var firstUp = frame.Observe(source, c, a, new Placeholder(),
            StructuralConstraint.Closed(frame.Higher), new Relation(order, firstForward));
        var firstDown = frame.Observe(source, a, c, new Placeholder(),
            StructuralConstraint.Closed(frame.Lower), new Relation(order, firstReturn));
        var secondUp = frame.Observe(source, e, b, new Placeholder(),
            StructuralConstraint.Closed(frame.Higher), new Relation(order, secondForward));
        var secondDown = frame.Observe(source, d, e, new Placeholder(),
            StructuralConstraint.Closed(frame.Lower), new Relation(order, secondReturn));
        AssertAllDirections(frame, frame.Compose(firstDown, firstUp));
        AssertAllDirections(frame, frame.Compose(secondDown, secondUp));
        var evidence = new Relation(operationPair,
            StructuralConstraint.Closed(firstReturn, secondReturn, firstUp, firstDown, secondUp, secondDown));
        AssertNoNumbers(evidence);
        // This is a counterexample to global inversion, not a likelihood model
        // and not proof that the two declared operation surfaces are unrelated.
    }

    [Fact]
    public void RepeatedReturnPublicationsKeepTheirEventsAndSharedEvidenceVisible()
    {
        var frame = new OrdinalReferenceFrame();
        var source = new Placeholder();
        var reference = new Placeholder();
        var firstEnd = new Placeholder();
        var secondEnd = new Placeholder();
        var firstEvent = new Placeholder();
        var secondEvent = new Placeholder();
        var sharedRoute = new Relation(new Placeholder(), new Placeholder());
        var first = frame.Observe(source, firstEnd, reference, firstEvent,
            StructuralConstraint.Closed(frame.Equal), sharedRoute);
        var second = frame.Observe(source, secondEnd, reference, secondEvent,
            StructuralConstraint.Closed(frame.Equal), sharedRoute);
        var replay = frame.Observe(source, firstEnd, reference, firstEvent,
            StructuralConstraint.Closed(frame.Equal), first);
        Assert.True(frame.TryRead(first, out _, out _, out _, out var eventA, out _, out var evidenceA));
        Assert.True(frame.TryRead(second, out _, out _, out _, out var eventB, out _, out var evidenceB));
        Assert.True(frame.TryRead(replay, out _, out _, out _, out var replayEvent, out _, out var replayProof));
        Assert.NotSame(eventA, eventB);
        Assert.Same(eventA, replayEvent);
        Assert.Same(sharedRoute, evidenceA);
        Assert.Same(sharedRoute, evidenceB);
        Assert.Same(first, replayProof);
        var history = StructuralConstraint.Closed(first, second, replay);
        AssertNoNumbers(history);
        // Distinct events preserve repeated evidence without asserting
        // independence; republishing the first event does not create a new one.
    }

    private static Relation Continue(Relation law, Relation earlier, Relation context)
    {
        Assert.True(FiniteLaw.TryReadResult(earlier, out var input, out var residue, out _));
        Assert.Empty(Entries(residue));
        return FiniteLaw.WithProvenance(FiniteLaw.Evaluate(law, input, context), earlier);
    }

    private static IElement SingleResult(Relation read)
    {
        Assert.True(FiniteLaw.TryReadResult(read, out var support, out var residue, out _));
        Assert.Empty(Entries(residue));
        return Assert.Single(Entries(support));
    }

    private static void AssertAllDirections(OrdinalReferenceFrame frame, Relation comparison)
    {
        Assert.True(frame.TryRead(comparison, out _, out _, out _, out _, out var support, out _));
        Assert.Equal(Entries(frame.Domain), Entries(support));
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail));
        Assert.Equal(seam, tail);
        return entries;
    }

    private static bool Contains(IElement root, IElement target) =>
        Traverse(root).Any(participant => ReferenceEquals(participant, target));

    private static void AssertNoNumbers(IElement root)
    {
        foreach (var participant in Traverse(root))
        {
            Assert.IsNotType<Carrier>(participant);
            Assert.IsNotType<CarrierLocation>(participant);
        }
    }

    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<IElement>();
        stack.Push(root);
        while (stack.TryPop(out var current))
        {
            if (!seen.Add(current)) continue;
            yield return current;
            if (current is Relation relation)
            {
                stack.Push(relation.Left);
                stack.Push(relation.Right);
            }
        }
    }
}
