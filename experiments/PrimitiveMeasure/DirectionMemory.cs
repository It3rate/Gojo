namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A selected transition on one O D V state word. D retains the last
/// resolved direction; V says whether the latest input resolves magnitude.
/// The caller supplies the initial direction and keeps the returned state.
/// This is a read/controller state, not a second store of material flow.</summary>
public static class DirectionMemory
{
    /// <summary>Connections for one update: input.V -> next.V; input.V selects
    /// input.D -> next.D or previous.D -> next.D. The latter is the feedback
    /// path. Orientation must agree; no update is selected across orientations.
    /// An exact zero or a reader-relative unresolved magnitude retains D.
    /// This does not alter ThreeBitMeasure's exact-zero interpretation.</summary>
    public static byte? Update(byte previous, byte observed)
    {
        if (previous > 7) throw new ArgumentOutOfRangeException(nameof(previous));
        if (observed > 7) throw new ArgumentOutOfRangeException(nameof(observed));
        if (((previous ^ observed) & ThreeBitMeasure.Orientation) != 0) return null;
        return (observed & ThreeBitMeasure.Value) != 0
            ? observed
            : (byte)(previous & (ThreeBitMeasure.Orientation | ThreeBitMeasure.Direction));
    }

    /// <summary>One byte, six meaningful bits: input ODV in bits 0..2 and
    /// memory ODV in bits 3..5; bits 6..7 are zero padding. The selected layout
    /// supplies Update's fixed feedback law, not reconfigurable wire storage.
    /// Update only the memory bits, leaving the input intact. True means a
    /// compatible transition was selected, even when memory stays unchanged.
    /// Different orientations return false and leave the complete byte intact.</summary>
    public static bool UpdateFrame(Span<byte> frame)
    {
        if (frame.Length != 1) throw new ArgumentException("Expected one six-bit frame byte.", nameof(frame));
        if ((frame[0] & 0b11000000) != 0) throw new ArgumentException("Frame padding bits must be zero.", nameof(frame));
        var observed = (byte)(frame[0] & 7);
        var previous = (byte)(frame[0] >> 3);
        var next = Update(previous, observed);
        if (next is not byte memory) return false;
        frame[0] = (byte)(observed | (memory << 3));
        return true;
    }

    /// <summary>Fine demonstration apparatus, in a chosen positive-right basis.
    /// V means |amount| exceeds threshold, not necessarily physical nonzero.
    /// At or inside the threshold this reader exposes neither magnitude nor
    /// sign, and emits canonical +0. Update supplies retained direction from
    /// its own state, never by inspecting the hidden sign of a weak input.
    /// Threshold zero agrees with the existing exact-zero observer.</summary>
    public static byte ObserveAtThreshold(long amount, long threshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        // Comparing both bounds avoids taking Abs(long.MinValue).
        var resolved = amount > threshold || amount < -threshold;
        return resolved ? ThreeBitMeasure.Encode(false, amount < 0, true) : (byte)0;
    }
}
