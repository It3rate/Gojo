using System.Text;
using System.Text.RegularExpressions;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private static string RefinedPaletteLabel(string source, string previous) => source switch
    {
        "B13" => "Ink blue, terracotta, ivory & gold",
        "B14" => "Teal, plum, ivory & gold",
        _ => previous
    };

    // A display-only interpretation of existing native colour selections.
    // Source names select presentation recipes; they do not supply geometry,
    // native grouping, connections or occupied-area meaning.
    private static string RestyleRefinedSvg(string svg)
    {
        var root = Regex.Match(svg, @"<svg\b[^>]*>", RegexOptions.CultureInvariant);
        if (!root.Success) return svg;
        var label = Regex.Match(root.Value, """\baria-label=(['"])(?<source>B\d{2}(?:\.B)?)(?=\s|&|['"])[^'"]*\1""",
            RegexOptions.CultureInvariant);
        if (!label.Success) return svg;
        var source = label.Groups["source"].Value;
        var opening = Regex.Replace(root.Value, """\sdata-source=(['"])[^'"]*\1""", "", RegexOptions.CultureInvariant);
        opening = opening.Insert(opening.Length - 1, $" data-source='{source}'");
        svg = svg[..root.Index] + opening + svg[(root.Index + root.Length)..];

        // A saved SVG can be repainted repeatedly without accumulating blocks.
        svg = Regex.Replace(svg, """<style\s+id=['"]refined-separation['"]\s*>[\s\S]*?</style>""",
            "", RegexOptions.CultureInvariant);
        svg = Regex.Replace(svg, """<style\s+data-refined-separation=['"]true['"]\s*>[\s\S]*?</style>""",
            "", RegexOptions.CultureInvariant);
        if (source is not ("B10" or "B11" or "B13" or "B14")) return svg;

        var scope = $"[data-refined][data-source='{source}']";
        var css = new StringBuilder();
        if (source is "B13" or "B14")
        {
            var colours = source == "B13"
                ? new[] { "#244954", "#B65D45", "#F6EACD", "#D3A84C" }
                : new[] { "#245B63", "#6A3D65", "#F6EACD", "#D3A84C" };
            css.AppendLine($"{scope} .field{{opacity:1!important}}");
            css.AppendLine($"{scope} .field .mass[data-colour-group]{{opacity:1!important}}");
            css.AppendLine($"{scope} .field .edge{{stroke:#192F36;stroke-width:calc(var(--band) + 4px)}}");
            css.AppendLine($"{scope} .field .keyline{{stroke:#FFF5DE;stroke-width:calc(var(--band) + 2.3px)}}");
            css.AppendLine($"{scope} .motif .edge{{stroke:#192F36;stroke-width:calc(var(--band) * .55 + 3.5px)}}");
            css.AppendLine($"{scope} .motif .keyline{{stroke:#FFF5DE;stroke-width:calc(var(--band) * .55 + 2px)}}");
            css.AppendLine($"{scope} .detail{{opacity:.8!important}}");
            for (var group = 0; group < colours.Length; group++)
                css.AppendLine($"{scope} [data-colour-group='{group}']{{--colour:{colours[group]}!important}}");
        }
        else
        {
            // The fine flower is carried by a stronger continuous course.
            // These are existing source paths; no additional rail is invented.
            var fine = scope + "[data-focus=detail]";
            css.AppendLine($"{fine} .field{{opacity:1!important}}");
            css.AppendLine($"{fine} .field .mass{{opacity:0!important}}");
            css.AppendLine($"{fine} .field .edge{{stroke:var(--ink);stroke-width:calc(var(--band) * 1.65 + 3.4px)}}");
            css.AppendLine($"{fine} .field .keyline{{stroke:var(--pale);stroke-width:calc(var(--band) * 1.65 + 1.8px)}}");
            css.AppendLine($"{fine} .field .pigment{{stroke-width:calc(var(--band) * 1.65)}}");
            css.AppendLine($"{fine} .motif{{opacity:1!important}}");
            css.AppendLine($"{fine} .detail{{opacity:.72!important}}");
        }

        var end = svg.IndexOf("</style>", StringComparison.Ordinal);
        if (end < 0) return svg;
        end += "</style>".Length;
        return svg.Insert(end, "<style data-refined-separation='true'>\n" + css + "</style>");
    }
}
