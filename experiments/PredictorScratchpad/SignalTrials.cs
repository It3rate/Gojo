namespace PredictorScratchpad;

/// <summary>
/// Fixed synthetic environments, not learner models. Host arithmetic and random
/// draws construct input only. The learner may receive the next A/B pair only
/// after issuing its prediction; names, seeds, phases, future samples, latent
/// controls and generator timing must stay outside its reachable Gojo structure.
/// </summary>
internal static class SignalTrials
{
    // Protocol fixed before inspecting predictor results: run every condition
    // at every seed, with the same supplied primitive palette and adaptation
    // parameters. Compare against hold, last velocity, equal participation and
    // participation frozen after a common warm-up. Report all seeds, failures
    // and phases, not a chosen best trace. Predict before admitting each input.
    // These are three evaluation repetitions, not a claim that the worlds are
    // free of the inductive biases explicitly supplied by this generator.
    private static readonly int[] Seeds = [103, 907, 2029];
    private const int Count = 216;

    // Both sensors arrive as raw integer ticks, nominal range 0..1000. They are
    // distinct carrier coordinates; equal numbers do not assert shared units.
    // No confidence denominator or semantic light/heat label is manufactured.
    // Any display grain is a report convention, not extra learner evidence.
    internal static IEnumerable<(string Name, int Seed, (long A, long B, string Phase)[] Samples)> Generate()
    {
        foreach (var seed in Seeds)
        {
            yield return ("ramp-plateau-cadence", seed, RampPlateau(seed));
            yield return ("smooth-turning", seed, SmoothTurning(seed));
            var mixed = MixedSources(seed);
            yield return ("mixed-anonymous-input", seed, mixed);
            yield return ("shuffled-marginal-control", seed, ShuffledControl(seed, mixed));
        }
    }

    private static (long A, long B, string Phase)[] RampPlateau(int seed)
    {
        var random = new Random(seed);
        var samples = new (long A, long B, string Phase)[Count];
        var a = 180L + random.Next(-20, 21);
        var b = 700L + random.Next(-20, 21);
        long[] slopesA = [3, 0, -2, 5, 0, -4];
        long[] slopesB = [-2, -2, 0, 1, 3, 0];
        string[] phases = ["rising/falling", "plateau/falling", "falling/plateau",
            "rising/rising", "plateau/rising", "falling/plateau"];
        int[][] cadence = [[1, 1, 2], [1, 2, 1, 3], [2, 1, 1, 1]];
        for (var index = 0; index < Count; index++)
        {
            var phase = index / 36;
            var stepPattern = cadence[index / 72];
            var elapsed = stepPattern[index % stepPattern.Length];
            // The learner sees arrival order, not hidden physical elapsed time.
            // A change in sampling cadence is therefore a real ambiguity, not
            // evidence that the underlying source necessarily changed speed.
            a += slopesA[phase] * elapsed;
            b += slopesB[phase] * elapsed;
            samples[index] = (a + random.Next(-2, 3), b + random.Next(-2, 3),
                $"{phases[phase]}; cadence-{index / 72 + 1}");
        }
        return samples;
    }

    private static (long A, long B, string Phase)[] SmoothTurning(int seed)
    {
        var random = new Random(seed + 10_000);
        var samples = new (long A, long B, string Phase)[Count];
        var phase = random.NextDouble() * Math.Tau;
        var basePeriod = 54 + random.Next(0, 17);
        for (var index = 0; index < Count; index++)
        {
            // Period and amplitude drift after the midpoint; no period, sine
            // identity, phase, or direction-of-turn flag is given to the learner.
            var drift = Math.Max(0, index - Count / 2) / (double)(Count / 2);
            phase += Math.Tau / (basePeriod + 18 * drift);
            var a = 510 + (175 - 30 * drift) * Math.Sin(phase);
            var b = 460 + 125 * Math.Sin(phase + 0.9) + 40 * Math.Sin(phase * 0.5);
            samples[index] = (Ticks(a + random.Next(-3, 4)), Ticks(b + random.Next(-3, 4)),
                index < Count / 2 ? "steady-turning" : "period-amplitude-drift");
        }
        return samples;
    }

    private static (long A, long B, string Phase)[] MixedSources(int seed)
    {
        var random = new Random(seed + 20_000);
        var samples = new (long A, long B, string Phase)[Count];
        var first = random.Next(2);
        var second = random.Next(2);
        var phase = random.NextDouble() * Math.Tau;
        var period = 78 + random.Next(0, 23);
        double smoothedB = 360;
        for (var index = 0; index < Count; index++)
        {
            // Independent random transitions cannot generally be forecast from
            // their previous sensor values. Persistence and response/recurrence
            // can still be useful. No success criterion requires recovering
            // these particular three latent components.
            if (random.NextDouble() < 0.10) first = 1 - first;
            if (random.NextDouble() < 0.07) second = 1 - second;
            phase += Math.Tau / period;
            var cycle = (1 + Math.Sin(phase)) / 2;
            var drift = Math.Max(0, index - Count / 2) / (double)(Count / 2);
            var a = 120 + (250 + 50 * drift) * first + 45 * second + 180 * cycle;
            var immediateB = 160 + 55 * first + (260 - 35 * drift) * second + 150 * cycle;
            // A bounded host environment state supplies one channel's delayed
            // response. This state and its coefficient never reach the learner.
            smoothedB += (immediateB - smoothedB) * 0.28;
            samples[index] = (Ticks(a + random.Next(-5, 6)), Ticks(smoothedB + random.Next(-5, 6)),
                index < Count / 2 ? "mixed-with-lag" : "mixed-with-lag-and-gain-drift");
        }
        return samples;
    }

    private static (long A, long B, string Phase)[] ShuffledControl(
        int seed, (long A, long B, string Phase)[] mixed)
    {
        // Independent shuffles preserve each mixed trial's exact marginal
        // values and scale while removing its original temporal ordering and
        // paired-channel alignment. This is a finite shuffled control, not a
        // claim of IID physical white noise or provable unpredictability.
        // Generation may inspect the whole environment; the learner may not.
        var a = mixed.Select(sample => sample.A).ToArray();
        var b = mixed.Select(sample => sample.B).ToArray();
        Shuffle(a, new Random(seed + 30_000));
        Shuffle(b, new Random(seed + 40_000));
        return a.Zip(b, (left, right) => (left, right, "independent-shuffled-marginals")).ToArray();
    }

    private static void Shuffle(long[] values, Random random)
    {
        for (var index = values.Length - 1; index > 0; index--)
        {
            var other = random.Next(index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }

    private static long Ticks(double value) =>
        checked((long)Math.Round(value, MidpointRounding.AwayFromZero));
}
