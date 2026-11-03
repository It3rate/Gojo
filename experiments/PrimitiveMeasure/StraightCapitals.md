# Straight capital atlas

Start with A and select **Trace closed walks**. A second reader follows three
existing ink spans around the upper loop. Change to X and then Joined X to see
how two strokes can have the same drawing and different connectivity.
[Open the atlas](https://gojo.inklingai.ca/encoding/explorer/?layer=25)

## Which letters are included?

The study uses ordinary straight-stroke Latin block capitals A, E, F, H, I,
K, L, M, N, T, V, W, X, Y and Z. There are 15 letter labels and 17 supplied
constructions. I has plain and barred forms. X has a projected crossing and a
joined variant. B, C, D, G, J, O, P, Q, R, S and U use curves in the selected
style. Angular substitutions for those letters would expand the roster.

## How to build each letter

Click a letter in the grid. The **How to draw** panel directly above the drawing
shows its recipe and stays visible as you inspect the ink and bits. With the Construction
selector focused, Right or Down goes forward and Left or Up goes back. The
arrows wrap between A and Z, including both versions of I and X along the way.
The inspection and ground settings carry over as you compare letters.

Read these directions with the letter upright. A point *partway along* a line
can be anywhere between its ends. The line continues through that point. Two
lines that *share* a point meet at the same ink site. The lengths, exact
slopes and spacing can vary. These recipes describe how the parts fit
together. The writing history separately records when the pen lifts.

- **A** From one shared tip, draw a leg down-left and another down-right. Join a point partway down each leg with a horizontal bar. The bar closes the triangle above it.
- **E** Draw a vertical spine. Extend a horizontal arm to the right from its top, another from partway down, and a third from its bottom.
- **F** Draw a vertical spine. Extend two horizontal arms to the right, one from the top and one from partway down. Leave the bottom end free.
- **H** Draw two separate vertical legs. Join a point partway down the left leg to a point partway down the right with a horizontal bar. Both legs continue past the bar.
- **I** Draw one straight vertical stroke from top to bottom.
- **Barred I** Draw two horizontal bars, one above the other. Join an interior point on each with a vertical stem. Both bars extend to either side of the stem.
- **K** Draw a vertical spine. From the same point partway down it, send one diagonal up-right and another down-right. The spine runs through the joint.
- **L** Draw straight down. At the bottom, turn right and draw a horizontal foot.
- **M** Start at the lower left and go straight up. Go down-right to an interior valley, then up-right to the other peak. Finish straight down.
- **N** Start at the lower left and go straight up. Draw a diagonal down-right to the bottom of the other upright, then go straight up again.
- **T** Draw one horizontal bar. From a point between its ends, draw a vertical stem down. The bar continues on both sides of the joint.
- **V** Start at the upper left. Go down-right to one low point, then up-right from that same point.
- **W** Start at the upper left. Go down-right, up-right, down-right and up-right. This makes two valleys on the bottom with a peak between them.
- **X** Draw a diagonal from upper left to lower right. Cross it with one from lower left to upper right. Each keeps its own point at the crossing, so you can only follow the same diagonal through.
- **Joined X** Draw the same two crossing diagonals. Give them one shared point where they cross. You can now trace a path from either diagonal onto the other.
- **Y** From the upper left and upper right, draw two diagonals down to one shared point. From that joint, draw a vertical stem down.
- **Z** Start at the upper left and go horizontally right. Go diagonally down-left to the bottom, then horizontally right again.

## The orientation agreement

Every new example starts with the same letter convention. Two ordered
construction spans share a local origin. The first span goes right, and the
second goes up in a supplied perpendicular direction. The origin is where
the leftmost horizontal projection and the lowest vertical projection meet.
For V those two projections come from different ink sites. There need be no
ink at that origin. Each letter has its own zero in this shared convention.

The ink supplies two kinds of correspondence to that frame.

1. Each authored run reports whether its horizontal and vertical projections
   increase, stay constant or decrease along its selected traversal.
2. A row [site,axis] says that the site's projection on this axis is zero.
   An X contact identifies the left edge and a Y contact the bottom.

This gives “positive right” an operational meaning. N's outer runs go up.
Z's outer runs go right. Moving the origin alone would leave that distinction
unsettled. The run observations supply it. The experiment assumes this
orientation and these observations at construction. A future page reader
could try to establish them from neighbouring glyphs. No lengths, heights,
pixel samples or trained classifier enter the matcher.

## Results across the complete roster

The bracketed site references below follow the ink along each straight run.
Shared references are actual junctions. Unbound results use this incidence,
run order up to reversal, and any explicitly supplied projected crossings.
The default column adds the conventional frame. Matching checks the same
site/run correspondence against the directional evidence, including reversed
run use. Letter labels are attached to the reference models for reporting.

| Construction | Runs | Compatible unbound | Compatible by default |
| --- | --- | --- | --- |
| A | [0,1,2] [0,3,4] [1,3] | A | A |
| E | [0,1,2] [0,3] [1,4] [2,5] | E | E |
| F | [0,1,2] [0,3] [1,4] | F | F |
| H | [0,1,2] [3,4,5] [1,4] | H, I | H |
| I | [0,1] | I | I |
| Barred I | [0,1,2] [3,4,5] [1,4] | H, I | I |
| K | [0,1,2] [1,3] [1,4] | K | K |
| L | [0,1] [1,2] | L, V | L |
| M | [0,1] [1,2] [2,3] [3,4] | M, W | M |
| N | [0,1] [1,2] [2,3] | N, Z | N |
| T | [0,1,2] [1,3] | T | T |
| V | [0,1] [1,2] | L, V | V |
| W | [0,1] [1,2] [2,3] [3,4] | M, W | W |
| X | [0,1,2] [3,4,5], projected pair [1,4] | X | X |
| Joined X | [0,1,2] [3,1,4] | X | X |
| Y | [0,1] [2,1] [1,3] | Y | Y |
| Z | [0,1] [1,2] [2,3] | N, Z | Z |

The enlarged roster changes what can be concluded about H. It was unique
among the earlier five examples. Adding the barred I introduces a second
construction with the same ink connections. Pen order can vary independently
for every example. These are compatibility results within a supplied catalogue
of straight runs. Inferring a decomposition from an actual drawing remains a
different experiment. [Earlier frame-free study](FrameFreeCapitals.md)

## Joints, turns and crossings

At a site the reader counts run ends and through runs. The number of incident
arms is ends + 2 × through. T gives three arms from one through run and one
end. Y gives three ends. K gives four arms from one through run and two ends.
Joined X gives four arms from two through runs. These counts describe topology;
choosing a route for a signal would require a passage law.

An ordinary X has two ink components. One crossing row associates the two
distinct interior sites 1 and 4 at a common projection. No material shortcut
between strands follows. In Joined X both runs contain site 1 and have a single
connected component. The matcher reads the projected pair from the supplied
table; it never discovers a connection from coincident screen coordinates.

A two-arm joint between distinct runs can also prove a corner. The two signed
projection patterns must differ even after one is reversed for the coarse
reader to rule out collinearity. This proves the bend of L or the apex of V.
A site where three or more arms meet is reported as a branch separately.
With weaker sign evidence, the angle can stay open.

## A loop as a higher-layer read

The existing graph read counts independent cycles. The added surface also
returns a walk through the participating span occurrences. For fully ordered
runs the reader grows a forest of ink spans. Each edge whose endpoints are
already connected supplies one independent cycle together with the return
path through the forest. A witness row records [run, span slot, reversed].
The owner keeps the original runs and their tail spans.

| A loop use | Original span | Read |
| --- | --- | --- |
| [2,0,0] | crossbar run 2, slot 0 | 1 → 3 |
| [1,0,1] | right leg run 1, slot 0 | 3 → 0 |
| [0,0,0] | left leg run 0, slot 0 | 0 → 1 |

The read closes at the same reference it started from. The packed witness is
`42 04 00` with two bits per field. Its positive-context presence port returns
support `02`. A test connects that port directly to an ordinary controlled
passage. Giving one crossbar end an independent site removes closure and
returns support `01`, which holds the passage. The loop-only read admits A
alone in this 15-label roster. This conclusion depends on the reference set.
For a partial order with admissible completions the cycle rank remains
readable, while exact walk and joint witnesses require settled orders. A
contradictory run supplies no admitted loop observation.

## Exact frame bytes

The frame rows [0,1] and [0,2] use two-bit construction references and pack
as `84`. For each run the signed X and Y changes occupy two bits apiece. O is
fixed by the frame. The low bit is V and the next bit is D. Values 00, 01
and 11 mean unchanged, positive nonzero and negative nonzero. Reversing a
run flips D for each participating component. The zero convention stays 00.

| Ink | X/Y observations in run order | Motion bytes | Zero contact bytes |
| --- | --- | --- | --- |
| A | (−,−), (+,−), (+,0) | `DF 01` | `82 C2 00` |
| N | (0,+), (+,−), (0,+) | `D4 04` | `10 64` |
| Z | (+,0), (−,−), (+,0) | `F1 01` | `20 76` |

Zero contact rows use the local ink site's reference width for both fields.
The separate X crossing uses two three-bit references [1,4], byte `21`.
In the atlas, the large diagram, miniature roster and dashed construction
arrows are example realizations. A check independently verifies that every
illustrated span and every zero contact honours its supplied qualitative
encoding.

~~~powershell
dotnet run --project experiments/PrimitiveMeasure -c Release -- --straight-capitals
dotnet test Tests/PrimitiveMeasure.Tests -c Release --filter FullyQualifiedName~StraightCapitalTests
~~~
