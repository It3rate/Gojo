const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/occurrence-composition/gallery.html'));
const directory = path.dirname(gallery), screenshots = path.resolve('artifacts/occurrence-composition-qa');

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(data.schema, 'occurrence-composition-v1');
  assert.equal(data.cases.length, 5); assert.equal(data.controls.length, 2);
  assert.equal(data.scaling.length, 20); assert.equal(data.nativeNumericElements, 0);
  assert.equal(data.archiveRoundTrip, true);
  assert.deepEqual(data.cases.map(c => c.stages.at(-1).compatible.length), [1, 1, 1, 1, 2]);
  assert.deepEqual(data.cases[0].stages.map(s => s.compatible.length), [3, 3, 2, 1]);
  assert.ok(data.cases[0].candidates.every(c => c.earlierVocabulary === 'Agrees'));
  assert.ok(data.controls.every(c => c.answer === 'Open'));
  assert.ok(data.scaling.every(r => r.attempts <= r.assignmentCapacity));
  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const errors = [], references = new Set();
  try {
    await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1050 } });
    page.on('pageerror', e => errors.push(e.message));
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    page.on('requestfailed', r => errors.push(r.url()));
    async function check() {
      await page.waitForFunction(() => [...document.images].every(i => i.complete && i.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'horizontal page overflow');
      for (const raw of await page.locator('[href],[src]').evaluateAll(es => es.flatMap(e =>
        ['href', 'src'].filter(k => e.hasAttribute(k)).map(k => e.getAttribute(k))))) {
        const url = new URL(raw, page.url());
        if (url.protocol !== 'file:' || references.has(url.href)) continue;
        const info = await fs.stat(fileURLToPath(url)); assert.ok(info.isFile() && info.size > 0); references.add(url.href);
      }
    }
    await page.goto(pathToFileURL(gallery).href); await check();
    assert.equal(await page.locator('#cases article').count(), 5);
    assert.equal(await page.locator('#controls article').count(), 2);
    assert.equal(await page.locator('#scaling tbody tr').count(), 20);
    for (const example of data.cases) {
      const article = page.locator(`[data-case="${example.id}"]`);
      for (const stage of example.stages) {
        await article.locator(`[data-step="${stage.index}"]`).click();
        assert.equal(await article.getAttribute('data-stage'), String(stage.index));
        if (stage.index === 0) assert.match(await article.locator('.question').textContent(), /no compound questions yet/);
        else {
          const banner = await article.locator('.question').textContent();
          assert.ok(banner.includes(stage.question));
          assert.ok(banner.includes(`Source answers: ${stage.sourceAnswer}`));
        }
        for (const read of stage.reads) {
          const card = article.locator(`[data-candidate="${read.index}"]`);
          assert.equal(await card.evaluate(e => e.classList.contains('rejected')), !read.isCompatible);
          assert.equal(await card.evaluate(e => e.classList.contains('resolved')),
            stage.index > 0 && stage.compatible.length === 1 && read.isSettled);
          const text = await card.locator('.candidate-answer').textContent();
          assert.equal(text, stage.index === 0 ? 'Not asked yet' : `This candidate answers: ${read.answer}`);
        }
        await check();
      }
      await article.screenshot({ path: path.join(screenshots, example.id + '.png') });
    }
    // The sole surviving two-loop candidate answers No to the last question.
    const survivor = page.locator('[data-case="C01"] .resolved');
    assert.equal(await survivor.count(), 1);
    assert.equal(await survivor.locator('.candidate-answer').textContent(), 'This candidate answers: No');
    const priorRejection = page.locator('[data-case="C01"] [data-candidate="0"]');
    assert.match(await priorRejection.locator('.badge').textContent(), /Ruled out at step 2/);
    assert.match(await priorRejection.locator('.reason').textContent(), /source Yes; this candidate No/);
    assert.equal(await page.locator('[data-case="C05"] .resolved').count(), 0);
    const unjoined = await fs.readFile(path.join(directory, 'C04-candidate-0.svg'), 'utf8');
    assert.match(unjoined, /class='separate-sites'/);
    await page.screenshot({ path: path.join(screenshots, 'desktop.png'), fullPage: true });
    await page.goto(pathToFileURL(gallery).href + '?case=C04');
    assert.ok(await page.locator('[data-case="C04"]').evaluate(e => Math.abs(e.getBoundingClientRect().top) < 80));
    await page.setViewportSize({ width: 390, height: 844 }); await check();
    await page.screenshot({ path: path.join(screenshots, 'mobile.png'), fullPage: true });
    assert.deepEqual(errors, []);
    process.stdout.write(JSON.stringify({ cases: 5, stages: data.cases.reduce((n, c) => n + c.stages.length, 0),
      controls: 2, scaling: 20, references: references.size, errors: 0, desktop: true, mobile: true }) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
