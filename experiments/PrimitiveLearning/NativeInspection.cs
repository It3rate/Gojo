using System.Security.Cryptography;
using System.Text;
using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveLearning;

public static class NativeFallback
{
    // The fallback is an explicit response hypothesis. The earlier unreadable
    // binding remains in the selection witness, rather than becoming sensor zero.
    public static Relation Choose(Relation publication, Carrier axis, long fallback)
    {
        var role = new Carrier();
        var condition = SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(axis, -16, 16));
        ReadWitness.TryRead(publication, out _, out _, out var advertised, out _, out _);
        var rules = SupportDecisionSurface.Create(new Carrier(),
            SupportDecisionSurface.Rule(new Carrier(), advertised, condition),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(axis, fallback)));
        var selection = SupportDecisionSurface.Select(new Carrier(), rules,
            SupportDecisionSurface.Binding(new Carrier(), role, publication));
        SupportDecisionSurface.TryReadSelection(selection, out _, out var response, out _, out _);
        return ReadWitness.Create(new Carrier(), publication, rules, response, selection, selection);
    }
}

public sealed record GraphCounts(int Carriers, int Relations, int Placeholders, int Coordinates);

public static class NativeInspection
{
    public static GraphCounts Count(IElement root)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var units = new HashSet<UnitShape>(ReferenceEqualityComparer.Instance);
        var carriers = 0; var relations = 0; var placeholders = 0; var locations = 0;
        void Visit(IElement element)
        {
            if (element is CarrierLocation point) { locations++; Visit(point.Carrier); return; }
            if (!seen.Add(element)) return;
            switch (element)
            {
                case Carrier carrier:
                    carriers++; foreach (var attachment in carrier.AttachedRelations) Visit(attachment);
                    if (units.Add(carrier.UnitShape)) { Visit(carrier.UnitShape.Surface); foreach (var descriptor in carrier.UnitShape.DescriptorSurfaces) Visit(descriptor); }
                    break;
                case Relation relation:
                    relations++; Visit(relation.Left); Visit(relation.Right); break;
                case Placeholder: placeholders++; break;
            }
        }
        Visit(root); return new(carriers, relations, placeholders, locations);
    }

    // Local identity numbering includes shared TickRefs and attachments. This
    // diagnostic signature is not a substitute for the native transport format.
    public static string Signature(IElement root)
    {
        var ids = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        int Id(object item) { if (!ids.TryGetValue(item, out var id)) ids.Add(item, id = ids.Count); return id; }
        var text = new StringBuilder();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var units = new HashSet<UnitShape>(ReferenceEqualityComparer.Instance);
        void Write(IElement element)
        {
            if (element is CarrierLocation point)
            {
                text.Append("L(").Append(Id(point.Carrier)).Append(',').Append(Id(point.Tick)).Append(',').Append(point.Ticks).Append(')');
                Write(point.Carrier); return;
            }
            var id = Id(element);
            if (!visited.Add(element)) { text.Append('@').Append(id); return; }
            switch (element)
            {
                case Carrier carrier:
                    text.Append("C(").Append(id).Append(',').Append(carrier.Value).Append(',').Append(carrier.Tension).Append(',').Append(carrier.IsInverse).Append('[');
                    foreach (var attachment in carrier.AttachedRelations) Write(attachment);
                    text.Append("U").Append(Id(carrier.UnitShape));
                    if (units.Add(carrier.UnitShape)) { Write(carrier.UnitShape.Surface); foreach (var descriptor in carrier.UnitShape.DescriptorSurfaces) Write(descriptor); }
                    text.Append("])"); break;
                case Relation relation:
                    text.Append("R(").Append(id).Append(','); Write(relation.Left); text.Append(','); Write(relation.Right); text.Append(')'); break;
                case Placeholder:
                    text.Append('?').Append(id); break;
                default: throw new InvalidDataException("Unsupported native inspection element.");
            }
        }
        Write(root);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))).ToLowerInvariant();
    }

    public static Relation RestoreVerified(Relation root, string path)
    {
        var bytes = GojoNumericFormat.Encode(root);
        File.WriteAllBytes(path, bytes);
        var decoded = GojoNumericFormat.Decode(bytes);
        if (decoded.Length != 1 || decoded[0] is not Relation restored || Signature(root) != Signature(restored))
            throw new InvalidDataException("Native graph identity/state signature changed in transport.");
        return restored;
    }
}
