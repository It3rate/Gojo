namespace Gojo.Core;

public sealed partial class Carrier
{
    // The dynamic structure is the incidence C --attached--> R itself. The
    // ordered R and its carrier sites retain their meaning while unattached;
    // there is no separate connection record or active-state registry.

    /// <summary>
    /// Attaches this relation identity to each concrete carrier it references
    /// and settles from this carrier. Reattaching the same identity does not
    /// duplicate its incidence; it resumes behavior against current state.
    /// </summary>
    public Relation Attach(Relation relation)
    {
        foreach (var carrier in relation.Carriers().Where(carrier => !carrier.IsUnknown).Distinct())
        {
            if (!carrier.IsAttached(relation)) carrier.relations.Add(relation);
        }

        SettleWorld();
        return relation;
    }

    /// <summary>Reads whether this exact relation identity is attached here.</summary>
    public bool IsAttached(Relation relation) =>
        relations.Any(existing => existing.SameIdentityAs(relation));

    /// <summary>
    /// Withdraws this exact relation identity from attachment owners reachable
    /// from this carrier or the relation's concrete sites. The local scope is
    /// discovered before any incidence is removed. Returns whether an
    /// incidence was removed; repeating the withdrawal is harmless.
    /// </summary>
    /// <remarks>
    /// This edits participation only. It does not settle, erase value or
    /// tension, retract transferred material, or zero a live publication's
    /// receiver. A receiver retains its last state until explicit input or
    /// other attached structure changes it. Reattach with Attach to resume
    /// settlement; relation structure retained elsewhere remains inspectable.
    /// No disconnected owner outside this local scope is inferred or searched.
    /// </remarks>
    public bool Detach(Relation relation)
    {
        var owners = ConnectedCarriers(new[] { this }.Concat(relation.Carriers()));
        var changed = false;
        foreach (var carrier in owners)
            changed |= carrier.relations.RemoveAll(existing => existing.SameIdentityAs(relation)) != 0;
        return changed;
    }
}
