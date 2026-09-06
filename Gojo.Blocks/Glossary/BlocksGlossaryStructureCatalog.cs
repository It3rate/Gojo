using Gojo.ConceptSurface;
using Gojo.Core;
using GojoCAD;
using GojoCAD.Structures;
using SkiaSharp;

namespace Gojo.Blocks;

internal static class BlocksGlossaryStructureCatalog
{
    public static IReadOnlyDictionary<string, BlocksGlossaryStructure> Create()
    {
        var structures = new Dictionary<string, BlocksGlossaryStructure>();
        AddResolutionTension(structures);
        AddSiteFlowBoundary(structures);
        AddActionsRepeat(structures);
        AddUnitsTicks(structures);
        AddBasisFrames(structures);
        AddSourceSupport(structures);
        AddGradesDimensions(structures);
        AddDirectionArea(structures);
        AddFaceNormalSupport(structures);
        AddCurves(structures);
        AddBranches(structures);
        AddBooleanParticipation(structures);
        AddConnectedPaths(structures);
        AddPathReadBases(structures);
        AddDerivedPathStructures(structures);
        AddBooleanOffsetReads(structures);
        AddCrystallizationProvenance(structures);
        AddDroneClipOffsetLadder(structures);
        AddRadialCrescentLadder(structures);
        AddRadialCrescentOutlineExperiment(structures);
        AddDiamondCuspRounding(structures);
        AddConstraintsSettlement(structures);
        AddTopologyParticipation(structures);
        AddRepetitionTimeline(structures);
        AddPcbDataAttachment(structures);
        return structures;
    }

    private static void AddResolutionTension(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "resolution-tension",
        [
            ("resolution-read", PartitionedSegment(), "S(A,B) read at coarse and fine resolution", "Resolution belongs to the read. A low-resolution read is still a valid read, not a failed value."),
            ("unspecified-site", Group(Site(), TensionSlot()), "P(?) + Tension(location)", "Illustrative backing: a concrete site beside an open slot. A constraint connecting that slot to the site's unknown coordinate remains to be constructed."),
            ("closure-tension", Group(SharedPolyline(), TensionSlot()), "S(A,B)+S(B,C)+S(C,A') + close(A',A) -> Tension(delta)", "The topology can ask for closure even while geometry still misses."),
            ("failed-read", Group(LineSegment(), TensionSlot()), "Read(S(A,B), requested basis) -> residue", "Illustrative line and open slot; the complete selected-source, basis, result and failed-read witness are not yet connected here."),
            ("residue-witness", Group(LineSegment(), TensionSlot()), "Read(source, basis) = result + residue", "Illustrative line and open slot; a full read witness must connect its source, basis, result and remainder separately from unsettled material tension."),
            ("settled-read", SharedPolyline(), "source + basis + resolution -> settled read", "This backing is a shared polyline. The caption describes intended settlement evidence; this specimen does not record convergence.")
        ]);

    private static void AddSiteFlowBoundary(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "site-flow-boundary",
        [
            ("site", Site(), "P(O)", null),
            ("radial-flow", RadialFlow(), "P(O) + orthogonal-outlet(P(O))", "With one site there is no preferred direction, so the normal reads as radial."),
            ("linear-potential", LinearFlow(), "P(A@x)+P(B@x) -> unresolved carrier potential + orthogonal-outlet(potential)", "Two points can make a linear potential even before they have visible length."),
            ("measured-boundary", CircleBoundary(includeRadius: true), "P(O)+radius-evidence(R) -> Circle(O,R)", null),
            ("open-exterior", LineSegment(), "P(A)+P(B) -> S(A,B) + exterior(open)", "The segment bounds a span without closing all surrounding support."),
            ("boundary-support", CircleArea(inverse: false), "Circle(O,R) -> Area(Circle)", "A boundary can be read as support when the inside or outside participates.")
        ]);

    private static void AddActionsRepeat(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "actions-repeat",
        [
            ("addition", SharedPolyline(), "S(A,B) + S(B,C) -> accumulated span", "Addition is shown as attaching compatible spans in sequence."),
            ("stretch", DirectedLine(inverse: false), "S(A,B) + action_stretch(3) -> S(A,B')", "Stretch reads the same segment relation at a fixed ratio. The source span remains inspectable while the multiplied read grows from it."),
            ("repeat", PartitionedSegment(), "Repeat(action, n) over S(A,B)", "The partition illustrates repetition. An executable repeat additionally needs its selected source, transition, controller and feedback evidence."),
            ("multiply", RectangleArea(inverse: false), "S(A,B) + stretch(k); also Repeat(add unit, n)", "Gojo multiplication includes both stretch and accumulated repeat, with stretch as the direct multiplicative read."),
            ("stretch-repeat", Group(DirectedLine(inverse: false), PartitionedSegment()), "Repeat(action_stretch(k), n) -> S0,S1,S2...", "The backing combines line and partition geometry. The intended repeated stretch still needs an explicit controller, feedback and transition history."),
            ("compare", PartitionedSegment(), "Read(target by unit U) -> quotient + residue", "Unit comparison asks how many repeated unit reads fit in a target.")
        ]);

    private static void AddUnitsTicks(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "units-ticks",
        [
            ("internal-partition", PartitionedSegment(), "S(A,B) + partition(n) -> P(t1..tn)", null),
            ("unit-comparison", Group(LineSegment(), PartitionedSegment()), "S(target) read by Unit(S(u0,u1))", "External comparison produces count and residue against a reusable unit."),
            ("tick-reference", PartitionedSegment(), "TickRef(S(A,B), t5)", "A tick reference names a readable location without copying the point."),
            ("segment-ruler", PartitionedSegment(), "P(A)+P(B) -> S(A,B) + partition(n)", "The ruler comes from a bounded segment, then adds repeated readable ticks."),
            ("circular-ruler", PartitionedCircle(), "P(O)+orthogonal-outlet(P(O))+radius-evidence(R) -> Circle(O,R)+partition(theta_i)", "A circular unit starts from a point orthogonal outlet, gains a circumference boundary, then partitions that circumference."),
            ("unit-shape", Group(PartitionedSegment(), PartitionedCircle(), RectangleArea(inverse: false)), "UnitShape(line | circle | area | curve read)", "A unit shape is stable structure that other reads can compare against; it can be linear, circular, area-like, curved, composite, or otherwise crystallized.")
        ]);

    private static void AddBasisFrames(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "basis-frames",
        [
            ("xy-frame", LineSegment(), "Frame(X,Y) + P(x,y) + S(P0,P1)", "XY is one basis, not the only way to make a carrier."),
            ("local-carrier", LineSegment(), "Basis(S(A,B)) = tangent + normal", "A carrier can carry its own local basis for later reads."),
            ("circular-basis", CircleBoundary(includeRadius: true), "Basis(Circle(O,R)) + P(theta)", "The circular basis inherits the radial origin and circumference provenance, then uses boundary partitions as angle evidence."),
            ("orthogonal-basis", LinearFlow(), "S(A,B) + normal support -> P(normal distance)", null),
            ("rotated-frame", Group(LineSegment(), DirectedLine(inverse: false)), "Frame' = rotate(Frame, theta)", "A frame transform should remain an inspectable relation."),
            ("read-surface", RectangleArea(inverse: false), "ReadSurface(source, basis, resolution)", "The surface is the context through which structure is interpreted.")
        ]);

    private static void AddSourceSupport(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[]
        {
            ("radial-positive", GlossaryRow.RadialPositive),
            ("radial-inverse", GlossaryRow.RadialInverse),
            ("linear", GlossaryRow.Linear)
        };
        var columns = new[] { "site", "flow", "boundary", "partitioned", "orientation", "direction" };
        foreach (var (rowKey, row) in rows)
            foreach (var column in columns)
            {
                structures[$"source-support-{rowKey}-{column}"] = SourceSupport(row, column);
            }
    }

    private static void AddDirectionArea(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[]
        {
            ("radial-positive", GlossaryRow.RadialPositive),
            ("radial-inverse", GlossaryRow.RadialInverse),
            ("linear", GlossaryRow.Linear)
        };
        var columns = new[]
        {
            ("directed-boundary", "directed-boundary"),
            ("boundary-support", "boundary-support"),
            ("directed-support", "directed-support"),
            ("area", "area"),
            ("directed-area", "directed-area"),
            ("bounded-area", "bounded-area")
        };
        foreach (var (rowKey, row) in rows)
            foreach (var (columnKey, column) in columns)
            {
                structures[$"direction-area-{rowKey}-{columnKey}"] = DirectionArea(row, column);
            }
    }

    private static void AddGradesDimensions(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "grades-dimensions",
        [
            ("site", Site(), "grade 0: P(O)", null),
            ("span", LineSegment(), "grade 1: S(A,B)", null),
            ("area", CircleArea(inverse: false), "grade 2: Area(boundary)", null),
            ("volume-hint", RectangleArea(inverse: false), "grade 3: Volume(surface shell)", "A 2D rectangle-area teaching placeholder. A connected volume shell and its 3D laws are not encoded by this specimen."),
            ("segment-normal", LinearFlow(), "S(A,B) + orthogonal normal -> grade+1 support", "A 1D carrier's normal in 2D is perpendicular line support."),
            ("point-pair-normal", Group(LinearSites(), LinearFlow()), "P(A)+P(B) in 3D -> cylindrical normal; A=B -> plane normal", "This linear-site/flow backing illustrates a proposed 3D distinction. It does not yet encode or verify a general cylindrical/plane normal law.")
        ]);

    private static void AddFaceNormalSupport(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "face-normal",
        [
            ("point-radial", RadialFlow(), "normal(P) = radial support", null),
            ("segment-sides", LinearFlow(), "normal(S) = side+ + side-", "The sides are discovered from the carrier and orientation, not separate symbolic fields."),
            ("arc-normal", DirectedCircle(inverse: false), "normal(Arc) = radial side support along arc", null),
            ("area-face", DirectedCircleArea(inverse: false), "Area(boundary) + face spin+", null),
            ("inverse-face", DirectedCircleArea(inverse: true), "Area(boundary) + face spin-", null),
            ("face-relation", DirectedLinearArea(inverse: false), "Boundary + support side + face read", "A face relation connects a boundary to the support it divides.")
        ]);

    private static void AddCurves(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[]
        {
            ("line-line", CurveJoinKind.LineLine),
            ("line-arc", CurveJoinKind.LineArc),
            ("arc-arc", CurveJoinKind.ArcArc)
        };
        var columns = new[]
        {
            ("corner", CurveColumn.Corner),
            ("arc-blend", CurveColumn.ArcBlend),
            ("bezier", CurveColumn.Bezier),
            ("clothoid", CurveColumn.Clothoid),
            ("involute", CurveColumn.Involute),
            ("plotted", CurveColumn.Plotted)
        };
        foreach (var (rowKey, row) in rows)
            foreach (var (columnKey, column) in columns)
            {
                structures[$"curves-{rowKey}-{columnKey}"] = Curve(row, column);
            }
    }

    private static void AddBranches(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[] { "tip", "middle", "structure" };
        var columns = new[] { "branch", "merge", "orthogonal-joint", "other" };
        foreach (var row in rows)
            foreach (var column in columns)
            {
                structures[$"branches-{row}-{column}"] = Branch(row, column);
            }
    }

    private static void AddBooleanParticipation(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var rows = new[] { "point", "segment", "area" };
        var columns = new[] { "participants", "touch", "overlap", "union", "intersection", "difference" };
        foreach (var row in rows)
            foreach (var column in columns)
            {
                structures[$"boolean-participation-{row}-{column}"] = BooleanParticipation(row, column);
            }
    }

    private static void AddConnectedPaths(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var ids = new[]
        {
            "separate-sections",
            "shared-point",
            "bonded-point",
            "orthogonal-endpoint",
            "higher-bridge",
            "closure-tension"
        };
        foreach (var id in ids)
        {
            structures[$"connected-paths-{id}"] = ConnectedPath(id);
        }
    }

    private static void AddPathReadBases(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var ids = new[]
        {
            "xy",
            "circular",
            "orthogonal",
            "mixed",
            "composite-read",
            "residue-error"
        };
        foreach (var id in ids)
        {
            structures[$"path-basis-{id}"] = PathReadBasis(id);
        }
    }

    private static void AddDerivedPathStructures(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var ids = new[]
        {
            "open-offset",
            "curved-offset",
            "inset-outset",
            "branch-merge-fill",
            "outline-fill",
            "offset-residue"
        };
        foreach (var id in ids)
        {
            structures[$"derived-path-{id}"] = DerivedPathStructure(id);
        }
    }

    private static void AddBooleanOffsetReads(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var ids = new[]
        {
            "source-carriers",
            "offset-reads",
            "split-intersections",
            "union-hole",
            "intersection-read",
            "rounded-ladder"
        };
        foreach (var id in ids)
        {
            structures[$"boolean-offset-{id}"] = BooleanOffsetRead(id);
        }
    }

    private static void AddCrystallizationProvenance(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "crystallization-provenance",
        [
            ("source-carriers", SharedPolyline(), "source carriers: S/Arc/Curve sections", null),
            ("read-path", SharedPolyline(), "Read(source sections) -> PathRead", "The read creates one path relation over many source sections."),
            ("crystallized-element", SharedPolyline(), "Crystallize(PathRead) -> Element(Path)", "The new path can be used directly after crystallization."),
            ("provenance-links", Group(SharedPolyline(), TensionSlot()), "Element(Path) + provenance(source read)", "Provenance remains inspectable but does not have to be the active rendering source."),
            ("independent-read", SharedPolyline(), "Read(Element(Path), new basis)", "A crystallized element can be partitioned or styled independently of its source."),
            ("residue-retained", Group(SharedPolyline(), TensionSlot()), "Element(Path) + residue(source mismatch)", "Crystallization should not erase unresolved evidence.")
        ]);

    private static void AddDroneClipOffsetLadder(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var ids = new[]
        {
            "source",
            "path-read",
            "offset-reads",
            "intersections",
            "split-sections",
            "outside-round",
            "inside-round",
            "merged-loops",
            "negative-spaces",
            "settled-read"
        };
        foreach (var id in ids)
        {
            structures[$"drone-clip-{id}"] = DroneClipOffsetLadder(id);
        }
    }

    private static void AddRadialCrescentLadder(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var ids = new[]
        {
            "source-seeds",
            "endpoint-caps",
            "trimmed-carriers",
            "offset-boundaries",
            "fill-participants",
            "merged-areas"
        };
        foreach (var id in ids)
        {
            structures[$"radial-crescent-{id}"] = RadialCrescentLadder(id);
        }
    }

    private static void AddRadialCrescentOutlineExperiment(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        structures["radial-crescent-outline-experiment-full"] =
            RadialCrescentCircularBodyOutlineExperiment(new FrameBasis());
        structures["radial-crescent-outline-experiment-sites"] =
            RadialCrescentCircularBodyExteriorSitesExperiment(new FrameBasis());
        structures["radial-crescent-outline-experiment-walked"] =
            RadialCrescentCircularBodyWalkedOutlineExperiment(new FrameBasis());
        structures["radial-crescent-outline-experiment-exploded"] =
            RadialCrescentCircularBodyExplodedOutlineExperiment(new FrameBasis());
        structures["radial-crescent-outline-experiment-replacements"] =
            RadialCrescentCircularBodyReplacementAreasExperiment(new FrameBasis());
        structures["radial-crescent-outline-experiment-rounded"] =
            RadialCrescentCircularBodyRoundedOutlineExperiment(new FrameBasis());
    }

    private static void AddDiamondCuspRounding(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var ids = new[]
        {
            "source",
            "outer-selection",
            "outer-rounded",
            "inner-selection",
            "inner-rounded",
            "result"
        };
        foreach (var id in ids)
        {
            structures[$"diamond-cusp-rounding-{id}"] = DiamondCuspRounding(id);
        }
    }

    private static void AddConstraintsSettlement(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "constraints-settlement",
        [
            ("equal-length", Group(LineSegment(), LineSegment()), "S(A,B) == S(C,D)", "Equality is a relation that can settle or leave tension."),
            ("tangency", Curve(CurveJoinKind.LineArc, CurveColumn.ArcBlend).Structure, "tangent(carrier A, carrier B, J)", "Tangency is local evidence at a join, not a global curve type."),
            ("radius", CircleBoundary(includeRadius: true), "P(O)+P(R) + constraint distance(O,R)", null),
            ("closure", Group(SharedPolyline(), TensionSlot()), "closed topology + geometric miss -> tension", null),
            ("settled", SharedPolyline(), "constraints + source -> settled read", null),
            ("residue", Group(LineSegment(), TensionSlot()), "settlement remainder -> residue", null)
        ]);

    private static void AddTopologyParticipation(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "topology-participation",
        [
            ("crossing", Group(LineSegment(), DirectedLine(inverse: true)), "S(A,B) crosses S(C,D) without join", "A crossing is detectable proximity, not automatic participation."),
            ("over-under", Group(LineSegment(), TensionSlot()), "S(over) + relation_over(S(under))", "The relation records ordering without merging the carriers."),
            ("containment", CircleArea(inverse: false), "P(x) inside Area(boundary)", null),
            ("adjacency", Group(RectangleArea(inverse: false), TranslatedAreaFill(18, 0, inverse: true)), "Area(A) adjacent Area(B) through shared boundary", null),
            ("near-far", Group(Site(), TensionSlot()), "proximity(P(A), P(B), resolution)", "Near and far depend on the read basis and resolution."),
            ("bonded-distinct", Group(TwoPointBond(), TensionSlot()), "P(A) + P(B) + bond(A,B)", "Bonded endpoints can remain distinct while participating topologically.")
        ]);

    private static void AddRepetitionTimeline(Dictionary<string, BlocksGlossaryStructure> structures) =>
        AddSpecimens(structures, "repetition-timeline",
        [
            ("controller", SelectionEvidence(), "controller + selected source", null),
            ("transition", DirectedLine(inverse: false), "Repeat(transition, source, n)", null),
            ("feedback", Group(DirectedCircle(inverse: false), TensionSlot()), "transition result -> feedback -> next read", null),
            ("timeline", PartitionedSegment(), "P(t0)..P(tn) + state read at each tick", null),
            ("accumulated-residue", Group(PartitionedSegment(), TensionSlot()), "residue(t0)+residue(t1)+...", null),
            ("stable-pattern", PartitionedCircle(), "repeat until stable pattern", "A pattern is a settled repetition structure that can itself be read.")
        ]);

    private static void AddPcbDataAttachment(Dictionary<string, BlocksGlossaryStructure> structures)
    {
        var ids = new[]
        {
            "schematic-intent",
            "layout-carriers",
            "data-attachment",
            "render-read",
            "rule-check",
            "physical-read"
        };
        foreach (var id in ids)
        {
            structures[$"pcb-data-{id}"] = PcbDataAttachment(id);
        }
    }

    private static BlocksGlossaryStructure SourceSupport(GlossaryRow row, string column)
    {
        var inverse = row == GlossaryRow.RadialInverse;
        var linear = row == GlossaryRow.Linear;
        return column switch
        {
            "site" when linear => Spec(
                LinearSites(),
                "P(A) + P(B), with A and B allowed to coincide"),
            "site" => Spec(
                Site(),
                "P(O)"),
            "flow" when linear => Spec(
                LinearFlow(),
                "S(C,0,0) + orthogonal-outlet(S(C,0,0))",
                "This is intentionally a collapsed pair, not a drawn-length segment. The shared-reference roles expose linear potential before distance is chosen; the visible vertical escape is a renderer read of the segment's orthogonal outlet."),
            "flow" => Spec(
                RadialFlow(),
                "P(O) + orthogonal-outlet(P(O))",
                inverse
                    ? "Radial inverse flow is the point-grade version of negative orthogonal support: the flow is read as moving into the site from every side. A signed read can pass through the origin, just as opposite side support passes through a segment."
                    : "Radial positive flow is the point-grade version of positive orthogonal support: with only one site, the normal has no preferred linear side, so outward support appears in every direction."),
            "boundary" when linear => Spec(
                LineSegment(),
                "P(A) + P(B) -> S(A,B)"),
            "boundary" => Spec(
                CircleBoundary(includeRadius: true),
                "P(O) + radius-evidence(R) -> Circle(O,R)"),
            "partitioned" when linear => Spec(
                PartitionedSegment(),
                "S(A,B) + P(t1) + P(t2) + ..."),
            "partitioned" => Spec(
                PartitionedCircle(),
                "Circle(O,R) + P(theta1) + P(theta2) + ...",
                "The backing structure uses points on a circular carrier for the boundary ticks. The rendered diagram still chooses a friendly geometric circle for legibility, and the exact radius-to-contour partition witness is still provisional."),
            "orientation" when linear => Spec(
                DirectedLine(inverse),
                inverse ? "orientation: left is positive; D(S(C,+u,-u))" : "orientation: right is positive; D(S(C,-u,+u))",
                "The current encoding intends orientation through ordered carrier evidence: the directed span says which side of zero is positive. A crystallized orientation should preserve that evidence rather than add an unrelated tag."),
            "orientation" => Spec(
                DirectedCircle(inverse),
                inverse ? "orientation: clockwise is positive; D(Circle(O,R), +theta -> -theta)" : "orientation: counterclockwise is positive; D(Circle(O,R), -theta -> +theta)",
                "This uses a directed span on a circular carrier as orientation evidence. A later crystallized orientation can carry the same ordered-read component on the boundary."),
            "direction" when linear => Spec(
                DirectionOnLine(inverse),
                inverse ? "P(0) + P(-u), read under left-positive orientation" : "P(0) + P(+u), read under right-positive orientation"),
            "direction" => Spec(
                DirectionOnCircle(inverse),
                inverse ? "P(theta0) + P(-theta), read under clockwise orientation" : "P(theta0) + P(+theta), read under counterclockwise orientation"),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure DirectionArea(GlossaryRow row, string column)
    {
        var inverse = row == GlossaryRow.RadialInverse;
        var linear = row == GlossaryRow.Linear;
        return column switch
        {
            "directed-boundary" when linear => Spec(
                DirectedLine(inverse),
                inverse ? "D(S(C,+u,-u))" : "D(S(C,-u,+u))"),
            "directed-boundary" => Spec(
                DirectedCircle(inverse),
                inverse ? "D(Circle(O,R), +theta -> -theta)" : "D(Circle(O,R), -theta -> +theta)"),
            "boundary-support" when linear => Spec(
                LinearFlow(),
                "S(C,0,0) + orthogonal-outlet(S(C,0,0))",
                "The structural seed is still a collapsed segment. The visible band is the renderer's interpretation of support emerging from that linear potential."),
            "boundary-support" => Spec(
                CircleArea(inverse),
                inverse ? "Area*(Circle(O,R))" : "Area(Circle(O,R))"),
            "directed-support" when linear => Spec(
                DirectedLinearArea(inverse),
                "Area(S(A,B) + side support) + D(S(A,B))",
                "Linear side support is represented as filled boundary plus a directed edge. The side should remain locally discoverable from the carrier, its orthogonal support, and the chosen orientation."),
            "directed-support" => Spec(
                DirectedCircleArea(inverse),
                "Area(Circle(O,R)) + D(Circle edge)",
                "Directed radial support is encoded as fill plus directed boundary evidence. The inward/outward side should be read from orthogonal support through the site, not duplicated as an unrelated primitive."),
            "area" when linear => Spec(
                RectangleArea(inverse),
                inverse ? "Area*(closed S-boundary)" : "Area(closed S-boundary)"),
            "area" => Spec(
                CircleArea(inverse),
                inverse ? "Area*(Circle(O,R))" : "Area(Circle(O,R))"),
            "directed-area" when linear => Spec(
                DirectedLinearArea(inverse),
                "Area(closed S-boundary) + face D(edge...)",
                "Face circulation is expressed by pairing area fill with directed boundary evidence. When an area crystallizes, this same orientation/spin component should be available on the face."),
            "directed-area" => Spec(
                DirectedCircleArea(inverse),
                "Area(Circle(O,R)) + face D(Circle edge)",
                "Face spin is represented as an area plus directed boundary evidence. The face can later carry that spin independently, whether it was inherited from an aligned boundary or chosen during a read."),
            "bounded-area" when linear => Spec(
                BoundedRectangleArea(inverse),
                "Area(boundary) + S-boundary + P(tick...)",
                "Ticks are carried as points on boundary spans. Later this should become partition evidence that knows which side or face of the boundary it belongs to."),
            "bounded-area" => Spec(
                BoundedCircleArea(inverse),
                "Area(Circle(O,R)) + P(O) + S(O,R) + P(theta...)",
                "The area fill is closed-boundary structure, the radius is explicit, and the tick marks are circular-carrier points. A future encoding should settle these as one contour with partition witnesses instead of parallel evidence."),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure Curve(CurveJoinKind row, CurveColumn column)
    {
        var frame = new FrameBasis();
        var lineA = GraphicStructure.FrameSegment(frame.Point(-48, 12), frame.Point(-8, 12));
        var lineB = GraphicStructure.FrameSegment(frame.Point(8, -10), frame.Point(48, -10));
        var arcA = GraphicStructure.FrameArc(frame.Point(-18, 8), frame.Point(-48, 8), frame.Point(-10, -24));
        var arcB = GraphicStructure.FrameArc(frame.Point(22, -2), frame.Point(8, 22), frame.Point(50, 0));
        var incoming = row switch
        {
            CurveJoinKind.LineArc => lineA,
            CurveJoinKind.ArcArc => arcA,
            _ => lineA
        };
        var outgoing = row switch
        {
            CurveJoinKind.LineLine => lineB,
            CurveJoinKind.LineArc => arcB,
            _ => arcB
        };

        return column switch
        {
            CurveColumn.Corner => Spec(
                Group(incoming, outgoing, GraphicStructure.Pin(incoming, outgoing)),
                "carrier A + carrier B + shared P(J)"),
            CurveColumn.ArcBlend => Spec(
                Group(incoming, outgoing, GraphicStructure.FrameArc(frame.Point(0, 4), frame.Point(-8, 12), frame.Point(8, -10))),
                "carrier A + carrier B + Arc(P0,P1) as tangent bridge",
                "The current bridge is explicit arc evidence. The future read should derive the smoothing relation from carrier tangency and requested settlement."),
            CurveColumn.Bezier => Spec(
                Group(incoming, outgoing, GraphicStructure.FrameCubicCurve(frame.Point(-8, 12), frame.Point(0, 24), frame.Point(10, -22), frame.Point(26, -10))),
                "carrier A + carrier B + Bezier(P0,H0,H1,P1)"),
            CurveColumn.Clothoid => Spec(
                Group(incoming, outgoing, GraphicStructure.FrameClothoidTransition(GraphicStructure.FrameCurveTransition(incoming, outgoing), GraphicStructure.FrameSegment(frame.Point(-18, -34), frame.Point(28, -34)))),
                "carrier A -> carrier B with curvature ramp over S(L,0,n)",
                "The current clothoid surface is transition plus length/grade evidence; it does not yet solve a true curvature-continuity path from the carrier pair."),
            CurveColumn.Involute => Spec(
                Group(incoming, outgoing, GraphicStructure.FrameInvoluteCurve(arcA, GraphicStructure.FrameSegment(frame.Point(-6, -34), frame.Point(34, -18)))),
                "Arc(base) + unwound tangent S(T,0,n)",
                "Involute generation has a structural foothold, but the exact unwrap relationship still needs richer inspection vocabulary."),
            CurveColumn.Plotted => Spec(
                PlottedPath(frame),
                "P0 + P1 + P2 + ... with S(Pi,Pi+1) samples",
                "This is intentionally a coordinate-sampled contrast case. It is Gojo structure, but it is not a carrier-tangency join."),
            _ => Spec(Group(incoming, outgoing), "carrier A + carrier B")
        };
    }

    private static BlocksGlossaryStructure Branch(string row, string column)
    {
        var frame = new FrameBasis();
        return (row, column) switch
        {
            ("tip", "branch") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-46, 0), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(42, -24)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(42, 24))),
                "S(A,J) + S(J,B) + S(J,C)"),
            ("tip", "merge") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-42, -24), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(-42, 24), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(46, 0))),
                "S(A,J) + S(B,J) + S(J,C)"),
            ("tip", "orthogonal-joint") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-42, 0), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(0, -34)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(38, 26))),
                "S(A,J) + S(J,O) + S(J,C)"),
            ("tip", "other") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-44, 8), frame.Point(-8, 8)),
                    GraphicStructure.FrameArc(frame.Point(0, 2), frame.Point(-8, 8), frame.Point(38, -18))),
                "S(A,J) + Arc(J,B)"),
            ("middle", "branch") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-48, 0), frame.Point(48, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 0), frame.Point(0, -36))),
                "S(A,B) + S(M,C), where M lies on S(A,B)"),
            ("middle", "merge") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-48, 0), frame.Point(48, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, -34), frame.Point(0, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, 34), frame.Point(0, 0))),
                "S(A,B) + S(C,M) + S(D,M), where M lies on S(A,B)"),
            ("middle", "orthogonal-joint") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-48, 0), frame.Point(48, 0)),
                    GraphicStructure.FrameArc(frame.Point(-16, -18), frame.Point(-30, -36), frame.Point(0, 0))),
                "S(A,B) + Arc(C,M), where M lies on S(A,B)"),
            ("middle", "other") => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-48, 0), frame.Point(48, 0)),
                    GraphicStructure.FrameSegment(frame.Point(0, -34), frame.Point(0, 34))),
                "S(A,B) + S(C,D), with no shared P(J)",
                "The crossing is just two carriers in the same visual region. There is no Pin/Junction relation, so the structure does not settle into a join."),
            ("structure", "branch") => Spec(
                MultiBranch(frame),
                "S(J,A) + S(J,B) + S(J,C) + ..."),
            ("structure", "merge") => Spec(
                BranchMergeNetwork(frame),
                "S(A,J) + S(J,B) + S(J,C) + S(B,K) + S(C,K)"),
            ("structure", "orthogonal-joint") => Spec(
                CurvedAreaWithLooseBranch(frame, partial: false),
                "Area(S + S + Arc + S) + loose S(U,V)",
                "A curved closed area is expressible. The unrelated branch line is expressible as separate structure, but the tutorial still uses wording to emphasize non-participation."),
            ("structure", "other") => Spec(
                CurvedAreaWithLooseBranch(frame, partial: true),
                "partial Area(S + S + Arc + ?open) + loose S(U,V)",
                "A poorly bounded U-area is not yet a settled Gojo area. This example keeps the open side as tension-like residue while the renderer sketches the intended partial support."),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure BooleanParticipation(string row, string column) =>
        row switch
        {
            "point" => BooleanPoint(column),
            "segment" => BooleanSegment(column),
            "area" => BooleanArea(column),
            _ => Spec(Site(), "P(O)")
        };

    private static BlocksGlossaryStructure BooleanPoint(string column)
    {
        var structure = PointSettlement(column);
        return column switch
        {
            "participants" => Spec(
                structure,
                "selected{P(A), P(B)} + scope(P-lane)",
                "The points are selected participants. They do not become a boolean value until a scope and occupancy basis read them."),
            "touch" => Spec(
                structure,
                "P(A@x) + P(B@x), no shared-support read yet",
                "Equal position is only coordinate evidence. Shared support appears only when the participants are read through a common occupancy basis."),
            "overlap" => Spec(
                structure,
                "settle(selected{P(A), P(B)}, scope=P(x)) -> shared/tension support",
                "At point resolution, two occupied values on the same scoped site become shared support or tension depending on the lane read."),
            "union" => Spec(
                structure,
                "settle(...) -> support(P(A) + P(B))",
                "Union is the support read of all selected point participation inside the scope, not a primitive true/false operation."),
            "intersection" => Spec(
                structure,
                "settle(...) -> shared support at P(x)",
                "Intersection keeps only the support that both selected point participations occupy under the same basis."),
            "difference" => Spec(
                structure,
                "settle(...) -> exclusive(P(A)) + residue(P(B))",
                "Difference is read as exclusive support from A under the selected scope, with B left visible as excluded or residue evidence."),
            _ => Spec(structure, "selected{P(A), P(B)}")
        };
    }

    private static BlocksGlossaryStructure BooleanSegment(string column)
    {
        var structure = SegmentSettlement(column);
        return column switch
        {
            "participants" => Spec(
                structure,
                "selected{S(A0,A1), S(B0,B1)} + scope S(0,n)",
                "The selected spans are ordinary segment participation. The scope carrier and partition read determine whether any boolean-like support exists."),
            "touch" => Spec(
                structure,
                "S(A,0,m) + S(B,m,n)",
                "Touching endpoints share boundary evidence, but there is no occupied length overlap unless the read asks for endpoint support."),
            "overlap" => Spec(
                structure,
                "S(A,0,k) + S(B,j,n) -> partitions {0,j,k,n}",
                "The overlap is discovered by splitting the carrier at local participation changes. Nothing global classifies the spans first."),
            "union" => Spec(
                structure,
                "support(A or B) over partitioned scope",
                "Union is the support projection of all selected span participation inside the scope."),
            "intersection" => Spec(
                structure,
                "shared/tension support(A and B) over partitioned scope",
                "Intersection comes from cells where both selected spans occupy the same support."),
            "difference" => Spec(
                structure,
                "exclusive(A not B) + residue(B)",
                "Difference is exclusive support from one selected span after the overlapping participation has been accounted for."),
            _ => Spec(structure, "segment settlement")
        };
    }

    private static BlocksGlossaryStructure BooleanArea(string column)
    {
        var structure = AreaParticipation(column);
        return column switch
        {
            "participants" => Spec(
                structure,
                "selected{Area(A), Area(B)} + scope",
                "Areas enter the read as occupied support. The later operation is a settlement over those participants."),
            "touch" => Spec(
                structure,
                "Area(A) + Area(B) with boundary contact",
                "Boundary contact is evidence, but it is not automatically shared occupied area."),
            "overlap" => Spec(
                structure,
                "Area(A) + Area(B) -> partitioned support cells",
                "Overlap is read by partitioning the participating faces into occupied cells."),
            "union" => Spec(
                structure,
                "AreaUnion(AreaFrame(A,B), outline, settlement, residue)",
                "The existing GojoCAD area union surface already carries selected areas, result outline, settlement evidence, and residue."),
            "intersection" => Spec(
                structure,
                "shared occupied cells from Area(A) and Area(B)",
                "Area intersection is the shared occupied support read. This example keeps the shared patch explicit while richer area reads continue to evolve."),
            "difference" => Spec(
                structure,
                "Area(A) + AntiArea(B) -> exclusive support + residue",
                "Area difference is represented as positive participation opposed by anti participation, with any unresolved mismatch kept as residue."),
            _ => Spec(structure, "area settlement")
        };
    }

    private static IElement PointSettlement(string column)
    {
        var carrier = new Carrier();
        var sameSite = column is "touch" or "overlap" or "intersection";
        var a = GraphicStructure.Point(carrier, sameSite ? 0 : -24);
        var b = GraphicStructure.Point(carrier, sameSite ? 0 : 24);
        var scope = sameSite
            ? GraphicStructure.Point(carrier, 0)
            : GraphicStructure.Segment(carrier, -48, 48);
        var selected = Group(a, b);
        var basis = BoundaryFlowLaneBasis.Create(
            new Carrier(),
            BoundaryExposure.BoundedWhole,
            FlowExposure.None,
            scope);
        var result = column switch
        {
            "participants" => Group(),
            "touch" => Group(),
            "difference" => a,
            "intersection" => sameSite ? Group(a, b) : Group(),
            _ => selected
        };
        var tension = column is "overlap" or "intersection" ? selected : Group();
        var exclusive = column == "difference" ? a : Group();
        var uncovered = column is "participants" or "touch" ? scope : Group();
        var residue = column == "difference" ? b : Group();

        return ParticipationSettlementSurface.Create(
            new Carrier(),
            selected,
            scope,
            scope,
            basis,
            Group(scope),
            selected,
            result,
            tension,
            exclusive,
            uncovered,
            result,
            residue,
            selected);
    }

    private static IElement SegmentSettlement(string column)
    {
        var carrier = new Carrier();
        var scope = GraphicStructure.Segment(carrier, 0, 100);
        var (a0, a1, b0, b1) = column switch
        {
            "participants" => (12L, 36L, 64L, 88L),
            "touch" => (10L, 50L, 50L, 90L),
            _ => (12L, 64L, 44L, 90L)
        };
        var a = GraphicStructure.Segment(carrier, a0, a1);
        var b = GraphicStructure.Segment(carrier, b0, b1);
        return ParticipationSettlementSurface.CreateSegmentSupport(
            new Carrier(),
            carrier,
            scope,
            a,
            b);
    }

    private static IElement AreaParticipation(string column)
    {
        var frame = new FrameBasis();
        var aBounds = (-42L, -22L, 8L, 24L);
        var bBounds = column switch
        {
            "participants" => (18L, -18L, 56L, 20L),
            "touch" => (8L, -22L, 52L, 24L),
            _ => (-8L, -26L, 42L, 18L)
        };
        var aBoundary = RectangleSegments(frame, aBounds.Item1, aBounds.Item2, aBounds.Item3, aBounds.Item4);
        var bBoundary = RectangleSegments(frame, bBounds.Item1, bBounds.Item2, bBounds.Item3, bBounds.Item4);
        var a = GraphicStructure.AreaFill(aBoundary);
        var b = GraphicStructure.AreaFill(bBoundary);
        var areaFrame = GraphicAreaParticipationSurface.AreaFrame(new Carrier(), a, b);

        return column switch
        {
            "union" => GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame),
            "intersection" => Group(
                areaFrame,
                GraphicStructure.AreaFill(RectangleSegments(frame, -8, -22, 8, 18))),
            "difference" => Group(
                a,
                GraphicStructure.AntiAreaFill(bBoundary),
                TensionSlot()),
            _ => areaFrame
        };
    }

    private static BlocksGlossaryStructure ConnectedPath(string id)
    {
        var frame = new FrameBasis();
        return id switch
        {
            "separate-sections" => Spec(
                Group(
                    GraphicStructure.FrameSegment(frame.Point(-52, 18), frame.Point(-12, 4)),
                    GraphicStructure.FrameArc(frame.Point(18, 6), frame.Point(8, 22), frame.Point(50, -12))),
                "S(A,B) + Arc(C,D), no shared P or bond",
                "The sections can be visually near each other without becoming one path. No shared point, bond, or higher-rung bridge has settled them together."),
            "shared-point" => ConnectedSharedPoint(frame),
            "bonded-point" => ConnectedBondedPoint(frame),
            "orthogonal-endpoint" => ConnectedOrthogonalEndpoint(frame),
            "higher-bridge" => ConnectedHigherBridge(frame),
            "closure-tension" => ConnectedClosureTension(frame),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure PathReadBasis(string id)
    {
        var frame = new FrameBasis();
        return id switch
        {
            "xy" => Spec(
                GraphicStructure.FrameSegment(frame.Point(-44, 26), frame.Point(46, -24)),
                "FrameXY(P0) + FrameXY(P1) -> S(P0,P1)",
                "This is the familiar coordinate construction. It is only one way to create section evidence."),
            "circular" => PathCircularBasis(),
            "orthogonal" => PathOrthogonalBasis(frame),
            "mixed" => PathMixedBasis(frame),
            "composite-read" => PathCompositeRead(frame),
            "residue-error" => PathResidueError(frame),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure DerivedPathStructure(string id)
    {
        var frame = new FrameBasis();
        return id switch
        {
            "open-offset" => DerivedOpenOffset(frame),
            "curved-offset" => DerivedCurvedOffset(frame),
            "inset-outset" => DerivedInsetOutset(frame),
            "branch-merge-fill" => DerivedBranchMergeFill(frame),
            "outline-fill" => DerivedOutlineFill(frame),
            "offset-residue" => DerivedOffsetResidue(frame),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure BooleanOffsetRead(string id)
    {
        var frame = new FrameBasis();
        return id switch
        {
            "source-carriers" => BooleanOffsetSourceCarriers(frame),
            "offset-reads" => BooleanOffsetReadStage(frame),
            "split-intersections" => BooleanOffsetSplitIntersections(frame),
            "union-hole" => BooleanOffsetUnionHole(frame),
            "intersection-read" => BooleanOffsetIntersectionRead(frame),
            "rounded-ladder" => BooleanOffsetRoundedLadder(frame),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure DroneClipOffsetLadder(string id)
    {
        var frame = new FrameBasis();
        return id switch
        {
            "source" => DroneClipSource(frame),
            "path-read" => DroneClipPathRead(frame),
            "offset-reads" => DroneClipOffsetReads(frame),
            "intersections" => DroneClipIntersections(frame),
            "split-sections" => DroneClipSplitSections(frame),
            "outside-round" => DroneClipOutsideRound(frame),
            "inside-round" => DroneClipInsideRound(frame),
            "merged-loops" => DroneClipMergedLoops(frame),
            "negative-spaces" => DroneClipNegativeSpaces(frame),
            "settled-read" => DroneClipSettledRead(frame),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure RadialCrescentLadder(string id)
    {
        var frame = new FrameBasis();
        return id switch
        {
            "source-seeds" => RadialCrescentSourceSeeds(frame),
            "endpoint-caps" => RadialCrescentEndpointCaps(frame),
            "trimmed-carriers" => RadialCrescentTrimmedCarriers(frame),
            "offset-boundaries" => RadialCrescentOffsetBoundaries(frame),
            "fill-participants" => RadialCrescentFillParticipants(frame),
            "merged-areas" => RadialCrescentMergedAreas(frame),
            "outline-read" => RadialCrescentOutlineRead(frame),
            "interior-round" => RadialCrescentInteriorRound(frame),
            "exterior-round" => RadialCrescentExteriorRound(frame),
            "settled-area" => RadialCrescentSettledArea(frame),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure DiamondCuspRounding(string id)
    {
        var frame = new FrameBasis();
        return id switch
        {
            "source" => DiamondCuspSource(frame),
            "outer-selection" => DiamondCuspOuterSelection(frame),
            "outer-rounded" => DiamondCuspOuterRounded(frame),
            "inner-selection" => DiamondCuspInnerSelection(frame),
            "inner-rounded" => DiamondCuspInnerRounded(frame),
            "result" => DiamondCuspResult(frame),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure PcbDataAttachment(string id)
    {
        var stage = PcbDataGlossaryGeometry.CreateStage();
        return id switch
        {
            "schematic-intent" => Spec(
                stage.Schematic,
                "Schematic ports + net-basis P(VCC/SIG/GND/LED)",
                "The schematic read groups component ports by net carriers before any physical route is chosen.",
                PcbDataBounds(),
                4),
            "layout-carriers" => Spec(
                stage.LayoutCarriers,
                "Board + footprints + pads + Path(trace sections)",
                "The layout rung adds frame carriers, shared pad sites, and routed path sections, but the copper meaning is still not just geometry.",
                PcbDataBounds(),
                5),
            "data-attachment" => Spec(
                stage.DataAttachments,
                "Trace(path) + attach(net, layer, width, cost)",
                "Each trace carrier locally holds attribute relations on a net basis, layer basis, width basis, and cost/check basis.",
                PcbDataBounds(),
                6),
            "render-read" => Spec(
                stage.RenderRead,
                "Read(layout + data, style basis) -> visible copper",
                "Color and stroke width are adapter reads from layer and width relations; they are not the source of PCB meaning.",
                PcbDataBounds(),
                7),
            "rule-check" => Spec(
                stage.RuleCheck,
                "Read(layout + rules) -> pass evidence + tension",
                "ERC/DRC checks read the same carriers through continuity, width, and clearance constraints, leaving unresolved evidence as tension.",
                PcbDataBounds(),
                8),
            "physical-read" => Spec(
                stage.PhysicalRead,
                "Read(render + fab basis) -> mask + copper + drill",
                "The manufactured board is another interpretation of the same data-bearing structure, not a separate geometry copy.",
                PcbDataBounds(),
                9),
            _ => Spec(Site(), "P(O)")
        };
    }

    private static BlocksGlossaryStructure ConnectedSharedPoint(FrameBasis frame)
    {
        var joint = frame.Point(0, 0);
        return Spec(
            Group(
                GraphicStructure.FrameSegment(frame.Point(-50, 30), joint),
                GraphicStructure.FrameArc(frame.Point(20, 10), joint, frame.Point(50, -16))),
            "S(A,J) + Arc(J,B), same P(J)",
            "The sections are connected because they reuse the same endpoint relation. The fact that one section is curved is incidental.");
    }

    private static BlocksGlossaryStructure ConnectedBondedPoint(FrameBasis frame)
    {
        var leftEnd = frame.Point(-4, 2);
        var rightStart = frame.Point(6, -3);
        return Spec(
            Group(
                GraphicStructure.FrameSegment(frame.Point(-52, 28), leftEnd),
                GraphicStructure.FrameArc(frame.Point(22, 8), rightStart, frame.Point(50, -12)),
                GraphicStructure.Pin(leftEnd, rightStart)),
            "S(A,J1) + Arc(J2,B) + bond(J1,J2)",
            "The endpoints stay distinct as evidence, but a bond relation makes them participate as one connected path point.");
    }

    private static BlocksGlossaryStructure ConnectedOrthogonalEndpoint(FrameBasis frame)
    {
        var end = frame.Point(18, -10);
        var arc = GraphicStructure.FrameArc(frame.Point(-16, 16), frame.Point(-48, 12), end);
        var branch = GraphicStructure.FrameSegment(end, frame.Point(42, -42));
        return Spec(
            Group(arc, branch, GraphicStructure.RadialFlow(end)),
            "Arc(A,J) + S(J,O) + orthogonal_outlet(P(J))",
            "A path section can grow from orthogonal support at the endpoint of an arc. The connection still comes from local relation evidence at J.");
    }

    private static BlocksGlossaryStructure ConnectedHigherBridge(FrameBasis frame)
    {
        var leftEnd = frame.Point(-16, 2);
        var rightStart = frame.Point(18, -8);
        var left = GraphicStructure.FrameSegment(frame.Point(-52, 18), leftEnd);
        var right = GraphicStructure.FrameSegment(rightStart, frame.Point(52, -24));
        var bridge = GraphicStructure.FrameCubicCurve(leftEnd, frame.Point(-2, -28), frame.Point(4, 28), rightStart);
        return Spec(
            Group(left, right, bridge, GraphicStructure.FrameCurveTransition(left, right)),
            "S(A,J1) + S(J2,B) + Bridge(J1,J2)",
            "A higher rung can join sections that were not directly connected, for example by adding a curve or transition surface.");
    }

    private static BlocksGlossaryStructure ConnectedClosureTension(FrameBasis frame)
    {
        var a = frame.Point(0, -42);
        var b = frame.Point(42, -10);
        var c = frame.Point(28, 34);
        var d = frame.Point(-28, 34);
        var e = frame.Point(-42, -8);
        var miss = frame.Point(-10, -34);
        return Spec(
            Group(
                GraphicStructure.FrameSegment(a, b),
                GraphicStructure.FrameSegment(b, c),
                GraphicStructure.FrameSegment(c, d),
                GraphicStructure.FrameSegment(d, e),
                GraphicStructure.FrameSegment(e, miss),
                GraphicStructure.Pin(miss, a),
                TensionSlot()),
            "S(P0,P1)+...+S(P4,Pn) + topology(Pn~P0) + tension",
            "The topology says the path should close even when the geometric evidence misses. The mismatch remains visible as tension instead of being hidden.");
    }

    private static BlocksGlossaryStructure PathCircularBasis()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        var direction = GraphicStructure.Point(circle, 42);
        var length = GraphicStructure.Segment(new Carrier(), 0, 58);
        return Spec(
            Group(origin, circle, direction, length, new Relation(direction, length)),
            "P(O) + CircleBasis(O) + P(theta) + S(length)",
            "The circular template is reusable direction evidence, like a light protractor at an endpoint. It can be moved or rotated without becoming a permanent part of every segment.");
    }

    private static BlocksGlossaryStructure PathOrthogonalBasis(FrameBasis frame)
    {
        var joint = frame.Point(-6, 8);
        var primary = GraphicStructure.FrameSegment(frame.Point(-52, 8), frame.Point(50, 8));
        var branch = GraphicStructure.FrameSegment(joint, frame.Point(-6, -38));
        return Spec(
            Group(primary, branch, GraphicStructure.RadialFlow(primary)),
            "S(A,B) + orthogonal_outlet(S(A,B)) + S(J,O)",
            "The section can be defined from support normal to another carrier rather than from XY position alone.");
    }

    private static BlocksGlossaryStructure PathMixedBasis(FrameBasis frame)
    {
        var origin = frame.Point(-34, 28);
        var endpoint = frame.Point(34, -22);
        var baseCarrier = new Carrier();
        var basePoint = GraphicStructure.Point(baseCarrier, 0);
        var circular = GraphicStructure.CircularCarrierAt(basePoint);
        var segment = GraphicStructure.FrameSegment(origin, endpoint);
        return Spec(
            Group(
                segment,
                basePoint,
                circular,
                GraphicStructure.Point(circular, 38),
                GraphicStructure.RadialFlow(segment)),
            "FrameXY(P0,P1) + CircleBasis(P0,theta) + orthogonal evidence",
            "Multiple bases can constrain the same section at once. A later read can reconcile or expose disagreement as residue.");
    }

    private static BlocksGlossaryStructure PathCompositeRead(FrameBasis frame)
    {
        var a = frame.Point(-54, 30);
        var b = frame.Point(-18, 6);
        var c = frame.Point(14, -10);
        var d = frame.Point(52, -24);
        var line = GraphicStructure.FrameSegment(a, b);
        var arc = GraphicStructure.FrameArc(frame.Point(-2, 18), b, c);
        var curve = GraphicStructure.FrameCubicCurve(c, frame.Point(24, 30), frame.Point(32, -42), d);
        var pathRead = PathRead([line, arc, curve], 120, 10);
        return Spec(
            pathRead.Read,
            "PathRead(sections{S,Arc,Curve}, resolution=12) -> PathCarrier(0,120) + P(12..108)",
            "The new path carrier is the crystallized read. The original sections remain as provenance and range-meld evidence, but the partitions belong to the path carrier itself.");
    }

    private static BlocksGlossaryStructure PathResidueError(FrameBasis frame)
    {
        var a = frame.Point(-42, 28);
        var b = frame.Point(28, 24);
        var c = frame.Point(42, -26);
        var d = frame.Point(-18, -36);
        var target = frame.Point(-38, 22);
        return Spec(
            Group(
                GraphicStructure.FrameSegment(a, b),
                GraphicStructure.FrameSegment(b, c),
                GraphicStructure.FrameSegment(c, d),
                GraphicStructure.Pin(d, target),
                TensionSlot()),
            "loose sections + intended_join(Pn~P0) + residue/tension",
            "Conflicting basis and topology evidence can negotiate later. Until then the attempted join and its error remain inspectable.");
    }

    private static BlocksGlossaryStructure DerivedOpenOffset(FrameBasis frame)
    {
        var sourceSections = new IElement[]
        {
            GraphicStructure.FrameSegment(frame.Point(-52, 30), frame.Point(-12, 6)),
            GraphicStructure.FrameSegment(frame.Point(-12, 6), frame.Point(52, -24))
        };
        var offsetSections = new IElement[]
        {
            GraphicStructure.FrameSegment(frame.Point(-50, 14), frame.Point(-8, -10)),
            GraphicStructure.FrameSegment(frame.Point(-8, -10), frame.Point(54, -40))
        };
        var source = PathRead(sourceSections, 120, 10);
        var offset = OffsetRead(source.Read, source.Carrier, source.Span, offsetSections, 18, 3, 120, 10);
        return Spec(
            offset,
            "PathRead(source) + OffsetRead(normal-distance=18, resolution=3) -> OffsetPathCarrier(0,120)",
            "The offset read carries a signed distance on the source path normal, resolution evidence for that normal measure, witness normals, and a separate derived path carrier.");
    }

    private static BlocksGlossaryStructure DerivedCurvedOffset(FrameBasis frame)
    {
        var sourceCurve = GraphicStructure.FrameCubicCurve(
            frame.Point(-52, 28),
            frame.Point(-18, -40),
            frame.Point(18, 42),
            frame.Point(52, -20));
        var offsetCurve = GraphicStructure.FrameCubicCurve(
            frame.Point(-48, 10),
            frame.Point(-16, -48),
            frame.Point(28, 22),
            frame.Point(56, -34));
        var source = PathRead([sourceCurve], 120, 10);
        var offset = OffsetRead(source.Read, source.Carrier, source.Span, [offsetCurve], 18, 3, 120, 10);
        return Spec(
            offset,
            "CurvePathRead(source) + OffsetRead(normal-distance=18, resolution=3)",
            "The curve offset uses the same structural read as the open offset: source path carrier, normal-distance carrier, resolution evidence, witness normals, and a derived path carrier.");
    }

    private static BlocksGlossaryStructure DerivedInsetOutset(FrameBasis frame)
    {
        var source = RectangleSegments(frame, -34, -24, 34, 24);
        var outset1 = RectangleSegments(frame, -42, -32, 42, 32);
        var outset2 = RectangleSegments(frame, -50, -40, 50, 40);
        var inset1 = RectangleSegments(frame, -24, -16, 24, 16);
        var inset2 = RectangleSegments(frame, -14, -8, 14, 8);
        var sourcePath = PathRead(source, 160, 16);
        return Spec(
            Group(
                sourcePath.Read,
                OffsetRead(sourcePath.Read, sourcePath.Carrier, sourcePath.Span, outset1, 8, 2, 160, 16),
                OffsetRead(sourcePath.Read, sourcePath.Carrier, sourcePath.Span, outset2, 16, 2, 160, 16),
                OffsetRead(sourcePath.Read, sourcePath.Carrier, sourcePath.Span, inset1, -8, 2, 160, 16),
                OffsetRead(sourcePath.Read, sourcePath.Carrier, sourcePath.Span, inset2, -16, 2, 160, 16)),
            "OutlinePathRead + OffsetRead(+d1,+d2,-d1,-d2)",
            "Inset and outset layers are repeated offset reads from one path carrier. Each layer has its own carrier and partitions, so it is related to the outline without replacing it.");
    }

    private static BlocksGlossaryStructure DerivedBranchMergeFill(FrameBasis frame)
    {
        var split = frame.Point(-28, 0);
        var merge = frame.Point(28, 0);
        var upper = GraphicStructure.FrameQuadraticCurve(split, frame.Point(0, -34), merge);
        var lower = GraphicStructure.FrameQuadraticCurve(split, frame.Point(0, 34), merge);
        var trunkLeft = GraphicStructure.FrameSegment(frame.Point(-54, 0), split);
        var trunkRight = GraphicStructure.FrameSegment(merge, frame.Point(54, 0));
        var fill = GraphicStructure.AreaFill(upper, lower);
        return Spec(
            Group(trunkLeft, upper, lower, trunkRight, fill),
            "S(A,J) + branch{upper,lower} + merge(K) -> AreaFill(enclosure)",
            "The branch and merge produce an enclosing path region. The fill is a derived area read over that enclosure, related to but distinct from the branch carriers.");
    }

    private static BlocksGlossaryStructure DerivedOutlineFill(FrameBasis frame)
    {
        var boundary = RectangleSegments(frame, -36, -26, 36, 26);
        var fill = GraphicStructure.AreaFill(boundary);
        return Spec(
            Group(Group(boundary), fill),
            "ClosedPath(ordered outline) -> AreaFill(face)",
            "This is the same family of read as a path or offset: the fill is derived from the outline, then can be inspected independently.");
    }

    private static BlocksGlossaryStructure DerivedOffsetResidue(FrameBasis frame)
    {
        var sourceSections = new IElement[]
        {
            GraphicStructure.FrameSegment(frame.Point(-52, 30), frame.Point(-10, 4)),
            GraphicStructure.FrameSegment(frame.Point(-10, 4), frame.Point(52, -26))
        };
        var intendedSections = new IElement[]
        {
            GraphicStructure.FrameSegment(frame.Point(-48, 14), frame.Point(-6, -12)),
            GraphicStructure.FrameSegment(frame.Point(-6, -12), frame.Point(56, -42))
        };
        var observedSections = new IElement[]
        {
            GraphicStructure.FrameSegment(frame.Point(-48, 14), frame.Point(2, -4)),
            GraphicStructure.FrameSegment(frame.Point(2, -4), frame.Point(56, -42))
        };
        var source = PathRead(sourceSections, 120, 10);
        var observed = PathRead(observedSections, 120, 10);
        var residue = Group(observed.Read, TensionSlot());
        var intended = OffsetRead(source.Read, source.Carrier, source.Span, intendedSections, 18, 3, 120, 10, residue);
        return Spec(
            intended,
            "OffsetRead(source,+d,resolution) + observed PathRead + residue/tension",
            "The intended offset carrier and the mismatching observed carrier are both preserved. The difference is tension attached to the read rather than a hidden failure.");
    }

    private static (Relation Read, Carrier Carrier, Relation Span) PathRead(IReadOnlyList<IElement> sections, long lengthTicks, int partitionCount)
    {
        var source = Group([.. sections]);
        var descriptor = new Relation(source, Relation.Segment(Carrier.Unknown, 0, Math.Max(1, lengthTicks)));
        var pathCarrier = new Carrier(UnitShape.DescribedAtom(descriptor));
        var span = GraphicStructure.Segment(pathCarrier, 0, lengthTicks);
        var crystal = Relation.OrderedGroup(
            pathCarrier,
            [span, .. PathPartitionPoints(pathCarrier, lengthTicks, partitionCount), ResolutionEvidence(pathCarrier, Math.Max(1, lengthTicks / Math.Max(1, partitionCount)))]);
        var provenance = PathRangeMelds(sections, pathCarrier, lengthTicks);
        return (new Relation(source, Group(crystal, provenance)), pathCarrier, span);
    }

    private static Relation OffsetRead(
        Relation sourcePathRead,
        Carrier sourcePathCarrier,
        Relation sourcePathSpan,
        IReadOnlyList<IElement> offsetSections,
        long distanceTicks,
        long resolutionTicks,
        long lengthTicks,
        int partitionCount,
        IElement? residueOrTension = null)
    {
        var normalCarrier = new Carrier(UnitShape.OrthogonalOf(sourcePathSpan));
        var normalDistance = GraphicStructure.Segment(normalCarrier, 0, distanceTicks);
        var normalBasis = Group(
            normalDistance,
            ResolutionEvidence(normalCarrier, Math.Max(1, Math.Abs(resolutionTicks))));
        var offsetPath = PathRead(offsetSections, lengthTicks, partitionCount);
        var witnesses = OffsetWitnesses(
            sourcePathCarrier,
            offsetPath.Carrier,
            normalCarrier,
            distanceTicks,
            lengthTicks,
            Math.Min(4, Math.Max(1, partitionCount - 1)));
        var readBasis = residueOrTension is null
            ? Group(sourcePathRead, normalBasis, witnesses)
            : Group(sourcePathRead, normalBasis, witnesses, residueOrTension);
        var read = new Relation(readBasis, offsetPath.Read);
        return read;
    }

    private static IElement[] PathPartitionPoints(Carrier pathCarrier, long lengthTicks, int partitionCount)
    {
        var count = Math.Max(1, partitionCount);
        var points = new List<IElement>();
        for (var index = 1; index < count; index++)
        {
            points.Add(GraphicStructure.Point(pathCarrier, lengthTicks * index / count));
        }

        return [.. points];
    }

    private static Relation PathRangeMelds(IReadOnlyList<IElement> sections, Carrier pathCarrier, long lengthTicks)
    {
        var elements = new List<IElement>();
        var count = Math.Max(1, sections.Count);
        for (var index = 0; index < sections.Count; index++)
        {
            var start = lengthTicks * index / count;
            var end = lengthTicks * (index + 1) / count;
            elements.Add(new Relation(sections[index], GraphicStructure.Segment(pathCarrier, start, end)));
            if (index > 0)
            {
                elements.Add(new Relation(sections[index - 1], sections[index]));
            }
        }

        return Group([.. elements]);
    }

    private static Relation OffsetWitnesses(
        Carrier sourcePathCarrier,
        Carrier offsetPathCarrier,
        Carrier normalCarrier,
        long distanceTicks,
        long lengthTicks,
        int witnessCount)
    {
        var witnesses = new List<IElement>();
        for (var index = 1; index <= witnessCount; index++)
        {
            var ticks = lengthTicks * index / (witnessCount + 1);
            var sourcePoint = GraphicStructure.Point(sourcePathCarrier, ticks);
            var offsetPoint = GraphicStructure.Point(offsetPathCarrier, ticks);
            var normalDistance = GraphicStructure.Segment(normalCarrier, 0, distanceTicks);
            witnesses.Add(new Relation(sourcePoint, new Relation(normalDistance, offsetPoint)));
        }

        return Group([.. witnesses]);
    }

    private static Relation ResolutionEvidence(Carrier sourceCarrier, long sourceTicksPerTick)
    {
        var ticks = Math.Max(1, sourceTicksPerTick);
        var resolutionCarrier = new Carrier(UnitShape.ResolutionOf(sourceCarrier.UnitShape, ticks));
        return new Relation(
            GraphicStructure.Segment(sourceCarrier, 0, ticks),
            GraphicStructure.Point(resolutionCarrier, 1));
    }

    private static BlocksGlossaryStructure BooleanOffsetSourceCarriers(FrameBasis frame)
    {
        var sources = BooleanOffsetBoxSources(frame);
        var pathReads = sources
            .Select(section => PathRead([section], 80, 4).Read)
            .ToArray();
        return Spec(
            Group(pathReads),
            "S0..S3 -> PathRead(Si)",
            "The starting lines are individual path reads. They may later settle into one outline, but the source carriers remain independent evidence at this rung.");
    }

    private static BlocksGlossaryStructure BooleanOffsetReadStage(FrameBasis frame)
    {
        var sources = BooleanOffsetBoxSources(frame);
        var offsets = BooleanOffsetPairReads(sources, frame);
        return Spec(
            Group(offsets),
            "PathRead(Si) + OffsetRead(+d,-d,resolution) for each carrier",
            "Each source carrier creates positive and inverse offset reads. The bands shown by the renderer are support from these paired derived paths, not edits to the source carriers.");
    }

    private static BlocksGlossaryStructure BooleanOffsetSplitIntersections(FrameBasis frame)
    {
        var sources = BooleanOffsetBoxSources(frame);
        var offsets = BooleanOffsetPairReads(sources, frame);
        var intersections = BooleanOffsetCornerIntersections(frame, offsets);
        var splits = offsets
            .Take(4)
            .Select(offset => SplitRead(offset, intersections))
            .ToArray();
        return Spec(
            Group([.. offsets, .. intersections, .. splits]),
            "Intersect(offset_i,offset_j) -> Pij; Split(offset_i,Pij)",
            "Intersection points are derived locations where offset paths participate together. Split reads use those points to divide offset carriers into selectable sections.");
    }

    private static BlocksGlossaryStructure BooleanOffsetUnionHole(FrameBasis frame)
    {
        var sources = BooleanOffsetBoxSources(frame);
        var offsets = BooleanOffsetPairReads(sources, frame);
        var outer = RectangleSegments(frame, -48, -34, 48, 34);
        var inner = RectangleSegments(frame, -24, -13, 24, 13);
        var outerLoop = LoopRead(outer, 160, clockwise: false);
        var innerLoop = LoopRead(inner, 96, clockwise: true);
        return Spec(
            Group(
                Group(offsets),
                outerLoop,
                innerLoop,
                GraphicStructure.AreaFill(outer),
                GraphicStructure.AntiAreaFill(inner)),
            "Split sections -> LoopOuter + LoopInner; AreaFill(LoopOuter) + AntiFill(LoopInner)",
            "The union read selects offset sections into an outer loop and an opposing inner loop. The negative space is represented as anti-fill participation, not as a visual eraser.");
    }

    private static BlocksGlossaryStructure BooleanOffsetIntersectionRead(FrameBasis frame)
    {
        var horizontal = RectangleSegments(frame, -48, -11, 48, 11);
        var vertical = RectangleSegments(frame, -12, -40, 12, 40);
        var overlap = RectangleSegments(frame, -12, -11, 12, 11);
        var horizontalLoop = LoopRead(horizontal, 120, clockwise: false);
        var verticalLoop = LoopRead(vertical, 110, clockwise: false);
        var overlapLoop = LoopRead(overlap, 60, clockwise: false);
        var intersection = new Relation(
            Group(horizontalLoop, verticalLoop, BooleanAreaIntersectionWitness(horizontalLoop, verticalLoop, overlapLoop)),
            Group(overlapLoop, GraphicStructure.AreaFill(overlap)));
        return Spec(
            intersection,
            "LoopA + LoopB + Intersect(A,B) -> LoopShared + AreaFill(shared)",
            "Intersection keeps only the support occupied by both derived areas. The two input loops remain inspectable, while the shared loop becomes its own fillable area.");
    }

    private static BlocksGlossaryStructure BooleanOffsetRoundedLadder(FrameBasis frame)
    {
        var source = RectangleSegments(frame, -42, -30, 42, 30);
        var sourceLoop = LoopRead(source, 144, clockwise: false);
        var rounded = RoundedRectangleSections(frame, -45, -33, 45, 33, 12);
        var roundedLoop = LoopRead(rounded, 144, clockwise: false);
        var inner = RoundedRectangleSections(frame, -21, -11, 21, 11, 8);
        var innerLoop = LoopRead(inner, 72, clockwise: true);
        var cornerRead = new Relation(sourceLoop, roundedLoop);
        var mergeRead = new Relation(
            Group(cornerRead, innerLoop),
            Group(GraphicStructure.AreaFill(rounded), GraphicStructure.AntiAreaFill(inner)));
        return Spec(
            mergeRead,
            "LoopSquare -> CornerRoundRead -> LoopRounded; merge with LoopInner -> AreaFill + AntiFill",
            "This ladder replaces corner sections with rounded transition carriers before the boolean merge. The rounded result is a new rung with provenance back to the square loop.");
    }

    private static IElement[] BooleanOffsetBoxSources(FrameBasis frame)
    {
        var a = frame.Point(-36, -20);
        var b = frame.Point(36, -20);
        var c = frame.Point(36, 20);
        var d = frame.Point(-36, 20);
        return
        [
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d),
            GraphicStructure.FrameSegment(d, a)
        ];
    }

    private static IElement[] BooleanOffsetPairReads(IReadOnlyList<IElement> sources, FrameBasis frame)
    {
        var reads = new List<IElement>();
        var positive = RectangleSegments(frame, -48, -34, 48, 34);
        var inverse = RectangleSegments(frame, -24, -13, 24, 13);
        for (var index = 0; index < sources.Count; index++)
        {
            var source = PathRead([sources[index]], 80, 4);
            reads.Add(source.Read);
            reads.Add(OffsetRead(source.Read, source.Carrier, source.Span, [positive[index % positive.Length]], 12, 2, 80, 4));
            reads.Add(OffsetRead(source.Read, source.Carrier, source.Span, [inverse[index % inverse.Length]], -12, 2, 80, 4));
        }

        return [.. reads];
    }

    private static IElement[] BooleanOffsetCornerIntersections(FrameBasis frame, IReadOnlyList<IElement> offsets)
    {
        var intersectionCarrier = new Carrier(UnitShape.DescribedAtom(Group([.. offsets])));
        var sites = new[]
        {
            frame.Point(-48, -34),
            frame.Point(48, -34),
            frame.Point(48, 34),
            frame.Point(-48, 34),
            frame.Point(-24, -13),
            frame.Point(24, -13),
            frame.Point(24, 13),
            frame.Point(-24, 13)
        };
        var intersections = new List<IElement>();
        for (var index = 0; index < sites.Length; index++)
        {
            var carrierPoint = GraphicStructure.Point(intersectionCarrier, index + 1);
            intersections.Add(new Relation(sites[index], carrierPoint));
        }

        return [.. intersections];
    }

    private static Relation SplitRead(IElement source, IReadOnlyList<IElement> intersections)
    {
        var splitCarrier = new Carrier(UnitShape.DescribedAtom(new Relation(source, Group([.. intersections]))));
        return new Relation(
            Group(source, Group([.. intersections])),
            Group(
                GraphicStructure.Segment(splitCarrier, 0, 1),
                GraphicStructure.Segment(splitCarrier, 1, 2),
                GraphicStructure.Segment(splitCarrier, 2, 3)));
    }

    private static Relation LoopRead(IReadOnlyList<IElement> sections, long lengthTicks, bool clockwise)
    {
        var path = PathRead(sections, lengthTicks, Math.Max(4, sections.Count));
        var loopCarrier = new Carrier(UnitShape.DescribedAtom(path.Read));
        var loopSpan = clockwise
            ? GraphicStructure.DirectedSegment(loopCarrier, lengthTicks, 0)
            : GraphicStructure.DirectedSegment(loopCarrier, 0, lengthTicks);
        return new Relation(
            path.Read,
            Group(
                loopSpan,
                GraphicStructure.Point(loopCarrier, 0),
                GraphicStructure.Point(loopCarrier, lengthTicks)));
    }

    private static Relation BooleanAreaIntersectionWitness(IElement first, IElement second, IElement shared) =>
        new(Group(first, second), shared);

    private static IElement[] RoundedRectangleSections(FrameBasis frame, long left, long top, long right, long bottom, long radius)
    {
        var topLeftStart = frame.Point(left + radius, top);
        var topRightStart = frame.Point(right - radius, top);
        var topRightEnd = frame.Point(right, top + radius);
        var bottomRightStart = frame.Point(right, bottom - radius);
        var bottomRightEnd = frame.Point(right - radius, bottom);
        var bottomLeftStart = frame.Point(left + radius, bottom);
        var bottomLeftEnd = frame.Point(left, bottom - radius);
        var topLeftEnd = frame.Point(left, top + radius);
        return
        [
            GraphicStructure.FrameSegment(topLeftStart, topRightStart),
            GraphicStructure.FrameQuadraticCurve(topRightStart, frame.Point(right, top), topRightEnd),
            GraphicStructure.FrameSegment(topRightEnd, bottomRightStart),
            GraphicStructure.FrameQuadraticCurve(bottomRightStart, frame.Point(right, bottom), bottomRightEnd),
            GraphicStructure.FrameSegment(bottomRightEnd, bottomLeftStart),
            GraphicStructure.FrameQuadraticCurve(bottomLeftStart, frame.Point(left, bottom), bottomLeftEnd),
            GraphicStructure.FrameSegment(bottomLeftEnd, topLeftEnd),
            GraphicStructure.FrameQuadraticCurve(topLeftEnd, frame.Point(left, top), topLeftStart)
        ];
    }

    private static BlocksGlossaryStructure DroneClipSource(FrameBasis frame)
    {
        var outer = DroneClipOuterSections(frame);
        var slot = RectangleSegments(frame, -26, -7, 26, 7);
        var holes = DroneClipHoleBoundaries(frame);
        return Spec(
            Group(Group(outer), Group(slot), Group(holes)),
            "STEP footprint evidence -> source sections{outer, slot, voids}",
            "This source is a simplified read of the old droneArmClip-Body footprint: independent carriers for the outer clip, arm slot, and internal voids.");
    }

    private static BlocksGlossaryStructure DroneClipPathRead(FrameBasis frame)
    {
        var outer = PathRead(DroneClipOuterSections(frame), 220, 12);
        var slot = PathRead(RectangleSegments(frame, -26, -7, 26, 7), 96, 8);
        return Spec(
            Group(outer.Read, slot.Read),
            "source sections -> PathCarrier(outer) + PathCarrier(slot)",
            "The first rung crystallizes connected sections into path carriers. The new carriers can be partitioned without directly correlating every tick to the source unless requested.");
    }

    private static BlocksGlossaryStructure DroneClipOffsetReads(FrameBasis frame)
    {
        var outer = PathRead(DroneClipOuterSections(frame), 220, 12);
        var slot = PathRead(RectangleSegments(frame, -26, -7, 26, 7), 96, 8);
        var outerOffset = OffsetRead(outer.Read, outer.Carrier, outer.Span, DroneClipOuterSections(frame), 5, 1, 220, 12);
        var slotOffset = OffsetRead(slot.Read, slot.Carrier, slot.Span, RectangleSegments(frame, -29, -10, 29, 10), -3, 1, 96, 8);
        return Spec(
            Group(outerOffset, slotOffset),
            "PathCarrier + OffsetRead(outside,+d); SlotCarrier + OffsetRead(inside,-d)",
            "Offset reads are normal-distance rungs from the source carriers. The outer support grows outward while the slot support reads inward as negative space.");
    }

    private static BlocksGlossaryStructure DroneClipIntersections(FrameBasis frame)
    {
        var outer = PathRead(DroneClipOuterSections(frame), 220, 12);
        var slot = PathRead(RectangleSegments(frame, -26, -7, 26, 7), 96, 8);
        var intersections = DroneClipCuspIntersections(frame, Group(outer.Read, slot.Read));
        return Spec(
            Group(outer.Read, slot.Read, Group(intersections)),
            "OffsetRead edges -> Intersect(cusp_i,cusp_j) -> Pij",
            "Offset and slot boundaries produce derived intersection/cusp locations. These locations are later used to split and select path sections.");
    }

    private static BlocksGlossaryStructure DroneClipSplitSections(FrameBasis frame)
    {
        var outer = PathRead(DroneClipOuterSections(frame), 220, 12);
        var slot = PathRead(RectangleSegments(frame, -26, -7, 26, 7), 96, 8);
        var intersections = DroneClipCuspIntersections(frame, Group(outer.Read, slot.Read));
        return Spec(
            Group(
                outer.Read,
                slot.Read,
                SplitRead(outer.Read, intersections),
                SplitRead(slot.Read, intersections)),
            "Pij + PathCarrier -> split sections + selected support",
            "The split rung turns intersection points into selectable path sections. Some sections become the final loops; others remain residue/provenance.");
    }

    private static BlocksGlossaryStructure DroneClipOutsideRound(FrameBasis frame)
    {
        var square = LoopRead(DroneClipOuterSections(frame), 220, clockwise: false);
        var rounded = LoopRead(DroneClipRoundedOuterSections(frame), 220, clockwise: false);
        return Spec(
            new Relation(square, rounded),
            "outer split sections -> CornerRoundRead(outside cusps) -> rounded outer loop",
            "The outside cusp rounding rung replaces selected corner sections with curved transition carriers while preserving the earlier outer-loop provenance.");
    }

    private static BlocksGlossaryStructure DroneClipInsideRound(FrameBasis frame)
    {
        var slot = LoopRead(RectangleSegments(frame, -26, -7, 26, 7), 96, clockwise: true);
        var roundedSlot = LoopRead(RoundedRectangleSections(frame, -29, -10, 29, 10, 5), 96, clockwise: true);
        var holes = DroneClipHoleBoundaries(frame);
        return Spec(
            Group(new Relation(slot, roundedSlot), Group(holes)),
            "slot loop -> CornerRoundRead(inside cusps) -> rounded inner loop",
            "Inside cusp rounding is a separate rung from outside rounding. The slot and void carriers keep inverse participation and can later become anti-fill.");
    }

    private static BlocksGlossaryStructure DroneClipMergedLoops(FrameBasis frame)
    {
        var outer = LoopRead(DroneClipRoundedOuterSections(frame), 220, clockwise: false);
        var slot = LoopRead(RoundedRectangleSections(frame, -29, -10, 29, 10, 5), 96, clockwise: true);
        var holes = DroneClipHoleBoundaries(frame);
        return Spec(
            Group(outer, slot, Group(holes), new Relation(Group(outer, slot, Group(holes)), TensionSlot())),
            "LoopOuter + LoopSlot + LoopVoids -> merged loop set",
            "The merge rung groups positive and inverse loops without losing their separate orientation. Any disagreement can remain as tension.");
    }

    private static BlocksGlossaryStructure DroneClipNegativeSpaces(FrameBasis frame)
    {
        var outer = DroneClipRoundedOuterSections(frame);
        var slot = RoundedRectangleSections(frame, -29, -10, 29, 10, 5);
        var holes = DroneClipHoleBoundaries(frame);
        return Spec(
            Group(
                GraphicStructure.AreaFill(outer),
                GraphicStructure.AntiAreaFill(slot),
                GraphicStructure.AntiAreaFill(holes)),
            "AreaFill(outer) + AntiFill(slot) + AntiFill(voids)",
            "The filled part is positive support. The slot and holes are anti-fill participation, so the negative spaces are structural holes.");
    }

    private static BlocksGlossaryStructure DroneClipSettledRead(FrameBasis frame)
    {
        var source = DroneClipOuterSections(frame);
        var outer = DroneClipRoundedOuterSections(frame);
        var slot = RoundedRectangleSections(frame, -29, -10, 29, 10, 5);
        var holes = DroneClipHoleBoundaries(frame);
        var settled = Group(
            GraphicStructure.AreaFill(outer),
            GraphicStructure.AntiAreaFill(slot),
            GraphicStructure.AntiAreaFill(holes));
        return Spec(
            new Relation(Group(Group(source), TensionSlot()), settled),
            "source + ladder residue -> settled clip read",
            "The final rung is an inspectable settled read: source provenance, rounded positive boundary, structural negative spaces, and residue for whatever the simplified read did not express.");
    }

    private static BlocksGlossaryStructure RadialCrescentSourceSeeds(FrameBasis frame)
    {
        var centerStrokes = RadialCrescentCenterStrokes(frame);
        var body = RadialCrescentBodyBoundaries(frame);
        return Spec(
            Group(Group(centerStrokes), Group(body)),
            "SketchViewer crescent+bent seed -> center carriers + body boundaries",
            "This rebuilds the Crescent radial ladder source as carrier evidence: two curved crescent strokes, two bent-arm polylines, a circle body, and a diamond body. It does not depend on the old Sketch Viewer render completing.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentEndpointCaps(FrameBasis frame)
    {
        var source = Group(Group(RadialCrescentCenterStrokes(frame)), Group(RadialCrescentBodyBoundaries(frame)));
        var caps = RadialCrescentEndpointCaps(frame, outerRadius: 28);
        var holes = RadialCrescentEndpointCaps(frame, outerRadius: 8);
        return Spec(
            new Relation(source, Group(Group(caps), Group(holes))),
            "LineTipSelection(source) -> endpoint CircleCaps(r=28) + endpoint holes(r=8)",
            "The endpoint rings are generated from selected open tips. The hole circles are paired anti-support for those same cap centers, not decorative marks.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentTrimmedCarriers(FrameBasis frame)
    {
        var source = RadialCrescentCenterStrokes(frame);
        var caps = RadialCrescentEndpointCaps(frame, outerRadius: 28);
        var trimmed = RadialCrescentTrimmedCenterStrokes(frame);
        return Spec(
            new Relation(Group(Group(source), Group(caps)), Group(trimmed)),
            "center carriers + endpoint caps -> trimmed carriers",
            "The open carriers are shortened against the cap boundaries. The original strokes remain provenance, while the trimmed strokes are the source for later offset reads.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentOffsetBoundaries(FrameBasis frame)
    {
        var trimmed = RadialCrescentTrimmedCenterStrokes(frame);
        var path = PathRead(trimmed, 360, 18);
        var offsets = OffsetRead(
            path.Read,
            path.Carrier,
            path.Span,
            RadialCrescentExteriorSharpBoundaries(frame),
            10,
            2,
            360,
            18);
        return Spec(
            offsets,
            "PathRead(trimmed carriers) + OffsetRead(normal-distance=10,resolution=2)",
            "The offset boundary rung carries signed normal distance and resolution evidence. The rendered bands can be calculated geometrically, but the meaning is the normal-distance read from the path carrier.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentFillParticipants(FrameBasis frame)
    {
        var exterior = RadialCrescentExteriorSharpBoundaries(frame);
        var holes = RadialCrescentInteriorHoles(frame, rounded: false);
        var participants = Group(
            GraphicStructure.AreaFill([.. exterior]),
            GraphicStructure.AntiAreaFill([.. holes]));
        return Spec(
            participants,
            "Offset boundaries -> AreaFill(participants) + AntiFill(endpoint/body holes)",
            "The early fill rung creates separate positive and inverse participants. They have not yet been forced into one settled outline.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentMergedAreas(FrameBasis frame)
    {
        var exterior = RadialCrescentExteriorSharpBoundaries(frame);
        var holes = RadialCrescentInteriorHoles(frame, rounded: false);
        var cutHoles = RadialCrescentInteriorCutOutlines(frame, rounded: false);
        var areaFrame = GraphicAreaParticipationSurface.AreaFrame(
            new Carrier(),
            GraphicStructure.AreaFill([.. exterior]),
            GraphicStructure.AntiAreaFill([.. holes]));
        var exteriorOutline = LoopRead(exterior, 420, clockwise: false);
        var holeOutline = LoopRead(cutHoles, 220, clockwise: true);
        var cutRead = RadialCrescentInteriorOutlineCutRead(frame, holes, cutHoles);
        var outlineFrame = GraphicOutlineSurface.OutlineFrame(new Carrier(), exteriorOutline, holeOutline);
        var areaUnion = GraphicAreaParticipationSurface.AreaUnion(new Carrier(), areaFrame, outlineFrame);
        var outlineRead = GraphicOutlineSurface.FromArea(new Carrier(), areaUnion, outlineFrame);
        return Spec(
            Group(areaUnion, outlineRead, cutRead),
            "AreaUnion(positive support, inverse holes) -> CutRead(intersections) -> OutlineRead(exterior cw + interior ccw)",
            "The merge gathers positive support and then cuts the inverse body holes anywhere arm support crosses them. The resulting interior loops carry shared cut sites, so later cusp selection can inspect the outline structure instead of the fill image.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentOutlineRead(FrameBasis frame)
    {
        var exterior = RadialCrescentExteriorSharpBoundaries(frame);
        var holes = RadialCrescentInteriorHoles(frame, rounded: false);
        var cutHoles = RadialCrescentInteriorCutOutlines(frame, rounded: false);
        var cutRead = RadialCrescentInteriorOutlineCutRead(frame, holes, cutHoles);
        var outlineCarrier = new Carrier(UnitShape.DescribedAtom(Group([.. exterior, .. holes])));
        var outline = Group(
            LoopRead(exterior, 420, clockwise: false),
            LoopRead(cutHoles, 220, clockwise: true),
            cutRead,
            GraphicStructure.Segment(outlineCarrier, 0, 420),
            ResolutionEvidence(outlineCarrier, 12));
        return Spec(
            new Relation(Group(GraphicStructure.AreaFill(exterior), GraphicStructure.AntiAreaFill(holes)), outline),
            "AreaFrame -> outline witness + partitioned outline carrier",
            "The outline read is independent but related to the area frame, much like a path read over several source sections.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentInteriorRound(FrameBasis frame)
    {
        var holes = RadialCrescentInteriorCutOutlines(frame, rounded: false);
        var rounded = RadialCrescentInteriorCutOutlines(frame, rounded: true);
        var cuspDomain = RadialCrescentInteriorCusps(frame, Group(holes));
        return Spec(
            new Relation(Group(Group(holes), Group(cuspDomain)), Group(rounded)),
            "InteriorCuspSelection -> RoundedBoundary(inner holes)",
            "Interior cusp rounding is its own rung. It replaces selected inverse-boundary corners while preserving anti-fill participation.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentExteriorRound(FrameBasis frame)
    {
        var exterior = RadialCrescentExteriorSharpBoundaries(frame);
        var rounded = RadialCrescentExteriorRoundedBoundaries(frame);
        var cuspDomain = RadialCrescentExteriorCusps(frame, Group(exterior));
        return Spec(
            new Relation(Group(Group(exterior), Group(cuspDomain)), Group(rounded)),
            "ExteriorCuspSelection -> RoundedBoundary(outer support)",
            "The exterior rounding rung selects outside cusps and replaces them with curve transitions. It is separate from the earlier interior rounding rung.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentSettledArea(FrameBasis frame)
    {
        var source = Group(Group(RadialCrescentCenterStrokes(frame)), Group(RadialCrescentBodyBoundaries(frame)));
        var exterior = RadialCrescentExteriorRoundedBoundaries(frame);
        var holes = RadialCrescentInteriorCutOutlines(frame, rounded: true);
        var settled = Group(
            GraphicStructure.AreaFill(exterior),
            GraphicStructure.AntiAreaFill(holes),
            LoopRead(exterior, 420, clockwise: false),
            LoopRead(holes, 220, clockwise: true));
        return Spec(
            new Relation(Group(source, TensionSlot()), settled),
            "source + endpoint/offset/fill/rounding rungs -> settled crescent radial area",
            "The final read keeps provenance and residue attached to the settled area. The broken old render is treated as tension, while the rebuilt structure remains inspectable.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentCircularBodyOutlineExperiment(FrameBasis frame)
    {
        var sourceOutline = RadialCrescentCircularBodySourceOutline(frame);
        return Spec(
            sourceOutline,
            "Circle body + rectangular arms + motor caps",
            "The first frame shows only the source boundaries used by the experimental read: one body circle, four diagonal rectangular arm carriers, and four motor cap circles.",
            RadialCrescentCircularBodyBounds(),
            7);
    }

    private static BlocksGlossaryStructure RadialCrescentCircularBodyExteriorSitesExperiment(FrameBasis frame)
    {
        var walkedOutline = RadialCrescentCircularBodyWalkedOutline(frame);
        var endpointMarkers = RadialCrescentExteriorEndpointMarkers(walkedOutline);
        return Spec(
            Group(walkedOutline, endpointMarkers),
            "BoundarySectionWalk -> exterior endpoint witnesses",
            "This intermediate frame marks the exterior perimeter endpoints after the intersection split. The small circles are Gojo circle elements around the walked outline sites.",
            RadialCrescentCircularBodyBounds(),
            7);
    }

    private static BlocksGlossaryStructure RadialCrescentCircularBodyWalkedOutlineExperiment(FrameBasis frame)
    {
        var walkedOutline = RadialCrescentCircularBodyWalkedOutline(frame);
        return Spec(
            walkedOutline,
            "AreaFrame(...) -> exterior boundary-section walk",
            "The exterior frame keeps only the union perimeter. Interior overlap fragments are removed; each remaining line or arc section is emitted as its own Gojo element with shared endpoint relations.",
            RadialCrescentCircularBodyBounds(),
            7);
    }

    private static BlocksGlossaryStructure RadialCrescentCircularBodyExplodedOutlineExperiment(FrameBasis frame)
    {
        var walkedOutline = RadialCrescentCircularBodyWalkedOutline(frame);
        var explodedOutline = RadialCrescentExplodedOutlineSections(walkedOutline);
        return Spec(
            explodedOutline,
            "BoundarySectionWalk -> exploded edge sections",
            "The third frame reads the walked outline sections and moves each section away from the body center. The sections are no longer visually joined, but each remains a Gojo line or arc section derived from the walked outline.",
            RadialCrescentCircularBodyExplodedBounds(),
            7);
    }

    private static BlocksGlossaryStructure RadialCrescentCircularBodyRoundedOutlineExperiment(FrameBasis frame)
    {
        var walkedOutline = RadialCrescentCircularBodyWalkedOutline(frame);
        var selection = RadialCrescentCircularBodyCuspSelection(walkedOutline);
        var roundAction = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            selection,
            walkedOutline,
            radiusTicks: 18);
        if (!GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
                roundAction,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            return Spec(
                roundAction,
                "Exterior cusp selection -> unresolved rounding",
                "The rounding read did not settle; the unresolved action remains visible as Gojo structure.",
                RadialCrescentCircularBodyBounds(),
                7);
        }

        return Spec(
            roundAction,
            "Walked outline + CuspSelection(16) -> rounded arc-section splice",
            "The final frame rejoins the walked sections and rounds every exterior cusp. Each cusp trims the incoming and outgoing source sections and inserts a new Gojo arc section, while retained circular pieces stay partial-circle arc sections.",
            RadialCrescentCircularBodyBounds(),
            7);
    }

    private static BlocksGlossaryStructure RadialCrescentCircularBodyReplacementAreasExperiment(FrameBasis frame)
    {
        var walkedOutline = RadialCrescentCircularBodyWalkedOutline(frame);
        var selection = RadialCrescentCircularBodyCuspSelection(walkedOutline);
        var roundAction = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            selection,
            walkedOutline,
            radiusTicks: 18);
        if (!GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
                roundAction,
                out _,
                out _,
                out _,
                out var roundedFrame,
                out _))
        {
            return Spec(
                roundAction,
                "Exterior cusp selection -> unresolved replacement groups",
                "The replacement grouping did not settle; the unresolved rounding action remains visible as Gojo structure.",
                RadialCrescentCircularBodyBounds(),
                7);
        }

        var replacementGroups = RadialCrescentRoundedReplacementGroups(walkedOutline, roundedFrame);
        return Spec(
            replacementGroups,
            "RoundedBoundary -> [new arcs | retained lines | retained circle arcs]",
            "This frame separates the rounded result into three Gojo groups: newly inserted cusp arcs, retained straight line sections, and retained circular arc sections from the body and motor caps.",
            RadialCrescentCircularBodyBounds(),
            7);
    }

    private static Relation RadialCrescentCircularBodyWalkedOutline(FrameBasis frame)
    {
        var areaFrame = RadialCrescentCircularBodyPositiveAreaFrame(frame);
        return GraphicAreaSettlementSurface.SettledPositiveBoundarySectionOutlinesFromAreas(
            new Carrier(),
            areaFrame);
    }

    private static Relation RadialCrescentCircularBodyCuspSelection(IElement walkedOutline)
    {
        var domain = GraphicBoundaryConceptSurface.CuspCandidateDomain(
            new Carrier(),
            walkedOutline,
            witnessRadiusTicks: 7);
        var pattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        return GraphicSelectionSurface.Search(new Carrier(), walkedOutline, domain, pattern);
    }

    private static Relation RadialCrescentRoundedReplacementGroups(IElement walkedOutline, IElement roundedFrame)
    {
        var sourceCircleCenters = GraphicReadGraph.From(walkedOutline)
            .ReadsOf<SharedFrameArcRead>()
            .Where(arc => TryPointKey(arc.Center, out _))
            .Select(arc =>
            {
                _ = TryPointKey(arc.Center, out var key);
                return key;
            })
            .ToHashSet();
        var roundedGraph = GraphicReadGraph.From(roundedFrame);
        var arcSpanIds = roundedGraph.ReadsOf<SharedFrameArcRead>()
            .Select(arc => SourceId(arc.Span.Source))
            .ToHashSet();
        var newCurves = new List<IElement>();
        var circleRemainders = new List<IElement>();
        foreach (var section in roundedGraph.ReadsOf<SharedFrameArcSectionRead>()
                     .DistinctBy(section => SourceId(section.Source)))
        {
            if (TryPointKey(section.Center, out var centerKey) &&
                sourceCircleCenters.Contains(centerKey))
            {
                circleRemainders.Add(section.Source);
            }
            else
            {
                newCurves.Add(section.Source);
            }
        }

        var straightRemainders = roundedGraph.ReadsOf<SharedFrameSegmentRead>()
            .DistinctBy(segment => SourceId(segment.Source))
            .Where(segment => !arcSpanIds.Contains(SourceId(segment.Source)))
            .Select(segment => segment.Source)
            .Cast<IElement>()
            .ToArray();

        return Group(
            Group([.. newCurves]),
            Group(straightRemainders),
            Group([.. circleRemainders]));
    }

    private static Relation RadialCrescentCircularBodySourceOutline(FrameBasis frame) =>
        Group(
            [
                FrameCircle(frame, 0, 0, 92),
                .. RadialCrescentCircularBodyArmSections(frame),
                .. RadialCrescentCircularBodyCapCircles(frame)
            ]);

    private static Relation RadialCrescentCircularBodyPositiveAreaFrame(FrameBasis frame)
    {
        IElement[] participants =
        [
            GraphicStructure.AreaFill(FrameCircle(frame, 0, 0, 92)),
            .. RadialCrescentCircularBodyArmFills(frame),
            .. RadialCrescentCircularBodyEndpointCapFills(frame)
        ];
        return GraphicAreaParticipationSurface.AreaFrame(new Carrier(), participants);
    }

    private static Relation RadialCrescentExteriorEndpointMarkers(IElement walkedOutline)
    {
        var elements = GraphicReadGraph.From(walkedOutline)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => SourceId(boundary.Source))
            .SelectMany(boundary => boundary.Segments.SelectMany(segment => new[] { segment.Start, segment.End }))
            .DistinctBy(FramePointKey)
            .Select(point => ExteriorEndpointMarker(point, 7))
            .Cast<IElement>()
            .ToArray();
        return elements.Length == 0 ? TensionSlot() : Group(elements);
    }

    private static IElement ExteriorEndpointMarker(SharedFramePointRead point, long radius)
    {
        if (!TryReadFramePoint(point, out var read))
        {
            return point.Source;
        }

        return GraphicStructure.FrameCircle(
            (Relation)point.Source,
            CreateFramePoint(read.Template, read.Point + new Point2(radius, 0)));
    }

    private static Relation RadialCrescentExplodedOutlineSections(IElement walkedOutline)
    {
        var graph = GraphicReadGraph.From(walkedOutline);
        var sectionBySpanSource = graph.ReadsOf<SharedFrameArcSectionRead>()
            .GroupBy(section => SourceId(section.BaseArc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        var arcBySpanSource = graph.ReadsOf<SharedFrameArcRead>()
            .GroupBy(arc => SourceId(arc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        var elements = new List<IElement>();
        foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>()
                     .DistinctBy(boundary => SourceId(boundary.Source)))
        {
            if (boundary.Segments.Count == 0 &&
                boundary.Circles is { Count: > 0 })
            {
                elements.AddRange(boundary.Circles
                    .DistinctBy(circle => SourceId(circle.Source))
                    .Select(ExplodedCircle));
                continue;
            }

            foreach (var edge in WalkedBoundaryEdges(boundary))
            {
                var spanSourceId = SourceId(edge.Segment.Source);
                if (sectionBySpanSource.TryGetValue(spanSourceId, out var section))
                {
                    elements.Add(ExplodedArcSection(edge, section));
                    continue;
                }

                if (arcBySpanSource.TryGetValue(spanSourceId, out var arc))
                {
                    elements.Add(ExplodedArc(edge, arc));
                    continue;
                }

                elements.Add(ExplodedSegment(edge));
            }
        }

        return elements.Count == 0
            ? TensionSlot()
            : Group([.. elements]);
    }

    private static IReadOnlyList<WalkedFrameEdge> WalkedBoundaryEdges(ClosedSharedFrameBoundaryRead boundary)
    {
        if (TryReadCoordinateBoundaryEdges(boundary, out var edges))
        {
            return edges;
        }

        return [.. boundary.Segments.Select(segment => new WalkedFrameEdge(segment, segment.Start, segment.End))];
    }

    private static bool TryReadCoordinateBoundaryEdges(
        ClosedSharedFrameBoundaryRead boundary,
        out IReadOnlyList<WalkedFrameEdge> edges)
    {
        edges = [];
        if (boundary.Segments.Count == 0 ||
            !TryPointKey(boundary.Segments[0].Start, out var startKey) ||
            !TryPointKey(boundary.Segments[0].End, out var currentKey))
        {
            return false;
        }

        var unused = boundary.Segments.Skip(1).ToList();
        var ordered = new List<WalkedFrameEdge>
        {
            new(boundary.Segments[0], boundary.Segments[0].Start, boundary.Segments[0].End)
        };

        while (unused.Count > 0)
        {
            var nextIndex = unused.FindIndex(segment =>
                TryPointKey(segment.Start, out var segmentStartKey) && segmentStartKey == currentKey ||
                TryPointKey(segment.End, out var segmentEndKey) && segmentEndKey == currentKey);
            if (nextIndex < 0)
            {
                return false;
            }

            var next = unused[nextIndex];
            unused.RemoveAt(nextIndex);
            if (TryPointKey(next.Start, out var nextStartKey) &&
                nextStartKey == currentKey &&
                TryPointKey(next.End, out var nextEndKey))
            {
                ordered.Add(new WalkedFrameEdge(next, next.Start, next.End));
                currentKey = nextEndKey;
                continue;
            }

            if (TryPointKey(next.Start, out var reverseEndKey))
            {
                ordered.Add(new WalkedFrameEdge(next, next.End, next.Start));
                currentKey = reverseEndKey;
            }
        }

        if (currentKey != startKey)
        {
            return false;
        }

        edges = ordered;
        return true;
    }

    private static IElement ExplodedSegment(WalkedFrameEdge edge)
    {
        if (!TryReadFramePoint(edge.Start, out var start) ||
            !TryReadFramePoint(edge.End, out var end))
        {
            return edge.Segment.Source;
        }

        var shift = RadialCrescentExplosionShift((start.Point + end.Point) * 0.5d);
        return GraphicStructure.FrameSegment(
            CreateFramePoint(start.Template, start.Point + shift),
            CreateFramePoint(end.Template, end.Point + shift));
    }

    private static IElement ExplodedArcSection(WalkedFrameEdge edge, SharedFrameArcSectionRead section)
    {
        if (!TryReadFramePoint(edge.Start, out var start) ||
            !TryReadFramePoint(edge.End, out var end) ||
            !TryReadFramePoint(section.Center, out var center) ||
            !TryReadFramePoint(section.Through, out var through))
        {
            return ExplodedSegment(edge);
        }

        var shift = RadialCrescentExplosionShift(through.Point);
        return GraphicStructure.FrameArcSection(
            CreateFramePoint(center.Template, center.Point + shift),
            CreateFramePoint(start.Template, start.Point + shift),
            CreateFramePoint(through.Template, through.Point + shift),
            CreateFramePoint(end.Template, end.Point + shift));
    }

    private static IElement ExplodedArc(WalkedFrameEdge edge, SharedFrameArcRead arc)
    {
        if (!TryReadFramePoint(edge.Start, out var start) ||
            !TryReadFramePoint(edge.End, out var end) ||
            !TryReadFramePoint(arc.Center, out var center))
        {
            return ExplodedSegment(edge);
        }

        var throughPoint = ArcMidPoint(center.Point, start.Point, end.Point);
        var shift = RadialCrescentExplosionShift(throughPoint);
        return GraphicStructure.FrameArcSection(
            CreateFramePoint(center.Template, center.Point + shift),
            CreateFramePoint(start.Template, start.Point + shift),
            CreateFramePoint(start.Template, throughPoint + shift),
            CreateFramePoint(end.Template, end.Point + shift));
    }

    private static IElement ExplodedCircle(SharedFrameCircleRead circle)
    {
        if (!TryReadFramePoint(circle.Center, out var center) ||
            !TryReadFramePoint(circle.RadiusPoint, out var radiusPoint))
        {
            return circle.Source;
        }

        var shift = RadialCrescentExplosionShift(center.Point);
        return GraphicStructure.FrameCircle(
            CreateFramePoint(center.Template, center.Point + shift),
            CreateFramePoint(radiusPoint.Template, radiusPoint.Point + shift));
    }

    private static Point2 RadialCrescentExplosionShift(Point2 midPoint)
    {
        var fromCenter = midPoint - new Point2(0d, 0d);
        var length = Math.Sqrt(fromCenter.X * fromCenter.X + fromCenter.Y * fromCenter.Y);
        if (length < 0.001d)
        {
            return new Point2(0d, 18d);
        }

        var distance = Math.Clamp(10d + length * 0.18d, 18d, 44d);
        return fromCenter * (distance / length);
    }

    private static Point2 ArcMidPoint(Point2 center, Point2 start, Point2 end)
    {
        var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
        var endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);
        var sweep = endAngle - startAngle;
        while (sweep <= -Math.PI)
        {
            sweep += Math.PI * 2d;
        }

        while (sweep > Math.PI)
        {
            sweep -= Math.PI * 2d;
        }

        var radius = Math.Sqrt(Math.Pow(start.X - center.X, 2d) + Math.Pow(start.Y - center.Y, 2d));
        var midAngle = startAngle + sweep * 0.5d;
        return new Point2(
            center.X + Math.Cos(midAngle) * radius,
            center.Y + Math.Sin(midAngle) * radius);
    }

    private static bool TryReadFramePoint(SharedFramePointRead point, out WalkedFramePoint read)
    {
        if (point.Coordinates.Count >= 2)
        {
            read = new WalkedFramePoint(
                point,
                new Point2(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks));
            return true;
        }

        read = default;
        return false;
    }

    private static Relation CreateFramePoint(SharedFramePointRead template, Point2 point)
    {
        var coordinates = template.Coordinates
            .Select((coordinate, index) => new CarrierLocation(
                coordinate.Carrier,
                index == 0 ? Rounded(point.X) : index == 1 ? Rounded(point.Y) : coordinate.Ticks))
            .ToArray();
        return GraphicStructure.FramePoint(template.Holder, coordinates);
    }

    private static bool TryPointKey(SharedFramePointRead point, out string key)
    {
        if (point.Coordinates.Count >= 2)
        {
            key = $"{point.Coordinates[0].Ticks}:{point.Coordinates[1].Ticks}";
            return true;
        }

        key = "";
        return false;
    }

    private static string FramePointKey(SharedFramePointRead point) =>
        point.Coordinates.Count >= 2
            ? $"{point.Coordinates[0].Ticks}:{point.Coordinates[1].Ticks}"
            : SourceId(point.Source).ToString();

    private static long Rounded(double value) =>
        (long)Math.Round(value, MidpointRounding.AwayFromZero);

    private static ulong SourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

    private static BlocksGlossaryStructure DiamondCuspSource(FrameBasis frame)
    {
        var stage = DiamondCuspGlossaryGeometry.CreateStage(roundOuter: false, roundInner: false);
        return Spec(
            Group(
                stage.Outer.Outline,
                stage.Inner.Outline,
                Group([.. stage.Outer.Joins]),
                Group([.. stage.Inner.Joins]),
                Group([.. stage.Outer.AngleBases]),
                Group([.. stage.Inner.AngleBases])),
            "OuterBoundary + InnerBoundary; Join(Pi) for each shared endpoint",
            "The source has two inspectable closed boundaries: the outer diamond-like support and an inner flat-bottom arc boundary. Both are built from Gojo sections and shared endpoint joins.",
            DiamondCuspBounds());
    }

    private static BlocksGlossaryStructure DiamondCuspOuterSelection(FrameBasis frame)
    {
        var stage = DiamondCuspGlossaryGeometry.CreateStage(roundOuter: false, roundInner: false);
        var selection = DiamondCuspSelectionRead(stage.Outer.Outline);
        return Spec(
            Group(stage.Outer.Outline, stage.Inner.Outline, Group([.. stage.Outer.AngleBases]), selection),
            "CuspCandidateDomain(outer) -> Selection(4)",
            "The first selection pass reads the four outer cusp sites from the outer closed boundary. The inner boundary remains present, but it is not part of this selection.",
            DiamondCuspBounds());
    }

    private static BlocksGlossaryStructure DiamondCuspOuterRounded(FrameBasis frame)
    {
        var source = DiamondCuspGlossaryGeometry.CreateStage(roundOuter: false, roundInner: false);
        var roundedStage = DiamondCuspGlossaryGeometry.CreateStage(roundOuter: true, roundInner: false);
        var selection = DiamondCuspSelectionRead(source.Outer.Outline);
        var round = DiamondCuspRoundAction(selection, source.Outer.Outline);
        return Spec(
            Group(source.Outer.Outline, source.Inner.Outline, selection, round, roundedStage.Outer.Outline),
            "OuterSelection + carrier tangents -> RoundedOuterBoundary",
            "The first rounding rung preserves the inner boundary and emits a new outer boundary. Retained arc pieces stay arcs, and each new outer corner curve is solved from adjacent carrier tangents.",
            DiamondCuspBounds());
    }

    private static BlocksGlossaryStructure DiamondCuspInnerSelection(FrameBasis frame)
    {
        var stage = DiamondCuspGlossaryGeometry.CreateStage(roundOuter: true, roundInner: false);
        var selection = DiamondCuspSelectionRead(stage.Inner.Outline);
        return Spec(
            Group(stage.Outer.Outline, stage.Inner.Outline, Group([.. stage.Inner.AngleBases]), selection),
            "CuspCandidateDomain(inner) -> Selection(2)",
            "The second selection pass runs on the inner flat-bottom arc boundary. Its two selected sites are the shared endpoints where the arc carrier meets the flat segment.",
            DiamondCuspBounds());
    }

    private static BlocksGlossaryStructure DiamondCuspInnerRounded(FrameBasis frame)
    {
        var source = DiamondCuspGlossaryGeometry.CreateStage(roundOuter: true, roundInner: false);
        var roundedStage = DiamondCuspGlossaryGeometry.CreateStage(roundOuter: true, roundInner: true);
        var selection = DiamondCuspSelectionRead(source.Inner.Outline);
        var round = DiamondCuspRoundAction(selection, source.Inner.Outline);
        return Spec(
            Group(source.Outer.Outline, source.Inner.Outline, selection, round, roundedStage.Inner.Outline),
            "InnerSelection + carrier tangents -> RoundedInnerBoundary",
            "The inner rounding rung trims the flat segment and arc carrier, then connects the retained pieces with tangent-derived curve sections. The retained upper section remains a Gojo arc.",
            DiamondCuspBounds());
    }

    private static BlocksGlossaryStructure DiamondCuspResult(FrameBasis frame)
    {
        var stage = DiamondCuspGlossaryGeometry.CreateStage(roundOuter: true, roundInner: true);
        return Spec(
            Group(
                stage.Outer.Outline,
                stage.Inner.Outline,
                GraphicStructure.AreaFill(stage.Outer.Outline),
                GraphicStructure.AntiAreaFill(stage.Inner.Outline)),
            "RoundedOuterBoundary + AntiAreaFill(RoundedInnerBoundary)",
            "The final face is a positive fill from the rounded outer boundary with the rounded inner boundary participating as a cutout. Both boundaries remain inspectable Gojo structure.",
            DiamondCuspBounds());
    }

    private static Relation DiamondCuspSelectionRead(IElement outline)
    {
        var domain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), outline, witnessRadiusTicks: 5);
        var pattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        return GraphicSelectionSurface.Search(new Carrier(), outline, domain, pattern);
    }

    private static Relation DiamondCuspRoundAction(IElement selection, IElement outline) =>
        GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(new Carrier(), selection, outline, radiusTicks: 12);

    private static Relation Chain(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return TensionSlot();
        }

        IElement structure = elements[^1];
        for (var index = elements.Count - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }

    private static SKRect DiamondCuspBounds() =>
        Bounds(-92, -92, 92, 84);

    private static SKRect PcbDataBounds() =>
        Bounds(-116, -76, 116, 76);

    private static BlocksGlossaryStructure RadialCrescentRoundingInteriorCusps(FrameBasis frame)
    {
        var exterior = RadialCrescentExteriorSharpBoundaries(frame);
        var holes = RadialCrescentInteriorHoles(frame, rounded: false);
        var cutHoles = RadialCrescentInteriorCutOutlines(frame, rounded: false);
        var areaUnion = RadialCrescentAreaUnion(frame, exterior, holes);
        var outline = RadialCrescentOutlineFrame(exterior, cutHoles);
        var cutRead = RadialCrescentInteriorOutlineCutRead(frame, holes, cutHoles);
        var cusps = RadialCrescentInteriorCusps(frame, outline);
        return Spec(
            Group(areaUnion, cutRead, GraphicOutlineSurface.FromArea(new Carrier(), areaUnion, outline), Group(cusps)),
            "OutlineRead(areaUnion) + CutRead(interior holes) + CuspSelection(24 sites)",
            "The selected sites live on the cut interior outline carriers: 8 sites on the crescent body openings and 16 on the diamond body openings. This keeps rounding attached to the outline, not the filled face.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentRoundingInteriorRounded(FrameBasis frame)
    {
        var exterior = RadialCrescentExteriorSharpBoundaries(frame);
        var holes = RadialCrescentInteriorCutOutlines(frame, rounded: false);
        var roundedHoles = RadialCrescentInteriorCutOutlines(frame, rounded: true);
        var outline = RadialCrescentOutlineFrame(exterior, holes);
        var roundedOutline = RadialCrescentOutlineFrame(exterior, roundedHoles);
        var cusps = RadialCrescentInteriorCusps(frame, outline);
        return Spec(
            new Relation(Group(outline, Group(cusps)), roundedOutline),
            "InteriorCuspSelection -> RoundedOutline(interior)",
            "Only the interior outline carriers are replaced here. The exterior perimeter remains the same evidence for the next selection pass.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentRoundingExteriorCusps(FrameBasis frame)
    {
        var exterior = RadialCrescentExteriorSharpBoundaries(frame);
        var roundedHoles = RadialCrescentInteriorCutOutlines(frame, rounded: true);
        var outline = RadialCrescentOutlineFrame(exterior, roundedHoles);
        var cusps = RadialCrescentExteriorCusps(frame, outline);
        return Spec(
            Group(outline, Group(cusps)),
            "Rounded interior outline + CuspSelection(exterior outlines)",
            "The second selection pass runs on the exterior outline carriers after the interior holes have already been rounded.",
            RadialCrescentBounds());
    }

    private static BlocksGlossaryStructure RadialCrescentRoundingFinalRounded(FrameBasis frame)
    {
        var exterior = RadialCrescentExteriorRoundedBoundaries(frame);
        var holes = RadialCrescentInteriorCutOutlines(frame, rounded: true);
        var areaUnion = RadialCrescentAreaUnion(frame, exterior, holes);
        var outline = RadialCrescentOutlineFrame(exterior, holes);
        return Spec(
            Group(areaUnion, GraphicOutlineSurface.FromArea(new Carrier(), areaUnion, outline)),
            "Rounded exterior + rounded interior -> AreaUnion -> OutlineRead",
            "The final display can be filled, but the rung is still defined by the rounded exterior and interior outline carriers.",
            RadialCrescentBounds());
    }

    private static Relation RadialCrescentAreaUnion(FrameBasis frame, IReadOnlyList<IElement> exterior, IReadOnlyList<IElement> holes)
    {
        var areaFrame = GraphicAreaParticipationSurface.AreaFrame(
            new Carrier(),
            GraphicStructure.AreaFill([.. exterior]),
            GraphicStructure.AntiAreaFill([.. holes]));
        return GraphicAreaParticipationSurface.AreaUnion(
            new Carrier(),
            areaFrame,
            RadialCrescentOutlineFrame(exterior, holes));
    }

    private static Relation RadialCrescentOutlineFrame(IReadOnlyList<IElement> exterior, IReadOnlyList<IElement> holes) =>
        GraphicOutlineSurface.OutlineFrame(
            new Carrier(),
            LoopRead(exterior, 420, clockwise: false),
            LoopRead(holes, 220, clockwise: true));

    private static Relation RadialCrescentInteriorOutlineCutRead(
        FrameBasis frame,
        IReadOnlyList<IElement> sourceHoles,
        IReadOnlyList<IElement> cutHoles)
    {
        var source = Group([.. sourceHoles]);
        var cutLoops = RadialCrescentInteriorCutOutlineLoops(frame, rounded: false);
        var cutPoints = RadialCrescentInteriorCuspPoints(frame);
        var cutCarrier = new Carrier(UnitShape.DescribedAtom(new Relation(source, Group([.. cutPoints]))));
        var cutSites = cutPoints
            .Select((point, index) => new Relation(point, GraphicStructure.Point(cutCarrier, index + 1)))
            .ToArray();
        var loopReads = cutLoops
            .Select(loop => LoopRead(loop, Math.Max(24, loop.Length * 12), clockwise: true))
            .ToArray();
        return new Relation(
            Group(source, Group([.. cutSites])),
            Group(
                Group([.. cutHoles]),
                Group([.. loopReads]),
                GraphicStructure.Segment(cutCarrier, 0, cutSites.Length),
                ResolutionEvidence(cutCarrier, 1)));
    }

    private static IElement[] RadialCrescentCenterStrokes(FrameBasis frame)
    {
        var leftArc = GraphicStructure.FrameArc(frame.Point(-240, 0), frame.Point(-165, 108), frame.Point(-165, -108));
        var rightArc = GraphicStructure.FrameArc(frame.Point(100, 0), frame.Point(25, -108), frame.Point(25, 108));
        var leftTop = frame.Point(173, 122);
        var leftKnee = frame.Point(247, -6);
        var leftLower = frame.Point(133, -88);
        var rightTop = frame.Point(357, 122);
        var rightKnee = frame.Point(283, -6);
        var rightLower = frame.Point(397, -88);
        return
        [
            leftArc,
            rightArc,
            GraphicStructure.FrameSegment(leftTop, leftKnee),
            GraphicStructure.FrameSegment(leftKnee, leftLower),
            GraphicStructure.FrameSegment(rightTop, rightKnee),
            GraphicStructure.FrameSegment(rightKnee, rightLower)
        ];
    }

    private static IElement[] RadialCrescentTrimmedCenterStrokes(FrameBasis frame)
    {
        var leftArc = GraphicStructure.FrameArc(frame.Point(-240, 0), frame.Point(-155, 92), frame.Point(-155, -92));
        var rightArc = GraphicStructure.FrameArc(frame.Point(100, 0), frame.Point(15, -92), frame.Point(15, 92));
        var leftTop = frame.Point(182, 106);
        var leftKnee = frame.Point(247, -6);
        var leftLower = frame.Point(148, -77);
        var rightTop = frame.Point(348, 106);
        var rightKnee = frame.Point(283, -6);
        var rightLower = frame.Point(382, -77);
        return
        [
            leftArc,
            rightArc,
            GraphicStructure.FrameSegment(leftTop, leftKnee),
            GraphicStructure.FrameSegment(leftKnee, leftLower),
            GraphicStructure.FrameSegment(rightTop, rightKnee),
            GraphicStructure.FrameSegment(rightKnee, rightLower)
        ];
    }

    private static IElement[] RadialCrescentBodyBoundaries(FrameBasis frame) =>
    [
        FrameCircle(frame, -70, 0, 78),
        .. DiamondSections(frame, 265, 0, 76, 88)
    ];

    private static IElement[] RadialCrescentEndpointCaps(FrameBasis frame, long outerRadius) =>
    [
        FrameCircle(frame, -165, 108, outerRadius),
        FrameCircle(frame, -165, -108, outerRadius),
        FrameCircle(frame, 25, -108, outerRadius),
        FrameCircle(frame, 25, 108, outerRadius),
        FrameCircle(frame, 173, 122, outerRadius),
        FrameCircle(frame, 133, -88, outerRadius),
        FrameCircle(frame, 357, 122, outerRadius),
        FrameCircle(frame, 397, -88, outerRadius)
    ];

    private static Relation[] RadialCrescentCircularBodyEndpointCapFills(FrameBasis frame) =>
    [
        .. RadialCrescentCircularBodyCapCircles(frame)
            .Select(circle => GraphicStructure.AreaFill(circle))
    ];

    private static Relation[] RadialCrescentCircularBodyArmFills(FrameBasis frame) =>
    [
        .. RadialCrescentCircularBodyArmSections(frame)
            .Chunk(4)
            .Select(sections => GraphicStructure.AreaFill(sections))
    ];

    private static IElement[] RadialCrescentCircularBodyArmSections(FrameBasis frame) =>
    [
        .. RadialCrescentArmRectangleSections(frame, -132, 108, 0, 0, 30),
        .. RadialCrescentArmRectangleSections(frame, 132, 108, 0, 0, 30),
        .. RadialCrescentArmRectangleSections(frame, 132, -108, 0, 0, 30),
        .. RadialCrescentArmRectangleSections(frame, -132, -108, 0, 0, 30)
    ];

    private static IElement[] RadialCrescentCircularBodyCapCircles(FrameBasis frame) =>
    [
        FrameCircle(frame, -132, 108, 34),
        FrameCircle(frame, 132, 108, 34),
        FrameCircle(frame, 132, -108, 34),
        FrameCircle(frame, -132, -108, 34)
    ];

    private static IElement[] RadialCrescentArmRectangleSections(
        FrameBasis frame,
        double startX,
        double startY,
        double endX,
        double endY,
        double width)
    {
        var dx = endX - startX;
        var dy = endY - startY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001d)
        {
            return [];
        }

        var normalX = -dy / length;
        var normalY = dx / length;
        var half = width * 0.5d;
        var a = frame.Point(Rounded(startX + normalX * half), Rounded(startY + normalY * half));
        var b = frame.Point(Rounded(endX + normalX * half), Rounded(endY + normalY * half));
        var c = frame.Point(Rounded(endX - normalX * half), Rounded(endY - normalY * half));
        var d = frame.Point(Rounded(startX - normalX * half), Rounded(startY - normalY * half));
        return
        [
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d),
            GraphicStructure.FrameSegment(d, a)
        ];
    }

    private static IElement[] RadialCrescentCircleSections(FrameBasis frame, long centerX, long centerY, long radius)
    {
        var center = frame.Point(centerX, centerY);
        var right = frame.Point(centerX + radius, centerY);
        var top = frame.Point(centerX, centerY + radius);
        var left = frame.Point(centerX - radius, centerY);
        var bottom = frame.Point(centerX, centerY - radius);
        return
        [
            GraphicStructure.FrameArc(center, right, top),
            GraphicStructure.FrameArc(center, top, left),
            GraphicStructure.FrameArc(center, left, bottom),
            GraphicStructure.FrameArc(center, bottom, right)
        ];
    }

    private static IElement[] RadialCrescentExteriorSharpBoundaries(FrameBasis frame) =>
    [
        FrameCircle(frame, -70, 0, 88),
        .. RadialCrescentArcBandSections(frame, -240, 0, -159, 116, -159, -116, -171, -100, -171, 100),
        .. RadialCrescentArcBandSections(frame, 100, 0, 19, -116, 19, 116, 31, 100, 31, -100),
        .. PolylineBandSections(frame, [(165, 117), (239, -11), (126, -93), (140, -83), (256, -1), (181, 127)]),
        .. PolylineBandSections(frame, [(349, 127), (274, -1), (390, -83), (404, -93), (291, -11), (365, 117)]),
        .. DiamondSections(frame, 265, 0, 86, 98),
        .. RadialCrescentEndpointCaps(frame, 28)
    ];

    private static IElement[] RadialCrescentExteriorRoundedBoundaries(FrameBasis frame) =>
    [
        FrameCircle(frame, -70, 0, 88),
        .. RadialCrescentArcBandSections(frame, -240, 0, -157, 116, -157, -116, -173, -100, -173, 100),
        .. RadialCrescentArcBandSections(frame, 100, 0, 17, -116, 17, 116, 33, 100, 33, -100),
        .. RoundedPolylineBandSections(frame, [(165, 117), (239, -11), (126, -93), (140, -83), (256, -1), (181, 127)]),
        .. RoundedPolylineBandSections(frame, [(349, 127), (274, -1), (390, -83), (404, -93), (291, -11), (365, 117)]),
        .. RoundedDiamondSections(frame, 265, 0, 89, 101, 14),
        .. RadialCrescentEndpointCaps(frame, 28)
    ];

    private static IElement[] RadialCrescentInteriorHoles(FrameBasis frame, bool rounded) =>
    [
        FrameCircle(frame, -70, 0, 68),
        .. (rounded ? RoundedDiamondSections(frame, 265, 0, 56, 68, 12) : DiamondSections(frame, 265, 0, 56, 68)),
        .. RadialCrescentEndpointCaps(frame, 8)
    ];

    private static IElement[] RadialCrescentInteriorCutOutlines(FrameBasis frame, bool rounded) =>
    [
        .. RadialCrescentInteriorCutOutlineLoops(frame, rounded).SelectMany(loop => loop),
        .. RadialCrescentEndpointCaps(frame, 8)
    ];

    private static IElement[][] RadialCrescentInteriorCutOutlineLoops(FrameBasis frame, bool rounded)
    {
        var loops = RadialCrescentGlossaryGeometry.InteriorCuspPointGroups();
        return
        [
            .. loops.Select(points => rounded
                ? RoundedClosedSections(frame, points, radius: 8)
                : ClosedSections(frame, points))
        ];
    }

    private static IElement[] RadialCrescentInteriorCusps(FrameBasis frame, IElement source) =>
        CuspRelations(
            frame,
            source,
            [.. RadialCrescentGlossaryGeometry.InteriorCuspPointGroups().SelectMany(group => group)]);

    private static IElement[] RadialCrescentExteriorCusps(FrameBasis frame, IElement source) =>
        CuspRelations(frame, source, [(-165, 108), (-165, -108), (25, -108), (25, 108), (173, 122), (133, -88), (357, 122), (397, -88)]);

    private static IElement[] CuspRelations(FrameBasis frame, IElement source, IReadOnlyList<(long X, long Y)> points)
    {
        var carrier = new Carrier(UnitShape.DescribedAtom(source));
        var result = new List<IElement>();
        for (var index = 0; index < points.Count; index++)
        {
            result.Add(new Relation(frame.Point(points[index].X, points[index].Y), GraphicStructure.Point(carrier, index + 1)));
        }

        return [.. result];
    }

    private static Relation[] RadialCrescentInteriorCuspPoints(FrameBasis frame) =>
        [.. RadialCrescentGlossaryGeometry.InteriorCuspPointGroups()
            .SelectMany(group => group)
            .Select(point => frame.Point(point.X, point.Y))];

    private static IElement[] ClosedSections(FrameBasis frame, IReadOnlyList<(long X, long Y)> points) =>
        ClosedSegments([.. points.Select(point => frame.Point(point.X, point.Y))]);

    private static IElement[] RoundedClosedSections(FrameBasis frame, IReadOnlyList<(long X, long Y)> points, long radius)
    {
        if (points.Count < 3)
        {
            return ClosedSections(frame, points);
        }

        var sections = new List<IElement>();
        for (var index = 0; index < points.Count; index++)
        {
            var previous = points[(index - 1 + points.Count) % points.Count];
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            var incoming = PullToward(current, previous, radius);
            var outgoing = PullToward(current, next, radius);
            var incomingPoint = frame.Point(incoming.X, incoming.Y);
            var currentPoint = frame.Point(current.X, current.Y);
            var outgoingPoint = frame.Point(outgoing.X, outgoing.Y);
            sections.Add(GraphicStructure.FrameQuadraticCurve(incomingPoint, currentPoint, outgoingPoint));

            var nextIncoming = PullToward(next, current, radius);
            sections.Add(GraphicStructure.FrameSegment(outgoingPoint, frame.Point(nextIncoming.X, nextIncoming.Y)));
        }

        return [.. sections];
    }

    private static (long X, long Y) PullToward((long X, long Y) from, (long X, long Y) to, long distance)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001d)
        {
            return from;
        }

        return (
            from.X + (long)Math.Round(dx / length * distance),
            from.Y + (long)Math.Round(dy / length * distance));
    }

    private static IElement[] RadialCrescentArcBandSections(
        FrameBasis frame,
        long centerX,
        long centerY,
        long outerStartX,
        long outerStartY,
        long outerEndX,
        long outerEndY,
        long innerStartX,
        long innerStartY,
        long innerEndX,
        long innerEndY)
    {
        var outerStart = frame.Point(outerStartX, outerStartY);
        var outerEnd = frame.Point(outerEndX, outerEndY);
        var innerStart = frame.Point(innerStartX, innerStartY);
        var innerEnd = frame.Point(innerEndX, innerEndY);
        var center = frame.Point(centerX, centerY);
        return
        [
            GraphicStructure.FrameArc(center, outerStart, outerEnd),
            GraphicStructure.FrameSegment(outerEnd, innerStart),
            GraphicStructure.FrameArc(center, innerStart, innerEnd),
            GraphicStructure.FrameSegment(innerEnd, outerStart)
        ];
    }

    private static IElement[] PolylineBandSections(FrameBasis frame, IReadOnlyList<(long X, long Y)> points)
    {
        var relations = points.Select(point => frame.Point(point.X, point.Y)).ToArray();
        return ClosedSegments(relations);
    }

    private static IElement[] RoundedPolylineBandSections(FrameBasis frame, IReadOnlyList<(long X, long Y)> points)
    {
        if (points.Count < 3)
        {
            return PolylineBandSections(frame, points);
        }

        var sections = new List<IElement>();
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            var after = points[(index + 2) % points.Count];
            var a = frame.Point(current.X, current.Y);
            var b = frame.Point(
                next.X + (after.X - next.X) / 5,
                next.Y + (after.Y - next.Y) / 5);
            var c = frame.Point(next.X, next.Y);
            sections.Add(GraphicStructure.FrameSegment(a, b));
            sections.Add(GraphicStructure.FrameQuadraticCurve(b, c, frame.Point(
                next.X + (current.X - next.X) / 5,
                next.Y + (current.Y - next.Y) / 5)));
        }

        return [.. sections];
    }

    private static IElement[] DiamondSections(FrameBasis frame, long centerX, long centerY, long halfWidth, long halfHeight)
    {
        var top = frame.Point(centerX, centerY + halfHeight);
        var right = frame.Point(centerX + halfWidth, centerY);
        var bottom = frame.Point(centerX, centerY - halfHeight);
        var left = frame.Point(centerX - halfWidth, centerY);
        return
        [
            GraphicStructure.FrameSegment(top, right),
            GraphicStructure.FrameSegment(right, bottom),
            GraphicStructure.FrameSegment(bottom, left),
            GraphicStructure.FrameSegment(left, top)
        ];
    }

    private static IElement[] RoundedDiamondSections(FrameBasis frame, long centerX, long centerY, long halfWidth, long halfHeight, long radius)
    {
        var topA = frame.Point(centerX + radius, centerY + halfHeight - radius);
        var topB = frame.Point(centerX - radius, centerY + halfHeight - radius);
        var rightA = frame.Point(centerX + halfWidth - radius, centerY - radius);
        var rightB = frame.Point(centerX + halfWidth - radius, centerY + radius);
        var bottomA = frame.Point(centerX - radius, centerY - halfHeight + radius);
        var bottomB = frame.Point(centerX + radius, centerY - halfHeight + radius);
        var leftA = frame.Point(centerX - halfWidth + radius, centerY + radius);
        var leftB = frame.Point(centerX - halfWidth + radius, centerY - radius);
        return
        [
            GraphicStructure.FrameSegment(topA, rightB),
            GraphicStructure.FrameQuadraticCurve(rightB, frame.Point(centerX + halfWidth, centerY), rightA),
            GraphicStructure.FrameSegment(rightA, bottomB),
            GraphicStructure.FrameQuadraticCurve(bottomB, frame.Point(centerX, centerY - halfHeight), bottomA),
            GraphicStructure.FrameSegment(bottomA, leftB),
            GraphicStructure.FrameQuadraticCurve(leftB, frame.Point(centerX - halfWidth, centerY), leftA),
            GraphicStructure.FrameSegment(leftA, topB),
            GraphicStructure.FrameQuadraticCurve(topB, frame.Point(centerX, centerY + halfHeight), topA)
        ];
    }

    private static IElement FrameCircle(FrameBasis frame, long centerX, long centerY, long radius) =>
        GraphicStructure.FrameCircle(frame.Point(centerX, centerY), frame.Point(centerX + radius, centerY));

    private static SKRect RadialCrescentBounds() =>
        Bounds(-270, -150, 430, 150);

    private static SKRect RadialCrescentCircularBodyBounds() =>
        Bounds(-190, -160, 190, 160);

    private static SKRect RadialCrescentCircularBodyExplodedBounds() =>
        Bounds(-210, -180, 210, 180);

    private static IElement[] DroneClipOuterSections(FrameBasis frame)
    {
        var points = new[]
        {
            frame.Point(-52, -12),
            frame.Point(-39, -20),
            frame.Point(-12, -20),
            frame.Point(-7, -26),
            frame.Point(7, -26),
            frame.Point(12, -20),
            frame.Point(39, -20),
            frame.Point(52, -12),
            frame.Point(52, 12),
            frame.Point(39, 20),
            frame.Point(12, 20),
            frame.Point(7, 26),
            frame.Point(-7, 26),
            frame.Point(-12, 20),
            frame.Point(-39, 20),
            frame.Point(-52, 12)
        };
        return ClosedSegments(points);
    }

    private static IElement[] DroneClipRoundedOuterSections(FrameBasis frame) =>
    [
        GraphicStructure.FrameSegment(frame.Point(-37, -20), frame.Point(-13, -20)),
        GraphicStructure.FrameQuadraticCurve(frame.Point(-13, -20), frame.Point(-9, -25), frame.Point(-4, -25)),
        GraphicStructure.FrameSegment(frame.Point(-4, -25), frame.Point(4, -25)),
        GraphicStructure.FrameQuadraticCurve(frame.Point(4, -25), frame.Point(9, -25), frame.Point(13, -20)),
        GraphicStructure.FrameSegment(frame.Point(13, -20), frame.Point(37, -20)),
        GraphicStructure.FrameQuadraticCurve(frame.Point(37, -20), frame.Point(52, -20), frame.Point(52, -12)),
        GraphicStructure.FrameSegment(frame.Point(52, -12), frame.Point(52, 12)),
        GraphicStructure.FrameQuadraticCurve(frame.Point(52, 12), frame.Point(52, 20), frame.Point(37, 20)),
        GraphicStructure.FrameSegment(frame.Point(37, 20), frame.Point(13, 20)),
        GraphicStructure.FrameQuadraticCurve(frame.Point(13, 20), frame.Point(9, 25), frame.Point(4, 25)),
        GraphicStructure.FrameSegment(frame.Point(4, 25), frame.Point(-4, 25)),
        GraphicStructure.FrameQuadraticCurve(frame.Point(-4, 25), frame.Point(-9, 25), frame.Point(-13, 20)),
        GraphicStructure.FrameSegment(frame.Point(-13, 20), frame.Point(-37, 20)),
        GraphicStructure.FrameQuadraticCurve(frame.Point(-37, 20), frame.Point(-52, 20), frame.Point(-52, 12)),
        GraphicStructure.FrameSegment(frame.Point(-52, 12), frame.Point(-52, -12)),
        GraphicStructure.FrameQuadraticCurve(frame.Point(-52, -12), frame.Point(-52, -20), frame.Point(-37, -20))
    ];

    private static IElement[] DroneClipHoleBoundaries(FrameBasis frame)
    {
        var left = GraphicStructure.FrameCircle(frame.Point(-37, 0), frame.Point(-34, 0));
        var right = GraphicStructure.FrameCircle(frame.Point(37, 0), frame.Point(40, 0));
        return [left, right];
    }

    private static IElement[] DroneClipCuspIntersections(FrameBasis frame, IElement source)
    {
        var points = new[]
        {
            frame.Point(-52, -12),
            frame.Point(-7, -26),
            frame.Point(7, -26),
            frame.Point(52, -12),
            frame.Point(52, 12),
            frame.Point(7, 26),
            frame.Point(-7, 26),
            frame.Point(-52, 12),
            frame.Point(-29, -10),
            frame.Point(29, -10),
            frame.Point(29, 10),
            frame.Point(-29, 10)
        };
        var carrier = new Carrier(UnitShape.DescribedAtom(source));
        var result = new List<IElement>();
        for (var index = 0; index < points.Length; index++)
        {
            result.Add(new Relation(points[index], GraphicStructure.Point(carrier, index + 1)));
        }

        return [.. result];
    }

    private static IElement[] ClosedSegments(IReadOnlyList<Relation> points)
    {
        var sections = new List<IElement>();
        for (var index = 0; index < points.Count; index++)
        {
            sections.Add(GraphicStructure.FrameSegment(points[index], points[(index + 1) % points.Count]));
        }

        return [.. sections];
    }

    private static IElement Site()
    {
        var frame = new FrameBasis();
        return frame.Point(0, 0);
    }

    private static IElement LinearSites()
    {
        var frame = new FrameBasis();
        return Group(frame.Point(0, 0), frame.Point(0, 0));
    }

    private static IElement RadialFlow()
    {
        var carrier = new Carrier();
        var point = GraphicStructure.Point(carrier, 0);
        return Group(point, GraphicStructure.RadialFlow(point));
    }

    private static IElement LinearFlow()
    {
        var carrier = new Carrier();
        var segment = GraphicStructure.Segment(carrier, 0, 0);
        return Group(segment, GraphicStructure.RadialFlow(segment));
    }

    private static IElement LineSegment()
    {
        var frame = new FrameBasis();
        return GraphicStructure.FrameSegment(frame.Point(-44, 0), frame.Point(44, 0));
    }

    private static IElement PartitionedSegment()
    {
        var frame = new FrameBasis();
        return Group(
            GraphicStructure.FrameSegment(frame.Point(-46, 0), frame.Point(46, 0)),
            frame.Point(-24, 0),
            frame.Point(0, 0),
            frame.Point(24, 0));
    }

    private static IElement DirectedLine(bool inverse)
    {
        var carrier = new Carrier();
        return Group(
            GraphicStructure.Point(carrier, 0),
            inverse
                ? GraphicStructure.DirectedSegment(carrier, 40, -40)
                : GraphicStructure.DirectedSegment(carrier, -40, 40));
    }

    private static IElement DirectionOnLine(bool inverse)
    {
        var carrier = new Carrier();
        return Group(
            GraphicStructure.Point(carrier, 0),
            GraphicStructure.Point(carrier, inverse ? -32 : 32),
            inverse
                ? GraphicStructure.DirectedSegment(carrier, 32, -32)
                : GraphicStructure.DirectedSegment(carrier, -32, 32));
    }

    private static IElement CircleBoundary(bool includeRadius)
    {
        var frame = new FrameBasis();
        var center = frame.Point(0, 0);
        var radius = frame.Point(34, 0);
        return includeRadius
            ? Group(GraphicStructure.FrameCircle(center, radius), center, radius, GraphicStructure.Pin(center, radius))
            : GraphicStructure.FrameCircle(center, radius);
    }

    private static IElement PartitionedCircle()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        return Group(
            origin,
            circle,
            GraphicStructure.Point(circle, 0),
            GraphicStructure.Point(circle, 15),
            GraphicStructure.Point(circle, 30),
            GraphicStructure.Point(circle, 45),
            GraphicStructure.Point(circle, 60),
            GraphicStructure.Point(circle, 75),
            GraphicStructure.Point(circle, 90),
            GraphicStructure.Point(circle, 105),
            GraphicStructure.Point(circle, 120));
    }

    private static IElement DirectedCircle(bool inverse)
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        return Group(
            origin,
            GraphicStructure.Point(circle, 0),
            GraphicStructure.Point(circle, 24),
            GraphicStructure.Point(circle, 48),
            inverse
                ? GraphicStructure.DirectedSegment(circle, 44, 4)
                : GraphicStructure.DirectedSegment(circle, 4, 44));
    }

    private static IElement DirectionOnCircle(bool inverse)
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        return Group(
            origin,
            GraphicStructure.Point(circle, 0),
            GraphicStructure.Point(circle, inverse ? -32 : 32),
            inverse
                ? GraphicStructure.DirectedSegment(circle, 32, -32)
                : GraphicStructure.DirectedSegment(circle, -32, 32));
    }

    private static IElement CircleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var circle = GraphicStructure.FrameCircle(frame.Point(0, 0), frame.Point(34, 0));
        var area = inverse
            ? GraphicStructure.AntiAreaFill(circle)
            : GraphicStructure.AreaFill(circle);
        return Group(area, circle);
    }

    private static IElement DirectedCircleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var circle = GraphicStructure.FrameCircle(frame.Point(0, 0), frame.Point(34, 0));
        var area = inverse
            ? GraphicStructure.AntiAreaFill(circle)
            : GraphicStructure.AreaFill(circle);
        return Group(area, circle, DirectedCircle(inverse));
    }

    private static IElement BoundedCircleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var center = frame.Point(0, 0);
        var radius = frame.Point(34, 0);
        var circle = GraphicStructure.FrameCircle(center, radius);
        var area = inverse
            ? GraphicStructure.AntiAreaFill(circle)
            : GraphicStructure.AreaFill(circle);
        return Group(area, circle, center, radius, PartitionedCircle());
    }

    private static IElement RectangleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var boundary = RectangleSegments(frame, -34, -22, 34, 22);
        var area = inverse
            ? GraphicStructure.AntiAreaFill(boundary)
            : GraphicStructure.AreaFill(boundary);
        return Group(area, Group(boundary));
    }

    private static IElement DirectedLinearArea(bool inverse)
    {
        var frame = new FrameBasis();
        var boundary = RectangleSegments(frame, -34, -22, 34, 22);
        return Group(
            inverse ? GraphicStructure.AntiAreaFill(boundary) : GraphicStructure.AreaFill(boundary),
            Group(boundary),
            DirectedLine(inverse));
    }

    private static IElement BoundedRectangleArea(bool inverse)
    {
        var frame = new FrameBasis();
        var boundary = RectangleSegments(frame, -34, -22, 34, 22);
        return Group(
            inverse ? GraphicStructure.AntiAreaFill(boundary) : GraphicStructure.AreaFill(boundary),
            Group(boundary),
            frame.Point(-12, -22),
            frame.Point(12, -22),
            frame.Point(-12, 22),
            frame.Point(12, 22));
    }

    private static IElement TranslatedAreaFill(long dx, long dy, bool inverse)
    {
        var frame = new FrameBasis();
        var boundary = RectangleSegments(frame, -34 + dx, -22 + dy, 34 + dx, 22 + dy);
        return inverse
            ? GraphicStructure.AntiAreaFill(boundary)
            : GraphicStructure.AreaFill(boundary);
    }

    private static IElement TwoPointBond()
    {
        var frame = new FrameBasis();
        var a = frame.Point(-10, 0);
        var b = frame.Point(10, 0);
        return Group(a, b, GraphicStructure.Pin(a, b));
    }

    private static IElement SharedPolyline()
    {
        var frame = new FrameBasis();
        var a = frame.Point(-44, 18);
        var b = frame.Point(-16, -16);
        var c = frame.Point(14, 16);
        var d = frame.Point(44, -18);
        return Group(
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d));
    }

    private static IElement SelectionEvidence()
    {
        var source = LineSegment();
        return Group(source, TensionSlot());
    }

    private static IElement PlottedPath(FrameBasis frame)
    {
        var a = frame.Point(-44, 24);
        var b = frame.Point(-24, -6);
        var c = frame.Point(0, 10);
        var d = frame.Point(24, -18);
        var e = frame.Point(46, 16);
        return Group(
            a,
            b,
            c,
            d,
            e,
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d),
            GraphicStructure.FrameSegment(d, e));
    }

    private static IElement MultiBranch(FrameBasis frame)
    {
        var hub = frame.Point(0, 0);
        return Group(
            GraphicStructure.FrameSegment(hub, frame.Point(-42, 0)),
            GraphicStructure.FrameSegment(hub, frame.Point(36, -30)),
            GraphicStructure.FrameSegment(hub, frame.Point(44, -8)),
            GraphicStructure.FrameSegment(hub, frame.Point(40, 18)),
            GraphicStructure.FrameSegment(hub, frame.Point(18, 36)));
    }

    private static IElement BranchMergeNetwork(FrameBasis frame)
    {
        var a = frame.Point(-50, 0);
        var b = frame.Point(-18, 0);
        var c = frame.Point(18, -24);
        var d = frame.Point(18, 24);
        var e = frame.Point(50, 0);
        return Group(
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(b, d),
            GraphicStructure.FrameSegment(c, e),
            GraphicStructure.FrameSegment(d, e));
    }

    private static IElement CurvedAreaWithLooseBranch(FrameBasis frame, bool partial)
    {
        var a = frame.Point(-34, -24);
        var b = frame.Point(-34, 24);
        var c = frame.Point(20, 24);
        var d = frame.Point(20, -24);
        var arc = GraphicStructure.FrameArc(frame.Point(28, 0), c, d);
        var left = GraphicStructure.FrameSegment(a, b);
        var bottom = GraphicStructure.FrameSegment(b, c);
        var top = GraphicStructure.FrameSegment(d, a);
        var loose = GraphicStructure.FrameSegment(frame.Point(-46, -34), frame.Point(46, -44));
        if (partial)
        {
            var tension = TensionSlot();
            return Group(left, bottom, arc, top, loose, tension);
        }

        var boundary = new IElement[] { left, bottom, arc, top };
        return Group(GraphicStructure.AreaFill(boundary), Group(boundary), loose);
    }

    private static IElement[] RectangleSegments(FrameBasis frame, long left, long top, long right, long bottom)
    {
        var a = frame.Point(left, top);
        var b = frame.Point(right, top);
        var c = frame.Point(right, bottom);
        var d = frame.Point(left, bottom);
        return
        [
            GraphicStructure.FrameSegment(a, b),
            GraphicStructure.FrameSegment(b, c),
            GraphicStructure.FrameSegment(c, d),
            GraphicStructure.FrameSegment(d, a)
        ];
    }

    private static Relation Group(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements);

    private static Relation TensionSlot()
    {
        var slot = new Placeholder();
        return new Relation(slot, slot.Dual());
    }

    private static BlocksGlossaryStructure Spec(
        IElement structure,
        string expression,
        string? encodingNote = null,
        SKRect? bounds = null,
        int seed = 0) =>
        new(structure, bounds ?? Bounds(-62, -48, 62, 48), seed, expression, encodingNote);

    private static void AddSpecimens(
        Dictionary<string, BlocksGlossaryStructure> structures,
        string prefix,
        IReadOnlyList<(string Id, IElement Structure, string Expression, string? Note)> entries)
    {
        foreach (var (id, structure, expression, note) in entries)
        {
            structures[$"{prefix}-{id}"] = Spec(structure, expression, note);
        }
    }

    private static SKRect Bounds(float left, float top, float right, float bottom) =>
        new(left, top, right, bottom);

    private enum GlossaryRow
    {
        RadialPositive,
        RadialInverse,
        Linear
    }

    private enum CurveJoinKind
    {
        LineLine,
        LineArc,
        ArcArc
    }

    private enum CurveColumn
    {
        Corner,
        ArcBlend,
        Bezier,
        Clothoid,
        Involute,
        Plotted
    }

    private readonly record struct WalkedFrameEdge(
        SharedFrameSegmentRead Segment,
        SharedFramePointRead Start,
        SharedFramePointRead End);

    private readonly record struct WalkedFramePoint(
        SharedFramePointRead Template,
        Point2 Point);

    private readonly record struct Point2(double X, double Y)
    {
        public static Point2 operator +(Point2 left, Point2 right) =>
            new(left.X + right.X, left.Y + right.Y);

        public static Point2 operator -(Point2 left, Point2 right) =>
            new(left.X - right.X, left.Y - right.Y);

        public static Point2 operator *(Point2 point, double scale) =>
            new(point.X * scale, point.Y * scale);
    }

    private sealed class FrameBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation Point(long px, long py) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, px), new CarrierLocation(y, py));
    }
}
