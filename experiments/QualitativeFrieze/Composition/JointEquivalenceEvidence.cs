using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Publishes equivalence without publishing the measurements which
/// established it. A shared extent binding is a comparison promise, not stroke
/// identity or endpoint connectivity. All source arithmetic remains audit-only.</summary>
public static class JointEquivalenceEvidence
{
    // stream = (joint roles -> ray slots) ->
    //   ((exact extent groups -> exact turn groups) ->
    //    (coarse turn hypotheses -> (unresolved extents -> unresolved turns)))
    // group = common unmeasured promise -> Closed(member -> promise)
    // extent member = ray slot; turn member = (local ray slot -> local ray slot)
    // A group therefore binds observations to one shared comparison role. It
    // never binds the two endpoint occurrences of the same source passage.
    public sealed class Read
    {
        public JointShapeDescription.Message Message { get; }
        public Relation ExactLengths { get; }
        public Relation ExactAngles { get; }
        public Relation SimilarAngles { get; }
        public Relation UnresolvedLengths { get; }
        public Relation UnresolvedAngles { get; }
        public Relation Open => new(UnresolvedLengths, UnresolvedAngles);
        public Relation Surface { get; }
        public Relation Audit { get; }
        internal Read(JointShapeDescription.Message message, Relation lengths, Relation angles, Relation similar,
            Relation openLengths, Relation openAngles, Relation audit, Relation? actual = null)
        {
            Message = message; ExactLengths = lengths; ExactAngles = angles; SimilarAngles = similar;
            UnresolvedLengths = openLengths; UnresolvedAngles = openAngles; Audit = audit;
            var slots = S(message.JointReads.SelectMany(joint => E(joint.Rays).Cast<Relation>().Select(ray => ray.Left)).ToArray());
            Surface = actual ?? new(new Relation(message.Roles, slots), new Relation(new Relation(lengths, angles),
                new Relation(similar, new Relation(openLengths, openAngles))));
        }
    }

    public sealed record Claim(string Kind, string State, Relation Witness);
    public sealed class Assessment
    {
        public IReadOnlyList<Claim> Claims { get; }
        public Relation Supported { get; }
        public Relation Contradicted { get; }
        public Relation Open { get; }
        public Relation Surface { get; }
        internal Assessment(Read stream, JointShapeReconstruction.Candidate candidate, IReadOnlyList<Claim> claims)
        {
            Claims = claims;
            Supported = S(claims.Where(claim => claim.State == "Supported").Select(claim => (IElement)claim.Witness).ToArray());
            Contradicted = S(claims.Where(claim => claim.State == "Contradicted").Select(claim => (IElement)claim.Witness).ToArray());
            Open = S(claims.Where(claim => claim.State == "Open").Select(claim => (IElement)claim.Witness).ToArray());
            Surface = new(new Relation(stream.Surface, candidate.Surface), new Relation(Supported, new Relation(Contradicted, Open)));
        }
    }

    private sealed record Ray(IElement Slot, IElement Role, IElement Stroke, Relation Word);
    private sealed record Angle(IElement Role, Ray First, Ray Second)
    { public Relation Member => new(First.Slot, Second.Slot); }
    private sealed record Product(IElement Axis, IElement Reach, Relation Surface);
    private sealed record Norm(Relation Word, IReadOnlyList<Product> Products, Relation Witness, bool Known);
    private sealed record Comparison(string State, Relation Witness);

    public static Read Analyze(JointShapeDescription.Analysis analysis)
    {
        var message = analysis.Message; var frame = analysis.Source.Graph.Frame;
        var metric = message.Basis.ReadRightAngle(S()).Answer == "Yes";
        var rays = SourceRays(analysis); var audits = new List<IElement>();
        // One endpoint occurrence per actual source stroke. Publishing both
        // would reveal its partner even though the intended cue is only size.
        var representatives = rays.GroupBy(ray => ray.Stroke, FieldNative.Participants.Instance).Select(group => group.First()).ToArray();
        var norms = representatives.ToDictionary(ray => ray.Slot, ray => Measure(frame, ray.Word, metric), FieldNative.Participants.Instance);
        var lengthGroups = new List<List<Ray>>(); var openLengths = new List<IElement>();
        foreach (var ray in representatives)
        {
            var measure = norms[ray.Slot];
            if (!measure.Known) { openLengths.Add(ray.Slot); continue; }
            var group = lengthGroups.FirstOrDefault(group => CompareNorms(measure, norms[group[0].Slot]).State == "Supported");
            if (group is null) lengthGroups.Add([ray]); else group.Add(ray);
        }
        var lengths = S(lengthGroups.Where(group => group.Count >= 2).Select(group =>
        {
            var clause = Group(group.Select(ray => ray.Slot));
            audits.Add(new Relation(clause, S(group.Skip(1).Select(ray => (IElement)
                CompareNorms(norms[group[0].Slot], norms[ray.Slot]).Witness).ToArray())));
            return (IElement)clause;
        }).ToArray());
        var angles = rays.GroupBy(ray => ray.Role, FieldNative.Participants.Instance).SelectMany(group =>
        {
            var local = group.ToArray(); var result = new List<Angle>();
            for (var a = 0; a < local.Length; a++) for (var b = a + 1; b < local.Length; b++)
                result.Add(new(group.Key, local[a], local[b]));
            return result;
        }).ToArray();
        var exactAngleGroups = new List<List<Angle>>(); var coarseGroups = new List<List<Angle>>(); var openAngles = new List<IElement>();
        foreach (var angle in angles)
        {
            if (!KnownRay(frame, angle.First.Word) || !KnownRay(frame, angle.Second.Word)) { openAngles.Add(angle.Member); continue; }
            var exact = exactAngleGroups.FirstOrDefault(group => !group.Any(prior => ReferenceEquals(prior.Role, angle.Role)) &&
                CompareAngles(frame, group[0].First.Word, group[0].Second.Word, angle.First.Word, angle.Second.Word, metric).State == "Supported");
            if (exact is null) exactAngleGroups.Add([angle]); else exact.Add(angle);
            var coarse = coarseGroups.FirstOrDefault(group => !group.Any(prior => ReferenceEquals(prior.Role, angle.Role)) &&
                CoarseAngle(frame, group[0].First.Word, group[0].Second.Word, angle.First.Word, angle.Second.Word, metric));
            if (coarse is null) coarseGroups.Add([angle]); else coarse.Add(angle);
        }
        var exactAngles = S(exactAngleGroups.Where(group => group.Count >= 2).Select(group =>
        {
            var clause = Group(group.Select(angle => (IElement)angle.Member));
            audits.Add(new Relation(clause, S(group.Skip(1).Select(angle => (IElement)CompareAngles(frame,
                group[0].First.Word, group[0].Second.Word, angle.First.Word, angle.Second.Word, metric).Witness).ToArray())));
            return (IElement)clause;
        }).ToArray());
        // Only publish a coarse group when it contributes a comparison not
        // already supplied by one exact group. Its separate lane is revisable.
        var similar = S(coarseGroups.Where(group => group.Count >= 2 && group.Skip(1).Any(angle =>
            CompareAngles(frame, group[0].First.Word, group[0].Second.Word, angle.First.Word, angle.Second.Word, metric).State != "Supported"))
            .Select(group => (IElement)Group(group.Select(angle => (IElement)angle.Member))).ToArray());
        var audit = new Relation(analysis.Audit, S(audits.ToArray()));
        return new(message, lengths, exactAngles, similar, StructuralConstraint.Open(openLengths.ToArray()),
            StructuralConstraint.Open(openAngles.ToArray()), audit);
    }

    /// <summary>Reads this exact supplementary contract in the same transported
    /// alias world as its joint message. It never searches analysis ancestry.</summary>
    public static bool TryRead(Relation surface, JointShapeDescription.Message message, out Read read, out Relation witness)
    {
        read = null!; witness = new(surface, StructuralConstraint.Open());
        if (surface is not { Left: Relation { Left: Relation roles, Right: Relation slots },
            Right: Relation { Left: Relation { Left: Relation lengths, Right: Relation angles },
                Right: Relation { Left: Relation similar, Right: Relation { Left: Relation openLengths, Right: Relation openAngles } } } } ||
            !Closed(roles) || E(roles).Length != E(message.Roles).Length || !Unique(E(roles)) ||
            E(roles).Any(role => !E(message.Roles).Contains(role, FieldNative.Participants.Instance)) || !Closed(slots)) return false;
        var expected = message.JointReads.SelectMany(joint => E(joint.Rays).Cast<Relation>().Select(ray => ray.Left)).ToArray();
        var promises = new HashSet<IElement>(FieldNative.Participants.Instance);
        if (E(slots).Length != expected.Length || !Unique(E(slots)) || E(slots).Any(slot => !expected.Contains(slot, FieldNative.Participants.Instance)) ||
            !Groups(lengths, false) || !Groups(angles, true) || !Groups(similar, true) ||
            !Scope(openLengths) || E(openLengths).Any(item => !expected.Contains(item, FieldNative.Participants.Instance)) ||
            !Scope(openAngles) || E(openAngles).Any(item => !LocalPair(item))) return false;
        read = new(message, lengths, angles, similar, openLengths, openAngles, S(), surface);
        witness = new(surface, read.Surface); return true;

        bool Groups(Relation scope, bool turns)
        {
            if (!Closed(scope)) return false;
            foreach (var item in E(scope))
            {
                if (item is not Relation { Left: Placeholder promise, Right: Relation members } || !Closed(members) ||
                    expected.Contains(promise, FieldNative.Participants.Instance) || E(message.Roles).Contains(promise, FieldNative.Participants.Instance) ||
                    !promises.Add(promise) || E(members).Length < 2) return false;
                var observations = new List<IElement>();
                foreach (var entry in E(members))
                {
                    if (entry is not Relation binding || !ReferenceEquals(binding.Right, promise) ||
                        (turns ? !LocalPair(binding.Left) : !expected.Contains(binding.Left, FieldNative.Participants.Instance))) return false;
                    if (observations.Any(prior => SameMember(prior, binding.Left))) return false;
                    observations.Add(binding.Left);
                }
            }
            return true;
        }
        bool LocalPair(IElement item) => item is Relation pair && !ReferenceEquals(pair.Left, pair.Right) &&
            message.JointReads.Any(joint => E(joint.Rays).OfType<Relation>().Any(ray => ReferenceEquals(ray.Left, pair.Left)) &&
                E(joint.Rays).OfType<Relation>().Any(ray => ReferenceEquals(ray.Left, pair.Right)));
    }

    public static Assessment Assess(Read stream, JointShapeReconstruction.Candidate candidate)
    {
        var message = stream.Message; var frame = message.Frame; var metric = message.Basis.ReadRightAngle(S()).Answer == "Yes";
        var rays = new Dictionary<IElement, Relation>(FieldNative.Participants.Instance); var claims = new List<Claim>();
        var norms = new Dictionary<IElement, Norm>(FieldNative.Participants.Instance);
        foreach (var joint in message.JointReads)
            foreach (var ray in E(joint.Rays).Cast<Relation>())
                if (candidate.Sites.TryGetValue(joint.Role, out var site) && candidate.SlotEdges.TryGetValue(ray.Left, out var stroke) &&
                    QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out var word) &&
                    (ReferenceEquals(a, site) || ReferenceEquals(b, site)))
                    rays[ray.Left] = ReferenceEquals(a, site) ? word : Inverse(frame, word);
        foreach (var group in E(stream.ExactLengths).Cast<Relation>())
        {
            var members = Members(group); var first = members[0];
            foreach (var next in members.Skip(1))
            {
                var comparison = rays.TryGetValue(first, out var a) && rays.TryGetValue(next, out var b)
                    ? CompareNorms(NativeNorm(first, a), NativeNorm(next, b)) : new Comparison("Open", StructuralConstraint.Open(first, next));
                Add("lengthEquivalence", group, comparison);
            }
        }
        foreach (var group in E(stream.ExactAngles).Cast<Relation>())
        {
            var members = Members(group).Cast<Relation>().ToArray(); var first = members[0];
            foreach (var next in members.Skip(1))
                Add("angleEquivalence", group, rays.TryGetValue(first.Left, out var a) && rays.TryGetValue(first.Right, out var b) &&
                    rays.TryGetValue(next.Left, out var c) && rays.TryGetValue(next.Right, out var d)
                    ? CompareAngles(frame, a, b, c, d, metric) : new("Open", StructuralConstraint.Open(first, next)));
        }
        foreach (var group in E(stream.SimilarAngles).Cast<Relation>())
        {
            // Coarse agreement supports a revisable similarity hypothesis. It
            // never supplies a hard equality constraint or a negative verdict.
            var members = Members(group).Cast<Relation>().ToArray(); var first = members[0];
            foreach (var next in members.Skip(1))
            {
                var supported = rays.TryGetValue(first.Left, out var a) && rays.TryGetValue(first.Right, out var b) &&
                    rays.TryGetValue(next.Left, out var c) && rays.TryGetValue(next.Right, out var d) && CoarseAngle(frame, a, b, c, d, metric);
                Add("angleSimilarityHypothesis", group, new(supported ? "Supported" : "Open", new(first, next)));
            }
        }
        foreach (var item in E(stream.UnresolvedLengths)) Add("lengthEquivalence", item, new("Open", StructuralConstraint.Open(item)));
        foreach (var item in E(stream.UnresolvedAngles)) Add("angleEquivalence", item, new("Open", StructuralConstraint.Open(item)));
        return new(stream, candidate, claims);
        void Add(string kind, IElement group, Comparison result) => claims.Add(new(kind, result.State, new Relation(group, result.Witness)));
        Norm NativeNorm(IElement slot, Relation word)
        {
            if (!norms.TryGetValue(slot, out var norm)) { norm = Measure(frame, word, metric); norms.Add(slot, norm); }
            return norm;
        }
    }

    public static IElement[] Members(Relation group) => E((Relation)group.Right).Cast<Relation>().Select(row => row.Left).ToArray();

    private static List<Ray> SourceRays(JointShapeDescription.Analysis analysis)
    {
        // Exact builder-audit grammar, not a recursive hunt for convenient
        // geometry. This private source read never survives the fresh port.
        var proofRows = (Relation)((Relation)((Relation)((Relation)analysis.Audit.Right).Right).Right).Left;
        var result = new List<Ray>();
        foreach (var proof in E(proofRows).Cast<Relation>())
        {
            var raw = (Relation)proof.Left; var fresh = (Relation)proof.Right;
            analysis.Source.Graph.TryReadJunction(raw, out _, out var native, out _, out _);
            var source = E(native).Cast<Relation>().ToArray(); var offered = E((Relation)((Relation)fresh.Right).Left).Cast<Relation>().ToArray();
            for (var index = 0; index < source.Length; index++)
            {
                var straight = E(analysis.Source.StraightPassages).Any(stroke => ReferenceEquals(stroke, source[index].Left));
                result.Add(new(offered[index].Left, fresh.Left, source[index].Left,
                    straight ? (Relation)((Relation)source[index].Right).Right : StructuralConstraint.Open()));
            }
        }
        return result;
    }

    private static Norm Measure(QualitativeMotionFrame frame, Relation word, bool metric)
    {
        if (!Reduced(frame, word, out var reduced)) return new(word, [], StructuralConstraint.Open(word), false);
        var moves = E(reduced).Cast<Relation>().ToArray();
        if (moves.Length == 0 || !UniformReach(moves)) return new(word, [], StructuralConstraint.Open(word), false);
        if (!metric && moves.Select(move => { frame.Direction(move, out var axis, out _, out _); return axis; })
            .Distinct(FieldNative.Participants.Instance).Count() != 1) return new(word, [], StructuralConstraint.Open(word), false);
        var products = new List<Product>(); var common = moves[0].Right;
        foreach (var a in moves) foreach (var b in moves)
        {
            frame.Direction(a, out var axisA, out _, out _); frame.Direction(b, out var axisB, out _, out _);
            if (!ReferenceEquals(axisA, axisB)) continue;
            var axis = metric ? (IElement)frame.Surface : axisA;
            // Distribution is actual repeated unit participation. Reduction
            // has already canceled opposing traversals of this common reach.
            products.Add(new(axis, common, new(new Relation(a, b), new Relation(axis, new Relation(common, common)))));
        }
        return new(word, products, new(word, S(products.Select(product => (IElement)product.Surface).ToArray())), true);
    }

    private static Comparison CompareNorms(Norm a, Norm b)
    {
        if (!a.Known || !b.Known || a.Products.Count == 0 || b.Products.Count == 0 ||
            !ReferenceEquals(a.Products[0].Reach, b.Products[0].Reach)) return new("Open", new(a.Witness, b.Witness));
        if (!ReferenceEquals(a.Products[0].Axis, b.Products[0].Axis)) return new("Open", new(a.Witness, b.Witness));
        // Measure has one common reach and one common selected squared-axis
        // promise. Pair actual occurrences directly rather than search a
        // repeatedly shortened list; the native cancellation is unchanged.
        var paired = Math.Min(a.Products.Count, b.Products.Count); var matched = new List<IElement>(); var unmatched = new List<IElement>();
        for (var index = 0; index < paired; index++) matched.Add(new Relation(a.Products[index].Surface, b.Products[index].Surface));
        unmatched.AddRange(a.Products.Skip(paired).Select(product => (IElement)product.Surface));
        unmatched.AddRange(b.Products.Skip(paired).Select(product => (IElement)product.Surface));
        return new(unmatched.Count == 0 ? "Supported" : "Contradicted", new(new Relation(a.Witness, b.Witness),
            new Relation(S(matched.ToArray()), S(unmatched.ToArray()))));
    }

    private static Comparison CompareAngles(QualitativeMotionFrame frame, Relation a, Relation b, Relation c, Relation d, bool metric)
    {
        if (!KnownRay(frame, a) || !KnownRay(frame, b) || !KnownRay(frame, c) || !KnownRay(frame, d))
            return new("Open", StructuralConstraint.Open(a, b, c, d));
        foreach (var action in Actions(frame, metric))
        {
            var x = Apply(a, action); var y = Apply(b, action);
            foreach (var order in new[] { (First: c, Second: d), (First: d, Second: c) })
                if (Proportional(frame, x, order.First, out var first) && Proportional(frame, y, order.Second, out var second))
                    return new("Supported", new(new Relation(new Relation(a, b), new Relation(c, d)),
                        new Relation(action, new Relation(first, second))));
        }
        var kindA = DefiniteAngle(frame, a, b, metric); var kindB = DefiniteAngle(frame, c, d, metric);
        return new(kindA is not null && kindB is not null && kindA != kindB ? "Contradicted" : "Open",
            new(new Relation(a, b), new Relation(c, d)));
    }

    private static int? DefiniteAngle(QualitativeMotionFrame frame, Relation a, Relation b, bool metric)
    {
        if (Proportional(frame, a, b, out _)) return 0;
        if (Proportional(frame, a, Inverse(frame, b), out _)) return 2;
        if (metric && Proportional(frame, Rotate(frame, a), b, out _) || metric && Proportional(frame, Rotate(frame, a), Inverse(frame, b), out _)) return 1;
        return null;
    }

    private static bool Proportional(QualitativeMotionFrame frame, Relation a, Relation b, out Relation witness)
    {
        witness = new(a, b);
        if (!Reduced(frame, a, out var x) || !Reduced(frame, b, out var y) || !SameHeading(frame, x, y)) return false;
        var first = E(x).Cast<Relation>().ToArray(); var second = E(y).Cast<Relation>().ToArray();
        if (!UniformReach(first) || !UniformReach(second)) return false;
        var positive = new List<Relation>(); var negative = new List<Relation>();
        foreach (var left in first) foreach (var right in second)
        {
            frame.Direction(left, out var axisA, out _, out _); frame.Direction(right, out var axisB, out _, out _);
            if (ReferenceEquals(axisA, axisB)) continue;
            // x_a*y_b and y_a*x_b have opposite determinant participation.
            (ReferenceEquals(axisA, frame.Horizontal) ? positive : negative).Add(new(left, right));
        }
        var pairs = new List<IElement>();
        while (positive.Count != 0 && negative.Count != 0)
        {
            pairs.Add(new Relation(positive[^1], negative[^1])); positive.RemoveAt(positive.Count - 1); negative.RemoveAt(negative.Count - 1);
        }
        witness = new(new Relation(a, b), new Relation(S(pairs.ToArray()), S(positive.Concat(negative).Cast<IElement>().ToArray())));
        return positive.Count == 0 && negative.Count == 0;
    }

    private static bool CoarseAngle(QualitativeMotionFrame frame, Relation a, Relation b, Relation c, Relation d, bool metric)
    {
        foreach (var action in Actions(frame, metric))
        {
            var x = Apply(a, action); var y = Apply(b, action);
            if (SameHeading(frame, x, c) && SameHeading(frame, y, d) || SameHeading(frame, x, d) && SameHeading(frame, y, c)) return true;
        }
        return false;
    }

    private static IEnumerable<Relation> Actions(QualitativeMotionFrame frame, bool metric)
    {
        IElement[] directions = [frame.Right, frame.Up, frame.Left, frame.Down];
        foreach (var offset in metric ? new[] { 0, 1, 2, 3 } : new[] { 0, 2 })
            yield return S(directions.Select((direction, index) => (IElement)new Relation(direction, directions[(index + offset) % 4])).ToArray());
        if (metric) foreach (var offset in new[] { 0, 1, 2, 3 })
            yield return S(directions.Select((direction, index) => (IElement)new Relation(direction, directions[(offset - index + 4) % 4])).ToArray());
    }
    private static Relation Apply(Relation word, Relation action) => S(E(word).Cast<Relation>().Select(move => (IElement)new Relation(
        E(action).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static bool SameHeading(QualitativeMotionFrame frame, Relation a, Relation b)
    {
        var x = Heading(frame, a); var y = Heading(frame, b);
        return Closed(x) && Closed(y) && E(x).Length == E(y).Length && E(x).All(direction => E(y).Contains(direction, FieldNative.Participants.Instance));
    }
    private static bool KnownRay(QualitativeMotionFrame frame, Relation word) => Reduced(frame, word, out var reduced) && E(reduced).Length != 0;
    private static bool Reduced(QualitativeMotionFrame frame, Relation word, out Relation reduced)
    {
        reduced = S();
        return frame.TryReadResult(frame.Reduce(S(), word), out reduced, out _, out var open) && Closed(reduced) && E(open).Length == 0 &&
            E(reduced).All(item => item is Relation move && frame.Direction(move, out _, out _, out _));
    }
    private static bool UniformReach(IReadOnlyList<Relation> moves) => moves.Count != 0 && moves.All(move => ReferenceEquals(move.Right, moves[0].Right));
    private static Relation Group(IEnumerable<IElement> members)
    {
        var promise = new Placeholder(); return new(promise, S(members.Select(member => (IElement)new Relation(member, promise)).ToArray()));
    }
    private static bool SameMember(IElement a, IElement b) => ReferenceEquals(a, b) || a is Relation x && b is Relation y &&
        (ReferenceEquals(x.Left, y.Left) && ReferenceEquals(x.Right, y.Right) || ReferenceEquals(x.Left, y.Right) && ReferenceEquals(x.Right, y.Left));
    private static bool Scope(Relation relation) => StructuralConstraint.TryReadScope(relation, out _, out _, out _);
    private static bool Unique(IEnumerable<IElement> items)
    { var seen = new HashSet<IElement>(FieldNative.Participants.Instance); return items.All(seen.Add); }
}
