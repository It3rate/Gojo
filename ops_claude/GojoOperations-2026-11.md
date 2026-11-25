# Operations in Gojo: what they are, where they live, how they run

Status: **exploratory discussion**, rewritten 25 November 2026. Nothing proposed
here is built. This version replaces the first draft (git `7d520953`). It folds in the
first round of answers and the two notes written in parallel on the same
question: [`ops_sol`](https://github.com/It3rate/Gojo/blob/ops_sol/Gojo/docs/GojoOperationsDiscussion.md)
and [`ops_neon`](https://github.com/It3rate/Gojo/blob/ops_neon/Gojo/docs/GojoOperations-2026-11.md).
Ideas taken from them are marked *(ops_sol)* or *(ops_neon)*. Updated after
master's tension merge (`9b805964`): pieces now built there are cited as
implemented. A [review of all three notes](GojoOperationsReview-2026-11.md)
says where they land.

Statuses follow the canonical prompt (§1): *implemented*, *documented
direction*, *exploratory* and *superseded*. Everything proposed here is
exploratory. Numeric claims are checked by the script in
[Appendix A](#appendix-a-numeric-checks).

## Contents

0. [Summary](#0-summary)
1. [Operations are not four things](#1-operations-are-not-four-things)
2. [What is combined decides how](#2-what-is-combined-decides-how)
3. [Where operations live](#3-where-operations-live)
4. [Direction: change B, make a new result, or both](#4-direction-change-b-make-a-new-result-or-both)
5. [Multiplication](#5-multiplication)
6. [Boolean reads](#6-boolean-reads)
7. [A larger structure as the target](#7-a-larger-structure-as-the-target)
8. [Rungs: numerics as the lift](#8-rungs-numerics-as-the-lift)
9. [Oscillation, growth and other stacked operations](#9-oscillation-growth-and-other-stacked-operations)
10. [How hard a value is to change](#10-how-hard-a-value-is-to-change)
11. [Compiling crystallized operations](#11-compiling-crystallized-operations)
12. [Three approaches, and how they combine](#12-three-approaches-and-how-they-combine)
13. [Conflicts and gaps in the axioms](#13-conflicts-and-gaps-in-the-axioms)
14. [Experiments that would decide](#14-experiments-that-would-decide)
15. [Open questions](#15-open-questions)
- [Appendix A: numeric checks](#appendix-a-numeric-checks)
- [Appendix B: evidence](#appendix-b-evidence)
- [Appendix C: sources](#appendix-c-sources)

## 0. Summary

1. **Operations are not four things.** Old Babylonian scribes used different
   words for joining onto something that keeps its identity and for heaping
   two things into a named result. They also distinguished tearing out from
   comparing, and they had four kinds of "multiplication". Division was a
   question rather than an operation, and the natural half of a thing was
   broken off rather than computed. The distinctions Gojo needs were kept by
   the oldest algebra we can read (§1).
2. **Every operation has three reads.** The *relation* is what holds; it is
   symmetric and reads in every direction. The *process* is what happens; it
   is directional per step and accountable for material. The *fold* is what is
   read off afterwards, with its loss (§1).
3. **What is combined decides how.** Amounts merge. A rate scales an amount,
   and rates compose. Positions differ, translate and average. Presences
   co-occur. Intensive levels such as temperatures never add, though their
   changes in one basis do (§2).
4. **There are four places for an operation.**
   - A *view* reinterprets planes and copies nothing.
   - A *connection* carries an operation with one live input: a calibration.
   - A *junction* carries two or more live inputs.
   - A *local structure* has state, carries or scope.

   Bond graphs draw the same line: a fixed transformer is a connection, and a
   modulated one is a junction (§3).
5. **Only conserving sums cross a port as takes.** A product of two live
   counts crosses as a read. Any material it has comes from a refinement, a
   supply or a new structure (§3, §4).
6. **Direction is visible in which roles a law writes.** An in-place process
   run in a workspace is a new result whose gap is the delta; committing makes
   it in place. Settling one event and advancing time are different acts:
   doubling `B` in place under a settle runs to capacity, and `B = 2B` as a
   relation narrows `B` to zero (§4).
7. **Multiplication has at least seven constructions,** and the order in
   which the operands arrived can be kept or forgotten. Which of the two
   happens is chosen by the question being asked, not by the operation (§5).
8. **Boolean reads are the bit rung of the same arithmetic.** AND is
   co-presence and also the carry of a one-bit sum. OR is a clamped sum and
   XOR a wrapped one. On unary planes, though, AND is the minimum, not the
   product (§6).
9. **A larger structure as the target is a focus with an exposed promise.**
   Scaling it can be a frame edit, a broadcast over selected lanes, a
   replication, a product with another structure, or a use of the structure
   as the multiplier (§7).
10. **Numerics is the lift between rungs.** It is a carry whose weights are
    place values and a coarse read that leaves residue. Radix is a semantic
    choice, not a packing one (§8).
11. **Oscillation, coasting and growth are one family,** varying by order and
    by the sign of feedback. At rung 0 the oscillator is the orientation
    note's generator J driven by time. The order of a step decides whether its
    orbit closes (§9).
12. **How hard a value is to change follows from where it sits** (§10).
13. **Crystallized operations compile to native units** under a contract:
    exact or declared, with overflow visible, recognized by content and
    guarded lane by lane (§11).
14. **Recommendation:** frames for meaning, flows for running, relations for
    reading backward, and tables as the crystallized form (§12).
15. **Master's tension system already implements much of this:**
    - joining, tearing out and cancelling;
    - delivering against an order;
    - heaping with a rate, and comparing;
    - thresholds held as data, and coasting at rate 1;
    - gates and turns.

    Its counting laws are the numeric stand-in until rungs arrive behind the
    same roles (§1.1, §3, §8).

### Decided in discussion

The first round of answers, 25 November 2026. They are held loosely ("not
certain of any of these") and take precedence over the proposals below.

- **Relation, process and fold are three distinct reads.**
- **Path provenance is an option, chosen by the question.** Sometimes the
  record of which operand swept which matters. Sometimes it should be hidden
  by crystallizing the product into a new direction that ignores its
  provenance.
- **Coupling rates are perturbable unless known to be constant.** A constant
  should be physically hard to perturb: kept out of the workspace, put in a
  locked table, or fixed by position.
- **Crystallized operations must stay compilable to native units.** An
  operation can always be done structurally. Once it is crystallized, CPU, GPU
  or FPGA instructions and math libraries may stand in where relevant. This
  must not drive the design, but the design must not make it impossible.

### What changed in this rewrite

- **Adopted from ops_sol:**
  - Høyrup's vocabulary of operations;
  - Descartes's `ab/u` and the BM 13901 strip;
  - the `B = 2B` trap and the plate example;
  - Shannon's polarity and three conformance cases;
  - the "dispatch in disguise" test for tables;
  - giving a result an output role first;
  - the three-readings experiment.
- **Adopted from ops_neon:**
  - the grade wall and the table wall;
  - bond graphs and signal-flow graphs;
  - views as a place;
  - the workspace/fork unification and the selected-lane broadcast;
  - the shadow energy, the first-order sibling and the backward read;
  - two compilation regimes, per-lane guards and canonical-only saving.
- **Corrected:**
  - The quarter-turn generator is the orientation note's J, now cited and
    reconciled with the oscillator (§9.1).
  - Babylonian quarter-square multiplication is speculative, not attested.
  - Radix is semantic, not a packing default.
  - Tables are small, composed or learned, never wide and filled from a
    predicate.
  - The narrative history is cut to an evidence table (Appendix B).
- **Checked:** the Babylonian vocabulary, against the first chapter of
  Høyrup's *Algebra in Cuneiform* (2017).

### Updated after the tension merge

- **Master's primitives are cited as implemented:** `Tension`, `Counts`,
  `Flows.When`, `Flows.Turn`, `Flows.Fulfill`, `Settling.Justify`,
  `Settling.ValueGap` and `Field.Room`.
- **Step order can be structure today,** through a joint transition or a turn
  bit (§9.2).
- **Views re-read bits and never compute** (§3). Facts about now are
  justified, not narrowed (§1.2).
- **Corrections from the final review:**
  - intensive changes add;
  - serialized arrivals have no corners;
  - `Convert` lowers by whole packets;
  - guards widen to groups where lanes interact;
  - `FiniteLawPlan` now lives in `Gojo.Encoding/Core/Native`.

## 1. Operations are not four things

### 1.1 What the scribes kept apart

Jens Høyrup's conformal translations render each Akkadian or Sumerian term the
same way every time it appears. Read that way, Old Babylonian calculation uses
distinct operations where modern notation writes `+`, `−`, `×` and `÷`. The
quotations below are from the first chapter of *Algebra in Cuneiform* (2017).

| Term | What it does | Gojo counterpart |
| --- | --- | --- |
| **Heap** (*kamārum*) | "to heap a and b". It can add abstract numbers, even "an area and … a length". The result has a name, "the heap", or "the things heaped" when the parts stay distinct | a relation, or a transition writing only a new result: `Counts.Sum`, with an optional rate (implemented) |
| **Join** (*waṣābum*) | "to join j to S". It is concrete and "conserves the identity of S": "S invariably remains in place, whereas, if necessary, j is moved around". The sum has no name, because "the operation creates nothing new" | an in-place process: received as pressure, then absorbed into `S` (`Tension.Receive`, `Absorb`, implemented) |
| **Tear out** (*nasāḫum*) | "from B to tear out a", the inverse of joining. It presupposes that `a` is part of `B`; what is left is "the remainder" | an in-place draw: a demand held as debt and paid from `S` (`Tension.Demand`, `Draw`, implemented) |
| **Compare** | "A over B, d goes beyond", for magnitudes where "the smaller is not part of the larger". The excess is "the going-beyond" | a two-faced gap: `Counts.Shortfall` each way round (implemented); `Settling.ValueGap` for open values; `Sides.Compare` |
| **Steps of** (*a.rá*) | the multiplication tables, abstract number by abstract number. "Going" `n` times gives `n·s`, or `A + n·s` when joined | a small table at one rung; repetition, such as `Counts.Accumulate` |
| **Raise** (*našûm*) | first used for volumes, raising a base of standard thickness 1 to a real height, then for "all determinations of a concrete magnitude by multiplication". The area of an existing rectangle is found by raising | a rate on a connection; the read of an existing construction |
| **Make hold** (*šutakūlum*) | "to construct a rectangle contained by the sides p and q", which "is no real multiplication". The texts give the area right after: "make 5 and 5 hold: 25" | pairing: new orthogonal structure (Axiom 11), then a read |
| **Repeat** (*eṣēpum*) | a "physical" doubling or `n`-doubling, with `n` at most 9, such as doubling a right triangle into a rectangle | replication of a structure |
| **Division** | "no separate operation … only a problem type": "what should I raise to d in order to get P?" Look up igi `d`, then raise. igi `n` is "detached": one of `n` equal parts | a relation read backward; a reciprocal table; splitting |
| **Break** (*ḫepûm*) | to bisect into the "natural" half (*bāmtum*, "moiety"), such as the radius of a diameter. Other halves are found by raising to 30′ | a structural midpoint, as against a calibrated ratio |
| **Confrontation** (*mitḫartum*) | a square seen as a configuration, its value "identified with the length of the side". "By Q, s is equal" gives the root. There were tables of squares and of "equals" | a raised grade read back at its side |

Four contrasts carry straight into Gojo:

- **Join versus heap is process versus relation.** One changes something in
  place; the other produces a named result and leaves the parts readable.
- **Make hold versus raise is construction versus read.** One builds new
  structure; the other measures a structure that already exists.
- **Steps of versus raise is a table versus a rate.** One combines abstract
  numbers; the other applies a coefficient to a concrete magnitude.
- **Break versus raise to 30′ is a landmark versus a calibration.**

### 1.2 Three reads of every operation

| Read | What it is | In EncodingC |
| --- | --- | --- |
| **Relation** | what holds among the roles. Symmetric; read in any direction; never moves anything | a relation law narrowing open supports |
| **Process** | what happens, step by step. Directional; it says which role is read and which written, what is conserved, and when | a transition law, with accounting promises |
| **Fold** | what is read off, at which resolution, with which loss | the role that is read, a coarse relation, a crystallized carrier |

Earlier iterations point the same way:

- Euclid VII, definition 15, defines multiplication asymmetrically: the
  latter number "is added as many times as there are units in the former".
  That the order does not matter is a theorem (VII.16), not part of the
  definition.
- Core 2 (axiom F10) reads `x * t` as a state and a transform, and its
  pinning event `A * B @ P` has a recessive host and a dominant applied
  operand.
- The older Numerics library paired `Calculate`, `CalculateInverse` and
  `CalculateInPlace` *(ops_neon)*.
- The current kernel separates projection, a read, from live output and
  settlement, which are mutation.

**Give a result an output role first** *(ops_sol)*. The output can be left
open, read through a view, materialized independently, or bound to a receiver.
A new carrier for every calculation would make live operations expensive and
blur observation with state. On master, a computed result is written by a law,
and only when it changes. The host-computed gap views of an earlier tension
version were replaced by counting laws (tension note, §11).

**Facts about now are justified, not narrowed.** Narrowing only rules out,
which suits facts that stay true. A possibility about now, such as "the product
exceeds the threshold", can stop holding and come back. `Settling.Justify`
(implemented) holds it while its reasons hold, so it reopens when they return.
This is truth maintenance (Doyle, 1979).

## 2. What is combined decides how

| Kind | Example | Combines with its own kind by |
| --- | --- | --- |
| Presence | a bit, an event, an occupied site | co-presence (AND), clamped union (OR), parity (XOR) |
| Amount (extensive) | units, charge, apples | merging: a conserving move |
| Position | a site against a landmark | difference gives an extent; an extent translates it; weights average it |
| Rate (intensive) | price, speed, gear ratio, valve opening | composition: rates multiply |
| Frame | an origin and a unit; a rung | composition through a shared middle frame |

Across kinds:
- **A rate on an amount gives an amount** on the rate's target carrier. The
  rate is read and the amount moves.
- **Two amounts on different carriers give pairs:** an area or a set of
  combinations.
- **A position minus a position gives an extent,** and a position plus an
  extent gives a position.
- **Intensive plus intensive is not defined.** Two cups at 50 and 30 degrees
  do not make 80. Combine the extensives they are ratios of, then read the
  ratio again: a weighted average. Changes in one basis do add: warming by 5
  and then by 3 is warming by 8 *(ops_sol)*. Only levels do not.

**Relative and absolute.** The Drive article's "position to position for
addition, from zero for multiplication" is Stevens's distinction between
interval and ratio scales. Differences need no zero; ratios do.

**The grade wall** *(ops_neon)*. Viète's law of homogeneity (1591) let only
like grades add, and made multiplication climb grades. Descartes broke it by
choosing a unit segment and constructing the fourth proportional, `1 : a ::
b : x`. A same-grade product therefore hides a fourth participant, the unit.
His product is `BE = BD·BC/AB`, and `ab` and `ab/u` remain different evidence
even when `u = 1` *(ops_sol)*. The Babylonian "projection" (*waṣītum*,
something that "sticks out") does the reverse in BM 13901, problem 1. A side
given a width of 1 becomes area that can be joined to a square *(ops_sol)*.

**What children have to learn.** Research on how children come to multiply
separates the same situations:
- Vergnaud separates the *isomorphism of measures* (a rate between two measure
  spaces) from the *product of measures* (area, combinations).
- Davydov and Elkonin teach multiplication as measuring with an intermediate
  unit, which is a change of unit.
- Confrey treats splitting, making several equal versions of an original at
  once, as a primitive independent of counting. The Babylonian igi "detaches"
  one of `n` equal parts in the same way.

## 3. Where operations live

| Place | What belongs there | In EncodingC | Physical criterion |
| --- | --- | --- | --- |
| **View** | a re-reading of existing planes, copying nothing and computing nothing | `Flipped`: NOT, `Field.Room`, `Odv.Reoriented` | another reading of the same participation *(ops_neon)*. Anything that calculates is a law (tension note, §11) |
| **Connection** | one live input: a rate, a shift, a coarse read | `Move`, `Convert(take, give)` | a bond-graph transformer; a scalar gain in a signal-flow graph |
| **Junction** | two or more live inputs and no state of its own | a row of a law over three or more roles: AND, `Counts.Sum`, a times table, the passage law (source, gate, target), any law gated by `Flows.When` | a *modulated* transformer; a bond-graph 0- or 1-junction |
| **Local structure** | state, carries, several sites, a schedule | ladders, multipliers, oscillators, spatial Boolean settlement | a mechanism with its own ports that settles on its own |

- **Bond graphs** (Paynter, 1961) are the crispest physical criterion *(ops_neon)*.
  - A transformer has a fixed modulus, `e₁ = m·e₂` and `f₂ = m·f₁`. It is a
    two-port connection that conserves power, which is exactly `Convert`.
  - A modulated transformer reads its modulus from a third, information port:
    variable multiplication.
  - A 0-junction shares one effort and sums the flows; a 1-junction shares one
    flow and sums the efforts. Junctions are where variables meet.
  - Which side sets effort and which sets flow (causality) is assigned after
    the relation is written. That is the process read of an acausal relation.
- **Signal-flow graphs** build every linear relation from copying, discarding,
  adding, zero and scalar gains, with their mirror images (Bonchi, Sobociński
  and Zanasi) *(ops_neon)*. Copying is a read fanned out, adding is a merge,
  and a gain is a connection. A product of two signals lies outside that
  linear core: it is where junctions begin.
- **Ports carry only reads and conserving takes** (implemented in EncodingC). A
  take must sit in a preserved sum with positive weights, which covers moves,
  branches, merges, constant-rate conversions and carries. A product of two
  live counts has no preserved sum, so it crosses as a read and happens inside
  the receiver. Chemical reaction networks corroborate this: rate-independent
  reactions stably compute sums, constant multiples, minimums and comparisons,
  but not the product of two inputs.
- **Partial application.** A junction with one input fixed is a connection,
  like a gear of fixed ratio. A connection whose rate is read live from
  elsewhere is a junction, like a valve set by another structure.
- **The kernel's rule** (implemented) that a point output sums and a frame
  output multiplies is a *fold* rule. It says whether the result stays in its
  grade. It does not say which operand is the rate, or where material comes
  from.
- **Naming.** The kernel says "operation port" and EncodingC "row of a
  wiring". *Junction* would serve both.

## 4. Direction: change B, make a new result, or both

**The law shows it** (implemented). A relation narrows and writes nothing. A
transition that writes `B` changes `B` in place. It may read `A` under an
`Unchanged` promise (control) or drain `A` under a preserved sum (material). A
transition that writes only `C` makes a new result.

**A workspace turns one into the other** *(ops_neon)*. An in-place process run
in a workspace writes only the workspace. Its gap is the delta: a new result.
Committing writes the change back, refused if the source moved: in place.

**Material scaled in place must come from somewhere.** A law that turns `B`
into `3B` cannot conserve `B` alone. There are four legitimate readings:
1. **Refinement:** the same stuff counted in a finer unit. `Convert(1, 3)`
   turns one coarse unit into three fine ones, and the place-value sum is
   preserved.
2. **Supply:** the extra units are drawn from a reservoir. A shortfall stays as
   debt: `Tension.Demand` holds it, and `Draw` pays it when there is value
   (implemented). Change from outside enters as pressure or debt and is never
   written into the value (Axiom 5).
3. **An informational update:** a coordinate, reference or supposition is
   rewritten inside the structure. No material is claimed, and nothing is
   written through a foreign port *(ops_neon)*.
4. **New structure:** pairing builds an area, which is not more of the same
   material.

**Settling is not advancing time** *(ops_sol)*. Write "double `B`" three ways
in EncodingC:
- **As a relation** over one position read twice, `B = 2B` admits only
  `B = 0`. It narrows to zero, or contradicts if zero is ruled out.
- **As a transition run by `Settle`,** which steps until nothing changes, it
  never goes quiet while `B` is nonzero. It doubles until the table admits
  nothing more: "times two" becomes "as large as capacity allows".
- **As a transition run once per `Step`,** it doubles once per tick:
  5, 10, 20, 40.

Feedback needs a captured before-state, an independent baseline, or wired
recurrence, and the time boundary must be explicit. A request for 10 into a
capacity of 8 leaves a shortfall, which is evidence of what was not accepted.

**The roles stay asymmetric even when the total commutes.** Partitive division
recovers the size of a share from the number of shares. Quotative division
recovers the number of shares from their size. Babylonian division was a
question about which value to raise, answered through a table of detached
parts.

**Recommendation.** Never use a flag. By default an operation makes a new read
(Axiom 8). Change something in place only as an explicit process on the
target, with its time boundary. Use the relation when an operation must be
read backward.

## 5. Multiplication

### 5.1 Seven constructions

| Construction | Term | What lives in structure | Material | Example |
| --- | --- | --- | --- | --- |
| Steps of | *a.rá* | a small relation at one rung, reused with carries | none: a read | 3 steps of 4 are 12 |
| Raise | *našûm* | a connection with a rate; for a gain, also a reference | the target keeps its kind | `(3 m / 1 m) × 4 m = 12 m`; with reference 2 m, 6 m *(ops_sol)* |
| Make hold | *šutakūlum* | an orthogonal product; its measure read later | new structure | 3 m and 4 m hold 12 m² |
| Repeat | *eṣēpum* | `n` placed instances with identities | each copy needs supply, or is a read of one design | three placed copies of a plate |
| Coast | "going" `n` times | a rate read, a supply drained, a clock | conserving; shortfall is draw | 5 a tick for 7 ticks is 35 |
| Change of unit | — | a move between rungs | conserving: the same stuff counted finer | 3 bags of 4 become 12 apples |
| Fourth proportional | — | a projection through a unit frame | none: a read | Descartes's `BD·BC/AB` |

These agree as counts and disagree about units, identity, material and which
value controls *(ops_sol)*. On one shared partition, a product of predicates is
AND per cell, and the overlap of counts `a` and `b` can be anything from 0 to
the smaller of them. Pairing predicates from independent axes counts tuples
instead, giving `a·b`.

### 5.2 The path is part of the product

When `a` and `b` grow a unit at a time, the product grows by
`Δ(ab) = aΔb + bΔa + ΔaΔb`: a row, a column and a corner. Each new unit of one
operand pairs with every unit of the other already there. The total does not
depend on the path, but the split does (Appendix A, check 5). With `a = 3` and
`b = 4`:
- **On the L path** (all of `A`, then all of `B`), all 12 cells are swept by
  `B`.
- **On the diagonal** (both together three times, then `B`), 3 cells are swept
  by `A`, 6 by `B`, and 3 are corners.
- **Serialized** (each paired arrival split, `A` before `B`, then the last `B`),
  3 are swept by `A`, 9 by `B`, and none are corners *(ops_sol)*. Corners appear
  only when arrivals coincide.

Some consequences:
- **Growing together builds squares by gnomons, 1, 3, 5.** BM 13901, problem 1,
  completes a square by joining exactly this corner. The square's area and its
  side are heaped to 45′; the side's unit-width strip is broken in half and
  moved around the square; the missing corner, 15′, is joined to make 1; and
  tearing out 30′ leaves the side, 30′ (check 8).
- **In a fine read the corners vanish,** as `ε² = 0` does in dual numbers. The
  two sweeps become the squeegee article's two triangles, `½·3·4 + ½·4·3`,
  which is Leibniz's rule. Counting both full sweeps would double the area;
  the triangle split is one valid way to account for it *(ops_sol)*.
- **The question chooses whether the record is kept** (decided). It can be
  kept as three route tallies (swept by `A`, swept by `B`, corners), as
  `Flows.Fulfill` counts receipts by route without a record per unit
  (implemented). It can also be recorded while running (a wiring that
  records), replayed later over the selected cone (the consolidation note's
  witness lane), or never read if no demand selects it.
- **Crystallization can drop the record on purpose.** The product becomes a
  carrier of its own, a new direction whose ticks no longer say which side
  swept which. The kernel's `TryCrystallizeFill` already "records magnitude and
  face, not construction endpoints". The prompt (§6) adds that such a loss must
  be exposed: it "cannot also claim exact reconstruction of forgotten detail".

### 5.3 Wide products must factor

**The table wall** *(ops_neon)*. A table has one bit per combination of role
values:
- a product of 0..3 and 0..3 is `4·4·10 = 160` bits;
- a product of 0..15 and 0..15 is `16·16·226 = 57,856` bits.

Its 226 outputs also exceed EncodingC's limit of 64 values in one primitive
read (`OnePrimitiveReadTellsApartAtMost64Values`). Tables beyond `Law.MaxBits`
are refused before they are built, and each gate added by `Flows.When`
quadruples a table (implemented). So a wide product must be factored:
- **Lattice:** every digit meets every digit in a cell, and the cell's place is
  `i + j`. Diagonals of equal place are summed with carries. Long
  multiplication multiplies digits while it adds rung indices. The Treviso
  arithmetic of 1478 lays out `934 × 314` in several such ways *(ops_sol)*.
- **Doubling:** Egyptian multiplication doubles (rungs of factor 2), lets the
  multiplier's bits select which doublings count, and adds. That is rungs plus
  Boolean selection plus addition *(ops_neon)*. Rhind problem 79 computes
  `2801 × 7` from the rows for 1, 2 and 4 *(ops_sol)*.
- **Coasting:** clocked accumulation of a rate.
- **Quarter squares:** `ab = ((a + b)² − (a − b)²)/4`, with one table of
  squares. It is a real technique, tabled from the 1690s and used in analog
  multipliers in the 1950s. Babylonian use is often claimed but speculative.
  Tables of squares existed, and their use for multiplication is not attested.

**The dispatch-in-disguise test** *(ops_sol)*. A small table filled once,
compared by content and checked by its accounting is structure: a scribe's
standard table. A wide table whose meaning lives only in the predicate that
filled it is an operator dispatch in disguise. A table must therefore agree
with the construction it stands for, such as counting distinct pairs. It must
preserve repeated-sample identity and keep reverse ambiguity open. Wide ones
are composed from small ones, or learned from observed processes.

## 6. Boolean reads

- **At the bit rung, Boolean reads are arithmetic with boundary accounting.**
  - AND is co-presence: the product of two presences, and the carry of their
    one-bit sum.
  - OR is the clamped one-bit sum (Axiom 7's stop boundary).
  - XOR is the wrapped one (the modulo boundary).
  - A half adder is the wrap plus the carry (check 7).
  - Spencer-Brown's law of calling (a call made again is the call) is the
    clamp; his law of crossing (a crossing made again is not the crossing) is
    the wrap.
- **On unary planes, AND is the minimum.** EncodingC counts are thermometer
  codes, with bit `j` meaning "more than `j`". On their planes, AND is the
  minimum and OR the maximum. XOR is the span of sites between the two values,
  whose population is their distance (check 7). Read as a count, that span is
  broken; read as sites, it is a segment between two landmarks. The product of
  two counts is not a planewise act.
- **Partition first, then select.**
  - Two occupants and a scope give four regions: both, A only, B only and
    neither. The 16 functions are choices of which regions feed the output.
  - Kept with the unselected regions as visible residue, the selection
    conserves, as in Fredkin and Toffoli's conservative logic.
  - With three or more inputs, name the selected evidence explicitly: "at
    least two", "all", "odd parity" and "exactly one" differ *(ops_sol)*.
- **NOT is room within a declared scope.** It is the `Flipped` room view, and
  never a complement over an unbounded universe.
- **Four zeros stay apart** *(ops_neon)*: an undetectable magnitude
  (`V = 0`), an unselected site, an empty support (a contradiction), and an
  absent presence.
- **A gate is an action** *(ops_sol)*. A relation answers which cases are
  admissible. Driving flow through one needs a transition, a receiver and a
  rule for what is blocked. A shared gate can be read many times, while one
  conserved unit goes to one route. The passage law multiplies a flow by a
  bit. `Flows.When` (implemented) makes any law wait on several gate bits: an
  AND of conditions on a flow. Because each gate quadruples the table, many
  conditions are combined by a small law first, and the flow waits on its one
  bit.
- **Polarity is a chosen read.** In Shannon's 1938 relay paper, 1 meant an
  *open* circuit (hindrance), so series was a sum and parallel a product. The
  conductance reading is the dual *(ops_sol)*. §9.7's "one is presence" is a
  choice to record, as the prompt says.
- **Weighted Boolean reads** are stochastic computing. A value is a fraction
  of ones, and AND of independent streams multiplies.

## 7. A larger structure as the target

The target is a focus and an exposed promise, such as "the outline and hole of
this plate, about this datum" *(ops_sol)*. Shared sites stay shared and
independent equal sites stay distinct. Parts outside the focus are carried
with their context.

| Meaning | What changes | Cost |
| --- | --- | --- |
| **Frame edit** | retarget the frame's unit landmark (one `TickRef`) and every read follows; retargeting the origin translates | constant |
| **Selected-lane broadcast** | a scaling law over the lanes an attention plane selects, bound only to measure planes. Where scaling meets a capacity that did not scale, the conflict shows as local `T+` *(ops_neon)* | per lane |
| **Replication** | `n` instances with placements and identities | per instance |
| **Product with a structure** | a sweep, every part with every part: an extrusion, a lattice, an outer product | per pair |
| **The structure as multiplier** | its count gates the repetition of another structure (Euclid VII definition 15), as a frieze repeats a motif | per repeat |
| **A field of rates** | each part read through its own calibration: a map, not a frame | per part |

A frame edit makes distributivity fall out: `k(a + b) = ka + kb`, because the
parts share the frame. A plate with a rectangular outline, a hole and a
mounting datum gives concrete cases *(ops_sol)*:

| Request | Consequence |
| --- | --- |
| Repeat three times | three instances with placements; the design is shared by reference |
| Stretch threefold along one axis | the hole reads as an ellipse; a demanded circular passage stays a visible mismatch |
| Dilate about the datum, `p' = o + 3(p − o)` | lengths ×3, areas ×9, mass ×9 at constant thickness, or ×27 if thickness scales too |
| Product with a thickness | a volume; a through-hole needs a correspondence carried into the product |

A negative uniform gain is a half-turn plus a stretch, while reversing one
axis reflects. A zero gain collapses the selected extent and leaves the
inverse open. A candidate change is held in a workspace and written back only
through a checked correspondence, with the stale check.

## 8. Rungs: numerics as the lift

**On the order of work,** the three notes converge:
- Fix the kinds and places of operations now, at rung 0, with a few tiny
  numeric checks alongside *(ops_sol)*.
- Variable multiplication waits for rungs, because of the grade wall and the
  table wall *(ops_neon)*.
- Multiplication is itself the relation between rungs. Core4 said so: "the
  same mechanism as resolution".

**The counting laws are the stand-in** (implemented on master). `Counts`
writes sums (with an optional rate), shortfalls, tallies, thresholds and
accumulations as small tables. The tension note calls them "the emulation of
the first numeric step" (§11 there), and states the contract this note's lift
needs: "With magnitudes in planes, each becomes arithmetic on a magnitude, the
tables stop growing with capacity, and every wiring that reads them stays as it
is." Rungs, and compiled stand-ins (§11), arrive behind the same roles.
`Counts.Rank` already makes the comparison a ladder needs: the higher tier
decides whenever it changed.

**The lift rule:**
- **Carry.** A carry is `Convert(k, 1)` from a fine count to a coarse one. Its
  preserved sum's weights are the place values (implemented,
  `WhatDoesNotFitIsCarriedUpARung`).
- **Coarse read.** Reading a coarser rung divides by the base and leaves the
  finer rung as residue.
- **Moving between rungs.** Moving a whole number up a rung multiplies by the
  base, and running the carry downward is a conserving change of unit.
- **Rung index.** The rung index is a logarithm; a lattice cell's place is the
  sum of its row's and column's.

**Rung codes** (consolidation note §5.8, a proposal):
- R0, detectable: one bit.
- R1, unary: thermometer counts.
- R2, place value: a crystallization of R1 "when the required queries and
  updates make it worthwhile".
- R3, rational and affine: the kernel.

The maps between rungs form a Galois connection, so a refinement never
contradicts a valid coarse read.

**Three conformance cases** *(ops_sol)*:
- **Add, then read coarse; or read coarse, then add.** 17 + 28 read in whole
  tens sees `1 + 2 = 3` tens, with residues 7 and 8. Recombining the residues
  crosses another ten, giving 4 tens and 5.
- **A product needs its factors' resolution.** `(10 + 7)(20 + 8)` at whole
  tens is 200. With the refinements connected, the partial products
  80, 140 and 56 make 476.
- **Quantizing is observable.** In the finite-read fixture with capacity 0..15,
  `(14 + 3) − 3` reads 12 after intermediate clipping, while `14 + (3 − 3)`
  reads 14.

**Radix is semantic.** Base 60 makes every regular number's reciprocal
terminate (2, 3 and 5 divide it), which is why the igi table exists. Whether a
quotient is exact, whether a reciprocal fits the radix, and whether a table has
the row are three separate questions *(ops_sol)*. Power-of-two radices pack
into machine words (§11), but that is a reason to crystallize into them, not a
reason to think in them.

**Floating digits.** Eight steps of `0;07,30` make `1`. Eight steps of `7,30`
make `1,00,00` (check 8). The table supplies the digits, and the application
supplies the scale *(ops_sol)*. That is an uncalibrated connection (§4.6 of the
prompt).

**Denominators and evidence.** The Numbers article reads 200/500 as more
precise than 2/5 because it comes from 100 reviews. A denominator carries
resolution only while it stays connected to the sampling that produced it; on
its own, 2/4 is just 1/2 *(ops_sol)*. That agrees with the prompt (§5.2: "a
nominal series is not measurement resolution").

## 9. Oscillation, growth and other stacked operations

### 9.1 At rung 0: the generator, driven by time

Read position against the landmark and velocity against rest, each with
`Sides`. As the oscillator runs, the pair passes through eight states (check
4). The axis states are the two events, *turned back* (velocity at rest) and
*crossed* (position at the landmark). The quadrant states between them are the
movements.

```text
turned back   moving    crossed    moving    turned back   moving    crossed    moving
 (+x, 0)  ->  (+, -)  -> (0, -v) -> (-, -) ->  (-x, 0)  ->  (-, +) -> (0, +v) -> (+, +) -> (+x, 0)
 (F, +)                  (I, -)                (F, -)                 (I, +)
```

The orientation note (§5) already defines the generator **J** as a
state-dependent alternation of two involutions: a face flip from `F` and a
reorientation from `I`. So `J² = Negate`, `J⁴` is the identity, and no
involution alone produces it. That note leaves J deferred. Read with position
on the forward face `F` and velocity on the opposed face `I`, the oscillator's
event states run `(F, +) → (I, −) → (F, −) → (I, +)`. That is J's cycle in the
conjugate direction, and it runs forward if velocity is read on the other
orientation. Two quarters negate: `(+x, 0)` becomes `(−x, 0)`.

The rung-0 oscillator is J driven by time, with the quadrants as the open
states between its events. Without "at" resolved, it reads as four quadrants.
The ops_neon note's six-state listing resolves only the crossings.

### 9.2 With magnitude: kick, then drift

```text
v <- v - k·x      the restoring pull
x <- x + v        drift by the new velocity
```

In integers, each read goes through a coarser rung, `v <- v - coarse(x)` and
`x <- x + coarse(v)`, with `coarse` a shift by `s` rungs. For the rescaled
velocity `ε·v`, that is the same step with `k = ε²` and `ε = 2^-s`.

- **Order carries meaning.**
  - In declared order the step has determinant 1, and the integer orbit closes
    exactly: it returns after 54 steps and never exceeds its starting 64.
  - Stepped together, both reading the old state, it has determinant `1 + k`
    and spirals out to 1187 in 400 steps (check 1).
  - This is symplectic versus explicit Euler, and Minsky's circle algorithm
    (HAKMEM 149) in integers.
- **The order can be structure today,** in two ways.
  - One joint transition computes both from the before-state *(ops_sol)*.
  - Kick and drift each wait on a turn bit, one on the bit and the other on its
    flipped view, so exactly one runs per engine step (`Flows.Turn`,
    implemented). Under a step taken together, this reproduces the in-order
    orbit step for step, at two engine steps per oscillator step (check 9).

  Either way, the order is saved with the machine.
- **The shadow energy** `H = ½(v² + kx² − kxv)` is conserved exactly *(ops_neon)*
  (check 2). The angle per step is `2·asin(√k/2)`, so `k` calibrates the
  period.
- **The material account.** The velocity the mover gains comes from the
  landmark, which takes the opposite momentum: a conserving move between two
  counts at a rate read from the distance. Position integrates velocity by a
  shear. A shear conserves no sum, but it is exactly reversible.
- **The fineness of the pull's read shapes the wave.** A pull read only as a
  side gives parabolic arcs; a pull proportional to the distance gives the
  sine *(ops_sol)*.

### 9.3 One family: order and sign

| | `m = +1`, restoring | `m = 0`, neutral | `m = −1`, reinforcing |
| --- | --- | --- | --- |
| **First order**, `x −= m·coarse(x)` | relaxation toward the landmark: equal ratios take equal times, which is Napier's logarithm. 1024, 614, 370, 225; halvings at steps 11, 22 and 34 | holds | compounding, the exponential: 1024, 1659, 2692, 4366 |
| **Second order**, kick then drift | oscillation, sine and cosine: 64, 86, 38, −41 | coasting, rate × time, multiplication: 64, 128, 192, 256 | runaway, hyperbolic cosine and sine: 64, 174, 469, 1267 |

The values are `x` at steps 0, 8, 16 and 24 (check 3).

- **Napier (1614) is the first row's left cell.** One point moves at constant
  speed. The other slows in proportion to the distance it has left, so the
  first point's distance is the logarithm *(ops_neon)*. The Complexity Spaces
  article's "times two … slowly used up as it approaches equilibrium" is the
  same picture.
- **CORDIC's `m` is the second row.** Its circular, linear and hyperbolic modes
  give sine and cosine, products and quotients, and exponentials, logarithms
  and roots.
- **`m` is a number, not an enum.** The unit's square is `−m`. Complex numbers
  (`i² = −1`) are `m = +1`, dual numbers (`ε² = 0`) are `m = 0`, and
  split-complex numbers (`j² = +1`) are `m = −1`. The Drive article that keeps
  `ac + bd` unfolded until folding has already separated the quadrant from
  the sign it folds with.
- **The family interpolates.** One tick of coasting adds; `n` ticks multiply;
  a negative `m` compounds; a positive `m` oscillates.

### 9.4 Calibration and reading backward

- **The coupling is a calibration.** Āryabhaṭa (499) built his sine table by
  making each second difference proportional to minus the sine reached so
  far. Run literally, a coupling of 1/225 peaks at 3375 instead of 3438. The
  exact discrete coupling, `2 − 2cos 3°45′ ≈ 1/233.5`, peaks at 3440 (check 6).
  A coupling is perturbable while it is being fitted, and locked once it is
  (§10).
- **The oscillator can be read backward** *(ops_neon)*. Wire the step law as a
  relation across a plan's moments, as the planner fixture does. The same
  structure then infers phase or amplitude from partial observations.
- **An exact contour turn** *(ops_sol)*: `(x, u) → ((3x + 4u)/5, (−4x + 3u)/5)`
  keeps `x² + u²`. It takes `(5, 0)` to `(3, −4)` and then to `(−7/5, −24/5)`
  (check 8). Whole ticks cannot hold the second step, so a refinement, an
  approximation or a declared loss belongs in the transition's witness.

### 9.5 Settling a remaining tension

| Method | The tension | How it is lowered |
| --- | --- | --- |
| False position (Egypt, Babylon) | the error of a convenient guess | scale the guess by goal over result |
| Excess and deficit (*Nine Chapters*, 7) | an overshoot and an undershoot | weight the two residues crosswise |
| Iterated averaging (Heron) | `x` and `N/x` disagree | replace `x` with their mean |
| Steelyard (Archimedes' law of the lever) | an unbalanced torque | slide the counterpoise until level |
| CORDIC, non-restoring division, successive approximation | remaining angle, remainder, or residual | at each rung, step in the direction of the remainder's sign |

Each settles a visible residual with a local rule. CORDIC and its relatives
read only a sign at each rung: coarse primitives, refined by a ladder, as the
prompt's §9.6 asks.

## 10. How hard a value is to change

Every mechanism below exists in EncodingC (implemented). Read together, they
form a ladder of stiffness.

| Where the value sits | What can change it | What a change costs | Physical picture |
| --- | --- | --- | --- |
| A count in a store, read by reference | any law of its structure, and any workspace that selects it | one step of a law | a valve setting; a coupling being learned |
| Outside every workspace's selection, or in an ignore layer | the structure's own laws, but no supposition | the owner must act | an instrument in use |
| Behind another structure's port | only the owner's laws; readers read, and a take must be a move through the single taker | the owner, or a move its accounting allows | a standard held elsewhere |
| A position no law writes, with `Unchanged` promises where it is read | nothing, short of rewiring | a structural edit, checked when laws are made | a measured constant |
| A constant plane | nothing in place; workspaces read it and cannot change it | moving positions to another store | a whole frame's orientation |
| Inside a law's table | nothing: tables are fixed when made | a new law | a gear cut in metal; a place value |

- **Moving down the ladder is crystallization,** and moving up is thawing: a
  fork rewires a row to a law that reads the rate from a store of its own.
  Committing a fork's rewiring is not built yet.
- **Examples:**
  - A ladder's base is structural: changing it reinterprets every value, so it
    belongs in the carry law.
  - The coupling of §9.4 is fitted, then locked.
  - The sign `m` is perturbable in a workspace that explores which concept
    fits, and locked in a crystallized oscillator.
  - `Counts.AtLeast` (implemented on master) keeps its threshold as a count "so
    a law, a learner or a host can move it".
- **Graded stiffness is open.** The prompt (§5.1) names "resistance to change"
  and leaves its law open. A graded read could count how many structures read
  a value, or how much tension a change would raise in them.

## 11. Compiling crystallized operations

**Where this stands.** The prompt allows a compiled form "if it can be traced
back to the Gojo surface" (§8.1). It also says such forms are "subordinate to"
the structure (§9.1), and that "an optimization must not shape the concept"
(§9.6). `FiniteLawPlan` is the existing pattern (implemented). It was first
written in the QualitativeExecution experiment and is now promoted to
`Gojo.Encoding/Core/Native`:
- any graph edit invalidates it (`IsCurrent`), and a stale plan returns
  unresolved evidence;
- a compute-only read allocates no witness;
- open or oversized surfaces are refused;
- the consolidation note measured about 196×, partly from dropping per-read
  witnesses.

**The life of an operation** *(ops_neon)*. While fluid, it is canonical
tables, open supports and learning counts, and it is the reference oracle.
Once crystallized, it is sealed behind ports and runs a guarded native plan.
Saving always writes the canonical structure, never a compiled artifact.

**Two regimes** *(ops_neon)*:
- **Bit-exact.** The native form gives the same bits at every exposed port,
  including carries, remainders, overflow into `T+`, and evidence.
- **Declared finite read.** A math-library sine stands in for an oscillator
  only under a contract that states calibration, rounding and tolerance. The
  tolerance can be expressed as integer ticks at a rung, and the read is marked
  lossy. The integer orbit of §9.2 is not `sin`. A CORDIC unit running the same
  iteration is bit-exact; `Math.Sin` is not.

**The contract:**
1. **Only crystallized structure is replaced.** Its law is a locked table and
   its rates are constants (§10).
2. **Exact on the declared domain,** or a declared finite read.
3. **Overflow stays visible.** Use widening or checked arithmetic, or prove from
   the declared capacities that the native width cannot overflow; bounded
   numbers make that proof easy. Overflow that does happen becomes `T+` or a
   carry out, never a silent wrap (§8.1).
4. **Residue, open values and contradictions survive.** Division keeps its
   remainder, and a zero divisor gives an open result, not an exception.
5. **Recognition is by content, not by name.** `SameTable` compares kind, role
   shapes and table bits (implemented). Small tables can be verified
   exhaustively at compile time, and a sealed subgraph is matched by structure
   *(ops_neon)*. A matching table still needs the right bindings and accounted
   effects *(ops_sol)*.
6. **Guards work lane by lane.** Only lanes that are
   `Live & Attended & SettledValidCodes & ~Aliased & ~Contested` take the fast
   path; the rest run canonically and write their evidence *(ops_neon)*. The
   guard widens to a group where lanes share capacity or a contested writer
   *(ops_sol)*. Any edit invalidates the plan.
7. **Witnesses are produced on demand.** A question that asks for provenance
   replays the canonical path over its cone. That provenance is chosen by the
   question is what keeps the fast path fast.
8. **Parity tests are the guarantee,** covering the overflow, residue, open and
   stale cases.

| Crystallized form | Native stand-in | Regime |
| --- | --- | --- |
| A small law (Boolean, passage, ODV addition) | bitwise words; `pshufb`/`vqtbl1q`; FPGA LUT (the passage law is one LUT6) | exact |
| Unary add, remove, compare | shifts, masks, `popcnt` | exact |
| A radix ladder: add or multiply | widening `ADD`/`MUL`, carry out to `T+` | exact |
| `Convert(take, give)` | one whole packet per firing: a compare and two adds. A bulk read takes `min(⌊s/take⌋, ⌊room/give⌋)` packets, never `⌊s·give/take⌋` *(ops_sol, check 10)* | exact |
| A selected-lane broadcast | masked vector operations | exact |
| A product of structures | outer product; GPU tensor units | exact in integers |
| The oscillator | integer kick and drift, vectorized; or a CORDIC core | exact |
| The oscillator read as a waveform | a math-library sine | declared |
| Narrowing supports | bitwise planes | exact |

**What keeps the door open,** all else being equal:
- Seal operation interiors behind ports.
- Keep a binary ladder reachable by crystallization, choosing radix per
  structure.
- Keep couplings as rung offsets where possible, since powers of two become
  shifts.
- Keep arithmetic laws small and separate from bookkeeping.
- Keep settled fast paths separate from open supports.
- Keep planes contiguous.
- Keep compiled plans derived and disposable.
- Declare the step order (§9.2).

Material and information should stay apart where they are apart; gates belong
to both.

## 12. Three approaches, and how they combine

### A. Junction tables: the scribe

**Idea.** Small tables over coarse roles, used at every rung with carries, as
in lattice multiplication. Relations give backward reads. Wide operations are
composed from small tables or learned from processes, and must pass the
dispatch-in-disguise test (§5.3).

**Strengths.**
- It is built on machinery that already works: `RelationLaw`, narrowing,
  `LearnedTable`, carries, and on master the counting laws, which are this
  approach at rung 1, written one way.
- It is exact.
- It reads backward: division, the open zero, contradiction.
- It is a lookup on any substrate.

**Weaknesses.**
- It is static: no time and no momentum.
- Table size multiplies with role sizes.
- A symmetric table hides which operand is the rate.
- Narrowing is sound but incomplete: coupled loops such as `x + y = 5`,
  `x·y = 6` need suppositions *(ops_neon)*.

### B. Coupled flows: the machine

**Idea.** Accumulators coupled by rates, stepped in a declared order, settle a
remaining tension.

| Operation | As a flow |
| --- | --- |
| Add | merging |
| Multiply | coasting, or pairing arrivals |
| Divide | quotition (steps until the gap closes) or partition (a round-robin split) |
| Oscillate, compound, take a logarithm | the family of §9.3 |
| Boolean | one-shot routing |

**Strengths.**
- It is physical.
- Intermediate states are meaningful numbers: partial products and remaining
  tension.
- Roles are explicit.
- It fits tension, which master now implements as `Tension`, with debt for
  any shortfall. It gives sine for free.

**Weaknesses.**
- It takes time: `n` steps, or `log n` with rungs.
- Step order and coupling calibration both carry meaning, so both must be
  structure.
- Its inverse is another process, not a reading of the same one.
- Flow can confuse observing with spending, unless reads and takes stay
  visible *(ops_sol)*.

### C. Frames and rungs: the geometer

**Idea.** Numbers are ladders of partitions, and operations are frame edits or
reads through composed frames.
- Adding concatenates extents at a shared site, and translating moves an
  origin.
- Multiplying changes unit: it composes projections through a shared middle
  frame, Descartes's fourth proportional.
- Dividing projects backward with residue.
- A power repeats a composition, and a logarithm counts rungs.

**Strengths.**
- The kernel already has projection with residue, unit shapes,
  `ResolutionOf` and `SubdivisionOf`.
- Scaling a structure is a constant-time retarget.
- Meaning lives in the frame.

**Weaknesses.**
- Projection is observation, so material needs an explicit route.
- Motion needs the contour construction.
- A product of two live values needs frames as values, like a slide rule.
- A new graph per scalar read would waste live structure *(ops_sol)*.

### Side by side

| | A: junction tables | B: coupled flows | C: frames and rungs |
| --- | --- | --- | --- |
| What an operation is | a small relation per rung, with carries | coupled accumulators settling a remainder | a frame edit, or a read through composed frames |
| Direction | from wiring | built in | the direction of projection |
| Material | none | supplied, or refined | none until an import |
| Inverse | the same table read backward | another process | the projection read backward |
| Intermediate states | supports | partial products, remaining tension | partial frames, residue |
| Path provenance | only if recorded | natural | not native |
| Sine and cosine | iterating over A | native | via a contour |
| Compiles to | LUTs and recognized instructions | vectorized integer loops, CORDIC | multiply-add, shifts, `DivRem` |
| Main risk | dispatch in disguise | step order and calibration | dynamics bolted on |

### How they combine

**C says what multiplication means:** a relation between frames or rungs.
**B says how it runs.** **A gives the backward reads, and is what a repeated B
crystallizes into:** "useful history may crystallize into an algorithm" (the
prompt's §6). The four places of §3 decide where each piece sits. The
relation, process and fold of §1 are the reads every piece offers.

A suggested order for work, at rung 0 in EncodingC:
1. The oscillator family with saved step order (§9).
2. Multiplication as coasting and as pairing, with provenance on demand (§5).
3. Tables crystallized from observed processes, checked as relations (§5.3).
4. A guarded native plan for one crystallized ladder (§11).

## 13. Conflicts and gaps in the axioms

Each gap is listed with the smallest clarification that would resolve it.

1. **The step kind carries meaning but is not saved.** It decides whether an
   orbit closes (§9.2) and what a compiled loop means. Within one engine it can
   already be structure, through a joint transition or `Flows.Turn` (§9.2).
   Between engines it cannot. *Clarify:* update order is structure saved with
   the machine, and `Step` versus `Settle` is part of an in-place process's
   contract.
2. **Where product material comes from is unstated** (prompt §4.4, Axiom 5).
   *Clarify:* "A product is a read unless a refinement or supply route is
   wired. A product that is demanded and not supplied is draw tension."
3. **Intensive and extensive are not distinguished.** Axiom 10's compatible
   addition is stated in units, but intensive levels of one unit still do not
   add. *Clarify:* addition is defined on amounts, extents and changes.
   Intensive levels combine through the extensives they are ratios of.
4. **The prompt's §9.7 "product is presence of both" holds at the bit rung
   only.** On unary planes AND is the minimum (§6). *Clarify* its scope.
5. **Generator J is deferred, and the oscillator gives it a first use.**
   *Proposal:* take J off the deferred list in EncodingC, with the rung-0
   oscillator (§9.1) as its fixture.
6. **The kernel's output-grade rule is a fold rule,** not the process, the
   roles or the supply (§3).
7. **There is no single word for a junction** (§3).
8. **The tension note needs no new kind.** The "times two used up" is goal
   tension in a logarithmic basis, and shortfall is material deficit.
9. **Reversibility has no name.** Shears conserve information without
   conserving a sum (§9.2). If a law can promise reversibility, the prompt
   needs a word for it.
10. **Stiffness has no structural account** beyond mutable and immutable
    (§10). *Clarify:* how hard a value is to change is a read of where it sits.
11. **The rule for compiled forms could be sharper.** *Clarify:* the contract of
    §11.
12. **Radix is semantic.** No axiom should prefer a radix for packing reasons
    (§8).
13. **An article and the prompt meet on denominators.** A denominator is
    resolution only while connected to its sampling (§8; prompt §5.2).
14. **Counting laws read one way.** `Counts` laws are functional transitions.
    Backward reads (subtraction, division, the open zero) need relations.
    *Decide* whether each counting law gets a relation twin, or whether
    relations are added only where a question needs one. *Lean:* neither as
    stated. `Sum`, `Shortfall`, `Tally` and `AtLeast` are built by one helper
    that reads inputs and writes one result, and `Rank` has the same shape:
    each is a relation read forward. Derive them from that relation, so
    backward reads come from the same table and nothing is kept twice. Read
    backward, `Sum` subtracts, and with `a` at 0 it answers the scribes'
    division question: what times the weight gives the total? `Accumulate`
    and `Streak` read their own last value, so they stay processes.
15. **Narrowing and justification apply to different facts.** Narrowing suits
    facts that stay true; `Settling.Justify` suits possibilities about now. The
    prompt could say which reads use which.

## 14. Experiments that would decide

All are at rung 0 in EncodingC unless noted.

| Id | Build | Expected | Decides |
| --- | --- | --- | --- |
| X1 | The 3 × 4 comparison *(ops_sol)*: a relation over `a, b ∈ 0..4`, `c ∈ 0..16`; pair schedules kept as route tallies; packets at 3 a tick from a supply of 12 into a capacity of 10, with `Tension` debt | the relation has 425 bits and 25 tuples, gives `(3, 4)` and `(4, 3)` for 12, leaves `b` open at `a = c = 0`, and contradicts `a = 2, c = 5`; the splits are `(0, 12, 0)`, `(3, 6, 3)` and `(3, 9, 0)`; deliveries run 0, 3, 6, 9, 9 with 3 left (checks 11 and 12) | whether structure explains relation, pairing and process before the helper's name is read |
| X2 | A times relation over `(a, b, c)` | divides; `b` open when `a = c = 0`; contradiction when `a = 0` and `c ≠ 0` | A's backward reads |
| X3 | A two-rung lattice from one small table, against one wide relation | equal products; bits and the 64-value limit counted | factoring (§5.3) |
| X4 | Lattice, doubling and quarter squares measured side by side | bits, steps, backward reads | which factoring to prefer *(ops_neon)* |
| X5 | A coasting multiplier: a rate read, a supply, a clock | target `r·d`, supply drained, shortfall as `T−` | B's material account |
| X6 | Arrival pairing: recording off, recording on, replayed | the split appears only when asked; recorded equals replayed | provenance chosen by the question |
| X7 | "Double `B`" as a relation, under `Settle`, and per `Step` | zero; capacity; 5, 10, 20, 40 | settling versus time (§4) |
| X8 | The rung-0 phase law: J driven by Sides events | eight states; two quarters negate; four return | §9.1; un-deferring J |
| X9 | Kick and drift with both step kinds, and alternated by `Flows.Turn` | in order closes with constant shadow energy; together spirals; turn-gated together equals in order (check 9) | step order as structure |
| X10 | The family of §9.3: first and second order, `m = +1, 0, −1` | relaxation, hold, growth; oscillation, coasting, runaway | one mechanism |
| X11 | Scaling a structure by its unit `TickRef` (kernel), and the plate in GojoCAD | every read scales; ratios hold; the hole becomes an ellipse under one-axis stretch | C's frame edit; §7 |
| X12 | A change of unit as a carry run downward | 3 coarse become 12 fine, place-value sum preserved | conserving multiplication |
| X13 | Clamp, wrap and carry at the bit rung; four-region routing with residue | OR, XOR, AND; 16 selections conserving | Boolean as arithmetic |
| X14 | One rate at each level of the stiffness ladder | each refuses or allows a change as §10 says | stiffness by position |
| X15 | A crystallized times table behind a lane-guarded native plan | parity, overflow to `T+`, fallback on edit or open lanes | the compile contract |

## 15. Open questions

1. Should step order, and `Step` versus `Settle`, be saved with the machine?
2. Should intensive versus extensive become an axiom-level distinction?
3. Should provenance on demand be recorded or replayed? Recording costs planes
   while running; replay costs a rerun when asked.
4. Is stiffness positional only, or also graded by support?
5. Should a supply that runs short leave a partial scaled result plus `T−`, or
   hold the whole step, as `Convert` does for one quantum *(ops_neon)*?
6. Which radix should each structure use? Base 60 for exact reciprocals, or
   a power of two once crystallized?
7. Is a narrow bridge test between an EncodingC ladder and the kernel's
   projection worth adding now *(ops_neon)*?
8. Which fixture first? The proposal is X1, then X8 to X10, then X5 to X7.
9. Should each static counting law be derived from one relation, so backward
   reads come from the same table (§13, item 14)?
10. Should turn-gated alternation be the standard way to state update order
    inside an engine (§9.2)?

The [review](GojoOperationsReview-2026-11.md) gathers these with the decisions
that follow from comparing the three notes.

## Appendix A: numeric checks

The script checks every numeric claim in this note. Run it with
`python3 -I checks.py`. Everything is integer or exact-rational arithmetic
except the two Āryabhaṭa tables.

```python
import math
from fractions import Fraction as F

def coarse(v, s):                      # read v through s coarser binary rungs
    return -((-v) >> s) if v < 0 else v >> s

def second_order(x, v, s, steps, in_order=True, m=1):
    """Kick, then drift: v -= m*coarse(x); x += coarse(v).
    In order: x reads the new v. Together: both read the old state."""
    path = [(x, v)]
    for _ in range(steps):
        if in_order:
            v = v - m * coarse(x, s); x = x + coarse(v, s)
        else:
            x, v = x + coarse(v, s), v - m * coarse(x, s)
        path.append((x, v))
    return path

# 1. Step order: declared order closes the orbit; a step taken together spirals.
p = second_order(64, 0, 3, 400)
back = next(i for i, q in enumerate(p[1:], 1) if q == p[0])
print("1. in order: back at start after", back, "steps, max |x|", max(abs(q[0]) for q in p))
p = second_order(64, 0, 3, 400, in_order=False)
print("   together: max |x| over 400 steps", max(abs(q[0]) for q in p))

# 2. Exact shadow energy of kick-drift, H = (v^2 + k x^2 - k x v)/2 (from ops_neon).
k = F(1, 8); x, v = F(5), F(0); H = lambda x, v: (v*v + k*x*x - k*x*v) / 2
h0 = H(x, v); same = True
for _ in range(1000):
    v -= k * x; x += v; same = same and H(x, v) == h0
print("2. shadow energy unchanged over 1000 exact steps:", same,
      "| per-step angle 2*asin(sqrt(k)/2) =", round(2 * math.asin(math.sqrt(1/8) / 2), 4),
      "vs acos(1-k/2) =", round(math.acos(1 - 1/16), 4))

# 3. Order and sign: first order x -= m*coarse(x); second order as above.
def first_order(x, s, steps, m):
    out = [x]
    for _ in range(steps):
        x = x - m * coarse(x, s); out.append(x)
    return out
for m in (1, 0, -1):
    f1 = first_order(1024, 4, 24, m); f2 = second_order(64, 64, 3, 24, True, m)
    print(f"3. m={m:+d}: first order x at 0,8,16,24 = {[f1[i] for i in (0, 8, 16, 24)]};"
          f" second order x = {[f2[i][0] for i in (0, 8, 16, 24)]}")
d = first_order(1024, 4, 40, 1)
halves = [next(i for i, q in enumerate(d) if q <= t) for t in (512, 256, 128)]
print("   first-order decay reaches 512, 256, 128 at steps", halves, "(equal ratios, near-equal times)")

# 4. Rung-0 phase: (sign x, sign v) with the axes between quadrants.
def sign(n): return (n > 0) - (n < 0)
p = second_order(64, 0, 3, back)
seen = []
for xv in p:
    st = (sign(xv[0]), sign(xv[1]))
    if not seen or seen[-1] != st: seen.append(st)
print("4. phases visited in one period:", seen)

# 5. Product by pairing arrivals; the split records the path.
def pairing(arrivals):
    a = b = by_a = by_b = corners = 0
    for da, db in arrivals:
        if da and db: by_a += b; by_b += a; corners += 1
        elif da:      by_a += b
        elif db:      by_b += a
        a += da; b += db
    return dict(product=by_a + by_b + corners, swept_by_A=by_a, swept_by_B=by_b, corners=corners)
print("5. L path  :", pairing([(1, 0)] * 3 + [(0, 1)] * 4))
print("   diagonal:", pairing([(1, 1)] * 3 + [(0, 1)]))

# 6. Aryabhata: second difference = -(sine so far)/coupling, radius 3438.
for coupling in (225.0, 1 / (2 - 2 * math.cos(math.radians(3.75)))):
    dd, s, t = 225.0, 0.0, []
    for _ in range(24):
        s += dd; t.append(round(s)); dd -= s / coupling
    print(f"6. coupling 1/{coupling:.1f}: first {t[:5]}, peak {max(t)}, true 3438")

# 7. Bit rung and unary planes.
print("7. bits: carry(a+b)=a*b:", all((a + b) // 2 == a * b for a in (0, 1) for b in (0, 1)),
      "| clamp(a+b)=a|b:", all(min(a + b, 1) == a | b for a in (0, 1) for b in (0, 1)),
      "| wrap(a+b)=a^b:", all((a + b) % 2 == a ^ b for a in (0, 1) for b in (0, 1)))
th = lambda n: (1 << n) - 1           # bit j set means "more than j"
ok = all(th(a) & th(b) == th(min(a, b)) and th(a) | th(b) == th(max(a, b))
         and bin(th(a) ^ th(b)).count("1") == abs(a - b) for a in range(9) for b in range(9))
print("   unary planes 0..8: AND is min, OR is max, XOR spans |a-b| sites:", ok)

# 8. Exact contour turn (from ops_sol) and two Babylonian readings.
x, u = F(5), F(0); pts = []
for _ in range(2):
    x, u = (3*x + 4*u) / 5, (-4*x + 3*u) / 5; pts.append((x, u))
print("8. 3-4-5 turn:", [(str(a), str(b)) for a, b in pts], "keeps x^2+u^2 =", pts[-1][0]**2 + pts[-1][1]**2)
print("   8 steps of 0;07,30 =", 8 * (F(7, 60) + F(30, 3600)), "| 8 steps of 7,30 =", 8 * (7*60 + 30), "= 1,00,00")
s = F(1, 2)
print("   BM 13901 #1: side 30' gives s^2 + s =", s*s + s, "= 45'; completing the corner gives", (s + F(1, 2))**2)

# 9. Update order as structure. Every row reads the state before the step (a
#    step taken together); kick waits on turn 0, drift on turn 1, so one runs
#    per engine step, and Flows.Turn flips the bit while both have work.
def turn_gated(x, v, s, engine_steps):
    turn, path = 0, [(x, v)]
    for _ in range(engine_steps):
        nx, nv = x, v
        if turn == 0: nv = v - coarse(x, s)
        else:         nx = x + coarse(v, s)
        x, v, turn = nx, nv, 1 - turn
        if turn == 0: path.append((x, v))
    return path
def joint(x, v, s, steps):                       # one row, both from the before-state
    path = [(x, v)]
    for _ in range(steps):
        nv = v - coarse(x, s); x, v = x + coarse(nv, s), nv; path.append((x, v))
    return path
t, o, j = turn_gated(64, 0, 3, 2 * back), second_order(64, 0, 3, back), joint(64, 0, 3, back)
print("9. turn-gated together == in order == joint transition:", t == o == j, "| closes:", t[-1] == (64, 0))

# 10. Convert(take=3, give=2) moves one whole packet per firing (from ops_sol).
def convert_run(source, target, room, take=3, give=2):
    while source >= take and target + give <= room:
        source -= take; target += give
    return source, target
print("10. Convert 3->2 from 8: room 3 ->", convert_run(8, 0, 3), "| room 99 ->", convert_run(8, 0, 99),
      "| bulk 8*2//3 =", 8 * 2 // 3)

# 11. The 3 x 4 relation (from ops_sol): a, b in 0..4, c in 0..16, c = ab.
rel = [(a, b, a * b) for a in range(5) for b in range(5)]
print("11. bits", 5 * 5 * 17, "| admitted", len(rel), "| c=12:", [(a, b) for a, b, c in rel if c == 12],
      "| a=0,c=0 leaves b", sorted({b for a, b, c in rel if a == 0 and c == 0}),
      "| a=2,c=5 leaves", sorted({b for a, b, c in rel if a == 2 and c == 5}),
      "| one sample:", sorted({a * a for a in range(5)}))

# 12. Packets into a capped receiver (from ops_sol): 3 a tick, supply 12, capacity 10.
supply, recv, trace = 12, 0, [0]
for _ in range(4):
    if supply >= 3 and recv + 3 <= 10: supply -= 3; recv += 3
    trace.append(recv)
print("12. receiver", trace, "| left at supply", supply, "| still", supply + recv,
      "| serialized split:", pairing([(1, 0), (0, 1)] * 3 + [(0, 1)]))

# 13. Corner share in the fine limit: 3N and 4N arriving together where they can.
N = 1000
print("13. corner share at x1000:", pairing([(1, 1)] * (3 * N) + [(0, 1)] * N)["corners"] / (12 * N * N))
```

Output, 25 November 2026 (checks 9 to 13 added after the tension merge):

```text
1. in order: back at start after 54 steps, max |x| 64
   together: max |x| over 400 steps 1187
2. shadow energy unchanged over 1000 exact steps: True | per-step angle 2*asin(sqrt(k)/2) = 0.3554 vs acos(1-k/2) = 0.3554
3. m=+1: first order x at 0,8,16,24 = [1024, 614, 370, 225]; second order x = [64, 86, 38, -41]
3. m=+0: first order x at 0,8,16,24 = [1024, 1024, 1024, 1024]; second order x = [64, 128, 192, 256]
3. m=-1: first order x at 0,8,16,24 = [1024, 1659, 2692, 4366]; second order x = [64, 174, 469, 1267]
   first-order decay reaches 512, 256, 128 at steps [11, 22, 34] (equal ratios, near-equal times)
4. phases visited in one period: [(1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0)]
5. L path  : {'product': 12, 'swept_by_A': 0, 'swept_by_B': 12, 'corners': 0}
   diagonal: {'product': 12, 'swept_by_A': 3, 'swept_by_B': 6, 'corners': 3}
6. coupling 1/225.0: first [225, 449, 671, 890, 1105], peak 3375, true 3438
6. coupling 1/233.5: first [225, 449, 671, 890, 1106], peak 3440, true 3438
7. bits: carry(a+b)=a*b: True | clamp(a+b)=a|b: True | wrap(a+b)=a^b: True
   unary planes 0..8: AND is min, OR is max, XOR spans |a-b| sites: True
8. 3-4-5 turn: [('3', '-4'), ('-7/5', '-24/5')] keeps x^2+u^2 = 25
   8 steps of 0;07,30 = 1 | 8 steps of 7,30 = 3600 = 1,00,00
   BM 13901 #1: side 30' gives s^2 + s = 3/4 = 45'; completing the corner gives 1
9. turn-gated together == in order == joint transition: True | closes: True
10. Convert 3->2 from 8: room 3 -> (5, 2) | room 99 -> (2, 4) | bulk 8*2//3 = 5
11. bits 425 | admitted 25 | c=12: [(3, 4), (4, 3)] | a=0,c=0 leaves b [0, 1, 2, 3, 4] | a=2,c=5 leaves [] | one sample: [0, 1, 4, 9, 16]
12. receiver [0, 3, 6, 9, 9] | left at supply 3 | still 12 | serialized split: {'product': 12, 'swept_by_A': 3, 'swept_by_B': 9, 'corners': 0}
13. corner share at x1000: 0.00025
```

## Appendix B: evidence

| Source | Mechanism | Lesson for Gojo |
| --- | --- | --- |
| Old Babylonian vocabulary (Høyrup) | join, heap, tear out, compare; steps of, raise, make hold, repeat; division as a question; break | operations are distinct acts with distinct roles (§1) |
| Babylonian floating digits | digits with the scale left to the application | an uncalibrated connection (§8) |
| igi tables | division by a reciprocal; regular numbers; "detaching" one of `n` parts | a relation read backward; splitting; radix as semantics |
| BM 13901 #1 | a side given a unit width becomes area; the corner completes the square | lifting a grade by a unit; the corner of the product |
| Egyptian doubling; Rhind 79 | doublings selected by bits and added | rungs plus Boolean selection plus addition |
| False position (Rhind 24–29) | guess, read the error, scale | solving by lowering tension |
| Euclid VII def 15 and VII.16 | multiplication defined by roles; symmetry proved | roles survive commutativity |
| Eudoxus (Euclid V def 5) | equal ratios by equimultiples and comparison | counting and comparing suffice for proportion |
| Euclid II and VI.23 | products of lines are rectangles; ratios compound through a middle term | grade-raising products; relation composition |
| Gnomon (Euclid II def 2) | odd numbers build squares | the incremental product |
| Application of areas (Euclid I.44, VI.28–29) | exact fit, falling short, exceeding *(ops_neon)* | two-faced tension in geometry |
| Archimedes, lever and steelyard | balance at reciprocal distances | equilibrium as computation |
| *Nine Chapters*: red and black rods; excess and deficit | two signed faces; two trials combined crosswise | keep `T+` and `T−` apart; interpolate residues |
| Brahmagupta | rules for fortunes, debts and zero | signed participation |
| Āryabhaṭa | second difference proportional to minus the sine | sine as coupled accumulation; the coupling is a calibration |
| Lattice; Treviso 1478; Napier's bones | digit products by place, diagonals with carries | the product of two ladders; rung indices add |
| Prosthaphaeresis; Napier 1614; slide rule | multiply by adding angles or logarithms | multiplication is addition one rung up |
| Viète 1591; Descartes 1637 | homogeneity; the unit and the fourth proportional | the hidden unit; `ab` against `ab/u` |
| Leibniz 1675 | `d(xy) = x dy + y dx` | each operand sweeps along the other |
| Shannon 1938 | relay circuits with 1 as open | polarity is a chosen read |
| Stevens 1946 | interval and ratio scales | differences need no zero; ratios do |
| Vergnaud; Davydov; Confrey; Fischbein | rate on amount, product of measures, change of unit, splitting | `×` names several structures |
| Bond graphs (Paynter 1961) | transformers, modulated transformers, junctions *(ops_neon)* | constants on connections, variables at junctions |
| Signal-flow graphs (Bonchi, Sobociński, Zanasi) | copy, add, discard, zero, gain *(ops_neon)* | the linear core; products begin junctions |
| Ball-and-disk integrator; DDA | a position modulates a flow, accumulated; overflow as output rate | multiplication as setting × flow over time |
| CORDIC (Volder 1959; Walther 1971) | one iteration with `m ∈ {1, 0, −1}` | one mechanism for sine, product and exponential |
| Minsky (HAKMEM 149) | update with the new value | step order decides whether the orbit closes |
| Stochastic computing; race logic | AND is a product, or a maximum | the encoding decides what an act means |
| Signed digits; online arithmetic | local carries; most significant digit first | coarse-first arithmetic on ODV-like digits |
| Conservative logic (Fredkin, Toffoli) | invertible gates conserving ones | Boolean as conserving routing |
| Chemical reaction networks | rate-independent reactions compute semilinear functions | a product of two counts needs sequencing |
| Laws of Form | calling and crossing | clamp and wrap |
| Propagators (Radul, Sussman) | cells of partial information *(ops_neon)* | the computer-science cousin of supports |

## Appendix C: sources

**Parallel notes and repository**
- [ops_sol: Where operations live in Gojo](https://github.com/It3rate/Gojo/blob/ops_sol/Gojo/docs/GojoOperationsDiscussion.md); [ops_neon: Operations, from physical relations to compiled hardware](https://github.com/It3rate/Gojo/blob/ops_neon/Gojo/docs/GojoOperations-2026-11.md)
- [Canonical prompt](../Gojo/docs/GojoSystemPrompt.md); [orientation, definition and use](../Gojo/docs/GojoOrientationDefinitionAndUse-2026-11.md) (§5, the generator); [tension note](../Gojo/docs/GojoTension-2026-11.md); [consolidation note](../Gojo/docs/GojoEncodingConsolidation-2026-11.md) (§5.8, §5.12, §7, K7); [finite-read fixture](../Gojo/docs/GojoFiniteReadFixture.md); [complex interpretations](../Gojo/docs/GojoComplexInterpretations-2026-09.md)
- [EncodingC README](../Gojo.EncodingC/README.md) and [glossary](../Gojo.EncodingC/GLOSSARY.md); `Gojo.EncodingC/Tests/LawTests.cs` (`SameTable`, the 64-value limit, Boolean bytes, the add relation); `SelfRunningTests.cs` (the carry ladder); `Laws/Accounting.cs` (`Unchanged`)
- `Gojo/Core/Relation.Operations.cs` (the output-grade rule; `TryCrystallizeFill`); [Equations README](../Gojo/Equations/README.md); [Operation Shape Catalog](../Gojo/axioms/Gojo%20Operation%20Shape%20Catalog.md); [Core4 Kernel Axioms](../Gojo/axioms/Core4%20Kernel%20Axioms.md); [Core4 Carrier Read And Resolution Axioms](../Gojo/axioms/Core4%20Carrier%20Read%20And%20Resolution%20Axioms.md); [Core4 Boundary Overflow Axioms](../Gojo/axioms/Core4%20Boundary%20Overflow%20Axioms.md)
- `Gojo.Encoding/Core/Native/FiniteLawPlan.cs`, first written as `experiments/QualitativeExecution/FiniteLawPlan.cs` with `FiniteLawBatchPlan.cs`
- Master's tension merge (`9b805964`): `Gojo.EncodingC/Flow/Tension.cs`; `Laws/Counts.cs`; `Flow/Flows.cs` (`When`, `Turn`, `Fulfill`); `Laws/Settling.cs` (`Justify`, `Latch`, `SameWhen`, `ValueGap`); `Substrate/Field.cs` (`Room`); `Laws/Law.cs` (`MaxBits`, `SameTable`); tests `TensionTests`, `CountsTests` and `RouteChoiceTests`; the [tension note](../Gojo/docs/GojoTension-2026-11.md), §8 and §11
- [The review of all three notes](GojoOperationsReview-2026-11.md)
- Drive folder articles: Pin, Accumulate, and Merge; Multiplying AI Segments; Boolean Arithmetic with Segments; Sequential Math, Parallel Math; Numbers (Complexity Spaces); Mathematics Without Infinity; Segment Math and Symmetry; The Mathematical Interpretation of Symmetric Numbers; Thinking in Directed Segments; Emergent Properties; Axioms (Core 2)

**Checked for this note**
- [Høyrup, *Algebra in Cuneiform*, chapter 1](https://www.mprl-series.mpg.de/media/textbooks/2/2/textbooks2ch2.pdf) (the vocabulary of §1.1, quoted from the text); [Høyrup, complex second-degree problems](https://mprl-series.mpg.de/textbooks/2/5/index.html)
- [Euclid VII def 15](https://mathcs.clarku.edu/~djoyce/elements/bookVII/defVII15.html); [VII.16](https://www.claymath.org/library/historical/euclid/files/elem.7.16.html); [Euclid V def 5](https://mathcs.clarku.edu/~djoyce/elements/bookV/defV5.html); [Euclid VI.23](https://mathcs.clarku.edu/~djoyce/elements/bookVI/propVI23.html); [Euclid II definitions](https://mathcs.clarku.edu/~djoyce/elements/bookII/defII.html); [Archimedes, On the Equilibrium of Planes](https://en.wikipedia.org/wiki/On_the_Equilibrium_of_Planes)
- [MacTutor, Egyptian papyri](https://mathshistory.st-andrews.ac.uk/HistTopics/Egyptian_papyri/); [Old Babylonian multiplication and reciprocal tables](https://www.math.stonybrook.edu/~tony/scgp/catalogue/bab-mult.html); [YBC 7289](https://en.wikipedia.org/wiki/YBC_7289); [McFarland, quarter tables](https://escholarship.org/uc/item/5n31064n)
- [Nine Chapters](https://en.wikipedia.org/wiki/The_Nine_Chapters_on_the_Mathematical_Art); [counting rods](https://en.wikipedia.org/wiki/Counting_rods); [Āryabhaṭa's sine table](https://en.wikipedia.org/wiki/%C4%80ryabha%E1%B9%ADa%27s_sine_table); [Singh, Āryabhaṭa and the first trigonometric table](https://arxiv.org/pdf/2309.13577); [lattice multiplication](https://en.wikipedia.org/wiki/Lattice_multiplication)
- [prosthaphaeresis](https://en.wikipedia.org/wiki/Prosthaphaeresis); [Napier's approach to logarithms](https://www.open.edu/openlearn/science-maths-technology/mathematics-statistics/john-napier/content-section-5); [La Géométrie (quotes)](https://en.wikiquote.org/wiki/La_G%C3%A9om%C3%A9trie); [product rule, historical note](https://www.proofwiki.org/wiki/Product_Rule_for_Differentials/Historical_Note)
- [Vergnaud, theoretical frameworks (1988)](https://www.gerard-vergnaud.org/texts/gvergnaud_1988_theoretical-frameworks_icme-6-budapest.pdf); [Measure Up (Davydov-based)](https://crdg.hawaii.edu/wp-content/uploads/measureup-2007-05b.pdf); [Stevens (1946)](https://static1.squarespace.com/static/61509123dbba3159acb3709f/t/615093390580ec0880021054/1461271683467/Stevens1946ScalesOfMeasurement.pdf)
- [Ball-and-disk integrator](https://en.wikipedia.org/wiki/Ball-and-disk_integrator); [digital differential analyzer](https://en.wikipedia.org/wiki/Digital_differential_analyzer); [CORDIC](https://en.wikipedia.org/wiki/CORDIC); [CORDIC modes](https://eprints.soton.ac.uk/263504/1/iee_cordic.pdf); [HAKMEM 149](https://www.cl.cam.ac.uk/~am21/hakmemc.html)
- [Stochastic computing survey](https://web.eecs.umich.edu/~jhayes/AA-WKQ-JPH_Promise-SC_IEEETCAD%2718.pdf); [race logic](https://sites.cs.ucsb.edu/~sherwood/pubs/IEEEMicro-16-race.pdf); [Avizienis (1961)](https://cr.yp.to/bib/1961/avizienis.bibtex); [Ercegovac, online arithmetic](https://web.cs.ucla.edu/~milos/DDECS2020Talk.pdf); [Fredkin and Toffoli, Conservative Logic](https://www.cs.princeton.edu/courses/archive/fall04/cos576/papers/fredkin_toffoli82.pdf); [Chen, Doty and Soloveichik](https://arxiv.org/abs/1204.4176v3); [Laws of Form](https://en.wikipedia.org/wiki/Laws_of_Form)

**Through the parallel notes, not rechecked here:** CDLI P493017, Proust on
Nippur, Rhind problem 79, the Treviso arithmetic and Shannon (1938) via
ops_sol; Paynter (1961), Bonchi, Sobociński and Zanasi, Viète (1591),
Bombelli, Grassmann, Radul and Sussman, and Hairer, Lubich and Wanner via
ops_neon.

**Recalled from reading:** Brahmagupta; Fischbein, Deri, Nello and Marino
(1985); Confrey (1994); Bresenham (1965); Minsky's circle algorithm in context;
Hölzle, Chambers and Ungar (1992) on deoptimization; Doyle (1979) on truth
maintenance, as cited in `Settling.Justify`.
