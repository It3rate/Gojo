// Verify the portable construction milestone locally. This never opens a
// deployed website: the package is served only from an ephemeral loopback URL.
const {chromium}=require(process.env.GOJO_PLAYWRIGHT_MODULE||'playwright');
const fs=require('node:fs/promises'),path=require('node:path'),http=require('node:http');
const assert=require('node:assert/strict'),crypto=require('node:crypto'),zlib=require('node:zlib');

const review=path.resolve(process.env.GOJO_CONSTRUCTION_REVIEW||'D:/_quickDraw/gojo-construction-series/review');
const qa=path.resolve(process.env.GOJO_CONSTRUCTION_QA||path.join(review,'..','qa'));
const types={'.html':'text/html','.json':'application/json','.gz':'application/gzip','.svg':'image/svg+xml'};
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const pathOf=edge=>'M'+edge.controls[0].join(' ')+({2:'L',3:'Q',4:'C'}[edge.controls.length])+edge.controls.slice(1).map(p=>p.join(' ')).join(' ');

function readEnvelope(html){
  const match=html.match(/<script id="construction-data" type="application\/json">([\s\S]*?)<\/script>/);
  assert.ok(match,'Construction data is embedded');
  const envelope=JSON.parse(match[1]);
  return envelope.encoding==='gzip-base64'?JSON.parse(zlib.gunzipSync(Buffer.from(envelope.payload,'base64'))):envelope;
}

(async()=>{
  const server=http.createServer(async(req,res)=>{
    try{
      const pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);
      let file=path.resolve(review,'.'+pathname);
      if(file!==review&&!file.startsWith(review+path.sep)){res.writeHead(403);return res.end();}
      if((await fs.stat(file)).isDirectory())file=path.join(file,'index.html');
      const bytes=await fs.readFile(file);
      res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Content-Length':bytes.length});
      res.end(bytes);
    }catch{res.writeHead(404);res.end('Not found');}
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base='http://127.0.0.1:'+server.address().port;
  let browser;
  try{
    await fs.mkdir(qa,{recursive:true});
    const html=await fs.readFile(path.join(review,'index.html'),'utf8');
    const data=readEnvelope(html),manifest=JSON.parse(await fs.readFile(path.join(review,'public-files.json'),'utf8'));
    assert.equal(data.public,true,'Portable package enables saved executable JSON downloads');
    const programs=data.scenes.flatMap(scene=>scene.variants.map(variant=>({scene,variant})));
    const totalRungs=programs.reduce((sum,{variant})=>sum+variant.rungs.length,0);
    assert.equal(data.scenes.length,7);assert.equal(programs.length,53);assert.equal(totalRungs,245);
    const churches=programs.filter(({scene})=>scene.id==='church-series');
    const letters=programs.filter(({scene})=>scene.id==='alphabet-series');
    const atlas=programs.filter(({scene})=>scene.id==='alphabet-atlas');
    const finalCount=entries=>entries.reduce((sum,{variant})=>sum+variant.rungs.at(-1).visibleEdgeIds.length,0);
    assert.equal(churches.length,14);assert.equal(finalCount(churches),383);
    assert.equal(letters.length,26);assert.equal(finalCount(letters),100);
    assert.equal(atlas.length,1);assert.equal(finalCount(atlas),100);
    assert.equal(new Set(programs.map(({variant})=>variant.id)).size,53,'Every program has a distinct identity');

    browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context=await browser.newContext({viewport:{width:1480,height:1100},reducedMotion:'reduce'});
    const externalRequests=[],errors=[];
    await context.route('**/*',async route=>{
      const url=route.request().url();
      if(url.startsWith(base+'/')||url.startsWith('blob:')||url.startsWith('data:'))return route.continue();
      externalRequests.push(url);await route.abort();
    });
    const page=await context.newPage();
    page.on('pageerror',error=>errors.push(error.message));
    page.on('response',response=>{if(response.status()>=400)errors.push(response.status()+' '+response.url());});
    const archiveFiles=new Map(),packageHashes=[];
    for(const relative of manifest.files){
      assert.ok(!path.isAbsolute(relative)&&!relative.split(/[\\/]/).includes('..'),'Package path stays inside review');
      const response=await context.request.get(base+'/'+relative);
      assert.equal(response.status(),200,relative);
      const stored=await fs.readFile(path.join(review,relative)),received=await response.body();
      assert.deepEqual(received,stored,relative+' exact served bytes');
      packageHashes.push({path:relative,bytes:stored.length,sha256:hash(stored)});
      if(relative.endsWith('.gojo.json.gz')){
        const decoded=zlib.gunzipSync(received);JSON.parse(decoded.toString('utf8'));
        archiveFiles.set(relative,{stored,decoded});
      }
    }
    assert.equal(archiveFiles.size,53,'Every selected native program is retained');
    for(const {variant} of programs){
      assert.equal(variant.archiveKind,'program');assert.ok(archiveFiles.has(variant.archivePath),variant.id+' native program download');
      assert.equal(variant.nativeRestoreVerified,true);assert.equal(variant.nativeReplayVerified,true);
    }

    async function select(scene,variant,index=0){
      await page.goto(base+'/#'+scene.id+'/'+variant.id+'/'+index);
      await page.waitForFunction(expected=>{
        const parts=location.hash.slice(1).split('/').map(decodeURIComponent);
        return parts[0]===expected.scene&&parts[1]===expected.variant&&
          document.querySelector('.variant-button[aria-pressed="true"]')?.textContent===expected.label&&
          document.querySelector('#step-count')?.textContent===`${expected.index+1} / ${expected.rungs}`;
      },{scene:scene.id,variant:variant.id,label:variant.label,index,rungs:variant.rungs.length});
    }
    async function setRung(index){
      await page.locator('#rung').evaluate((node,value)=>{node.value=String(value);node.dispatchEvent(new Event('input',{bubbles:true}));},index);
    }
    await select(programs[0].scene,programs[0].variant);
    assert.equal(await page.locator('.scene-button').count(),7);
    let rungReads=0,dependencySelections=0,guideModeChecks=0,coordinatePathReads=0;
    const readChecks=[];
    for(const {scene,variant} of programs){
      await select(scene,variant);
      await page.locator('#guide-mode').selectOption('fade');
      assert.equal(await page.locator('.rung-button').count(),variant.rungs.length,variant.id);
      assert.equal(await page.locator('#native-status').innerText(),'Native restore checked');
      assert.equal(await page.locator('#previous').isDisabled(),true);
      const edgeMap=new Map(variant.analysis.edges.map(edge=>[edge.id,edge]));
      for(let index=0;index<variant.rungs.length;index++){
        await setRung(index);
        const rung=variant.rungs[index];
        assert.equal(await page.locator('#step-count').innerText(),`${index+1} / ${variant.rungs.length}`);
        const actual=await page.locator('#drawing .stroke:not(.dependency-overlay)').evaluateAll(nodes=>nodes.map(node=>({
          id:node.dataset.edge,d:node.getAttribute('d'),guide:node.classList.contains('guide'),introduced:node.classList.contains('introduced')
        })));
        const expected=[...rung.guideEdgeIds.filter(id=>!rung.visibleEdgeIds.includes(id)).map(id=>({id,guide:true,introduced:false})),
          ...rung.visibleEdgeIds.map(id=>({id,guide:false,introduced:rung.introducedEdgeIds.includes(id)}))]
          .map(row=>({...row,d:pathOf(edgeMap.get(row.id))}));
        assert.deepEqual(actual,expected,variant.id+' step '+index+' rendered controls and memberships');
        coordinatePathReads+=actual.length;
        assert.equal(await page.locator('#drawing .hit').count(),expected.length,variant.id+' inspectable spans');
        assert.equal(await page.locator('.constraint.failed').count(),rung.constraints.filter(c=>c.status==='failed').length);
        assert.equal(await page.locator('.constraint.open').count(),rung.constraints.filter(c=>c.status==='open').length);
        rungReads++;
      }
      assert.equal(await page.locator('#next').isDisabled(),true);
      const final=variant.rungs.at(-1),allDependencies=variant.rungs.flatMap(rung=>rung.dependencies);
      const referenced=new Set(allDependencies.flatMap(dep=>dep.targetEdgeIds));
      const candidates=final.visibleEdgeIds.map(id=>edgeMap.get(id)).filter(edge=>referenced.has(edge.id));
      const candidate=candidates.find(edge=>edge.controls.length===2)||candidates[0];
      assert.ok(candidate,variant.id+' exposes a retained construction reference');
      await page.locator('#guide-mode').selectOption('hide');
      assert.equal(await page.locator('#drawing .guide.stroke').count(),0);
      const hit=page.locator('#drawing .hit[data-edge="'+candidate.id+'"]');
      const position=await hit.evaluate(node=>{
        const point=node.getPointAtLength(node.getTotalLength()/2).matrixTransform(node.getScreenCTM());
        return{x:point.x,y:point.y};
      });
      await page.mouse.move(position.x,position.y);
      // Keyboard focus selects the exact span when several hit surfaces overlap.
      await hit.focus();
      assert.equal(await page.locator('#tooltip').isVisible(),true,variant.id+' reference hover/focus');
      await hit.press('Enter');await page.mouse.move(20,20);
      assert.ok((await page.locator('#selection').innerText()).includes(candidate.id));
      const expectedReferences=[...new Set(allDependencies.filter(dep=>dep.targetEdgeIds.includes(candidate.id)).flatMap(dep=>dep.sourceSiteIds))].sort();
      const actualReferences=await page.locator('#drawing [data-reference-site]').evaluateAll(nodes=>nodes.map(node=>node.dataset.referenceSite).sort());
      assert.deepEqual(actualReferences,expectedReferences,variant.id+' hidden retained guide sites remain inspectable');
      assert.ok(actualReferences.length>0);dependencySelections++;
      await page.keyboard.press('Escape');
      assert.equal(await page.locator('#drawing [data-reference-site]').count(),0);
      await page.locator('#guide-mode').selectOption('retain');
      assert.equal(await page.locator('#drawing .guide.stroke').count(),final.guideEdgeIds.length);
      await page.locator('#guide-opacity').evaluate(node=>{node.value='25';node.dispatchEvent(new Event('input',{bubbles:true}));});
      assert.ok((await page.locator('#drawing .guide.stroke').evaluateAll(nodes=>nodes.map(node=>Number(node.getAttribute('opacity'))))).every(value=>value===0.25));
      await page.locator('#guide-mode').selectOption('fade');
      assert.ok((await page.locator('#drawing .guide.stroke').evaluateAll(nodes=>nodes.map(node=>Number(node.getAttribute('opacity'))))).every(value=>value>0&&value<=0.25));
      await page.locator('#show-sites').check();
      assert.equal(await page.locator('#drawing .site:not(.dependency-overlay)').count(),new Set(final.dependencies.flatMap(dep=>dep.sourceSiteIds)).size);
      await page.locator('#show-sites').uncheck();
      guideModeChecks++;
      readChecks.push({id:variant.id,family:scene.id,rungs:variant.rungs.length,finalSpans:final.visibleEdgeIds.length,
        failedConditions:final.constraints.filter(c=>c.status==='failed').length});
    }

    const church=churches.find(({variant})=>variant.id==='church-01')||churches[0],alphabet=atlas[0],screenshots=[];
    for(const [label,entry] of [['church',church],['alphabet-atlas',alphabet]]){
      for(const [stage,index] of [['rough',0],['finished',entry.variant.rungs.length-1]]){
        await select(entry.scene,entry.variant,index);
        await page.locator('#guide-mode').selectOption(stage==='rough'?'retain':'fade');
        const filename=label+'-'+stage+'.png';
        await page.screenshot({path:path.join(qa,filename),fullPage:true});screenshots.push(filename);
      }
    }
    await select(church.scene,church.variant);
    await page.locator('#next').click();assert.equal(await page.locator('#rung').inputValue(),'1');
    await page.locator('#previous').click();assert.equal(await page.locator('#rung').inputValue(),'0');
    await page.locator('.rung-button').nth(2).click();assert.equal(await page.locator('#rung').inputValue(),'2');
    await setRung(0);await page.locator('#play').click();
    await page.waitForFunction(()=>document.querySelector('#rung').value==='1');
    await page.locator('#play').click();assert.equal(await page.locator('#play').innerText(),'Play');
    await page.locator('#show-comparison').uncheck();assert.equal(await page.locator('#comparison').isHidden(),true);
    await page.locator('#show-comparison').check();assert.equal(await page.locator('#comparison').isVisible(),true);
    await page.locator('.comparison-card').nth(1).click();
    assert.equal(await page.locator('.variant-button[aria-pressed="true"]').innerText(),church.scene.variants[1].label);
    await page.locator('.variant-button').nth(0).click();
    assert.equal(await page.locator('.variant-button[aria-pressed="true"]').innerText(),church.scene.variants[0].label);
    const selected=church.scene.variants[0],downloadEvent=page.waitForEvent('download');
    await page.locator('#archive').click();const download=await downloadEvent;
    assert.equal(download.suggestedFilename(),selected.id+'.gojo.json');
    assert.deepEqual(await fs.readFile(await download.path()),archiveFiles.get(selected.archivePath).decoded,'Saved native JSON matches retained executable program');

    await select(alphabet.scene,alphabet.variant,alphabet.variant.rungs.length-1);
    await page.setViewportSize({width:390,height:844});
    const sizing=await page.evaluate(()=>({scroll:document.documentElement.scrollWidth,client:document.documentElement.clientWidth}));
    assert.ok(sizing.scroll<=sizing.client,'Phone page overflows its width');
    await page.screenshot({path:path.join(qa,'construction-mobile.png'),fullPage:true});screenshots.push('construction-mobile.png');
    assert.equal((await context.request.get(base+'/archives/missing-program.json.gz')).status(),404);
    assert.deepEqual(errors,[]);assert.deepEqual(externalRequests,[],'Review must not depend on the deployed website or external assets');
    const result={schema:'gojo.construction-series-browser-verification.v1',passed:true,localOnly:true,
      families:data.scenes.length,programs:programs.length,rungReads,coordinatePathReads,dependencySelections,guideModeChecks,
      sourceCoherence:{churches:churches.length,churchFinalEdges:finalCount(churches),letters:letters.length,
        individualLetterFinalEdges:finalCount(letters),alphabetAtlasFinalEdges:finalCount(atlas)},
      verifiedProgramDownloads:archiveFiles.size,savedNativeJsonVerified:true,mobile:sizing,scriptErrors:errors,
      externalRequests,readChecks,packageHashes,screenshots};
    await fs.writeFile(path.join(qa,'browser-summary.json'),JSON.stringify(result,null,2)+'\n');
    console.log(JSON.stringify({passed:true,families:result.families,programs:result.programs,rungReads,coordinatePathReads,
      dependencySelections,guideModeChecks,verifiedProgramDownloads:archiveFiles.size,summary:path.join(qa,'browser-summary.json')},null,2));
  }finally{await browser?.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
