# Can the frieze ask the pens for more or less ink?

A machine receives the same geometry three times. At two fixed beats the frieze receives three positive reach units, nothing, or three negative reach units. We ask whether that input changes the length of actual unique ink in the requested direction. A long retraced segment pays no extra credit. Only the eight-byte feedback policy is trained. Each machine's underlying named path, its pressure network and the physical inputs are kept fixed within a comparison. A larger line budget may or may not help this particular design. [Three-way drawings](../../artifacts/local-tension-ornament/density/Gallery.html).

## What happened

We trained 32 finalists across the running key, inward spiral, crossing ribbons and branching spray. Two arms used four replicates and 512 paid policies per island. All **16,384 proposals** saw all three requests on six training settings, giving **294,912 training drawings**. One arm selected for undisturbed form. The other gave 55% influence to form under the two requests and 45% to the signed density change. Both used the same mutation grammar, search streams and shortlist size. All 32 finalists were locked before the shared reserve gate opened.

| Engine | Response-trained | Form-trained | Gain | Parents beyond .08 | Default policy | Fixed strength | Turn lane | Misrouted | Direct to pen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Running key | .897 | .603 | +.293 | 12/12 | .971 | .895 | .486 | .518 | .902 |
| Inward spiral | .899 | .590 | +.309 | 12/12 | .842 | .898 | .469 | .610 | .913 |
| Crossing ribbons | .837 | .546 | +.291 | 11/12 | .751 | .837 | .507 | .587 | .847 |
| Branching spray | .956 | .556 | +.399 | 12/12 | .901 | .960 | .526 | .447 | .963 |

The score has neutral point .5, one pitch of extra unique length gives 1, and one pitch of loss gives 0. Rows describe average scores across settings, so a saturated score cannot quantify a larger difference. The frieze and lower structures can communicate useful signed demands. Every quiet and downward-cut pair produced identical material across all three requests. Sending the request on the turn lane generally lost the desired response despite sometimes visibly changing ink. Freezing the route strengths retained almost all of the response. This experiment establishes causal use of routed pressure; learning the routing strength gave little extra benefit on this task.

Fourteen of the sixteen preregistered comparisons were supported. The form-equivalence predictions for key and ribbons were inconclusive. The key lost .043 in broad form under the two requests (seven of twelve parents passed the adverse .035 margin), while the ribbons gained .051 (seven passed the favorable margin). The authored default key already achieved a stronger .971 response than its learned .897. The explicit response instruction selected differently from form-only training, but the entire improvement cannot be attributed to inventing a capability from scratch.

## Limits observable in the lines

Positive requested ink exceeded negative requested ink in **1,059 of 1,152** paired response-trained live trials. In 61 it went the wrong way, and in 32 the lengths were effectively equal. A post-hoc stronger check requires that the no-request drawing lie between the other two. This held, with some genuine change, on 931 trials. Both inequalities were strict on only 686 trials. A high plus-minus response can therefore coexist with poor control around the neutral baseline.

The extreme examples show both sides. A spray produced 207.9 units of unique ink on negative, 241.7 on zero and 252.5 on positive. The most adverse selected spiral produced 198.0, 96.0 and 120.0 respectively. The path can change direction, miss a handoff, or hit a slot boundary; adding a short reach at one moment cannot guarantee greater whole-pattern density. The pressure input asks a question about available geometry, rather than assigning the finished pattern a length. [Extremes at the same scale](../../artifacts/local-tension-ornament/density/Largest%20and%20smallest%20changes.svg).

## Inspection and replay

Each of the 72 reserve settings combines one of twelve new bases, two views and a 2-, 5- or 9-slot field. We crossed all finalists with seven interventions, plus four authored controls. This produced **16,416 matched triples and 49,248 native reserve drawings**. The cases retain program bytes, realized unique line length, original broad score, uninterrupted owner-course fraction, historical reference similarity, action/relief count and hashes for both projected material and the full chronological trace.

A fresh executable working directory replayed every program, input and pressure injection. All 49,248 trace hashes, independently counted unique lengths, broad/historical/continuous reads and action/relief counts agreed. A separate Python readback recovered every grouped verdict from the raw rows. [Verification certificate](../../artifacts/local-tension-ornament/density/verification.json) · [All predictions and outcomes](../../artifacts/local-tension-ornament/density/reserve/verdicts.json) · [Matched parent effects](../../artifacts/local-tension-ornament/density/reserve/groups.csv).

From the repository root, build the project with `dotnet build experiments/OrnamentDensityResponse -c Release`. The runner supports `freeze RUN`, `train RUN [workers]`, `verify RUN` and `assay RUN SHARED_RELEASE.json`. Use a new directory for each registration. The source assembly is matched to the primary run; the executable refuses to open results against a different pressure kernel. The exact release used here bound finalist SHA-256 `3C60ECC2CA8DFEE7238FEA96B5297FCEC3A06B684683453DAB9B6E220AD8AC1E` under registration `72482E5A350563E3471751CE17304CC667B67B21F43E2B204ED2E48AE42A91D4`. `Verify.py RUN FROZEN_RUNTIME_DIRECTORY` replays recorded cases, and `Gallery.py RUN` renders them. [Frozen design in readable form](Protocol.md).
