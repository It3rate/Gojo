const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const zlib = require('node:zlib');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/ornament-learning/gallery.html'));
const directory = path.dirname(gallery);
const readJson = async name => JSON.parse(await fs.readFile(path.join(directory, name), 'utf8'));
// JSONL is deliberately compact; support both default .NET record casing and
// the camelCase convention used by the human-readable summary/gallery files.
const camel = value => Array.isArray(value) ? value.map(camel) : value && typeof value === 'object'
  ? Object.fromEntries(Object.entries(value).map(([key, item]) => [key[0].toLowerCase() + key.slice(1), camel(item)])) : value;
const jsonLines = async name => (await fs.readFile(path.join(directory, name), 'utf8')).split(/\r?\n/)
  .filter(line => line.trim()).map((line, index) => {
    try { return camel(JSON.parse(line)); }
    catch (error) { throw new Error(`${name}:${index + 1} is not a complete JSONL record: ${error.message}`); }
  });
const count = (rows, predicate) => rows.filter(predicate).length;

(async () => {
  const summary = await readJson('summary.json');
  const data = await readJson('gallery-data.json');
  const rows = await jsonLines('evaluation.jsonl');
  const manifest = await jsonLines('manifest.jsonl');
  assert.equal(summary.schema, 'ornament-learning-pilot-v1');
  assert.equal(data.schema, 'ornament-learning-gallery-v1');
  assert.deepEqual(data.summary, summary);
  assert.equal(summary.partitionIsolation, true);
  assert.equal(summary.frozenBeforeValidation, true);
  assert.ok(summary.materializedTrainRows >= summary.usableTrainRows);
  assert.ok(summary.usableTrainRows <= summary.trainBoundaries);
  assert.ok(summary.auditWitnessesRetained <= summary.detailedAuditLimit);
  assert.ok(summary.modelEpisodes <= summary.usableTrainRows || summary.checkpointLoaded);
  assert.equal(manifest.length, summary.materializedTrainRows + rows.length);
  assert.equal(new Set(rows.map(row => row.index)).size, rows.length);
  assert.ok(rows.every(row => row.split !== 'train'), 'evaluation contains no training rows');
  for (const row of rows) {
    assert.ok(row.teacherBoundaries >= 0 && row.admissible >= 0 && row.firstStreamAdmissible >= 0);
    assert.ok(row.admissible <= summary.prototypes && row.firstStreamAdmissible <= summary.prototypes);
    assert.ok(Number.isFinite(row.milliseconds) && row.milliseconds >= 0);
    if (row.seenExact) assert.equal(row.seenShape, true);
    if (row.exactTop1) { assert.equal(row.seenExact, true); assert.equal(row.shapeTop1, true); }
    if (row.shapeTop1) { assert.equal(row.shapeTop3, true); assert.equal(row.seenShape, true); }
    if (row.exactTop1 || row.shapeTop1 || row.shapeTop3 || row.uniformShapeTop1 || row.frequencyShapeTop1 || row.randomShapeTop1) {
      assert.ok(row.teacherBoundaries > 0); assert.ok(row.admissible > 0);
    }
    if (row.firstStreamShapeTop1) { assert.ok(row.teacherBoundaries > 0); assert.ok(row.firstStreamAdmissible > 0); }
    if (row.conflicting) assert.equal(row.admissible, 0, 'frequency cannot overrule a proven native landmark conflict');
    if (row.generatedClosed) assert.ok(row.admissible > 0);
    if (row.teacherShapeAdmissible !== undefined) {
      if (row.teacherShapeAdmissible) { assert.equal(row.seenShape, true); assert.ok(row.teacherBoundaries > 0 && row.admissible > 0); }
      if (row.teacherExactAdmissible) { assert.equal(row.seenExact, true); assert.equal(row.teacherShapeAdmissible, true); }
      if (row.shapeTop3) assert.equal(row.teacherShapeAdmissible, true);
      if (row.exactTop1) assert.equal(row.teacherExactAdmissible, true);
      if (row.conflicting) { assert.equal(row.teacherShapeAdmissible, false); assert.equal(row.teacherExactAdmissible, false); }
    }
  }

  const groupSplits = new Map();
  const trainingKeys = new Set(), trainingShapes = new Set();
  for (const item of manifest) {
    const descriptor = item.descriptor;
    assert.ok(descriptor && ['train', 'validation', 'test'].includes(descriptor.split));
    const old = groupSplits.get(descriptor.group);
    assert.ok(old === undefined || old === descriptor.split, 'construction siblings remain in one partition');
    groupSplits.set(descriptor.group, descriptor.split);
    if (descriptor.split === 'train') {
      assert.ok(Array.isArray(item.keys) && Array.isArray(item.shapes));
      item.keys.forEach(key => trainingKeys.add(key)); item.shapes.forEach(key => trainingShapes.add(key));
    } else {
      assert.ok(Array.isArray(item.teacherKeys) && Array.isArray(item.teacherShapes));
      const row = rows.find(row => row.index === descriptor.index);
      assert.ok(row); assert.deepEqual(item.row, row); assert.equal(row.group, descriptor.group); assert.equal(row.split, descriptor.split);
      if (!summary.checkpointLoaded) {
        assert.equal(row.seenExact, item.teacherKeys.some(key => trainingKeys.has(key)));
        assert.equal(row.seenShape, item.teacherShapes.some(key => trainingShapes.has(key)));
      }
    }
  }
  if (!summary.checkpointLoaded) {
    assert.equal(trainingKeys.size, summary.prototypes);
    assert.equal(trainingShapes.size, summary.coarseRouteTypes);
  }
  for (const partition of summary.partitions) {
    const members = rows.filter(row => row.split === partition.split);
    const expected = {
      rows: members.length, groups: new Set(members.map(row => row.group)).size,
      withTeacherBoundary: count(members, row => row.teacherBoundaries > 0),
      seenExact: count(members, row => row.seenExact), seenShape: count(members, row => row.seenShape),
      conflicting: count(members, row => row.conflicting),
      conflictAbstentions: count(members, row => row.conflicting && row.admissible === 0),
      abstentions: count(members, row => row.admissible === 0),
      meaningsProposed: members.reduce((sum, row) => sum + row.admissible, 0),
      firstStreamMeaningsProposed: members.reduce((sum, row) => sum + row.firstStreamAdmissible, 0),
      unseenShapeRows: count(members, row => row.teacherBoundaries > 0 && !row.seenShape),
      unseenShapeTop1: count(members, row => !row.seenShape && row.shapeTop1),
      generatedClosed: count(members, row => row.generatedClosed),
      generatedWithOpenClaims: count(members, row => row.generatedClosed && row.generatedOpenClaims > 0)
    };
    for (const key of ['exactTop1', 'shapeTop1', 'shapeTop3', 'firstStreamShapeTop1', 'uniformShapeTop1', 'frequencyShapeTop1', 'randomShapeTop1'])
      expected[key] = count(members, row => row[key]);
    if (partition.cleanFamiliarRows !== undefined) {
      const familiar = row => row.teacherBoundaries > 0 && row.seenShape && !row.conflicting;
      const single = row => row.teacherBoundaries === 1 && row.seenExact && !row.conflicting;
      Object.assign(expected, {
        cleanFamiliarRows: count(members, familiar),
        cleanFamiliarAdmitted: count(members, row => familiar(row) && row.teacherShapeAdmissible),
        cleanFamiliarTop1: count(members, row => familiar(row) && row.shapeTop1),
        singleBoundaryFamiliarRows: count(members, single),
        singleBoundaryFamiliarAdmitted: count(members, row => single(row) && row.teacherExactAdmissible)
      });
    }
    for (const [key, value] of Object.entries(expected)) assert.equal(partition[key], value, `${partition.split}.${key}`);
  }
  assert.equal(summary.partitions.reduce((sum, partition) => sum + partition.rows, 0), rows.length);
  assert.ok(summary.replayChecks <= Math.min(24, rows.length));
  const modelBytes = await fs.readFile(path.join(directory, 'model.gojo.json.gz'));
  assert.equal(summary.modelGzipBytes, modelBytes.length);
  assert.equal(summary.modelSha256, crypto.createHash('sha256').update(modelBytes).digest('hex'));
  const model = JSON.parse(zlib.gunzipSync(modelBytes).toString('utf8'));
  assert.equal(model.version, 3); assert.equal(model.carriers.length, 0);
  assert.ok(model.nodes.length > 0);
  const repairBytes = await fs.readFile(path.join(directory, 'repair-probes.gojo.json.gz'));
  const repair = JSON.parse(zlib.gunzipSync(repairBytes).toString('utf8'));
  assert.equal(repair.version, 3); assert.equal(repair.carriers.length, 0);
  assert.ok(summary.repairProbes.length <= 6);
  for (const probe of summary.repairProbes) {
    assert.equal(probe.preservedParent, true);
    assert.equal(probe.unchangedOtherChords, true);
    assert.equal(probe.netEndpointsPreserved, true);
  }
  const episodeBytes = await fs.readFile(path.join(directory, 'retained-episodes.gojo.json.gz'));
  const episodes = JSON.parse(zlib.gunzipSync(episodeBytes).toString('utf8'));
  assert.equal(episodes.version, 3);
  assert.equal(episodes.sourceId, 'ornament-learning-retained-receiver-episodes-v1');
  assert.equal(episodes.carriers.length, 0);
  assert.ok(episodes.nodes.length > 0, 'retained receiver explanations are native structure');

  const images = new Map();
  const addImage = (file, kind, expected) => {
    assert.ok(typeof file === 'string' && file.endsWith('.svg') && !path.isAbsolute(file) && !file.includes('#'));
    const absolute = path.resolve(directory, file);
    assert.ok(absolute.startsWith(directory + path.sep), 'images stay within the output directory');
    assert.ok(!images.has(file), 'image names are unique per inspected view');
    images.set(file, { kind, expected });
  };
  for (const example of data.examples) {
    const row = rows.find(row => row.index === Number(example.id.slice(1)));
    assert.ok(row, 'inspected case is an actual evaluation episode');
    assert.equal(example.group, row.group); assert.equal(example.split, row.split);
    assert.ok(example.candidates.length <= 4);
    assert.ok(example.candidates.filter(candidate => candidate.compatible).length <= 3);
    assert.ok(example.candidates.filter(candidate => !candidate.compatible).length <= 1);
    addImage(example.teacher, 'teacher'); addImage(example.pattern, 'pattern');
    for (const stream of example.streams) addImage(stream.image, 'stream', stream.passages);
    for (const candidate of example.candidates) {
      assert.ok(Number.isFinite(candidate.score) && Number.isFinite(candidate.share));
      assert.ok(candidate.share >= 0 && candidate.share <= 1);
      assert.equal(candidate.compatible, candidate.contradicted === 0);
      assert.ok(candidate.support > 0 && candidate.sides >= 3);
      addImage(candidate.image, 'candidate', candidate.sides);
    }
    if (example.generated) {
      assert.ok(row.admissible > 0 && example.candidates.some(candidate => candidate.compatible));
      addImage(example.generated, 'generated');
    }
  }
  for (const [file, item] of images) {
    const svg = await fs.readFile(path.join(directory, file), 'utf8');
    assert.ok(svg.includes('<svg') && svg.includes('</svg>')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg));
    const paths = [...svg.matchAll(/<path\b/g)].length;
    if (item.kind === 'candidate') assert.equal(paths, item.expected);
    if (item.kind === 'stream') assert.ok(paths <= item.expected);
  }
  const report = { evaluatedRows: rows.length, manifestRows: manifest.length, inspectedCases: data.examples.length,
    prototypes: summary.prototypes, fragments: summary.fragments, svgFiles: images.size, modelNodes: model.nodes.length,
    receiverEpisodeNodes: episodes.nodes.length };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const screenshots = path.resolve(__dirname, '../../../artifacts', path.basename(directory) + '-qa'); await fs.mkdir(screenshots, { recursive: true });
  const errors = [], references = new Set(); let browserStates = 0;
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
        if (url.protocol === 'file:') { assert.equal(url.hash, '', 'local report links use query parameters'); references.add(url.href); }
      }
      browserStates++;
    }
    async function inspect(example) {
      await page.selectOption('#case', example.id); await assets();
      assert.equal(await page.locator('#content > h2').innerText(), example.id);
      assert.equal(new URL(page.url()).searchParams.get('id'), example.id);
      assert.deepEqual(await page.locator('.candidate .badge').allTextContents(), example.candidates.map(candidate =>
        candidate.compatible ? 'Compatible proposal' : 'Conflict retained'));
      assert.equal(await page.locator('.teacher img').count(), 2);
      assert.ok((await page.locator('#content').innerText()).includes('not calibrated probabilities'));
      assert.ok((await page.locator('.teacher').innerText()).includes('withheld'));
    }
    await page.goto(pathToFileURL(gallery).href);
    await page.waitForFunction(() => Boolean(globalThis.ORNAMENT_LEARNING));
    assert.deepEqual(await page.evaluate(() => globalThis.ORNAMENT_LEARNING), data);
    assert.equal(await page.locator('#case option').count(), data.examples.length);
    for (const example of data.examples) await inspect(example);
    if (data.examples.length) {
      await inspect(data.examples.find(example => example.generated) || data.examples[0]);
      await page.screenshot({ path: path.join(screenshots, 'learning-case.png'), fullPage: true });
      await page.setViewportSize({ width: 390, height: 844 });
      for (const example of data.examples) await inspect(example);
      await page.screenshot({ path: path.join(screenshots, 'learning-mobile.png'), fullPage: true });
      const last = data.examples.at(-1);
      await page.goto(pathToFileURL(gallery).href + '?id=' + encodeURIComponent(last.id)); await assets();
      assert.equal(await page.locator('#case').inputValue(), last.id);
    }
    await page.setViewportSize({ width: 1500, height: 1100 });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await assets();
    assert.equal(await page.locator('.tile').count(), data.examples.length);
    await page.screenshot({ path: path.join(screenshots, 'learning-overview.png'), fullPage: true });
    if (data.examples.length) {
      await page.locator('.tile').first().click(); await assets();
      assert.equal(await page.locator('#case').inputValue(), data.examples[0].id);
    }
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, [], 'no browser errors');
    Object.assign(report, { browserStates, localReferences: references.size, screenshots, errors: 0 });
    await fs.writeFile(path.join(directory, 'browser-check.json'), JSON.stringify(report, null, 2) + '\n');
    process.stdout.write(JSON.stringify(report) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
