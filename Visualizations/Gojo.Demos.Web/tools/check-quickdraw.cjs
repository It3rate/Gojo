// Browser and download checks for the published Quick Draw category.
// Set GOJO_BASE_URL for production checks; otherwise serve the tracked site.
const {chromium}=require(process.env.GOJO_PLAYWRIGHT_MODULE||'playwright');
const fs=require('node:fs/promises'),path=require('node:path'),http=require('node:http');
const assert=require('node:assert/strict'),crypto=require('node:crypto'),zlib=require('node:zlib');
const web=path.resolve(process.env.GOJO_WEB_ROOT||path.join(__dirname,'..'));
const screenshotDir=process.env.GOJO_SCREENSHOTS||path.join(require('node:os').tmpdir(),'GojoQuickDrawQA');
const types={'.html':'text/html','.css':'text/css','.js':'text/javascript','.json':'application/json','.svg':'image/svg+xml','.gz':'application/gzip'};
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
    browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context=await browser.newContext({viewport:{width:1440,height:1100},reducedMotion:'reduce'});
    const page=await context.newPage(),errors=[];
    page.on('pageerror',error=>errors.push(error.message));
    page.on('response',response=>{if(response.status()>=400&&response.url().startsWith(base))errors.push(response.status()+' '+response.url());});
    const catalogue=JSON.parse(await fs.readFile(path.join(web,'quickdraw/catalogue.json'),'utf8'));
    await page.goto(base+'/');
    for(const route of ['/quickdraw/','/ornaments/','/minds/','/syntax/','/core4/'])
      assert.ok(await page.locator('a[href="'+route+'"]').count()>=1,'Homepage route '+route);
    for(const suffix of ['','/','/index.html']){
      await page.goto(base+'/quickdraw'+suffix);
      assert.equal(await page.locator('h1').textContent(),'Quick Draw');
      for(const study of catalogue.studies)assert.equal(await page.locator('a[href="'+study.route+'"]').count(),1);
    }
    await page.screenshot({path:path.join(screenshotDir,'category-desktop.png'),fullPage:true});
    const downloads=[];
    for(const study of catalogue.studies){
      const manifest=JSON.parse(await fs.readFile(path.join(web,study.route,'publication.json'),'utf8'));
      for(const file of manifest.files){
        const response=await context.request.get(base+study.route+file.path);
        assert.equal(response.status(),200,file.path);
        const bytes=await response.body();
        const compressed=file.path.endsWith('.json.gz'),decoded=compressed&&response.headers()['content-encoding']==='gzip'&&!(bytes[0]===31&&bytes[1]===139);
        if(decoded){const stored=await fs.readFile(path.join(web,study.route,file.path));assert.equal(crypto.createHash('sha256').update(stored).digest('hex'),file.publishedSha256,file.path);assert.deepEqual(bytes,zlib.gunzipSync(stored),file.path+' decoded HTTP body');}
        else assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),file.publishedSha256,file.path);
        if(compressed){JSON.parse((decoded?bytes:zlib.gunzipSync(bytes)).toString('utf8'));downloads.push(file.path);}
      }
    }
    const route='/quickdraw/line-art';
    for(const suffix of ['','/','/index.html']){
      await page.goto(base+route+suffix+'#diagnostic-alphabet');
      assert.match(await page.locator('#scene-title').textContent(),/alphabet/i);
      assert.deepEqual(await page.locator('#object-analysis tbody tr').evaluateAll(rows=>rows.map(row=>row.dataset.letter)),[...'ABCDEFGHIJKLMNOPQRSTUVWXYZ']);
      assert.equal(await page.locator('[data-scene="diagnostic-rope-branch"]').count(),0);
      const links=await page.locator('a[download]').evaluateAll(elements=>elements.map(element=>element.href));
      assert.ok(links.length>0,'Native download link');
      for(const url of links)assert.ok(new URL(url).pathname.startsWith(route+'/archives/'),url);
    }
    const scenes=await page.locator('.scene-button').evaluateAll(buttons=>buttons.map(button=>button.dataset.scene));
    for(const id of scenes){
      await page.locator('[data-scene="'+id+'"]').click();
      const counts=await page.evaluate(()=>({expected:active.analysis.edges.length,actual:document.querySelectorAll('.drawing-edge').length}));
      assert.equal(counts.actual,counts.expected,id);
    }
    await page.locator('[data-scene="diagnostic-alphabet"]').click();
    await page.locator('[data-letter="A"] button').click();
    assert.match(await page.locator('#selection').textContent(),/Authored capital letter/);
    assert.ok(await page.locator('.drawing-edge.highlight').count()>0);
    await page.keyboard.press('Escape');
    assert.equal(await page.locator('.drawing-edge.highlight').count(),0);
    await page.locator('.joint').first().hover();assert.equal(await page.locator('#tooltip').getAttribute('hidden'),null);
    await page.mouse.move(0,0);await page.locator('.hit-edge').first().focus();
    assert.equal(await page.locator('#tooltip').getAttribute('hidden'),null);
    await page.keyboard.press('Escape');
    for(const id of ['joints','regions','relations','objects','endpoints']){
      await page.locator('#'+id).check();await page.locator('#'+id).uncheck();
    }
    await page.locator('#joints').check();await page.locator('#relations').check();
    await page.locator('#coverage > details > summary').click();
    const downloadEvent=page.waitForEvent('download');await page.locator('a[download]').click();const download=await downloadEvent;
    assert.equal(download.suggestedFilename(),'diagnostic-alphabet.gojo.json');
    assert.deepEqual(await fs.readFile(await download.path()),zlib.gunzipSync(await fs.readFile(path.join(web,route,'archives/diagnostic-alphabet.gojo.json.gz'))),'Native JSON download bytes');
    await page.screenshot({path:path.join(screenshotDir,'alphabet-desktop.png'),fullPage:true});
    await page.setViewportSize({width:390,height:844});
    assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth),390,'Phone page overflows');
    assert.equal(await page.locator('#object-analysis tbody tr').count(),26);
    await page.screenshot({path:path.join(screenshotDir,'alphabet-mobile.png'),fullPage:true});
    assert.equal((await context.request.get(base+'/quickdraw/missing-study/absent.json.gz')).status(),404,'Missing study asset falls back to homepage');
    assert.deepEqual(errors,[]);
    console.log(JSON.stringify({base,studies:catalogue.studies.length,scenes:scenes.length,letters:26,verifiedDownloads:downloads.length,savedNativeJsonVerified:true,screenshots:screenshotDir},null,2));
  }finally{await browser?.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
