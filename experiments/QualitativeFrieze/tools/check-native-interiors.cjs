const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/native-interiors/gallery.html'));
const directory = path.dirname(gallery);
const statuses = ['Supported', 'Possible', 'Open', 'Refuted'];
const statusText = { Supported: 'Supported', Possible: 'Conditional', Open: 'Unresolved', Refuted: 'Contradicted' };
const featureLabel = name => ({ 'Star-like': 'Distributed alternating turns · star candidate',
  'Contains notch': 'Paired inset / notch' })[name] || name;
const names = ['Closed', 'Elementary', 'Triangle', 'Four-sided', 'Hexagon', 'Rectangle', 'Equal-sided', 'Square',
  'Contains concavity', 'Contains notch', 'Contains zigzag', 'Star-like'];
const images = new Map();

(async () => {
  const d = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(d.schema, 'native-interiors-v1');
  assert.deepEqual(JSON.parse((await fs.readFile(path.join(directory, 'data.js'), 'utf8'))
    .replace(/^globalThis\.NATIVE_INTERIORS\s*=\s*/, '').replace(/;\s*$/, '')), d);
  assert.equal(d.sources.length, 20); assert.equal(d.counts.sources, 20);
  assert.equal(new Set(d.sources.map(source => source.id)).size, d.sources.length);
  let boundaries = 0, conditional = 0, claims = 0;
  const statusCounts = {};
  for (const s of d.sources) {
    assert.ok(s.sourceStrokes > 0 && s.sourceStrokes <= 160, s.id + ': bounded source patch');
    assert.ok(s.observationEdges >= 0 && s.observationNodes > 0);
    assert.equal(s.boundaries.length, s.observedBoundaries + s.conditionalBoundaries);
    assert.equal(new Set(s.boundaries.map(b => b.id)).size, s.boundaries.length);
    images.set(s.sourceImage, { kind: 'source', edges: s.observationEdges });
    images.set(s.mapImage, { kind: 'map', edges: s.observationEdges, boundaries: s.observedBoundaries });
    const supportedFeatures = {};
    for (const b of s.boundaries) {
      boundaries++; if (b.conditional) conditional++;
      assert.ok(b.observedSegments >= 3); assert.ok(b.sideCount >= 3 && b.sideCount <= b.observedSegments);
      assert.deepEqual(b.claims.map(c => c.name), names);
      assert.equal(b.conditional, b.id.startsWith('H'));
      const elementary = b.claims.find(c => c.name === 'Elementary');
      if (b.obstructions > 0) assert.equal(elementary.status, 'Refuted', 'a known island prevents an elementary-cell claim');
      if (!b.conditional && elementary.status === 'Supported') {
        assert.equal(s.unknownEmbedding, 0); assert.equal(b.obstructions, 0); assert.equal(b.unknownInterior, 0);
        for (const c of b.claims.filter(c => c.status === 'Supported')) supportedFeatures[c.name] = (supportedFeatures[c.name] || 0) + 1;
      }
      const closed = b.claims.find(c => c.name === 'Closed');
      assert.equal(closed.answer, 'Yes'); assert.equal(closed.status, b.conditional ? 'Possible' : 'Supported');
      for (const c of b.claims) {
        claims++; statusCounts[c.status] = (statusCounts[c.status] || 0) + 1;
        assert.ok(statuses.includes(c.status));
        assert.ok(['Yes', 'No', 'Open'].includes(c.answer));
        assert.equal(c.assumptions > 0, b.conditional, 'only completion contexts carry assumed passages');
        if (c.answer === 'Yes') {
          assert.equal(c.status, c.assumptions ? 'Possible' : 'Supported');
          assert.ok(c.support > 0); assert.equal(c.refutation, 0); assert.equal(c.open, 0);
        } else if (c.answer === 'No') {
          assert.equal(c.status, 'Refuted'); assert.ok(c.refutation > 0); assert.equal(c.support, 0); assert.equal(c.open, 0);
        } else {
          assert.equal(c.status, 'Open'); assert.ok(c.open > 0); assert.equal(c.support, 0); assert.equal(c.refutation, 0);
        }
      }
      images.set(b.image, { kind: 'boundary', conditional: b.conditional, selected: b.observedSegments });
    }
    assert.deepEqual(s.supportedFeatures, supportedFeatures, 'source totals include only supported elementary interiors');
  }
  assert.equal(d.counts.boundaries, boundaries); assert.equal(d.counts.conditional, conditional);
  const source = id => { const s = d.sources.find(s => s.id === id); assert.ok(s, id); return s; };
  const claim = (b, name) => { const c = b.claims.find(c => c.name === name); assert.ok(c, name); return c; };
  const grid = source('I01'), divided = source('I16'), gap = source('I15');
  assert.equal(grid.observedBoundaries, 4); assert.equal(grid.conditionalBoundaries, 0);
  assert.equal(grid.supportedFeatures.Square, 4, 'only four elementary cells, not the enclosing square');
  assert.equal(divided.observedBoundaries, 2); assert.equal(divided.conditionalBoundaries, 0);
  assert.ok(divided.boundaries.every(b => b.sideCount === 4 && claim(b, 'Elementary').status === 'Supported'));
  assert.equal(gap.observedBoundaries, 0); assert.equal(gap.proposals, 1); assert.equal(gap.conditionalBoundaries, 1);
  assert.equal(claim(gap.boundaries[0], 'Square').status, 'Possible', 'the missing side is conditional rather than observed');

  const svgTexts = [];
  for (const [file, expected] of images) {
    assert.ok(!file.includes('#') && !path.isAbsolute(file));
    const filename = path.resolve(directory, file);
    assert.ok(filename.startsWith(path.resolve(directory) + path.sep), 'artifact images remain inside their result directory');
    const svg = await fs.readFile(filename, 'utf8');
    assert.ok(svg.includes('<svg') && svg.includes('</svg>')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg));
    if (expected.kind === 'source' || expected.kind === 'map')
      assert.equal([...svg.matchAll(/class='observed-edge'/g)].length, expected.edges, file + ': native edge pieces');
    if (expected.kind === 'map') assert.equal([...svg.matchAll(/class='interior-region'/g)].length, expected.boundaries);
    if (expected.kind === 'boundary') {
      assert.equal([...svg.matchAll(/class='selected-boundary(?: assumed-edge)?'/g)].length, expected.selected);
      assert.equal(svg.includes("stroke-dasharray='8 6'"), expected.conditional, 'only assumed closures use dashed amber ink');
      assert.equal(svg.includes('assumed-edge'), expected.conditional);
    }
    svgTexts.push({ file, svg });
  }
  const report = { sources: d.sources.length, boundaries, conditional, claims, statusCounts, svgFiles: images.size };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const screenshots = path.resolve(__dirname, '../../../artifacts/native-interiors-qa'); await fs.mkdir(screenshots, { recursive: true });
  const errors = [], references = new Set(); let browserStates = 0;
  try {
    const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => { if (request.failure()?.errorText !== 'net::ERR_ABORTED') errors.push(request.url()); });
    async function assets() {
      await page.waitForFunction(() => [...document.querySelectorAll('img[src]')].filter(image => image.getClientRects().length)
        .every(image => image.complete && image.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'no horizontal page overflow: ' + page.url());
      for (const raw of await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element => ['href', 'src']
        .filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))))) {
        assert.ok(!raw.includes('#'), 'local artifact links use query parameters rather than fragments');
        const url = new URL(raw, page.url()); if (url.protocol === 'file:') references.add(url.href);
      }
      browserStates++;
    }
    async function inspect(s, b) {
      await page.evaluate(({ source, boundary }) => {
        const sourceSelect = document.getElementById('source');
        if (sourceSelect.value !== source) { sourceSelect.value = source; sourceSelect.dispatchEvent(new Event('change')); }
        if (boundary) {
          const boundarySelect = document.getElementById('boundary');
          boundarySelect.value = boundary; boundarySelect.dispatchEvent(new Event('change'));
        }
      }, { source: s.id, boundary: b?.id });
      await assets();
      assert.equal(await page.locator('#source').inputValue(), s.id);
      assert.equal(await page.locator('#boundary option').count(), s.boundaries.length);
      assert.equal(await page.locator('#source-image').getAttribute('src'), s.sourceImage);
      assert.equal(await page.locator('#map-image').getAttribute('src'), s.mapImage);
      assert.equal(await page.locator('#selected-image').isVisible(), Boolean(b));
      assert.equal(await page.locator('#features').isVisible(), Boolean(b));
      assert.equal(await page.locator('#empty').isVisible(), !b);
      const url = new URL(page.url()); assert.equal(url.searchParams.get('source'), s.id); assert.equal(url.hash, '');
      assert.equal(url.searchParams.get('boundary'), b?.id || null);
      if (b) {
        assert.equal(await page.locator('#selected-image').getAttribute('src'), b.image);
        assert.equal(await page.locator('#selected-title').innerText(), b.id + (b.conditional ? ' · assumed closure' : ' · observed boundary'));
        const visible = b.claims.filter(c => c.status !== 'Refuted'), refuted = b.claims.filter(c => c.status === 'Refuted');
        assert.deepEqual(await page.locator('#claims .claim').evaluateAll(elements => elements.map(element => ({
          name: element.dataset.claim, status: element.querySelector('.status').textContent,
          heading: element.querySelector('strong').textContent,
          conditional: element.classList.contains('possible'), open: element.classList.contains('open')
        }))), visible.map(c => ({ name: c.name, status: statusText[c.status], heading: featureLabel(c.name),
          conditional: c.status === 'Possible', open: c.status === 'Open' })));
        assert.deepEqual(await page.locator('#refuted .claim').evaluateAll(elements => elements.map(element => ({
          name: element.dataset.claim, status: element.querySelector('.status').textContent
        }))), refuted.map(c => ({ name: c.name, status: 'Contradicted' })));
        assert.equal(await page.locator('#boundary-list [aria-selected="true"]').getAttribute('data-boundary'), b.id);
        if (b.conditional) assert.ok((await page.locator('#selected-note').innerText()).includes('not observed source evidence'));
      } else {
        assert.equal(await page.locator('#selected-image').getAttribute('src'), null);
        assert.equal(await page.locator('#claims .claim').count(), 0); assert.equal(await page.locator('#refuted .claim').count(), 0);
      }
    }
    await page.goto(pathToFileURL(gallery).href);
    await page.waitForFunction(() => document.querySelector('#source')?.options.length === 20);
    const xmlErrors = await page.evaluate(items => items.flatMap(({ file, svg }) =>
      new DOMParser().parseFromString(svg, 'image/svg+xml').querySelector('parsererror') ? [file] : []), svgTexts);
    assert.deepEqual(xmlErrors, [], 'all SVG documents parse as XML');
    for (const s of d.sources) {
      if (s.boundaries.length === 0) await inspect(s, null);
      else for (const b of s.boundaries) await inspect(s, b);
    }
    await inspect(grid, grid.boundaries[0]);
    await page.screenshot({ path: path.join(screenshots, 'elementary-grid-interiors.png'), fullPage: true });
    await inspect(gap, gap.boundaries[0]);
    await page.screenshot({ path: path.join(screenshots, 'conditional-open-enclosure.png'), fullPage: true });
    const arms = source('I10');
    await inspect(arms, arms.boundaries.find(b => claim(b, 'Star-like').status === 'Supported') || arms.boundaries[0]);
    await page.screenshot({ path: path.join(screenshots, 'stepped-arm-feature-read.png'), fullPage: true });
    const busy = source('I11'); await inspect(busy, busy.boundaries[0] || null);
    await page.screenshot({ path: path.join(screenshots, 'serif-compartment-feature-read.png'), fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    await inspect(gap, gap.boundaries[0]);
    await page.screenshot({ path: path.join(screenshots, 'mobile-conditional-enclosure.png'), fullPage: true });
    await inspect(busy, busy.boundaries[0] || null);
    await page.screenshot({ path: path.join(screenshots, 'mobile-bourgoin-compartments.png'), fullPage: true });
    await page.setViewportSize({ width: 1500, height: 1100 });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href);
    await page.evaluate(() => document.querySelectorAll('img').forEach(image => { image.loading = 'eager'; })); await assets();
    assert.equal(await page.locator('#atlas [data-source]').count(), 20);
    assert.equal(await page.locator('#atlas img').count(), 20);
    await page.screenshot({ path: path.join(screenshots, 'all-native-lattice-interiors.png'), fullPage: true });
    await page.locator('#atlas [data-source="I16"] a').click(); await assets();
    assert.equal(await page.locator('#source').inputValue(), 'I16');
    const second = divided.boundaries.at(-1);
    await page.locator('#boundary-list [data-boundary="' + second.id + '"]').click(); await assets();
    assert.equal(await page.locator('#boundary').inputValue(), second.id);
    await page.reload(); await assets();
    assert.equal(await page.locator('#source').inputValue(), 'I16');
    assert.equal(await page.locator('#boundary').inputValue(), second.id, 'query URL preserves the selected boundary on reload');
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href);
    await page.evaluate(() => document.querySelectorAll('img').forEach(image => { image.loading = 'eager'; })); await assets();
    assert.equal(await page.locator('#atlas [data-source]').count(), 20);
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, [], 'browser errors');
    Object.assign(report, { browserStates, localReferences: references.size, screenshots, errors: 0 });
    await fs.writeFile(path.join(directory, 'browser-check.json'), JSON.stringify(report, null, 2) + '\n');
    process.stdout.write(JSON.stringify(report) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
