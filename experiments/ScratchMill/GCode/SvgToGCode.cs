using SkiaSharp;
using System.Text;
using System.Text.RegularExpressions;

namespace ScratchMill.GCode
{
    public class SvgToGCode : GCodeConverter
    {
        protected string _resolution => GCodeWriter._resolution;
        private Regex _svgMotionRegex;
        private SKRect _bounds = SKRect.Empty;
        public SvgToGCode()
        {
            string pattern = @"(?<command>[QLMZ])((?:[\d.e,-]*\s*)*)";
            _svgMotionRegex = new Regex(pattern, RegexOptions.Compiled);
        }
        public void Reset()
        {
            _bounds = SKRect.Empty;
        }
        public string ConvertToGCode(string svg, IKcData elementData, SKMatrix transform)
        {
            if (elementData.Svg == null)
            {
                //var svg = elementData.Path.ToSvgPathData();
                //track.Path.WriteSvgToDisk(index++, track.PassKind.ToString());
            }
            var path = elementData.Svg;
            var scaleX = transform.ScaleX;
            var scaleY = transform.ScaleY;
            bool addTailOverlap = elementData.OverlapTail;
            bool isClearingOperation = elementData.PassKind == PassKind.SolderMask;

            var sb = new StringBuilder();
            var defaultZMillDepth = -0.04f;
            var millDepth = (elementData != null) ?
                "(depthIndex" + (char)elementData.PassKind + elementData.ContourIndex + ")" :
                defaultZMillDepth.ToString(_resolution);


            var matches = _svgMotionRegex.Matches(svg);
            double endX = 0;
            double endY = 0;
            double lastX = 0;
            double lastY = 0;
            string strX = "";
            string strY = "";
            var nums = new string[2];
            var segLen = 0.0;
            var lineLen = 0.0;
            var curAction = "";
            var prevAction = "";
            var overlaps = new List<string>();
            bool shapeStarted = false;
            bool doneFirstMove = false;
            foreach (Match match in matches)
            {
                var command = match.Groups["command"].Value;

                var numbersString = match.Groups[1].Value.Trim();
                var numbers = numbersString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (numbers.Length > 0)
                {
                    var startIndex = command == "Q" ? 2 : 0;
                    endX = double.Parse(numbers[0 + startIndex]) / scaleX;
                    endY = double.Parse(numbers[1 + startIndex]) / scaleY;
                    _bounds = SKRect.Union(_bounds, new SKRect((float)endX, (float)endY, (float)endX, (float)endY));
                    strX = (endX).ToString(_resolution);
                    strY = (endY).ToString(_resolution);
                    if (!shapeStarted)
                    {
                        nums[0] = strX;
                        nums[1] = strY;
                        shapeStarted = true;
                    }
                    else
                    {
                        segLen = Math.Sqrt((endX - lastX) * (endX - lastX) + (endY - lastY) * (endY - lastY));
                    }

                }

                switch (command)
                {
                    case "Q": // these come from arcs, so this code assumes arcs. Finer grained curves needs polylines in gCode.
                        if (segLen < 0.5)
                        {
                            curAction = $"G01 X{strX} Y{strY}";
                        }
                        else
                        {
                            var midX = double.Parse(numbers[0]) / scaleX;
                            var midY = double.Parse(numbers[1]) / scaleY;
                            bool isCw = IsClockwiseArc(lastX, lastY, midX, midY, endX, endY);
                            var strRot = isCw ? "G02" : "G03";

                            double radius;
                            double xc, yc;
                            try
                            {
                                // Sample a point on the quadratic Bézier at t=0.5 for arc fitting
                                double t = 0.5;
                                double mtX = (1 - t) * (1 - t) * lastX + 2 * (1 - t) * t * midX + t * t * endX;
                                double mtY = (1 - t) * (1 - t) * lastY + 2 * (1 - t) * t * midY + t * t * endY;

                                // Fit a circular arc through start (lastX, lastY), mid (mtX, mtY), and end (endX, endY)
                                // Midpoints of chords
                                double m1x = (lastX + mtX) / 2;
                                double m1y = (lastY + mtY) / 2;
                                double m2x = (mtX + endX) / 2;
                                double m2y = (mtY + endY) / 2;

                                // Slopes of chords
                                double chord1Slope = (mtX - lastX) != 0 ? (mtY - lastY) / (mtX - lastX) : double.PositiveInfinity;
                                double chord2Slope = (endX - mtX) != 0 ? (endY - mtY) / (endX - mtX) : double.PositiveInfinity;

                                // Perpendicular slopes
                                double perp1Slope = chord1Slope == 0 ? double.PositiveInfinity : (chord1Slope == double.PositiveInfinity ? 0 : -1 / chord1Slope);
                                double perp2Slope = chord2Slope == 0 ? double.PositiveInfinity : (chord2Slope == double.PositiveInfinity ? 0 : -1 / chord2Slope);

                                // Center coordinates
                                if (perp1Slope == perp2Slope) // Parallel or collinear, fallback to linear
                                {
                                    curAction = $"G01 X{strX} Y{strY}";
                                    break;
                                }
                                else if (perp1Slope == double.PositiveInfinity)
                                {
                                    xc = m1x;
                                    yc = perp2Slope * (xc - m2x) + m2y;
                                }
                                else if (perp2Slope == double.PositiveInfinity)
                                {
                                    xc = m2x;
                                    yc = perp1Slope * (xc - m1x) + m1y;
                                }
                                else
                                {
                                    xc = (perp1Slope * m1x - perp2Slope * m2x + m2y - m1y) / (perp1Slope - perp2Slope);
                                    yc = perp1Slope * (xc - m1x) + m1y;
                                }
                                radius = Math.Sqrt(Math.Pow(xc - lastX, 2) + Math.Pow(yc - lastY, 2));

                                // Verify radius against end point for consistency
                                double radToEnd = Math.Sqrt(Math.Pow(xc - endX, 2) + Math.Pow(yc - endY, 2));
                                if (Math.Abs(radius - radToEnd) > 0.01)
                                {
                                    curAction = $"G01 X{strX} Y{strY}"; // Fallback to line
                                    break;
                                }

                                // Check if radius is reasonable
                                if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0.001)
                                {
                                    curAction = $"G01 X{strX} Y{strY}"; // Fallback/close enough to a line
                                    break;
                                }
                            }
                            catch
                            {
                                curAction = $"G01 X{strX} Y{strY}"; // Fallback to connected
                                break;
                            }

                            if (curAction == "")
                            {
                                curAction = $"{strRot} X{strX} Y{strY} R{radius.ToString(_resolution)}"; // arc
                            }
                        }
                        break;
                    case "L":
                        curAction = $"G01 X{strX} Y{strY}";
                        break;
                    case "M":
                        segLen = 0;
                        if (isClearingOperation && doneFirstMove) // for clearing, don't lift the z every move.
                        {
                            curAction = $"G01 X{strX} Y{strY}";
                        }
                        else
                        {
                            sb.AppendLine(@$"
(SafeZMove)
G00 X{strX} Y{strY}
(ZSpeed)
G01 Z{millDepth}
;(Pause1)
(HorzScratchSpeed)");
                        }
                        doneFirstMove = true;
                        break;

                    case "Z": // end shape
                        foreach (var overlap in overlaps)
                        {
                            sb.AppendLine(overlap);
                        }
                        curAction = "";
                        prevAction = "";
                        overlaps.Clear();
                        lineLen = 0;
                        segLen = 0;
                        shapeStarted = false;
                        break;
                }
                if (curAction != "" && curAction != prevAction)
                {
                    sb.AppendLine(curAction);
                    if (addTailOverlap && lineLen < 3) // make overlap distance based eventually
                    {
                        overlaps.Add(curAction);
                    }
                    lineLen += segLen;
                }
                lastX = endX;
                lastY = endY;
                prevAction = curAction;
                curAction = "";
            }
            var result = sb.ToString();
            //_sbGCode.Append(result);

            if (elementData != null)
            {
                elementData.Svg = svg;
                elementData.GCode = result;
            }
            return result;
        }
        private bool IsClockwiseArc(double p0x, double p0y, double p1x, double p1y, double p2x, double p2y)
        {
            var dx1 = p1x - p0x;
            var dy1 = p1y - p0y;
            var dx2 = p2x - p0x;
            var dy2 = p2y - p0y;

            var crossProduct = dx1 * dy2 - dy1 * dx2;
            return crossProduct <= 0;
        }
    }
}
