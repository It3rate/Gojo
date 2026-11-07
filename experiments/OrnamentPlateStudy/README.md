# A printed fret as a training reference

Meyer’s plate 81 lays out an angular course on a construction grid and shows both banks of its band. We treated the center between those banks as one possible path. The first row is a six-stroke course. The second is an eight-stroke course with a second inward turn. This experiment asks whether exposure to the first gives a machine useful structure for drawing the second, and whether a stored local pressure helps or harms either read.

A later [two-machine comparison](../OrnamentFlowPilot/README.md) uses the Figure 1 control to ask what happens when the same printed line comes from two different networks of connected pen outlets.

The first figure becomes a six-row native Gojo path program when a teacher is allowed to supply it. Two other arms begin with mutations of the eight named engines. One selects them by measured agreement with the first printed figure. The other measures generic construction properties. All three arms also adjust a local pen/machine/frieze pressure policy. Selection and judging stay outside the saved machine. The contour comparison uses intersection and union of exact continuous intervals on each supporting line. The chronology read follows the actual owner and site identities. Neither read needs pixels.

Both figures were inspected and measured by the investigator to define the references. Candidates and the trainer are blind to figure 2 during search and validation. A fit establishes agreement with this registered interpretation of the ink. Historical stroke order is unknown. A second figure from the same page also has a close stylistic relationship to the first.

| Figure | Inferred ordered centerline on the printed grid | Native bound extent rows |
| --- | --- | --- |
| 1 | N4, E4, S2, W2, S2, E4. Period 6. | A, A, B, B, B, A. |
| 2 | N6, E6, S4, W2, N2, W2, S4, E6. Period 8. | A+B, A+B, A, B, B, B, A, A+B. |

With A=4 and B=2 the exact reconstructions use six and eight rows. One body in this grammar can hold up to eight. A more involved fret or multiple independent courses is a separate question. Stretching the outside period, independently rebinding A and B, or changing an endpoint address can invalidate a seam. A two-bank display of the reconstructed centerline uses an explicitly chosen broad pen width. The display adds no second pen to the machine.

## Local verification

The [prospective design](Protocol.md) fixes the training and transfer figures, budgets, margins, controls and prospective tests. The executable also records both reference and pressure-control scores. Run from the repository root:

~~~powershell
dotnet build experiments/OrnamentPlateStudy -c Release
$runner='experiments/OrnamentPlateStudy/bin/Release/net10.0/OrnamentPlateStudy.dll'
dotnet $runner freeze NEW_RUN
dotnet $runner train NEW_RUN
# After independent approval and a release containing the finalist SHA:
dotnet $runner assay NEW_RUN RELEASE.json
python experiments/OrnamentPlateStudy/Gallery.py NEW_RUN artifacts/ornament-tension-sources
~~~

The frozen finalists are native bytes. The actual pen needs only those bytes, its tension policy and the realization inputs. [Compare the printed banks and executed paths](../../artifacts/local-tension-plate/primary/Gallery.html).

## Recorded results

We froze twelve finalists after 6,144 paid proposals and 24,576 native training drawings. The figure-2 release was subsequent to the finalist lock. The 1,344 final comparisons include both figures, twelve physical bases, two slot budgets, two feedback conditions, three arms with four finalists each and two separate authored controls.

| Live arm | Figure 1 historical utility | Figure 1 exact ink | Figure 2 historical utility | Figure 2 exact ink |
| --- | ---: | ---: | ---: | ---: |
| Supplied figure 1 + historical teacher | .620 | 16/96 | .426 | 0/96 |
| Named catalogue + historical teacher | .469 | 0/96 | .387 | 0/96 |
| Named catalogue + general teacher | .297 | 0/96 | .303 | 0/96 |

The two exactly authored programs attained exact line support on all 16 matched-ratio/slot checks under quiet actuation. In the same matched cases, turning on their default live policy perturbed all those exact drawings. Learned pressure policies changed some deposited ink in **12/12** transfer parents. Across the 12 trained finalists the live-minus-quiet mean historical transfer advantage was only **0.0046**. A change being available does not make a local pressure report evidence of historical correctness.

All five prospective comparisons were supported. The positive result for transfer is a relative difference in coarse similarity. **None of 288 live drawings by trained finalists reproduced figure 2 exactly.** The additional inner return appears in the eight-row authored control and was absent or incorrectly placed in the trained programs. The investigator supplied both reference transcriptions. The first-figure prior is real construction knowledge and is credited accordingly.

An independent reader divided the 56 exhibited centerline histories into elementary vectors and separately enumerated matching direction subsequences. All their line-overlap, clean-deposition, chronology, seam and historical utility values matched the scored records. The verifier also checked all final table hashes, 1,344 unique case positions and all 12 ledgers of 512 candidates. The local gallery was exercised in Edge across 16 figure/replicate/feedback combinations. [Printed comparison](../../artifacts/local-tension-plate/primary/images/PrintedComparison.png) · [All predictions](../../artifacts/local-tension-plate/primary/final/verdicts.json) · [Independent audit](../../artifacts/local-tension-plate/primary/verification.json).
