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
        AddDerivedPathStructures(structures);
        AddBooleanOffsetReads(structures);
        AddDroneClipOffsetLadder(structures);
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
        GraphicStructure.FrameSegment(frame.Point(-44, -14), frame.Point(-37, -20)),
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
        GraphicStructure.FrameQuadraticCurve(frame.Point(-52, -12), frame.Point(-52, -20), frame.Point(-44, -14))
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
