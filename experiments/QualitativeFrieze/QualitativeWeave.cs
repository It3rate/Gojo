using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// An authored two-role field on a finite cylinder. The second half of each
/// row is supplied by native finite-law reads, rather than a hidden color flip.
/// Ordered columns wrap; ordered rows have exposed endpoints. Shared pitch
/// participants specify repeated steps without assigning distance or area.
/// </summary>
public sealed class QualitativeWeave
{
    // Surface = ((SeedRows -> ExchangeLaw) ->
    //            ((Columns -> Rows) -> ((ColumnOrder -> RowOrder) ->
    //             ((ColumnPitch -> RowPitch) -> PitchBindings)))) ->
    //           ((Coordinates -> Partition.Surface) ->
    //            (HalfShift -> (GenerationEvidence -> GenerationUnresolved)))
    // coordinate = actual-cell -> (actual-column -> actual-row)
    // pitch binding = actual ordered column/row edge -> its shared pitch
    // generation = ((actual-seed-row -> actual-seed-role) ->
    //               (actual-source-cell -> actual-target-cell)) -> finite-law-read
    // Columns, rows, coordinates, maps and witnesses use native finite scopes.
    // The shared pitch is a construction premise, not calibrated equal area.
    public Relation Surface { get; }
    public Relation SeedRows { get; }
    public Relation RolePair { get; }
    public Relation Columns { get; }
    public Relation Rows { get; }
    public Relation ColumnOrder { get; }
    public Relation RowOrder { get; }
    public IElement ColumnPitch { get; }
    public IElement RowPitch { get; }
    public Relation PitchBindings { get; }
    public Relation Cells { get; }
    public Relation Coordinates { get; }
    public Relation Adjacency { get; }
    public Relation HalfShift { get; }
    public QualitativePartition Partition { get; }
    public Relation ExchangeLaw { get; }
    public Relation GenerationEvidence { get; }
    public Relation GenerationUnresolved { get; }

    public QualitativeWeave(Relation seedRows, IElement firstRole, IElement secondRole)
        : this(seedRows, new Relation(firstRole, secondRole)) { }

    /// <summary>
    /// A complete rectangular seed publishes a finite cylinder. Open, malformed
    /// or ragged row shapes retain the supplied seed and an open field instead
    /// of guessing dimensions. Unknown roles and incomplete laws retain local
    /// generation reads but cannot certify a complete exchange.
    /// </summary>
    public QualitativeWeave(Relation seedRows, Relation rolePair, Relation? exchangeLaw = null)
    {
        SeedRows = seedRows; RolePair = rolePair;
        ExchangeLaw = exchangeLaw ?? CreateExchangeLaw(rolePair);
        ColumnPitch = new Placeholder(); RowPitch = new Placeholder();
        var seedClosed = ReadScope(seedRows, out var rawRows);
        if (rawRows.Length > 16) throw new ArgumentOutOfRangeException(nameof(seedRows), "At most sixteen seed rows are supported.");
        var rowValues = new IElement[rawRows.Length][];
        var rectangular = seedClosed && rawRows.Length != 0;
        for (var i = 0; i < rawRows.Length; i++)
        {
            var closed = rawRows[i] is Relation row && ReadScope(row, out rowValues[i]);
            rowValues[i] ??= [];
            if (rowValues[i].Length > 16) throw new ArgumentOutOfRangeException(nameof(seedRows), "At most sixteen half-columns are supported.");
            rectangular &= closed && rowValues[i].Length != 0 && rowValues[i].Length == rowValues[0].Length;
        }
        var rowCount = rectangular ? rawRows.Length : 0;
        var halfColumns = rectangular ? rowValues[0].Length : 0;
        var columnIds = Enumerable.Range(0, halfColumns * 2).Select(_ => (IElement)new Placeholder()).ToArray();
        var rowIds = Enumerable.Range(0, rowCount).Select(_ => (IElement)new Placeholder()).ToArray();
        Columns = Scope(rectangular, columnIds); Rows = Scope(rectangular, rowIds);
        var columnEdges = new List<IElement>(); var rowEdges = new List<IElement>(); var pitch = new List<IElement>();
        for (var x = 0; x < columnIds.Length; x++)
        {
            var edge = new Relation(columnIds[x], columnIds[(x + 1) % columnIds.Length]);
            columnEdges.Add(edge); pitch.Add(new Relation(edge, ColumnPitch));
        }
        for (var y = 0; y + 1 < rowIds.Length; y++)
        {
            var edge = new Relation(rowIds[y], rowIds[y + 1]);
            rowEdges.Add(edge); pitch.Add(new Relation(edge, RowPitch));
        }
        ColumnOrder = Scope(rectangular, columnEdges.ToArray()); RowOrder = Scope(rectangular, rowEdges.ToArray());
        PitchBindings = Scope(rectangular, pitch.ToArray());
        var actual = new IElement[rowCount, columnIds.Length];
        var cells = new List<IElement>(); var coordinates = new List<IElement>(); var adjacency = new List<IElement>();
        var labels = new List<IElement>(); var shift = new List<IElement>(); var generated = new List<IElement>(); var pending = new List<IElement>();
        if (!rectangular) pending.Add(new Relation(seedRows, new Relation(rolePair, ExchangeLaw)));
        for (var y = 0; y < rowCount; y++)
            for (var x = 0; x < columnIds.Length; x++)
            {
                var cell = new Placeholder(); actual[y, x] = cell; cells.Add(cell);
                coordinates.Add(new Relation(cell, new Relation(columnIds[x], rowIds[y])));
            }
        for (var y = 0; y < rowCount; y++)
            for (var x = 0; x < columnIds.Length; x++)
            {
                adjacency.Add(new Relation(actual[y, x], actual[y, (x + 1) % columnIds.Length]));
                if (y + 1 < rowCount) adjacency.Add(new Relation(actual[y, x], actual[y + 1, x]));
                shift.Add(new Relation(actual[y, x], actual[y, (x + halfColumns) % columnIds.Length]));
            }
        for (var y = 0; y < rowCount; y++)
            for (var x = 0; x < halfColumns; x++)
            {
                var source = actual[y, x]; var target = actual[y, x + halfColumns]; var role = rowValues[y][x];
                if (IsRole(role)) labels.Add(new Relation(source, role));
                else pending.Add(new Relation(source, new Relation(role, rolePair)));
                var read = FiniteLaw.Evaluate(ExchangeLaw, S(role), S(rolePair));
                generated.Add(new Relation(new Relation(new Relation(rawRows[y], role), new Relation(source, target)), read));
                if (FiniteLaw.TryReadResult(read, out var support, out var unresolved, out _) &&
                    ReadScope(support, out var values) && values.Length == 1 && IsRole(values[0]) &&
                    ReadScope(unresolved, out var remainder) && remainder.Length == 0)
                    labels.Add(new Relation(target, values[0]));
                else pending.Add(new Relation(target, read));
            }
        Cells = Scope(rectangular, cells.ToArray()); Coordinates = Scope(rectangular, coordinates.ToArray());
        Adjacency = Scope(rectangular, adjacency.ToArray()); HalfShift = Scope(rectangular, shift.ToArray());
        GenerationEvidence = S(generated.ToArray()); GenerationUnresolved = S(pending.ToArray());
        Partition = new QualitativePartition(Cells, Adjacency, rolePair, S(labels.ToArray()));
        Surface = new Relation(new Relation(new Relation(seedRows, ExchangeLaw),
                new Relation(new Relation(Columns, Rows), new Relation(new Relation(ColumnOrder, RowOrder),
                    new Relation(new Relation(ColumnPitch, RowPitch), PitchBindings)))),
            new Relation(new Relation(Coordinates, Partition.Surface), new Relation(HalfShift,
                new Relation(GenerationEvidence, GenerationUnresolved))));
    }

    /// <summary>Exchange is justified by the two visible rows and their actual
    /// role-pair operand. The pair is connected input, not an operation tag.</summary>
    public static Relation CreateExchangeLaw(Relation rolePair) => FiniteLaw.Create(
        S(rolePair.Left, rolePair.Right), S(rolePair), S(rolePair.Left, rolePair.Right),
        FiniteLaw.Row(rolePair.Left, rolePair, S(rolePair.Right)),
        FiniteLaw.Row(rolePair.Right, rolePair, S(rolePair.Left)));

    public Relation EvaluateHalfShift() => Partition.EvaluateExchange(HalfShift);

    // These strings author explicit native role occurrences. They are discarded
    // after construction; reads and rendering consume only the resulting graph.
    public static QualitativeWeave SteppedChevron() => Authored(
        "11111111", "01111111", "00111111", "00011111", "00011111", "00111111", "01111111", "11111111");

    public static QualitativeWeave NestedFret() => Authored(
        "11111111", "10000001", "10111101", "10100101", "10101101", "10100001", "10111111", "10000000");

    public static QualitativeWeave FoldedBlocks() => Authored(
        "1111111111", "1111111111", "1100000000", "1100000000", "1100111111", "1100111111",
        "1100110000", "1100110000", "0000110000", "0000110000");

    private static QualitativeWeave Authored(params string[] rows)
    {
        var first = new Placeholder(); var second = new Placeholder();
        return new QualitativeWeave(S(rows.Select(row => (IElement)S(row.Select(bit => bit == '1' ? (IElement)first : second).ToArray())).ToArray()), first, second);
    }
    private bool IsRole(IElement value) => !Same(RolePair.Left, RolePair.Right) && (Same(value, RolePair.Left) || Same(value, RolePair.Right));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static Relation Scope(bool closed, params IElement[] values) => closed ? S(values) : StructuralConstraint.Open(values);
    private static bool ReadScope(Relation scope, out IElement[] values) =>
        StructuralConstraint.TryReadScope(scope, out values, out var seam, out var tail) && seam.Equals(tail);
}
