# When two rails meet

**Can a comparison open a valve before anyone measures a length?** Imagine two paper strips on a straight guide. Each carries a pencil line from a start mark to a tip. Nearby, a Y-shaped collector holds two arrivals. A receiver can draw one arrival if the lines settle its question about their lengths. The paper strips supply the evidence. The Y supplies what moves. [Open layer 32](https://gojo.inklingai.ca/encoding/explorer/?layer=32)

Choose **Across a latch**, **Feet meet** and **B is longer**, then press **Next request**. Only the starts line up, so either line could be longer and the receiver waits. Change **Feet meet** to **Two contacts**. Think of the second socket as a clip across both strips of paper. A's pencil line ends before the clip and B's goes past it. Now press **Next request** again. B must be longer, so one arrival moves into the inbox. Changing a dropdown starts that example over.

~~~text
             foot                         latch
A             o -------- tip A ------------ o
              |                             |      contact sockets
B             o --------------------------- o ----- tip B
~~~

For the uncertain case, keep **Two contacts** and choose **Unknown A order**. The lines start together and end at two different stops. The receiver can draw under **Either is longer**, since a tie is ruled out. Under **A is longer** it waits, since either line could be the longer one.

The lengths of the dashes are illustrative. The construction assumes straight rails held on a common line, distinct projected positions for distinct named stops within one owner, and closed occupied spans. A positive extension counts as greater extent. The direction of B relative to A and the size of every gap start unspecified. In loose space a single physical touch would also allow the rails to pivot.

## How the reference forms

Each rail exposes locally numbered stop ports and an order read over them. It can keep other stops private. A socket has an A half and a B half. Each side can seat or lift its own stop. Two seated halves in the same socket witness a common projected position. Two occupied halves in different sockets leave the reference open. The owners keep separate sites even when their marks coincide.

| Evidence | What the dock can establish |
| --- | --- |
| Empty sockets or only one seated half | No cross-rail read |
| Both feet in one socket | Coincident feet. Matching and reversed orientations remain possible. |
| A second socket repeats the feet | The same correspondence is witnessed twice. Orientation stays open. |
| Two distinct matches with known order on both rails | The relative orientation follows from those orders. Unmatched stops within the same bracket can still fall in either order or coincide. |
| The sockets force incompatible strict orders or match one stop to two distinct stops | No consistent placement. The occupied seats remain inspectable. Lifting a contact can restore a read. |

For **Unknown A order**, A says that two distinct stops follow its start but supplies no order between them. The second socket seats B's tip at one of those stops. A's tip ends at the other. Exactly one span is longer. A neighbor asking *which one?* gets an open answer. A neighbor asking *is there a unique longer span?* gets yes. Here the contacts and distinct-stop promise produce the exclusive choice that layer 31 supplied directly as a mask.

## Encoding chart

All byte values below are hexadecimal. The public names A0 and B0 belong to different owners. The connector uses their public port positions. The private site labels in the explorer are deliberately permuted.

| Surface | Rows or fields | Bytes in the two-contact example |
| --- | --- | --- |
| Physical sockets | Two three-bit seat codes per row. Zero means open. A seated code is the public port index plus one. | Feet alone give `[1,1] [0,0]` → `09 00`. Adding the latches gives `[1,1] [3,3]` → `C9 06`. |
| Occupied spans | On A and B independently, two-bit public start and tip fields. | `[0,1] [0,1]` → `44`. |
| Possible orientations | Bit 0 follows B's local order, bit 1 reverses it in A's frame. | Feet alone `03`. Across the latch `01`. With B reversed `02`. |
| Extent comparison | Pair roles are “A at least as long as B” and its converse. The four support bits admit neither, A longer, B longer, equal. | Across the latch `04`. Inside the bracket `0E`. With distinct unordered ends `06`. With matched ends and feet `08`. |
| Longer-span links | The same pair passes through four two-bit fields. A strict winner keeps its link and a tie clears both. The candidates are `[3,0]` and `[3,1]` in the link owner's scope. | Map `24`, link rows `73`. With unknown A order the published link support is `06`. |
| Receiver's request | A question about the complete pair drives a read into the receiver's block bit. The gate opens on a resolved yes. | Question `06` accepts either strict winner. A held sample is valid only after a fresh successful capture for this request. |

### What actually moves

The private Y slots hold two whole arrivals of unspecified magnitude. The public stem reports presence. A live comparison can authorize drawing one arrival through that coarse surface. Source priority is left first. The receiver has a block bit, an inbox and a shelf. Each step reads the same connections.

| Eligible request | Y private store | Receiver store | Public Y |
| --- | --- | --- | --- |
| Before requesting | `14` | `01` | Present |
| First request | `10` | `02` | Present |
| Retry with the inbox full | `10` | `02` | Present |
| Park the inbox on the shelf, then request | `00` | `06` | Empty |

A resolved no closes the gate and the transfer rows witness a hold. An ambiguous or unsupported read defers settlement before either row runs. The live test lifts a contact after the first delivery and parks that arrival. Although the last sampled gate was open and the inbox is now empty, the remaining Y arrival stays put. A later contact can authorize its delivery. Changing the scope of a read never retracts an earlier delivery.

## Four checks on the pre-numeric encoding

| Capability | Executable check |
| --- | --- |
| Compare unmeasured extents | Test shared and shifted starts, opposite traversal, containment, overlapping spans, single-position intersection, disjoint spans and points. Knowing which tip is later can still leave both length orders and equality open. |
| Keep an unresolved choice connected | Two distinct tips beyond a shared start can give joint support `06`. A subsequent witness on the other rail can order them. Repeating one contact cannot supply that witness. |
| Separate observation from material draw | Two observations can report presence. Only accepted whole arrivals leave the Y. Occupied capacity, a false guard and a missing current proof have different witnesses. |
| Use a whole relation at the next level | A link owner consumes the live span comparison; the receiver consumes the link owner's public predicate. Neither needs access to the rails or Y's private slots. The existing connections continue to work when a seat is lifted and reseated. |

The dock only asks about stops needed for current contacts and the four selected ends. For each relative orientation it follows their strict comparisons, including paths that alternate between owners through contacts. It checks the resulting constraints against the 75 possible ordinal arrangements of four labeled ends, including coincidences. Each admitted arrangement retains its orientation and four two-bit ordinal ranks as read evidence. Ranks carry no gap sizes. Where each nonzero span has a positive part outside the other, their unmeasured extents can compare either way or equally.

This completes the four encoding checks for the stated domain. The joint port preserves the choice across the boundary. A peer given only two separate “maybe” samples would need a correspondence to recover it. The experimenter supplies actual seating events, local order evidence and the choice of guard. Discovery of contacts in an image, a length calibration, curved or folded rails, and fractional withdrawal of a pulse each require further evidence or laws.

Run the rail checks with `dotnet test Tests/PrimitiveMeasure.Tests --filter FullyQualifiedName~RailContactTests`. The independent small-case check enumerates two local orders interleaved on one line and allows stops from different rails to coincide. [Earlier paired-read experiment](PairedReach.md) · [Y settlement and memory](WritingLine.md#reading-remembering-and-drawing-from-the-stem)
