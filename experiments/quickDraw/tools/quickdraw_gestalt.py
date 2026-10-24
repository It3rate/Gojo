"""Import measured Gestalt proposals as connected, replayable native recipes.

The geometry adapter makes revisable local decisions. This author retains actual
input controls, primitive engines, intermediate replacements and acquired ink;
the saved native program alone executes every construction stage.
"""
from __future__ import annotations

import argparse
from fractions import Fraction
import json
from pathlib import Path

from construction_recipes import Recipe, loc
from quickdraw_gestalt_geometry import build as propose


def rounded(point):
    return tuple(round(value) for value in point)


class Author:
    def __init__(self, observation, proposal):
        self.observation, self.proposal = observation, proposal
        self.recipe = Recipe("quickdraw-gestalt-"+observation["sourceId"], "Church "+observation["sourceId"]+" · simplification")
        if len(proposal["steps"]) > 4:
            self.recipe.data["id"] = "quickdraw-synthesis-"+observation["sourceId"]
            self.recipe.data["family"] = self.recipe.data["id"]
        self.positions, self.edges, self.aliases, self.endpoints = {}, {}, {}, {}
        self.synthesis_rows, self.binding_axes = [], {}
        self.serial = 0

    def point(self, key, x, y):
        self.recipe.point(key, x, y)
        def evaluate(expr, axis):
            if isinstance(expr, int):
                return expr
            a, b = self.positions[expr["from"]][axis], self.positions[expr["to"]][axis]
            origin = self.positions[expr.get("origin", expr["from"])][axis]
            n = (b-a)*expr["numerator"]
            displacement = abs(n)//expr["denominator"] * (-1 if n < 0 else 1)
            return origin+displacement
        self.positions[key] = (evaluate(x, 0), evaluate(y, 1))
        return key

    def sync(self):
        # Primitive helpers append exact affine sites through Recipe directly.
        for site in self.recipe.data["sites"]:
            if site["id"] in self.positions:
                continue
            def evaluate(expr, axis):
                if isinstance(expr, int):
                    return expr
                n = (self.positions[expr["to"]][axis]-self.positions[expr["from"]][axis])*expr["numerator"]
                return self.positions[expr.get("origin", expr["from"])][axis] + abs(n)//expr["denominator"]*(-1 if n < 0 else 1)
            self.positions[site["id"]] = (evaluate(site["x"], 0), evaluate(site["y"], 1))

    def move(self, key, target, refs):
        """Measured displacement from a consumed native barycentric input frame."""
        refs = list(dict.fromkeys(refs))
        if not refs:
            raise ValueError("A replacement must consume actual earlier native controls")
        first = prior = refs[0]
        for i, ref in enumerate(refs[1:]):
            prior = self.point(f"{key}-input-{i}", loc(first, ref, 1, len(refs), prior), loc(first, ref, 1, len(refs), prior))
        target = rounded(target)
        delta = [target[j]-self.positions[prior][j] for j in range(2)]
        return self.point(key, loc("page-origin", "page-extent", delta[0], 512, prior),
                          loc("page-origin", "page-extent", delta[1], 512, prior))

    def controls(self, engine_id):
        return self.edges[self.aliases[engine_id]]["controls"]

    def weighted_site(self, key, inputs):
        """An affine combination of actual consumed controls, with native residue."""
        selected = [(self.controls(item["edgeId"])[item["controlIndex"]], Fraction(str(item.get("weight", 1))))
                    for item in inputs if item.get("weight", 1) != 0]
        if not selected:
            raise ValueError("A synthesis binding needs actual participating controls")
        total = sum(weight for _, weight in selected)
        if total == 0:
            raise ValueError("An affine coordinate cannot use a zero normalization")
        first = prior = selected[0][0]
        for i, (site, weight) in enumerate(selected[1:]):
            fraction = weight/total
            prior = self.point(f"{key}-term-{i}",
                loc(first, site, fraction.numerator, fraction.denominator, prior),
                loc(first, site, fraction.numerator, fraction.denominator, prior))
        return prior

    def axis_site(self, key, inputs):
        coefficients = {}
        for item in inputs:
            site = self.controls(item["edgeId"])[item["controlIndex"]]
            coefficients[site] = coefficients.get(site, Fraction(0))+Fraction(str(item.get("weight", 1)))
        total = sum(coefficients.values())
        signature = tuple(sorted((site, str(weight/total)) for site, weight in coefficients.items() if weight))
        token = (self.recipe.at, signature)
        if token not in self.binding_axes:
            self.binding_axes[token] = self.weighted_site(key, inputs)
        return self.binding_axes[token]

    def bind_sites(self, key, construction):
        bound, descriptions = {}, []
        for i, binding in enumerate(construction.get("endpointBindings", [])):
            name = key+f"-binding-{i}"
            inputs = binding.get("inputs", [])
            if binding.get("mode") == "mirror" and "axisInputs" in binding and "xInputs" not in binding and "yInputs" not in binding:
                source = self.weighted_site(name+"-source", inputs)
                axis = self.axis_site(name+"-axis", binding["axisInputs"])
                site = self.point(name, loc(source, axis, 1, 1, axis), loc(source))
            elif "xInputs" in binding or "yInputs" in binding:
                x = self.weighted_site(name+"-x", binding.get("xInputs", inputs))
                y = self.weighted_site(name+"-y", binding.get("yInputs", inputs))
                site = x if x == y else self.point(name, loc(x), loc(y))
            else:
                site = self.weighted_site(name, inputs)
            bound[binding["targetSiteKey"]] = site
            descriptions.append({"targetSiteKey": binding["targetSiteKey"], "siteId": site, "mode": binding.get("mode", "affine")})
        pairs = []
        pair_keys = set()
        for i, frame in enumerate(construction.get("axisRectangles", [])):
            tl, br = bound[frame["topLeftSiteKey"]], bound[frame["bottomRightSiteKey"]]
            bound[frame["topRightSiteKey"]] = self.point(key+f"-rectangle-tr-{i}", loc(br), loc(tl))
            bound[frame["bottomLeftSiteKey"]] = self.point(key+f"-rectangle-bl-{i}", loc(tl), loc(br))
        for i, pair in enumerate(construction.get("symmetryPairs", [])):
            first = bound[pair["firstSiteKey"]]
            axis = self.axis_site(key+f"-symmetry-axis-{i}", pair["axisInputs"])
            token = (pair["firstSiteKey"], pair["secondSiteKey"], axis)
            if token in pair_keys:
                continue
            pair_keys.add(token)
            second = self.point(key+f"-mirrored-site-{i}", loc(first, axis, 1, 1, axis), loc(first))
            bound[pair["secondSiteKey"]] = second
            pairs.append({"firstSiteId": first, "secondSiteId": second, "axisSiteId": axis})
        parallelograms = []
        for i, frame in enumerate(construction.get("parallelograms", [])):
            a, b, c = (bound[frame[name]] for name in ("aSiteKey", "bSiteKey", "cSiteKey"))
            d = self.point(key+f"-parallel-corner-{i}", loc(a, b, 1, 1, c), loc(a, b, 1, 1, c))
            bound[frame["dSiteKey"]] = d
            parallelograms.append({"aSiteId": a, "bSiteId": b, "cSiteId": c, "dSiteId": d})
        if bound:
            self.synthesis_rows.append({"operationId": key, "rung": self.recipe.at,
                "bindings": [row | {"siteId": bound[row["targetSiteKey"]]} for row in descriptions],
                "symmetryPairs": pairs, "parallelograms": parallelograms,
                "axisRectangles": [{name.replace("Key", "Id"): bound[site] for name, site in frame.items()}
                                   for frame in construction.get("axisRectangles", [])]})
        return bound

    def new_batch(self, operation):
        after = operation["afterEdges"]
        source_ids = operation["sourceEdgeIds"]
        refs = [site for key in source_ids for site in self.controls(key)]
        for edge in after:
            for key in edge.get("transform", {}).get("sourceEdgeIds", []):
                refs += self.controls(key)
        key = f"r{self.recipe.at}-{operation['id']}"
        bound_sites = self.bind_sites(key, operation.get("construction", {}))
        targets = [p for edge in after for p in edge["controls"]]
        if not targets:
            return
        vertices = operation.get("construction", {}).get("vertices", [])
        frame_points = vertices or targets
        minimum = tuple(min(p[j] for p in frame_points) for j in range(2))
        maximum = tuple(max(p[j] for p in frame_points) for j in range(2))
        frame_a = self.move(key+"-frame-a", minimum, refs)
        frame_b = self.move(key+"-frame-b", maximum, [frame_a, *refs])
        primitives = {}
        engine = operation.get("construction", {}).get("engine")
        shared_center = None
        if engine == "rectangle":
            sites, _ = self.recipe.rectangle(key+"-rectangle", frame_a, frame_b)
            self.sync()
            primitives = {self.positions[site]: site for site in sites}
        elif engine == "triangle" and len(vertices) == 3:
            width, height = (self.positions[frame_b][j]-self.positions[frame_a][j] for j in range(2))
            sites = []
            for i, vertex in enumerate(vertices):
                p = rounded(vertex)
                sites.append(self.point(key+f"-triangle-vertex-{i}",
                    loc(frame_a, frame_b, p[0]-self.positions[frame_a][0], width or 1),
                    loc(frame_a, frame_b, p[1]-self.positions[frame_a][1], height or 1)))
            self.recipe.poly(key+"-triangle", sites)
            primitives = {self.positions[site]: site for site in sites}
        elif engine == "centered-cross":
            shared_center = self.move(key+"-shared-center", operation["construction"]["center"], refs)
        if operation["kind"] == "balance-local-roof":
            left, old_peak = self.controls(source_ids[0])[0], self.controls(source_ids[0])[-1]
            right = self.controls(source_ids[1])[-1]
            axis = self.point(key+"-reflection-axis", loc(left, right, 1, 2), loc(old_peak))
            peak = self.point(key+"-relaxed-apex", loc(old_peak, axis, round(operation["construction"]["relaxation"]*1000), 1000), loc(old_peak))
            # The revised roof consumes the old apex and the actual base
            # midpoint. Remaining asymmetry stays visible after relaxation.
            primitives = {rounded(after[0]["controls"][0]): left, rounded(after[0]["controls"][-1]): peak,
                          rounded(after[1]["controls"][-1]): right}
        for edge in after:
            if edge["id"] in self.aliases:
                continue
            native_id = "g-"+edge["id"]
            controls = []
            transform = edge.get("transform")
            template = self.controls(transform["sourceEdgeIds"][0]) if transform else []
            if transform and transform.get("reversed"):
                template = list(reversed(template))
            for i, point in enumerate(edge["controls"]):
                wanted = rounded(point)
                endpoint = i in (0, len(edge["controls"])-1)
                token = edge.get("siteKeys", [None]*len(edge["controls"]))[i]
                if token is None and endpoint:
                    token = edge.get("endpointKeys", [None, None])[0 if i == 0 else 1]
                cache_key = (self.recipe.at, token, wanted)
                # A repeated profile must consume every corresponding actual
                # template control, including its endpoints. Its incident
                # profiles are explicitly reintroduced in this same patch and
                # then reuse the new shared endpoint sites.
                is_primitive_member = edge.get("primitive", {}).get("group") == after[0].get("primitive", {}).get("group")
                if operation["kind"] == "balance-local-roof":
                    is_primitive_member = edge in after[:2]
                force_control = transform or (shared_center is not None and edge in after[:2])
                site = bound_sites.get(token)
                if site is None:
                    site = None if force_control else (primitives.get(wanted) if is_primitive_member else None) or (self.endpoints.get(cache_key) if token else None)
                if site is None:
                    site_key = f"{native_id}-control-{i}"
                    if shared_center is not None:
                        p = self.positions[shared_center]
                        site = self.point(site_key,
                            loc("page-origin", "page-extent", wanted[0]-p[0], 512, shared_center),
                            loc("page-origin", "page-extent", wanted[1]-p[1], 512, shared_center))
                    elif transform and len(template) == len(edge["controls"]):
                        matrix = transform["matrix"]
                        if abs(matrix[1])+abs(matrix[3]) > 1e-9:
                            raise ValueError("Cross-axis affine transforms require a different native frame engine")
                        source = template[i]
                        # The actual earlier template control participates in both
                        # coordinates; a finite source/target span supplies scale.
                        low = self.move(site_key+"-source-zero", (0, 0), refs)
                        base = self.move(site_key+"-translation", (matrix[2], matrix[5]), refs)
                        site = self.point(site_key, loc(low, source, round(matrix[0]*1000000), 1000000, base),
                                          loc(low, source, round(matrix[4]*1000000), 1000000, base))
                    else:
                        # Preserve exact integer target controls with affine
                        # displacement, including zero-width/height frames.
                        site = self.move(site_key, wanted, [frame_a, frame_b])
                if token:
                    self.endpoints[cache_key] = site
                controls.append(site)
            self.recipe.edge(native_id, controls, edge["role"].replace("-", " "))
            self.aliases[edge["id"]] = native_id
            self.edges[native_id] = self.recipe.data["edges"][-1]
            if shared_center is not None and edge in after[:2]:
                for j, endpoint in enumerate((controls[0], controls[-1])):
                    self.recipe.edge(f"{native_id}-join-guide-{j}", [shared_center, endpoint], "Shared proposed cross center; original pen events remain separate")
            # The current specialized repeat reader validates translations only.
            # Axis-scaled reuse is represented by the actual template-control
            # dependencies above; do not misdeclare it as a translation request.

    def run(self):
        mapping, stages = [], []
        for index, step in enumerate(self.proposal["steps"]):
            self.recipe.begin(step["title"], "Offline interpretation pass; original acquisition order and exact samples are retained separately.")
            if index:
                previous = self.proposal["steps"][index-1]
                for edge in previous["edges"]+previous.get("residueEdges", []):
                    controls = self.controls(edge["id"])
                    for key, position, site in zip(edge["endpointKeys"], (edge["controls"][0], edge["controls"][-1]), (controls[0], controls[-1])):
                        self.endpoints[(index, key, rounded(position))] = site
            if index == 0:
                self.point("page-origin", 0, 0)
                self.point("page-extent", 512, 512)
                self.recipe.rectangle("measured-page", "page-origin", "page-extent")
                self.sync()
                for edge in step["edges"]:
                    controls = []
                    for i, position in enumerate(edge["controls"]):
                        endpoint = i in (0, len(edge["controls"])-1)
                        token = edge["endpointKeys"][0 if i == 0 else 1] if endpoint else edge["id"]+f"-handle-{i}"
                        site = self.endpoints.get((0, token))
                        if site is None:
                            site = "source-fit-"+token
                            p = rounded(position)
                            self.point(site, loc("page-origin", "page-extent", p[0], 512), loc("page-origin", "page-extent", p[1], 512))
                            self.endpoints[(0, token)] = site
                        controls.append(site)
                    native_id = "g-"+edge["id"]
                    self.recipe.edge(native_id, controls, "Measured source interval")
                    self.aliases[edge["id"]] = native_id
                    self.edges[native_id] = self.recipe.data["edges"][-1]
                bounds = self.observation["bounds"]
                refs = [site for edge in step["edges"] for site in self.controls(edge["id"])]
                a = self.move("observed-envelope-a", bounds[:2], refs)
                b = self.move("observed-envelope-b", bounds[2:], [a, *refs])
                self.envelope, _ = self.recipe.rectangle("observed-envelope", a, b)
                self.sync()
            else:
                for op in step["operations"]:
                    unchanged = op["sourceEdgeIds"] == op["targetEdgeIds"] and all(edge["id"] in self.aliases for edge in op["afterEdges"])
                    if op["accepted"] and not unchanged:
                        self.new_batch(op)
            visible = [self.aliases[edge["id"]] for edge in step["edges"]]
            residue = [self.aliases[edge["id"]] for edge in step.get("residueEdges", [])]
            for i, relation in enumerate(step.get("relations", [])):
                if relation["kind"] == "near-containment":
                    group = next(group for group in step["groups"] if group["id"] == relation["groupId"])
                    self.recipe.contain(f"r{index}-envelope-containment-{i}", self.envelope,
                        [self.aliases[edge] for edge in group["edgeIds"]],
                        "Conservative control-hull containment in the observed whole-drawing envelope; wall occupancy remains unresolved")
            self.recipe.finish(visible, [edge["id"] for edge in self.recipe.data["edges"] if edge["id"] not in visible])
            active = step["edges"]+step.get("residueEdges", [])
            mapping.append({"rung": index, "links": [{"spanIds": edge["spans"], "edgeIds": [self.aliases[edge["id"]]]} for edge in active if edge["spans"]],
                            "unmatchedSpanIds": sorted({span["id"] for span in self.observation["spans"]}-{sid for edge in active for sid in edge["spans"]}),
                            "inferredEdgeIds": [self.aliases[edge["id"]] for edge in active if edge.get("inferred")], "residueEdgeIds": residue})
            operations = []
            for op in step["operations"]:
                accepted = op["accepted"]
                unchanged = op["sourceEdgeIds"] == op["targetEdgeIds"]
                operations.append({"id": op["id"], "title": op["kind"].replace("-", " "), "status": "accepted" if accepted else "rejected",
                    "rule": op["kind"], "reason": op["reason"], "inputEdgeIds": [self.aliases[key] for key in op["sourceEdgeIds"]],
                    "replacementEdgeIds": [self.aliases[key] for key in op["targetEdgeIds"]] if accepted and not unchanged else [],
                    "sourceSpanIds": op["sourceSpans"], "metrics": op.get("measurements", {}) | {"scoreBefore": op.get("scoreBefore"), "scoreAfter": op.get("scoreAfter")},
                    "thresholds": op.get("thresholds", {}), "construction": op.get("construction", {})})
            roles = [{"id": group["id"], "label": group.get("role", group["kind"]).replace("-", " "),
                      "sourceSpanIds": group.get("spans", []), "edgeIds": [self.aliases[key] for key in group["edgeIds"]],
                      "qualification": "Provisional evidence-supported primitive; retained helper boundaries are guides."} for group in step.get("groups", [])]
            stages.append({"rungId": f"r{index}", "title": step["title"], "summary": step.get("summary", "Local authored preferences; fit and unresolved source detail remain visible."),
                "metrics": step["metrics"] | {"sourceFitRms": step["metrics"]["sourceRms"], "sourceFitMax": step["metrics"]["sourceMax"], "proposalFitRms": step["metrics"]["proposalRms"]},
                "residueEdgeIds": residue, "operations": operations, "roles": roles,
                "unsupported": [{"id": "remaining-ink", "label": "Retained small detail", "edgeIds": residue,
                    "sourceSpanIds": [sid for edge in step.get("residueEdges", []) for sid in edge["spans"]], "qualification": "Preserved as active faint source detail, not removed from the evidence."}] if residue else [],
                "relations": step.get("relations", []), "convergence": step["convergence"]})
            for name in ("viewHypothesis", "policyWeights", "referencePolicy", "conceptualMetrics", "view", "jointMerges"):
                if name in step:
                    stages[-1][name] = step[name]
            if "viewHypothesis" in step:
                def native_refs(value):
                    if isinstance(value, list):
                        return [native_refs(item) for item in value]
                    if isinstance(value, dict):
                        return {key: ([self.aliases[item] for item in items] if key in ("supportEdgeIds", "edgeIds", "participantEdgeIds") else native_refs(items)) for key, items in value.items()}
                    return value
                stages[-1]["viewHypothesis"] = native_refs(step["viewHypothesis"])
        source_mapping = {"sourceId": self.observation["sourceId"], "envelopeSiteIds": ["page-origin", "page-extent"], "passes": mapping}
        self.recipe.data["sourceMapping"] = source_mapping
        if self.synthesis_rows:
            self.recipe.data["synthesisMapping"] = self.synthesis_rows
        self.recipe.data["description"] = f"{len(stages)} source-linked interpretation passes with explicit prior controls, real primitive frames and retained acquired ink. Numeric precision is retained in this experiment."
        policy = self.proposal["policy"]
        if "completionPolicy" in self.proposal:
            policy = policy | {"completion": self.proposal["completionPolicy"]}
        return self.recipe.data, {"sourceId": self.observation["sourceId"], "programId": self.recipe.data["id"], "label": "Church "+self.observation["sourceId"],
                                  "policy": policy, "passes": stages, "sourceMapping": source_mapping}


def build(observations_path, source_path, output, synthesize=False):
    observations = [json.loads(line) for line in observations_path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]
    originals = {str(json.loads(line)["key_id"]): line for line in source_path.read_text(encoding="utf-8-sig").splitlines() if line.strip()}
    output.mkdir(parents=True, exist_ok=True)
    proposals, programs, records, packets = [], [], [], []
    for observation in observations:
        proposal = propose(observation)
        if synthesize:
            from quickdraw_gestalt_completion import extend
            proposal = extend(observation, proposal)
        program, record = Author(observation, proposal).run()
        original = originals[observation["sourceId"]]
        proposals.append(proposal); programs.append(program); records.append(record)
        packets.append({"sourceOriginalJson": original, "source": json.loads(original), "observation": observation, "program": program, "proposal": proposal})
    for filename, values in (("proposals.ndjson", proposals), ("programs.ndjson", programs), ("packets.ndjson", packets)):
        (output/filename).write_text("".join(json.dumps(value, separators=(",", ":"), ensure_ascii=False)+"\n" for value in values), encoding="utf-8")
    manifest = {"schema": "gojo.quickdraw-gestalt-manifest.v1", "policy": records[0]["policy"], "sources": records}
    if synthesize:
        manifest.update({"schema": "gojo.quickdraw-synthesis-manifest.v1", "title": "From rough joints to a synthesized church",
                         "introduction": "Follow seven stages from original ink to connected shapes and view-aware synthesis. Later stages give less weight to the acquired drawing and build increasingly from their own preceding structure."})
    (output/"manifest.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    (output/"observations.ndjson").write_bytes(observations_path.read_bytes())
    (output/"selected.ndjson").write_bytes(source_path.read_bytes())
    return {"drawings": len(programs), "passes": sum(len(program["rungs"]) for program in programs), "output": str(output)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--observations", type=Path, required=True)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--synthesize", action="store_true")
    args = parser.parse_args()
    print(json.dumps(build(args.observations, args.source, args.output, args.synthesize), indent=2))
