const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');

const argument = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(argument || path.join(__dirname, '../results/ornament-defaults/gallery.html'));
const directory = path.dirname(gallery);
const dataOnly = process.argv.includes('--data-only');
const known = value => value === 'Yes' || value === 'No';
const answers = ['Yes', 'No', 'Open', 'Blocked'];
const sameSet = (actual, expected, message) => assert.deepEqual([...actual].sort(), [...expected].sort(), message);
function preference(first, second) {
  if (!first.eligible || !second.eligible) return null;
  for (const [al, au, bl, bu] of [
    [first.strongLower, first.strongUpper, second.strongLower, second.strongUpper],
    [first.weakLower, first.weakUpper, second.weakLower, second.weakUpper]]) {
    if (au < bl) return -1;
    if (bu < al) return 1;
    if (al !== au || bl !== bu) return null;
  }
  return 0;
}

(async () => {
  const data = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(data.schema, 'ornament-defaults-v1');
  const browserData = await fs.readFile(path.join(directory, 'data.js'), 'utf8');
  assert.deepEqual(JSON.parse(browserData.replace(/^globalThis\.ORNAMENT_DEFAULTS\s*=\s*/, '').replace(/;\s*$/, '')), data);
  assert.equal(data.counts.designs, 72); assert.equal(data.counts.families, 6); assert.equal(data.counts.rules, 12);
  assert.equal(data.rules.length, 12); assert.equal(data.families.length, 6);
  assert.equal(data.counts.transportAnswers, 72 * 12);
  assert.deepEqual(data.standards, { aspectLimit: 4, resolutionLimit: 8, envelopeHalfSpan: 8 });
  assert.equal(new Set(data.rules.map(rule => rule.id)).size, data.rules.length);
  const allCases = new Map(); let profiles = 0, reads = 0, signatures = 0, svgFiles = 0;
  for (const family of data.families) {
    assert.equal(family.cases.length, 12);
    const cases = new Map(family.cases.map(item => [item.id, item]));
    assert.equal(cases.size, 12); assert.ok(cases.has(family.referenceId));
    assert.equal(family.profiles.length, 11);
    assert.equal(new Set(family.profiles.map(profile => profile.id)).size, family.profiles.length);
    for (const item of family.cases) {
      assert.ok(!allCases.has(item.id)); allCases.set(item.id, item);
      assert.equal(item.facts.length, 12); assert.ok(item.facts.every(answer => answers.includes(answer)));
      assert.equal(item.facts[5], 'Open', 'outlines cannot establish occupied-interior ownership');
      assert.ok(item.sites > 0 && item.segments > 0 && item.groups > 0);
      for (const relative of [item.image, item.groupImage, item.envelopeImage, item.envelopeGroupImage]) {
        const svg = await fs.readFile(path.join(directory, relative), 'utf8');
        assert.ok(svg.includes('<svg')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg));
        assert.equal([...svg.matchAll(/class=['"]actual-segment['"]/g)].length, item.segments, item.id + ' complete native strokes');
        assert.equal([...svg.matchAll(/class=['"]selected-envelope['"]/g)].length,
          relative === item.envelopeImage || relative === item.envelopeGroupImage ? 1 : 0);
        svgFiles++;
      }
    }
    for (const profile of [...family.profiles, ...family.interventions]) {
      profiles++; reads += profile.results.length;
      assert.equal(profile.results.length, 12); assert.equal(profile.tiers.length, 12); assert.equal(profile.expected.length, 12);
      assert.ok(profile.tiers.every(tier => Number.isInteger(tier) && tier >= 0 && tier <= 3));
      assert.ok(profile.expected.every(expected => typeof expected === 'boolean'));
      assert.equal(new Set(profile.waived).size, profile.waived.length);
      assert.ok(profile.waived.every(rule => profile.tiers[rule] === 1 || profile.tiers[rule] === 2));
      for (const result of profile.results) {
        const item = cases.get(result.id); assert.ok(item); assert.equal(result.reads.length, 12);
        const expectedReads = item.facts.map((fact, rule) => profile.tiers[rule] === 3 ? 'Unused' : profile.waived.includes(rule) ? 'Waived' :
          !known(fact) ? fact : (fact === 'Yes') === profile.expected[rule] ? 'Yes' : 'No');
        assert.deepEqual(result.reads, expectedReads, family.id + ' ' + profile.id + ' ' + item.id + ' actual policy decisions');
        const required = expectedReads.filter((_, rule) => profile.tiers[rule] === 0);
        const answer = required.includes('Blocked') ? 'Blocked' : required.includes('No') ? 'No' : required.includes('Open') ? 'Open' : 'Yes';
        assert.equal(result.answer, answer); assert.equal(result.eligible, answer === 'Yes' || answer === 'Open');
        for (const [tier, lo, hi] of [[1, 'strongLower', 'strongUpper'], [2, 'weakLower', 'weakUpper']]) {
          const decisions = expectedReads.filter((_, rule) => profile.tiers[rule] === tier);
          const lower = decisions.filter(value => value === 'No').length;
          assert.equal(result[lo], lower); assert.equal(result[hi], lower + decisions.filter(value => value === 'Open' || value === 'Blocked').length);
        }
        assert.equal(result.frontier, result.eligible && !profile.results.some(other => preference(other, result) === -1),
          family.id + ' ' + profile.id + ' ' + item.id + ' native preference frontier');
      }
      if (profile.id === 'free') assert.ok(profile.results.every(result => result.answer === 'Yes' && result.frontier));
      if (profile.id === 'ordinary') assert.ok(profile.results.every(result => result.answer === 'Yes'), 'defaults alone never reject');
      if (profile.id === 'require-fill') assert.ok(profile.results.every(result => result.answer === 'Open'), 'missing required supplier remains open');
    }
    const reference = cases.get(family.referenceId);
    const goals = family.cases.filter(item => item.facts[8] === 'Yes').map(item => item.id);
    sameSet(family.signatures.goalIds, goals);
    const basis = [0, 1, 2, 3, 4, 6, 7, 9, 10, 11].filter(rule => known(reference.facts[rule]));
    const subsets = Array.from({ length: 2 ** basis.length }, (_, mask) => {
      const rules = basis.filter((_, bit) => mask & (1 << bit));
      const accepted = family.cases.filter(item => rules.every(rule => !known(item.facts[rule]) || item.facts[rule] === reference.facts[rule])).map(item => item.id);
      return { rules, accepted, falseAccepted: accepted.filter(id => !goals.includes(id)), falseExcluded: goals.filter(id => !accepted.includes(id)) };
    });
    signatures += subsets.length; assert.equal(family.signatures.checked, subsets.length);
    assert.equal(family.signatures.exactPossible, subsets.some(item => !item.falseAccepted.length && !item.falseExcluded.length));
    for (const solution of family.signatures.solutions) {
      reads += 12;
      assert.equal(solution.rules.length, solution.expected.length);
      assert.ok(solution.rules.every(rule => rule !== 8 && rule !== 5 && basis.includes(rule)));
      const actual = subsets.find(item => JSON.stringify(item.rules) === JSON.stringify(solution.rules)); assert.ok(actual);
      sameSet(solution.accepted, actual.accepted); sameSet(solution.falseAccepted, actual.falseAccepted); sameSet(solution.falseExcluded, actual.falseExcluded);
      assert.deepEqual(solution.expected, solution.rules.map(rule => reference.facts[rule] === 'Yes'));
      assert.ok(Number.isInteger(solution.participants) && solution.participants > 0);
      assert.ok(!subsets.some(other => other.falseAccepted.length <= actual.falseAccepted.length && other.falseExcluded.length <= actual.falseExcluded.length && other.rules.length <= actual.rules.length &&
        (other.falseAccepted.length < actual.falseAccepted.length || other.falseExcluded.length < actual.falseExcluded.length || other.rules.length < actual.rules.length)), 'nondominated finite description');
    }
  }
  assert.equal(allCases.size, data.counts.designs); assert.equal(reads, data.counts.policyReads);
  assert.equal(signatures, data.counts.signaturesChecked);
  const native = await fs.stat(path.join(directory, 'defaults.gojo.json.gz')); assert.ok(native.size > 1000);
  const summary = { designs: allCases.size, profiles, policyReads: reads, signaturesChecked: signatures, svgFiles };
  if (dataOnly) { process.stdout.write(JSON.stringify(summary) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
    'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const errors = [], references = new Set(); let states = 0;
  try {
    const screenshots = path.resolve('artifacts/ornament-defaults-qa'); await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => errors.push(request.url()));
    async function assets() {
      await page.waitForFunction(() => [...document.images].filter(image => {
        const box = image.getBoundingClientRect(); return image.getClientRects().length && image.hasAttribute('src') &&
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
    await page.waitForFunction(n => document.querySelector('#family')?.options.length === n, data.families.length);
    await assets();
    async function state(family, profile, display = 'all', groups = false, envelope = false) {
      await page.selectOption('#family', family.id); await page.selectOption('#profile', profile.key);
      await page.selectOption('#display', display); await page.locator('#groups').setChecked(groups);
      await page.locator('#envelope').setChecked(envelope); await assets();
      const image = item => envelope ? groups ? item.envelopeGroupImage : item.envelopeImage : groups ? item.groupImage : item.image;
      const expected = family.cases.filter(item => {
        const result = profile.results.find(result => result.id === item.id);
        return display === 'all' || display === 'preferred' && result.frontier || display === 'admissible' && result.eligible || display === 'excluded' && !result.eligible;
      });
      const actual = await page.locator('[data-target]').evaluateAll(elements => elements.map(element => ({ id: element.dataset.target,
        image: element.querySelector('img').getAttribute('src'), status: element.querySelector('.pill').textContent })));
      assert.deepEqual(actual.map(item => item.id), expected.map(item => item.id));
      const status = result => result.answer === 'No' ? 'Outside required claims' : result.answer === 'Blocked' ? 'Required read blocked' :
        result.answer === 'Open' ? 'Required read unresolved' : profile.tiers.includes(0) ? 'Requirements satisfied' :
          profile.id === 'free' ? 'No required claims' : 'Possible under defaults';
      actual.forEach((item, index) => {
        assert.equal(item.image, image(expected[index]));
        assert.equal(item.status, status(profile.results.find(result => result.id === item.id)));
      });
      assert.equal(await page.locator('#policy-rules tr').count(), 12);
      assert.ok((await page.locator('#signatures').innerText()).includes(family.signatures.exactPossible ?
        'An exact separating conjunction exists in this finite corpus.' :
        'No single conjunction of the tested reference cues exactly separates this target set.'), 'restricted rule-language claim');
      const reference = family.cases.find(item => item.id === family.referenceId);
      assert.equal(await page.locator('#reference-image').getAttribute('src'), image(reference));
      return expected;
    }
    for (const family of data.families) {
      for (const profile of family.profiles.map(item => ({ ...item, key: 'profile/' + item.id })))
        for (const display of ['all', 'preferred', 'admissible', 'excluded']) await state(family, profile, display);
      for (const profile of family.interventions.map(item => ({ ...item, key: 'intervention/' + item.id }))) await state(family, profile);
      const ordinary = { ...family.profiles.find(item => item.id === 'ordinary'), key: 'profile/ordinary' };
      await state(family, ordinary, 'all', false, true);
      await state(family, ordinary, 'all', true, true);
      await state(family, ordinary, 'all', true);
      await page.locator('[data-inspect="' + family.referenceId + '"]').click(); await assets();
      assert.equal(await page.locator('#inspect-facts tr').count(), 12);
      const reference = family.cases.find(item => item.id === family.referenceId);
      const facts = await page.locator('#inspect-facts .fact').allTextContents(); assert.deepEqual(facts, reference.facts);
      const colours = await page.locator('#inspect-facts .fact').evaluateAll(elements => elements.map(element => getComputedStyle(element).color));
      assert.equal(new Set(colours).size, 1, 'Yes, No and Open facts use neutral equal colour');
      const result = ordinary.results.find(result => result.id === reference.id);
      assert.ok((await page.locator('#inspect-result').innerText()).includes('Strong defaults: ' + (result.strongLower === result.strongUpper ? result.strongLower : result.strongLower + '–' + result.strongUpper)));
      await page.locator('#close-inspection').click(); assert.equal(await page.locator('#inspection').isVisible(), false);
    }
    const lobes = data.families.find(family => family.id === 'LOBES');
    await state(lobes, { ...lobes.profiles.find(profile => profile.id === 'pin-closed-units'), key: 'profile/pin-closed-units' });
    await allImages(); await page.screenshot({ path: path.join(screenshots, 'desktop-lobes.png'), fullPage: true });
    await page.locator('[data-inspect="P02"]').click(); await assets();
    await page.locator('#inspection').screenshot({ path: path.join(screenshots, 'two-petal-contract.png') });
    const loop = data.families.find(family => family.id === 'LOOP');
    await state(loop, { ...loop.profiles.find(profile => profile.id === 'ordinary'), key: 'profile/ordinary' });
    assert.equal(await page.locator('#inspection').isVisible(), false, 'changing family clears stale candidate inspection');
    await page.locator('[data-inspect="C10"]').click(); await assets();
    await page.locator('#inspection').screenshot({ path: path.join(screenshots, 'thin-default.png') });
    await page.locator('#envelope').setChecked(true); await assets();
    assert.equal(await page.locator('#inspect-target').getAttribute('src'), allCases.get('C10').envelopeImage);
    await page.locator('#inspection').screenshot({ path: path.join(screenshots, 'thin-boundary.png') });
    await page.setViewportSize({ width: 390, height: 844 }); await assets();
    await page.evaluate(() => { document.documentElement.style.scrollBehavior = 'auto'; window.scrollTo(0, 0); });
    await page.screenshot({ path: path.join(screenshots, 'mobile.png') });
    await page.locator('#inspection').screenshot({ path: path.join(screenshots, 'mobile-comparison.png') });
    await page.setViewportSize({ width: 1500, height: 1100 });
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await allImages(); await assets();
    assert.equal(await page.locator('img').count(), 72); assert.equal(await page.locator('[data-family]').count(), 6);
    await page.screenshot({ path: path.join(screenshots, 'overview.png'), fullPage: true });
    await page.locator('a[href="gallery.html?family=LOBES&case=P02"]').click(); await assets();
    assert.equal(await page.locator('#family').inputValue(), 'LOBES'); assert.ok(await page.locator('#inspection').isVisible());
    assert.equal(await page.locator('#inspect-target').getAttribute('src'), allCases.get('P02').image);
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, [], 'browser errors');
    process.stdout.write(JSON.stringify({ ...summary, browserStates: states, localReferences: references.size }) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
