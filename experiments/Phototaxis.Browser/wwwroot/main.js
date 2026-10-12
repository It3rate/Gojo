import { dotnet } from './_framework/dotnet.js';
try {
  const runtime = await dotnet.withDiagnosticTracing(false).withApplicationArgumentsFromQuery().create();
  const config = runtime.getConfig();
  await runtime.runMain(config.mainAssemblyName, [globalThis.location.href]);
} catch (error) {
  document.getElementById('loading-status').textContent = String(error);
  console.error(error);
}
