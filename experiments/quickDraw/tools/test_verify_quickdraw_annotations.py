"""False reads at the measured-annotation boundary, independent of discovery."""
import copy
import unittest

from verify_quickdraw_annotations import audit_annotations


def fixture():
    def entry(key, spans):
        return {"id": key, "spans": spans, "point": [10, 10], "qualification": "Finite source read"}
    observation = {"sourceId": "fixture", "spans": [
        {"id": "a", "controls": [[10, 0], [10, 10]], "strokeIndex": 0, "startIndex": 0, "endIndex": 1},
        {"id": "b", "controls": [[10, 10], [20, 10]], "strokeIndex": 0, "startIndex": 1, "endIndex": 2}]}
    joint = {**entry("joint", ["a", "b"]), "observedSharedSite": True,
             "participants": [{"spanId": "a", "t": 1, "sourceSite": {"strokeIndex": 0, "sampleIndex": 1}},
                              {"spanId": "b", "t": 0, "sourceSite": {"strokeIndex": 0, "sampleIndex": 1}}],
             "rays": [{"spanId": "a", "t": 1, "toward": "start", "vector": [0, -10]},
                      {"spanId": "b", "t": 0, "toward": "end", "vector": [10, 0]}],
             "rayPairs": [{"first": 0, "second": 1, "opposedResidualDegrees": 90,
                           "perpendicularResidualDegrees": 0, "sameDirectionResidualDegrees": 90}]}
    annotation = {"sourceId": "fixture", "lines": [entry("line-a", ["a"]), entry("line-b", ["b"])],
                  "joints": [joint], "symmetries": [], "relations": []}
    return annotation, observation


class AnnotationVerificationTests(unittest.TestCase):
    def test_actual_ordered_source_rays_and_acquired_shared_site_are_supported(self):
        annotation, observation = fixture()
        self.assertEqual(audit_annotations(annotation, observation)["rays"], 2)

    def test_equal_coordinate_pen_lifts_cannot_claim_shared_acquisition_identity(self):
        annotation, observation = fixture()
        observation["spans"][1].update(strokeIndex=1, startIndex=0, endIndex=1)
        annotation["joints"][0]["participants"][1]["sourceSite"] = {"strokeIndex": 1, "sampleIndex": 0}
        with self.assertRaisesRegex(AssertionError, "shared acquisition"):
            audit_annotations(annotation, observation)
        annotation["joints"][0]["observedSharedSite"] = False
        audit_annotations(annotation, observation)

    def test_reversed_ray_cannot_keep_its_original_direction_or_right_angle_claim(self):
        annotation, observation = fixture()
        annotation["joints"][0]["rays"][0]["vector"] = [0, 10]
        with self.assertRaisesRegex(AssertionError, "tangent"):
            audit_annotations(annotation, observation)

    def test_changed_angular_residual_cannot_hide_behind_a_joint_label(self):
        annotation, observation = fixture()
        annotation["joints"][0]["label"] = "T"
        annotation["joints"][0]["rayPairs"][0]["opposedResidualDegrees"] = 0
        with self.assertRaisesRegex(AssertionError, "angular residual"):
            audit_annotations(annotation, observation)

    def test_absent_or_duplicate_line_participation_is_rejected(self):
        annotation, observation = fixture()
        for replacement in ([], [copy.deepcopy(annotation["lines"][0])]*2):
            candidate = copy.deepcopy(annotation)
            candidate["lines"] = replacement
            with self.subTest(lines=len(replacement)), self.assertRaises(AssertionError):
                audit_annotations(candidate, observation)


if __name__ == "__main__":
    unittest.main()
