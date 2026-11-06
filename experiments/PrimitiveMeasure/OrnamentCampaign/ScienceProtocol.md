# Prospective questions and independent judge

This is the design record for the first local ornament tournament. It is written before evaluating the competition. It tests whether a small packed machine learns useful structural connections across varied inputs. Its subject is a circular strip of ornament landmarks. The six strata describe constructions of rail occupancy. Recognition of a glyph, two-dimensional intersections, historical style, and judgments of beauty need additional experiments.

## Enrolment

The corpus contains 264 different parents. Each is tested under all four briefs. A parent contributes an ordinary view and a reflected view with an external perturbation, giving 2,112 episodes. The perturbation toggles one unprotected site after the first scan. It is supplied by the apparatus and has separate material accounting. Reflecting relative to the seam and perturbing are bundled in the second view. A comparison of the two views alone cannot attribute a difference to just one of those changes.

| Partition | Parents in each of the six strata | Episodes per brief | Construction changes applied to the base recipe |
| --- | ---: | ---: | --- |
| Train | 16 | 192 | Move an occupied site one step or introduce a satellite mark. |
| Validation | 8 | 96 | Relocate one mark by two steps and introduce a satellite. |
| Blind test | 12 | 144 | Remove one mark, introduce a mark in a vacant position and another one target interval away. |
| Reserve | 8 | 96 | Remove a mark and suppress its target-interval neighbor, then introduce an adjacent pair. |

The morphologies are periodic combs, adjacent doublets, a central open window between clusters, an initially reflected scaffold, repeating rising gap lengths and two interleaved periodic components. Some modification requests can coincide with existing material. Every accepted seed has four or more ink sites and at least five vacant sites. Width ranges from 22 to 36. A global width-plus-canonical-ink key rejects an exact rotated or reflected duplicate across parent IDs. The four goal rows of one parent intentionally reuse the same material seed. The held-out constructions combine new modifications within known morphological families. Generalization to an independently authored art tradition is a different claim.

Each parent supplies an uncalibrated shared lattice with a requested interval of 2 to 5 steps, 2 to 5 stock slots, and an edit-attempt budget of `4 + floor(width/4)`. A run lasts `2 × width + 3` beats. Ordinary inputs include an anchored ink site in three quarters of parent indices, one or two protected vacant sites every third parent index and one locally concealed site every fourth index beginning at 2. These categories overlap. Strictly ordinal input with no common step basis would leave these distance goals open. Hiding sensor evidence withdraws information from the machine while leaving the external judge able to inspect the physical rail.

## Goals and score

The goal bits, immutable damaged seed, accessible local observations, and available stock are execution inputs. The apparatus computes the score only after a run. Any change to a protected position, output outside the rail, material imbalance or impossible edit/stock accounting sets utility to zero and emits a violation. Initial ink plus supplied stock plus the net external change must equal final ink plus remaining stock. Edits must fit within charged attempts, the clock and the actual treatment's budget. Otherwise utility is `max(0, 1000 − error)`. Each error below is denominated in offline utility points. Scaling is rounded to nearest integer, and every brief reports zero error separately as exact settlement. Utility is an intentionally chosen ranking for this campaign, not an intrinsic quantity of the primitive system.

| Brief | Source and desideratum | Error |
| --- | --- | --- |
| Balance | Keep the initial line count and equalize its circular gaps. | Charge 180 per missing or extra ink site. For gaps g, final count n and width w, sum `abs(n × g − w)`. Subtract the unavoidable `2 × (w mod n) × (n − (w mod n))`, then scale by `1000/(2 × w × n)`. Fewer than two sites gives zero utility. |
| Infill | Fill circular intervals close to the target gap d. | Sum `abs(g − d)`, subtract the distance from w to the nearest multiple of d, then scale by `1000/w`. Fewer than two sites gives zero utility. Different allocations can have the same total gap error. |
| Mirror | Retain each supplied source mark and complete its counterpart across the fixed seam axis. | Count differences from the union of the source and its reflection across `i ↔ width − 1 − i`. Scale by `1000/size of that union`. Extra ink in the intervening ground is charged. |
| Seam | Repair the gap crossing the fixed seam between the last and first site. | Compare that circular gap to d, scaled by `1000/max(d, floor(w/3))`. Each changed site outside the first and last `d+1` positions costs 100. Fewer than two sites gives zero utility. This is a seam-spacing observation, not proof that two ink endpoints are one site. |

Edits, refusals, consumed stock and unresolved reads remain separate operational diagnostics. A conflict between a protected site and a brief can limit attainable utility. An initially solved or impossible brief remains in its group. Always compare a champion to the untouched seed and matched controls when interpreting a high absolute score.

## What each prediction tests

The frozen ledger contains 240 rows, ten questions conditioned on four briefs and six seed constructions. The independent replication unit is a generated parent family member. Training replicates, two views, and multiple brief reads do not create new independent families.

| Mechanism | Compared arms |
| --- | --- |
| Structure | Evolved structural edits versus parameter-only proposals, under matched training budgets. |
| Channel | Evolved full grammar versus retrained open-loop search. |
| Selection | Evolved full grammar versus equally funded full-grammar random proposals. |
| Cut | Same evolved program with live versus absent communication. |
| Freeze | At every site use the initial observation table instead of sensing the subsequently modified rail. |
| Flip | Same program with the wire polarity inverted. |
| Clock | Same program with the signal replaced by an alternating clock. |
| Hide | Same program with all sites visible versus all locally concealed. |
| Stock | Same program with supplied stock versus empty initial stock. Stock may subsequently be recovered by an erasing action. |
| Zero budget | With zero permitted edit attempts, live versus inverted channel must give identical material, including an external disturbance. This is an instrument control. |

Directional margins are 20 points for structure/channel, 15 for structural evolution against random, and 10 for wire and visibility changes. Stock uses 20 for a directional prediction of improvement, 10 for neutral or adverse predictions. The ledger fixes context-specific exceptions. Regular comb balance predicts simpler parameter search ahead of full evolution. Near-reflected seeds predict smaller wire and search effects for mirror completion. Frozen evidence may suffice for a localized seam. Rhythmic input can be competitive on a comb. Funding extra insertions may hurt a count-preserving regular comb.

For each cell, average the paired utility difference (left minus right) across views and matched training replicates separately for every held-out parent. At least eight distinct parents are required for support. A directional prediction needs at least two thirds of parent means to meet or exceed its signed margin. The same fraction in the opposite direction contradicts it. For an equivalence prediction, two thirds within the closed margin supports it; two thirds outside the band in one common direction contradicts it. Other cases are inconclusive. The exact zero-budget control is contradicted by any individual material or utility disagreement, even if an aggregate difference cancels it. Report this engineering control separately from the 216 substantive conditional predictions. These are predeclared decision rules, not p-values.

Freeze the seed and prediction ledgers, score, training design and executable identity before running agents. Keep all competing winners, failures and indeterminate cells. Reserve recipes are generated and locked at the same time, but remain unevaluated until a second prospective question has been frozen.
