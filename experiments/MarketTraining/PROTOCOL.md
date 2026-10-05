# Offline market exploration: first bounded campaign

This protocol is fixed before the new campaign and utility-probe observations.
It tests a way to run the existing negotiated-port world independently, with
small retained state and sparse reporting. It does not add a market-selection
law or claim an evolved population. The supplied provider recipes remain
replaceable experimental scaffolding, not a permanent population taxonomy.

## Native continuation and optional observation

The existing native world has the connected shape:

```text
World = Group(operational structure, pending obligations, latest evidence, version)
Continuation = Group(operational structure, pending obligations, open, version)
```

After reading the current outcome and decision for external counters, release
the latest diagnostic frame. Preserve operational carriers, attachments,
input sites, offers, wallets, willingness and the pending obligations needed
to evaluate the next observation. Missing outcomes remain unresolved; omitted
history is not recoverable and must not be advertised as an exact replay.

This is a retention change, not an allocation optimization: the current
`TryObserve` implementation still constructs its native equations and evidence.
The runner must not replace that work with host arithmetic or silently bypass
qualified reads. Host counters, scenario generation and checkpoint scheduling
are outside the learner. No observer row or phase name enters its decisions.

No live GUI, per-step graph snapshot or indefinitely retained frame list is
required. Emit aggregate windows incrementally and replace one latest native
checkpoint. Checkpoint transport also carries external generator position,
seed and observer accumulator state, separately from the native learner.
Resume must restore the native world, not reconstruct it from report values.
Preserve enough commit-offset information to avoid counting trailing report
rows twice after an interrupted save. Refuse silent reuse of an existing run
directory; resume is explicit and checks implementation compatibility.

## Supplied environment

Retain the existing three providers, four offers, two consumers, operating
costs, exploratory bids, price adjustment, contact cadence and finite funding
law. Both consumer demands remain present. Requested inlets remain 5 and 8
resource ticks per arrival, limited by native headroom and the same finite
4,096-tick reservoir. Do not replenish spent resources or restart the world
between phases. Reservoir depletion is a condition to report, not convergence.

Use an external deterministic seeded input generator with initial level 100.
Each cycle has six blocks of 40 observations:

1. Rise by 3, with bounded seeded jitter of -1, 0 or +1.
2. The same range, with fresh permutations.
3. Fall by 5, with the same bounded jitter.
4. Flat input.
5. Rise by 6, with the same bounded jitter.
6. Return to the initial rise range.

Cycle position 119 has a disclosed missing observation; the environment's
underlying level still advances. Absolute position and level are included in
checkpoint orchestration state. Expected-range variation and larger changes
are observed separately. No requirement to predict the unforeseen boundary,
recover instantly or find a global optimum is imposed.

The first campaign uses seeds **11, 23 and 47**, each for **480 arrivals** in
one continuously adapting world. Aggregate windows contain **40 arrivals**;
save the latest checkpoint every **120 arrivals** and at orderly termination.
Different seeds are independent replications of the same small population,
not a larger connected market. Larger populations remain a later experiment.

## Signs to inspect, not designated winners

Report per consumer and requested basis:

- demanded and readable opportunities, evaluated and unresolved obligations;
- zero-service and delivered error, relief and paid service coverage;
- new paid contacts, quote rejection, blocked capacity and connection turnover;
- service expense and unsuccessful contact expense alongside estimated benefit;
- requested and admitted funding, wallet/reserve balances and depletion;
- asks and continued participation across repeated ranges and changed ranges.

Keep timing, temporary allocation, retained graph size and disk output separate
from the experiment's supplied economic credits. Measure simulation admission,
observer reads and serialization separately when doing the resource audit.
Use warmups and repeat measurements; do not claim a platform-independent speed
or a fundamental lower bound. A removed witness can reduce retention without
reducing its construction cost.

Low activity or low connection turnover alone is not stability. It may be
starvation, missing observations, an inactive demand, or useful settled behavior.
This pass reports the ingredients rather than inventing a success threshold.

## Small potential-utility probes

Use the same unchanged native negotiation policy and source level 100 rising
by 3 for 32 arrivals per fixture. Only the selected consumer requests service;
both existing funding requests remain unchanged. These are authored comparison
fixtures, not autonomous discovery or final species definitions.

**Price probe:** use the direction consumer and the two independent direction
offers. Give both an operating floor of 1, with asks 2 and 3. Set the other two
asks to 10,000 so they can be contacted and negotiate but cannot supply service
within this bounded trial. Run both initial encounter orders and both assignments
of the cheaper ask to the two otherwise equivalent direction offers.

**Value probe:** use the source-change consumer. Offer 0 publishes a direction
packet through its explicit one-source-tick proxy; offer 2 publishes the
intermediate change of 3 once readable. Set both asks to 3 and operating floors
to 1; the two other asks start at 10,000. Run starting with either relevant offer.
This compares differing usefulness at the same price within one requested basis.

Inject authored prices through informational carrier settlement and cost sites
through explicit structural retargeting. Do not change money balances. Retain
capacity, cyclic encounters and all current feedback rules. Record opportunities
to encounter each candidate, actual paid occupancy and prospective outcomes.
There is no required winner. High unused quotes still incur exploration costs;
these fixtures do not claim to isolate every feature of a general market.

## Verification boundary and next decisions

Check that discarding the latest frame does not change subsequent native
behavior. Check checkpoint/resume against uninterrupted execution, including
pending promises and unknown observations. Verify conservation and report
units; never turn unresolved reads into zeros for convenience.

The next behavioral law should follow the observations. Likely candidates are
comparison with the consumer's incumbent and pressure-driven exploration, but
neither is tuned into this campaign. Advertising or facilitator structures can
later be introduced as explicit hypotheses, then replaced or grown if utility
supports them. A successful runner is infrastructure for exploring those laws,
not evidence that autonomous evolution is already implemented.
