namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Authored trial configuration; the caption is never passed to an
/// executor. The builder selects an artifact and explicit input/response roles.</summary>
public sealed record ConstructionBehaviorCase(string Title, string Description,
    int Source, int StartSite = 0, int? ResponseField = 0, bool Pin = false,
    int[]? SelectedUses = null);

/// <summary>A working trial owns an independently decoded artifact. Its two
/// response experiments have separate activation stores and never influence
/// one another. Builders, geometric bases, names and pixels do not execute.</summary>
public sealed class ConstructionBehavior
{
    private sealed class Observation : IPrimitiveReadPort
    {
        public byte Support = 1;
        public byte ReadSupport() => Support;
    }

    private readonly Observation observation = new();
    public ConstructionBehaviorCase Case { get; }
    public ConstructionExample Example { get; }
    public FamilyWalk Walk { get; }
    public FamilyAdjustment Adjustment { get; }

    public ConstructionBehavior(int caseIndex = 0)
    {
        Case = Cases[caseIndex];
        var source = ConstructionAtlas.All[Case.Source];
        var original = source.Surface;
        var saved = FamilyUseCodec.Encode(original.Table, original.Entry, original.Uses);
        var decoded = FamilyUseCodec.Decode(saved);
        var retained = new ConstructionSurface(decoded.Table, decoded.Entry, decoded.Uses);
        Example = new(source.Pattern, source.Method, Case.Description, retained, source.Points.ToArray());
        Dictionary<int, PassageLaw>? laws = null;
        if (Case.SelectedUses is { } choices)
        {
            laws = new()
            {
                [choices[0]] = PassageLaw.Controlled(whenPresent: false),
                [choices[1]] = PassageLaw.Controlled(whenPresent: true)
            };
        }
        Walk = new(retained.Table, retained.Entry, retained.Uses, Case.StartSite, laws, observation);
        Adjustment = new(retained.Table, retained.Entry, retained.Uses, Case.ResponseField,
            Case.Pin ? [Case.ResponseField!.Value] : []);
    }

    /// <summary>Supplied public evidence. Changing a publication does not move
    /// activation or rewrite any retained family field.</summary>
    public void SetRouteObservation(byte support)
    {
        if (support > 3) throw new ArgumentOutOfRangeException(nameof(support));
        observation.Support = support;
    }

    private static int Find(string pattern, string method) => Enumerable.Range(0, ConstructionAtlas.All.Count)
        .Single(i => ConstructionAtlas.All[i].Pattern == pattern && ConstructionAtlas.All[i].Method == method);

    public static IReadOnlyList<ConstructionBehaviorCase> Cases { get; } = Array.AsReadOnly(new[]
    {
        new ConstructionBehaviorCase("Path · common reach", "One ordered cycle and one shared reach remain. A request can walk the cycle; one adjustment is observed on all four links.", Find("Square", "Repeated directed path")),
        new ConstructionBehaviorCase("Parallel sides · common reach", "The retained side uses point right and up. Two departures compete at the starting corner until an explicit choice law is supplied.", Find("Square", "Two ordered rails")),
        new ConstructionBehaviorCase("Radial result · builder absent", "After removing the radial source, this result retains exactly the same family and use encoding as the path example. The same behavior must agree.", Find("Square", "Radial parts and chords")),
        new ConstructionBehaviorCase("Path · two reach roles", "Opposite links reuse two independent reach roles. Adjusting the first changes only the links that reference it; no numeric lengths are supplied.", Find("Rectangle", "Repeated directed path")),
        new ConstructionBehaviorCase("Y · competing departures", "Both outward arms are enabled. The activation waits at their shared junction rather than being copied or selected by a hidden priority.", Find("Y", "Branch reading")),
        new ConstructionBehaviorCase("Y · explicit selector", "The same junction now has complementary law bits on the two arms. A known selector picks one; an uncertain selector leaves both predicted and the activation waiting.", Find("Y", "Branch reading"), SelectedUses: [2, 4]),
        new ConstructionBehaviorCase("Y · incoming arm", "The retained merge reading permits an activation on one arm to enter the shared stem. This single-request trial does not establish simultaneous merge arithmetic.", Find("Y", "Merge reading"), StartSite: 2),
        new ConstructionBehaviorCase("House · shared seam", "Roof and room use one seam and its value field. One adjustment is seen by both groups; routing still needs explicit choices at competing departures.", Find("House", "Roof and room share seam")),
        new ConstructionBehaviorCase("House · independent seams", "The two seams have matching endpoints but independent links and value fields. An adjustment of the roof seam does not move the room seam's read.", Find("House", "Coincident independent seams")),
        new ConstructionBehaviorCase("Path · pinned adjustment", "An explicitly connected pin blocks the reach adjustment. The request stays waiting; the path itself has not vanished.", Find("Square", "Repeated directed path"), Pin: true),
        new ConstructionBehaviorCase("Path · open response", "The input has no selected adjustment binding. Its request stays waiting and no farther reach is invented.", Find("Square", "Repeated directed path"), ResponseField: null),
        new ConstructionBehaviorCase("Church · shared face traces", "The independent artifact keeps four face walks and their seams. Unselected junction alternatives remain visible; a field adjustment affects only its actual references.", Find("Church", "Body roof tower and spire"))
    });
}
