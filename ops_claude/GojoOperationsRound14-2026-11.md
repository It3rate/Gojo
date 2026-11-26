# Round 14 (Claude): the branches compared, and numbers with any partition

Status: **Round 14, a documentation round**, 26 November 2026. No code changed.
This note does two things:
1. It compares the latest operations code and notes on all three branches,
   with what I would take and what I would leave.
2. It answers whether numbers here support variable partition sizes, such as
   a five-star review that is exactly "out of five", or whether they are stuck
   with a binary resolution.

Every claim marked *measured* or *checked* was run for this note against the
pinned commits below.

## Round numbers

The branches number their rounds differently. This is the user's sequence as
this branch counts it, with the commits that answer each round, so the other
branches can line up their own numbering.

| Round | The ask | This branch |
| --- | --- | --- |
| 1 | Speculate on operations, write a detailed note | `c7cf6779` |
| 2 | First answers: three reads, path option, stiffness, native compilation | `7d520953` |
| 3 | Compare the ops_sol, ops_neon and ops_claude notes | (reply only) |
| 4 | Rewrite the note with everything learned | `e0f06b05` |
| 5 | Final review, updated to master's tension system | `39c0c2f8`, `d19aa71a` |
| 6 | Explore primitives under the named operations | (reply only) |
| 7 | Add that section and a classification test | `dda7b531`, `159cff7f` |
| 8 | Build the operations plan | `5e72e80d` to `0dda684c` |
| 9 | Coasting multiplier and settling check | `c18ed3e1` |
| 10 | Continue to a stopping place; experiments for comparison | `a113ce8f` to `c04869c1` |
| 11 | Compare the branches; same output, different potentials | `2d9b6bb6` |
| 12 | Act on feedback; the long-term proposal (symbolic regression, numbers) | `928ef76f`, `6a7d3f17` |
| 13 | Numbers and recognition (called "Round A" in the proposal) | `0bc24176` to `a26f5b80` |
| **14** | **This note: compare all code and notes; variable partitions** | this commit |

The other branches' latest work answers round 13:
- **ops_sol** `9e5f8444`: "Add bounded numeric rungs and qualifying reads".
- **ops_neon** `059f5c45`: Neon calls this its "Round 2", counting from its own
  symbolic-regression proposal.

## Part 1: the branches compared

### Where each stands

| | ops_sol `9e5f8444` | ops_neon `059f5c45` | this branch `a26f5b80` |
| --- | --- | --- | --- |
| EncodingC tests (master has 211) | 248, all pass | 257, all pass | 264, all pass |
| Production lines added since master | 83 | 4,320 | 751 |
| Test lines added | 1,292 | 2,837 | 1,856 |
| Core mechanism | authored circuits in tests; two internal helpers | `Operations.Balance`/`Bind` (adopted from here) plus many purpose-built laws | `Polynomial` relations run by `Bind` |
| Completion policy | stated per fixture | an enum: `LeastTotal` or `AllAdmitted` | a numeric cost per free role; none means all completions stay open |
| Numbers | radix 4, two binary-coded digits, fixed, internal | `RungNumber`, radix 2 to 10, split sum, carry, difference and borrow transitions, and a multiplier | `Ladder`, any radix, one column relation narrowed in every direction |
| Recognition | supplied sum and product candidates, checked against observations | `StructuralCompression`: separate balance, product and packet-trade recognizers; a two-input sparse hypothesis; residue discovery | `Recognition`: exact relations by nullspace, sparse fits with exceptions, probes, comparison, witnesses from a learned table |
| Native execution | none | `CompiledPlan`, now driven by the analyzer | none |

### Measured: one decimal workload, three ways

The workload is 3-digit base-10 addition over 4,096 lanes, with random
operands. Each run is checked against ordinary addition, and the best of
five runs is reported, in Release.

| | Law storage | Steps to settle | Time per lane | Correct |
| --- | --- | --- | --- | --- |
| This branch: one column relation, narrowed | 4,000 bits | 2 | 68 µs | yes |
| Neon, canonical: split sum and carry transitions | about 4,000,000 bits | 2 | 114 µs | yes |
| Neon, compiled | same | not reported | 54 µs | yes |

Sol has no general adder to measure. Three things follow:
- **Relations are about a thousand times smaller in law storage than
  transitions** for the same column, because a transition table is quadratic
  in its states.
- **On this workload, narrowing is faster than Neon's canonical transitions
  and close to its compiled path.**
- **Neon's compiler gains about 2 times here,** against the hundreds-fold
  gains it reports on single-law kernels. A ladder is many small laws, so
  the overhead between rows dominates.

### ops_sol

**Pros**
- **Precise evidence about partial publication.** With only the coarse digit
  published, an amount of 8 to 11 stays possible, and "at least 10?" stays
  open. The unconnected fine socket reports a skipped observation rather
  than guessing. A published fine digit of 0, 1 or 3 then decides.
- **Material carry is atomic.** One admission row increments the fine digit or
  carries `(c, 3)` to `(c + 1, 0)`, so a stored digit never leaves 0..3. At
  `(3, 3)` the sixteenth unit waits as supplier pressure, and the weighted
  accounting checks `pressure + 4·coarse + fine` on every transition.
- **It names a narrowing rule worth keeping.** Narrowing is monotone within
  one assessment: a changed source needs a fresh scope or an explicit
  reopening.
- **Acceptance requires agreement across every ordered input.** One withheld
  answer leaves qualification open.
- **Save and load mid-carry continue identically.**
- **It touches nothing in master,** and the qualitative port keeps its own
  meaning beside the numeric read.

**Cons**
- One radix (4), two digits, internal: a fixture, not a library.
- Binary-coded digits only. No adder across many rungs, and no multiplication.
- Discovery is a supplied candidate family, not a search.

### ops_neon

**Pros**
- **The broadest working numeric layer:**
  - three-digit base-10 addition and subtraction;
  - multi-digit multiplication up to `0..99 × 0..99`;
  - a two-rung coasting multiplier (experiment B13);
  - coupled relations resolved by forks (experiment B2).
- **Its symbolic regression covers more of the space than ours:**
  - automatic discovery of a free residue role, which ours lacks;
  - min and max folds among its hypotheses;
  - a realization signature that tells a live stretch, a captured repeat and
    a sampled repeat apart under a mid-run edit;
  - description-length accounting throughout.
- **It closes the loop to native code.** The compiled plan now takes its
  recognition from the analyzer instead of hand-written matchers.
- **It adopted the shared core:** `Balance` and `Bind`, landmark counts in
  place of fixed offsets, observation-only receipts, and J derived from
  existing moves.
- **Live landmarks make the kick and the drift one table.** This branch's
  kick and drift are also one law under two bindings, which our notes had
  not yet said.

**Cons**
- **The completion policy is an enum,** two named options, against the axioms'
  "no enums". A numeric cost covers both options and everything between.
- **Recognition is per family.** `BalanceFit`, `ProductFit` and
  `PacketTradeFit` each have their own recognizer, and `SparseHypothesis`
  fixes two inputs and four forms. Those are kinds in disguise. One
  polynomial family covers balance, product and carry with any number of
  roles.
- **Numbers are transitions,** so they are large and one-directional:
  - about 4 million bits for one decimal column, and radix capped at 10;
  - subtraction is separate laws rather than the same relation read backward;
  - the adder and multiplier wirings run on concrete values. Open digits exist
    (`RungNumber.Open` gives per-rung supports, and `DigitAddRelation` is a
    relation), but they are used for coupled constraints, not wired into a
    ladder that narrows in every direction.
- **The surface is large:** about 1,000 lines of compression, 1,851 of
  compiled plan and 393 of numbers.
- **Machines saved on master still will not load.** The transport gained bits
  without a version marker; this was accepted in Neon's notes but not yet
  built.

### This branch

**Pros**
- **One relation family for everything:**
  - `Polynomial` is canonical integer terms of any arity and degree;
  - balance, product and carry are its cases;
  - the counting laws keep master's exact bits;
  - text is a `Lens` read.
- **Numbers are relations.** One 4,000-bit column relation gives the sum
  forward and the difference backward, and keeps an unknown digit open,
  coupled to its neighbours, until something settles it. Overflow and borrow
  are visible residue on the last carry. A digit is a count, so a ladder can
  be read over material counts.
- **Recognition stays in that one family:**
  - exact relations by nullspace, with no direction;
  - sparse fits with exceptions, where a wall is an exception rather than
    another law;
  - probes, which isolated a product in two questions;
  - comparison with description lengths;
  - fitting from a learned table before it is complete.
- **It is small:** 751 production lines.

**Cons**
- **No multiplication across rungs yet.** Neon has it.
- **One radix per ladder.** Mixed radix works today only by wiring columns by
  hand (Part 2).
- **No residue discovery,** and no min or max folds in the hypothesis space.
  Neon has both.
- **Narrowing is monotone.** A source that changes needs a reopened scope, as
  Sol says, and `Ladder` has no explicit reopen.
- **`Fits` is brute force.** It grows quickly with roles and terms, and
  `Relations` uses 64-bit integers that a high degree could overflow.
- **Equations are not saved as equations.** `Transport` saves the relation's
  table; the polynomial, the compressed form, is not persisted.
- **There is no native path.** Neon's analyzer-driven compiler is the
  precedent to take.

### What I would take, and what I would leave

| Take | From | Why |
| --- | --- | --- |
| `Polynomial` as the one relation family, and `Bind` with a numeric cost | this branch | one mechanism, numbers not kinds, any arity |
| Relational ladders as the canonical number | this branch | a thousand times smaller, dual direction, open digits, faster than canonical transitions |
| Multiplication across rungs: digit products split into low and high digits | ops_neon | the missing half of arithmetic; rewrite as `a·b + c_in = r·c_out + d` in the polynomial family |
| Two-rung coasting | ops_neon | removes the slow unit-by-unit settling of round 9 |
| Residue discovery, and min and max folds as hypotheses | ops_neon | recovers shortfall's hidden excess, and folds that a polynomial cannot express |
| Realization signatures and the analyzer-driven compiled plan | ops_neon | trace evidence of mechanism, and the bridge from equation to native |
| Partial-publication evidence, atomic carries, and "reopen before narrowing again" | ops_sol | honest reads of incomplete numbers, and a contract `Ladder` should keep |
| Exact substitution only with agreement on every ordered input | ops_sol | the acceptance rule for any compressed form |
| The evidence suite, B14 to B18 | ops_sol | gates for compression and compilation |

| Leave | From | Why |
| --- | --- | --- |
| `FreeRolePolicy` as an enum | ops_neon | a numeric cost does both options and more |
| A recognizer and fit type per family | ops_neon | one polynomial search, with hints to prune it |
| Numbers only as transitions | ops_neon | keep transitions as the derived concrete run that compiles, not as the canonical number |
| The radix cap at 10 | ops_neon | a relation column at radix 60 is 864,000 bits and fits |
| Unversioned transport bits | ops_neon | add a version marker first |
| A fixed-radix internal helper as the number type | ops_sol | keep as a fixture beside the general ladder |

## Part 2: numbers with any partition

**Short answer: yes.** The numbers here are not stuck with binary resolution.
A partition is a count's capacity, and any capacity is allowed:
- a five-star rating is exactly "out of five";
- each rung of a number can have its own partition;
- quantities with different denominators combine exactly, by refining to a
  common partition.

Binary appears only as an optional way to store codes, and it never changes
which values exist. Checked against this branch's code:

| Question | What the code does | Checked |
| --- | --- | --- |
| Is a rating out of five exact? | `Shape.Unary(5)` holds exactly 0..5 in five planes. A rating of 4 is four of five partitions filled, and `Field.Room` reads the one that is left. | yes |
| Does binary storage force "out of eight"? | No. `Shape.Binary(6)` holds the same six values in three planes, and codes 6 and 7 are not values. The shape's size declares the partitions; the code is storage. | yes |
| Can each rung have its own partition? | Yes: a column at each rung is `a + b + c_in = r·c_out + s` with that rung's `r`. Wiring fifths under whole stars (radices 5 and 6) gives `3 3/5 + 1 4/5 = 5 2/5`. `Ladder` itself takes one radix, so today this is wired by hand. | yes |
| Can different denominators combine? | Yes, by refining both to a common partition with conserving moves. Two fifths refine into 6 fifteenths (`Convert(1, 3)`), one third into 5 (`Convert(1, 5)`), and both into one pile give `2/5 + 1/3 = 11/15`, with nothing left behind. | yes |
| Would binary have been exact? | No. 3/5 in 3 binary places is 5/8, and in 16 places 39,322/65,536, still not exact. Fifths are exact only on a partition of five. | yes |
| Can recognition see the partition? | Yes. A fifths column comes back as `a + b + c − 5d − e = 0`, so the partition is a weight in the equation, a number a fit recovers. | yes |

### How I would think about it

- **The denominator is the carrier's partition count, and the count's capacity
  is that denominator.** The older Gojo idea of a fraction whose denominator
  specifies partitions fits directly:
  - the numerator is how many partitions are filled;
  - the denominator is the capacity;
  - the room is what is left.

  No new type is needed for a single portion.
- **A number is a ladder of partitions.** Each rung is a carrier split into
  `r` parts, and the place value of a rung is the product of the radices
  below it. Uniform radix is one choice. Mixed radix covers time (60, 60,
  24), stars and fifths of a star, or a Babylonian 60 with its many divisors.
- **Combining denominators is common refinement, not conversion to binary.**
  The least common partition is where two portions meet exactly. This is the
  Babylonian reason for base 60 and the Egyptian practice of unit fractions,
  done with structure: refining is a conserving move, and the common
  partition is a weight in a balance (`3a + 5b = c` in fifteenths).
- **The extension that remains is the product of partitions,** not doubling.
  A ladder's range is the product of its radices. That is still multiplicative
  growth, but each factor is chosen by the data: 5 for stars, 60 for minutes.
  There is no forced power of two. Only the native tier, machine words, is
  binary, and there the declared size is a guard, so "out of five" stays out
  of five.
- **Mind the scale, not only the partition.** A partition count says how
  finely; it does not say where zero is or whether averages mean anything.
  Most five-star ratings run 1 to 5 with no zero, which is an interval scale
  with four steps, not a ratio scale of five. Gojo has the pieces to say so:
  - **ordinal:** landmarks and sides only;
  - **interval:** a count measured from a landmark count, as our oscillator
    measures position;
  - **ratio:** a count from zero.

  This is Stevens' distinction, already cited in the operations note. An
  average of star ratings is exact in fifteenths or thirds of a star, but
  whether it means anything is a question about the scale.

### Limits as the code stands

1. **`Ladder` has one radix.** Mixed radix needs a radix per rung, which is a
   small change.
2. **Common denominators can hit the table wall.** A transition over counts of
   fifteenths, `Counts.Sum(15, 15)`, is already refused. The relation form,
   `3a + 5b = c`, fits easily, and rungs extend past it.
3. **A support distinguishes at most 64 values,** so a single partition finer
   than 63 needs two rungs: minutes as a rung of 60 is fine, thousandths as
   one rung is not.
4. **There is no portion type that keeps a denominator with its count.** The
   capacity does this implicitly today. Explicit portions with common
   refinement, comparison by cross-multiplication (`a·d − c·b`, a degree-2
   polynomial), and rescaling between partitions (five stars to ten points
   exactly, five to three with residue) are not built.

## Part 3: proposal for the next code round

1. **Mixed-radix ladders.** `Ladder` takes a radix per rung; place value is
   the product below. Tests:
   - stars and fifths: `3 3/5 + 1 4/5`;
   - a clock: 59 seconds plus 1 carries into minutes;
   - a radix-60 rung recognized as weight 60.
2. **Portions and common refinement.** A count with its capacity as the
   denominator. Refinement to a common partition as conserving moves, and as
   a balance with the refinement factors as weights. Comparison across
   denominators. Rescaling, exact where the partitions divide and with
   visible residue where they do not.
3. **A five-star fixture.** Ratings as counts out of five, a histogram as
   tallies, the mean as an exact portion, the scale marked as interval (a
   landmark at one star), and the error a binary encoding would have added,
   shown beside it.
4. **Multiplication across rungs** in the polynomial family:
   `a·b + c_in = r·c_out + d`.
5. **Residue discovery and folds in recognition:** a hidden residue role, and
   min and max as hypothesis forms.
6. **Reopening a ladder** when its source changes (Sol's contract), and walls
   counted as exceptions inside the probe loop.
7. **Persisting equations:** save the polynomial with the law it compresses,
   behind a transport version marker.

## Sources

Pinned commits:
- ops_sol `9e5f8444`
- ops_neon `059f5c45`
- this branch `a26f5b80`

Files read:
- **ops_sol:** `Operations/RadixFour.cs`, `Tests/OperationNumeric*Tests.cs`,
  `Gojo/docs/GojoOperationsDiscoveryProposal.md`
- **ops_neon:** `Laws/Operations.cs`, `Numbers/Numbers.cs`,
  `Learning/StructuralCompression.cs`, `Compilation/CompiledPlan.cs`,
  `Tests/NumbersTests.cs`, `Tests/SymbolicRegressionTests.cs`,
  `Tests/EquifinalPotentialsTests.cs`, `Gojo/docs/GojoOperations-2026-11.md`
- **this branch:** `Laws/Polynomial.cs`, `Laws/Numbers.cs`,
  `Laws/Recognition.cs`, and their tests

The decimal benchmark and the partition checks were run from small console
programs outside the repository, against each branch's Release build.
