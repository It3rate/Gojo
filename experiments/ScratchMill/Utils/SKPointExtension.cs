
using ScratchMill.KiCad;
using SkiaSharp;

namespace ScratchMill.Utils
{
    public static class SkPointExtension
    {
        public static SKPoint SKPoint(this KcPosition pos) =>
            new SKPoint((float)pos.X, (float)pos.Y);
        public static SKPoint SKPointTransformed(this SKPoint pos, SKMatrix matrix)
        {
            var result = matrix.MapPoint(pos);
            //var result = new SKPoint(
            //	(pos.X + matrix.TransX) * matrix.ScaleX,
            //	(pos.Y + matrix.TransY) * matrix.ScaleY);
            return result;
        }
        //public static SKPoint SKPointTransformed(this SKPoint pos, SKMatrix matrix) => matrix.MapPoint(pos);
        public static SKPoint SKPointTransformed(this KcPosition pos, SKMatrix matrix) =>
            SKPointTransformed(pos.SKPoint(), matrix);

        public static SKPoint[] SKPointsTransformed(this List<KcPosition> positions, SKMatrix matrix)
        {
            var result = new List<SKPoint>();
            foreach (var position in positions)
            {
                result.Add(position.SKPointTransformed(matrix));
            }
            return result.ToArray();
        }

        public static float PointDistanceToLine(this SKPoint point, SKPoint start, SKPoint end)
        {
            return point.DistanceToLine(start, end);
        }

        public static float DistanceTo(this SKPoint point, SKPoint other)
        {
            var dx = point.X - other.X;
            var dy = point.Y - other.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        public static SKPoint Multiply(this SKPoint point, float value) =>
            new(point.X * value, point.Y * value);

        public static float DistanceToLine(this SKPoint point, SKPoint start, SKPoint end)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared == 0)
            {
                return point.DistanceTo(start);
            }

            var t = Math.Clamp(((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared, 0f, 1f);
            var projection = new SKPoint(start.X + t * dx, start.Y + t * dy);
            return point.DistanceTo(projection);
        }

        public static SKPoint CalculateCircleCenter(this SKPoint start, SKPoint mid, SKPoint end)
        {
            var mid1 = new SKPoint((start.X + mid.X) / 2, (start.Y + mid.Y) / 2);
            var mid2 = new SKPoint((end.X + mid.X) / 2, (end.Y + mid.Y) / 2);

            var slope1 = -(start.X - mid.X) / (start.Y - mid.Y);
            var slope2 = -(end.X - mid.X) / (end.Y - mid.Y);

            var c1 = mid1.Y - slope1 * mid1.X;
            var c2 = mid2.Y - slope2 * mid2.X;

            var centerX = (c2 - c1) / (slope1 - slope2);
            var centerY = slope1 * centerX + c1;

            return new SKPoint(centerX, centerY);
        }

        public static float CalculateAngle(this SKPoint center, SKPoint point)
        {
            return (float)(Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI);
        }

    }
}
