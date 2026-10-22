const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const zlib = require('node:zlib');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/weighted-shapes/gallery.html'));
const directory = path.dirname(gallery);

function ratio(value) {
  assert.equal(typeof value, 'string');
  assert.match(value, /^\d+\/[1-9]\d*$/);
  const [n, d] = value.split('/').map(BigInt);
  assert.ok(n <= d, value + ' must be a probability within its declared pool');
  return { n, d };
}
function equal(a, b) { return a.n * b.d === b.n * a.d; }
function add(a, b) { return { n: a.n * b.d + b.n * a.d, d: a.d * b.d }; }

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'gallery-data.json'), 'utf8'));
  const summary = JSON.parse(await fs.readFile(path.join(directory, 'summary.json'), 'utf8'));
  assert.equal(data.schema, 'weighted-shape-gallery-v1');
  assert.deepEqual(data.summary, summary);
  assert.ok(data.examples.length > 0);
  assert.equal(new Set(data.examples.map(row => row.id)).size, data.examples.length);
  if (typeof summary.sources === 'number') assert.equal(data.examples.length, summary.sources);
  const images = new Map(); let results = 0, candidates = 0, evidenceRows = 0, neutralChecks = 0;
  function image(file, assumptions) {
    assert.equal(typeof file, 'string'); assert.ok(file.endsWith('.svg'));
    assert.ok(!path.isAbsolute(file) && !file.includes('#'));
    assert.ok(path.resolve(directory, file).startsWith(directory + path.sep));
    assert.ok(!images.has(file), 'unique image: ' + file); images.set(file, assumptions);
  }
  for (const row of data.examples) {
    assert.ok(row.id && row.name && row.evidence);
    image(row.sourceImage, 0);
    assert.ok(row.results.length > 0);
    assert.equal(new Set(row.results.map(result => result.id)).size, row.results.length);
    for (const result of row.results) {
      results++;
      assert.ok(result.name && result.premise && result.diagnostics);
      assert.ok(result.candidates.length > 0 && result.candidates.length <= 6);
      assert.equal(new Set(result.candidates.map(candidate => candidate.id)).size, result.candidates.length);
      assert.ok(result.candidates.some(candidate => candidate.id === result.baselineId), 'baseline must be visible');
      assert.ok(result.candidates.some(candidate => candidate.id === result.selectedId), 'weighted preference must be visible');
      let priorMass = { n: 0n, d: 1n }, posteriorMass = { n: 0n, d: 1n };
      for (const candidate of result.candidates) {
        candidates++;
        assert.ok(candidate.id && candidate.name && candidate.audit);
        assert.ok(Number.isInteger(candidate.assumptions) && candidate.assumptions >= 0);
        image(candidate.image, candidate.assumptions);
        const prior = ratio(candidate.prior), posterior = ratio(candidate.posterior);
        priorMass = add(priorMass, prior); posteriorMass = add(posteriorMass, posterior);
        if (/uniform-flat/.test(result.id)) { assert.ok(equal(prior, posterior), 'flat likelihood must preserve prior'); neutralChecks++; }
      }
      assert.ok(priorMass.n <= priorMass.d, 'displayed prior mass cannot exceed whole pool');
      assert.ok(posteriorMass.n <= posteriorMass.d, 'displayed posterior mass cannot exceed whole pool');
      assert.ok(Array.isArray(result.evidence));
      for (const evidence of result.evidence) {
        evidenceRows++;
        assert.ok(evidence.name && evidence.outcome && evidence.family && evidence.estimate && evidence.note);
      }
    }
  }
  for (const [file, assumptions] of images) {
    const svg = await fs.readFile(path.join(directory, file), 'utf8');
    assert.ok(svg.includes('<svg') && svg.includes('</svg>') && svg.includes('<title>'), file);
    assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg), file);
    assert.equal([...svg.matchAll(/<path\b[^>]*stroke-dasharray=/g)].length, assumptions,
      file + ': every retained assumed passage must be visibly dashed');
  }
  const archives = [];
  for (const file of (await fs.readdir(directory)).filter(file => file.endsWith('.gojo.json.gz'))) {
    const graph = JSON.parse(zlib.gunzipSync(await fs.readFile(path.join(directory, file))).toString('utf8'));
    assert.equal(graph.version, 3, file);
    assert.ok(graph.sourceId && graph.nodes.length > 0, file);
    assert.equal(graph.carriers.length, 0, file + ': selected statistical / structural layer remains unmeasured');
    archives.push({ file, sourceId: graph.sourceId, nodes: graph.nodes.length });
  }
  const report = { cases: data.examples.length, results, displayedCandidates: candidates, evidenceRows,
    neutralChecks, svgFiles: images.size, nativeArchives: archives };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const screenshots = path.resolve(__dirname, '../../../artifacts/weighted-shapes-qa');
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
    await page.waitForFunction(() => globalThis.WEIGHTED_SHAPES && document.querySelector('#case')?.options.length);
    assert.deepEqual(await page.evaluate(() => globalThis.WEIGHTED_SHAPES), data);
    const screenshotIndices = new Set([0, 14, 15, 63, data.examples.length - 1]);
    let photographedChange = false;
    for (const [index, row] of data.examples.entries()) {
      await page.selectOption('#case', row.id);
      assert.equal(await page.locator('#profile option').count(), row.results.length);
      for (const result of row.results) {
        await page.selectOption('#profile', result.id);
        assert.equal(await page.locator('.case').count(), 1);
        assert.equal(await page.locator('.case').getAttribute('data-case'), row.id);
        assert.equal(await page.locator('.case').getAttribute('data-profile'), result.id);
        assert.equal(await page.locator('.candidate').count(), result.candidates.length);
        assert.equal(await page.locator('.ranking tbody tr').count(), result.candidates.length);
        assert.equal(await page.locator('.evidence tbody tr').count(), result.evidence.length);
        assert.equal(new URL(page.url()).searchParams.get('case'), row.id);
        assert.equal(new URL(page.url()).searchParams.get('profile'), result.id);
        await assets();
      }
      await page.selectOption('#profile', row.results[0].id);
      const changed = row.results.find(result => result.baselineId !== result.selectedId);
      if (screenshotIndices.has(index) || (!photographedChange && changed)) {
        if (changed) { await page.selectOption('#profile', changed.id); photographedChange = true; }
        await page.locator('.comparison').screenshot({ path: path.join(screenshots, row.id + '-comparison.png') });
        if (index === 0) await page.screenshot({ path: path.join(screenshots, row.id + '-desktop.png'), fullPage: true });
      }
    }
    const linked = data.examples[Math.min(15, data.examples.length - 1)], profile = linked.results.at(-1);
    await page.goto(pathToFileURL(gallery).href + '?case=' + encodeURIComponent(linked.id) + '&profile=' + encodeURIComponent(profile.id));
    await page.waitForFunction(({ id, profile }) => document.querySelector('#case')?.value === id &&
      document.querySelector('#profile')?.value === profile, { id: linked.id, profile: profile.id }); await assets();
    await page.setViewportSize({ width: 430, height: 932 });
    for (const row of [data.examples[0], linked, data.examples.at(-1)]) {
      await page.selectOption('#case', row.id); await assets();
    }
    await page.screenshot({ path: path.join(screenshots, 'mobile.png'), fullPage: true });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await assets();
    assert.equal(await page.locator('.tile').count(), data.examples.length);
    await page.screenshot({ path: path.join(screenshots, 'overview-mobile.png'), fullPage: false });
    await page.setViewportSize({ width: 1500, height: 1050 }); await assets();
    await page.screenshot({ path: path.join(screenshots, 'overview-desktop.png'), fullPage: false });
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, []);
    Object.assign(report, { browserStates: states, localReferences: references.size, errors: errors.length, screenshots });
  } finally { await browser.close(); }
  process.stdout.write(JSON.stringify(report) + '\n');
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
