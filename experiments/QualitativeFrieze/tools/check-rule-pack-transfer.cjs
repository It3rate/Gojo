const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');
const arg = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(arg || path.join(__dirname, '../results/rule-pack-transfer/gallery.html'));
const directory = path.dirname(gallery);
const dataOnly = process.argv.includes('--data-only');
const answer = value => ['Yes', 'No', 'Open', 'Blocked'].includes(value);
const known = value => value === 'Yes' || value === 'No';
const key = row => [row.pack, row.context, row.case].join('|');
const outcome = value => ({ Yes: 'Agrees', No: 'Conflicts with expectation', Open: 'Unresolved', Blocked: 'Read blocked', Unused: 'Carried unused', Waived: 'Waived; evidence retained' })[value] || value;
const status = row => row.answer === 'No' ? 'Outside required claims' : row.answer === 'Blocked' ? 'Required read blocked' : row.answer === 'Open' ? 'Required read unresolved' : 'Contract agrees';

(async () => {
  const d = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(d.schema, 'rule-pack-transfer-v1');
  const script = await fs.readFile(path.join(directory, 'data.js'), 'utf8');
  assert.deepEqual(JSON.parse(script.replace(/^globalThis\.RULE_PACK_TRANSFER\s*=\s*/, '').replace(/;\s*$/, '')), d);
  assert.equal(d.counts.designs, 54); assert.equal(d.counts.domains, 3); assert.equal(d.counts.packs, 3);
  assert.equal(d.cases.length, d.counts.designs); assert.equal(d.packs.length, 3);
  const cases = new Map(d.cases.map(c => [c.id, c])); assert.equal(cases.size, 54);
  const packs = new Map(d.packs.map(p => [p.id, p])); assert.equal(packs.size, 3);
  for (const domain of ['ORNAMENT', 'GLYPH', 'NETWORK']) {
    assert.equal(d.cases.filter(c => c.domain === domain).length, 18);
    for (const family of ['CONTOUR', 'BRIDGE', 'COURSES']) assert.equal(d.cases.filter(c => c.domain === domain && c.family === family).length, 6);
  }
  const reads = new Map(d.reads.map(row => [key(row), row])); assert.equal(reads.size, d.reads.length);
  assert.equal(d.counts.applications, d.reads.length);
  let svgFiles = 0, transportChecks = 0, expectedReads = 0;
  for (const p of d.packs) {
    assert.equal(p.contexts.length, 5); assert.equal(new Set(p.contexts.map(c => c.id)).size, 5);
    assert.ok(p.rules.length > 0); assert.ok(p.rules.every(r => r.predicateParticipants > 0));
    for (const context of p.contexts) for (const c of d.cases) {
      expectedReads++;
      const row = reads.get([p.id, context.id, c.id].join('|')); assert.ok(row);
      assert.ok(answer(row.answer)); assert.equal(row.eligible, row.answer === 'Yes' || row.answer === 'Open');
      assert.equal(row.facts.length, p.rules.length);
      for (const f of row.facts) {
        assert.ok(answer(f.answer)); assert.ok([0, 1, 2, 3].includes(f.tier));
        assert.equal(typeof f.expected, 'boolean'); assert.equal(typeof f.waived, 'boolean');
        assert.ok(['Yes', 'No', 'Open', 'Blocked', 'Unused', 'Waived'].includes(f.decision));
        const expected = f.tier === 3 ? 'Unused' : f.waived ? 'Waived' : !known(f.answer) ? f.answer :
          (f.answer === 'Yes') === f.expected ? 'Yes' : 'No';
        assert.equal(f.decision, expected, c.id + ' ' + context.id + ' actual fact/policy distinction');
      }
      const required = row.facts.filter(f => f.tier === 0).map(f => f.decision);
      assert.equal(row.answer, required.includes('Blocked') ? 'Blocked' : required.includes('No') ? 'No' : required.includes('Open') ? 'Open' : 'Yes');
      for (const [tier, lo, hi] of [[1, 'strongLower', 'strongUpper'], [2, 'weakLower', 'weakUpper']]) {
        const values = row.facts.filter(f => f.tier === tier).map(f => f.decision);
        const no = values.filter(value => value === 'No').length;
        assert.equal(row[lo], no); assert.equal(row[hi], no + values.filter(value => value === 'Open' || value === 'Blocked').length);
      }
      if (row.transportSame !== null && row.transportSame !== undefined) { transportChecks++; assert.equal(row.transportSame, true); }
    }
  }
  assert.equal(expectedReads, d.reads.length); assert.equal(transportChecks, d.counts.transportChecks);
  assert.equal(transportChecks, 54 * 3);
  for (const p of d.packs) for (const c of d.cases) {
    const row = context => reads.get([p.id, context, c.id].join('|'));
    const ordinary = row('ordinary'), waived = row('contact-exception'), symmetry = row('symmetry-preferred');
    const strict = row('construction-strict'), coarse = row('geometry-unavailable');
    for (const alternate of [waived, symmetry, coarse]) assert.equal(alternate.eligible, ordinary.eligible,
      'default/context changes cannot alter the unchanged required arrangement');
    assert.deepEqual(waived.facts.map(f => f.answer), ordinary.facts.map(f => f.answer), 'a waiver does not rewrite facts');
    assert.deepEqual(symmetry.facts.map(f => f.answer), ordinary.facts.map(f => f.answer), 'changing authority does not rewrite facts');
    assert.equal(waived.facts[1].decision, 'Waived');
    assert.equal(waived.strongLower, ordinary.strongLower - (ordinary.facts[1].answer === 'No' ? 1 : 0));
    assert.equal(coarse.facts[0].answer, ordinary.facts[0].answer, 'withholding geometry preserves the native graph read');
    assert.equal(coarse.facts[5].answer, ordinary.facts[5].answer, 'mechanical inventory is a separate exposed service');
    assert.ok(coarse.facts.slice(1, 5).every(f => f.answer === 'Open'), 'absent geometry cannot be recovered from private words');
    if (strict.eligible) assert.equal(ordinary.eligible, true, 'adding a requirement cannot admit an excluded graph');
  }
  for (const c of d.cases) for (const relative of [c.image, c.groupImage]) {
    const svg = await fs.readFile(path.join(directory, relative), 'utf8');
    assert.ok(svg.includes('<svg')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg));
    assert.ok([...svg.matchAll(/class=['"]actual-segment['"]/g)].length > 0); svgFiles++;
  }
  let stages = 0, outputs = 0, retained = 0;
  for (const g of d.generation) {
    assert.ok(g.id && g.domain && g.name && g.sourceImage); assert.ok(g.stages.length >= 6);
    assert.equal(new Set(g.stages.map(s => s.id)).size, g.stages.length);
    assert.ok((await fs.readFile(path.join(directory, g.sourceImage), 'utf8')).includes('<svg'));
    for (const s of g.stages) {
      stages++; assert.ok(answer(s.answer)); assert.ok(answer(s.sourceEquality)); assert.ok(answer(s.coarse)); assert.ok(answer(s.outputConformance));
      assert.equal(typeof s.sharedAssumed, 'boolean'); assert.ok(s.carried >= 0 && s.withdrawn >= 0);
      assert.ok(s.eligible >= s.frontier && s.frontier >= 0);
      assert.equal(new Set(s.proposals.map(p => p.id)).size, s.proposals.length);
      assert.equal(s.eligible, s.proposals.filter(p => p.sourceEligible).length);
      assert.equal(s.frontier, s.proposals.filter(p => p.frontier).length);
      assert.equal(s.proposals.filter(p => p.selected).length, s.selected ? 1 : 0);
      if (s.selected) {
        const selected = s.proposals.find(p => p.id === s.selected); assert.ok(selected); assert.equal(selected.selected, true);
        assert.equal(selected.sourceEligible, true); assert.equal(selected.frontier, true);
      }
      for (const p of s.proposals) {
        assert.ok(p.sourceOpen >= 0 && p.sourceRefuted >= 0);
        if (p.frontier) assert.equal(p.sourceEligible, true);
      }
      if (s.image) {
        outputs++; assert.ok(s.selected); assert.equal(s.answer, 'Yes'); assert.equal(s.outputConformance, 'Yes');
        const svg = await fs.readFile(path.join(directory, s.image), 'utf8'); assert.ok(svg.includes('<svg')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg));
      } else if (s.previousImage) {
        retained++; assert.ok((await fs.readFile(path.join(directory, s.previousImage), 'utf8')).includes('<svg'));
      }
    }
    const stage = id => g.stages.find(s => s.id === id);
    for (const id of ['initial', 'partial']) {
      assert.equal(stage(id).sourceEquality, 'Open', 'a successful drawing cannot confirm unpublished supplier equality');
      assert.equal(stage(id).sharedAssumed, true); assert.equal(stage(id).outputConformance, 'Yes');
    }
    assert.equal(stage('reveal').sourceEquality, 'No'); assert.equal(stage('reveal').sharedAssumed, false);
    assert.equal(stage('reveal').withdrawn, 1); assert.equal(stage('reveal').coarse, 'Yes');
    for (const id of ['conflict', 'capability-limited']) {
      assert.equal(stage(id).selected, null); assert.equal(stage(id).image, null); assert.equal(stage(id).answer, 'Open');
      assert.ok(stage(id).previousImage, 'retain history without claiming a new output');
    }
    assert.equal(stage('conflict').sourceEquality, 'Open'); assert.equal(stage('scoped').sourceEquality, 'No');
    assert.equal(stage('capability-limited').sourceEquality, 'No');
    for (const id of ['independent-prior', 'unused-prior']) {
      assert.equal(stage(id).sourceEquality, 'Open'); assert.equal(stage(id).sharedAssumed, false);
      assert.equal(stage(id).coarse, 'Yes');
    }
  }
  assert.equal(stages, d.counts.generationStages); assert.equal(d.generation.length, 3);
  assert.ok(outputs >= 3); assert.ok(retained >= 1, 'unresolved stages retain earlier output explicitly');
  const generatedSvgs = (await fs.readdir(path.join(directory, 'patterns'))).filter(file => file.endsWith('.svg'));
  for (const file of generatedSvgs) {
    const svg = await fs.readFile(path.join(directory, 'patterns', file), 'utf8');
    assert.ok(svg.includes('<svg')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(svg));
    assert.ok([...svg.matchAll(/class=['"]actual-segment['"]/g)].length > 0, file + ' retains native strokes');
  }
  assert.equal(generatedSvgs.length, 2 * (cases.size + d.generation.length + outputs));
  assert.ok((await fs.stat(path.join(directory, 'transfer.gojo.json.gz'))).size > 1000);
  const summary = { designs: cases.size, packs: packs.size, applications: reads.size, transportChecks, generationStages: stages, outputs, retained, corpusSvgFiles: svgFiles, allSvgFiles: generatedSvgs.length };
  if (dataOnly) { process.stdout.write(JSON.stringify(summary) + '\n'); return; }

  const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE || 'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
  const browser = await chromium.launch({ headless: true, executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const errors = [], references = new Set(); let statesChecked = 0;
  try {
    const screenshots = path.resolve('artifacts/rule-pack-transfer-qa'); await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
    page.on('pageerror', error => errors.push(error.message)); page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => errors.push(request.url()));
    async function assets() {
      await page.waitForFunction(() => [...document.images].filter(image => {
        const b = image.getBoundingClientRect(); return image.getClientRects().length && image.hasAttribute('src') &&
          (image.loading !== 'lazy' || b.bottom > 0 && b.top < innerHeight && b.right > 0 && b.left < innerWidth);
      }).every(image => image.complete && image.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'horizontal page overflow');
      for (const raw of await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element => ['href', 'src'].filter(k => element.hasAttribute(k)).map(k => element.getAttribute(k))))) {
        assert.ok(!raw.includes('#'), 'fragment-free local links'); const url = new URL(raw, page.url()); if (url.protocol === 'file:') references.add(url.href);
      }
      statesChecked++;
    }
    async function eager() {
      await page.evaluate(() => document.querySelectorAll('img[src]').forEach(image => { image.loading = 'eager'; }));
      await page.waitForFunction(() => [...document.querySelectorAll('img[src]')].every(image => image.complete && image.naturalWidth > 0));
    }
    await page.goto(pathToFileURL(gallery).href); await page.waitForFunction(() => document.querySelector('#pack')?.options.length > 0); await assets();
    async function transfer(p, context, domain = 'all', display = 'all', groups = false) {
      await page.selectOption('#pack', p.id); await page.selectOption('#context', context.id); await page.selectOption('#domain', domain);
      await page.selectOption('#display', display); await page.locator('#groups').setChecked(groups); await assets();
      const expected = d.cases.filter(c => domain === 'all' || c.domain === domain).filter(c => {
        const r = reads.get([p.id, context.id, c.id].join('|')); return display === 'all' || display === 'eligible' && r.eligible || display === 'excluded' && !r.eligible;
      });
      const cards = await page.locator('#transfer-cards [data-case]').evaluateAll(elements => elements.map(element => ({ id: element.dataset.case,
        status: element.querySelector('.pill').textContent, image: element.querySelector('img').getAttribute('src') })));
      assert.deepEqual(cards.map(c => c.id), expected.map(c => c.id));
      cards.forEach((c, i) => { assert.equal(c.status, status(reads.get([p.id, context.id, c.id].join('|')))); assert.equal(c.image, groups ? expected[i].groupImage : expected[i].image); });
    }
    for (const p of d.packs) for (const context of p.contexts) {
      for (const domain of ['all', 'ORNAMENT', 'GLYPH', 'NETWORK']) await transfer(p, context, domain);
      for (const display of ['eligible', 'excluded']) await transfer(p, context, 'all', display);
    }
    for (const p of d.packs) {
      await transfer(p, p.contexts[0], 'all', 'all', true);
      for (const id of ['O-C01', 'G-B02', 'N-R04']) {
        await page.locator('[data-inspect="' + id + '"]').click(); await assets();
        const r = reads.get([p.id, p.contexts[0].id, id].join('|'));
        assert.deepEqual(await page.locator('#inspect-facts .fact').allTextContents(), r.facts.map(f => f.answer));
        const colours = await page.locator('#inspect-facts .fact').evaluateAll(elements => elements.map(element => getComputedStyle(element).color));
        assert.equal(new Set(colours).size, 1, 'fact polarity has no red/green correctness colour');
        assert.equal(await page.locator('#inspect-image').getAttribute('src'), cases.get(id).groupImage);
      }
      await page.locator('#close-inspection').click(); assert.equal(await page.locator('#inspection').isVisible(), false);
    }
    await transfer(d.packs[1], d.packs[1].contexts[0], 'all', 'all'); await eager();
    await page.screenshot({ path: path.join(screenshots, 'transfer.png'), fullPage: true });
    await page.locator('#generation-tab').click(); assert.equal(await page.locator('#generation').isVisible(), true);
    for (const g of d.generation) {
      await page.selectOption('#scenario', g.id);
      for (const s of g.stages) {
        await page.selectOption('#stage', s.id); await assets();
        assert.equal(await page.locator('#generation-source').getAttribute('src'), g.sourceImage);
        assert.equal(await page.locator('#proposals [data-proposal]').count(), s.proposals.length);
        if (s.image || s.previousImage) {
          assert.equal(await page.locator('#generation-output').getAttribute('src'), s.image || s.previousImage);
          assert.equal(await page.locator('#generation-output').evaluate(element => element.classList.contains('ghost')), !s.image);
          assert.equal(await page.locator('#generation-no-output').isVisible(), false);
          assert.ok((await page.locator('#generation-output-label').innerText()).includes(s.image ? 'Actual output' : 'Retained earlier output'));
        } else { assert.equal(await page.locator('#generation-output').isVisible(), false); assert.equal(await page.locator('#generation-no-output').isVisible(), true); }
        assert.ok((await page.locator('#generation-reads').innerText()).includes(s.sourceEquality));
        assert.equal(await page.locator('.stage-button[aria-selected="true"]').getAttribute('data-stage'), s.id);
      }
      await page.locator('[data-stage="' + g.stages[0].id + '"]').click(); await assets();
      assert.equal(await page.locator('#stage').inputValue(), g.stages[0].id);
    }
    const g = d.generation[0]; await page.selectOption('#scenario', g.id);
    const revision = g.stages.find(s => s.withdrawn > 0) || g.stages.at(-1); await page.selectOption('#stage', revision.id); await assets(); await eager();
    await page.screenshot({ path: path.join(screenshots, 'generation-revised.png'), fullPage: true });
    const held = g.stages.find(s => !s.image && s.previousImage); if (held) { await page.selectOption('#stage', held.id); await assets(); await page.screenshot({ path: path.join(screenshots, 'generation-retained.png'), fullPage: true }); }
    await page.setViewportSize({ width: 390, height: 844 }); await assets(); await page.screenshot({ path: path.join(screenshots, 'mobile-generation.png'), fullPage: true });
    await page.locator('#transfer-tab').click(); await assets(); await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ path: path.join(screenshots, 'mobile-transfer.png') });
    await page.setViewportSize({ width: 1500, height: 1100 }); await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await eager(); await assets();
    assert.equal(await page.locator('#cases [data-case]').count(), 54); assert.equal(await page.locator('#cases [data-domain]').count(), 3);
    await page.screenshot({ path: path.join(screenshots, 'overview.png'), fullPage: true });
    await page.locator('#cases [data-case="N-R04"]').click(); await assets(); assert.equal(await page.locator('#inspect-image').getAttribute('src'), cases.get('N-R04').image);
    assert.equal(await page.locator('#pack').inputValue(), 'COURSES', 'overview links select the displayed family question explicitly');
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await page.locator('#generation-cards [data-scenario]').first().click(); await assets();
    assert.equal(await page.locator('#generation').isVisible(), true);
    for (const reference of references) await fs.access(fileURLToPath(new URL(reference)));
    assert.deepEqual(errors, [], 'browser errors');
    process.stdout.write(JSON.stringify({ ...summary, browserStates: statesChecked, localReferences: references.size }) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
