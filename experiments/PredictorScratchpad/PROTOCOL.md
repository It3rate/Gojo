# Predictor network: first exploratory protocol

Fixed before running the predictor. The purpose is promising signal and useful
failure, not recovery of the generator's intended causes. This first network
adapts participation in a supplied palette; it does not grow its own topology.

## Input boundary and timing

Four conditions use seeds 103, 907 and 2029, each with 216 arrivals: changing
ramp/plateau/cadence; smooth turning with drift; two anonymous mixed inputs;
and independently shuffled controls with the mixed inputs' exact channel
marginals. The generator lives outside the learner. Only two numeric sensor
observations cross its boundary. No seed, phase label, future value, switch,
source count or cycle phase reaches a predictor.

The host schedules arrivals and native reads. It declares one initial sample
to establish a local origin and seed short traces. Every subsequent forecast
is read and frozen **before** admitting the next sample. Native errors are
calculated from those frozen forecasts, then temporal traces and adaptation
state advance. The first eight arrivals are warm-up; all later arrivals are
scored, including regime transitions. No omitted seed or late parameter tuning.

## Supplied structure

Use the same seven branches for both sensors in every condition:

1. Hold current input.
2. Continue the last observed change.
3. Continue half the last change.
4. Continue a slowly following change trace, updated with gain 1/4.
5. Continue the last change plus half its change from the previous change.
6. Reverse the last change (a very short recurrence bias).
7. Continue one quarter of the other sensor's last change, using a supplied
   local calibration. This is an exploratory coupling, not a discovered cause.

The difference, difference-of-differences, slow response and cross-input
transformation feed further equations. Their connected Gojo structure is the
meaning; labels exist only in the evaluator's report.

Each branch's subsequent squared prediction error updates its local trace:

```text
L_next = floor((7 * L + error_squared) / 8)
w = floor(65536 * 256 / (256 + L))
adaptive_forecast = floor(sum(w * forecast) / sum(w))
```

These are native equations, including the dynamic denominator. Compute the
whole numerator before division. Signed division follows Gojo's actual read
semantics; inspect its residue rather than assuming host rounding rules.
Initial L is zero. The 256 squared-tick prior is a supplied calibration, and
65536 is participation grain. Weights are neither probability nor confidence.
Each sensor's error remains in its own basis. No combined light/heat score is
used to steer participation.

State contains bounded temporal coordinates and running error traces. Evaluation
may save observations, but the learner cannot read that history. Derived output
is evaluated from connected equations on demand; only explicitly temporal state
is committed between arrivals. Locally unused arithmetic residue remains in the
read witness, not repeatedly deposited as new transferred pressure.

## Comparisons and inspection

Report every primitive alongside the adaptive mixture. Compare the adaptive
mixture with hold, last-change, an equal mixture of the same seven branches,
and a mixture whose participation is frozen after exactly 40 completed
predict-observe-update cycles, affecting the following forecast. Branch temporal
states keep evolving in all mixture controls. No
after-the-fact best branch is permitted to act as a forecaster.

Pre-run review amendment: also include a causal running-level mean, using only
a native accumulated sum and count. It is a comparator, not an extra adaptive
branch. This challenges the restricted recent-sample palette on shuffled input;
beating hold does not establish useful temporal prediction. This amendment was
made before any predictor result was available.

Primary metric: mean absolute error in each sensor's original ticks. Also
report mean squared error, signed bias, and native adaptive-minus-baseline
differences. Report all three seeds and pooled native totals, with phase-level
inspection to expose adaptation lag. Full traces belong only to the evaluator.

Inspect bounded graph size, unchanged repeated reads, forecast timing, signed
predictions, arithmetic residue, archive replay, and the ability to perturb
restored temporal state without changing the original graph. These are mechanism
checks, not success criteria for the forecasting result.

Retain weak or adverse results. In particular, a loss-weighted mixture may react
too late at transitions, averaging can destroy a useful sharp prediction,
quantization can stall adaptation, and temporal primitives may underfit slow
recurrence. A shuffled control can still favor a slow/mean-like response under
squared error; that is not evidence that its permutation has been discovered.

No kernel fix or revised prediction law will be silently inserted after viewing
results. Record a defect and its correction separately if execution requires one.
