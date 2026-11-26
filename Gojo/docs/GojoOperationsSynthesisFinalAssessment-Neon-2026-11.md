# Gojo Operations Synthesis — Final Assessment (Neon)

**Author:** Neon
**Date:** 2026-11-26 (revised after Sol's and Claude's final assessments)
**Branch reviewed:** `ops_synthesize` (originally `53bd235b`, revised on top of `2e876211`, relative to `master` `9b805964`)

---

## 1. Verdict

**Ready to merge into `master`** once one final `dotnet test Gojo.slnx` runs on a machine with `wasm-tools` (for the five `.Browser` hosts) and `QualitativeExecution.Tests`.

**Revision note after Sol's and Claude's final assessments:** Sol was right to hold the merge on `53bd235b`. All five `CompiledPlan` dynamic-topology and execution-evidence counterexamples in Sol's assessment reproduced cleanly against `53bd235b`, and Claude's Pass-1 domain-wrap fix landed in `2e876211`. Every Pass-3 defect identified by Sol and Claude is now fixed and pinned by regression tests in this revision:

1. **Same-tick `Port` / `Socket` / `Flipped` writes to `Rows` (`CompiledPlan.DetectsDynamicTopology`):** `WriteTarget` now unwraps `Surface.Underlying` and follows `Port.Owner` and `Socket.Plugged?.Owner` into `rowStores`, falling back canonically whenever any wiring can write any wiring's `Rows` store (`ASameTickPortWriteRetargetingARowMatchesCanonicalExecution`).
2. **Same-tick `Store.Life` recruitment and retirement (`CompiledPlan.DetectsDynamicTopology`):** Any wiring that writes a `Life` surface (directly or through `Flipped`) now triggers canonical execution for the containing engine so recruited or retired positions take effect immediately in the same tick and across `Settle` (`RecruitingASparePositionInTheSameTickRunsALaterCompiledFollower`).
3. **Port / Socket lane rebinds and extensions between steps (`WiringSnapshot`):** `WiringSnapshot` now snapshots `Port` lane targets (direct, through `Socket.Plugged`, or through `Flipped`), underlying target `Store.RawLive`, and `Workspace.Count`/`Introduced`, invalidating `IsCurrent` and `cachedEngineWaves` immediately when a port lane is rebound or extended (`RebindingAPortLaneBetweenStepsUpdatesEngineWavesAndEvidence`).
4. **Non-identity `Aliased` vs. `Skipped` classification (`CompiledWiring`):** Non-identity wirings now check `wiring.Aliased()` under `activeMask` and fall back canonically whenever any attended row is aliased (`AnAliasedNonIdentityPacketRowReportsAliasedRatherThanSkipped`), and `StepFastOnly`/`StepWithResult` check `CanAccelerate` before `staticRunnableCount == 0`.
5. **Non-identity multi-wave row scheduling (`CompiledWiring`) and empty-runnable wave parity:** Non-identity wirings now fall back whenever `wiring.Waves(staticRunnable).Count > 1` (`NonIdentityPacketRowsSharingAPositionAcrossDifferentPlanesSplitRowWaves`), and `RelationalColumnPlan.TryStepWithResult` / `FusedConcreteLadderAdder.BuildWiringSteps` now emit zero row waves (`[]`) when `staticRunnable` is empty (`AnEmptyRunnableLadderEmitsNoRowWaves`).
6. **Public API hygiene (Claude §4, item 3):** The six `Recognized*` kernel-family booleans on `CompiledPlan` are now `internal`; callers observe execution through `CompiledStepReport` and `HasAnyCompiledKernel`.
7. **`Recognition.Residue` / `Recognition.Residues` edges (Sol):** Added optional `int blocked = -1` support matching `Compare`/`Restore`, fell back to `Shape.Binary(capacity + 1)` above unary's 63-plane limit so `maxCapacity: 64` no longer throws, and bounded domain products with `DomainUpTo` (`ResiduesHandleBlockedBitsAndCapacitiesAboveSixtyThree`).
8. **`Portions` cache key & boundary documentation (Sol / Claude / Neon):** Keyed `Portions.Valids` directly on `(Shape, Shape)` and documented the small-denominator table ceiling (~20), conservative separate-support pooling in `WirePool`, and `Project` remainder basis (`r / d₂` source ticks or `r / (d₁ · d₂)` of the whole).

Nothing on `ops_synthesize` blocks integration architecturally. Across all three passes, the branch keeps the canonical Gojo discipline intact:

- **Implemented and pinned by tests:** relation-plus-binding operations (`Operations`), canonical integer equations (`Polynomial`), mixed-radix ladders with selected unit rungs (`Numbers` / `Ladder`), sparse/exact and one-/two-residue equation recovery (`Recognition`), live-denominator portions (`Portions`), live-position scope disjointness (`Surface.DisjointPositions`), native fractional outward rounding (`QualifiedReadSurface`), path-parameterized conformance gates (`ExecutionConformancePath.All`, including B14–B18, schedule replay, master save/load parity, and full dynamic-topology/evidence regressions), and an optional derived compiled backend (`CompiledPlan`).
- **Documented direction (explicitly kept out of claims):** pressure-driven table↔equation switching, tension-fed free-role costs, cross-domain equation-hint reuse (§9 experiments), joint multi-rung intervals without a `Fork`, persistent transport versioning for step mode/equations/units, dimensional unit algebra, and dynamic scope allocators.
- **Kept off the branch on purpose:** speculative exploratory mechanics (`Flows.Sweep`, oscillators, coasting/stiffness probes, `Booleans.cs`, `Dynamics.cs`, `Multiplication.cs`, approximate waveform lowering) remain on their originating experiment branches as historical evidence.

---

## 2. Assessment By Pass

### Pass 1 (Claude) — Core Operations, Polynomials, Ladders, Recognition

**Strengths**
- Cleanest possible core factorization: `Operations.Balance`, `Product`, and `Bind` rebuild `Counts.Sum`, `Shortfall`, `Tally`, and `Accumulate` bit-for-bit identically while exposing polarity, read direction, write target, and numeric free-role cost as ordinary numbers.
- `Polynomial` stays canonical, integer-only, and string-free (text lives solely in `Lens.Describe`).
- `Ladder` sits genuinely *over* pre-numeric counts without erasing them, preserves mixed radices (5-star scales, `[2, 5]` half-stars, `[60, 24]` clocks) without power-of-two distortion, and keeps `Measure` (open finer rungs) distinct from `Restrict` (exact counts).
- Zero changes to `Transport.cs` and all 211 `master` tests pass untouched.

**Status of items spotted in final audit**
1. **Resolved in `2e876211`:** Unchecked `long` domain product in `Recognition.Relations` and `Recognition.Probe` bounded via `DomainUpTo` (`ADomainTooLargeToEnumerateIsNeverCheckedOverNothing`), and unused `Lcm` helper removed.
2. **Accepted, documented limits (no change needed):** `Interval` is per-rung (sound, tightenable via `Fork`), `Ladder.Multiply` is single-radix and column-local (explicitly tested in B16), and `Fits` is brute-force over small roles.

### Pass 2 (Sol) — Conformance Gates, Units, Scope, Accounting, Exactness

**Strengths**
- `ExecutionConformancePath.All` is the single best integration decision on the branch: every behavioral contract (B14–B18, schedule replay `100 -> 010` vs. `001`, master `9b805964` byte-for-byte save/load, `Reopen` / `ReopenRows`, workspaces, scope, units, publication, and now all dynamic-topology and wave/alias evidence checks) runs uniformly against both `Engine` and `CompiledPlan`.
- Hardening `Polynomial` canonicalization/evaluation to `BigInteger` and `Ladder.Measure` against host multiplication wrap caught subtle, real arithmetic bugs before they could reach `master`.
- `Ladder.AtUnitRung` / `TryAdd` and `Surface.DisjointPositions` add the minimum necessary unit-selection and live-site-identity machinery without introducing tags, enums, or hidden registries.
- `Operations.Bind(Polynomial, ...)` derives positive `PreservedSum` invariants cleanly when—and only when—a homogeneous balance accounts for the same weighted terms before and after.
- The native `Gojo/Structures/QualifiedReadSurface.cs` outward-rounding fix for fractional residues (+4/−4 lines, +32 lines of tests in `Gojo.Tests`) is clean, surgical, and well-tested.

**Status of items noted**
1. **Resolved in `2e876211`:** The seven Pass-2 test files were added to the test inventory table in `Gojo.EncodingC/README.md`.
2. **Host-view vs. persisted boundary (already honestly documented in §11):** `Ladder.unitRung` is a host view unless published as a port or bound through explicit weighted relations, and `Surface.DisjointPositions` is a current live read that a caller must requalify after a port rebind.
3. **Minor contract note:** `Ladder.TryAdd` returns `null` for mismatched radices or selected units, while still throwing `ArgumentException` if `a`, `b`, and `sum` live in different `Store`s (`Numbers.cs:349`). That matches single-store local wiring semantics and is fine as-is.

### Pass 3 (Neon) — Compiled Backend, Portions, Two-Residue Recognition (Self-Critique)

**Strengths**
- Strict delta-only port: zero duplication of Pass 1/2 `Polynomial`, `Numbers`, `Ladder`, `Counts`, `Flows`, or `Transport`; only six internal accessors added across `Plane`, `Store`, `Wiring`, and `Engine`.
- `CompiledPlan` is genuinely optional and derived: deleting `Gojo.EncodingC/Compilation/CompiledPlan.cs` changes zero EncodingC semantics and zero saved bytes. It adds zero enums (enforced by `PassThreeAddsNoEnumsToEncodingC`), zero strings, zero floats/doubles/decimals, and zero `System.Math` references.
- Every kernel qualifies strictly from table bits, accounting, bindings, and schedule—never by name—and cleanly falls back to `Engine` on recorded wirings, ports/sockets/workspaces, shared-site relations (`Ladder.Add(x, x, doubled)`, B16), non-column relations, multi-wiring simultaneous steps, broken codes, dynamic-topology graphs (`Rows` writes through any surface or `Life` writes), aliased or multi-wave non-identity rows, and stale snapshots (including port lane rebinds).
- Whole-path Release benchmarks answered the architecture question decisively:
  - Accelerating Pass-1 relational `Ladder.Add` **directly in place** (`1.388 ms` whole-path on 4,096-lane `[10, 10, 10]`) beat extracting to a concrete `Store` layout and writing back (`14.568 ms`) by **10.5×**, while preserving open/uncertain supports (`2.549 ms` whole-path / `1,776×` speedup on 65,536-lane `[2, 5]` with 50% open coarse lanes).
  - Ineligible fallback (`FactoredInverse`, shared-site `x + x`) incurred **zero penalty** under `Settle` (slightly faster than `Engine.Settle` because it avoids allocating unused per-tick `EngineStep.Waves` dictionaries while `CompiledPlan.Step` still computes exact `Waves`).
- Both Sol defects in `Portions` are fixed and pinned by tests (`(1, 0)` and `n > d` contradict via `Portions.Valid`; `Portions.AddSameWhole` carries `whole ∈ {0, 1, 2}` so `5/5 + 5/5` settles to `whole = 2, rem = 0`), and `Portions.Valids` now keys directly on `(Shape, Shape)`.
- `Operations.System` + `Recognition.Residues` confirmed Pass 1's §7 item 7 hypothesis: two hidden residue roles with unit numeric cost recover and restore `min(a, b)`, `max(a, b)`, and `|a − b|` (including blocked-bit marked forms and residue capacities above 63) with zero new fold kinds or enums.

**Remaining post-merge maintenance debt in Pass 3**
1. **`CompiledPlan.cs` is ~3,140 lines in a single file.** Keeping it isolated in `Compilation/` limits its blast radius, and the code compiles with zero warnings, but splitting it into 4–5 focused internal files under `Gojo.EncodingC/Compilation/` (plan/snapshot, relational column/ladder kernel, concrete ladder kernel, packet-convert kernel, SIMD transition kernel) will substantially improve auditability after merge.
2. **Staleness checks re-compare cloned reference planes on every `Step`/`Settle` call.** `WiringSnapshot.Matches` guarantees airtight staleness detection without adding mutation counters to `Store` or `Wiring`. For `Settle` (one check up front) the overhead is negligible; a monotonic structural-epoch integer on `Store`/`Wiring` is a straightforward future optimization if single-step hot loops ever care.
3. **Growing the general table kernel to small relations (Claude §4, item 2):** The family-specific adder/convert/multiply kernels are verified and earn the headline speedups, while the small-table SIMD kernel is the family-independent mechanism in keeping with Gojo. Extending general bit-sliced lowering to arbitrary small relations is the right post-merge direction.
4. **Benchmark scope must stay honestly framed.** The 270×–10,000× speedups reflect wide uniform batches (4,096–65,536 lanes) using portable `System.Numerics.Vector<ulong>` SIMD (JIT-lowered to AVX2/NEON per CPU). On 1-lane graphs (like B16), `Engine` is already ~0.1 ms, and compiling only amortizes over repeated steps.
5. **Search scalability in `Recognition.Residues`:** Like `Fits`, `Residues` is deterministic host search suited to small roles and small integer weights/capacities, not a general large-domain solver.

---

## 3. Canonical Prompt & Hygiene Check

| Check | Status | Evidence |
| --- | --- | --- |
| Meaning stays in connected structure (`GojoSystemPrompt` Axiom 1, §13) | Pass | Removing `CompiledPlan` or `Lens` leaves every law, ladder, port, and transport byte intact; `Recognized*` kernel booleans are `internal`. |
| Zero enums, strings, real numbers, `System.Math` in `Gojo.EncodingC` | Pass | `SubstrateTests` reflection guards + `PassThreeAddsNoEnumsToEncodingC` all pass (all production files live in `namespace Gojo.EncodingC`). |
| Pre-numeric preserved (`§3.6`, `§8`) | Pass | All 211 `master` tests pass unchanged; `Counts` laws are bit-identical. |
| Transport compatibility (`§7` item 6) | Pass | `git diff origin/master -- Gojo.EncodingC/Transport/` is empty; `master` `9b805964` save loads, steps, and re-saves byte-for-byte on both `Engine` and `CompiledPlan`. |
| Compiler warnings | Pass | Clean Release and Debug builds introduce **0 new warnings** (the only 2 warnings in `Gojo.EncodingC` are pre-existing in `Flow/Engine.cs:214` from `master`). |
| Leftover debug / TODO markers | Pass | Zero `TODO`/`FIXME`/`HACK` markers in any added/modified production file. |

---

## 4. Verification Status & Remaining Gate Before Merging to `master`

- **Verified after post-assessment fixes (Linux, `.NET 10.0.404`):**
  - `Gojo.EncodingC` (`Debug` and `Release`): **343 passed, 0 failed, 0 skipped** (all 5 Sol `CompiledPlan` repros confirmed failing before the fix and passing after on `ExecutionConformancePath.All`, plus empty-runnable wave parity and `ResiduesHandleBlockedBitsAndCapacitiesAboveSixtyThree`).
  - 18 other non-WASM, non-`QualitativeExecution` test projects in `Gojo.slnx` (`Release`), including `Gojo.Tests` (1,074) and `GojoCAD.Tests` (155): **3,789 passed, 0 failed** (**4,132 passed, 0 failed** total across 19 test projects).
- **Remaining final gate before merging to `master`:**
  - Run one full `dotnet test Gojo.slnx` on a host with `wasm-tools` installed (for the five `.Browser` projects) plus `QualitativeExecution.Tests` (3,509 tests; has zero project dependency on `Gojo.EncodingC`).

---

## 5. Post-Merge Follow-Ups (Deferred Cleanly)

1. Split `Gojo.EncodingC/Compilation/CompiledPlan.cs` into smaller internal files under `Gojo.EncodingC/Compilation/`.
2. Add a trimmed differential fuzz test covering ports, `Store.Life` recruitment/retirement, and full `EngineStep`/`WiringStep` evidence fields.
3. Grow the general compiled kernel to arbitrary small relations so special kernels have to beat it by a measured margin.
4. Leave `Transport.cs` unversioned until a concrete feature needs persisted step mode, equations, or unit metadata.
5. Run the §9 axiom experiments (pressure-driven table↔equation switching, tension-driven free-role costs, cross-domain equation-hint reuse) directly on top of the merged `master` baseline.
