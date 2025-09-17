// ReSharper disable once CheckNamespace

namespace Rene.Utils.Db.UnitTest.Specifications
{
    using Builder;
    using Models;
    using Models.Specifications;

    public class SpecificationEvaluatorTests
    {
        [Fact]
        public void SpecificationFiltersCorrectly_WithoutEF()
        {
            // Arrange
            var spec = new NameStartsWithSpec("A");

            var data = new List<Sample>
            {
                new() { Id = 1, Name = "Alpha" },
                new() { Id = 2, Name = "Beta" }
            };

            // Act
            var results = SpecificationEvaluator<Sample>.Evaluate(data, spec).ToList();

            // Assert
            results.Should().HaveCount(1);
            results.First().Name.Should().Be("Alpha");
        }
    }
}