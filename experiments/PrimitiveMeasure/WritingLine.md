# One stem, two inputs

Give a Y two arrivals. One receiving slot accepts the first and leaves the second waiting. A switched Y accepts the selected inlet. A third construction accepts both and publishes one bit answering “is anything here?” Try **Trace passage**, then change **Connection** and repeat. The same page also lets you assign the dots of i and j and change the upper joint of a 4. [Open layer 30](https://gojo.inklingai.ca/encoding/explorer/?layer=30)

## Which law belongs to the Y?

The earlier Y sent each inlet to the same receiving slot. Its two passage rows allowed a whole handoff into an empty slot. The second pulse could still be waiting when the stem reported presence. [Earlier Y and T trials](GlyphEvidence.md)

The new **Select an inlet** construction uses the existing clear-gate passage on the left and set-gate passage on the right. A lone pulse on the unselected side waits even when the receiver is empty. If the gate is withheld and both arrive, the reader can predict that the stem will participate. Which inlet will remain occupied is unresolved.

**Collect both** gives each inlet a receiving slot. A single calculated port looks at those slots using the existing presence law. In the common positive context its truth table is OR. Here 1 means participating at this resolution. The exact amount remains unmeasured.

| Left arrival | Right arrival | Stem after collection | Private store before → after |
|---:|---:|---:|---|
| 0 | 0 | 0 | `00 → 00` |
| 1 | 0 | 1 | `01 → 04` |
| 0 | 1 | 1 | `02 → 10` |
| 1 | 1 | 1 | `03 → 14` |

The five private sites are left inlet 0, right inlet 1, first receiver 2, gate 3, second receiver 4. The two transfer rows are `[0,3,2]` and `[1,3,4]`. At rest after both arrive, sites 2 and 4 are occupied and both inlets are empty. In the read table the two imported receivers have endpoint addresses 0 and 2. Its calculated output is endpoint 1. The row `[0,2,1]` packs as `18`. An outlet binding `[2,1]` (byte `06`) publishes that calculated endpoint on the Y's stem arm. Repeatedly reading it leaves the store at `14`. If the first receiver is already occupied, another left arrival stays at its inlet while the right can still pass.

| Two arrivals | Material state after one beat | Public stem |
|---|---|---|
| One receiving slot, left row first | `06` · right waits | Present |
| Switch selects right | `0D` · gate set, left waits | Present |
| Two receiving slots with OR | `14` · both received | Present |

The same ink arms participate in all three. The transition bitmap constrains legal changes of state. The read bitmap constrains what a receiver can conclude about the occupied slots. Retaining the two contributions is one implementation of a coarse OR output. An irreversible fusion, or the response of a downstream device that draws from that output, would require its own accounting.

## i and j share a writing line

Select **i and j**, then **Right stem**. In this hand i ends on the writing line and j descends below it. Select **Hide baseline**. Both models fit the published dot and the coarse stem. Switch to **Dots and hook**. The turned end supplies independent evidence for j while the baseline stays withheld.

| Page structure | Encoding | Meaning |
|---|---|---|
| Stem spans | `[0,1]`, `[2,3]` | Two separate strokes, each directed from head to foot |
| Point marks | `[4]`, `[5]` | Each dot is one participating point |
| Upright level order | `[3,1,0,2]` | Descender, baseline, body height, dot level |
| Writing frame | `[1,0]` | Shared baseline through body height |
| Level of each site, in site order | `[0,1,0,3,2,2]` | The two heads agree in height. One foot lands on the baseline, the other below |
| Ordinary dot membership | `[0,4]`, `[2,5]` | Each stem head groups with a separate dot |
| Swapped membership | `[0,5]`, `[2,4]` | The marks and their projections stay in place while each assignment changes |

Choose **Right unassigned**. The right dot is still visibly above the right stem. Reading the owner's complete assignments now rules out both *dotted* models for that stem. **Hide membership** and j becomes a possible completion again. Its dot ownership stays open. Changing any assignment leaves the two strokes and two isolated dots as four ink components. The dotted links in the picture are group memberships. Heights are ordered landmarks with illustrative screen spacings. The reader receives only the selected observations, even when the overview shows both glyphs.

## Two forms of 4

In both hands a slant descends to an elbow. A horizontal bar runs from that elbow through the upright, and the upright continues to the foot. The upper ends can share a site or remain separate. Both references have the supplied label 4.

| Hand | Slant | Bar | Upright | Independent loops |
|---|---|---|---|---:|
| Open | `[0,1]` | `[1,2,3]` | `[4,2,5]` | 0 |
| Closed | `[4,1]` | `[1,2,3]` | `[4,2,5]` | 1 |

**Runs and upper joint** distinguishes the two constructions. **Hide upper joint** gives that slant tip its own reference in the comparison view of each owner. Both now present identical ordered rows and directional evidence. Both remain compatible and agree on the digit label. The closed owner still has its original witnessed walk. **Only loop count** admits A alongside the closed 4, or T alongside the open 4, within this four-model trial. Closure distinguishes the two hands, yet cannot identify the digit by itself.

All three experiments start with authored structures. The writing line, the dot memberships and the family label for the two 4s are supplied. Inferring those relations from unfamiliar handwriting remains a separate experiment.
