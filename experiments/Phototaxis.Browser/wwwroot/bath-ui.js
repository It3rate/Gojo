let activeGraphUrl;
let stopRequested = false;
export async function jevStatus() {
  const result = await fetch('/jev/status');
  return result.ok ? await result.text() : '{"available":false}';
}
export async function askJev(request) {
  const result = await fetch('/jev', {method:'POST', headers:{'Content-Type':'application/json'}, body:request});
  const text = await result.text();
  if (!result.ok) throw new Error(`Local Jev bridge: ${result.status}`);
  return text;
}
document.getElementById('stop-training').addEventListener('click', () => { stopRequested = true; });
export function takeStopRequest() {
  const requested = stopRequested; stopRequested = false; return requested;
}
export function downloadGraph(name, base64) {
  if (activeGraphUrl) URL.revokeObjectURL(activeGraphUrl);
  const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
  activeGraphUrl = URL.createObjectURL(new Blob([bytes], {type:'application/octet-stream'}));
  const link = document.getElementById('native-graph-download');
  link.href = activeGraphUrl; link.download = name;
  document.getElementById('graph-name').textContent = name;
  document.getElementById('graph-export').hidden = false;
  link.focus();
}
export function setStatus(status) {
  document.querySelector('.loading')?.remove();
  document.getElementById('simulation-status').textContent = status;
  const state = JSON.parse(status);
  document.getElementById('training-overlay').hidden = !state.training;
  if (state.training) {
    document.getElementById('training-progress').textContent = `${Math.ceil(state.trainingRemaining)} simulated seconds remaining`;
    document.getElementById('training-speed').textContent = `${state.actualRate.toFixed(1)}× actual speed · ${state.population} alive · ${state.rewrites} circuit changes · ${(state.resourceYield * 100).toFixed(0)}% supply`;
  }
}
