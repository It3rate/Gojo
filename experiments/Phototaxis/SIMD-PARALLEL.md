# Small SIMD and parallel trial experiments

The existing connected Gojo programs remain authoritative:

```text
G(equation, input, output carrier, G(target site -> source ...))
G(selector, scope, G(G(support, source, calibration) ...), output)
```

These experiments change execution and host scheduling. They do not construct
different creature policies or add rewards, sensors, learning rules or Jev calls.
The default remains `Compact`; both experiments are available explicitly in the
native headless runner.

## Exact bounded SIMD

`--backend compactsimd` uses two `Vector128<double>` lanes for sibling affine
operand projections. A lane is eligible only when its scale is an exact integer,
the absolute scale is at most 2^20, its source offset is at most 2^26, and its
target start is at most 2^46. Multiplication and addition therefore produce
exact integers within 2^47, below the 2^53 exact-integer limit of IEEE doubles.
The source-basis residue is exactly zero for these eligible maps. This path does
not approximate division or round fractional projections.

Fractional, constant, wide, unresolved, incompatible or unsupported reads use
the existing scalar projection. Sum/product reduction still follows the original
checked scalar order, and only operands of an already selected branch are
grouped. Live coordinates, topology checks and independent native witnesses
retain the compact backend's contract.

`execution.simdBatches` and `simdLanes` report actual vector use during the timed
phase, while `simdSupported` reports runtime acceleration support. The API follows
the [.NET SIMD guidance](https://learn.microsoft.com/dotnet/standard/simd).

## Independent parallel worlds

`trials` starts independent runner processes with a concurrency limit of one to
four. Each process owns its world, caches, native identities and random stream.
There is no shared creature state or concurrent settlement within one world.
Parallelism increases experiment throughput; individual worlds remain serial.

Every child uses fixed resource observations and identical computation charges.
Machine contention cannot change its food availability or metabolic accounting.
Per-seed state digests can therefore be compared across execution backends and
concurrency settings. These digests cover observed physical/prediction state,
not exhaustive native graph identity. Focused tests additionally compare native
result/residue, frozen witnesses, feedback state and unresolved reads.

Batch wall time includes child startup, world construction, warmup, timed
simulation, state inspection and process exit. `endToEndSimulatedSpeed` divides
the total timed simulated seconds by this complete wall time. Child `actualSpeed`
measures only its timed phase; the two numbers have different denominators.
Ctrl+C terminates the children spawned by that trial batch and reports
`cancelled: true` with a nonzero exit status instead of a completed result.

## Run or repeat the comparison

Build once:

```powershell
dotnet build experiments/Phototaxis.Runner/Phototaxis.Runner.csproj -c Release
```

Four worlds with two running at once:

```powershell
dotnet run --project experiments/Phototaxis.Runner -c Release --no-build -- trials --seconds 20 --warmup 5 --population 20 --count 4 --parallel 2 --seeds 7,19,31,43 --backend compact
```

For the same worlds with SIMD, use `--backend compactsimd`. To repeat the measured
matrix (serial compact, serial SIMD, two and four concurrent compact worlds):

```powershell
./experiments/Phototaxis.Runner/Compare-SimdParallel.ps1
```

The helper rotates configuration order over three passes, retains raw JSON,
compares each seed's digest across every run and rejects assembly changes.
Run it while builds, tests and other simulations are idle.

## Results

Measured 10 October 2026 on the local Intel i7-7700HQ (four physical cores,
eight logical processors), Windows 10, .NET 10.0.12, Release x64. Vector128,
Vector256 and AVX2 hardware acceleration were available. Each batch ran four
independent worlds (seeds 7, 19, 31, 43), initially 20 creatures each, with
five simulated seconds of warmup and 20 timed seconds. These are medians of
three complete batch timings. There were 48 child trials in total.

| Configuration | Median batch wall time | Aggregate timed simulation / wall second | Throughput relative to serial compact |
| --- | ---: | ---: | ---: |
| Compact, serial | 40.705 s | 1.965 | 1.00x |
| CompactSimd, serial | 41.255 s | 1.939 | 0.987x |
| Compact, two concurrent trials | 24.670 s | 3.243 | 1.65x |
| Compact, four concurrent trials | 16.595 s | 4.821 | 2.45x |

The SIMD experiment gave no convincing end-to-end benefit. Serial SIMD ranged
from 37.245 to 42.129 seconds while serial compact ranged from 40.141 to 41.042
seconds; its median was about 1.3% slower. It really executed SIMD: the three
SIMD batches recorded 1,174,170 vector calls and 2,348,340 lanes in their timed
phases. Mean timed allocations remained approximately 7.6 GB per four-world
batch for all configurations (cumulative allocation, not resident memory).

Two concurrent trials gave 1.65x aggregate throughput; four gave 2.45x. Individual
worlds became slower under contention, but the whole batch finished sooner.
The recommendation for this machine is four concurrent trials with the scalar
compact backend. SIMD stays optional. Larger SIMD gains would require a different
batching/data layout; this experiment alone does not establish that benefit.

Every seed had exactly the same observed state digest across all 12 batches.
All runs used native assembly `f4fa5721-8065-47c2-b6eb-7b18db739470`. Raw records,
host flags, CPU/allocation counters and the summary are retained in
[`artifacts/phototaxis/simd-parallel/20261010-measured`](../../artifacts/phototaxis/simd-parallel/20261010-measured/comparison.json).

The full solution gate passed all 1,874 tests, including 116 phototaxis tests and
12 new SIMD cases. Review and tests cover exact boundaries, reversed frames,
fractional/wide fallback, live coefficients, frozen witnesses, lazy alternatives
and matching physical/feedback/prediction state. Runner smoke trials verified
both `dotnet` and apphost launch, repeated-seed digest consistency and concurrent
execution. A Ctrl+C smoke test verified that the batch and both spawned children
exit without orphan processes or an unhandled cancellation stack trace.

The browser remains on the default compact execution path. These results measure
native headless trials, with no rendering or Jev requests.
