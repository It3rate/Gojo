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

## pilot-02

Status: complete. Settings: `{"consumerTrain": 192, "discoveryTrain": 384, "pilot": true, "probePrice": 1, "refreshTest": 768, "refreshTrain": 2048, "seedStart": 1, "seeds": 2, "switchProbability": 0.04, "testEpisodes": 256}`.

Counts: `{"resourceIdentitiesChecked": 212, "runnerEvaluations": 107008, "runnerTrainingEvents": 19712, "savedTestEvaluations": 107008, "sourceHashesChecked": 122, "uniqueRows": 226}`.

- Pilot seed data: do not present as fresh confirmation.
- Some current source hashes differ from the run manifest; preserved run data remains historical.

| Stage | Controller | Condition | Train | Seeds | Accuracy | Coverage | Acquisitions | Return |
|---|---|---|---:|---:|---|---|---|---|
| discovery | frozen | fresh-rule | 384 | 2 | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | learned | fresh-rule | 384 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | no-expansion | fresh-rule | 384 | 2 | 50.781% [16.038, 85.525] | 99.805% [97.323, 102.286] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | no-memory | fresh-rule | 384 | 2 | 33.594% [-249.318, 316.505] | 62.500% [-413.983, 538.983] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | previous-pair | fresh-rule | 384 | 2 | 49.023% [6.835, 91.212] | 99.805% [97.323, 102.286] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | shuffled-communication | fresh-rule | 384 | 2 | 48.242% [11.017, 85.467] | 99.609% [99.609, 99.609] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | shuffled-feedback | fresh-rule | 384 | 2 | 49.609% [9.902, 89.316] | 99.805% [97.323, 102.286] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| refresh | always-probe | delayed-history/baseline | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 7.000 [7.000, 7.000] |
| refresh | always-probe | delayed-history/expensive-probe | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 3.000 [3.000, 3.000] |
| refresh | always-probe | delayed-history/fast-switch | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 7.000 [7.000, 7.000] |
| refresh | always-probe | delayed-history/missing-signals | 2048 | 2 | 81.576% [72.476, 90.675] | 81.576% [72.476, 90.675] | 100.000% [100.000, 100.000] | 5.526 [4.798, 6.254] |
| refresh | always-probe | immediate/baseline | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 7.000 [7.000, 7.000] |
| refresh | always-probe | immediate/expensive-probe | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 3.000 [3.000, 3.000] |
| refresh | always-probe | immediate/fast-switch | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 7.000 [7.000, 7.000] |
| refresh | always-probe | immediate/missing-signals | 2048 | 2 | 81.576% [72.476, 90.675] | 81.576% [72.476, 90.675] | 100.000% [100.000, 100.000] | 5.526 [4.798, 6.254] |
| refresh | failure-probe | delayed-history/baseline | 2048 | 2 | 83.789% [76.344, 91.234] | 100.000% [100.000, 100.000] | 16.341% [8.896, 23.786] | 4.594 [3.031, 6.158] |
| refresh | failure-probe | delayed-history/expensive-probe | 2048 | 2 | 83.789% [76.344, 91.234] | 100.000% [100.000, 100.000] | 16.341% [8.896, 23.786] | 3.941 [2.079, 5.802] |
| refresh | failure-probe | delayed-history/fast-switch | 2048 | 2 | 75.195% [52.860, 97.530] | 100.000% [100.000, 100.000] | 24.935% [2.600, 47.270] | 2.790 [-1.901, 7.480] |
| refresh | failure-probe | delayed-history/missing-signals | 2048 | 2 | 27.148% [-130.852, 185.149] | 32.617% [-170.053, 235.288] | 5.599% [-39.071, 50.269] | 1.460 [-5.373, 8.293] |
| refresh | failure-probe | immediate/baseline | 2048 | 2 | 95.378% [89.587, 101.168] | 100.000% [100.000, 100.000] | 4.753% [-1.038, 10.543] | 7.028 [5.812, 8.244] |
| refresh | failure-probe | immediate/expensive-probe | 2048 | 2 | 95.378% [89.587, 101.168] | 100.000% [100.000, 100.000] | 4.753% [-1.038, 10.543] | 6.838 [5.390, 8.286] |
| refresh | failure-probe | immediate/fast-switch | 2048 | 2 | 88.151% [84.842, 91.460] | 100.000% [100.000, 100.000] | 11.979% [8.670, 15.288] | 5.510 [4.816, 6.205] |
| refresh | failure-probe | immediate/missing-signals | 2048 | 2 | 25.781% [9.237, 42.326] | 27.018% [12.955, 41.081] | 1.367% [-1.114, 3.849] | 1.900 [0.254, 3.547] |
| refresh | fixed-interval | delayed-history/baseline | 2048 | 2 | 86.133% [58.834, 113.431] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.102 [-0.358, 10.561] |
| refresh | fixed-interval | delayed-history/expensive-probe | 2048 | 2 | 86.133% [58.834, 113.431] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 4.602 [-0.858, 10.061] |
| refresh | fixed-interval | delayed-history/fast-switch | 2048 | 2 | 70.052% [50.199, 89.906] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 1.885 [-2.085, 5.856] |
| refresh | fixed-interval | delayed-history/missing-signals | 2048 | 2 | 67.122% [61.332, 72.913] | 79.167% [52.695, 105.638] | 12.500% [12.500, 12.500] | 3.799 [-0.535, 8.134] |
| refresh | fixed-interval | immediate/baseline | 2048 | 2 | 86.133% [58.834, 113.431] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.102 [-0.358, 10.561] |
| refresh | fixed-interval | immediate/expensive-probe | 2048 | 2 | 86.133% [58.834, 113.431] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 4.602 [-0.858, 10.061] |
| refresh | fixed-interval | immediate/fast-switch | 2048 | 2 | 70.052% [50.199, 89.906] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 1.885 [-2.085, 5.856] |
| refresh | fixed-interval | immediate/missing-signals | 2048 | 2 | 67.122% [61.332, 72.913] | 79.167% [52.695, 105.638] | 12.500% [12.500, 12.500] | 3.799 [-0.535, 8.134] |
| refresh | learned-refresh | delayed-history/baseline | 2048 | 2 | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 99.349% [99.349, 99.349] | 6.996 [6.996, 6.996] |
| refresh | learned-refresh | delayed-history/expensive-probe | 2048 | 2 | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 99.349% [99.349, 99.349] | 3.022 [3.022, 3.022] |
| refresh | learned-refresh | delayed-history/fast-switch | 2048 | 2 | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 99.349% [99.349, 99.349] | 6.996 [6.996, 6.996] |
| refresh | learned-refresh | delayed-history/missing-signals | 2048 | 2 | 0.716% [-0.111, 1.543] | 0.716% [-0.111, 1.543] | 0.391% [0.391, 0.391] | 0.053 [-0.013, 0.120] |
| refresh | learned-refresh | immediate/baseline | 2048 | 2 | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 6.991 [6.991, 6.991] |
| refresh | learned-refresh | immediate/expensive-probe | 2048 | 2 | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 2.996 [2.996, 2.996] |
| refresh | learned-refresh | immediate/fast-switch | 2048 | 2 | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 99.870% [99.870, 99.870] | 6.991 [6.991, 6.991] |
| refresh | learned-refresh | immediate/missing-signals | 2048 | 2 | 7.227% [-39.925, 54.378] | 7.227% [-39.925, 54.378] | 8.984% [-43.958, 61.927] | 0.488 [-2.754, 3.731] |
| refresh | never-probe | delayed-history/baseline | 2048 | 2 | 55.794% [-4.593, 116.182] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -0.842 [-12.920, 11.235] |
| refresh | never-probe | delayed-history/expensive-probe | 2048 | 2 | 55.794% [-4.593, 116.182] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -0.848 [-12.925, 11.230] |
| refresh | never-probe | delayed-history/fast-switch | 2048 | 2 | 52.995% [28.178, 77.812] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -1.402 [-6.366, 3.561] |
| refresh | never-probe | delayed-history/missing-signals | 2048 | 2 | 55.794% [-4.593, 116.182] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -0.842 [-12.920, 11.235] |
| refresh | never-probe | immediate/baseline | 2048 | 2 | 55.794% [-4.593, 116.182] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -0.842 [-12.920, 11.235] |
| refresh | never-probe | immediate/expensive-probe | 2048 | 2 | 55.794% [-4.593, 116.182] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -0.848 [-12.925, 11.230] |
| refresh | never-probe | immediate/fast-switch | 2048 | 2 | 52.995% [28.178, 77.812] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -1.402 [-6.366, 3.561] |
| refresh | never-probe | immediate/missing-signals | 2048 | 2 | 55.794% [-4.593, 116.182] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -0.842 [-12.920, 11.235] |
| refresh | outcome-memory | delayed-history/baseline | 2048 | 2 | 79.557% [64.667, 94.447] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 3.911 [0.933, 6.889] |
| refresh | outcome-memory | delayed-history/expensive-probe | 2048 | 2 | 79.557% [64.667, 94.447] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 3.911 [0.933, 6.889] |
| refresh | outcome-memory | delayed-history/fast-switch | 2048 | 2 | 61.328% [21.621, 101.035] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 0.266 [-7.676, 8.207] |
| refresh | outcome-memory | delayed-history/missing-signals | 2048 | 2 | 79.557% [64.667, 94.447] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 3.911 [0.933, 6.889] |
| refresh | outcome-memory | immediate/baseline | 2048 | 2 | 95.117% [89.327, 100.908] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.023 [5.865, 8.182] |
| refresh | outcome-memory | immediate/expensive-probe | 2048 | 2 | 95.117% [89.327, 100.908] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.023 [5.865, 8.182] |
| refresh | outcome-memory | immediate/fast-switch | 2048 | 2 | 86.393% [78.948, 93.838] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 5.279 [3.790, 6.768] |
| refresh | outcome-memory | immediate/missing-signals | 2048 | 2 | 95.117% [89.327, 100.908] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.023 [5.865, 8.182] |
| source | fixed-sum | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | flat | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | frozen | baseline | 192 | 2 | 50.195% [27.860, 72.530] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.961 [-6.428, 2.506] |
| source | learned | absent-state | 192 | 2 | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 0.000 [0.000, 0.000] |
| source | learned | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | learned | inverted-input | 192 | 2 | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -12.000 [-12.000, -12.000] |
| source | learned | long-delay | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | learned | missing-signals | 192 | 2 | 81.836% [39.647, 124.025] | 81.836% [39.647, 124.025] | 100.000% [100.000, 100.000] | 6.547 [3.172, 9.922] |
| source | learned | opposite-state | 192 | 2 | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -12.000 [-12.000, -12.000] |
| source | learned | same-state | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | neutral-left | baseline | 0 | 2 | 49.023% [16.762, 81.285] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.195 [-8.648, 4.257] |
| source | no-expansion | baseline | 192 | 2 | 50.977% [18.715, 83.238] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -1.781 [-8.531, 4.969] |
| source | no-memory | baseline | 192 | 2 | 50.977% [18.715, 83.238] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-8.257, 4.648] |
| source | previous-pair | baseline | 192 | 2 | 49.609% [34.719, 64.499] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -2.055 [-4.735, 0.626] |
| source | shuffled-communication | baseline | 192 | 2 | 50.391% [35.501, 65.281] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.875 [-4.853, 1.103] |
| source | shuffled-feedback | baseline | 192 | 2 | 47.070% [29.699, 64.442] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -2.562 [-5.739, 0.614] |
| transfer | fixed-sum | baseline | 0 | 2 | 50.977% [18.715, 83.238] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-8.257, 4.648] |
| transfer | fixed-sum | baseline | 8 | 2 | 50.977% [18.715, 83.238] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-8.257, 4.648] |
| transfer | fixed-sum | baseline | 16 | 2 | 72.070% [14.992, 129.149] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 2.414 [-9.002, 13.830] |
| transfer | fixed-sum | baseline | 32 | 2 | 94.922% [30.398, 159.446] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.984 [-5.920, 19.889] |
| transfer | fixed-sum | baseline | 64 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fixed-sum | baseline | 128 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fixed-sum | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | flat | baseline | 0 | 2 | 50.977% [18.715, 83.238] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-8.257, 4.648] |
| transfer | flat | baseline | 8 | 2 | 50.977% [18.715, 83.238] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-8.257, 4.648] |
| transfer | flat | baseline | 16 | 2 | 64.258% [22.069, 106.446] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 0.852 [-7.586, 9.289] |
| transfer | flat | baseline | 32 | 2 | 87.695% [-68.651, 244.041] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.539 [-25.730, 36.808] |
| transfer | flat | baseline | 64 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | flat | baseline | 128 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | flat | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fresh-service | baseline | 0 | 2 | 50.977% [18.715, 83.238] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-8.257, 4.648] |
| transfer | fresh-service | baseline | 8 | 2 | 49.609% [34.719, 64.499] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.078 [-5.056, 0.900] |
| transfer | fresh-service | baseline | 16 | 2 | 50.977% [18.715, 83.238] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-8.257, 4.648] |
| transfer | fresh-service | baseline | 32 | 2 | 50.977% [18.715, 83.238] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.758 [-8.210, 4.695] |
| transfer | fresh-service | baseline | 64 | 2 | 49.609% [34.719, 64.499] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -2.055 [-4.735, 0.626] |
| transfer | fresh-service | baseline | 128 | 2 | 87.891% [-65.974, 241.755] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.578 [-25.195, 36.351] |
| transfer | fresh-service | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | no-expansion | baseline | 0 | 2 | 50.977% [18.715, 83.238] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -1.781 [-8.531, 4.969] |
| transfer | no-expansion | baseline | 8 | 2 | 50.977% [18.715, 83.238] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -1.781 [-8.531, 4.969] |
| transfer | no-expansion | baseline | 16 | 2 | 50.977% [18.715, 83.238] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -1.781 [-8.531, 4.969] |
| transfer | no-expansion | baseline | 32 | 2 | 50.977% [38.568, 63.385] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -1.781 [-4.561, 0.998] |
| transfer | no-expansion | baseline | 64 | 2 | 51.758% [29.423, 74.093] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -1.625 [-6.390, 3.140] |
| transfer | no-expansion | baseline | 128 | 2 | 51.758% [29.423, 74.093] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -1.625 [-6.390, 3.140] |
| transfer | no-expansion | baseline | 192 | 2 | 51.758% [29.423, 74.093] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -1.625 [-6.390, 3.140] |
| transfer | reused-service | baseline | 0 | 2 | 50.977% [18.715, 83.238] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-8.257, 4.648] |
| transfer | reused-service | baseline | 8 | 2 | 63.086% [-123.040, 249.212] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 0.617 [-36.608, 37.842] |
| transfer | reused-service | baseline | 16 | 2 | 88.867% [-52.589, 230.323] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.773 [-22.518, 34.065] |
| transfer | reused-service | baseline | 32 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 64 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 128 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | shuffled-communication | baseline | 0 | 2 | 50.977% [18.715, 83.238] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.758 [-8.210, 4.695] |
| transfer | shuffled-communication | baseline | 8 | 2 | 50.977% [18.715, 83.238] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.758 [-8.210, 4.695] |
| transfer | shuffled-communication | baseline | 16 | 2 | 51.758% [29.423, 74.093] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.602 [-6.069, 2.865] |
| transfer | shuffled-communication | baseline | 32 | 2 | 51.953% [32.100, 71.807] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.562 [-5.533, 2.408] |
| transfer | shuffled-communication | baseline | 64 | 2 | 51.562% [51.562, 51.562] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.641 [-1.641, -1.641] |
| transfer | shuffled-communication | baseline | 128 | 2 | 52.539% [40.131, 64.947] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.445 [-3.927, 1.036] |
| transfer | shuffled-communication | baseline | 192 | 2 | 52.539% [40.131, 64.947] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.445 [-3.927, 1.036] |

### Paired effects

| Stage | Left minus right | Condition | Train | Metric | Seeds | Difference |
|---|---|---|---:|---|---:|---|
| discovery | learned minus no-expansion | fresh-rule | 384 | accuracy | 2 | 49.219% [14.475, 83.962] |
| discovery | learned minus no-expansion | fresh-rule | 384 | coverage | 2 | 0.195% [-2.286, 2.677] |
| discovery | learned minus frozen | fresh-rule | 384 | accuracy | 2 | 100.000% [100.000, 100.000] |
| discovery | learned minus frozen | fresh-rule | 384 | coverage | 2 | 100.000% [100.000, 100.000] |
| discovery | learned minus shuffled-feedback | fresh-rule | 384 | accuracy | 2 | 50.391% [10.684, 90.098] |
| discovery | learned minus shuffled-feedback | fresh-rule | 384 | coverage | 2 | 0.195% [-2.286, 2.677] |
| discovery | learned minus shuffled-communication | fresh-rule | 384 | accuracy | 2 | 51.758% [14.533, 88.983] |
| discovery | learned minus shuffled-communication | fresh-rule | 384 | coverage | 2 | 0.391% [0.391, 0.391] |
| discovery | learned minus no-memory | fresh-rule | 384 | accuracy | 2 | 66.406% [-216.505, 349.318] |
| discovery | learned minus no-memory | fresh-rule | 384 | coverage | 2 | 37.500% [-438.983, 513.983] |
| discovery | learned minus previous-pair | fresh-rule | 384 | accuracy | 2 | 50.977% [8.788, 93.165] |
| discovery | learned minus previous-pair | fresh-rule | 384 | coverage | 2 | 0.195% [-2.286, 2.677] |
| source | learned minus no-expansion | baseline | 192 | accuracy | 2 | 49.023% [16.762, 81.285] |
| source | learned minus no-expansion | baseline | 192 | meanReturn | 2 | 9.781 [3.031, 16.531] |
| source | learned minus frozen | baseline | 192 | accuracy | 2 | 49.805% [27.470, 72.140] |
| source | learned minus frozen | baseline | 192 | meanReturn | 2 | 9.961 [5.494, 14.428] |
| source | learned minus shuffled-feedback | baseline | 192 | accuracy | 2 | 52.930% [35.558, 70.301] |
| source | learned minus shuffled-feedback | baseline | 192 | meanReturn | 2 | 10.562 [7.386, 13.739] |
| source | learned minus shuffled-communication | baseline | 192 | accuracy | 2 | 49.609% [34.719, 64.499] |
| source | learned minus shuffled-communication | baseline | 192 | meanReturn | 2 | 9.875 [6.897, 12.853] |
| source | learned minus no-memory | baseline | 192 | accuracy | 2 | 49.023% [16.762, 81.285] |
| source | learned minus no-memory | baseline | 192 | meanReturn | 2 | 9.805 [3.352, 16.257] |
| source | learned minus previous-pair | baseline | 192 | accuracy | 2 | 50.391% [35.501, 65.281] |
| source | learned minus previous-pair | baseline | 192 | meanReturn | 2 | 10.055 [7.374, 12.735] |
| source | learned minus flat | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| source | learned minus flat | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| source | learned minus fixed-sum | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| source | learned minus fixed-sum | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 0 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus no-expansion | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus no-expansion | baseline | 0 | meanReturn | 2 | -0.023 [-0.321, 0.274] |
| transfer | reused-service minus shuffled-communication | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus shuffled-communication | baseline | 0 | meanReturn | 2 | -0.047 [-0.047, -0.047] |
| transfer | reused-service minus flat | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 0 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 0 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 8 | accuracy | 2 | 13.477% [-157.759, 184.713] |
| transfer | reused-service minus fresh-service | baseline | 8 | meanReturn | 2 | 2.695 [-31.552, 36.943] |
| transfer | reused-service minus no-expansion | baseline | 8 | accuracy | 2 | 12.109% [-141.755, 165.974] |
| transfer | reused-service minus no-expansion | baseline | 8 | meanReturn | 2 | 2.398 [-28.077, 32.873] |
| transfer | reused-service minus shuffled-communication | baseline | 8 | accuracy | 2 | 12.109% [-141.755, 165.974] |
| transfer | reused-service minus shuffled-communication | baseline | 8 | meanReturn | 2 | 2.375 [-28.398, 33.148] |
| transfer | reused-service minus flat | baseline | 8 | accuracy | 2 | 12.109% [-141.755, 165.974] |
| transfer | reused-service minus flat | baseline | 8 | meanReturn | 2 | 2.422 [-28.351, 33.195] |
| transfer | reused-service minus fixed-sum | baseline | 8 | accuracy | 2 | 12.109% [-141.755, 165.974] |
| transfer | reused-service minus fixed-sum | baseline | 8 | meanReturn | 2 | 2.422 [-28.351, 33.195] |
| transfer | reused-service minus fresh-service | baseline | 16 | accuracy | 2 | 37.891% [-135.827, 211.608] |
| transfer | reused-service minus fresh-service | baseline | 16 | meanReturn | 2 | 7.578 [-27.165, 42.322] |
| transfer | reused-service minus no-expansion | baseline | 16 | accuracy | 2 | 37.891% [-135.827, 211.608] |
| transfer | reused-service minus no-expansion | baseline | 16 | meanReturn | 2 | 7.555 [-27.487, 42.596] |
| transfer | reused-service minus shuffled-communication | baseline | 16 | accuracy | 2 | 37.109% [-126.682, 200.900] |
| transfer | reused-service minus shuffled-communication | baseline | 16 | meanReturn | 2 | 7.375 [-25.383, 40.133] |
| transfer | reused-service minus flat | baseline | 16 | accuracy | 2 | 24.609% [-159.035, 208.254] |
| transfer | reused-service minus flat | baseline | 16 | meanReturn | 2 | 4.922 [-31.807, 41.651] |
| transfer | reused-service minus fixed-sum | baseline | 16 | accuracy | 2 | 16.797% [-67.580, 101.174] |
| transfer | reused-service minus fixed-sum | baseline | 16 | meanReturn | 2 | 3.359 [-13.516, 20.235] |
| transfer | reused-service minus fresh-service | baseline | 32 | accuracy | 2 | 49.023% [16.762, 81.285] |
| transfer | reused-service minus fresh-service | baseline | 32 | meanReturn | 2 | 9.758 [3.305, 16.210] |
| transfer | reused-service minus no-expansion | baseline | 32 | accuracy | 2 | 49.023% [36.615, 61.432] |
| transfer | reused-service minus no-expansion | baseline | 32 | meanReturn | 2 | 9.781 [7.002, 12.561] |
| transfer | reused-service minus shuffled-communication | baseline | 32 | accuracy | 2 | 48.047% [28.193, 67.900] |
| transfer | reused-service minus shuffled-communication | baseline | 32 | meanReturn | 2 | 9.562 [5.592, 13.533] |
| transfer | reused-service minus flat | baseline | 32 | accuracy | 2 | 12.305% [-144.041, 168.651] |
| transfer | reused-service minus flat | baseline | 32 | meanReturn | 2 | 2.461 [-28.808, 33.730] |
| transfer | reused-service minus fixed-sum | baseline | 32 | accuracy | 2 | 5.078% [-59.446, 69.602] |
| transfer | reused-service minus fixed-sum | baseline | 32 | meanReturn | 2 | 1.016 [-11.889, 13.920] |
| transfer | reused-service minus fresh-service | baseline | 64 | accuracy | 2 | 50.391% [35.501, 65.281] |
| transfer | reused-service minus fresh-service | baseline | 64 | meanReturn | 2 | 10.055 [7.374, 12.735] |
| transfer | reused-service minus no-expansion | baseline | 64 | accuracy | 2 | 48.242% [25.907, 70.577] |
| transfer | reused-service minus no-expansion | baseline | 64 | meanReturn | 2 | 9.625 [4.860, 14.390] |
| transfer | reused-service minus shuffled-communication | baseline | 64 | accuracy | 2 | 48.438% [48.438, 48.438] |
| transfer | reused-service minus shuffled-communication | baseline | 64 | meanReturn | 2 | 9.641 [9.641, 9.641] |
| transfer | reused-service minus flat | baseline | 64 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 64 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 64 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 64 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 128 | accuracy | 2 | 12.109% [-141.755, 165.974] |
| transfer | reused-service minus fresh-service | baseline | 128 | meanReturn | 2 | 2.422 [-28.351, 33.195] |
| transfer | reused-service minus no-expansion | baseline | 128 | accuracy | 2 | 48.242% [25.907, 70.577] |
| transfer | reused-service minus no-expansion | baseline | 128 | meanReturn | 2 | 9.625 [4.860, 14.390] |
| transfer | reused-service minus shuffled-communication | baseline | 128 | accuracy | 2 | 47.461% [35.053, 59.869] |
| transfer | reused-service minus shuffled-communication | baseline | 128 | meanReturn | 2 | 9.445 [6.964, 11.927] |
| transfer | reused-service minus flat | baseline | 128 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 128 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 128 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 128 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus no-expansion | baseline | 192 | accuracy | 2 | 48.242% [25.907, 70.577] |
| transfer | reused-service minus no-expansion | baseline | 192 | meanReturn | 2 | 9.625 [4.860, 14.390] |
| transfer | reused-service minus shuffled-communication | baseline | 192 | accuracy | 2 | 47.461% [35.053, 59.869] |
| transfer | reused-service minus shuffled-communication | baseline | 192 | meanReturn | 2 | 9.445 [6.964, 11.927] |
| transfer | reused-service minus flat | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | meanReturn | 2 | -0.004 [-0.004, -0.004] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | queryRate | 2 | -0.651% [-0.651, -0.651] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | accuracy | 2 | 44.076% [-16.312, 104.463] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | meanReturn | 2 | 7.839 [-4.239, 19.916] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | queryRate | 2 | 99.219% [99.219, 99.219] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | accuracy | 2 | 20.312% [5.422, 35.203] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | meanReturn | 2 | 3.085 [0.107, 6.063] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | queryRate | 2 | 99.349% [99.349, 99.349] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | accuracy | 2 | 16.081% [8.636, 23.526] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | meanReturn | 2 | 2.402 [0.838, 3.965] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | queryRate | 2 | 83.008% [75.563, 90.453] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | accuracy | 2 | 13.737% [-13.562, 41.035] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | meanReturn | 2 | 1.895 [-3.565, 7.354] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | queryRate | 2 | 86.849% [86.849, 86.849] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | meanReturn | 2 | 0.022 [0.022, 0.022] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | queryRate | 2 | -0.651% [-0.651, -0.651] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | accuracy | 2 | 44.076% [-16.312, 104.463] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | meanReturn | 2 | 3.870 [-8.208, 15.947] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | queryRate | 2 | 99.219% [99.219, 99.219] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | accuracy | 2 | 20.312% [5.422, 35.203] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | meanReturn | 2 | -0.889 [-3.867, 2.089] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | queryRate | 2 | 99.349% [99.349, 99.349] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | accuracy | 2 | 16.081% [8.636, 23.526] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | meanReturn | 2 | -0.919 [-2.780, 0.943] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | queryRate | 2 | 83.008% [75.563, 90.453] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | accuracy | 2 | 13.737% [-13.562, 41.035] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | meanReturn | 2 | -1.579 [-7.039, 3.880] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | queryRate | 2 | 86.849% [86.849, 86.849] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | meanReturn | 2 | -0.004 [-0.004, -0.004] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | queryRate | 2 | -0.651% [-0.651, -0.651] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | accuracy | 2 | 46.875% [22.058, 71.692] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | meanReturn | 2 | 8.398 [3.435, 13.362] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | queryRate | 2 | 99.219% [99.219, 99.219] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | accuracy | 2 | 38.542% [-1.165, 78.249] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | meanReturn | 2 | 6.730 [-1.211, 14.672] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | queryRate | 2 | 99.349% [99.349, 99.349] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | accuracy | 2 | 24.674% [2.339, 47.010] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | meanReturn | 2 | 4.206 [-0.484, 8.897] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | queryRate | 2 | 74.414% [52.079, 96.749] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | accuracy | 2 | 29.818% [9.964, 49.671] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | meanReturn | 2 | 5.111 [1.140, 9.081] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | queryRate | 2 | 86.849% [86.849, 86.849] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | accuracy | 2 | -80.859% [-89.132, -72.587] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | meanReturn | 2 | -5.473 [-6.134, -4.811] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | queryRate | 2 | -99.609% [-99.609, -99.609] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | accuracy | 2 | -55.078% [-114.638, 4.482] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | meanReturn | 2 | 0.896 [-11.116, 12.907] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | queryRate | 2 | 0.260% [0.260, 0.260] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | accuracy | 2 | -78.841% [-94.558, -63.124] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | meanReturn | 2 | -3.858 [-6.902, -0.814] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | queryRate | 2 | 0.391% [0.391, 0.391] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | accuracy | 2 | -26.432% [-183.605, 130.741] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | meanReturn | 2 | -1.406 [-8.173, 5.360] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | queryRate | 2 | -5.208% [-49.879, 39.462] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | accuracy | 2 | -66.406% [-73.024, -59.788] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | meanReturn | 2 | -3.746 [-8.147, 0.655] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | queryRate | 2 | -12.109% [-12.109, -12.109] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | meanReturn | 2 | -0.009 [-0.009, -0.009] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | queryRate | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | accuracy | 2 | 44.076% [-16.312, 104.463] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | meanReturn | 2 | 7.833 [-4.244, 19.911] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | queryRate | 2 | 99.740% [99.740, 99.740] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | accuracy | 2 | 4.753% [-1.038, 10.543] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | meanReturn | 2 | -0.033 [-1.191, 1.126] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | queryRate | 2 | 99.870% [99.870, 99.870] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | accuracy | 2 | 4.492% [-1.298, 10.283] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | meanReturn | 2 | -0.037 [-1.253, 1.179] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | queryRate | 2 | 95.117% [89.327, 100.908] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | accuracy | 2 | 13.737% [-13.562, 41.035] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | meanReturn | 2 | 1.889 [-3.570, 7.349] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | queryRate | 2 | 87.370% [87.370, 87.370] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | meanReturn | 2 | -0.004 [-0.004, -0.004] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | queryRate | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | accuracy | 2 | 44.076% [-16.312, 104.463] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | meanReturn | 2 | 3.844 [-8.234, 15.921] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | queryRate | 2 | 99.740% [99.740, 99.740] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | accuracy | 2 | 4.753% [-1.038, 10.543] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | meanReturn | 2 | -4.027 [-5.185, -2.869] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | queryRate | 2 | 99.870% [99.870, 99.870] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | accuracy | 2 | 4.492% [-1.298, 10.283] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | meanReturn | 2 | -3.842 [-5.289, -2.394] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | queryRate | 2 | 95.117% [89.327, 100.908] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | accuracy | 2 | 13.737% [-13.562, 41.035] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | meanReturn | 2 | -1.605 [-7.065, 3.854] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | queryRate | 2 | 87.370% [87.370, 87.370] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | meanReturn | 2 | -0.009 [-0.009, -0.009] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | queryRate | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | accuracy | 2 | 46.875% [22.058, 71.692] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | meanReturn | 2 | 8.393 [3.430, 13.357] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | queryRate | 2 | 99.740% [99.740, 99.740] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | accuracy | 2 | 13.477% [6.032, 20.922] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | meanReturn | 2 | 1.712 [0.223, 3.201] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | queryRate | 2 | 99.870% [99.870, 99.870] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | accuracy | 2 | 11.719% [8.410, 15.028] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | meanReturn | 2 | 1.480 [0.786, 2.175] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | queryRate | 2 | 87.891% [84.582, 91.200] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | accuracy | 2 | 29.818% [9.964, 49.671] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | meanReturn | 2 | 5.105 [1.135, 9.076] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | queryRate | 2 | 87.370% [87.370, 87.370] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | accuracy | 2 | -74.349% [-112.401, -36.297] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | meanReturn | 2 | -5.038 [-7.553, -2.523] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | queryRate | 2 | -91.016% [-143.958, -38.073] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | accuracy | 2 | -48.568% [-61.803, -35.332] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | meanReturn | 2 | 1.331 [-7.504, 10.166] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | queryRate | 2 | 8.854% [-44.088, 61.797] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | accuracy | 2 | -87.891% [-140.833, -34.948] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | meanReturn | 2 | -6.535 [-10.936, -2.134] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | queryRate | 2 | 8.984% [-43.958, 61.927] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | accuracy | 2 | -18.555% [-82.251, 45.142] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | meanReturn | 2 | -1.412 [-6.301, 3.477] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | queryRate | 2 | 7.617% [-42.844, 58.078] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | accuracy | 2 | -59.896% [-112.838, -6.953] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | meanReturn | 2 | -3.311 [-10.889, 4.266] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | queryRate | 2 | -3.516% [-56.458, 49.427] |

### Native archives

- relation: 238,387 bytes; signature=True, behavior=True; `relation.gojo`.
- consumer: 71,294 bytes; signature=True, behavior=True; `consumer.gojo`.
- refresh-0: 104,543 bytes; signature=True, behavior=True; `refresh-0.gojo`.
- refresh-4: 98,781 bytes; signature=True, behavior=True; `refresh-4.gojo`.

Interval method: two-sided 95% Student t on seed means or paired seed differences; df>30 uses conservative df30 critical; exploratory contrasts without multiplicity correction.

## pilot-03

Status: complete. Settings: `{"consumerTrain": 192, "discoveryTrain": 384, "pilot": true, "probePrice": 2, "refreshTest": 768, "refreshTrain": 2048, "seedStart": 3, "seeds": 2, "switchProbability": 0.04, "testEpisodes": 256}`.

Counts: `{"resourceIdentitiesChecked": 212, "runnerEvaluations": 107008, "runnerTrainingEvents": 19712, "savedTestEvaluations": 107008, "sourceHashesChecked": 123, "uniqueRows": 226}`.

- Pilot seed data: do not present as fresh confirmation.
- Some current source hashes differ from the run manifest; preserved run data remains historical.

| Stage | Controller | Condition | Train | Seeds | Accuracy | Coverage | Acquisitions | Return |
|---|---|---|---:|---:|---|---|---|---|
| discovery | frozen | fresh-rule | 384 | 2 | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | learned | fresh-rule | 384 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | no-expansion | fresh-rule | 384 | 2 | 42.578% [17.761, 67.395] | 90.234% [-33.850, 214.318] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | no-memory | fresh-rule | 384 | 2 | 42.188% [-27.300, 111.675] | 88.281% [-60.620, 237.182] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | previous-pair | fresh-rule | 384 | 2 | 46.289% [4.100, 88.478] | 87.891% [-61.010, 236.791] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | shuffled-communication | fresh-rule | 384 | 2 | 51.953% [32.100, 71.807] | 99.609% [99.609, 99.609] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | shuffled-feedback | fresh-rule | 384 | 2 | 49.023% [6.835, 91.212] | 99.805% [97.323, 102.286] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| refresh | always-probe | delayed-history/baseline | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.000 [6.000, 6.000] |
| refresh | always-probe | delayed-history/expensive-probe | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 3.000 [3.000, 3.000] |
| refresh | always-probe | delayed-history/fast-switch | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.000 [6.000, 6.000] |
| refresh | always-probe | delayed-history/missing-signals | 2048 | 2 | 79.557% [64.667, 94.447] | 79.557% [64.667, 94.447] | 100.000% [100.000, 100.000] | 4.365 [3.173, 5.556] |
| refresh | always-probe | immediate/baseline | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.000 [6.000, 6.000] |
| refresh | always-probe | immediate/expensive-probe | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 3.000 [3.000, 3.000] |
| refresh | always-probe | immediate/fast-switch | 2048 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.000 [6.000, 6.000] |
| refresh | always-probe | immediate/missing-signals | 2048 | 2 | 79.557% [64.667, 94.447] | 79.557% [64.667, 94.447] | 100.000% [100.000, 100.000] | 4.365 [3.173, 5.556] |
| refresh | failure-probe | delayed-history/baseline | 2048 | 2 | 86.719% [85.064, 88.373] | 100.000% [100.000, 100.000] | 13.151% [11.497, 14.805] | 5.081 [4.783, 5.379] |
| refresh | failure-probe | delayed-history/expensive-probe | 2048 | 2 | 86.719% [85.064, 88.373] | 100.000% [100.000, 100.000] | 13.151% [11.497, 14.805] | 4.686 [4.438, 4.934] |
| refresh | failure-probe | delayed-history/fast-switch | 2048 | 2 | 75.065% [72.583, 77.547] | 100.000% [100.000, 100.000] | 24.805% [19.014, 30.595] | 2.517 [1.905, 3.129] |
| refresh | failure-probe | delayed-history/missing-signals | 2048 | 2 | 12.565% [-147.090, 172.220] | 13.932% [-163.094, 190.959] | 1.497% [-15.874, 18.869] | 0.811 [-9.529, 11.152] |
| refresh | failure-probe | immediate/baseline | 2048 | 2 | 96.745% [96.745, 96.745] | 100.000% [100.000, 100.000] | 3.385% [3.385, 3.385] | 7.281 [7.281, 7.281] |
| refresh | failure-probe | immediate/expensive-probe | 2048 | 2 | 96.745% [96.745, 96.745] | 100.000% [100.000, 100.000] | 3.385% [3.385, 3.385] | 7.180 [7.180, 7.180] |
| refresh | failure-probe | immediate/fast-switch | 2048 | 2 | 90.039% [75.976, 104.102] | 100.000% [100.000, 100.000] | 10.026% [-4.864, 24.916] | 5.807 [2.697, 8.918] |
| refresh | failure-probe | immediate/missing-signals | 2048 | 2 | 0.911% [-10.670, 12.493] | 0.977% [-11.432, 13.385] | 0.195% [-0.632, 1.023] | 0.061 [-0.749, 0.872] |
| refresh | fixed-interval | delayed-history/baseline | 2048 | 2 | 88.346% [82.556, 94.137] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.419 [4.261, 6.577] |
| refresh | fixed-interval | delayed-history/expensive-probe | 2048 | 2 | 88.346% [82.556, 94.137] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.044 [3.886, 6.202] |
| refresh | fixed-interval | delayed-history/fast-switch | 2048 | 2 | 71.224% [48.062, 94.386] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 1.995 [-2.638, 6.627] |
| refresh | fixed-interval | delayed-history/missing-signals | 2048 | 2 | 68.685% [59.585, 77.784] | 78.125% [51.654, 104.596] | 12.500% [12.500, 12.500] | 4.112 [2.755, 5.469] |
| refresh | fixed-interval | immediate/baseline | 2048 | 2 | 88.346% [82.556, 94.137] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.419 [4.261, 6.577] |
| refresh | fixed-interval | immediate/expensive-probe | 2048 | 2 | 88.346% [82.556, 94.137] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.044 [3.886, 6.202] |
| refresh | fixed-interval | immediate/fast-switch | 2048 | 2 | 71.224% [48.062, 94.386] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 1.995 [-2.638, 6.627] |
| refresh | fixed-interval | immediate/missing-signals | 2048 | 2 | 68.685% [59.585, 77.784] | 78.125% [51.654, 104.596] | 12.500% [12.500, 12.500] | 4.112 [2.755, 5.469] |
| refresh | learned-refresh | delayed-history/baseline | 2048 | 2 | 97.786% [71.315, 124.258] | 99.870% [99.870, 99.870] | 62.174% [-410.172, 534.521] | 6.329 [2.177, 10.482] |
| refresh | learned-refresh | delayed-history/expensive-probe | 2048 | 2 | 97.786% [71.315, 124.258] | 99.870% [99.870, 99.870] | 62.174% [-410.172, 534.521] | 4.464 [-13.859, 22.787] |
| refresh | learned-refresh | delayed-history/fast-switch | 2048 | 2 | 91.602% [-13.456, 196.659] | 99.870% [99.870, 99.870] | 62.174% [-410.172, 534.521] | 5.092 [-6.472, 16.657] |
| refresh | learned-refresh | delayed-history/missing-signals | 2048 | 2 | 1.042% [-3.922, 6.005] | 1.042% [-3.922, 6.005] | 0.846% [-4.944, 6.637] | 0.066 [-0.215, 0.348] |
| refresh | learned-refresh | immediate/baseline | 2048 | 2 | 96.615% [96.615, 96.615] | 99.870% [99.870, 99.870] | 3.385% [3.385, 3.385] | 7.271 [7.271, 7.271] |
| refresh | learned-refresh | immediate/expensive-probe | 2048 | 2 | 96.615% [96.615, 96.615] | 99.870% [99.870, 99.870] | 3.385% [3.385, 3.385] | 7.169 [7.169, 7.169] |
| refresh | learned-refresh | immediate/fast-switch | 2048 | 2 | 89.909% [75.846, 103.972] | 99.870% [99.870, 99.870] | 10.026% [-4.864, 24.916] | 5.797 [2.687, 8.907] |
| refresh | learned-refresh | immediate/missing-signals | 2048 | 2 | 6.576% [-57.121, 70.272] | 6.836% [-58.515, 72.187] | 0.586% [-1.896, 3.068] | 0.483 [-4.364, 5.331] |
| refresh | never-probe | delayed-history/baseline | 2048 | 2 | 45.117% [-174.098, 264.332] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -2.979 [-46.822, 40.864] |
| refresh | never-probe | delayed-history/expensive-probe | 2048 | 2 | 45.117% [-174.098, 264.332] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -2.983 [-46.826, 40.860] |
| refresh | never-probe | delayed-history/fast-switch | 2048 | 2 | 49.870% [23.399, 76.341] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -2.029 [-7.323, 3.266] |
| refresh | never-probe | delayed-history/missing-signals | 2048 | 2 | 13.932% [-163.094, 190.959] | 50.000% [-585.310, 685.310] | 0.130% [0.130, 0.130] | -3.216 [-44.048, 37.616] |
| refresh | never-probe | immediate/baseline | 2048 | 2 | 45.117% [-174.098, 264.332] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -2.979 [-46.822, 40.864] |
| refresh | never-probe | immediate/expensive-probe | 2048 | 2 | 45.117% [-174.098, 264.332] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -2.983 [-46.826, 40.860] |
| refresh | never-probe | immediate/fast-switch | 2048 | 2 | 49.870% [23.399, 76.341] | 100.000% [100.000, 100.000] | 0.130% [0.130, 0.130] | -2.029 [-7.323, 3.266] |
| refresh | never-probe | immediate/missing-signals | 2048 | 2 | 13.932% [-163.094, 190.959] | 50.000% [-585.310, 685.310] | 0.130% [0.130, 0.130] | -3.216 [-44.048, 37.616] |
| refresh | outcome-memory | delayed-history/baseline | 2048 | 2 | 84.245% [80.936, 87.554] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 4.849 [4.187, 5.511] |
| refresh | outcome-memory | delayed-history/expensive-probe | 2048 | 2 | 84.245% [80.936, 87.554] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 4.849 [4.187, 5.511] |
| refresh | outcome-memory | delayed-history/fast-switch | 2048 | 2 | 63.216% [32.609, 93.824] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 0.643 [-5.478, 6.765] |
| refresh | outcome-memory | delayed-history/missing-signals | 2048 | 2 | 84.245% [80.936, 87.554] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 4.849 [4.187, 5.511] |
| refresh | outcome-memory | immediate/baseline | 2048 | 2 | 96.484% [96.484, 96.484] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.297 [7.297, 7.297] |
| refresh | outcome-memory | immediate/expensive-probe | 2048 | 2 | 96.484% [96.484, 96.484] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.297 [7.297, 7.297] |
| refresh | outcome-memory | immediate/fast-switch | 2048 | 2 | 88.737% [71.365, 106.109] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 5.747 [2.273, 9.222] |
| refresh | outcome-memory | immediate/missing-signals | 2048 | 2 | 96.484% [96.484, 96.484] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.297 [7.297, 7.297] |
| source | fixed-sum | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | flat | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | frozen | baseline | 192 | 2 | 50.391% [25.574, 75.207] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.922 [-6.885, 3.041] |
| source | learned | absent-state | 192 | 2 | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 0.000 [0.000, 0.000] |
| source | learned | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | learned | inverted-input | 192 | 2 | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -12.000 [-12.000, -12.000] |
| source | learned | long-delay | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | learned | missing-signals | 192 | 2 | 78.516% [63.626, 93.406] | 78.516% [63.626, 93.406] | 100.000% [100.000, 100.000] | 6.281 [5.090, 7.472] |
| source | learned | opposite-state | 192 | 2 | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -12.000 [-12.000, -12.000] |
| source | learned | same-state | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | neutral-left | baseline | 0 | 2 | 50.781% [25.964, 75.598] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.844 [-6.807, 3.120] |
| source | no-expansion | baseline | 192 | 2 | 50.391% [25.574, 75.207] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.922 [-6.885, 3.041] |
| source | no-memory | baseline | 192 | 2 | 49.414% [37.006, 61.822] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.117 [-4.599, 0.364] |
| source | previous-pair | baseline | 192 | 2 | 47.070% [29.699, 64.442] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -2.539 [-6.013, 0.935] |
| source | shuffled-communication | baseline | 192 | 2 | 49.609% [29.756, 69.463] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -2.031 [-6.002, 1.939] |
| source | shuffled-feedback | baseline | 192 | 2 | 49.805% [32.433, 67.176] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -2.016 [-5.788, 1.757] |
| transfer | fixed-sum | baseline | 0 | 2 | 49.219% [24.402, 74.036] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.156 [-7.120, 2.807] |
| transfer | fixed-sum | baseline | 8 | 2 | 59.961% [-101.348, 221.270] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -0.008 [-32.270, 32.254] |
| transfer | fixed-sum | baseline | 16 | 2 | 74.023% [56.652, 91.395] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 2.805 [-0.670, 6.279] |
| transfer | fixed-sum | baseline | 32 | 2 | 87.305% [84.823, 89.786] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.461 [4.965, 5.957] |
| transfer | fixed-sum | baseline | 64 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fixed-sum | baseline | 128 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fixed-sum | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | flat | baseline | 0 | 2 | 49.219% [24.402, 74.036] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.156 [-7.120, 2.807] |
| transfer | flat | baseline | 8 | 2 | 54.883% [-41.903, 151.668] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.023 [-20.381, 18.334] |
| transfer | flat | baseline | 16 | 2 | 61.914% [54.469, 69.359] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 0.383 [-1.106, 1.872] |
| transfer | flat | baseline | 32 | 2 | 87.305% [84.823, 89.786] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.461 [4.965, 5.957] |
| transfer | flat | baseline | 64 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | flat | baseline | 128 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | flat | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fresh-service | baseline | 0 | 2 | 49.219% [24.402, 74.036] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.156 [-7.120, 2.807] |
| transfer | fresh-service | baseline | 8 | 2 | 50.781% [30.928, 70.635] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.844 [-5.814, 2.127] |
| transfer | fresh-service | baseline | 16 | 2 | 48.828% [28.975, 68.682] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.234 [-6.205, 1.736] |
| transfer | fresh-service | baseline | 32 | 2 | 49.805% [37.396, 62.213] | 99.805% [97.323, 102.286] | 100.000% [100.000, 100.000] | -2.016 [-4.795, 0.764] |
| transfer | fresh-service | baseline | 64 | 2 | 50.000% [40.073, 59.927] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.000 [-3.985, -0.015] |
| transfer | fresh-service | baseline | 128 | 2 | 88.867% [-52.589, 230.323] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.773 [-22.518, 34.065] |
| transfer | fresh-service | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | no-expansion | baseline | 0 | 2 | 49.219% [24.402, 74.036] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.156 [-7.120, 2.807] |
| transfer | no-expansion | baseline | 8 | 2 | 50.781% [30.928, 70.635] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.844 [-5.814, 2.127] |
| transfer | no-expansion | baseline | 16 | 2 | 48.828% [28.975, 68.682] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.234 [-6.205, 1.736] |
| transfer | no-expansion | baseline | 32 | 2 | 48.633% [31.261, 66.005] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.273 [-5.748, 1.201] |
| transfer | no-expansion | baseline | 64 | 2 | 49.805% [47.323, 52.286] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.039 [-2.535, -1.543] |
| transfer | no-expansion | baseline | 128 | 2 | 50.977% [43.532, 58.422] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.805 [-3.294, -0.316] |
| transfer | no-expansion | baseline | 192 | 2 | 49.609% [44.646, 54.573] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.078 [-3.071, -1.085] |
| transfer | reused-service | baseline | 0 | 2 | 49.219% [24.402, 74.036] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.156 [-7.120, 2.807] |
| transfer | reused-service | baseline | 8 | 2 | 59.961% [-101.348, 221.270] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -0.008 [-32.270, 32.254] |
| transfer | reused-service | baseline | 16 | 2 | 86.328% [-87.390, 260.046] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.266 [-29.478, 40.009] |
| transfer | reused-service | baseline | 32 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 64 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 128 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 192 | 2 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | shuffled-communication | baseline | 0 | 2 | 49.023% [21.725, 76.322] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -2.148 [-7.608, 3.311] |
| transfer | shuffled-communication | baseline | 8 | 2 | 49.023% [21.725, 76.322] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -2.148 [-7.608, 3.311] |
| transfer | shuffled-communication | baseline | 16 | 2 | 49.414% [27.079, 71.749] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -2.070 [-6.537, 2.397] |
| transfer | shuffled-communication | baseline | 32 | 2 | 50.586% [43.141, 58.031] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.836 [-3.325, -0.347] |
| transfer | shuffled-communication | baseline | 64 | 2 | 51.367% [38.959, 63.776] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.680 [-4.161, 0.802] |
| transfer | shuffled-communication | baseline | 128 | 2 | 51.367% [38.959, 63.776] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.680 [-4.161, 0.802] |
| transfer | shuffled-communication | baseline | 192 | 2 | 51.758% [44.313, 59.203] | 99.609% [99.609, 99.609] | 100.000% [100.000, 100.000] | -1.602 [-3.091, -0.113] |

### Paired effects

| Stage | Left minus right | Condition | Train | Metric | Seeds | Difference |
|---|---|---|---:|---|---:|---|
| discovery | learned minus no-expansion | fresh-rule | 384 | accuracy | 2 | 57.422% [32.605, 82.239] |
| discovery | learned minus no-expansion | fresh-rule | 384 | coverage | 2 | 9.766% [-114.318, 133.850] |
| discovery | learned minus frozen | fresh-rule | 384 | accuracy | 2 | 100.000% [100.000, 100.000] |
| discovery | learned minus frozen | fresh-rule | 384 | coverage | 2 | 100.000% [100.000, 100.000] |
| discovery | learned minus shuffled-feedback | fresh-rule | 384 | accuracy | 2 | 50.977% [8.788, 93.165] |
| discovery | learned minus shuffled-feedback | fresh-rule | 384 | coverage | 2 | 0.195% [-2.286, 2.677] |
| discovery | learned minus shuffled-communication | fresh-rule | 384 | accuracy | 2 | 48.047% [28.193, 67.900] |
| discovery | learned minus shuffled-communication | fresh-rule | 384 | coverage | 2 | 0.391% [0.391, 0.391] |
| discovery | learned minus no-memory | fresh-rule | 384 | accuracy | 2 | 57.812% [-11.675, 127.300] |
| discovery | learned minus no-memory | fresh-rule | 384 | coverage | 2 | 11.719% [-137.182, 160.620] |
| discovery | learned minus previous-pair | fresh-rule | 384 | accuracy | 2 | 53.711% [11.522, 95.900] |
| discovery | learned minus previous-pair | fresh-rule | 384 | coverage | 2 | 12.109% [-136.791, 161.010] |
| source | learned minus no-expansion | baseline | 192 | accuracy | 2 | 49.609% [24.793, 74.426] |
| source | learned minus no-expansion | baseline | 192 | meanReturn | 2 | 9.922 [4.959, 14.885] |
| source | learned minus frozen | baseline | 192 | accuracy | 2 | 49.609% [24.793, 74.426] |
| source | learned minus frozen | baseline | 192 | meanReturn | 2 | 9.922 [4.959, 14.885] |
| source | learned minus shuffled-feedback | baseline | 192 | accuracy | 2 | 50.195% [32.824, 67.567] |
| source | learned minus shuffled-feedback | baseline | 192 | meanReturn | 2 | 10.016 [6.243, 13.788] |
| source | learned minus shuffled-communication | baseline | 192 | accuracy | 2 | 50.391% [30.537, 70.244] |
| source | learned minus shuffled-communication | baseline | 192 | meanReturn | 2 | 10.031 [6.061, 14.002] |
| source | learned minus no-memory | baseline | 192 | accuracy | 2 | 50.586% [38.178, 62.994] |
| source | learned minus no-memory | baseline | 192 | meanReturn | 2 | 10.117 [7.636, 12.599] |
| source | learned minus previous-pair | baseline | 192 | accuracy | 2 | 52.930% [35.558, 70.301] |
| source | learned minus previous-pair | baseline | 192 | meanReturn | 2 | 10.539 [7.065, 14.013] |
| source | learned minus flat | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| source | learned minus flat | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| source | learned minus fixed-sum | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| source | learned minus fixed-sum | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 0 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus no-expansion | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus no-expansion | baseline | 0 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus shuffled-communication | baseline | 0 | accuracy | 2 | 0.195% [-2.286, 2.677] |
| transfer | reused-service minus shuffled-communication | baseline | 0 | meanReturn | 2 | -0.008 [-0.504, 0.489] |
| transfer | reused-service minus flat | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 0 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 0 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 0 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 8 | accuracy | 2 | 9.180% [-132.276, 150.635] |
| transfer | reused-service minus fresh-service | baseline | 8 | meanReturn | 2 | 1.836 [-26.455, 30.127] |
| transfer | reused-service minus no-expansion | baseline | 8 | accuracy | 2 | 9.180% [-132.276, 150.635] |
| transfer | reused-service minus no-expansion | baseline | 8 | meanReturn | 2 | 1.836 [-26.455, 30.127] |
| transfer | reused-service minus shuffled-communication | baseline | 8 | accuracy | 2 | 10.938% [-123.073, 144.948] |
| transfer | reused-service minus shuffled-communication | baseline | 8 | meanReturn | 2 | 2.141 [-24.662, 28.943] |
| transfer | reused-service minus flat | baseline | 8 | accuracy | 2 | 5.078% [-59.446, 69.602] |
| transfer | reused-service minus flat | baseline | 8 | meanReturn | 2 | 1.016 [-11.889, 13.920] |
| transfer | reused-service minus fixed-sum | baseline | 8 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 8 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 16 | accuracy | 2 | 37.500% [-156.071, 231.071] |
| transfer | reused-service minus fresh-service | baseline | 16 | meanReturn | 2 | 7.500 [-31.214, 46.214] |
| transfer | reused-service minus no-expansion | baseline | 16 | accuracy | 2 | 37.500% [-156.071, 231.071] |
| transfer | reused-service minus no-expansion | baseline | 16 | meanReturn | 2 | 7.500 [-31.214, 46.214] |
| transfer | reused-service minus shuffled-communication | baseline | 16 | accuracy | 2 | 36.914% [-159.139, 232.967] |
| transfer | reused-service minus shuffled-communication | baseline | 16 | meanReturn | 2 | 7.336 [-31.875, 46.546] |
| transfer | reused-service minus flat | baseline | 16 | accuracy | 2 | 24.414% [-156.749, 205.577] |
| transfer | reused-service minus flat | baseline | 16 | meanReturn | 2 | 4.883 [-31.350, 41.115] |
| transfer | reused-service minus fixed-sum | baseline | 16 | accuracy | 2 | 12.305% [-144.041, 168.651] |
| transfer | reused-service minus fixed-sum | baseline | 16 | meanReturn | 2 | 2.461 [-28.808, 33.730] |
| transfer | reused-service minus fresh-service | baseline | 32 | accuracy | 2 | 50.195% [37.787, 62.604] |
| transfer | reused-service minus fresh-service | baseline | 32 | meanReturn | 2 | 10.016 [7.236, 12.795] |
| transfer | reused-service minus no-expansion | baseline | 32 | accuracy | 2 | 51.367% [33.995, 68.739] |
| transfer | reused-service minus no-expansion | baseline | 32 | meanReturn | 2 | 10.273 [6.799, 13.748] |
| transfer | reused-service minus shuffled-communication | baseline | 32 | accuracy | 2 | 49.414% [41.969, 56.859] |
| transfer | reused-service minus shuffled-communication | baseline | 32 | meanReturn | 2 | 9.836 [8.347, 11.325] |
| transfer | reused-service minus flat | baseline | 32 | accuracy | 2 | 12.695% [10.214, 15.177] |
| transfer | reused-service minus flat | baseline | 32 | meanReturn | 2 | 2.539 [2.043, 3.035] |
| transfer | reused-service minus fixed-sum | baseline | 32 | accuracy | 2 | 12.695% [10.214, 15.177] |
| transfer | reused-service minus fixed-sum | baseline | 32 | meanReturn | 2 | 2.539 [2.043, 3.035] |
| transfer | reused-service minus fresh-service | baseline | 64 | accuracy | 2 | 50.000% [40.073, 59.927] |
| transfer | reused-service minus fresh-service | baseline | 64 | meanReturn | 2 | 10.000 [8.015, 11.985] |
| transfer | reused-service minus no-expansion | baseline | 64 | accuracy | 2 | 50.195% [47.714, 52.677] |
| transfer | reused-service minus no-expansion | baseline | 64 | meanReturn | 2 | 10.039 [9.543, 10.535] |
| transfer | reused-service minus shuffled-communication | baseline | 64 | accuracy | 2 | 48.633% [36.224, 61.041] |
| transfer | reused-service minus shuffled-communication | baseline | 64 | meanReturn | 2 | 9.680 [7.198, 12.161] |
| transfer | reused-service minus flat | baseline | 64 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 64 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 64 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 64 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 128 | accuracy | 2 | 11.133% [-130.323, 152.589] |
| transfer | reused-service minus fresh-service | baseline | 128 | meanReturn | 2 | 2.227 [-26.065, 30.518] |
| transfer | reused-service minus no-expansion | baseline | 128 | accuracy | 2 | 49.023% [41.578, 56.468] |
| transfer | reused-service minus no-expansion | baseline | 128 | meanReturn | 2 | 9.805 [8.316, 11.294] |
| transfer | reused-service minus shuffled-communication | baseline | 128 | accuracy | 2 | 48.633% [36.224, 61.041] |
| transfer | reused-service minus shuffled-communication | baseline | 128 | meanReturn | 2 | 9.680 [7.198, 12.161] |
| transfer | reused-service minus flat | baseline | 128 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 128 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 128 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 128 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus no-expansion | baseline | 192 | accuracy | 2 | 50.391% [45.427, 55.354] |
| transfer | reused-service minus no-expansion | baseline | 192 | meanReturn | 2 | 10.078 [9.085, 11.071] |
| transfer | reused-service minus shuffled-communication | baseline | 192 | accuracy | 2 | 48.242% [40.797, 55.687] |
| transfer | reused-service minus shuffled-communication | baseline | 192 | meanReturn | 2 | 9.602 [8.113, 11.091] |
| transfer | reused-service minus flat | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 192 | accuracy | 2 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 192 | meanReturn | 2 | 0.000 [0.000, 0.000] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | accuracy | 2 | -2.214% [-28.685, 24.258] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | meanReturn | 2 | 0.329 [-3.823, 4.482] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | queryRate | 2 | -37.826% [-510.172, 434.521] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | accuracy | 2 | 52.669% [-140.075, 245.413] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | meanReturn | 2 | 9.309 [-38.687, 57.304] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | queryRate | 2 | 62.044% [-410.302, 534.391] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | accuracy | 2 | 13.542% [-16.239, 43.322] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | meanReturn | 2 | 1.480 [-2.010, 4.971] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | queryRate | 2 | 62.174% [-410.172, 534.521] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | accuracy | 2 | 11.068% [-13.749, 35.885] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | meanReturn | 2 | 1.249 [-3.202, 5.699] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | queryRate | 2 | 49.023% [-421.669, 519.716] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | accuracy | 2 | 9.440% [-22.822, 41.702] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | meanReturn | 2 | 0.910 [-2.084, 3.905] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | queryRate | 2 | 49.674% [-422.672, 522.021] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | accuracy | 2 | -2.214% [-28.685, 24.258] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | meanReturn | 2 | 1.464 [-16.859, 19.787] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | queryRate | 2 | -37.826% [-510.172, 434.521] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | accuracy | 2 | 52.669% [-140.075, 245.413] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | meanReturn | 2 | 7.447 [-54.719, 69.613] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | queryRate | 2 | 62.044% [-410.302, 534.391] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | accuracy | 2 | 13.542% [-16.239, 43.322] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | meanReturn | 2 | -0.385 [-18.046, 17.277] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | queryRate | 2 | 62.174% [-410.172, 534.521] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | accuracy | 2 | 11.068% [-13.749, 35.885] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | meanReturn | 2 | -0.222 [-18.793, 18.349] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | queryRate | 2 | 49.023% [-421.669, 519.716] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | accuracy | 2 | 9.440% [-22.822, 41.702] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | meanReturn | 2 | -0.580 [-17.745, 16.585] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | queryRate | 2 | 49.674% [-422.672, 522.021] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | accuracy | 2 | -8.398% [-113.456, 96.659] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | meanReturn | 2 | -0.908 [-12.472, 10.657] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | queryRate | 2 | -37.826% [-510.172, 434.521] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | accuracy | 2 | 41.732% [-36.855, 120.318] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | meanReturn | 2 | 7.121 [0.851, 13.391] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | queryRate | 2 | 62.044% [-410.302, 534.391] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | accuracy | 2 | 28.385% [-46.065, 102.836] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | meanReturn | 2 | 4.449 [-0.994, 9.892] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | queryRate | 2 | 62.174% [-410.172, 534.521] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | accuracy | 2 | 16.536% [-86.040, 119.113] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | meanReturn | 2 | 2.576 [-8.377, 13.528] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | queryRate | 2 | 37.370% [-440.767, 515.507] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | accuracy | 2 | 20.378% [-61.518, 102.273] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | meanReturn | 2 | 3.098 [-3.835, 10.030] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | queryRate | 2 | 49.674% [-422.672, 522.021] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | accuracy | 2 | -78.516% [-98.369, -58.662] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | meanReturn | 2 | -4.298 [-5.771, -2.826] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | queryRate | 2 | -99.154% [-104.944, -93.363] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | accuracy | 2 | -12.891% [-194.881, 169.099] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | meanReturn | 2 | 3.283 [-37.268, 43.833] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | queryRate | 2 | 0.716% [-5.074, 6.507] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | accuracy | 2 | -83.203% [-91.475, -74.931] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | meanReturn | 2 | -4.783 [-5.726, -3.840] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | queryRate | 2 | 0.846% [-4.944, 6.637] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | accuracy | 2 | -11.523% [-176.142, 153.095] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | meanReturn | 2 | -0.745 [-11.366, 9.877] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | queryRate | 2 | -0.651% [-23.813, 22.511] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | accuracy | 2 | -67.643% [-81.706, -53.580] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | meanReturn | 2 | -4.046 [-5.121, -2.970] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | queryRate | 2 | -11.654% [-17.444, -5.863] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | accuracy | 2 | -3.385% [-3.385, -3.385] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | meanReturn | 2 | 1.271 [1.271, 1.271] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | queryRate | 2 | -96.615% [-96.615, -96.615] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | accuracy | 2 | 51.497% [-167.718, 270.713] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | meanReturn | 2 | 10.250 [-33.593, 54.093] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | queryRate | 2 | 3.255% [3.255, 3.255] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | accuracy | 2 | 0.130% [0.130, 0.130] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | meanReturn | 2 | -0.026 [-0.026, -0.026] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | queryRate | 2 | 3.385% [3.385, 3.385] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | meanReturn | 2 | -0.010 [-0.010, -0.010] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | queryRate | 2 | 0.000% [0.000, 0.000] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | accuracy | 2 | 8.268% [2.478, 14.059] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | meanReturn | 2 | 1.852 [0.693, 3.010] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | queryRate | 2 | -9.115% [-9.115, -9.115] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | accuracy | 2 | -3.385% [-3.385, -3.385] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | meanReturn | 2 | 4.169 [4.169, 4.169] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | queryRate | 2 | -96.615% [-96.615, -96.615] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | accuracy | 2 | 51.497% [-167.718, 270.713] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | meanReturn | 2 | 10.152 [-33.691, 53.995] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | queryRate | 2 | 3.255% [3.255, 3.255] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | accuracy | 2 | 0.130% [0.130, 0.130] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | meanReturn | 2 | -0.128 [-0.128, -0.128] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | queryRate | 2 | 3.385% [3.385, 3.385] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | meanReturn | 2 | -0.010 [-0.010, -0.010] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | queryRate | 2 | 0.000% [0.000, 0.000] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | accuracy | 2 | 8.268% [2.478, 14.059] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | meanReturn | 2 | 2.125 [0.967, 3.283] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | queryRate | 2 | -9.115% [-9.115, -9.115] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | accuracy | 2 | -10.091% [-24.154, 3.972] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | meanReturn | 2 | -0.203 [-3.313, 2.907] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | queryRate | 2 | -89.974% [-104.864, -75.084] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | accuracy | 2 | 40.039% [27.631, 52.447] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | meanReturn | 2 | 7.826 [5.642, 10.009] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | queryRate | 2 | 9.896% [-4.994, 24.786] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | accuracy | 2 | 1.172% [-2.137, 4.481] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | meanReturn | 2 | 0.049 [-0.315, 0.413] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | queryRate | 2 | 10.026% [-4.864, 24.916] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | accuracy | 2 | -0.130% [-0.130, -0.130] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | meanReturn | 2 | -0.010 [-0.010, -0.010] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | queryRate | 2 | 0.000% [0.000, 0.000] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | accuracy | 2 | 18.685% [9.585, 27.784] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | meanReturn | 2 | 3.802 [2.280, 5.324] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | queryRate | 2 | -2.474% [-17.364, 12.416] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | accuracy | 2 | -72.982% [-151.568, 5.605] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | meanReturn | 2 | -3.882 [-9.920, 2.157] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | queryRate | 2 | -99.414% [-101.896, -96.932] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | accuracy | 2 | -7.357% [-248.080, 233.366] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | meanReturn | 2 | 3.699 [-32.285, 39.684] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | queryRate | 2 | 0.456% [-2.026, 2.937] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | accuracy | 2 | -89.909% [-153.605, -26.212] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | meanReturn | 2 | -6.814 [-11.661, -1.966] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | queryRate | 2 | 0.586% [-1.896, 3.068] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | accuracy | 2 | 5.664% [-69.614, 80.942] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | meanReturn | 2 | 0.422 [-5.236, 6.080] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | queryRate | 2 | 0.391% [-2.918, 3.700] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | accuracy | 2 | -62.109% [-134.905, 10.687] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | meanReturn | 2 | -3.629 [-7.120, -0.138] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | queryRate | 2 | -11.914% [-14.396, -9.432] |

### Native archives

- relation: 242,135 bytes; signature=True, behavior=True; `relation.gojo`.
- consumer: 71,294 bytes; signature=True, behavior=True; `consumer.gojo`.
- pipeline-0: 433,076 bytes; signature=True, behavior=True; `pipeline-0.gojo`.
- refresh-0: 104,543 bytes; signature=True, behavior=True; `refresh-0.gojo`.
- pipeline-4: 433,328 bytes; signature=True, behavior=True; `pipeline-4.gojo`.
- refresh-4: 110,305 bytes; signature=True, behavior=True; `refresh-4.gojo`.

Interval method: two-sided 95% Student t on seed means or paired seed differences; df>30 uses conservative df30 critical; exploratory contrasts without multiplicity correction.
