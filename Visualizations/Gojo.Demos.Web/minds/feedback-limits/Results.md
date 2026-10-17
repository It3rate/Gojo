# Experiment 02: feedback limits

Protocol `feedback-limits-v1`. 72 completed runs, 0 apparatus failures. 0 event/round field disagreements with the independent exact reference.

All values below use A units; movement is the sum of absolute changes since the preceding paired observation. It is not physical energy. Each row shows the first completed run. Distinct-trace counts compare recorded observations, not complete restorable graphs.

| Condition | Final A | Final B | B minus A | Last-round movement | Comparison |
| --- | ---: | ---: | ---: | ---: | --- |
| Half response, longer window | 7 | 7 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Full response | 5 | 9 | 4 | 8 | 3 runs; 1 distinct; 0 mismatches |
| Quarter response | 34/5 | 139/20 | 3/20 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Reverse the initial disagreement | 7 | 7 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Start one unit apart | 11/2 | 11/2 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Start in agreement | 5 | 5 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Start at zero and one | 1/2 | 1/2 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Quarter response, equal physical ticks | 69/10 | 71/10 | 1/5 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Quarter response, finer ticks | 349/50 | 1399/200 | 3/200 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Quarter response, coarser ticks | 6 | 15/2 | 3/2 | 0 | 3 runs; 1 distinct; 0 mismatches |
| One B unit per A unit | 7 | 7 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Four B units per A unit | 7 | 7 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Receive without responding | 5 | 9 | 4 | 0 | 3 runs; 1 distinct; 0 mismatches |
| No deliveries | 5 | 9 | 4 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Only A reaches B | 5 | 101/20 | 1/20 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Every other B message is lost | 7 | 7 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Full response, one-round delivery delay | 5 | 9 | 4 | 8 | 3 runs; 1 distinct; 0 mismatches |
| Full response, A responds first | 9 | 9 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Full response, B responds first | 5 | 5 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| An old message arrives after a new one | 7 | 7 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Select changed references in round six | 4 | 4 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Observe conflicting references only | 7 | 7 | 0 | 0 | 3 runs; 1 distinct; 0 mismatches |
| Periodically select conflicting references | 2 | 8 | 6 | 6 | 3 runs; 1 distinct; 0 mismatches |
| Quarter response, two applications allowed | 13/2 | 15/2 | 1 | 0 | 3 runs; 1 distinct; 0 mismatches |

Twelve rounds end observation. Equal values, quiet estimates, unresolved requests and controller limits are separate findings. Ordinary exact arithmetic retains the same fractional requests, copied messages and counters. A match establishes selected contract agreement, not a Gojo performance advantage.

[Viewer](index.html) · [Selected rules](Protocol.md) · [All conditions and parameters](cases.json) · [Summary and failures](summary.json). Individual run files retain both implementations; the viewer includes every distinct recorded trajectory.
