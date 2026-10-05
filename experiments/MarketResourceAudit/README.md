# Negotiated-port resource audit

This is a read-only diagnostic of the frozen `NegotiatedPorts` policy, with
short paired continuations of independently restored worlds. It changes no
market rule, cost, population, input sequence, or shared core implementation.
The observer records never enter a learner. The original results are read but
never overwritten.

The distinction being measured is structural:

```text
world = G(operational, pending promises, latest diagnostic frame, version)
continuation roots = (operational, pending promises, version)
forgotten-frame world = G(operational, pending promises, open slot, version)
```

The pending promises are still needed to evaluate predictions at the next
arrival. Forgetting the latest completed diagnostic frame does not forget those
promises, mutate operational structure, withdraw live routes, or reset wallets.
The open slot explicitly makes the omitted diagnostic history unavailable.

## First measured result

[Full findings](results/resource-audit.json) include every measurement and its
methodology. In the main saved world:

| Selected graph | Carriers | Relations | Native encoded bytes |
|---|---:|---:|---:|
| Full latest world | 694 | 7,913 | 290,272 |
| Operational structure alone | 171 | 2,182 | Not measured |
| Operational + pending + version roots | 192 | 2,431 | 84,128 |
| Runnable envelope with frame omitted | 193 | 2,441 | 84,446 |

The runnable envelope is approximately **72% smaller in carrier count, 69%
smaller in relation count, and 71% smaller in encoded bytes**. All four final
worlds showed a similar split. The operational graph and diagnostic frame share
structure, so their standalone counts must not be added. These are graph counts
and packet sizes, not retained managed-heap measurements.

For each of the four worlds, the audit restored two independent copies and
forgot the frame in one of them after every subsequent admission. It compared
their complete continuation graphs (normalizing process-local IDs only), plus
financial and prediction reads. All **96 paired arrivals matched**, covering
changing increments, flatness, falling values, missing input, a jump, demand
withdrawal, and scarce funding. This establishes the bounded continuation
fixture, not a proof over every possible future input.

## Computation and allocation

One warmup per trial was followed by three recorded Release runs, alternating
trial order. These are local diagnostics on .NET 10.0.12/Windows, not controlled
performance benchmarks. Each run used 110 arrivals:

| Stage | Measured scope | Result across the four trials |
|---|---|---|
| Admission | Existing `TryObserve`, including native evidence construction | 253–293 MB allocated; 280–390 ms per run |
| Compact local reads | Wallets, prices, current ports/routes, pending promises and prior outcomes | Mean 18.3–19.0 MB and 22.6–32.8 ms per run |
| Compact JSON | One observer row per arrival, immediately discarded | About 0.184 MB allocated and mean 2.7–3.0 ms per run |
| Full native checkpoint | Capture and encode once after the run | Mean 8.25–8.92 MB allocated; 6–44 ms across individual samples |
| Continuation checkpoint | Capture and encode the three continuation roots | Mean about 2.46 MB allocated; mean 1.8–3.1 ms |

MB here is decimal and denotes **cumulative allocation**, not simultaneous
memory use. Stage timings exclude construction of the initial market, forced
collection between runs, graph comparisons, and disk writes. The compact
inspector is not the older full replay/viewer inspector, so this is not an
exact accounting of that viewer's cost. All three final continuation signatures
were identical within each trial. A further observer-only read also left each
continuation graph unchanged.

The important limitation is that **admission already creates the diagnostic
frame**. Dropping that frame afterward reduces what remains reachable and what
must be checkpointed; it does not avoid creating its evidence or calculating the
financial decisions. The earlier roughly 254–294 MB figure likewise measured
admission, not display or replay serialization. This audit does not separate
unavoidable arithmetic temporaries from avoidable diagnostic construction
inside admission, and does not establish a fundamental minimum for Gojo.

Running without a full replay archive is immediately useful for storage and
retention. Reducing creation cost further would require a separately tested
change: reusable financial equations, bounded witness retention, and a precise
choice of which historical guarantees to preserve.

## Reproduce

From the repository root:

```powershell
dotnet run --project experiments/MarketResourceAudit -c Release -- experiments/MarketResourceAudit/obj/recheck.json
```

The destination must not already exist. The harness loads the four committed
final worlds, checks frame forgetting, warms the four frozen workloads, and
writes one bounded JSON report. It retains no continuous log or growing series
of replay frames. Do not replace the committed first findings with a rerun.
