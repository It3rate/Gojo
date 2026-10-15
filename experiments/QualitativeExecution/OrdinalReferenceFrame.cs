using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Exact ordinal comparisons at one declared resolution. Reference identities
/// may be a fixed marker or retained observations; none stores a numeric sample.
/// Equal means the same resolved position under this order's contract. It is
/// not the threshold detector's finite band or a universal numerical zero.
/// </summary>
public sealed class OrdinalReferenceFrame
{
    public Relation Order { get; }
    public IElement Resolution { get; }
    public Relation Lower { get; }
    public Relation Equal { get; }
    public Relation Higher { get; }
    public Relation Domain { get; }
    public Relation CompositionLaw { get; }
    public Relation Surface { get; }
    public FiniteLawPlan Plan { get; }

    public OrdinalReferenceFrame(Relation? order = null, IElement? resolution = null)
    {
        Order = order ?? new Relation(new Placeholder(), new Placeholder());
        Resolution = resolution ?? new Relation(Order, new Placeholder());
        Lower = Order.Reverse();
        Equal = new Relation(Order.Left, Order.Left);
        Higher = Order;
        Domain = StructuralConstraint.Closed(Lower, Equal, Higher);
        CompositionLaw = FiniteLaw.Create(Domain, Domain, Domain,
            FiniteLaw.Row(Lower, Lower, StructuralConstraint.Closed(Lower)),
            FiniteLaw.Row(Lower, Equal, StructuralConstraint.Closed(Lower)),
            FiniteLaw.Row(Lower, Higher, Domain),
            FiniteLaw.Row(Equal, Lower, StructuralConstraint.Closed(Lower)),
            FiniteLaw.Row(Equal, Equal, StructuralConstraint.Closed(Equal)),
            FiniteLaw.Row(Equal, Higher, StructuralConstraint.Closed(Higher)),
            FiniteLaw.Row(Higher, Lower, Domain),
            FiniteLaw.Row(Higher, Equal, StructuralConstraint.Closed(Higher)),
            FiniteLaw.Row(Higher, Higher, StructuralConstraint.Closed(Higher)));
        Surface = new Relation(new Relation(Order, Resolution),
            new Relation(Domain, CompositionLaw));
        Plan = FiniteLawPlan.Capture(CompositionLaw) ??
            throw new InvalidOperationException("The authored ordinal law must be readable.");
    }

    // publication = ((source -> (subject -> reference)) -> (frame -> context))
    //               -> (support -> evidence).
    // A context may expose an event pair, detector window or retained-reference
    // transition. Its identity never implies the same context as another read.
    public Relation Observe(IElement source, IElement subject, IElement reference,
        IElement context, Relation support, IElement? evidence = null) =>
        new(new Relation(new Relation(source, new Relation(subject, reference)),
                new Relation(Surface, context)),
            new Relation(support, evidence ?? StructuralConstraint.Closed()));

    /// <summary>
    /// Reads only this frame's exact publication shape. A support may remain
    /// open, empty or foreign; subsequent native law reads retain those states.
    /// </summary>
    public bool TryRead(Relation publication, out IElement source,
        out IElement subject, out IElement reference, out IElement context,
        out Relation support, out IElement evidence)
    {
        source = subject = reference = context = evidence = null!;
        support = null!;
        if (publication.Left is not Relation
            {
                Left: Relation { Right: Relation pair } identity,
                Right: Relation contract
            } || !ReferenceEquals(contract.Left, Surface) ||
            publication.Right is not Relation { Left: Relation alternatives } reading ||
            !StructuralConstraint.TryReadScope(alternatives, out _, out _, out _)) return false;
        source = identity.Left;
        subject = pair.Left;
        reference = pair.Right;
        context = contract.Right;
        support = alternatives;
        evidence = reading.Right;
        return true;
    }

    /// <summary>
    /// Composes a:b and b:c through their actual shared reference. Different
    /// frames, sources and disconnected pivots remain the original failed pair.
    /// Mixed directions admit all three outcomes; no distance is invented.
    /// </summary>
    public Relation Compose(Relation first, Relation second, bool compiled = false)
    {
        var offered = new Relation(first, second);
        if (!TryRead(first, out var source, out var subject, out var pivot,
                out var firstContext, out var firstSupport, out _) ||
            !TryRead(second, out var otherSource, out var otherPivot, out var reference,
                out var secondContext, out var secondSupport, out _) ||
            !FiniteLaw.SameParticipant(source, otherSource) ||
            !FiniteLaw.SameParticipant(pivot, otherPivot)) return offered;

        var read = compiled ? Plan.Evaluate(firstSupport, secondSupport)
            : FiniteLaw.Evaluate(CompositionLaw, firstSupport, secondSupport);
        read = FiniteLaw.WithProvenance(read, new Relation(offered, pivot));
        FiniteLaw.TryReadResult(read, out var support, out _, out _);
        return Observe(source, subject, reference,
            new Relation(firstContext, secondContext), support, read);
    }
}
