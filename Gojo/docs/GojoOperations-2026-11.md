# Operations: from physical relations to compiled hardware

Status: **proposal** (final synthesis revision, 24 November 2026, on branch
`ops_neon` synced to `master` `9b805964`). Nothing beyond existing
`Gojo.EncodingC` (including the tension layer landed on `master`) and
`Gojo/Core` primitives is built yet except where explicitly marked
**implemented**. This revision incorporates both the new `EncodingC` tension
system on `master` and the parallel second-round revisions of `ops_sol`
(`9fa5a67b`) and `ops_claude` (`e0f06b05`). It follows the epistemic discipline
of [GojoSystemPrompt.md](GojoSystemPrompt.md) (§1) and builds on
[GojoStructureClosure-2026-11.md](GojoStructureClosure-2026-11.md),
[GojoTension-2026-11.md](GojoTension-2026-11.md),
[GojoOrientationDefinitionAndUse-2026-11.md](GojoOrientationDefinitionAndUse-2026-11.md),
and [GojoEncodingConsolidation-2026-11.md](GojoEncodingConsolidation-2026-11.md).

---

## 0. The short version

1. **Operations are not four things — and every operation has three reads.**
   Keep **relation** (symmetric, bidirectional admissibility), **process**
   (directional, timed, accountable transition), and **fold** (selected output
   read with explicit loss) distinct. Give every readable result an **output
   role first** rather than unconditionally allocating a fresh carrier.
2. **Operations and numerics meet at multiplication.** Boolean logic, ordering
   (`Sides`), material tension (`Tension`, `Field.Room`), fixed-ratio scaling
   (`Convert`), and small pre-numeric counts (`Counts`) already run on `master`.
   Variable multiplication (`A × B`) cannot be isolated from partitions and
   rungs because of both a *semantic grade wall* (`ab` vs. calibrated `ab/u`)
   and a *table-product wall* (`Law.MaxBits` and the 64-value support read cap).
   **Radix is semantic** (e.g., base 60 for exact regular reciprocals), while
   power-of-2 ladders remain a clean crystallization target for hardware words.
3. **What is combined decides how.** Presence, extensive amounts, positions,
   intensive rates, and frames obey different laws. **Intensive quantities with
   matching units (temperatures, densities, prices, speeds) never add**; they
   combine through the extensives they are ratios of.
4. **Four places for an operation:**
   - **View** (`Flipped`, `Field.Room`): zero-copy reinterpretation of planes;
   - **Connection** (`Flows.Move`, `Convert`, Bond Graph `TF`): one live input
     with fixed rate/calibration;
   - **Junction** (`AND`, `Passage`, `Counts.*`, `Settling.ValueGap`,
     `Counts.Rank`, Bond Graph `MTF`): 2+ live inputs, stateless;
   - **Local structure** (`Engine` behind `Port`s): internal state, carries,
     multi-site scope, or schedule.
5. **The new tension layer on `master` (`9b805964`) already settles Phase 0's
   material groundwork and sharpens two operational rules:**
   - `Tension`, `Field.Room`, `Flows.Fulfill`/`Turn`/`When`, `Counts`,
     `Settling`, `Supports.Possible`, grouped multi-store `Fork`s,
     `Fork(isolatePorts: true)`, and full-plane `SameReference` are now
     **implemented on `master`**.
   - Live operational reads split into **accumulated** (`RelationLaw` monotonic
     narrowing, reopened only via gated `Forget` or `Workspace`) versus
     **justified** (functional `TransitionLaw`s like `Counts.*` and
     `Settling.Justify` that recompute from current inputs and go quiet when
     unchanged). `GojoTension-2026-11.md` §8 explicitly defers the **exact
     numeric gap between open values** to numeric/operations support.
6. **Conservation limits port takes; reversibility is a second invariant;
   timing & settling are distinct.** Conserving port moves compute only
   semilinear functions—never live `a · b`, which requires refinement, supplied
   flow (`Flows.Fulfill` + `T−`), or orthogonal pairing. Shears
   ($y \mathrel{+}= f(x)$) conserve information reversibly without conserving a
   sum. Doubling `B` as a relation (`B = 2B`), under `Settle()` (runs to
   capacity), and per `Step()` (`5, 10, 20, 40`) are three different acts.
7. **One bitwise act changes meaning by encoding.** At the bit rung, `AND` is
   carry/product, `OR` is clamped sum, and `XOR` is wrapped sum. On unary
   thermometer planes, **`AND` is `min`, `OR` is `max`, and `XOR` is the span
   `[min, max)`** (a segment, not a valid unary prefix).
8. **One 2×3 order-and-sign family generates Napier `log`/`exp`, `sin`/`cos`,
   coasting/multiply, and `cosh`/`sinh`.** At rung 0, the 8-state `Sides`
   oscillator cycle realizes the deferred **generator `J`** of
   [GojoOrientationDefinitionAndUse-2026-11.md](GojoOrientationDefinitionAndUse-2026-11.md)
   §5 driven by time. Because declared-order kick-drift closes (for $0 < k < 4$)
   while `StepTogether` spirals, **step order and `Step` vs. `Settle` must be
   saved with the machine.**
9. **Crystallized operations compile under two strict regimes without shaping
   the concept:** **Regime 1 (Bit-Exact)** preserves every exposed bit, packet
   boundary (`Convert` fires whole `(take, give)` packets, never bulk
   $\lfloor x\cdot\text{give}/\text{take}\rfloor$!), `T+`/`T−` overflow, and
   both **per-lane and group contention guards**; **Regime 2 (Declared
   Finite-Read)** permits math libraries (`libm` / FPU / CORDIC) only behind an
   explicit integer-tick tolerance and provenance contract.

---

## 1. The stance — and why operations and numerics meet at multiplication

Gojo does not begin with symbolic operators (`+`, `−`, `×`, `÷`, `sin`) and ask
host code to evaluate them. It builds connected structure whose local rules
absorb, narrow, route, and preserve participation
([GojoSystemPrompt.md](GojoSystemPrompt.md), §9.6). In
[`Gojo.EncodingC`](../../Gojo.EncodingC/README.md), everything that runs is one
of two table lookups across bit-sliced `Plane`s:

- a **`TransitionLaw`** changes concrete bits under checked
  [`Accounting`](../../Gojo.EncodingC/Laws/Accounting.cs) promises
  (`PreservedSum`, `Unchanged`, `NeverDecreases`, `NeverIncreases`);
- a **`RelationLaw`** narrows open `Supports` in every direction without
  mutating settled state.

### Pushback on "solidify operations first, bolt on numerics later"

Solidifying *operational grammar* first works cleanly for **Boolean logic**,
**ordering (`Sides`)**, **material tension (`Tension`, `Flows.Fulfill`)**, and
**fixed-ratio scaling (`Convert`)** — all implemented on `master`. However,
**variable multiplication (`A × B`) and multi-rung arithmetic cannot be
designed blind to partitions and rungs**, for four concrete reasons:

1. **The grade wall (semantic).** Without an explicit unit segment `u`,
   multiplying two 1D magnitudes climbs grade into a 2D area/pair (`ab`).
   Staying on a 1D carrier requires Descartes's **4-place calibrated relation**
   `u : a :: b : x`, i.e. `ab/u` (§3.3). Even when `u = 1`, `ab` and `ab/u`
   carry different structural evidence.
2. **The table-product wall (computational in `EncodingC`).** Every law table
   packs one bit per role-value tuple, refused before allocation if it exceeds
   `Law.MaxBits` (`Law.TableByteCount`, now on `master`), and one primitive
   support projection caps at 64 distinctions
   (`LawTests.OnePrimitiveReadTellsApartAtMost64Values`). A dense 3-role
   multiplication table `(a, b, a·b)` explodes immediately (Appendix A, check 7):
   - `0..3` (radix 4): `4 × 4 × 10 = 160` bits;
   - `0..4` (`ops_sol`'s `3 × 4` reference): `5 × 5 × 17 = 425` bits (54 bytes);
   - `0..7` (radix 8): `8 × 8 × 50 = 3,200` bits;
   - `0..15` (radix 16): `16 × 16 × 226 = 57,856` bits, exceeding 64 values per
     primitive support read.

   Wide multiplication must therefore **factor across coarse rungs** (lattice,
   Egyptian doubling, or quarter-squares) or **unroll across time** (coasting).
3. **The dispatch-in-disguise test (*ops_sol*, *ops_claude*).** A small table
   filled once, compared by content (`Law.SameTable`), and checked by
   `Accounting` is structure. A wide table whose meaning exists only inside the
   C# predicate that populated it is symbolic operator dispatch in disguise.
   Wide arithmetic tables must be composed from small ones or learned from
   observed processes.
4. **Numeric-ladder boundary laws and semantic radix.** Tiny bounded numeric
   checks prevent combine, scale, and repeat from blurring (Appendix A, check 7):
   - **Add-then-observe ≠ observe-then-add:** `17 + 28` observed as whole tens
     gives `(4 tens, 5)`; observing tens first gives `1 + 2 = 3` tens plus raw
     residues `7 + 8 = 15`, which re-cross another ten.
   - **Cross-rung partial products:** `(10 + 7)(20 + 8)` expands to `200`, `80`,
     `140`, `56`; each cell's place index is the *sum* of its row and column
     indices (**rung index = logarithm**).
   - **Quantizing is observable and non-associative:** In a clamped `[0, 15]`
     carrier, `(14 + 3) - 3 = 12` whereas `14 + (3 - 3) = 14`. Associativity
     holds only when overflow routes into `T+` or a coarser rung.
   - **Radix is semantic, not merely a packing choice:** Base 60 makes regular
     reciprocals (`2, 3, 5`) terminate; power-of-2 radices are a target for
     crystallized hardware packing (§6), not a mandate on conceptual structure.
   - **Denominators carry resolution only while connected to sampling:** `200/500`
     from 100 observations carries finer evidence than `2/5` only while tied to
     those observations; detached, `2/4 = 1/2`
     ([GojoSystemPrompt.md](GojoSystemPrompt.md) §5.2).

---

## 2. Lineage and what the new `master` tension layer settles

Using the canonical prompt's status terms (**implemented**, **documented
direction**, **exploratory**, **superseded**):

| Generation / Source | Status | How operations were expressed | Carry forward | Do not re-import |
| --- | --- | --- | --- | --- |
| **Drive Articles** (`Pin, Accumulate, and Merge`, `Multiplying AI Segments`, `Boolean Arithmetic with Segments`, `Sequential Math, Parallel Math`, `Mathematics Without Infinity`, `Segment Math and Symmetry`, `Numbers`) | Design intuition | **Pin** (shared context) → **Merge** (1:1 additive, 1:ALL multiplicative, 16 Boolean quadrants) → **Fold** (explicit loss). | Pin vs. fold; 4-quadrant bridge; parallel (`\`) vs. sequential (`L`) path split; squeegee fine limit; finite bounds. | Host prose orchestration or detached denominator precision. |
| **`Numerics` & `ResoEngine / Core2` & `Core4.Kernel`** | Superseded | OOP ops (`Calculate`/`Inverse`/`InPlace`); Core2 asymmetric pinning `A * B @ P`; Core4 binary `RelationFold` with `0`-partition addition override. | Host/applied asymmetry; explicit forward/inverse/in-place contrast; addition preserves grain while multiplication combines bases. | C# operator hierarchies, `0`-denominator dispatch, or coupling sign/orthogonal/`i` ([continuity review](GojoCore2Core4ContinuityReview-2026-09.md)). |
| **`Gojo` C# Kernel** ([`Relation.Operations.cs`](../Core/Relation.Operations.cs), [`Relation.LiveOutput.cs`](../Core/Relation.LiveOutput.cs), [`OperationFollow.cs`](../Structures/OperationFollow.cs), [`Equation.cs`](../Equations/Equation.cs), [Operation Shape Catalog](../axioms/Gojo%20Operation%20Shape%20Catalog.md), [Material Refinement](GojoMaterialRefinement.md), [Finite Read Fixture](GojoFiniteReadFixture.md)) | **Implemented** (kernel) | Point output sums (`AlignedProduct`), frame output multiplies (`OrthogonalProduct`) keeping opposed faces; tested in [`OperationConformanceTests.cs`](../../Tests/Gojo.Tests/Core/OperationConformanceTests.cs), [`ResolutionLadderTests.cs`](../../Tests/Gojo.Tests/Core/ResolutionLadderTests.cs), [`FiniteReadSurfaceTests.cs`](../../Tests/Gojo.Tests/Core/FiniteReadSurfaceTests.cs), [`EquationSequenceTests.cs`](../../Tests/Gojo.Tests/Equations/EquationSequenceTests.cs). | Split between projection (with residue) and live mutation; wrap/carry/radix/ratio fixtures; `TryCrystallizeFill`; contour rotation ([GojoContourRotationFixture.md](GojoContourRotationFixture.md)). | Host C# arithmetic evaluation as the sole execution path; reading output grade as the whole operation instead of the fold rule. |
| **`Gojo.Encoding` Core** ([`FiniteLawPlan.cs`](../../Gojo.Encoding/Core/Native/FiniteLawPlan.cs), [`experiments/QualitativeExecution/FiniteLawPlan.cs`](../../experiments/QualitativeExecution/FiniteLawPlan.cs)) | **Implemented** (encoding core) | Derived finite-mask compiled plan guarded by `IsCurrent` over native graph edits; open/oversized surfaces refused; zero witness allocation on compute-only reads. | Exact precedent for derived, invalidation-guarded native acceleration where native structure stays sole authority. | Treating mask indices as category tags or bypassing staleness checks. |
| **`Gojo.EncodingC` on `master` (`9b805964`)** ([README](../../Gojo.EncodingC/README.md), [GLOSSARY](../../Gojo.EncodingC/GLOSSARY.md), [GojoTension-2026-11.md](GojoTension-2026-11.md)) | **Exploratory** (active, **211/211 tests passing**) | Self-running bit tables + newly landed **tension layer**: [`Tension.cs`](../../Gojo.EncodingC/Flow/Tension.cs), [`Field.Room`](../../Gojo.EncodingC/Substrate/Field.cs), [`Flows.cs`](../../Gojo.EncodingC/Flow/Flows.cs) (`Fulfill`, `Turn`, `When`), [`Counts.cs`](../../Gojo.EncodingC/Laws/Counts.cs) (`Sum`, `Shortfall`, `Tally`, `AtLeast`, `Accumulate`, `Streak`, `Rank`), [`Settling.cs`](../../Gojo.EncodingC/Laws/Settling.cs) (`Justify`, `ValueGap`, `SameWhen`, `Latch`, `Forget`, `Enact`), [`Supports.Possible`](../../Gojo.EncodingC/Workspaces/Supports.cs), grouped multi-store [`Fork.cs`](../../Gojo.EncodingC/Flow/Fork.cs) + `isolatePorts`, `Law.TableByteCount`, full-plane `SameReference`. | Complete material tension (`T+`/`T−`), open route choice, truth-maintained justification, pre-numeric counting laws, open-value agreement gap, strict tier ranking, atomic multi-store forks, isolated port-snapshot trials, and exact bit-for-bit `Transport` save/load with zero transport format changes. | Missing: saved step kind (`Step`/`StepTogether`/`Settle`), `Reversible` shear accounting, un-deferred generator `J`, variable multiplication, exact numeric gap between open values, whole-structure transforms, stacked dynamics, and derived hardware lowering. |

### 2.1 How the new tension system on `master` shapes operations

Landing `9b805964` on `master` changes four concrete pieces of the operations
design immediately:

1. **Material supply, room views, multi-gates, and turns are already built.**
   `Tension` (`T+`/`T−` + `Absorb`/`Draw`/`Cancel`/`Deliver`), `Field.Room`,
   `Flows.Fulfill` (conserving transfer + order closure + optional receipt
   tally), `Flows.When` (multi-gate composition with `Law.MaxBits` pre-check),
   `Flows.Turn` (quiet work-following alternation), and `Supports.Possible`
   (open route choice) are live on `master`. Accountable coasting
   multiplication (`Flows.Fulfill` + clock) and two-faced oscillator states
   (`Field.Room`) now reuse existing primitives directly.
2. **Accumulated vs. justified operational reads.** `master` draws a crisp line
   ([GojoTension-2026-11.md](GojoTension-2026-11.md) §3.5):
   - **Accumulated (`RelationLaw`):** narrows open `Supports` monotonically and
     never reopens on its own—right for timeless constraints and facts learned
     about the past (reopened only via a gated `Settling.Forget` or a
     `Workspace`).
   - **Justified (`TransitionLaw` with `Unchanged` inputs, e.g. `Counts.*`,
     `Settling.Justify`):** recomputes one output from current inputs each step
     and writes only on change, settling quietly without self-contention—right
     for live derived reads (`Sum`, `Shortfall`, `AtLeast`, `Tally`, present
     gates).
   Every operational read (`A + B`, `A × B`, `A ≥ B`) must declare whether it is
   accumulated or justified (and justified laws must be declared before whatever
   reads their output).
3. **`Counts` proves the table wall in live code — and defines the exact seam
   numeric ladders must replace.** On `master`, `Counts.Tally` caps at 7 bits
   (8 exceeds `Law.MaxBits`), `Flows.When` quadruples table bits per gate, and
   `Counts.Sum`/`Shortfall` keep capacities small. Moreover,
   [GojoTension-2026-11.md](GojoTension-2026-11.md) §8 explicitly defers **the
   exact numeric gap between two open values** (`Settling.ValueGap` tests
   agreement/disagreement; `Counts.Shortfall` measures two concrete counts; how
   far apart two *open* values are belongs directly to multi-rung operations).
4. **Atomic multi-store `Fork`s and full-plane `SameReference` are live.** A
   `Fork` now shares one supposition group across multiple parent stores
   (committed or reverted all-or-nothing via `Fork.Commit()`/`Fork.Revert()`),
   rebinds wiring rows + `Life` so self-recruiting structures fork cleanly
   (rewiring itself is not committed yet), and supports `Fork(isolatePorts:
   true)` snapshotting taken ports even through `Flipped` views like `Room`
   (refusing commit). Meanwhile, `Wiring.SameReference` checks **every plane**
   of open roles, cleanly enforcing same-sample constraints like `B = 2B` and
   `a · a` diagonal squares without false aliasing.

---

## 3. Historical, cognitive, and physical foundations

### 3.1 Old Babylonian mathematics (1800–1600 BCE)

Jens Høyrup's conformal translations (*Lengths, Widths, Surfaces*, 2002;
*Algebra in Cuneiform*, 2017, ch. 1) preserve operational distinctions that
modern `+`, `−`, `×`, `÷` erase:

| Modern symbol | Old Babylonian operation | Physical meaning | Gojo `EncodingC` counterpart |
| --- | --- | --- | --- |
| `+` | ***waṣābum*** (*daḫ*, "join `j` to `S`") | **Asymmetric, in-place.** `S` stays put and keeps its identity while absorbing `j`; no new name for the sum. | Conserved material move (`Flows.Move`, `Tension.Absorb`). |
| `+` | ***kamārum*** (*gar.gar*, "heap `a` and `b`") | **Symmetric, new read/entity.** Produces a named result (*nakmartum*, or plural when constituents stay distinct); can heap heterogeneous measures. | Non-mutating `RelationLaw(a, b, sum)` or justified `Counts.Sum` into an output role. |
| `−` | ***nasāḫum*** (*zi*, "tear out `a` from `B`") | **In-place inverse of *waṣābum*.** Presupposes `a` is inside `B`; leaves diminished remainder (*šapiltum*). | Material draw (`Tension.Draw` into `Field.Room`). |
| `−` | **Comparison** ("`A` over `B`, `d` goes beyond") | **Non-mutating relation** between two separate magnitudes yielding excess or deficit. | Two-faced `Counts.Shortfall` (each way round) and `Sides.Compare`. |
| `×` | ***našûm*** (*íl/nim*, "raise") | **Scaling by a factor/rate** (raising a unit-thickness base to height `h`, or reading the measure of an existing figure). | Fixed calibration on a connection (`Flows.Convert`) or port/frame read. |
| `×` | ***šutakūlum*** (*gu₇.gu₇*, "make hold") | **Grade-raising construction** of a rectangle from two sides (*mitḫartum* / "confrontation" for squaring). | Bilinear pair / 2D structure / `UnitShape.OrthogonalProduct`, read later. |
| `×` | ***a.rá* / *alākum*** ("steps of" / "going") | **Iterated repetition** (`n` steps of `s`, or joining `s` `n` times). | Small rung table; clocked accumulation / repetition controller. |
| `×` | ***eṣēpum*** ("repeat physically", `n ≤ 9`) | **Concrete structural replication** (e.g., doubling a right triangle into a rectangle). | Topological replication of a whole structure (`n` placed instances). |
| `÷` | **Relational query + *igi* reciprocal** | Division is a question (*"what shall I raise to `d` to get `P`?"*); *igi* `n` "detaches" one of `n` equal parts. Non-regular divisors report *"7 does not divide"*. | Backward `RelationLaw` query + reciprocal table + open residue. |
| `÷ 2` | **Raise to `0;30`** vs. ***ḫepûm* / *bāmtum*** | Calibrated halving vs. breaking off the structural "moiety" (radius of a diameter, half-base). | Calibrated ratio `Convert(2, 1)` vs. structural midpoint landmark. |

Three further Babylonian takeaways matter directly:
- **Floating sexagesimal digits (CDLI P493017; Proust):** `8` steps of `7:30`
  yield `1` (`0;07,30 → 1`) or `3600` (`7,30 → 1,00,00`) depending on the
  application's scale—matching Gojo's uncalibrated connection vs. calibrated
  rung read.
- **BM 13901 problem 1 (*waṣītum*, the unit-width strip):** Turning a 1D side
  `x` into an area strip `u·x` so it can join a square `x²`, splitting the
  strip around the square, and filling the missing corner `(u/2)²` makes grade
  lifting and gnomon completion explicit (Appendix A, check 8).
- **Quarter-square multiplication (computational technique, NOT established
  Babylonian history):** While Old Babylonian square tables existed, using
  $$a \cdot b = \frac{(a + b)^2 - (a - b)^2}{4}$$
  for general multiplication is **historically speculative** (McFarland;
  tabled from the 1690s and used in 1950s analog multipliers). As an optional
  *`EncodingC` factoring method*, it remains worth benchmarking against
  lattices and doubling because a 1D square table `n <-> n²` stays
  bidirectional in one pass (Appendix A, check 7).

### 3.2 Greece, Egypt, China, India, and Early Modern Europe

- **Greece (Euclid II, V, VI, VII; Archimedes):** Discrete *arithmos* vs.
  continuous *megethos*; pre-numeric ratios via equimultiples (Book V Def. 5)
  and *anthyphairesis*; asymmetric definition of multiplication (VII Def. 15)
  with commutativity as a *theorem* (VII.16); application of areas
  (*parabolē*, *elleipsis* = deficit, *hyperbolē* = excess); Archimedes's
  steelyard balancing torque as equilibrium computation.
- **Egypt (Rhind Papyrus):** Duplation (Rhind 79: `2801 × 7` via `1, 2, 4`
  rows) reduces multiplication to **Rungs + Boolean Selection + Addition** with
  no multiplication table; false position (Rhind 24–29) solves by measuring
  residual error and scaling.
- **China (*Nine Chapters*) & India (Brahmagupta, Āryabhaṭa, lattice):** Red
  and black rods / fortunes and debts keep `T+` and `T−` physically apart;
  *fangcheng* broadcasts uniform scaling across a column; *ying buzu* (excess
  and deficit) interpolates two opposite tensions; lattice / *Treviso
  Arithmetic* (1478) shows multiple layouts for `934 × 314` along equal-place
  diagonals; **Āryabhaṭa (499 CE)** computed sines by discrete second
  differences ($1/225$ vs. exact discrete coupling $1/233.5$; Appendix A,
  check 4).
- **Viète → Descartes → Prosthaphaeresis → Napier:** Viète (*lex
  homogeneorum*, 1591) enforced strict grade rank; Descartes (*La Géométrie*,
  1637) constructed `BE = BD·BC/AB` (`ab/u`), proving same-grade multiplication
  always hides unit calibration `u`; prosthaphaeresis multiplied via trig phase
  addition; **Napier (1614)** defined logarithms kinematically with a constant-
  speed clock paired to a point relaxing toward a landmark at speed proportional
  to remaining distance.

### 3.3 Measurement theory, conservation, and physical computation

1. **Participant kinds & extensive vs. intensive (Stevens 1946; Vergnaud 1988;
   Davydov; Confrey):**
   - **Presence** (bit / occupancy): Boolean (`AND`, `OR`, `XOR`, `Flipped`).
   - **Amount / Extent (extensive):** merges and conserves (counts, lengths,
     masses, charges).
   - **Position (interval):** `pos − pos = extent`; `pos + extent = pos`; adding
     two positions directly is undefined without an origin.
   - **Rate / Ratio (intensive):** temperature, density, speed, unit price.
     **Two intensive quantities with matching units never add**; combine the
     extensives they are ratios of, or apply a rate to an extensive.
   - **Frame (origin + unit + orientation):** composes through a shared middle
     frame; retargeting scales or translates every dependent read.
2. **Conservation (CRN limit) + Reversibility/Shear invariant (Chen, Doty &
   Soloveichik 2012/2014; Fredkin & Toffoli 1982):**
   - Conserved port moves compute only **semilinear** functions (sums, constant
     multiples, `min`, `max`, comparisons)—**never the product of two live
     counts**. Accountable multiplication therefore takes one of three material
     forms: **refinement** (carry downward: 3 bags of 4 → 12 apples), **supplied
     flow** (rate read, supply drained via `Flows.Fulfill`, shortfall as `T−`),
     or **pairing** (new 2D structure, not material).
   - **Second invariant — reversibility (shears):** A shear $y \mathrel{+}= f(x)$
     with `x` `Unchanged` conserves no sum yet is exactly invertible. Symplectic
     oscillators close because each half-step is a shear. Gojo has `PreservedSum`
     for conservation, **not yet a checked accounting promise for reversibility**
     (§7, gap 9).
3. **Bond Graphs (`TF` vs. `MTF`) & Signal-Flow Graphs (Paynter 1961; Bonchi,
   Sobociński & Zanasi 2014–2025):** Fixed-ratio `TF` (`Convert`) lives on a
   2-port connection; modulated `MTF` (3rd information port) and 0-/1-junctions
   are where live variables meet; signal-flow graphs show variable `a · b` is
   exactly where linear wiring (`copy`, `add`, `scale`, `discard`) ends.
4. **Solving by lowering tension, Propagators, and Polarity:** False position,
   *ying buzu*, Heron averaging, steelyard equilibrium, CORDIC, and Radul &
   Sussman's Propagators all reduce residual mismatch locally. Grassmann
   separates outer product from scalar ratios; Spencer-Brown (*Laws of Form*)
   maps calling/crossing onto clamp (`OR`) and wrap (`XOR`); Shannon (1938)
   shows polarity (`1 = presence` vs. `1 = open circuit`) is a declared read.

---

## 4. Resolving the core design questions

### 4.1 Four places where operations live

| Place | What belongs here | Implemented in `EncodingC` (`master`) | Physical criterion |
| --- | --- | --- | --- |
| **1. View (`Surface`)** | Zero-copy reinterpretation of existing planes | [`Flipped`](../../Gojo.EncodingC/Substrate/Flipped.cs), [`Field.Room`](../../Gojo.EncodingC/Substrate/Field.cs) (anti-fill), `Odv.Reoriented` | Another reading of the same participation; copies nothing. |
| **2. Connection (`Wiring` row, 1 live input)** | Stateless fixed-parameter 2-port transfers, rates, shifts, coarse reads | `Flows.Move`, `Flows.Convert(take, give)` (*našûm* / `TF`), `Sides.Compare` | Bond-graph fixed transformer (`TF`); scalar wire gain. |
| **3. Junction (`Wiring` row, 2+ live inputs, stateless)** | Stateless combinations of 2+ live signals | 1-byte Boolean `RelationLaw`, `Flows.Passage`, `Flows.GatedMove`, `Flows.Fulfill`, `Counts.Sum`/`Shortfall`/`AtLeast`/`Tally`/`Rank`, `Settling.Justify`/`ValueGap`/`SameWhen` | Bond-graph modulated transformer (`MTF`) or 0-/1-junction. |
| **4. Local Structure (`Engine` with `Port`s)** | Anything with **internal state**, **carry ladders**, **multi-site scope**, or **schedule** | Multi-rung ladders, factored `A × B` multipliers, spatial Boolean settlement, 2×3 $m$-oscillators (`sin`/`cos`/`exp`/`log`) | Mechanism behind sealed `Port`s (Closure C7/C8) that settles on its own. |

Partial application moves smoothly across levels: fixing one operand of a
junction collapses it to a connection; making a connection's rate live lifts it
to a junction.

### 4.2 Output-role-first, directionality, timing/aliasing, and the `3 × 4` comparison

- **Output role first:** Every readable result starts as an output role (left
  open, viewed live, materialized into a carrier, or bound to a receiver)
  rather than allocating a fresh carrier on every calculation.
- **In-place vs. new result without flags:** Written roles + law kind encode it
  directly. Running an in-place process inside a grouped
  [`Fork`](../../Gojo.EncodingC/Flow/Fork.cs) turns it into a non-destructive
  candidate across all participating stores (with [`Gap`](../../Gojo.EncodingC/Workspaces/Gap.cs)
  recording the delta); `Fork.Commit()` writes back atomically across the
  group (refused if any store moved or if `PortsIsolated` is true). Note that
  `Fork.Commit()` currently commits state workspaces, not row rewiring.
- **Accumulated vs. justified reads:** Use a `RelationLaw` when backward reads
  or monotonic narrowing are wanted; use a justified functional `TransitionLaw`
  (like `Counts.*` or `Settling.Justify`) when a derived output must stay live
  as inputs change and settle quietly without contention.

#### Timing, settling, and aliasing contract for `A × B` acting on `B`

With gain `A = 2` and initial `B = 5`, five distinct uses of the would-be
product `10` must never be conflated:

| Use | Connected evidence after the first result | Timing / boundary rule |
| --- | --- | --- |
| **Read `A` and `B`** | Output role reads `10`; `A = 2` and `B = 5` stay intact. | Observational (no write). |
| **Publish / crystallize** | Live output held against an independent baseline, or captured carrier with stable tick promise. | Distinct target carrier or view. |
| **Apply once to `B`** | Receiver bound to `B` samples **`B₀ = 5`** at the before-boundary, requests `10`, and lets `B`'s port accept, partially accept (capacity `8` → shortfall `2`), or decline. | **Explicit before-boundary capture `B₀`**; never reread the mutating receiver during one settlement. |
| **Feed back to `B` (`Step` vs. `Settle`)** | Per `Step()`: `5 → 10 → 20 → 40 …` Under `Settle()`: never goes quiet while `B > 0`, **running straight to capacity!** | **Explicit time boundary and stop condition**; `Step()` and `Settle()` mean different things! |
| **Ask `B = 2B` at one instant** | Shared-sample relation (enforced by full-plane `Wiring.SameReference` on `master`). Exact scalar solution is `0`; with `B = 5` it is **incompatible**. | **Never authorizes erasing `B`.** |

#### Common comparison fixture: `3 × 4` under three reads (*ops_sol* `X1`)

One numeral pair `(3, 4)` separates all three operational reads cleanly:

1. **Bounded relation (`a, b ∈ {0..4}, c ∈ {0..16}`, 425 bits / 54 bytes, 25
   tuples):** `(3, 4) → 12`; `(3, 12) → 4`; `c = 12` alone leaves ordered pairs
   `{(3, 4), (4, 3)}` (note marginals `{3, 4} × {3, 4}` lose joint exclusion of
   `(3, 3)` and `(4, 4)`); `a = 0, c = 0` leaves `b` open; `a = 2, c = 5` gives
   empty support; binding `a` and `b` to the **same sample** leaves only
   diagonal squares `{0, 1, 4, 9, 16}`, so requesting `c = 12` contradicts.
2. **Pair construction (arrival schedule, §4.3):** Same `3` arrivals of `A` and
   `4` of `B` yield total `12` with three distinct arrival-provenance splits
   (`L`: `(0, 12, 0)`, diagonal `\`: `(3, 6, 3)`, serialized `A`-before-`B`:
   `(3, 9, 0)`).
3. **Supplied packet process (`3/tick`, supply 12, capacity 10):** Under
   indivisible packets (`Flows.Fulfill` / `Convert`), receiver steps
   `0 → 3 → 6 → 9 → 9` (3 left at supply, 1 unused room, total 12 conserved,
   shortfall visible); under partial packets, it reaches `10` (2 left at
   supply).

### 4.3 Path provenance and the discrete Leibniz rule

For monotone arrivals `(Δa, Δb)` added to `(a, b)`, new pairs satisfy:

$$\Delta(a \cdot b) = b\,\Delta a + a\,\Delta b + \Delta a\,\Delta b$$

Verified in Appendix A (check 5) for reaching `3 × 4 = 12`:

| Schedule | Swept by `A` ($b\,\Delta a$) | Swept by `B` ($a\,\Delta b$) | Same-step corners ($\Delta a\,\Delta b$) | Total |
| --- | ---: | ---: | ---: | ---: |
| **`L`-path** (3 `A`, then 4 `B`) | `0` | `12` | `0` | `12` |
| **Diagonal `\`** (3 paired `AB`, then 1 `B`) | `3` | `6` | `3` | `12` |
| **Serialized** (`A` before `B` ×3, then 1 `B`) | `3` | `9` | `0` | `12` |

In the fine-grain limit ($\Delta a\,\Delta b \to 0$, dual $\varepsilon^2 = 0$),
the two sweeps become the Drive squeegee's two triangles (`½·3·4 + ½·4·3`).
**Provenance is chosen by the question:** recording off (total only), live
recording planes, replay on demand over the selected cone, or explicit
crystallization (`TryCrystallizeFill`) that intentionally discards path history
and exposes that loss.

### 4.4 Multiplying a larger structure (`k × Structure` or `A × Structure`)

Test every structural scaling claim against one canonical fixture (*ops_sol*):
**a plate with a rectangular outline, a circular through-hole, a mounting datum
`o`, and an interface to a surrounding assembly**. The target is a **focus plus
an exposed promise**; genuine shared sites stay shared, independent
equal-coordinate sites stay distinguishable, and parts outside the focus travel
with their context.

| Request | Mechanism in Gojo / `EncodingC` | Consequences on the plate |
| --- | --- | --- |
| **1. Frame / Port Re-Calibration (*našûm*, O(1))** | Retarget the shared unit/frame at the `Port` (zero interior edits). | Dependent reads scale together; distributivity $k(a+b) = ka+kb$ holds automatically; ratios stay invariant. |
| **2. Uniform Dilation about Datum `o`** | `Attention`-selected lane broadcast $p' = o + 3(p - o)$ bound only to measure planes (never `Life`, row refs, or fixed topology). | Lengths ×3, areas ×9, mass ×9 (fixed thickness) or ×27 (thickness scaled too). Hitting an unscaled capacity or fixed mounting site surfaces **local `T+` / boundary mismatch**. |
| **3. Anisotropic 1-Axis Stretch** | Selected-lane broadcast along one exposed axis only. | Displacements and area scale ×3; **a circular hole reads as an ellipse** if participating—or stays a **connected mismatch** if circularity is demanded. |
| **4. Replication (*eṣēpum*) or Product (*šutakūlum*)** | `3 × Plate` creates 3 placed identities sharing design by reference; `Plate × Thickness` sweeps a volume basis. | Through-hole exclusion needs explicit correspondence carried into `Plate × Thickness`. |
| **5. Structure as Multiplier / Field of Rates** | Structure's count gates repetition of another motif (Euclid VII Def. 15); or each part carries its own calibration map. | Separates uniform frame edits from spatially varying rate fields. |

Uniform negative gain in a 2D plane is a half-turn plus stretch; negating one
axis reflects; zero gain collapses the selected extent and leaves the inverse
open. Candidate edits run inside a grouped `Fork` and write back only through a
checked correspondence and stale check.

### 4.5 Boolean operations across encodings and grades

Polarity (`1 = presence`, `0 = absence`, [GojoSystemPrompt.md](GojoSystemPrompt.md)
§9.7) is a declared read choice (Shannon 1938 used `1 = open circuit`, flipping
series/parallel duality). Even with `1 = presence` fixed, **the same planewise
bitwise instruction means different arithmetic by encoding** (Appendix A,
check 6):

| Bitwise act | On single bits (bit rung, `0` or `1`) | On unary thermometer planes (`k` leading `1`s) |
| --- | --- | --- |
| Bitwise `AND` (`a & b`) | **Product / carry** of `a + b` (`1·1 = 1`, `x·0 = 0`) | **`min(a, b)`** (NOT multiplication!) |
| Bitwise `OR` (a \| b) | **Clamped sum** of `a + b` (Axiom 7 stop; Spencer-Brown calling) | **`max(a, b)`** |
| Bitwise `XOR` (`a ^ b`) | **Wrapped sum** of `a + b` (Axiom 7 modulo; Spencer-Brown crossing) | **Span `[min, max)`** of length `max − min` — valid *segment*, **broken unary count** when `min > 0` |

This resolves a live scope ambiguity in
[GojoSystemPrompt.md](GojoSystemPrompt.md) §9.7: *"a product is presence of
both"* holds at the **bit rung** and on **constraint supports**, not as a
planewise `AND` across unary counts (§7, gap 4).

Across grades:
1. **Scope-relative complement:** [`Flipped`](../../Gojo.EncodingC/Substrate/Flipped.cs)
   and [`Field.Room`](../../Gojo.EncodingC/Substrate/Field.cs) invert within a
   declared scope (`Live` or capacity), never an unbounded universe.
2. **1-byte bidirectional gates (implemented):** `AND = 0x87`, `OR = 0xE1`,
   `XOR = 0x69` ([`LawTests.cs`](../../Gojo.EncodingC/Tests/LawTests.cs#L188-L205));
   a **gate acting on flow** is a `TransitionLaw` (`Flows.Passage`,
   `Flows.When`, `Tension.Route`) where one shared gate can be read many times
   while one conserved unit goes to one route.
3. **Spatial Boolean & 3+ contributors:** Over [`Paths`](../../Gojo.EncodingC/Laws/Paths.cs)
   and [`Sides`](../../Gojo.EncodingC/Laws/Sides.cs), two scopes yield four
   occupancy regions (`neither`, `A only`, `B only`, `both`) with unselected
   regions kept as conserving residue; for 3+ contributors, explicitly name
   `all`, `at least two`, `exactly one`, or `odd parity` (`Counts.Tally` on
   `master` already reads k-of-n up to 7 inputs!).
4. **Four distinct zero-like states:** `V = 0` (undetectable at current
   resolution), unselected (`Attention = 0`), empty support (contradiction),
   and Boolean `0` (absent presence).

### 4.6 Stacked physical dynamics: the 2×3 order-and-sign family and generator `J`

#### Rung 0: the 8-state `Sides` oscillator IS generator `J` driven by time

Reading position against the landmark (`x`) and velocity against rest (`v`)
with [`Sides`](../../Gojo.EncodingC/Laws/Sides.cs) (resolving zero/at-axis
events) yields an **8-state cycle** (Appendix A, check 3):

```text
turned back   moving    crossed    moving    turned back   moving    crossed    moving
  (+x, 0)  →  (+, −)  → (0, −v) → (−, −)  →   (−x, 0)  →  (−, +) → (0, +v)  →  (+, +) → (+x, 0)
  (F, +)                (I, −)                 (F, −)               (I, +)
```

[GojoOrientationDefinitionAndUse-2026-11.md](GojoOrientationDefinitionAndUse-2026-11.md)
§5 already defines the deferred order-4 generator **`J`** as a state-dependent
alternation of two involutions (`J(F, x) = (I, x)` face flip;
`J(I, x) = (F, −x)` reorient, giving `J² = Negate`, `J⁴ = id`). Reading
position on forward face `F` and velocity on opposed face `I`, the four event
states (`turned back`, `crossed`, `turned back`, `crossed`) trace
**`(F, +) → (I, −) → (F, −) → (I, +)`**—literally `J` driven by time (with the
four quadrants as the open movement states between events, collapsing to a
4-state Gray cycle `11 → 10 → 00 → 01 → 11` when `at` is unresolved)!

#### Magnitude: one 2×3 order-and-sign family (`sin`/`cos`, coast, `exp`/`log`)

```text
First order  (1 accumulator):   x ← x − m · coarse(x, shift)
Second order (2 accumulators, DECLARED ORDER — kick then drift):
  Wave 1 (kick):   v ← v − m · coarse(x, shift)     # Newton 3rd-law move with landmark
  Wave 2 (drift):  x ← x + coarse(v, shift)         # reversible shear
```

One integer feedback sign $m \in \{+1, 0, -1\}$ (never an enum) and one order
choice (1st vs. 2nd) generate six fundamental dynamics (verified in Appendix A,
check 3):

| Order | $m = +1$ (restoring) | $m = 0$ (neutral) | $m = -1$ (reinforcing) |
| --- | --- | --- | --- |
| **1st order** | **Napier relaxation / `log`**: equal ratios in equal times (`1024 → 512, 256, 128` at steps `11, 22, 34`) | Hold (`1024, 1024, …`) | **Compounding / `exp`**: `1024, 1659, 2692, 4366` |
| **2nd order** (kick-drift) | **Harmonic oscillation (`sin`/`cos`)**, complex fold $i^2 = -1$ (`64, 86, 38, −41`) | **Coasting / `r · d` multiply**, dual fold $\varepsilon^2 = 0$ (`64, 128, 192, 256`) | **Hyperbolic (`cosh`/`sinh`)**, split-complex fold $j^2 = +1$ (`64, 174, 469, 1267`) |

Key mathematical and `EncodingC` properties:
- **Exact stability & shadow energy:** Unrounded kick-drift $v' = v - kx$,
  $x' = x + v'$ has determinant `1` and conserves exact shadow energy
  $$H_{\text{shadow}} = \frac{1}{2}\big(v^2 + k x^2 - k x v\big)$$
  which is positive-definite (bounded orbits) for **$0 < k < 4$** (effective
  angle per step $2\arcsin(\sqrt{k}/2)$). At $k = 1$, starting from `(1, 0)`
  gives an exact **6-step integer hexagon** `(1,0) → (0,−1) → (−1,−1) → (−1,0)
  → (0,1) → (1,1) → (1,0)` (Appendix A, check 2).
- **Step order is structure, not a runner preference:** For `(64, 0, shift = 3)`,
  declared order closes in **54 steps** (`max |x| = 64`), whereas
  `StepTogether` spirals to `max |x| = 1187` in 400 steps (Appendix A,
  check 1). Currently `Engine` saves internal row order, **but** leaves
  `Step()` vs. `StepTogether()`, `Step()` vs. `Settle()`, and cross-engine
  order to the caller—this must be saved with the machine (§7, gap 1).
- **Waveform refinement & calibration:** A `Sides`-only constant restoring pull
  gives parabolic arcs; distance-proportional pull refines to sine/cosine.
  Āryabhaṭa's `1/225` vs. exact discrete `1/233.5` shows coupling is a
  calibration (perturbable while fitting, locked once crystallized).
- **Phase contour vs. restoring dynamics:** Exact rational contour rotation
  `(3x + 4u)/5, (−4x + 3u)/5` preserves $x^2 + u^2 = 25$, taking `(5, 0)` →
  `(3, −4)` → `(−7/5, −24/5)` (Appendix A, check 8), requiring refinement or
  declared loss at whole ticks. Wiring kick-drift `AsRelation()` across a
  multi-moment plan allows backward inference of phase or landmark from partial
  observations.
- **Powers:** Repeated multiplicative participation—sequential (clocked
  self-multiplication) or parallel (linked scaling rungs where rung `m` tracks
  `a×` rung `m − 1`).

---

## 5. Candidate architectures, recommended hybrid, and the stiffness ladder

### 5.1 Three centers of gravity — and how they combine

| Architecture | Core mechanism | Best at | Weakest at |
| --- | --- | --- | --- |
| **A. Junction Tables (*Scribe*)** | Small `RelationLaw` tables composed in lattices or learned from processes; must pass the dispatch-in-disguise test. | Exact bidirectional reads (`−`, `÷`, factoring, open supports); learned reflexes. | Table explosion; arc-consistent narrowing needs suppositions on coupled loops; static (no momentum/supply). |
| **B. Coupled Flows (*Machine*)** | `TransitionLaw`s + `Tension` + `Flows.Fulfill` + reversible shears + 2×3 $m$-oscillators. | Material conservation, `T+`/`T−` shortfall, path provenance, live dynamics, high compilability. | Latency; step order and coupling must be saved; inverse requires a separate process or temporal `AsRelation()` plan. |
| **C. Frames & Rungs (*Geometer*)** | Calibrated frames (`ab/u`), `ResolutionOf`/`SubdivisionOf` carry ladders, port/frame scaling. | O(1) whole-structure scaling, unit algebra, extensive vs. intensive clarity, semantic radices. | Projection is observation (material needs routes); dynamic motion requires contours or flows. |

**Recommended Hybrid Architecture:**
- **Geometer (C)** owns *meaning*: participant kinds, semantic radices,
  multi-rung carry ladders, explicit unit frames, and O(1) port scaling.
- **Machine (B)** owns *material flow and live dynamics*: `Tension`,
  `Flows.Fulfill`, conserved `Convert`, reversible two-wave shears, and the
  2×3 $m$-oscillator family.
- **Relational Pin-and-Fold (A + grouped `Fork`)** owns *non-destructive
  arithmetic and inverses*: Boolean, *kamārum* sum/sub, factored relational
  multiplication (measuring lattice vs. doubling vs. quarter-squares in Phase
  3), unfolded *šutakūlum* pairs, and temporal inverse plans.
- **Scribe (A crystallized)** serves as the *learned/compiled reflex* behind
  sealed `Port`s once stable (§6).

### 5.2 The 6-rung positional stiffness ladder (perturbable vs. locked rates)

Every rung already exists in `EncodingC` on `master` (zero metadata flags):

| Stiffness rung | Where the value sits in `EncodingC` | Who can perturb it? | Compiler consequence (§6) |
| --- | --- | --- | --- |
| **1. Live store count** | Writable `Store` plane (e.g. `Counts.AtLeast`'s live threshold on `master`) | Local wirings or any selecting `Workspace` | Runtime lane variable |
| **2. Unselected / ignored** | Writable `Store`, `0` in `Attention` / workspace keep layer | Owner's laws, not active supposition | Runtime variable on attended lanes only |
| **3. Behind foreign `Port`** | Owned by peer `Engine`, read through `Socket` | Owner only; local readers never mutate | Loop-invariant across local step while peer holds |
| **4. `Unchanged` role** | No wiring writes it; reading laws promise `Unchanged` | Nothing short of rewiring | Read-only operand; safe to hoist/broadcast |
| **5. Constant `Plane`** | Constant plane (e.g. `O` on `Odv`) | Refused by `Workspace` writes; needs store edit | **Compile-time uniform constant** (epoch-guarded) |
| **6. Locked in law table** | Folded into `TransitionLaw`/`RelationLaw` bits (`Flows.Convert`) | Immutable; requires installing a new law | **Hard compile-time immediate** (ALU/DSP/LUT fold) |

Moving *down* is **crystallization**; moving *up* in a `Fork` is **thawing**
(noting `Fork.Commit()` does not yet commit row rewiring). Graded stiffness
(e.g. counting readers or tension raised by change) stays open per
[GojoSystemPrompt.md](GojoSystemPrompt.md) §5.1.

---

## 6. Crystallization, compilation, and hardware acceleration (CPU / NEON / AVX / GPU / FPGA / math libraries)

> **Core rule:** Hardware must never drive the conceptual design
> ([GojoSystemPrompt.md](GojoSystemPrompt.md) §9.6). Every operation remains
> definable and runnable in canonical `EncodingC` bit-tables. Once crystallized
> behind sealed `Port`s, however, a derived backend may lower its interior to
> CPU ALU, SIMD (ARM NEON / x86 AVX), GPU, FPGA, or math-library units under a
> strict, verified contract.

This generalizes Consolidation K7 and promoted
[`Gojo.Encoding/Core/Native/FiniteLawPlan.cs`](../../Gojo.Encoding/Core/Native/FiniteLawPlan.cs)
(`IsCurrent` invalidation, compute-only witness skipping, open/oversized
refusal).

### 6.1 Two-phase lifecycle

```text
  EXPLORATORY / FLUID PHASE                    CRYSTALLIZED / HOT-PATH PHASE
  ─────────────────────────                    ─────────────────────────────
  • Canonical EncodingC bit-tables             • Sealed local Engine / Port boundary (C7/C8)
  • Open supports, partial inputs              • Epoch-guarded CompiledPlan (IsCurrent)
  • Backward narrowing, probes, forks          • Guarded settled, unaliased, uncontested lanes/groups
  • Live learning counts (stiffness 1–4)       • Runs on CPU ALU / NEON / AVX / GPU / FPGA / libm
  • Always the reference oracle                • Any open/broken/contested lane deopts to canonical
```

`Transport.Save` **always** serializes canonical Gojo structure—never a
compiled artifact or backend tag.

### 6.2 Two lowering regimes: Bit-Exact vs. Declared Finite-Read

#### Regime 1: Bit-Exact Lowering (zero approximation)

The native path produces the **exact same bit pattern** at every exposed port
(including carries, packet remainders, `T+`/`T−`, and `Evidence` planes) as
stepping canonical `EncodingC`:

- **Bit-sliced `Plane` laws → SIMD / FPGA LUTs:** Contiguous `ulong[]` planes
  lower directly to 128–512 lane bitwise circuits on ARM NEON (`vandq`,
  `vorrq`, `veorq`, `vbicq`, `vqtbl1q`), x86 AVX2/AVX-512 (`vpand`, `vpor`,
  `vpshufb`, `vpternlogq`), or **FPGA LUT6** (`Flows.Passage` = 1 LUT6:
  `64'h8048201000040201`).
- **Crystallized uniform-radix ladders → Widening ALU / SIMD `ADD` & `MUL`:**
  Power-of-2 crystallized rungs pack via bit-transpose into native integer
  vectors (`vaddq`, `vmull`, `BigMul`, `DSP48`), routing overflow directly into
  `T+` or carry-out—never wrapping silently.
- **Packet-exact `Flows.Convert(take, give)` & `Flows.Fulfill` (critical catch
  from *ops_sol*):** A packet law fires only whole indivisible `(take, give)`
  quanta, **never** bulk $\lfloor x \cdot \text{give} / \text{take} \rfloor$!
  Starting from `x = 8` with `Convert(3, 2)` and room `10`, packet firing
  transfers `2` packets (`4` target, `2` source left), whereas bulk
  $\lfloor 8 \times 2 / 3 \rfloor$ erroneously gives `5` (Appendix A, check 7).
  Exact Regime 1 lowering is therefore:
  $$p = \min\!\big(\lfloor x / \text{take}\rfloor,\; \lfloor \text{room} / \text{give}\rfloor\big), \quad x' = x - p\cdot\text{take}, \quad y' = y + p\cdot\text{give}$$
  with $p \in \{0, 1\}$ per single `Step()` and full $p$ under `Settle()` (and
  similarly capped by `order` and `tallyRoom` for `Flows.Fulfill`).
- **Discrete symplectic oscillator (integer kick-drift / CORDIC):** Lowers to
  vectorized integer shift-and-accumulate loops (`vmlaq`) or integer CORDIC
  cores with bit-exact parity. **Never silently replace a discrete Minsky loop
  with `libm sin` in Regime 1**, since $\omega_{\text{eff}} = 2\arcsin(\sqrt{k}/2)
  \neq \sqrt{k}$.

#### Regime 2: Declared Approximate / Math-Library Lowering (finite-read contract)

Lowering a crystallized waveform/transcendental read (`sin`/`cos`, `exp`/`log`,
`sqrt`) to FPU / vector `libm` / hardware CORDIC is legal **only when** the
crystallized boundary declares a **finite-read contract**
([GojoFiniteReadFixture.md](GojoFiniteReadFixture.md), Axiom 9, §8.1):
- declared input/output calibration (quantum, scale, phase origin, capacity);
- declared rounding and clipping rule (clipped tails surfaced in `T+`/`T−` or
  open endpoints);
- declared **integer-tick tolerance bound** (`±1 tick at rung r` expressed via
  `Convert` weights—zero floats in canonical structure);
- explicit provenance marking that the output used a declared lossy lowering
  rather than bit-exact oscillator replay.

### 6.3 Opcode-free structural recognition

No `"Multiply"` strings or `OpCode` enums ever enter canonical structure
([GojoSystemPrompt.md](GojoSystemPrompt.md) §9.1, §9.6). Recognition works in
three verifiable layers:
1. **Content-addressed law table hashes** (`Law.SameTable` already checks kind,
   shape, and table bits).
2. **Mechanical exhaustive verification of small tables, `Accounting`, and
   stiffness:** Prove at compile time that a carry preserves `1·fine + B·coarse`,
   a 3-role table satisfies digit add/mul, a 2-role table satisfies `x²` or
   `k·x`, a saved declared-order pair implements kick-drift, and any operand at
   stiffness rung 5–6 (§5.2) is a safe immediate constant.
3. **Sealed `Port` subgraph matching + epoch guard (`IsCurrent`):** Bindings,
   shared references (`SameReference`), capacity, accounting, and saved step
   kind are verified together; any structural edit, rewiring, or lane rebind
   invalidates `IsCurrent` and falls back cleanly.

### 6.4 Per-lane guards, group guards, and Gojo semantics

1. **Per-lane fast-path guard mask:** Only lanes matching
   `Live & Attended & SettledValidCodes & ~Aliased & ~Contested` take the fast
   path; all others execute canonically and populate exact `Evidence` planes.
2. **Group guards for shared capacity / contention (*ops_sol*):** Wherever
   multiple rows or lanes can contend for one shared stock or capacity
   ([`RuntimeTests.cs`](../../Gojo.EncodingC/Tests/RuntimeTests.cs#L138-L167)),
   a group guard either verifies non-contention before batching or falls back
   to canonical stepping for the contested group.
3. **No silent hardware wraparound & optional witnesses:** Use widening
   arithmetic (`BigMul`, NEON `vmull`) or capacity-proven non-overflowing
   widths; excess routes to `T+`, unmet draw to `T−`, zero divisor to open
   support/`Unadmitted`. When path provenance or witness lanes are demanded,
   either the compiled path populates them or replays the canonical cone on
   demand.

### 6.5 Six design guardrails ("don't accidentally block compilation")

1. **Seal operation interiors behind thin `Port`s (Closure C7/C8).**
2. **Choose radix semantically per structure, while keeping a power-of-2 ladder
   reachable via crystallization** (with explicit `PreservedSum([1, B])` carry
   weights so native integer/SIMD lowering is mechanically provable without
   forcing semantic thinking into powers of two).
3. **Keep settled `TransitionLaw` bindings separate from `Binding.Open`
   supports** and **separate small arithmetic laws from bookkeeping**.
4. **Preserve contiguous bit-sliced (`Plane`) Structure-of-Arrays storage.**
5. **Save declared step order and step kind (`Step`/`StepTogether`/`Settle`)
   with the machine** and keep conserved material separate from pure
   informational subgraphs.
6. **Keep `CompiledPlan` strictly derived and disposable**, guaranteed by
   randomized differential parity tests covering overflow, packet remainders,
   open supports, group contention, and stale `IsCurrent` invalidation.

### 6.6 Hardware lowering map

| Crystallized Gojo operation | Canonical `EncodingC` form | CPU scalar | ARM NEON / x86 SIMD | GPU | FPGA | Regime |
| --- | --- | --- | --- | --- | --- | --- |
| Small law (`Passage`, Boolean, `Odv.Addition`, `Counts.Rank`) | Bit-table sum of products across `Plane`s | Bitwise `u64` (64 lanes/op) | `Vector128`/`256`/`512`; `vqtbl1q` / `vpshufb` 4-bit LUTs | Warp bitwise + const LUT | LUT3–LUT6 (`Passage` = 1 LUT6) | **Bit-exact** |
| Unary add / remove / `min` / `max` | Thermometer prefix planes (`Unary`) | Shift / mask / `popcnt` | `vshl`/`vshr`/`vcntq`/`vandq`/`vorrq` | `__popc` / `__ballot_sync` | Thermometer shift regs | **Bit-exact** |
| Multi-rung radix-`B` add / sub / `Shortfall` | Carry ladder (`Convert(B, 1)` + rung adder) | `ADD`/`SUB` + overflow → `T+`/`T−` | `vaddq`/`vsubq` + widening overflow detect | Coalesced int kernel | Carry-select adder | **Bit-exact** |
| Packet `Convert(take, give)` & `Fulfill` | `TransitionLaw`, `PreservedSum([give, take])` | Packet `p = min(x/take, room/give)` (exact remainder) | Vector packet `div`/mul-shift + `min` + remainder | Int packet kernel | Hard `DSP48` packet unit | **Bit-exact** |
| Factored multiply `A × B` (lattice, doubling, quarter-square) | Sealed multi-rung `Engine` with `Port`s | Widening `BigMul` → rungs + `T+` | NEON `vmull_u32` / AVX `vpmuludq` | Int / tensor lane kernel | Hard `DSP48` mult + carry | **Bit-exact** |
| Whole-structure broadcast (`k × Selected`) | `Wiring` over `Attention` plane | Masked loop over packed lanes | Predicated NEON/AVX masked vector mul | Predicated warp kernel | Masked parallel DSP array | **Bit-exact** |
| 2×3 $m$-oscillator (`sin`/`cos`, coast, `exp`/`log`) | Saved declared-order `(x, v)` or `x` shear loop | Int shift-acc (Regime 1) or `libm` (Regime 2) | NEON int shift-acc (`vmlaq`) or vector trig/CORDIC | Parallel oscillator kernel | Pipelined leapfrog / CORDIC IP | **Bit-exact** (discrete) or **Declared Finite-Read** (`libm`) |
| Open `Supports` narrowing | Bitwise `RelationLaw.Narrow` | Bitwise `u64` tuple-AND/OR | SIMD bitwise plane propagation | Parallel plane propagation | LUT / AND-OR fabric | **Bit-exact** |

---

## 7. Axiom conflicts and gaps surfaced by this design

| # | Conflict or gap | Smallest clarification to resolve it |
| --- | --- | --- |
| 1 | **Step kind (`Step` vs. `StepTogether`, `Step` vs. `Settle`) and cross-engine order are not saved with the machine.** They decide whether coupled shears close or explode and whether in-place doubling steps once or saturates (§4.2, §4.6). | Save required step kind and coupled peer schedule as structure inside `Engine` and `Transport`. |
| 2 | **Product material source is unspecified** (prompt §4.4, Axiom 5). Live `a · b` has no conserving sum (§3.3). | Clarify: *"A product is a read unless a refinement or supply route (`Flows.Fulfill`) is wired; demanded but unsupplied product material becomes `T−`."* |
| 3 | **Intensive and extensive quantities are not distinguished.** Axiom 10 states additive compatibility in units, yet same-unit intensive reads never add (§3.3). | Clarify: *"Addition applies to extensive amounts and extents; intensive reads combine through the extensives they are ratios of."* |
| 4 | **§9.7 ("a product is presence of both") holds only at the bit rung and on constraint supports.** On unary thermometer planes, `AND` is `min` (§4.5). | Scope §9.7 explicitly by encoding. |
| 5 | **Deferred generator `J` meets the rung-0 oscillator.** [GojoOrientationDefinitionAndUse-2026-11.md](GojoOrientationDefinitionAndUse-2026-11.md) §5 defines `J` (`J² = Negate`, `J⁴ = id`) as deferred; the 8-state `Sides` oscillator is `J` driven by time (§4.6). | Un-defer `J` in `EncodingC` with the rung-0 oscillator as its first fixture. |
| 6 | **Kernel output-grade rule is a fold rule, not the whole operation.** "Point sums, frame multiplies" chooses fold grade, not rate/amount roles, timing, or supply. | Keep fold grade separate from process, conservation, and timing contracts (§4.2). |
| 7 | **No single canonical word for a stateless multi-operand meeting point.** Kernel uses "operation port", `EncodingC` uses "wiring row". | Adopt *junction* across prompt and `EncodingC` docs (§4.1). |
| 8 | **Tension note needs no new kind — but defers one open-value numeric gap.** Operational shortfalls reuse `T+`/`T−`, `Gap`, and `Counts.Rank` (all on `master`); [GojoTension-2026-11.md](GojoTension-2026-11.md) §8 explicitly leaves exact numeric distance between two *open* values to numeric support. | Track exact open-value distance as a first-class Phase 2 numeric deliverable rather than adding a new tension kind. |
| 9 | **Reversibility (shears) has no accounting word yet.** `Accounting` checks `PreservedSum`, while shears ($y \mathrel{+}= f(x)$) conserve information without conserving a sum (§3.3). | Add a checked `Reversible` / `Shear` accounting promise alongside `PreservedSum`. |
| 10 | **Stiffness lacks a structural account** beyond mutable vs. immutable (§5.1, §9.2). | Adopt the 6-rung positional stiffness ladder (§5.2), already instantiated by `Counts.AtLeast` on `master`. |
| 11 | **Compiled-form contract could be tighter** (§8.1, §9.1). | Codify Regime 1 (Bit-Exact, packet-accurate) vs. Regime 2 (Declared Finite-Read with integer-tick tolerance), content recognition, per-lane + group guards, and `IsCurrent` invalidation (§6). |
| 12 | **Radix is semantic, not a packing default** (§1). | Keep radix choice per-structure in axioms; treat power-of-2 packing as a crystallization/lowering target (§6.5). |
| 13 | **Denominators and resolution evidence** (*Numbers* article vs. prompt §5.2). | Clarify that a denominator carries measurement resolution only while connected to the sampling structure that produced it (§1). |

---

## 8. Concrete phased plan & falsifiable experiments (`X1–X15`)

Because `master` (`9b805964`) already implements material `Tension`,
`Field.Room`, `Flows.Fulfill`/`Turn`/`When`, `Counts`, `Settling`, grouped
multi-store `Fork`s, `Fork(isolatePorts: true)`, `Law.TableByteCount`, and
full-plane `SameReference`, **Phase 0 shrinks cleanly** to the two missing
engine/accounting primitives plus the `3 × 4` baseline comparison. Each phase
reports saved bytes (`Transport.Measure`), table bits, and negative/unresolved
tests.

### Phase 0 — Saved step kind, shear reversibility, and the `3 × 4` baseline (`X1`, `X7`)
- **Saved step kind & `Reversible` accounting:** Record required step mode
  (`Step` vs. `StepTogether`, single-step vs. `Settle`) in `Engine`/`Transport`
  (gap 1) and add a checked `Reversible` / `Shear` invariant in `Accounting`
  (gap 9).
- **`X1` (`3 × 4` common comparison, *ops_sol*):** Build one set of
  participants read three ways—bounded relation (425 bits / 54 bytes, including
  same-sample diagonal squares `{0,1,4,9,16}` contradicting `12`), arrival-pair
  construction, and `Flows.Fulfill` supplied `3/tick` packets into capacity `10`
  (`0→3→6→9→9`, total 12 conserved)—plus AND on a shared partition, verifying
  structure alone distinguishes them before reading any helper name.
- **`X7` (`Settle` vs. `Step` vs. relation):** Run "double `B`" as a shared-
  sample relation (`B = 2B` → `0` or contradiction via `SameReference`), under
  `Settle()` (saturates to capacity), and per `Step()` (`5, 10, 20, 40`).

### Phase 1 — Boolean operations across bits, unary planes, lanes, and spans (`X13`)
- Stock 16 two-input 1-byte bidirectional Boolean `RelationLaw`s + lane-parallel
  `Attention`/`Flipped` views.
- **`X13` (bit rung vs. unary rung + conserving 4-region routing):** Pin
  `AND`/`OR`/`XOR` as carry(product)/clamp/wrap at the bit rung, `min`/`max`/
  span on unary thermometer planes, and conserving 4-region spatial routing
  over `Paths`/`Sides` with open-boundary residue.
- **3+ contributors & negative tests:** Leverage `Counts.Tally` on `master` for
  `all`, `at least two`, `exactly one`, `odd parity`; assert `V = 0`,
  unselected, empty support, and Boolean `0` remain four distinct bit patterns.

### Phase 2 — Multi-rung numerics, two additions, division duals, and open-value gaps (`X12`)
- Uniform-radix carry ladder (testing radix `4` and `8`, keeping radix
  selectable semantically per structure) chained by `Flows.Convert(B, 1)`
  (`PreservedSum([1, B])`), replacing `Counts`'s single-rung table ceiling.
- Contrast **material *waṣābum*/*nasāḫum*** (conserved multi-rung transfer with
  top-rung `T+` / unmet `T−`) against **relational *kamārum*** (open-support
  ripple-carry `RelationLaw`) and **justified live sums** (recomputing cleanly
  like `Counts.Sum`).
- **`X12` (conserving refinement):** Run a carry downward (`3` coarse → `12`
  fine) preserving weighted sum; verify quotition vs. partition division and
  coarse-first signed-digit settle.
- **Exact numeric gap between open values (fulfilling `GojoTension-2026-11.md`
  §8):** Extend `Settling.ValueGap` (agreement) + `Counts.Shortfall` (concrete
  counts) to bounded open-value distance intervals across rungs.
- **Ladder boundary fixtures:** Pin `17 + 28` add-then-observe vs.
  observe-then-add, `(10 + 7)(20 + 8)` cross-rung partial products, and
  `[0, 15]` clamped non-associativity `(14 + 3) - 3 = 12 ≠ 14 + (3 - 3) = 14`.

### Phase 3 — Factored multiplication, provenance, stiffness, and plate scaling (`X2–X6`, `X11`, `X14`)
1. **`X2` & `X3` & `X4` (factoring shootout):** Measure small-table `(a, b, c)`
   backward reads (`a = c = 0` open; `a = 0, c ≠ 0` contradiction), two-rung
   place-value lattice, Egyptian doubling, and 1D quarter-squares side by side
   on table bits, saved bytes, steps, and backward factoring (`? × ? = 12`),
   enforcing the dispatch-in-disguise test.
2. **`X5` (accountable coasting on `master` tension):** Rate read + supply +
   clock wired through `Flows.Fulfill` (`target = r·d`, supply drained,
   shortfall in `T−`, optional receipt tally).
3. **`X6` (path provenance on demand):** Arrival pairing with `L`-path
   `(0, 12, 0)`, diagonal `\` `(3, 6, 3)`, and serialized `(3, 9, 0)` run with
   recording off, recording on, and replayed—confirming recorded equals replayed
   and provenance appears only when asked.
4. **`X14` (6-rung stiffness ladder):** Place one coupling rate at each rung of
   §5.2 (starting from `Counts.AtLeast`'s live store threshold down to a locked
   table) and verify perturbation/thawing rules with zero flags.
5. **`X11` (plate-with-hole-and-datum scaling):** Contrast O(1) unit/frame
   retargeting, `Attention`-selected uniform dilation hitting a fixed mounting/
   capacity limit (`T+`/mismatch), 1-axis stretch (circle → ellipse or
   connected mismatch), replication (`3 × Plate`), and `Plate × Thickness`
   through-hole exclusion inside a grouped `Fork`.

### Phase 4 — Stacked dynamics: generator `J` and the 2×3 $m$-family (`X8–X10`)
- **`X8` (un-deferring generator `J`):** Rung-0 8-state `Sides` oscillator
  (`(+x,0) → (+,−) → (0,−v) → …`) tracing `J`'s `(F,+) → (I,−) → (F,−) → (I,+)`
  event cycle; two quarters negate, four return.
- **`X9` (step order + shadow energy + `k = 1` hexagon):** Prove declared-order
  kick-drift closes (`period = 54`, `max |x| = 64`, exact $H_{\text{shadow}}$;
  `k = 1` exact 6-step hexagon) while `StepTogether` spirals (`max |x| = 1187`).
- **`X10` (2×3 order-and-sign family):** First-order relaxation (Napier
  halvings at steps `11, 22, 34`), hold, and compounding alongside second-order
  `sin`/`cos` ($m = +1$), coasting ($m = 0$), and `cosh`/`sinh` ($m = -1$),
  plus `AsRelation()` backward phase inference and constant-sign vs. linear
  pull waveform refinement.

### Phase 5 — Derived compiled backend prototype (`X15`: `NEON` / SIMD / ALU / `libm`)
Opt-in `CompiledPlan` strictly outside canonical `Transport`, guarded by
`IsCurrent`:
1. **SIMD bit-sliced `Plane` backend** (`Vector128` NEON / `Vector256` AVX2 /
   portable `Vector<ulong>`).
2. **`X15` (recognized radix-`B` ALU & packet-`Convert` fast path):** Recognize
   sealed ladders and packet conversions purely from verified table bits,
   `Accounting`, and stiffness rungs; enforce exact packet `Convert(take, give)`
   (`p = min(x/take, room/give)`), widening `ADD`/`BigMul` → `T+`, **per-lane
   AND group contention guards**, and instant fallback on open/broken/aliased/
   contested lanes or `IsCurrent == false`.
3. **Regime 2 declared finite-read lowering:** Opt-in `libm`/CORDIC waveform
   lowering behind an explicit integer-tick tolerance and provenance contract.
4. **Differential parity & honest benchmarks:** Randomized bit-for-bit parity
   for Regime 1, declared-bound parity for Regime 2, reporting saved bytes,
   lanes/sec, and deopt fallback rates.

---

## 9. Open decisions for next discussion

1. **First implementation checkpoint:** Start with `X1` + `X7` (Phase 0 `3 × 4`
   comparison + saved step kind), then `X8–X10` (oscillator & `J`), then
   `X2–X6` (factored multiply & provenance)?
2. **Cross-engine schedule encoding (gap 1):** Should coupled peer-engine step
   order live inside a parent composite `Engine` (`Fuse` / saved wave
   schedule), or as a standalone saved schedule referencing peer engines?
3. **Partial vs. atomic reservoir scaling:** When a supply reservoir runs dry
   mid-scaling, commit partial packets + `T−` draw (matching `Flows.Fulfill`
   per packet), or hold the whole multi-packet scaling atomically?
4. **Provenance on demand (record vs. replay):** Default to recording off +
   on-demand replay over the selected cone (zero steady-state plane cost), with
   live split planes opt-in?
5. **Kernel bridge scope:** Keep Phases 0–5 inside `Gojo.EncodingC` while
   adding one narrow conformance test verifying a crystallized `EncodingC`
   ladder matches [`ResolutionLadderTests.cs`](../../Tests/Gojo.Tests/Core/ResolutionLadderTests.cs)?

---

## 10. Sources

Checked during preparation of this note (marked `*`) and recalled from reading:

- *Repository sources (`master` `9b805964` + `ops_sol` `9fa5a67b` + `ops_claude`
  `e0f06b05`): [GojoSystemPrompt.md](GojoSystemPrompt.md),
  [GojoTension-2026-11.md](GojoTension-2026-11.md),
  [GojoOrientationDefinitionAndUse-2026-11.md](GojoOrientationDefinitionAndUse-2026-11.md),
  [GojoStructureClosure-2026-11.md](GojoStructureClosure-2026-11.md),
  [GojoEncodingConsolidation-2026-11.md](GojoEncodingConsolidation-2026-11.md),
  [GojoEncodingCCloudHandoff-2026-11-19.md](GojoEncodingCCloudHandoff-2026-11-19.md),
  [GojoCore2Core4ContinuityReview-2026-09.md](GojoCore2Core4ContinuityReview-2026-09.md),
  [GojoComplexInterpretations-2026-09.md](GojoComplexInterpretations-2026-09.md),
  [GojoFiniteReadFixture.md](GojoFiniteReadFixture.md),
  [GojoContourRotationFixture.md](GojoContourRotationFixture.md),
  [GojoConditionalTemporalMechanics.md](GojoConditionalTemporalMechanics.md),
  [GojoMaterialRefinement.md](GojoMaterialRefinement.md),
  [Gojo Operation Shape Catalog.md](../axioms/Gojo%20Operation%20Shape%20Catalog.md),
  [`Gojo.EncodingC`](../../Gojo.EncodingC/README.md) (`Tension.cs`, `Counts.cs`,
  `Settling.cs`, `Flows.cs`, `Fork.cs`, `Engine.cs`, `Wiring.cs`, `Field.cs`,
  `Supports.cs`, `Odv.cs`, `Sides.cs`, `Paths.cs`, `Accounting.cs`, `Gap.cs`,
  `TensionTests.cs`, `CountsTests.cs`, `RouteChoiceTests.cs`, `ForkTests.cs`,
  `LawTests.cs`, `SelfRunningTests.cs`, `FlowTests.cs`, `RuntimeTests.cs`),
  [`FiniteLawPlan.cs`](../../Gojo.Encoding/Core/Native/FiniteLawPlan.cs),
  [`Relation.Operations.cs`](../Core/Relation.Operations.cs),
  [`Relation.LiveOutput.cs`](../Core/Relation.LiveOutput.cs),
  [`OperationFollow.cs`](../Structures/OperationFollow.cs),
  [`OperationConformanceTests.cs`](../../Tests/Gojo.Tests/Core/OperationConformanceTests.cs),
  [`ResolutionLadderTests.cs`](../../Tests/Gojo.Tests/Core/ResolutionLadderTests.cs),
  [`FiniteReadSurfaceTests.cs`](../../Tests/Gojo.Tests/Core/FiniteReadSurfaceTests.cs),
  [`EquationSequenceTests.cs`](../../Tests/Gojo.Tests/Equations/EquationSequenceTests.cs).
- *Author design articles (Google Drive `Articles/` & `math/`): *Pin,
  Accumulate, and Merge*; *Multiplying AI Segments*; *Boolean Arithmetic with
  Segments*; *Sequential Math, Parallel Math*; *Segment Math and Symmetry*;
  *Mathematics Without Infinity*; *Thinking in Directed Segments*; *Numbers*;
  *Core4 Bootstrap*; *UniversalMath*; *Core 2 Prompt Axioms*.
- *History & foundations (checked directly or via `ops_sol`/`ops_claude`):
  Høyrup (2002, 2017, *Algebra in Cuneiform* ch. 1 & 3, BM 13901); CDLI P493017
  & P252955; Proust on Nippur; McFarland (*Quarter-Squares Revisited*); Euclid
  II, V, VI, VII; Fowler (*The Mathematics of Plato's Academy*); Rhind Papyrus
  (problem 79); *Jiuzhang Suanshu*; Āryabhaṭa (499 CE); *Treviso Arithmetic*
  (1478); Viète (1591); Descartes (1637); prosthaphaeresis; Napier (1614);
  Shannon (1938); Stevens (1946); Vergnaud (1988); Davydov; Grassmann (1844);
  Spencer-Brown (1969); Peirce; Paynter (1961); Bonchi, Sobociński & Zanasi
  (2014–2025); Chen, Doty & Soloveichik (2012/2014); Fredkin & Toffoli (1982);
  Walther (1971, unified CORDIC); Minsky (1972, HAKMEM 149); Hairer, Lubich &
  Wanner (2006).
- Recalled from reading (not re-verified for this revision): Brahmagupta (628
  CE); Bombelli (1572); Archimedes, *On the Equilibrium of Planes*; Confrey
  (1994); Radul & Sussman (2009); Avizienis (1961); Hölzle, Chambers & Ungar
  (1992).

---

## Appendix A: Reproducible numeric and encoding checks (Python)

Run directly with `python3` to verify every numeric claim in this note:

```python
import math
from fractions import Fraction as F

def coarse(v, s):
    """Integer coarse read through s binary rungs (truncate toward zero)."""
    return -((-v) >> s) if v < 0 else v >> s

def second_order(x, v, s, steps, in_order=True, m=1):
    """Kick, then drift: v -= m*coarse(x); x += coarse(v)."""
    path = [(x, v)]
    for _ in range(steps):
        if in_order:
            v = v - m * coarse(x, s); x = x + coarse(v, s)
        else:
            x, v = x + coarse(v, s), v - m * coarse(x, s)
        path.append((x, v))
    return path

def first_order(x, s, steps, m):
    out = [x]
    for _ in range(steps):
        x = x - m * coarse(x, s); out.append(x)
    return out

# 1. Step order: declared (symplectic) closes; StepTogether spirals.
p_ord = second_order(64, 0, 3, 400, in_order=True)
period = next(i for i, q in enumerate(p_ord[1:], 1) if q == p_ord[0])
p_tog = second_order(64, 0, 3, 400, in_order=False)
print(f"1. in-order: period={period}, max|x|={max(abs(q[0]) for q in p_ord)} | "
      f"together: max|x|={max(abs(q[0]) for q in p_tog)}")

# 2. Exact shadow energy H = (v^2 + k*x^2 - k*x*v)/2 & k=1 exact 6-step hexagon.
k = F(1, 8); x, v = F(5), F(0)
H = lambda x, v: (v * v + k * x * x - k * x * v) / 2
h0 = H(x, v); conserved = True
for _ in range(1000):
    v -= k * x; x += v; conserved &= (H(x, v) == h0)
hex_x, hex_v, hex_orbit = 1, 0, [(1, 0)]
for _ in range(6):
    hex_v -= hex_x; hex_x += hex_v; hex_orbit.append((hex_x, hex_v))
print(f"2. shadow H exact over 1000 steps: {conserved} | k=1 hexagon: {hex_orbit}")

# 3. 2x3 Order-and-Sign family (m in {+1, 0, -1}) + 8-state Sides/J phase sequence.
for m in (1, 0, -1):
    f1 = first_order(1024, 4, 24, m); f2 = second_order(64, 64, 3, 24, True, m)
    print(f"3. m={m:+d}: 1st-order x@[0,8,16,24]={[f1[i] for i in (0,8,16,24)]} | "
          f"2nd-order x={[f2[i][0] for i in (0,8,16,24)]}")
decay = first_order(1024, 4, 40, 1)
print("   Napier 1st-order halvings (512,256,128) at steps:",
      [next(i for i, q in enumerate(decay) if q <= t) for t in (512, 256, 128)])
sgn = lambda n: (n > 0) - (n < 0)
phases = []
for xv in second_order(64, 0, 3, period):
    st = (sgn(xv[0]), sgn(xv[1]))
    if not phases or phases[-1] != st: phases.append(st)
print("   8-state Sides/J cycle:", phases)

# 4. Aryabhata (499 CE): second difference = -(sine so far)/coupling, R=3438.
for c in (225.0, 1 / (2 - 2 * math.cos(math.radians(3.75)))):
    d, s, vals = 225.0, 0.0, []
    for _ in range(24):
        s += d; vals.append(round(s)); d -= s / c
    print(f"4. Aryabhata 1/{c:.1f}: first5={vals[:5]}, peak={max(vals)} (R=3438)")

# 5. Path provenance: Delta(a*b) = b*da + a*db + da*db (3 schedules for 3x4=12).
def pair_path(arrivals):
    a = b = by_a = by_b = corners = 0
    for da, db in arrivals:
        if da and db: by_a += b; by_b += a; corners += 1
        elif da:      by_a += b
        elif db:      by_b += a
        a += da; b += db
    return (a * b, by_a, by_b, corners)
print("5. L-path (3A, 4B):", pair_path([(1, 0)] * 3 + [(0, 1)] * 4),
      "| diagonal (3AB, B):", pair_path([(1, 1)] * 3 + [(0, 1)]),
      "| serialized (A,B)*3, B:", pair_path([(1, 0), (0, 1)] * 3 + [(0, 1)]))

# 6. Bitwise across encodings: bit rung vs unary thermometer rung.
th = lambda n: (1 << n) - 1
bits_ok = all((a + b) // 2 == (a & b) and min(a + b, 1) == (a | b) and (a + b) % 2 == (a ^ b)
              for a in (0, 1) for b in (0, 1))
unary_ok = all((th(a) & th(b)) == th(min(a, b)) and (th(a) | th(b)) == th(max(a, b))
               and bin(th(a) ^ th(b)).count("1") == abs(a - b)
               for a in range(9) for b in range(9))
print(f"6. bit rung (AND=carry/mul, OR=clamp+, XOR=wrap+): {bits_ok} | "
      f"unary 0..8 (AND=min, OR=max, XOR=span): {unary_ok}")

# 7. Packet Convert(3,2) vs naive DivRem + ladder edge cases + table sizes.
def convert_settle(src, room, take, give):
    p = min(src // take, room // give)
    return (src - p * take, p * give)
print(f"7. Convert(3,2) from 8: room=3 -> {convert_settle(8,3,3,2)}, "
      f"room=10 -> {convert_settle(8,10,3,2)} vs wrong bulk (8*2)//3 = {(8*2)//3}")
clamp = lambda v: max(0, min(15, v))
print(f"   17+28: add->obs={divmod(45,10)} vs obs->add=({1+2},{7+8}) | "
      f"clamp[0,15]: (14+3)-3={clamp(clamp(14+3)-3)} != 14+(3-3)={clamp(14+clamp(3-3))}")
assert all(((i + j) ** 2 - (i - j) ** 2) == 4 * i * j for i in range(16) for j in range(16))
print("   table bits (0..3, 0..4, 0..7, 0..15):",
      [(n, n * n * ((n - 1) ** 2 + 1)) for n in (4, 5, 8, 16)])

# 8. Exact 3-4-5 contour turn & BM 13901 #1.
cx, cu = F(5), F(0); pts = []
for _ in range(2):
    cx, cu = (3 * cx + 4 * cu) / 5, (-4 * cx + 3 * cu) / 5; pts.append((str(cx), str(cu)))
print(f"8. 3-4-5 turn: {pts}, x^2+u^2={cx*cx+cu*cu} | "
      f"BM 13901 #1 (s=1/2): s^2+s={F(1,2)**2+F(1,2)}, (s+1/2)^2={(F(1,2)+F(1,2))**2}")
```

Verified output (24 November 2026):

```text
1. in-order: period=54, max|x|=64 | together: max|x|=1187
2. shadow H exact over 1000 steps: True | k=1 hexagon: [(1, 0), (0, -1), (-1, -1), (-1, 0), (0, 1), (1, 1), (1, 0)]
3. m=+1: 1st-order x@[0,8,16,24]=[1024, 614, 370, 225] | 2nd-order x=[64, 86, 38, -41]
3. m=+0: 1st-order x@[0,8,16,24]=[1024, 1024, 1024, 1024] | 2nd-order x=[64, 128, 192, 256]
3. m=-1: 1st-order x@[0,8,16,24]=[1024, 1659, 2692, 4366] | 2nd-order x=[64, 174, 469, 1267]
   Napier 1st-order halvings (512,256,128) at steps: [11, 22, 34]
   8-state Sides/J cycle: [(1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0)]
4. Aryabhata 1/225.0: first5=[225, 449, 671, 890, 1105], peak=3375 (R=3438)
4. Aryabhata 1/233.5: first5=[225, 449, 671, 890, 1106], peak=3440 (R=3438)
5. L-path (3A, 4B): (12, 0, 12, 0) | diagonal (3AB, B): (12, 3, 6, 3) | serialized (A,B)*3, B: (12, 3, 9, 0)
6. bit rung (AND=carry/mul, OR=clamp+, XOR=wrap+): True | unary 0..8 (AND=min, OR=max, XOR=span): True
7. Convert(3,2) from 8: room=3 -> (5, 2), room=10 -> (2, 4) vs wrong bulk (8*2)//3 = 5
   17+28: add->obs=(4, 5) vs obs->add=(3,15) | clamp[0,15]: (14+3)-3=12 != 14+(3-3)=14
   table bits (0..3, 0..4, 0..7, 0..15): [(4, 160), (5, 425), (8, 3200), (16, 57856)]
8. 3-4-5 turn: [('3', '-4'), ('-7/5', '-24/5')], x^2+u^2=25 | BM 13901 #1 (s=1/2): s^2+s=3/4, (s+1/2)^2=1
```
