using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An explicit independent-actuator realization of an exposed native
/// topology. Each edge has its own acknowledged placement and path; published
/// endpoint roles then justify a joined observation. This is a different causal
/// history from a serial shared cursor, not a claim that histories are equal.</summary>
public sealed class OrnamentNetworkConstruction
{
    // definition = (source -> (topology -> representative bindings)) ->
    //              (cause -> Closed(actual edge -> (program -> difference proof)))
    // commands = Closed(actual edge -> actual actuator surface)
    // role-site = (actual representative role -> actual publication) -> binding
    // capture = definition -> ((commands -> isolated commands) ->
    //           ((actual execution captures -> role-sites) -> (joined graph -> unresolved)))
    public QualitativeMotionFrame Frame { get; }
    public IElement Source { get; }
    public Relation SourceTopology { get; }
    public Relation SourceBindings { get; }
    public IElement Cause { get; }
    public Relation Definition { get; }
    public Relation DifferenceReads { get; }
    public Relation Commands { get; private set; } = S();
    public Relation IsolatedCommands { get; private set; } = S();
    public Relation RoleSites { get; private set; } = S();
    public UnitArrangementNode[] Units { get; private set; } = [];
    public PrimitiveFlowLeaf[] IsolatedUnits { get; private set; } = [];
    public QualitativeGraphJoin? Join { get; private set; }
    public Relation OutputBindings => Join?.Bindings ?? StructuralConstraint.Open();
    public Relation Bindings => OutputBindings;
    public Relation Unresolved { get; private set; }
    public Relation Surface => Capture();
    public bool IsComplete => Join is not null && E(Unresolved).Length == 0 && Join.TryReadPassed(Join.Evaluate());
    private readonly List<(IElement Edge, IElement From, IElement To, Relation Placement, UnitArrangementProgram Program)> plans = [];
    private readonly List<(IElement Role, Relation Word)> isolated = [];
    private readonly bool ready;

    public OrnamentNetworkConstruction(QualitativeMotionFrame frame, OrnamentDerivedScaffold source,
        Relation bindings, IElement cause)
        : this(frame, source.Topology, bindings, cause, source.Surface, source.IsComplete) { }

    public OrnamentNetworkConstruction(QualitativeMotionFrame frame, Relation topology, Relation bindings, IElement cause)
        : this(frame, topology, bindings, cause, topology, true) { }

    private OrnamentNetworkConstruction(QualitativeMotionFrame frame, Relation topology, Relation bindings,
        IElement cause, IElement source, bool supported)
    {
        Frame = frame; SourceTopology = topology; SourceBindings = bindings; Cause = cause; Source = source;
        var pending = new List<IElement>(); var differences = new List<IElement>(); var definitions = new List<IElement>();
        var sites = Array.Empty<IElement>(); var edges = Array.Empty<IElement>(); var faces = Array.Empty<IElement>();
        ready = supported && Closed(bindings, out _) && topology.Left is Relation siteScope && Closed(siteScope, out sites) &&
            topology.Right is Relation { Left: Relation edgeScope, Right: Relation faceScope } && Closed(edgeScope, out edges) && Closed(faceScope, out faces) &&
            N.Distinct(sites) && N.Distinct(edges) && N.Distinct(faces);
        if (!ready) pending.Add(new Relation(source, new Relation(topology, bindings)));
        var actualEdges = new List<(IElement Identity, IElement From, IElement To)>();
        foreach (var item in edges)
        {
            if (item is not Relation { Right: Relation ends } row || !sites.Any(site => N.Same(site, ends.Left)) ||
                !sites.Any(site => N.Same(site, ends.Right)) || N.Same(ends.Left, ends.Right) ||
                actualEdges.Any(edge => N.Same(edge.Identity, row.Left)))
            { pending.Add(new Relation(item, topology)); continue; }
            actualEdges.Add((row.Left, ends.Left, ends.Right));
        }
        foreach (var site in sites) if (!Binding(site, out _, out _)) pending.Add(new Relation(site, bindings));
        foreach (var item in faces)
        {
            if (item is not Relation { Right: Relation boundary } || !Closed(boundary, out var parts) || parts.Length < 3)
            { pending.Add(new Relation(item, topology)); continue; }
            var orientations = new List<Relation>();
            foreach (var part in parts)
                if (part is Relation { Right: Relation ends } participation &&
                    actualEdges.SingleOrDefault(edge => N.Same(edge.Identity, participation.Left)) is var edge && edge.Identity is not null &&
                    (N.Same(edge.From, ends.Left) && N.Same(edge.To, ends.Right) || N.Same(edge.To, ends.Left) && N.Same(edge.From, ends.Right)))
                    orientations.Add(ends);
                else pending.Add(new Relation(part, item));
            if (orientations.Count != parts.Length || orientations.Where((ends, index) =>
                    !N.Same(ends.Right, orientations[(index + 1) % orientations.Count].Left)).Any()) pending.Add(new Relation(item, topology));
        }
        foreach (var (edge, from, to) in actualEdges)
        {
            if (!Binding(from, out var fromBinding, out var fromWord) || !Binding(to, out var toBinding, out var toWord)) continue;
            var difference = frame.Reduce(toWord, Inverse(frame, fromWord));
            if (!frame.TryReadResult(difference, out var chord, out _, out var unknown) || !Closed(chord, out _) || E(unknown).Length != 0)
            { pending.Add(new Relation(edge, difference)); continue; }
            var proof = new Relation(new Relation(fromBinding, toBinding), difference); differences.Add(new Relation(edge, proof));
            var path = new Relation(S(), S(chord));
            var ports = S(new Relation(from, new Relation(path, chord)), new Relation(to, new Relation(path, path)));
            var program = new UnitArrangementProgram(frame, "one independent correspondence edge", S(path), ports,
                evidence: new Relation(source, new Relation(edge, proof)));
            plans.Add((edge, from, to, fromWord, program)); definitions.Add(new Relation(edge, new Relation(program.Surface, proof)));
        }
        foreach (var site in sites.Where(site => !actualEdges.Any(edge => N.Same(site, edge.From) || N.Same(site, edge.To))))
            if (Binding(site, out _, out var word)) isolated.Add((site, word));
        ready &= pending.Count == 0 && plans.Count == edges.Length;
        DifferenceReads = S(differences.ToArray()); Unresolved = S(pending.ToArray());
        Definition = new(new Relation(source, new Relation(topology, bindings)), new Relation(cause, S(definitions.ToArray())));
    }

    public Relation Run()
    {
        if (!ready || E(Unresolved).Length != 0 || Join is not null) return Capture();
        var units = new List<UnitArrangementNode>(); var points = new List<PrimitiveFlowLeaf>();
        var commands = new List<IElement>(); var pointCommands = new List<IElement>();
        var rawStrokes = new List<IElement>(); var rawBindings = new List<IElement>(); var roleSites = new List<IElement>(); var joins = new List<IElement>();
        var firstSites = new Dictionary<IElement, (IElement Site, IElement Evidence)>(N.Participants.Instance);
        foreach (var (edge, from, to, placement, program) in plans)
        {
            var unit = new UnitArrangementNode("independent network passage", [program], program, placement);
            units.Add(unit); commands.Add(new Relation(edge, unit.Surface)); rawStrokes.AddRange(E(unit.Current.Graph.Strokes));
            foreach (var role in new[] { from, to })
            {
                if (!UnitArrangementConnection.TryReadPublication(unit, role, out var publication, out var binding))
                { Unresolved = Append(Unresolved, new Relation(edge, unit.Current.Surface)); continue; }
                Register(role, binding, publication);
            }
        }
        foreach (var (role, word) in isolated)
        {
            var drawing = new QualitativeDrawingReceiver(Frame);
            var leaf = new PrimitiveFlowLeaf("isolated network representative", drawing, S(drawing.Jump(word), drawing.Anchor()));
            points.Add(leaf); pointCommands.Add(new Relation(role, leaf.Surface));
            for (var step = 0; step < 2 && E(leaf.CompletionEvents).Length == 0; step++)
            {
                var request = leaf.Propose(new Placeholder(), S(new Placeholder()));
                if (!leaf.TryReadRequest(request, out var before, out var offer) ||
                    !leaf.TryReadAccepted(leaf.Acknowledge(request, drawing.Step(before, offer)), out _)) break;
            }
            if (E(leaf.CompletionEvents).Length != 1 || !drawing.TryReadState(drawing.State, out _, out var binding, out _, out _, out _, out _, out _))
                Unresolved = Append(Unresolved, new Relation(role, leaf.Capture()));
            else Register(role, binding, new Relation(leaf.Surface, leaf.CompletionEvents));
        }
        Units = units.ToArray(); IsolatedUnits = points.ToArray(); Commands = S(commands.ToArray()); IsolatedCommands = S(pointCommands.ToArray());
        RoleSites = S(roleSites.ToArray());
        if (E(Unresolved).Length != 0) return Capture();
        var graph = new QualitativeStrokeGraph(Frame, S(rawBindings.Cast<Relation>().Select(row => row.Left).ToArray()), S(rawStrokes.ToArray()));
        Join = new(graph, S(rawBindings.ToArray()), S(joins.ToArray()));
        if (!Join.TryReadPassed(Join.Evaluate())) Unresolved = Append(Unresolved, Join.Surface);
        return Capture();

        void Register(IElement role, Relation binding, IElement publication)
        {
            rawBindings.Add(binding); var proof = new Relation(new Relation(role, publication), binding); roleSites.Add(proof);
            if (firstSites.TryGetValue(role, out var first))
                joins.Add(new Relation(new Relation(binding.Left, first.Site), new Relation(proof, first.Evidence)));
            else firstSites.Add(role, (binding.Left, proof));
        }
    }

    public Relation Capture() => new(Definition, new Relation(new Relation(Commands, IsolatedCommands),
        new Relation(new Relation(S(Units.Select(unit => (IElement)new Relation(unit.PlacementExecution.Capture(), unit.Current.Surface))
            .Concat(IsolatedUnits.Select(leaf => (IElement)leaf.Capture())).ToArray()), RoleSites),
            new Relation(Join?.Surface ?? StructuralConstraint.Open(), Unresolved))));

    private bool Binding(IElement role, out Relation binding, out Relation word)
    {
        binding = word = null!;
        var matches = E(SourceBindings).OfType<Relation>().Where(row => N.Same(row.Left, role)).ToArray();
        if (!Closed(SourceBindings, out _) || matches is not [var actual] || actual.Right is not Relation value || !Closed(value, out var moves) ||
            moves.Any(item => item is not Relation move || !Frame.Direction(move, out _, out _, out _))) return false;
        binding = actual; word = value; return true;
    }
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static Relation Append(Relation scope, IElement item) => S([.. E(scope), item]);
    private static bool Closed(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
