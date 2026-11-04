import { dotnet } from './_framework/dotnet.js';
globalThis.openEncodingNotes = file => window.open('/encoding/notes/' + encodeURIComponent(file), '_blank', 'noopener,noreferrer');
globalThis.initialEncodingLayer = () => {
  const layer = Number(new URLSearchParams(location.search).get('layer'));
  return Number.isInteger(layer) && layer > 0 && layer <= 33 ? layer : 1;
};
try {
const runtime = await dotnet.withDiagnosticTracing(false).create();
await runtime.runMain(runtime.getConfig().mainAssemblyName, []);
} catch (error) { document.getElementById('out').textContent = 'Unable to load the explorer. Please reload. ' + error.message; }
