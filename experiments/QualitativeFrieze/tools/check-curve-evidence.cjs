const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const zlib = require('node:zlib');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/curve-evidence/gallery.html'));
const directory = path.dirname(gallery);

function ratio(value) {
  if (value === 'Open') return null;
  assert.equal(typeof value, 'string');
  assert.match(value, /^\d+\/[1-9]\d*$/);
  const [n, d] = value.split('/').map(BigInt);
  assert.ok(n <= d, value + ' lies within the declared candidate pool');
  return { n, d };
}
function add(a, b) { return { n: a.n * b.d + b.n * a.d, d: a.d * b.d }; }

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'gallery-data.json'), 'utf8'));
  const summary = JSON.parse(await fs.readFile(path.join(directory, 'summary.json'), 'utf8'));
  assert.equal(data.schema, 'curve-evidence-gallery-v1');
  assert.deepEqual(data.summary, summary);
  assert.ok(data.examples.length > 0);
  if (typeof summary.cases === 'number') assert.equal(data.examples.length, summary.cases);
  assert.equal(new Set(data.examples.map(row => row.id)).size, data.examples.length);
  const images = new Map(); let candidates = 0, evidenceRows = 0, curves = 0, assumed = 0;
  const joinKinds = new Set();
  function image(file, drawing) {
    assert.equal(typeof file, 'string');
    assert.ok(file.endsWith('.svg') && !path.isAbsolute(file) && !file.includes('#'));
    assert.ok(path.resolve(directory, file).startsWith(directory + path.sep));
    assert.ok(!images.has(file), 'unique image: ' + file);
    assert.equal(drawing.straight + drawing.quadratic + drawing.cubic, drawing.strokes);
    assert.ok(drawing.assumed >= 0 && drawing.assumed <= drawing.strokes);
    assert.equal(typeof drawing.closed, 'boolean');
    assert.ok(Array.isArray(drawing.joints));
    for (const joint of drawing.joints) joinKinds.add(joint.mark);
    curves += drawing.quadratic + drawing.cubic; assumed += drawing.assumed;
    images.set(file, drawing);
  }
  for (const row of data.examples) {
    assert.ok(row.id && row.name && row.note && row.diagnostics);
    image(row.sourceImage, row.source); image(row.coarseImage, row.coarse);
    assert.ok(row.candidates.length >= 1);
    assert.equal(new Set(row.candidates.map(candidate => candidate.id)).size, row.candidates.length);
    if (row.selectedId !== null) assert.ok(row.candidates.some(candidate => candidate.id === row.selectedId));
    let prior = { n: 0n, d: 1n }, posterior = { n: 0n, d: 1n }, complete = true;
    for (const candidate of row.candidates) {
      candidates++;
      assert.ok(candidate.id && candidate.name && candidate.interpretation && candidate.audit);
      image(candidate.image, candidate.drawing);
      const a = ratio(candidate.prior), b = ratio(candidate.posterior);
      if (a && b) { prior = add(prior, a); posterior = add(posterior, b); } else complete = false;
    }
    if (complete) {
      assert.equal(prior.n, prior.d, row.id + ': full candidate prior mass');
      assert.equal(posterior.n, posterior.d, row.id + ': full candidate posterior mass');
    }
    assert.ok(row.evidence.length > 0);
    for (const evidence of row.evidence) {
      evidenceRows++;
      assert.ok(evidence.name && evidence.outcome && evidence.basis && evidence.estimate && evidence.note);
    }
  }
  for (const [file, drawing] of images) {
    const svg = await fs.readFile(path.join(directory, file), 'utf8');
    assert.ok(svg.includes('<svg') && svg.includes('</svg>') && svg.includes('<title>'), file);
    assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg), file);
    assert.equal([...svg.matchAll(/<path class='stroke(?: assumed)?'/g)].length, drawing.strokes, file + ': every stroke displayed');
    assert.equal([...svg.matchAll(/<path class='stroke assumed'/g)].length, drawing.assumed, file + ': assumptions remain visibly distinct');
    assert.equal([...svg.matchAll(/<g class='joint'/g)].length, drawing.joints.filter(joint => joint.mark !== 'None').length);
    for (const mark of drawing.joints.filter(joint => joint.mark !== 'None').map(joint => joint.mark))
      assert.ok(svg.includes("data-mark='" + mark + "'"), file + ': joint read marker ' + mark);
    const actualCurves = [...svg.matchAll(/<path class='stroke[^']*' d='[^']* [CQ]/g)].length;
    assert.equal(actualCurves, drawing.quadratic + drawing.cubic, file + ': curves preserved at display boundary');
  }
  assert.ok(curves > 0 && assumed > 0, 'exercise both curves and explicit missing-span completion');
  assert.ok(joinKinds.has('Tangent') && joinKinds.has('Cusp') && joinKinds.has('Open') && joinKinds.has('Unresolved'),
    'distinguish smooth, cusp, open and unresolved local readings');
  const archives = [];
  for (const file of (await fs.readdir(directory, { recursive: true })).filter(file => file.endsWith('.gojo.json.gz'))) {
    const graph = JSON.parse(zlib.gunzipSync(await fs.readFile(path.join(directory, file))).toString('utf8'));
    assert.equal(graph.version, 3, file);
    assert.ok(graph.sourceId && graph.nodes.length > 0, file);
    const nativeNodes = new Map(graph.nodes.map(node => [node.id, node]));
    for (const carrier of graph.carriers) {
      assert.equal(carrier.value ?? 0, 0, file + ': holders do not store measured geometry');
      assert.equal(carrier.tension ?? 0, 0, file + ': read-only interpolation holders remain quiet');
      if (carrier.valueTick) assert.equal(nativeNodes.get(carrier.valueTick)?.ticks, 0, file + ': retained holder value is zero');
      if (carrier.tensionTick) assert.equal(nativeNodes.get(carrier.tensionTick)?.ticks, 0, file + ': retained holder tension is zero');
    }
    for (const node of graph.nodes) {
      // Placeholder ticks carry packet identity, not geometry. Ordered groups,
      // interpolation ports and statistical unit scopes use fixed 0/1 addresses.
      if (node.kind !== 'placeholder' && node.ticks != null)
        assert.ok(node.ticks === 0 || node.ticks === 1, file + ': only fixed unit/port addresses');
      if (node.start != null) assert.ok(node.start === 0 || node.start === 1, file + ': only fixed span start');
      if (node.end != null) assert.ok(node.end === 0 || node.end === 1, file + ': only fixed span end');
    }
    archives.push({ file, sourceId: graph.sourceId, nodes: graph.nodes.length, carriers: graph.carriers.length });
  }
  assert.ok(archives.length > 0, 'standalone native receipt archive');
  const report = { cases: data.examples.length, candidates, evidenceRows, svgFiles: images.size,
    displayedCurves: curves, displayedAssumptions: assumed, joinKinds: [...joinKinds].sort(), nativeArchives: archives };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const screenshots = path.resolve(__dirname, '../../../artifacts/' + path.basename(directory) + '-qa');
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
    await page.waitForFunction(() => globalThis.CURVE_EVIDENCE && document.querySelector('#case')?.options.length);
    assert.deepEqual(await page.evaluate(() => globalThis.CURVE_EVIDENCE), data);
    const capture = new Set([0, 1, 5, 7, 8, 9, 17, 27, 66, 67, 78, 91, 95, Math.floor(data.examples.length / 2),
      data.examples.length - 2, data.examples.length - 1]);
    for (const [index, row] of data.examples.entries()) {
      await page.selectOption('#case', row.id);
      assert.equal(await page.locator('.case').getAttribute('data-case'), row.id);
      assert.equal(await page.locator('.candidate').count(), row.candidates.length);
      assert.equal(await page.locator('.evidence tbody tr').count(), row.evidence.length);
      const selected = row.candidates.find(candidate => candidate.id === row.selectedId);
      const selectedWeight = selected ? ratio(selected.posterior) : null;
      const tied = selectedWeight ? row.candidates.filter(candidate => {
        const weight = ratio(candidate.posterior);
        return weight && weight.n * selectedWeight.d === selectedWeight.n * weight.d;
      }).length : 0;
      assert.equal(await page.locator('.candidate.preferred').count(), tied);
      assert.equal(await page.locator('.tie-note').count(), tied > 1 ? 1 : 0);
      assert.equal(new URL(page.url()).searchParams.get('case'), row.id);
      await assets();
      if (capture.has(index)) {
        await page.locator('details').evaluateAll(elements => elements.forEach(element => element.open = true));
        await page.screenshot({ path: path.join(screenshots, row.id + '-desktop.png'), fullPage: true });
        await page.locator('.comparison').screenshot({ path: path.join(screenshots, row.id + '-comparison.png') });
        await page.locator('.alternatives').screenshot({ path: path.join(screenshots, row.id + '-alternatives.png') });
      }
    }
    const linked = data.examples[Math.floor(data.examples.length / 2)];
    await page.goto(pathToFileURL(gallery).href + '?case=' + encodeURIComponent(linked.id));
    await page.waitForFunction(id => document.querySelector('#case')?.value === id, linked.id); await assets();
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
    process.stdout.write(JSON.stringify({ ...report, browserStates: states, localReferences: references.size,
      browserErrors: errors, screenshots }) + '\n');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
