# Did the washer drop?

On a factory assembly line, the control desk tells a dispenser to drop one flat metal washer into a parts tray, and a lamp stays lit while the tray holds any washers.

The desk expects a confirmation signal called a **receipt**. After one beat, no signal has come back. Did the request get lost on the way out? Or did the washer fall into the tray and the receipt get lost on the way back? The desk has the same evidence in both cases. Asking again is reasonable. Whether asking again dispenses a second washer depends on one bit in the release rule. [Open the interactive counter](https://gojo.inklingai.ca/encoding/explorer/?layer=34)

**Try it in three steps**

1. Start with **Inside the machine**, **Receipt lost**, and **Remember delivery**. The first beat has already happened. One washer is in the tray. Click **Next beat**. The desk gets its receipt while the second washer stays in stock. Switch to **Serve every request** and click **Next beat** again. The desk gets the same receipt, this time with two washers in the tray.
2. Choose **Desk only**. Switch between **Request lost** and **Receipt lost**. At beat one the evidence is identical. Choose **Desk + tray lamp**. Now the desk can distinguish an empty tray from an occupied one. Once a washer is present, the lamp stays lit even if a second one falls.
3. Choose **Test both rules**. The first two receipts are lost. Click **Next beat** twice. On beat three the remembering counter can still send a receipt. The other has exhausted its stock. Try **Release pulses** and **Tray lamp** on the same schedule. Each control change returns to beat one.

## What is actually connected?

There are three working owners. The desk holds one confirmation bit. The counter holds two stock sites and a memory of successful delivery. The tray has an intake gate, an inbox and a shelf. A two-wire fixture sets which signals can travel on a given beat. The desk retries once per beat while it is waiting.

~~~text
desk waiting ── request wire ──► release rule ◄── remembered delivery
                                    │
                              opens the intake
                                    ▼
counter stock 0 ──┐              tray inbox ──► tray shelf
                  ├── one intake ────▲
counter stock 1 ──┘

either successful handoff ──► remember delivery
successful handoff OR recognized retry ── receipt wire ──► desk confirmation
tray inbox OR tray shelf ──► optional occupancy lamp
~~~

A stock site starts with a whole washer. Moving its contents empties that site and fills an empty tray site. The washer's weight and size have no encoding. A receipt can cross a wire repeatedly without emptying the tray. The distinction comes from the connection itself. The stock uses a material passage, and the receipt samples an event or a retained observation.

| Encoding | Bits and roles | What changes |
| --- | --- | --- |
| Each local site | Fixed positive-right orientation and positive direction in the bucket. Its stored bit is V. | An empty material site reads ODV `000`, an occupied site `001`. Signal sites use the same two words for off and on. |
| Release rule | Select bit `L + 2R`. L is an arrived request. R is the remembered delivery. | `0010` serves the first request. `1010` also serves a request after a delivery. |
| Stock → tray | Two one-bit endpoint pairs, low field first. `[outlet 0, intake 0]`, then `[outlet 1, intake 0]`. Packed byte `04` hex. | The first eligible stock site fills the common inbox. A second arrival on that beat encounters an occupied site. |
| Material law | State index `S + 2C + 4T` for source, gate and target. Bit `before + 8 × after` admits a transition. | Only `011 → 110` transfers. In both words the middle bit is the open gate. Every other state holds. |
| Receipt | `moved OR (request AND NOT admitted)`. OR truth mask `1110`. | A denied repeat can reuse a recorded delivery. An admitted request that found empty stock produces no receipt. |
| Feedback | `remembered OR moved` at the counter and `confirmed OR returned` at the desk. | Both sample after settlement, before either latch changes. Only the desk's first returned receipt ends the order. |
| Tray lamp | OR of the two material site reads. | The lamp can tell empty from occupied. Both one washer and two turn it on. Reading it leaves both sites intact. |

For the two supplied laws, a denied *arrived* request always denotes a remembered delivery. Adding another reason for denial, such as a safety interlock, would require retaining that distinction on the receipt path.

The four release bits have this order. Only the last row differs.

| Request L | Remembered R | Remember delivery | Serve every request |
| --- | --- | --- | --- |
| 0 | 0 | hold | hold |
| 1 | 0 | admit | admit |
| 0 | 1 | hold | hold |
| 1 | 1 | hold and replay receipt | admit and try to release |

Every beat first parks the preceding inbox contents if the shelf is free, samples the request and gate, gives the two stock outlets their turns, and samples receipt and feedback. The shelf and inbox provide two actual places where a washer can settle. Each owner sees only the surfaces wired to it.

## One probe, three different answers

Keep the forward wire open for three beats. Open the return wire only on the third. The packed words are forward `111` and return `100`, with the earliest beat in the low bit. The table runs in time order from top to bottom.

| Beat | Remember delivery | Serve every request | What the desk hears |
| --- | --- | --- | --- |
| 1 | First washer falls. Receipt lost. | First washer falls. Receipt lost. | Silence from both. |
| 2 | Receipt replayed and lost. One washer in tray. | Second washer falls. Receipt lost. | Silence from both. |
| 3 | Receipt replayed. One washer remains in stock. | Stock empty. No transfer or receipt. | Receipt from the remembering counter only. |

The earliest distinguishing beat depends on which port you can see. Watching the falling washers distinguishes these rules on beat two. Listening at the desk takes at least three beats. Reading the occupancy lamp cannot distinguish the rules on any shared schedule in this fixture. Both trays become occupied on the same first delivered request, and both retain their contents. This is a property distinction with a physical consequence. A branch driven only by that lamp has no evidence on which to react to a double delivery.

## What the bits predict, and what the desk knows

With **known wire words**, there is a direct read independent of the runtime. Let `F` select forward-open beats and `B` return-open beats. For a nonnegative window word, `first(x) = x & -x` isolates the earliest set bit, or yields zero.

* The remembering counter drops a washer at `first(F)`. Its first heard receipt is `first(F & B)`. Further requests can replay the receipt after a lost return.
* The other counter has two stock sites. Its eligible releases are the first two set bits of `F`. Its first heard receipt is the earliest of these with an open return wire. Cut the release word after that beat because the desk stops requesting. If neither return gets through, both eligible releases stand.

With **unknown wire words**, the desk instead asks which whole release/receipt histories fit its evidence. It knows the chosen table and starting state. It supplies the receipts it heard, a visibility mask, and optionally the current lamp bit. The reader retains each compatible pair of event words once. A hidden beat admits either observation. An observed silent beat admits only silence. No candidate moves a washer. Two heard receipts for this single order, or a heard receipt with an empty final tray, yield an empty candidate set.

For instance, after two observed silent beats the remembering rule admits zero or one delivered washer. The other admits zero, one or two. With its lamp lit the latter still admits one or two. A guess about a missed receipt cannot narrow that range. With the wiring itself unknown, either rule can also explain every possible desk-only receipt word in the six-beat corpus if each may have its own hidden wire schedule. Holding the wire schedule fixed across the two machines is what makes a distinguishing probe possible.

## Trial results

The sweep exhausts all 64 forward words and all 64 return words over six beats. Each schedule ran once with each rule, for 8,192 executions. The independent bit read matched every release beat and every heard receipt. Stock plus tray held two washers after every beat. Each row below counts schedules. These counts have no assumed real-world failure probability.

| Release rule | Schedules | Desk confirmed | Two washers delivered | At least one delivered, desk still waiting |
| --- | ---: | ---: | ---: | ---: |
| Remember delivery | 4,096 | 3,367 | 0 | 665 |
| Serve every request | 4,096 | 2,928 | 1,824 | 1,104 |

Pairing the two executions of each schedule and comparing **entire traces** distinguishes the two rule tables on 1,824 schedules at the release port, 439 at the desk receipt port, and zero at the occupancy lamp. At three beats there is exactly one distinguishing schedule at the desk, the `111 / 100` probe above.

[Download the 8,192 trial rows as CSV in a ZIP](data/washer-trials.zip). Each row gives the release mask, the two link words, predicted and observed event words, and final stock and tray site bits. Numbers in the CSV are decimal packed words. A two-bit tray word of `3` means both tray sites are occupied.

~~~text
dotnet run --project experiments/PrimitiveMeasure -c Release -- --washer-counter
dotnet run --project experiments/PrimitiveMeasure -c Release -- --washer-counter --rows
~~~

## What this establishes

One local bit of retained evidence is sufficient to suppress duplicate fulfillment **for one outstanding order**. Eventual delivery still requires the request to arrive. Confirmation requires a usable return on a beat with a receipt. The model uses a serial clock, fresh memory, initially full stock, and wires that can lose pulses. The three-versus-two-beat distinction depends on this capacity and on the absence of restocking. The next order would need its own boundary. Clearing memory while an old retry can still arrive would let that retry release fresh stock.

The release rules, feedback connections and probe schedules are authored. The experiment measures which differences are visible from a given port. It leaves discovering the rules and choosing a probe autonomously as separate work.
