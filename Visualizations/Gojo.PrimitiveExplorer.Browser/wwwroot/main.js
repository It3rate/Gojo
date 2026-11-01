import { dotnet } from './_framework/dotnet.js';
globalThis.openEncodingNotes = file => window.open('/encoding/notes/' + encodeURIComponent(file), '_blank', 'noopener,noreferrer');
try {
const runtime = await dotnet.withDiagnosticTracing(false).create();
await runtime.runMain(runtime.getConfig().mainAssemblyName, []);
} catch (error) { document.getElementById('out').textContent = 'Unable to load the explorer. Please reload. ' + error.message; }
