// Select captured applications. No point transformation or inference occurs here.
(() => {
  const template = document.querySelector('#template-select');
  const offset = document.querySelector('#offset-select');
  const code = document.querySelector('#action-code');
  const panels = [...document.querySelectorAll('.action-panel')];
  const bits = [...document.querySelectorAll('.bit-toggle')];
  if (!template || !offset || !code || !panels.length) return;
  const actions = new Map(panels.map(panel => [panel.dataset.template + '/' + panel.dataset.code + '/' + panel.dataset.offset, panel]));
  const show = () => {
    const panel = actions.get(template.value + '/' + code.value + '/' + offset.value);
    if (!panel) return;
    panels.forEach(item => {item.hidden = item !== panel;});
    bits.forEach(button => {
      const value = panel.dataset.bits[3 - Number(button.dataset.bit)];
      button.querySelector('strong').textContent = value;
      button.setAttribute('aria-pressed', String(value === '1'));
    });
    const params = new URLSearchParams(location.search);
    params.set('template', template.value);
    params.set('code', code.value);
    params.set('offset', offset.value);
    history.replaceState(null, '', location.pathname + '?' + params + location.hash);
  };
  const chooseOption = (select, value) => {
    if ([...select.options].some(option => option.value === value)) select.value = value;
  };
  const params = new URLSearchParams(location.search);
  chooseOption(template, params.get('template'));
  chooseOption(offset, params.get('offset'));
  chooseOption(code, params.get('code'));
  [template, offset, code].forEach(select => select.addEventListener('change', show));
  bits.forEach(button => button.addEventListener('click', () => {
    // The bitwise change selects an action address, not a geometric result.
    code.value = String(Number(code.value) ^ (1 << Number(button.dataset.bit)));
    show();
  }));
  const advance = delta => {code.value = String((Number(code.value) + delta + 16) % 16);show();};
  document.querySelector('#previous-action').addEventListener('click', () => advance(-1));
  document.querySelector('#next-action').addEventListener('click', () => advance(1));
  const keyboard = (select, after) => select.addEventListener('keydown', event => {
    if (!['ArrowLeft','ArrowUp','ArrowRight','ArrowDown'].includes(event.key)) return;
    event.preventDefault();
    const delta = ['ArrowLeft','ArrowUp'].includes(event.key) ? -1 : 1;
    select.selectedIndex = (select.selectedIndex + delta + select.options.length) % select.options.length;
    after();
  });
  [template, offset, code].forEach(select => keyboard(select, show));
  document.querySelector('#show-arrows').addEventListener('change', event => document.body.classList.toggle('hide-arrows', !event.target.checked));

  const engines = [...document.querySelectorAll('.engine-panel')];
  const engineSelect = document.querySelector('#engine-select');
  if (engines.length && engineSelect) {
    const choose = id => {
      if (!engines.some(panel => panel.dataset.engine === id)) return;
      engineSelect.value = id;
      engines.forEach(panel => {panel.hidden = panel.dataset.engine !== id;});
    };
    engineSelect.addEventListener('change', () => choose(engineSelect.value));
    keyboard(engineSelect, () => choose(engineSelect.value));
    const next = delta => {
      engineSelect.selectedIndex = (engineSelect.selectedIndex + delta + engineSelect.options.length) % engineSelect.options.length;
      choose(engineSelect.value);
    };
    document.querySelector('#previous-engine').addEventListener('click', () => next(-1));
    document.querySelector('#next-engine').addEventListener('click', () => next(1));
    document.querySelectorAll('[data-select-engine]').forEach(button => button.addEventListener('click', () => {
      choose(button.dataset.selectEngine);
      document.querySelector('#engines').scrollIntoView({behavior:'smooth',block:'start'});
    }));
  }
  document.documentElement.classList.add('js');
  show();
})();
