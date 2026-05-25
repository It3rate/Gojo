using Gojo.Core;

namespace Gojo.Reads;

/// <summary>
/// Projection/read helpers over existing relation structure. These remain
/// observational: they split spans, support, and traversal states without
/// creating new settlement machinery.
/// </summary>
public static class RelationReads
{
    #region Boundary and Traversal Reads

    /// <summary>Reads distinct visible boundary points from relation surfaces.</summary>
    public static CarrierLocation[] BoundaryPointsOn(Carrier reader, params Relation[] relations)
    {
        return relations
            .Select(relation => relation.BoundariesOn(reader))
            .Where(boundary => boundary.LeftLocation.Ticks != boundary.RightLocation.Ticks)
            .SelectMany(boundary => new[] { boundary.LeftLocation, boundary.RightLocation })
            .Distinct()
            .OrderBy(location => location.Carrier.Id)
            .ThenBy(location => location.Ticks)
            .ToArray();
    }

    /// <summary>Reads boundary points crossed by a directed travel relation.</summary>
    public static CarrierLocation[] TraversedBoundaryPointsOn(Carrier reader, Relation travel, params Relation[] relations)
    {
        if (!TryReadTraversal(reader, travel, out var start, out var end))
        {
            return [];
        }

        return BoundaryPointsOn(reader, relations)
            .Where(point => point.Carrier.SameIdentityAs(reader) && IsTraversed(point.Ticks, start, end))
            .OrderBy(point => start < end ? point.Ticks : -point.Ticks)
            .ToArray();
    }

    /// <summary>Reads crossed boundary points paired with travel flow.</summary>
    public static Relation[] TraversedBoundaryStatesOn(Carrier reader, Relation travel, params Relation[] relations)
    {
        var flow = travel.FlowOn(reader);
        return flow.ReadFrom(reader).IsVisible
            ? TraversedBoundaryPointsOn(reader, travel, relations)
                .Select(point => new Relation(point, flow))
                .ToArray()
            : [];
    }

    /// <summary>Splits a travel relation into spans at crossed boundary points.</summary>
    public static Relation[] TraversedSpansOn(Carrier reader, Relation travel, params Relation[] relations)
    {
        if (!TryReadTraversal(reader, travel, out var start, out var end))
        {
            return [];
        }

        var points = new List<CarrierLocation>
        {
            new(reader, start)
        };

        foreach (var point in TraversedBoundaryPointsOn(reader, travel, relations))
        {
            AddPoint(point);
        }

        AddPoint(new CarrierLocation(reader, end));

        var spans = new List<Relation>();
        for (var i = 0; i < points.Count - 1; i++)
        {
            if (points[i].Ticks != points[i + 1].Ticks)
            {
                spans.Add(new(points[i], points[i + 1]));
            }
        }

        return [.. spans];

        void AddPoint(CarrierLocation point)
        {
            if (!points.Contains(point))
            {
                points.Add(point);
            }
        }
    }

    /// <summary>Reads traversed spans paired with each span's flow.</summary>
    public static Relation[] TraversedSpanStatesOn(Carrier reader, Relation travel, params Relation[] relations) =>
        TraversedSpansOn(reader, travel, relations)
            .Select(span => new Relation(span, span.FlowOn(reader)))
            .ToArray();

    /// <summary>Reads traversed spans paired with support state.</summary>
    public static Relation[] TraversedSupportStatesOn(Carrier reader, Relation travel, params Relation[] support) =>
        TraversedSpansOn(reader, travel, support)
            .Select(span => new Relation(span, SupportStateOn(span, reader, support)))
            .ToArray();

    /// <summary>Projects traversed support states into support slots.</summary>
    public static Relation[] TraversedSupportOn(Carrier reader, Relation travel, params Relation[] support) =>
        ProjectTraversedStatesOn(reader, travel, state => state.SupportOn(reader), support);

    /// <summary>Projects traversed support states into tension slots.</summary>
    public static Relation[] TraversedTensionOn(Carrier reader, Relation travel, params Relation[] support) =>
        ProjectTraversedStatesOn(reader, travel, state => state.TensionOn(reader), support);

    /// <summary>Projects traversed support states into exclusive slots.</summary>
    public static Relation[] TraversedExclusiveOn(Carrier reader, Relation travel, params Relation[] support) =>
        ProjectTraversedStatesOn(reader, travel, state => state.ExclusiveOn(reader), support);

    /// <summary>Reads traversed pass spans as support projection.</summary>
    public static Relation[] TraversedPassOn(Carrier reader, Relation travel, params Relation[] support) =>
        TraversedSupportOn(reader, travel, support);

    /// <summary>Reads traversed spans with no support participation as blocked spans.</summary>
    public static Relation[] TraversedBlockOn(Carrier reader, Relation travel, params Relation[] support) =>
        TraversedSupportStatesOn(reader, travel, support)
            .Where(state => state.Left is Relation && state.Right is Relation supportState && !HasStateParticipationOn(supportState, reader))
            .Select(state => (Relation)state.Left)
            .ToArray();

    #endregion

    #region Coverage and Support Reads

    /// <summary>Splits all covered carrier spans from relation boundaries.</summary>
    public static Relation[] CoveredSpansOn(Carrier reader, params Relation[] relations)
    {
        var boundaries = relations
            .Select(relation => relation.BoundariesOn(reader))
            .Where(boundary => boundary.LeftLocation.Ticks != boundary.RightLocation.Ticks)
            .ToArray();

        if (boundaries.Length == 0)
        {
            return [];
        }

        var spans = new List<Relation>();
        foreach (var group in boundaries.GroupBy(boundary => boundary.LeftLocation.Carrier))
        {
            var groupedBoundaries = group.ToArray();
            var points = groupedBoundaries
                .SelectMany(boundary => new[] { boundary.LeftLocation, boundary.RightLocation })
                .Distinct()
                .OrderBy(location => location.Ticks)
                .ToArray();

            for (var i = 0; i < points.Length - 1; i++)
            {
                var start = points[i];
                var end = points[i + 1];
                if (IsCovered(start.Ticks, end.Ticks, groupedBoundaries))
                {
                    spans.Add(new(start, end));
                }
            }
        }

        return [.. spans];
    }

    /// <summary>Reads covered spans clipped to a scope boundary.</summary>
    public static Relation[] CoveredSpansWithinOn(Carrier reader, Relation scope, params Relation[] relations)
    {
        var scopeBoundary = scope.BoundariesOn(reader);
        if (scopeBoundary.LeftLocation.Ticks == scopeBoundary.RightLocation.Ticks)
        {
            return [];
        }

        var coveredBoundaries = ClipToScope(reader, scopeBoundary, relations);
        if (coveredBoundaries.Length == 0)
        {
            return [];
        }

        return SplitScopeOn(scopeBoundary, coveredBoundaries)
            .Where(span => IsCovered(span.LeftLocation.Ticks, span.RightLocation.Ticks, coveredBoundaries))
            .ToArray();
    }

    /// <summary>Reads support state for every covered span.</summary>
    public static Relation[] SupportStatesOn(Carrier reader, params Relation[] relations) =>
        CoveredSpansOn(reader, relations)
            .Select(span => new Relation(span, SupportStateOn(span, reader, relations)))
            .ToArray();

    /// <summary>Reads support state for every covered span inside a scope.</summary>
    public static Relation[] SupportStatesWithinOn(Carrier reader, Relation scope, params Relation[] relations) =>
        CoveredSpansWithinOn(reader, scope, relations)
            .Select(span => new Relation(span, SupportStateOn(span, reader, relations)))
            .ToArray();

    /// <summary>Projects covered support states into support slots.</summary>
    public static Relation[] SupportOn(Carrier reader, params Relation[] relations) =>
        ProjectStatesOn(reader, state => state.SupportOn(reader), relations);

    /// <summary>Projects covered support states into tension slots.</summary>
    public static Relation[] TensionOn(Carrier reader, params Relation[] relations) =>
        ProjectStatesOn(reader, state => state.TensionOn(reader), relations);

    /// <summary>Projects covered support states into exclusive slots.</summary>
    public static Relation[] ExclusiveOn(Carrier reader, params Relation[] relations) =>
        ProjectStatesOn(reader, state => state.ExclusiveOn(reader), relations);

    /// <summary>Projects scoped support states into support slots.</summary>
    public static Relation[] SupportWithinOn(Carrier reader, Relation scope, params Relation[] relations) =>
        ProjectStatesWithinOn(reader, scope, state => state.SupportOn(reader), relations);

    /// <summary>Projects scoped support states into tension slots.</summary>
    public static Relation[] TensionWithinOn(Carrier reader, Relation scope, params Relation[] relations) =>
        ProjectStatesWithinOn(reader, scope, state => state.TensionOn(reader), relations);

    /// <summary>Projects scoped support states into exclusive slots.</summary>
    public static Relation[] ExclusiveWithinOn(Carrier reader, Relation scope, params Relation[] relations) =>
        ProjectStatesWithinOn(reader, scope, state => state.ExclusiveOn(reader), relations);

    /// <summary>Reads spans inside scope that are not covered by supplied relations.</summary>
    public static Relation[] UncoveredSpansOn(Carrier reader, Relation scope, params Relation[] covered)
    {
        var scopeBoundary = scope.BoundariesOn(reader);
        if (scopeBoundary.LeftLocation.Ticks == scopeBoundary.RightLocation.Ticks)
        {
            return [];
        }

        var coveredBoundaries = ClipToScope(reader, scopeBoundary, covered);
        return SplitScopeOn(scopeBoundary, coveredBoundaries)
            .Where(span => !IsCovered(span.LeftLocation.Ticks, span.RightLocation.Ticks, coveredBoundaries))
            .ToArray();
    }

    #endregion

    #region Projection Helpers

    /// <summary>Projects covered support states and drops invisible results.</summary>
    private static Relation[] ProjectStatesOn(
        Carrier reader,
        Func<Relation, Relation> projection,
        Relation[] relations) =>
        SupportStatesOn(reader, relations)
            .Select(state => new Relation(state.Left, projection(state)))
            .Where(state => state.Right is Relation projected && HasStateParticipationOn(projected, reader))
            .ToArray();

    /// <summary>Projects scoped support states and drops invisible results.</summary>
    private static Relation[] ProjectStatesWithinOn(
        Carrier reader,
        Relation scope,
        Func<Relation, Relation> projection,
        Relation[] relations) =>
        SupportStatesWithinOn(reader, scope, relations)
            .Select(state => new Relation(state.Left, projection(state)))
            .Where(state => state.Right is Relation projected && HasStateParticipationOn(projected, reader))
            .ToArray();

    /// <summary>Projects traversed support states and drops invisible results.</summary>
    private static Relation[] ProjectTraversedStatesOn(
        Carrier reader,
        Relation travel,
        Func<Relation, Relation> projection,
        Relation[] relations) =>
        TraversedSupportStatesOn(reader, travel, relations)
            .Select(state => new Relation(state.Left, projection(state)))
            .Where(state => state.Right is Relation projected && HasStateParticipationOn(projected, reader))
            .ToArray();

    /// <summary>Builds reverse/forward support state for a span from covering relations.</summary>
    private static Relation SupportStateOn(Relation span, Carrier reader, Relation[] relations)
    {
        var reverse = 0UL;
        var forward = 0UL;
        var spanStart = Math.Min(span.LeftLocation.Ticks, span.RightLocation.Ticks);
        var spanEnd = Math.Max(span.LeftLocation.Ticks, span.RightLocation.Ticks);
        foreach (var relation in relations)
        {
            var boundaries = relation.BoundariesOn(reader);
            if (!boundaries.LeftLocation.Carrier.SameIdentityAs(span.LeftLocation.Carrier) ||
                !IsCovered(spanStart, spanEnd, [boundaries]))
            {
                continue;
            }

            var flow = relation.FlowOn(reader).ReadFrom(reader);
            if (flow.Direction > 0)
            {
                forward++;
            }
            else if (flow.Direction < 0)
            {
                reverse++;
            }
        }

        return new(
            new CarrierLocation(reader, -checked((long)reverse)),
            new CarrierLocation(reader, forward));
    }

    /// <summary>Returns true when any state slot participates in the reader frame.</summary>
    private static bool HasStateParticipationOn(Relation state, Carrier reader) =>
        state.CarrierLocations().Any(location => location.ReadFrom(reader).IsVisible);

    /// <summary>Returns true when a span is fully covered by any boundary.</summary>
    private static bool IsCovered(long start, long end, Relation[] boundaries) =>
        boundaries.Any(boundary => boundary.LeftLocation.Ticks <= start && boundary.RightLocation.Ticks >= end);

    /// <summary>Returns true when a point is crossed by a directed traversal.</summary>
    private static bool IsTraversed(long point, long from, long to) =>
        from < to
            ? point > from && point <= to
            : point < from && point >= to;

    /// <summary>Reads nonzero travel endpoints in a reader carrier frame.</summary>
    private static bool TryReadTraversal(Carrier reader, Relation travel, out long start, out long end)
    {
        if (!travel.TryGetCarrierLocations(out var leftLocation, out var rightLocation) ||
            !TryReadInCarrier(reader, leftLocation, rightLocation, out start, out end) ||
            start == end)
        {
            start = 0;
            end = 0;
            return false;
        }

        return true;
    }

    /// <summary>Reads two locations only when both are grounded on the reader carrier.</summary>
    private static bool TryReadInCarrier(
        Carrier reader,
        CarrierLocation left,
        CarrierLocation right,
        out long leftTicks,
        out long rightTicks)
    {
        if (!left.Carrier.SameIdentityAs(reader) || !right.Carrier.SameIdentityAs(reader))
        {
            leftTicks = 0;
            rightTicks = 0;
            return false;
        }

        leftTicks = left.Ticks;
        rightTicks = right.Ticks;
        return true;
    }

    /// <summary>Clips one boundary to a scope boundary.</summary>
    private static Relation ClipTo(Relation scope, Relation boundary)
    {
        if (!scope.LeftLocation.Carrier.SameIdentityAs(boundary.LeftLocation.Carrier))
        {
            var zero = new CarrierLocation(scope.LeftLocation.Carrier, 0);
            return new(zero, zero);
        }

        var start = Math.Max(scope.LeftLocation.Ticks, boundary.LeftLocation.Ticks);
        var end = Math.Min(scope.RightLocation.Ticks, boundary.RightLocation.Ticks);
        return start < end
            ? new Relation(new CarrierLocation(scope.LeftLocation.Carrier, start), new CarrierLocation(scope.LeftLocation.Carrier, end))
            : new Relation(scope.LeftLocation, scope.LeftLocation);
    }

    /// <summary>Clips all relation boundaries to a visible scope.</summary>
    private static Relation[] ClipToScope(Carrier reader, Relation scope, Relation[] relations) =>
        relations
            .Select(relation => relation.BoundariesOn(reader))
            .Select(boundary => ClipTo(scope, boundary))
            .Where(boundary => boundary.LeftLocation.Ticks != boundary.RightLocation.Ticks)
            .ToArray();

    /// <summary>Splits a scope span on boundary endpoints.</summary>
    private static Relation[] SplitScopeOn(Relation scope, Relation[] boundaries)
    {
        var points = boundaries
            .SelectMany(boundary => new[] { boundary.LeftLocation, boundary.RightLocation })
            .Append(scope.LeftLocation)
            .Append(scope.RightLocation)
            .Distinct()
            .OrderBy(location => location.Ticks)
            .ToArray();

        var spans = new List<Relation>();
        for (var i = 0; i < points.Length - 1; i++)
        {
            var start = points[i];
            var end = points[i + 1];
            spans.Add(new(start, end));
        }

        return [.. spans];
    }

    #endregion
}
