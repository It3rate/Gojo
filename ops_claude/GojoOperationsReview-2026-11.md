# Operations: where we land

Status: **review**, 25 November 2026. Nothing new is built. This compares the
final versions of the three operations notes with each other and with master
after the tension merge (`9b805964`). It states the core they share, settles
what still differs, lists the corrections each note needs, and proposes the
first steps.

| Note | Where | Version | Written against |
| --- | --- | --- | --- |
| ops_sol | `Gojo/docs/GojoOperationsDiscussion.md` on `ops_sol` | `9fa5a67b` | `bece9fc9`, before the tension merge |
| ops_neon | `Gojo/docs/GojoOperations-2026-11.md` on `ops_neon` | `1e842ae0` | `bece9fc9`, before the tension merge |
| ops_claude | [`ops_claude/GojoOperations-2026-11.md`](GojoOperations-2026-11.md) on `claude/youthful-newton-wh29co` | this commit | `9b805964`, after the tension merge |

The three converged a great deal in their revisions: each read the others and
says so. What remains are a few choices and a handful of errors.

## 1. Where we land

These hold in all three notes unless marked. Master's tension system now
implements several of them.

1. **Operations are distinct acts, not four symbols.** Høyrup's reading of the
   Old Babylonian texts keeps them apart:
   - *join* onto something that keeps its identity, and *heap* into a named
     result;
   - *tear out* a part, and *compare* two separate magnitudes;
   - *steps of*, *raise*, *make hold* and *repeat*;
   - division as a question, answered through reciprocals.

   The vocabulary is quoted from the source in ops_claude.
2. **Every operation offers three reads.** The *relation* (what holds) reads in
   every direction. The *process* (what happens) has a direction, a time and a
   material account. The *fold* (what is read) has a resolution and a loss.
3. **A result gets an output role first.** It stays open, is read through a
   view, is materialized, or is bound to a receiver (ops_sol). On master, a
   computed result is written by a law when it changes, never by a host view
   (tension note, §11).
4. **There are four places for an operation.** Sol states the rule as "where
   roles and law are readable"; Neon and ops_claude name the places:
   - A *view* re-reads bits and never computes.
   - A *connection* carries one live input: a constant rate, like `Convert` or
     a bond-graph transformer.
   - A *junction* takes two or more live inputs: a law over three or more roles,
     a modulated transformer, or a gate through `Flows.When`.
   - A *local structure* has state, carries or scope.

   Ports carry only reads and conserving takes.
5. **Direction is visible in which roles a law writes.**
   - A workspace or fork turns an in-place process into a candidate result, and
     committing makes it in place.
   - Settling one event is not advancing time. `B = 2B` as one constraint is an
     incompatibility, not permission to erase `B`.
6. **Material is multiplied in only four accountable ways:**
   - refinement, which counts the same stuff finer;
   - supply, where any shortfall is debt (now built as `Tension.Demand` and
     `Draw`);
   - an informational update;
   - new structure.

   A product of two live counts crosses a port only as a read. Rate-independent
   conserving reactions compute only semilinear functions.
7. **Multiplication has several constructions:** steps of, raise, make hold,
   repeat, coasting, change of unit, and the fourth proportional. They agree as
   counts and disagree on units, identity, material and which operand
   controls. `ab` and `ab/u` are different evidence.
8. **The path is part of the product,** and the question decides whether it is
   kept.
   - The total is `Δ(ab) = aΔb + bΔa + ΔaΔb` summed over steps.
   - Corners are real when arrivals coincide, absent when arrivals are
     serialized, and vanish in a fine read.
   - When kept, the split is three route tallies, as `Flows.Fulfill` counts
     receipts (implemented). It can also be recorded while running or replayed
     later. Crystallization forgets it on purpose.
9. **Boolean is bit-rung arithmetic and an action.**
   - AND is the carry and the product, OR the clamped sum, XOR the wrapped sum.
   - On unary planes AND is the minimum, OR the maximum, and XOR the span
     between the two values.
   - Partition first, then select. A gate acts on a flow (`Flows.When`), and
     polarity is a chosen read (Shannon, 1938).
10. **The counting laws stand in for numerics.** The tension note calls them
    "the emulation of the first numeric step". Rungs, meaning carries whose
    weights are place values and coarse reads that leave residue, arrive later
    behind the same roles: "every wiring that reads them stays as it is".
    - Wide products must factor: lattice, doubling, clocked accumulation, or
      quarter squares, whose Babylonian use is speculative.
    - Radix is chosen per structure.
11. **One family covers the dynamics.**
    - It has two orders and three feedback signs. First order gives relaxation
      (Napier's logarithm), holding and growth. Second order gives oscillation,
      coasting and runaway.
    - At rung 0, the oscillator is the orientation note's generator J driven by
      time.
    - Update order decides whether the orbit closes. Today it can be made
      structure by a joint transition (ops_sol) or by a turn bit from
      `Flows.Turn` (see §2).
12. **How hard a value is to change follows from where it sits.** There are six
    positions, from a store count to a law's table. Crystallizing moves a value
    down; thawing in a fork moves it up. This came from ops_claude, and the
    other two adopted it.
13. **Compiled forms are derived and guarded.**
    - Canonical structure is the authority, and saving writes only the
      canonical structure (ops_neon).
    - A native stand-in replaces only crystallized structure. It must be
      bit-exact, or a declared finite read.
    - Overflow stays visible.
    - Recognition goes by content: `SameTable` compares kind, role shapes and
      bits.
    - Guards apply per lane, widened to a group where lanes share capacity or
      a contested writer (ops_sol).
    - `Convert` lowers by whole packets (ops_sol).
    - Witnesses are produced on demand.
    - The precedent is `FiniteLawPlan`, now promoted to
      `Gojo.Encoding/Core/Native`.

## 2. What master's tension system already provides

All of these are implemented on master (`9b805964`):

| Act | On master | What it settles for operations |
| --- | --- | --- |
| Join | `Tension.Receive` then `Absorb` | change enters as pressure; a full value leaves pressure waiting |
| Tear out | `Tension.Demand` then `Draw`, a move into the value's `Field.Room` | an empty value leaves debt: the supply shortfall of item 6 |
| Cancel | `Tension.Cancel` | netting only where the structure says two faces are one quantity |
| Deliver against an order | `Flows.Fulfill`, `Tension.Deliver` | two preserved sums; a tally per route records where units came from without a record per unit |
| Heap, with a rate | `Counts.Sum(a, b, weight)` | a new result; the weight is a constant locked in the table |
| Compare | `Counts.Shortfall`, swapped for the other face; `Settling.ValueGap` for open values | the two-faced comparison ("A over B, d goes beyond") |
| Threshold | `Counts.AtLeast`, with the threshold held as data | a perturbable constant (stiffness position 1) |
| Tally | `Counts.Tally` | presences into a count; information tension |
| Coast at rate 1 | `Counts.Accumulate`, `Counts.Streak` | rate × time while a signal holds |
| Rank | `Counts.Rank` | strict domination: the higher tier decides when it changed, the comparison a ladder needs |
| Gate | `Flows.When`, any number of gates | AND of conditions on a flow; each gate quadruples the table |
| Turn | `Flows.Turn` | alternation as a law |
| A choice about now | `Settling.Justify` | re-derived from its reasons each step, so it reopens |

What this changes:

- **The material half of the design is built.** Join, tear out, cancel,
  delivery and debt exist and are tested. Neon's "Phase 0" is largely done.
  Sol's note still says the material tension model "is still direction"; that
  sentence is out of date.
- **The counting laws are the junction-table approach at rung 1,** written as
  functional transitions that write a new result. They read one way only.
  Backward reads (subtraction, division, the open zero) still exist only as
  small relations, such as EncodingC's add relation. Whether they also read
  backward is a decision for you; §5 gives my lean.
- **The tension note supplies the numerics contract all three notes asked
  for.** Counting laws now; magnitudes in planes later, behind the same roles.
  Compiled stand-ins slot into the same place.
- **Step order can be structure today.** Kick and drift can wait on a turn bit
  and its flipped view, so exactly one runs per engine step. Run with every row
  reading the state before the step, this reproduces the in-order orbit step
  for step, as a joint transition does. It costs two engine steps per
  oscillator step, and it closes the step-order gap inside one engine. The
  order between separate engines is still the host's choice.
- **Views never compute.** The tension note replaced host-computed gap views
  with counting laws (§11 there). In the four places, a view re-reads bits;
  anything that calculates is a law.
- **Relations about now should be justified, not narrowed.** Narrowing suits
  facts that stay true. A possibility about now, such as "the product exceeds
  the threshold", should be held by `Justify`, so it reopens when its reasons
  return.
- **Tables are bounded by refusal.** Oversized tables are refused before they
  are built, so the table wall (item 10) is enforced.

## 3. What still differs, and how I would settle it

| Question | ops_sol | ops_neon | ops_claude | Where I land |
| --- | --- | --- | --- | --- |
| Order of work | grammar now, with small exact checks; numerals later | kinds now, with small checks; variable multiplication needs rungs | multiplication is the relation between rungs; numerics is the lift | Compatible. Build the grammar at rungs 0 and 1 now, with the counting laws as the numeric stand-in. Rungs come later behind the same roles |
| Where to start | the 3 × 4 comparison: relation, pairs, packets | a five-phase plan | the oscillator family, then multiplication as a process | Sol's 3 × 4 comparison first, since one small fixture exercises all three approaches; then the oscillator family |
| Radix | from evidence | radix 4 as the default for open ladders | per structure | Per structure. Radix 4 is a fair *exploration* default, because an open value costs a plane per value. Powers of two belong at crystallization |
| Compile guards | grouped where lanes interact | per lane | per lane | Per lane, widened to a group where lanes share capacity or a contested writer |
| Lowering `Convert(take, give)` | whole packets per firing | `DivRem(x·give, take)` | multiply-shift or `DivRem` | Whole packets. From 8 units at 3 → 2, the process emits 4 and leaves 2; `⌊8·2/3⌋ = 5` is wrong. A bulk read takes `min(⌊s/take⌋, ⌊room/give⌋)` packets |
| Napier's logarithm | first-order geometric decay | the `m = −1` row of a second-order loop | first-order relaxation | First order, restoring |
| The quarter-turn | J deferred, cited | built from Sides, not cited | J cited; an eight-state cycle | J from the orientation note, with the oscillator as its fixture |
| Intensive quantities | levels do not add; changes do | never add | never add | Levels do not add. Changes in one basis do |
| Material and information | "a gate is an action" | a hard wall | gates belong to both | Keep them apart where they are apart; gates and routes are both |
| A plan | small comparisons | Phases 0 to 5 | an experiment table | Neon's phases as the later plan, with Phase 0 now on master |

## 4. Corrections each note still needs

**ops_sol** is the most accurate, and still dense to read.
- Update it to master: material tension is built, not "still direction".

**ops_neon** is the most actionable, with these slips:
- Napier belongs in first-order relaxation, not the `m = −1` second-order row.
- Cite the orientation note's J for the quarter-turn. A six-state Sides cycle
  is not "structurally identical" to `PF → PI → NF → NI`.
- Lower `Convert` by packets, not `DivRem(x·give, take)`.
- Widen per-lane guards to groups where lanes interact.
- Intensive changes add; only levels do not.
- Phase 0 is largely on master now.
- The corner share in the fine limit is 2.5 × 10⁻⁴ at ×1000, not 1.4 × 10⁻⁴.
  Both the diagonal and the interleaved schedules give `1/(4N)`.
- The Bombelli characterization overreaches.

**ops_claude** is fixed in this commit:
- `Convert` lowering;
- group guards;
- intensive changes;
- serialized arrivals;
- master's tension primitives;
- step order through `Flows.Turn` or a joint transition;
- views never compute;
- `Justify` for facts about now;
- `FiniteLawPlan`'s promoted location.

## 5. Decisions for you

1. **First fixture:** the 3 × 4 comparison, then the oscillator family?
2. **Counting laws:** give them relation twins for backward reads (division,
   subtraction, open values), or keep them as one-way transitions and add
   relations only where a question needs one? My lean is neither as stated.
   `Sum`, `Shortfall`, `Tally` and `AtLeast` are built by one helper that reads
   inputs and writes one result, and `Rank` has the same shape: each is a
   relation read forward. Derive them from that relation, so backward reads
   come from the same table and nothing is kept twice. Read backward, `Sum`
   subtracts, and with `a` at 0 it answers the scribes' division question:
   what times the weight gives the total? `Accumulate` and `Streak` read their
   own last value, so they stay processes.
   `OperationPrimitiveTests` shows the reads are already calculable from
   heap's table: compare by reading it backward, threshold by folding,
   accumulate by binding the total in place.
3. **Step order:** adopt turn-gated alternation inside an engine as the
   standard way to state update order? Separately, should the host's choice
   of step kind, and the order between engines, be saved?
4. **Axiom clarifications** to adopt now, each stated in ops_claude §13:
   - addition is defined on amounts and changes;
   - product material comes from refinement or supply;
   - the scope of §9.7's "product is presence of both";
   - "junction" as the shared word;
   - a reversibility promise;
   - stiffness by position;
   - the compiled-form contract;
   - taking J off the deferred list.
   - bindings are not operations: where a result goes, polarity, which role
     is read back, and folds are free to every law (ops_claude §1.3).
5. **Radix:** per structure, with radix 4 as the exploration default?
6. **Provenance:** route tallies as the default record when asked, with replay
   as the fallback?
7. **One canonical note:** write a single operations note in `Gojo/docs` from
   these three? ops_claude, now current with master, would be the natural
   base, with Sol's 3 × 4 section and Neon's lowering map folded in.

## 6. First steps

1. **Fixture A, the 3 × 4 comparison** (EncodingC tests, from ops_sol):
   - *Relation.* `a, b ∈ 0..4`, `c ∈ 0..16`: 425 bits and 25 admitted tuples.
     It must divide, give `(3, 4)` and `(4, 3)` for `c = 12`, leave `b` open
     when `a = c = 0`, contradict `a = 2, c = 5`, and allow only
     `{0, 1, 4, 9, 16}` when both inputs are bound to one sample.
   - *Pairs.* The L, diagonal and serialized schedules give equal totals with
     splits `(0, 12, 0)`, `(3, 6, 3)` and `(3, 9, 0)`, kept as route tallies.
   - *Packets.* A rate of 3 a tick from a supply of 12 into a capacity of 10
     delivers `0, 3, 6, 9, 9` and leaves 3 at the supply. Any demand left
     unpaid stays as debt on `Tension`. The relation still reads 12 while the
     process delivers 9.
2. **Fixture B, the oscillator family** (EncodingC tests):
   - the rung-0 eight-state cycle as J driven by Sides events;
   - kick and drift alternated by `Flows.Turn` under a step taken together;
   - the order × sign table;
   - all of it saved, restored, and stepped to the same bits.
3. **Then the canonical note and the accepted clarifications,** followed by
   Neon's later phases (multi-rung ladders, factored products, a guarded
   native plan) as the plan.

## 7. Checks

Every number here is computed by the script in Appendix A of
[ops_claude](GojoOperations-2026-11.md#appendix-a-numeric-checks):
- turn-gated stepping (check 9);
- `Convert` packets (check 10);
- the 3 × 4 relation and packets (checks 11 and 12);
- the corner share (check 13);
- all earlier figures (checks 1 to 8).
