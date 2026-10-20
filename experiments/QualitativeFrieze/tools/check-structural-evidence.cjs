const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL, fileURLToPath } = require('node:url');
const { chromium } = require(process.env.GOJO_PLAYWRIGHT_MODULE ||
  'C:/Users/Robin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const gallery = path.resolve(process.argv[2] || path.join(__dirname, '../results/structural-evidence/gallery.html'));
const directory = path.dirname(gallery), screenshots = path.resolve('artifacts/structural-evidence-qa');

(async () => {
  const model = JSON.parse(await fs.readFile(path.join(directory, 'diagnostics.json'), 'utf8'));
  assert.equal(model.schema, 'structural-evidence-v1');
  assert.equal(model.cases.length, 6); assert.equal(model.episodes.length, 27);
  assert.equal(model.nativeNumericElements, 0); assert.equal(model.archiveRoundTrip, true);
  assert.equal(model.policy.structuralTrainingEpisodes, 0);
  const old = JSON.parse(await fs.readFile(path.join(directory, '../evidence-selection/diagnostics.json'), 'utf8'));
  assert.deepEqual(model.policy.coefficients, old.training.coefficients);
  for (const summary of model.summaries) { assert.equal(summary.resolved, 5); assert.equal(summary.open, 1); }
  const transferred = model.episodes.filter(e => e.policy === 'transferred' && e.control === null);
  assert.equal(transferred.filter(e => e.correctAtVocabulary).length, 5);
  for (const episode of model.episodes) {
    assert.equal(episode.stages[0].name, '0 · Start: no source facts yet');
    for (const [stageIndex, stage] of episode.stages.entries()) {
      for (const read of stage.reads) {
        assert.ok(Object.hasOwn(read, 'answerToQuestion'), `${episode.id}: candidate answer must be exported`);
        if (stageIndex === 0) assert.equal(read.answerToQuestion, null);
        else assert.ok(['Yes', 'No', 'Open'].includes(read.answerToQuestion));
      }
    }
  }
  const closure = model.episodes.find(e => e.caseId === 'S01' && e.policy === 'fixed' && e.control === null);
  assert.equal(closure.stages[0].eligible.length, 4);
  assert.equal(closure.stages[1].answer, 'No');
  assert.equal(closure.stages[1].eligible.length, 4);
  assert.ok(closure.stages[1].reads.every(read => read.answerToQuestion === 'No'));
  assert.equal(closure.stages[2].question, 'Is there a closed loop with exactly three edges?');
  assert.equal(closure.stages[2].answer, 'Yes');
  assert.deepEqual(closure.stages[2].eligible, [3]);
  assert.equal(closure.stages[2].reads.find(read => read.index === 1).answerToQuestion, 'No');
  assert.equal(closure.stages[2].reads.find(read => read.index === 3).answerToQuestion, 'Yes');
  const ambiguous = transferred.find(e => e.caseId === 'S06');
  assert.equal(ambiguous.correctAmbiguity, true); assert.equal(ambiguous.stages.at(-1).eligible.length, 2);
  const controls = Object.fromEntries(model.episodes.filter(e => e.control).map(e => [e.control, e]));
  assert.ok(controls.unsearched.stages.slice(1).every(s => s.answer === 'Open'));
  assert.ok(controls.conflict.hasConflict); assert.equal(controls.conflict.stages.at(-1).eligible.length, 0);
  assert.ok(controls.incapable.hasBlocked); assert.equal(controls.incapable.requests, 0);
  const crossing = await fs.readFile(path.join(directory, 'S02-source.svg'), 'utf8');
  assert.match(crossing, /class='declared-crossing'/);
  const fragments = await fs.readFile(path.join(directory, 'S04-candidate-0.svg'), 'utf8');
  assert.match(fragments, /class='separate-sites'/);
  const browser = await chromium.launch({ headless: true,
    executablePath: process.env.GOJO_BROWSER || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const references = new Set(), errors = [];
  try {
    await fs.mkdir(screenshots, { recursive: true });
    const page = await browser.newPage({ viewport: { width: 1500, height: 1050 } });
    page.on('pageerror', e => errors.push(e.message));
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    page.on('requestfailed', r => errors.push(r.url()));
    async function check() {
      await page.waitForFunction(() => [...document.images].every(i => i.complete && i.naturalWidth > 0));
      for (const raw of await page.locator('[href],[src]').evaluateAll(es => es.flatMap(e =>
        ['href', 'src'].filter(k => e.hasAttribute(k)).map(k => e.getAttribute(k))))) {
        const url = new URL(raw, page.url());
        if (url.protocol !== 'file:' || references.has(url.href)) continue;
        const info = await fs.stat(fileURLToPath(url)); assert.ok(info.isFile() && info.size > 0); references.add(url.href);
      }
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
    }
    await page.goto(pathToFileURL(gallery).href); await check();
    assert.equal(await page.locator('#cases article').count(), 6);
    assert.equal(await page.locator('#controls article').count(), 3);
    for (const example of model.cases) {
      const article = page.locator(`#cases article[data-case="${example.id}"]`);
      for (const episode of model.episodes.filter(e => e.caseId === example.id && e.control === null)) {
        await article.locator('select').selectOption(episode.id);
        for (let index = 0; index < episode.stages.length; index++) {
          await article.locator(`[data-step="${index}"]`).click();
          assert.equal(await article.getAttribute('data-stage'), String(index));
          const stage = episode.stages[index];
          for (const candidate of example.candidates) {
            const card = article.locator(`[data-candidate="${candidate.index}"]`);
            const read = stage.reads.find(r => r.index === candidate.index);
            assert.equal(await card.evaluate(e => e.classList.contains('rejected')), !stage.eligible.includes(candidate.index));
            const candidateAnswer = await card.locator('.candidate-answer').textContent();
            if (index === 0) assert.match(candidateAnswer, /Not asked yet/);
            else {
              assert.match(candidateAnswer, /^This candidate answers:/);
              assert.ok(candidateAnswer.includes(read.answerToQuestion), `${episode.id}: displayed answer disagrees with native prediction`);
            }
            assert.match(await card.locator('.badge').textContent(), /^(Still possible|Ruled out|Not yet tested|Blocked)/);
          }
          if (index === 0) {
            assert.match(await article.locator('.evidence').textContent(), /start|starting|baseline/i);
            assert.equal(await article.locator('.candidate.resolved').count(), 0);
          }
        }
      }
      await article.locator('select').selectOption(example.id + '-transferred');
      await article.locator('[data-step]').last().click();
    }
    const closureArticle = page.locator('#cases article[data-case="S01"]');
    await closureArticle.locator('select').selectOption(closure.id);
    await closureArticle.locator('[data-step="1"]').click();
    assert.equal(await closureArticle.locator('.candidate.resolved').count(), 0);
    assert.equal(await closureArticle.locator('.candidate.kept').count(), 4);
    assert.equal(await closureArticle.locator('.candidate.rejected').count(), 0);
    for (const candidate of await closureArticle.locator('.candidate').all()) {
      assert.equal((await candidate.locator('.candidate-answer').textContent()).trim(), 'This candidate answers: No');
      assert.match(await candidate.locator('.badge').textContent(), /^Still possible/);
    }
    assert.match(await closureArticle.locator('.evidence').textContent(), /cannot distinguish|does not distinguish|nondiscriminating|no alternatives.*ruled out/i);
    await closureArticle.screenshot({ path: path.join(screenshots, 'nondiscriminating-no.png') });
    await closureArticle.locator('[data-step="2"]').click();
    assert.match(await closureArticle.locator('.evidence').textContent(), /closed loop with exactly three edges/);
    assert.match(await closureArticle.locator('[data-candidate="1"] .candidate-answer').textContent(), /answers: No/);
    assert.match(await closureArticle.locator('[data-candidate="1"] .badge').textContent(), /^Ruled out/);
    assert.match(await closureArticle.locator('[data-candidate="3"] .candidate-answer').textContent(), /answers: Yes/);
    assert.equal(await closureArticle.locator('.candidate.resolved').count(), 1);
    await closureArticle.screenshot({ path: path.join(screenshots, 'closed-loop-question.png') });
    const crossingArticle = page.locator('#cases article[data-case="S02"]');
    assert.equal(await crossingArticle.locator('.candidate.resolved').count(), 1);
    assert.match(await crossingArticle.locator('.candidate.resolved .candidate-answer').textContent(), /answers: No/,
      'A sole survivor can answer No: green is compatibility, not an affirmative answer');
    await check();
    await page.screenshot({ path: path.join(screenshots, 'gallery-desktop.png'), fullPage: true });
    await page.locator('#cases article[data-case="S02"]').screenshot({ path: path.join(screenshots, 'crossing-question.png') });
    await page.locator('#cases article[data-case="S04"]').screenshot({ path: path.join(screenshots, 'continuity-question.png') });
    await page.locator('#cases article[data-case="S06"]').screenshot({ path: path.join(screenshots, 'recipe-ambiguity.png') });
    for (const details of await page.locator('details').all()) await details.locator('summary').click();
    await check();
    await page.setViewportSize({ width: 390, height: 844 }); await check();
    await page.screenshot({ path: path.join(screenshots, 'gallery-mobile.png'), fullPage: true });
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({ cases: 6, episodes: 27, controls: 3, policies: 4,
      localReferences: references.size, browserErrors: errors.length, screenshots }, null, 2));
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
