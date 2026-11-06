// Exercise the compiled desktop-style canvas through real keyboard and pointer input.
// GOJO_WEB_ROOT selects a local build; GOJO_BASE_URL selects a published site.
const { chromium } = require('playwright');
const { serve, firstSelectorY } = require('./explorer-browser-harness.cjs');
const { createHash } = require('node:crypto');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(process.env.GOJO_WEB_ROOT || path.join(__dirname, '../publish-deploy/syntax-site'));
const output = path.resolve(process.env.GOJO_QA_OUTPUT || path.join(require('node:os').tmpdir(), 'gojo-race-browser'));

(async () => {
  await fs.mkdir(output, {recursive:true});
  let browser, server;
  try {
    const base = process.env.GOJO_BASE_URL || ('http://127.0.0.1:' + (server = await serve(root)).address().port);
    browser = await chromium.launch({headless:true, channel:'msedge'});
    const page = await browser.newPage({viewport:{width:1530,height:980}});
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    await page.goto(base + '/encoding/explorer/?layer=45');
    await page.waitForSelector('canvas', {timeout:90000});
    assert.equal(await page.evaluate(() => globalThis.initialEncodingLayer()), 45);
    let y = null;
    for (let poll=0; poll<60 && !y; poll++) { await page.waitForTimeout(500); y = await firstSelectorY(page); }
    assert.ok(y, 'Race selector rendered');
    async function focusFirst() {
      await page.mouse.click(330,y); await page.keyboard.press('Escape');
    }
    async function control(index) {
      await focusFirst();
      for (let i=0; i<index; i++) await page.keyboard.press('Tab');
    }
    async function select(index, value) {
      await control(index); await page.keyboard.press('Home');
      for (let i=0; i<value; i++) await page.keyboard.press('ArrowDown');
      await page.keyboard.press('Escape'); await page.waitForTimeout(150);
    }
    async function press(index) {
      await control(index); await page.keyboard.press('Space'); await page.waitForTimeout(280);
      await page.mouse.move(1520,970);
    }
    async function shot(name) {
      await page.screenshot({path:path.join(output, name + '.png')});
    }
    async function readPanel() {
      const png = await page.screenshot({clip:{x:265,y:385,width:350,height:340}});
      return createHash('sha256').update(png).digest('hex');
    }
    await shot('initial');
    const initial = await readPanel();
    for (let scenario=0; scenario<14; scenario++) {
      await select(0,scenario); await select(1,scenario === 10 ? 1 : 0);
      await press(2); // Run race; the recorded moment selector now follows the course size.
      assert.notEqual(await readPanel(), initial, 'Trial ' + scenario + ' produced an encoding');
      await shot('case-' + scenario + '-finish');
      await select(2,0); await shot('case-' + scenario + '-start');
      if ([7,10,12,13].includes(scenario)) {
        await select(2,2); await shot('case-' + scenario + '-obstacle');
        await control(2); await page.keyboard.press('End'); await page.waitForTimeout(150);
        await shot('case-' + scenario + '-last');
      }
      await press(4); // Reset after the new third selector.
      assert.equal(await readPanel(), initial, 'Reset returns to the empty encoding panel');
    }
    const opened = page.waitForEvent('popup', {timeout:15000});
    await page.mouse.click(1430,905);
    const popup = await opened; await popup.waitForLoadState();
    assert.match(popup.url(), /RaceOrder\.html$/);
    const html = await (await page.request.get(popup.url())).text();
    assert.equal(/href="[^"]+\.md(?:#|")/i.test(html), false);
    const assets = new Set([...html.matchAll(/(?:href|src)="(assets\/[^"]+)"/g)].map(match => match[1]));
    for (const asset of assets) {
      const response = await page.request.get(base + '/encoding/notes/' + asset);
      assert.equal(response.status(),200,asset);
      assert.deepEqual(await response.body(),await fs.readFile(path.join(root,'encoding/notes',decodeURIComponent(asset))));
    }
    assert.deepEqual(errors, []);
    console.log('Layer 45: 14 trials, recorded moments, reset, notes and ' + assets.size + ' downloads passed. ' + output);
  } finally { await browser?.close(); server?.close(); }
})().catch(error => { console.error(error); process.exitCode=1; });
