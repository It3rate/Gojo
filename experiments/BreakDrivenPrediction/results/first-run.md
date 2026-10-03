# First break-driven continuation trials

All forecasts precede their outcomes. All arrivals are scored, including startup.

| Trial | Correct direction: export / no timing / last change | Turns anticipated | False turns | Mean magnitude error | Timing links added / removed | Candidate inspections | Final carriers / relations | Final pressure |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| regular-3-5 | 105/112 / 85/112 / 84/111 | 20/27 | 0 | 0.04 | 2 / 0 | 41 | 85 / 873 | 0 |
| mirrored-3-5 | 105/112 / 84/112 / 84/111 | 21/27 | 0 | 0.01 | 2 / 0 | 42 | 85 / 873 | 0 |
| cadence-change | 98/112 / 85/112 / 84/111 | 14/27 | 1 | 0.04 | 4 / 2 | 41 | 85 / 873 | 0 |
| irregular | 82/107 / 78/107 / 77/106 | 4/29 | 0 | 0.04 | 2 / 2 | 44 | 77 / 788 | 0 |
| scale-change | 105/112 / 85/112 / 84/111 | 20/27 | 0 | 0.33 | 2 / 0 | 41 | 85 / 873 | 0 |
| stable-no-request | 34/34 / 34/34 / 33/33 | 0/0 | 0 | 4.53 | 0 / 0 | 0 | 31 / 268 | 0 |
| stable-with-request | 34/34 / 34/34 / 33/33 | 0/0 | 0 | 1.53 | 0 / 0 | 0 | 54 / 489 | 0 |
| zero-and-chatter | 61/81 / 50/81 / 49/80 | 12/25 | 1 | 0.17 | 4 / 2 | 49 | 85 / 873 | 0 |

Direction means strict nonzero signed agreement at the supplied integer basis. Zero deltas remain uncovered by the two primitives. Last-change has no forecast for the first arrival. Magnitude error ignores sign deliberately; wrong direction with correct scale can therefore have zero magnitude error. Timing links may remain available but dormant when their source is not selected.

The environment uses host arithmetic; learner forecasts, difference reads, comparison claims, support changes and timing calculations use native Gojo structure. Host code applies the declared recruitment and bounded support policy. These trials do not establish autonomous primitive discovery, physical cadence, learned utility, adaptive resolution or crystallization.

Candidate inspections count primitive match reads during break-triggered recruitment. Active recurrence, assessment and any existing timing route still require per-arrival reads; this count is not a measure of total compute savings.

Reachable graph samples, observation count: carriers/relations. These include attachment ownership and retained evidence; they exclude discarded calculation allocations.

- regular-3-5: 32: 85/873; 64: 85/873; 96: 85/873
- mirrored-3-5: 32: 85/873; 64: 85/873; 96: 85/873
- cadence-change: 32: 85/873; 64: 77/790; 96: 85/873
- irregular: 32: 85/873; 64: 77/788; 96: 77/788
- scale-change: 32: 85/873; 64: 85/873; 96: 85/873
- stable-no-request: 32: 31/268
- stable-with-request: 32: 54/489
- zero-and-chatter: 32: 85/873; 64: 85/873
