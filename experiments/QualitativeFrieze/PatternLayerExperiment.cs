using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A bounded laboratory of alternative observations and deliberate
/// field passes. Display coordinates are read from the native ordered cells;
/// they never select a law or supply a source value.</summary>
public static class PatternLayerExperiment
{
    private static readonly IEqualityComparer<IElement> Identity = ReferenceEqualityComparer.Instance;
    // experiment = (drive-analyses -> common-frame-observation) ->
    //              (field-source -> (field-passes -> motif-correspondences))
    // observation focus = (original-directed-field -> selected-bidirectional-field)
    // Mask authoring selects actual cells; all nonselected labels stay explicit.
    private sealed record FieldPicture(string Name, string Detail, QualitativePartition Partition,
        Relation Source, IReadOnlySet<IElement>? Marked = null, string Mark = "#d36c3b");
    private sealed record DrivePicture(string Name, string Detail,
        IReadOnlyList<IReadOnlyList<(long X, long Y)>> Paths, Relation Source);

    public static int Run(string[] arguments)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "layers");
        for (var i = 0; i < arguments.Length; i++)
            if (arguments[i] == "--output" && i + 1 < arguments.Length) output = arguments[++i];
            else throw new ArgumentException("Usage: --layers [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var drives = BuildDrives(out var driveSource, out var driveDiagnostics);
        var fields = BuildFields(out var weave, out var fieldSource, out var fieldDiagnostics);
        var source = new Relation(driveSource, fieldSource);
        if (Traverse(source).Any(value => value is Carrier or CarrierLocation))
            throw new InvalidOperationException("A measured participant entered the native source analysis.");
        var observation = new Relation(S(drives.Select(picture => (IElement)picture.Source).ToArray()), FieldView(weave));
        var svg = Draw(drives, fields, weave);
        File.WriteAllText(Path.Combine(output, "gallery.svg"), svg);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(svg));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "qualitative-pattern-layers-v1", NativeSource = Counts(source), Observation = Counts(observation),
            Drives = driveDiagnostics, Fields = fieldDiagnostics,
            Notes = new[] { "Host counts are projections; source decisions use actual cells, roles, laws and witnesses.",
                "A field correspondence does not reconstruct unique driving history.",
                "Multi-pass union and subtraction are observational field transformations, not material settlement." }
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(new { Drives = driveDiagnostics, Fields = fieldDiagnostics }));
        Console.WriteLine($"Wrote layered analysis to {Path.GetFullPath(output)}.");
        return 0;
    }

    private static IReadOnlyList<DrivePicture> BuildDrives(out Relation source, out object diagnostics)
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var zigzag = FriezeFixture.CombinedDriveZigzag(frame: frame, horizontalReach: h, verticalReach: v);
        var staircase = zigzag.WithSampling(S(E(zigzag.Network.Ports)
            .Where(port => !ReferenceEquals(port, zigzag.RootPort)).ToArray()), "Same drives / sample each component");
        var slow = FriezeFixture.CombinedDriveZigzag(true, frame, h, v);
        var calibration = FriezeRealization.CreateCalibration(zigzag, FriezeRealization.DefaultLengths(zigzag, 4, 4));
        var pictures = new List<DrivePicture>(); var native = new List<IElement>(); var runs = new List<object>();
        foreach (var (name, detail, fixture) in new[]
        {
            ("Grouped contributions / zigzag", "One root sample combines right + alternating up/down", zigzag),
            ("Same laws / staircase", "Each component firing commits its own sample", staircase),
            ("Different recurrence / slow vertical", "Only one of two root phases also triggers the vertical drive", slow)
        })
        {
            var trace = fixture.Run(12); var analysis = FriezeDriveAnalysis.Evaluate(fixture, trace);
            if (!FriezeDriveAnalysis.TryReadResult(fixture, analysis, out var clauses, out var unresolved) || E(unresolved).Length != 0)
                throw new InvalidOperationException("The authored drive did not expose complete causal analysis.");
            if (!FriezeRealization.TryRealize(fixture, trace, calibration, out var measured) ||
                !FriezeRealization.TryRead(measured, out _, out _, out var samples, out var pending) || E(pending).Length != 0)
                throw new InvalidOperationException("The selected view did not realize its samples.");
            var points = new List<(long X, long Y)>();
            foreach (var clause in E(samples).Cast<Relation>())
            {
                if (!FriezeRealization.TryReadSamplePoints(clause, out _, out var before, out var after) ||
                    !FriezeRealization.TryReadPoint(before, out var bx, out var by) ||
                    !FriezeRealization.TryReadPoint(after, out var ax, out var ay))
                    throw new InvalidOperationException("A realized sample lost its point correspondence.");
                if (points.Count == 0) points.Add((bx, by)); points.Add((ax, ay));
            }
            native.Add(analysis);
            pictures.Add(new(name, detail, [points], new Relation(analysis, measured)));
            fixture.TryReadTrace(trace, out _, out _, out var steps, out var applications, out _, out _);
            runs.Add(new { Name = name, RootDrives = E(steps).Length, Applications = E(applications).Length,
                Samples = E(clauses).Length, End = new { X = points[^1].X, Y = points[^1].Y }, Native = Counts(analysis) });
        }
        // Non-draining observation of independent traces in their advertised
        // shared frame. It neither identifies their occurrences nor transfers
        // conserved material between cursors.
        var observation = new Relation(new Relation(pictures[0].Source, pictures[2].Source), frame.Surface);
        pictures.Add(new("Two drawing cursors / common frame", "Fast teal and slow magenta retain separate event histories",
            [pictures[0].Paths[0], pictures[2].Paths[0]], observation));
        source = new Relation(S(native.ToArray()), frame.Surface); diagnostics = runs;
        return pictures;
    }

    private static IReadOnlyList<FieldPicture> BuildFields(out QualitativeWeave weave,
        out Relation source, out object diagnostics)
    {
        weave = QualitativeWeave.NestedFret();
        // Clearance observes a separately exposed bidirectional neighborhood.
        // Reverse edges are supplied, never inferred inside the original view.
        var edges = E(weave.Adjacency).Cast<Relation>().ToArray();
        var neighborhood = S(edges.Cast<IElement>().Concat(edges.Select(edge =>
            (IElement)new Relation(edge.Right, edge.Left))).ToArray());
        var basis = new QualitativePartition(weave.Cells, neighborhood, weave.RolePair, weave.Partition.Labels);
        var focusBasis = new Relation(weave.Surface, basis.Surface);
        var columns = E(weave.Columns); var rows = E(weave.Rows);
        var coordinates = E(weave.Coordinates).Cast<Relation>().ToArray();
        var analysis = QualitativeFieldAnalysis.FromLabels(basis, basis.RolePair.Left);
        var coverage = analysis.Coverage; var coverageRead = analysis.Evaluate();
        ReadCoverage(analysis, out _, out var layers, out var centers);
        var last = E(layers).Last(); var quietCells = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var center in E(centers).Cast<Relation>())
            if (coverage.TryReadCenter(center, out var layer, out var members) && ReferenceEquals(layer, last))
                quietCells.UnionWith(E(members));
        if (quietCells.Count == 0) throw new InvalidOperationException("The selected field has no deepest quiet plateau.");
        var union = QualitativeFieldPass.UnionLaw(basis.RolePair, basis.RolePair.Left);
        var difference = QualitativeFieldPass.DifferenceLaw(basis.RolePair, basis.RolePair.Left);
        var plateauScope = S(quietCells.ToArray());
        var allQuiet = Mask(basis, quietCells);
        var naive = Compose(basis, allQuiet, union, basis.Cells, new Relation(coverageRead, plateauScope));
        // All white cells tie in this coarse field. An authored landmark makes
        // the underdetermined choice explicit rather than selecting a hidden
        // first candidate or treating clearance as a complete aesthetic law.
        var landmark = coordinates.Single(coordinate => coordinate.Right is Relation pair &&
            ReferenceEquals(pair.Left, columns[3]) && ReferenceEquals(pair.Right, rows[3]));
        if (!quietCells.Contains(landmark.Left)) throw new InvalidOperationException("The selected landmark is outside the quiet focus.");
        var selectedQuiet = new HashSet<IElement>(Identity) { landmark.Left };
        var quietScope = S(landmark.Left); var quietMask = Mask(basis, selectedQuiet);
        var quietProposal = new Relation(new Relation(coverageRead, new Relation(landmark, quietScope)), quietMask.Surface);
        var quietPass = Compose(basis, quietMask, union, basis.Cells, quietProposal);
        var afterQuiet = QualitativeFieldAnalysis.FromLabels(quietPass.Result, basis.RolePair.Left);
        var afterQuietRead = afterQuiet.Evaluate();
        ReadCoverage(afterQuiet, out _, out var quietLayers, out _);

        // Reuse the same field motif with one step of the actual column cycle.
        // The correspondence and each old/new label remain source evidence.
        var originalLabels = E(basis.Labels).Cast<Relation>().ToDictionary(row => row.Left, Identity);
        var donorLabels = new List<IElement>(); var mapRows = new List<IElement>(); var donorEvidence = new List<IElement>();
        foreach (var coordinate in coordinates)
        {
            var pair = (Relation)coordinate.Right;
            var index = Array.FindIndex(columns, column => ReferenceEquals(column, pair.Left));
            var target = coordinates.Single(candidate => candidate.Right is Relation other &&
                ReferenceEquals(other.Left, columns[(index + 1) % columns.Length]) && ReferenceEquals(other.Right, pair.Right)).Left;
            var map = new Relation(coordinate.Left, target); var old = originalLabels[coordinate.Left];
            var label = new Relation(target, old.Right);
            mapRows.Add(map); donorLabels.Add(label); donorEvidence.Add(new Relation(new Relation(old, map), label));
        }
        var donor = new QualitativePartition(basis.Cells, basis.Adjacency, basis.RolePair, S(donorLabels.ToArray()));
        var donorMap = S(mapRows.ToArray());
        var donorSource = new Relation(new Relation(focusBasis, donorMap), new Relation(donor.Surface, S(donorEvidence.ToArray())));
        var joined = Compose(basis, donor, union, basis.Cells, donorSource);
        var layered = new QualitativeFieldAnalysis(joined.Result, basis.RolePair.Left, joined.Supports);
        var layeredRead = layered.Evaluate();
        ReadCoverage(layered, out var crowdedRows, out _, out _);
        var crowdedCells = E(crowdedRows).Cast<Relation>().Select(row => row.Left).ToHashSet(Identity);
        var crowdedScope = S(crowdedCells.ToArray()); var crowdMask = Mask(basis, crowdedCells);
        var relaxation = Compose(joined.Result, crowdMask, difference, crowdedScope, layeredRead, joined.Read);
        var relaxSource = new Relation(new Relation(joined.Read, layeredRead), relaxation.Read);
        var recognition = Recognize(basis, weave, donor, donorMap, out var correspondenceDiagnostics);
        var exchangeReads = new List<IElement>(); var exchangeDiagnostics = new List<object>();
        foreach (var (name, field) in new[] { ("Original", basis), ("Naive quiet fill", naive.Result),
            ("Landmark addition", quietPass.Result), ("Shifted layer", donor), ("Union", joined.Result), ("Relaxed", relaxation.Result) })
        {
            var read = field.EvaluateExchange(weave.HalfShift); exchangeReads.Add(read);
            if (!field.TryReadExchange(read, out _, out _, out var unmatched, out var pending) || E(pending).Length != 0)
                throw new InvalidOperationException("A selected half-shift test lost its exact mismatch evidence.");
            exchangeDiagnostics.Add(new { Name = name, Passed = field.TryReadExchangePassed(read), Unmatched = E(unmatched).Length });
        }
        var constructions = S(focusBasis, coverageRead, naive.Read, quietProposal, quietPass.Read, afterQuietRead,
            donorSource, joined.Read, layeredRead, relaxSource, recognition, S(exchangeReads.ToArray()));
        source = new Relation(weave.Surface, constructions);
        diagnostics = new
        {
            Native = Counts(source), Cells = E(basis.Cells).Length, QuietPlateauCells = quietCells.Count,
            SelectedQuietCells = selectedQuiet.Count, NaiveFillAllQuiet = FieldSummary(naive.Result),
            OrdinalShellsBefore = E(layers).Length, OrdinalShellsAfterQuietAddition = E(quietLayers).Length,
            CrowdedCells = crowdedCells.Count, Correspondences = correspondenceDiagnostics, SelectedHalfShift = exchangeDiagnostics,
            Original = FieldSummary(basis), QuietAddition = FieldSummary(quietPass.Result),
            ShiftedLayer = FieldSummary(donor), Union = FieldSummary(joined.Result), Relaxed = FieldSummary(relaxation.Result)
        };
        return new FieldPicture[]
        {
            new("Original nested field", "A finite cylinder; both roles have explicit labels", basis, focusBasis),
            new("Read quiet regions", "Deepest local plateaus in the selected graph clearance", basis, coverageRead, quietCells),
            new("Counterexample / fill every quiet cell", "All 64 white cells tie here; clearance alone loses the pattern", naive.Result, naive.Read),
            new("Add at an explicit quiet landmark", "One actual row/column site selects among the tied candidates", quietPass.Result, quietPass.Read, selectedQuiet),
            new("A second contributing layer", "Reuse the motif with a one-column phase shift", donor, donorSource),
            new("Union of the two layers", "The visible result forgets which contributor supplied each cell", joined.Result, joined.Read),
            new("Read multiple support", "Magenta cells have two distinct layer witnesses", joined.Result, layeredRead, crowdedCells, "#c3458d"),
            new("Subtract at the crowded focus", "32 selected cells removed; carried cells preserve their old labels", relaxation.Result, relaxSource),
            new("Recognize motif in the opposite role", "Half-shift maps the whole dark/light field to its exchange", basis, recognition)
        };
    }

    private static QualitativePartition Mask(QualitativePartition basis, IReadOnlySet<IElement> selected) =>
        new(basis.Cells, basis.Adjacency, basis.RolePair, S(E(basis.Cells).Select(cell =>
            (IElement)new Relation(cell, selected.Contains(cell) ? basis.RolePair.Left : basis.RolePair.Right)).ToArray()));

    private static void ReadCoverage(QualitativeFieldAnalysis analysis, out Relation multiple,
        out Relation layers, out Relation centers)
    {
        if (!analysis.TryReadResult(analysis.Evaluate(), out _, out _, out multiple, out layers, out centers, out var pending) || E(pending).Length != 0)
            throw new InvalidOperationException("The declared field coverage did not settle.");
    }

    private static (QualitativePartition Result, Relation Read, Relation Timeline, Relation Supports) Compose(QualitativePartition first,
        QualitativePartition second, Relation law, Relation focus, IElement goal, Relation? previous = null)
    {
        var pass = new QualitativeFieldPass(first, second, law, focus, goal, previous);
        var read = pass.Evaluate();
        if (!pass.TryReadResult(read, out var result, out _, out _, out var pending, out var timeline) ||
            !ReferenceEquals(result, pass.Result.Surface) || E(pending).Length != 0)
            throw new InvalidOperationException("The explicit field pass did not settle.");
        return (pass.Result, read, timeline, pass.Supports(first.RolePair.Left));
    }

    private static Relation Recognize(QualitativePartition basis, QualitativeWeave weave,
        QualitativePartition donor, Relation donorMap, out object diagnostics)
    {
        var columns = E(weave.Columns); var coordinates = E(weave.Coordinates).Cast<Relation>().ToArray();
        var halfColumns = columns.Take(columns.Length / 2).ToHashSet(Identity);
        var focus = S(coordinates.Where(row => halfColumns.Contains(((Relation)row.Right).Left)).Select(row => row.Left).ToArray());
        var selected = E(focus).ToHashSet(Identity);
        Relation Restrict(Relation map) => S(E(map).Cast<Relation>().Where(row => selected.Contains(row.Left)).Cast<IElement>().ToArray());
        var identity = S(E(focus).Select(cell => (IElement)new Relation(cell, cell)).ToArray());
        var halfShift = Restrict(weave.HalfShift); var shift = Restrict(donorMap);
        var reader = new MotifFieldCorrespondence(basis, focus);
        var same = MotifFieldCorrespondence.RoleCorrespondence(basis.RolePair, basis.RolePair);
        var opposite = MotifFieldCorrespondence.RoleCorrespondence(basis.RolePair, new Relation(basis.RolePair.Right, basis.RolePair.Left));
        var sameRead = reader.Evaluate(basis, same, S(identity, halfShift));
        var oppositeRead = reader.Evaluate(basis, opposite, S(identity, halfShift));
        var donorRead = reader.Evaluate(donor, same, S(shift));
        Require(sameRead, basis, identity, true); Require(sameRead, basis, halfShift, false);
        Require(oppositeRead, basis, halfShift, true); Require(oppositeRead, basis, identity, false);
        Require(donorRead, donor, shift, true);
        // Each candidate reflects the actual column order, then shifts within
        // its explicit cycle. No mirror is accepted from a visual resemblance.
        var mirrors = new List<IElement>(); var mirrorConstruction = new List<IElement>();
        for (var offset = 0; offset < columns.Length; offset++)
        {
            var columnMap = S(columns.Select((column, index) => (IElement)new Relation(column,
                columns[(offset + columns.Length - index) % columns.Length])).ToArray());
            var pairs = E(columnMap).Cast<Relation>().ToArray();
            var map = S(coordinates.Where(row => selected.Contains(row.Left)).Select(row =>
            {
                var pair = (Relation)row.Right; var targetColumn = pairs.Single(mapping => ReferenceEquals(mapping.Left, pair.Left)).Right;
                var target = coordinates.Single(candidate => candidate.Right is Relation other &&
                    ReferenceEquals(other.Left, targetColumn) && ReferenceEquals(other.Right, pair.Right)).Left;
                return (IElement)new Relation(row.Left, target);
            }).ToArray());
            mirrors.Add(map); mirrorConstruction.Add(new Relation(new Relation(weave.ColumnOrder, columnMap), map));
        }
        var mirrorRead = reader.Evaluate(basis, opposite, S(mirrors.ToArray()));
        object Describe(Relation read, QualitativePartition field)
        {
            if (!reader.TryReadResult(read, out _, out var candidates, out var pending) || E(pending).Length != 0)
                throw new InvalidOperationException("The bounded motif read lost its candidate scope.");
            return E(candidates).Cast<Relation>().Select(candidate =>
            {
                reader.TryReadCandidate(candidate, out _, out _, out var map, out _, out var matched, out var unmatched, out var unresolved);
                return new { Passed = reader.TryReadPassed(field, candidate), MapRows = E(map).Length,
                    Matched = E(matched).Length, Unmatched = E(unmatched).Length, Unresolved = E(unresolved).Length };
            }).ToArray();
        }
        diagnostics = new { TemplateCells = selected.Count, SameRole = Describe(sameRead, basis),
            OppositeRole = Describe(oppositeRead, basis), ShiftedDonor = Describe(donorRead, donor),
            ColumnReflectionOppositeRole = Describe(mirrorRead, basis) };
        return new Relation(reader.Surface, S(sameRead, oppositeRead, donorRead,
            new Relation(S(mirrorConstruction.ToArray()), mirrorRead)));
        void Require(Relation read, QualitativePartition field, Relation map, bool expected)
        {
            if (!reader.TryReadResult(read, out _, out var candidates, out _) ||
                E(candidates).OfType<Relation>().SingleOrDefault(candidate => reader.TryReadCandidate(candidate,
                    out _, out _, out var actualMap, out _, out _, out _, out _) && ReferenceEquals(map, actualMap)) is not { } candidate ||
                reader.TryReadPassed(field, candidate) != expected)
                throw new InvalidOperationException("An authored correspondence did not meet its declared test.");
        }
    }

    private static object FieldSummary(QualitativePartition field)
    {
        if (!field.TryReadResult(field.Evaluate(), out var first, out var second, out var regions, out _, out var pending) || E(pending).Length != 0)
            throw new InvalidOperationException("An authored field has unresolved labels or topology.");
        return new { First = E(first).Length, Second = E(second).Length, Regions = E(regions).Length };
    }

    private static Relation FieldView(QualitativeWeave weave)
    {
        // Shared uncalibrated pitches receive independent positive view units.
        // Actual column and row identities bind to concrete spans only here.
        var horizontal = new Carrier(); var vertical = new Carrier();
        var columns = E(weave.Columns); var rows = E(weave.Rows);
        var spans = new List<IElement>();
        foreach (var coordinate in E(weave.Coordinates).Cast<Relation>())
        {
            var pair = (Relation)coordinate.Right;
            var x = Array.FindIndex(columns, column => ReferenceEquals(column, pair.Left));
            var y = Array.FindIndex(rows, row => ReferenceEquals(row, pair.Right));
            spans.Add(new Relation(coordinate, new Relation(Relation.Segment(horizontal, x, x + 1), Relation.Segment(vertical, y, y + 1))));
        }
        return new Relation(new Relation(weave.Surface, S(new Relation(weave.ColumnPitch, Relation.Segment(horizontal, 0, 1)),
            new Relation(weave.RowPitch, Relation.Segment(vertical, 0, 1)))), S(spans.ToArray()));
    }

    private static string Draw(IReadOnlyList<DrivePicture> drives, IReadOnlyList<FieldPicture> fields,
        QualitativeWeave weave)
    {
        const int width = 1460, header = 104, driveRow = 235, fieldRow = 226;
        var driveHeight = ((drives.Count + 1) / 2) * driveRow;
        var fieldTop = header + driveHeight + 68;
        var height = fieldTop + ((fields.Count + 1) / 2) * fieldRow + 58;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\"><style>:root{{--a:#202c31;--b:#f6f1e4}}.marks{{opacity:.85}}.hide-marks .marks{{display:none}}.invert{{--a:#f6f1e4;--b:#202c31}}</style><rect width=\"100%\" height=\"100%\" fill=\"var(--b)\"/><g font-family=\"system-ui,sans-serif\" fill=\"var(--a)\">");
        Text(24, 36, "One process, several useful perspectives", 28, "650");
        Text(24, 65, "Rhythms → motion applications → committed path → two-role field → selected edit → correspondence", 16);
        Text(24, 89, "Source laws and cells are pre-numeric. Distances and plot positions below are separate presentation choices.", 13);
        for (var i = 0; i < drives.Count; i++)
        {
            var picture = drives[i]; var x = 16 + i % 2 * 724; var y = header + i / 2 * driveRow;
            Card(x, y, 710, driveRow - 12); Text(x + 16, y + 27, picture.Name, 17, "600");
            Text(x + 16, y + 49, picture.Detail, 12);
            var points = picture.Paths.SelectMany(path => path).ToArray();
            var minY = points.Min(point => point.Y); var maxY = points.Max(point => point.Y);
            var maxX = points.Max(point => point.X); var scaleX = 650.0 / Math.Max(1, maxX);
            var scaleY = 112.0 / Math.Max(1, maxY - minY);
            var ox = x + 29; var oy = y + 76;
            svg.Append($"<path d=\"M{ox},{F(oy + maxY * scaleY)}h650\" stroke=\"#b9c4c0\" stroke-dasharray=\"3 5\"/>");
            for (var p = 0; p < picture.Paths.Count; p++)
            {
                var color = p == 0 ? "#177f82" : "#b44b81";
                string Position((long X, long Y) point) => F(ox + point.X * scaleX) + "," + F(oy + (maxY - point.Y) * scaleY);
                svg.Append($"<polyline points=\"{string.Join(' ', picture.Paths[p].Select(Position))}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"3\" stroke-linejoin=\"round\"/>");
                foreach (var point in picture.Paths[p])
                    svg.Append($"<circle cx=\"{F(ox + point.X * scaleX)}\" cy=\"{F(oy + (maxY - point.Y) * scaleY)}\" r=\"2.6\" fill=\"{color}\"/>");
            }
            Text(x + 16, y + driveRow - 28, $"{picture.Paths.Count} cursor trace(s); samples are marked; shared axes and reaches stay connected", 11);
        }
        Text(24, fieldTop - 37, "Inspect the field, choose a focus, retain the change", 23, "600");
        Text(24, fieldTop - 14, "Orange: quiet plateau or selected edit. Magenta: distinct contributing layers meet. Each band repeats one actual cell domain.", 12);
        var columns = E(weave.Columns); var rows = E(weave.Rows);
        var columnIndex = columns.Select((cell, index) => (cell, index)).ToDictionary(pair => pair.cell, pair => pair.index, Identity);
        var rowIndex = rows.Select((cell, index) => (cell, index)).ToDictionary(pair => pair.cell, pair => pair.index, Identity);
        var coordinates = E(weave.Coordinates).Cast<Relation>().Select(row =>
            (Cell: row.Left, X: columnIndex[((Relation)row.Right).Left], Y: rowIndex[((Relation)row.Right).Right])).ToArray();
        for (var i = 0; i < fields.Count; i++)
        {
            var picture = fields[i]; var x = 16 + i % 2 * 724; var y = fieldTop + i / 2 * fieldRow;
            Card(x, y, 710, fieldRow - 12); Text(x + 16, y + 27, picture.Name, 17, "600");
            Text(x + 16, y + 49, picture.Detail, 12);
            var labels = E(picture.Partition.Labels).Cast<Relation>().ToDictionary(row => row.Left, row => row.Right, Identity);
            var cellSize = Math.Min(14, 124.0 / rows.Length); var unitWidth = columns.Length * cellSize;
            var repeats = Math.Max(1, (int)(660 / unitWidth)); var ox = x + (710 - unitWidth * repeats) / 2; var oy = y + 68;
            for (var repeat = 0; repeat < repeats; repeat++)
                foreach (var coordinate in coordinates)
                {
                    var first = labels.TryGetValue(coordinate.Cell, out var role) && ReferenceEquals(role, picture.Partition.RolePair.Left);
                    var px = ox + (coordinate.X + repeat * columns.Length) * cellSize;
                    var py = oy + (rows.Length - 1 - coordinate.Y) * cellSize;
                    svg.Append($"<rect x=\"{F(px)}\" y=\"{F(py)}\" width=\"{F(cellSize + .02)}\" height=\"{F(cellSize + .02)}\" fill=\"var(--{(first ? 'a' : 'b')})\"/>");
                    if (picture.Marked?.Contains(coordinate.Cell) == true)
                        svg.Append($"<rect class=\"marks\" x=\"{F(px + 1)}\" y=\"{F(py + 1)}\" width=\"{F(cellSize - 2)}\" height=\"{F(cellSize - 2)}\" fill=\"{picture.Mark}\"/>");
                }
            Text(x + 16, y + fieldRow - 29, $"{labels.Values.Count(role => ReferenceEquals(role, picture.Partition.RolePair.Left))} dark / {labels.Values.Count(role => ReferenceEquals(role, picture.Partition.RolePair.Right))} light cells in selected period", 11);
        }
        Text(24, height - 27, "Quiet and busy are selected questions. Color exchange, geometric reflection and Gojo duality remain different operations.", 13);
        return svg.Append("</g></svg>").ToString();
        void Card(int x, int y, int w, int h) => svg.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{w}\" height=\"{h}\" rx=\"10\" fill=\"none\" stroke=\"#c9c9bb\"/>");
        void Text(double x, double y, string value, int size, string weight = "400") => svg.Append($"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{size}\" font-weight=\"{weight}\">{WebUtility.HtmlEncode(value)}</text>");
    }

    private static string Html(string svg) => "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Pattern layers and field analysis</title><style>body{margin:0;background:#f6f1e4;color:#202c31;font:16px system-ui}main{max-width:1460px;margin:auto;padding:24px}p{max-width:1100px;line-height:1.6}svg{width:100%;height:auto}.controls{position:sticky;top:0;background:#f6f1e4;padding:14px;display:flex;gap:24px;z-index:2}.invert svg{--a:#f6f1e4;--b:#202c31}.hide-marks .marks{display:none}a{color:#176e7b}</style><main><h1>Pattern layers: inspect, then edit</h1><p>These are deliberately chosen experiments. The phase networks and finite laws produce the motion; separate sampling connections commit a path. Field operations read actual cells, roles and adjacency. The white role participates just as explicitly as the dark role.</p><div class=\"controls\"><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('invert',this.checked)\">Exchange display colours</label><label><input type=\"checkbox\" checked onchange=\"document.body.classList.toggle('hide-marks',!this.checked)\">Show analysis focus</label></div>" + svg + "<p>A zigzag and a staircase can use the same driving laws with different sampling. Several cursors can be observed in a common declared frame while keeping their histories separate. A final field can support several matching motifs and maps; it cannot uniquely recover the original clocks or drawing order.</p><p>The edits are explicit observational union and subtraction passes. Quiet means a local maximum of ordinal graph clearance from the selected role. Crowded means distinct declared contributors occupy the same cell; this alone does not prove geometric retracing or aesthetic failure. Native footprints, old and new labels, law reads and carried complements remain attached. These are mechanics for future training, with authored preferences and no learned reward or automatic optimizer.</p><p><a href=\"gallery.svg\">Standalone SVG</a> · <a href=\"diagnostics.json\">Diagnostics</a> · <a href=\"../shaping/gallery.html\">Earlier shaping gallery</a></p></main></html>";

    private static object Counts(IElement source)
    {
        var elements = Traverse(source).ToArray();
        return new { Elements = elements.Length, Carriers = elements.Count(value => value is Carrier), Locations = elements.Count(value => value is CarrierLocation) };
    }
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(source);
        while (stack.TryPop(out var item))
        { if (!seen.Add(item)) continue; yield return item; if (item is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); } }
    }
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
