# Configuration and registration

`AddDbUtils` is available on both `IServiceCollection` and `IHostApplicationBuilder`
in the `Microsoft.Extensions.DependencyInjection` namespace.

```csharp
builder.Services.AddDbUtils<ShopDbContext>(
    options => options.UseMapster(),
    typeof(ProductDto).Assembly);

// Equivalent host-builder entry point; choose one registration approach.
builder.AddDbUtils<ShopDbContext>(
    options => options.UseMapster(),
    typeof(ProductDto).Assembly);
```

Register the EF Core context and MediatR separately. Pass the assemblies containing
your public view models explicitly; each view model must implement
`IWithGenericHandler<TEntity>` and have a public parameterless constructor.
The library registers closed, scoped handlers for the discovered pairs.

## Mapper lifetime

A configured mapper factory is invoked once per dependency-injection scope.
With no mapper configuration, register `IDbUtilsMapper` yourself:

```csharp
builder.Services.AddScoped<IDbUtilsMapper, MyMapper>();
builder.AddDbUtils<ShopDbContext>(typeof(ProductDto).Assembly);
```

Alternatively use `UseMapper<MyMapper>()` or a factory:

```csharp
builder.AddDbUtils<ShopDbContext>(
    options => options.UseMapper<MyMapper>(),
    typeof(ProductDto).Assembly);
```

`MyMapper` is an application implementation of `IDbUtilsMapper`.
See [mapping](mapping.md) for the manual builder and optional integrations.

## Default persistence

Without an explicit unit of work, write handlers save through the registered
`DbContext`. The fallback unit of work is internal infrastructure, not an
application type to instantiate.

## Custom unit of work

Implement `IDbUtilsUnitOfWork` and register the concrete type used by the handlers:

```csharp
builder.Services.AddScoped<ShopUnitOfWork>();
builder.AddDbUtils<ShopDbContext>(options =>
{
    options.UseMapster();
    options.UseOwnUnitOfWork<ShopUnitOfWork>();
}, typeof(ProductDto).Assembly);
```

Or select the same type through the generic overload:

```csharp
builder.Services.AddScoped<ShopUnitOfWork>();
builder.AddDbUtils<ShopDbContext, ShopUnitOfWork>(
    options => options.UseMapster(),
    typeof(ProductDto).Assembly);
```

These are alternatives, not registrations to combine. `ShopUnitOfWork` must implement
all three `IDbUtilsUnitOfWork` save methods. The handlers invoke its
`SaveChangesAsync(CancellationToken)` after each add, update or delete.
The library does not create a transaction spanning multiple commands automatically.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| No handler for a command | DTO is public, implements `IWithGenericHandler<TEntity>`, and its assembly was passed to `AddDbUtils` |
| Cannot create a DTO during registration | It needs a public parameterless constructor |
| Cannot resolve a mapper | Configure a mapper or register `IDbUtilsMapper` |
| Cannot resolve a custom unit of work | Register its concrete type, not only the interface |
| Query translation fails | Projection and specification expressions must be supported by your EF Core provider |
