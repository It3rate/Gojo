# Sampling before measurement

This small exploratory Gojo fixture receives a presence predicate and a service that answers only candidate-versus-anchor order. It does not receive the private source numbers while learning its landmarks.

The original and transformed signals have identical ordinal projections: **True**. The transformation changes private magnitudes by 100 × value + 1000 while preserving absence. It cannot establish a metric from ordering.

| Scenario | Acquisitions | Present | Distinct fixture values | Landmarks | Comparison queries | Coarse intervals | Final archive bytes |
|---|---:|---:|---:|---:|---:|---:|---:|
| An irregular signal | 3 | 0 | 0 | 0/1 | 0 | 0 | 7,979 |
| The same signal, rescaled | 3 | 0 | 0 | 0/1 | 0 | 0 | 7,979 |
| A tiny change inside a broad interval | 4 | 4 | 4 | 2/2 | 6 | 2 | 29,814 |

## What the distance example establishes

With just two anchors, both 100 miles and 100 miles + 1 mm occupy the same 90-to-110-mile interval. A separate comparison against the preceding present observation still detects the +1 direction. Neither the interval nor that sign says how large the change was.

Only the final step supplies explicit integer-millimetre publications through a common Gojo basis, retaining the calibration 1 mile = 1,609,344 mm. The two values then read 160,934,400 mm and 160,934,401 mm. These manually chosen reference distances demonstrate the contract; they are not measurements acquired from a physical instrument.

## Structural boundary

`SamplingSurface` retains native samples, sorted anchors, visit witnesses, requested comparisons, open magnitude slots, and later sample-to-qualified-publication bindings. The C# dictionary is only the external comparison instrument. JSON and the browser view inspect the native structure and may show private reference values in a clearly separate display.

The finite metric publication uses a signed int.MaxValue domain and quantum 1. Clipped or discarded detail follows that exposed finite-read contract; no wider exact answer is calculated to rescue it. No equal-spacing assumption, distribution, learned nominal series, or learned metric is claimed.

The anchor budget is **not** a graph-memory bound. All visits and comparison witnesses remain retained in this small fixture, so native archive sizes can continue growing after the anchor budget fills. Each step has its own `.gojo` archive; `results.json` and `index.html` are presentation projections.
