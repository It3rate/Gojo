# Composite emergence: generated analysis

Seed-paired descriptive intervals. Checkpoints are separate groups; repeated episodes are not independent replicates.

Source/transfer acquisitions are counted without a probe fee. Refresh acquisitions are paid. Recovery is first-success delay, not sustained recovery.

## pilot-01

Status: complete. Settings: `{"consumerTrain": 96, "discoveryTrain": 320, "pilot": true, "probePrice": 1, "refreshTest": 256, "refreshTrain": 384, "seedStart": 0, "seeds": 1, "switchProbability": 0.04, "testEpisodes": 128}`.

Counts: `{"resourceIdentitiesChecked": 100, "runnerEvaluations": 19840, "runnerTrainingEvents": 4544, "savedTestEvaluations": 19840, "sourceHashesChecked": 122, "uniqueRows": 107}`.

- Pilot seed data: do not present as fresh confirmation.
- Some current source hashes differ from the run manifest; preserved run data remains historical.

| Stage | Controller | Condition | Train | Seeds | Accuracy | Coverage | Acquisitions | Return |
|---|---|---|---:|---:|---|---|---|---|
| discovery | frozen | fresh-rule | 320 | 1 | 0.000% | 0.000% | 0.000% | 0.000 |
| discovery | learned | fresh-rule | 320 | 1 | 100.000% | 100.000% | 0.000% | 0.000 |
| discovery | no-expansion | fresh-rule | 320 | 1 | 36.719% | 77.344% | 0.000% | 0.000 |
| discovery | no-memory | fresh-rule | 320 | 1 | 40.625% | 76.562% | 0.000% | 0.000 |
| discovery | previous-pair | fresh-rule | 320 | 1 | 41.406% | 71.094% | 0.000% | 0.000 |
| discovery | shuffled-communication | fresh-rule | 320 | 1 | 38.281% | 69.531% | 0.000% | 0.000 |
| discovery | shuffled-feedback | fresh-rule | 320 | 1 | 59.375% | 99.219% | 0.000% | 0.000 |
| refresh | always-probe | delayed-history/baseline | 384 | 1 | 100.000% | 100.000% | 100.000% | 7.000 |
| refresh | always-probe | delayed-history/expensive-probe | 384 | 1 | 100.000% | 100.000% | 100.000% | 3.000 |
| refresh | always-probe | delayed-history/fast-switch | 384 | 1 | 100.000% | 100.000% | 100.000% | 7.000 |
| refresh | always-probe | delayed-history/missing-signals | 384 | 1 | 83.984% | 83.984% | 100.000% | 5.719 |
| refresh | always-probe | immediate/baseline | 384 | 1 | 100.000% | 100.000% | 100.000% | 7.000 |
| refresh | always-probe | immediate/expensive-probe | 384 | 1 | 100.000% | 100.000% | 100.000% | 3.000 |
| refresh | always-probe | immediate/fast-switch | 384 | 1 | 100.000% | 100.000% | 100.000% | 7.000 |
| refresh | always-probe | immediate/missing-signals | 384 | 1 | 83.984% | 83.984% | 100.000% | 5.719 |
| refresh | failure-probe | delayed-history/baseline | 384 | 1 | 84.375% | 100.000% | 16.016% | 4.715 |
| refresh | failure-probe | delayed-history/expensive-probe | 384 | 1 | 84.375% | 100.000% | 16.016% | 4.074 |
| refresh | failure-probe | delayed-history/fast-switch | 384 | 1 | 73.047% | 100.000% | 26.172% | 2.348 |
| refresh | failure-probe | delayed-history/missing-signals | 384 | 1 | 48.438% | 60.156% | 12.109% | 2.348 |
| refresh | failure-probe | immediate/baseline | 384 | 1 | 96.875% | 100.000% | 3.516% | 7.340 |
| refresh | failure-probe | immediate/expensive-probe | 384 | 1 | 96.875% | 100.000% | 3.516% | 7.199 |
| refresh | failure-probe | immediate/fast-switch | 384 | 1 | 88.672% | 100.000% | 11.719% | 5.617 |
| refresh | failure-probe | immediate/missing-signals | 384 | 1 | 96.875% | 100.000% | 3.516% | 7.340 |
| refresh | fixed-interval | delayed-history/baseline | 384 | 1 | 90.234% | 100.000% | 12.500% | 5.922 |
| refresh | fixed-interval | delayed-history/expensive-probe | 384 | 1 | 90.234% | 100.000% | 12.500% | 5.422 |
| refresh | fixed-interval | delayed-history/fast-switch | 384 | 1 | 75.000% | 100.000% | 12.500% | 2.875 |
| refresh | fixed-interval | delayed-history/missing-signals | 384 | 1 | 76.172% | 81.250% | 12.500% | 5.359 |
| refresh | fixed-interval | immediate/baseline | 384 | 1 | 90.234% | 100.000% | 12.500% | 5.922 |
| refresh | fixed-interval | immediate/expensive-probe | 384 | 1 | 90.234% | 100.000% | 12.500% | 5.422 |
| refresh | fixed-interval | immediate/fast-switch | 384 | 1 | 75.000% | 100.000% | 12.500% | 2.875 |
| refresh | fixed-interval | immediate/missing-signals | 384 | 1 | 76.172% | 81.250% | 12.500% | 5.359 |
| refresh | learned-refresh | delayed-history/baseline | 384 | 1 | 59.375% | 99.609% | 0.391% | -0.082 |
| refresh | learned-refresh | delayed-history/expensive-probe | 384 | 1 | 59.375% | 99.609% | 0.391% | -0.098 |
| refresh | learned-refresh | delayed-history/fast-switch | 384 | 1 | 44.922% | 99.609% | 0.391% | -2.973 |
| refresh | learned-refresh | delayed-history/missing-signals | 384 | 1 | 59.375% | 99.609% | 0.391% | -0.082 |
| refresh | learned-refresh | immediate/baseline | 384 | 1 | 99.609% | 99.609% | 99.609% | 6.973 |
| refresh | learned-refresh | immediate/expensive-probe | 384 | 1 | 99.609% | 99.609% | 99.609% | 2.988 |
| refresh | learned-refresh | immediate/fast-switch | 384 | 1 | 99.609% | 99.609% | 99.609% | 6.973 |
| refresh | learned-refresh | immediate/missing-signals | 384 | 1 | 13.672% | 13.672% | 17.969% | 0.914 |
| refresh | never-probe | delayed-history/baseline | 384 | 1 | 59.766% | 100.000% | 0.391% | -0.051 |
| refresh | never-probe | delayed-history/expensive-probe | 384 | 1 | 59.766% | 100.000% | 0.391% | -0.066 |
| refresh | never-probe | delayed-history/fast-switch | 384 | 1 | 45.312% | 100.000% | 0.391% | -2.941 |
| refresh | never-probe | delayed-history/missing-signals | 384 | 1 | 59.766% | 100.000% | 0.391% | -0.051 |
| refresh | never-probe | immediate/baseline | 384 | 1 | 59.766% | 100.000% | 0.391% | -0.051 |
| refresh | never-probe | immediate/expensive-probe | 384 | 1 | 59.766% | 100.000% | 0.391% | -0.066 |
| refresh | never-probe | immediate/fast-switch | 384 | 1 | 45.312% | 100.000% | 0.391% | -2.941 |
| refresh | never-probe | immediate/missing-signals | 384 | 1 | 59.766% | 100.000% | 0.391% | -0.051 |
| refresh | outcome-memory | delayed-history/baseline | 384 | 1 | 83.203% | 100.000% | 0.000% | 4.641 |
| refresh | outcome-memory | delayed-history/expensive-probe | 384 | 1 | 83.203% | 100.000% | 0.000% | 4.641 |
| refresh | outcome-memory | delayed-history/fast-switch | 384 | 1 | 57.422% | 100.000% | 0.000% | -0.516 |
| refresh | outcome-memory | delayed-history/missing-signals | 384 | 1 | 83.203% | 100.000% | 0.000% | 4.641 |
| refresh | outcome-memory | immediate/baseline | 384 | 1 | 96.484% | 100.000% | 0.000% | 7.297 |
| refresh | outcome-memory | immediate/expensive-probe | 384 | 1 | 96.484% | 100.000% | 0.000% | 7.297 |
| refresh | outcome-memory | immediate/fast-switch | 384 | 1 | 86.328% | 100.000% | 0.000% | 5.266 |
| refresh | outcome-memory | immediate/missing-signals | 384 | 1 | 96.484% | 100.000% | 0.000% | 7.297 |
| source | fixed-sum | baseline | 96 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| source | flat | baseline | 96 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| source | frozen | baseline | 96 | 1 | 49.219% | 100.000% | 100.000% | -2.156 |
| source | learned | absent-state | 96 | 1 | 0.000% | 0.000% | 100.000% | 0.000 |
| source | learned | baseline | 96 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| source | learned | inverted-input | 96 | 1 | 0.000% | 100.000% | 100.000% | -12.000 |
| source | learned | long-delay | 96 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| source | learned | missing-signals | 96 | 1 | 82.031% | 82.031% | 100.000% | 6.562 |
| source | learned | opposite-state | 96 | 1 | 0.000% | 100.000% | 100.000% | -12.000 |
| source | learned | same-state | 96 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| source | neutral-left | baseline | 0 | 1 | 52.344% | 100.000% | 100.000% | -1.531 |
| source | no-expansion | baseline | 96 | 1 | 49.219% | 100.000% | 100.000% | -2.156 |
| source | no-memory | baseline | 96 | 1 | 46.875% | 100.000% | 100.000% | -2.625 |
| source | previous-pair | baseline | 96 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| source | shuffled-communication | baseline | 96 | 1 | 47.656% | 99.219% | 100.000% | -2.375 |
| source | shuffled-feedback | baseline | 96 | 1 | 53.125% | 99.219% | 100.000% | -1.281 |
| transfer | fixed-sum | baseline | 0 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | fixed-sum | baseline | 8 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | fixed-sum | baseline | 16 | 1 | 92.188% | 100.000% | 100.000% | 6.438 |
| transfer | fixed-sum | baseline | 32 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | fixed-sum | baseline | 64 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | fixed-sum | baseline | 96 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | flat | baseline | 0 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | flat | baseline | 8 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | flat | baseline | 16 | 1 | 60.156% | 100.000% | 100.000% | 0.031 |
| transfer | flat | baseline | 32 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | flat | baseline | 64 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | flat | baseline | 96 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | fresh-service | baseline | 0 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | fresh-service | baseline | 8 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | fresh-service | baseline | 16 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | fresh-service | baseline | 32 | 1 | 56.250% | 99.219% | 100.000% | -0.656 |
| transfer | fresh-service | baseline | 64 | 1 | 56.250% | 99.219% | 100.000% | -0.656 |
| transfer | fresh-service | baseline | 96 | 1 | 56.250% | 100.000% | 100.000% | -0.750 |
| transfer | no-expansion | baseline | 0 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | no-expansion | baseline | 8 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | no-expansion | baseline | 16 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | no-expansion | baseline | 32 | 1 | 50.781% | 100.000% | 100.000% | -1.844 |
| transfer | no-expansion | baseline | 64 | 1 | 50.781% | 100.000% | 100.000% | -1.844 |
| transfer | no-expansion | baseline | 96 | 1 | 50.781% | 100.000% | 100.000% | -1.844 |
| transfer | reused-service | baseline | 0 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | reused-service | baseline | 8 | 1 | 47.656% | 100.000% | 100.000% | -2.469 |
| transfer | reused-service | baseline | 16 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | reused-service | baseline | 32 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | reused-service | baseline | 64 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | reused-service | baseline | 96 | 1 | 100.000% | 100.000% | 100.000% | 8.000 |
| transfer | shuffled-communication | baseline | 0 | 1 | 47.656% | 99.219% | 100.000% | -2.375 |
| transfer | shuffled-communication | baseline | 8 | 1 | 47.656% | 99.219% | 100.000% | -2.375 |
| transfer | shuffled-communication | baseline | 16 | 1 | 45.312% | 99.219% | 100.000% | -2.844 |
| transfer | shuffled-communication | baseline | 32 | 1 | 45.312% | 99.219% | 100.000% | -2.844 |
| transfer | shuffled-communication | baseline | 64 | 1 | 51.562% | 99.219% | 100.000% | -1.594 |
| transfer | shuffled-communication | baseline | 96 | 1 | 51.562% | 99.219% | 100.000% | -1.594 |

### Paired effects

| Stage | Left minus right | Condition | Train | Metric | Seeds | Difference |
|---|---|---|---:|---|---:|---|
| discovery | learned minus no-expansion | fresh-rule | 320 | accuracy | 1 | 63.281% |
| discovery | learned minus no-expansion | fresh-rule | 320 | coverage | 1 | 22.656% |
| discovery | learned minus frozen | fresh-rule | 320 | accuracy | 1 | 100.000% |
| discovery | learned minus frozen | fresh-rule | 320 | coverage | 1 | 100.000% |
| discovery | learned minus shuffled-feedback | fresh-rule | 320 | accuracy | 1 | 40.625% |
| discovery | learned minus shuffled-feedback | fresh-rule | 320 | coverage | 1 | 0.781% |
| discovery | learned minus shuffled-communication | fresh-rule | 320 | accuracy | 1 | 61.719% |
| discovery | learned minus shuffled-communication | fresh-rule | 320 | coverage | 1 | 30.469% |
| discovery | learned minus no-memory | fresh-rule | 320 | accuracy | 1 | 59.375% |
| discovery | learned minus no-memory | fresh-rule | 320 | coverage | 1 | 23.438% |
| discovery | learned minus previous-pair | fresh-rule | 320 | accuracy | 1 | 58.594% |
| discovery | learned minus previous-pair | fresh-rule | 320 | coverage | 1 | 28.906% |
| source | learned minus no-expansion | baseline | 96 | accuracy | 1 | 50.781% |
| source | learned minus no-expansion | baseline | 96 | meanReturn | 1 | 10.156 |
| source | learned minus frozen | baseline | 96 | accuracy | 1 | 50.781% |
| source | learned minus frozen | baseline | 96 | meanReturn | 1 | 10.156 |
| source | learned minus shuffled-feedback | baseline | 96 | accuracy | 1 | 46.875% |
| source | learned minus shuffled-feedback | baseline | 96 | meanReturn | 1 | 9.281 |
| source | learned minus shuffled-communication | baseline | 96 | accuracy | 1 | 52.344% |
| source | learned minus shuffled-communication | baseline | 96 | meanReturn | 1 | 10.375 |
| source | learned minus no-memory | baseline | 96 | accuracy | 1 | 53.125% |
| source | learned minus no-memory | baseline | 96 | meanReturn | 1 | 10.625 |
| source | learned minus previous-pair | baseline | 96 | accuracy | 1 | 52.344% |
| source | learned minus previous-pair | baseline | 96 | meanReturn | 1 | 10.469 |
| source | learned minus flat | baseline | 96 | accuracy | 1 | 0.000% |
| source | learned minus flat | baseline | 96 | meanReturn | 1 | 0.000 |
| source | learned minus fixed-sum | baseline | 96 | accuracy | 1 | 0.000% |
| source | learned minus fixed-sum | baseline | 96 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fresh-service | baseline | 0 | accuracy | 1 | 0.000% |
| transfer | reused-service minus fresh-service | baseline | 0 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus no-expansion | baseline | 0 | accuracy | 1 | 0.000% |
| transfer | reused-service minus no-expansion | baseline | 0 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus shuffled-communication | baseline | 0 | accuracy | 1 | 0.000% |
| transfer | reused-service minus shuffled-communication | baseline | 0 | meanReturn | 1 | -0.094 |
| transfer | reused-service minus flat | baseline | 0 | accuracy | 1 | 0.000% |
| transfer | reused-service minus flat | baseline | 0 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fixed-sum | baseline | 0 | accuracy | 1 | 0.000% |
| transfer | reused-service minus fixed-sum | baseline | 0 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fresh-service | baseline | 8 | accuracy | 1 | 0.000% |
| transfer | reused-service minus fresh-service | baseline | 8 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus no-expansion | baseline | 8 | accuracy | 1 | 0.000% |
| transfer | reused-service minus no-expansion | baseline | 8 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus shuffled-communication | baseline | 8 | accuracy | 1 | 0.000% |
| transfer | reused-service minus shuffled-communication | baseline | 8 | meanReturn | 1 | -0.094 |
| transfer | reused-service minus flat | baseline | 8 | accuracy | 1 | 0.000% |
| transfer | reused-service minus flat | baseline | 8 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fixed-sum | baseline | 8 | accuracy | 1 | 0.000% |
| transfer | reused-service minus fixed-sum | baseline | 8 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fresh-service | baseline | 16 | accuracy | 1 | 52.344% |
| transfer | reused-service minus fresh-service | baseline | 16 | meanReturn | 1 | 10.469 |
| transfer | reused-service minus no-expansion | baseline | 16 | accuracy | 1 | 52.344% |
| transfer | reused-service minus no-expansion | baseline | 16 | meanReturn | 1 | 10.469 |
| transfer | reused-service minus shuffled-communication | baseline | 16 | accuracy | 1 | 54.688% |
| transfer | reused-service minus shuffled-communication | baseline | 16 | meanReturn | 1 | 10.844 |
| transfer | reused-service minus flat | baseline | 16 | accuracy | 1 | 39.844% |
| transfer | reused-service minus flat | baseline | 16 | meanReturn | 1 | 7.969 |
| transfer | reused-service minus fixed-sum | baseline | 16 | accuracy | 1 | 7.812% |
| transfer | reused-service minus fixed-sum | baseline | 16 | meanReturn | 1 | 1.562 |
| transfer | reused-service minus fresh-service | baseline | 32 | accuracy | 1 | 43.750% |
| transfer | reused-service minus fresh-service | baseline | 32 | meanReturn | 1 | 8.656 |
| transfer | reused-service minus no-expansion | baseline | 32 | accuracy | 1 | 49.219% |
| transfer | reused-service minus no-expansion | baseline | 32 | meanReturn | 1 | 9.844 |
| transfer | reused-service minus shuffled-communication | baseline | 32 | accuracy | 1 | 54.688% |
| transfer | reused-service minus shuffled-communication | baseline | 32 | meanReturn | 1 | 10.844 |
| transfer | reused-service minus flat | baseline | 32 | accuracy | 1 | 0.000% |
| transfer | reused-service minus flat | baseline | 32 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fixed-sum | baseline | 32 | accuracy | 1 | 0.000% |
| transfer | reused-service minus fixed-sum | baseline | 32 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fresh-service | baseline | 64 | accuracy | 1 | 43.750% |
| transfer | reused-service minus fresh-service | baseline | 64 | meanReturn | 1 | 8.656 |
| transfer | reused-service minus no-expansion | baseline | 64 | accuracy | 1 | 49.219% |
| transfer | reused-service minus no-expansion | baseline | 64 | meanReturn | 1 | 9.844 |
| transfer | reused-service minus shuffled-communication | baseline | 64 | accuracy | 1 | 48.438% |
| transfer | reused-service minus shuffled-communication | baseline | 64 | meanReturn | 1 | 9.594 |
| transfer | reused-service minus flat | baseline | 64 | accuracy | 1 | 0.000% |
| transfer | reused-service minus flat | baseline | 64 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fixed-sum | baseline | 64 | accuracy | 1 | 0.000% |
| transfer | reused-service minus fixed-sum | baseline | 64 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fresh-service | baseline | 96 | accuracy | 1 | 43.750% |
| transfer | reused-service minus fresh-service | baseline | 96 | meanReturn | 1 | 8.750 |
| transfer | reused-service minus no-expansion | baseline | 96 | accuracy | 1 | 49.219% |
| transfer | reused-service minus no-expansion | baseline | 96 | meanReturn | 1 | 9.844 |
| transfer | reused-service minus shuffled-communication | baseline | 96 | accuracy | 1 | 48.438% |
| transfer | reused-service minus shuffled-communication | baseline | 96 | meanReturn | 1 | 9.594 |
| transfer | reused-service minus flat | baseline | 96 | accuracy | 1 | 0.000% |
| transfer | reused-service minus flat | baseline | 96 | meanReturn | 1 | 0.000 |
| transfer | reused-service minus fixed-sum | baseline | 96 | accuracy | 1 | 0.000% |
| transfer | reused-service minus fixed-sum | baseline | 96 | meanReturn | 1 | 0.000 |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 384 | accuracy | 1 | -40.625% |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 384 | meanReturn | 1 | -7.082 |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 384 | queryRate | 1 | -99.609% |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 384 | accuracy | 1 | -0.391% |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 384 | meanReturn | 1 | -0.031 |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 384 | queryRate | 1 | 0.000% |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 384 | accuracy | 1 | -23.828% |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 384 | meanReturn | 1 | -4.723 |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 384 | queryRate | 1 | 0.391% |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 384 | accuracy | 1 | -25.000% |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 384 | meanReturn | 1 | -4.797 |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 384 | queryRate | 1 | -15.625% |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 384 | accuracy | 1 | -30.859% |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 384 | meanReturn | 1 | -6.004 |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 384 | queryRate | 1 | -12.109% |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 384 | accuracy | 1 | -40.625% |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 384 | meanReturn | 1 | -3.098 |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 384 | queryRate | 1 | -99.609% |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 384 | accuracy | 1 | -0.391% |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 384 | meanReturn | 1 | -0.031 |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 384 | queryRate | 1 | 0.000% |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 384 | accuracy | 1 | -23.828% |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 384 | meanReturn | 1 | -4.738 |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 384 | queryRate | 1 | 0.391% |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 384 | accuracy | 1 | -25.000% |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 384 | meanReturn | 1 | -4.172 |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 384 | queryRate | 1 | -15.625% |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 384 | accuracy | 1 | -30.859% |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 384 | meanReturn | 1 | -5.520 |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 384 | queryRate | 1 | -12.109% |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 384 | accuracy | 1 | -55.078% |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 384 | meanReturn | 1 | -9.973 |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 384 | queryRate | 1 | -99.609% |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 384 | accuracy | 1 | -0.391% |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 384 | meanReturn | 1 | -0.031 |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 384 | queryRate | 1 | 0.000% |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 384 | accuracy | 1 | -12.500% |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 384 | meanReturn | 1 | -2.457 |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 384 | queryRate | 1 | 0.391% |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 384 | accuracy | 1 | -28.125% |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 384 | meanReturn | 1 | -5.320 |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 384 | queryRate | 1 | -25.781% |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 384 | accuracy | 1 | -30.078% |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 384 | meanReturn | 1 | -5.848 |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 384 | queryRate | 1 | -12.109% |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 384 | accuracy | 1 | -24.609% |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 384 | meanReturn | 1 | -5.801 |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 384 | queryRate | 1 | -99.609% |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 384 | accuracy | 1 | -0.391% |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 384 | meanReturn | 1 | -0.031 |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 384 | queryRate | 1 | 0.000% |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 384 | accuracy | 1 | -23.828% |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 384 | meanReturn | 1 | -4.723 |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 384 | queryRate | 1 | 0.391% |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 384 | accuracy | 1 | 10.938% |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 384 | meanReturn | 1 | -2.430 |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 384 | queryRate | 1 | -11.719% |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 384 | accuracy | 1 | -16.797% |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 384 | meanReturn | 1 | -5.441 |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 384 | queryRate | 1 | -12.109% |
| refresh | learned-refresh minus always-probe | immediate/baseline | 384 | accuracy | 1 | -0.391% |
| refresh | learned-refresh minus always-probe | immediate/baseline | 384 | meanReturn | 1 | -0.027 |
| refresh | learned-refresh minus always-probe | immediate/baseline | 384 | queryRate | 1 | -0.391% |
| refresh | learned-refresh minus never-probe | immediate/baseline | 384 | accuracy | 1 | 39.844% |
| refresh | learned-refresh minus never-probe | immediate/baseline | 384 | meanReturn | 1 | 7.023 |
| refresh | learned-refresh minus never-probe | immediate/baseline | 384 | queryRate | 1 | 99.219% |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 384 | accuracy | 1 | 3.125% |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 384 | meanReturn | 1 | -0.324 |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 384 | queryRate | 1 | 99.609% |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 384 | accuracy | 1 | 2.734% |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 384 | meanReturn | 1 | -0.367 |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 384 | queryRate | 1 | 96.094% |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 384 | accuracy | 1 | 9.375% |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 384 | meanReturn | 1 | 1.051 |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 384 | queryRate | 1 | 87.109% |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 384 | accuracy | 1 | -0.391% |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 384 | meanReturn | 1 | -0.012 |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 384 | queryRate | 1 | -0.391% |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 384 | accuracy | 1 | 39.844% |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 384 | meanReturn | 1 | 3.055 |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 384 | queryRate | 1 | 99.219% |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 384 | accuracy | 1 | 3.125% |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 384 | meanReturn | 1 | -4.309 |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 384 | queryRate | 1 | 99.609% |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 384 | accuracy | 1 | 2.734% |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 384 | meanReturn | 1 | -4.211 |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 384 | queryRate | 1 | 96.094% |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 384 | accuracy | 1 | 9.375% |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 384 | meanReturn | 1 | -2.434 |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 384 | queryRate | 1 | 87.109% |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 384 | accuracy | 1 | -0.391% |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 384 | meanReturn | 1 | -0.027 |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 384 | queryRate | 1 | -0.391% |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 384 | accuracy | 1 | 54.297% |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 384 | meanReturn | 1 | 9.914 |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 384 | queryRate | 1 | 99.219% |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 384 | accuracy | 1 | 13.281% |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 384 | meanReturn | 1 | 1.707 |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 384 | queryRate | 1 | 99.609% |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 384 | accuracy | 1 | 10.938% |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 384 | meanReturn | 1 | 1.355 |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 384 | queryRate | 1 | 87.891% |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 384 | accuracy | 1 | 24.609% |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 384 | meanReturn | 1 | 4.098 |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 384 | queryRate | 1 | 87.109% |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 384 | accuracy | 1 | -70.312% |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 384 | meanReturn | 1 | -4.805 |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 384 | queryRate | 1 | -82.031% |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 384 | accuracy | 1 | -46.094% |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 384 | meanReturn | 1 | 0.965 |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 384 | queryRate | 1 | 17.578% |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 384 | accuracy | 1 | -82.812% |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 384 | meanReturn | 1 | -6.383 |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 384 | queryRate | 1 | 17.969% |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 384 | accuracy | 1 | -83.203% |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 384 | meanReturn | 1 | -6.426 |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 384 | queryRate | 1 | 14.453% |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 384 | accuracy | 1 | -62.500% |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 384 | meanReturn | 1 | -4.445 |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 384 | queryRate | 1 | 5.469% |

### Native archives

- relation: 238,387 bytes; signature=True, behavior=True; `relation.gojo`.
- consumer: 71,294 bytes; signature=True, behavior=True; `consumer.gojo`.
- refresh-0: 104,543 bytes; signature=True, behavior=True; `refresh-0.gojo`.
- refresh-4: 98,781 bytes; signature=True, behavior=True; `refresh-4.gojo`.

Interval method: two-sided 95% Student t on seed means or paired seed differences; df>30 uses conservative df30 critical; exploratory contrasts without multiplicity correction.
