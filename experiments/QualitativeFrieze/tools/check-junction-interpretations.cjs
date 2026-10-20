// Verify the generated junction-interpretation study against its captured native reads.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { fileURLToPath, pathToFileURL } = require('node:url');
const { gunzipSync } = require('node:zlib');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');

const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/junction-interpretations/gallery.html'));
const directory = path.dirname(gallery);
const screenshots = process.env.GOJO_SCREENSHOTS || path.join(os.tmpdir(), 'GojoJunctionInterpretationsQA');
const checkedLinks = new Set();
const browserErrors = [];

async function checkReference(raw, base) {
  const url = new URL(raw, base);
  if (url.protocol !== 'file:') return;
  const filename = fileURLToPath(url);
  if (checkedLinks.has(url.href)) return;
  const info = await fs.stat(filename);
  assert.ok(info.isFile() && info.size > 0, 'Missing or empty local asset: ' + url.href);
  if (url.hash && filename.endsWith('.html')) {
    const html = await fs.readFile(filename, 'utf8');
    const id = decodeURIComponent(url.hash.slice(1));
    assert.ok(new RegExp('id=["\\\']' + id.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '["\\\']').test(html),
      'Missing fragment: ' + url.href);
  }
  checkedLinks.add(url.href);
}

async function checkPageAssets(page) {
  const references = await page.locator('[href], [src], object[data]').evaluateAll(items => items.flatMap(item =>
    ['href', 'src', 'data'].filter(key => item.hasAttribute(key)).map(key => item.getAttribute(key))));
  for (const raw of references) await checkReference(raw, page.url());
  assert.deepEqual(await page.locator('img').evaluateAll(images => images
    .filter(img => !img.complete || img.naturalWidth === 0).map(img => img.src)), [], 'Broken displayed images');
}

async function waitForImages(page) {
  await page.waitForFunction(() => [...document.images].every(img => img.complete && img.naturalWidth > 0));
}

async function assertNoOverflow(page) {
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
    'Horizontal page overflow at ' + JSON.stringify(page.viewportSize()) + ': ' + page.url());
}

async function checkSvg(filename) {
  await checkReference(filename, pathToFileURL(gallery));
  const svg = await fs.readFile(path.join(directory, filename), 'utf8');
  assert.match(svg, /<svg\b/, filename);
  assert.doesNotMatch(svg, /(?:NaN|Infinity)/, filename);
  return svg;
}

function assertSourceFacts(item, expected) {
  for (const [key, value] of Object.entries(expected))
    assert.equal(item.sourceFacts[key], value, item.id + ' actual source ' + key);
}

function checkNativeSemantics(captured) {
  assert.equal(captured.schema, 'junction-interpretations-v1');
  assert.equal(captured.nativeNumericElements, 0);
  assert.equal(captured.archiveRoundTrip, true);
  assert.equal(captured.cases.length, 8);
  assert.deepEqual(captured.cases.map(item => item.id), ['J01', 'J02', 'J03', 'J04', 'J05', 'J06', 'J07', 'J08']);
  const byId = Object.fromEntries(captured.cases.map(item => [item.id, item]));
  assertSourceFacts(byId.J01, { joinedSites: 0, strokePairConnections: 0, components: 2, endpoints: 4, x: 0 });
  assertSourceFacts(byId.J02, { joinedSites: 1, strokePairConnections: 6, components: 1, endpoints: 4, x: 1 });
  assertSourceFacts(byId.J03, { joinedSites: 1, strokePairConnections: 3, components: 1, endpoints: 3, y: 1 });
  assertSourceFacts(byId.J04, { joinedSites: 1, strokePairConnections: 3, components: 1, endpoints: 3, t: 1 });
  assertSourceFacts(byId.J05, { joinedSites: 0, strokePairConnections: 0, components: 2, endpoints: 4, t: 0 });
  assertSourceFacts(byId.J06, { joinedSites: 0, strokePairConnections: 0, components: 3, endpoints: 6 });
  assertSourceFacts(byId.J07, { joinedSites: 0, strokePairConnections: 0, components: 3, endpoints: 6 });
  assertSourceFacts(byId.J08, { joinedSites: 0, strokePairConnections: 0, components: 2, endpoints: 4, x: 0 });
  assert.equal(byId.J01.inferenceFacts.projectedCrossings, 1);
  assert.equal(byId.J02.inferenceFacts.projectedCrossings, 0);
  assert.equal(byId.J05.inferenceFacts.projectedCrossings, 1);
  assert.equal(byId.J01.inferenceFacts.strictCrossings, 1);
  assert.equal(byId.J01.inferenceFacts.projectedContacts, 0);
  assert.equal(byId.J05.inferenceFacts.strictCrossings, 0);
  assert.equal(byId.J05.inferenceFacts.projectedContacts, 1);
  assert.equal(byId.J06.inferenceFacts.facingGaps, 1);
  assert.equal(byId.J07.inferenceFacts.facingGaps, 0);
  assert.ok(byId.J07.inferenceFacts.blocked > 0, 'Offset gap retains blocked collinearity evidence');
  assert.ok(byId.J08.inferenceFacts.open > 0, 'Angular intersection remains open under narrow reader');
  for (const item of captured.cases) {
    assert.equal(item.candidates.length, item.inferenceFacts.candidates, item.id + ' renders all captured hypotheses');
    for (const candidate of item.candidates) {
      assert.ok(candidate.obligationCount > 0, item.id + '/' + candidate.id + ' preserves open obligations');
      assert.ok(candidate.virtualComponents <= item.sourceFacts.components,
        item.id + '/' + candidate.id + ' virtual continuation cannot create a component');
      if (candidate.bridgeCount === 0) assert.equal(candidate.virtualComponents, item.sourceFacts.components,
        item.id + '/' + candidate.id + ' depth/cut proposals do not weld topology');
    }
  }
  assert.ok(byId.J06.candidates.some(candidate => candidate.bridgeCount > 0), 'Gap supports a continuation proposal');
  assert.ok(byId.J06.candidates.some(candidate => candidate.bridgeCount === 0 && candidate.cutCount > 0),
    'Deliberate cut remains a competing reading');
  assert.ok(byId.J06.candidates.some(candidate => candidate.virtualComponents < byId.J06.sourceFacts.components),
    'A bridge changes only the hypothetical component read');
  assert.ok(byId.J01.candidates.every(candidate => candidate.bridgeCount === 0),
    'Depth proposals at a crossing never create a junction');
}

const tagCount = (svg, className) => [...svg.matchAll(new RegExp(`class=['"]${className}['"]`, 'g'))].length;
const geometry = svg => [...svg.matchAll(/class=['"]proposed-foreground['"][^>]*\bd=['"]([^'"]+)/g)].map(match => match[1]);

(async () => {
  const captured = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  checkNativeSemantics(captured);
  const archive = JSON.parse(gunzipSync(await fs.readFile(path.join(directory, 'junctions.gojo.json.gz'))).toString('utf8'));
  assert.ok(archive && typeof archive === 'object', 'Native archive is readable gzip JSON');
  let candidateChecks = 0;
  const candidateSvgs = new Map();
  for (const item of captured.cases) {
    const source = await checkSvg(item.source);
    const actual = await checkSvg(item.actual);
    const cues = await checkSvg(item.cues);
    for (const svg of [source, actual, cues]) assert.equal(tagCount(svg, 'observed'), item.sourceFacts.strokes,
      item.id + ' preserves every actual observed stroke');
    assert.equal(tagCount(source, 'actual-join'), 0, item.id + ' observation does not reveal hidden topology');
    assert.equal(tagCount(source, 'endpoint'), 0, item.id + ' observation keeps site markers in its own view');
    assert.equal(tagCount(actual, 'actual-join'), item.sourceFacts.joinedSites, item.id + ' marks only actual joins');
    assert.equal(tagCount(actual, 'endpoint'), item.sourceFacts.endpoints, item.id + ' marks actual endpoints');
    assert.equal(tagCount(actual, 'projected-cue'), 0, item.id + ' actual topology is distinct from projected cue evidence');
    assert.equal(tagCount(cues, 'projected-cue'), item.inferenceFacts.projectedCrossings,
      item.id + ' renders every certified projected intersection');
    assert.equal(tagCount(cues, 'actual-join'), 0, item.id + ' projected rings are not actual join dots');
    for (const candidate of item.candidates) {
      const svg = await checkSvg(candidate.image);
      candidateSvgs.set(candidate.id, svg);
      assert.equal(tagCount(svg, 'observed'), item.sourceFacts.strokes, candidate.id + ' source strokes unchanged');
      assert.equal(tagCount(svg, 'proposed-continuation'), candidate.bridgeCount, candidate.id + ' renders actual bridge proposal');
      assert.equal(tagCount(svg, 'actual-join'), 0, candidate.id + ' selecting an interpretation never installs a junction');
      assert.equal(tagCount(svg, 'projected-cue'), item.inferenceFacts.projectedCrossings);
      assert.equal(tagCount(svg, 'proposed-foreground'), candidate.depthCount,
        candidate.id + ' makes each proposed ordering inspectable');
    }
  }
  assert.notDeepEqual(geometry(candidateSvgs.get('J01-H1')), geometry(candidateSvgs.get('J01-H2')),
    'Opposed front/back proposals must differ in rendered foreground, not just their caption');

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
    assert.equal(await page.locator('article.case').count(), captured.cases.length);
    await checkPageAssets(page);
    await assertNoOverflow(page);

    for (const item of captured.cases) {
      const card = page.locator(`article.case[data-case="${item.id}"]`);
      assert.deepEqual(await card.locator('.observation-facts dd').allTextContents(),
        [item.sourceFacts.strokes, item.sourceFacts.sites, 0].map(String));
      const actualFacts = await card.locator('.source-facts').textContent();
      const actualDrawing = await card.locator('.panel').nth(1).locator('img').getAttribute('src');
      assert.equal(actualDrawing, item.actual);
      assert.deepEqual(await card.locator('.source-facts dd').allTextContents(), [item.sourceFacts.joinedSites,
        item.sourceFacts.strokePairConnections, item.sourceFacts.components, item.sourceFacts.endpoints,
        `${item.sourceFacts.t} / ${item.sourceFacts.y} / ${item.sourceFacts.x}`, item.sourceFacts.l].map(String));
      assert.equal(await card.locator('.hypothesis-choice option').count(), item.candidates.length + 1);
      const sourceAtStart = await page.locator('article.case .source-facts').allTextContents();
      const candidateBefore = await page.locator('article.case').evaluateAll(cards => cards.map(article => article.dataset.hypothesis));
      const sequence = ['-1', ...item.candidates.map((_, index) => String(index)), '-1'];
      for (const selected of sequence) {
        await card.locator('.hypothesis-choice').selectOption(selected);
        const candidate = selected === '-1' ? null : item.candidates[Number(selected)];
        assert.equal(await card.getAttribute('data-hypothesis'), candidate ? candidate.id : 'cues');
        assert.equal(await card.locator('.candidate-image').getAttribute('src'), candidate ? candidate.image : item.cues);
        assert.equal(await card.locator('.candidate-link').getAttribute('href'), candidate ? candidate.image : item.cues);
        assert.equal(await card.locator('.source-facts').textContent(), actualFacts, item.id + ' source facts survive every interpretation');
        assert.equal(await card.locator('.panel').nth(1).locator('img').getAttribute('src'), actualDrawing);
        assert.deepEqual(await page.locator('article.case .source-facts').allTextContents(), sourceAtStart,
          item.id + ' never changes actual topology in any card');
        const otherAfter = await page.locator('article.case').evaluateAll(cards => cards.map(article => article.dataset.hypothesis));
        otherAfter.forEach((value, index) => { if (captured.cases[index].id !== item.id) assert.equal(value, candidateBefore[index],
          item.id + ' selection must not change another source case'); });
        assert.deepEqual(await card.locator('.candidate-facts dd').allTextContents(), [item.inferenceFacts.strictCrossings,
          item.inferenceFacts.projectedContacts, item.inferenceFacts.facingGaps, item.inferenceFacts.candidates,
          item.inferenceFacts.blocked, item.inferenceFacts.open].map(String));
        const status = await card.locator('.status').textContent();
        if (candidate) {
          assert.match(status, /Open hypothesis.*source remains unchanged/);
          assert.match(await card.locator('.candidate-description').textContent(),
            new RegExp(`Components under this observational reading: ${candidate.virtualComponents}; actual source: ${item.sourceFacts.components}`));
        } else if (item.inferenceFacts.open > 0) assert.match(status, /open evidence items.*missing read is not a negative finding/);
        else if (item.candidates.length === 0) assert.match(status, /No candidate proposed by this bounded reader/);
        await waitForImages(page);
        await checkPageAssets(page);
        if (candidate && ['J01', 'J06'].includes(item.id))
          await card.screenshot({ path: path.join(screenshots, candidate.id + '-reading.png') });
        candidateChecks++;
      }
      if (item.candidates.length) await card.locator('.hypothesis-choice').selectOption('0');
    }

    for (const id of ['J01', 'J02', 'J05', 'J06', 'J08'])
      await page.locator(`article.case[data-case="${id}"]`).screenshot({ path: path.join(screenshots, id + '-readings.png') });
    await page.screenshot({ path: path.join(screenshots, 'junction-interpretations-desktop.png'), fullPage: true });
    await page.goto(pathToFileURL(gallery).href + '?case=J06');
    await waitForImages(page);
    assert.ok(await page.locator('#J06').evaluate(article => Math.abs(article.getBoundingClientRect().top) < 100),
      'Query navigation reaches the requested case');
    await page.goto(pathToFileURL(gallery).href + '?case=not-a-case');
    await waitForImages(page);
    assert.equal(await page.locator('article.case').count(), 8, 'An unknown case query leaves the gallery available');

    const overview = pathToFileURL(path.join(directory, 'overview.html')).href;
    await page.goto(overview);
    await waitForImages(page);
    assert.equal(await page.locator('article').count(), 8);
    assert.equal(await page.locator('img').count(), 24);
    await checkPageAssets(page);
    await assertNoOverflow(page);
    await page.screenshot({ path: path.join(screenshots, 'junction-interpretations-overview.png'), fullPage: true });

    for (const route of [pathToFileURL(gallery).href, overview]) {
      await page.setViewportSize({ width: 390, height: 844 });
      await page.goto(route);
      await waitForImages(page);
      await checkPageAssets(page);
      await assertNoOverflow(page);
      const label = route === overview ? 'overview-mobile' : 'mobile';
      await page.screenshot({ path: path.join(screenshots, 'junction-interpretations-' + label + '.png'), fullPage: true });
      await page.screenshot({ path: path.join(screenshots, 'junction-interpretations-' + label + '-top.png') });
    }
    assert.deepEqual(browserErrors, []);
    console.log(`Verified 8 native source/read cases, ${candidateChecks} interpretation selections, actual-vs-projected markers, distinct depth ordering, non-mutating hypotheses, ${checkedLinks.size} local references, readable native gzip JSON, query navigation, desktop/mobile layouts and browser errors.`);
    console.log('Screenshots: ' + screenshots);
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
