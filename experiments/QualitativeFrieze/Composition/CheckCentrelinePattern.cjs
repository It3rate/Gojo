// Presentation checks only; native model laws are exercised by the C# tests.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const http = require('node:http');
const {chromium} = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'playwright');
const root = path.resolve(process.env.GOJO_WEB_ROOT || path.join(__dirname, '../../../Visualizations/Gojo.Demos.Web'));
const screenshots = process.env.GOJO_SCREENSHOTS || path.resolve(__dirname, '../../../artifacts/centreline-patterns/browser');
const mime = {'.html':'text/html','.css':'text/css','.js':'text/javascript','.json':'application/json','.svg':'image/svg+xml','.md':'text/plain','.png':'image/png'};
(async () => {
  const server = http.createServer(async (req,res) => {
    try {
      let file = path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
      if (file !== root && !file.startsWith(root+path.sep)) {res.writeHead(403);return res.end();}
      if ((await fs.stat(file)).isDirectory()) file=path.join(file,'index.html');
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
    for (const suffix of ['', '/', '/index.html']) {
      await page.goto(base+'/ornaments/centreline'+suffix);
      await page.waitForFunction(()=>document.documentElement.classList.contains('js'));
      assert.equal(await page.locator('.pattern-panel:not(.variant-panel):visible').count(),1);
    }
    const report=await (await context.request.get(base+'/ornaments/centreline/study-data.json')).json();
    const editorial=await (await context.request.get(base+'/ornaments/centreline/editorial-notes.json')).json();
    assert.equal(report.patterns.length,18);
    assert.equal(editorial.excluded.length,10);
    assert.equal(await page.locator('.pattern-card').count(),18);
    assert.equal(await page.locator('.variant-panel').count(),report.variants.length);
    const ids=await page.locator('[id]').evaluateAll(nodes=>nodes.map(n=>n.id));
    assert.equal(new Set(ids).size,ids.length,'duplicate HTML or SVG IDs');
    const dependencies=await page.locator('a[href],script[src],link[href],img[src]').evaluateAll(nodes=>nodes.map(n=>n.href||n.src));
    for (const url of new Set(dependencies.filter(u=>u.startsWith(base)))) {
      const response=await context.request.get(url.split('#')[0]);
      assert.equal(response.status(),200,url);
    }
    for (const pattern of report.patterns) {
      await page.locator('#pattern-select').selectOption(pattern.id);
      const panel=page.locator(`.pattern-panel[data-pattern="${pattern.id}"]`);
      assert.equal(await panel.isVisible(),true,pattern.id);
      assert.equal(await panel.locator('.diagram .path-group').count(),pattern.paths.length);
      for (const group of pattern.groups.filter(g=>g.id!=='all')) {
        await panel.locator(`[data-group="${group.id}"]`).click();
        const selected=await panel.locator('.diagram .path-group:not(.dimmed)').evaluateAll(nodes=>nodes.map(n=>Number(n.dataset.path)).sort((a,b)=>a-b));
        assert.deepEqual(selected,[...group.pathIds].sort((a,b)=>a-b));
      }
      await panel.locator('.group-button[data-paths="*"]').click();
      assert.equal(await panel.locator('.diagram .dimmed').count(),0);
      if(pattern.paths.length) {
        const pathId=pattern.paths[0].id;
        await panel.locator(`.path-key[data-pick-path="${pathId}"]`).click();
        assert.equal(await panel.locator('.diagram .path-group:not(.dimmed)').count(),1);
        await panel.locator('.group-button[data-paths="*"]').click();
      }
    }
    await page.locator('#pattern-select').selectOption(report.patterns[0].id);
    await page.locator('#pattern-select').focus();
    await page.keyboard.press('ArrowDown');
    assert.equal(await page.locator('#pattern-select').inputValue(),report.patterns[1].id);
    assert.equal(await page.evaluate(()=>document.activeElement.id),'pattern-select');
    await page.keyboard.press('ArrowLeft');
    assert.equal(await page.locator('#pattern-select').inputValue(),report.patterns[0].id);
    assert.equal(await page.evaluate(()=>document.activeElement.id),'pattern-select');
    await page.locator('.pattern-card[data-select-pattern="CR4"]').click();
    assert.equal(await page.locator('#pattern-select').inputValue(),'CR4');
    await page.locator('#show-directions').check();
    await page.locator('#show-ribbon').check();
    assert.equal(await page.locator('body').evaluate(el=>el.classList.contains('broad-lines')),true);
    await page.locator('#show-ribbon').uncheck();
    await page.locator('#show-context').uncheck();
    await page.locator('.pattern-panel[data-pattern="CR4"] [data-group="outer"]').click();
    const panel=page.locator('.pattern-panel[data-pattern="CR4"]');
    assert.ok(await panel.locator('.diagram .dimmed').count()>0);
    assert.equal(await panel.locator('.diagram .dimmed:visible').count(),0);
    await page.locator('#show-context').check();
    await page.locator('#inspect').evaluate(el=>el.scrollIntoView({block:'start'}));
    await page.screenshot({path:path.join(screenshots,'weave-pair-desktop.png')});
    await panel.locator('.group-button[data-paths="*"]').click();
    await page.locator('#show-directions').uncheck();
    await page.locator('#gallery').evaluate(el=>el.scrollIntoView({block:'start'}));
    await page.screenshot({path:path.join(screenshots,'gallery-desktop.png')});
    await page.locator('#gallery').screenshot({path:path.join(screenshots,'all-patterns.png')});
    await page.locator('#reflection').evaluate(el=>el.scrollIntoView({block:'start'}));
    await page.screenshot({path:path.join(screenshots,'reflection-desktop.png')});
    const assertNoOverflow=async()=>{
      const dimensions=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));
      assert.ok(dimensions.scroll<=dimensions.width+1,JSON.stringify(dimensions));
    };
    await assertNoOverflow();
    await page.setViewportSize({width:390,height:844});
    await page.locator('#inspect').evaluate(el=>el.scrollIntoView({block:'start'}));
    await assertNoOverflow();
    await page.screenshot({path:path.join(screenshots,'inspector-phone.png')});
    await page.locator('#gallery').evaluate(el=>el.scrollIntoView({block:'start'}));
    await page.screenshot({path:path.join(screenshots,'gallery-phone.png')});
    await page.locator('#pattern-select').selectOption('A6');
    await page.reload();
    await page.waitForFunction(()=>document.documentElement.classList.contains('js'));
    assert.equal(await page.locator('#pattern-select').inputValue(),'A6');
    const nojs=await browser.newContext({javaScriptEnabled:false,viewport:{width:390,height:844}});
    const fallback=await nojs.newPage();
    await fallback.goto(base+'/ornaments/centreline/');
    assert.equal(await fallback.locator('.pattern-panel:not(.variant-panel):visible').count(),18);
    assert.equal(await fallback.locator('#notes').isVisible(),true);
    assert.equal(await fallback.locator('.source-sheet').count(),3);
    await nojs.close();
    assert.deepEqual(errors,[]);
    console.log(JSON.stringify({patterns:18,variants:report.variants.length,excluded:10,pathAndGroupSelection:true,keyboard:true,mobile:true,noJavaScript:true,screenshots}));
  } finally {await browser?.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
