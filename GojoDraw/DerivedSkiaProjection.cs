using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Projection derived from GojoCAD relation evidence. The viewport only fits
/// the derived model-space structure into pixels at the render boundary.
/// </summary>
public sealed class DerivedSkiaProjection : IGojoCadSkiaProjection
{
    private readonly ProjectionEvidence evidence = new();
    private readonly Dictionary<LocationKey, SKPoint> modelPoints = [];
    private readonly Dictionary<ulong, LinearBasis> linearBases = [];
    private readonly Dictionary<ulong, CircularBasis> circularBases = [];
    private readonly Dictionary<ulong, long> circularPeriods = [];
    private readonly SKRect viewport;
    private readonly Random random;
    private ViewportTransform? viewportTransform;

    public DerivedSkiaProjection(IElement structure, SKRect viewport, int seed)
    {
        this.viewport = viewport;
        random = new Random(seed);
        Collect(structure ?? throw new ArgumentNullException(nameof(structure)));
    }

    public SKPoint Project(CarrierLocation location)
    {
        var normalized = location.Carrier is null
            ? new CarrierLocation(Carrier.Unknown, location.Ticks)
            : Normalize(location);
        return ReadViewportTransform().Apply(ProjectModel(normalized));
    }

    private SKPoint ProjectModel(CarrierLocation location)
    {
        var key = LocationKey.Of(location);
        if (modelPoints.TryGetValue(key, out var point))
        {
            return point;
        }

        point = IsCircularCarrier(location.Carrier)
            ? ProjectCircularModel(location)
            : ProjectLinearModel(location);
        modelPoints[key] = point;

        return point;
    }

    private SKPoint ProjectLinearModel(CarrierLocation location)
    {
        var basis = ReadLinearBasis(location.Carrier);
        return new SKPoint(
            basis.Origin.X + location.Ticks * basis.TickVector.X,
            basis.Origin.Y + location.Ticks * basis.TickVector.Y);
    }

    private SKPoint ProjectCircularModel(CarrierLocation location)
    {
        var basis = ReadCircularProjectionBasis(location.Carrier);
        var phase = (location.Ticks - basis.AnchorTicks) / (float)basis.PeriodTicks;
        var angle = phase * MathF.PI * 2f;
        return new SKPoint(
            basis.Center.X + MathF.Cos(angle) * basis.Radius,
            basis.Center.Y - MathF.Sin(angle) * basis.Radius);
    }

    private LinearBasis ReadLinearBasis(Carrier carrier)
    {
        var forward = NormalizeCarrier(carrier);
        if (linearBases.TryGetValue(forward.Id, out var basis))
        {
            return basis;
        }

        var origin = new SKPoint(0, 0);
        var angle = 0f;
        if (forward.TryReadLinearOrigin(out var linearOrigin))
        {
            origin = ProjectModel(Normalize(linearOrigin));
            angle = SampleAngle();
        }

        basis = new LinearBasis(origin, new SKPoint(MathF.Cos(angle), -MathF.Sin(angle)));
        linearBases[forward.Id] = basis;
        return basis;
    }

    private CircularBasis ReadCircularProjectionBasis(Carrier carrier)
    {
        if (circularBases.TryGetValue(carrier.Id, out var basis))
        {
            return basis;
        }

        var origin = ReadCircularOrigin(carrier);
        var center = ProjectModel(Normalize(origin));

        var attachment = evidence.Attachments
            .Select(attachment => attachment.ForCircularCarrier(carrier))
            .FirstOrDefault(match =>
                match.Circular.Carrier is not null &&
                match.Other.Carrier is not null &&
                !match.Other.Carrier.TryReadCircularOrigin(out _));
        if (attachment.Circular.Carrier is not null)
        {
            var anchorPoint = ProjectModel(Normalize(attachment.Other));
            var radius = Distance(center, anchorPoint);
            if (radius <= 0.1f)
            {
                radius = SampleModelRadius(carrier, origin.Ticks);
            }

            basis = new CircularBasis(
                center,
                radius,
                attachment.Circular.Ticks,
                ReadCircularPeriod(carrier, attachment.Circular.Ticks));
            circularBases[carrier.Id] = basis;
            return basis;
        }

        var marker = evidence.Locations
            .Where(location => location.Carrier.SameIdentityAs(carrier))
            .OrderBy(location => Math.Abs(location.Ticks - origin.Ticks))
            .FirstOrDefault(location => location.Ticks != origin.Ticks);
        var anchorTicks = marker.Carrier is null ? origin.Ticks : marker.Ticks;

        basis = new CircularBasis(
            center,
            SampleModelRadius(carrier, origin.Ticks),
            anchorTicks,
            ReadCircularPeriod(carrier, anchorTicks));
        circularBases[carrier.Id] = basis;
        return basis;
    }

    private long ReadCircularPeriod(Carrier carrier, long anchorTicks)
    {
        if (circularPeriods.TryGetValue(carrier.Id, out var period))
        {
            return Math.Max(1, period);
        }

        var maxDelta = ReadObservedTickDelta(carrier, anchorTicks);
        var lower = Math.Max(1, maxDelta * 2);
        var upper = Math.Max(lower + 1, maxDelta * 4);
        period = upper >= int.MaxValue
            ? lower
            : random.Next((int)lower, (int)upper);
        circularPeriods[carrier.Id] = period;
        return period;
    }

    private long ReadObservedTickDelta(Carrier carrier, long anchorTicks)
    {
        var observedTicks = evidence.Locations
            .Where(location => location.Carrier.SameIdentityAs(carrier))
            .Select(location => location.Ticks)
            .ToArray();
        return observedTicks.Length == 0
            ? Math.Max(1, Math.Abs(anchorTicks))
            : Math.Max(1, observedTicks.Max(tick => Math.Abs(tick - anchorTicks)));
    }

    private float SampleModelRadius(Carrier carrier, long originTicks)
    {
        var tickDelta = ReadObservedTickDelta(carrier, originTicks);
        return tickDelta * (1.2f + (float)random.NextDouble() * 1.4f);
    }

    private ViewportTransform ReadViewportTransform()
    {
        if (viewportTransform is not null)
        {
            return viewportTransform;
        }

        viewportTransform = ViewportTransform.From(ReadModelBounds(), viewport);
        return viewportTransform;
    }

    private SKRect ReadModelBounds()
    {
        var bounds = new ModelBounds();
        foreach (var location in evidence.Locations.Select(Normalize))
        {
            bounds.Include(ProjectModel(location));
        }

        foreach (var carrier in evidence.Locations
            .Select(location => NormalizeCarrier(location.Carrier))
            .Where(IsCircularCarrier)
            .GroupBy(carrier => carrier.Id)
            .Select(group => group.First()))
        {
            var circle = ReadCircularProjectionBasis(carrier);
            bounds.IncludeCircle(circle.Center, circle.Radius);
        }

        return bounds.ToRect();
    }

    private void Collect(IElement element)
    {
        if (element is not Relation relation)
        {
            return;
        }

        if (RecognizePointOnAnyCarrier(relation, out var point))
        {
            evidence.Locations.Add(point);
            return;
        }

        if (RecognizePointPair(relation, out var left, out var right))
        {
            evidence.Attachments.Add(new AttachmentEvidence(left, right));
        }

        Collect(relation.Left);
        Collect(relation.Right);
    }

    private static bool RecognizePointPair(
        Relation relation,
        out CarrierLocation left,
        out CarrierLocation right)
    {
        if (relation.Left is Relation leftPoint &&
            relation.Right is Relation rightPoint &&
            RecognizePointOnAnyCarrier(leftPoint, out left) &&
            RecognizePointOnAnyCarrier(rightPoint, out right))
        {
            return true;
        }

        left = default;
        right = default;
        return false;
    }

    private static bool RecognizePointOnAnyCarrier(Relation relation, out CarrierLocation location)
    {
        if (relation.Left is CarrierLocation candidate &&
            relation.Right is Carrier carrier &&
            candidate.Carrier.SameIdentityAs(carrier))
        {
            location = candidate;
            return true;
        }

        location = default;
        return false;
    }

    private float SampleAngle() => (float)random.NextDouble() * MathF.PI * 2f;

    private static CarrierLocation ReadCircularOrigin(Carrier carrier) =>
        carrier.TryReadCircularOrigin(out var origin)
            ? origin
            : new CarrierLocation(Carrier.Unknown, 0);

    private static bool IsCircularCarrier(Carrier carrier) => carrier.TryReadCircularOrigin(out _);

    private static CarrierLocation Normalize(CarrierLocation location)
    {
        return location.Carrier.IsInverse
            ? new CarrierLocation(location.Carrier.Inverse(), checked(-location.Ticks))
            : location;
    }

    private static Carrier NormalizeCarrier(Carrier carrier) => carrier.IsInverse ? carrier.Inverse() : carrier;

    private static float Distance(SKPoint left, SKPoint right)
    {
        var dx = right.X - left.X;
        var dy = right.Y - left.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private sealed class ProjectionEvidence
    {
        public List<CarrierLocation> Locations { get; } = [];

        public List<AttachmentEvidence> Attachments { get; } = [];
    }

    private sealed class ModelBounds
    {
        private bool hasPoint;
        private float left;
        private float top;
        private float right;
        private float bottom;

        public void Include(SKPoint point)
        {
            if (!hasPoint)
            {
                left = right = point.X;
                top = bottom = point.Y;
                hasPoint = true;
                return;
            }

            left = MathF.Min(left, point.X);
            top = MathF.Min(top, point.Y);
            right = MathF.Max(right, point.X);
            bottom = MathF.Max(bottom, point.Y);
        }

        public void IncludeCircle(SKPoint center, float radius)
        {
            Include(new SKPoint(center.X - radius, center.Y - radius));
            Include(new SKPoint(center.X + radius, center.Y + radius));
        }

        public SKRect ToRect()
        {
            if (!hasPoint)
            {
                return new SKRect(-1, -1, 1, 1);
            }

            if (Math.Abs(right - left) <= 0.001f)
            {
                left -= 1f;
                right += 1f;
            }

            if (Math.Abs(bottom - top) <= 0.001f)
            {
                top -= 1f;
                bottom += 1f;
            }

            return new SKRect(left, top, right, bottom);
        }
    }

    private sealed class ViewportTransform
    {
        private readonly float scale;
        private readonly SKPoint offset;

        private ViewportTransform(float scale, SKPoint offset)
        {
            this.scale = scale;
            this.offset = offset;
        }

        public static ViewportTransform From(SKRect modelBounds, SKRect viewport)
        {
            var margin = MathF.Min(36f, MathF.Min(viewport.Width, viewport.Height) * 0.12f);
            var innerWidth = Math.Max(1f, viewport.Width - margin * 2f);
            var innerHeight = Math.Max(1f, viewport.Height - margin * 2f);
            var scale = MathF.Min(innerWidth / modelBounds.Width, innerHeight / modelBounds.Height);
            var modelCenter = new SKPoint(
                modelBounds.Left + modelBounds.Width * 0.5f,
                modelBounds.Top + modelBounds.Height * 0.5f);
            var viewportCenter = new SKPoint(
                viewport.Left + viewport.Width * 0.5f,
                viewport.Top + viewport.Height * 0.5f);
            var offset = new SKPoint(
                viewportCenter.X - modelCenter.X * scale,
                viewportCenter.Y - modelCenter.Y * scale);
            return new ViewportTransform(scale, offset);
        }

        public SKPoint Apply(SKPoint point) => new(
            offset.X + point.X * scale,
            offset.Y + point.Y * scale);
    }

    private readonly record struct LinearBasis(SKPoint Origin, SKPoint TickVector);

    private readonly record struct CircularBasis(SKPoint Center, float Radius, long AnchorTicks, long PeriodTicks);

    private readonly record struct AttachmentEvidence(CarrierLocation Left, CarrierLocation Right)
    {
        public (CarrierLocation Circular, CarrierLocation Other) ForCircularCarrier(Carrier carrier)
        {
            if (Left.Carrier.SameIdentityAs(carrier))
            {
                return (Left, Right);
            }

            if (Right.Carrier.SameIdentityAs(carrier))
            {
                return (Right, Left);
            }

            return default;
        }
    }

    private readonly record struct LocationKey(ulong CarrierId, long Ticks)
    {
        public static LocationKey Of(CarrierLocation location) => new(NormalizeCarrier(location.Carrier).Id, location.Ticks);
    }
}
