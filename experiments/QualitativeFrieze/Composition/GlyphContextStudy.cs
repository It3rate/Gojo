using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// A deliberately narrow context read: where does a supplied load projection
/// lie relative to explicitly selected contacts? This is not a statics solver.
/// Ground, metric basis, contact identity and query are connected premises;
/// no glyph label selects the answer or supplies an invisible support.
/// </summary>
public static class GlyphContextStudy
{
    // surface = Closed(frame, similarity-basis, Closed(sections),
    //                  ground-start-word -> ground-end-word,
    //                  Closed(actual contact sites), load-word)
    // evidence = surface -> Closed(contacts before projection,
    //                             at projection, after projection, unresolved)
    // Each comparison row is the actual contact site -> its position word.
    // "Before" and "after" belong to the ordered ground frame, not to gravity.
    public sealed record ContextRead(bool Complete, string ProjectionRead, int ContactCount,
        double[] Projection, Relation Evidence);
    public sealed record Case(string Id, string Title, string Glyph, string Interpretation,
        string Construction, string Limits, long[][][] Lines, long[][] Contacts,
        long[] Load, long[][] Ground, double[] Projection, int ContactCount,
        string ProjectionRead, bool NativeComplete)
    {
        [JsonIgnore] public Relation Surface { get; init; } = null!;
        [JsonIgnore] public Relation Evidence { get; init; } = null!;
    }
    public sealed record Transfer(string Name, string Glyphs, string Structure,
        string Use, string Needs, string Limit);
    public sealed record Report(Case[] Cases, Transfer[] Transfers, string[] Limits)
    {
        [JsonIgnore] public Relation ArchiveRoot { get; init; } = null!;
    }

    public static ContextRead Read(Relation surface)
    {
        var before = new List<IElement>(); var at = new List<IElement>();
        var after = new List<IElement>(); var unknown = new List<IElement>();
        double[] projection = [];
        var contactCount = 0;
        ContextRead Finish()
        {
            var complete = unknown.Count == 0;
            var classification = !complete || contactCount == 0 ? "unresolved" :
                before.Count > 0 && after.Count > 0 ? "inside" :
                at.Count > 0 ? "boundary" : "outside";
            return new(complete, classification, contactCount, projection,
                new Relation(surface, S(S(before.ToArray()), S(at.ToArray()), S(after.ToArray()), S(unknown.ToArray()))));
        }
        if (!Closed(surface) || E(surface) is not [Relation frameSurface, Relation basis,
                Relation sections, Relation ground, Relation contacts, Relation loadWord] ||
            !Curve.Frame.TryFromSurface(frameSurface, out var frame) ||
            !CurveShapeQualities.IsSimilarityBasis(frame, basis) || !Closed(sections) || !Closed(contacts) ||
            ground.Left is not Relation groundA || ground.Right is not Relation groundB ||
            !frame.Vector(groundA, out var a) || !frame.Vector(groundB, out var b) ||
            !frame.Vector(loadWord, out var load) || (b - a).Zero)
        { unknown.Add(surface); return Finish(); }

        // A single actual site cannot silently acquire conflicting positions.
        var positions = new Dictionary<IElement, (Curve.Vector Position, Relation Word)>(N.Participants.Instance);
        foreach (var entry in E(sections))
        {
            if (entry is not Relation section) { unknown.Add(entry); continue; }
            var read = frame.Read(section);
            if (!read.Complete) { unknown.Add(section); continue; }
            var words = E(read.ControlWords).Cast<Relation>().ToArray();
            Add(read.Start, read.Controls[0], words[0]); Add(read.End, read.Controls[^1], words[^1]);
        }
        void Add(IElement site, Curve.Vector value, Relation word)
        {
            if (positions.TryGetValue(site, out var previous) && previous.Position != value)
                unknown.Add(new Relation(site, new Relation(previous.Word, word)));
            else positions.TryAdd(site, (value, word));
        }
        var axis = b - a; var denominator = Curve.Vector.Dot(axis, axis);
        var query = Curve.Vector.Dot(load - a, axis);
        projection = [(double)a.X + (double)axis.X * (double)query / (double)denominator,
                      (double)a.Y + (double)axis.Y * (double)query / (double)denominator];
        var seen = new HashSet<IElement>(N.Participants.Instance);
        foreach (var site in E(contacts))
        {
            if (!seen.Add(site)) continue; // A second read of one contact is not a second support.
            if (!positions.TryGetValue(site, out var location) ||
                !Curve.Vector.Cross(location.Position - a, axis).IsZero)
            { unknown.Add(new Relation(site, ground)); continue; }
            contactCount++;
            var delta = Curve.Vector.Dot(location.Position - a, axis) - query;
            (delta.Sign < 0 ? before : delta.Sign > 0 ? after : at).Add(new Relation(site, location.Word));
        }
        return Finish();
    }

    public static Report Run()
    {
        var cases = new List<Case>();
        (int X, int Y)[][] a = [[(0, 0), (1, 2), (2, 4), (3, 2), (4, 0)], [(1, 2), (3, 2)]];
        (int X, int Y)[][] v = [[(0, 4), (2, 0), (4, 4)]];
        (int X, int Y)[][] arch = [[(0, 0), (2, 4), (4, 0)]];
        (int X, int Y)[][] n = [[(0, 0), (0, 4), (4, 0), (4, 4)]];
        (int X, int Y)[][] z = [[(0, 4), (4, 4), (0, 0), (4, 0)]];
        Add("a-centered", "A: between two feet", "A", a, [(0, 0), (4, 0)], (2, 3), (0, 0), (4, 0),
            "A two-sided support candidate.", "Two legs end on the declared ground; the crossbar is a separate attachment.");
        Add("a-shifted", "A: move the load, keep the letter", "A", a, [(0, 0), (4, 0)], (5, 3), (0, 0), (4, 0),
            "The same support no longer brackets the supplied load.", "Only the query location changes; the source shape and contacts are retained.");
        Add("v-point", "V: a single contact", "V", v, [(2, 0)], (2, 3), (0, 0), (4, 0),
            "A pivot or funnel is a plausible use; broad support is not supplied.", "Both arms meet at one actual site on the ground.");
        Add("v-shifted", "V: a small off-centre demand", "V", v, [(2, 0)], (3, 3), (0, 0), (4, 0),
            "A single contact does not bracket an offset load.", "One contact stays fixed while the selected load position changes.");
        Add("v-inverted", "Invert V: now two ground contacts", "V", arch, [(0, 0), (4, 0)], (2, 3), (0, 0), (4, 0),
            "The same path topology can serve as a two-legged support candidate.", "Reverse its vertical placement and declare the two lower endpoints as contacts.");
        Add("n-ground", "N: two feet, without a baseline bar", "N", n, [(0, 0), (4, 0)], (2, 3), (0, 0), (4, 0),
            "Bracing is a possible use, but the diagonal is not a material-strength proof.", "A continuous path visits both lower sites; they are explicitly chosen as contacts.");
        Add("z-ground", "Z: a baseline bar", "Z", z, [(0, 0), (4, 0)], (2, 3), (0, 0), (4, 0),
            "This coarse contact read agrees with N, despite the different baseline structure.", "The bar's endpoints are declared contacts. No continuous contact pressure distribution is inferred.");
        Add("z-wall", "Z: change the supporting frame", "Z", z, [(0, 0), (0, 4)], (3, 2), (0, 0), (0, 4),
            "Ground is contextual: this read uses a vertical wall as its reference.", "Choose the left two endpoints and project across onto the wall instead of down onto a floor.");
        Add("t-support", "T: a post and a top surface", "T", [[(0, 4), (2, 4), (4, 4)], [(2, 0), (2, 4)]],
            [(2, 0)], (2, 4), (0, 0), (4, 0), "A central pedestal needs additional support assumptions.",
            "The top and stem share an actual midpoint; the only declared floor contact is the post foot.");
        Add("h-support", "H: two posts joined across", "H", [[(0, 0), (0, 2), (0, 4)], [(4, 0), (4, 2), (4, 4)], [(0, 2), (4, 2)]],
            [(0, 0), (4, 0)], (2, 2), (0, 0), (4, 0), "A bridge or brace candidate with two supports.",
            "The crosspiece shares both mid-stem sites. A contact-span read says nothing about traversal permission.");
        Add("a-unselected", "A: the contacts were not supplied", "A", a, [], (2, 3), (0, 0), (4, 0),
            "A familiar outline is insufficient to answer this support query.", "The source shape remains present; the selected contact inventory is empty.");
        Add("a-open", "A: the contact inventory is incomplete", "A", a, [(0, 0)], (2, 3), (0, 0), (4, 0),
            "A partial read must not confidently report outside the support.", "One contact is supplied in an open inventory; other contacts remain unspecified.", openContacts: true);

        var transfers = new Transfer[]
        {
            new("Bend", "L, U, V, corners of Z", "An ordered handoff between two incident sections.",
                "Continue a cursor or packet around a change of heading.", "A connected continuation route; history alone does not install it.", "Does not establish a rigid joint, strength, or two-way traffic."),
            new("Attachment", "F, E, T", "A later element shares an existing endpoint or interior site.",
                "Add a branch, handle, joist, sensor or sign without replacing the carrier.", "Shared site plus a selected coupling or route, if interaction is intended.", "An attachment does not automatically give all incident paths permission to communicate."),
            new("Bridge", "H, A crossbar", "Two distinct local sites are joined by a third section.",
                "Exchange across previously separate strands or support a transverse span.", "Separate directional permissions, capacity and receiver availability for material exchange.", "One drawn bridge does not imply two-way use, simultaneous use or mechanical load bearing."),
            new("Choice", "T, Y, K", "One arrival can address several possible continuations.",
                "Select an outlet according to a control or retain unresolved alternatives.", "A finite law and a rule for actual acceptance; uncertain prediction must not copy a packet.", "A visual fork does not choose, prioritize or duplicate by itself."),
            new("Merge", "Y, junctions of M and W", "Several source routes meet one receiving site or passage.",
                "Combine observations, or compete for a material receiver.", "An observation OR law or an explicit conserving transfer; these are different uses.", "Participation OR is not addition of conserved material."),
            new("Crossover", "X, crossed 4, Q tail", "Two visible paths can coincide in projection while their sites remain distinct.",
                "Pass independently, or install an explicit exchange junction as an alternative.", "Actual identity/coupling evidence; crossing ink is not enough.", "The drawing cannot decide over/under, contact or weld."),
            new("Enclosure", "A, B, D, O, P, R, 0, 6, 8, 9", "Selected connected sections contain graph cycles.",
                "A loop, feedback route, container boundary or repeated circuit can be proposed.", "A planar boundary read for area; a directed route and controller for recurrent execution.", "An undirected graph cycle proves neither enclosed physical volume nor a running feedback loop."),
            new("Support and range", "A, H, N, V, Z, T", "A projected query lies between, on or outside selected contacts.",
                "A support-span check, bracketed estimate or between-anchors query.", "Declared frame, contacts and projection; material mechanics need further laws.", "No stiffness, mass distribution, friction, force balance or statics is solved here."),
            new("Surface for activity", "T, H, table-like assemblies", "A crosspiece exposes locations for additional relations.",
                "A workspace, exchange surface or display arrangement can attach to those sites.", "The interacting participants, goals, actions and histories must be connected in the relevant context.", "A table-shaped graph does not encode eating, paperwork, gambling or linguistic meanings by itself.")
        };
        return new(cases.ToArray(), transfers,
        ["These are authored constructions and contextual probes, not learned semantic categories.",
         "Contact-span inclusion is one geometric condition, not certification of stability. N and Z can agree under this coarse read.",
         "The local words count a shared uncalibrated reach. The renderer supplies pixels; no physical unit or universal glyph proportion is inferred.",
         "The native read is observational. It does not mutate the glyph, apply a force, or settle a mechanical structure."])
        { ArchiveRoot = S(cases.Select(c => (IElement)new Relation(c.Surface, c.Evidence)).ToArray()) };

        void Add(string id, string title, string glyph, (int X, int Y)[][] paths,
            (int X, int Y)[] contactPoints, (int X, int Y) load,
            (int X, int Y) ga, (int X, int Y) gb, string interpretation, string construction, bool openContacts = false)
        {
            var frame = new Curve.Frame(new QualitativeMotionFrame());
            // This fixture author explicitly chooses which occurrences share a
            // site. The runtime reader never welds by matching coordinates.
            var sites = paths.SelectMany(p => p).Distinct().ToDictionary(p => p, _ => (IElement)new Placeholder());
            var sections = paths.SelectMany(path => path.Zip(path.Skip(1), (p, q) => frame.Line(sites[p], sites[q],
                frame.Word(p.X, p.Y), frame.Word(q.X, q.Y)))).ToArray();
            var contacts = contactPoints.Select(p => sites[p]).ToArray();
            var surface = S(frame.Surface, CurveShapeQualities.SimilarityBasis(frame), S(sections),
                new Relation(frame.Word(ga.X, ga.Y), frame.Word(gb.X, gb.Y)),
                openContacts ? StructuralConstraint.Open(contacts) : S(contacts), frame.Word(load.X, load.Y));
            var read = Read(surface);
            cases.Add(new(id, title, glyph, interpretation, construction,
                "Projection relative to selected contacts only; no rigidity, forces, friction or dynamic stability law is supplied.",
                sections.Select(s => frame.Read(s).Controls.Select(p => new[] { checked((long)p.X), checked((long)p.Y) }).ToArray()).ToArray(),
                contactPoints.Select(p => new long[] { p.X, p.Y }).ToArray(), [load.X, load.Y], [[ga.X, ga.Y], [gb.X, gb.Y]],
                read.Projection, read.ContactCount, read.ProjectionRead, read.Complete)
            { Surface = surface, Evidence = read.Evidence });
        }
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
