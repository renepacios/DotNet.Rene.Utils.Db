# Samples and tests

## Run a sample

The repository includes three SQLite-backed ASP.NET Core examples:

| Project | Mapping approach |
|---------|------------------|
| `Rene.Utils.Db.Sample.AutoMapper` | AutoMapper profiles |
| `Rene.Utils.Db.Sample.Mapster` | Mapster configuration |
| `Rene.Utils.Db.Sample.ManualMapper` | Manual expressions, DI-aware functions and update mappings |

From the repository root, for example:

```shell
dotnet run --project samples/Rene.Utils.Db.Sample.ManualMapper
```

Use the URL printed by the application. In Development, Swagger UI is available
at `/swagger`. The `WeatherForecast` controller shows list, primary-key lookup,
pagination, add, update and delete through generic commands.

Each sample initializes its own SQLite database. Run these projects as development
examples, not as production deployment templates.

Each sample registers a `WeatherForecastEventHandler` through MediatR assembly
scanning to demonstrate [CRUD events](events.md) after Add, Update and Delete.
Both explicit-command and mediator-extension controllers publish the same events.

All samples also show [command logging](command-logging.md): AutoMapper configures
Before and Completed lambdas, Mapster configures only Completed, and ManualMapper
resolves an application-owned scoped event writer. Both controller styles use
the same pipeline.
The ManualMapper sample also demonstrates optional [command authorization](authorization.md):
one requirement handler shared by generic write commands and a concrete count query.
Enable it with `--Authorization:Enabled=true`; it is disabled by default.
The host must supply a real authenticated principal to grant protected operations.

The source is available under
[samples](https://github.com/renepd/DotNet.Rene.Utils.Db/tree/develop/samples).

## Unit tests

From the repository root:

```shell
dotnet test tests/Rene.Utils.Db.UnitTest/Rene.Utils.Db.UnitTest.csproj
```

The unit test project uses xUnit, FluentAssertions, Moq and EF Core InMemory.
It covers command handlers, mappings, registration, specification evaluation and
the optional authorization pipeline.
InMemory is useful for behavioral tests but does not validate SQL translation.

## SQL Server integration tests

Integration tests start SQL Server with Testcontainers. They require a running
Docker environment and the AdventureWorksLT2022 backup, which is not committed.

Download the backup to `resources/backups/AdventureWorksLT2022.bak`.
For PowerShell:

```powershell
New-Item -ItemType Directory -Force resources\backups
Invoke-WebRequest `
  https://github.com/microsoft/sql-server-samples/releases/download/adventureworks/AdventureWorksLT2022.bak `
  -OutFile resources\backups\AdventureWorksLT2022.bak
dotnet test tests\Rene.Utils.Db.IntegrationTest\Rene.Utils.Db.IntegrationTest.csproj
```

For all tests, after preparing Docker and the backup:

```shell
dotnet test
```

The CI workflow downloads the backup before running the test suite.
