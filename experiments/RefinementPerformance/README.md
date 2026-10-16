# Refinement performance landmark

A repeatable, single-thread resource comparison of evolving qualitative and
measured Gojo structures. The benchmark contains no learning or creature physics.
It does not use timing assertions in unit tests.

The recurrent workload connects cells in a ring. Neighbor signals, previous
observations, stock and action feed subsequent rounds through exposed native
ports. One native finite-law program runs through three execution backends:

- Native interpretation with a new read witness at every application.
- Compiled atomic tables with the same per-application native witness output.
- Batched support masks, with source validation once per stage and no
  per-application witness allocation.

The numeric counterpart reuses carrier values, equations and interval policies.
Its native and compiled variants retain the same numeric dynamics; the compiled
variant derives arithmetic and selection from the native source and continues
to use kernel tension settlement. The categorical and numeric models answer
corresponding signal/control questions at different detail; they are not a
proved refinement of one identical dynamics law.

A second workload covers structural features: E/H/B generation and recognition,
shared landmarks, forward/inverse resolution, uncertainty, state/site refinement,
ordinal composition, law obligations/counterexamples, independent schedules,
held observations and replay/expiry refusal. Its measured variant attaches
numeric calibration and timelines to those same identities. These are bounded
episode audits, separate from the evolving ring.

Run from the repository root in Release:

```powershell
dotnet run --project experiments/RefinementPerformance -c Release -- --sizes 32,128,512 --rounds 256 --samples 3 --coverage-size 16 --coverage-rounds 16 --output experiments/RefinementPerformance/results/2026-10-16-primary
dotnet run --project experiments/RefinementPerformance -c Release -- --sizes 512 --rounds 8192 --samples 3 --mode optimized --no-coverage --output experiments/RefinementPerformance/results/2026-10-16-sustained
```

`--mode` accepts `all`, `native`, `compiled`, `batched`, `qualitative`, `numeric`
or `optimized`. `--sizes` is a comma-separated list. Each timing sample owns a
fresh workload. Warmup uses separate instances; sample order rotates. JSON keeps
all raw timings, checksums, setup/allocation/audit costs, GC counts, graph counts,
runtime, build configuration and normalized source fingerprint. Markdown shows
medians and ranges. Construction/capture and post-run evidence audits are outside
the execution timer. Managed allocation is cumulative, not retained or peak RAM.

Native and compiled qualitative checksums must agree; native and compiled numeric
checksums must agree; all breadth variants must agree. Numeric and qualitative
answers need not match. Unit tests also reconstruct selected native numeric
predictions/residue and finite-law proof rows from the derived plans.

Neither witness lane retains a complete run archive: it constructs every read
witness and retains bounded recent evidence. Compute lanes omit those repeated
constructions. An audit can decode current state and selected retained final
inputs; it cannot recover a history the run did not keep.

The reusable `FiniteLawBatchPlan` lives in the qualitative experiment project.
It supports up to 64 domain identities using atomic tables and masks without
the older eight-output limit or an exponential all-subsets table. Numeric plans
remain narrow benchmark backends for forward-axis affine sums/products and exact
interval conditions. Open/incompatible inputs still need native readers.

See the [assessment](../../Gojo/docs/GojoRefinementComputeComparison.md) for
recorded data, comparisons, limits and optimization priorities.
