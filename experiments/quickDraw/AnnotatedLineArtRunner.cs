using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;

namespace Gojo.Experiments.QuickDraw;

/// <summary>Adapter/export boundary. Only display names come from the external
/// envelope. Every geometry, membership, dependency and diagnostic is read
/// from the independently restored native graph.</summary>
public static class AnnotatedLineArtRunner
{
    public static int Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--annotate" || args[1] != "--input" || args[3] != "--output")
        { Console.Error.WriteLine("Usage: --annotate --input <scenes.ndjson> --output <directory>"); return 2; }
        var destination = Path.GetFullPath(args[4]); Directory.CreateDirectory(destination);
        using var reader = File.OpenText(args[2]);
        using var output = new StreamWriter(Path.Combine(destination, "inspector.ndjson"), false, new UTF8Encoding(false));
        var summaries = new List<object>(); var ids = new HashSet<string>(StringComparer.Ordinal); string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var document = JsonDocument.Parse(line); var envelope = document.RootElement;
            var id = envelope.GetProperty("id").GetString() ?? throw new InvalidDataException("A scene envelope needs an id.");
            if (!ids.Add(id)) throw new InvalidDataException("Repeated scene envelope id.");
            var source = AnnotatedLineArt.Import(envelope);
            var archive = GojoStructureCompactJson.FromElement("authored-line-art", source);
            var json = GojoStructureCompactJson.Serialize(archive);
            var filename = $"{summaries.Count + 1:D3}.annotated.gojo.json";
            File.WriteAllText(Path.Combine(destination, filename), json, new UTF8Encoding(false));
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored) ||
                restored.Root is not Relation root || !AnnotatedLineArt.TryRead(root, out var scene))
                throw new InvalidDataException($"Native annotation restore failed for envelope {id}.");
            var row = new { id, title = Text(envelope, "title", id), notes = Text(envelope, "notes", ""), viewBox = scene.ViewBox,
                coordinateQuantum = scene.Frame.Quantum, nativeRestoreVerified = true, archive = filename,
                analysis = Inspect(scene, envelope) };
            output.WriteLine(JsonSerializer.Serialize(row));
            summaries.Add(new { id, archive = filename, sites = scene.Sites.Length, edges = scene.Edges.Length, strokes = scene.Strokes.Length,
                regions = scene.Regions.Length, closedRegions = scene.Regions.Count(region => region.Closed),
                correspondences = scene.Correspondences.Length, verifiedCorrespondences = scene.Correspondences.Count(map => map.Verified),
                nativeRestoreVerified = true, archiveNodes = archive.Counts.Nodes });
        }
        File.WriteAllText(Path.Combine(destination, "summary.json"), JsonSerializer.Serialize(summaries,
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        Console.WriteLine($"Restored and inspected {summaries.Count} native annotated line-art scenes into {destination}"); return 0;
    }

    public static object Inspect(AnnotatedLineArt.Read scene, JsonElement displayEnvelope)
    {
        string S(Placeholder site) => "s" + scene.Sites.Single(item => ReferenceEquals(item.Event, site)).Index;
        string E(AnnotatedLineArt.Edge edge) => "e" + edge.Index;
        string[] Objects(AnnotatedLineArt.Edge edge) => scene.Objects.Where(item => item.Edges.Contains(edge)).Select(item => "o" + item.Index).ToArray();
        string[] SiteObjects(AnnotatedLineArt.Site site) => scene.Edges.Where(edge => ReferenceEquals(edge.Curve.From, site.Event) || ReferenceEquals(edge.Curve.To, site.Event))
            .SelectMany(Objects).Distinct().ToArray();
        object[] DisplayControls(AnnotatedLineArt.Edge edge) => edge.Curve.Controls.Select(point => (object)new[] { point.X, point.Y }).ToArray();
        string Chord(AnnotatedLineArt.Edge edge)
        {
            var a = edge.Curve.Controls[0]; var b = edge.Curve.Controls[^1];
            return a == b ? "zero" : a.Y == b.Y ? "horizontal" : a.X == b.X ? "vertical" : "oblique";
        }
        bool? SharedObjects(AnnotatedLineArt.Site a, AnnotatedLineArt.Site b)
        {
            var first = SiteObjects(a); var second = SiteObjects(b);
            return first.Length == 0 || second.Length == 0 ? null : first.Intersect(second).Any();
        }
        return new {
            sites = scene.Sites.Select(site => {
                var incidence = scene.Edges.Where(edge => ReferenceEquals(edge.Curve.From, site.Event) || ReferenceEquals(edge.Curve.To, site.Event)).ToArray();
                var degree = scene.Edges.Sum(edge => (ReferenceEquals(edge.Curve.From, site.Event) ? 1 : 0) + (ReferenceEquals(edge.Curve.To, site.Event) ? 1 : 0));
                return new { id = "s" + site.Index, sourceIndex = site.Index, x = site.Position.X, y = site.Position.Y,
                    incidentEdges = incidence.Select(E).ToArray(), degree, kind = AnnotatedLineArt.JointKind(scene, site), objectIds = SiteObjects(site), status = "calculated exact tangent incidence" };
            }).ToArray(),
            edges = scene.Edges.Select(edge => new { id = E(edge), sourceIndex = edge.Index, from = S(edge.Curve.From), to = S(edge.Curve.To), controls = DisplayControls(edge),
                kind = AnnotatedLineArt.TraceKind(edge), chordOrientation = Chord(edge), objectIds = Objects(edge),
                groupIds = scene.Groups.Where(group => group.Membership.Edges.Contains(edge)).Select(group => "g" + group.Membership.Index).ToArray(),
                regionIds = scene.Regions.Where(region => region.Boundary.Any(step => ReferenceEquals(step.Edge, edge))).Select(region => "r" + region.Index).ToArray(),
                status = "native source geometry; exact trace read" }).ToArray(),
            strokes = scene.Strokes.Select(stroke => new { id = "p" + stroke.Index, edges = stroke.Edges.Select(E).ToArray(),
                continuous = stroke.Edges.Skip(1).Where((edge, i) => !ReferenceEquals(stroke.Edges[i].Curve.To, edge.Curve.From)).Any() == false,
                status = "authored traversal; continuity calculated" }).ToArray(),
            objects = scene.Objects.Select(item => new { id = "o" + item.Index, edges = item.Edges.Select(E).ToArray(),
                label = DisplayLabel(displayEnvelope, "objects", item.Index), status = "authored membership" }).ToArray(),
            regions = scene.Regions.Select(region => new { id = "r" + region.Index,
                boundary = region.Boundary.Select(step => new { edge = E(step.Edge), forward = step.Forward }).ToArray(), closed = region.Closed,
                role = region.Figure is 1 ? "figure" : region.Figure is 0 ? "void" : "unselected", parent = region.Parent is null ? null : "r" + region.Parent.Index,
                status = "authored boundary and role; identity closure calculated; simple interior and fill unproved" }).ToArray(),
            groups = scene.Groups.Select(group => new { id = "g" + group.Membership.Index, edges = group.Membership.Edges.Select(E).ToArray(),
                role = group.Detail ? "detail" : "template", label = DisplayLabel(displayEnvelope, "groups", group.Membership.Index), status = "authored focus membership" }).ToArray(),
            correspondences = scene.Correspondences.Select(map => new { id = "m" + map.Index, kind = map.Kind, source = map.Matches.Select(match => E(match.Source)).ToArray(),
                target = map.Matches.Select(match => E(match.Target)).ToArray(), reversed = map.Matches.Select(match => match.Reversed).ToArray(), verified = map.Verified,
                geometryMatches = map.GeometryMatches, identityMatches = map.IdentityMatches,
                axis = map.Kind == "mirror" ? map.Descriptor.Select(point => new[] { point.X, point.Y }).ToArray() : null,
                translation = map.Kind == "translation" ? new[] { map.Descriptor[0].X, map.Descriptor[0].Y } : null,
                status = "authored correspondence; exact control and site checks calculated" }).ToArray(),
            crossings = scene.Crossings.Select(crossing => {
                var a = AnnotatedLineArt.Contains(crossing.First, crossing.At); var b = AnnotatedLineArt.Contains(crossing.Second, crossing.At);
                bool? contact = a == false || b == false ? false : a is null || b is null ? null : true;
                return new { id = "x" + crossing.Index, edges = new[] { E(crossing.First), E(crossing.Second) }, site = new[] { crossing.At.X, crossing.At.Y },
                    geometricContact = contact, joined = AnnotatedLineArt.JoinedAt(crossing), over = crossing.Over is null ? null : E(crossing.Over),
                    status = "authored passage selection; projected incidence calculated; over-order authored or open" };
            }).ToArray(),
            contacts = scene.Contacts.Select(contact => new { id = "c" + contact.Index, sites = new[] { S(contact.First.Event), S(contact.Second.Event) },
                coincident = contact.First.Position == contact.Second.Position, sameObject = SharedObjects(contact.First, contact.Second),
                kind = AnnotatedLineArt.ContactKind(scene, contact), incidentEdges = scene.Edges.Where(edge =>
                    ReferenceEquals(edge.Curve.From, contact.First.Event) || ReferenceEquals(edge.Curve.To, contact.First.Event) ||
                    ReferenceEquals(edge.Curve.From, contact.Second.Event) || ReferenceEquals(edge.Curve.To, contact.Second.Event)).Select(E).ToArray(),
                status = "authored contact between distinct sites; coordinate agreement calculated; no mechanical coupling law" }).ToArray()
        };
    }
    private static string Text(JsonElement source, string name, string fallback) => source.TryGetProperty(name, out var value)
        ? value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : value.GetRawText() : fallback;
    private static string DisplayLabel(JsonElement envelope, string name, int index) => envelope.TryGetProperty(name, out var rows) && index < rows.GetArrayLength()
        ? Text(rows[index], "label", Text(rows[index], "id", $"{name} {index + 1}")) : $"{name} {index + 1}";
}
