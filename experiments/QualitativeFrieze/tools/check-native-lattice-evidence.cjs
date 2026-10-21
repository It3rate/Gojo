const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/native-lattice-evidence/gallery.html'));
const directory = path.dirname(gallery);
const statuses = ['Yes', 'No', 'Open', 'Conflict', 'Blocked'];
const label = answer => ({ Yes: 'Selected construction supported', No: 'Selected construction refused',
  Open: 'Evidence remains unresolved', Conflict: 'Conflicting evidence retained', Blocked: 'Evidence route blocked' })[answer];
const sorted = values => [...values].sort();

(async () => {
  const d = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(d.schema, 'native-lattice-evidence-v1');
  assert.deepEqual(JSON.parse((await fs.readFile(path.join(directory, 'data.js'), 'utf8'))
    .replace(/^globalThis\.NATIVE_LATTICE_EVIDENCE\s*=\s*/, '').replace(/;\s*$/, '')), d);
  assert.equal(d.sources.length, 13); assert.equal(d.counts.sources, 13); assert.equal(d.goals.length, 6);
  assert.equal(new Set(d.sources.map(source => source.id)).size, d.sources.length);
  const images = new Map(); let candidates = 0, queries = 0, samples = 0;
  const answers = {};
  for (const s of d.sources) {
    images.set(s.sourceImage, { kind: 'source', strokes: s.visibleSourceStrokes });
    assert.ok(s.sourceStrokes >= s.visibleSourceStrokes);
    assert.ok(s.candidates.length > 0 && s.candidates.length <= 32);
    assert.ok(s.sourceSites > 0 && s.derivedSupports > 0);
    const finiteCounts = new Map(d.goals.filter(goal => goal.finite).map(goal => [goal.id, {}]));
    assert.equal(s.capacityExamples.length, 3);
    for (const p of s.capacityExamples) {
      assert.ok(statuses.includes(p.answer)); assert.ok(p.queries <= p.queryCapacity); assert.ok(p.replies <= p.serviceCapacity);
      if (p.queryCapacity === 0) { assert.equal(p.answer, 'Open'); assert.equal(p.queries, 0); assert.equal(p.replies, 0); assert.ok(p.cuts > 0); }
      if (p.serviceCapacity === 0) { assert.equal(p.answer, 'Open'); assert.equal(p.replies, 0); assert.ok(p.serviceOpen > 0); }
    }
    for (const c of s.candidates) {
      candidates++; assert.equal(c.reads.length, 6); assert.ok(c.sourceStrokes >= 4);
      assert.ok(c.visibleSourceStrokes <= c.sourceStrokes);
      images.set(c.selectedImage, { kind: 'selected', strokes: s.visibleSourceStrokes, selected: c.visibleSourceStrokes });
      assert.equal(new Set(c.reads.map(r => r.goal)).size, 6);
      for (const r of c.reads) {
        queries++; answers[r.answer] = (answers[r.answer] || 0) + 1;
        const goal = d.goals.find(goal => goal.id === r.goal); assert.ok(goal); assert.equal(r.finite, goal.finite);
        assert.ok(statuses.includes(r.answer)); assert.equal(r.consumed, r.visited.length);
        assert.equal(new Set(r.visited).size, r.visited.length); assert.ok(r.consumed <= r.queryCapacity);
        assert.ok(r.serviceConsumed <= r.serviceCapacity); assert.equal(r.serviceAccepted, r.observedFacts.length);
        assert.equal(new Set(r.serviceRequests.map(item => item.name)).size, r.serviceRequests.length, 'one service request per memoized leaf');
        assert.ok(r.serviceRequests.every(item => r.visited.includes(item.name)), 'only reached obligations ask the source');
        assert.deepEqual(sorted(r.serviceRequests.filter(item => item.accepted).map(item => item.name)), sorted(r.observedFacts.map(item => item.name)));
        assert.ok(r.serviceRequests.every(item => !item.name.startsWith('Uniform repetition')), 'unused stream regularity is not requested');
        if (['g0', 'g1', 'g2'].includes(r.goal))
          assert.ok(r.serviceRequests.every(item => !item.name.startsWith('Both encounters inside')), 'supporting-line potential does not ask finite clipping');
        if (['g0', 'g1', 'g3', 'g4'].includes(r.goal))
          assert.ok(r.serviceRequests.every(item => item.name !== 'Equal reach between the two families'), 'weaker question does not request square equality');
        if (r.goal === 'g2' || r.goal === 'g5') {
          assert.equal(r.serviceRequests[0].name, 'Equal reach between the two families');
          if (r.serviceRequests[0].answer === 'No') {
            assert.equal(r.answer, 'No'); assert.equal(r.serviceRequests.length, 1); assert.equal(r.visited.length, 2);
          }
        }
        if (r.finite) {
          const counts = finiteCounts.get(r.goal); counts[r.answer] = (counts[r.answer] || 0) + 1;
          if (r.sampleImage) {
            samples++; assert.equal(r.answer, 'Yes'); assert.equal(r.completionAnswer, 'Yes'); assert.equal(r.conditionalAnswer, 'Yes');
            assert.equal(r.sampleSites, 4); assert.equal(r.sampleEdges, 4); assert.equal(r.cycleWitnesses, 1);
            assert.deepEqual(r.assumed, [], 'all sampled premises came through actual producer evidence in this run');
            images.set(r.sampleImage, { kind: 'sample' });
          } else { assert.notEqual(r.completionAnswer, 'Yes'); assert.equal(r.sampleSites, 0); assert.equal(r.sampleEdges, 0); }
        } else {
          assert.equal(r.sampleImage, null); assert.equal(r.completionAnswer, null); assert.equal(r.conditionalAnswer, null);
        }
        if (r.answer !== 'Yes') assert.equal(r.sampleImage, null, 'unresolved/refuted source is not promoted to an actual sample');
      }
    }
    for (const summary of s.summaries) assert.deepEqual(summary.counts, finiteCounts.get(summary.goal), 'overview totals count actual searched candidate results');
  }
  assert.equal(d.counts.candidates, candidates); assert.equal(d.counts.queries, queries); assert.equal(d.counts.samples, samples);
  assert.deepEqual(d.counts.answers, answers);
  const source = id => d.sources.find(s => s.id === id);
  const reads = (id, goal) => source(id).candidates.map(c => c.reads.find(r => r.goal === goal));
  assert.equal(source('N02').candidates.length, 9);
  assert.equal(reads('N02', 'g3').filter(r => r.answer === 'Yes').length, 9);
  assert.equal(reads('N02', 'g4').filter(r => r.answer === 'Yes').length, 9);
  assert.equal(reads('N02', 'g5').filter(r => r.answer === 'Yes').length, 5, 'four small and one enclosing square boundary, not five elementary faces');
  assert.ok(reads('N01', 'g4').every(r => r.answer === 'Open'), 'drawing direction labels do not supply a quarter turn');
  assert.ok(reads('N03', 'g5').some(r => r.answer === 'No') && reads('N03', 'g5').some(r => r.answer === 'Yes'));
  assert.ok(source('N04').candidates.some(c => c.reads.find(r => r.goal === 'g0').answer === 'Yes' && c.reads.find(r => r.goal === 'g3').answer === 'No'), 'finite clipping preserves supporting-line possibility');
  assert.ok(source('N05').unresolvedPositions > 0 && source('N05').visibleSourceStrokes < source('N05').sourceStrokes);
  assert.ok(reads('N06', 'g0').some(r => r.answer === 'No'), 'coincident distinct passages do not form separated cell sides');
  assert.ok(reads('N07', 'g0').some(r => r.answer === 'Open'), 'unspecified interpolation is not replaced with a chord');
  assert.ok(d.sources.some(s => s.unsearched > 0), 'bounded family discovery exposes a search remainder');
  for (const [file, expected] of images) {
    const svg = await fs.readFile(path.join(directory, file), 'utf8');
    assert.ok(svg.includes('<svg')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg));
    if (expected.kind !== 'sample') assert.equal([...svg.matchAll(/class='source-segment(?: selected-support)?'/g)].length, expected.strokes);
    if (expected.kind === 'selected') assert.equal([...svg.matchAll(/class='source-segment selected-support'/g)].length, expected.selected);
    if (expected.kind === 'sample') {
      assert.equal([...svg.matchAll(/class='read-boundary'/g)].length, 4);
      assert.equal([...svg.matchAll(/class='read-site'/g)].length, 4);
      assert.ok(svg.includes('read corners do not weld source passages'));
    }
  }
  const report = { sources: d.sources.length, candidates, queries, samples, svgFiles: images.size };
  if (process.argv.includes('--data-only')) { process.stdout.write(JSON.stringify(report) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const errors = [], references = new Set(); let browserStates = 0;
  const screenshots = path.resolve(__dirname, '../../../artifacts/native-lattice-evidence-qa'); await fs.mkdir(screenshots, { recursive: true });
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
        assert.ok(!raw.includes('#'), 'local artifact links use query parameters');
        const url = new URL(raw, page.url()); if (url.protocol === 'file:') references.add(url.href);
      }
      browserStates++;
    }
    async function inspect(s, c, r) {
      await page.evaluate(({ source, candidate, goal }) => {
        const sourceSelect = document.getElementById('source');
        if (sourceSelect.value !== source) { sourceSelect.value = source; sourceSelect.dispatchEvent(new Event('change')); }
        document.getElementById('candidate').value = candidate;
        const goalSelect = document.getElementById('goal'); goalSelect.value = goal; goalSelect.dispatchEvent(new Event('change'));
      }, { source: s.id, candidate: c.id, goal: r.goal });
      await assets();
      assert.equal(await page.locator('#answer').innerText(), label(r.answer));
      assert.equal(await page.locator('#source-image').getAttribute('src'), c.selectedImage);
      assert.equal(await page.locator('#sample-image').isVisible(), Boolean(r.sampleImage));
      if (r.sampleImage) assert.equal(await page.locator('#sample-image').getAttribute('src'), r.sampleImage);
      else assert.equal(await page.locator('#sample-image').getAttribute('src'), null);
      assert.deepEqual(await page.locator('#facts .fact span').allTextContents(), r.observedFacts.map(f => f.name));
      assert.deepEqual(await page.locator('#facts .fact b').allTextContents(), r.observedFacts.map(f => f.answer));
      if (r.serviceRequests.length) assert.deepEqual(await page.locator('#service-trace strong').allTextContents(), r.serviceRequests.map((f, i) => (i + 1) + '. ' + f.name));
      assert.deepEqual(await page.locator('#missing li').allTextContents(), r.missing);
      assert.deepEqual(await page.locator('#unasked li').allTextContents(), r.unasked);
      assert.deepEqual(await page.locator('#cuts li').allTextContents(), r.budgetCuts);
      assert.equal(await page.locator('#views [aria-selected="true"]').getAttribute('data-goal'), r.goal);
    }
    await page.goto(pathToFileURL(gallery).href); await page.waitForFunction(() => document.querySelector('#source')?.options.length === 13);
    for (const s of d.sources) {
      for (const c of s.candidates) await inspect(s, c, c.reads.find(r => r.goal === 'g5'));
      for (const r of s.candidates[0].reads) await inspect(s, s.candidates[0], r);
    }
    const squareSource = source('N02'); const large = squareSource.candidates.find(c => c.id === 'C05') || squareSource.candidates[0];
    await inspect(squareSource, large, large.reads.find(r => r.goal === 'g5'));
    await page.screenshot({ path: path.join(screenshots, 'native-square-boundary.png'), fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    const refusal = source('N03'), refused = refusal.candidates.find(c => c.reads.find(r => r.goal === 'g5').answer === 'No');
    await inspect(refusal, refused, refused.reads.find(r => r.goal === 'g5'));
    assert.equal(await page.locator('#service-trace li').count(), 1);
    await page.screenshot({ path: path.join(screenshots, 'mobile-early-square-refusal.png'), fullPage: true });
    await page.setViewportSize({ width: 1500, height: 1100 });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href);
    await page.evaluate(() => document.querySelectorAll('img').forEach(image => { image.loading = 'eager'; })); await assets();
    assert.equal(await page.locator('[data-source]').count(), 13); assert.equal(await page.locator('.summary a').count(), 39);
    await page.screenshot({ path: path.join(screenshots, 'native-source-overview.png'), fullPage: true });
    await page.locator('[data-source="N02"] .summary a').last().click(); await assets();
    assert.equal(await page.locator('#source').inputValue(), 'N02'); assert.equal(await page.locator('#goal').inputValue(), 'g5');
    await page.locator('#views [data-goal="g3"]').click(); await assets(); assert.equal(await page.locator('#goal').inputValue(), 'g3');
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, [], 'browser errors');
    Object.assign(report, { browserStates, localReferences: references.size, screenshots, errors: 0 });
    await fs.writeFile(path.join(directory, 'browser-check.json'), JSON.stringify(report, null, 2) + '\n');
    process.stdout.write(JSON.stringify(report) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
