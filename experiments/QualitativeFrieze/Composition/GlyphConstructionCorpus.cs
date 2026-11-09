using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Authored glyph fixtures, not inferred handwriting conventions.
/// surface = (frame -> Closed(actual sections)) -> Closed(ordered pen strokes)
/// pen stroke = Closed(ordered visits)
/// visit = actual section -> ((traversal start -> traversal end) -> ordered control-word references)
/// A repeated visit does not create a second material section. Separate stroke
/// groups mean a lifted pen; they do not disconnect the finished shared sites.
/// Coordinates are one illustrative native affine construction, not universal
/// glyph dimensions. All descriptive strings disappear without losing the graph
/// or its selected stroke order. No flow permission follows from that history.</summary>
public static class GlyphConstructionCorpus
{
    public const string CapitalGuide = "https://www.lwtears.com/sites/default/files/2021-12/HWT%20Capital%20Letter%20Formation%20Chart.pdf";
    public const string ManuscriptGuide = "https://media.zaner-bloser.com/handwriting/pdfs/ZB_HW_Manuscript_Practice_Package.pdf";
    public sealed record Point(long X, long Y);
    public sealed record SectionView(int Index, int StartSite, int EndSite, Point[] Controls,
        bool? Straight, Point StartTangent, Point EndTangent);
    public sealed record VisitView(int Section, bool Reverse);
    public sealed record StrokeView(int Index, VisitView[] Visits, bool Continuous, bool Closed);
    public sealed record SiteView(int Index, long X, long Y, int Degree, string Joint,
        int OpposedPairs, int CoorientedPairs, bool DirectionComplete);
    public sealed record TopologySummary(int Components, int FreeEnds, int Branches, int CycleRank,
        int PenStrokes, int RevisitedSections, bool Complete);
    public sealed record Fixture(string Glyph, string VariantId, string Title, string Construction, string Note,
        SectionView[] Lines, StrokeView[] Strokes, SiteView[] Sites, TopologySummary Topology)
    {
        public string SourceTitle { get; init; } = "Authored comparison of constructions";
        public string SourceUrl { get; init; } = "";
        public GlyphConstructionHistory.Report ConstructionRead { get; init; } = null!;
        [JsonIgnore] public Curve.Frame Frame { get; init; } = null!;
        [JsonIgnore] public Relation Sections { get; init; } = null!;
        [JsonIgnore] public Relation History { get; init; } = null!;
        [JsonIgnore] public Relation Surface { get; init; } = null!;
        [JsonIgnore] public Relation ReadWitness { get; init; } = null!;
        [JsonIgnore] public Relation JointWitnesses { get; init; } = null!;
    }

    public static IReadOnlyList<Fixture> Create()
    {
        var all = new List<Fixture>();
        void Add(string glyph, string explanation, Action<Builder> make, string note = "")
        {
            var b = new Builder(); make(b);
            var suppliedCard = glyph is "J" or "T" or "4" or "5";
            all.Add(b.Build(glyph, "baseline", "School-print construction", explanation, note) with
            {
                SourceTitle = suppliedCard ? "Supplied teaching card; compared with publisher guides" :
                    glyph is "G" || char.IsDigit(glyph[0]) ? "Zaner-Bloser manuscript formation guide" : "Learning Without Tears capital formation chart",
                SourceUrl = glyph is "G" || char.IsDigit(glyph[0]) ? ManuscriptGuide : CapitalGuide
            });
        }
        Add("A", "Start at the apex and descend the left leg. Lift, return to that top endpoint and descend the right leg. Return to an interior landmark on the left leg and join across to the right leg.", b =>
        {
            b.P("t",4,10).P("lm",2,5).P("bl",0,0).P("rm",6,5).P("br",8,0);
            b.L("t","lm").L("lm","bl").L("t","rm").L("rm","br").L("lm","rm"); b.Stroke(0,1).Stroke(2,3).Stroke(4);
        });
        Add("B", "Draw the stem from top to bottom. Lift and return to its top endpoint; draw the upper bowl back to the middle landmark, then continue the lower bowl to the bottom endpoint.", b =>
        {
            Stem(b); b.C("t","m",7,10,7,5).C("m","b",8,5,8,0); b.Stroke(0,1).Stroke(2,3);
        });
        Add("C", "Make one open curved return from the upper right to the lower right; the two ends remain distinct.", b =>
        { b.P("t",7,9).P("b",7,1).C("t","b",-2,12,-2,-2).Stroke(0); });
        Add("D", "Draw the stem downwards. Lift and return to its top endpoint, then curve around to stop at the existing bottom endpoint.", b =>
        { b.P("t",0,10).P("b",0,0).L("t","b").C("t","b",9,10,9,0).Stroke(0).Stroke(1); });
        Add("E", "Draw the stem downwards. Return to its top endpoint for the top bar, to an interior landmark for the middle bar, and to its bottom endpoint for the bottom bar. Each bar leaves toward the right.", b =>
        {
            Stem(b); b.P("tr",8,10).P("mr",6,5).P("br",8,0);
            b.L("t","tr").L("m","mr").L("b","br"); b.Stroke(0,1).Stroke(2).Stroke(3).Stroke(4);
        });
        Add("F", "Draw the stem downwards. Lift and return to its top endpoint to attach a rightward bar. Lift again and return to an interior landmark on the existing stem to begin a second rightward branch.", b =>
        { Stem(b); b.P("tr",8,10).P("mr",6,5).L("t","tr").L("m","mr"); b.Stroke(0,1).Stroke(2).Stroke(3); });
        Add("G", "Follow an open curved return, rise a short distance at its lower end, then turn inward.", b =>
        { b.P("t",7,9).P("b",7,1).P("r",7,5).P("in",4,5).C("t","b",-2,12,-2,-2).L("b","r").L("r","in").Stroke(0,1,2); });
        Add("H", "Draw the left stem downwards. Use its top as a reference, move to an estimated rightward offset, and draw the parallel right stem downwards. Return to an interior landmark on the left stem and draw across to stop at the corresponding landmark on the right stem.", b =>
        {
            b.P("lt",0,10).P("lm",0,5).P("lb",0,0).P("rt",8,10).P("rm",8,5).P("rb",8,0);
            b.L("lt","lm").L("lm","lb").L("rt","rm").L("rm","rb").L("lm","rm"); b.Stroke(0,1).Stroke(2,3).Stroke(4);
            b.OffsetStart(1,0,"lt","rt",8,0);
        }, "The offset is one supplied realization of an estimated placement. The bridge is drawn left-to-right; this history alone does not make later traffic one-way.");
        Add("I", "Draw the central stem downwards. Offset left from its top endpoint and draw the top bar through it; then offset left from the bottom endpoint and draw the bottom bar through that endpoint.", b =>
        {
            b.P("tl",0,10).P("t",4,10).P("tr",8,10).P("bl",0,0).P("b",4,0).P("br",8,0);
            b.L("tl","t").L("t","tr").L("t","b").L("bl","b").L("b","br"); b.Stroke(2).Stroke(0,1).Stroke(3,4);
            b.OffsetStart(1,0,"t","tl",-4,0).OffsetStart(2,0,"b","bl",-4,0);
        });
        Add("J", "Draw the top bar left to right. Lift and return to a landmark inside that bar, descend the stem, then curve left into the hook.", b =>
        { b.P("tl",0,10).P("t",6,10).P("tr",8,10).P("r",6,3).P("l",0,3).L("tl","t").L("t","tr").L("t","r").C("r","l",6,-1,0,-1).Stroke(0,1).Stroke(2,3); },
            "Top bar first follows the supplied card. The publisher chart also teaches a stem-and-hook-first variant, retained below.");
        Add("K", "Draw the stem downwards. Offset right from its top endpoint, then draw diagonally into the existing middle landmark and continue toward the lower right.", b =>
        { Stem(b); b.P("tr",8,10).P("br",8,0).L("tr","m").L("m","br"); b.Stroke(0,1).Stroke(2,3); b.OffsetStart(1,0,"t","tr",8,0); });
        Add("L", "Descend the stem and continue around the bottom corner without lifting the pen.", b =>
        { b.P("t",0,10).P("c",0,0).P("r",8,0).L("t","c").L("c","r").Stroke(0,1); });
        Add("M", "Draw the left stem downwards. Lift and return to its top, descend diagonally to the valley, rise to the upper right, then descend the right stem.", b =>
        { b.P("t",0,10).P("b",0,0).P("v",4,5).P("rt",8,10).P("rb",8,0).L("t","b").L("t","v").L("v","rt").L("rt","rb").Stroke(0).Stroke(1,2,3); });
        Add("N", "Draw the left stem downwards. Lift and return to its top endpoint, descend diagonally to the lower right, then continue up the right stem.", b =>
        { b.P("t",0,10).P("b",0,0).P("rb",8,0).P("rt",8,10).L("t","b").L("t","rb").L("rb","rt").Stroke(0).Stroke(1,2); },
            "The first stem descends; the later right stem rises in this taught construction. Its distinction from Z also depends on the selected frame.");
        Add("O", "Start at the top, travel leftwards around the closed contour, and return counterclockwise to the starting site.", Circle,
            "Four native quadratic sections provide one rounded outline; this is not an exact metric-circle claim.");
        Add("P", "Draw the stem downwards. Lift and return to its top endpoint, then draw a bowl that stops at an interior landmark on the existing stem.", b =>
        { Stem(b); b.C("t","m",8,10,8,5).Stroke(0,1).Stroke(2); });
        Add("Q", "Draw a closed rounded contour, lift, then draw a short diagonal across its lower right area.", b =>
        { Circle(b); b.P("a",5,3).P("z",9,-1).L("a","z").Stroke(4); b.OffsetStart(1,0,"b","a",1,3); },
            "The tail is a separate stroke with separate sites. A visible intersection does not silently weld it to the contour.");
        Add("R", "Draw the stem downwards. Lift and return to its top, draw the bowl back to the middle landmark, then continue directly into the diagonal leg.", b =>
        { Stem(b); b.P("r",8,0).C("t","m",8,10,8,5).L("m","r").Stroke(0,1).Stroke(2,3); });
        Add("S", "Follow an upper curved return into a middle site, then reverse the bending sense through the lower return.", b =>
        { b.P("t",7,9).P("m",4,5).P("b",1,1).C("t","m",-2,12,-2,6).C("m","b",10,4,10,-2).Stroke(0,1); });
        Add("T", "Draw the top bar through its central site, lift, then attach a descending stem to that site.", b =>
        { b.P("l",0,10).P("j",4,10).P("r",8,10).P("b",4,0).L("l","j").L("j","r").L("j","b").Stroke(0,1).Stroke(2); },
            "Top bar first follows the supplied card. A stem-first teaching variant is also shown; the resulting artifact has the same connectivity.");
        Add("U", "Descend the left side, follow a curved bottom return, and rise on the right without lifting.", b =>
        { b.P("lt",0,10).P("lb",0,3).P("rb",8,3).P("rt",8,10).L("lt","lb").C("lb","rb",0,-1,8,-1).L("rb","rt").Stroke(0,1,2); });
        Add("V", "Descend to one bottom site, then rise from that same site toward the opposite upper end.", b =>
        { b.Path((0,10),(4,0),(8,10)); }, "A low point is not, by itself, a certificate of physical instability; that needs a support and load context.");
        Add("W", "Descend, rise to an inner peak, descend again, then rise to the final endpoint.", b =>
        { b.Path((0,10),(2,0),(4,5),(6,0),(8,10)); });
        Add("X", "Draw a diagonal from upper left to lower right. Lift and offset to the upper right, then draw down to the lower left. Their central sites coincide in this view but remain independently identified.", b =>
        {
            b.P("a",0,10).P("ca",4,5).P("b",8,0).P("c",8,10).P("cb",4,5).P("d",0,0);
            b.L("a","ca").L("ca","b").L("c","cb").L("cb","d").Stroke(0,1).Stroke(2,3); b.OffsetStart(1,0,"a","c",8,0);
        }, "This is a crossing interpretation. See the joined variant for an explicit four-arm shared site.");
        Add("Y", "Descend from the upper left and stop at the junction landmark. Lift and offset to the upper right, draw into that existing landmark, then continue down the stem.", b =>
        { b.P("l",0,10).P("j",4,5).P("b",4,0).P("r",8,10).L("l","j").L("j","b").L("r","j").Stroke(0).Stroke(2,1); b.OffsetStart(1,0,"l","r",8,0); });
        Add("Z", "Cross the top, descend diagonally to the lower left, then cross the bottom in one path.", b =>
        { b.Path((0,10),(8,10),(0,0),(8,0)); }, "Rotate its viewing frame and the same unlabelled three-link topology admits another glyph interpretation.");
        Add("0", "Start at the top and travel leftwards around a counterclockwise closed contour. Its topology alone does not distinguish it from O.", Circle);
        Add("1", "Start at the top and draw one straight downward stroke to the bottom stop.", b =>
        { b.Path((4,10),(4,0)); }, "The plain school-print form follows the supplied card. A leading serif or baseline would be another construction.");
        Add("2", "Curve across the top, descend diagonally to the lower left, then turn along the baseline.", b =>
        { b.P("l",0,8).P("r",8,8).P("b",0,0).P("e",8,0).C("l","r",0,12,8,12).L("r","b").L("b","e").Stroke(0,1,2); });
        Add("3", "Make two successive rightward curved returns joined at a middle site, with no stem closing either bowl.", b =>
        { b.P("t",0,10).P("m",0,5).P("b",0,0).C("t","m",8,10,8,5).C("m","b",8,5,8,0).Stroke(0,1); });
        Add("4", "Descend diagonally, turn right through a crossing site, then lift and draw a stem through that site.", b =>
        {
            b.P("a",5,10).P("c",0,3).P("j",6,3).P("r",8,3).P("t",6,10).P("b",6,0);
            b.L("a","c").L("c","j").L("j","r").L("t","j").L("j","b").Stroke(0,1,2).Stroke(3,4);
            b.OffsetStart(1,0,"a","t",1,0);
        }, "Diagonal first follows the supplied card; Zaner-Bloser also teaches a vertical first arm. This open-top 4 has no cycle; the closed-top variant does.");
        Add("5", "Cross the top toward the left, descend a short stem, then make a curved lower return.", b =>
        { b.P("tr",8,10).P("tl",0,10).P("m",0,6).P("b",0,0).L("tr","tl").L("tl","m").C("m","b",10,8,10,-1).Stroke(0,1,2); },
            "One continuous top-bar-first stroke follows the supplied card. A separately added top bar is a legitimate alternate teaching convention.");
        Add("6", "Start at the free upper tail and curve down the outer left. Continue around the lower bowl, rise on its right side, and return through the upper bowl to stop at the earlier left-hand landmark.", b =>
        { b.P("t",7,10).P("j",0,4).P("r",8,3).C("t","j",3,8,-1,7).C("j","r",0,-1,8,-1).C("r","j",8,8,0,8).Stroke(0,1,2); });
        Add("7", "Cross the top and descend diagonally from its right endpoint without lifting.", b =>
        { b.Path((0,10),(8,10),(2,0)); });
        Add("8", "Start near the top, curve down the upper left and through the waist into the lower right. Round the bottom, pass through the waist again from the lower left, and finish up the upper right at the starting site.", b =>
        {
            b.P("j",4,5).P("t",4,10).P("b",4,0);
            b.C("t","j",-2,10,-2,8).C("j","b",10,2,11,0).C("b","j",-3,0,-2,2).C("j","t",10,8,10,10).Stroke(0,1,2,3);
        });
        Add("9", "Start near the upper right, curve leftwards around the bowl and back to the starting landmark, then continue into the descending tail.", b =>
        { b.P("j",7,9).P("l",0,7).P("b",1,0).C("j","l",3,12,-1,10).C("l","j",0,3,9,2).C("j","b",9,3,5,-1).Stroke(0,1,2); });

        Fixture Base(string glyph) => all.Single(f => f.Glyph == glyph && f.VariantId == "baseline");
        void History(string glyph, string id, string title, string description, params int[][] strokes)
        {
            var source = Base(glyph); var sections = E(source.Sections).Cast<Relation>().ToArray();
            var history = S(strokes.Select(stroke => (IElement)S(stroke.Select(i => Visit(source.Frame, sections[Math.Abs(i)-1], i < 0)).ToArray())).ToArray());
            var result = Read(new Relation(new Relation(source.Frame.Surface,source.Sections),history), glyph,id,title,description,
                "An alternative construction, not the selected school-print baseline. The actual sections and sites are retained; changing this history does not install a traffic law.");
            // Retain a placement only when the corresponding stroke still
            // starts at that same site. Reversing a stroke can invalidate its
            // old relocation plan without invalidating the new history.
            var originalStrokes = E(source.History); var newStrokes = E(history);
            var originalPlacements = (Relation)((Relation)source.ConstructionRead.Surface.Left).Right;
            var placements = E(originalPlacements).Cast<Relation>().SelectMany(row =>
            {
                var context = (Relation)row.Left; var pair = (Relation)context.Left; var sites = (Relation)context.Right;
                var target = Array.FindIndex(originalStrokes, x => ReferenceEquals(x,pair.Left));
                var previous = Array.FindIndex(originalStrokes, x => ReferenceEquals(x,pair.Right));
                if (target >= newStrokes.Length || previous >= newStrokes.Length || previous >= target) return Array.Empty<IElement>();
                var first = (Relation)E((Relation)newStrokes[target])[0];
                var newStart = ((Relation)((Relation)first.Right).Left).Left;
                if (!ReferenceEquals(newStart,sites.Right)) return Array.Empty<IElement>();
                return new IElement[] { GlyphConstructionHistory.Placement((Relation)newStrokes[target], (Relation)newStrokes[previous],
                    sites.Left, sites.Right, (Relation)row.Right) };
            }).ToArray();
            all.Add(result with { ConstructionRead = GlyphConstructionHistory.Read(result.Surface,S(placements)) });
        }
        History("L","attached","Stem plus attached foot","Draw the stem, lift, then attach the foot at its lower endpoint.", [1],[2]);
        History("F","corner-first","Continuous top corner","Start at the top bar's right end, turn down into the whole stem, then add the middle bar.", [-3,1,2],[4]);
        History("F","bars-incoming","Bars arrive at the stem","Draw the stem downwards, then draw each bar from the outside toward it. The upper bar stops at its top endpoint; the middle bar stops at its interior landmark.", [1,2],[-3],[-4]);
        History("H","bridge-reversed","Opposed bridge read","Draw the same two stems, then draw the bridge from right to left.", [1,2],[3,4],[-5]);
        History("T","two-corners","Two corner traversals","Turn from the left arm down the stem, lift, retrace that same stem upward, then turn onto the right arm.", [1,3],[-3,2]);
        History("Y","outward","Outward construction read","Rise through the stem and leave along the left arm, then draw the right arm outward from the junction.", [-2,-1],[-3]);
        History("A","continuous-apex","Continuous turn through the apex","Start at the left foot, rise to the apex and continue down the right leg, then add the crossbar. This contrasts with the taught two descending strokes.", [-2,-1,3,4],[5]);
        History("8","two-loops","Two separately traced loops","Complete the upper loop, lift, then complete the lower loop through the same actual waist. This contrasts with the continuous taught figure-eight crossing.", [1,4],[2,3]);
        History("D","continuous","Continuous stem and return","Descend the stem and continue around the curved return in reverse to close in one stroke.", [1,-2]);
        History("E","bar-first","Bars before stem","Draw the three bars first, then connect their left sites with the stem.", [3],[4],[5],[1,2]);
        History("T","stem-first","Teaching variant: stem first","Draw the stem downwards first, then draw the horizontal bar through its existing top endpoint.", [3],[1,2]);
        History("J","stem-first","Teaching variant: stem and hook first","Draw the descending stem and hook first, then draw the top bar through the existing stem endpoint.", [3,4],[1,2]);
        History("G","attached-bar","Teaching variant: separately attached bar","Draw the curved body and short rise first, then draw the interior bar rightward to stop at the body's existing endpoint.", [1,2],[-3]);
        for (var i = 0; i < all.Count; i++)
            if (all[i].VariantId == "stem-first" || all[i].Glyph == "G" && all[i].VariantId == "attached-bar")
                all[i] = all[i] with { SourceTitle = "Learning Without Tears capital formation chart", SourceUrl = CapitalGuide };
        var x = new Builder(); x.P("a",0,10).P("j",4,5).P("b",8,0).P("c",8,10).P("d",0,0)
            .L("a","j").L("j","b").L("c","j").L("j","d").Stroke(0,1).Stroke(2,3);
        x.OffsetStart(1,0,"a","c",8,0);
        all.Add(x.Build("X","joined","Four-arm shared junction","Draw two diagonals through one explicitly shared central site.",
            "The silhouette matches the crossing fixture, but the selected incidence graph has one component and a degree-four junction. Flow routes are still a separate question."));
        var four = new Builder(); four.P("t",6,10).P("c",0,3).P("j",6,3).P("r",8,3).P("b",6,0)
            .L("t","c").L("c","j").L("j","r").L("t","j").L("j","b").Stroke(0,1,2).Stroke(3,4);
        all.Add(four.Build("4","closed-top","Closed-top construction","Join the diagonal and vertical strokes at their upper endpoint, creating a triangular cycle.",
            "This changes the actual topology, unlike merely changing pen order."));
        return all.AsReadOnly();
    }

    private static void Stem(Builder b) => b.P("t",0,10).P("m",0,5).P("b",0,0).L("t","m").L("m","b");
    private static void Circle(Builder b) => b.P("t",4,10).P("l",0,5).P("b",4,0).P("r",8,5)
        .Q("t","l",0,10).Q("l","b",0,0).Q("b","r",8,0).Q("r","t",8,10).Stroke(0,1,2,3);

    public static Relation Visit(Curve.Frame frame, Relation section, bool reverse = false)
    {
        var read = frame.Read(section); var words = E(read.ControlWords);
        return new(section, new Relation(new Relation(reverse ? read.End : read.Start, reverse ? read.Start : read.End),
            S(reverse ? words.Reverse().ToArray() : words)));
    }

    /// <summary>Exact local grammar reader. Metadata supplies captions only.
    /// Invalid histories and inconsistent site coordinates stay incomplete;
    /// coordinate equality never joins two independent native sites.</summary>
    public static Fixture Read(Relation surface, string glyph = "?", string variantId = "read", string title = "Native read",
        string construction = "", string note = "")
    {
        if (surface is not { Left: Relation { Left: Relation basis, Right: Relation sections }, Right: Relation history } ||
            !Curve.Frame.TryFromSurface(basis, out var frame))
            throw new ArgumentException("Expected the exact glyph construction surface grammar.", nameof(surface));
        var complete = Closed(sections) && Closed(history);
        var actual = E(sections).OfType<Relation>().ToArray();
        complete &= actual.Length == E(sections).Length && actual.Distinct(ReferenceEqualityComparer.Instance).Count() == actual.Length;
        var incidenceKnown = Closed(sections) && actual.Length == E(sections).Length &&
            actual.Distinct(ReferenceEqualityComparer.Instance).Count() == actual.Length;
        var sites = new List<IElement>(); var positions = new List<Point>(); var degrees = new List<int>();
        var lines = new List<SectionView>(); var problems = new List<IElement>(); var reads = new List<IElement>();
        var conflictingSites = new HashSet<int>();
        var tangentRays = new List<(int Site, Curve.Vector Vector, Relation Native)>();
        int Site(IElement site, Point point)
        {
            var index = sites.FindIndex(s => ReferenceEquals(s,site));
            if (index >= 0) { if (positions[index] != point) { complete = false; conflictingSites.Add(index); problems.Add(new Relation(site, StructuralConstraint.Open())); } return index; }
            sites.Add(site); positions.Add(point); degrees.Add(0); return sites.Count-1;
        }
        for (var i = 0; i < actual.Length; i++)
        {
            var read = frame.Read(actual[i]); reads.Add(read.Surface);
            if (!read.Complete) { complete = false; incidenceKnown = false; problems.Add(actual[i]); continue; }
            var points = read.Controls.Select(P).ToArray(); var a = Site(read.Start,points[0]); var b = Site(read.End,points[^1]);
            degrees[a]++; degrees[b]++;
            lines.Add(new(i,a,b,points,read.Straight,P(read.StartVector),P(read.EndVector)));
            tangentRays.Add((a,read.StartVector,new Relation(actual[i],new Relation(read.Start,read.StartTangent))));
            tangentRays.Add((b,-read.EndVector,new Relation(actual[i],new Relation(read.End,Oppose(frame,read.EndTangent)))));
        }
        var strokeViews = new List<StrokeView>(); var visitsPerSection = new int[actual.Length];
        foreach (var entry in E(history))
        {
            if (entry is not Relation stroke) { complete = false; problems.Add(entry); continue; }
            var valid = Closed(stroke); var visits = new List<VisitView>(); IElement? first = null; IElement? previous = null;
            foreach (var item in E(stroke))
            {
                if (item is not Relation { Left: Relation edge, Right: Relation { Left: Relation ends, Right: Relation words } })
                { valid = false; problems.Add(item); continue; }
                var index = Array.FindIndex(actual,s => ReferenceEquals(s,edge)); var read = frame.Read(edge);
                if (index < 0 || !read.Complete || !Closed(words)) { valid = false; problems.Add(item); continue; }
                var ordered = E(words); var sourceWords = E(read.ControlWords);
                bool SameWords(IEnumerable<IElement> a) => a.SequenceEqual(ordered,ReferenceEqualityComparer.Instance);
                var forward = ReferenceEquals(ends.Left,read.Start) && ReferenceEquals(ends.Right,read.End) && SameWords(sourceWords);
                var reverse = ReferenceEquals(ends.Left,read.End) && ReferenceEquals(ends.Right,read.Start) && SameWords(sourceWords.Reverse());
                if (!forward && !reverse) { valid = false; problems.Add(item); continue; }
                if (previous is not null && !ReferenceEquals(previous,ends.Left)) { valid = false; problems.Add(new Relation(previous,ends.Left)); }
                first ??= ends.Left; previous = ends.Right; visits.Add(new(index,!forward)); visitsPerSection[index]++;
            }
            if (visits.Count == 0) valid = false;
            complete &= valid;
            strokeViews.Add(new(strokeViews.Count,visits.ToArray(),valid,valid && ReferenceEquals(first,previous)));
        }
        if (visitsPerSection.Any(v => v == 0)) { complete = false; problems.Add(history); }
        var parents = Enumerable.Range(0,sites.Count).ToArray();
        int Find(int i) { while (parents[i] != i) { parents[i] = parents[parents[i]]; i = parents[i]; } return i; }
        foreach (var line in lines) parents[Find(line.EndSite)] = Find(line.StartSite);
        var components = Enumerable.Range(0,sites.Count).Select(Find).Distinct().Count();
        var topology = new TopologySummary(components,degrees.Count(d=>d==1),degrees.Count(d=>d>=3),
            lines.Count-sites.Count+components,strokeViews.Count,visitsPerSection.Sum(v=>Math.Max(0,v-1)),complete);
        // Every ray retains the actual section, actual endpoint and outward
        // tangent word. Pair comparisons are observations in this one frame;
        // they do not introduce an angle law or flow between the paired rays.
        var siteViews = new List<SiteView>(); var joints = new List<IElement>();
        for(var i=0;i<sites.Count;i++)
        {
            var rays=tangentRays.Where(r=>r.Site==i).ToArray(); var pairs=new List<IElement>();
            var opposed=0; var cooriented=0; var oppositeCounts=new int[rays.Length];
            var known=incidenceKnown && !conflictingSites.Contains(i) && rays.All(r=>!r.Vector.Zero);
            for(var a=0;a<rays.Length;a++) for(var b=a+1;b<rays.Length;b++)
            {
                pairs.Add(new Relation(rays[a].Native,rays[b].Native));
                if(!known || !Curve.Vector.Cross(rays[a].Vector,rays[b].Vector).IsZero) continue;
                var sign=Curve.Vector.Dot(rays[a].Vector,rays[b].Vector).Sign;
                if(sign<0) { opposed++; oppositeCounts[a]++; oppositeCounts[b]++; }
                else if(sign>0) cooriented++;
            }
            var joint=!known ? "unresolved" : rays.Length switch
            {
                1=>"endpoint",
                2 when opposed==1=>"tangent-continuation",
                2 when cooriented==1=>"fold-back",
                2=>"bend",
                3 when opposed==1 && cooriented==0=>"T-like",
                3=>"three-arm",
                4 when opposed==2 && oppositeCounts.All(n=>n==1)=>"crossing-like",
                _=>"multi-arm"
            };
            siteViews.Add(new(i,positions[i].X,positions[i].Y,degrees[i],joint,opposed,cooriented,known));
            var rayScope=S(rays.Select(r=>(IElement)r.Native).ToArray());
            joints.Add(new Relation(new Relation(surface,sites[i]),new Relation(rayScope,
                new Relation(S(pairs.ToArray()),known ? S() : StructuralConstraint.Open(rayScope)))));
        }
        var jointWitnesses=S(joints.ToArray());
        var witness = new Relation(surface,new Relation(S(reads.ToArray()),new Relation(jointWitnesses,S(problems.ToArray()))));
        return new(glyph,variantId,title,construction,note,lines.ToArray(),strokeViews.ToArray(),
            siteViews.ToArray(),topology)
            { Frame = frame, Sections = sections, History = history, Surface = surface, ReadWitness = witness,
              JointWitnesses = jointWitnesses, ConstructionRead = GlyphConstructionHistory.Read(surface) };
    }

    private static Point P(Curve.Vector vector) => new(checked((long)vector.X),checked((long)vector.Y));
    private static Relation Oppose(Curve.Frame frame,Relation word) => S(E(word).Cast<Relation>().Select(move=>
    {
        frame.Motion.Direction(move,out var axis,out var forward,out var reach);
        return (IElement)QualitativeMotionFrame.Move(ReferenceEquals(axis,frame.Motion.Horizontal)
            ? forward ? frame.Motion.Left : frame.Motion.Right : forward ? frame.Motion.Down : frame.Motion.Up,reach);
    }).ToArray());
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope,out _,out var seam,out var tail) && seam.Equals(tail);

    private sealed class Builder
    {
        private readonly Curve.Frame frame = new(new QualitativeMotionFrame());
        private readonly Dictionary<string,(IElement Site,Relation Word)> sites = [];
        private readonly List<Relation> sections = []; private readonly List<Relation> strokes = [];
        private readonly List<(int Target, int Source, string Landmark, string Start, int X, int Y)> offsets = [];
        public Builder P(string name,int x,int y) { sites.Add(name,(new Placeholder(),frame.Word(x,y))); return this; }
        public Builder L(string from,string to) { sections.Add(frame.Line(sites[from].Site,sites[to].Site,sites[from].Word,sites[to].Word)); return this; }
        public Builder Q(string from,string to,int x,int y)
        { sections.Add(frame.Quadratic(sites[from].Site,sites[to].Site,sites[from].Word,frame.Word(x,y),sites[to].Word)); return this; }
        public Builder C(string from,string to,int x1,int y1,int x2,int y2)
        { sections.Add(frame.Cubic(sites[from].Site,sites[to].Site,sites[from].Word,frame.Word(x1,y1),frame.Word(x2,y2),sites[to].Word)); return this; }
        public Builder Stroke(params int[] indices) { strokes.Add(S(indices.Select(i=>(IElement)Visit(frame,sections[i])).ToArray())); return this; }
        public Builder OffsetStart(int target, int source, string landmark, string start, int x, int y)
        { offsets.Add((target,source,landmark,start,x,y)); return this; }
        public void Path(params (int X,int Y)[] points)
        { for(var i=0;i<points.Length;i++) P($"p{i}",points[i].X,points[i].Y); for(var i=0;i<points.Length-1;i++) L($"p{i}",$"p{i+1}"); Stroke(Enumerable.Range(0,sections.Count).ToArray()); }
        public Fixture Build(string glyph,string id,string title,string text,string note)
        {
            var fixture = Read(new Relation(new Relation(frame.Surface,S(sections.Cast<IElement>().ToArray())),S(strokes.Cast<IElement>().ToArray())),glyph,id,title,text,note);
            var placements = S(offsets.Select(p => (IElement)GlyphConstructionHistory.Placement(strokes[p.Target],strokes[p.Source],
                sites[p.Landmark].Site,sites[p.Start].Site,frame.Word(p.X,p.Y))).ToArray());
            return fixture with { ConstructionRead = GlyphConstructionHistory.Read(fixture.Surface,placements) };
        }
    }
}
