# Operations: from physical relations to compiled hardware

Status: **proposal**, 24 November 2026, on branch `ops_neon`. Nothing beyond
existing `Gojo.EncodingC` primitives is built yet except where explicitly marked
**implemented**. This document follows the epistemic discipline of
[GojoSystemPrompt.md](GojoSystemPrompt.md) (§1), builds on
[GojoStructureClosure-2026-11.md](GojoStructureClosure-2026-11.md) and
[GojoTension-2026-11.md](GojoTension-2026-11.md), and records the architectural
exploration for implementing Boolean, additive, multiplicative, and stacked
dynamical operations—while keeping crystallized structures cleanly compilable to
modern CPU, SIMD (ARM NEON / x86 AVX), GPU, FPGA, and math-library backends.

---

## 1. The stance — and why operations and numerics meet at multiplication

Gojo does not begin with symbolic operators (`+`, `−`, `×`, `÷`, `sin`) and ask
host code to evaluate them. It builds connected structure whose local rules
absorb, narrow, route, and preserve participation
([GojoSystemPrompt.md](GojoSystemPrompt.md), §9.6). In
[`Gojo.EncodingC`](../../Gojo.EncodingC/README.md), everything that runs is one
of two table lookups across planes:

- a **`TransitionLaw`** changes concrete bits under checked `Accounting`
  promises (such as positive-weight `PreservedSum` conservation);
- a **`RelationLaw`** narrows open `Supports` in every direction without
  mutating settled state.

The working question is: **how should Boolean, additive, multiplicative, and
stacked operations (`sin`/`cos`, `exp`/`log`, powers) live in this substrate,
before and after multi-rung numerics arrive?**

### Pushback on "solidify operations first, bolt on numerics later"

Solidifying the *operational semantics* first is the right instinct for
**Boolean**, **ordering/sides**, **material conservation**, and **fixed-ratio
scaling**. All four already work pre-numerically in `EncodingC` today.

However, **variable multiplication (`A * B`) cannot be cleanly separated from
partitions and rungs**, for two hard structural reasons:

1. **The unit/grade wall (semantic).** Without an explicit unit partition `1`,
   multiplying two 1D magnitudes cannot stay 1D—it produces a 2D bilinear
   structure (an area, rectangle, or ordered pair). Staying on a 1D carrier
   secretly turns `A * B = C` into a **4-place calibrated relation**
   `1 : A :: B : C` (Descartes, 1637; §3.4 below). The earlier `Core4 Bootstrap`
   experiment ran into the same wall from the opposite direction: merging two
   partitioned structures naturally multiplied their partitions, forcing an
   artificial `0`-partition override just to make addition stop multiplying!
2. **The table-product wall (computational in `EncodingC`).** Every law table
   stores one bit per combination of role values, and one primitive support read
   deliberately caps at 64 distinctions (`LawTests.OnePrimitiveReadTellsApartAtMost64Values`).
   A single 3-role multiplication table `(a, b, a·b)` explodes immediately:
   - `0..3`: `4 × 4 × 10 = 160` bits (tiny, coarse);
   - `0..7`: `8 × 8 × 50 = 3,200` bits;
   - `0..15`: `16 × 16 × 226 = 57,856` bits, and `226 > 64` exceeds a single
     primitive support read.

   Wide multiplication therefore *cannot* live as one wide primitive table. It
   must either **factor across coarse rungs/partitions** (place-value carries,
   Egyptian doubling, or Babylonian quarter-squares) or **unroll across time**
   (clocked accumulation).

**Working rule:** Solidify the *kinds* of operations and *where they live* now,
**without forcing one monolithic `Multiply` primitive before partitions and
rungs exist.** Keep fixed scaling, bilinear pairing, iterated accumulation, and
unit-calibrated products distinct from the start; multi-rung numerics then slot
into the exact seams already left for them.

---

## 2. Lineage: what prior iterations and design articles already proved

Using the canonical prompt's status terms (**implemented**, **documented
direction**, **exploratory**, **superseded**):

| Generation / Source | Status | How operations were expressed | Carry forward | Do not re-import |
| --- | --- | --- | --- | --- |
| **Drive Articles** (`Pin, Accumulate, and Merge`, `Multiplying AI Segments`, `Boolean Arithmetic with Segments`, `Sequential Math, Parallel Math`, `Mathematics Without Infinity`, `UniversalMath`) | Design intuition | Three stages: **Pin** (shared context without loss) → **Merge** (1:1 additive knit, 1:ALL multiplicative stretch/blend, 16 Boolean quadrant masks) → **Fold** (collapse with explicit loss). | Separating *pinning* (operands survive) from *folding* (lossy collapse); 4-quadrant bridge between Boolean masks and products; distinguishing parallel (`\`) vs sequential (`L`) composition; finite bounds without infinity. | Leaving execution to informal prose or host orchestration. |
| **`Numerics`** (`NumericsCore/Expressions/Operations.cs`) | Superseded | OOP `OperationBase` / `BinaryOperationsBase` subclasses with `Calculate`, `CalculateInverse`, and `CalculateInPlace`. | Explicit forward/inverse pairing and in-place vs new-result distinction. | Putting operational meaning in C# class hierarchies. |
| **`ResoEngine / Core2`** (`Core2/Algebra`, `Core2/Boolean`, `Axioms.md` Parts I–X) | Superseded | Asymmetric pinning `A * B @ P` (recessive host `A`, dominant applied transform `B`); encoding vs action duality (`3` vs `S_3`, `i` vs `J`); Boolean as frame-relative piece families on constant partitions. | Host/applied asymmetry; Boolean as partitioned occupancy over an explicit frame yielding co-present pieces rather than one bare scalar. | Coupling denominator sign, orthogonal breakout, and complex `i` into one `Axis`/`Proportion` object ([continuity review](GojoCore2Core4ContinuityReview-2026-09.md)). |
| **`Core4.Kernel`** (`RelationFold.cs`, `Core4 Bootstrap`) | Superseded | One binary `Relation(A, B)` interpreted by `RelationFold`: combining partitioned structures multiplies bases; a `0`-partition override blocks interior mixing to produce addition. | Deep physical law: **addition preserves partition grain; multiplication substitutes/combines bases**. | Overloading one binary `RelationFold` with `0`-denominator special-case dispatch. |
| **`Gojo` C# Kernel** ([`Relation.Operations.cs`](../Core/Relation.Operations.cs), [`Equation.cs`](../Equations/Equation.cs), [Operation Shape Catalog](../axioms/Gojo%20Operation%20Shape%20Catalog.md)) | **Implemented** (kernel) | Exposed operational ports: `(portA -> portB) -> P(out,1)` = additive collapse (`AlignedProduct`); `-> S(out,0,1)` = multiplicative (`OrthogonalProduct`), preserving opposed-face terms before optional fold. Projection = read; [`Relation.LiveOutput.cs`](../Core/Relation.LiveOutput.cs) / settlement = mutation. | Clean split between observation (with source residue) and mutation (via tension); explicit unit algebra; Boolean as cross-grade participation settlement. | Relying on host C# code (`TryReadOperationSignalFlow`) to evaluate operations while the graph merely describes them. |
| **`Gojo.EncodingC`** ([README](../../Gojo.EncodingC/README.md), [GLOSSARY](../../Gojo.EncodingC/GLOSSARY.md)) | **Exploratory** (active) | Self-running bit tables: `TransitionLaw` + `Accounting`, `RelationLaw` + open `Supports`, `Flipped` live views, `Workspace`/`Fork`, `Closure`. | **Already built:** 1-byte bidirectional Boolean (`AND = 0x87`, `OR = 0xE1`, `XOR = 0x69`), bidirectional count add/sub (`LawTests`), `Odv.Addition(Detection)`, fixed-ratio `Flows.Convert(take, give)`, 2-rung carry (`WhatDoesNotFitIsCarriedUpARung`), `Sides` events, and `Absorb`/`Draw`/`Cancel` as one `Flows.Move` law via `Flipped` room views ([tension note](GojoTension-2026-11.md)). | Missing: variable multiplication, whole-structure scaling, multi-site Boolean, stacked dynamics (`sin`/`cos`), and derived hardware lowering. |

---

## 3. Historical and physical foundations: why `+`, `−`, `×`, `÷` are not four things

Modern algebraic notation collapses physically distinct operations onto four
overloaded symbols. Earlier mathematical traditions—and physical/diagrammatic
theories of computation—preserved distinctions that map almost one-to-one onto
Gojo's structure.

### 3.1 Old Babylonian mathematics (1800–1600 BCE)

Jens Høyrup's conformal translations (*Lengths, Widths, Surfaces*, 2002;
*Algebra in Cuneiform*, 2017) show that Old Babylonian calculators never treated
addition, subtraction, or multiplication as single monolithic operations. Their
distinct verbs match Gojo's exact architectural seams:

| Modern symbol | Old Babylonian operation | Physical meaning | Gojo `EncodingC` counterpart |
| --- | --- | --- | --- |
| `+` | ***waṣābum*** (*daḫ*, "join/append `j` to `S`") | **Asymmetric, in-place.** `S` stays put and keeps its identity while absorbing `j` (like interest *ṣibtum* joined to a bank deposit `S`). No new name for the sum. | Conserved material transfer / absorption (`Flows.Move` into `S`, `T+` absorb). |
| `+` | ***kamārum*** (*gar.gar*, "heap `a` and `b`") | **Symmetric, new entity.** Produces a named result (*nakmartum*, "the heap", or plural *kimrātum* when constituents stay distinct). Can heap heterogeneous measures (e.g. a length and an area). | Non-mutating bidirectional `RelationLaw(a, b, sum)` or observational fold into a fresh target. |
| `−` | ***nasāḫum*** (*zi*, "tear out `a` from `B`") | **In-place inverse of *waṣābum*.** Presupposes `a` is physically inside `B`; leaves diminished remainder (*šapiltum*). | Material draw (`T−` moving into `Flipped` room view of `B`). |
| `−` | **Comparison** ("`A` over `B`, `d` goes beyond") | **Non-mutating relation** between two separate magnitudes yielding excess (*watartum*/*dirig*) or falling-short deficit. | Two-faced `Gap` relation (`have, want, deficit, excess`) and `Sides.Compare`. |
| `×` | ***našûm*** (*íl/nim*, "to raise") | **Scaling by a factor/rate** (originally raising a base of standard thickness 1 cubit to height `h`). | Fixed calibration *on a connection* (`Flows.Convert(take, give)`) or port scale. |
| `×` | ***šutakūlum*** (*gu₇.gu₇*, "make hold each other") | **Grade-raising construction** of a rectangle/surface from two segments, preserving both sides. (*šutamḫurum* / *mitḫartum*, "confrontation", for squaring a side.) | Bilinear pair / 2D structure / `UnitShape.OrthogonalProduct` (unfolded product). |
| `×` | ***a.rá* / *alākum*** ("steps of" / "going") | **Iterated repetition** (`3 steps of 6 are 18`, or joining `s` `n` times to `B`). | Clocked accumulation / repetition controller. |
| `×` | ***eṣēpum*** ("repeat/double physically", `n ≤ 9`) | **Concrete structural replication** (e.g. doubling a right triangle to make a rectangle). | Topological replication of a whole structure (`RepeatSurface`). |
| `÷` | **Relational query + *igi* table** | Division is not a primitive; scribes ask *"what shall I raise to `d` to get `P`?"* and multiply by the tabulated reciprocal (`igi`). Non-regular divisors explicitly report *"7 does not divide"* (residue/approximation). | Running a multiplication `RelationLaw` backwards, with explicit remainder/unresolved support. |
| `÷ 2` | **Scaling by `0;30`** vs ***bāmtum* / *ḫepûm*** | Arbitrary multiplicative half vs structural "moiety" obtained by "breaking" a whole (radius of a diameter, half-base of a triangle). | Calibrated ratio `Convert(2, 1)` vs structural bisection/midpoint landmark. |

Two additional Babylonian techniques speak directly to `EncodingC`:

- **Floating sexagesimal place-value** had no fixed radix point (`30` could mean
  `30`, `30 × 60`, or `1/2`, with scale supplied by context)—matching Gojo's
  separation of **rung digits** from **carrier calibration**.
- **Quarter-square multiplication** via tables of squares (Senkereh tablets):
  $$a \cdot b = \frac{(a + b)^2 - (a - b)^2}{4}$$
  Instead of an $O(N^3)$ 3-role multiplication table, squaring is a **2-role
  ($O(N)$ entries) table** `n <-> n²`, reducing multiplication to 1D lookups
  plus addition and subtraction—and keeping the whole network **bidirectional**!

### 3.2 Ancient Greece (Euclid Books II, V, VI, VII; Theaetetus; Eudoxus)

- **Discrete multitude (*arithmos*) vs continuous magnitude (*megethos*).**
  Counting and measuring are different acts; equality of ratios does not require
  measuring either magnitude against a global ruler first.
- **Pre-numeric ratios (*anthyphairesis* and Eudoxus, Book V Def. 5).** Two
  magnitudes stand in a ratio *before numbers exist*, compared via mutual
  subtraction (continued fractions) or equimultiples (`mA ⋚ nB` iff
  `mC ⋚ nD`). Ratio and scaling therefore belong to **connections** before
  numeric rungs exist.
- **Application of areas (*parabolē*, *elleipsis*, *hyperbolē*).** Fitting an
  area onto a baseline produces exact fit (*parabolē*), **deficit**
  (*elleipsis*), or **excess** (*hyperbolē*)—the exact geometric prototype of
  two-faced tension (`T−` and `T+`).

### 3.3 Ancient Egypt, China, and India

- **Egyptian duplation (Rhind Papyrus).** Multiplication requires **no
  multiplication table at all**: only repeated doubling (`1×, 2×, 4×, 8×, ...`,
  a 1-operand shift/rung carry) plus a **Boolean selection plane** choosing
  which doubled terms enter the sum. Multiplication decomposes cleanly into
  **Rungs + Boolean Selection + Addition**.
- **Chinese counting rods (*Jiuzhang Suanshu*) and Indian *dhana*/*ṛṇa*
  (Brahmagupta).** Positive and negative quantities are kept on physically
  distinct carriers (red vs black rods; fortunes vs debts) with explicit rules
  for zero (*śūnya*)—mirroring `TwoFaceTests` and unnetted `T+`/`T−`. And
  Chinese *fangcheng* elimination applies uniform scaling across an entire
  column (whole-structure broadcast).

### 3.4 The Renaissance & Early Modern pivot: Viète → Napier → Descartes

- **François Viète (*lex homogeneorum*, 1591) vs René Descartes (*La Géométrie*,
  1637).** Viète enforced strict dimensional rank: only like grades add, and
  multiplication climbs ranks (`line → plane → solid`, echoing Gojo Axiom 10).
  Descartes broke classical homogeneity by declaring an **explicit Unit segment
  `1`** and defining `a × b` as the **fourth proportional** `1 : a :: b : x` via
  similar triangles. That single move exposed a permanent structural truth:
  **same-grade multiplication always hides a fourth participant—the Unit
  calibration.**
- **Prosthaphaeresis (Ibn Yunus, Werner, Wittich, Brahe, Bürgi, 1580s):**
  multiplied numbers via trigonometric tables using
  $$\cos\alpha \cdot \cos\beta = \frac{1}{2}\big(\cos(\alpha - \beta) + \cos(\alpha + \beta)\big)$$
  turning products into phase addition and halving.
- **John Napier's kinematic logarithms (1614) and William Oughtred's slide
  rule.** Napier did not define logarithms algebraically with exponents; he
  defined them **physically using two points moving along lines**: one moving at
  constant speed (additive carrier), the other moving toward a fixed landmark at
  speed proportional to its remaining distance (multiplicative/geometric
  carrier). Oughtred's slide rule then performed multiplication literally by
  adding extents on a logarithmically calibrated carrier.
- **Rafael Bombelli (*L'Algebra*, 1572):** treated `+i` and `−i` as signed
  orientation operators acting on magnitudes rather than mysterious numbers.

### 3.5 Modern structural and physical computation

- **Hermann Grassmann (*Ausdehnungslehre*, 1844/1862):** separated the
  grade-raising **outer product** (*šutakūlum*), the grade-lowering **inner /
  regressive contraction**, and **scalars as ratios of homogeneous extensive
  magnitudes**.
- **G. Spencer-Brown (*Laws of Form*, 1969) & C. S. Peirce (Existential
  Graphs):** grounded Boolean algebra in a single spatial act—**drawing a
  distinction (boundary)** and **crossing it (inversion)**—matching Gojo's
  polarity rule (`1 = presence`, `0 = absence`, §9.7) and `Flipped` views.
- **Henry Paynter (*Bond Graphs*, 1961):** gave the crispest physical criterion
  for *connection vs. structure*:
  - A **fixed-ratio Transformer (`TF`)** (`e₁ = k·e₂`, `f₂ = k·f₁`, conserving
    power) lives **on a 2-port connection**—exactly `Flows.Convert(take, give)`.
  - A **Modulated Transformer (`MTF`)** has a **third information port** reading
    a modulating signal `A` that varies the connection rate between `B` and
    `C`—exactly variable multiplication `A × B`!
- **Signal Flow Graphs / Interacting Hopf Algebras (Bonchi, Sobociński, Zanasi,
  2014–2025):** prove that all linear wiring reduces to four primitives:
  **copy** (information fanout), **add** (material merge), **scale** (wire
  gain), and **discard/zero**. Variable multiplication `A × B` is the exact
  threshold where linear wiring ends and a bilinear 3-port structure begins.
- **Propagator Networks (Radul & Sussman, 2009):** multidirectional constraint
  cells refining partial-information supports—the direct computer-science
  cousin of `EncodingC`'s `RelationLaw` + `Supports`.
- **Symplectic / Leapfrog / Minsky discrete harmonic oscillator (Verlet;
  Minsky, HAKMEM #149):** turns the landmark + momentum intuition for `sin`/`cos`
  into an **exact integer/unary closed orbit** with zero floating-point math
  (§4.5).

---

## 4. Resolving the core design questions

### 4.1 On connections vs. as structures of their own

Use the **Bond Graph + `EncodingC` arity/state test**:

| Place | What belongs here | Examples in `EncodingC` | Why |
| --- | --- | --- | --- |
| **Live View (`Surface`)** | Zero-copy reinterpretation of existing planes | `Flipped` (NOT, `Odv.Reoriented`, unary room/anti-fill), future framed/scaled port view | No bits copied or mutated; reading and writing pass straight through. |
| **On a Connection (`Wiring` row)** | Stateless, fixed-parameter 2-port transfers, rates, coarse reads, and single-bit gates | `Flows.Move`, `Flows.Convert(take, give)` (*našûm* / `TF`), `Flows.Passage`, `Flows.GatedMove`, `Sides.Compare`, 1-byte Boolean `RelationLaw`s | Rate or polarity is crystallized in the small shared table; no internal memory or 3rd dynamic operand needed. |
| **Structure of its own (`Engine` / multi-row mechanism with `Port`s)** | Anything with **3+ dynamic participants**, **internal state/memory**, **carry ladders**, or **multi-site spatial scope** | Variable `A * B` (duplation, quarter-square, `MTF`), unfolded 2D *šutakūlum* pair, multi-rung arithmetic ladder, spatial Boolean settlement, `sin`/`cos` oscillator, Napier `exp`/`log` | Needs local scratch positions, phase/momentum state, carry wires, or sealed ports so external structures see only its interface (Closure C7/C8). |

Because `EncodingC` allows `Engine.Fuse` and thin `Port`/`Socket` connections,
this boundary stays fluid—matching [GojoSystemPrompt.md](GojoSystemPrompt.md)
§4.6: a rate or small operator can be absorbed into a sender/receiver when
private, or stand alone as a shared structure with ports when reused.

### 4.2 Directional (`A op B` modifies `B`) vs. new result (`A op B -> C`)

**Both are physically real, and `EncodingC` already encodes the difference
without any opcode flag**, purely from **law kind + written roles + conservation**:

1. **In-place modification (`A` acts on `B`, mutating `B` — *waṣābum* / *nasāḫum* / Core2 `host * applied`):**
   - Encoded as a **`TransitionLaw`** where `A` is either read-only
     (`Unchanged(0)`, control/information) or drained (`PreservedSum`, material
     flow), and `B` is written.
   - **The Conservation Trap (critical physical constraint):** Material
     addition `A -> B` (`Flows.Move`) trivially conserves `A + B`. Material
     in-place multiplication (`3 × B` overwriting `B`) **cannot conserve `B`
     alone**—where did the extra `2B` units come from? Physically, scaling a
     quantity in place is only legal in three distinct ways:
     1. *Unit-grain conversion*: `Convert(1, 3)` turns 1 coarse unit into 3 fine
        units, conserving weighted sum `3·coarse + 1·fine`.
     2. *Reservoir-backed amplification*: a 3-role transition draws the extra
        units from an explicit supply reservoir (leaving `T−` draw if the
        reservoir runs dry!).
     3. *Informational / geometric update*: rewriting a coordinate, reference,
        or supposition inside a local structure (no material conservation
        promise claimed; cannot write directly through a foreign port!).
2. **New result (`A op B -> C`, keeping `A` and `B` intact — *kamārum* / *šutakūlum* / Descartes):**
   - Encoded as a **`RelationLaw(A, B, C)`** (runs in every direction: forward
     evaluation, backward subtraction/division/factoring, open support
     propagation) or a **`TransitionLaw(A [read], B [read], C [write])`**
     (functional publication into a fresh target store).
   - Preserves provenance: if a downstream claim detects error at `C`, `A` and
     `B` are still there to inspect or revise.
3. **Unification via `Workspace` / `Fork`:**
   - Running an in-place operator inside a [`Workspace`](../../Gojo.EncodingC/Workspaces/Workspace.cs)
     or [`Fork`](../../Gojo.EncodingC/Flow/Fork.cs) automatically turns it into
     a **non-destructive new result** (copy-on-write, with
     [`Gap`](../../Gojo.EncodingC/Workspaces/Gap.cs) exposing the exact delta!).
   - Calling `Commit` turns that supposition back into an **in-place edit**
     (refused atomically if the source moved).

### 4.3 What it means to multiply a larger structure (`k × Structure` or `A × Structure`)

"Multiply a whole house (or graph, or store) by 2" conflates **three genuinely
different physical operations**. Keeping them structurally distinct avoids
ambiguity:

1. **Port / Frame Re-Calibration (*našûm*, O(1) cost):**
   - Leave the interior untouched. Attach a scaling calibration (`Convert`, or
     a shared basis/unit carrier read at the port—analogous to constant plane
     `O` turning an entire `Odv` store in one bit).
   - All internal ratios, seams, and constraints stay invariant; external
     readers crossing the port read scaled magnitudes.
2. **Selected-Lane Broadcast (Chinese *fangcheng* / `EncodingC` lane-parallel `Wiring`):**
   - Apply a scaling wiring across every position selected by an `Attention` /
     selection plane, bound **only to measure planes** (never to row references,
     `Life`, or fixed topology).
   - **Physical payoff:** If scaling a substructure pushes a part against an
     unscaled local capacity or constraint (e.g., doubling wall length without
     widening a doorway's span limit), the conflict surfaces immediately as
     **local `T+` / contradiction tension** right where the structure strains!
3. **Structural Replication / Bilinear Product (*eṣēpum* / *šutakūlum*):**
   - `3 × Triangle` creates 3 linked instances (repetition / `RepeatSurface`),
     while `Segment(3) × Segment(4)` builds a 2D Cartesian product / sheet
     structure. Neither stretches existing numeric values; both multiply
     *topology*.

### 4.4 Boolean operations across grades (1 bit → spans → structures)

Following [GojoSystemPrompt.md](GojoSystemPrompt.md) §8.2 & §9.7 (`1 = presence`,
`0 = absence`):

1. **Negation / Complement without copying:** [`Flipped`](../../Gojo.EncodingC/Substrate/Flipped.cs)
   inverts selected planes live (bit NOT, unary room/anti-fill with reversed
   plane order, and `Odv.Reoriented`). Complement is always **frame/scope-relative**
   (`All & ~A` over a store's `Live` or selected area, never an unbounded
   universe).
2. **Selection / Filtering / Gating:** `Attention` planes and `Workspace`
   keep/ignore layers perform coarse-to-fine masking without copying or touching
   unselected material.
3. **Pointwise 1-byte bidirectional logic (implemented):** Every 2-input Boolean
   operation is one byte over `(a, b, out)` (`AND = 0x87`, `OR = 0xE1`,
   `XOR = 0x69`, etc., [`LawTests.cs`](../../Gojo.EncodingC/Tests/LawTests.cs#L188-L205)),
   running across all aligned positions in parallel and narrowing backwards
   (e.g., `out = 1` on `AND` forces both `a = 1` and `b = 1`).
4. **Spatial / Structural Boolean (spans, regions, paths):** Over [`Paths`](../../Gojo.EncodingC/Laws/Paths.cs)
   and [`Sides`](../../Gojo.EncodingC/Laws/Sides.cs), two segments or regions
   partition an explicit scope into four occupancy masks (`neither`, `A only`,
   `B only`, `both`), on which any of the 16 Boolean rules selects surviving
   participation while preserving open-boundary residue.
5. **Non-negotiable distinction:** `V = 0` (undetectable magnitude at current
   resolution), unselected (`Attention = 0`), empty support (contradiction), and
   Boolean `0` (absent presence) remain four different bit patterns.

### 4.5 Stacked physical operations: `sin`/`cos`, `exp`/`log`, and powers

#### Sine and cosine: Landmark + Momentum as a symplectic discrete oscillator

Your landmark-and-momentum picture—something accelerated toward a landmark,
gaining momentum, overshooting it, and then being pulled back from the other
side—maps **directly** onto `EncodingC` with no real numbers:

- **State:** two two-faced quantities (forward face `+` and opposed face `−`,
  never netted automatically unless a cancel law is wired, matching
  `TwoFaceTests` and [GojoTension-2026-11.md](GojoTension-2026-11.md)):
  - **Position `x`** relative to the landmark (`x+` = after landmark, `x−` =
    before landmark; at the coarsest rung, simply a [`Sides.Word`](../../Gojo.EncodingC/Laws/Sides.cs)).
  - **Momentum / velocity `v`** (`v+` = moving with orientation, `v−` = moving
    against orientation).
- **Coupled dynamics (two ordered waves per tick):**
  1. **Wave 1 — Kick (restoring acceleration toward landmark):**
     $$v_{t+1} = v_t - k \cdot x_t$$
     When on the positive side (`x+`), flow adds to `v−` (or draws from `v+`
     through its room view) at rate `k` (`Flows.Convert`); when on the negative
     side (`x−`), flow adds to `v+`.
  2. **Wave 2 — Drift (position advances by the *updated* momentum):**
     $$x_{t+1} = x_t + v_{t+1}$$
- **Why Wave 1 → Wave 2 order is mathematically essential:**
  - Updating `x` and `v` simultaneously (`Engine.StepTogether`, explicit Euler)
    has determinant $1 + k > 1$: every cycle injects artificial energy and the
    orbit spirals outward.
  - Updating `v` then `x` in **two waves** (symplectic Euler / leapfrog /
    Marvin Minsky's integer circle algorithm `x' = x - εy`, `y' = y + εx'`) has
    determinant **exactly 1**: it conserves a discrete shadow energy
    $H_{\text{shadow}} = \frac{1}{2}(v^2 + k x^2 - k x v)$ and stays on a
    **closed periodic orbit indefinitely in pure integers / unary counts**!
- **Resolution ladder of the same oscillator:**
  - *Rung 0 (pre-numeric, `Sides` only):* 4-phase discrete cycle
    `before/with → at/with → after/with → after/against → at/against → before/against`
    — structurally identical to Axiom 10's quarter-turn cycle
    `PF → PI → NF → NI → PF`!
  - *Rung 1+ (unary / multi-rung counts + stiffness `k`):* `x(t)` traces a
    discrete cosine wave and `v(t)` traces a discrete sine wave (90° out of
    phase), with amplitude set by initial displacement and period set by `k`.
  - *Bidirectional read:* wiring the two-wave transition `AsRelation()` across
    a multi-moment plan (just like `SelfRunningTests.Planner`) lets the exact
    same structure **run forward** to generate waves or **narrow backwards** to
    infer phase, amplitude, or landmark position from partial observations!

#### Exponential / Logarithm: Napier's first-order sibling

Drop the momentum carrier and feed position directly back into its own rate
toward the landmark (`x_{t+1} = x_t - k·x_t`), paired with a constant-rate clock
carrier (`t_{t+1} = t_t + 1`). That is literally **John Napier's 1614 kinematic
definition of the logarithm**! Moving `t` additively scales `x`
multiplicatively—so `exp`/`log` and `sin`/`cos` are the **1st-order and
2nd-order instances of the exact same landmark-feedback motif**.

#### Powers

As noted in `Sequential Math, Parallel Math` and [GojoSystemPrompt.md](GojoSystemPrompt.md)
§6, powers are **repeated multiplicative participation**—either sequential
(clocked self-multiplication with a repeat counter) or parallel (a chain of
linked scaling rungs tracking simultaneously, where rung `m` stays `a×` rung
`m-1`).

---

## 5. Three candidate architectures — and the recommended hybrid

All three approaches stay faithful to Gojo's axioms (no strings, no floats, no
semantic host dispatcher) and differ in where the architectural center of
gravity sits.

### Option A: Physical Three-Facet Split (Flow / Constraint / View)

- **Design:** Zero new substrate primitives in `EncodingC`.
  - *Views:* `Flipped` (NOT, room/anti-fill, reorientation).
  - *Connections:* `Flows.Move`, `Flows.Convert(take, give)` (fixed scaling),
    1-byte Boolean `RelationLaw`s, `Odv.Addition`.
  - *Local structures:* Clocked/Egyptian duplation multipliers, Babylonian
    quarter-square networks, unfolded 2D *šutakūlum* pair stores, multi-rung
    carry ladders, and two-wave symplectic oscillators.
- **Pros:** Minimal substrate; rock-solid material conservation; immediately
  expressible on analog/biological/FPGA substrates; preserves every Babylonian
  distinction naturally.
- **Cons:** Variable `A * B` requires multi-step clocking or explicit multi-row
  wiring boilerplate; whole-structure scaling has no built-in shared-frame view.

### Option B: Frame-and-Calibration First (Descartes's 4th Proportional + Modulated Views)

- **Design:** Lower `Gojo/Core`'s frame projection (`S(C0,a,b) -> S(C1,c,d)`)
  into `EncodingC` as a first-class calibrated basis/frame surface.
  - Every measure store references a shared frame (origin + unit partition).
  - Static scaling is an O(1) reframed port/view; variable `A * B` is a
    **Modulated Transformer (`MTF`)** where `A` drives the target span reading
    `B` (`1 : A :: B : C`); nonlinear frames (`log`, `sin`) turn addition into
    multiplication directly.
  - Whole-structure scaling (`k × Structure`) happens in O(1) by scaling the
    shared unit carrier.
- **Pros:** Deeply unifies `Gojo/Core`, the Drive articles, and `EncodingC`;
  cleanest story for whole-structure scaling and nonlinear scales.
- **Cons:** Adds new surface/projection machinery to `EncodingC`; 4-role
  `(u, a, b, c)` bit tables blow up unless factored anyway; risks blurring
  conserved material transfer with basis re-scaling unless strictly separated.

### Option C: Relational-First Pin-and-Fold with Workspace/Fork Enactment

- **Design:** Every operation is canonically a **non-destructive pinned
  `RelationLaw` network** (Boolean, add/sub, factored multiply via duplation or
  quarter-squares, temporal oscillator chains).
  - Directionality is never baked into the operation: narrowing open `C` yields
    a new result; narrowing `B_before -> B_after` inside a `Workspace`/`Fork`
    and committing yields in-place modification; binding `C` and leaving `A` or
    `B` open yields subtraction, division, factoring, roots, and inverse trig.
  - Material `TransitionLaw`s live as a separate enactment layer that executes
    settled transfers.
- **Pros:** Maximizes `EncodingC`'s strongest capability (uncertainty in the
  same planes, bidirectional inference, `Question` cones, `Fork` suppositions);
  eliminates forward/inverse/in-place duplication.
- **Cons:** Open supports cost 1 plane per value (forcing very coarse rungs,
  e.g. radix 2–4); local narrowing is arc-consistent only (coupled loops like
  `x + y = 5, x·y = 6` need probes/suppositions); pure relations still need
  Option A's conserving transitions for real material flow.

### Side-by-side comparison

| Criterion | Option A (Three-Facet) | Option B (Frame-First) | Option C (Relational-First) | **Recommended Hybrid** |
| --- | --- | --- | --- | --- |
| New `EncodingC` primitives needed | None | Medium (framed views / projections) | Low (factored relation builders) | **Minimal** (shared-basis port pattern only) |
| Material conservation safety | Strong (checked `PreservedSum`) | Medium (risk of ruler/matter blur) | Strong (separate enact layer) | **Strong** |
| Bidirectional / inverse (`−`, `÷`, roots) | Partial (relations only) | Medium | Native everywhere | **Native on relational ops** |
| Whole-structure scaling (`k × Structure`) | Manual broadcast / port convert | Native O(1) shared frame | Via workspace/selection | **All 3 kept explicit** (port, broadcast, replicate) |
| Multi-rung numerics fit | Clean (carry chain) | Clean (subdivided frames) | Clean (layered carry relations) | **Clean (uniform-radix carry ladders)** |
| Hardware / NEON / GPU / FPGA compilability | **Very high** | High (if factored) | Medium-high (on settled fast paths) | **Very high** (§6) |

### Recommended Hybrid Architecture

Combine **Option A's hard physical boundary**, **Option C's relational-first
arithmetic core**, and **Option B's shared-basis port calibration**:

1. **Hard wall between Material Flow and Informational Computation:**
   - *Material* (`TransitionLaw` + `PreservedSum` + `T+`/`T−` + `Flipped` room):
     *waṣābum* (move/absorb), *nasāḫum* (draw), *našûm* (fixed-rate `Convert` on
     a connection), and reservoir-backed amplification.
   - *Informational / Relational* (`RelationLaw` + open `Supports` +
     `Workspace`/`Fork` enactment): Boolean, *kamārum* sum/sub, factored
     multiplication (Egyptian duplation and Babylonian quarter-squares across
     coarse rungs), comparisons, and bidirectional temporal chains.
2. **Never one monolithic `Multiply`:** Keep all four physical multiplications
   explicitly distinct:
   - connection rate (`Flows.Convert`),
   - whole-structure port/frame calibration,
   - factored/clocked scalar product (duplation / quarter-square / rung carry),
   - unfolded bilinear product (*šutakūlum* / 2D pair store).
3. **Uniform-radix coarse rungs (`B = 2, 4, 8, 16`) as the default numeric
   ladder:** Small enough for tiny bit-tables and open supports during
   exploration, and bit-exact packable into native hardware integers and SIMD
   lanes after crystallization (§6).

---

## 6. Crystallization, Compilation, and Hardware Acceleration (CPU / NEON / AVX / GPU / FPGA / Math Libraries)

> **Core rule:** Hardware must never drive the conceptual design
> ([GojoSystemPrompt.md](GojoSystemPrompt.md) §9.6). Every operation must remain
> fully definable and executable in canonical `EncodingC` bit-tables. **Once a
> structure crystallizes, however, a derived compiler backend must be able to
> lower its interior to native CPU, SIMD (ARM NEON / x86 AVX), GPU, FPGA, or
> math-library units without breaking Gojo's semantics.**

This directly extends Consolidation K7 (*native authority with derived compiled
plans*, `FiniteLawPlan.IsCurrent`) and Axiom 12 (*crystallization hardens a
stable tick promise*).

### 6.1 The two-phase lifecycle of an operation

```text
  EXPLORATORY / FLUID PHASE                    CRYSTALLIZED / HOT-PATH PHASE
  ─────────────────────────                    ─────────────────────────────
  • Canonical EncodingC bit-tables             • Sealed local Engine / Port boundary (C7/C8)
  • Open supports, partial inputs              • Epoch-guarded CompiledPlan (IsCurrent)
  • Backward narrowing, probes, forks          • Settled, valid, unaliased, uncontested lanes
  • Learning counts, rewiring topology         • Runs on CPU ALU / NEON / AVX / GPU / FPGA / libm
  • Always the reference oracle                • Any open/broken/overflow lane deopts to canonical
```

`Transport.Save` **always** serializes the canonical Gojo structure—never the
hardware-specific compiled artifact. A restored machine runs identically on any
substrate and can re-compile locally for whichever hardware it lands on.

### 6.2 Two lowering regimes: Bit-Exact vs. Declared Finite-Read

Not every hardware substitution makes the same mathematical promise. Keep these
two regimes strictly separate:

#### Regime 1: Bit-Exact Lowering (zero approximation)

The hardware op produces the **exact same bit pattern** at every exposed port
(including carries, remainders, overflow into `T+`, and evidence planes) as
stepping the canonical `EncodingC` structure:

- **Bit-sliced `Plane` laws → SIMD / FPGA LUTs:**
  - A `Plane` is already a contiguous array of `u64` words across `N` lanes.
  - Any small `TransitionLaw` or `RelationLaw` reduces to a sum-of-products
    Boolean circuit running **128–512 lanes per instruction** on ARM NEON
    (`vandq`, `vorrq`, `veorq`, `vbicq`, `vqtbl1q` for 4-bit LUTs) or x86
    AVX2/AVX-512 (`vpand`, `vpor`, `vpshufb`, `vpternlogq`), or directly into
    **FPGA LUT6 slices** (`Flows.Passage` is literally one LUT6:
    `64'h8048201000040201`).
- **Crystallized uniform-radix carry ladders → Native integer ALU / SIMD `ADD` & `MUL`:**
  - A chain of `k` unary or binary rungs `(r₀, r₁, ..., r_{k-1})` linked by
    `Convert(B, 1)` carries (with verified `PreservedSum([1, B])` weights)
    represents $\sum_{i=0}^{k-1} r_i \cdot B^i$.
  - For power-of-2 radices (`B = 2, 4, 8, 16`), packing/unpacking across `N`
    lanes is pure bit-transpose into native `u16`/`u32`/`u64` arrays, where
    addition, subtraction, fixed scaling, and factored multiplication execute as
    **single widening hardware instructions** (`ADD`, `MUL`, ARM NEON
    `vaddq`/`vmulq`/`vmull`, x86 `mulx`, FPGA `DSP48`), unpacking the low bits
    back to result rungs and routing any high overflow bits directly into `T+`
    (or the carry-out port)!
- **Fixed-rate `Convert(take, give)` & Division with Remainder:**
  - Lowers to exact integer `(q, r) = DivRem(x * give, take)` (or reciprocal
    multiply-shift for constant divisors), preserving exact source remainder `r`
    without losing a single unit.

#### Regime 2: Declared Approximate / Math-Library Lowering (explicit finite-read contract)

For stacked transcendental operations (`sin`/`cos`, `exp`/`log`, `sqrt`), a
discrete symplectic oscillator with coarse stiffness `k` obeys its **discrete
shadow Hamiltonian** (effective frequency $\omega_{\text{eff}} = 2\arcsin(\sqrt{k}/2)$),
which differs slightly from continuous IEEE-754 `Math.Sin(t)`.

Therefore, lowering a crystallized dynamical structure to hardware FPU / vector
`sin`/`cos` / CORDIC / `libm` is legal **only when** the crystallized boundary
carries an explicit **finite-read contract** (Axiom 9 & §8.1):

- declared input/output calibration (quantum, scale, phase origin, capacity);
- declared rounding and clipping rule (outward or nearest, with clipped tails
  visible as `T+`/`T−` or open endpoints);
- declared tolerance bound between the hardware library call and the canonical
  discrete dynamics;
- explicit provenance marking that the read used a lossy/approximate lowering
  rather than claiming exact step-by-step replay of the discrete oscillator.

An exact reader asking for discrete oscillator replay stays in Regime 1 (or
canonical stepping); a crystallized signal/geometry consumer accepting the
finite-read contract gets full hardware `sin`/`cos`/`exp`/`log` speed.

### 6.3 Structural recognition without opcode enums or strings

Gojo forbids storing `"Multiply"` or `OpCode.Sin` tags in the encoding (§9.1,
§9.6). A compiler recognizes hardware-accelerable subgraphs **strictly from
verifiable structure**, in three layers:

1. **Content-addressed law table signatures:** `Law.SameTable` already compares
   laws solely by their bits. A backend caches verified algebraic identities
   keyed by table content hash (`AND = 0x87`, `Passage = 0x8048201000040201`,
   radix-`B` half/full adder, 1D square table, `Odv.Addition(Detection)`).
2. **Mechanical verification of small tables & `Accounting` promises:** Because
   primitive law tables are tiny, a compiler can **prove exhaustively** at
   compile time (rather than trusting a builder name!) that:
   - a carry law preserves `1·fine + B·coarse` (`Accounting.Invariants`);
   - a 3-role table satisfies `r == (a + b) % B` or `r == (a * b) % B`;
   - a 2-role table satisfies `y == x * x` (Babylonian square table) or
     `y == a * x` (fixed multiplier);
   - a two-wiring pair implements the symplectic update `v' = v - k*x; x' = x + v'`.
3. **Sealed subgraph pattern matching + epoch guard (`IsCurrent`):** Over a
   local `Engine` whose boundary is sealed by `Port`s (no foreign direct store
   access), the compiler matches composed idioms (radix-`B` ladder, duplation /
   quarter-square multiplier, lane-parallel broadcast, symplectic oscillator)
   and emits a `CompiledPlan` guarded by a topology/law version token. Any
   structural edit, rewiring, lane rebind, or law change flips `IsCurrent = false`
   and falls back immediately to canonical execution.

### 6.4 Deoptimization guards and preserving Gojo semantics

Hardware instructions assume clean, settled, in-range inputs and silently wrap
on overflow. Every compiled Gojo kernel therefore obeys **two non-negotiable
runtime rules**:

1. **Per-lane fast-path guard mask:** Compute the bitmask of lanes that are:
   `Live & Attended & SettledValidCodes & ~Aliased & ~Contested`.
   - Only guarded lanes run through the compiled SIMD / ALU / math-lib path.
   - Any lane with open `Supports`, malformed unary prefix (`010`), aliased
     ports, or `StepTogether` contention executes on the canonical `EncodingC`
     path and writes its exact `Evidence` planes (`Unread`, `Unadmitted`,
     `Ambiguous`, `Contested`).
2. **No silent hardware wraparound:**
   - Use widening arithmetic (`Math.BigMul`, ARM NEON `vmull`, carry/overflow
     flags) so excess magnitude **never** silently wraps modulo $2^{32}$ or
     $2^{64}$ (Axiom 8, §8.1).
   - Overflow routes into `T+` (or the next carry rung); unmet draw routes into
     `T−`; division by zero / empty domain sets `Unadmitted` / empty support;
     integer division preserves exact source-basis remainder.

### 6.5 Six design guardrails ("don't accidentally block compilation")

While designing canonical operations, following these six light guardrails costs
nothing conceptually and keeps every hardware door wide open:

1. **Seal operation interiors behind thin `Port` boundaries (Closure C7/C8).**
   Never let outside wirings reach directly into internal scratch/carry bits of
   a multiplier or oscillator. Thin ports make the interior legally
   substitutable.
2. **Use uniform, preferably power-of-2 radices (`B = 2, 4, 8, 16`) with
   explicit `Accounting` weights for default numeric ladders.** Explicit
   `PreservedSum([1, B])` weights make place-value structure mechanically
   provable without metadata tags, and power-of-2 radices pack into CPU/NEON
   registers with shifts and masks instead of divisions. (Non-power-of-2 radices
   like 10 or 60 still work canonically and via base-conversion fast paths.)
3. **Keep settled `TransitionLaw` fast paths cleanly separate from open
   `Supports` narrowing.** `EncodingC` already separates plain `Binding` from
   `Binding.Open`; keep this clean so fast-path guards are a single mask check.
4. **Preserve contiguous bit-sliced (`Plane`) Structure-of-Arrays storage.**
   Contiguous `ulong[]` planes are directly vectorizable on ARM NEON
   (`Vector128`), x86 AVX2/AVX-512 (`Vector256`/`Vector512`), and GPU warps. Do
   not scatter per-position state into heap-allocated objects.
5. **Keep material conservation separate from pure informational computation.**
   Pure functional / relational math subgraphs can be fused, reordered,
   vectorized, and offloaded to GPU/DSP freely; conserved material transfers
   respect explicit wave/contention rules.
6. **Keep `CompiledPlan` strictly derived and disposable.** Never put
   compiled blobs, opcode enums, or float tolerances into canonical machine
   meaning; verify every compiled kernel against canonical `EncodingC` stepping
   via differential conformance tests.

### 6.6 Hardware lowering map

| Crystallized Gojo operation | Canonical `EncodingC` form | CPU scalar | ARM NEON / x86 SIMD | GPU | FPGA | Equivalence regime |
| --- | --- | --- | --- | --- | --- | --- |
| Small `TransitionLaw` / `RelationLaw` (e.g. `Passage`, Boolean, `Odv.Addition`) | Bit-table sum of products across `Plane`s | Bitwise `u64` (64 lanes/op) | `Vector128`/`256`/`512` bitwise; `vqtbl1q` / `vpshufb` for 4-bit LUTs | Warp `u32`/`u64` bitwise + constant LUT | Direct LUT3–LUT6 (`Passage` = 1 LUT6) | **Bit-exact** |
| Unary count `Add`/`Remove`/`Compare` | Thermometer prefix planes (`Unary`) | Shift / mask / `popcnt` | `vshl`/`vshr`/`vcntq` | `__popc` / `__ballot_sync` | Thermometer shift registers | **Bit-exact** |
| Multi-rung radix-`B` add/sub | Carry ladder (`Convert(B, 1)` + rung adder) | `ADD`/`SUB` + overflow flag → `T+`/`T−` | `vaddq` / `vsubq` + saturating/widening overflow detect | Coalesced integer kernel | Ripple / carry-select adder | **Bit-exact** |
| Fixed scaling *našûm* (`Convert(take, give)`) | 2-port `TransitionLaw` with `PreservedSum([give, take])` | `DivRem` / multiply-shift + exact remainder | `vmull` + shift + remainder mask | Integer FMA / shift | Hard `DSP48` constant mult | **Bit-exact** |
| Factored multiply `A * B` (duplation, quarter-square, rung ladder) | Sealed multi-rung `Engine` with ports | Widening `BigMul` → result rungs + `T+` overflow | NEON `vmull_u32` / AVX `vpmuludq` across lane batches | Integer/tensor lane kernel | Hard `DSP48` multiplier + carry | **Bit-exact** |
| Whole-structure broadcast (`k × Selected`) | `Wiring` over `Attention` plane | Masked loop over packed lane arrays | Predicated NEON/AVX masked vector multiply | Predicated warp kernel | Masked parallel DSP array | **Bit-exact** |
| Symplectic harmonic oscillator (`sin`/`cos`) & Napier `exp`/`log` | Two-wave coupled two-face `(x, v)` or `(x, t)` around landmark | Discrete integer kick-drift (Regime 1) or `libm` (Regime 2) | NEON integer kick-drift (`vmlaq`) or vector trig/CORDIC | Parallel oscillator batch kernel | Pipelined integer leapfrog or CORDIC IP | **Bit-exact** (discrete) or **Declared Finite-Read** (`libm`) |
| Open `Supports` narrowing | Bitwise `RelationLaw.Narrow` | Bitwise `u64` tuple-AND/OR | SIMD bitwise plane propagation | Parallel plane propagation | LUT/AND-OR fabric | **Bit-exact** |

---

## 7. Concrete phased plan (for branch `ops_neon`)

Each phase is a small, self-contained checkpoint with explicit structural tests,
negative/unresolved tests, and saved byte-size measurements (`Transport.Measure`).

### Phase 0 — Tension & two-faced gap groundwork (prerequisite alignment)
Align with [GojoTension-2026-11.md](GojoTension-2026-11.md) §8 steps 1–2:
- Material `T+` and `T−` unary fields + `Flipped` room-view `Absorb`/`Draw`/`Cancel`
  using existing `Flows.Move` (never netted by default).
- Two-faced `Gap` relation over `(have, want, deficit, excess)`.
- **Exit tests:** overflow waits in `T+` and absorbs when room opens; unmet draw
  waits in `T−`; conservation holds under `Accounting`; deficit and excess stay
  distinct.

### Phase 1 — Boolean operations across bits, lanes, and spans
- Stock 16 two-input 1-byte bidirectional Boolean `RelationLaw`s (building on
  `LawTests.ABooleanOperationIsOneByteReadInEveryDirection`).
- Lane-parallel masked Boolean using `Attention` planes and `Flipped` live
  complement views.
- Spatial Boolean over [`Paths`](../../Gojo.EncodingC/Laws/Paths.cs) /
  [`Sides`](../../Gojo.EncodingC/Laws/Sides.cs): four occupancy regions
  (`neither`, `A only`, `B only`, `both`) inside an explicit scope, yielding
  union, intersection, XOR, difference, and complement while keeping open-end
  residue visible.
- **Negative tests:** `V = 0` (undetectable) is never treated as unselected or
  Boolean false; shared coordinate alone is not intersection; empty support
  (contradiction) stays distinct from Boolean `0`.

### Phase 2 — Multi-rung numerics & the two additions (*waṣābum* vs. *kamārum*)
- Build a small uniform-radix ladder helper (e.g. radix `B = 4` or `B = 8` over
  2–3 unary rungs) chained by `Flows.Convert(B, 1)` carries (`PreservedSum([1, B])`).
- Build both additive surfaces and pin their contrast in tests:
  - **Material *waṣābum* / *nasāḫum*:** in-place conserved transfer across
    rungs with carry-up (`Convert(B, 1)`), borrow-down (`Convert(1, B)`), and
    top-rung overflow landing in `T+` / unmet draw in `T−`.
  - **Relational *kamārum*:** multi-rung ripple-carry `RelationLaw` network over
    open supports `(a_i, b_i, cin_i, sum_i, cout_i)` that adds forward,
    subtracts backward, and propagates partial uncertainty across rungs.

### Phase 3 — The four multiplications & whole-structure scaling
Prototype and measure all four distinct multiplication forms in `EncodingC`
(reporting saved bytes, table bits, steps, and backward invertibility):
1. **Fixed scaling (*našûm* / `TF`):** `Flows.Convert(take, give)` on a
   connection and as a standalone calibrator structure with ports.
2. **Clocked / Egyptian duplation (*a.rá*):** doubling ladder + Boolean
   selection gate driven by multiplier bits + additive accumulator.
3. **Bidirectional Babylonian quarter-squares:** 1D square `RelationLaw`
   (`n <-> n²`) + relational add/sub + exact divide-by-4, tested forward (`3 × 4 -> 12`),
   backward (division: `3 × ? = 12`, and factoring: `? × ? = 12` leaving open
   candidates!), and with open inputs.
4. **Unfolded bilinear product (*šutakūlum*):** 2D pair/quadrant store
   preserving both input factors and opposed-face signs prior to optional fold.
5. **Whole-structure scaling tests** contrasting all three meanings on one small
   multi-position shape:
   - O(1) port/frame calibration,
   - `Attention`-selected lane broadcast (including a test where scaling hits an
     unscaled capacity and produces localized `T+` tension!),
   - structural repetition (*eṣēpum*).

### Phase 4 — Stacked physical dynamics (`sin`/`cos` & Napier `exp`/`log`)
- **Landmark + Momentum Harmonic Oscillator (`sin`/`cos`):**
  - *Rung 0:* pre-numeric `Sides` 4-phase loop around a landmark.
  - *Rung 1:* two-face unary/multi-rung `(x+, x−)` and `(v+, v−)` stepped in
    two ordered waves (kick then drift).
  - *Control test:* prove closed periodic orbit over many cycles under two-wave
    symplectic stepping vs energy growth when run simultaneously (`StepTogether`).
  - *Relational inverse test:* wire `AsRelation()` across a multi-moment plan to
    recover initial phase/side backwards from later observations.
- **Napier kinematic `exp`/`log`:** first-order landmark decay coupled with a
  linear clock carrier, demonstrating multiplication-as-time-addition on a
  nonlinear calibrated carrier.

### Phase 5 — Derived compiled backend prototype (the `NEON` / SIMD / ALU seam)
Build a deliberately small, opt-in `CompiledPlan` layer (kept strictly outside
canonical `Transport` and guarded by `IsCurrent`):
1. **SIMD bit-sliced `Plane` backend:** accelerate `Plane` bitwise operations
   and `RelationLaw` / `TransitionLaw` lane stepping using
   `System.Runtime.Intrinsics` (`Vector128` / ARM NEON, `Vector256` / AVX2,
   portable `Vector<ulong>` fallback).
2. **Recognized radix-`B` carry-ladder ALU fast path:** recognize a sealed
   uniform-radix adder or multiplier strictly from its verified table bits +
   `Accounting` weights (no string tags!), lower settled/unguarded lanes to
   widening native `ADD`/`MUL` with exact `T+` overflow capture, and deopt open,
   broken, aliased, or contested lanes to the canonical plane stepper.
3. **Declared finite-read math-library lowering:** opt-in lowering for a
   crystallized oscillator under an explicit finite-read calibration/tolerance
   contract.
4. **Differential conformance & benchmark suite:**
   - Assert **bit-for-bit identity** between canonical `EncodingC` and Regime 1
     compiled execution across random settled, open, broken, overflow, and
     rewired (stale `IsCurrent`) cases.
   - Assert **declared-bound conformance** for Regime 2 finite-read lowering.
   - Report honest metrics: saved canonical bytes, working memory, lanes/sec
     canonical vs SIMD vs ALU fast path, and fallback rates.

---

## 8. Open questions for the axioms and next design discussion

1. **Default rung radix (`B = 4` vs `B = 8` vs `B = 16` vs mixed/regular):**
   Radix 4 keeps open supports tiny (4 planes/rung) and tables minuscule while
   still packing 2 bits/rung cleanly into NEON/CPU integers; radix 8 or 16
   reduces carry depth at higher support cost; Babylonian radix 60 maximizes
   exact reciprocals (`2, 3, 5`). Should we standardize on radix 4 for open
   relational ladders and allow wider power-of-2 rungs once settled?
2. **Primary relational multiplier: Egyptian duplation vs. Babylonian
   quarter-squares?** Quarter-squares uses one tiny 1D square table and works
   in one relational pass (great for bidirectional division/factoring, though it
   needs `a + b` headroom); duplation needs *no* square table at all and works
   purely from Boolean + doubling + add (though it expands across bit rungs or
   ticks). Phase 3 should measure both directly before choosing a favorite.
3. **Where does a declared finite-read tolerance live without putting floats in
   the encoding?** Under §9.6, the canonical encoding uses no real numbers. A
   finite-read error bound for Regime 2 compilation can be expressed cleanly as
   an integer tick/subdivision ratio (`±1 tick at rung r` via `Convert` weights)
   so even the approximation contract stays integer and substrate-independent!
4. **In-place multiplication with a reservoir:** When material scaling draws
   from an explicit supply reservoir, should an insufficient supply leave a
   partial scaled result + `T−` draw on the reservoir, or hold the entire
   scaling step atomically (as `Flows.Convert` currently does for a single
   quantum)?
5. **Alignment with `Gojo/Core`:** Should Phase 2–4 stay strictly inside
   `Gojo.EncodingC` until proven (per [AGENTS.md](../../AGENTS.md) promotion
   rules), or should we also define a narrow bridge test checking that a
   crystallized `EncodingC` ladder agrees with `Gojo/Core`'s exact `Relation`
   projection/residue contract?

---

## 9. Sources

Checked during preparation of this note (marked `*`) and recalled from reading:

- *Canonical repository sources: [GojoSystemPrompt.md](GojoSystemPrompt.md),
  [GojoTension-2026-11.md](GojoTension-2026-11.md),
  [GojoStructureClosure-2026-11.md](GojoStructureClosure-2026-11.md),
  [GojoEncodingConsolidation-2026-11.md](GojoEncodingConsolidation-2026-11.md),
  [GojoEncodingCCloudHandoff-2026-11-19.md](GojoEncodingCCloudHandoff-2026-11-19.md),
  [GojoCore2Core4ContinuityReview-2026-09.md](GojoCore2Core4ContinuityReview-2026-09.md),
  [Gojo Operation Shape Catalog.md](../axioms/Gojo%20Operation%20Shape%20Catalog.md),
  [`Gojo.EncodingC`](../../Gojo.EncodingC/README.md) (`Odv.cs`, `Sides.cs`,
  `Paths.cs`, `Flows.cs`, `Accounting.cs`, `Wiring.cs`, `Engine.cs`, `Gap.cs`,
  `LawTests.cs`, `SelfRunningTests.cs`, `TwoFaceTests.cs`),
  [`Relation.Operations.cs`](../Core/Relation.Operations.cs),
  [`Equation.cs`](../Equations/Equation.cs), and historical `Core4`, `Core2`,
  and `Numerics` repositories.
- *Author design articles (Google Drive `Articles/` & `math/`): *Boolean
  Arithmetic with Segments*, *Multiplying AI Segments*, *Pin, Accumulate, and
  Merge*, *Sequential Math, Parallel Math*, *Segment Math and Symmetry*,
  *Mathematics Without Infinity*, *Thinking in Directed Segments*, *Core4
  Bootstrap*, *UniversalMath*, and *Core 2 Prompt Axioms*.
- *Høyrup, J. (2002). *Lengths, Widths, Surfaces: A Portrait of Old Babylonian
  Algebra and Its Kin*. Springer; and Høyrup, J. (2017). *Algebra in Cuneiform:
  Introduction to an Old Babylonian Geometrical Technique*. Max Planck Research
  Library (MPRL Textbooks 2).
- *Euclid, *Elements* Books II, V, VI, VII; Becker, O. (1933) and Fowler, D.
  (1987/1999), *The Mathematics of Plato's Academy* (anthyphairesis and
  pre-Eudoxean proportion).
- Rhind Mathematical Papyrus (Egyptian duplation/mediation); *Jiuzhang Suanshu*
  (Chinese counting rods & *fangcheng*); Brahmagupta (628 CE, *Brāhmasphuṭasiddhānta*).
- *Viète, F. (1591). *In artem analyticem isagoge* (*lex homogeneorum*).
- *Descartes, R. (1637). *La Géométrie* (unit segment and fourth-proportional
  multiplication).
- *Werner, J., Wittich, P., Brahe, T., and Bürgi, J. (1580s–1620):
  prosthaphaeresis and *Progress Tabulen*.
- *Napier, J. (1614). *Mirifici Logarithmorum Canonis Descriptio* (kinematic
  two-point definition of logarithms); Oughtred, W. (1622/1632), slide rule.
- Bombelli, R. (1572). *L'Algebra*.
- *Grassmann, H. (1844/1862). *Die Ausdehnungslehre* (extension theory: outer
  product vs scalar ratios of extensive magnitudes).
- *Spencer-Brown, G. (1969). *Laws of Form*; Peirce, C. S., Existential Graphs.
- *Paynter, H. M. (1961). *Analysis and Design of Engineering Systems* (Bond
  Graphs: `TF` vs `MTF` modulated transformers); Broenink, J. F. (1999),
  *Introduction to Physical Systems Modelling with Bond Graphs*.
- *Bonchi, F., Sobociński, P., and Zanasi, F. (2014–2017). *Interacting Hopf
  Algebras*; Piedeleu, R. and Zanasi, F. (2025). *An Introduction to String
  Diagrams for Computer Scientists*. Cambridge University Press.
- Radul, A. and Sussman, G. J. (2009). *The Art of the Propagator*. MIT CSAIL.
- *Hairer, E., Lubich, C., and Wanner, G. (2006). *Geometric Numerical
  Integration* (symplectic Euler / Störmer–Verlet / leapfrog shadow
  Hamiltonians); Minsky, M. (1972), HAKMEM Item 149 (integer circle algorithm).
