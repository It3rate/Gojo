# Composite emergence: generated analysis

Seed-paired descriptive intervals. Checkpoints are separate groups; repeated episodes are not independent replicates.

Source/transfer acquisitions are counted without a probe fee. Refresh acquisitions are paid. Recovery is first-success delay, not sustained recovery.

## confirmation-01

Status: complete. Settings: `{"consumerTrain": 192, "discoveryTrain": 768, "pilot": false, "probePrice": 2, "refreshTest": 2048, "refreshTrain": 2048, "seedStart": 200, "seeds": 8, "switchProbability": 0.04, "testEpisodes": 768}`.

Counts: `{"resourceIdentitiesChecked": 848, "runnerEvaluations": 1185792, "runnerTrainingEvents": 100352, "savedTestEvaluations": 1185792, "sourceHashesChecked": 123, "uniqueRows": 904}`.

| Stage | Controller | Condition | Train | Seeds | Accuracy | Coverage | Acquisitions | Return |
|---|---|---|---:|---:|---|---|---|---|
| discovery | frozen | fresh-rule | 768 | 8 | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | learned | fresh-rule | 768 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | no-expansion | fresh-rule | 768 | 8 | 45.752% [40.607, 50.897] | 90.576% [79.708, 101.444] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | no-memory | fresh-rule | 768 | 8 | 42.253% [34.892, 49.613] | 84.489% [68.856, 100.122] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | previous-pair | fresh-rule | 768 | 8 | 48.503% [44.271, 52.735] | 95.361% [87.727, 102.996] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | shuffled-communication | fresh-rule | 768 | 8 | 49.268% [46.390, 52.145] | 98.177% [94.174, 102.180] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| discovery | shuffled-feedback | fresh-rule | 768 | 8 | 46.826% [43.599, 50.054] | 96.501% [91.195, 101.807] | 0.000% [0.000, 0.000] | 0.000 [0.000, 0.000] |
| refresh | always-probe | delayed-history/baseline | 2048 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.000 [6.000, 6.000] |
| refresh | always-probe | delayed-history/expensive-probe | 2048 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 3.000 [3.000, 3.000] |
| refresh | always-probe | delayed-history/fast-switch | 2048 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.000 [6.000, 6.000] |
| refresh | always-probe | delayed-history/missing-signals | 2048 | 8 | 81.024% [80.011, 82.037] | 81.024% [80.011, 82.037] | 100.000% [100.000, 100.000] | 4.482 [4.401, 4.563] |
| refresh | always-probe | immediate/baseline | 2048 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.000 [6.000, 6.000] |
| refresh | always-probe | immediate/expensive-probe | 2048 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 3.000 [3.000, 3.000] |
| refresh | always-probe | immediate/fast-switch | 2048 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.000 [6.000, 6.000] |
| refresh | always-probe | immediate/missing-signals | 2048 | 8 | 81.024% [80.011, 82.037] | 81.024% [80.011, 82.037] | 100.000% [100.000, 100.000] | 4.482 [4.401, 4.563] |
| refresh | failure-probe | delayed-history/baseline | 2048 | 8 | 86.597% [85.940, 87.254] | 100.000% [100.000, 100.000] | 13.446% [12.789, 14.103] | 5.050 [4.906, 5.195] |
| refresh | failure-probe | delayed-history/expensive-probe | 2048 | 8 | 86.597% [85.940, 87.254] | 100.000% [100.000, 100.000] | 13.446% [12.789, 14.103] | 4.647 [4.483, 4.811] |
| refresh | failure-probe | delayed-history/fast-switch | 2048 | 8 | 75.757% [74.852, 76.662] | 100.000% [100.000, 100.000] | 24.249% [23.362, 25.137] | 2.666 [2.468, 2.865] |
| refresh | failure-probe | delayed-history/missing-signals | 2048 | 8 | 6.665% [2.120, 11.210] | 7.654% [2.599, 12.709] | 1.038% [0.504, 1.572] | 0.394 [0.100, 0.687] |
| refresh | failure-probe | immediate/baseline | 2048 | 8 | 96.088% [95.852, 96.323] | 100.000% [100.000, 100.000] | 3.961% [3.726, 4.197] | 7.138 [7.087, 7.190] |
| refresh | failure-probe | immediate/expensive-probe | 2048 | 8 | 96.088% [95.852, 96.323] | 100.000% [100.000, 100.000] | 3.961% [3.726, 4.197] | 7.019 [6.961, 7.078] |
| refresh | failure-probe | immediate/fast-switch | 2048 | 8 | 89.587% [89.221, 89.954] | 100.000% [100.000, 100.000] | 10.455% [10.082, 10.828] | 5.708 [5.628, 5.789] |
| refresh | failure-probe | immediate/missing-signals | 2048 | 8 | 6.335% [-0.006, 12.677] | 6.659% [0.036, 13.282] | 0.372% [0.087, 0.658] | 0.461 [-0.007, 0.929] |
| refresh | fixed-interval | delayed-history/baseline | 2048 | 8 | 88.208% [87.068, 89.348] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.392 [5.164, 5.620] |
| refresh | fixed-interval | delayed-history/expensive-probe | 2048 | 8 | 88.208% [87.068, 89.348] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.017 [4.789, 5.245] |
| refresh | fixed-interval | delayed-history/fast-switch | 2048 | 8 | 73.682% [71.981, 75.382] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 2.486 [2.146, 2.826] |
| refresh | fixed-interval | delayed-history/missing-signals | 2048 | 8 | 72.034% [70.028, 74.039] | 81.641% [79.777, 83.504] | 12.500% [12.500, 12.500] | 4.360 [4.119, 4.601] |
| refresh | fixed-interval | immediate/baseline | 2048 | 8 | 88.208% [87.068, 89.348] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.392 [5.164, 5.620] |
| refresh | fixed-interval | immediate/expensive-probe | 2048 | 8 | 88.208% [87.068, 89.348] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 5.017 [4.789, 5.245] |
| refresh | fixed-interval | immediate/fast-switch | 2048 | 8 | 73.682% [71.981, 75.382] | 100.000% [100.000, 100.000] | 12.500% [12.500, 12.500] | 2.486 [2.146, 2.826] |
| refresh | fixed-interval | immediate/missing-signals | 2048 | 8 | 72.034% [70.028, 74.039] | 81.641% [79.777, 83.504] | 12.500% [12.500, 12.500] | 4.360 [4.119, 4.601] |
| refresh | learned-refresh | delayed-history/baseline | 2048 | 8 | 97.791% [95.317, 100.264] | 99.951% [99.951, 99.951] | 72.028% [39.978, 104.078] | 6.123 [5.975, 6.271] |
| refresh | learned-refresh | delayed-history/expensive-probe | 2048 | 8 | 97.791% [95.317, 100.264] | 99.951% [99.951, 99.951] | 72.028% [39.978, 104.078] | 3.963 [2.854, 5.071] |
| refresh | learned-refresh | delayed-history/fast-switch | 2048 | 8 | 94.562% [88.352, 100.772] | 99.951% [99.951, 99.951] | 72.345% [40.635, 104.055] | 5.471 [4.862, 6.080] |
| refresh | learned-refresh | delayed-history/missing-signals | 2048 | 8 | 0.458% [0.269, 0.647] | 0.519% [0.324, 0.714] | 0.348% [0.178, 0.518] | 0.022 [0.005, 0.040] |
| refresh | learned-refresh | immediate/baseline | 2048 | 8 | 96.265% [95.885, 96.644] | 99.951% [99.951, 99.951] | 10.699% [2.359, 19.040] | 7.045 [6.926, 7.164] |
| refresh | learned-refresh | immediate/expensive-probe | 2048 | 8 | 96.265% [95.885, 96.644] | 99.951% [99.951, 99.951] | 10.699% [2.359, 19.040] | 6.724 [6.362, 7.086] |
| refresh | learned-refresh | immediate/fast-switch | 2048 | 8 | 90.063% [89.380, 90.747] | 99.951% [99.951, 99.951] | 15.533% [8.979, 22.088] | 5.708 [5.634, 5.782] |
| refresh | learned-refresh | immediate/missing-signals | 2048 | 8 | 4.962% [0.849, 9.075] | 5.206% [0.935, 9.478] | 0.397% [0.250, 0.544] | 0.360 [0.052, 0.667] |
| refresh | never-probe | delayed-history/baseline | 2048 | 8 | 45.471% [43.951, 46.991] | 100.000% [100.000, 100.000] | 0.049% [0.049, 0.049] | -2.907 [-3.211, -2.603] |
| refresh | never-probe | delayed-history/expensive-probe | 2048 | 8 | 45.471% [43.951, 46.991] | 100.000% [100.000, 100.000] | 0.049% [0.049, 0.049] | -2.908 [-3.212, -2.604] |
| refresh | never-probe | delayed-history/fast-switch | 2048 | 8 | 48.047% [46.021, 50.073] | 100.000% [100.000, 100.000] | 0.049% [0.049, 0.049] | -2.392 [-2.797, -1.986] |
| refresh | never-probe | delayed-history/missing-signals | 2048 | 8 | 39.545% [26.116, 52.973] | 87.500% [57.942, 117.058] | 0.049% [0.049, 0.049] | -2.592 [-3.509, -1.675] |
| refresh | never-probe | immediate/baseline | 2048 | 8 | 45.471% [43.951, 46.991] | 100.000% [100.000, 100.000] | 0.049% [0.049, 0.049] | -2.907 [-3.211, -2.603] |
| refresh | never-probe | immediate/expensive-probe | 2048 | 8 | 45.471% [43.951, 46.991] | 100.000% [100.000, 100.000] | 0.049% [0.049, 0.049] | -2.908 [-3.212, -2.604] |
| refresh | never-probe | immediate/fast-switch | 2048 | 8 | 48.047% [46.021, 50.073] | 100.000% [100.000, 100.000] | 0.049% [0.049, 0.049] | -2.392 [-2.797, -1.986] |
| refresh | never-probe | immediate/missing-signals | 2048 | 8 | 39.545% [26.116, 52.973] | 87.500% [57.942, 117.058] | 0.049% [0.049, 0.049] | -2.592 [-3.509, -1.675] |
| refresh | outcome-memory | delayed-history/baseline | 2048 | 8 | 83.051% [82.317, 83.784] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 4.610 [4.463, 4.757] |
| refresh | outcome-memory | delayed-history/expensive-probe | 2048 | 8 | 83.051% [82.317, 83.784] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 4.610 [4.463, 4.757] |
| refresh | outcome-memory | delayed-history/fast-switch | 2048 | 8 | 64.355% [63.207, 65.503] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 0.871 [0.641, 1.101] |
| refresh | outcome-memory | delayed-history/missing-signals | 2048 | 8 | 83.051% [82.317, 83.784] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 4.610 [4.463, 4.757] |
| refresh | outcome-memory | immediate/baseline | 2048 | 8 | 95.868% [95.589, 96.146] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.174 [7.118, 7.229] |
| refresh | outcome-memory | immediate/expensive-probe | 2048 | 8 | 95.868% [95.589, 96.146] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.174 [7.118, 7.229] |
| refresh | outcome-memory | immediate/fast-switch | 2048 | 8 | 88.257% [87.839, 88.675] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 5.651 [5.568, 5.735] |
| refresh | outcome-memory | immediate/missing-signals | 2048 | 8 | 95.868% [95.589, 96.146] | 100.000% [100.000, 100.000] | 0.000% [0.000, 0.000] | 7.174 [7.118, 7.229] |
| source | fixed-sum | baseline | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | flat | baseline | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | frozen | baseline | 192 | 8 | 49.870% [48.549, 51.190] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.026 [-2.290, -1.762] |
| source | learned | absent-state | 192 | 8 | 0.000% [0.000, 0.000] | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 0.000 [0.000, 0.000] |
| source | learned | baseline | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | learned | inverted-input | 192 | 8 | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -12.000 [-12.000, -12.000] |
| source | learned | long-delay | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | learned | missing-signals | 192 | 8 | 80.648% [79.233, 82.062] | 80.648% [79.233, 82.062] | 100.000% [100.000, 100.000] | 6.452 [6.339, 6.565] |
| source | learned | opposite-state | 192 | 8 | 0.000% [0.000, 0.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -12.000 [-12.000, -12.000] |
| source | learned | same-state | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| source | neutral-left | baseline | 0 | 8 | 49.202% [47.612, 50.793] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.160 [-2.478, -1.841] |
| source | no-expansion | baseline | 192 | 8 | 50.391% [48.478, 52.304] | 99.935% [99.877, 99.993] | 100.000% [100.000, 100.000] | -1.914 [-2.294, -1.534] |
| source | no-memory | baseline | 192 | 8 | 50.228% [48.182, 52.274] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.954 [-2.364, -1.545] |
| source | previous-pair | baseline | 192 | 8 | 50.309% [48.892, 51.726] | 99.902% [99.852, 99.953] | 100.000% [100.000, 100.000] | -1.926 [-2.208, -1.645] |
| source | shuffled-communication | baseline | 192 | 8 | 49.382% [47.742, 51.021] | 99.886% [99.848, 99.925] | 100.000% [100.000, 100.000] | -2.110 [-2.438, -1.782] |
| source | shuffled-feedback | baseline | 192 | 8 | 49.984% [48.048, 51.919] | 99.902% [99.852, 99.953] | 100.000% [100.000, 100.000] | -1.992 [-2.382, -1.601] |
| transfer | fixed-sum | baseline | 0 | 8 | 50.798% [49.207, 52.388] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.840 [-2.159, -1.522] |
| transfer | fixed-sum | baseline | 8 | 8 | 55.355% [48.117, 62.593] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -0.929 [-2.377, 0.519] |
| transfer | fixed-sum | baseline | 16 | 8 | 74.870% [61.473, 88.267] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 2.974 [0.295, 5.653] |
| transfer | fixed-sum | baseline | 32 | 8 | 94.336% [89.236, 99.436] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.867 [5.847, 7.887] |
| transfer | fixed-sum | baseline | 64 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fixed-sum | baseline | 128 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fixed-sum | baseline | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | flat | baseline | 0 | 8 | 50.798% [49.207, 52.388] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.840 [-2.159, -1.522] |
| transfer | flat | baseline | 8 | 8 | 52.262% [47.967, 56.558] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.548 [-2.407, -0.688] |
| transfer | flat | baseline | 16 | 8 | 62.419% [52.793, 72.044] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 0.484 [-1.441, 2.409] |
| transfer | flat | baseline | 32 | 8 | 86.605% [78.353, 94.857] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.321 [3.671, 6.971] |
| transfer | flat | baseline | 64 | 8 | 98.324% [94.359, 102.288] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 7.665 [6.872, 8.458] |
| transfer | flat | baseline | 128 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | flat | baseline | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | fresh-service | baseline | 0 | 8 | 50.798% [49.207, 52.388] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.840 [-2.159, -1.522] |
| transfer | fresh-service | baseline | 8 | 8 | 50.065% [48.311, 51.819] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.987 [-2.338, -1.636] |
| transfer | fresh-service | baseline | 16 | 8 | 49.577% [48.951, 50.203] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -2.085 [-2.210, -1.959] |
| transfer | fresh-service | baseline | 32 | 8 | 48.877% [47.218, 50.536] | 99.919% [99.862, 99.975] | 100.000% [100.000, 100.000] | -2.215 [-2.548, -1.882] |
| transfer | fresh-service | baseline | 64 | 8 | 49.691% [48.390, 50.992] | 99.951% [99.895, 100.008] | 100.000% [100.000, 100.000] | -2.056 [-2.316, -1.796] |
| transfer | fresh-service | baseline | 128 | 8 | 87.321% [75.962, 98.680] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 5.464 [3.192, 7.736] |
| transfer | fresh-service | baseline | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | no-expansion | baseline | 0 | 8 | 50.765% [49.159, 52.371] | 99.935% [99.877, 99.993] | 100.000% [100.000, 100.000] | -1.839 [-2.160, -1.518] |
| transfer | no-expansion | baseline | 8 | 8 | 50.830% [49.208, 52.452] | 99.935% [99.877, 99.993] | 100.000% [100.000, 100.000] | -1.826 [-2.151, -1.501] |
| transfer | no-expansion | baseline | 16 | 8 | 50.195% [48.664, 51.727] | 99.935% [99.877, 99.993] | 100.000% [100.000, 100.000] | -1.953 [-2.256, -1.650] |
| transfer | no-expansion | baseline | 32 | 8 | 50.146% [48.572, 51.721] | 99.935% [99.877, 99.993] | 100.000% [100.000, 100.000] | -1.963 [-2.275, -1.651] |
| transfer | no-expansion | baseline | 64 | 8 | 49.089% [47.515, 50.662] | 99.935% [99.877, 99.993] | 100.000% [100.000, 100.000] | -2.174 [-2.486, -1.863] |
| transfer | no-expansion | baseline | 128 | 8 | 50.049% [48.594, 51.503] | 99.935% [99.877, 99.993] | 100.000% [100.000, 100.000] | -1.982 [-2.274, -1.691] |
| transfer | no-expansion | baseline | 192 | 8 | 50.146% [48.541, 51.752] | 99.935% [99.877, 99.993] | 100.000% [100.000, 100.000] | -1.963 [-2.286, -1.640] |
| transfer | reused-service | baseline | 0 | 8 | 50.798% [49.207, 52.388] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -1.840 [-2.159, -1.522] |
| transfer | reused-service | baseline | 8 | 8 | 59.570% [49.364, 69.776] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | -0.086 [-2.127, 1.955] |
| transfer | reused-service | baseline | 16 | 8 | 91.097% [80.819, 101.375] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 6.219 [4.164, 8.275] |
| transfer | reused-service | baseline | 32 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 64 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 128 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | reused-service | baseline | 192 | 8 | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 100.000% [100.000, 100.000] | 8.000 [8.000, 8.000] |
| transfer | shuffled-communication | baseline | 0 | 8 | 50.749% [49.164, 52.333] | 99.886% [99.848, 99.925] | 100.000% [100.000, 100.000] | -1.837 [-2.156, -1.517] |
| transfer | shuffled-communication | baseline | 8 | 8 | 50.146% [48.450, 51.843] | 99.886% [99.848, 99.925] | 100.000% [100.000, 100.000] | -1.957 [-2.298, -1.616] |
| transfer | shuffled-communication | baseline | 16 | 8 | 49.951% [48.639, 51.264] | 99.886% [99.848, 99.925] | 100.000% [100.000, 100.000] | -1.996 [-2.259, -1.733] |
| transfer | shuffled-communication | baseline | 32 | 8 | 49.170% [47.428, 50.912] | 99.886% [99.848, 99.925] | 100.000% [100.000, 100.000] | -2.152 [-2.501, -1.804] |
| transfer | shuffled-communication | baseline | 64 | 8 | 49.268% [47.156, 51.379] | 99.886% [99.848, 99.925] | 100.000% [100.000, 100.000] | -2.133 [-2.555, -1.710] |
| transfer | shuffled-communication | baseline | 128 | 8 | 49.723% [47.971, 51.476] | 99.886% [99.848, 99.925] | 100.000% [100.000, 100.000] | -2.042 [-2.392, -1.691] |
| transfer | shuffled-communication | baseline | 192 | 8 | 49.642% [47.874, 51.409] | 99.886% [99.848, 99.925] | 100.000% [100.000, 100.000] | -2.058 [-2.412, -1.704] |

### Paired effects

| Stage | Left minus right | Condition | Train | Metric | Seeds | Difference |
|---|---|---|---:|---|---:|---|
| discovery | learned minus no-expansion | fresh-rule | 768 | accuracy | 8 | 54.248% [49.103, 59.393] |
| discovery | learned minus no-expansion | fresh-rule | 768 | coverage | 8 | 9.424% [-1.444, 20.292] |
| discovery | learned minus frozen | fresh-rule | 768 | accuracy | 8 | 100.000% [100.000, 100.000] |
| discovery | learned minus frozen | fresh-rule | 768 | coverage | 8 | 100.000% [100.000, 100.000] |
| discovery | learned minus shuffled-feedback | fresh-rule | 768 | accuracy | 8 | 53.174% [49.946, 56.401] |
| discovery | learned minus shuffled-feedback | fresh-rule | 768 | coverage | 8 | 3.499% [-1.807, 8.805] |
| discovery | learned minus shuffled-communication | fresh-rule | 768 | accuracy | 8 | 50.732% [47.855, 53.610] |
| discovery | learned minus shuffled-communication | fresh-rule | 768 | coverage | 8 | 1.823% [-2.180, 5.826] |
| discovery | learned minus no-memory | fresh-rule | 768 | accuracy | 8 | 57.747% [50.387, 65.108] |
| discovery | learned minus no-memory | fresh-rule | 768 | coverage | 8 | 15.511% [-0.122, 31.144] |
| discovery | learned minus previous-pair | fresh-rule | 768 | accuracy | 8 | 51.497% [47.265, 55.729] |
| discovery | learned minus previous-pair | fresh-rule | 768 | coverage | 8 | 4.639% [-2.996, 12.273] |
| source | learned minus no-expansion | baseline | 192 | accuracy | 8 | 49.609% [47.696, 51.522] |
| source | learned minus no-expansion | baseline | 192 | meanReturn | 8 | 9.914 [9.534, 10.294] |
| source | learned minus frozen | baseline | 192 | accuracy | 8 | 50.130% [48.810, 51.451] |
| source | learned minus frozen | baseline | 192 | meanReturn | 8 | 10.026 [9.762, 10.290] |
| source | learned minus shuffled-feedback | baseline | 192 | accuracy | 8 | 50.016% [48.081, 51.952] |
| source | learned minus shuffled-feedback | baseline | 192 | meanReturn | 8 | 9.992 [9.601, 10.382] |
| source | learned minus shuffled-communication | baseline | 192 | accuracy | 8 | 50.618% [48.979, 52.258] |
| source | learned minus shuffled-communication | baseline | 192 | meanReturn | 8 | 10.110 [9.782, 10.438] |
| source | learned minus no-memory | baseline | 192 | accuracy | 8 | 49.772% [47.726, 51.818] |
| source | learned minus no-memory | baseline | 192 | meanReturn | 8 | 9.954 [9.545, 10.364] |
| source | learned minus previous-pair | baseline | 192 | accuracy | 8 | 49.691% [48.274, 51.108] |
| source | learned minus previous-pair | baseline | 192 | meanReturn | 8 | 9.926 [9.645, 10.208] |
| source | learned minus flat | baseline | 192 | accuracy | 8 | 0.000% [0.000, 0.000] |
| source | learned minus flat | baseline | 192 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| source | learned minus fixed-sum | baseline | 192 | accuracy | 8 | 0.000% [0.000, 0.000] |
| source | learned minus fixed-sum | baseline | 192 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 0 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 0 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus no-expansion | baseline | 0 | accuracy | 8 | 0.033% [-0.018, 0.083] |
| transfer | reused-service minus no-expansion | baseline | 0 | meanReturn | 8 | -0.001 [-0.010, 0.007] |
| transfer | reused-service minus shuffled-communication | baseline | 0 | accuracy | 8 | 0.049% [-0.008, 0.105] |
| transfer | reused-service minus shuffled-communication | baseline | 0 | meanReturn | 8 | -0.004 [-0.015, 0.007] |
| transfer | reused-service minus flat | baseline | 0 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 0 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 0 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 0 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 8 | accuracy | 8 | 9.505% [-0.651, 19.662] |
| transfer | reused-service minus fresh-service | baseline | 8 | meanReturn | 8 | 1.901 [-0.130, 3.932] |
| transfer | reused-service minus no-expansion | baseline | 8 | accuracy | 8 | 8.740% [-1.468, 18.949] |
| transfer | reused-service minus no-expansion | baseline | 8 | meanReturn | 8 | 1.740 [-0.300, 3.780] |
| transfer | reused-service minus shuffled-communication | baseline | 8 | accuracy | 8 | 9.424% [-1.429, 20.276] |
| transfer | reused-service minus shuffled-communication | baseline | 8 | meanReturn | 8 | 1.871 [-0.298, 4.040] |
| transfer | reused-service minus flat | baseline | 8 | accuracy | 8 | 7.308% [-1.947, 16.563] |
| transfer | reused-service minus flat | baseline | 8 | meanReturn | 8 | 1.462 [-0.389, 3.313] |
| transfer | reused-service minus fixed-sum | baseline | 8 | accuracy | 8 | 4.215% [-3.065, 11.496] |
| transfer | reused-service minus fixed-sum | baseline | 8 | meanReturn | 8 | 0.843 [-0.613, 2.299] |
| transfer | reused-service minus fresh-service | baseline | 16 | accuracy | 8 | 41.520% [31.312, 51.728] |
| transfer | reused-service minus fresh-service | baseline | 16 | meanReturn | 8 | 8.304 [6.262, 10.346] |
| transfer | reused-service minus no-expansion | baseline | 16 | accuracy | 8 | 40.902% [30.223, 51.581] |
| transfer | reused-service minus no-expansion | baseline | 16 | meanReturn | 8 | 8.173 [6.042, 10.303] |
| transfer | reused-service minus shuffled-communication | baseline | 16 | accuracy | 8 | 41.146% [30.074, 52.217] |
| transfer | reused-service minus shuffled-communication | baseline | 16 | meanReturn | 8 | 8.215 [6.000, 10.431] |
| transfer | reused-service minus flat | baseline | 16 | accuracy | 8 | 28.678% [18.977, 38.380] |
| transfer | reused-service minus flat | baseline | 16 | meanReturn | 8 | 5.736 [3.795, 7.676] |
| transfer | reused-service minus fixed-sum | baseline | 16 | accuracy | 8 | 16.227% [8.367, 24.088] |
| transfer | reused-service minus fixed-sum | baseline | 16 | meanReturn | 8 | 3.245 [1.673, 4.818] |
| transfer | reused-service minus fresh-service | baseline | 32 | accuracy | 8 | 51.123% [49.464, 52.782] |
| transfer | reused-service minus fresh-service | baseline | 32 | meanReturn | 8 | 10.215 [9.882, 10.548] |
| transfer | reused-service minus no-expansion | baseline | 32 | accuracy | 8 | 49.854% [48.279, 51.428] |
| transfer | reused-service minus no-expansion | baseline | 32 | meanReturn | 8 | 9.963 [9.651, 10.275] |
| transfer | reused-service minus shuffled-communication | baseline | 32 | accuracy | 8 | 50.830% [49.088, 52.572] |
| transfer | reused-service minus shuffled-communication | baseline | 32 | meanReturn | 8 | 10.152 [9.804, 10.501] |
| transfer | reused-service minus flat | baseline | 32 | accuracy | 8 | 13.395% [5.143, 21.647] |
| transfer | reused-service minus flat | baseline | 32 | meanReturn | 8 | 2.679 [1.029, 4.329] |
| transfer | reused-service minus fixed-sum | baseline | 32 | accuracy | 8 | 5.664% [0.564, 10.764] |
| transfer | reused-service minus fixed-sum | baseline | 32 | meanReturn | 8 | 1.133 [0.113, 2.153] |
| transfer | reused-service minus fresh-service | baseline | 64 | accuracy | 8 | 50.309% [49.008, 51.610] |
| transfer | reused-service minus fresh-service | baseline | 64 | meanReturn | 8 | 10.056 [9.796, 10.316] |
| transfer | reused-service minus no-expansion | baseline | 64 | accuracy | 8 | 50.911% [49.338, 52.485] |
| transfer | reused-service minus no-expansion | baseline | 64 | meanReturn | 8 | 10.174 [9.863, 10.486] |
| transfer | reused-service minus shuffled-communication | baseline | 64 | accuracy | 8 | 50.732% [48.621, 52.844] |
| transfer | reused-service minus shuffled-communication | baseline | 64 | meanReturn | 8 | 10.133 [9.710, 10.555] |
| transfer | reused-service minus flat | baseline | 64 | accuracy | 8 | 1.676% [-2.288, 5.641] |
| transfer | reused-service minus flat | baseline | 64 | meanReturn | 8 | 0.335 [-0.458, 1.128] |
| transfer | reused-service minus fixed-sum | baseline | 64 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 64 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 128 | accuracy | 8 | 12.679% [1.320, 24.038] |
| transfer | reused-service minus fresh-service | baseline | 128 | meanReturn | 8 | 2.536 [0.264, 4.808] |
| transfer | reused-service minus no-expansion | baseline | 128 | accuracy | 8 | 49.951% [48.497, 51.406] |
| transfer | reused-service minus no-expansion | baseline | 128 | meanReturn | 8 | 9.982 [9.691, 10.274] |
| transfer | reused-service minus shuffled-communication | baseline | 128 | accuracy | 8 | 50.277% [48.524, 52.029] |
| transfer | reused-service minus shuffled-communication | baseline | 128 | meanReturn | 8 | 10.042 [9.691, 10.392] |
| transfer | reused-service minus flat | baseline | 128 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 128 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 128 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 128 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 192 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fresh-service | baseline | 192 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus no-expansion | baseline | 192 | accuracy | 8 | 49.854% [48.248, 51.459] |
| transfer | reused-service minus no-expansion | baseline | 192 | meanReturn | 8 | 9.963 [9.640, 10.286] |
| transfer | reused-service minus shuffled-communication | baseline | 192 | accuracy | 8 | 50.358% [48.591, 52.126] |
| transfer | reused-service minus shuffled-communication | baseline | 192 | meanReturn | 8 | 10.058 [9.704, 10.412] |
| transfer | reused-service minus flat | baseline | 192 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus flat | baseline | 192 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 192 | accuracy | 8 | 0.000% [0.000, 0.000] |
| transfer | reused-service minus fixed-sum | baseline | 192 | meanReturn | 8 | 0.000 [0.000, 0.000] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | accuracy | 8 | -2.209% [-4.683, 0.264] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | meanReturn | 8 | 0.123 [-0.025, 0.271] |
| refresh | learned-refresh minus always-probe | delayed-history/baseline | 2048 | queryRate | 8 | -27.972% [-60.022, 4.078] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | accuracy | 8 | 52.319% [49.323, 55.316] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | meanReturn | 8 | 9.030 [8.689, 9.371] |
| refresh | learned-refresh minus never-probe | delayed-history/baseline | 2048 | queryRate | 8 | 71.979% [39.929, 104.029] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | accuracy | 8 | 14.740% [12.293, 17.186] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | meanReturn | 8 | 1.513 [1.283, 1.744] |
| refresh | learned-refresh minus outcome-memory | delayed-history/baseline | 2048 | queryRate | 8 | 72.028% [39.978, 104.078] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | accuracy | 8 | 11.194% [8.560, 13.828] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | meanReturn | 8 | 1.073 [0.881, 1.265] |
| refresh | learned-refresh minus failure-probe | delayed-history/baseline | 2048 | queryRate | 8 | 58.582% [26.602, 90.561] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | accuracy | 8 | 9.583% [6.962, 12.203] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | meanReturn | 8 | 0.732 [0.437, 1.026] |
| refresh | learned-refresh minus fixed-interval | delayed-history/baseline | 2048 | queryRate | 8 | 59.528% [27.478, 91.578] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | accuracy | 8 | -2.209% [-4.683, 0.264] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | meanReturn | 8 | 0.963 [-0.146, 2.071] |
| refresh | learned-refresh minus always-probe | delayed-history/expensive-probe | 2048 | queryRate | 8 | -27.972% [-60.022, 4.078] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | accuracy | 8 | 52.319% [49.323, 55.316] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | meanReturn | 8 | 6.871 [5.734, 8.008] |
| refresh | learned-refresh minus never-probe | delayed-history/expensive-probe | 2048 | queryRate | 8 | 71.979% [39.929, 104.029] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | accuracy | 8 | 14.740% [12.293, 17.186] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | meanReturn | 8 | -0.648 [-1.794, 0.499] |
| refresh | learned-refresh minus outcome-memory | delayed-history/expensive-probe | 2048 | queryRate | 8 | 72.028% [39.978, 104.078] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | accuracy | 8 | 11.194% [8.560, 13.828] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | meanReturn | 8 | -0.684 [-1.785, 0.416] |
| refresh | learned-refresh minus failure-probe | delayed-history/expensive-probe | 2048 | queryRate | 8 | 58.582% [26.602, 90.561] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | accuracy | 8 | 9.583% [6.962, 12.203] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | meanReturn | 8 | -1.054 [-2.214, 0.106] |
| refresh | learned-refresh minus fixed-interval | delayed-history/expensive-probe | 2048 | queryRate | 8 | 59.528% [27.478, 91.578] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | accuracy | 8 | -5.438% [-11.648, 0.772] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | meanReturn | 8 | -0.529 [-1.138, 0.080] |
| refresh | learned-refresh minus always-probe | delayed-history/fast-switch | 2048 | queryRate | 8 | -27.655% [-59.365, 4.055] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | accuracy | 8 | 46.515% [39.438, 53.592] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | meanReturn | 8 | 7.863 [7.047, 8.679] |
| refresh | learned-refresh minus never-probe | delayed-history/fast-switch | 2048 | queryRate | 8 | 72.296% [40.586, 104.006] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | accuracy | 8 | 30.206% [24.278, 36.134] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | meanReturn | 8 | 4.600 [4.027, 5.174] |
| refresh | learned-refresh minus outcome-memory | delayed-history/fast-switch | 2048 | queryRate | 8 | 72.345% [40.635, 104.055] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | accuracy | 8 | 18.805% [12.771, 24.839] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | meanReturn | 8 | 2.805 [2.224, 3.386] |
| refresh | learned-refresh minus failure-probe | delayed-history/fast-switch | 2048 | queryRate | 8 | 48.096% [16.132, 80.059] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | accuracy | 8 | 20.880% [14.945, 26.815] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | meanReturn | 8 | 2.985 [2.383, 3.587] |
| refresh | learned-refresh minus fixed-interval | delayed-history/fast-switch | 2048 | queryRate | 8 | 59.845% [28.135, 91.555] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | accuracy | 8 | -80.566% [-81.500, -79.633] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | meanReturn | 8 | -4.460 [-4.537, -4.382] |
| refresh | learned-refresh minus always-probe | delayed-history/missing-signals | 2048 | queryRate | 8 | -99.652% [-99.822, -99.482] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | accuracy | 8 | -39.087% [-52.471, -25.702] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | meanReturn | 8 | 2.614 [1.689, 3.539] |
| refresh | learned-refresh minus never-probe | delayed-history/missing-signals | 2048 | queryRate | 8 | 0.299% [0.129, 0.469] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | accuracy | 8 | -82.593% [-83.345, -81.841] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | meanReturn | 8 | -4.588 [-4.740, -4.435] |
| refresh | learned-refresh minus outcome-memory | delayed-history/missing-signals | 2048 | queryRate | 8 | 0.348% [0.178, 0.518] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | accuracy | 8 | -6.207% [-10.720, -1.695] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | meanReturn | 8 | -0.371 [-0.661, -0.081] |
| refresh | learned-refresh minus failure-probe | delayed-history/missing-signals | 2048 | queryRate | 8 | -0.690% [-1.275, -0.105] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | accuracy | 8 | -71.576% [-73.677, -69.474] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | meanReturn | 8 | -4.338 [-4.585, -4.090] |
| refresh | learned-refresh minus fixed-interval | delayed-history/missing-signals | 2048 | queryRate | 8 | -12.152% [-12.322, -11.982] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | accuracy | 8 | -3.735% [-4.115, -3.356] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | meanReturn | 8 | 1.045 [0.926, 1.164] |
| refresh | learned-refresh minus always-probe | immediate/baseline | 2048 | queryRate | 8 | -89.301% [-97.641, -80.960] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | accuracy | 8 | 50.793% [49.169, 52.418] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | meanReturn | 8 | 9.952 [9.680, 10.223] |
| refresh | learned-refresh minus never-probe | immediate/baseline | 2048 | queryRate | 8 | 10.651% [2.310, 18.991] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | accuracy | 8 | 0.397% [0.094, 0.699] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | meanReturn | 8 | -0.129 [-0.238, -0.019] |
| refresh | learned-refresh minus outcome-memory | immediate/baseline | 2048 | queryRate | 8 | 10.699% [2.359, 19.040] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | accuracy | 8 | 0.177% [-0.102, 0.456] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | meanReturn | 8 | -0.094 [-0.205, 0.018] |
| refresh | learned-refresh minus failure-probe | immediate/baseline | 2048 | queryRate | 8 | 6.738% [-1.617, 15.094] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | accuracy | 8 | 8.057% [6.962, 9.151] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | meanReturn | 8 | 1.653 [1.477, 1.829] |
| refresh | learned-refresh minus fixed-interval | immediate/baseline | 2048 | queryRate | 8 | -1.801% [-10.141, 6.540] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | accuracy | 8 | -3.735% [-4.115, -3.356] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | meanReturn | 8 | 3.724 [3.362, 4.086] |
| refresh | learned-refresh minus always-probe | immediate/expensive-probe | 2048 | queryRate | 8 | -89.301% [-97.641, -80.960] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | accuracy | 8 | 50.793% [49.169, 52.418] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | meanReturn | 8 | 9.632 [9.270, 9.994] |
| refresh | learned-refresh minus never-probe | immediate/expensive-probe | 2048 | queryRate | 8 | 10.651% [2.310, 18.991] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | accuracy | 8 | 0.397% [0.094, 0.699] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | meanReturn | 8 | -0.450 [-0.809, -0.091] |
| refresh | learned-refresh minus outcome-memory | immediate/expensive-probe | 2048 | queryRate | 8 | 10.699% [2.359, 19.040] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | accuracy | 8 | 0.177% [-0.102, 0.456] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | meanReturn | 8 | -0.296 [-0.658, 0.066] |
| refresh | learned-refresh minus failure-probe | immediate/expensive-probe | 2048 | queryRate | 8 | 6.738% [-1.617, 15.094] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | accuracy | 8 | 8.057% [6.962, 9.151] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | meanReturn | 8 | 1.707 [1.375, 2.039] |
| refresh | learned-refresh minus fixed-interval | immediate/expensive-probe | 2048 | queryRate | 8 | -1.801% [-10.141, 6.540] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | accuracy | 8 | -9.937% [-10.620, -9.253] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | meanReturn | 8 | -0.292 [-0.366, -0.218] |
| refresh | learned-refresh minus always-probe | immediate/fast-switch | 2048 | queryRate | 8 | -84.467% [-91.021, -77.912] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | accuracy | 8 | 42.017% [39.808, 44.225] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | meanReturn | 8 | 8.099 [7.650, 8.549] |
| refresh | learned-refresh minus never-probe | immediate/fast-switch | 2048 | queryRate | 8 | 15.485% [8.930, 22.039] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | accuracy | 8 | 1.807% [1.063, 2.550] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | meanReturn | 8 | 0.057 [0.006, 0.107] |
| refresh | learned-refresh minus outcome-memory | immediate/fast-switch | 2048 | queryRate | 8 | 15.533% [8.979, 22.088] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | accuracy | 8 | 0.476% [-0.216, 1.168] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | meanReturn | 8 | -0.000 [-0.015, 0.014] |
| refresh | learned-refresh minus failure-probe | immediate/fast-switch | 2048 | queryRate | 8 | 5.078% [-1.372, 11.529] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | accuracy | 8 | 16.382% [14.348, 18.416] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | meanReturn | 8 | 3.222 [2.921, 3.523] |
| refresh | learned-refresh minus fixed-interval | immediate/fast-switch | 2048 | queryRate | 8 | 3.033% [-3.521, 9.588] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | accuracy | 8 | -76.062% [-79.926, -72.198] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | meanReturn | 8 | -4.122 [-4.410, -3.835] |
| refresh | learned-refresh minus always-probe | immediate/missing-signals | 2048 | queryRate | 8 | -99.603% [-99.750, -99.456] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | accuracy | 8 | -34.583% [-50.929, -18.236] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | meanReturn | 8 | 2.952 [2.244, 3.659] |
| refresh | learned-refresh minus never-probe | immediate/missing-signals | 2048 | queryRate | 8 | 0.348% [0.201, 0.495] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | accuracy | 8 | -90.906% [-95.023, -86.789] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | meanReturn | 8 | -6.814 [-7.125, -6.503] |
| refresh | learned-refresh minus outcome-memory | immediate/missing-signals | 2048 | queryRate | 8 | 0.397% [0.250, 0.544] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | accuracy | 8 | -1.373% [-9.336, 6.590] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | meanReturn | 8 | -0.101 [-0.687, 0.485] |
| refresh | learned-refresh minus failure-probe | immediate/missing-signals | 2048 | queryRate | 8 | 0.024% [-0.296, 0.344] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | accuracy | 8 | -67.072% [-71.900, -62.243] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | meanReturn | 8 | -4.000 [-4.365, -3.635] |
| refresh | learned-refresh minus fixed-interval | immediate/missing-signals | 2048 | queryRate | 8 | -12.103% [-12.250, -11.956] |

### Conditional first success after switches

Means give each eligible seed equal weight. A seed with no completed interval is omitted from delay, not treated as instant recovery. Interrupted or unresolved intervals remain in the count columns. No sustained recovery is measured.

| Controller | Condition | Delay-eligible seeds | Mean first-success delay | Mean completed intervals | Mean unresolved/interrupted intervals | Omitted seeds |
|---|---|---:|---|---|---|---|
| always-probe | delayed-history/baseline | 8 | 0.000 [0.000, 0.000] | 84.125 [78.536, 89.714] | 0.000 [0.000, 0.000] | [] |
| always-probe | delayed-history/expensive-probe | 8 | 0.000 [0.000, 0.000] | 84.125 [78.536, 89.714] | 0.000 [0.000, 0.000] | [] |
| always-probe | delayed-history/fast-switch | 8 | 0.000 [0.000, 0.000] | 240.000 [231.784, 248.216] | 0.000 [0.000, 0.000] | [] |
| always-probe | delayed-history/missing-signals | 8 | 0.210 [0.151, 0.269] | 83.125 [77.886, 88.364] | 1.000 [0.106, 1.894] | [] |
| always-probe | immediate/baseline | 8 | 0.000 [0.000, 0.000] | 84.125 [78.536, 89.714] | 0.000 [0.000, 0.000] | [] |
| always-probe | immediate/expensive-probe | 8 | 0.000 [0.000, 0.000] | 84.125 [78.536, 89.714] | 0.000 [0.000, 0.000] | [] |
| always-probe | immediate/fast-switch | 8 | 0.000 [0.000, 0.000] | 240.000 [231.784, 248.216] | 0.000 [0.000, 0.000] | [] |
| always-probe | immediate/missing-signals | 8 | 0.210 [0.151, 0.269] | 83.125 [77.886, 88.364] | 1.000 [0.106, 1.894] | [] |
| failure-probe | delayed-history/baseline | 8 | 3.377 [3.153, 3.600] | 71.625 [67.915, 75.335] | 12.500 [10.311, 14.689] | [] |
| failure-probe | delayed-history/expensive-probe | 8 | 3.377 [3.153, 3.600] | 71.625 [67.915, 75.335] | 12.500 [10.311, 14.689] | [] |
| failure-probe | delayed-history/fast-switch | 8 | 1.872 [1.740, 2.005] | 179.500 [170.463, 188.537] | 60.500 [55.605, 65.395] | [] |
| failure-probe | delayed-history/missing-signals | 7 | 3.659 [2.352, 4.966] | 5.000 [2.036, 7.964] | 79.125 [73.291, 84.959] | [207] |
| failure-probe | immediate/baseline | 8 | 0.950 [0.929, 0.972] | 80.125 [75.303, 84.947] | 4.000 [2.328, 5.672] | [] |
| failure-probe | immediate/expensive-probe | 8 | 0.950 [0.929, 0.972] | 80.125 [75.303, 84.947] | 4.000 [2.328, 5.672] | [] |
| failure-probe | immediate/fast-switch | 8 | 0.874 [0.855, 0.894] | 213.125 [205.483, 220.767] | 26.875 [22.968, 30.782] | [] |
| failure-probe | immediate/missing-signals | 7 | 0.932 [0.813, 1.051] | 5.750 [0.014, 11.486] | 78.375 [71.839, 84.911] | [207] |
| fixed-interval | delayed-history/baseline | 8 | 2.822 [2.651, 2.992] | 71.750 [67.540, 75.960] | 12.375 [10.330, 14.420] | [] |
| fixed-interval | delayed-history/expensive-probe | 8 | 2.822 [2.651, 2.992] | 71.750 [67.540, 75.960] | 12.375 [10.330, 14.420] | [] |
| fixed-interval | delayed-history/fast-switch | 8 | 2.072 [1.912, 2.232] | 175.250 [171.210, 179.290] | 64.750 [59.191, 70.309] | [] |
| fixed-interval | delayed-history/missing-signals | 8 | 3.988 [3.774, 4.203] | 67.750 [63.612, 71.888] | 16.375 [14.536, 18.214] | [] |
| fixed-interval | immediate/baseline | 8 | 2.822 [2.651, 2.992] | 71.750 [67.540, 75.960] | 12.375 [10.330, 14.420] | [] |
| fixed-interval | immediate/expensive-probe | 8 | 2.822 [2.651, 2.992] | 71.750 [67.540, 75.960] | 12.375 [10.330, 14.420] | [] |
| fixed-interval | immediate/fast-switch | 8 | 2.072 [1.912, 2.232] | 175.250 [171.210, 179.290] | 64.750 [59.191, 70.309] | [] |
| fixed-interval | immediate/missing-signals | 8 | 3.988 [3.774, 4.203] | 67.750 [63.612, 71.888] | 16.375 [14.536, 18.214] | [] |
| learned-refresh | delayed-history/baseline | 8 | 0.496 [-0.071, 1.064] | 81.125 [75.924, 86.326] | 3.000 [-0.575, 6.575] | [] |
| learned-refresh | delayed-history/expensive-probe | 8 | 0.496 [-0.071, 1.064] | 81.125 [75.924, 86.326] | 3.000 [-0.575, 6.575] | [] |
| learned-refresh | delayed-history/fast-switch | 8 | 0.437 [-0.071, 0.945] | 226.375 [208.810, 243.940] | 13.625 [-1.970, 29.220] | [] |
| learned-refresh | delayed-history/missing-signals | 2 | 2.000 [-23.412, 27.412] | 0.250 [-0.137, 0.637] | 83.875 [78.162, 89.588] | [200, 202, 203, 204, 205, 206] |
| learned-refresh | immediate/baseline | 8 | 0.892 [0.817, 0.968] | 80.250 [75.360, 85.140] | 3.875 [2.299, 5.451] | [] |
| learned-refresh | immediate/expensive-probe | 8 | 0.892 [0.817, 0.968] | 80.250 [75.360, 85.140] | 3.875 [2.299, 5.451] | [] |
| learned-refresh | immediate/fast-switch | 8 | 0.826 [0.759, 0.893] | 214.500 [206.594, 222.406] | 25.500 [21.261, 29.739] | [] |
| learned-refresh | immediate/missing-signals | 8 | 0.887 [0.586, 1.189] | 4.375 [1.281, 7.469] | 79.750 [73.742, 85.758] | [] |
| never-probe | delayed-history/baseline | 8 | 0.000 [0.000, 0.000] | 41.750 [38.933, 44.567] | 42.375 [39.587, 45.163] | [] |
| never-probe | delayed-history/expensive-probe | 8 | 0.000 [0.000, 0.000] | 41.750 [38.933, 44.567] | 42.375 [39.587, 45.163] | [] |
| never-probe | delayed-history/fast-switch | 8 | 0.000 [0.000, 0.000] | 119.750 [115.564, 123.936] | 120.250 [116.210, 124.290] | [] |
| never-probe | delayed-history/missing-signals | 7 | 0.000 [0.000, 0.000] | 36.875 [24.138, 49.612] | 47.250 [36.555, 57.945] | [207] |
| never-probe | immediate/baseline | 8 | 0.000 [0.000, 0.000] | 41.750 [38.933, 44.567] | 42.375 [39.587, 45.163] | [] |
| never-probe | immediate/expensive-probe | 8 | 0.000 [0.000, 0.000] | 41.750 [38.933, 44.567] | 42.375 [39.587, 45.163] | [] |
| never-probe | immediate/fast-switch | 8 | 0.000 [0.000, 0.000] | 119.750 [115.564, 123.936] | 120.250 [116.210, 124.290] | [] |
| never-probe | immediate/missing-signals | 7 | 0.000 [0.000, 0.000] | 36.875 [24.138, 49.612] | 47.250 [36.555, 57.945] | [207] |
| outcome-memory | delayed-history/baseline | 8 | 3.977 [3.829, 4.124] | 69.625 [65.782, 73.468] | 14.500 [12.657, 16.343] | [] |
| outcome-memory | delayed-history/expensive-probe | 8 | 3.977 [3.829, 4.124] | 69.625 [65.782, 73.468] | 14.500 [12.657, 16.343] | [] |
| outcome-memory | delayed-history/fast-switch | 8 | 2.327 [2.149, 2.504] | 168.750 [163.014, 174.486] | 71.250 [66.993, 75.507] | [] |
| outcome-memory | delayed-history/missing-signals | 8 | 3.977 [3.829, 4.124] | 69.625 [65.782, 73.468] | 14.500 [12.657, 16.343] | [] |
| outcome-memory | immediate/baseline | 8 | 1.000 [1.000, 1.000] | 79.875 [75.053, 84.697] | 4.250 [2.421, 6.079] | [] |
| outcome-memory | immediate/expensive-probe | 8 | 1.000 [1.000, 1.000] | 79.875 [75.053, 84.697] | 4.250 [2.421, 6.079] | [] |
| outcome-memory | immediate/fast-switch | 8 | 1.000 [1.000, 1.000] | 209.250 [201.843, 216.657] | 30.750 [26.400, 35.100] | [] |
| outcome-memory | immediate/missing-signals | 8 | 1.000 [1.000, 1.000] | 79.875 [75.053, 84.697] | 4.250 [2.421, 6.079] | [] |

### Native archives

- relation: 242,135 bytes; signature=True, behavior=True; `relation.gojo`.
- consumer: 71,294 bytes; signature=True, behavior=True; `consumer.gojo`.
- pipeline-0: 430,321 bytes; signature=True, behavior=True; `pipeline-0.gojo`.
- refresh-0: 104,543 bytes; signature=True, behavior=True; `refresh-0.gojo`.
- pipeline-4: 433,328 bytes; signature=True, behavior=True; `pipeline-4.gojo`.
- refresh-4: 110,305 bytes; signature=True, behavior=True; `refresh-4.gojo`.

Interval method: two-sided 95% Student t on seed means or paired seed differences; df>30 uses conservative df30 critical; exploratory contrasts without multiplicity correction.
