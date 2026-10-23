"""Independent checks for geometry/traversal evaluation of restored curves."""
import json
from pathlib import Path
import tempfile
import unittest

from review_reconstruction import evaluate, measure, polylines


class NativeReviewMetricTests(unittest.TestCase):
    def assert_zero(self, result):
        for key, value in result.items():
            with self.subTest(metric=key):
                self.assertLess(abs(value), 1e-12)

    def test_same_line_with_different_cubic_speed_has_same_geometry_and_traversal(self):
        # These cubics travel along the exact same line, with different speed.
        uniform = polylines([[[[0, 0], [1/3, 0], [2/3, 0], [1, 0]]]])
        eased = polylines([[[[0, 0], [0, 0], [0, 0], [1, 0]]]])
        self.assert_zero(measure([[[0, 0], [1, 0]]], uniform))
        self.assert_zero(measure([[[0, 0], [1, 0]]], eased))
        self.assert_zero(measure(uniform, eased))

    def test_reverse_traversal_is_exposed_without_changing_geometric_match(self):
        source = [[[0, 0], [1, 0]]]
        result = measure(source, [[[1, 0], [0, 0]]])
        self.assertLess(result["symmetricSampleRms"], 1e-12)
        self.assertLess(result["maximumSampleDistance"], 1e-12)
        self.assertGreater(result["orderedArcRms"], .5)
        self.assertEqual(result["maximumEndpointError"], 1)

    def test_retracing_is_exposed_despite_zero_unordered_distance(self):
        result = measure([[[0, 0], [1, 0], [0, 0], [1, 0]]], [[[0, 0], [1, 0]]])
        self.assertLess(result["symmetricSampleRms"], 1e-12)
        self.assertGreater(result["orderedArcRms"], .3)
        self.assertEqual(result["maximumEndpointError"], 0)

    def test_pen_lift_and_gap_are_preserved_and_gap_filling_is_measured(self):
        source = [[[0, 0], [.4, 0]], [[.6, 0], [1, 0]]]
        curves = [[[[0, 0], [.1, 0], [.3, 0], [.4, 0]]],
                  [[[.6, 0], [.7, 0], [.9, 0], [1, 0]]]]
        predicted = polylines(curves)
        self.assertEqual(len(predicted), 2)
        self.assertEqual(predicted[0][-1], (.4, 0))
        self.assertEqual(predicted[1][0], (.6, 0))
        self.assert_zero(measure(source, predicted))
        filled = measure(source, [[[0, 0], [.5, 0]], [[.5, 0], [1, 0]]])
        self.assertGreater(filled["symmetricSampleRms"], .01)
        self.assertAlmostEqual(filled["maximumEndpointError"], .1)
        with self.assertRaisesRegex(ValueError, "Stroke count"):
            measure(source, [[[0, 0], [1, 0]]])

    def test_stroke_correspondence_is_not_reassigned_to_get_a_better_score(self):
        first, second = [[0, 0], [1, 0]], [[0, 1], [1, 1]]
        result = measure([first, second], [second, first])
        self.assertEqual(result["orderedArcRms"], 1)
        self.assertEqual(result["symmetricSampleRms"], 1)

    def test_empty_mismatch_is_rejected_and_singletons_remain_observable(self):
        self.assert_zero(measure([[], [[.2, .3]]], [[], [[.2, .3]]]))
        with self.assertRaisesRegex(ValueError, "Empty/nonempty"):
            measure([[], [[.2, .3]]], [[[.2, .3]], []])
        with self.assertRaisesRegex(ValueError, "Stroke count"):
            measure([[], []], [[]])
        changed = measure([[[.2, .3]]], [[[.2, .4]]])
        self.assertAlmostEqual(changed["symmetricSampleRms"], .1)
        self.assertAlmostEqual(changed["maximumEndpointError"], .1)

    def test_noncontiguous_sections_cannot_invent_a_connecting_path(self):
        sections = [[[[0, 0], [.1, 0], [.3, 0], [.4, 0]],
                     [[.6, 0], [.7, 0], [.9, 0], [1, 0]]]]
        with self.assertRaisesRegex(ValueError, "Noncontiguous"):
            polylines(sections)

    def test_duplicate_or_mismatched_audit_envelopes_are_rejected(self):
        with tempfile.TemporaryDirectory() as root:
            directory = Path(root)
            native = directory / "native"
            native.mkdir()
            def write_ids(path, ids):
                path.write_text("".join(json.dumps({"id": value}) + "\n" for value in ids), encoding="utf-8")
            for audits, packets, decoded in ((["a", "a"], ["a"], ["a"]),
                                             (["a"], ["a", "a"], ["a"]),
                                             (["a"], ["a"], ["a", "a"]),
                                             (["a"], ["b"], ["a"]),
                                             (["a"], ["a"], ["b"])):
                with self.subTest(audits=audits, packets=packets, decoded=decoded):
                    write_ids(directory / "private-audit.ndjson", audits)
                    write_ids(directory / "packets.ndjson", packets)
                    write_ids(native / "decoded.ndjson", decoded)
                    with self.assertRaisesRegex(ValueError, "ID"):
                        evaluate(directory, native)


if __name__ == "__main__":
    unittest.main()
