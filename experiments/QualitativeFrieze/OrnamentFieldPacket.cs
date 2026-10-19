using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A program sample and its explicit field law transported as one
/// identity graph. Source citations and screen geometry belong to a separate
/// catalogue. Loading does not reconstruct motifs or seams from those labels.</summary>
public sealed record OrnamentFieldPacket(OrnamentProgram Program, Relation Bindings,
    Relation Window, Relation SeamContract, Relation Source, Relation Correspondence)
{
    // packet = (program -> (bindings -> window)) -> seam contract
    public static string Save(OrnamentProgram program, Relation bindings, Relation window, Relation seamContract)
    {
        var root = new Relation(new Relation(program.Surface, new Relation(bindings, window)), seamContract);
        return JsonSerializer.Serialize(new Document(1,
            GojoStructureCompactJson.FromElement("ornament-field-packet-v1", root)), Options);
    }

    /// <summary>Imports a fresh reference graph and verifies the complete
    /// standard frame before rebinding the whole packet through one memo.
    /// Syntactic import is separate from program execution and seam acceptance.</summary>
    public static bool TryLoad(string json, out OrnamentFieldPacket packet, out Relation unresolved)
    {
        packet = null!; unresolved = StructuralConstraint.Open();
        Document? document;
        try { document = JsonSerializer.Deserialize<Document>(json, Options); }
        catch (JsonException) { return false; }
        if (document is not { Version: 1, Native: { Version: 3, Carriers: not null, Nodes: not null } native } ||
            native.Carriers.Count != 0 || native.Nodes.Any(node => node is null || node.Kind is not ("relation" or "placeholder"))) return false;
        RehydratedCompactStructure restored;
        try { if (!GojoStructureCompactJson.TryToElement(native, out restored)) return false; }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or NullReferenceException or IndexOutOfRangeException or OverflowException) { return false; }
        if (restored.Root is not Relation source || source.Left is not Relation { Left: Relation { Left: Relation frameSurface } } ||
            !QualitativeMotionFrameRead.TryReadStandard(frameSurface, out var frameRead)) return false;
        unresolved = StructuralConstraint.Open(source);
        if (!frameRead.TryRebind(source, out var rebound, out var correspondence) ||
            rebound is not Relation { Left: Relation sample, Right: Relation contract } ||
            sample.Left is not Relation { Left: var actualFrame, Right: Relation { Left: Relation motifs,
                Right: Relation { Left: Relation transforms, Right: Relation { Left: Relation parameters, Right: Relation layout } } } } program ||
            !ReferenceEquals(actualFrame, frameRead.Target.Surface) ||
            sample.Right is not Relation { Left: Relation bindings, Right: Relation window }) return false;
        var executable = new OrnamentProgram(frameRead.Target, motifs, transforms, parameters, layout, program);
        packet = new(executable, bindings, window, contract, source, new(frameRead.Witness, correspondence));
        unresolved = N.S(); return true;
    }

    private sealed record Document(int? Version, GojoCompactStructureDocument? Native);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
}
