using Gojo.Core;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>Local handles into native support, return, work and price equations.</summary>
public sealed class Utility
{
    public const int ExplorationAllowance = 24;
    public Carrier Visits { get; }
    public Carrier Returns { get; }
    public Carrier Work { get; }
    public Carrier Duration { get; }
    public Carrier RecentReturn { get; }
    public Relation Bid { get; }
    public Relation Root => G(Visits, Returns, Work, Duration, Bid, RecentReturn);

    public Utility(Units units)
    {
        Visits = new Carrier(units.Count.UnitShape);
        Returns = new Carrier(units.Return.UnitShape);
        Work = new Carrier(units.Work.UnitShape);
        Duration = new Carrier(units.Clock.UnitShape);
        RecentReturn = new Carrier(units.Return.UnitShape);
        var support = Sum((Visits.ValueSlot, 1), (L(Visits, 1), 1));
        var mean = Divide(Signed(Returns), support, new Carrier(units.Return.UnitShape));
        var novelty = Divide(L(units.Return, ExplorationAllowance), support, new Carrier(units.Return.UnitShape));
        var cost = Scale(Divide(Work.ValueSlot, support, new Carrier(units.Work.UnitShape)),
            Computation.CaloriesPerRuntimeSecond, System.Diagnostics.Stopwatch.Frequency, new Carrier(units.Return.UnitShape));
        Bid = SumTo(new Carrier(units.Return.UnitShape), (mean, 1), (Signed(RecentReturn), 1), (novelty, 1), (cost, -1));
    }

    public void Record(CarrierLocation returned, long duration, long work)
    {
        Carrier.ReceiveTogether(G(L(Visits, 1), L(Returns, returned.Ticks),
            L(Duration, Math.Max(1, duration)), L(Work, Math.Max(0, work)), L(RecentReturn, returned.Ticks - Held(RecentReturn))));
    }
}

/// <summary>Source, signed coefficient and operation remain connected without this reader.</summary>
public sealed class Component
{
    public IElement Source { get; }
    public Carrier Coefficient { get; }
    public Relation Output { get; }
    public Utility Utility { get; }
    public Relation Root { get; }

    public Component(Units units, IElement source, long coefficient)
    {
        Source = source;
        Coefficient = new Carrier(units.Force.UnitShape);
        Aim(Coefficient, coefficient);
        Output = Scale(Product(source, Signed(Coefficient)), 1, 15, new Carrier(units.Force.UnitShape));
        Utility = new Utility(units);
        Root = G(source, Coefficient, Output, Utility.Root);
    }

    public Component Copy(Units units, IElement? source = null, long? coefficient = null) =>
        new(units, source ?? Source, coefficient ?? Held(Coefficient));
}

/// <summary>Finite connected processing structure, with measured local trial support.</summary>
public sealed class Circuit
{
    public const int MaximumComponents = 4;
    public Component[] Components { get; }
    public Relation Output { get; }
    public Utility Utility { get; }
    public Relation Root { get; }
    public Circuit(Units units, params Component[] components)
    {
        Components = components;
        Output = SumTo(new Carrier(units.Force.UnitShape), components.Select(c => ((IElement)c.Output, 1L)).ToArray());
        Utility = new Utility(units);
        Root = G(G(components.Select(c => (IElement)c.Root).ToArray()), Output, Utility.Root);
    }

    public Relation Record(Units units, Relation relief, long duration, long work)
    {
        if (relief.Left is not CarrierLocation returned || Components.Length == 0) return G(relief, new Placeholder());
        Utility.Record(returned, duration, work);
        var share = Read(Scale(returned, 1, Components.Length, new Carrier(units.Return.UnitShape)));
        var sharedWork = Read(Scale(L(Utility.Work, work), 1, Components.Length, new Carrier(units.Work.UnitShape)));
        if (share.Left is CarrierLocation part && sharedWork.Left is CarrierLocation workPart)
            foreach (var component in Components) component.Utility.Record(part, duration, workPart.Ticks);
        // One common outcome, with an explicit attribution calibration and remainder.
        return G(relief, share, sharedWork, Utility.Root, G(Components.Select(c => (IElement)c.Utility.Root).ToArray()));
    }
}

public static class CircuitMarket
{
    public static Circuit Propose(Units units, Circuit current, IElement[] sources, Carrier cursor, out Relation correspondence)
    {
        var position = (long)cursor.Value;
        var terms = current.Components.Select(c => c.Copy(units)).ToList();
        if (terms.Count == 0) terms.Add(new Component(units, sources[0], 0));
        var selected = (int)(position / 4 % terms.Count);
        var source = sources[1 + (int)(position % (sources.Length - 1))];
        var catalog = G(sources);
        if (position >= 6 && position % 6 != 3 && position % 6 != 4)
        {
            var available = sources.Concat(current.Components.Select(c => c.Source)).Distinct().ToArray();
            var a = available[(int)(position / 3 % available.Length)];
            var b = available[(int)((position / 2 + 1) % available.Length)];
            Func<IElement, IElement, Relation>[] builders = [
                (x,y) => ExpressionGraph.Add(units,x,y), (x,y) => ExpressionGraph.Subtract(units,x,y),
                (x,y) => ExpressionGraph.Multiply(units,x,y), (x,y) => ExpressionGraph.Quotient(units,x,y),
                (x,y) => ExpressionGraph.Less(units,x,y), (x,y) => ExpressionGraph.Greater(units,x,y),
                (x,y) => ExpressionGraph.Equal(units,x,y),
                (x,y) => ExpressionGraph.And(units,ExpressionGraph.Truth(units,x),ExpressionGraph.Truth(units,y)),
                (x,y) => ExpressionGraph.Or(units,ExpressionGraph.Truth(units,x),ExpressionGraph.Truth(units,y)),
                (x,y) => ExpressionGraph.Xor(units,ExpressionGraph.Truth(units,x),ExpressionGraph.Truth(units,y)),
                (x,y) => ExpressionGraph.Not(units,ExpressionGraph.Truth(units,x)),
                (x,y) => ExpressionGraph.Choose(units,ExpressionGraph.Greater(units,x,y),
                    available[(int)((position / 5 + 2) % available.Length)], available[(int)((position / 7 + 3) % available.Length)])];
            var expression = builders[(position / 3 + position % 3 * 7) % builders.Length](a, b);
            if (ExpressionGraph.Depth(catalog, expression) <= ExpressionGraph.MaximumDepth &&
                ExpressionGraph.Count(catalog, terms.Select(t => t.Source).Append(expression).ToArray()) <= ExpressionGraph.MaximumOperations)
                source = expression;
        }
        // A bounded deterministic proposal order. The rewritten relations carry
        // the actual operation; these branches are construction, not operation tags.
        if (position % 6 < 3 && terms.Count < Circuit.MaximumComponents)
            terms.Add(new Component(units, source, position % 2 == 0 ? 6 : -6));
        else if (position % 6 == 3 && terms.Count > 1)
            terms.RemoveAt(1 + (int)(position / 4 % (terms.Count - 1)));
        else if (position % 6 == 4)
        {
            var coefficients = new long[] { -12, -6, 0, 6, 12 };
            terms[selected] = new Component(units, terms[selected].Source, coefficients[(position / 4 + selected) % coefficients.Length]);
        }
        else
        {
            var inputs = ExpressionGraph.Inputs(terms[selected].Source);
            // Pruning can remove an intermediate stage as well as a whole term.
            if (position % 12 == 5 && inputs.Length != 0) source = inputs[(int)(position / 12 % inputs.Length)];
            terms[selected] = new Component(units, source, -Held(terms[selected].Coefficient));
        }
        cursor.Add(1);
        var proposal = new Circuit(units, terms.ToArray());
        correspondence = G(new Relation(current.Root, proposal.Root),
            G(current.Components.Select(c => (IElement)c.Root).ToArray()), G(terms.Select(c => (IElement)c.Root).ToArray()));
        return proposal;
    }

    public static Circuit? Select(Units units, IReadOnlyList<Circuit> candidates, Carrier wallet, Carrier spent,
        long time, out Relation auction)
    {
        var quotes = new List<IElement>();
        var comparisons = new List<IElement>();
        Circuit? winner = null;
        CarrierLocation winningBid = default;
        foreach (var candidate in candidates)
        {
            if (wallet.Value == 0) break;
            var payment = Transfer(wallet, spent, 1);
            var read = ReadMany(candidate.Utility.Bid)[0];
            quotes.Add(G(candidate.Root, read, payment));
            if (!TryValue(read, out _)) continue;
            var bid = (CarrierLocation)read.Left;
            if (winner is not null)
            {
                // First supply wins exact ties; every strict comparison is witnessed.
                var better = AtLeast(bid, L(winningBid.Carrier, checked(winningBid.Ticks + 1)), out var claim);
                comparisons.Add(claim);
                if (!better) continue;
            }
            winner = candidate; winningBid = bid;
        }
        auction = G(L(units.Clock, time), G(quotes.ToArray()), G(comparisons.ToArray()),
            winner is null ? new Placeholder() : new Relation(winner.Root, winningBid));
        return winner;
    }

    public static Circuit Mate(Units units, Muscle a, Muscle b, IElement[] parentA, IElement[] parentB,
        IElement[] childSources, Carrier wallet, Carrier spent, long time, out Relation auction, out Relation inheritance)
    {
        var supplies = a.Current.Components.Concat(b.Current.Components).ToArray();
        var quotes = new List<IElement>();
        var comparisons = new List<IElement>();
        var selected = new List<(Component Source, IElement Target, CarrierLocation Bid)>();
        foreach (var supply in supplies)
        {
            if (wallet.Value == 0) break;
            var parentSources = a.Current.Components.Contains(supply) ? parentA : parentB;
            var target = ExpressionGraph.Rebind(supply.Source, parentSources, childSources);
            if (target is Placeholder || ExpressionGraph.Depth(G(childSources), target) > ExpressionGraph.MaximumDepth)
            { quotes.Add(G(supply.Root, target)); continue; }
            var payment = Transfer(wallet, spent, 1);
            var read = ReadMany(supply.Utility.Bid)[0];
            quotes.Add(G(supply.Root, read, payment));
            if (!TryValue(read, out _)) continue;
            var bid = (CarrierLocation)read.Left;
            var port = CircuitInspection.SourceIndex(G(childSources), target);
            var same = selected.FindIndex(s => ReferenceEquals(s.Target, target) ||
                port >= 0 && CircuitInspection.SourceIndex(G(childSources), s.Target) == port);
            if (same >= 0)
            {
                var better = AtLeast(bid, L(selected[same].Bid.Carrier, selected[same].Bid.Ticks + 1), out var claim);
                comparisons.Add(claim);
                if (!better) continue;
                selected.RemoveAt(same);
            }
            selected.Add((supply, target, bid));
        }
        // Local native comparisons order excess supplies. No random crossover.
        var ordered = new List<(Component Source, IElement Target, CarrierLocation Bid)>();
        foreach (var supply in selected)
        {
            var insertion = ordered.Count;
            for (var i = 0; i < ordered.Count; i++)
            {
                if (!AtLeast(supply.Bid, L(ordered[i].Bid.Carrier, ordered[i].Bid.Ticks + 1), out var claim))
                { comparisons.Add(claim); continue; }
                comparisons.Add(claim); insertion = i; break;
            }
            ordered.Insert(insertion, supply);
        }
        var retainedList = new List<(Component Source, IElement Target, CarrierLocation Bid)>();
        var childCatalog = G(childSources);
        foreach (var item in ordered)
        {
            if (retainedList.Count == Circuit.MaximumComponents ||
                ExpressionGraph.Count(childCatalog, retainedList.Select(s => s.Target).Append(item.Target).ToArray()) > ExpressionGraph.MaximumOperations)
            { comparisons.Add(G(item.Source.Root, item.Target, new Placeholder())); continue; }
            retainedList.Add(item);
        }
        var retained = retainedList.ToArray();
        var components = retained.Select(s => s.Source.Copy(units, s.Target)).ToArray();
        // Unfunded or incompatible mating exposes its open auction; it supplies
        // an inert circuit rather than manufacturing an inherited component.
        var child = new Circuit(units, components);
        inheritance = G(retained.Select((s, i) => (IElement)G(new Relation(s.Source.Source, s.Target),
            new Relation(L(s.Source.Coefficient, Held(s.Source.Coefficient)), L(components[i].Coefficient, Held(components[i].Coefficient))),
            s.Bid)).ToArray());
        auction = G(L(units.Clock, time), G(quotes.ToArray()), G(comparisons.ToArray()), child.Root, inheritance);
        return child;
    }
}
