/* Presentation controls over captured Gojo reads. No construction or inference runs here. */
(() => {
  const cases = [...document.querySelectorAll('.case-panel')];
  const picker = document.getElementById('case-select');
  if (!cases.length || !picker) return;
  let active = 0, view = 'ribbon';
  const crossSelection = new Map();
  function selectCross(index) {
    const panel = cases[active], details = [...panel.querySelectorAll('.cross-witness')];
    if (!details.length) return;
    index = (index + details.length) % details.length;
    crossSelection.set(panel.dataset.case, index);
    details.forEach((el, i) => el.hidden = i !== index);
    const key = details[index].dataset.cross;
    panel.querySelectorAll('.cross-pick').forEach(el => {
      const selected = el.dataset.cross === key;
      el.classList.toggle('selected', selected);
      el.setAttribute('aria-pressed', String(selected));
    });
    panel.querySelectorAll('.depth-visit').forEach(el => el.classList.toggle('selected', el.dataset.cross === key));
    const output = panel.querySelector('.cross-count');
    if (output) output.textContent = `${index + 1} of ${details.length}`;
  }
  function selectCase(index) {
    active = (index + cases.length) % cases.length;
    cases.forEach((panel, i) => panel.hidden = i !== active);
    picker.value = cases[active].dataset.case;
    document.querySelectorAll('[data-pick-case]').forEach(el => el.setAttribute('aria-current', String(el.dataset.pickCase === picker.value)));
    selectView(view);
    selectCross(crossSelection.get(picker.value) || 0);
    history.replaceState(null, '', `?case=${encodeURIComponent(picker.value)}&view=${view}${location.hash}`);
  }
  function selectView(next) {
    view = next;
    document.querySelectorAll('.view-svg').forEach(el => el.hidden = el.dataset.view !== view);
    document.querySelectorAll('[data-select-view]').forEach(el => el.setAttribute('aria-pressed', String(el.dataset.selectView === view)));
    const desc = {
      ribbon:'The visible black ribbon. Local undercuts hide a small part of the lower strand while its native route stays connected.',
      strands:'Every present route is colored by strand and remains visible through a crossing. Arrows show the selected traversal.',
      depth:'Local depth samples are raised or lowered for display. The depths between encounters remain unspecified.'
    };
    document.getElementById('view-description').textContent = desc[view];
  }
  picker.addEventListener('change', () => selectCase(cases.findIndex(el => el.dataset.case === picker.value)));
  picker.addEventListener('keydown', e => {
    if (['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(e.key)) {
      e.preventDefault(); selectCase(active + (e.key === 'ArrowLeft' || e.key === 'ArrowUp' ? -1 : 1));
    }
  });
  document.getElementById('previous-case').addEventListener('click', () => selectCase(active - 1));
  document.getElementById('next-case').addEventListener('click', () => selectCase(active + 1));
  document.querySelectorAll('[data-select-view]').forEach(el => el.addEventListener('click', () => {
    selectView(el.dataset.selectView);
    history.replaceState(null, '', `?case=${encodeURIComponent(picker.value)}&view=${view}${location.hash}`);
  }));
  document.getElementById('show-labels').addEventListener('change', e => document.body.classList.toggle('hide-labels', !e.target.checked));
  document.querySelectorAll('[data-pick-case]').forEach(el => el.addEventListener('click', e => {
    e.preventDefault(); selectCase(cases.findIndex(c => c.dataset.case === el.dataset.pickCase));
    document.getElementById('inspect').scrollIntoView({block:'start'});
  }));
  cases.forEach(panel => {
    const select = key => selectCross([...panel.querySelectorAll('.cross-witness')].findIndex(el => el.dataset.cross === key));
    panel.querySelectorAll('.cross-pick,.depth-visit').forEach(el => {
      el.addEventListener('click', () => select(el.dataset.cross));
      el.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); select(el.dataset.cross); } });
    });
    panel.querySelector('.previous-cross')?.addEventListener('click', () => selectCross((crossSelection.get(panel.dataset.case) || 0) - 1));
    panel.querySelector('.next-cross')?.addEventListener('click', () => selectCross((crossSelection.get(panel.dataset.case) || 0) + 1));
  });
  const params = new URLSearchParams(location.search);
  if (['ribbon','strands','depth'].includes(params.get('view'))) view = params.get('view');
  const selected = cases.findIndex(el => el.dataset.case === params.get('case'));
  document.documentElement.classList.add('js');
  selectCase(Math.max(0, selected));
})();
