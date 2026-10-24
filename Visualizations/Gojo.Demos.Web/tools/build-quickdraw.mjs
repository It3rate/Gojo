// Publish explicitly prepared drawing studies and an extensible category index.
// This adapter copies captured results. It does not run or reinterpret Gojo.
// node Visualizations/Gojo.Demos.Web/tools/build-quickdraw.mjs [--check|--dry-run]
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';
import {readConstruction,compactConstructionHtml} from './construction-presentation.mjs';

const web=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const repo=path.resolve(web,'../..'),category=path.join(web,'quickdraw');
const check=process.argv.includes('--check'),dryRun=process.argv.includes('--dry-run');
if(check&&dryRun)throw new Error('Select either --check or --dry-run.');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const escape=value=>String(value).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
function inside(root,relative){
  if(typeof relative!=='string'||!relative||!/^[A-Za-z0-9._/-]+$/.test(relative)||
      relative.startsWith('/')||relative.split('/').some(part=>!part||part==='.'||part==='..'))
    throw new Error('Invalid selected publication path: '+relative);
  const resolved=path.resolve(root,relative);
  if(!resolved.startsWith(root+path.sep))throw new Error('Publication path leaves its root.');
  return resolved;
}
async function walk(dir){
  let entries;try{entries=await fs.readdir(dir,{withFileTypes:true});}catch(e){if(e.code==='ENOENT')return[];throw e;}
  const result=[];
  for(const entry of entries){
    const file=path.join(dir,entry.name);
    if(entry.isSymbolicLink())throw new Error('Symlink in publication directory: '+file);
    if(entry.isDirectory())result.push(...await walk(file));else result.push(file);
  }
  return result;
}
async function write(root,relative,bytes){
  if(dryRun)return;
  const target=inside(root,relative);
  let actual;try{actual=await fs.readFile(target);}catch(e){if(e.code!=='ENOENT')throw e;}
  if(actual?.equals(bytes))return;
  if(check)throw new Error('Missing or stale published file: '+target);
  await fs.mkdir(path.dirname(target),{recursive:true});await fs.writeFile(target,bytes);
}
async function buildStudy(study){
  const source=inside(repo,study.source),output=inside(category,study.id),route='/quickdraw/'+study.id+'/';
  const selection=JSON.parse(await fs.readFile(path.join(source,'public-files.json'),'utf8'));
  if(!Array.isArray(selection.files)||!selection.files.includes('index.html')||
      new Set(selection.files).size!==selection.files.length||selection.files.includes('publication.json'))
    throw new Error('Publication selection needs a unique file list including index.html.');
  const files=new Map(),records=[],externalDownloads=[];
  const archiveUrls=new Map();
  if(study.archiveStorage){
    const {repository,commit}=study.archiveStorage;
    if(!/^[A-Za-z0-9_-]+\/[A-Za-z0-9_.-]+$/.test(repository)||!/^[a-f0-9]{40}$/.test(commit))
      throw new Error('Archive storage requires an explicit repository and immutable commit.');
    for(const relative of selection.files.filter(file=>file.endsWith('.gojo.json.gz')))
      archiveUrls.set(relative,`https://raw.githubusercontent.com/${repository}/${commit}/${study.source}/${relative}`);
  }
  for(const relative of selection.files){
    const input=inside(source,relative),real=await fs.realpath(input);
    if(!real.startsWith(source+path.sep))throw new Error('Selected source leaves its directory: '+relative);
    const bytes=await fs.readFile(input),plain=relative.endsWith('.gz')?zlib.gunzipSync(bytes):bytes;
    const text=plain.toString('utf8');
    if(/\.(?:html?|css|js|mjs|json|ndjson|txt|md|svg)(?:\.gz)?$/i.test(relative)&&/file:\/{1,3}|\b[A-Za-z]:[\\/](?!\/)/i.test(text))
      throw new Error('Local machine path in selected publication: '+relative);
    let published=bytes;
    if(archiveUrls.has(relative)){
      JSON.parse(text);
      externalDownloads.push({path:relative,url:archiveUrls.get(relative),sha256:hash(bytes),decodedSha256:hash(plain),bytes:bytes.length});
      continue;
    }
    if(relative==='index.html'){
      if(!/<head(?:\s[^>]*)?>/i.test(text))throw new Error('Entry is missing its HTML head.');
      const base=text.match(/<base\s+href=["']([^"']+)["']\s*\/?>/i);
      if(/<base\b/i.test(text)&&base?.[1]!==route)throw new Error('Entry has an incompatible base URL.');
      let entry=base?text:text.replace(/<head(?:\s[^>]*)?>/i,'$&<base href="'+route+'">');
      if(study.compactCapturedReads){
        const payload=readConstruction(entry);
        for(const scene of payload.scenes)for(const design of scene.variants)for(const read of design.resolutions||[design])
          for(const key of ['archive','archivePath'])if(read[key]){
            if(!archiveUrls.has(read[key]))throw new Error('Unselected external archive '+read[key]);
            read[key]=archiveUrls.get(read[key]);
          }
        entry=compactConstructionHtml(entry,payload);
      }
      published=Buffer.from(entry);
    }
    if(relative.endsWith('.json.gz'))JSON.parse(text);
    files.set(relative,published);
    records.push({path:relative,sourceSha256:hash(bytes),publishedSha256:hash(published),bytes:published.length});
  }
  // Keep the earlier executable downloads at their existing URLs.
  if(study.retainedDownloadsSource){
    const retained=inside(repo,study.retainedDownloadsSource);
    const previous=JSON.parse(await fs.readFile(path.join(retained,'public-files.json'),'utf8'));
    for(const relative of previous.files.filter(file=>file.startsWith('archives/')&&file.endsWith('.gojo.json.gz'))){
      if(files.has(relative))throw new Error('Retained download conflicts with selected output.');
      const bytes=await fs.readFile(inside(retained,relative));JSON.parse(zlib.gunzipSync(bytes));
      files.set(relative,bytes);records.push({path:relative,sourceSha256:hash(bytes),publishedSha256:hash(bytes),bytes:bytes.length,retainedFrom:study.retainedDownloadsSource});
    }
  }
  const entry=files.get('index.html').toString('utf8');
  let sceneCount=null;
  if(study.readSurface){
    const construction=study.readSurface==='restored-native-construction';
    if(!construction&&study.readSurface!=='restored-native-inspector')throw new Error('Unknown selected study read surface.');
    const embedded=construction?entry.match(/<script\b[^>]*id=["']construction-data["'][^>]*>([\s\S]*?)<\/script>/i):entry.match(/<script\b[^>]*id=["']scene-data["'][^>]*>([\s\S]*?)<\/script>/i);
    if(!embedded)throw new Error('Entry lacks the captured native scene payload.');
    let payload=construction?readConstruction(entry):JSON.parse(embedded[1]);
    if(/file:\/{1,3}|\b[A-Za-z]:[\\/](?!\/)/i.test(JSON.stringify(payload)))throw new Error('Machine-local path in captured native payload.');
    if(!Array.isArray(payload.scenes)||payload.scenes.length===0)throw new Error('Entry has no captured scenes.');
    const reads=construction?payload.scenes.flatMap(scene=>scene.variants.flatMap(design=>design.resolutions||[design])):payload.scenes;
    if(reads.length===0)throw new Error('Entry has no captured native reads.');
    for(const scene of reads){
      if(scene.nativeRestoreVerified!==true||(construction&&scene.nativeReplayVerified!==true))throw new Error('Unverified scene in public entry: '+scene.id);
      for(const link of [scene.archive,scene.archivePath])
        if(link&&!files.has(link)&&!externalDownloads.some(file=>file.url===link))throw new Error('Unpublished native archive link: '+link);
    }
    sceneCount=payload.scenes.length;
  }
  const publication={schema:'gojo.quickdraw.website-publication.v1',route,source:study.source,scenes:sceneCount,
    scope:'Captured study presentation and explicitly selected downloads; no experiment execution during publication.',files:records,
    ...(externalDownloads.length?{archiveStorage:study.archiveStorage,externalDownloads}:{}),
    ...(study.compactCapturedReads?{presentation:'Lossless shared-subtree transport; every decoded captured value checked against source.'}:{})};
  files.set('publication.json',Buffer.from(JSON.stringify(publication,null,2)+'\n'));
  if(!dryRun){
    // Remove only obsolete files previously owned by this study's manifest.
    let previous=[];
    try{previous=JSON.parse(await fs.readFile(path.join(output,'publication.json'),'utf8')).files.map(file=>file.path);}
    catch(e){if(e.code!=='ENOENT')throw e;}
    for(const file of await walk(output)){
      const relative=path.relative(output,file).replaceAll(path.sep,'/');
      if(files.has(relative))continue;
      if(check||!previous.includes(relative))throw new Error('Unselected file in study publication: '+relative);
      await fs.unlink(inside(output,relative));
    }
    for(const [relative,bytes] of files)await write(output,relative,bytes);
  }
  return {...study,route,scenes:sceneCount,files:files.size,bytes:[...files.values()].reduce((sum,bytes)=>sum+bytes.length,0)};
}
const catalogue=JSON.parse(await fs.readFile(path.join(web,'tools/quickdraw-studies.json'),'utf8'));
if(!Array.isArray(catalogue.studies)||new Set(catalogue.studies.map(s=>s.id)).size!==catalogue.studies.length||
    catalogue.studies.some(s=>!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(s.id)))throw new Error('Invalid study catalogue.');
const studies=[];
for(const study of catalogue.studies)studies.push(await buildStudy(study));
const cards=studies.map(s=>`<a class="card" href="${s.route}"><span class="kind">${escape(s.kind)}</span><h3>${escape(s.title)}</h3><p>${escape(s.description)}</p><span class="route">${escape(s.detail)}</span></a>`).join('\n');
const index=Buffer.from(`<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="description" content="Gojo drawing evidence, reconstruction and Quick Draw experiments."><title>Quick Draw studies · Gojo</title><link rel="stylesheet" href="/app.css"></head>
<body><main><nav aria-label="Gojo navigation" style="margin-bottom:32px"><a href="/" style="color:#076c77;text-decoration:none">← All Gojo demos</a></nav>
<header class="intro"><p class="eyebrow">Gojo · Drawing studies</p><h1>Quick Draw</h1><p class="summary">Drawings as evidence: explore their structure, preserve their imperfections, and investigate how much can be reconstructed from a few hints.</p><p class="note">Each study distinguishes observed drawings, authored controls and calculated reads.</p></header>
<section aria-labelledby="studies-heading"><div class="section-heading"><p class="section-number">01</p><h2 id="studies-heading">Studies and inspectors</h2></div><div class="cards">${cards}</div></section>
<p class="note" style="margin-top:32px">This collection will grow as the acquisition, reconstruction and learning experiments develop. Clean line-art controls and authored construction programs remain separate from imperfect observations and learned intention.</p>
</main></body></html>\n`);
const publicCatalogue=Buffer.from(JSON.stringify({schema:'gojo.quickdraw.catalogue.v1',studies:studies.map(({id,title,description,kind,detail,route,scenes})=>({id,title,description,kind,detail,route,scenes}))},null,2)+'\n');
await write(category,'index.html',index);await write(category,'catalogue.json',publicCatalogue);
console.log(JSON.stringify({mode:dryRun?'dry-run':check?'check':'build',route:'/quickdraw/',studies:studies.map(({id,scenes,files,bytes})=>({id,scenes,files,bytes})),
  bytes:index.length+publicCatalogue.length+studies.reduce((sum,s)=>sum+s.bytes,0)},null,2));
