# What changed when the machine had to draw a path?

The machine can now earn its result by drawing a continuous course, leaving it at a real joint and attaching another stroke. That let the experiment distinguish two borders that earlier judges treated alike: a pen winding back toward an earlier part of its course, and several independent lines that happen to make a row of boxes.

The **[design sampler](../../artifacts/local-path-ornament/sampler/Explore.html)** is a good starting point. It shows twenty machines from the paid training population at two different settings of their extent slots. Edges, crossings, forks and apparent contacts can be inspected through their recorded site identities. Each period on the page was actually executed on an initially blank strip. [Printable sampler](../../artifacts/local-path-ornament/sampler/Sampler.svg).

## What the first tournament established

Twenty-eight judges read five briefs at six different perspectives. We registered the design and executable before searching. The 280 populations tried **120,960 paid candidate programs on 483,840 training drawings**. These are trials rather than distinct inventions. Four independent searches per brief and arm selected finalists using validation alone. The 20 full-judge finalists were distinct and occupied 16–44 bytes each. Their median size was 25 bytes. Repeating the competition in a new directory with four workers instead of eight reproduced every finalist and all 280 compressed search histories.

The blind assessment compared the locked machines and controls on twelve new extent/pitch bases with two realizations each. Among the **85 overall predictions**, 63 were supported, 14 inconclusive and eight contrary. The 340 further questions restricted those same comparisons to smaller groups of related inputs. The most substantive distinction was between optimizing the ground around the lines and optimizing the complete ornamental brief. The latter improved the registered full score on all five briefs. Across briefs, full selection averaged **0.851** on the 0–1 utility scale, compared with **0.600** for field alone. The full judge also favored the very qualities it had trained for. Agreement with the drawings people might prefer needed separate examination.

| Brief | Full judge | Field alone | Most effective tested pair of perspectives |
| --- | ---: | ---: | --- |
| Quiet border | 0.861 | 0.810 | Seam and rhythm, 0.797 |
| Turning key | 0.885 | 0.570 | Seam and rhythm, 0.872 |
| Countermarch | 0.831 | 0.448 | Symmetry and rhythm, 0.835 |
| Branched spray | 0.841 | 0.581 | Topology and craft, 0.779 |
| Stepped lace | 0.838 | 0.590 | Topology and craft, 0.700 |

**Which criteria helped depended on the job.** Retraining without seam cost the turning key 0.155. Omitting symmetry cost countermarch 0.135. Omitting topology cost branched spray 0.155 and stepped lace 0.136. Quiet border actually scored higher after leaving topology out. Simpler combinations can also beat the 28-criterion search under the same budget, as symmetry and rhythm did on countermarch.

We counted how often an individual reading reversed a parent–child numerical preference. Avoiding false joins did so 172 times, parallel reflection 104, avoiding overdraw 96 and accent alternation 87. Band reach tipped only four comparisons. A judge may have few such occasions because the starting programs already satisfy it, or because other judges agree with it. [All 28 readings, their individual counts and the full combination table](MeasurementResults.md).

## What the paths actually did

In 29 distinct full-machine examples selected for inspection, 22 contained a site with three or more distinct incident directions. Twenty had a directed fork, seven a directed merge, seven a cycle of shared identities, and 17 had some coincident positions with different identities. The selected stepped-lace example recorded three forks and three merges across its owners. Another machine recorded six successful target bindings but had zero three-arm sites. A target-binding event alone cannot establish a visible branching motif. [Construction examples](../../artifacts/local-path-ornament/wave1/ConstructionSheet.png) · [History audit](HistoryAudit.md).

Cutting the incoming communication on the same full machines cost 0.177 full utility on average. For branched spray the score fell from 0.841 to 0.548. The countermarch average rose slightly with the wire cut. There is one stroke opportunity per owner and beat, with explicit blocked and unresolved requests. This experiment does not impose one conserved ink stock shared by all pens.

## The drawings caught a problem with the judges

The first anonymous panel included twenty side-by-side comparisons. Two AI reviewers saw the same brief and actual strokes at the same scale. Neither saw the genomes, scores or machine origins before sealing a choice. They agreed on a non-tied direction 16 times. The registered brief preferred that side in 12 of those 16 cases.

We fitted a small external judge using just six perspective differences and the first ballots. Leaving one whole brief out at a time also gave 12/16. We then locked its weights, chose twenty new pairings and opened new physical bases. The original brief and the fitted judge each agreed with **10 of 14 unanimous choices** on the new panel. The prediction of a two-pair advantage for the fitted judge was inconclusive. The two observers share a model family. The result supports neither a human preference rate nor a general aesthetic standard. The gallery can export a separate human ballot for future experiments.

One disagreement is worth seeing. Both reviewers chose the opposing diagonal figures for a countermarch. The full judge gave the narrow rectangular slots **0.467 more utility**. The simplicity, regularity and measurable symmetry of the slots served the numeric goal while obscuring the sense of counter-movement in the named brief. [See the two drawings at equal scale](../../artifacts/local-path-ornament/preference/PreferencePair.png) · [All reserve choices and predictions](../../artifacts/local-path-ornament/preference/reserve-result.json).

## Changing what a judge can witness

A ladder could score highly as a turning key. We added a reader that checks whether *one connected pen course* travels back horizontally and makes a sequence of directed quarter-turns. A second reader requires a real side arm and a continuing stem at the same site. We compared the original judge, each specialized reader alone, and a blend, using another **98,304 candidate trials and 393,216 training drawings**. All 24 finalists were locked before reserve assessment.

On the reserve returning-key task, original selection scored **0.000** on the new history reading. Blended selection scored **0.910**, while retaining 0.890 on the original scale compared with the baseline's 0.936. On branched spray the history reading rose from **0.205 to 0.897**, with an original-scale cost from 0.827 to 0.753. The changed judges selected different *machines*, which drew after the judges were disconnected. Original and specialized scores describe different benefits.

The render exposed another limit. A pen could turn around a little box, revisit the same position, then proceed. That satisfied several directional turns while looking like box tabs. The returning-course witness recognized genuine material order, but could still miss the open passage that makes a running meander recognizable. [Compare actual drawings from all four new searches](../../artifacts/local-path-ornament/shape-refinement/Gallery.html) · [Refinement measurements](../OrnamentPathRefinement/README.md).

### An open return and a shared seam

Across the 288 key drawings in the first refinement, a separate inspection found **zero strong bays with a genuinely open return**. We therefore locked a final search on 22 new normalized extent/pitch combinations, disjoint from the earlier ones. It compared the earlier blended reader with one that additionally required clearance along an open return and an actual handoff across a seam. A seven-row feasibility control was withheld from search. A box with a departing lead served as a counterexample. The eight finalists came from 32,768 paid proposals and 196,608 training drawings.

On the final twelve bases, all **96/96 drawings** by the open-course finalists exhibited strong local returns and shared handoffs. The matched earlier judge produced strong local returns on 72/96 and shared handoffs on **0/96**. Mean open-course utility rose from 0.301 to 0.879. The original broad score fell from 0.893 to 0.796.

A final audit followed the *entire* band. In **48/96** enhanced drawings, one chronological course made a strong return in every bay. **Zero of 96** had such a whole course free of self-contact. The supplied feasibility control achieved this on 12 of 24 realizations. One winning pen descended into a seam and its next bay rose over the stroke it had just drawn. That is a concrete reason to give a higher-level observer the history of more than one bay. [Open-course drawings and controls at the same scale](../../artifacts/local-path-ornament/open-meander/Gallery.html) · [Exact law and fresh-cohort results](../OrnamentOpenMeander/README.md).

## What the writable frame learned

Three of the twenty full finalists changed a retained frame byte during a fixed warmup. We kept their final program bytes and reset all other state. In the next drawing, one of those changes reflected the visible ink about the horizontal axis on all twelve reserve bases. Its goal score stayed the same. Across all twenty finalists, carrying the changed byte produced **zero mean gain over starting fresh**. In the three changed machines the warmup also left exactly the same frame bytes as the blind-toggle control. The final bits cannot tell which route wrote them. This establishes a small mechanism for persistent, local self-modification. A policy for deciding *when* it helps remains open. [Compare the drawings](../../artifacts/local-path-ornament/retention/comparison.svg) · [Retention measurements](../OrnamentPathReview/RetentionResults.md).

## Scale and verification

The three searches together charged **252,032 program trials and 1,073,664 training drawings**. Validation, blind testing, controls, replication, visual review and sampler renders are additional work, and the numbers above exclude them. A clean directory containing only the saved programs, inputs and runtime reproduced all 360 selected primary trace digests. The repository's 6,362 tests passed across 17 test projects. The primary gallery was checked against all 1,191 rendered stroke projections and 2,458 events, and the scoring apparatus passed 18 separately constructed checks. The runnable evidence, seal identities and portable programs are available through the [local run guide](README.md).
