using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Optional, deliberately finer publication of native position words.
/// The selected interpretation gives every repeated occurrence of one common
/// reach the same extent in both independent axes. It is not inferred from a
/// history-blind graph, and it is not a pixel or renderer measurement. The
/// policy can consume the resulting coarse receipts without receiving words.</summary>
public static class OrnamentDefaultGeometry
{
    public sealed class Publication
    {
        public QualitativeMotionFrame Frame { get; }
        public QualitativeRecognitionObservation Observation { get; }
        /// <summary>Closed(actual published site -> native position word).</summary>
        public Relation Bindings { get; }
        public Relation MechanicalGroups { get; }
        public Relation Port { get; }
        public Relation Provenance { get; }

        public Publication(QualitativeMotionFrame frame, QualitativeRecognitionObservation observation,
            Relation bindings, Relation? mechanicalGroups = null)
        {
            Frame = frame; Observation = observation; Bindings = bindings;
            MechanicalGroups = mechanicalGroups ?? StructuralConstraint.Open();
            Port = new(new Relation(frame.Surface, bindings), new Relation(observation.Surface, MechanicalGroups));
            Provenance = S();
        }

        private Publication(Publication read, Relation provenance)
        { Frame = read.Frame; Observation = read.Observation; Bindings = read.Bindings; MechanicalGroups = read.MechanicalGroups; Port = read.Port; Provenance = provenance; }

        /// <summary>Exact independent port import. Rebinding preserves public
        /// site/edge aliases across words, graph and mechanical declarations.</summary>
        public static bool TryRead(Relation port, out Publication publication, out Relation witness)
        {
            publication = null!;
            witness = new(port, StructuralConstraint.Open());
            if (port is not { Left: Relation { Left: Relation sourceFrame } } ||
                !QualitativeMotionFrameRead.TryReadStandard(sourceFrame, out var frame) ||
                !frame.TryRebind(port, out var renamed, out var basisProof) || renamed is not Relation
                { Left: Relation { Right: Relation bindings }, Right: Relation { Left: Relation observationSurface, Right: Relation groups } } ||
                observationSurface is not { Left: Relation { Left: Relation observationFrame } } ||
                !QualitativeMotionFrameRead.TryReadStandard(observationFrame, out var observationRead) ||
                !observationRead.TryRebind(new Relation(observationSurface, groups), out var rebound, out var observationProof) ||
                rebound is not Relation { Left: Relation { Left: Relation { Right: Relation sites }, Right: Relation
                    { Left: Relation segments, Right: Relation { Left: Relation continuations, Right: Relation crossings } } },
                    Right: Relation publicGroups }) return false;
            var observation = new QualitativeRecognitionObservation(observationRead.Target, sites, segments, continuations, crossings);
            witness = new(port, new Relation(basisProof, observationProof));
            publication = new(new Publication(frame.Target, observation, bindings, publicGroups), witness);
            return true;
        }

        public static Publication FromSample(OrnamentReconstructionCases.Sample sample)
        {
            var words = new List<IElement>();
            foreach (var row in E(sample.Bindings).OfType<Relation>())
                if (sample.Publisher.TryMapSite(row.Left, out var site)) words.Add(new Relation(site, row.Right));
            return new(sample.Frame, sample.Observation, S(words.ToArray()), sample.MechanicalGroups);
        }
    }

    /// <summary>Source-free, optional chosen comparison standards. The aspect
    /// and resolution thresholds are themselves repeated native participation.
    /// No envelope is invented when the caller leaves it unspecified.</summary>
    public sealed class Standard
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public Relation EqualAxisReach { get; }
        public Relation AspectThreshold { get; }
        public Relation ResolutionThreshold { get; }
        public Relation Envelope { get; }
        public Relation Surface { get; }
        public Question NoUnexpectedEncounters { get; }
        public Question NoCollinearOverlap { get; }
        public Question LegibleClosedGroups { get; }
        public Question ModerateAspect { get; }
        public Question WithinEnvelope { get; }
        public Question DisjointFilledInteriors { get; }
        public Question HorizontalMirror { get; }
        public Question QuarterTurn { get; }
        public IReadOnlyList<Question> Questions { get; }
        internal int AspectLimit { get; }
        internal int ThinLimit { get; }
        internal int? EnvelopeHalfSpan { get; }

        public Standard(int aspectLimit = 4, int thinLimit = 8, int? envelopeHalfSpan = null)
        {
            if (aspectLimit < 1 || thinLimit < 1 || envelopeHalfSpan is < 1)
                throw new ArgumentOutOfRangeException(nameof(aspectLimit), "Comparison standards must be positive native repetitions.");
            AspectLimit = aspectLimit; ThinLimit = thinLimit; EnvelopeHalfSpan = envelopeHalfSpan;
            EqualAxisReach = new(QualitativeMotionFrame.Move(Frame.Right, Reach), QualitativeMotionFrame.Move(Frame.Up, Reach));
            AspectThreshold = Word(aspectLimit, 0); ResolutionThreshold = Word(thinLimit, 0);
            Envelope = envelopeHalfSpan is int half ? S(Word(-half, -half), Word(half, -half), Word(half, half), Word(-half, half)) : StructuralConstraint.Open();
            Surface = new(new Relation(Frame.Surface, EqualAxisReach), new Relation(AspectThreshold, new Relation(ResolutionThreshold, Envelope)));

            // Question bodies are source-free structural obligations. Concrete
            // reads retain their inspected participation and comparisons below.
            IElement a = new Placeholder(), b = new Placeholder(), c = new Placeholder(), d = new Placeholder();
            var first = new Relation(a, b); var second = new Relation(c, d);
            NoUnexpectedEncounters = new(this, "No unexplained geometric encounters", new(new Relation(first, second), S(new Relation(a, c), new Relation(first, second))), 0);
            NoCollinearOverlap = new(this, "No positive collinear overlap", new(new Relation(first, second), new Relation(new Relation(a, c), new Relation(b, d))), 1);
            LegibleClosedGroups = new(this, "Closed contours retain resolvable breadth", new(new Relation(first, new Relation(second, new Relation(d, a))), ResolutionThreshold), 2);
            ModerateAspect = new(this, "Ordinary overall aspect", new(new Relation(first, second), AspectThreshold), 3);
            WithinEnvelope = new(this, "Inside the explicitly selected envelope", new(a, Envelope), 4);
            DisjointFilledInteriors = new(this, "Disjoint occupied interiors", new(S(first, second), StructuralConstraint.Open()), 5);
            HorizontalMirror = new(this, "Mirror in the selected word frame", new(new Relation(first, second), S(
                new Relation(Frame.Right, Frame.Left), new Relation(Frame.Left, Frame.Right), new Relation(Frame.Up, Frame.Up), new Relation(Frame.Down, Frame.Down))), 6);
            QuarterTurn = new(this, "Quarter-basis symmetry in the selected word frame", new(new Relation(first, second), S(
                new Relation(Frame.Right, Frame.Up), new Relation(Frame.Up, Frame.Left), new Relation(Frame.Left, Frame.Down), new Relation(Frame.Down, Frame.Right))), 7);
            Questions = [NoUnexpectedEncounters, NoCollinearOverlap, LegibleClosedGroups, ModerateAspect, WithinEnvelope, DisjointFilledInteriors, HorizontalMirror, QuarterTurn];
        }

        public static bool TryRead(Relation surface, out Standard standard, out Relation witness)
        {
            standard = null!; witness = new(surface, StructuralConstraint.Open());
            if (surface is not { Left: Relation { Left: Relation sourceFrame } } ||
                !QualitativeMotionFrameRead.TryReadStandard(sourceFrame, out var read) ||
                !read.TryRebind(surface, out var rebound, out var basisProof) ||
                rebound is not Relation { Right: Relation { Left: Relation aspect, Right: Relation
                    { Left: Relation resolution, Right: Relation envelope } } } ||
                !ReadPositive(aspect, read.Target, out var aspectCount) || !ReadPositive(resolution, read.Target, out var resolutionCount)) return false;
            int? halfSpan = null;
            if (C(envelope, out var corners))
            {
                if (corners is not [Relation first, _, _, _] || !ReadWord(first, read.Target, out var x, out var y) || x >= 0 || x != y) return false;
                halfSpan = -x;
            }
            var candidate = new Standard(aspectCount, resolutionCount, halfSpan);
            if (!SameShape(surface, candidate.Surface, out var aliases)) return false;
            standard = candidate; witness = new(surface, new Relation(candidate.Surface, new Relation(basisProof, aliases))); return true;
        }

        private Relation Word(int x, int y) => S(Enumerable.Repeat(x < 0 ? Frame.Left : Frame.Right, Math.Abs(x))
            .Concat(Enumerable.Repeat(y < 0 ? Frame.Down : Frame.Up, Math.Abs(y)))
            .Select(direction => (IElement)QualitativeMotionFrame.Move(direction, Reach)).ToArray());
    }

    public sealed class Question
    {
        public string Name { get; }
        public Relation Surface { get; }
        internal Standard Standard { get; }
        internal int Reader { get; }
        internal Question(Standard standard, string name, Relation obligation, int reader)
        { Standard = standard; Name = name; Reader = reader; Surface = new(standard.Surface, obligation); }
        private Question(string name, Relation surface, Question parsed)
        { Name = name; Surface = surface; Standard = parsed.Standard; Reader = parsed.Reader; }

        public static bool TryRead(string name, Relation surface, out Question question, out Relation witness)
        {
            question = null!; witness = new(surface, StructuralConstraint.Open());
            if (surface.Left is not Relation standardSurface || !Standard.TryRead(standardSurface, out var standard, out var standardProof)) return false;
            foreach (var candidate in standard.Questions)
                if (SameShape(surface, candidate.Surface, out var aliases))
                {
                    question = new(name, surface, candidate);
                    witness = new(surface, new Relation(standardProof, aliases)); return true;
                }
            return false;
        }
    }

    public sealed class Receipt
    {
        public Question Question { get; }
        public Publication Publication { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Basis { get; }
        public Relation Surface { get; }
        public string Answer { get; }

        public Receipt(Question question, Publication publication)
        {
            Question = question; Publication = publication;
            var yes = new List<IElement>(); var no = new List<IElement>(); var open = new List<IElement>(); var blocked = new List<IElement>();
            if (!Question.TryRead(question.Name, question.Surface, out var verified, out var questionProof))
            {
                Supported = Refuted = Open = S(); Blocked = S(question.Surface); Answer = "Blocked";
                Basis = new(question.Surface, questionProof); Surface = new(new Relation(question.Surface, publication.Port), new Relation(Basis, new Relation(Supported, new Relation(Refuted, new Relation(Open, Blocked))))); return;
            }
            question = verified;
            Basis = new(new Relation(questionProof, publication.Provenance), new Relation(question.Standard.Surface, publication.Frame.Surface));
            if (question.Reader == 5)
                open.Add(new Relation(publication.Observation.Surface, question.Surface)); // boundary is not occupied fill
            else if (!TryPositions(publication, out var positions, out var reach, out var causes)) open.Add(causes);
            else
            {
                Basis = new(Basis, new Relation(question.Standard.Reach, reach));
                var observation = publication.Observation;
                var edges = new List<Edge>();
                foreach (var row in E(observation.Segments).OfType<Relation>())
                    if (observation.TryReadSegment(row, out _, out var a, out var b, out _) && positions.TryGetValue(a, out var pa) && positions.TryGetValue(b, out var pb))
                        edges.Add(new(row, a, b, pa, pb));
                    else open.Add(row);
                if (!C(observation.Segments, out _)) open.Add(observation.Segments);
                if (open.Count == 0)
                {
                    switch (question.Reader)
                    {
                        case 0: Encounters(); break;
                        case 1: Overlaps(); break;
                        case 2: Breadth(); break;
                        case 3: Aspect(); break;
                        case 4: Boundary(); break;
                        case 6: Symmetry(false); break;
                        case 7: Symmetry(true); break;
                    }
                }

                Relation Pair(Edge a, Edge b) => new(new Relation(a.Row, b.Row), new Relation(
                    new Relation(new Relation(a.A.Word, a.B.Word), new Relation(b.A.Word, b.B.Word)), Basis));
                void Record(bool holds, IElement proof) { (holds ? yes : no).Add(proof); }
                void Encounters()
                {
                    if (!C(observation.Crossings, out var declarations)) { open.Add(observation.Crossings); return; }
                    foreach (var declaration in declarations)
                        if (declaration is not Relation row || !observation.TryReadCrossing(row, out _, out _)) open.Add(declaration);
                    if (open.Count != 0) return;
                    for (var i = 0; i < edges.Count; i++) for (var j = i + 1; j < edges.Count; j++)
                    {
                        var a = edges[i]; var b = edges[j];
                        var shared = N.Same(a.From, b.From) || N.Same(a.From, b.To) || N.Same(a.To, b.From) || N.Same(a.To, b.To);
                        var overlap = PositiveOverlap(a, b); var proper = Proper(a, b);
                        var declared = declarations.OfType<Relation>().Any(row =>
                            ReferenceEquals(row.Left, a.Row) && ReferenceEquals(row.Right, b.Row) || ReferenceEquals(row.Left, b.Row) && ReferenceEquals(row.Right, a.Row));
                        var incident = Touches(a, b);
                        // A shared endpoint explains contact, never positive
                        // retracing. A declared crossing explains a proper
                        // crossing, never a coincident interval or stray touch.
                        Record(!overlap && (!incident || shared || proper && declared), new Relation(Pair(a, b), S(declarations.Where(row => row is Relation r &&
                            (ReferenceEquals(r.Left, a.Row) && ReferenceEquals(r.Right, b.Row) || ReferenceEquals(r.Left, b.Row) && ReferenceEquals(r.Right, a.Row))).ToArray())));
                    }
                }
                void Overlaps()
                {
                    for (var i = 0; i < edges.Count; i++) for (var j = i + 1; j < edges.Count; j++)
                        Record(!PositiveOverlap(edges[i], edges[j]), Pair(edges[i], edges[j]));
                }
                void Aspect()
                {
                    if (positions.Count == 0) { open.Add(publication.Bindings); return; }
                    var values = positions.Values.ToArray(); var width = values.Max(p => p.X) - values.Min(p => p.X); var height = values.Max(p => p.Y) - values.Min(p => p.Y);
                    var min = Math.Min(width, height); var max = Math.Max(width, height);
                    Record(min > 0 && (long)min * question.Standard.AspectLimit >= max,
                        new Relation(Extrema(values), question.Standard.AspectThreshold));
                }
                void Boundary()
                {
                    if (question.Standard.EnvelopeHalfSpan is not int half) { open.Add(question.Standard.Envelope); return; }
                    foreach (var item in positions)
                        Record(item.Value.X >= -half && item.Value.X <= half && item.Value.Y >= -half && item.Value.Y <= half,
                            new Relation(new Relation(item.Key, item.Value.Word), question.Standard.Envelope));
                }
                void Breadth()
                {
                    if (!C(publication.MechanicalGroups, out var groups)) { open.Add(publication.MechanicalGroups); return; }
                    if (positions.Count == 0) { open.Add(publication.Bindings); return; }
                    var all = positions.Values.ToArray(); var reference = Math.Max(all.Max(p => p.X) - all.Min(p => p.X), all.Max(p => p.Y) - all.Min(p => p.Y));
                    foreach (var item in groups)
                    {
                        if (item is not Relation { Right: Relation { Left: Relation memberScope, Right: Relation closure } } group || !C(closure, out var ends) || !C(memberScope, out var members))
                        { open.Add(item); continue; }
                        if (ends.Length == 0) continue; // no two-dimensional loop obligation for an ordinary line
                        if (ends is not [Relation seam] || !N.Same(seam.Left, seam.Right)) { open.Add(group); continue; }
                        var loopEdges = members.Select(member => edges.SingleOrDefault(edge => ReferenceEquals(member, edge.Row))).ToArray();
                        if (loopEdges.Length < 3 || loopEdges.Any(edge => edge is null)) { open.Add(group); continue; }
                        if (!N.Same(loopEdges[0]!.From, seam.Left) || !N.Same(loopEdges[^1]!.To, seam.Right) ||
                            Enumerable.Range(1, loopEdges.Length - 1).Any(index => !N.Same(loopEdges[index - 1]!.To, loopEdges[index]!.From)))
                        { open.Add(group); continue; }
                        var loopPoints = loopEdges.SelectMany(edge => new[] { edge!.A, edge.B }).ToArray();
                        foreach (var edge in loopEdges)
                        {
                            var dx = edge!.B.X - edge.A.X; var dy = edge.B.Y - edge.A.Y;
                            var span = loopPoints.Select(p => (long)dx * p.Y - (long)dy * p.X).ToArray();
                            var extent = span.Max() - span.Min(); var normal = Math.Abs(dx) + Math.Abs(dy);
                            // Exact strip breadth under this selected dual L1
                            // word basis. This detects flattening, including
                            // diagonal flattening, not every narrow concavity.
                            Record(normal > 0 && extent * question.Standard.ThinLimit >= (long)normal * reference,
                                new Relation(group, new Relation(edge.Row, new Relation(Extrema(loopPoints), new Relation(Extrema(all), question.Standard.ResolutionThreshold)))));
                        }
                    }
                }
                void Symmetry(bool quarter)
                {
                    if (positions.Count == 0) { open.Add(publication.Bindings); return; }
                    var values = positions.Values.ToArray();
                    var cx2 = values.Max(p => p.X) + values.Min(p => p.X); var cy2 = values.Max(p => p.Y) + values.Min(p => p.Y);
                    (long X, long Y) Transformed(Position p) => quarter
                        ? ((long)cx2 + cy2 - 2L * p.Y, (long)cy2 - cx2 + 2L * p.X)
                        : (2L * cx2 - 2L * p.X, 2L * p.Y);
                    var remaining = new List<Edge>(edges); var correspondence = new List<IElement>();
                    foreach (var edge in edges)
                    {
                        var a = Transformed(edge.A); var b = Transformed(edge.B);
                        var match = remaining.FirstOrDefault(other =>
                            a == (2L * other.A.X, 2L * other.A.Y) && b == (2L * other.B.X, 2L * other.B.Y) ||
                            a == (2L * other.B.X, 2L * other.B.Y) && b == (2L * other.A.X, 2L * other.A.Y));
                        if (match is null) { no.Add(new Relation(edge.Row, new Relation(Extrema(values), question.Surface))); continue; }
                        remaining.Remove(match); correspondence.Add(new Relation(edge.Row, match.Row));
                    }
                    yes.Add(new Relation(S(correspondence.ToArray()), new Relation(Extrema(values), Basis)));
                }
            }
            Supported = S(yes.ToArray()); Refuted = S(no.ToArray()); Open = S(open.ToArray()); Blocked = S(blocked.ToArray());
            Answer = blocked.Count > 0 ? "Blocked" : no.Count > 0 ? "No" : open.Count > 0 ? "Open" : "Yes";
            Surface = new(new Relation(question.Surface, publication.Port), new Relation(Basis,
                new Relation(Supported, new Relation(Refuted, new Relation(Open, Blocked)))));
        }
    }

    private sealed record Position(int X, int Y, Relation Word);
    private sealed record Edge(Relation Row, IElement From, IElement To, Position A, Position B);

    private static bool TryPositions(Publication publication, out Dictionary<IElement, Position> positions, out IElement reach, out Relation causes)
    {
        positions = new(N.Participants.Instance); var failures = new List<IElement>(); var reaches = new HashSet<IElement>(N.Participants.Instance);
        reach = null!;
        if (!C(publication.Bindings, out var rows) || !C(publication.Observation.Sites, out var sites))
        { causes = new(publication.Bindings, publication.Observation.Sites); return false; }
        if (rows.Length != sites.Length || rows.Any(item => item is not Relation row || !sites.Any(site => N.Same(site, row.Left))))
        { causes = new(publication.Bindings, publication.Observation.Sites); return false; }
        foreach (var site in sites)
        {
            var bindings = rows.OfType<Relation>().Where(row => N.Same(row.Left, site)).ToArray();
            if (bindings is not [var binding] || binding.Right is not Relation word || !C(word, out var terms)) { failures.Add(site); continue; }
            var x = 0; var y = 0; var valid = true;
            foreach (var item in terms)
            {
                if (item is not Relation move || !publication.Frame.Direction(move, out var axis, out var forward, out var extent)) { failures.Add(item); valid = false; continue; }
                reaches.Add(extent);
                if (ReferenceEquals(axis, publication.Frame.Horizontal)) x = checked(x + (forward ? 1 : -1)); else y = checked(y + (forward ? 1 : -1));
            }
            if (valid) positions.Add(site, new(x, y, word));
        }
        if (reaches.Count != 1) failures.Add(publication.Bindings);
        causes = S(failures.ToArray()); if (failures.Count != 0) return false;
        reach = reaches.Single(); return true;
    }

    private static Relation Extrema(Position[] points)
    {
        if (points.Length == 0) return S();
        return S(points.MinBy(p => p.X)!.Word, points.MaxBy(p => p.X)!.Word, points.MinBy(p => p.Y)!.Word, points.MaxBy(p => p.Y)!.Word);
    }
    private static bool ReadPositive(Relation word, QualitativeMotionFrame frame, out int count)
    {
        count = 0;
        if (!C(word, out var terms) || terms.Length == 0) return false;
        foreach (var term in terms)
            if (term is not Relation move || !ReferenceEquals(move.Left, frame.Right) || !frame.Direction(move, out _, out _, out _)) return false;
        count = terms.Length; return true;
    }
    private static bool ReadWord(Relation word, QualitativeMotionFrame frame, out int x, out int y)
    {
        x = y = 0; if (!C(word, out var terms)) return false;
        foreach (var term in terms)
        {
            if (term is not Relation move || !frame.Direction(move, out var axis, out var forward, out _)) return false;
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return true;
    }
    private static bool SameShape(IElement source, IElement target, out Relation witness)
    {
        var forward = new Dictionary<IElement, IElement>(N.Participants.Instance); var reverse = new Dictionary<IElement, IElement>(N.Participants.Instance);
        var pending = new Stack<(IElement Source, IElement Target)>(); pending.Push((source, target)); var valid = true;
        while (pending.TryPop(out var pair))
        {
            if (forward.TryGetValue(pair.Source, out var prior)) { if (!N.Same(prior, pair.Target)) { valid = false; break; } continue; }
            if (reverse.TryGetValue(pair.Target, out var other) && !N.Same(other, pair.Source)) { valid = false; break; }
            if (pair.Source is Relation a && pair.Target is Relation b)
            { forward.Add(a, b); reverse.Add(b, a); pending.Push((a.Left, b.Left)); pending.Push((a.Right, b.Right)); }
            else if (pair.Source is Placeholder aSlot && pair.Target is Placeholder bSlot && aSlot.IsInverse == bSlot.IsInverse)
            {
                if (forward.TryGetValue(aSlot.Dual(), out var dual) && !N.Same(dual, bSlot.Dual())) { valid = false; break; }
                if (reverse.TryGetValue(bSlot.Dual(), out var sourceDual) && !N.Same(sourceDual, aSlot.Dual())) { valid = false; break; }
                forward.Add(aSlot, bSlot); reverse.Add(bSlot, aSlot);
            }
            else { valid = false; break; }
        }
        witness = valid ? S(forward.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()) : StructuralConstraint.Open(); return valid;
    }
    private static long Cross(Position a, Position b, Position c) => checked((long)(b.X - a.X) * (c.Y - a.Y) - (long)(b.Y - a.Y) * (c.X - a.X));
    private static bool Proper(Edge a, Edge b) => Math.Sign(Cross(a.A, a.B, b.A)) * Math.Sign(Cross(a.A, a.B, b.B)) < 0 && Math.Sign(Cross(b.A, b.B, a.A)) * Math.Sign(Cross(b.A, b.B, a.B)) < 0;
    private static bool On(Position a, Position b, Position p) => Cross(a, b, p) == 0 && p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X) && p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);
    private static bool Touches(Edge a, Edge b) => Proper(a, b) || On(a.A, a.B, b.A) || On(a.A, a.B, b.B) || On(b.A, b.B, a.A) || On(b.A, b.B, a.B);
    private static bool PositiveOverlap(Edge a, Edge b)
    {
        if (Cross(a.A, a.B, b.A) != 0 || Cross(a.A, a.B, b.B) != 0) return false;
        return a.A.X != a.B.X
            ? Math.Max(Math.Min(a.A.X, a.B.X), Math.Min(b.A.X, b.B.X)) < Math.Min(Math.Max(a.A.X, a.B.X), Math.Max(b.A.X, b.B.X))
            : Math.Max(Math.Min(a.A.Y, a.B.Y), Math.Min(b.A.Y, b.B.Y)) < Math.Min(Math.Max(a.A.Y, a.B.Y), Math.Max(b.A.Y, b.B.Y));
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool C(Relation scope, out IElement[] entries) => QualitativeRecognitionObservation.Closed(scope, out entries);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
