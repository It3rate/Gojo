namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A non-draining group read in one fixed positive O/D context.
/// Bit L + 2*R admits a whole pair of V readings. Zero admits no pair.</summary>
public abstract class PresencePairPort
{
    public abstract byte ReadPairs();

    /// <summary>An unwired role has no additional restriction. A wired observer
    /// with no admitted word supplies no usable reading in this context.</summary>
    public static PresencePairPort Bind(byte admitted, IPrimitiveReadPort? left = null,
        IPrimitiveReadPort? right = null) => new BoundPair(CheckMask(admitted), left, right);

    /// <summary>Four two-bit fields, low first. Field s gives the output pair
    /// for input pair s. Both outputs therefore follow the same candidate.</summary>
    public PresencePairPort Map(byte fields) => new MappedPair(this, fields);

    /// <summary>One truth bit per whole pair. Returns ordinary ODV support.</summary>
    public IPrimitiveReadPort Test(byte truth) => new Predicate(this, CheckMask(truth));

    private static byte CheckMask(byte mask) => mask <= 15 ? mask :
        throw new ArgumentOutOfRangeException(nameof(mask), "A pair has four possible states.");

    private sealed class BoundPair : PresencePairPort
    {
        private readonly byte admitted;
        private readonly ReadConnections imports = new();
        private readonly int? left, right;

        public BoundPair(byte admitted, IPrimitiveReadPort? left, IPrimitiveReadPort? right)
        {
            this.admitted = admitted;
            this.left = left is null ? null : imports.Import(left);
            this.right = right is null ? null : imports.Import(right);
        }

        private byte Sample(int? endpoint) => endpoint is int id ? imports.Read(id) : (byte)3;

        public override byte ReadPairs()
        {
            var first = Sample(left);
            var shared = left.HasValue && left == right;
            var second = shared ? first : Sample(right);
            if (((first | second) & ~3) != 0) return 0;
            int pairs = 0;
            for (int state = 0; state < 4; state++)
            {
                int l = state & 1, r = state >> 1;
                if ((first & (1 << l)) != 0 && (second & (1 << r)) != 0 && (!shared || l == r))
                    pairs |= 1 << state;
            }
            return (byte)(pairs & admitted);
        }
    }

    private sealed class MappedPair(PresencePairPort input, byte fields) : PresencePairPort
    {
        public override byte ReadPairs()
        {
            var candidates = input.ReadPairs();
            int result = 0;
            for (int state = 0; state < 4; state++)
                if ((candidates & (1 << state)) != 0)
                    result |= 1 << ((fields >> (2 * state)) & 3);
            return (byte)result;
        }
    }

    private sealed class Predicate(PresencePairPort input, byte truth) : IPrimitiveReadPort
    {
        public byte ReadSupport()
        {
            var candidates = input.ReadPairs();
            int no = (candidates & ~truth) != 0 ? 1 : 0;
            int yes = (candidates & truth) != 0 ? 2 : 0;
            return (byte)(no | yes);
        }
    }
}
