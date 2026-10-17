using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A derived connected view of an existing stroke graph. Supplied
/// declarations alone propose joins; formal position equality admits them.
/// The original sites, stroke identities and causes remain unchanged.</summary>
public sealed class QualitativeGraphJoin
{
    // declaration = (actual source site -> actual source site) -> cause
    // accepted = Closed(actual declarations)
    // binding check = declaration -> ((actual binding -> actual binding) -> equality read)
    // class = (Closed(actual member sites) -> quotient site) ->
    //         (Closed(actual accepted declarations) -> Closed(binding checks))
    // site-map = Closed(actual source site -> quotient site)
    // stroke-map = Closed(actual source stroke -> corresponding output stroke)
    // output stroke cause is the actual source stroke, including its history.
    // Surface retains the source, its bindings and all declarations alongside
    // the quotient view, correspondences, accepted/blocked and open obligations.
    public QualitativeStrokeGraph Source { get; }
    public Relation SourceBindings { get; }
    public Relation DeclaredJoins { get; }
    public QualitativeStrokeGraph Graph { get; }
    public Relation Bindings { get; }
    public Relation SiteMap { get; }
    public Relation StrokeMap { get; }
    public Relation Classes { get; }
    public Relation BindingChecks { get; }
    public Relation Accepted { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    private readonly Relation read;

    public QualitativeGraphJoin(QualitativeStrokeGraph source, Relation sourceBindings, Relation declaredJoins)
    {
        Source = source; SourceBindings = sourceBindings; DeclaredJoins = declaredJoins;
        var unresolved = new List<IElement>(); var blocked = new List<IElement>(); var accepted = new List<IElement>();
        var checks = new List<IElement>(); var sites = new List<IElement>();
        var sourceClosed = Closed(source.Sites); var bindingsClosed = Closed(sourceBindings);
        if (!sourceClosed) unresolved.Add(source.Sites);
        if (!Closed(source.Strokes)) unresolved.Add(source.Strokes);
        if (!bindingsClosed) unresolved.Add(sourceBindings);
        if (!Closed(declaredJoins)) unresolved.Add(declaredJoins);
        foreach (var site in E(source.Sites))
            if (sites.Any(actual => Same(actual, site))) unresolved.Add(new Relation(site, source.Sites));
            else sites.Add(site);
        var bindingRows = new Relation?[sites.Count];
        for (var index = 0; index < sites.Count; index++)
        {
            var rows = E(sourceBindings).OfType<Relation>().Where(row => Same(row.Left, sites[index])).ToArray();
            if (!bindingsClosed || rows.Length != 1 || rows[0].Right is not Relation word || !ValidWord(word))
                unresolved.Add(new Relation(sites[index], sourceBindings));
            else bindingRows[index] = rows[0];
        }
        foreach (var row in E(sourceBindings))
            if (row is not Relation binding || !sites.Any(site => Same(site, binding.Left)))
                unresolved.Add(new Relation(row, sourceBindings));
        var parents = Enumerable.Range(0, sites.Count).ToArray();
        foreach (var item in E(declaredJoins))
        {
            if (item is not Relation { Left: Relation pair } declaration)
            { unresolved.Add(new Relation(item, declaredJoins)); continue; }
            var from = sites.FindIndex(site => Same(site, pair.Left)); var to = sites.FindIndex(site => Same(site, pair.Right));
            if (from < 0 || to < 0)
            { blocked.Add(new Relation(declaration, source.Sites)); continue; }
            if (!sourceClosed || bindingRows[from] is not Relation first || bindingRows[to] is not Relation second)
            { unresolved.Add(new Relation(declaration, sourceBindings)); continue; }
            var equality = source.Frame.Reduce((Relation)first.Right, Inverse((Relation)second.Right));
            var witness = new Relation(declaration, new Relation(new Relation(first, second), equality)); checks.Add(witness);
            if (!source.Frame.TryReadResult(equality, out var remaining, out _, out var pending) ||
                !Closed(remaining) || E(pending).Length != 0)
            { unresolved.Add(witness); continue; }
            if (E(remaining).Length != 0) { blocked.Add(witness); continue; }
            accepted.Add(declaration); parents[Find(to)] = Find(from);
        }
        Accepted = S(accepted.ToArray()); Blocked = S(blocked.ToArray()); BindingChecks = S(checks.ToArray());
        var classes = new List<IElement>(); var siteMap = new List<IElement>(); var bindings = new List<IElement>();
        var outputs = new IElement[sites.Count];
        foreach (var group in Enumerable.Range(0, sites.Count).GroupBy(Find))
        {
            var members = group.ToArray(); var memberSites = members.Select(index => sites[index]).ToArray();
            // A singleton reuses its original site. A declared multi-site class
            // has a separate identity, retaining all original identities below.
            IElement target = members.Length == 1 ? memberSites[0] : new Placeholder();
            foreach (var index in members) { outputs[index] = target; siteMap.Add(new Relation(sites[index], target)); }
            var classDeclarations = accepted.OfType<Relation>().Where(row => row.Left is Relation pair &&
                memberSites.Any(site => Same(site, pair.Left)) && memberSites.Any(site => Same(site, pair.Right))).ToArray();
            var classChecks = checks.OfType<Relation>().Where(check => classDeclarations.Any(row => ReferenceEquals(row, check.Left))).ToArray();
            classes.Add(new Relation(new Relation(S(memberSites), target), new Relation(S(classDeclarations), S(classChecks))));
            var available = members.Select(index => bindingRows[index]).FirstOrDefault(row => row is not null);
            if (available is not null) bindings.Add(new Relation(target, available.Right));
        }
        var strokes = new List<IElement>(); var strokeMap = new List<IElement>();
        foreach (var item in E(source.Strokes))
        {
            if (item is not Relation stroke || !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word))
            { unresolved.Add(new Relation(item, source.Surface)); strokes.Add(item); continue; }
            var a = sites.FindIndex(site => Same(site, from)); var b = sites.FindIndex(site => Same(site, to));
            if (a < 0 || b < 0 || !ValidWord(word))
            { unresolved.Add(new Relation(stroke, source.Surface)); strokes.Add(stroke); continue; }
            var output = QualitativeStrokeGraph.Stroke(stroke, outputs[a], outputs[b], word);
            strokes.Add(output); strokeMap.Add(new Relation(stroke, output));
        }
        SiteMap = S(siteMap.ToArray()); StrokeMap = S(strokeMap.ToArray()); Classes = S(classes.ToArray());
        Unresolved = S(unresolved.ToArray());
        var complete = unresolved.Count == 0;
        Bindings = Scope(complete && bindings.Count == classes.Count, bindings.ToArray());
        Graph = new(source.Frame, Scope(sourceClosed, outputs.Where((site, index) =>
                !outputs.Take(index).Any(previous => ReferenceEquals(previous, site))).ToArray()),
            Scope(complete, strokes.ToArray()), source.PeriodWord);
        Surface = new Relation(new Relation(Source.Surface, new Relation(SourceBindings, DeclaredJoins)),
            new Relation(new Relation(Graph.Surface, Bindings), new Relation(new Relation(SiteMap, StrokeMap),
                new Relation(Classes, new Relation(BindingChecks, new Relation(Accepted, new Relation(Blocked, Unresolved)))))));
        read = new Relation(Surface, new Relation(Accepted, new Relation(Blocked, Unresolved)));

        int Find(int index) { while (parents[index] != index) index = parents[index]; return index; }
    }

    public Relation Evaluate() => read;

    /// <summary>Reads this exact captured contract and rechecks input admission,
    /// exact equivalence classes and every copied stroke. A fabricated result
    /// graph or correspondence cannot certify itself through a copied wrapper.</summary>
    public bool TryReadPassed(Relation candidate)
    {
        if (!ReferenceEquals(candidate.Left, Surface) || !ReferenceEquals(candidate.Right, read.Right) ||
            E(Blocked).Length != 0 || E(Unresolved).Length != 0 ||
            !new[] { Source.Sites, Source.Strokes, SourceBindings, DeclaredJoins, Graph.Sites, Graph.Strokes,
                Bindings, SiteMap, StrokeMap, Classes, Accepted, BindingChecks }.All(Closed)) return false;
        var sites = E(Source.Sites); var mapping = E(SiteMap).OfType<Relation>().ToArray();
        if (mapping.Length != sites.Length || E(Graph.Sites).Length != E(Classes).Length || E(Bindings).Length != E(Classes).Length) return false;
        foreach (var site in sites)
            if (mapping.Count(row => Same(row.Left, site)) != 1 || !UniqueBinding(site, out _)) return false;
        var targets = sites.Select(site => mapping.Single(row => Same(row.Left, site)).Right).ToArray();
        var parents = Enumerable.Range(0, sites.Length).ToArray();
        foreach (var item in E(DeclaredJoins))
        {
            if (item is not Relation { Left: Relation pair } row || !E(Accepted).Any(actual => ReferenceEquals(actual, row))) return false;
            var a = Array.FindIndex(sites, site => Same(site, pair.Left)); var b = Array.FindIndex(sites, site => Same(site, pair.Right));
            if (a < 0 || b < 0 || !UniqueBinding(pair.Left, out var first) || !UniqueBinding(pair.Right, out var second) ||
                !Zero(Source.Frame.Reduce((Relation)first.Right, Inverse((Relation)second.Right)))) return false;
            parents[Find(b)] = Find(a);
        }
        for (var i = 0; i < sites.Length; i++)
        {
            var target = targets[i];
            if (!E(Graph.Sites).Any(site => Same(site, target))) return false;
            for (var j = 0; j < sites.Length; j++)
            {
                var other = targets[j];
                if ((Find(i) == Find(j)) != Same(target, other)) return false;
            }
        }
        foreach (var item in E(Classes))
        {
            if (item is not Relation { Left: Relation partition, Right: Relation evidence } ||
                partition.Left is not Relation members || !Closed(members) || evidence.Left is not Relation joins || evidence.Right is not Relation proofs ||
                !Closed(joins) || !Closed(proofs)) return false;
            var expected = mapping.Where(row => Same(row.Right, partition.Right)).Select(row => row.Left).ToArray();
            if (E(members).Length != expected.Length || E(members).Any(member => !expected.Any(site => Same(site, member)))) return false;
            var binding = E(Bindings).OfType<Relation>().Where(row => Same(row.Left, partition.Right)).ToArray();
            if (binding.Length != 1 || binding[0].Right is not Relation outputWord) return false;
            foreach (var member in expected)
                if (!UniqueBinding(member, out var original) || !Zero(Source.Frame.Reduce(outputWord, Inverse((Relation)original.Right)))) return false;
        }
        var originalStrokes = E(Source.Strokes); var copied = E(Graph.Strokes); var strokeMaps = E(StrokeMap);
        if (originalStrokes.Length != copied.Length || copied.Length != strokeMaps.Length) return false;
        for (var i = 0; i < originalStrokes.Length; i++)
        {
            if (originalStrokes[i] is not Relation original || copied[i] is not Relation output || strokeMaps[i] is not Relation row ||
                !ReferenceEquals(row.Left, original) || !ReferenceEquals(row.Right, output) ||
                !QualitativeStrokeGraph.TryReadStroke(original, out _, out var a, out var b, out var word) ||
                !QualitativeStrokeGraph.TryReadStroke(output, out var cause, out var x, out var y, out var outputWord) ||
                !ReferenceEquals(cause, original) || !ReferenceEquals(word, outputWord) ||
                mapping.SingleOrDefault(pair => Same(pair.Left, a)) is not Relation from ||
                mapping.SingleOrDefault(pair => Same(pair.Left, b)) is not Relation to ||
                !Same(from.Right, x) || !Same(to.Right, y)) return false;
        }
        return true;

        int Find(int index) { while (parents[index] != index) index = parents[index]; return index; }
    }

    private bool UniqueBinding(IElement site, out Relation binding)
    {
        binding = null!; var rows = E(SourceBindings).OfType<Relation>().Where(row => Same(row.Left, site)).ToArray();
        if (rows.Length != 1 || rows[0].Right is not Relation word || !ValidWord(word)) return false;
        binding = rows[0]; return true;
    }
    private bool ValidWord(Relation word) => Closed(word) && E(word).All(item => item is Relation move && Source.Frame.Direction(move, out _, out _, out _));
    private Relation Inverse(Relation word)
    {
        var output = new List<IElement>(); var complete = Closed(word);
        foreach (var item in E(word))
            if (item is Relation move && E(Source.Frame.Opposites).OfType<Relation>().FirstOrDefault(pair => ReferenceEquals(pair.Left, move.Left)) is Relation opposite)
                output.Add(QualitativeMotionFrame.Move(opposite.Right, move.Right));
            else { complete = false; output.Add(item); }
        return Scope(complete, output.ToArray());
    }
    private bool Zero(Relation equality) => Source.Frame.TryReadResult(equality, out var remaining, out _, out var pending) &&
        Closed(remaining) && E(remaining).Length == 0 && E(pending).Length == 0;
    private static Relation Scope(bool complete, params IElement[] values) => complete ? S(values) : StructuralConstraint.Open(values);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static bool Same(IElement first, IElement second) => ReferenceEquals(first, second) || first is Placeholder a && second is Placeholder b && a.Equals(b);
}
