# Shared timing ladder: first results

The fixed protocol ran 360 base ticks per condition and seed. Two independent sources received the same environment: fine acquisition every tick and sparse acquisition every twelve ticks, both reporting every twelve ticks. Each independent learner used the same native cadence construction, demanded width 3, and diagnostic widths 1, 3 and 12.

| Condition | Seed | Acquisition | Acquisitions / reports | Known events / incomplete reports | Issued / settled / unassessable / pending | Hits: width 1 / 3 / 12 | Final support | Forecast cells / assessment rows |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| regular | 113 | fine | 360 / 30 | 71 / 1 | 29 / 28 / 0 / 1 | 28 / 28 / 28 | 8 | 87 / 84 |
| regular | 113 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| regular | 907 | fine | 360 / 30 | 71 / 1 | 29 / 28 / 0 / 1 | 28 / 28 / 28 | 8 | 87 / 84 |
| regular | 907 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| regular | 2029 | fine | 360 / 30 | 71 / 1 | 29 / 28 / 0 / 1 | 28 / 28 / 28 | 8 | 87 / 84 |
| regular | 2029 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| jitter | 113 | fine | 360 / 30 | 72 / 1 | 29 / 28 / 0 / 1 | 12 / 20 / 28 | 8 | 87 / 84 |
| jitter | 113 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| jitter | 907 | fine | 360 / 30 | 70 / 1 | 29 / 28 / 0 / 1 | 5 / 14 / 28 | 4 | 87 / 84 |
| jitter | 907 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| jitter | 2029 | fine | 360 / 30 | 73 / 1 | 29 / 28 / 0 / 1 | 12 / 18 / 28 | 8 | 87 / 84 |
| jitter | 2029 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| drift | 113 | fine | 360 / 30 | 57 / 1 | 29 / 28 / 0 / 1 | 26 / 27 / 28 | 8 | 87 / 84 |
| drift | 113 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| drift | 907 | fine | 360 / 30 | 57 / 1 | 29 / 28 / 0 / 1 | 26 / 27 / 28 | 8 | 87 / 84 |
| drift | 907 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| drift | 2029 | fine | 360 / 30 | 57 / 1 | 29 / 28 / 0 / 1 | 26 / 27 / 28 | 8 | 87 / 84 |
| drift | 2029 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| stopped | 113 | fine | 360 / 30 | 35 / 1 | 29 / 28 / 0 / 1 | 13 / 13 / 13 | 0 | 87 / 39 |
| stopped | 113 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| stopped | 907 | fine | 360 / 30 | 35 / 1 | 29 / 28 / 0 / 1 | 13 / 13 / 13 | 0 | 87 / 39 |
| stopped | 907 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |
| stopped | 2029 | fine | 360 / 30 | 35 / 1 | 29 / 28 / 0 / 1 | 13 / 13 / 13 | 0 | 87 / 39 |
| stopped | 2029 | sparse | 30 / 30 | 0 / 30 | 0 / 0 / 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 |

Known events count only explicit public point spans, including the first fine report's partial occurrences. An open sparse collection contributes no known events; that is not a zero-event assertion. Incomplete reports include open collections and the fine startup report's explicit missing-difference residue. The learner does not bootstrap from either.

Hits use settled forecasts as their denominator, including absence misses. Widths 1, 3 and 12 make different promises: wider-cell agreement is not an accuracy improvement over a narrower task. Every rung shares the same forecast and event, so the three reads are not independent evidence. Only the frozen width-3 verdict changes support. A forecast still pending at tick 360 is not counted as a miss.

The trace CSV records each report, prior forecast issuance/prediction, new issuance, native verdict, all three rung matches, and one signed event-minus-prediction value per event-assessed forecast. Negative is early and positive is late. Unknown assessments and absence misses leave event time and signed error blank; they are not zero error.

Regular uses five-tick runs. Jitter draws each run length uniformly from 4, 5 or 6 using the stated seed. Drift selects lengths 5, 6, 7 and 8 for runs starting in successive ninety-tick blocks; a run crossing a boundary completes before the new duration applies. Stopped follows five-tick runs through tick 180, then holds its endpoint. Reversal detection occurs on the first tick with the changed nonzero delta. Only jitter uses randomness: regular, drift and stopped repeat identical environments across seeds and are not independent replicates.

Sparse acquisition's open fine-event contract cannot bootstrap this event-based learner; no sparse forecasts demonstrate a limitation of this supplied interface and learner, not an impossibility of learning from endpoints. Support is a bounded supplied utility trace, not calibrated confidence or a gating rule. This pass neither prunes rungs nor learns atrophy; an old cadence may continue proposing after misses.

Forecast-cell rows and event-assessment rows count exposed native read products as a work proxy. They exclude receipt checks, absence-deadline probes, source calculations, native subreads and temporary allocations; they are not measured runtime or evidence of compute savings. `graph-sizes.csv` samples each source and learner at prefixes 120, 240 and 360 after publication/assessment, counting reachable carriers, relations, tick references, locations, unit shapes and placeholders. These counts describe retained graphs, not total allocation or processing cost.

