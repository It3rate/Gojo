const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/retained-match/gallery.html'));
const directory = path.dirname(gallery), screenshots = path.resolve('artifacts/retained-match-qa');

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(data.schema, 'retained-match-v1'); assert.equal(data.cases.length, 5); assert.equal(data.controls.length, 5);
  assert.equal(data.nativeNumericElements, 0); assert.equal(data.archiveRoundTrip, true);
  assert.ok(data.nativeElements > 0);
  const expected = { R01: ['Yes', 'No'], R02: ['Yes', 'Yes'], R03: ['Yes', 'No'], R04: ['Yes', 'Yes'], R05: ['Yes', 'Yes'] };
  for (const c of data.cases) {
    assert.deepEqual([c.freshAnswer, c.retainedAnswer], expected[c.id]);
    assert.equal(c.initialAnswer, 'Yes'); assert.equal(c.recheckAnswer, 'Yes');
    assert.equal(c.retainedRoles, c.id === 'R03' ? 2 : 1); assert.ok(c.selectedEdges > 0);
    for (const key of ['initial', 'fresh', 'retained', 'recheck']) {
      assert.ok(c.search[key].assignments >= 0); assert.ok(c.search[key].mappings >= 0);
    }
    const source = await fs.readFile(path.join(directory, c.sourceImage), 'utf8');
    const focused = await fs.readFile(path.join(directory, c.focusedImage), 'utf8');
    const classes = text => [...text.matchAll(/class=(['"])(.*?)\1/g)].flatMap(match => match[2].split(/\s+/));
    assert.equal(classes(source).filter(value => value === 'retained-edge').length, 0);
    assert.equal(classes(focused).filter(value => value === 'retained-edge').length, c.selectedEdges);
    // Highlighting may change paint, but must preserve the complete scene.
    function geometry(text) {
      const names = new Set(['d', 'x1', 'y1', 'x2', 'y2', 'cx', 'cy', 'r', 'rx', 'ry', 'points', 'x', 'y', 'width', 'height', 'transform']);
      return [...text.matchAll(/<(path|line|circle|ellipse|polygon|polyline|rect)\b([^>]*)>/g)].map(match => [match[1],
        [...match[2].matchAll(/([\w:-]+)=(['"])(.*?)\2/g)].filter(attr => names.has(attr[1])).map(attr => [attr[1], attr[3]]).sort()]);
    }
    assert.deepEqual(geometry(focused), geometry(source), c.id + ': focus must preserve scene geometry');
  }
  assert.deepEqual(data.controls.map(c => c.answer), ['No', 'Open', 'Open', 'Open', 'Blocked']);
  assert.ok(data.controls[1].stale > 0); assert.ok(data.controls[4].blocked > 0);
  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const errors = [], references = new Set();
  try {
    await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1050 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => errors.push(request.url()));
    async function checkPage() {
      await page.waitForFunction(() => [...document.images].every(image => image.complete && image.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'horizontal page overflow');
      for (const raw of await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element =>
        ['href', 'src'].filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))))) {
        assert.ok(!raw.includes('#'), 'local links should not need fragments');
        const url = new URL(raw, page.url());
        if (url.protocol !== 'file:' || references.has(url.href)) continue;
        const info = await fs.stat(fileURLToPath(url)); assert.ok(info.isFile() && info.size > 0); references.add(url.href);
      }
      const text = await page.locator('body').innerText();
      assert.ok(!/midpoint/i.test(text), 'between should not imply measured subdivision');
      assert.ok(!/earlier vocabulary|previous language|previous experiment/i.test(text));
    }
    await page.goto(pathToFileURL(gallery).href); await checkPage();
    assert.equal(await page.locator('#cases > article').count(), 5);
    assert.equal(await page.locator('#controls > article').count(), 5);
    async function checkSteps() {
      for (const c of data.cases) {
        const article = page.locator(`[data-case="${c.id}"]`);
        const sourceUrl = await article.locator('.source-image').getAttribute('src');
        const focusedUrl = await article.locator('.focused-image').getAttribute('src');
        assert.equal(sourceUrl, c.sourceImage); assert.equal(focusedUrl, c.focusedImage);
        for (let step = 0; step < 4; step++) {
          await article.locator(`[data-step="${step}"]`).click();
          assert.equal(await article.getAttribute('data-step'), String(step));
          assert.equal(await article.locator('[aria-pressed="true"]').count(), 1);
          assert.equal(await article.locator(`[data-step="${step}"]`).getAttribute('aria-pressed'), 'true');
          assert.equal(await article.locator('.source-image').getAttribute('src'), sourceUrl);
          assert.equal(await article.locator('.focused-image').getAttribute('src'), focusedUrl);
          assert.ok(await article.locator('.source-image').isVisible()); assert.ok(await article.locator('.focused-image').isVisible());
          assert.equal(await article.locator('.comparison').isVisible(), step >= 2);
          const stage = await article.locator('.stage').innerText();
          if (step === 0) {
            assert.ok(stage.includes(c.initialQuestion)); assert.equal(await article.locator('.initial-answer').innerText(), 'Yes');
          } else if (step === 1) assert.ok(stage.includes(c.focusLabel));
          else if (step === 2) assert.ok(stage.includes(c.followupQuestion));
          else {
            assert.ok(stage.includes(c.initialQuestion)); assert.equal(await article.locator('.recheck-answer').innerText(), 'Yes');
          }
          if (step >= 2) {
            assert.equal(await article.locator('.fresh-answer').innerText(), c.freshAnswer);
            assert.equal(await article.locator('.retained-answer').innerText(), c.retainedAnswer);
            assert.equal(await article.locator('.comparison-question').innerText(), c.followupQuestion);
            assert.deepEqual(await article.locator('.answer-card .answer').evaluateAll(elements => elements.map(element =>
              [getComputedStyle(element).color, getComputedStyle(element).opacity])),
              [['rgb(38, 61, 78)', '1'], ['rgb(38, 61, 78)', '1']], 'answer values share neutral styling');
          }
          assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        }
      }
    }
    await checkSteps();
    for (const [index, c] of data.controls.entries()) {
      const control = page.locator(`[data-control="${index}"]`);
      assert.equal(await control.locator('.answer').innerText(), c.answer);
      assert.ok((await control.innerText()).includes(c.purpose));
    }
    await page.locator('[data-case="R01"]').screenshot({ path: path.join(screenshots, 'R01-retained-path.png') });
    await page.locator('[data-case="R03"]').screenshot({ path: path.join(screenshots, 'R03-retained-pair.png') });
    await page.locator('[data-case="R04"]').screenshot({ path: path.join(screenshots, 'R04-between.png') });
    await page.screenshot({ path: path.join(screenshots, 'desktop.png'), fullPage: true });
    await page.goto(pathToFileURL(gallery).href + '?case=R03');
    assert.ok(await page.locator('[data-case="R03"]').evaluate(element => Math.abs(element.getBoundingClientRect().top) < 80));
    await page.setViewportSize({ width: 390, height: 844 }); await checkPage(); await checkSteps();
    await page.screenshot({ path: path.join(screenshots, 'mobile.png'), fullPage: true });
    assert.deepEqual(errors, []);
    process.stdout.write(JSON.stringify({ cases: 5, stages: 20, controls: 5, references: references.size,
      errors: 0, desktop: true, mobile: true }) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
