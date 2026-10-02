using System.Globalization;
using System.Security;
using System.Text;

namespace PredictorScratchpad;

/// <summary>Static rendering adapter over completed trace rows. Coordinates,
/// clipping and labels are display operations; no learner result is calculated here.</summary>
internal static class PredictionPlots
{
    internal static void Write(string outputDirectory)
    {
        var rows = File.ReadLines(Path.Combine(outputDirectory, "trace.csv")).Skip(1)
            .Select(line => line.Split(','))
            .Where(fields => fields.Length >= 8 && fields[1] == "103" &&
                fields[5] is "hold" or "adaptive" or "running-mean")
            .Select(fields => (Trial: fields[0], Arrival: int.Parse(fields[2], CultureInfo.InvariantCulture),
                Channel: int.Parse(fields[4], CultureInfo.InvariantCulture), Model: fields[5],
                Observed: double.Parse(fields[6], CultureInfo.InvariantCulture),
                Forecast: double.Parse(fields[7], CultureInfo.InvariantCulture)))
            .ToArray();
        (string Key, string Title)[] conditions =
        [
            ("ramp-plateau-cadence", "Ramp, plateau and changing arrival cadence"),
            ("smooth-turning", "Smooth turning, then period and amplitude drift"),
            ("mixed-anonymous-input", "Mixed anonymous input with delayed response"),
            ("shuffled-marginal-control", "Shuffled marginal control")
        ];
        const string observedColor = "#293846";
        const string adaptiveColor = "#bc355f";
        const string holdColor = "#c08516";
        const string meanColor = "#177a89";
        const double plotWidth = 570;
        const double plotHeight = 205;
        var clipped = 0;
        var svg = new StringBuilder();
        string F(double number) => number.ToString("0.##", CultureInfo.InvariantCulture);
        void Text(double x, double y, string text, int size = 13, string color = "#52616b", string extra = "") =>
            svg.Append($"<text x='{F(x)}' y='{F(y)}' font-size='{size}' fill='{color}' {extra}>{SecurityElement.Escape(text)}</text>");
        void Line(double x1, double y1, double x2, double y2, string color, string extra = "") =>
            svg.Append($"<line x1='{F(x1)}' y1='{F(y1)}' x2='{F(x2)}' y2='{F(y2)}' stroke='{color}' {extra}/>");
        svg.Append("<svg xmlns='http://www.w3.org/2000/svg' width='1400' height='1500' viewBox='0 0 1400 1500' role='img' aria-labelledby='plot-title plot-description'>");
        svg.Append("<title id='plot-title'>Gojo predictor: all four conditions, seed 103</title><desc id='plot-description'>Eight sensor panels compare observed input with prior adaptive, hold and running-mean forecasts. Every panel uses arrival 1 through 215 and raw sensor ticks 0 through 1000. Seed 103 is a fixed representative seed, not a selected best result.</desc>");
        svg.Append("<rect width='1400' height='1500' fill='#f5f3ee'/><g font-family='Segoe UI,Arial,sans-serif'>");
        Text(44, 39, "GOJO / PREDICTOR EXPERIMENT", 12, "#177a89", "font-weight='700' letter-spacing='1.4'");
        Text(44, 78, "Forecasts against arriving input", 31, observedColor, "font-weight='650'");
        Text(44, 105, "All four conditions · first protocol seed 103 · each forecast issued before its plotted observation", 15);
        (string Label, string Color, string Dash)[] legend =
        [
            ("Observed", observedColor, ""), ("Adaptive", adaptiveColor, ""),
            ("Hold", holdColor, "stroke-dasharray='5 3'"), ("Running mean", meanColor, "stroke-dasharray='9 4'")
        ];
        for (var index = 0; index < legend.Length; index++)
        {
            var x = 44 + index * 190;
            Line(x, 133, x + 34, 133, legend[index].Color, $"stroke-width='2.6' {legend[index].Dash}");
            Text(x + 43, 138, legend[index].Label, 14, observedColor);
        }

        for (var condition = 0; condition < conditions.Length; condition++)
        {
            var top = 190 + condition * 304;
            Text(44, top - 10, conditions[condition].Title, 18, observedColor, "font-weight='600'");
            for (var channel = 0; channel < 2; channel++)
            {
                var left = 91 + channel * 676;
                var plotTop = top + 27;
                var clip = $"clip-{condition}-{channel}";
                svg.Append($"<rect x='{left - 47}' y='{top + 1}' width='644' height='268' rx='10' fill='#ffffff'/>");
                Text(left, top + 17, $"SENSOR {(channel == 0 ? "A" : "B")} · RAW TICKS", 11, "#52616b", "font-weight='650' letter-spacing='.8'");
                svg.Append($"<defs><clipPath id='{clip}'><rect x='{left}' y='{plotTop}' width='{F(plotWidth)}' height='{F(plotHeight)}'/></clipPath></defs>");
                double X(int arrival) => left + (arrival - 1) / 214.0 * plotWidth;
                double Y(double ticks) => plotTop + plotHeight - ticks / 1000.0 * plotHeight;
                foreach (var ticks in new[] { 0, 250, 500, 750, 1000 })
                {
                    Line(left, Y(ticks), left + plotWidth, Y(ticks), "#e6eaeb");
                    Text(left - 10, Y(ticks) + 4, ticks.ToString(CultureInfo.InvariantCulture), 11, "#78858c", "text-anchor='end'");
                }
                foreach (var arrival in new[] { 1, 50, 100, 150, 200, 215 })
                {
                    Line(X(arrival), plotTop, X(arrival), plotTop + plotHeight, "#edf0f1");
                    Text(X(arrival), plotTop + plotHeight + 18, arrival.ToString(CultureInfo.InvariantCulture), 11, "#78858c", "text-anchor='middle'");
                }
                Text(left + plotWidth, plotTop + plotHeight + 34, "arrival", 11, "#78858c", "text-anchor='end'");
                var panel = rows.Where(row => row.Trial == conditions[condition].Key && row.Channel == channel)
                    .OrderBy(row => row.Arrival).ToArray();
                if (panel.Length == 0)
                {
                    Text(left + plotWidth / 2, plotTop + plotHeight / 2, "No trace rows available", 16, "#78858c", "text-anchor='middle'");
                    continue;
                }
                void Trace(string model, bool actual, string color, double width, string dash = "")
                {
                    var points = new StringBuilder();
                    foreach (var row in panel.Where(row => row.Model == model && row.Arrival is >= 1 and <= 215))
                    {
                        var value = actual ? row.Observed : row.Forecast;
                        if (value is < 0 or > 1000) clipped++;
                        points.Append(F(X(row.Arrival))).Append(',').Append(F(Y(value))).Append(' ');
                    }
                    svg.Append($"<polyline points='{points}' fill='none' stroke='{color}' stroke-width='{F(width)}' {dash} clip-path='url(#{clip})' stroke-linejoin='round' stroke-linecap='round'/>");
                }
                Trace("running-mean", false, meanColor, 1.65, "stroke-dasharray='9 4'");
                Trace("hold", false, holdColor, 1.45, "stroke-dasharray='5 3'");
                Trace("hold", true, observedColor, 1.65);
                Trace("adaptive", false, adaptiveColor, 1.75);
            }
        }
        Line(44, 1404, 1356, 1404, "#d5dcde");
        Text(44, 1431, "Same axes throughout: arrival 1–215; nominal sensor range 0–1000. Warm-up arrivals are shown as well.", 13);
        Text(44, 1453, "Overlapping traces can conceal one another. This is one fixed seed for inspection; compare all seeds in the numerical report.", 13);
        Text(44, 1475, clipped == 0 ? "No plotted values extend beyond the fixed vertical range. Rendering reads completed trace rows only."
            : $"{clipped} plotted points extend beyond 0–1000 and are clipped at the panel boundary; original values remain in trace.csv.", 13);
        svg.Append("</g></svg>");
        File.WriteAllText(Path.Combine(outputDirectory, "forecast-traces.svg"), svg.ToString(), new UTF8Encoding(false));
    }
}
