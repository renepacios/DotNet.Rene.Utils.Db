# Rene.Utils.Db

## Overview
Rene.Utils.Db is a utility library for handling database operations using Entity Framework Core. It provides generic command handlers and unit of work implementations to simplify database interactions.

## Features
- Generic command handlers for CRUD operations
- Unit of Work pattern implementation
- Integration with MediatR
- Pluggable mapping: no dependency on any mapping library (AutoMapper, Mapster or manual mappings)
- Specification-based queries
- Projections in database queries based on the properties of the target ViewModel

## Getting Started

### Prerequisites
- .NET 10.0
- Entity Framework Core
- MediatR
- Optional: AutoMapper (`Rene.Utils.Db.AutoMapper`) or Mapster (`Rene.Utils.Db.Mapster`)

### 1. Install the packages
```bash
dotnet add package Rene.Utils.Db
# Choose a mapper integration (or use manual mappings, see below)
dotnet add package Rene.Utils.Db.AutoMapper
dotnet add package Rene.Utils.Db.Mapster
```

### 2. Define your entity and DbContext
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
}
```

### 3. Mark your view models
Implement `IWithGenericHandler<TEntity>` in each view model that needs the generic CRUD handlers. The library scans the given assemblies and registers the handlers for them.
```csharp
public class ProductViewModel : IWithGenericHandler<Product>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### 4. Register the services
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShopDbContext>(options => options.UseSqlite("Data Source=shop.db"));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// Registers the generic handlers for the view models of the given assemblies and configures the mapper
builder.AddDbUtils<ShopDbContext>(options => options.UseMapster(), typeof(Program).Assembly);
```
`AddDbUtils` is available for both `IHostApplicationBuilder` and `IServiceCollection`. By default the `DbContext` is used directly as unit of work. To use your own, call `options.UseOwnUnitOfWork<TUnitOfWork>()` or `AddDbUtils<TDbContext, TUnitOfWork>(...)`.

### 5. Send the commands
```csharp
[ApiController]
[Route("[controller]")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await sender.Send(new GetAllCommand<ProductViewModel>()));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
        => Ok(await sender.Send(new GetCommand<ProductViewModel>(id)));

    [HttpGet("paginated")]
    public async Task<IActionResult> GetPaginated(int page = 0, int size = 10)
        => Ok(await sender.Send(new GetPaginatedCommand<ProductViewModel, Product>(new DbUtilsAllSpecification<Product>(), size, page)));

    [HttpPost]
    public async Task<IActionResult> Post(ProductViewModel viewModel)
        => Ok(await sender.Send(new AddCommand<ProductViewModel>(viewModel)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, ProductViewModel viewModel)
        => Ok(await sender.Send(new UpdateCommand<ProductViewModel>(viewModel, id)));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
        => Ok(await sender.Send(new DeleteCommand<ProductViewModel>(id)));
}
```

| Command | Result |
| ------- | ------ |
| `GetAllCommand<TViewModel>` | `IEnumerable<TViewModel>` (projected in the database query) |
| `GetCommand<TViewModel>(id)` | `TViewModel` (throws `KeyNotFoundException` if not found) |
| `GetBySpecCommand<TViewModel, TEntity>(spec)` | `IReadOnlyList<TViewModel>` filtered by an `IDbUtilsSpecification<TEntity>` |
| `GetPaginatedCommand<TViewModel, TEntity>(spec, size, page)` | `IDbUtilsPaginatedData<TViewModel>` |
| `AddCommand<TViewModel>(viewModel)` | The created `TViewModel` |
| `UpdateCommand<TViewModel>(viewModel, id)` | The updated `TViewModel` |
| `DeleteCommand<TViewModel>(id)` | `bool` |

### Samples
- [Rene.Utils.Db.Sample.AutoMapper](samples/Rene.Utils.Db.Sample.AutoMapper): mapping with AutoMapper.
- [Rene.Utils.Db.Sample.Mapster](samples/Rene.Utils.Db.Sample.Mapster): mapping with Mapster.
- [Rene.Utils.Db.Sample.ManualMapper](samples/Rene.Utils.Db.Sample.ManualMapper): manual mappings without any mapping library.
## Mapping configuration
The library doesn't depend on any mapping library. The mapper used by the generic handlers (`IDbUtilsMapper`) is configured in `DbUtilsOptions`:

```csharp
// AutoMapper (package Rene.Utils.Db.AutoMapper). IMapper must be registered in the container.
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.AddDbUtils<MyDbContext>(options => options.UseAutoMapper(), typeof(Program).Assembly);

// Mapster (package Rene.Utils.Db.Mapster). Uses TypeAdapterConfig from the container or GlobalSettings.
builder.AddDbUtils<MyDbContext>(options => options.UseMapster(), typeof(Program).Assembly);

// Your own IDbUtilsMapper implementation (dependencies resolved from the container)
builder.AddDbUtils<MyDbContext>(options => options.UseMapper<MyMapper>(), typeof(Program).Assembly);

// Factory based on the dependency injection container
builder.AddDbUtils<MyDbContext>(options => options.UseMapper(sp => new MyMapper(sp.GetRequiredService<IFoo>())), typeof(Program).Assembly);

// Manual mappings, without any mapping library
builder.AddDbUtils<MyDbContext>(options => options.UseMapper(m => m
    // Expression: used for in-memory mapping and translated to SQL in projections (GetAll, GetBySpec, GetPaginated)
    .Map<Product, ProductDto>(p => new ProductDto { Id = p.Id, Name = p.Name })
    .Map<ProductDto, Product>(dto => new Product { Name = dto.Name })
    // Function with access to the container: in-memory only (entities are materialized before mapping)
    .Map<Product, ProductDetailsDto>((sp, p) => new ProductDetailsDto { Id = p.Id, Price = sp.GetRequiredService<IPriceService>().Get(p) })
    // Update an existing entity (UpdateCommand)
    .MapInto<ProductDto, Product>((dto, entity) => entity.Name = dto.Name)), typeof(Program).Assembly);
```

To integrate another library, implement `IDbUtilsMapper` and expose an extension method over `DbUtilsOptions` that calls `options.UseMapper(...)`.

## Contributing
Contributions are welcome! Please open an issue or submit a pull request.

## Code of conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.


 ## Acknowledgements

* [XUnit](https://xunit.github.io/)
* [Fluent Assertions](http://www.fluentassertions.com/)
* [Moq](https://github.com/devlooped/moq)

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact
For any questions or suggestions, please contact [contacto@webrene.es](mailto:contacto@webrene.es).
