"""Focused acquisition/refinement and learning-boundary guarantees."""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import faithful_pipeline as pipeline


def stroke(points):
    return [[p[0] for p in points], [p[1] for p in points], list(range(len(points))) ]


class FaithfulPipelineTests(unittest.TestCase):
    def test_budget_refinement_retains_every_old_boundary(self):
        drawing = [stroke([(0, 0), (1, 2), (2, 1), (3, 4), (4, 0), (5, 3), (6, 0), (7, 2)])]
        data = pipeline.extract(drawing, (1, 2, 4, 7))
        previous = set()
        for snapshot in data["snapshots"]:
            boundaries = {n[k] for n in snapshot["nodes"] for k in ("sourceStart", "sourceEnd")}
            self.assertTrue(previous <= boundaries)
            self.assertEqual(len(snapshot["nodes"]), snapshot["requestedBudget"])
            previous = boundaries
        self.assertTrue(all(n["parent"] is None or n["parent"] < n["node"] for n in data["nodes"].values()))

    def test_pen_lift_floor_and_sample_capacity_are_explicit(self):
        drawing = [stroke([(0, i), (1, i)]) for i in range(5)]
        data = pipeline.extract(drawing, (3, 12))
        first, second = data["snapshots"]
        self.assertEqual((first["requestedBudget"], first["effectiveBudget"], first["strokeFloor"]), (3, 5, 5))
        self.assertEqual(second["effectiveBudget"], 5)
        self.assertTrue(second["sampleCapacityReached"])
        self.assertEqual(len(first["packet"]["strokes"]), 5)

    def test_empty_and_dot_strokes_do_not_acquire_connecting_ink(self):
        data = pipeline.extract([[[], [], []], stroke([(2, 3)]), stroke([(4, 5), (5, 5)])], (3,))
        packet = data["snapshots"][0]["packet"]
        self.assertEqual(packet["strokes"][0]["sections"], [])
        dot = packet["strokes"][1]["sections"][0]
        self.assertEqual(dot["start"], dot["end"])
        self.assertTrue(all(v == 0 for v in dot["features"][1:]))

    def test_clock_reversal_stays_in_private_correspondence(self):
        drawing = [[[0, 1, 2], [0, 1, 0], [20, 15, 5]]]
        data = pipeline.extract(drawing, (1,))
        self.assertEqual(data["snapshots"][0]["nodes"][0]["sourceTime"], [20, 5])
        self.assertNotIn("sourceTime", json.dumps(data["snapshots"][0]["packet"]))

    def test_profile_quantization_is_explicit_and_coarser_than_anchors(self):
        hint = pipeline.hint_for([(0.123, .126), (.219, .42), (.8, .71)])
        self.assertEqual(hint["start"], [round(.123*1024), round(.126*1024)])
        self.assertTrue(all(v % 16 == 0 for v in hint["features"][3:]))
        self.assertEqual(hint["features"][1:3], [b-a for a, b in zip(hint["start"], hint["end"])])

    def test_source_shape_is_not_reflected_or_stretched_by_normalization(self):
        data = pipeline.extract([stroke([(1, 0), (0, 1), (1, 2)])], (1,))
        self.assertEqual(data["strokes"][0], [(.5, 0), (0, .5), (.5, 1)])

    def test_geometry_group_retains_acquisition_direction(self):
        points = [(0, 0), (2, 1), (3, 0)]
        self.assertNotEqual(pipeline.geometry_signature([stroke(points)]),
                            pipeline.geometry_signature([stroke(points[::-1])]))
        self.assertEqual(pipeline.geometry_signature([stroke(points)]),
                         pipeline.geometry_signature([stroke([(x*5+7, y*5-4) for x, y in points])]))

    def test_sampler_blocks_reserved_ids_and_groups_without_using_recognized(self):
        reserved = {"key_id": "reserved", "drawing": [stroke([(0, 0), (1, 1)])]}
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source, output = root / "source", root / "output"
            source.mkdir()
            for i, category in enumerate(pipeline.CATEGORIES):
                rows = [reserved, {"key_id": f"near{i}", "drawing": [stroke([(4, 4), (6, 6)])]},
                        {"key_id": f"keep{i}", "recognized": False, "drawing": [stroke([(0, 0), (i+2, -1), (3, 1)])]}]
                (source / f"{category}.ndjson").write_text("\n".join(json.dumps(r) for r in rows), encoding="utf-8")
            records, manifest, report = pipeline.sample_corpus(source, output, 1, 0, [reserved])
            self.assertEqual(len(records), 5)
            self.assertTrue(all(r["recognized"] is False for r in records))
            self.assertEqual(report["rejected"]["reserved_or_duplicate_id"], 5)
            self.assertEqual(report["rejected"]["development_geometry_group"], 5)
            self.assertEqual(len({r["key_id"] for r in manifest}), 5)

    def test_test_targets_cannot_change_training_or_model_selection(self):
        examples = []
        for i, split in enumerate(("train", "train", "validation", "test")):
            drawing = [stroke([(j/12, ((j/12)*(1-j/12))*(i+1)) for j in range(13)])]
            examples.append((pipeline.extract(drawing, (1, 3)), {"split": split}))
        before, candidates_before = pipeline.train(examples)
        after_examples = copy.deepcopy(examples)
        for snapshot in after_examples[-1][0]["snapshots"]:
            for node in snapshot["nodes"]:
                node["fit"]["controls"] = [[1e9, -1e9]]*4
                node["points"] = [[-1e9, 1e9]]*len(node["points"])
        after, candidates_after = pipeline.train(after_examples)
        self.assertEqual(before, after)
        self.assertEqual(candidates_before, candidates_after)

    def test_public_export_separates_fitted_targets_and_source_samples(self):
        data = pipeline.extract([stroke([(0, 0), (1, 2), (2, 0), (3, 1)])], (1, 3))
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary)
            pipeline.emit_packets([(data, {"key_id": "source", "category": "hidden-category", "split": "development"})], output)
            public = (output / "packets.ndjson").read_text()
            private = (output / "private-audit.ndjson").read_text()
            for secret in ("hidden-category", "controls", "sourceStrokes", "sourceStart", "sourceTime"):
                self.assertNotIn(secret, public)
                self.assertIn(secret, private)

    def test_frozen_inference_never_trains_or_rewrites_model(self):
        model = {"coordinateQuantum": 1024, "coefficientQuantum": 1000000, "profileStep": 16,
                 "weights": pipeline.fixed_profile_weights(), "trainingCount": 0}
        record = {"key_id": "fresh", "word": "unseen", "drawing": [stroke([(0, 0), (1, 1), (2, 0)])]}
        with tempfile.TemporaryDirectory() as temporary:
            folder = Path(temporary)
            model_path, input_path = folder / "model.json", folder / "source.ndjson"
            model_path.write_text(json.dumps(model), encoding="utf-8")
            input_path.write_text(json.dumps(record), encoding="utf-8")
            before = model_path.read_bytes()
            with patch.object(pipeline, "train", side_effect=AssertionError("Inference must not train")):
                pipeline.encode_input(input_path, model_path, folder / "output")
            self.assertEqual(before, model_path.read_bytes())
            rows = (folder / "output" / "packets.ndjson").read_text().splitlines()
            self.assertEqual(len(rows), 4)
            self.assertNotIn("unseen", "\n".join(rows))


if __name__ == "__main__":
    unittest.main()
