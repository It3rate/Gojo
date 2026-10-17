using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite authored neighbour topologies and local laws. Construction
/// ranks choose exposed observation connections only; no rank or coordinate
/// array participates in runtime conditional evaluation.</summary>
public sealed class AdaptiveWallpaperStudy
{
    public Case[] Cases { get; }
    public Relation Surface { get; }

    public sealed record Case(string Name, string Description, AdaptiveWallpaperMachine Machine,
        int Rounds, Relation Seeds, Relation FrozenCells, Relation Cells, Relation Support);

    public AdaptiveWallpaperStudy()
    {
        Cases = [Pair(), Patch("cushion", 3, false), Patch("key", 5, false), Patch("rosette", 5, true)];
        Surface = S(Cases.Select(example => (IElement)example.Support).ToArray());
    }

    private static Case Pair()
    {
        var grammar = new AdaptiveMotifGrammar(new(), new Placeholder(), "cushion");
        var a = new AdaptiveMotifNode("held source", grammar, S(), grammar.Plus, frozen: true);
        var b = new AdaptiveMotifNode("adapting neighbour", grammar, Word(grammar, grammar.Frame.Right, 8), grammar.Plus);
        var link = new AdaptiveMotifLink(a, b, grammar.East, grammar.West);
        var observations = S(new Relation(b.Cell, link.Surface));
        var machine = new AdaptiveWallpaperMachine("One neighbour repairs a seam", grammar, [a, b], [link], observations);
        return Define(machine.Name, "The left motif holds its handedness. Its neighbour reads the incompatible boundary offer, rebuilds as the reflected motif and republishes its actual ports. The waiting packet crosses on a later round.",
            machine, S(S(a.Cell, b.Cell)), S(a.Cell));
    }

    private static Case Patch(string family, int size, bool heldCross)
    {
        var grammar = new AdaptiveMotifGrammar(new(), new Placeholder(), family);
        var middle = size / 2; var nodes = new AdaptiveMotifNode[size, size];
        var cells = new List<IElement>(); var frozen = new List<IElement>();
        for (var y = 0; y < size; y++)
        {
            var row = new List<IElement>();
            for (var x = 0; x < size; x++)
            {
                var rank = Math.Abs(x - middle) + Math.Abs(y - middle);
                var hold = rank == 0 || heldCross && rank == 1;
                var anchor = S(Word(grammar, x < middle ? grammar.Frame.Left : grammar.Frame.Right, Math.Abs(x - middle) * 8),
                    Word(grammar, y < middle ? grammar.Frame.Down : grammar.Frame.Up, Math.Abs(y - middle) * 8));
                // Flatten the two actual repeated-participation words. These
                // are instructions, never a hidden Cartesian source state.
                var anchorWord = S(E(anchor).Cast<Relation>().SelectMany(E).ToArray());
                var node = nodes[x, y] = new AdaptiveMotifNode($"{family} {x + 1}:{y + 1}", grammar, anchorWord, grammar.Plus, hold);
                row.Add(node.Cell); if (hold) frozen.Add(node.Cell);
            }
            cells.Add(S(row.ToArray()));
        }
        var links = new List<AdaptiveMotifLink>(); var observations = new List<IElement>();
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
        {
            if (x + 1 < size) Connect(x, y, x + 1, y, grammar.East, grammar.West);
            if (y + 1 < size) Connect(x, y, x, y + 1, grammar.North, grammar.South);
        }
        var name = family switch
        {
            "cushion" => "Fourfold cushion agreement",
            "key" => "A field of interlocking keys",
            _ => "Rosette field with four held seams"
        };
        var description = heldCross
            ? "The centre and its four axis neighbours hold the same handedness. Surrounding motifs adapt through their inward observation ports, preserving fourfold symmetry while four central material passages remain visibly blocked."
            : "All motifs begin with the same incompatible boundary offers. The centre holds its variant; local non-draining observations carry its constraint outward. Complete replacement motifs publish new ports, and queued packets wait for renewed seam acceptance.";
        var ordered = E(S(cells.ToArray())).Cast<Relation>().SelectMany(E).Select(cell => nodes.Cast<AdaptiveMotifNode>().Single(node => ReferenceEquals(node.Cell, cell))).ToArray();
        var machine = new AdaptiveWallpaperMachine(name, grammar, ordered, links, S(observations.ToArray()));
        return Define(name, description, machine, S(cells.ToArray()), S(frozen.ToArray()));

        void Connect(int x, int y, int nx, int ny, IElement fromRole, IElement toRole)
        {
            var link = new AdaptiveMotifLink(nodes[x, y], nodes[nx, ny], fromRole, toRole); links.Add(link);
            var a = Math.Abs(x - middle) + Math.Abs(y - middle); var b = Math.Abs(nx - middle) + Math.Abs(ny - middle);
            var observer = a > b ? nodes[x, y] : nodes[nx, ny];
            observations.Add(new Relation(observer.Cell, link.Surface));
        }
    }

    private static Case Define(string name, string description, AdaptiveWallpaperMachine machine, Relation cells, Relation frozen)
    {
        var seeds = S(machine.Links.Select(link => (IElement)new Relation(link.Surface, new Placeholder())).ToArray());
        var support = new Relation(machine.Surface, new Relation(cells, new Relation(frozen, seeds)));
        return new(name, description, machine, 6, seeds, frozen, cells, support);
    }
    private static Relation Word(AdaptiveMotifGrammar grammar, IElement direction, int count) =>
        S(Enumerable.Range(0, count).Select(_ => (IElement)QualitativeMotionFrame.Move(direction, grammar.Reach)).ToArray());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
