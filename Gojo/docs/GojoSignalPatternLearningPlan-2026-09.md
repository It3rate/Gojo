# Plan for learning useful patterns in signals

Status: exploratory plan, September 8, 2026. No new implementation or syntax
contract is introduced here. The inspected Gojo baseline is commit `8ddc024`.
This plan follows the [prospective calibration fixture](GojoProspectiveCalibrationFixture.md)
and complements the [CAD and electronics assessment](GojoCadEcadSyntaxAssessment-2026-09.md).

## The objective and the boundary already crossed

The objective is to discover locally useful continuations of changing inputs,
test them against later observations, and eventually let usefulness change the
participation of their structures. Agreement between neighbours is useful
communication, but does not by itself establish predictive skill. A model that
describes yesterday's samples has made a different claim from one that predicted
today's sample before it arrived.

The present forecast fixture establishes that distinction within one explicitly
chosen affine family. It does not discover the family, learn confidence, or
optimize reward. We should retain that honest boundary while expanding the
inventory of possible continuations.

Locality means the relations selected for an explanation. A space predicting a
planet's motion may deliberately omit its weather. A space predicting a circuit
voltage may omit the detailed motion of individual charges. These exclusions
are part of the model's scope; they are neither claims that the omitted effects
are zero nor obligations to investigate every unknown.

## Existing work worth carrying forward

| Existing source | What it provides | What it does not establish |
| --- | --- | --- |
| Historical Core4 [PatternState](../../../Core4/Core4.Runtime/Calibration/PatternState.cs) and [tests](../../../Core4/Core4.Tests/Calibration/PatternStateTests.cs) | Three-sample windows, differences, change in differences, ratios, reversals, range, smoothed fit pressures and time since reversal. | A discovered period, a uniquely identified generating law, or current Gojo semantics. |
| Historical Core4 [Calibrater](../../../Core4/Core4.Runtime/Calibration/Calibrater.cs) | Experiments with changing local origins, units and responses to samples. | A specification to port unchanged. |
| Current [equation structures](../Equations/README.md), [affine constructions](GojoAffineConstructionFixture.md) and [bounded quantities](GojoBoundedQuantityFixture.md) | Composable arithmetic, selected exact reads, explicit operands, open structure and conditional bounds. | An automatic search through signal models or a general distribution algebra. |
| [Landmark calibration](GojoLandmarkCalibrationFixture.md) | Alternative interpretations of a report, feedback and explicit response. | Evidence that an approximate landmark contains a tolerance or that its source is trustworthy. |
| [Two-space feedback](GojoTwoSpaceFeedbackFixture.md) | Local requests, response strength, retained disagreements and actual settlement. | Predictive learning from static agreement. |
| [LocalCalibration.Forecast](../Structures/LocalCalibration.Forecast.cs) and [syntax tests](../../Tests/Gojo.Encoding.Tests/GojoSyntaxForecastTests.cs) | Receipts, a fit from selected past samples, future issuance, later assessment, copied numeric operands and replay. | Autonomous family selection, a general delivery scheduler, delayed reward allocation or history compression. |

The sibling Core4 links assume the local repositories remain beside Gojo. They
are historical evidence, not dependencies of this plan. The
[legacy inventory](LegacyCore4IdeaInventory.md) provides broader context.

Several details in Core4 deserve reinterpretation before reuse. `LastSlope`
is a difference between samples, not a rate divided by elapsed source time.
Ratios depend on a meaningful origin and need an explicit unresolved result
near a zero denominator. Additive and ratio residuals need a common evaluation
basis before their sizes can be compared. The implementation has availability
flags but also returns zero-valued reads for incomplete windows; a Gojo
continuation must preserve the unavailable observation. Its integer smoothing
can lose small adjustments, so any later material response must account for
unabsorbed change. These are audit questions for a future adaptation, not
changes to historical code in this pass.

## What a signal observation needs to retain

The next fixtures should retain the existing separation between source time
and local availability, and make the following connected roles explicit:

- The source port, observed subject, quantity basis and context.
- The acquisition time or interval, clock calibration and timing uncertainty.
- When the observation became available locally, separately from when it happened.
- Whether the value is instantaneous, an interval average, an integral, an
  event count or another selected read. These give different equations.
- The value, open parts, source resolution and any justified expectation model.
- Shared origin with other observations when that relationship is known, without
  requiring the receiver to investigate an entire external provenance chain.
- Any filter, resampling, delay or coordinate transformation used to obtain the read.

For example, a sensor's one-second average and its end-of-second value can
differ even if both arrive labelled with the same time. A reported zero and a
missing sample must not coincide structurally. A prediction for a time interval
also needs a different target contract from the existing single future point.

The current receipt contract admits one stream in increasing source-time order.
Multiple clocks, out-of-order arrivals, corrections to old measurements and
interval-valued observations are extensions to specify, not existing promises.

## Heuristics to investigate

These are ways to propose candidate structure. None should silently declare
the source's true algorithm. Their assumptions, windows and unresolved cases
belong with their evidence.

| Observed feature | Candidate mechanism | Useful evidence and principal trap |
| --- | --- | --- |
| Little change | Hold the last value; constant local level | Always keep this inexpensive baseline. A short flat interval can hide a turning point or an unobserved event. |
| Similar increments | Affine continuation using actual elapsed time | Compare rates when time gaps differ. Continued increase alone does not imply constant slope. |
| Similar changes in rate | Quadratic or another local polynomial | Use time-aware divided differences. Higher order fits can explain a short window and extrapolate badly. |
| Similar ratios | Multiplicative growth about a selected baseline | Compare equal durations or model duration explicitly. Ratios of raw values change when an arbitrary offset is added. |
| Up, down, up | Reversal events and a possible repeating phase | Retain candidate extrema and their timing. Three samples show at most local reversal evidence, not a reliable period. |
| Repeated shapes at a lag | Autocorrelation or direct lagged correspondence | Detrend when justified; preserve gaps and window length. A repeated source, harmonic or trend can produce misleading agreement. |
| Concentrated frequency content | Sinusoidal components or a periodic template | A spectrum proposes frequency content, not a causal decomposition. Windowing, offset and aliasing matter. |
| Similar values at the same cycle position | Phase folding and a within-cycle template | Keep phase alignment, cycle count and cycle-to-cycle variation. Do not force a changing period into one immutable template. |
| Ramps followed by sharp returns | Piecewise affine segments and reset events | Distinguish a sawtooth reset, a triangle's change of slope, saturation and missing data. |
| Frequency or shape changes over time | Windowed spectra, wavelets or local phase tracking | Short windows locate change better but provide less evidence for slow repetition. |
| One channel follows another | Delayed coupling, a filter or a driven response | Keep the delay and common-source alternatives. Correlation alone does not identify causal direction. |
| Compact recurrences explain future samples | Autoregressive/state-space candidates; later sparse equation discovery | A convenient state representation need not be unique or match the physical source's internals. |

For concrete reference methods, NIST describes
[autocorrelation](https://www.itl.nist.gov/div898/handbook/eda/section3/eda35c.htm)
and [spectral plots](https://www.itl.nist.gov/div898/handbook/eda/section3/eda33r.htm).
SciPy's [peak detection](https://docs.scipy.org/doc/scipy/reference/generated/scipy.signal.find_peaks.html)
shows how distance, prominence and width can constrain candidate extrema.
These are selected read policies, not universal thresholds embedded in numbers.

Unevenly timed samples deserve their own fixtures. The generalized
[Lomb–Scargle method](https://docs.scipy.org/doc/scipy/reference/generated/scipy.signal.lombscargle.html)
fits sinusoidal terms at candidate frequencies using the supplied times and can
fit a floating offset. It is a useful comparison method, not permission to
ignore the observation process or a requirement that Gojo store floating-point
arrays as its canonical signal structure.

[Short-time Fourier analysis](https://docs.scipy.org/doc/scipy/reference/generated/scipy.signal.ShortTimeFFT.html)
and [continuous wavelets](https://pywavelets.readthedocs.io/en/latest/ref/cwt.html)
are references for time-local frequency/scale evidence. Filtering and
[local polynomial smoothing](https://docs.scipy.org/doc/scipy/reference/generated/scipy.signal.savgol_filter.html)
can make features readable, but a centred window using later samples is a
retrospective read. It cannot supply an earlier online forecast without
recording the resulting delay. Derived evidence should retain its source window.

The later [SINDy work by Brunton, Proctor and Kutz](https://arxiv.org/abs/1509.03580)
is relevant to choosing a small set of governing terms from a larger candidate
family. This is a future comparison point once timing, state, derivatives,
noise assumptions and prospective testing are established; it is not the
immediate next implementation.

## How repeating and combined structures could look

The following is mathematical planning notation, not additional `gojo 1`
grammar or an implemented signal reader.

The [contour-grounded rotation review](GojoContourRotationAndLocalInterfaces-2026-09.md)
also clarifies the geometric side of phase. A numeric period or trigonometric
formula does not by itself construct a physical contour or rotation. The
participating recurrence/traversal and its calibration must remain explicit;
angular units are selected reads of that construction.

For a fixed positive period `T`, define an unwrapped cycle coordinate
`u(t) = phi0 + (t - t0)/T`. Its integer cycle count and wrapped phase
`phi = u - floor(u)` are different reads. The convention here is `0 <= phi < 1`.
Retain the unwrapped correspondence: wrapping a phase does not erase elapsed
time or discard physical material.

An ideal sinusoid can then be described by a baseline, amplitude and phase
law: `x(t) = b + A sin(2*pi*phi(t))`. A sawtooth is a ramp over this phase with
an explicit reset; a triangle connects an ascending and descending ramp;
a pulse adds duty and boundary conventions. Ideal sawtooth and pulse signals
have discontinuities. A continuously evolving physical source may instead have
finite transition times. SciPy explicitly notes that its ideal
[sawtooth](https://docs.scipy.org/doc/scipy/reference/generated/scipy.signal.sawtooth.html)
is not band-limited and has infinitely many harmonics, which can alias when
sampled. Matching a few samples therefore does not identify the underlying wave.

Use an exact rational ramp first. For illustration, samples `0, 1, 2` are
consistent with indefinite linear growth and a three-step ramp that returns
to zero on the next sample. These candidates predict `3` and `0`, respectively.
The families should make different future claims and wait for evidence;
neither deserves certainty because it explains three observations. Periods and
phases may remain several competing candidates even after one reset.

A uniformly sampled, baseline-subtracted sinusoid also has the recurrence
`z[n+1] = 2*cos(omega*dt)*z[n] - z[n-1]`. This gives a compact local state
candidate. It does not remove the need to identify the coefficient, specify
sampling, or compare alternative frequencies. General sine, cosine and their
irrational results need a defining construction plus a justified approximation
and refinement contract. Finite rational ticks must not masquerade as exact
values of arbitrary transcendental functions.

Combinations need distinct topology:

- **Superposition:** both component contributions participate in one sum.
- **Alternatives:** competing explanations predict the same exposed quantity;
  their predictions are not automatically added together.
- **Modulation:** one evolving structure changes another's amplitude or phase.
- **Filtering and delay:** an output depends on a retained history of an input.
- **Regime change:** different laws apply on connected time regions, with
  explicit transition evidence and possibly unresolved boundaries.

A sum of nearby frequencies may exhibit beating. A strong harmonic may be
mistaken for the fundamental. A small portion of a long sinusoid may resemble a
line. These are useful adversarial fixtures because more than one explanation
can remain valid at the available resolution and horizon. Uniform samples also
permit aliases: no fitting strategy can recover detail absent from the
observation contract without additional assumptions or evidence.

## Uncertainty, attention and assessment

Keep a conditional prediction separate from uncertainty about its premises.
An exactly calculated prediction of eleven does not establish an eleven-centred
tolerance. A coarse tick or an approximate landmark does not imply a uniform
interval, a Gaussian, or known variance. Any future soft expectation should
connect its centre or landmark, shape, scale, context and supporting evidence;
unknown shape or scale remains open. Phase uncertainty may straddle a wrap
boundary or have several modes, so a single ordinary interval can be a poor read.

Shared uncertainty must retain correspondence. Two computations using the same
sample are not two independent observations; the difference of an observation
with itself can cancel where two independent approximate observations cannot.
When external overlap is unknown, retain that uncertainty. Historical utility
and correlated variation may later support reducing redundant participation,
but ten agreeing routes are not automatically ten independent confirmations.

Uncertainty also communicates relevance. “About 100 miles” may deliberately
discourage refinement. A focus or utility surface should decide whether a more
detailed prediction would matter; residual uncertainty alone should not trigger
exploration. Keep measurement error, omitted effects, approximation error,
model mismatch and goal shortfall available as different interpretations.

Candidate comparisons should use the same target, exposed quantity basis,
available evidence cutoff and chosen horizon. Keep each signed mismatch for
direction, but assess predictive quality using a declared nonnegative loss or
another justified utility read: errors of `+5` and `-5` must not cancel into a
claim of perfect prediction. Scaling between different quantities needs an
explicit basis. Assessments should also retain coverage, missing outcomes,
cost and applicability; a candidate that forecasts only easy cases needs that
selection visible.

## Proposed experiment sequence

Each row is a future fixture with a deliberately narrow host policy. Passing it
would support the stated contract, not establish autonomous intelligence.

| Stage | Small experiment | Evidence required before widening scope |
| --- | --- | --- |
| S1: competing continuations | Hold-last, affine and selected multiplicative candidates see a growing sequence and then a reversal. Include unequal sample intervals. | Predictions precede outcomes; candidates share the comparison basis; no automatic winner from training fit; unavailable or invalid ratios remain open. |
| S2: phase and events | Rational sawtooth, triangle and pulse templates compete with a continuing ramp. | Period, phase, reset and cycle count are inspectable; endpoint conventions agree; missed samples are not resets; all intervals and event times survive restoration. |
| S3: ambiguity | A sinusoid-like sample sequence with aliases, unknown phase, gaps and timing error. | Several candidates may survive; justified refinement narrows rather than invents evidence; no claim of exact transcendental evaluation from rounded samples. |
| S4: composition | Offset plus trend plus repetition; then two nearby frequencies, a changed amplitude and a changed period. | Sum, modulation and alternatives remain different structures; predictions are evaluated at short and longer horizons; shared sources are not counted repeatedly. |
| S5: driven dynamics | Predict a capacitor response to a known changing input under a selected RC model. | Input history, stored state, elapsed time and observation are distinct; held-out input changes test the response; prediction alone does not actuate the circuit. |
| S6: usefulness and response | Neighbouring spaces propose actions with different immediate and delayed outcomes. | Action/outcome correspondence, reward timing, costs and unresolved credit remain explicit; local parameter changes enter through an authorized response/settlement path. |
| S7: compact reusable explanations | Retain a successful recurrence or composite while reducing old sample detail. | Preserved domain, state initialization, error expectations and source summary are inspectable; lossy compression is acknowledged; changed conditions can reopen alternatives. |

S1 is the recommended next learning fixture when implementation resumes. S2
can initially avoid new transcendental mathematics. S5 shares prerequisites
with the CAD/ECAD plan, and should wait for its physical-state contract. Reward
optimization is deliberately later than basic prospective comparison.

## Reward, internal freedom and eventual execution

A useful endpoint does not uniquely determine an internal algorithm. An
inverting local space followed by a reinverting neighbour may predict or act
just as successfully as a direct representation. Complexity, resource use or
adaptability might later distinguish them; a requirement that every intermediate
value resemble the external signal would be unjustified.

Reward is not simply prediction accuracy. A system can accurately predict an
undesirable outcome and should act to change it; successful action can also
invalidate its earlier no-action forecast. Record which intervention scenario
each forecast described. Short and long reward horizons may conflict, and an
endpoint reward does not uniquely assign causal credit. These choices need
explicit experimental policies before they can be learned locally.

Keep future execution possible by putting parameters, state transitions,
dependencies, availability cutoffs, readout, permitted mutations and retained
witnesses in the structure. Parsing an action is still not executing it. A
future compiler can specialize a selected, well-defined read; it cannot infer
missing laws from a name such as “sine”, “learner” or “reward”. No compiler,
signal engine, new grammar or learning policy is part of this documentation pass.

## Verification boundary

This plan is grounded in the inspected source and existing fixtures. The
forecast, affine and bounded-quantity syntax tests were included in the passing
focused run recorded in the
[companion assessment](GojoCadEcadSyntaxAssessment-2026-09.md#verification-of-this-assessment).
The proposed signal experiments were not implemented or executed. Core4 was
reviewed as historical source. External method references were reviewed on
September 8, 2026; they provide comparison methods rather than Gojo conformance.
