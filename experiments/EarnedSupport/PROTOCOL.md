# Earned support: frozen exploratory protocol

Written before the campaign outcomes are inspected. This tests an experimental
feedback law, not a universal Gojo economic law or a demonstration of equilibrium.
The preceding NegotiatedPorts and MarketTraining code and results stay unchanged.

## Question and scope

Can later, useful interpretation support continued operation without a manually
refilled fixed reservoir? Does that support subside during unproductive periods,
and does varying its exchange into spendable credits reveal missing regulation?
Exploration and mistakes are legitimate costs. Their existence is not failure;
indefinite consumption without useful outcomes is not evidence of learning.

The native market, supplied consumers, four offers, three providers, downward
negotiation, capacity, cyclic exploration, immediate retention rule, and initial
4,096 credits are unchanged. No comparative supplier selection, growth, new price
increases, or hidden replenishment is added. The new connection is earned backing
to the shared reservoir. That pooled backing may subsidize other consumers;
preferential allocation to the source of success remains a separate question.

## Intended connected structure

```
world
  market: operational sites, equations, live ports, pending promises
  policy: trace resolution, decay frame, reward exchange frame
  each consumer -> trace -> division remainder -> fractional payment remainder
  later evaluated promise -> signed benefit -> trace transition -> reward
  reward -> explicit issuance -> reservoir -> wallets -> suppliers -> expenditure
  ledger: initial + issued <-> all resource owners (including spent)
  consumed observation version + latest settlement witness
```

The structure must retain these meanings if its C# construction/inspection helper
disappears. Recurrence, division, clamping, ledger, and response are Gojo equations,
claims, calibration, and carrier settlement. Host code schedules arrivals and
collects reports. Completed witnesses may be deliberately released after reporting;
live traces, fractional residues, the once-only receipt boundary, and pending
market promises remain connected and serializable.

For each evaluated, demanded outcome, use the existing signed gross benefit `g`:
four credits of supplied valuation per error unit avoided in that consumer's
chosen basis. Direction and fine error units are different; report them separately.
Service prices are internal transfers and are not subtracted again as system cost.

```
u = 7 * trace + 64 * g + previous division remainder
trace', division remainder' = native division of u by 8
v = positive(trace') * gain numerator + previous payment remainder
payment, payment remainder' = native division of v by (64 * gain denominator)
issued' = issued + payment
reservoir' = reservoir + payment
expected total = initial + issued'
```

`trace` has 64 subdivisions per gross-benefit credit; it starts at zero. Preserve
signed division residue rather than pretending every division is exact. Negative
trace reduces future support; it does not retroactively confiscate balances.
The chosen 7/8 recurrence is an authored short persistence, not learned timing.
An evaluated no-service outcome has zero relief and advances the decay. Unknown
outcomes freeze both carries and trace, and earn nothing. No demanded outcome
means no reward opportunity. Positive carryover paid during a flat period is
deferred support, not fresh value produced by that flat observation.

Settlement occurs after the market's current admission, so earned support first
becomes available on the next arrival. The same frame cannot earn support twice.
Credits are explicitly issued, not silently transferred out of an infinite energy
carrier. The supply equation includes all issuance. A growing credit balance is
not by itself greater skill, useful output, or additional service capacity.

## Frozen campaign

- Periodic changing input: the existing six-block jitter generator, 1,440 arrivals
  each for seeds 11, 23, 47 and gains 0, 1/2, 1, 2: twelve paired trials.
- Long flat and return: seed 11; 240 rise-with-jitter arrivals, 960 flat arrivals,
  240 return-to-rise arrivals; gains 0 and 1: two trials.
- Unpredictable changes: seed 11; independent deterministic-hash signs of magnitude
  3, 1,440 arrivals; gains 0 and 1: two trials. Generator labels, seed, and future
  values never enter the interpreting network. This is a finite diagnostic of
  unhelpful prediction, not a proof of mathematical randomness.

All demands remain on. Original funding requests stay 5 and 8 credits per arrival,
bounded by the actual reservoir and wallet capacity. These fixed requests are a
known potential bottleneck even when more backing is available. Four gain values
are different reward exchange policies, **not** a currency redenomination test.
No result-driven tuning or reruns replace these first outcomes. Corrections to a
defective implementation must be documented before rerunning affected trials.

## Observation and interpretation

Keep window summaries every 120 arrivals and a current native checkpoint with
external generator position; avoid retaining every frame. Report demanded and
unresolved outcomes, errors in each consumer basis, signed gross benefit, positive/
zero/harmful paid outcomes, purchases by offer, contacts, service price turnover,
actual operating/contact expenditure, issuance, reservoir, traces, currency ledger,
and native allocation separately from report overhead. All service/contact costs,
including exploration, remain in expenditure. No unpaid counterfactual service is
counted as a delivered answer.

Promising evidence: continued useful operation past closed-pool depletion; backing
responds to later outcomes; losses/flat inputs weaken backing; suppliers receive
actual payments. Negative evidence: unbounded nominal accumulation at unchanged
capacity, positive issuance despite nonpositive net utility, flat-period waste,
stranded credit, starvation preventing recovery, or reward benefiting inefficient
incumbents. These are findings, not conditions to tune away.

The whole system's supplied gross valuation versus modeled expenditure is an
explicit laboratory comparison, not an objective universal value measure. Host
CPU/memory measurements are diagnostic and are not included in the symbolic cost
schedule. Price discovery, comparative selection, value of computation itself,
adaptive resolution, directed supplier chains, inflation/purchasing-power feedback,
and an exploration reserve remain open mechanisms.

## Checks before interpretation

Verify once-only settlement, missing-outcome preservation, signed feedback and
division residue, initial-plus-issued accounting, gain-zero equivalence, graph
roundtrip/continuation, and witness release. Verify supplier payments do not mint
another copy of the final reward. Tests establish the mechanism's contract; they
must not assert that an intended provider wins or that the economy converges.
