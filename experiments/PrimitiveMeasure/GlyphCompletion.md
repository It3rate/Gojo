# Across a glyph gap

Two arrows can constrain a missing path. A construction can supply one. This
experiment puts those situations side by side. [Open layer 27](https://gojo.inklingai.ca/encoding/explorer/?layer=27)

To hold the stem fixed and try changing the reach of a D or B, see [Push a bowl outward](BowlReach.md).

## Try D first

Choose **D · ends only**. The left drawing shows the stem and the two headings.
Its return already connects the top to the foot in the encoding. We know the
whole ink has a loop. What remains open is how the pen travels on the return.

Choose **Steer as you travel**. In the right drawing the pen advances and turns.
Switch **Illustrated bow** between shallow and deep. Both drawings obey the
same encoded law. Choose **Two corners**. Now the pen goes right, down, and
left. Choose **Half a circle**. This proposal adds a centre, common reach and
two equal shares of a whole. All three obey the two arrows. **One straight**
keeps the same heading throughout and fails the arrival condition.

The two trial rows bind candidate departure to observed departure and
candidate arrival to observed arrival. The headings are compared in the
same supplied upright frame. The check promises only endpoint identity
pattern and headings. It does not compare outlines or imply a metric fit.
Trying a proposal leaves the original steering support at `03` (either).
The sketch supplies scale and placement for inspection.

## Move a cut in O

Choose **C from O**. One cut stays anchored. Change **Ink extent** to place
the other cut at one of seven interior stops on a radial circle with eight
equal shares. The shares are landmarks for this experiment. The original
[one-arc O](CurvedCapitals.md) can still express the whole without them.
The distance from centre to boundary remains unmeasured.

* **Ink only** exposes an open walk. Its two tips alone leave the existence
  of a retained passage unresolved.
* **Ask owner** inspects the owner's complete construction inventory. The
  owner can publish a one-bit answer. The diagram shows its construction in
  dashes so you can inspect the reason for that answer.
* **Try passage** starts at the arrival tip and follows the complement's
  actual signed uses. Success requires arrival at the original start site.
* **Reverse** reads both windows in the opposite direction. The tips swap,
  and the same retained spans are traversed in reverse order and direction.

Switch to **Open C** at the same extent. Its visible ink has exactly the same
ordered site pairs. Its owner holds only the open spans. Asking now gives a
definite absence within that owner's inventory. Withholding the reply leaves
the reader uncertain in both cases. No query or attempted traversal inks the gap.

## A concrete encoding

The table uses six visible parts in the forward walk. Each edge has a local
index. A signed use is twice the edge index plus its reversal bit.
The three views on the retained owner are half-open intervals in use order.

| Row | Stored or calculated | What a reader can follow |
| --- | --- | --- |
| Source arcs | Eight radial rows with the same share slot, 32 | Ordered cuts around one whole. The unwrapped end binds back to the reference site |
| Signed uses | `0 2 4 6 8 10 12 14`, bytes `20 64 A8 EC` | Each use traverses one source arc forward |
| Ink window | `[0,6)` | Site 1 to site 10 |
| Complement window | `[6,8)` | Use 12 goes from site 10 to 11. Use 14 goes from 11 to 1 |
| Whole window | `[0,8)` | The ink and complement together return to site 1 |
| Reversed uses | `11 9 7 5 3 1 15 13` | The ink goes from 10 to 1. The complement goes from 1 to 11 to 10 |
| Either owner, unqueried | `03` | Both answers to the return question remain possible |
| Return reply | `02` from retained O, `01` from open C | Present or absent in the supplied complete inventory |

## What the second reader learned

The existing two-choice Gojo query learner can pay for the construction
reply or use the free ink observation. It sees which question it was asked:
does the visible ink close, or can the owner traverse a retained return?
It never sees the glyph's name, its private tables, or the correct choice.
After the decision the owner walks the requested spans. A correct definite
prediction earns four ticks, an incorrect one costs four, and an open answer
earns zero. A query costs one tick. These are experiment settings.

Training used cuts at two, four and six parts. Evaluation froze the learner
and used one, three, five and seven parts, with both directions, both owners
and both questions. Each policy faced the same 32 trials.

| Policy | Correct | Open | Queries paid | Net ticks |
| --- | ---: | ---: | ---: | ---: |
| Frozen learner | 32 | 0 | 16 | 112 |
| Always ask | 32 | 0 | 32 | 96 |
| Never ask | 16 | 16 | 0 | 64 |

This demonstrates learned use of an offered observation. The two questions,
construction families and cost law were supplied. The trials are variations
of those families. Invention of a new interpolation rule and transfer to an
independent drawing remain open.

~~~powershell
dotnet run --project experiments/PrimitiveLearning -c Release -- --glyph-cuts
~~~
