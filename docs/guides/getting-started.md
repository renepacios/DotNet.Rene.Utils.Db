# Getting started

Use a .NET 10 application with Entity Framework Core and MediatR.
The following example uses SQLite and manual mappings, so no additional mapping library is needed.

## Install

In an ASP.NET Core application:

```shell
dotnet add package Rene.Utils.Db
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.14
```

MediatR is a dependency of the library. Choose EF Core packages compatible with the
version used by the installed release of Rene.Utils.Db; this source tree uses EF Core 10.

## Define the entity, context and view model

```csharp
using Microsoft.EntityFrameworkCore;
using Rene.Utils.Db;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}

public class ProductDto : IWithGenericHandler<Product>
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

Keep the DTO public and constructible without arguments: registration scans exported
types and creates an instance to discover its entity type.

## Register services and mappings

In `Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlite("Data Source=shop.db"));
builder.Services.AddMediatR(options =>
    options.RegisterServicesFromAssemblyContaining<ProductDto>());

builder.AddDbUtils<ShopDbContext>(options => options.UseMapper(mapper => mapper
    .Map<Product, ProductDto>(entity => new ProductDto
    {
        Id = entity.Id,
        Name = entity.Name,
        Price = entity.Price
    })
    .Map<ProductDto, Product>(dto => new Product
    {
        Name = dto.Name,
        Price = dto.Price
    })
    .MapInto<ProductDto, Product>((dto, entity) =>
    {
        entity.Name = dto.Name;
        entity.Price = dto.Price;
    })), typeof(ProductDto).Assembly);

var app = builder.Build();

app.MapGet("/products", async (MediatR.ISender sender, CancellationToken token) =>
    await sender.Send(new Rene.Utils.Db.Commands.GetAllCommand<ProductDto>(), token));

app.Run();
```

Create the database schema with your usual EF Core migrations before serving requests.
Registration does not create tables or apply migrations automatically.

Each write command saves changes. For custom persistence behavior see
[configuration](configuration.md); for all request types see [commands](commands.md).
