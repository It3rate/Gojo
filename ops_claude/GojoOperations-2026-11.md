# Operations in Gojo: where adding, multiplying and boolean reads live

Status: **exploratory discussion**, 25 November 2026. Nothing here is built.

This note gathers what the repository already holds about operations: the
canonical prompt, the kernel, EncodingC, the Core 2 and Core4 notes, and the
articles in the Drive folder. It adds a survey of how other cultures and
machines have related magnitudes, then answers the questions that were asked
and offers three approaches to discuss before any planning or code.

Statuses follow the canonical prompt (§1): *implemented*, *documented
direction*, *exploratory* and *superseded*. Everything this note proposes is
exploratory. Small numeric claims are checked by the script in
[Appendix A](#appendix-a-numeric-checks).

## Contents

0. [The short version](#0-the-short-version)
1. [What was asked](#1-what-was-asked)
2. [What Gojo already holds about operations](#2-what-gojo-already-holds-about-operations)
3. [Pushback: operations and numerics are one design](#3-pushback-operations-and-numerics-are-one-design)
4. [How people and machines have related magnitudes](#4-how-people-and-machines-have-related-magnitudes)
5. [Distinctions the survey forces](#5-distinctions-the-survey-forces)
6. [The questions, answered](#6-the-questions-answered)
7. [Three approaches](#7-three-approaches)
8. [Conflicts and gaps in the axioms](#8-conflicts-and-gaps-in-the-axioms)
9. [Experiments that would decide](#9-experiments-that-would-decide)
10. [Questions back](#10-questions-back)
- [Appendix A: numeric checks](#appendix-a-numeric-checks)
- [Appendix B: sources](#appendix-b-sources)

## 0. The short version

1. **Pushback on the order.** The *shape* of an operation can be fixed before
   numerics: which roles are read, which are written, what is conserved, what
   may cross a port and how the result folds. Multiplication cannot be
   finished first, because multiplication is the relation between rungs: a
   count read in another count's unit. Core4 already wrote this down
   ("Multiplication, Division, And Addition As Tick Attachments ... the same
   mechanism as resolution"). Proposal: define every operation at rung 0
   (presence, unary count, ODV word), with one lift rule between rungs. The
   lift is the carry, a conserving rate whose weights are place values, and
   the read of a coarser rung. Numerics then *is* the lift, not a later layer.
2. **The first cut is what is being combined**, not "boolean, add,
   multiply". Amounts merge. A rate scales an amount. Rates compose. Positions
   differ, translate and average. Presences co-occur. Intensive quantities
   (temperatures, densities, prices) never add.
3. **Constants live on connections; variables meet at junctions.** An
   operation with one live input (scale or shift by a constant) is a
   calibration on a relation: Axiom 8's projection, EncodingC's `Convert`. An
   operation with two live inputs is a junction, a law over three or more roles
   inside one local structure. Fix one input of a junction and it becomes a
   connection.
4. **Conservation decides what can cross a port.** Sums with positive weights
   (moves, branches, merges, constant-rate conversions, carries) can be taken
   through a port. A product of two live counts has no preserved sum, so it
   crosses only as a read. Any material it has comes from one of three places:
   a refinement (the same stuff counted in a finer unit), a supply drained at a
   rate, or new structure such as an area.
5. **Direction is three reads, not a flag.** The *relation* is symmetric and
   reads in every direction, so division and the open zero fall out of it. The
   *process* is directional per step: a rate is read, an amount moves, and a
   later arrival sweeps along what is already there. The *fold* is the
   collapse that is read. Which roles a law writes already shows direction in
   EncodingC.
6. **The path is part of the product.** The total `a·b` does not depend on the
   path. Its split into "A swept along B" and "B swept along A" records the
   order of arrivals: `Δ(ab) = aΔb + bΔa + ΔaΔb`. The two-triangle picture in
   the Drive articles is the fine limit. At unit resolution the corners are
   real (they are the Pythagorean gnomons); in a fine read they vanish, as in
   dual numbers.
7. **Boolean operations are the bit rung of the same arithmetic.** AND is
   co-presence, which is both the product and the carry of a one-bit sum. OR is
   a clamped one-bit sum and XOR a wrapped one: Axiom 7's two boundaries. NOT
   is room within a scope. The 16 functions are routings of the four
   co-presence quadrants; kept with the unselected quadrants as residue, they
   conserve. On unary counts, AND of the planes is the minimum and OR the
   maximum, not the product.
8. **Scaling a whole structure is a frame edit.** Retarget its unit landmark
   (one `TickRef`) and every read follows while internal ratios stay put;
   translation retargets the origin. Structure × structure is a sweep, every
   part with every part. A structure can also be the multiplier: its count
   gates repetition, as in a frieze.
9. **Sine and cosine belong to one family.** Two accumulators each feed a
   coarsened copy of the other into themselves, with a feedback sign `m`.
   `m = +1` restores (sine and cosine), `m = 0` coasts (rate × time is
   multiplication) and `m = −1` reinforces (exponential and logarithm). CORDIC
   has used exactly this parameter since 1971. At rung 0, the oscillator's
   phase is a two-bit Gray cycle of `Sides` events, turned back and crossed:
   Axiom 10's quarter-turn action, built from two alternating flips.
10. **Update order carries meaning.** Stepped in declared order, the integer
    oscillator closes its orbit exactly (Minsky); stepped together, it spirals
    out. EncodingC does not save the step kind with a machine. For oscillators
    it has to be structure.
11. **Solving by lowering tension is ancient.** Egyptian false position,
    Chinese excess and deficit, Babylonian iterated roots and the steelyard all
    settle rather than solve.
12. **Three approaches:** A, junction tables (the scribe); B, coupled flows
    (the machine); C, frames and rungs (the geometer). Recommendation: C for
    what multiplication *means*, B for how it *runs*, and A as the crystallized
    form learning produces, built first at rung 0 in EncodingC.

## 1. What was asked

- Operations should be settled before numerics, which are mostly partitions
  on carriers and rungs of carriers. Push back if that order is wrong.
- Boolean, additive and multiplicative operations come first; sine, cosine and
  other stacked operations later. All should be physically intuitive. Sine and
  cosine, for example, as something accelerating toward a landmark, passing
  it, and being pulled back from the other side.
- Do these operations live on connections, or are they structure of their own?
- What would it mean for a larger structure to be the target of a
  multiplication?
- Should `A * B` modify `B`, always create a new result, or both, with the
  direction encoded?
- Look at Core4 and the earlier iterations, the Drive articles, and outside
  sources, including old ways of multiplying, Renaissance, Greek and especially
  Babylonian mathematics. Suggest two or three approaches.

"Here" is read as EncodingC, the current pre-numeric substrate, with the
kernel as the numeric reference. Where the two differ, both are described.

## 2. What Gojo already holds about operations

### 2.1 The canonical prompt (documented direction; parts implemented in the kernel)

- **Axiom 7, local accounting.** `P(C,k) -> P(C,0)` wraps (modulo) and
  `P(C,k) -> P(C,k)` clamps. Crossed excess becomes tension that a route may
  absorb: `P(C0,k) -> P(C1,0)` groups source tension by `k` and routes it
  `k:1`. That route is a carry, and a carry is division by `k` with a
  remainder. `P(C0,k) -> P(C1,m)` is a ratio projection.
- **Axiom 8, projection.** `S(C0,a,b) -> S(C1,c,d)` reads `x` as
  `c + (x−a)(d−c)/(b−a)`, with source residue. With `a = c = 0` and `b = 1`
  this is multiplication by `d`, and it is a read, not a mutation.
- **Axiom 10, unit algebra.** `u^2 = u -> u`. Compatible addition may
  collapse; incompatible addition stays as several participations. Products
  preserve repeated and orthogonal operands, and opposed-flow terms stay
  visible until a fold. Complex arithmetic is a selected interpretation with
  the cycle `PF -> PI -> NF -> NI -> PF`. Dimensional commutativity belongs to
  a local read, and authored order is kept. Quotients keep `n = q·d + r`.
- **Axiom 11.** A segment pushed orthogonal is a sheet or rectangle
  possibility: the product of measures.
- **§6.** Powers are repeated product participation, not a `Power` tag.
  Recurrence is output fed back into a later input.
- **§8.1.** Equations are relation-shaped reads, never a canonical symbolic
  tree.
- **§8.2.** Boolean-style operations are low-resolution reads over occupied
  participation through an explicit scope. A bare boundary is not an operand,
  and a shared coordinate is not an intersection.
- **§9.6.** No real numbers and no math library: counting, comparing and
  looking up a table are enough. Primitives are coarse, and finer resolution
  comes from ladders of coarse rungs.
- **§9.7.** One is presence. "A product is presence of both. `1 · 1 = 1` and
  `x · 0 = 0`."

### 2.2 The kernel (implemented)

- **The output's shape chooses the operation.** In `Relation.Operations.cs`, an
  operation port `(portA -> portB) -> output` projects each operand through its
  own exposed port. A point output then sums and a frame output multiplies.
  Factors with several components expand into a product lattice instead of
  folding early.
- **Equations** (`Gojo/Equations`): affine frames; weighted sums and products
  by the same rule; sequences, fanout, polynomials and powers
  (`EquationPower`, `EquationPolynomial`); resolution search.
- **Structures:** `RepeatSurface` (controller, transition, feedback,
  timeline); `OperationClaim` and `OperationFollow` (a mismatch, and a response
  route); `ComplexFrame` with contour and powers; `UnitQuotient`,
  `QuantityQuotient`, `ResolutionLadder`, `BoundedQuantity.Operations` and
  `UnitShape.Subdivision`.
- **The Operation Shape Catalog** (`Gojo/axioms/Gojo Operation Shape
  Catalog.md`) lists the affordances. `P(C0,1) -> P(C1,0)` is a one-for-one
  accumulator and `P(C0,k) -> P(C1,0)` a base or counter carry. There are ratio
  projections, and `(S0 -> S1.P1)^2` is square-like repeated basis scaling.
  Product terms with opposed faces stay as four contributions (`AC`, `AD`,
  `BC`, `BD`) until a fold.

### 2.3 EncodingC (implemented; the project is exploratory)

- **An operation is a table.** A relation law admits tuples; a transition law
  maps a whole state to its successor. AND, OR and XOR as relations over
  `(a, b, out)` are the bytes `87`, `E1` and `69`. Adding two unary counts of
  0 to 3 into a sum of 0 to 6 is one relation of 112 bits with 16 admitted
  tuples, and the same table subtracts
  (`AddingTwoCountsIsOneRelationThatAlsoSubtracts`).
- **Direction is visible in the table.** A role the law never changes is read;
  a role it changes is written. Writing through a port is allowed only when a
  preserved sum with positive weights covers the role, that is, as a move.
- **Rates are laws.** `Flows.Convert(take, give, …)` preserves
  `give·source + take·target`, and `Move` is the 1:1 case. A carry is
  `Convert(3, 1, …)` from a fine count to a coarse one, and the weights of its
  preserved sum are the place values (`WhatDoesNotFitIsCarriedUpARung`).
- **A gate multiplies a flow by a bit.** The passage law reduces to
  `S' = S & (~C | T)` and `T' = T | (S & C)`.
- **Where, and which way.** ODV words hold V, D and O. The three orientation
  moves, Reverse, FaceFlip and Reorient, each undo themselves, and each is the
  composition of the other two. `Sides` says where something is against a
  landmark, with events such as toward, away, crossed and turned back.
- **Two kinds of step.** A step either runs in declared order or is taken
  together (`StepTogether`), with every row reading the state before the step.
  The README notes that the kind of step is chosen by whoever steps the machine
  and is not saved with it.
- **Tension (a proposal).** `T+` and `T−` as unary fields, with absorb, draw
  and cancel as one existing law, `Flows.Move`, into a value or its room view
  ([tension note](../Gojo/docs/GojoTension-2026-11.md)).

### 2.4 Core 2 and Core4 (superseded design memory)

These are historical, but they already tried most of the ideas below, and
their conclusions are worth keeping in view.

- **Core4, append and smear.** Unpartitioned with unpartitioned appends
  magnitude, which reads as addition. Partitioned with partitioned smears the
  partner over the host's partitions, which reads as multiplication. The
  operation came from the operands' structure, not from a tag.
- **Core4, preserve before collapse.** When two composite branches meet, four
  contributions are kept: aligned `AA` and `BB`, orthogonal `AB` and `BA`. The
  note calls this "more primitive than a complex-number or directed-segment
  formula."
- **Core4, operations are carrier layers.** There is one transform per layer.
  Multiplication, division, addition, resolution, approximation, inversion,
  filtering, wrapping, clamping and oscillation differ only by the value in the
  transform slot, the tension the layer exposes, and the routes attached to
  it. Under "Multiplication, Division, And Addition As Tick Attachments", zero
  attachment is additive, positive is expansive grouping, negative is
  contractive partitioning, and repeated positive is power-like: "the same
  mechanism as resolution."
- **Core4, oscillation from routed boundaries.** A velocity carrier is
  incremented or decremented by which side of the position is active, and
  "distance or later-layer magnitude affects how strongly velocity changes".
  This is the sine example from the request.
- **Core4, boolean filters** as pass and block routes over support, with `not`
  only inside an explicit scope.
- **The Core 2 axioms** (the "Axioms" document in the Drive folder):
  - F10: in `x * t`, `x` is state and `t` is transform data, so
    multiplication is asymmetric. F11: `S_a ∘ S_b = S_(ab)`.
  - P5: `A * B @ P` is a pinning event with a recessive host `A`, a dominant
    applied `B` and a host-relative position `P`.
  - P22 to P24: opposed resolution on one carrier yields a directed segment,
    same-direction resolution yields reinforcement, and orthogonal resolution
    yields a corner or an area.
  - R1 to R21: a repetition has a carrier, an index and a continuation law.
    Inverse continuation, such as roots and logarithms, is generally not unique
    and keeps a candidate set.
  - M4: signed digits are allowed on a resolution ladder.
  - Part IX: boolean results on directed segments need a frame; complement is
    relative to the frame; truth is evaluated on constant partitions; results
    are co-present families of pieces.

### 2.5 The Drive articles (the author's own model)

- **Pin, Accumulate, and Merge.** An operation has stages. *Pinning* selects
  operands and joins them minimally into one frame. *Merging* combines them.
  *Folding* collapses the result back to the original dimensions. Addition
  merges each portion with a mapped portion (1:1). Multiplication maps each
  portion with the whole other operand (1:ALL); done portion by portion it
  reads as repeated addition, and done continuously as stretching. Three
  glasses, three cases and three gallons of wine have distinct measurement
  physics. "Joining versus merging is a very key idea in mathematics." Two
  directed segments multiplied give four quadrants with two diagonals: one
  reads comparison (dot-like), the other combined strength (cross-like). And
  "2·3 = 7 tells us where the tension is."
- **Multiplying AI Segments.** The squeegee: dragging it down the window
  merges the width into the height. A partial state is a segment from what is
  done to the goal. Multiplication is "the full merging of two values, A into
  B, and B into A". One-dimensional multiplication always loses the operands.
  The natural shape is two triangles: `4 × 3 = ½·3·4 + ½·4·3`.
- **Boolean Arithmetic with Segments.** Two segments and a zero give five
  points of interest and four areas: none, both, A only and B only. The 16
  boolean functions select any combination of them. Comparison and addition go
  position to position (relative); ratios and multiplication go from zero
  (absolute). False is not nothing. A nonlinear number line is an ordinary line
  with an operation applied. Two measures can merge by yes or no, one for one,
  scaled, or repeated.
- **Sequential Math, Parallel Math.** "Down 1, across 1" is an L in sequence
  and a diagonal in parallel, and "there is nothing in math that distinguishes
  an 'L' from a '\\'". `3×3×3` in sequence runs three speeds one after another;
  `3^3` in parallel runs them at once, each rung tracking the one before.
- **Complexity Spaces** (the "Numbers" document). Two-element operations are
  grouping, appending, scaling and boolean. In scaling, the second value acts
  "more as a director than 'contributor of stuff'". Boolean operations are
  one-shot. Stretching to twice the size in slow motion has "a 'larger'
  pressure at each moment until the equation is resolved. The 'times two' is
  slowly used up as it approaches equilibrium." Multiplying values of the same
  type happens over time; values of different types give an area.
- **Mathematics Without Infinity.** Every number has a unit, a range, ticks
  and a repetition limit. A product of two below-resolution terms is pruned, as
  `ε² = 0` is in dual numbers.
- **Segment Math and Symmetry** and **The Mathematical Interpretation of
  Symmetric Numbers.** Four units (identity, negate value, negate line, and
  both) are the four symmetry actions. The 16 four-bit patterns filter to seven
  frieze patterns. A three-dot pattern is a contradiction that forces a
  translation: a new dimension. The unit is a landmark that positive values
  move toward.

### 2.6 What these say together

Across five iterations the same moves keep returning:

- **The operation comes from shape, not a tag:** Core4's append and smear, and
  the kernel's output grade.
- **Product terms stay unfolded until a read folds them:** Core4's
  `AA/AB/BA/BB`, the catalog's `AC/AD/BC/BD`, and the articles' four quadrants.
- **Multiplication and resolution are one mechanism:** Core4's tick
  attachments and Core 2's ladders.
- **Oscillation is routed response at a landmark:** Core4 §20 and the request.
- **Boolean reads are routing over partitions in a scope:** Core4's filters,
  Core 2 Part IX, and §8.2.

What has never been settled is the dynamics: where product material comes
from, which operand is read and which is moved, and how the path of an
operation is kept.

## 3. Pushback: operations and numerics are one design

This is partly agreed and partly not.

**Agreed:** the *shape* of an operation can be fixed now, before numerics:
which roles are read and which written, what is conserved, what may cross a
port, how the result folds, and where tension goes. These are rung-0
questions, and presence bits, unary counts and ODV words can answer them.

**Not agreed:** multiplication cannot be finished first, because
multiplication is the relation between rungs. `3 × 4` is "three ticks of a
carrier whose tick is four." The partitions and rungs named in the request are
where it lives:

- Core4 reached the same conclusion and wrote it down: "Multiplication,
  Division, And Addition As Tick Attachments ... This is the same mechanism as
  resolution."
- The carry that lets addition work at any resolution is itself a
  multiplication by a constant. EncodingC's carry is `Convert(3, 1)`, and its
  weights are place values.
- Descartes needed a unit segment before the product of two lines could be a
  line (§4.6). Without the unit it is a rectangle (Euclid; Axiom 11).
- Davydov's curriculum teaches multiplication through measuring with an
  intermediate unit (§4.7).

**Proposal.** Define every operation at rung 0 and give it one lift rule.
Rung 0 is presence, a unary count and an ODV word. The lift is the carry
between rungs (a conserving `Convert` whose weights are place values) and the
read of a coarser rung (division by the base, with the finer rung as residue).
Numerics is then not a later layer; it is the lift. This keeps the prompt's §9.6 ("finer
resolution is built from ladders of coarse rungs"), and it lets operations be
tested at rung 0 now.

**A second pushback: the first cut is what is being combined**, not boolean,
add and multiply (§5.1). The request's own "relative for addition, absolute
for multiplication" is exactly Stevens's interval and ratio scales. "Three
glasses, three cases, three gallons" is Vergnaud's distinction between a count,
a rate and a measure. Settle the kinds of participant first, and the
operations follow from them.

## 4. How people and machines have related magnitudes

Each subsection says what was done, then what it teaches Gojo.

### 4.1 Egypt: doubling, and guessing then scaling

*What was done.*
- **Multiplying by doubling and adding.** For `41 × 59`, double 59 to get
  118, 236, 472, 944 and 1888. Since `41 = 32 + 8 + 1`, add
  `1888 + 472 + 59 = 2419`.
- **False position** for the linear "aha" problems (Rhind 24 to 29). Problem
  24 asks for a heap that, with a seventh of itself, makes 19. Guess 7: seven
  and its seventh make 8. Scale the guess by 19/8 to get 16⅝.

*What it teaches Gojo.*
- **Doubling gives the operands different roles.** The multiplicand is
  material copied up a ladder of doublings, a ladder of rungs with factor 2.
  The multiplier is a pattern of gates that selects which rungs to add.
  Multiplication is gated accumulation over a binary ladder, and division runs
  the same ladder the other way.
- **False position is solving by lowering tension.** Choose a convenient
  guess, read its error against the goal, and correct in proportion. This is
  the prompt's §5.4 "set the prediction to the goal and let the flow, with its error,
  change … until the difference is small", three and a half thousand years
  early.

### 4.2 Mesopotamia: relative magnitude, tables, and moving area

*What was done.*
- **Floating place value.** Sexagesimal digits carried no marker for the
  absolute power of sixty; context supplied the scale. A number was a ratio to
  an unstated unit until something calibrated it.
- **Division as multiplication by a reciprocal,** read from standard tables.
  The standard table lists regular numbers, those with no prime factor other
  than 2, 3 and 5, whose reciprocals terminate in base sixty. Irregular
  divisors gave approximations. There were multiplication tables as well.
- **Cut-and-paste geometry** (Høyrup's reading of the Old Babylonian texts).
  Quadratic problems were solved by imagining rectangles and squares being
  cut, moved and completed. Each step is visibly correct, "naive" in Høyrup's
  sense, and the basic tools were cut-and-paste, quadratic completion and
  change of scale.
- **Roots to remarkable precision.** YBC 7289 gives √2 as 1;24,51,10, about
  1.41421296. How it was found is not known. An iterative procedure appears on
  another tablet, and the averaging method later named after Heron is a
  plausible route. Tables of squares existed; the common claim that they were
  used for quarter-square multiplication is speculative.

*What it teaches Gojo.*
- **Relative first, calibrated later.** A floating number is an uncalibrated
  connection (§4.6 of the prompt): it claims a ratio and no scale.
- **A table read backward is division.** A reciprocal table is a crystallized
  relation, used in whichever direction is needed, and EncodingC's relations
  already work this way. Regularity is a property of the rung base: what base
  sixty represents exactly, and what leaves residue.
- **Algebra as conserving rearrangement.** Cut-and-paste moves area without
  creating or destroying it, and "completing the square" adds the missing
  corner, the same `ΔaΔb` term as in §5.6. Solving by moving material inside a
  fixed total is settlement, not symbolic manipulation.

### 4.3 Greece: number, magnitude and ratio

*What was done.*
- **Multiplication is defined asymmetrically.** Euclid VII, definition 15: "A
  number is said to multiply a number when the latter is added as many times as
  there are units in the former." That the order does not matter is a theorem
  (VII.16), not part of the definition.
- **Ratio without division.** Euclid V, definition 5, attributed to Eudoxus:
  magnitudes are in the same ratio when, for any equimultiples taken, the
  multiples of the first and third alike exceed, are alike equal to, or alike
  fall short of the multiples of the second and fourth. Equality of ratio is
  defined by repetition and comparison alone.
- **Products of magnitudes raise dimension.** The product of two lines is a
  rectangle (the geometric algebra of Book II), and nothing brings it back to a
  line. Ratios compose through a shared middle term: equiangular
  parallelograms are in the ratio compounded of the ratios of their sides
  (VI.23), and `A:B` compounded with `B:C` is `A:C`.
- **The gnomon** (II, definition 2) is the L-shaped region that grows a figure
  into a similar, larger one. Adding the odd numbers 1, 3, 5, … builds the
  squares.
- **The lever.** Archimedes (*On the Equilibrium of Planes* I.6–7):
  magnitudes balance at distances reciprocally proportional to their weights.
  A steelyard reads a weight by sliding its counterpoise until the beam is
  level, finding a proportion by settling.

*What it teaches Gojo.*
- **Eudoxus is Gojo-native.** Ratio defined by counting repetitions and
  comparing them, with no division and no number, is exactly the primitive set
  of the prompt's §9.6 and of EncodingC: unary counts and mask tests.
- **Asymmetric roles are old and legitimate.** The definition is directional;
  the symmetry of the total is something proved.
- **Without a unit, a product is new structure.** Two extents make an area,
  not a longer extent.
- **Composition through a shared middle is relation composition,** the
  kernel's `A -> B -> C`.
- **Equilibrium computes.** The lever finds a ratio by lowering a torque to
  zero.

### 4.4 China: two colours of rod, and excess and deficit

*What was done.*
- **Signed rods.** On the counting board, red rods were positive and black
  ones negative; other conventions are also reported. The eighth chapter of the
  *Nine Chapters*, *fangcheng*, solves systems by column operations on the
  board, with sign rules in the text: in the subtraction step, "subtract
  same-signed numbers, add different-signed numbers".
- **Excess and deficit** (*ying buzu*, chapter 7), the method later called
  double false position. In problem 1, people buy an item together: if each
  pays 8 there are 3 left over, and if each pays 7 they are 4 short. The rule
  combines the two trials crosswise: people `= (3 + 4)/(8 − 7) = 7`, price
  `= (8·4 + 7·3)/(8 − 7) = 53`.

*What it teaches Gojo.*
- **Two faces kept apart.** Red and black rods on one board are the tension
  note's `T+` and `T−`: never netted by default, and combined only by a stated
  rule.
- **The answer is a weighted crossing of an overshoot and an undershoot.**
  Excess and deficit settles by two-faced tension: one trial overshoots,
  another undershoots, and the true value lies where the residues cancel. That
  is interpolation between residues, the same shape as Axiom 8's projection
  with residue.
- **A board is a store, and a procedure is a law on it.** The board does not
  know what the rods mean.

### 4.5 India: place value, debts, and sine by second differences

*What was done.*
- **Signed quantities and zero.** Brahmagupta (628) gave rules for fortunes,
  debts and zero.
- **Sine by second differences.** Āryabhaṭa (499) tabulated 24 sine
  differences at intervals of 3°45′, with radius 3438, starting from 225. The
  rule in Gaṇitapāda 12, as Nīlakaṇṭha and modern commentators read it, makes
  each second difference proportional to minus the sine already reached: each
  difference is the one before it, diminished by the sine so far divided by
  225.
- **Lattice multiplication.** A close relative, *kapāṭa-sandhi*, appears in a
  commentary on the twelfth-century *Līlāvatī*. The method travelled through
  the Arabic world (*shabakh*) to Italy (*gelosia*: the Treviso arithmetic of
  1478, Pacioli in 1494), and Napier's bones (1617) rest on the same
  principle.

*What it teaches Gojo.*
- **This is the request's sine.** A quantity whose change of change is
  proportional to minus itself is something pulled toward a landmark in
  proportion to its distance. Āryabhaṭa computed sines with addition and one
  coupling rate.
- **The coupling is a calibration.** Run literally, a coupling of 1/225 is
  slightly too strong, and the table peaks at 3375 instead of 3438. With the
  exact discrete coupling, `2 − 2cos 3°45′ ≈ 1/233.5`, it peaks at 3440
  (Appendix A). The rate between the two accumulators is the numeric question,
  and it is a question about rungs.
- **A lattice is the product of two ladders.** Every digit of one number meets
  every digit of the other in a cell, an orthogonal grid: the product of
  measures. Each cell's place is the sum of its row's place and its column's,
  and the diagonals of equal place are summed with carries. Long
  multiplication multiplies digits while it adds rung indices.

### 4.6 Early modern Europe: multiplication as motion, angle and projection

*What was done.*
- **Prosthaphaeresis.** Before logarithms, astronomers multiplied with
  `cos a · cos b = ½[cos(a − b) + cos(a + b)]`: look up two angles, add and
  subtract them, and look up the results. The identity is credited to Ibn Yunus
  and to Werner (about 1510). Wittich brought the method to Tycho Brahe's
  observatory around 1580, Ursus published it in 1588, and Bürgi proved and
  used the identities.
- **Napier's kinematic logarithm (1614).** Two points move along two lines.
  One moves at constant speed. The other starts at the same speed and slows in
  proportion to the distance it still has to go. The distance covered by the
  first is the logarithm of the distance remaining for the second.
- **Bürgi's progress tables (1620)** pair an arithmetic progression with a
  geometric one. The slide rule (Oughtred, 1620s) multiplies by adding lengths
  on two logarithmic scales.
- **Descartes's unit (1637):** "Let AB be taken as unity, and let it be
  required to multiply BD by BC. I have only to join the points A and C, and
  draw DE parallel to CA; then BE is the product of BD and BC."
- **Leibniz's product rule (1675):** `d(xy) = x dy + y dx`.

*What it teaches Gojo.*
- **Multiplication is addition one rung up:** on angles in prosthaphaeresis,
  on logarithmic scales in Napier and the slide rule. The Drive article says
  the same thing: a nonlinear number line is an ordinary line with an operation
  applied.
- **Napier's picture is the "times two" that is used up as it approaches
  equilibrium.** A gap closing in proportion to itself spends a ratio, and the
  constant-speed point counts how much ratio has been spent. The time a
  proportional relaxation takes is a logarithm.
- **Descartes's product is a Gojo projection:** a frame calibrated by a unit
  and read at another point (Axiom 8). The product needs the unit, and the unit
  is a choice.
- **Leibniz's rule is the squeegee.** Each term is one operand swept along the
  other (§5.6).

### 4.7 What children have to learn

Research on how children come to multiply separates situations that the single
symbol `×` merges.

- **Vergnaud's multiplicative conceptual field** (1983, 1988) distinguishes an
  *isomorphism of measures*, a direct proportion between two measure spaces
  (bags to apples at four apples per bag), from a *product of measures* (area,
  combinations) and from multiple proportion. Multiplication, division, ratio,
  proportion and linear functions belong to one field.
- **Davydov and Elkonin** introduce number, and later multiplication, through
  measurement with an intermediate unit. Count the large units, count the
  small units in one large unit, and the product counts small units.
  Multiplication is a change of unit.
- **Confrey's splitting conjecture** (1994) treats splitting, making several
  equal versions of an original at once, as a primitive independent of
  counting. Steffe's counting schemes are the contrasting view.
- **Fischbein and colleagues** (1985) found repeated addition to be the
  intuitive model children fall back on, along with its misconception that
  multiplying always makes things bigger.
- **Stevens's scales of measurement** (1946). Interval scales admit
  differences; ratio scales, with a fixed zero, admit ratios and only
  similarity transformations.

*What it teaches Gojo.* `×` covers at least three structures that should be
kept apart:
- a **rate applied to an amount**, which is directional: the rate is read and
  the amount flows;
- a **product of measures**, which is new orthogonal structure;
- a **change of unit, or a split**, which is a move between rungs.

Stevens's scales are the request's "relative versus absolute": addition needs
only differences, while multiplication needs a zero.

### 4.8 Machines that compute by moving

- **Gears and differentials.** A gear pair is a ratio, and a train composes
  ratios: the compound ratio in metal. A differential adds or subtracts
  rotations. (`Gojo.GearShapeLadder` already exists in this repository.)
- **The ball-and-disk integrator** (James Thomson, 1876; used by Kelvin and in
  Bush's differential analyzer, 1931). A disk turns with the independent
  variable, and a ball's distance from the centre is the integrand. The output
  turns at a rate proportional to that distance: at the centre it does not
  turn, and past the centre it reverses. Multiplication here is a *setting* (a
  position) modulating a *flow* (a rotation), accumulated. Two integrators
  feeding each other solve `x'' = −x`, which gives sine and cosine.
- **The digital differential analyzer.** Northrop engineers (1949–50)
  replaced the wheel-and-disk integrators with accumulators whose overflow
  pulses are the output rate: the carry is the signal. Two such integrators in a
  loop draw circular arcs. Bresenham's line algorithm (1965) uses the same
  error accumulator.
- **CORDIC.** Volder (1959) rotated vectors with shifts and adds. Walther
  (1971) unified the elementary functions with one parameter, `m`:

  ```text
  x' = x − m·σ·y·2^−i      m = 1 circular, 0 linear, −1 hyperbolic
  y' = y + σ·x·2^−i
  z' = z − σ·e_i            e_i = atan 2^−i, 2^−i, atanh 2^−i
  ```

  Rotation mode drives `z` to zero by its sign; vectoring mode drives `y` to
  zero. The linear mode multiplies (`y + x·z`) and divides (`z + y/x`). The
  circular mode gives sine, cosine and arctangent, and the hyperbolic mode
  exponentials, logarithms and square roots. At each rung `i`, the only
  decision is a sign.
- **Minsky's circle algorithm** (HAKMEM item 149, 1972):
  `x ← x − ε·y`, then `y ← y + ε·x` using the *new* `x`. The orbits are closed
  ellipses, stable for |ε| < 2, and each half step is exactly invertible in
  integers, so integer orbits cycle. Updating both from the old values spirals
  outward.
- **Kelvin's tide predictor** (1870s) summed sinusoids with pulleys and a wire:
  addition of oscillations as a physical sum of lengths.

### 4.9 Computing with physical encodings

- **Stochastic computing** (Gaines and Poppelbaum in the 1960s; surveyed by
  Alaghi, Qian and Hayes, 2018). A value is the fraction of ones in a stream.
  AND of two independent streams multiplies them, and a multiplexer gives
  scaled addition. Precision costs stream length.
- **Race logic** (Madhavan, Sherwood and Strukov, 2014). A value is an arrival
  time. The first arrival (OR) is the minimum, the last (AND) is the maximum,
  and a delay adds a constant. The operators are MIN, MAX, ADD-CONSTANT and
  INHIBIT. Later work frames this as tropical algebra, in which
  "multiplication" is the addition of delays.
- **Signed digits and online arithmetic.** Avizienis (1961): with digits from
  {−1, 0, 1}, a carry travels only to the neighbouring digit, so addition time
  does not grow with length. Ercegovac's online arithmetic works from the most
  significant digit first, with a fixed delay. ODV words are signed digits with
  an undetectable value.
- **Conservative logic** (Fredkin and Toffoli, 1982). Gates are invertible and
  conserve the number of ones; the Fredkin gate, a controlled swap, is
  universal. In the billiard-ball model, logic is balls deflecting one another,
  so AND is two balls meeting.
- **Chemical reaction networks.** Counts that react without regard to rate
  compute exactly the semilinear functions (Chen, Doty and Soloveichik,
  2012/2014, extending Angluin, Aspnes and Eisenstat): sums, constant
  multiples, minimums, maximums and comparisons, piece by piece. The product of
  two input counts is not among them. Computing it needs sequencing.
- **Interaction nets and linear logic.** In Lafont's interaction nets (1990),
  computation is local rewriting where two agents meet at their principal
  ports. They build on Girard's linear logic (1987), in which a resource is
  used exactly once unless structure explicitly copies or discards it.
- **Laws of Form** (Spencer-Brown, 1969). The law of calling, "the value of a
  call made again is the value of the call" (condensation), and the law of
  crossing, "the value of a crossing made again is not the value of the
  crossing" (cancellation).

### 4.10 The lessons in one table

| Source | Mechanism | Lesson for Gojo |
| --- | --- | --- |
| Egyptian doubling | Gated sum over a ladder of doublings | The multiplier gates; the multiplicand is material |
| False position (Egypt, Babylon) | Guess, read the error, scale | Solve by lowering tension against a goal |
| Babylonian floating place value | Ratio with the scale left to context | Relative magnitude is an uncalibrated read |
| Reciprocal tables | Division as multiplication, read from a table | A relation read backward; regularity belongs to the base |
| Cut-and-paste | Rearranging area at a fixed total | Algebra as conserving settlement; completion adds the corner |
| Eudoxus | Ratio by equimultiples and comparison | Counting and comparing suffice for proportion |
| Euclid VII def 15 | Defined asymmetrically, proved symmetric | Roles are real even when the total commutes |
| Compound ratio | `A:B` with `B:C` gives `A:C` | Multiplication of ratios is relation composition |
| Gnomon | Odd numbers build squares | The incremental product: row, column and corner |
| Lever, steelyard | Balance at reciprocal distances | Equilibrium as computation |
| Red and black rods | Two signed faces on one board | Keep `T+` and `T−` apart |
| Excess and deficit | Interpolate an overshoot and an undershoot | Two-faced residues settle the answer |
| Āryabhaṭa | Second difference proportional to minus the sine | Sine as coupled accumulation; the coupling is a calibration |
| Lattice, Napier's bones | Grid of digit products, diagonals by place | Product of ladders; rung indices add |
| Prosthaphaeresis, Napier, slide rule | Multiply by adding angles or logarithms | Multiplication is addition one rung up |
| Descartes | Product through a unit and parallels | Multiplication is projection through a calibrated frame |
| Leibniz | `d(xy) = x dy + y dx` | Each operand sweeps along the other |
| Vergnaud, Davydov, Confrey | Rate on amount, product of measures, change of unit, splitting | `×` names several structures |
| Ball-and-disk, DDA | A position modulates a flow, accumulated | Multiplication as setting × flow over time |
| CORDIC | One iteration with `m ∈ {1, 0, −1}` | One mechanism for sine, multiplication and exponential |
| Minsky | Update with the new value | Step order decides whether the orbit closes |
| Stochastic and race logic | AND is a product, or a maximum | The encoding decides what an act means |
| Signed digits, online arithmetic | Local carries, most significant digit first | Coarse-first arithmetic over ODV-like digits |
| Conservative logic | Invertible gates that conserve ones | Logic as conserving routing |
| Chemical reaction networks | Rate-independent reactions compute semilinear functions | Products of two counts need sequencing |
| Laws of Form | Calling and crossing | Clamp and wrap are the two primitive boundaries |

## 5. Distinctions the survey forces

### 5.1 What is being combined

| Kind | Example | Combines with its own kind by | In Gojo today |
| --- | --- | --- | --- |
| Presence | a bit, an event, an occupied site | co-presence (AND), clamped union (OR), parity (XOR) | planes; prompt §9.7 |
| Amount (extensive) | units, charge, apples | merging: a conserving move | unary counts, `Move` |
| Position | a site against a landmark | difference gives an extent; an extent translates it; weights average it | locations, `Sides`, ODV |
| Rate (intensive) | price, speed, gear ratio, valve opening | composition (rates multiply) | ratio projection, `Convert` |
| Frame | an origin and a unit; a rung | composition through a shared middle frame | projection, unit shape, `ResolutionOf` |

Across kinds:

- **A rate on an amount gives an amount** on the rate's target carrier. The
  rate is read and the amount moves: price × quantity, speed × time, a gear on
  a shaft.
- **An amount with an amount on another carrier gives pairs:** an area, a set
  of combinations, a new orthogonal structure (Axiom 11).
- **A position plus an extent gives a position;** a position minus a position
  gives an extent. `Sides` already reads these as "a step moves the side" and
  "the sides agree".
- **Intensive plus intensive is not defined.** Two cups of water at 50 and 30
  degrees do not make 80. Combine the extensives the intensives are ratios of
  (heat and mass), then read the ratio again: a weighted average. This is the
  blend in the Drive article's red and blue water.

### 5.2 Constants live on connections; variables meet at junctions

- **One live input.** Scaling or shifting by a constant is a calibration, and
  it sits on a relation between two frames. In the kernel this is
  `S(C0,0,k) -> S(C1,0,m)`, which multiplies by `m/k`, or a general
  `S(C0,a,b) -> S(C1,c,d)`, which carries a scale and a shift. In EncodingC it
  is `Convert(take, give)` between two counts.
- **Two live inputs.** `a·b` with both live needs three participants. That is a
  junction: a law over at least three roles inside one local structure. In
  EncodingC it is a row of a relation or transition over `(a, b, c)`; in the
  kernel it is an operation port, `(portA -> portB) -> output`.
- **Partial application.** A junction with one role fixed to a constant is a
  connection: the slide rule with its slide clamped, a gear of fixed ratio. A
  connection whose ratio is read live from another carrier is a junction: a
  variable gear, a valve set by another structure.

The canonical prompt (§4.6) already treats calibration this way: allowed and
never required, absorbed into a sender or receiver, or standing alone as a
small structure.

### 5.3 Conservation: which operations can be moves

- **What can be taken through a port:** preserved sums with positive weights.
  That covers moves, branches, merges, constant-rate conversions (`Convert`)
  and carries. All are additive in a calibrated measure.
- **What cannot:** a product of two live counts. No preserved sum relates `a`,
  `b` and `a·b`, so a law that computes `c = a·b` from two counts is not a move.
  The result on chemical reaction networks corroborates this: rate-independent
  reactions can stably add, scale by constants, take minimums and compare, but
  they cannot stably compute the product of two inputs.
- **Each multiplicative situation still has an accountable realization:**
  1. *Refinement:* the same material counted in a finer unit. Three bags of
     four become twelve apples by running a carry downward. This is Axiom 9's
     material refinement, and it conserves.
  2. *Supplied flow:* a rate is read, and a supply drains into a target for a
     duration. This conserves overall, and any shortfall is draw tension on the
     target.
  3. *Pairing:* new orthogonal structure, an area or a set of combinations.
     It is a frame, not material.

  The only thing not allowed is creating units from nothing.
- **A second invariant: reversibility.** A shear, `y += f(x)` with `x` only
  read, is exactly invertible even though it conserves no sum. Minsky's integer
  oscillator cycles because each half step is a shear. Conservative logic holds
  both invariants at once. Gojo has a word for the first invariant
  (accounting), but not yet for the second.

### 5.4 Rungs: multiplication is addition one rung up

- **A ladder** is counts `c0, c1, …` with carry laws `Convert(k, 1)` between
  neighbours. The weights of the preserved sum are the place values
  (`WhatDoesNotFitIsCarriedUpARung`).
- **Reading a coarser rung divides by the base,** with the finer rung as
  residue. **Moving a whole number up a rung multiplies by the base.** If rungs
  are addressed by reference, that move is a retarget, not a copy.
- **The rung index is a logarithm.** A lattice cell's rung is the sum of its
  row's rung and its column's, so place-value multiplication adds rung
  indices while it multiplies digits: the slide rule inside long
  multiplication.
- **Babylonian relative place value** left the rung offset to context, which
  is an uncalibrated read.
- **The kernel already has the pieces** (implemented): `ResolutionOf` and
  `SubdivisionOf` for coarser and finer ticks, ratio projections, and carry
  routes. Multiplying or dividing by a constant is moving between rungs.

### 5.5 One bitwise act, several meanings

| Act | On single bits | On unary (thermometer) planes | On stochastic streams | On arrival times |
| --- | --- | --- | --- | --- |
| AND | both present; the product; the carry of the sum | minimum | product | maximum (last to arrive) |
| OR | either present; the clamped sum | maximum | `p + q − pq` | minimum (first to arrive) |
| XOR | parity; the wrapped sum | the sites between the two values | — | — |
| delay | — | — | — | add a constant |
| multiplexer | — | — | scaled sum | — |

Two consequences for EncodingC, whose counts are thermometer codes (bit `j`
means "more than `j`"):

- **AND of two counts' planes is their minimum, and OR their maximum**
  (Appendix A). The product of two counts is not a planewise act. It needs
  pairing or a process.
- **XOR of two counts' planes is the span of sites between them,** whose
  population is their distance. Read as a count it is broken (`0110` is not a
  prefix); read as a set of sites it is a segment between two landmarks. The
  same bits mean different things to different readers.

Canonical prompt §9.7 ("a product is presence of both") is therefore right at
the bit rung and wrong at the count rung. §8 below flags this.

### 5.6 The path is part of the product

When `a` and `b` grow one unit at a time, the product grows by
`Δ(ab) = aΔb + bΔa + ΔaΔb`: a row, a column and a corner. Each new unit of `A`
pairs with every unit of `B` already present, and the reverse.

- **The total does not depend on the path; the split does.** With
  `a = 3, b = 4` (Appendix A):
  - *L path* (all of `A`, then all of `B`): 12 cells, all swept by `B`.
  - *Diagonal* (both together three times, then `B` alone): 3 cells swept by
    `A`, 6 by `B`, and 3 corners.
- **Growing together builds squares by gnomons:** 1, 3, 5. This is the
  Pythagorean construction.
- **In a fine read the corners vanish** (`ΔaΔb` is second order), and the two
  sweeps become the two triangles of the squeegee article,
  `½·3·4 + ½·4·3`. That is Leibniz's rule, and the dual-number pruning of the
  "Mathematics Without Infinity" article.
- **"Nothing in math distinguishes an L from a \\"** is exactly this
  provenance, and Gojo can keep it. Three planes per product cell (swept by A,
  swept by B, corner) would make it inspectable at the cost of the grid. A
  count of each kind would keep the split without the grid.

### 5.7 The order of a step can carry meaning

- **The integer oscillator** `x += coarse(v)`, `v −= coarse(x)`, stepped in
  declared order (velocity reads the new position), returns exactly to its
  starting point after 54 steps, with its amplitude never above the starting
  64. Stepped together, with both reading the old state, it spirals out to 1187
  in 400 steps (Appendix A).
- **This is the difference between the symplectic (leapfrog) and the explicit
  Euler step** in numerical physics. In declared order, every half step is a
  shear, and shears preserve area.
- **Āryabhaṭa's coupling shows the same sensitivity,** to calibration rather
  than order (§4.5).

EncodingC's README says the step kind is chosen by whoever steps a machine
and is not saved with it. For coupled accumulators that choice decides whether
an emergent invariant holds, so it belongs in the structure (§8).

### 5.8 Solving by lowering tension is old

| Method | Tension | How it is lowered |
| --- | --- | --- |
| False position (Egypt, Babylon) | the error of a convenient guess | scale the guess by goal / error |
| Excess and deficit (China) | an overshoot and an undershoot | cross-weight the two residues |
| Iterated averaging (Babylon, Heron) | `x` and `N/x` disagree | replace `x` by their mean |
| Steelyard (Rome, from Archimedes' law) | unbalanced torque | slide the counterpoise until level |
| CORDIC, non-restoring division | remaining angle or remainder | apply the rung's step in the direction of its sign |
| Napier's moving point | remaining distance | close it in proportion to itself |

Each settles a visible residual with a local rule. None hands an equation to a
solver.

## 6. The questions, answered

### 6.1 Are operations on connections or structure of their own?

Both, depending on how many live inputs there are (§5.2), and never on the
connection *between* local structures except as a read or a move.

- **Between local structures (ports),** only reads and takes cross. A take
  must conserve, so the arithmetic that crosses a port materially is addition
  in a calibrated measure: moves, branches, merges, constant-rate conversions
  and carries. Every other operation crosses as a read by reference and
  happens inside the receiver. EncodingC already enforces this (§2.3).
- **Inside a local structure,** a one-input operation is a calibration on a
  relation (a connection), and a two-input operation is a junction: a small
  structure of its own with input roles it reads and an output role it writes.
- **The kernel's rule** that the output's shape chooses the operation (a point
  output sums, a frame output multiplies) is a good *fold* rule. It says
  whether the result stays in its grade or opens a new one. It does not yet say
  which operand is the rate and which the amount, or where product material
  comes from.

Gojo has no single word for a junction yet: the kernel says "operation port"
and EncodingC says "row of a wiring". One word would help (§8).

### 6.2 What does it mean for a larger structure to be the target of a multiplication?

There are five distinct meanings, and each should be visible in the structure.

1. **Uniform scaling (similarity) edits the frame, not the elements.** If a
   structure's measures are read through one shared frame, multiplying the
   structure retargets the frame's unit landmark (one `TickRef`). Every
   dependent read follows, internal ratios are unchanged, and nothing inside is
   edited: this is the prompt's §4.1, a structural port retargeting a `TickRef`. Adding a
   constant to the whole structure retargets its origin instead: translation.
   Both edits are constant-time. **Distributivity follows:** `k(a + b) = ka + kb`
   because the concatenated parts share the frame.
2. **Material multiplication of a structure's counts** needs a supply (each
   unit draws `k − 1` more), or a change of unit (crystallize a carrier whose
   tick is `k` old ticks). With a change of unit, no material moves: the count
   stays the same and the unit grows, as in Core4's tick attachment. Which one
   is meant must show.
3. **Structure × structure is a sweep:** every part of `A` with every part of
   `B`. Two-part segments give four quadrants, two ladders give a lattice, and a
   profile swept along a path is an extrusion. The grade rises by one.
4. **A structure can be the multiplier.** Its count gates repetition of
   another structure (Euclid VII definition 15). The repository's frieze and
   ornament work is multiplication as repetition of a motif along a path.
5. **Non-uniform scaling** is a field of rates over a structure, each part read
   through its own local calibration: a map rather than a frame, or in the
   Drive article's words, a nonlinear number line.

### 6.3 Should `A·B` modify `B`, create a new result, or both?

Keep three reads apart, and let the law show which one is meant.

- **The relation (what holds).** `c = a·b` as a relation law over
  `(a, b, c)` is symmetric and reads in every direction. Given `a` and `c` it
  divides. Given `a = 0` and `c = 0`, `b` stays open, every value possible,
  which is exactly Axiom 10's "A zero or open denominator cannot supply a
  numeric answer. Preserve the participants and an open result." Given `a = 0`
  and `c ≠ 0`, it is a
  contradiction, all zeros. Nothing is modified; information narrows.
- **The process (how it happens),** which is directional per step:
  - *In place,* `B ← B·A`: a transition writing `B` while `A` is read. This is
    a gain on `B`'s own flow, or compounding (`B += B·r`). Under Axiom 5 it is
    tension introduced on `B` (the demanded growth) and absorbed from a supply.
    That is the Drive article's "times two" being used up, or Napier's
    proportional gap. Use it when `B` is a running amount and `A` a setting.
  - *New result,* `C ← A·B`: a transition writing only `C`, with `A` and `B`
    read. A meter reading rate × time.
  - *Mutual:* the arrival-pairing process, where whichever operand gains a unit
    sweeps it along the other. Each event is directional; the total is
    symmetric; the split records the path (§5.6). This is the squeegee's
    "A into B, and B into A".
- **The fold (what is read):** a scalar, the four quadrants, or a new frame.

The roles stay asymmetric even when the total commutes. Partitive division
recovers the size of a group from the number of groups; quotative division
recovers the number of groups from their size. Euclid defines multiplication
asymmetrically, Core 2's F10 calls the factors state and transform, and Axiom
10 makes commutativity a local read.

**Recommendation.** Never a flag. In EncodingC the direction is already
visible from which roles a law writes and which preserved sums it keeps. Make
the default a new read (non-mutating, Axiom 8), use in-place change only as an
explicit process on the target, and use the relation when an operation must be
read backward.

### 6.4 Where do boolean operations fit?

- **At rung 0, they are arithmetic with boundary accounting.** AND is
  co-presence: the product of two presences, and the carry of their one-bit
  sum. OR is the clamped one-bit sum (Axiom 7's stop boundary); XOR is the
  wrapped one (the modulo boundary). The half adder is the wrap plus the carry.
- **NOT is room within a scope.** Complement needs a frame (Core 2 S2; the prompt's §8.2).
  The tension note's room view, a count's planes inverted and read in reverse
  order, is the rung-0 NOT within a capacity.
- **Spencer-Brown's two laws are the two boundaries:** calling (condensation,
  idempotent) is the clamp, and crossing (cancellation, an involution) is the
  wrap.
- **The 16 functions are routings.** Partition by co-presence into the four
  quadrants (both, A only, B only, neither), then select which quadrants feed
  the output. If the unselected quadrants remain as visible residue, the
  operation conserves, as in conservative logic. This is the Drive articles'
  "four areas, 16 combinations" and Core 2's "truth evaluated on constant
  partitions".
- **A gate is AND applied to a flow.** `GatedMove` and the passage law multiply
  a flow by a bit, and a valve with `k` positions passing `j` multiplies it by
  `j/k`.
- **On counts, AND and OR are the minimum and maximum** (§5.5).
- **Weighted booleans,** which the Drive article interpolates by probability,
  are stochastic computing: AND of independent rates is their product.

### 6.5 Sine and cosine, and other stacked operations

**At rung 0 the oscillator is a Sides sentence.** Take the sign of the
position against the landmark and the sign of the velocity. As the oscillator
runs, the pair visits four quadrants in Gray order, one bit flipping per
quarter: turned back (the velocity flips), crossed (the side flips), turned
back, crossed. Sides already has both events.

```text
(+, +)  -turned back->  (+, −)  -crossed->  (−, −)  -turned back->  (−, +)  -crossed->  (+, +)
```

Two quarters negate both signs, and four return. That is Axiom 10's generator
action, read on quadrants rather than on unit directions. Neither flip alone is
the generator, which agrees with the prompt ("the primitive face dual remains
an involution; it is not multiplication by i"). The generator is a
state-dependent law that alternates two flips over time. In EncodingC it is a
transition law on two bits: `11 → 10 → 00 → 01 → 11`.

**With magnitude, it is two accumulators.**

```text
x : signed position against the landmark
v : signed velocity
each step, in declared order:
  x += coarse(v)          coarse = read through k rungs
  v -= m · coarse(x)      m = +1 restoring, 0 none, −1 reinforcing
```

- **The material account.** The velocity the mover gains comes from the
  landmark, which takes the opposite momentum (Newton's third law). That is a
  conserving move between the mover's velocity count and the landmark's, at a
  rate read from the distance. The position integrates the velocity by a shear,
  which conserves no sum but is exactly reversible.
- **How fine the pull is read shapes the wave.** A pull read only as a side
  (constant magnitude toward the landmark) gives parabolic arcs, a rounded
  triangle wave. A pull proportional to the distance gives the sine. Refining
  the read refines the waveform, coarse first.
- **The sign `m` selects the geometry.** `m = +1` gives oscillation;
  `m = 0` gives coasting, where the position grows by the rate each step (rate
  × time, a multiplication); `m = −1` gives exponential growth. Appendix A
  shows all three. `m` is a number, not an enum, and CORDIC uses exactly it.
  It is also the fold of the inverse × inverse quadrant with its sign turned
  over: the unit's square is `−m`. Complex numbers (`i² = −1`) are `m = +1`,
  dual numbers (`ε² = 0`) are `m = 0`, and split-complex numbers (`j² = +1`)
  are `m = −1`. The Drive article that keeps `ac + bd` unfolded until it is
  "converted ... during folding" has already separated the quadrant from the
  sign it folds with.
- **The family interpolates.** One tick of coasting adds the rate; `n` ticks
  multiply by `n`; turning `m` negative compounds; turning it positive
  oscillates. The Drive article's hope of "interpolating between ... even
  addition and multiplication" when they are modelled as continuous systems is
  a walk through `(duration, m)`.

**Other stacked operations settle a remaining tension.** CORDIC's rotation and
vectoring modes, non-restoring division and successive approximation read only
the sign of what remains at each rung and apply that rung's step. Heron's
averaging and false position read its size and correct in proportion. Division
is running until the gap closes and
counting the steps (quotition, the Axiom 7 carry) or splitting round-robin into
`n` lanes (partition, a conserving branch). Roots and logarithms keep candidate
sets (Core 2 R13 to R15; the prompt's fractional-power reads).

## 7. Three approaches

Each approach realizes the Drive articles' pin, merge and fold differently.

### 7.1 A, junction tables: the scribe

**Idea.** Every operation is a small relation law over coarse roles: a
one-digit times table, the ODV addition table, AND. It is used at every rung
of a ladder, with carries between rungs, as in lattice multiplication. Reading
in every direction comes free with the relation. A process, where one is
needed, is the relation crystallized into a transition.

```text
times  = relation over (a: 0..3, b: 0..3, c: 0..9), admitted where c = a·b
         4·4·10 = 160 bits, 16 admitted tuples
forward:   a = 2, b = 3   ->  c = 6
backward:  a = 2, c = 6   ->  b = 3            division
           a = 0, c = 0   ->  b open {0..3}    the open zero
           a = 0, c = 4   ->  contradiction
ladder:    digits a1 a0, b1 b0
           cells (a0,b0) at rung 0; (a0,b1), (a1,b0) at rung 1; (a1,b1) at rung 2
           sum each rung's cells; carry up with Convert(base, 1)
```

**Pin, merge, fold.** Pinning wires a row and binds its roles; merging is
narrowing; folding is choosing which role to read.

**Strengths.**
- It uses machinery that already works: `RelationLaw`, narrowing,
  `LearnedTable`, carries.
- It reads in every direction, giving division, subtraction, the open zero and
  contradiction.
- It is exact, and learnable: tables crystallize from counts.
- It is a lookup table on any substrate (a LUT6 holds the passage law).
- It is how the scribes worked.

**Weaknesses.**
- It is static: no time, no momentum, no sine without iterating over it.
- Tables grow multiplicatively. A 4-bit by 4-bit product as one relation is
  `16·16·226 = 57,856` bits, so ladders are mandatory.
- The path (L or diagonal) is lost unless recorded.
- A symmetric table hides the rate–amount asymmetry; direction comes only from
  wiring.

**Fit.** It follows the prompt's §8.1 and §9.6 closely. "Lowering tension" here means
narrowing information tension, which is real settlement but not material
dynamics.

### 7.2 B, coupled flows: the machine

**Idea.** Every operation is a process of accumulators coupled by rates. It
settles a remaining tension and refines coarse first through rungs.

| Operation | Process |
| --- | --- |
| Add | merge: a move |
| Multiply | coast (a rate read, a supply drained into a target for a duration) or pair arrivals |
| Divide | run until the gap closes and count the steps, or split round-robin into lanes |
| Oscillate, exponentiate, take a logarithm | the `m` family |
| Roots | Heron's averaging or false position |
| Boolean | one-shot co-presence routing |

```text
rate   r : a count read by reference (a valve setting)   intensive, read
supply S : a count                                        extensive, drained
target T : a count                                        extensive, filled
clock    : d ticks                                        the duration, a controller
each tick: move r units S -> T     preserved S + T
after d ticks: T gained r·d; S lost r·d; any shortfall is draw tension on T
```

**Pin, merge, fold.** Pinning couples rows in one local structure, or plugs a
socket; merging is stepping; folding reads an accumulator at a chosen rung.

**Strengths.**
- It is physically intuitive: pouring, a valve over time, the squeegee, a
  spring, compounding.
- Intermediate states are meaningful numbers. A partial product or a
  remaining tension is exactly the inspectable intermediate state the axioms
  ask for.
- The roles are explicit: rates are read, amounts move.
- It is native to tension. The "times two" being used up is goal tension in a
  logarithmic basis, and supply shortfall is `T−`.
- Sine and cosine come free, and it reuses `Convert`, `Move`, `Sides` and
  `StepTogether`.
- Path provenance is natural.

**Weaknesses.**
- It takes time: `n` steps for `n` units, or `log n` with rungs, as in CORDIC.
- Calibration of the coupling and the order of the step both carry meaning,
  so both must be structure.
- The reaction-network result says a product needs sequencing, so B needs an
  explicit clock or order.
- An inverse is another process, not the same table read backward.

**Fit.** It is the closest to "dynamically lowers tension rather than solves
equations", and it puts Core4's oscillation idea and the tension note on one
mechanism.

### 7.3 C, frames and rungs: the geometer

**Idea.** Numbers are ladders of partitions, and an operation is an edit of a
frame or a read through composed frames.

| Operation | Frame act |
| --- | --- |
| Add | concatenate extents that share a site (a join) |
| Translate | move the origin landmark |
| Multiply | change of unit: compose projections through a shared middle frame |
| Divide | project backward, with residue |
| Power | repeated composition, a train of projections (the "parallel" `3^3`) |
| Logarithm | count rungs |
| Product of measures | an orthogonal frame, a sheet |

```text
unit frame           S(U,0,1)
times b              S(U,0,1) -> S(C,0,b)        a reads as a·b (Axiom 8)
compound ratio       S(U,0,1) -> S(C,0,b), then S(C,0,1) -> S(D,0,c)
                     the middle frame C is shared; a reads as a·b·c
scale a structure    retarget the TickRef at its frame's unit end
translate it         retarget the TickRef at its origin
live multiplier      bind the unit end to another carrier's value (a slide rule)
logarithmic carrier  the rung index of a ladder; products add rung indices
```

**Pin, merge, fold.** Pinning shares a site or a frame; merging composes
projections; folding crystallizes.

**Strengths.**
- The kernel already has projection with residue, `ResolutionOf` and
  `SubdivisionOf`, ratio projections, carry routes and unit quotients.
- Scaling a whole structure is one constant-time retarget.
- Meaning lives in the frame, the strongest fit with Axiom 1.
- It ties operations and numerics together, and similarity comes free.

**Weaknesses.**
- Projection is observation, so material multiplication needs an explicit
  import or transfer route.
- Dynamics such as sine are not native; they need the contour construction.
- A product of two live values needs frames as values (a unit landmark bound
  to a live carrier).
- Exact readers with rational residue are already the canonical prompt's
  hardest area.

**Fit.** It is the most Gojo-native for meaning, and the least for motion.

### 7.4 Side by side

| | A: junction tables | B: coupled flows | C: frames and rungs |
| --- | --- | --- | --- |
| What an operation is | a small relation, reused at every rung with carries | accumulators coupled by rates, settling a remainder | a frame edit, or a read through composed frames |
| Physical picture | a times table; a reflex | a valve over time, a spring, a squeegee | a ruler, a gear train, a slide rule, a zoom |
| Where it lives | a junction row in one local structure | a small local structure of counts and rate laws | relations between frames; tick references |
| Direction | from wiring: a relation reads every way, a transition writes | built in: rates read, amounts move | the direction of projection; retargeting writes |
| Material | none: information narrows | drained from a supply, or refined | none until an import or crystallization |
| Inverse | the same table read backward | another process | the projection read backward, with residue |
| Intermediate states | narrowing supports | partial products and remaining tension | partial frames and residue |
| Path provenance | lost unless recorded | natural | not native |
| Sine and cosine | a table, or iteration | native, `m = +1` | via the contour construction |
| Exactness | exact | exact in integers; the coupling is calibrated | exact, with rational residue |
| Cost | table size multiplies | time, or `log n` with rungs | constant-time retargets; reads cost a projection |
| Footholds | `RelationLaw`, `LearnedTable`, carries | `Convert`, `Move`, `Sides`, `StepTogether`, tension note | kernel projection, unit shapes, `RepeatSurface` |
| Main risk | static; hides rate and amount | step order and calibration carry meaning | dynamics and material bolted on |

### 7.5 How they fit together, and a recommendation

They are not exclusive. **C says what multiplication means**, a relation
between frames or rungs. **B says how it runs**, physically and in time. **A is
what a repeated B crystallizes into:** a learned law that answers at once. That
last step matches the existing direction that "useful history may crystallize
into an algorithm" (the prompt's §6) and EncodingC's learning of laws from outcomes.

A suggested order:

1. **At rung 0 in EncodingC, build B's primitives with C's coupling:** a
   carry ladder as the rung, a coupled pair as the process, with step order
   saved as structure.
2. **Record path provenance** for products as evidence planes or counts.
3. **Crystallize tables (A) from observed processes** with `LearnedTable`, and
   check that they read backward as relations.

The alternative, starting from A, is faster to build but would postpone every
question the request is about: direction, physics, sine and material.

## 8. Conflicts and gaps in the axioms

These are stated as requested, each with the smallest clarification that would
resolve it.

1. **The step kind carries meaning but is not saved.** The EncodingC README
   leaves the choice between stepping in order and stepping together to
   whoever runs the machine. For coupled accumulators this decides whether an
   orbit closes or spirals (§5.7). *Clarify:* the order between coupled rows
   (or the step kind) is structure, saved with the machine.
2. **The prompt does not say where product material comes from.** Its §4.4
   accounts incoming participation as accepted, blocked and remainder, and
   Axiom 5 sends external change in as tension, but a multiplication of
   material has no conserving form. *Clarify:* "A product is a read unless a
   refinement or supply route is wired. A product that is demanded and not
   supplied is draw tension."
3. **Intensive and extensive are not distinguished.** Axiom 10 says
   compatible addition may collapse, but compatibility is stated in units, and
   intensive quantities with the same unit (temperatures, densities, rates)
   still do not add. *Clarify:* addition is defined on amounts and extents;
   intensive reads combine through the extensives they are ratios of.
4. **The prompt's §9.7, "a product is presence of both", holds only at the bit rung.** On
   unary planes, AND is the minimum (§5.5). *Clarify* the scope, so that nobody
   wires a planewise AND across counts as a multiplier.
5. **The generator and the flips should be named apart.** The symmetry
   articles' four units and EncodingC's three orientation moves form a group
   of flips, each its own inverse. Axiom 10's generator is a quarter turn whose
   square negates. The prompt already says the face dual is not
   multiplication by `i`. *Clarify:* a quarter turn can be built as a
   state-dependent alternation of two flips over time (§6.5), so the generator
   needs time order as well as the flips.
6. **The kernel's output-grade rule is a fold rule.** "A point output sums, a
   frame output multiplies" says whether the result stays in its grade. It
   should not be read as the process, the rate–amount roles, or the supply.
7. **There is no single word for a junction.** The kernel says "operation
   port" and EncodingC "row of a wiring". One word would help the axioms say
   §5.2.
8. **The tension note needs no new kind for operations.** The "times two being
   used up" is goal tension read in a logarithmic or rung basis, and supply
   shortfall is material deficit. This agrees with the note.
9. **Reversibility has no name yet.** Accounting covers conserved sums, but
   shears conserve information without conserving any sum (§5.3). If
   reversibility is to be a promise a law can carry, it needs a word in the
   prompt.

## 9. Experiments that would decide

These are not a plan; each would show which approach carries a claim most
cheaply. All are at rung 0 in EncodingC unless noted.

| Id | Build | Expected structure | Decides |
| --- | --- | --- | --- |
| E1 | A times relation over `(a, b, c)` | divides; leaves `b` open when `a = c = 0`; contradiction when `a = 0` and `c ≠ 0` | A's backward reads |
| E2 | A two-rung lattice: four cells of one table, sums by rung, a carry | equals the product; table bits counted against one wide relation | A plus C at scale |
| E3 | A coasting multiplier: a rate read by reference, a supply, a target, a clock | target `= r·d`; supply drained; shortfall left as `T−` | B's material account |
| E4 | Arrival pairing with planes for swept by A, swept by B, and corners | equal totals with different provenance on the L and diagonal paths | B's path record; prompt §5.3 |
| E5 | The two-bit Sides phase law | `11 → 10 → 00 → 01`; two steps negate; four return | rung-0 sine; Axiom 10's generator |
| E6 | The Minsky pair with coarse reads, both step kinds | in order closes; together spirals | step order as structure (§8.1) |
| E7 | The same machine for `m = +1, 0, −1` | oscillation, linear growth, exponential growth | the `m` family |
| E8 | Scaling a structure by retargeting its unit `TickRef` (kernel) | every read scales; ratios unchanged; nothing else edited | C's frame edit |
| E9 | A change of unit as a carry run downward | 3 coarse units become 12 fine ones; the place-value sum is preserved | conserving multiplication (§5.3) |
| E10 | Clamp, wrap and carry at the bit rung; routing of the four quadrants | OR, XOR and AND; 16 functions as selections with residue kept | boolean as arithmetic |
| E11 | Quotition and partition | counting steps to empty equals the carry route; a round-robin split conserves | the two divisions |
| E12 | A coarse-first settle: the remainder as a signed-digit ladder, a sign read per rung | converges toward the exact projection, one rung at a time | B with C's rungs |

## 10. Questions back

1. **Is an operation primarily the relation, the process, or the fold?** This
   note argues Gojo needs all three as distinct reads. Is that right?
2. **Should a product keep its path provenance (L or diagonal) by default,** or
   only when asked?
3. **Is it acceptable to make step order meaning-bearing,** saved with the
   machine?
4. **Should intensive versus extensive become an axiom-level distinction?**
5. **Where should a coupling rate live:** as a constant in a `Convert` law,
   fixed when the law is made, or as a count in a store, read by reference so
   it can be perturbed and learned? The preferences point to the second.
6. **Which first fixture?** The proposal is E5, E6 and E7 together (the
   oscillator family at rung 0), then E3 and E4 (multiplication as a process
   with provenance).

## Appendix A: numeric checks

The script below checks the small numeric claims in this note. Run it with
`python3 -I checks.py`. Everything is integer arithmetic except the Āryabhaṭa
table.

```python
import math

def coarse(v, s):                      # read v through s coarser binary rungs
    return -((-v) >> s) if v < 0 else v >> s

def run(x, v, s, steps, in_order, m=1):
    """x += coarse(v); v -= m*coarse(x). In order: v reads the new x."""
    path = [(x, v)]
    for _ in range(steps):
        if in_order:
            x = x + coarse(v, s); v = v - m * coarse(x, s)
        else:
            x, v = x + coarse(v, s), v - m * coarse(x, s)
        path.append((x, v))
    return path

# 1. Step order: declared order closes the orbit, a step taken together spirals.
p = run(64, 0, 3, 400, in_order=True)
back = next(i for i, q in enumerate(p[1:], 1) if q == p[0])
print("in order: back at start after", back, "steps, max |x|", max(abs(q[0]) for q in p))
p = run(64, 0, 3, 400, in_order=False)
print("together: max |x| over 400 steps", max(abs(q[0]) for q in p))

# 2. One machine, the sign m of its feedback.
for m in (1, 0, -1):
    p = run(64, 64, 3, 24, True, m)
    print(f"m={m:+d}: x at steps 0, 8, 16, 24 =", [p[i][0] for i in (0, 8, 16, 24)])
print("m=0 coasting at rate 5 for 7 steps: x =", run(0, 40, 3, 7, True, 0)[-1][0])

# 3. Product by pairing arrivals; the split records the path.
def pairing(arrivals):
    a = b = a_last = b_last = corners = 0
    for da, db in arrivals:
        if da and db: a_last += b; b_last += a; corners += 1
        elif da:      a_last += b
        elif db:      b_last += a
        a += da; b += db
    return dict(a=a, b=b, product=a_last + b_last + corners,
                swept_by_A=a_last, swept_by_B=b_last, corners=corners)
print("L path  :", pairing([(1, 0)] * 3 + [(0, 1)] * 4))
print("diagonal:", pairing([(1, 1)] * 3 + [(0, 1)]))

# 4. Aryabhata: second difference = -(sine so far)/coupling, R = 3438.
for coupling in (225.0, 1 / (2 - 2 * math.cos(math.radians(3.75)))):
    d, s, t = 225.0, 0.0, []
    for _ in range(24):
        s += d; t.append(round(s)); d -= s / coupling
    print(f"coupling 1/{coupling:.1f}: first {t[:5]}, peak {max(t)}, true 3438")

# 5. Bit rung: carry of a+b is a*b; OR is the clamped sum, XOR the wrapped sum.
print("bits a b | carry AND | clamp OR | wrap XOR")
for a in (0, 1):
    for b in (0, 1):
        print(f"     {a} {b} |   {(a+b)//2}    {a&b} |   {min(a+b,1)}    {a|b} |  {(a+b)%2}    {a^b}")

# 6. Unary (thermometer) planes: AND is min, OR is max, XOR is the span between.
th = lambda n: (1 << n) - 1           # bit j set means "more than j"
a, b = 5, 2
print("unary 5,2: AND", bin(th(a) & th(b)), "= min", bin(th(min(a, b))),
      "| OR", bin(th(a) | th(b)), "= max", bin(th(max(a, b))),
      "| XOR", bin(th(a) ^ th(b)), "= sites 2..4, popcount", bin(th(a) ^ th(b)).count("1"))
```

Output, 25 November 2026:

```text
in order: back at start after 54 steps, max |x| 64
together: max |x| over 400 steps 1187
m=+1: x at steps 0, 8, 16, 24 = [64, 91, 43, -43]
m=+0: x at steps 0, 8, 16, 24 = [64, 128, 192, 256]
m=-1: x at steps 0, 8, 16, 24 = [64, 166, 440, 1191]
m=0 coasting at rate 5 for 7 steps: x = 35
L path  : {'a': 3, 'b': 4, 'product': 12, 'swept_by_A': 0, 'swept_by_B': 12, 'corners': 0}
diagonal: {'a': 3, 'b': 4, 'product': 12, 'swept_by_A': 3, 'swept_by_B': 6, 'corners': 3}
coupling 1/225.0: first [225, 449, 671, 890, 1105], peak 3375, true 3438
coupling 1/233.5: first [225, 449, 671, 890, 1106], peak 3440, true 3438
bits a b | carry AND | clamp OR | wrap XOR
     0 0 |   0    0 |   0    0 |  0    0
     0 1 |   0    0 |   1    1 |  1    1
     1 0 |   0    0 |   1    1 |  1    1
     1 1 |   1    1 |   1    1 |  0    0
unary 5,2: AND 0b11 = min 0b11 | OR 0b11111 = max 0b11111 | XOR 0b11100 = sites 2..4, popcount 3
```

## Appendix B: sources

**In this repository**
- [Canonical prompt](../Gojo/docs/GojoSystemPrompt.md); [tension note](../Gojo/docs/GojoTension-2026-11.md)
- [EncodingC README](../Gojo.EncodingC/README.md) and [glossary](../Gojo.EncodingC/GLOSSARY.md)
- `Gojo/Core/Relation.Operations.cs`; [Equations README](../Gojo/Equations/README.md)
- [Operation Shape Catalog](../Gojo/axioms/Gojo%20Operation%20Shape%20Catalog.md)
- [Core4 Kernel Axioms](../Gojo/axioms/Core4%20Kernel%20Axioms.md), [Core4 Carrier Read And Resolution Axioms](../Gojo/axioms/Core4%20Carrier%20Read%20And%20Resolution%20Axioms.md), [Core4 Boundary Overflow Axioms](../Gojo/axioms/Core4%20Boundary%20Overflow%20Axioms.md), [Legacy Core4 idea inventory](../Gojo/docs/LegacyCore4IdeaInventory.md)
- [Complex interpretations](../Gojo/docs/GojoComplexInterpretations-2026-09.md); [primitive numbers and control](../experiments/PRIMITIVE-NUMBER-ANALYSIS.md); [race order experiment](../experiments/PrimitiveMeasure/RaceOrder.md)

**Drive folder articles** (the author's): Pin, Accumulate, and Merge;
Multiplying AI Segments; Boolean Arithmetic with Segments; Sequential Math,
Parallel Math; Numbers (Complexity Spaces); Mathematics Without Infinity;
Segment Math and Symmetry; The Mathematical Interpretation of Symmetric
Numbers; Thinking in Directed Segments; Emergent Properties; Axioms (Core 2).

**History**
- Egypt: [MacTutor, Egyptian papyri](https://mathshistory.st-andrews.ac.uk/HistTopics/Egyptian_papyri/); [Egyptian algebra](https://en.wikipedia.org/wiki/Egyptian_algebra)
- Mesopotamia: [Old Babylonian multiplication and reciprocal tables](https://www.math.stonybrook.edu/~tony/scgp/catalogue/bab-mult.html); [Joyce, sexagesimal reciprocals](https://cs.clarku.edu/~djoyce/ma105/reciprocals3600.html); [Late Babylonian tables of regular numbers](https://arxiv.org/pdf/1306.5989); [Høyrup, complex second-degree problems](https://mprl-series.mpg.de/textbooks/2/5/index.html); [Høyrup, Lengths, Widths, Surfaces](https://www.springer.com/9780387953038); [IM 67118](https://en.wikipedia.org/wiki/IM_67118); [YBC 7289](https://en.wikipedia.org/wiki/YBC_7289); [Casselman on YBC 7289](https://personal.math.ubc.ca/~cass/euclid/ybc/analysis.html); [McFarland, quarter tables](https://escholarship.org/uc/item/5n31064n)
- Greece: [Euclid VII def 15](https://mathcs.clarku.edu/~djoyce/elements/bookVII/defVII15.html); [VII.16](https://www.claymath.org/library/historical/euclid/files/elem.7.16.html); [Euclid V def 5](https://mathcs.clarku.edu/~djoyce/elements/bookV/defV5.html); [Euclid VI.23](https://mathcs.clarku.edu/~djoyce/elements/bookVI/propVI23.html); [Euclid II definitions](https://mathcs.clarku.edu/~djoyce/elements/bookII/defII.html); [Archimedes, On the Equilibrium of Planes](https://en.wikipedia.org/wiki/On_the_Equilibrium_of_Planes); [MacTutor, Archimedes on statics](https://mathshistory.st-andrews.ac.uk/Extras/Archimedes_on_statics/)
- China: [Nine Chapters](https://en.wikipedia.org/wiki/The_Nine_Chapters_on_the_Mathematical_Art); [MacTutor, Nine Chapters](https://mathshistory.st-andrews.ac.uk/HistTopics/Nine_chapters); [counting rods](https://en.wikipedia.org/wiki/Counting_rods)
- India: [Āryabhaṭa's sine table](https://en.wikipedia.org/wiki/%C4%80ryabha%E1%B9%ADa%27s_sine_table); [Joyce, Āryabhaṭa's trigonometry](https://mathcs.clarku.edu/%7Edjoyce/ma105/aryatrig.pdf); [Singh, Āryabhaṭa and the first trigonometric table](https://arxiv.org/pdf/2309.13577); [lattice multiplication](https://en.wikipedia.org/wiki/Lattice_multiplication)
- Early modern: [prosthaphaeresis](https://en.wikipedia.org/wiki/Prosthaphaeresis); [Tycho Brahe and mathematical techniques](https://www.sites.hps.cam.ac.uk/starry/tychomaths.html); [Napier's approach to logarithms](https://www.open.edu/openlearn/science-maths-technology/mathematics-statistics/john-napier/content-section-5); [MacTutor, Napier](https://mathshistory.st-andrews.ac.uk/Biographies/Napier.html); [La Géométrie (quotes)](https://en.wikiquote.org/wiki/La_G%C3%A9om%C3%A9trie); [Stanford Encyclopedia, Descartes' mathematics](https://plato.stanford.edu/archives/win2013/entries/descartes-mathematics/); [product rule, historical note](https://www.proofwiki.org/wiki/Product_Rule_for_Differentials/Historical_Note)

**Mathematics education and measurement**
- [Vergnaud, theoretical frameworks (1988)](https://www.gerard-vergnaud.org/texts/gvergnaud_1988_theoretical-frameworks_icme-6-budapest.pdf); [Sharing a unit, measuring a length](https://arxiv.org/pdf/2610.14735)
- [Measure Up, a Davydov-based curriculum](https://crdg.hawaii.edu/wp-content/uploads/measureup-2007-05b.pdf); [Wallis, multiplicative reasoning through measures](https://repository.uwtsd.ac.uk/id/eprint/3066/1/Wallis%2C%20Rachel%20%282024%29%20PhD%20Exploring%20the%20learning%20and%20teaching%20of%20multiplicative%20reasoning%20through%20measures.pdf)
- Confrey's splitting conjecture: [summary](https://cavac.at/cavacopedia/Multiplication%20and%20repeated%20addition)
- [Stevens, On the Theory of Scales of Measurement (1946)](https://static1.squarespace.com/static/61509123dbba3159acb3709f/t/615093390580ec0880021054/1461271683467/Stevens1946ScalesOfMeasurement.pdf)

**Machines and encodings**
- [Ball-and-disk integrator](https://en.wikipedia.org/wiki/Ball-and-disk_integrator); [differential analyser](https://en.wikipedia.org/wiki/Differential_analyser); [digital differential analyzer](https://en.wikipedia.org/wiki/Digital_differential_analyzer)
- [CORDIC](https://en.wikipedia.org/wiki/CORDIC); [Walther, A unified algorithm for elementary functions](https://www.scribd.com/document/352295602/A-Unified-Algorithm-for-Elementary-Functions); [Muller, CORDIC and elementary functions](https://perso.ens-lyon.fr/jean-michel.muller/00204786.pdf); [CORDIC modes (Southampton)](https://eprints.soton.ac.uk/263504/1/iee_cordic.pdf)
- [HAKMEM, item 149](https://www.cl.cam.ac.uk/~am21/hakmemc.html); [Minskytron archaeology](https://www.masswerk.at/nowgobang/2023/minskytron)
- [Alaghi, Qian and Hayes, The Promise and Challenge of Stochastic Computing](https://web.eecs.umich.edu/~jhayes/AA-WKQ-JPH_Promise-SC_IEEETCAD%2718.pdf)
- [Race logic, IEEE Micro](https://sites.cs.ucsb.edu/~sherwood/pubs/IEEEMicro-16-race.pdf); [Temporal state machines and tropical algebra](https://arxiv.org/pdf/2009.14243.pdf)
- [Avizienis, signed-digit representations (1961)](https://cr.yp.to/bib/1961/avizienis.bibtex); [Ercegovac, online arithmetic](https://web.cs.ucla.edu/~milos/DDECS2020Talk.pdf)
- [Fredkin and Toffoli, Conservative Logic](https://www.cs.princeton.edu/courses/archive/fall04/cos576/papers/fredkin_toffoli82.pdf); [billiard-ball computer](https://en.wikipedia.org/wiki/Billiard-ball_computer)
- [Chen, Doty and Soloveichik, deterministic function computation with chemical reaction networks](https://arxiv.org/abs/1204.4176v3); [Doty and Hajiaghayi, leaderless deterministic CRNs](https://arxiv.org/pdf/1304.4519)
- [Interaction nets](https://en.wikipedia.org/wiki/Interaction_nets)
- [Laws of Form](https://en.wikipedia.org/wiki/Laws_of_Form); [Kauffman, Laws of Form](https://homepages.math.uic.edu/~kauffman/Laws.pdf)

Recalled from reading and not checked for this note: Brahmagupta's rules for
fortunes and debts; Fischbein, Deri, Nello and Marino (1985); Girard (1987);
Bresenham (1965); Kelvin's tide predictor; Feynman's half-step method.
