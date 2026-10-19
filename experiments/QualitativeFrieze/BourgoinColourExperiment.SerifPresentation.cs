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

        // The retained slender leaf/leaves accent the long axis within each
        // chamber. Their contrast increases with the selected level, without
        // introducing new caps, detached flowers or interior subdivisions.
        css.AppendLine($"{scope} .motif [data-colour-group]{{--colour:var(--pale)!important}}");
        css.AppendLine($"{scope} .motif .edge{{stroke:var(--ink);stroke-width:calc(var(--band) * .11 + 1.1px)}}");
        css.AppendLine($"{scope} .motif .keyline{{stroke:var(--pale);stroke-width:calc(var(--band) * .11 + .55px)}}");
        css.AppendLine($"{scope} .motif .pigment{{stroke-width:calc(var(--band) * .11)}}");
        css.AppendLine($"{scope}[data-focus=field] .motif{{opacity:.5!important}}");
        css.AppendLine($"{scope}[data-focus=motif] .motif{{opacity:.85!important}}");
        css.AppendLine($"{scope}[data-focus=detail] .motif{{opacity:1!important}}");
        css.AppendLine($"{scope} .detail .edge,{scope} .detail .keyline{{stroke:none}}");
        css.AppendLine($"{scope} .detail .pigment{{stroke:var(--ink);stroke-width:.85px}}");
        css.AppendLine($"{scope}[data-focus=field] .detail{{opacity:.3!important}}");
        css.AppendLine($"{scope}[data-focus=motif] .detail{{opacity:.5!important}}");
        css.AppendLine($"{scope}[data-focus=detail] .detail{{opacity:.75!important}}");
    }
}
