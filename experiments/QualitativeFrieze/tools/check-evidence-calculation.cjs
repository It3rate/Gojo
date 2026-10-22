const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const zlib = require('node:zlib');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/evidence-calculation/gallery.html'));
const directory = path.dirname(gallery);

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'gallery-data.json'), 'utf8'));
  const summary = JSON.parse(await fs.readFile(path.join(directory, 'summary.json'), 'utf8'));
  assert.equal(data.schema, 'evidence-calculation-v1');
  assert.deepEqual(data.summary, summary);
  assert.equal(summary.cases, data.cases.length);
  assert.ok(data.cases.length >= 16);
  assert.equal(new Set(data.cases.map(row => row.id)).size, data.cases.length);
  assert.equal(data.cases.reduce((sum, row) => sum + row.checks.length, 0), summary.lawChecks);
  assert.equal(summary.lawChecks, summary.passedChecks);
  const images = new Set(); let steps = 0;
  for (const row of data.cases) {
    assert.ok(row.id && row.name && row.question && row.premises && row.finding);
    assert.ok(row.checks.length > 0);
    assert.ok(row.checks.every(check => check.passed === true && check.law && check.detail));
    assert.ok(row.checks.some(check => check.law === 'Standalone native transport'));
    assert.ok(row.steps.length > 0);
    for (const step of row.steps) {
      steps++;
      assert.ok(step.name && step.explanation);
      assert.ok(step.candidates.length > 0);
      assert.ok(step.candidates.every(candidate => candidate.candidate && candidate.prior && candidate.likelihood && candidate.posterior && candidate.status));
      assert.equal(typeof step.image, 'string');
      assert.ok(step.image.endsWith('.svg') && !path.isAbsolute(step.image) && !step.image.includes('#'));
      assert.ok(path.resolve(directory, step.image).startsWith(directory + path.sep));
      assert.ok(!images.has(step.image)); images.add(step.image);
    }
  }
  const find = id => data.cases.find(row => row.id === id);
  assert.equal(find('E01').steps.length, 4);
  assert.equal(find('E03').steps.at(-1).candidates[0].posterior, '99/100');
  assert.equal(find('E04').steps.at(-1).candidates[1].posterior, '0/1');
  assert.equal(find('E05').steps.at(-1).candidates[1].posterior, '99/100');
  assert.equal(find('E06').steps.at(-1).candidates[0].posterior, '9/10');
  assert.equal(find('E10').steps.at(-1).candidates[0].posterior, '9/13');
  assert.equal(find('E14').steps.at(-1).candidates[0].posterior, '2/3');
  assert.equal(find('E14').steps.at(-1).candidates[1].posterior, '51/101');
  assert.equal(find('E16').steps[0].candidates[0].posterior, '1/2');
  assert.equal(find('E16').steps[0].candidates[0].status, 'Scope report probability 1/100');
  assert.equal(find('E16').steps[1].candidates[0].status, 'Scope report probability 1/1');
  for (const file of images) {
    const svg = await fs.readFile(path.join(directory, file), 'utf8');
    assert.ok(svg.includes('<svg') && svg.includes('</svg>') && svg.includes('<title>'), file);
    assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg), file);
  }
  const archive = JSON.parse(zlib.gunzipSync(await fs.readFile(path.join(directory, 'receipts.gojo.json.gz'))).toString('utf8'));
  assert.equal(archive.version, 3);
  assert.equal(archive.sourceId, 'evidence-calculation-v1');
  assert.ok(archive.nodes.length > 0);
  const report = { cases: data.cases.length, lawChecks: summary.lawChecks, steps, svgFiles: images.size, nativeNodes: archive.nodes.length };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const screenshots = path.resolve(__dirname, '../../../artifacts/evidence-calculation-qa');
  await fs.mkdir(screenshots, { recursive: true });
  const errors = [], references = new Set(); let states = 0;
  try {
    const page = await browser.newPage({ viewport: { width: 1500, height: 1050 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => { if (request.failure()?.errorText !== 'net::ERR_ABORTED') errors.push(request.url()); });
    async function assets() {
      await page.waitForFunction(() => [...document.querySelectorAll('img[src]')].filter(image => {
        const box = image.getBoundingClientRect(); return box.width && box.height && box.top < innerHeight && box.bottom >= 0;
      }).every(image => image.complete && image.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'no page overflow');
      for (const raw of await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element => ['href', 'src']
        .filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))))) {
        const url = new URL(raw, page.url());
        if (url.protocol === 'file:') { assert.equal(url.hash, ''); references.add(url.href); }
      }
      states++;
    }
    await page.goto(pathToFileURL(gallery).href);
    await page.waitForFunction(() => globalThis.EVIDENCE_CALCULATION && document.querySelector('#case')?.options.length);
    assert.deepEqual(await page.evaluate(() => globalThis.EVIDENCE_CALCULATION), data);
    for (const row of data.cases) {
      await page.selectOption('#case', row.id);
      assert.equal(await page.locator('.case').count(), 1);
      assert.equal(await page.locator('.step').count(), row.steps.length);
      assert.equal(new URL(page.url()).searchParams.get('case'), row.id);
      await assets();
      if (['E01', 'E03', 'E04', 'E08', 'E14', 'E16'].includes(row.id)) {
        await page.locator('details').evaluateAll(elements => elements.forEach(element => element.open = true));
        await page.screenshot({ path: path.join(screenshots, row.id + '-desktop.png'), fullPage: true });
      }
    }
    await page.goto(pathToFileURL(gallery).href + '?case=E03');
    await page.waitForFunction(() => document.querySelector('#case')?.value === 'E03'); await assets();
    await page.setViewportSize({ width: 430, height: 932 });
    for (const id of ['E01', 'E08', 'E14']) { await page.selectOption('#case', id); await assets(); }
    await page.screenshot({ path: path.join(screenshots, 'mobile.png'), fullPage: true });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await assets();
    assert.equal(await page.locator('.case').count(), data.cases.length);
    await page.screenshot({ path: path.join(screenshots, 'overview-mobile.png'), fullPage: false });
    await page.setViewportSize({ width: 1500, height: 1050 }); await assets();
    await page.screenshot({ path: path.join(screenshots, 'overview-desktop.png'), fullPage: false });
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, []);
    Object.assign(report, { browserStates: states, localReferences: references.size, errors: errors.length, screenshots });
  } finally { await browser.close(); }
  process.stdout.write(JSON.stringify(report) + '\n');
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
