# Gojo operations synthesis

Status: **round 16, pass 1 of 3**, 26 November 2026. Branch `ops_synthesize`,
started from master `9b805964`.

This is the shared specification for operations, numbers and recognition in
`Gojo.EncodingC`. It is written in three passes on one branch, each building
on the last, before anything is merged into master:

| Pass | Author | What it brings | Done when |
| --- | --- | --- | --- |
| 1 | Claude | The core library: operations as relations and bindings, polynomial relations, numbers as rungs, recognition | This spec is written, and its contracts are pinned by tests |
| 2 | Sol | Conformance and contracts: scope, units, uncertainty, accounting, save and load, the native bound fix | Sol's acceptance checks (§6) pass, and run against any execution path |
| 3 | Neon | The compiled backend as an option, plus the partition and uncertainty extensions the core lacks | The backend passes every pass-2 gate, and old master saves load and step identically |

The canonical prompt (`Gojo/docs/GojoSystemPrompt.md`) outranks this document.
Where the two disagree, the prompt wins, and the disagreement is recorded in
§9.

Round names differ by branch. This branch counts the user's rounds 1 to 16.
Sol's "Operations Round 3" is round 14 here, and its "Round 4" is round 15.
Neon's "Round 2" is round 13, its "Round 2 review" round 14, and its "Round 3"
round 15.

## 1. How to use this document

- **Read §3 before changing code.** Each contract there names the test that
  pins it. Changing a contract means changing its test and saying why in §11.
- **Your instructions are §6 (Sol) or §7 (Neon).** They are ordered. The
  numbered items are the work; the suggestions under them are not
  requirements.
- **Append your notes to §11** when your pass ends: what you changed, what you
  refused and why, and what you found wrong in the earlier passes.
- **Disagree in the open.** If a pass-1 decision is wrong, change it, add a
  test that shows why, and record it in §11. Do not quietly route around it.

## 2. What pass 1 contains

Production code, about 1,100 lines with comments, all in `Gojo.EncodingC`:

| File | What it is |
| --- | --- |
| `Laws/Operations.cs` | `Balance`, `Product`, and `Bind`: a relation run as a transition law, with read, write and free roles, a numeric cost per free role, and an optional blocked bit |
| `Laws/Polynomial.cs` | A relation as integers, Σ weight · Π value^exponent = 0, in canonical form, with its table, its value and its description length in bits |
| `Laws/Numbers.cs` | `Numbers`, which holds digits, columns, `DigitScale`, `SameShare`, `Rescale`, `Group`, `Refine` and `Admit`. `Ladder`, which holds mixed-radix rungs, `Restrict`, `Measure`, `Observe`, `Reopen`, `Interval`, `Add`, `Scale` and `Multiply` |
| `Laws/Recognition.cs` | `Relations`, `Fits`, `Survivors`, `Probe`, `Predict`, `Residue`, `Witnesses`, `Compare` and `Restore` |
| `Laws/Counts.cs` | `Sum`, `Shortfall`, `Tally` and `Accumulate` are rebuilt as bindings of a balance, with the same bits as before |
| `Lens/Lens.cs` | `Describe(Polynomial)`, the text form for people |

The changes stay small:
- No core substrate, flow, workspace or transport file is changed.
- There are no enums and no string fields, and nothing uses a real number or
  `System.Math`. The existing reflection guards check all of this.

Tests: seven new files with 48 tests: `OperationTests`,
`OperationPrimitiveTests`, `BitRungTests`, `SettlingVersusTimeTests`,
`NumberTests`, `NumberResolutionTests` and `RecognitionTests`. All 211 master
tests pass unchanged, for 259 in total.

**Left out on purpose.** These stay on `claude/youthful-newton-wh29co` as
evidence:
- `Flows.Sweep` and the product-as-process tests;
- the oscillator, coasting-multiplier, stiffness and configuration-potential
  experiments.

They change a core file (`Flows`), or test ideas rather than contracts. The
branch's design notes are in `ops_claude/` there:
- `GojoOperations-2026-11.md`;
- `GojoOperationsProposal-2026-11.md`;
- `GojoOperationsRound14-2026-11.md` and `GojoOperationsRound15-2026-11.md`.

**Fixed while assembling this pass.** Sol's round-4 review found the first two;
the other two came out of the work:
- **Stale output after re-observation.** Reopening one ladder left the outputs
  derived from it narrowed. `Ladder.Reopen(network, position)` now reopens a
  whole network.
- **Restore accepting a different law.** `Restore` accepted an equation that
  kept the law's accounting but changed its table. It now refuses one.
- **Carry and padding as observations.** The finest carry in and `Multiply`'s
  zero padding were held as supports, so reopening a network opened them.
  They are now constant planes.
- **The no-overflow fact.** In `Multiply`, the fact that a running sum fits
  was a restriction on a support. It is now a constant, so reopening cannot
  loosen it.

## 3. The specification

### 3.1 Operations: a relation and a binding

**What it is.** A relation says what holds among some amounts and moves
nothing. A binding says how one step uses the relation: each relation role
reads a law role before the step, writes one after it, or is free.

**How one relation becomes several operations:**
- **Heap,** the balance a + b − total = 0, is a sum into a new count, an
  accumulation over the count it read, and, read backward, subtraction.
- **The gap,** have + short − want − over = 0, is shortfall with the excess
  as residue, and excess with the roles swapped.
- **The product** a · b − c = 0 pairs two counts forward. Narrowed, it
  divides; with both inputs bound to one sample, it squares.
- **The bit rung.** a + b − 2 · carry − sum = 0 is the half adder. Its carry
  is AND and its sum XOR. Under polarity bindings, those two laws, a wire and
  the two constants give all sixteen two-input functions.

**What sets an operation apart is read from its table and accounting, never
from a name:**
- **Mechanism: used up or kept.** Is a written role's gain paid for by
  another role (a move), or by nothing (a read)?
- **Mechanism: along or across.** Do units trade one for one, or meet at a
  rate or corner?
- **Bindings, open to every law:** polarity, where the result goes, read
  direction, and folding.

Contracts:

1. **A free role is residue, and its cost is stated.** The completions with
   the least Σ cost · value over the free roles are kept. With no cost, every
   completion stays open, and the law is not functional
   (`HeapNarrowedBackwardSubtractsAndTheGapKeepsOneFace`).
2. **Where nothing fits, the step holds.** The written roles keep their
   values and the relation is left unmet. An optional blocked bit is 1 where
   the step held and 0 where it wrote
   (`RunBackwardTheProductDividesOrLeavesTheChoiceOpen`,
   `UnderSettleItGoesQuietOnlyWhereItIsBlocked`).
3. **Each law role has one writer.** A relation role reads or writes, never
   both. Shapes must match (`ABindingNamesEachRoleOnceAndMatchesShapes`).
4. **A binding's accounting says whether it is functional, and promises
   `Unchanged` for every role it does not write.** It does not yet derive a
   preserved sum: see §4 and §6, item 5.
5. **Rebuilt counting laws keep their bits.** `Counts.Sum`, `Shortfall`,
   `Tally` and `Accumulate` are bit-identical to the step functions they
   replace (`CountingLawsAreBindingsOfABalanceAndKeepTheirBits`).
6. **Settling is not time.** A result written over its own input changes once
   a step, and under `Settle` it goes quiet where it is blocked. Written into
   a new count, it settles at once (`SettlingVersusTimeTests`).
7. **Wide operations are small tables wired together.** The full adder is
   32 states, reused at every bit. A wide adder's transition is refused at
   4 bits (`ARippleAdderSettlesInTheStepsItsCarriesNeed`,
   `ALatticeOfBitProductsIsTheWideProduct`).

### 3.2 Equations as numbers

A `Polynomial` is a relation written as integers: for each term, a weight and
one exponent per role.
- A balance is the polynomial whose terms are single roles.
- A product is the term ab against the term c.
- A column between rungs is a balance whose weight is the radix.

Contracts:

1. **Canonical form:**
   - equal monomials are merged and zero terms dropped;
   - the weights are divided by their common factor;
   - the first weight is positive.

   Two equations are the same when their numbers are the same
   (`APolynomialIsCanonicalNumbers`).
2. **The table is the only thing that runs.** A polynomial builds its table
   once, and nothing in the engine reads the polynomial.
3. **`Bits` is the description length** that recognition compares with a
   table's bits.
4. **Text is a lens read.** `Lens.Describe` writes "a + b + c − 10d − e = 0",
   and the encoding holds no strings.
5. **Equations are not saved.** Transport saves tables, exactly as on master.

### 3.3 Numbers as rungs

Numbers are a place-value layer over counts. They add range and erase
nothing.

Contracts:

1. **A digit is a count.** It is unary or binary-coded, and the shape's size
   declares exactly which values exist. A ladder can be read over the first
   planes of material counts (`ACountGroupsIntoRungsAndRefinesBack`).
2. **Each rung has its own radix,** finest first. A rung is worth the product
   of the radices below it. Fifths sit under whole stars (5, 6), minutes
   under hours (60, 24), and nothing is a power of two unless the data is
   (`EachRungHasItsOwnPartition`).
3. **A column is a balance with the radix as its weight:**
   a + b + carry in = radix · carry out + sum. A ladder of columns narrows
   forward, backward, and with open digits. The same wiring adds 487 + 356
   and subtracts 843 − 487 (`AColumnIsABalanceWithTheRadixAsItsWeight`,
   `DecimalRungsAddSubtractAndKeepOpenDigitsOpen`).
4. **Carries are carry-outs, finest first, and the last is the overflow.**
   The finest carry in is a constant 0. Overflow, and a borrow below zero,
   are visible on the last carry. Forbidding them gives a contradiction, not
   a clip (`WhatDoesNotFitIsTheLastCarry`).
5. **Partitions add under a sum.** Below the top rung, the operands share
   radices. The top rungs may differ, so three of five plus two of five is
   five of ten. The sum's top rung, out of ten, comes from the shapes, with
   no rule (`ThreeOfFivePlusTwoOfFiveIsFiveOfTen`).
6. **Partitions multiply under a product.** `Scale` (a · k) and `Multiply`
   (a · b, as a lattice of digit products) are relations, so they also
   narrow backward. Both work at one radix, and the product has exactly
   n + m rungs (`LaddersMultiplyAcrossRungs`).
7. **Material moves conserve the place-value sum.** `Admit` takes one unit of
   pressure into a two-rung number atomically, and the unit waits as pressure
   at the top. `Group` and `Refine` move whole packets
   (`UnitsAreAdmittedAndCarriedAtomically`).
8. **Narrowing is monotone within one observation.**
   - Reopening one ladder leaves what was derived from it narrowed, so a new
     reading contradicts it visibly.
   - `Ladder.Reopen(network, position)` reopens every open binding of a
     network.
   - What is known by structure stays: constant carries, padding, and the
     fact that a product fits. A caller's restriction on an open carry is
     reopened, and must be stated again
     (`ANewObservationReopensTheLadder`,
     `AReobservationReopensWhatWasDerivedFromIt`,
     `AProductNetworkReobservesAfterReopening`).
9. **A reading validates first.** `Measure`, and `Restrict` of a whole
   value, check the rung and the range before any bit changes.

### 3.4 Uncertainty, resolution and residue

Nothing is silently dropped. Every loss has a place in the structure:

| What is not handled | Where it goes | Pinned by |
| --- | --- | --- |
| A sum that does not fit | The last carry, as overflow | `WhatDoesNotFitIsTheLastCarry` |
| A difference below zero | The last carry, as borrow; forbidden, an empty support | `WhatDoesNotFitIsTheLastCarry` |
| A value read only coarsely | The open finer rungs (`Measure`) | `AMeasurementCarriesItsUncertaintyThroughASum` |
| A change of partition that does not divide | The residue role of `Rescale` | `ChangingPartitionKeepsWhatDoesNotFit` |
| The same share known differently | Two ladders with different resolution, related by `SameShare` | `TheSameShareIsNotTheSameKnowledge` |
| A product read from measurements | A set of products, never a rounded one | `PartitionsMultiplyAndMeasurementsKeepTheirSets` |
| A bound looser than the truth | `Interval`, a sound bound read rung by rung, which a supposition can tighten | `AMeasurementCarriesItsUncertaintyThroughASum` |
| A step that could not complete | The blocked bit of `Bind` | `UnderSettleItGoesQuietOnlyWhereItIsBlocked` |
| An equation weaker than its data | `Found.Unexplained` | `ASystemIsAsTightAsTheDataAndSaysWhereItIsNot` |
| Data an equation does not explain | A fit's exceptions | `OneWrongEntryIsAnExceptionNotANewLaw` |

Contracts:

1. **A count and a measurement are different knowledge.**
   - A count closes its finer rungs at zero; a measurement leaves them open.
   - "3 of 5" read as a count and as a measurement give different supports
     and different sums.
2. **Reduction is a read the caller asks for, never automatic.** 5/10 stays
   five of ten until something reads it as one of two.
3. **Supports say what is possible, not how likely.** No uniform
   distribution is assumed anywhere.

### 3.5 Recognition

Recognition is `Bind` read backward: from tuples to the equations they keep.
It is host code. Everything it returns is numbers: polynomials, tuples, and
the indexes of exceptions.

Contracts:

1. **`Relations` returns a system, with what it leaves unexplained.** It
   finds the lowest-degree relations, computed in unbounded integers.
   `Found.Unexplained` is empty when the system is exact. `Checked` is false
   where the domain was too large to enumerate
   (`FullTablesGiveTheirRelationsBack`,
   `ASystemIsAsTightAsTheDataAndSaysWhereItIsNot`).
2. **A fit admits a stated number of exceptions.**
   - A wall is an exception, not another law.
   - An irregular table stays a table.
   - `Survivors` keeps the candidates the witnesses allow, counting walls as
     exceptions.
   - `Probe` asks the question that splits the survivors most

   (`AWallIsAnExceptionNotAnotherLaw`, `AnIrregularTableStaysATable`,
   `AQuestionTellsASumFromAProduct`,
   `AWallInTheWitnessesDoesNotRuleOutTheLaw`).
3. **Witnesses are read sparsely.** They come from a functional law or from a
   `LearnedTable`'s counts, without building a second dense table
   (`ALearnedTableBecomesAnEquationBeforeItIsComplete`).
4. **`Residue` finds a hidden free role and its cost:** shortfall's excess,
   accumulate's overflow, a clamped OR's overflow
   (`AHiddenResidueRoleIsFound`).
5. **Nothing is accepted automatically.**
   - `Compare` reports whether the tables are the same, how many
     before-states differ, both sizes in bits, and whether the equation keeps
     the law's accounting.
   - `Restore` refuses an equation whose table differs, even when it keeps
     the accounting. It also refuses one whose table breaks the accounting.
     Otherwise it attaches the original's promises
     (`ARestoredLawKeepsTheOriginalsPromises`).
6. **An equation holds over its declared domain and witnesses.** A finite fit
   says nothing beyond the roles' sizes.

### 3.6 What pre-numeric keeps

- Every master test passes unchanged.
- Order, occupancy, sides, paths, tension and held material need no numeric
  binding.
- A digit is a count, so numbers read material without replacing it.

## 4. Known limits of pass 1

These are true of the code now. Each has an owner in §6 or §7.

1. **A ladder knows partitions, not units.** Adding two ladders adds counts
   of their finest rungs. Sol found the same issue in Neon's anchors
   (`2/1 + 2/5` read as `4/1`).
   - `Ladder.Add` checks that the radices below the top agree.
   - It cannot check that the two finest rungs are the same unit, which is
     the caller's claim today.

   Owner: Sol (§6, item 3).
2. **There is no scope.** A ladder does not know which sites or reports it
   counts. So a replicated view of the same members could be added as if it
   were a second, disjoint pool. Owner: Sol (§6, item 4).
3. **A binding's accounting is weak.** `Bind` promises only functionality and
   `Unchanged` roles, not a preserved sum, even where the relation conserves.
   `Restore` attaches the original law's promises, so a restored law keeps
   them, but a fresh binding does not derive them. Owner: Sol (§6, item 5).
4. **`Interval` is per rung,** so it is sound but can be loose. A joint
   interval is not built.
5. **Multiplication is narrower than addition.** It works at one radix, the
   product has exactly n + m rungs, and a lattice knows only what each column
   knows. A question the whole product could answer at once may stay open.
6. **`Fits` is brute force.** It is fine for a few small roles and slow
   beyond them.
7. **The step mode is not saved.** Declared-order and taken-together steps
   are chosen by the caller and are not part of the saved form. A machine
   that relies on one must be stepped the same way after loading. Owners:
   Sol writes the gate (§6, item 2), Neon meets it (§7, item 3).

## 5. Deferred by agreement

All three branches agree to leave these out of the first merge:

- **Accepting a learned equation automatically.**
- **Probabilistic or weighted uncertainty.** Supports stay possibility, with
  no likelihood.
- **Nonlinear propagation and interval inference in general.**
- **Persisting equations, step mode or reversibility in the transport.** This
  waits for one version marker, designed once (§7, item 6).
- **Rounding to nearest.** It needs a joint window over coarse and fine
  digits, or a landmark half a partition away.
- **Unequal regions within a rung.** The native `CoarseReadSurface` is the
  reference.
- **Search policies for recognition,** beyond the deterministic building
  blocks above.

## 6. Pass 2: Sol, for conformance and contracts

Start from the head of `ops_synthesize` after pass 1. Commit on
`ops_synthesize`.

1. **Review pass 1 against §3.** Record defects in §11 and fix them with
   tests. You have caught more real defects than either other branch in every
   round, so treat §3 as claims to break, not as settled.
2. **Write the conformance gates so they take the execution path as a
   parameter.** Today the only path is `Engine`; in pass 3 it will also be
   the compiled plan. The gates:
   - your six acceptance checks below;
   - experiments B14 to B18 from the claude branch's
     `ops_claude/GojoOperationsExperiments-2026-11.md`;
   - parity: the same planes and supports after `Settle`;
   - tick parity: the same step count, in declared order and taken together;
   - quiet: the same quiet step;
   - epoch: the same result after `Reopen` and re-observation;
   - save and load, including your repro where a simultaneous chain stepped
     to `010` before saving and `001` after loading.
3. **Units on ladders** (§4, limit 1). Give a ladder the least numeric unit
   that lets `Add` refuse raw ticks of different units, so that an explicit
   `Rescale` or common refinement is needed instead.
   - A suggestion, not a requirement: the finest rung's place value against a
     declared base, as an integer. It should meet the native
     `UnitShape.SubdivisionOf` and `ResolutionLadder` contracts.
   - No tags.
4. **Scope** (§4, limit 2). Disjoint scopes may pool, and a replicated view
   of the same members may not. A missing scope, an exact zero and a
   contradiction stay distinguishable.
   - Port `OperationScopeTests` and `OperationPartitionPoolingTests` onto
     the pass-1 `Ladder` where they fit.
   - Where they do not fit, add the smallest structure that makes them fit.
5. **Accounting at the binding** (§4, limit 3). Decide which promises `Bind`
   can derive from its relation and binding, and pin each with a test.
   - Example: a preserved sum where a conserving balance is bound in place.
   - Some bindings conserve nothing: a sum into a new count keeps its inputs.
     Say so rather than force a promise.
6. **The native fractional-bound correction.** Bring
   `Gojo/Structures/QualifiedReadSurface.cs` and `QualifiedReadSurfaceTests`.
   It is independent of EncodingC and can land first.
7. **Partial publication, skipped observations and exact qualification.**
   These are reads with stated provenance. Keep the basis a value was read on
   (count or measurement, which rung, which source), not only its supports.
8. **Promote fixture binding policy only where it is general.** This was your
   own critique. It goes into `Operations` or `Numbers` as numbers, not as
   new types.

Your acceptance checks from round 4, with where pass 1 stands:

| Check | Pass 1 | Owner |
| --- | --- | --- |
| Two actual scopes give exactly 5/10; a replicated view is not a second scope | The arithmetic is pinned; scope is not represented | Sol, item 4 |
| Two pending outcomes keep {5, 6, 7}/10 across save and load; a later observation narrows the same graph; missing scope, zero and contradiction differ | Supports are planes, so they save, but this is untested for ladders. Zero and contradiction differ; missing scope is not represented | Sol, items 2 and 4 |
| The whole and the calibration tell 5/10, 5/5 and common refinement apart; two whole portions keep two whole units | Top rungs may differ, so two wholes fit. Calibration is missing (§4, limit 1) | Sol, item 3 |
| A reframe keeps source-relative loss and rounds away nothing; a reader rechecks value and residue after load; a finer display does not raise acquisition resolution | `Rescale` has a residue role, and `Measure` keeps finer rungs open. After-load rechecking is untested | Sol, items 2 and 7 |
| Pre-numeric stays usable; every admitted action has its own accounting and permission | Master's tests pass unchanged. The accounting at bindings is weak (§4, limit 3) | Sol, item 5 |
| A fit, compiler or codec qualifies against these reads and keeps old saves before replacing a path | `Restore` requires the same table and the same promises. Transport is unchanged | Sol writes the gates, Neon meets them |

**Done when:**
- every item above is pinned by a test or recorded in §11 as refused, with
  the reason;
- the gates from item 2 can be pointed at a second execution path without
  being edited.

## 7. Pass 3: Neon, for the compiled backend and partition extensions

Start from the head of `ops_synthesize` after pass 2. Commit on
`ops_synthesize`.

1. **Port deltas, not copies.** Your round 3 adopted pass 1's `Polynomial`,
   `Bind` and `Counts`, and its own `Ladder`, `Numbers` and `Recognition`
   overlap pass 1 heavily. Do not bring your copies of those files.
   - Add to the pass-1 files only what they lack.
   - Keep `Booleans.cs`, `Dynamics.cs`, `Multiplication.cs` and the other
     exploratory files on `ops_neon`, as you proposed.
2. **Put `CompiledPlan` in `Gojo.EncodingC/Compilation/`, as an optional,
   derived backend:**
   - A plan is lowered only when `Recognition.Compare`, or `SameTable`,
     shows that its kernel is the same table as the relational law it
     replaces. Never by a name.
   - It falls back to `Engine` wherever it is not eligible.
   - It changes no transport bits and adds no enums. The reflection guards
     pass: no `System.Math`, strings or real numbers.
3. **Meet every pass-2 gate** with the compiled plan as the execution path,
   including save, load and the step-mode case.
4. **Benchmark the whole path, not only the kernel,** as Sol asked: layout
   conversion, eligibility checks and fallback. Report on the pass-1 ladders
   (`Ladder.Add`) as the relational baseline.
5. **Partitions and uncertainty.**
   - Port `VariablePartitionAndUncertaintyTests` onto the pass-1 `Ladder`
     and the pass-2 units. The mechanism cases are the most valuable here:
     one shared site doubled against two independent samples summed, and
     x − x against x₁ − x₂.
   - Add a `Portions` piece only where a test cannot be written against
     `Ladder`, and fix the two defects Sol found first:
     - the bare pool admitted `(1, 0)` as an operand;
     - `AddSameWhole` returned an empty support for 5/5 + 5/5, because its
       whole role is one bit.
   - Pooling (n₁, d₁) ⊕ (n₂, d₂) is two sums, a suggestion of how to start.
     It may need no new type.
6. **Transport, only if the backend needs it.** Design one version marker,
   once. Master saves must load bit for bit and step identically. If nothing
   requires a change, make none.
7. **`FitsFold` (min and max).** Pass 1 treats min and max as balances with
   two residue faces: a = c + e and b = c + f, with e + f kept least by
   cost. First show
   whether `Residue`, extended to two hidden roles, finds them. If a new
   hypothesis is truly needed, write it as numbers (weights, exponents,
   costs), not as a kind.

**Done when:**
- the backend is optional, every pass-2 gate passes with it, and master saves
  load and step identically;
- the benchmark covers the whole path;
- nothing in the pass-1 core is duplicated.

## 8. Rules for every pass

- **Verify** with `dotnet test Gojo.slnx`, or at least
  `dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj` plus any project you
  touch. Report the counts in §11.
- **Gojo-native numbers.** No enums, tags or string fields in the encoding. A
  concept that does not fit the numbers extends the numbers.
- **Nothing silently dropped.** Uncertainty and what does not fit become
  supports, residue, a carry or tension, never a rounded value.
- **Pre-numeric stays usable.** Master's tests pass unchanged, and a numeric
  layer reads counts without replacing them.
- **The saved form is master's** until a version marker is agreed (§7,
  item 6).
- **Tests are sentences,** as in the rest of EncodingC, and each new contract
  in §3 names its test.

## 9. Axiom notes

These are conflicts or gaps between the user's axioms and what the code can
do. They are for the user to settle, not for a pass to decide alone.

1. **Calculated or stored.** The axioms prefer calculated values. A
   polynomial is calculated, but what runs and what is saved is its table.
   The axioms do not say when a compressed form may replace the stored one.
   Pass 1 holds that it may not until a version marker and a parity gate
   exist.
2. **Larger is larger.** Binary-coded digits, as in the clock test, keep the
   value set exactly, but lose "more is more planes". Master's `Shape`
   already allows both layouts. Pass 1 uses unary by default and binary only
   where range needs it.
3. **Physically interpretable needs units.** A rung's partition is numeric,
   but its unit is implicit (§4, limit 1). This is a gap in the
   representation, not a choice.
4. **Widening against discarding.** `Interval` widens what is possible and
   never narrows it. The axioms say uncertainty must not be discarded, but
   not whether a read may loosen it. Suggested wording: a read may widen what
   is possible, and never narrow it without evidence.
5. **Where costs come from.** A free role's cost is a stored integer chosen
   by whoever binds. It could later be calculated from tension. The axioms do
   not say which.
6. **Discovered by structure, or by the host?** Recognition returns numbers,
   so concepts are discoverable numerically, but the search itself is host
   code. Whether a structure must eventually run its own search is not
   stated.

## 10. Provenance

| Piece | From |
| --- | --- |
| Relation plus binding; free roles as residue; holding where nothing fits | Claude, rounds 7 to 13 |
| Numeric free-role cost, with no cost meaning every completion is open | Sol's review of Claude's least-sum bias |
| `Polynomial`; the bit rung; numbers as rungs over counts | Claude |
| Atomic admission into a two-rung number | Sol; generalized to any radix by Neon and here |
| A system plus `Unexplained` instead of one equation | Sol's counterexample |
| `Compare.KeepsAccounting`; `Restore` requiring the same table | Sol's reviews |
| Monotone narrowing and `Reopen`; whole-network reopen | Sol's reviews |
| Unbounded-integer nullspace; sparse witnesses; walls counted as exceptions; residue discovery | Neon (`DiscoverBoundBalance`, the wall-aware probe), adapted |
| A digit multiplier across rungs | Neon's digit product; Sol's factoring comparison for its limits |
| `SameShare`, `Rescale` with a residue role | Sol (native projection with residue), Neon (projection) |
| The compiled backend, `Portions`, mechanism-dependent uncertainty | Neon, pass 3 |
| Conformance gates, units, scope, the native bound fix | Sol, pass 2 |

## 11. Pass notes

### Pass 1 (Claude)

- **Built:** §2 and §3. 259 EncodingC tests pass, 48 of them new.
- **Fixed from Sol's round-4 review:**
  - stale output after re-observation, now `Ladder.Reopen(network, …)`;
  - a restored law that keeps the accounting but changes behaviour, now
    refused.
- **Fixed while assembling:** carry-ins, padding and the no-overflow fact are
  constants.
- **Not done, and why:**
  - **Units and scope.** They need the native calibration contracts, which
    Sol knows best.
  - **Accounting at the binding.** Sol has the sharpest view of which
    promises hold.
  - **The compiled path.** Neon has the only real one.
- **Weaknesses to look for:**
  - `Interval`'s looseness;
  - the multiplication lattice's limits;
  - `Fits`' cost beyond small roles;
  - mixed radix and the resolution reads, which are the newest code here and
    have had the least use.

### Pass 2 (Sol)

*To be written.*

### Pass 3 (Neon)

*To be written.*

## Verification

Pass 1, on `ops_synthesize`:

```powershell
dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj
```

Result: 259 passed, 0 failed.
