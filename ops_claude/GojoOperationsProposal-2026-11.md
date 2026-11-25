# From settling to equations: a proposal

Status: **proposal**, 25 November 2026. Nothing here is built yet. It answers
three questions about the long-term hope for operations:
- whether it is plausible;
- whether it fits the system as it is evolving;
- whether numbers belong now.

It then proposes the next round of code. It draws on this branch's code and
notes, on `ops_sol` and `ops_neon` as compared in
[GojoOperationsComparison-2026-11.md](GojoOperationsComparison-2026-11.md), and
on the older `Gojo/Equations` layer.

## 0. Short answers

**Is it plausible?** Yes, under three conditions:
1. **Learning stays local.** Any function can be learned by settling, but
   only as small tables composed together. The table wall forces this, and
   the composition is the expression a symbolic fit searches for.
2. **Compression is exact first.** A table is recognized as a known relation
   with numeric weights, and what doesn't fit is kept as residue.
   Description length decides when compression pays. Approximate fitting
   comes later, for noise.
3. **Speed comes from changing the carrier.** Compressed equations run
   natively only when the crystallized region holds machine words, not bit
   planes. That needs numbers.

**Does it fit the system?** Yes, closely:
- `LearnedTable` already turns witnessed transitions into a law, and says
  that "a law that fits predicts its own history, so only what it gets wrong
  is worth storing".
- `Operations` already writes equations as relations with numeric weights.
- Residue, debt and the blocked bit already hold the mismatch.
- The older `Gojo/Equations` roadmap is the same path: affine, weighted sum,
  bilinear, state transition, polynomial, candidate fitting. Its rule is
  "first a clear Gojo structure, then an executable helper, and only later a
  compiled/backend representation".

**Numbers now, or stay pre-numeric?** Numbers now, built as structure: rungs
whose carries are balances, not host integers.
- **Pre-numeric made us honest.** Corners, receipts, debt, step order,
  quiet-is-not-met and polarity-as-binding were all found because nothing
  could hide behind arithmetic.
- **It now limits us in three ways:** range (fixtures stop near 12),
  identifiability (on bits, product, minimum and AND are the same table),
  and speed (there is no native form of a bit plane).
- **Rungs keep the honesty.** A carry is a visible balance, and place value
  is a weight that can be read and recognized.

## 1. The hope, stated as a pipeline

```text
observe -> learn small laws -> read hints -> recognize -> equation + residue
        -> verify -> crystallize to words -> compile per read direction
        -> fall back on edit, uncertainty or mismatch
```

1. **Observe and learn.** Settling maps an input or goal to an outcome.
   Witnessed transitions are counted into small learned tables.
2. **Read hints.** The structure already exposes conservation, direction,
   separability, symmetry, rates, reversibility and sensitivity. These are
   calculated from the tables (§3.1).
3. **Recognize.** Find a relation and binding that reproduce the table: a
   balance with integer weights, a product, or a composition. Anything left
   over is residue.
4. **Verify.** The bound relation must have the same table bits as the learned
   law, apart from the declared exceptions.
5. **Crystallize.** A recognized equation that is stable and used often moves
   down the stiffness ladder. Its carriers become words, its kernels native
   instructions, and its exceptions a small table.
6. **Compile and guard.** Each direction of read gets a kernel where one
   exists: adding and subtracting, multiplying and dividing with remainder.
   Parity is checked against the canonical form. Any edit, open support or
   exception falls back to the structure.

## 2. Why tables would converge on ordinary operations

- **The table wall makes learning compositional.** A transition table is
  quadratic in its states, so no learned law can be wide. Wide behaviour must
  come from many small laws sharing roles. That wiring is an expression graph,
  which is exactly the space symbolic regression searches. The wall that
  limits fixtures is also what pushes the structure toward expressions.
- **The world's regularities are few.** Most of what a structure predicts
  will conserve something (moves), accumulate (balances), scale (products),
  repeat (cycles), or be symmetric (polarity, order, translation). These are
  the mechanisms of the operations note (§1.3), so a recognizer needs only a
  few relation families.
- **This branch already shows convergence by construction.** Master's
  counting laws, written as step functions, turned out to be bindings of one
  balance with identical bits. A learned sum table would be recognized the
  same way.

## 3. Recognition: friendly to symbolic regression, but exact first

### 3.1 The hints are already in the structure

Symbolic regression systems spend most of their effort inferring structure
from data. AI Feynman (Udrescu and Tegmark, 2020) tests for symmetry,
separability and dimensional consistency before it fits anything. Gojo
stores much of this directly, or can calculate it exactly from a small table:

| Hint | In Gojo | What it tells a fitter |
| --- | --- | --- |
| Which variable is the target | which roles a law writes (`TransitionLaw.Writes`) | the fit's left-hand side |
| Conservation | `PreservedSum` in the accounting | a balance across time: a move |
| Inputs kept | `Unchanged` in the accounting | a read: information |
| Units and kinds | role shapes; along against across | dimensional analysis |
| Additive separability | no corners, `f(a+1,b+1) − f(a+1,b) − f(a,b+1) + f(a,b) = 0` (`OperationPrimitiveTests`) | `f = g(a) + h(b)` |
| Multiplicative separability | constant corners, or the ratio test | a product |
| Translation symmetry | depends only on a gap: `f(a+1, b+1) = f(a, b)` | a difference, a compare |
| A rate | trades read from the table: `(take, give)` | a conversion's weights |
| Polarity symmetry | the function's class under flipped bindings | the boolean classes |
| Reversibility | each after-state from at most one before-state (Neon's `IsReversible`) | a bijection: shear, rotation |
| Step-order sensitivity | an orbit closes in order and spirals taken together | coupled dynamics |
| Where it stops fitting | blocked bits, residue, debt | the exceptions |
| Sensitivity | the neighbourhood read: the output after a unit change at each site | partial derivatives, and which configuration it is |

### 3.2 Exact fits for small tables

On exhaustive small tables there is no noise to fight, so exact methods come
first.

- **Balances are an integer kernel.** Find small integers `w` with
  `Σ w·v = 0` for every admitted tuple. Checked here on this branch's tables,
  each search recovers the relation exactly, unique up to scale:
  - heap gives `(1, 1, −1)`;
  - the weighted heap gives `(1, 2, −1)`;
  - the half adder gives `(1, 1, −2, −1)`;
  - the kick, over `(v, L, x, v')`, gives `(1, 1, −1, −1)`.

  A product fits no linear balance, which is correct.
- **Folds are balances with a residue role restored.** Shortfall alone fits no
  balance. With the excess added back as a role, it fits `(1, −1, 1, −1)`.
  So when a table does not fit, try adding a free role, a hidden variable.
  That is how `Bind`'s residue is discovered rather than declared.
- **Polynomial relations are a vanishing ideal.** The relations a finite set of
  points satisfies form an ideal. The Buchberger–Möller algorithm (1982)
  computes its generators, and the approximate version (Heldt, Kreuzer,
  Pokutta and Poulisse, 2009) tolerates noise. This fits Gojo better than
  ordinary symbolic regression, which fits an explicit `y = f(x)` in one
  direction. An ideal is implicit and has no direction, like a relation read
  every way.
- **Step maps are sparse regression.** SINDy (Brunton, Proctor and Kutz,
  2016) fits `x' = Σ c·θ(x)` over a library of candidate terms, from
  trajectories. A learned transition table is the step map itself. It keeps
  no history, only before and after, which is exactly what SINDy fits.

### 3.3 Several configurations are evidence, not noise

Your point: repeated addition and stretching give the same products but
suggest different structure. They also leave different traces, so a
structure that watches the process, not only the result, can tell which
mechanism the world has. This is where Gojo can do more than input-output
regression.

| Configuration | Trajectory signature | Neighbourhood signature |
| --- | --- | --- |
| Coasting (repeated addition) | constant first difference: the rate | moving the rate changes every later tick; moving the clock changes the length |
| Stretching (change of unit) | one step, all outputs scale together | moving the scale changes every output in proportion at once |
| Pairing (an area) | first difference `aΔb + bΔa`, with corners where arrivals coincide | the schedule changes the receipts, not the total |
| Lattice (factored) | settles in carry-chain steps | wider operands reuse the same tables; an impossible product goes quiet |
| Dense relation | one narrowing | division and contradiction come free |

`ConfigurationPotentialTests` already shows the same thing geometrically:
- one square, made as lines, a path or a circle;
- three neighbourhoods that barely overlap.

Recognizing which configuration fits a trace means learning mechanism, not
only function. It also predicts what the world will do next under a
perturbation.

### 3.4 Description length decides when to compress

Compress a table when the equation's bits plus its exceptions' bits are fewer
than the table's bits. This is minimum description length (Rissanen, 1978).
Gojo already counts these bits: `Law.BitCount` and `Transport` sizes.
Exceptions stay as residue, a small table of what the equation gets wrong,
exactly as `LearnedTable`'s note anticipates. Stiffness adds a second
criterion: compress only what has stopped changing. A table still being
learned stays a table.

### 3.5 Identifiability needs range

On tiny domains, many formulas give the same table:
- on bits, product, minimum and AND are one table, and the clamped sum,
  maximum and OR another;
- on `0..4`, product and minimum still agree on 10 of 25 pairs.

A fitter cannot tell mechanisms apart until the domain separates them. This is
the strongest argument for numbers now (§6).

## 4. Equations in Gojo form

- **An equation is a relation network with numeric weights.** A balance's
  weights are the coefficients; a product is a monomial; shared roles
  compose them. "a + 2b − c = 0" is a rendering, a read, not a stored formula.
  This keeps the axioms: the numbers are the structure, perturbable and
  discoverable.
- **Both directions come from the same relation.** Each read direction is a
  binding. A compiled form offers a kernel per direction where one exists:
  - add and subtract;
  - multiply, and divide with a remainder and an open zero.

  A factored or compiled form that loses an exact inverse must say so, and
  keep a joint check (Sol's measurement: an impossible product goes quiet in a
  factored network).
- **Uncertainty survives compression.** An open value is a support, a set of
  values. On unary codes a support is a bitmask, and adding two supports is a
  shift-and-OR, which runs natively. Lanes whose supports are open, or whose
  binding is blocked, fall back to the structure.
- **Several configurations of one output form an equivalence class.** E-graphs
  (Willsey et al., 2021) store many equivalent expressions compactly and pick
  one by a cost function. Here, membership is relation equality (`SameTable`).
  The choice is either the cheapest configuration (size and steps) or the one
  whose neighbourhood holds the next target. That makes your "different
  potentials from the same starting point" a data structure with a selection
  rule.

## 5. Native speed: where it can come from

- **Measured now:** master's engine costs about 5 µs per lane per step for a
  three-role law. Neon's compiled plan recognizes kernels and keeps
  bit-for-bit parity, but runs at 0.8 to 1.0 times canonical speed, because
  it still reads and writes bit planes.
- **Where the gain is:** the crystallized region has to hold words. Data
  converts to words at the region's boundary, stays native inside it, and
  converts back only where structure reads it. Inside, an add or multiply on a
  word costs about a nanosecond, less with SIMD. That is three to four orders
  of magnitude per lane-step for compressed arithmetic. This is an estimate,
  to be measured.
- **The guard contract already exists:** Neon's recognition from table bits,
  per-lane parity, deoptimization and invalidation on edit, and
  `FiniteLawPlan` on master. Add Sol's tick-parity check (experiment B17).
- **So compression without numbers cannot deliver speed.** Numbers are the
  carrier of the compressed form.

## 6. Numbers: now, as rungs

### What pre-numeric gave us

Every one of these findings would have been hidden by host arithmetic:
- corners exist only where arrivals coincide;
- receipts differ from plain moves, and recording must not hold material;
- an unpaid product is debt, not a smaller product;
- step order decides whether an orbit closes;
- quiet is not met;
- polarity is a binding;
- fate belongs to roles;
- a rate's stiffness follows from where it sits.

### What it costs now

- **Range:** every fixture stays at 3 × 4 or below.
- **Identifiability** (§3.5).
- **Speed:** there is no native form of a plane.
- **Storage:** a unary value takes as many planes as its capacity.

### Three tiers

| Tier | Carrier | For | Canonical? |
| --- | --- | --- | --- |
| Unary | thermometer planes | meaning, learning, narrowing, small counts | yes |
| Rungs | ladders of small digits joined by carry balances | range, structural numbers | yes |
| Words | machine integers inside a crystallized region | speed | no: a guarded cache |

**Rungs keep the honesty.** A carry is a balance with the radix as its weight:
`a + b + c_in = r·c_out + s`. The bit rung's full adder is the case `r = 2`,
already tested. A carry between rungs is a conserving move, `Convert(1, r)`.
A number is then a ladder of counts linked by visible balances. Place value is
a weight a recognizer can read. Overflow is residue or pressure. Neon's
`Rungs` and ladder are the furthest along; rebuilt on `Bind`, a ladder becomes
recognizable as one balance family.

## 7. What to take from each branch now

| From | Take | Decline, or change |
| --- | --- | --- |
| ops_sol | the evidence conformance suite: pair formation, why a hold happened, joint against marginal reads, scoped commits, tick parity (experiments B14 to B18); the receipt correction (done) | nothing; its circuits stay tests |
| ops_neon | rungs at any radix, the two divisions, the open gap, reversibility with `Inverse`, the saved step kind with a transport version bit, compile guards with recognition from bits | 16 named gates as the vocabulary, since a gate is a mask and a binding; signed values as a fixed offset, where a landmark count is perturbable; J in the kernel before the orientation note decides |
| this branch | `Bind` as the core, residue and the blocked bit, mechanisms against bindings, coordinates calculated from laws, configuration neighbourhoods | build cost is 3 to 10 times slower; it is one-off and can be optimized |

## 8. Proposal for the next round of code

### Round A: recognition and rungs

1. **Recognize a law as a bound relation.** This is `Bind`'s inverse: given a
   table, find a relation and binding that reproduce it.
   - **Input:** a `TransitionLaw` or crystallized `LearnedTable`, and a bound
     `R` on weights.
   - **Read** the hints first: written roles, conserved roles, corners and
     trades.
   - **Search** for a balance with an integer kernel `|w| ≤ R` over the read
     roles' values before the step and the written roles' values after it.
     Allow at most one added free role as residue. Test products as
     monomials.
   - **Verify** with `SameTable` against `Bind` of the result.
   - **Return numbers,** never a kind: the weights, the read and write
     arrays, the residue role's capacity, the pairs that differ (exceptions),
     and the bits saved.
   - **Tests:**
     - It recovers `Counts.Sum`, `Tally`, `Shortfall` (with its residue),
       `Accumulate` (in place), the kick and the drift, and the half and full
       adders.
     - A `LearnedTable` crystallized from watching the coasting booking is
       recognized as the booking balance.
     - A random functional table is not.
     - A table differing in one pair compresses to an equation plus one
       exception, with the description-length gain stated.
2. **Rungs on `Bind`.**
   - **Laws:** the radix-`r` full adder as a balance, `(1, 1, 1, −r, −1)`;
     ripple ladders; a conserving carry between rungs; and a digit lattice
     product.
   - **Tests at radix 2, 4 and 10:** parity with the wide relation where that
     still fits; then a 3-digit base-10 sum and product (0..999) that the wide
     relation cannot hold; carry delay as step order; and overflow as visible
     residue.
3. **Signatures as calculated reads:**
   - corners and the ratio test for separability;
   - the gap test for translation symmetry;
   - reversibility;
   - trajectory first and second differences, telling coasting from
     stretching from pairing.

### Round B: crystallize and compile

- Crystallize a recognized balance or product over rungs into words, with a
  kernel per read direction.
- Carry over Neon's guards, and run B16 and B17 as gates.
- Measure the speedup honestly against the canonical engine.

### Round C: configurations and trajectories

- Equivalence classes of configurations, with cost and neighbourhood reads.
- Step-map regression over a library of balances and products, from learned
  tables.

## 9. Risks, and where the axioms need a word

- **Search can blow up.** Keep fits exact and small, and prune with hints. A
  failed fit is an answer: the table stays a table.
- **Noise is not an exception.** `LearnedTable`'s thresholds separate the two
  for now. Approximate ideals come later.
- **Recognizers must not become enums.** Recognition returns numbers (weights,
  exponents, radices), never a kernel kind. This is the dispatch-in-disguise
  test applied to compression.
- **Canonical and native can drift.** Parity tests are the gate, and words are
  never canonical.
- **Compression can lose an inverse.** It must declare the loss and keep a
  joint check.
- **Axiom gap:** "use the Gojo numeric system as the canonical
  representation", and "prefer calculated values to stored values", sit
  uneasily with native words. Proposal: words are a guarded cache of a
  calculation, below the canonical tiers, and saving writes only the
  canonical structure (Neon's rule).
- **Axiom gap:** the axioms do not yet say when a learned table should become
  an equation. Proposal: when description length says it pays and the
  stiffness ladder says it has stopped changing.

## Sources

From this repository:
- `Gojo.EncodingC/Learning/LearnedTable.cs`
- `Gojo/Equations/README.md`
- this branch's `Operations` and tests
- `ops_sol` at `29df5335`, and `ops_neon` at `6f655fc2`

From memory, to check before they are relied on:
- Schmidt and Lipson (2009), "Distilling free-form natural laws from
  experimental data"
- Udrescu and Tegmark (2020), "AI Feynman"
- Brunton, Proctor and Kutz (2016), SINDy
- Möller and Buchberger (1982), on the vanishing ideal of points
- Heldt, Kreuzer, Pokutta and Poulisse (2009), approximate vanishing ideals
- Willsey et al. (2021), egg: e-graphs and equality saturation
- Rissanen (1978), minimum description length
- Edelman and Gally (2001), degeneracy

The integer-kernel results in §3.2 and the aliasing counts in §3.5 were
computed for this note.
