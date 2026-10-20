const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/evidence-selection/gallery.html'));
const directory = path.dirname(gallery), screenshots = path.resolve('artifacts/evidence-selection-qa');

(async () => {
  const model = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(model.schema, 'evidence-selection-v1');
  assert.equal(model.nativeNumericElements, 0);
  assert.ok(model.archiveRoundTrip && model.policyReconstructedFromArchive);
  assert.equal(model.training.coefficients.length, 5);
  assert.ok(model.training.coefficients.every(Number.isFinite));
  assert.ok(model.training.rows > 0 && model.costs.elapsedMilliseconds > 0);
  const policies = Object.fromEntries(model.summaries.map(s => [s.policy, s]));
  assert.equal(Object.keys(policies).length, 4);
  for (const summary of model.summaries) assert.equal(summary.episodes, model.options.evaluationEpisodes);
  assert.ok(policies.learned.identified > policies.fixed.identified);
  const training = model.metrics.filter(m => m.split === 'training');
  const evaluation = model.metrics.filter(m => m.split === 'held-out');
  assert.deepEqual([...new Set(training.map(m => m.family))].sort(), ['period-four', 'single-exception']);
  assert.deepEqual([...new Set(evaluation.map(m => m.family))].sort(), ['contiguous-block', 'reflected-pair']);
  assert.equal(evaluation.length, model.options.evaluationEpisodes * 4);
  const controls = Object.fromEntries(model.metrics.filter(m => !['training', 'held-out'].includes(m.split)).map(m => [m.split, m]));
  assert.equal(controls.open.compatible, 4); assert.equal(controls.open.identified, false);
  assert.ok(controls.conflict.conflictCount > 0); assert.equal(controls.conflict.identified, false);
  assert.ok(controls.incapable.blockedCount > 0); assert.equal(controls.incapable.compatible, 0);
  assert.equal(controls.unavailable.identified, false); assert.equal(controls.unavailable.uniqueCompatible, true);
  const archive = await fs.open(path.join(directory, 'evidence.gojo.json.gz'));
  try { const bytes = Buffer.alloc(2); await archive.read(bytes, 0, 2, 0); assert.deepEqual([...bytes], [31, 139]); }
  finally { await archive.close(); }
  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const references = new Set(), errors = [];
  try {
    await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1050 } });
    page.on('pageerror', e => errors.push(e.message));
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    page.on('requestfailed', r => errors.push(r.url()));
    async function check() {
      await page.waitForFunction(() => [...document.images].every(i => i.complete && i.naturalWidth > 0));
      for (const raw of await page.locator('[href],[src]').evaluateAll(es => es.flatMap(e =>
        ['href', 'src'].filter(k => e.hasAttribute(k)).map(k => e.getAttribute(k))))) {
        const url = new URL(raw, page.url());
        if (url.protocol !== 'file:' || references.has(url.href)) continue;
        assert.ok((await fs.stat(fileURLToPath(url))).isFile()); references.add(url.href);
      }
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
    }
    await page.goto(pathToFileURL(gallery).href); await check();
    assert.deepEqual(JSON.parse(await page.locator('#data').textContent()), model);
    assert.equal(await page.locator('[data-episode]').count(), model.episodes.length);
    assert.equal(await page.locator('#summary tr').count(), 4);
    await page.screenshot({ path: path.join(screenshots, 'gallery-desktop.png'), fullPage: true });
    await page.locator('[data-episode="held-out-learned-000"]').screenshot({ path: path.join(screenshots, 'learned-episode.png') });
    await page.locator('[data-episode="unavailable-learned-000"]').screenshot({ path: path.join(screenshots, 'false-singleton.png') });
    for (const details of await page.locator('details').all()) await details.locator('summary').click();
    await check();
    await page.setViewportSize({ width: 390, height: 844 }); await check();
    await page.screenshot({ path: path.join(screenshots, 'gallery-mobile.png'), fullPage: true });
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({ episodes: model.episodes.length, outputs: model.auditOutputs,
      localReferences: references.size, browserErrors: errors.length, screenshots }, null, 2));
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
