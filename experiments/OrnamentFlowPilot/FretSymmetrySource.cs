using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.PrimitiveMeasure;
using System.Text.Json.Serialization;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal sealed record FretSourceRead(string AcrossStrip, string RiseStrip, string Clock, int BeatsPerLap,
    string MirrorStrip, int Laps, int[] AcrossReaches, int[] RiseReaches, PathSite[] Segments,
    [property: JsonIgnore] byte[] StoredWords, [property: JsonIgnore] byte[] EmittedWords);

// One continuous centerline, starting where a long upper stroke begins.
// The two native motion voices share a clock and deposit into one ordered path.
internal static class FretSymmetrySource
{
    private static byte Move(int axis, bool negative, int reach) =>
        (byte)((reach << 4) | (axis << 3) | ThreeBitMeasure.Encode(false, negative, true));

    internal static PathSite[] Direct(int innerReach = 1) => ReadDirect(innerReach).Segments;
    internal static PathSite[] Glide(int innerReach = 1) => ReadGlide(innerReach).Segments;

    internal static FretSourceRead ReadDirect(int innerReach = 1)
    {
        // The two E1 strokes have equal observed lengths and independent reach ports.
        byte[] across = [Move(0, false, 0), Move(0, true, 1), Move(0, false, 2), Move(0, true, 1),
                         Move(0, false, 0), Move(0, true, 1), Move(0, false, 3), Move(0, true, 1)];
        byte[] rise = [Move(1, true, 0), Move(1, false, 1), Move(1, false, 1), Move(1, true, 0),
                       Move(1, false, 0), Move(1, true, 1), Move(1, true, 1), Move(1, false, 0)];
        return Read(across, rise, new ParticipationStrip(0), [7, 2, innerReach, 1], [3, 1]);
    }

    internal static FretSourceRead ReadGlide(int innerReach = 1)
    {
        byte[] across = [Move(0, false, 0), Move(0, true, 1), Move(0, false, 2), Move(0, true, 1)];
        byte[] rise = [Move(1, true, 0), Move(1, false, 1), Move(1, false, 1), Move(1, true, 0)];
        return Read(across, rise, new ParticipationStrip(0, 1), [7, 2, innerReach], [3, 1]);
    }

    // A shorter invented half: across, down, back, down. The same two
    // voices play it again in the reflected vertical frame.
    internal static FretSourceRead ReadHook(int across, int inset, int firstDrop, int lastDrop)
    {
        byte[] horizontal = [Move(0, false, 0), Move(0, true, 1)];
        byte[] vertical = [Move(1, true, 0), Move(1, true, 1)];
        return Read(horizontal, vertical, new ParticipationStrip(0, 1), [across, inset], [firstDrop, lastDrop]);
    }

    internal static FretSourceRead ReadIndependentHook(int across, int inset, int firstDrop, int lastDrop, int secondAcross)
    {
        byte[] horizontal = [Move(0, false, 0), Move(0, true, 1), Move(0, false, 2), Move(0, true, 1)];
        byte[] vertical = [Move(1, true, 0), Move(1, true, 1), Move(1, false, 0), Move(1, false, 1)];
        return Read(horizontal, vertical, new ParticipationStrip(0), [across, inset, secondAcross], [firstDrop, lastDrop]);
    }

    internal static FretSourceRead Read(byte[] across, byte[] rise, ParticipationStrip mirror,
        int[] acrossReaches, int[] riseReaches)
    {
        if (acrossReaches.Concat(riseReaches).Any(length => length <= 0))
            throw new ArgumentException("A participating reach in this fixture must have positive extent.");
        var score = new BorderScore(across, rise, 1, 2);
        var lap = score.Predict();
        var segments = new List<PathSite>();
        var stored = new List<byte>();
        var emitted = new List<byte>();
        var phase = default(BorderPhase);
        for (int cycle = 0; cycle < mirror.Count; cycle++)
        {
            for (int beat = 0; beat < lap.Beats; beat++)
            {
                var move = score.Step(ref phase).Single().Move;
                stored.Add(move.Bits);
                // Reflection changes the sign in the rise frame. Clock and row order stay forward.
                if (move.Axis == 1 && mirror.At(cycle) != 0) move = move.Backward();
                emitted.Add(move.Bits);
                var vector = move.Axis == 0
                    ? new PathSite(move.Sense * acrossReaches[move.Reach], 0)
                    : new PathSite(0, move.Sense * riseReaches[move.Reach]);
                segments.Add(vector);
            }
        }
        return new(Convert.ToHexString(score.VoiceBytes(0)), Convert.ToHexString(score.VoiceBytes(1)),
            Convert.ToHexString(score.ClockBytes()), lap.Beats, Convert.ToHexString(mirror.Bytes()),
            mirror.Count, acrossReaches, riseReaches, segments.ToArray(), stored.ToArray(), emitted.ToArray());
    }
}
