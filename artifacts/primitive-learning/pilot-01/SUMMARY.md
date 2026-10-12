# Primitive learning pilot

1 seeds; 640 training + 256 held-out episodes per run; 55.9 seconds.

Test phases freeze learned ledgers. Query fees are debited; comparison work and training audits are reported separately.

| Experiment | Variant | Condition | Mean return | Correct | Coverage | Queries |
|---|---|---|---:|---:|---:|---:|
| distinctions | learned | baseline | 0.000 | 100.0% | 100.0% | 0.0% |
| distinctions | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| distinctions | shuffled-feedback | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | learned | baseline | 0.000 | 71.1% | 100.0% | 0.0% |
| prediction | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | shuffled-feedback | baseline | 0.000 | 49.2% | 100.0% | 0.0% |
| queries | learned | baseline | 1.688 | 71.1% | 100.0% | 0.0% |
| queries | frozen-initial | baseline | 1.688 | 71.1% | 100.0% | 0.0% |
| queries | shuffled-feedback | baseline | 1.688 | 71.1% | 100.0% | 0.0% |
| programs | learned | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| programs | frozen-initial | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| programs | shuffled-feedback | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A0P0Q0S0 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P0Q0S0 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A0P1Q0S0 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q0S0 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A0P0Q1S0 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P0Q1S0 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A0P1Q1S0 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q1S0 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A0P0Q0S1 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P0Q0S1 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A0P1Q0S1 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q0S1 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A0P0Q1S1 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P0Q1S1 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A0P1Q1S1 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | novel-loads | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | less-persistent | 0.125 | 51.6% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | expensive-query | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | changed-fit | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | missing-and-absent | 0.312 | 52.3% | 100.0% | 0.0% |
| composite | no-communication | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| composite | inverted-fit | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | inverted-flow | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | always-query | baseline | -0.688 | 53.9% | 100.0% | 100.0% |
| composite | never-query | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | shuffled-feedback | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | neutral-hold | baseline | 0.000 | 0.0% | 100.0% | 0.0% |
| composite | neutral-left | baseline | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | neutral-alternating | baseline | 0.000 | 50.0% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | encoding-1 | 0.312 | 53.9% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | encoding-2 | 0.312 | 53.9% | 100.0% | 0.0% |

Finite episodic decisions, authored local feedback contracts, and a16-program grammar. This does not establish closed-loop survival, unrestricted synthesis or hierarchy discovery. See experiment PROTOCOL.md and LITERATURE.md.
