using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// Authored, role-bound pen instructions for the coupled-extent experiment.
/// A phase contains ordered direction/extent-role uses. A publication binds each
/// role to a finite chain of the frame's shared, uncalibrated reach. The generic
/// reader expands those actual uses and retains reduction and drawing evidence.
/// It has no shape-name dispatch and performs no settlement or interpolation
/// between target pictures. Each realization is an observation of supplied
/// extent publications, not a reconstruction of the controller's private state.
/// </summary>
public static class CoupledShapeTemplate
{
    // template = frame -> (Closed(extent roles) -> Closed(phase scopes))
    // phase scope = Closed(direction -> extent role, ...)
    // publication = Closed(extent role -> Closed(shared reach, ...), ...)
    // expansion = (actual phase -> actual publication) ->
    //             (Closed(consumed role bindings) -> expanded move word)
    // step = (expansion -> before position) -> (sum witness -> actual section)
    // execution = (template -> publication) ->
    //             Closed(ordered steps, ordered sections, position ports)
    // realization = (template -> Closed(upper publication, lower publication))
    //               -> Closed(upper execution, lower execution, reflection,
    //                         lower application, start comparison, end comparison)
    // comparison = (port A -> port B) -> displacement reduction witness.
    // Coincidence never inserts a site alias or a joining relation.

    public sealed record Template(Curve.Frame Frame, IElement WidthRole,
        IElement HeightRole, Relation Surface);

    public sealed record RouteRead(Relation Sections, Relation Execution,
        Relation Publication, Relation Positions, int[][] Points,
        IElement Start, IElement End, Relation StartPort, Relation EndPort,
        int[] SiteIds, int ZeroSections);

    public sealed record Realization(Template Template, RouteRead Upper,
        RouteRead Lower, Relation Reflection, Relation LowerApplication,
        Relation StartComparison, Relation EndComparison,
        bool StartCoincident, bool EndCoincident, Relation PublicationCorrespondence, Relation Surface);

    public static Template ChevronCap()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var width = new Placeholder(); var height = new Placeholder();
        return Create(frame, width, height,
            S(Use(frame.Motion.Right, height), Use(frame.Motion.Up, height)),
            S(Use(frame.Motion.Right, width)),
            S(Use(frame.Motion.Right, height), Use(frame.Motion.Down, height)));
    }

    public static Template CardinalCap()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var width = new Placeholder(); var height = new Placeholder();
        return Create(frame, width, height,
            S(Use(frame.Motion.Up, height)), S(Use(frame.Motion.Right, width)),
            S(Use(frame.Motion.Down, height)));
    }

    /// <summary>A seven-phase cap with one inset. This is an authored transfer
    /// probe, not a recovered historical ornament or a universal hook grammar.</summary>
    public static Template InsetCap()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var width = new Placeholder(); var height = new Placeholder();
        return Create(frame, width, height,
            S(Use(frame.Motion.Up, height), Use(frame.Motion.Up, height), Use(frame.Motion.Up, height)),
            S(Use(frame.Motion.Right, width), Use(frame.Motion.Right, height), Use(frame.Motion.Right, height)),
            S(Use(frame.Motion.Down, height)), S(Use(frame.Motion.Left, height)),
            S(Use(frame.Motion.Down, height)), S(Use(frame.Motion.Right, height)),
            S(Use(frame.Motion.Down, height)));
    }

    private static Template Create(Curve.Frame frame, IElement width, IElement height,
        params Relation[] phases) => new(frame, width, height,
            new Relation(frame.Surface, new Relation(S(width, height), S(phases))));

    /// <summary>The integer counts are boundary input: a count expands to that
    /// many references to one actual shared reach. Zero is a known empty extent,
    /// not an absent phase or an unbound input. No absolute length is introduced.</summary>
    public static Relation Publish(Template template, int width, int height)
    {
        if (width is < 0 or > 64 || height is < 0 or > 64)
            throw new ArgumentOutOfRangeException(nameof(width), "This finite study admits zero through 64 shared reaches per extent.");
        return S(new Relation(template.WidthRole, S(Enumerable.Repeat(template.Frame.Reach, width).ToArray())),
            new Relation(template.HeightRole, S(Enumerable.Repeat(template.Frame.Reach, height).ToArray())));
    }

    public static Realization Realize(Template template, int upperWidth, int lowerWidth,
        int upperHeight = 2, int lowerHeight = 2, Relation? publicationEvidence = null) =>
        Realize(template, Publish(template, upperWidth, upperHeight), Publish(template, lowerWidth, lowerHeight), publicationEvidence);

    public static Realization Realize(Template template, Relation upperPublication, Relation lowerPublication,
        Relation? publicationEvidence = null)
    {
        var upper = Execute(template, upperPublication);
        var lowerSource = Execute(template, lowerPublication);
        var correspondence = PublicationCorrespondence(template, upperPublication, lowerPublication, publicationEvidence);
        var reflection = CentrelineAction.FromBits(template.Frame, 4, template.Frame.Word(0, 0));
        var application = CentrelineAction.Apply(template.Frame, reflection, lowerSource.Sections);
        if (!CentrelineAction.TryReadApplication(template.Frame, application, out var applied) || !applied.Complete)
            throw new InvalidOperationException("The declared vertical-frame action did not read.");
        var lower = ReadRoute(template.Frame, applied.Output, lowerSource.Execution, lowerPublication);
        var start = Compare(template.Frame, upper.StartPort, lower.StartPort, out var starts);
        var end = Compare(template.Frame, upper.EndPort, lower.EndPort, out var ends);
        var source = new Relation(template.Surface, S(upperPublication, lowerPublication));
        var surface = new Relation(source, S(upper.Execution, lowerSource.Execution,
            reflection, application, start, end, correspondence));
        return new(template, upper, lower, reflection, application, start, end, starts, ends, correspondence, surface);
    }

    /// <summary>Explicitly requests start-to-start and end-to-end correspondence
    /// in a derived connected graph. Formal equality admits each declared seam
    /// separately. This does not mutate the independent source paths, weld
    /// material, identify other coincident sites, or create a new flow law.</summary>
    public static QualitativeGraphJoin DeclareSeams(Realization realization, IElement? cause = null)
    {
        var frame = realization.Template.Frame;
        var routes = new[] { realization.Upper, realization.Lower };
        var bindings = routes.SelectMany(route => E(route.Positions)).Cast<Relation>().ToArray();
        var sites = bindings.Select(binding => binding.Left).ToArray();
        var strokes = routes.SelectMany(route => E(route.Sections)).Cast<Relation>().Select(section =>
        {
            var read = frame.Read(section);
            if (!read.Complete) throw new ArgumentException("An unreadable section cannot supply a declared seam.", nameof(realization));
            var words = E(read.ControlWords);
            var difference = Difference(frame, (Relation)words[0], (Relation)words[^1]);
            if (!frame.Motion.TryReadResult(difference, out var chord, out _, out var unresolved) || E(unresolved).Length != 0)
                throw new ArgumentException("An unresolved chord cannot supply this exact seam read.", nameof(realization));
            return (IElement)QualitativeStrokeGraph.Stroke(section, read.Start, read.End, chord);
        }).ToArray();
        var graph = new QualitativeStrokeGraph(frame.Motion, S(sites), S(strokes));
        var declarationCause = cause ?? realization.Surface;
        var declarations = S(
            new Relation(new Relation(realization.Upper.Start, realization.Lower.Start), declarationCause),
            new Relation(new Relation(realization.Upper.End, realization.Lower.End), declarationCause));
        return new QualitativeGraphJoin(graph, S(bindings), declarations);
    }

    /// <summary>The optional four public samples are ordered upper width, lower
    /// width, upper height, lower height. Their stable shared tick promise is
    /// explicitly interpreted as one template reach. This is a selected local
    /// calibration, not an assertion that arbitrary numeric ticks are lengths.
    /// Samples must be captured coordinates rather than live value slots.</summary>
    private static Relation PublicationCorrespondence(Template template, Relation upper,
        Relation lower, Relation? evidence)
    {
        if (evidence is null) return S();
        if (!Closed(evidence, out var samples) || samples.Length != 4 || samples.Any(item => item is not CarrierLocation))
            throw new ArgumentException("Expected four captured public extent samples.", nameof(evidence));
        var locations = samples.Cast<CarrierLocation>().ToArray();
        var unit = locations[0].Carrier.UnitShape;
        if (locations.Any(at => at.Carrier.IsUnknown || !ReferenceEquals(at.Carrier.UnitShape, unit) ||
            at.SameTickRefAs(at.Carrier.ValueSlot)))
            throw new ArgumentException("Public samples must be captured and share one stable tick promise.", nameof(evidence));
        var upperRows = E(upper).Cast<Relation>().ToArray(); var lowerRows = E(lower).Cast<Relation>().ToArray();
        Relation Row(Relation[] rows, IElement role) => rows.Single(row => ReferenceEquals(row.Left, role));
        var rows = new[] { Row(upperRows, template.WidthRole), Row(lowerRows, template.WidthRole),
            Row(upperRows, template.HeightRole), Row(lowerRows, template.HeightRole) };
        for (var i = 0; i < rows.Length; i++)
            if (locations[i].Ticks != E((Relation)rows[i].Right).Length)
                throw new ArgumentException("A public sample disagrees with its bound reach chain.", nameof(evidence));
        var calibration = new Relation(unit.Surface, S(template.Frame.Reach));
        return new Relation(new Relation(evidence, calibration),
            S(samples.Select((sample, i) => (IElement)new Relation(sample, rows[i])).ToArray()));
    }

    /// <summary>Exact finite reader: missing or foreign role publications are
    /// refused; the caller must keep that observation unresolved. In particular,
    /// an absent binding is never completed as a zero-length phase.</summary>
    public static bool CanRealize(Template template, Relation publication) =>
        TryInstructions(template, out var roles, out var phases) &&
        TryPublication(template.Frame, roles, publication, out var _) &&
        phases.Length != 0;

    private static RouteRead Execute(Template template, Relation publication)
    {
        if (!TryInstructions(template, out var roles, out var phases) ||
            !TryPublication(template.Frame, roles, publication, out var bindings))
            throw new ArgumentException("The native template or publication is incomplete or incompatible.", nameof(publication));
        var frame = template.Frame; var steps = new List<IElement>(); var sections = new List<IElement>();
        Relation position = frame.Word(0, 0); IElement site = new Placeholder();
        foreach (var phase in phases)
        {
            var consumed = new List<IElement>(); var moves = new List<IElement>();
            foreach (var use in E(phase).Cast<Relation>())
            {
                var binding = bindings.Single(row => ReferenceEquals(row.Left, use.Right));
                consumed.Add(binding);
                foreach (var reach in E((Relation)binding.Right))
                    moves.Add(QualitativeMotionFrame.Move(use.Left, reach));
            }
            var word = S(moves.ToArray());
            var expansion = new Relation(new Relation(phase, publication), new Relation(S(consumed.ToArray()), word));
            var sum = frame.Motion.Reduce(position, word);
            if (!frame.Motion.TryReadResult(sum, out var nextPosition, out _, out var pending) || E(pending).Length != 0)
                throw new InvalidOperationException("The shared-reach displacement did not reduce completely.");
            IElement nextSite = new Placeholder();
            var section = frame.Line(site, nextSite, position, nextPosition);
            steps.Add(new Relation(new Relation(expansion, position), new Relation(sum, section)));
            sections.Add(section); site = nextSite; position = nextPosition;
        }
        var scope = S(sections.ToArray());
        var provisional = ReadRoute(frame, scope, S(), publication);
        var execution = new Relation(new Relation(template.Surface, publication),
            S(S(steps.ToArray()), scope, provisional.Positions));
        return provisional with { Execution = execution };
    }

    private static RouteRead ReadRoute(Curve.Frame frame, Relation sections, Relation execution, Relation publication)
    {
        var reads = E(sections).Cast<Relation>().Select(frame.Read).ToArray();
        if (reads.Length == 0 || reads.Any(read => !read.Complete))
            throw new InvalidOperationException("The complete route lost a readable section.");
        var ports = new List<IElement>(); var points = new List<int[]>(); var sites = new List<IElement>();
        for (var i = 0; i < reads.Length; i++)
        {
            var read = reads[i]; var controls = E(read.ControlWords);
            if (i == 0)
            {
                ports.Add(new Relation(read.Start, controls[0])); sites.Add(read.Start);
                points.Add([checked((int)read.Controls[0].X), checked((int)read.Controls[0].Y)]);
            }
            else if (!ReferenceEquals(reads[i - 1].End, read.Start))
                throw new InvalidOperationException("The route lost its actual shared handoff.");
            ports.Add(new Relation(read.End, controls[^1])); sites.Add(read.End);
            points.Add([checked((int)read.Controls[^1].X), checked((int)read.Controls[^1].Y)]);
        }
        var identities = new List<IElement>();
        var ids = sites.Select(site =>
        {
            var index = identities.FindIndex(other => ReferenceEquals(site, other));
            if (index >= 0) return index;
            identities.Add(site); return identities.Count - 1;
        }).ToArray();
        return new(sections, execution, publication, S(ports.ToArray()), points.ToArray(), reads[0].Start,
            reads[^1].End, (Relation)ports[0], (Relation)ports[^1], ids,
            reads.Count(read => read.Controls[0] == read.Controls[^1]));
    }

    private static Relation Compare(Curve.Frame frame, Relation first, Relation second, out bool coincident)
    {
        var difference = Difference(frame, (Relation)first.Right, (Relation)second.Right);
        coincident = frame.Motion.TryReadResult(difference, out var remaining, out _, out var unresolved) &&
            E(remaining).Length == 0 && E(unresolved).Length == 0;
        return new(new Relation(first, second), difference);
    }

    private static Relation Difference(Curve.Frame frame, Relation from, Relation to)
    {
        var inverse = S(E(from).Cast<Relation>().Select(move =>
        {
            frame.Motion.Direction(move, out var axis, out var forward, out var reach);
            return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis, frame.Motion.Horizontal)
                ? forward ? frame.Motion.Left : frame.Motion.Right
                : forward ? frame.Motion.Down : frame.Motion.Up, reach);
        }).ToArray());
        return frame.Motion.Reduce(inverse, to);
    }

    private static bool TryInstructions(Template template, out IElement[] roles, out Relation[] phases)
    {
        roles = []; phases = [];
        if (template.Surface is not { Left: var basis, Right: Relation { Left: Relation roleScope, Right: Relation phaseScope } } ||
            !ReferenceEquals(basis, template.Frame.Surface) || !Closed(roleScope, out roles) ||
            roles.Length == 0 || roles.Distinct(ReferenceEqualityComparer.Instance).Count() != roles.Length ||
            !Closed(phaseScope, out var items) || items.Length == 0 || items.Any(item => item is not Relation)) return false;
        var selectedRoles = roles;
        foreach (var item in items.Cast<Relation>())
            if (!Closed(item, out var uses) || uses.Length == 0 || uses.Any(use => use is not Relation row ||
                !selectedRoles.Any(role => ReferenceEquals(role, row.Right)) ||
                !template.Frame.Motion.Direction(QualitativeMotionFrame.Move(row.Left, template.Frame.Reach), out _, out _, out _))) return false;
        phases = items.Cast<Relation>().ToArray(); return true;
    }

    private static bool TryPublication(Curve.Frame frame, IElement[] roles, Relation publication, out Relation[] bindings)
    {
        bindings = [];
        if (!Closed(publication, out var rows) || rows.Length != roles.Length || rows.Any(row => row is not Relation)) return false;
        bindings = rows.Cast<Relation>().ToArray();
        foreach (var role in roles)
        {
            var matches = bindings.Where(row => ReferenceEquals(row.Left, role)).ToArray();
            if (matches.Length != 1 || matches[0].Right is not Relation chain || !Closed(chain, out var reaches) ||
                reaches.Length > 64 || reaches.Any(reach => !ReferenceEquals(reach, frame.Reach))) return false;
        }
        return true;
    }

    private static Relation Use(IElement direction, IElement role) => new(direction, role);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] items) =>
        StructuralConstraint.TryReadScope(scope, out items, out var seam, out var tail) && seam.Equals(tail);
}
