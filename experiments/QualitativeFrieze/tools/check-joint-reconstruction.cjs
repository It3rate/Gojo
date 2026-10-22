const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const zlib = require('node:zlib');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/joint-reconstruction/gallery.html'));
const directory = path.dirname(gallery);

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'gallery-data.json'), 'utf8'));
  const summary = JSON.parse(await fs.readFile(path.join(directory, 'summary.json'), 'utf8'));
  assert.equal(data.schema, 'joint-reconstruction-gallery-v1');
  assert.equal(summary.schema, 'joint-reconstruction-v1');
  assert.deepEqual(data.summary, summary);
  assert.equal(summary.sources, data.examples.length);
  assert.equal(summary.attempts, summary.sources * 3);
  assert.equal(summary.messageAblationAttempts, summary.sources * 9);
  assert.ok(data.examples.length > 0);
  assert.equal(new Set(data.examples.map(row => row.id)).size, data.examples.length);
  const images = new Map();
  const image = (file, assumed) => {
    assert.equal(typeof file, 'string'); assert.ok(file.endsWith('.svg'));
    assert.ok(!path.isAbsolute(file) && !file.includes('#'));
    assert.ok(path.resolve(directory, file).startsWith(directory + path.sep));
    assert.ok(!images.has(file)); images.set(file, assumed);
  };
  for (const row of data.examples) {
    assert.ok(row.id && row.name && row.description);
    assert.equal(row.attempts.length, 3);
    assert.equal(row.attempts.filter(attempt => attempt.selected).length, 1);
    assert.equal(new Set(row.attempts.map(attempt => attempt.index)).size, 3);
    const result = summary.results.find(result => result.id === row.id);
    assert.ok(result); assert.equal(result.attempts.length, 3);
    assert.equal(result.messageAblations.length, 3);
    assert.deepEqual(row.description.messageAblations, result.messageAblations);
    assert.equal(new Set(result.messageAblations.map(probe => probe.profile)).size, 3);
    const noOrder = result.messageAblations.find(probe => probe.profile === 'Without axis-order cues');
    assert.ok(noOrder); assert.equal(noOrder.strokes, 0); assert.ok(noOrder.open > 0);
    assert.equal(result.selected, row.attempts.find(attempt => attempt.selected).index);
    assert.ok(Array.isArray(row.description.joints));
    assert.equal(new Set(row.description.joints.map(joint => joint.id)).size, row.description.joints.length);
    for (const field of ['horizontalBuckets', 'verticalBuckets']) {
      assert.ok(Array.isArray(row.description[field]));
      const members = row.description[field].flat();
      assert.equal(new Set(members).size, members.length);
      assert.ok(members.every(member => row.description.joints.some(joint => joint.id === member)));
    }
    image(row.sourceImage, 0);
    for (const attempt of row.attempts) {
      assert.ok(Number.isInteger(attempt.assumptions) && attempt.assumptions >= 0);
      assert.ok(attempt.feedback && typeof attempt.feedback === 'object');
      const resultAttempt = result.attempts.find(result => result.attempt === attempt.index);
      assert.ok(resultAttempt);
      assert.equal(resultAttempt.assumptions, attempt.assumptions);
      assert.equal(resultAttempt.violated, attempt.feedback.violations);
      assert.equal(resultAttempt.open, attempt.feedback.open);
      image(attempt.image, attempt.assumptions);
    }
  }
  const selected = summary.results.map(result => result.attempts.find(attempt => attempt.attempt === result.selected));
  assert.equal(summary.exactReferenceConnectivitySelected, selected.filter(attempt =>
    attempt.reference.matched === attempt.reference.expected && attempt.reference.additional === 0).length);
  assert.equal(summary.selectedWithoutProvenViolation, selected.filter(attempt => attempt.violated === 0).length);
  assert.equal(summary.selectedWithAssumedClosure, selected.filter(attempt => attempt.assumptions > 0).length);
  const archives = [
    ['messages.gojo.json.gz', 'anonymous-joint-messages-v1'],
    ['receiver-episodes.gojo.json.gz', 'joint-reconstruction-receiver-v1'],
    ['audit.gojo.json.gz', 'joint-reconstruction-private-teacher-audit-v1']
  ];
  for (const [file, sourceId] of archives) {
    const graph = JSON.parse(zlib.gunzipSync(await fs.readFile(path.join(directory, file))).toString('utf8'));
    assert.equal(graph.version, 3); assert.equal(graph.sourceId, sourceId);
    assert.equal(graph.carriers.length, 0); assert.ok(graph.nodes.length > 0);
  }
  for (const [file, expectedAssumptions] of images) {
    const svg = await fs.readFile(path.join(directory, file), 'utf8');
    assert.ok(svg.includes('<svg') && svg.includes('</svg>'));
    assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg), file);
    const dashed = [...svg.matchAll(/<path\b[^>]*stroke-dasharray=/g)].length;
    assert.equal(dashed, expectedAssumptions, `${file}: assumed native strokes are rendered explicitly`);
  }
  const report = { cases: data.examples.length, attempts: data.examples.length * 3, ablationAttempts: summary.messageAblationAttempts, svgFiles: images.size, nativeArchives: archives.length };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }
  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const screenshots = path.resolve(__dirname, '../../../artifacts/joint-reconstruction-qa');
  await fs.mkdir(screenshots, { recursive: true });
  const errors = [], references = new Set(); let states = 0;
  try {
    const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => { if (request.failure()?.errorText !== 'net::ERR_ABORTED') errors.push(request.url()); });
    async function assets() {
      await page.waitForFunction(() => [...document.querySelectorAll('img[src]')].filter(image => image.getClientRects().length)
        .every(image => image.complete && image.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'no page overflow');
      for (const raw of await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element => ['href', 'src']
        .filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))))) {
        const url = new URL(raw, page.url());
        if (url.protocol === 'file:') { assert.equal(url.hash, ''); references.add(url.href); }
      }
      states++;
    }
    for (const [index, row] of data.examples.entries()) {
      await page.goto(pathToFileURL(gallery).href + '?case=' + encodeURIComponent(row.id));
      await page.waitForFunction(id => document.querySelector('#case')?.value === id, row.id);
      assert.equal(await page.locator('.attempt').count(), 3);
      assert.equal(await page.locator('.attempt.selected').count(), 1);
      assert.deepEqual(await page.evaluate(() => globalThis.JOINT_RECONSTRUCTION), data);
      await assets();
      if ([0, 4, 7, 11, 13].includes(index)) await page.screenshot({ path: path.join(screenshots, row.id + '-desktop.png'), fullPage: true });
    }
    await page.setViewportSize({ width: 430, height: 932 });
    for (const row of [data.examples[0], data.examples.find(row => row.id === 'J08'), data.examples.at(-1)].filter(Boolean)) {
      await page.selectOption('#case', row.id); await assets();
      assert.equal(new URL(page.url()).searchParams.get('case'), row.id);
    }
    await page.screenshot({ path: path.join(screenshots, 'mobile.png'), fullPage: true });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await assets();
    assert.equal(await page.locator('.tile').count(), data.examples.length);
    await page.screenshot({ path: path.join(screenshots, 'overview-mobile.png'), fullPage: true });
    await page.setViewportSize({ width: 1500, height: 1100 }); await assets();
    await page.screenshot({ path: path.join(screenshots, 'overview-desktop.png'), fullPage: true });
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, []);
    Object.assign(report, { browserStates: states, localReferences: references.size, errors: errors.length, screenshots });
  } finally { await browser.close(); }
  process.stdout.write(JSON.stringify(report) + '\n');
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
