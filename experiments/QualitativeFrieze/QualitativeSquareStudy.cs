using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>One finite landmark cell with independently exposed direction
/// actions, site actions, contour succession and periodic seams. The grid is
/// authored repeated participation, without measured positions or carriers.</summary>
public sealed class QualitativeSquareStudy
{
    public QualitativeMotionFrame Frame { get; } = new();
    public IElement Reach { get; } = new Placeholder();
    public Relation Landmarks { get; }
    public Relation SiteBindings { get; }
    public Relation DirectionTurn { get; }
    public Relation DirectionMirror { get; }
    public Relation OtherDirectionMirror { get; }
    public Relation SiteTurn { get; }
    public Relation SiteMirror { get; }
    public Relation GeneratorCorrespondence { get; }
    public QualitativeActionAlgebra DirectionActions { get; }
    public QualitativeActionAlgebra SiteActions { get; }
    public QualitativeMarkedContour Contour { get; }
    public QualitativeMarkedContour UnequalOrUnknownTurns { get; }
    public Relation ContourCorrespondence { get; }
    public Relation ContourAgreement { get; }
    public QualitativePeriodicCell Cell { get; }
    public Relation CellRead { get; }
    public QualitativeStrokeGraph Motif { get; }
    public IReadOnlyList<Law> Laws { get; }
    public Relation ActionTransport { get; }
    public Relation SiteTransport { get; }
    public Relation Surface { get; }
    private readonly IElement[,] sites = new IElement[5, 5];
    private readonly IElement[] xLandmarks = Enumerable.Range(0, 5).Select(_ => (IElement)new Placeholder()).ToArray();
    private readonly IElement[] yLandmarks = Enumerable.Range(0, 5).Select(_ => (IElement)new Placeholder()).ToArray();

    public sealed record Law(string Name, Relation Witness, bool? Passed);
    public sealed record Pattern(string Name, QualitativeStrokeGraph Graph, Relation Bindings,
        Relation Actions, Relation Correspondences, Relation Surface);
    public sealed record Deformation(Pattern Source, QualitativePatternTransform Transform, QualitativeStrokeGraph Graph,
        Relation Bindings, Relation SiteMap, Relation StrokeMap, Relation ChordMap, Relation MapRead,
        Relation EndpointChecks, Relation Unresolved, Relation Surface);
    public sealed record Symmetry(Relation Witness, bool? Preserves);

    public QualitativeSquareStudy()
    {
        var bindings = new List<IElement>(); var landmarkRows = new List<IElement>();
        for (var x = 0; x < 5; x++) for (var y = 0; y < 5; y++)
        {
            var site = sites[x, y] = x == 2 && y == 2 ? Frame.Origin : new Placeholder();
            landmarkRows.Add(new Relation(new Relation(xLandmarks[x], yLandmarks[y]), site));
            bindings.Add(new Relation(site, Position(x - 2, y - 2)));
        }
        // Successor rows retain actual adjacent landmarks and common reaches.
        var xSteps = S(Enumerable.Range(0, 4).Select(i => (IElement)new Relation(new Relation(xLandmarks[i], xLandmarks[i + 1]), Move(Frame.Right))).ToArray());
        var ySteps = S(Enumerable.Range(0, 4).Select(i => (IElement)new Relation(new Relation(yLandmarks[i], yLandmarks[i + 1]), Move(Frame.Up))).ToArray());
        Landmarks = new Relation(new Relation(S(xLandmarks), S(yLandmarks)), new Relation(new Relation(xSteps, ySteps), S(landmarkRows.ToArray())));
        SiteBindings = S(bindings.ToArray());
        var directions = new IElement[] { Frame.Right, Frame.Up, Frame.Left, Frame.Down };
        DirectionActions = new(S(directions));
        DirectionTurn = S(directions.Select((direction, i) => (IElement)new Relation(direction, directions[(i + 1) % 4])).ToArray());
        DirectionMirror = S(new Relation(Frame.Right, Frame.Left), new Relation(Frame.Up, Frame.Up), new Relation(Frame.Left, Frame.Right), new Relation(Frame.Down, Frame.Down));
        var otherMirrorRead = DirectionActions.Evaluate(S(DirectionTurn, DirectionTurn, DirectionMirror));
        DirectionActions.TryReadResult(otherMirrorRead, out var otherMirror, out _, out _, out _); OtherDirectionMirror = otherMirror;
        SiteActions = new(S(bindings.Cast<Relation>().Select(row => row.Left).ToArray()));
        SiteTurn = S(Enumerable.Range(0, 5).SelectMany(x => Enumerable.Range(0, 5).Select(y => (IElement)new Relation(sites[x, y], sites[4 - y, x]))).ToArray());
        SiteMirror = S(Enumerable.Range(0, 5).SelectMany(x => Enumerable.Range(0, 5).Select(y => (IElement)new Relation(sites[x, y], sites[4 - x, y]))).ToArray());
        GeneratorCorrespondence = S(new Relation(DirectionTurn, SiteTurn), new Relation(DirectionMirror, SiteMirror));

        var markers = S(sites[4, 2], sites[2, 4], sites[0, 2], sites[2, 0]); var markerEntries = E(markers);
        var succession = S(markerEntries.Select((marker, i) => (IElement)new Relation(marker, markerEntries[(i + 1) % 4])).ToArray());
        var radialReach = S(Reach, Reach);
        UnequalOrUnknownTurns = new(sites[2, 2], S(radialReach), markers, succession, markerEntries[0]);
        Contour = UnequalOrUnknownTurns.WithEqualTurns(new Placeholder());
        ContourCorrespondence = S(markerEntries.Select((marker, i) => (IElement)new Relation(marker, directions[i])).ToArray());
        ContourAgreement = Contour.CompareAction(DirectionActions.Domain, DirectionTurn, ContourCorrespondence);

        var bottom = QualitativePeriodicCell.Path(S(Enumerable.Range(0, 5).Select(i => sites[i, 0]).ToArray()), Word(Frame.Right, 4));
        var top = QualitativePeriodicCell.Path(S(Enumerable.Range(0, 5).Select(i => sites[i, 4]).ToArray()), Word(Frame.Right, 4));
        var left = QualitativePeriodicCell.Path(S(Enumerable.Range(0, 5).Select(i => sites[0, i]).ToArray()), Word(Frame.Up, 4));
        var right = QualitativePeriodicCell.Path(S(Enumerable.Range(0, 5).Select(i => sites[4, i]).ToArray()), Word(Frame.Up, 4));
        var seamX = QualitativePeriodicCell.Seam(left, right, S(Enumerable.Range(0, 5).Select(i => (IElement)new Relation(sites[0, i], sites[4, i])).ToArray()));
        var seamY = QualitativePeriodicCell.Seam(bottom, top, S(Enumerable.Range(0, 5).Select(i => (IElement)new Relation(sites[i, 0], sites[i, 4])).ToArray()));
        var quotient = new Dictionary<(int, int), IElement>(); var glue = new List<Relation>();
        for (var x = 0; x < 5; x++) for (var y = 0; y < 5; y++) if (x == 0 || y == 0 || x == 4 || y == 4)
        {
            var key = (x % 4, y % 4); if (!quotient.TryGetValue(key, out var site)) quotient.Add(key, site = new Placeholder());
            glue.Add(new Relation(sites[x, y], site));
        }
        Cell = new(Frame, bottom, right, top, left, seamX, seamY, QualitativePeriodicCell.Glue(glue.ToArray())); CellRead = Cell.Evaluate();
        // This small handed hook touches an actual seam landmark. Orbit copies
        // share sites through the action map, never by rendered coincidence.
        Motif = new(Frame, SiteActions.Domain, S(
            QualitativeStrokeGraph.Stroke(new Placeholder(), sites[0, 2], sites[1, 2], Word(Frame.Right, 1)),
            QualitativeStrokeGraph.Stroke(new Placeholder(), sites[1, 2], sites[1, 3], Word(Frame.Up, 1))));
        var laws = new List<Law>();
        Equality("four turns return", DirectionActions.Compare(S(DirectionTurn, DirectionTurn, DirectionTurn, DirectionTurn), S()));
        Equality("two mirrors return", DirectionActions.Compare(S(DirectionMirror, DirectionMirror), S()));
        Equality("mirror / turn / mirror reverses turn", DirectionActions.Compare(S(DirectionMirror, DirectionTurn, DirectionMirror), S(DirectionTurn, DirectionTurn, DirectionTurn)));
        Equality("turn and mirror commute", DirectionActions.Commutes(DirectionTurn, DirectionMirror));
        Equality("independent flips commute", DirectionActions.Commutes(DirectionMirror, OtherDirectionMirror));
        Equality("two flips equal two turns", DirectionActions.Compare(S(DirectionMirror, OtherDirectionMirror), S(DirectionTurn, DirectionTurn)));
        Equality("one turn equals two flips", DirectionActions.Compare(S(DirectionTurn), S(DirectionMirror, OtherDirectionMirror)));
        Equality("site four-turn return", SiteActions.Compare(S(SiteTurn, SiteTurn, SiteTurn, SiteTurn), S()), SiteActions);
        Equality("site mirror relation", SiteActions.Compare(S(SiteMirror, SiteTurn, SiteMirror), S(SiteTurn, SiteTurn, SiteTurn)), SiteActions);
        Laws = laws.AsReadOnly();
        var transport = new List<IElement>();
        Transport(DirectionTurn, Cell.XWord, Cell.YWord); Transport(DirectionTurn, Cell.YWord, Inverse(Cell.XWord));
        Transport(DirectionMirror, Cell.XWord, Inverse(Cell.XWord)); Transport(DirectionMirror, Cell.YWord, Cell.YWord);
        ActionTransport = S(transport.ToArray());
        SiteTransport = S(RelateActions(DirectionTurn, SiteTurn), RelateActions(DirectionMirror, SiteMirror));
        Surface = new Relation(new Relation(Frame.Surface, new Relation(Landmarks, SiteBindings)),
            new Relation(new Relation(DirectionActions.Surface, new Relation(DirectionTurn, DirectionMirror)),
                new Relation(new Relation(SiteActions.Surface, new Relation(SiteTurn, SiteMirror)),
                    new Relation(new Relation(Contour.Surface, new Relation(ContourCorrespondence, ContourAgreement)),
                        new Relation(new Relation(Cell.Surface, CellRead), new Relation(Motif.Surface,
                            new Relation(S(laws.Select(law => (IElement)law.Witness).ToArray()), new Relation(ActionTransport,
                                new Relation(GeneratorCorrespondence, SiteTransport)))))))));
        void Equality(string name, Relation witness, QualitativeActionAlgebra? algebra = null)
        { var known = (algebra ?? DirectionActions).TryReadEquality(witness, out var equal); laws.Add(new(name, witness, known ? equal : null)); }
        void Transport(Relation action, Relation input, Relation target)
        {
            var transform = AxisTransform(action); var read = transform.Apply(input);
            if (!transform.TryReadComplete(read, out var output)) throw new InvalidOperationException("A declared direction action could not transport its period word.");
            var check = Frame.Reduce(output, Inverse(target));
            transport.Add(new Relation(new Relation(action, new Relation(input, target)), new Relation(read, check)));
        }
    }

    public QualitativePatternTransform AxisTransform(Relation action)
    {
        var complete = StructuralConstraint.TryReadScope(action, out var entries, out var seam, out var tail) && seam.Equals(tail);
        var rows = new List<IElement>();
        foreach (var entry in entries ?? [])
            if (entry is Relation row) rows.Add(new Relation(Move(row.Left), S(Move(row.Right))));
            else { complete = false; rows.Add(entry); }
        return new(Frame, Frame, complete ? S(rows.ToArray()) : StructuralConstraint.Open(rows.ToArray()), action);
    }

    /// <summary>Checks that the site action and direction action transport the
    /// same actual landmark displacements. Two unrelated permutations satisfying
    /// the same abstract group laws would not establish this correspondence.</summary>
    public Relation RelateActions(Relation directions, Relation siteMap)
    {
        var checks = new List<IElement>(); var pending = new List<IElement>(); var transform = AxisTransform(directions);
        var actionRead = SiteActions.Evaluate(S(siteMap));
        if (!SiteActions.TryReadResult(actionRead, out var map, out var blocked, out var unknown, out _) || E(blocked).Length != 0 || E(unknown).Length != 0)
            pending.Add(actionRead);
        else foreach (var row in E(map).Cast<Relation>())
        {
            var from = E(SiteBindings).Cast<Relation>().Single(binding => ReferenceEquals(binding.Left, row.Left));
            var to = E(SiteBindings).Cast<Relation>().Single(binding => ReferenceEquals(binding.Left, row.Right));
            var read = transform.Apply((Relation)from.Right);
            if (!transform.TryReadComplete(read, out var output)) { pending.Add(read); continue; }
            var cancellation = Frame.Reduce(output, Inverse((Relation)to.Right));
            var evidence = new Relation(new Relation(from, new Relation(row, to)), new Relation(read, cancellation));
            checks.Add(evidence);
            if (!Frame.TryReadResult(cancellation, out var rest, out _, out var unmet) || E(rest).Length != 0 || E(unmet).Length != 0) pending.Add(evidence);
        }
        return new Relation(new Relation(SiteBindings, new Relation(directions, siteMap)), new Relation(S(checks.ToArray()), S(pending.ToArray())));
    }

    public Pattern Generate(string name, params Relation[] directionWords)
    {
        var strokes = new List<IElement>(); var actionReads = new List<IElement>(); var correspondences = new List<IElement>();
        foreach (var word in directionWords)
        {
            var directionRead = DirectionActions.Evaluate(word);
            if (!DirectionActions.TryReadResult(directionRead, out var directionMap, out var blocked, out var pending, out _) || E(blocked).Length != 0 || E(pending).Length != 0)
                throw new ArgumentException("A pattern requires an admitted finite action.");
            var siteWord = S(E(word).Select(action => E(GeneratorCorrespondence).Cast<Relation>()
                .SingleOrDefault(row => ReferenceEquals(row.Left, action))?.Right ??
                throw new ArgumentException("The site correspondence is not exposed for this action.")).ToArray());
            var siteRead = SiteActions.Evaluate(siteWord); SiteActions.TryReadResult(siteRead, out var siteMap, out _, out _, out _);
            var transform = AxisTransform(directionMap); var siteRows = E(siteMap).Cast<Relation>().ToArray(); var images = new List<IElement>();
            foreach (var edge in E(Motif.Strokes).Cast<Relation>())
            {
                QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out var chord);
                var read = transform.Apply(chord); if (!transform.TryReadComplete(read, out var output)) throw new InvalidOperationException("Unresolved orbit chord.");
                var a = siteRows.Single(row => ReferenceEquals(row.Left, from)); var b = siteRows.Single(row => ReferenceEquals(row.Left, to));
                var emitted = QualitativeStrokeGraph.Stroke(new Relation(edge, new Relation(directionRead, siteRead)), a.Right, b.Right, output);
                strokes.Add(emitted); images.Add(new Relation(edge, emitted));
            }
            actionReads.Add(new Relation(directionRead, siteRead)); correspondences.Add(new Relation(siteMap, S(images.ToArray())));
        }
        var graph = new QualitativeStrokeGraph(Frame, SiteActions.Domain, S(strokes.ToArray()));
        var actions = S(actionReads.ToArray()); var maps = S(correspondences.ToArray());
        var surface = new Relation(Surface, new Relation(actions, new Relation(maps, graph.Surface)));
        return new(name, graph, SiteBindings, actions, maps, surface);
    }

    /// <summary>Constructs and checks a self-correspondence of the actual finite
    /// directed stroke multiset. Merely having a turn in the frame does not
    /// establish that a particular motif is invariant under that turn.</summary>
    public Symmetry ReadSymmetry(Pattern pattern, Relation directionAction, Relation siteAction)
    {
        var directionRead = DirectionActions.Evaluate(S(directionAction)); var siteRead = SiteActions.Evaluate(S(siteAction));
        var header = new Relation(pattern.Surface, new Relation(directionRead, siteRead));
        if (!ReferenceEquals(pattern.Graph.Frame, Frame) || !ReferenceEquals(pattern.Graph.Sites, SiteActions.Domain) || !ReferenceEquals(pattern.Bindings, SiteBindings) ||
            !DirectionActions.TryReadResult(directionRead, out var directions, out var db, out var du, out _) || E(db).Length != 0 || E(du).Length != 0 ||
            !SiteActions.TryReadResult(siteRead, out var sitesMap, out var sb, out var su, out _) || E(sb).Length != 0 || E(su).Length != 0 ||
            !StructuralConstraint.TryReadScope(pattern.Graph.Strokes, out var strokes, out var seam, out var tail) || !seam.Equals(tail))
            return new(new Relation(header, StructuralConstraint.Open()), null);
        var transform = AxisTransform(directions); var consumed = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var edges = new List<IElement>(); var chords = new List<IElement>(); var missing = new List<IElement>(); var checks = new List<IElement>();
        foreach (var item in strokes.Cast<Relation>())
        {
            if (!QualitativeStrokeGraph.TryReadStroke(item, out _, out var from, out var to, out var word)) return new(new Relation(header, StructuralConstraint.Open(item)), null);
            var a = E(sitesMap).Cast<Relation>().Single(row => ReferenceEquals(row.Left, from)).Right;
            var b = E(sitesMap).Cast<Relation>().Single(row => ReferenceEquals(row.Left, to)).Right;
            var projection = transform.Apply(word);
            if (!transform.TryReadComplete(projection, out var mapped)) return new(new Relation(header, StructuralConstraint.Open(projection)), null);
            Relation? found = null;
            foreach (var candidate in strokes.Cast<Relation>())
            {
                if (consumed.Contains(candidate)) continue;
                QualitativeStrokeGraph.TryReadStroke(candidate, out _, out var x, out var y, out var targetWord);
                if (!ReferenceEquals(a, x) || !ReferenceEquals(b, y)) continue;
                var cancellation = Frame.Reduce(mapped, Inverse(targetWord));
                if (!Frame.TryReadResult(cancellation, out var residual, out _, out var pending) || E(residual).Length != 0 || E(pending).Length != 0) continue;
                found = candidate; consumed.Add(candidate); edges.Add(new Relation(item, candidate)); chords.Add(new Relation(word, targetWord));
                checks.Add(new Relation(new Relation(item, candidate), new Relation(projection, cancellation))); break;
            }
            if (found is null) missing.Add(new Relation(item, new Relation(new Relation(a, b), projection)));
        }
        if (missing.Count != 0) return new(new Relation(header, new Relation(S(checks.ToArray()), S(missing.ToArray()))), false);
        var mapRead = pattern.Graph.EvaluateMap(pattern.Graph, sitesMap, S(edges.ToArray()), S(chords.ToArray()));
        return new(new Relation(header, new Relation(S(checks.ToArray()), mapRead)), pattern.Graph.TryReadMapPassed(pattern.Graph, mapRead));
    }

    public Deformation Deform(Pattern pattern, QualitativePatternTransform transform)
    {
        var bindings = new List<IElement>(); var siteRows = new List<IElement>(); var strokes = new List<IElement>();
        var strokeRows = new List<IElement>(); var chordRows = new List<IElement>(); var reads = new List<IElement>();
        var endpointChecks = new List<IElement>(); var pending = new List<IElement>();
        foreach (var binding in E(pattern.Bindings).Cast<Relation>())
        {
            var read = transform.Apply((Relation)binding.Right);
            transform.TryReadResult(read, out var position, out _, out _, out _);
            if (!transform.TryReadComplete(read, out _)) pending.Add(read);
            IElement site = ReferenceEquals(binding.Left, pattern.Graph.Frame.Origin) ? transform.Target.Origin : new Placeholder();
            bindings.Add(new Relation(site, position)); siteRows.Add(new Relation(binding.Left, site)); reads.Add(read);
        }
        foreach (var edge in E(pattern.Graph.Strokes).Cast<Relation>())
        {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out var chord);
            var read = transform.Apply(chord);
            transform.TryReadResult(read, out var output, out _, out _, out _);
            if (!transform.TryReadComplete(read, out _)) pending.Add(read);
            var a = siteRows.Cast<Relation>().Single(row => ReferenceEquals(row.Left, from)); var b = siteRows.Cast<Relation>().Single(row => ReferenceEquals(row.Left, to));
            var emitted = QualitativeStrokeGraph.Stroke(new Relation(edge, read), a.Right, b.Right, output);
            strokes.Add(emitted); strokeRows.Add(new Relation(edge, emitted)); chordRows.Add(new Relation(chord, output)); reads.Add(read);
            var before = bindings.Cast<Relation>().Single(binding => ReferenceEquals(binding.Left, a.Right));
            var after = bindings.Cast<Relation>().Single(binding => ReferenceEquals(binding.Left, b.Right));
            var addition = transform.Target.Reduce((Relation)before.Right, output);
            transform.Target.TryReadResult(addition, out var reached, out _, out _);
            var inverse = InverseIn(transform.Target, (Relation)after.Right);
            var comparison = transform.Target.Reduce(reached, inverse);
            var check = new Relation(emitted, new Relation(new Relation(before, after), new Relation(addition, comparison)));
            endpointChecks.Add(check);
            if (!transform.Target.TryReadResult(comparison, out var residue, out _, out var unknown) || E(residue).Length != 0 || E(unknown).Length != 0) pending.Add(check);
        }
        var target = new QualitativeStrokeGraph(transform.Target, S(bindings.Cast<Relation>().Select(row => row.Left).ToArray()),
            pending.Count == 0 ? S(strokes.ToArray()) : StructuralConstraint.Open(strokes.ToArray()));
        var sitesMap = S(siteRows.ToArray()); var strokesMap = S(strokeRows.ToArray()); var chordsMap = S(chordRows.ToArray());
        var mapRead = pattern.Graph.EvaluateMap(target, sitesMap, strokesMap, chordsMap);
        var surface = new Relation(pattern.Surface, new Relation(transform.Surface,
            new Relation(S(reads.ToArray()), new Relation(target.Surface, new Relation(mapRead,
                new Relation(S(bindings.ToArray()), new Relation(S(endpointChecks.ToArray()), S(pending.ToArray()))))))));
        return new(pattern, transform, target, S(bindings.ToArray()), sitesMap, strokesMap, chordsMap, mapRead,
            S(endpointChecks.ToArray()), S(pending.ToArray()), surface);
    }

    public Relation Inverse(Relation word)
        => InverseIn(Frame, word);
    private static Relation InverseIn(QualitativeMotionFrame frame, Relation word)
    {
        if (!StructuralConstraint.TryReadScope(word, out var entries, out var seam, out var tail)) return StructuralConstraint.Open(word);
        var inverse = new List<IElement>(); var complete = seam.Equals(tail);
        foreach (var item in entries.Reverse())
        {
            if (item is not Relation move || !frame.Direction(move, out _, out _, out _)) { complete = false; inverse.Add(item); continue; }
            var opposite = E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right;
            inverse.Add(QualitativeMotionFrame.Move(opposite, move.Right));
        }
        return complete ? S(inverse.ToArray()) : StructuralConstraint.Open(inverse.ToArray());
    }
    private Relation Position(int x, int y) => S([.. E(Word(x < 0 ? Frame.Left : Frame.Right, Math.Abs(x))), .. E(Word(y < 0 ? Frame.Down : Frame.Up, Math.Abs(y)))]);
    private Relation Move(IElement direction) => QualitativeMotionFrame.Move(direction, Reach);
    private Relation Word(IElement direction, int repeats) => S(Enumerable.Range(0, repeats).Select(_ => (IElement)Move(direction)).ToArray());
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
