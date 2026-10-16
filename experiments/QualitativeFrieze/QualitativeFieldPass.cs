using System.Runtime.CompilerServices;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A finite, non-draining field interpretation. The actual binary law
/// determines each selected cell; the two roles remain symmetric participants.</summary>
public sealed class QualitativeFieldPass
{
    // Surface = ((source.Surface -> tool.Surface) -> law) -> (focus -> goal)
    // read = Surface -> (result.Surface ->
    //                    ((complement -> applications) ->
    //                     ((source-read -> tool-read) -> (unresolved -> timeline))))
    // application = cell -> ((actual-source-label -> actual-tool-label) ->
    //                        (actual-finite-law-read -> actual-result-label))
    // complement = (outside-cells -> actual-outside-source-labels) ->
    //              (outside-interior-edges -> crossing-cut-edges)
    // timeline = prior-read-or-source.Surface -> (Surface -> result.Surface)
    // Ordered pass history is explicit provenance, not an autonomous clock,
    // mutation, conserved material transfer or a claim about the active head.
    public QualitativePartition Source { get; }
    public QualitativePartition Tool { get; }
    public Relation Law { get; }
    public Relation Focus { get; }
    public IElement Goal { get; }
    public Relation? Previous { get; }
    public Relation Surface { get; }
    public QualitativePartition Result { get; }
    public Relation Timeline { get; }
    public Relation SourceRead { get; }
    public Relation ToolRead { get; }
    private readonly Relation read;
    private readonly Relation complement;
    private readonly Relation applications;
    private readonly Relation unresolved;

    public QualitativeFieldPass(QualitativePartition source, QualitativePartition tool,
        Relation law, Relation? focus = null, IElement? goal = null, Relation? previous = null)
    {
        Source = source; Tool = tool; Law = law; Focus = focus ?? source.Cells;
        Goal = goal ?? new Placeholder(); Previous = previous;
        Surface = new Relation(new Relation(new Relation(source.Surface, tool.Surface), law), new Relation(Focus, Goal));
        SourceRead = source.Evaluate(); ToolRead = tool.Evaluate();
        var pending = new List<IElement>(); var applied = new List<IElement>();
        FieldNative.Read(source.Cells, out var cells);
        var selected = FieldNative.Read(Focus, out var focusCells) &&
            FieldNative.Distinct(focusCells) && focusCells.All(cell => cells.Any(candidate => FieldNative.Same(cell, candidate)));
        var shared = ReferenceEquals(source.Cells, tool.Cells) && ReferenceEquals(source.Adjacency, tool.Adjacency) &&
            ReferenceEquals(source.RolePair, tool.RolePair);
        var domain = FieldNative.Read(source.Cells, out _) && FieldNative.Distinct(cells);
        var legalLaw = DomainLaw(law, source.RolePair);
        Relation? priorPending = null;
        var prior = previous is null || PreviousResult(previous, source.Surface, out priorPending);
        if (!selected) pending.Add(new Relation(Focus, source.Cells));
        if (!shared) pending.Add(new Relation(source.Surface, tool.Surface));
        if (!domain) pending.Add(new Relation(source.Cells, Surface));
        if (!legalLaw) pending.Add(new Relation(law, source.RolePair));
        if (!prior) pending.Add(new Relation(previous!, source.Surface));
        else if (priorPending is not null) pending.AddRange(FieldNative.E(priorPending));
        var chosen = selected ? focusCells.ToHashSet(FieldNative.Participants.Instance) : new HashSet<IElement>(FieldNative.Participants.Instance);
        var outside = selected ? cells.Where(cell => !chosen.Contains(cell)).ToArray() : [];
        var outsideClosed = selected && domain;
        FieldNative.Read(source.Labels, out var sourceRows);
        FieldNative.Read(source.Adjacency, out var edges);
        var outsideSet = outside.ToHashSet(FieldNative.Participants.Instance);
        var outsideLabels = sourceRows.OfType<Relation>().Where(row => outsideSet.Contains(row.Left)).Cast<IElement>().ToArray();
        var insideEdges = edges.OfType<Relation>().Where(edge => outsideSet.Contains(edge.Left) && outsideSet.Contains(edge.Right)).Cast<IElement>().ToArray();
        var cutEdges = edges.OfType<Relation>().Where(edge => chosen.Contains(edge.Left) != chosen.Contains(edge.Right)).Cast<IElement>().ToArray();
        complement = new Relation(new Relation(FieldNative.Scope(outsideClosed, outside),
                FieldNative.Scope(outsideClosed && FieldNative.Read(source.Labels, out _), outsideLabels)),
            new Relation(FieldNative.Scope(outsideClosed && FieldNative.Read(source.Adjacency, out _), insideEdges),
                FieldNative.Scope(outsideClosed && FieldNative.Read(source.Adjacency, out _), selected ? cutEdges : [])));
        // Input reads remain connected even when known local labels can still
        // be evaluated. Missing/open/conflicting labels cannot become defaults.
        if (source.TryReadResult(SourceRead, out _, out _, out _, out _, out var sourcePending))
            pending.AddRange(FieldNative.E(sourcePending));
        else pending.Add(SourceRead);
        if (tool.TryReadResult(ToolRead, out _, out _, out _, out _, out var toolPending))
            pending.AddRange(FieldNative.E(toolPending));
        else pending.Add(ToolRead);

        if (!selected || !shared || !domain || !legalLaw || !prior)
            Result = source;
        else
        {
            var labels = outsideLabels.ToList();
            foreach (var cell in focusCells)
            {
                var a = FieldNative.Label(source, cell); var b = FieldNative.Label(tool, cell);
                if (a is null || b is null)
                { pending.Add(new Relation(cell, new Relation(source.Labels, tool.Labels))); continue; }
                var lawRead = FiniteLaw.Evaluate(law, FieldNative.S(a.Right), FieldNative.S(b.Right));
                if (!FiniteLaw.TryReadResult(lawRead, out var support, out var residue, out var consumed) ||
                    !FieldNative.Read(support, out var outcomes) || outcomes.Length != 1 ||
                    !FieldNative.IsRole(source.RolePair, outcomes[0]) || FieldNative.E(residue).Length != 0 || FieldNative.E(consumed).Length == 0)
                { pending.Add(new Relation(cell, lawRead)); continue; }
                var output = new Relation(cell, outcomes[0]); labels.Add(output);
                applied.Add(new Relation(cell, new Relation(new Relation(a, b), new Relation(lawRead, output))));
            }
            Result = new QualitativePartition(source.Cells, source.Adjacency, source.RolePair,
                FieldNative.Scope(FieldNative.Read(source.Labels, out _), labels.ToArray()));
        }
        applications = FieldNative.S(applied.ToArray());
        // The prior read and both complete input reads remain inspectable, even
        // if a focus leaves uncertain source participation outside the edit.
        unresolved = FieldNative.S(pending.ToArray());
        Timeline = new Relation(previous ?? source.Surface, new Relation(Surface, Result.Surface));
        read = new Relation(Surface, new Relation(Result.Surface,
            new Relation(new Relation(complement, applications),
                new Relation(new Relation(SourceRead, ToolRead), new Relation(unresolved, Timeline)))));
    }

    public Relation Evaluate() => read;

    /// <summary>Reads this immutable derivation, including all its native
    /// obligations. A copied outer shell is allowed; replaced evidence is not.</summary>
    public bool TryReadResult(Relation candidate, out Relation resultSurface, out Relation retainedComplement,
        out Relation applied, out Relation pending, out Relation timeline)
    {
        resultSurface = retainedComplement = applied = pending = timeline = null!;
        if (!ReferenceEquals(candidate.Left, Surface) || candidate.Right is not Relation result ||
            !ReferenceEquals(result.Left, Result.Surface) || result.Right is not Relation { Left: Relation body, Right: Relation evidence } ||
            !ReferenceEquals(body.Left, complement) || !ReferenceEquals(body.Right, applications) ||
            evidence.Left is not Relation inputs || !ReferenceEquals(inputs.Left, SourceRead) || !ReferenceEquals(inputs.Right, ToolRead) ||
            evidence.Right is not Relation history ||
            !ReferenceEquals(history.Left, unresolved) || !ReferenceEquals(history.Right, Timeline)) return false;
        resultSurface = Result.Surface; retainedComplement = complement; applied = applications; pending = unresolved; timeline = Timeline;
        return true;
    }

    public bool TryReadApplication(Relation application, out IElement cell, out Relation sourceLabel,
        out Relation toolLabel, out Relation lawRead, out Relation resultLabel)
    {
        cell = null!; sourceLabel = toolLabel = lawRead = resultLabel = null!;
        if (!FieldNative.E(applications).Any(item => ReferenceEquals(item, application)) ||
            application.Right is not Relation { Left: Relation inputs, Right: Relation output } ||
            inputs.Left is not Relation a || inputs.Right is not Relation b ||
            output.Left is not Relation lawResult || output.Right is not Relation label) return false;
        cell = application.Left; sourceLabel = a; toolLabel = b; lawRead = lawResult; resultLabel = label; return true;
    }

    /// <summary>Projects actual causal supports for one result role. Two routes
    /// to the very same label are one witness; distinct contributing labels stay
    /// distinct. This records support multiplicity, not conserved material.</summary>
    public Relation Supports(IElement selectedRole)
    {
        FieldNative.Read(Result.Cells, out var cells); var rows = new List<IElement>();
        foreach (var cell in cells)
        {
            var label = FieldNative.Label(Result, cell);
            if (!FieldNative.IsRole(Result.RolePair, selectedRole) || label is null)
            { rows.Add(new Relation(cell, StructuralConstraint.Open())); continue; }
            if (!FieldNative.Same(label.Right, selectedRole))
            { rows.Add(new Relation(cell, FieldNative.S())); continue; }
            var app = FieldNative.E(applications).OfType<Relation>().FirstOrDefault(item => FieldNative.Same(item.Left, cell));
            if (app is null) { rows.Add(new Relation(cell, FieldNative.S(label))); continue; }
            TryReadApplication(app, out _, out var a, out var b, out _, out _);
            var evidence = new List<IElement>();
            if (FieldNative.Same(a.Right, selectedRole)) evidence.Add(a);
            if (FieldNative.Same(b.Right, selectedRole) && !evidence.Any(item => FieldNative.Same(item, b))) evidence.Add(b);
            if (evidence.Count == 0) evidence.Add(app);
            rows.Add(new Relation(cell, FieldNative.S(evidence.ToArray())));
        }
        return FieldNative.Scope(FieldNative.Read(Result.Cells, out _), rows.ToArray());
    }

    public static Relation UnionLaw(Relation roles, IElement selectedRole) => MakeLaw(roles, selectedRole, (a, b) => a || b);
    public static Relation IntersectionLaw(Relation roles, IElement selectedRole) => MakeLaw(roles, selectedRole, (a, b) => a && b);
    public static Relation DifferenceLaw(Relation roles, IElement selectedRole) => MakeLaw(roles, selectedRole, (a, b) => a && !b);

    private static Relation MakeLaw(Relation roles, IElement selectedRole, Func<bool, bool, bool> choose)
    {
        if (!FieldNative.IsRole(roles, selectedRole) || FieldNative.Same(roles.Left, roles.Right))
            return new Relation(new Relation(roles, selectedRole), StructuralConstraint.Open());
        var other = FieldNative.Same(roles.Left, selectedRole) ? roles.Right : roles.Left;
        var states = new[] { roles.Left, roles.Right }; var domain = FieldNative.S(states);
        return FiniteLaw.Create(domain, domain, domain,
            states.SelectMany(a => states.Select(b => FiniteLaw.Row(a, b,
                FieldNative.S(choose(FieldNative.Same(a, selectedRole), FieldNative.Same(b, selectedRole)) ? selectedRole : other)))).ToArray());
    }

    private static bool DomainLaw(Relation law, Relation roles) => !FieldNative.Same(roles.Left, roles.Right) &&
        FiniteLaw.TryRead(law, out var left, out var right, out var output, out _) &&
        new[] { left, right, output }.All(scope => FieldNative.Read(scope, out var entries) && entries.Length == 2 &&
            FieldNative.Distinct(entries) && entries.All(entry => FieldNative.IsRole(roles, entry)));

    // This checks a readable prior derivation and carries its obligations. It
    // does not authenticate an author or assert a unique active history head.
    private static bool PreviousResult(Relation previous, Relation source, out Relation? pending)
    {
        pending = null;
        if (previous.Left is not Relation surface ||
            surface.Left is not Relation { Left: Relation inputs, Right: Relation law } ||
            inputs.Left is not Relation from || inputs.Right is not Relation tool ||
            surface.Right is not Relation { Left: Relation focus } || !FieldNative.Syntax(focus) ||
            !PartitionSurface(from, out var cells, out var edges, out var roles, out _) ||
            !PartitionSurface(tool, out var toolCells, out var toolEdges, out var toolRoles, out _) ||
            !ReferenceEquals(cells, toolCells) || !ReferenceEquals(edges, toolEdges) || !ReferenceEquals(roles, toolRoles) ||
            !DomainLaw(law, roles) || previous.Right is not Relation result || !ReferenceEquals(result.Left, source) ||
            !PartitionSurface(source, out var resultCells, out var resultEdges, out var resultRoles, out _) ||
            !ReferenceEquals(cells, resultCells) || !ReferenceEquals(edges, resultEdges) || !ReferenceEquals(roles, resultRoles) ||
            result.Right is not Relation { Left: Relation body, Right: Relation proof } ||
            body.Left is not Relation complement || body.Right is not Relation applications || !FieldNative.Read(applications, out var apps) ||
            complement.Left is not Relation retained || complement.Right is not Relation cut ||
            retained.Left is not Relation outside || retained.Right is not Relation labels ||
            cut.Left is not Relation interior || cut.Right is not Relation boundary ||
            !new[] { outside, labels, interior, boundary }.All(FieldNative.Syntax) ||
            proof.Left is not Relation { Left: Relation fromRead, Right: Relation toolRead } ||
            !PartitionRead(fromRead, from, roles) || !PartitionRead(toolRead, tool, roles) ||
            proof.Right is not Relation { Left: Relation unresolved, Right: Relation timeline } ||
            !FieldNative.Read(unresolved, out _) || timeline.Right is not Relation action ||
            !ReferenceEquals(action.Left, surface) || !ReferenceEquals(action.Right, source)) return false;
        foreach (var app in apps)
            if (app is not Relation { Right: Relation application } ||
                application.Left is not Relation { Left: Relation, Right: Relation } ||
                application.Right is not Relation { Left: Relation lawRead, Right: Relation } ||
                lawRead.Left is not Relation { Left: Relation appliedLaw } || !ReferenceEquals(appliedLaw.Left, law) ||
                !FiniteLaw.TryReadResult(lawRead, out _, out _, out _)) return false;
        // The first predecessor is its actual source surface; later histories
        // carry a prior result pointing at that same source surface.
        if (!ReferenceEquals(timeline.Left, from) &&
            (timeline.Left is not Relation earlier || earlier.Right is not Relation earlierResult || !ReferenceEquals(earlierResult.Left, from))) return false;
        pending = unresolved; return true;
    }

    private static bool PartitionSurface(Relation surface, out Relation cells, out Relation edges, out Relation roles, out Relation labels)
    {
        cells = edges = roles = labels = null!;
        if (surface.Left is not Relation { Left: Relation c, Right: Relation e } ||
            surface.Right is not Relation { Left: Relation l, Right: Relation r } ||
            !FieldNative.Syntax(c) || !FieldNative.Syntax(e) || !FieldNative.Syntax(l)) return false;
        cells = c; edges = e; roles = r; labels = l; return true;
    }

    private static bool PartitionRead(Relation read, Relation surface, Relation roles)
    {
        if (!ReferenceEquals(read.Left, surface) || read.Right is not Relation { Left: Relation groups, Right: Relation geometry } ||
            groups.Left is not Relation { Left: var firstRole, Right: Relation first } ||
            groups.Right is not Relation { Left: var secondRole, Right: Relation second } ||
            !FieldNative.Same(firstRole, roles.Left) || !FieldNative.Same(secondRole, roles.Right) ||
            geometry.Left is not Relation regions || geometry.Right is not Relation { Left: Relation interfaces, Right: Relation unresolved }) return false;
        return new[] { first, second, regions, interfaces }.All(FieldNative.Syntax) && FieldNative.Read(unresolved, out _);
    }
}

// These are derived indexes/readers of explicit native participants. No role,
// operation, coordinate or adjacency meaning is assigned by a class-name tag.
internal static class FieldNative
{
    internal static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    internal static Relation Scope(bool closed, params IElement[] entries) => closed ? S(entries) : StructuralConstraint.Open(entries);
    internal static bool Read(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    internal static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    internal static bool Syntax(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out _, out _);
    internal static bool IsRole(Relation pair, IElement role) => Same(pair.Left, role) || Same(pair.Right, role);
    internal static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation p && b is CarrierLocation q && p.Carrier.SameIdentityAs(q.Carrier) && p.SameTickRefAs(q);
    internal static bool Distinct(IElement[] entries) => entries.Distinct(Participants.Instance).Count() == entries.Length;
    internal static Relation? Label(QualitativePartition field, IElement cell)
    {
        if (!Read(field.Labels, out var entries) || Same(field.RolePair.Left, field.RolePair.Right)) return null;
        var rows = entries.OfType<Relation>().Where(row => Same(row.Left, cell)).ToArray();
        return rows.Length == 1 && IsRole(field.RolePair, rows[0].Right) ? rows[0] : null;
    }
    internal sealed class Participants : IEqualityComparer<IElement>
    {
        internal static readonly Participants Instance = new();
        public bool Equals(IElement? x, IElement? y) => x is not null && y is not null && Same(x, y);
        public int GetHashCode(IElement value) => value switch
        {
            Placeholder slot => slot.GetHashCode(),
            CarrierLocation site => HashCode.Combine(site.Carrier.Id, RuntimeHelpers.GetHashCode(site.Tick)),
            _ => RuntimeHelpers.GetHashCode(value)
        };
    }
}
