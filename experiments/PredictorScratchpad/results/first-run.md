# Predictor network: observed results
The protocol was committed before execution. The learner sees only two sensor values after issuing each forecast. All prediction, loss, weighting and metric arithmetic uses connected Gojo equations; the host schedules reads/commits and generates the environment.
Seven supplied branches compose change, curvature, slow response and a calibrated cross-input contribution. Participation adapts; topology does not grow. Weights are not probabilities or confidence. Squared error drives adaptation; absolute error is the primary reported metric.
## Pooled mean absolute error (original sensor ticks)
Lower is better. Each channel stays separate. All post-warm-up observations, including transitions, contribute.
| Condition | Channel | Hold | Last change | Adaptive | Equal | Frozen 40 | Running mean | Adaptive - hold | Adaptive - equal |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| ramp-plateau-cadence | 0 | 4.19 | 4.08 | 3.48 | 3.56 | 3.54 | 83.24 | -0.71 | -0.08 |
| ramp-plateau-cadence | 1 | 2.53 | 3.25 | 2.60 | 2.60 | 2.60 | 67.11 | 0.07 | 0.00 |
| smooth-turning | 0 | 10.98 | 4.12 | 5.70 | 7.45 | 5.44 | 109.36 | -5.28 | -1.75 |
| smooth-turning | 1 | 8.55 | 3.94 | 5.08 | 5.92 | 5.07 | 86.46 | -3.47 | -0.84 |
| mixed-anonymous-input | 0 | 33.73 | 61.71 | 39.94 | 42.58 | 39.23 | 121.60 | 6.21 | -2.64 |
| mixed-anonymous-input | 1 | 17.41 | 14.51 | 13.04 | 13.84 | 12.80 | 96.90 | -4.37 | -0.80 |
| shuffled-marginal-control | 0 | 153.89 | 268.91 | 157.50 | 186.39 | 157.44 | 123.12 | 3.61 | -28.89 |
| shuffled-marginal-control | 1 | 136.02 | 238.64 | 137.86 | 162.99 | 142.74 | 96.83 | 1.84 | -25.13 |
## Bounded learner structure
ramp-plateau-cadence/103: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
smooth-turning/103: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
mixed-anonymous-input/103: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
shuffled-marginal-control/103: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
ramp-plateau-cadence/907: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
smooth-turning/907: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
mixed-anonymous-input/907: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
shuffled-marginal-control/907: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
ramp-plateau-cadence/2029: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
smooth-turning/2029: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
mixed-anonymous-input/2029: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
shuffled-marginal-control/2029: 919 carriers, 9916 relations, 6094 coordinates; unchanged from creation.
Mechanism assertions passed: 224117. Assertions check execution and preservation, not predictive superiority.
## Limits
Fixed supplied palette and update law; host-scheduled reads/temporal commits; manually calibrated cross-input contribution and loss prior; integer quantization residue; no autonomous growth, pruning, learned source separation, intervention or phase discovery. Exact numerical forecasts should not be mistaken for calibrated uncertainty. A running mean is a crucial control for shuffled input.
