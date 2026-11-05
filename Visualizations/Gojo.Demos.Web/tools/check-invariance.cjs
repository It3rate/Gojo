// Portable browser QA for the retained Primitive Explorer experiment.
// Use the existing isolated Playwright installation through NODE_PATH.
const { chromium } = require('playwright');
const fs = require('node:fs/promises'), path = require('node:path'), http = require('node:http');
const assert = require('node:assert/strict');
const root = path.resolve(process.env.GOJO_WEB_ROOT || path.join(__dirname, '../publish-deploy/syntax-site'));
const output = path.resolve(process.env.GOJO_QA_OUTPUT || path.join(require('node:os').tmpdir(), 'gojo-invariance-browser'));
(async () => {
  await fs.mkdir(output, { recursive: true });
  const server = http.createServer(async (req, res) => {
    try {
      let file = path.resolve(root, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
      if (!file.startsWith(root + path.sep)) throw Error('Outside site');
      if ((await fs.stat(file)).isDirectory()) file = path.join(file, 'index.html');
      res.setHeader('Content-Type', ({'.js':'text/javascript','.wasm':'application/wasm','.html':'text/html',
        '.css':'text/css','.json':'application/json','.svg':'image/svg+xml','.csv':'text/csv'})[path.extname(file)] || 'application/octet-stream');
      res.end(await fs.readFile(file));
    } catch { res.writeHead(404); res.end(); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  let browser;
  try {
    browser = await chromium.launch({headless:true, channel:'msedge'});
    const page = await browser.newPage({viewport:{width:1530,height:980}});
    const errors = []; page.on('pageerror', error => errors.push(error.message));
    const base = process.env.GOJO_BASE_URL || 'http://127.0.0.1:' + server.address().port;
    await page.goto(base + '/encoding/explorer/?layer=44');
    await page.waitForSelector('canvas', {timeout:90000});
    await page.waitForTimeout(12000);
    assert.equal(await page.evaluate(() => globalThis.initialEncodingLayer()), 44);
    async function shot(name) {
      await page.mouse.move(1520,970); await page.waitForTimeout(120);
      await page.screenshot({path:path.join(output, name + '.png')});
    }
    await shot('initial');
    if (process.env.GOJO_SHOT_ONLY) { assert.deepEqual(errors,[]); return; }
    async function focusSelector(index) {
      await page.mouse.click(330,262); await page.keyboard.press('Escape');
      for (let i=0;i<index;i++) await page.keyboard.press('Tab');
    }
    async function select(index,value) {
      await focusSelector(index);
      if (index) await page.keyboard.press('Space');
      await page.keyboard.press('Home');
      for (let i=0;i<value;i++) { await page.keyboard.press('ArrowDown'); await page.waitForTimeout(45); }
      if (index) await page.keyboard.press('Enter');
      await page.keyboard.press('Escape'); await page.waitForTimeout(120);
    }
    async function action(index=0) {
      await focusSelector(1);
      for (let i=0;i<=index;i++) await page.keyboard.press('Tab');
      await page.keyboard.press('Space'); await page.waitForTimeout(180);
    }
    for (let specimen=0;specimen<12;specimen++) {
      await select(0,specimen);
      for (let variation=0;variation<3;variation++) {
        await select(1,variation); await action(); await shot(`case-${specimen}-${variation}`);
      }
      await action(1);
      console.log('Exercised concept '+specimen+': preserving, challenge, concealed, reset.');
    }
    await select(0,5); await action(); await shot('switch-reuse');
    const popupPromise = page.waitForEvent('popup',{timeout:15000});
    await page.mouse.click(1430,905);
    const popup = await popupPromise; await popup.waitForLoadState();
    assert.match(popup.url(), /InvarianceExperiments\.html/);
    assert.match(await popup.title(), /Small concepts under change/i);
    const html = await (await page.request.get(base+'/encoding/notes/InvarianceExperiments.html')).text();
    assert.equal(/href="[^\"]+\.md(?:"|#)/.test(html),false);
    const assets = [...new Set([...html.matchAll(/(?:href|src)="(assets\/[^\"]+)"/g)].map(match=>match[1]))];
    for (const asset of assets) {
      const response = await page.request.get(base+'/encoding/notes/'+asset);
      assert.equal(response.status(),200,asset);
      assert.deepEqual(await response.body(),await fs.readFile(path.join(root,'encoding/notes',decodeURIComponent(asset))));
    }
    assert.deepEqual(errors,[]);
    console.log(`Layer44 loaded; 12 cases x3 variations and resets exercised; matching HTML notes and ${assets.length} assets verified. No page errors. Screenshots: ${output}`);
  } finally { await browser?.close(); server.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
