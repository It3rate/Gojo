using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// An explicitly selected numeric quotient read. Operands, output basis and
/// standard are connected structure; unknown or undefined reads retain an open
/// result and their original participation. This helper never settles carriers.
/// </summary>
public static class QuantityQuotient
{
    /// <summary>Connects numerator sample, denominator sample, output carrier and standard.</summary>
    public static Relation Create(Carrier root, IElement numerator, IElement denominator,
        Carrier output, IElement standard) =>
        Relation.OrderedGroup(root, numerator, denominator, output, standard);

    /// <summary>Reads the four fields without discovering operands in nested lookalikes.</summary>
    public static bool TryRead(Relation request, out IElement numerator, out IElement denominator,
        out Carrier output, out IElement standard)
    {
        if (request.TryReadOrderedGroup(out var fields) && fields.Length == 4 &&
            fields[2] is Carrier target && !target.IsUnknown)
        {
            numerator = fields[0];
            denominator = fields[1];
            output = target;
            standard = fields[3];
            return true;
        }

        numerator = denominator = standard = null!;
        output = null!;
        return false;
    }

    /// <summary>
    /// Observes n/d in the exact native quotient unit using the existing affine
    /// projection law. q truncates toward zero and n = q*d + r in numerator
    /// ticks. A false read of a valid request still produces an unresolved
    /// witness; zero and unknown denominators never become numeric answers.
    /// </summary>
    public static bool TryObserve(Relation request, out Relation witness)
    {
        if (!TryRead(request, out var numerator, out var denominator, out var output, out var standard))
        {
            witness = null!;
            return false;
        }

        IElement evidence = standard;
        if (numerator is CarrierLocation n && denominator is CarrierLocation d)
        {
            var expression = UnitQuotient.Create(new Carrier(),
                Relation.OrderedGroup(new Carrier(), Relation.Point(n.Carrier, 1)),
                Relation.OrderedGroup(new Carrier(), Relation.Point(d.Carrier, 1)), standard);
            evidence = expression;
            if (d.Ticks != 0 && UnitQuotient.TryReadUnit(expression, out var unit) && output.UnitShape.Equals(unit))
            {
                // The source span counts numerator ticks per output tick. Its
                // extent comes from the retained denominator sample, not from
                // equating the numerator and denominator carrier identities.
                var projection = new Relation(Relation.Segment(n.Carrier, 0, d.Ticks),
                    Relation.Segment(output, 0, 1));
                if (projection.TryProject(n, output, out var read) &&
                    read.TryReadProjection(out var result, out var residue))
                {
                    witness = ReadWitness.Create(new Carrier(), request,
                        new Relation(denominator, projection), result, residue, evidence);
                    return true;
                }
            }
        }

        witness = ReadWitness.Create(new Carrier(), request, request,
            new Placeholder(), new Relation(numerator, denominator), evidence);
        return false;
    }
}
