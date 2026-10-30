using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.PrimitiveRuntime;

/// <summary>
/// Authored bounded construction, entirely before metric calibration.
/// State atoms are ordered landmark/heading pairs, never independent masks.
/// An input pulse selects a native transition; the renderer supplies no motion.
/// </summary>
public sealed class MotionDefinition
{
    public IReadOnlyList<Placeholder> Landmarks { get; } = Array.AsReadOnly<Placeholder>([new(), new(), new(), new()]);
    public Placeholder Anchor { get; } = new();
    public Placeholder Moving { get; } = new();
    public Placeholder Independent { get; } = new();
    public Placeholder FarAnchor { get; } = new();
    public Placeholder Input { get; } = new();
    public Placeholder Output { get; } = new();
    public Placeholder Pulse { get; } = new();
    public Placeholder ReverseInput { get; } = new();
    public Placeholder UnadmittedInput { get; } = new();
    public Placeholder OpenRemainder { get; } = new();
    public Relation Forward { get; }
    public Relation Backward { get; }
    public Relation Connection { get; }
    public Relation AttachedConnection { get; }
    public Relation IndependentConnection { get; }
    public Relation Order { get; }
    public IReadOnlyList<Relation> States { get; }
    public Relation StateDomain { get; }
    public Relation CommandDomain { get; }
    public Relation Law { get; }
    public Relation Feedback { get; }
    public Relation Surface { get; }

    public MotionDefinition()
    {
        Forward = new(Landmarks[0], Landmarks[^1]);
        Backward = Forward.Reverse();
        Connection = new(Anchor, Moving);
        AttachedConnection = new(Moving, FarAnchor);
        IndependentConnection = new(Anchor, Independent);
        var edges = Landmarks.Zip(Landmarks.Skip(1), (a, b) => new Relation(a, b)).ToArray();
        Order = StructuralConstraint.Closed(edges);
        States = Array.AsReadOnly(Landmarks.SelectMany(site => new[] { new Relation(site, Forward), new Relation(site, Backward) }).ToArray());
        StateDomain = StructuralConstraint.Closed(States.ToArray());
        CommandDomain = StructuralConstraint.Closed(Pulse, ReverseInput, UnadmittedInput);
        var rows = new List<Relation>();
        foreach (var edge in edges)
        {
            rows.Add(FiniteLaw.Row(State(edge.Left, Forward), Pulse,
                StructuralConstraint.Closed(State(edge.Right, Forward))));
            rows.Add(FiniteLaw.Row(State(edge.Right, Backward), Pulse,
                StructuralConstraint.Closed(State(edge.Left, Backward))));
        }
        // Explicit selected endpoint response: change heading first, then the
        // next pulse moves. Neither missing rows nor arbitrary relations bounce.
        rows.Add(FiniteLaw.Row(State(Landmarks[^1], Forward), Pulse,
            StructuralConstraint.Closed(State(Landmarks[^1], Backward))));
        rows.Add(FiniteLaw.Row(State(Landmarks[0], Backward), Pulse,
            StructuralConstraint.Closed(State(Landmarks[0], Forward))));
        foreach (var site in Landmarks)
        {
            rows.Add(FiniteLaw.Row(State(site, Forward), ReverseInput,
                StructuralConstraint.Closed(State(site, Backward))));
            rows.Add(FiniteLaw.Row(State(site, Backward), ReverseInput,
                StructuralConstraint.Closed(State(site, Forward))));
        }
        Law = FiniteLaw.Create(StateDomain, CommandDomain, StateDomain, rows.ToArray());
        Feedback = new(Output, Input);
        // Carrier-free exact scope, not RepeatSurface's carrier-rooted metric
        // clock grammar. The shared ports expose output-to-later-input feedback.
        var controller = new Relation(new Relation(Input, Law), new Relation(Output, Feedback));
        var sites = StructuralConstraint.Closed(Connection, AttachedConnection, IndependentConnection,
            new Relation(Anchor, Landmarks[0]), new Relation(FarAnchor, Landmarks[^1]),
            new Relation(Independent, Landmarks[1]), new Relation(Moving, Input));
        Surface = new(new Relation(Order, sites), new Relation(controller, OpenRemainder));
    }

    public Relation State(IElement landmark, IElement heading) => States.Single(state =>
        ReferenceEquals(state.Left, landmark) && ReferenceEquals(state.Right, heading));

    public int LandmarkOf(Relation state)
    {
        for (var index = 0; index < Landmarks.Count; index++)
            if (ReferenceEquals(Landmarks[index], state.Left)) return index;
        return -1;
    }
}
