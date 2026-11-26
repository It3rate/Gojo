# Operations: experiments for comparing branches

Status: **comparison stage**, 25 November 2026. This is not a final design. It
lists shared experiments that the three operations branches (`ops_sol`,
`ops_neon` and this one, `claude/youthful-newton-wh29co`) can each run, so we
can compare what works and what doesn't. Part A covers what this branch has
built. Part B proposes new experiments meant to stress the design where it is
weakest. Part C says what to measure, so the results are comparable.

At the time of writing, `ops_sol` (`7ea82356`) and `ops_neon` (`f0267a36`)
contain notes only, both written after master's tension merge. This branch
has code and tests in `Gojo.EncodingC`. The design they follow is in
[GojoOperations-2026-11.md](GojoOperations-2026-11.md), §1.3 and §16. The
review of all three notes is in
[GojoOperationsReview-2026-11.md](GojoOperationsReview-2026-11.md).

## How this branch builds operations

The answers a comparison should test are these:
- **An operation is a relation.** It says what holds and moves nothing:
  `Operations.Balance` (additive, along) and `Operations.Product`
  (multiplicative, across).
- **Running it is a binding** (`Operations.Bind`). Each role of the relation
  reads a law role before the step, writes one after it, or is left free.
  - A free role is residue: kept as small as the relation allows, then
    dropped.
  - Several least completions mean the answer is open, for a structure to
    choose.
  - No completion holds the step. An optional blocked bit says so.
- **Bindings are free; mechanisms are not.** Where a result goes, polarity,
  which role is read back, and folds are bindings. Whether a source is used up
  or kept, and whether units meet along or across, are mechanisms. Both
  mechanisms can be calculated from a law's table and accounting.
- **Material is accounted by master's tension system.** `Tension`, `Fulfill`
  and moves carry it, plus `Flows.Sweep` here for receipts.
- **Step order is structure.** It is set either by declaring rows in order or
  by gating them on a turn bit.

## Part A: built here

Run each with `dotnet test Gojo.EncodingC/Gojo.EncodingC.csproj --filter <Tests>`.

| Id | Experiment | What it shows | Tests |
| --- | --- | --- | --- |
| A1 | Coordinates from laws | used up or kept per role, from `Writes` and `Conserves`; along or across from trades and corners; join, tear out and cancel are one move bound to different faces | `OperationPrimitiveTests` |
| A2 | Counting laws as bindings | `Counts.Sum`, `Tally`, `Shortfall` and `Accumulate` are bindings of a balance, with the same bits as master's step functions | `OperationTests` |
| A3 | The 3 × 4 relation | 425 bits, 25 tuples; answers in every role; open at 0 over 0; nothing for 5 over 2; squares when both inputs read one count | `OperationTests` |
| A4 | Pairing schedules | receipts split 12 as (0, 12), (3, 6) and (3, 9); corners are 0, 3 and 0 as residue; plain moves forget the path | `ProductProcessTests` |
| A5 | Packets | a dock of 10 leaves one packet order open; the gap to 12 is 3 | `ProductProcessTests` |
| A6 | Oscillator and step order | declared order closes in 6 steps with constant shadow energy; together spirals; a toggle-gated together run equals declared order; the events are J run backward | `OscillatorTests` |
| A7 | Order × sign family | relaxation, hold and compounding; oscillation, coasting and runaway | `OscillatorTests` |
| A8 | Save and load | a saved oscillator steps to the same bits | `OscillatorTests` |
| A9 | Coasting multiplier | rate × time booked as debt and settled by delivery; target plus debt equals rate × elapsed at every step; a short stock leaves debt | `CoastingMultiplierTests` |
| A10 | Double `B` four ways | the relation admits only 0; once a step gives 5, 10, 20, 40; under `Settle` it is quiet but blocked; into a new count it settles met | `SettlingVersusTimeTests` |
| A11 | Bit rung | half adder; all 16 functions from AND, XOR and a wire under polarity; ripple adder delay is step order; a lattice equals the wide product; wide transitions are refused | `BitRungTests` |
| A12 | Stiffness | a rate as a count, as constant planes and as a table weight, each refusing or allowing change as its position says | `StiffnessTests` |
| A13 | Polynomials and completion cost | every relation as canonical integer terms; `Bind` keeps only the completions a stated cost prefers, or all of them | `RecognitionTests`, `OperationTests` |
| A14 | Rung numbers | radix-4 rungs match the wide balance on 256 lanes; decimal rungs add, subtract, keep open digits open, and show overflow and borrow; a count groups into rungs and back | `NumberTests` |
| A15 | Recognition | exact relations from full tables; walls as exceptions; one wrong entry as an exception; irregular tables stay tables; probes isolate a product in two questions | `RecognitionTests` |
| A16 | Settling to an equation | a learned table, never complete, plus probes run on the machine, gives the machine's own law exactly | `RecognitionTests` |

## Part B: experiments to exercise it all

Each is small enough to fit `Law.MaxBits`, and each probes something Part A
does not. The column on the right says what the experiment is meant to break.

| Id | Build | Expected if the design holds | Meant to break |
| --- | --- | --- | --- |
| B1 | **Division two ways.** 13 ÷ 3 as the product relation read backward with a remainder role (`a·b + r = c`, `r < a`), and as quotition: tear out 3 until the gap is under 3. Then partition: deal 13 round-robin into 3 counts. | Relation: quotient 4, remainder 1, in one narrowing. Quotition: 4 steps, 1 left as residue. Partition: shares 5, 4, 4, with the extra unit's place visible. All agree on 13. | Whether the relation and the process give the same answer with the remainder visible, and whether partition's unevenness is kept. |
| B2 | **Coupled relations.** `x + y = 5` and `x·y = 6` over small counts, narrowed together, then a supposition in a fork for each surviving `x`. | Narrowing leaves {2, 3} for each value, without saying which goes with which. A supposition of `x = 2` settles (2, 3) and one of `x = 3` settles (3, 2). | Narrowing keeps marginals and loses which value goes with which. Do forks recover the pairs without host search? |
| B3 | **A structure as the target.** Scale every segment of a small structure by `k`: by one rate binding over many lanes, by a frame edit (the unit), and as material with a supply. | The binding and the frame give the same lengths. The material version drains `k·Σ` from supply, or leaves debt. | Whether "multiply a structure" has one meaning or three, and which costs what. |
| B4 | **Damping.** Add a first-order relaxation of the velocity to the oscillator, paid into a friction count. | The orbit spirals in to the landmark. The energy lost equals what the friction count received. It settles blocked at rest or met at the landmark, depending on rounding. | Whether loss can be accounted as material (tension) rather than vanishing. |
| B5 | **Perturbation mid-run.** Move the landmark count halfway through an orbit, and change the coasting rate halfway through booking. Repeat with each on constant planes. | The count moves and the orbit re-centres; the booking's later ticks use the new rate. The constant versions refuse a workspace's change. | Whether perturbability is truly positional, and whether mid-run changes stay legible. |
| B6 | **Provenance by replay.** Run pairing with plain moves while logging arrivals, then replay the log in a fork with sweeps. | The replayed receipts equal a recorded run's. The live run carried no receipt tables. | The cost of recording against the cost of replaying; provenance chosen by the question. |
| B7 | **Crystallization.** Observe coasting at rates 0..3 for 0..3 ticks, build a table with `TransitionLaw.TryFromPairs`, and read it as a relation. | The observed table equals the bound product relation, `SameTable` after alignment. | Whether a process crystallizes into the relation it computes, with nothing named. |
| B8 | **A compiled stand-in.** Replace the crystallized 3 × 3 product with a native multiply behind a lane guard. | Bit-exact parity over every lane. Fallback to the table where a lane is open or would overflow. | The compile contract (§11): guards, overflow made visible, open lanes. |
| B9 | **Change of unit.** 3 bags of 4 become 12 apples via `Convert(1, 4)` between two counts; carries between rungs as moves. | The place-value sum is preserved at every step. Reading backward recovers bags and apples. | Conserving multiplication, and whether rungs are moves rather than arithmetic. |
| B10 | **Contention.** Two heaps write one total in a step taken together, and then are rewritten as one balance with both as inputs. | The first is contested and reported; neither runs. The second settles. | One writer per role, and whether a junction (two live inputs) needs to be a single law. |
| B11 | **False reads.** A coarse read (`⌊x/2⌋`) whose dropped bit must stay visible as residue. A clamped sum that must not be classed as across. A held step that must not read as met. | Residue present; corners only at the bend; blocked bit set. | Negative tests: structure that looks right but hides a loss. |
| B12 | **Boolean over a scope.** "All" and "any" over a selection of lanes, as a tally then a threshold, against a gate. | All is the tally equal to the scope's size; any is a tally of at least one. The gate acts on a flow and the read does not. | Sol's distinction between a Boolean read over a scope and a gate as an action. |
| B13 | **A two-rung coast.** Coast with the rate in two rungs (ones and fours) so packets move a rung at a time. | 3 × 3 delivered in fewer steps than unit delivery, with carries visible. | Whether rungs remove the slow settling of A9 (§16, item 13). |
| B14 | **Swap two arrivals' identities** without changing counts *(ops_sol)*. Query a named pair, remove one participant, form the pair again, save and continue. | The final count and aggregate split are unchanged; a per-pair record shows the latest formation; the saved machine continues the same way. | Whether aggregate receipts are enough, or occurrence identity must be kept. |
| B15 | **One held delivery, four causes** *(ops_sol)*: a closed opportunity, an empty supplier, a full dock, an unbooked order. | Each hold is explained by a different port or ledger, and each names the event that would make the row eligible again. | Whether "why it held" is readable in structure, not only "that it held". |
| B16 | **Joint queries with false local alternatives** *(ops_sol)*: product 5 over `0..3`, products 1 and 9 over `0..4`, and both factor roles bound to one sample. | The dense relation contradicts 5 and narrows 1 and 9 exactly; factored networks stay quiet with open marginals unless a joint check is added. | Whether a compressed or factored form keeps the exact inverse, or declares the loss. |
| B17 | **A one-step budget for a multi-packet conversion** *(ops_sol)*, in declared order, taken together and through any compiled path, then saved and continued. | The same accepted stock, debt and tick count in every form. | Clock parity between canonical and compiled execution. |
| B18 | **Overlapping scoped changes** *(ops_sol)*, with one calibration moved before commit. | The scope refuses the stale patch and names the captured premise; a whole application commits once. | Whether captured premises and grouped commits survive compression. |

## Part C: what to report

For each experiment, a branch should report:
1. **Where the meaning lives:** which laws, roles and bindings carry it, and
   what would be lost if the helper type were deleted.
2. **Size:** states and table bits for each law, against `Law.MaxBits`.
3. **Time:** engine steps to quiet, in declared order and taken together.
4. **Visibility mid-run:** what an intermediate state reads as, including
   residue, debt, blocked bits and receipts.
5. **Backward reads:** which questions the same structure answers in reverse,
   and which need a second structure.
6. **Mechanisms used:** how many distinct laws or relations, and how many are
   bindings of the same one.
7. **Host code:** anything computed outside the structure and written in. This
   is the dispatch-in-disguise check.
8. **What failed:** refusals, contention, gates that would not fit, and
   anything that needed a workaround. This branch's list is
   [§16](GojoOperations-2026-11.md#16-what-is-built-and-what-building-changed).

## Suggested order

Run B11, B10 and B1 first: they test the claims everything else rests on.
B16 and B17 should run against any compressed or compiled form before it is
trusted.
Then run B7 and B13, because crystallization and rungs decide how operations
scale past the table wall. B2 and B3 test whether relations and frames hold
up under more structure, and B4 to B6 test the material and provenance
accounts. B8 waits until something has crystallized.
