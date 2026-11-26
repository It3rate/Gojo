# Round 15 (Claude): numbers that keep what they know, and who should merge

Status: **Round 15**, 26 November 2026. Code and notes on
`claude/youthful-newton-wh29co`. The round numbers follow the log in
[GojoOperationsRound14-2026-11.md](GojoOperationsRound14-2026-11.md). The other
branches number this round differently:

| This branch | ops_sol | ops_neon |
| --- | --- | --- |
| Round 14 (documentation) | "Operations Round 3" (`ba0746cf`) | "Round 2 Review" (`c331c000`) |
| **Round 15 (this note)** | its next round | its "Round 3" |

This round does three things:
1. **It brings this branch up to the level I recommended,** taking in the
   other branches' Round 14 reviews.
2. **It answers your question about uncertainty and resolution through
   operations.**
3. **It gives an honest view on who should write the merge into master.**

All 277 EncodingC tests pass (master has 211), every pre-numeric test among
them.

## 1. What changed in code

| Commit | Change | Taken from |
| --- | --- | --- |
| `88f18744` | `Recognition.Relations` returns a system of equations across degrees and the tuples it admits without witness; the lowest-degree equation alone can admit a forbidden tuple | ops_sol's review |
| `88f18744` | `Recognition.Compare` says whether the law's accounting is kept, and `Restore` rebuilds the law with it, so a move stays a move at a port | ops_sol's review |
| `88f18744` | `Recognition.Residue` finds a hidden residue role: shortfall's excess, accumulate's overflow, a clamped OR's overflow | ops_neon |
| `88f18744` | `Recognition.Survivors` counts walls as exceptions; `Witnesses` reads a learned table without building a dense law; the nullspace runs in unbounded integers | ops_neon's review |
| `8ca9b5b8` | `Ladder` takes a radix per rung, unary or binary-coded; the top rung's radix is the whole's partitions plus one | all three Round 14 notes |
| `8ca9b5b8` | `Ladder.Add` lets top rungs differ, so partitions add: three of five plus two of five are five of ten | this round's question |
| `8ca9b5b8` | `Measure`, `Interval` and `Reopen`: resolution as structure, and a new observation scope | ops_sol's contract |
| `8ca9b5b8` | `Numbers.SameShare` and `Numbers.Rescale`: equal shares as a balance with the denominators as weights; a change of partition with a residue role | ops_sol (native projection with residue), ops_neon (soft denominators) |
| `8ca9b5b8` | `Numbers.DigitScale`, `Ladder.Scale`, `Ladder.Multiply`: multiplication across rungs, as relations | ops_neon (its digit multiplier) |
| `8ca9b5b8` | `Numbers.Admit`: one unit admitted and carried atomically, the fine digit never leaving its partition | ops_sol (`RadixFour.Admit`) |

New tests:
- `NumberResolutionTests`, nine cases;
- `RecognitionTests`, four new cases: the system counterexample, hidden
  residue, restored accounting, and walls among witnesses.

## 2. Uncertainty and resolution through operations

You asked that the system account for precision as it moves through
operations, never discard it, and turn what it cannot handle into tension or
residue. Here is how this branch does that now, and where it leaves room.

### Two kinds of "3 out of 5"

"3 out of 5" can be two different things, and the structure has to say
which:
- **A count:** three of five stars filled. It is exact.
- **A measurement:** a continuous quantity read only to the fifth. It means
  at least 3 and less than 4 fifths, so it is not precise at all.

A number is a ladder of rungs, and resolution is which rungs are known.
Below the rung that was read, the finer rungs are either held at zero (a
count: `Restrict`) or left open (a measurement: `Measure`). Nothing else marks
the difference: no flag and no precision field. The supports are what is
still possible.

### Addition: partitions add, and uncertainty is carried

*Checked:*
- **Counts are exact.** Three of five plus two of five is five of ten: the
  sum's rung is out of ten because the column's shapes add the operands'
  partitions. No rule says so.
- **Measurements carry their openness.** Read to the whole star with tenths
  below, 3 measured plus 2 measured gives whole stars of 5 or 6, because the
  tenths may carry. The interval each rung allows is 5.0 to 6.9.
- **That interval is sound, not tight, and the gap is askable.** Supposing the
  sum were 6.9 contradicts, since nothing below 4.0 plus below 3.0 reaches
  it; 6.8 is consistent. A supposition in a fork tightens a bound when
  someone needs it.

So the additive relation keeps exactly what is known. A count stays exact; a
measurement's spread widens by the spreads it adds. Nothing is rounded, and
the slack in the per-rung bound is a question you can ask.

### Multiplication: partitions multiply, sets stay sets

*Checked:*
- **Counts pair.** Three of five times two of five is six of twenty-five
  cells: make hold, and the partitions multiply.
- **Measurements keep their exact set.** A value known to be 4 or 5 halves,
  times one known to be 2 or 3, is 8, 10, 12 or 15 quarters, and nothing in
  between. A relation carries the exact set, not an interval around it.
- **Across rungs, the lattice multiplies in both directions.** 47 × 36 =
  1,692 forward, and 1,692 over 47 settles 36 backward.
- **A lattice keeps only what each column knows.** Some questions the whole
  product answers at once may stay open in it (Sol's factoring result). The
  dense relation is the exact oracle wherever it fits.

You suggested the accounting depends on the mechanism, and it does. The same
product through the dense relation, the lattice, or coasting has different
tightness backward and different intermediate knowledge. The choice of
mechanism is a choice of what stays known.

### Ratio and division: say which meaning

"3 out of 5 against 2 out of 5" can mean at least four different things,
and each is a different relation, bound on purpose:

| Meaning | Structure | Built |
| --- | --- | --- |
| Keep both counts | no operation: two counts side by side | yes |
| Are they the same share? | `SameShare(d1, d2)`: `d2·a − d1·b = 0`, the denominators as weights | yes |
| Read one in another partition | `Rescale(from, to)`: `to·a = from·b + residue`; three fifths are four eighths and one tenth, the tenth kept | yes |
| A quotient | the product read backward, `a = b·q + r`, with a remainder | as the product relation narrowed backward; a remainder role is not yet wired for ladders |

### Reduction is a read, never done for you

Five tenths and one half are the same share, but not the same knowledge.
*Checked:*
- One half measured to the half means anything from 5 to 9 tenths.
- Five tenths measured to the tenth means 5.

Reducing 5/10 to 1/2 discards the partition, which is the evidence. So
nothing reduces automatically; a reduced share is a read someone asks for.

### Context picks the read

The same five, from two ratings out of five, reads two ways:
- as a **total**, five of ten;
- as a **mean**, in halves of a star, two and a half of five.

The numbers are the same; the read is chosen.

### Where what cannot be handled goes

Nothing is silently dropped. Every loss has a place in the structure:

| What is not handled | Where it goes |
| --- | --- |
| A sum that does not fit | the last carry, as overflow |
| A difference below zero | the last carry, as borrow; with borrow forbidden, an empty support (contradiction) |
| A change of partition that does not divide | the residue role of `Rescale` |
| A value read only coarsely | the open finer rungs |
| A bound that is looser than the truth | answerable by supposition; the per-rung `Interval` says it is a bound |
| A step that could not complete | the blocked bit of `Bind` |
| An equation weaker than its data | `Found.Unexplained`: the admitted tuples nobody witnessed |
| Data an equation does not explain | the exceptions of a fit |

### Room left for extension

These are not built, and each has a place to go:
1. **Rounding to nearest.** Floor reads are representable now: a coarse digit
   with the finer rungs open. A nearest read (3 means 2.5 to 3.5) correlates
   the coarse and fine digits. Representing it needs a joint relation, or a
   landmark half a partition away. Today it would be marginally looser than
   the truth.
2. **Tighter bounds.** `Interval` reads each rung alone. A bound that searches
   suppositions to tighten itself is a small addition. A joint read over
   several rungs is a larger one.
3. **Weighted uncertainty.** Supports say what is possible, not how likely.
   `LearnedTable` already counts witnesses, and weights per value would be
   the extension. Nothing here assumes uniform possibility.
4. **Calibrated units.** The native kernel already has subdivision factors,
   resolution links and projection with residue (`UnitShape.SubdivisionOf`,
   `ResolutionLadder`, `Relation.Projection`), as Sol pointed out. EncodingC's
   rungs should meet those contracts when the two are joined.
5. **Unequal regions.** The native `CoarseReadSurface` reads unequal regions,
   such as a five-region instrument. EncodingC has only equal partitions per
   rung.
6. **Error budgets for a compiled read.** Neon's declared finite-read regime
   is where an approximate native read would state its envelope.

### Pre-numeric is untouched

Every master test still passes, and the numbers erase nothing:
- **A digit is a count,** so a ladder can be read over material counts.
- **Admission and grouping are conserving moves.**
- **The ordinal and side reads are untouched:** a qualitative read runs on
  its own roles beside the numeric one (Sol's fixture).

## 3. What I did not build, and why

| Not built | Why |
| --- | --- |
| A native compiled path | Neon has a real one, now 338 to 2,815 times faster on single-law kernels and about 2 times on a decimal ladder. Rewriting it here would duplicate it. It should be ported once, driven by `Polynomial`, after the core is merged. |
| Transport changes (saving equations, saved step kind, reversibility bits) | Machines saved on master must still load. That needs a version marker designed once, at merge time. Neon's branch has added bits without one. |
| A sparse witness store | Master's `LearnedTable` is dense by design. `Witnesses` now reads it without building another dense table, and a sparse store is a learning-layer decision. |
| Min and max as hypothesis kinds | They are balances with two residue faces (`a = c + e`, `b = c + f`, `e + f` least), so they need no new kind. `Residue` finds one hidden role, and a two-role search is a direction, not built. |
| Automatic choice of partition, unequal regions, nearest rounding | Listed in §2 as room for extension; each needs a design of its own. |
| A smarter `Fits` | It is brute force: fine at these sizes, too slow for many roles. Neon's hint pre-filter (`ReadHints`) is the right next step. |

## 4. The other branches since Round 14

Both other branches published notes only since their round-13 code. Sol
commented on our code in `ba0746cf`, Neon in `c331c000`.

- **ops_sol** (`ba0746cf`). Sol's critiques of this branch were right, and
  are fixed in §1: the incomplete lowest-degree equation, bits without
  accounting, and grouping that needed room for a full packet. Its pointer to
  the native kernel's subdivision and residue contracts shaped `Rescale` and
  the extension list. Sol's caution stands too: a polynomial is a
  description, and the law's table with its accounting stays authoritative.
  `Restore` makes that explicit.
- **ops_neon** (`c331c000`). Neon's review proposes converging on this
  branch's core: `Polynomial`, `Bind` with a numeric cost, `Recognition` and
  the relational ladder, with Neon's residue discovery and compiled plan and
  Sol's conformance gates added. Its critiques are fixed in §1: dense
  witnesses, overflow, walls in probes, no residue discovery, no
  multiplication, unary-only digits and one radix. Its proposed `int[]`
  partitions are what `Ladder` now takes.

## 5. Who should write the merge into master

All three branches agree on far more than they disagree, so this matters
less than it might. Here are the facts I would weigh, measured against
master `9b805964`:

| | ops_sol | ops_neon | this branch |
| --- | --- | --- | --- |
| Production lines added in EncodingC | 83 | 4,320 | 1,098 |
| New top-level types | 3 | 24 | 5 |
| Enums added | 0 | 1 | 0 |
| Master core files changed | none | 8: `Engine`, `Flows`, `Wiring`, `Accounting`, `TransitionLaw`, `Plane`, `Store`, `Transport` | 1: `Flows` (`Sweep` added); plus `Counts` re-derived with the same bits, and `Lens` |
| Transport compatible with master | yes | no (unversioned bits) | yes |
| Defects others found in its code | few: little code to find them in | quiet parity, multiply overflow (both fixed), unversioned transport, an enum | the sweep's admission, an incomplete system, bits without accounting, dense witnesses, overflow, walls in probes (all fixed) |
| Defects it found in others' code | the most, in every round | several, measured carefully | some, mostly measurement |
| What its Round 14 note proposes as the base | a small reference plus conformance tests | this branch's core | this branch's core |

My recommendation, as one author, one reviewer and one follow-on:
1. **This branch writes the core library merge:** `Polynomial`, `Bind` with
   cost, `Numbers` and `Ladder`, `Recognition`, and `Counts` re-derived.
   - Both other branches' latest notes propose this core as the base: Neon
     explicitly, Sol for the column relation and the binding, with caveats.
   - It is the smallest change to master that carries the design: one core
     file touched, no enums, transport unchanged.
   - Its style matches EncodingC's: small static laws, prose summaries, and
     tests named as sentences.
2. **Sol reviews it, and owns the conformance gates:** experiments B14 to
   B18, partial publication, exact qualification, and accounting at the
   binding. Sol's reviews have been the most accurate of the three in every
   round, finding real defects in both other branches each time. I would
   want its sign-off before anything lands.
3. **Neon writes a second, later merge:** the compiled plan driven by
   `Polynomial`, behind a transport version marker, with its numeric breadth
   (multiplier variants, coasting across rungs) ported onto the shared core as
   configurations. Its speed work is real, and only Neon has done it.

I am recommending my own branch's core, so weigh that. The evidence I would
point to is that both other branches proposed adopting it on their own.

Two things would change my recommendation:
- **If master should stay minimal,** with numbers living in experiments
  until they prove themselves, Sol should write the merge; its style is the
  most conservative.
- **If native speed is the priority now,** Neon's compiled plan should come
  first.

This branch's own weaknesses, stated plainly:
- **Its notes run long.**
- **It shipped defects the others caught,** five of them in round 13 alone.
- **`Fits` is brute force.**
- **Mixed radix and the resolution reads are new this round,** with less
  soak time than the rest.

## Verification

- `dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj`: 277 passed.
- The footprint figures in §5 come from `git diff` against master
  `9b805964` for each branch head:
  - ops_sol `ba0746cf` (code at `9e5f8444`);
  - ops_neon `c331c000` (code at `059f5c45`);
  - this branch `8ca9b5b8`.
