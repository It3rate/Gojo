using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Materializes an observed adjacency through a fresh acknowledged
/// drawing circuit. Representative word bindings are an explicit interpretation;
/// source topology, command correspondence and actual actuator receipts remain
/// connected. Equal-position roles do not acquire a shared site automatically.</summary>
public sealed class OrnamentScaffoldExecution
{
    // definition = (derived scaffold -> actual role bindings) ->
    //              (cause -> (Closed(actual edge -> leaf surface) -> difference reads))
    // realization = definition -> (root capture ->
    //               (Closed((actual role -> actual cursor site) -> receipt) ->
    //                (accepted graph join -> unresolved)))
    public QualitativeMotionFrame Frame { get; }
    public IElement Source { get; }
    public Relation SourceTopology { get; }
    public Relation SourceBindings { get; }
    public IElement Cause { get; }
    public QualitativeDrawingReceiver Drawing { get; }
    public PrimitiveFlowGroup? Root { get; }
    public Relation Commands { get; }
    public Relation IsolatedCommands { get; }
    public Relation DifferenceReads { get; }
    public Relation Definition { get; }
    public Relation RoleSites { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();
    public Relation Pending => Root?.Pending ?? S();
    public QualitativeGraphJoin? Join { get; private set; }
    public Relation OutputBindings => Join?.Bindings ?? StructuralConstraint.Open();
    public Relation Bindings => OutputBindings;
    public bool IsComplete => Join is not null && (Root is null || Root.IsAtBoundary &&
        E(Root.CompletionEvents).Length == 1) && E(Unresolved).Length == 0 && Join.TryReadPassed(Join.Evaluate());
    public Relation Surface => Capture();
    private readonly List<(IElement Edge, IElement From, IElement To, PrimitiveFlowLeaf Leaf)> compiled = [];
    private readonly List<(IElement Role, PrimitiveFlowLeaf Leaf)> isolated = [];
    private readonly int actionBudget;
    private readonly bool ready;

    public OrnamentScaffoldExecution(QualitativeMotionFrame frame, OrnamentDerivedScaffold scaffold,
        Relation bindings, IElement cause)
        : this(frame, scaffold.Topology, bindings, cause, scaffold.Surface, scaffold.IsComplete) { }

    /// <summary>Consumes an exposed topology without requiring its producer's
    /// private implementation. Topology is sites -> (edge rows -> face rows),
    /// edge row = actual edge identity -> (actual from role -> actual to role).
    /// Face rows retain optional ordered incidence; no metric embedding is
    /// inferred from them or from the selected representative words.</summary>
    public OrnamentScaffoldExecution(QualitativeMotionFrame frame, Relation topology,
        Relation bindings, IElement cause)
        : this(frame, topology, bindings, cause, topology, true) { }

    private OrnamentScaffoldExecution(QualitativeMotionFrame frame, Relation topology,
        Relation bindings, IElement cause, IElement source, bool supported)
    {
        Frame = frame; Source = source; SourceTopology = topology; SourceBindings = bindings; Cause = cause; Drawing = new(frame);
        var pending = new List<IElement>(); var differences = new List<IElement>(); var commands = new List<IElement>();
        var sites = Array.Empty<IElement>(); var edgeRows = Array.Empty<IElement>(); var faceRows = Array.Empty<IElement>();
        ready = supported && Closed(bindings, out _) && topology.Left is Relation siteScope && Closed(siteScope, out sites) &&
            topology.Right is Relation { Left: Relation edges, Right: Relation faces } && Closed(edges, out edgeRows) && Closed(faces, out faceRows) &&
            N.Distinct(sites) && N.Distinct(edgeRows) && N.Distinct(faceRows);
        if (!ready) pending.Add(new Relation(source, new Relation(topology, bindings)));
        var actualEdges = new List<(IElement Identity, IElement From, IElement To)>();
        foreach (var item in edgeRows)
        {
            if (item is not Relation { Right: Relation ends } edge ||
                !sites.Any(site => N.Same(site, ends.Left)) || !sites.Any(site => N.Same(site, ends.Right)) || N.Same(ends.Left, ends.Right) ||
                actualEdges.Any(old => N.Same(old.Identity, edge.Left)))
            { pending.Add(new Relation(item, topology)); continue; }
            actualEdges.Add((edge.Left, ends.Left, ends.Right));
        }
        foreach (var site in sites)
            if (!TryBinding(site, out _, out _)) pending.Add(new Relation(site, bindings));
        foreach (var item in faceRows)
        {
            if (item is not Relation { Right: Relation boundary } || !Closed(boundary, out var parts) || parts.Length < 3)
            { pending.Add(new Relation(item, topology)); continue; }
            var ends = new List<Relation>();
            foreach (var part in parts)
                if (part is Relation { Right: Relation orientation } participation &&
                    actualEdges.SingleOrDefault(edge => N.Same(edge.Identity, participation.Left)) is var edge && edge.Identity is not null &&
                    (N.Same(edge.From, orientation.Left) && N.Same(edge.To, orientation.Right) ||
                     N.Same(edge.To, orientation.Left) && N.Same(edge.From, orientation.Right))) ends.Add(orientation);
                else pending.Add(new Relation(part, item));
            if (ends.Count != parts.Length || ends.Where((end, index) => !N.Same(end.Right, ends[(index + 1) % ends.Count].Left)).Any())
                pending.Add(new Relation(item, topology));
        }
        foreach (var (edge, from, to) in actualEdges)
        {
            if (
                !TryBinding(from, out var fromBinding, out var fromWord) || !TryBinding(to, out var toBinding, out var toWord))
            { pending.Add(new Relation(edge, bindings)); continue; }
            var difference = frame.Reduce(toWord, Inverse(frame, fromWord));
            if (!frame.TryReadResult(difference, out var chord, out _, out var unknown) || !Closed(chord, out _) || E(unknown).Length != 0)
            { pending.Add(new Relation(edge, difference)); continue; }
            differences.Add(new Relation(edge, new Relation(new Relation(fromBinding, toBinding), difference)));
            var leaf = new PrimitiveFlowLeaf("one derived adjacency", Drawing,
                S(Drawing.Reset(), Drawing.Jump(fromWord), Drawing.Draw(chord)));
            compiled.Add((edge, from, to, leaf)); commands.Add(new Relation(edge, leaf.Surface));
        }
        foreach (var site in sites.Where(site => !actualEdges.Any(edge => N.Same(site, edge.From) || N.Same(site, edge.To))))
        {
            if (!TryBinding(site, out _, out var word)) continue;
            isolated.Add((site, new PrimitiveFlowLeaf("one isolated representative", Drawing, S(Drawing.Reset(), Drawing.Jump(word)))));
        }
        ready &= pending.Count == 0 && compiled.Count == edgeRows.Length;
        var children = compiled.Select(item => (PrimitiveFlowNode)item.Leaf).Concat(isolated.Select(item => (PrimitiveFlowNode)item.Leaf)).ToArray();
        if (children.Length != 0) Root = Balanced(children);
        actionBudget = checked(compiled.Count * 3 + isolated.Count * 2);
        Commands = S(commands.ToArray()); IsolatedCommands = S(isolated.Select(item => (IElement)new Relation(item.Role, item.Leaf.Surface)).ToArray());
        DifferenceReads = S(differences.ToArray()); Unresolved = S(pending.ToArray());
        Definition = new(new Relation(source, new Relation(topology, bindings)),
            new Relation(cause, new Relation(new Relation(Commands, IsolatedCommands), DifferenceReads)));
    }

    /// <summary>One bounded drive of the already constructed native controller.
    /// A short drive retains its phase and may resume; it cannot certify or join
    /// the unexecuted suffix. Calling after completion does not replay material.</summary>
    public Relation Run(int? budget = null)
    {
        if (!ready || Join is not null) return Capture();
        if (Root is null) { Materialize(); return Capture(); }
        var remaining = budget ?? actionBudget;
        if (remaining < 0) throw new ArgumentOutOfRangeException(nameof(budget));
        for (var step = 0; step < remaining && E(Root.CompletionEvents).Length == 0; step++)
        {
            var request = Root.Propose(new Placeholder(), S(new Placeholder()));
            if (!Root.TryReadRequest(request, out var before, out var offer))
            { Unresolved = Append(Unresolved, new Relation(request, Root.Capture())); break; }
            var receipt = Drawing.Step(before, offer);
            var acknowledgment = Root.Acknowledge(request, receipt);
            if (!Root.TryReadAccepted(acknowledgment, out _))
            { Unresolved = Append(Unresolved, acknowledgment); break; }
        }
        if (E(Root.CompletionEvents).Length == 1 && E(Unresolved).Length == 0) Materialize();
        return Capture();
    }

    public Relation Capture() => new(Definition, new Relation(Root?.Capture() ?? S(),
        new Relation(RoleSites, new Relation(Join?.Surface ?? StructuralConstraint.Open(), Unresolved))));

    private void Materialize()
    {
        var raw = new List<IElement>(); var rawBindings = new List<IElement>(); var roleSites = new List<IElement>();
        var joins = new List<IElement>();
        var firstSites = new Dictionary<IElement, (IElement Site, Relation Proof)>(N.Participants.Instance);
        foreach (var (edge, fromRole, toRole, leaf) in compiled)
        {
            // Read the actual leaf completion's accepted drawing receipts,
            // rather than infer which global stroke belongs to its command.
            if (E(leaf.CompletionEvents) is not [Relation completion] ||
                completion.Right is not Relation { Left: Relation receipts })
            { Unresolved = Append(Unresolved, new Relation(edge, leaf.Capture())); return; }
            var rows = E(receipts).OfType<Relation>().Select(receipt => (Receipt: receipt, Strokes: Added(receipt))).ToArray();
            var drawn = rows.Where(row => row.Strokes.Length != 0).ToArray();
            if (drawn is not [var accepted] || accepted.Strokes is not [Relation stroke] ||
                !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
            { Unresolved = Append(Unresolved, new Relation(edge, completion)); return; }
            raw.Add(stroke); Register(fromRole, from, edge, accepted.Receipt); Register(toRole, to, edge, accepted.Receipt);
        }
        foreach (var (role, leaf) in isolated)
        {
            if (E(leaf.CompletionEvents) is not [Relation completion] || completion.Right is not Relation { Left: Relation receipts } ||
                E(receipts) is not [_, Relation receipt] || !Drawing.TryReadAccepted(receipt, out var after, out var added) || E(added).Length != 0 ||
                !Drawing.TryReadState(after, out _, out var cursor, out _, out _, out _, out _, out _))
            { Unresolved = Append(Unresolved, new Relation(role, leaf.Capture())); return; }
            Register(role, cursor.Left, role, receipt);
        }
        RoleSites = S(roleSites.ToArray());
        if (E(Unresolved).Length != 0) return;
        var graph = new QualitativeStrokeGraph(Frame,
            S(rawBindings.Cast<Relation>().Select(row => row.Left).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray()), S(raw.ToArray()));
        var join = new QualitativeGraphJoin(graph, S(rawBindings.ToArray()), S(joins.ToArray()));
        if (!join.TryReadPassed(join.Evaluate())) Unresolved = Append(Unresolved, join.Surface);
        Join = join;

        IElement[] Added(Relation receipt) => Drawing.TryReadAccepted(receipt, out _, out var strokes) ? E(strokes) : [];
        void Register(IElement role, IElement site, IElement edge, Relation receipt)
        {
            var binding = E(Drawing.SiteBindings).OfType<Relation>().SingleOrDefault(row => N.Same(row.Left, site));
            if (binding is null) { Unresolved = Append(Unresolved, new Relation(role, site)); return; }
            rawBindings.Add(binding);
            var proof = new Relation(new Relation(role, site), new Relation(edge, receipt)); roleSites.Add(proof);
            if (firstSites.TryGetValue(role, out var first))
                joins.Add(new Relation(new Relation(site, first.Site), new Relation(proof, first.Proof)));
            else firstSites.Add(role, (site, proof));
        }
    }

    private bool TryBinding(IElement role, out Relation binding, out Relation word)
    {
        binding = word = null!;
        var matches = E(SourceBindings).OfType<Relation>().Where(row => N.Same(row.Left, role)).ToArray();
        if (!Closed(SourceBindings, out _) || matches is not [var actual] || actual.Right is not Relation value ||
            !Closed(value, out var moves) || moves.Any(item => item is not Relation move || !Frame.Direction(move, out _, out _, out _))) return false;
        binding = actual; word = value; return true;
    }
    private static PrimitiveFlowGroup Balanced(PrimitiveFlowNode[] children)
    {
        var level = children;
        while (level.Length > 4)
            level = level.Chunk(4).Select(chunk => (PrimitiveFlowNode)new PrimitiveFlowGroup("construction block", chunk)).ToArray();
        return new("derived scaffold construction", level);
    }
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)QualitativeMotionFrame.Move(E(frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static Relation Append(Relation scope, IElement value) => S([.. E(scope), value]);
    private static bool Closed(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
