# Specifications

`IDbUtilsSpecification<TEntity>` describes criteria, related data, ordering and paging
independently of a view model. `GetBySpecCommand` and `GetPaginatedCommand` apply it
to the entity query before mapping.

## Implement a specification

This class uses `Product` from [getting started](getting-started.md):

```csharp
using System.Linq.Expressions;
using Rene.Utils.Db;

public sealed class ProductNameSpec(string term) : IDbUtilsSpecification<Product>
{
    public Expression<Func<Product, bool>> Criteria => product => product.Name.Contains(term);
    public ICollection<Expression<Func<Product, bool>>> Criterias { get; } = [];
    public List<Expression<Func<Product, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public List<KeyValuePair<bool, Expression<Func<Product, object>>>> OrderBy { get; } =
        [new(true, product => product.Id)];
    public Expression<Func<Product, object>> GroupBy => null!;
    public bool IgnoreQueryFilters => false;
    public bool AsNoTracking => true;
    public int Take => 0;
    public int Skip => 0;
    public bool IsPagingEnabled => false;
}
```

`Criteria` and each entry in `Criterias` are applied as filters.
For ordering pairs, `true` means ascending and `false` descending.
Include expressions and include paths use EF Core eager loading; they must refer
to navigation properties supported by the entity model.

`DbUtilsAllSpecification<TEntity>` supplies an unrestricted specification, optionally
with ordering, when a paginated command needs no filtering.

## Combine predicates

```csharp
using Rene.Utils.Db.CompositeSpecifications;

var first = new ProductNameSpec("Keyboard");
var second = new ProductNameSpec("Mouse");

var either = first.Or(second);
var both = first.And(second);
var excluded = first.ToInvert();
var sorted = either.OrderBy(new[] { "Name,asc", "Id,desc" });
```

`And`, `Or` and `ToInvert` compose predicate expressions. Their inputs must have
criteria; they are not a way to combine empty `DbUtilsAllSpecification` instances.
`OrderBy` also expects criteria in its input in this version.

> [!IMPORTANT]
> In this version, logical composition does not merge includes, paging, grouping or
> EF query flags from the operands. Configure the final query shape deliberately.
> `OrderBy` preserves existing ordering for composite inputs and appends the requested order.

`AddIncludeSpecification` can add a navigation expression to a composite result.
It mutates the composite specification and throws for specifications that do not
implement the composite contract. Add includes after predicate composition;
later composition does not preserve them.

## EF Core behavior

The evaluator applies `IgnoreQueryFilters` and `AsNoTracking` from the specification.
Ignoring global filters affects the whole query and should only be enabled when
your application's authorization and data-isolation rules permit it.

The `AsSplitQuery` contract and fluent query-option helpers are not enabled in the
source version documented here.

## Paging and query shape

For `GetBySpecCommand`, setting `IsPagingEnabled` applies `Skip` and `Take`.
For `GetPaginatedCommand`, leave it `false`: that command rejects specifications
with paging already enabled. See [pagination](pagination.md).

`GroupBy` is flattened back into the entity sequence by the evaluator; it does not
produce aggregate DTOs. Implement custom queries for aggregate reports.
