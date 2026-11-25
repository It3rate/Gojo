# Operations: from physical relations to compiled hardware

Status: **proposal** (revised 24 November 2026 on branch `ops_neon`, updated
after comparative review of `ops_sol` and `ops_claude`). Nothing beyond existing
`Gojo.EncodingC` and `Gojo/Core` primitives is built yet except where explicitly
marked **implemented**. This document follows the epistemic discipline of
[GojoSystemPrompt.md](GojoSystemPrompt.md) (§1), builds on
[GojoStructureClosure-2026-11.md](GojoStructureClosure-2026-11.md),
[GojoTension-2026-11.md](GojoTension-2026-11.md), and
[GojoEncodingConsolidation-2026-11.md](GojoEncodingConsolidation-2026-11.md),
and lays out a unified design for Boolean, additive, multiplicative, and stacked
dynamical operations—while keeping crystallized structures cleanly compilable to
CPU, SIMD (ARM NEON / x86 AVX), GPU, FPGA, and math-library backends.

---

## 0. The short version

1. **Operations and numerics meet at multiplication.** Boolean logic, ordering
   (`Sides`), material conservation, and fixed-ratio scaling (`Convert`) already
   work pre-numerically. Variable multiplication (`A × B`) cannot be isolated
   from partitions and rungs because of both a *semantic unit wall* (1D
   calibrated products require a 4th participant, the unit `1`) and a
   *computational table wall* (primitive `EncodingC` tables blow up past coarse
   radices). Solidify operation *kinds* now alongside tiny bounded numeric
   checks so combine, scale, repeat, capacity, and remainder never blur.
2. **Start from what is being combined.** Presence, extensive amounts,
   positions, intensive rates, and frames obey different laws. In particular,
   **intensive quantities with matching units (temperatures, densities, prices,
   speeds) do not add**; they combine through the extensives they are ratios of.
3. **Constants live on connections; live variables meet at junctions/structures.**
   Fixed rates (`Flows.Convert`, Paynter's `TF`), live reorientations
   (`Flipped`), and 1-byte bit gates belong on connections or views. Anything
   combining 2+ live variables (`MTF`), carrying state, spanning rungs, or
   acting across a spatial scope is a local structure behind sealed `Port`s.
4. **Conservation forbids naive material multiplication; reversibility is a
   second invariant.** Rate-independent conserving port moves compute only
   semilinear functions (sums, constant multiples, min, max, comparisons)—never
   a product of two live counts. Accountable multiplication takes one of three
   forms: *refinement* (carry downward), *supplied flow* (rate × duration from a
   reservoir, with shortfall as `T−`), or *pairing* (new 2D frame, not
   material). Shears ($y \mathrel{+}= f(x)$) conserve no sum yet are exactly
   reversible—an invariant Gojo should name alongside `PreservedSum`.
5. **Output role first; timing and aliasing decide mutation.** Give every
   readable result an output role (open, viewed, materialized, or bound to a
   receiver) before allocating a carrier. Encode in-place vs. new-result by law
   kind + written roles + `Workspace`/`Fork`, and never conflate a single
   settlement pass with a time step (`apply once` captures `B₀`; `feedback`
   needs a time boundary; simultaneous `B = 2B` at one instant is an
   incompatibility when `B ≠ 0`, never permission to erase `B`).
6. **One bitwise act changes meaning by encoding.** At the single-bit rung,
   `AND` is carry/product, `OR` is clamped sum, and `XOR` is wrapped sum. On
   unary thermometer planes, **`AND` is `min`, `OR` is `max`, and `XOR` is the
   span `[min, max)`** (a valid segment, not a valid unary prefix).
7. **One two-accumulator loop generates `sin`/`cos`, coasting/multiply, and
   `exp`/`log`.** Stepping `(x, v)` in **declared order** with feedback sign
   $m \in \{+1, 0, -1\}$ yields circular oscillation ($m = +1$, complex fold
   $i^2 = -1$), linear coasting/multiplication ($m = 0$, dual fold
   $\varepsilon^2 = 0$), and hyperbolic/Napier `exp`/`log` ($m = -1$,
   split-complex fold $j^2 = +1$). At rung 0 it collapses to a 2-bit `Sides`
   Gray cycle (`11 → 10 → 00 → 01 → 11`) building Axiom 10's order-4 generator.
   Because `Step()` closes the integer orbit while `StepTogether()` spirals,
   **step kind and coupled order must be saved with the machine.**
8. **Hardware compilation is a derived, epoch-guarded projection over
   crystallized structure—never the source of meaning.** Split lowering strictly
   into **Regime 1 (Bit-Exact)** (SIMD bitwise, widening ALU/NEON/AVX/DSP48 with
   `T+`/`T−` overflow, exact `DivRem`, integer leapfrog) and **Regime 2
   (Declared Finite-Read)** (math libraries / FPU / CORDIC permitted *only*
   behind an explicit integer-tick tolerance and provenance contract). Recognize
   subgraphs by content hash, mechanical table verification, and sealed `Port`
   patterns—never opcode enums or strings.

---

## 1. The stance — and why operations and numerics meet at multiplication

Gojo does not begin with symbolic operators (`+`, `−`, `×`, `÷`, `sin`) and ask
host code to evaluate them. It builds connected structure whose local rules
absorb, narrow, route, and preserve participation
([GojoSystemPrompt.md](GojoSystemPrompt.md), §9.6). In
[`Gojo.EncodingC`](../../Gojo.EncodingC/README.md), everything that runs is one
of two table lookups across bit-sliced `Plane`s:

- a **`TransitionLaw`** changes concrete bits under checked
  [`Accounting`](../../Gojo.EncodingC/Laws/Accounting.cs) promises (such as
  positive-weight `PreservedSum` conservation or `Unchanged` read-only roles);
- a **`RelationLaw`** narrows open `Supports` in every direction without
  mutating settled state.

### Pushback on "solidify operations first, bolt on numerics later"

Solidifying *operational distinctions* first is right for **Boolean logic**,
**ordering (`Sides`)**, **material conservation**, and **fixed-ratio scaling**
— all four already run pre-numerically in `EncodingC`. However, **variable
multiplication (`A × B`) and multi-rung arithmetic cannot be designed in
isolation from partitions and rungs**, for three concrete reasons:

1. **The unit/grade wall (semantic).** Without an explicit unit partition `1`,
   multiplying two 1D magnitudes cannot stay 1D: it creates a 2D bilinear
   structure (an area, rectangle, or ordered pair). Staying on a 1D carrier
   turns `A × B = C` into a **4-place calibrated relation** `1 : A :: B : C`
   (Descartes, 1637; §3.3). The earlier `Core4 Bootstrap` experiment hit the
   mirror image of this wall: combining two partitioned structures naturally
   multiplied their partitions, forcing an artificial `0`-partition override
   just to make addition stop multiplying.
2. **The table-product wall (computational in `EncodingC`).** Every law table
   stores one bit per combination of role values, and one primitive support read
   caps at 64 distinctions (`LawTests.OnePrimitiveReadTellsApartAtMost64Values`).
   A single 3-role multiplication table `(a, b, a·b)` explodes immediately
   (Appendix A, check 7):
   - Radix `4` (`0..3`): `4 × 4 × 10 = 160` bits (tiny, coarse);
   - Radix `8` (`0..7`): `8 × 8 × 50 = 3,200` bits;
   - Radix `16` (`0..15`): `16 × 16 × 226 = 57,856` bits, and `226 > 64`
     already breaks a single primitive support read.

   Wide multiplication therefore *cannot* be one wide primitive table. It must
   either **factor across coarse rungs/partitions** (place-value lattice/carry,
   Egyptian duplation, or quarter-square lookup) or **unroll across time**
   (clocked accumulation).
3. **Qualitative-only tests hide numeric boundary laws.** Without tiny bounded
   numeric checks alongside qualitative cases, combine, scale, and repeat look
   interchangeable until unit, capacity, and remainder show up. Three small
   numeric edge cases (verified in Appendix A, check 6) must remain true across
   every design choice:
   - **Add-then-observe ≠ observe-then-add:** `17 + 28` observed as whole tens
     gives `4` tens and remainder `5`; observing tens first gives `1 + 2 = 3`
     tens plus raw residues `7 + 8 = 15`, which re-cross another ten.
   - **Cross-rung partial products:** `(10 + 7)(20 + 8)` generates partial
     products at shifted rungs (`200`, `80`, `140`, `56`) whose rung index is
     the *sum* of its row rung and column rung—the slide rule inside long
     multiplication (`rung index = logarithm`).
   - **Bounded saturation is non-associative:** In a clamped `[0, 15]` carrier,
     `(14 + 3) - 3 = 12` whereas `14 + (3 - 3) = 14`. Algebraic identities like
     associativity hold only when overflow is routed to `T+` or a coarser rung
     rather than clipped.

**Working rule:** Solidify the *kinds* of operations, participant types,
connection-vs-structure seams, and small bounded numeric checks together now,
**without forcing one monolithic `Multiply` primitive**. Multi-rung numerics
then snap cleanly into the carry and frame seams already prepared for them.

---

## 2. Lineage: what prior iterations, repo tests, and articles already proved

Using the canonical prompt's status terms (**implemented**, **documented
direction**, **exploratory**, **superseded**):

| Generation / Source | Status | How operations were expressed | Carry forward | Do not re-import |
| --- | --- | --- | --- | --- |
| **Drive Articles** (`Pin, Accumulate, and Merge`, `Multiplying AI Segments`, `Boolean Arithmetic with Segments`, `Sequential Math, Parallel Math`, `Mathematics Without Infinity`, `Segment Math and Symmetry`, `Thinking in Directed Segments`) | Design intuition | Three stages: **Pin** (shared context without loss) → **Merge** (1:1 additive knit, 1:ALL multiplicative stretch/blend, 16 Boolean quadrant masks) → **Fold** (collapse with explicit loss). | Pin vs. fold separation; 4-quadrant Boolean/product bridge; parallel (`\`) vs. sequential (`L`) path composition; squeegee fine limit; finite bounds without infinity. | Informal host orchestration or leaving path provenance unspecified. |
| **`Numerics`** (`NumericsCore/Expressions/Operations.cs`) | Superseded | OOP `OperationBase` / `BinaryOperationsBase` subclasses (`Calculate`, `CalculateInverse`, `CalculateInPlace`). | Explicit forward/inverse pairing and in-place vs. new-result contrast. | Encoding operational meaning in C# class hierarchies. |
| **`ResoEngine / Core2`** (`Core2/Algebra`, `Core2/Boolean`, `Axioms.md` I–X) | Superseded | Asymmetric pinning `A * B @ P` (recessive host `A`, dominant transform `B`); encoding vs. action duality (`3` vs. `S_3`); Boolean as frame-relative piece families on constant partitions. | Host/applied asymmetry; Boolean over an explicit frame yielding co-present pieces rather than one bare scalar. | Coupling denominator sign, orthogonal breakout, and complex `i` into one `Axis`/`Proportion` object ([continuity review](GojoCore2Core4ContinuityReview-2026-09.md)). |
| **`Core4.Kernel`** (`RelationFold.cs`, `Core4 Bootstrap`) | Superseded | One binary `Relation(A, B)` interpreted by `RelationFold`; combining partitioned bases multiplies them, forcing a `0`-partition override to get addition. | Physical law: **addition preserves partition grain; multiplication combines or substitutes bases**. | Overloading one binary fold with `0`-denominator special-case dispatch. |
| **`Gojo` C# Kernel** ([`Relation.Operations.cs`](../Core/Relation.Operations.cs), [`Relation.LiveOutput.cs`](../Core/Relation.LiveOutput.cs), [`OperationFollow.cs`](../Structures/OperationFollow.cs), [`Equation.cs`](../Equations/Equation.cs), [Operation Shape Catalog](../axioms/Gojo%20Operation%20Shape%20Catalog.md), [Material Refinement](GojoMaterialRefinement.md), [Finite Read Fixture](GojoFiniteReadFixture.md)) | **Implemented** (kernel) | Exposed operational ports: `(portA -> portB) -> P(out,1)` = additive collapse (`AlignedProduct`); `-> S(out,0,1)` = multiplicative (`OrthogonalProduct`) keeping opposed faces before fold. Tested in [`OperationConformanceTests.cs`](../../Tests/Gojo.Tests/Core/OperationConformanceTests.cs), [`ResolutionLadderTests.cs`](../../Tests/Gojo.Tests/Core/ResolutionLadderTests.cs), [`FiniteReadSurfaceTests.cs`](../../Tests/Gojo.Tests/Core/FiniteReadSurfaceTests.cs), and [`EquationSequenceTests.cs`](../../Tests/Gojo.Tests/Equations/EquationSequenceTests.cs). | Clean split between observation (with source residue) and mutation (via `LiveOutput` / `OperationFollow` / tension); explicit unit algebra; explicit wrap/carry/radix/ratio fixtures; `TryCrystallizeFill`. | Relying on host C# evaluation (`TryReadOperationSignalFlow`) while the graph merely describes an operation; reading output grade ("point sums, frame multiplies") as the whole process rather than just the fold rule. |
| **`QualitativeExecution`** ([`FiniteLawPlan.cs`](../../experiments/QualitativeExecution/FiniteLawPlan.cs), [`FiniteLawBatchPlan.cs`](../../experiments/QualitativeExecution/FiniteLawBatchPlan.cs)) & **`PrimitiveMeasure`** ([`RaceOrder.md`](../../experiments/PrimitiveMeasure/RaceOrder.md)) | **Exploratory** | Derived finite-mask compiled plans guarded by `IsCurrent` over native graph edits; race-order temporal comparisons. | Exact pattern for **derived, invalidation-guarded native acceleration** where native structure remains sole authority. | Treating mask indices as category names or bypassing invalidation guards. |
| **`Gojo.EncodingC`** ([README](../../Gojo.EncodingC/README.md), [GLOSSARY](../../Gojo.EncodingC/GLOSSARY.md)) | **Exploratory** (active) | Self-running bit tables: `TransitionLaw` + `Accounting`, `RelationLaw` + open `Supports`, `Flipped` live views, `Workspace`/`Fork`, `Closure`. | **Already built:** 1-byte bidirectional Boolean (`AND = 0x87`, `OR = 0xE1`, `XOR = 0x69`, [`LawTests.cs`](../../Gojo.EncodingC/Tests/LawTests.cs)), bidirectional count add/sub, `Odv.Addition(Detection)`, fixed `Flows.Convert(take, give)`, 2-rung carry (`WhatDoesNotFitIsCarriedUpARung`, [`SelfRunningTests.cs`](../../Gojo.EncodingC/Tests/SelfRunningTests.cs#L401-L427)), `Sides` events, rising-edge observer ([`FlowTests.cs`](../../Gojo.EncodingC/Tests/FlowTests.cs)), `Absorb`/`Draw`/`Cancel` via `Flipped` room views ([tension note](GojoTension-2026-11.md)). | Missing: variable multiplication, saved step kind/order, whole-structure scaling, multi-site Boolean, stacked dynamics (`sin`/`cos`), reversibility accounting, and derived hardware lowering. |

---

## 3. Historical, cognitive, and physical foundations

Modern algebraic notation compresses physically distinct acts onto four
overloaded symbols (`+`, `−`, `×`, `÷`). Historical traditions, measurement
theory, and physical computation kept these acts apart—and their distinctions
map directly onto Gojo's seams.

### 3.1 Old Babylonian mathematics (1800–1600 BCE)

Jens Høyrup's conformal translations (*Lengths, Widths, Surfaces*, 2002;
*Algebra in Cuneiform*, 2017) show that Old Babylonian calculators maintained
separate operational verbs for physically distinct acts:

| Modern symbol | Old Babylonian operation | Physical meaning | Gojo `EncodingC` counterpart |
| --- | --- | --- | --- |
| `+` | ***waṣābum*** (*daḫ*, "join/append `j` to `S`") | **Asymmetric, in-place.** `S` keeps its identity while absorbing `j` (like interest *ṣibtum* added to a deposit `S`). No new entity named. | Conserved material transfer / absorption (`Flows.Move` into `S`, `T+` absorb). |
| `+` | ***kamārum*** (*gar.gar*, "heap `a` and `b`") | **Symmetric, new read/entity.** Produces a named result (*nakmartum*, "the heap", or plural *kimrātum* when parts remain distinct). Can heap heterogeneous measures. | Non-mutating bidirectional `RelationLaw(a, b, sum)` or observational fold into an output role. |
| `−` | ***nasāḫum*** (*zi*, "tear out `a` from `B`") | **In-place inverse of *waṣābum*.** Presupposes `a` is physically inside `B`; leaves diminished remainder (*šapiltum*). | Material draw (`T−` moving into `Flipped` room view of `B`). |
| `−` | **Comparison** ("`A` over `B`, `d` goes beyond") | **Non-mutating relation** between two separate magnitudes yielding excess (*watartum*/*dirig*) or deficit. | Two-faced `Gap` relation (`have, want, deficit, excess`) and `Sides.Compare`. |
| `×` | ***našûm*** (*íl/nim*, "to raise") | **Scaling by a factor/rate** (originally raising a base of standard thickness 1 cubit to height `h`). | Fixed calibration *on a connection* (`Flows.Convert(take, give)`) or port/frame scale. |
| `×` | ***šutakūlum*** (*gu₇.gu₇*, "make hold each other") | **Grade-raising construction** of a surface from two segments, preserving both sides (*šutamḫurum*/*mitḫartum*, "confrontation", for squaring). | Bilinear pair / 2D structure / `UnitShape.OrthogonalProduct` (unfolded product). |
| `×` | ***a.rá* / *alākum*** ("steps of" / "going") | **Iterated repetition** (`3 steps of 6 are 18`, or appending `s` `n` times). | Clocked accumulation / repetition controller. |
| `×` | ***eṣēpum*** ("repeat physically", `n ≤ 9`) | **Concrete structural replication** (e.g., doubling a right triangle into a rectangle). | Topological replication of a whole structure (`RepeatSurface` / linked instances). |
| `÷` | **Relational query + *igi* reciprocal table** | Division is not a primitive; scribes ask *"what shall I raise to `d` to get `P`?"* and multiply by the reciprocal (`igi`). Non-regular divisors explicitly report *"7 does not divide"* (residue/approximation). | Running a multiplication `RelationLaw` backwards, keeping open remainder or unresolved support. |
| `÷ 2` | **Scaling by `0;30`** vs. ***bāmtum* / *ḫepûm*** | Arbitrary multiplicative half vs. structural "moiety" obtained by "breaking" a whole (radius of a diameter, half-base of a triangle). | Calibrated ratio `Convert(2, 1)` vs. structural bisection/midpoint landmark. |

Two computational notes connected to Babylonian tables:

- **Floating sexagesimal place value** had no fixed radix point (`30` could mean
  `30`, `30 × 60`, or `1/2`, with scale supplied by context)—matching Gojo's
  separation of **rung digits** from **carrier calibration** (an uncalibrated
  read).
- **Quarter-square multiplication (computational technique, not established
  Babylonian history):** While Old Babylonian scribes kept tables of squares
  (e.g. Senkereh), using them via the identity
  $$a \cdot b = \frac{(a + b)^2 - (a - b)^2}{4}$$
  is **historically speculative** (McFarland; `ops_claude` §4.2). As an
  *`EncodingC` implementation technique*, however, it remains genuinely useful:
  it turns an $O(N^3)$-bit 3-role multiplication table into a 1D 2-role square
  table `n <-> n²` plus relational addition/subtraction and exact halving, while
  staying **bidirectional** in one relational pass (Appendix A, check 7).

### 3.2 Greece, Egypt, China, and India

- **Greece (Euclid Books II, V, VI, VII; Eudoxus; Archimedes):** Separated
  discrete multitude (*arithmos*) from continuous magnitude (*megethos*).
  Ratios exist **pre-numerically** via mutual subtraction (*anthyphairesis*) or
  Eudoxean equimultiples (`mA ⋚ nB` iff `mC ⋚ nD`) before numbers arrive.
  Euclid's *application of areas* yields exact fit (*parabolē*), **deficit**
  (*elleipsis*), or **excess** (*hyperbolē*)—the geometric ancestor of two-faced
  tension (`T−` and `T+`). Archimedes's lever/steelyard turns multiplication and
  division into physical equilibrium.
- **Egypt (Rhind Papyrus):**
  - *Duplation* multiplies with **no multiplication table at all**: only
    repeated doubling (`1×, 2×, 4×, 8×, …`, a 1-operand rung shift) plus a
    **Boolean selection plane** picking which doubled terms enter the sum
    (**Rungs + Boolean Selection + Addition**).
  - *False position* solves linear problems by guessing a trial value, measuring
    the mismatch, and scaling by the ratio—early **solving by lowering
    tension**.
- **China (*Jiuzhang Suanshu* / Nine Chapters):** Red and black counting rods
  keep positive and negative amounts on physically distinct carriers (matching
  `TwoFaceTests` and unnetted `T+`/`T−`); *fangcheng* elimination broadcasts
  uniform scaling across a whole column; *ying buzu* ("excess and deficit")
  solves problems from two opposite-signed tensions (double false position).
- **India (Brahmagupta, Āryabhaṭa, lattice multiplication):** Brahmagupta
  formalized fortunes (*dhana*), debts (*ṛṇa*), and zero (*śūnya*) as distinct
  operational roles; lattice (*gelosia*) multiplication factors products into
  small digit cells summed along constant-rung diagonals; **Āryabhaṭa (499 CE)**
  built the first sine table with a pure discrete second-difference recurrence
  ($\Delta_{k+1} = \Delta_k - s_k / 225$, vs. exact discrete coupling
  $1/233.5$ at $3.75^\circ$ steps; Appendix A, check 3)—a direct ancestor of
  the discrete landmark oscillator (§4.6).

### 3.3 Renaissance and Early Modern pivot: Viète → Napier → Descartes

- **Viète (*lex homogeneorum*, 1591) vs. Descartes (*La Géométrie*, 1637):**
  Viète kept grades strictly homogeneous (multiplying lines makes a plane,
  echoing Axiom 10). Descartes broke classical homogeneity by declaring an
  **explicit unit segment `1`** and constructing `a × b` as the **fourth
  proportional** `1 : a :: b : x` via similar triangles—exposing the permanent
  rule that **same-grade multiplication hides a fourth participant: unit
  calibration**.
- **Prosthaphaeresis (1580s) & Napier's kinematic logarithms (1614):**
  Prosthaphaeresis multiplied via phase addition and halving,
  $\cos\alpha \cos\beta = \frac{1}{2}(\cos(\alpha-\beta)+\cos(\alpha+\beta))$.
  Napier defined logarithms **physically** with two moving points: one at
  constant speed (additive clock), the other approaching a fixed landmark at
  speed proportional to its remaining distance (geometric carrier). Oughtred's
  slide rule then performed multiplication literally as extent addition on a
  logarithmically calibrated carrier.
- **Bombelli (1572):** Treated `+i` and `−i` as signed orientation operators
  acting on magnitudes rather than mysterious numbers.

### 3.4 Measurement theory, conservation, and physical computation

Four modern laws tighten the historical intuition into hard engineering rules:

1. **Participant kinds & extensive vs. intensive (Stevens 1946; Vergnaud 1988;
   Davydov):**
   Every operation depends first on *what kind of participant* enters it:
   - **Presence** (bit / occupancy): combines via Boolean logic (`AND`, `OR`,
     `XOR`, `Flipped`).
   - **Amount / Extent (extensive):** adds when compatible (lengths, counts,
     masses, charges, elapsed durations); splits and conserves.
   - **Position (interval / landmark-relative):** subtracting two positions
     yields a displacement (extensive); adding a displacement to a position
     moves it; **adding two positions directly is meaningless without an
     explicit origin**.
   - **Rate / Ratio (intensive):** temperature, density, speed, unit price,
     concentration. **Two intensive quantities with matching units never add**
     (mixing 20°C water with 20°C water does not yield 40°C water). Combining
     intensive reads requires combining the *extensives* they are ratios of—or
     multiplying a rate by an extensive to produce a target extensive
     (`speed × time → distance`, `price × count → cost`).
   - **Frame (origin + unit + orientation):** transforms *reads* of everything
     inside it; multiplying two frames composes them.
2. **Conservation & the CRN semilinear limit + Reversibility/Shear invariant
   (Chen, Doty & Soloveichik 2012/2014; Fredkin & Toffoli 1982):**
   - What can be *taken through a conserving port* is a preserved sum with
     positive weights (`Flows.Move`, branches, merges, constant-rate `Convert`,
     carries). Chemical reaction network theory proves that rate-independent
     conserving reactions stably compute **only semilinear functions** (sums,
     constant multiples, `min`, `max`, comparisons)—**never the product of two
     live counts**.
   - Consequently, every accountable multiplicative act takes one of three legal
     forms: (i) **refinement** (carry downward: 3 bags of 4 → 12 apples,
     conserving weighted sum); (ii) **supplied flow** (read rate `r`, drain a
     supply reservoir into target over duration `d`, leaving shortfall as `T−`);
     or (iii) **pairing** (construct a 2D orthogonal frame/combination—structure,
     not material). Creating material units from nothing is forbidden.
   - **Second invariant — reversibility (shears):** A shear $y \mathrel{+}= f(x)$
     where `x` is read-only (`Unchanged`) conserves **no sum**, yet is **exactly
     invertible** ($y \mathrel{-}= f(x)$). Symplectic discrete oscillators close
     because each half-step is a shear. Gojo has an accounting word for
     conservation (`PreservedSum`), **not yet for reversibility** (§7, gap 9).
3. **Bond Graphs (`TF` vs. `MTF`) & Signal Flow Graphs (Paynter 1961; Bonchi,
   Sobociński & Zanasi 2014–2025):**
   - A **fixed Transformer (`TF`)** (`e₁ = k·e₂`, `f₂ = k·f₁`) lives **on a
     2-port connection** (`Flows.Convert(take, give)`).
   - A **Modulated Transformer (`MTF`)** adds a **third information port**
     carrying modulating signal `A` that varies the ratio between `B` and `C`—
     marking the exact structural boundary where linear wiring (`copy`, `add`,
     `scale`, `discard`) ends and a bilinear 3-port structure begins.
4. **Solving by lowering tension, Propagators, and Diagrammatic Logic:**
   False position, *ying buzu*, Heron averaging, Archimedes's steelyard, CORDIC,
   and Radul & Sussman's Propagator Networks all converge on Gojo's twin
   mechanisms: narrowing open `Supports` (`RelationLaw`) and relaxing residual
   mismatch (`Gap` / `T+` / `T−`). Grassmann's *Ausdehnungslehre* separates
   outer product (*šutakūlum*) from scalar ratios; Spencer-Brown (*Laws of
   Form*) and Peirce (Existential Graphs) ground Boolean logic in drawing and
   crossing a boundary (`1 = presence`, `Flipped` views); Shannon (1938) notes
   that relay conductance vs. hindrance flips `AND`/`OR` under complement.

---

## 4. Resolving the core design questions

### 4.1 On connections vs. as structures of their own

Use the **Bond Graph + `EncodingC` arity/state test**:

| Place | What belongs here | Examples in `EncodingC` | Why |
| --- | --- | --- | --- |
| **Live View (`Surface`)** | Zero-copy reinterpretation of existing planes | [`Flipped`](../../Gojo.EncodingC/Substrate/Flipped.cs) (NOT, `Odv.Reoriented`, unary room/anti-fill), future framed/scaled port view | No bits copied or mutated; reads and writes pass straight through. |
| **On a Connection (`Wiring` row)** | Stateless, fixed-parameter 2-port transfers, rates, coarse reads, and single-bit gates | `Flows.Move`, `Flows.Convert(take, give)` (*našûm* / `TF`), `Flows.Passage`, `Flows.GatedMove`, `Sides.Compare`, 1-byte Boolean `RelationLaw`s | Rate or polarity is crystallized in the small shared table; no internal memory or 3rd dynamic operand needed. |
| **Structure of its own (`Engine` / multi-row mechanism with `Port`s)** | Anything with **2+ live dynamic operands (3+ ports)**, **internal state/memory**, **carry ladders**, or **multi-site spatial scope** | Variable `A × B` (duplation, lattice, quarter-square, `MTF`), unfolded 2D *šutakūlum* pair, multi-rung arithmetic ladder, spatial Boolean settlement, unified $m$-oscillator (`sin`/`cos`/`exp`/`log`) | Needs local scratch positions, phase/momentum state, carry wires, or sealed `Port`s so outside readers see only its interface (Closure C7/C8). |

Because `EncodingC` supports `Engine.Fuse` and thin `Port`/`Socket` wiring, a
small rate or operator can be absorbed into a sender/receiver when private or
extracted as a ported shared structure when reused (matching
[GojoSystemPrompt.md](GojoSystemPrompt.md) §4.6).

### 4.2 Output-role-first, directionality (`A op B` modifies `B` vs. new result), and timing/aliasing

First rule (from `ops_sol`): **give every readable result an output role before
allocating a fresh carrier.** An operation exposes an output role that may stay
open (narrowed on demand), be read as a live view, be crystallized/materialized
into its own store, or be bound to a receiver port.

Second rule: **both in-place mutation and non-destructive new results are
physically real, and `EncodingC` distinguishes them without any opcode flag**
via law kind, written roles, and `Workspace`/`Fork`:

1. **In-place modification (`A` acts on `B` — *waṣābum* / *nasāḫum*):**
   Encoded as a `TransitionLaw` where `A` is read-only (`Unchanged`) or drained
   (`PreservedSum`) and `B` is written. By the CRN conservation law (§3.4),
   scaling `B` in place is legal only as:
   - *unit refinement* (`Convert(1, 3)` preserving weighted sum `3·coarse + 1·fine`),
   - *reservoir-backed flow* (3-role transition drawing extra units from a
     supply, leaving unmet demand as `T−`), or
   - *local informational / shear update* (explicitly non-conserving; cannot
     write directly through a foreign port).
2. **New result (`A op B -> C` — *kamārum* / *šutakūlum* / Descartes):**
   Encoded as a `RelationLaw(A, B, C)` (running in every direction: forward
   evaluation, backward subtraction/division/factoring, open support narrowing)
   or a functional `TransitionLaw(A [read], B [read], C [write])`. Both inputs
   survive for provenance and downstream revision.
3. **Unification via `Workspace` / `Fork`:**
   Running an in-place operator inside a
   [`Workspace`](../../Gojo.EncodingC/Workspaces/Workspace.cs) or
   [`Fork`](../../Gojo.EncodingC/Flow/Fork.cs) turns it automatically into a
   non-destructive candidate (copy-on-write, with
   [`Gap`](../../Gojo.EncodingC/Workspaces/Gap.cs) recording the exact delta).
   `Commit` turns the candidate back into an in-place edit (refused atomically
   if the source moved).

#### Timing and aliasing contract for `A × B` acting on `B`

With gain `A = 2` and initial `B = 5`, five distinct uses of the would-be
product `10` must never be conflated (adapted from `ops_sol`):

| Use | Connected evidence after the first result | Timing / boundary requirement |
| --- | --- | --- |
| **Read `A` and `B`** | Output role reads `10`; `A = 2` and `B = 5` stay intact and readable. | None (observational). |
| **Publish or crystallize** | Live output held against an independent baseline, or captured carrier with stable tick promise and provenance. | Distinct target carrier or view. |
| **Apply once to `B`** | Receiver bound to `B` samples **`B₀ = 5`** at the application's before-boundary, requests `10`, and lets `B`'s port settle or decline (recording requested, accepted, and shortfall/capacity evidence). | **Explicit before-boundary capture `B₀`** so settlement never rereads the mutating receiver as its own operand. |
| **Feed back to `B`** | Each committed after-state connects to the next before-state: `5 → 10 → 20 → 40 …` | **Explicit time boundary**, repeat controller, and stop condition. |
| **Ask `B = 2B` at one instant** | Simultaneous constraint on one shared sample. Exact scalar solution is `0`; with `B = 5` it is **incompatible** (empty support / contradiction evidence). | **Never authorizes erasing `B`.** |

**Hard rule:** *Settling one event and advancing time are distinct acts.*
Aliasing an output back to its own input requires either a captured before-state
`B₀`, an independent baseline, or an explicitly timed feedback loop.

### 4.3 Path provenance and the discrete Leibniz rule

In *Sequential Math, Parallel Math* and the Drive squeegee articles, the order
in which two growing extents `a` and `b` arrive matters when provenance is
asked. Algebraically, any step `(Δa, Δb)` changes the product by the **discrete
Leibniz rule**:

$$\Delta(a \cdot b) = a\,\Delta b + b\,\Delta a + \Delta a\,\Delta b$$

The three terms are physically distinct regions of the growing rectangle:
- **$a\,\Delta b$ (row swept by `B`)** — pairs formed because `B` arrived while
  `a` was already present;
- **$b\,\Delta a$ (column swept by `A`)** — pairs formed because `A` arrived
  while `b` was already present;
- **$\Delta a\,\Delta b$ (gnomon corner)** — simultaneous co-arrivals.

Verified in Appendix A (check 4) for reaching `3 × 4 = 12`:
- **`L`-path** (3 arrivals of `A`, then 4 of `B`): `swept_by_A = 0`,
  `swept_by_B = 12`, `corners = 0` (total `12`).
- **Diagonal `\` path** (3 co-arrivals of `A, B`, then 1 of `B`):
  `swept_by_A = 3`, `swept_by_B = 6`, `corners = 3` (total `12`).

In the fine-grain limit (scaling `a, b` by `N = 1000` with single-tick steps),
the corner share drops to $\approx 1.4 \times 10^{-4}$—recovering the Drive
squeegee two-triangle limit and dual-number differentiation
($\varepsilon^2 = 0$). **Provenance is chosen by the question:** a machine may
keep recording off (total only), wire the three split planes on (live record),
replay an event log on demand, or crystallize a finished total to discard path
history intentionally.

### 4.4 Multiplying a larger structure (`k × Structure` or `A × Structure`)

"Multiply a whole structure by 3" conflates **four structurally distinct
operations**. Test every design against a canonical geometric stress fixture
(adapted from `ops_sol`): **a plate with a rectangular outline, a circular
through-hole, a mounting datum `o`, and an interface to a surrounding
assembly**.

| Request | Mechanism in Gojo / `EncodingC` | Structural & physical consequences on the plate |
| --- | --- | --- |
| **1. Port / Frame Re-Calibration (*našûm*, O(1))** | Leave interior untouched; attach a shared calibration/unit carrier at the `Port` (analogous to turning an entire `Odv` store with one `O` plane). | All internal ratios and seams stay invariant; external readers crossing the port read scaled magnitudes. |
| **2. Uniform Dilation about Datum `o`** | Selected-lane broadcast over an `Attention` plane applying $p' = o + 3(p - o)$ only to participating measure planes (never `Life`, row refs, or fixed topology). | In-plane lengths scale 3×, areas 9×; thickness and mass participate separately (mass scales 9× at fixed thickness, 27× if thickness also scales). If scaling hits an unscaled local capacity or fixed mounting site, conflict surfaces immediately as **local `T+` / boundary mismatch** right where the structure strains. |
| **3. Anisotropic 1-Axis Stretch** | Selected-lane broadcast along one exposed axis only. | Displacements along that axis scale 3×; area scales 3×. **A circular hole becomes an ellipse** if its geometry participates—or stays a **connected mismatch** if a circular passage constraint is demanded. |
| **4. Structural Replication (*eṣēpum*) or Bilinear Product (*šutakūlum*)** | Repeat instances (`3 × Plate`) with explicit placements/correspondences, or form `Plate × Thickness` as a higher-grade product. | Replication creates 3 distinct identities sharing design by reference; `Plate × Thickness` forms a volume basis where the hole's exclusion needs explicit correspondence to remain a through-hole. |

Additional boundary laws on structural gains:
- **Negative gain:** Uniform negative gain in a 2D plane is a half-turn plus
  stretch; negating a single axis is a reflection (orientation flip).
- **Zero gain:** Collapses the selected geometric extent and leaves the inverse
  open (never silently erases unrelated topology).
- **Shared vs. equal sites:** Genuine shared sites stay shared after scaling;
  independent equal-coordinate sites stay distinguishable.

### 4.5 Boolean operations across encodings and grades

Following [GojoSystemPrompt.md](GojoSystemPrompt.md) §8.2 & §9.7 (`1 = presence`,
`0 = absence`), one crucial distinction must be stated explicitly: **the same
planewise bitwise instruction means different arithmetic at different encodings**
(verified in Appendix A, check 5):

| Bitwise act | On single bits (bit rung, `0` or `1`) | On unary thermometer planes (`k` leading `1`s) |
| --- | --- | --- |
| Bitwise `AND` (`a & b`) | **Product / carry** of `a + b` (`1·1 = 1`, `x·0 = 0`) | **`min(a, b)`** (NOT multiplication!) |
| Bitwise `OR` (a \| b) | **Clamped sum** of `a + b` (Axiom 7 stop) | **`max(a, b)`** |
| Bitwise `XOR` (`a ^ b`) | **Wrapped sum** of `a + b` (Axiom 7 modulo) | **Span `[min, max)`** of length `max − min` — a valid *segment*, **not** a valid unary prefix when `min > 0` |

This resolves a live ambiguity in [GojoSystemPrompt.md](GojoSystemPrompt.md)
§9.7: *"a product is presence of both"* holds at the **bit rung** and on
**constraint supports**, never as a planewise `AND` across unary counts (§7,
gap 4).

Across grades, Boolean behavior composes cleanly from five existing seams:
1. **Live complement without copying:**
   [`Flipped`](../../Gojo.EncodingC/Substrate/Flipped.cs) inverts selected
   planes live (bit NOT, unary room/anti-fill with reversed plane order,
   `Odv.Reoriented`). Complement is always **scope-relative** (`Live & ~A` or
   selected area, never an unbounded universe).
2. **Selection / gating:** `Attention` planes and `Workspace` keep/ignore masks
   filter participation without copying or mutating unselected material.
3. **Pointwise 1-byte bidirectional logic (implemented):** Every 2-input Boolean
   relation is one byte over `(a, b, out)` (`AND = 0x87`, `OR = 0xE1`,
   `XOR = 0x69`, [`LawTests.cs`](../../Gojo.EncodingC/Tests/LawTests.cs#L188-L205)),
   running across aligned positions in parallel and narrowing backwards (`out = 1`
   on `AND` forces `a = 1` and `b = 1`).
4. **Spatial / structural Boolean (spans, regions, paths):** Over
   [`Paths`](../../Gojo.EncodingC/Laws/Paths.cs) and
   [`Sides`](../../Gojo.EncodingC/Laws/Sides.cs), two segments or regions
   partition an explicit scope into four occupancy masks (`neither`, `A only`,
   `B only`, `both`), on which any of the 16 Boolean masks selects surviving
   participation while preserving open-boundary residue.
5. **Four distinct zero-like states:** `V = 0` (undetectable magnitude at
   current resolution), unselected (`Attention = 0`), empty support
   (contradiction), and Boolean `0` (absent presence) remain four distinct bit
   patterns.

### 4.6 Stacked physical dynamics: the unified $m \in \{+1, 0, -1\}$ family and powers

#### One two-accumulator loop: `sin`/`cos`, coasting/multiply, and `exp`/`log`

Your landmark-and-momentum intuition—a mover accelerated toward a landmark,
gaining momentum, overshooting it, and being pulled back from the other side—is
actually **one member of a unified three-geometry family** (also the exact heart
of Walther's unified CORDIC):

```text
State (two two-faced quantities, never auto-netted without an explicit cancel law):
  x : signed position relative to landmark  (x+, x−)
  v : signed momentum / velocity            (v+, v−)

Each tick, in DECLARED ORDER (two half-steps / waves):
  Wave 1 (drift):  x  ←  x + coarse(v, shift)          # reversible shear
  Wave 2 (kick):   v  ←  v − m · coarse(x, shift)      # m ∈ {+1, 0, −1}
```

The single integer feedback sign $m \in \{+1, 0, -1\}$ (not an enum!) selects
both the physical geometry and the quadratic fold of the `inverse × inverse`
quadrant (unit square $= -m$; verified in Appendix A, check 2):

| Feedback sign $m$ | Physical behavior | Function family | Algebraic fold (`u² = −m`) |
| --- | --- | --- | --- |
| **$m = +1$** (restoring pull) | Landmark + momentum harmonic oscillation (Minsky circle; Āryabhaṭa second differences) | **`sin` / `cos`** (90° phase pair) | Complex numbers ($i^2 = -1$, [GojoComplexInterpretations](GojoComplexInterpretations-2026-09.md)) |
| **$m = 0$** (zero pull) | Constant-rate coasting (`x += r` for `d` ticks → `r · d`) | **Clocked multiplication** | Dual numbers ($\varepsilon^2 = 0$, fine-limit Leibniz derivative) |
| **$m = -1$** (reinforcing pull) | Growth/decay proportional to distance from landmark (Napier's 1614 kinematic logarithm when paired with a linear clock) | **`exp` / `log` / `sinh` / `cosh`** | Split-complex numbers ($j^2 = +1$) |

Three structural properties make this design deeply Gojo-native:

1. **Resolution ladder of the same oscillator:**
   - **Rung 0 (pre-numeric `Sides` sentence):** Keeping only the signs of `(x, v)`
     turns the oscillator into a 2-bit transition law visiting four quadrants in
     **Gray order**, one bit flipping per quarter:
     ```text
     (+,+) ─turned back→ (+,−) ─crossed→ (−,−) ─turned back→ (−,+) ─crossed→ (+,+)
       11        →         10      →       00        →         01      →       11
     ```
     Both transitions (`turned back` and `crossed`) already exist in
     [`Sides.cs`](../../Gojo.EncodingC/Laws/Sides.cs). Two quarters negate both
     signs and four return—constructing **Axiom 10's order-4 generator** by
     alternating two `Sides` involutions over time, without turning the
     primitive face dual itself into multiplication by `i`.
   - **Coarse pull → refined pull:** Reading the restoring pull only as a `Sides`
     sign (constant acceleration toward the landmark) produces parabolic arcs (a
     rounded triangle wave); refining the read to distance-proportional pull
     refines the waveform smoothly into sine and cosine.
   - **Material account:** The velocity gained by the mover is transferred from
     the landmark taking opposite momentum (Newton's third law = a conserving
     move between mover and landmark velocity counts at a rate read from
     distance), while position integrates velocity via an exactly reversible
     **shear**.
   - **Bidirectional queries:** Wiring the two-wave step `AsRelation()` across a
     multi-moment plan (as in `SelfRunningTests.Planner`) lets the exact same
     structure generate waves forward or narrow phase, amplitude, and landmark
     position backwards from partial observations.
2. **Why step order is mathematical structure, not a runner preference:**
   - Stepping Wave 1 then Wave 2 in **declared order** (symplectic Euler /
     leapfrog / Minsky) has Jacobian determinant **exactly 1**: it conserves a
     discrete shadow energy $H_{\text{shadow}} = \frac{1}{2}(v^2 + k x^2 - k x v)$
     and closes an **exact periodic integer orbit** (`x₀ = 64, v₀ = 0, shift = 3`
     returns to `(64, 0)` after **54 steps** with `max |x| = 64`; Appendix A,
     check 1).
   - Stepping both rows simultaneously (`Engine.StepTogether`, explicit Euler)
     has determinant $1 + k > 1$: energy grows every cycle, spiraling to
     `max |x| = 1187` within 400 steps!
   - **Verified `EncodingC` gap:** Within a single
     [`Engine`](../../Gojo.EncodingC/Flow/Engine.cs), declared wiring order is
     recorded, *but* the choice between `Step()` and `StepTogether()` is left to
     the caller (`Engine.Settle(bool together)`), and stepping order *across*
     peer engines is explicitly caller-chosen
     ([`Gojo.EncodingC/README.md`](../../Gojo.EncodingC/README.md)). For
     coupled shears, **step kind and coupled order carry operational meaning and
     must be saved with the machine** (§7, gap 1).
3. **Exact rational turns vs. discrete shadows:** A discrete rational phase turn
   such as `(3x + 4u)/5, (-4x + 3u)/5` (noted in `ops_sol`) keeps exact length
   only when division by 5 is represented without truncation—otherwise whole-tick
   stepping introduces rounding loss or needs a finer rung. The symplectic
   integer recurrence avoids division drift entirely while staying exact on its
   discrete shadow orbit (see §6.2 for when math-library `sin` is or isn't
   legally substitutable).

#### Powers

As noted in *Sequential Math, Parallel Math* and
[GojoSystemPrompt.md](GojoSystemPrompt.md) §6, powers are **repeated
multiplicative participation**—either sequential (clocked self-multiplication
with a repeat counter) or parallel (linked scaling rungs tracking
simultaneously, where rung `m` stays `a×` rung `m − 1`).

---

## 5. Candidate architectures, recommended hybrid, and the stiffness ladder

### 5.1 Three centers of gravity — and how they compose

Every Gojo-faithful approach keeps meaning in connected structure and avoids
strings, floats, and opcode enums. They differ in which facet is primary:

| Architecture | Metaphor | Core mechanism | Best at | Weakest at |
| --- | --- | --- | --- | --- |
| **A. Junction Tables** | *The Scribe* | Crystallized `RelationLaw` tables over small radices, composed in place-value lattices or quarter-square networks; `Workspace`/`Fork` enactment. | Bidirectional reads (`−`, `÷`, factoring, open supports); learned/cached reflexes; zero-step lookup. | Table blowup past coarse radices; arc-consistent local narrowing needs probes on coupled loops; lacks material flow conservation on its own. |
| **B. Coupled Flows** | *The Machine* | `TransitionLaw`s + `Accounting` (`PreservedSum` & shears) + `Flipped` room views + clocked accumulation & two-wave symplectic oscillators. | Material conservation, `T+`/`T−` supply shortfall, path provenance, live physical dynamics (`sin`/`cos`/`exp`/`log`), high hardware compilability. | Clocked latency for scalar products; harder backward factoring/division until wrapped in a temporal `AsRelation()` plan or crystallized table. |
| **C. Frames & Rungs** | *The Geometer* | Shared calibrated frames (`1 : A :: B : C`), `ResolutionOf`/`SubdivisionOf` carry ladders, and `MTF` modulated port views. | Whole-structure O(1) scaling, unit algebra, extensive vs. intensive clarity, multi-rung place-value meaning (`rung = log`). | Adds frame-view machinery to `EncodingC`; 4-role `(u, a, b, c)` tables must still factor; risks blurring ruler re-scaling with conserved material if not walled off. |

**Recommended Hybrid Architecture** (synthesizing the strongest role of each):

- **Geometer (C) defines the meaning:** participant kinds (extensive vs.
  intensive), uniform-radix carry ladders (`B = 2, 4, 8, 16`), explicit unit
  calibrations, and shared-basis port scaling.
- **Machine (B) runs live material flow and dynamics:** strict wall around
  conserved `TransitionLaw`s (*waṣābum*, *nasāḫum*, *našûm* `Convert`,
  reservoir-backed multiplication) and reversible two-wave shears (the unified
  $m$-oscillator).
- **Relational Pin-and-Fold (A + `Workspace`/`Fork`) owns non-destructive
  computation and inverses:** Boolean, *kamārum* sum/sub, factored relational
  multiplication (testing both Egyptian duplation and quarter-squares in Phase
  3), unfolded *šutakūlum* pairs, and temporal inverse queries.
- **Scribe (A crystallized) serves as the learned / compiled reflex:** once a
  subgraph stabilizes behind sealed ports, small crystallized tables and
  compiled native plans (§6) replace multi-step execution transparently.

### 5.2 The 6-rung positional stiffness ladder (perturbable vs. locked rates)

A recurrent design tension is when a rate, factor, or coupling constant should
be **live and perturbable** versus **frozen and compile-time constant**. Rather
than adding a `bool IsMutable` flag, `EncodingC` already provides a **6-rung
stiffness ladder strictly by where a value sits** (from `ops_claude` §7.6):

| Stiffness rung | Where the value sits in `EncodingC` | Who can perturb it? | Compiler consequence (§6) |
| --- | --- | --- | --- |
| **1. Live store count** | Ordinary writable `Store` plane inside local `Engine` | Any local wiring or incoming transfer | Runtime variable (loaded per lane) |
| **2. Unselected / masked** | Writable `Store`, currently `0` in `Attention` / keep mask | Protected from active step; mutable once re-attended | Runtime variable on attended lanes only |
| **3. Behind a foreign `Port`** | Owned by peer `Engine`, exposed read-only via `Socket` | Owner only; local readers can observe, never mutate | Loop-invariant across local step if peer holds still |
| **4. `Unchanged` role** | Bound to a law whose `Accounting` marks that role `Unchanged` | Law itself promises not to mutate it; other wirings may | Read-only operand; safe to broadcast / hoist |
| **5. Constant `Plane`** | Single-bit/constant plane (like `O` turning an `Odv` store) | Refused by ordinary `Workspace` writes; requires structural edit | **Compile-time uniform constant** (epoch-guarded) |
| **6. Locked inside law table** | Folded directly into `TransitionLaw` / `RelationLaw` bits (`Flows.Convert(take, give)`) | Immutable; changing it requires building and installing a new law | **Hard compile-time immediate** (constant-folded into ALU/DSP/LUT) |

Moving *down* the ladder is **crystallization**; moving *up* inside a `Fork` is
**thawing for exploration**. The exact same structural mechanism governs
learnability, safety, and compiler constant specialization.

---

## 6. Crystallization, compilation, and hardware acceleration (CPU / NEON / AVX / GPU / FPGA / math libraries)

> **Core rule:** Hardware must never drive the conceptual design
> ([GojoSystemPrompt.md](GojoSystemPrompt.md) §9.6). Every operation remains
> fully definable and executable in canonical `EncodingC` bit-tables. **Once a
> structure crystallizes, however, a derived compiler backend must be able to
> lower its interior to CPU ALU, SIMD (ARM NEON / x86 AVX), GPU, FPGA, or
> math-library units without violating Gojo's semantics.**

This extends Consolidation K7 (*native authority with derived compiled plans*),
[`FiniteLawPlan.IsCurrent`](../../experiments/QualitativeExecution/FiniteLawPlan.cs),
and Axiom 12 (*crystallization hardens a stable tick promise*).

### 6.1 Two-phase lifecycle

```text
  EXPLORATORY / FLUID PHASE                    CRYSTALLIZED / HOT-PATH PHASE
  ─────────────────────────                    ─────────────────────────────
  • Canonical EncodingC bit-tables             • Sealed local Engine / Port boundary (C7/C8)
  • Open supports, partial inputs              • Epoch-guarded CompiledPlan (IsCurrent)
  • Backward narrowing, probes, forks          • Settled, valid, unaliased, uncontested lanes
  • Learning counts, rewiring topology         • Runs on CPU ALU / NEON / AVX / GPU / FPGA / libm
  • Always the reference oracle                • Any open/broken/overflow lane deopts to canonical
```

`Transport.Save` **always** serializes the canonical Gojo structure—never a
compiled binary or backend tag. A restored machine runs identically on any host
and re-compiles locally for whichever accelerator is present.

### 6.2 Two lowering regimes: Bit-Exact vs. Declared Finite-Read

Not every hardware substitution makes the same mathematical promise. Keep these
two regimes strictly separate:

#### Regime 1: Bit-Exact Lowering (zero approximation)

The hardware path produces the **exact same bit pattern** at every exposed port
(including carries, remainders, `T+`/`T−` overflow, and `Evidence` planes) as
stepping the canonical `EncodingC` structure:

- **Bit-sliced `Plane` laws → SIMD / FPGA LUTs:** A `Plane` is a contiguous
  `ulong[]` across `N` lanes. Any small `TransitionLaw` or `RelationLaw` lowers
  to a Boolean circuit running **128–512 lanes per instruction** on ARM NEON
  (`vandq`, `vorrq`, `veorq`, `vbicq`, `vqtbl1q` for 4-bit LUTs) or x86
  AVX2/AVX-512 (`vpand`, `vpor`, `vpshufb`, `vpternlogq`), or directly onto
  **FPGA LUT6 slices** (`Flows.Passage` is literally one LUT6:
  `64'h8048201000040201`).
- **Crystallized uniform-radix carry ladders → Native widening ALU / SIMD `ADD` & `MUL`:**
  A chain of `k` rungs `(r₀, r₁, …, r_{k-1})` linked by verified
  `Convert(B, 1)` carries (`PreservedSum([1, B])`) represents
  $\sum_{i=0}^{k-1} r_i B^i$. For power-of-2 radices (`B = 2, 4, 8, 16`),
  packing/unpacking across `N` lanes is pure bit-transpose into native
  `u16`/`u32`/`u64` arrays, where add, sub, constant scale, and factored
  multiply execute as **widening hardware instructions** (`ADD`, `BigMul`, ARM
  NEON `vaddq`/`vmulq`/`vmull`, x86 `mulx`, FPGA `DSP48`), unpacking low bits
  back to result rungs and routing high overflow bits directly into `T+` (or
  carry-out).
- **Fixed-rate `Convert(take, give)` & exact division:** Lowers to integer
  `(q, r) = DivRem(x * give, take)` (or constant reciprocal multiply-shift),
  retaining exact source-basis remainder `r`.
- **Discrete symplectic oscillator (integer kick-drift):** Lowers to integer
  shift-and-accumulate loops (`vmlaq`/integer SIMD) preserving exact bit-for-bit
  parity with the declared-order discrete orbit. **A math library's `sin` may
  never silently replace a discrete Minsky loop in Regime 1**, because the
  discrete shadow Hamiltonian has effective frequency
  $\omega_{\text{eff}} = 2\arcsin(\sqrt{k}/2) \neq \sqrt{k}$.

#### Regime 2: Declared Approximate / Math-Library Lowering (explicit finite-read contract)

Lowering a crystallized continuous-target read (`sin`/`cos`, `exp`/`log`,
`sqrt`) to FPU / vector `libm` / hardware CORDIC is legal **only when** the
crystallized boundary carries an explicit **finite-read contract** (Axiom 9,
[GojoFiniteReadFixture.md](GojoFiniteReadFixture.md), §8.1):

- declared input/output calibration (quantum, scale, phase origin, capacity);
- declared rounding and clipping rule (outward or nearest, with clipped tails
  surfaced in `T+`/`T−` or open endpoints);
- declared **integer-tick tolerance bound** (`±1 tick at rung r` expressed via
  `Convert` weights—no floats stored in canonical structure!) between the
  library call and the canonical model;
- explicit provenance marking that the output used a declared finite-read
  lowering rather than bit-exact step replay.

### 6.3 Structural recognition without opcode enums or strings

Gojo forbids storing `"Multiply"` or `OpCode.Sin` tags in canonical structure
([GojoSystemPrompt.md](GojoSystemPrompt.md) §9.1, §9.6). A backend recognizes
accelerable subgraphs **strictly from verifiable structure** in three layers:

1. **Content-addressed law table signatures:** `Law.SameTable` compares laws
   solely by their bit tables. Verified identities cache by table content hash
   (`AND = 0x87`, `Passage = 0x8048201000040201`, radix-`B` half/full adder,
   1D square table, `Odv.Addition(Detection)`).
2. **Mechanical exhaustive verification of small tables & `Accounting`:**
   Because primitive tables are small, a compiler **proves** (never trusts a
   builder name) that:
   - a carry law preserves `1·fine + B·coarse` (`Accounting.Invariants`);
   - a 3-role table satisfies `r == (a + b) % B` or `r == (a * b) % B`;
   - a 2-role table satisfies `y == x * x` or `y == k * x`;
   - a declared-order wiring pair implements the symplectic shear
     `x' = x + coarse(v); v' = v - m * coarse(x')`;
   - any operand sitting at stiffness rung 5 or 6 (§5.2) is a proven
     compile-time constant eligible for immediate folding.
3. **Sealed subgraph pattern matching + epoch guard (`IsCurrent`):** Over a
   local `Engine` whose boundary is sealed by `Port`s (no foreign direct store
   access), matching composed idioms emits a `CompiledPlan` guarded by a
   topology/law/port epoch token (directly generalizing
   [`FiniteLawPlan.IsCurrent`](../../experiments/QualitativeExecution/FiniteLawPlan.cs)).
   Any structural edit, rewiring, lane rebind, or law change flips
   `IsCurrent = false` and falls back cleanly to canonical stepping.

### 6.4 Deoptimization guards and preserving Gojo semantics

Every compiled kernel obeys **three non-negotiable runtime rules**:

1. **Per-lane fast-path guard mask:** Compute the bitmask of lanes satisfying
   `Live & Attended & SettledValidCodes & ~Aliased & ~Contested`.
   - Guarded lanes run through the compiled SIMD / ALU / math-lib path.
   - Any lane with open `Supports`, malformed unary prefix (`010`), aliased
     ports, or `StepTogether` contention deopts immediately to canonical
     `EncodingC` execution and writes exact `Evidence` planes (`Unread`,
     `Unadmitted`, `Ambiguous`, `Contested`).
2. **No silent hardware wraparound:** Use widening arithmetic (`Math.BigMul`,
   ARM NEON `vmull`, overflow flags) so excess magnitude never wraps silently
   modulo $2^{32}$ or $2^{64}$ (Axiom 8). Overflow routes into `T+` (or the next
   carry rung); unmet draw routes into `T−`; empty domains / divide-by-zero set
   `Unadmitted` / empty support; division keeps exact source remainder.
3. **Optional witness preservation:** When a witness or provenance lane is wired
   (Consolidation §5.12; §4.3 path split), either the compiled kernel populates
   it identically or deopts to canonical stepping whenever recording is active.

### 6.5 Six design guardrails ("don't accidentally block compilation")

1. **Seal operation interiors behind thin `Port` boundaries (Closure C7/C8).**
   External wirings must not reach directly into internal scratch or carry bits.
2. **Use uniform power-of-2 radices (`B = 2, 4, 8, 16`) with explicit
   `Accounting` weights for default numeric ladders.** Explicit
   `PreservedSum([1, B])` weights make place-value structure mechanically
   provable without metadata, and power-of-2 radices pack into CPU/NEON
   registers with shifts and masks. (Radix 10 or 60 still work canonically and
   via base-conversion fast paths.)
3. **Keep settled `TransitionLaw` bindings cleanly separate from `Binding.Open`
   support narrowing** so fast-path checks stay a single mask test.
4. **Preserve contiguous bit-sliced (`Plane`) Structure-of-Arrays storage.**
   Contiguous `ulong[]` planes vectorize directly on ARM NEON (`Vector128`),
   x86 AVX2/AVX-512 (`Vector256`/`Vector512`), and GPU warps.
5. **Keep conserved material transfers separate from pure informational
   computation** and **save declared step order/kind with the machine**, so the
   compiler knows exactly which subgraphs may be fused/reordered and which
   shears must preserve wave order.
6. **Keep `CompiledPlan` strictly derived and disposable**, verified by
   randomized differential conformance tests against canonical stepping.

### 6.6 Hardware lowering map

| Crystallized Gojo operation | Canonical `EncodingC` form | CPU scalar | ARM NEON / x86 SIMD | GPU | FPGA | Equivalence regime |
| --- | --- | --- | --- | --- | --- | --- |
| Small `TransitionLaw` / `RelationLaw` (`Passage`, Boolean, `Odv.Addition`) | Bit-table sum of products across `Plane`s | Bitwise `u64` (64 lanes/op) | `Vector128`/`256`/`512` bitwise; `vqtbl1q` / `vpshufb` for 4-bit LUTs | Warp `u32`/`u64` bitwise + constant LUT | Direct LUT3–LUT6 (`Passage` = 1 LUT6) | **Bit-exact** |
| Unary count `Add` / `Remove` / `min` / `max` | Thermometer prefix planes (`Unary`) | Shift / mask / `popcnt` | `vshl`/`vshr`/`vcntq` / `vandq` / `vorrq` | `__popc` / `__ballot_sync` | Thermometer shift registers | **Bit-exact** |
| Multi-rung radix-`B` add / sub | Carry ladder (`Convert(B, 1)` + rung adder) | `ADD`/`SUB` + overflow → `T+`/`T−` | `vaddq` / `vsubq` + widening overflow detect | Coalesced integer kernel | Ripple / carry-select adder | **Bit-exact** |
| Fixed scaling *našûm* (`Convert(take, give)`) | 2-port `TransitionLaw`, `PreservedSum([give, take])` | `DivRem` / multiply-shift + exact remainder | `vmull` + shift + remainder mask | Integer FMA / shift | Hard `DSP48` constant mult | **Bit-exact** |
| Factored multiply `A × B` (duplation, lattice, quarter-square) | Sealed multi-rung `Engine` with `Port`s | Widening `BigMul` → result rungs + `T+` overflow | NEON `vmull_u32` / AVX `vpmuludq` across lane batches | Integer/tensor lane kernel | Hard `DSP48` multiplier + carry | **Bit-exact** |
| Whole-structure broadcast (`k × Selected`) | `Wiring` over `Attention` plane | Masked loop over packed lane arrays | Predicated NEON/AVX masked vector multiply | Predicated warp kernel | Masked parallel DSP array | **Bit-exact** |
| Unified $m$-oscillator (`sin`/`cos`, coast, `exp`/`log`) | Saved declared-order two-wave `(x, v)` shear loop | Integer shift-acc (Regime 1) or `libm` (Regime 2) | NEON integer shift-acc (`vmlaq`) or vector trig/CORDIC | Parallel oscillator batch kernel | Pipelined integer leapfrog or CORDIC IP | **Bit-exact** (discrete) or **Declared Finite-Read** (`libm`) |
| Open `Supports` narrowing | Bitwise `RelationLaw.Narrow` | Bitwise `u64` tuple-AND/OR | SIMD bitwise plane propagation | Parallel plane propagation | LUT / AND-OR fabric | **Bit-exact** |

---

## 7. Axiom conflicts and gaps surfaced by this design

Implementing operations honestly exposes **11 small gaps or ambiguities** in
[GojoSystemPrompt.md](GojoSystemPrompt.md) and
[`Gojo.EncodingC/README.md`](../../Gojo.EncodingC/README.md) (consolidated from
`ops_claude` §8 and verified against the codebase):

| # | Conflict or gap | Smallest clarification to resolve it |
| --- | --- | --- |
| 1 | **Step kind and cross-engine order are not saved with the machine.** `Engine` saves internal row order, but `Step()` vs. `StepTogether()` and peer-engine stepping order are caller-chosen—yet for coupled shears this decides whether an orbit closes or explodes (§4.6). | Save required step kind (`InOrder` vs. `Together`) and coupled peer schedule as structure inside the machine and `Transport`. |
| 2 | **Product material source is unspecified.** §4.4 accounts incoming participation (accepted, blocked, remainder) and Axiom 5 routes external change to tension, but live material multiplication cannot conserve a single sum (§3.4). | Clarify: *"A product is a read unless a refinement or supply route is wired; demanded but unsupplied product material becomes draw tension (`T−`)."* |
| 3 | **Intensive and extensive quantities are not distinguished.** Axiom 10 states additive compatibility in terms of units, yet same-unit intensive reads (temperatures, densities, rates) still do not add (§3.4). | Clarify: *"Addition applies to extensive amounts and extents; intensive reads combine through the extensives they are ratios of."* |
| 4 | **§9.7 ("a product is presence of both") holds only at the bit rung and on constraint supports.** On unary thermometer planes, `AND` is `min`, not multiplication (§4.5). | Scope §9.7 explicitly by encoding so no one wires planewise `AND` across unary counts as a multiplier. |
| 5 | **Order-4 generator vs. primitive flips.** The symmetry articles' four units and `EncodingC`'s orientation flips are involutions (`f² = id`), whereas Axiom 10's generator is a quarter-turn (`g² = −1`, `g⁴ = id`). | Clarify that the quarter-turn generator arises by state-dependently alternating two flips over time (rung-0 `Sides` Gray cycle `11 → 10 → 00 → 01 → 11`, §4.6). |
| 6 | **Kernel output-grade rule is a fold rule, not the whole operation.** "Point output sums, frame output multiplies" chooses whether a fold stays in grade; it does not specify rate/amount roles, timing, or material supply. | Keep fold grade separate from process, conservation, and timing contracts (§4.2). |
| 7 | **No single canonical term for a multi-operand junction.** The kernel uses "operation port" and `EncodingC` uses "wiring row" / "local engine". | Adopt one term (e.g. *junction*) across prompt and `EncodingC` docs for where 2+ live signals meet. |
| 8 | **Tension note needs no new tension kind for operations.** "Times-two being used up" is goal tension in a log/rung basis; supply shortfall is material `T−` deficit. | Confirm in [GojoTension-2026-11.md](GojoTension-2026-11.md) that operational shortfalls reuse existing `T+`/`T−` and `Gap` rather than adding new kinds. |
| 9 | **Reversibility (shears) has no accounting word yet.** `Accounting` checks conserved positive sums (`PreservedSum`), but shears ($y \mathrel{+}= f(x)$) conserve information without conserving any sum (§3.4). | Add a checked `Reversible` / `Bijective` (or `Shear`) accounting promise alongside `PreservedSum`. |
| 10 | **Stiffness (resistance to change) lacks a structural definition.** Prompt §5.1 leaves graded resistance open; §9.2 separates mutable state from immutable promises. | Adopt the 6-rung positional stiffness ladder (§5.2): stiffness is a read of structural position (and optionally support width), not a metadata flag. |
| 11 | **Compiled-form contract could be tighter.** §8.1 and §9.1 permit compiled forms traced to structure without specifying the exact equivalence boundary. | Codify Regime 1 (Bit-Exact) vs. Regime 2 (Declared Finite-Read with integer-tick tolerance), content recognition, per-lane deopt guards, and `IsCurrent` invalidation (§6). |

---

## 8. Concrete phased plan & falsifiable micro-experiments (`E1–E15`)

Each phase is a self-contained checkpoint with explicit positive tests,
negative/unresolved tests, saved byte-size measurements (`Transport.Measure`),
and targeted micro-experiments (`E1–E15`, drawn from `ops_claude` §9 plus
`ops_sol`'s geometric and aliasing stress fixtures).

### Phase 0 — Tension, saved step kind, and two-faced gap groundwork
Align with [GojoTension-2026-11.md](GojoTension-2026-11.md) §8 steps 1–2 and
fix the step-order gap before building coupled dynamics:
- Material `T+` and `T−` unary fields + `Flipped` room-view
  `Absorb`/`Draw`/`Cancel` via existing `Flows.Move` (never netted by default).
- Two-faced `Gap` relation over `(have, want, deficit, excess)`.
- **Saved step kind & `Reversible` accounting prototype:** record required
  stepping mode (`InOrder` vs. `Together`) in `Engine` / `Transport` (gap 1) and
  check shear invertibility alongside `PreservedSum` (gap 9).
- **Exit tests:** overflow waits in `T+` and absorbs when room opens; unmet draw
  waits in `T−`; conservation holds under `Accounting`; deficit and excess stay
  distinct; saved-and-restored engines preserve their required step kind.

### Phase 1 — Boolean operations across bits, unary planes, lanes, and spans
- Stock 16 two-input 1-byte bidirectional Boolean `RelationLaw`s (building on
  `LawTests.ABooleanOperationIsOneByteReadInEveryDirection`).
- **`E10` (bit rung vs. unary rung):** Verify `AND`/`OR`/`XOR` as
  carry(product)/clamped-sum/wrapped-sum at the bit rung, AND explicitly pin
  their meaning on unary thermometer planes as `min`/`max`/span (catching broken
  unary prefixes when `min > 0`).
- Lane-parallel masked Boolean using `Attention` planes and `Flipped` live
  complement views.
- Spatial Boolean over [`Paths`](../../Gojo.EncodingC/Laws/Paths.cs) /
  [`Sides`](../../Gojo.EncodingC/Laws/Sides.cs): four occupancy regions
  (`neither`, `A only`, `B only`, `both`) inside an explicit scope with
  open-boundary residue preserved.
- **Negative tests:** `V = 0` (undetectable), unselected (`Attention = 0`),
  empty support (contradiction), and Boolean `0` remain distinct; shared
  coordinate alone is not intersection.

### Phase 2 — Multi-rung numerics, two additions, and division duals
- Uniform-radix ladder helper (`B = 4` or `B = 8` over 2–3 unary rungs) chained
  by `Flows.Convert(B, 1)` carries (`PreservedSum([1, B])`).
- Contrast both additions in tests:
  - **Material *waṣābum* / *nasāḫum*:** in-place conserved transfer across rungs
    with carry-up, borrow-down, top-rung overflow → `T+`, unmet draw → `T−`.
  - **Relational *kamārum*:** multi-rung ripple-carry `RelationLaw` over open
    supports `(a_i, b_i, cin_i, sum_i, cout_i)` adding forward, subtracting
    backward, and propagating partial uncertainty.
- **`E9` (conserving refinement):** Run a carry downward (`3` coarse units →
  `12` fine units) conserving weighted sum.
- **`E11` (quotition vs. partition division):** Counting steps to empty equals
  the carry route (quotition); round-robin distribution across targets conserves
  (partition).
- **`E12` (coarse-first settle):** Signed-digit remainder ladder converging
  toward exact projection one rung at a time.
- **Ladder boundary fixtures (from `ops_sol`):** Pin `17 + 28` add-then-observe
  vs. observe-then-add (residues re-crossing a ten) and `[0, 15]` clamped
  non-associativity `(14 + 3) - 3 = 12 ≠ 14 + (3 - 3) = 14` versus associative
  `T+`-routed overflow.

### Phase 3 — The four multiplications, provenance, aliasing, and plate scaling
Prototype and measure all four distinct multiplicative forms (reporting saved
bytes, table bits, step count, and backward invertibility):
1. **Fixed scaling (*našûm* / `TF`) & stiffness ladder (`E14`):** Test one
   coupling rate placed at all 6 rungs of the positional stiffness ladder (§5.2)
   to prove perturbability, `Fork` thawing, and port protection with zero flags.
2. **Coasting / clocked & Egyptian duplation (*a.rá*, `E3`):** Read rate by
   reference, drain supply into target over clock ticks (`target = r·d`),
   exposing supply shortfall as `T−`; compare against binary doubling + Boolean
   selection.
3. **Bidirectional relational product (`E1`, `E2`) & quarter-square benchmark:**
   - `E1`: Coarse `(a, b, c)` times relation dividing backwards, leaving `b`
     open when `a = c = 0`, and surfacing contradiction when `a = 0, c ≠ 0`.
   - `E2`: Two-rung place-value lattice (4 cells of one small table + diagonal
     rung sums + carry, verifying `(10 + 7)(20 + 8)` cross-rung behavior) vs.
     1D quarter-square lookup (`n <-> n²` + add/sub + `/4`) on saved bytes,
     table bits, and backward factoring (`? × ? = 12`).
4. **Unfolded bilinear product (*šutakūlum*) & path provenance (`E4`, `E15`):**
   - `E4`: Arrival pairing with `swept_by_A`, `swept_by_B`, and `corners`
     planes verifying equal total `12` with distinct splits on `L`-path
     `(0, 12, 0)` vs. diagonal `\` `(3, 6, 3)`.
   - `E15`: Same product with recording off, recording on, and replayed—proving
     provenance is chosen by the question and recorded/replayed splits agree.
5. **Aliasing & timing fixtures (from `ops_sol`):** Pin all 5 rows of §4.2 on
   `gain = 2, B = 5`: read, publish, apply-once with captured `B₀`, timed
   feedback (`5, 10, 20, 40`), and simultaneous `B = 2B` reporting
   incompatibility without erasing `B`.
6. **Plate-with-hole-and-datum scaling fixtures (`E8` + `ops_sol` §4.4):**
   - `E8`: O(1) scaling via shared unit/frame retargeting (all reads scale,
     ratios unchanged, zero interior edits).
   - `Attention`-selected uniform dilation about datum (`p' = o + 3(p − o)`)
     hitting an unscaled mounting/capacity constraint → localized `T+`/mismatch.
   - 1-axis stretch turning circular hole to ellipse or exposing connected
     circular-passage mismatch.
   - Structural repetition (`3 × Plate`) and `Plate × Thickness` volume with
     through-hole exclusion.

### Phase 4 — Stacked physical dynamics (`sin`/`cos`, coast, `exp`/`log`)
- **`E5` (rung-0 `Sides` Gray cycle):** Two-bit phase law
  `11 → 10 → 00 → 01 → 11` alternating `turned back` and `crossed`; two steps
  negate, four return (Axiom 10 generator).
- **`E6` (step-order proof):** Integer Minsky pair `(x₀ = 64, v₀ = 0, shift = 3)`
  proving declared order closes in 54 steps (`max |x| = 64`) while
  `StepTogether` spirals (`max |x| = 1187` over 400 steps).
- **`E7` (unified $m \in \{+1, 0, -1\}$ family):** Exact same two-accumulator
  machine switching solely on feedback sign `m` to produce oscillation
  (`sin`/`cos`), linear coasting (`r · d`), and hyperbolic/Napier exponential
  growth (`exp`/`log`).
- **Waveform refinement & inverse query tests:** Constant-sign pull (parabolic
  wave) vs. distance-proportional pull (sine wave); multi-moment `AsRelation()`
  recovering initial side/phase backwards from later observations.

### Phase 5 — Derived compiled backend prototype (`NEON` / SIMD / ALU / `libm`)
Build a small, strictly opt-in `CompiledPlan` layer outside canonical
`Transport` and guarded by `IsCurrent`:
1. **SIMD bit-sliced `Plane` backend:** Accelerate `Plane` bitwise operations
   and small `RelationLaw` / `TransitionLaw` stepping via
   `System.Runtime.Intrinsics` (`Vector128` / ARM NEON, `Vector256` / AVX2,
   portable `Vector<ulong>` fallback).
2. **`E13` (recognized radix-`B` ALU fast path with deopt guard):** Recognize a
   sealed uniform-radix adder or multiplier strictly from verified table bits +
   `Accounting` weights + stiffness-rung constants (no string tags!), lower
   guarded settled lanes to widening `ADD`/`BigMul` with exact `T+` overflow
   capture, deopt open/broken/aliased/contested lanes to canonical stepping, and
   verify that any structural edit invalidates `IsCurrent` immediately.
3. **Regime 2 declared finite-read math-library lowering:** Opt-in lowering for
   a crystallized continuous-target oscillator behind an explicit integer-tick
   tolerance and provenance contract.
4. **Differential conformance & honest benchmark report:**
   - Bit-for-bit parity between canonical `EncodingC` and Regime 1 compiled
     execution across randomized settled, open, broken, overflow, aliased, and
     rewired cases.
   - Declared-bound conformance for Regime 2 finite-read lowering.
   - Report canonical saved bytes, working memory, lanes/sec (canonical vs. SIMD
     vs. ALU fast path), and deopt fallback rates.

---

## 9. Open decisions for next discussion

1. **Default open-support rung radix (`B = 4` vs. `B = 8` vs. `B = 16`):**
   Radix 4 keeps open supports tiny (4 planes/rung) and tables minuscule while
   packing cleanly as 2 bits/rung into NEON/CPU integers; radix 8 or 16 cuts
   carry depth at higher open-support cost; radix 60 maximizes exact
   reciprocals. Recommend defaulting to **radix 4** for open relational ladders
   and widening power-of-2 packing once settled.
2. **Primary relational multiplier (`E2` lattice vs. duplation vs.
   quarter-squares):** Phase 3 should measure all three on saved bytes, steps,
   and backward factoring (`? × ? = 12`) before promoting one to encoding core.
3. **Partial vs. atomic reservoir scaling:** When material multiplication draws
   from an explicit supply reservoir that runs dry partway through, should it
   commit partial scaled stock plus `T−` draw on the reservoir, or hold the step
   atomically (as `Flows.Convert` currently does per quantum)?
4. **Cross-engine schedule encoding:** For gap 1 (§7), should coupled
   multi-engine step order live in a parent composite `Engine` (via `Fuse` /
   wave schedule), or as an explicit saved schedule structure referencing peer
   engines?
5. **Kernel bridge scope:** Keep Phases 0–5 inside `Gojo.EncodingC` (per
   [AGENTS.md](../../AGENTS.md) promotion rules) while adding one narrow bridge
   test checking that a crystallized `EncodingC` carry ladder matches
   `Gojo/Core`'s [`ResolutionLadderTests.cs`](../../Tests/Gojo.Tests/Core/ResolutionLadderTests.cs)
   exact projection/residue contract?

---

## 10. Sources

Checked during preparation of this note (marked `*`) and recalled from reading:

- *Repository sources: [GojoSystemPrompt.md](GojoSystemPrompt.md),
  [GojoTension-2026-11.md](GojoTension-2026-11.md),
  [GojoStructureClosure-2026-11.md](GojoStructureClosure-2026-11.md),
  [GojoEncodingConsolidation-2026-11.md](GojoEncodingConsolidation-2026-11.md),
  [GojoEncodingCCloudHandoff-2026-11-19.md](GojoEncodingCCloudHandoff-2026-11-19.md),
  [GojoCore2Core4ContinuityReview-2026-09.md](GojoCore2Core4ContinuityReview-2026-09.md),
  [GojoComplexInterpretations-2026-09.md](GojoComplexInterpretations-2026-09.md),
  [GojoFiniteReadFixture.md](GojoFiniteReadFixture.md),
  [GojoMaterialRefinement.md](GojoMaterialRefinement.md),
  [Gojo Operation Shape Catalog.md](../axioms/Gojo%20Operation%20Shape%20Catalog.md),
  [`Gojo.EncodingC`](../../Gojo.EncodingC/README.md) (`Odv.cs`, `Sides.cs`,
  `Paths.cs`, `Flows.cs`, `Accounting.cs`, `Wiring.cs`, `Engine.cs`, `Gap.cs`,
  `LawTests.cs`, `SelfRunningTests.cs`, `FlowTests.cs`, `RuntimeTests.cs`,
  `TwoFaceTests.cs`),
  [`Relation.Operations.cs`](../Core/Relation.Operations.cs),
  [`Relation.LiveOutput.cs`](../Core/Relation.LiveOutput.cs),
  [`OperationFollow.cs`](../Structures/OperationFollow.cs),
  [`Equation.cs`](../Equations/Equation.cs),
  [`FiniteLawPlan.cs`](../../experiments/QualitativeExecution/FiniteLawPlan.cs),
  [`RaceOrder.md`](../../experiments/PrimitiveMeasure/RaceOrder.md),
  [`OperationConformanceTests.cs`](../../Tests/Gojo.Tests/Core/OperationConformanceTests.cs),
  [`ResolutionLadderTests.cs`](../../Tests/Gojo.Tests/Core/ResolutionLadderTests.cs),
  [`FiniteReadSurfaceTests.cs`](../../Tests/Gojo.Tests/Core/FiniteReadSurfaceTests.cs),
  [`EquationSequenceTests.cs`](../../Tests/Gojo.Tests/Equations/EquationSequenceTests.cs),
  and historical `Core4`, `Core2`, and `Numerics` repositories.
- *Author design articles (Google Drive `Articles/` & `math/`): *Pin,
  Accumulate, and Merge*; *Multiplying AI Segments*; *Boolean Arithmetic with
  Segments*; *Sequential Math, Parallel Math*; *Segment Math and Symmetry*;
  *Mathematics Without Infinity*; *Thinking in Directed Segments*; *Core4
  Bootstrap*; *UniversalMath*; *Core 2 Prompt Axioms*.
- *History: Høyrup, J. (2002), *Lengths, Widths, Surfaces* (Springer) & (2017),
  *Algebra in Cuneiform* (MPRL); McFarland on quarter-square speculation;
  Euclid, *Elements* Books II, V, VI, VII; Fowler (1987/1999), *The Mathematics
  of Plato's Academy*; Rhind Mathematical Papyrus; *Jiuzhang Suanshu* (Nine
  Chapters); Āryabhaṭa (499 CE) second-difference sine table; Viète (1591);
  Descartes (1637), *La Géométrie*; Werner/Wittich/Brahe/Bürgi
  (prosthaphaeresis); Napier (1614) & Oughtred (slide rule); Bombelli (1572);
  *Treviso Arithmetic* (1478).
- *Measurement, conservation & machines: Stevens, S. S. (1946), *On the Theory
  of Scales of Measurement*; Vergnaud, G. (1988), conceptual fields;
  Grassmann, H. (1844/1862), *Die Ausdehnungslehre*; Spencer-Brown, G. (1969),
  *Laws of Form*; Peirce, C. S., Existential Graphs; Shannon, C. E. (1938),
  relay circuits; Paynter, H. M. (1961), *Bond Graphs*; Bonchi, Sobociński &
  Zanasi (2014–2025), *Interacting Hopf Algebras* / *String Diagrams*; Chen,
  Doty & Soloveichik (2012/2014), deterministic chemical reaction networks;
  Fredkin & Toffoli (1982), *Conservative Logic*; Walther (1971), unified
  CORDIC; Minsky (1972), HAKMEM Item 149; Hairer, Lubich & Wanner (2006),
  *Geometric Numerical Integration*.
- Recalled from reading (not re-verified for this revision): Brahmagupta (628
  CE, *Brāhmasphuṭasiddhānta*); Archimedes, *On the Equilibrium of Planes*;
  Radul & Sussman (2009), *The Art of the Propagator*; Avizienis (1961),
  signed-digit arithmetic; Hölzle, Chambers & Ungar (1992), dynamic
  deoptimization.

---

## Appendix A: Reproducible numeric and encoding checks (Python)

Run directly with `python3` to reproduce every numeric figure cited above:

```python
import math

def coarse(n, shift):
    """Truncate toward zero by 2**shift (integer coarse read)."""
    return int(n / (1 << shift))

def run_osc(x, v, shift, steps, in_order=True, m=1):
    path = [(x, v)]
    for _ in range(steps):
        if in_order:
            x = x + coarse(v, shift)
            v = v - m * coarse(x, shift)
        else:
            x, v = x + coarse(v, shift), v - m * coarse(x, shift)
        path.append((x, v))
    return path

# 1. Step order: declared (symplectic) closes; StepTogether spirals.
p_ord = run_osc(64, 0, 3, 400, in_order=True)
period = next(i for i, q in enumerate(p_ord[1:], 1) if q == p_ord[0])
p_tog = run_osc(64, 0, 3, 400, in_order=False)
print(f"1. in-order: period={period}, max|x|={max(abs(q[0]) for q in p_ord)} | "
      f"together: max|x|={max(abs(q[0]) for q in p_tog)}")

# 2. One machine, three signs m in {+1, 0, -1} + rung-0 Gray cycle.
for m in (1, 0, -1):
    p = run_osc(64, 64, 3, 24, True, m)
    print(f"2. m={m:+d}: x@[0,8,16,24] = {[p[i][0] for i in (0, 8, 16, 24)]}")
print(f"   m=0 coast (rate 5, 7 steps): x = {run_osc(0, 40, 3, 7, True, 0)[-1][0]}")
s = (1, 1); gray = [s]
for _ in range(4):
    s = (s[1], 1 - s[0]); gray.append(s)
print("   rung-0 Gray cycle:", " -> ".join(f"{a}{b}" for a, b in gray))

# 3. Aryabhata (499 CE): second difference = -(sine so far)/coupling, R=3438.
for c in (225.0, 1 / (2 - 2 * math.cos(math.radians(3.75)))):
    d, s, vals = 225.0, 0.0, []
    for _ in range(24):
        s += d; vals.append(round(s)); d -= s / c
    print(f"3. Aryabhata 1/{c:.1f}: first5={vals[:5]}, peak={max(vals)} (R=3438)")

# 4. Path provenance: Delta(a*b) = a*db + b*da + da*db.
def pair_path(arrivals):
    a = b = by_a = by_b = corners = 0
    for da, db in arrivals:
        if da and db: by_a += b; by_b += a; corners += 1
        elif da:      by_a += b
        elif db:      by_b += a
        a += da; b += db
    return (a * b, by_a, by_b, corners)
print("4. L-path   (3A then 4B):", pair_path([(1, 0)] * 3 + [(0, 1)] * 4))
print("   diagonal (3AB then B):", pair_path([(1, 1)] * 3 + [(0, 1)]))

# 5. Bitwise across encodings: bit rung vs unary thermometer rung.
th = lambda n: (1 << n) - 1
u, v = 5, 2
print(f"5. bit 1&1={1&1}(mul/carry), 1|1={1|1}(clamp+), 1^1={1^1}(wrap+)")
print(f"   unary(5,2): AND={bin(th(u)&th(v))}(min={min(u,v)}), "
      f"OR={bin(th(u)|th(v))}(max={max(u,v)}), "
      f"XOR={bin(th(u)^th(v))}(span len={bin(th(u)^th(v)).count('1')})")

# 6. Numeric ladder edge cases: observe-vs-add and bounded saturation non-associativity.
t_after, r_after = divmod(17 + 28, 10)
ta, ra = divmod(17, 10); tb, rb = divmod(28, 10)
clamp = lambda x: max(0, min(15, x))
print(f"6. 17+28: add->obs=({t_after} tens, {r_after}) vs obs->add=({ta+tb} tens, {ra+rb} rem)")
print(f"   clamp[0,15]: (14+3)-3={clamp(clamp(14+3)-3)} != 14+(3-3)={clamp(14+clamp(3-3))}")

# 7. Table-size explosion & quarter-square exactness.
assert all(((i+j)**2 - (i-j)**2) == 4 * i * j for i in range(16) for j in range(16))
sizes = [(n, n * n * ((n - 1) ** 2 + 1), 2 * n - 1) for n in (4, 8, 16)]
print("7. (radix, 3-role bits, 1D square entries):", sizes)
```

Verified output (24 November 2026):

```text
1. in-order: period=54, max|x|=64 | together: max|x|=1187
2. m=+1: x@[0,8,16,24] = [64, 91, 43, -43]
2. m=+0: x@[0,8,16,24] = [64, 128, 192, 256]
2. m=-1: x@[0,8,16,24] = [64, 166, 440, 1191]
   m=0 coast (rate 5, 7 steps): x = 35
   rung-0 Gray cycle: 11 -> 10 -> 00 -> 01 -> 11
3. Aryabhata 1/225.0: first5=[225, 449, 671, 890, 1105], peak=3375 (R=3438)
3. Aryabhata 1/233.5: first5=[225, 449, 671, 890, 1106], peak=3440 (R=3438)
4. L-path   (3A then 4B): (12, 0, 12, 0)
   diagonal (3AB then B): (12, 3, 6, 3)
5. bit 1&1=1(mul/carry), 1|1=1(clamp+), 1^1=0(wrap+)
   unary(5,2): AND=0b11(min=2), OR=0b11111(max=5), XOR=0b11100(span len=3)
6. 17+28: add->obs=(4 tens, 5) vs obs->add=(3 tens, 15 rem)
   clamp[0,15]: (14+3)-3=12 != 14+(3-3)=14
7. (radix, 3-role bits, 1D square entries): [(4, 160, 7), (8, 3200, 15), (16, 57856, 31)]
```
