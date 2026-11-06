# A retained frame changed the drawing, with exactly the same score

One ornamental machine changed its own frame during a warmup, then used that changed frame when asked to draw on a new carrier. On **all 24** unfamiliar realizations, its next drawing was exactly the vertical reflection of the drawing it would otherwise have made. Every one earned the same score as its unreflected counterpart. This gives us an example of persistent, mechanically effective change, and a clear reason the measured utility stayed equal.

[See the original, carried and frozen drawings on the same frame](../../artifacts/local-path-ornament/retention/comparison.svg).

## What the machine was allowed to remember

Twenty trained full-judge finalists received the same warmup from their training bank. We saved only their resulting program bytes. We then started each reserve episode with fresh sites, cursors, ports and clock. The carried program competed against its original program, its original program with frame writes disabled, and a blind flip of every writable frame. All 20 finalists advanced, including the 17 whose warmup left every byte as it began.

Each arm ran on 12 new parent calibrations, each with two realizations, making **1,920** reserve executions. The machine drew in a three-, four-, or five-bay setting. Scores use each original brief and its frozen criteria. [Prospective protocol](Retention.md) · [Common release record](../../artifacts/local-path-ornament/reserve-release.json).

## Outcomes

| Prospective question | Outcome |
| --- | --- |
| R1. Can the warmup leave a bit that changes new material? | Supported. The branched-spray finalist from replicate 3 changed the actual ink on all 12 reserve parents. Two other finalists changed bytes without changing the occupied lines. |
| R2. Will carrying the warmup leave pooled utility equivalent to starting fresh? | Supported. Every one of the 480 individual carry–fresh scores was equal. |
| R3. Is carrying a particular learned frame better than blindly flipping all writable frames? | Unavailable. In all three retained changes the blind-flip control produced identical starting bytes, so the two hypotheses cannot be discriminated on this warmup. |
| R4. Does carry plus continuing to switch beat the original frozen frame on the three changed finalists? | Inconclusive. Mean gain was 0.01568; three of twelve parent means reached 0.02. The prespecified requirement was eight. |

The changed branch-spray machine drew a reflection about Y=0 on every visibly changed case. At each of three, four and five bays, final ink changed in 8 of 160 carry/fresh pairs. The other pairs produced the same final ink. Every score was unchanged, including for all 24 reflected outputs. This is behaviorally different under orientation, while equally acceptable under the chosen utility reads. Connecting a later use that distinguishes the two sides could make this same retained bit consequential in a different way.

We copied five execution files to an empty directory and independently reproduced all 1,920 reserve results, including start/end program bytes, vector-ink fingerprints, stroke counts, switch counts and utility. The follow-up tests a deterministic one-bit local frame write. It supplies no evidence of accumulating profitable edits or improving open-ended learning.

[Raw cases](../../artifacts/local-path-ornament/retention/reserve-cases.csv) · [Parent effects](../../artifacts/local-path-ornament/retention/reserve-parents.csv) · [Verdicts](../../artifacts/local-path-ornament/retention/verdicts.json) · [Cold verification](../../artifacts/local-path-ornament/retention/verification.json)
