using Gojo.Core;

namespace CompositeEmergence;

/// <summary>Allocates a paid probe or reuses the advertised service. Its only
/// contexts are publicly available service presence, coarse age and the previous
/// independent resource outcome. No private apparatus state is an input.</summary>
public sealed class RefreshLearner
{
    private readonly LocalChoiceMarket market;
    public Carrier AvailableAxis => market.ContextAxes[0];
    public Carrier AgeAxis => market.ContextAxes[1];
    public Carrier OutcomeAxis => market.ContextAxes[2];
    public Carrier ChoiceAxis => market.ChoiceAxis;
    public Carrier QueryAxis => market.ChoiceAxis;
    public Carrier ResourceAxis => market.ResourceAxis;
    public Relation Root => market.Root;
    public Relation LearnedRoot => market.LearnedRoot;
    public Relation LastDecision => market.LastDecision;
    public Relation LastFeedback => market.LastFeedback;
    public int RowCount => market.RowCount;
    public long UpdateCount => market.UpdateCount;
    public long AgingEvents => market.AgingEvents;

    public RefreshLearner(Carrier availableAxis, Carrier ageAxis, Carrier outcomeAxis, Carrier resourceAxis)
    {
        market = new([availableAxis, ageAxis, outcomeAxis], [(0, 1), (0, 3), (-1, 1)], new Carrier(), [0L, 1L], resourceAxis);
    }
    private RefreshLearner(Relation root) => market = LocalChoiceMarket.FromRoot(root);
    public static RefreshLearner FromRoot(Relation root) => new(root);
    public Relation Decide(Relation available, Relation age, Relation previousOutcome, bool learning = true) =>
        market.Decide([available, age, previousOutcome], learning);
    public Relation BindFeedback(Relation physicalResource) => market.BindFeedback(physicalResource);
    public Relation Observe(Relation boundResource, bool learning = true) => market.Observe(boundResource, learning);
    public void ResetOperational() => market.ResetOperational();
    public Relation ReadEvidence() => market.ReadEvidence();
    public IReadOnlyList<MarketEvidence> InspectEvidence() => market.InspectEvidence();
}
