using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Role-relative occupancy and clearance on an explicitly selected
/// observation topology. Other roles remain occupied in the source partition;
/// an empty support here means absence of this selected role only.</summary>
public sealed class QualitativeFieldAnalysis
{
    // Surface = ((source.Surface -> selected-role) ->
    //            (declared-supports -> observation-adjacency)) -> goal
    // read = Surface -> ((source-read -> qualified-coverage-read) -> unresolved)
    // qualified coverage uses the original cells, explicit observer edges and
    // actual support identities. No reverse edge, metric or fresh sample is
    // invented. Goals are explicit participants, not built-in scoring commands.
    public QualitativePartition Source { get; }
    public IElement SelectedRole { get; }
    public Relation Supports { get; }
    public Relation Adjacency { get; }
    public IElement Goal { get; }
    public Relation Surface { get; }
    public QualitativeCoverage Coverage { get; }
    public Relation SourceRead { get; }
    public Relation CoverageRead { get; }
    private readonly Relation read;
    private readonly Relation pending;

    public QualitativeFieldAnalysis(QualitativePartition source, IElement selectedRole,
        Relation supports, Relation? observationAdjacency = null, IElement? goal = null)
    {
        Source = source; SelectedRole = selectedRole; Supports = supports;
        Adjacency = observationAdjacency ?? source.Adjacency; Goal = goal ?? new Placeholder();
        Surface = new Relation(new Relation(new Relation(source.Surface, selectedRole), new Relation(supports, Adjacency)), Goal);
        SourceRead = source.Evaluate(); var unresolved = new List<IElement>(); var qualified = new List<IElement>();
        FieldNative.Read(source.Cells, out var cells); var closed = FieldNative.Read(supports, out var offers);
        if (!FieldNative.IsRole(source.RolePair, selectedRole)) unresolved.Add(new Relation(selectedRole, source.RolePair));
        if (!closed) unresolved.Add(new Relation(supports, Surface));
        foreach (var offer in offers)
            if (offer is not Relation row || !cells.Any(cell => FieldNative.Same(cell, row.Left)))
                unresolved.Add(new Relation(offer, source.Cells));
        foreach (var cell in cells)
        {
            var label = FieldNative.Label(source, cell);
            var rows = offers.OfType<Relation>().Where(row => FieldNative.Same(row.Left, cell)).ToArray();
            if (!FieldNative.IsRole(source.RolePair, selectedRole) || label is null)
            { qualified.Add(new Relation(cell, StructuralConstraint.Open())); unresolved.Add(new Relation(cell, source.Labels)); continue; }
            if (!closed || rows.Length != 1 || rows[0].Right is not Relation support || !FieldNative.Read(support, out var witnesses))
            { qualified.Add(new Relation(cell, StructuralConstraint.Open())); unresolved.Add(new Relation(cell, supports)); continue; }
            if (FieldNative.Same(label.Right, selectedRole))
            {
                // An explicitly occupied role needs at least one actual causal
                // support. A missing witness cannot reclassify it as empty.
                if (witnesses.Length == 0)
                { qualified.Add(new Relation(cell, StructuralConstraint.Open())); unresolved.Add(new Relation(rows[0], label)); }
                else qualified.Add(rows[0]);
            }
            else if (witnesses.Length == 0) qualified.Add(rows[0]);
            else
            { qualified.Add(new Relation(cell, StructuralConstraint.Open())); unresolved.Add(new Relation(rows[0], label)); }
        }
        if (source.TryReadResult(SourceRead, out _, out _, out _, out _, out var sourcePending))
            unresolved.AddRange(FieldNative.E(sourcePending));
        else unresolved.Add(SourceRead);
        Coverage = new QualitativeCoverage(source.Cells, Adjacency,
            FieldNative.Scope(FieldNative.Read(source.Cells, out _), qualified.ToArray()));
        CoverageRead = Coverage.Evaluate();
        if (Coverage.TryReadResult(CoverageRead, out _, out _, out _, out _, out _, out var coveragePending))
            unresolved.AddRange(FieldNative.E(coveragePending));
        else unresolved.Add(CoverageRead);
        pending = FieldNative.S(unresolved.ToArray());
        read = new Relation(Surface, new Relation(new Relation(SourceRead, CoverageRead), pending));
    }

    public static QualitativeFieldAnalysis FromLabels(QualitativePartition source, IElement selectedRole,
        Relation? observationAdjacency = null, IElement? goal = null)
    {
        FieldNative.Read(source.Cells, out var cells);
        var rows = cells.Select(cell =>
        {
            var label = FieldNative.Label(source, cell);
            return (IElement)new Relation(cell, label is null || !FieldNative.IsRole(source.RolePair, selectedRole)
                ? StructuralConstraint.Open() : FieldNative.S(FieldNative.Same(label.Right, selectedRole) ? [label] : []));
        }).ToArray();
        return new QualitativeFieldAnalysis(source, selectedRole,
            FieldNative.Scope(FieldNative.Read(source.Cells, out _), rows), observationAdjacency, goal);
    }

    public Relation Evaluate() => read;

    /// <summary>The closed evidence scopes report justified local reads only;
    /// unresolved obligations remain part of this same source/read/goal frame.</summary>
    public bool TryReadResult(Relation candidate, out Relation empty, out Relation single, out Relation multiple,
        out Relation layers, out Relation centers, out Relation unresolved)
    {
        empty = single = multiple = layers = centers = unresolved = null!;
        if (!ReferenceEquals(candidate.Left, Surface) || candidate.Right is not Relation { Left: Relation reads } body ||
            !ReferenceEquals(reads.Left, SourceRead) || !ReferenceEquals(reads.Right, CoverageRead) ||
            !ReferenceEquals(body.Right, pending) ||
            !Coverage.TryReadResult(CoverageRead, out var none, out var one, out var many, out var shells, out var plateaus, out _)) return false;
        empty = none; single = one; multiple = many; layers = shells; centers = plateaus; unresolved = pending; return true;
    }
}
