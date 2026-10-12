# Primitive learning: saved-run analysis

Intervals are two-sided 95% Student t intervals over independent seed means or paired seed differences. Episodes are not treated as independent replicates. Intervals describe this seed population; they are not corrected for multiple exploratory comparisons or a guarantee outside the authored task.

## campaign-fresh

Source: `D:\source\repos\It3rate\Gojo-phototaxis\artifacts\primitive-learning\campaign-fresh`. 8 seeds starting at 100; 1536 training and 768 test episodes per run.

### Main test results

| Variant | Return [95% CI] | Correct | Coverage | Covered accuracy | Query rate |
|---|---:|---:|---:|---:|---:|
| A0P0Q0S0 | 0.01 [-0.11, 0.13] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| A0P0Q0S1 | 0.01 [-0.11, 0.13] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| A0P0Q1S0 | -0.99 [-1.11, -0.87] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) |
| A0P0Q1S1 | -0.99 [-1.11, -0.87] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) |
| A0P1Q0S0 | 0.01 [-0.11, 0.13] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| A0P1Q0S1 | 0.01 [-0.11, 0.13] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| A0P1Q1S0 | -0.55 [-0.82, -0.29] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| A0P1Q1S1 | -0.55 [-0.82, -0.29] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| A1P0Q0S0 | 0.01 [-0.11, 0.13] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| A1P0Q0S1 | 1.43 [1.10, 1.76] (n=8) | 67.90% [63.80, 72.01] (n=8) | 100.00% [100.00, 100.00] (n=8) | 67.90% [63.80, 72.01] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| A1P0Q1S0 | -0.99 [-1.11, -0.87] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) |
| A1P0Q1S1 | 3.00 [3.00, 3.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) |
| A1P1Q0S0 | 0.01 [-0.11, 0.13] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| A1P1Q0S1 | 1.42 [1.05, 1.79] (n=8) | 67.74% [63.13, 72.35] (n=8) | 100.00% [100.00, 100.00] (n=8) | 67.74% [63.13, 72.35] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| A1P1Q1S0 | -0.55 [-0.82, -0.29] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| A1P1Q1S1 | 3.26 [3.16, 3.36] (n=8) | 97.71% [96.78, 98.63] (n=8) | 100.00% [100.00, 100.00] (n=8) | 97.71% [96.78, 98.63] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| always-query | 3.00 [3.00, 3.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) |
| inverted-fit | -4.38 [-4.59, -4.16] (n=8) | 2.29% [1.37, 3.22] (n=8) | 100.00% [100.00, 100.00] (n=8) | 2.29% [1.37, 3.22] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| inverted-flow | -4.38 [-4.59, -4.16] (n=8) | 2.29% [1.37, 3.22] (n=8) | 100.00% [100.00, 100.00] (n=8) | 2.29% [1.37, 3.22] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| neutral-alternating | -0.05 [-0.10, -0.00] (n=8) | 49.37% [48.76, 49.97] (n=8) | 100.00% [100.00, 100.00] (n=8) | 49.37% [48.76, 49.97] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| neutral-hold | 0.00 [0.00, 0.00] (n=8) | 0.00% [0.00, 0.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 0.00% [0.00, 0.00] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| neutral-left | 0.01 [-0.11, 0.13] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| never-query | 1.77 [1.62, 1.93] (n=8) | 72.15% [70.22, 74.09] (n=8) | 100.00% [100.00, 100.00] (n=8) | 72.15% [70.22, 74.09] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| no-communication | -0.56 [-0.71, -0.41] (n=8) | 0.00% [0.00, 0.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 0.00% [0.00, 0.00] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| shuffled-feedback | 0.01 [-0.11, 0.13] (n=8) | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.00% [0.00, 0.00] (n=8) |
| simultaneous-query | 3.00 [3.00, 3.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) |
| whole-route-credit | 3.02 [2.43, 3.60] (n=8) | 94.76% [87.20, 102.32] (n=8) | 100.00% [100.00, 100.00] (n=8) | 94.76% [87.20, 102.32] (n=8) | 56.51% [41.76, 71.26] (n=8) |

### Prespecified paired primary contrasts

Positive differences favor the full controller; each contrast uses matching seed tapes.

| Full minus control | Return difference [95% CI] | Correct-rate difference | Query-rate difference |
|---|---:|---:|---:|
| A1P1Q1S1 versus A0P0Q0S0 | 3.25 [3.17, 3.33] (n=8) | 47.62 pp [45.27, 49.98] (n=8) | 55.86 pp [40.90, 70.82] (n=8) |
| A1P1Q1S1 versus neutral-hold | 3.26 [3.16, 3.36] (n=8) | 97.71 pp [96.78, 98.63] (n=8) | 55.86 pp [40.90, 70.82] (n=8) |
| A1P1Q1S1 versus neutral-left | 3.25 [3.17, 3.33] (n=8) | 47.62 pp [45.27, 49.98] (n=8) | 55.86 pp [40.90, 70.82] (n=8) |
| A1P1Q1S1 versus neutral-alternating | 3.31 [3.19, 3.43] (n=8) | 48.34 pp [47.22, 49.46] (n=8) | 55.86 pp [40.90, 70.82] (n=8) |
| A1P1Q1S1 versus always-query | 0.26 [0.16, 0.36] (n=8) | -2.29 pp [-3.22, -1.37] (n=8) | -44.14 pp [-59.10, -29.18] (n=8) |
| A1P1Q1S1 versus A1P0Q1S1 | 0.26 [0.16, 0.36] (n=8) | -2.29 pp [-3.22, -1.37] (n=8) | -44.14 pp [-59.10, -29.18] (n=8) |

### Auxiliary paired contrasts

These controls change the declared information, feedback or training schedule. Effects apply to the specified consumer and retained initial policy.

| Full minus control | Return difference [95% CI] | Correct-rate difference | Query-rate difference |
|---|---:|---:|---:|---:|
| A1P1Q1S1 versus A0P1Q1S1 | 3.81 [3.62, 4.00] (n=8) | 47.62 pp [45.27, 49.98] (n=8) | 0.00 pp [0.00, 0.00] (n=8) |
| A1P1Q1S1 versus A1P1Q0S1 | 1.84 [1.45, 2.22] (n=8) | 29.96 pp [25.52, 34.41] (n=8) | 55.86 pp [40.90, 70.82] (n=8) |
| A1P1Q1S1 versus A1P1Q1S0 | 3.81 [3.62, 4.00] (n=8) | 47.62 pp [45.27, 49.98] (n=8) | 0.00 pp [0.00, 0.00] (n=8) |
| A1P1Q1S1 versus shuffled-feedback | 3.25 [3.17, 3.33] (n=8) | 47.62 pp [45.27, 49.98] (n=8) | 55.86 pp [40.90, 70.82] (n=8) |
| A1P1Q1S1 versus no-communication | 3.82 [3.74, 3.89] (n=8) | 97.71 pp [96.78, 98.63] (n=8) | 0.00 pp [0.00, 0.00] (n=8) |
| A1P1Q1S1 versus inverted-fit | 7.63 [7.49, 7.78] (n=8) | 95.41 pp [93.57, 97.25] (n=8) | 0.00 pp [0.00, 0.00] (n=8) |
| A1P1Q1S1 versus inverted-flow | 7.63 [7.49, 7.78] (n=8) | 95.41 pp [93.57, 97.25] (n=8) | 0.00 pp [0.00, 0.00] (n=8) |
| A1P1Q1S1 versus never-query | 1.49 [1.35, 1.62] (n=8) | 25.55 pp [23.31, 27.80] (n=8) | 55.86 pp [40.90, 70.82] (n=8) |
| A1P1Q1S1 versus whole-route-credit | 0.24 [-0.37, 0.86] (n=8) | 2.95 pp [-4.68, 10.58] (n=8) | -0.65 pp [-22.90, 21.60] (n=8) |
| A1P1Q1S1 versus simultaneous-query | 0.26 [0.16, 0.36] (n=8) | -2.29 pp [-3.22, -1.37] (n=8) | -44.14 pp [-59.10, -29.18] (n=8) |

### Factorial contrasts

k-factor alternating sum of 2^k marginal cell means; remaining factors equally averaged. Main effects=on minus off; AS=difference of differences; APQS=fourth-order cell contrast.

| Contrast | Return [95% CI] | Correct-rate contrast |
|---|---:|---:|
| A | 1.33 [1.22, 1.44] (n=8) | 16.63 pp [15.20, 18.06] (n=8) |
| P | 0.20 [0.10, 0.29] (n=8) | -0.31 pp [-1.07, 0.46] (n=8) |
| Q | -0.16 [-0.27, -0.05] (n=8) | 7.76 pp [7.01, 8.51] (n=8) |
| S | 1.33 [1.22, 1.44] (n=8) | 16.63 pp [15.20, 18.06] (n=8) |
| AS | 2.66 [2.43, 2.89] (n=8) | 33.26 pp [30.40, 36.11] (n=8) |
| APQS | -0.17 [-0.66, 0.32] (n=8) | -2.13 pp [-8.22, 3.96] (n=8) |

### Isolated primitives

| Primitive / control | Correct | Coverage | Accuracy when covered | Return |
|---|---:|---:|---:|---:|
| distinctions / frozen-initial | 0.00% [0.00, 0.00] (n=8) | 0.00% [0.00, 0.00] (n=8) | unavailable | 0.00 [0.00, 0.00] (n=8) |
| distinctions / learned | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 0.00 [0.00, 0.00] (n=8) |
| distinctions / shuffled-feedback | 0.00% [0.00, 0.00] (n=8) | 0.00% [0.00, 0.00] (n=8) | unavailable | 0.00 [0.00, 0.00] (n=8) |
| prediction / frozen-initial | 0.00% [0.00, 0.00] (n=8) | 0.00% [0.00, 0.00] (n=8) | unavailable | 0.00 [0.00, 0.00] (n=8) |
| prediction / learned | 67.56% [62.05, 73.07] (n=8) | 90.19% [78.84, 101.53] (n=8) | 75.55% [71.09, 80.01] (n=8) | 0.00 [0.00, 0.00] (n=8) |
| prediction / shuffled-feedback | 41.16% [29.64, 52.68] (n=8) | 91.03% [76.14, 105.92] (n=8) | 47.85% [28.52, 67.19] (n=8) | 0.00 [0.00, 0.00] (n=8) |
| queries / frozen-initial | 27.44% [25.14, 29.74] (n=8) | 100.00% [100.00, 100.00] (n=8) | 27.44% [25.14, 29.74] (n=8) | -1.80 [-1.99, -1.62] (n=8) |
| queries / learned | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 3.00 [3.00, 3.00] (n=8) |
| queries / shuffled-feedback | 27.44% [25.14, 29.74] (n=8) | 100.00% [100.00, 100.00] (n=8) | 27.44% [25.14, 29.74] (n=8) | -1.80 [-1.99, -1.62] (n=8) |
| programs / frozen-initial | 50.08% [48.57, 51.60] (n=8) | 100.00% [100.00, 100.00] (n=8) | 50.08% [48.57, 51.60] (n=8) | 0.01 [-0.11, 0.13] (n=8) |
| programs / learned | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 100.00% [100.00, 100.00] (n=8) | 4.00 [4.00, 4.00] (n=8) |
| programs / shuffled-feedback | 59.46% [40.09, 78.82] (n=8) | 100.00% [100.00, 100.00] (n=8) | 59.46% [40.09, 78.82] (n=8) | 0.76 [-0.79, 2.31] (n=8) |

Isolated learned predictor: predictable cue0 coverage 100.00% [100.00, 100.00] (n=8), accuracy when covered 94.99% [93.87, 96.11] (n=8); unreliable cue1 coverage 80.01% [56.95, 103.08] (n=8), accuracy when covered 50.15% [45.70, 54.59] (n=8). Cue0 denotes a predictable relationship, which can be reversal or repetition; it does not imply physical persistence.

### Frozen test shifts

| Condition | Return [95% CI] | Change from baseline | Correct | Query rate |
|---|---:|---:|---:|---:|
| changed-fit | 2.52 [2.39, 2.65] (n=8) | -0.74 [-0.81, -0.67] (n=8) | 88.48% [87.61, 89.34] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| expensive-query | 1.02 [0.34, 1.71] (n=8) | -2.23 [-2.83, -1.64] (n=8) | 97.71% [96.78, 98.63] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| less-predictable | 2.36 [2.13, 2.59] (n=8) | -0.90 [-1.21, -0.58] (n=8) | 86.49% [81.84, 91.15] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| missing-and-absent | 2.73 [2.68, 2.78] (n=8) | -0.53 [-0.60, -0.46] (n=8) | 87.91% [86.71, 89.10] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| novel-loads | 2.80 [2.68, 2.92] (n=8) | -0.46 [-0.51, -0.41] (n=8) | 91.98% [91.18, 92.78] (n=8) | 55.86% [40.90, 70.82] (n=8) |
| reversed-flow-law | 0.08 [-0.91, 1.07] (n=8) | -3.18 [-4.26, -2.10] (n=8) | 57.99% [43.76, 72.22] (n=8) | 55.86% [40.90, 70.82] (n=8) |

### Query placement, work and retained graph

Full query rate in the predictable cue0 context: 12.50% [-17.06, 42.06] (n=8); unreliable cue1 context: 100.00% [100.00, 100.00] (n=8).

Deployment comparisons/episode: 1.51 [1.50, 1.52] (n=8); fit probes/episode: 0.00 [0.00, 0.00] (n=8); shadow actions/episode: 0.00 [0.00, 0.00] (n=8).

Full graph: carriers 40998.25 [39798.55, 42197.95] (n=8); relations 430774.75 [419939.30, 441610.20] (n=8); placeholders 614.50 [599.64, 629.36] (n=8); coordinate occurrences 196762.75 [192455.49, 201070.01] (n=8). Active landmarks 2.00 [2.00, 2.00] (n=8); predictor rows 4.00 [4.00, 4.00] (n=8); accepted edits 4.62 [2.68, 6.57] (n=8); selected complexity 8.00 [8.00, 8.00] (n=8).

Seed 100 native archive sizes (transport bytes, not process memory): composite.gojo: 16,513,657; distinctions.gojo: 125,758; predictor.gojo: 55,698; program.gojo: 14,967,449; query.gojo: 3,081,964.

### Private encoding checks

- encoding-1: 8 paired seeds; all decisions/outcomes/work/footprint equal: **True**. Differences: `[]`.
- encoding-2: 8 paired seeds; all decisions/outcomes/work/footprint equal: **True**. Differences: `[]`.

### Ideal reference and scope

Balanced contexts; known fit distinction; fixed relation law; query gives exact current direction; no missing/absence. These are model expectations, not measurements.

Expected selective-query return 3.300; always-query 3.000; learned forecast without queries 1.800; fixed previous-direction without queries -1.800.

The complete seed curves and auxiliary paired contrasts are in analysis.json. Curves are training block outcomes, not frozen test performance. The saved trace is a small descriptive window, not another independent replicate.

## persistent-fresh

Source: `D:\source\repos\It3rate\Gojo-phototaxis\artifacts\primitive-learning\persistent-fresh`. 4 seeds starting at 100; 1536 training and 768 test episodes per run.

### Main test results

| Variant | Return [95% CI] | Correct | Coverage | Covered accuracy | Query rate |
|---|---:|---:|---:|---:|---:|
| A0P0Q0S0 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A0P0Q0S1 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A0P0Q1S0 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A0P0Q1S1 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A0P1Q0S0 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A0P1Q0S1 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A0P1Q1S0 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A0P1Q1S1 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A1P0Q0S0 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A1P0Q0S1 | 1.35 [0.32, 2.39] (n=4) | 66.93% [53.95, 79.91] (n=4) | 100.00% [100.00, 100.00] (n=4) | 66.93% [53.95, 79.91] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A1P0Q1S0 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A1P0Q1S1 | 3.32 [3.29, 3.35] (n=4) | 97.75% [97.65, 97.86] (n=4) | 100.00% [100.00, 100.00] (n=4) | 97.75% [97.65, 97.86] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A1P1Q0S0 | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A1P1Q0S1 | 1.43 [0.67, 2.20] (n=4) | 67.94% [58.33, 77.54] (n=4) | 100.00% [100.00, 100.00] (n=4) | 67.94% [58.33, 77.54] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| A1P1Q1S0 | -0.49 [-0.69, -0.29] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| A1P1Q1S1 | 3.32 [3.29, 3.35] (n=4) | 97.75% [97.65, 97.86] (n=4) | 100.00% [100.00, 100.00] (n=4) | 97.75% [97.65, 97.86] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| always-query | 3.00 [3.00, 3.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) |
| inverted-fit | -4.32 [-4.34, -4.29] (n=4) | 2.25% [2.14, 2.35] (n=4) | 100.00% [100.00, 100.00] (n=4) | 2.25% [2.14, 2.35] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| inverted-flow | -4.32 [-4.34, -4.29] (n=4) | 2.25% [2.14, 2.35] (n=4) | 100.00% [100.00, 100.00] (n=4) | 2.25% [2.14, 2.35] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| neutral-alternating | 0.00 [-0.23, 0.23] (n=4) | 50.03% [47.14, 52.93] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.03% [47.14, 52.93] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| neutral-hold | 0.00 [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| neutral-left | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| never-query | 1.86 [1.54, 2.18] (n=4) | 73.24% [69.23, 77.25] (n=4) | 100.00% [100.00, 100.00] (n=4) | 73.24% [69.23, 77.25] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| no-communication | -0.50 [-0.53, -0.47] (n=4) | 0.00% [0.00, 0.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| shuffled-feedback | 0.01 [-0.18, 0.20] (n=4) | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.00% [0.00, 0.00] (n=4) |
| simultaneous-query | 3.32 [3.29, 3.35] (n=4) | 97.75% [97.65, 97.86] (n=4) | 100.00% [100.00, 100.00] (n=4) | 97.75% [97.65, 97.86] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| whole-route-credit | 2.63 [0.74, 4.51] (n=4) | 92.19% [72.09, 112.29] (n=4) | 100.00% [100.00, 100.00] (n=4) | 92.19% [72.09, 112.29] (n=4) | 74.97% [28.97, 120.97] (n=4) |

### Prespecified paired primary contrasts

Positive differences favor the full controller; each contrast uses matching seed tapes.

| Full minus control | Return difference [95% CI] | Correct-rate difference | Query-rate difference |
|---|---:|---:|---:|
| A1P1Q1S1 versus A0P0Q0S0 | 3.31 [3.14, 3.49] (n=4) | 47.66 pp [45.34, 49.97] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus neutral-hold | 3.32 [3.29, 3.35] (n=4) | 97.75 pp [97.65, 97.86] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus neutral-left | 3.31 [3.14, 3.49] (n=4) | 47.66 pp [45.34, 49.97] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus neutral-alternating | 3.32 [3.09, 3.55] (n=4) | 47.72 pp [44.89, 50.55] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus always-query | 0.32 [0.29, 0.35] (n=4) | -2.25 pp [-2.35, -2.14] (n=4) | -50.20 pp [-52.94, -47.45] (n=4) |
| A1P1Q1S1 versus A1P0Q1S1 | 0.00 [0.00, 0.00] (n=4) | 0.00 pp [0.00, 0.00] (n=4) | 0.00 pp [0.00, 0.00] (n=4) |

### Auxiliary paired contrasts

These controls change the declared information, feedback or training schedule. Effects apply to the specified consumer and retained initial policy.

| Full minus control | Return difference [95% CI] | Correct-rate difference | Query-rate difference |
|---|---:|---:|---:|---:|
| A1P1Q1S1 versus A0P1Q1S1 | 3.81 [3.63, 4.00] (n=4) | 47.66 pp [45.34, 49.97] (n=4) | 0.00 pp [0.00, 0.00] (n=4) |
| A1P1Q1S1 versus A1P1Q0S1 | 1.89 [1.14, 2.64] (n=4) | 29.82 pp [20.19, 39.44] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus A1P1Q1S0 | 3.81 [3.63, 4.00] (n=4) | 47.66 pp [45.34, 49.97] (n=4) | 0.00 pp [0.00, 0.00] (n=4) |
| A1P1Q1S1 versus shuffled-feedback | 3.31 [3.14, 3.49] (n=4) | 47.66 pp [45.34, 49.97] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus no-communication | 3.82 [3.81, 3.83] (n=4) | 97.75 pp [97.65, 97.86] (n=4) | 0.00 pp [0.00, 0.00] (n=4) |
| A1P1Q1S1 versus inverted-fit | 7.64 [7.62, 7.66] (n=4) | 95.51 pp [95.30, 95.72] (n=4) | 0.00 pp [0.00, 0.00] (n=4) |
| A1P1Q1S1 versus inverted-flow | 7.64 [7.62, 7.66] (n=4) | 95.51 pp [95.30, 95.72] (n=4) | 0.00 pp [0.00, 0.00] (n=4) |
| A1P1Q1S1 versus never-query | 1.46 [1.17, 1.76] (n=4) | 24.51 pp [20.59, 28.44] (n=4) | 49.80 pp [47.06, 52.55] (n=4) |
| A1P1Q1S1 versus whole-route-credit | 0.70 [-1.17, 2.57] (n=4) | 5.57 pp [-14.57, 25.70] (n=4) | -25.16 pp [-71.47, 21.15] (n=4) |
| A1P1Q1S1 versus simultaneous-query | 0.00 [0.00, 0.00] (n=4) | 0.00 pp [0.00, 0.00] (n=4) | 0.00 pp [0.00, 0.00] (n=4) |

### Factorial contrasts

k-factor alternating sum of 2^k marginal cell means; remaining factors equally averaged. Main effects=on minus off; AS=difference of differences; APQS=fourth-order cell contrast.

| Contrast | Return [95% CI] | Correct-rate contrast |
|---|---:|---:|
| A | 1.30 [1.12, 1.47] (n=4) | 16.25 pp [14.06, 18.43] (n=4) |
| P | 0.01 [-0.14, 0.16] (n=4) | 0.13 pp [-1.71, 1.96] (n=4) |
| Q | 0.11 [-0.04, 0.26] (n=4) | 7.58 pp [5.41, 9.76] (n=4) |
| S | 1.30 [1.12, 1.47] (n=4) | 16.25 pp [14.06, 18.43] (n=4) |
| AS | 2.60 [2.25, 2.95] (n=4) | 32.50 pp [28.12, 36.87] (n=4) |
| APQS | -0.08 [-1.26, 1.09] (n=4) | -1.01 pp [-15.69, 13.68] (n=4) |

### Isolated primitives

| Primitive / control | Correct | Coverage | Accuracy when covered | Return |
|---|---:|---:|---:|---:|
| distinctions / frozen-initial | 0.00% [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | unavailable | 0.00 [0.00, 0.00] (n=4) |
| distinctions / learned | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 0.00 [0.00, 0.00] (n=4) |
| distinctions / shuffled-feedback | 0.00% [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | unavailable | 0.00 [0.00, 0.00] (n=4) |
| prediction / frozen-initial | 0.00% [0.00, 0.00] (n=4) | 0.00% [0.00, 0.00] (n=4) | unavailable | 0.00 [0.00, 0.00] (n=4) |
| prediction / learned | 73.24% [69.23, 77.25] (n=4) | 100.00% [100.00, 100.00] (n=4) | 73.24% [69.23, 77.25] (n=4) | 0.00 [0.00, 0.00] (n=4) |
| prediction / shuffled-feedback | 50.46% [21.47, 79.44] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.46% [21.47, 79.44] (n=4) | 0.00 [0.00, 0.00] (n=4) |
| queries / frozen-initial | 73.08% [70.86, 75.30] (n=4) | 100.00% [100.00, 100.00] (n=4) | 73.08% [70.86, 75.30] (n=4) | 1.85 [1.67, 2.02] (n=4) |
| queries / learned | 97.75% [97.65, 97.86] (n=4) | 100.00% [100.00, 100.00] (n=4) | 97.75% [97.65, 97.86] (n=4) | 3.32 [3.29, 3.35] (n=4) |
| queries / shuffled-feedback | 73.08% [70.86, 75.30] (n=4) | 100.00% [100.00, 100.00] (n=4) | 73.08% [70.86, 75.30] (n=4) | 1.85 [1.67, 2.02] (n=4) |
| programs / frozen-initial | 50.10% [47.75, 52.45] (n=4) | 100.00% [100.00, 100.00] (n=4) | 50.10% [47.75, 52.45] (n=4) | 0.01 [-0.18, 0.20] (n=4) |
| programs / learned | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 100.00% [100.00, 100.00] (n=4) | 4.00 [4.00, 4.00] (n=4) |
| programs / shuffled-feedback | 43.72% [-7.62, 95.06] (n=4) | 100.00% [100.00, 100.00] (n=4) | 43.72% [-7.62, 95.06] (n=4) | -0.50 [-4.61, 3.60] (n=4) |

Isolated learned predictor: predictable cue0 coverage 100.00% [100.00, 100.00] (n=4), accuracy when covered 95.52% [95.16, 95.88] (n=4); unreliable cue1 coverage 100.00% [100.00, 100.00] (n=4), accuracy when covered 50.83% [44.65, 57.02] (n=4). Cue0 denotes a predictable relationship, which can be reversal or repetition; it does not imply physical persistence.

### Frozen test shifts

| Condition | Return [95% CI] | Change from baseline | Correct | Query rate |
|---|---:|---:|---:|---:|
| changed-fit | 2.58 [2.51, 2.65] (n=4) | -0.74 [-0.84, -0.64] (n=4) | 88.48% [87.30, 89.65] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| expensive-query | 1.33 [1.19, 1.47] (n=4) | -1.99 [-2.10, -1.88] (n=4) | 97.75% [97.65, 97.86] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| less-predictable | 2.36 [2.16, 2.56] (n=4) | -0.96 [-1.18, -0.75] (n=4) | 85.71% [83.03, 88.39] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| missing-and-absent | 2.90 [2.85, 2.96] (n=4) | -0.42 [-0.45, -0.39] (n=4) | 89.49% [88.80, 90.17] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| novel-loads | 2.86 [2.81, 2.90] (n=4) | -0.47 [-0.54, -0.39] (n=4) | 91.93% [91.08, 92.77] (n=4) | 49.80% [47.06, 52.55] (n=4) |
| reversed-flow-law | -0.29 [-0.46, -0.11] (n=4) | -3.61 [-3.81, -3.40] (n=4) | 52.64% [50.13, 55.15] (n=4) | 49.80% [47.06, 52.55] (n=4) |

### Query placement, work and retained graph

Full query rate in the predictable cue0 context: 0.00% [0.00, 0.00] (n=4); unreliable cue1 context: 100.00% [100.00, 100.00] (n=4).

Deployment comparisons/episode: 1.51 [1.48, 1.54] (n=4); fit probes/episode: 0.00 [0.00, 0.00] (n=4); shadow actions/episode: 0.00 [0.00, 0.00] (n=4).

Full graph: carriers 41486.75 [40883.69, 42089.81] (n=4); relations 435046.00 [429622.56, 440469.44] (n=4); placeholders 624.50 [598.69, 650.31] (n=4); coordinate occurrences 198427.75 [196267.94, 200587.56] (n=4). Active landmarks 2.00 [2.00, 2.00] (n=4); predictor rows 4.00 [4.00, 4.00] (n=4); accepted edits 3.25 [0.24, 6.26] (n=4); selected complexity 8.00 [8.00, 8.00] (n=4).

Seed 100 native archive sizes (transport bytes, not process memory): composite.gojo: 16,446,107; distinctions.gojo: 125,758; predictor.gojo: 55,698; program.gojo: 14,898,863; query.gojo: 3,025,766.

### Private encoding checks

- encoding-1: 4 paired seeds; all decisions/outcomes/work/footprint equal: **True**. Differences: `[]`.
- encoding-2: 4 paired seeds; all decisions/outcomes/work/footprint equal: **True**. Differences: `[]`.

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
- Conditional accuracy is averaged over seeds with nonzero coverage and accompanies its own n. Zero-coverage seeds are excluded from that statistic rather than treated as 0% accurate; paired accuracy contrasts require coverage on both sides.
- Student t intervals are not clipped to 0–100%; small seed samples can produce wide intervals beyond physical rate bounds.
- Neutral/ablated models may have limited information or initial grammar. Measured complementarity does not prove every remaining algorithm incapable.
