"""Independent source-fidelity and public-message checks for reconstruction.

These do not certify intent recovery, topology inference or statistical
generalization. They check preservation and false reads at the adapter boundary.
"""
import copy
import json
import unittest

import faithful_pipeline as pipeline


class ReconstructionContractTests(unittest.TestCase):
    def setUp(self):
        # Shared scalar coefficients apply identically on x and y. Nonzero
        # profile terms make this exercise the public hints rather than lines.
        self.weights = pipeline.expand_scalar_weights(
            [[-.2, .8, .4, -.1], [.7, -.1, .2, .5]])

    def test_detached_packet_contains_only_quantized_observations(self):
        data = pipeline.extract([[[0, 8, 2, 9], [0, 1, 7, 10], [40, 30, 60, 70]]], (1,))
        snapshot = data["snapshots"][0]
        packet = json.loads(json.dumps(snapshot["packet"]))
        self.assertEqual(set(packet), {"coordinateQuantum", "strokes"})
        self.assertEqual(set(packet["strokes"][0]), {"sections"})
        hint = packet["strokes"][0]["sections"][0]
        self.assertEqual(set(hint), {"start", "end", "features"})
        self.assertEqual([len(hint[k]) for k in ("start", "end", "features")], [2, 2, 9])
        self.assertTrue(all(isinstance(v, int) for k in hint for v in hint[k]))
        self.assertTrue(all(v % pipeline.PROFILE_STEP == 0 for v in hint["features"][3:]))
        before = pipeline.decode(hint, self.weights)
        # The receiving decode has no access to changed private target/source.
        snapshot["nodes"][0]["fit"]["controls"] = [[900, -900]] * 4
        snapshot["nodes"][0]["points"] = [(900, -900)] * 4
        self.assertEqual(pipeline.decode(hint, self.weights), before)

    def test_original_frame_mirror_changes_generated_orientation(self):
        # An asymmetric acquired route; no ideal category or canonical frame.
        drawing = [[[0, 0, 3, 8, 10, 7, 1], [0, 3, 5, 7, 10, 9, 6]]]
        mirrored = [[[-x for x in drawing[0][0]], drawing[0][1][:]]]
        a = pipeline.extract(drawing, (1,))["snapshots"][0]["packet"]["strokes"][0]["sections"][0]
        b = pipeline.extract(mirrored, (1,))["snapshots"][0]["packet"]["strokes"][0]["sections"][0]
        ca, cb = pipeline.decode(a, self.weights), pipeline.decode(b, self.weights)
        self.assertNotEqual(ca, cb)
        self.assertEqual(cb, [[pipeline.COORDINATE_QUANTUM-x, y] for x, y in ca])

    def test_receiver_reflection_and_axis_exchange_are_exact(self):
        hint = pipeline.hint_for([(0.13, 0.2), (.8, .36), (.37, .92), (.7, .65)])
        original = pipeline.decode(hint, self.weights)
        reflected = copy.deepcopy(hint)
        for key in ("start", "end"):
            reflected[key][0] *= -1
        for i in (1, 3, 5, 7):
            reflected["features"][i] *= -1
        self.assertEqual(pipeline.decode(reflected, self.weights), [[-x, y] for x, y in original])
        exchanged = copy.deepcopy(hint)
        for key in ("start", "end"):
            exchanged[key].reverse()
        for i in (1, 3, 5, 7):
            exchanged["features"][i:i+2] = exchanged["features"][i:i+2][::-1]
        self.assertEqual(pipeline.decode(exchanged, self.weights), [[y, x] for x, y in original])

    def test_pen_lifts_gaps_and_recorded_order_survive_small_budget(self):
        # Time is intentionally backward both inside and across strokes.
        drawing = [[[10, 7, 4], [0, 2, 0], [70, 60, 50]],
                   [[1, 2], [8, 9], [20, 10]]]
        data = pipeline.extract(drawing, (1,))
        snap = data["snapshots"][0]
        self.assertEqual((snap["requestedBudget"], snap["effectiveBudget"], snap["strokeFloor"]), (1, 2, 2))
        self.assertEqual([n["stroke"] for n in snap["nodes"]], [0, 1])
        self.assertEqual([n["sourceTime"] for n in snap["nodes"]], [[70, 50], [20, 10]])
        sections = [s["sections"] for s in snap["packet"]["strokes"]]
        self.assertEqual([len(s) for s in sections], [1, 1])
        self.assertNotEqual(sections[0][0]["end"], sections[1][0]["start"])
        expected_end = [round(v*pipeline.COORDINATE_QUANTUM) for v in data["strokes"][0][-1]]
        self.assertEqual(pipeline.decode(sections[0][0], self.weights)[-1], expected_end)

    def test_open_route_is_not_completed_even_with_near_return(self):
        drawing = [[[0, 10, 10, 0, 0], [0, 0, 10, 10, .2]]]
        snap = pipeline.extract(drawing, (3,))["snapshots"][0]
        hints = snap["packet"]["strokes"][0]["sections"]
        self.assertNotEqual(hints[0]["start"], hints[-1]["end"])
        self.assertNotEqual(pipeline.decode(hints[0], self.weights)[0],
                            pipeline.decode(hints[-1], self.weights)[-1])

    def test_budget_metrics_count_original_sample_events_once(self):
        # Two repeated coordinates are still distinct acquisition events. Only
        # overlap introduced by partitioning is removed from the denominator.
        drawing = [[[0, 1, 1, 3, 4, 5, 6, 7, 8], [0, 2, 2, 6, 1, 0, 4, 6, 8]],
                   [[8, 9, 10], [2, 1, 0]]]
        expected = sum(len(stroke[0]) for stroke in drawing)
        for snap in pipeline.extract(drawing, (2, 3, 6, 20))["snapshots"]:
            for mode in ("fit", "line", "learned"):
                with self.subTest(budget=snap["requestedBudget"], mode=mode):
                    self.assertEqual(pipeline.measure_snapshot(snap, self.weights, mode)["samples"], expected)

    def test_ordered_metric_exposes_collinear_retracing(self):
        snap = pipeline.extract([[[0, 10, 0, 10], [0, 0, 0, 0]]], (1,))["snapshots"][0]
        # Unordered geometric distance to the segment is zero, but flattening
        # the source retrace into one directed segment loses recorded progress.
        result = pipeline.measure_snapshot(snap, mode="line")
        self.assertGreater(result["rms"], .4)
        self.assertEqual(result["samples"], 4)

    def test_empty_and_singleton_strokes_remain_separate(self):
        snap = pipeline.extract([[[], [], []], [[7], [9], [18]], [[7], [9], [3]]], (3,))["snapshots"][0]
        self.assertEqual([len(s["sections"]) for s in snap["packet"]["strokes"]], [0, 1, 1])
        self.assertEqual(snap["effectiveBudget"], 2)
        self.assertTrue(snap["sampleCapacityReached"])
        self.assertEqual(pipeline.measure_snapshot(snap, self.weights)["samples"], 2)


if __name__ == "__main__":
    unittest.main()
