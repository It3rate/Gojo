# Primitive learning campaign

8 seeds; 1536 training + 768 held-out episodes per run; 1369.3 seconds.

Test phases freeze learned ledgers. Query fees are debited; comparison work and training audits are reported separately.

| Experiment | Variant | Condition | Mean return | Correct | Coverage | Queries |
|---|---|---|---:|---:|---:|---:|
| distinctions | learned | baseline | 0.000 | 100.0% | 100.0% | 0.0% |
| distinctions | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| distinctions | shuffled-feedback | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | learned | baseline | 0.000 | 67.6% | 90.2% | 0.0% |
| prediction | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | shuffled-feedback | baseline | 0.000 | 41.2% | 91.0% | 0.0% |
| queries | learned | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| queries | frozen-initial | baseline | -1.805 | 27.4% | 100.0% | 0.0% |
| queries | shuffled-feedback | baseline | -1.805 | 27.4% | 100.0% | 0.0% |
| programs | learned | baseline | 4.000 | 100.0% | 100.0% | 0.0% |
| programs | frozen-initial | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| programs | shuffled-feedback | baseline | 0.757 | 59.5% | 100.0% | 0.0% |
| composite | A0P0Q0S0 | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| composite | A1P0Q0S0 | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| composite | A0P1Q0S0 | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| composite | A1P1Q0S0 | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| composite | A0P0Q1S0 | baseline | -0.993 | 50.1% | 100.0% | 100.0% |
| composite | A1P0Q1S0 | baseline | -0.993 | 50.1% | 100.0% | 100.0% |
| composite | A0P1Q1S0 | baseline | -0.552 | 50.1% | 100.0% | 55.9% |
| composite | A1P1Q1S0 | baseline | -0.552 | 50.1% | 100.0% | 55.9% |
| composite | A0P0Q0S1 | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| composite | A1P0Q0S1 | baseline | 1.432 | 67.9% | 100.0% | 0.0% |
| composite | A0P1Q0S1 | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| composite | A1P1Q0S1 | baseline | 1.419 | 67.7% | 100.0% | 0.0% |
| composite | A0P0Q1S1 | baseline | -0.993 | 50.1% | 100.0% | 100.0% |
| composite | A1P0Q1S1 | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | A0P1Q1S1 | baseline | -0.552 | 50.1% | 100.0% | 55.9% |
| composite | A1P1Q1S1 | baseline | 3.258 | 97.7% | 100.0% | 55.9% |
| composite | A1P1Q1S1 | novel-loads | 2.799 | 92.0% | 100.0% | 55.9% |
| composite | A1P1Q1S1 | less-predictable | 2.361 | 86.5% | 100.0% | 55.9% |
| composite | A1P1Q1S1 | reversed-flow-law | 0.081 | 58.0% | 100.0% | 55.9% |
| composite | A1P1Q1S1 | expensive-query | 1.023 | 97.7% | 100.0% | 55.9% |
| composite | A1P1Q1S1 | changed-fit | 2.520 | 88.5% | 100.0% | 55.9% |
| composite | A1P1Q1S1 | missing-and-absent | 2.727 | 87.9% | 100.0% | 55.9% |
| composite | no-communication | baseline | -0.559 | 0.0% | 100.0% | 55.9% |
| composite | inverted-fit | baseline | -4.375 | 2.3% | 100.0% | 55.9% |
| composite | inverted-flow | baseline | -4.375 | 2.3% | 100.0% | 55.9% |
| composite | always-query | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | never-query | baseline | 1.772 | 72.2% | 100.0% | 0.0% |
| composite | shuffled-feedback | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| composite | whole-route-credit | baseline | 3.016 | 94.8% | 100.0% | 56.5% |
| composite | simultaneous-query | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | neutral-hold | baseline | 0.000 | 0.0% | 100.0% | 0.0% |
| composite | neutral-left | baseline | 0.007 | 50.1% | 100.0% | 0.0% |
| composite | neutral-alternating | baseline | -0.051 | 49.4% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | encoding-1 | 3.258 | 97.7% | 100.0% | 55.9% |
| composite | A1P1Q1S1 | encoding-2 | 3.258 | 97.7% | 100.0% | 55.9% |

Finite episodic decisions, authored local feedback contracts, and a16-program grammar. This does not establish closed-loop survival, unrestricted synthesis or hierarchy discovery. See experiment PROTOCOL.md and LITERATURE.md.
