# Compact C# execution

The canonical programs remain connected Gojo structure:

```text
G(equation, input coordinates, output carrier, G(target site -> source ...))
G(selector source, selector scope, G(G(support, source, calibration) ...), output)
```

`CompactReadPlan` is a disposable execution projection of those surfaces. It
builds indexed instructions and retains each instruction's actual source element.
It never supplies creature policy, learns coefficients outside the graph, or
replaces carrier settlement. `Native.ExecutionCorrespondence(root)` exposes
instruction sites connected back to their native sources.

## Execution contract

- Cached plans check connected relation sides before reuse. Changed topology
  rebuilds the plan; mutable tick coordinates and coefficients are sampled on
  every read. Roots promote on their second use; one-shot residual programs use
  the previous reader rather than paying compilation cost. A small cache retains
  one single-root and one multi-root plan per weakly held first relation.
- Per-call pooled arrays hold sampled coordinates, numeric results, selected
  branches and projection residues. They do not retarget operational sites or
  share mutable results across calls. Returned reads remain independent of the
  scratch arrays and keep earlier admitted values.
- Familiar scalar sums, products, affine projections and scoped choices execute
  from the indexed plan. A selected branch is the only branch evaluated. Offered
  bands are still checked for invalid support, gaps and overlap.
- Integer projections truncate toward zero and retain the exact reduced
  source-basis remainder. Checked `Int128` intermediates accelerate bounded
  calculations. Wider calculations, inverse-flow participation, unfamiliar
  equations, conflicting bindings and unresolved observations use the previous
  native reader. No missing answer becomes zero.
- Numeric results are materialized as ordinary Gojo read and selection witnesses,
  including ordered upstream residue. Imports, feedback clocks, material
  transfers, markets and settlement use the same existing structures and routes.

`NativeExecution` chooses the host execution adapter for a scoped run. Its enum
and counters describe execution only; they are absent from creature meaning and
native archives. The default is compact. `Planned` selects the previous cached
reader; `Reference` selects the general native reader. This also permits tests to
switch adapters independently without process-wide mutable configuration.

`CompactSimd` is an optional bounded SIMD experiment; the default remains
`Compact`. Independent process trials and the SIMD comparison are documented
in [SIMD-PARALLEL.md](SIMD-PARALLEL.md).

## Repeatable comparisons

Build the Release runner, then run:

```powershell
dotnet build experiments/Phototaxis.Runner/Phototaxis.Runner.csproj -c Release
./experiments/Phototaxis.Runner/Compare-Backends.ps1
```

The comparison starts separate sequential processes, rotates execution order,
tests both cold and warmed operation and fixes machine resource observations.
Energy charges therefore remain identical. Construction is outside the timer;
cold profiles include initial JIT and execution-plan construction. Warm profiles
advance five simulated seconds before measuring fifteen more. The helper rejects
different observed end-state digests and retains the raw records for inspection.
Run it while builds and other trials are idle. It measures a small early-life
population; use longer trials to check mature populations and offspring.

One direct comparison or longer trial can use:

```powershell
dotnet run --project experiments/Phototaxis.Runner -c Release --no-build -- profile 60 20 --cognitive --roving --fixed-resources --backend compact
dotnet run --project experiments/Phototaxis.Runner -c Release --no-build -- profile 60 20 --cognitive --roving --fixed-resources --backend planned
```

Profile output includes execution counts, GC collections, cumulative allocation,
assembly identity and a digest of physical and prediction state. The digest is a
read-only comparison aid, not an exhaustive native-graph proof. Focused tests also
compare integer results and retained residue with the general reader, verify lazy
and unresolved behaviour, transport, caller edits and independent feedback state
through time.

## Measured results (9 October 2026)

Release build on the local Windows machine, 20 initial creatures, seed 7, roving
lights, rocks, cognitive ecology, progress and courtship enabled. No rendering or
Jev requests. Each short row is the median of three separate process timings;
allocation is the mean cumulative allocation, not retained memory.

| Timed trial | Previous wall time | Compact wall time | Throughput gain | Allocation reduction |
| --- | ---: | ---: | ---: | ---: |
| 15 simulated seconds, cold | 5.320 s | 4.699 s | 13.2% | 36.2% (2.369 to 1.512 GB) |
| 15 simulated seconds after 5 seconds warmup | 4.481 s | 4.246 s | 5.5% | 42.9% (2.440 to 1.394 GB) |
| 120 simulated seconds after 5 seconds warmup, one paired trial | 55.151 s | 40.683 s | 35.6% | 46.4% (26.685 to 14.292 GB) |

The final warmed trials ranged from 3.878 to 4.426 seconds for compact and from
4.403 to 4.539 seconds for the previous backend. These measurements establish a
modest improvement on this workload, with timing variability. Earlier prototype
results suggested a larger gain; they are not the final build's result.

All twelve short trials had identical observed state digests within their cold
or warm cohort. The final warm compact runs compiled 160 plans, reused 29,017
cached plans and deferred 1,816 transient batches, with no numeric fallback.
The final Phototaxis assembly identity is
`9512ada5-e09a-48ed-b1cd-f2edff2231a9`. Raw measurements are retained in
[`artifacts/phototaxis/compact-backend/measured`](../../artifacts/phototaxis/compact-backend/measured/comparison.json).

The longer trial also matched its observed state digest exactly and finished
with 28 creatures. It compiled 535 plans and reused 282,171 cached plans, with no
numeric fallback. Its circuit phase fell from 27.926 to 15.697 seconds; cognition
fell from 18.254 to 16.927 seconds. Cognition and circuit work still account for
about 80% of compact runtime. The previous backend ran at 2.18 times real time
and compact at 2.95 times real time. This is one longer pair, not a repeated
median. Its records are [compact](../../artifacts/phototaxis/compact-backend/measured/compact-long.json)
and [previous](../../artifacts/phototaxis/compact-backend/measured/planned-long.json).

The full solution gate passed all 1,862 tests, including 104 phototaxis tests and
17 new compact-execution cases. The focused world comparisons exercised two
seeds over 600 ticks, including physical, prediction and feedback state.

The browser AOT publish succeeded. The Skia/WebAssembly preview advanced to tick
751 (37.55 simulated seconds), with one birth and 21 living creatures, and paused
successfully. No console warnings or errors were recorded and Jev inquiries
remained disabled. A [preview screenshot](../../artifacts/phototaxis/compact-backend/browser.png)
is retained alongside the headless measurements. Browser speed was not measured
as a controlled comparison; the table above is native headless execution.

Normal simulation still bills measured computation as metabolic energy. A
faster backend can therefore change biological outcomes in normal runs. The
comparison fixes those observations so execution speed and result parity can be
checked with identical energy charges.
