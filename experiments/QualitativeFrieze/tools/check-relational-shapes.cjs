const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const zlib = require('node:zlib');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/relational-shapes/gallery.html'));
const directory = path.dirname(gallery);

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'gallery-data.json'), 'utf8'));
  assert.equal(data.schema, 'relational-shape-gallery-v1');
  assert.ok(data.examples.length > 0);
  assert.equal(new Set(data.examples.map(row => row.id)).size, data.examples.length);
  try {
    const summary = JSON.parse(await fs.readFile(path.join(directory, 'summary.json'), 'utf8'));
    assert.deepEqual(data.summary, summary);
    const count = summary.sources ?? summary.cases ?? summary.samples;
    if (typeof count === 'number') assert.equal(count, data.examples.length);
  } catch (error) { if (error.code !== 'ENOENT') throw error; }
  const images = new Map(); let profiles = 0, attempts = 0;
  function image(file, assumed) {
    assert.equal(typeof file, 'string'); assert.ok(file.endsWith('.svg'));
    assert.ok(!path.isAbsolute(file) && !file.includes('#'));
    assert.ok(path.resolve(directory, file).startsWith(directory + path.sep));
    assert.ok(!images.has(file), `Unique file for ${file}`); images.set(file, assumed);
  }
  for (const row of data.examples) {
    assert.ok(row.id && row.name && row.evidence);
    assert.ok(Array.isArray(row.evidence.joints));
    assert.equal(new Set(row.evidence.joints.map(joint => joint.id)).size, row.evidence.joints.length);
    assert.ok(Array.isArray(row.evidence.streams));
    for (const stream of row.evidence.streams) {
      assert.ok(stream.context && stream.claim);
      assert.ok(stream.support !== undefined && (stream.opposition !== undefined || stream.doubt !== undefined));
    }
    assert.ok(row.results.length > 0);
    assert.equal(new Set(row.results.map(result => result.id)).size, row.results.length);
    image(row.sourceImage, 0);
    for (const result of row.results) {
      profiles++;
      assert.ok(result.id && result.name && result.feedback);
      assert.ok(Number.isInteger(result.assumptions) && result.assumptions >= 0);
      image(result.image, result.assumptions);
      assert.equal(result.attempts.length, 3);
      assert.equal(result.attempts.filter(attempt => attempt.selected).length, 1);
      assert.equal(new Set(result.attempts.map(attempt => attempt.index)).size, 3);
      for (const attempt of result.attempts) {
        attempts++;
        assert.ok(attempt.name && attempt.feedback);
        assert.ok(Number.isInteger(attempt.assumptions) && attempt.assumptions >= 0);
        image(attempt.image, attempt.assumptions);
      }
    }
  }
  for (const [file, assumed] of images) {
    const svg = await fs.readFile(path.join(directory, file), 'utf8');
    assert.ok(svg.includes('<svg') && svg.includes('</svg>'), file);
    assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg), file);
    const dashed = [...svg.matchAll(/<path\b[^>]*stroke-dasharray=/g)].length;
    assert.equal(dashed, assumed, `${file}: native assumptions must be visibly dashed`);
  }
  const archiveFiles = (await fs.readdir(directory)).filter(file => file.endsWith('.gojo.json.gz'));
  const archives = [];
  for (const file of archiveFiles) {
    const graph = JSON.parse(zlib.gunzipSync(await fs.readFile(path.join(directory, file))).toString('utf8'));
    assert.equal(graph.version, 3, file);
    assert.ok(typeof graph.sourceId === 'string' && graph.sourceId.length > 0, file);
    assert.equal(graph.carriers.length, 0, file);
    assert.ok(graph.nodes.length > 0, file);
    archives.push({ file, sourceId: graph.sourceId, nodes: graph.nodes.length });
  }
  const report = { cases: data.examples.length, profiles, retainedAttempts: attempts, svgFiles: images.size, nativeArchives: archives };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }
  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const screenshots = path.resolve(__dirname, '../../../artifacts/relational-shapes-qa');
  await fs.mkdir(screenshots, { recursive: true });
  const errors = [], references = new Set(); let states = 0;
  try {
    const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
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
    await page.waitForFunction(() => globalThis.RELATIONAL_SHAPES && document.querySelector('#case')?.options.length);
    assert.deepEqual(await page.evaluate(() => globalThis.RELATIONAL_SHAPES), data);
    const screenshotCases = new Set([0, 7, 14, 15, 63, data.examples.length - 1]);
    for (const [index, row] of data.examples.entries()) {
      await page.selectOption('#case', row.id);
      assert.equal(await page.locator('.profile').count(), row.results.length);
      assert.equal(await page.locator('.context').count(), 4);
      for (const result of row.results) {
        await page.locator('.profile-choice').evaluateAll((buttons, id) => buttons.find(button => button.dataset.profile === id).click(), result.id);
        await page.waitForFunction(id => document.querySelector('.attempt-section')?.dataset.profile === id, result.id);
        assert.equal(await page.locator('.attempt').count(), 3);
        assert.equal(await page.locator('.attempt.selected').count(), 1);
        assert.equal(await page.locator('.profile.active').count(), 1);
        assert.equal(new URL(page.url()).searchParams.get('case'), row.id);
        assert.equal(new URL(page.url()).searchParams.get('profile'), result.id);
        await assets();
      }
      if (screenshotCases.has(index)) {
        await page.screenshot({ path: path.join(screenshots, row.id + '-desktop.png'), fullPage: true });
        await page.locator('.profiles').screenshot({ path: path.join(screenshots, row.id + '-profiles.png') });
      }
    }
    const linked = data.examples[Math.min(15, data.examples.length - 1)];
    const linkedProfile = linked.results.at(-1);
    await page.goto(pathToFileURL(gallery).href + '?case=' + encodeURIComponent(linked.id) + '&profile=' + encodeURIComponent(linkedProfile.id));
    await page.waitForFunction(({ id, profile }) => document.querySelector('#case')?.value === id &&
      document.querySelector('.attempt-section')?.dataset.profile === profile, { id: linked.id, profile: linkedProfile.id });
    await assets();
    await page.setViewportSize({ width: 430, height: 932 });
    for (const row of [data.examples[0], linked, data.examples.at(-1)]) {
      await page.selectOption('#case', row.id); await assets();
      assert.equal(await page.locator('.attempt.selected').count(), 1);
    }
    await page.screenshot({ path: path.join(screenshots, 'mobile.png'), fullPage: true });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await assets();
    assert.equal(await page.locator('.tile').count(), data.examples.length);
    await page.screenshot({ path: path.join(screenshots, 'overview-mobile.png'), fullPage: false });
    await page.setViewportSize({ width: 1500, height: 1100 }); await assets();
    await page.screenshot({ path: path.join(screenshots, 'overview-desktop.png'), fullPage: false });
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, []);
    Object.assign(report, { browserStates: states, localReferences: references.size, errors: errors.length, screenshots });
  } finally { await browser.close(); }
  process.stdout.write(JSON.stringify(report) + '\n');
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
