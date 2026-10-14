# Experiment 02: limits of two-space feedback

Protocol `feedback-limits-v1`, selected before the first experiment-02 run.
This is an exploratory comparison, not preregistered confirmation.

## Question and scope

Which effects come from response strength, delivery order, whole-tick response
resolution, supplied references, or exhaustion of an application controller?
Compare the existing Gojo rules with an independent conventional exact model
under the same inputs and realized event sequence. Matching numbers are a
conformance result, not evidence that Gojo is more intelligent or efficient.

The selected cases are in `StudyProtocol.Cases`: 24 conditions, each run in
three fresh worlds for twelve rounds. All conditions are retained. There is
no randomness or tuned parameter search. Repeated worlds probe this execution
path; identical repeats are not independent evidence of generality.

The default starts at 5 and 9 in A units, with half responses. One A actuator
tick changes the estimate by 1/10 A unit; one B actuator tick changes it by
1/20 A unit. Twenty-four applications are allowed per participant, independently
of the twelve-round observation window. The budget condition allows two.

## Changes selected for comparison

- Full and quarter responses, compared with half.
- Reversed, smaller, equal, and zero-to-one starting estimates.
- Quarter responses with equal physical ticks, ten-times finer ticks, or
  ten-times coarser ticks. These change actuator response resolution, not the
  display's digit count.
- B calibration of 1 or 4 native units per A unit, adjusting its actuator
  calibration to keep physical response resolution fixed.
- No responses despite deliveries; no deliveries; one-way delivery; and
  loss of every other B-to-A message. The disconnected control disables host
  transport; calibration surfaces remain available to the diagnostic observer.
- A one-round delay of both streams; fresh-message sequences in both orders;
  and redelivery of an old message after a newer one.
- Newly supplied references of 4 and 4 selected in round six; conflicting
  references observed without selecting them; and references of 2 and 8
  selected every third round. Reference-only rounds replace peer responses.
- A two-application controller with quarter responses, keeping twelve observed
  rounds so refusal and fractional requests remain visible afterward.

All nonmentioned parameters retain their defaults. Timing comparisons use the
full-response case as their comparison, and resolution comparisons use quarter
response. This is a small stated case set, not an exhaustive parameter study.

## Participant and host boundary

The host chooses cases, initial premises, rounds and an event tape before
running either implementation. Both implementations receive the same tape.
They calculate their own publications, received values, requests and state
changes; the conventional model never reads Gojo answers to choose a response.

The Gojo side reuses the authored two-space fixture. Initial values, endpoint
calibrations, link maps and controllers are parameterized before parsing.
External value changes still pass through the existing response/tension path.
New references are new source observations, not actuator assignments.

```text
local report -> calibration -> published observation
published observation -> selected link -> receipt
receipt + current prediction -> mismatch -> response request
request -> whole actuator change + fractional plan residue
application controller -> accepted/refused application -> new state/history
before -> event + read/application witness -> after -> timeline
```

Each participant sees its own report, state, reference and received messages.
The diagnostic Gojo observer also sees both local predictions and histories;
its broader access supplies no participant action. The conventional reference
has its own rational values, message snapshots, receipt sequence and counters.

## Exact conventional contract

Let `v` be the whole actuator value, `q` actuator ticks per native output unit,
and `s` native output units per common A unit. Its estimate is `v / (q*s)`.
Both publication and delivery retain exact fractions. For a native received
target `t`, a response fraction `1/d` requests `(t - v/q)*q/d` actuator ticks.
Truncate that request toward zero; retain the fraction separately. A free
actuator absorbs the whole change down to zero and retains any unpaid draw as
signed tension. A reference response uses `d=1`.

A successful application, including a zero change, uses one controller step.
Refused applications leave state/history unchanged. Only a newer source
publication is admitted; a consumed receipt cannot be applied twice. The last
plan remains a historical plan until replaced. It is not automatically a new
request or accumulated tension. Silence and a delivered zero are different.

The independent reference uses host `BigInteger` fractions in the evaluation
boundary. It does not call Gojo's exact scalar, prediction, response or observer
readers. No floating-point baseline is needed for this first contract comparison;
decimal chart conversion remains a presentation interface.

## Records and checks

Keep every scheduled event, admission/application result, message publication
and delivery identity, exact estimate, reference mismatch, plan request,
fraction, actuator value/tension, successful application count and outstanding
transport message count. Record application calls separately from successful
applications. Count rejected deliveries separately from losses. After each
round, retain Gojo observations of average, disagreement and movement.

Compare implementations at every event and paired observation, not just the
last value. Report the first and all later disagreements without changing the
scientific success criterion. A contract mismatch is an apparatus finding to
investigate; lack of agreement between participants is a valid outcome.
Do not claim complete attractors from repeating visible estimates.

Ending the observation window does not mean permanently stopping. Zero final
movement, nonzero disagreement, unused response fraction, remaining controller
allowance and messages in flight are separate observations. Costs here are
messages and calls, not physical energy or a benchmark of overall efficiency.

## Amendments and conclusions

Save each run to a new output directory by default. Retain apparatus failures
and explain any fix or changed condition. Do not repair an inconvenient result
by changing response rules, thresholds or the case set after observing it.
Any conclusions in the generated results are limited to these selected rules.
