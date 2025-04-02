namespace Rene.Utils.Db.UnitTest.InMemoryTesting.Specifications
{
    using _Common;
    using Builder;
    using Models.Specifications;
    using Tests.Common;

    [Collection("InMemoryContextCollection")]
    public class SpecificationQueryTests(InMemoryContextFixture fixture)
    {
        [Fact]
        public void GetProductsWithNameContains_1()
        {
            // Arrange
            var context = fixture.Context;
            var spec = new ProductNameContainsSpec("1");

            // Act
            var results = SpecificationEvaluator<Product>.GetQuery(context.Products, spec).ToList();

            // Assert
            results.Should().HaveCount(1);
            results.First().Name.Should().Be("Product 1");
        }


        [Fact]
        public void GetProductsWithNameContains_Prod()
        {
            // Re-using the same fixture/context:
            var context = fixture.Context;
            var spec = new ProductNameContainsSpec("Prod");

            var results = SpecificationEvaluator<Product>.GetQuery(context.Products, spec).ToList();

            results.Should().HaveCount(3); // "Product 1", "Product 2" & "Product 3"
        }
    }
}