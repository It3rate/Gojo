// Browser checks for the recorded study; it does not rerun the analysis.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const http = require('node:http');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'playwright');
const web = path.resolve(__dirname, '../../Visualizations/Gojo.Demos.Web');
const screenshots = process.env.GOJO_SCREENSHOTS || path.resolve(__dirname, '../../artifacts/local-ornament-flow-pilot/site-qa');
const mime = { '.html':'text/html', '.css':'text/css', '.js':'text/javascript', '.json':'application/json', '.svg':'image/svg+xml', '.md':'text/plain' };

(async () => {
  const server = http.createServer(async (req, res) => {
    try {
      let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
      if (file !== web && !file.startsWith(web + path.sep)) { res.writeHead(403); return res.end(); }
      if ((await fs.stat(file)).isDirectory()) file = path.join(file, 'index.html');
      res.writeHead(200, { 'Content-Type':mime[path.extname(file)] || 'application/octet-stream' });
      res.end(await fs.readFile(file));
    } catch { res.writeHead(404); res.end('Not found'); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = (process.env.GOJO_BASE_URL || `http://127.0.0.1:${server.address().port}`).replace(/\/$/, '');
  let browser;
  try {
    await fs.mkdir(screenshots, {recursive:true});
    browser = await chromium.launch({headless:true, executablePath:process.env.GOJO_BROWSER});
    const context = await browser.newContext({viewport:{width:1440,height:1000}, reducedMotion:'reduce'});
    const page = await context.newPage(), errors=[];
    page.on('pageerror', e=>errors.push(e.message));
    page.on('response', r=>{if(r.url().startsWith(base) && r.status()>=400) errors.push(`${r.status()} ${r.url()}`);});
    for(const suffix of ['', '/', '/index.html']) {
      await page.goto(base+'/ornaments/signatures'+suffix);
      await page.waitForFunction(()=>document.documentElement.classList.contains('js'));
      assert.equal(await page.locator('.pattern-panel:visible').count(),1);
    }
    const data = await (await context.request.get(base+'/ornaments/signatures/study-data.json')).json();
    assert.equal(await page.locator('.pattern-card').count(),data.patterns.length);
    assert.equal(await page.locator('.contrast').count(),data.contrasts.length);
    assert.equal(await page.locator('.overlap-pair').count(),data.overlaps.length);
    const internal = await page.locator('a[href],script[src],link[href]').evaluateAll(nodes=>nodes.map(n=>n.href||n.src));
    for(const url of new Set(internal.filter(u=>u.startsWith(base)))) {
      const response=await context.request.get(url.split('#')[0]);
      assert.equal(response.status(),200,url);
    }
    const ids=await page.locator('[id]').evaluateAll(nodes=>nodes.map(n=>n.id));
    assert.equal(new Set(ids).size,ids.length,'duplicate document/SVG IDs');
    const localAnchors=await page.locator('a[href^="#"]').evaluateAll(nodes=>nodes.map(n=>n.getAttribute('href').slice(1)));
    assert.deepEqual(await page.evaluate(anchors=>anchors.filter(id=>!document.getElementById(id)),localAnchors),[]);
    for(const p of data.patterns) {
      await page.locator('#pattern-select').selectOption(p.id);
      await page.waitForFunction(id=>document.querySelector('.pattern-panel:not([hidden])')?.dataset.pattern===id,p.id);
      for(const kind of ['equal','proportion','cut','direction']) {
        await page.locator('#read-kind').selectOption(kind);
        assert.ok((await page.locator('#witness-count').textContent()).length>0);
        if(await page.locator('#next-witness').isEnabled()) await page.locator('#next-witness').click();
      }
    }
    await page.locator('#pattern-select').selectOption(data.patterns[0].id);
    await page.locator('#pattern-select').focus();
    await page.keyboard.press('ArrowDown');
    assert.equal(await page.locator('#pattern-select').inputValue(),data.patterns[1].id);
    assert.equal(await page.evaluate(()=>document.activeElement.id),'pattern-select');
    await page.keyboard.press('ArrowLeft');
    assert.equal(await page.locator('#pattern-select').inputValue(),data.patterns[0].id);
    for(const layer of ['flow','quarter','half','labels']) {
      await page.locator(`[data-layer="${layer}"]`).check();
      assert.equal(await page.locator('body').evaluate((body,layer)=>body.classList.contains('no-'+layer),layer),false);
    }
    await page.locator('#read-kind').selectOption('cut');
    await page.locator('#frame-filter').selectOption('quarter-left');
    await page.locator('#traversal-filter').selectOption('backward');
    await page.locator('#inspector').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'inspector-desktop.png')});
    await page.locator('#collection').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'patterns-desktop.png')});
    await page.locator('#contrasts').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'contrasts-desktop.png')});
    const checkOverflow = async()=>{
      const overflow=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth,
        outside:[...document.querySelectorAll('body *')].filter(el=>{
          if(!(el instanceof HTMLElement)||el.getBoundingClientRect().right<=innerWidth+1||!el.getClientRects().length||getComputedStyle(el).position==='absolute')return false;
          for(let p=el.parentElement;p;p=p.parentElement)if(['auto','scroll','hidden','clip'].includes(getComputedStyle(p).overflowX))return false;
          return true;
        }).slice(0,20).map(el=>({tag:el.tagName,id:el.id,class:el.className,text:el.textContent.slice(0,80)}))}));
      assert.ok(overflow.scroll<=overflow.width+1,'page overflows viewport: '+JSON.stringify(overflow));
    };
    await checkOverflow();
    await page.setViewportSize({width:390,height:844});
    await page.locator('#collection').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'patterns-phone.png')});
    await checkOverflow();
    await page.locator('#inspector').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'inspector-phone.png')});
    await page.locator('#witness-content').scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(screenshots,'witness-phone.png')});
    await checkOverflow();
    const plain=await browser.newContext({javaScriptEnabled:false,viewport:{width:390,height:844}});
    const staticPage=await plain.newPage();
    await staticPage.goto(base+'/ornaments/signatures/');
    assert.equal(await staticPage.locator('.pattern-panel:visible').count(),data.patterns.length);
    assert.equal(await staticPage.locator('.selection-controls:visible').count(),0);
    assert.deepEqual(errors,[]);
    console.log(`Checked ${data.patterns.length} references, ${data.contrasts.length} contrasts, all window reads, keyboard focus, no-JS fallback and desktop/phone layout. Screenshots: ${screenshots}`);
  } finally { await browser?.close(); await new Promise(resolve=>server.close(resolve)); }
})().catch(error=>{console.error(error);process.exitCode=1;});
