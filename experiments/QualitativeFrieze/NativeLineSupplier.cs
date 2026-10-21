using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using P = Gojo.Experiments.QualitativeFrieze.StructuralPotential;
using E = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Connects actual published native passages to the same candidate
/// obligations used by StructuralPotential. Family discovery is bounded and
/// eager; geometric premise calculations run only at requested service leaves.
/// The public port explicitly exposes word positions and straight interpolation.
/// Private execution provenance is not part of that port.</summary>
public sealed class NativeLineSupplier
{
    // public port = (actual graph -> offered endpoint words) ->
    //               (straight passage scope -> selected radial/turn model)
    // support map = actual observed passage -> actual derived support
    // selection = (Closed(A0,A1) -> Closed(B0,B1)) -> source/target correspondence
    // connection = (public port -> selected support view) -> actual target source
    // producer reply = actual predicate -> evidence of native cancellation,
    //                  order, interpolation, direction or contour action.
    public NativeLinePublication Publication { get; }
    public NativeLatticeSources.SourceCase OriginalSource => Publication.Original;
    public NativeLatticeSources.SourceCase Source { get; }
    public NativeLineSupport Support { get; }
    public NativeLineBasis Basis { get; }
    public QualitativeRecognitionObservation Observation { get; }
    public Relation ObservationMap { get; }
    public Relation Port { get; }
    public IReadOnlyList<Candidate> Candidates { get; }
    public Relation Unsearched { get; }
    public Relation Unsupported { get; }
    public Relation Surface { get; }

    public NativeLineSupplier(NativeLatticeSources.SourceCase source, int candidateCapacity = 32)
        : this(source, new NativeLineBasis(source.Graph.Frame, source.TurnModel), candidateCapacity) { }

    private NativeLineSupplier(NativeLatticeSources.SourceCase source, NativeLineBasis basis, int candidateCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(candidateCapacity);
        Publication = new(source);
        source = Publication.Published;
        Source = source; Support = new(source.Graph, source.Bindings);
        Basis = basis;
        Port = new(new Relation(source.Graph.Surface, source.Bindings), new Relation(source.StraightPassages, Basis.Surface));
        var rows = new List<IElement>(); var maps = new List<IElement>(); var unsupported = new List<IElement>();
        var horizontal = new List<Relation>(); var vertical = new List<Relation>();
        foreach (var stroke in Items(Support.Graph.Strokes).OfType<Relation>())
        {
            if (!QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word))
            { unsupported.Add(stroke); continue; }
            var read = source.Graph.Frame.Reduce(S(), word);
            var known = source.Graph.Frame.TryReadResult(read, out var delta, out _, out var pending) && Items(pending).Length == 0 && Closed(delta);
            var headings = known ? Items(delta).OfType<Relation>().Select(move => move.Left)
                .Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray() : [];
            var observed = new Relation(new Placeholder(), new Relation(new Relation(from, to), known ? S(headings) : StructuralConstraint.Open()));
            rows.Add(observed); maps.Add(new Relation(observed, stroke));
            if (known && Pure(source.Graph.Frame, delta!, out var axis, out _))
            {
                if (ReferenceEquals(axis, source.Graph.Frame.Horizontal)) horizontal.Add(stroke);
                else vertical.Add(stroke);
            }
            else unsupported.Add(new Relation(stroke, read));
        }
        var allPublished = rows.Count == Items(Support.Graph.Strokes).Length && Closed(Support.Graph.Strokes);
        Observation = new(source.Graph.Frame, Support.Graph.Sites, allPublished ? S(rows.ToArray()) : StructuralConstraint.Open(rows.ToArray()),
            crossings: StructuralConstraint.Open());
        ObservationMap = S(maps.ToArray());
        var candidates = new List<Candidate>(); var cuts = new List<IElement>();
        for (var a = 0; a < horizontal.Count; a++) for (var aa = a + 1; aa < horizontal.Count; aa++)
            for (var b = 0; b < vertical.Count; b++) for (var bb = b + 1; bb < vertical.Count; bb++)
            {
                var supports = new[] { horizontal[a], horizontal[aa], vertical[b], vertical[bb] };
                // A negative known transverse order is a reversed selected frame,
                // not a new physical orientation or a failed separation.
                Orient(supports, 0, source.Graph.Frame.Vertical);
                Orient(supports, 2, source.Graph.Frame.Horizontal);
                var selection = new Relation(S(supports[0], supports[1]), S(supports[2], supports[3]));
                if (candidates.Count == candidateCapacity) { cuts.Add(selection); continue; }
                candidates.Add(new(this, supports, selection));
            }
        Candidates = candidates; Unsearched = S(cuts.ToArray()); Unsupported = S(unsupported.ToArray());
        Surface = new(Port, new Relation(Support.Surface, new Relation(Observation.Surface, new Relation(ObservationMap,
            new Relation(S(candidates.Select(candidate => (IElement)candidate.Surface).ToArray()), new Relation(Unsearched, Unsupported))))));

        void Orient(Relation[] supports, int first, Relation axis)
        {
            if (TryStart(supports[first], out var left) && TryStart(supports[first + 1], out var right))
            {
                var gap = Difference(source.Graph.Frame, Project(source.Graph.Frame, right, axis), Project(source.Graph.Frame, left, axis));
                if (Sign(source.Graph.Frame, gap, axis) == -1)
                    (supports[first], supports[first + 1]) = (supports[first + 1], supports[first]);
            }
        }
        bool TryStart(Relation stroke, out Relation position)
        {
            position = null!;
            return QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var site, out _, out _) &&
                Position(source, site, out position, out _) == "Yes";
        }
    }

    /// <summary>Rebuild a supplier from just its exposed native port. The
    /// complete port is rebound through one shared frame correspondence, so
    /// geometry, strokes, interpolation declarations and model retain aliases.
    /// No original program, factory ID or private execution history is needed.</summary>
    public static bool TryReadPort(Relation port, out NativeLineSupplier supplier, out Relation witness, int candidateCapacity = 32)
    {
        supplier = null!; witness = new(port, StructuralConstraint.Open());
        if (port.Left is not Relation { Left: Relation { Left: Relation frameSurface } } ||
            !QualitativeMotionFrameRead.TryReadStandard(frameSurface, out var frame) ||
            !frame.TryRebind(port, out var rebound, out var change) ||
            rebound is not Relation { Left: Relation { Left: Relation graphSurface, Right: Relation words },
                Right: Relation { Left: Relation straight, Right: Relation model } } ||
            graphSurface.Right is not Relation { Left: Relation { Left: Relation sites, Right: Relation strokes }, Right: Relation period } ||
            !ReferenceEquals(graphSurface.Left, frame.Target.Surface) ||
            !NativeLineBasis.TryReadInFrame(frame.Target, model, out var basis, out var basisRead)) return false;
        var graph = new QualitativeStrokeGraph(frame.Target, sites, strokes, period);
        var source = new NativeLatticeSources.SourceCase("imported", "Imported native word port", "No authoring history is available.",
            graph, words, straight, change, false);
        supplier = new(source, basis, candidateCapacity);
        witness = new(port, new Relation(change, new Relation(new Relation(graphSurface, graph.Surface), new Relation(basisRead, supplier.Port))));
        return true;
    }

    public sealed class Candidate
    {
        public NativeLineSupplier Supplier { get; }
        public P.Frame Frame { get; }
        public IReadOnlyList<Relation> Supports { get; }
        public Relation Selection { get; }
        public Relation Connection { get; }
        public Relation Surface { get; }

        internal Candidate(NativeLineSupplier supplier, Relation[] supports, Relation selection)
        {
            Supplier = supplier; Supports = supports; Selection = selection;
            var observed = supports.Select(stroke => (Relation)Items(supplier.ObservationMap).Cast<Relation>()
                .Single(row => ReferenceEquals(row.Right, stroke)).Left).ToArray();
            Frame = new(supplier.Observation, observed);
            var correspondence = S(observed.Select((passage, i) => (IElement)new Relation(passage, supports[i])).ToArray());
            Connection = new(new Relation(supplier.Port, new Relation(selection, correspondence)), Frame.Source);
            Surface = new(Connection, Frame.Surface);
        }

        public E.Service Service(int capacity = 32) => new(Frame.Source, Connection, Supplier.Port,
            S(Frame.Leaves.Select(leaf => (IElement)leaf.Predicate).ToArray()), capacity, request =>
                new E.Reply(request, Derive(request.Predicate)));

        public E.Receipt Read(Relation goal, int queryCapacity = 64, int serviceCapacity = 32) =>
            E.Read(Frame.Laws, new E.Publication(Frame.Source, []), goal, Service(serviceCapacity), queryCapacity);

        public Outcome Complete(Relation goal, Relation allowed, int queryCapacity = 64, int serviceCapacity = 32)
        {
            var read = Read(goal, queryCapacity, serviceCapacity);
            // Service cuts/refusals are not assumed geometric conditions. An
            // actual Open producer account also remains active in this new read.
            var safe = Items(read.BudgetCuts).Length == 0 && Items(read.ServiceOpen).Length == 0 && Items(read.ServiceBlocked).Length == 0;
            return new(read, safe ? Frame.Complete(read.ObservedPublication, goal, allowed, queryCapacity) : null);
        }

        private D.Evidence Derive(Relation predicate)
        {
            var source = Supplier.Source; var motion = source.Graph.Frame;
            if (ReferenceEquals(predicate, Frame.RightAngle))
            {
                var read = Supplier.Basis.ReadRightAngle(predicate);
                return new(predicate, Wrap(read.Supported), Wrap(read.Refuted), Wrap(read.Open), Wrap(read.Blocked));
            }
            if (ReferenceEquals(predicate, Frame.EqualSpacing))
            {
                var a = Gap(0, motion.Vertical); var b = Gap(2, motion.Horizontal);
                var proof = new Relation(a.Proof, b.Proof);
                if (a.Answer != "Yes" || b.Answer != "Yes") return Reply(Worst(a.Answer, b.Answer), proof);
                // Equal signed differences do not generally mean equal span
                // magnitudes when either direction is still unresolved.
                if (Sign(motion, a.Reduction!, motion.Vertical) is null || Sign(motion, b.Reduction!, motion.Horizontal) is null)
                    return Reply("Open", proof);
                var first = Aligned(a.Word!, motion.Vertical); var second = Aligned(b.Word!, motion.Horizontal);
                var read = Difference(motion, first.Word, second.Word);
                var sign = Sign(motion, read, motion.Horizontal);
                // Cross-axis comparison explicitly selects the common radial
                // reach interpretation advertised on the public basis port.
                proof = new(new Relation(Supplier.Basis.Surface, proof), new Relation(new Relation(first.Proof, second.Proof), read));
                return Reply(sign is null ? "Open" : sign == 0 ? "Yes" : "No", proof);
            }
            if (ReferenceEquals(predicate, Frame.SeparateA) || ReferenceEquals(predicate, Frame.SeparateB))
            {
                var first = ReferenceEquals(predicate, Frame.SeparateA) ? 0 : 2;
                var gap = Gap(first, first == 0 ? motion.Vertical : motion.Horizontal);
                if (gap.Answer != "Yes") return Reply(gap.Answer, gap.Proof);
                var sign = Sign(motion, gap.Reduction!, first == 0 ? motion.Vertical : motion.Horizontal);
                return Reply(sign is null ? "Open" : sign == 0 ? "No" : "Yes", gap.Proof);
            }
            if (ReferenceEquals(predicate, Frame.Independent))
            {
                var reads = Supports.Select(Profile).ToArray();
                var valid = reads.All(read => read.Answer == "Yes") &&
                    reads.Take(2).All(read => ReferenceEquals(read.Axis, motion.Horizontal)) &&
                    reads.Skip(2).All(read => ReferenceEquals(read.Axis, motion.Vertical));
                return Reply(valid ? "Yes" : reads.Any(read => read.Answer == "Blocked") ? "Blocked" : "Open",
                    new Relation(motion.Surface, S(reads.Select(read => (IElement)read.Proof).ToArray())));
            }
            if (ReferenceEquals(predicate, Frame.Straight))
            {
                var reads = Supports.Select(CheckSupport).ToArray();
                return Reply(reads.Aggregate("Yes", (answer, read) => Worst(answer, read.Answer)),
                    S(reads.Select(read => (IElement)read.Proof).ToArray()));
            }
            for (var i = 0; i < Frame.SpanCoverage.Count; i++)
                if (ReferenceEquals(predicate, Frame.SpanCoverage[i])) return Coverage(i);
            return Reply("Open", new Relation(predicate, Supplier.Port));

            Relation Wrap(Relation scope) => S(Items(scope).Select(proof => (IElement)new Relation(Connection, proof)).ToArray());
            D.Evidence Reply(string answer, Relation proof) => D.Evidence.FromAnswer(predicate, answer, new Relation(Connection, proof));
            D.Evidence Coverage(int index)
            {
                var support = CheckSupport(Supports[index]);
                if (support.Answer != "Yes") return Reply(support.Answer, support.Proof);
                var others = index < 2 ? new[] { 2, 3 } : new[] { 0, 1 };
                var pointReads = others.Select(other => Encounter(index, other)).ToArray();
                if (pointReads.Any(read => read.Answer != "Yes"))
                    return Reply(pointReads.Aggregate("Yes", (answer, read) => Worst(answer, read.Answer)),
                        S(pointReads.Select(read => (IElement)read.Proof).ToArray()));
                var intervals = pointReads.Select(point => Inside(Supports[index], point.Word!)).ToArray();
                return Reply(intervals.Any(read => read.Answer == "No") ? "No" :
                    intervals.Aggregate("Yes", (answer, read) => Worst(answer, read.Answer)),
                    new Relation(support.Proof, S(intervals.Select((read, i) => (IElement)new Relation(pointReads[i].Proof, read.Proof)).ToArray())));
            }
        }

        private sealed record Calculation(string Answer, Relation Proof, Relation? Word = null, Relation? Axis = null, Relation? Reduction = null);

        private Calculation Profile(Relation stroke)
        {
            var motion = Supplier.Source.Graph.Frame;
            if (!Items(Supplier.Support.Graph.Strokes).Any(actual => ReferenceEquals(actual, stroke)) ||
                !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out var word))
                return new("Blocked", new(stroke, Supplier.Support.Graph.Surface));
            var read = motion.Reduce(S(), word);
            if (!motion.TryReadResult(read, out var remaining, out _, out var open) || Items(open).Length != 0 || !Pure(motion, remaining, out var axis, out _))
                return new("Open", new(stroke, read));
            return new("Yes", new(stroke, read), remaining, axis);
        }

        private Calculation CheckSupport(Relation stroke)
        {
            var source = Supplier.Source; var motion = source.Graph.Frame; var profile = Profile(stroke);
            if (profile.Answer != "Yes") return profile;
            var route = Items(Supplier.Support.Correspondence).OfType<Relation>().Single(row => ReferenceEquals(row.Left, stroke));
            var proofs = new List<IElement> { profile.Proof, route }; var answer = "Yes";
            foreach (var use in Items((Relation)route.Right).Cast<Relation>())
            {
                var actual = (Relation)use.Left;
                if (!Items(source.StraightPassages).Any(declared => ReferenceEquals(actual, declared))) answer = Worst(answer, "Open");
                if (!QualitativeStrokeGraph.TryReadStroke(actual, out _, out var from, out var to, out var word)) return new("Blocked", route);
                var a = Position(source, from, out var start, out var ap); var b = Position(source, to, out var end, out var bp);
                proofs.Add(new Relation(actual, new Relation(ap, bp))); answer = Worst(answer, Worst(a, b));
                if (a != "Yes" || b != "Yes") continue;
                var endpoint = motion.Reduce(start, word); motion.TryReadResult(endpoint, out var predicted, out _, out _);
                var consistency = Difference(motion, predicted, end); proofs.Add(new Relation(endpoint, consistency));
                var sign = SignAny(motion, consistency);
                answer = Worst(answer, sign is null ? "Open" : sign == 0 ? "Yes" : "Blocked");
            }
            return new(answer, new(source.StraightPassages, S(proofs.ToArray())), profile.Word, profile.Axis);
        }

        private Calculation Gap(int first, Relation transverse)
        {
            var source = Supplier.Source; var motion = source.Graph.Frame;
            QualitativeStrokeGraph.TryReadStroke(Supports[first], out _, out var a, out _, out _);
            QualitativeStrokeGraph.TryReadStroke(Supports[first + 1], out _, out var b, out _, out _);
            var aa = Position(source, a, out var pa, out var ap); var bb = Position(source, b, out var pb, out var bp);
            var inputs = new Relation(ap, bp);
            if (aa != "Yes" || bb != "Yes") return new(Worst(aa, bb), inputs);
            var read = Difference(motion, Project(motion, pb, transverse), Project(motion, pa, transverse));
            motion.TryReadResult(read, out var word, out _, out _);
            return new("Yes", new(inputs, read), word, transverse, read);
        }

        private (Relation Word, Relation Proof) Aligned(Relation word, Relation axis)
        {
            var motion = Supplier.Source.Graph.Frame;
            var sign = Sign(motion, motion.Reduce(S(), word), axis);
            var oriented = sign == -1 ? UnitArrangementRead.Inverse(motion, word) : word;
            var original = Items(oriented).Cast<Relation>().ToArray();
            var mapped = original.Select(move => new Relation(
                motion.Direction(move, out _, out var forward, out _) && forward ? motion.Right : motion.Left, move.Right)).ToArray();
            var result = S(mapped.Cast<IElement>().ToArray());
            var map = S(original.Select((move, i) => (IElement)new Relation(move, mapped[i])).ToArray());
            return (result, new(new Relation(word, oriented), new Relation(new Relation(axis, motion.Horizontal), new Relation(map, result))));
        }

        private Calculation Encounter(int first, int second)
        {
            var source = Supplier.Source; var motion = source.Graph.Frame;
            var h = first < 2 ? first : second; var v = first < 2 ? second : first;
            QualitativeStrokeGraph.TryReadStroke(Supports[h], out _, out var hs, out _, out _);
            QualitativeStrokeGraph.TryReadStroke(Supports[v], out _, out var vs, out _, out _);
            var ha = Position(source, hs, out var hp, out var hproof); var va = Position(source, vs, out var vp, out var vproof);
            var proof = new Relation(new Relation(Supports[h], Supports[v]), new Relation(hproof, vproof));
            if (ha != "Yes" || va != "Yes") return new(Worst(ha, va), proof);
            var point = S(Items(Project(motion, vp, motion.Horizontal)).Concat(Items(Project(motion, hp, motion.Vertical))).ToArray());
            return new("Yes", new(proof, point), point);
        }

        private Calculation Inside(Relation stroke, Relation point)
        {
            var source = Supplier.Source; var motion = source.Graph.Frame;
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _);
            var a = Position(source, from, out var start, out var ap); var b = Position(source, to, out var end, out var bp);
            if (a != "Yes" || b != "Yes") return new(Worst(a, b), new(ap, bp));
            var profile = Profile(stroke); if (profile.Answer != "Yes") return profile;
            var before = Difference(motion, point, start); var after = Difference(motion, end, point);
            var x = Sign(motion, before, profile.Axis!); var y = Sign(motion, after, profile.Axis!);
            var proof = new Relation(new Relation(stroke, point), new Relation(new Relation(ap, bp), new Relation(before, after)));
            return new(x == -1 || y == -1 ? "No" : x is null || y is null ? "Open" : "Yes", proof);
        }
    }

    public sealed class Outcome(E.Receipt sourceRead, P.Completion? completion)
    {
        public E.Receipt SourceRead { get; } = sourceRead;
        public P.Completion? Completion { get; } = completion;
        public Relation Surface { get; } = new(sourceRead.Surface, completion?.Surface ?? StructuralConstraint.Open());
    }

    private static string Position(NativeLatticeSources.SourceCase source, IElement site, out Relation word, out Relation proof)
    {
        word = S(); var rows = Items(source.Bindings).OfType<Relation>().Where(row => ReferenceEquals(row.Left, site)).ToArray();
        proof = new(new Relation(source.Graph.Surface, source.Bindings), new Relation(site, S(rows.Cast<IElement>().ToArray())));
        if (!Closed(source.Bindings)) return "Open";
        if (rows.Length != 1 || rows[0].Right is not Relation actual) return "Open";
        if (!Closed(actual) || Items(actual).Any(item => item is not Relation move || !source.Graph.Frame.Direction(move, out _, out _, out _))) return "Open";
        word = actual; return "Yes";
    }
    private static Relation Project(QualitativeMotionFrame frame, Relation word, Relation axis) => S(Items(word).Cast<Relation>()
        .Where(move => frame.Direction(move, out var actual, out _, out _) && ReferenceEquals(actual, axis)).Cast<IElement>().ToArray());
    private static Relation Difference(QualitativeMotionFrame frame, Relation first, Relation second) => frame.Reduce(first, UnitArrangementRead.Inverse(frame, second));
    private static int? Sign(QualitativeMotionFrame frame, Relation reduction, Relation axis)
    {
        if (!frame.TryReadResult(reduction, out var word, out _, out var open) || Items(open).Length != 0 || !Closed(word)) return null;
        if (Items(word).Length == 0) return 0;
        return Pure(frame, word, out var actual, out var positive) && ReferenceEquals(actual, axis) ? positive ? 1 : -1 : null;
    }
    private static int? SignAny(QualitativeMotionFrame frame, Relation reduction)
    {
        if (!frame.TryReadResult(reduction, out var word, out _, out var open) || Items(open).Length != 0 || !Closed(word)) return null;
        return Items(word).Length == 0 ? 0 : 1;
    }
    private static bool Pure(QualitativeMotionFrame frame, Relation word, out Relation axis, out bool forward)
    {
        axis = null!; forward = false; var rows = Items(word);
        if (!Closed(word) || rows.Length == 0 || rows[0] is not Relation first || !frame.Direction(first, out axis, out forward, out _)) return false;
        var chosen = axis; var face = forward;
        return rows.All(item => item is Relation move && frame.Direction(move, out var actual, out var direction, out _) && ReferenceEquals(actual, chosen) && direction == face);
    }
    private static string Worst(string a, string b) => a == "Blocked" || b == "Blocked" ? "Blocked" : a == "Open" || b == "Open" ? "Open" : a == "No" || b == "No" ? "No" : "Yes";
    private static Relation S(params IElement[] entries) => P.S(entries);
    private static IElement[] Items(Relation scope) => P.Items(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var a, out var b) && a.Equals(b);
}
