# Operations: the three branches compared

Status: **comparison**, 25 November 2026. This compares the operations code on
three branches, each checked out side by side, built and tested here:

| Branch | Commit | Builds on |
| --- | --- | --- |
| `ops_sol` | `29df5335` | master `9b805964` |
| `ops_neon` | `6f655fc2` | master `9b805964` |
| `claude/youthful-newton-wh29co` (this one) | after `c04869c1` | master `9b805964` |

All three build, and every EncodingC test passes on each: 237, 241 and 249
tests, against master's 211. The last section takes up a separate question:
whether different small structures that give the same output can carry
different potentials.

## 1. In one paragraph each

- **ops_sol** is the most careful about evidence. It has almost no production
  code: two internal helpers, 40 lines. It builds its circuits in the tests and
  measures each contract exactly: which pair formed and how, why a delivery
  held, which premise a scoped commit captured, and what a factored network
  can no longer infer backward. It is the best record of what a design must
  preserve, and it offers the least reusable library.
- **ops_neon** has built the most. It adds 1,988 production lines:
  - multi-rung numerics at any radix: carries, refinement, an open gap
    relation, and division by quotition and by partition;
  - four multipliers: lattice, Egyptian doubling, quarter squares and bounded;
  - dynamics with J moved into the kernel;
  - scaling a structure through its frame;
  - a reversibility promise and saved step kind, both added to master's core;
  - a 1,082-line compiled plan that recognizes kernels from table bits.

  It covers the most of the plan, with the most code and the most named,
  purpose-built laws.
- **This branch** is the smallest library built on one shared mechanism: a
  relation, run by binding its roles. A balance and a product, bound different
  ways, give heap, compare, accumulate, the counting laws with their exact
  bits, the half adder and the oscillator. It has 158 production lines. It
  covers less than Neon: no multi-rung numerics beyond the bit adder, no
  compiled plan, and no scaling of a structure.

## 2. What each built

| Area | ops_sol | ops_neon | this branch |
| --- | --- | --- | --- |
| 3 × 4 relation, read every way | yes, with ordered candidates kept beside marginals | yes | yes |
| Pairing and provenance | per-pair formation record (which side arrived, or both) | aggregate sweep and corner counts | aggregate receipts (`Flows.Sweep`), corners as residue |
| Packets and debt | yes, plus qualified booking with a ticket | yes | yes |
| Counting laws from one relation | no | no | yes, same bits as master |
| Booleans | scope reads | 16 named gates, `Gate(mask)`, four regions, Fredkin routing | 16 from AND, XOR and a wire under polarity bindings |
| Rungs and numerics | factored 2-digit product | radix-B digits, carries, refinement, ladders, open gap, two divisions | bit adder and 2-bit lattice only |
| Multiplication variants | dense against factored | lattice, Egyptian, quarter squares, bounded, coasting, pairing | relation, pairing, coasting, lattice |
| Dynamics | none | J in the kernel, rung-0 oscillator, signed shears, order × sign | kick and drift as balances on counts, J from existing moves, order × sign |
| Step order | contracts discussed | step kind saved in `Engine` and `Transport` | turn-bit gating and declared order |
| Settling against time | none | yes | yes, with a blocked bit |
| A structure as the target | scoped gain with grouped workspace commit | frame recalibration, broadcast, stretch, replication | none |
| Stiffness | positions discussed | six-rung ladder | three positions tested |
| Compiled plans | none | recognizer, SIMD, guards, deoptimization, finite-read regime | none |
| Changes to master's core | none | `Engine`, `Accounting`, `TransitionLaw`, `Wiring`, `Transport` format, `Plane` | `Counts` re-derived with the same bits; `Flows.Sweep` added |

## 3. Size and speed

All numbers were measured here, in Release builds, with the same harness
run against each branch.

| Measure | ops_sol | ops_neon | this branch |
| --- | --- | --- | --- |
| Production lines added | 40 | 1,988 | 158 |
| Test lines added | 924 | 1,931 | 1,388 |
| Tests added | 26 | 30 | 38 |
| Test time, operations classes | 1.0 s | 1.6 s | 2.1 s (both mostly JIT warm-up) |
| Table build, `Shortfall(9, 9)` | 3.2 ms | 2.9 ms | 35 ms |
| Table build, `Sum(9, 9)` | 11.8 ms | 9.3 ms | 42 ms |
| Engine step, one 3-role law × 65,536 lanes | 552 ms in order, 307 ms together | 524 ms in order, 308 ms together | 544 ms in order, 309 ms together |
| Compiled step, same wiring | none | 299 ms | none |
| Compiled step, bounded multiply × 65,536 lanes | none | 470 ms (canonical 436) | none |
| Compiled step, `Convert(3, 2)` × 65,536 lanes | none | 974 ms (canonical 751) | none |
| Largest table a fixture uses | 160 bits (dense product) | `SignedShear(15)`, 961 states, 0.92 M bits | `Flows.Sweep(3, 3, 4, 12)`, 1,040 states, 1.08 M bits |
| Steps for a factored 2-bit product, together | 3 | not measured | 3, plus the quiet step |

What the numbers say:
- **Stepping speed is the same everywhere,** because all three use master's
  engine. That engine is slow at scale: about 5 to 8 µs per lane per step.
- **Neon's compiled plan is correct but not faster.** It recognizes its
  kernels from table bits, matches canonical results bit for bit, guards and
  deoptimizes as designed, and runs at 0.8 to 1.0 times canonical speed. The
  cost is in decoding and encoding planes, not in the table lookup. The
  contract is in place; the speed is not yet.
- **This branch pays for its shared mechanism at construction.**
  `Operations.Bind` enumerates the relation and groups its tuples, so building
  a table takes 3 to 10 times longer than master's step functions. It happens
  once per law and is cached. Stepping is unaffected, because the bits are the
  same.
- **The table wall binds all three.** Every fixture stays at 3 × 4 scale or
  smaller. Neon gets furthest past it with rungs; Sol and this branch shrink
  tables by factoring at the bit rung.

## 4. Pros and cons

### ops_sol
**Pros**
- Evidence contracts are exact and tested: per-pair formation, why a hold
  happened, joint against marginal supports, grouped scoped commits with
  staleness, and qualified booking with a ticket.
- It touches nothing in master. The production footprint is 40 internal lines.
- It found real problems in the other two branches:
  - **this branch's sweep:** a full receipt holds the material move, so
    recording changes the process;
  - **Neon's quarter-square circuit:** it loses exact backward support;
  - **Neon's compiled settling:** its tick count differs from the canonical
    engine's.
- It measured the cost of factoring: tables fall from 160 bits to 24, rows
  rise from 16 to 96, and an impossible product goes quiet instead of
  contradicting.

**Cons**
- There is little to reuse. Circuits are authored in tests, and the two
  helpers are internal.
- There are no dynamics, rungs, compiled plans or stiffness code.
- Extending it means copying circuit patterns, not calling a library.

### ops_neon
**Pros**
- It covers the most: rungs at any radix with conserving carries,
  refinement and an open gap; two divisions; several multipliers; dynamics;
  scaling a structure through its frame; a reversibility promise with
  `Inverse()`; and a step kind saved with the engine.
- The compiled plan is a serious scaffold. It recognizes kernels from bits
  rather than names, keeps per-lane parity, deoptimizes on dirty lanes,
  invalidates on edits, and separates the bit-exact regime from the
  declared finite-read regime.
- Reversibility and a saved step kind are useful kernel additions. Both were
  asked for in this branch's §13.

**Cons**
- It is large: 1,988 production lines, many purpose-built laws written as
  host step functions, and 16 named gates. That leans against "one small
  shared mechanism".
- It changes master's core:
  - `Engine` gains a saved step kind, and `Settle` an overload that uses it;
  - the transport format gains two bits with no version marker, so machines
    saved on master will not load;
  - J moves into the kernel, which the orientation note had left as its own
    decision.
- Signed values use a constant offset in the code, not a landmark held in
  structure, so the centre cannot be perturbed.
- The quarter-square circuit loses exact backward reads, which Sol measured.
  The compiled path is not yet faster.

### This branch
**Pros**
- One mechanism, `Operations.Bind`, with free roles as residue. It
  re-derives master's counting laws with identical bits.
- Mechanism and binding are separated, and a test calculates them from any
  law's table and accounting.
- The production code is small (158 lines), and the only core change is
  `Counts` re-derived with the same bits.
- Several things are made visible in structure: a blocked bit for held steps,
  debt for unpaid products, residue for folds, and corners as residue.
- Motion needs no signs. The landmark is a count, so the centre is
  perturbable.
- Stiffness by position, and booleans as polarity bindings, are tested
  end to end.

**Cons**
- Tables take 3 to 10 times longer to build.
- There are no multi-rung numerics, compiled plans, or scaling of a
  structure.
- The sweep coupled recording to admission, as Sol found: a full receipt
  blocked the move. *Fixed after this comparison* (`928ef76f`): the receipt
  only observes, and a receipt without room fills to capacity.
- Provenance is aggregate. "Which pair formed how" needs Sol's per-pair
  record.
- Only relations that `Balance` or `Product` express are covered. Reset,
  threshold and rank are still step functions.

## 5. Extensibility

| Question | ops_sol | ops_neon | this branch |
| --- | --- | --- | --- |
| Adding a new operation means | authoring a circuit in a test | writing a new law class or step function | writing a relation and binding it; often only a new binding of an existing relation |
| Reading backward comes from | the same relation (dense) | a second structure, or a relation where one exists | the same relation, through `Bind` or narrowing |
| Getting past the table wall | factoring, by hand | rungs and the ladder builder | factoring at the bit rung; rungs not yet built |
| Native speed | none | a scaffold exists, with no speedup yet | none |
| Risk to master | none | core and format changes | low: same bits, two additions |
| Fit with "one small shared mechanism" | neutral | weakest | strongest |

My view on combining them:
- **Take this branch's binding mechanism as the core.** Every operation is a
  relation plus a binding, with residue and a blocked bit.
- **Take from Neon:**
  - rungs and the ladder, rebuilt as balances where they can be (a carry is
    a balance with weight equal to the radix);
  - reversibility;
  - the saved step kind, with a transport version bit;
  - the compiled plan's guard and deoptimization contract, which is worth
    keeping before it is fast.
- **Take from Sol:** the evidence contracts as conformance tests every branch
  must pass:
  - per-pair formation;
  - why a hold happened;
  - joint against marginal reads;
  - scoped commits.

  Its correction of the sweep is already taken: the receipt no longer
  holds the move.

## 6. Same output, different potentials

You asked whether small structures of different configurations could give
the same output yet hold different potentials. Your example was a square
made from parallel lines, from a path, or from points around a circle, each
then making further shapes more easily. It is possible, and it is already
visible in what the branches built.

**A test of it.** `ConfigurationPotentialTests` builds one square three ways
in EncodingC:

| Configuration | How the square is made | Size and time | One change away |
| --- | --- | --- | --- |
| **Lines** | A 7 × 7 grid of lanes. A column line and a row line pair at each cell: the product of two bits, written in one step. | 49 lanes, one law of 8 states, one step | Move a line: a **rectangle**. Add one: a **grid** of 6 corners. Remove one: 2 points. |
| **Path** | A mover steps along its heading and turns by J after each side. | Two laws of 392 states, 8 steps a lap | Another side length: a **scaled square**. Turn by J²: a **segment**. No turn: a **line**. Turn by J³: the square the **other way round**. A side that grows at each turn: a **spiral** that never closes. |
| **Circle** | Kick and drift, with the kick's weight `k`. | Two laws of 729 states, 4 steps a lap | `k = 1`: a **hexagon**. `k = 3`: a **triangle**. `k = 0`: a point. `k = 4`: it does not return. A larger start: a **scaled square**. |

All three give four vertices a quarter turn apart. The neighbourhoods barely
overlap:
- only lines make a rectangle in one change;
- only the circle makes a triangle or hexagon;
- only the path opens into a spiral.

The circle's polygons come from one weight because the kick-and-drift step
has trace `2 − k`. Its integer orbits close with period 6, 4 and 3 for
`k` = 1, 2 and 3.

**The same holds for operations.** The 3 × 4 fixtures in all three branches
are this phenomenon:

| Configuration of 12 | Same output | One change away |
| --- | --- | --- |
| Dense product relation | 12 | Division and exact contradiction, read backward (all branches) |
| Factored lattice | 12 | Wider products from the same small tables, but an impossible product goes quiet instead of contradicting (Sol, Neon) |
| Pairing with receipts | 12 | How it grew, and other schedules |
| Coasting from a rate | 12 | Any multiple of the ticks: move the rate count |
| Quarter squares | 12 | Squares for free, with backward reads lost (Neon, measured by Sol) |

**Why it fits Gojo.** "Potential" does not need a new concept. It is a
calculated neighbourhood: perturb one site of the structure (a count, a
weight, a choice of law) and read which outputs become reachable. That is
exactly what the axioms ask for: structure that is traversable, perturbable
numerically, and discoverable without enumerating cases. A machine could keep
several configurations of the same output side by side and grow the one
whose neighbourhood holds the next target. That choice could itself be a
read: the smallest perturbation that reaches the goal.

**Where it comes from.** Biology calls this *degeneracy*: structurally
different elements performing the same function. Edelman and Gally (2001)
argued that degeneracy is what makes systems evolvable. Wagner's neutral
networks (2005) make the same point: genotypes with the same phenotype but
different mutational neighbourhoods. These references are from memory.

**What a longer-term version needs:**
1. **A way to hold several configurations for one output** and read them as
   one. This is the junction or fold read the design already has: the
   output is a fold, and the configurations are what it folds.
2. **A neighbourhood read:** for each perturbable site, the output after a
   unit change. This is cheap for counts and weights, and harder for choices
   of law.
3. **A cost per configuration** of size and steps, since the cheapest
   configuration is not always the one with the right potential.
4. **A rule for growth:** keep, copy or retire configurations by how often
   their neighbourhoods were used. This is selection, and it belongs with the
   learning work, not with operations.

**Caveats.** The fixture is small, and the circle's "square" is a square in
its own metric: in position and velocity it is a parallelogram. The path's
turtle laws are written as step functions in the test, not as bindings.

## 7. How this was measured

The harness and the commands are reproducible from this repository:
1. Check out each branch side by side with `git worktree`.
2. Run `dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj -c Release` in each.
3. Run a small console benchmark referencing each branch's built assembly.
   - It times table builds for `Counts.Shortfall` and `Counts.Sum`.
   - It times engine stepping for `Shortfall(3, 3)` over 65,536 lanes.
   - On Neon, it times `CompiledPlan.RunStep` on the same wiring and on
     65,536-lane bounded multiply and `Convert(3, 2)` wirings.

Timings vary with the machine. The ratios are what matter.
