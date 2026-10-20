const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const zlib = require('node:zlib');
const { pathToFileURL, fileURLToPath } = require('node:url');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');

const argument = process.argv.slice(2).find(value => !value.startsWith('--'));
const gallery = path.resolve(argument || path.join(__dirname, '../results/ornament-reconstruction/gallery.html'));
const directory = path.dirname(gallery), screenshots = path.resolve('artifacts/ornament-reconstruction-qa');
const dataOnly = process.argv.includes('--data-only');
const settled = code => code === '+' || code === '-';
const answer = code => code === '+' ? 'Yes' : code === '-' ? 'No' : code === '!' ? 'Blocked' : 'Open';
const channels = {
  motif: new Set(['visible motif']),
  relational: new Set(['visible motif', 'joint occurrence', 'crossing', 'continuation']),
  mechanical: new Set(['visible motif', 'joint occurrence', 'crossing', 'continuation', 'mechanical path'])
};
const objects = (columns, rows) => rows.map(row => Object.fromEntries(columns.map((name, i) => [name, row[i]])));

(async () => {
  const compressed = await fs.readFile(path.join(directory, 'diagnostics.json.gz'));
  const data = JSON.parse(zlib.gunzipSync(compressed).toString('utf8'));
  assert.equal(data.schema, 'ornament-reconstruction-v1');
  const dataScript = await fs.readFile(path.join(directory, 'data.js'), 'utf8');
  const packed = dataScript.match(/ORNAMENT_RECONSTRUCTION_GZIP\s*=\s*"([A-Za-z0-9+/=]+)"/);
  if (packed) assert.deepEqual(Buffer.from(packed[1], 'base64'), compressed, 'browser data must match the diagnostic artifact');
  else {
    const plain = dataScript.match(/ORNAMENT_RECONSTRUCTION\s*=\s*([\s\S]*);\s*$/);
    assert.ok(plain, 'browser data declaration'); assert.deepEqual(JSON.parse(plain[1]), data);
  }
  assert.equal(data.counts.archiveRoundTrip, true);
  assert.equal(data.counts.candidates, data.candidates.length); assert.equal(data.counts.sources, data.sources.length);
  assert.equal(data.counts.queries, data.queries.length); assert.equal(data.counts.decisions, data.trials.length);
  assert.equal(new Set(data.candidates.map(candidate => candidate.id)).size, data.candidates.length);
  assert.equal(new Set(data.sources.map(source => source.id)).size, data.sources.length);
  assert.equal(new Set(data.queries.map(query => query.id)).size, data.queries.length);
  assert.ok(data.counts.snapshotParticipants > 0); assert.equal(data.timing.cache.equivalent, true);
  if (data.sharedCosts) {
    assert.ok(data.sharedCosts.queryDefinitions > 0);
    assert.equal(data.sharedCosts.incrementPerClaim, 8); assert.equal(data.sharedCosts.emptyMessage, 2);
  }
  if (data.transportedGeneration) {
    assert.equal(data.transportedGeneration.preserved, true);
    const regenerated = data.transportedGeneration.recipes || data.transportedGeneration.families;
    assert.equal(data.transportedGeneration.strictGraphs, regenerated);
    assert.equal(data.transportedGeneration.answerComparisons, regenerated * data.queries.length);
  }
  const sources = new Map(data.sources.map(source => [source.id, source]));
  const candidates = new Map(data.candidates.map(candidate => [candidate.id, candidate]));
  const goals = new Map(data.goals.map(goal => [goal.id, goal]));
  const ordered = data.candidates.map((candidate, index) => ({ candidate, index }))
    .sort((a, b) => a.candidate.edges - b.candidate.edges || a.candidate.groups - b.candidate.groups || a.index - b.index)
    .map(item => item.candidate);
  const profiles = new Map();
  for (const specimen of [...data.sources, ...data.candidates]) {
    assert.equal(specimen.answers.length, data.queries.length); assert.match(specimen.answers, /^[+?!-]+$/);
    assert.ok(specimen.edges > 0 && specimen.sites > 0 && specimen.groups > 0);
    profiles.set(specimen, new Map(data.goals.map(goal => [goal.id,
      goal.queryIndices.map(index => specimen.answers[index]).join('')])));
  }
  const base = objects(data.trialColumns, data.trials);
  assert.equal(base.reduce((total, row) => total + row.enumerated, 0), data.counts.subsets);
  const dialogue = data.dialogue ? objects(data.dialogue.columns, data.dialogue.rows) : [];
  const rows = [...base, ...dialogue], admissions = new Map();
  let checkedRows = 0, robustRows = 0, wrongConsensus = 0;
  for (const row of rows) {
    const source = sources.get(row.source), goal = goals.get(row.goal);
    assert.ok(source && goal && channels[row.engine], 'declared source, goal and engine');
    assert.ok(['compact', 'reverse', 'withheld family'].includes(row.context));
    assert.equal(row.hints, row.queryIndices.length); assert.equal(row.hints, row.claimAnswers.length);
    assert.equal(new Set(row.queryIndices).size, row.hints);
    assert.ok(row.nativeCost >= 2 && Number.isInteger(row.nativeCost));
    for (let i = 0; i < row.queryIndices.length; i++) {
      const q = row.queryIndices[i]; assert.ok(q >= 0 && q < data.queries.length && Number.isInteger(q));
      assert.ok(channels[row.engine].has(data.queries[q].channel), 'sent query must be an available service');
      assert.ok(['Yes', 'No'].includes(row.claimAnswers[i]));
      assert.equal(row.claimAnswers[i], answer(source.answers[q]), 'claims must match actually assessed sender evidence');
    }
    const key = JSON.stringify([row.engine, row.context === 'withheld family' ? source.family : '', row.queryIndices, row.claimAnswers]);
    let filtered = admissions.get(key);
    if (!filtered) {
      const universe = ordered.filter(candidate => row.context !== 'withheld family' || candidate.family !== source.family);
      const accepts = (candidate, possible) => row.queryIndices.every((q, i) => {
        const code = candidate.answers[q];
        return possible && code === '?' || settled(code) && answer(code) === row.claimAnswers[i];
      });
      filtered = { admitted: universe.filter(candidate => accepts(candidate, false)), possible: universe.filter(candidate => accepts(candidate, true)) };
      admissions.set(key, filtered);
    }
    assert.equal(row.admitted, filtered.admitted.length, 'settled candidate count');
    assert.equal(row.possible, filtered.possible.length, 'Open alternatives must remain possible');
    const sourceProfile = profiles.get(source).get(goal.id), sourceKnown = !/[?!]/.test(sourceProfile);
    const safe = candidate => sourceKnown && profiles.get(candidate).get(goal.id) === sourceProfile;
    assert.equal(row.safeAdmitted, filtered.admitted.filter(safe).length);
    assert.equal(row.robust, filtered.admitted.length > 0 && filtered.possible.every(safe), 'all possible alternatives must satisfy the judge');
    const selected = filtered.admitted.length === 0 ? undefined : row.context === 'reverse'
      ? filtered.admitted.at(-1) : filtered.admitted[0];
    assert.equal(row.selected ?? null, selected?.id ?? null, 'selection follows declared preference');
    assert.equal(row.goalUnknown, [...sourceProfile].filter(code => !settled(code)).length);
    const selectedProfile = selected && profiles.get(selected).get(goal.id);
    assert.equal(row.outputUnknown, selected ? [...selectedProfile].filter(code => !settled(code)).length : goal.queryIndices.length);
    const expectedRead = !selected || !sourceKnown || /[?!]/.test(selectedProfile) ? null : sourceProfile === selectedProfile;
    assert.equal(row.selectedGoal, expectedRead, 'fresh verified output profile');
    assert.ok(['Yes', 'No', 'Open', 'Blocked'].includes(row.incidence));
    assert.ok(['Yes', 'No', 'Open', 'Blocked'].includes(row.relations));
    if (!selected) { assert.equal(row.incidence, 'Open'); assert.equal(row.relations, 'Open'); }
    if (row.relations === 'Yes') assert.equal(row.incidence, 'Yes');
    const outside = !selected ? 0 : data.queries.filter((_, q) => !goal.queryIndices.includes(q) &&
      settled(source.answers[q]) && settled(selected.answers[q]) && source.answers[q] !== selected.answers[q]).length;
    assert.equal(row.outsideFocusMismatches, outside);
    checkedRows++; if (row.robust) robustRows++;
  }
  for (const row of data.rawTrials) {
    assert.ok(sources.has(row.source)); assert.ok(['incidence and headings', 'full published relations'].includes(row.scope));
    assert.ok(row.admitted >= 0 && row.nativeCost > 0 && row.attempts >= 0);
    if (row.open !== undefined) assert.ok(row.open >= 0 && row.blocked >= 0 && row.admitted + row.open + row.blocked <= data.candidates.length);
    if (row.selected) {
      assert.ok(candidates.has(row.selected)); assert.equal(row.incidence, 'Yes');
      if (row.scope === 'full published relations') assert.equal(row.relations, 'Yes');
    }
  }
  if (data.dialogue) {
    assert.equal(data.dialogue.schema, 'ornament-dialogue-v1'); assert.equal(dialogue.length, data.dialogue.episodes.length * 2);
    const episodeKeys = new Set();
    const decisions = new Map(dialogue.map(row => [[row.source, row.engine, row.goal, row.context, row.strategy].join('|'), row]));
    for (const episode of data.dialogue.episodes) {
      const key = [episode.source, episode.engine, episode.goal, episode.context].join('|');
      assert.ok(!episodeKeys.has(key)); episodeKeys.add(key);
      assert.equal(episode.requests, episode.steps.length);
      assert.equal(episode.originalHints, episode.steps.filter(step => ['Yes', 'No'].includes(step.answer)).length);
      assert.equal(episode.originalHints - episode.finalHints, episode.pruned);
      assert.ok(episode.finalHints >= 0 && episode.finalHints <= episode.originalHints);
      const source = sources.get(episode.source);
      for (const step of episode.steps) {
        assert.ok(step.query >= 0 && step.query < data.queries.length && channels[episode.engine].has(data.queries[step.query].channel));
        assert.equal(step.answer, answer(source.answers[step.query]));
        assert.ok(step.gain > 0 && step.cost > 0 && step.before >= step.after);
        if (step.answer === 'Open' || step.answer === 'Blocked') assert.equal(step.before, step.after);
      }
      assert.equal(decisions.get(key + '|dialogue').hints, episode.originalHints);
      const pruned = decisions.get(key + '|dialogue-pruned'); assert.equal(pruned.hints, episode.finalHints);
      if (episode.uniform && pruned.selectedGoal === false) wrongConsensus++;
    }
  }
  const svgCache = new Map();
  function geometry(svg) {
    const keys = new Set(['x1', 'y1', 'x2', 'y2', 'cx', 'cy', 'r', 'd', 'points']);
    return [...svg.matchAll(/<(line|path|polyline|polygon|circle)\b([^>]*)>/g)].map(match => [match[1],
      [...match[2].matchAll(/([\w:-]+)=(['"])(.*?)\2/g)].filter(attr => keys.has(attr[1])).map(attr => [attr[1], attr[3]])]);
  }
  async function svg(relative) {
    if (!svgCache.has(relative)) {
      const contents = await fs.readFile(path.join(directory, relative), 'utf8');
      assert.ok(contents.includes('<svg')); assert.ok(!/\b(?:NaN|Infinity)\b/.test(contents));
      svgCache.set(relative, contents);
    }
    return svgCache.get(relative);
  }
  for (const specimen of [...data.candidates, ...data.sources]) {
    const normal = await svg(specimen.image), grouped = await svg(specimen.groupImage);
    assert.equal([...normal.matchAll(/class=['"]actual-segment['"]/g)].length, specimen.edges, specimen.id + ' actual segment count');
    assert.equal([...grouped.matchAll(/class=['"]actual-segment['"]/g)].length, specimen.edges);
    assert.deepEqual(geometry(grouped), geometry(normal), specimen.id + ' grouped view preserves the complete geometry');
    if (specimen.outputImage) {
      const fresh = await svg(specimen.outputImage), freshGroups = await svg(specimen.outputGroupImage);
      assert.equal([...fresh.matchAll(/class=['"]actual-segment['"]/g)].length, specimen.edges);
      assert.deepEqual(geometry(fresh), geometry(normal), specimen.id + ' fresh native execution preserves displayed geometry');
      assert.deepEqual(geometry(freshGroups), geometry(fresh));
    }
  }
  const summary = { candidates: data.candidates.length, sources: data.sources.length, queries: data.queries.length,
    decisions: checkedRows, uniqueMessageFilters: admissions.size, robustDecisions: robustRows, svgFiles: svgCache.size,
    dialogueEpisodes: data.dialogue?.episodes.length || 0, wrongConditionalConsensus: wrongConsensus };
  if (dataOnly) { process.stdout.write(JSON.stringify(summary) + '\n'); return; }

  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const errors = [], references = new Set(); let states = 0;
  try {
    await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1100 } });
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('requestfailed', request => errors.push(request.url()));
    async function assets() {
      await page.waitForFunction(() => [...document.images].filter(image => {
        const box = image.getBoundingClientRect();
        return image.loading !== 'lazy' || box.bottom > 0 && box.top < innerHeight && box.right > 0 && box.left < innerWidth;
      }).every(image => image.complete && image.naturalWidth > 0));
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'horizontal page overflow');
      for (const raw of await page.locator('[href],[src]').evaluateAll(elements => elements.flatMap(element =>
        ['href', 'src'].filter(key => element.hasAttribute(key)).map(key => element.getAttribute(key))))) {
        assert.ok(!raw.includes('#'), 'file links must not require fragments: ' + raw);
        const url = new URL(raw, page.url()); if (url.protocol !== 'file:' || references.has(url.href)) continue;
        references.add(url.href);
      }
    }
    async function open() {
      await page.goto(pathToFileURL(gallery).href);
      await page.waitForFunction(n => document.querySelector('#source')?.options.length === n, data.sources.length);
      await assets();
    }
    async function state(row, grouped = false, raw = false) {
      await page.selectOption('#source', row.source);
      if (!raw) {
        for (const key of ['goal', 'engine', 'context', 'strategy']) await page.selectOption('#' + key, row[key]);
      } else await page.selectOption('#strategy', row.scope === 'full published relations' ? 'raw-full' : 'raw-incidence');
      await page.locator('#groups').setChecked(grouped); await assets();
      const source = sources.get(row.source), target = candidates.get(row.selected);
      assert.equal(await page.locator('#source-image').getAttribute('src'), grouped ? source.groupImage : source.image);
      if (target) {
        assert.equal(await page.locator('#selected-image').getAttribute('src'), grouped
          ? target.outputGroupImage || target.groupImage : target.outputImage || target.image);
        assert.ok((await page.locator('#selected-meta').innerText()).includes(target.id));
      } else assert.equal(await page.locator('#selected-image').count(), 0);
      const outcomes = await page.locator('#outcomes .outcome strong').allTextContents();
      assert.ok(outcomes.includes(row.incidence)); assert.ok(outcomes.includes(row.relations));
      if (!raw) {
        assert.equal(outcomes[0], row.selectedGoal === null ? 'Unresolved' : row.selectedGoal ? 'Preserved' : 'Mismatch');
        if (row.robust && row.incidence === 'No') assert.ok((await page.locator('#interpretation').innerText()).includes('vocabulary'),
          'a sufficient limited read must not imply complete ornament reconstruction');
        assert.equal(await page.locator('#questions tr').count(), goals.get(row.goal).queryIndices.length);
        const questionRows = await page.locator('#questions tr').evaluateAll(elements => elements.map(element => [...element.cells].map(cell => cell.textContent)));
        for (const [i, q] of goals.get(row.goal).queryIndices.entries()) {
          assert.equal(questionRows[i][0], data.queries[q].label); assert.equal(questionRows[i][2], answer(source.answers[q]));
          assert.equal(questionRows[i][3], target ? answer(target.answers[q]) : 'Open');
        }
        if (row.strategy.startsWith('dialogue') && data.dialogue) {
          const episode = data.dialogue.episodes.find(item => item.source === row.source && item.engine === row.engine &&
            item.goal === row.goal && item.context === row.context);
          assert.ok(await page.locator('#dialogue-detail').isVisible());
          assert.equal(await page.locator('#dialogue-steps tr').count(), Math.max(1, episode.steps.length));
          if (episode.steps.length === 0) assert.ok((await page.locator('#dialogue-steps').innerText()).includes('No supplier observation'));
        }
      }
      states++;
    }
    await open();
    await page.locator('section').first().screenshot({ path: path.join(screenshots, 'desktop-default-composition.png') });
    const examples = [
      rows.find(row => row.robust && row.hints > 0 && row.hints <= 3 && row.goal === 'combined'),
      rows.find(row => row.selectedGoal === true && row.incidence === 'No' && row.hints > 0),
      rows.find(row => row.context === 'withheld family' && row.selectedGoal === false && row.hints > 0),
      rows.find(row => row.selectedGoal === null && row.hints > 0), rows.find(row => row.selected === null),
      ...['minimum', 'greedy', 'mixed', 'visible', 'relational', 'mechanical', 'empty', 'dialogue', 'dialogue-pruned']
        .map(strategy => rows.find(row => row.strategy === strategy && row.engine === 'mechanical' && row.goal === 'combined' && row.context === 'compact'))
    ].filter(Boolean);
    for (const row of examples) await state(row);
    for (const grouped of [true, false]) {
      await state(examples[0], grouped);
      await page.locator('section').first().screenshot({ path: path.join(screenshots, grouped ? 'desktop-groups.png' : 'desktop.png') });
    }
    const lostMechanics = data.rawTrials.find(row => row.mechanicalRead === false) || data.rawTrials[0];
    await page.selectOption('#goal', 'mechanics'); await state(lostMechanics, false, true);
    await page.locator('section').first().screenshot({ path: path.join(screenshots, 'complete-packet.png') });
    for (const button of await page.locator('[data-find]').all()) { await button.click(); await assets(); states++; }
    if (dialogue.length) {
      const row = dialogue.find(row => row.strategy === 'dialogue-pruned' && row.robust && row.hints > 0) || dialogue[0];
      await state(row); await page.locator('#dialogue-detail').evaluate(element => { element.open = true; });
      await page.locator('section').first().screenshot({ path: path.join(screenshots, 'dialogue.png') });
    }
    await page.setViewportSize({ width: 390, height: 844 });
    for (const row of examples) await state(row);
    await page.locator('section').first().screenshot({ path: path.join(screenshots, 'mobile.png') });
    const firstSource = data.sources.find(source => source.family === 'H') || data.sources[0];
    await page.goto(pathToFileURL(gallery).href + '?source=' + encodeURIComponent(firstSource.id));
    await page.waitForFunction(id => document.querySelector('#source')?.value === id, firstSource.id); await assets();
    await page.goto(pathToFileURL(path.join(directory, 'overview.html')).href); await assets();
    assert.equal(await page.locator('article').count(), data.candidates.length);
    const overviewLinks = await page.locator('article').evaluateAll(elements => elements.map(element => ({
      href: element.querySelector('a')?.getAttribute('href'), image: element.querySelector('img')?.getAttribute('src')
    })));
    for (const link of overviewLinks) {
      const candidate = data.candidates.find(item => item.image === link.image); assert.ok(candidate, 'overview image is a declared candidate');
      const target = new URL(link.href, page.url()); assert.equal(path.basename(target.pathname), 'gallery.html');
      assert.equal(target.searchParams.get('source'), sources.has(candidate.id) ? candidate.id : null,
        'overview must open the variant displayed in its thumbnail');
    }
    await page.setViewportSize({ width: 1500, height: 1000 }); await assets();
    await page.screenshot({ path: path.join(screenshots, 'overview-top.png') });
    for (const reference of references) {
      const info = await fs.stat(fileURLToPath(reference)); assert.ok(info.isFile() && info.size > 0, reference);
    }
    assert.deepEqual(errors, []);
    Object.assign(summary, { browserStates: states, references: references.size, errors: 0, desktop: true, mobile: true });
    await fs.writeFile(path.join(screenshots, 'verification.json'), JSON.stringify(summary, null, 2));
    process.stdout.write(JSON.stringify(summary) + '\n');
  } finally { await browser.close(); }
})().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
