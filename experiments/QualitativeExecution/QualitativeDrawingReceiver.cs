using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>A local drawing actuator over unmeasured chord participation.
/// Questions and finite choices belong upstream. This receiver executes one
/// definite offered action; it never resolves alternatives or samples for them.
/// Historical branches are independent ledgers. TryCommit separately guards
/// the convenience receiver's chosen current state.</summary>
public sealed class QualitativeDrawingReceiver
{
    // cursor/anchor binding = actual-site -> closed formal displacement
    // draw = (cursor-port -> cursor-port) -> (chord -> stroke-port)
    // jump = (cursor-port -> cursor-port) -> (chord -> Closed())
    // reset = (anchor-port -> cursor-port) -> Closed()
    // reanchor = (cursor-port -> anchor-port) -> Closed()
    // mirror = orientation-port -> total direction correspondence
    // direction-action-port = orientation-port -> actual frame opposite pairs
    // direction-action = direction-action-port -> total opposite-preserving map
    // clause = selected-variables -> actual action pattern
    // Surface = (receiver -> frame.Surface) -> (ports -> clauses)
    // state = (Surface -> (source -> event)) ->
    //   ((cursor -> anchor) -> ((orientation -> site-bindings) ->
    //                         ((strokes -> event-history) -> provenance)))
    // offer = ((source -> receiver) -> (event -> predecessor)) ->
    //         (Surface -> (action-support -> cause))
    // receipt = ((before -> offer) -> Surface) ->
    //   ((after -> accepted-offers) -> (blocked -> (unresolved -> proofs)))
    // stroke = actual-offer -> ((from-site -> to-site) -> mapped-chord)
    // proof = action -> (clause-read -> (direction-projection -> position-read))
    // Movement gives a fresh site, including a zero coordinate return. Reset
    // explicitly reuses the anchor site. Coordinate equality never fuses sites.

    public QualitativeMotionFrame Frame { get; }
    public IElement Source { get; }
    public IElement Receiver { get; } = new Placeholder();
    public Relation CursorPort { get; }
    public Relation AnchorPort { get; }
    public Relation OrientationPort { get; }
    public Relation DirectionActionPort { get; }
    public Relation StrokePort { get; }
    public Relation Clauses { get; }
    public Relation Surface { get; }
    public Relation IdentityOrientation { get; }
    /// <summary>Exchanges this frame's horizontal faces, keeping vertical faces.</summary>
    public Relation HorizontalMirror { get; }
    /// <summary>Exchanges this frame's vertical faces, keeping horizontal faces.</summary>
    public Relation VerticalMirror { get; }
    public Relation State { get; private set; }
    public Relation Sites { get { ReadCurrent(out _, out _, out _, out var sites, out _, out _); return sites; } }
    public Relation SiteBindings => Sites;
    public Relation Strokes { get { ReadCurrent(out _, out _, out _, out _, out var strokes, out _); return strokes; } }
    public Relation History { get { ReadCurrent(out _, out _, out _, out _, out _, out var history); return history; } }
    public IElement CursorSite { get { ReadCurrent(out var cursor, out _, out _, out _, out _, out _); return cursor.Left; } }
    public Relation Position { get { ReadCurrent(out var cursor, out _, out _, out _, out _, out _); return (Relation)cursor.Right; } }
    public IElement AnchorSite { get { ReadCurrent(out _, out var anchor, out _, out _, out _, out _); return anchor.Left; } }
    public Relation Orientation { get { ReadCurrent(out _, out _, out var orientation, out _, out _, out _); return orientation; } }

    private readonly Relation noStroke = S();
    private readonly Relation movementRoute;
    private readonly Relation reset;
    private readonly Relation anchor;

    public QualitativeDrawingReceiver(QualitativeMotionFrame frame, IElement? source = null,
        IElement? initialEvent = null, IElement? initialSite = null)
    {
        Frame = frame; Source = source ?? new Placeholder();
        CursorPort = new Relation(Receiver, new Relation(new Placeholder(), new Placeholder()));
        AnchorPort = new Relation(Receiver, new Relation(new Placeholder(), new Placeholder()));
        var directions = S(Frame.Right, Frame.Left, Frame.Up, Frame.Down);
        OrientationPort = new Relation(Receiver, new Relation(directions, directions));
        DirectionActionPort = new Relation(OrientationPort, Frame.Opposites);
        StrokePort = new Relation(CursorPort, new Relation(new Placeholder(), new Placeholder()));
        movementRoute = new Relation(CursorPort, CursorPort);
        IdentityOrientation = S(new Relation(Frame.Right, Frame.Right), new Relation(Frame.Left, Frame.Left),
            new Relation(Frame.Up, Frame.Up), new Relation(Frame.Down, Frame.Down));
        HorizontalMirror = S(new Relation(Frame.Right, Frame.Left), new Relation(Frame.Left, Frame.Right),
            new Relation(Frame.Up, Frame.Up), new Relation(Frame.Down, Frame.Down));
        VerticalMirror = S(new Relation(Frame.Right, Frame.Right), new Relation(Frame.Left, Frame.Left),
            new Relation(Frame.Up, Frame.Down), new Relation(Frame.Down, Frame.Up));
        reset = new Relation(new Relation(AnchorPort, CursorPort), noStroke);
        anchor = new Relation(new Relation(CursorPort, AnchorPort), noStroke);
        var word = new Placeholder(); var map = new Placeholder();
        Clauses = S(new Relation(S(word), new Relation(movementRoute, new Relation(word, StrokePort))),
            new Relation(S(word), new Relation(movementRoute, new Relation(word, noStroke))),
            new Relation(S(), reset), new Relation(S(), anchor), new Relation(S(map), new Relation(OrientationPort, map)),
            new Relation(S(map), new Relation(DirectionActionPort, map)));
        Surface = new Relation(new Relation(Receiver, Frame.Surface),
            new Relation(S(CursorPort, AnchorPort, OrientationPort, StrokePort, DirectionActionPort), Clauses));
        State = Initial(initialEvent ?? new Placeholder(), initialSite);
    }

    public Relation Draw(Relation chord) => new(movementRoute, new Relation(chord, StrokePort));
    public Relation Jump(Relation chord) => new(movementRoute, new Relation(chord, noStroke));
    public Relation Reset() => reset;
    public Relation Anchor() => anchor;
    public Relation Reanchor() => anchor;
    /// <summary>Composes an explicit axis-face correspondence with the current
    /// output orientation. It does not reflect existing sites or stroke history.</summary>
    public Relation Mirror(Relation directionMap) => new(OrientationPort, directionMap);

    /// <summary>Composes a total, opposite-preserving direction correspondence
    /// with future output. Axis exchange is permitted; existing sites and
    /// strokes keep their original frame and history.</summary>
    public Relation DirectionAction(Relation directionMap) => new(DirectionActionPort, directionMap);

    public bool TryReadDraw(IElement action, out Relation chord) => ReadMove(action, StrokePort, out chord);
    public bool TryReadJump(IElement action, out Relation chord) => ReadMove(action, noStroke, out chord);
    public bool TryReadMirror(IElement action, out Relation directionMap)
    {
        directionMap = null!;
        if (action is not Relation { Right: Relation map } row || !ReferenceEquals(row.Left, OrientationPort)) return false;
        directionMap = map; return true;
    }
    public bool TryReadDirectionAction(IElement action, out Relation directionMap)
    {
        directionMap = null!;
        if (action is not Relation { Right: Relation map } row || !ReferenceEquals(row.Left, DirectionActionPort)) return false;
        directionMap = map; return true;
    }
    public bool IsReset(IElement action) => ReferenceEquals(action, reset);
    public bool IsAnchor(IElement action) => ReferenceEquals(action, anchor);

    public Relation Initial(IElement eventIdentity, IElement? site = null)
    {
        var binding = new Relation(site ?? new Placeholder(), S());
        return MakeState(eventIdentity, binding, binding, IdentityOrientation, S(binding), S(), S(eventIdentity), S());
    }

    public Relation Offer(IElement source, IElement eventIdentity, IElement priorEvent,
        Relation actionSupport, IElement? cause = null) =>
        new(new Relation(new Relation(source, Receiver), new Relation(eventIdentity, priorEvent)),
            new Relation(Surface, new Relation(actionSupport, cause ?? S())));

    public Relation Apply(IElement eventIdentity, Relation closedActionSupport, IElement cause, IElement? priorEvent = null)
    {
        TryReadState(State, out var previous, out _, out _, out _, out _, out _, out _);
        var receipt = Step(State, Offer(Source, eventIdentity, priorEvent ?? previous, closedActionSupport, cause));
        TryCommit(receipt);
        return receipt;
    }

    /// <summary>Commits an accepted local patch only while its exact source
    /// state is this receiver's current head. Historical acceptance remains a
    /// valid read but cannot overwrite later or independently branched state.</summary>
    public bool TryCommit(Relation receipt)
    {
        if (!TryReadStep(receipt, out var before, out _, out _, out _, out _, out _) ||
            !ReferenceEquals(before, State) || !TryReadAccepted(receipt, out var after, out _)) return false;
        State = after; return true;
    }

    public Relation Step(Relation state, Relation offer)
    {
        var after = state; var accepted = S(); var blocked = S(); var pending = S(); var proofs = S();
        if (!TryReadState(state, out var prior, out var cursor, out var oldAnchor, out var orientation, out var sites, out var strokes, out var history) ||
            !ReadOffer(offer, out var owner, out var occurrence, out var predecessor, out var support, out _))
        { pending = S(new Relation(state, offer)); return Receipt(); }
        if (!Same(owner, Source) || !Same(prior, predecessor) || E(history).Any(item => Same(item, occurrence)))
        { blocked = S(offer); return Receipt(); }
        if (!Closed(support) || E(support) is not [Relation action])
        { pending = S(new Relation(offer, support)); return Receipt(); }
        var clause = ReadClause(action);
        if (clause is null) { blocked = S(offer); return Receipt(); }
        var clauseRead = StructuralConstraint.Match(clause.Right, (Relation)clause.Left, S(action));
        if (!StructuralConstraint.TryReadUnique(clauseRead, out _, out _))
        { pending = S(clauseRead); return Receipt(); }
        var nextCursor = cursor; var nextAnchor = oldAnchor; var nextOrientation = orientation;
        var nextSites = sites; var nextStrokes = strokes; Relation projection = S(); Relation positionRead = S();
        if (TryReadDraw(action, out var draw) || TryReadJump(action, out draw))
        {
            if (!ValidWord(draw)) { pending = S(new Relation(action, Frame.Surface)); return Receipt(); }
            projection = Project(orientation, draw);
            var mapped = (Relation)((Relation)projection.Right).Left;
            positionRead = Frame.Reduce((Relation)cursor.Right, mapped);
            if (!Frame.TryReadResult(positionRead, out var position, out _, out _) || !Closed(position))
            { pending = S(positionRead); return Receipt(); }
            nextCursor = new Relation(new Placeholder(), position);
            nextSites = S([.. E(sites), nextCursor]);
            if (TryReadDraw(action, out _))
            {
                var stroke = new Relation(offer, new Relation(new Relation(cursor.Left, nextCursor.Left), mapped));
                nextStrokes = S([.. E(strokes), stroke]);
            }
        }
        else if (IsReset(action)) nextCursor = oldAnchor;
        else if (IsAnchor(action)) nextAnchor = cursor;
        else if (TryReadMirror(action, out var map))
        {
            if (!ValidMirror(map)) { pending = S(new Relation(action, Frame.Opposites)); return Receipt(); }
            nextOrientation = Compose(orientation, map);
            projection = new Relation(new Relation(orientation, map), nextOrientation);
        }
        else if (TryReadDirectionAction(action, out map))
        {
            if (!TryDirectionMap(map, out var pairs)) { pending = S(new Relation(action, Frame.Opposites)); return Receipt(); }
            nextOrientation = Compose(orientation, map);
            projection = new Relation(new Relation(orientation, map), new Relation(nextOrientation, pairs));
        }
        else { blocked = S(offer); return Receipt(); }
        var proof = new Relation(action, new Relation(clauseRead, new Relation(projection, positionRead)));
        after = MakeState(occurrence, nextCursor, nextAnchor, nextOrientation, nextSites, nextStrokes,
            S([.. E(history), occurrence]), new Relation(state, new Relation(offer, proof)));
        accepted = S(offer); proofs = S(proof); return Receipt();

        Relation Receipt() => new(new Relation(new Relation(state, offer), Surface), new Relation(new Relation(after, accepted),
            new Relation(blocked, new Relation(pending, proofs))));
    }

    public bool TryReadState(Relation state, out IElement eventIdentity, out Relation cursor, out Relation anchorBinding,
        out Relation orientation, out Relation sites, out Relation strokes, out Relation history)
    {
        eventIdentity = null!; cursor = anchorBinding = orientation = sites = strokes = history = null!;
        if (state.Left is not Relation { Right: Relation owner } context || !ReferenceEquals(context.Left, Surface) || !Same(owner.Left, Source) ||
            state.Right is not Relation { Left: Relation positions, Right: Relation body } ||
            positions.Left is not Relation current || positions.Right is not Relation mark ||
            body.Left is not Relation { Left: Relation frame, Right: Relation bindings } ||
            body.Right is not Relation { Left: Relation ledger } || ledger.Left is not Relation ink || ledger.Right is not Relation events ||
            !ValidOrientation(frame) || !Closed(bindings) || !Closed(ink) || !Closed(events) || E(events).Length == 0 ||
            !Same(E(events)[^1], owner.Right) || !Unique(E(events)) ||
            E(bindings).Any(item => item is not Relation binding || binding.Right is not Relation position || !ValidWord(position)) ||
            !Unique(E(bindings).Cast<Relation>().Select(binding => binding.Left).ToArray()) ||
            !E(bindings).Any(item => ReferenceEquals(item, current)) || !E(bindings).Any(item => ReferenceEquals(item, mark))) return false;
        foreach (var stroke in E(ink))
            if (stroke is not Relation row || !TryReadStroke(row, out var cause, out var from, out var to, out var chord) || !ValidWord(chord) ||
                cause is not Relation strokeOffer || !ReadOffer(strokeOffer, out var source, out var occurrence, out _, out _, out _) ||
                !Same(source, Source) || !E(events).Any(item => Same(item, occurrence)) ||
                !E(bindings).Cast<Relation>().Any(binding => Same(binding.Left, from)) ||
                !E(bindings).Cast<Relation>().Any(binding => Same(binding.Left, to))) return false;
        eventIdentity = owner.Right; cursor = current; anchorBinding = mark; orientation = frame; sites = bindings; strokes = ink; history = events;
        return true;
    }

    public bool TryReadStep(Relation receipt, out Relation before, out Relation offer, out Relation after,
        out Relation accepted, out Relation blocked, out Relation unresolved)
    {
        before = offer = after = accepted = blocked = unresolved = null!;
        if (receipt.Left is not Relation { Left: Relation premise } context || !ReferenceEquals(context.Right, Surface) ||
            premise.Left is not Relation start || premise.Right is not Relation request ||
            receipt.Right is not Relation { Left: Relation result, Right: Relation rest } ||
            result.Left is not Relation end || result.Right is not Relation admitted || rest.Left is not Relation refused ||
            rest.Right is not Relation { Left: Relation pending, Right: Relation proofs } ||
            !Closed(admitted) || !Closed(refused) || !Closed(pending) || !Closed(proofs)) return false;
        before = start; offer = request; after = end; accepted = admitted; blocked = refused; unresolved = pending; return true;
    }

    /// <summary>Checks local ownership, fresh occurrence, predecessor, accepted
    /// action clause, exact state patch and projection accounting. Historical
    /// ledger fabrication is not a global authentication service.</summary>
    public bool TryReadAccepted(Relation receipt, out Relation after, out Relation addedStrokes)
    {
        after = addedStrokes = null!;
        if (!TryReadStep(receipt, out var before, out var offer, out var current, out var accepted, out var blocked, out var pending) ||
            E(accepted) is not [var admitted] || !ReferenceEquals(admitted, offer) || E(blocked).Length != 0 || E(pending).Length != 0 ||
            !TryReadState(before, out var prior, out var cursor, out var mark, out var orientation, out var sites, out var strokes, out var history) ||
            !TryReadState(current, out var occurrence, out var next, out var nextMark, out var nextOrientation, out var nextSites, out var nextStrokes, out var nextHistory) ||
            !ReadOffer(offer, out var owner, out var offeredEvent, out var predecessor, out var support, out _) ||
            !Same(owner, Source) || !Same(prior, predecessor) || !Same(occurrence, offeredEvent) || E(history).Any(item => Same(item, occurrence)) ||
            !Appended(history, nextHistory, occurrence) || !Closed(support) || E(support) is not [Relation action] ||
            current.Right is not Relation { Right: Relation { Right: Relation { Right: Relation provenance } } } ||
            !ReferenceEquals(provenance.Left, before) || provenance.Right is not Relation operation || !ReferenceEquals(operation.Left, offer) ||
            operation.Right is not Relation proof || !ReferenceEquals(proof.Left, action) ||
            receipt.Right is not Relation { Right: Relation { Right: Relation { Right: Relation proofScope } } } ||
            E(proofScope) is not [var actualProof] || !ReferenceEquals(actualProof, proof) ||
            proof.Right is not Relation { Left: Relation clauseRead, Right: Relation effect } ||
            effect.Left is not Relation projection || effect.Right is not Relation reduction || !ValidClauseRead(action, clauseRead)) return false;
        var extra = new List<IElement>();
        if (TryReadDraw(action, out var chord) || TryReadJump(action, out chord))
        {
            if (!ValidWord(chord) || !ValidProjection(projection, orientation, chord, out var mapped) ||
                !Frame.TryReadResult(reduction, out var position, out _, out _) ||
                reduction.Left is not Relation { Right: Relation operands } || !ReferenceEquals(operands.Left, cursor.Right) ||
                !ReferenceEquals(operands.Right, mapped) || !ReferenceEquals(position, next.Right) ||
                !ReferenceEquals(mark, nextMark) || !ReferenceEquals(orientation, nextOrientation) ||
                E(sites).Cast<Relation>().Any(binding => Same(binding.Left, next.Left)) || !Appended(sites, nextSites, next)) return false;
            if (TryReadDraw(action, out _))
            {
                if (E(nextStrokes).Length != E(strokes).Length + 1 || E(nextStrokes)[^1] is not Relation stroke ||
                    !TryReadStroke(stroke, out var cause, out var from, out var to, out var word) || !ReferenceEquals(cause, offer) ||
                    !Same(from, cursor.Left) || !Same(to, next.Left) || !ReferenceEquals(word, mapped) || !Appended(strokes, nextStrokes, stroke)) return false;
                extra.Add(stroke);
            }
            else if (!ReferenceEquals(strokes, nextStrokes)) return false;
        }
        else
        {
            if (!ReferenceEquals(sites, nextSites) || !ReferenceEquals(strokes, nextStrokes) || !Closed(reduction) || E(reduction).Length != 0) return false;
            if (IsReset(action))
            { if (!ReferenceEquals(next, mark) || !ReferenceEquals(nextMark, mark) || !ReferenceEquals(orientation, nextOrientation) || !Closed(projection) || E(projection).Length != 0) return false; }
            else if (IsAnchor(action))
            { if (!ReferenceEquals(next, cursor) || !ReferenceEquals(nextMark, cursor) || !ReferenceEquals(orientation, nextOrientation) || !Closed(projection) || E(projection).Length != 0) return false; }
            else if (TryReadMirror(action, out var map))
            {
                if (!ValidMirror(map) || !ReferenceEquals(next, cursor) || !ReferenceEquals(nextMark, mark) ||
                    projection.Left is not Relation sources || !ReferenceEquals(sources.Left, orientation) || !ReferenceEquals(sources.Right, map) ||
                    !ReferenceEquals(projection.Right, nextOrientation) || !OrientationEqual(Compose(orientation, map), nextOrientation)) return false;
            }
            else if (TryReadDirectionAction(action, out map))
            {
                if (!TryDirectionMap(map, out var pairs) || !ReferenceEquals(next, cursor) || !ReferenceEquals(nextMark, mark) ||
                    projection.Left is not Relation sources || !ReferenceEquals(sources.Left, orientation) || !ReferenceEquals(sources.Right, map) ||
                    projection.Right is not Relation { Left: Relation result, Right: Relation correspondence } ||
                    !ReferenceEquals(result, nextOrientation) || !OrientationEqual(Compose(orientation, map), nextOrientation) ||
                    !SameDirectionPairs(correspondence, pairs)) return false;
            }
            else return false;
        }
        after = current; addedStrokes = S(extra.ToArray()); return true;
    }

    public static bool TryReadStroke(Relation row, out IElement cause, out IElement from, out IElement to, out Relation chord)
    {
        cause = from = to = null!; chord = null!;
        if (row.Right is not Relation { Left: Relation ends, Right: Relation word } ||
            !StructuralConstraint.TryReadScope(word, out _, out _, out _)) return false;
        cause = row.Left; from = ends.Left; to = ends.Right; chord = word; return true;
    }

    public bool TryReadBinding(Relation binding, out IElement site, out Relation position)
    {
        site = null!; position = null!;
        if (binding.Right is not Relation word || !ValidWord(word)) return false;
        site = binding.Left; position = word; return true;
    }

    private Relation Project(Relation orientation, Relation chord)
    {
        var mapped = new List<IElement>(); var bindings = new List<IElement>();
        foreach (var item in E(chord).Cast<Relation>())
        {
            var direction = E(orientation).Cast<Relation>().Single(row => ReferenceEquals(row.Left, item.Left)).Right;
            var result = ReferenceEquals(direction, item.Left) ? item : QualitativeMotionFrame.Move(direction, item.Right);
            mapped.Add(result); bindings.Add(new Relation(item, result));
        }
        return new Relation(new Relation(orientation, chord), new Relation(S(mapped.ToArray()), S(bindings.ToArray())));
    }

    private bool ValidProjection(Relation read, Relation orientation, Relation chord, out Relation mapped)
    {
        mapped = null!;
        if (read.Left is not Relation inputs || !ReferenceEquals(inputs.Left, orientation) || !ReferenceEquals(inputs.Right, chord) ||
            read.Right is not Relation { Left: Relation result, Right: Relation bindings } || !ValidWord(result) || !Closed(bindings) ||
            E(chord).Length != E(result).Length || E(chord).Length != E(bindings).Length) return false;
        var before = E(chord); var after = E(result); var rows = E(bindings);
        for (var i = 0; i < before.Length; i++)
        {
            if (before[i] is not Relation a || after[i] is not Relation b || rows[i] is not Relation pair ||
                !ReferenceEquals(pair.Left, a) || !ReferenceEquals(pair.Right, b) || !Same(a.Right, b.Right)) return false;
            var direction = E(orientation).Cast<Relation>().Single(row => ReferenceEquals(row.Left, a.Left)).Right;
            if (!ReferenceEquals(b.Left, direction) || ReferenceEquals(direction, a.Left) && !ReferenceEquals(a, b)) return false;
        }
        mapped = result; return true;
    }

    private Relation Compose(Relation before, Relation incoming) => S(E(before).Cast<Relation>().Select(row =>
        (IElement)new Relation(row.Left, E(incoming).Cast<Relation>().Single(next => ReferenceEquals(next.Left, row.Right)).Right)).ToArray());

    private bool ValidOrientation(Relation map) => TryDirectionMap(map, out _);

    private bool TryDirectionMap(Relation map, out Relation correspondence)
    {
        correspondence = null!;
        if (!Closed(map) || E(map).Length != 4 || E(map).Any(item => item is not Relation)) return false;
        var rows = E(map).Cast<Relation>().ToArray(); var directions = new IElement[] { Frame.Right, Frame.Left, Frame.Up, Frame.Down };
        if (!Unique(rows.Select(row => row.Left).ToArray()) || !Unique(rows.Select(row => row.Right).ToArray()) ||
            rows.Any(row => !directions.Any(direction => ReferenceEquals(direction, row.Left)) || !directions.Any(direction => ReferenceEquals(direction, row.Right)))) return false;
        if (!Closed(Frame.Opposites) || E(Frame.Opposites).Length != directions.Length ||
            E(Frame.Opposites).Any(item => item is not Relation)) return false;
        var pairs = E(Frame.Opposites).Cast<Relation>().ToArray();
        if (!Unique(pairs.Select(pair => pair.Left).ToArray()) || !Unique(pairs.Select(pair => pair.Right).ToArray()) ||
            pairs.Any(pair => ReferenceEquals(pair.Left, pair.Right) ||
                !directions.Any(direction => ReferenceEquals(direction, pair.Left)) ||
                !directions.Any(direction => ReferenceEquals(direction, pair.Right)) ||
                !pairs.Any(reverse => ReferenceEquals(reverse.Left, pair.Right) && ReferenceEquals(reverse.Right, pair.Left)))) return false;
        var evidence = new List<IElement>();
        foreach (var pair in pairs)
        {
            var first = rows.Single(row => ReferenceEquals(row.Left, pair.Left));
            var second = rows.Single(row => ReferenceEquals(row.Left, pair.Right));
            var target = pairs.SingleOrDefault(other => ReferenceEquals(other.Left, first.Right) && ReferenceEquals(other.Right, second.Right));
            if (target is null) return false;
            evidence.Add(new Relation(new Relation(pair, target), new Relation(first, second)));
        }
        correspondence = S(evidence.ToArray()); return true;
    }

    private bool ValidMirror(Relation map)
    {
        if (!ValidOrientation(map)) return false;
        var reach = new Placeholder();
        foreach (var row in E(map).Cast<Relation>())
        {
            Frame.Direction(QualitativeMotionFrame.Move(row.Left, reach), out var source, out _, out _);
            Frame.Direction(QualitativeMotionFrame.Move(row.Right, reach), out var target, out _, out _);
            if (!ReferenceEquals(source, target)) return false;
        }
        return true;
    }
    private static bool SameDirectionPairs(Relation actual, Relation expected) => Closed(actual) &&
        E(actual).Length == E(expected).Length && E(actual).Zip(E(expected)).All(pair =>
            pair.First is Relation { Left: Relation a, Right: Relation b } && pair.Second is Relation { Left: Relation x, Right: Relation y } &&
            ReferenceEquals(a.Left, x.Left) && ReferenceEquals(a.Right, x.Right) &&
            ReferenceEquals(b.Left, y.Left) && ReferenceEquals(b.Right, y.Right));
    private static bool OrientationEqual(Relation a, Relation b) => E(a).Length == E(b).Length && E(a).Cast<Relation>().All(row =>
        E(b).OfType<Relation>().Any(other => ReferenceEquals(other.Left, row.Left) && ReferenceEquals(other.Right, row.Right)));
    private bool ValidWord(Relation word) => Closed(word) && E(word).All(item => item is Relation move && Frame.Direction(move, out _, out _, out _));
    private bool ReadMove(IElement action, IElement outlet, out Relation chord)
    {
        chord = null!;
        if (action is not Relation { Left: Relation route, Right: Relation payload } || !ReferenceEquals(route.Left, CursorPort) ||
            !ReferenceEquals(route.Right, CursorPort) || !ReferenceEquals(payload.Right, outlet) || payload.Left is not Relation word) return false;
        chord = word; return true;
    }
    private Relation? ReadClause(IElement action)
    {
        var index = TryReadDraw(action, out _) ? 0 : TryReadJump(action, out _) ? 1 : IsReset(action) ? 2 : IsAnchor(action) ? 3 : TryReadMirror(action, out _) ? 4 : TryReadDirectionAction(action, out _) ? 5 : -1;
        return index < 0 ? null : (Relation)E(Clauses)[index];
    }
    private bool ValidClauseRead(Relation action, Relation read)
    {
        var clause = ReadClause(action);
        return clause is not null && StructuralConstraint.TryReadUnique(read, out _, out _) &&
            StructuralConstraint.TryRead(read, out var request, out _, out _, out _) &&
            request.Left is Relation { Left: Relation definition, Right: Relation initial } &&
            ReferenceEquals(definition.Left, clause.Right) && ReferenceEquals(definition.Right, clause.Left) && Closed(initial) && E(initial).Length == 0 &&
            request.Right is Relation support && Closed(support) && E(support) is [var selected] && ReferenceEquals(selected, action);
    }
    private bool ReadOffer(Relation offer, out IElement source, out IElement occurrence, out IElement prior, out Relation support, out IElement cause)
    {
        source = occurrence = prior = cause = null!; support = null!;
        if (offer.Left is not Relation { Left: Relation owners, Right: Relation events } || !Same(owners.Right, Receiver) ||
            offer.Right is not Relation body || !ReferenceEquals(body.Left, Surface) || body.Right is not Relation { Left: Relation actions } request ||
            !StructuralConstraint.TryReadScope(actions, out _, out _, out _)) return false;
        source = owners.Left; occurrence = events.Left; prior = events.Right; support = actions; cause = request.Right; return true;
    }
    private Relation MakeState(IElement occurrence, Relation cursor, Relation mark, Relation orientation, Relation sites, Relation strokes, Relation history, IElement proof) =>
        new(new Relation(Surface, new Relation(Source, occurrence)), new Relation(new Relation(cursor, mark),
            new Relation(new Relation(orientation, sites), new Relation(new Relation(strokes, history), proof))));
    private void ReadCurrent(out Relation cursor, out Relation mark, out Relation orientation, out Relation sites, out Relation strokes, out Relation history)
    {
        if (!TryReadState(State, out _, out cursor, out mark, out orientation, out sites, out strokes, out history))
            throw new InvalidOperationException("The current drawing ledger must retain its native state contract.");
    }
    private static bool Appended(Relation before, Relation after, IElement last) => E(after).Length == E(before).Length + 1 &&
        E(before).Zip(E(after)).All(pair => ReferenceEquals(pair.First, pair.Second)) && Same(E(after)[^1], last);
    private static bool Unique(IElement[] items) => items.Select((item, i) => items.Take(i).Any(other => Same(item, other))).All(duplicate => !duplicate);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
