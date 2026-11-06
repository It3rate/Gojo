// Exercise the keyboard controls in the compiled explorer and verify its notes.
const { chromium } = require('playwright');
const { createHash } = require('node:crypto');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const { serve, firstSelectorY } = require('./explorer-browser-harness.cjs');
const root = path.resolve(process.env.GOJO_WEB_ROOT || path.join(__dirname, '../publish-deploy/syntax-site'));
const output = path.resolve(process.env.GOJO_QA_OUTPUT || path.join(require('node:os').tmpdir(), 'gojo-choice-browser'));

(async () => {
  await fs.mkdir(output, {recursive:true});
  let server, browser;
  try {
    const base = process.env.GOJO_BASE_URL || ('http://127.0.0.1:' + (server = await serve(root)).address().port);
    browser = await chromium.launch({headless:true, channel:'msedge'});
    const page = await browser.newPage({viewport:{width:1530,height:980}});
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto(base + '/encoding/explorer/?layer=46');
    await page.waitForSelector('canvas', {timeout:90000});
    assert.equal(await page.evaluate(() => globalThis.initialEncodingLayer()), 46);
    let y;
    for (let i=0; i<80 && !y; i++) { await page.waitForTimeout(500); y = await firstSelectorY(page); }
    assert.ok(y, 'Trial selector rendered');
    async function focus(index) {
      await page.mouse.click(330,y);
      await page.keyboard.press('Escape');
      for(let i=0;i<index;i++) await page.keyboard.press('Tab');
    }
    async function act(index) {
      await focus(index); await page.keyboard.press('Space');
      await page.mouse.move(1520,970); await page.waitForTimeout(220);
    }
    async function hash(x,y,width,height) {
      return createHash('sha256').update(await page.screenshot({clip:{x,y,width,height}})).digest('hex');
    }
    async function capture(name) { await page.screenshot({path:path.join(output,name+'.png')}); }
    const initial = await hash(265,385,350,320);
    await capture('ready');
    await focus(0);
    const firstSelector = await hash(265,y-8,168,18);
    let count = 0;
    for (let scenario=0; scenario<20; scenario++) {
      await act(1);
      assert.notEqual(await hash(265,385,350,320),initial,'Trial produces recorded reads');
      await capture('trial-'+scenario+'-last');
      await focus(1); await page.keyboard.press('Home'); await page.keyboard.press('Escape');
      await capture('trial-'+scenario+'-first');
      await page.keyboard.press('ArrowRight');
      await page.keyboard.press('ArrowLeft');
      await act(3);
      assert.equal(await hash(265,385,350,320),initial,'Reset withdraws the trace');
      count++;
      await focus(0);
      await page.keyboard.press('ArrowDown');
      await page.waitForTimeout(180);
      if (await hash(265,y-8,168,18) === firstSelector) break;
    }
    assert.ok(count >= 9 && count < 20, 'All scenarios including the priority trial were browsable');
    const popupEvent = page.waitForEvent('popup',{timeout:15000});
    await page.mouse.click(1430,905);
    const popup = await popupEvent; await popup.waitForLoadState();
    assert.match(popup.url(), /TensionChoice\.html$/);
    const html = await (await page.request.get(popup.url())).text();
    assert.equal(/href="[^"]+\.md(?:#|")/i.test(html),false);
    const assets = new Set([...html.matchAll(/(?:href|src)="(assets\/[^"]+)"/g)].map(match=>match[1]));
    for(const asset of assets) {
      const response=await page.request.get(base+'/encoding/notes/'+asset);
      assert.equal(response.status(),200,asset);
      assert.deepEqual(await response.body(),await fs.readFile(path.join(root,'encoding/notes',decodeURIComponent(asset))));
    }
    assert.deepEqual(errors,[]);
    console.log('Layer 46 verified: '+count+' trials, turn controls, reset, notes and '+assets.size+' downloads. '+output);
  } finally { await browser?.close(); server?.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
