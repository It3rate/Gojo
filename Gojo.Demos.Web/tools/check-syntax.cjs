// Browser smoke/visual QA. Set GOJO_PLAYWRIGHT_MODULE to a bundled Playwright
// package if it is not installed locally; GOJO_BROWSER can select installed Edge.
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'playwright');
const fs=require('node:fs/promises'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const web=path.resolve(__dirname,'..');
const output=process.env.GOJO_SCREENSHOTS || path.join(require('node:os').tmpdir(),'GojoSyntaxGalleryQA');
const types={'.html':'text/html','.js':'text/javascript','.css':'text/css','.svg':'image/svg+xml','.gojo':'text/plain'};
(async()=>{
  const catalogue=JSON.parse(await fs.readFile(path.join(__dirname,'syntax-catalog.json'),'utf8'));
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
  const base='http://127.0.0.1:'+server.address().port;
  let browser;
  try{
    await fs.mkdir(output,{recursive:true});
    browser=await chromium.launch({headless:true,executablePath:process.env.GOJO_BROWSER});
    const context=await browser.newContext({viewport:{width:1440,height:1100},reducedMotion:'reduce'});
    await context.grantPermissions(['clipboard-read','clipboard-write'],{origin:base});
    const page=await context.newPage(),errors=[];
    page.on('pageerror',e=>errors.push(e.message));
    page.on('response',r=>{if(r.status()>=400&&r.url().startsWith(base))errors.push(r.status()+' '+r.url());});
    await page.goto(base+'/syntax/');
    assert.equal(await page.locator('.demo-card').count(),12);
    await page.screenshot({path:path.join(output,'gallery-desktop.png'),fullPage:true});
    for(const category of ['Structure','Uncertainty','Change','All']){
      await page.locator('[data-filter="'+category+'"]').click();
      assert.equal(await page.locator('.demo-card:visible').count(),category==='All'?12:catalogue.demos.filter(d=>d.category===category).length);
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
    await page.setViewportSize({width:390,height:844});
    await page.goto(base+'/syntax/');
    assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),true);
    await page.screenshot({path:path.join(output,'gallery-mobile.png'),fullPage:true});
    // All copied syntax and every page/thumbnail are actual local resources.
    for(const d of catalogue.demos){
      for(const suffix of [d.id+'/','thumbnails/'+d.id+'.svg','fixtures/'+d.fixture]){
        const response=await page.request.get(base+'/syntax/'+suffix);assert.equal(response.status(),200,suffix);
      }
    }
    await page.goto(base+'/syntax/curve-replay/?step=999');
    assert.equal(await page.locator('[data-step="0"]').getAttribute('aria-pressed'),'true');
    assert.deepEqual(errors,[]);
    console.log('Browser QA passed: '+catalogue.demos.length+' demos, '+states+' steps, desktop/mobile layouts, history, filters, syntax, clipboard and keyboard focus. Screenshots: '+output);
  }finally{
    if(browser)await browser.close();
    await new Promise(resolve=>server.close(resolve));
  }
})().catch(error=>{console.error(error);process.exitCode=1;});
