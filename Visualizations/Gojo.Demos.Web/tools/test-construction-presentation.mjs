import test from 'node:test';
import assert from 'node:assert/strict';
import {compactConstruction,readConstruction,expandConstruction} from './construction-presentation.mjs';

test('lossless transport retains distinct IDs, ordering, signed values and nested reads',()=>{
  const shared={controls:[[0,-3],[127,512]],sourceSiteIds:['first','second'],visible:true,note:'Repeated captured evidence remains unchanged.'};
  const source={scenes:[{id:'one',reads:[shared,shared]},{id:'two',reads:[structuredClone(shared)]}],residue:null,value:0};
  const packet=compactConstruction(source);
  const restored=readConstruction('<script id="construction-data" type="application/json">'+JSON.stringify(packet)+'</script>');
  assert.deepEqual(restored,source);
  restored.scenes[0].reads[0].controls[0][0]=99;
  assert.equal(restored.scenes[0].reads[1].controls[0][0],0,'Decoded equal containers remain independently editable.');
});

test('transport refuses ambiguous reference fields and out-of-range references',()=>{
  assert.throws(()=>compactConstruction({ordinary:{$ref:5}}),/Reserved/);
  assert.throws(()=>expandConstruction({encoding:'interned-json-v1',values:[],root:{$ref:0}}),/Invalid/);
});
