const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');

const argument = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(argument || path.join(__dirname, '../results/ornament-concepts/gallery.html'));
const directory = path.dirname(gallery);
const dataOnly = process.argv.includes('--data-only');
const known = value => value === 'Yes' || value === 'No';
const answer = value => ['Yes', 'No', 'Open', 'Blocked'].includes(value);
const key = (source, target) => source + '|' + target;

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(data.schema, 'ornament-concepts-v1');
  const script = await fs.readFile(path.join(directory, 'data.js'), 'utf8');
  assert.deepEqual(JSON.parse(script.replace(/^globalThis\.ORNAMENT_CONCEPTS\s*=\s*/, '').replace(/;\s*$/, '')), data,
    'browser data equals the diagnostic artifact');
  assert.equal(data.counts.designs, data.cases.length);
  assert.equal(data.counts.queries, data.queries.length);
  assert.equal(data.counts.pairComparisons, data.cases.length ** 2);
  assert.equal(data.pairs.length, data.counts.pairComparisons);
  assert.equal(data.counts.transportGraphs, data.cases.length);
  assert.equal(data.counts.transportedQuestions, data.cases.length);
  assert.equal(data.counts.transportAnswers, data.cases.length * data.queries.length);
  const cases = new Map(data.cases.map(item => [item.id, item]));
  const pairs = new Map(data.pairs.map(pair => [key(pair.source, pair.target), pair]));
  assert.equal(cases.size, data.cases.length); assert.equal(pairs.size, data.pairs.length);
  assert.ok(cases.has(data.defaultSource));
  for (const item of data.cases) {
    assert.equal(item.answers.length, data.queries.length); assert.ok(item.answers.every(answer));
    assert.ok(item.sites > 0 && item.segments > 0 && item.groups > 0);
    assert.ok(item.branchQuestionParticipants > 0 && item.cornerQuestionParticipants > 0);
  }
  for (const pair of data.pairs) {
    const source = cases.get(pair.source), target = cases.get(pair.target);
    assert.ok(source && target);
    for (const field of ['legacy', 'branches', 'corners', 'incidence', 'orderedRelations']) assert.ok(answer(pair[field]));
    const disagreements = source.answers.flatMap((value, query) => known(value) && known(target.answers[query]) &&
      value !== target.answers[query] ? [query] : []);
    assert.deepEqual(pair.disagreements, disagreements);
    assert.equal(pair.legacy, disagreements.length ? 'No' : [...source.answers, ...target.answers].every(known) ? 'Yes' : 'Open');
    assert.ok(Number.isInteger(pair.branchWork) && pair.branchWork >= 0 && pair.branchWork <= 20000);
    assert.ok(Number.isInteger(pair.cornerWork) && pair.cornerWork >= 0 && pair.cornerWork <= 20000);
    if (pair.branches === 'Yes') {
      assert.equal(pair.sourceAnchors, pair.targetAnchors); assert.equal(pair.sourceRoutes, pair.targetRoutes);
      assert.equal(pair.correspondenceRows, pair.sourceAnchors + pair.sourceRoutes, 'complete native site and route correspondence');
    }
    if (pair.orderedRelations === 'Yes') assert.equal(pair.incidence, 'Yes');
    if (pair.incidence === 'Yes') assert.equal(pair.corners, 'Yes');
    if (pair.corners === 'Yes') assert.equal(pair.branches, 'Yes');
    for (const field of ['legacy', 'branches', 'corners']) {
      const reverse = pairs.get(key(pair.target, pair.source));
      if (known(pair[field]) && known(reverse[field])) assert.equal(pair[field], reverse[field], field + ' symmetry');
      if (pair.source === pair.target) assert.equal(pair[field], 'Yes', field + ' reflexivity');
    }
  }
  for (const field of ['branches', 'corners']) {
    // Whole-scope equivalence must not depend on the chosen representative.
    // A capacity-limited Open result may defer a proof, but cannot contradict it.
    for (const a of data.cases) for (const b of data.cases) {
      if (pairs.get(key(a.id, b.id))[field] !== 'Yes') continue;
      for (const c of data.cases) if (pairs.get(key(b.id, c.id))[field] === 'Yes')
        assert.notEqual(pairs.get(key(a.id, c.id))[field], 'No', field + ' transitivity');
    }
  }
  const baseline = data.pairs.filter(pair => pair.source === data.defaultSource);
  for (const [name, field] of [['legacy', 'legacy'], ['branches', 'branches'], ['corners', 'corners']])
    assert.equal(data.baseline[name], baseline.filter(pair => pair[field] === 'Yes').length);
  assert.equal(data.baseline.flexibleDifferentIncidence, baseline.filter(pair => pair.branches === 'Yes' && pair.incidence === 'No').length);
  assert.equal(data.baseline.flexibleDifferentVocabulary, baseline.filter(pair => pair.branches === 'Yes' && pair.legacy === 'No').length);
  let svgFiles = 0;
  for (const item of data.cases) for (const relative of [item.image, item.groupImage]) {
    const svg = await fs.readFile(path.join(directory, relative), 'utf8');
    assert.ok(svg.includes('<svg')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg));
    assert.equal([...svg.matchAll(/class=['"]actual-segment['"]/g)].length, item.segments, item.id + ' complete observed segments');
    svgFiles++;
  }
  const summary = { designs: cases.size, pairComparisons: pairs.size, svgFiles, baseline: data.baseline };
  if (dataOnly) { process.stdout.write(JSON.stringify(summary) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
    'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const errors = [], references = new Set(); let states = 0;
  try {
    const screenshots = path.resolve('artifacts/ornament-concepts-qa');
    await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => errors.push(request.url()));
    async function assets() {
      await page.waitForFunction(() => [...document.images].filter(image => {
        const box = image.getBoundingClientRect();
        return image.getClientRects().length && image.hasAttribute('src') &&
          (image.loading !== 'lazy' || box.bottom > 0 && box.top < innerHeight && box.right > 0 && box.left < innerWidth);
      }).every(image => image.complete && image.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'horizontal page overflow');
      for (const raw of await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element =>
        ['href', 'src'].filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))))) {
        assert.ok(!raw.includes('#'), 'file links should not require fragments: ' + raw);
        const url = new URL(raw, page.url()); if (url.protocol === 'file:') references.add(url.href);
      }
      states++;
    }
    async function allImages() {
      await page.evaluate(() => document.querySelectorAll('img[src]').forEach(image => { image.loading = 'eager'; }));
      await page.waitForFunction(() => [...document.querySelectorAll('img[src]')].every(image => image.complete && image.naturalWidth > 0));
    }
    await page.goto(pathToFileURL(gallery).href);
    await page.waitForFunction(n => document.querySelector('#source')?.options.length === n, data.cases.length);
    await assets();
    function judged(pair, focus) {
      if (focus !== 'branches-legacy') return pair[focus];
      const values = [pair.branches, pair.legacy];
      return values.includes('No') ? 'No' : values.includes('Blocked') ? 'Blocked' : values.includes('Open') ? 'Open' : 'Yes';
    }
    async function state(source, focus = 'branches', display = 'accepted', groups = false) {
      await page.selectOption('#source', source);
      await page.selectOption('#focus', focus);
      await page.selectOption('#display', display);
      await page.locator('#groups').setChecked(groups);
      await assets();
      const item = cases.get(source);
      assert.equal(await page.locator('#source-image').getAttribute('src'), groups ? item.groupImage : item.image);
      const expected = data.pairs.filter(pair => pair.source === source && (display === 'all' || judged(pair, focus) === 'Yes' &&
        (display === 'accepted' || display === 'different' && pair.incidence === 'No' || display === 'vocabulary' && pair.legacy === 'No')));
      assert.equal(await page.locator('[data-target]').count(), expected.length);
      const rendered = await page.locator('[data-target]').evaluateAll(elements => elements.map(element => ({
        target: element.dataset.target, outcome: element.querySelector('.pill').textContent,
        image: element.querySelector('img').getAttribute('src')
      })));
      assert.deepEqual(rendered.map(card => card.target), expected.map(pair => pair.target));
      for (let index = 0; index < expected.length; index++) {
        const pair = expected[index], target = cases.get(pair.target), value = judged(pair, focus);
        assert.equal(rendered[index].outcome, value === 'Yes' ? 'Supported' : value === 'No' ? 'Outside this contract' : value);
        assert.equal(rendered[index].image, groups ? target.groupImage : target.image);
      }
    }
    for (const item of data.cases)
      for (const focus of ['branches', 'corners', 'legacy', 'branches-legacy']) await state(item.id, focus);
    for (const display of ['all', 'different', 'vocabulary'])
      for (const focus of ['branches', 'corners', 'legacy', 'branches-legacy']) await state(data.defaultSource, focus, display);
    await state(data.defaultSource, 'branches', 'accepted', true);
    await state(data.defaultSource);
    const alternative = baseline.find(pair => pair.branches === 'Yes' && pair.incidence === 'No' && pair.target !== pair.source);
    assert.ok(alternative, 'at least one supported alternative differs under strict incidence');
    await page.locator('[data-target="' + alternative.target + '"]').click();
    await assets();
    assert.ok(await page.locator('#inspection').isVisible());
    assert.equal(await page.locator('#inspect-target').getAttribute('src'), cases.get(alternative.target).image);
    assert.equal(await page.locator('#inspect-source').getAttribute('src'), cases.get(data.defaultSource).image);
    assert.equal(await page.locator('#disagreements tr').count(), Math.max(1, alternative.disagreements.length));
    const facts = await page.locator('#inspect-facts').innerText();
    assert.ok(facts.includes('Current contract: Yes'));
    assert.ok(facts.includes('Strict incidence: No'));
    for (const [example, target] of [['mechanics', 'R21'], ['count', 'R23'], ['rim', 'R24']]) {
      await page.locator('[data-example="' + example + '"]').click(); await assets();
      assert.equal(await page.locator('#inspect-target').getAttribute('src'), cases.get(target).image);
    }
    await page.locator('[data-example="different"]').click(); await assets(); await allImages();
    await page.screenshot({ path: path.join(screenshots, 'desktop.png'), fullPage: true });
    await page.locator('#inspection').screenshot({ path: path.join(screenshots, 'comparison.png') });
    await page.setViewportSize({ width: 390, height: 844 }); await assets();
    await page.evaluate(() => { document.documentElement.style.scrollBehavior = 'auto'; window.scrollTo(0, 0); });
    await page.screenshot({ path: path.join(screenshots, 'mobile.png') });
    await page.locator('#inspection').screenshot({ path: path.join(screenshots, 'mobile-comparison.png') });
    await page.setViewportSize({ width: 1500, height: 1100 });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href);
    await page.waitForFunction(() => document.images.length > 0); await assets(); await allImages();
    assert.equal(await page.locator('img').count(), data.cases.length);
    await page.screenshot({ path: path.join(screenshots, 'overview.png'), fullPage: true });
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, [], 'browser errors');
    process.stdout.write(JSON.stringify({ ...summary, browserStates: states, localReferences: references.size }) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
