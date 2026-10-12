# Protected candidate trials: analysis

2 seeds; 20,480 world requests. All declared audits passed.

Costs are abstract; physical gross minus paid burn is the primary return. Internal fees do not create income.

| Arm | Net/request | Gross/request | Burn/request | Coverage | Trials | Promoted | Seed work | Earned work |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Existing error | 2.888 | 5.403 | 2.515 | 69.8% | 0 | 0 | 0 | 0 |
| Existing progress | 2.496 | 4.946 | 2.450 | 68.1% | 0 | 0 | 0 | 0 |
| Seed + paired | 1.249 | 3.636 | 2.387 | 59.8% | 10 | 2 | 1191 | 415 |
| Seed + unfiltered | 1.211 | 3.606 | 2.395 | 59.8% | 10 | 6 | 1191 | 415 |
| Paired, no seed | 0.631 | 2.046 | 1.415 | 43.3% | 1 | 1 | 0 | 164 |

## Paired contrasts

| Difference in net/request | Mean | 95% interval | Positive seeds |
| --- | ---: | --- | ---: |
| paired minus unfiltered | 0.038 | None for this pilot | 2/2 |
| paired minus no-seed | 0.618 | None for this pilot | 1/2 |
| paired minus existing error | -1.639 | None for this pilot | 0/2 |
| paired minus existing progress | -1.248 | None for this pilot | 0/2 |

The first two contrasts are primary. Legacy contrasts compare full packages.

## Noise and later usefulness

| Arm | Noise promotions | Noise trial work | Newly useful net/request | Profitable seeds | Rare first16 correct/requests |
| --- | ---: | ---: | ---: | ---: | ---: |
| Existing error | 0 | 0 | -0.266 | 1/2 | 31/32 |
| Existing progress | 0 | 0 | -0.114 | 0/2 | 17/32 |
| Seed + paired | 0 | 393 | -1.241 | 0/2 | 3/32 |
| Seed + unfiltered | 2 | 393 | -1.241 | 0/2 | 3/32 |
| Paired, no seed | 0 | 0 | -0.595 | 0/2 | 3/32 |

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
