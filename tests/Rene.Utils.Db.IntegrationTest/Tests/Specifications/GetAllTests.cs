// ReSharper disable once CheckNamespace

namespace Rene.Utils.Db.IntegrationTest
{
    using ExtensionHelpers;
    using FluentAssertions;
    using Tests.Common.Dto;

    [Collection("TestContainerCollection")]
    public class GetAllTests(MsSqlContainerFixture fixture)
    {
        [Fact]
        public async Task GetAll_Should_Work_As_Expected()
        {
            // Arrange
            var mediator = fixture.Mediator ?? throw new InvalidOperationException("Mediator is null");

            // Act
            var response = await mediator.GetAll<ProductDto>(CancellationToken.None);

            // Assert
            response.Should().NotBeEmpty();
            response.Should().AllBeOfType<ProductDto>();
            response.Count().Should().Be(fixture.Context.Products.Count());
        }
    }
}