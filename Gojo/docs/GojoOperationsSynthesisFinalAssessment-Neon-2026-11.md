# Gojo Operations Synthesis — Final Assessment (Neon)

**Author:** Neon
**Date:** 2026-11-26
**Branch reviewed:** `ops_synthesize` at `53bd235b` (relative to `master` `9b805964`)
**Diff size:** 38 files changed, +9,444 / −20 lines (`Gojo.EncodingC`: 329 passing tests, up from 211 on `master`)

---

## 1. Verdict

**Ready to merge into `master`**, with three tiny, optional polish fixes (under 15 lines total) and one final full-solution `dotnet test Gojo.slnx` run on a machine with `wasm-tools` installed.

Nothing on `ops_synthesize` blocks integration architecturally. Across all three passes, the branch keeps the canonical Gojo discipline intact:

- **Implemented and pinned by tests:** relation-plus-binding operations (`Operations`), canonical integer equations (`Polynomial`), mixed-radix ladders with selected unit rungs (`Numbers` / `Ladder`), sparse/exact and one-/two-residue equation recovery (`Recognition`), live-denominator portions (`Portions`), live-position scope disjointness (`Surface.DisjointPositions`), native fractional outward rounding (`QualifiedReadSurface`), path-parameterized conformance gates (`ExecutionConformancePath.All`, including B14–B18, schedule replay, and master save/load parity), and an optional derived compiled backend (`CompiledPlan`).
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

**Small items spotted in final audit**
1. **Unchecked `long` domain product in `Recognition.Relations` and `Recognition.Probe`** (`Gojo.EncodingC/Laws/Recognition.cs:129` and `:480`):
   `roles.Aggregate(1L, (n, r) => n * r.Size)` can silently overflow signed `long` on large multi-role calls, potentially wrapping to a negative number `<= 1 << 20` and bypassing the enumeration guard. Pass 2 already hardened `Polynomial`, `Measure`, and `Residues` against host overflow; guarding those two products with `BigInteger` or saturated multiplication closes the last host-overflow gap in `Recognition`.
2. **Unused helper method** (`Gojo.EncodingC/Laws/Recognition.cs:657`): `private static long Lcm(long a, long b)` is unreferenced and can be deleted.
3. **Accepted, documented limits (no change needed):** `Interval` is per-rung (sound, tightenable via `Fork`), `Ladder.Multiply` is single-radix and column-local (explicitly tested in B16), and `Fits` is brute-force over small roles.

### Pass 2 (Sol) — Conformance Gates, Units, Scope, Accounting, Exactness

**Strengths**
- `ExecutionConformancePath.All` is the single best integration decision on the branch: every behavioral contract (B14–B18, schedule replay `100 -> 010` vs. `001`, master `9b805964` byte-for-byte save/load, `Reopen` / `ReopenRows`, workspaces, scope, units, publication) automatically ran against `CompiledPlan` in Pass 3 without modifying a single assertion.
- Hardening `Polynomial` canonicalization/evaluation to `BigInteger` and `Ladder.Measure` against host multiplication wrap caught subtle, real arithmetic bugs before they could reach `master`.
- `Ladder.AtUnitRung` / `TryAdd` and `Surface.DisjointPositions` add the minimum necessary unit-selection and live-site-identity machinery without introducing tags, enums, or hidden registries.
- `Operations.Bind(Polynomial, ...)` derives positive `PreservedSum` invariants cleanly when—and only when—a homogeneous balance accounts for the same weighted terms before and after.
- The native `Gojo/Structures/QualifiedReadSurface.cs` outward-rounding fix for fractional residues (+4/−4 lines, +32 lines of tests in `Gojo.Tests`) is clean, surgical, and well-tested.

**Small items / honest boundaries noted**
1. **`Gojo.EncodingC/README.md` test table omission:** Seven Pass-2 test files (`ExecutionScheduleConformanceTests`, `OperationsEvidenceConformanceTests`, `PolynomialExactnessTests`, `SynthesisPublicationTests`, `SynthesisScopeTests`, `SynthesisScopedWorkspaceTests`, `SynthesisUnitTests`) are missing from the test inventory table at the bottom of `Gojo.EncodingC/README.md`. Worth adding so the README accurately reflects the full suite.
2. **Host-view vs. persisted boundary (already honestly documented in §11, worth repeating for clarity):** `Ladder.unitRung` is a host view unless published as a port or bound through explicit weighted relations, and `Surface.DisjointPositions` is a current live read that a caller must requalify after a port rebind. Neither claims to be a saved transport policy.
3. **Minor contract note:** `Ladder.TryAdd` returns `null` for mismatched radices or selected units, while still throwing `ArgumentException` if `a`, `b`, and `sum` live in different `Store`s (`Numbers.cs:349`). That matches single-store local wiring semantics and is fine as-is.

### Pass 3 (Neon) — Compiled Backend, Portions, Two-Residue Recognition (Self-Critique)

**Strengths**
- Strict delta-only port: zero duplication of Pass 1/2 `Polynomial`, `Numbers`, `Ladder`, `Counts`, `Flows`, or `Transport`; only six internal accessors added across `Plane`, `Store`, `Wiring`, and `Engine`.
- `CompiledPlan` is genuinely optional and derived: deleting `Gojo.EncodingC/Compilation/CompiledPlan.cs` changes zero EncodingC semantics and zero saved bytes. It adds zero enums (enforced by `PassThreeAddsNoEnumsToEncodingC`), zero strings, zero floats/doubles/decimals, and zero `System.Math` references.
- Every kernel qualifies strictly from table bits, accounting, bindings, and schedule—never by name—and cleanly falls back to `Engine` on recorded wirings, ports/sockets/workspaces, shared-site relations (`Ladder.Add(x, x, doubled)`, B16), non-column relations, multi-wiring simultaneous steps, broken codes, self-rewiring graphs, and stale snapshots.
- Whole-path Release benchmarks answered the architecture question decisively:
  - Accelerating Pass-1 relational `Ladder.Add` **directly in place** (`1.388 ms` whole-path on 4,096-lane `[10, 10, 10]`) beat extracting to a concrete `Store` layout and writing back (`14.568 ms`) by **10.5×**, while preserving open/uncertain supports (`2.549 ms` whole-path / `1,776×` speedup on 65,536-lane `[2, 5]` with 50% open coarse lanes).
  - Ineligible fallback (`FactoredInverse`, shared-site `x + x`) incurred **zero penalty** under `Settle` (slightly faster than `Engine.Settle` because it avoids allocating unused per-tick `EngineStep.Waves` dictionaries while `CompiledPlan.Step` still computes exact `Waves`).
- Both Sol defects in `Portions` are fixed and pinned by tests (`(1, 0)` and `n > d` contradict via `Portions.Valid`; `Portions.AddSameWhole` carries `whole ∈ {0, 1, 2}` so `5/5 + 5/5` settles to `whole = 2, rem = 0`).
- `Operations.System` + `Recognition.Residues` confirmed Pass 1's §7 item 7 hypothesis: two hidden residue roles with unit numeric cost recover and restore `min(a, b)`, `max(a, b)`, and `|a − b|` with zero new fold kinds or enums.

**Honest weaknesses / maintenance debt in Pass 3**
1. **`CompiledPlan.cs` is 3,070 lines in a single file.** That is larger than all other new Pass 1+2 production code combined (~1,600 lines). Keeping it isolated in `Compilation/` limits its blast radius, and the code compiles with zero warnings, but splitting it into 4–5 focused internal files under `Gojo.EncodingC/Compilation/` (plan/snapshot, relational column/ladder kernel, concrete ladder kernel, packet-convert kernel, SIMD transition kernel) would substantially improve auditability after merge.
2. **Staleness checks re-compare cloned reference planes on every `Step`/`Settle` call.** `WiringSnapshot.Matches` guarantees airtight staleness detection without adding mutation counters to `Store` or `Wiring`. For `Settle` (one check up front) the overhead is negligible; for tight loops calling single-tick `CompiledPlan.Step()` on very wide stores (65,536+ lanes), comparing 16+ reference planes per call adds a few tens of microseconds per step. Acceptable today; a monotonic structural-epoch integer on `Store`/`Wiring` is a straightforward future optimization if single-step hot loops ever care.
3. **Benchmark scope must stay honestly framed.** The 270×–10,000× speedups reflect wide uniform batches (4,096–65,536 lanes) where `Engine`'s fully general per-row interpreter (`MeetAtAWrittenBit` $O(P^2)$ per row, `Waves` hash tables, per-bit `Reference`/`Gather`, state-table scans) dominates. On 1-lane graphs (like B16), `Engine` is already ~0.1 ms, and compiling only amortizes over repeated steps.
4. **Minor cache-key nit in `Portions.Valid`** (`Gojo.EncodingC/Laws/Portions.cs:46`): using `(numerator.Size, denominator.Size, !numerator.IsUnary || !denominator.IsUnary)` as the dictionary key can thrash if a caller mixes `(unary, binary)` and `(binary, unary)` of identical sizes (correctness is unaffected because line 50 verifies exact `Shape` equality, so keying directly on `(Shape, Shape)` is simply cleaner).
5. **Search scalability in `Recognition.Residues`:** Like `Fits`, `Residues` is deterministic host search suited to small roles and small integer weights/capacities (`maxWeight = 2`, `maxCapacity = 8`), not a general large-domain solver.

---

## 3. Canonical Prompt & Hygiene Check

| Check | Status | Evidence |
| --- | --- | --- |
| Meaning stays in connected structure (`GojoSystemPrompt` Axiom 1, §13) | Pass | Removing `CompiledPlan` or `Lens` leaves every law, ladder, port, and transport byte intact. |
| Zero enums, strings, real numbers, `System.Math` in `Gojo.EncodingC` | Pass | `SubstrateTests` reflection guards + `PassThreeAddsNoEnumsToEncodingC` all pass (all production files live in `namespace Gojo.EncodingC`). |
| Pre-numeric preserved (`§3.6`, `§8`) | Pass | All 211 `master` tests pass unchanged; `Counts` laws are bit-identical. |
| Transport compatibility (`§7` item 6) | Pass | `git diff origin/master -- Gojo.EncodingC/Transport/` is empty; `master` `9b805964` save loads, steps, and re-saves byte-for-byte on both `Engine` and `CompiledPlan`. |
| Compiler warnings | Pass | Clean Release build introduces **0 new warnings** (the only 2 warnings in `Gojo.EncodingC` are pre-existing in `Flow/Engine.cs:214` from `master`). |
| Leftover debug / TODO markers | Pass | Zero `TODO`/`FIXME`/`HACK` markers in any added/modified production file; temporary benchmark scratch directory removed. |

---

## 4. Verification Status & Remaining Gate Before Merging to `master`

- **Verified on `53bd235b` (Linux, `.NET 10.0.404`):**
  - `Gojo.EncodingC` (`Debug` and `Release`): **329 passed, 0 failed, 0 skipped**.
  - 18 other non-WASM, non-`QualitativeExecution` test projects in `Gojo.slnx` (`Release`), including `Gojo.Tests` (1,074) and `GojoCAD.Tests` (155): **3,789 passed, 0 failed** (**4,118 passed, 0 failed** total across 19 test projects).
- **Not yet re-run on `53bd235b` (last run full on Pass 2 `f117a7c9` on Windows):**
  - The five `.Browser` projects in `Gojo.slnx` (require `wasm-tools`, not installed on this Linux VM).
  - `QualitativeExecution.Tests` (3,509 tests, ~11 min; has zero project dependency on `Gojo.EncodingC`).

---

## 5. Recommended Action Plan

### Before merging `ops_synthesize` into `master`
1. **Run one full `dotnet test Gojo.slnx`** on Windows (or a machine with `wasm-tools`) once Claude's and Sol's final assessments are in, to close the browser-build and `QualitativeExecution.Tests` verification box on the final commit.
2. **Optional 15-minute polish pass (recommended, very low risk) once all three assessments are collected:**
   - Guard the `domain` multiplication in `Recognition.Relations` (`Recognition.cs:129`) and `Recognition.Probe` (`Recognition.cs:480`) against `long` overflow, and remove the unused `Lcm` helper (`Recognition.cs:657`).
   - Add the 7 Pass-2 test files to the test inventory table in `Gojo.EncodingC/README.md`.
   - Change `Portions.Valids` cache key (`Portions.cs:46`) to `(Shape, Shape)`.

### Defer to post-merge (keep the merge clean)
1. Split `Gojo.EncodingC/Compilation/CompiledPlan.cs` into smaller internal files under `Gojo.EncodingC/Compilation/`.
2. Leave `Transport.cs` unversioned until a concrete feature needs persisted step mode, equations, or unit metadata.
3. Run the §9 axiom experiments (pressure-driven table↔equation switching, tension-driven free-role costs, cross-domain equation-hint reuse) directly on top of the merged `master` baseline.
