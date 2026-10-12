using Gojo.Core;
using Gojo.Equations;
using Gojo.Reads;

namespace Gojo.Structures;

/// <summary>
/// A discrete, non-draining choice over occupied one-tick selector participation.
/// Scope, offered support bands, sources and calibrations are ordinary relations.
/// Gaps, overlapping offers and incompatible reads retain an open result.
/// </summary>
public static class ScopedChoice
{
    // G(selector source, selector scope, G(G(support band, source, calibration)...), output).
    // A selector at n occupies [n,n+1); offered bands are forward, half-open spans.
    public static Relation Create(IElement selector, Relation scope, Carrier output, params Relation[] offers) =>
        Relation.OrderedGroup(new Carrier(), selector, scope,
            Relation.OrderedGroup(new Carrier(), offers), output);

    public static Relation Offer(Relation support, IElement source, Relation calibration) =>
        Relation.OrderedGroup(new Carrier(), support, source, calibration);

    public static bool TryRead(Relation choice, out IElement selector, out Relation scope,
        out Relation[] offers, out Carrier output)
    {
        selector = null!; scope = null!; output = null!; offers = [];
        if (!choice.TryReadOrderedGroup(out var fields) || fields.Length != 4 ||
            fields[1] is not Relation domain || !TrySpan(domain, out _, out _, out _) ||
            fields[2] is not Relation group || !group.TryReadOrderedGroup(out var entries) ||
            fields[3] is not Carrier target || target.IsUnknown || entries.Any(e => e is not Relation)) return false;
        selector = fields[0]; scope = domain; offers = entries.Cast<Relation>().ToArray(); output = target;
        return true;
    }

    public static bool TryReadOffer(Relation offer, out Relation support, out IElement source, out Relation calibration)
    {
        support = calibration = null!; source = null!;
        if (!offer.TryReadOrderedGroup(out var fields) || fields.Length != 3 ||
            fields[0] is not Relation band || !TrySpan(band, out _, out _, out _) || fields[2] is not Relation map) return false;
        support = band; source = fields[1]; calibration = map; return true;
    }

    public static bool TrySpan(Relation span, out Carrier axis, out long start, out long end)
    {
        axis = null!; start = end = 0;
        if (!Point(span.Left, out var a) || !Point(span.Right, out var b) || a.Carrier.IsUnknown ||
            !a.Carrier.SameIdentityAs(b.Carrier) || a.Ticks >= b.Ticks) return false;
        axis = a.Carrier; start = a.Ticks; end = b.Ticks; return true;
    }

    public static bool TrySelect(Relation choice, Relation selectorRead, out IElement source,
        out Relation calibration, out Relation witness)
    {
        source = new Placeholder(); calibration = null!;
        witness = ReadWitness.Create(new Carrier(), choice, choice, source, selectorRead, choice);
        if (!TryRead(choice, out _, out var scope, out var offers, out _) ||
            !TrySpan(scope, out var axis, out var lower, out var upper) ||
            !Equation.TryReadPrediction(selectorRead, out var sample, out _) ||
            !axis.SameIdentityAs(sample.Carrier) || sample.Ticks < lower || sample.Ticks >= upper || sample.Ticks == long.MaxValue) return false;
        var occupied = Relation.Segment(axis, sample.Ticks, sample.Ticks + 1);
        var matches = new List<Relation>();
        foreach (var offer in offers)
        {
            if (!TryReadOffer(offer, out var band, out _, out _) ||
                !TrySpan(band, out var carrier, out var a, out var b) || !axis.SameIdentityAs(carrier) || a < lower || b > upper) return false;
            // The existing support reader establishes occupied coverage, rather
            // than inferring truth from a bare point or coincident boundary.
            var passed = RelationReads.CoveredSpansWithinOn(axis, occupied, band);
            if (passed.Length == 1 && TrySpan(passed[0], out _, out var p, out var q) && p == sample.Ticks && q == sample.Ticks + 1)
                matches.Add(offer);
        }
        var evidence = Relation.OrderedGroup(new Carrier(), selectorRead, occupied,
            Relation.OrderedGroup(new Carrier(), matches.Cast<IElement>().ToArray()));
        if (matches.Count != 1)
        {
            witness = ReadWitness.Create(new Carrier(), choice, scope, source, evidence, choice); return false;
        }
        TryReadOffer(matches[0], out _, out source, out calibration);
        witness = ReadWitness.Create(new Carrier(), choice, scope, matches[0], selectorRead.Right, evidence);
        return true;
    }

    /// <summary>Observe only the selected source. Unchosen open/undefined sources do not become operands.</summary>
    public static bool TryObserve(Relation choice, Func<IElement, Relation> read, out Relation prediction)
    {
        prediction = Relation.UnresolvedRead(choice, choice, Carrier.Unknown);
        if (!TryRead(choice, out var selector, out _, out _, out var output)) return false;
        var selectorRead = read(selector);
        if (!TrySelect(choice, selectorRead, out var source, out var calibration, out var selection))
        { prediction = new Relation(new Placeholder(), selection); return false; }
        var selectedRead = read(source);
        if (!Equation.TryReadPrediction(selectedRead, out var value, out _) || value.Carrier.IsUnknown ||
            !Equation.TryPredict(calibration, value, output, out var result) ||
            !Equation.TryReadPrediction(result, out var projected, out _))
        { prediction = new Relation(new Placeholder(), Relation.OrderedGroup(new Carrier(), selection, selectedRead)); return false; }
        prediction = new Relation(projected, Relation.OrderedGroup(new Carrier(), selection, selectedRead.Right, result.Right));
        return true;
    }

    private static bool Point(IElement element, out CarrierLocation point)
    {
        if (element is CarrierLocation location) { point = location; return true; }
        if (element is Relation { Left: CarrierLocation site, Right: Carrier carrier } && site.Carrier.SameIdentityAs(carrier))
        { point = site; return true; }
        point = default; return false;
    }
}
