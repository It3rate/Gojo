# First local service growth results

The structural substitution works. A paid consumer of an upstream service can
construct an independent implementation, validate it against later observations,
release the upstream dependency, and sell through its existing live outlet.
Public connection counts and bounded local discovery also work as mechanisms.
The first campaign does **not** establish that popularity improves choices, that
promotion establishes lasting usefulness, or that investment is profit-driven.

The original protocol was committed as `9585c75`; implementation and the exact
executable policy were frozen as `6710100` before this campaign. All 21 runs used
one source/build fingerprint and one protocol fingerprint. There were no campaign
reruns or settings chosen after outcomes. A consumer-bound reader mismatch was
fixed during contract checks, before the campaign began.

## Paired results

These are totals across three seeds for each changing-demand row, and one seed
for the unpredictable row. Cost means actual operating, contact, construction
and shadow expenditure. Fees between participants are internal transfers. Gross
benefit is the supplied calibration of error avoided versus predicting no change;
it is neither spendable currency nor a universal measure of value.

| Fixture | Buy-only cost | Make cost, no hint | Make cost, public hint | Gross benefit in all three policies |
|---|---:|---:|---:|---:|
| Changing demand, restricted access | 4,347 | 3,172 | 3,172 | 11,944 |
| Changing demand, direct access | 4,456 | 3,500 | 3,500 | 12,376 |
| Unpredictable sign, direct access | 1,623 | 1,440 | 1,440 | -252 |

Under the **supplied** costs and repricing law, construction saves 27.0% with
restricted access and 21.5% with direct access on the changing-demand fixtures.
Interpretation errors are unchanged within each paired comparison. This saving
includes the construction and shadow trials; it is not just a lower supplier bill.
The fine service remains useful when the second consumer requests magnitude:
60, 70 and 68 paid fine outcomes are evaluated in the three seeds, identically
across the paired policies. Coarse output cannot pass the finer eligibility read.

Every changing-demand construction promotes at arrival 6 after four delayed
supporting outcomes. Upstream input purchases then stop: the intermediary's
input payments fall from 1,284 to 16 per restricted-access run, and from 920 to
16 per direct-access run. The fine supplier remains independently available to
the finer consumer. Perturbation checks show that its old output no longer feeds
the promoted intermediary's calculation.

The public-use hint changes **no observed behavior** here. All seven make/no-hint
versus make/hint pairs match in all semantic fields of all 42 corresponding
60-arrival windows and current-state observations. Timing/allocation fields and
the native policy weight are intentionally different. With only two supplied
offers, this is a weak test of the hint's potential. It gives no justification
for increasing its weight or claiming a discovery advantage.

Raw evidence is in [first-campaign](results/first-campaign); the compact comparison
is [comparison.json](results/first-campaign/comparison.json). `Summarize.ps1`
recreates that observer-only comparison from the preserved runs.

## Weaknesses exposed

**A lucky run can promote a poor model.** Both unpredictable make arms promote
at arrival 17, after 15 candidate outcomes. Support then stays at 4 because
promotion is irreversible in this prototype. Overall gross benefit is -252,
the same as buy-only, with 140 helpful and 147 harmful paid outcomes. Lower
operating cost is real, but this is cheaper unhelpful operation. Four net support
steps do not establish lasting usefulness or calibrated certainty.

**Local search is bounded but insufficiently selective.** In seed 11, the second
consumer pays to inspect the incompatible coarse advertisement 83 times during
its fine-demand phase. Contact costs consume 488 of 1,056 credits with restricted
access, and 610 of 1,176 with direct access. On the unpredictable fixture they
consume 1,168 of 1,440 credits: 81.1% of expenditure. Repeated rejection does not
yet change future attention or search cadence. The windows containing flat periods
also include zero-benefit paid uses; the sparse summaries do not isolate exact
flat-only spending, and that detail should not be invented afterward.

**Investment is not yet chosen for its own return.** The candidate is built at
the first ready affordable opportunity, and promotion forces price 6 to 2.
Seed-11 restricted-access intermediary receipts minus expenses fall from 1,023
to 614 while system cost falls. The prescribed discount passes savings to buyers.
Conversely, the unpredictable make run leaves the intermediary with positive
margin 346 despite negative aggregate consumer benefit. Sales and local profit
can coexist with poor overall use, especially under supplied consumer funding.
These are not demonstrations of autonomous profit-maximizing pricing or investment.

## What the public signal means

An advertisement exposes active connections and qualified active connections.
Each physical client contributes at most one of each. Repeated registration and
repeated good outcomes cannot add transaction votes. Detachment retracts the
contribution. The qualified count means that the client's latest evaluated use
helped in **its own interpretation**, not that the service is generally accurate.

Ordinary preference still requires compatibility, capacity, affordability and a
positive private margin. Public support is capped at two contributions. Untested
offers and periodic non-incumbent trials retain a route into paid evaluation.
Reading an offer uses maintained counts; it does not enumerate the clientele.
Queries inspect at most two advertisements from at most eight local references.

There are several boundaries:

- No count measures independence, age, confidence, outcome frequency or magnitude
  of benefit. A copied client with a genuinely new identity can add another vote;
  this is not an identity or collusion defense.
- Upstream qualification is propagated downstream evidence. It does not isolate
  the supplier's contribution; the latest evaluated downstream use can overwrite
  the intermediary's one qualification at that supplier.
- Missing data preserve an unknown verdict in the counter helper. This network
  releases the affected route, retracting its public contribution, rather than
  retaining a stale endorsement through missing input or a changed read basis.
- The replacement retains its existing advertised coarse contract and port.
  Neighborhoods and routes are supplied. This does not yet create new producers,
  discover unknown neighbors, or evolve advertising/facilitator structures.

## Native structure and actual resource use

No kernel API was changed. This is a new experiment using existing Gojo arithmetic,
claims, live publication, transfers, relation structure and native serialization.
The interpretation, paid dependency, publication gate, resource accounts, candidate
binding, support and public count contributions are connected native structures.
The candidate recipe is independently constructed from already exposed raw input;
it is supplied, not inferred from a private supplier's black-box behavior.

Host code schedules observations and the authored policy, installs the candidate,
and gathers reports. It still builds many local read/comparison witnesses during
each step. The structure is not an autonomous self-scheduling population. The
current port can be shared within one arrival; its presence resets on the next.
Pending promises retain the small amount of historical information needed for
later evaluation, and completed diagnostic frames leave the retained graph.

Across the final checkpoints there are **224–230 carriers and 2,797–2,868 relations**,
with compressed checkpoints of 43,989–50,051 bytes. The successful continuation
audit at 32 observations and a promoted campaign at 360 both retain 230 carriers
and 2,866 relations; this path does not archive every completed observation.

However, the 7,560 native steps allocate **136.46 GB cumulatively**, about
**17.2 MiB per observation**, and take 317 seconds measured around the native pass.
Observer aggregation plus releasing frames adds 3.75 GB and 5.98 seconds. That is
about 2.7% of measured per-arrival allocation and 1.9% of the corresponding time.
Final checkpoint-plus-inspection operations take 7–43 ms and about 4.4 MB of
allocation each, measured separately. These are cumulative allocations, not peak
resident memory; initial setup and all other runner overhead are outside those
step counters. Other validation ran concurrently, so timing is not a benchmark.

Most measured churn is in the native calculation path, not report storage. Some
of that path constructs witnesses for inspection, so the measurements do not prove
all of it is essential to operation. Reusing more equation/comparison structure is
still an important efficiency opportunity. The explicit operating prices above
are authored costs, not measurements of actual CPU or allocation complexity.

Likewise, bounded discovery is not proof of million-actor scalability. Registration
can scan a service's local incidences; settlement/disconnection can traverse a
connected graph. This runner visits its supplied actors each arrival and prepares
currency calibrations for every owner pair. Those fixture mechanisms must not be
mistaken for a scalable distributed scheduler.

## Validation and next experiment

The Release build is clean. Five discovery check groups and ten service integration
groups pass. The repository suite passes **1,752 tests**. Every campaign conserves
the initial currency exactly; all four native expense categories reconcile to
spent resources, and no search exceeds two inspected offers.

An actual CLI audit compared 32 uninterrupted arrivals against 5 plus a resumed
27, crossing promotion at arrival 6. Full normalized native graphs, source state
and semantic report totals agree; promotion and investment happen exactly once.
Trailing uncheckpointed report bytes are removed on resume. A report shorter than
its checkpoint is rejected without padding it. Audit material is ignored under
`obj/continuation-audit`; the production campaign checkpoints are preserved.

The next useful step is a small continuation of this experiment:

1. Make poor later outcomes able to reopen a promoted structure's choice; allow
   local search to become dormant after repeated incompatible or unhelpful trials,
   with new input/tension reopening it.
2. Use own actual expenditure and realized demand to test an investment/price
   choice, rather than automatically constructing and imposing a discount.
3. Give the same two-offer inspection budget a richer, still local neighborhood
   of a few near alternatives and one newcomer. That can test whether public use
   reduces discovery cost without suppressing independent trial evidence.

Merger, splitting, arbitrary recipe invention and a much larger population can
wait. The mechanisms needed for this local experiment already fit Gojo; the
missing evidence concerns the supplied adaptive policies and their computational
cost, not another general layer of concept labels.
