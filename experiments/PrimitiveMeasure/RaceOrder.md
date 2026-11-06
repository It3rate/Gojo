# From two landmarks to a race finish

Two horses wait for the same start. The first question is which horse crosses the finish first. A course obstruction can hold one horse back. A limited view of the finish can leave the observer unsure even after both horses have arrived. The second question is whether the horse favored before the start went on to win. Answering that takes a connection between two owners, the one publishing the favorite and the one publishing the finish.

Run **The favorite finishes first**, then select an earlier moment to see the start. Compare **Connect the goal to a reserve** with **Withdraw the goal from the reserve**. In both races A finishes and B asks to cross a closed passage. Only the connected goal can enable assistance. Finally compare **Two crossings in one coarse observation** with **One witnessed shared finish**. [Open the race experiment](https://gojo.inklingai.ca/encoding/explorer/?layer=45)

## Reading a small arc

| Part of the arc | What to look for |
| --- | --- |
| Initial state | Two entrants are ready at a shared start. |
| Goal | The contest asks for a finish from both entrants and their order. Completion and determination of the order are separate reads. |
| Trigger | The common start signal permits each ready entrant to begin. |
| Obstacle | A closed passage can hold a started entrant on the course. An obscured observation affects what the reader can report. |
| Result | The actual finish events supply the evidence for placing the entrants. A separate reader can relate that evidence to the earlier favorite. |

The organizer's purpose gives the reader a question. The start and the subsequent admissions provide the causes of motion. An entrant's wish to win would be another local structure with a means to influence its actions. Likewise an observer can ask whether a rock reached the bottom of a hill, while the earthquake that released it supplies a physical trigger. Assigning the rock a motive would add a premise.

## What transfers

On an ordered stroke, ask whether the first selected landmark comes before the second one. At the finish, bind those roles to the entrants' crossing events and ask the same question. Their common basis now comes from the race and its finish. Swapping the roles reverses a strict order. Connecting both roles to one witnessed event establishes equality at that event's resolution. Two separate crossings seen in a single coarse view can still admit several orders.

One level up, a reader connects the earlier favorite to the result for the same race and entrants. An upset gives two compatible facts. A forecast can favor A and the finish can place B first. Agreement is a read about these two observations, with its own sensitivity to a missing or foreign connection.

**Concept.** An ordered pair can be used in a second domain when both roles are bound to evidence on a common basis.

**Limit.** A finish order gives a placing in this contest. Inferring physical speed requires further distance and timing evidence. The experiment supplies the contest conditions and course permissions.

## Which way does the information travel?

```text
common start signal
    ├─ A ready → A on course → A finished ──┐
    └─ B ready → B on course → B finished ──┴─ arrival evidence
                                                 ├─ did both arrive?
                                                 └─ which came first?
forecast for this race and entrants ──────────┐           │
                                              └───────────┴─ did the favorite win outright?
```

A shared signal can enable both starts. Each entrant supplies its own activation. Following the crossings produces an observed order. Connecting that order to the forecast produces a further read. Disconnecting the goal question leaves the start and course passages available to both entrants.

The result reader in this experiment waits for both finish contacts. With only A across the line, the timeline shows A finished and B still on the course. The placing reader still publishes all three candidate orders. A reader designed to announce the first arrival immediately could answer a different question.

## When the goal changes the result

The reserve has one activation of its own. It can move to an open site when B is running, has requested a closed passage, and the goal says the contest is incomplete. Every beat gives the reserve the same opportunity to sample the preceding inputs. The effective permission for B reads either the ordinary opening or the released reserve. The entrants still move through their own passages.

| Beat | What happens | B's five stored bits, hex | Ordinary permission | Reserve released | B's effective permission |
| --- | --- | --- | --- | --- | --- |
| 0 | B waits at the start. | `01` | Yes | No | Yes |
| 1 | The common start releases B. | `0A` | Yes | No | Yes |
| 2 | A finishes. B requests passage and is held. | `0A` | No | No | No |
| 3 | The reserve acts on the unmet goal and B's earlier refusal. | `0A` | No | Yes | Yes |
| 4 | B requests passage again and finishes. | `1C` | No | Yes | Yes |

With the goal disconnected from the same reserve, B stays on the course through beat 4. A third run opens the ordinary passage externally. These are different causes for a similar final placing. The common start, finish requests, course openings and sensor arrangements are supplied experimental inputs. The reserve's decision follows its stored law and the connected reads.

## Reading the bytes

| What is encoded | Concrete example |
| --- | --- |
| Entrant state | Low bits 0, 1 and 2 are ready, running and finished. Bits 3 and 4 hold the gate samples committed by the lane. The current external signals are displayed separately. |
| An entrant's two passages | Ordered rows `[0,3,1]` and `[1,4,2]` connect ready through start to running, then running through admission to finished. At three bits per reference the packed rows are `58 42 01`. Both lanes use the same controlled passage law. |
| Spatial landmark order | `02` holds the one-bit site references `[0,1]`, with the first reference in the low bit. |
| Finish evidence | In the ordinary run `02 02` holds the two contact references followed by the earlier-to-later row. With two distinct contacts in one coarse sample there is no precedence row. A witnessed tie has two bindings to one event site. |
| Placing read | Masks `1`, `2` and `4` admit the first role earlier, the second role earlier, and a tied placing respectively. Two references to one event provide a witness for a tie. Mask `7` admits all three placings. The roles can be exchanged while the actual horses stay on their original lanes. |
| Favorite won outright | Mask `1` admits no, `2` admits yes, `3` admits both. Mask `0` records a missing basis, such as a forecast for another race. Two independent uncertain readings can disagree even if their masks match. |

The consumer binds the forecast and finish through the race's own finish contact and the entrants' corresponding ports. In the ordinary run its row `18` connects the two public observations to a calculated answer. Role exchange changes the order read predictably. A witnessed tie gives a definite “no” to sole victory. Two independent contact marks in one coarse sample leave the finer timing open. That uncertainty concerns their placing. Their source references stay distinct.

## Where the work happens

| Private course steps | Host rank reads, ordinary run | Supplier inspections, ordinary run | Host rank reads, course correction | Supplier inspections, course correction |
| ---: | ---: | ---: | ---: | ---: |
| 16 | 8 | 16 | 14 | 32 |
| 256 | 8 | 256 | 14 | 512 |
| 16,384 | 8 | 16,384 | 14 | 32,768 |

The ordinary run samples the two public ranks at four moments. In the correction run the supplier edits two adjoining course references after the finish. It withdraws the qualification of the present placing, checks part of the revised course, then checks the rest. Earlier captured observations retain what the reader knew at those moments. The consumer spends two public reads per comparable observation. The supplier pays for inspecting the explicit course pairs.

These counts measure the named operations. Total construction time, rendering, memory, and other work are outside the counters. The two entrants share one course, and finish requests are supplied on particular beats. The experiment can report placings, while speed, personal motives, betting probabilities and selection of a new goal remain open.

## Repeating the runs

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --race-order experiments/PrimitiveMeasure/data/race-order
```

The [trial results](data/race-order/trials.csv) contain 14 supplied scenarios and their selected encoding bytes. The [timeline](data/race-order/beats.csv) contains 58 recorded moments across those scenarios. A course correction has several observations on the same clock beat. The [work comparison](data/race-order/work.csv) contains six separate runs that vary the amount of private course detail.
