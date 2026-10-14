# Experiment 03: what the forecasts showed

Twelve selected sequences ran under `predictions-v1`: three development checks
and nine evaluation cases. There were no apparatus failures or field differences
from the independent exact implementation of the same line rule. No kernel law
or prediction policy changed after seeing the evaluation results.

Open the [prediction viewer](/minds/predictions/).
Uncheck “Reveal this target's observation” to inspect the forecast before its
outcome arrives. The [protocol](/minds/notes/prediction-protocol/),
[complete result table](/minds/notes/prediction-results/) and
[all input tapes](/minds/predictions/cases.json) are retained.

## The useful result is that it can be wrong visibly

At the reversal, the past samples are 8 and 11, so the line predicts 14.
The next observation is 8. Its mismatch is -6, and that old prediction stays
14. Using the now-available 11 and 8, the next line predicts 5 correctly.
That is a new fit from new observations, not a correction of history and not
a learned decision to use a different kind of model.

The line can do better over a whole sequence while making a worse mistake
at its turning point. In this case its mean absolute error is 6/7, versus 3
for hold-last, but its largest miss is 6 versus 3. Both readings matter.

## Where each rule helped

Errors below use exactly the same targets with observed outcomes.

| Evaluation case | Shared targets | Line mean absolute error | Hold-last mean absolute error | What happened |
| --- | ---: | ---: | ---: | --- |
| Constant below zero | 6 | 0 | 0 | Both continue the constant; Gojo retains the flat frame's time residue. |
| Unequal-time ramp | 5 | 0 | 39/5 | The line uses elapsed source time correctly. |
| Reversal | 7 | 6/7 | 3 | One large miss, then the newly fitted line follows the fall. |
| Alternating values | 8 | 12 | 6 | The line extends the wrong direction every time. |
| Sudden step | 7 | 16/7 | 8/7 | Both miss the jump; the line also overshoots the following plateau. |
| Gaps across a change | 5 | 26/15 | 11/5 | The older line makes a large miss when measurements return. |
| Missing initial observations | 3 | 1/2 | 3 | Prediction waits for enough known samples; elapsed time includes the gaps. |
| Final blackout | 1 | 0 | 3 | Only one shared target can be scored; five later predictions remain unassessed. |
| Entirely missing stream | 0 | unavailable | unavailable | Neither method has a value to continue. |

The alternating and step sequences are clear losses for the line rule. The
blackout is especially easy to misread: zero error on its one scored target
does not validate any prediction made afterward. These selected examples do
not establish a population-wide winner, and their sizes should not be treated
as independent statistical evidence for a general claim.

## Missing values remain different from wrong predictions

In the gaps case, the last known samples at times 1 and 2 are 3 and 5. The
line predicts 7 and 9 for times 3 and 4; both outcomes are missing, so neither
is assigned an error. At time 5 it predicts 11, but the observation is 4.
That is an assessable miss of -7, with the newest training sample three time
units old. The next line spans the actual source-time gap and predicts 11/3.

The runner skips open samples when choosing its two training receipts. This is
an authored rule, not Gojo discovering which observations deserve attention.
There is no age cutoff or confidence adjustment. The old line keeps being
extended during a blackout; the records expose that limitation.

Hold-last can issue after one known observation, whereas the line needs two.
The full records show each method's coverage and hold-last's error on its
additional targets. A control that issues nothing has no score, rather than
a perfect score. All averaging uses absolute error so opposite misses cannot
cancel.

## What is implemented, and what is still missing

Implemented here: a sequential experiment adapter over existing Gojo forecast
structure, exact issue/arrival/assessment records, fixed selection of known
receipts, independent calculations, missing-outcome accounting, a connected
timeline, a before/after viewer and preserved input tapes. Source code:

- [Participant adapter](/minds/source/Minds/Predictions/ForecastSpace.cs.txt)
- [Input and outcome driver](/minds/source/Minds/Predictions/PredictionStudy.cs.txt)
- [External reference and measurements](/minds/source/Minds/Predictions/PredictionEvaluation.cs.txt)
- [Exact case definitions](/minds/source/Minds/Predictions/PredictionProtocol.cs.txt)
- [Report and journal writer](/minds/source/Minds/Presentation/PredictionsReport.cs.txt)
- [Boundary tests](/minds/source/Minds.Tests/PredictionTests.cs.txt)

The participant prediction and signed mismatch come from Gojo's existing
connected frames. External rational arithmetic is used for the independent
reference and summary metrics; browser numbers draw the plot. None feeds back
into prediction. Matching ordinary exact arithmetic validates this selected
calculation; it establishes no numerical advantage over that arithmetic.

Work is recorded as API calls and retained receipts/issues/assessments. Those
counts are not timings, allocation measurements or energy. The current
implementation retains histories and repeatedly reads them; it has no bounded
memory or throughput claim. The JSON files are diagnostic records, not saved
worlds with an exact replay promise.

This makes several next questions concrete without requiring a fix now:

- Keep line and hold-last as distinct connected candidates, with a genuine
  Gojo hold-last construction, before letting past performance change their
  contribution. Two equal training samples already allow a flat fitted line,
  but that is not the same policy as copying the most recent value.
- Specify how later misses can request a closer read or change model use.
  Here mismatch is a retained observation, not automatically a goal, reward
  or material tension. Experiment 04 can test a broader observer's usefulness.
- Decide how age and missingness should affect a prediction only when a task
  supplies a reason. Do not turn them into an unexplained confidence number.
- Test any new selection or mixture rule on new sequences. The evaluation
  cases published here have now been inspected and cannot serve as untouched
  confirmation for changes chosen because of these results.

This is a small test of prospective expectation and its failure. It is not
yet multi-source interpretation, consensus, autonomous model discovery, or
evidence for the stronger claims discussed in Ingressing Minds.
