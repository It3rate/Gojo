# Enough structure: retained run

This is a construction and inspection experiment. The runner supplies the pulses and binding proposals. No learning or automatic topology growth is claimed.

## Receivers

| Construction | Supplied total | Sender value | Sender tension | Receiver value | Unmet | Occupied answer region | Goal reduction on this step |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| direct / free | 0 | 0 | 0 | 0 | 3 | [1,8] | 0 |
| bound / free | 0 | 0 | 0 | 0 | 3 | [1,8] | 0 |
| direct / clamp 3 | 0 | 0 | 0 | 0 | 3 | [1,8] | 0 |
| bound / clamp 3 | 0 | 0 | 0 | 0 | 3 | [1,8] | 0 |
| direct / free | 1 | 0 | 0 | 1 | 2 | [1,8] | 1 |
| bound / free | 1 | 0 | 0 | 1 | 2 | [1,8] | 1 |
| direct / clamp 3 | 1 | 0 | 0 | 1 | 2 | [1,8] | 1 |
| bound / clamp 3 | 1 | 0 | 0 | 1 | 2 | [1,8] | 1 |
| direct / free | 2 | 0 | 0 | 2 | 1 | [1,8] | 1 |
| bound / free | 2 | 0 | 0 | 2 | 1 | [1,8] | 1 |
| direct / clamp 3 | 2 | 0 | 0 | 2 | 1 | [1,8] | 1 |
| bound / clamp 3 | 2 | 0 | 0 | 2 | 1 | [1,8] | 1 |
| direct / free | 3 | 0 | 0 | 3 | 0 | [0,0] | 1 |
| bound / free | 3 | 0 | 0 | 3 | 0 | [0,0] | 1 |
| direct / clamp 3 | 3 | 0 | 0 | 3 | 0 | [0,0] | 1 |
| bound / clamp 3 | 3 | 0 | 0 | 3 | 0 | [0,0] | 1 |
| direct / free | 4 | 0 | 0 | 4 | 0 | [0,0] | 0 |
| bound / free | 4 | 0 | 0 | 4 | 0 | [0,0] | 0 |
| direct / clamp 3 | 4 | 0 | 1 | 3 | 0 | [0,0] | 0 |
| bound / clamp 3 | 4 | 0 | 1 | 3 | 0 | [0,0] | 0 |
| direct / free | 8 | 0 | 0 | 8 | 0 | [0,0] | 0 |
| bound / free | 8 | 0 | 0 | 8 | 0 | [0,0] | 0 |
| direct / clamp 3 | 8 | 0 | 5 | 3 | 0 | [0,0] | 0 |
| bound / clamp 3 | 8 | 0 | 5 | 3 | 0 | [0,0] | 0 |

At total 8, the amount question agrees: **false**. The enough question still agrees.

## Open and failed bindings

| Candidate | Receiver after 4 ticks | Unmet before / after activation | Reduction |
| --- | ---: | --- | ---: |
| open | 0 | 3 / 3 | 0 |
| nonzero target | 0 | 3 / 3 | 0 |
| reversed | 0 | 3 / 3 | 0 |
| completed after arrival | 3 | 3 / 0 | 3 |

## Calculation inspection

Each interval inspection captures its exposed premises, performs one affine projection, checks a domain interval (two one-sided claims), checks the acceptable interval (two more), and publishes membership in a supplied two-region partition. An amount inspection omits the acceptable-interval comparison and answer partition.

Each material accounting witness retains seven native equation predictions: four before/after held-amount sums, available supply, accepted receiver change, and accounted total. An equality claim checks available = accepted + post-source-held. Each goal response adds one before-minus-after demand equation. The fixture does not replace these with detached scalar bookkeeping.

| Selected root | Reachable carriers | Relations | Tick references | Placeholders | Native bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| live limited world | 19 | 148 | 98 | 0 | 5926 |
| enough witness | 26 | 224 | 137 | 0 | 8510 |
| amount witness | 15 | 115 | 76 | 0 | 4584 |
| material account | 41 | 426 | 267 | 0 | 16005 |
| goal response | 88 | 1044 | 586 | 0 | 36926 |

These are retained reachable graph counts including physical attachments and unit evidence, not minimal query complexity or a benchmark. The material and goal witnesses retain substantially more evidence than the live construction.

Comparisons qualify the question domain, acceptable interval and partition under a selected native 1:1 correspondence before comparing occupied answer regions. Equality is evidence about these observations; the runner's sample campaign is not a learned universal theorem.

Current limits: the host schedules arrivals, proposes bindings and requests inspection. Goal response is observational and creates no carrier tension. No autonomous retention, reinforcement, decay, general solver or biological self-maintenance is implemented. Captured exposed numeric premises remain stable; the archive contains the final live worlds plus retained observations, not a full independent physical world per historical step.

## Source receipts

- `Surfaces.cs`: `674bda41f3e2029375c051e94ceecae7674fa97204991800a934c0573360cfbd`
- `Program.cs`: `5aacbf6df4b0691b722d214c130212d8cb06f0abe5d7cce11444308678d493b6`
- `EnoughStructure.csproj`: `2d51025c6fa8d9584657cf1acf95b19adcda63481aea5ae03f45ceb34a005f81`
- `../EnoughStructure.Tests/ConstructionTests.cs`: `1e33654a6559a3df872765c83775add168fe26299eaa444f26c0734fac4f9057`
