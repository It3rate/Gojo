// Check the static publication and its original gallery controls, locally or live.
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'playwright');
const fs = require('node:fs/promises'), path = require('node:path'), http = require('node:http');
const assert = require('node:assert/strict'), crypto = require('node:crypto');
const web = path.resolve(__dirname, '..');
const screenshots = process.env.GOJO_SCREENSHOTS || path.join(require('node:os').tmpdir(), 'GojoOrnamentsQA');
const types = { '.html':'text/html', '.css':'text/css', '.js':'text/javascript', '.svg':'image/svg+xml', '.json':'application/json', '.png':'image/png', '.jpg':'image/jpeg', '.txt':'text/plain', '.md':'text/plain' };
async function walk(dir) {
  const files = [];
  for (const item of await fs.readdir(dir, { withFileTypes:true })) {
    const file = path.join(dir,item.name);
    files.push(...(item.isDirectory() ? await walk(file) : [file]));
  }
  return files;
}
(async () => {
  const server = http.createServer(async (req,res) => {
    try {
      let file = path.resolve(web,'.' + decodeURIComponent(new URL(req.url,'http://localhost').pathname));
      if (file !== web && !file.startsWith(web + path.sep)) { res.writeHead(403); return res.end(); }
      if ((await fs.stat(file)).isDirectory()) file = path.join(file,'index.html');
      const bytes = await fs.readFile(file);
      res.writeHead(200, { 'Content-Type':types[path.extname(file)] || 'application/octet-stream' }); res.end(bytes);
    } catch { res.writeHead(404); res.end('Not found'); }
  });
  await new Promise(resolve => server.listen(0,'127.0.0.1',resolve));
  const base = (process.env.GOJO_BASE_URL || 'http://127.0.0.1:' + server.address().port).replace(/\/$/,'');
  let browser;
  try {
    await fs.mkdir(screenshots,{recursive:true});
    browser = await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context = await browser.newContext({viewport:{width:1440,height:1100},reducedMotion:'reduce'});
    const page = await context.newPage(), errors = [];
    page.on('pageerror', e => errors.push(e.message));
    page.on('response', r => { if(r.status() >= 400 && r.url().startsWith(base)) errors.push(r.status() + ' ' + r.url()); });
    const {collections} = JSON.parse(await fs.readFile(path.join(__dirname,'ornament-collections.json'),'utf8'));
    const links = new Set();
    for (const file of (await walk(path.join(web,'ornaments'))).filter(f => f.endsWith('.html'))) {
      const route = '/' + path.relative(web,file).replaceAll(path.sep,'/');
      for (const [,raw] of (await fs.readFile(file,'utf8')).matchAll(/(?:href|src)=['"]([^'"]+)['"]/g)) {
        const url = new URL(raw.replaceAll('&amp;','&'),base + route);
        if (url.origin !== base || raw.startsWith('#')) continue;
        url.hash = ''; links.add(url.href);
      }
    }
    const list = [...links];
    for (let i=0;i<list.length;i+=6) await Promise.all(list.slice(i,i+6).map(async url => {
      const response = await context.request.get(url); assert.equal(response.status(),200,url);
      if (!new URL(url).pathname.endsWith('/') && !url.endsWith('.html'))
        assert.ok(!/^\s*<!doctype html/i.test((await response.body()).toString('utf8',0,80)),'HTML fallback: ' + url);
    }));
    for (const c of collections) {
      const manifest = JSON.parse(await fs.readFile(path.join(web,'ornaments',c.id,'publication.json'),'utf8'));
      const assets = manifest.files.filter(f => !f.source.endsWith('.html'));
      for (let i=0;i<assets.length;i+=6) await Promise.all(assets.slice(i,i+6).map(async f => {
        const response = await context.request.get(base+f.url); assert.equal(response.status(),200,f.url);
        assert.equal(crypto.createHash('sha256').update(await response.body()).digest('hex'),f.sha256,f.url);
      }));
      console.log('Verified published artifact hashes for ' + c.id + ': ' + assets.length + ' files.');
    }
    await page.goto(base+'/');
    assert.equal(await page.locator('a[href="/ornaments/"]').count(),1);
    for (const suffix of ['', '/', '/index.html']) {
      await page.goto(base+'/ornaments'+suffix);
      assert.equal(await page.locator('.collection').count(),collections.length);
      assert.equal(await page.locator('h1').textContent(),'Ornament studies');
    }
    await page.screenshot({path:path.join(screenshots,'collections-desktop.png'),fullPage:true});
    for (const c of collections) {
      for (const suffix of ['', '/', '/index.html', '/gallery.html']) {
        await page.goto(base+'/ornaments/'+c.id+suffix);
        assert.equal(await page.locator('.ornament-navigation').count(),1);
        const anchors = await page.locator('a[href^="#"]').evaluateAll(as => as.map(a => a.getAttribute('href').slice(1)));
        assert.deepEqual(await page.evaluate(ids => ids.filter(id => !document.getElementById(id)),anchors),[]);
      }
      if(c.presentation === 'refined-compact') {
        const manifest = JSON.parse(await fs.readFile(path.join(web,'ornaments',c.id,'publication.json'),'utf8'));
        assert.equal(manifest.count,108);
        assert.equal(manifest.presentation,'refined-compact');
        assert.equal(manifest.files.filter(f => /\/R-B[\d]+(?:\.B)?-(?:field|motif|detail)(?:-closeup)?\.svg$/.test(f.source)).length,216);
        assert.equal(manifest.files.filter(f => f.source.endsWith('.gz')).length,0);
        assert.equal(await page.locator('article').count(),36);
        assert.equal(await page.locator('.publication-scope').count(),1);
        assert.equal(await page.locator('#curves, #construction').count(),0);
        const published = new Set(manifest.files.map(f => f.url));
        async function checkSelected(mode, unit) {
          const chosen = await page.locator('article').evaluateAll(cards => cards.map(card => ({
            mode:card.dataset.mode,
            image:new URL(card.querySelector('.hero img').src).pathname,
            link:new URL(card.querySelector('.hero').href).pathname,
            pressed:[...card.querySelectorAll('.choice[aria-pressed=true]')].map(button => button.dataset.mode)
          })));
          for (const item of chosen) {
            assert.equal(item.mode,mode);
            assert.deepEqual(item.pressed,[mode]);
            assert.equal(item.image,item.link);
            assert.ok(published.has(item.image),'Undeclared gallery image: '+item.image);
            assert.ok(item.image.endsWith('-'+mode+(unit?'-closeup':'')+'.svg'));
          }
        }
        for (const suffix of ['', '/', '/index.html', '/gallery.html']) {
          await page.goto(base+'/ornaments/'+c.id+suffix);
          for (const mode of ['field','detail','motif']) {
            await page.selectOption('#global-mode',mode);
            await checkSelected(mode,false);
          }
          await page.locator('#unit').check();
          await checkSelected('motif',true);
          await page.locator('#unit').uncheck();
          await checkSelected('motif',false);
        }
        const first = page.locator('article').first();
        await first.locator('.choice[data-mode="detail"]').click();
        assert.equal(await first.getAttribute('data-mode'),'detail');
        assert.ok((await first.locator('.hero img').getAttribute('src')).endsWith('-detail.svg'));
        assert.equal(await page.locator('article[data-mode="motif"]').count(),35);
        await page.goto(base+'/ornaments/'+c.id+'/overview.html');
        assert.equal(await page.locator('.triptych img').count(),108);
        await page.goto(base+'/ornaments/'+c.id+'/complexity.html');
        assert.equal(await page.locator('tbody tr').count(),36);
        await page.goto(base+'/ornaments/'+c.id+'/');
        await page.screenshot({path:path.join(screenshots,c.id+'-desktop.png')});
      } else if(c.id === 'bourgoin') {
        assert.equal(await page.locator('article').count(),36);
        for(const view of ['units','ports','construction','ink']) {
          await page.selectOption('#view',view);
          assert.equal(await page.locator('body').getAttribute('data-view'),view);
        }
        await page.locator('#weight').fill('4');
        assert.equal(await page.evaluate(() => document.documentElement.style.getPropertyValue('--weight')),'4');
        const overlay = page.locator('.comparison-workbench').first();
        await overlay.evaluate(el => { const details=el.closest('details'); if(details)details.open=true; });
        const original = await overlay.locator('.registered-ink').getAttribute('transform');
        await overlay.locator('[data-register="x"]').evaluate(el => { el.value=Number(el.value)+1;el.dispatchEvent(new Event('input',{bubbles:true})); });
        assert.notEqual(await overlay.locator('.registered-ink').getAttribute('transform'),original);
        await overlay.locator('.reset-registration').click();
        assert.equal(await overlay.locator('.registered-ink').getAttribute('transform'),original);
        await page.goto(base+'/ornaments/'+c.id+'/');
        await page.screenshot({path:path.join(screenshots,c.id+'-desktop.png')});
      }
    }
    await page.setViewportSize({width:390,height:844});
    for(const route of ['/ornaments/',...collections.map(c => '/ornaments/'+c.id+'/')]) {
      await page.goto(base+route);
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth+1),'Mobile overflow: '+route);
      await page.screenshot({path:path.join(screenshots,route.split('/').filter(Boolean).join('-')+'-mobile.png')});
    }
    const missing = await context.request.get(base+'/ornaments/no-such-collection/absent.json');
    assert.equal(missing.status(),404,'Missing ornament resource should not return the homepage');
    assert.deepEqual(errors,[]);
    console.log('Verified '+links.size+' linked resources, navigation, gallery aliases, controls and mobile layouts. Screenshots: '+screenshots);
  } finally { await browser?.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error => { console.error(error); process.exitCode=1; });
