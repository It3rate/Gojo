const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');

const supplied = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(supplied || path.join(__dirname, '../results/structural-potential/gallery.html'));
const directory = path.dirname(gallery);
const dataOnly = process.argv.includes('--data-only');
const answer = value => ['Yes', 'No', 'Open', 'Conflict', 'Blocked'].includes(value);
const status = value => ({ Yes: 'Construction contract supported', No: 'This construction recipe is refuted',
  Conflict: 'Conflicting premises remain visible', Open: 'Construction remains unresolved', Blocked: 'This read is blocked' })[value];
const sorted = values => [...values].sort();

(async () => {
  const d = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(d.schema, 'structural-potential-v1');
  assert.deepEqual(JSON.parse((await fs.readFile(path.join(directory, 'data.js'), 'utf8'))
    .replace(/^globalThis\.STRUCTURAL_POTENTIAL\s*=\s*/, '').replace(/;\s*$/, '')), d);
  assert.equal(d.cases.length, 12); assert.equal(d.counts.cases, 12);
  assert.equal(d.goals.length, 6); assert.equal(d.goals.filter(goal => goal.finite).length, 3);
  assert.equal(new Set(d.cases.map(c => c.id)).size, d.cases.length);
  const query = (id, goal, capacity = 64) => d.cases.find(c => c.id === id).reads.find(r => r.goal === goal && r.capacity === capacity);
  let states = 0, samples = 0, svgFiles = 0;
  const imageFiles = new Set();
  for (const c of d.cases) {
    assert.equal(c.sourceSites, 8); assert.equal(c.sourceEdges, 4);
    assert.equal(c.authored.length, 12); assert.equal(c.reads.length, 18);
    assert.equal(new Set(c.authored.map(f => f.name)).size, 12);
    const leaves = new Set(c.authored.map(f => f.name));
    const vocabulary = new Set([...leaves, ...d.goals.map(g => g.name)]);
    assert.ok(c.allowed.every(name => leaves.has(name)), 'assumptions name actual source predicate aliases');
    const sourceSvg = await fs.readFile(path.join(directory, c.sourceImage), 'utf8'); imageFiles.add(c.sourceImage);
    assert.equal([...sourceSvg.matchAll(/class=['"]source-segment['"]/g)].length, 4);
    assert.equal([...sourceSvg.matchAll(/<circle\b/g)].length, 8);
    assert.ok(sourceSvg.includes('not the geometric position'), 'source inventory picture is not evidence of measured position');
    const readKeys = new Set();
    for (const r of c.reads) {
      states++; const key = r.goal + '|' + r.capacity; assert.ok(!readKeys.has(key)); readKeys.add(key);
      const goal = d.goals.find(g => g.id === r.goal); assert.ok(goal); assert.equal(r.finite, goal.finite);
      assert.ok([64, 4, 0].includes(r.capacity)); assert.ok(answer(r.sourceAnswer));
      assert.ok(r.consumed >= 0 && r.consumed <= r.capacity);
      assert.equal(r.consumed, r.visited.length, 'one actual predicate is inspected per consumed query slot');
      assert.equal(new Set(r.visited).size, r.visited.length);
      for (const field of ['visited', 'unasked', 'missing', 'budgetCuts', 'conditionalVisited', 'conditionalMissing', 'conditionalCuts'])
        assert.ok(r[field].every(name => vocabulary.has(name)), field + ' names only actual exported aliases');
      assert.ok(r.missing.every(name => leaves.has(name)), 'missing obligations are leaves, not cut composite rules');
      assert.deepEqual(sorted(r.consulted.map(f => f.name)), sorted(r.visited.filter(name => leaves.has(name))));
      for (const fact of r.consulted) assert.deepEqual(fact.answers, c.authored.find(f => f.name === fact.name).answers,
        'consulting a premise does not rewrite its published polarity');
      assert.ok(r.assumed.every(name => c.allowed.includes(name)), 'only authorized assumptions are introduced');
      assert.ok(r.assumed.every(name => r.missing.includes(name)), 'only demanded missing source leaves are assumed');
      assert.ok(r.assumed.every(name => c.authored.find(f => f.name === name).answers.length === 0),
        'an assumption cannot override known positive, negative or conflicting evidence');
      if (r.capacity === 0) { assert.equal(r.sourceAnswer, 'Open'); assert.equal(r.visited.length, 0); assert.equal(r.missing.length, 0); assert.ok(r.budgetCuts.length); }
      if (r.budgetCuts.length) { assert.equal(r.assumed.length, 0); assert.equal(r.sampleImage, null); }
      if (!r.finite) {
        assert.equal(r.conditionalAnswer, null); assert.equal(r.completionAnswer, null); assert.equal(r.sampleImage, null);
        assert.equal(r.assumed.length, 0);
      } else {
        assert.ok(answer(r.conditionalAnswer)); assert.ok(answer(r.completionAnswer));
        assert.equal(r.completionAnswer, r.conditionalAnswer);
        if (['No', 'Conflict', 'Blocked'].includes(r.sourceAnswer)) {
          assert.equal(r.completionAnswer, r.sourceAnswer); assert.equal(r.assumed.length, 0); assert.equal(r.sampleImage, null);
        }
        if (r.sampleImage) {
          samples++; assert.equal(r.completionAnswer, 'Yes'); assert.equal(r.sampleSites, 4); assert.equal(r.sampleEdges, 4);
          assert.equal(r.cycleWitnesses, 1); assert.equal(r.sharedReach, r.goal === 'g5');
          const svg = await fs.readFile(path.join(directory, r.sampleImage), 'utf8'); imageFiles.add(r.sampleImage);
          assert.equal([...svg.matchAll(/class=['"]actual-segment['"]/g)].length, 4);
          assert.equal([...svg.matchAll(/class=['"]read-site['"]/g)].length, 4);
          assert.ok(svg.includes('no measured lengths or source-graph weld'));
        } else {
          assert.notEqual(r.completionAnswer, 'Yes'); assert.equal(r.sampleSites, 0); assert.equal(r.sampleEdges, 0);
          assert.equal(r.cycleWitnesses, 0);
        }
      }
    }
  }
  assert.equal(states, 216); assert.equal(d.counts.queries, states); assert.equal(d.counts.samples, samples);
  // Interpretive controls: the same finite-support refusal can preserve potential;
  // a requested stronger shape exposes evidence that the coarser query can skip.
  assert.equal(query('P01', 'g0').sourceAnswer, 'Yes'); assert.equal(query('P01', 'g3').sourceAnswer, 'Open');
  assert.equal(query('P01', 'g3').completionAnswer, 'Yes'); assert.equal(query('P01', 'g4').completionAnswer, 'Open');
  assert.equal(query('P04', 'g4').sourceAnswer, 'Yes'); assert.equal(query('P04', 'g5').sourceAnswer, 'No');
  assert.equal(query('P05', 'g0').sourceAnswer, 'Yes'); assert.equal(query('P05', 'g3').sourceAnswer, 'No');
  assert.equal(query('P06', 'g3').sourceAnswer, 'Yes'); assert.equal(query('P06', 'g4').sourceAnswer, 'Conflict');
  assert.equal(query('P08', 'g5').completionAnswer, 'Open', 'two within-family uniformity facts do not supply cross-family equality');
  assert.equal(query('P09', 'g3').completionAnswer, 'Open');
  assert.equal(query('P10', 'g5').sourceAnswer, 'Open'); assert.equal(query('P10', 'g5').completionAnswer, 'Yes');
  assert.equal(query('P11', 'g3').completionAnswer, 'Open'); assert.equal(query('P11', 'g3').assumed.length, 3);
  assert.equal(query('P12', 'g0').sourceAnswer, 'No');
  for (const image of imageFiles) {
    const svg = await fs.readFile(path.join(directory, image), 'utf8'); assert.ok(svg.includes('<svg'));
    assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg)); svgFiles++;
  }
  const generated = (await fs.readdir(path.join(directory, 'patterns'))).filter(file => file.endsWith('.svg'));
  assert.equal(generated.length, imageFiles.size, 'all current image files have a data reference');
  const summary = JSON.parse(await fs.readFile(path.join(directory, 'summary.json'), 'utf8'));
  assert.equal(summary.Cases, 12); assert.equal(summary.SourceReads, 72); assert.equal(summary.TransportChecks, 72);
  assert.equal(summary.CompletionAttempts, 36); assert.equal(summary.BoundarySamples, samples);
  assert.ok((await fs.stat(path.join(directory, 'evidence.gojo.json.gz'))).size > 1000);
  const report = { cases: d.cases.length, queryStates: states, samples, svgFiles, transportChecks: summary.TransportChecks };
  if (dataOnly) { process.stdout.write(JSON.stringify(report) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const errors = [], references = new Set(); let browserStates = 0;
  const screenshots = path.resolve('artifacts/structural-potential-qa'); await fs.mkdir(screenshots, { recursive: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1500, height: 1120 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => errors.push(request.url()));
    async function assets() {
      await page.waitForFunction(() => [...document.querySelectorAll('img[src]')].filter(image => image.getClientRects().length)
        .every(image => image.complete && image.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'no horizontal overflow');
      for (const raw of await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element => ['href', 'src']
        .filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))))) {
        assert.ok(!raw.includes('#'), 'local links use query parameters instead of fragments');
        const url = new URL(raw, page.url()); if (url.protocol === 'file:') references.add(url.href);
      }
      browserStates++;
    }
    async function inspect(c, r) {
      await page.selectOption('#case', c.id); await page.selectOption('#question', r.goal); await page.selectOption('#capacity', String(r.capacity));
      await assets();
      assert.equal(await page.locator('#source-answer').innerText(), status(r.sourceAnswer));
      assert.equal(await page.locator('#source-image').getAttribute('src'), c.sourceImage);
      assert.deepEqual(await page.locator('#source-facts .fact-row strong').allTextContents(), r.consulted.map(f => f.name));
      assert.deepEqual(await page.locator('#source-facts .fact-value').allTextContents(), r.consulted.map(f => f.answers.length ? f.answers.join(' + ') : 'No reply published'));
      assert.equal(await page.locator('#authored .fact-row').count(), 12);
      assert.deepEqual(await page.locator('#query-stages span').evaluateAll(items => items.map(item => item.childNodes[item.childNodes.length - 1].textContent)), r.visited);
      assert.deepEqual(await page.locator('#missing-remainder li').allTextContents(), r.missing);
      assert.deepEqual(await page.locator('#unasked-remainder li').allTextContents(), r.unasked);
      assert.deepEqual(await page.locator('#budget-remainder li').allTextContents(), r.budgetCuts);
      assert.equal(await page.locator('#potentials [aria-selected="true"]').getAttribute('data-question'), r.goal);
      assert.equal(await page.locator('#completion-image').isVisible(), Boolean(r.sampleImage));
      assert.equal(await page.locator('#no-completion').isVisible(), !r.sampleImage);
      const expectedCompletion = !r.finite ? 'Observation only · no completion requested' : r.sampleImage ?
        r.assumed.length ? 'Witnessed conditional boundary read' : 'Boundary read witnessed from supplied premises' : status(r.completionAnswer);
      assert.equal(await page.locator('#completion-answer').innerText(), expectedCompletion);
      if (r.sampleImage) assert.equal(await page.locator('#completion-image').getAttribute('src'), r.sampleImage);
      else assert.equal(await page.locator('#completion-image').getAttribute('src'), null);
      assert.deepEqual(await page.locator('#completion-assumptions li').allTextContents(), r.assumed);
      assert.ok((await page.locator('#deduction-counts').innerText()).startsWith(r.consumed + ' of ' + r.capacity));
    }
    await page.goto(pathToFileURL(gallery).href); await page.waitForFunction(() => document.querySelector('#case')?.options.length === 12);
    for (const c of d.cases) for (const r of c.reads) await inspect(c, r);
    for (const [id, goal, file] of [['P03', 'g5', 'conditional-square.png'], ['P05', 'g3', 'finite-refusal.png'], ['P06', 'g4', 'conflict.png']]) {
      const c = d.cases.find(c => c.id === id); await inspect(c, query(id, goal));
      await page.screenshot({ path: path.join(screenshots, file), fullPage: true });
    }
    // Question tiles reselect the actual read rather than merely changing its label.
    await page.locator('[data-question="g0"]').click(); await assets(); assert.equal(await page.locator('#question').inputValue(), 'g0');
    await page.setViewportSize({ width: 390, height: 844 });
    for (const [id, goal, capacity] of [['P03', 'g5', 64], ['P06', 'g4', 64], ['P10', 'g5', 0]])
      await inspect(d.cases.find(c => c.id === id), query(id, goal, capacity));
    await page.screenshot({ path: path.join(screenshots, 'mobile-budget-cut.png'), fullPage: true });
    await inspect(d.cases.find(c => c.id === 'P03'), query('P03', 'g5'));
    await page.screenshot({ path: path.join(screenshots, 'mobile-completion.png'), fullPage: true });
    await page.setViewportSize({ width: 1500, height: 1120 });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href);
    await page.evaluate(() => document.querySelectorAll('img').forEach(image => { image.loading = 'eager'; })); await assets();
    assert.equal(await page.locator('[data-case]').count(), 12); assert.equal(await page.locator('.queries a').count(), 36);
    await page.screenshot({ path: path.join(screenshots, 'overview.png'), fullPage: true });
    await page.locator('[data-case="P04"] .queries a').last().click(); await assets();
    assert.equal(await page.locator('#case').inputValue(), 'P04'); assert.equal(await page.locator('#question').inputValue(), 'g5');
    assert.equal(await page.locator('#source-answer').innerText(), status(query('P04', 'g5').sourceAnswer));
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, [], 'browser errors');
    process.stdout.write(JSON.stringify({ ...report, browserStates, localReferences: references.size, screenshots }) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
