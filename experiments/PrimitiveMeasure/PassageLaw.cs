namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>An eight-byte relation between whole before/after presence states.
/// State = S + 2*C + 4*T; these are three V bits in a fixed compatible context,
/// not an ODV word. Bit before + 8*after admits that complete transition.
/// The law preserves control and permits only holding or a whole forward
/// handoff into an empty receiver. Presence never specifies an amount of one.</summary>
public sealed class PassageLaw
{
    private readonly byte[] allowed;

    public PassageLaw(ReadOnlySpan<byte> bitmap)
    {
        if (bitmap.Length != 8)
            throw new ArgumentException("Expected one bit for each of the 64 before/after pairs.", nameof(bitmap));

        allowed = bitmap.ToArray();
        for (var before = 0; before < 8; before++)
            for (var after = 0; after < 8; after++)
            {
                if ((allowed[after] & (1 << before)) == 0) continue;
                var sourceBefore = before & 1;
                var sourceAfter = after & 1;
                var targetBefore = (before >> 2) & 1;
                var targetAfter = (after >> 2) & 1;
                if ((before & 2) != (after & 2) || sourceAfter > sourceBefore ||
                    targetAfter < targetBefore || sourceBefore + targetBefore != sourceAfter + targetAfter)
                    throw new ArgumentException("A passage must preserve control and permit only holding or a whole forward handoff.", nameof(bitmap));
            }
    }

    /// <summary>Compile a passage that hands off only when S=1, C=1, T=0.
    /// Execution and prediction use the resulting bits, not this builder's name.</summary>
    public static PassageLaw Controlled()
    {
        var bitmap = new byte[8];
        for (var before = 0; before < 8; before++)
        {
            var after = before == State(true, true, false) ? State(false, true, true) : before;
            bitmap[after] |= (byte)(1 << before);
        }
        return new PassageLaw(bitmap);
    }

    public byte[] PackedBytes() => allowed.ToArray();

    public bool Allows(byte before, byte after)
    {
        if (before > 7) throw new ArgumentOutOfRangeException(nameof(before));
        if (after > 7) throw new ArgumentOutOfRangeException(nameof(after));
        return (allowed[after] & (1 << before)) != 0;
    }

    /// <summary>Bit n admits the complete three-site state n. Zero support
    /// means no admitted state; support 1 means the known all-empty state.
    /// Uncertainty remains a set of joint states, not independent output bits.</summary>
    public byte Predict(byte beforeSupport)
    {
        byte result = 0;
        for (var after = 0; after < 8; after++)
            if ((allowed[after] & beforeSupport) != 0) result |= (byte)(1 << after);
        return result;
    }

    public static byte State(bool source, bool control, bool target) =>
        (byte)((source ? 1 : 0) | (control ? 2 : 0) | (target ? 4 : 0));

    /// <summary>Apply this law within a selected three-to-five-site scope.
    /// Role positions select S/C/T from each whole candidate. Replacing these
    /// three bits in that same candidate preserves all other participation and
    /// correspondence, including a control shared by successive passages.
    /// This is a local prediction; it neither updates stores nor schedules steps.</summary>
    public uint PredictScope(uint beforeSupport, ReadOnlySpan<int> rolePositions, int siteCount)
    {
        var roleMask = ValidateScope(beforeSupport, rolePositions, siteCount);
        uint result = 0;
        for (var wholeBefore = 0; wholeBefore < (1 << siteCount); wholeBefore++)
        {
            if ((beforeSupport & (1U << wholeBefore)) == 0) continue;
            var before = Extract(wholeBefore, rolePositions);
            var afterSupport = Predict((byte)(1 << before));
            for (var after = 0; after < 8; after++)
            {
                if ((afterSupport & (1 << after)) == 0) continue;
                var wholeAfter = wholeBefore & ~roleMask;
                for (var role = 0; role < 3; role++)
                    wholeAfter |= ((after >> role) & 1) << rolePositions[role];
                result |= 1U << wholeAfter;
            }
        }
        return result;
    }

    /// <summary>Read one passage's three roles from a joint scope support.
    /// The returned projection can lose relations with omitted sites; retain
    /// the original scope support when predicting another connected passage.</summary>
    public static byte ProjectScope(uint support, ReadOnlySpan<int> rolePositions, int siteCount)
    {
        ValidateScope(support, rolePositions, siteCount);
        byte result = 0;
        for (var state = 0; state < (1 << siteCount); state++)
            if ((support & (1U << state)) != 0)
                result |= (byte)(1 << Extract(state, rolePositions));
        return result;
    }

    private static byte Extract(int state, ReadOnlySpan<int> rolePositions) =>
        (byte)(((state >> rolePositions[0]) & 1) |
            (((state >> rolePositions[1]) & 1) << 1) |
            (((state >> rolePositions[2]) & 1) << 2));

    private static int ValidateScope(uint support, ReadOnlySpan<int> rolePositions, int siteCount)
    {
        if (siteCount is < 3 or > 5) throw new ArgumentOutOfRangeException(nameof(siteCount));
        if (rolePositions.Length != 3)
            throw new ArgumentException("Supply exactly the source, control and target positions.", nameof(rolePositions));
        var roleMask = 0;
        foreach (var position in rolePositions)
        {
            if (position < 0 || position >= siteCount)
                throw new ArgumentOutOfRangeException(nameof(rolePositions));
            var bit = 1 << position;
            if ((roleMask & bit) != 0)
                throw new ArgumentException("Passage roles must refer to distinct sites.", nameof(rolePositions));
            roleMask |= bit;
        }
        var validSupport = siteCount == 5 ? uint.MaxValue : (1U << (1 << siteCount)) - 1;
        if ((support & ~validSupport) != 0)
            throw new ArgumentOutOfRangeException(nameof(support), "Support contains states outside this scope.");
        return roleMask;
    }
}
