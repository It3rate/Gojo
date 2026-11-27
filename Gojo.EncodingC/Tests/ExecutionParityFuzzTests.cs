using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Random networks run on every execution path exactly as the engine
/// runs them (Gojo/docs/GojoOperationsSynthesis-2026-11.md, pass 4). The
/// hand-built gates pin the cases someone thought of; this one looks for the
/// rest. Each seed builds the same network twice on one store, from parts
/// drawn at random:
/// - ladders narrowing in every direction, with open, partial, measured and
///   contradictory digits;
/// - concrete ripple columns;
/// - conversions, with rows on one position or across positions;
/// - small random tables over shared fields;
/// - laws that recruit and retire positions within a step;
/// - reads and moves through ports and sockets;
/// - a move that rewrites another wiring's row through a port;
/// - random relations over open values, some reading one value in two roles.
/// One copy steps on the engine and the other on the path, in declared order
/// and taken together. Between rounds both are disturbed alike: re-observed,
/// corrupted, rebound or retired. Every step must match in state and in
/// evidence: the waves, and each row's changed, unread, unadmitted,
/// ambiguous, skipped, aliased, contested and noted bits. Neither ever
/// throws: a running structure turns what cannot happen into evidence.</summary>
public sealed class ExecutionParityFuzzTests
{
    private const int Seeds = 48;
    private const int Rounds = 8;

    private sealed record Network(Engine Engine, Store Sites, Wiring[] Wirings, Action<Random> Disturb);

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath), DisableDiscoveryEnumeration = true)]
    public void RandomNetworksRunAsTheEngineRunsThem(ExecutionConformancePath path)
    {
        for (var seed = 0; seed < Seeds; seed++)
            foreach (var together in new[] { false, true })
                Compare(path, seed, together);
    }

    private static void Compare(ExecutionConformancePath path, int seed, bool together)
    {
        var expected = Build(seed);
        var actual = Build(seed);
        var choices = new Random(~seed);
        for (var round = 0; round < Rounds; round++)
        {
            var where = $"seed {seed}, round {round}, {(together ? "together" : "in order")}";
            var stepping = choices.Next(2) == 0;
            var limit = choices.Next(1, 6);
            // A running structure never throws (GojoSystemPrompt §9.3): what
            // cannot happen is evidence, on the engine and on every path.
            EngineStep? step = null, followedStep = null;
            int? settled = null, followed = null;
            var refused = Record.Exception(() => { if (stepping) step = together ? expected.Engine.StepTogether() : expected.Engine.Step(); else settled = expected.Engine.Settle(limit, together); });
            Assert.True(refused is null, $"{where}: the engine threw {refused?.GetType().Name}: {refused?.Message}");
            refused = Record.Exception(() => { if (stepping) followedStep = path.Step(actual.Engine, together); else followed = path.Settle(actual.Engine, limit, together); });
            Assert.True(refused is null, $"{where}: the path threw {refused?.GetType().Name}: {refused?.Message}");
            if (stepping)
            {
                var difference = StepDifference(step!, followedStep!);
                Assert.True(difference is null, $"{where}: {difference}");
            }
            else
                Assert.True(settled == followed && expected.Engine.Tick == actual.Engine.Tick,
                    $"{where}: settled in {settled} and {followed}, at ticks {expected.Engine.Tick} and {actual.Engine.Tick}");
            var state = StateDifference(expected, actual);
            Assert.True(state is null, $"{where}: {state}");
            var disturbance = choices.Next();
            expected.Disturb(new Random(disturbance));
            actual.Disturb(new Random(disturbance));
        }
    }

    private static string? StepDifference(EngineStep expected, EngineStep actual)
    {
        if (expected.Tick != actual.Tick) return "tick";
        if (expected.Waves.Count != actual.Waves.Count || !expected.Waves.Zip(actual.Waves).All(w => w.First.SequenceEqual(w.Second))) return "engine waves";
        if (expected.Steps.Count != actual.Steps.Count) return "wiring steps";
        for (var i = 0; i < expected.Steps.Count; i++)
        {
            WiringStep e = expected.Steps[i], a = actual.Steps[i];
            var field = !e.Changed.Equals(a.Changed) ? "changed" : !e.Unread.Equals(a.Unread) ? "unread"
                : !e.Unadmitted.Equals(a.Unadmitted) ? "unadmitted" : !e.Ambiguous.Equals(a.Ambiguous) ? "ambiguous"
                : !e.Skipped.Equals(a.Skipped) ? "skipped" : !e.Aliased.Equals(a.Aliased) ? "aliased"
                : !Same(e.Contested, a.Contested) ? "contested" : !Same(e.Noted, a.Noted) ? "noted"
                : !e.Before.SequenceEqual(a.Before) ? "before" : !e.After.SequenceEqual(a.After) ? "after"
                : !e.Waves.SequenceEqual(a.Waves) ? "row waves" : null;
            if (field is not null) return $"wiring {i}: {field}";
        }
        return null;
    }

    private static bool Same(Plane? x, Plane? y) => x is null ? y is null : y is not null && x.Equals(y);

    private static string? StateDifference(Network expected, Network actual)
    {
        if (StoreDifference(expected.Sites, actual.Sites) is { } sites) return $"sites: {sites}";
        for (var w = 0; w < expected.Wirings.Length; w++)
            if (StoreDifference(expected.Wirings[w].Rows, actual.Wirings[w].Rows) is { } rows) return $"rows of wiring {w}: {rows}";
        byte[]? saved = Saved(expected.Engine), loaded = Saved(actual.Engine);
        return saved is null == loaded is null && (saved is null || saved.AsSpan().SequenceEqual(loaded)) ? null : "saved bytes";
    }

    private static string? StoreDifference(Store x, Store y)
    {
        if (x.Count != y.Count || x.PlaneCount != y.PlaneCount) return "size";
        for (var i = 0; i < x.Count; i++)
        {
            if (x.IsLive(i) != y.IsLive(i) || x.IsSpare(i) != y.IsSpare(i)) return $"position {i} life";
            if (!x.IsLive(i)) continue;
            for (var p = 0; p < x.PlaneCount; p++)
                if (x.Read(p, i) != y.Read(p, i)) return $"plane {p} at position {i}";
        }
        return null;
    }

    private static byte[]? Saved(Engine engine)
    {
        try { return Transport.Save(engine); }
        catch (InvalidOperationException) { return null; }
        catch (ArgumentException) { return null; }
    }

    private static Network Build(int seed)
    {
        var rng = new Random(seed);
        var lanes = rng.Next(2, 40);
        var sites = new Store(0, lanes);
        if (rng.Next(2) == 0) sites.AddSpare(rng.Next(1, 4));
        var live = sites.Live;
        int[] Positions(int count, bool spares) => [.. Enumerable.Range(0, count).Select(_ => spares ? rng.Next(sites.Count) : live.Indexes().ElementAt(rng.Next(live.Count)))];
        var wirings = new List<Wiring>();
        var fields = new List<Field>();
        var disturbances = new List<Action<Random>>();

        Field Count(Shape shape)
        {
            var field = new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => sites.AddPlane())]);
            foreach (var p in live.Indexes()) field.Write(sites, p, rng.Next(shape.Size));
            fields.Add(field);
            return field;
        }

        TransitionLaw Table(Shape[] shapes, int keep)
        {
            var table = rng.Next();
            return TransitionLaw.FromFunction(shapes, s =>
            {
                var h = new Random(s.Aggregate(table, (acc, v) => unchecked(acc * 31 + v)));
                return h.Next(3) == 0 ? s : [.. shapes.Select((shape, r) => r < keep ? s[r] : h.Next(shape.Size))];
            }, new Accounting(true, [.. Enumerable.Range(0, keep).Select(r => (Invariant)new Unchanged(r))]));
        }

        void Rows(Wiring wiring, bool across)
        {
            if (!across) foreach (var p in live.Indexes()) wiring.Add([.. Enumerable.Repeat(p, wiring.Bindings.Count)]);
            else for (var row = rng.Next(1, 6); row > 0; row--) wiring.Add(Positions(wiring.Bindings.Count, spares: true));
        }

        for (var part = rng.Next(2, 7); part > 0; part--)
        {
            var added = new List<Wiring>();
            try
            {
                switch (rng.Next(8))
                {
                    case 0:
                    {
                        var rungs = rng.Next(1, 3);
                        int[] radices = [.. Enumerable.Range(0, rungs).Select(_ => rng.Next(5) == 0 ? rng.Next(17, 25) : rng.Next(2, 11))];
                        var binary = rng.Next(2) == 0;
                        int[] top = [.. radices];
                        if (rng.Next(2) == 0) top[^1] = 2 * radices[^1] - 1;
                        Ladder x = new(sites, radices, binary), y = new(sites, radices, binary), z = new(sites, top, binary);
                        var (columns, carries) = Ladder.Add(x, y, z, live);
                        void Observe(Random r, bool again)
                        {
                            foreach (var p in live.Indexes())
                            {
                                if (!sites.IsLive(p) || (again && r.Next(3) != 0)) continue;
                                if (again) Ladder.ReopenRows(columns.Select(c => (c, p)));
                                foreach (var ladder in new[] { x, y, z })
                                    switch (r.Next(5))
                                    {
                                        case 0: ladder.Restrict(p, r.NextInt64(ladder.Capacity + 1)); break;
                                        case 1:
                                            var rung = r.Next(ladder.Radices.Count);
                                            ladder.Restrict(p, rung, (ulong)r.NextInt64() & ((1UL << ladder.Radices[rung]) - 1));
                                            break;
                                        case 2:
                                            var measured = r.Next(ladder.Radices.Count);
                                            ladder.Measure(p, r.NextInt64((ladder.Capacity + 1) / ladder.PlaceValue(measured)), measured);
                                            break;
                                    }
                                if (r.Next(4) == 0) carries[^1].Restrict(p, (ulong)r.Next(1, 4));
                            }
                        }
                        Observe(rng, again: false);
                        disturbances.Add(r => Observe(r, again: true));
                        added.AddRange(columns);
                        break;
                    }
                    case 1:
                    {
                        var binary = rng.Next(2) == 0;
                        var carry = Count(Numbers.Carry);
                        for (var rung = rng.Next(1, 3); rung > 0; rung--)
                        {
                            var radix = rng.Next(2, 7);
                            var d = Numbers.Digit(radix, binary);
                            Field a = Count(d), b = Count(d), sum = Count(d), next = Count(Numbers.Carry);
                            var law = Operations.Bind(Numbers.DigitSum(radix, binary).Relation, [d, d, Numbers.Carry, Numbers.Carry, d], [0, 1, 2, -1, -1], [-1, -1, -1, 3, 4]);
                            added.Add(Tension.Wire(law, [a.On(sites), b.On(sites), carry.On(sites), next.On(sites), sum.On(sites)], live));
                            carry = next;
                        }
                        break;
                    }
                    case 2:
                    {
                        int from = rng.Next(1, 6), to = rng.Next(1, 6);
                        Field source = Count(Shape.Unary(from)), target = Count(Shape.Unary(to));
                        var move = new Wiring(Flows.Convert(rng.Next(1, 4), rng.Next(1, 4), from, to), source.On(sites), target.On(sites));
                        Rows(move, across: rng.Next(2) == 0);
                        added.Add(move);
                        break;
                    }
                    case 3:
                    {
                        if (fields.Count == 0) Count(rng.Next(2) == 0 ? Shape.Unary(rng.Next(1, 4)) : Shape.Binary(rng.Next(2, 5)));
                        var roles = fields.OrderBy(_ => rng.Next()).Take(rng.Next(1, 4)).ToArray();
                        var table = new Wiring(Table([.. roles.Select(f => f.Shape)], keep: 0), [.. roles.Select(f => f.On(sites))]);
                        Rows(table, across: rng.Next(3) == 0);
                        added.Add(table);
                        break;
                    }
                    case 4:
                    {
                        var kind = rng.Next(4);
                        var life = new Wiring(TransitionLaw.FromFunction([Shape.Bit], v => [kind == 0 ? 1 : kind == 1 ? 0 : kind == 2 ? 1 - v[0] : v[0]], new Accounting(true)),
                            new Binding(sites.Life, 0));
                        foreach (var p in Positions(rng.Next(1, 3), spares: true)) life.Add(p);
                        added.Add(life);
                        break;
                    }
                    case 5:
                    {
                        var cap = rng.Next(1, 4);
                        Field published = Count(Shape.Unary(cap)), local = Count(Shape.Unary(cap));
                        var port = sites.Publish(Positions(rng.Next(1, 4), spares: false), [.. published.Planes]);
                        Surface surface = port;
                        if (rng.Next(2) == 0)
                        {
                            var socket = Socket.For(port);
                            socket.Plug(port);
                            surface = socket;
                        }
                        var remote = new Field(published.Shape, [.. Enumerable.Range(0, published.Planes.Count)]).On(surface);
                        var wiring = rng.Next(2) == 0
                            ? new Wiring(Flows.Convert(1, 1, cap, cap), remote, local.On(sites))
                            : new Wiring(Table([published.Shape, local.Shape], keep: 1), remote, local.On(sites));
                        for (var row = rng.Next(1, 4); row > 0; row--) wiring.Add(rng.Next(port.Count), Positions(1, spares: false)[0]);
                        disturbances.Add(r =>
                        {
                            if (r.Next(3) != 0) return;
                            var p = live.Indexes().ElementAt(r.Next(live.Count));
                            if (!sites.IsLive(p)) return;
                            var lane = r.Next(port.Count);
                            try { sites.Rebind(port, lane, p); }
                            catch (ArgumentException) { } // one taker per position: a refused rebind is refused in both copies
                        });
                        added.Add(wiring);
                        break;
                    }
                    case 6:
                    {
                        var target = wirings.LastOrDefault(w => w.Live.Count > 0);
                        if (target is null) break;
                        var row = target.Live.Indexes().First();
                        var role = rng.Next(target.Bindings.Count);
                        var planes = target.ReferencePlanes(role);
                        if (planes.Count == 0) break;
                        var port = target.Rows.Publish([row], [planes[rng.Next(planes.Count)]]);
                        var source = Count(Shape.Unary(1));
                        var rewire = new Wiring(Flows.Convert(1, 1, 1, 1), source.On(sites), new Binding(port, 0));
                        rewire.Add(Positions(1, spares: false)[0], 0);
                        added.Add(rewire);
                        break;
                    }
                    case 7:
                    {
                        // A random relation over open values; one value may fill two roles (a diagonal).
                        var values = Enumerable.Range(0, rng.Next(1, 4)).Select(_ =>
                        {
                            var shape = rng.Next(2) == 0 ? Shape.Unary(rng.Next(1, 5)) : Shape.Binary(rng.Next(2, 9));
                            return Supports.On(sites, new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => sites.AddPlane())]));
                        }).ToArray();
                        var roles = Enumerable.Range(0, rng.Next(2, 5)).Select(_ => values[rng.Next(values.Length)]).ToArray();
                        var table = rng.Next();
                        var density = rng.Next(1, 4);
                        var relation = RelationLaw.FromPredicate([.. roles.Select(v => v.Field.Shape)],
                            t => new Random(t.Aggregate(table, (acc, v) => unchecked(acc * 31 + v))).Next(4) < density);
                        var wiring = new Wiring(relation, [.. roles.Select(v => v.Variable)]);
                        foreach (var p in live.Indexes()) wiring.Add([.. Enumerable.Repeat(p, roles.Length)]);
                        void Narrow(Random r, bool again)
                        {
                            foreach (var value in values)
                                foreach (var p in live.Indexes())
                                {
                                    if (!sites.IsLive(p) || r.Next(again ? 4 : 2) != 0) continue;
                                    if (again && r.Next(2) == 0) value.Forget(p);
                                    else value.Restrict(p, (ulong)r.NextInt64() & (value.Field.Shape.Size == 64 ? ulong.MaxValue : (1UL << value.Field.Shape.Size) - 1));
                                }
                        }
                        Narrow(rng, again: false);
                        disturbances.Add(r => Narrow(r, again: true));
                        added.Add(wiring);
                        break;
                    }
                }
            }
            catch (Exception refused) when (refused is ArgumentException or InvalidOperationException)
            {
                // A part the substrate refuses to build is left out, alike in both copies.
                continue;
            }
            wirings.AddRange(added);
        }

        var engine = new Engine();
        foreach (var wiring in wirings.OrderBy(_ => rng.Next())) engine.Add(wiring);
        void Disturb(Random r)
        {
            foreach (var disturbance in disturbances) disturbance(r);
            foreach (var p in live.Indexes())
                for (var plane = 0; plane < sites.PlaneCount; plane++)
                    if (sites.IsLive(p) && !sites.IsConstantPlane(plane) && r.Next(64) == 0) sites.Write(plane, p, r.Next(2) == 0);
            if (r.Next(6) == 0 && live.Indexes().ElementAt(r.Next(live.Count)) is var gone && sites.IsLive(gone) && sites.Live.Count > 1) sites.Retire(gone);
        }
        return new Network(engine, sites, [.. wirings], Disturb);
    }
}
