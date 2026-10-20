const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');
const { gunzipSync } = require('node:zlib');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/equivalence-revision/gallery.html'));
const directory = path.dirname(gallery);
const screenshots = path.resolve(process.env.GOJO_SCREENSHOTS || 'artifacts/equivalence-revision-qa');
const checked = new Set(), errors = [];

async function assets(page) {
  await page.waitForFunction(() => [...document.images].every(img => img.complete && img.naturalWidth > 0));
  const refs = await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element =>
    ['href', 'src'].filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))));
  for (const raw of refs) {
    const url = new URL(raw, page.url());
    if (url.protocol !== 'file:' || checked.has(url.href)) continue;
    const info = await fs.stat(fileURLToPath(url));
    assert.ok(info.isFile() && info.size > 0, 'Missing or empty reference: ' + url.href);
    checked.add(url.href);
  }
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
    'Horizontal overflow at ' + JSON.stringify(page.viewportSize()));
}

(async () => {
  const model = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(model.schema, 'equivalence-revision-v1');
  assert.equal(model.cases.length, 4); assert.equal(model.stages, 10);
  assert.equal(model.nativeNumericElements, 0); assert.equal(model.archiveRoundTrip, true);
  assert.ok(JSON.parse(gunzipSync(await fs.readFile(path.join(directory, 'equivalence.gojo.json.gz')))));
  const byId = Object.fromEntries(model.cases.map(item => [item.id, item]));
  const [before, reveal, next] = byId.E01.stages;
  assert.ok(before.coarseClosed && reveal.coarseClosed && next.coarseClosed);
  assert.equal(before.publications.length, 0);
  assert.equal(before.candidates.find(c => c.chosen).supported, 0, 'First guess has no source equality proof');
  assert.ok(before.candidates.find(c => c.chosen).open > 0, 'First guess remains provisional');
  assert.equal(reveal.withdrawn, 1);
  assert.notDeepEqual(before.sampleValues, reveal.sampleValues);
  assert.deepEqual(reveal.sampleValues, next.sampleValues);
  assert.equal(next.withdrawn, 0, 'Distinguishing next message prevents the same repair');
  const local = byId.E02.stages[1];
  assert.equal(local.carried, 10); assert.equal(local.withdrawn, 5);
  assert.equal(local.cycles, 3); assert.equal(local.coarseClosed, true);
  const [unresolved, routeA, routeB] = byId.E03.stages;
  assert.equal(unresolved.image, null); assert.equal(unresolved.eligible, 0);
  for (const stage of [unresolved, routeA, routeB]) {
    assert.ok(stage.conflicts > 0); assert.equal(stage.publications.length, 2);
  }
  assert.ok(routeA.image && routeB.image);
  assert.notEqual(routeA.chosen, routeB.chosen);
  assert.deepEqual(routeA.publications.map(pub => pub.working), [true, false]);
  assert.deepEqual(routeB.publications.map(pub => pub.working), [false, true]);
  const [incapable, coarse] = byId.E04.stages;
  assert.equal(incapable.image, null); assert.equal(incapable.eligible, 0);
  assert.ok(incapable.candidates.some(c => c.blocked > 0));
  assert.ok(coarse.image && coarse.coarseClosed && coarse.lowerResolution && coarse.blockedExact);
  assert.equal(coarse.publications[0].working, false, 'Fine evidence stays visible outside coarse read');
  assert.ok(coarse.candidates.find(c => c.chosen).sampleRefuted > 0,
    'A coarse icon must not silently certify fine source reconstruction');
  assert.equal(model.cases.flatMap(item => item.stages).filter(stage => stage.image).length, 8);
  for (const stage of model.cases.flatMap(item => item.stages)) if (stage.image) {
    const svg = await fs.readFile(path.join(directory, stage.image), 'utf8');
    assert.match(svg, /class='observed'/); assert.doesNotMatch(svg, /NaN|Infinity/);
  }

  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  try {
    await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1050 }, reducedMotion: 'reduce' });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => errors.push(request.url() + ': ' + request.failure()?.errorText));
    await page.goto(pathToFileURL(gallery).href); await assets(page);
    assert.deepEqual(JSON.parse(await page.locator('#data').textContent()), model);
    assert.equal(await page.locator('article.case').count(), 4);
    assert.equal(await page.locator('[data-stage]').count(), 10);
    for (const item of model.cases) for (const stage of item.stages) {
      const panel = page.locator(`[data-stage="${stage.id}"]`);
      assert.equal(await panel.getAttribute('data-output'), stage.image ? 'yes' : 'no');
      assert.equal(await panel.getAttribute('data-lower-resolution'), String(stage.lowerResolution));
      assert.equal(await panel.locator('.publication').count(), stage.publications.length);
      if (stage.image) assert.equal(await panel.locator('img').getAttribute('src'), stage.image);
    }
    await page.screenshot({ path: path.join(screenshots, 'gallery-desktop.png'), fullPage: true });
    await page.locator('article[data-case="E02"]').screenshot({ path: path.join(screenshots, 'local-exception.png') });
    for (const details of await page.locator('details.candidates').all()) await details.locator('summary').click();
    await assets(page);
    assert.equal(await page.locator('.candidate-row').count(), model.cases.flatMap(c => c.stages).reduce((sum, s) => sum + s.candidates.length, 0));
    await page.setViewportSize({ width: 390, height: 844 }); await assets(page);
    await page.screenshot({ path: path.join(screenshots, 'gallery-mobile.png'), fullPage: true });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await assets(page);
    assert.equal(await page.locator('figure').count(), 10);
    await page.setViewportSize({ width: 1500, height: 1050 }); await assets(page);
    await page.screenshot({ path: path.join(screenshots, 'overview-desktop.png'), fullPage: true });
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({ cases: 4, stages: 10, outputs: 8, localReferences: checked.size,
      browserErrors: errors.length, screenshots }, null, 2));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
