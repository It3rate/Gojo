# Experiment 03: predictions before observations

Protocol `predictions-v1`. 12 completed cases; 0 apparatus failures; 0 field differences from the independent exact affine reference.

Mean absolute error (MAE) is compared only on shared targets with an observed outcome. Missing targets are not zeros. The external hold-last baseline also has wider coverage after just one known sample; complete counts and errors are in each trace.

| Case | Split | Line issued / targets | Common scored | Line MAE | Hold-last MAE | Line awaiting outcome | Exact-model differences |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Constant (development) | development | 4/6 | 4 | 0 | 0 | 0 | 0 |
| Ramp (development) | development | 4/6 | 4 | 0 | 4 | 0 | 0 |
| Unequal intervals (development) | development | 3/5 | 3 | 1 | 1 | 0 | 0 |
| A constant below zero | evaluation | 6/8 | 6 | 0 | 0 | 0 | 0 |
| A ramp with unequal intervals | evaluation | 5/7 | 5 | 0 | 39/5 | 0 | 0 |
| The trend reverses | evaluation | 7/9 | 7 | 6/7 | 3 | 0 | 0 |
| The signal alternates | evaluation | 8/10 | 8 | 12 | 6 | 0 | 0 |
| A sudden step | evaluation | 7/9 | 7 | 16/7 | 8/7 | 0 | 0 |
| Missing samples across a change | evaluation | 8/10 | 5 | 26/15 | 11/5 | 3 | 0 |
| Missing observations at the start | evaluation | 3/8 | 3 | 1/2 | 3 | 0 | 0 |
| Observations stop arriving | evaluation | 6/8 | 1 | 0 | 3 | 5 | 0 |
| No measured values | evaluation | 0/6 | 0 | unavailable | unavailable | 0 | 0 |

No-prediction coverage is zero in every case; its error is unavailable. Counts measure selected calls and retained records, not CPU or energy. These hand-authored examples do not establish a population-wide winner. No feedback changes the selected model family.

[Viewer](/minds/predictions/) · [Selected rules](/minds/predictions/Protocol.md) · [All tapes](/minds/predictions/cases.json) · [Complete summary](/minds/predictions/summary.json). Each run includes a before-observation/after-observation event journal.
