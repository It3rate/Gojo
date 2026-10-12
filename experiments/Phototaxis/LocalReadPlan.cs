using System.Runtime.CompilerServices;
using ConnectionAssembly;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace Phototaxis;

/// <summary>
/// Disposable execution projection of ordinary native recipes. It retains
/// topology handles, never authoritative coefficients or computed values.
/// All numeric work and residue still come from Gojo's equation readers.
/// </summary>
internal static class LocalReadPlan
{
    private static readonly ConditionalWeakTable<Relation, Plan> plans = new();

    public static Relation[] ReadMany(IElement[] roots)
    {
        var memo = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var active = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var observed = new Dictionary<TickRef, TickRef>(ReferenceEqualityComparer.Instance);

        TickRef Observe(TickRef tick)
        {
            if (!observed.TryGetValue(tick, out var frozen)) observed.Add(tick, frozen = new TickRef(tick.Ticks));
            return frozen;
        }

        Relation Evaluate(IElement source)
        {
            if (source is CarrierLocation leaf && !leaf.Carrier.IsUnknown)
                return new Relation(new CarrierLocation(leaf.Carrier, Observe(leaf.Tick)), Relation.Point(leaf.Carrier, 0));
            if (source is not Relation node) return Relation.UnresolvedRead(source, source, NativeMath.Output(source));
            if (memo.TryGetValue(node, out var previous)) return previous;
            if (!active.Add(node)) return Relation.UnresolvedRead(node, node, NativeMath.Output(node));
            try
            {
                if (ScopedChoice.TryRead(node, out _, out _, out _, out _))
                {
                    ScopedChoice.TryObserve(node, Evaluate, out var selected);
                    return memo[node] = selected;
                }
                var plan = plans.GetValue(node, static n => new Plan(n));
                if (!plan.Current())
                {
                    plans.Remove(node);
                    plan = plans.GetValue(node, static n => new Plan(n));
                }
                // Open/cyclic/unfamiliar recipes retain the general reader's
                // unresolved contract instead of acquiring a zero fast path.
                if (!plan.Complete) return memo[node] = NativeMath.Read(node);

                var upstream = new Relation[plan.Sources.Length];
                var ticks = new TickRef[plan.Targets.Length];
                for (var i = 0; i < upstream.Length; i++)
                {
                    upstream[i] = Evaluate(plan.Sources[i]);
                    if (!Equation.TryReadPrediction(upstream[i], out var result, out _) || result.Carrier.IsUnknown ||
                        !result.Carrier.SameIdentityAs(plan.Targets[i].Carrier))
                        return memo[node] = Failed();
                    ticks[i] = new TickRef(result.Ticks);
                    for (var j = 0; j < i; j++)
                    {
                        if (!ReferenceEquals(plan.Targets[j].Tick, plan.Targets[i].Tick)) continue;
                        if (ticks[j].Ticks != result.Ticks) return memo[node] = Failed();
                        ticks[i] = ticks[j];
                    }
                }
                var copied = new Dictionary<Layout, IElement>(ReferenceEqualityComparer.Instance);
                IElement Copy(Layout layout, bool capture)
                {
                    if (!capture && !layout.Bound) return layout.Source;
                    if (layout.Source is CarrierLocation site)
                    {
                        var tick = layout.Binding >= 0 ? ticks[layout.Binding] : capture ? Observe(site.Tick) : site.Tick;
                        return ReferenceEquals(tick, site.Tick) ? site : new CarrierLocation(site.Carrier, tick);
                    }
                    if (layout.Source is TickRef reference)
                        return layout.Binding >= 0 ? ticks[layout.Binding] : capture ? Observe(reference) : reference;
                    if (layout.Source is not Relation relation) return layout.Source;
                    if (copied.TryGetValue(layout, out var prior)) return prior;
                    var left = Copy(layout.Left!, capture);
                    var right = Copy(layout.Right!, capture);
                    var copy = ReferenceEquals(left, relation.Left) && ReferenceEquals(right, relation.Right)
                        ? relation : new Relation(left, right);
                    copied.Add(layout, copy);
                    return copy;
                }
                var equation = (Relation)Copy(plan.Equation!, false);
                // Input and equation may alias. Capturing input must not reuse
                // an unobserved equation copy from the previous pass.
                copied.Clear();
                var input = Copy(plan.Input!, true);
                if (equation.Left is Relation { Left: CarrierLocation start, Right: CarrierLocation end } &&
                    start.Carrier.SameIdentityAs(end.Carrier) && start.Ticks == end.Ticks)
                    return memo[node] = Failed();
                // These recipes expose direct operations or affine frames.
                // Broader equation families stay on the general native path.
                Relation prediction;
                var accepted = equation.TryReadOperationFrom(input, plan.Output!, out prediction) ||
                    input is CarrierLocation scalar && equation.TryProject(scalar, plan.Output!, out prediction);
                if (!accepted) accepted = Equation.TryPredict(equation, input, plan.Output!, out prediction);
                if (!accepted || !Equation.TryReadPrediction(prediction, out var calculated, out var residue))
                    return memo[node] = Failed();
                foreach (var read in upstream) residue = new Relation(residue, read.Right);
                return memo[node] = new Relation(new CarrierLocation(calculated.Carrier, calculated.Ticks), residue);

                Relation Failed()
                {
                    var failed = Relation.UnresolvedRead(plan.Input!.Source, plan.Equation!.Source, plan.Output!);
                    return new Relation(failed.Left, Native.G(failed.Right,
                        Native.G(upstream.Where(r => r is not null).Cast<IElement>().ToArray())));
                }
            }
            finally { active.Remove(node); }
        }

        return roots.Select(Evaluate).ToArray();
    }

    private sealed class Layout(IElement source)
    {
        public IElement Source { get; } = source;
        public Layout? Left;
        public Layout? Right;
        public int Binding = -1;
        public bool Bound;
    }

    private sealed class Plan
    {
        public bool Complete { get; private set; }
        public Carrier? Output { get; }
        public Layout? Equation { get; }
        public Layout? Input { get; }
        public CarrierLocation[] Targets { get; } = [];
        public IElement[] Sources { get; } = [];
        private readonly List<(Relation Node, IElement Left, IElement Right)> edges = [];

        public Plan(Relation recipe)
        {
            var visited = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            void Shape(IElement value)
            {
                if (value is not Relation r || !visited.Add(r)) return;
                edges.Add((r, r.Left, r.Right));
                Shape(r.Left); Shape(r.Right);
            }
            var parts = NativeMath.Parts(recipe);
            if (parts.Length != 4 || parts[0] is not Relation equation || parts[2] is not Carrier output ||
                parts[3] is not Relation bindings) return;
            var bound = NativeMath.Parts(bindings);
            if (bound.Any(b => b is not Relation { Left: CarrierLocation })) return;
            Targets = bound.Cast<Relation>().Select(b => (CarrierLocation)b.Left).ToArray();
            Sources = bound.Cast<Relation>().Select(b => b.Right).ToArray();
            if (Targets.Any(t => t.Carrier.IsUnknown)) return;
            Output = output;
            // Capture group topology without following upstream source recipes.
            void GroupShape(IElement value)
            {
                if (value is not Relation r || !visited.Add(r)) return;
                edges.Add((r, r.Left, r.Right));
                if (bound.Contains(r)) { Shape(r.Left); return; }
                if (ReferenceEquals(r, equation) || ReferenceEquals(r, parts[1])) { Shape(r.Left); Shape(r.Right); return; }
                GroupShape(r.Left); GroupShape(r.Right);
            }
            GroupShape(recipe);
            Shape(equation); Shape(parts[1]);
            var layouts = new Dictionary<IElement, Layout>(ReferenceEqualityComparer.Instance);
            var building = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            var cycle = false;
            Layout Build(IElement value)
            {
                if (building.Contains(value)) { cycle = true; return new Layout(new Placeholder()); }
                if (layouts.TryGetValue(value, out var existing)) return existing;
                var layout = new Layout(value);
                layouts.Add(value, layout);
                building.Add(value);
                var tick = value is CarrierLocation site ? site.Tick : value as TickRef;
                if (tick is not null) layout.Binding = Array.FindIndex(Targets, t => ReferenceEquals(t.Tick, tick));
                if (value is Relation r)
                {
                    layout.Left = Build(r.Left); layout.Right = Build(r.Right);
                    layout.Bound = layout.Left.Bound || layout.Right.Bound;
                }
                else layout.Bound = layout.Binding >= 0;
                building.Remove(value);
                return layout;
            }
            Equation = Build(equation); Input = Build(parts[1]);
            Complete = !cycle;
        }

        public bool Current()
        {
            foreach (var edge in edges)
                if (!ReferenceEquals(edge.Node.Left, edge.Left) || !ReferenceEquals(edge.Node.Right, edge.Right)) return false;
            return true;
        }
    }
}
