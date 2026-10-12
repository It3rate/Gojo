# Primitive learning campaign

4 seeds; 1536 training + 768 held-out episodes per run; 742.4 seconds.

Test phases freeze learned ledgers. Query fees are debited; comparison work and training audits are reported separately.

| Experiment | Variant | Condition | Mean return | Correct | Coverage | Queries |
|---|---|---|---:|---:|---:|---:|
| distinctions | learned | baseline | 0.000 | 100.0% | 100.0% | 0.0% |
| distinctions | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| distinctions | shuffled-feedback | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | learned | baseline | 0.000 | 73.2% | 100.0% | 0.0% |
| prediction | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | shuffled-feedback | baseline | 0.000 | 50.5% | 100.0% | 0.0% |
| queries | learned | baseline | 3.322 | 97.8% | 100.0% | 49.8% |
| queries | frozen-initial | baseline | 1.846 | 73.1% | 100.0% | 0.0% |
| queries | shuffled-feedback | baseline | 1.846 | 73.1% | 100.0% | 0.0% |
| programs | learned | baseline | 4.000 | 100.0% | 100.0% | 0.0% |
| programs | frozen-initial | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| programs | shuffled-feedback | baseline | -0.503 | 43.7% | 100.0% | 0.0% |
| composite | A0P0Q0S0 | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| composite | A1P0Q0S0 | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| composite | A0P1Q0S0 | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| composite | A1P1Q0S0 | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| composite | A0P0Q1S0 | baseline | -0.490 | 50.1% | 100.0% | 49.8% |
| composite | A1P0Q1S0 | baseline | -0.490 | 50.1% | 100.0% | 49.8% |
| composite | A0P1Q1S0 | baseline | -0.490 | 50.1% | 100.0% | 49.8% |
| composite | A1P1Q1S0 | baseline | -0.490 | 50.1% | 100.0% | 49.8% |
| composite | A0P0Q0S1 | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| composite | A1P0Q0S1 | baseline | 1.354 | 66.9% | 100.0% | 0.0% |
| composite | A0P1Q0S1 | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| composite | A1P1Q0S1 | baseline | 1.435 | 67.9% | 100.0% | 0.0% |
| composite | A0P0Q1S1 | baseline | -0.490 | 50.1% | 100.0% | 49.8% |
| composite | A1P0Q1S1 | baseline | 3.322 | 97.8% | 100.0% | 49.8% |
| composite | A0P1Q1S1 | baseline | -0.490 | 50.1% | 100.0% | 49.8% |
| composite | A1P1Q1S1 | baseline | 3.322 | 97.8% | 100.0% | 49.8% |
| composite | A1P1Q1S1 | novel-loads | 2.856 | 91.9% | 100.0% | 49.8% |
| composite | A1P1Q1S1 | less-predictable | 2.359 | 85.7% | 100.0% | 49.8% |
| composite | A1P1Q1S1 | reversed-flow-law | -0.287 | 52.6% | 100.0% | 49.8% |
| composite | A1P1Q1S1 | expensive-query | 1.330 | 97.8% | 100.0% | 49.8% |
| composite | A1P1Q1S1 | changed-fit | 2.580 | 88.5% | 100.0% | 49.8% |
| composite | A1P1Q1S1 | missing-and-absent | 2.903 | 89.5% | 100.0% | 49.8% |
| composite | no-communication | baseline | -0.498 | 0.0% | 100.0% | 49.8% |
| composite | inverted-fit | baseline | -4.318 | 2.2% | 100.0% | 49.8% |
| composite | inverted-flow | baseline | -4.318 | 2.2% | 100.0% | 49.8% |
| composite | always-query | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | never-query | baseline | 1.859 | 73.2% | 100.0% | 0.0% |
| composite | shuffled-feedback | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| composite | whole-route-credit | baseline | 2.625 | 92.2% | 100.0% | 75.0% |
| composite | simultaneous-query | baseline | 3.322 | 97.8% | 100.0% | 49.8% |
| composite | neutral-hold | baseline | 0.000 | 0.0% | 100.0% | 0.0% |
| composite | neutral-left | baseline | 0.008 | 50.1% | 100.0% | 0.0% |
| composite | neutral-alternating | baseline | 0.003 | 50.0% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | encoding-1 | 3.322 | 97.8% | 100.0% | 49.8% |
| composite | A1P1Q1S1 | encoding-2 | 3.322 | 97.8% | 100.0% | 49.8% |

Finite episodic decisions, authored local feedback contracts, and a16-program grammar. This does not establish closed-loop survival, unrestricted synthesis or hierarchy discovery. See experiment PROTOCOL.md and LITERATURE.md.
