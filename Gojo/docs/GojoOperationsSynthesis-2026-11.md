# Gojo operations synthesis

Status: **round 16, pass 4 of 4**. Branch `ops_synthesize`,
started from master `9b805964`.

This is the shared specification for operations, numbers and recognition in
`Gojo.EncodingC`. It is written in four passes on one branch, each building
on the last, before anything is merged into master:

| Pass | Author | What it brings | Done when |
| --- | --- | --- | --- |
| 1 | Claude | The core library: operations as relations and bindings, polynomial relations, numbers as rungs, recognition | This spec is written, and its contracts are pinned by tests |
| 2 | Sol | Conformance and contracts: scope, units, uncertainty, accounting, save and load, the native bound fix | Sol's acceptance checks (§6) pass, and run against any execution path |
| 3 | Neon | The compiled backend as an option, plus the partition and uncertainty extensions the core lacks | The backend passes every pass-2 gate, and old master saves load and step identically |
| 4 | Neon, with Claude's fuzz gate | General kernels only: the five family kernels leave, a general relation kernel joins the table kernel, and specialization follows a rule (§12) | Every gate passes, including the parity fuzz, and the benchmark is re-run |

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
- **Your instructions are §6 (Sol), §7 (Neon) or §12 (pass 4).** They are ordered. The
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

1. **A free role can account for residue, and its cost is stated.** The
   completions with the least Σ cost · value over the free roles are kept.
   With no cost all completions qualify. A transition with several different
   written results stays open. Completions with the same written result share
   one after-state. Retain an unresolved residue by binding it to a role that
   is written or read as support.
2. **Where nothing fits, the step holds.** The written roles keep their
   values and the relation is left unmet. An optional blocked bit is 1 where
   the step held and 0 where it wrote
   (`RunBackwardTheProductDividesOrLeavesTheChoiceOpen`,
   `UnderSettleItGoesQuietOnlyWhereItIsBlocked`).
3. **Each law role has one writer.** A relation role reads or writes, never
   both. Shapes must match (`ABindingNamesEachRoleOnceAndMatchesShapes`).
4. **A binding's accounting says whether it is functional, and promises
   `Unchanged` for every role it does not write.** `Bind(Polynomial, …)` also
   derives a positive `PreservedSum` when a homogeneous linear balance names
   the same weighted accounts before and after. Every nonzero term must be
   bound. The transition checks the promise on all admitted pairs, holds
   included. A read into a new count can leave both inputs unchanged without
   promising conservation. A conserved transition still needs an explicitly
   wired taking port, any gate, and room at the destination.
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
2. **The interpreter runs the table.** A polynomial builds its table once.
   A derived execution form (`CompiledPlan`) lowers strictly from the table's
   own bits and bindings through two family-independent kernels (small-table
   transitions and word-outer bit-sliced relations), retains a route back to
   `Engine` wherever ineligible, oversized, dynamic-topology, or stale, and
   changes no transport bits
   (`GeneralRelationKernelAcceleratesLaddersClocksAndSharedSiteDiagonals`,
   `GeneralRelationKernelCoversPortionsProductsOdvSidesAndValueGap`,
   `GeneralTransitionKernelAcceleratesSmallConvertShortfallRippleAndMultiplyTables`,
   `OversizedRecordedBrokenDynamicAndStaleCasesFallBackCleanlyWithExactParity`).
3. **`Bits` estimates the description length** that recognition compares with
   a table's bits. It is not a transport size.
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
5. **Add aligns rungs and the selected unit.** Below the top rung, the
   operands share radices. The top rungs may differ and hold different
   maximum counts. `AtUnitRung` selects a reference from the existing rungs.
   `TicksPerUnit` reads its subdivision factor from the intervening radices.
   `TryAdd` leaves an incompatible selection unbound. Common refinement
   uses an explicit weighted relation. An agreed physical standard is a
   premise of this local use. Matching ladder shapes cannot establish it.
   Two whole portions can sum to two whole units
   (`TwoPortionsAddOnOneSelectedWhole`,
   `DifferentUnitsNeedAnExplicitCommonRefinement`).
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
   - `Ladder.Reopen(network, position)` reopens writable supports at a common
     position in an aligned network. `ReopenRows` follows the actual role
     references of explicitly selected live rows when indexes differ. Read
     ports keep their supplier's evidence.
   - What is known by structure stays: constant carries, padding, and the
     fact that a product fits. A caller's restriction on an open carry is
     reopened, and must be stated again
     (`ANewObservationReopensTheLadder`,
     `AReobservationReopensWhatWasDerivedFromIt`,
     `AProductNetworkReobservesAfterReopening`).
9. **A reading validates first.** `Measure`, and `Restrict` of a whole
   value, check the rung and the range before any bit changes. A signed
   host range check does not depend on multiplying an unchecked place
   value (`MeasurementRangeChecksDoNotWrapOrNarrowOnFailure`).
10. **A pool reads selected members.** A bounded tally can connect to
    published scopes whose actual sites are disjoint in a common part basis.
    Its result keeps the occupied count and full member capacity. A second
    publication of the same sites fails qualification, even when presented
    in reverse order. `DisjointPositions` inspects currently exposed live
    positions. A missing publication leaves the row skipped. Known zero and
    empty support follow the ordinary count and contradiction contracts.
    The caller requalifies after a scope is retargeted. That check is a local
    read and has no saved automatic disjointness policy.
11. **Live-denominator portions validate operands and retain whole capacity.**
    Where a denominator itself sits in open supports rather than a fixed-radix
    ladder, `Portions.Valid` and `Portions.WirePool` reject zero-denominator
    and over-full operands such as `(1, 0)` as contradictions,
    `Portions.AddSameWhole` carries `whole ∈ {0, 1, 2}` so two full portions
    (`5/5 + 5/5`) keep both wholes, and `Portions.Project` and `Compare` keep
    exact source-basis residue and float-free cross-multiplication
    (`LiveDenominatorPortionsRejectZeroDenominatorAndKeepTwoWholesForFullPortions`,
    `OrdinalIntervalAndRatioReadsStayDistinctOnFiveStarScales`).

### 3.4 Uncertainty, resolution and residue

Nothing is silently dropped. Every loss has a place in the structure:

| What is not handled | Where it goes | Pinned by |
| --- | --- | --- |
| A sum that does not fit | The last carry, as overflow | `WhatDoesNotFitIsTheLastCarry` |
| A difference below zero | The last carry, as borrow; forbidden, an empty support | `WhatDoesNotFitIsTheLastCarry` |
| A value read only coarsely | The open finer rungs (`Measure`) | `AMeasurementCarriesItsUncertaintyThroughASum` |
| A change of partition that does not divide | The residue role of `Rescale` or `Portions.Project` | `ChangingPartitionKeepsWhatDoesNotFit` |
| The same share known differently | Two ladders with different resolution, related by `SameShare` | `TheSameShareIsNotTheSameKnowledge` |
| A product read from measurements | A set of products, never a rounded one | `PartitionsMultiplyAndMeasurementsKeepTheirSets` |
| A bound looser than the truth | `Interval`, a sound bound read rung by rung, which a `Fork` supposition can tighten | `AMeasurementCarriesItsUncertaintyThroughASum`, `MixedPartitionHalfStarsAddSubtractAndNarrowJointlyInAFork` |
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
4. **Uncertainty depends on mechanism and site identity.** Doubling one shared
   coarse sample (`2·x` via shared site on `Ladder.Add` or a balance) skips
   odd half-steps (`{12, 14}`), while summing two independent coarse samples
   keeps them (`{12, 13, 14}`). Shared-site difference (`x − x`) cancels to
   zero while independent difference (`x₁ − x₂`) stays open
   (`DoublingOneCoarseSampleDiffersFromSummingTwoIndependentSamples`,
   `SharedSiteDifferenceCancelsToZeroWhileIndependentDifferenceStaysOpen`).

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
4. **`Residue` and `Residues` find hidden free roles and their cost:**
   one residue role for shortfall's excess, accumulate's overflow and clamped
   OR (`AHiddenResidueRoleIsFound`), and two-residue balance systems
   (`Operations.System` + `Recognition.Residues`) for piecewise linear bounds
   `min(a, b)`, `max(a, b)` and `|a − b|` with no new fold kind or enum
   (`TwoHiddenResidueRolesRecoverMinMaxAndAbsoluteDifference`).
5. **Nothing is accepted automatically.**
   - `Compare` reports whether the tables are the same, how many
     before-states differ, both sizes in bits, and whether the equation keeps
     the law's accounting.
   - `Restore` refuses an equation whose table differs, even when it keeps
     the accounting. It also refuses one whose table breaks the accounting.
     Otherwise it attaches the original's promises
     (`ARestoredLawKeepsTheOriginalsPromises`).
   - A blocked bit is part of the binding when reproducing its table. When
     supplied to `Witnesses`, it keeps marked holds out of a proposed law's
     relation samples.
6. **An equation holds over its declared domain and witnesses.** A finite fit
   says nothing beyond the roles' sizes.

### 3.6 What pre-numeric keeps

- Every master test passes unchanged.
- Order, occupancy, sides, paths, tension and held material need no numeric
  binding.
- A digit is a count, so numbers read material without replacing it.

## 4. Known limits of pass 1

These were the pass-1 limits. §6 records the pass-2 outcomes and §7 assigns
the remaining work.

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
   A read that narrows says what it ignored (§9, item 2).
8. **Promote fixture binding policy only where it is general.** This was your
   own critique. It goes into `Operations` or `Numbers` as numbers, not as
   new types.

The round-4 acceptance checks and their current evidence:

| Check | Pass 1 | Pass 2 and what remains |
| --- | --- | --- |
| Two scopes pool to `5/10`. A replica of one scope does not qualify as a second. | Ladder arithmetic with supplied counts. | `SynthesisScopeTests` connects actual member lanes to a ladder tally, with an open socket for a rejected scope. Site identity is qualified when the fixture connects or rechecks that scope. |
| Two pending outcomes retain `{5,6,7}/10` through load and later narrow on the same graph. Missing scope, exact zero and contradiction differ. | Open supports already save as planes. | `SynthesisScopeTests` persists the linked suppliers and receiver, compares round-trip bytes, and narrows both graphs to `6/10`. Separate rows expose `Skipped`, the observed count and `Unadmitted`. |
| The selected whole and calibration distinguish `5/10`, `5/5` and common refinement. Two full portions keep two whole units. | Different top rungs could hold a larger result. The unit was implicit. | `SynthesisUnitTests` exercises the selected unit, `10/5`, and `2/5 + 1/3 = 11/15` through authored common refinement. A mismatched selection leaves the candidate unbound. |
| Reframing retains source-relative loss and bounds. Finer display cannot raise acquisition resolution. | `Rescale` and `Measure` retained residue and open finer rungs. | `SynthesisPublicationTests` rechecks connected residue after load, rejects crossed possibilities, and distinguishes unavailable publication from coarse acquisition. `FractionalProjectionRoundsBoundsOutwardAndRetainsSourceEvidence` verifies both signed native target frames. |
| Pre-numeric operations still run. A material action supplies accounting and permission. | Master behavior and law tables were retained. `Bind` described functionality and kept roles. | Original tests still exercise qualitative laws. A checked positive balance can allow an explicitly wired move, including after load. A kept-input read cannot take on that basis. |
| A replacement qualifies against the same observations, accounting, and old saves. | `Restore` required matching tables and preserved promises. Transport used the master grammar. | An explicit blocked role participates in exact recognition and restoration. The path-parameterized gates cover observations, recorded evidence, step budgets, quiet, re-observation and old-master replay. Pass 3 registers and qualifies its backend. |

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
   - Lowering is chosen where it pays, and can be undone (§9, item 1).
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
- **Kernels are general by default.** A compiled kernel is lowered from a
  law's own table. A special kernel is admitted only by the rule in §12.3.
- **Every execution path passes the parity fuzz** (§12.4), as well as the
  hand-built gates.

## 9. Axiom notes

After pass 1, the user answered the open questions below in round 16. They
are working positions, not fixed rules, and most will be settled by
experiment. Each says what it asks of the passes now, and the experiment that
would test it.

1. **Equations and tables are always a blend.** A law can move back and
   forth between a table and an equation. There is no threshold of time or
   structure count for the change. It follows pressure: what holding the
   table costs against what calculating it costs, plus obvious
   optimizations.
   - **For the passes:** `Compare` reports both sizes in bits. Treat that as
     one input to a pressure, not as a test that replaces the table once it
     is passed. A compiled kernel (§7) is the same kind of choice: made
     where it pays, and reversible.
   - **The saved form still changes only behind a version marker.** That is
     a compatibility rule, not a claim about which form is the real one.
   - **Experiment:** a structure that holds one law both ways and moves
     toward the cheaper form as its load changes, with its behaviour
     unchanged.
2. **A read may widen what is possible, and sometimes narrow it.** Every
   part is working out the best structure for its situation, so a read is not
   only passive.
   - **Widening helps, and is allowed.** `Interval`'s widening stands as it
     is.
   - **Narrowing is an obvious or instant optimization, taken with a grain
     of salt.** For example, a read can ignore a table of zeros, or a table
     that the read itself rules out.
   - **For the passes:** a narrowing read says what it ignored, so that part
     can be brought back. It never removes a possibility silently.
   - **Experiment:** which narrowing shortcuts pay, and how often they have to
     be undone.
3. **Costs ideally come from tension.** Whoever sets up an operation is also
   acting on tension or on goals that do not match, and nothing decides from
   above. Some choices will still look arbitrary: guesses, attempts, and
   models made from many small machines. The aim is to find an equation and
   then solve it, so the engine has to adapt.
   - **For the passes:** keep a cost a number a structure can write, so that
     a law or a tension reading can set it later. Do not hard-code a policy.
   - **Experiment:** free-role costs read from a tension count. The same
     binding should then keep a different completion as pressure moves.
4. **Structures will run their own equation search, with hints from many
   histories.** A metaphor carries a choke point or an alternate route from
   one domain into another. In the same way, a structure should reuse the
   forms it has found rather than discover them afresh each time. The
   mechanism is not known yet. It may be weak links to calculation, or a
   structure trained to think of the form.
   - **For the passes:** recognition stays host code for now. Keep what it
     finds as numbers that compare across domains: polynomials, bindings and
     costs. `Survivors` already takes a list of candidates, so earlier finds
     can be offered there today. They order the search; they do not replace
     it.
   - **Experiment:** offer equations found in one domain, such as a sum of
     counts, as first candidates in another, such as a sum of rungs or of
     sides. Count how many witnesses the hint saves.
5. **Larger is larger.** Binary-coded digits, as in the clock test, keep the
   value set exactly but lose "more is more planes". Master's `Shape`
   already allows both layouts. Pass 1 uses unary by default, and binary only
   where range needs it.
6. **Physically interpretable needs units.** Pass 2 makes the selected unit
   a rung in the number's existing structure. This makes relative scale
   explicit within an agreed local standard. Calibration between distinct
   standards still needs its own connected relation.

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

- **Conformance.** [`ExecutionConformancePath.All`](../../Gojo.EncodingC/Tests/ExecutionConformancePath.cs)
  is the registration point for the reference interpreter and any derived backend. A path supplies
  single-step, settle and workspace preview behavior. The tests build the
  same open supports, ports, rows and schedules for each path. Expectations
  cover planes and supports, admitted and skipped rows, scope identity,
  held participation, quiet tick, revised observations and save/load. The
  compiled path can use the interpreter on ineligible surfaces, including
  workspace preview if the backend cannot qualify it.
- **Replay.** A fixed byte image generated on master `9b805964` loads and
  re-saves byte-for-byte. From the same `100` chain, a step taken together
  yields `010` and a declared-order step yields `001`. After a save mid-run
  both machines stay in parity when the caller reuses the same mode. The
  saved machine carries wiring order, state and recorded evidence. The
  caller supplies the mode again after loading. The elapsed host tick
  counter restarts at zero. Replay compares step deltas.
- **Units and scope.** The numeric unit is a selected existing rung on a
  locally agreed standard. Place-value columns determine its subdivision.
  This mirrors the native scale direction of `UnitShape.SubdivisionOf`.
  Selecting a different rung changes the interpretation without reobserving
  a value. Where an exported read needs the selection after load, publish
  the selected rung as a port, or bind the calibration as weighted relations.
  No transport rule assigns a physical standard to arbitrary equal shapes.
  Disjointness is read from live published positions. The finite scope
  fixture keeps the contributing membership, whole and result connected.
  A port retarget requires a fresh qualification. A generic dynamic scope
  allocator and a dimensional unit algebra remain outside this pass.
- **Uncertainty.** A receiver reads only published rungs. A finer publication
  of the same acquisition can refine an earlier read when available, and a
  genuinely coarse acquisition stays open. Reframing keeps the exact
  coupled integer remainder in the original basis. The native qualified
  read now rounds interval bounds outward for fractional as well as whole
  residues, including with reversed target order. Receiver narrowing leaves
  the supplier's original support available to compare or reobserve. The
  selected row, source ports and retained residue explain that narrower use.
- **Accounting.** A binding can derive functionality from distinct after
  states and `Unchanged` from unwritten roles. An authored homogeneous
  balance can also supply a positive invariant when the same weighted
  accounts appear on both sides. Signed differences, hidden nonzero terms,
  bare tables and kept-input reads do not establish this permission.
  Binding a blocked bit is part of replaying a law, and marked holds are
  excluded from witnesses when the caller supplies that role. Existing
  counting laws use the same tables and the old codec. The binding accepts
  explicit integer costs. Connecting those costs to live tension remains
  an experiment under §9.
- **Corrected pass-1 defects.** Reopening a mapped row now follows its
  references and cannot reopen a read-only supplier. Opening only part of
  the affected network still leaves constraints in the rest. `Measure`
  validates without multiplying a host value that can wrap. Polynomial
  canonicalization compares full exponent vectors. Evaluation and prediction
  use exact arithmetic so overflow cannot admit an unsupported tuple. Very
  large authored powers can still make table construction or search costly.

The experiments from the
[Claude experiment log](https://github.com/It3rate/Gojo/blob/7d14db45/ops_claude/GojoOperationsExperiments-2026-11.md)
are now runnable through [the conformance tests](../../Gojo.EncodingC/Tests/OperationsEvidenceConformanceTests.cs):

| Experiment | Pass-2 gate |
| --- | --- |
| B14 | Named pair formation distinguishes equal aggregate counts, and a removal and later formation update only the affected receipt after reload. |
| B15 | Opportunity, stock, receiving room and a booked demand explain four held deliveries and each has a distinct way to release it. |
| B16 | Dense inverse narrows products 1 and 9 and rejects 5. Shared sample identity constrains both roles. A quiet factored network can keep false alternatives, so it cannot stand in for the exact read. |
| B17 | A tick admits one packet, keeps held stock and debt visible, and continues after save in either selected schedule. |
| B18 | A selected calibration guards an atomic edit at overlapping positions. A stale or split patch leaves the source unchanged. |

### Pass 3 (Neon)

- **Deltas only, zero core duplication.** Pass 1's `Polynomial`, `Numbers`,
  `Ladder`, `Counts`, `Flows`, and `Transport` stayed untouched. Only six
  internal accessors were added for the derived backend (`Plane.RawWords`,
  `Plane.TrimTail`, `Store.RawPlane`, `Store.RawLive`, `Wiring.Reference`,
  `Engine.AdvanceTick`). Exploratory files (`Booleans.cs`, `Dynamics.cs`,
  `Multiplication.cs`) and speculative approximate oscillator lowering
  (`FiniteReadContract`, `ApproximateWaveformRead`) stayed on `ops_neon` and
  were refused here.
- **Optional, derived compiled backend ([`CompiledPlan`](../../Gojo.EncodingC/Compilation/CompiledPlan.cs)).**
  Registered in [`ExecutionConformancePath.All`](../../Gojo.EncodingC/Tests/ExecutionConformancePath.cs).
  Adds zero enums (replacing `ops_neon`'s `KernelKind` enum with polymorphic
  internal kernel types; verified by `PassThreeAddsNoEnumsToEncodingC`), zero
  string fields, zero real numbers, zero `System.Math` calls, and zero
  `Transport` changes. Dropping a `CompiledPlan` returns directly to `Engine`
  with identical bits and transport bytes (§9, item 1).
  - **What compiles (strictly via `SameTable`, `Recognition.Compare`,
    accounting, bindings, and schedule — never by name):**
    - *Pass-1 relational `Ladder.Add` chains and single columns*
      (`Numbers.Column` verified via `SameTable`, pairwise-disjoint direct
      `Store` bindings, identity-mapped): 64-lane bit-sliced SWAR on settled
      words (`Size <= 16`) with exact 64-lane bitwise factorized narrowing
      (`AB[a+b] → ValidT[a+b+cin == p·cout + s]`) on open, backward,
      contradictory, or wider (`17..64`, e.g., `[60, 24]` clocks) words —
      accelerating Pass-1's relational `Ladder.Add` directly in place with zero
      layout conversion.
    - *Concrete multi-rung mixed-partition ripple adders* (combined 5-role or
      split 4-role `(Sum, Carry)` pairs verified against `Numbers.DigitSum` via
      `Operations.Bind` + `SameTable`, including mixed combined/split chains):
      64-lane bit-sliced SWAR across arbitrary mixed radices `<= 16`.
    - *Unary `Flows.Convert(take, give)`* (verified via positive
      `PreservedSum` accounting + `SameTable`): portable
      `System.Numerics.Vector<ulong>` SIMD unary plane shift, exact bounded
      multi-step `SettlePacketConvertExact`, and guarded single-wave
      non-identity stepping (e.g., the `100 → 001` chain under declared order).
    - *Bounded lossless multiply* (`a·b = val + ovf`, verified via `SameTable`
      and unchanged inputs).
    - *General small-table functional transitions* (`totalRoleBits <= 64`,
      `StateCount <= 512`, `<= 256` changing states): L1-resident portable
      SIMD XOR-flip kernel lowered from the table's own bits (covers
      `Counts.Shortfall`, `Counts.Sum`, `Flows.Fulfill`, `Settling.Justify`,
      `Numbers.Admit`, and B15's gated moves).
  - **What falls back cleanly to `Engine`:**
    - Any recorded wiring (`Wiring.Recorded is not null`, preserving all 7
      evidence planes and `Noted`);
    - Any non-`Store` binding (`Port`, `Socket`, `Workspace`, `Flipped`,
      `Life`, `Gap`, `Room`); `CompiledPlan.Preview` delegates to
      `Workspace.Apply`;
    - Relational wirings with shared planes / `SameReference` (such as
      `Ladder.Add(x, x, doubled)` or B16's `sameOperand: true` product);
    - Non-column `RelationLaw`s (B16's factored product inverse, `Portions`
      live-denominator relations, `Operations.Product`);
    - Dynamic-topology engines: any wiring writing any wiring's `Rows` store
      (directly or through `Port`, `Socket`, or `Flipped`) or writing any
      `Store.Life` surface (recruitment or retirement);
    - Non-identity wirings with active `Aliased` rows or multiple canonical row
      waves (`Wiring.Waves(staticRunnable).Count > 1`);
    - Multi-wiring simultaneous steps (`together: true`), so `100 → 010` and
      simultaneous ripple-carry schedules match `Engine.StepTogether` tick for
      tick (only a single identity-mapped contention-free wiring compiles under
      `together: true`);
    - Runtime broken codes (`010` unary, `>= Size` binary, broken multi-bit
      constants) deopt before mutating any bit;
    - Stale plans (`!IsCurrent`) after adding wirings, adding/retiring/rewiring
      rows, toggling `Attention`, enabling `Record`, plugging/unplugging a
      `Socket`, rebinding or extending a `Port` lane, mutating `Workspace`
      candidates, or adding/recruiting/retiring `Store` positions.
  - **Defects caught and fixed during Pass-3 review and final assessments:**
    - Removed `ops_neon`'s `private enum KernelKind` and made the six
      `Recognized*` kernel-family booleans `internal` (Claude's assessment);
    - Fixed `Store.Recruit`/`Retire` staleness, `Wiring.Attention` gating
      inside compiled runnable masks, broken multi-bit constant-plane deopt on
      relational columns, and exact per-rung `Before`/`After` tuple
      reconstruction across multi-rung concrete ripple adders;
    - Replaced `ops_neon`'s scalar per-row `Reference()`/`Runnable()` compile
      scans with 64-lane bitwise identity-reference verification so
      `Compile()` stays sub-millisecond even at 65,536 lanes;
    - Fixed all five Sol final-assessment `CompiledPlan` blockers (same-tick
      `Port`/`Socket` writes to `Rows`, same-tick `Store.Life`
      recruitment/retirement, `Port`/`Socket` lane rebinds in `WiringSnapshot`,
      non-identity `Aliased` vs. `Skipped` classification, and shared-position
      multi-wave non-identity packet rows) plus empty-runnable ladder wave
      parity, pinned as regressions on `ExecutionConformancePath.All` asserting
      full `EngineStep` and `WiringStep` evidence;
    - Extended `Recognition.Residue`/`Residues` with optional `blocked` role
      support and binary residue shapes above capacity 63 (`maxCapacity: 64`
      no longer throws), and keyed `Portions.Valids` directly on
      `(Shape, Shape)`.
- **Whole-path vs. kernel-only Release benchmarks (idle CPU, plane-for-plane
  and tick-for-tick verified against `Engine.Settle`):**

| Workload | Lanes | `Engine.Settle` | `Compile` only | Whole-path (`Compile + Settle`) | Steady kernel (`Settle`) | Whole-path speedup | Steady speedup |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Pass-1 `Ladder.Add [10,10,10]` settled (relational) | 4,096 | 376.859 ms | 0.251 ms | 1.388 ms | 0.965 ms (235.7 ns/lane) | 271.5× | 390.4× |
| Pass-1 `Ladder.Add [2,5]` settled (relational) | 65,536 | 4,777.069 ms | 2.555 ms | 3.167 ms | 1.071 ms (16.3 ns/lane) | 1,508.4× | 4,460.4× |
| Pass-1 `Ladder.Add [2,5]` 50% open coarse (relational) | 65,536 | 4,526.657 ms | 1.024 ms | 2.549 ms | 1.490 ms (22.7 ns/lane) | 1,775.7× | 3,037.2× |
| Concrete split `[10,10,10]` ripple (`TransitionLaw`) | 4,096 | 679.750 ms | 0.930 ms | 1.272 ms | 0.179 ms (43.7 ns/lane) | 534.4× | 3,795.4× |
| Concrete combined `[2,5]` ripple (`TransitionLaw`) | 65,536 | 4,446.100 ms | 1.526 ms | 2.105 ms | 0.441 ms (6.7 ns/lane) | 2,112.5× | 10,075.0× |
| `Flows.Convert(2, 3, 9, 9)` AVX2 / exact-settle | 65,536 | 11,363.129 ms | 0.289 ms | 2.948 ms | 2.655 ms (40.5 ns/lane) | 3,854.1× | 4,280.4× |
| `Counts.Shortfall(3, 3)` SIMD XOR-flip | 65,536 | 1,847.867 ms | 0.393 ms | 1.037 ms | 0.680 ms (10.4 ns/lane) | 1,782.3× | 2,718.7× |
| *Layout conversion check:* Pass-1 `Ladder` ↔ concrete `Store` (`[10,10,10]`) | 4,096 | 376.859 ms | — | 14.568 ms (extract + concrete + write-back) | — | 25.9× | — |
| *Ineligible fallback:* B16 `FactoredInverse` (`UsedFastPath=false`) | 1 | 0.1058 ms | — | 0.0915 ms | 0.0680 ms | — | — |
| *Ineligible fallback:* Shared-site `Ladder.Add(x, x, doubled)` (`UsedFastPath=false`) | 4,096 | 254.257 ms | — | 153.892 ms | 155.530 ms | — | — |

  Two findings stand out from the whole-path numbers:
  1. **In-place relational lowering beats layout conversion by 10.5×** on
     4,096-lane `[10, 10, 10]` (1.388 ms whole-path in place vs. 14.568 ms to
     extract Pass-1 `Ladder` digits into a concrete `Store`, run the 0.179 ms
     concrete SWAR kernel, and write back). Layout conversion only pays if a
     structure stays concrete across many repeated steps without re-entering
     open supports.
  2. **Ineligible fallback has zero penalty under `Settle`** (in fact it is
     slightly faster than `Engine.Settle` — 153.9 ms vs. 254.3 ms on 4,096-lane
     shared-site `Ladder.Add` — because `CompiledPlan.Settle` steps each
     fallback wiring in declared order directly without recomputing unused
     `EngineStep.Waves` dictionaries on every tick; `CompiledPlan.Step` still
     produces exact `EngineStep.Waves`).
- **Partitions, live-denominator portions, and mechanism-dependent uncertainty
  (§7, item 5).**
  - Ported onto Pass-1/2 `Ladder`, `AtUnitRung`, and `ExecutionConformancePath.All`
    in [`VariablePartitionAndUncertaintyTests`](../../Gojo.EncodingC/Tests/VariablePartitionAndUncertaintyTests.cs):
    mixed `[2, 5]` half-stars with forward/backward settling and `Fork` joint
    corner elimination; shared-site `2·x` (`{12, 14}`) versus independent
    `x₁ + x₂` (`{12, 13, 14}`) directly on Pass-1 `Ladder.Add` and single-rung
    balances; shared-site cancellation (`x − x == 0`) versus independent
    difference (`x₁ − x₂` open); ordinal vs. interval vs. ratio five-star
    reads; material non-splitting (`Flows.Convert(5, 8, 5, 8)`) vs. conserving
    common refinement (`2/5 + 1/3 → 11/15`); and cross-structure partition-5
    atomic admission surviving `SaveLocals` mid-carry.
  - Added minimal [`Portions`](../../Gojo.EncodingC/Laws/Portions.cs) only for
    live-denominator relations a fixed-radix `Ladder` cannot express, fixing
    both of Sol's defects first:
    1. `Portions.Valid` and `Portions.WirePool` now enforce `d >= 1 && n <= d`,
       so `(1, 0)` and `(6, 5)` contradict immediately instead of pooling;
    2. `Portions.AddSameWhole` gives its `whole` role capacity 2 (`{0, 1, 2}`),
       so `5/5 + 5/5` settles cleanly to `(whole: 2, rem: 0)`.
- **Transport (§7, item 6).** No version marker or codec change was needed;
  `Transport.cs` is byte-for-byte identical to master `9b805964`, and the
  captured master save loads, steps, and re-saves identically on both `Engine`
  and `CompiledPlan`.
- **`FitsFold` (`min` / `max`, §7, item 7).** Confirmed Pass 1's proposal:
  adding [`Operations.System`](../../Gojo.EncodingC/Laws/Operations.cs) and
  [`Recognition.Residues`](../../Gojo.EncodingC/Laws/Recognition.cs) (plus
  system `Compare`/`Restore` overloads) over two hidden residue roles with
  unit numeric cost recovers `min(a, b)` (`a = c + e`, `b = c + f`),
  `max(a, b)` (`c = a + e`, `c = b + f`), and `|a − b|` (`c = a − b + e`,
  `c = b − a + f`) with exact table and accounting restoration and no new fold
  kind or enum (`TwoHiddenResidueRolesRecoverMinMaxAndAbsoluteDifference`).

## 12. Pass 4: general kernels, with specialization earned

Status: instructions written by Claude in round 16. Neon does the reduction and
writes the kernel; the permanent fuzz gate is Claude's (§12.4).

### 12.1 Why

The user's position: special mechanisms may exist, but they should evolve
into being when they are needed, not be written up front. All three
assessments agree.

Pass 3's `CompiledPlan` has one general kernel and five tied to law
families. The general one is the small-table transition kernel, lowered from
a law's own bits. About two thousand of the file's 3,147 lines belong to the
five family kernels.

**Whose they are.** All of `CompiledPlan` is Neon's work. It began on
`ops_neon` in `f6586cb5` (25 November), was ported in pass 3 (`53bd235b`) and
hardened against Sol's repros in `8a24cfca`. Claude's round-14 benchmark
comparison helped start the race for ladder speed. The kernels are correct
under every gate. What is wrong is their place in the design, not their
behaviour.

### 12.2 What Neon does, in order

1. **Remove the five family kernels from `CompiledPlan`:**
   - `RelationalColumnPlan` and `RelationalColumnKernel`;
   - `FusedRelationalLadderAdder`;
   - `FusedConcreteLadderAdder`;
   - `PacketConvertKernel`, with `SettlePacketConvertExact` and the
     non-identity packet path;
   - `BoundedMultiplyKernel`;
   - their `TryRecognize…` entry points.

   Keep the scaffolding (eligibility, snapshots, staleness, fallback,
   telemetry) and the small-table transition kernel. The removed kernels stay
   in history (`8a24cfca`) and on `ops_neon` as reference implementations and
   speed baselines.
2. **Remove the `Recognized…` flags.** Tests assert through
   `CompiledStepReport` (`UsedFastPath`, `FastLanes`, `DeoptLanes`) and
   `HasAnyCompiledKernel`. At most two internal flags may remain, telling
   transition lowering from relation lowering.
3. **Add a general relation kernel,** lowered from the relation's own
   admitted tuples. It narrows supports exactly as the interpreter does, 64
   positions at a time. Neon's design:
   - **Word-outer.** It runs directly over `Store.RawPlane` words with
     `Vector<ulong>`, not tuple-outer.
   - **Tuples grouped by role-0 value.** A small offset table, derived only
     from the relation's bits, skips any value whose support mask is empty in
     the current word. Settled and narrowed lanes therefore skip dead tuples
     at once.
   - **A ceiling on admitted tuples,** for example 4,096 to 8,192. Larger
     tables fall back to the engine, so compiling and scanning stay small.
   - **Shared sites.** Where identity-mapped roles share planes, as in
     `Ladder.Add(x, x, doubled)`, it keeps only the tuples whose shared roles
     agree, instead of falling back.
   - **The engine's evidence for every row:** unadmitted as contradiction,
     ambiguous, skipped, aliased, and the same waves.

   It covers `Ladder.Add` columns, `Portions`, `Operations.Product`,
   `Odv.Addition`, `Sides` and `Settling.ValueGap` through one mechanism, with
   no adder-specific line.
4. **Pass every gate:**
   - the conformance tests on `ExecutionConformancePath.All`;
   - Sol's five topology and evidence regressions;
   - `ExecutionParityFuzzTests` (§12.4).
5. **Re-run the whole-path benchmark** with the two general kernels, against
   `Engine` and against the removed kernels' recorded numbers. Speed is a
   result to report, not a gate: the general kernels need only be correct.
6. **Update the docs:**
   - §3.2, contract 2;
   - the README's compilation row;
   - the glossary's compiled-plan row;
   - the pass-4 notes in §12.6.

### 12.3 How a special kernel can come back

A specialization is admitted only when all five hold:

1. **Need.** The general kernel is the measured bottleneck in a real
   workload, as its share of whole-path time. Winning a benchmark race does
   not count.
2. **Shape, not family.** It is lowered from what `Recognition` finds in the
   law's table, such as "a linear balance with these weights" or "a product".
   It is never chosen by a law's name or family. One equation-shape kernel
   then serves every law of that shape: a balance kernel would cover columns,
   conversions, shortfall, tally and `SameShare` alike.
3. **Exact.** `SameTable` against the law it replaces, and full evidence
   parity: the conformance tests and `ExecutionParityFuzzTests` compare
   `EngineStep` and `WiringStep`, not only final bytes.
4. **Paid for.** Its whole-path cost, including recognition and
   qualification time, beats the general kernel by a stated factor. The
   factor and its measurement are recorded here.
5. **Removable.** It goes when the general kernel catches up, or when the
   workload that justified it is gone.

This is the table-and-equation blend of §9, item 1, carried into execution:
- the table kernel runs where a table is irregular;
- an equation kernel runs where a short equation exists and pays.

Later, the structure itself could read that pressure as tension.

### 12.4 The permanent parity fuzz

`ExecutionParityFuzzTests.RandomNetworksRunAsTheEngineRunsThem` runs on every
registered path. Each of its 48 seeds builds one store from random parts:
- ladders, with open, partial, measured and contradictory digits;
- concrete ripple columns;
- conversions, with rows on one position or across positions;
- random tables over shared fields;
- laws that recruit and retire positions within a step;
- reads and moves through ports and sockets;
- a move that rewrites another wiring's row through a port;
- random relations over open values, some reading one value in two roles (a
  diagonal). This part was added in pass 4, for the general relation kernel.

Each network runs in both step modes for eight rounds. Between rounds, both
copies are disturbed alike: re-observed, corrupted, rebound or retired.

What it compares:
- state, including every wiring's rows and the saved bytes;
- the engine's waves;
- every row's evidence: changed, unread, unadmitted, ambiguous, skipped,
  aliased, contested and noted, with the before and after states and the row
  waves.

It takes about 14 seconds for both paths. Compiled kernels run in about a
third of the networks.

**Checked against the pre-fix backend.** Run against pass 3's backend before
Neon's fix (`53bd235b`), it fails at seed 2. Over 400 seeds it finds every
class of Sol's findings:
- wrong planes;
- a step reported quiet too early, from a stranded delivery;
- differences in the engine's waves, the row waves, and the changed,
  unadmitted, ambiguous and before evidence.

On the current backend, the same 400 seeds pass.

### 12.5 A master defect the fuzz found

In a taken-together step, a law that retires a position and another wiring
that writes the same position cause the engine to throw. `Wiring.Commit`
writes into the newly retired position. In declared order, the same write is
skipped and reported as skipped.

This is master code (`Engine.StepTogether`, `Wiring.Commit`), not the
backend. The fuzz treats the engine as the reference, refusals included: a
path must refuse where the engine refuses, and that run ends.

The fix needs a decision about what a taken-together step means when a
position dies within it. The options are to contest the write and the
retirement, to apply retirements last, or to skip the write and report it
as skipped. It is left for a separate change.

### 12.6 Done when, and pass-4 notes

Pass 4 is done when:
- `CompiledPlan` holds only the scaffolding and the two general kernels;
- every gate passes, the fuzz included;
- the benchmark is re-run and recorded;
- `dotnet test Gojo.slnx` passes on the final candidate.

Then merge into master.

**Pass 4 notes (Neon):**
- **Reduction & cleanup (`Gojo.EncodingC/Compilation/CompiledPlan.cs`, 3,147 → 1,572 lines, −50.0%):**
  - Removed all five law-family kernels (`RelationalColumnPlan`/`RelationalColumnKernel`, `FusedRelationalLadderAdder`, `FusedConcreteLadderAdder`, `PacketConvertKernel` with `SettlePacketConvertExact` and the non-identity packet path, and `BoundedMultiplyKernel`), all `TryRecognize…` helpers, all static reference-law caches, and all six per-family `Recognized…` flags.
  - Retained all hardened Pass-3 scaffolding (eligibility, `WiringSnapshot` including `Port`/`Socket`/`Flipped`/`Workspace`/`Store.RawLive` invalidation, `DetectsDynamicTopology`, fallback to `Engine`, `CompiledStepReport`, `HasAnyCompiledKernel`), two internal flags distinguishing transition vs. relation lowering (`HasTransitionKernel`, `HasRelationKernel`), and the general small-table transition kernel (`SimdTransitionPlan`, `totalRoleBits <= 64`, `StateCount <= 512`).
  - Zero new enums (`PassThreeAndFourAddNoEnumsToEncodingC`), zero strings/floats/doubles/decimals/`System.Math`, zero new internal accessors on `RelationLaw`, and zero diff in `Transport/` against `origin/master`.
- **General bit-sliced relation kernel (`SimdRelationPlan`):**
  - Lowered strictly from `relation.Bytes()` (`BitOperations.TrailingZeroCount`) with no law-family checks, up to `MaxRelationAdmittedTuples = 8192` and `role.Size <= 64` (covering `[60, 24]` clock columns at 7,200 admitted tuples, `[10, 10, 10]` at 200, `[2, 5]` at 50, `Portions`, `Operations.Product`, `Odv.Addition`, `Sides`, and `Settling.ValueGap`; larger tables fall back cleanly to `Engine`).
  - Packs admitted tuples into a flat CSR table grouped by role-0 value (`groupStart` + `flatOthers`) and executes word-outer with portable `Vector<ulong>` SIMD + 64-lane scalar tail directly over `Store.RawPlane` words, skipping any role-0 value whose support mask is zero in the current word.
  - Supports identity-mapped `SameReference` diagonals (e.g., `Ladder.Add(x, x, doubled)` and squaring `Operations.Product`) by pre-filtering admitted tuples where full-plane-shared open roles agree and writing back once on the canonical role; partial or plain/open plane overlap falls back to `Engine`.
  - Decodes plain, constant, unary, and binary-doubling roles with pre-mutation `Unread` validation/deopt and emits full canonical `WiringStep` evidence (`Changed`, `Unread`, `Unadmitted`, `Ambiguous`, `Skipped`, `Aliased`, `Contested`, `Before`, `After`, `Waves`), including single-wiring `together: true`.
- **Whole-path vs. steady-kernel Release benchmarks (Linux `.NET 10.0.404`, idle CPU, GC-isolated median trials, plane-for-plane and tick-for-tick verified against `Engine.Settle`):**

| Workload | Lanes | Fast | `Engine.Settle` | `Compile` | Pass-4 Whole (`Compile+Settle`) | Pass-4 Steady (`Settle`) | Pass-4 Whole speedup | Pass-4 Steady speedup | Pass-3 Specialized baseline (Whole / Steady) |
| --- | ---: | :---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Pass-1 `Ladder.Add [10,10,10]` settled | 4,096 | True | 337.986 ms | 0.544 ms | 1.090 ms | 0.546 ms (133.3 ns/lane) | 310.1× | 619.0× | 1.388 ms / 0.965 ms (271.5× / 390.4×) |
| Pass-1 `Ladder.Add [2,5]` settled | 65,536 | True | 4,031.924 ms | 1.252 ms | 2.070 ms | 0.819 ms (12.5 ns/lane) | 1,947.8× | 4,923.0× | 3.167 ms / 1.071 ms (1,508.4× / 4,460.4×) |
| Pass-1 `Ladder.Add [2,5]` 50% open coarse | 65,536 | True | 3,968.191 ms | 0.989 ms | 1.745 ms | 0.756 ms (11.5 ns/lane) | 2,274.6× | 5,251.0× | 2.549 ms / 1.490 ms (1,775.7× / 3,037.2×) |
| Shared-site `Ladder.Add(x, x, 2x)` | 4,096 | True | 212.316 ms | 0.219 ms | 0.261 ms | 0.046 ms (11.3 ns/lane) | 814.4× | 4,595.6× | *fell back:* 153.892 ms / 155.530 ms |
| Concrete split `[10,10,10]` ripple *(carry compiled, sum > 512 falls back)* | 4,096 | True | 587.864 ms | 0.282 ms | 240.623 ms | 229.808 ms | 2.4× | 2.6× | 1.272 ms / 0.179 ms (534.4× / 3,795.4×) |
| Concrete combined `[2,5]` ripple | 65,536 | True | 3,866.235 ms | 1.116 ms | 5.185 ms | 4.221 ms (64.4 ns/lane) | 745.6× | 915.9× | 2.105 ms / 0.441 ms (2,112.5× / 10,075.0×) |
| `Flows.Convert(2, 3, 9, 9)` *(100 states ≤ 512)* | 65,536 | True | 4,969.059 ms | 0.248 ms | 1.454 ms | 1.206 ms (18.4 ns/lane) | 3,416.3× | 4,118.6× | 2.948 ms / 2.655 ms (3,854.1× / 4,280.4×) |
| `Counts.Shortfall(3, 3)` | 65,536 | True | 1,383.824 ms | 0.276 ms | 0.799 ms | 0.523 ms (8.0 ns/lane) | 1,731.9× | 2,644.9× | 1.037 ms / 0.680 ms (1,782.3× / 2,718.7×) |
| `Portions.AddSameWhole(5)` | 4,096 | True | 110.400 ms | 0.069 ms | 0.113 ms | 0.044 ms (10.6 ns/lane) | 975.3× | 2,532.1× | *fell back in Pass 3* |
| `Odv.Addition()` | 65,536 | True | 1,253.559 ms | 0.251 ms | 0.640 ms | 0.388 ms (5.9 ns/lane) | 1,959.6× | 3,227.5× | *fell back in Pass 3* |
| B16 `FactoredInverse` *(1 lane)* | 1 | True | 0.371 ms | 0.126 ms | 0.177 ms | 0.052 ms | 2.1× | 7.1× | *fell back:* 0.0915 ms / 0.0680 ms |

  Three benchmark takeaways:
  1. **The general relation kernel beat Pass 3's removed relational ladder kernel on all three relational ladder workloads** (1.090 ms vs. 1.388 ms on `[10,10,10]`, 2.070 ms vs. 3.167 ms on `[2,5]` settled, and 1.745 ms vs. 2.549 ms on `[2,5]` 50% open) because it runs `Vector<ulong>` across every word — open and settled alike — with role-0 dead-value skipping.
  2. **It unlocked 814×–4,596× speedups on workloads Pass 3 could not accelerate at all:** shared-site `Ladder.Add(x, x, 2x)`, `Portions.AddSameWhole(5)`, `Odv.Addition()`, and 1-lane B16 `FactoredInverse`.
  3. **Honest regressions where the removed Pass-3 specialized kernels remain faster speed targets:** concrete split `[10,10,10]` ripple (the 400-state carry wirings accelerate while the 2,000-state sum wirings exceed `MaxTransitionStates = 512` and fall back: 2.4× vs. Pass 3's 534×), concrete combined `[2,5]` ripple (746× whole / 916× steady vs. Pass 3's 2,113× / 10,075×), and transitions whose state space exceeds 512 (`Flows.Convert` at larger capacities, wider bounded multiplies, or non-identity packet chains). Any future specialization returns only under §12.3.
- **Extended fuzz verification:** In addition to the permanent 48-seed `ExecutionParityFuzzTests` gate, a 400-seed sweep (`ordered` + `together`, 6,400 randomized steps/settles against `ExecutionParityFuzzTests.Compare`) passed on Pass 4's `CompiledPlan` in 20.6 s with zero mismatches.

## Verification

Pass 1, on `ops_synthesize`:

```powershell
dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj
```

Result: 259 passed, 0 failed.

`dotnet test Gojo.slnx` on the same commit:
- **EncodingC:** 259 passed.
- **QualitativeExecution:** 3,509 passed, in about 11 minutes.
- **Gojo:** 1,072 passed.
- **Every other test project:** all passed.
- **GojoCAD:** at first one failure, `GojoCadSkiaAdapterTests`, because the
  test project had no native SkiaSharp library for Linux.
  - The fix adds `SkiaSharp.NativeAssets.Linux.NoDependencies` 2.88.9, the
    same version as `SkiaSharp`, to `Tests/GojoCAD.Tests`.
  - GojoCAD now passes 155 tests. The 24 tests the missing library had kept
    from running are among them.
  - GojoCAD does not depend on EncodingC, and nothing else in the solution
    does either.

Use this as the baseline for passes 2 and 3.

Pass 2: `dotnet test Gojo.slnx` passed on Windows, including 1,074 native
Gojo tests and 3,509 QualitativeExecution tests. The final EncodingC run
passed 289 tests after the last conformance cases, and GojoCAD passed 155
with the added native-assets dependency.

Pass 3 (Linux, `.NET 10.0.404`, no `wasm-tools` workload installed so the five
`.Browser` hosts in `Gojo.slnx` do not build on this VM):
- **EncodingC (`Release` and `Debug`):** **329 passed, 0 failed** (all 20
  Pass-2 `[Theory]` conformance gates + 7 new `VariablePartitionAndUncertaintyTests`
  `[Theory]` tests executed across both `Engine` and `CompiledPlan`, plus 5
  `CompiledPlanTests` and 1 new `RecognitionTests` two-residue test; all
  `SubstrateTests` reflection guards and `PassThreeAddsNoEnumsToEncodingC`
  pass).
- **All 18 other non-WASM, non-`QualitativeExecution` test projects in
  `Gojo.slnx` (`Release`):** **3,789 passed, 0 failed** (`Gojo.Tests`: 1,074,
  `GojoCAD.Tests`: 155, `Gojo.Encoding.Tests`: 725,
  `Gojo.Encoding.Core.Tests`: 353, `PrimitiveExplorer.Tests`: 69,
  `Gojo.GearShapeLadder.Tests`: 21, `Minds.Tests`: 49, `Phototaxis.Tests`: 129,
  `PrimitiveForager.Tests`: 6, `PrimitiveLearning.Tests`: 67,
  `CompositeEmergence.Tests`: 66, `AdaptiveLifecycle.Tests`: 25,
  `UtilityEcology.Tests`: 119, `PrimitiveMeasure.Tests`: 717,
  `ConstructionMachines.Tests`: 149, `OrnamentPathCampaign.Tests`: 12,
  `OrnamentTension.Tests`: 49, `OrnamentFollowup.Tests`: 4), for **4,118
  passed, 0 failed** total across 19 test projects (`QualitativeExecution.Tests`
  was untouched and has no dependency on `Gojo.EncodingC`).

Post-assessment fixes (Claude `2e876211` + Neon follow-up on `ops_synthesize`,
Linux, `.NET 10.0.404`):
- **EncodingC (`Release` and `Debug`):** **343 passed, 0 failed** (including
  Claude's `ADomainTooLargeToEnumerateIsNeverCheckedOverNothing`, all five Sol
  `CompiledPlan` topology/evidence regressions + empty-runnable wave parity on
  `ExecutionConformancePath.All`, and
  `ResiduesHandleBlockedBitsAndCapacitiesAboveSixtyThree`).
- **19 non-WASM, non-`QualitativeExecution` test projects in `Gojo.slnx`
  (`Release`):** **4,132 passed, 0 failed** (`Gojo.EncodingC`: 343,
  `Gojo.Tests`: 1,074, `GojoCAD.Tests`: 155, plus all 16 remaining projects).

Pass 4 (Claude `20c89682` permanent fuzz gate + Neon general-kernel reduction on
`ops_synthesize`, Linux, `.NET 10.0.404`, no `wasm-tools` workload installed):
- **EncodingC (`Release` and `Debug`):** **345 passed, 0 failed, 0 skipped**
  (Release: 10 s, Debug: 26 s; 0 new warnings; includes all
  `ExecutionConformancePath.All` gates, Sol's five dynamic-topology/evidence
  regressions, Claude's permanent 48-seed `ExecutionParityFuzzTests`, and the
  rewritten Pass-4 `CompiledPlanTests` + `PassThreeAndFourAddNoEnumsToEncodingC`).
- **Extended differential fuzz:** 400 seeds × (`ordered` + `together`) × 8 rounds
  against `ExecutionParityFuzzTests.Compare` on `CompiledPlan` passed in 20.6 s
  with zero mismatches.
- **19 non-WASM, non-`QualitativeExecution` test projects in `Gojo.slnx`
  (`Release`):** **4,134 passed, 0 failed, 0 skipped** (`Gojo.EncodingC`: 345,
  `Gojo.Tests`: 1,074, `GojoCAD.Tests`: 155, `Gojo.Encoding.Tests`: 725,
  `Gojo.Encoding.Core.Tests`: 353, `PrimitiveExplorer.Tests`: 69,
  `Gojo.GearShapeLadder.Tests`: 21, `Minds.Tests`: 49, `Phototaxis.Tests`: 129,
  `PrimitiveForager.Tests`: 6, `PrimitiveLearning.Tests`: 67,
  `CompositeEmergence.Tests`: 66, `AdaptiveLifecycle.Tests`: 25,
  `UtilityEcology.Tests`: 119, `PrimitiveMeasure.Tests`: 717,
  `ConstructionMachines.Tests`: 149, `OrnamentPathCampaign.Tests`: 12,
  `OrnamentTension.Tests`: 49, `OrnamentFollowup.Tests`: 4). Remaining pre-merge
  gate on a host with `wasm-tools`: full `dotnet test Gojo.slnx` covering the
  five `.Browser` hosts and `QualitativeExecution.Tests` (3,509 tests; zero
  dependency on `Gojo.EncodingC`).



