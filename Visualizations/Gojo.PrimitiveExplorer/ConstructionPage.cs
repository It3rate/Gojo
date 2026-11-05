using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private static readonly string[] ConstructionColors = ["#167C6C", "#9B5A27", "#6551A2", "#2E78A6", "#A6476F", "#74802C"];

    private ExplorerPage ConstructionPage()
    {
        var catalogue = ConstructionAtlas.All;
        var patterns = catalogue.Select(example => example.Pattern).Distinct().ToArray();
        var choices = catalogue.Where(example => example.Pattern == patterns[Mode]).ToArray();
        var example = choices[Variant];
        var surface = example.Surface;
        var table = surface.Table;
        var template = table.Template;
        var uses = surface.Uses;
        var entry = surface.Entry;
        var reverse = A == 1;
        var p = new ExplorerPage("Construction clues",
            "The same outline can come from different connected uses. Hide a kind of evidence and inspect the constructions that still fit.", "ConstructionHints.md")
        {
            DrawingGuide = (example.Pattern + " · " + example.Method, example.Description)
        };
        Choose(p, "Pattern", patterns, Mode, value => { Mode = value; Variant = 0; }, true);
        Choose(p, "Construction", choices.Select(item => item.Method).ToArray(), Variant, value => Variant = value, true);
        Choose(p, "View", ["Forward", "Reverse"], A, value => A = value);
        Choose(p, "Evidence", ["All clues", "Hide directions", "Hide grouping", "Hide geometry", "Connections only"], Control, value => Control = value);

        ConstructionBits(p, surface);
        ConstructionBasisBits(p, surface.Basis);
        ConstructionDrawing(p, example, reverse, Control);

        p.Events.Add("Grey lines are the stored links. Colored arrows are selected uses of those links; two arrows on a seam do not create two material edges.");
        p.Events.Add("The bit panel retains the complete source. Evidence controls restrict what the comparison reader receives; they do not delete or rewrite the source.");
        for (var group = 0; group < uses.GroupCount; group++)
        {
            var occurrences = uses.ReadGroup(table, entry, group, reverse);
            var path = string.Join(" · ", occurrences.Select(use => $"{use.Span.Start.Local}→{use.Span.End.Local}"));
            var state = uses.IsClosed(table, entry, group, reverse) ? "closed" :
                uses.IsContinuous(table, entry, group, reverse) ? "continuous, open" : "not a continuous walk";
            p.Events.Add($"Source group {group}: {path}; {state}.");
        }
        ConstructionRead(p, example, reverse, Control);
        p.Boundary = "Authored construction catalogue and local evidence reads. Coordinates only draw the example. Compatibility preserves possibilities; it does not identify a unique concept, learn a construction, or prove hidden geometry.";
        return p;
    }

    private static void ConstructionBits(ExplorerPage p, ConstructionSurface surface)
    {
        var table = surface.Table;
        var template = table.Template;
        var uses = surface.Uses;
        p.Rows("Source · field positions", "[O group, D group]. These constant/owned choices are shared by the entries, not repeated in their payloads.", 2,
            Enumerable.Range(0, template.FieldCount).Select(i => { var field = template.Field(i); return new[] { field.OrientationGroup, field.DirectionGroup }; }).ToArray(), "fields",
            roleKeys: Enumerable.Range(0, template.FieldCount).Select(i => new[] { "field" + i, "field" + i }).ToArray(), actual: template.FieldBytes());
        p.Rows("Source · links", "[Start site, end site, value field]. Repeated site references make an actual joint. Repeated field references share an amount.", template.ReferenceBits,
            Enumerable.Range(0, template.LinkCount).Select(i => { var link = template.Link(i); return new[] { link.Start, link.End, link.Field }; }).ToArray(), "links",
            roleKeys: Enumerable.Range(0, template.LinkCount).Select(i => { var link = template.Link(i); return new[] { "site" + link.Start, "site" + link.End, "field" + link.Field }; }).ToArray(),
            actual: template.LinkBytes(), rowKeys: Enumerable.Range(0, template.LinkCount).Select(i => "link" + i).ToArray());
        var linkBlock = p.Blocks[^1];
        p.Blocks[^1] = linkBlock with
        {
            Marks = linkBlock.Marks.Select((mark, i) => mark with
            {
                Keys = mark.Keys.Take(mark.Keys.Length - 1)
                    .Concat(Enumerable.Range(0, uses.UseCount).Where(position => (uses.SignedUse(position) >> 1) == i / 3).Select(position => "use" + position))
                    .Append(mark.Keys[^1]).ToArray()
            }).ToArray()
        };
        var useMarks = new List<BitMark>();
        for (var group = 0; group < uses.GroupCount; group++)
        {
            var range = uses.Range(group);
            for (var at = range[0]; at < range[1]; at++)
            {
                var signed = uses.SignedUse(at);
                useMarks.Add(new(at * uses.UseBits, uses.UseBits, ["group" + group, "link" + (signed >> 1), "field" + template.Link(signed >> 1).Field, "use" + at],
                    $"Occurrence {at}: link {signed >> 1}, {((signed & 1) == 0 ? "forward" : "reverse")}, group {group}. Low bit selects the view."));
            }
        }
        p.Bits("Source · directed uses", "Each word is (link reference << 1) | reverse. Reusing a link retains the same physical seam; changing this view does not change O/D/V.",
            uses.UseBytes(), uses.UseCount * uses.UseBits, "uses", useMarks.ToArray());
        p.Rows("Source · group boundaries", "[First use, exclusive end]. These ranges delimit selected groups; they are collection boundaries, not geometric distances.", uses.RangeBits,
            Enumerable.Range(0, uses.GroupCount).Select(uses.Range).ToArray(), "groups", actual: uses.RangeBytes(),
            rowKeys: Enumerable.Range(0, uses.GroupCount).Select(i => "group" + i).ToArray());
        foreach (var width in table.Widths)
        {
            var marks = new List<BitMark>();
            var offset = 0;
            for (var row = 0; row < table.Count(width); row++)
            for (var field = 0; field < template.FieldCount; field++)
            {
                var bits = width + template.Field(field).OwnedBits;
                var keys = new List<string>();
                keys.AddRange(Enumerable.Range(0, template.LinkCount).Where(i => template.Link(i).Field == field).Select(i => "link" + i));
                keys.AddRange(Enumerable.Range(0, uses.UseCount).Where(i => template.Link(uses.SignedUse(i) >> 1).Field == field).Select(i => "use" + i));
                keys.Add("field" + field);
                marks.Add(new(offset, bits, keys.ToArray(), $"Instance {row}, value role {field}; {width} value bits then owned direction/orientation."));
                offset += bits;
            }
            p.Bits($"Source · {width}-bit value group", "Participation is stored once per value role. Repeated references do not duplicate this storage.",
                table.PackedBytes(width), checked((int)table.PayloadBitLength(width)), "values", marks.ToArray());
        }
    }

    private static void ConstructionDrawing(ExplorerPage p, ConstructionExample example, bool reverse, int evidence)
    {
        var surface = example.Surface;
        var template = surface.Table.Template;
        var points = example.Points;
        if (points.Length == 0)
        {
            p.Label("No geometric placement supplied.", 350, 220);
            return;
        }
        var minX = points.Min(point => point.X); var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y); var maxY = points.Max(point => point.Y);
        var scale = Math.Min(470 / Math.Max(.1, maxX - minX), 280 / Math.Max(.1, maxY - minY));
        (double X, double Y) At(int site) => (350 + (points[site].X - (minX + maxX) / 2) * scale,
            220 - (points[site].Y - (minY + maxY) / 2) * scale);
        p.Label("Illustrative source placement · distances and angles are not read from this picture", 350, 30);
        for (var link = 0; link < template.LinkCount; link++)
        {
            var row = template.Link(link);
            var from = At(row.Start); var to = At(row.End);
            var key = "link" + link;
            var detail = $"Stored link {link}: site {row.Start} to site {row.End}, value role {row.Field}. This is one source edge even when several groups use it.";
            p.Drawing.Add(new("stroke", key, "", detail, from.X, from.Y, to.X, to.Y)
                { Color = "#929D98", LineWidth = 3, RelatedKeys = ["field" + row.Field, "site" + row.Start, "site" + row.End] });
            p.Details[key] = detail;
        }
        var useLocations = Enumerable.Range(0, surface.Uses.UseCount).GroupBy(position => surface.Uses.SignedUse(position) >> 1)
            .ToDictionary(group => group.Key, group => group.ToArray());
        if (evidence != 4)
        for (var group = 0; group < surface.Uses.GroupCount; group++)
        foreach (var occurrence in surface.Uses.ReadGroup(surface.Table, surface.Entry, group, reverse))
        {
            var original = template.Link(occurrence.Link);
            var originalFrom = At(original.Start); var originalTo = At(original.End);
            var from = At(occurrence.Span.Start.Local); var to = At(occurrence.Span.End.Local);
            var dx = to.X - from.X; var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1) continue;
            var repeated = useLocations[occurrence.Link];
            var rank = Array.IndexOf(repeated, occurrence.Position);
            var offset = repeated.Length == 1 ? 0 : (rank - (repeated.Length - 1) / 2.0) * 14;
            var ox = -(originalTo.Y - originalFrom.Y) / length * offset;
            var oy = (originalTo.X - originalFrom.X) / length * offset;
            var key = "use" + occurrence.Position;
            var detail = $"Use {occurrence.Position} in group {group}: link {occurrence.Link}, selected {occurrence.Span.Start.Local} → {occurrence.Span.End.Local}. This arrow is an observation route, not an extra physical edge.";
            p.Drawing.Add(new(evidence == 1 ? "stroke" : "edge", key, "", detail,
                from.X + dx * .17 + ox, from.Y + dy * .17 + oy, to.X - dx * .17 + ox, to.Y - dy * .17 + oy,
                Active: (surface.Table.ReadCoarse(surface.Entry, occurrence.Span.Value.Field) & 1) != 0)
                { Color = evidence == 2 ? "#334D48" : ConstructionColors[group % ConstructionColors.Length], LineWidth = 2,
                    RelatedKeys = ["link" + occurrence.Link, "field" + occurrence.Span.Value.Field, "group" + group] });
            p.Details[key] = detail;
        }
        for (var site = 0; site < points.Length; site++)
        {
            var point = At(site);
            var samePosition = Enumerable.Range(0, points.Length).Where(other => points[other] == points[site]).ToArray();
            var rank = Array.IndexOf(samePosition, site);
            p.Circle("site" + site, $"Site {site}. Equal drawing coordinates do not merge this site with another reference.", point.X, point.Y, 5 + rank * 3);
            p.Label("s" + site, point.X + (rank % 2 == 0 ? 13 : -13), point.Y + (rank % 2 == 0 ? -22 : 12));
        }
        if (evidence != 4 && evidence != 2)
        {
            var groups = surface.Uses.GroupCount;
            for (var group = 0; group < groups; group++)
            {
                var x = 350 + (group - (groups - 1) / 2.0) * Math.Min(100, 620.0 / Math.Max(1, groups));
                var detail = $"Group {group}: this color follows its selected ordered uses. Hover its group boundary and directed-use bits.";
                p.Drawing.Add(new("stroke", "group" + group, "", detail, x - 18, 410, x + 18, 410)
                    { Color = ConstructionColors[group % ConstructionColors.Length], LineWidth = 4 });
                p.Label("group " + group, x, 420);
                p.Details["group" + group] = detail;
            }
        }
        else p.Label(evidence == 2 ? "Grouping withheld from the read" : "Direction and group uses withheld from the read", 350, 410);
        p.Label("Grey = stored links · colored arrows = selected uses, not extra edges", 350, 453);
    }

    private static void ConstructionBasisBits(ExplorerPage p, ConstructionBasis? basis)
    {
        if (basis is null) return;
        p.Rows("Source · geometry site bindings", "[Family site, construction site]. This correspondence attaches the geometry source to the actual local sites.", basis.Bits,
            Enumerable.Range(0, basis.SiteBindings).Select(basis.SiteRow).ToArray(), "basis", actual: basis.SiteBytes(),
            roleKeys: Enumerable.Range(0, basis.SiteBindings).Select(i => new[] { "site" + basis.SiteRow(i)[0], "basis" }).ToArray());
        p.Rows("Source · geometry link bindings", "[Family link, construction link]. Geometry is read only when the connection correspondence holds.", basis.Bits,
            Enumerable.Range(0, basis.LinkBindings).Select(basis.LinkRow).ToArray(), "basis", actual: basis.LinkBytes(),
            roleKeys: Enumerable.Range(0, basis.LinkBindings).Select(i => new[] { "link" + basis.LinkRow(i)[0], "basis" }).ToArray());
        if (basis.GapBindings > 0)
            p.Rows("Source · whole-gap bindings", "[Rail role, family value field]. A repeated value reference supplies the same unmeasured gap to both rail roles.", basis.Bits,
                Enumerable.Range(0, basis.GapBindings).Select(basis.GapRow).ToArray(), "basis", actual: basis.GapBytes(),
                roleKeys: Enumerable.Range(0, basis.GapBindings).Select(i => new[] { "basis", "field" + basis.GapRow(i)[1] }).ToArray());
        if (basis.PathSource is { } path)
        {
            for (var lane = 0; lane < 4; lane++)
                p.Rows($"Source · quarter lane {lane}", "[Start, end, reach]. The four lane positions select +u,+v,−u,−v in an explicitly perpendicular planar frame.",
                    3, [path.Wiring(lane)], "basis", actual: path.PackedLane(lane));
        }
        if (basis.RailSource is { } rails)
        {
            p.Bits("Source · across order", "Ordered landmark references; no numerical gaps are assigned.", rails.Across.SiteBytes(), rails.Across.Count * rails.Across.Bits, "basis");
            p.Bits("Source · rise order", "The second ordered landmark collection.", rails.Rise.SiteBytes(), rails.Rise.Count * rails.Rise.Bits, "basis");
            p.Rows("Source · paired rail sites", "[Across landmark, rise landmark]. Shared landmark references establish alignment.", rails.Bits,
                Enumerable.Range(0, rails.SiteCount).Select(rails.Site).ToArray(), "basis", actual: rails.SiteBytes());
            p.Bits("Source · rail direction bindings", "The two rails bind to adjacent rays of the closed equal four-part contour.", rails.AxisBytes(), rails.AxisBytes().Length == 0 ? 0 : 36, "basis");
            ConstructionContourBits(p, rails.TurnBasis);
        }
        if (basis.RadialSource is { } contour) ConstructionContourBits(p, contour);
    }

    private static void ConstructionContourBits(ExplorerPage p, ContourPattern contour)
    {
        p.Rows("Source · radial arc parts", "[Start, end, part reference]. Shared part references supply equal angular shares in this declared radial construction.", contour.ReferenceBits,
            Enumerable.Range(0, contour.PartCount).Select(contour.ArcRow).ToArray(), "basis", actual: contour.ArcBytes());
        p.Bits("Source · common radial reach", "The centre-to-site rows reuse the same radius source throughout the traversal.",
            contour.RadialBytes(), (contour.PartCount + 1) * 3 * contour.ReferenceBits, "basis");
        p.Bits("Source · radial whole", "The selected whole traversal retains its start, unwrapped end and participation source.",
            contour.WholeBytes(), 3 * contour.ReferenceBits, "basis");
        p.Bits("Source · radial return", "The unwrapped end returns to the actual start site.", contour.ClosureBytes(),
            contour.ClosureBytes().Length == 0 ? 0 : 2 * contour.ReferenceBits, "basis");
        if (contour.Read().JoinedChords)
            p.Bits("Source · joined chords", "Successive contour sites supply chord endpoints. The family correspondence attaches these chords to its own links.",
                contour.ChordBytes(), contour.PartCount * 2 * contour.ReferenceBits, "basis");
    }

    private static void ConstructionRead(ExplorerPage p, ConstructionExample example, bool reverse, int evidence)
    {
        var read = ConstructionEvidence.Read(example.Surface, reverse);
        var published = read.Publish((ConstructionVisibility)evidence);
        var matches = ConstructionEvidence.Match(published, ConstructionAtlas.All, reverse);
        var known = published.Values.Count(value => value.HasValue);
        p.Bits("Published · known clues", "One availability bit per derived clue. A missing clue is open; it is not a known zero. Source structure remains unchanged.",
            published.KnownBytes(), published.Values.Count, "known",
            Enumerable.Range(0, published.Values.Count).Select(i => new BitMark(i, 1, ["clue" + i],
                ConstructionEvidence.Labels[i] + (published.Values[i].HasValue ? ": published" : ": withheld or unproved"))).ToArray());
        p.Bits("Published · clue values", "Calculated observations, not stored source metadata. Read each value together with its availability bit; zero-filled unavailable slots do not assert zero.",
            published.PackedBytes(), published.Values.Count * published.ValueBits, "clues",
            Enumerable.Range(0, published.Values.Count).Select(i => new BitMark(i * published.ValueBits, published.ValueBits, ["clue" + i],
                ConstructionEvidence.Labels[i] + ": " + (published.Values[i]?.ToString() ?? "open"))).ToArray());
        var candidates = new byte[(ConstructionAtlas.All.Count + 7) / 8];
        foreach (var match in matches) candidates[match / 8] |= (byte)(1 << (match % 8));
        p.Bits("Published · compatible constructions", "One bit per catalogue construction. A set bit means no known clue conflicts; it does not establish identity or probability. Hover a bit for its candidate and evidence.",
            candidates, ConstructionAtlas.All.Count, "candidates", Enumerable.Range(0, ConstructionAtlas.All.Count).Select(i =>
            {
                var candidate = ConstructionAtlas.All[i];
                var conflicts = published.ConflictsWith(ConstructionEvidence.Read(candidate.Surface, reverse));
                var reason = conflicts.Length == 0 ? "No known clue conflicts." : "Conflicts: " + string.Join(", ", conflicts.Select(slot => ConstructionEvidence.Labels[slot]));
                return new BitMark(i, 1, ["candidate" + i], candidate.Pattern + " · " + candidate.Method + ". " + reason);
            }).ToArray());
        string Value(int slot) => published.Values[slot]?.ToString() ?? "?";
        string Proof(int slot) => published.Values[slot] is 1 ? "proved" : published.Values[slot] is 0 ? "degenerate" : "open";
        p.Result = $"{known}/{published.Values.Count} clues published; {matches.Length} compatible constructions. Rectangle {Proof(17)}; square {Proof(18)}.";
        var patterns = matches.GroupBy(i => ConstructionAtlas.All[i].Pattern).Select(group => group.Key + " (" + group.Count() + ")");
        p.Events.Add($"Sites {Value(0)} · links {Value(1)} · graph cycles {Value(3)} · use groups {Value(5)} · closed groups {Value(7)}.");
        p.Events.Add("Remaining catalogue patterns: " + string.Join(", ", patterns) + ".");
        p.Events.Add("? means open. Reverse changes directed uses; evidence filters only change the published read. Pixels never enter this comparison.");
    }
}
