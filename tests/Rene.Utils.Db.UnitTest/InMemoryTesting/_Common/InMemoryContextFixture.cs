namespace Rene.Utils.Db.UnitTest.InMemoryTesting._Common
{
    using Microsoft.EntityFrameworkCore;
    using Tests.Common;

    public class InMemoryContextFixture : IAsyncLifetime
    {
        private readonly DbContextOptions<TestingDbContext> _options = new DbContextOptionsBuilder<TestingDbContext>()
            .UseInMemoryDatabase($"InMemoryContext_{Guid.NewGuid()}")
            .Options;


        private TestingDbContext? _context;
        public TestingDbContext Context => _context ??= new TestingDbContext(_options);

        public async Task InitializeAsync()
        {
            await Context.Database.EnsureCreatedAsync();
            await SeedData();
        }

        public async Task DisposeAsync()
        {
            await Context.Database.EnsureDeletedAsync();
        }

        private async Task SeedData()
        {
            var timestamp = DateTime.UtcNow;

            var productCategories = new List<ProductCategory>
            {
                new() { ProductCategoryId = 1, Name = "Category 1", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductCategoryId = 2, Name = "Category 2", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductCategoryId = 3, Name = "Category 3", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp }
            };

            var productDescriptions = new List<ProductDescription>
            {
                new() { ProductDescriptionId = 1, Description = "Product 1", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductDescriptionId = 2, Description = "Product 2", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductDescriptionId = 3, Description = "Product 3", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp }
            };

            var productModels = new List<ProductModel>
            {
                new() { ProductModelId = 1, Name = "Product Model 1", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductModelId = 2, Name = "Product Model 2", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductModelId = 3, Name = "Product Model 3", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp }
            };

            var productModelDescriptions = new List<ProductModelProductDescription>
            {
                new() { ProductModelId = 1, ProductDescriptionId = 1, Culture = "en", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductModelId = 2, ProductDescriptionId = 2, Culture = "en", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductModelId = 3, ProductDescriptionId = 3, Culture = "en", Rowguid = Guid.NewGuid(), ModifiedDate = timestamp }
            };

            var products = new List<Product>
            {
                new() { ProductId = 1, Name = "Product 1", ProductNumber = "P1", ProductCategoryId = 1, ProductModelId = 1, Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductId = 2, Name = "Product 2", ProductNumber = "P2", ProductCategoryId = 2, ProductModelId = 2, Rowguid = Guid.NewGuid(), ModifiedDate = timestamp },
                new() { ProductId = 3, Name = "Product 3", ProductNumber = "P3", ProductCategoryId = 3, ProductModelId = 3, Rowguid = Guid.NewGuid(), ModifiedDate = timestamp }
            };

            await Context.ProductModels.AddRangeAsync(productModels);
            await Context.ProductDescriptions.AddRangeAsync(productDescriptions);
            await Context.ProductModelProductDescriptions.AddRangeAsync(productModelDescriptions);
            await Context.Products.AddRangeAsync(products);

            await Context.SaveChangesAsync();
        }
    }
}