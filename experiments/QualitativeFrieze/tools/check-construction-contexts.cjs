// Check the generated construction-context gallery against its captured model.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { fileURLToPath, pathToFileURL } = require('node:url');
const { gunzipSync } = require('node:zlib');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');

const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/construction-contexts/gallery.html'));
const directory = path.dirname(gallery);
const screenshots = process.env.GOJO_SCREENSHOTS || path.join(os.tmpdir(), 'GojoConstructionContextsQA');
const checkedLinks = new Set();
const browserErrors = [];
let stepChecks = 0;

async function checkFileReference(raw, base) {
  const url = new URL(raw, base);
  if (url.protocol !== 'file:') return;
  const file = fileURLToPath(url);
  if (checkedLinks.has(url.href)) return;
  const info = await fs.stat(file);
  assert.ok(info.isFile() && info.size > 0, 'Missing or empty local asset: ' + url.href);
  if (url.hash && file.endsWith('.html')) {
    const html = await fs.readFile(file, 'utf8');
    const id = decodeURIComponent(url.hash.slice(1));
    assert.ok(new RegExp('id=["\\\']' + id.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '["\\\']').test(html),
      'Missing fragment: ' + url.href);
  }
  checkedLinks.add(url.href);
}

async function checkPageAssets(page) {
  const references = await page.locator('[href], [src], object[data]').evaluateAll(items => items.flatMap(item =>
    ['href', 'src', 'data'].filter(key => item.hasAttribute(key)).map(key => item.getAttribute(key))));
  for (const raw of references) await checkFileReference(raw, page.url());
  const incomplete = await page.locator('img').evaluateAll(images => images
    .filter(img => !img.complete || img.naturalWidth === 0).map(img => img.src));
  assert.deepEqual(incomplete, [], 'Broken displayed images');
}

async function waitForImages(page) {
  await page.waitForFunction(() => [...document.images].every(img => img.complete && img.naturalWidth > 0));
}

async function checkSelected(card, step, construction, index, count) {
  assert.equal(await card.getAttribute('data-step'), step.id);
  assert.equal(await card.locator('.step-title').textContent(), step.name);
  assert.equal(await card.locator('.step-description').textContent(), step.description);
  assert.equal(await card.locator('.step-number').textContent(), `${index + 1} / ${count}`);
  assert.equal(await card.locator('.step-range').inputValue(), String(index));
  const expected = construction ? step.construction : step.plain;
  assert.equal(await card.locator('img.picture').getAttribute('src'), expected);
  assert.equal(await card.locator('a.art-link').getAttribute('href'), expected);
  assert.equal(await card.locator('a.download').getAttribute('href'), expected);
  assert.deepEqual(await card.locator('.step-button[aria-pressed="true"]').allTextContents(), [String(index + 1)]);
  assert.deepEqual(await card.locator('dd').allTextContents(), [step.paths, step.chords, step.closedClaims,
    step.ports, step.rungs, step.acceptedActions, step.blocked, step.nativeNumericElements].map(String));
  assert.equal(await card.locator('.status.blocked').count(), step.blocked || step.open ? 1 : 0);
  if (step.blocked) assert.match(await card.locator('.status').textContent(), /blocked edit evidence.*retained/);
  if (step.open && !step.blocked) assert.match(await card.locator('.status').textContent(), /open edit evidence.*retained/);
  stepChecks++;
}

async function assertNoOverflow(page) {
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
    'Horizontal page overflow at ' + JSON.stringify(page.viewportSize()) + ': ' + page.url());
}

(async () => {
  const captured = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(captured.schema, 'gojo-construction-contexts-v1');
  assert.equal(captured.nativeNumericElements, 0);
  assert.equal(captured.archiveRoundTrip, true);
  assert.equal(captured.cases.length, 4);
  assert.equal(captured.views.length, 3);
  assert.equal(captured.cases.flatMap(item => item.variants.flatMap(variant => variant.steps)).length, 17);
  const archive = JSON.parse(gunzipSync(await fs.readFile(path.join(directory, 'contexts.gojo.json.gz'))).toString('utf8'));
  assert.ok(archive && typeof archive === 'object', 'Native download is a parseable gzip JSON archive');
  for (const step of captured.cases.flatMap(item => item.variants.flatMap(variant => variant.steps))) {
    assert.equal(step.nativeNumericElements, 0, step.id);
    assert.ok(step.acceptedActions > 0, step.id + ' has acknowledged action receipts');
    for (const key of ['plain', 'construction']) {
      const svg = await fs.readFile(path.join(directory, step[key]), 'utf8');
      assert.match(svg, /<svg\b/);
      assert.doesNotMatch(svg, /(?:NaN|Infinity)/, step[key]);
      assert.equal([...svg.matchAll(/<line\b/g)].length, step.chords, step[key] + ' draws every acknowledged chord');
      assert.equal([...svg.matchAll(/<circle\b/g)].length, key === 'construction' ? step.paths : 0,
        step[key] + ' actual path starts');
      await checkFileReference(step[key], pathToFileURL(gallery));
    }
  }
  for (const view of captured.views) {
    const svg = await fs.readFile(path.join(directory, view.result), 'utf8');
    const sites = [...svg.matchAll(/<circle\b[^>]*class=['"]site['"][^>]*>/g)].map(match => match[0]);
    const bridges = [...svg.matchAll(/<circle\b[^>]*class=['"]bridge['"][^>]*>/g)].map(match => match[0]);
    assert.equal(sites.length, view.groupCount, view.id + ' marks every actual endpoint role');
    const position = circle => ['cx', 'cy'].map(axis => circle.match(new RegExp(axis + "=['\"]([^'\"]+)['\"]"))[1]).join('/');
    for (const bridge of bridges)
      assert.ok(!sites.some(site => position(site) === position(bridge)), 'A projected crossing is not marked as an actual site');
    if (view.id === 'V02') assert.equal(bridges.length, 2, 'Both projected X crossings retain visible bridge gaps');
  }
  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  try {
    await fs.mkdir(screenshots, { recursive: true });
    const context = await browser.newContext({ viewport: { width: 1440, height: 1050 }, reducedMotion: 'reduce' });
    const page = await context.newPage();
    page.on('pageerror', error => browserErrors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') browserErrors.push(message.text()); });
    page.on('requestfailed', request => browserErrors.push(request.url() + ': ' + request.failure()?.errorText));
    await page.goto(pathToFileURL(gallery).href);
    await waitForImages(page);
    assert.deepEqual(JSON.parse(await page.locator('#data').textContent()), captured);
    assert.equal(await page.locator('article.variant').count(), captured.cases.reduce((n, item) => n + item.variants.length, 0));
    assert.equal(await page.locator('article.field-card').count(), captured.views.length);
    await checkPageAssets(page);
    await assertNoOverflow(page);

    for (const construction of [true, false]) {
      await page.locator('#construction').setChecked(construction);
      for (const item of captured.cases) for (const variant of item.variants) {
        const card = page.locator(`article.variant[data-variant="${variant.id}"]`);
        for (let index = 0; index < variant.steps.length; index++) {
          await card.locator('.step-button').nth(index).click();
          await checkSelected(card, variant.steps[index], construction, index, variant.steps.length);
          await waitForImages(page);
          await checkPageAssets(page);
        }
        // Exercise the independent slider path in reverse, including its endpoints.
        for (let index = variant.steps.length - 1; index >= 0; index--) {
          await card.locator('.step-range').evaluate((slider, value) => {
            slider.value = String(value); slider.dispatchEvent(new Event('input', { bubbles: true }));
          }, index);
          await checkSelected(card, variant.steps[index], construction, index, variant.steps.length);
        }
      }
    }
    for (const view of captured.views) {
      const card = page.locator(`article.field-card[data-view="${view.id}"]`);
      assert.deepEqual(await card.locator('img').evaluateAll(images => images.map(img => img.getAttribute('src'))),
        [view.groups, view.result, view.ornament]);
      assert.match(await card.locator('.field-facts').textContent(), new RegExp(`${view.groupCount} groups.*${view.contacts} contact passages.*${view.decorations} motif placements`));
    }
    await page.locator('#construction').check();
    await page.goto(pathToFileURL(gallery).href + '?case=C03');
    await waitForImages(page);
    assert.ok(await page.locator('#C03').evaluate(section => Math.abs(section.getBoundingClientRect().top) < 130));
    await page.screenshot({ path: path.join(screenshots, 'construction-contexts-desktop.png'), fullPage: true });

    const overview = pathToFileURL(path.join(directory, 'overview.html')).href;
    await page.goto(overview);
    await waitForImages(page);
    assert.equal(await page.locator('img').count(), 17 + captured.views.length * 3);
    await checkPageAssets(page);
    await assertNoOverflow(page);
    await page.screenshot({ path: path.join(screenshots, 'construction-contexts-overview.png'), fullPage: true });

    const crossing = await context.newPage();
    await crossing.setViewportSize({ width: 700, height: 360 });
    await crossing.goto(pathToFileURL(path.join(directory, 'V02-result.svg')).href);
    await crossing.screenshot({ path: path.join(screenshots, 'construction-contexts-V02-crossings.png') });
    await crossing.close();

    for (const route of [pathToFileURL(gallery).href, overview]) {
      await page.setViewportSize({ width: 390, height: 844 });
      await page.goto(route);
      await waitForImages(page);
      await checkPageAssets(page);
      await assertNoOverflow(page);
      await page.screenshot({ path: path.join(screenshots,
        route === overview ? 'construction-contexts-overview-mobile.png' : 'construction-contexts-mobile.png'), fullPage: true });
      await page.screenshot({ path: path.join(screenshots,
        route === overview ? 'construction-contexts-overview-mobile-top.png' : 'construction-contexts-mobile-top.png') });
    }
    assert.deepEqual(browserErrors, []);
    console.log(`Verified 17 steps through buttons and sliders in both display modes (${stepChecks} state checks), 3 field constructions with actual site markers and projected-crossing gaps, ${checkedLinks.size} local references, native gzip JSON, desktop/mobile layouts and browser errors.`);
    console.log('Screenshots: ' + screenshots);
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
