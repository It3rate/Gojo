# Refinement compute comparison: first large workload landmark

Status: implemented and measured, 2026-10-16. This follows the
[resolution and schedule experiments](GojoResolutionAndScheduleExperiment.md).
The code is in [RefinementPerformance](../../experiments/RefinementPerformance/README.md).
No kernel primitive or settlement rule changes. This is a resource benchmark,
without learning, creature physics or a result-quality score.

## Main finding

A compiled finite circuit has a substantial fast path. In the sustained run,
512 interacting cells ran 8,192 rounds: 4,194,304 cell-rounds and approximately
41.9 million finite-law applications per sample. The ratio of median execution
durations was approximately **196 times** in favor of qualitative batched masks
over the compiled numeric counterpart.

This gain combines lower detail, finite-table execution, compact state and omitted
per-application witnesses. It is not an isolated comparison of integer arithmetic
against bit arithmetic. Native qualitative interpretation is in the same broad
cost range as numeric native reads and changes relative rank across sizes. The
broader structural suite currently shows only modest gains.

## Sustained compute: both models compiled

Three fresh samples per mode, single thread, Release, .NET 10.0.12 x64, Windows
10.0.26200, workstation GC. Hardware: AMD Ryzen 7 5700G, 8 cores / 16 logical
processors. No parallel benchmark workloads or test runs were started during
measurement. This is one desktop process, not an isolated hardware lab.

| Mode | Median seconds | Observed range, seconds | Cell-rounds/second | Managed bytes/cell-round |
| --- | ---: | ---: | ---: | ---: |
| Qualitative batched masks | 0.1623 | 0.1397–0.2849 | 25,838,291 | 0 |
| Numeric compiled reads with kernel settlement | 31.8499 | 31.3035–32.0081 | 131,690 | 9,804 |

Construction/capture is separate: median 4.40 ms qualitative and 122.04 ms numeric.
Post-run state/proof audits are also separate: median 0.07 ms and 1.75 ms.
The numeric compute run cumulatively allocates about 41.1 GB per sample; this is
transient allocation, not retained or peak memory. Checksums repeat across fresh
samples. Both runs use the same normalized source fingerprint recorded in JSON.

Raw [sustained measurements](../../experiments/RefinementPerformance/results/2026-10-16-sustained/comparison.json)
and [generated table](../../experiments/RefinementPerformance/results/2026-10-16-sustained/comparison.md)
retain the complete data and qualifications.

## What the unoptimized and witnessed paths cost

The main matrix uses 32, 128 and 512 cells, each running 256 rounds, with three
samples per mode. At 512 cells this is 131,072 cell-rounds per sample:

| Execution mode | Median seconds | Managed bytes/cell-round |
| --- | ---: | ---: |
| Qualitative native interpretation + witnesses | 21.318 | 263,082 |
| Qualitative compiled tables + witnesses | 1.423 | 25,353 |
| Qualitative batched masks | 0.00557 | 0 |
| Numeric native reads + witnesses + settlement | 30.527 | 113,307 |
| Numeric compiled reads + settlement | 1.137 | 10,049 |

The qualitative witnessed compiler is about 15 times faster than native finite
interpretation here while still constructing each application witness. Omitting
those repeated constructions and batching validation produces the much larger
compute-only improvement. The numeric compiler also helps substantially: about
27 times at this size. Numeric carrier changes still run the real kernel.

At 32 cells the native qualitative median is 0.863 s versus numeric 0.542 s;
at 128 cells it is 3.374 s versus numeric 4.582 s. These data do not establish a
uniform intrinsic native speed advantage. Small fast timings also show warmup
and runtime optimization effects; the sustained run is the better fast-path
landmark. Raw ranges remain visible rather than selecting a best sample.

The 512-cell source graphs expose 26,727 relations and no carriers for the finite
program, versus 205,324 relations and 8,195 carriers for the measured program.
These are reachable relation-graph counts, not complete heap or byte-storage
measurements. Qualitative modes share a setup builder that also captures plans
for address encoding; their construction data do not isolate cold compilation
break-even against a compiler-free native builder.

Raw [matrix measurements](../../experiments/RefinementPerformance/results/2026-10-16-primary/comparison.json)
and [all generated tables](../../experiments/RefinementPerformance/results/2026-10-16-primary/comparison.md)
include setup allocation, audits, GC counts, graph sizes, metadata and checksums.

## Connected recurrent workload

The native finite program is inspectable independently of its runner:

```text
cell = exposed field / previous field / stock / stage slots
neighbor operands = bindings to the actual adjacent cell's slots
application = ((left-port -> right-port) -> native-law) -> output-port
program = Closed(ordered applications)
feedback = next-field -> field; old-field -> previous-field
```

Each round applies ten finite laws per cell: neighbor comparison, previous-sample
trend, ordinal composition through the shared neighbor pivot, sign product,
trend-to-input policy, stock transition, transition-to-state projection, the
existing stock/light conditional policy, directional response and an authored
three-phase disturbance. Closed multivalued composition support remains a mask
of alternatives. The disturbance is an explicit categorical test input cycle,
not a private measured signal. Neighbor output and memory affect later rounds.

The numeric counterpart uses persistent connected carriers and reusable native
equations: a six-port weighted recurrence from self, neighbors, external forcing
and prior commands; threshold, trend and gradient differences; a product; two
numeric conditional selections; stock settlement; and signal/previous/command
delta imports. Its five equation reads, two selections and stock settlement are
a descriptive work count, not eight equal-cost primitive operations.

The two models serve corresponding signal/control roles with different update
laws and detail. Their trajectories are deliberately not asserted equivalent.
In particular numeric clamp pressure remains tension, while the finite stock
blocks excess pulses. Numeric/qualitative timings therefore compare complete
model resource costs, not a proved semantics-preserving abstraction of one
identical physical simulation. No demand-driven stage pruning is used: the finite
program executes all ten stages each round.

## Broader structural coverage

The second suite repeatedly audits bounded episodes, separately from the evolving
ring. It covers:

- E/H/B generation, recognition, local role constraints and shared landmarks;
- resolution images/preimages, lossy alternatives and open support;
- same-event stock and site refinement;
- ordinal composition, passing law obligations, a cancellation counterexample
  and an unresolved premise;
- independent schedules, held observations, replay and expiry refusal;
- measured suppliers, non-midpoint calibration and numeric timeline spans.

For 16 cells and 16 episodes, median native qualitative time is **78.55 ms** and
measured time is **103.12 ms**, about 1.31 times. Compiling ordinal composition
alone gives **101.63 ms** qualitative and **101.31 ms** measured in this run, with
overlapping ranges and no demonstrated whole-suite gain. Matching, schedules,
calibration checks and witness construction still dominate this suite. Measured
mode adds interpretation work to the same structural episode; it does not replace
the matcher with a different numeric pattern algorithm.

Thus the 196-times recurrent fast path is not a speed claim for the complete
primitive library. The breadth suite is retained as an optimization baseline
for those currently native mechanisms.

## Implemented compilation and evidence promises

`FiniteLawBatchPlan` captures the actual native finite-law atomic reads and proof
rows. It supports up to 64 identities per domain, including all nine stock
transition identities. Its table has left-count × right-count entries rather
than an exponential table for every possible support. Batched singleton reads
use direct atomic lookup; multivalued supports union the applicable results.

A batch checks captured relation/tick dependencies once, validates every input
before writing, and then executes synchronously without callbacks or allocation.
Foreign masks, stale sources and shifted buffer overlap refuse atomically.
Callers must prevent concurrent edits. Address bits retain their owning law's
basis meaning; they are not portable category numbers. Selected output support
and consumed-row witnesses can be reconstructed from the plan while current.

The numeric plans are narrower benchmark backends for forward-axis affine-port
sums/products and exact interval conditions. They derive parameters and selected
rule identities from native structure, guard source edits per round, and leave
all carrier mutation on the existing tension path. Opposed axes and general
uncertain supports remain native. Final input samples allow a selected cell's
native equation predictions, source-basis residue and policy evidence to be
reconstructed and checked after timing.

Both witness modes construct every read but retain bounded recent evidence,
rather than an exact full archive. Compute modes omit repeated witness
construction. Post-run audits do not recover unretained histories. Witness-mode
output encoding includes per-read source validation; mask execution amortizes
validation across the stage. These differences are intentional and explicit.

## Optimization priorities suggested by the measurements

1. Keep native finite laws authoritative and use the new batched plan for closed,
   repeatedly executed finite circuits. Wider output support removes the earlier
   nine-transition compilation obstacle.
2. Separate repeated compute from explicitly requested evidence materialization.
   Preserve the source and required samples; do not promise reconstruction of
   forgotten history.
3. Profile and reduce numeric settlement allocation. Even compiled numeric reads
   still allocate about 9.8 KB per cell-round. Grouped changes and reused local
   settlement scratch space are candidates, not optimizations proved here.
4. Compile stable structural templates and schedule contracts without removing
   identity, stale-source or replay checks. The breadth suite currently gets
   little benefit from compiling only one local law.
5. Consider narrower masks, fused stages and optional demand-driven pruning.
   Current state uses ordinary `ulong` arrays; there is no cross-cell bit packing,
   SIMD or automatic coarse-question short circuit in this run.

Future changes should rerun both suites, retain raw samples and state any changed
workload/evidence promise. The benchmark intentionally preserves the present
limitations instead of treating a faster reduced model as universal equivalence.

## Verification

Twenty-nine new cases protect finite batch compilation, recurrent backend parity,
structural breadth answers, numeric backend parity, selected native evidence,
signed projection/residue and stale-source refusal. Timing thresholds are absent.
The benchmark itself checks deterministic repetitions, qualitative backend
agreement, numeric backend agreement and breadth agreement across interpretation.

`dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal` passes all
2,780 tests across 12 test projects, including 350 qualitative execution cases,
with zero failures and zero skipped tests. The final source fingerprint matches
both benchmark artifacts; subsequent changes are documentation only.
