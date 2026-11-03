# Command authorization

Authorization is opt-in. Existing commands, handlers and mediator extensions are unchanged.
Definitions associate requirements with a command; requirement handlers implement reusable checks.
The contracts live in `Rene.Utils.Db.Authorization` in the abstractions package.
The behavior and registration are included in the core package; no third-party authorization
package or ASP.NET authentication scheme is installed.

## Register by convention

```csharp
using Microsoft.Extensions.DependencyInjection;
using Rene.Utils.Db.Authorization;

builder.Services.AddDbUtilsAuthorization(typeof(Program).Assembly);
```

Pass one or more assemblies containing authorization definitions and requirement handlers.
The scanner registers closed, nonabstract classes with scoped lifetimes. Repeated registration
does not duplicate the behavior or identical definition/handler registrations.
Register authorization before caching or any other behavior that may return without invoking
the rest of the pipeline. Continue registering MediatR and `AddDbUtils` as usual.

> [!IMPORTANT]
> Commands without a registered definition are allowed, including generic CRUD commands.
> This preserves existing behavior but is not a deny-by-default authorization system.
> Review all operations exposed by your application. Calling handlers directly bypasses MediatR behaviors.

## Associate operations with requirements

The following illustrative types use the `ProductDto` from [getting started](getting-started.md):

```csharp
using Rene.Utils.Db.Authorization;
using Rene.Utils.Db.Commands;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class ProductAuthorization :
    IAuthorizationAdd<ProductDto>, IAuthorizationDelete<ProductDto>
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements(AddCommand<ProductDto> command)
        => [new PermissionRequirement("products.create")];

    public IEnumerable<IAuthorizationRequirement> GetRequirements(DeleteCommand<ProductDto> command)
        => [new PermissionRequirement("products.delete")];
}
```

One class can implement several operation interfaces. A class that implements only an
authorization definition does not need to implement `IWithGenericHandler`; that marker
remains on the DTO for registering CRUD handlers.

| Interface | Command |
|-----------|---------|
| `IAuthorizationAdd<TViewModel>` | `AddCommand<TViewModel>` |
| `IAuthorizationUpdate<TViewModel>` | `UpdateCommand<TViewModel>` |
| `IAuthorizationDelete<TViewModel>` | `DeleteCommand<TViewModel>` |
| `IAuthorizationGet<TViewModel>` | `GetCommand<TViewModel>` |
| `IAuthorizationGetAll<TViewModel>` | `GetAllCommand<TViewModel>` |
| `IAuthorizationGetBySpec<TViewModel, TModel>` | `GetBySpecCommand<TViewModel, TModel>` |
| `IAuthorizationGetPaginated<TViewModel, TModel>` | `GetPaginatedCommand<TViewModel, TModel>` with any constructor |
| `ICommandAuthorization<TCommand>` | Any concrete MediatR request |

These are specializations of `ICommandAuthorization<TCommand>`; they share the same pipeline.
Definitions are matched to the closed request type, not automatically to its base classes.
Get-by-ID, get-all, specification and paginated reads are separate authorization registrations.

## Validate once, reuse everywhere

```csharp
public interface ICurrentPermissions
{
    Task<bool> HasPermission(string permission, CancellationToken cancellationToken);
}

public sealed class PermissionRequirementHandler(ICurrentPermissions permissions)
    : IRequirementHandler<PermissionRequirement>
{
    public async Task<AuthorizationResult> Handle(
        PermissionRequirement requirement, CancellationToken cancellationToken)
    {
        return await permissions.HasPermission(requirement.Permission, cancellationToken)
            ? AuthorizationResult.Allow()
            : AuthorizationResult.Deny("The required permission is missing.");
    }
}
```

`ICurrentPermissions` is an application service: implement and register it using your trusted
identity/permission source. Never trust a user identifier or permission supplied in the command
as evidence that the caller has that identity or permission.
The library deliberately does not choose JWT, cookies, roles, claims or an HTTP user source.
Dependencies of requirement handlers are resolved in the same scope as the command.

Exactly one handler must be registered for each concrete requirement type returned by a
definition. Missing handlers, duplicate different handlers and null results are configuration
errors and throw `InvalidOperationException`. Exceptions from handlers propagate.

## Command-specific and typed requirements

Definitions can inspect an update/delete/get ID or the view model in an add/update command:

```csharp
public sealed record ProductAccessRequirement(object ProductId) : IAuthorizationRequirement;

public sealed class DeleteProductAuthorization : IAuthorizationDelete<ProductDto>
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements(DeleteCommand<ProductDto> command)
        => [new PermissionRequirement("products.delete"), new ProductAccessRequirement(command.Id)];
}
```

Provide an `IRequirementHandler<ProductAccessRequirement>` that checks the persisted entity
against the current user's scope. It can reuse the same database/context services through DI.
If both delete definitions above are registered, both apply; definitions are not overrides.

Requirements can also be generic, for example `EntityAccessRequirement<Product>`.
A concrete handler implementing `IRequirementHandler<EntityAccessRequirement<Product>>`
is discovered automatically. Open generic classes are not expanded by scanning; register a
closed generic handler explicitly:

```csharp
// Assuming the application defines these generic types and Product:
services.AddAuthorizationRequirementHandler<
    EntityAccessRequirement<Product>, EntityAccessRequirementHandler<Product>>();
```

For generic authorization definitions, register the closed implementation explicitly with
`AddScoped<ICommandAuthorization<DeleteCommand<ProductDto>>, YourClosedDefinition>()`.
Call `AddDbUtilsAuthorization` as well to install the behavior.

## Custom commands

```csharp
public sealed record PublishProductCommand(int ProductId) : MediatR.IRequest<bool>;

public sealed class PublishProductAuthorization : ICommandAuthorization<PublishProductCommand>
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements(PublishProductCommand command)
        => [new PermissionRequirement("products.publish"), new ProductAccessRequirement(command.ProductId)];
}
```

The custom command's business handler is registered through MediatR as usual.
No authorization-specific base class or interface is required on that handler.

## Evaluation and errors

All definitions and requirements are combined with AND and evaluated sequentially before
the business handler. The first denial stops processing and throws
`CommandAuthorizationException`; no successful-looking default response is returned.
Cancellation is forwarded to handlers and prevents execution of the business handler.

Definitions must return a non-null collection containing at least one non-null requirement.
Returning an empty collection is an error, not an anonymous-access declaration.
Do not retain mutable per-request state inside a definition; return new requirement instances.
There is no automatic requirement deduplication or OR policy.

Denials are logged with the command and requirement type, not request payloads.
The exception exposes `CommandType`, `RequirementType` and `FailureReason` for trusted host
handling. Do not expose internal reasons or resource details directly to clients.
In an HTTP host, translate to 401 if unauthenticated or 403 if authenticated; the library itself
does not know whether authentication has occurred.

## Boundaries

Authorization grants access to the operation, not an automatic row filter. Tenant and ownership
filters must be applied before counts and pagination. Control `IgnoreQueryFilters` explicitly.
A database authorization check does not eliminate races between the check and the write;
use transactions/concurrency controls for invariants that must remain true at commit.

## Executable sample

The ManualMapper sample includes one permission handler shared by add/update/delete definitions
and a concrete `GetForecastCountCommand` protected by another definition.

```powershell
dotnet run --project samples\Rene.Utils.Db.Sample.ManualMapper -- --Authorization:Enabled=true
```

This opts in without changing the other samples or the default demo behavior.
Both `/WeatherForecast` (explicit commands) and `/WeatherForecastExtensions` use the same rules.
The custom request is exposed at `/WeatherForecastAuthorization/count`.
Generic reads have no definitions and remain public. Writes require `forecast.write`; the custom
count requires `forecast.read`, represented by `permission` claims on an authenticated principal.

No authentication provider is configured in the demo. When enabled without integrating a real
authentication provider, protected requests intentionally return 401. Configure your host's
authentication and middleware to populate `HttpContext.User`; do not simulate permissions
with untrusted headers. The sample translates denials to generic HTTP problem responses.
