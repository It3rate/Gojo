// Browser checks over the captured weave presentation. No model computations run here.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const http = require('node:http');
const {chromium} = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'playwright');
const root = path.resolve(__dirname, '../../../Visualizations/Gojo.Demos.Web');
const screenshots = process.env.GOJO_SCREENSHOTS || path.resolve(__dirname, '../../../artifacts/angular-weave/browser');
const mime = {'.html':'text/html','.css':'text/css','.js':'text/javascript','.json':'application/json','.svg':'image/svg+xml','.md':'text/plain'};
(async () => {
  const server = http.createServer(async (req,res) => {
    try {
      let file=path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
      if(file!==root && !file.startsWith(root+path.sep)) {res.writeHead(403);return res.end();}
      if((await fs.stat(file)).isDirectory())file=path.join(file,'index.html');
      res.writeHead(200,{'Content-Type':mime[path.extname(file)] || 'application/octet-stream'});
      res.end(await fs.readFile(file));
    } catch {res.writeHead(404);res.end('Not found');}
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base=(process.env.GOJO_BASE_URL || `http://127.0.0.1:${server.address().port}`).replace(/\/$/,'');
  let browser;
  try {
    await fs.mkdir(screenshots,{recursive:true});
    browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context=await browser.newContext({viewport:{width:1440,height:1050},reducedMotion:'reduce'});
    const page=await context.newPage(), errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    page.on('response',r=>{if(r.url().startsWith(base) && r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
    for(const suffix of ['', '/', '/index.html']) {
      await page.goto(base+'/ornaments/weave'+suffix);
      await page.waitForFunction(()=>document.documentElement.classList.contains('js'));
      assert.equal(await page.locator('.case-panel:visible').count(),1);
    }
    const data=await (await context.request.get(base+'/ornaments/weave/study-data.json')).json();
    assert.equal(await page.locator('.case-panel').count(),data.weave.cases.length);
    assert.equal(await page.locator('#continuation .inference-card').count(),data.continuations.length);
    assert.equal(await page.locator('.junction-card').count(),(data.junctions || []).length);
    const ids=await page.locator('[id]').evaluateAll(nodes=>nodes.map(n=>n.id));
    assert.equal(new Set(ids).size,ids.length,'duplicate IDs');
    const dependencies=await page.locator('a[href],script[src],link[href]').evaluateAll(nodes=>nodes.map(n=>n.href||n.src));
    for(const url of new Set(dependencies.filter(u=>u.startsWith(base)))) {
      const response=await context.request.get(url.split('#')[0]);
      assert.equal(response.status(),200,url);
    }
    for(const item of data.weave.cases) {
      await page.locator('#case-select').selectOption(item.id);
      const panel=page.locator(`.case-panel[data-case="${item.id}"]`);
      assert.equal(await panel.isVisible(),true,item.id);
      for(const view of ['strands','depth','ribbon']) {
        await page.locator(`[data-select-view="${view}"]`).click();
        assert.equal(await panel.locator('.view-svg:visible').count(),1);
        assert.equal(await panel.locator(`.view-svg[data-view="${view}"]`).isVisible(),true);
      }
      if(item.crossings.length) {
        assert.equal(await panel.locator('.cross-witness:visible').count(),1);
        const index=Math.min(1,item.crossings.length-1);
        const id=String(item.crossings[index].id);
        await panel.locator(`.view-svg:visible .cross-pick[data-cross="${id}"]`).click();
        assert.equal(await panel.locator('.cross-witness:visible').getAttribute('data-cross'),id);
        await panel.locator('.next-cross').click();
        await panel.locator('.previous-cross').click();
        assert.equal(await panel.locator('.cross-witness:visible').getAttribute('data-cross'),id);
      }
    }
    await page.locator('#case-select').selectOption(data.weave.cases[0].id);
    await page.locator('#case-select').focus();
    await page.keyboard.press('ArrowDown');
    assert.equal(await page.locator('#case-select').inputValue(),data.weave.cases[1].id);
    assert.equal(await page.evaluate(()=>document.activeElement.id),'case-select');
    await page.keyboard.press('ArrowLeft');
    assert.equal(await page.locator('#case-select').inputValue(),data.weave.cases[0].id);
    await page.locator('#show-labels').check();
    assert.equal(await page.locator('body').evaluate(el=>el.classList.contains('hide-labels')),false);
    await page.locator('#inspect').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'weave-desktop.png')});
    await page.locator('[data-select-view="strands"]').click();
    await page.screenshot({path:path.join(screenshots,'strands-desktop.png')});
    await page.locator('[data-select-view="depth"]').click();
    await page.screenshot({path:path.join(screenshots,'depth-desktop.png')});
    await page.locator('.case-panel:visible .detail-grid').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'crossing-desktop.png')});
    await page.locator('#continuation').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'continuation-desktop.png')});
    await page.locator('#junctions').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'junctions-desktop.png')});
    const assertNoOverflow=async()=>{
      const dimensions=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));
      assert.ok(dimensions.scroll<=dimensions.width+1,JSON.stringify(dimensions));
    };
    await assertNoOverflow();
    await page.setViewportSize({width:390,height:844});
    await page.locator('[data-select-view="ribbon"]').click();
    await page.locator('#inspect').scrollIntoViewIfNeeded();
    await assertNoOverflow();
    await page.screenshot({path:path.join(screenshots,'weave-phone.png')});
    await page.locator('.case-panel:visible .cross-detail').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'crossing-phone.png')});
    await page.locator('#continuation').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'continuation-phone.png')});
    await page.locator('a[data-pick-case]').last().click();
    assert.equal(await page.locator('#case-select').inputValue(),data.weave.cases.at(-1).id);
    await page.reload();
    assert.equal(await page.locator('#case-select').inputValue(),data.weave.cases.at(-1).id);
    const nojs=await browser.newContext({javaScriptEnabled:false,viewport:{width:390,height:844}});
    const fallback=await nojs.newPage();
    await fallback.goto(base+'/ornaments/weave/');
    assert.equal(await fallback.locator('.case-panel:visible').count(),data.weave.cases.length);
    assert.equal(await fallback.locator('#continuation .inference-card:visible').count(),data.continuations.length);
    assert.equal(await fallback.locator('#notes').isVisible(),true);
    await nojs.close();
    assert.deepEqual(errors,[]);
    console.log(JSON.stringify({cases:data.weave.cases.length,continuations:data.continuations.length,modes:3,keyboard:true,mobile:true,noJavaScript:true,screenshots}));
  } finally {await browser?.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
