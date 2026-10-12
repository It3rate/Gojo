# Learning-progress development run

2 paired seeds; 16,384 total world tasks.
Resource prices are abstract. The primary measure is physical gross minus burned work per **all** world tasks.

## Group results

| Arm | Net/task | 95% descriptive interval | Gross/task | Burn/task | Action coverage | Bought accuracy | Updates |
|---|---:|---:|---:|---:|---:|---:|---:|
| progress/error-only/upfront | 2.472 | — | 4.931 | 2.459 | 67.7% | 93.6% | 598 |
| progress/signed/upfront | 2.481 | — | 4.911 | 2.430 | 68.0% | 93.5% | 460 |
| progress/cycle-only/upfront | 2.481 | — | 4.911 | 2.430 | 68.0% | 93.5% | 460 |
| progress/change/upfront | 2.481 | — | 4.911 | 2.430 | 68.0% | 93.5% | 460 |

## Paired changes

| Left minus right | Status | Net/task change | 95% descriptive interval | Positive seeds |
|---|---|---:|---:|---:|
| signed minus error | Primary | 0.010 | — | 2/2 |
| signed minus cycle | Primary | 0.000 | — | 0/2 |
| change minus signed | Diagnostic | 0.000 | — | 0/2 |

This development run has no confidence intervals. It is not fresh confirmation evidence.

## Permission and funding

| Arm | Decisions | Error requests | Final gate requests | Paid grants | Requested but unfunded | Suppressed error requests | Model updates |
|---|---:|---:|---:|---:|---:|---:|---:|
| progress/error-only/upfront | 2774 | 598 | 598 | 598 | 0 | 0 | 598 |
| progress/signed/upfront | 2784 | 627 | 460 | 460 | 0 | 167 | 460 |
| progress/cycle-only/upfront | 2784 | 627 | 460 | 460 | 0 | 167 | 460 |
| progress/change/upfront | 2784 | 627 | 460 | 460 | 0 | 167 | 460 |

## Unique progress comparisons and opened episodes

| Arm | Versions first read | Numeric error versions | Open error versions | Positive threshold | Regression threshold | Coverage threshold | Positive episodes | Change episodes | Coverage episodes | Retests |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| progress/error-only/upfront | 163 | 151 | 12 | 4 | 1 | 1 | 0 | 0 | 0 | 0 |
| progress/signed/upfront | 163 | 152 | 11 | 2 | 1 | 0 | 1 | 0 | 0 | 84 |
| progress/cycle-only/upfront | 163 | 152 | 11 | 2 | 1 | 0 | 0 | 0 | 0 | 85 |
| progress/change/upfront | 163 | 152 | 11 | 2 | 1 | 0 | 1 | 1 | 0 | 83 |

Version counts are deduplicated per seed and provider. Adjacent versions may share one block.
An episode opening is not necessarily a funded grant, and neither guarantees a useful model update.

## Pure noise: source 2 in every phase

| Arm | Net/task | Burn/task | Coverage | Paid grants | Updates | Refinements | Comparisons | Positive/change/coverage episodes |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| progress/error-only/upfront | -1.439 | 1.002 | 17.8% | 148 | 148 | 9 | 170 | 0/0/0 |
| progress/signed/upfront | -1.437 | 0.999 | 19.0% | 100 | 100 | 9 | 192 | 0/0/0 |
| progress/cycle-only/upfront | -1.437 | 0.999 | 19.0% | 100 | 100 | 9 | 192 | 0/0/0 |
| progress/change/upfront | -1.437 | 0.999 | 19.0% | 100 | 100 | 9 | 192 | 0/0/0 |

## Early noise and final new utility: source 3

| Arm | Early-noise net/task | Early-noise paid grants | Final net/task | Profitable final seeds | Final coverage | Final bought accuracy | Numeric/open versions first read in final phase |
|---|---:|---:|---:|---:|---:|---:|---|
| progress/error-only/upfront | -1.565 | 68 | 0.775 | 1/2 | 30.9% | 95.2% | 4/1 |
| progress/signed/upfront | -1.345 | 52 | 0.830 | 1/2 | 30.9% | 94.0% | 5/0 |
| progress/cycle-only/upfront | -1.345 | 52 | 0.830 | 1/2 | 30.9% | 94.0% | 5/0 |
| progress/change/upfront | -1.345 | 52 | 0.830 | 1/2 | 30.9% | 94.0% | 5/0 |

Final-phase versions are assigned to the phase of their first decision read; their blocks can include earlier-phase observations.
A returning source's old comparison can be read in the new phase. Full per-seed window-known counts are in analysis.json.

## Rare return: first 16 world requests in phase 3

| Arm | Net/request | Coverage | Bought accuracy | Paid grants |
|---|---:|---:|---:|---:|
| progress/error-only/upfront | 0.406 | 15.6% | 80.0% | 5 |
| progress/signed/upfront | 0.469 | 15.6% | 80.0% | 3 |
| progress/cycle-only/upfront | 0.469 | 15.6% | 80.0% | 3 |
| progress/change/upfront | 0.469 | 15.6% | 80.0% | 3 |

The first 16 **purchases** are also reported separately in analysis.json; these are different denominators.

## Phase and source diagnostics

| Arm | Phase (0-based) | Source | Net/task | Burn/task | Coverage | Bought accuracy | Updates | Refinements |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| progress/error-only/upfront | 0 | 0 | 5.535 | 2.324 | 100.0% | 99.3% | 50 | 0 |
| progress/error-only/upfront | 0 | 1 | -1.453 | 1.295 | 27.3% | 58.5% | 53 | 2 |
| progress/error-only/upfront | 0 | 2 | -2.534 | 1.837 | 38.8% | 50.0% | 66 | 3 |
| progress/error-only/upfront | 0 | 3 | -2.560 | 1.854 | 44.1% | 51.2% | 41 | 2 |
| progress/error-only/upfront | 1 | 0 | 3.152 | 3.835 | 100.0% | 95.0% | 92 | 7 |
| progress/error-only/upfront | 1 | 2 | -1.050 | 0.768 | 12.3% | 48.6% | 35 | 2 |
| progress/error-only/upfront | 1 | 3 | -1.160 | 0.678 | 12.1% | 40.0% | 15 | 0 |
| progress/error-only/upfront | 2 | 0 | 4.370 | 3.630 | 100.0% | 100.0% | 43 | 0 |
| progress/error-only/upfront | 2 | 2 | -1.270 | 0.765 | 12.6% | 40.0% | 30 | 2 |
| progress/error-only/upfront | 2 | 3 | -1.106 | 0.888 | 11.9% | 50.0% | 12 | 2 |
| progress/error-only/upfront | 3 | 0 | 4.355 | 3.645 | 100.0% | 100.0% | 22 | 0 |
| progress/error-only/upfront | 3 | 1 | 9.364 | 3.177 | 81.0% | 99.0% | 59 | 1 |
| progress/error-only/upfront | 3 | 2 | -1.204 | 0.924 | 12.8% | 50.0% | 17 | 2 |
| progress/error-only/upfront | 3 | 3 | 0.775 | 1.469 | 30.9% | 95.2% | 63 | 1 |
| progress/signed/upfront | 0 | 0 | 5.535 | 2.324 | 100.0% | 99.3% | 50 | 0 |
| progress/signed/upfront | 0 | 1 | -1.407 | 1.248 | 27.3% | 58.5% | 45 | 2 |
| progress/signed/upfront | 0 | 2 | -2.553 | 1.874 | 41.8% | 50.7% | 54 | 3 |
| progress/signed/upfront | 0 | 3 | -2.550 | 1.845 | 44.1% | 51.2% | 40 | 2 |
| progress/signed/upfront | 1 | 0 | 3.237 | 3.750 | 100.0% | 95.0% | 73 | 6 |
| progress/signed/upfront | 1 | 2 | -1.238 | 0.818 | 14.1% | 45.0% | 21 | 3 |
| progress/signed/upfront | 1 | 3 | -0.790 | 0.630 | 12.1% | 53.3% | 8 | 0 |
| progress/signed/upfront | 2 | 0 | 4.370 | 3.630 | 100.0% | 100.0% | 43 | 0 |
| progress/signed/upfront | 2 | 2 | -1.160 | 0.741 | 12.6% | 43.3% | 14 | 2 |
| progress/signed/upfront | 2 | 3 | -0.888 | 0.851 | 11.9% | 58.3% | 4 | 2 |
| progress/signed/upfront | 3 | 0 | 4.355 | 3.645 | 100.0% | 100.0% | 22 | 0 |
| progress/signed/upfront | 3 | 1 | 8.889 | 3.151 | 81.0% | 97.5% | 36 | 2 |
| progress/signed/upfront | 3 | 2 | -0.967 | 0.810 | 12.8% | 55.0% | 11 | 1 |
| progress/signed/upfront | 3 | 3 | 0.830 | 1.337 | 30.9% | 94.0% | 39 | 0 |
| progress/cycle-only/upfront | 0 | 0 | 5.535 | 2.324 | 100.0% | 99.3% | 50 | 0 |
| progress/cycle-only/upfront | 0 | 1 | -1.407 | 1.248 | 27.3% | 58.5% | 45 | 2 |
| progress/cycle-only/upfront | 0 | 2 | -2.553 | 1.874 | 41.8% | 50.7% | 54 | 3 |
| progress/cycle-only/upfront | 0 | 3 | -2.550 | 1.845 | 44.1% | 51.2% | 40 | 2 |
| progress/cycle-only/upfront | 1 | 0 | 3.237 | 3.750 | 100.0% | 95.0% | 73 | 6 |
| progress/cycle-only/upfront | 1 | 2 | -1.238 | 0.818 | 14.1% | 45.0% | 21 | 3 |
| progress/cycle-only/upfront | 1 | 3 | -0.790 | 0.630 | 12.1% | 53.3% | 8 | 0 |
| progress/cycle-only/upfront | 2 | 0 | 4.370 | 3.630 | 100.0% | 100.0% | 43 | 0 |
| progress/cycle-only/upfront | 2 | 2 | -1.160 | 0.741 | 12.6% | 43.3% | 14 | 2 |
| progress/cycle-only/upfront | 2 | 3 | -0.888 | 0.851 | 11.9% | 58.3% | 4 | 2 |
| progress/cycle-only/upfront | 3 | 0 | 4.355 | 3.645 | 100.0% | 100.0% | 22 | 0 |
| progress/cycle-only/upfront | 3 | 1 | 8.889 | 3.151 | 81.0% | 97.5% | 36 | 2 |
| progress/cycle-only/upfront | 3 | 2 | -0.967 | 0.810 | 12.8% | 55.0% | 11 | 1 |
| progress/cycle-only/upfront | 3 | 3 | 0.830 | 1.337 | 30.9% | 94.0% | 39 | 0 |
| progress/change/upfront | 0 | 0 | 5.535 | 2.324 | 100.0% | 99.3% | 50 | 0 |
| progress/change/upfront | 0 | 1 | -1.407 | 1.248 | 27.3% | 58.5% | 45 | 2 |
| progress/change/upfront | 0 | 2 | -2.553 | 1.874 | 41.8% | 50.7% | 54 | 3 |
| progress/change/upfront | 0 | 3 | -2.550 | 1.845 | 44.1% | 51.2% | 40 | 2 |
| progress/change/upfront | 1 | 0 | 3.237 | 3.750 | 100.0% | 95.0% | 73 | 6 |
| progress/change/upfront | 1 | 2 | -1.238 | 0.818 | 14.1% | 45.0% | 21 | 3 |
| progress/change/upfront | 1 | 3 | -0.790 | 0.630 | 12.1% | 53.3% | 8 | 0 |
| progress/change/upfront | 2 | 0 | 4.370 | 3.630 | 100.0% | 100.0% | 43 | 0 |
| progress/change/upfront | 2 | 2 | -1.160 | 0.741 | 12.6% | 43.3% | 14 | 2 |
| progress/change/upfront | 2 | 3 | -0.888 | 0.851 | 11.9% | 58.3% | 4 | 2 |
| progress/change/upfront | 3 | 0 | 4.355 | 3.645 | 100.0% | 100.0% | 22 | 0 |
| progress/change/upfront | 3 | 1 | 8.889 | 3.151 | 81.0% | 97.5% | 36 | 2 |
| progress/change/upfront | 3 | 2 | -0.967 | 0.810 | 12.8% | 55.0% | 11 | 1 |
| progress/change/upfront | 3 | 3 | 0.830 | 1.337 | 30.9% | 94.0% | 39 | 0 |

## Checks and limits

Passed: per-step currency conservation; trace/row totals; paired tapes and source schedules; frozen source hashes; saved native transport, aliases and the declared behavior fixture; progress version/count/coverage consistency.

The complete settings, arm definitions, per-seed variation, grant-trigger breakdowns, source hashes and archive digests are in analysis.json.

- Primary endpoint includes every world task, holds, startup and failed paid investigations.
- Eight seed estimates use descriptive unadjusted Student t(7) intervals; two-seed pilots have no confidence intervals.
- Internal upfront fees redistribute resources; physical gross minus burned work determines group return.
- Costs are abstract; passive monitor work is priced inside the existing one-unit inspection in all four arms.
- Phase/source descriptions are evaluator-only labels and never learner inputs.
- Pure noise means source 2 in all phases; source 3 is separately reported as early noise only in phases 0-2.
- Per-source burns are attributed to their world step, not marginal causal credit.
- Progress snapshots precede current feedback. Trigger and episode counters can record an episode opening without a funded current grant.
- Point progress counts deduplicate by seed, provider source and comparison version; adjacent versions can share a block.
- Evaluator window-known counts are reconstructed from prior bought public advice and actual consequences; they do not calibrate or train the monitor.
- Numeric progress is conditional on supported predictions; interpretation coverage and open comparisons remain separately visible.
- Common adaptation is a descriptive first error-free 16-request window in phase 1, not proof of permanent mastery or a primary endpoint.
- Observer comparisons completed at the last feedback may not have a later decision read; completed and first-read counts differ.
- Paid permissions, requested permissions and row-recorded actual model updates are different quantities.
- Saved-file checks cover their declared retained-anchor/presence and paid-decision fixture, not novel external world reconnection.
