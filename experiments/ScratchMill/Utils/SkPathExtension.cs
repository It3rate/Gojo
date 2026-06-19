using SkiaSharp;
using System.Diagnostics;
using System.Text;
using static SkiaSharp.SKPath;

namespace ScratchMill.Utils
{
    public static class SkPathExtension
    {
        public static bool IsClosed(this SKPath path, int contourIndex)
        {
            using (var iterator = path.CreateRawIterator())
            {
                int currentContour = -1;
                SKPathVerb verb;
                SKPoint[] points = new SKPoint[4];

                while ((verb = iterator.Next(points)) != SKPathVerb.Done)
                {
                    if (verb == SKPathVerb.Move)
                    {
                        currentContour++;
                    }
                    if (currentContour == contourIndex && verb == SKPathVerb.Close)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void WriteSvgToDisk(this SKPath skpath, int id, string type)
        {
            SKRect svgBounds = skpath.Bounds;

            float padding = Math.Max(svgBounds.Width, svgBounds.Height) * 0.1f;
            float viewBoxMinX = svgBounds.Left - padding;
            float viewBoxMinY = svgBounds.Top - padding;
            float viewBoxWidth = svgBounds.Width + 2 * padding;
            float viewBoxHeight = svgBounds.Height + 2 * padding;

            int svgWidth = 800;
            int svgHeight = (int)(svgWidth * (viewBoxHeight / viewBoxWidth));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{svgWidth}"" height=""{svgHeight}"" viewBox=""{viewBoxMinX} {viewBoxMinY} {viewBoxWidth} {viewBoxHeight}"" fill=""none"">");
            sb.AppendLine($@"<path d=""{skpath.ToSvgPathData()}"" fill=""Beige"" stroke=""Red"" stroke-width=""0.5"" />");
            sb.AppendLine("</svg>");

            var outputDirectory = Path.Combine(AppContext.BaseDirectory, "SvgDebug");
            Directory.CreateDirectory(outputDirectory);
            var path = Path.Combine(outputDirectory, $"{type}_{id}.svg");
            File.WriteAllText(path, sb.ToString());
        }


        public static SKPath CloneWithTransform(this SKPath pathIn, Func<SKPoint, SKPoint> transform)
        {
            SKPath pathOut = new SKPath();

            using (SKPath.RawIterator iterator = pathIn.CreateRawIterator())
            {
                SKPoint[] points = new SKPoint[4];
                SKPathVerb pathVerb = SKPathVerb.Move;
                SKPoint firstPoint = new SKPoint();
                SKPoint lastPoint = new SKPoint();

                while ((pathVerb = iterator.Next(points)) != SKPathVerb.Done)
                {
                    switch (pathVerb)
                    {
                        case SKPathVerb.Move:
                            pathOut.MoveTo(transform(points[0]));
                            firstPoint = lastPoint = points[0];
                            break;

                        case SKPathVerb.Line:
                            SKPoint[] linePoints = Interpolate(points[0], points[1]);

                            foreach (SKPoint pt in linePoints)
                            {
                                pathOut.LineTo(transform(pt));
                            }

                            lastPoint = points[1];
                            break;

                        case SKPathVerb.Cubic:
                            SKPoint[] cubicPoints = FlattenCubic(points[0], points[1], points[2], points[3]);

                            foreach (SKPoint pt in cubicPoints)
                            {
                                pathOut.LineTo(transform(pt));
                            }

                            lastPoint = points[3];
                            break;

                        case SKPathVerb.Quad:
                            SKPoint[] quadPoints = FlattenQuadratic(points[0], points[1], points[2]);

                            foreach (SKPoint pt in quadPoints)
                            {
                                pathOut.LineTo(transform(pt));
                            }

                            lastPoint = points[2];
                            break;

                        case SKPathVerb.Conic:
                            SKPoint[] conicPoints = FlattenConic(points[0], points[1], points[2], iterator.ConicWeight());

                            foreach (SKPoint pt in conicPoints)
                            {
                                pathOut.LineTo(transform(pt));
                            }

                            lastPoint = points[2];
                            break;

                        case SKPathVerb.Close:
                            SKPoint[] closePoints = Interpolate(lastPoint, firstPoint);

                            foreach (SKPoint pt in closePoints)
                            {
                                pathOut.LineTo(transform(pt));
                            }

                            firstPoint = lastPoint = new SKPoint(0, 0);
                            pathOut.Close();
                            break;
                    }
                }
            }
            return pathOut;
        }

        static SKPoint[] Interpolate(SKPoint pt0, SKPoint pt1)
        {
            int count = (int)Math.Max(1, Length(pt0, pt1));
            SKPoint[] points = new SKPoint[count];

            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / count;
                float x = (1 - t) * pt0.X + t * pt1.X;
                float y = (1 - t) * pt0.Y + t * pt1.Y;
                points[i] = new SKPoint(x, y);
            }

            return points;
        }

        static SKPoint[] FlattenCubic(SKPoint pt0, SKPoint pt1, SKPoint pt2, SKPoint pt3)
        {
            int count = (int)Math.Max(1, Length(pt0, pt1) + Length(pt1, pt2) + Length(pt2, pt3));
            SKPoint[] points = new SKPoint[count];

            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / count;
                float x = (1 - t) * (1 - t) * (1 - t) * pt0.X +
                          3 * t * (1 - t) * (1 - t) * pt1.X +
                          3 * t * t * (1 - t) * pt2.X +
                          t * t * t * pt3.X;
                float y = (1 - t) * (1 - t) * (1 - t) * pt0.Y +
                          3 * t * (1 - t) * (1 - t) * pt1.Y +
                          3 * t * t * (1 - t) * pt2.Y +
                          t * t * t * pt3.Y;
                points[i] = new SKPoint(x, y);
            }

            return points;
        }

        static SKPoint[] FlattenQuadratic(SKPoint pt0, SKPoint pt1, SKPoint pt2)
        {
            int count = (int)Math.Max(1, Length(pt0, pt1) + Length(pt1, pt2));
            SKPoint[] points = new SKPoint[count];

            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / count;
                float x = (1 - t) * (1 - t) * pt0.X + 2 * t * (1 - t) * pt1.X + t * t * pt2.X;
                float y = (1 - t) * (1 - t) * pt0.Y + 2 * t * (1 - t) * pt1.Y + t * t * pt2.Y;
                points[i] = new SKPoint(x, y);
            }

            return points;
        }

        static SKPoint[] FlattenConic(SKPoint pt0, SKPoint pt1, SKPoint pt2, float weight)
        {
            int count = (int)Math.Max(1, Length(pt0, pt1) + Length(pt1, pt2));
            SKPoint[] points = new SKPoint[count];

            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / count;
                float denominator = (1 - t) * (1 - t) + 2 * weight * t * (1 - t) + t * t;
                float x = (1 - t) * (1 - t) * pt0.X + 2 * weight * t * (1 - t) * pt1.X + t * t * pt2.X;
                float y = (1 - t) * (1 - t) * pt0.Y + 2 * weight * t * (1 - t) * pt1.Y + t * t * pt2.Y;
                x /= denominator;
                y /= denominator;
                points[i] = new SKPoint(x, y);
            }

            return points;
        }
        static double Length(SKPoint pt0, SKPoint pt1)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt0.X, 2) + Math.Pow(pt1.Y - pt0.Y, 2));
        }




        /// <summary>
        /// Make segments smaller to accommodate autolevel.
        /// </summary>
        public static SKPath EnforceMaxSegLength(this SKPath path, float maxLength = 1f)
        {
            // Create a new path to store the subdivided result
            var result = new SKPath();
            if (path == null || path.IsEmpty)
            {
                Trace.WriteLine($"Path is null or empty.");
            }

            //Trace.WriteLine($"Path: PointCount={path.PointCount}, VerbCount={path.VerbCount}, IsClosed={path.IsClosed(0)}");
            using (RawIterator iterator = path.CreateRawIterator())
            {
                SKPoint[] points = new SKPoint[4];
                SKPoint lastPoint = SKPoint.Empty;
                SKPoint firstPoint = SKPoint.Empty;
                bool firstMove = true;
                var verb = iterator.Next(points);
                while (verb != SKPathVerb.Done)
                {
                    switch (verb)
                    {
                        case SKPathVerb.Move:
                            result.MoveTo(points[0]);
                            lastPoint = points[0];
                            firstPoint = points[0];
                            firstMove = false;
                            break;
                        case SKPathVerb.Line:
                            if (!firstMove)
                            {
                                //result.LineTo(points[1]);
                                result.AddSubdividedLine(lastPoint, points[1], maxLength);
                                lastPoint = points[1];
                            }
                            else
                            {//shouldn't happen?
                                result.MoveTo(points[0]);
                                lastPoint = points[0];
                                firstPoint = points[0];
                                firstMove = false;
                            }
                            break;
                        case SKPathVerb.Quad:
                        case SKPathVerb.Cubic:
                        case SKPathVerb.Conic:
                            var tempPath = new SKPath();
                            tempPath.MoveTo(lastPoint); // need the Zero point to reduce curve
                            if (verb == SKPathVerb.Quad)
                            {
                                tempPath.QuadTo(points[1], points[2]);
                                lastPoint = points[2];
                            }
                            else if (verb == SKPathVerb.Cubic)
                            {
                                tempPath.CubicTo(points[1], points[2], points[3]);
                                lastPoint = points[3];
                            }
                            else // CONIC
                            {
                                tempPath.ConicTo(points[1], points[2], 1); // Don't have the w parameter here, so default
                                lastPoint = points[2];
                            }
                            tempPath.FlattenPathContour(result, maxLength);// 5 / 30f); // should be max curvature too
                            result.LineTo(lastPoint);
                            break;
                        case SKPathVerb.Close:
                        case SKPathVerb.Done:
                            if (!firstMove)
                            {
                                result.Close();
                            }
                            break;
                    }
                    verb = iterator.Next(points);
                }
            }
            return result;
        }
        public static void AddSubdividedLine(this SKPath newPath, SKPoint start, SKPoint end, float maxLength)
        {
            var segment = end - start;
            float length = segment.Length;

            if (length <= maxLength)
            {
                newPath.LineTo(end);
            }
            else
            {
                int segmentCount = (int)Math.Ceiling(length / maxLength);
                float tStep = 1f / segmentCount;

                for (int k = 1; k < segmentCount; k++)
                {
                    float t = k * tStep;
                    var interpolated = start + segment.Multiply(t);
                    newPath.LineTo(interpolated);
                }
                newPath.LineTo(end);
            }
        }
        public static void FlattenPathContour(this SKPath addPath, SKPath resultPath, float maxLength)
        {
            //maxLength *= 0.5f;
            using (var pathMeasure = new SKPathMeasure(addPath, false, 1))
            {
                float length = pathMeasure.Length;
                int count = (int)(length / maxLength);

                var start = addPath.Points[0];
                var end = addPath.LastPoint;

                // add more points to sharp or long curves, will change to curve fitting eventually
                pathMeasure.GetPosition(length / 2f, out SKPoint mid);
                var segDist = start.DistanceTo(end);
                var distToLine = mid.PointDistanceToLine(start, end);
                var offsetRatio = distToLine / segDist;
                if (offsetRatio > 0.2f / 30f)
                {
                    count = Math.Max(count * 2, 4);
                }

                var step = length / (float)count;
                var distance = step;
                for (int i = 0; i < count; i++)
                {
                    if (pathMeasure.GetPosition(distance, out SKPoint position))
                    {
                        resultPath.LineTo(position);
                    }
                    distance += step;
                }
            }
        }


    }
}
