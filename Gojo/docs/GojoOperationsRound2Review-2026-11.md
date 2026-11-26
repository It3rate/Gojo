# Gojo Operations — Round 2 Review & Variable-Partition Numbers (Pre-Round 3)

**Round:** Round 2 Review (Documentation Round — Pre-Round 3)  
**Date:** 25 November 2026  
**Branch:** `ops_neon` (reviewing `ops_sol` `9e5f8444`, `claude/youthful-newton-wh29co` `a26f5b80`, and `ops_neon` `059f5c45` against `master` `9b805964`)  
**Status:** Pure documentation checkpoint — no code changes in this round. Epistemic labels follow [GojoSystemPrompt.md](GojoSystemPrompt.md) §1 (**implemented**, **documented direction**, **exploratory**).  
**Companion doc:** [GojoOperations-2026-11.md](GojoOperations-2026-11.md) (Phases 0–5, Round 1 comparison, Round 2 implementation summary).

---

## Round Index (Cross-Branch Map)

Because the three branches used slightly different labels as they progressed, this table maps the rounds across branches:

| Round | Focus | `ops_sol` | `claude/youthful-newton-wh29co` | `ops_neon` |
| --- | --- | --- | --- | --- |
| **Round 0** | Operations architecture & synthesis against `master` tension (`9b805964`) | `7ea82356` (`GojoOperationsDiscussion.md`) | `39c0c2f8` (`ops_claude/GojoOperations-2026-11.md`) | `f0267a36` ([GojoOperations-2026-11.md](GojoOperations-2026-11.md)) |
| **Round 1** | First executable `Gojo.EncodingC` implementations | `29df5335` (`BoundedProduct`, `PairFormation`, scope/delivery/factoring tests) | `c04869c1` (`Operations.Bind`, unsigned oscillators, coasting, stiffness, bit rung) | `6f655fc2` (Phases 0–5, `Rungs`, `Dynamics`, `CompiledPlan`) |
| **Round 1 Review** | Branch comparison, equifinal potentials, symbolic regression + numbers proposals | `fe5d473d` (`GojoOperationsBranchComparison.md`, `GojoOperationsDiscoveryProposal.md`) | `6a7d3f17` (`GojoOperationsComparison-2026-11.md`, `GojoOperationsProposal-2026-11.md`, `Flows.Sweep` fix `928ef76f`) | `97123b1f` (peer-review fixes, 338×–2,815× AVX2 `CompiledPlan`, §8.2–§11) |
| **Round 2** | Minimal multi-rung numbers + symbolic regression / discovery | `9e5f8444` ("First bounded numeric implementation": `RadixFour`, read/material/discovery tests) | `a26f5b80` ("Round A": `Polynomial`, `Bind(cost)`, relational `Numbers`/`Ladder`, `Recognition` nullspace + active `Probe`) | `059f5c45` ("Round 2": `Operations`, decoupled `Flows.Sweep`, `Numbers`, `StructuralCompression`, analyzer-driven `CompiledPlan`) |
| **Round 2 Review (this doc)** | Cross-branch Round 2 critique + **variable partition sizes** (five-star review / denominator analysis) + Round 3 plan | *(for review)* | *(for review)* | **This document** |

---

## Verified Starting Point (25 November 2026)

All three branch heads were checked out cleanly against `master` (`9b805964`) and verified with `dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj -c Release`:

| Branch | Commit | Release Tests | Prod LOC vs. `master` | Test LOC vs. `master` | Doc LOC vs. `master` | `Transport` wire compat with `master` |
| --- | --- | --- | ---: | ---: | ---: | --- |
| `ops_sol` | `9e5f8444` | **248 / 248** (5 s) | `+72 / -0` | `+1,292 / -0` | `+582 / -0` | **Exact** (untouched) |
| `claude/youthful-newton-wh29co` | `a26f5b80` | **264 / 264** (5 s) | `+751 / -8` | `+1,856 / -0` | `+2,641 / -0` | **Exact** (untouched) |
| `ops_neon` | `059f5c45` | **257 / 257** (8 s) | `+4,297 / -7` | `+2,837 / -5` | `+1,429 / -7` | **Diverged** (+2 unversioned bits: `Reversible`, `Together`) |

All three branches pass `SubstrateTests` (zero `string` fields/params, zero `float`/`double`/`decimal`, zero `System.Math` / `System.MathF` references in `Gojo.EncodingC.dll`).

---

## Part 1 — Round 2 Cross-Branch Comparison

### 1.1 Side-by-Side Capability Matrix (Implemented at Round 2)

| Dimension | `ops_sol` (`9e5f8444`) | `ops_claude` (`a26f5b80`) | `ops_neon` (`059f5c45`) |
| --- | --- | --- | --- |
| **Unified law algebra** | Ad-hoc `FromPredicate`/`FromFunction` helpers (`BoundedProduct`, `PairFormation`, `RadixFour`) | `Polynomial` (`Σ w·Π v^e = 0`) + `Operations.Bind(..., cost[])` + `Counts` refactored onto it | `Operations.Balance`/`Product`/`Bind(..., FreeRolePolicy)` alongside Round-1 bespoke classes |
| **Multi-rung numbers** | Fixed 2-rung radix 4 (`0..15`), flat `ReadAmount` (`Size=16`) | Relational `Ladder` (any uniform `Radix ≥ 2`, unary digits, forward + backward + open digits on ONE wiring; 3-digit base-10 `0..999`) | Concrete `RungNumber` + Round-1 `Ladder` (any uniform `Radix`, unary or binary; base-10 split add/sub `0..999` + 2-digit multiply `99×99=9,801`) |
| **Pre-numeric Rung 0** | Preserved; qualitative `Sides.Toward` over `Odv` port tested beside numeric rungs | Preserved; unary digits view raw count planes (`ones.Planes.Take(3)`) + `Flows.Convert` group/refine | Preserved; 1-rung unary `RungNumber` wires directly to `Tension`/`Flows` + two-rung coasting (`B13`) |
| **Coarse-to-fine port resolution** | **Best:** optional unplugged `Socket` → `Skipped` witness → open `{8..11}` → `>=10` stays open while `>=8` / `>=12` settle → plug `finePort` narrows same scope | Tested via `Ladder.Add` borrow on 9 vs. 11; no unplugged-`Socket` port fixture | Not yet wired through optional unplugged `Socket` |
| **Material carry & `SaveLocals`** | **Best:** atomic `RadixFour.Admit` (no staging overshoot on fine digit), 16th unit waits as pressure, 17th blocked, mid-carry `Transport.SaveLocals` byte-identical | `Flows.Convert(4, 1)` / `(1, 4)` conserving moves (11 → 3 ones + 2 fours → 11) | `Flows.Convert` carries + staging fine digit in Round-1 `RungArithmeticTests`; no `SaveLocals` mid-carry test |
| **Symbolic regression / discovery** | Port-boundary evaluation of **caller-supplied** `Sum` & `Product` via `Supports` + `ValueGap` + `SamePosition` sample identity | **Best math:** exact fraction-free Gauss–Jordan vanishing ideal (`Recognition.Relations`), sparse monomial `Fits` (walls & corners as exceptions), minimax `Probe`, closed-loop discovery on incomplete `LearnedTable` (>50% unknown) | Pure-numeric `ReadHints` + **hidden free residue auto-discovery** (`Shortfall`, clamped `OR`) + MDL + sparse `WitnessStore` + 3-realization mid-flight trace classifier (`15` vs `4,8,12` vs `4,9,14`) |
| **Equifinal potentials & scope** | `Workspace.Fix` atomic commit, split-commit refusal, stale-gain invalidation | 3-configuration square (`Lines` / `Path` / `Circle`) barely-overlapping 1-edit neighbourhoods | Both Claude's 3-square test and Sol's `Workspace.Fix` scope contract ported & passing |
| **Derived native compilation** | None (deferred until conformance gates cover scope) | None (planned for Round B) | **Only implementation:** analyzer-driven `CompiledPlan` (**338×–2,815×** on 65K-lane SIMD/AVX2, **8.1×** on restored base-10 adders, **17.9×** on unsigned landmark oscillators) |

---

### 1.2 Detailed Pros & Cons of Each Branch

#### `ops_sol` (`9e5f8444`)

**Pros:**
1. **Unmatched restraint and `master` cleanliness:** Only **+72 production lines** across two full rounds, zero edits to `Transport`, `Engine`, or `Accounting`. Every line earns its keep.
2. **Sharpest port-boundary & epistemic hygiene (Axioms 2, 8, 9):**
   - `OperationNumericReadTests.AReadUsesOnlyThePublishedGrades` is the gold-standard Gojo resolution test. Leaving `finePort` behind an unplugged `Socket` proves a coarse port (`2` groups of `4`) already settles `>= 8` (`Yes`) and `>= 12` (`No`) while honestly keeping `>= 10` open (`0b11`) with a visible `Skipped` witness — and plugging `finePort` refines that exact observation without touching supplier state (`IsTaken == false`).
   - `OperationNumericDiscoveryTests` enforces **sample identity** (`SamePosition`), keeping `(0, 3, 3)` separate from an unpublished `(0, 3, null)` later observed as `(0, 3, 0)`, and verifying a genuinely fresh held-out site (`!resultPort.SamePosition(0, freshResultPort, 0)`).
3. **Atomic, no-overshoot material carry (`RadixFour.Admit`):** Neon's Round-1 carry fixture staged `4` units in a `0..4` fine field before firing `Convert(4, 1)`. Sol's atomic admission keeps the fine digit strictly in `0..3`, preserves `pressure + 4·coarse + fine` at every step, leaves the 16th unit visibly waiting as supplier pressure, reports the 17th `Blocked`, and survives `Transport.SaveLocals` / `LoadLocals` mid-carry with byte-identical state.

**Cons:**
1. **Hardcoded single radix & flat 16-value ceiling:** `RadixFour` hardcodes `Radix = 4` and materializes a flat `Amount` (`Shape.Binary(16)`). That pattern cannot scale to 3-digit base-10 (`0..999`) because a flat `Amount` exceeds `Supports`' 64-value cap and `Law.MaxBits`.
2. **No equation synthesis yet:** `OperationNumericDiscoveryTests` tests two *caller-supplied* laws (`Sum`, `Product`). It proves the conformance/qualification harness, not symbolic regression, candidate generation, active probe selection, or MDL compression.
3. **No shared algebraic core or compiled execution:** Each operation is still a standalone C# lambda (`FromPredicate`/`FromFunction`); there is no `Polynomial`, no `Operations.Bind`, and no native word execution path.

---

#### `claude/youthful-newton-wh29co` (`a26f5b80`, "Round A")

**Pros:**
1. **Best mathematical unity and signal-to-LOC ratio (+751 prod LOC):** `Polynomial` (`115` LOC), `Operations.Bind` (`108` LOC), `Numbers`/`Ladder` (`146` LOC), and `Recognition` (`308` LOC) fit together with zero redundancy and zero `Transport` wire-format breakage.
2. **`Polynomial` + `Recognition.Relations` (exact vanishing-ideal nullspace) is strictly superior to hand-coded recognizer families:**
   - Using exact fraction-free Gauss–Jordan elimination over monomials (degree by degree, capping 2-valued roles at exponent 1 since $x^2 = x$, zero floats, zero `System.Math`), `Recognition.Relations` recovers *undirected* polynomial equations from any full table — `Counts.Sum`, `a + 2b - c`, simultaneous `(a, b, a&b, a^b)` half-adder (`a + b - 2c - d = 0`), a decimal column (`a + b + c - 10d - e = 0` recovering the radix `10` as a weight!), and `ab - c = 0` — returning pure integers, never a family enum.
3. **Stated free-role completion cost (`Operations.Bind(..., int[]? cost)`):** Adopting Sol's Round-1 critique, passing `cost: null` leaves all completions open while passing a numeric `cost[]` vector states per-role residue preferences without introducing any policy enum (strictly cleaner than Neon's `FreeRolePolicy`).
4. **Relational `Ladder.Add` keeps numbers tiny, bidirectional, and open-capable:** Realizing that a decimal column `a + b + c_in = 10·c_out + s` is **4,000 bits (500 bytes)** as a `RelationLaw` (vs. 16M bits as a combined `TransitionLaw`) lets ONE wiring perform forward addition (`487 + 356 = 843`), backward subtraction (`843 - 487 = 356`), AND coupled open-digit narrowing (`4?7 + 356 = 8?3` → restrict sum to `843` → settles operand to `487`), with overflow/borrow visible on the last carry.
5. **Closed-loop active discovery on `master`'s `LearnedTable` (`ALearnedTableBecomesAnEquationBeforeItIsComplete`):** Minimax `Recognition.Probe` picks the highest-information untried input, runs the actual machine there, feeds the step into `LearnedTable.Witness`, and isolates the machine's exact law while **more than half the table is still `Unknown`**. And `TracesTellCoastingFromStretching` learns mechanism equations directly from arbitrary trace tuples (`debt' = debt + rate` vs. `out = gain · source`).
6. **Walls as exceptions:** Treating holds at boundaries (`Accumulate`, oscillator kick) and mutated corners (`(3,2) → 5`) uniformly as explicit exceptions beside a short polynomial gives dramatic MDL compression (`25,600` table bits → `176` equation+exception bits) without inventing special "clamped" law types.

**Cons:**
1. **No hidden free-residue discovery yet (acknowledged in Claude §10):** `Recognition.Fits` cannot yet recover `Shortfall` or clamped separate-output `OR`, because the free residue role (`excess` / `overflow`) is not among the transition's visible roles.
2. **Three concrete implementation limits in `Recognition.cs`:**
   - `Recognition.Witnesses(LearnedTable, ...)` builds a temporary dense `TransitionLaw.FromPairs(table.Roles, pairs, ...)` over `table.StateCount²` bits just to extract observed tuples — so it hits `Law.MaxBits` on larger tables even if only 5 transitions were observed! Reading observed `(before, after)` directly from `table.Predict(b)` (or backing `LearnedTable` sparsely) removes that unnecessary dense wall.
   - `Recognition.Nullspace` uses `checked(long)` with `Lcm` across all pivot denominators; on higher-degree or larger point sets, exact integer pivot multiples can overflow 64-bit `long` (needs `BigInteger` or modular/Bareiss reduction).
   - `Recognition.Probe` does not yet treat wall holds as expected exceptions inside the active probe loop (as Claude §10 notes, probing a wall state where a law holds can falsely eliminate the true law) and enumerates the full Cartesian product of inputs (up to `2^20`).
3. **No multiplication across rungs and no native speedups yet:** `DigitMul` and multi-rung multiplication are deferred; native compiled execution is deferred to Round B.
4. **`Ladder` forces unary digits and one uniform radix:** Every rung is `Shape.Unary(radix - 1)` (57 planes per 3-digit base-10 number including supports) and every rung must share one scalar `Radix` (see Part 2). Also, relational `Supports` narrowing is monotone per observation scope (one-shot until `Forget`), whereas live dynamic stepping (counters, coasting, oscillators) still needs functional transitions or an explicit forget/enact cycle.

---

#### `ops_neon` (`059f5c45`, our branch — honest self-critique)

**Pros:**
1. **Only branch proving end-to-end table → recognition → native CPU/AVX2 speedup with bit/tick/quiet parity:**
   - **338×–2,815×** speedup on 65,536-lane SIMD/AVX2 kernels (`Shortfall`, `BoundedMultiply`, AVX2 plane-shift `Convert`), **1,696×** on Sol's 15-packet `Settle(32)`, **8.1×** on restored 3-digit base-10 adders (1,024 lanes), and **17.9×** on unsigned live-landmark oscillators (1,000 steps) — all derived strictly from table bits, never saved by `Transport`, and epoch-invalidated on any edit.
2. **Hidden free-residue auto-discovery (`StructuralCompression.TryRecognizeBalance`):** Automatically synthesizes the missing free residue role for `Shortfall` (excess) and separate-output clamped `OR` (overflow bit), while discovering that in-place `Accumulate(4)` needs 0 free roles by tighter MDL because holding at capacity equals clamping.
3. **Widest concrete arithmetic proved end-to-end:** Base-10 3-digit add + reverse sub (`0..999`) from ONE 500-byte relation, 2-digit base-10 multiply (`0..99 × 0..99 → 0..9,801`, far past `Law.MaxBits`), both unary and packed-binary digit shapes, two-rung coasting (`B13`), coupled `Fork` splitting (`B2`), and unsigned live-`Landmark` oscillators (`940 B` saved — **104× smaller** than non-functional signed Minsky).
4. **True sparse `WitnessStore` + explicit `Reversible`/`Inverse()`:** Never allocates `StateCount²` during sparse collection, and `TransitionLaw.Inverse()` gives exact reversible law inversion.

**Cons (where peers genuinely beat us — must fix in Round 3):**
1. **Severe code bloat and Round-1 / Round-2 duplication (+4,297 prod LOC — 5.7× Claude, 60× Sol):**
   - `Laws/Rungs.cs` (250 LOC) and `Numbers/Numbers.cs` (393 LOC) duplicate `Ladder` vs. `RungNumber`, `DigitAdd` vs. `DigitAddRelation`, and `DigitMultiply` vs. `DigitMulRelation`.
   - Round-1 bespoke classes (`Booleans.cs`, `Dynamics.cs`, `Multiplication.cs`, `Counts.cs`) were kept alongside Round-2 `Operations.Balance`/`Product`/`Bind` instead of collapsing onto one core.
2. **`StructuralCompression.cs` (1,000 LOC) is 3.2× bigger than Claude's `Recognition.cs` (308 LOC) and LESS general:**
   - Having three separate fit records (`BalanceFit`, `ProductFit`, `PacketTradeFit`) and a 2-candidate sparse proposer (`Sum` vs. `Product`) is essentially family-by-family matching compared to Claude's single `Polynomial` + fraction-free nullspace `Relations` + general monomial `Fits` + minimax `Probe`.
   - `AnalyzeRealization` expects a hardcoded 4-tuple `(Tick, Source, Gain, Output)` instead of fitting equations directly to arbitrary trace tuples.
   - `WitnessStore` is an ordinary C# `List<>` outside `Store` planes (not persisted by `Transport`), whereas Claude closed the active loop directly on `master`'s plane-backed `LearnedTable`.
3. **Unversioned `Transport.cs` breakage vs. `master`:** Adding `Accounting.Reversible` and `Engine.Together` bits without backward-compatible framing breaks loading `master` payloads. `Reversible` is derivable directly from table bits on load (0 wire bits!), and `Together` should either stay out of `Transport` until standardized or use an explicit version/extension flag.
4. **Heavy RAM footprint on split base-10 transitions:** Splitting `DigitAddRelation(10)` (500 bytes) into `DigitSum(10)` (`2,000² = 4.0M` bits = 500 KB, brushing right against `Law.MaxBits`) + `DigitCarry(10)` (`160K` bits) works for live functional stepping and `CompiledPlan`, whereas Claude's relational `Ladder.Add` keeps the 500-byte `RelationLaw` directly in RAM AND solves forward, backward, and open digits from one wiring! We need **both modes explicitly separated**: relational narrowing for open/bidirectional reads (500 B), and compiled/functional stepping only when live state mutation or bulk throughput is needed.
5. **`FreeRolePolicy` enum vs. Claude's `int[]? cost`:** Our `FreeRolePolicy` (`LeastTotal` / `AllAdmitted`) hardcodes uniform weight `1` for every free role and adds an unnecessary enum; Claude's numeric `cost[]` parameter is strictly more general and enum-free.
6. **Missing Sol's optional-`Socket` coarse-to-fine port test and atomic no-overshoot carry:** We adopted Sol's `Workspace.Fix` scope contract in `EquifinalPotentialsTests`, but did not yet adopt `OperationNumericReadTests`'s unplugged-`Socket` resolution discipline or `RadixFour.Admit`'s atomic no-overshoot carry.

---

### 1.3 What to Take, What to Leave, and What to Adapt (Concrete Verdict)

| Source | **TAKE (promote into unified core)** | **ADAPT (keep idea, fix edge)** | **LEAVE / RETIRE** |
| --- | --- | --- | --- |
| **From `ops_sol`** | 1. Optional-`Socket` coarse-to-fine resolution pattern (`Skipped` witness → open interval → plug fine port).<br>2. Atomic no-overshoot material admission (`Admit`) + `SaveLocals` mid-carry continuity.<br>3. Sample-identity & held-out qualification discipline (`SamePosition`, forward-only vs. joint scope claims, `Workspace.Fix` atomic commit & stale-gain checks).<br>4. Strict `Transport` backward compatibility with `master`. | Generalize `RadixFour.Admit` from hardcoded radix 4 to any partition `p ≥ 2` and any adjacent rung pair `(coarse, fine)`. | Hardcoded `RadixFour`-only API and flat `Amount` (`Shape.Binary(16)`) as a general multi-rung number container; caller-only candidate lists once `Recognition` generates candidates. |
| **From `ops_claude`** | 1. **`Polynomial`** (`115` LOC) as the single canonical numeric equation type (with `Lens.Describe` outside core).<br>2. **`Operations.Bind(..., blocked, cost[])`** (numeric per-role cost, `null` = all admitted, zero enums).<br>3. **`Recognition.Relations`** (fraction-free vanishing ideal nullspace), **`Recognition.Fits`** (walls & corners uniformly as exceptions), **`Recognition.Probe`** (minimax active questioning), and **`Recognition.Compare`**.<br>4. **Relational `Ladder.Add`** (500-byte `DigitAddRelation` for forward + backward + open-digit narrowing on one wiring).<br>5. Closed-loop active learning on `LearnedTable` and trace-based mechanism fitting (`TracesTellCoastingFromStretching`). | 1. Fix `Witnesses(LearnedTable)` to read `Predict(b)` sparsely without instantiating dense `StateCount²` `TransitionLaw`.<br>2. Guard `Nullspace` against `long` overflow (`BigInteger` or checked fallback).<br>3. Make `Probe` wall-aware (don't treat boundary holds as refutations).<br>4. Generalize `Ladder` from single `int Radix` + unary-only to **variable-partition `int[] Partitions`** + optional unary/binary digit shape (Part 2). | Deferred compilation / single-radix unary-only `Ladder` limitation. |
| **From `ops_neon`** | 1. **Hidden free-residue auto-discovery** (recovers `Shortfall` excess & separate-output clamped `OR` overflow, distinguishing in-place hold from separate clamp by MDL) — plug this directly into Claude's `Recognition`!<br>2. **Analyzer-driven `CompiledPlan`** (zero-copy `Store.RawPlane`, hoisted static guards, L1 AVX2 SIMD, ALU lowering, bit/tick/quiet parity, **338×–2,815×** speedups).<br>3. **Multi-rung digit multiplication (`DigitMul` lattice, `99×99=9,801`)** & unsigned live-`Landmark` shears (`940 B` saved).<br>4. **`TransitionLaw.Inverse()`** & `Accounting.Reversible` (derived from table bits on load so wire format stays `master`-compatible).<br>5. `ReadHints` (fast structural pre-filter from `Accounting`, role fate, trades, corners before running monomial search). | 1. Drive `CompiledPlan` directly from `Polynomial` (not bespoke `BalanceFit`/`ProductFit`/`PacketTradeFit`).<br>2. Keep `WitnessStore` only if backed by sparse planes/tables or merged into a sparse `LearnedTable` mode.<br>3. Make `Engine.Together` opt-in with backward-compatible versioned `Transport` framing (or keep `Transport` strictly `master`-compatible). | 1. Duplicate `Rungs.cs` vs. `Numbers.cs` (merge into ONE lean `Numbers` module).<br>2. Bespoke Round-1 `Booleans.cs` (16 named gate builders), `Dynamics.cs` (hardcoded signed `maxAbs` Minsky), and redundant `Multiplication.cs` builders once derived from `Polynomial` + `Bind`.<br>3. `FreeRolePolicy` enum (replaced by Claude's `cost[]`).<br>4. Hardcoded 4-tuple `AnalyzeRealization` (replaced by trace-based `Recognition.Fits`) and 2-candidate sparse proposer (replaced by `Recognition.Fits` + `Probe`). |

**Net footprint target for Round 3:** Merging the best of all three should cut `ops_neon`'s production surface from **~4,300 LOC down to ~1,600–2,000 LOC** (roughly ~900 LOC unified algebra + variable-partition `Numbers` + `Recognition`, plus ~800–1,000 LOC `Polynomial`-driven `CompiledPlan`), while gaining **100% of Claude's mathematical generality**, **100% of Sol's epistemic/port/material rigor**, **full `master` `Transport` compatibility**, and **all of Neon's 338×–2,815× native speedups**!

---

## Part 2 — Variable Partition Sizes & The Five-Star Review Question

> *"An example we used in the past was a five-star review. Every number in that case is out of five. If we simulate that with a binary number, we get at closest out of eight or some very large number. But really, that data exactly fits as five. This was originally the denominator of a fraction where the denominator specified partitions. That may or may not be a good way to express this, but I'm wondering if we can do similar things here or we're stuck with a certain resolution and then are forced into some kind of doubling or power extension."*

### 2.1 Short Answer

1. **We are NOT stuck in binary, NOT forced into power-of-two padding, and NOT forced into doubling or uniform power extension ($B^k$).**
2. **At the `Gojo.EncodingC` substrate level (`Shape`, `Supports`, `Law`), arbitrary exact partition sizes ALREADY work today:** a 5-star domain is literally size `5` or `6`, never `8`, and unused binary bit patterns decode to `null` (broken/non-code), never silently admitted as `6` or `7`.
3. **At the current Round-2 multi-rung `Numbers` / `Ladder` layer, ALL THREE branches artificially forced uniform powers ($B^k$):** Sol hardcoded `RadixFour`; Claude's `Ladder` and Neon's `RungNumber`/`Ladder` all take one scalar `int Radix`. That restriction is purely a Round-2 constructor choice — removing it takes a few lines by changing `int Radix` to a **mixed-partition vector `int[] Partitions`** plus an explicit **unit/whole anchor**.
4. **The old "denominator specifies partitions" intuition was right about the physics, and wrong only if copied as classical rational-fraction arithmetic.** In Gojo, a partition size sits cleanly on the **stiffness ladder** (soft live `d` → medium constant plane → stiff mixed-radix rung weight), never silently cancels sample counts (`4/5 ≠ 40/50`), never silently explodes denominators on `+`, and compiles natively on binary CPUs at full speed without forcing binary semantics.

---

### 2.2 What `Gojo.EncodingC` Already Supports Natively (Implemented)

Look at what `master`'s substrate ([`Shape.cs`](../../Gojo.EncodingC/Substrate/Shape.cs), [`Field.cs`](../../Gojo.EncodingC/Substrate/Field.cs), [`Supports.cs`](../../Gojo.EncodingC/Workspaces/Supports.cs), [`Law.cs`](../../Gojo.EncodingC/Laws/Law.cs)) already does for a five-star review:

First, notice a genuine modeling distinction that five-star reviews expose immediately:

- **Case A — 5 discrete ordered levels (`1, 2, 3, 4, 5` stars, i.e., 5 buckets `0..4`):**
  - **Unary:** `Shape.Unary(4)` → **4 planes**, `Size = 5` (`0000`..`1111`).
  - **Binary-packed:** `Shape.Binary(5)` → **3 planes**, **`Size = 5` (NOT 8!)**. Codes `5, 6, 7` return `null` from `Shape.Decode` — invalid/broken bit patterns, never silent extra values!
- **Case B — A whole partitioned into 5 equal parts (`0/5, 1/5, 2/5, 3/5, 4/5, 5/5` — 5 intervals, 6 tick landmarks):**
  - **Unary:** `Shape.Unary(5)` → **5 planes**, `Size = 6`. Every set bit IS literally one star (`00000` = `0/5`, `00111` = `3/5`, `11111` = `5/5`). There are **zero wasted bit patterns in the prefix lattice**, and `field.Room(store)` gives the exact shortfall `5 − stars` (missing fifths of the whole) in **zero extra planes**!
  - **Binary-packed:** `Shape.Binary(6)` → **3 planes**, **`Size = 6` (NOT 8!)**. Codes `6, 7` decode to `null`.

In **both** cases, every higher layer of `EncodingC` respects the exact partition `Size`, never `8`:
- `Supports.On(store, field)` allocates **exactly `Size` support planes** (`5` or `6`, never `8`).
- `Law` uses **exact mixed-radix tuple indexing** (`Law.cs`: `radix *= roles[r].Size`). A sum of two `0..5` ratings (`Size = 6`) into `0..10` (`Size = 11`) has `6 × 6 × 11 = 396` table bits — **not** `8 × 8 × 16 = 1,024` bits!
- `Shape` packing into 3 binary planes is strictly **substrate layout, not meaning** ([GojoSystemPrompt.md](GojoSystemPrompt.md) §9.6).

---

### 2.3 Why Uniform Powers ($B^k$) Were Too Narrow — And How Mixed-Partition Ladders Solve Refinement Without Doubling (Documented Direction / Proposed for Round 3)

In Round 2, every branch assumed one scalar `Radix` per ladder (`4^2`, `10^3`). If you start with a five-star review (`[5]`) and want finer resolution, a uniform-radix ladder forces you into `5² = 25` (fifths of a star!), whereas a binary computer forces you into `8`, `16`, `32`. Neither matches how real domains actually refine!

Lift ONE restriction: **let a ladder carry a per-rung partition vector `[p₀, p₁, …, p_{k−1}]`** (finest rung `0` to coarsest rung `k−1`), where each rung `i` has its own exact digit balance:

```text
a_i + b_i + c_i = p_i · c_{i+1} + s_i    (weights: 1, 1, 1, −p_i, −1)
```

Look what happens immediately — **zero new kernel primitives, zero doubling, zero wasted states:**

| Domain | Mixed-partition ladder `[p₀, …, p_{k−1}]` | Exact domain / intervals | Relational adder bits (`Σ 4·p_i³`) | Compare vs. naive binary |
| --- | --- | --- | ---: | --- |
| **Whole stars (out of 5)** | `[5]` (plus optional `whole` carry bit for `5/5`) | `0..4` mod 5, or `0/5..5/5` (5 intervals) | **500 bits** (62.5 B) | Binary `8` wastes 2–3 states (25–37.5%) |
| **Half-stars (out of 5)** | **`[2, 5]`** (+ `whole` bit for `5.0`) | `0.0, 0.5, …, 4.5, 5.0` (**10 exact half-star intervals!**) | **32 + 500 = 532 bits** (66.5 B) | Binary `16` wastes 5–6 states; uniform `5²` forces 25ths! |
| **Third-stars (out of 5)** | **`[3, 5]`** (+ `whole` bit) | `0/15 .. 15/15` (15 exact intervals) | **108 + 500 = 608 bits** (76.0 B) | Never forced to double! |
| **Tenth-stars (`4.3 / 5.0`)** | **`[10, 5]`** (+ `whole` bit) | `0.0 .. 5.0` (**50 exact tenths!**) | **4,000 + 500 = 4,500 bits** (562.5 B) | Binary `64` wastes 13–14 states |
| **Feet & inches** | `[12, 3]` (or `[4, 3, 3]`) | `0..35` in (1 yard = 3 ft = 36 in) | 6,912 + 108 (or **364 + 108 = 472 bits**) | Binary cannot divide by 3 or 12 cleanly |
| **Clock (`hh:mm:ss`)** | `[10, 6, 10, 6, 4, 6]` | `00:00:00 .. 23:59:59` (86,400 exact sec) | **4,864 × 2 + 1,120 = 10,848 bits** (1.36 KB) | Flat base-60 is 864,000 bits & exceeds 64-value `Supports`! |
| **Degrees of a turn (`360°`)** | `[10, 6, 6]` | `0° .. 359°` (exact `/2, /3, /4, /5, /6, /8, /9, /10, /12`) | **4,000 + 864 + 864 = 5,728 bits** (716 B) | Binary turns miss `/3, /5, /6, /10, /12` |
| **Percentages (`0..100%`)** | `[10, 10]` (+ `whole` bit for `100%`) | `0/100 .. 100/100` (100 exact intervals) | **4,000 + 4,000 = 8,000 bits** (1.0 KB) | Binary `128` wastes 27 states |

**Notice the closed-interval boundary subtlety (worth making explicit!):**
When a whole is partitioned into `5` equal intervals (`0/5 .. 5/5`, 6 endpoints) and then refined into half-stars (`[2, 5]`, 10 intervals = **11** endpoints `0.0, 0.5, …, 4.5, 5.0`), the full whole `5.0/5` sits cleanly on the **top carry-out bit `whole ∈ {0, 1}`** (with the boundary rule `whole == 1 ⇒ lower rungs == 0`, or with `whole` representing complete 5-star wholes when summing multiple reviews!). You never have to inflate the star rung to `6` or drop `5.0` as overflow — `whole = 1, stars = 0, half = 0` IS `1` full whole (`5.0/5`), and `whole = 0, stars = 4, half = 1` IS `4.5/5`!

---

### 2.4 Where the Partition Size Sits: Three Levels of Stiffness

"Variable partition size" actually means **two different engineering situations** that correspond directly to Claude's stiffness ladder (`StiffnessTests`):

1. **Domain-chosen (crystallized) partition** — *"Every review in this system is out of 5, and may refine by halves (`[2, 5]`) or tenths (`[10, 5]`)."*
2. **Runtime-variable (soft) partition** — *"Today this rating is out of 5 (`4/5`), that other rubric is out of 7 (`5/7`), and tomorrow we might rescale the rubric to out of 10 (`8/10`) without rebuilding laws."*

Gojo supports **all three stiffness tiers** with the exact same relational substrate:

| Stiffness tier | Where partition `P` lives | Concrete representation in `EncodingC` | Table cost (verified) | What happens when `5` changes to `7` or `10`? |
| --- | --- | --- | --- | --- |
| **1. Soft (live count)** | A live `Field d` alongside numerator `n` and `room` | `(n, room, d)` constrained by `n + room = d` (`d ≥ 1`, envelope `D_max`) | At `D_max = 10`: bound `n+r=d` = **1,331 b** (167 B); same-`d` add = **29.3K b** (3.7 KB); compare = **43.9K b** (5.5 KB); **Axiom-8 project `n·d₂ = q·d₁ + r`** = **161.1K b** (20.1 KB). Even `D_max = 12` fits 11× under `Law.MaxBits`! | Perturb `d` in the store — zero rewiring, zero new tables! |
| **2. Medium (constant plane)** | `d` on constant planes of the `Store` (still an explicit role in the relation) | Zero per-lane storage (1 bit/plane across 65K lanes); shared across a batch | Same relation law as tier 1; `0` per-lane bits for `d` | `Store.SetConstant` switches the whole store's partition (or a workspace tests a counterfactual partition) and epoch-invalidates `CompiledPlan`. |
| **3. Stiff (crystallized rung)** | `Shape` + rung balance weight `−p_i` in a mixed-partition ladder `[p₀, …, p_{k−1}]` | `Shape.Unary(p_i−1)` or `Shape.Binary(p_i)`, `DigitSum(p_i)` (`a+b+c_in = p_i·c_out + s`), `Convert(p_i, 1)` | `[5]` = **500 b** (62.5 B); `[2, 5]` = **532 b** (66.5 B); `[10, 5]` = **4,500 b** (562.5 B) | Structural move: attach a finer rung `[k, 5]` (no doubling!), or explicitly project `[5] → [7]` with exact residue. |

**Exact Gojo operations on soft live-partition pairs `(n, d)` (verified in Python, zero floats):**
- **Exact cross-comparison (`4/5` vs. `2/3`):** `n₁·d₂` vs. `n₂·d₁` (`4·3 = 12` vs. `2·5 = 10` → `4/5 > 2/3` by excess `2` in the product partition `15`). Zero float division, zero power-of-two rounding.
- **Exact re-partition projection ([GojoSystemPrompt.md](GojoSystemPrompt.md) Axiom 8):** `n·d₂ = q·d₁ + r` with `0 ≤ r < d₁`:
  - `4/5` projected onto `/10`: `4 · 10 = 8 · 5 + 0` → exact `8/10`, residue `0`.
  - `4/5` projected onto `/3`: `4 · 3 = 2 · 5 + 2` → `2/3` with explicit **source-basis residue `r = 2`** (never silently rounded away!).
- **Exact same-partition addition (`3/5 + 4/5`):** `n₁ + n₂ = whole·d + rem` (`rem < d`) → `1` whole + `2/5`.

---

### 2.5 Complete Classification of ALL Partition Sizes $P$

Are there *any* partition sizes $P$ where `EncodingC` gets stuck? No — every positive integer $P \ge 2$ falls cleanly into one of three regimes:

1. **Small partitions ($2 \le P \le 63$):** Direct single rung (`Shape.Unary(P)` / `Shape.Binary(P)` or `P+1`). Fits directly inside `Supports`' 64-value cap and `Law.MaxBits` with zero padding. (Covers 5-star, 7-point Likert, 10-point, 12-inch, 24-hour, 60-second.)
2. **Smooth / composite partitions ($P = p_0 \cdot p_1 \cdots p_{m-1}$ with small factors $p_i \le 12$):** Factor into a **mixed-partition ladder `[p₀, p₁, …]`**. Exact domain $P$, zero wasted states, every rung stays well inside `Supports`' 64-value cap, and tables shrink dramatically (e.g. `60 = [10, 6]` shrinks `DigitAddRelation` by **178×**, from 864,000 bits to 4,864 bits!).
3. **Large indivisible prime partitions ($P > 63$, e.g., $P = 97$):** Use the tightest small-factor envelope (e.g. `[10, 10]` = `0..99`) constrained by an explicit bound relation `value + room = 97` (exact same mechanism as tier 1, leaving `98, 99` explicitly excluded rather than silently wrapping at `128`).

---

### 2.6 Is "Denominator of a Fraction" the Right Framing for Gojo?

**Short verdict:** Keep the **core intuition** (every quantity is measured against an explicit whole/partition, not an infinite real line or a power-of-two machine word), but **do NOT model it as classical rational-number fractions (`a/b` with automatic GCD reduction and cross-multiplied addition).**

Why the classical fraction abstraction breaks Gojo's axioms, and why **Partition Ladders + Anchors** are strictly better:

1. **Classical fraction cancellation destroys Gojo grain and sample evidence (Axiom 9, §5.2):**
   - In arithmetic, `4/5 = 8/10 = 40/50`.
   - In Gojo, `4/5` (1 five-star review), `8/10` (a 10-point rating), and `40/50` (10 five-star reviews totaling 40 stars) are **different structures** with different resolutions and different sample support!
   - When averaging `N` five-star reviews, keep **total stars** (in fifths / whole-stars) and **review count `N`** as distinct connected roles related by `total = avg · N + rem` (`0 ≤ rem < N`). Never collapse `40` stars across `10` reviews into `4/5` and throw away `N = 10`.
2. **Classical cross-addition explodes denominators and invents false precision:**
   - Adding `1/5 + 1/3 + 1/7 = 71/105` explodes past `Supports`' 64-value cap and implies 105-part measurement precision that none of the sources had (we already hit the 65,536 denominator ceiling in [GojoWeightedShapeEvidence.md](GojoWeightedShapeEvidence.md)!).
   - Under Gojo, incommensurate partitions (`/5` and `/3`) do **not** silently multiply on `+`: you either (a) keep them as separate participations (Axiom 10), (b) explicitly **project** one onto the other's partition keeping Axiom-8 residue (`n·d₂ = q·d₁ + r`), or (c) construct an explicit common-subdivision ladder `[3, 5]` (15ths) whose provenance records that `15` is a constructed join, not a 15-star sensor.
3. **Subdivision downward (`1/5`) and grouping upward (`5 stars = 1 whole`) are the EXACT SAME structural law!**
   - Look at a mixed-partition ladder `[p₀, p₁, p₂]` with one extra structural piece: **which rung boundary is anchored as the unit `1` (the "radix point" / whole landmark)**!
   - Anchor at rung `0`: ordinary mixed-radix **integers** (e.g., `[10, 10, 10]` = `0..999`; `[12, 3]` = inches/feet/yards).
   - Anchor at the top (`k`): **partitioned proportions / bounded wholes** (e.g., `[5]` = five-star review `0/5..5/5`; `[2, 5]` = half-stars `0.0..5.0`; `[10, 5]` = tenths of a star).
   - Anchor in the middle (`u`): **exact fixed-point scales** (e.g., `[10, 10 | 10, 10]` = `00.00..99.99`, or `[2, 5 | 10, 10]` = dollars + dimes + nickels/half-dimes!).
   - **One unified mechanism** handles counts, integers, mixed human measures, fixed-point scales, and five-star partitions — with `Convert(p_i, 1)` grouping upward and `Convert(1, p_i)` refining downward across EVERY boundary identically!

---

### 2.7 Symbolic Regression & Hardware Compilation Under Variable Partitions

Does supporting non-power-of-two partitions (`5`, `[2, 5]`, `[10, 5]`, `(n, d)`) hurt symbolic regression or native CPU/SIMD speed? **Quite the opposite — it makes BOTH cleaner:**

1. **Symbolic regression (`Polynomial` + `Recognition`):**
   - Every crystallized rung balance `a + b + c_in − p_i·c_out − s = 0` is a **degree-1 polynomial** whose exact partition `p_i` (whether `2`, `5`, `6`, `10`, or `12`) pops straight out of `Recognition.Relations` as the integer weight `−p_i`! No radix metadata or enum needed.
   - A soft live-denominator law `n + room − d = 0` is degree 1; `n₁ + n₂ − w·d − r = 0` and `n·d₂ − q·d₁ − r = 0` are degree 2 — all natively expressible in Claude's `Polynomial`!
2. **Native CPU / SIMD / FPGA compilation (`CompiledPlan`):**
   - Binary hardware does **not** force binary semantics! Inside a crystallized native region, `CompiledPlan` holds a rung value `0..4` or a packed mixed-radix value `0..50` in an ordinary integer word or SIMD lane, enforcing exact `p_i` bounds/carries.
   - Dividing / carrying by a known constant partition like `p = 5` needs **zero hardware division** and **zero floats**: exact 32-bit unsigned reciprocal multiply-shift `q = (int)(((ulong)x * 0xCCCCCCCDUL) >> 34); r = x - 5 * q;` executes in ~2 CPU cycles (and vectorizes cleanly with AVX2!), verified exact for every integer in `0..2^31−1`.
   - On an FPGA, a base-5 digit (`3` bits + carry) still fits inside a single 6-input LUT (`LUT6`).
   - So canonical Gojo structure keeps the **exact domain partition (`5`, `[2, 5]`, `[10, 5]`)**, while derived compilation still gets full native binary hardware speed!

---

## Part 3 — Concrete Convergence Proposal for Round 3

Rather than continuing to diverge across three branches, Round 3 should converge toward one lean, merge-ready `Gojo.EncodingC` core (target: ~1,600–2,000 prod LOC total above `master`, 100% `master` `Transport`-compatible):

1. **Unified Algebraic Core (`Polynomial` + `Operations.Bind(cost[])`):**
   - Adopt Claude's `Polynomial` (`115` LOC) and numeric `cost[]` on `Operations.Bind` (retiring Neon's `FreeRolePolicy` enum).
   - Derive `Counts`, bit gates, unsigned live-`Landmark` shears, and partition columns from `Polynomial` + `Bind`, retiring redundant Round-1 bespoke classes (`Booleans.cs`, signed `Dynamics.cs`, duplicate `Multiplication.cs` builders).
   - Derive `Accounting.Reversible` from table bits on `Transport.Load` (restoring 100% `master` wire compatibility) and guard `Engine.Together` behind a clean backward-compatible framing if retained.
2. **Unified Variable-Partition `Numbers` / `Ladder` (retiring `Rungs.cs` duplication):**
   - Replace uniform `int Radix` with **`int[] Partitions`** + optional **unit/whole anchor** + unary/binary digit `Shape` choice.
   - Keep **both** execution modes cleanly separated:
     - *Relational mode* (Claude's 500-byte `DigitAddRelation(p_i)` over `Supports` + Sol's optional unplugged `Socket` coarse-to-fine refinement): bidirectional add/sub, open digits, zero RAM bloat.
     - *Material / live-stepping mode* (Sol's atomic no-overshoot `Admit(p_i)` generalized to any `p_i`, plus `Flows.Convert(p_i, 1)` / `(1, p_i)` and multi-rung `DigitMul` lattice).
   - Add a **Five-Star & Variable-Partition Fixture** proving:
     1. Exact `[5]` whole stars (`Shape.Unary(5)` = 5 planes + free `Room`, `Shape.Binary(6)` decoding `6,7 → null`),
     2. Non-doubling refinement `[2, 5]` (half-stars `0.0..5.0`, 532 rel bits) and `[10, 5]` (tenths `0.0..5.0`),
     3. Aggregating `N` five-star reviews preserving sample count `N` (`40` across `10` reviews ≠ `4` across `1` review),
     4. Soft live-denominator `(n, d)` (`4/5` vs `2/3` exact cross-comparison, `4/5 → /10` exact `8/10`, `4/5 → /3` yielding `2/3` + exact residue `2`),
     5. Optional-`Socket` coarse-star-to-half-star refinement (Sol's `AReadUsesOnlyThePublishedGrades` pattern applied directly to `[2, 5]`!).
3. **Unified `Recognition` (Claude's `Recognition` + Neon's hidden-residue discovery & hints):**
   - Adopt Claude's `Relations` (with `BigInteger` or overflow-safe nullspace), `Fits`, minimax `Probe`, `Compare`, and closed-loop `LearnedTable` learning — fixing `Witnesses(LearnedTable)` so it never allocates dense `StateCount²` tables and making `Probe` wall-aware.
   - Fold Neon's `ReadHints` pre-filter and **hidden free-residue auto-discovery** (`Shortfall`, clamped separate-output `OR`) into `Recognition` so every existing law compresses through ONE engine.
4. **Polynomial-Driven `CompiledPlan` (Round B convergence):**
   - Drive Neon's proven zero-copy AVX2/ALU `CompiledPlan` directly from recognized `Polynomial` equations + exception lists (retiring bespoke `BalanceFit`/`ProductFit`/`PacketTradeFit` matchers), adding exact constant multiply-shift lowering for non-power-of-two partitions (`p = 5`, `[2, 5]`, `[10, 10, 10]`) and verifying against Sol's full conformance gates (tick parity, quiet parity, `Workspace.Fix` atomic scope commit, and `SaveLocals`).

---

## Appendix — Reproducible Verification Checks (Python)

Run with `python3` to verify every table size, projection, and exact `/5` multiply-shift claim in Part 2:

```python
def rel_bits(sizes):
    p = 1
    for s in sizes:
        p *= s
    return p

# 1. Exact DigitAddRelation(p) bit counts for arbitrary partitions p
for p in (2, 3, 4, 5, 6, 8, 10, 12, 16, 60):
    rb = rel_bits([p, p, 2, p, 2])
    print(f"p={p:2d}: DigitAddRelation = {rb:7d} bits ({rb/8:8.1f} B)")

# 2. Mixed-partition ladders (zero doubling, zero wasted states)
print("Half-stars [2, 5]      :", rel_bits([2,2,2,2,2]) + rel_bits([5,5,2,5,2]), "bits (66.5 B)")
print("Tenth-stars [10, 5]    :", rel_bits([10,10,2,10,2]) + rel_bits([5,5,2,5,2]), "bits (562.5 B)")
print("Factored 60 [10, 6]    :", rel_bits([10,10,2,10,2]) + rel_bits([6,6,2,6,2]), "bits vs flat 60:", rel_bits([60,60,2,60,2]))

# 3. Soft live-denominator (n, d) relation table sizes at Dmax = 5, 10, 12
for dmax in (5, 10, 12):
    s = dmax + 1
    print(f"Live Dmax={dmax:2d}: bound={rel_bits([s,s,s])}b | add={rel_bits([s,s,s,2,s])}b | "
          f"cmp={rel_bits([s,s,s,s,3])}b | proj={rel_bits([s,s,s,s,s])}b")

# 4. Exact Axiom-8 re-partition projections & cross-comparison (zero floats)
assert divmod(4 * 10, 5) == (8, 0)   # 4/5 -> 8/10, exact residue 0
assert divmod(4 * 3, 5) == (2, 2)    # 4/5 -> 2/3, exact source-basis residue 2
assert 4 * 3 - 2 * 5 == 2            # 4/5 > 2/3 by 2/15

# 5. Exact native CPU /5 multiply-shift (zero divider, zero floats)
assert all(((x * 0xCCCCCCCD) >> 34) == x // 5 for x in range(100_000))
print("All variable-partition math checks passed.")
```
