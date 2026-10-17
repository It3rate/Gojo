using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A bounded two-direction patch generated through actual seam rows.
/// Site identities are joined by these rows before positions are interpreted;
/// equal formal displacement alone never fuses sites.</summary>
public sealed class QualitativeTilePatch
{
    public Relation Tiles { get; }
    public Relation HorizontalSteps { get; }
    public Relation VerticalSteps { get; }
    public Relation Placements { get; }
    public Relation SeamApplications { get; }
    public Relation SiteCorrespondence { get; }
    public Relation BindingChecks { get; }
    public Relation CommutingCorners { get; }
    public QualitativeSquareStudy.Pattern Pattern { get; }
    public Relation Surface => Pattern.Surface;

    public QualitativeTilePatch(QualitativeSquareStudy study, QualitativeSquareStudy.Pattern source, int columns, int rows)
    {
        if (columns is < 1 or > 6 || rows is < 1 or > 6 || !study.Cell.TryReadPassed(study.CellRead))
            throw new ArgumentException("A bounded patch requires a verified cell and one to six occurrences on each axis.");
        if (!ReferenceEquals(source.Graph.Frame, study.Frame) || !ReferenceEquals(source.Bindings, study.SiteBindings))
            throw new ArgumentException("The patch needs this cell's exposed source landmark bindings.");
        if (!Closed(source.Graph.Sites) || !Closed(source.Graph.Strokes))
            throw new ArgumentException("Bounded patch expansion requires closed source site and stroke scopes.");
        var baseSites = E(source.Bindings).Cast<Relation>().Select(row => row.Left).ToArray();
        var declaredSites = E(source.Graph.Sites);
        var siteSet = new HashSet<IElement>(declaredSites, ReferenceEqualityComparer.Instance);
        if (siteSet.Count != declaredSites.Length || declaredSites.Length != baseSites.Length || baseSites.Any(site => !siteSet.Contains(site)))
            throw new ArgumentException("The patch needs the complete exposed source landmark domain.");
        var sourceStrokes = E(source.Graph.Strokes);
        if (sourceStrokes.Any(stroke => stroke is not Relation row || !QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out _) ||
            !siteSet.Contains(from) || !siteSet.Contains(to)))
            throw new ArgumentException("Every source stroke endpoint must belong to the exposed landmark domain.");
        var tile = new IElement[columns, rows]; var x = new IElement[columns]; var y = new IElement[rows];
        for (var i = 0; i < columns; i++) x[i] = new Placeholder(); for (var j = 0; j < rows; j++) y[j] = new Placeholder();
        var tiles = new List<IElement>(); var horizontal = new List<IElement>(); var vertical = new List<IElement>(); var placements = new List<IElement>();
        var applications = new List<IElement>(); var participants = new Dictionary<(int, int, IElement), Relation>();
        var parent = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < columns; i++) for (var j = 0; j < rows; j++)
        {
            var identity = tile[i, j] = new Placeholder(); tiles.Add(new Relation(new Relation(x[i], y[j]), identity));
            var word = S([.. Enumerable.Range(0, i).SelectMany(_ => E(study.Cell.XWord)), .. Enumerable.Range(0, j).SelectMany(_ => E(study.Cell.YWord))]);
            placements.Add(new Relation(identity, word));
            foreach (var site in baseSites) { var entry = new Relation(identity, site); participants.Add((i, j, site), entry); parent.Add(entry, entry); }
        }
        for (var i = 0; i < columns; i++) for (var j = 0; j < rows; j++)
        {
            if (i + 1 < columns) Link(i, j, i + 1, j, study.Cell.SeamX, horizontal);
            if (j + 1 < rows) Link(i, j, i, j + 1, study.Cell.SeamY, vertical);
        }
        var patchSites = new Dictionary<Relation, IElement>(ReferenceEqualityComparer.Instance); var correspondence = new List<IElement>();
        var bindings = new List<IElement>(); var checks = new List<IElement>(); var mapped = new Dictionary<(int, int, IElement), IElement>();
        foreach (var ((i, j, site), occurrence) in participants)
        {
            var root = Find(occurrence);
            if (!patchSites.TryGetValue(root, out var target)) patchSites.Add(root, target = i == 0 && j == 0 && ReferenceEquals(site, study.Frame.Origin) ? study.Frame.Origin : new Placeholder());
            mapped.Add((i, j, site), target); correspondence.Add(new Relation(occurrence, target));
            var local = E(source.Bindings).Cast<Relation>().Single(binding => ReferenceEquals(binding.Left, site));
            var placement = placements.Cast<Relation>().Single(row => ReferenceEquals(row.Left, tile[i, j]));
            var addition = study.Frame.Reduce((Relation)placement.Right, (Relation)local.Right); study.Frame.TryReadResult(addition, out var position, out _, out _);
            var existing = bindings.Cast<Relation>().SingleOrDefault(binding => ReferenceEquals(binding.Left, target));
            if (existing is null) bindings.Add(new Relation(target, position));
            else
            {
                var comparison = study.Frame.Reduce(position, study.Inverse((Relation)existing.Right));
                checks.Add(new Relation(new Relation(occurrence, new Relation(local, placement)), comparison));
                if (!study.Frame.TryReadResult(comparison, out var rest, out _, out var pending) || E(rest).Length != 0 || E(pending).Length != 0)
                    throw new InvalidOperationException("Explicitly joined seam sites disagree at the formal displacement read.");
            }
        }
        var strokes = new List<IElement>();
        for (var i = 0; i < columns; i++) for (var j = 0; j < rows; j++) foreach (var stroke in sourceStrokes.Cast<Relation>())
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out var word);
            strokes.Add(QualitativeStrokeGraph.Stroke(new Relation(tile[i, j], stroke), mapped[(i, j, a)], mapped[(i, j, b)], word));
        }
        var corners = new List<IElement>();
        for (var i = 0; i + 1 < columns; i++) for (var j = 0; j + 1 < rows; j++)
        {
            var a = tile[i, j]; var b = tile[i + 1, j]; var c = tile[i + 1, j + 1]; var d = tile[i, j + 1];
            Relation Edge(List<IElement> domain, IElement from, IElement to) => domain.Cast<Relation>().Single(row => row.Left is Relation pair && ReferenceEquals(pair.Left, from) && ReferenceEquals(pair.Right, to));
            corners.Add(new Relation(S(Edge(horizontal, a, b), Edge(vertical, b, c)), S(Edge(vertical, a, d), Edge(horizontal, d, c))));
        }
        Tiles = new Relation(new Relation(S(x), S(y)), S(tiles.ToArray())); HorizontalSteps = S(horizontal.ToArray()); VerticalSteps = S(vertical.ToArray());
        Placements = S(placements.ToArray()); SeamApplications = S(applications.ToArray()); SiteCorrespondence = S(correspondence.ToArray());
        BindingChecks = S(checks.ToArray()); CommutingCorners = S(corners.ToArray());
        var graph = new QualitativeStrokeGraph(study.Frame, S(bindings.Cast<Relation>().Select(row => row.Left).ToArray()), S(strokes.ToArray()));
        var topology = new Relation(SeamApplications, new Relation(SiteCorrespondence, new Relation(BindingChecks, CommutingCorners)));
        var execution = new Relation(new Relation(HorizontalSteps, VerticalSteps), new Relation(Placements, topology));
        var native = new Relation(new Relation(source.Surface, study.Cell.Surface),
            new Relation(Tiles, new Relation(execution, new Relation(graph.Surface, S(bindings.ToArray())))));
        Pattern = new(source.Name + " / two-direction patch", graph, S(bindings.ToArray()), source.Actions, SiteCorrespondence, native);

        Relation Find(Relation item) { while (!ReferenceEquals(parent[item], item)) item = parent[item]; return item; }
        void Link(int i, int j, int k, int l, Relation seam, List<IElement> steps)
        {
            var adjacency = new Relation(new Relation(tile[i, j], tile[k, l]), seam); steps.Add(adjacency);
            foreach (var row in E((Relation)seam.Right).Cast<Relation>())
            {
                var before = participants[(i, j, row.Right)]; var after = participants[(k, l, row.Left)];
                applications.Add(new Relation(adjacency, new Relation(row, new Relation(before, after))));
                parent[Find(after)] = Find(before);
            }
        }
    }
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
