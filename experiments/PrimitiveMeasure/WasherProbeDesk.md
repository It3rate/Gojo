# Which test should run next?

At a factory test station, a controller tries identically loaded washer dispensers from one production batch. On each test it can listen for the dispenser’s confirmation signal, look at the tray’s occupancy lamp, or listen for a metal washer falling through the chute. It wants to learn how the batch behaves without opening a dispenser. [Open the test station in layer 35](https://gojo.inklingai.ca/encoding/explorer/?layer=35)

**Start in All sensors and click Run next test.** The controller asks for one washer and listens for a receipt. With the default batch it hears one. Four of the twelve possible designs could have done that. On its next test it withholds the first receipt and listens on the second beat. Now try a batch with a cut return wire. The first test gives silence, so the next question changes to “did anything reach the tray?” You choose the hidden batch for the experiment. The controller gets only the sensor reading.

**Every test uses a fresh dispenser with the same loading and wiring.** Within a test, a washer that falls stays in that tray. The setup makes observations comparable across tests and keeps the cost visible. A trial can spend several beats and release real parts. An unanswered test still spends its beats. [Earlier single-order experiment](WasherCounter.md)

## Four things to try

| At the station | What to look for |
| --- | --- |
| With **All sensors**, choose **Can two fall?** | The desk skips its usual opening question. It holds back the first receipt and listens to the chute on beat two. One reading settles the material question even though the return lead may still be unknown. |
| Select **Old rack · chute**, **Retry · 2 · wired**, and **Tell batches apart**. Run to a stop. Switch to **Chute only** and repeat. | The original rack leaves two designs. The extra socket opens the return wire on beat one and listens for a second fall. Now the chute can reveal an electrical fault. |
| Select **Protect stock** and ask **Can two fall?** | The desk blocks a test if *any* surviving design might release a second washer on that order. It can gather safer evidence first. In some batches the very action needed to resolve the risk remains forbidden. |
| Select **First read missing**, then **Inspect stuck lamp**. | A missing sample leaves all prior candidates alive. The inspection preset uses an empty dispenser whose lamp is stuck on. A later chute reading contradicts a prediction shared by every survivor. You can see the set become empty. |

## What makes a choice

Each candidate has one contact in the desk’s twelve-bit memory. Each test socket has a row of measured predictions. The following path is local to this desk.

~~~text
live candidate bits AND a socket’s measured ON contacts  ──► possible ON
live candidate bits AND their complement                 ──► possible OFF
both possible ──► informative socket
informative + wired + untried + affordable + safe ──► gate can open
one shared activation ──► first open socket ──► real test ──► one sensor
sensor support ──► retain compatible candidates ──► choose again
~~~

The desk prefers a test that directly answers its selected yes/no question when one is legal. **Shorter test** then uses physical row order, shortest first. **Even split** counts the contacts on the smaller side of each eligible partition and favors the largest such count, breaking ties in row order. Neither choice uses a probability of a fault. Two explanations that have exactly the same readings still occupy two contacts.

The ten outgoing passages share one activation site. Their packed endpoint rows are `[0,1,2]`, `[0,3,4]` through `[0,19,20]`, each ordered as credit, gate, socket. The first open passage moves the credit. Its socket drives a test with the encoded request and return wires and samples the specified sensor on the last beat. A return passage re-arms the desk. One scheduled trial can run, however many gates open together. The desk records the cost and tried socket before it reads the result. In this experiment a socket is tried at most once per episode.

| Sensor support | What the desk retains |
| --- | --- |
| `01` observed off | Prior AND complement of the socket’s ON contacts |
| `10` observed on | Prior AND the socket’s ON contacts |
| `00` missing or `11` unresolved | Entire prior. These are different witnesses on the timeline. |

Asking only questions that divide the survivors has a blind spot. Either possible answer fits at least one survivor. **Inspect stuck lamp** also permits testing a shared prediction when there is no remaining legal split. In the empty, stuck-on example, silence on the return wire and a lit lamp leave four plausible loaded dispensers with cut leads. All four predict a washer will fall on the first request. The chute stays quiet. That is evidence against the joint premises of the rack, batch and readings. It does not locate the fault by itself.

## The tenth socket

The first nine tests delayed the return wire until the final sample. A sweep over arbitrary wire schedules found a pair those tests could never separate through the chute. Both dispensers start with two washers and serve every request. Only one has a working receipt lead.

| Beat | Command from the station | Working lead | Cut lead |
| --- | --- | --- | --- |
| 1 | Send the request and allow its receipt back. | A washer falls and the desk stops asking. | A washer falls and the desk keeps asking. |
| 2 | Leave the request wire open and listen at the chute. | Quiet. | A second washer falls. |

This extra socket turns a property of one signal into a visible difference at another through feedback. The old rack remains selectable. Finding the missing experiment was an **offline comparison**, followed by a human-authored addition to the rack. Once the socket is installed, the desk itself decides when the surviving candidates justify using it.

## Exact encoding

The twelve candidate positions below are stable within this rack. “Once” remembers a successful delivery and can replay the receipt. “Retry” tries to release fresh stock on each request. Each cell lists the positions for zero, one and two washers respectively.

| Batch return lead | Once | Retry |
| --- | --- | --- |
| Working | 0, 1, 2 | 3, 4, 5 |
| Cut | 6, 7, 8 | 9, 10, 11 |

A socket occupies four bytes. Bits 0–2 drive the forward wire and bits 3–5 drive the return wire, earliest beat at the low end of each field. Bits 6–7 select receipt (0), lamp (1) or falling-washer pulse (2). Bits 8–19 hold the twelve ON predictions. Bits 20–31 identify trials that could release a second washer. The last driven forward beat fixes the sampling time. Each reference design was run once per socket to fill the two contact masks, for 120 calibration runs.

| Socket | Last reading | Program hex | ON mask hex | Double-release mask hex |
| ---: | --- | ---: | ---: | ---: |
| 0 | Receipt, beat 1 | 09 | 036 | 000 |
| 1 | Lamp, beat 1 | 49 | DB6 | 000 |
| 2 | Chute, beat 1 | 89 | DB6 | 000 |
| 3 | Receipt, beat 2 | 13 | 026 | 820 |
| 4 | Lamp, beat 2 | 53 | DB6 | 820 |
| 5 | Chute, beat 2 | 93 | 820 | 820 |
| 6 | Chute, beat 2 after an early receipt | 8B | 800 | 800 |
| 7 | Receipt, beat 3 | 27 | 006 | 820 |
| 8 | Lamp, beat 3 | 67 | DB6 | 820 |
| 9 | Chute, beat 3 | A7 | 000 | 820 |

For example, socket 6 is stored as `8B 00 08 80` in low-byte-first order. Socket 5 can spend the second washer in batches 5 and 11. Socket 6 can spend it only in batch 11. The same three public surfaces are available in both cases. The timing of the feedback changes the physical risk.

## Learning boundaries and cost

An independent sweep ran each of the twelve designs on **all 4,096 six-beat forward/return schedules**. It compared every sampled beat for each of the 66 pairs of designs on each sensor. For every pair and sensor that differed anywhere in those traces, at least one of the ten short sockets produced a distinguishing sample. The shorter rack missed the early-receipt case. Completeness here is relative to this family, these sensors and this six-beat check.

| Available sensors | Distinguishable groups among 12 | Least beats that guarantee the observable group |
| --- | ---: | ---: |
| None | 1 | 0 |
| Receipt | 4 | 5 |
| Lamp | 2 | 1 |
| Chute | 4 | 4 |
| Receipt and lamp | 5 | 5 |
| Receipt and chute | 6 | 5 |
| Lamp and chute | 4 | 4 |
| All three | 6 | 5 |

The last column comes from a separate offline search over fresh tests, with reliable readings and stock protection off. A strategy must work on either answer. Each test costs its own beats plus the greater cost of its two continuations. This search never drives a socket in the desk.

Even with all sensors, the four empty designs share one observable group. Likewise a working dispenser that remembers its first delivery behaves the same with one or two washers initially loaded. A lit tray lamp reports occupancy, so watching it for longer cannot expose the spare washer. Agreement at a port can answer a useful question while preserving uncertainty about the mechanism inside.

There is a concrete price to choosing locally. With receipts alone, a five-beat strategy can start with socket 3 (two beats). If it hears a receipt, it follows with socket 7 (three beats). After silence, it uses socket 0 (one beat). That distinguishes all four receipt behaviors. Both local priorities start with socket 0 instead. In a four-beat window their choices can favor different batches. After an initial receipt, **Shorter test** can identify a one-washer retry dispenser on its second test. **Even split** can instead identify the behavior of a remembering dispenser. Neither rule guarantees the best use of every budget.

## Reproducible trials

The episode sweep covers all twelve hidden batches, all eight combinations of sensors, budgets from zero to nine beats, stock protection on or off, both priorities, and three questions. It repeats each setting with a reliable first read, a lost first read and an unresolved first read. That gives **34,560 episodes** and **44,302 executed tests**. The real batch remained among the survivors in every episode. Every material trace conserved its initial stock, and every protected test released at most one washer.

| Local priority | Episodes | Requested read resolved | Beats spent | Washers released | Reliable episodes missing an affordable guaranteed result |
| --- | ---: | ---: | ---: | ---: | ---: |
| Shorter test | 17,280 | 5,904 | 28,608 | 16,266 | 10 |
| Even split | 17,280 | 5,918 | 28,668 | 16,226 | 12 |

For **Tell batches apart**, “resolved” means the remaining designs have identical behavior at all installed sensors under the complete test bank. With no sensors that question reaches its coarsest possible resolution immediately. For the other two questions, every survivor must agree on the requested yes/no property. The totals weight the enumerated settings equally. They are counts of what was identifiable under specified constraints, rather than estimated factory success rates.

[Download the trials and distinguishing witnesses](data/washer-probes.zip). The episode CSV records the tested sockets in execution order, sensor supports, remaining design bits, material used and the offline cost bound. In `first_read`, −1 means the sensor worked normally, 0 withholds the first sample, and 3 supplies unresolved support. In the second CSV, each of 198 rows gives one design pair and sensor, the shortest observed distinguishing prefix, and all sockets that distinguish it. An earliest beat of zero means no difference was observed in the six-beat sweep.

~~~text
dotnet run --project experiments/PrimitiveMeasure -c Release -- --washer-probes
dotnet run --project experiments/PrimitiveMeasure -c Release -- --washer-probes --rows
dotnet run --project experiments/PrimitiveMeasure -c Release -- --washer-probe-witnesses
~~~

The desk learns which calibrated explanations survive and can act on a partial answer. Its family, sensors, choice rules and stock guarantees are supplied. Variable loading between trials, intermittent faults without an explicit missing-sample witness, invention of new sensors, and automatic repair of a rejected model would each require more structure.

The [empty-socket experiment](TimingSocket.md) starts with the original nine sockets, lets the desk request an edit, and checks whether its bench can earn the missing socket from repeated observations.
