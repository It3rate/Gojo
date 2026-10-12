# Primitive learning: saved-run analysis

Intervals are two-sided 95% Student t intervals over independent seed means or paired seed differences. Episodes are not treated as independent replicates. Intervals describe this seed population; they are not corrected for multiple exploratory comparisons or a guarantee outside the authored task.

## pilot-02

Source: `D:\source\repos\It3rate\Gojo-phototaxis\artifacts\primitive-learning\pilot-02`. 2 seeds starting at 0; 640 training and 256 test episodes per run.

### Main test results

| Variant | Return [95% CI] | Correct | Coverage | Covered accuracy | Query rate |
|---|---:|---:|---:|---:|---:|
| A0P0Q0S0 | 0.17 [-1.61, 1.96] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| A0P0Q0S1 | 0.17 [-1.61, 1.96] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| A0P0Q1S0 | -0.59 [-1.81, 0.62] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| A0P0Q1S1 | -0.59 [-1.81, 0.62] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| A0P1Q0S0 | 0.17 [-1.61, 1.96] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| A0P1Q0S1 | 0.17 [-1.61, 1.96] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| A0P1Q1S0 | -0.59 [-1.81, 0.62] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| A0P1Q1S1 | -0.59 [-1.81, 0.62] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| A1P0Q0S0 | 0.17 [-1.61, 1.96] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| A1P0Q0S1 | 1.69 [1.69, 1.69] (n=2) | 71.09% [71.09, 71.09] (n=2) | 100.00% [100.00, 100.00] (n=2) | 71.09% [71.09, 71.09] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| A1P0Q1S0 | -0.59 [-1.81, 0.62] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| A1P0Q1S1 | 3.17 [0.97, 5.38] (n=2) | 99.22% [89.29, 109.15] (n=2) | 100.00% [100.00, 100.00] (n=2) | 99.22% [89.29, 109.15] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| A1P1Q0S0 | 0.17 [-1.61, 1.96] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| A1P1Q0S1 | 1.25 [-4.31, 6.81] (n=2) | 65.62% [-3.86, 135.11] (n=2) | 100.00% [100.00, 100.00] (n=2) | 65.62% [-3.86, 135.11] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| A1P1Q1S0 | -0.59 [-1.81, 0.62] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| A1P1Q1S1 | 3.17 [0.97, 5.38] (n=2) | 99.22% [89.29, 109.15] (n=2) | 100.00% [100.00, 100.00] (n=2) | 99.22% [89.29, 109.15] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| always-query | 3.00 [3.00, 3.00] (n=2) | 100.00% [100.00, 100.00] (n=2) | 100.00% [100.00, 100.00] (n=2) | 100.00% [100.00, 100.00] (n=2) | 100.00% [100.00, 100.00] (n=2) |
| inverted-fit | -4.70 [-8.50, -0.90] (n=2) | 0.78% [-9.15, 10.71] (n=2) | 100.00% [100.00, 100.00] (n=2) | 0.78% [-9.15, 10.71] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| inverted-flow | -4.70 [-8.50, -0.90] (n=2) | 0.78% [-9.15, 10.71] (n=2) | 100.00% [100.00, 100.00] (n=2) | 0.78% [-9.15, 10.71] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| neutral-alternating | -0.20 [-2.78, 2.38] (n=2) | 47.46% [15.20, 79.72] (n=2) | 100.00% [100.00, 100.00] (n=2) | 47.46% [15.20, 79.72] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| neutral-hold | 0.00 [0.00, 0.00] (n=2) | 0.00% [0.00, 0.00] (n=2) | 100.00% [100.00, 100.00] (n=2) | 0.00% [0.00, 0.00] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| neutral-left | 0.17 [-1.61, 1.96] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| never-query | 1.77 [0.77, 2.76] (n=2) | 72.07% [59.66, 84.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 72.07% [59.66, 84.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |
| no-communication | 0.00 [0.00, 0.00] (n=2) | 0.00% [0.00, 0.00] (n=2) | 0.00% [0.00, 0.00] (n=2) | unavailable | 76.37% [-223.92, 376.65] (n=2) |
| shuffled-feedback | 0.17 [-1.61, 1.96] (n=2) | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.00% [0.00, 0.00] (n=2) |

### Prespecified paired primary contrasts

Positive differences favor the full controller; each contrast uses matching seed tapes.

| Full minus control | Return difference [95% CI] | Correct-rate difference | Query-rate difference |
|---|---:|---:|---:|
| A1P1Q1S1 versus A0P0Q0S0 | 3.00 [-0.99, 7.00] (n=2) | 47.07 pp [34.66, 59.48] (n=2) | 76.37 pp [-223.92, 376.65] (n=2) |
| A1P1Q1S1 versus neutral-hold | 3.17 [0.97, 5.38] (n=2) | 99.22 pp [89.29, 109.15] (n=2) | 76.37 pp [-223.92, 376.65] (n=2) |
| A1P1Q1S1 versus neutral-left | 3.00 [-0.99, 7.00] (n=2) | 47.07 pp [34.66, 59.48] (n=2) | 76.37 pp [-223.92, 376.65] (n=2) |
| A1P1Q1S1 versus neutral-alternating | 3.38 [-1.41, 8.17] (n=2) | 51.76 pp [29.42, 74.09] (n=2) | 76.37 pp [-223.92, 376.65] (n=2) |
| A1P1Q1S1 versus always-query | 0.17 [-2.03, 2.38] (n=2) | -0.78 pp [-10.71, 9.15] (n=2) | -23.63 pp [-323.92, 276.65] (n=2) |
| A1P1Q1S1 versus A1P0Q1S1 | 0.00 [0.00, 0.00] (n=2) | 0.00 pp [0.00, 0.00] (n=2) | 0.00 pp [0.00, 0.00] (n=2) |

### Factorial contrasts

k-factor alternating sum of 2^k marginal cell means; remaining factors equally averaged. Main effects=on minus off; AS=difference of differences; APQS=fourth-order cell contrast.

| Contrast | Return [95% CI] | Correct-rate contrast |
|---|---:|---:|
| A | 1.27 [1.27, 1.27] (n=2) | 15.82 pp [15.82, 15.82] (n=2) |
| P | -0.05 [-0.75, 0.64] (n=2) | -0.68 pp [-9.37, 8.00] (n=2) |
| Q | -0.15 [-3.65, 3.35] (n=2) | 7.71 pp [1.51, 13.92] (n=2) |
| S | 1.27 [1.27, 1.27] (n=2) | 15.82 pp [15.82, 15.82] (n=2) |
| AS | 2.53 [2.53, 2.53] (n=2) | 31.64 pp [31.64, 31.64] (n=2) |
| APQS | 0.44 [-5.12, 6.00] (n=2) | 5.47 pp [-64.02, 74.96] (n=2) |

### Isolated primitives

| Primitive / control | Correct | Coverage | Accuracy when covered | Return |
|---|---:|---:|---:|---:|
| distinctions / frozen-initial | 0.00% [0.00, 0.00] (n=2) | 0.00% [0.00, 0.00] (n=2) | unavailable | 0.00 [0.00, 0.00] (n=2) |
| distinctions / learned | 100.00% [100.00, 100.00] (n=2) | 100.00% [100.00, 100.00] (n=2) | 100.00% [100.00, 100.00] (n=2) | 0.00 [0.00, 0.00] (n=2) |
| distinctions / shuffled-feedback | 0.00% [0.00, 0.00] (n=2) | 0.00% [0.00, 0.00] (n=2) | unavailable | 0.00 [0.00, 0.00] (n=2) |
| prediction / frozen-initial | 0.00% [0.00, 0.00] (n=2) | 0.00% [0.00, 0.00] (n=2) | unavailable | 0.00 [0.00, 0.00] (n=2) |
| prediction / learned | 72.07% [59.66, 84.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 72.07% [59.66, 84.48] (n=2) | 0.00 [0.00, 0.00] (n=2) |
| prediction / shuffled-feedback | 37.30% [-114.08, 188.69] (n=2) | 100.00% [100.00, 100.00] (n=2) | 37.30% [-114.08, 188.69] (n=2) | 0.00 [0.00, 0.00] (n=2) |
| queries / frozen-initial | 71.09% [71.09, 71.09] (n=2) | 100.00% [100.00, 100.00] (n=2) | 71.09% [71.09, 71.09] (n=2) | 1.69 [1.69, 1.69] (n=2) |
| queries / learned | 99.22% [89.29, 109.15] (n=2) | 100.00% [100.00, 100.00] (n=2) | 99.22% [89.29, 109.15] (n=2) | 3.17 [0.97, 5.38] (n=2) |
| queries / shuffled-feedback | 71.88% [61.95, 81.80] (n=2) | 100.00% [100.00, 100.00] (n=2) | 71.88% [61.95, 81.80] (n=2) | 1.53 [-0.48, 3.54] (n=2) |
| programs / frozen-initial | 52.15% [29.81, 74.48] (n=2) | 100.00% [100.00, 100.00] (n=2) | 52.15% [29.81, 74.48] (n=2) | 0.17 [-1.61, 1.96] (n=2) |
| programs / learned | 100.00% [100.00, 100.00] (n=2) | 100.00% [100.00, 100.00] (n=2) | 100.00% [100.00, 100.00] (n=2) | 4.00 [4.00, 4.00] (n=2) |
| programs / shuffled-feedback | 50.00% [-585.31, 685.31] (n=2) | 100.00% [100.00, 100.00] (n=2) | 50.00% [-585.31, 685.31] (n=2) | 0.00 [-50.82, 50.82] (n=2) |

### Frozen test shifts

| Condition | Return [95% CI] | Change from baseline | Correct | Query rate |
|---|---:|---:|---:|---:|
| changed-fit | 2.38 [0.76, 3.99] (n=2) | -0.80 [-1.39, -0.20] (n=2) | 89.26% [71.89, 106.63] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| expensive-query | 1.65 [-6.57, 9.86] (n=2) | -1.53 [-7.53, 4.48] (n=2) | 99.22% [89.29, 109.15] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| less-persistent | 2.61 [-2.33, 7.55] (n=2) | -0.56 [-7.71, 6.58] (n=2) | 92.19% [-7.08, 191.45] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| missing-and-absent | 2.80 [1.88, 3.72] (n=2) | -0.37 [-1.66, 0.92] (n=2) | 90.04% [77.63, 102.45] (n=2) | 76.37% [-223.92, 376.65] (n=2) |
| novel-loads | 2.63 [1.01, 4.24] (n=2) | -0.55 [-1.14, 0.05] (n=2) | 92.38% [75.01, 109.75] (n=2) | 76.37% [-223.92, 376.65] (n=2) |

### Query placement, work and retained graph

Full query rate in the predictable cue0 context: 50.00% [-585.31, 685.31] (n=2); unreliable cue1 context: 100.00% [100.00, 100.00] (n=2).

Deployment comparisons/episode: 1.48 [1.28, 1.68] (n=2); fit probes/episode: 0.00 [0.00, 0.00] (n=2); shadow actions/episode: 0.00 [0.00, 0.00] (n=2).

Full graph: carriers 38072.00 [14209.75, 61934.25] (n=2); relations 369647.50 [175604.69, 563690.31] (n=2); placeholders 597.50 [362.44, 832.56] (n=2); coordinate occurrences 149751.00 [86855.29, 212646.71] (n=2). Active landmarks 2.00 [2.00, 2.00] (n=2); predictor rows 4.00 [4.00, 4.00] (n=2); accepted edits 5.00 [-7.71, 17.71] (n=2); selected complexity 8.00 [8.00, 8.00] (n=2).

Seed0 native archive sizes (transport bytes, not process memory): distinctions.gojo: 135,034; predictor.gojo: 55,698; program.gojo: 13,089,973; query.gojo: 1,635,390.

### Private encoding checks

- encoding-1: 2 paired seeds; all decisions/outcomes/work/footprint equal: **True**. Differences: `[]`.
- encoding-2: 2 paired seeds; all decisions/outcomes/work/footprint equal: **True**. Differences: `[]`.

### Ideal reference and scope

Balanced contexts; known fit distinction; fixed relation law; query gives exact current direction; no missing/absence. These are model expectations, not measurements.

Expected selective-query return 3.300; always-query 3.000; learned forecast without queries 1.800; fixed previous-direction without queries 1.800.

The complete seed curves and auxiliary paired contrasts are in analysis.json. Curves are training block outcomes, not frozen test performance. The saved trace is a small descriptive window, not another independent replicate.

## Limits shared by these runs

- All 16 program grammar candidates were constructed before the campaign. This evaluates bounded native selection and local composition, not unrestricted program discovery.
- Adaptive distinctions are piecewise-constant consequence hypotheses. Predictor count alternatives are empirical observations, not guaranteed sensor support.
- Query training uses a declared known-fitting shadow task. Shadow work is counted; comparison CPU work and training audits are not debited as the deployment resource fee.
- Frozen tests check fixed learned models. A changed law or price is not adapted during those tests, so degradation measures transfer of that fixed policy.
- Native graph sizes are retained reachable structures, including bounded receipts and evidence. They are neither process memory nor the number of active learned rules alone.
- Conditional accuracy is averaged over seeds with nonzero coverage and accompanies its own n. Zero-coverage seeds are excluded from that statistic rather than treated as0% accurate; paired accuracy contrasts require coverage on both sides.
- Student t intervals are not clipped to0–100%; small pilot samples can produce wide intervals beyond physical rate bounds.
- Neutral/ablated models may have limited information or initial grammar. Measured complementarity does not prove every remaining algorithm incapable.
