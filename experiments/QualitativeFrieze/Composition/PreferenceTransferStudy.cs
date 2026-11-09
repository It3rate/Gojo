using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Paired apparatus: train a pass preference, bind it by a supplied
/// correspondence into a new local context, then let that context act and
/// revise through complete passes. Correspondences are hypotheses supplied
/// by this study; neither source utility nor their existence identifies the
/// target's motion. All target action uses the existing native controller.</summary>
public static class PreferenceTransferStudy
{
    public static readonly string[] Scenarios = ["new-start", "changed-goal", "cross-family",
        "reordered-options", "reversed-response", "corrected-reversal", "capacity", "withheld-first", "lost-section"];
    public static readonly string[] Modes = ["cold", "reuse", "fixed"];

    public sealed record Source(PartialDecisionStudy.Run Run, PassPolicyMemory Height, PassPolicyMemory Width,
        PassPolicyMemory.PreferenceRead HeightPreference, PassPolicyMemory.PreferenceRead WidthPreference);
    public sealed record Run(string Id, string Scenario, string Mode, string SourceFamily, string TargetFamily,
        string SourceId, bool TransferAccepted, int HeightSourceIndex, int WidthSourceIndex,
        int HeightTargetIndex, int WidthTargetIndex, PartialDecisionStudy.Run Target)
    {
        public int? FirstSettledPass => FirstSettlement(Target);
        [JsonIgnore] public Relation? NativeSurface { get; init; }
    }

    public static Source Train(string family = "inset", bool compiled = true)
    {
        PassPolicyMemory? height = null, width = null;
        var run = PartialDecisionStudy.Execute("opposed", "passes", family, compiled,
            configuration: new(ExportPolicy: (memory, isHeight) => { if (isHeight) height = memory; else width = memory; }));
        var hp = height!.ExportPreference(); var wp = width!.ExportPreference();
        if (!hp.Available || !wp.Available)
            throw new InvalidOperationException("Source training did not retain a useful preference for both roles.");
        return new(run, height, width, hp, wp);
    }

    public static Run Execute(Source source, string scenario, string mode,
        bool compiled = true, bool captureNative = false)
    {
        if (!Scenarios.Contains(scenario) || !Modes.Contains(mode))
            throw new ArgumentException("Unknown supplied preference-transfer condition.");
        var targetFamily = scenario == "cross-family"
            ? source.Run.Family == "inset" ? "chevron" : "inset" : source.Run.Family;
        var inset = targetFamily == "inset";
        var reversed = scenario is "reversed-response" or "corrected-reversal";
        var reordered = scenario == "reordered-options";
        var imports = new List<IElement>(); var accepted = new List<bool>();
        var hi = -1; var wi = -1;
        var sourceHeightIndex = Index(source.Height, source.HeightPreference.Option!);
        var sourceWidthIndex = Index(source.Width, source.WidthPreference.Option!);
        // Fixture choices specify a new world, not the controller's answer.
        // Reversal is carried by an affine 0..12 -> 12..0 relation in that world.
        var setup = new PartialDecisionStudy.Configuration(
            InitialHeight: reversed ? 10 : scenario == "new-start" ? 2 : 1,
            InitialWidth: scenario == "new-start" ? 9 : 10,
            Goal: scenario == "changed-goal" ? inset ? 15 : 5 : null,
            ReverseHeightProjection: reversed, ReverseOptionOrder: reordered,
            UnavailablePass: scenario == "withheld-first" ? 1 : null,
            InitializePolicy: (memory, isHeight) =>
            {
                if (mode == "cold") return;
                var from = isHeight ? source.Height : source.Width;
                var preference = isHeight ? source.HeightPreference : source.WidthPreference;
                // Correspondence is an authored role/polarity analogy. In the
                // uncorrected reversal it preserves the old signed motion word,
                // which is precisely the wrong prediction of the new mechanism.
                // The corrected case explicitly incorporates the reversed frame.
                var swap = reordered ^ (isHeight && scenario == "reversed-response");
                var rows = S(from.Options.Select((option, i) => (IElement)new Relation(option,
                    memory.Options[swap ? 1 - i : i])).ToArray());
                var premise = new Relation(from.Context, memory.Context);
                var mapping = memory.PreferenceMapping(preference.Surface, rows, premise);
                var applied = memory.ImportPreference(preference.Surface, mapping);
                imports.Add(applied.Witness); accepted.Add(applied.Accepted);
                var index = applied.TargetOption is null ? -1 : Index(memory, applied.TargetOption);
                if (isHeight) hi = index; else wi = index;
            });
        var target = PartialDecisionStudy.Execute(scenario == "capacity" ? "capacity"
            : scenario == "lost-section" ? "lost-section" : "opposed",
            mode == "fixed" ? "fixed" : "passes", targetFamily, compiled, captureNative, setup);
        var native = captureNative ? S(source.HeightPreference.Surface, source.WidthPreference.Surface,
            S(imports.ToArray()), target.NativeSurface!) : null;
        return new($"{scenario}-{mode}-{source.Run.Family}", scenario, mode, source.Run.Family,
            targetFamily, source.Run.Id, accepted.Count == 2 && accepted.All(x => x),
            sourceHeightIndex, sourceWidthIndex, hi, wi, target) { NativeSurface = native };
    }

    public static int? FirstSettlement(PartialDecisionStudy.Run target) => target.Frames
        .Where(f => f.Step == PartialDecisionStudy.BeatsPerPass && f.Gap == f.Goal &&
            f.EndpointDifference == 0 && f.HeightTension == 0 && f.WidthTension == 0)
        .Select(f => (int?)f.Pass).FirstOrDefault();

    private static int Index(PassPolicyMemory memory, Relation word) =>
        memory.Options.ToList().FindIndex(option => ReferenceEquals(option, word));
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);

    public static int RunExperiment(string[] args)
    {
        var output = args.FirstOrDefault() ?? Path.Combine("experiments", "QualitativeFrieze", "results", "preference-transfer");
        Directory.CreateDirectory(output);
        var sources = PartialDecisionStudy.Families.Select(f => Train(f)).ToArray();
        var runs = new List<Run>();
        foreach (var source in sources) foreach (var scenario in Scenarios) foreach (var mode in Modes)
            runs.Add(Execute(source, scenario, mode));
        var comparisons = new List<object>();
        foreach (var scenario in new[] { "cross-family", "reordered-options", "reversed-response", "capacity" })
        {
            var nativeSource = Train(compiled: false); var compiledSource = Train();
            var native = Execute(nativeSource, scenario, "reuse", compiled: false);
            var compact = Execute(compiledSource, scenario, "reuse", captureNative: true);
            var agree = JsonSerializer.Serialize(native.Target.Frames) == JsonSerializer.Serialize(compact.Target.Frames)
                && JsonSerializer.Serialize(native.Target.PassResults) == JsonSerializer.Serialize(compact.Target.PassResults);
            if (!agree) throw new InvalidOperationException("Native and compiled transfer traces disagree.");
            var bytes = GojoNumericFormat.Encode(compact.NativeSurface!);
            if (GojoNumericFormat.Decode(bytes) is not [Relation]) throw new InvalidOperationException("Transfer archive did not decode.");
            File.WriteAllBytes(Path.Combine(output, scenario + ".gojo"), bytes);
            comparisons.Add(new { scenario, agree, archiveBytes = bytes.Length });
        }
        var json = JsonSerializer.Serialize(new {
            scope = "Two supplied source constructions trained over six passes; 54 paired target conditions, each with six full passes. Only preference is transferred through a supplied correspondence.",
            sources = sources.Select(s => s.Run), runs, comparisons }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(Path.Combine(output, "study-data.json"), json);
        var view = Path.Combine("experiments", "QualitativeFrieze", "PreferenceTransferView.html");
        if (File.Exists(view)) File.WriteAllText(Path.Combine(output, "index.html"), File.ReadAllText(view).Replace("__DATA__", json));
        Console.WriteLine($"Recorded {runs.Count} targets; final joint goals {runs.Count(r => r.Target.JointSettled)}; {Path.GetFullPath(output)}");
        return 0;
    }
}
