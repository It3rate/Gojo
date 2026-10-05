# First offline campaign and utility probes

The runner now continues the existing native market without a display or a
retained replay archive. The first campaign and paired utility fixtures follow
the protocol committed as `d62ba92` before their outcomes were observed. No
native selection law, price parameter or predictor was tuned after results.
This is infrastructure for exploring adaptive structures, not a claim that the
current supplied population has evolved or that a general market has settled.

## How much state is for operation?

The separate [resource audit](../MarketResourceAudit/README.md) distinguishes
retention from computation. For the earlier main saved world:

| Retained native graph | Carriers | Relations | Native packet bytes |
| --- | ---: | ---: | ---: |
| Full world with latest diagnostic frame | 694 | 7,913 | 290,272 |
| Operational structure alone | 171 | 2,182 | Not measured |
| Runnable world with pending promises, without completed frame | 193 | 2,441 | 84,446 |

About 69% of the relations and 71% of the encoded bytes could be omitted from
this continuation without changing any operational decision in the paired
fixtures. Four independently restored worlds matched across 96 subsequent
arrivals, including missing input, changing direction, withdrawal and scarcity.
Operational structure and diagnostic frames overlap, so their counts cannot be
added. This is a measured sufficient continuation, not a proof of a minimal
representation or a retained managed-heap measurement.

The large temporary allocation is a different issue. The existing admission
still allocates about 253–293 MB per 110 arrivals, before the optional observer
and output serialization. It builds both the native calculation and its current
evidence. Compact inspection added about 18–19 MB in the audit, and compact
JSON about 0.184 MB. Simply omitting display or forgetting the completed frame
does not avoid creating that evidence. Separating unavoidable calculation from
avoidable witness construction needs a further implementation experiment.

The new runner therefore reduces retention and saved output now, while leaving
the established native calculation intact. It makes no claim of a faster
learning law or a host-arithmetic shortcut. Reusable financial equations and
more selective witness construction remain possible later improvements.

## Continuous runs with sparse output

Each seed ran 480 arrivals without resetting native learning state, through two
cycles of bounded variations, a larger direction change, flat input, a stronger
rise and return to the original range. This is three independent replications
of the same small population, not a larger connected market.

| Seed | Evaluated demanded outcomes per consumer | Direction error: zero baseline / delivered | Change error: zero baseline / delivered | Final reservoir |
| --- | ---: | ---: | ---: | ---: |
| 11 | 475 | 397 / 70 | 1,569 / 958 | 1,970 |
| 23 | 475 | 397 / 72 | 1,587 / 1,000 | 1,938 |
| 47 | 475 | 397 / 66 | 1,576 / 961 | 2,033 |

Direction and source-change ticks use different bases. These errors include
unserved demanded observations, using the explicit zero/no-service baseline.
They do not establish improvement over a strong persistent-trend predictor.
Each consumer had four unresolved prior obligations and one final outstanding
obligation; a final obligation need not have purchased service. Missing
observations were never replaced by known zeros or convenient later outcomes.

All 1,440 admissions preserved the native 4,096-credit accounting. None emptied
the reservoir, and every consumer had some spendable credit at its admission;
this does not imply every service was affordable. Final asks varied with seed:
`[1,4,2,3]`, `[2,2,3,3]`, and `[1,4,2,3]`. Asks can only fall under this law;
settling at a floor alone would not establish useful market equilibrium.

The figures show useful interpretation through changing input, alongside
substantial connection turnover and repeated unhelpful flat-period purchases.
For seed 11, the two 40-arrival flat windows spent 293 and 263 consumer credits
despite zero evaluated error in both consumer bases during those windows.
Outcome counters belong to their evaluation arrival, which can evaluate an
earlier phase's promise. There is no requirement to anticipate an unforeseen
boundary or adapt instantly; persistent flat-period discovery expense is a
separate and visible mechanism to improve.

Each campaign retained five output files totaling approximately 70–72 KB:
manifest, 12 aggregate window rows, final summary, latest checkpoint inspection
and one replaceable checkpoint. The compressed checkpoint itself was 41–43 KB.
Final native continuations contained 182–193 carriers and 2,318–2,441 relations.
No full per-observation replay was saved. All three campaigns and the six small
utility probes together produced 16 retained result files totaling 395,033 bytes.

Native admission for each 480-arrival run took about 2.4–2.6 seconds and
cumulatively allocated 1.32–1.36 GB on this machine; observer aggregation added
about 0.43–0.44 seconds and 259–263 MB. The runner currently reads into both
window and cumulative counters, so this observer differs from the resource
audit's compact inspector. These are process diagnostics, not simultaneously
resident memory, economic credits or portable performance guarantees.

The learner and cumulative counters retain bounded state. Sparse `windows.jsonl`
still grows on disk with run duration. Releasing completed evidence deliberately
forgets exact detailed history; it is not a promise to replay discarded data.

## Price and value are different from affordability

The six authored [potential-utility fixtures](results/utility-probes/utility-probes.json)
use the unchanged native policy, including cyclic local encounters and paid
contacts. They do not require a preferred winner. Every fixture conserved
resources and issued 31 paid services: 30 were evaluated and one remained pending.

With equivalent direction output and equal operating floors, swapping asks of
2 and 3 left purchase counts unchanged:

| Initial encounter | Offer 0 purchases | Offer 1 purchases |
| --- | ---: | ---: |
| Offer 0 first, either assignment of cheaper ask | 8 | 23 |
| Offer 1 first, either assignment of cheaper ask | 7 | 24 |

The cyclic neighborhood and failed high-price encounters help one incumbent
remain selected longer. Both affordable services pass the current positive-net
renewal threshold. The policy does not compare their net value directly.

In the equal-price value fixtures, the coarse proxy and exact intermediate
change generated gross benefit of 4 and 12 credits per evaluated service,
respectively. Both cost 3, giving ongoing net benefit of 1 versus 9 before
contact expense. Purchases split 16/15 or 15/16 depending on initial encounter.
Recognizing that both are helpful is implemented; preferentially allocating
participation to the more valuable one is not established.

The fixtures alter informational ask carriers through settlement and cost
coordinates through structural retargeting. They do not mint wallet funds or
invent permanent population types. Other high-price offers remain discoverable
and still consume contact expenses, so these are bounded local probes rather
than a complete isolation of every market factor. One explanation of captured
contact expense was corrected after the utility run; no numerical observation,
policy or fixture was changed or rerun.

## Continuation and verification

The native checkpoint contains operational structure and pending promises.
External generator position, seed and observer counters are separate transport
entries. Restore recreates the native world rather than inferring it from
report numbers. Source/build and protocol compatibility are checked on resume.

- Four focused check groups passed, covering frame release, native restore,
  generator continuity and outcome/funding denominators.
- An actual CLI run split at 17 arrivals and resumed for 15 matched a continuous
  32-arrival run in complete normalized native graph, generator state and all
  semantic cumulative counters. Timing and allocation counters were excluded.
- Appended uncommitted report bytes were removed on resume without duplicate
  counting. A metrics file shorter than the committed offset was rejected
  without padding or truncating it further.
- The solution passed all 1,752 tests; no shared-core code changed.

## Next mechanism to investigate

The immediate missing comparison is local: preserve a small native trace of
what the incumbent has earned for this consumer and compare a prospective paid
challenger against it, accounting for discovery and switching costs. Do not
grant an unevaluated candidate future usefulness or free counterfactual work.
This can be tested with the same paired fixtures before expanding population.

Then test how unmet obligations, changed evidence and changed offers create
exploration pressure, with some bounded opportunity for unfamiliar candidates.
The target is useful stability across expected variations, plus capacity for
larger adaptation, not instantaneous recovery or perfect prediction.

The runner can support longer and larger experiments, but the present finite
reservoir eventually drains into a spent sink. Long-duration ecological trials
need an explicitly funded horizon or an explicit environmental replenishment
law before low activity can be interpreted as adaptive quiet. Replicating a
small world does not demonstrate competition at larger connected scale.
Advertising and facilitator structures remain potential-utility hypotheses;
their eventual discovery and growth are separate capabilities to investigate.
