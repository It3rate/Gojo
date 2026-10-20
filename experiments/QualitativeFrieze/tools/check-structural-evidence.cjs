const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/structural-evidence/gallery.html'));
const directory = path.dirname(gallery), screenshots = path.resolve('artifacts/structural-evidence-qa');

(async () => {
  const model = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(model.schema, 'structural-evidence-v1');
  assert.equal(model.cases.length, 6); assert.equal(model.episodes.length, 27);
  assert.equal(model.nativeNumericElements, 0); assert.equal(model.archiveRoundTrip, true);
  assert.equal(model.policy.structuralTrainingEpisodes, 0);
  const old = JSON.parse(await fs.readFile(path.join(directory, '../evidence-selection/diagnostics.json'), 'utf8'));
  assert.deepEqual(model.policy.coefficients, old.training.coefficients);
  for (const summary of model.summaries) { assert.equal(summary.resolved, 5); assert.equal(summary.open, 1); }
  const transferred = model.episodes.filter(e => e.policy === 'transferred' && e.control === null);
  assert.equal(transferred.filter(e => e.correctAtVocabulary).length, 5);
  const ambiguous = transferred.find(e => e.caseId === 'S06');
  assert.equal(ambiguous.correctAmbiguity, true); assert.equal(ambiguous.stages.at(-1).eligible.length, 2);
  const controls = Object.fromEntries(model.episodes.filter(e => e.control).map(e => [e.control, e]));
  assert.ok(controls.unsearched.stages.slice(1).every(s => s.answer === 'Open'));
  assert.ok(controls.conflict.hasConflict); assert.equal(controls.conflict.stages.at(-1).eligible.length, 0);
  assert.ok(controls.incapable.hasBlocked); assert.equal(controls.incapable.requests, 0);
  const crossing = await fs.readFile(path.join(directory, 'S02-source.svg'), 'utf8');
  assert.match(crossing, /class='declared-crossing'/);
  const fragments = await fs.readFile(path.join(directory, 'S04-candidate-0.svg'), 'utf8');
  assert.match(fragments, /class='separate-sites'/);
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
        const info = await fs.stat(fileURLToPath(url)); assert.ok(info.isFile() && info.size > 0); references.add(url.href);
      }
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
    }
    await page.goto(pathToFileURL(gallery).href); await check();
    assert.equal(await page.locator('#cases article').count(), 6);
    assert.equal(await page.locator('#controls article').count(), 3);
    for (const example of model.cases) {
      const article = page.locator(`#cases article[data-case="${example.id}"]`);
      for (const episode of model.episodes.filter(e => e.caseId === example.id && e.control === null)) {
        await article.locator('select').selectOption(episode.id);
        for (let index = 0; index < episode.stages.length; index++) {
          await article.locator(`[data-step="${index}"]`).click();
          assert.equal(await article.getAttribute('data-stage'), String(index));
          const stage = episode.stages[index];
          for (const candidate of example.candidates) {
            const card = article.locator(`[data-candidate="${candidate.index}"]`);
            assert.equal(await card.evaluate(e => e.classList.contains('rejected')), !stage.eligible.includes(candidate.index));
          }
        }
      }
      await article.locator('select').selectOption(example.id + '-transferred');
      await article.locator('[data-step]').last().click();
    }
    await check();
    await page.screenshot({ path: path.join(screenshots, 'gallery-desktop.png'), fullPage: true });
    await page.locator('#cases article[data-case="S02"]').screenshot({ path: path.join(screenshots, 'crossing-question.png') });
    await page.locator('#cases article[data-case="S04"]').screenshot({ path: path.join(screenshots, 'continuity-question.png') });
    await page.locator('#cases article[data-case="S06"]').screenshot({ path: path.join(screenshots, 'recipe-ambiguity.png') });
    for (const details of await page.locator('details').all()) await details.locator('summary').click();
    await check();
    await page.setViewportSize({ width: 390, height: 844 }); await check();
    await page.screenshot({ path: path.join(screenshots, 'gallery-mobile.png'), fullPage: true });
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({ cases: 6, episodes: 27, controls: 3, policies: 4,
      localReferences: references.size, browserErrors: errors.length, screenshots }, null, 2));
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
