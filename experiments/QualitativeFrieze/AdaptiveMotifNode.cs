using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A persistent local actuator with a replaceable active motif focus.
/// Replaced ink remains in drawing history; only an accepted complete program
/// can publish new boundary ports. No material packet is moved by revision.</summary>
public sealed class AdaptiveMotifNode
{
    public string Name { get; }
    public IElement Cell { get; } = new Placeholder();
    public AdaptiveMotifGrammar Grammar { get; }
    public QualitativeDrawingReceiver Drawing { get; }
    public Relation AnchorWord { get; }
    public Relation Policy { get; }
    public Relation Surface { get; }
    public MotifRevision Current { get; private set; } = null!;
    public Relation Revisions { get; private set; } = S();
    public Relation Replacements { get; private set; } = S();
    public Relation Pending { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();

    public AdaptiveMotifNode(string name, AdaptiveMotifGrammar grammar, Relation anchorWord,
        AdaptiveMotifGrammar.Template initialTemplate, bool frozen = false)
    {
        Name = name; Grammar = grammar; AnchorWord = anchorWord; Drawing = new(grammar.Frame);
        var variants = S(grammar.Plus.Surface, grammar.Minus.Surface);
        Policy = FiniteLaw.Create(variants, variants, variants,
            FiniteLaw.Row(grammar.Plus.Surface, grammar.Plus.Surface, S(frozen ? grammar.Plus.Surface : grammar.Minus.Surface)),
            FiniteLaw.Row(grammar.Plus.Surface, grammar.Minus.Surface, S(grammar.Plus.Surface)),
            FiniteLaw.Row(grammar.Minus.Surface, grammar.Plus.Surface, S(grammar.Minus.Surface)),
            FiniteLaw.Row(grammar.Minus.Surface, grammar.Minus.Surface, S(frozen ? grammar.Minus.Surface : grammar.Plus.Surface)));
        Surface = new(new Relation(Cell, grammar.Surface), new Relation(new Relation(Drawing.Surface, anchorWord), Policy));
        foreach (var action in new[] { Drawing.Jump(anchorWord), Drawing.Anchor() })
            if (!Drawing.TryReadAccepted(Drawing.Apply(new Placeholder(), S(action), Surface), out _, out _))
                throw new ArgumentException("An initial anchor needs a complete word in the exposed frame.");
        if (!Build(initialTemplate, Surface, out var revision)) throw new ArgumentException("An initial motif needs a complete executable template.");
        Current = revision; Revisions = S(revision.Surface);
    }

    public Relation Advertisement(MotifRevision? revision = null)
    {
        var actual = revision ?? Current;
        return E(Revisions).Any(head => ReferenceEquals(head, actual.Surface))
            ? new Relation(new Relation(Cell, actual.Surface), new Relation(actual.Template.Surface, actual.Ports))
            : new Relation(Cell, StructuralConstraint.Open(actual.Surface));
    }

    public Relation Propose(Relation question)
    {
        if (E(Pending).Length != 0 || !QualitativeQuestion.TryReadAnswer(question, out var selected) || Template(selected) is null)
            return Refuse(question);
        var proposal = new Relation(new Relation(Cell, Current.Surface), new Relation(Drawing.State, new Relation(question, selected)));
        Pending = S(proposal); return proposal;
    }

    public Relation Commit(Relation proposal)
    {
        if (E(Pending) is not [var actual] || !ReferenceEquals(actual, proposal) ||
            proposal.Left is not Relation focus || !ReferenceEquals(focus.Left, Cell) || !ReferenceEquals(focus.Right, Current.Surface) ||
            proposal.Right is not Relation { Left: var receiverHead, Right: Relation { Left: Relation question } choice } || !ReferenceEquals(receiverHead, Drawing.State) ||
            !QualitativeQuestion.TryReadAnswer(question, out var selected) || !ReferenceEquals(selected, choice.Right) || Template(selected) is not { } template)
            return Refuse(proposal);
        var before = Current;
        if (!Build(template, proposal, out var after)) return Refuse(proposal);
        var replacement = new Relation(proposal, new Relation(after.Surface, new Relation(before.Surface, after.Surface)));
        Current = after; Revisions = Append(Revisions, after.Surface); Replacements = Append(Replacements, replacement); Pending = S();
        return replacement;
    }

    private AdaptiveMotifGrammar.Template? Template(IElement candidate) => ReferenceEquals(candidate, Grammar.Plus.Surface) ? Grammar.Plus :
        ReferenceEquals(candidate, Grammar.Minus.Surface) ? Grammar.Minus : null;

    private bool Build(AdaptiveMotifGrammar.Template template, IElement cause, out MotifRevision revision)
    {
        revision = null!;
        if (Template(template.Surface) is not { } actual || !ReferenceEquals(actual, template)) return false;
        var before = Drawing.State; var head = before; var receipts = new List<IElement>(); var bindings = new List<IElement>();
        var strokes = new List<IElement>(); var closures = new List<IElement>(); var publications = new List<IElement>(); var compiled = new List<IElement>();
        var identity = new Placeholder();
        foreach (var path in E(template.Paths).Cast<Relation>())
        {
            if (!Step(Drawing.Reset(), path) || !Step(Drawing.Jump((Relation)path.Left), path)) return false;
            Drawing.TryReadState(head, out _, out var start, out _, out _, out _, out _, out _);
            foreach (var chord in E((Relation)path.Right).Cast<Relation>())
            {
                var action = Drawing.Draw(chord); var previous = head;
                if (!Step(action, new Relation(path, chord))) return false;
                compiled.Add(new Relation(new Relation(path, chord), action));
                foreach (var selector in E(template.Ports).Cast<Relation>().Where(row => row.Right is Relation pick &&
                    ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, chord)))
                {
                    Drawing.TryReadState(previous, out _, out var cursor, out _, out _, out _, out _, out _);
                    publications.Add(new Relation(new Relation(Cell, selector.Left), new Relation(identity,
                        new Relation(selector, new Relation(receipts[^1], cursor)))));
                }
            }
            Drawing.TryReadState(head, out _, out var end, out _, out _, out _, out _, out _);
            closures.Add(new Relation(new Relation(end.Left, start.Left), new Relation(path, S(receipts.ToArray()))));
        }
        var sites = strokes.Cast<Relation>().SelectMany(stroke =>
        { QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _); return new[] { a, b }; }).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
        Drawing.TryReadState(head, out _, out _, out _, out _, out var allBindings, out _, out _);
        bindings.AddRange(E(allBindings).Cast<Relation>().Where(row => sites.Any(site => ReferenceEquals(site, row.Left))));
        var graph = new QualitativeStrokeGraph(Grammar.Frame, S(sites), S(strokes.ToArray()));
        var sourceBindings = S(bindings.ToArray()); var local = S(closures.ToArray()); var join = new QualitativeGraphJoin(graph, sourceBindings, local);
        if (!join.TryReadPassed(join.Evaluate()) || publications.Count != E(template.Ports).Length) return false;
        // The complete pure branch was checked before choosing this live head.
        foreach (var receipt in receipts.Cast<Relation>())
            if (!Drawing.TryCommit(receipt)) throw new InvalidOperationException("A preflighted motif branch lost its exact receiver head.");
        revision = new(this, identity, template, before, head, S(receipts.ToArray()), S(publications.ToArray()), graph, sourceBindings, local, join, S(compiled.ToArray()), cause);
        return true;

        bool Step(Relation action, IElement evidence)
        {
            Drawing.TryReadState(head, out var prior, out _, out _, out _, out _, out _, out _);
            var receipt = Drawing.Step(head, Drawing.Offer(Drawing.Source, new Placeholder(), prior, S(action), new Relation(cause, evidence)));
            if (!Drawing.TryReadAccepted(receipt, out var after, out var added)) { Unresolved = Append(Unresolved, receipt); return false; }
            receipts.Add(receipt); strokes.AddRange(E(added)); head = after; return true;
        }
    }

    private Relation Refuse(IElement evidence) { Unresolved = Append(Unresolved, evidence); return new Relation(Surface, StructuralConstraint.Open(evidence)); }
    private static Relation Append(Relation scope, IElement value) => S([.. E(scope), value]);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

public sealed class MotifRevision
{
    public AdaptiveMotifNode Node { get; }
    public IElement Identity { get; }
    public AdaptiveMotifGrammar.Template Template { get; }
    public Relation Before { get; }
    public Relation After { get; }
    public Relation Receipts { get; }
    public Relation Ports { get; }
    public QualitativeStrokeGraph Graph { get; }
    public Relation Bindings { get; }
    public Relation Closures { get; }
    public QualitativeGraphJoin Join { get; }
    public Relation Surface { get; }
    internal MotifRevision(AdaptiveMotifNode node, IElement identity, AdaptiveMotifGrammar.Template template,
        Relation before, Relation after, Relation receipts, Relation ports, QualitativeStrokeGraph graph,
        Relation bindings, Relation closures, QualitativeGraphJoin join, Relation compilation, IElement cause)
    {
        Node = node; Identity = identity; Template = template; Before = before; After = after; Receipts = receipts;
        Ports = ports; Graph = graph; Bindings = bindings; Closures = closures; Join = join;
        Surface = new(new Relation(node.Surface, new Relation(identity, template.Surface)),
            new Relation(new Relation(before, after), new Relation(receipts, new Relation(ports,
                new Relation(join.Surface, new Relation(compilation, cause))))));
    }
    public bool TryReadPort(IElement role, out Relation publication, out Relation binding)
    {
        publication = binding = null!;
        var matches = E(Ports).OfType<Relation>().Where(row => row.Left is Relation owner && ReferenceEquals(owner.Left, Node.Cell) && ReferenceEquals(owner.Right, role)).ToArray();
        if (matches is not [var actual] || actual.Right is not Relation { Left: var revision, Right: Relation { Left: Relation selector, Right: Relation effect } } ||
            !ReferenceEquals(revision, Identity) || !E(Template.Ports).Any(row => ReferenceEquals(row, selector)) ||
            effect.Left is not Relation receipt || !E(Receipts).Any(row => ReferenceEquals(row, receipt)) || effect.Right is not Relation cursor ||
            !Node.Drawing.TryReadStep(receipt, out var before, out _, out _, out _, out _, out _) ||
            !Node.Drawing.TryReadState(before, out _, out var actualCursor, out _, out _, out _, out _, out _) || !ReferenceEquals(actualCursor, cursor)) return false;
        publication = actual; binding = cursor; return true;
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
