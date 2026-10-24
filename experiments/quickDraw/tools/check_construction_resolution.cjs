// Exercise saved native reads locally. This checker never visits the website.
const {chromium}=require(process.env.GOJO_PLAYWRIGHT_MODULE||'playwright');
const fs=require('node:fs/promises'),path=require('node:path'),http=require('node:http');
const assert=require('node:assert/strict'),zlib=require('node:zlib'),crypto=require('node:crypto');
const {pathToFileURL}=require('node:url');
const review=path.resolve(process.env.GOJO_CONSTRUCTION_REVIEW||'D:/_quickDraw/gojo-construction-resolution/review');
const qa=path.resolve(process.env.GOJO_CONSTRUCTION_QA||path.join(review,'..','qa'));
const digest=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const pathOf=edge=>'M'+edge.controls[0].join(' ')+({2:'L',3:'Q',4:'C'}[edge.controls.length])+edge.controls.slice(1).map(p=>p.join(' ')).join(' ');
function envelope(html){const raw=JSON.parse(html.match(/<script id="construction-data" type="application\/json">([\s\S]*?)<\/script>/)[1]);return raw.encoding==='gzip-base64'?JSON.parse(zlib.gunzipSync(Buffer.from(raw.payload,'base64'))):raw}

(async()=>{
  const server=http.createServer(async(req,res)=>{
    try{
      let file=path.resolve(review,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
      if(file!==review&&!file.startsWith(review+path.sep)){res.writeHead(403);return res.end()}
      if((await fs.stat(file)).isDirectory())file=path.join(file,'index.html');
      const bytes=await fs.readFile(file);res.writeHead(200,{'Content-Type':file.endsWith('.html')?'text/html':file.endsWith('.gz')?'application/gzip':'application/json'});res.end(bytes);
    }catch{res.writeHead(404);res.end()}
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base='http://127.0.0.1:'+server.address().port;
  let browser;
  try{
    await fs.mkdir(qa,{recursive:true});
    const data=envelope(await fs.readFile(path.join(review,'index.html'),'utf8'));
    const designs=data.scenes.flatMap(scene=>scene.variants.map(design=>({scene,design})));
    const programs=designs.flatMap(row=>row.design.resolutions.map(variant=>({...row,variant})));
    assert.ok(data.resolution.levels.length>1);assert.equal(data.public,true);
    assert.equal(new Set(programs.map(row=>row.variant.id)).size,programs.length);
    for(const {design}of designs)assert.deepEqual(design.resolutions.map(v=>v.resolution.id),data.resolution.levels.map(level=>level.id));
    const manifest=JSON.parse(await fs.readFile(path.join(review,'public-files.json'),'utf8'));
    const archiveFiles=new Map(),packageHashes=[];
    for(const relative of manifest.files){
      assert.ok(!path.isAbsolute(relative)&&!relative.split(/[\\/]/).includes('..'));
      const bytes=await fs.readFile(path.join(review,relative));
      packageHashes.push({path:relative,bytes:bytes.length,sha256:digest(bytes)});
      if(relative.endsWith('.gojo.json.gz')){const decoded=zlib.gunzipSync(bytes);JSON.parse(decoded);archiveFiles.set(relative,decoded)}
    }
    assert.equal(archiveFiles.size,programs.length);
    for(const {variant}of programs){assert.equal(variant.archiveKind,'program');assert.ok(archiveFiles.has(variant.archivePath));assert.ok(variant.nativeRestoreVerified&&variant.nativeReplayVerified)}
    browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context=await browser.newContext({viewport:{width:1480,height:1100},reducedMotion:'reduce'});
    const errors=[],externalRequests=[];
    await context.route('**/*',async route=>{
      const url=route.request().url();
      if(url.startsWith(base+'/')||url.startsWith('blob:')||url.startsWith('data:')||url.startsWith('file:'))return route.continue();
      externalRequests.push(url);await route.abort();
    });
    const page=await context.newPage();page.on('pageerror',error=>errors.push(error.message));
    await page.goto(base+'/');await page.waitForSelector('#resolution-panel:not([hidden])');
    async function select(scene,design,index,level){
      await page.evaluate(hash=>{location.hash=hash},[scene.id,design.id,index,level].map(encodeURIComponent).join('/'));
      await page.waitForFunction(expected=>location.hash.slice(1)===expected.hash&&document.querySelector('#resolution-label').textContent===expected.label&&document.querySelector('#rung').value===String(expected.index)&&document.querySelector('.variant-button[aria-pressed=true]')?.textContent===expected.designLabel,
        {hash:[scene.id,design.id,index,level].map(encodeURIComponent).join('/'),label:data.resolution.levels.find(l=>l.id===level).label,index,designLabel:design.label});
    }
    async function setRung(index){await page.locator('#rung').evaluate((node,value)=>{node.value=value;node.dispatchEvent(new Event('input',{bubbles:true}))},String(index))}
    let rungReads=0,coordinatePathReads=0,comparisonPathReads=0,dependencySelections=0;
    const readChecks=[];
    for(const {scene,design}of designs){
      for(let levelIndex=0;levelIndex<design.resolutions.length;levelIndex++){
        const variant=design.resolutions[levelIndex];await select(scene,design,0,variant.resolution.id);
        assert.equal(await page.locator('#resolution').inputValue(),String(levelIndex));
        assert.equal(await page.locator('.variant-button[aria-pressed=true]').innerText(),design.label);
        assert.equal(await page.locator('#native-status').innerText(),'Native restore checked');
        const edgeMap=new Map(variant.analysis.edges.map(edge=>[edge.id,edge]));
        for(let index=0;index<variant.rungs.length;index++){
          await setRung(index);const rung=variant.rungs[index];
          const actual=await page.locator('#drawing .stroke:not(.dependency-overlay)').evaluateAll(nodes=>nodes.map(n=>({id:n.dataset.edge,d:n.getAttribute('d'),guide:n.classList.contains('guide')})));
          const expected=[...rung.guideEdgeIds.filter(id=>!rung.visibleEdgeIds.includes(id)).map(id=>({id,guide:true})),...rung.visibleEdgeIds.map(id=>({id,guide:false}))].map(row=>({...row,d:pathOf(edgeMap.get(row.id))}));
          assert.deepEqual(actual,expected,variant.id+' rung '+index+' uses exact saved native read');
          coordinatePathReads+=actual.length;rungReads++;
        }
        // The comparison panel keeps the same drawing and step at every level.
        await page.locator('#comparison-mode').selectOption('resolutions');
        const cards=await page.locator('.comparison-card').evaluateAll(nodes=>nodes.map(n=>({id:n.dataset.variant,paths:[...n.querySelectorAll('svg path')].map(p=>p.getAttribute('d'))})));
        assert.equal(cards.length,design.resolutions.length);
        for(let i=0;i<cards.length;i++){
          const read=design.resolutions[i],edges=new Map(read.analysis.edges.map(e=>[e.id,e]));
          assert.equal(cards[i].id,read.id);assert.deepEqual(cards[i].paths,read.rungs.at(-1).visibleEdgeIds.map(id=>pathOf(edges.get(id))));comparisonPathReads+=cards[i].paths.length;
        }
        const dependencies=variant.rungs.flatMap(r=>r.dependencies),final=variant.rungs.at(-1);
        const edgeId=final.visibleEdgeIds.find(id=>dependencies.some(d=>d.targetEdgeIds.includes(id)));
        assert.ok(edgeId,variant.id+' retains prior construction references');
        const hit=page.locator('#drawing .hit[data-edge="'+edgeId+'"]');await hit.focus();await hit.press('Enter');
        const expectedSites=[...new Set(dependencies.filter(d=>d.targetEdgeIds.includes(edgeId)).flatMap(d=>d.sourceSiteIds))].sort();
        assert.deepEqual(await page.locator('#drawing [data-reference-site]').evaluateAll(nodes=>nodes.map(n=>n.dataset.referenceSite).sort()),expectedSites);
        await page.keyboard.press('Escape');dependencySelections++;
        readChecks.push({id:variant.id,design:design.id,resolution:variant.resolution.id,rungs:variant.rungs.length,finalSpans:final.visibleEdgeIds.length});
      }
    }
    // Changing either dial preserves the other; switching drawings retains the
    // selected resolution. Browser navigation restores all four hash fields.
    const entry=designs.find(row=>row.design.id==='church-11')||designs[0],lastLevel=data.resolution.levels.at(-1);
    await select(entry.scene,entry.design,entry.design.resolutions[0].rungs.length-1,lastLevel.id);
    await page.locator('#resolution').evaluate(node=>{node.value='1';node.dispatchEvent(new Event('input',{bubbles:true}))});
    assert.equal(await page.locator('#rung').inputValue(),String(entry.design.resolutions[0].rungs.length-1));
    await setRung(0);assert.equal(await page.locator('#resolution').inputValue(),'1');
    await page.locator('#comparison-mode').selectOption('drawings');
    assert.equal(await page.locator('.comparison-card').count(),entry.scene.variants.length);
    if(entry.scene.variants.length>1){await page.locator('.comparison-card').nth(1).click();assert.equal(await page.locator('#resolution').inputValue(),'1');assert.equal(await page.locator('.variant-button[aria-pressed=true]').innerText(),entry.scene.variants[1].label)}
    await select(entry.scene,entry.design,entry.design.resolutions[0].rungs.length-1,lastLevel.id);
    const selected=entry.design.resolutions.at(-1),event=page.waitForEvent('download');await page.locator('#archive').click();const download=await event;
    assert.equal(download.suggestedFilename(),selected.id+'.gojo.json');assert.deepEqual(await fs.readFile(await download.path()),archiveFiles.get(selected.archivePath));
    await page.locator('#comparison-mode').selectOption('resolutions');
    const screenshots=[];
    for(const candidate of [entry,designs.find(row=>row.design.id==='church-03'),designs.find(row=>row.design.id==='alphabet-atlas')].filter(Boolean)){
      await select(candidate.scene,candidate.design,candidate.design.resolutions[0].rungs.length-1,lastLevel.id);
      await page.locator('#guide-mode').selectOption('hide');const name=candidate.design.id+'-coarse.png';
      await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(qa,name),fullPage:true});screenshots.push(name);
    }
    await page.setViewportSize({width:390,height:844});
    const mobile=await page.evaluate(()=>({scroll:document.documentElement.scrollWidth,client:document.documentElement.clientWidth}));
    assert.ok(mobile.scroll<=mobile.client);await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(qa,'resolution-mobile.png'),fullPage:true});screenshots.push('resolution-mobile.png');
    // Embedded geometry and the resolution controls also run without a server.
    await page.goto(pathToFileURL(path.join(review,'index.html')).href+'#'+[entry.scene.id,entry.design.id,0,lastLevel.id].join('/'));
    await page.waitForFunction(label=>document.querySelector('#resolution-label')?.textContent===label,lastLevel.label);
    assert.ok(await page.locator('#drawing .stroke').count());
    assert.deepEqual(errors,[]);assert.deepEqual(externalRequests,[]);
    const result={schema:'gojo.construction-resolution-browser-verification.v1',passed:true,localOnly:true,
      families:data.scenes.length,designs:designs.length,programs:programs.length,levels:data.resolution.levels.length,
      rungReads,coordinatePathReads,comparisonPathReads,dependencySelections,verifiedProgramDownloads:archiveFiles.size,
      savedNativeJsonVerified:true,fileOriginVerified:true,mobile,scriptErrors:errors,externalRequests,readChecks,packageHashes,screenshots};
    await fs.writeFile(path.join(qa,'browser-summary.json'),JSON.stringify(result,null,2)+'\n');
    console.log(JSON.stringify({...result,readChecks:undefined,packageHashes:undefined},null,2));
  }finally{await browser?.close();await new Promise(resolve=>server.close(resolve))}
})().catch(error=>{console.error(error);process.exitCode=1});
