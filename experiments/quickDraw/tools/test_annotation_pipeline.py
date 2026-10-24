"""Annotation stays observational over frozen construction and acquisition."""
import json
import tempfile
import unittest
from pathlib import Path

from annotate_quickdraw_interpretation import build
from quickdraw_interpretation import build as build_constructions


class AnnotationPipelineTests(unittest.TestCase):
    def test_annotation_stage_preserves_input_bytes_and_exact_acquisition_text(self):
        source_text = '{"key_id":"fixture","drawing":[[[0,100,100],[0,0,100]],[[20.0000000000000001,80],[40,40]]]}\n'
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root/"input.ndjson"
            source.write_bytes(source_text.encode())
            destination = root/"run"
            build_constructions(source, destination)
            names = ("observations.ndjson", "programs.ndjson", "selected.ndjson")
            before = {name: (destination/name).read_bytes() for name in names}
            manifest = build(destination)
            self.assertTrue(manifest["constructionUnchanged"])
            self.assertEqual(manifest["programs"], 3)
            self.assertEqual(before, {name: (destination/name).read_bytes() for name in names})
            packets = [json.loads(line) for line in (destination/"annotation-packets.ndjson").read_text().splitlines()]
            self.assertEqual(len(packets), 3)
            self.assertTrue(all(p["sourceOriginalJson"] == source_text.rstrip("\n") for p in packets))
            self.assertEqual({p["annotations"]["mode"] for p in packets},
                             {"observed-fit", "gentle-priors", "stronger-priors"})


if __name__ == "__main__":
    unittest.main()
