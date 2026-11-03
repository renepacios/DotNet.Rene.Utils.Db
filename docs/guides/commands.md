# Commands

Commands live in `Rene.Utils.Db.Commands`. Send them using `ISender` or `IMediator`;
the generic handlers are resolved by MediatR.

The examples use the `Product` and `ProductDto` types from [getting started](getting-started.md),
an injected `ISender sender`, and a `CancellationToken cancellationToken`.

| Command | Result |
|---------|--------|
| `GetCommand<TViewModel>` | One view model |
| `GetAllCommand<TViewModel>` | `IEnumerable<TViewModel>` |
| `GetBySpecCommand<TViewModel, TEntity>` | `IReadOnlyList<TViewModel>` |
| `GetPaginatedCommand<TViewModel, TEntity>` | `IDbUtilsPaginatedData<TViewModel>` |
| `AddCommand<TViewModel>` | Created view model |
| `UpdateCommand<TViewModel>` | Updated view model |
| `DeleteCommand<TViewModel>` | `bool` |

## Read

```csharp
using Rene.Utils.Db.Commands;

var product = await sender.Send(new GetCommand<ProductDto>(id), cancellationToken);
var products = await sender.Send(new GetAllCommand<ProductDto>(), cancellationToken);
var filtered = await sender.Send(
    new GetBySpecCommand<ProductDto, Product>(specification), cancellationToken);
```

`Get` uses the EF Core primary-key lookup. `GetAll` and `GetBySpec` use no-tracking
queries. If the mapper supports projection, list queries project in the database;
otherwise entities are materialized and mapped in memory.

`GetBySpec` needs an actual specification. Use `GetAll` when no filter or query shape
is required. See [specifications](specifications.md) for a complete specification class.

## Write

```csharp
var created = await sender.Send(
    new AddCommand<ProductDto>(new ProductDto { Name = "Keyboard", Price = 49 }),
    cancellationToken);

var updated = await sender.Send(
    new UpdateCommand<ProductDto>(
        new ProductDto { Name = "Keyboard Pro", Price = 79 }, created.Id),
    cancellationToken);

var deleted = await sender.Send(new DeleteCommand<ProductDto>(created.Id), cancellationToken);
```

Adding requires a DTO-to-entity mapping and an entity-to-DTO mapping.
Updating requires mapping into an existing entity. The update handler retains
the primary-key value supplied in the command. Do not treat update as a partial-patch
operation unless your mapping explicitly implements that behavior.

Each write persists changes through the configured unit of work or the context.

## Errors and application responsibilities

Get, update and delete throw `KeyNotFoundException` when the identifier is not found.
Delete returns `true` after a successful deletion; absence is not reported as `false`.

Commands do not provide authorization or application validation automatically.
Use MediatR behaviors, validators or your API boundary for those policies.
Translate missing-entity exceptions to the appropriate HTTP response in the host application.

Pass cancellation tokens to `Send`. Handlers forward them to supported query and
save operations; in this version, the primary-key lookups in get, update and delete
do not forward the token to `FindAsync`.
