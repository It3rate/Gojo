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
    const curves = process.argv.includes('--curved-capitals');
    const gap = process.argv.includes('--glyph-gap');
    const bowl = process.argv.includes('--bowl-reach');
    const glyph = process.argv.includes('--glyph-clues');
    const writing = process.argv.includes('--writing-line');
    const paired = process.argv.includes('--paired-reach');
    const rails = process.argv.includes('--rail-contacts');
    const border = process.argv.includes('--border-score');
    const washer = process.argv.includes('--washer-counter');
    const probeDesk = process.argv.includes('--washer-probes');
    const timing = process.argv.includes('--timing-sockets');
    const ornaments = process.argv.includes('--ornament-workshop');
    const hints = process.argv.includes('--drawing-hints');
    const whitespace = process.argv.includes('--whitespace');
    await page.goto(base + '/encoding/explorer/?layer=' + (whitespace ? '38&view=whitespace' : hints ? 38 : ornaments ? '37&plate=5' : timing ? 36 : probeDesk ? 35 : washer ? 34 : border ? 33 : rails ? 32 : paired ? 31 : writing ? 30 : glyph ? 29 : bowl ? 28 : gap ? 27 : curves ? 26 : straight ? 25 : capitals ? 23 : fork ? 22 : seamLadders ? 20 : pathExchange ? 19 : 16));
    await page.waitForSelector('canvas', { timeout: 90000 }); await page.waitForTimeout(12000);
    async function select(x, count) {
      await page.mouse.click(x, 254); await page.keyboard.press('Home');
      for (let i = 0; i < count; i++) await page.keyboard.press('ArrowDown');
      await page.keyboard.press('Enter'); await page.waitForTimeout(whitespace ? 1000 : 250);
    }
    if (whitespace) {
      await page.screenshot({path:path.join(output,'whitespace-initial.png')});
      const bits=()=>page.screenshot({clip:{x:280,y:360,width:330,height:140}});
      const result=()=>page.screenshot({clip:{x:262,y:883,width:990,height:40}});
      const initial=await bits(),initialResult=await result();
      async function action(x){await page.mouse.click(x,245);await page.mouse.move(1517,960);await page.waitForTimeout(600);}
      await action(935);assert.ok(!initial.equals(await bits()),'A local handoff changes the gap participation.');
      await action(1037);const even=await result();
      assert.ok(!initialResult.equals(even),'The full pair graph reaches an even allocation.');
      await page.screenshot({path:path.join(output,'whitespace-fret.png')});
      await select(645,1);await action(1037);const remainder=await result();
      assert.ok(!even.equals(remainder),'An indivisible extra step remains visible.');
      await select(645,2);await action(1037);assert.ok(!remainder.equals(await result()),'The calibrated finer run resolves the mismatch.');
      await select(645,0);await select(810,1);await action(1037);
      assert.ok(!even.equals(await result()),'A neighbor-only plateau is distinguished from global evenness.');
      await select(810,2);await action(1037);
      assert.ok(!even.equals(await result()),'An unseen gap leaves the whole-spacing read unresolved.');
      await select(810,0);await select(480,3);await action(1037);
      const pentagon=await result();
      await action(1107);const pentagonInitial=await result();
      assert.ok(!pentagon.equals(pentagonInitial),'The selected pentagon settles before reset.');
      await action(1037);assert.ok(pentagon.equals(await result()),'The same pentagon settles again after reset.');
      await page.screenshot({path:path.join(output,'whitespace-pentagon.png')});
      const opened=page.waitForEvent('popup');await page.mouse.click(1430,902);
      const notes=await opened;await notes.waitForLoadState();assert.match(await notes.title(),/Let the gaps settle/);
      assert.equal(await notes.locator('main img').count(),5);
      for(const img of await notes.locator('main img').all()){
        const svg=await notes.request.get(new URL(await img.getAttribute('src'),notes.url()).href);
        assert.equal(svg.status(),200);assert.match(await svg.text(),/data-key=/);
      }
      const href=await notes.getByRole('link',{name:'Download the 72 settlement runs',exact:true}).getAttribute('href');
      assert.equal((await (await notes.request.get(new URL(href,notes.url()).href)).text()).trim().split('\n').length,73);
      assert.deepEqual(errors,[]);
      console.log('Verified live gap transfers, even settlement, retained remainder, finer basis, local plateau, hidden read, pentagon, notes and data.');
      return;
    }
    if (hints) {
      const drawing=()=>page.screenshot({clip:{x:660,y:340,width:820,height:360}});
      const fret=await drawing();
      await page.screenshot({path:path.join(output,'drawing-hints-fret.png')});
      await select(330,1); const rectangle=await drawing();
      assert.ok(!fret.equals(rectangle),'The same rail grammar supports a different connected path.');
      await select(480,4); assert.ok(!rectangle.equals(await drawing()),'Conflicting order supplies no drawing.');
      await select(330,2);
      await page.screenshot({path:path.join(output,'drawing-hints-circle.png')});
      const circle=await drawing();
      await select(645,3); assert.ok(!circle.equals(await drawing()),'A hidden gate holds the cursor and leaves no closing chord.');
      await select(645,0); await page.waitForTimeout(600);
      await page.mouse.click(760,245); await page.mouse.move(1517,960); await page.waitForTimeout(500);
      assert.ok(!circle.equals(await drawing()),'The first gate leaves a landmark without yet drawing a chord.');
      await page.mouse.click(860,245); await page.mouse.move(1517,960); await page.waitForTimeout(250);
      assert.ok(circle.equals(await drawing()),'The whole recorded traversal restores the same rectangle.');
      const bits=await page.screenshot({clip:{x:280,y:330,width:330,height:370}});
      await page.mouse.click(970,245); await page.mouse.move(1517,960); await page.waitForTimeout(250);
      assert.ok(!circle.equals(await drawing()),'The numerical realization changes.');
      assert.ok(bits.equals(await page.screenshot({clip:{x:280,y:330,width:330,height:370}})),'The encoded structure stays unchanged.');
      await select(330,3); const separate=await drawing(); await select(480,1);
      assert.ok(!separate.equals(await drawing()),'Simultaneous gates make a different path.');
      const opened=page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes=await opened; await notes.waitForLoadState();
      assert.match(await notes.title(),/Draw from hints/);
      const images=notes.locator('main img'); assert.equal(await images.count(),8);
      for(const img of await images.all()){
        const url=new URL(await img.getAttribute('src'),notes.url()).href;
        const svg=await notes.request.get(url); assert.equal(svg.status(),200); assert.match(await svg.text(),/data-key=/);
      }
      for(const [name,lines] of [['All circular mark masks',5457],['Rail witness checks',361]]){
        const href=await notes.getByRole('link',{name,exact:true}).getAttribute('href');
        assert.equal((await (await notes.request.get(new URL(href,notes.url()).href)).text()).trim().split('\n').length,lines);
      }
      assert.deepEqual(errors,[]);
      console.log('Verified invariant encoding, varied drawings, conflicts, hidden-gate hold, playback, paired streams, notes and surveys.');
      return;
    }
    if (ornaments) {
      const plate = () => page.screenshot({clip:{x:720,y:335,width:680,height:340}});
      const opening=await plate(); await select(645,1); const both=await plate();
      assert.ok(!opening.equals(both),'The stricter crown must suppress alternate inner presses.');
      await select(645,2); assert.ok(!both.equals(await plate()),'Equal numbers of flowers occupy different windows.');
      await select(645,0); await select(480,8); const waiting=await plate();
      assert.ok(!opening.equals(waiting),'An unresolved arm must stop the plate early.');
      await select(480,0); await select(330,0);
      assert.ok(!opening.equals(await plate()),'The deep link initially opened the arcade rather than the default rosette.');
      await page.screenshot({path:path.join(output,'ornament-workshop-rosette.png')});
      await select(330,4);
      assert.ok(opening.equals(await plate()),'Selecting the arcade again restores its printed scene.');
      const opened=page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes=await opened; await notes.waitForLoadState();
      assert.match(await notes.title(),/Ornament workshop/);
      const images=notes.locator('main img'); assert.equal(await images.count(),5);
      for(const img of await images.all()){
        const url=new URL(await img.getAttribute('src'),notes.url()).href;
        const svg=await notes.request.get(url); assert.equal(svg.status(),200); assert.match(await svg.text(),/<path data-cell=/);
      }
      const csv=await notes.getByRole('link',{name:/Download the 198 print runs/}).getAttribute('href');
      assert.equal((await (await notes.request.get(new URL(csv,notes.url()).href)).text()).trim().split('\n').length,199);
      assert.deepEqual(errors,[]); console.log('Verified deep-linked plates, conditional child starts, uncertain hold, notes, vectors and sweep.');
      return;
    }
    if (timing) {
      const result = () => page.screenshot({clip:{x:262,y:882,width:990,height:38}});
      async function finish() {
        await page.mouse.click(1090,245); await page.mouse.move(1517,960); await page.waitForTimeout(500);
      }
      const initial = await result(); await finish(); const used = await result();
      assert.ok(!initial.equals(used), 'The factory learns and uses its socket.');
      await select(330,1); await finish(); const empty = await result();
      assert.ok(!used.equals(empty), 'The spare washer cannot be seen.');
      await select(330,2); await finish();
      assert.ok(used.equals(await result()), 'The border desk uses its own learned socket.');
      await page.screenshot({path:path.join(output,'timing-socket-border.png')});
      await select(480,13); await finish();
      assert.ok(empty.equals(await result()), 'A nonrepeatable split leaves the plotter socket empty.');
      await select(480,0); await select(330,0); await finish();
      await page.screenshot({path:path.join(output,'timing-socket-factory.png')});
      const opened=page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes=await opened; await notes.waitForLoadState();
      assert.match(await notes.title(),/Can a test earn a socket/);
      const archive=await notes.getByRole('link',{name:/Download the timing trials/}).getAttribute('href');
      const data=await notes.request.get(new URL(archive,notes.url()).href);
      assert.equal(data.status(),200); assert.equal((await data.body()).readUInt32LE(),0x04034b50);
      assert.deepEqual(errors,[]);
      console.log('Verified washer discovery, spare stock, border swatches, failed repeat, notes and CSV archive.');
      return;
    }
    if (probeDesk) {
      const result = () => page.screenshot({clip:{x:262,y:882,width:990,height:38}});
      async function run(n) {
        for(let i=0;i<n;i++) {
          await page.mouse.click(940,245); await page.mouse.move(1517,960);
          await page.waitForTimeout(550);
        }
      }
      const initial = await result(); await run(1);
      assert.ok(!initial.equals(await result()), 'A public sample narrows the candidate set.');
      await page.screenshot({path:path.join(output,'washer-probe-desk.png')});
      await select(330,3); await run(3); const oldRack = await result();
      await select(330,2); await run(3);
      assert.ok(!oldRack.equals(await result()), 'The feedback test separates the two chute histories.');
      await select(330,6); await run(1); const lost = await result();
      await select(330,0); await run(1);
      assert.ok(!lost.equals(await result()), 'A missing sample preserves both alternatives.');
      await select(330,7);
      for (let i = 0; i < 4; i++) {
        const before = await result(); await run(1);
        assert.ok(!before.equals(await result()), 'Each inspection sample must advance the desk.');
      }
      const conflict = await result(); await run(1);
      assert.ok(conflict.equals(await result()), 'No test can run once every design has been contradicted.');
      await page.screenshot({path:path.join(output,'washer-probe-conflict.png')});
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(), /Which test should run next/);
      const archive = await notes.getByRole('link', {name:/Download the trials and distinguishing/}).getAttribute('href');
      const data = await notes.request.get(new URL(archive,notes.url()).href);
      assert.equal(data.status(),200); assert.equal((await data.body()).readUInt32LE(),0x04034b50);
      assert.deepEqual(errors, []);
      console.log('Verified adaptive probe choice, the added socket, missing samples, inspection, notes and data.');
      return;
    }
    if (washer) {
      const result = () => page.screenshot({clip:{x:262,y:882,width:990,height:38}});
      async function advance() {
        await page.mouse.click(765,245); await page.mouse.move(1517,960); await page.waitForTimeout(250);
      }
      await advance(); const remembered = await result();
      await select(645,1); await advance();
      assert.ok(!remembered.equals(await result()), 'Serving every request changes the retry delivery.');
      await page.screenshot({path:path.join(output,'washer-counter.png')});
      await select(330,1); const silence = await result();
      await select(480,1);
      assert.ok(silence.equals(await result()), 'Both causes leave the desk with the same silent beat.');
      await select(330,2); const lampOff = await result(); await select(480,0);
      assert.ok(!lampOff.equals(await result()), 'The tray lamp resolves the first silent beat.');
      await select(330,3); await advance(); await advance(); const receipts = await result();
      await page.screenshot({path:path.join(output,'washer-probe.png')});
      await select(645,1); await advance(); await advance();
      assert.ok(!receipts.equals(await result()), 'The third beat separates the receipts while occupancy still agrees.');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(), /Did the washer drop/);
      const archive = await notes.getByRole('link', {name:/Download the 8,192 trial rows/}).getAttribute('href');
      const data = await notes.request.get(new URL(archive,notes.url()).href);
      assert.equal(data.status(),200); assert.equal((await data.body()).readUInt32LE(),0x04034b50);
      assert.deepEqual(errors, []);
      console.log('Verified physical releases, masked desk evidence, tray lamp, controlled probe, notes and trial download.');
      return;
    }
    if (border) {
      const result = () => page.screenshot({clip:{x:262,y:882,width:990,height:38}});
      const direct = await result();
      await select(480,2);
      assert.ok(!direct.equals(await result()), 'Grouped siblings change the period.');
      await page.mouse.click(765,245); await page.mouse.move(1517,960); await page.waitForTimeout(300);
      await page.screenshot({path:path.join(output,'border-streams.png')});
      await select(480,6); const rectangle = await result(); await select(480,7);
      assert.ok(!rectangle.equals(await result()), 'Sampling the opposed streams together changes the return period.');
      await select(330,1);
      const hidden = await result(); await select(480,1);
      assert.ok(hidden.equals(await result()), 'A hidden cut must supply the same evidence as hidden ink.');
      await select(645,1);
      assert.ok(!hidden.equals(await result()), 'Revealing the cut rejects the repeat.');
      await select(330,3); await select(810,1); await page.mouse.click(940,245); await page.mouse.move(1517,960);
      await page.waitForTimeout(300); await page.screenshot({path:path.join(output,'border-coverage.png')});
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(), /Read a repeating border/);
      const archive = await notes.getByRole('link', {name:/Download the two CSV/}).getAttribute('href');
      const data = await notes.request.get(new URL(archive,notes.url()).href);
      assert.equal(data.status(),200); assert.equal((await data.body()).readUInt32LE(),0x04034b50);
      assert.deepEqual(errors, []);
      console.log('Stream scheduling, withheld and cut ink, coverage controls, notes and ZIP checked.');
      return;
    }
    if (rails) {
      const result = () => page.screenshot({clip:{x:262,y:882,width:990,height:38}});
      const one = await result();
      await select(480,1);
      assert.ok(!one.equals(await result()), 'Two seats at the second socket should order the default reaches.');
      await select(330,3);
      const before = await result();
      await page.mouse.click(765,245); await page.mouse.move(1517,960); await page.waitForTimeout(400);
      assert.ok(!before.equals(await result()), 'The receiver should draw on an unresolved winner.');
      await page.screenshot({path:path.join(output,'rail-contacts.png')});
      await select(480,5);
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(), /When two rails meet/);
      assert.match(await notes.locator('main').innerText(), /C9 06/);
      assert.deepEqual(errors, []);
      console.log('Live contact controls, unresolved winner, material draw and notes checked.');
      return;
    }
    if (paired) {
      const picture = () => page.screenshot({clip:{x:670,y:390,width:780,height:265}});
      const result = () => page.screenshot({clip:{x:262,y:882,width:990,height:38}});
      const initial = await picture();
      await page.mouse.click(925,245); await page.mouse.move(1517,960); await page.waitForTimeout(400);
      assert.ok(!initial.equals(await picture()), 'The waiting pulse should reach the inbox.');
      await page.screenshot({path:path.join(output,'paired-reach.png')});
      const either = await result();
      await select(480, 1);
      assert.ok(!either.equals(await result()), 'Independent tips admit both and neither.');
      await select(480, 0); await select(330, 1); await select(645, 1);
      await page.screenshot({path:path.join(output,'paired-mark.png')});
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(), /Two reaches, one choice/);
      assert.match(await notes.locator('main').innerText(), /04 05/);
      assert.deepEqual(errors, []);
      console.log('Joint reach read, material handoff, alternative mark links and HTML notes checked.');
      return;
    }
    if (writing) {
      const capture = name => page.screenshot({path:path.join(output,name+'.png')});
      const result = () => page.screenshot({clip:{x:262,y:882,width:990,height:38}});
      const diagram = () => page.screenshot({clip:{x:834,y:416,width:460,height:210}});
      async function trace(x) {
        await page.mouse.click(x,245); await page.mouse.move(1517,960); await page.waitForTimeout(300);
      }
      const before = await result(); await trace(775); await capture('collected-Y');
      const collected = await result();
      assert.ok(!before.equals(collected),'Both contributions should reach the OR surface.');
      await select(480,1); await trace(775); await capture('one-slot-Y');
      assert.ok(!collected.equals(await result()),'Only one pulse fits the shared receiving slot.');
      await select(480,2); await select(800,1); await trace(920); await capture('switched-Y');
      await select(800,2); await capture('unseen-switch');
      await select(330,1); await select(480,1); await capture('baseline-j');
      const grounded = await result();
      await select(650,1);
      assert.ok(!grounded.equals(await result()),'Withholding the line must reopen i and j.');
      await select(650,4); await capture('hook-j');
      assert.ok(grounded.equals(await result()),'The hook should independently distinguish j.');
      await select(650,0); await select(805,1); await capture('unassigned-dot');
      assert.ok(!grounded.equals(await result()),'An unassigned dot cannot certify a dotted letter.');
      await select(805,2); await capture('swapped-dots');
      await select(330,2); await capture('open-four');
      await select(650,1); const hidden = await diagram(), family = await result();
      await select(480,1);
      assert.ok(hidden.equals(await diagram()) && family.equals(await result()),'The cut view should agree for both hands.');
      await select(650,0); await capture('closed-four');
      assert.ok(!family.equals(await result()),'Revealing the join should distinguish the hand.');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(),/One stem, two inputs/);
      assert.match(await notes.locator('main').innerText(),/03 → 14/);
      assert.deepEqual(errors,[]);
      console.log('Y collection, capacity and selection, i/j evidence, both 4 hands, and HTML notes checked.');
      return;
    }
    if (glyph) {
      const capture = name => page.screenshot({path:path.join(output,name+'.png')});
      const result = () => page.screenshot({clip:{x:262,y:882,width:1000,height:36}});
      const picture = () => page.screenshot({clip:{x:1120,y:432,width:140,height:166}});
      await capture('glyph-b');
      const full = await result();
      await select(655,8); await capture('withheld-loop');
      assert.ok(!full.equals(await result()),'Withholding closure and extension should admit p.');
      const partial = await result(), b = await picture();
      await select(815,1); await capture('proposed-p');
      assert.ok(partial.equals(await result()),'Drawing p must preserve the set of candidates.');
      assert.ok(!b.equals(await picture()),'The two completions should draw different tails.');
      await select(655,0); await select(483,4); await capture('digit-six');
      await select(483,5); await capture('digit-nine');
      await select(483,9); await capture('digit-eight');
      await select(483,11); await capture('unmatched-double-bowl');
      const rejected = await result();
      await select(655,8); await capture('false-completion');
      assert.ok(!rejected.equals(await result()),'Hiding the second loop should revive the two false completions.');
      assert.ok(partial.equals(await result()),'The held-out sample should admit the same b and p.');
      await select(330,1); await select(482,2);
      await page.mouse.click(626,245); await page.waitForTimeout(400); await capture('merge-Y');
      const merge = await result();
      await select(330,2); await capture('branch-Y');
      assert.ok(!merge.equals(await result()),'Reusing Y as a branch must change the read.');
      await select(330,5); const both = await result(); await select(658,2);
      assert.ok(!both.equals(await result()),'Reading the optional gate should narrow the prediction.');
      await page.mouse.click(783,245); await page.waitForTimeout(400); await capture('turn-T');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(),/Glyph clues and junctions/);
      assert.match(await notes.locator('main').innerText(),/03 → 06/);
      assert.deepEqual(errors,[]);
      console.log('Glyph evidence withdrawal, two drawn completions, Y routes, T gate and HTML notes checked.');
      return;
    }
    if (bowl) {
      const capture = name => page.screenshot({path:path.join(output,name+'.png')});
      const picture = () => page.screenshot({clip:{x:834,y:416,width:460,height:180}});
      const result = () => page.screenshot({clip:{x:264,y:881,width:980,height:33}});
      async function press(x) {
        await page.mouse.click(x,246); await page.mouse.move(1520,966); await page.waitForTimeout(350);
      }
      await capture('D-before');
      const d = await picture();
      await press(625); await capture('D-pushed');
      assert.ok(!d.equals(await picture()),'The adjustable and radial D should separate.');
      const widened = await picture(), free = await result();
      await select(480,1);
      assert.ok(widened.equals(await picture()),'Inspecting either D should keep both outlines.');
      assert.ok(!free.equals(await result()),'The pinned owner should report a different outcome.');
      await press(705);
      assert.ok(d.equals(await picture()),'Reset should restore the original common outline.');
      await select(330,1); await capture('B-before');
      const b = await picture();
      await press(625); await capture('B-pushed');
      assert.ok(!b.equals(await picture()),'The B tips should respond.');
      const top = await result();
      await select(480,1); await capture('B-linked');
      assert.ok(!top.equals(await result()),'The shared B should report both tips moving.');
      await select(330,2); await capture('D-ends-only');
      const bare = await picture(), waiting = await result();
      await press(462);
      assert.ok(bare.equals(await picture()),'Pushing an unreadable tip must leave the interior open.');
      assert.ok(!waiting.equals(await result()),'The waiting request should still be reported.');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(),/Push a bowl outward/);
      assert.match(await notes.locator('main').innerText(),/09 → 28/);
      assert.deepEqual(errors,[]);
      console.log('D overlay, pin, reset, independent and linked B, unreadable tip and HTML notes checked.');
      return;
    }
    if (gap) {
      const capture = name => page.screenshot({path:path.join(output,name+'.png')});
      const result = () => page.screenshot({clip:{x:264,y:881,width:980,height:33}});
      const picture = () => page.screenshot({clip:{x:834,y:405,width:460,height:245}});
      await capture('cut-in-O');
      const initial = await result();
      await select(480,2); assert.ok(!initial.equals(await result()),'Moving the cut must change the arrival.');
      const forward = await result();
      await select(630,1); assert.ok(!forward.equals(await result()),'Reversal must swap the tips.');
      await select(790,2); await capture('return-walk');
      await select(330,1); await capture('open-walk');
      await page.keyboard.press('ArrowRight'); await page.waitForTimeout(250); // D, with fewer controls
      await capture('D-ends');
      await select(490,0);
      await select(655,0);
      const shallow = await picture();
      await select(655,1); assert.ok(!shallow.equals(await picture()),'Two compatible bow depths should be visible.');
      await capture('D-bow');
      await select(490,1); await capture('D-corners');
      await select(490,4); await capture('D-straight');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(),/Across a glyph gap/);
      assert.match(await notes.locator('main').innerText(),/112/);
      assert.deepEqual(errors,[]);
      console.log('Glyph cut, reverse walk, return, D alternatives, bow depth and HTML notes checked.');
      return;
    }
    if (curves) {
      const capture = name => page.screenshot({path:path.join(output,name+'.png')});
      const witnesses = () => page.screenshot({clip:{x:265,y:886,width:980,height:25}});
      const clickTile = async (column,row) => {
        await page.mouse.click(1071+column*51,435+row*46);
        await page.waitForTimeout(220);
      };
      await capture('rounded-D');
      await select(495,0);
      const coarse = await witnesses();
      await clickTile(1,0);
      assert.ok(coarse.equals(await witnesses()),'The two D forms should agree with the interior withdrawn.');
      await select(495,1);
      assert.ok(!coarse.equals(await witnesses()),'The two interior corners should narrow the comparison.');
      await capture('angular-D');
      await clickTile(0,1); // retained circle with one arc omitted
      await select(675,1); await capture('gapped-O');
      const openInk = await witnesses();
      await select(495,2);
      assert.ok(!openInk.equals(await witnesses()),'Reading the retained radial source should narrow the compatible C constructions.');
      await capture('gapped-O-source');
      const opened = page.waitForEvent('popup'); await page.mouse.click(1430,902);
      const notes = await opened; await notes.waitForLoadState();
      assert.match(await notes.title(),/Curves in capitals/);
      assert.equal(await notes.locator('#how-to-build-each-letter ~ ul').first().locator('li').count(),17);
      await notes.close();
      await clickTile(2,1); // O
      await capture('radial-O');
      await page.keyboard.press('ArrowRight');
      await capture('two-bowls-B');
      assert.deepEqual(errors,[]);
      console.log('Curved capitals: D evidence withdrawal and restoration, C source overlay, radial O, keyboard selection and construction notes checked.');
      return;
    }
    if (straight) {
      const capture = name => page.screenshot({path:path.join(output, name + '.png')});
      const footer = () => page.screenshot({clip:{x:262,y:883,width:1060,height:45}});
      // This app renders into one canvas. Compare the visible construction
      // sentence above the ink, independently of the control that selected it.
      const recipe = () => page.screenshot({clip:{x:660,y:316,width:820,height:42}});
      async function clickLetter(column, row, offset = 0) {
        await page.mouse.click(1088 + column * 48 + offset, 433 + row * 46);
        await page.waitForTimeout(180);
        return recipe();
      }
      async function expectRecipe(expected, action) {
        for (let i = 0; i < 12; i++) {
          if (expected.equals(await recipe())) return;
          await page.waitForTimeout(120);
        }
        throw Error(action + ' did not show the expected construction.');
      }
      const a = await recipe();
      await page.mouse.move(1136, 433); // preview E while A is still selected
      await expectRecipe(a, 'Hovering another letter');
      await page.mouse.click(340, 448); // pin an encoding bit
      await expectRecipe(a, 'Pinning an encoding bit');
      const e = await clickLetter(1, 0);
      const f = await clickLetter(2, 0, 16); // white space inside the F tile
      const z = await clickLetter(1, 3);
      assert.equal(new Set([a,e,f,z].map(image => image.toString('base64'))).size, 4, 'Clicking the letters should change their recipes.');
      for (const [key, expected] of [['ArrowRight',a], ['ArrowDown',e], ['ArrowDown',f], ['ArrowLeft',e], ['ArrowUp',a], ['ArrowLeft',z]]) {
        await page.keyboard.press(key); await expectRecipe(expected, key + ' from the grid selection');
      }
      await select(328, 0); // commit in the real dropdown, then keep browsing without a click
      for (const [key, expected] of [['ArrowDown',e], ['ArrowRight',f], ['ArrowUp',e], ['ArrowLeft',a]]) {
        await page.keyboard.press(key); await expectRecipe(expected, key + ' on the closed Construction dropdown');
      }
      await page.mouse.click(328, 245); // compare live while the list is open
      await page.keyboard.press('ArrowRight'); await expectRecipe(e, 'Right in the open list');
      await page.keyboard.press('ArrowDown'); await expectRecipe(f, 'Down in the open list');
      await page.keyboard.press('Enter');
      await page.keyboard.press('ArrowLeft'); await expectRecipe(e, 'Left after committing the open list');
      await clickLetter(0, 0);
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
      assert.equal(await notes.locator('#how-to-build-each-letter ~ ul').first().locator('li').count(), 17);
      await notes.close();
      await page.setViewportSize({width:1220, height:900});
      await capture('compact-construction-guide');
      assert.deepEqual(errors, []);
      console.log('Straight capital atlas: clickable glyphs, repeated arrows, wraparound, open and closed dropdown focus, loop, crossings, ground and 17 published recipes checked.');
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
