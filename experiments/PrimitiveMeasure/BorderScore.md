# Read a repeating border

Imagine a pen drawing **up, right, down, right** along the edge of a page. After four beats the controller is ready to repeat and the pen is back at its original height. Its tip is two horizontal moves farther along. We can prove all three facts by inspecting the score before drawing anything. [Open layer 33](https://gojo.inklingai.ca/encoding/explorer/?layer=33)

Choose **Alternating siblings** and step four beats. One stream supplies up and down, the other supplies rightward drift. Now choose **Together**. Both heads fire on every beat, and the pen samples their combined destination. The staircase becomes a pair of sloping strokes. The same components still contribute two rightward moves and return to their starting height over a joint period. The period now takes two beats.

~~~text
                           first sample   second   third   fourth
Direct or alternating          up         right    down    right
Together                    up + right   down + right
~~~

The picture supplies example lengths. A positive reach in the experiment can be any positive length consistent with its bindings. The two plane axes are a supplied geometric frame. The earlier qualitative frieze supplies the reference motion word and examples of differing causal programs. This compact version uses a cyclic clock strip to route beats to sibling streams. [Earlier frieze constructions](https://github.com/It3rate/Gojo/blob/0c0507da/Gojo/docs/GojoQualitativeFriezeExperiment.md)

## What connects to what

Two sibling arrays in one score hold movement bytes in a shared reach basis. A third array holds two-bit clock selections. Positions wrap at each array boundary. On a beat, each selected head emits its current cell and advances one place. A disabled head stays put. Both emissions on one beat retain their source and phase even when the pen draws one combined segment. These are timing signals. Separate owners would need an explicit correspondence to share a reach across that boundary.

| Bits in a movement byte | Local meaning |
| --- | --- |
| 0 through 2 | The original O/D/V word. Bit 0 enables nonzero travel, bit 1 is direction and bit 2 is orientation. Changing both O and D preserves physical sense in the supplied frame. |
| 3 | Select the horizontal or vertical carrier. |
| 4 through 7 | Select a shared reach on that carrier. Reusing the reference promises equal length. Distinct references leave equality open. |

| Construction | First stream | Second stream | Clock fields in beat order | Packed clock hex |
| --- | --- | --- | --- | --- |
| Direct square wave | `09 01 0B 01` | `00` (disabled) | first | `01` |
| Alternating siblings | `09 0B` | `01` | first, second | `09` |
| Together | `09 0B` | `01` | both | `03` |
| Opposed siblings | `09 0B` | `01 03` | alternating or together | `09` or `03` |
| Local loop alone | `09 01 0B 03` | `01` | first, first, first, first | `55` |
| Loop with drift | same | same | first, first, first, both | `D5` |

Clock field 0 waits, 1 selects the first head, 2 selects the second, and 3 selects both. Fields pack low first. The containing ring supplies its length. In the last example the local rectangle has zero displacement. On its last beat its move left coincides with its sibling's move right by the same reach. On that beat the two horizontal moves cancel. Over the full period the combined path ends one reach to the right of where it began. Returning to a phase, to a height, or to a projected position each answers a different question. A shared ink site requires its own attachment.

**Opposed · alternate** and **Opposed · together** use exactly the same vertical and horizontal streams. Alternating gives up, right, down, left. The sampled rectangle returns to its origin and its clockwise signed sweep is `−H×V`. Together gives a diagonal out and the opposite diagonal back. It also returns, with zero sweep. For this read each beat commits a straight segment between sampled positions. The sum of `x·dy − y·dx` along a closed walk gives twice its signed sweep in products of the two unknown reaches. Reordering a fixed set of additive moves can change this sweep while preserving the endpoint.

### Reading and unwinding the bits

Count how often each head is selected in one clock lap. For a ring of `n` cells selected `f` times, its head returns after `n / gcd(n,f)` clock laps. Take the first common return of both heads and the clock. In that window each completed stream lap contributes its signed uses of each reach. Opposite uses of the same reach cancel in the endpoint read. Each count says how many times an unmeasured reach participates.

**Walk backward** reads the preceding clock field, backs up the selected heads, and reverses the directed uses. Within a grouped beat the fine firings also appear in reverse order. This recovers the visited cells and sampling boundaries. A flattened diagonal drawing alone lacks the original clock wiring.

## Equal lengths as a prediction

In **Same linear carrier**, three links point right on one chain and two point right on another. With distinct positive reaches the comparison is `3A − 2B`. Either chain could be longer, or they could be equal. The equal-link view substitutes one unknown reach for both and predicts a positive difference. Choose **Shared reach** to put that correspondence in the actual wiring. Now the same conclusion follows directly.

**Separate fall** applies the test to the border. Up uses one vertical reach and down uses another. The controller's return is certain and the height change has all three signs available. Assuming equal reaches predicts a return to height. The source bytes stay available for the original read. Separate equalization on each axis is sufficient here.

## Hiding, lifting and predicting

In **Missing ink**, each slot admits some of the five marks (blank, right, up, left, down). A hidden slot admits all five. An observed blank admits one. The motor can carry the pen through a slot while it is lifted, leaving a real break in the ink. The observer sees only its selected slots.

Hide the third stroke of the wave. Five raw completions survive. Under the square-wave family only **up, right, down, right** survives, so that model can continue drawing a predicted next repeat. Switch the third stroke to **Pen lifted** while it is still hidden. The evidence and prediction agree with the previous display. Reveal all the ink and the family has no match. Nothing fills in the original blank.

The family uses all four start phases, four quarter-turns, two reflection choices (original or mirrored), and both traversal senses. These 64 structural witnesses yield 16 distinct words. A candidate uses one transform throughout its past and forecast. Different candidates remain separate even when they agree about a particular slot. Additional observations can intersect slot supports when a common phase and slot correspondence is supplied.

## A second property can ask for a branch

In **Coverage outlet**, the pen still runs one square-wave period. A second structure asks whether every part of the **horizontal projection** between its start and end is inked. A vertical stroke covers one point in that view. A rightward stroke covers a whole interval. Lift the first rightward stroke and an open interval turns white. Lift the downward stroke instead and the horizontal projection stays covered, even though the visible motif has a gap.

A separate gauge can compare the separation between two parallel lanes with the desired spacing on a common normal rail. Keep all strokes inked and switch **Lane spacing** from **At limit** to **Beyond limit**. The coverage along the travel axis still passes. The spacing test alone can now release a pulse. The two checks feed an OR read connected to the existing guarded passage. **Try side outlet** releases the waiting pulse if either defect is certain. With only a possible defect it stays put. A second request cannot spend the same pulse twice. [Earlier ordinal comparison](PairedReach.md)

This is a property-relative use of orthogonality. One connected journey can preserve phase and displacement while a spacing or ink intervention changes the coverage verdict. X and Y agreement with individual steps depends on which part of the path and which sampling rule a reader selects. The branch pressure here is a one-bit request derived from a supplied coverage requirement. The outlet is wired in advance. Growing a route, painting the uncovered region and evaluating a full two-dimensional fill remain further experiments.

### Boolean spans a neighbor can use

The rail stores ordered named stops. Its occupancy mask alternates a point and the open interval to its successor. On a four-stop rail the seven low bits refer to `P, (P,Q), Q, (Q,R), R, (R,S), S`. Boolean law bit `a + 2b` selects an output at each position. The result remains an operand on the same rail. A foreign rail requires an explicit correspondence.

| Operands on the same rail | Question | Result |
| --- | --- | --- |
| [P,R] and [Q,S] | Both, law `08` | [Q,R], mask `1C` |
| Same spans | A minus B, law `02` | [P,Q), mask `03` |
| Same spans | Either, law `0E` | [P,S], mask `7F` |
| [P,Q] and [Q,S] | Both | The single point Q, mask `04` |

For the coverage reader, certain ink is the union of witnessed ink spans. Possible ink also includes unobserved strokes. Subtract possible ink from the required span to obtain definite white. Subtract certain ink to obtain possible white. The request concerns an uncovered interval of positive extent. The present strip reader takes up to 31 beats of known forward-or-stationary horizontal motion and assumes continuous segments between sampled positions.

## The sweep

[Download the two CSV files with all 18,192 trials](data/border-trials.zip).

The motor sweep uses every assignment of four cardinal directions to four cells (two per stream), all 16 two-beat clock strips, and two reach bindings, shared by axis or distinct per cell. Each row predicts the first joint phase return and the signed coefficient of every reach. It then runs to the first observed return, compares every coefficient and unwinds every firing back to the start. A separate numeric check gives each reach independently any integer length 1 through 8 and reads attainable endpoint signs in that finite apparatus. All 8,192 rows agreed. Of these, 3,584 include a simultaneous firing. The longest first return in this particular sweep is four beats.

| Endpoint proof from positive, unmeasured reaches | Equal-link view returns | Equal-link view drifts |
| --- | ---: | ---: |
| Must return | 1,140 | 0 |
| Must drift | 0 | 6,200 |
| Open | 628 | 224 |

The ink sweep uses all 625 four-slot words over the five marks, including words outside the family. Each is read under all 16 visibility masks. “Unique wrong” means one distinct family word fits every visible slot but disagrees with the withheld ground truth.

| Hidden slots | No family match | Unique correct | Unique wrong | Multiple possible words |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 609 | 16 | 0 | 0 |
| 1 | 2,180 | 64 | 256 | 0 |
| 2 | 2,550 | 0 | 0 | 1,200 |
| 3 | 500 | 0 | 0 | 2,000 |
| 4 | 0 | 0 | 0 | 625 |

Counts describe an exhaustive, equally enumerated fixture. Dormant cells and timing variants can yield the same ink. The counts supply no natural frequency or confidence for a real drawing. Every true family member remained among the predictions under every mask.

Run either study from the repository root. Add `--rows` after the study name to emit the corresponding CSV.

~~~powershell
dotnet run --project experiments/PrimitiveMeasure -c Release -- --border-scores
dotnet run --project experiments/PrimitiveMeasure -c Release -- --border-windows
~~~

In the score CSV the four two-bit digits of `moves_hex` select right, up, left, down (low digit first). The first two digits belong to stream 0. The two two-bit digits of `gates_hex` select the clock masks. Sign support bits mean negative, zero, positive. Return classes 0, 1, 2 mean guaranteed projected return, guaranteed drift and unresolved. In the ink CSV each nibble of `ink_hex` names one of the five marks in the order above, earliest slot low. The raw completion count concerns the four-slot window. Forecasts beyond it assume continued repetition.

The [timing bench](TimingSocket.md) reuses these voice strips to test two plotters with different drive connections. It can retain a clock edit and render the resulting border across three repeats.
