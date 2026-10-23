const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const zlib = require('node:zlib');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/ornament-composition/gallery.html'));
const directory = path.dirname(gallery);

function local(file) {
  assert.equal(typeof file, 'string');
  assert.ok(!path.isAbsolute(file) && !file.includes('#') && !file.includes('..'), 'relative asset without a fragment: ' + file);
  const resolved = path.resolve(directory, file);
  assert.ok(resolved.startsWith(directory + path.sep), 'asset remains in gallery: ' + file);
  return resolved;
}

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'gallery-data.json'), 'utf8'));
  const summary = JSON.parse(await fs.readFile(path.join(directory, 'summary.json'), 'utf8'));
  assert.equal(data.schema, 'ornament-composition-v1');
  assert.deepEqual(data.summary, summary);
  assert.ok(data.examples.length > 0);
  if (typeof summary.cases === 'number') assert.equal(data.examples.length, summary.cases);
  assert.equal(new Set(data.examples.map(row => row.id)).size, data.examples.length);
  const images = new Set(), native = new Set();
  let paths = 0, curves = 0, masks = 0, evidenceRows = 0, filledPaths = 0;
  async function inspectSvg(file, drawing, overlay = false) {
    assert.ok(!images.has(file), 'unique image'); images.add(file);
    const svg = await fs.readFile(local(file), 'utf8');
    assert.ok(svg.includes('<svg') && svg.includes('</svg>') && svg.includes('<title>') && svg.includes('<desc>'));
    assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg), file + ': finite display');
    assert.ok(svg.includes("clip-path='url(#viewport)'"), file + ': explicit display extent');
    assert.equal([...svg.matchAll(/<path class='ink /g)].length, drawing.paths.length, file + ': every supplied native projection displayed');
    const ids = new Set([...svg.matchAll(/\bid='([^']+)'/g)].map(match => match[1]));
    for (const match of svg.matchAll(/url\(#([^)]+)\)/g)) assert.ok(ids.has(match[1]), file + ': mask and clip references resolve');
    if (overlay) assert.ok(svg.includes("fill='transparent'"), 'frame overlay leaves ornament visible');
    for (const ink of drawing.paths) {
      paths++;
      assert.equal(typeof ink.data, 'string'); assert.match(ink.data, /^\s*M/i, file + ': concrete display start');
      assert.ok(Number.isFinite(ink.strokeWidth) && ink.strokeWidth >= 0);
      assert.ok(Number.isFinite(ink.opacity) && ink.opacity >= 0 && ink.opacity <= 1);
      assert.ok(ink.role && ink.fill && ink.stroke);
      if (/[CQAS]/i.test(ink.data)) curves++;
      if (ink.fill !== 'none' && ink.fill !== 'transparent') filledPaths++;
      if (ink.cuts?.length) {
        masks++;
        for (const cut of ink.cuts) assert.ok(cut.data && Number.isFinite(cut.strokeWidth) && cut.strokeWidth >= 0);
      }
    }
  }
  for (const row of data.examples) {
    assert.ok(row.id && row.name && row.note && row.audit);
    assert.equal(row.panels.length, 4, row.id + ': frame, compound segment and two colour readings');
    assert.ok(row.evidence.length > 0); evidenceRows += row.evidence.length;
    for (const cue of row.evidence) assert.ok(cue.name && cue.outcome && cue.basis && cue.note);
    assert.ok(row.nativePacket?.endsWith('.gojo.json.gz'), row.id + ': native construction accompanies display');
    native.add(local(row.nativePacket));
    for (const panel of row.panels) {
      assert.ok(panel.name && panel.note && panel.image.endsWith('.svg'));
      assert.ok(Array.isArray(panel.drawing.paths));
      await inspectSvg(panel.image, panel.drawing);
    }
    assert.equal(row.panels[0].drawing.flipY, row.panels[2].drawing.flipY, row.id + ': overlay presentation faces match');
    await inspectSvg(row.frameOverlay, row.panels[0].drawing, true);
  }
  const archives = [];
  let tickMinimum = null, tickMaximum = null, nonUnitAddressNodes = 0;
  function tick(value) {
    assert.ok(typeof value === 'number' ? Number.isSafeInteger(value) : typeof value === 'string' && /^-?\d+$/.test(value),
      'native locations retain exact integer coordinates');
    const integer = BigInt(value);
    if (tickMinimum === null || integer < tickMinimum) tickMinimum = integer;
    if (tickMaximum === null || integer > tickMaximum) tickMaximum = integer;
    if (integer !== 0n && integer !== 1n) nonUnitAddressNodes++;
  }
  for (const file of native) {
    const graph = JSON.parse(zlib.gunzipSync(await fs.readFile(file)).toString('utf8'));
    assert.equal(graph.version, 3, path.basename(file));
    assert.ok(graph.sourceId && graph.nodes.length > 0 && graph.carriers.length > 0);
    const nodes = new Map(graph.nodes.map(node => [node.id, node]));
    assert.equal(nodes.size, graph.nodes.length, 'native identities distinct');
    for (const carrier of graph.carriers) {
      assert.equal(carrier.value ?? 0, 0, 'native construction holders retain zero value');
      assert.equal(carrier.tension ?? 0, 0, 'observational native holders remain quiet');
      if (carrier.valueTick) assert.equal(nodes.get(carrier.valueTick)?.ticks, 0);
      if (carrier.tensionTick) assert.equal(nodes.get(carrier.tensionTick)?.ticks, 0);
    }
    for (const node of graph.nodes) {
      // Repeat controllers and their timeline sites may use ordinal addresses
      // beyond 0/1. This generic transport audit does not classify a location's
      // mathematical role; native readers and focused tests establish that.
      if (node.kind !== 'placeholder' && node.ticks != null) tick(node.ticks);
      if (node.start != null) tick(node.start);
      if (node.end != null) tick(node.end);
    }
    archives.push({ file: path.relative(directory, file), nodes: graph.nodes.length, carriers: graph.carriers.length });
  }
  assert.ok(curves > 0 && filledPaths > 0, 'curved and filled constructions present');
  const report = { cases: data.examples.length, svgFiles: images.size, paths, curves, filledPaths,
    visibilityMasks: masks, evidenceRows, nativeArchives: archives,
    nativeLocationAudit: { minimum: tickMinimum?.toString(), maximum: tickMaximum?.toString(),
      nonUnitAddressNodes, qualification: 'Integer location range only. Generic JSON inspection does not distinguish timeline ordinals, unit/port addresses and geometric participation.' } };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const screenshots = path.resolve(__dirname, '../../../artifacts/' + path.basename(directory) + '-qa');
  await fs.mkdir(screenshots, { recursive: true });
  const errors = [], references = new Set(); let browserStates = 0;
  try {
    const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } });
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
        if (url.protocol === 'file:') { assert.equal(url.hash, '', 'no fragile navigation fragments'); references.add(url.href); }
      }
      browserStates++;
    }
    await page.goto(pathToFileURL(gallery).href);
    await page.waitForFunction(() => globalThis.ORNAMENT_COMPOSITION && document.querySelector('#case')?.options.length);
    assert.deepEqual(await page.evaluate(() => globalThis.ORNAMENT_COMPOSITION), data);
    for (const [index, row] of data.examples.entries()) {
      await page.selectOption('#case', row.id);
      assert.equal(await page.locator('.case').getAttribute('data-case'), row.id);
      assert.equal(await page.locator('.panel').count(), 4);
      assert.equal(await page.locator('.evidence tbody tr').count(), row.evidence.length);
      assert.equal(new URL(page.url()).searchParams.get('case'), row.id);
      await assets();
      await page.locator('.hero').screenshot({ path: path.join(screenshots, row.id + '-design.png') });
      await page.click('#alternate-view'); await assets();
      assert.equal(await page.locator('#alternate-view').getAttribute('aria-pressed'), 'true');
      assert.equal(await page.locator('#hero-image').getAttribute('src'), row.panels[3].image);
      await page.locator('.hero').screenshot({ path: path.join(screenshots, row.id + '-alternate.png') });
      await page.check('#frame-overlay'); await assets(); assert.ok(await page.locator('#frame-image').isVisible());
      await page.click('#design-view'); await assets();
      assert.equal(await page.locator('#hero-image').getAttribute('src'), row.panels[2].image);
      if (index === 0 || index === Math.floor(data.examples.length / 2) || index === data.examples.length - 1) {
        await page.locator('.comparison').screenshot({ path: path.join(screenshots, row.id + '-comparison.png') });
        await page.evaluate(() => scrollTo(0, 0));
        await page.waitForTimeout(60);
        await page.screenshot({ path: path.join(screenshots, row.id + '-desktop.png'), fullPage: true });
      }
    }
    const linked = data.examples[Math.floor(data.examples.length / 2)];
    await page.goto(pathToFileURL(gallery).href + '?case=' + encodeURIComponent(linked.id));
    await page.waitForFunction(id => document.querySelector('#case')?.value === id, linked.id); await assets();
    await page.setViewportSize({ width: 430, height: 932 });
    for (const row of [data.examples[0], linked, data.examples.at(-1)]) {
      await page.selectOption('#case', row.id); await assets();
      await page.click('#alternate-view'); await page.check('#frame-overlay'); await assets();
    }
    await page.evaluate(() => scrollTo(0, 0)); await page.waitForTimeout(60);
    await page.screenshot({ path: path.join(screenshots, 'mobile.png'), fullPage: true });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await assets();
    assert.equal(await page.locator('.tile').count(), data.examples.length);
    assert.equal(await page.locator('.tile img').count(), data.examples.length * 2);
    await page.screenshot({ path: path.join(screenshots, 'overview-mobile.png'), fullPage: false });
    await page.setViewportSize({ width: 1600, height: 1100 }); await assets();
    await page.screenshot({ path: path.join(screenshots, 'overview-desktop.png'), fullPage: false });
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, []);
    process.stdout.write(JSON.stringify({ ...report, browserStates, localReferences: references.size, browserErrors: errors, screenshots }) + '\n');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
