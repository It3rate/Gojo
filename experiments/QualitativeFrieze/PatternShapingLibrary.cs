using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A display adapter over one authored native phase-to-word program.
/// The name supplies no execution meaning. Words are ordered sample emissions;
/// repeated actual moves inside a word are repeated reach participation.</summary>
public sealed class PatternShapingEntry
{
    public string Name { get; }
    public FriezeFixture Fixture { get; }
    public Relation Words { get; }
    public Relation Surface { get; }
    public int PhaseCount => Fixture.Components.Sum(component => Entries(component.Domain).Length);

    internal PatternShapingEntry(string name, FriezeFixture fixture, Relation words)
    {
        Name = name; Fixture = fixture; Words = words;
        Surface = new Relation(fixture.Surface, words);
    }

    private static IElement[] Entries(Relation scope) =>
        StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}

/// <summary>Deliberately shaped qualitative centerline programs, inspired by
/// simple fret and meander families rather than exact tracings of a reference.
/// No source coordinates, pixel widths, random search or numeric calibration
/// enter these graphs. Ribbon width and white-space claims need a separate read.
/// </summary>
public sealed class PatternShapingLibrary
{
    // entry = fixture-surface -> ordered-words
    // centering-edit = (old-fixture-surface -> new-fixture-surface) ->
    //                  (old-start-bindings -> new-start-bindings)
    // library = (motion-frame -> Closed(shared-reaches)) ->
    //           (Closed(entry-surfaces) -> Closed(actual-edits))
    public QualitativeMotionFrame Frame { get; }
    public IElement HorizontalReach { get; }
    public IElement VerticalReach { get; }
    public IReadOnlyList<PatternShapingEntry> Patterns { get; }
    public Relation Surface { get; }
    public Relation Edits { get; }
    public FriezeFixture CenteredMeander { get; }
    public Relation CenteredMeanderWords { get; }
    public Relation CenteringEdit { get; }

    public PatternShapingLibrary(QualitativeMotionFrame? frame = null,
        IElement? horizontalReach = null, IElement? verticalReach = null)
    {
        Frame = frame ?? new QualitativeMotionFrame();
        HorizontalReach = horizontalReach ?? new Placeholder();
        VerticalReach = verticalReach ?? new Placeholder();
        var r = QualitativeMotionFrame.Move(Frame.Right, HorizontalReach);
        var l = QualitativeMotionFrame.Move(Frame.Left, HorizontalReach);
        var u = QualitativeMotionFrame.Move(Frame.Up, VerticalReach);
        var d = QualitativeMotionFrame.Move(Frame.Down, VerticalReach);

        // Each inner closed scope is one stroke/sample, not a host distance.
        // For example S(r,r) retains the very same shared reach twice.
        var square = S(S(u), S(r), S(d), S(r));
        var stepped = S(S(u), S(r), S(u), S(r), S(d), S(r), S(d), S(r));
        var folded = S(S(r), S(u), S(l), S(u), S(r, r), S(d, d), S(r));
        var broad = S(S(r, r), S(u), S(l, l), S(u), S(r, r, r), S(d, d), S(r, r));
        var alternating = Join(folded, ReverseWords(folded));

        // An inward turn leaves an adjacent, distinct route back out. The
        // shared one-reach corridors are explicit in these ordered words.
        var nested = S(S(u, u, u, u), S(r, r, r, r), S(d, d, d), S(l, l),
            S(u), S(r), S(u), S(l, l), S(d, d, d), S(r, r, r, r));
        var deep = S(S(u, u, u, u, u, u, u), S(r, r, r, r, r, r),
            S(d, d, d, d, d), S(l, l, l, l), S(u, u, u), S(r, r),
            S(d), S(l), S(d), S(r, r), S(u, u, u), S(l, l, l, l),
            S(d, d, d, d, d, d), S(r, r, r, r, r, r));
        var alternatingNested = Join(nested, ReverseWords(nested));

        var entries = new[]
        {
            Entry("Square wave", square),
            Entry("Stepped battlement", stepped),
            Entry("Folded key", folded),
            Entry("Broad folded key", broad),
            Entry("Alternating opposed keys", alternating),
            Entry("Nested key", nested),
            Entry("Deeper nested key", deep),
            Entry("Alternating nested keys", alternatingNested)
        };
        Patterns = Array.AsReadOnly(entries);

        // Move the phase origin past the first rise of the folded key. This
        // selects an existing phase; it neither changes the emitted motions nor
        // assigns a numeric vertical translation. The old view remains intact.
        var original = entries[2].Fixture;
        var firstRisePhase = Entries(original.Components.Single().Domain)[1];
        var centeredStarts = S(new Relation(original.RootPort, S(firstRisePhase)));
        CenteredMeander = original.WithStarts(centeredStarts, "Folded key — centered phase origin");
        CenteredMeanderWords = S(Entries(folded).Skip(2).Concat(Entries(folded).Take(2)).ToArray());
        CenteringEdit = new Relation(new Relation(original.Surface, CenteredMeander.Surface),
            new Relation(original.Starts, centeredStarts));
        Edits = S(CenteringEdit);
        Surface = new Relation(new Relation(Frame.Surface, S(HorizontalReach, VerticalReach)),
            new Relation(S(entries.Select(entry => (IElement)entry.Surface).ToArray()), Edits));

        PatternShapingEntry Entry(string name, Relation words) =>
            new(name, FriezeFixture.Sequence(Frame, words, name), words);
    }

    // Reversing word order does not reverse relation order inside a move or
    // negate its direction. In these vertically neutral cells it authors an
    // opposed neighboring cell while retaining exact original word identities.
    private static Relation ReverseWords(Relation words) => S(Entries(words).Reverse().ToArray());
    private static Relation Join(Relation first, Relation second) => S(Entries(first).Concat(Entries(second)).ToArray());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] Entries(Relation scope) =>
        StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
