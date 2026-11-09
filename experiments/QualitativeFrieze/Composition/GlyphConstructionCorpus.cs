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
            all.Add(b.Build(glyph, "baseline", "One supplied construction", explanation, note));
        }
        Add("A", "Rise along the left leg to the apex, descend the right leg, then attach a crossbar between two interior sites.", b =>
        {
            b.P("bl",0,0).P("lm",2,5).P("t",4,10).P("rm",6,5).P("br",8,0);
            b.L("bl","lm").L("lm","t").L("t","rm").L("rm","br").L("lm","rm"); b.Stroke(0,1,2,3).Stroke(4);
        });
        Add("B", "Draw one stem through its midpoint, then return through the upper and lower curved bowls.", b =>
        {
            b.P("b",0,0).P("m",0,5).P("t",0,10);
            b.L("b","m").L("m","t").C("t","m",7,10,7,5).C("m","b",8,5,8,0); b.Stroke(0,1).Stroke(2,3);
        });
        Add("C", "Make one open curved return from the upper right to the lower right; the two ends remain distinct.", b =>
        { b.P("t",7,9).P("b",7,1).C("t","b",-2,12,-2,-2).Stroke(0); });
        Add("D", "Draw a straight stem, lift, then attach a curved return from its top to its bottom.", b =>
        { b.P("b",0,0).P("t",0,10).L("b","t").C("t","b",9,10,9,0).Stroke(0).Stroke(1); });
        Add("E", "Draw a stem through the middle attachment, then add the top, middle, and bottom bars.", b =>
        {
            Stem(b); b.P("tr",8,10).P("mr",6,5).P("br",8,0);
            b.L("t","tr").L("m","mr").L("b","br"); b.Stroke(0,1).Stroke(2).Stroke(3).Stroke(4);
        });
        Add("F", "Draw a stem through the middle attachment, then add a bar at its top and another at its midpoint.", b =>
        { Stem(b); b.P("tr",8,10).P("mr",6,5).L("t","tr").L("m","mr"); b.Stroke(0,1).Stroke(2).Stroke(3); });
        Add("G", "Follow an open curved return, rise a short distance at its lower end, then turn inward.", b =>
        { b.P("t",7,9).P("b",7,1).P("r",7,5).P("in",4,5).C("t","b",-2,12,-2,-2).L("b","r").L("r","in").Stroke(0,1,2); });
        Add("H", "Draw two stems, each with an interior site, then draw a bridge between those two sites.", b =>
        {
            b.P("lb",0,0).P("lm",0,5).P("lt",0,10).P("rb",8,0).P("rm",8,5).P("rt",8,10);
            b.L("lb","lm").L("lm","lt").L("rb","rm").L("rm","rt").L("lm","rm"); b.Stroke(0,1).Stroke(2,3).Stroke(4);
        }, "The bridge is drawn left-to-right here. That is historical order, not a one-way traffic law.");
        Add("I", "Draw a top bar, attach a central stem, then draw a bottom bar through its lower endpoint.", b =>
        {
            b.P("tl",0,10).P("t",4,10).P("tr",8,10).P("bl",0,0).P("b",4,0).P("br",8,0);
            b.L("tl","t").L("t","tr").L("t","b").L("bl","b").L("b","br"); b.Stroke(0,1).Stroke(2).Stroke(3,4);
        });
        Add("J", "Draw a top bar, then descend from its right end and curve left into an open hook.", b =>
        { b.P("tl",0,10).P("t",7,10).P("r",7,3).P("l",0,3).L("tl","t").L("t","r").C("r","l",7,-1,0,-1).Stroke(0).Stroke(1,2); });
        Add("K", "Draw the stem, then descend from the upper right into its midpoint and leave toward the lower right.", b =>
        { Stem(b); b.P("tr",8,10).P("br",8,0).L("tr","m").L("m","br"); b.Stroke(0,1).Stroke(2,3); });
        Add("L", "Descend the stem and continue around the bottom corner without lifting the pen.", b =>
        { b.P("t",0,10).P("c",0,0).P("r",8,0).L("t","c").L("c","r").Stroke(0,1); });
        Add("M", "Rise, descend toward an interior valley, rise again, and descend the opposite side in one continuous path.", b =>
        { b.Path((0,0),(0,10),(4,5),(8,10),(8,0)); });
        Add("N", "Rise on the left, descend diagonally to the lower right, then rise on the right.", b =>
        { b.Path((0,0),(0,10),(8,0),(8,10)); }, "Its distinction from Z depends on the selected horizontal and vertical frame.");
        Add("O", "Traverse a rounded closed contour and return to the actual starting site.", Circle,
            "Four native quadratic sections provide one rounded outline; this is not an exact metric-circle claim.");
        Add("P", "Draw a stem, then add a curved return from its top to an interior site, leaving a lower tail.", b =>
        { Stem(b); b.C("t","m",8,10,8,5).Stroke(0,1).Stroke(2); });
        Add("Q", "Draw a closed rounded contour, lift, then draw a short diagonal across its lower right area.", b =>
        { Circle(b); b.P("a",5,3).P("z",9,-1).L("a","z").Stroke(4); },
            "The tail is a separate stroke with separate sites. A visible intersection does not silently weld it to the contour.");
        Add("R", "Draw a stem and upper bowl, then add a diagonal leg from their shared interior junction.", b =>
        { Stem(b); b.P("r",8,0).C("t","m",8,10,8,5).L("m","r").Stroke(0,1).Stroke(2).Stroke(3); });
        Add("S", "Follow an upper curved return into a middle site, then reverse the bending sense through the lower return.", b =>
        { b.P("t",7,9).P("m",4,5).P("b",1,1).C("t","m",-2,12,-2,6).C("m","b",10,4,10,-2).Stroke(0,1); });
        Add("T", "Draw the top bar through its central site, lift, then attach a descending stem to that site.", b =>
        { b.P("l",0,10).P("j",4,10).P("r",8,10).P("b",4,0).L("l","j").L("j","r").L("j","b").Stroke(0,1).Stroke(2); });
        Add("U", "Descend the left side, follow a curved bottom return, and rise on the right without lifting.", b =>
        { b.P("lt",0,10).P("lb",0,3).P("rb",8,3).P("rt",8,10).L("lt","lb").C("lb","rb",0,-1,8,-1).L("rb","rt").Stroke(0,1,2); });
        Add("V", "Descend to one bottom site, then rise from that same site toward the opposite upper end.", b =>
        { b.Path((0,10),(4,0),(8,10)); }, "A low point is not, by itself, a certificate of physical instability; that needs a support and load context.");
        Add("W", "Descend, rise to an inner peak, descend again, then rise to the final endpoint.", b =>
        { b.Path((0,10),(2,0),(4,5),(6,0),(8,10)); });
        Add("X", "Draw two diagonals, lifting between them. Their central sites coincide in this view but are independently identified.", b =>
        {
            b.P("a",0,0).P("ca",4,5).P("b",8,10).P("c",0,10).P("cb",4,5).P("d",8,0);
            b.L("a","ca").L("ca","b").L("c","cb").L("cb","d").Stroke(0,1).Stroke(2,3);
        }, "This is a crossing interpretation. See the joined variant for an explicit four-arm shared site.");
        Add("Y", "Descend from the upper left through a junction into the stem, lift, then attach the upper right arm.", b =>
        { b.P("l",0,10).P("j",4,5).P("b",4,0).P("r",8,10).L("l","j").L("j","b").L("r","j").Stroke(0,1).Stroke(2); });
        Add("Z", "Cross the top, descend diagonally to the lower left, then cross the bottom in one path.", b =>
        { b.Path((0,10),(8,10),(0,0),(8,0)); }, "Rotate its viewing frame and the same unlabelled three-link topology admits another glyph interpretation.");
        Add("0", "Traverse one rounded closed contour; no topology-only read distinguishes this fixture from O.", Circle);
        Add("1", "Draw a short leading diagonal into the top of a stem, descend, then add a baseline through its bottom site.", b =>
        { b.P("a",2,8).P("t",4,10).P("b",4,0).P("l",1,0).P("r",7,0).L("a","t").L("t","b").L("l","b").L("b","r").Stroke(0,1).Stroke(2,3); });
        Add("2", "Curve across the top, descend diagonally to the lower left, then turn along the baseline.", b =>
        { b.P("l",0,8).P("r",8,8).P("b",0,0).P("e",8,0).C("l","r",0,12,8,12).L("r","b").L("b","e").Stroke(0,1,2); });
        Add("3", "Make two successive rightward curved returns joined at a middle site, with no stem closing either bowl.", b =>
        { b.P("t",0,10).P("m",0,5).P("b",0,0).C("t","m",8,10,8,5).C("m","b",8,5,8,0).Stroke(0,1); });
        Add("4", "Descend diagonally, turn right through a crossing site, then lift and draw a stem through that site.", b =>
        {
            b.P("a",5,10).P("c",0,3).P("j",6,3).P("r",8,3).P("t",6,10).P("b",6,0);
            b.L("a","c").L("c","j").L("j","r").L("t","j").L("j","b").Stroke(0,1,2).Stroke(3,4);
        }, "This open-top 4 has no cycle. The closed-top variant shares the upper starting site and does have one.");
        Add("5", "Cross the top toward the left, descend a short stem, then make a curved lower return.", b =>
        { b.P("tr",8,10).P("tl",0,10).P("m",0,6).P("b",0,0).L("tr","tl").L("tl","m").C("m","b",10,8,10,-1).Stroke(0,1,2); });
        Add("6", "Curve down from a free upper tail into a bowl, then travel around that bowl back to its actual joining site.", b =>
        { b.P("t",7,10).P("j",0,4).P("r",8,3).C("t","j",1,12,-1,7).C("j","r",0,8,8,8).C("r","j",8,-1,0,-1).Stroke(0,1,2); });
        Add("7", "Cross the top and descend diagonally from its right endpoint without lifting.", b =>
        { b.Path((0,10),(8,10),(2,0)); });
        Add("8", "Trace an upper loop, return to the waist, then trace a lower loop through the same waist.", b =>
        {
            b.P("j",4,5).P("t",4,10).P("b",4,0);
            b.C("j","t",-2,5,-2,10).C("t","j",10,10,10,5).C("j","b",-3,5,-3,0).C("b","j",11,0,11,5).Stroke(0,1,2,3);
        });
        Add("9", "Trace an upper loop and continue from its joining site into a descending curved tail.", b =>
        { b.P("j",8,6).P("l",0,7).P("b",1,0).C("j","l",8,11,0,11).C("l","j",0,3,8,3).C("j","b",8,1,5,-1).Stroke(0,1,2); });

        Fixture Base(string glyph) => all.Single(f => f.Glyph == glyph && f.VariantId == "baseline");
        void History(string glyph, string id, string title, string description, params int[][] strokes)
        {
            var source = Base(glyph); var sections = E(source.Sections).Cast<Relation>().ToArray();
            var history = S(strokes.Select(stroke => (IElement)S(stroke.Select(i => Visit(source.Frame, sections[Math.Abs(i)-1], i < 0)).ToArray())).ToArray());
            all.Add(Read(new Relation(new Relation(source.Frame.Surface,source.Sections),history), glyph,id,title,description,
                "Same actual sections and sites as the baseline. Only the selected ordered construction history changes; no traffic permission is implied."));
        }
        History("L","attached","Stem plus attached foot","Draw the stem, lift, then attach the foot at its lower endpoint.", [1],[2]);
        History("F","corner-first","Continuous top corner","Start at the top bar's right end, turn down into the whole stem, then add the middle bar.", [-3,-2,-1],[4]);
        History("H","bridge-reversed","Opposed bridge read","Draw the same two stems, then draw the bridge from right to left.", [1,2],[3,4],[-5]);
        History("T","two-corners","Two corner traversals","Turn from the left arm down the stem, lift, retrace that same stem upward, then turn onto the right arm.", [1,3],[-3,2]);
        History("Y","outward","Outward construction read","Rise through the stem and leave along the left arm, then draw the right arm outward from the junction.", [-2,-1],[-3]);
        History("A","separate-legs","Two legs plus crossbar","Draw the left leg to the apex, lift and draw the right leg down, then attach the crossbar.", [1,2],[3,4],[5]);
        History("8","two-loops","Two separately traced loops","Complete the upper loop, lift, then complete the lower loop through the same actual waist.", [1,2],[3,4]);
        History("D","continuous","Continuous stem and return","Rise along the stem and continue around the curved return to close in one stroke.", [1,2]);
        History("E","bar-first","Bars before stem","Draw the three bars first, then connect their left sites with the stem.", [3],[4],[5],[1,2]);
        var x = new Builder(); x.P("a",0,0).P("j",4,5).P("b",8,10).P("c",0,10).P("d",8,0)
            .L("a","j").L("j","b").L("c","j").L("j","d").Stroke(0,1).Stroke(2,3);
        all.Add(x.Build("X","joined","Four-arm shared junction","Draw two diagonals through one explicitly shared central site.",
            "The silhouette matches the crossing fixture, but the selected incidence graph has one component and a degree-four junction. Flow routes are still a separate question."));
        var four = new Builder(); four.P("t",6,10).P("c",0,3).P("j",6,3).P("r",8,3).P("b",6,0)
            .L("t","c").L("c","j").L("j","r").L("t","j").L("j","b").Stroke(0,1,2).Stroke(3,4);
        all.Add(four.Build("4","closed-top","Closed-top construction","Join the diagonal and vertical strokes at their upper endpoint, creating a triangular cycle.",
            "This changes the actual topology, unlike merely changing pen order."));
        return all.AsReadOnly();
    }

    private static void Stem(Builder b) => b.P("b",0,0).P("m",0,5).P("t",0,10).L("b","m").L("m","t");
    private static void Circle(Builder b) => b.P("t",4,10).P("r",8,5).P("b",4,0).P("l",0,5)
        .Q("t","r",8,10).Q("r","b",8,0).Q("b","l",0,0).Q("l","t",0,10).Stroke(0,1,2,3);

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
            { Frame = frame, Sections = sections, History = history, Surface = surface, ReadWitness = witness, JointWitnesses = jointWitnesses };
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
        public Builder P(string name,int x,int y) { sites.Add(name,(new Placeholder(),frame.Word(x,y))); return this; }
        public Builder L(string from,string to) { sections.Add(frame.Line(sites[from].Site,sites[to].Site,sites[from].Word,sites[to].Word)); return this; }
        public Builder Q(string from,string to,int x,int y)
        { sections.Add(frame.Quadratic(sites[from].Site,sites[to].Site,sites[from].Word,frame.Word(x,y),sites[to].Word)); return this; }
        public Builder C(string from,string to,int x1,int y1,int x2,int y2)
        { sections.Add(frame.Cubic(sites[from].Site,sites[to].Site,sites[from].Word,frame.Word(x1,y1),frame.Word(x2,y2),sites[to].Word)); return this; }
        public Builder Stroke(params int[] indices) { strokes.Add(S(indices.Select(i=>(IElement)Visit(frame,sections[i])).ToArray())); return this; }
        public void Path(params (int X,int Y)[] points)
        { for(var i=0;i<points.Length;i++) P($"p{i}",points[i].X,points[i].Y); for(var i=0;i<points.Length-1;i++) L($"p{i}",$"p{i+1}"); Stroke(Enumerable.Range(0,sections.Count).ToArray()); }
        public Fixture Build(string glyph,string id,string title,string text,string note) =>
            Read(new Relation(new Relation(frame.Surface,S(sections.Cast<IElement>().ToArray())),S(strokes.Cast<IElement>().ToArray())),glyph,id,title,text,note);
    }
}
