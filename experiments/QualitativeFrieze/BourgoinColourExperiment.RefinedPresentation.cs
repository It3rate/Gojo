using System.Text;
using System.Text.RegularExpressions;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private static string RefinedPaletteLabel(string source, string mode) => RefinedPalette(source, mode).Name;

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
        var focus = Regex.Match(root.Value, """\bdata-focus=['"](field|motif|detail)['"]""", RegexOptions.CultureInvariant);
        var mode = focus.Success ? focus.Groups[1].Value : "motif";
        var palette = RefinedPalette(source, mode);
        var opening = Regex.Replace(root.Value, """\sdata-(?:source|palette)=(['"])[^'"]*\1""", "", RegexOptions.CultureInvariant);
        foreach (var (name, colour) in new[] { ("ground", palette.Ground), ("field", palette.Field), ("motif", palette.Motif),
            ("detail", palette.Detail), ("gold", palette.Gold), ("pale", palette.Pale), ("ink", palette.Ink) })
            opening = Regex.Replace(opening, "--" + name + """:([^;'\"]+)""", "--" + name + ":" + colour, RegexOptions.CultureInvariant);
        opening = opening.Insert(opening.Length - 1, $" data-source='{source}' data-palette='{H(palette.Name)}'");
        svg = svg[..root.Index] + opening + svg[(root.Index + root.Length)..];

        // A saved SVG can be repainted repeatedly without accumulating blocks.
        svg = Regex.Replace(svg, """<style\s+id=['"]refined-separation['"]\s*>[\s\S]*?</style>""",
            "", RegexOptions.CultureInvariant);
        svg = Regex.Replace(svg, """<style\s+data-refined-separation=['"]true['"]\s*>[\s\S]*?</style>""",
            "", RegexOptions.CultureInvariant);
        if (source is not ("B10" or "B11" or "B13" or "B14" or "B32")) return svg;

        var scope = $"[data-refined][data-source='{source}']";
        var css = new StringBuilder();
        if (source is "B13" or "B14")
        {
            css.AppendLine($"{scope} .field{{opacity:1!important}}");
            css.AppendLine($"{scope} .field .mass[data-colour-group]{{opacity:1!important}}");
            css.AppendLine($"{scope}[data-view=ornament] .base{{opacity:0}}");
            css.AppendLine($"{scope} .field .edge{{stroke:var(--pale);stroke-width:calc(var(--band) * .2 + 1.6px)}}");
            css.AppendLine($"{scope} .field .keyline{{stroke:var(--ink);stroke-width:calc(var(--band) * .2 + .95px)}}");
            css.AppendLine($"{scope} .field .pigment{{stroke:var(--ink);stroke-width:calc(var(--band) * .2 + .55px)}}");
            css.AppendLine($"{scope} .motif{{opacity:.82!important}}");
            css.AppendLine($"{scope} .motif [data-colour-group]{{--colour:var(--gold)!important}}");
            css.AppendLine($"{scope} .motif .edge{{stroke:var(--ink);stroke-width:calc(var(--band) * .35 + 1.4px)}}");
            css.AppendLine($"{scope} .motif .keyline{{stroke:var(--pale);stroke-width:calc(var(--band) * .35 + .7px)}}");
            css.AppendLine($"{scope} .motif .pigment{{stroke-width:calc(var(--band) * .35)}}");
            css.AppendLine($"{scope} .detail{{opacity:.45!important}}");
            css.AppendLine($"{scope} .detail .edge,{scope} .detail .keyline{{stroke:none}}");
            css.AppendLine($"{scope} .detail .pigment{{stroke:var(--ink);stroke-width:calc(var(--band) * .12 + .45px)}}");
            for (var group = 0; group < 4; group++)
                css.AppendLine($"{scope} .field [data-colour-group='{group}']{{--colour:var(--{(group % 2 == 0 ? "field" : "motif")})!important}}");
        }
        else if (source == "B32")
        {
            // The displayed band width follows the imported guide's screen
            // reach. It neither changes a native gap nor establishes a join.
            var breadth = mode == "field" ? 5.4 : mode == "motif" ? 5.0 : 4.6;
            css.AppendLine($"{scope} .field,{scope} .motif{{opacity:1!important}}");
            css.AppendLine($"{scope}[data-view=ornament] .base{{opacity:0}}");
            css.AppendLine($"{scope} .field{{stroke-linecap:square;stroke-linejoin:miter;stroke-miterlimit:3}}");
            css.AppendLine($"{scope} .field .edge{{stroke:var(--ink);stroke-width:calc(var(--reach-px) * {F(breadth)})}}");
            css.AppendLine($"{scope} .field .keyline{{stroke:var(--gold);stroke-width:calc(var(--reach-px) * {F(breadth * .74)})}}");
            css.AppendLine($"{scope} .field .pigment{{stroke:var(--field);stroke-width:calc(var(--reach-px) * {F(breadth * .48)})}}");
            css.AppendLine($"{scope} .motif .mass,{scope} .detail .mass{{fill:none}}");
            css.AppendLine($"{scope} .motif .edge{{stroke-width:calc(var(--reach-px) * .8)}}");
            css.AppendLine($"{scope} .motif .keyline{{stroke-width:calc(var(--reach-px) * .5)}}");
            css.AppendLine($"{scope} .motif .pigment{{stroke:var(--motif);stroke-width:calc(var(--reach-px) * .25)}}");
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
