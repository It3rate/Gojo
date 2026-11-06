# What the published drawing histories actually contain

Can a machine earn a good score by naming many “branches” while drawing an ordinary line? We inspected the finished graphs of the 60 published examples. An array position is a site identity. An endpoint that lands geometrically on another stroke can still belong to a completely different site. An attachment to the beginning of the next repeat can also be a single line continuing straight through a seam. The count of instructions executed tells us where to look; the resulting connections determine what was built.

## What survived in the drawings

These counts are for the 29 distinct full finalist/program/input exhibits in this selected gallery. The gallery favors several highest, lowest and first examples. Its frequencies describe those exhibits only.

| Feature in at least one place | Exhibits |
| --- | ---: |
| Stroke borrowed a source site different from the pen's current cursor | 22 |
| Stroke ended at an already bound site | 28 |
| Three or more distinct geometric arms incident on one actual site | 22 |
| Three-arm joint with at least two outgoing directions | 20 |
| Three-arm joint with at least two incoming directions | 7 |
| Two private owners actually connected to the same drawn site | 20 |
| Identity-connected graph contained a cycle | 7 |
| Two distinct active sites projected to the same X,Y position | 17 |
| Two distinct straight segments crossed strictly inside both | 3 |
| Local law changed a bit in its owner's writable frame | 5 |
| Final frame bytes differed from the inherited program | 4 |

The first **stepped-lace** example made 15 strokes. It has three directed forks, three directed mergers, four sites shared between owners and cycle rank six. The first **countermarch** example executed three borrow events and six target attachments. Its material has zero three-arm sites. Six different projected positions each hold multiple distinct site identities. Treating its nine events as nine ornamental branch joints would lose the distinction the encoding was designed to preserve.

For each of the five validation-selected first drawings, we compared the actual union of straight ink spans before and after intervening on its local ports. Cutting the incoming foreign port changed ink in **turning-key**, **branched-spray** and **stepped-lace**. Freezing the writable frame changed ink in **branched-spray** alone. Its best published realization changed its frame four times, leaving its final bytes identical to its starting bytes. The path performed intermediate work even when the change canceled by the end.

## What was checked

Every one of the 60 exhibits had an ordered draw witness and one funded row opportunity per material stroke. The endpoint displacement matched the encoded direction and shared extent under its actual realization. Replaying the local write witnesses accounted for every changed final program byte. Each owner receives one opportunity per beat; the experiment supplies no common ink reservoir, line width, elasticity, friction or stroke-energy price. A rendering can show a physical connection, a crossing, or two sites coinciding in the calibration, and those have separate evidence.

The directed fork and merger counts require three distinct incident directions at one site. A V-shaped endpoint can have two entering paths with no outgoing stem; this test deliberately leaves that case out. Graph cycle rank counts distinct undirected identity bonds. It does not count the number of enclosed visible regions. A strict interior-crossing pair excludes endpoint contacts and collinear overlaps.

The standalone reader verifies published exhibits after assessment. It does not train, select or replay a candidate. From the repository root:

```powershell
python experiments/OrnamentPathCampaign/HistoryAudit.py artifacts/local-path-ornament/wave1/exhibits.json artifacts/local-path-ornament/history-audit
```

[Individual histories](../../artifacts/local-path-ornament/history-audit/history.csv) · [Counts and matched controls](../../artifacts/local-path-ornament/history-audit/summary.json).
