// Exercise the actual Avalonia browser canvas and native controls, then inspect
// captured frames alongside the exhaustive C# presentation/primitive tests.
const { chromium } = require('playwright');
const fs = require('node:fs/promises'), path = require('node:path');
const http = require('node:http'), assert = require('node:assert/strict');
const web = path.resolve(__dirname, '../publish-deploy/syntax-site');
(async () => {
  const server = http.createServer(async (req, res) => {
    try {
      let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
      if (!file.startsWith(web + path.sep)) throw Error('Outside stage');
      if ((await fs.stat(file)).isDirectory()) file = path.join(file, 'index.html');
      res.setHeader('Content-Type', ({ '.js': 'text/javascript', '.wasm': 'application/wasm', '.html': 'text/html', '.css': 'text/css', '.json': 'application/json' })[path.extname(file)] || 'application/octet-stream');
      res.end(await fs.readFile(file));
    } catch { res.writeHead(404); res.end(); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  let browser;
  try {
    browser = await chromium.launch({ headless: true, channel: 'msedge' });
    const page = await browser.newPage({ viewport: { width: 1530, height: 980 } });
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    const base = process.env.GOJO_BASE_URL || 'http://127.0.0.1:' + server.address().port;
    const output = process.env.GOJO_CAPTURE_DIR || require('os').tmpdir(); await fs.mkdir(output, { recursive: true });
    await page.goto(base + '/encoding/explorer/?layer=16');
    await page.waitForSelector('canvas', { timeout: 90000 }); await page.waitForTimeout(12000);
    async function select(x, count) {
      await page.mouse.click(x, 254); await page.keyboard.press('Home');
      for (let i = 0; i < count; i++) await page.keyboard.press('ArrowDown');
      await page.keyboard.press('Enter'); await page.waitForTimeout(250);
    }
    await select(330, 2); // reverse the middle use
    await page.screenshot({ path: path.join(output, 'browser-boundary.png') });
    const popupPromise = page.waitForEvent('popup'); await page.mouse.click(1430, 902);
    const popup = await popupPromise; await popup.waitForLoadState();
    assert.match(await popup.title(), /Closure, claims/);
    assert.equal(await popup.locator('table').count() >= 4, true);
    await popup.close();
    await page.goto(base + '/encoding/explorer/?layer=17');
    await page.waitForSelector('canvas', { timeout: 90000 }); await page.waitForTimeout(5000);
    await page.screenshot({ path: path.join(output, 'browser-cycle-initial.png') });
    await select(590, 2); // uncertain closing observation
    for (let i = 0; i < 4; i++) { await page.mouse.click(735, 244); await page.waitForTimeout(500); }
    await page.screenshot({ path: path.join(output, 'browser-cycle.png') });
    await page.goto(base + '/encoding/explorer/?layer=18');
    await page.waitForSelector('canvas', { timeout: 90000 }); await page.waitForTimeout(5000);
    await select(330, 2); await select(470, 1);
    await page.mouse.click(630, 254); await page.waitForTimeout(1800);
    await page.screenshot({ path: path.join(output, 'browser-budget.png') });
    assert.deepEqual(errors, []);
    console.log('Boundary deep link, HTML notes, cycle controls and measured read executed without browser errors. Frames: ' + output);
  } finally { await browser?.close(); server.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
