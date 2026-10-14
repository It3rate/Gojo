import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
const repo=process.cwd(), web=path.join(repo,'Gojo.Demos.Web'), minds=path.join(web,'minds');
async function walk(dir){return (await Promise.all((await fs.readdir(dir,{withFileTypes:true})).map(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]))).flat();}
let checked=0;
for(const file of await walk(minds)){
 if(!file.endsWith('.html'))continue;
 const text=await fs.readFile(file,'utf8');
 if(/__TRACE_JSON__|file:\/\/\/|C:\\Users\\/.test(text))throw new Error('Unpublished placeholder/path '+file);
 for(const [,url] of text.matchAll(/(?:href|src)="([^"]+)"/g)){
  if(!url.startsWith('/minds/'))continue;
  const parsed=new URL(url,'https://gojo.inklingai.ca');
  let target=path.join(web,parsed.pathname);if(parsed.pathname.endsWith('/'))target=path.join(target,'index.html');
  await fs.access(target);checked++;
  if(parsed.hash){const body=await fs.readFile(target,'utf8');if(!body.includes('id="'+parsed.hash.slice(1)+'"'))throw new Error('Missing anchor '+url);}
 }
}
const original=await fs.readFile(path.join(repo,'Minds/results/two-space/trace.json'),'utf8');
const published=await fs.readFile(path.join(minds,'two-space/trace.json'),'utf8');
if(original!==published)throw new Error('Trace changed during publication');
const viewer=await fs.readFile(path.join(minds,'two-space/index.html'),'utf8');
const embedded=viewer.match(/<script type="application\/json" id="data">([\s\S]*?)<\/script>/)[1];
if(JSON.stringify(JSON.parse(embedded))!==JSON.stringify(JSON.parse(original)))throw new Error('Embedded data mismatch');
const config=JSON.parse(await fs.readFile(path.join(web,'staticwebapp.config.json'),'utf8'));
if(!config.navigationFallback.exclude.includes('/minds/*'))throw new Error('Missing fallback exclusion');
const selection=JSON.parse(await fs.readFile(path.join(repo,'Minds/results/predictions/publication.json'),'utf8'));
const capture=path.join(repo,'Minds/results/predictions',selection.directory);
const protocol=await fs.readFile(path.join(capture,'Protocol.md'));
const record=JSON.parse(await fs.readFile(path.join(capture,'protocol-record.json'),'utf8'));
if(crypto.createHash('sha256').update(protocol).digest('hex').toUpperCase()!==record.protocolSha256)
 throw new Error('Captured prediction protocol checksum changed');
if(!protocol.equals(await fs.readFile(path.join(minds,'predictions/Protocol.md'))))
 throw new Error('Published prediction protocol bytes differ from the captured protocol');
console.log('Verified '+checked+' local page links/anchors, unchanged trace and embedded data, and missing-route configuration.');
