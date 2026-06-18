# Gojo Blocks Browser

Browser-hosted Avalonia build for Gojo Blocks.

Live site:

```text
https://gojo.inklinai.ca
```

Production publish:

```powershell
dotnet publish .\Gojo.Blocks.Browser\Gojo.Blocks.Browser.csproj -c Release -o .\Gojo.Blocks.Browser\publish-browser -p:UseSharedCompilation=false

npx --yes @azure/static-web-apps-cli deploy `
  ".\Gojo.Blocks.Browser\publish-browser\wwwroot" `
  --swa-config-location ".\Gojo.Blocks.Browser\publish-browser\wwwroot" `
  --env production `
  --app-name gojo `
  --resource-group inkling `
  --no-use-keychain
```

The deploy command expects the Static Web Apps deployment token to be available
in the local environment, for example as `SWA_CLI_DEPLOYMENT_TOKEN`.
