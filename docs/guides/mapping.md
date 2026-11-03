# Mapping

All handlers depend on `IDbUtilsMapper`. Select one implementation when calling
`AddDbUtils`. AutoMapper and Mapster are optional packages.

## Manual expressions and update mappings

```csharp
builder.AddDbUtils<ShopDbContext>(options => options.UseMapper(mapper => mapper
    .Map<Product, ProductDto>(entity => new ProductDto
    {
        Id = entity.Id, Name = entity.Name, Price = entity.Price
    })
    .Map<ProductDto, Product>(dto => new Product
    {
        Name = dto.Name, Price = dto.Price
    })
    .MapInto<ProductDto, Product>((dto, entity) =>
    {
        entity.Name = dto.Name;
        entity.Price = dto.Price;
    })), typeof(ProductDto).Assembly);
```

An expression mapping is compiled for in-memory mapping and retained for query
projection. Keep it translatable by your EF Core provider.
`MapInto` changes an existing instance and is used by update commands.

## Functions with dependency injection

```csharp
builder.Services.AddSingleton(TimeProvider.System);

builder.AddDbUtils<ShopDbContext>(options => options.UseMapper(mapper => mapper
    .Map<Product, ProductDto>((services, entity) =>
    {
        var utcNow = services.GetRequiredService<TimeProvider>().GetUtcNow();
        services.GetRequiredService<ILogger<ProductDto>>()
            .LogDebug("Mapping product {ProductId} at {UtcNow}", entity.Id, utcNow);
        return new ProductDto { Id = entity.Id, Name = entity.Name, Price = entity.Price };
    })), typeof(ProductDto).Assembly);
```

This two-argument function overload is in-memory only. Unless a projection expression
is also registered for the same pair, list queries materialize entities before mapping.
The snippet only registers the read mapping: add DTO-to-entity and `MapInto` mappings
when enabling writes.

The manual mapper requires the relevant mappings to be registered explicitly.
Missing mapping definitions result in `InvalidOperationException`.

## AutoMapper

Install `Rene.Utils.Db.AutoMapper` and configure the required mapping profiles:

```csharp
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.AddDbUtils<ShopDbContext>(
    options => options.UseAutoMapper(),
    typeof(ProductDto).Assembly);
```

`UseAutoMapper` resolves `AutoMapper.IMapper` from dependency injection. You can
also supply a mapper factory. Configure entity-to-DTO, DTO-to-entity and update
behavior in your profiles.

## Mapster

Install `Rene.Utils.Db.Mapster`:

```csharp
var config = new Mapster.TypeAdapterConfig();
config.NewConfig<Product, ProductDto>();
config.NewConfig<ProductDto, Product>();

builder.AddDbUtils<ShopDbContext>(
    options => options.UseMapster(config),
    typeof(ProductDto).Assembly);
```

`UseMapster()` without arguments resolves a registered `TypeAdapterConfig`,
falling back to `TypeAdapterConfig.GlobalSettings`. Overloads accept an explicit
configuration or a factory.

## Custom mapper

Implement `IDbUtilsMapper` and select it with `UseMapper<MyMapper>()` or
`UseMapper(services => new MyMapper(...))`.
The contract separates mapping, mapping into an existing object, projection and
the `CanProject` capability check. Return `false` from `CanProject` when a mapping
cannot be translated; handlers will use in-memory mapping for lists instead.

Projection failures from the provider are not silently retried as in-memory queries.
