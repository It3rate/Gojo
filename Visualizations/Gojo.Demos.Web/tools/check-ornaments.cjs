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
    async function loadImages() {
      await page.evaluate(async () => {
        for (const image of document.images) image.loading='eager';
        await Promise.all([...document.images].map(image=>image.decode()));
      });
    }
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
        if (suffix === '/gallery.html') await page.waitForURL(base+'/ornaments/'+c.id+'/');
        assert.equal(await page.locator('.ornament-navigation').count(),1);
        const anchors = await page.locator('a[href^="#"]').evaluateAll(as => as.map(a => a.getAttribute('href').slice(1)));
        assert.deepEqual(await page.evaluate(ids => ids.filter(id => !document.getElementById(id)),anchors),[]);
      }
      const legacy = await fs.readFile(path.join(web,'ornaments',c.id,'gallery.html'),'utf8');
      assert.ok(Buffer.byteLength(legacy)<2048,'Gallery alias duplicates its canonical page: '+c.id);
      await page.goto(base+'/ornaments/'+c.id+'/gallery.html?alias-check=1#alias-check');
      await page.waitForURL(base+'/ornaments/'+c.id+'/?alias-check=1#alias-check');
      assert.equal(await page.locator('.ornament-navigation').count(),1);
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
          if (suffix === '/gallery.html') await page.waitForURL(base+'/ornaments/'+c.id+'/');
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
      } else if(c.presentation === 'interior-ornament-compact') {
        const manifest=JSON.parse(await fs.readFile(path.join(web,'ornaments',c.id,'publication.json'),'utf8'));
        assert.equal(manifest.count,12);
        assert.equal(manifest.files.filter(f=>/\/patterns\/F\d{3}-[24]\.svg$/.test(f.source)).length,24);
        assert.equal(manifest.files.filter(f=>/\.gz$|gallery-data\.json$/.test(f.source)).length,0);
        assert.equal(await page.locator('.publication-scope').count(),1);
        assert.equal(await page.locator('.card').count(),12);
        const cases=JSON.parse(await page.locator('#data').textContent());
        assert.equal(cases.length,12);
        assert.ok(cases.every(e=>!e.panels&&!e.drawing&&!e.nativePacket));
        for(const item of cases) {
          await page.selectOption('#design',item.id);
          assert.equal(await page.locator('#title').textContent(),item.id+' · '+item.name);
          for(const view of ['design','mesh']) {
            await page.locator('[data-view="'+view+'"]').click();
            assert.equal(await page.locator('[data-view="'+view+'"]').getAttribute('aria-pressed'),'true');
            const image=new URL(await page.locator('#field').getAttribute('src'),base+'/ornaments/'+c.id+'/').pathname;
            assert.equal(image,'/ornaments/'+c.id+'/'+item[view]);
            assert.equal(await page.locator('#original').getAttribute('href'),item[view]);
          }
        }
        await page.goto(base+'/ornaments/'+c.id+'/gallery.html?case=F011');
        await page.waitForURL(base+'/ornaments/'+c.id+'/?case=F011');
        assert.equal(await page.locator('#design').inputValue(),'F011');
        await page.locator('#next').click();assert.equal(await page.locator('#design').inputValue(),'F012');
        await page.locator('#previous').click();assert.equal(await page.locator('#design').inputValue(),'F011');
        await page.goto(base+'/ornaments/'+c.id+'/designs.html');
        assert.equal(await page.locator('.design-tile').count(),12);
        assert.equal(await page.locator('.design-tile img').count(),12);
        await loadImages();
        await page.screenshot({path:path.join(screenshots,c.id+'-sheet.png'),fullPage:true});
        await page.goto(base+'/ornaments/'+c.id+'/');
        await loadImages();
        await page.screenshot({path:path.join(screenshots,c.id+'-desktop.png'),fullPage:true});
        console.log('Verified all twelve interior ornament designs, retained mesh views, case selection, captured scope and whole design sheet.');
      } else if(c.presentation === 'interiors-compact') {
        const manifest = JSON.parse(await fs.readFile(path.join(web,'ornaments',c.id,'publication.json'),'utf8'));
        assert.equal(manifest.count,100);
        assert.equal(manifest.presentation,'interiors-compact');
        assert.equal(manifest.files.filter(f => /\/I\d{3}(?:-cells)?\.svg$/.test(f.source)).length,200);
        assert.equal(manifest.files.filter(f => /\/X0[1-4]\.svg$/.test(f.source)).length,4);
        assert.equal(manifest.files.filter(f => f.source.endsWith('.gz')).length,0);
        assert.equal(await page.locator('.card').count(),100);
        assert.equal(await page.locator('.publication-scope').count(),1);
        assert.match(await page.locator('.publication-scope').textContent(),/Construction stages, full diagnostics and native archives remain/);
        const data=await page.locator('#data').textContent(),captured=JSON.parse(data);
        assert.equal(captured.length,100);
        const published=new Set(manifest.files.map(f=>f.url));
        for(const mode of ['cells','lines']) {
          await page.selectOption('#view',mode);
          for(const item of captured) {
            await page.selectOption('#design',item.id);
            const image=new URL(await page.locator('#field').getAttribute('src'),base+'/ornaments/'+c.id+'/').pathname;
            assert.equal(image,'/ornaments/'+c.id+'/'+item.id+(mode==='cells'?'-cells':'')+'.svg');
            assert.ok(published.has(image));
            assert.equal(await page.locator('#title').textContent(),item.id+' · '+item.name);
            assert.equal(await page.locator('#fingerprint').textContent(),item.fingerprint);
            assert.match(await page.locator('#native').textContent(),/0 numeric carriers/);
            assert.ok(item.overlaps===0&&item.multiplyOwned===0&&item.unowned===0);
            assert.equal(await page.locator('.card.selected').getAttribute('data-case'),item.id);
          }
        }
        await page.goto(base+'/ornaments/'+c.id+'/?design=I090');
        assert.equal(await page.locator('#design').inputValue(),'I090');
        await page.locator('#next').click();assert.equal(await page.locator('#design').inputValue(),'I091');
        await page.locator('#previous').click();assert.equal(await page.locator('#design').inputValue(),'I090');
        await page.selectOption('#family',captured[0].family);
        assert.equal(await page.locator('.card:visible').count(),captured.filter(x=>x.family===captured[0].family).length);
        await page.selectOption('#family','');await page.locator('#search').fill('I061');
        assert.equal(await page.locator('.card:visible').count(),1);
        await page.locator('.card:visible').click();assert.equal(await page.locator('#design').inputValue(),'I061');
        await page.locator('#search').fill('');
        await page.locator('[data-rating="4"]').click();
        await page.locator('#review-note').fill('A repeated shared-boundary study.');
        await page.reload();assert.equal(await page.locator('#review-note').inputValue(),'A repeated shared-boundary study.');
        assert.equal(await page.locator('[data-rating="4"]').getAttribute('aria-pressed'),'true');
        const downloadEvent=page.waitForEvent('download');await page.locator('#export').click();
        const download=await downloadEvent,exportedPath=path.join(screenshots,'interior-ratings.json');await download.saveAs(exportedPath);
        const exported=JSON.parse(await fs.readFile(exportedPath,'utf8'));
        assert.equal(exported.reviews.I061.rating,4);
        assert.equal(exported.reviews.I061.fingerprint,captured.find(x=>x.id==='I061').fingerprint);
        await page.locator('[data-rating="0"]').click();
        await page.locator('#review-note').fill('');
        await page.locator('#import-file').setInputFiles(exportedPath);
        await page.waitForFunction(()=>document.getElementById('rating-status').textContent.startsWith('Imported'));
        assert.equal(await page.locator('#review-note').inputValue(),'A repeated shared-boundary study.');
        const bad={...exported,reviews:{I061:{...exported.reviews.I061,fingerprint:'wrong-capture'}}};
        await page.locator('#import-file').setInputFiles({name:'mismatched-ratings.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(bad))});
        await page.waitForFunction(()=>document.getElementById('rating-status').textContent.includes('do not match'));
        assert.equal(await page.locator('#review-note').inputValue(),'A repeated shared-boundary study.');
        await page.evaluate(value=>localStorage.setItem('gojo-lattice-interiors-web-ratings-v1',JSON.stringify(value.reviews)),bad);
        await page.reload();
        assert.equal(await page.locator('#rating-status').textContent(),'Not rated');
        assert.equal(await page.locator('#review-note').inputValue(),'');
        await page.evaluate(()=>localStorage.removeItem('gojo-lattice-interiors-web-ratings-v1'));
        await page.goto(base+'/ornaments/'+c.id+'/overview.html');
        assert.equal(await page.locator('figure').count(),100);
        for(const mode of ['cells','lines']) {
          await page.selectOption('#overview-mode',mode);
          const images=await page.locator('figure img').evaluateAll(xs=>xs.map(x=>({id:x.dataset.case,url:new URL(x.src).pathname})));
          assert.equal(images.length,100);
          for(const image of images)assert.equal(image.url,'/ornaments/'+c.id+'/'+image.id+(mode==='cells'?'-cells':'')+'.svg');
        }
        await page.goto(base+'/ornaments/'+c.id+'/rules.html');
        assert.equal(await page.locator('.rules img').count(),4);
        assert.equal(await page.locator('a[href$=".gz"]').count(),0);
        assert.equal(await page.locator('.publication-scope').count(),1);
        await page.goto(base+'/ornaments/'+c.id+'/');
        await page.screenshot({path:path.join(screenshots,c.id+'-desktop.png')});
        console.log('Verified all 100 interior designs in both views, captured facts, filters, ratings, matching import, mismatched import rejection and all-design overview.');
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
    for(const route of ['/ornaments/',...collections.flatMap(c => ['/ornaments/'+c.id+'/',...(c.presentation==='interiors-compact'?['/ornaments/'+c.id+'/overview.html','/ornaments/'+c.id+'/rules.html']:[]),...(c.presentation==='interior-ornament-compact'?['/ornaments/'+c.id+'/designs.html']:[])])]) {
      await page.goto(base+route);
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth+1),'Mobile overflow: '+route);
      await loadImages();
      await page.screenshot({path:path.join(screenshots,route.split('/').filter(Boolean).join('-')+'-mobile.png')});
    }
    const missing = await context.request.get(base+'/ornaments/no-such-collection/absent.json');
    assert.equal(missing.status(),404,'Missing ornament resource should not return the homepage');
    assert.deepEqual(errors,[]);
    console.log('Verified '+links.size+' linked resources, navigation, gallery aliases, controls and mobile layouts. Screenshots: '+screenshots);
  } finally { await browser?.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error => { console.error(error); process.exitCode=1; });
