"""Adversarial identity and causal-affine contracts for conceptual synthesis."""
from copy import deepcopy
import unittest

from verify_quickdraw_synthesis import audit_endpoint_bindings, audit_native_symmetry, audit_objective, audit_view, measure, profile_delta
from verify_quickdraw_interpretation import evaluate_program


def fixture():
    def coord(first, last=None, numerator=0, denominator=1, origin=None):
        return {"origin": origin or first, "from": first, "to": last or first,
                "numerator": numerator, "denominator": denominator}
    program = {"parameters": [], "sites": [
        {"id": key, "at": 0, "x": x, "y": y}
        for key, x, y in (("a", 0, 0), ("b", 10, 0), ("c", 12, 0), ("d", 20, 0), ("c-equal", 12, 0))],
        "edges": [{"id": "g-left", "at": 0, "controls": ["a", "b"]},
                  {"id": "g-right", "at": 0, "controls": ["c", "d"]}]}
    program["sites"].append({"id": "joined", "at": 1, "x": coord("b", "c", 1, 2), "y": coord("b", "c", 1, 2)})
    program["edges"] += [{"id": "g-new-left", "at": 1, "controls": ["a", "joined"]},
                         {"id": "g-new-right", "at": 1, "controls": ["joined", "d"]}]
    before = [{"id": "left", "controls": [[0, 0], [10, 0]], "endpointKeys": ["old-a", "old-b"]},
              {"id": "right", "controls": [[12, 0], [20, 0]], "endpointKeys": ["old-c", "old-d"]}]
    after = [{"id": "new-left", "controls": [[0, 0], [11, 0]], "siteKeys": ["a", "joint"], "endpointKeys": ["a", "joint"]},
             {"id": "new-right", "controls": [[11, 0], [20, 0]], "siteKeys": ["joint", "d"], "endpointKeys": ["joint", "d"]}]
    operation = {"beforeEdges": before, "afterEdges": after, "construction": {"endpointBindings": [
        {"targetSiteKey": "joint", "mode": "weighted-merge", "inputs": [
            {"edgeId": "left", "controlIndex": 1, "weight": .5},
            {"edgeId": "right", "controlIndex": 0, "weight": .5}]}]}}
    return operation, program


class SynthesisContracts(unittest.TestCase):
    def test_merge_retains_distinct_sources_and_creates_one_target_identity(self):
        operation, program = fixture()
        self.assertEqual(1, audit_endpoint_bindings(operation, program)["boundTargetSites"])
        self.assertNotEqual(program["edges"][0]["controls"][-1], program["edges"][1]["controls"][0])
        self.assertEqual(program["edges"][2]["controls"][-1], program["edges"][3]["controls"][0])

    def test_equal_target_coordinates_do_not_establish_a_shared_joint(self):
        operation, program = fixture()
        duplicate = deepcopy(program["sites"][-1]); duplicate["id"] = "joined-copy"
        program["sites"].append(duplicate)
        program["edges"][-1]["controls"][0] = "joined-copy"
        with self.assertRaisesRegex(AssertionError, "distinct native identity"):
            audit_endpoint_bindings(operation, program)

    def test_equal_input_coordinate_cannot_substitute_actual_participant(self):
        operation, program = fixture()
        for axis in ("x", "y"):
            program["sites"][-1][axis]["to"] = "c-equal"
        with self.assertRaisesRegex(AssertionError, "actual prior control"):
            audit_endpoint_bindings(operation, program)

    def test_an_input_in_y_does_not_establish_its_x_influence(self):
        operation, program = fixture()
        program["sites"][-1]["x"]["to"] = "c-equal"
        with self.assertRaisesRegex(AssertionError, "nonzero affine input influences"):
            audit_endpoint_bindings(operation, program)

    def test_affine_weights_and_actual_input_effect_are_auditable(self):
        operation, program = fixture()
        operation["construction"]["endpointBindings"][0]["inputs"][1]["weight"] = .75
        with self.assertRaisesRegex(AssertionError, "affine combination"):
            audit_endpoint_bindings(operation, program)
        operation["construction"]["endpointBindings"][0]["inputs"][0]["weight"] = .25
        with self.assertRaisesRegex(AssertionError, "declared affine input"):
            audit_endpoint_bindings(operation, program)

    def test_perturbing_one_input_moves_the_actual_shared_target(self):
        operation, program = fixture()
        # The raw finite control and its actual native input are changed together.
        operation["beforeEdges"][1]["controls"][0][0] += 4
        program["sites"][2]["x"] += 4
        for edge in operation["afterEdges"]:
            edge["controls"][1 if edge["id"] == "new-left" else 0][0] += 2
        self.assertEqual(1, audit_endpoint_bindings(operation, program)["boundTargetSites"])
        operation["afterEdges"][0]["controls"][-1][0] -= 2
        with self.assertRaisesRegex(AssertionError, "competing proposal coordinates"):
            audit_endpoint_bindings(operation, program)

    def test_signed_mirror_uses_actual_facade_axis_not_page_symmetry(self):
        operation, program = fixture()
        operation["afterEdges"] = [{"id": "reflected", "controls": [[8, 0], [20, 0]],
            "endpointKeys": ["reflected", "d"], "siteKeys": ["reflected", "d"]}]
        operation["construction"]["endpointBindings"] = [{"targetSiteKey": "reflected", "mode": "mirror",
            "xInputs": [{"edgeId": "left", "controlIndex": 0, "weight": 1},
                        {"edgeId": "right", "controlIndex": 1, "weight": 1},
                        {"edgeId": "right", "controlIndex": 0, "weight": -1}],
            "yInputs": [{"edgeId": "right", "controlIndex": 0, "weight": 1}]}]
        program["sites"].append({"id": "reflected", "at": 1,
            "x": {"origin": "d", "from": "c", "to": "a", "numerator": 1, "denominator": 1},
            "y": {"origin": "c", "from": "c", "to": "c", "numerator": 0, "denominator": 1}})
        program["edges"].append({"id": "g-reflected", "at": 1, "controls": ["reflected", "d"]})
        self.assertEqual(1, audit_endpoint_bindings(operation, program)["boundTargetSites"])
        operation["construction"]["endpointBindings"][0]["xInputs"][0]["edgeId"] = "missing-page-axis"
        with self.assertRaisesRegex(AssertionError, "absent/future"):
            audit_endpoint_bindings(operation, program)


def seeded_author():
    from quickdraw_gestalt import Author
    author = Author({"sourceId": "fixture"}, {"steps": []})
    author.recipe.begin("Original", "Test input")
    for key, x, y in (("page-origin", 0, 0), ("page-extent", 512, 512),
                      ("a", 0, 0), ("b", 10, 0), ("c", 12, 0), ("d", 20, 0)):
        author.point(key, x, y)
    for key, controls in (("left", ["a", "b"]), ("right", ["c", "d"])):
        author.recipe.edge("g-"+key, controls)
        author.aliases[key] = "g-"+key
        author.edges["g-"+key] = author.recipe.data["edges"][-1]
    author.recipe.finish(["g-left", "g-right"], [])
    author.recipe.begin("Synthesis", "Actual prior-control bindings")
    return author


class NativeAuthorContracts(unittest.TestCase):
    def test_actual_author_shares_one_joint_and_responds_to_prior_input(self):
        author = seeded_author()
        operation, _ = fixture()
        operation.update({"id": "merge", "kind": "merge-close-joints", "sourceEdgeIds": ["left", "right"]})
        for edge in operation["afterEdges"]:
            edge["role"] = "boundary"
        author.new_batch(operation)
        left = author.edges["g-new-left"]["controls"][-1]
        right = author.edges["g-new-right"]["controls"][0]
        self.assertEqual(left, right)
        values, _ = evaluate_program(author.recipe.data)
        self.assertEqual([11, 0], values[left])
        perturbed = deepcopy(author.recipe.data)
        next(site for site in perturbed["sites"] if site["id"] == "c")["x"] += 4
        shifted, _ = evaluate_program(perturbed)
        self.assertEqual([13, 0], shifted[left])
        self.assertEqual(1, audit_endpoint_bindings(operation, author.recipe.data)["boundTargetSites"])

    def test_native_mirror_preserves_exact_symmetry_when_axis_moves(self):
        author = seeded_author()
        construction = {"endpointBindings": [
            {"targetSiteKey": "first", "mode": "reference", "inputs": [{"edgeId": "right", "controlIndex": 0, "weight": 1}]},
            {"targetSiteKey": "second", "mode": "mirror", "inputs": [{"edgeId": "right", "controlIndex": 0, "weight": 1}],
             "axisInputs": [{"edgeId": "left", "controlIndex": 0, "weight": .5}, {"edgeId": "right", "controlIndex": 1, "weight": .5}]}],
            "symmetryPairs": [{"firstSiteKey": "first", "secondSiteKey": "second",
                "axisInputs": [{"edgeId": "left", "controlIndex": 0, "weight": .5}, {"edgeId": "right", "controlIndex": 1, "weight": .5}]}]}
        # Two traversed profiles can visit the same shared corner pair. The
        # retained symmetry witness must select the actual final control sites.
        duplicate = deepcopy(construction["symmetryPairs"][0])
        duplicate["axisInputs"].reverse()
        construction["symmetryPairs"].append(duplicate)
        bound = author.bind_sites("mirror-fixture", construction)
        values, _ = evaluate_program(author.recipe.data)
        axis = author.synthesis_rows[-1]["symmetryPairs"][0]["axisSiteId"]
        self.assertEqual([12, 0], values[bound["first"]])
        self.assertEqual([8, 0], values[bound["second"]])
        self.assertEqual(2*values[axis][0], values[bound["first"]][0]+values[bound["second"]][0])
        perturbed = deepcopy(author.recipe.data)
        next(site for site in perturbed["sites"] if site["id"] == "d")["x"] += 6
        shifted, _ = evaluate_program(perturbed)
        self.assertEqual([14, 0], shifted[bound["second"]])
        self.assertEqual(2*shifted[axis][0], shifted[bound["first"]][0]+shifted[bound["second"]][0])
        author.recipe.edge("paired", [bound["first"], bound["second"]])
        author.recipe.data["synthesisMapping"] = author.synthesis_rows
        self.assertEqual(1, audit_native_symmetry(author.recipe.data)["nativeExactReflectionPairs"])

    def test_equal_mirror_coordinates_cannot_substitute_a_reflection_program(self):
        operation, program = fixture()
        # Both endpoints are numerically mirrored around x=10, but their
        # coordinate instructions do not consume that common axis.
        program["synthesisMapping"] = [{"operationId": "forged", "rung": 1, "bindings": [],
            "symmetryPairs": [{"firstSiteId": "a", "secondSiteId": "d", "axisSiteId": "b"}]}]
        with self.assertRaisesRegex(AssertionError, "actual reflection dependency"):
            audit_native_symmetry(program)

    def test_actual_side_corner_closes_and_follows_the_depth_vector(self):
        author = seeded_author()
        for key, y in (("b", 4), ("c", 20)):
            next(site for site in author.recipe.data["sites"] if site["id"] == key)["y"] = y
            author.positions[key] = (author.positions[key][0], y)
        inputs = {
            "a": [{"edgeId": "left", "controlIndex": 0, "weight": 1}],
            "b": [{"edgeId": "left", "controlIndex": 1, "weight": 1}],
            "c": [{"edgeId": "right", "controlIndex": 0, "weight": 1}],
            "d": [{"edgeId": "left", "controlIndex": 1, "weight": 1},
                  {"edgeId": "right", "controlIndex": 0, "weight": 1},
                  {"edgeId": "left", "controlIndex": 0, "weight": -1}]}
        construction = {"endpointBindings": [{"targetSiteKey": key, "inputs": rows} for key, rows in inputs.items()],
            "parallelograms": [{"aSiteKey": "a", "bSiteKey": "b", "cSiteKey": "c", "dSiteKey": "d"}]}
        bound = author.bind_sites("side", construction)
        for first, second in (("a", "b"), ("b", "d"), ("d", "c"), ("c", "a")):
            author.recipe.edge(first+second, [bound[first], bound[second]])
        author.recipe.data["synthesisMapping"] = author.synthesis_rows
        self.assertEqual(1, audit_native_symmetry(author.recipe.data)["nativeSideParallelograms"])
        values, _ = evaluate_program(author.recipe.data)
        self.assertEqual([22, 24], values[bound["d"]])
        perturbed = deepcopy(author.recipe.data)
        next(site for site in perturbed["sites"] if site["id"] == "b")["y"] += 6
        shifted, _ = evaluate_program(perturbed)
        self.assertEqual([22, 30], shifted[bound["d"]])
        forged = deepcopy(author.recipe.data)
        next(site for site in forged["sites"] if site["id"] == bound["d"])["x"] = 22
        with self.assertRaisesRegex(AssertionError, "actual corner dependencies"):
            audit_native_symmetry(forged)


def objective_fixture():
    from test_quickdraw_gestalt_contract import fixture as source_fixture
    observation, _, _ = source_fixture()
    operation, _ = fixture()
    operation.update({"id": "ordered-concept", "kind": "merge-nearby-proposal-joints", "accepted": True,
                      "selectionPolicy": "authored ordered synthesis; no optimization guarantee"})
    for edge, key, parent, span in zip(operation["beforeEdges"]+operation["afterEdges"],
            ("left", "right", "new-left", "new-right"), (None, None, "left", "right"), ("s0", "s1", "s0", "s1")):
        edge["id"] = key; edge["spans"] = [span]
        edge["parents"] = [parent] if parent else []
    before, after = measure(observation, ["s0", "s1"], operation["beforeEdges"]), measure(observation, ["s0", "s1"], operation["afterEdges"])
    delta = profile_delta(operation["beforeEdges"], operation["afterEdges"])
    operation["construction"]["jointClusters"] = [{"targetSiteKey": "joint", "point": [11, 0], "spreadBefore": 1, "spreadAfter": 0,
        "inputs": [{"edgeId": "left", "controlIndex": 1, "weight": .5}, {"edgeId": "right", "controlIndex": 0, "weight": .5}]}]
    weights = {"originalInk": .1, "previousInterpretation": 100, "geometricPrior": .01}
    operation["measurements"] = {"before": before, "after": after, "originalBefore": before, "originalAfter": after,
        "previousPass": delta, "constraintBefore": 1, "constraintAfter": 0}
    operation["thresholds"] = {"rawDistanceIsAcceptanceGate": False, "policyWeights": weights}
    operation["scoreBefore"] = .1*before["rms"]**2+.01
    operation["scoreAfter"] = .1*after["rms"]**2+100*delta["rms"]**2
    return operation, before, after


class OrderedObjectiveContracts(unittest.TestCase):
    def test_ordered_conceptual_pass_can_worsen_raw_self_and_weighted_scores(self):
        operation, before, after = objective_fixture()
        self.assertGreater(after["rms"], before["rms"])
        self.assertGreater(operation["measurements"]["previousPass"]["rms"], 0)
        self.assertGreater(operation["scoreAfter"], operation["scoreBefore"])
        audit_objective(operation, before, after, {})

    def test_declared_residual_and_weighted_score_are_independently_checked(self):
        for field in ("constraint", "score"):
            operation, before, after = objective_fixture()
            if field == "constraint":
                operation["measurements"]["constraintBefore"] += 1
            else:
                operation["scoreAfter"] += 1
            with self.subTest(field=field), self.assertRaises(AssertionError):
                audit_objective(operation, before, after, {})

    def test_self_displacement_must_consume_immediate_geometry(self):
        operation, before, after = objective_fixture()
        operation["afterEdges"][0]["parents"] = ["old-retired-profile"]
        with self.assertRaisesRegex(AssertionError, "immediate input"):
            audit_objective(operation, before, after, {})


class ViewContracts(unittest.TestCase):
    def test_view_alternatives_remain_supported_and_inspectable(self):
        operation, program = fixture()
        step = {"operations": [], "viewHypothesis": {"kind": "front", "reason": "Current facade read",
            "supportEdgeIds": ["left"], "frontEdgeIds": ["left"], "side": None, "candidates": [
                {"kind": "front", "support": 1, "reason": "Upright support", "supportEdgeIds": ["left"]},
                {"kind": "oblique", "support": 0, "reason": "No depth support", "supportEdgeIds": []}]}}
        known = {edge["id"]: edge for edge in operation["beforeEdges"]}
        self.assertEqual(2, audit_view(step, known, program)["retainedAlternatives"])
        step["viewHypothesis"]["candidates"].pop()
        with self.assertRaisesRegex(AssertionError, "alternate reading"):
            audit_view(step, known, program)

    def test_side_view_cannot_apply_global_front_symmetry(self):
        operation, program = fixture()
        step = {"operations": [{"kind": "synthesize-local-rectangular-facade", "construction": {"viewFrame": "front"}}],
            "viewHypothesis": {"kind": "oblique", "reason": "Retained depth marks", "supportEdgeIds": ["left", "right"],
                "frontEdgeIds": ["left"], "side": {"depthSign": 1, "depth": 30, "supportEdgeIds": ["right"]},
                "candidates": [{"kind": "front", "support": .4, "reason": "Facade", "supportEdgeIds": ["left"]},
                               {"kind": "oblique", "support": .6, "reason": "Depth", "supportEdgeIds": ["right"]}]}}
        known = {edge["id"]: edge for edge in operation["beforeEdges"]}
        with self.assertRaisesRegex(AssertionError, "global frontal"):
            audit_view(step, known, program)
        step["operations"][0]["construction"]["viewFrame"] = "localFront"
        self.assertEqual("oblique", audit_view(step, known, program)["kind"])


if __name__ == "__main__":
    unittest.main()
