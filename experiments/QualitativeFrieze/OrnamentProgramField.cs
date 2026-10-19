using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A companion, window-independent seam contract over an ornament
/// program. Rules select actual motif/transform identities and motif-local
/// public roles. A signed offset walks the sampled native slot order; it does
/// not inspect display coordinates or infer contact from coincident ink.</summary>
public sealed class OrnamentProgramField
{
    // contract = (program -> (column cycle -> row cycle)) -> Closed(rules)
    // rule = ((from motif -> from transform) -> (to motif -> to transform)) ->
    //        (Closed(signed axis steps) -> (from public role -> to public role))
    // positive step = program -> actual phase cycle; negative = reverse(step)
    // walk = ((actual rule -> source application) -> actual offset) ->
    //        (Closed(actual slot transitions) -> target application)
    // admitted join = (unit-quotient site -> unit-quotient site) ->
    //                 (actual current agreement -> actual site-map rows)
    public OrnamentProgramRun Run { get; }
    public Relation SeamContract { get; }
    public QualitativeStrokeGraph SourceGraph { get; }
    public Relation SourceBindings { get; }
    public QualitativeGraphJoin Join { get; }
    public QualitativeStrokeGraph Graph => Join.Graph;
    public Relation Bindings => Join.Bindings;
    public Relation Accepted => Join.Accepted;
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    /// <summary>Requested walks leaving the chosen finite sample. These are
    /// boundary obligations, not failed interior connections or all endpoints.</summary>
    public Relation Frontier { get; }
    /// <summary>Observed neighbours whose selected target identity does not
    /// activate this conditional motif/transform rule.</summary>
    public Relation Inactive { get; }
    public Relation Adjacencies { get; }
    public Relation SeamReads { get; }
    public Relation Validation { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public static Relation Contract(OrnamentProgram program, params Relation[] rules)
    {
        var axes = (Relation)program.Layout.Left;
        return new(new Relation(program.Surface, new Relation(axes.Left, axes.Right)), N.S(rules));
    }
    public static Relation Contract(OrnamentProgramRun run, params Relation[] rules) => Contract(run.Program, rules);

    /// <summary>Authoring shorthand for repeated participation in the two
    /// actual cycle directions. Integers only choose how many native steps to
    /// construct; no measured position or distance enters the contract.</summary>
    public static Relation Offset(OrnamentProgram program, int columns, int rows)
    {
        var axes = (Relation)program.Layout.Left;
        var u = new Relation(program.Surface, axes.Left); var v = new Relation(program.Surface, axes.Right);
        return N.S(Enumerable.Repeat<IElement>(columns < 0 ? u.Reverse() : u, Math.Abs(columns))
            .Concat(Enumerable.Repeat<IElement>(rows < 0 ? v.Reverse() : v, Math.Abs(rows))).ToArray());
    }
    public static Relation Rule(Relation fromMotif, Relation fromTransform, Relation toMotif, Relation toTransform,
        Relation offset, IElement fromRole, IElement toRole) =>
        new(new Relation(new Relation(fromMotif, fromTransform), new Relation(toMotif, toTransform)),
            new Relation(offset, new Relation(fromRole, toRole)));

    public OrnamentProgramField(OrnamentProgramRun run, Relation seamContract)
    {
        Run = run; SeamContract = seamContract;
        var blocked = new List<IElement>(); var pending = new List<IElement>(); var frontier = new List<IElement>();
        var inactive = new List<IElement>(); var adjacency = new List<IElement>(); var reads = new List<IElement>(); var declarations = new List<IElement>(); var validation = new List<IElement>();
        var sites = new List<IElement>(); var strokes = new List<IElement>(); var bindings = new List<IElement>();
        var units = run.Units.Distinct<UnitArrangementNode>(ReferenceEqualityComparer.Instance).ToArray();
        if (units.Length != run.Units.Count) blocked.Add(run.Surface);
        foreach (var unit in units)
        {
            var local = unit.Current.Join;
            sites.AddRange(N.E(local.Graph.Sites)); strokes.AddRange(N.E(local.Graph.Strokes)); bindings.AddRange(N.E(local.Bindings));
            if (!ReferenceEquals(local.Graph.Frame, run.Program.Frame) || !local.TryReadPassed(local.Evaluate())) pending.Add(unit.Current.Surface);
        }
        SourceGraph = new(run.Program.Frame, N.S(sites.ToArray()), N.S(strokes.ToArray())); SourceBindings = N.S(bindings.ToArray());
        if (!run.IsComplete) pending.Add(run.Surface);
        var sourceReady = run.Surface is { Left: Relation { Left: var nativeProgram, Right: Relation { Left: var nativeBindings, Right: var nativeWindow } },
            Right: Relation { Right: Relation { Left: Relation nativeApplications, Right: Relation { Right: Relation { Left: Relation nativeBlocked, Right: Relation nativePending } } } } } &&
            ReferenceEquals(nativeProgram, run.Program.Surface) && ReferenceEquals(nativeBindings, run.Bindings) && ReferenceEquals(nativeWindow, run.Window) &&
            SameEntries(nativeApplications, run.Applications) && SameEntries(nativeBlocked, run.Blocked) && SameEntries(nativePending, run.Unresolved);
        if (!sourceReady) blocked.Add(new Relation(run.Surface, new Relation(run.Program.Surface, new Relation(run.Bindings, new Relation(run.Window, run.Applications)))));
        var uCycle = new Relation(new Placeholder(), new Placeholder()); var vCycle = uCycle;
        var contextReady = run.Program.Layout.Left is Relation { Left: Relation, Right: Relation };
        if (contextReady)
        {
            var cycles = (Relation)run.Program.Layout.Left; uCycle = (Relation)cycles.Left; vCycle = (Relation)cycles.Right;
            if (ReferenceEquals(uCycle, vCycle)) { blocked.Add(cycles); contextReady = false; }
        }
        else pending.Add(run.Program.Layout);
        Relation columnsScope = N.S(), rowsScope = N.S(); IElement[] columns = [], rows = [];
        var windowReady = run.Window is { Right: Relation { Left: Relation cs, Right: Relation rs } };
        if (windowReady)
        {
            var domains = (Relation)run.Window.Right; columnsScope = (Relation)domains.Left; rowsScope = (Relation)domains.Right;
            windowReady = Closed(columnsScope, out columns) & Closed(rowsScope, out rows);
            if (!windowReady) pending.Add(run.Window);
            if (!N.Distinct(columns.Concat(rows).ToArray()) || columns.Concat(rows).Any(slot => slot is not Placeholder))
            { blocked.Add(run.Window); windowReady = false; }
        }
        else pending.Add(run.Window);
        var occurrences = new Dictionary<(IElement Row, IElement Column), Occurrence>();
        var nativeRules = run.Program.Layout.Right is Relation { Right: Relation definitionRules } ? definitionRules : N.S();
        if (!Closed(run.Applications, out var applications)) pending.Add(run.Applications);
        foreach (var item in applications)
        {
            if (item is not Relation { Left: Relation { Left: Relation cell, Right: Relation rule },
                    Right: Relation { Left: Relation choices, Right: Relation { Left: Relation position, Right: Relation revision } } } application ||
                !rows.Any(slot => N.Same(slot, cell.Left)) || !columns.Any(slot => N.Same(slot, cell.Right)) ||
                !N.E(nativeRules).Any(actual => ReferenceEquals(actual, rule)) || rule.Right is not Relation parameterSlots ||
                !N.E(run.Bindings).OfType<Relation>().Any(binding => N.Same(binding.Left, parameterSlots.Left) && ReferenceEquals(binding.Right, choices.Left)) ||
                !N.E(run.Bindings).OfType<Relation>().Any(binding => N.Same(binding.Left, parameterSlots.Right) && ReferenceEquals(binding.Right, choices.Right)))
            { blocked.Add(item); continue; }
            if (!ExpectedCell(cell.Left, cell.Right, rule, position, out var placementRead))
            { blocked.Add(new Relation(application, placementRead)); continue; }
            validation.Add(new Relation(application, placementRead));
            var owners = units.Where(unit => N.E(unit.Revisions).Any(actual => ReferenceEquals(actual, revision))).ToArray();
            if (owners is not [var owner] || choices.Left is not Relation motif || choices.Right is not Relation transform ||
                !N.E(run.Program.Motifs).Any(actual => ReferenceEquals(actual, motif)) || !N.E(run.Program.Transforms).Any(actual => ReferenceEquals(actual, transform)))
            { blocked.Add(item); continue; }
            var fresh = ReferenceEquals(owner.Current.Surface, revision) && ReferenceEquals(owner.PlacementWord, position) &&
                MatchesProgram(motif, owner.Current.Program) && MatchesTransform(transform, owner.Transform);
            if (!fresh) pending.Add(new Relation(application, owner.Advertisement()));
            var occurrence = new Occurrence(application, owner, motif, transform, fresh);
            if (!occurrences.TryAdd((cell.Left, cell.Right), occurrence)) blocked.Add(application);
        }
        if (windowReady)
            foreach (var row in rows) foreach (var column in columns)
                if (!occurrences.ContainsKey((row, column))) pending.Add(new Relation(new Relation(row, column), run.Applications));
        IElement[] rules = [];
        if (seamContract is not { Left: Relation { Left: var sourceProgram, Right: Relation basis }, Right: Relation ruleScope } ||
            !ReferenceEquals(sourceProgram, run.Program.Surface) || !ReferenceEquals(basis.Left, uCycle) || !ReferenceEquals(basis.Right, vCycle))
        { blocked.Add(seamContract); contextReady = false; }
        else if (!Closed(ruleScope, out rules)) pending.Add(ruleScope);
        foreach (var item in rules)
        {
            if (item is not Relation { Left: Relation { Left: Relation from, Right: Relation to },
                    Right: Relation { Left: Relation offset, Right: Relation roles } } rule ||
                !N.E(run.Program.Motifs).Any(actual => ReferenceEquals(actual, from.Left)) ||
                !N.E(run.Program.Motifs).Any(actual => ReferenceEquals(actual, to.Left)) ||
                !N.E(run.Program.Transforms).Any(actual => ReferenceEquals(actual, from.Right)) ||
                !N.E(run.Program.Transforms).Any(actual => ReferenceEquals(actual, to.Right)))
            { blocked.Add(item); continue; }
            if (!Closed(offset, out var steps)) { pending.Add(new Relation(rule, offset)); continue; }
            if (steps.Any(step => !Direction(step, out _, out _))) { pending.Add(new Relation(rule, offset)); continue; }
            if (!sourceReady || !contextReady || !windowReady) continue;
            foreach (var entry in occurrences)
            {
                var first = entry.Value;
                if (!ReferenceEquals(first.Motif, from.Left) || !ReferenceEquals(first.Transform, from.Right)) continue;
                if (!first.Fresh) { pending.Add(new Relation(rule, first.Application)); continue; }
                var column = entry.Key.Column; var row = entry.Key.Row; var transitions = new List<IElement>(); var outside = false;
                foreach (var step in steps)
                {
                    Direction(step, out var horizontal, out var forward);
                    var domain = horizontal ? columns : rows; var current = horizontal ? column : row;
                    var index = Array.FindIndex(domain, actual => N.Same(actual, current)); var next = index + (forward ? 1 : -1);
                    if (next < 0 || next >= domain.Length)
                    { frontier.Add(new Relation(new Relation(rule, first.Application), new Relation(offset, new Relation(N.S(transitions.ToArray()), current)))); outside = true; break; }
                    transitions.Add(new Relation(new Relation(step, horizontal ? columnsScope : rowsScope), new Relation(current, domain[next])));
                    if (horizontal) column = domain[next]; else row = domain[next];
                }
                if (outside) continue;
                if (!occurrences.TryGetValue((row, column), out var second))
                { pending.Add(new Relation(rule, new Relation(row, column))); continue; }
                var walk = new Relation(new Relation(new Relation(rule, first.Application), offset), new Relation(N.S(transitions.ToArray()), second.Application));
                adjacency.Add(walk);
                if (!ReferenceEquals(second.Motif, to.Left) || !ReferenceEquals(second.Transform, to.Right))
                { inactive.Add(walk); continue; }
                if (!second.Fresh) { pending.Add(walk); continue; }
                var connection = new UnitArrangementConnection(run.Program.Frame, first.Unit, roles.Left, second.Unit, roles.Right);
                var observed = connection.Observe(); var seamRead = new Relation(walk, new Relation(connection.Surface, observed)); reads.Add(seamRead);
                if (N.E(connection.Blocked).Length != 0) { blocked.Add(seamRead); continue; }
                foreach (var accepted in N.E(connection.AcceptedJoins).OfType<Relation>())
                {
                    if (!connection.IsCurrentAgreement(accepted) || accepted.Left is not Relation ends ||
                        !MapSite(first.Unit, ends.Left, out var a) || !MapSite(second.Unit, ends.Right, out var b))
                    { pending.Add(seamRead); continue; }
                    declarations.Add(new Relation(new Relation(a.Right, b.Right), new Relation(accepted, new Relation(a, b))));
                }
            }
        }
        Join = new(SourceGraph, SourceBindings, N.S(declarations.ToArray()));
        blocked.AddRange(N.E(Join.Blocked)); pending.AddRange(N.E(Join.Unresolved));
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray()); Frontier = N.S(frontier.ToArray()); Inactive = N.S(inactive.ToArray());
        Adjacencies = N.S(adjacency.ToArray()); SeamReads = N.S(reads.ToArray());
        Validation = N.S(validation.ToArray());
        Surface = new(new Relation(run.Surface, seamContract), new Relation(new Relation(SourceGraph.Surface, SourceBindings),
            new Relation(new Relation(Validation, new Relation(Adjacencies, SeamReads)), new Relation(Join.Surface,
                new Relation(Accepted, new Relation(Blocked, new Relation(Unresolved, new Relation(Frontier, Inactive))))))));

        bool ExpectedCell(IElement row, IElement column, Relation rule, Relation position, out Relation proof)
        {
            proof = new(new Relation(run.Program.Surface, run.Window), new Relation(new Relation(row, column), position));
            if (!contextReady || !windowReady || run.Window.Left is not Relation start || rule.Left is not Relation phasePair ||
                run.Program.Layout.Right is not Relation { Left: Relation stepSlots }) return false;
            var ri = Array.FindIndex(rows, actual => N.Same(actual, row)); var ci = Array.FindIndex(columns, actual => N.Same(actual, column));
            if (!Phase(uCycle, start.Left, columns.Take(ci), out var up, out var uSteps) ||
                !Phase(vCycle, start.Right, rows.Take(ri), out var vp, out var vSteps) || !N.Same(phasePair.Left, up) || !N.Same(phasePair.Right, vp)) return false;
            var x = N.E(run.Bindings).OfType<Relation>().Where(binding => N.Same(binding.Left, stepSlots.Left)).ToArray();
            var y = N.E(run.Bindings).OfType<Relation>().Where(binding => N.Same(binding.Left, stepSlots.Right)).ToArray();
            if (x is not [Relation { Right: Relation stepU }] || y is not [Relation { Right: Relation stepV }] ||
                !Word(stepU) || !Word(stepV) || !Word(position)) return false;
            var participation = rows.Take(ri).Select(slot => (IElement)new Relation(slot, stepV))
                .Concat(columns.Take(ci).Select(slot => (IElement)new Relation(slot, stepU))).ToArray();
            var displacement = N.S(Enumerable.Repeat(stepV, ri).Concat(Enumerable.Repeat(stepU, ci)).SelectMany(N.E).ToArray());
            var compare = run.Program.Frame.Reduce(displacement, UnitArrangementRead.Inverse(run.Program.Frame, position));
            proof = new(new Relation(run.Program.Surface, run.Window), new Relation(new Relation(uSteps, vSteps), new Relation(N.S(participation), compare)));
            return UnitArrangementRead.IsZero(run.Program.Frame, compare);
        }
        bool Word(Relation word) => N.Read(word, out var moves) && moves.All(item => item is Relation move && run.Program.Frame.Direction(move, out _, out _, out _));
        bool Phase(Relation cycle, IElement initial, IEnumerable<IElement> slots, out IElement current, out Relation transitions)
        {
            current = initial; var result = new List<IElement>(); transitions = N.S();
            if (cycle is not { Left: Relation domain, Right: Relation edgeScope } || !N.Read(domain, out var members) || !N.Read(edgeScope, out var edges) ||
                !members.Any(member => N.Same(member, initial))) return false;
            foreach (var slot in slots)
            {
                var phase = current; var next = edges.OfType<Relation>().Where(edge => N.Same(edge.Left, phase)).ToArray();
                if (next is not [var edge] || !members.Any(member => N.Same(member, edge.Right))) return false;
                result.Add(new Relation(slot, edge)); current = edge.Right;
            }
            transitions = N.S(result.ToArray()); return true;
        }

        bool Direction(IElement value, out bool horizontal, out bool forward)
        {
            horizontal = false; forward = false;
            if (value is not Relation direction) return false;
            if (ReferenceEquals(direction.Left, run.Program.Surface))
            {
                forward = true;
                if (ReferenceEquals(direction.Right, uCycle)) { horizontal = true; return true; }
                return ReferenceEquals(direction.Right, vCycle);
            }
            if (!ReferenceEquals(direction.Right, run.Program.Surface)) return false;
            if (ReferenceEquals(direction.Left, uCycle)) { horizontal = true; return true; }
            return ReferenceEquals(direction.Left, vCycle);
        }
    }

    private sealed record Occurrence(Relation Application, UnitArrangementNode Unit, Relation Motif, Relation Transform, bool Fresh);
    private static bool MapSite(UnitArrangementNode unit, IElement raw, out Relation map)
    {
        var matches = N.E(unit.Current.Join.SiteMap).OfType<Relation>().Where(row => N.Same(row.Left, raw)).ToArray();
        map = matches.FirstOrDefault()!; return matches.Length == 1;
    }
    private static bool MatchesProgram(Relation source, UnitArrangementProgram program) =>
        source is { Left: var frame, Right: Relation { Left: Relation { Left: var paths, Right: var ports }, Right: Relation { Left: var closed, Right: var evidence } } } &&
        ReferenceEquals(frame, program.Frame.Surface) && ReferenceEquals(paths, program.Paths) && ReferenceEquals(ports, program.Ports) &&
        ReferenceEquals(closed, program.ClosedPaths) && ReferenceEquals(evidence, program.Evidence);
    private static bool MatchesTransform(Relation source, QualitativePatternTransform? transform) => transform is not null &&
        source is { Left: Relation endpoints, Right: Relation { Left: var rows, Right: var evidence } } &&
        ReferenceEquals(endpoints.Left, transform.Source.Surface) && ReferenceEquals(endpoints.Right, transform.Target.Surface) &&
        ReferenceEquals(rows, transform.Rows) && ReferenceEquals(evidence, ((Relation)transform.Surface.Right).Right);
    private static bool Closed(Relation scope, out IElement[] entries) => N.Read(scope, out entries);
    private static bool SameEntries(Relation first, Relation second) => N.Read(first, out var a) && N.Read(second, out var b) &&
        a.Length == b.Length && a.Zip(b).All(pair => N.Same(pair.First, pair.Second));
}
