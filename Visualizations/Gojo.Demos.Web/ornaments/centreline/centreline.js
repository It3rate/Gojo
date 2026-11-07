// Presentation selection only. Every path, group and comparison was captured by
// the native study; this file performs no symmetry or topology computation.
(() => {
  const panels = [...document.querySelectorAll('.pattern-panel:not(.variant-panel)')];
  const select = document.querySelector('#pattern-select');
  if (!panels.length || !select) return;
  const ids = panels.map(panel => panel.dataset.pattern);
  let active = ids.includes(location.hash.replace('#pattern-', '')) ? location.hash.replace('#pattern-', '') : 'CR4';
  if (!ids.includes(active)) active = ids[0];

  const choose = (id, navigate = false) => {
    if (!ids.includes(id)) return;
    active = id;
    select.value = id;
    panels.forEach(panel => { panel.hidden = panel.dataset.pattern !== id; });
    document.querySelectorAll('.pattern-card').forEach(card => {
      card.classList.toggle('selected', card.dataset.selectPattern === id);
      if (card.dataset.selectPattern === id) card.setAttribute('aria-current', 'true');
      else card.removeAttribute('aria-current');
    });
    history.replaceState(null, '', '#pattern-' + id);
    if (navigate) document.querySelector('#inspect').scrollIntoView({behavior:'smooth', block:'start'});
  };
  const advance = delta => choose(ids[(ids.indexOf(active) + delta + ids.length) % ids.length]);
  document.querySelector('#previous-pattern').addEventListener('click', () => advance(-1));
  document.querySelector('#next-pattern').addEventListener('click', () => advance(1));
  select.addEventListener('change', () => choose(select.value));
  select.addEventListener('keydown', event => {
    if (['ArrowLeft','ArrowUp','ArrowRight','ArrowDown'].includes(event.key)) {
      event.preventDefault();
      advance(['ArrowLeft','ArrowUp'].includes(event.key) ? -1 : 1);
    }
  });
  document.querySelectorAll('[data-select-pattern]').forEach(link => link.addEventListener('click', event => {
    if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    choose(link.dataset.selectPattern, true);
  }));
  document.querySelectorAll('[data-open-source]').forEach(link => link.addEventListener('click', () => {
    document.querySelector('#source-' + link.dataset.openSource).open = true;
  }));
  document.querySelector('#show-context').addEventListener('change', event => document.body.classList.toggle('hide-context', !event.target.checked));
  document.querySelector('#show-directions').addEventListener('change', event => document.body.classList.toggle('hide-directions', !event.target.checked));
  document.querySelector('#show-ribbon').addEventListener('change', event => document.body.classList.toggle('broad-lines', event.target.checked));

  const isolate = (panel, wanted, label) => {
    const selected = wanted === '*' ? null : wanted.map(String);
    panel.querySelectorAll('.diagram .path-group').forEach(group => group.classList.toggle('dimmed', selected !== null && !selected.includes(group.dataset.path)));
    panel.querySelectorAll('.path-key').forEach(key => key.classList.toggle('selected', selected !== null && selected.length === 1 && selected[0] === key.dataset.pickPath));
    panel.querySelector('.selection-caption').textContent = label + (selected === null ? ' Select a group or a colored line to isolate its contribution.' : ' Other paths remain available in the same structure.');
  };
  document.querySelectorAll('.pattern-panel').forEach(panel => {
    const buttons = [...panel.querySelectorAll('.group-button')];
    buttons.forEach(button => button.addEventListener('click', () => {
      buttons.forEach(item => item.setAttribute('aria-pressed', String(item === button)));
      isolate(panel, button.dataset.paths === '*' ? '*' : JSON.parse(button.dataset.paths), button.textContent.trim() + '.');
    }));
    panel.querySelectorAll('[data-pick-path]').forEach(item => {
      const action = () => {
        buttons.forEach(button => button.setAttribute('aria-pressed', 'false'));
        isolate(panel, [item.dataset.pickPath], 'Path ' + item.dataset.pickPath + ' selected.');
      };
      item.addEventListener('click', action);
      if (item.tagName.toLowerCase() !== 'button') item.addEventListener('keydown', event => {
        if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); action(); }
      });
      item.addEventListener('pointerenter', () => {
        panel.querySelectorAll('.diagram .path-group').forEach(group => {
          group.classList.toggle('hovered', group.dataset.path === item.dataset.pickPath);
          group.classList.toggle('hover-dimmed', group.dataset.path !== item.dataset.pickPath);
        });
      });
      item.addEventListener('pointerleave', () => panel.querySelectorAll('.hovered,.hover-dimmed').forEach(group => group.classList.remove('hovered','hover-dimmed')));
    });
  });
  window.addEventListener('hashchange', () => {
    const id = location.hash.replace('#pattern-', '');
    if (ids.includes(id)) choose(id);
  });
  document.documentElement.classList.add('js');
  choose(active);
})();
