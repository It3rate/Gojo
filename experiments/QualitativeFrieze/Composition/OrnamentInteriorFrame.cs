using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Attachment = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentMotifAttachment;
using Substrate = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentSubstrate;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A native construction landmark read from selected actual boundary
/// endpoints or exposed mesh ports. Closure by shared sites is distinct from a
/// chosen scaffold. An endpoint-average landmark is neither an area centroid
/// nor a proof of interior containment or fill.</summary>
public static class OrnamentInteriorFrame
{
    // surface = (frame -> selection) -> (corner rows -> (join rows -> calculation))
    // cycle selection = Closed(actual ordered curve cycle)
    // scaffold selection = actual curve section scope -> exposed port scope
    // corner row = actual selected section/port -> actual endpoint/port word
    // join row = (section -> next section) -> witnessed endpoint difference
    // calculation = ((empty seed -> additive receipts) -> actual sum output) ->
    //               (corner rows -> (equal partition groups -> (first group -> agreements)))
    // Every actual surviving move of the sum participates in exactly one group.
    // The number of groups is the number of selected endpoint rows. The first
    // group's actual word is the output; all other groups retain zero-difference
    // witnesses. An unavailable exact partition remains explicitly open.

    public sealed record Read(Relation[] Sections, Relation[] Corners, Relation Centre,
        bool ClosedBySite, bool CentreComplete, bool Complete, bool Scaffold, Relation Surface);

    public static Read FromCycle(Curve.Frame frame, Relation orderedCurveCycle)
    {
        var source = Items(orderedCurveCycle).OfType<Relation>().ToArray();
        var curves = source.Select(frame.Read).ToArray();
        var corners = curves.Select(c => c.Complete ? Word(c, true) : StructuralConstraint.Open(c.Section)).ToArray();
        var rows = S(source.Select((s, i) => (IElement)new Relation(s, corners[i])).ToArray());
        var joins = S(curves.Select((c, i) => (IElement)new Relation(new Relation(c.Section, curves[(i + 1) % curves.Length].Section),
            c.Complete && curves[(i + 1) % curves.Length].Complete ?
                Substrate.Difference(frame, Word(c, false), corners[(i + 1) % curves.Length]) : StructuralConstraint.Open(c.Section))).ToArray());
        var selection = S(orderedCurveCycle);
        return ReadFrame(frame, new Relation(new Relation(frame.Surface, selection),
            new Relation(rows, new Relation(joins, Calculate(frame, rows, corners)))));
    }

    /// <summary>Ports must select actual sections in the supplied closed scope.
    /// They may be endpoints or witnessed straight interior encounters. These
    /// observations do not split the strands or assert a closed region.</summary>
    public static Read FromScaffold(Curve.Frame frame, Relation actualCurveSections, Relation selectedPorts)
    {
        var ports = Items(selectedPorts).OfType<Relation>().Select(p => Attachment.ReadPort(frame, p)).ToArray();
        var rows = S(ports.Select(p => (IElement)new Relation(p.Surface, p.Position)).ToArray());
        var joins = StructuralConstraint.Open(actualCurveSections, selectedPorts);
        var selection = new Relation(actualCurveSections, selectedPorts);
        return ReadFrame(frame, new Relation(new Relation(frame.Surface, selection),
            new Relation(rows, new Relation(joins, Calculate(frame, rows, ports.Select(p => p.Position).ToArray())))));
    }

    public static Read ReadFrame(Curve.Frame frame, Relation surface)
    {
        Read Open(Relation[]? sections = null, Relation[]? corners = null, bool scaffold = false, bool closed = false) =>
            new(sections ?? [], corners ?? [], StructuralConstraint.Open(surface), closed, false, false, scaffold, surface);
        if (surface is not { Left: Relation { Left: var selectedFrame, Right: Relation selection },
                Right: Relation { Left: Relation cornerScope, Right: Relation { Left: Relation joins, Right: Relation calculation } } } ||
            !Same(selectedFrame, frame.Surface) || !Entries(cornerScope, out var cornerRows) || cornerRows.Length is < 2 or > 128) return Open();
        Relation[] sections; Relation[] corners; var scaffold = !Entries(selection, out var selected); var closed = false;
        if (!scaffold)
        {
            if (selected is not [Relation cycle] || !Entries(cycle, out var values) || values.Length != cornerRows.Length ||
                values.Any(v => v is not Relation) || !Entries(joins, out var joinRows) || joinRows.Length != values.Length) return Open();
            sections = values.Cast<Relation>().ToArray(); var curves = sections.Select(frame.Read).ToArray();
            if (curves.Any(c => !c.Complete)) return Open(sections);
            corners = curves.Select(c => Word(c, true)).ToArray(); closed = true;
            for (var i = 0; i < curves.Length; i++)
            {
                var next = (i + 1) % curves.Length;
                if (!Corner(cornerRows[i], sections[i], corners[i]) || joinRows[i] is not Relation { Left: Relation pair, Right: Relation difference } ||
                    !Same(pair.Left, sections[i]) || !Same(pair.Right, sections[next])) return Open(sections, corners);
                var incident = Substrate.ReadDifference(frame, difference, Word(curves[i], false), corners[next]);
                if (incident is null) return Open(sections, corners);
                closed &= incident == true && Same(curves[i].End, curves[next].Start);
            }
            if (!closed) return Open(sections, corners);
        }
        else
        {
            if (selection is not { Left: Relation sectionScope, Right: Relation portScope } ||
                !Entries(sectionScope, out var values) || values.Length == 0 || values.Any(v => v is not Relation) ||
                !Entries(portScope, out var portValues) || portValues.Length != cornerRows.Length || portValues.Any(p => p is not Relation) ||
                !OpenScope(joins, out var pending) || pending is not [var pendingSections, var pendingPorts] ||
                !Same(pendingSections, sectionScope) || !Same(pendingPorts, portScope)) return Open(scaffold: true);
            sections = values.Cast<Relation>().ToArray();
            if (sections.Any(s => !frame.Read(s).Complete)) return Open(sections, scaffold: true);
            var ports = portValues.Cast<Relation>().Select(p => Attachment.ReadPort(frame, p)).ToArray();
            corners = ports.Select(p => p.Position).ToArray();
            for (var i = 0; i < ports.Length; i++)
                if (!ports[i].Complete || !sections.Any(s => Same(s, ports[i].Section)) || !Corner(cornerRows[i], ports[i].Surface, corners[i]))
                    return Open(sections, corners, true);
        }
        if (!ReadCalculation(frame, calculation, cornerScope, corners, out var centre)) return Open(sections, corners, scaffold, closed);
        return new(sections, corners, centre, closed, true, true, scaffold, surface);
    }

    private static Relation Calculate(Curve.Frame frame, Relation cornerRows, Relation[] corners)
    {
        var seed = S(); var sum = seed; var receipts = new List<IElement>();
        foreach (var corner in corners)
        {
            var receipt = frame.Motion.Reduce(sum, corner); receipts.Add(receipt);
            sum = frame.Motion.TryReadResult(receipt, out var actual, out _, out _) ? actual : StructuralConstraint.Open(receipt);
        }
        Relation groups = StructuralConstraint.Open(sum); Relation centre = StructuralConstraint.Open(sum, cornerRows); var agreements = S();
        if (corners.Length >= 2 && Entries(sum, out var moves) && frame.Vector(sum, out _))
        {
            var byDirection = moves.Cast<Relation>().GroupBy(m => m.Left, ReferenceEqualityComparer.Instance).ToArray();
            if (byDirection.All(g => g.Count() % corners.Length == 0))
            {
                var parts = Enumerable.Range(0, corners.Length).Select(_ => new List<IElement>()).ToArray();
                foreach (var direction in byDirection)
                {
                    var all = direction.ToArray(); var each = all.Length / corners.Length;
                    for (var i = 0; i < parts.Length; i++) parts[i].AddRange(all.Skip(i * each).Take(each));
                }
                var words = parts.Select(p => S(p.ToArray())).ToArray(); groups = S(words.Cast<IElement>().ToArray()); centre = words[0];
                agreements = S(words.Skip(1).Select(w => (IElement)Substrate.Difference(frame, centre, w)).ToArray());
            }
        }
        return new(new Relation(new Relation(seed, S(receipts.ToArray())), sum), new Relation(cornerRows,
            new Relation(groups, new Relation(centre, agreements))));
    }

    private static bool ReadCalculation(Curve.Frame frame, Relation surface, Relation cornerRows, Relation[] corners, out Relation centre)
    {
        centre = null!;
        if (surface is not { Left: Relation { Left: Relation { Left: Relation seed, Right: Relation receipts }, Right: Relation sum },
                Right: Relation { Left: var denominator, Right: Relation { Left: Relation groups,
                    Right: Relation { Left: Relation output, Right: Relation agreements } } } } ||
            !Same(denominator, cornerRows) || !Entries(seed, out var empty) || empty.Length != 0 ||
            !Entries(receipts, out var additions) || additions.Length != corners.Length ||
            !Entries(groups, out var parts) || parts.Length != corners.Length || parts.Any(p => p is not Relation) ||
            !Entries(agreements, out var differences) || differences.Length != corners.Length - 1) return false;
        var before = seed;
        for (var i = 0; i < additions.Length; i++)
        {
            if (additions[i] is not Relation receipt || receipt.Left is not Relation { Right: Relation sources } ||
                !Same(sources.Left, before) || !Same(sources.Right, corners[i]) ||
                !frame.Motion.TryReadResult(receipt, out var actual, out _, out _)) return false;
            before = actual;
        }
        if (!Same(sum, before) || !frame.Vector(sum, out _) || !Entries(sum, out var survivors) || !Same(output, parts[0])) return false;
        var occurrences = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        foreach (var move in survivors) { occurrences.TryGetValue(move, out var count); occurrences[move] = count + 1; }
        for (var i = 0; i < parts.Length; i++)
        {
            if (!Entries((Relation)parts[i], out var moves) || !frame.Vector((Relation)parts[i], out _)) return false;
            foreach (var move in moves)
            {
                if (!occurrences.TryGetValue(move, out var count) || count == 0) return false;
                occurrences[move] = count - 1;
            }
            if (i > 0 && (differences[i - 1] is not Relation difference ||
                Substrate.ReadDifference(frame, difference, output, (Relation)parts[i]) != true)) return false;
        }
        if (occurrences.Values.Any(n => n != 0)) return false;
        centre = output; return true;
    }

    private static bool Corner(IElement row, Relation source, Relation word) => row is Relation pair && Same(pair.Left, source) && Same(pair.Right, word);
    private static Relation Word(Curve.Read curve, bool start) => (Relation)Items(curve.ControlWords)[start ? 0 : curve.Controls.Length - 1];
    private static bool OpenScope(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && !seam.Equals(tail);
    private static bool Entries(Relation scope, out IElement[] entries) => StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] Items(Relation scope) => Entries(scope, out var entries) ? entries : [];
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
}
