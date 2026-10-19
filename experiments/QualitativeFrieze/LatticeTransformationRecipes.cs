using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.LatticeLadderAuthor;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Geometry-changing successors to selected catalogue constructions.
/// Profiles replace actual native chord occurrences, with endpoint-preserving
/// reduction evidence. Cuts and shared-run fusions remain separate edits.
/// Authoring coordinates below are read only to select native instructions;
/// execution consumes the resulting path words, ports, periods and receipts.</summary>
public sealed partial class LatticeTransformationRecipes
{
    public IReadOnlyList<LatticeCatalogueCase> Cases { get; }
    public IReadOnlyDictionary<string, int> BasisStageIndices { get; }
    private readonly int columns;
    private readonly int rows;

    private static readonly string[] SourceIds = ["C001", "C026", "C031", "C041", "C056", "C066", "C071", "C081", "C086", "C096"];
    private static readonly string[] OperationNames = ["Broad square shoulders", "Diamond-shoulder borders", "Repeated pointed teeth",
        "Alternating concave and convex bends", "Two-stage staircase borders", "Broad bulbs with narrow necks",
        "Fused shared spines", "Fused spines with open returns", "Released dividers and notched windows", "Stepped borders and open chambers"];

    public LatticeTransformationRecipes(int columns = 3, int rows = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        this.columns = columns; this.rows = rows;
        var basis = new LatticeCatalogueRecipes(columns, rows);
        var cases = new List<LatticeCatalogueCase>(); var indices = new Dictionary<string, int>();
        foreach (var sourceId in SourceIds)
        {
            var source = basis.Cases.Single(item => item.Id == sourceId);
            for (var operation = 0; operation < 10; operation++)
            {
                var id = $"T{cases.Count + 1:000}";
                // Compare surgery within its selected fine reach. Counting
                // the preparatory uniform substitution as new shape support
                // would confuse a basis refinement with a visible mutation.
                indices.Add(id, source.Stages.Count);
                cases.Add(Build(source, operation, id));
            }
        }
        Cases = cases.AsReadOnly(); BasisStageIndices = indices;
    }

    private LatticeCatalogueCase Build(LatticeCatalogueCase basis, int operation, string id)
    {
        var stages = basis.Stages.ToList(); var source = basis.Current.Ladder;
        var frame = source.Current.Program.Frame;
        var reach = E(source.Current.Program.Paths).Cast<Relation>().SelectMany(path => E((Relation)path.Right).Cast<Relation>())
            .SelectMany(E).Cast<Relation>().Select(move => move.Right).Distinct(FieldNative.Participants.Instance).Single();
        var a = new LatticeLadderAuthor(frame, reach);
        var scale = a.Map(a.Word(4, 0), a.Word(0, 4));
        var column = a.MapWord(scale, basis.Current.Recipe.ColumnPitch, out var columnRead);
        var row = a.MapWord(scale, basis.Current.Recipe.RowPitch, out var rowRead);
        var scaled = source.Transform("Expose four fine reaches within each old reach", scale);
        Append(scaled, "Expose fine surgery landmarks",
            "The actual paths and both period words follow the same four-occurrence substitution. This rung selects a finer authoring reach; it does not change the visible shape by itself.",
            new Relation(columnRead, rowRead));

        if (basis.Id == "C066")
        {
            var retargeted = RetargetCommonStarts(source, a, a.Word(8, 0));
            Append(retargeted, "Move the common three-way chamber hub",
                "All three actual closed routes consume the same displaced hub landmark. The first and final chords change through native reductions while intermediate boundary chords and publication roles are carried. This is a start-site retargeting edit, not an endpoint-preserving refinement.",
                retargeted.Current.Correspondence);
        }

        var geometry = Segments(source.Current.Program, a).ToArray();
        var focus = geometry.Where(segment => segment.Vector != default).OrderByDescending(segment => Gcd(segment.Vector.X, segment.Vector.Y))
            .ThenByDescending(segment => Math.Abs(segment.Vector.X) + Math.Abs(segment.Vector.Y)).First();
        var canonical = Canonical(focus.Vector); var length = Gcd(canonical.X, canonical.Y);
        var tangent = new V(canonical.X / length, canonical.Y / length);
        var normal = new V(-tangent.Y, tangent.X);
        // On the diagonal diamond frame an equal tangential/normal lead
        // would lie exactly on an old radial spoke. Select a distinct native
        // landmark so this edit creates an oblique shoulder, not an accidental
        // partially coincident drawing history.
        var depth = Math.Max(2, length / 4) + (basis.Id == "C086" ? 1 : 0);
        var profile = Profile(operation, length, depth);
        var rows = geometry.Where(segment => Canonical(segment.Vector) == canonical).Select(segment => segment.Chord)
            .Distinct(FieldNative.Participants.Instance).Cast<Relation>().Select(chord =>
            {
                var vector = Vector(frame, chord); var positive = vector == canonical;
                var chain = S(profile.Zip(profile.Skip(1), (from, to) =>
                    (IElement)a.Word(tangent.X * (to.X - from.X) + normal.X * (to.Y - from.Y),
                        tangent.Y * (to.X - from.X) + normal.Y * (to.Y - from.Y))).ToArray());
                return (IElement)new Relation(chord, positive ? chain : LatticeGeometrySurgery.InverseChain(frame, chain));
            }).ToArray();
        var profileEvidence = new Relation(S(geometry.Where(segment => Canonical(segment.Vector) == canonical)
            .Select(segment => (IElement)new Relation(segment.Path, segment.Chord)).ToArray()),
            new Relation(a.Map(a.Word(0, 1), a.Word(-1, 0)).Surface, S(rows)));
        var profiled = LatticeGeometrySurgery.RefineAndPublish(source, OperationNames[operation], S(rows), profileEvidence);
        Append(profiled, OperationNames[operation],
            "Actual selected chord identities become a new ordered chain with the same endpoint displacement. Shared opposed outlines receive the reverse of the same profile. Newly introduced vertices are then published explicitly.", profileEvidence);

        if (basis.Id == "C031")
        {
            // A one-axis edit of an already keyed boundary can read as a few
            // minor extra teeth. A second actual direction receives a different
            // coordinated profile, producing a new two-stage compartment.
            var currentSegments = Segments(source.Current.Program, a).ToArray();
            var secondFocus = currentSegments.Where(segment => Cross(segment.Vector, canonical) != 0)
                .OrderByDescending(segment => Gcd(segment.Vector.X, segment.Vector.Y)).First();
            var secondVector = Canonical(secondFocus.Vector); var secondLength = Gcd(secondVector.X, secondVector.Y);
            var secondTangent = new V(secondVector.X / secondLength, secondVector.Y / secondLength);
            var secondNormal = new V(-secondTangent.Y, secondTangent.X);
            var initialDepth = Math.Max(2, secondLength / 4);
            var rejected = new List<IElement>();
            QualitativeLatticeLadder? secondEdit = null; Relation? secondInstruction = null;
            foreach (var candidateDepth in new[] { initialDepth, initialDepth - 1, initialDepth + 1, -initialDepth,
                         1 - initialDepth, -initialDepth - 1, Math.Max(1, initialDepth / 2), -Math.Max(1, initialDepth / 2) }.Where(value => value != 0).Distinct())
            {
                var secondProfile = Profile((operation + 4) % 6, secondLength, candidateDepth);
                var secondRows = currentSegments.Where(segment => Canonical(segment.Vector) == secondVector).Select(segment => segment.Chord)
                    .Distinct(FieldNative.Participants.Instance).Cast<Relation>().Select(chord =>
                    {
                        var chain = S(secondProfile.Zip(secondProfile.Skip(1), (from, to) => (IElement)a.Word(
                            secondTangent.X * (to.X - from.X) + secondNormal.X * (to.Y - from.Y),
                            secondTangent.Y * (to.X - from.X) + secondNormal.Y * (to.Y - from.Y))).ToArray());
                        return (IElement)new Relation(chord, Vector(frame, chord) == secondVector ? chain : LatticeGeometrySurgery.InverseChain(frame, chain));
                    }).ToArray();
                var instruction = S(secondRows);
                var candidate = LatticeGeometrySurgery.RefineAndPublish(source, "Reform the perpendicular keyed shoulders", instruction);
                if (PathOverlap(candidate.Current.Program, a) is { } violation)
                {
                    rejected.Add(new Relation(candidate.Surface, violation)); continue;
                }
                secondInstruction = new Relation(instruction, S(rejected.ToArray()));
                secondEdit = rejected.Count == 0 ? candidate : LatticeGeometrySurgery.RefineAndPublish(source,
                    "Reform the perpendicular keyed shoulders", instruction, secondInstruction);
                break;
            }
            if (secondEdit is null || secondInstruction is null) throw new InvalidOperationException(id + ": the bounded perpendicular shoulder proposals all retraced their path.");
            Append(secondEdit, "Reform the perpendicular keyed shoulders",
                "A second actual direction family receives a different endpoint-conserving hook or stepped profile. The two coordinated surgeries change the compartment in both directions while retaining the original keyed boundary as a readable ancestor.", secondInstruction);
        }

        var acceptedFusion = false;
        if (operation is 6 or 7)
        {
            var segments = Segments(source.Current.Program, a).ToArray();
            var pair = FindFusionPair(segments);
            if (pair is not null)
            {
                var ordered = Canonical(pair.Value.First.Vector) == pair.Value.First.Vector;
                var sharedStart = ordered ? pair.Value.Start : pair.Value.End;
                var sharedEnd = ordered ? pair.Value.End : pair.Value.Start;
                var fused = LatticeGeometrySurgery.FuseChordRuns(source, "Converge two routes through a shared middle run",
                    pair.Value.First.Path, pair.Value.First.Chord, pair.Value.Second.Path, pair.Value.Second.Chord,
                    a.Word(sharedStart.X, sharedStart.Y), a.Word(sharedEnd.X, sharedEnd.Y));
                acceptedFusion = true;
                Append(fused, "Converge routes through a shared spine",
                    "Two actual parallel chord occurrences are rerouted through one declared middle interval. Both approaches retain their endpoint obligations, while the repeated field joins the two published spine endpoints through explicit port agreements.",
                    new Relation(new Relation(pair.Value.First.Path, pair.Value.First.Chord), new Relation(pair.Value.Second.Path, pair.Value.Second.Chord)));
            }
            else OpenWindow("Open a return where no parallel pair is available");
        }
        if (operation >= 7) OpenWindow(operation == 8 ? "Withdraw a divider or open a boundary window" : "Open an alternate chamber return");

        if (basis.Id is "C031" or "C086")
        {
            var agreed = LatticeGeometrySurgery.ConformPublishedContacts(source, "Negotiate published boundary contacts", column, row);
            if (!ReferenceEquals(agreed, source))
                Append(agreed, "Negotiate published boundary contacts",
                    "Published landmark proposals from this unit and its declared neighbouring periods are read against actual chord spans. Endpoint-conserving splits and new publication roles expose the accepted fine contacts; the repeated field still validates their native seam agreements separately.",
                    agreed.Current.Correspondence);
        }

        var outcomeName = operation is 6 or 7 && !acceptedFusion ? "Shouldered routes with opened returns" : OperationNames[operation];
        return new(id, $"{outcomeName} / {basis.Family}", outcomeName,
            $"Starts from {basis.Id}: {basis.Name}. Changes actual path geometry and, where selected, removes or fuses participation. Earlier grid, compartment and motif reads stay available as native ancestors.", stages.AsReadOnly());

        void Append(QualitativeLatticeLadder changed, string name, string description, Relation evidence)
        {
            var previousDepth = source.Rungs.Count;
            foreach (var rung in changed.Rungs.Skip(previousDepth))
            {
                var prefix = changed;
                if (!ReferenceEquals(rung, changed.Current) && !QualitativeLatticeLadder.TryRead(frame, rung.Surface, out prefix))
                    throw new InvalidOperationException("A geometry surgery rung must retain an exact native replay contract.");
                var recipe = a.Compile(id + "-s" + stages.Count, name, description, "Native geometric mutation; no historical plate equivalence is asserted.",
                    prefix.Current.Program, column, row, columns, this.rows);
                stages.Add(new(name, description, prefix, recipe, evidence));
            }
            source = changed;
        }

        void OpenWindow(string name)
        {
            var paths = E(source.Current.Program.Paths).Cast<Relation>().ToArray();
            var candidates = paths.Select(path => (Path: path, Words: E((Relation)path.Right).Cast<Relation>().ToArray())).ToArray();
            var simple = candidates.Where(item => item.Words.Length == 1).OrderBy(item => Math.Abs(Vector(frame, item.Words[0]).X) + Math.Abs(Vector(frame, item.Words[0]).Y)).FirstOrDefault();
            if (simple.Path is not null && paths.Length > 1)
            {
                var deleted = LatticeGeometrySurgery.DeletePaths(source, name, S(simple.Path), simple.Path);
                Append(deleted, name, "An actual single-run divider is withdrawn along with its endpoint roles. Adjacent compartments can now be read together; the old divider remains in the earlier rung.", simple.Path);
                return;
            }
            var chosen = Segments(source.Current.Program, a).OrderByDescending(segment => Gcd(segment.Vector.X, segment.Vector.Y))
                .First(segment => Gcd(segment.Vector.X, segment.Vector.Y) >= 3);
            var removedDirection = Canonical(chosen.Vector);
            // The opposed boundary of the same repeated cell must lose the
            // same visible span. Cutting only one drawing history would leave
            // the neighbour's carried counterpart covering the alleged gap.
            while (Segments(source.Current.Program, a).FirstOrDefault(segment => Canonical(segment.Vector) == removedDirection) is { } occurrence)
            {
                var vector = occurrence.Vector; var count = Gcd(vector.X, vector.Y); var part = Math.Max(1, count / 3);
                var direction = new V(vector.X / count, vector.Y / count);
                var lead = a.Word(direction.X * part, direction.Y * part);
                var gap = a.Word(direction.X * (count - 2 * part), direction.Y * (count - 2 * part));
                var trail = a.Word(direction.X * part, direction.Y * part);
                var cut = LatticeGeometrySurgery.CutChordAndPublish(source, name, occurrence.Path, occurrence.Chord, lead, gap, trail);
                Append(cut, name, "The middle span is removed from each matching ordered boundary occurrence, including its opposed shared-outline counterpart. Native prefix/suffix witnesses locate the new fragments; the old closure obligations are withdrawn explicitly.",
                    new Relation(occurrence.Path, new Relation(occurrence.Chord, S(lead, gap, trail))));
            }
        }
    }

    private readonly record struct V(int X, int Y)
    {
        public static V operator +(V a, V b) => new(a.X + b.X, a.Y + b.Y);
        public static V operator -(V a, V b) => new(a.X - b.X, a.Y - b.Y);
        public static V operator *(V a, int factor) => new(a.X * factor, a.Y * factor);
    }
    private sealed record Segment(Relation Path, Relation Chord, V Start, V End)
    { public V Vector => End - Start; }

    private static IEnumerable<Segment> Segments(UnitArrangementProgram unit, LatticeLadderAuthor a)
    {
        foreach (var path in E(unit.Paths).Cast<Relation>())
        {
            var at = Vector(a.Frame, (Relation)path.Left);
            foreach (var word in E((Relation)path.Right).Cast<Relation>())
            { var next = at + Vector(a.Frame, word); yield return new(path, word, at, next); at = next; }
        }
    }

    private static V Vector(QualitativeMotionFrame frame, Relation word)
    {
        var result = new V();
        foreach (var move in E(word).Cast<Relation>())
        {
            if (!frame.Direction(move, out var axis, out var forward, out _)) throw new ArgumentException("A surgery word needs a complete native direction.");
            result += ReferenceEquals(axis, frame.Horizontal) ? new V(forward ? 1 : -1, 0) : new V(0, forward ? 1 : -1);
        }
        return result;
    }
    private static V Canonical(V vector) => vector.X < 0 || vector.X == 0 && vector.Y < 0 ? vector * -1 : vector;
    private static int Gcd(int x, int y) { x = Math.Abs(x); y = Math.Abs(y); while (y != 0) (x, y) = (y, x % y); return x; }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
