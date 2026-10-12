# Primitive learning pilot

2 seeds; 640 training + 256 held-out episodes per run; 119.3 seconds.

Test phases freeze learned ledgers. Query fees are debited; comparison work and training audits are reported separately.

| Experiment | Variant | Condition | Mean return | Correct | Coverage | Queries |
|---|---|---|---:|---:|---:|---:|
| distinctions | learned | baseline | 0.000 | 100.0% | 100.0% | 0.0% |
| distinctions | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| distinctions | shuffled-feedback | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | learned | baseline | 0.000 | 70.5% | 100.0% | 0.0% |
| prediction | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | shuffled-feedback | baseline | 0.000 | 47.9% | 100.0% | 0.0% |
| queries | learned | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| queries | frozen-initial | baseline | -1.719 | 28.5% | 100.0% | 0.0% |
| queries | shuffled-feedback | baseline | -1.719 | 28.5% | 100.0% | 0.0% |
| programs | learned | baseline | 4.000 | 100.0% | 100.0% | 0.0% |
| programs | frozen-initial | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| programs | shuffled-feedback | baseline | -1.875 | 26.6% | 100.0% | 0.0% |
| composite | A0P0Q0S0 | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| composite | A1P0Q0S0 | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| composite | A0P1Q0S0 | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| composite | A1P1Q0S0 | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| composite | A0P0Q1S0 | baseline | -0.984 | 50.2% | 100.0% | 100.0% |
| composite | A1P0Q1S0 | baseline | -0.984 | 50.2% | 100.0% | 100.0% |
| composite | A0P1Q1S0 | baseline | -0.984 | 50.2% | 100.0% | 100.0% |
| composite | A1P1Q1S0 | baseline | -0.984 | 50.2% | 100.0% | 100.0% |
| composite | A0P0Q0S1 | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| composite | A1P0Q0S1 | baseline | 1.719 | 71.5% | 100.0% | 0.0% |
| composite | A0P1Q0S1 | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| composite | A1P1Q0S1 | baseline | 1.641 | 70.5% | 100.0% | 0.0% |
| composite | A0P0Q1S1 | baseline | -0.984 | 50.2% | 100.0% | 100.0% |
| composite | A1P0Q1S1 | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | A0P1Q1S1 | baseline | -0.984 | 50.2% | 100.0% | 100.0% |
| composite | A1P1Q1S1 | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | A1P1Q1S1 | novel-loads | 2.453 | 93.2% | 100.0% | 100.0% |
| composite | A1P1Q1S1 | less-predictable | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | A1P1Q1S1 | reversed-flow-law | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | A1P1Q1S1 | expensive-query | -1.000 | 100.0% | 100.0% | 100.0% |
| composite | A1P1Q1S1 | changed-fit | 2.203 | 90.0% | 100.0% | 100.0% |
| composite | A1P1Q1S1 | missing-and-absent | 2.641 | 91.0% | 100.0% | 100.0% |
| composite | no-communication | baseline | -1.000 | 0.0% | 100.0% | 100.0% |
| composite | inverted-fit | baseline | -5.000 | 0.0% | 100.0% | 100.0% |
| composite | inverted-flow | baseline | -5.000 | 0.0% | 100.0% | 100.0% |
| composite | always-query | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | never-query | baseline | 1.641 | 70.5% | 100.0% | 0.0% |
| composite | shuffled-feedback | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| composite | whole-route-credit | baseline | 0.936 | 64.6% | 100.0% | 23.6% |
| composite | neutral-hold | baseline | 0.000 | 0.0% | 100.0% | 0.0% |
| composite | neutral-left | baseline | 0.016 | 50.2% | 100.0% | 0.0% |
| composite | neutral-alternating | baseline | -0.234 | 47.1% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | encoding-1 | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | A1P1Q1S1 | encoding-2 | 3.000 | 100.0% | 100.0% | 100.0% |

Finite episodic decisions, authored local feedback contracts, and a16-program grammar. This does not establish closed-loop survival, unrestricted synthesis or hierarchy discovery. See experiment PROTOCOL.md and LITERATURE.md.
