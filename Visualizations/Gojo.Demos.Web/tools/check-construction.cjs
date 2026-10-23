// Browser, retained-reference, and download checks for the construction study.
// GOJO_BASE_URL selects a deployed site; GOJO_WEB_ROOT selects expected assets.
const {chromium}=require(process.env.GOJO_PLAYWRIGHT_MODULE||'playwright');
const fs=require('node:fs/promises'),path=require('node:path'),http=require('node:http');
const assert=require('node:assert/strict'),crypto=require('node:crypto'),zlib=require('node:zlib');
const web=path.resolve(process.env.GOJO_WEB_ROOT||path.join(__dirname,'..'));
const screenshotDir=process.env.GOJO_SCREENSHOTS||path.join(require('node:os').tmpdir(),'GojoConstructionQA');
const route='/quickdraw/construction/';
const types={'.html':'text/html','.css':'text/css','.js':'text/javascript','.json':'application/json','.svg':'image/svg+xml','.gz':'application/gzip'};

function readEnvelope(html){
  const match=html.match(/<script id="construction-data" type="application\/json">([\s\S]*?)<\/script>/);
  assert.ok(match,'Construction data is embedded');
  const envelope=JSON.parse(match[1]);
  return envelope.encoding==='gzip-base64'?JSON.parse(zlib.gunzipSync(Buffer.from(envelope.payload,'base64'))):envelope;
}

(async()=>{
  const server=http.createServer(async(req,res)=>{
    try{
      let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
      if(file!==web&&!file.startsWith(web+path.sep)){res.writeHead(403);return res.end();}
      if((await fs.stat(file)).isDirectory())file=path.join(file,'index.html');
      const bytes=await fs.readFile(file);res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream'});res.end(bytes);
    }catch{res.writeHead(404);res.end('Not found');}
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base=(process.env.GOJO_BASE_URL||'http://127.0.0.1:'+server.address().port).replace(/\/$/,'');
  let browser;
  try{
    await fs.mkdir(screenshotDir,{recursive:true});
    const data=readEnvelope(await fs.readFile(path.join(web,route,'index.html'),'utf8'));
    const manifest=JSON.parse(await fs.readFile(path.join(web,route,'publication.json'),'utf8'));
    assert.equal(data.public,true);
    browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context=await browser.newContext({viewport:{width:1480,height:1100},reducedMotion:'reduce'});
    const page=await context.newPage(),errors=[],readChecks=[],downloads=[];
    page.on('pageerror',error=>errors.push(error.message));
    page.on('response',response=>{if(response.status()>=400&&response.url().startsWith(base))errors.push(response.status()+' '+response.url());});
    for(const file of manifest.files){
      const response=await context.request.get(base+route+file.path);
      assert.equal(response.status(),200,file.path);
      const bytes=await response.body(),stored=await fs.readFile(path.join(web,route,file.path));
      const compressed=file.path.endsWith('.json.gz');
      const decoded=compressed&&response.headers()['content-encoding']==='gzip'&&!(bytes[0]===31&&bytes[1]===139);
      assert.equal(crypto.createHash('sha256').update(stored).digest('hex'),file.publishedSha256,file.path+' expected bytes');
      if(decoded)assert.deepEqual(bytes,zlib.gunzipSync(stored),file.path+' decoded HTTP body');
      else assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),file.publishedSha256,file.path+' published bytes');
      if(compressed){JSON.parse((decoded?bytes:zlib.gunzipSync(bytes)).toString('utf8'));downloads.push(file.path);}
    }
    for(const suffix of ['','/','/index.html']){
      await page.goto(base+route.slice(0,-1)+suffix);
      await page.waitForFunction(()=>document.querySelectorAll('.scene-button').length>0);
      assert.equal(await page.locator('.scene-button').count(),data.scenes.length);
      assert.equal(await page.locator('header a[href="/quickdraw/"]').count(),1);
      const href=await page.locator('#archive').evaluate(link=>link.href);
      assert.ok(href&&new URL(href).pathname.startsWith(route+'archives/'),'Portable archive link on '+suffix);
    }
    let rungReads=0,dependencySelections=0;
    for(const scene of data.scenes)for(const variant of scene.variants){
      await page.goto(base+route+'#'+scene.id+'/'+variant.id+'/0');
      await page.waitForFunction(label=>document.querySelector('.variant-button[aria-pressed="true"]')?.textContent===label,variant.label);
      assert.equal(await page.locator('.rung-button').count(),variant.rungs.length,variant.id);
      assert.equal(await page.locator('#native-status').innerText(),'Native restore checked');
      assert.equal(variant.nativeRestoreVerified,true);assert.equal(variant.nativeReplayVerified,true);
      for(let i=0;i<variant.rungs.length;i++){
        const rung=variant.rungs[i];
        await page.locator('#rung').evaluate((node,value)=>{node.value=String(value);node.dispatchEvent(new Event('input',{bubbles:true}));},i);
        assert.equal(await page.locator('#step-count').innerText(),`${i+1} / ${variant.rungs.length}`);
        assert.equal(await page.locator('.stroke:not(.guide):not(.dependency-overlay)').count(),rung.visibleEdgeIds.length,variant.id+' visible spans');
        assert.equal(await page.locator('.stroke.introduced').count(),rung.visibleEdgeIds.filter(id=>rung.introducedEdgeIds.includes(id)).length,variant.id+' introduced spans');
        assert.equal(await page.locator('.constraint.failed').count(),rung.constraints.filter(c=>c.status==='failed').length,variant.id+' failed reads');
        rungReads++;
      }
      const final=variant.rungs.at(-1),edgeMap=new Map(variant.analysis.edges.map(edge=>[edge.id,edge]));
      const referenced=new Set(variant.rungs.flatMap(r=>r.dependencies.flatMap(d=>d.targetEdgeIds)));
      const candidates=final.visibleEdgeIds.map(id=>edgeMap.get(id)).filter(edge=>referenced.has(edge.id));
      const candidate=candidates.find(edge=>edge.controls.length===2)||candidates[0];
      if(candidate){
        const locator=page.locator('.hit[data-edge="'+candidate.id+'"]');
        const point=await locator.evaluate(node=>{const p=node.getPointAtLength(node.getTotalLength()/2).matrixTransform(node.getScreenCTM());return{x:p.x,y:p.y};});
        await page.mouse.move(point.x,point.y);await page.mouse.click(point.x,point.y);
        // Exact keyboard selection remains available where projected spans overlap.
        if(!(await page.locator('#selection').innerText()).includes(candidate.id)){await locator.focus();await locator.press('Enter');}
        assert.ok((await page.locator('#selection').innerText()).includes(candidate.id));
        assert.ok(await page.locator('[data-reference-site]').count()>0,variant.id+' retained sites');
        dependencySelections++;
      }
      await page.keyboard.press('Escape');await page.mouse.move(20,20);
      await page.locator('#guide-mode').selectOption('hide');assert.equal(await page.locator('.guide.stroke').count(),0);
      await page.locator('#guide-mode').selectOption('retain');
      await page.locator('#guide-mode').selectOption('fade');
      await page.screenshot({path:path.join(screenshotDir,variant.id+'.png'),fullPage:true});
      readChecks.push({id:variant.id,rungs:variant.rungs.length,visibleSpans:final.visibleEdgeIds.length,failedConditions:final.constraints.filter(c=>c.status==='failed').length});
      if(variant.archivePath){
        assert.equal(variant.archiveKind,'program');
        assert.ok(downloads.includes(variant.archivePath),'Published executable program '+variant.id);
      }
    }
    const first=data.scenes[0],initial=first.variants[0];
    await page.goto(base+route+'#'+first.id+'/'+initial.id+'/0');
    await page.waitForFunction(label=>document.querySelector('.variant-button[aria-pressed="true"]')?.textContent===label,initial.label);
    await page.screenshot({path:path.join(screenshotDir,'construction-rough.png'),fullPage:true});
    if(initial.rungs.length>1){
      await page.locator('#play').click();await page.waitForTimeout(1650);await page.locator('#play').click();
      assert.equal(await page.locator('#step-count').innerText(),`2 / ${initial.rungs.length}`,'Playback advances one rung');
    }
    await page.locator('#show-comparison').uncheck();assert.ok(await page.locator('#comparison').isHidden());
    await page.locator('#show-comparison').check();
    if(first.variants.length>1){
      assert.ok(await page.locator('#comparison').isVisible());
      await page.locator('.comparison-card').nth(1).click();
      assert.equal(await page.locator('.variant-button[aria-pressed="true"]').innerText(),first.variants[1].label);
    }
    await page.setViewportSize({width:390,height:844});
    const sizing=await page.evaluate(()=>({scroll:document.documentElement.scrollWidth,client:document.documentElement.clientWidth}));
    assert.ok(sizing.scroll<=sizing.client,'Phone page overflows');
    await page.screenshot({path:path.join(screenshotDir,'construction-mobile.png'),fullPage:true});
    const selected=first.variants[Math.min(1,first.variants.length-1)];
    if(selected.archivePath){
      const downloadEvent=page.waitForEvent('download');await page.locator('#archive').click();const download=await downloadEvent;
      assert.equal(download.suggestedFilename(),selected.id+'.gojo.json');
      assert.deepEqual(await fs.readFile(await download.path()),zlib.gunzipSync(await fs.readFile(path.join(web,route,selected.archivePath))),'Saved executable program bytes');
    }
    assert.equal((await context.request.get(base+route+'archives/missing-program.json.gz')).status(),404,'Missing archive must not fall back to a webpage');
    assert.deepEqual(errors,[]);
    const result={base,passed:true,families:data.scenes.length,variants:readChecks.length,rungReads,dependencySelections,verifiedProgramDownloads:downloads.length,savedNativeJsonVerified:true,mobile:sizing,scriptErrors:errors,readChecks,screenshots:screenshotDir};
    await fs.writeFile(path.join(screenshotDir,'browser-qa.json'),JSON.stringify(result,null,2)+'\n');
    console.log(JSON.stringify(result,null,2));
  }finally{await browser?.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
