# Which action helps?

Three actions initially help a little. On a later try, one eases the selected need twice as fast. Can that action earn more turns from the relief the actor actually experiences? And what happens to a second need that the same action helps, harms, or has no means to reach?

Think of a jockey with bills to pay, standing to earn at the track, and children waiting for a ride home. “Purse” measures the outstanding financial need. “Standing” and “pickup” have their own accounts. The three outlets are alternative uses of one turn. The examples vary which consequences follow each choice.

Run **Three offers, B opens a second service** and step through the recorded turns. Then try **Move that service to C**. The same chooser and starting memory run in both cases. Compare the three outlets' forecasts with the settled changes in purse, standing and pickup. [Open the experiment](https://gojo.inklingai.ca/encoding/explorer/?layer=46)

**Concept.** A local outlet can earn the next opportunity by repeatedly delivering relief in the need currently in focus.

**Limit.** A preference is specific to its evidence, its time window and the available routes. An outstanding need can persist, and a missed opportunity to learn can preserve a mistaken preference.

## How a choice is made

```text
need in focus + advertised outlets + current access + previous relief
                                    │
                      one opportunity visits A, B, C or waits
                                    │
           the chosen outlet's connections act on their own need banks
                                    │
                its connected feedback rows record observed changes
                                    │
                         available for the next choice
```

Each need has up to 48 outstanding sites in its own basis. A service moves one marker from outstanding to accounted, or in the opposite direction for harm. The counts shown in the inspector are reads of those sites. A purse marker, a standing marker and a pickup marker have no common exchange rate. Here “tension” means an outstanding need read at this resolution. The signed carrier version of an unpaid draw is a separate, tested construction. [Where a goal gets its drive](GoalFlow.md)

One conserved activation starts idle. Each outlet can claim it through a passage. To qualify, the selected need must be present, the outlet must advertise a relevant connection, and it must be open. The chooser reads only its retained forecasts of these offers. Initially, untried offers rank first. Among tried offers, two observed steps of relief rank above one. A known ineffective or harmful offer cannot win on that need. A rotating starting position resolves ties. The selected action acts, feedback from its own connected need sites updates its three small memory fields, and the activation returns for the next turn. This initial exploration rule and the rotating order are supplied as structure.

All twelve opportunities in the main trial have the same duration. Each first use relieves one purse claim. B has a second service connection gated by a retained record that B has already been used. On B's next use, it can relieve two claims in that interval. This measures **relief per equal opportunity**. It leaves longer delays, different action costs, and longer-term averages open.

| Turns | Choices | What the actor learns about purse |
| --- | --- | --- |
| 1, 2, 3 | A, B, C | Each first try relieves one claim. |
| 4 | A | A again relieves one. |
| 5 | B | B was expected to relieve one and actually relieves two. |
| 6 through 12 | B each time | B retains the strongest observed relief among the open offers. |

In the matched controls, the stronger connection moves while the timing, offers, initial needs and chooser stay fixed.

| Faster outlet | Choices of A, B, C with purse feedback | Choices of A, B, C with purse feedback disconnected |
| --- | --- | --- |
| A | 10, 1, 1 | 4, 4, 4 |
| B | 2, 9, 1 | 4, 4, 4 |
| C | 2, 2, 8 | 4, 4, 4 |

With **three equally helpful outlets and feedback connected**, each gets four turns. The physical service still works when purse feedback is disconnected. The actor cannot use that relief to revise its purse forecast. The asymmetric counts in the first column include the turns needed to discover the stronger connection in each position.

## Three needs, three different answers

In the optimistic trial the actor predicts that B will relieve **two purse claims**. It tries B. Outstanding purse claims rise from 36 to 37, and standing claims fall from 32 to 30. The actor records a purse debit of one and standing relief of two. The two reads can coexist. A forecast has no passage that pays an actual claim. These are modeled outcomes, including an authored optimistic starting forecast, rather than actual odds or a financial model.

In **Attend to pickup without an outlet**, all three actions are physically open. The pickup account has six claims and none of the outlets advertises a pickup service. The opportunity stays idle on all twelve turns and all six claims remain. A separate test supplies three advertised services that have *already been observed to accomplish nothing*. The same decision to hold then has different evidence. A rock held in place is a useful analogy for this distinction between stored potential and an afforded path. The race experiment's need is an observer's outstanding claim. The analogy supplies no intention to the rock.

## Letting the situation choose the need

Most trials select one basis explicitly to measure what the action chooser learns. In **Let a due pickup take priority**, a separate circuit chooses the basis. A due, outstanding pickup with an open advertised route gets first priority, then an actionable purse, then standing. The clock supplies access and deadline observations. The priority comes from stored rules and connections. The relief forecasts come from experience.

| Turn | Situation | Focus and result |
| --- | --- | --- |
| 3 | Pickup is due, but its route is closed. | Purse. Two pickup claims stay outstanding. |
| 4 | Pickup's route opens while the due signal is uncertain. | Purse and pickup are both possible. The opportunity stays idle. |
| 5 | Pickup is confirmed due and reachable. | C reduces pickup from two to one while purse obligations rise from five to six. |
| 6 | One more pickup claim remains. | C completes it at a further purse cost. Seven purse claims remain. |
| 7 | Pickup is satisfied. | The focus returns to purse, which falls to six. |

The return to purse requires no comparison of the size of a payment with the importance of a child. The chosen priority is one explicit policy. A useful failure case is when an urgent outlet advertises a route but the actor remembers only harm from it. The focus can select pickup and the action chooser can hold, even though a lower-priority action would help. Passing that unused opportunity to another need would require another connection.

## What is retained

| Bytes or bits | Meaning in this experiment |
| --- | --- |
| Opportunity low four bits | Idle, A, B, C. Exactly one site is occupied, including on a held turn. Three ordered grant rows pack as `60 50 C1 03` at three bits per reference. |
| Three bits per remembered outcome | 0 means unseen, 1 harm, 2 observed no change, 3 one step of relief and 4 two steps. Nine fields retain a separate sample for each action and need. This is a local outcome code. |
| Effect rows | Four two-bit fields name an outlet, a need, the relief or harm direction, and whether a previous use must already be present. These belong to the provider behind the outlet. |
| Feedback rows | Three four-bit fields bind the occupied outlet, the sensed need, and the destination memory site. A disconnected return supplies no new sample. |
| Focus support | Bits 0 through 3 admit waiting, purse, standing and pickup. A narrowly known basis can execute. If several remain possible, the opportunity is held. |

Each service is bounded by the receiving bank. A proposed two-step relief with only one remaining claim delivers one, and the signed blocked field records the other. An extra debit at the top of the bank also remains visible. Feedback records the realized change of the selected account. In this fixture all service associated with the choice settles in one interval and no unrelated change happens during it. Delayed outcomes and attribution in a changing environment need further structure.

Finally, try **A stale negative forecast prevents a new trial**. B could give faster relief, but starts with a remembered adverse outcome. A keeps helping, so B never earns a trial to correct its forecast. The actor learns within the offered opportunities. It has no periodic retest, automatic discovery of a new outlet, or general proof that its favorite is best. There is also no requirement that all demand disappear before another action can occur.

## Reproduce the results

```powershell
dotnet run --project experiments/PrimitiveMeasure -- --tension-choice experiments/PrimitiveMeasure/data/tension-choice
```

The [recorded turns](data/tension-choice/turns.csv) cover nine twelve-turn cases and one eight-turn priority run, for 116 observations. The [wiring](data/tension-choice/wiring.csv) records each apparatus's selected packed rows. The [position and feedback controls](data/tension-choice/permutations.csv) contain seven separate twelve-opportunity runs, including the equal-service control. In the turns file, blank forecasts or observations mean the corresponding information was unavailable. A stored zero relief means a known unchanged outcome.
