using Gojo.Core;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class LatticeTransformationRecipes
{
    /// <summary>Retargets the actually advertised common start without changing
    /// any intermediate contour landmark. The retargeting delta and all prefix
    /// reductions remain connected to the replaced paths. It is deliberately a
    /// focused topology/site edit, not a refinement that would promise the old
    /// start still occupies the same site.</summary>
    private static QualitativeLatticeLadder RetargetCommonStarts(QualitativeLatticeLadder source,
        LatticeLadderAuthor a, Relation displacement)
    {
        var old = source.Current.Program;
        var paths = E(old.Paths).Cast<Relation>().ToArray();
        var firstStart = (Relation)paths[0].Left;
        var common = a.Advance(firstStart, displacement, out var startRead);
        var pairs = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var words = new Dictionary<Relation, (Relation First, Relation Last)>(ReferenceEqualityComparer.Instance);
        var receipts = new List<IElement> { startRead };
        foreach (var path in paths)
        {
            var startClaim = a.Frame.Reduce((Relation)path.Left, a.Inverse(firstStart));
            if (!UnitArrangementRead.IsZero(a.Frame, startClaim)) throw new ArgumentException("The selected routes must advertise the same native start.");
            var chain = E((Relation)path.Right).Cast<Relation>().ToArray();
            var first = a.Advance(chain[0], a.Inverse(displacement), out var firstRead);
            var last = a.Advance(chain[^1], displacement, out var lastRead);
            var replacement = LatticeLadderAuthor.Path(common, [first, .. chain.Skip(1).Take(chain.Length - 2), last]);
            pairs.Add(path, replacement); words.Add(path, (first, last));
            receipts.Add(new Relation(new Relation(path.Left, common), new Relation(startClaim, new Relation(firstRead, lastRead))));
        }
        var ports = E(old.Ports).Cast<Relation>().Select(port =>
        {
            var pick = (Relation)port.Right; var oldPath = (Relation)pick.Left;
            var chain = E((Relation)oldPath.Right).Cast<Relation>().ToArray();
            IElement selector = ReferenceEquals(pick.Right, oldPath) ? pairs[oldPath]
                : ReferenceEquals(pick.Right, chain[0]) ? words[oldPath].First
                : ReferenceEquals(pick.Right, chain[^1]) ? words[oldPath].Last : pick.Right;
            return (IElement)new Relation(port.Left, new Relation(pairs[oldPath], selector));
        }).ToArray();
        var correspondence = S(pairs.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray());
        return source.ReplacePaths("Retarget the common chamber hub", old.Paths, S(pairs.Values.Cast<IElement>().ToArray()), S(ports),
            S(E(old.ClosedPaths).Cast<Relation>().Select(path => (IElement)pairs[path]).ToArray()),
            new Relation(new Relation(firstStart, new Relation(displacement, common)), new Relation(correspondence, S(receipts.ToArray()))));
    }
}
