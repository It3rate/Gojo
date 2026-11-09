using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Small authored changes with retained source correspondence. Names
/// describe the read; substitutions, connections and contour promises execute it.</summary>
public static class ShapeNeighborhoodStudy
{
    public sealed record Neighbor(string Id, string Name, string ChangeKind, string Change,
        string Retains, string Limits, long[][][] Lines, int RadialParts, bool WholeContour,
        bool? EqualSectionLengths, bool? PerpendicularJoins, int? Components, int? Sections)
    {
        [JsonIgnore] public Relation NativeSurface { get; init; } = null!;
        [JsonIgnore] public Relation? NativeCircle { get; init; }
        [JsonIgnore] public Relation? NativePartition { get; init; }
    }
    public sealed record Report(Neighbor[] Shapes, string[] Findings);
    public sealed record GeometryRead(bool? EqualSectionLengths, bool? PerpendicularJoins, Relation Evidence);

    public static Report Explore()
    {
        var f = new Curve.Frame(new QualitativeMotionFrame());
        var rectangleMachine = ConstructionExchangeMachine.Create(f, 0);
        var squareMachine = rectangleMachine.WithEqualExtents();
        var square = squareMachine.Execute(squareMachine.Publish(2, 2, 0));
        var rectangle = rectangleMachine.Execute(rectangleMachine.Publish(3, 2, 0));
        var results = new List<Neighbor>();
        results.Add(Planar("square", "Square with a shared extent", "binding retained",
            "Both extents are joined by an equality obligation.", "Ordered loop, four phases, right corners, equal sides.",
            "A rectangular offer is rejected until the equality obligation is released.", square.Sections, square.Surface));
        results.Add(Planar("rectangle", "Rectangle", "release binding",
            "Release W = H; the two opposing pairs still share their respective reach.", "Four phases, closure, right corners, opposing side correspondence.",
            "Releasing equality does not remove the turn or closure obligations.", rectangle.Sections,
            S(new Relation(squareMachine.Surface, rectangleMachine.Surface), rectangle.Surface)));

        var diagonal = CentrelineAction.Create(f, OrnamentBandComposition.Map(f,
            f.Word(1, 1), f.Word(-1, -1), f.Word(-1, 1), f.Word(1, -1)), f.Word(0, 0));
        var stretch = CentrelineAction.Create(f, OrnamentBandComposition.Map(f,
            f.Word(2, 0), f.Word(-2, 0), f.Word(0, 1), f.Word(0, -1)), f.Word(0, 0));
        var shear = CentrelineAction.Create(f, OrnamentBandComposition.Map(f,
            f.Word(1, 0), f.Word(-1, 0), f.Word(1, 1), f.Word(-1, -1)), f.Word(0, 0));
        var d = Apply(diagonal, square.Sections);
        var sd = Apply(stretch, d.Output);
        var s = Apply(stretch, square.Sections);
        var ds = Apply(diagonal, s.Output);
        var h = Apply(shear, square.Sections);
        results.Add(Planar("diagonal-square", "Diagonally transformed square", "word substitution",
            "Map each H step to H+V and each V step to -H+V.", "Equal sides, right corners, ordered closed loop.",
            "This integer-word map turns 45 degrees AND scales by sqrt(2); it is not a unit-length rotation.", d.Output, S(square.Surface, d.Surface)));
        results.Add(Planar("rhombus", "Rhombus: turn, then stretch", "word substitution",
            "Apply the diagonal map, then double only the horizontal component.", "Equal sides, closure, opposing sides.",
            "Right corners no longer survive. Equal sides alone do not imply a square.", sd.Output, S(square.Surface, d.Surface, sd.Surface)));
        results.Add(Planar("diagonal-rectangle", "Rectangle: stretch, then turn", "word substitution",
            "Double the original horizontal component, then apply the diagonal map.", "Right corners, closure, opposing sides.",
            "Equal adjacent sides no longer survive. The same two actions in the other order give a rhombus.", ds.Output, S(square.Surface, s.Surface, ds.Surface)));
        results.Add(Planar("parallelogram", "Sheared loop", "word substitution",
            "Add one horizontal step to every upward step, and its inverse to every downward step.", "Closure, order, parallel opposing sides.",
            "Neither equal adjacent sides nor right corners is preserved here.", h.Output, S(square.Surface, h.Surface)));

        // Extensions are new sections with correspondence to the old boundaries.
        // Their geometric intersections deliberately do not create shared sites.
        var supports = new List<IElement>(); var extensions = new List<IElement>();
        var oppose = CentrelineAction.FromBits(f, 6, f.Word(0, 0));
        var triple = OrnamentBandComposition.Map(f,
            f.Word(3, 0), f.Word(-3, 0), f.Word(0, 3), f.Word(0, -3));
        foreach (var edge in E(square.Sections).Cast<Relation>())
        {
            // x -> 3x - (a+b), for each endpoint x. The opposed endpoint
            // words, their sum, and the final substitution all retain native
            // application receipts. No screen-coordinate extrapolator chooses
            // the new endpoints. Each independent application makes new sites.
            var negated = Apply(oppose, S(edge));
            var words = E(f.Read((Relation)E(negated.Output)[0]).ControlWords).Cast<Relation>().ToArray();
            var sum = f.Motion.Reduce(words[0], words[^1]);
            if (!f.Motion.TryReadResult(sum, out var displacement, out _, out var unresolved) || E(unresolved).Length != 0)
                throw new InvalidOperationException("The support extension has an unresolved displacement.");
            var extensionAction = CentrelineAction.Create(f, triple, displacement);
            var extended = Apply(extensionAction, S(edge));
            supports.AddRange(E(extended.Output));
            extensions.Add(new Relation(edge, S(negated.Surface, sum, extensionAction, extended.Surface)));
        }
        var hash = S(supports.ToArray()); var rotatedHash = Apply(diagonal, hash);
        results.Add(Planar("hash", "Extended supporting lines", "change connectivity/focus",
            "Select the extended supports instead of the bounded perimeter.", "Four support directions and correspondence to the source edges.",
            "The four strokes cross in projection; they are not welded. This is not a closed boundary.", hash, S(square.Surface, S(extensions.ToArray()))));
        results.Add(Planar("rotated-hash", "Diagonal supporting lines", "word substitution",
            "Apply the same diagonal map to the four extended supports.", "Four separate strokes; projected crossings.",
            "Joining crossings would be an additional topology change.", rotatedHash.Output, S(S(extensions.ToArray()), rotatedHash.Surface)));

        foreach (var (id, name, option, description) in new[]
        {
            ("notch", "Notched loop", 5, "Supply a six-phase candidate with a nonzero detour and a checked return."),
            ("open", "Open three-step path", 6, "Supply a three-phase candidate with no return edge.")
        })
        {
            var machine = ConstructionExchangeMachine.Create(f, option);
            var execution = machine.Execute(machine.Publish(3, 2, 1));
            var proposedRoles = S(new Relation(squareMachine.WidthRole, machine.WidthRole),
                new Relation(squareMachine.HeightRole, machine.HeightRole), new Relation(squareMachine.NotchRole, machine.NotchRole));
            results.Add(Planar(id, name, "authored structural neighbor", description,
                "Proposed width, height and notch role correspondences link the candidate to the source program.",
                "This is an authored candidate association, not an executed edit or a claim of shared site identity. Adding or removing edges needs a structural edit law.", execution.Sections,
                S(new Relation(new Relation(squareMachine.Surface, machine.Surface), proposedRoles), execution.Surface)));
        }

        var circle = MakeCircle(out var whole, out var contour);
        if (!ContourConstruction.TryReadCircle(circle, out var circleRead)) throw new InvalidOperationException("Circle promise failed.");
        foreach (var count in new[] { 4, 5, 6, 8 })
        {
            var fine = new Carrier(UnitShape.SubdivisionOf(contour.UnitShape, count));
            var partition = G(circle, new Relation(Relation.Segment(fine, 0, count), whole));
            if (!ContourConstruction.TryReadPartition(partition, out var partitionRead)) throw new InvalidOperationException("Contour partition failed.");
            var markers = new List<IElement>(); var locations = new List<IElement>();
            for (var i = 0; i < count; i++)
            {
                var request = G(partition, Relation.Point(fine, i));
                if (!ContourConstruction.TryLocate(request, out var located)) throw new InvalidOperationException("Contour marker failed.");
                located.TryReadOrderedGroup(out var observation);
                ((Relation)observation[2]).TryReadOrderedGroup(out var locatedFields);
                markers.Add(locatedFields[0]); locations.Add(located);
            }
            var edges = S(markers.Select((marker, i) => (IElement)new Relation(marker, markers[(i + 1) % count])).ToArray());
            results.Add(new("radial-" + count, count + " equal contour partitions, joined", "replace boundary construction",
                "Subdivide one native circular whole, locate its marks, and join successive marks; return to the first mark.",
                "Shared contour and radial reach; ordered subdivision; explicit adjacency and closing seam.",
                "The regular picture illustrates the declared contour contract. No screen coordinates prove side lengths; the chord-to-planar metric reader remains open.",
                [], count, false, null, null, 1, count)
            { NativeSurface = S(circle, partition, partitionRead, S(locations.ToArray()), edges), NativeCircle = circle, NativePartition = partition });
        }
        results.Add(new("circle", "Whole circular contour", "replace boundary construction",
            "Select the circular whole itself rather than a list of chords between selected marks.",
            "Point-derived common offset, radial attachment, whole traversal and closing site.",
            "A native contour is present; this does not claim that dropping a polygon's side count generates a circle.",
            [], 0, true, null, null, 1, null)
        { NativeSurface = S(circle, circleRead), NativeCircle = circle });
        return new(results.ToArray(),
        [
            "A weaker equality obligation broadens a range; a different view need not broaden it.",
            "Stretch and diagonal reorientation preserve different constraints when their order is exchanged.",
            "Four equal contour partitions give the square candidate. A rectangle can use unequal alternating angular gaps on the same circle; an ellipse is not required.",
            "A five-sided boundary requires another marker and edge plus a different turn partition. It is not merely a four-side binding made unknown.",
            "The whole circle is an intrinsic construction, not an increasingly dense polygon.",
            "Coordinates are a selected rendering/read basis. Copying a candidate does not transfer material or alter the source."
        ]);

        CentrelineAction.ApplicationRead Apply(Relation action, Relation source)
        {
            var application = CentrelineAction.Apply(f, action, source);
            if (!CentrelineAction.TryReadApplication(f, application, out var read) || !read.Complete)
                throw new InvalidOperationException("Neighborhood substitution did not settle.");
            return read;
        }
        Neighbor Planar(string id, string name, string kind, string change, string retains, string limits, Relation sections, Relation provenance)
        {
            var native = S(f.Surface, CurveShapeQualities.SimilarityBasis(f), sections, provenance);
            var qualities = ReadGeometry(native);
            var topology = ConstructionExchangeStudy.Topology(f, sections);
            var lines = E(sections).Cast<Relation>().Select(section => f.Read(section).Controls
                .Select(p => new[] { checked((long)p.X), checked((long)p.Y) }).ToArray()).ToArray();
            return new(id, name, kind, change, retains, limits, lines, 0, false,
                qualities.EqualSectionLengths, qualities.PerpendicularJoins, topology.Components, lines.Length)
            { NativeSurface = S(f.Surface, E(native)[1], sections, S(provenance, qualities.Evidence, topology.Witness)) };
        }
    }

    /// <summary>Derived finite metric reads, conditional on the explicitly
    /// retained perpendicular/equal-axis basis. Length equality is per selected
    /// section, not per maximal geometric side; perpendicular joins do not claim
    /// convexity or distinguish a 90-degree interior from a 270-degree interior.
    /// Open paths, conflicting site positions and separate supports are not
    /// silently promoted to a consistently positioned closed polygon.</summary>
    public static GeometryRead ReadGeometry(Relation surface)
    {
        if (!Closed(surface, out var fields) || fields.Length != 4 || fields[0] is not Relation frameSurface ||
            !Curve.Frame.TryFromSurface(frameSurface, out var f) || fields[1] is not Relation basis ||
            fields[2] is not Relation scope || !Closed(scope, out var items) || items.Any(item => item is not Relation))
            return new(null, null, surface);
        var sections = items.Cast<Relation>().Select(f.Read).ToArray();
        var evidence = S(basis, scope, S(sections.Select(s => (IElement)s.ControlWords).ToArray()));
        if (!CurveShapeQualities.IsSimilarityBasis(f, basis) || sections.Length < 3 ||
            sections.Any(s => !s.Complete || s.Controls.Length != 2)) return new(null, null, evidence);
        var sites = new Dictionary<IElement, (Curve.Vector Position, Relation Word)>(ReferenceEqualityComparer.Instance);
        var conflicts = new List<IElement>();
        foreach (var section in sections)
        {
            var words = E(section.ControlWords).Cast<Relation>().ToArray();
            Check(section.Start, section.Controls[0], words[0]);
            Check(section.End, section.Controls[^1], words[^1]);
        }
        if (conflicts.Count != 0) return new(null, null, S(evidence, S(conflicts.ToArray())));
        for (var i = 0; i < sections.Length; i++)
            if (!ReferenceEquals(sections[i].End, sections[(i + 1) % sections.Length].Start)) return new(null, null, evidence);
        var vectors = sections.Select(s => s.Controls[1] - s.Controls[0]).ToArray();
        if (vectors.Any(v => v.Zero)) return new(null, null, evidence);
        var first = Curve.Vector.Dot(vectors[0], vectors[0]);
        return new(vectors.All(v => Curve.Vector.Dot(v, v) == first),
            vectors.Select((v, i) => Curve.Vector.Dot(v, vectors[(i + 1) % vectors.Length])).All(v => v.IsZero), evidence);

        void Check(IElement site, Curve.Vector position, Relation word)
        {
            if (!sites.TryGetValue(site, out var previous)) sites.Add(site, (position, word));
            else if (previous.Position != position)
                conflicts.Add(new Relation(new Relation(site, previous.Word), new Relation(site, word)));
        }
    }

    public static Relation MakeCircle(out Relation whole, out Carrier contour)
    {
        var origin = Relation.Point(new Carrier(), 0);
        contour = new Carrier(UnitShape.CircularFrom(origin));
        var ray = new Carrier(UnitShape.LinearAt(origin));
        var radialStart = Relation.Point(ray, 0); var radialEnd = Relation.Point(ray, 1);
        var radius = new Relation(radialStart, radialEnd);
        var offset = new Relation(Relation.OrthogonalOutlet(origin), new Relation(radius, contour));
        var start = Relation.Point(contour, 0); var end = Relation.Point(contour, 1);
        whole = new Relation(start, end);
        return G(offset, G(new Relation(origin, radialStart), radius, new Relation(radialEnd, start)), whole, new Relation(end, start));
    }
    private static Relation G(params IElement[] entries) => Relation.OrderedGroup(new Carrier(), entries);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
