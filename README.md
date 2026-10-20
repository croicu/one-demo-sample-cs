# one-demo-sample

Sample C# service code for the one-demo scanner: a few typical components (an HTTP client, a background monitor, a scheduled job) that call other services and retry on failure.

---

## Build

```bash
dotnet build
```

## Run

```bash
dotnet run --project src/OneDemoSample
```

## Lint

```bash
dotnet format
```

## Test

```bash
dotnet test
dotnet test --filter "FullyQualifiedName=Croicu.One.Demo.Sample.OneDemoSample.Tests.Unit.ProgramTests.Main_RunsClean"   # single test
```
