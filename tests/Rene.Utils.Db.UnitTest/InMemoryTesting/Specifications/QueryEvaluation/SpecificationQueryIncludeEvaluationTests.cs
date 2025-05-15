// ReSharper disable once CheckNamespace
namespace Rene.Utils.Db.UnitTest.Specifications
{
    using Builder;
    using Models.Specifications;
    using Tests.Common;

    [Collection("InMemoryContextCollection")]
    public class SpecificationQueryIncludeEvaluationTests(InMemoryContextFixture fixture)
    {
        [Fact]
        public void GetProducts_Include_ProductModel()
        {
            // Arrange
            var context = fixture.Context;
            var spec = new ProductIncludeModelSpec();

            // Act
            var results = SpecificationEvaluator<Product>.GetQuery(context.Products, spec).ToList();

            // Assert
            var expectedCount = fixture.Context.Products.Count();

            results.Should().HaveCount(expectedCount);
            results.Should().AllSatisfy(x => x.ProductModel.Should().NotBeNull());
        }
    }
}