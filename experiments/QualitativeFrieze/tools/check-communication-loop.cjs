// Verify presentation against captured native experiment diagnostics.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');
const { gunzipSync } = require('node:zlib');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');

const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/communication-loop/gallery.html'));
const directory = path.dirname(gallery);
const screenshots = path.resolve(process.env.GOJO_SCREENSHOTS || 'artifacts/communication-loop-qa');
const references = new Set();
const errors = [];

async function assets(page) {
  await page.waitForFunction(() => [...document.images].every(img => img.complete && img.naturalWidth > 0));
  const links = await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element =>
    ['href', 'src'].filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))));
  for (const raw of links) {
    const url = new URL(raw, page.url());
    if (url.protocol !== 'file:' || references.has(url.href)) continue;
    const info = await fs.stat(fileURLToPath(url));
    assert.ok(info.isFile() && info.size > 0, 'Missing or empty asset: ' + url.href);
    references.add(url.href);
  }
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
    'Horizontal overflow at ' + JSON.stringify(page.viewportSize()));
}

(async () => {
  const model = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(model.schema, 'communication-loop-v1');
  assert.equal(model.cases.length, 3);
  assert.equal(model.trials, 6);
  assert.equal(model.librarySize, 14);
  assert.equal(model.beforeSatisfied, 0);
  assert.equal(model.afterSatisfied, 6);
  assert.equal(model.nativeNumericElements, 0);
  assert.equal(model.archiveRoundTrip, true);
  assert.equal(model.ablations.reduce((sum, item) => sum + item.options, 0), 16);
  for (const item of model.ablations) {
    assert.equal(item.minimumHints, 1);
    assert.equal(item.all.length, item.options);
    assert.ok(item.best.length > 0);
    assert.ok(item.all.some(option => !option.robust));
    for (const best of item.best) {
      assert.equal(best.hints, 1);
      assert.equal(best.admissible, 2);
      assert.equal(best.robust, true);
    }
    for (const option of item.all)
      if (option.robust) assert.ok(option.admissible > 0, 'Empty acceptance cannot count as robust');
  }
  assert.ok(JSON.parse(gunzipSync(await fs.readFile(path.join(directory, 'communication.gojo.json.gz')))));
  for (const item of model.cases) {
    assert.equal(item.trials.length, 2);
    for (const trial of item.trials) {
      assert.equal(trial.before.read.satisfied, false);
      assert.equal(trial.after.read.satisfied, true);
      assert.equal(trial.after.sentHints, trial.before.sentHints + 1);
      assert.equal(trial.after.admissible, 2);
      assert.ok(trial.before.admissible > trial.after.admissible);
      assert.ok(trial.after.patterns.some(pattern => pattern.segments > 1));
      for (const sample of [trial.before, trial.after]) {
        assert.equal(sample.examined, 14);
        const svg = await fs.readFile(path.join(directory, sample.image), 'utf8');
        assert.match(svg, /<svg\b/);
        assert.doesNotMatch(svg, /NaN|Infinity/);
      }
    }
  }
  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  let selections = 0;
  try {
    await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1440, height: 1050 }, reducedMotion: 'reduce' });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => errors.push(request.url() + ': ' + request.failure()?.errorText));
    await page.goto(pathToFileURL(gallery).href);
    assert.deepEqual(JSON.parse(await page.locator('#data').textContent()), model);
    assert.equal(await page.locator('article.case').count(), 3);
    await assets(page);
    for (const item of model.cases) {
      const card = page.locator(`article[data-case="${item.id}"]`);
      for (let index = 0; index < item.trials.length; index++) {
        await card.locator('select').selectOption(String(index));
        const trial = item.trials[index];
        assert.equal(await card.locator('.before img').getAttribute('src'), trial.before.image);
        assert.equal(await card.locator('.after img').getAttribute('src'), trial.after.image);
        assert.equal(await card.locator('.before').getAttribute('data-satisfied'), 'false');
        assert.equal(await card.locator('.after').getAttribute('data-satisfied'), 'true');
        assert.equal(await card.locator('.after .patterns li').count(), trial.after.patterns.length);
        assert.equal(await card.locator('.before .patterns li').count(), trial.before.patterns.length);
        assert.equal(await card.locator('.before .sent-patterns').textContent(), trial.before.sentPatterns.join(' · '));
        assert.equal(await card.locator('.after .sent-patterns').textContent(), trial.after.sentPatterns.join(' · '));
        await assets(page); selections++;
      }
      await card.locator('select').selectOption('0');
      const ablation = model.ablations.find(option => option.case === item.id);
      assert.equal(await card.locator('.ablation tbody tr').count(), ablation.options);
    }
    await page.screenshot({ path: path.join(screenshots, 'gallery-desktop.png'), fullPage: true });
    await page.locator('article.case').nth(1).screenshot({ path: path.join(screenshots, 'closure-case.png') });
    await page.setViewportSize({ width: 390, height: 844 });
    await assets(page);
    await page.screenshot({ path: path.join(screenshots, 'gallery-mobile.png'), fullPage: true });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href);
    await assets(page); assert.equal(await page.locator('article').count(), 6);
    await page.setViewportSize({ width: 1440, height: 1050 });
    await assets(page);
    await page.screenshot({ path: path.join(screenshots, 'overview-desktop.png'), fullPage: true });
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({ cases: model.cases.length, selections, localReferences: references.size,
      browserErrors: errors.length, screenshots }, null, 2));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
