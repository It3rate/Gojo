using Gojo.Core;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.Encoding;

/// <summary>
/// Reads a one-carrier motion goal through local obstacle and branch structure.
/// The detour is not a named preposition: it is an ordered path whose middle
/// segment lives on a different carrier and rejoins the primary carrier after
/// the blocked interval.
/// </summary>
public static class IntervalDetourSettlementSurface
{
    public readonly record struct DetourSettlementRead(
        Relation FocusDefinition,
        Relation ContextDefinition,
        Relation BranchCandidate,
        Carrier PrimaryCarrier,
        Carrier BranchCarrier,
        long CurrentTicks,
        long GoalTicks,
        long EntryTicks,
        long ExitTicks,
        long BranchEntryTicks,
        long BranchExitTicks,
        long ObstacleStartTicks,
        long ObstacleEndTicks,
        Relation Path,
        Relation DirectTension);

    public readonly record struct BlockedSettlementRead(
        Relation FocusDefinition,
        Relation ContextDefinition,
        Relation Obstacle,
        Carrier PrimaryCarrier,
        long CurrentTicks,
        long GoalTicks,
        long ObstacleStartTicks,
        long ObstacleEndTicks,
        Relation DirectTension);

    public static Relation BranchCandidate(
        Carrier root,
        Carrier primaryCarrier,
        long entryTicks,
        Carrier branchCarrier,
        long branchEntryTicks,
        long branchExitTicks,
        long exitTicks) =>
        Relation.OrderedGroup(
            root ?? new Carrier(),
            new Relation(
                Relation.Point(primaryCarrier, entryTicks),
                Relation.Point(branchCarrier, branchEntryTicks)),
            Relation.Segment(branchCarrier, branchEntryTicks, branchExitTicks),
            new Relation(
                Relation.Point(branchCarrier, branchExitTicks),
                Relation.Point(primaryCarrier, exitTicks)));

    public static Relation PositiveOutletBranchCandidate(
        Carrier root,
        Carrier primaryCarrier,
        long entryTicks,
        Carrier branchCarrier,
        long branchEntryTicks,
        long branchExitTicks,
        long exitTicks)
    {
        var positiveOutlet = PositiveOutlet(
            primaryCarrier,
            entryTicks,
            exitTicks,
            branchCarrier,
            branchEntryTicks,
            branchExitTicks);
        return Relation.OrderedGroup(
            root ?? new Carrier(),
            new Relation(
                Relation.Point(primaryCarrier, entryTicks),
                Relation.Point(branchCarrier.Inverse(), branchEntryTicks)),
            positiveOutlet,
            new Relation(
                Relation.Point(branchCarrier, branchExitTicks),
                Relation.Point(primaryCarrier, exitTicks)));
    }

    public static bool TryCreateSettlement(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation focusDefinition,
        Relation contextDefinition,
        Carrier root,
        out Relation settlement)
    {
        if (!TryPlan(graph, focusDefinition, contextDefinition, out var plan))
        {
            settlement = null!;
            return false;
        }

        settlement = ReadWitness.Create(
            root ?? new Carrier(),
            focusDefinition,
            contextDefinition,
            plan.Result,
            plan.DirectTension,
            plan.Provenance);
        return true;
    }

    public static bool TryReadDetour(Relation settlement, out DetourSettlementRead read)
    {
        if (ReadWitness.TryRead(
                settlement,
                out var selected,
                out var readSurface,
                out var result,
                out var residueOrTension,
                out var provenance) &&
            selected is Relation focusDefinition &&
            readSurface is Relation contextDefinition &&
            provenance is Relation branchCandidate &&
            result is Relation path &&
            residueOrTension is Relation directTension &&
            TryReadPath(path, out var pathRead) &&
            TryReadBlockedTension(
                directTension,
                out var primaryCarrier,
                out var currentTicks,
                out var goalTicks,
                out _,
                out var obstacleStart,
                out var obstacleEnd) &&
            pathRead.PrimaryCarrier.SameIdentityAs(primaryCarrier) &&
            pathRead.CurrentTicks == currentTicks &&
            pathRead.GoalTicks == goalTicks)
        {
            read = new DetourSettlementRead(
                focusDefinition,
                contextDefinition,
                branchCandidate,
                primaryCarrier,
                pathRead.BranchCarrier,
                currentTicks,
                goalTicks,
                pathRead.EntryTicks,
                pathRead.ExitTicks,
                pathRead.BranchEntryTicks,
                pathRead.BranchExitTicks,
                obstacleStart,
                obstacleEnd,
                path,
                directTension);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadBlocked(Relation settlement, out BlockedSettlementRead read)
    {
        if (ReadWitness.TryRead(
                settlement,
                out var selected,
                out var readSurface,
                out var result,
                out var residueOrTension,
                out var provenance) &&
            selected is Relation focusDefinition &&
            readSurface is Relation contextDefinition &&
            provenance is Relation obstacle &&
            EncodedTextSurface.IsUnknownPoint(result) &&
            residueOrTension is Relation directTension &&
            TryReadBlockedTension(
                directTension,
                out var primaryCarrier,
                out var currentTicks,
                out var goalTicks,
                out _,
                out var obstacleStart,
                out var obstacleEnd))
        {
            read = new BlockedSettlementRead(
                focusDefinition,
                contextDefinition,
                obstacle,
                primaryCarrier,
                currentTicks,
                goalTicks,
                obstacleStart,
                obstacleEnd,
                directTension);
            return true;
        }

        read = default;
        return false;
    }

    private static bool TryPlan(
        ConceptDefinitionDiscoverySurface.DefinitionGraph graph,
        Relation focusDefinition,
        Relation contextDefinition,
        out SettlementPlan plan)
    {
        if (!graph.TryFind(focusDefinition, out var focusNode) ||
            !TryReadPoint(focusNode.Read.Body, out var focus) ||
            !graph.TryFind(contextDefinition, out var contextNode) ||
            !TryReadContext(contextNode.Read.Body, focus, out var context))
        {
            plan = default;
            return false;
        }

        var directStart = Math.Min(focus.Ticks, context.GoalTicks);
        var directEnd = Math.Max(focus.Ticks, context.GoalTicks);
        var blocker = context.Obstacles
            .Where(obstacle => IntersectsOpen(directStart, directEnd, obstacle.StartTicks, obstacle.EndTicks))
            .OrderBy(obstacle => DistanceFromRouteStart(focus.Ticks, context.GoalTicks, obstacle.StartTicks))
            .FirstOrDefault();
        if (blocker.Element is null)
        {
            plan = new SettlementPlan(
                Relation.OrderedGroup(
                    new Carrier(),
                    Relation.Segment(focus.Carrier, focus.Ticks, context.GoalTicks)),
                EncodedTextSurface.UnknownPoint(),
                contextDefinition);
            return true;
        }

        var directTension = BlockedDirectTension(
            new Carrier(),
            focus.Carrier,
            focus.Ticks,
            context.GoalTicks,
            blocker.Element);
        foreach (var branch in context.Branches)
        {
            if (!CoversObstacle(focus.Ticks, context.GoalTicks, blocker, branch))
            {
                continue;
            }

            plan = new SettlementPlan(
                branch.IsDirectedPositiveOutlet
                    ? DirectedDetourPath(
                        new Carrier(),
                        focus.Carrier,
                        branch.BranchCarrier,
                        focus.Ticks,
                        context.GoalTicks,
                        branch.EntryTicks,
                        branch.ExitTicks,
                        branch.BranchEntryTicks,
                        branch.BranchExitTicks)
                    : DetourPath(
                        new Carrier(),
                        focus.Carrier,
                        branch.BranchCarrier,
                        focus.Ticks,
                        context.GoalTicks,
                        branch.EntryTicks,
                        branch.ExitTicks,
                        branch.BranchEntryTicks,
                        branch.BranchExitTicks),
                directTension,
                branch.Element);
            return true;
        }

        plan = new SettlementPlan(
            EncodedTextSurface.UnknownPoint(),
            directTension,
            blocker.Element);
        return true;
    }

    private static bool TryReadContext(
        IElement element,
        CarrierLocation focus,
        out ContextRead context)
    {
        if (element is not Relation group ||
            !EncodedTextSurface.TryReadRootElements(group, out var elements))
        {
            context = default;
            return false;
        }

        long? goalTicks = null;
        var obstacles = new List<ObstacleRead>();
        var branches = new List<BranchRead>();
        foreach (var item in elements)
        {
            if (TryReadPoint(item, out var point) &&
                point.Carrier.SameIdentityAs(focus.Carrier) &&
                point.Ticks != focus.Ticks)
            {
                if (goalTicks is null ||
                    Math.Abs(point.Ticks - focus.Ticks) > Math.Abs(goalTicks.Value - focus.Ticks))
                {
                    goalTicks = point.Ticks;
                }

                continue;
            }

            if (TryReadSegment(item, out var segmentCarrier, out var start, out var end) &&
                segmentCarrier.SameIdentityAs(focus.Carrier))
            {
                obstacles.Add(new ObstacleRead(item, start, end));
                continue;
            }

            if (TryReadBranchCandidate(item, focus.Carrier, out var branch))
            {
                branches.Add(branch);
            }
        }

        if (goalTicks is not { } foundGoal)
        {
            context = default;
            return false;
        }

        context = new ContextRead(foundGoal, [.. obstacles], [.. branches]);
        return true;
    }

    private static bool TryReadBranchCandidate(
        IElement element,
        Carrier primaryCarrier,
        out BranchRead branch)
    {
        if (element is not Relation group ||
            !EncodedTextSurface.TryReadRootElements(group, out var elements) ||
            elements.Length < 3)
        {
            branch = default;
            return false;
        }

        var links = elements.SelectMany(LinkOrEmpty).ToArray();
        var branchFlows = elements
            .SelectMany(item => BranchFlowOrEmpty(item, primaryCarrier))
            .ToArray();
        if (links.Length < 2 || branchFlows.Length == 0)
        {
            branch = default;
            return false;
        }

        foreach (var flow in branchFlows)
        {
            var primaryLinks = links
                .Select(link => TryOrientLink(link, primaryCarrier, flow, out var oriented)
                    ? oriented
                    : (BranchLink?)null)
                .Where(link => link is not null)
                .Select(link => link!.Value)
                .Where(link => IsWithinClosed(flow.StartTicks, flow.EndTicks, link.BranchTicks))
                .ToArray();
            if (primaryLinks.Length < 2)
            {
                continue;
            }

            var ordered = primaryLinks
                .OrderBy(link => link.PrimaryTicks)
                .ToArray();
            var entry = ordered[0];
            var exit = ordered[^1];
            if (!MatchesFlowEnds(flow, entry, exit))
            {
                continue;
            }

            branch = new BranchRead(
                element,
                primaryCarrier,
                flow.BranchCarrier,
                entry.PrimaryTicks,
                exit.PrimaryTicks,
                entry.BranchTicks,
                exit.BranchTicks,
                flow.IsDirectedPositiveOutlet);
            return true;
        }

        branch = default;
        return false;
    }

    private static bool TryReadPath(IElement element, out DetourPathRead read)
    {
        if (element is Relation path &&
            EncodedTextSurface.TryReadRootElements(path, out var elements) &&
            elements.Length == 5 &&
            TryReadSegment(elements[0], out var primaryCarrier, out var currentTicks, out var entryTicks) &&
            TryReadLink(elements[1], out var entryPrimary, out var branchEntry) &&
            TryReadBranchFlow(elements[2], primaryCarrier, out var branchFlow) &&
            TryReadLink(elements[3], out var branchExit, out var exitPrimary) &&
            TryReadSegment(elements[4], out var exitCarrier, out var exitTicks, out var goalTicks) &&
            entryPrimary.Carrier.SameIdentityAs(primaryCarrier) &&
            entryPrimary.Ticks == entryTicks &&
            exitPrimary.Carrier.SameIdentityAs(primaryCarrier) &&
            exitPrimary.Ticks == exitTicks &&
            exitCarrier.SameIdentityAs(primaryCarrier) &&
            BranchPointMatches(branchEntry, branchFlow.EntryCarrier, branchFlow.StartTicks) &&
            BranchPointMatches(branchExit, branchFlow.ExitCarrier, branchFlow.EndTicks))
        {
            read = new DetourPathRead(
                primaryCarrier,
                branchFlow.BranchCarrier,
                currentTicks,
                goalTicks,
                entryTicks,
                exitTicks,
                branchEntry.Ticks,
                branchExit.Ticks);
            return true;
        }

        read = default;
        return false;
    }

    private static Relation DetourPath(
        Carrier root,
        Carrier primaryCarrier,
        Carrier branchCarrier,
        long currentTicks,
        long goalTicks,
        long entryTicks,
        long exitTicks,
        long branchEntryTicks,
        long branchExitTicks) =>
        Relation.OrderedGroup(
            root ?? new Carrier(),
            Relation.Segment(primaryCarrier, currentTicks, entryTicks),
            new Relation(
                Relation.Point(primaryCarrier, entryTicks),
                Relation.Point(branchCarrier, branchEntryTicks)),
            Relation.Segment(branchCarrier, branchEntryTicks, branchExitTicks),
            new Relation(
                Relation.Point(branchCarrier, branchExitTicks),
                Relation.Point(primaryCarrier, exitTicks)),
            Relation.Segment(primaryCarrier, exitTicks, goalTicks));

    private static Relation DirectedDetourPath(
        Carrier root,
        Carrier primaryCarrier,
        Carrier branchCarrier,
        long currentTicks,
        long goalTicks,
        long entryTicks,
        long exitTicks,
        long branchEntryTicks,
        long branchExitTicks) =>
        Relation.OrderedGroup(
            root ?? new Carrier(),
            Relation.Segment(primaryCarrier, currentTicks, entryTicks),
            new Relation(
                Relation.Point(primaryCarrier, entryTicks),
                Relation.Point(branchCarrier.Inverse(), branchEntryTicks)),
            PositiveOutlet(
                primaryCarrier,
                entryTicks,
                exitTicks,
                branchCarrier,
                branchEntryTicks,
                branchExitTicks),
            new Relation(
                Relation.Point(branchCarrier, branchExitTicks),
                Relation.Point(primaryCarrier, exitTicks)),
            Relation.Segment(primaryCarrier, exitTicks, goalTicks));

    private static Relation PositiveOutlet(
        Carrier primaryCarrier,
        long entryTicks,
        long exitTicks,
        Carrier branchCarrier,
        long branchEntryTicks,
        long branchExitTicks) =>
        new(
            Relation.Segment(primaryCarrier, entryTicks, exitTicks),
            Relation.DirectedSegment(branchCarrier, branchEntryTicks, branchExitTicks));

    private static Relation BlockedDirectTension(
        Carrier root,
        Carrier primaryCarrier,
        long currentTicks,
        long goalTicks,
        IElement obstacle) =>
        Relation.OrderedGroup(
            root ?? new Carrier(),
            Relation.Segment(primaryCarrier, currentTicks, goalTicks),
            obstacle);

    private static bool TryReadBlockedTension(
        IElement element,
        out Carrier primaryCarrier,
        out long currentTicks,
        out long goalTicks,
        out Relation directSpan,
        out long obstacleStartTicks,
        out long obstacleEndTicks)
    {
        if (element is Relation tension &&
            EncodedTextSurface.TryReadRootElements(tension, out var elements) &&
            elements.Length == 2 &&
            elements[0] is Relation direct &&
            TryReadSegment(direct, out primaryCarrier, out currentTicks, out goalTicks) &&
            TryReadSegment(elements[1], out var obstacleCarrier, out obstacleStartTicks, out obstacleEndTicks) &&
            obstacleCarrier.SameIdentityAs(primaryCarrier))
        {
            directSpan = direct;
            return true;
        }

        primaryCarrier = Carrier.Unknown;
        currentTicks = 0;
        goalTicks = 0;
        directSpan = null!;
        obstacleStartTicks = 0;
        obstacleEndTicks = 0;
        return false;
    }

    private static bool CoversObstacle(
        long routeStartTicks,
        long routeGoalTicks,
        ObstacleRead obstacle,
        BranchRead branch)
    {
        var routeLength = Math.Abs(routeGoalTicks - routeStartTicks);
        var entry = DistanceFromRouteStart(routeStartTicks, routeGoalTicks, branch.EntryTicks);
        var exit = DistanceFromRouteStart(routeStartTicks, routeGoalTicks, branch.ExitTicks);
        var obstacleStart = DistanceFromRouteStart(routeStartTicks, routeGoalTicks, obstacle.StartTicks);
        var obstacleEnd = DistanceFromRouteStart(routeStartTicks, routeGoalTicks, obstacle.EndTicks);
        if (entry > exit)
        {
            (entry, exit) = (exit, entry);
        }

        if (obstacleStart > obstacleEnd)
        {
            (obstacleStart, obstacleEnd) = (obstacleEnd, obstacleStart);
        }

        return entry >= 0 &&
            exit <= routeLength &&
            entry < obstacleStart &&
            exit > obstacleEnd;
    }

    private static long DistanceFromRouteStart(long startTicks, long goalTicks, long ticks) =>
        goalTicks >= startTicks ? ticks - startTicks : startTicks - ticks;

    private static bool IntersectsOpen(long firstStart, long firstEnd, long secondStart, long secondEnd) =>
        Math.Max(firstStart, secondStart) < Math.Min(firstEnd, secondEnd);

    private static bool IsWithinClosed(long start, long end, long ticks) =>
        ticks >= Math.Min(start, end) && ticks <= Math.Max(start, end);

    private static IEnumerable<LinkRead> LinkOrEmpty(IElement element)
    {
        if (TryReadLink(element, out var left, out var right))
        {
            yield return new LinkRead(left, right);
        }
    }

    private static IEnumerable<BranchFlowRead> BranchFlowOrEmpty(IElement element, Carrier primaryCarrier)
    {
        if (TryReadBranchFlow(element, primaryCarrier, out var flow))
        {
            yield return flow;
        }
    }

    private static bool TryOrientLink(
        LinkRead link,
        Carrier primaryCarrier,
        BranchFlowRead flow,
        out BranchLink oriented)
    {
        if (link.Left.Carrier.SameIdentityAs(primaryCarrier) &&
            IsFlowCarrier(flow, link.Right.Carrier))
        {
            oriented = new BranchLink(link.Left.Ticks, link.Right.Carrier, link.Right.Ticks);
            return true;
        }

        if (link.Right.Carrier.SameIdentityAs(primaryCarrier) &&
            IsFlowCarrier(flow, link.Left.Carrier))
        {
            oriented = new BranchLink(link.Right.Ticks, link.Left.Carrier, link.Left.Ticks);
            return true;
        }

        oriented = default;
        return false;
    }

    private static bool TryReadBranchFlow(
        IElement element,
        Carrier primaryCarrier,
        out BranchFlowRead flow)
    {
        if (TryReadPositiveOutlet(
                element,
                primaryCarrier,
                out _,
                out _,
                out var branchCarrier,
                out var directedStart,
                out var directedEnd))
        {
            flow = new BranchFlowRead(
                branchCarrier,
                branchCarrier.Inverse(),
                branchCarrier,
                directedStart,
                directedEnd,
                IsDirectedPositiveOutlet: true);
            return true;
        }

        if (TryReadSegment(element, out var carrier, out var start, out var end) &&
            !carrier.SameIdentityAs(primaryCarrier))
        {
            flow = new BranchFlowRead(
                carrier,
                carrier,
                carrier,
                start,
                end,
                IsDirectedPositiveOutlet: false);
            return true;
        }

        flow = default;
        return false;
    }

    public static bool TryReadPositiveOutlet(
        IElement element,
        Carrier primaryCarrier,
        out long sourceStartTicks,
        out long sourceEndTicks,
        out Carrier branchCarrier,
        out long branchEntryTicks,
        out long branchExitTicks)
    {
        if (element is Relation outlet &&
            TryReadSegment(outlet.Left, out var sourceCarrier, out sourceStartTicks, out sourceEndTicks) &&
            sourceCarrier.SameIdentityAs(primaryCarrier) &&
            outlet.Right is Relation directedSegment &&
            directedSegment.TryReadDirectedSegment(out var inverseStart, out var positiveEnd) &&
            !positiveEnd.Carrier.SameIdentityAs(primaryCarrier) &&
            inverseStart.Carrier.SameIdentityAs(positiveEnd.Carrier.Inverse()))
        {
            branchCarrier = positiveEnd.Carrier;
            branchEntryTicks = inverseStart.Ticks;
            branchExitTicks = positiveEnd.Ticks;
            return true;
        }

        sourceStartTicks = 0;
        sourceEndTicks = 0;
        branchCarrier = Carrier.Unknown;
        branchEntryTicks = 0;
        branchExitTicks = 0;
        return false;
    }

    private static bool MatchesFlowEnds(BranchFlowRead flow, BranchLink entry, BranchLink exit) =>
        BranchPointMatches(entry.BranchCarrier, entry.BranchTicks, flow.EntryCarrier, flow.StartTicks) &&
        BranchPointMatches(exit.BranchCarrier, exit.BranchTicks, flow.ExitCarrier, flow.EndTicks);

    private static bool IsFlowCarrier(BranchFlowRead flow, Carrier carrier) =>
        carrier.SameIdentityAs(flow.EntryCarrier) ||
        carrier.SameIdentityAs(flow.ExitCarrier);

    private static bool BranchPointMatches(CarrierLocation point, Carrier carrier, long ticks) =>
        BranchPointMatches(point.Carrier, point.Ticks, carrier, ticks);

    private static bool BranchPointMatches(Carrier foundCarrier, long foundTicks, Carrier expectedCarrier, long expectedTicks) =>
        foundCarrier.SameIdentityAs(expectedCarrier) &&
        foundTicks == expectedTicks;

    private static bool TryReadLink(
        IElement element,
        out CarrierLocation left,
        out CarrierLocation right)
    {
        if (element is Relation link &&
            TryReadPoint(link.Left, out left) &&
            TryReadPoint(link.Right, out right) &&
            !left.Carrier.SameIdentityAs(right.Carrier))
        {
            return true;
        }

        left = default;
        right = default;
        return false;
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static bool TryReadSegment(IElement element, out Carrier carrier, out long start, out long end)
    {
        if (element is Relation segment &&
            TryReadPoint(segment.Left, out var left) &&
            TryReadPoint(segment.Right, out var right) &&
            left.Carrier.SameIdentityAs(right.Carrier))
        {
            carrier = left.Carrier;
            start = Math.Min(left.Ticks, right.Ticks);
            end = Math.Max(left.Ticks, right.Ticks);
            return true;
        }

        carrier = Carrier.Unknown;
        start = 0;
        end = 0;
        return false;
    }

    private readonly record struct SettlementPlan(
        IElement Result,
        IElement DirectTension,
        IElement Provenance);

    private readonly record struct ContextRead(
        long GoalTicks,
        ObstacleRead[] Obstacles,
        BranchRead[] Branches);

    private readonly record struct ObstacleRead(
        IElement Element,
        long StartTicks,
        long EndTicks);

    private readonly record struct BranchRead(
        IElement Element,
        Carrier PrimaryCarrier,
        Carrier BranchCarrier,
        long EntryTicks,
        long ExitTicks,
        long BranchEntryTicks,
        long BranchExitTicks,
        bool IsDirectedPositiveOutlet);

    private readonly record struct LinkRead(
        CarrierLocation Left,
        CarrierLocation Right);

    private readonly record struct BranchLink(
        long PrimaryTicks,
        Carrier BranchCarrier,
        long BranchTicks);

    private readonly record struct BranchFlowRead(
        Carrier BranchCarrier,
        Carrier EntryCarrier,
        Carrier ExitCarrier,
        long StartTicks,
        long EndTicks,
        bool IsDirectedPositiveOutlet);

    private readonly record struct DetourPathRead(
        Carrier PrimaryCarrier,
        Carrier BranchCarrier,
        long CurrentTicks,
        long GoalTicks,
        long EntryTicks,
        long ExitTicks,
        long BranchEntryTicks,
        long BranchExitTicks);
}
