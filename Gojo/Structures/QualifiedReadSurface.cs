using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Structures;

/// <summary>
/// A scoped numeric read whose display basis, variation, interpreted coverage,
/// excluded participation and source history remain connected Gojo structure.
/// No denominator is interpreted as confidence without a supporting relation.
/// </summary>
public static class QualifiedReadSurface
{
    /// <summary>Creates an observation. Variation is an explicitly supplied
    /// interval on the value carrier, or unresolved structure. Coverage is a
    /// separate read of the selected source, never an implicit value weight.</summary>
    public static Relation Create(Carrier root, IElement selected, Relation basis,
        IElement value, IElement variation, IElement coverage, IElement scope,
        IElement excluded, IElement provenance) =>
        ReadWitness.Create(root, selected, new Relation(basis, scope), value,
            Group(variation, coverage, excluded), provenance);

    public static bool TryRead(Relation read, out IElement selected, out Relation basis,
        out IElement value, out IElement variation, out IElement coverage,
        out IElement scope, out IElement excluded, out IElement provenance) =>
        TryReadObservation(ReadCurrent(read, new HashSet<Relation>(ReferenceEqualityComparer.Instance)),
            out selected, out basis, out value, out variation, out coverage, out scope, out excluded, out provenance);

    /// <summary>Reads an already published observation only. Does not execute
    /// a derived read, follow its sources, or elaborate its variation.</summary>
    public static bool TryReadPublished(Relation read, out IElement selected, out Relation basis,
        out IElement value, out IElement variation, out IElement coverage,
        out IElement scope, out IElement excluded, out IElement provenance) =>
        TryReadObservation(read, out selected, out basis, out value, out variation,
            out coverage, out scope, out excluded, out provenance);

    /// <summary>Consumes a materialized nominal service value in its exposed
    /// local basis. A pending supplier calculation remains pending.</summary>
    public static bool TryReadPublishedValue(Relation read, out CarrierLocation value)
    {
        if (TryReadPublished(read, out _, out var basis, out var result, out _, out _, out _, out _, out _) &&
            result is CarrierLocation location && location.Carrier is not null && location.Tick is not null &&
            TryFrame(basis, out var start, out var end) &&
            !start.Carrier.IsUnknown && start.Ticks != end.Ticks && location.Carrier.SameIdentityAs(start.Carrier))
        { value = location; return true; }
        value = default; return false;
    }

    // Only an observation has a value slot. Derived reads retain a connected
    // equation and inputs; their current observation is produced on demand.
    private static bool TryReadObservation(Relation read, out IElement selected, out Relation basis,
        out IElement value, out IElement variation, out IElement coverage,
        out IElement scope, out IElement excluded, out IElement provenance)
    {
        if (ReadWitness.TryRead(read, out selected, out var surface, out value, out var residue, out provenance) &&
            surface is Relation frame && frame.Left is Relation candidate &&
            TryFrame(candidate, out _, out _) && residue is Relation qualifications &&
            qualifications.Right is Carrier holder && qualifications.TryReadOrderedElementsOn(holder, out var parts) && parts.Length == 3)
        {
            basis = candidate; scope = frame.Right;
            variation = parts[0]; coverage = parts[1]; excluded = parts[2];
            return true;
        }
        selected = value = variation = coverage = scope = excluded = provenance = null!;
        basis = null!;
        return false;
    }

    private static Relation ReadCurrent(Relation read, HashSet<Relation> active)
    {
        if (!active.Add(read)) return Pending(new Carrier(), read, read);
        try
        {
            if (TryReadObservation(read, out _, out _, out _, out _, out _, out _, out _, out _)) return read;
            if (!ReadWitness.TryRead(read, out var selected, out var surface, out _, out _, out _) ||
                surface is not Relation equation || read.Right is not Carrier root) return read;
            if (selected is Relation source && TryFrame(equation.Left as Relation, out _, out _) &&
                TryFrame(equation.Right as Relation, out _, out _))
                return ReframeCurrent(root, source, ReadCurrent(source, active), equation);
            if (selected is Relation inputs && inputs.Right is Carrier holder &&
                inputs.TryReadOrderedElementsOn(holder, out var parts) && parts.Length == 2 &&
                parts[0] is Relation left && parts[1] is Relation right &&
                TryPoint(equation.Right, out var output) && !output.Carrier.IsUnknown && output.Ticks == 1)
                return CombineCurrent(root, left, right, ReadCurrent(left, active), ReadCurrent(right, active), equation, output.Carrier);
            return read;
        }
        finally { active.Remove(read); }
    }

    /// <summary>Reads the nominal amount only when its local basis exists.
    /// This does not claim the attached coverage, scope or variation is settled.</summary>
    public static bool TryReadValue(Relation read, out CarrierLocation value)
    {
        if (TryRead(read, out _, out var basis, out var result, out _, out _, out _, out _, out _) &&
            result is CarrierLocation location && TryFrame(basis, out var start, out var end) &&
            !start.Carrier.IsUnknown && start.Ticks != end.Ticks && location.Carrier.SameIdentityAs(start.Carrier))
        { value = location; return true; }
        value = default; return false;
    }

    /// <summary>Changes the read basis, preserving the entire earlier read.
    /// The supplied alignment is explicit, including any opposed-face mapping.
    /// Enlarging the display basis does not create acquisition evidence.</summary>
    public static Relation Reframe(Carrier root, Relation source, Relation alignment)
        => Pending(root, source, alignment);

    private static Relation ReframeCurrent(Carrier root, Relation source, Relation observation, Relation alignment)
    {
        if (!TryReadObservation(observation, out _, out var basis, out _, out var variation, out var coverage,
                out var scope, out var excluded, out _) || alignment.Right is not Relation target ||
            !TryFrame(target, out var targetStart, out var targetEnd) || !alignment.Left.Equals(basis) ||
            !TryReadValue(observation, out var value) || targetStart.Carrier.IsUnknown || targetStart.Ticks == targetEnd.Ticks)
            return Pending(root, source, alignment);

        if (!alignment.TryProject(value, targetStart.Carrier, out var projected))
            return Pending(root, source, alignment);
        IElement nextVariation = new Placeholder();
        var proof = new List<IElement> { alignment, projected };
        if (TryBounds(variation, value.Carrier, out var low, out var high))
        {
            // Both endpoint roles are retained; only this interval read finds
            // minima/maxima. Outward rounding keeps coarse bounds conservative.
            var endpoints = new List<CarrierLocation>();
            foreach (var endpoint in new[] { low, high })
            {
                endpoints.Add(ProjectBound(alignment, basis, target, endpoint, lower: true, proof));
                endpoints.Add(ProjectBound(alignment, basis, target, endpoint, lower: false, proof));
            }
            nextVariation = Envelope(endpoints, proof);
        }
        return Create(root, source, target, projected.Left, nextVariation, coverage, scope,
            Group(excluded, projected.Right), Group(proof.ToArray()));
    }

    /// <summary>Connects an additive calculation. Alignment is required when
    /// read, not when the structure is built. No answer is stored here.</summary>
    public static Relation Add(Carrier root, Relation left, Relation right)
    {
        var axis = new Carrier();
        return Pending(root, Group(left, right), Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(axis)));
    }

    /// <summary>Reads left minus right in their shared local basis.</summary>
    public static Relation Subtract(Carrier root, Relation left, Relation right)
    {
        var axis = new Carrier();
        return Pending(root, Group(left, right), Equation.Sum(axis,
            Equation.UnitPort(axis), Equation.WeightedPort(axis, 0, 1, 0, -1)));
    }

    // Deliberately restricted to these affine operations. Arbitrary nonlinear
    // functions cannot be bounded just by evaluating interval endpoints.
    private static Relation CombineCurrent(Carrier root, Relation left, Relation right,
        Relation leftObservation, Relation rightObservation, Relation equation, Carrier calculationAxis)
    {
        if (!TryReadObservation(leftObservation, out _, out var leftBasis, out _, out var leftVariation, out var leftCoverage,
                out var leftScope, out var leftExcluded, out _) ||
            !TryReadObservation(rightObservation, out _, out var rightBasis, out _, out var rightVariation, out var rightCoverage,
                out var rightScope, out var rightExcluded, out _) ||
            !leftBasis.Equals(rightBasis) || !TryReadValue(leftObservation, out var a) || !TryReadValue(rightObservation, out var b))
            return Pending(root, Group(left, right), equation);

        TryFrame(leftBasis, out var start, out _);
        var proof = new List<IElement> { equation };
        Relation PredictAt(CarrierLocation x, CarrierLocation y)
        {
            // Normalize against the CURRENT shared origin, run the retained
            // equation, then rejoin that origin. The equation carries polarity.
            var normalize = Equation.Difference(start.Carrier, start.Carrier, calculationAxis);
            var nx = Equation.Predict(normalize, new Relation(start, x), calculationAxis);
            var ny = Equation.Predict(normalize, new Relation(start, y), calculationAxis);
            var calculated = Equation.Predict(equation, new Relation(nx.Left, ny.Left), calculationAxis);
            var rejoin = Equation.Sum(start.Carrier, Equation.UnitPort(calculationAxis), Equation.UnitPort(start.Carrier));
            var answer = Equation.Predict(rejoin, new Relation(calculated.Left, start), start.Carrier);
            proof.Add(Group(normalize, nx, ny, calculated, rejoin, answer));
            return answer;
        }
        var prediction = PredictAt(a, b);
        if (!Equation.TryReadPrediction(prediction, out var result, out var remainder))
            return Pending(root, Group(left, right), equation);

        IElement variation = new Placeholder();
        if (TryBounds(leftVariation, a.Carrier, out var aLow, out var aHigh) &&
            TryBounds(rightVariation, b.Carrier, out var bLow, out var bHigh))
        {
            var endpoints = new List<CarrierLocation>();
            foreach (var x in new[] { aLow, aHigh })
            foreach (var y in new[] { bLow, bHigh })
            {
                // A reused observation is one variable. Do not invent
                // independent alternatives for x - x or x + x.
                if (ReferenceEquals(left, right) && x != y) continue;
                var candidate = PredictAt(x, y);
                if (Equation.TryReadPrediction(candidate, out var endpoint, out _)) endpoints.Add(endpoint);
            }
            variation = Envelope(endpoints, proof);
        }
        return Create(root, Group(left, right), leftBasis, result, variation,
            ReferenceEquals(leftCoverage, rightCoverage) ? leftCoverage : Group(leftCoverage, rightCoverage),
            ReferenceEquals(leftScope, rightScope) ? leftScope : Group(leftScope, rightScope),
            Group(leftExcluded, rightExcluded, remainder), Group(proof.ToArray()));
    }

    /// <summary>Compares interval separation while retaining both reads. A
    /// positive result means every left alternative exceeds every right one
    /// on the carrier's increasing coordinate direction. The returned interval
    /// distinguishes overlap from a wholly reversed ordering.</summary>
    public static Relation Separation(Carrier root, Relation left, Relation right)
    {
        var difference = Subtract(new Carrier(), left, right);
        return Pending(root, Group(left, right), difference);
    }

    /// <summary>Calculates the current interval separation. The returned value
    /// is a transient observation; reread the circuit after inputs change.</summary>
    public static bool TryReadSeparation(Relation separation, out CarrierLocation value, out Relation witness)
    {
        value = default;
        witness = separation;
        if (!ReadWitness.TryRead(separation, out var selected, out var surface, out _, out _, out _) ||
            surface is not Relation difference) return false;
        if (!TryRead(difference, out _, out var basis, out _, out var variation, out _, out _, out _, out _) ||
            !TryFrame(basis, out var frame, out _) || !TryBounds(variation, frame.Carrier, out var lower, out var upper))
            return false;
        var displacement = Equation.Predict(Equation.Difference(frame.Carrier, frame.Carrier, frame.Carrier),
            new Relation(frame, lower), frame.Carrier);
        if (!Equation.TryReadPrediction(displacement, out value, out _)) return false;
        witness = ReadWitness.Create(new Carrier(), selected, difference, displacement.Left,
            new Relation(lower, upper), Group(difference, displacement));
        return true;
    }

    private static Relation Pending(Carrier root, IElement source, IElement attempted) =>
        ReadWitness.Create(root, source, attempted, new Placeholder(), new Relation(source, attempted), attempted);

    private static CarrierLocation ProjectBound(Relation alignment, Relation basis, Relation target,
        CarrierLocation endpoint, bool lower, List<IElement> proof)
    {
        TryFrame(basis, out var s0, out var s1);
        TryFrame(target, out var t0, out var t1);
        alignment.TryProject(endpoint, t0.Carrier, out var projected);
        proof.Add(projected);
        var value = projected.LeftLocation;
        if (projected.Right is not CarrierLocation residue || residue.Ticks == 0) return value;
        var direction = Math.Sign(residue.Ticks) * Math.Sign(s1.Ticks - s0.Ticks) * Math.Sign(t1.Ticks - t0.Ticks);
        if ((lower && direction < 0) || (!lower && direction > 0))
        {
            var step = Equation.Predict(Equation.Sum(value.Carrier, Equation.UnitPort(value.Carrier), Equation.UnitPort(value.Carrier)),
                new Relation(value, new CarrierLocation(value.Carrier, direction)), value.Carrier);
            proof.Add(step);
            Equation.TryReadPrediction(step, out value, out _);
        }
        return value;
    }

    private static Relation Envelope(IReadOnlyList<CarrierLocation> endpoints, List<IElement> proof)
    {
        var entries = endpoints.Select(point => ExtremaTournament.Entry(point, point)).ToArray();
        var minimum = ExtremaTournament.Minimum(new Carrier(), entries, out _);
        var maximum = ExtremaTournament.Maximum(new Carrier(), entries, out _);
        ExtremaTournament.TryReadWinner(minimum, out var minEntry);
        ExtremaTournament.TryReadWinner(maximum, out var maxEntry);
        ExtremaTournament.TryReadEntry(minEntry, out _, out var low);
        ExtremaTournament.TryReadEntry(maxEntry, out _, out var high);
        proof.Add(minimum); proof.Add(maximum);
        return new Relation(low, high);
    }

    public static bool TryReadBounds(Relation read, out CarrierLocation low, out CarrierLocation high)
    {
        if (TryRead(read, out _, out var basis, out _, out var variation, out _, out _, out _, out _) &&
            TryFrame(basis, out var start, out var end) && !start.Carrier.IsUnknown && start.Ticks != end.Ticks &&
            TryBounds(variation, start.Carrier, out low, out high)) return true;
        low = high = default; return false;
    }

    private static bool TryBounds(IElement element, Carrier axis, out CarrierLocation low, out CarrierLocation high)
    {
        if (element is Relation interval && TryFrame(interval, out low, out high) &&
            low.Carrier.SameIdentityAs(axis) && low.Ticks <= high.Ticks) return true;
        low = high = default; return false;
    }

    private static bool TryFrame(Relation? relation, out CarrierLocation start, out CarrierLocation end)
    {
        if (relation is not null && TryPoint(relation.Left, out start) && TryPoint(relation.Right, out end) &&
            start.Carrier is not null && end.Carrier is not null && start.Carrier.SameIdentityAs(end.Carrier)) return true;
        start = end = default; return false;
    }
    private static bool TryPoint(IElement element, out CarrierLocation location)
    {
        if (element is CarrierLocation direct) { location = direct; return true; }
        if (element is Relation point && point.Left is CarrierLocation p && point.Right is Carrier c && c.SameIdentityAs(p.Carrier))
        { location = p; return true; }
        location = default; return false;
    }
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
}
