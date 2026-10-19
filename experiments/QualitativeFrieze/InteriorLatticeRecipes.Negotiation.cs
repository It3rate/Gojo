using Gojo.Core;

namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class InteriorLatticeRecipes
{
    private InteriorLatticeCase Negotiate(InteriorLatticeCase source, Spec spec, IReadOnlyList<ActualTile> tiles,
        IReadOnlyDictionary<Cell, Relation> atoms, IReadOnlyList<Relation> targetGroups, LatticeLadderAuthor author)
    {
        var width = spec.Width / 2;
        var indices = new Dictionary<(int Column, int Row), int>();
        for (var i = 0; i < tiles.Count; i++) indices.Add((tiles[i].Translation.X / width, tiles[i].Translation.Y / spec.Height), i);
        var baseFaces = Enumerable.Range(0, tiles.Count).Select(_ => new List<IElement>()).ToArray();
        var targetOwner = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < targetGroups.Count; i++) foreach (var face in E(targetGroups[i].Right)) targetOwner.Add(face, i);
        var baseOwner = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        foreach (var atom in atoms)
        {
            // Only the finite frontier uses a clipped base rectangle. Every
            // accepted target occurrence itself is whole, including the ones
            // crossing the selected period boundary.
            var column = Math.Clamp(atom.Key.X / width, 0, columns * 2 - 1);
            var row = atom.Key.Y / spec.Height;
            var index = indices[(column, row)];
            baseFaces[index].Add(atom.Value); baseOwner.Add(atom.Value, index);
        }
        var baseClaims = S(targetGroups.Select((group, i) => (IElement)new Relation(group.Left, S(baseFaces[i].ToArray()))).ToArray());
        var proposals = S(targetGroups.Select((group, i) => (IElement)new Relation(group.Left,
            S(baseFaces[i].Concat(E(group.Right)).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray()))).ToArray());
        var resolutions = S(atoms.Values.Where(face => baseOwner[face] != targetOwner[face])
            .Select(face => (IElement)new Relation(face, targetGroups[targetOwner[face]].Left)).ToArray());
        var read = InteriorBoundaryNegotiation.Reconcile(source.Current.Complex, proposals, resolutions);
        if (!read.IsSettled) throw new InvalidOperationException(source.Id + ": explicit repeated seam choices must settle.");
        var accepted = read.ResultClaims;
        var steps = new List<InteriorLatticeStage> { source.Stages[0] };
        Add("Merge the rectangular starting rooms", "Repeated ordinary rectangles first absorb their atomic separators. The finite frontier is shown separately from the full interior occurrences.", baseClaims,
            Transition(source.Current.Complex, source.Stages[0].Groups, baseClaims));
        Add("Expose the neighbours' incompatible offers", "Each side retains its current atoms and independently requests a broad shoulder of its neighbour. The offered interiors overlap, so these are blocked desired claims rather than a valid filled partition.", proposals,
            new(new Relation(baseClaims, proposals), new Relation(resolutions, read.Surface)));
        Add("Accept one common stepped seam", "Every contested actual face has an explicit chosen owner. The accepted and rejected side offers stay connected to the negotiation witness; only the resulting exclusive common boundary is rendered as the settled seam.", accepted,
            new(new Relation(proposals, resolutions), new Relation(read.Surface, Transition(source.Current.Complex, baseClaims, accepted))));

        var acceptedRows = E(accepted).Cast<Relation>().ToArray();
        Relation Current(Relation row) => acceptedRows.Single(candidate => ReferenceEquals(candidate.Left, row.Left));
        var oldPeriod = source.Periodicity;
        var instances = E(oldPeriod.Right).Cast<Relation>().Select(instance =>
        {
            var body = (Relation)instance.Right;
            return (IElement)new Relation(Current((Relation)instance.Left), new Relation(Current((Relation)body.Left), body.Right));
        }).ToArray();
        return source with
        {
            Stages = steps.AsReadOnly(),
            Periodicity = new Relation(oldPeriod.Left, S(instances)),
            SeamOffers = new(new Relation(baseClaims, proposals), new Relation(resolutions, read.Surface))
        };

        void Add(string name, string description, Relation claims, Relation transition)
        {
            var current = source.Current;
            steps.Add(new(name, description, current.Complex, claims, current.Bindings, transition,
                new(new Relation(current.Complex, claims), new Relation(current.Bindings, transition))));
        }
    }
}
