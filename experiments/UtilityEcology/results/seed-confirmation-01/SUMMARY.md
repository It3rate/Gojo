# Protected candidate trials: analysis

8 seeds; 81,920 world requests. All declared audits passed.

Costs are abstract; physical gross minus paid burn is the primary return. Internal fees do not create income.

| Arm | Net/request | Gross/request | Burn/request | Coverage | Trials | Promoted | Seed work | Earned work |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Existing error | 2.836 | 5.323 | 2.487 | 69.0% | 0 | 0 | 0 | 0 |
| Existing progress | 2.731 | 5.203 | 2.472 | 69.2% | 0 | 0 | 0 | 0 |
| Seed + paired | 1.301 | 3.715 | 2.413 | 60.8% | 39 | 8 | 4650 | 1484 |
| Seed + unfiltered | 1.294 | 3.715 | 2.420 | 60.8% | 39 | 23 | 4650 | 1484 |
| Paired, no seed | 0.327 | 1.512 | 1.185 | 37.4% | 2 | 2 | 0 | 331 |

## Paired contrasts

| Difference in net/request | Mean | 95% interval | Positive seeds |
| --- | ---: | --- | ---: |
| paired minus unfiltered | 0.007 | [-0.005, 0.019] | 6/8 |
| paired minus no-seed | 0.974 | [0.308, 1.640] | 6/8 |
| paired minus existing error | -1.534 | [-1.796, -1.273] | 0/8 |
| paired minus existing progress | -1.430 | [-1.714, -1.146] | 0/8 |

The first two contrasts are primary. Legacy contrasts compare full packages.

## Noise and later usefulness

| Arm | Noise promotions | Noise trial work | Newly useful net/request | Profitable seeds | Rare first16 correct/requests |
| --- | ---: | ---: | ---: | ---: | ---: |
| Existing error | 0 | 0 | -0.273 | 2/8 | 74/128 |
| Existing progress | 0 | 0 | 0.285 | 4/8 | 74/128 |
| Seed + paired | 0 | 1508 | -1.320 | 0/8 | 7/128 |
| Seed + unfiltered | 8 | 1508 | -1.320 | 0/8 | 7/128 |
| Paired, no seed | 0 | 0 | -0.778 | 0/8 | 7/128 |

## Interpretation limits

- All-task endpoint includes holds, startup, failed work and rejected trials.
- Only eight independent seed differences receive descriptive unadjusted t(7) intervals; two-seed pilots have none.
- Fees redistribute conserved funds; actual physical gross minus real burn defines group return.
- Costs are abstract. Candidate replay estimates cannot mint external income.
- Both structures are frozen during the declared shared validation block; candidate open reuses the incumbent projected resource and contributes exactly zero paired gain.
- Phase and source quality are evaluator labels, never learner inputs.
- Burn is assigned to its world request; per-source net is not causal contribution credit.
- Fresh16 windows are descriptive, stop before another promotion and may include online learning after each prediction.
- Reported row updates count base-predictor updates only. Candidate trainingCount counts paid physical training experiences, not successful ordinal updates.
- A promoted candidate can later learn as the current model. Historical trial roots retain live model carrier state; captured validation publications preserve what was evaluated, not a permanent frozen model snapshot.
- Supplier reserve is its last observed request-time balance, not a simultaneous final balance across all services.
- Post-first-closure windows begin at different times and are selection-conditioned; fixed final-phase results are also reported.
- A fee-funded service can remain solvent while the whole system loses physical resource.
- Trial causes inferred from coverage/gain are diagnostics; an incomplete trial alone does not identify why it aborted.
- Saved archive checks cover their declared retained-anchor and paid-gate fixture, not a whole-world restart.
