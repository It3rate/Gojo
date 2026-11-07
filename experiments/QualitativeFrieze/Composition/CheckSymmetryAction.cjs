// Presentation checks only. Native C# tests own action and correspondence laws.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const http = require('node:http');
const {chromium} = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'playwright');
const root = path.resolve(process.env.GOJO_WEB_ROOT || path.join(__dirname, '../../../Visualizations/Gojo.Demos.Web'));
const screenshots = process.env.GOJO_SCREENSHOTS || path.resolve(__dirname, '../../../artifacts/symmetry-actions/browser');
const mime = {'.html':'text/html','.css':'text/css','.js':'text/javascript','.json':'application/json','.svg':'image/svg+xml','.md':'text/plain','.png':'image/png'};
(async () => {
  const server = http.createServer(async (req,res) => {
    try {
      let file = path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
      if(file !== root && !file.startsWith(root+path.sep)){res.writeHead(403);return res.end();}
      if((await fs.stat(file)).isDirectory())file=path.join(file,'index.html');
      res.writeHead(200,{'Content-Type':mime[path.extname(file)] || 'application/octet-stream'});
      res.end(await fs.readFile(file));
    }catch{res.writeHead(404);res.end('Not found');}
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base=(process.env.GOJO_BASE_URL || `http://127.0.0.1:${server.address().port}`).replace(/\/$/,'');
  let browser;
  try {
    await fs.mkdir(screenshots,{recursive:true});
    browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context=await browser.newContext({viewport:{width:1440,height:1050},reducedMotion:'reduce'});
    const page=await context.newPage(),errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    page.on('response',r=>{if(r.url().startsWith(base)&&r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
    for(const suffix of ['', '/', '/index.html']){
      await page.goto(base+'/ornaments/symmetry-actions'+suffix);
      await page.waitForFunction(()=>document.documentElement.classList.contains('js'));
      assert.equal(await page.locator('.action-panel:visible').count(),1);
    }
    const report=await(await context.request.get(base+'/ornaments/symmetry-actions/study-data.json')).json();
    assert.equal(report.corpus.length,18);
    assert.equal(report.actions.length,report.templates.length*16*report.offsets.length);
    assert.equal(await page.locator('.action-panel').count(),report.actions.length);
    assert.equal(await page.locator('.reading-panel').count(),report.tReadings.length);
    assert.equal(await page.locator('.engine-panel').count(),report.engines.length);
    assert.equal(await page.locator('.control-panel').count(),report.controls.length);
    const ids=await page.locator('[id]').evaluateAll(nodes=>nodes.map(n=>n.id));
    assert.equal(new Set(ids).size,ids.length,'Duplicate HTML or SVG IDs');
    const links=await page.locator('a[href],link[href],script[src],img[src]').evaluateAll(nodes=>nodes.map(n=>n.href||n.src));
    for(const url of new Set(links.filter(u=>u.startsWith(base)))){
      const result=await context.request.get(url.split('#')[0]);
      assert.equal(result.status(),200,url);
    }
    for(const template of report.templates){
      await page.locator('#template-select').selectOption(template.id);
      for(const offset of report.offsets){
        await page.locator('#offset-select').selectOption(offset.id);
        for(let code=0;code<16;code++){
          await page.locator('#action-code').selectOption(String(code));
          const expected=report.actions.find(row=>row.templateId===template.id&&row.offsetId===offset.id&&row.code===code);
          const active=page.locator('.action-panel:visible');
          assert.equal(await active.count(),1);
          assert.equal(await active.getAttribute('data-template'),template.id);
          assert.equal(await active.getAttribute('data-offset'),offset.id);
          assert.equal(await active.getAttribute('data-bits'),expected.bits);
          assert.equal(await page.locator('.bit-toggle strong').allTextContents().then(x=>x.join('')),expected.bits);
        }
      }
    }
    await page.locator('#action-code').selectOption('0');
    for(const bit of [0,1,2,3]){
      await page.locator(`.bit-toggle[data-bit="${bit}"]`).click();
      assert.equal(await page.locator('#action-code').inputValue(),String((1<<(bit+1))-1));
    }
    await page.locator('#next-action').click();
    assert.equal(await page.locator('#action-code').inputValue(),'0');
    await page.locator('#previous-action').click();
    assert.equal(await page.locator('#action-code').inputValue(),'15');
    for(const id of ['template-select','offset-select','action-code']){
      const select=page.locator('#'+id);
      await select.focus();
      const before=await select.evaluate(el=>el.selectedIndex);
      const length=await select.locator('option').count();
      await page.keyboard.press('ArrowDown');
      assert.equal(await select.evaluate(el=>el.selectedIndex),(before+1)%length);
      assert.equal(await page.evaluate(()=>document.activeElement.id),id);
      await page.keyboard.press('ArrowLeft');
      assert.equal(await select.evaluate(el=>el.selectedIndex),before);
      assert.equal(await page.evaluate(()=>document.activeElement.id),id);
    }
    for(const engine of report.engines){
      await page.locator('#engine-select').selectOption(engine.id);
      assert.equal(await page.locator('.engine-panel:visible').getAttribute('data-engine'),engine.id);
    }
    if(report.engines.length){
      const select=page.locator('#engine-select');
      await select.focus();
      await page.keyboard.press('ArrowDown');
      assert.equal(await page.evaluate(()=>document.activeElement.id),'engine-select');
      await page.locator('[data-select-engine]').first().click();
      assert.equal(await page.locator('.engine-panel:visible').getAttribute('data-engine'),await page.locator('[data-select-engine]').first().getAttribute('data-select-engine'));
    }
    await page.locator('#show-arrows').uncheck();
    assert.equal(await page.locator('.arrow:visible').count(),0);
    await page.locator('#show-arrows').check();
    assert.ok(await page.locator('.arrow:visible').count()>0);
    await page.locator('#overlap').screenshot({path:path.join(screenshots,'overlapping-readings.png')});
    await page.locator('#template-select').selectOption(report.templates[0].id);
    await page.locator('#offset-select').selectOption(report.offsets[0].id);
    await page.locator('#action-code').selectOption('8');
    await page.locator('#laboratory').evaluate(el=>el.scrollIntoView({block:'start'}));
    await page.screenshot({path:path.join(screenshots,'action-laboratory-desktop.png')});
    await page.locator('#engines').evaluate(el=>el.scrollIntoView({block:'start'}));
    await page.screenshot({path:path.join(screenshots,'candidate-engine-desktop.png')});
    await page.locator('#controls').screenshot({path:path.join(screenshots,'comparison-controls.png')});
    const noOverflow=async()=>{
      const dims=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth}));
      assert.ok(dims.scroll<=dims.width+1,JSON.stringify(dims));
    };
    await noOverflow();
    await page.reload();
    await page.waitForFunction(()=>document.documentElement.classList.contains('js'));
    assert.equal(await page.locator('#action-code').inputValue(),'8');
    assert.equal(await page.locator('#template-select').inputValue(),report.templates[0].id);
    await page.setViewportSize({width:390,height:844});
    await page.locator('#laboratory').evaluate(el=>el.scrollIntoView({block:'start'}));
    await noOverflow();
    await page.screenshot({path:path.join(screenshots,'action-laboratory-phone.png')});
    await page.locator('#overlap').evaluate(el=>el.scrollIntoView({block:'start'}));
    await page.screenshot({path:path.join(screenshots,'overlapping-readings-phone.png')});
    const nojs=await browser.newContext({javaScriptEnabled:false,viewport:{width:390,height:844}});
    const fallback=await nojs.newPage();
    await fallback.goto(base+'/ornaments/symmetry-actions/');
    assert.equal(await fallback.locator('.action-panel:visible').count(),1);
    assert.equal(await fallback.locator('.lab-controls:visible').count(),0);
    assert.equal(await fallback.locator('.reading-panel:visible').count(),report.tReadings.length);
    assert.equal(await fallback.locator('#notes').isVisible(),true);
    await nojs.close();
    assert.deepEqual(errors,[]);
    console.log(JSON.stringify({patterns:report.corpus.length,readings:report.tReadings.length,actions:report.actions.length,engines:report.engines.length,controls:report.controls.length,selection:true,bitButtons:true,keyboard:true,mobile:true,noJavaScript:true,screenshots}));
  }finally{await browser?.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
