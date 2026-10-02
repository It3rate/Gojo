using Gojo.Core;
using Gojo.Encoding;
using Gojo.Encoding.Structures;
using Gojo.Equations;
using N = PredictorScratchpad.NativeCircuit;

namespace PredictorScratchpad;

// Post-trial mechanism inspection. These probes do not change the frozen
// predictor palette, parameters, input generation, or scoring window.
internal static class ResolutionProbes
{
    internal static string[] Run(string outputDirectory, Action<bool, string> check)
    {
        var evidence = new List<IElement>();
        var lines = new List<string>();
        foreach (var numerator in new long[] { 11, -11 })
        {
            var equation = N.Divide(N.Constant(numerator), N.Constant(4));
            var read = N.Read(equation);
            var residues = NonzeroResidues(read);
            check(PredictorNetwork.Scalar(read).Ticks == (numerator > 0 ? 2 : -2), "Signed quotient changed.");
            check(residues.Length == 1 && residues[0].Ticks == (numerator > 0 ? 3 : -3), "Signed residue was lost.");
            lines.Add($"{numerator}/4 reads {PredictorNetwork.Scalar(read).Ticks}, with source-basis residue {residues[0].Ticks}.");
            evidence.Add(N.Group(equation, read));
        }
        foreach (var (priorWeight, increment, divisor, label) in new (long, long, long, string)[]
        {
            (7, 1, 8, "loss with repeated squared error 1"),
            (7, 7, 8, "loss with repeated squared error 7"),
            (3, 1, 4, "slow change with repeated change +1"),
            (3, -1, 4, "slow change with repeated change -1")
        })
        {
            var state = N.State();
            var equation = N.Reframe(N.Divide(N.Sum((state, priorWeight), (N.Constant(increment), 1)),
                N.Constant(divisor)), state.Carrier);
            Relation read = null!;
            for (var arrival = 0; arrival < 16; arrival++)
            {
                read = N.Read(equation);
                check(N.Assign(state, PredictorNetwork.Scalar(read)), "Probe commit lost its basis.");
            }
            var residues = NonzeroResidues(read);
            check(state.Ticks == 0 && residues.Length == 1 && residues[0].Ticks == increment,
                "The recorded update deadband changed; inspect its resolution.");
            lines.Add($"{label}: state {state.Ticks} after 16 updates from zero; last read retains residue {residues[0].Ticks}. It is locally discarded at the temporal commit, not accumulated as transferred tension.");
            evidence.Add(N.Group(state, equation, read));
        }

        // Changing the source later must not rewrite a successful quotient or
        // its observed residue. Failed reads intentionally retain a live request.
        var source = N.State(11);
        var denominator = N.State(4);
        var dynamicRatio = N.Divide(source, denominator);
        var frozen = N.Read(dynamicRatio);
        N.Assign(source, new CarrierLocation(source.Carrier, 20));
        N.Assign(denominator, new CarrierLocation(denominator.Carrier, 3));
        var later = N.Read(dynamicRatio);
        check(PredictorNetwork.Scalar(frozen).Ticks == 2 && NonzeroResidues(frozen).Single().Ticks == 3 &&
            PredictorNetwork.Scalar(later).Ticks == 6 && NonzeroResidues(later).Single().Ticks == 2,
            "A later read rewrote successful observation coordinates.");
        lines.Add("Successful quotient and residue coordinates stay frozen after another input/read. Failed reads retain a live pending surface plus frozen upstream observations; they are not wholly frozen historical snapshots.");
        evidence.Add(N.Group(dynamicRatio, frozen, later));
        File.WriteAllText(Path.Combine(outputDirectory, "resolution-probes.gojo.json"),
            GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("predictor/resolution-probes",
                N.Group(evidence.ToArray()))));
        return lines.ToArray();
    }

    // Inspect the native residue in each successful local read, not upstream
    // predictions or group-index coordinates. Different carrier bases are never
    // summed into a fabricated total noise score.
    internal static CarrierLocation[] NonzeroResidues(Relation read)
    {
        var found = new List<CarrierLocation>();
        var visited = new HashSet<ulong>();
        void NativeResidue(IElement part)
        {
            if (part is CarrierLocation location && location.Ticks != 0) found.Add(location);
            else if (part is Relation relation)
            {
                NativeResidue(relation.Left);
                NativeResidue(relation.Right);
            }
        }
        void Visit(Relation prediction)
        {
            if (!visited.Add(prediction.Id) || !Equation.TryReadPrediction(prediction, out _, out var residue)) return;
            if (residue is not Relation group || N.Parts(group) is not { Length: 2 } parts || parts[1] is not Relation upstream)
                return; // a leaf read has only its zero point residue
            NativeResidue(parts[0]);
            foreach (var parent in N.Parts(upstream).OfType<Relation>()) Visit(parent);
        }
        Visit(read);
        return found.ToArray();
    }
}
