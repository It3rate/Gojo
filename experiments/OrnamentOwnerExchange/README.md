# Exchanging learned ornament owners

Can a writer use a sensor trained with a different partner? This local diagnostic changed just the sensor rows in 40 frozen ornament machines. Every writer kept its own direction of work and delivery timing, and every sensor published its ordinary single-bit signal. The 40 × 40 wiring combinations ran on the primary test seeds already used to assess these machines. The study design was saved before running the combinations.

The clearest case paired a mirror sensor with a mixed-brief writer. The sensor marked an empty spot when its reflected partner held ink. The writer already knew how to transfer stock to a spot when its input changed from absent to present. On the mirror task, the pair improved by 197 points over that writer's original partner and by 201 over the disconnected writer, on a 1,000-point scale. Every one of 72 paired parent recipes gained at least 20 points over both controls. The same pair lost 474, 147 and 212 points relative to its original partner on balance, infill and seam, respectively.

Among 2,496 foreign sensor/receiver/brief combinations, 2,192 affected the material outcome in at least one test case. Of the 304 that always matched the original material, 183 still changed the wire trace. Seven combinations gave a decisive improvement over the original and 1,478 gave a decisive loss. Four met the design's stronger reuse criterion. All four used the same mixed writer on the mirror task. Three separately encoded sensors produced exactly the same material outputs in that writer. Zero hard violations were observed. Material equality covers final ink, stock and accepted edits. It can hide a difference elsewhere in the timeline.

Run this from the repository root after recovering the frozen primary artifacts. The project loads the archived primary assembly when available. A fresh checkout can compile against the source project, but any run must match the primary protocol's assembly hash. Supply a new output folder.

~~~powershell
dotnet run --project experiments/OrnamentOwnerExchange -c Release -- artifacts/local-ornament/primary artifacts/local-ornament/owner-exchange 8
python experiments/OrnamentOwnerExchange/Summarize.py artifacts/local-ornament/owner-exchange
~~~

The output includes the pre-run design and its hashes, complete owner genomes, 2,560 panel summaries covering 368,640 executions, the replay certificate for all 9,216 native diagonal records, and the checked summary. All four individual briefs are retained for every mixed writer. Each panel covers 144 cases, two sibling forms of 72 parent recipes. These are comparisons within a single finite, previously assessed bank. They support a concrete example of useful reuse and a direct test of its limits. Generalization to new seeds remains open.
