# Demand-directed timing reads: first results

The fixed protocol uses 720 fine acquisitions and 60 twelve-tick reports per environment. One source publishes the identical packet to two independently controlled learners on the same public port. The eager reference constructs all three rungs; the attentive learner selects demand and applies the supplied probe/dormancy policy.

| Condition | Seed | Learner | Issued / exported / pending | Settled / absence / unassessable | Issue / event-assessment rows | Extra issue rows | Unused issues / issue rows / assessed rows | Dormant / sleep / wake reports | Final support | Managed allocated bytes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| regular | 113 | attentive | 49 / 49 / 1 | 48 / 0 / 0 | 59 / 58 | 10 | 0 / 0 / 0 | 0 / 0 / 0 | 8 | 42854840 |
| regular | 113 | eager | 59 / 49 / 1 | 58 / 0 / 0 | 177 / 174 | 118 | 10 / 30 / 30 | 0 / 0 / 0 | 8 | 51001440 |
| regular | 907 | attentive | 49 / 49 / 1 | 48 / 0 / 0 | 59 / 58 | 10 | 0 / 0 / 0 | 0 / 0 / 0 | 8 | 42850464 |
| regular | 907 | eager | 59 / 49 / 1 | 58 / 0 / 0 | 177 / 174 | 118 | 10 / 30 / 30 | 0 / 0 / 0 | 8 | 51000320 |
| regular | 2029 | attentive | 49 / 49 / 1 | 48 / 0 / 0 | 59 / 58 | 10 | 0 / 0 / 0 | 0 / 0 / 0 | 8 | 42846880 |
| regular | 2029 | eager | 59 / 49 / 1 | 58 / 0 / 0 | 177 / 174 | 118 | 10 / 30 / 30 | 0 / 0 / 0 | 8 | 50999264 |
| jitter | 113 | attentive | 49 / 49 / 1 | 48 / 0 / 0 | 64 / 63 | 15 | 0 / 0 / 0 | 0 / 0 / 0 | 6 | 44052352 |
| jitter | 113 | eager | 59 / 49 / 1 | 58 / 0 / 0 | 177 / 174 | 118 | 10 / 30 / 30 | 0 / 0 / 0 | 6 | 51168704 |
| jitter | 907 | attentive | 49 / 49 / 1 | 48 / 0 / 0 | 69 / 67 | 20 | 0 / 0 / 0 | 0 / 0 / 0 | 4 | 44811336 |
| jitter | 907 | eager | 59 / 49 / 1 | 58 / 0 / 0 | 177 / 174 | 118 | 10 / 30 / 30 | 0 / 0 / 0 | 4 | 51126032 |
| jitter | 2029 | attentive | 49 / 49 / 1 | 48 / 0 / 0 | 68 / 66 | 19 | 0 / 0 / 0 | 0 / 0 / 0 | 3 | 44740448 |
| jitter | 2029 | eager | 59 / 49 / 1 | 58 / 0 / 0 | 177 / 174 | 118 | 10 / 30 / 30 | 0 / 0 / 0 | 3 | 51207104 |
| intermittent | 113 | attentive | 34 / 34 / 0 | 34 / 17 / 0 | 51 / 19 | 17 | 0 / 0 / 0 | 15 / 3 / 2 | 0 | 32169584 |
| intermittent | 113 | eager | 59 / 49 / 1 | 58 / 30 / 0 | 177 / 84 | 118 | 10 / 30 / 27 | 0 / 0 / 0 | 0 | 44487504 |
| intermittent | 907 | attentive | 34 / 34 / 0 | 34 / 17 / 0 | 51 / 19 | 17 | 0 / 0 / 0 | 15 / 3 / 2 | 0 | 32169584 |
| intermittent | 907 | eager | 59 / 49 / 1 | 58 / 30 / 0 | 177 / 84 | 118 | 10 / 30 / 27 | 0 / 0 / 0 | 0 | 44487504 |
| intermittent | 2029 | attentive | 34 / 34 / 0 | 34 / 17 / 0 | 51 / 19 | 17 | 0 / 0 / 0 | 15 / 3 / 2 | 0 | 32169584 |
| intermittent | 2029 | eager | 59 / 49 / 1 | 58 / 30 / 0 | 177 / 84 | 118 | 10 / 30 / 27 | 0 / 0 / 0 | 0 | 44487504 |
| changed-cadence | 113 | attentive | 49 / 49 / 1 | 48 / 6 / 0 | 64 / 52 | 15 | 0 / 0 / 0 | 0 / 0 / 0 | 8 | 41944840 |
| changed-cadence | 113 | eager | 59 / 49 / 1 | 58 / 15 / 0 | 177 / 129 | 118 | 10 / 30 / 3 | 0 / 0 / 0 | 8 | 47611640 |
| changed-cadence | 907 | attentive | 49 / 49 / 1 | 48 / 6 / 0 | 64 / 52 | 15 | 0 / 0 / 0 | 0 / 0 / 0 | 8 | 41944840 |
| changed-cadence | 907 | eager | 59 / 49 / 1 | 58 / 15 / 0 | 177 / 129 | 118 | 10 / 30 / 3 | 0 / 0 / 0 | 8 | 47611640 |
| changed-cadence | 2029 | attentive | 49 / 49 / 1 | 48 / 6 / 0 | 64 / 52 | 15 | 0 / 0 / 0 | 0 / 0 / 0 | 8 | 41944840 |
| changed-cadence | 2029 | eager | 59 / 49 / 1 | 58 / 15 / 0 | 177 / 129 | 118 | 10 / 30 / 3 | 0 / 0 / 0 | 8 | 47611640 |

Each cell below reports wanted hits/misses; unused hits/misses, grouped by the width frozen at issuance. Wanted means consumer demand was positive when the forecast was issued. Eager forecasts created during withdrawal are retained and graded honestly but are not retroactively useful when demand returns. Different widths are different promises, and different issued sets are not a paired accuracy comparison.

| Condition | Seed | Learner | Width 1: wanted; unused | Width 3: wanted; unused | Width 12: wanted; unused |
| --- | --- | --- | --- | --- | --- |
| regular | 113 | attentive | 10/0; 0/0 | 28/0; 0/0 | 10/0; 0/0 |
| regular | 113 | eager | 10/0; 0/0 | 28/0; 10/0 | 10/0; 0/0 |
| regular | 907 | attentive | 10/0; 0/0 | 28/0; 0/0 | 10/0; 0/0 |
| regular | 907 | eager | 10/0; 0/0 | 28/0; 10/0 | 10/0; 0/0 |
| regular | 2029 | attentive | 10/0; 0/0 | 28/0; 0/0 | 10/0; 0/0 |
| regular | 2029 | eager | 10/0; 0/0 | 28/0; 10/0 | 10/0; 0/0 |
| jitter | 113 | attentive | 3/7; 0/0 | 20/8; 0/0 | 10/0; 0/0 |
| jitter | 113 | eager | 3/7; 0/0 | 20/8; 7/3 | 10/0; 0/0 |
| jitter | 907 | attentive | 3/7; 0/0 | 14/14; 0/0 | 10/0; 0/0 |
| jitter | 907 | eager | 3/7; 0/0 | 14/14; 5/5 | 10/0; 0/0 |
| jitter | 2029 | attentive | 2/8; 0/0 | 16/12; 0/0 | 10/0; 0/0 |
| jitter | 2029 | eager | 2/8; 0/0 | 16/12; 8/2 | 10/0; 0/0 |
| intermittent | 113 | attentive | 9/1; 0/0 | 8/15; 0/0 | 0/1; 0/0 |
| intermittent | 113 | eager | 9/1; 0/0 | 8/20; 9/1 | 1/9; 0/0 |
| intermittent | 907 | attentive | 9/1; 0/0 | 8/15; 0/0 | 0/1; 0/0 |
| intermittent | 907 | eager | 9/1; 0/0 | 8/20; 9/1 | 1/9; 0/0 |
| intermittent | 2029 | attentive | 9/1; 0/0 | 8/15; 0/0 | 0/1; 0/0 |
| intermittent | 2029 | eager | 9/1; 0/0 | 8/20; 9/1 | 1/9; 0/0 |
| changed-cadence | 113 | attentive | 10/0; 0/0 | 22/6; 0/0 | 9/1; 0/0 |
| changed-cadence | 113 | eager | 10/0; 0/0 | 22/6; 0/10 | 9/1; 0/0 |
| changed-cadence | 907 | attentive | 10/0; 0/0 | 22/6; 0/0 | 9/1; 0/0 |
| changed-cadence | 907 | eager | 10/0; 0/0 | 22/6; 0/10 | 9/1; 0/0 |
| changed-cadence | 2029 | attentive | 10/0; 0/0 | 22/6; 0/0 | 9/1; 0/0 |
| changed-cadence | 2029 | eager | 10/0; 0/0 | 22/6; 0/10 | 9/1; 0/0 |

The following constant host reference predictions were fixed before input: no event, or at least one event, in the next twelve-tick report window. They use only complete public reports for grading; startup uncertainty is excluded. These easier event-presence scores are environment diagnostics, not Gojo reasoning claims or exact-timing baselines. Their shared denominator is the complete-report count.

| Condition | Seed | Fine acquisitions / reports | Complete reports | Constant absence correct | Constant presence correct |
| --- | --- | --- | --- | --- | --- |
| regular | 113 | 720 / 60 | 59 | 0 | 59 |
| regular | 907 | 720 / 60 | 59 | 0 | 59 |
| regular | 2029 | 720 / 60 | 59 | 0 | 59 |
| jitter | 113 | 720 / 60 | 59 | 0 | 59 |
| jitter | 907 | 720 / 60 | 59 | 0 | 59 |
| jitter | 2029 | 720 / 60 | 59 | 0 | 59 |
| intermittent | 113 | 720 / 60 | 59 | 30 | 29 |
| intermittent | 907 | 720 / 60 | 59 | 30 | 29 |
| intermittent | 2029 | 720 / 60 | 59 | 30 | 29 |
| changed-cadence | 113 | 720 / 60 | 59 | 15 | 44 |
| changed-cadence | 907 | 720 / 60 | 59 | 15 | 44 |
| changed-cadence | 2029 | 720 / 60 | 59 | 15 | 44 |

Demand is width 3 through tick 240, zero at 241–360, width 12 at 361–480, width 1 at 481–600, then width 3. It is applied before report consumption. The eager reference retains its last positive width during withdrawal and continues all-rung work; unused rows are counted separately. Outstanding promises keep their original width and wanted-at-issue classification. The trace records both that frozen promise and current demand.

Regular runs last five ticks; jitter draws lengths 4–6. Intermittent alternates 120 moving and 120 flat ticks, restarting a positive five-tick run at each moving block. Changed cadence runs at five through tick 180, holds through 360, restarts at eight at 361, then restarts at four at 541. Only jitter is random: the other seed-labelled rows repeat deterministic environments and are not independent trials. Reversal times are detector observations, not physical inflection claims.

New forecasts are issued only after the current report has been assessed, strictly beyond its availability; there is no same-batch grading. Event assessments use the native selected-rung verdict and signed difference. Absence misses have no invented event time or zero timing error. Forecasts still pending at tick 720 are neither successes nor misses. Dormant means the native event gate, distinct from zero utility demand. A wake can preserve knowledge that an event arrived without immediately supplying the two fresh events needed for a new cadence.

Issued rows and event-assessment rows are exposed structural work proxies. Extra attentive rows are diagnostic probes; eager extra rows are its always-created unselected reads. All rows share a forecast, so they are not independent evidence or separate rewards. The proxies omit width selection, report validation, absence probes, attention/control witnesses, source arithmetic and transient allocations; they do not measure runtime or net compute savings. Source acquisition remains 720 for both consumers. No sensor savings or adaptive sampler is claimed.

Managed allocated bytes are the raw same-thread counter difference immediately around each learner's `TryObserve` call, with all sixty calls and startup included. Source observation/publication, demand assignment, report scoring/formatting and graph capture are outside that interval. The CSV records each call and the table their cumulative sum. This diagnostic can expose controller overhead hidden by rung counts; it is not a benchmark, CPU time, total process allocation or retained-memory measurement. No warmup rows are removed.

The native policy supplies demand, four-issue probe cadence, failure-triggered probing and absence-driven dormancy; it does not learn those rules. Diagnostics do not yet revise which width is demanded. `graph-sizes.csv` samples each source and learner every 120 ticks after report processing. Counts describe reachable retained structure, not cumulative allocation. `traces.csv` keeps report-level outputs outside the learners; neither model retains this report history.
