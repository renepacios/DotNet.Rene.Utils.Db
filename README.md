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
	working on it!

### Prerequisites
- .NET 6.0 or later
- Entity Framework Core
- MediatR
- Optional: AutoMapper (`Rene.Utils.Db.AutoMapper`) or Mapster (`Rene.Utils.Db.Mapster`)

### Mapping configuration
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
### Installation
Clone the repository:

working on it!

##### Development
` `C# 

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
