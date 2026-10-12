# Primitive agency: receiver buckets and steering interpretation

Twelve hand-designed native calibrations ran for 600 ticks through Computed, native Lookup, and captured Packed backends. This is a bounded manual comparison, not learning. The previous default experiment and its output folder remain separate.

## Fixed world and controller

The 31-cell world, relocation every160 ticks, initial position15/charge60/heat0, physiological buckets, round2 memory/heat/rest rules, temporal trend, and one-charge-unit spatial-query fee are identical. All receiver instruments encounter the same physical light field; only their declared four-region local quantization changes. No fine light coordinate enters the brain.

Strict0 retains negative, exact zero, and positive regions. Gentle4 and broad10 split each sign into near/far regions while preserving a separate exact-zero region. A higher-priority native condition admits the union of near-negative/zero/near-positive only when the entire exposed difference support is contained there: seeking stays and cooling continues its remembered heading. Whole-sign conditions follow. This interprets conservative interval arithmetic; it does not subtract bucket ranks or silently pick a sign.

The paid comparator remains ordinal. It publishes both near/far negative regions or both near/far positive regions, and exact zero alone. It cannot establish a small nonzero magnitude. Original coarse support and the unresolved initial action witness remain in the native snapshot after a query.

## Discovery: 600-tick window


| Trial | Backend | Ticks | Cycles | Starvation | Overheat | Mean charge | Moves | Multi-region gradients | First actions unresolved | Queries |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| discovery/baseline_strict0 | Computed | 600 | 15 | 0 | 0 | 60.60 | 213 | 278 | 219 | 219 |
| discovery/baseline_gentle4 | Computed | 600 | 15 | 0 | 0 | 60.60 | 213 | 600 | 219 | 219 |
| discovery/baseline_broad10 | Computed | 600 | 15 | 0 | 0 | 60.60 | 213 | 600 | 219 | 219 |
| discovery/uniform_strict0 | Computed | 600 | 14 | 10 | 0 | 56.59 | 231 | 414 | 263 | 263 |
| discovery/uniform_gentle4 | Computed | 600 | 14 | 10 | 0 | 56.59 | 231 | 600 | 263 | 263 |
| discovery/uniform_broad10 | Computed | 600 | 14 | 10 | 0 | 56.59 | 231 | 600 | 263 | 263 |
| discovery/dimfocused_strict0 | Computed | 600 | 16 | 6 | 0 | 57.92 | 227 | 573 | 292 | 292 |
| discovery/dimfocused_gentle4 | Computed | 600 | 16 | 6 | 0 | 57.92 | 227 | 600 | 292 | 292 |
| discovery/dimfocused_broad10 | Computed | 600 | 16 | 6 | 0 | 57.92 | 227 | 600 | 292 | 292 |
| discovery/brightfocused_strict0 | Computed | 600 | 14 | 2 | 0 | 60.77 | 205 | 205 | 159 | 159 |
| discovery/brightfocused_gentle4 | Computed | 600 | 14 | 2 | 0 | 60.77 | 205 | 600 | 159 | 159 |
| discovery/brightfocused_broad10 | Computed | 600 | 12 | 12 | 0 | 59.64 | 212 | 600 | 102 | 102 |

Multi-region gradients and unresolved first actions are distinct: several sign-consistent regions can establish a motion, and a fully supported near-group can establish a hold. Neither requires resolving its exact member.

Selection: **baseline_strict0**. Require zero starvation, zero overheat, and at least the reference cycle count. Minimize queries, then prefer higher mean charge, then more cycles. On a complete tie keep the existing strict reference before comparing variant ids. Reference cycles: 15. Qualifying calibrations: baseline_strict0, baseline_broad10, baseline_gentle4.

## 2400-tick validation and no-query ablation

Longer validation in the same periodic world; this is not a held-out world.

| Trial | Backend | Ticks | Cycles | Starvation | Overheat | Mean charge | Moves | Multi-region gradients | First actions unresolved | Queries |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| reference/baseline_strict0 | Computed | 2400 | 60 | 0 | 0 | 60.67 | 812 | 1094 | 845 | 845 |
| selected/baseline_strict0 | Computed | 2400 | 60 | 0 | 0 | 60.67 | 812 | 1094 | 845 | 845 |
| reference/baseline_strict0 | Lookup | 2400 | 60 | 0 | 0 | 60.67 | 812 | 1094 | 845 | 845 |
| selected/baseline_strict0 | Lookup | 2400 | 60 | 0 | 0 | 60.67 | 812 | 1094 | 845 | 845 |
| reference/baseline_strict0 | Packed | 2400 | 60 | 0 | 0 | 60.67 | 812 | 1094 | 845 | 845 |
| selected/baseline_strict0 | Packed | 2400 | 60 | 0 | 0 | 60.67 | 812 | 1094 | 845 | 845 |
| no_query/baseline_strict0/no_query | Packed | 2400 | 33 | 334 | 0 | 48.18 | 970 | 1470 | 1206 | 0 |

The selected no-query ablation keeps its receiver partition, interpretation, memory, heat/rest policy, and temporal fallback while setting the native refinement gate to zero. There was no further search or controller/world tuning after selection.

All matched trajectories and all3072 input-mask pairs agree across the three adapters: **True**. Each calibration checks all256 pairs, including empty/open inputs. Native archives are decoded to verify memory, all chosen light/output regions, and ordered action rules. The final snapshot retains original arithmetic support and any actual comparator publication.

## Measured arithmetic adapters

| Calibration | Computed ns/read | Native lookup ns/read | Packed ns/read | Computed bytes/read | Lookup bytes/read | Packed bytes/read |
|---|---:|---:|---:|---:|---:|---:|
| baseline_strict0 | 12640 | 27877 | 8196 | 27067 | 47044 | 14167 |
| baseline_gentle4 | 14624 | 32891 | 8470 | 30032 | 55381 | 15539 |
| baseline_broad10 | 14389 | 36189 | 8949 | 30032 | 55381 | 15539 |
| uniform_strict0 | 13906 | 32877 | 9880 | 27067 | 47044 | 14167 |
| uniform_gentle4 | 13481 | 38273 | 8493 | 30032 | 55381 | 15539 |
| uniform_broad10 | 13683 | 32186 | 8118 | 30032 | 55381 | 15539 |
| dimfocused_strict0 | 12612 | 26496 | 7634 | 27067 | 47044 | 14167 |
| dimfocused_gentle4 | 14328 | 32700 | 8530 | 30032 | 55381 | 15539 |
| dimfocused_broad10 | 13941 | 31046 | 8335 | 29899 | 54314 | 15475 |
| brightfocused_strict0 | 12035 | 27226 | 7615 | 27067 | 47044 | 14167 |
| brightfocused_gentle4 | 14026 | 31653 | 8372 | 30032 | 55381 | 15539 |
| brightfocused_broad10 | 14325 | 32704 | 8753 | 29977 | 54429 | 15539 |

Three warmed rotating passes per calibration use identical native publication sequences. Native inspection/witness allocation is included; construction/capture is separate. These short process-local measurements are exploratory. Lookup revalidates native rows; Packed derives masks from a captured native contract and still publishes native witnesses. Broader output partitions can allocate more support without improving behavior.

`totalOperations` counts publications, native condition checks/projections, memory writes, coarse reads, and queries rather than CPU instructions. Trial loop timers exclude construction, archive readback, and file output. JSON and replay data are display projections; the `.gojo` archives retain the chosen native partitions/rules/memory. Charge/cool cycles remain external outcome accounting, never world-written brain states.

## Selection tie correction and legacy replay regression

The initial alphabetical tie selected baseline_broad10 despite identical outcomes/queries. The final rule instead retains the simpler existing baseline_strict0 on a complete tie; selected validation/archives were regenerated without policy or world tuning. First-selection JSON/report and selected archives are preserved under first-selection/.

Default mode ran into a separate legacy-validation folder. All eight prior trials matched every existing physical and decision step field over 600 ticks, including rule labels and actual query supports. The new initial-action-unresolved field and process-local timing/allocation/new hashes were excluded. The original artifacts/primitive-agency folder was not modified. See legacy-regression.json for field lists.

