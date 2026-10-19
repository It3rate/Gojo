using System.Text;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private static void AppendQuietSerifStyle(StringBuilder css, string scope)
    {
        // Each native colour group selects one actual closed serif-I chamber.
        // Its mass is the coloured region. A coloured centred boundary stroke
        // would enlarge that region into its neighbour, so draw only a narrow
        // neutral dividing line and leave the source pigment pass unpainted.
        css.AppendLine($"{scope} .field{{opacity:1!important}}");
        css.AppendLine($"{scope} .field .mass[data-colour-group]{{opacity:1!important}}");
        css.AppendLine($"{scope}[data-view=ornament] .base{{opacity:0}}");
        css.AppendLine($"{scope} .field .edge{{stroke:var(--ink);stroke-width:1.45px}}");
        css.AppendLine($"{scope} .field .keyline,{scope} .field .pigment{{stroke:none}}");

        // Each level has its own palette. The overall reading separates the
        // four chambers; finer palettes bring their field colours closer and
        // give the retained leaves and veins the stronger contrast instead.
        css.AppendLine($"{scope} .motif [data-colour-group]{{--colour:var(--pale)!important}}");
        css.AppendLine($"{scope} .motif .edge{{stroke:var(--ink);stroke-width:calc(var(--band) * .11 + 1.1px)}}");
        css.AppendLine($"{scope} .motif .keyline{{stroke:var(--pale);stroke-width:calc(var(--band) * .11 + .55px)}}");
        css.AppendLine($"{scope} .motif .pigment{{stroke-width:calc(var(--band) * .11)}}");
        css.AppendLine($"{scope}[data-focus=field] .motif{{opacity:.2!important}}");
        css.AppendLine($"{scope}[data-focus=motif] .motif{{opacity:.95!important}}");
        css.AppendLine($"{scope}[data-focus=detail] .motif{{opacity:1!important}}");
        css.AppendLine($"{scope}[data-focus=detail] .field .edge{{stroke-width:.85px;opacity:.4}}");
        css.AppendLine($"{scope} .detail .edge,{scope} .detail .keyline{{stroke:none}}");
        css.AppendLine($"{scope} .detail .pigment{{stroke:var(--ink);stroke-width:.85px}}");
        css.AppendLine($"{scope}[data-focus=field] .detail{{opacity:.2!important}}");
        css.AppendLine($"{scope}[data-focus=motif] .detail{{opacity:.65!important}}");
        css.AppendLine($"{scope}[data-focus=detail] .detail{{opacity:.95!important}}");
        css.AppendLine($"{scope}[data-focus=detail] .detail .pigment{{stroke-width:1.05px}}");
    }
}
