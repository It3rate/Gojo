# Timing and the useful outlet

A machine has a missing seam and two connected pens. One can bind its next stroke to the next bay. The other's final stroke is already bound at an intentional terminal. Will the machine eventually send more of its pressure to the pen that can use it? This study uses the unchanged tournament runtime. It injects no hints, rewards or corrective instructions. The missing physical seam creates the pressure, and its later local read is the source of credit. [Registered fixtures and predictions](Protocol.md).

**Yes under a particular timing; the extra traffic did not buy more completed seams.** Across the early-action fixtures with equal starting strengths, the live machine sent an average of 91.7% of its late admitted seam units to the useful pen. The fixed-strength control sent 55.3%. Under both, that pen completed 31.3% of its seam opportunities. It repaired different bays, giving different final line material in 44 of the 48 matched drawings. [Follow the received units and actual joins](../../artifacts/local-tension-ornament/route-diagnosis/Timeline.png).

## What changed and what failed

| Condition | Late useful share, live / fixed | Useful seams settled, live / fixed | Useful relief / miss in live |
| --- | ---: | ---: | ---: |
| Can bind, early act, uniform routes | .917 / .553 | .313 / .313 | 638 / 0 |
| Can bind, same-beat act, uniform routes | .206 / .553 | .179 / .313 | 0 / 330 |
| Cannot reach seam, early act, uniform routes | .500 / .500 | .000 / .000 | 0 / 0 |
| Can bind, early act, wrong prior | .000 / .000 | .000 / .000 | 0 / 0 |

The averages include missing opportunities as zero as registered. In the early reachable test, 11 of the 12 bases produced a confirmed useful receipt and subsequent admissions. On the twelfth, the upward base stroke already exceeded the permitted vertical slots. Without a participating stroke the machine's seam reader had no obligation to repair. The two owner placements separately supported the registered late-traffic advantage. With the useful pen in position zero the advantage averaged .223, and in position one .504.

After each matched live run's first actual relief, the learned router sent 2,948 units to the useful pen and 88 to the bound pen. Fixed routes over exactly those same time intervals sent 1,958 and 1,529, respectively. Despite this strong shift in allocation, total useful seam repairs matched in every individual early comparison. Pen debt averaged 1.08 versus 1.31. Machine debt averaged 1.83 in both. The first diagnosis shows a redistribution of responsibility, not an improvement in the original seam task.

**The same actuator can be unfairly punished by its observation schedule.** On the first basis, the early pen acted at beat 30 and the machine confirmed relief at beat 31, increasing the route strength from one to two. With the last pen row delayed one beat, the pen repaired at beat 31, coincident with the machine's reading. This router requires a strictly later read for credit. At beat 39 the next bay was missing the seam again; the receipt was recorded as a miss and strength fell from one to zero. This is a causal limitation of the current receipt law. An unreachable outlet never earned positive reinforcement. A prior that directed pressure first to the already bound terminal could also prevent the capable pen from acquiring any evidence.

The downward cut blocked repairs as expected. The registered prediction that cyclic misbinding would also block every repair was contrary. A packet can relay long enough for three cyclic lane shifts to restore a seam request. On the first basis the machine's seam demand at beat 7 initially arrived at the pen on the extent lane at beat 9. Subsequent relays visited turn and seam lanes. A seam unit reached the useful pen at beat 42, another at beat 45, and the pen drew on the seam lane at beat 46. No useful route relief was credited under that early misbinding sample. A wrong interpretation at one boundary does not guarantee that the entire loop is disabled.

The registered headline decisions were four supported, one inconclusive (more repairs in the early fixture), and one contrary (the combined disabled/misbound control). Two additional position-specific readings of the routing hypothesis were both supported. These are tightly constructed diagnostic controls on one topology, not historical quality tests or learned ornament claims.

## Verification

The [registration](../../artifacts/local-tension-ornament/route-diagnosis/seal.json) fingerprint is **A3CBC7A5C2DD4A4B42D9A13C6BA0774F6CD9BBD0A332E0A83777E88229743B03**. All 288 physical configurations in six modes were evaluated and all 1,728 fresh native replays reproduced the recorded material and routing statistics. The chart compares 32-bay draws; the same design tests 56-bay draws. The reserve learning and judging archives play no role in settlement.

[All mode and timing summaries](../../artifacts/local-tension-ornament/route-diagnosis/summary.csv) · [Parent-level verdicts](../../artifacts/local-tension-ornament/route-diagnosis/verdicts.json) · [Actual exemplar timelines](../../artifacts/local-tension-ornament/route-diagnosis/examples/early-Live.json).

From the repository root, run the recorded runtime from the local archive with **verify** and the run directory. **freeze** makes a fresh registration, **run** evaluates the fixtures and **verify** replays every recorded case. The renderer **Report.py** reads the saved cases and witnesses.
