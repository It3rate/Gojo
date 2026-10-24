// Lossless transport for captured inspector JSON. This does not calculate geometry.
import zlib from 'node:zlib';
import assert from 'node:assert/strict';

export function readConstruction(html){
  const match=html.match(/<script\b[^>]*id=["']construction-data["'][^>]*>([\s\S]*?)<\/script>/i);
  if(!match)throw new Error('Missing captured construction payload.');
  let value=JSON.parse(match[1]);
  if(value.encoding==='gzip-base64')value=JSON.parse(zlib.gunzipSync(Buffer.from(value.payload,'base64')));
  return expandConstruction(value);
}

export function expandConstruction(packet){
  if(packet.encoding!=='interned-json-v1')return packet;
  function expand(value){
    if(Array.isArray(value))return value.map(expand);
    if(value&&typeof value==='object'){
      if(Object.keys(value).length===1&&Number.isInteger(value.$ref)){
        if(value.$ref<0||value.$ref>=packet.values.length)throw new Error('Invalid captured read reference.');
        return expand(packet.values[value.$ref]);
      }
      return Object.fromEntries(Object.entries(value).map(([key,item])=>[key,expand(item)]));
    }
    return value;
  }
  return expand(packet.root);
}

export function compactConstruction(data){
  const counts=new Map(),table=[],indices=new Map(),texts=new WeakMap();
  function scan(value){
    if(!value||typeof value!=='object')return;
    if(!Array.isArray(value)&&Object.hasOwn(value,'$ref'))throw new Error('Reserved presentation reference field.');
    const text=JSON.stringify(value);texts.set(value,text);
    if(text.length>=64)counts.set(text,(counts.get(text)||0)+1);
    for(const child of Object.values(value))scan(child);
  }
  function pack(value){
    if(!value||typeof value!=='object')return value;
    const text=texts.get(value);
    if(counts.get(text)>1&&indices.has(text))return{$ref:indices.get(text)};
    const packed=Array.isArray(value)?value.map(pack):Object.fromEntries(Object.entries(value).map(([key,item])=>[key,pack(item)]));
    if(counts.get(text)>1){const index=table.length;table.push(packed);indices.set(text,index);return{$ref:index};}
    return packed;
  }
  scan(data);
  const root=pack(data),packet={encoding:'interned-json-v1',values:table,root};
  assert.deepEqual(expandConstruction(packet),data,'Publication compaction must preserve every captured value.');
  return{encoding:'gzip-base64',payload:zlib.gzipSync(Buffer.from(JSON.stringify(packet)),{level:9}).toString('base64')};
}

export function compactConstructionHtml(html,data){
  const envelope=JSON.stringify(compactConstruction(data));
  let result=html.replace(/(<script\b[^>]*id=["']construction-data["'][^>]*>)[\s\S]*?(<\/script>)/i,(_,a,b)=>a+envelope+b);
  const start='const DATA=envelope.encoding',end=",NS='http://www.w3.org/2000/svg';";
  const from=result.indexOf(start),to=result.indexOf(end,from);
  if(from<0||to<0)throw new Error('Unrecognized construction viewer initialization.');
  const decode=expandConstruction.toString();
  result=result.slice(0,from)+decode+'\nconst packed='+result.slice(from+'const DATA='.length,to)+";\nconst DATA=expandConstruction(packed)"+result.slice(to);
  assert.deepEqual(readConstruction(result),data,'Published HTML must retain the complete captured payload.');
  return result;
}
