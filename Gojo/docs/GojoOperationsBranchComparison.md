# Operations branch comparison

26 November 2026 · comparison checkpoint, with authored alternatives and measured probes

The three checkpoints share master `9b805964`. This review pins Sol at `29df5335`, Claude at `c04869c1` on `claude/youthful-newton-wh29co`, and Neon at `6f655fc2`. The comparison asks whether an implementation gives the expected answer on a selected read, what it costs to produce and retain that answer, and what its remaining structure makes possible on the next request.

The design follow-up reviews the later Claude comparison and proposal. Claude `6a7d3f17` also changes sweep receipts to saturate without blocking material. The follow-up proposes a small test for turning settled observations into reusable equations. [From settlement to reusable equations](GojoOperationsDiscoveryProposal.md)

The [Operations Round 3 review](GojoOperationsRound3.md) compares the later implementations and examines variable partitions with a five-star scale.

## What I would carry forward

| Branch | Strength | Cost or limit | What to carry forward |
| --- | --- | --- | --- |
| **Sol** | An exact local reference, named pair occurrences, explicit request and acknowledgement, and a grouped preview and checked writeback. An incomplete route, a split scope, and a stale calibration can be told apart. | The dense joint table grows as the product of role sizes. Its compact bit network preserves only local supports. Occurrence wiring requires an authored grid in the fixture. Booking and geometry are bounded examples with fixture-local builders. | Keep the exact reference for small domains, the local request/receipt protocol and the rule that changing a structure requires a binding to its actual sites. Generalize a builder only after it works with another target. [Dense product](https://github.com/It3rate/Gojo/blob/29df5335/Gojo.EncodingC/Tests/OperationProductRelationTests.cs) · [Pairing](https://github.com/It3rate/Gojo/blob/29df5335/Gojo.EncodingC/Tests/OperationPairingTests.cs) · [Booking](https://github.com/It3rate/Gojo/blob/29df5335/Gojo.EncodingC/Tests/OperationRequestTests.cs) · [Scoped application](https://github.com/It3rate/Gojo/blob/29df5335/Gojo.EncodingC/Tests/OperationScopeTests.cs) |
| **Claude** | Arithmetic relations have a reusable binding from the same roles into differently directed transitions. A caller can publish a fresh result or write to an existing role. The optional blocked bit makes an unmet quiet hold visible. The branch also exercises concrete bit rungs, elapsed demand and a live versus locked rate. | Free roles are chosen by the least unweighted total in the current binder. That is a policy for choosing a completion, and a different world may need those alternatives. Bound transitions have dense state costs. In a compound sweep, a full receipt can hold up material because receipt recording is itself part of the transition. | Keep directional role binding and selectable blocked evidence. Make completion and receipt participation explicit at the selected port before adopting either as a default. [Binding](https://github.com/It3rate/Gojo/blob/c04869c1/Gojo.EncodingC/Laws/Operations.cs#L41) · [Bit rungs](https://github.com/It3rate/Gojo/blob/c04869c1/Gojo.EncodingC/Tests/BitRungTests.cs) · [Sweep](https://github.com/It3rate/Gojo/blob/c04869c1/Gojo.EncodingC/Flow/Flows.cs#L103) · [Time and rate experiments](https://github.com/It3rate/Gojo/blob/c04869c1/ops_claude/GojoOperationsExperiments-2026-11.md) |
| **Neon** | The richest set of interchangeable forward arithmetic constructions, including radix lattices, Egyptian doubling and quarter-squares. Its compiler recognizes selected live laws and wiring and can accelerate an eligible reaction while keeping the canonical representation. | Each new specialization adds recognition, guard, fallback and observable-step obligations. The composed quarter-square has loose backward support and a wide final join in this implementation. Bounded multiply can report less than the total excess when the overflow role itself fills. | Keep the construction alternatives and a derived plan where parity is established for the requested output and time contract. Require an explicit remaining-excess read if the bounded split is used for accounting. [Multiplier constructions and bounded overflow](https://github.com/It3rate/Gojo/blob/6f655fc2/Gojo.EncodingC/Laws/Multiplication.cs) · [Compiled plans](https://github.com/It3rate/Gojo/blob/6f655fc2/Gojo.EncodingC/Compilation/CompiledPlan.cs) |

For a small exact question, start with the direct relation. For an interaction, take the relevant output through a local binding into an accounted transition. Where a continuation needs a contributor, a carry, a joint or a radial landmark, retain those sites and connections. This combination lets a forward evaluator optimize one read without replacing the whole world that produced it.

## Size

The source footprint below comes from `git diff --numstat 9b805964...<head>`. Each head has that exact merge base. Runtime counts include changed C# outside `Gojo.EncodingC/Tests`. These counts describe the scope of three branches, including the extra compiler on Neon.

| Branch | Files | Runtime lines added / removed | Test lines added / removed | Documentation lines added / removed |
| --- | ---: | ---: | ---: | ---: |
| Sol | 9 | 40 / 0 | 924 / 0 | 320 / 0 |
| Claude | 14 | 158 / 8 | 1170 / 0 | 1883 / 0 |
| Neon | 24 | 1968 / 7 | 1931 / 5 | 1137 / 6 |

To measure a running construction separately, I counted shared laws once and measured the complete canonical `Transport.Save` byte stream from isolated builds. Saved bytes include the connected stores and wiring. The raw law bit count describes the logical table, while the save format can encode a functional transition as after-state indexes.

| Factors `0..3`, one specimen | Raw law bits | Value and support planes | Wiring rows | Saved bytes |
| --- | ---: | ---: | ---: | ---: |
| Direct binary relation, common control | 160 | 26 | 1 | 56 |
| Sol bit support lattice | 24 | 36 | 6 | 97 |
| Claude bit transition lattice | 320 | 12 | 6 | 68 |
| Claude direct bound transition | 25,600 | 8 | 1 | 175 |
| Neon Egyptian doubling | 40 | 54 | 7 | 140 |
| Neon radix-2 lattice | 48 | 84 | 10 | 233 |

The Claude transition rows read and write concrete values. The relation rows in the other cases also carry open support. The savings reverse at different layers. Sol cuts the logical table from 160 to 24 bits, but the full single-specimen save grows from 56 to 97 bytes. At 16 specimens, the common dense relation saves in 131 bytes, Sol's factored relation in 335, and Claude's concrete lattice in 260. In that batch the dense relation has 16 wired rows and each lattice has 96.

At four-bit factors (`0..15`), the single-specimen direct relation takes 57,856 law bits and saves in 7,727 bytes. Neon's radix-4 lattice uses 512 law bits, 10 rows and 378 saved bytes. Egyptian doubling uses 40 law bits, 34 rows and 731 saved bytes. These are good reasons to keep both options. The direct baseline uses ordinary open binding planes to hold the 226-element result domain beyond the small `Supports` mask helper. All three agree on the checked forward `13 × 11`. Their backward support is a separate contract. At `0..4`, the current quarter-square final join instead drives the graph to 112,661 law bits and 14,306 saved bytes, against 425 bits and 103 bytes for the matching dense binary relation. Decomposition pays when each new join stays small. [Sol size assertions](https://github.com/It3rate/Gojo/blob/29df5335/Gojo.EncodingC/Tests/OperationFactoringTests.cs) · [Claude two-bit lattice](https://github.com/It3rate/Gojo/blob/c04869c1/Gojo.EncodingC/Tests/BitRungTests.cs#L146) · [Neon multiplier builders](https://github.com/It3rate/Gojo/blob/6f655fc2/Gojo.EncodingC/Laws/Multiplication.cs)

## Speed on matched workloads

The timings are from source-only Release builds of the pinned trees, run sequentially on a Windows 10 Core i7-7700HQ with .NET runtime 10.0.12 and tiered compilation disabled. Each entry is the median of seven batch averages. Brackets show the lowest and highest batch averages. Input reset, plan creation, output verification and setup are outside the timer. The packet runs warm up 24 times and use 48 repetitions per batch. The product runs use 96 warmups and 320 repetitions per batch. Each verified the output after every timed call.

**Packet conversion.** Each of 64 independent lanes uses `Flows.Convert(3, 2)` on direct stores, with a wiring that has no attached recorder and a settle budget of 32. The two-packet case starts with source capacity and stock 8 and an empty capacity-10 dock. It ends at `(2,4)` in three logical ticks including the quiet step. The fifteen-packet case starts with source capacity and stock 47 and an empty capacity-32 dock. It ends at `(2,30)` in 16 ticks. The compiled run checks use of the fast path and zero deoptimized lanes.

| Packet conversion | Two packets, ms per 64 lanes | Fifteen packets, ms per 64 lanes |
| --- | ---: | ---: |
| Sol canonical | 3.52 [3.37–4.61] | 228.51 [191.87–284.13] |
| Claude canonical | 3.20 [3.14–3.28] | 235.88 [224.15–258.56] |
| Neon canonical | 5.69 [5.29–11.61] | 271.35 [232.50–298.92] |
| Neon compiled | 1.00 [0.80–1.50] | 8.05 [5.61–9.47] |

Compared within the Neon process, the compiled median is about **5.7×** and **34×** faster, respectively. The fifteen-packet case allocated about 330 MB per timed call on the canonical path and 10.5 MB on the compiled path, measured as bytes allocated by the running thread. This is separate from the saved footprint.

**Two-bit forward multiplication.** Each pass executes one ordered `Engine.Step` over one specimen, cycling through all 16 settled input combinations on every run. The common control uses `RelationLaw.FromPredicate` with binary domains of sizes 4, 4 and 10. The branched cases use Sol's four support-bearing bit products and two half adders, Claude's concrete bit transitions in the same topology, and Neon's two-bit Egyptian builder. Each pass is verified against ordinary multiplication. The timing contract here is the settled forward result.

| Forward product | Shared dense relation, µs per specimen | Branch's factored path, µs per specimen |
| --- | ---: | ---: |
| Sol | 9.89 [8.55–20.83] | 60.30 [52.97–80.32] |
| Claude | 12.73 [8.63–22.25] | 66.98 [59.50–88.99] |
| Neon | 7.76 [7.27–8.06] | 51.83 [51.28–52.98] |

For this tiny domain the one-row relation has the least ordered-pass work. Changing the expected read to general inverse support or moving to a wider domain changes the comparison. These execution-only timings use prebuilt machines and isolated receivers. The common-path variation across processes gives a sense of the noise. Production speed should be judged within the same read, clock and evidence contract.

Neon's earlier packet discrepancy was real at `f6586cb5`. At `6f655fc2`, the limit-one probe with source 8 and dock capacity 10 leaves source 5, dock 2, and tick 1 in both execution modes. The latest eligible bulk path obeys the budget and advances time. The generic compiled settling loop still tests equality of store contents across the step. Canonical settling checks for quiet at every row, so an ordered step can do work and restore its own initial state. In a separate probe with two ordered toggles on one bit, canonical settlement spent its four-tick budget and returned unresolved. Compiled settlement stopped after one tick. Both ended with the original bit. This matters when a dependent reader cares about activity or elapsed steps. [Packet bulk path](https://github.com/It3rate/Gojo/blob/6f655fc2/Gojo.EncodingC/Compilation/CompiledPlan.cs#L438) · [Canonical settling](https://github.com/It3rate/Gojo/blob/6f655fc2/Gojo.EncodingC/Flow/Engine.cs#L193)

The pinned EncodingC Release suites passed in isolated trees (237 Sol, 244 Claude, 241 Neon). The matched probes above also check behaviors outside the existing tests.

## Same output, different next moves

Yes, there is a concrete long-term path. The output agreement has to say **which read agrees**. A current number, an occupied boundary, an ordered closed traversal, and a history of how it formed are different promises. Two machines can agree at one resolution and expose different local features to the next operation. An internal difference matters only when it changes what that operation can observe, bind, or do.

- **Already implemented.** In the construction-context study, one closed four-chord path and four separately reset sides have complete ordered support correspondence. A side-path edit can bind an actual member only in the split construction. The original traversal constraint is withdrawn when the sides become independent. A new closure in C02 needs an authored proposal. In C04, the current drawing also survives capture into a fresh independent world, while old selectors and historical reads stop at the new boundary. The geometry in these cases is square-like under the selected display calibration. [Construction-context experiments C01, C02 and C04](GojoConstructionContextExperiments.md#cases)
- **Reproduced in a temporary native CAD probe.** Construct two tiles over `x=0..100` and `x=100..200`, each spanning `y=0..100`, and read their `SettledPositiveAreaUnion` via the exposed fill roles. Both pairs initially give the same exterior with one loop and six exterior segments. One pair shares a seam `TickRef`. The other uses coordinate-equal independent references. Move only the left seam from 100 to 80, then explicitly issue fresh fills from both live sources. The shared pair still has one perimeter. The independent pair has two perimeters with a 20-tick gap. The previously issued fill carriers retain the old geometry in both cases because their unit shapes captured it. The probe supplies the two candidate graphs and the edit. It exercises their consequences and the difference between a current read and a retained observation. [Fill construction](../../Applications/GojoCAD/GraphicStructure.cs#L184) · [Tick reference rules](GojoSystemPrompt.md#23-carrier-location-and-tick-reference)
- **Operations already give a small arithmetic example.** Sol's bit network and its dense law agree for every settled pair of two-bit factors. The network exposes carries and partial products that a consumer can bind to when they are part of its declared interface. The dense law can reject impossible product 5 over `0..3` while the factored network goes quiet with open supports. Neon has several built configurations for the same forward multiplication. Claude's binding example adds another way to use the same relation, with a write or an unresolved hold. These are observable differences today. Neon’s quarter-square offers a similar limit. An output of 1 over `0..4` leaves each factor with local support `{0,1,2}`, while the direct relation narrows to `{1}`. Automatic discovery or selection of a configuration from a downstream task remains exploratory. [Factored support tests](https://github.com/It3rate/Gojo/blob/29df5335/Gojo.EncodingC/Tests/OperationFactoringTests.cs)

### A square experiment on one common seed

Use a declared equal-axis frame, an origin, and a single reference point at `(2,0)`. Ask each small construction for the same ordered four-chord boundary through `(2,0), (0,2), (-2,0), (0,-2)`. Make the boundary read account for every actual occurrence, with a basis correspondence and orientation. Three candidates can use the same seed:

| Configuration | Interior evidence | Matched continuation |
| --- | --- | --- |
| Two calibrated parallel pairs | Ordered offsets, intersections, and distinct opposite-side relationships | Extend one pair's offset from the center while the other stays fixed. Ask which corners remain bound and whether the revised boundary still reads as a rectangle. |
| Closed path | Actual consecutive edges and a shared start/end site | Retain two edges as a selected path and append or refine a chord. Read which joins and closure survive. Ask the same continuation of the parallel pairs. |
| Center, radius and quartered contour | The shared center and radial attachments, an origin marker, and a traversal partition | Refine the partition and ask for an additional polygon vertex. Report an open result or calibration residue if the chosen rung cannot represent it. Ask the same continuation of the closed path. |

Run **all** continuations against **all** candidates. Record which ports are already bound, how many relations or calibrations must be added, which sites move, which transitions admit or hold, and how many steps each answer takes. Keep the unchanged boundary read, the perturbed read, and the correspondence side by side. Then capture only the common boundary into a standalone world and repeat. The difference after that capture shows which follow-on depended on live construction and which was already justified by the boundary itself.

I would start with these authored alternatives and a receiver that explicitly asks for one of the follow-ons. It gives us a way to test the more ambitious idea later: can local unresolved demand distinguish which candidate helps, without a catalog of square types or a rule that chooses a configuration by its name?
