// Script-level viewer verification with a small DOM facade; not a visual browser test.
import fs from 'node:fs/promises';
import vm from 'node:vm';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
const file=process.argv[2]||'Gojo.Demos.Web/minds/feedback-limits/index.html';
const html=await fs.readFile(file,'utf8');
const json=html.match(/<script type="application\/json" id="data">([\s\S]*?)<\/script>/)[1];
const script=[...html.matchAll(/<script>([\s\S]*?)<\/script>/g)][0][1];
class Element{
 constructor(){this.value='';this.textContent='';this.innerHTML='';this.disabled=false;this.listeners={};this.children=[];}
 append(x){this.children.push(x);if(this.value==='')this.value=x.value;}
 addEventListener(name,callback){this.listeners[name]=callback;}
 fire(name){this.listeners[name]?.();}
 click(){if(!this.disabled)this.fire('click');}
}
const elements=new Map([...html.matchAll(/id="([^"]+)"/g)].map(m=>[m[1],new Element()]));
elements.get('data').textContent=json;elements.get('scope').value='rounds';elements.get('step').value='0';
const context=vm.createContext({document:{getElementById:id=>{if(!elements.has(id))throw Error('Unknown DOM id '+id);return elements.get(id);},createElement:()=>new Element()},location:{search:'?case=quarter'},URLSearchParams,console});
vm.runInContext(script,context);
let checks=0;const assert=(truth,message)=>{checks++;if(!truth)throw Error(message)};
const data=JSON.parse(json),get=id=>elements.get(id);
for(let k=0;k<data.views.length;k++){
 const v=data.views[k];get('condition').value=String(k);get('condition').fire('change');
 for(const scope of ['rounds','events']){
  get('scope').value=scope;get('scope').fire('change');
  for(let i=0;i<v.gojo[scope].length;i++){
   get('step').value=String(i);get('step').fire('input');const row=v.gojo[scope][i];
   for(const [id,key]of [['av','a'],['bv','b'],['mean','mean'],['gap','gap']])assert(get(id).textContent===row.values[key],v.case.id+' '+scope+' '+i+' '+key);
   assert(!/NaN|Infinity/.test(get('valuePlot').innerHTML+get('motionPlot').innerHTML),'Invalid chart coordinate');
   assert(get('prev').disabled===(i===0),'Previous button');assert(get('next').disabled===(i===v.gojo[scope].length-1),'Next button');
  }
  get('step').value='0';get('step').fire('input');get('next').click();assert(Number(get('step').value)===1,'Next action');get('prev').click();assert(Number(get('step').value)===0,'Previous action');
 }
}
for(const match of get('downloads').innerHTML.matchAll(/href="([^"]+)"/g))await fs.access(new URL(match[1],pathToFileURL(path.resolve(file))));
console.log('Passed '+checks+' viewer state/controls/chart checks across '+data.views.length+' distinct trajectories. This check does not render CSS.');
