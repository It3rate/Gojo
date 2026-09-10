// Browser smoke/visual QA. Set GOJO_PLAYWRIGHT_MODULE to a bundled Playwright
// package if it is not installed locally; GOJO_BROWSER can select installed Edge.
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'playwright');
const fs=require('node:fs/promises'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const web=path.resolve(__dirname,'..');
const output=process.env.GOJO_SCREENSHOTS || path.join(require('node:os').tmpdir(),'GojoSyntaxGalleryQA');
const types={'.html':'text/html','.js':'text/javascript','.css':'text/css','.svg':'image/svg+xml','.gojo':'text/plain'};
(async()=>{
  const catalogue=JSON.parse(await fs.readFile(path.join(__dirname,'syntax-catalog.json'),'utf8'));
  const collection=JSON.parse(await fs.readFile(path.join(__dirname,'syntax-walkthroughs.json'),'utf8'));
  const entries=new Map(catalogue.demos.map(d=>[d.id,d]));
  for(const d of collection.walkthroughs)entries.set(d.id,d);
  const demoRoute=d=>'/syntax/'+(d.route||d.id)+'/';
  const server=http.createServer(async(req,res)=>{
    try{
      const requested=decodeURIComponent(new URL(req.url,'http://localhost').pathname);
      let file=path.resolve(web,'.'+requested);
      if(file!==web&&!file.startsWith(web+path.sep)){res.writeHead(403);return res.end();}
      if((await fs.stat(file)).isDirectory())file=path.join(file,'index.html');
      const data=await fs.readFile(file);
      res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream'});res.end(data);
    }catch{res.writeHead(404);res.end('Not found');}
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base=process.env.GOJO_BASE_URL||'http://127.0.0.1:'+server.address().port;
  let browser;
  try{
    await fs.mkdir(output,{recursive:true});
    browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context=await browser.newContext({viewport:{width:1440,height:1100},reducedMotion:'reduce'});
    // Keep copy checks inside the test page so they do not overwrite the
    // person's operating-system clipboard while the suite runs.
    await context.addInitScript(()=>{
      let copied='';
      Object.defineProperty(navigator,'clipboard',{configurable:true,value:{
        writeText:async text=>{copied=text;},readText:async()=>copied
      }});
    });
    const page=await context.newPage(),errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    page.on('response',r=>{if(r.status()>=400&&r.url().startsWith(base))errors.push(r.status()+' '+r.url());});
    // Azure also serves directory indexes without adding a trailing slash.
    // Exercise the URL the browser actually uses to resolve every asset/link.
    for(const demo of [null,...catalogue.demos]){
      const route='/syntax'+(demo?'/'+demo.id:'');
      for(const suffix of ['', '/', '/index.html']){
        await page.goto(base+route+suffix+(demo?'?step=1':''));
        assert.equal(await page.evaluate(()=>getComputedStyle(document.body).margin),'0px',route+suffix+' CSS');
        const assets=await page.locator('link[rel=stylesheet], script[src], img[src]').evaluateAll(elements=>elements.map(e=>e.href||e.src));
        assert.ok(assets.length>0);
        for(const asset of assets)assert.ok(new URL(asset).pathname.startsWith('/syntax/'),asset);
        const links=await page.locator('a[href]').evaluateAll(elements=>elements.map(e=>({raw:e.getAttribute('href'),url:e.href})));
        for(const link of links){
          if(link.raw.startsWith('#')||new URL(link.url).origin!==base)continue;
          const pathname=new URL(link.url).pathname;
          assert.ok(pathname==='/'||pathname.startsWith('/syntax/'),link.url);
        }
        if(demo){
          assert.equal(await page.locator('#step-result').textContent(),demo.steps[1].result);
          await page.locator('#open-syntax').click();
          assert.match(await page.locator('#syntax-code').textContent(),/^gojo 1;/);
          const download=await page.request.get(await page.locator('.syntax-actions a[download]').evaluate(e=>e.href));
          assert.equal(download.status(),200);
          assert.match(await download.text(),/^gojo 1;/);
          await page.keyboard.press('Escape');
        }else{
          const loaded=await page.locator('.thumbnail img').evaluateAll(async images=>{
            images.forEach(image=>{image.loading='eager';});
            return Promise.all(images.map(async image=>{await image.decode();return image.naturalWidth>0;}));
          });
          assert.equal(loaded.length,entries.size);assert.ok(loaded.every(Boolean));
          await page.locator('.demo-card').first().click();
          assert.equal(new URL(page.url()).pathname,demoRoute(entries.get(collection.featured[0])));
        }
      }
    }
    await page.goto(base+'/syntax/');
    assert.equal(await page.locator('.demo-card').count(),entries.size);
    assert.deepEqual(await page.locator('.featured .demo-card').evaluateAll(cards=>cards.map(c=>c.getAttribute('href'))),collection.featured.map(id=>demoRoute(entries.get(id))));
    const featuredBottom=await page.locator('.featured').evaluate(el=>el.getBoundingClientRect().bottom);
    assert.ok(featuredBottom<900,'Featured collection should fit above the fold on a 1440 × 900 desktop: '+featuredBottom);
    const imageWidths=await page.locator('.thumbnail img').evaluateAll(images=>images.map(i=>i.getBoundingClientRect().width));
    assert.ok(imageWidths[6]<imageWidths[0]/2,'Extras need smaller thumbnails');
    await page.screenshot({path:path.join(output,'gallery-desktop.png'),fullPage:true});
    for(const category of ['Structure','Uncertainty','Change','All']){
      await page.locator('[data-filter="'+category+'"]').click();
      assert.equal(await page.locator('.demo-card:visible').count(),category==='All'?entries.size:[...entries.values()].filter(d=>d.category===category).length);
      assert.equal(await page.locator('.featured').isVisible(),category!=='Uncertainty');
    }
    let states=0;
    for(const d of catalogue.demos){
      await page.goto(base+'/syntax/'+d.id+'/');
      for(let i=0;i<d.steps.length;i++){
        await page.locator('[data-step="'+i+'"]').click();
        assert.equal(await page.locator('#step-result').textContent(),d.steps[i].result);
        assert.equal(await page.locator('[data-step][aria-pressed=true]').count(),1);
        assert.equal(await page.locator('#previous-step').isDisabled(),i===0);
        assert.equal(await page.locator('#next-step').isDisabled(),i===d.steps.length-1);
        const bounds=await page.evaluate(()=>{
          const svg=document.querySelector('#drawing svg');
          return [...svg.querySelectorAll('text')].map(t=>({text:t.textContent,box:t.getBBox()})).filter(t=>t.box.x<0||t.box.x+t.box.width>700||t.box.y<0||t.box.y+t.box.height>440).map(t=>t.text);
        });
        assert.deepEqual(bounds,[],d.id+' step '+i+': SVG text outside view');
        await page.screenshot({path:path.join(output,d.id+'-'+i+'.png'),fullPage:true});
        states++;
      }
      await page.reload();
      assert.equal(await page.locator('#step-result').textContent(),d.steps.at(-1).result);
      await page.locator('#previous-step').click();
      assert.equal(await page.locator('#step-result').textContent(),d.steps.at(-2).result);
      await page.goBack();
      assert.equal(await page.locator('#step-result').textContent(),d.steps.at(-1).result);
      await page.locator('#open-syntax').click();
      assert.equal(await page.locator('#syntax-dialog').evaluate(el=>el.open),true);
      const source=(await fs.readFile(path.join(web,'syntax/fixtures',d.fixture),'utf8')).replace(/\r\n/g,'\n');
      assert.equal(await page.locator('#syntax-code').textContent(),source);
      await page.locator('#copy-syntax').click();
      assert.equal((await page.evaluate(()=>navigator.clipboard.readText())).replace(/\r\n/g,'\n'),source);
      if(d.id==='curve-replay')await page.screenshot({path:path.join(output,'syntax-popup.png'),fullPage:true});
      await page.keyboard.press('Escape');
      assert.equal(await page.locator('#open-syntax').evaluate(el=>el===document.activeElement),true);
      await page.locator('#open-syntax').click();
      await page.locator('#close-syntax').click();
      await page.setViewportSize({width:390,height:844});
      for(let i=0;i<d.steps.length;i++){
        await page.locator('[data-step="'+i+'"]').click();
        assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),true,d.id+' mobile width');
      }
      await page.screenshot({path:path.join(output,d.id+'-mobile.png'),fullPage:true});
      await page.locator('#open-syntax').click();
      assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),true,d.id+' mobile dialog width');
      await page.keyboard.press('Escape');
      await page.setViewportSize({width:1440,height:1100});
    }
    for(const d of collection.walkthroughs){
      for(const suffix of ['', '/', '/index.html']){
        await page.goto(base+demoRoute(d).slice(0,-1)+suffix+'?step=1');
        assert.equal(await page.locator('.notebook-nav a').first().getAttribute('href'),'/syntax/');
        assert.ok(await page.locator('svg').first().innerHTML());
        for(const link of await page.locator('a[href]').evaluateAll(nodes=>nodes.map(n=>n.href))){
          if(new URL(link).origin!==base)continue;
          const response=await page.request.get(link);
          assert.equal(response.status(),200,link);
          if(new URL(link).pathname.endsWith('.gojo'))assert.match(await response.text(),/^gojo 1;/);
        }
      }
      for(const [name,width,height] of [['desktop',1440,900],['phone',390,844]]){
        await page.setViewportSize({width,height});
        await page.goto(base+demoRoute(d));
        for(let step=0;step<d.stepCount;step++){
          if(d.id==='curve-replay')await page.locator('[data-step="'+step+'"]').click();
          else{
            await page.locator('#step').fill(String(step));
            assert.equal(await page.locator('#position').innerText(),(step+1)+' / '+d.stepCount);
          }
          assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true,d.id+' '+name+' step '+step+' width');
        }
        const syntax=page.locator('details').filter({has:page.locator('#syntax')});
        await syntax.locator('summary').click();
        assert.ok(await page.locator('#syntax').isVisible(),d.id+' syntax');
        assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true,d.id+' expanded syntax width');
        await syntax.locator('summary').click();
        if(d.id==='grounded-affine-basis'){
          await page.locator('#step').fill('7');
          for(let turn=0;turn<4;turn++){
            const points=()=>page.locator('#scene circle').evaluateAll(nodes=>nodes.map(n=>[+n.getAttribute('cx'),+n.getAttribute('cy')]));
            const front=await points(),numeric=await page.locator('#numeric').innerText();
            await page.locator('#mirror').click();
            const back=await points();
            assert.deepEqual(back,front.map(([x,y])=>[440-x,y]),'Back view preserves the viewer horizon');
            assert.equal(await page.locator('#numeric').innerText(),numeric);
            await page.locator('#mirror').click();await page.locator('#rotate').click();
          }
        }
        await page.screenshot({path:path.join(output,d.id+'-walkthrough-'+name+'.png'),fullPage:true});
      }
    }
    await page.setViewportSize({width:390,height:844});
    await page.goto(base+'/syntax/');
    assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),true);
    await page.screenshot({path:path.join(output,'gallery-mobile.png'),fullPage:true});
    await page.goto(base+'/');
    assert.ok(await page.locator('.syntax-entry').isVisible());
    await page.locator('.syntax-entry').click();
    assert.equal(new URL(page.url()).pathname,'/syntax/');
    // All copied syntax and every page/thumbnail are actual local resources.
    for(const d of catalogue.demos){
      for(const suffix of [d.id+'/','thumbnails/'+d.id+'.svg','fixtures/'+d.fixture]){
        const response=await page.request.get(base+'/syntax/'+suffix);assert.equal(response.status(),200,suffix);
      }
    }
    await page.goto(base+'/syntax/curve-replay/?step=999');
    assert.equal(await page.locator('[data-step="0"]').getAttribute('aria-pressed'),'true');
    assert.deepEqual(errors,[]);
    console.log('Browser QA passed: '+entries.size+' indexed examples, '+collection.walkthroughs.length+' full walkthroughs, '+states+' overview steps; directory/index variants, featured order, compact extras, loaded assets, desktop/mobile layouts, view invariance, filters, syntax and home link. Screenshots: '+output);
  }finally{
    if(browser)await browser.close();
    await new Promise(resolve=>server.close(resolve));
  }
})().catch(error=>{console.error(error);process.exitCode=1;});
