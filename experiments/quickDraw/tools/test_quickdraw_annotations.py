"""Annotation reads retain ordered support, uncertainty and false-read boundaries."""
import copy
import math
import unittest

from quickdraw_annotations import annotate, classify_rays, point, tangent, _line_read, POLICIES
from quickdraw_structure_observations import observe_record


def observe(*strokes):
    return observe_record({"key_id": "fixture", "drawing": [[[p[0] for p in stroke], [p[1] for p in stroke]] for stroke in strokes]})


def rays(degrees):
    return [{"spanId": f"s{i}", "t": 0, "toward": "end", "angleDegrees": d % 360,
             "vector": [math.cos(math.radians(d)), math.sin(math.radians(d))]} for i, d in enumerate(degrees)]


class QuickDrawAnnotationTests(unittest.TestCase):
    def test_every_interval_gets_a_read_without_mutating_acquisition(self):
        observed = observe([(0, 0), (100, 8), (100, 80)], [], [(20, 30)])
        before = copy.deepcopy(observed)
        annotations = annotate(observed, "gentle-priors")
        self.assertEqual(observed, before)
        self.assertEqual({r["spans"][0] for r in annotations["lines"]}, {s["id"] for s in observed["spans"]})
        for row, span in zip(annotations["lines"], observed["spans"]):
            self.assertEqual(row["sourceRange"], {k: span[k] for k in ("strokeIndex", "startIndex", "endIndex")})
            self.assertEqual(row["controls"], span["controls"])

    def test_joint_vocabulary_reads_ray_structure_and_rejects_duplicate_arms(self):
        examples = {"I": [0, 180], "L": [0, 90], "T": [0, 90, 180],
                    "Y": [30, 150, 270], "X": [0, 90, 180, 270], "K": [0, 45, 90, 270]}
        for label, directions in examples.items():
            with self.subTest(label=label):
                self.assertEqual(classify_rays(rays(directions), 10)[0], label)
        self.assertEqual(classify_rays(rays([0, 0, 180]), 32)[0], "coincident rays")
        self.assertEqual(classify_rays(rays([0, 0.5, 180, 180.5]), 32)[0], "coincident rays")
        self.assertEqual(classify_rays(rays([0, 5, 190]), 10)[0], "T")

    def test_angled_L_does_not_claim_a_failed_right_angle_fit(self):
        _, candidates, _ = classify_rays(rays([0, 35]), 20)
        self.assertEqual([c["label"] for c in candidates], ["L", "angled L"])
        self.assertIsNone(candidates[0]["thresholdDegrees"])
        self.assertIsNone(candidates[1]["thresholdDegrees"])
        self.assertEqual(candidates[1]["residualDegrees"], 55)

    def test_same_acquired_sample_and_equal_pen_lift_coordinates_stay_distinct(self):
        shared = annotate(observe([(0, 0), (80, 0), (80, 80)]), "observed-fit")
        self.assertTrue(any(j["label"] == "L" and j["observedSharedSite"] for j in shared["joints"]))
        separate = annotate(observe([(0, 0), (80, 0)], [(80, 0), (80, 80)]), "observed-fit")
        self.assertTrue(any(j["label"] == "L" and not j["observedSharedSite"] for j in separate["joints"]))
        self.assertEqual(len(separate["lines"]), 2)

    def test_retraced_acquisition_keeps_its_intervals_without_manufacturing_T(self):
        observed = observe([(0, 0), (100, 0), (0, 0), (100, 0)])
        annotations = annotate(observed, "stronger-priors")
        self.assertEqual(len(annotations["lines"]), 3)
        self.assertTrue(any(j["label"] == "coincident rays" for j in annotations["joints"]))
        self.assertFalse(any(j["label"] in ("T", "X") for j in annotations["joints"]))

    def test_proximity_tolerance_widens_without_closing_the_observed_gap(self):
        observed = observe([(-80, 0), (80, 0)], [(0, 8), (0, 80)], [(-224, -224)], [(224, 224)])
        strict = annotate(observed, "observed-fit")
        gentle = annotate(observed, "gentle-priors")
        self.assertFalse(any(j["label"] == "T" for j in strict["joints"]))
        ts = [j for j in gentle["joints"] if j["label"] == "T"]
        self.assertTrue(ts)
        self.assertTrue(any(abs(j["gap"]-8) < 1e-8 and not j["observedSharedSite"] for j in ts))
        self.assertEqual(observed["strokes"][1]["sourcePoints"][0], [0, 8])

    def test_complete_link_neighborhood_does_not_bridge_a_chain_of_small_gaps(self):
        observed = observe([(0, 0), (0, -80)], [(8, 0), (8, 80)], [(16, 0), (16, -80)],
                           [(-224, -224)], [(224, 224)])
        annotations = annotate(observed, "gentle-priors")
        endpoint_groups = [j for j in annotations["joints"] if j.get("supportKind") == "endpoint neighborhood"]
        self.assertTrue(all(j["gap"] <= 10+1e-9 for j in endpoint_groups))
        self.assertFalse(any(len(j["spans"]) == 3 for j in endpoint_groups))

    def test_local_curve_derivative_can_be_tangent_when_its_chord_is_not(self):
        controls = [[0, 0], [100, 0], [100, 100]]
        self.assertEqual(tangent(controls, 0), [200, 0])
        self.assertEqual(tangent(controls, 1), [0, 200])
        observed = observe([(-100, 0), (0, 0)], [(0, 0), (100, 100)])
        observed["spans"][1]["controls"] = controls
        # This fixture supplies an already-fitted finite profile in source units.
        observed["spans"][0]["controls"] = [[-100, 0], [0, 0]]
        observed["spans"][1]["kind"] = "quadratic"
        result = annotate(observed, "observed-fit")
        self.assertTrue(any(r["kind"] == "tangent" and r["measured"]["angleResidualDegrees"] < 1e-9 for r in result["relations"]))

    def test_cycle_O_is_not_a_two_ray_junction_or_an_inserted_contact(self):
        observed = observe([(0, 0), (100, 0), (100, 100), (0, 100), (0, 0)])
        rows = annotate(observed, "observed-fit")["joints"]
        cycles = [j for j in rows if j["label"] == "O"]
        self.assertEqual(len(cycles), 1)
        self.assertEqual(cycles[0]["kind"], "closed-cycle")
        self.assertEqual(cycles[0]["rays"], [])
        self.assertFalse(cycles[0]["observedSharedSite"])
        self.assertNotEqual(cycles[0]["participants"][0]["sourceSite"], cycles[0]["participants"][-1]["sourceSite"])

    def test_local_mirror_pair_keeps_axis_correspondence_and_whole_scope_remainder(self):
        observed = observe([(-100, -100), (-100, 100)], [(100, 100), (100, -100)], [(40, 80), (80, 90)])
        result = annotate(observed, "stronger-priors")
        local = [s for s in result["symmetries"] if s["measured"]["scope"] == "selected pair only"]
        self.assertTrue(local)
        exact = next(s for s in local if s["correspondences"][0]["error"] < 1e-8)
        self.assertTrue(exact["correspondences"][0]["reversed"])
        whole = [s for s in result["symmetries"] if s["measured"]["scope"] == "whole drawing"]
        self.assertTrue(any(s["unmatchedSpanIds"] and s["measured"]["coverage"] < 1 for s in whole))

    def test_flat_drawing_uses_nonzero_observer_axes_and_empty_read_invents_none(self):
        for observed in (observe([(0, 0), (10, 0)], [(20, 0), (30, 0)]),
                         observe([(0, 0), (0, 10)], [(0, 20), (0, 30)])):
            rows = annotate(observed, "observed-fit")["symmetries"]
            self.assertTrue(rows)
            self.assertTrue(all(math.dist(s["axis"]["from"], s["axis"]["to"]) > 0 for s in rows))
        self.assertFalse(annotate(observe([]), "observed-fit")["symmetries"])
        self.assertFalse(annotate(observe([(0, 0)]), "observed-fit")["symmetries"])

    def test_translation_repetition_retains_reversal_and_residual(self):
        observed = observe([(0, 0), (50, 0)], [(50, 20), (0, 20)])
        result = annotate(observed, "observed-fit")
        repeats = [r for r in result["relations"] if r["kind"] == "repeated"]
        self.assertEqual(len(repeats), 1)
        self.assertTrue(repeats[0]["correspondences"][0]["reversed"])
        self.assertAlmostEqual(repeats[0]["measured"]["max"], 0)

    def test_all_computed_mirror_and_pair_candidates_are_retained(self):
        observed = observe(*[[(x, 0), (x, 100)] for x in range(0, 100, 10)])
        result = annotate(observed, "stronger-priors")
        self.assertEqual(result["selection"]["mirrors"]["localCandidates"], result["selection"]["mirrors"]["localReported"])
        self.assertGreater(result["selection"]["mirrors"]["localReported"], 16)
        for count in result["selection"]["relations"].values():
            self.assertEqual(count["candidates"], count["reported"])

    def test_native_annotation_read_does_not_claim_a_broken_mapped_joint(self):
        observed = observe([(-50, 0), (0, 0)], [(0, 0), (0, 50)])
        native_edges = [{"id": "e0", "controls": [[0, 0], [50, 0]]},
                        {"id": "e1", "controls": [[300, 300], [300, 350]]}]
        mapping = {s["id"]: [f"e{i}"] for i, s in enumerate(observed["spans"])}
        native = {"analysis": {"edges": native_edges}, "observationMapping": {"spanEdges": mapping}}
        annotations = annotate(observed, "observed-fit", native)
        joint = next(j for j in annotations["joints"] if j["label"] == "L")
        self.assertEqual(joint["read"]["status"], "outside-tolerance")
        self.assertIsNone(joint["read"]["point"])
        self.assertIsNotNone(joint["read"]["supportCenter"])
        self.assertEqual(native["analysis"]["edges"], native_edges)

    def test_bend_side_changes_with_acquired_direction_without_claiming_outward(self):
        span = {"id": "curve", "strokeIndex": 0, "startIndex": 0, "endIndex": 2,
                "controls": [[0, 0], [50, 80], [100, 0]], "lineError": {"max": 40}, "fitError": {"rms": 0, "max": 0}}
        forward = _line_read(span, POLICIES["observed-fit"])
        backward = _line_read(span | {"controls": span["controls"][::-1]}, POLICIES["observed-fit"])
        self.assertEqual(forward["bend"]["sense"], "right")
        self.assertEqual(backward["bend"]["sense"], "left")
        self.assertEqual(forward["bend"]["signedSagitta"], -backward["bend"]["signedSagitta"])
        self.assertIn("without a region basis", forward["bend"]["qualification"])


if __name__ == "__main__":
    unittest.main()
