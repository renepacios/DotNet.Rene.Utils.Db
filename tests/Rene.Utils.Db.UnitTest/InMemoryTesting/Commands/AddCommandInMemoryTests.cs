namespace Rene.Utils.Db.UnitTest.InMemoryTesting.Commands
{
    using AutoMapper;
    using Builder;
    using Db.Commands;
    using Microsoft.EntityFrameworkCore;
    using Tests.Common;

    public class AddCommandInMemoryTests
    {
        [Fact]
        public async Task AddCommand_AddsEntity_ToInMemoryDatabase()
        {
            // Arrange
            // 1. Create InMemory EF context
            var options = new DbContextOptionsBuilder<TestingDbContext>()
                .UseInMemoryDatabase($"AddCommandTest_{Guid.NewGuid()}")
                .Options;

            await using var context = new TestingDbContext(options);

            // 2. Create a real (or mock) AutoMapper
            // If you have simple mappings, you can use the real mapping config. 
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ProductDescriptionDto, ProductDescription>();
                cfg.CreateMap<ProductDescription, ProductDescriptionDto>();
            });

            var mapper = mapperConfig.CreateMapper();

            var identifier = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;
            var vm = new ProductDescriptionDto
            {
                ProductDescriptionId = 123,
                Description = "Foo",
                Rowguid = identifier,
                ModifiedDate = timestamp
            };

            var addCommand = new AddCommand<ProductDescriptionDto>(vm);

            // 3. Instantiate the command handler (UoW can be null or a fake)
            var handler = new GenericCommandHandler<
                ProductDescriptionDto,
                ProductDescription,
                TestingDbContext,
                IDbUtilsUnitOfWork>(mapper, context, null);

            // Act
            var result = await handler.Handle(addCommand, CancellationToken.None);

            // Assert
            // 1. Check the entity is in the DB
            context.ProductDescriptions.Count().Should().Be(1);

            var stored = context.ProductDescriptions.First();
            stored.ProductDescriptionId.Should().Be(123);
            stored.Rowguid.Should().Be(identifier);
            stored.Description.Should().Be("Foo");
            result.ModifiedDate.Should().Be(timestamp);

            // 2. Check the returned ViewModel is correct 
            result.ProductDescriptionId.Should().Be(123);
            result.Rowguid.Should().Be(identifier);
            result.Description.Should().Be("Foo");
            result.ModifiedDate.Should().Be(timestamp);
        }
    }
}