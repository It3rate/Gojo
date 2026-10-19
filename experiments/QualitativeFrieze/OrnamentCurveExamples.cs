using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Optional curved readings of three native detail studies. The
/// guide programs, actual branch roles and interrupted ends remain unchanged;
/// the selection only chooses which final paths may receive display fillets.</summary>
public static class OrnamentCurveExamples
{
    public sealed record Example(BourgoinOrnamentDesignLibrary.Design Design, Relation CurveHints, string Notes);

    public static IReadOnlyList<Example> Create() =>
        OrnamentDetailLibrary.Create(["D02", "D07", "D09"]).Select(Build).ToArray();

    private static Example Build(OrnamentDetailLibrary.Study study)
    {
        var design = study.Design; var enrichment = design.Enrichment;
        var sourcePaths = N.E(enrichment.Source.Paths).Cast<Relation>().ToArray();
        var organicGuides = study.Id switch
        {
            "D02" => new[] { sourcePaths[0] },
            "D07" => new[] { sourcePaths[0], sourcePaths[^1] },
            "D09" => sourcePaths,
            _ => throw new ArgumentException("Curve examples use the three declared detail studies.", nameof(study))
        };
        // Eight/ten-chord contours are the actual authored lens leaves in
        // these two studies. Their twelve-chord pearl siblings stay faceted.
        var leaves = study.Id == "D09" ? [] : N.E(enrichment.Result.ClosedPaths).Cast<Relation>()
            .Where(path => !sourcePaths.Any(actual => ReferenceEquals(actual, path)) &&
                N.E((Relation)path.Right).Length is 8 or 10).ToArray();
        var selected = organicGuides.Concat(leaves).ToArray();
        var roles = ClaimedRoles(design.Source.SeamContract);
        var fixedPorts = N.E(enrichment.Source.Ports).Cast<Relation>().Where(port =>
            roles.Contains(port.Left) && port.Right is Relation pick &&
            organicGuides.Any(path => ReferenceEquals(path, pick.Left))).ToList();
        foreach (var path in leaves)
        {
            var words = N.E((Relation)path.Right);
            var tip = N.E(enrichment.Result.Ports).Cast<Relation>().Single(port =>
                port.Right is Relation pick && ReferenceEquals(pick.Left, path) &&
                ReferenceEquals(pick.Right, words[words.Length / 2]));
            fixedPorts.Add(tip);
        }
        var selection = new OrnamentCurveSelection(enrichment.Result, N.S(selected), N.S(fixedPorts.ToArray()));
        if (!selection.IsComplete)
            throw new InvalidOperationException("Curved detail examples must select actual final paths and public fixed landmarks.");
        var notes = study.Id == "D09"
            ? "Three separately acknowledged wave paths receive bounded display fillets. Both actual interruption endpoints remain exact, and the native gap displacement witness stays in the source. The two faceted pearl contours are carried unchanged. No hidden under-strand or over/under law is added."
            : "The parent stem and selected flowing guides receive bounded display fillets, together with the actual pointed leaf contours. Claimed branch landmarks and the leaves' actual halfway tip ports stay fixed. Veins, faceted pearls and every unselected chord guide are carried unchanged.";
        return new(design with { Id = "C-" + study.Id, Title = design.Title + " · curved reading" },
            N.S(selection.Surface), notes);
    }

    private static HashSet<IElement> ClaimedRoles(Relation seamContract)
    {
        var result = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        if (seamContract.Right is not Relation rules || !N.Read(rules, out var entries))
            throw new ArgumentException("A detail seam contract must expose its exact native rule scope.", nameof(seamContract));
        foreach (var entry in entries)
        {
            if (entry is not Relation { Right: Relation { Right: Relation roles } })
                throw new ArgumentException("A detail seam rule must expose its two actual public roles.", nameof(seamContract));
            result.Add(roles.Left); result.Add(roles.Right);
        }
        return result;
    }
}
