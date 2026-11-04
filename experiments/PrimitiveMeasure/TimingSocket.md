# Can a test earn a socket?

A factory has two test rigs. One dispenses flat metal washers into a tray. The other draws a repeating border with two pen drives. Each has a desk with nine installed tests and one empty socket. When two machine designs still fit and its tests cannot tell them apart, the desk can ask a timing bench to try a small change. [Open layer 36](https://gojo.inklingai.ca/encoding/explorer/?layer=36)

Choose **Washer · return wire** and click **Next event**. The old rack tests the unknown until two possible designs remain. The bench moves a pulse, runs a reference machine of each design, and repeats the comparison on fresh references in the opposite order. If the same machine gives ON both times and the other gives OFF both times, the socket keeps the edited test. One more click runs it on a fresh unknown. **Run to end** takes you straight to the outcome.

Now choose **Border · drive swap**. The same timing bench compares two ways of drawing an ornament. Each row of the picture shows the same clock applied to both reference plotters. The solid strokes cover the test window. The ring marks its endpoint. The dashed continuation shows what the clock draws when it repeats twice more and both pen drives keep their phase. Only the selected sensor on the last beat of the solid portion supplies evidence to the bench.

## The washer that reveals a wire

Both remaining dispensers are loaded with two washers and serve each retry. A has a working confirmation wire. B’s wire is cut. The chute sensor reports a falling washer. Neither candidate can be eliminated by the installed chute tests.

| Test | Beat 1 | Beat 2, when the chute is sampled | A / B |
| --- | --- | --- | --- |
| Original | Ask for a washer and hold the confirmation wire shut. | Allow the confirmation through. Both desks were still waiting and asked again. | ON / ON |
| Edited | Ask for a washer and allow the confirmation through. | A’s desk has stopped asking. B’s desk still asks for its second washer. | OFF / ON |

The bench used eight beats and six washers to confirm the split. Each two-beat comparison used one washer in A and two in B. The repeated comparison used fresh stock. The desk then used its own test budget to identify the unknown. **Washer · spare stock** gives the negative control. Two dispensers that remember delivery behave alike with one or two washers loaded. Running the permitted variants leaves the socket empty and both designs possible.

## The border that reveals a drive

The wave uses two existing voice strips. One cycles **up, down**, the other repeatedly moves **right**. In A the main clock drives the vertical voice. In B it drives the horizontal voice. The other wire supplies the accent. The desk’s question in this example is whether the pen moved horizontally on the sampled beat. The desk starts with the two candidate tickets, so it can request the bench immediately.

| Timing | A’s two strokes | B’s two strokes | Horizontal motion on beat 2 |
| --- | --- | --- | --- |
| Accent on beat 2 | Up, then down-and-right | Right, then up-and-right | Yes / yes |
| Accent on beat 1 | Up-and-right, then down | Up-and-right, then right | No / yes |

The original swatches can look different to a person who sees the whole drawing. The installed horizontal sensor sees agreement. Moving the accent exposes the difference at that sensor. **Border · ink only** asks whether the pen moved at all. Both remain possible under that read. **Border · frame flip** uses opposite O/D encodings for the same physical moves. The socket cannot distinguish those encodings through the pen-motion sensors.

The eight printer designs use the existing wave, opposed sibling streams, the wave in its opposite orientation convention, and the loop with lateral drift. Each has two assignments of the motor leads. The probe supplies the clock strip during a test. Its three read surfaces are any motion, horizontal motion, and upward motion on the final beat. **Border · rewind** reverses the score traversal while delivering the command pulses in their original wall-clock order. **Border · turn and feed** begins with an early accent and can discover a useful *later* one. [How the border score works](BorderScore.md)

## Where each decision lives

| Connection | Information carried | Local effect |
| --- | --- | --- |
| Desk → bench | Two surviving reference tickets, enabled parent programs, stock protection and remaining desk time | Opens one request if the question is unresolved and no installed test on the connected sensors separates this pair. A known separating test blocked by cost or a missing reading still counts as known. |
| Parent → variant | One adjacent pair of timing contacts | Swap `01` and `10` on the same wire in an allowed direction. Preserve both pulse counts, sensor, and sample time. Deduplicate edited words and ignore words already on the connected rack. |
| Bench → reference pair | Edited program and access to fresh examples A and B | Run A then B. Only two resolved, different readings ask for a repeat. The repeat runs B then A and compares the readings against the original ticket positions. |
| Reference pair → vacant socket | Repeated sensor difference, measured candidate scope and material-use contacts | The local repeat law authorizes one installation. The old program remains available. An occupied socket or expired request cannot be overwritten. |
| Socket → desk → fresh unknown | Newly wired test program, via the desk’s existing activation passage | Use the new reading to narrow the desk’s live candidates. A missing or uncertain reading preserves them. The reference evidence by itself never identifies the unknown. |

The bench cannot read the unknown or inspect private machine state. Its public meter counts washers released or beats on which a print impression was applied. This meter accounts for cost separately from the sensor bit used to distinguish designs. Before a comparison, the bench reserves enough beats and material for both runs at their stated maximum, then charges actual consumption. Stock protection vetoes new programs that might spend two units in one run. Unsupported or unrepeatable observations still cost the beats and material used. Testing stops when an edit earns the socket, the admissible variants run out, or the limits prevent further runs.

## Encoding chart

The eight-bit test program places the main wire at bits 0–2, the return or accent wire at 3–5, and the sensor port at 6–7. Each wire field stores the first beat in its least significant bit. The last main pulse fixes the sample time.

| Example | Parent → child, hex | Three timing fields as sensor / auxiliary / main | Measured contacts stored locally |
| --- | --- | --- | --- |
| Washer, chute | `93 → 8B` | `10 / 010 / 011 → 10 / 001 / 011` | Scope `820` hex for tickets 5 and 11. ON `800`, more-than-one washer `800`. |
| Border, horizontal | `53 → 4B` | `01 / 010 / 011 → 01 / 001 / 011` | Scope `003` for tickets 0 and 1. ON `002`, more-than-one impression `003`. |

The repeat gate has sixteen contacts. For resolved reference bits, the two-bit pair index is `A + 2B`. Contact `first + 4 × repeat` permits installation for `1 → 1` or `2 → 2`, giving the bitmap `0420` hex. Missing and ambiguous supports never enter this gate as settled pairs. Swapping which machine reads ON in the repeat fails it.

The vacant position is socket 6, the position used by the authored feedback test in the previous study. On this rack its program, prediction contacts and scope start empty. The bench receives only the connected parent words. The four-byte installed row stores the program, twelve ON contacts and twelve material-use contacts. A separate local scope mask states which references were measured. A future desk whose candidate set extends beyond that mask cannot treat the unmeasured bits as OFF. [Original desk and its passage wiring](WasherProbeDesk.md)

## What the mode sweep covers

The sweep uses **all unordered pairs** of the twelve dispensers and eight printers, with each member tried as the fresh unknown. It crosses all eight sensor subsets, early and late parent pulses, forward and reverse printer traversal, and sixteen operating settings. The interactive factory examples additionally start with all twelve candidates. The tabulated episodes start with a supplied pair so the same ambiguity can be tested under each setting.

| Domain | Episodes | Bench requests | Socket retained and used | Fresh read then identified the machine |
| --- | ---: | ---: | ---: | ---: |
| Washer dispenser | 33,792 | 9,250 | 350 | 290 |
| Border, forward | 14,336 | 6,264 | 552 | 460 |
| Border, rewind | 14,336 | 3,606 | 228 | 190 |
| Total | 62,464 | 19,120 | 1,130 | 940 |

The last two columns concern episodes that installed a socket. In the other 190 installations the unknown’s first sample was deliberately withheld, so the bench had earned a test but the desk still lacked an answer. Each retained socket was exercised. The real candidate survived every episode.

| Operating setting (3,904 episodes each) | Retained and used |
| --- | ---: |
| Both shift directions | 190 |
| Earlier only / later only | 116 / 74 |
| Even-split choice / inspect shared predictions | 190 / 180 |
| Explicitly distinguish the pair / first unknown sample missing | 190 / 190 |
| Locked timing / question already settled | 0 / 0 |
| Protect material / short desk clock / short bench clock / low supplies | 0 in each |
| Missing reference / ambiguous reference / changed result on repeat | 0 in each |

An independent check enumerates all adjacent time positions directly for the clean pair runs and compares the expected distinguishing power with what the desk actually retains. Tests also exercise every legal three-beat input word on each sensor with both edit directions, competing installation requests, reuse on a fresh desk, and refusal to apply a pair-scoped row to a wider prior.

[Download the timing trials](data/timing-sockets.zip). Each CSV row identifies the two reference tickets, hidden member, wiring, seed and mode, followed by the request scope, retained parent/child words, learned masks, actual desk socket sequence, ODV samples, reference comparisons and cost, and final candidate mask. Empty program fields denote a vacant socket. Reproduce the summary or the rows with:

~~~text
dotnet run --project experiments/PrimitiveMeasure -c Release -- --timing-sockets
dotnet run --project experiments/PrimitiveMeasure -c Release -- --timing-sockets --rows
~~~

These results apply to repeatable, freshly initialized references in the supplied family. The system can earn one nearby timing program. The pulse-edit grammar, reference tickets, sensor contracts, repeat criterion and budgets are supplied. Agreement after all allowed edits says what this bench could establish within that neighborhood. It leaves open what a different sensor, a second pulse, or a longer program could establish.
