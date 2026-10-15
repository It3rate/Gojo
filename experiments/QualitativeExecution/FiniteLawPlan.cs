using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A derived finite-mask backend for one native law. Bits address that law's
/// participant identities; they are not category names, magnitudes or primitives.
/// A native graph edit invalidates the plan until it is explicitly recaptured.
/// </summary>
public sealed class FiniteLawPlan
{
    private readonly IElement[] leftStates;
    private readonly IElement[] rightStates;
    private readonly IElement[] outputStates;
    private readonly Relation outputDomain;
    private readonly byte[] outputs;
    private readonly Relation[][] consumedRows;
    private readonly (Relation Site, IElement Left, IElement Right)[] relationSites;
    private readonly (TickRef Site, long Value)[] tickSites;
    private readonly int rightMaskCount;

    /// <summary>The actual connected law from which the execution table derives.</summary>
    public Relation SourceLaw { get; }

    /// <summary>False after any captured relation endpoint or tick is retargeted.</summary>
    public bool IsCurrent => Intact();

    private FiniteLawPlan(Relation law, IElement[] left, IElement[] right, IElement[] output,
        Relation outputDomain, byte[] outputs, Relation[][] consumedRows)
    {
        SourceLaw = law; leftStates = left; rightStates = right; outputStates = output;
        this.outputDomain = outputDomain; this.outputs = outputs; this.consumedRows = consumedRows;
        rightMaskCount = 1 << right.Length;
        var relations = new List<(Relation, IElement, IElement)>();
        var ticks = new List<(TickRef, long)>();
        var seenRelations = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var seenTicks = new HashSet<TickRef>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(law);
        while (pending.TryPop(out var element))
            if (element is Relation relation && seenRelations.Add(relation))
            {
                relations.Add((relation, relation.Left, relation.Right));
                pending.Push(relation.Left); pending.Push(relation.Right);
            }
            else if (element is CarrierLocation location && seenTicks.Add(location.Tick))
                ticks.Add((location.Tick, location.Ticks));
            else if (element is TickRef tick && seenTicks.Add(tick)) ticks.Add((tick, tick.Ticks));
        relationSites = relations.ToArray(); tickSites = ticks.ToArray();
    }

    /// <summary>Validates closed finite rows, then interprets each atomic pair
    /// through FiniteLaw.Evaluate. No second domain algebra is introduced.</summary>
    public static FiniteLawPlan? Capture(Relation law)
    {
        if (!FiniteLaw.TryRead(law, out var leftDomain, out var rightDomain, out var outputDomain, out var rows)) return null;
        FiniteLaw.DomainEntries(leftDomain, out var left);
        FiniteLaw.DomainEntries(rightDomain, out var right);
        FiniteLaw.DomainEntries(outputDomain, out var output);
        if (left.Length > 8 || right.Length > 8 || output.Length > 8) return null;
        foreach (var row in rows)
        {
            if (!FiniteLaw.TryReadRow(row, out var a, out var b, out var support) ||
                FiniteLaw.IndexOf(left, a) < 0 || FiniteLaw.IndexOf(right, b) < 0 ||
                !TryEncode(support, output, out _)) return null;
        }
        var atomic = new byte[left.Length * right.Length];
        var consumed = new Relation[atomic.Length][];
        for (var i = 0; i < left.Length; i++)
            for (var j = 0; j < right.Length; j++)
            {
                var read = FiniteLaw.Evaluate(law, StructuralConstraint.Closed(left[i]), StructuralConstraint.Closed(right[j]));
                if (!FiniteLaw.TryReadResult(read, out var support, out var residue, out var provenance) ||
                    FiniteLaw.Entries(residue).Length != 0 || !TryEncode(support, output, out var mask)) return null;
                var index = i * right.Length + j;
                atomic[index] = mask;
                consumed[index] = FiniteLaw.Entries(provenance).Cast<Relation>().ToArray();
            }
        var rightMaskCount = 1 << right.Length;
        var table = new byte[(1 << left.Length) * rightMaskCount];
        for (var a = 0; a < 1 << left.Length; a++)
            for (var b = 0; b < rightMaskCount; b++)
                for (var i = 0; i < left.Length; i++)
                    if ((a & 1 << i) != 0)
                        for (var j = 0; j < right.Length; j++)
                            if ((b & 1 << j) != 0) table[a * rightMaskCount + b] |= atomic[i * right.Length + j];
        return new FiniteLawPlan(law, left, right, output, outputDomain, table, consumed);
    }

    public bool TryEncodeLeft(Relation support, out byte mask) => EncodeCurrent(support, leftStates, out mask);
    public bool TryEncodeRight(Relation support, out byte mask) => EncodeCurrent(support, rightStates, out mask);
    public bool TryEncodeOutput(Relation support, out byte mask) => EncodeCurrent(support, outputStates, out mask);

    /// <summary>Compute-only read with no witness allocation. The masks are
    /// meaningful only in this captured law's bases. Closed empty support is 0;
    /// open support is never silently encoded as that empty mask.</summary>
    public bool TryEvaluateMasks(byte left, byte right, out byte result)
    {
        result = 0;
        if (!Intact() || left >= 1 << leftStates.Length || right >= rightMaskCount) return false;
        result = outputs[left * rightMaskCount + right];
        return true;
    }

    /// <summary>Witness-producing execution. Closed compatible supports use the
    /// derived table; open/incompatible supports retain the native reader's
    /// unresolved semantics. This mode includes graph materialization costs.</summary>
    public Relation Evaluate(Relation left, Relation right)
    {
        if (!Intact()) return FiniteLaw.Unresolved(SourceLaw, left, right, StaleEvidence());
        if (!TryEncode(left, leftStates, out var a) || !TryEncode(right, rightStates, out var b))
            return FiniteLaw.Evaluate(SourceLaw, left, right);
        var mask = outputs[a * rightMaskCount + b];
        var support = StructuralConstraint.Closed(outputStates.Where((_, i) => (mask & 1 << i) != 0).ToArray());
        var used = new List<IElement>();
        for (var i = 0; i < leftStates.Length; i++)
            if ((a & 1 << i) != 0)
                for (var j = 0; j < rightStates.Length; j++)
                    if ((b & 1 << j) != 0)
                        foreach (var row in consumedRows[i * rightStates.Length + j])
                            if (!used.Any(prior => ReferenceEquals(prior, row))) used.Add(row);
        return FiniteLaw.Witness(SourceLaw, left, right, outputDomain, support,
            StructuralConstraint.Closed(), StructuralConstraint.Closed(used.ToArray()));
    }

    private bool EncodeCurrent(Relation support, IElement[] states, out byte mask)
    {
        mask = 0;
        return Intact() && TryEncode(support, states, out mask);
    }

    private static bool TryEncode(Relation support, IElement[] states, out byte mask)
    {
        mask = 0;
        if (!FiniteLaw.ClosedEntries(support, out var entries)) return false;
        foreach (var entry in entries)
        {
            var index = FiniteLaw.IndexOf(states, entry);
            if (index < 0) { mask = 0; return false; }
            mask |= (byte)(1 << index);
        }
        return true;
    }

    private bool Intact()
    {
        foreach (var site in relationSites)
            if (!ReferenceEquals(site.Site.Left, site.Left) || !ReferenceEquals(site.Site.Right, site.Right)) return false;
        foreach (var site in tickSites)
            if (site.Site.Ticks != site.Value) return false;
        return true;
    }

    private Relation StaleEvidence()
    {
        var changed = new List<IElement>();
        foreach (var site in relationSites)
            if (!ReferenceEquals(site.Site.Left, site.Left) || !ReferenceEquals(site.Site.Right, site.Right))
                changed.Add(new Relation(site.Site, new Relation(site.Left, site.Right)));
        foreach (var site in tickSites)
            if (site.Site.Ticks != site.Value) changed.Add(new Relation(site.Site, new TickRef(site.Value)));
        return new Relation(SourceLaw, StructuralConstraint.Closed(changed.ToArray()));
    }
}
