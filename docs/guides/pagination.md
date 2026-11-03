# Pagination

`GetPaginatedCommand<TViewModel, TEntity>` returns `IDbUtilsPaginatedData<TViewModel>`.
Pages are zero-based.

```csharp
using Rene.Utils.Db;
using Rene.Utils.Db.Commands;

var page = await sender.Send(
    new GetPaginatedCommand<ProductDto, Product>(size: 20, pageNumber: 0),
    cancellationToken);
```

## Filtered pages

```csharp
var specification = new ProductNameSpec("Keyboard");

var page = await sender.Send(
    new GetPaginatedCommand<ProductDto, Product>(specification, size: 20, pageNumber: 1),
    cancellationToken);
```

Or pass `PaginationOptions`:

```csharp
var options = new PaginationOptions { Size = 20, Page = 1 };
var page = await sender.Send(
    new GetPaginatedCommand<ProductDto, Product>(specification, options),
    cancellationToken);
```

`PaginationOptions.AsDefault` creates options with size 10 and page 0.
The options constructor requires a specification argument; use the size/page
constructor when no specification is needed.

## Result fields

| Field | Meaning |
|-------|---------|
| `Content` | View models in the returned page |
| `Number` | Requested zero-based page |
| `Size` | Requested page size |
| `NumberOfElements` | Actual count in `Content` |
| `TotalElements` | Count of all matching entities before command-level paging |
| `TotalPages` | Total matching pages |
| `First` | Whether the requested page is page 0 |
| `Last` | Whether no additional row was found beyond this page |
| `Empty` | Whether the returned page contains no rows |

The handler counts the matching query, then fetches at most `size + 1` rows.
The extra row determines `Last` and is not included in `Content`.
Count and data are separate queries, so concurrent writes can change the data
between them.

## Input and ordering rules

Validate `size > 0` and `pageNumber >= 0` at your API boundary.
The result constructor rejects nonpositive size; the command does not provide
complete pagination validation.

Use deterministic ordering, including a unique tie-breaker such as the entity ID,
for reliable paging. Without ordering, a provider does not guarantee stable results.
You can provide ordering through a specification, including
`DbUtilsAllSpecification<TEntity>`'s ordering constructor.

Do not pass a specification with `IsPagingEnabled = true`: the handler throws
`ArgumentException` rather than applying paging twice.
