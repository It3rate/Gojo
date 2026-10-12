using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConnectionAssembly;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>
/// Disposable indexed execution of G(equation, input, output, G(bindings)) and
/// G(selector, scope, G(offers), output). Every instruction retains its native
/// source. Current ticks are sampled per call; no derived value is authoritative.
/// Unknown shapes, ambiguous choices and arithmetic outside the fast range use
/// the existing reader. Output and all source-basis residue remain Gojo structure.
/// </summary>
internal static class CompactReadPlan
{
    private static readonly ConditionalWeakTable<Relation, Cache> caches = new();

    public static Relation[] ReadMany(IElement[] roots, NativeExecution? measurement)
    {
        if (roots.Length == 0) return [];
        // Direct coordinates have no stable boxed root to cache.
        if (roots[0] is not Relation first) return LocalReadPlan.ReadMany(roots);
        var cache = caches.GetValue(first, static _ => new Cache());
        // A fresh residual/one-shot inquiry does not benefit from compiling an
        // entire graph. Promote reused roots; keep transient reads native.
        if (Interlocked.Exchange(ref cache.Seen, 1) == 0)
        {
            if (measurement is not null) measurement.DeferredBatches++;
            return LocalReadPlan.ReadMany(roots);
        }
        var program = roots.Length == 1 ? cache.Single : cache.Multiple;
        if (program is null || !program.Matches(roots) || !program.Current())
        {
            lock (cache)
            {
                program = roots.Length == 1 ? cache.Single : cache.Multiple;
                if (program is null || !program.Matches(roots) || !program.Current())
                {
                    program = new Compiler().Build(roots);
                    if (roots.Length == 1) cache.Single = program; else cache.Multiple = program;
                    if (measurement is not null) measurement.PlanBuilds++;
                }
                else if (measurement is not null) measurement.CacheHits++;
            }
        }
        else if (measurement is not null) measurement.CacheHits++;
        if (program.Cyclic) return LocalReadPlan.ReadMany(roots);
        using var run = new Run(program, measurement);
        var result = new Relation[roots.Length];
        for (var i = 0; i < result.Length; i++)
        {
            run.Evaluate(program.Roots[i]);
            result[i] = run.Witness(program.Roots[i]);
        }
        return result;
    }

    public static Relation Correspondence(IElement root)
    {
        var program = new Compiler().Build([root]);
        var axis = new Carrier();
        return G(root, G(program.Code.Select((n, i) => (IElement)new Relation(L(axis, i), n.Source)).ToArray()));
    }

    private sealed class Cache { public int Seen; public Program? Single; public Program? Multiple; }
    // An instruction kind is a compiled read of a connected shape, not its source meaning.
    private enum Kind { Fallback, Leaf, Constant, Affine, Sum, Product, Choice }
    private readonly record struct Tick(int Live = -1, int Source = -1)
    {
        public long Get(Run run) => Source >= 0 ? run.Slots[Source].Value : Live >= 0 ? run.Ticks[Live] : 0;
    }
    private readonly record struct Frame(Carrier Axis, Tick Start, Tick End, bool Point = false);
    private readonly record struct Binding(Carrier Axis, int Source, int Alias);
    private readonly record struct Projection(Frame From, Frame To, Tick Input, Carrier InputAxis);
    private readonly record struct Offer(Relation Native, Frame Support, int Source, int Projection);
    private readonly record struct Sample(long Value, long Numerator, long Denominator, Carrier Source)
    {
        // Project already reduced and range-checked this source subdivision.
        public IElement Residue() => Denominator == 1 ? new CarrierLocation(Source, Numerator) :
            new Relation(new CarrierLocation(Source, Numerator), Relation.Point(Source, Denominator));
    }
    private sealed class Instruction(IElement source)
    {
        public IElement Source = source;
        public Kind Kind;
        public Carrier Output = Carrier.Unknown;
        public Tick Value;
        public Carrier InputAxis = Carrier.Unknown;
        public Tick Input;
        public Frame Target;
        public Binding[] Bindings = [];
        public int[] Projections = [];
        public int Selector;
        public Relation? Scope;
        public Frame Domain;
        public Offer[] Offers = [];
    }
    private sealed class Program
    {
        public IElement[] NativeRoots = [];
        public int[] Roots = [];
        public Instruction[] Code = [];
        public TickRef[] Ticks = [];
        public Projection[] Projections = [];
        public (Relation Node, IElement Left, IElement Right)[] Edges = [];
        public bool Cyclic;
        public bool Matches(IElement[] roots)
        {
            if (roots.Length != NativeRoots.Length) return false;
            for (var i = 0; i < roots.Length; i++) if (!ReferenceEquals(roots[i], NativeRoots[i])) return false;
            return true;
        }
        public bool Current()
        {
            foreach (var edge in Edges)
                if (!ReferenceEquals(edge.Node.Left, edge.Left) || !ReferenceEquals(edge.Node.Right, edge.Right)) return false;
            return true;
        }
    }

    private sealed class Compiler
    {
        private readonly List<Instruction> code = [];
        private readonly Dictionary<IElement, int> nodes = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<IElement> active = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<TickRef, int> tickIndices = new(ReferenceEqualityComparer.Instance);
        private readonly List<TickRef> ticks = [];
        private readonly List<Projection> projections = [];
        private readonly Dictionary<Relation, (IElement Left, IElement Right)> edges = new(ReferenceEqualityComparer.Instance);
        private bool cyclic;

        public Program Build(IElement[] roots)
        {
            foreach (var root in roots) Track(root);
            var indices = roots.Select(Node).ToArray();
            return new() { NativeRoots = roots.ToArray(), Roots = indices, Code = code.ToArray(), Ticks = ticks.ToArray(),
                Projections = projections.ToArray(), Edges = edges.Select(e => (e.Key, e.Value.Left, e.Value.Right)).ToArray(), Cyclic = cyclic };
        }

        private void Track(IElement source)
        {
            if (source is not Relation r || !edges.TryAdd(r, (r.Left, r.Right))) return;
            Track(r.Left); Track(r.Right);
        }

        private Tick Coordinate(TickRef reference, Dictionary<TickRef, int>? bound = null)
        {
            if (bound is not null && bound.TryGetValue(reference, out var source)) return new(-1, source);
            if (!tickIndices.TryGetValue(reference, out var index))
            { index = ticks.Count; tickIndices.Add(reference, index); ticks.Add(reference); }
            return new(index, -1);
        }

        private bool ReadFrame(IElement value, Dictionary<TickRef, int>? bound, out Frame frame)
        {
            if (Point(value, out var point))
            { frame = new(point.Carrier, new(-1, -1), Coordinate(point.Tick, bound), true); return true; }
            if (value is Relation r && Point(r.Left, out var a) && Point(r.Right, out var b) && a.Carrier.SameIdentityAs(b.Carrier))
            { frame = new(a.Carrier, Coordinate(a.Tick, bound), Coordinate(b.Tick, bound)); return true; }
            frame = default; return false;
        }

        private static bool Point(IElement value, out CarrierLocation point)
        {
            if (value is CarrierLocation p) { point = p; return true; }
            if (value is Relation { Left: CarrierLocation site, Right: Carrier axis } && site.Carrier.SameIdentityAs(axis))
            { point = site; return true; }
            point = default; return false;
        }

        private int Project(Frame from, Frame to, Tick input, Carrier axis)
        { var index = projections.Count; projections.Add(new(from, to, input, axis)); return index; }

        private bool Affine(Relation equation, Tick input, Carrier inputAxis, Carrier output,
            Dictionary<TickRef, int>? bound, out int projection)
        {
            projection = -1;
            if (!ReadFrame(equation.Left, bound, out var a) || !ReadFrame(equation.Right, bound, out var b) ||
                inputAxis.IsUnknown || inputAxis.IsInverse || a.Axis.IsInverse || b.Axis.IsInverse) return false;
            if (b.Axis.IsUnknown || b.Axis.SameIdentityAs(output))
            { projection = Project(a, b, input, inputAxis); return true; }
            if (a.Axis.IsUnknown || a.Axis.SameIdentityAs(output))
            { projection = Project(b, a, input, inputAxis); return true; }
            return false;
        }

        private int Node(IElement source)
        {
            if (nodes.TryGetValue(source, out var existing))
            { if (active.Contains(source)) cyclic = true; return existing; }
            var index = code.Count; var n = new Instruction(source); code.Add(n); nodes.Add(source, index); active.Add(source);
            try
            {
                if (source is CarrierLocation leaf && !leaf.Carrier.IsUnknown)
                { n.Kind = Kind.Leaf; n.Output = leaf.Carrier; n.Value = Coordinate(leaf.Tick); return index; }
                if (source is not Relation root) return index;
                n.Output = NativeMath.Output(root);
                if (ScopedChoice.TryRead(root, out var selector, out var scope, out var offers, out var output))
                {
                    n.Output = output; n.Selector = Node(selector); n.Scope = scope;
                    if (!ReadFrame(scope, null, out n.Domain) || n.Domain.Axis.IsInverse) return index;
                    var choices = new List<Offer>();
                    foreach (var offer in offers)
                    {
                        if (!ScopedChoice.TryReadOffer(offer, out var support, out var value, out var calibration) ||
                            !ReadFrame(support, null, out var span)) return index;
                        var branch = Node(value);
                        // Open branches stay lazy. Selecting one falls back to the native unresolved read.
                        Affine(calibration, new(-1, branch), code[branch].Output, output, null, out var map);
                        choices.Add(new(offer, span, branch, map));
                    }
                    n.Offers = choices.ToArray(); n.Kind = Kind.Choice; return index;
                }
                var fields = Parts(root);
                if (fields.Length != 4 || fields[0] is not Relation equation || fields[2] is not Carrier target ||
                    target.IsUnknown || target.IsInverse || fields[3] is not Relation bindings) return index;
                n.Output = target;
                var bound = new Dictionary<TickRef, int>(ReferenceEqualityComparer.Instance);
                var aliases = new Dictionary<TickRef, int>(ReferenceEqualityComparer.Instance);
                var entries = new List<Binding>();
                foreach (var entry in Parts(bindings))
                {
                    if (entry is not Relation { Left: CarrierLocation site } binding || site.Carrier.IsUnknown) return index;
                    var upstream = Node(binding.Right);
                    var alias = aliases.TryGetValue(site.Tick, out var prior) ? prior : -1;
                    entries.Add(new(site.Carrier, upstream, alias));
                    if (alias < 0) { aliases.Add(site.Tick, entries.Count - 1); bound.Add(site.Tick, upstream); }
                }
                n.Bindings = entries.ToArray();
                if (equation.Left is CarrierLocation constant && equation.Right is Carrier carrier &&
                    constant.Carrier.SameIdentityAs(carrier) && target.SameIdentityAs(carrier) && fields[1] is CarrierLocation supplied)
                {
                    n.Value = Coordinate(constant.Tick, bound); n.Input = Coordinate(supplied.Tick, bound);
                    n.InputAxis = supplied.Carrier; n.Kind = Kind.Constant; return index;
                }
                if (fields[1] is CarrierLocation scalar && Affine(equation, Coordinate(scalar.Tick, bound), scalar.Carrier, target, bound, out var affine))
                { n.Projections = [affine]; n.Kind = Kind.Affine; return index; }
                if (!ReadFrame(equation.Right, bound, out n.Target) ||
                    (!n.Target.Axis.IsUnknown && !n.Target.Axis.SameIdentityAs(target))) return index;
                var ports = new List<int>();
                bool Operands(IElement port, IElement input)
                {
                    if (port is Relation p && !ReadFrame(p, bound, out _) && ReadFrame(p.Left, bound, out var from) &&
                        ReadFrame(p.Right, bound, out var to) && input is CarrierLocation location)
                    {
                        if (from.Axis.IsInverse || to.Axis.IsInverse || location.Carrier.IsInverse || location.Carrier.IsUnknown) return false;
                        ports.Add(Project(from, to, Coordinate(location.Tick, bound), location.Carrier)); return true;
                    }
                    return port is Relation group && input is Relation inputs && Operands(group.Left, inputs.Left) && Operands(group.Right, inputs.Right);
                }
                if (!Operands(equation.Left, fields[1]) || ports.Count == 0) return index;
                n.Projections = ports.ToArray(); n.Kind = n.Target.Point ? Kind.Sum : Kind.Product; return index;
            }
            finally { active.Remove(source); }
        }
    }

    private struct Slot
    {
        public byte State;
        public long Value;
        public int Selected;
        public Relation? Read;
        public bool Native;
    }

    private sealed class Run : IDisposable
    {
        private readonly Program program;
        private readonly NativeExecution? measurement;
        private readonly bool simd;
        internal readonly Slot[] Slots;
        internal readonly long[] Ticks;
        private readonly Sample[] samples;
        private readonly TickRef?[] observed;

        public Run(Program program, NativeExecution? measurement)
        {
            this.program = program; this.measurement = measurement;
            simd = measurement?.Backend == NativeExecutionBackend.CompactSimd && NativeExecution.SimdSupported;
            Slots = ArrayPool<Slot>.Shared.Rent(Math.Max(1, program.Code.Length));
            Array.Clear(Slots, 0, program.Code.Length);
            Ticks = ArrayPool<long>.Shared.Rent(Math.Max(1, program.Ticks.Length));
            observed = ArrayPool<TickRef?>.Shared.Rent(Math.Max(1, program.Ticks.Length));
            Array.Clear(observed, 0, program.Ticks.Length);
            samples = ArrayPool<Sample>.Shared.Rent(Math.Max(1, program.Projections.Length));
            for (var i = 0; i < program.Ticks.Length; i++) Ticks[i] = program.Ticks[i].Ticks;
        }

        private Carrier Output(int index) => Slots[index].Native
            ? Slots[index].Read!.Left is CarrierLocation point ? point.Carrier : Carrier.Unknown : program.Code[index].Output;

        public bool Evaluate(int index)
        {
            ref var slot = ref Slots[index];
            if (slot.State == 2) return !slot.Native || TryValue(slot.Read!, out _);
            if (slot.State == 1) return Fallback(index);
            slot.State = 1;
            var n = program.Code[index];
            try
            {
                if (n.Kind == Kind.Fallback) return Fallback(index);
                if (n.Kind == Kind.Leaf) slot.Value = n.Value.Get(this);
                else if (n.Kind == Kind.Choice)
                {
                    if (!Evaluate(n.Selector) || !Output(n.Selector).SameIdentityAs(n.Domain.Axis)) return Fallback(index);
                    var selector = Slots[n.Selector].Value;
                    var lower = n.Domain.Start.Get(this); var upper = n.Domain.End.Get(this);
                    if (lower >= upper || selector < lower || selector >= upper || selector == long.MaxValue) return Fallback(index);
                    var chosen = -1;
                    for (var i = 0; i < n.Offers.Length; i++)
                    {
                        var support = n.Offers[i].Support;
                        var a = support.Start.Get(this); var b = support.End.Get(this);
                        if (!support.Axis.SameIdentityAs(n.Domain.Axis) || a >= b || a < lower || b > upper) return Fallback(index);
                        if (selector >= a && selector < b)
                        { if (chosen >= 0) return Fallback(index); chosen = i; }
                    }
                    if (chosen < 0) return Fallback(index);
                    var offer = n.Offers[chosen];
                    if (offer.Projection < 0 || !Evaluate(offer.Source) ||
                        !Output(offer.Source).SameIdentityAs(program.Projections[offer.Projection].InputAxis) ||
                        !Project(offer.Projection)) return Fallback(index);
                    slot.Selected = chosen; slot.Value = samples[offer.Projection].Value;
                }
                else
                {
                    for (var i = 0; i < n.Bindings.Length; i++)
                    {
                        var binding = n.Bindings[i];
                        if (!Evaluate(binding.Source) || !Output(binding.Source).SameIdentityAs(binding.Axis) ||
                            binding.Alias >= 0 && Slots[binding.Source].Value != Slots[n.Bindings[binding.Alias].Source].Value) return Fallback(index);
                    }
                    if (n.Kind == Kind.Constant) slot.Value = n.Value.Get(this);
                    else if (n.Kind == Kind.Affine)
                    { if (!Project(n.Projections[0])) return Fallback(index); slot.Value = samples[n.Projections[0]].Value; }
                    else
                    {
                        var extent = checked((Int128)n.Target.End.Get(this) - n.Target.Start.Get(this));
                        if (extent == 0) return Fallback(index);
                        Int128 value = n.Kind == Kind.Sum ? 0 : 1;
                        for (var i = 0; i < n.Projections.Length; i++)
                        {
                            var projection = n.Projections[i];
                            // Only sibling operands of this already-selected read
                            // are grouped. Choice alternatives remain lazy.
                            if (simd && i + 1 < n.Projections.Length && ProjectPair(projection, n.Projections[i + 1]))
                            {
                                value = n.Kind == Kind.Sum ? checked(value + samples[projection].Value) : checked(value * samples[projection].Value);
                                var next = n.Projections[++i];
                                value = n.Kind == Kind.Sum ? checked(value + samples[next].Value) : checked(value * samples[next].Value);
                                continue;
                            }
                            if (!Project(projection)) return Fallback(index);
                            value = n.Kind == Kind.Sum ? checked(value + samples[projection].Value) : checked(value * samples[projection].Value);
                        }
                        slot.Value = checked((long)(checked(value * extent) + n.Target.Start.Get(this)));
                    }
                }
                slot.State = 2;
                if (measurement is not null) measurement.NumericSteps++;
                return true;
            }
            catch (OverflowException) { return Fallback(index); }
        }

        private bool Fallback(int index)
        {
            var read = LocalReadPlan.ReadMany([program.Code[index].Source])[0];
            ref var slot = ref Slots[index]; slot.State = 2; slot.Native = true; slot.Read = read;
            var complete = TryValue(read, out slot.Value);
            if (measurement is not null) measurement.Fallbacks++;
            return complete;
        }

        private bool Project(int index)
        {
            var p = program.Projections[index];
            var a = p.From.Start.Get(this); var b = p.From.End.Get(this);
            if (!(p.From.Axis.IsUnknown || p.From.Axis.SameIdentityAs(p.InputAxis) ||
                a == 0 && p.From.Axis.UnitShape.CompatibleWith(p.InputAxis.UnitShape))) return false;
            var sourceExtent = checked((Int128)b - a);
            if (sourceExtent == 0) return false;
            var c = p.To.Start.Get(this); var targetExtent = checked((Int128)p.To.End.Get(this) - c);
            var input = p.Input.Get(this);
            if (targetExtent == 0) { samples[index] = new(c, input, 1, p.InputAxis); return true; }
            var numerator = checked(checked((Int128)input - a) * targetExtent);
            var quotient = numerator / sourceExtent;
            var remainder = numerator % sourceExtent;
            var denominator = targetExtent;
            if (denominator < 0) { remainder = checked(-remainder); denominator = checked(-denominator); }
            var gcd = Gcd(remainder, denominator);
            samples[index] = new(checked((long)(c + quotient)), checked((long)(remainder / gcd)),
                checked((long)(denominator / gcd)), p.InputAxis);
            return true;
        }

        private readonly record struct IntegralLane(double Delta, double Factor, double Offset, Carrier Source);

        // This exploratory SIMD path deliberately accepts only exact integer
        // affine maps. Each lane has |delta| <= 2^26, |factor| <= 2^20,
        // |offset| <= 2^46, so its product and final sum are exact IEEE doubles
        // within +/-2^47. No packed Int128 or approximate division is used.
        // Nonintegral maps retain the scalar Int128 projection and residue.
        private bool Integral(int index, out IntegralLane lane)
        {
            lane = default;
            var p = program.Projections[index];
            var a = p.From.Start.Get(this); var b = p.From.End.Get(this);
            if (!(p.From.Axis.IsUnknown || p.From.Axis.SameIdentityAs(p.InputAxis) ||
                a == 0 && p.From.Axis.UnitShape.CompatibleWith(p.InputAxis.UnitShape))) return false;
            var sourceExtent = (Int128)b - a;
            var c = p.To.Start.Get(this); var targetExtent = (Int128)p.To.End.Get(this) - c;
            if (sourceExtent == 0 || targetExtent == 0 || targetExtent % sourceExtent != 0) return false;
            var factor = targetExtent / sourceExtent;
            var delta = (Int128)p.Input.Get(this) - a;
            if (factor < -(1L << 20) || factor > 1L << 20 || delta < -(1L << 26) || delta > 1L << 26 ||
                c < -(1L << 46) || c > 1L << 46) return false;
            lane = new((double)delta, (double)factor, c, p.InputAxis);
            return true;
        }

        private bool ProjectPair(int first, int second)
        {
            if (!Integral(first, out var a) || !Integral(second, out var b)) return false;
            var result = Vector128.Create(a.Delta, b.Delta) * Vector128.Create(a.Factor, b.Factor) +
                Vector128.Create(a.Offset, b.Offset);
            samples[first] = new((long)result.GetElement(0), 0, 1, a.Source);
            samples[second] = new((long)result.GetElement(1), 0, 1, b.Source);
            if (measurement is not null) { measurement.SimdBatches++; measurement.SimdLanes += 2; }
            return true;
        }

        private static Int128 Gcd(Int128 a, Int128 b)
        {
            if (a < 0) a = checked(-a);
            while (b != 0) { var remainder = a % b; a = b; b = remainder; }
            return a;
        }

        public Relation Witness(int index)
        {
            ref var slot = ref Slots[index];
            if (slot.Read is not null) return slot.Read;
            var n = program.Code[index]; IElement residue;
            if (n.Kind == Kind.Leaf)
            {
                var tick = observed[n.Value.Live] ??= new TickRef(slot.Value);
                return slot.Read = new(new CarrierLocation(n.Output, tick), Relation.Point(n.Output, 0));
            }
            if (n.Kind == Kind.Choice)
            {
                var selector = Witness(n.Selector); var offer = n.Offers[slot.Selected];
                var occupied = Relation.Segment(n.Domain.Axis, Slots[n.Selector].Value, Slots[n.Selector].Value + 1);
                var evidence = G(selector, occupied, G(offer.Native));
                var selection = ReadWitness.Create(new Carrier(), n.Source, n.Scope!, offer.Native, selector.Right, evidence);
                residue = G(selection, Witness(offer.Source).Right, samples[offer.Projection].Residue());
            }
            else
            {
                if (n.Kind == Kind.Constant) residue = new CarrierLocation(n.InputAxis, n.Input.Get(this));
                else
                {
                    residue = samples[n.Projections[^1]].Residue();
                    for (var i = n.Projections.Length - 2; i >= 0; i--)
                        residue = new Relation(samples[n.Projections[i]].Residue(), residue);
                }
                foreach (var binding in n.Bindings) residue = new Relation(residue, Witness(binding.Source).Right);
            }
            return slot.Read = new(new CarrierLocation(n.Output, slot.Value), residue);
        }

        public void Dispose()
        {
            ArrayPool<Slot>.Shared.Return(Slots, clearArray: true);
            ArrayPool<long>.Shared.Return(Ticks);
            ArrayPool<TickRef?>.Shared.Return(observed, clearArray: true);
            ArrayPool<Sample>.Shared.Return(samples, clearArray: true);
        }
    }
}
