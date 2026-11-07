using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentFlowPilot;

// These are source-selection and execution witnesses, not new ornament kinds.
// The trace consumed by the observers is emitted by packed Gojo motion voices.
internal sealed record ReferencePattern(string Id, string Name, string Source, string Scope,
    FretSourceRead Encoding);

internal sealed record ReferenceSelection(string Source, string Status, string Reason,
    string? PatternId = null);

internal static class ReferencePatterns
{
    private static byte Move(int axis, bool negative, int reach) =>
        (byte)((reach << 4) | (axis << 3) | ThreeBitMeasure.Encode(false, negative, true));

    internal static ReferencePattern[] Read() =>
    [
        new("running-key", "Running key", "Attachment 1, row 1",
            "One continuous six-reach course; the two detached border rules are excluded. " +
            "The raster suggests a shared spacing unit: horizontal reaches 3 and 1, vertical reaches 4 and 2. " +
            "Those are declared idealization bindings, not recovered physical measurements.",
            FretSymmetrySource.Read(
                [Move(0, false, 0), Move(0, false, 0), Move(0, true, 1)],
                [Move(1, false, 0), Move(1, true, 1), Move(1, true, 1)],
                new ParticipationStrip(0), [3, 1], [4, 2])),
        new("opposed-hook", "Opposed hook meander", "Attachment 1, row 2",
            "One continuous eight-reach course; detached border rules are excluded. " +
            "A four-reach hook repeats in the reflected vertical frame. The selected spacing binding is " +
            "horizontal 3 and 1 with two equal vertical reaches; exact ink thickness is not modeled.",
            FretSymmetrySource.ReadHook(3, 1, 1, 1)),
        new("battlement", "Battlement", "Attachment 2, row 5",
            "The square-wave course between two detached horizontal border rules. " +
            "Equal horizontal steps and equal rise/fall are the selected idealization. " +
            "One across/rise pair is replayed with the rise frame reflected, producing four reaches.",
            FretSymmetrySource.Read(
                [Move(0, false, 0)], [Move(1, false, 0)],
                new ParticipationStrip(0, 1), [1], [1]))
    ];

    // Rows are counted from the top of each attachment, and separately in the
    // left and right columns of attachment 3. No source bitmap is republished.
    // "Deferred" concerns this observer's grammar, not the pattern's quality.
    internal static ReferenceSelection[] Selections() =>
    [
        new("Attachment 1, row 1", "selected", "A clear single cardinal course, with detached framing rules omitted.", "running-key"),
        new("Attachment 1, row 2", "selected", "A clear single cardinal course, with detached framing rules omitted.", "opposed-hook"),
        new("Attachment 1, row 3", "deferred", "A meander alternates with separate four-pane square motifs; a multiple-component and joint read is needed."),
        new("Attachment 1, row 4", "deferred", "The orange design contains visible crossing/contact sites. Ink alone does not specify through-routing versus joined flow."),
        new("Attachment 1, row 5", "expanded study", "The later centre-line study resolves the small central S-shaped bridge: this is a continuous in-and-out spiral meander, not isolated finite spirals. Its fourteen-reach course is covered at /ornaments/centreline/ (A5)."),
        new("Attachment 1, row 6", "deferred", "Nested square outlines and grouped spiral motifs require separate components; colour may also distinguish their ownership."),
        new("Attachment 2, row 1", "deferred", "The continuous upper rail meets several stems. This resembles the opposed-hook family but is not the same single-course graph."),
        new("Attachment 2, row 2", "deferred", "Curved scrolls with a common baseline need curved carriers and explicit contact interpretation."),
        new("Attachment 2, row 3", "deferred", "The angular interlock has crossing/contact sites whose flow routing is not established by the raster."),
        new("Attachment 2, row 4", "deferred", "Diagonal crosses and diamond openings exceed the cardinal-only course grammar; crossings also need a routing read."),
        new("Attachment 2, row 5", "selected", "A clear square-wave course between detached border rules.", "battlement"),
        new("Attachment 2, row 6", "inner family only", "The familiar interlocking course is visible, but continuous outer rails meet it. The standalone interlock control does not represent those extra contacts.", "interlock"),
        new("Attachment 2, row 7", "deferred", "Leaves and closed curved lobes share a central stem; this is a branching, curved, multiple-component construction."),
        new("Attachment 2, row 8", "deferred", "Angular crossing motifs and detached dots need multiple pens and an explicit crossing interpretation."),
        new("Attachment 3, left row 1", "inner family only", "A narrow interlocking course appears inside attached framing. The framing contacts are outside the standalone course read.", "interlock"),
        new("Attachment 3, left row 2", "deferred", "A row of finite square spirals needs start/stop pens and separate components, rather than an invented continuous course."),
        new("Attachment 3, left row 3", "deferred", "Curved rosettes, lattice joints, and surrounding cells require an area and multiple-course analysis."),
        new("Attachment 3, left row 4", "deferred", "Diagonal latticework and curved enclosed lobes exceed the cardinal single-course grammar."),
        new("Attachment 3, left row 5", "deferred", "A diagonal zigzag against a filled band needs diagonal carriers and separate foreground/background scope."),
        new("Attachment 3, left row 6", "deferred", "Several diagonal crossing courses require a multiple-pen and crossing-aware observer."),
        new("Attachment 3, left row 7", "deferred", "A Greek-key-like course follows a curved band; preserving its local frame requires a curved carrier rather than flattening the image silently."),
        new("Attachment 3, left row 8", "deferred", "Curved feather leaves are outside this line-course reader."),
        new("Attachment 3, right row 1", "deferred", "Interwoven curved ribbons require curve and over/under interpretation."),
        new("Attachment 3, right row 2", "deferred", "Paired curved spirals and corner ornaments have separate ends and components."),
        new("Attachment 3, right row 3", "existing reference", "A clear standalone instance of the previously analyzed interlocking fret; reuse the existing idealization rather than count it as a new family.", "interlock"),
        new("Attachment 3, right row 4", "deferred", "The angular braid includes diagonal segments and over/under breaks, requiring separate course identities."),
        new("Attachment 3, right row 5", "deferred", "Paired curved leaves around a stem need a branching curved construction."),
        new("Attachment 3, right row 6", "deferred", "Filled curls and detached circles require area, curve, and multiple-component reads.")
    ];
}
