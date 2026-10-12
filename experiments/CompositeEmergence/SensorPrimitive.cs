using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace CompositeEmergence;

/// <summary>A deliberately limited learner. Its only observation and learning
/// target is its own direction channel; there is no action, resource, second
/// channel, or apparatus-mode port.</summary>
public sealed class SensorPrimitive
{
    private readonly OrdinalPredictor predictor;
    private readonly List<Relation> receipts = [];
    public Carrier Axis { get; }
    public Relation Root { get; private set; }
    public Relation Current { get; private set; }
    public Relation LastForecast { get; private set; }
    public int Updates => predictor.Updates;

    public SensorPrimitive(Carrier axis)
    {
        Axis = axis; predictor = new OrdinalPredictor(axis, 1);
        Current = NativeGraph.Open(axis, Relation.Segment(axis, -1, 1));
        LastForecast = predictor.Predict(); Root = Build();
    }
    private SensorPrimitive(Relation root)
    {
        var fields = NativeGraph.Parts(root, 5); Axis = (Carrier)fields[0];
        predictor = OrdinalPredictor.FromRoot((Relation)fields[1]); Current = (Relation)fields[2]; LastForecast = (Relation)fields[3];
        receipts.AddRange(NativeGraph.Parts((Relation)fields[4]).Cast<Relation>()); Root = root;
    }
    public static SensorPrimitive FromRoot(Relation root) => new(root);
    public Relation OwnForecast() { LastForecast = predictor.Predict(); Root = Build(); return LastForecast; }
    public Relation Accept(Relation publication, bool learn = true)
    {
        var identity = NativeGraph.Identity(publication);
        if (!NativeGraph.TryValue(publication, Axis, out var value) || value.Ticks is not (-1 or 1) ||
            receipts.Any(receipt => ReferenceEquals(NativeGraph.Identity(receipt), identity)))
        {
            Current = NativeGraph.Open(identity, Relation.Segment(Axis, -1, 1)); Root = Build(); return Current;
        }
        LastForecast = predictor.Predict(); Current = NativeGraph.CapturePublication(publication, Axis);
        predictor.Observe(LastForecast, Current, learn);
        // An eight-event identity window is deliberate finite memory. It is not
        // a claim of duplicate protection over an unlimited historical stream.
        receipts.Add(Current); if (receipts.Count > NativeGraph.ReceiptLimit) receipts.RemoveAt(0);
        Root = Build(); return Current;
    }
    public void ResetOperational()
    {
        predictor.ResetContext(); receipts.Clear(); Current = NativeGraph.Open(Axis, Relation.Segment(Axis, -1, 1));
        LastForecast = predictor.Predict(); Root = Build();
    }
    private Relation Build() => NativeGraph.G(Axis, predictor.Root, Current, LastForecast, NativeGraph.G(receipts.ToArray()));
}
