using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.PrimitiveRuntime;

/// <summary>
/// A selected carrier-free application passage. The exact pattern requires
/// closed singleton input, command and result scopes, and one consumed native
/// proof row with the same participants and a closed singleton output.
/// The shared structural matcher and ApplyUnique implement the policy; compact
/// execution may capture their answer for each atomic pair.
/// </summary>
public sealed class TransitionApplicationGuard
{
    public Relation Law { get; }
    public Relation Pattern { get; }
    public Relation Variables { get; }
    public Placeholder OutputSlot { get; }
    public Relation Surface { get; }

    public TransitionApplicationGuard(Relation law)
    {
        if (!FiniteLaw.TryRead(law, out _, out _, out _, out _))
            throw new ArgumentException("Expected a native finite law.", nameof(law));
        Law = law;
        var before = new Placeholder();
        var command = new Placeholder();
        OutputSlot = new Placeholder();
        var evidence = new Placeholder();
        var selected = new List<IElement> { before, command, OutputSlot, evidence };
        Relation One(IElement item)
        {
            // Reusing the selected seam at both ends demands actual closure.
            // The one spine cell demands exactly one participant, not merely
            // an eventual union containing one distinct visible outcome.
            var seam = new Placeholder(); selected.Add(seam);
            return new Relation(seam, new Relation(item, seam));
        }
        var row = new Relation(new Relation(before, command), One(OutputSlot));
        Pattern = new Relation(evidence, new Relation(One(before),
            new Relation(One(command), new Relation(One(OutputSlot), One(row)))));
        Variables = StructuralConstraint.Closed(selected.ToArray());
        Surface = new Relation(Law, new Relation(Pattern, new Relation(Variables, OutputSlot)));
    }

    private TransitionApplicationGuard(Relation surface, Relation law, Relation pattern,
        Relation variables, Placeholder output)
    { Surface = surface; Law = law; Pattern = pattern; Variables = variables; OutputSlot = output; }

    /// <summary>Reuses the actual captured guard shape after independent arena
    /// reconstruction. It does not replace that shape with a newly authored one.</summary>
    public static TransitionApplicationGuard FromSurface(Relation surface)
    {
        if (surface.Left is not Relation law ||
            surface.Right is not Relation { Left: Relation pattern, Right: Relation body } ||
            body.Left is not Relation variables || body.Right is not Placeholder output ||
            !FiniteLaw.TryRead(law, out _, out _, out _, out _) ||
            !StructuralConstraint.TryReadScope(variables, out var selected, out var seam, out var tail) ||
            !seam.Equals(tail) || !selected.All(item => item is Placeholder) ||
            !selected.Any(item => item is Placeholder slot && slot.Equals(output)))
            throw new ArgumentException("Expected a captured transition application surface.", nameof(surface));
        return new(surface, law, pattern, variables, output);
    }

    /// <summary>Returns ordinary constraint evidence, including the complete
    /// finite-law read. Open inputs and competing actual routes remain visible.</summary>
    public Relation Evaluate(Relation before, Relation command)
    {
        var native = FiniteLaw.Evaluate(Law, before, command);
        if (!FiniteLaw.TryReadResult(native, out var result, out _, out var proofs))
            throw new InvalidOperationException("Finite-law reader returned an unreadable witness.");
        var offered = new Relation(native, new Relation(before,
            new Relation(command, new Relation(result, proofs))));
        return StructuralConstraint.Match(Pattern, Variables, StructuralConstraint.Closed(offered));
    }

    /// <summary>Refusal returns the unresolved output slot. A caller retains its
    /// prior binding instead of treating that slot as an applied result.</summary>
    public bool TryApply(Relation before, Relation command, out IElement applied, out Relation read)
    {
        read = Evaluate(before, command);
        return StructuralConstraint.ApplyUnique(read, OutputSlot, out applied);
    }
}
