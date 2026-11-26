# Final assessment of the operations synthesis: Claude

26 November 2026. Branch `ops_synthesize` at `53bd235b`. It contains pass 1
(Claude), pass 2 (Sol) and pass 3 (Neon) on top of master `9b805964`.

## Verdict

**Ready to merge into master.** I found no defect that blocks the merge.

Every contract in the synthesis spec has a test. The full solution passes.
A differential fuzz I ran against the compiled backend found no difference
from the engine (§3).

The points below are follow-ups, ranked. None changes the verdict:
- Two are worth deciding before the merge: §4, items 1 and 2.
- The rest can follow it.

## 1. What each pass did well

**Pass 2 (Sol)** is the strongest of the three. It found and fixed four real
defects in my pass 1:
- exponent keys that collided at 64 and above;
- `long` evaluation that could wrap and admit a false tuple;
- a `Measure` range check that multiplied before it checked;
- `Reopen`, which could reopen a read-only supplier.

It also did the following:
- **Units without tags.** A unit is a selected rung (`AtUnitRung`). `TryAdd`
  refuses sums whose selections differ. Common refinement is a weighted
  relation the caller wires.
- **Scope from site identity.** `DisjointPositions` reads it from the live
  published positions, never from values.
- **Derived conservation.** `Bind(Polynomial, …)` derives a preserved sum
  only where every term is bound and the same weighted accounts appear on
  both sides.
- **Path-parameterized gates.** The conformance gates take the execution
  path as a parameter, which is what let pass 3 plug in. They cover the
  scheduling repro and replay of a master save.
- **The native fix.** The qualified read now rounds fractional bounds
  outward.

**Pass 3 (Neon)** delivered the backend as asked:
- It is optional and derived. Every kernel is admitted by a `SameTable`
  check, and everything ineligible falls back to the engine.
- Transport is byte-identical to master. There are no enums, and the
  exploratory files stayed on `ops_neon`.
- It fixed both `Portions` defects Sol found.
- `Residues` showed that min, max and |a − b| need no new kind, only two
  residue roles with a cost.
- The benchmarks measure the whole path, including the cost of layout
  conversion. That is how they show converting is about 10 times slower than
  running in place.

**The spec held up.** Both passes kept the contracts, limits and acceptance
table current, with test names as evidence. The spec now describes the code.

## 2. Verification

| Run | Result |
| --- | --- |
| `dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj` | 329 passed, 0 failed |
| `dotnet test Gojo.slnx` (Linux) | All 20 test projects passed, 0 failures. This includes Gojo (1,074), GojoCAD (155), QualitativeExecution (3,509) and PrimitiveMeasure (717) |
| Differential fuzz of `CompiledPlan` against `Engine` (§3) | 3,000 random networks, 0 mismatches |

## 3. The differential fuzz

**Purpose.** The conformance gates use hand-built cases. A backend this size
(3,070 lines) needs random ones too.

**Method.** I built each random network twice from one seed, ran one copy
with `Engine` and the other with `CompiledPlan`, and compared:
- every plane at every live position;
- each settle result;
- the engine tick.

The comparison ran after every step or settle, for 12 rounds, in both
declared-order and taken-together mode.

| Family (300 seeds × 2 modes) | What was random | Plans with compiled kernels | Fast-path calls |
| --- | --- | --- | --- |
| Relational `Ladder.Add` | 1–3 rungs, radices 2–29, unary or binary, a wider top rung; exact, partial-digit, measured, open and contradictory digits; restricted overflow; re-observation through `ReopenRows` | 300 / 300 | 4,788 |
| Concrete ripple adders | Mixed radices 2–8, unary or binary; raw bit corruption, including broken codes | 249 / 300 | 1,057 |
| `Flows.Convert` chains | take, give and capacities; corruption; positions retired mid-run | 300 / 300 | 1,046 |
| Random functional tables | 1–3 roles over shared fields, unary or binary; corruption; retirement | 300 / 300 | 2,190 |
| Mixed engines | All four families in one engine, in shuffled declared order | 300 / 300 | 1,438 |

The result: **no mismatch in bits, settle results or ticks.**

Two things the fuzz did not reach:
- **Mixed engines taken together.** These used no fast path, which is by
  design: several wirings taken together fall back to the engine.
- **Sharing beyond one family.** In the mixed runs, the families shared an
  engine but not fields.

The harness is not committed: it took about 10 minutes. A trimmed version,
with about 20 seeds per family, would run in under a minute. It would be a
good permanent gate for any new kernel. I can add it if you want it.

## 4. Follow-ups, ranked

1. **The backend is many special mechanisms, against the axiom of one shared
   mechanism.** Of `CompiledPlan`'s 3,070 lines, about 1,400 are kernels for
   one family of law, adders and their columns:
   - `RelationalColumnPlan`;
   - `FusedRelationalLadderAdder`;
   - `FusedConcreteLadderAdder`.

   Convert and bounded multiply have their own kernels too. Only the
   small-table kernel, lowered from a table's own bits, is
   family-independent. It is the mechanism in keeping with Gojo.
   - The special kernels are correct (§3), and they earn the headline
     speedups.
   - But each new family of law would add another one. Keeping them behind
     an optional backend is the right containment.

   My recommendation: merge it as it is, and record the direction. That
   direction is to grow the general kernel to relations, meaning bit-sliced
   narrowing of any small relation table. A special kernel then has to beat
   the general one by a measured margin to stay.
2. **`CompiledPlan`'s public `Recognized…` flags name kernel families.** There
   are six booleans, one per kind. They are reads, not stored tags, but they
   put a catalogue of kinds into the public surface. `CompiledStepReport`
   already gives a numeric read, the fast and fallback lane counts.
   - Make the flags internal, or test through the report, before other code
     starts depending on them.
   - This is small, and easiest before the merge.
3. **Units hold within one local standard, and single-rung ladders carry no
   unit.**
   - `[2]` and `[6]`, both at their default unit rung, still add as raw
     counts.
   - The convention that makes the check work is to write a fraction as a
     finer rung under its whole, `[5, 2]` at unit rung 1. Then a mismatch is
     refused.

   Sol states the boundary plainly: matching shapes cannot establish a
   physical standard. I have added the convention to the glossary. A
   dimensional unit algebra remains future work.
4. **`Portions` tables stop at small denominators.** Its relations have up to
   five roles over the denominator's range, so they exceed `Law.MaxBits` above
   a maximum denominator of about 20. This should be stated in its summary.
   `Portions.Compare` writes its order as a code: 0 below, 1 equal, 2 above.
   That is ordered and numeric, but it is a code. Two faces, as the gap uses,
   would be the Gojo-native form if comparison grows.
5. **The step mode is still the caller's.** A machine stepped taken-together
   must be stepped the same way after loading. Pass 2 pins this, and the
   spec records it. It waits for the transport version marker, which nothing
   has needed yet.
6. **Small notes on the docs:**
   - Pass 3's notes say "five internal accessors" but list six.
   - "AVX2" refers to portable `System.Numerics.Vector<T>`, which runs on any
     CPU; AVX2 is only what the JIT emits on x86. That is better than the
     notes claim.
   - Sol's new test classes have no class summaries, unlike the older ones.
     The test names already read as sentences, so this is cosmetic.

## 5. What I changed in this final pass

Documentation only:
- **The glossary** now covers:
  - unit rung and ticks per unit;
  - scope and disjoint positions;
  - live-denominator portions;
  - systems of equations;
  - residues;
  - the compiled plan.
- **The README test table** now lists Sol's seven test files:
  - `SynthesisUnitTests`, `SynthesisScopeTests` and
    `SynthesisPublicationTests`;
  - `SynthesisScopedWorkspaceTests` and `ExecutionScheduleConformanceTests`;
  - `OperationsEvidenceConformanceTests` and `PolynomialExactnessTests`.

No code changed.

## 6. After the merge, in order

1. **Decide items 1 and 2 of §4.** Either record the direction for the
   backend, or make its flags internal.
2. **Add the trimmed differential fuzz** as a permanent gate for new kernels.
3. **Grow the general compiled kernel to small relations,** and retire
   special kernels that do not beat it.
4. **Run the experiments in spec §9:**
   - a law held as table and equation, moving by pressure;
   - costs read from tension;
   - equations reused across domains as hints.
5. **Design the transport version marker,** once something needs it: step
   mode, or saved equations.
