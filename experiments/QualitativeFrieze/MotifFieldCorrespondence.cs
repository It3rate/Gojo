using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// Recognizes an explicitly focused labeled topology inside another field.
/// Candidate maps are supplied evidence, not an inferred unique inverse or a
/// reconstruction of causal strokes. A partial focus compares induced directed
/// support; its cut edges and both unselected complements remain inspectable.
/// </summary>
public sealed class MotifFieldCorrespondence
{
    // Surface = template.Surface -> selected-cells
    // read = (Surface -> (field.Surface -> (role-map -> candidate-maps))) ->
    //        (template-complement -> (candidate-reads -> unresolved))
    // candidate = ((Surface -> (field.Surface -> role-map)) -> actual-cell-map) ->
    //             (field-complement -> (matched -> (unmatched -> unresolved)))
    // complement = (unselected-cells -> actual-unselected-labels) ->
    //              (actual-unselected-edges -> actual-cut-edges)
    // label match = (actual-cell-map-row -> actual-role-map-row) ->
    //               (actual-template-label -> actual-field-label)
    // support match = actual-edge -> (mapped-edge -> Closed(actual-matching-edges))
    public QualitativePartition Template { get; }
    public Relation Focus { get; }
    public Relation Surface { get; }

    public MotifFieldCorrespondence(QualitativePartition template, Relation focus)
    {
        Template = template; Focus = focus; Surface = new Relation(template.Surface, focus);
    }

    public Relation Evaluate(QualitativePartition field, Relation roleMap, Relation candidateMaps)
    {
        var pending = new List<IElement>(); var basis = BasisOf(Template, pending);
        var selected = Selected(basis, pending);
        var candidates = new List<IElement>();
        if (!ReadScope(candidateMaps, out var offered)) pending.Add(new Relation(candidateMaps, Surface));
        foreach (var value in offered)
            if (value is Relation map) candidates.Add(EvaluateCandidate(field, roleMap, map));
            else pending.Add(new Relation(value, candidateMaps));
        return new Relation(new Relation(Surface, new Relation(field.Surface, new Relation(roleMap, candidateMaps))),
            new Relation(Complement(basis, selected, ClosedFocus(basis)), new Relation(S(candidates.ToArray()), S(pending.ToArray()))));
    }

    public bool TryReadResult(Relation read, out Relation templateComplement, out Relation candidates, out Relation unresolved)
    {
        templateComplement = candidates = unresolved = null!;
        if (read.Left is not Relation request || !ReferenceEquals(request.Left, Surface) ||
            request.Right is not Relation { Left: Relation field, Right: Relation { Left: Relation roles, Right: Relation maps } } ||
            read.Right is not Relation { Left: Relation complement, Right: Relation { Left: Relation outcomes, Right: Relation pending } } ||
            !IsComplement(complement) || !ReadScope(outcomes, out var entries) || !ReadScope(pending, out _)) return false;
        var offered = Entries(maps).OfType<Relation>().ToArray();
        if (entries.Length != offered.Length) return false;
        for (var index = 0; index < entries.Length; index++)
        {
            var value = entries[index];
            if (value is not Relation candidate || !TryReadCandidate(candidate, out var actualField, out var actualRoles,
                out var actualMap, out _, out _, out _, out _) || !ReferenceEquals(actualField, field) || !ReferenceEquals(actualRoles, roles) ||
                !ReferenceEquals(offered[index], actualMap)) return false;
        }
        templateComplement = complement; candidates = outcomes; unresolved = pending; return true;
    }

    public bool TryReadCandidate(Relation candidate, out Relation fieldSurface, out Relation roleMap, out Relation cellMap,
        out Relation fieldComplement, out Relation matched, out Relation unmatched, out Relation unresolved)
    {
        fieldSurface = roleMap = cellMap = fieldComplement = matched = unmatched = unresolved = null!;
        if (candidate.Left is not Relation { Left: Relation context, Right: Relation map } || !ReferenceEquals(context.Left, Surface) ||
            context.Right is not Relation { Left: Relation field, Right: Relation roles } ||
            candidate.Right is not Relation { Left: Relation complement, Right: Relation { Left: Relation yes,
                Right: Relation { Left: Relation no, Right: Relation pending } } } ||
            !IsComplement(complement) || !ReadScope(yes, out _) || !ReadScope(no, out _) || !ReadScope(pending, out _)) return false;
        fieldSurface = field; roleMap = roles; cellMap = map; fieldComplement = complement;
        matched = yes; unmatched = no; unresolved = pending; return true;
    }

    /// <summary>Rechecks the native map, focused source and retained evidence.
    /// Derived wrappers may be copied; actual label, map and topology rows must
    /// remain the source participants. Empty failure or match scopes cannot
    /// manufacture a certificate.</summary>
    public bool TryReadPassed(QualitativePartition field, Relation candidate)
    {
        if (!TryReadCandidate(candidate, out var actualField, out var roles, out var map, out var complement,
            out var matched, out var no, out var pending) || !ReferenceEquals(actualField, field.Surface) ||
            Entries(no).Length != 0 || Entries(pending).Length != 0) return false;
        return TryReadCandidate(EvaluateCandidate(field, roles, map), out _, out _, out _, out var freshComplement,
            out var freshMatched, out var freshNo, out var freshPending) && Entries(freshNo).Length == 0 && Entries(freshPending).Length == 0 &&
            SameComplement(complement, freshComplement) && SameMatches(field, matched, freshMatched);
    }

    /// <summary>Connects roles in their supplied order. Passing a reversed target
    /// pair expresses opposite-role recognition with the same reader.</summary>
    public static Relation RoleCorrespondence(Relation sourcePair, Relation targetPair) =>
        S(new Relation(sourcePair.Left, targetPair.Left), new Relation(sourcePair.Right, targetPair.Right));

    private Relation EvaluateCandidate(QualitativePartition field, Relation roleMap, Relation cellMap)
    {
        var matched = new List<IElement>(); var unmatched = new List<IElement>(); var pending = new List<IElement>();
        var source = BasisOf(Template, pending); var target = BasisOf(field, pending); var selected = Selected(source, pending);
        var roleRows = ReadRoleMap(field, roleMap, unmatched, pending);
        if (!ReadScope(cellMap, out var rawMap)) pending.Add(new Relation(cellMap, Surface));
        var offered = new List<Relation>();
        foreach (var value in rawMap)
            if (value is Relation row && Has(selected, row.Left) && Has(target.Cells, row.Right)) offered.Add(row);
            else unmatched.Add(new Relation(value, new Relation(Focus, field.Cells)));
        var map = new List<Relation>();
        foreach (var cell in selected)
        {
            var rows = offered.Where(row => Same(row.Left, cell)).ToArray();
            if (rows.Length == 0) { pending.Add(new Relation(cell, cellMap)); continue; }
            if (rows.Length != 1) { unmatched.Add(new Relation(cell, S(rows))); continue; }
            map.Add(rows[0]);
        }
        foreach (var row in map)
        {
            var aliases = map.Where(other => Same(other.Right, row.Right)).ToArray();
            if (aliases.Length != 1) { unmatched.Add(new Relation(S(aliases), row.Right)); continue; }
            var a = Label(source, row.Left, pending); var b = Label(target, row.Right, pending);
            if (a is null || b is null) continue;
            var role = roleRows.SingleOrDefault(role => Same(role.Left, a.Right));
            if (role is null) { pending.Add(new Relation(row, roleMap)); continue; }
            var witness = new Relation(new Relation(row, role), new Relation(a, b));
            (Same(role.Right, b.Right) ? matched : unmatched).Add(witness);
        }
        var image = Distinct(map.Select(row => row.Right));
        foreach (var edge in source.Edges.Where(edge => Has(selected, edge.Left) && Has(selected, edge.Right)))
        {
            var a = map.SingleOrDefault(row => Same(row.Left, edge.Left)); var b = map.SingleOrDefault(row => Same(row.Left, edge.Right));
            if (a is null || b is null) { pending.Add(new Relation(edge, cellMap)); continue; }
            var expected = new Relation(a.Right, b.Right);
            var actual = target.Edges.Where(item => Same(item.Left, expected.Left) && Same(item.Right, expected.Right)).ToArray();
            var witness = new Relation(edge, new Relation(expected, S(actual)));
            (actual.Length != 0 ? matched : unmatched).Add(witness);
        }
        // Reverse support rules out an extra edge inside the candidate image.
        // Edges crossing its boundary belong to the explicit complement.
        foreach (var edge in target.Edges.Where(edge => Has(image, edge.Left) && Has(image, edge.Right)))
        {
            var a = map.Where(row => Same(row.Right, edge.Left)).ToArray(); var b = map.Where(row => Same(row.Right, edge.Right)).ToArray();
            if (a.Length != 1 || b.Length != 1) { unmatched.Add(new Relation(edge, cellMap)); continue; }
            var expected = new Relation(a[0].Left, b[0].Left);
            var actual = source.Edges.Where(item => Same(item.Left, expected.Left) && Same(item.Right, expected.Right)).ToArray();
            var witness = new Relation(edge, new Relation(expected, S(actual)));
            (actual.Length != 0 ? matched : unmatched).Add(witness);
        }
        var imageClosed = ClosedFocus(source) && ReadScope(cellMap, out _) && rawMap.Length == map.Count &&
            map.Count == selected.Length && image.Length == map.Count;
        return new Relation(new Relation(new Relation(Surface, new Relation(field.Surface, roleMap)), cellMap),
            new Relation(Complement(target, image, imageClosed), new Relation(S(matched.ToArray()), new Relation(S(unmatched.ToArray()), S(pending.ToArray())))));
    }

    private Relation[] ReadRoleMap(QualitativePartition field, Relation roleMap, List<IElement> unmatched, List<IElement> pending)
    {
        if (!ReadScope(roleMap, out var raw)) pending.Add(new Relation(roleMap, Surface));
        var admitted = new List<Relation>();
        foreach (var value in raw)
            if (value is Relation row && IsRole(Template.RolePair, row.Left) && IsRole(field.RolePair, row.Right)) admitted.Add(row);
            else unmatched.Add(new Relation(value, new Relation(Template.RolePair, field.RolePair)));
        var selected = new List<Relation>();
        foreach (var role in new[] { Template.RolePair.Left, Template.RolePair.Right })
        {
            var rows = admitted.Where(row => Same(row.Left, role)).ToArray();
            if (rows.Length == 0) pending.Add(new Relation(role, roleMap));
            else if (rows.Length != 1) unmatched.Add(new Relation(role, S(rows)));
            else selected.Add(rows[0]);
        }
        if (selected.Count == 2 && Same(selected[0].Right, selected[1].Right)) unmatched.Add(new Relation(roleMap, field.RolePair));
        return selected.ToArray();
    }

    private IElement[] Selected(Basis basis, List<IElement> pending)
    {
        if (!ReadScope(Focus, out var cells)) pending.Add(new Relation(Focus, Surface));
        var distinct = Distinct(cells);
        if (cells.Length == 0 || distinct.Length != cells.Length || cells.Any(cell => !Has(basis.Cells, cell)))
            pending.Add(new Relation(Focus, Template.Cells));
        return distinct.Where(cell => Has(basis.Cells, cell)).ToArray();
    }
    private bool ClosedFocus(Basis basis) => ReadScope(Focus, out var cells) && cells.All(cell => Has(basis.Cells, cell));
    private static Basis BasisOf(QualitativePartition partition, List<IElement> pending)
    {
        var cellsClosed = ReadScope(partition.Cells, out var rawCells);
        var cells = Distinct(rawCells);
        if (!cellsClosed || cells.Length != rawCells.Length) pending.Add(new Relation(partition.Cells, partition.Surface));
        var edgesClosed = ReadScope(partition.Adjacency, out var rawEdges);
        if (!edgesClosed) pending.Add(new Relation(partition.Adjacency, partition.Surface));
        var edges = new List<Relation>();
        foreach (var value in rawEdges)
            if (value is Relation edge && Has(cells, edge.Left) && Has(cells, edge.Right)) edges.Add(edge);
            else pending.Add(new Relation(value, partition.Cells));
        var labelsClosed = ReadScope(partition.Labels, out var rawLabels);
        if (!labelsClosed) pending.Add(new Relation(partition.Labels, partition.Surface));
        var labels = new List<Relation>();
        foreach (var value in rawLabels)
            if (value is Relation label && Has(cells, label.Left)) labels.Add(label);
            else pending.Add(new Relation(value, partition.Labels));
        if (Same(partition.RolePair.Left, partition.RolePair.Right)) pending.Add(new Relation(partition.RolePair, partition.Surface));
        return new(partition, cells, edges.ToArray(), labels.ToArray(), cellsClosed, edgesClosed, labelsClosed);
    }
    private static Relation? Label(Basis basis, IElement cell, List<IElement> pending)
    {
        var labels = basis.Labels.Where(label => Same(label.Left, cell)).ToArray();
        if (labels.Length == 1 && IsRole(basis.Partition.RolePair, labels[0].Right)) return labels[0];
        pending.Add(new Relation(cell, new Relation(basis.Partition.Labels, S(labels)))); return null;
    }
    private static Relation Complement(Basis basis, IElement[] selected, bool selectionClosed)
    {
        // An unfinished selection may still select any exposed participant.
        // Its current prefix cannot establish positive complement membership.
        if (!selectionClosed) return new Relation(new Relation(StructuralConstraint.Open(), StructuralConstraint.Open()),
            new Relation(StructuralConstraint.Open(), StructuralConstraint.Open()));
        var outside = basis.Cells.Where(cell => !Has(selected, cell)).ToArray();
        return new Relation(new Relation(Scope(basis.CellsClosed && selectionClosed, outside), Scope(basis.LabelsClosed && selectionClosed,
                basis.Labels.Where(label => Has(outside, label.Left)).Cast<IElement>().ToArray())),
            new Relation(Scope(basis.EdgesClosed && selectionClosed, basis.Edges.Where(edge => Has(outside, edge.Left) && Has(outside, edge.Right)).Cast<IElement>().ToArray()),
                Scope(basis.EdgesClosed && selectionClosed, basis.Edges.Where(edge => Has(selected, edge.Left) != Has(selected, edge.Right)).Cast<IElement>().ToArray())));
    }
    private static bool IsComplement(Relation complement) => complement.Left is Relation { Left: Relation cells, Right: Relation labels } &&
        complement.Right is Relation { Left: Relation edges, Right: Relation cut } &&
        new[] { cells, labels, edges, cut }.All(scope => StructuralConstraint.TryReadScope(scope, out _, out _, out _));
    private static bool SameComplement(Relation a, Relation b)
    {
        var left = new[] { ((Relation)a.Left).Left, ((Relation)a.Left).Right, ((Relation)a.Right).Left, ((Relation)a.Right).Right };
        var right = new[] { ((Relation)b.Left).Left, ((Relation)b.Left).Right, ((Relation)b.Right).Left, ((Relation)b.Right).Right };
        return left.Zip(right).All(pair => ReadScope((Relation)pair.First, out var x) && ReadScope((Relation)pair.Second, out var y) &&
            x.Length == y.Length && x.Zip(y).All(values => ReferenceEquals(values.First, values.Second)));
    }
    private bool SameMatches(QualitativePartition field, Relation retained, Relation fresh)
    {
        if (!ReadScope(retained, out var a) || !ReadScope(fresh, out var b) || a.Length != b.Length) return false;
        var edges = Entries(Template.Adjacency).Concat(Entries(field.Adjacency)).ToHashSet(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] is not Relation actual || b[i] is not Relation expected) return false;
            if (edges.Contains(expected.Left))
            {
                // Only this declared child is a scope. A mapped primitive edge
                // between placeholders must keep its actual endpoints; it is
                // not normalized as an empty open finite scope.
                if (!ReferenceEquals(actual.Left, expected.Left) || actual.Right is not Relation actualBody ||
                    expected.Right is not Relation expectedBody || actualBody.Left is not Relation actualEdge ||
                    expectedBody.Left is not Relation expectedEdge || !ReferenceEquals(actualEdge.Left, expectedEdge.Left) ||
                    !ReferenceEquals(actualEdge.Right, expectedEdge.Right) || actualBody.Right is not Relation actualOffers ||
                    expectedBody.Right is not Relation expectedOffers || !SameActualRows(actualOffers, expectedOffers)) return false;
            }
            else
            {
                if (actual.Left is not Relation actualMaps || expected.Left is not Relation expectedMaps ||
                    actual.Right is not Relation actualLabels || expected.Right is not Relation expectedLabels ||
                    !ReferenceEquals(actualMaps.Left, expectedMaps.Left) || !ReferenceEquals(actualMaps.Right, expectedMaps.Right) ||
                    !ReferenceEquals(actualLabels.Left, expectedLabels.Left) || !ReferenceEquals(actualLabels.Right, expectedLabels.Right)) return false;
            }
        }
        return true;
    }
    private static bool SameActualRows(Relation a, Relation b) => ReadScope(a, out var x) && ReadScope(b, out var y) &&
        x.Length == y.Length && x.Zip(y).All(pair => ReferenceEquals(pair.First, pair.Second));
    private sealed record Basis(QualitativePartition Partition, IElement[] Cells, Relation[] Edges, Relation[] Labels,
        bool CellsClosed, bool EdgesClosed, bool LabelsClosed);
    private static bool IsRole(Relation pair, IElement role) => !Same(pair.Left, pair.Right) && (Same(pair.Left, role) || Same(pair.Right, role));
    private static bool Has(IEnumerable<IElement> source, IElement element) => source.Any(item => Same(item, element));
    private static IElement[] Distinct(IEnumerable<IElement> source)
    {
        var values = new List<IElement>(); foreach (var item in source) if (!Has(values, item)) values.Add(item); return values.ToArray();
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation p && b is CarrierLocation q && p.Carrier.SameIdentityAs(q.Carrier) && p.SameTickRefAs(q);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static Relation Scope(bool closed, params IElement[] values) => closed ? S(values) : StructuralConstraint.Open(values);
    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var values, out _, out _) ? values : [];
    private static bool ReadScope(Relation scope, out IElement[] values) =>
        StructuralConstraint.TryReadScope(scope, out values, out var seam, out var tail) && seam.Equals(tail);
}
