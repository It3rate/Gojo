# What the ornament machines learned

Two strips can look identical and still call for different actions. If a new mark was intended, completing its reflection can repair an ornament. If it was a slip, reflecting it can spread the damage. A nine-byte machine learned to reflect visible marks. Its failure on a slip led to a second experiment in which the reader could consult the owner of the marks through a bound port. With enough ink, a seven-byte machine then completed all 192 reserve outlines. The useful part of the connection can be inspected, disconnected and replayed without a teacher.

Other hoped-for results did much less well. Search learned little about repairing a repeat boundary without a landmark. Learning which mutations to try from the history of earlier searches yielded four inconclusive predictions. Moving two competent one-dimensional machines into a common fabric usually made bilateral symmetry worse.

[Inspect a successful repair and an accident](../../../artifacts/local-ornament/primary/Explore.html) · [See rail and fabric drawings](../../../artifacts/local-ornament/primary/Gallery.html) · [How to replay the machines](README.md)

## What competed

Each candidate owns two small tables. One senses the ordered strip. Its last row publishes a Boolean read over a single connection. Rows in the other table can admit a local transfer of material. A writer may depend on that connection, delay it, or ignore it. Search can rewire ports, replace laws, add or remove rows, change their priority, and rebind transfers. One useful machine fits in seven bytes.

The order of evidence mattered. First we sealed the 240 predictions, starting recipes, judges, budgets and executable. Independent populations then searched in parallel, with up to eight workers. Every proposed program used a matched training bank within its competition. We chose finalists using training and validation data, then opened blind scores. Only after recording that report did we design the three follow-ups. All three sets of finalists were frozen before any of their reserve scores were opened. [Original predictions](../../../artifacts/local-ornament/primary/manifest/hypotheses.csv) · [First report](../../../artifacts/local-ornament/primary/Report.md) · [Common reserve lock](../../../artifacts/local-ornament/reserve-gate.json)

| Competition | Paid program trials | Training executions | Frozen finalists | Evaluation after lock |
| --- | ---: | ---: | ---: | --- |
| Four specialist briefs and one mixed league, four search methods, four replicates | 327,680 | 10,485,760 | 80 | 59,904 blind case/intervention rows, each paired with holding the seed |
| History-based proposer | 49,152 | 1,572,864 | 24 | 2,304 reserve rows |
| Seam landmark | 49,152 | 1,572,864 | 12 | 3,456 reserve rows |
| Authored mirror source | 768 | 294,912 | 3 | 3,456 reserve rows |
| **Total** | **426,752** | **13,926,400** | **119** | Plus 8,064 two-axis compositions in the primary transfer probe |

A paid trial can repeat the same bytes. In particular the seam experiment tested the same 16,384 proposal positions under each of three cue arrangements. The mirror experiment enumerated 256 pairs of Boolean laws per arrangement. The 240 primary comparisons are ten types of question across four briefs and six seed families. A blind cell has twelve distinct held-out parents. Each parent mean combines two views and four training replicates. Parents are unique up to rotation and reflection. Held-out seeds use new constructions within the six familiar families. The larger execution totals measure computational work.

On this machine the primary training command took 8.51 seconds with eight workers and the compiled Gojo tables. Choosing and locking the finalists took another 3.03 seconds. These are timings for 22–36-site rails, one to four Boolean rows per owner and a bounded number of beats. The broader design, auditing and repository checks took longer. [Phase timings](../../../artifacts/local-ornament/primary/timing.log).

## What the blind strip tests say

All 24 zero-edit-budget controls passed. Among the 216 substantive predictions, **125 were supported, 80 inconclusive and 11 contrary** under the registered margins. These are grouped decision rules, not significance tests. In particular, a prediction of equivalence can be supported when a simpler structure does equally well.

| Brief | Evolved specialists, mean utility above idle | Full random search, same budget | Evolved outcomes, better / tied / worse |
| --- | ---: | ---: | ---: |
| Balance, keep count while equalizing gaps | +57.2 | +72.6 | 455 / 76 / 45 |
| Infill, approach a target gap | +232.8 | +208.3 | 502 / 66 / 8 |
| Mirror, preserve marks and complete reflection | +140.0 | +134.7 | 426 / 142 / 8 |
| Seam, repair the gap across a repeat boundary | +4.7 | −4.3 | 55 / 407 / 114 |

Each brief has its own 0–1000 utility. Comparisons use the same external disturbance on the idle strip. In the 24 evolution-versus-random predictions, three were supported, 19 inconclusive and two contrary. A general advantage for evolution remains open. The mixed evolved league averaged +18.0 on balance, +76.2 on infill, −7.6 on mirror and −13.3 on seam. A specialist's success gave little assurance that one shared controller could switch successfully between briefs.

**Infill exposed a problem with the brief itself.** Approaching the requested gap permits thinning a crowded strip. One seven-byte winner, `01104000016003`, uses its writer to recover adjacent ink into stock and ignores the connecting wire. In a post-hoc count across the 576 evolved infill outcomes, 430 finished with fewer occupied sites than idle, 79 with the same count and 67 with more. The mean change was −1.83 sites. The open-loop winners added net ink on zero of 576 comparisons. If the intended goal is to fill empty space while retaining original marks, those two requirements need their own witnesses. [Reproduce this count and check the sealed records](Audit.py).

## Three questions the primary results made worth asking

### Can the reader distinguish an authored mark from a slip?

The primary mirror winner `0104C2004900244374` filled five missing reflections on the window example, taking utility from 500 to 1,000. Cutting its wire held the score at 500. Freezing observations at the initial state also reached 1,000 but cost ten attempts instead of five. On another input, a disturbance at site 3 was copied to site 30. Its live score was 857, compared with 929 when the sensor ignored later changes. A connected wire revealed a real ability and also explained a specific error.

For the follow-up, we supplied two worlds with identical visible ink and different source ownership. The owner exports just the participation of the corresponding source site. Ample and scarce stock were tested separately on the same 48 reserve parents.

| Reader and resource condition | Exact outlines | What happened |
| --- | ---: | --- |
| Current ink, ample stock | 96 / 192 | Copied the diagnostic accidental mark in 96 worlds. |
| Bound authorized source, ample stock | **192 / 192** | Zero unwanted counterparts and zero missed sites. |
| Bound authorized source, scarce stock | 34 / 192 | Zero unwanted counterparts, 618 intended sites missed. |

The bound winner `0100114A006204` uses two NOR laws and a passage from stock to the current site. Within this known-input task it places ink when the corresponding authorized site participates and the local site is empty. Moving the same winner's source binding one position off gave 721 unwanted marks and 793 missed sites under ample stock. Cutting it missed 1,188 sites. All six registered comparisons met their criteria. The experiment supplies the source record and correspondence. Learning consists of choosing how to use that evidence through the wire. [Mirror protocol, figures and runner](../../OrnamentMirrorFollowup/README.md).

### Does knowing where the seam is make it actionable?

The primary seam specialists regressed more often than they improved. The follow-up gave one public sensor port either the original seed, a marker at the repeat boundary, or an equally sparse marker in the interior. For the same seam-trained finalists, the true marker beat the misplaced marker by **64.35 utility points on average**, reaching the 20-point margin on 37 of 48 parents. Only one parent favored the decoy by that margin. The other three predictions were inconclusive. For example, seam-trained machines beat idle by 42.98 points on average, but reached the margin on only 22 parents. A particular connection can matter even when the training method has yet to produce a reliable repair across the cohort. [Seam outcomes](../../../artifacts/local-ornament/seam-location/reserve/verdicts.csv).

### Can histories teach the next search where to look?

We counted which mutation families had improved their parents in earlier training, retaining failures in the denominator. A fresh proposer used these counts to select mutations. It competed with uniform selection and a control that shifted the same preferences onto other mutations. Relative to uniform, the history-based proposer averaged **−2.04 on infill and −2.96 on balance**. Relative to shifted preferences it averaged +23.46 and −12.97 respectively. None reached the required margin in 32 of 48 reserve parents. Utility of this form of self-improvement remains unestablished. [All four proposer predictions and outcomes](../../../artifacts/local-ornament/proposal-transfer-v2/reserve/contrasts.csv).

## What transferred into a drawing

A horizontal machine and a vertical machine were trained on separate strip tasks. A third structure read one participation from each and spent its own stock at their crossing. We tested six protected, glyph-like seeds, changed the crossing laws, disconnected signals and altered their timing. All 8,064 outputs preserved protected ink and accounted for the crossing writer's stock. Switching OR to XOR changed outputs in all six seed families. The prediction that learned strip controllers would improve bilateral symmetry was contrary. Average mirror mismatches increased in every family. The pictures demonstrate a causal composition, with an unresolved problem in learning a shared aesthetic objective. [Inspect the fabric pictures](../../../artifacts/local-ornament/primary/Gallery.html).

## Where the boundary is

The generator learned port choices, Boolean laws and material passages inside a bounded grammar. The choice of goals, available senses, unit-step substrate, opportunities to act and endpoint bindings between domains were supplied. “Reflector” and “thinner” are short human descriptions of inspectable computations. The primary machines can be reloaded and reused from their bits. Acquisition of a shared vocabulary of callable structures across populations remains a further experiment.

A post-hoc census found 65 distinct byte strings among the 80 finalists. Repeated whole programs all came from the fixed-wiring control. Among the 40 full-grammar random and evolved finalists, four sensor functions and five writer decisions recurred on fully known inputs, although the connected sensor-output and writer-guard circuits had different encodings. For example, two mirror sensors published the complement of the reflected occupancy. One computed it in a single row. The other routed it through an intermediate row. This is a concrete candidate for a shared word. The census leaves open whether their timing and behavior under uncertain inputs agree, or whether naming the operation would improve future searches. [Circuit census](../../../artifacts/local-ornament/verification/vocabulary-census.csv) · [Scope of the equality read](../../../artifacts/local-ornament/verification/vocabulary-census.json).

## Can a reader actually help a different writer?

We plugged each of the 40 full-grammar finalists' readers into each finalist's writer. The receiving writer retained its scan direction and delay. These 1,600 assemblies ran 368,640 times on the **already-seen primary test seeds**. The comparison rules were written before making the exchanges. Reassembling each machine with its original reader reproduced all 9,216 corresponding primary case records, including their traces.

Of 2,496 foreign-reader comparisons, **seven decisively improved** on the writer's original partner and **1,478 decisively worsened**. Here a comparison is one donor, receiver and brief. A decisive change needed at least 20 points in the same direction on 48 of 72 parents. Four substitutions also decisively beat cutting the wire and exceeded idle on average. All four paired a mirror-trained reader with the same mixed-goal writer on the mirror brief. The nine-byte reflector's reader was one of them. With that writer it gained **197.41 points on average** over the original connection and improved by at least 20 on **all 72 parents**.

That seven-byte assembly, `01104249002304`, does something quite specific. The donor sees a reflected mark and an empty site here. The receiving writer deposits on the transition from an absent to a present signal. Keeping both parts fixed and changing the brief cost 474.12 points on balance, 146.99 on infill and 211.80 on seam, relative to the original connection. The same physical edit can serve one goal and damage another.

There are also many ways to appear interchangeable. Of the 304 substitutions producing exactly the same final material as the original on every case, 183 changed at least one execution trace. Three distinct random mirror sensors produced the same series of final patterns in the successful receiving writer. One-bit parts can work together across independently trained machines, with compatibility depending strongly on the recipient and goal. Estimating how often a *chosen* substitution helps requires fresh parents. [Explore all owner exchanges](../../../artifacts/local-ornament/owner-exchange/Explore.html) · [Exchange protocol and runner](../../OrnamentOwnerExchange/README.md).

For the next competition I would let training nominate a small library of readers that have worked with more than one writer. On fresh motifs, give eligible writers three separately observable obligations: retain an authored stroke, fill an empty span, and stay within stock. Change the active goal while holding the starting picture fixed. Compare borrowing a reader with spending the same training budget on a new one. That would test whether retaining a piece of vocabulary actually helps future machines learn.

## Verification and record keeping

A second primary search started in an empty directory with four workers. It evaluated the entire 10,485,760-episode training schedule again and reproduced the first run's exact finalist file, including all 80 final history hashes. This verification is excluded from the competition totals above. [Replication record](../../../artifacts/local-ornament/verification/primary-replication.json).

All **2,304 primary cold replays** matched direct reference evaluation byte for byte, including hashes of the execution trace. Fresh processes also reproduced the 24 proposer examples and 36 mirror examples. All 3,456 recorded seam cases matched on ink, stock, edits, attempts and blocked passages. Repeating these cases and their controls through both execution paths gave 7,200 matches, including trace hashes. The original seam case files recorded the material results only. [Seam replay certificate](../../../artifacts/local-ornament/verification/seam-replay-certificate.json).

The solution's 6,350 tests passed across 16 test projects. The browser checks exercised all 240 primary comparison selections, all 2,880 parent rows, the eight displayed mirror conditions and both galleries. The exchange heatmap was checked against the recorded numbers for all 2,560 comparisons. [Test totals](../../../artifacts/local-ornament/verification/solution-tests.json).

Held-out figures, inputs, program bytes and hashes are available in the [compact evidence archive](RecordedEvidence.zip). The [complete local archive](../../../artifacts/local-ornament/CompleteRun.zip) includes generation-by-generation evidence for unsuccessful programs as well.

One proposer registration stopped before training because its ledger importer assumed an empty initial history. The corrected importer verified the actual bound start and all 512 generations from its eight source populations before a separately sealed rerun. The first seam registration attempt stopped on the text file's byte-order mark before issuing a seal. Both fixes preceded the reserve assessment. The primary blind report and its fingerprint were preserved.
