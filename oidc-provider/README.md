# OIDC Provider Azure Function

This project hosts a minimal Azure Function endpoint that routes incoming HTTP requests to command handlers. New commands can be added inside the `switch` statement in `CommandProcessor.HandleAsync`.

## Prerequisites

- .NET SDK 8.0 or later
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) (v4)
- Azure Storage emulator (for example, Azurite) if you want to run the function locally with durable storage requirements

## Local development

1. Copy `local.settings.json.sample` to `local.settings.json` and review the settings.
2. Restore dependencies and build the solution:
   ```powershell
   dotnet restore oidc-provider/oidc-provider.sln
   dotnet build oidc-provider/oidc-provider.sln
   ```
3. Start the function host from the project root:
   ```powershell
   func start --csharp --dotnet-isolated --script-root oidc-provider/src/OidcProvider/bin/Debug/net8.0
   ```
4. Send requests to `http://localhost:7071/api/commands/{command}` or pass `?command=` as a query string parameter.

## Continuous integration

A dedicated GitHub Actions workflow (`.github/workflows/oidc-provider-ci.yml`) restores dependencies, builds the function, and runs the accompanying unit tests on each push or pull request that touches the `oidc-provider` directory.

Run the same checks locally with:
```powershell
dotnet test oidc-provider/oidc-provider.sln
```
