using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>A selected unmeasured affine interpretation of finite control
/// participation. Controls, shared parameter and interpolation dependencies are
/// native structure. Integer vectors and shape names below are derived reads;
/// no metric, angle, radius or rendered control point is a kernel primitive.</summary>
public static class QualitativeCurve
{
    public readonly record struct Vector(BigInteger X, BigInteger Y)
    {
        public bool Zero => X.IsZero && Y.IsZero;
        public static Vector operator -(Vector a, Vector b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector operator +(Vector a, Vector b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector operator -(Vector a) => new(-a.X, -a.Y);
        public static BigInteger Cross(Vector a, Vector b) => a.X * b.Y - a.Y * b.X;
        public static BigInteger Dot(Vector a, Vector b) => a.X * b.X + a.Y * b.Y;
    }

    public sealed class Frame
    {
        // frame = motion basis -> shared uncalibrated reach
        // section = (from site -> to site) -> (Closed(control words) -> scheme)
        // interval = (start marker -> end marker) ->
        //            (parameter -> Closed(start -> parameter, parameter -> end))
        // scheme = (frame -> interval) -> (Closed(interpolation operations) -> output)
        // Each interpolation's output is the next level's actual input. All
        // operations use the same parameter. Holder carriers inside the shared
        // TransformSurface port grammar remain unmeasured structural roots.
        public QualitativeMotionFrame Motion { get; }
        public IElement Reach { get; }
        public Relation Surface { get; }

        public Frame(QualitativeMotionFrame motion, IElement? reach = null)
        { Motion = motion; Reach = reach ?? new Placeholder(); Surface = new(motion.Surface, Reach); }

        public Relation Word(int x, int y)
        {
            if ((BigInteger.Abs(x) + BigInteger.Abs(y)) > 65536)
                throw new ArgumentOutOfRangeException(nameof(x), "A fixture word is bounded to 65536 native participations.");
            return S(Enumerable.Repeat(0, (int)BigInteger.Abs(x)).Select(_ => (IElement)QualitativeMotionFrame.Move(x < 0 ? Motion.Left : Motion.Right, Reach))
                .Concat(Enumerable.Repeat(0, (int)BigInteger.Abs(y)).Select(_ => (IElement)QualitativeMotionFrame.Move(y < 0 ? Motion.Down : Motion.Up, Reach))).ToArray());
        }

        public bool Vector(Relation word, out Vector vector)
        {
            vector = default;
            if (!Entries(word, out var entries)) return false;
            var x = BigInteger.Zero; var y = BigInteger.Zero;
            foreach (var item in entries)
            {
                if (item is not Relation move || !Motion.Direction(move, out var axis, out var forward, out var reach) || !Same(reach, Reach)) return false;
                if (ReferenceEquals(axis, Motion.Horizontal)) x += forward ? 1 : -1;
                else if (ReferenceEquals(axis, Motion.Vertical)) y += forward ? 1 : -1;
                else return false;
            }
            vector = new(x, y); return true;
        }

        public Relation Line(IElement from, IElement to, Relation startWord, Relation endWord) => Build(from, to, startWord, endWord);
        public Relation Quadratic(IElement from, IElement to, Relation startWord, Relation controlWord, Relation endWord) => Build(from, to, startWord, controlWord, endWord);
        public Relation Cubic(IElement from, IElement to, Relation startWord, Relation control1Word, Relation control2Word, Relation endWord) => Build(from, to, startWord, control1Word, control2Word, endWord);

        private Relation Build(IElement from, IElement to, params Relation[] words)
        {
            var parameter = new Placeholder(); var operations = new List<IElement>();
            IElement[] level = words.Cast<IElement>().ToArray();
            while (level.Length > 1)
            {
                var next = new IElement[level.Length - 1];
                for (var i = 0; i < next.Length; i++)
                {
                    next[i] = new Placeholder();
                    operations.Add(TransformSurface.Interpolate(level[i], level[i + 1], parameter, next[i]));
                }
                level = next;
            }
            // These before relations declare a selected convex interpolation
            // contract. They do not authorize polynomial extrapolation beyond
            // the two parameter markers.
            var start = new Placeholder(); var end = new Placeholder();
            var interval = new Relation(new Relation(start, end), new Relation(parameter,
                S(new Relation(start, parameter), new Relation(parameter, end))));
            var scheme = new Relation(new Relation(Surface, interval), new Relation(S(operations.ToArray()), level[0]));
            return new(new Relation(from, to), new Relation(S(words.Cast<IElement>().ToArray()), scheme));
        }

        public Read Read(Relation section)
        {
            var open = StructuralConstraint.Open();
            if (section is not { Left: Relation endpoints, Right: Relation { Left: Relation words, Right: Relation scheme } } ||
                !Entries(words, out var controls) || controls.Length is < 2 or > 4 || controls.Any(item => item is not Relation) ||
                !Scheme(scheme, controls)) return Incomplete(section, section.Left, section.Right, open);
            var vectors = new Vector[controls.Length];
            for (var i = 0; i < controls.Length; i++)
                if (!Vector((Relation)controls[i], out vectors[i])) return Incomplete(section, endpoints.Left, endpoints.Right, words);
            var differences = vectors.Zip(vectors.Skip(1), (a, b) => b - a).ToArray();
            var start = differences.FirstOrDefault(v => !v.Zero);
            var end = differences.LastOrDefault(v => !v.Zero);
            var startWord = DifferenceWord((Relation)controls[0], FirstOther(controls, vectors));
            var endWord = DifferenceWord(LastOther(controls, vectors), (Relation)controls[^1]);
            var nonzero = differences.Where(v => !v.Zero).ToArray();
            bool? straight = nonzero.Length == 0 ? null : nonzero.All(v => QualitativeCurve.Vector.Cross(nonzero[0], v).IsZero);
            var coefficients = differences.Length switch
            {
                1 => Array.Empty<BigInteger>(),
                2 => [QualitativeCurve.Vector.Cross(differences[0], differences[1])],
                _ => new[] { QualitativeCurve.Vector.Cross(differences[0], differences[1]),
                    QualitativeCurve.Vector.Cross(differences[0], differences[2]), QualitativeCurve.Vector.Cross(differences[1], differences[2]) }
            };
            var signs = coefficients.Select(value => value.Sign).ToArray();
            var positive = signs.Any(s => s > 0); var negative = signs.Any(s => s < 0);
            int? bend = positive && !negative ? 1 : negative && !positive ? -1 : straight == true ? 0 : null;
            var regular = RegularCertificate(differences, coefficients);
            var inflected = regular && coefficients.Length == 3 && coefficients[0].Sign * coefficients[2].Sign < 0;
            var tangentEvidence = new Relation(new Relation(endpoints.Left, startWord), new Relation(endpoints.Right, endWord));
            var nativeDifferences = S(Enumerable.Range(0, controls.Length - 1)
                .Select(i => (IElement)new Relation(new Relation(controls[i], controls[i + 1]), DifferenceWord((Relation)controls[i], (Relation)controls[i + 1]))).ToArray());
            var pending = new List<IElement>();
            if (bend is null) pending.Add(new Relation(nativeDifferences, StructuralConstraint.Open()));
            if (!regular) pending.Add(new Relation(scheme, StructuralConstraint.Open()));
            var witness = new Relation(new Relation(Surface, section), new Relation(tangentEvidence,
                new Relation(nativeDifferences, S(pending.ToArray()))));
            return new(this, section, endpoints.Left, endpoints.Right, words, vectors, startWord, endWord,
                start, end, true, straight, bend, inflected, regular, witness);
        }

        private Read Incomplete(Relation section, IElement start, IElement end, Relation words) =>
            new(this, section, start, end, words, [], StructuralConstraint.Open(), StructuralConstraint.Open(),
                default, default, false, null, null, false, false,
                new Relation(new Relation(Surface, section), StructuralConstraint.Open(section)));

        private bool Scheme(Relation scheme, IElement[] controls)
        {
            if (scheme is not { Left: Relation { Left: var frame, Right: Relation interval },
                Right: Relation { Left: Relation operations, Right: var output } } || !Same(frame, Surface) ||
                !Interval(interval, out var parameter) || !Entries(operations, out var rows)) return false;
            var at = 0; var level = controls;
            var outputs = new HashSet<Placeholder>();
            while (level.Length > 1)
            {
                var next = new IElement[level.Length - 1];
                for (var i = 0; i < next.Length; i++)
                {
                    if (at >= rows.Length || rows[at++] is not Relation operation ||
                        !TransformSurface.TryReadInterpolate(operation, out var a, out var b, out var t, out var result) ||
                        !Same(a, level[i]) || !Same(b, level[i + 1]) || !Same(t, parameter) ||
                        result is not Placeholder { IsInverse: false } slot || Same(slot, parameter) || !outputs.Add(slot)) return false;
                    next[i] = result;
                }
                level = next;
            }
            return at == rows.Length && Same(output, level[0]);
        }

        private static bool Interval(Relation interval, out Placeholder parameter)
        {
            parameter = null!;
            if (interval is not { Left: Relation { Left: Placeholder { IsInverse: false } start, Right: Placeholder { IsInverse: false } end },
                Right: Relation { Left: Placeholder { IsInverse: false } at, Right: Relation order } } ||
                Same(start, end) || Same(start, at) || Same(end, at) || !Entries(order, out var pairs) ||
                pairs is not [Relation first, Relation second] || !Same(first.Left, start) || !Same(first.Right, at) ||
                !Same(second.Left, at) || !Same(second.Right, end)) return false;
            parameter = at; return true;
        }

        private Relation DifferenceWord(Relation from, Relation to)
        {
            if (!Entries(from, out var a) || !Entries(to, out var b)) return StructuralConstraint.Open(from, to);
            var inverse = new List<IElement>();
            foreach (var item in a)
            {
                if (item is not Relation move || !Motion.Direction(move, out var axis, out var forward, out var reach)) return StructuralConstraint.Open(from, to);
                inverse.Add(QualitativeMotionFrame.Move(ReferenceEquals(axis, Motion.Horizontal)
                    ? forward ? Motion.Left : Motion.Right : forward ? Motion.Down : Motion.Up, reach));
            }
            var witness = Motion.Reduce(S(inverse.ToArray()), S(b));
            return Motion.TryReadResult(witness, out var remaining, out _, out _) ? remaining : StructuralConstraint.Open(from, to);
        }

        public Relation Reverse(Relation section)
        {
            var read = Read(section); if (!read.Complete) return new(section, StructuralConstraint.Open());
            return Build(read.End, read.Start, E(read.ControlWords).Cast<Relation>().Reverse().ToArray());
        }

        /// <summary>Reflects through the horizontal axis by default; false
        /// reflects through the vertical axis. Sites remain distinct unless
        /// already shared in the selected section.</summary>
        public Relation Reflect(Relation section, bool horizontal = true)
        {
            var read = Read(section); if (!read.Complete) return new(section, StructuralConstraint.Open());
            Relation Map(Relation word) => S(E(word).Cast<Relation>().Select(move =>
            {
                Motion.Direction(move, out var axis, out var forward, out var reach);
                if (ReferenceEquals(axis, horizontal ? Motion.Vertical : Motion.Horizontal))
                    return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis, Motion.Horizontal)
                        ? forward ? Motion.Left : Motion.Right : forward ? Motion.Down : Motion.Up, reach);
                return move;
            }).ToArray());
            return Build(read.Start, read.End, E(read.ControlWords).Cast<Relation>().Select(Map).ToArray());
        }

        private static Relation FirstOther(IElement[] controls, Vector[] vectors)
        {
            var index = Array.FindIndex(vectors, value => value != vectors[0]);
            return (Relation)controls[index >= 0 ? index : 0];
        }

        private static Relation LastOther(IElement[] controls, Vector[] vectors)
        {
            var index = Array.FindLastIndex(vectors, value => value != vectors[^1]);
            return (Relation)controls[index >= 0 ? index : vectors.Length - 1];
        }
    }

    public sealed record Read(Frame Frame, Relation Section, IElement Start, IElement End,
        Relation ControlWords, Vector[] Controls, Relation StartTangent, Relation EndTangent,
        Vector StartVector, Vector EndVector, bool Complete, bool? Straight,
        int? BendSign, bool Inflected, bool Regular, Relation Surface);
    public sealed record JoinRead(bool Connected, bool? Smooth, bool? Cusp, bool? Corner, Relation Surface);

    public static JoinRead Join(Read incoming, Read outgoing)
    {
        var connected = Same(incoming.End, outgoing.Start);
        var compatible = Same(incoming.Frame.Surface, outgoing.Frame.Surface);
        var witness = new Relation(new Relation(incoming.Surface, outgoing.Surface),
            new Relation(new Relation(incoming.End, outgoing.Start), new Relation(incoming.EndTangent, outgoing.StartTangent)));
        if (!connected || !compatible || !incoming.Complete || !outgoing.Complete ||
            incoming.Controls[^1] != outgoing.Controls[0] || incoming.EndVector.Zero || outgoing.StartVector.Zero)
            return new(connected, null, null, null, new Relation(witness, StructuralConstraint.Open()));
        var cross = Vector.Cross(incoming.EndVector, outgoing.StartVector); var dot = Vector.Dot(incoming.EndVector, outgoing.StartVector);
        return new(true, cross.IsZero && dot.Sign > 0, cross.IsZero && dot.Sign < 0, !cross.IsZero, witness);
    }

    /// <summary>Interior side is an explicit oriented-boundary premise: +1
    /// selects left, -1 right. Closure alone supplies no such premise.</summary>
    public static int? Convexity(Read read, int? interiorSide)
    {
        if (!read.Complete || !read.Regular || interiorSide is not (1 or -1) || read.BendSign is null || read.Inflected) return null;
        return read.BendSign * interiorSide;
    }

    private static bool RegularCertificate(Vector[] differences, BigInteger[] curvature)
    {
        if (differences[0].Zero || differences[^1].Zero) return false;
        // A strictly signed projection of every derivative control keeps the
        // derivative out of zero. This is sufficient, not exhaustive.
        var directions = differences.Concat([new Vector(1, 0), new Vector(-1, 0), new Vector(0, 1), new Vector(0, -1),
            differences.Aggregate(default(Vector), (sum, value) => sum + value)]);
        if (directions.Any(direction => !direction.Zero && differences.All(value => Vector.Dot(direction, value).Sign > 0))) return true;
        // A uniformly signed curvature numerator with a nonzero endpoint
        // coefficient is strictly signed on the open interval, precluding an
        // interior zero derivative. Endpoints were checked separately.
        return curvature.Length != 0 && (curvature[0].Sign != 0 || curvature[^1].Sign != 0) &&
            (curvature.All(value => value.Sign >= 0) || curvature.All(value => value.Sign <= 0));
    }

    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static bool Entries(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => Entries(scope, out var entries) ? entries : [];
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
