# Experiment 02: what changed the answer?

September 15, 2026. Protocol `feedback-limits-v1`; recorded run directory
`20260915-230957`. This is an exploratory result under selected local rules.

[Open the viewer](/minds/feedback-limits/?case=quarter),
[read the complete table](/minds/notes/limits-results/),
or [inspect the rules selected before running](/minds/notes/limits-protocol/).

## What ran

Twenty-four conditions, three fresh worlds each, twelve observed rounds per
world: **72 completed runs, no apparatus failures, no mismatches against the
independent exact reference**. The comparison covered 5,229 event observations
and 936 paired observations, including 40 recorded fields and acceptance
outcomes. Each condition's three recorded trajectories matched. No randomness
was introduced; this is limited evidence about repeatability of this path,
not a general replay guarantee or 72 independent demonstrations of a claim.

The ordinary reference used its own rational arithmetic, copied messages,
fractional requests and history counters. It did not borrow Gojo predictions
or plans. Matching it is the expected contract check. This experiment establishes
no numerical superiority, learned policy, or performance advantage for Gojo.

## Quiet disagreement can come from resolution

The quarter-response run from experiment 01 continued past its original six
rounds. A stayed at 6.8; B moved from 7.05 to 6.95 by round eight. Neither estimate
changed in rounds nine through twelve, despite twelve unused applications per
participant at the end.

The remaining gap is **0.15**. A's latest request is **3/8 of an actuator tick**;
B's is **−3/4 of an actuator tick**. Both truncate toward zero. These requests
remain visible as plan residue; neither carrier has unresolved tension.
The fractions are not accumulated across repeated requests under this rule.

| Quarter-response condition | A tick, in common units | B tick, in common units | Final A | Final B | Remaining gap |
| --- | ---: | ---: | ---: | ---: | ---: |
| Coarser response | 1 | 1/2 | 6 | 7.5 | 1.5 |
| Original response resolution | 1/10 | 1/20 | 6.8 | 6.95 | 0.15 |
| Finer response | 1/100 | 1/200 | 6.98 | 6.995 | 0.015 |
| Equal physical tick sizes | 1/10 | 1/10 | 6.9 | 7.1 | 0.2 |

The tenfold gap changes are observations of these selected cases, not a general
convergence law. Unequal response resolutions also permit the average to drift
from its initial value. Equal physical ticks preserve the average here while
still leaving disagreement.

This suggests a useful future comparison of explicitly selected refinement or
accumulation policies. It does **not** justify turning read residue into tension
or adding a hidden accumulator to make this experiment agree. Any such policy
would be a new condition, with its own meaning, cost and tests.

## Timing can select different agreements

With full responses and captured messages, the estimates keep swapping 5 and 9.
When A responds first and publishes its updated view before B responds, both
reach 9. Reversing that order gives 5 and 5.

Adding a one-round delay produces paired readings that alternate between an
exchange and a quiet interval. Two messages remain in transport at the end.
Quiet intervals therefore do not establish that future behavior is settled.
These are observed repeated estimates, not proof of a complete dynamical
attractor including every history and controller state.

A deliberately redelivered old publication was refused after the newer one
arrived. The refusal remained in the event record; the latest receipt was not
replaced, and the next response used the newer observation.

## Equal visible outcomes can have different causes

No-response and no-delivery controls both remain at 5 and 9. Their records
distinguish 24 admitted messages with no applications from 24 dropped messages
and no receipts. One-way delivery reaches 5 and 5.05, with a remaining fractional
request on B. Losing every other B message has no effect after this particular
half-response pair has already agreed; that does not establish general
robustness to loss.

The two-application control remains at 6.5 and 7.5. Its later requests are
refused because the controller is exhausted, with **20 refused application
calls** across the pair. That is a different cause of quiet disagreement from
the fractional requests with spare allowance in the quarter-response run.

Changing B's unit label/calibration from two native units per A unit to one or
four, while preserving its physical tick size, leaves the common-unit
trajectory unchanged. Changing units and changing resolution are different
interventions.

## Seeing a reference and acting on it are separate

Observing new conflicting references of 2 and 8 without selecting them leaves
peer agreement at 7 intact. The observer reports the reference mismatches.
Selecting newly supplied references of 4 and 4 moves both spaces to 4.

Selecting references of 2 and 8 every third round alternates between those
values and peer agreement at 5 and 5. Both local references can be satisfied
while the peers disagree; agreement between peers can leave both references
unsatisfied. These references are supplied premises, not proof of truth.
No mechanism learned which requirement deserved priority.

## What this adds, and what remains open

The experiment now separates several reasons for a quiet read: agreement,
sub-tick requests, missing deliveries, selected inaction, delayed messages and
controller exhaustion. Gojo retains the related structure and exposes these
distinctions. The equally instrumented conventional model can retain them too.

The next roadmap experiment remains prospective prediction. The main open
learning question is whether a participant can use these distinctions to choose
useful work, resolution or connections. Here those choices were authored and
recorded. We have exercised the tools, rather than established that skill.

## Implementation and validation

- [Case matrix and event-tape builder](/minds/source/Minds/FeedbackLimits/StudyProtocol.cs.txt).
- [Gojo execution and observations](/minds/source/Minds/FeedbackLimits/GojoStudy.cs.txt).
- [Independent exact reference](/minds/source/Minds/FeedbackLimits/ConventionalReference.cs.txt).
- [Event/round comparison](/minds/source/Minds/FeedbackLimits/StudyComparison.cs.txt).
- [Focused contract tests](/minds/source/Minds.Tests/FeedbackLimitsTests.cs.txt).

The Gojo kernel response law was not changed. The local adapter gained a
nonthrowing receipt path and explicit new reference observations. Tests cover
all cases against the independent reference, stale admission, unit changes,
controller refusals, observation without actuator mutation, and detection of a
deliberately changed comparison record. Experiment-01 tests remain in place.

No condition or response rule was revised after the first experiment-02 run.
Exact replay, physical energy measurements, probabilistic trust, and automatic
resolution selection remain outside this experiment.
