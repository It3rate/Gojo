# Does a frame changed by drawing help on the next drawing?

This is a prospective follow-up to the path campaign. In the primary exhibits, a few machines changed one of their own frame bits while drawing. We will retain only the resulting program bytes and ask whether that small physical change helps when a new episode begins. All other state is reset, including the cursor, published endpoints and clock. The test selects no finalist or warmup for having written a bit.

## Cohort and comparisons

Use every full-judge finalist (five briefs × four independent searches), selected and locked before the primary blind assessment. Each gets the same fixed warmup from the primary training corpus: A=2, B=1, pitch=6, four bays, initial phase=1, seed pulse present. The native runtime executes one live warmup per finalist. Its output bytes are the carried program for every reserve parent. There is no chain of reserve-to-reserve updates. The policy thus cannot learn a reserve goal or observe its evaluator. All four arms are evaluated on each of the two registered realizations of all twelve reserve parents.

| Arm | Starting bits | During reserve drawing |
| --- | --- | --- |
| Fresh | Original finalist | Local writes enabled |
| Carry | Final bytes from the fixed warmup | Local writes enabled |
| Frozen | Original finalist | Local frame writes disabled |
| Blind flip | Original finalist, invert the vertical-frame bit of every owner with a nonzero write-law nibble | Local writes enabled |

Blind flip deliberately ignores what the warmup would actually do. If it creates exactly the same starting bytes as carry, downstream outputs should agree completely. No write law, no warmup change, and no visible consequence of a changed bit are retained outcomes. Use the private external judge and criterion weights already frozen for that finalist's brief. Scores retain the original viability rule and 0–1 scale. Also compare the union of collinear ink spans; differences in site identity or event count alone do not establish a visible difference.

## Registered predictions

1. **R1, material persistence.** At least one of the 20 finalists will have different inherited and carried bytes, and its carried ink will differ from fresh ink on at least one of the two forms of six or more reserve parents. Otherwise the prediction fails. Report changed bytes, changed ink and ink-identical runs separately.
2. **R2, pooled utility.** Predict equivalence of carry and fresh. For each of twelve parents, average paired carry-minus-fresh utility across both forms and all 20 finalists. Support requires eight parents within ±0.02 utility. Eight at or above +0.02 or at or below −0.02 are respectively favorable or unfavorable contrary evidence (equivalence takes precedence at an exact margin).
3. **R3, specificity of the retained change.** Predict carry is better than blindly flipping writable frames, averaging over the finalists for which carried bytes differ from **both** fresh and blind flip. At least eight of twelve parent means must increase by 0.02; an equally large reliable loss is contrary. A population with no distinct such finalist supplies no information for this prediction. The eligible set is fixed entirely by a training warmup, before opening the reserve.
4. **R4, writes during the new episode.** Predict carry beats frozen by the same directional criterion among warmup-changed finalists, or unavailable if there are none. This joint contrast permits both carried state and further local adjustments. The carry–fresh contrast isolates the inherited bytes, and fresh–frozen isolates writes during the new episode. Report all three.

The twelve reserve parents are common to all finalists. Repeated runs, phases, and search replicates are dependent evidence. Warmup failures, reversals and individuals for which both byte states yield the same ink remain in the raw case table. These tests concern a single bounded writable bit per owner that can change its projected basis. They do not imply discovery of a new instruction, external human preference, a programmable learning rule, or a generally improving self-modifying generator.
