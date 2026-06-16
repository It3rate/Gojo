using Gojo.Core;
using Gojo.ConceptSurface;
using GojoCAD;
using GojoCAD.Structures;
using SkiaSharp;

namespace Gojo.Blocks;

internal static class BlocksGlossaryStructureCatalog
{
    public static IReadOnlyDictionary<string, BlocksGlossaryStructure> Create()
    {
        var structures = new Dictionary<string, BlocksGlossaryStructure>();
        AddSourceSupport(structures);
        AddDirectionArea(structures);
        AddCurves(structures);
        AddBranches(structures);
        AddBooleanParticipation(structures);
        AddConnectedPaths(structures);
        AddPathReadBases(structures);
        return structures;
    }

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
                "S(C,0,0) + flow_perp(S(C,0,0))",
                "This is intentionally a collapsed pair, not a drawn-length segment. The two coincident points expose a linear potential before distance is chosen; the visible vertical escape is a renderer read of the segment's orthogonal flow."),
            "flow" => Spec(
                RadialFlow(),
                "P(O) + flow_radial(P(O))",
                inverse
                    ? "Radial inverse flow is the point-grade version of negative orthogonal support: the flow is read as moving into the site from every side. A signed read can pass through the origin, just as opposite side support passes through a segment."
                    : "Radial positive flow is the point-grade version of positive orthogonal support: with only one site, the normal has no preferred linear side, so outward support appears in every direction."),
            "boundary" when linear => Spec(
                LineSegment(),
                "P(A) + P(B) -> S(A,B)"),
            "boundary" => Spec(
                CircleBoundary(includeRadius: true),
                "P(O) + P(R) -> Circle(O,R)"),
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
                "S(C,0,0) + flow_perp(S(C,0,0))",
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
        var pathCarrier = new Carrier();
        var pathRead = Group(
            GraphicStructure.Segment(pathCarrier, 0, 120),
            GraphicStructure.Point(pathCarrier, 20),
            GraphicStructure.Point(pathCarrier, 40),
            GraphicStructure.Point(pathCarrier, 60),
            GraphicStructure.Point(pathCarrier, 80),
            GraphicStructure.Point(pathCarrier, 100));
        return Spec(
            Group(line, arc, curve, pathRead),
            "sections{S,Arc,Curve} -> PathCarrier(0,n) + P(t...)",
            "The path read is derived from the sections but gets its own carrier, partitions, flow, and measurement basis.");
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

    private sealed class FrameBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation Point(long px, long py) =>
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, px), new CarrierLocation(y, py));
    }
}
