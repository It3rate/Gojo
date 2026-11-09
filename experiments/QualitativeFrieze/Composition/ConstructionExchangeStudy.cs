using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Authored exchange apparatus. A selected output agreement does not
/// infer generating history, mutate a source, or prove unrestricted equivalence.</summary>
public static class ConstructionExchangeStudy
{
    public sealed record MachineInfo(string Id, string Name, string Preserves, string Changes);
    public sealed record Shape(string Id, string Name, string Scenario, int Width, int Height, int Notch,
        bool Admissible, int Failed, int Unresolved, int Sections, int ZeroSections,
        int Components, bool Closed, bool? EqualTurns, long[][][] Lines)
    {
        [JsonIgnore] public ConstructionExchangeMachine Machine { get; init; } = null!;
        [JsonIgnore] public ConstructionExchangeMachine.Execution Execution { get; init; } = null!;
        public int SeedCount => ProgramEntries(1).Length;
        public int AdvanceCount => ProgramEntries(2).Length;
        public int EqualityCount => ProgramEntries(4).Length;
        private IElement[] ProgramEntries(int index) => E((Relation)E((Relation)Machine.Surface.Right)[index]);
    }
    public sealed record Comparison(string Scenario, string From, string To, bool BoundaryAgrees,
        bool DirectedAgrees, bool TopologyAgrees, bool Admissible, int Matches,
        int SourceOnly, int TargetOnly, int Unresolved)
    {
        public bool BoundaryExchange => BoundaryAgrees && Admissible;
        public bool BoundaryAndTopologyExchange => BoundaryExchange && TopologyAgrees;
        [JsonIgnore] public Relation Witness { get; init; } = null!;
    }
    public sealed record ContourProbe(int Markers, bool Closed, bool EqualTurns, int OpenObligations)
    {
        [JsonIgnore] public Relation Witness { get; init; } = null!;
    }
    public sealed record Report(MachineInfo[] Machines, Shape[] Shapes, Comparison[] Comparisons, ContourProbe[] Contours);

    public static readonly MachineInfo[] Machines =
    [
        new("walk", "Four-step closed walk", "Ordered handoffs; closing return; opposing reaches share roles.", "Width and height vary independently. A notch needs extra phases."),
        new("rails", "Two pairs of rails", "Shared intersections; parallel pairs; common width and height.", "Aspect changes directly. Concavity needs a different boundary construction."),
        new("radial-equal", "Four equal radial turns", "Common contour; cyclic adjacency; equal-turn promise; equal component reaches.", "Uniform scaling fits. Stretching needs release of equal-turn calibration."),
        new("radial-open", "Four radial points, angular gaps open", "Common contour and cyclic adjacency remain; equal-turn obligation stays open.", "Rectangles fit the supplied centered realization; general radial interpolation remains open."),
        new("caps", "Two opposed caps", "Two three-phase routes share their authored start and end sites.", "Same rectangle outline; different section count and traversal."),
        new("notch", "Six-step notched walk", "Ordered return survives the inserted left/up detour.", "A zero-at-resolution notch retains two phases. Increasing its binding exposes concavity."),
        new("open", "Three-step open walk", "Order and shared adjacent sites; no return edge is supplied.", "Closing it requires adding the missing connection."),
        new("detached", "Four detached rails", "Matching coordinate expressions, but independent endpoint identities.", "The outline can agree with a loop. Joining requires explicit topology.")
    ];

    public static Report Explore()
    {
        var frame = new Curve.Frame(new QualitativeMotionFrame());
        var machines = Enumerable.Range(0, Machines.Length).Select(i => ConstructionExchangeMachine.Create(frame, i)).ToArray();
        var shapes = new List<Shape>(); var comparisons = new List<Comparison>();
        foreach (var (scenario, w, h, n) in new[] { ("square", 2, 2, 0), ("stretch", 3, 2, 0),
                     ("notch", 3, 2, 1), ("zero-read", 0, 2, 0) })
        {
            var current = machines.Select((machine, index) => Describe(machine, Machines[index], scenario, w, h, n)).ToArray();
            shapes.AddRange(current);
            for (var i = 0; i < current.Length; i++)
                for (var j = i + 1; j < current.Length; j++) comparisons.Add(Compare(frame, current[i], current[j]));
        }
        // Pre-numeric count probe: actual radial attachments and successor edges.
        // No coordinate fit supplies the conditional equal-turn construction.
        var contours = new List<ContourProbe>();
        foreach (var count in new[] { 3, 4, 5, 6, 8 })
        {
            var markers = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
            var edges = S(markers.Select((marker, i) => (IElement)new Relation(marker, markers[(i + 1) % count])).ToArray());
            var contour = new QualitativeMarkedContour(new Placeholder(), S(new Placeholder()), S(markers), edges, markers[0])
                .WithEqualTurns(new Placeholder());
            var read = contour.Evaluate(); contour.TryReadResult(read, out _, out _, out var pending);
            contours.Add(new(count, contour.TryReadClosedContour(read, out _), contour.TryReadEqualTurns(read, out _), E(pending).Length) { Witness = read });
        }
        return new(Machines, shapes.ToArray(), comparisons.ToArray(), contours.ToArray());
    }

    public static Shape Describe(ConstructionExchangeMachine machine, MachineInfo info, string scenario, int w, int h, int n)
    {
        var execution = machine.Execute(machine.Publish(w, h, n));
        var sections = E(execution.Sections).Cast<Relation>().Select(machine.Frame.Read).ToArray();
        var lines = sections.Select(section => section.Controls.Select(p => new[] { checked((long)p.X), checked((long)p.Y) }).ToArray()).ToArray();
        var topology = Topology(machine.Frame, execution.Sections);
        bool? equalTurns = machine.Contour is null ? null : machine.Contour.TryReadEqualTurns(machine.Contour.Evaluate(), out _);
        return new(info.Id, info.Name, scenario, w, h, n, execution.Admissible, E(execution.Failed).Length,
            E(execution.Unresolved).Length, sections.Length, sections.Count(s => s.Controls[0] == s.Controls[^1]),
            topology.Components, topology.Closed, equalTurns, lines) { Machine = machine, Execution = execution };
    }

    public static Comparison Compare(Curve.Frame frame, Shape from, Shape to)
    {
        var boundary = BoundaryExchangeRead.Evaluate(frame, from.Execution.Sections, to.Execution.Sections);
        var directed = BoundaryExchangeRead.Evaluate(frame, from.Execution.Sections, to.Execution.Sections, directed: true);
        var a = Topology(frame, from.Execution.Sections); var b = Topology(frame, to.Execution.Sections);
        // A deliberately coarse contract, not general graph isomorphism.
        var topologyAgrees = a.Components == b.Components && a.Closed == b.Closed && a.Branches.SequenceEqual(b.Branches);
        var contract = S(new Relation(from.Execution.Surface, to.Execution.Surface), boundary.Surface, directed.Surface, a.Witness, b.Witness);
        return new(from.Scenario, from.Id, to.Id, boundary.Equivalent, directed.Equivalent, topologyAgrees,
            from.Execution.Admissible && to.Execution.Admissible, E(boundary.Matches).Length, E(boundary.SourceOnly).Length,
            E(boundary.TargetOnly).Length, E(boundary.Unresolved).Length) { Witness = contract };
    }

    public sealed record TopologyRead(int Components, bool Closed, int[] Branches, Relation Witness);
    public static TopologyRead Topology(Curve.Frame frame, Relation sections)
    {
        var endpoints = new Dictionary<IElement, List<IElement>>(ReferenceEqualityComparer.Instance);
        var incidence = new Dictionary<IElement, List<IElement>>(ReferenceEqualityComparer.Instance);
        foreach (var section in E(sections).Cast<Relation>())
        {
            var read = frame.Read(section);
            foreach (var (from, to) in new[] { (read.Start, read.End), (read.End, read.Start) })
            {
                if (!endpoints.TryGetValue(from, out var neighbors)) endpoints[from] = neighbors = [];
                neighbors.Add(to);
                if (!incidence.TryGetValue(from, out var edges)) incidence[from] = edges = [];
                edges.Add(section);
            }
        }
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var groups = new List<IElement>();
        foreach (var site in endpoints.Keys)
        {
            if (!seen.Add(site)) continue;
            var members = new List<IElement>(); var stack = new Stack<IElement>(); stack.Push(site);
            while (stack.TryPop(out var current))
            {
                members.Add(new Relation(current, S(incidence[current].ToArray())));
                foreach (var next in endpoints[current]) if (seen.Add(next)) stack.Push(next);
            }
            groups.Add(S(members.ToArray()));
        }
        return new(groups.Count, endpoints.Count > 0 && endpoints.Values.All(v => v.Count == 2),
            endpoints.Values.Select(v => v.Count).Where(d => d != 2).Order().ToArray(), new Relation(sections, S(groups.ToArray())));
    }

    public static int RunExperiment(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "construction-exchange");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --construction-exchange [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var report = Explore();
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "results.json"), json);
        foreach (var scenario in report.Shapes.Select(s => s.Scenario).Distinct())
        {
            // Retain native programs, publications, correspondence and read
            // obligations. The JSON/HTML is only a projection of this graph.
            var root = S(S(report.Shapes.Where(s => s.Scenario == scenario).Select(s => (IElement)s.Execution.Surface).ToArray()),
                S(report.Comparisons.Where(c => c.Scenario == scenario).Select(c => (IElement)c.Witness).ToArray()));
            var bytes = GojoNumericFormat.Encode(root);
            if (GojoNumericFormat.Decode(bytes) is not [Relation]) throw new InvalidOperationException("Exchange archive did not restore.");
            File.WriteAllBytes(Path.Combine(output, scenario + ".gojo"), bytes);
        }
        File.WriteAllBytes(Path.Combine(output, "contours.gojo"), GojoNumericFormat.Encode(S(report.Contours.Select(c => (IElement)c.Witness).ToArray())));
        ConstructionExchangeGallery.Write(output, report, json);
        Console.WriteLine($"{report.Shapes.Length} constructions; {report.Comparisons.Length} pair comparisons; " +
            $"{report.Comparisons.Count(c => c.BoundaryExchange)} boundary exchanges; " +
            $"{report.Comparisons.Count(c => c.BoundaryAndTopologyExchange)} also satisfy the coarse topology contract.");
        Console.WriteLine(Path.GetFullPath(Path.Combine(output, "index.html")));
        return 0;
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
