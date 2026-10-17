using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed class AdaptiveMotifLink
{
    public AdaptiveMotifNode From { get; }
    public AdaptiveMotifNode To { get; }
    public IElement FromRole { get; }
    public IElement ToRole { get; }
    public Relation Surface { get; }
    public Relation InputPort { get; }
    public Relation QueuePort { get; }
    public Relation TargetPort { get; }
    public Relation Export { get; }
    public Relation Delivery { get; }
    public AdaptiveMotifLink(AdaptiveMotifNode from, AdaptiveMotifNode to, IElement fromRole, IElement toRole)
    {
        From = from; To = to; FromRole = fromRole; ToRole = toRole;
        var boundary = new Relation(new Relation(from.Cell, to.Cell), new Relation(fromRole, toRole));
        InputPort = new(from.Cell, new Placeholder()); QueuePort = new(boundary, new Placeholder()); TargetPort = new(to.Cell, new Placeholder());
        Export = new(InputPort, QueuePort); Delivery = new(QueuePort, TargetPort);
        Surface = new(boundary, new Relation(new Relation(InputPort, QueuePort), new Relation(TargetPort, new Relation(Export, Delivery))));
    }
}

/// <summary>Persistent material passages and live, replaceable boundary offers.
/// Observation routes are explicitly non-draining; a rejected material seam
/// can ask for repair without allowing its waiting packet to cross.</summary>
public sealed class AdaptiveWallpaperMachine
{
    public string Name { get; }
    public AdaptiveMotifGrammar Grammar { get; }
    public IReadOnlyList<AdaptiveMotifNode> Nodes { get; }
    public IReadOnlyList<AdaptiveMotifLink> Links { get; }
    public Relation Observations { get; }
    public QualitativePacketTransport Transport { get; }
    public Relation Surface { get; }
    public Relation Timeline { get; private set; } = S();
    public Relation Negotiations { get; private set; } = S();
    public Relation Deliveries { get; private set; } = S();
    public Relation Blocked { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();
    public Relation CurrentUnknown { get; private set; } = S();
    public Relation CurrentSeams { get; private set; } = S();
    public Relation DefinitionProblems { get; }
    public bool IsValid => E(DefinitionProblems).Length == 0;
    private readonly IElement refinement = new Placeholder();

    public AdaptiveWallpaperMachine(string name, AdaptiveMotifGrammar grammar, IReadOnlyList<AdaptiveMotifNode> nodes,
        IReadOnlyList<AdaptiveMotifLink> links, Relation observations)
    {
        Name = name; Grammar = grammar; Nodes = Array.AsReadOnly(nodes.ToArray()); Links = Array.AsReadOnly(links.ToArray()); Observations = observations;
        var problems = new List<IElement>(); var capacities = new List<IElement>(); var routes = new List<IElement>();
        foreach (var node in Nodes)
            if (!ReferenceEquals(node.Grammar, grammar) || Nodes.Count(other => ReferenceEquals(other.Cell, node.Cell)) != 1) problems.Add(node.Surface);
        var seenLinks = new HashSet<AdaptiveMotifLink>();
        foreach (var link in Links)
        {
            if (!seenLinks.Add(link)) { problems.Add(link.Surface); continue; }
            if (!Nodes.Any(node => ReferenceEquals(node, link.From)) || !Nodes.Any(node => ReferenceEquals(node, link.To)) || ReferenceEquals(link.From, link.To) ||
                Links.Count(other => ReferenceEquals(other, link)) != 1 || !link.From.Current.TryReadPort(link.FromRole, out _, out _) || !link.To.Current.TryReadPort(link.ToRole, out _, out _)) problems.Add(link.Surface);
            foreach (var port in new[] { link.InputPort, link.QueuePort, link.TargetPort }) capacities.Add(new Relation(port, S(new Placeholder())));
            routes.Add(link.Export); routes.Add(link.Delivery);
        }
        if (!Closed(observations)) problems.Add(observations);
        foreach (var row in E(observations))
            if (row is not Relation route || Nodes.Where(node => ReferenceEquals(node.Cell, route.Left)).ToArray() is not [var observer] ||
                Links.Where(link => ReferenceEquals(link.Surface, route.Right)).ToArray() is not [var passage] ||
                !ReferenceEquals(observer, passage.From) && !ReferenceEquals(observer, passage.To)) problems.Add(row);
        Transport = new(S(capacities.ToArray()), S(routes.ToArray()), new Relation(S(Links.Select(link => (IElement)link.Surface).ToArray()), observations));
        if (!Transport.TryReadState(Transport.State, out _, out _, out _, out _)) problems.Add(Transport.Surface);
        DefinitionProblems = S(problems.ToArray());
        Surface = new(new Relation(grammar.Surface, S(Nodes.Select(node => (IElement)node.Surface).ToArray())),
            new Relation(S(Links.Select(link => (IElement)link.Surface).ToArray()), new Relation(observations, new Relation(Transport.Surface, DefinitionProblems))));
        if (IsValid) CurrentSeams = S(Links.Select(link => (IElement)Negotiate(link)).ToArray());
    }

    public Relation Snapshot() => S(Nodes.Select(node => (IElement)new Relation(node.Cell, node.Current.Surface)).ToArray());
    public Relation Seed(AdaptiveMotifLink link, IElement packet) => IsValid && Links.Any(actual => ReferenceEquals(actual, link))
        ? Transport.Import(new Placeholder(), link.InputPort, S(packet), Surface) : Refuse(new Relation(link.Surface, packet));

    // agreement = ((machine -> declared link) -> (exact left head -> exact right head)) ->
    //             ((actual left publication -> actual right publication) ->
    //              ((actual left binding -> actual right binding) -> formal equality read))
    public Relation Negotiate(AdaptiveMotifLink link)
    {
        if (!IsValid || !Links.Any(actual => ReferenceEquals(actual, link))) return Refuse(link.Surface);
        Relation body;
        if (!link.From.Current.TryReadPort(link.FromRole, out var a, out var ab) || !link.To.Current.TryReadPort(link.ToRole, out var b, out var bb))
            body = StructuralConstraint.Open(link.Surface);
        else body = new(new Relation(a, b), new Relation(new Relation(ab, bb), Grammar.Frame.Reduce((Relation)ab.Right, Inverse((Relation)bb.Right))));
        var read = new Relation(new Relation(new Relation(Surface, link.Surface), new Relation(link.From.Current.Surface, link.To.Current.Surface)), body);
        Negotiations = Append(Negotiations, read);
        CurrentSeams = S(E(CurrentSeams).Where(item => item is not Relation { Left: Relation { Left: Relation prior } } ||
            !ReferenceEquals(prior.Right, link.Surface)).Concat(new[] { read }).ToArray());
        return read;
    }

    public bool TryReadAccepted(Relation agreement, out AdaptiveMotifLink link)
    {
        link = null!;
        if (!IsValid || !E(Negotiations).Any(actual => ReferenceEquals(actual, agreement)) ||
            agreement.Left is not Relation { Left: Relation context, Right: Relation heads } || !ReferenceEquals(context.Left, Surface) ||
            Links.SingleOrDefault(actual => ReferenceEquals(actual.Surface, context.Right)) is not { } actualLink ||
            !ReferenceEquals(heads.Left, actualLink.From.Current.Surface) || !ReferenceEquals(heads.Right, actualLink.To.Current.Surface) ||
            !actualLink.From.Current.TryReadPort(actualLink.FromRole, out var a, out var ab) || !actualLink.To.Current.TryReadPort(actualLink.ToRole, out var b, out var bb) ||
            agreement.Right is not Relation { Left: Relation publications, Right: Relation { Left: Relation bindings, Right: Relation equality } } ||
            !ReferenceEquals(publications.Left, a) || !ReferenceEquals(publications.Right, b) || !ReferenceEquals(bindings.Left, ab) || !ReferenceEquals(bindings.Right, bb) ||
            equality.Left is not Relation { Left: var frame, Right: Relation operands } || !ReferenceEquals(frame, Grammar.Frame.Surface) ||
            !ReferenceEquals(operands.Left, ab.Right) || operands.Right is not Relation inverse || !SameWord(inverse, Inverse((Relation)bb.Right)) ||
            !Grammar.Frame.TryReadResult(equality, out var remaining, out _, out var pending) || E(remaining).Length != 0 || !Closed(remaining) || E(pending).Length != 0) return false;
        link = actualLink; return true;
    }

    public bool Deliver(AdaptiveMotifLink link, Relation agreement)
    {
        if (!IsValid || !Links.Any(actual => ReferenceEquals(actual, link))) { Refuse(link.Surface); return false; }
        var packets = Transport.PortPackets(link.QueuePort);
        if (E(packets).Length == 0) return false;
        if (!TryReadAccepted(agreement, out var actual) || !ReferenceEquals(actual, link))
        { Blocked = Append(Blocked, new Relation(agreement, Transport.ReadPort(Transport.State, link.QueuePort))); return false; }
        var moves = S(E(packets).Select(packet => (IElement)new Relation(packet, link.Delivery)).ToArray());
        var transfer = Transport.Prepare(new Placeholder(), moves, new Relation(agreement, new Relation(Snapshot(), Transport.State)));
        if (!Transport.TryReadAccepted(transfer, out _, out _) || !Transport.TryCommit(transfer)) { Blocked = Append(Blocked, transfer); return false; }
        Deliveries = Append(Deliveries, transfer); return true;
    }

    public Relation Round(Relation? schedule = null)
    {
        if (!IsValid || Nodes.Any(node => E(node.Pending).Length != 0)) return Refuse(Snapshot());
        var order = schedule ?? S(Nodes.Select(node => (IElement)node.Cell).ToArray());
        if (!Closed(order) || E(order).Length != Nodes.Count || Nodes.Any(node => E(order).Count(cell => ReferenceEquals(cell, node.Cell)) != 1)) return Refuse(order);
        var before = Snapshot(); var material = Transport.State;
        var heads = Nodes.ToDictionary(node => node, node => node.Current);
        var seams = Links.Select(link => Negotiate(link)).ToArray(); var arrivals = new List<IElement>();
        foreach (var (link, index) in Links.Select((link, index) => (link, index)))
        {
            if (E(Transport.PortPackets(material, link.QueuePort)).Length != 0)
            { var prior = Transport.State; Deliver(link, seams[index]); arrivals.Add(new Relation(seams[index], new Relation(prior, Transport.State))); }
            var offered = Transport.PortPackets(material, link.InputPort);
            if (E(offered).Length == 0) continue;
            var transfer = Transport.Prepare(new Placeholder(), S(E(offered).Select(packet => (IElement)new Relation(packet, link.Export)).ToArray()), new Relation(before, link.Surface));
            if (Transport.TryReadAccepted(transfer, out _, out _) && Transport.TryCommit(transfer)) arrivals.Add(transfer);
            else Blocked = Append(Blocked, transfer);
        }
        var proposals = new List<IElement>(); var questions = new List<IElement>(); var unknown = new List<IElement>();
        foreach (var cell in E(order))
        {
            var node = Nodes.Single(actual => ReferenceEquals(actual.Cell, cell));
            var subscriptions = E(Observations).Cast<Relation>().Where(row => ReferenceEquals(row.Left, node.Cell)).ToArray();
            if (subscriptions.Length == 0) continue;
            var neighbours = new List<IElement>(); var evidence = new List<IElement>();
            foreach (var route in subscriptions)
            {
                var link = Links.Single(actual => ReferenceEquals(actual.Surface, route.Right));
                var other = ReferenceEquals(link.From, node) ? link.To : link.From;
                var advertised = other.Advertisement(heads[other]); neighbours.Add(heads[other].Template.Surface);
                evidence.Add(new Relation(route, new Relation(advertised, seams[Links.ToList().IndexOf(link)])));
            }
            var read = FiniteLaw.WithProvenance(FiniteLaw.Evaluate(node.Policy, S(heads[node].Template.Surface),
                S(neighbours.Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray())), new Relation(before, S(evidence.ToArray())));
            var choices = S(Grammar.Plus.Surface, Grammar.Minus.Surface);
            var question = QualitativeQuestion.Ask(read, S(E(choices).Select(choice => (IElement)new Relation(choice, choice)).ToArray()), refinement);
            questions.Add(new Relation(node.Cell, question));
            if (!QualitativeQuestion.TryReadAnswer(question, out var selected)) { unknown.Add(new Relation(node.Cell, question)); continue; }
            if (!ReferenceEquals(selected, heads[node].Template.Surface)) proposals.Add(new Relation(node.Cell, node.Propose(question)));
        }
        var replacements = new List<IElement>();
        foreach (var proposal in proposals.Cast<Relation>()) replacements.Add(Nodes.Single(node => ReferenceEquals(node.Cell, proposal.Left)).Commit((Relation)proposal.Right));
        CurrentUnknown = S(unknown.ToArray());
        CurrentSeams = S(Links.Select(link => (IElement)Negotiate(link)).ToArray());
        var step = new Relation(new Relation(Surface, new Relation(before, material)), new Relation(S(seams),
            new Relation(S(arrivals.ToArray()), new Relation(S(questions.ToArray()), new Relation(S(proposals.ToArray()),
                new Relation(S(replacements.ToArray()), new Relation(Snapshot(), new Relation(Transport.State, new Relation(CurrentSeams, CurrentUnknown)))))))));
        Timeline = Append(Timeline, step); return step;
    }

    public QualitativeGraphJoin Field()
    {
        var sites = S(Nodes.SelectMany(node => E(node.Current.Graph.Sites)).ToArray()); var strokes = S(Nodes.SelectMany(node => E(node.Current.Graph.Strokes)).ToArray());
        var bindings = S(Nodes.SelectMany(node => E(node.Current.Bindings)).ToArray()); var joins = Nodes.SelectMany(node => E(node.Current.Closures)).ToList();
        foreach (var seam in E(CurrentSeams).Cast<Relation>())
            if (TryReadAccepted(seam, out _) && seam.Right is Relation { Right: Relation { Left: Relation ports } })
                joins.Add(new Relation(new Relation(((Relation)ports.Left).Left, ((Relation)ports.Right).Left), seam));
        return new(new QualitativeStrokeGraph(Grammar.Frame, sites, strokes), bindings, S(joins.ToArray()));
    }

    private Relation Inverse(Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move => (IElement)QualitativeMotionFrame.Move(
        E(Grammar.Frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static bool SameWord(Relation a, Relation b) => E(a).Length == E(b).Length && E(a).Cast<Relation>().Zip(E(b).Cast<Relation>()).All(pair => ReferenceEquals(pair.First.Left, pair.Second.Left) && ReferenceEquals(pair.First.Right, pair.Second.Right));
    private Relation Refuse(IElement evidence) { Unresolved = Append(Unresolved, evidence); return new Relation(Surface, StructuralConstraint.Open(evidence)); }
    private static Relation Append(Relation scope, IElement value) => S([.. E(scope), value]);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
