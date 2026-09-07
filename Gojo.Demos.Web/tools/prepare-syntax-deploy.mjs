// Reconcile the cached browser bundle with the current public deployment,
// then overlay only the syntax website and its landing-page/config changes.
// This does not deploy. A complete cached bundle must already be present.
import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';
const web=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const cache=path.join(web,'publish-deploy-root');
const stage=path.join(web,'publish-deploy','syntax-site');
const origin='https://gojo.inklingai.ca/';
const backup=await fs.mkdtemp(path.join(os.tmpdir(),'GojoSyntaxDeployBackup-'));
const baseline=new Map();
const hash=bytes=>'sha256-'+crypto.createHash('sha256').update(bytes).digest('base64');
async function walk(dir){
  const result=[];
  for(const entry of await fs.readdir(dir,{withFileTypes:true})){
    const file=path.join(dir,entry.name);
    if(entry.isDirectory())result.push(...await walk(file));
    else result.push(file);
  }
  return result;
}
const applications=['blocks','gears','gearLadder','sketch'];
// Cached experimental variants can outlive their public deployment. Do not
// publish them merely because a root navigation fallback answers their URL.
const cachedLoaders=(await walk(cache)).filter(f=>f.endsWith(path.join('_framework','dotnet.js')));
for(const loader of cachedLoaders){
  const relative=path.relative(cache,loader);
  if(applications.includes(path.dirname(path.dirname(relative))))continue;
  const response=await fetch(new URL(relative.replaceAll(path.sep,'/'),origin),{signal:AbortSignal.timeout(45000)});
  await response.body?.cancel();
  if(response.status!==404)throw new Error('Additional live route requires inclusion before deployment: '+relative);
}
await fs.mkdir(stage,{recursive:true});
for(const app of applications)await fs.cp(path.join(cache,app),path.join(stage,app),{recursive:true});
const originalFiles=await walk(stage);
const loaders=originalFiles.filter(f=>f.endsWith(path.join('_framework','dotnet.js')));
if(loaders.length<4)throw new Error('A complete existing browser deployment is required');
function target(relative){
  const file=path.resolve(stage,relative);
  if(!file.startsWith(stage+path.sep))throw new Error('Path leaves deployment root: '+relative);
  return file;
}
async function readLive(relative){
  const url=new URL(relative.replaceAll(path.sep,'/'),origin);
  if(url.origin!==new URL(origin).origin)throw new Error('Unexpected asset origin');
  const response=await fetch(url,{signal:AbortSignal.timeout(45000)});
  if(!response.ok)throw new Error(response.status+' '+url);
  const bytes=Buffer.from(await response.arrayBuffer());
  if(!relative.endsWith('.html')&&/^\s*<!doctype html/i.test(bytes.toString('utf8',0,100)))
    throw new Error('Asset returned an HTML fallback: '+relative);
  return bytes;
}
async function write(relative,bytes){
  const file=target(relative);
  let old;
  try{old=await fs.readFile(file);}catch(e){if(e.code!=='ENOENT')throw e;}
  if(old?.equals(bytes))return false;
  if(old){
    const saved=path.join(backup,relative);
    await fs.mkdir(path.dirname(saved),{recursive:true});
    await fs.writeFile(saved,old);
  }
  await fs.mkdir(path.dirname(file),{recursive:true});await fs.writeFile(file,bytes);
  return true;
}
async function preserve(relative,expected){
  let bytes;
  if(expected){
    try{bytes=await fs.readFile(target(relative));}catch(e){if(e.code!=='ENOENT')throw e;}
    if(bytes&&hash(bytes)!==expected)bytes=undefined;
  }
  if(!bytes)bytes=await readLive(relative);
  if(expected&&hash(bytes)!==expected)throw new Error('Live manifest integrity mismatch: '+relative);
  baseline.set(relative.replaceAll(path.sep,'/'),hash(bytes));
  if(await write(relative,bytes)){
    // Recreate precompressed siblings whenever the underlying asset changed.
    for(const [extension,compress] of [['.br',zlib.brotliCompressSync],['.gz',zlib.gzipSync]])
      await write(relative+extension,compress(bytes));
  }
  return bytes;
}
for(const loader of loaders){
  const relative=path.relative(stage,loader);
  const bytes=await preserve(relative);
  const source=bytes.toString('utf8'),start=source.indexOf('/*json-start*/'),end=source.indexOf('/*json-end*/');
  if(start<0||end<start)throw new Error('Unrecognized runtime manifest: '+relative);
  const config=JSON.parse(source.slice(start+14,end)),resources=new Map();
  function collect(value){
    if(!value||typeof value!=='object')return;
    if(typeof value.name==='string')resources.set(value.name,value.hash);
    for(const child of Object.values(value))collect(child);
  }
  collect(config.resources);
  const entries=[...resources.entries()];
  for(let i=0;i<entries.length;i+=6)
    await Promise.all(entries.slice(i,i+6).map(([name,integrity])=>preserve(path.join(path.dirname(relative),name),integrity)));
  const app=path.dirname(path.dirname(relative));
  for(const name of ['index.html','main.js','app.css'])await preserve(path.join(app,name));
  console.log('Preserved live '+app+': '+entries.length+' runtime resources.');
}
await fs.writeFile(path.join(backup,'live-baseline.json'),JSON.stringify(Object.fromEntries(baseline),null,2));
for(const file of originalFiles){
  // Verify untouched cached precompressed payloads agree with their plain
  // counterpart for the resources retained by the live manifests.
  const relative=path.relative(stage,file),plain=relative.replace(/\.(br|gz)$/,'');
  if(plain===relative||!baseline.has(plain.replaceAll(path.sep,'/')))continue;
  const bytes=await fs.readFile(file);
  const decoded=file.endsWith('.br')?zlib.brotliDecompressSync(bytes):zlib.gunzipSync(bytes);
  if(hash(decoded)!==baseline.get(plain.replaceAll(path.sep,'/'))){
    const source=await fs.readFile(target(plain));
    await write(relative,file.endsWith('.br')?zlib.brotliCompressSync(source):zlib.gzipSync(source));
  }
}
for(const name of ['index.html','app.css','staticwebapp.config.json'])
  await write(name,await fs.readFile(path.join(web,name)));
for(const file of await walk(path.join(web,'syntax')))
  await write(path.relative(web,file),await fs.readFile(file));
console.log('Prepared syntax overlay. Preserved '+baseline.size+' existing public assets.');
console.log('Deployment directory: '+stage);
console.log('Backup and public-asset manifest: '+backup);
