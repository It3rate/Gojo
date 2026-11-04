using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One pair of reference tickets and the parent's connected test words.
/// Only the requesting desk can bind the result back into its local vacancy.</summary>
public sealed class TimingRequest
{
    public int Pair { get; }
    public IReadOnlyList<WasherProbe> Parents { get; }
    internal bool Protect { get; }
    internal int DeskBeats { get; }
    internal Func<WasherProbe,int,int,bool> Bind { get; }
    internal TimingRequest(int pair, WasherProbe[] parents, bool protect, int deskBeats,
        Func<WasherProbe,int,int,bool> bind)
    {
        Pair = pair; Parents = Array.AsReadOnly(parents); Protect = protect; DeskBeats = deskBeats; Bind = bind;
    }
}

public readonly record struct PulseEdit(WasherProbe Parent, WasherProbe Child);
public readonly record struct TimingCheck(PulseEdit Edit, bool Repeat, TimingRead? Left, TimingRead? Right,
    string Outcome, int BeatsLeft, int UnitsLeft);

public sealed class TimingBench
{
    // Acceptance bit first + 4*repeat. A resolved pair encodes leftON + 2*rightON.
    public const ushort RetentionLaw = (1 << 5) | (1 << 10);
    public const int Earlier = 1, Later = 2, Both = 3;
    private readonly TimingRequest request;
    private readonly Func<int,WasherProbe,TimingRead> reference;
    private readonly Func<WasherProbe,int> maximum;
    private readonly PulseEdit[] edits;
    private readonly int left, right;
    private int cursor;
    private int? first;
    private int risk;
    public int BeatsLeft { get; private set; }
    public int UnitsLeft { get; private set; }
    public PulseEdit? Retained { get; private set; }

    public TimingBench(TimingRequest request, Func<int,WasherProbe,TimingRead> reference,
        Func<WasherProbe,int> maximumUnits, int beats, int units, int directions = Both)
    {
        if (beats < 0 || units < 0 || (uint)directions > Both) throw new ArgumentOutOfRangeException();
        this.request = request; this.reference = reference; maximum = maximumUnits;
        BeatsLeft = beats; UnitsLeft = units;
        left = BitOperations.TrailingZeroCount((uint)request.Pair);
        right = BitOperations.Log2((uint)request.Pair);
        edits = Neighbors(request.Parents,directions).ToArray();
    }

    /// <summary>Move a set contact into an adjacent empty cell of the same wire.
    /// Preserve the end clock, sensor, pulse counts and the original word.</summary>
    public static IEnumerable<PulseEdit> Neighbors(IReadOnlyList<WasherProbe> parents, int directions)
    {
        var seen = parents.Select(p=>p.Word).ToHashSet();
        foreach (var parent in parents)
            for (int offset = 0; offset <= 3; offset += 3)
            for (int i = 0; i < parent.Beats-1; i++)
            {
                int at = i+offset;
                int pair = (parent.Word >> at) & 3;
                int direction = pair switch { 1 => Later, 2 => Earlier, _ => 0 };
                if ((directions & direction) == 0) continue;
                var child = new WasherProbe((byte)(parent.Word ^ (3 << at)));
                if (child.Forward != 0 && child.Beats == parent.Beats && seen.Add(child.Word))
                    yield return new(parent,child);
            }
    }

    public TimingCheck? Step()
    {
        if (Retained.HasValue || cursor == edits.Length) return null;
        var edit = edits[cursor];
        var p = edit.Child;
        int? previous = first;
        first = null;
        int cap = maximum(p);
        if (cap < 0) throw new InvalidOperationException("Invalid material reservation.");
        cursor++; // Commit this attempt even if a fixture fails after consuming material.
        string? block = p.Beats > request.DeskBeats ? "Desk time limit" :
            request.Protect && cap > 1 ? "Stock protection" :
            BeatsLeft / 2 < p.Beats ? "Bench time limit" : UnitsLeft / 2 < cap ? "Material limit" : null;
        if (block != null) return new(edit,previous.HasValue,null,null,block,BeatsLeft,UnitsLeft);

        TimingRead Read(int ticket)
        {
            BeatsLeft -= p.Beats;
            UnitsLeft -= cap;
            var read = reference(ticket,p);
            if (read.Support > 3 || (uint)read.Units > cap)
                throw new InvalidOperationException("Reference exceeded its declared surface or capacity.");
            UnitsLeft += cap-read.Units;
            return read;
        }
        TimingRead a,b;
        if (previous.HasValue) { b = Read(right); a = Read(left); }
        else { a = Read(left); b = Read(right); risk = 0; }
        if (a.Units>1) risk |= 1 << left;
        if (b.Units>1) risk |= 1 << right;
        bool resolved = a.Support is 1 or 2 && b.Support is 1 or 2;
        int pair = (a.Support >> 1) | ((b.Support >> 1) << 1);
        string outcome;
        if (!resolved) outcome = "Incomplete readings";
        else if (previous is int prior)
        {
            if ((RetentionLaw & (1 << (prior + 4*pair))) == 0) outcome = "Split did not repeat";
            else if (request.Bind(p,1 << (pair == 1 ? left : right),risk))
            {
                Retained = edit;
                outcome = "Socket retained";
            }
            else outcome = "Request expired";
        }
        else if (pair is 1 or 2)
        {
            first = pair;
            cursor--; // Clock the same variant on two fresh references in reverse order.
            outcome = "Repeat requested";
        }
        else outcome = "References agree";
        return new(edit,previous.HasValue,a,b,outcome,BeatsLeft,UnitsLeft);
    }
}
