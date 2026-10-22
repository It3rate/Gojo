using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>Calculated finite reads over a curve's actual interpolation structure.
/// Similarity, sample resolution and the operands remain connected premises.
/// No measured length, exact-circle convention or global curve maximum is added.</summary>
public static class CurveShapeQualities
{
    private const int ParticipationCapacity = 4096;

    public sealed record Fraction(BigInteger Numerator, BigInteger Denominator, Relation Surface)
    {
        public override string ToString() => $"{Numerator}/{Denominator}";
    }

    public sealed record Heading(Fraction? HorizontalShare, bool? Diagonal, Relation Surface);
    public sealed record Sample(int Numerator, int Denominator, Fraction? SignedExcursion, Relation Surface);
    public sealed record QualityRead(Heading StartHeading, Heading EndHeading, IReadOnlyList<Sample> Samples,
        Fraction? Strength, bool? Retracing, Relation Surface);
    public sealed record JoinQuality(bool Connected, bool? Smooth, Fraction? IncomingSquaredCurvature,
        Fraction? OutgoingSquaredCurvature, bool? EqualCurvature, bool? OpposedBending, Relation Surface);

    /// <summary>Selects equal reaches and a right-angle quarter-turn cycle in
    /// this frame. This is an authored similarity premise, not evidence that an
    /// arbitrary affine frame has a Euclidean metric. Its four concrete unit
    /// words and their correspondence carry the selection without a mode tag.</summary>
    public static Relation SimilarityBasis(Curve.Frame frame)
    {
        var right = frame.Word(1, 0); var up = frame.Word(0, 1);
        var left = frame.Word(-1, 0); var down = frame.Word(0, -1);
        return new(new Relation(frame.Surface, new Relation(right, up)),
            new Relation(Closed(new Relation(right, up), new Relation(up, left),
                new Relation(left, down), new Relation(down, right)), new Relation(frame.Reach, frame.Reach)));
    }

    public static QualityRead Read(Curve.Read source, Relation? basis = null)
    {
        // Do not trust a caller-edited diagnostic record instead of its source.
        var read = source.Frame.Read(source.Section);
        var selected = basis is not null && Basis(read.Frame, basis);
        var premise = basis ?? StructuralConstraint.Open(read.Frame.Surface);
        var context = new Relation(read.Surface, premise);
        var start = ReadHeading(read.StartVector, read.StartTangent, context, selected && read.Complete);
        var end = ReadHeading(read.EndVector, read.EndTangent, context, selected && read.Complete);
        var samples = new List<Sample>();
        if (read.Complete)
        {
            var controls = Entries(read.ControlWords); var degree = controls.Length - 1;
            var scheme = (Relation)((Relation)read.Section.Right).Right;
            var parameter = ((Relation)((Relation)((Relation)scheme.Left).Right).Right).Left;
            var chord = read.Controls[^1] - read.Controls[0];
            var squaredChord = Curve.Vector.Dot(chord, chord);
            for (var part = 1; part <= 3; part++)
            {
                var coefficients = Weights(degree, part, 4);
                var denominator = BigInteger.Pow(4, degree);
                var pointNumerator = Sum(read.Controls, coefficients);
                var participation = Closed(coefficients.SelectMany((coefficient, index) =>
                    Enumerable.Repeat(controls[index], (int)coefficient)).ToArray());
                var partition = Ratio(part, 4, new Relation(parameter, scheme));
                var sampleSource = new Relation(new Relation(context, new Relation(parameter, partition!.Surface)),
                    new Relation(participation, Repeat(read.Frame.Reach, denominator)));
                var cross = Curve.Vector.Cross(chord, pointNumerator - Scale(read.Controls[0], denominator));
                var excursion = selected && !squaredChord.IsZero
                    ? Ratio(cross, denominator * squaredChord, sampleSource) : null;
                samples.Add(new(part, 4, excursion,
                    new Relation(sampleSource, excursion?.Surface ?? StructuralConstraint.Open(sampleSource))));
            }
        }
        var strongest = samples.Where(sample => sample.SignedExcursion is not null)
            .Select(sample => sample.SignedExcursion!).OrderByDescending(value => value, AbsoluteComparer.Instance).FirstOrDefault();
        var strength = strongest is not null && samples.All(sample => sample.SignedExcursion is not null)
            ? Ratio(BigInteger.Abs(strongest.Numerator), strongest.Denominator,
                new Relation(context, Closed(samples.Select(sample => (IElement)sample.Surface).ToArray()))) : null;
        var retracing = Retracing(read, out var progress);
        var surface = new Relation(context, new Relation(new Relation(start.Surface, end.Surface),
            new Relation(Closed(samples.Select(sample => (IElement)sample.Surface).ToArray()),
                new Relation(strength?.Surface ?? StructuralConstraint.Open(context), progress))));
        return new(start, end, samples, strength, retracing, surface);
    }

    /// <summary>At a forward tangent join, compares endpoint curvature
    /// magnitudes without taking roots: kappa^2 = cross(v,a)^2 / dot(v,v)^3.
    /// Equal curvature is local second-order agreement, not whole-curve mirror
    /// symmetry. Zero endpoint derivatives remain open even when a tangent
    /// limit exists.</summary>
    public static JoinQuality Join(Curve.Read incomingSource, Curve.Read outgoingSource, Relation? basis = null)
    {
        var incoming = incomingSource.Frame.Read(incomingSource.Section);
        var outgoing = outgoingSource.Frame.Read(outgoingSource.Section);
        var joint = Curve.Join(incoming, outgoing);
        var context = new Relation(joint.Surface, basis ?? StructuralConstraint.Open(incoming.Frame.Surface));
        Fraction? a = null; Fraction? b = null; bool? equal = null; bool? opposite = null;
        if (joint.Smooth == true && basis is not null && Basis(incoming.Frame, basis) &&
            Same(incoming.Frame.Surface, outgoing.Frame.Surface) &&
            Endpoint(incoming, false, out var firstVelocity, out var firstAcceleration) &&
            Endpoint(outgoing, true, out var secondVelocity, out var secondAcceleration))
        {
            var firstCross = Curve.Vector.Cross(firstVelocity, firstAcceleration);
            var secondCross = Curve.Vector.Cross(secondVelocity, secondAcceleration);
            a = Ratio(firstCross * firstCross, BigInteger.Pow(Curve.Vector.Dot(firstVelocity, firstVelocity), 3),
                new Relation(context, incoming.ControlWords));
            b = Ratio(secondCross * secondCross, BigInteger.Pow(Curve.Vector.Dot(secondVelocity, secondVelocity), 3),
                new Relation(context, outgoing.ControlWords));
            opposite = firstCross.Sign * secondCross.Sign < 0;
            if (a is not null && b is not null)
                equal = a.Numerator * b.Denominator == b.Numerator * a.Denominator;
        }
        var operands = new Relation(a?.Surface ?? StructuralConstraint.Open(incoming.Section),
            b?.Surface ?? StructuralConstraint.Open(outgoing.Section));
        return new(joint.Connected, joint.Smooth, a, b, equal, opposite, new Relation(context, operands));
    }

    private static Heading ReadHeading(Curve.Vector vector, Relation word, Relation context, bool selected)
    {
        var denominator = BigInteger.Abs(vector.X) + BigInteger.Abs(vector.Y);
        var source = new Relation(context, word);
        var share = selected && !denominator.IsZero ? Ratio(BigInteger.Abs(vector.X), denominator, source) : null;
        return new(share, share is null ? null : BigInteger.Abs(vector.X) == BigInteger.Abs(vector.Y),
            new Relation(source, share?.Surface ?? StructuralConstraint.Open(word)));
    }

    private static bool Endpoint(Curve.Read read, bool start, out Curve.Vector velocity, out Curve.Vector acceleration)
    {
        velocity = default; acceleration = default;
        if (!read.Complete) return false;
        var c = read.Controls; var degree = c.Length - 1;
        velocity = Scale(start ? c[1] - c[0] : c[^1] - c[^2], degree);
        if (velocity.Zero) return false;
        if (degree > 1)
            acceleration = Scale(start ? c[2] - Scale(c[1], 2) + c[0] : c[^1] - Scale(c[^2], 2) + c[^3], degree * (degree - 1));
        return true;
    }

    private static bool? Retracing(Curve.Read read, out Relation surface)
    {
        var context = new Relation(read.Surface, read.ControlWords);
        surface = new(context, StructuralConstraint.Open(context));
        if (!read.Complete || read.Straight != true) return null;
        var differences = read.Controls.Zip(read.Controls.Skip(1), (a, b) => b - a).ToArray();
        var direction = differences.FirstOrDefault(value => !value.Zero);
        if (direction.Zero) return null;
        // All controls are collinear. One nonzero coordinate is enough for
        // affine scalar order; no equal-axis or Euclidean premise is needed.
        var values = differences.Select(value => direction.X.IsZero ? value.Y : value.X).ToArray();
        context = new Relation(context, direction.X.IsZero ? read.Frame.Motion.Vertical : read.Frame.Motion.Horizontal);
        var witnesses = new List<IElement>(); var resolved = true;
        var controls = Entries(read.ControlWords);
        var scheme = (Relation)((Relation)read.Section.Right).Right;
        var parameter = ((Relation)((Relation)((Relation)scheme.Left).Right).Right).Left;
        var signs = new List<int>();
        for (var part = 0; part <= 4; part++)
        {
            var weights = Weights(values.Length - 1, part, 4);
            var value = values.Select((v, i) => v * weights[i]).Aggregate(BigInteger.Zero, (a, b) => a + b);
            signs.Add(value.Sign);
            var nativeWeights = Closed(weights.SelectMany((weight, index) =>
                Enumerable.Repeat((IElement)new Relation(controls[index], controls[index + 1]), (int)weight)).ToArray());
            var result = Ratio(value, BigInteger.Pow(4, values.Length - 1), new Relation(context, nativeWeights));
            resolved &= result is not null;
            var partition = Ratio(part, 4, new Relation(parameter, scheme));
            witnesses.Add(new Relation(partition!.Surface, result?.Surface ?? StructuralConstraint.Open(nativeWeights)));
        }
        bool? retraces = !resolved ? null : signs.Any(sign => sign > 0) && signs.Any(sign => sign < 0) ? true :
            values.All(value => value.Sign >= 0) || values.All(value => value.Sign <= 0) ? false : null;
        // The signed sampled derivative values are the certificate. Merely
        // seeing opposing derivative controls would be insufficient.
        surface = new(context, new Relation(Closed(witnesses.ToArray()), retraces is null
            ? StructuralConstraint.Open(context) : Closed(read.Section)));
        return retraces;
    }

    public static bool IsSimilarityBasis(Curve.Frame frame, Relation basis) => Basis(frame, basis);

    private static bool Basis(Curve.Frame frame, Relation basis)
    {
        if (basis is not { Left: Relation { Left: var selectedFrame, Right: Relation { Left: Relation right, Right: Relation up } },
            Right: Relation { Left: Relation cycle, Right: Relation equality } } || !Same(selectedFrame, frame.Surface) ||
            !Same(equality.Left, frame.Reach) || !Same(equality.Right, frame.Reach) ||
            !frame.Vector(right, out var r) || r != new Curve.Vector(1, 0) ||
            !frame.Vector(up, out var u) || u != new Curve.Vector(0, 1)) return false;
        var rows = Entries(cycle);
        if (rows is not [Relation first, Relation second, Relation third, Relation fourth] ||
            !Same(first.Left, right) || !Same(first.Right, up) || !Same(second.Left, up) ||
            !Same(second.Right, third.Left) || !Same(third.Right, fourth.Left) || !Same(fourth.Right, right) ||
            second.Right is not Relation left || third.Right is not Relation down) return false;
        return frame.Vector(left, out var l) && l == new Curve.Vector(-1, 0) &&
            frame.Vector(down, out var d) && d == new Curve.Vector(0, -1);
    }

    private static Fraction? Ratio(BigInteger numerator, BigInteger denominator, IElement operands)
    {
        if (denominator.Sign <= 0) return null;
        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        numerator /= divisor; denominator /= divisor;
        if (BigInteger.Abs(numerator) + denominator > ParticipationCapacity) return null;
        // A ratio is paired signed participation over one explicit partition,
        // anchored to its operands. The fresh unit is a local comparison role,
        // not a measured carrier or a new physical length promise.
        var unit = new Placeholder();
        var positive = Repeat(unit, BigInteger.Max(numerator, BigInteger.Zero));
        var negative = Repeat(unit, BigInteger.Max(-numerator, BigInteger.Zero));
        var surface = new Relation(new Relation(operands, unit), new Relation(new Relation(positive, negative), Repeat(unit, denominator)));
        return new(numerator, denominator, surface);
    }

    private static BigInteger[] Weights(int degree, int part, int whole)
    {
        var result = new BigInteger[degree + 1];
        for (var i = 0; i <= degree; i++)
        {
            var binomial = degree == 3 && i is 1 or 2 ? 3 : degree == 2 && i == 1 ? 2 : 1;
            result[i] = binomial * BigInteger.Pow(whole - part, degree - i) * BigInteger.Pow(part, i);
        }
        return result;
    }

    private static Curve.Vector Scale(Curve.Vector value, BigInteger factor) => new(value.X * factor, value.Y * factor);
    private static Curve.Vector Sum(Curve.Vector[] values, BigInteger[] weights) => values.Select((v, i) => Scale(v, weights[i]))
        .Aggregate(default(Curve.Vector), (a, b) => a + b);
    private static Relation Repeat(IElement item, BigInteger count) => count >= 0 && count <= ParticipationCapacity
        ? Closed(Enumerable.Repeat(item, (int)count).ToArray()) : StructuralConstraint.Open(item);
    private static Relation Closed(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var items, out var seam, out var tail) && seam.Equals(tail) ? items : [];
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);

    private sealed class AbsoluteComparer : IComparer<Fraction>
    {
        public static AbsoluteComparer Instance { get; } = new();
        public int Compare(Fraction? a, Fraction? b) => a is null ? b is null ? 0 : -1 : b is null ? 1 :
            (BigInteger.Abs(a.Numerator) * b.Denominator).CompareTo(BigInteger.Abs(b.Numerator) * a.Denominator);
    }
}
