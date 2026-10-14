import fs from 'node:fs/promises';
import path from 'node:path';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const directory=path.resolve('Gojo.Demos.Web/minds/predictions');
const html=await fs.readFile(path.join(directory,'index.html'),'utf8');
const payload=html.match(/<script id="data" type="application\/json">([\s\S]*?)<\/script>/)[1];
const data=JSON.parse(payload),script=html.match(/<script>\s*([\s\S]*?)<\/script>/)[1];
const dom=new Map();
for(const [,id] of html.matchAll(/\bid="([^"]+)"/g))dom.set(id,{value:'0',checked:false,innerHTML:'',textContent:'',handlers:{},addEventListener(event,fn){this.handlers[event]=fn;}});
dom.get('data').textContent=payload;dom.get('after').checked=true;
vm.runInNewContext(script,{document:{getElementById:id=>{assert(dom.has(id),'Missing element '+id);return dom.get(id);}},URLSearchParams,location:{search:'?case=reversal'}});
let checks=0;
for(const run of data.completed){
 const captured=JSON.parse(await fs.readFile(path.join(directory,'runs',run.case.id+'.json'),'utf8'));
 assert.deepEqual(run,captured);checks++;
 const events=(await fs.readFile(path.join(directory,'runs',run.case.id+'-events.jsonl'),'utf8')).trim().split(/\r?\n/).map(JSON.parse);
 assert.equal(events.length,run.rows.length*2);checks++;
 dom.get('case').value=run.case.id;dom.get('case').handlers.change();
 for(let i=0;i<run.rows.length;i++){
  const row=run.rows[i],before=events[i*2],after=events[i*2+1];
  assert.equal(before.phase,'before-observation');assert.equal(before.prediction,row.prediction);
  assert.equal(Object.hasOwn(before,'outcome'),false);assert.deepEqual(after.row,row);checks+=4;
  for(const reveal of [false,true]){
   dom.get('step').value=String(i);dom.get('after').checked=reveal;dom.get('step').handlers.input();
   assert.equal(dom.get('prediction').textContent,row.prediction??'unavailable');
   assert.equal(dom.get('hold').textContent,run.reference[i].holdLast??'unavailable');
   assert.equal(dom.get('outcome').textContent,reveal?(row.outcome===null?'missing':String(row.outcome)):'not yet admitted');
   assert.equal(dom.get('error').textContent,reveal?(row.error??'unavailable'):'not yet assessed');
   assert.equal(dom.get('previous').disabled,i===0);assert.equal(dom.get('next').disabled,i===run.rows.length-1);
   const chart=dom.get('chart').innerHTML;assert(!/NaN|Infinity|undefined/.test(chart));
   assert.equal((chart.match(/<circle /g)||[]).length,run.rows.slice(0,reveal?i+1:i).filter(r=>r.outcome!==null).length);
   assert.equal((chart.match(/<path /g)||[]).length,run.rows.slice(0,i+1).filter(r=>r.prediction!==null).length);
   assert.equal((chart.match(/<rect /g)||[]).length,run.reference.slice(0,i+1).filter(r=>r.holdLast!==null).length);
   assert(dom.get('download').href.endsWith(run.case.id+'.json'));checks+=11;
  }
 }
 dom.get('step').value='0';dom.get('next').handlers.click();assert.equal(Number(dom.get('step').value),1);
 dom.get('previous').handlers.click();assert.equal(Number(dom.get('step').value),0);checks+=2;
}
assert.equal(data.failures.length,0);assert(data.completed.every(r=>r.differences.length===0));
console.log(`Passed ${checks+2} checks over all cases, targets and reveal states; event journals and embedded traces agree. Script-level DOM checks, not browser layout rendering.`);
