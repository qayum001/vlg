# dotnet test with .NET 10 SDK + xunit.v3
Updated: 2026-10-16
Related: [tooling](../project/tooling.md)

- Symptom: `dotnet test` fails with "Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK".
- Fix in repo: `/global.json` → `"test": { "runner": "Microsoft.Testing.Platform" }`, test csproj `UseMicrosoftTestingPlatformRunner=true`, no `Microsoft.NET.Test.Sdk` / `xunit.runner.visualstudio`.
- With MTP the project goes after `--project`: `dotnet test --project tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj`.
