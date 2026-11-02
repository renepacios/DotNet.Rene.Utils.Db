namespace Rene.Utils.Db.UnitTest.Specifications
{
    using Builder;
    using CompositeSpecifications;
    using Microsoft.EntityFrameworkCore;
    using Models;

    public class SpecificationQueryOptionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetQuery_AppliesSplitQueryOnlyWhenEnabled(bool enabled)
        {
            using var context = new QueryOptionsContext(new DbContextOptionsBuilder<QueryOptionsContext>()
                .UseSqlServer("Server=localhost;Database=SpecificationTests;Integrated Security=true")
                .Options);
            IDbUtilsSpecification<QueryParent> specification = new CompositeSpecification<QueryParent>
            {
                Includes = [parent => parent.Children]
            };
            if (enabled)
                specification = specification.AsSplitQuery();

            var query = SpecificationEvaluator<QueryParent>.GetQuery(context.Set<QueryParent>(), specification);

            query.Expression.ToString().Contains("AsSplitQuery()").Should().Be(enabled);
            var sql = query.ToQueryString();
            sql.Should().Contain("SELECT");
            sql.Contains("JOIN").Should().Be(!enabled);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void GetQuery_AppliesFiltersAndTracking_WithSplitQueryOnInMemory(
            bool ignoreQueryFilters, bool asNoTracking)
        {
            using var context = new QueryOptionsContext(new DbContextOptionsBuilder<QueryOptionsContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
            context.Samples.AddRange(
                new Sample { Id = 1, Name = "Visible" },
                new Sample { Id = 2, Name = "Hidden" });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            var specification = new DbUtilsAllSpecification<Sample>().AsSplitQuery();
            if (ignoreQueryFilters)
                specification = specification.IgnoreQueryFilters();
            if (asNoTracking)
                specification = specification.AsNoTracking();

            var results = SpecificationEvaluator<Sample>.GetQuery(context.Samples, specification).ToList();

            results.Should().HaveCount(ignoreQueryFilters ? 2 : 1);
            context.ChangeTracker.Entries<Sample>().Should().HaveCount(asNoTracking ? 0 : results.Count);
        }

        [Fact]
        public void Evaluate_QueryOptions_DoNotAffectEnumerableEvaluation()
        {
            var data = new[] { new Sample { Id = 1, Name = "Alpha" }, new Sample { Id = 2, Name = "Beta" } };
            var specification = new Models.Specifications.NameStartsWithSpec("A")
                .AsSplitQuery().IgnoreQueryFilters().AsNoTracking();

            SpecificationEvaluator<Sample>.Evaluate(data, specification).Should().ContainSingle()
                .Which.Name.Should().Be("Alpha");
        }

        private sealed class QueryOptionsContext(DbContextOptions<QueryOptionsContext> options) : DbContext(options)
        {
            public DbSet<Sample> Samples => Set<Sample>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Sample>().HasQueryFilter(sample => sample.Name != "Hidden");
                modelBuilder.Entity<QueryParent>().HasMany(parent => parent.Children).WithOne()
                    .HasForeignKey(child => child.ParentId);
            }
        }

        private sealed class QueryParent
        {
            public int Id { get; set; }
            public List<QueryChild> Children { get; set; } = [];
        }

        private sealed class QueryChild
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
        }
    }
}
