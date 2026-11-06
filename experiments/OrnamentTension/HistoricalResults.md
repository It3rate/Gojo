# What can the historical reader distinguish?

We compared eight independently sourced withheld object sketches to all eight training sketches. The question was whether the geometric reader could recover the intended family when it had to discriminate among similar alternatives. This check used no generated candidate programs, ballots or search results. [Browse the paired drawings, object links and full comparison matrix](../../artifacts/local-tension-ornament/history-reference/Index.html).

| Withheld focus | Intended similarity | Margin over best alternative | First choice |
| --- | ---: | ---: | --- |
| Running key | .830 | +.024 | Running key |
| Inward spiral | .963 | +.069 | Inward spiral |
| Zigzag | .708 | −.069 | Opposed chevron |
| Opposed chevron | .916 | +.193 | Opposed chevron |
| Crossing ribbons | .770 | −.031 | Opposed chevron |
| Branching spray | .606 | +.159 | Branching spray |
| Alternating pinline | .770 | +.118 | Alternating pinline |
| Counterstep | .903 | +.020 | Counterstep |

The intended training family ranked first in six cases and second in two. The two diagonal alternatives are informative. Repeated slants can describe chevrons, crossings or a diamond-enclosing border. A still image supplies limited evidence about their construction. The numeric distance also depends on the angular approximations, how much of each register was selected and the nominated period. This instrument is most defensible as one source of evidence alongside timelike continuity and real port incidence.

The training and held-out objects are distinct, but the same team selected the eight families and made the reductions on both sides. The experiment did not learn the historical abstractions themselves. A label such as *running key* here denotes the selected geometric focus. Neither a high score nor a recovered label establishes that an artifact belongs to the historical tradition of the museum object.

To reproduce the figure from a directory containing the three recorded DLLs:

```powershell
python experiments/OrnamentTension/HistoricalSurvey.py artifacts/local-tension-ornament/runtime artifacts/local-tension-ornament/history-reference
```

The script invokes the recorded .NET reference reader to construct the 64 underlying values. [Reference coordinates and scores](../../artifacts/local-tension-ornament/history-reference/packets.json) · [Independent source descriptions and metric definitions](Criteria.md).
