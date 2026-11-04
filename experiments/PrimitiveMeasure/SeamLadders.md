# Two uses of a seam

In each house the roof and room meet at A and B. In the left
house, both faces use the same seam edge. In the right house the room has its
own edge between the very same sites. Add an interior landmark M through the
roof and watch what the attached room reader sees. [Open the two houses](https://gojo.inklingai.ca/encoding/explorer/?layer=20)

If the room shares the edge, it can see the new mark immediately as it
traverses the edge the other way. If it owns a separate edge, its view still
goes straight from B to A. The open-O experiment farther down asks a related
question. Can one reader close the gap in its interpretation while another
still sees the gap in the original stroke? [Try the open stroke](https://gojo.inklingai.ca/encoding/explorer/?layer=21)

~~~text
        P
       / \
      A---B      roof walks A → B → P → A
      |   |      room walks B → A → C → D → B
      C---D
~~~

Both walks run counterclockwise in the drawing. They cross their common
interface in opposite senses along its length. A house-local catalogue owns
the edges. Each face takes an ordered range of signed uses from it. Each seam
reader stays bound to its original use position as the owner adds a rung.

## Try three changes

| Control | Shared edge | Independent edges |
| --- | --- | --- |
| Cut the roof seam | Roof reads A → M → B. Room reads B → M → A. | Roof reads A → M → B. Room still reads B → A. |
| Also bind the room to M | Both readers already have M. | An explicit binding lets the room recover B → M → A. |
| Select the combined boundary | Five distinct edge references survive. | Seven edge references survive, including two coincident seam edges with opposite signs. |

M is an asserted interior landmark. Its distance from A or B is unspecified.
The rung adds a finer observation while both original A–B endpoint reads
remain available. The room binding in the second house grants correspondence
at M. The two edges still have independent state.

The whole-house boundary read adds one signed occurrence for each edge use,
grouped by **edge reference**. In the first house the seam contributes +1 and
−1 to the same entry. Both face walks and the seam itself remain intact.
In the second house those contributions go to different entries. A projection
that combined coincident lines could give the two buildings the same outline.
That would answer a different question about their geometry.

## Encoding chart

Site positions are A=0, B=1, P=2, C=3, D=4, M=5. Edge 0 is A → B. Edges 1–5
are B → P, P → A, A → C, C → D, D → B. Only the independent house has edge
6, another A → B. The house is one small owner with two face surfaces. Public
reads give the landmark sequence and a coarse flow observation. The inspector
also shows the owner's arrays. All fields start at the low bit.

| Array | Shared edge bytes | Independent edge bytes | What the fields mean |
| --- | --- | --- | --- |
| Edge rows | `48 24 60 23 03` | `48 24 60 23 83 00` | Pairs of 3-bit site references |
| Signed uses | `20 14 86 0A` | `20 D4 86 0A` | Seven 4-bit uses in face order. Use = 2 × edge position + reversal bit. |
| Face ranges | `D8 0E` | `D8 0E` | Half-open ranges [0,3) and [3,7), two 3-bit fields per range |
| Cut through roof | `28` | `28` | Row [0,5] connects roof edge 0 to landmark M |
| Optional room correspondence | empty | `06` | Row [6,0] connects room edge 6 to roof cut **row** 0 |
| Initial flow samples | `55 05` | `55 15` | Each edge has fixed O=0 and owns V followed by D |

The room's seam use changes from 1 to 13 between houses. Its reversal bit
stays set. Only the edge reference above that bit changes. Adding the cut or
its binding writes the small new row in the chart. The authored edges, uses
and existing sample words keep their bits.

## A second meter distinguishes the operations

Choose the roof meter or source control. The room meter always keeps its
frame. Each port reorients the same stored sample if its reference frame is
opposed. Flipping a meter toggles that read again. Reversing the supplied
source changes D on the roof edge.

| Selected observation | Roof word | Room sharing the sample | Room with its own sample |
| --- | --- | --- | --- |
| As wired | 001 | 111 | 111 |
| Flip roof meter | 111 | 111 | 111 |
| Reverse roof flow sample | 011 | 101 | 111 |
| Zero roof flow sample | 000 | 110 | 111 |

In the last row, 110 has V=0. O and D describe the chosen frame and its
encoding convention. The zero read alone supplies no evidence of physical
motion. The playground supplies observations directly as experimental input.
A material handoff would require its own passage, capacity and settlement.
Sharing or binding a landmark supplies no such handoff.

## An open stroke and a proposed O

Layer 21 uses the same range reader. Here the rung changes which edge uses
participate. The catalogue supplies edges A → B, B → C, C → D, D → A.
View 0 reads only the first three. View 1 includes the fourth, which spans the
gap. Closure checks every interior join and compares the two end sites.
[Open the stroke and interpretation](https://gojo.inklingai.ca/encoding/explorer/?layer=21)

| Layout | Bytes | Read |
| --- | --- | --- |
| Two windows over the signed uses | `18 08` | [0,3) and [0,4) |
| Open source → completed view | `02` | One-bit view references [0,1] |
| Closed template → gapped variant | `01` | One-bit view references [1,0] |

Starting from the open stroke, D → A belongs to a proposed completion. The
observed walk still ends at D. Starting from a supplied closed template, the
derived variant omits the final interval. Its gap is an authored omission.
Switching rung direction preserves both underlying windows. The renderer
illustrates curved strokes. Recognition of a letter and selection of a
physically allowed bridge would each need further evidence.

## Fragmentation as a cue for attention

Repeated splits, moves and retargeting may leave many live ranges or retired
slots. They carry different evidence. A heavily revised path may deserve a
look at whether two alternatives should remain as rungs, or whether one
frequently reused read has become stable enough to crystallize. A large
number of ranges can also describe a perfectly stable branched structure.
Retired slots can reflect earlier layout changes after the uncertainty has
already settled. For a future experiment, compare local edit history and
reread cost with the cost of keeping those alternatives available. Preserve
each live port's correspondence during compaction. This pass records the
hypothesis and supplies no automatic compaction or confidence policy.

## Reproduce the comparison

~~~powershell
dotnet run --project experiments/PrimitiveMeasure -c Release -- --seam-ladders
dotnet test Tests/PrimitiveMeasure.Tests -c Release --filter FullyQualifiedName~HouseSeamTests
~~~

The tests cover face closure, signed cancellation by edge identity, retained
port reads across a cut, scoped landmark correspondence, meter reversal, a
changed sample, zero participation and both directions of the stroke rung.
