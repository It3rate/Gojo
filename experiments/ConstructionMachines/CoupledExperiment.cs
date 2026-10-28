using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

public static class CoupledExperiment
{
    private sealed record Setup(string Id, string Title, string Description, string[] Notes,
        long Rise = 40, long Step = 4, long Reviews = 3, bool Suitable = true, int Passes = 48);
    private sealed record Metrics(double goal, double attachments, double differential, double load,
        double introduced, double released, double sourceFeature, double jointFeature, double threshold, double attempts, double budget);
    private sealed record Review(string name, double before, double after, double benefit, double minimum, double endpointBefore,
        double endpointAfter, int pathsBefore, int pathsAfter, bool eligible);
    private sealed record Frame(int index, int pass, string phase, string model, double[][] outline,
        double[][] joint, Metrics metrics, Review[] reviews, string[] events);
    private sealed record Scene(string id, string title, string description, string[] notes, double[][] goal,
        double[][] initialOutline, double[][] initialJoint, List<Frame> frames);

    public static void Write(string outputFolder)
    {
        var folder = Path.GetFullPath(outputFolder); Directory.CreateDirectory(folder);
        Setup[] setups =
        [
            new("adaptive", "The moving frame creates a joint conflict", "Position and scale requests move the outline. Its eave/ridge relationship then challenges the initially compatible roof joint.",
                ["The joint starts with zero differential mismatch, while the outer shape already has placement demands.", "A replacement requires observed load and a supported local preview. Its acceptance does not clear the mismatch."]),
            new("rigid", "Keep the rigid joint and inspect the compromise", "The same connected targets run with no proposal-review budget. The shape and joint can still move and push back on one another.",
                ["The outline can yield to the rigid joint, so its own reference error remains important.", "A reduced local mismatch is recorded as released load, not as proof that the complete drawing is correct."], Reviews: 0),
            new("flat", "Position and scale change without a joint change", "The reference remains flat while requesting the same translation, width and height changes.",
                ["Movement and attachment error can be present while the joint's differential relationship remains compatible.", "The review controller should stay inactive; motion alone does not justify a new joint."], Rise: 0),
            new("slow", "Slower movement changes when review becomes useful", "The outline admits smaller changes through the same links and uses the same review threshold.",
                ["Compare the first review with the first experiment: there is no scheduled replacement pass.", "The review still uses the locally available publications and the same candidate tests."], Step: 2, Passes: 64),
            new("unsuitable", "Pressure with no acceptable alternative", "The library contains an opposed arm adjustment and a proposal that removes the supporting stem.",
                ["A proposal can lower positional error and still be refused for breaking a required path.", "Exhausting the review budget leaves the conflict visible; it does not force a replacement."], Suitable: false)
        ];
        var scenes = new List<Scene>(); var roots = new List<IElement>();
        foreach (var setup in setups)
        {
            var model = CoupledSettlement.Create(setup.Rise, setup.Step, setup.Reviews, setup.Suitable);
            var initialOutline = Points(model.Outline); var initialJoint = Points(model.Joint.Mechanical);
            var frames = new List<Frame>();
            Capture(0, "Start", model.LastRead, new Placeholder(), [], ["The joint's initial attachments agree. The independent outline reference supplies the first placement/scale demands."]);
            var controller = Gojo.Structures.TimeStepSequence.Span(new Carrier(), 0, setup.Passes);
            Gojo.Structures.TimeStepSequence.TryReadSpan(controller, out var from, out var to);
            int firstChange = -1;
            for (var pass = (int)from.Ticks + 1; pass <= to.Ticks; pass++)
            {
                var step = model.Step(); var reviews = ReadReviews(model, step.Decision);
                Capture(pass, "Local response", step.AfterMotion, step.PreviousSelection, [],
                    ["All coordinate proposals used one captured set of publications. Admitted parameter changes entered through signed carrier tension.",
                     $"The observed differential now reads {Scalar(CoupledSettlement.FeatureMismatch(step.AfterMotion))}. The load ledger records only the change from its previous level."],
                    reviews.Length > 0 ? ((Carrier)F(model.Policy)[3]).Value - 1 : null);
                if (reviews.Length > 0)
                {
                    if (step.Rewritten && firstChange < 0) firstChange = pass;
                    Capture(pass, step.Rewritten ? "Proposal accepted" : "Proposals rejected", step.AfterDecision, model.Selected, reviews,
                        [step.Rewritten ? "The proposed construction was installed at the same public coordinates. Existing attachment ports remain connected."
                            : "No preview satisfied all local claims. The previous construction remains selected.",
                         "Review does not clear load. Only later observed changes can increase or release it.",
                         "Preview saw this joint and the neighbour's captured public coordinates; it did not see the neighbour's private shape reference."]);
                }
            }
            scenes.Add(new Scene(setup.Id, setup.Title, setup.Description, setup.Notes, Points(model.World.Machines[2]), initialOutline, initialJoint, frames));
            roots.Add(G(controller, model.Surface));
            var last = frames[^1].metrics;
            Console.WriteLine($"{setup.Id}: first change {firstChange}, goal {last.goal:0.##}, attachments {last.attachments:0.##}, local load {last.load:0.##}, reviews {last.attempts:0}");

            void Capture(int pass, string phase, Relation read, IElement selected, Review[] reviews, string[] events, double? attemptsBeforeReview = null)
            {
                var f = F(F(read)[2]); var p = F(model.Policy);
                var metrics = new Metrics(Number(CoupledSettlement.NormValue(f[0])), Number(CoupledSettlement.NormValue(f[1])),
                    Number(CoupledSettlement.FeatureMismatch(read)), model.Load.Remaining.Value, model.Load.Introduced.Value, model.Load.Released.Value,
                    Number(CoupledSettlement.NormValue(f[3])), Number(CoupledSettlement.NormValue(f[4])), Number(p[0]), attemptsBeforeReview ?? ((Carrier)p[3]).Value, SpanEnd((Relation)p[2]));
                frames.Add(new Frame(frames.Count, pass, phase, Label(model, selected), Points(model.Outline), Points(model.Joint.Mechanical), metrics, reviews, events));
            }
        }
        var binary = GojoNumericFormat.Encode(G(roots.ToArray()));
        var loaded = F(GojoNumericFormat.Decode(binary)[0]);
        foreach (var root in loaded) CoupledSettlement.Restore((Relation)F(root)[1]).Step();
        File.WriteAllBytes(Path.Combine(folder, "coupled.gojo.bin"), binary);
        var json = JsonSerializer.Serialize(new
        {
            summary = new[]
            {
                "The shape reference drives position/scale and a roof-rise read. Attachment links communicate in both directions, so the frame can also yield to the joint.",
                "Local mismatch is the neighbour's published ridge-above-eaves span minus the joint's span. Review load tracks its magnitude; it is not confidence or conserved physical energy.",
                "Introduced load = remaining load + released load. Repeated reads add nothing, and accepting a proposal clears nothing.",
                "Candidate definitions and response laws are authored. Review timing and selection depend on the observed mismatch, private preview results and path obligations.",
                "Preview holds neighbour publications fixed for a bounded trial. It is a conditional local prediction; the live neighbour can keep moving afterwards.",
                "The native archive retains constructions, links, claims, proposals, accounting and histories. The browser only plays captured observations."
            }, scenarios = scenes
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, "coupled-observations.json"), json);
        CoupledReview.Write(Path.Combine(folder, "review.html"), json);
        Console.WriteLine($"{scenes.Count} coupled worlds, {scenes.Sum(s => s.frames.Count)} observations; all restored and continued; {folder}");
    }

    private static Review[] ReadReviews(CoupledSettlement model, Relation decision)
    {
        if (F(decision)[2] is not Relation result || !result.TryReadOrderedGroup(out var fields) || fields.Length != 2) return [];
        return F(fields[1]).Cast<Relation>().Where(r => F(r)[2] is Relation).Select(r =>
        {
            var f = F(F(r)[2]);
            return new Review(Label(model, F(r)[1]), Number(f[0]), Number(f[1]), Number(f[2]), Number(F(model.Policy)[1]), Number(f[3]), Number(f[4]),
                (int)Number(f[5]), (int)Number(f[6]), CoupledSettlement.Eligible(r));
        }).ToArray();
    }
    private static string Label(CoupledSettlement model, IElement selected)
    {
        if (selected is Placeholder) return "Rigid through-joint";
        var index = Array.FindIndex(F(model.Proposals), p => ReferenceEquals(p, selected));
        return index switch { 0 => "Opposed arm adjustment", 1 => "Released arms without support", 2 => "Released eave arms", _ => "Recorded proposal" };
    }
    private static double[][] Points(NegotiatingMachine machine) => machine.Points.Select(Program.XY).ToArray();
    private static double Number(IElement scalar) => CoupledSettlement.Number(scalar);
    private static string Scalar(IElement scalar) { ProjectionResidue.TryRead(scalar, out _, out var n, out var d); return d == 1 ? n.ToString() : $"{n}/{d}"; }
    private static long SpanEnd(Relation span) { Gojo.Structures.TimeStepSequence.TryReadSpan(span, out _, out var end); return end.Ticks; }
}
