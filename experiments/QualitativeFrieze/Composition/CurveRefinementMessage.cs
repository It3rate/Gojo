using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Shapes = Gojo.Experiments.QualitativeExecution.CurveShapeQualities;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A detached, finite-resolution profile publication. Native ratio
/// bands and actual trace examples carry the information. Source quality
/// witnesses, handles and interpolation ancestry stay in the evaluator.</summary>
public static class CurveRefinementMessage
{
    // packet = (base message -> (similarity -> circular-like reference)) ->
    //          (Closed(span reports) -> Closed(join reports))
    // span report = role -> ((entrance band -> exit band) ->
    //                       (Closed(parameter -> excursion band) -> progress))
    // join report = (incoming role -> outgoing role) -> (balance band -> opposed)
    // band = (lower ratio -> upper ratio) -> representative ratio
    // ratio = signed horizontal participation word -> positive partition word
    public readonly record struct Ratio(BigInteger N, BigInteger D)
    {
        public static Ratio Of(BigInteger n, BigInteger d)
        { if (d.IsZero) throw new ArgumentException("A ratio needs a nonzero partition."); if (d.Sign < 0) { n = -n; d = -d; } var g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(n), d); return new(n / g, d / g); }
        public static Ratio operator +(Ratio a, Ratio b) => Of(a.N * b.D + b.N * a.D, a.D * b.D);
        public static Ratio operator -(Ratio a, Ratio b) => Of(a.N * b.D - b.N * a.D, a.D * b.D);
        public static Ratio operator /(Ratio a, int d) => Of(a.N, a.D * d);
        public int CompareTo(Ratio b) => (N * b.D).CompareTo(b.N * D);
        public override string ToString() => $"{N}/{D}";
    }
    public sealed record SpanReport(Scenes.Span Span, Relation Entrance, Relation Exit, IReadOnlyList<Relation> Samples,
        Relation Progress, Relation Surface);
    public sealed record JoinReport(Scenes.Join Join, Relation Balance, Relation Opposed, Relation Surface);
    public sealed record Packet(Scenes.Message Base, Relation Similarity, Relation Reference, Relation HalfReference,
        IReadOnlyList<SpanReport> Spans, IReadOnlyList<JoinReport> Joins, Relation Surface);
    public sealed record Check(string Name, Relation Supported, Relation Contradicted, Relation Open, Relation Surface)
    { public int Matches => E(Supported).Length; public int Conflicts => E(Contradicted).Length; public int Unresolved => E(Open).Length; }
    public sealed record Assessment(IReadOnlyList<Check> Checks, Relation Surface)
    { public int Matches => Checks.Sum(row => row.Matches); public int Conflicts => Checks.Sum(row => row.Conflicts); public int Unresolved => Checks.Sum(row => row.Unresolved); }

    public static Packet Publish(CurveRefinementCorpus.Case source)
    {
        var message = Scenes.Publish(source.Source, source.Tangents, source.Bends, source.Traces, source.Interior);
        var f = message.Frame;
        var similarity = source.Similarity ? Shapes.SimilarityBasis(f) : StructuralConstraint.Open(f.Surface);
        var sourceBasis = source.Similarity ? Shapes.SimilarityBasis(source.Source.Frame) : null;
        // A rational 5/9-handle quarter-turn example. Zero of a selected
        // fullness comparison, not an exact circle or universal curvature.
        var reference = f.Cubic(new Placeholder(), new Placeholder(), f.Word(0, 0), f.Word(0, 5), f.Word(4, 9), f.Word(9, 9));
        var halfReference = f.Cubic(new Placeholder(), new Placeholder(), f.Word(0, -3), f.Word(4, -3), f.Word(4, 3), f.Word(0, 3));
        var spans = source.Source.Reads.Select((read, i) =>
        {
            var quality = Shapes.Read(read, sourceBasis); var span = message.Spans[i];
            var entrance = source.Profiles && source.Tangents ? HeadingBand(quality.StartHeading.HorizontalShare, f) : StructuralConstraint.Open(span.StartHeading);
            var exit = source.Profiles && source.Tangents ? HeadingBand(quality.EndHeading.HorizontalShare, f) : StructuralConstraint.Open(span.EndHeading);
            var samples = Enumerable.Range(0, 3).Select(j => source.Profiles && source.Similarity && quality.Samples.Count == 3 ?
                ExcursionBand(quality.Samples[j].SignedExcursion, f) : StructuralConstraint.Open(span.Role)).ToArray();
            var progress = source.Profiles ? Fact(quality.Retracing, span.Role) : StructuralConstraint.Open(span.Role);
            var reports = S(samples.Select((band, j) => (IElement)new Relation(NativeRatio(f, Ratio.Of(j + 1, 4)), band)).ToArray());
            var surface = new Relation(span.Role, new Relation(new Relation(entrance, exit), new Relation(reports, progress)));
            return new SpanReport(span, entrance, exit, samples, progress, surface);
        }).ToArray();
        var originals = source.Source.Reads;
        var joins = message.Joins.Select(join =>
        {
            var i = Array.IndexOf(message.Spans.ToArray(), join.Incoming); var j = Array.IndexOf(message.Spans.ToArray(), join.Outgoing);
            var quality = Shapes.Join(originals[i], originals[j], sourceBasis);
            var balance = source.Profiles && source.Similarity ? BalanceBand(quality, f) : StructuralConstraint.Open(join.Surface);
            var opposed = source.Profiles ? Fact(quality.OpposedBending, new Relation(join.Incoming.Role, join.Outgoing.Role)) : StructuralConstraint.Open(join.Surface);
            return new JoinReport(join, balance, opposed, new Relation(new Relation(join.Incoming.Role, join.Outgoing.Role), new Relation(balance, opposed)));
        }).ToArray();
        var root = new Relation(new Relation(message.Surface, new Relation(similarity, new Relation(reference, halfReference))),
            new Relation(S(spans.Select(row => (IElement)row.Surface).ToArray()), S(joins.Select(row => (IElement)row.Surface).ToArray())));
        return new(message, similarity, reference, halfReference, spans, joins, root);
    }

    public static Assessment Assess(Packet packet, Scenes.Scene candidate, bool profiles = true)
    {
        var old = CurveEvidenceExperiment.Assess(packet.Base, candidate);
        var checks = old.Groups.Select(group => new Check(group.Name, group.Supported, group.Contradicted, group.Open, group.Surface)).ToList();
        if (!profiles) return new(checks, S(checks.Select(row => (IElement)row.Surface).ToArray()));
        var direction = new Lane("Directional balance, including equal-axis diagonals");
        var force = new Lane("Signed three-part curve profile"); var progress = new Lane("Passage progress or retracing");
        var symmetry = new Lane("Local tangent curvature balance");
        var reads = candidate.Reads;
        Curve.Read? Match(Scenes.Span span) => MatchRead(packet.Base, reads, span, candidate.Frame);
        foreach (var report in packet.Spans)
        {
            var read = Match(report.Span);
            if (read is null)
            {
                foreach (var target in new[] { direction, force, progress }) target.Add(null, new Relation(report.Surface, StructuralConstraint.Open(candidate.Surface)));
                continue;
            }
            var quality = Shapes.Read(read, packet.Similarity);
            direction.Add(InBand(report.Entrance, quality.StartHeading.HorizontalShare, packet.Base.Frame), new Relation(report.Entrance, read.StartTangent));
            direction.Add(InBand(report.Exit, quality.EndHeading.HorizontalShare, packet.Base.Frame), new Relation(report.Exit, read.EndTangent));
            for (var j = 0; j < 3; j++)
                force.Add(InBand(report.Samples[j], quality.Samples.Count == 3 ? quality.Samples[j].SignedExcursion : null, packet.Base.Frame),
                    new Relation(new Relation(report.Surface, NativeRatio(packet.Base.Frame, Ratio.Of(j + 1, 4))), read.Section));
            progress.Add(CompareFact(report.Progress, quality.Retracing), new Relation(report.Progress, read.Section));
        }
        foreach (var report in packet.Joins)
        {
            var incoming = Match(report.Join.Incoming); var outgoing = Match(report.Join.Outgoing);
            var quality = incoming is null || outgoing is null ? null : Shapes.Join(incoming, outgoing, packet.Similarity);
            symmetry.Add(InBand(report.Balance, Balance(quality), packet.Base.Frame), new Relation(report.Surface,
                new Relation(incoming?.Section ?? candidate.Surface, outgoing?.Section ?? candidate.Surface)));
            symmetry.Add(CompareFact(report.Opposed, quality?.OpposedBending), new Relation(report.Opposed,
                new Relation(incoming?.Section ?? candidate.Surface, outgoing?.Section ?? candidate.Surface)));
        }
        checks.AddRange(new[] { direction, force, progress, symmetry }.Select(lane => lane.Read(packet.Surface, candidate.Surface)));
        return new(checks, S(checks.Select(row => (IElement)row.Surface).ToArray()));
    }

    public static Curve.Read? MatchRead(Scenes.Message message, IReadOnlyList<Curve.Read> reads, Scenes.Span span, Curve.Frame frame)
    {
        if (!ReferenceEquals(message.Frame.Surface, frame.Surface) || !frame.Vector(span.Start, out var start) || !frame.Vector(span.End, out var end)) return null;
        var matches = reads.Where(read => read.Complete && FieldNative.Same(read.Start, span.From) && FieldNative.Same(read.End, span.To) &&
            read.Controls[0] == start && read.Controls[^1] == end).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    public static Scenes.Scene Propose(Packet packet, Ratio first, Ratio second, bool adaptive = false, bool straight = false, bool fold = false)
    {
        var f = packet.Base.Frame; var sections = new List<Relation>();
        foreach (var report in packet.Spans)
        {
            var span = report.Span;
            if (straight || Scenes.TraceStraight(packet.Base, span) == true && !fold)
            { sections.Add(f.Line(span.From, span.To, span.Start, span.End)); continue; }
            if (!adaptive) { sections.Add(Section(packet, report, first, second, fold)); continue; }
            var fit = FitSection(packet, report);
            var choices = Fractions.SelectMany(a => Fractions.Select(b => Section(packet, report, a, b, false)));
            if (fit is not null) choices = new[] { fit }.Concat(choices);
            var alternatives = choices
                .Select((section, i) => (section, i, Cost: LocalCost(packet, report, section))).OrderBy(row => row.Cost).ThenBy(row => row.i).ToArray();
            sections.Add(alternatives[0].section);
        }
        return new(f, sections, interior: packet.Base.Interior);
    }

    public static readonly Ratio[] Fractions = [Ratio.Of(1, 4), Ratio.Of(1, 3), Ratio.Of(1, 2), Ratio.Of(5, 9), Ratio.Of(2, 3), Ratio.Of(3, 4), Ratio.Of(1, 1)];

    private static Relation? FitSection(Packet packet, SpanReport report)
    {
        var f = packet.Base.Frame; var span = report.Span;
        if (!TryBand(report.Samples[0], f, out _, out _, out var s1) || !TryBand(report.Samples[2], f, out _, out _, out var s3)) return null;
        f.Vector(span.Start, out var a); f.Vector(span.End, out var b); var chord = b - a;
        var d0 = Heading(report.Entrance, span.StartHeading, f, chord); var d1 = Heading(report.Exit, span.EndHeading, f, chord);
        var cross0 = Curve.Vector.Cross(chord, d0); var cross1 = Curve.Vector.Cross(chord, d1); var square = Curve.Vector.Dot(chord, chord);
        if (cross0.IsZero || cross1.IsZero || square.IsZero) return null;
        // Solve the two finite cubic excursion equations at 1/4 and 3/4.
        // Their inputs are public band representatives, not source samples.
        var q0 = Ratio.Of((24 * s1.N * s3.D - 8 * s3.N * s1.D) * square, 9 * s1.D * s3.D * cross0);
        var q1 = Ratio.Of((8 * s1.N * s3.D - 24 * s3.N * s1.D) * square, 9 * s1.D * s3.D * cross1);
        if (q0.N <= 0 || q1.N <= 0) return null;
        var c1 = a + new Curve.Vector(Round(d0.X * q0.N, q0.D), Round(d0.Y * q0.N, q0.D));
        var c2 = b - new Curve.Vector(Round(d1.X * q1.N, q1.D), Round(d1.Y * q1.N, q1.D));
        if (new[] { c1, c2 }.Any(v => BigInteger.Abs(v.X) + BigInteger.Abs(v.Y) > 65536)) return null;
        return f.Cubic(span.From, span.To, span.Start, f.Word((int)c1.X, (int)c1.Y), f.Word((int)c2.X, (int)c2.Y), span.End);
    }
    private static Relation Section(Packet packet, SpanReport report, Ratio first, Ratio second, bool fold)
    {
        var f = packet.Base.Frame; var span = report.Span;
        f.Vector(span.Start, out var start); f.Vector(span.End, out var end);
        var chord = end - start;
        var a = Heading(report.Entrance, span.StartHeading, f, chord);
        var b = Heading(report.Exit, span.EndHeading, f, chord);
        var scale = BigInteger.Max(BigInteger.Abs(chord.X), BigInteger.Abs(chord.Y));
        Curve.Vector Handle(Curve.Vector direction, Ratio ratio)
        {
            var max = BigInteger.Max(BigInteger.Abs(direction.X), BigInteger.Abs(direction.Y));
            return max.IsZero ? default : new(Round(direction.X * scale * ratio.N, max * ratio.D), Round(direction.Y * scale * ratio.N, max * ratio.D));
        }
        var c1 = start + Handle(a, first); var c2 = end - Handle(b, second);
        if (fold && Scenes.TraceStraight(packet.Base, span) == true)
        { c1 = start + Handle(a, Ratio.Of(2, 1)); c2 = end - Handle(b, Ratio.Of(2, 1)); }
        else if (Scenes.TraceStraight(packet.Base, span) == false && !Closed(span.StartHeading) && !Closed(span.EndHeading))
        {
            var normal = new Curve.Vector(-chord.Y.Sign, chord.X.Sign);
            int SideAt(int i) => TryBand(report.Samples[i], f, out _, out _, out var sample) && sample.N.Sign != 0 ? sample.N.Sign :
                -(Scenes.Side(f, span.Bends) ?? -1);
            var sign0 = SideAt(0); var sign1 = SideAt(2);
            c1 += Handle(new Curve.Vector(sign0 * normal.X, sign0 * normal.Y), first);
            c2 += Handle(new Curve.Vector(sign1 * normal.X, sign1 * normal.Y), second);
        }
        return f.Cubic(span.From, span.To, span.Start, f.Word((int)c1.X, (int)c1.Y), f.Word((int)c2.X, (int)c2.Y), span.End);
    }

    private static int LocalCost(Packet packet, SpanReport report, Relation section)
    {
        var read = packet.Base.Frame.Read(section); var q = Shapes.Read(read, packet.Similarity); var cost = 0;
        void Add(bool? result) { cost += result == false ? 8 : result is null ? 1 : 0; }
        Add(Scenes.TraceStraight(packet.Base, report.Span) is bool expected && read.Straight is bool actual ? expected == actual : null);
        Add(InBand(report.Entrance, q.StartHeading.HorizontalShare, packet.Base.Frame)); Add(InBand(report.Exit, q.EndHeading.HorizontalShare, packet.Base.Frame));
        for (var j = 0; j < 3; j++) Add(InBand(report.Samples[j], q.Samples.Count == 3 ? q.Samples[j].SignedExcursion : null, packet.Base.Frame));
        Add(CompareFact(report.Progress, q.Retracing));
        if (Closed(report.Span.Bends) && E(report.Span.Bends).Length == 2) Add(read.Inflected);
        return cost;
    }

    private static Curve.Vector Heading(Relation band, Relation sectors, Curve.Frame frame, Curve.Vector chord)
    {
        var sign = Scenes.Direction(sectors, frame);
        if (sign.Zero) return new(chord.X.Sign, chord.Y.Sign);
        if (!TryBand(band, frame, out _, out _, out var share)) return sign;
        return new(sign.X * share.N, sign.Y * (share.D - share.N));
    }

    public static Relation NativeRatio(Curve.Frame f, Ratio ratio)
    {
        var reduced = Ratio.Of(ratio.N, ratio.D);
        if (BigInteger.Abs(reduced.N) + reduced.D > 2048) return StructuralConstraint.Open(f.Surface);
        return new(f.Word((int)reduced.N, 0), f.Word((int)reduced.D, 0));
    }
    public static bool TryRatio(Relation surface, Curve.Frame f, out Ratio ratio)
    {
        ratio = default;
        if (surface.Left is not Relation n || surface.Right is not Relation d || !f.Vector(n, out var numerator) ||
            !f.Vector(d, out var denominator) || numerator.Y != 0 || denominator.Y != 0 || denominator.X <= 0) return false;
        ratio = Ratio.Of(numerator.X, denominator.X); return true;
    }
    public static bool TryBand(Relation surface, Curve.Frame f, out Ratio lower, out Ratio upper, out Ratio representative)
    {
        lower = upper = representative = default;
        return surface.Left is Relation limits && limits.Left is Relation a && limits.Right is Relation b && surface.Right is Relation at &&
            TryRatio(a, f, out lower) && TryRatio(b, f, out upper) && TryRatio(at, f, out representative) &&
            lower.CompareTo(upper) <= 0 && lower.CompareTo(representative) <= 0 && upper.CompareTo(representative) >= 0;
    }
    public static bool? InBand(Relation band, Shapes.Fraction? value, Curve.Frame f) => value is null ? null : InBand(band, Ratio.Of(value.Numerator, value.Denominator), f);
    public static bool? InBand(Relation band, Ratio? value, Curve.Frame f) => value is null || !TryBand(band, f, out var lower, out var upper, out _) ? null :
        lower.CompareTo(value.Value) <= 0 && upper.CompareTo(value.Value) >= 0;
    public static string DescribeBand(Relation band, Curve.Frame f) => TryBand(band, f, out var a, out var b, out var r) ? $"{a} … {b} (sample {r})" : "Open";
    private static Relation Band(Curve.Frame f, Ratio low, Ratio high, Ratio at) => new(new Relation(NativeRatio(f, low), NativeRatio(f, high)), NativeRatio(f, at));
    private static Relation HeadingBand(Shapes.Fraction? value, Curve.Frame f)
    {
        if (value is null) return StructuralConstraint.Open(f.Surface);
        Ratio[] examples = [Ratio.Of(0, 1), Ratio.Of(1, 4), Ratio.Of(1, 3), Ratio.Of(2, 5), Ratio.Of(1, 2), Ratio.Of(3, 5), Ratio.Of(2, 3), Ratio.Of(3, 4), Ratio.Of(1, 1)];
        var v = Ratio.Of(value.Numerator, value.Denominator);
        var index = Enumerable.Range(0, examples.Length).OrderBy(i => Abs(examples[i] - v), RationalComparer.Instance).First();
        return Band(f, index == 0 ? examples[0] : (examples[index - 1] + examples[index]) / 2,
            index == examples.Length - 1 ? examples[^1] : (examples[index] + examples[index + 1]) / 2, examples[index]);
    }
    private static Relation ExcursionBand(Shapes.Fraction? value, Curve.Frame f)
    {
        if (value is null) return StructuralConstraint.Open(f.Surface);
        var centre = Round(value.Numerator * 24, value.Denominator);
        return Band(f, Ratio.Of(centre * 2 - 1, 48), Ratio.Of(centre * 2 + 1, 48), Ratio.Of(centre, 24));
    }
    private static Ratio? Balance(Shapes.JoinQuality? q)
    {
        if (q?.IncomingSquaredCurvature is not { } a || q.OutgoingSquaredCurvature is not { } b) return null;
        var first = a.Numerator * b.Denominator; var second = b.Numerator * a.Denominator;
        return first + second == 0 ? Ratio.Of(1, 2) : Ratio.Of(first, first + second);
    }
    private static Relation BalanceBand(Shapes.JoinQuality q, Curve.Frame f)
    {
        var balance = Balance(q);
        if (balance is null) return StructuralConstraint.Open(f.Surface);
        var centre = Round(balance.Value.N * 4, balance.Value.D);
        return Band(f, Ratio.Of(BigInteger.Max(0, centre * 2 - 1), 8), Ratio.Of(BigInteger.Min(8, centre * 2 + 1), 8), Ratio.Of(centre, 4));
    }
    public static Relation Fact(bool? value, IElement scope) => value is null ? StructuralConstraint.Open(scope) : value.Value ? S(scope) : S();
    public static bool? ReadFact(Relation fact) => !Closed(fact) ? null : E(fact).Length != 0;
    private static bool? CompareFact(Relation expected, bool? actual) => ReadFact(expected) is bool known && actual is bool response ? known == response : null;
    private static BigInteger Round(BigInteger n, BigInteger d) => n.Sign * ((BigInteger.Abs(n) * 2 + d) / (2 * d));
    private static Ratio Abs(Ratio x) => Ratio.Of(BigInteger.Abs(x.N), x.D);
    private sealed class RationalComparer : IComparer<Ratio>
    { public static RationalComparer Instance { get; } = new(); public int Compare(Ratio x, Ratio y) => x.CompareTo(y); }
    private sealed class Lane(string name)
    {
        private readonly List<IElement> yes = [], no = [], open = [];
        public void Add(bool? result, IElement witness) => (result == true ? yes : result == false ? no : open).Add(witness);
        public Check Read(IElement source, IElement candidate) => new(name, S(yes.ToArray()), S(no.ToArray()), S(open.ToArray()),
            new Relation(new Relation(source, candidate), new Relation(S(yes.ToArray()), new Relation(S(no.ToArray()), S(open.ToArray())))));
    }
}
