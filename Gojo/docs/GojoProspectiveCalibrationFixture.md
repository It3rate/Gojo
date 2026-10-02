# Learning a continuation before its outcome

Status: exploratory selected contracts, September 7, 2026. This follows the
[two-space communication experiment](GojoTwoSpaceFeedbackFixture.md).

Agreement between spaces is not evidence of predictive usefulness. This
experiment gives a prediction an explicit future target and records it before
the target sample is available. Later feedback can challenge that historical
prediction without rewriting it. The test environment supplies known samples;
the local reader receives only the samples admitted so far.

[Open the nine-step explanation](examples/prospective-calibration.html).

## The smallest experiment

At source times zero and one, a sensor reports three and seven. A selected
affine continuation connects these samples as the frame `0..1 -> 3..7`.
The frame predicts eleven at time two. Eleven has not entered the local space
when that forecast is recorded. When it arrives, the forecast has zero mismatch.

A later forecast predicts fifteen at time three. The sensor instead reports
fourteen. The old prediction stays fifteen with mismatch minus one. A new frame
through `(2,11)` and `(3,14)` predicts seventeen at time four. This is a new
version of a local explanation, not a repair of its earlier prediction.

Two samples determine the parameters **within the chosen affine family**.
They do not discover that family, prove it will continue, establish a tolerance,
or supply confidence. Irregular source-time spacing participates in the frame;
sample arrival order is not a substitute for elapsed source time.

## Implemented ordered structures

The notation below is the selected contract. Every group, frame, open slot and
witness is ordinary Gojo structure, expressible with the existing syntax.

```text
stream = group(publication port, subject, context, local event axis)
state = group(stream, latest local event | open,
              received samples, issued forecasts, assessments)
observation = group(port, source time, sample | open, subject, context)
record request = group(state, observation, local arrival event)
receipt = witness(observation, record request,
                  group(copied observation, copied arrival event), residue, stream)

fit request = group(state, first receipt, second receipt, encoding frame)
fit = witness(group(first receipt, second receipt), fit request,
              source-time frame -> encoded-value frame, residue, encoding frame)
candidate = group(fit, readout frame, open expectation)

issue request = group(state, candidate, target source time, local issue event)
forecast = group(stream, copied issue event, copied target time,
                 group(copied fitted frame, copied readout frame),
                 copied exposed prediction, expectation)
issue = witness(candidate, issue request, forecast, residue, fitted evidence)

assess request = group(state, issue, receipt, local assessment event)
assessment = witness(issue, assess request, observed - predicted,
                     residue, correspondence and timing)
```

The append operations return a new state. The caller carries that state forward
sequentially. Numeric observation does not inject material tension, assign
reward, or change an actuator. An assessment may later motivate an explicitly
connected response.

The readers are in [LocalCalibration.Forecast.cs](../Structures/LocalCalibration.Forecast.cs).
The [authored fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/prospective-calibration.gojo)
contains the actual request patterns, open sensor input, alternative encodings,
actions and feedback correspondence. The driver binds those patterns with
`StructuralPattern.Refine` and invokes the selected readers; action names are
not an interpreter. No parser token or numeric packet version changes.

## Prerequisites this boundary checks

- Source time and local availability are separate axes. Local events advance
  strictly. This fixture admits one source stream in increasing source-time
  order; gaps and pending values are valid, while reordering needs a different
  receipt contract.
- A fit uses two concrete receipts already present in the selected local state.
  A pending sample remains an open observation, never an invented zero.
- The target is later than every received source sample at issuance. The fit's
  evidence must also belong to that state. Historical fitting is allowed;
  claiming an already observed target as a prospective test is refused.
- Assessment uses the exact issued witness and an explicitly selected receipt
  at its target time, from the same stream, subject, context and quantity basis.
  The receipt must have arrived after issuance. Arrival can be delayed.
- Each issue can be assessed once along the supplied state history. Re-reading
  an assessment does not create fresh evidence or another reward.
- Numeric operands of fitted and issued frames are copied. Retargeting the
  original sample, frame, or target does not rewrite an issued prediction.
  This is a retained-evidence contract, not authentication of an external source
  or proof that a caller has supplied its real current history.

Within this single-stream contract, a source port and its strictly increasing
source-time coordinate designate a sample. Assessment matches that designated
target; it does not infer shared `TickRef` identity from equal coordinates.
Local events are ordinal availability/action order, not a synchronized physical
clock. A future source target can have a smaller number than the local issue
event. The fit's entire declared state must be a prefix of the issuing state,
so fitting after seeing feedback cannot be presented against an earlier state.
An independently replayed branch is another possible history, not another
independent physical observation.

Encoding and readout are nondegenerate affine frames in this selected fixture.
A fitted temporal frame may be constant: two equal samples can propose a flat
continuation. That forecast retains the unrepresented time input as residue.
A zero source span cannot fit a continuation. Out-of-range stored results stay
unresolved; intermediate arithmetic uses exact rationals. Open expectations
remain open even when a conditional scalar prediction or mismatch is exact.

## Internal representation and utility

An encoding `x -> -x` can make the fitted internal continuation predict minus
eleven. A readout `z -> -z` exposes eleven. Both inversions remain connected
frames. The endpoint assessment is identical to that of the direct encoding;
no hidden requirement forces the internal value to match the sensor's sign.
The ordinary mathematics of each frame still holds.

There is no built-in pressure to eliminate the two inversions. Such a change
would need a utility advantage, for example lower resource cost while retaining
useful exposed behavior. Endpoint success alone need not distinguish equivalent
internal representations.

Predictive mismatch, desired outcome, and reward are distinct surfaces. A
future reward may evaluate a whole composition without uniquely identifying
which internal participant deserves credit. This experiment does not invent
that assignment, optimize response strength, or claim that a lower internal
mismatch is always more useful. Short- and long-horizon rewards, costs, and
interventions need explicit targets and correspondence of their own.

## Next boundary

The [signal pattern learning plan](GojoSignalPatternLearningPlan-2026-09.md)
now records the existing Core4/Gojo evidence, candidate heuristics and staged
experiments, including repeating and combined signals. It is a planning
checkpoint; this fixture's implemented contract remains the bounded affine
experiment described above.

After this prerequisite, compare candidate continuations by predictions made
before later samples arrive. Preserve alternatives and their applicability;
test growth, reversal, repetition and changed conditions. Then let an explicit
utility rule change participation. Choosing a family, discovering parameters,
testing continuation, selecting an action, and assigning delayed credit remain
separate steps.

## Verification and limits

[Syntax execution tests](../../Gojo.Encoding.Tests/GojoSyntaxForecastTests.cs)
exercise fitting, prospective issuance, surprise, competing continuations,
double inversion, incorrect readout, unequal time spacing, fractions, negative
values, flat continuation, pending observations, missing targets, delayed
assessment, stale/local-clock refusal, duplicate consumption, copied numeric
operands, overflow, and source/subject/context/basis identity. They execute after
numeric restoration, including restoring a pending forecast and later consuming
its outcome. Renaming every authored identifier leaves the behavior unchanged.

The host selects sample windows, model family, readout, action order and events.
This is parameter inference and prospective testing within one chosen family,
not autonomous family discovery, reward optimization, source authentication,
general out-of-order delivery, or a multi-space learning scheduler. This bounded
history implementation copies and inspects retained records; no throughput or
unbounded-memory claim is made. Reward delay, credit assignment, exploration
policy and history compression remain subsequent experiments.

Verification on September 7: solution tests pass **1,382 cases** (782 kernel,
424 encoding, 155 CAD, 21 gear-ladder). This pass adds 31 semantic/demo cases
and the nineteenth authored fixture's numeric-inventory case. All nine demo
rows are compared with executed Gojo state. Browser checks cover each step,
local links, desktop/mobile layout, and the syntax disclosure.
