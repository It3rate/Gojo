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
    const pathExchange = process.argv.includes('--path-exchange');
    const seamLadders = process.argv.includes('--seam-ladders');
    const fork = process.argv.includes('--fork');
    const capitals = process.argv.includes('--capitals');
    const straight = process.argv.includes('--straight-capitals');
    await page.goto(base + '/encoding/explorer/?layer=' + (straight ? 25 : capitals ? 23 : fork ? 22 : seamLadders ? 20 : pathExchange ? 19 : 16));
    await page.waitForSelector('canvas', { timeout: 90000 }); await page.waitForTimeout(12000);
    async function select(x, count) {
      await page.mouse.click(x, 254); await page.keyboard.press('Home');
      for (let i = 0; i < count; i++) await page.keyboard.press('ArrowDown');
      await page.keyboard.press('Enter'); await page.waitForTimeout(250);
    }
    if (straight) {
      const capture = name => page.screenshot({path:path.join(output, name + '.png')});
      const footer = () => page.screenshot({clip:{x:262,y:883,width:1060,height:45}});
      await select(475, 2); await capture('A-loop');
      await select(328, 9); // N
      const bound = await footer();
      await select(646, 0); await capture('unbound-N');
      assert.ok(!bound.equals(await footer()), 'Removing the convention should recover the N/Z alternatives.');
      await select(475, 1); await select(328, 13); await capture('projected-X');
      const joints = () => page.screenshot({clip:{x:650,y:345,width:835,height:366}});
      const crossing = await joints();
      await select(328, 14); await capture('joined-X');
      assert.ok(!crossing.equals(await joints()), 'Joining the two cuts must change the joint read.');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(), /Straight capital atlas/);
      assert.ok((await notes.locator('main').innerText()).includes('42 04 00'));
      assert.deepEqual(errors, []);
      console.log('Straight capital atlas: loop, ground withdrawal, separate and joined crossings, HTML chart checked.');
      return;
    }
    if (capitals) {
      const capture = name => page.screenshot({ path: path.join(output, name + '.png') });
      const footer = () => page.screenshot({ clip: { x: 262, y: 883, width: 1060, height: 45 } });
      await capture('capital-H');
      await select(328, 3);
      const unbound = await footer();
      await select(485, 1);
      await capture('rotated-N');
      assert.ok(unbound.equals(await footer()), 'Turning the illustration should preserve candidates.');
      await select(630, 0);
      await capture('grounded-N');
      assert.ok(!unbound.equals(await footer()), 'The supplied ground should change the N/Z read.');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430, 902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(), /Capitals without a construction box/);
      assert.ok((await notes.locator('main').innerText()).includes('62 EA'));
      await notes.close();
      await page.goto(base + '/encoding/explorer/?layer=24');
      await page.waitForSelector('canvas', { timeout: 90000 }); await page.waitForTimeout(5000);
      await select(330, 1);
      const partial = await footer();
      await select(536, 1); await capture('partial-E');
      assert.ok(partial.equals(await footer()), 'Switching writers should preserve the spatial result.');
      await select(330, 2); await capture('conflicting-E');
      assert.ok(!partial.equals(await footer()), 'Contradictory evidence should change the read.');
      assert.deepEqual(errors, []);
      console.log('Capital comparisons, rotation, local ground, pen history, conflict and HTML notes exercised.');
      return;
    }
    if (fork) {
      const capture = name => page.screenshot({ path: path.join(output, 'fork-' + name + '.png') });
      const footer = () => page.screenshot({ clip: { x: 262, y: 881, width: 1060, height: 44 } });
      const step = async () => {
        const before = await footer();
        for (let attempt = 0; attempt < 3; attempt++) {
          await page.mouse.click(862, 244);
          for (let poll = 0; poll < 8; poll++) {
            await page.waitForTimeout(180);
            if (!before.equals(await footer())) return;
          }
        }
        throw Error('Advance did not change the beat readout');
      };
      await capture('initial');
      const initial = await footer();
      await step(); await step();
      const refused = await footer();
      assert.ok(!initial.equals(refused), 'The stale offer should be inspected by beat 2.');
      await capture('refused');
      await step(); await step(); await step();
      await capture('arrival');
      assert.ok(!refused.equals(await footer()), 'Release and delivery should change the observed counts.');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430, 902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(), /fork with two owners/);
      assert.ok((await notes.locator('main').innerText()).includes('58 40 01'));
      assert.deepEqual(errors, []);
      console.log('Fork refusal, release, arrival and HTML encoding notes exercised. Frames in ' + output);
      return;
    }
    if (seamLadders) {
      const capture = name => page.screenshot({ path: path.join(output, name + '.png') });
      const result = () => page.screenshot({ clip: { x: 262, y: 882, width: 1080, height: 50 } });
      const original = await result();
      await select(330, 1);
      const cut = await result();
      assert.ok(!original.equals(cut), 'Adding the cut should change the room read comparison.');
      await capture('roof-cut');
      await select(330, 2);
      assert.ok(!cut.equals(await result()), 'The explicit room binding should change the result.');
      await capture('room-binding');
      await select(480, 2); // reverse the supplied sample while both rooms read M
      await capture('opposed-sample');
      await select(660, 0); // signed sum over both face walks
      await capture('combined-boundary');
      const opened = page.waitForEvent('popup');
      await page.mouse.click(1430, 902);
      const notes = await opened;
      await notes.waitForLoadState();
      assert.match(await notes.title(), /Two uses of a seam/);
      assert.ok((await notes.locator('main').innerText()).includes('20 D4 86 0A'));
      await notes.close();
      await page.goto(base + '/encoding/explorer/?layer=21');
      await page.waitForSelector('canvas', { timeout: 90000 }); await page.waitForTimeout(5000);
      await capture('stroke-completed');
      const proposed = await result();
      await select(330, 1);
      assert.ok(!proposed.equals(await result()), 'Reversing the rung should change the source and result.');
      await capture('template-gapped');
      assert.deepEqual(errors, []);
      console.log('Seam cut, room correspondence, both stroke views and HTML encoding chart exercised.');
      return;
    }
    if (pathExchange) {
      const capture = async name => page.screenshot({ path: path.join(output, 'path-exchange-' + name + '.png') });
      await capture('initial');
      await select(330, 1);
      const verdict = () => page.screenshot({ clip: { x: 262, y: 884, width: 1060, height: 40 } });
      const before = await verdict();
      const advance = async () => { await page.mouse.click(820, 244); await page.waitForTimeout(600); };
      await advance(); await capture('withdrawn');
      const withdrawn = await verdict();
      assert.ok(!before.equals(withdrawn), 'Editing B should withdraw closure');
      await advance(); await advance(); await capture('settled');
      assert.ok(!withdrawn.equals(await verdict()), 'Checking B should change the unresolved result');
      const opened = page.waitForEvent('popup');
      await page.mouse.click(1430, 902);
      const notes = await opened;
      await notes.waitForLoadState();
      assert.match(await notes.title(), /Two private paths/);
      assert.ok(await notes.locator('table').count() >= 4);
      assert.deepEqual(errors, []);
      console.log('Path exchange loaded with HTML notes. Inspect initial, withdrawn and settled frames in ' + output);
      return;
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
