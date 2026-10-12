# Primitive learning pilot

2 seeds; 640 training + 256 held-out episodes per run; 101.0 seconds.

Test phases freeze learned ledgers. Query fees are debited; comparison work and training audits are reported separately.

| Experiment | Variant | Condition | Mean return | Correct | Coverage | Queries |
|---|---|---|---:|---:|---:|---:|
| distinctions | learned | baseline | 0.000 | 100.0% | 100.0% | 0.0% |
| distinctions | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| distinctions | shuffled-feedback | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | learned | baseline | 0.000 | 72.1% | 100.0% | 0.0% |
| prediction | frozen-initial | baseline | 0.000 | 0.0% | 0.0% | 0.0% |
| prediction | shuffled-feedback | baseline | 0.000 | 37.3% | 100.0% | 0.0% |
| queries | learned | baseline | 3.174 | 99.2% | 100.0% | 76.4% |
| queries | frozen-initial | baseline | 1.688 | 71.1% | 100.0% | 0.0% |
| queries | shuffled-feedback | baseline | 1.529 | 71.9% | 100.0% | 22.1% |
| programs | learned | baseline | 4.000 | 100.0% | 100.0% | 0.0% |
| programs | frozen-initial | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| programs | shuffled-feedback | baseline | 0.000 | 50.0% | 100.0% | 0.0% |
| composite | A0P0Q0S0 | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| composite | A1P0Q0S0 | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| composite | A0P1Q0S0 | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| composite | A1P1Q0S0 | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| composite | A0P0Q1S0 | baseline | -0.592 | 52.1% | 100.0% | 76.4% |
| composite | A1P0Q1S0 | baseline | -0.592 | 52.1% | 100.0% | 76.4% |
| composite | A0P1Q1S0 | baseline | -0.592 | 52.1% | 100.0% | 76.4% |
| composite | A1P1Q1S0 | baseline | -0.592 | 52.1% | 100.0% | 76.4% |
| composite | A0P0Q0S1 | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| composite | A1P0Q0S1 | baseline | 1.688 | 71.1% | 100.0% | 0.0% |
| composite | A0P1Q0S1 | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| composite | A1P1Q0S1 | baseline | 1.250 | 65.6% | 100.0% | 0.0% |
| composite | A0P0Q1S1 | baseline | -0.592 | 52.1% | 100.0% | 76.4% |
| composite | A1P0Q1S1 | baseline | 3.174 | 99.2% | 100.0% | 76.4% |
| composite | A0P1Q1S1 | baseline | -0.592 | 52.1% | 100.0% | 76.4% |
| composite | A1P1Q1S1 | baseline | 3.174 | 99.2% | 100.0% | 76.4% |
| composite | A1P1Q1S1 | novel-loads | 2.627 | 92.4% | 100.0% | 76.4% |
| composite | A1P1Q1S1 | less-persistent | 2.611 | 92.2% | 100.0% | 76.4% |
| composite | A1P1Q1S1 | expensive-query | 1.646 | 99.2% | 100.0% | 76.4% |
| composite | A1P1Q1S1 | changed-fit | 2.377 | 89.3% | 100.0% | 76.4% |
| composite | A1P1Q1S1 | missing-and-absent | 2.803 | 90.0% | 93.4% | 76.4% |
| composite | no-communication | baseline | 0.000 | 0.0% | 0.0% | 76.4% |
| composite | inverted-fit | baseline | -4.701 | 0.8% | 100.0% | 76.4% |
| composite | inverted-flow | baseline | -4.701 | 0.8% | 100.0% | 76.4% |
| composite | always-query | baseline | 3.000 | 100.0% | 100.0% | 100.0% |
| composite | never-query | baseline | 1.766 | 72.1% | 100.0% | 0.0% |
| composite | shuffled-feedback | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| composite | neutral-hold | baseline | 0.000 | 0.0% | 100.0% | 0.0% |
| composite | neutral-left | baseline | 0.172 | 52.1% | 100.0% | 0.0% |
| composite | neutral-alternating | baseline | -0.203 | 47.5% | 100.0% | 0.0% |
| composite | A1P1Q1S1 | encoding-1 | 3.174 | 99.2% | 100.0% | 76.4% |
| composite | A1P1Q1S1 | encoding-2 | 3.174 | 99.2% | 100.0% | 76.4% |

Finite episodic decisions, authored local feedback contracts, and a16-program grammar. This does not establish closed-loop survival, unrestricted synthesis or hierarchy discovery. See experiment PROTOCOL.md and LITERATURE.md.
