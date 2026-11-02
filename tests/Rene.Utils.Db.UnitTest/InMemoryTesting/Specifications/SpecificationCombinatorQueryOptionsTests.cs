namespace Rene.Utils.Db.UnitTest.Specifications
{
    using CompositeSpecifications;
    using Models;
    using Models.Specifications;
    using System.Linq.Expressions;

    public class SpecificationCombinatorQueryOptionsTests
    {
        [Theory]
        [InlineData("AsSplitQuery")]
        [InlineData("IgnoreQueryFilters")]
        [InlineData("AsNoTracking")]
        public void QueryOption_EnablesOnlyRequestedOption_WithoutChangingOriginal(string option)
        {
            var original = new NameStartsWithSpec("A");

            var result = ApplyOption(original, option);

            result.Should().NotBeSameAs(original);
            result.AsSplitQuery.Should().Be(option == "AsSplitQuery");
            result.IgnoreQueryFilters.Should().Be(option == "IgnoreQueryFilters");
            result.AsNoTracking.Should().Be(option == "AsNoTracking");
            original.AsSplitQuery.Should().BeFalse();
            original.IgnoreQueryFilters.Should().BeFalse();
            original.AsNoTracking.Should().BeFalse();
            result.Criteria.Compile()(new Sample { Name = "Alpha" }).Should().BeTrue();
            result.Criteria.Compile()(new Sample { Name = "Beta" }).Should().BeFalse();
        }

        [Theory]
        [InlineData("AsSplitQuery")]
        [InlineData("IgnoreQueryFilters")]
        [InlineData("AsNoTracking")]
        public void QueryOption_NullSpecification_Throws(string option)
        {
            Action act = () => ApplyOption(null, option);

            act.Should().Throw<ArgumentNullException>().WithParameterName("specification");
        }

        [Fact]
        public void QueryOptions_CanBeChainedRepeatedly_OnSpecificationWithoutCriteria()
        {
            var original = new DbUtilsAllSpecification<Sample>();

            var result = original.AsSplitQuery().IgnoreQueryFilters().AsNoTracking().AsSplitQuery();

            AssertAllOptions(result);
            original.AsSplitQuery.Should().BeFalse();
            result.Criteria.Should().BeNull();
        }

        [Fact]
        public void QueryOptions_PreserveSpecificationState_AndCopyCollections()
        {
            var original = new CompositeSpecification<Sample>
            {
                Criteria = sample => sample.Id > 0,
                Criterias = new List<Expression<Func<Sample, bool>>> { sample => sample.Name != null },
                Includes = new List<Expression<Func<Sample, object>>> { sample => sample.Name },
                IncludeStrings = new List<string> { "Related" },
                OrderBy = new List<KeyValuePair<bool, Expression<Func<Sample, object>>>>
                {
                    new(true, sample => sample.Id)
                },
                GroupBy = sample => sample.Name,
                Take = 5,
                Skip = 2,
                IsPagingEnabled = true
            };

            var result = original.AsSplitQuery().IgnoreQueryFilters().AsNoTracking();

            result.Criteria.Should().BeSameAs(original.Criteria);
            result.Criterias.Should().Equal(original.Criterias).And.NotBeSameAs(original.Criterias);
            result.Includes.Should().Equal(original.Includes).And.NotBeSameAs(original.Includes);
            result.IncludeStrings.Should().Equal(original.IncludeStrings).And.NotBeSameAs(original.IncludeStrings);
            result.OrderBy.Should().Equal(original.OrderBy).And.NotBeSameAs(original.OrderBy);
            result.GroupBy.Should().BeSameAs(original.GroupBy);
            result.Take.Should().Be(5);
            result.Skip.Should().Be(2);
            result.IsPagingEnabled.Should().BeTrue();
            AssertAllOptions(result);
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void BinaryCombinators_PreserveOptionsFromEitherOperand(bool useOr, bool leftEnabled, bool rightEnabled)
        {
            IDbUtilsSpecification<Sample> left = new NameStartsWithSpec("A");
            IDbUtilsSpecification<Sample> right = new NameStartsWithSpec("B");
            if (leftEnabled)
                left = left.AsSplitQuery().IgnoreQueryFilters().AsNoTracking();
            if (rightEnabled)
                right = right.AsSplitQuery().IgnoreQueryFilters().AsNoTracking();

            var result = useOr ? left.Or(right) : left.And(right);

            result.AsSplitQuery.Should().Be(leftEnabled || rightEnabled);
            result.IgnoreQueryFilters.Should().Be(leftEnabled || rightEnabled);
            result.AsNoTracking.Should().Be(leftEnabled || rightEnabled);
            result.Criteria.Compile()(new Sample { Name = "Alpha" }).Should().Be(useOr);
        }

        [Fact]
        public void BinaryCombinators_MergeDifferentOptions_AndSupportOptionsAfterComposition()
        {
            var left = new NameStartsWithSpec("A").AsSplitQuery();
            var right = new NameStartsWithSpec("B").IgnoreQueryFilters();

            var result = left.Or(right).AsNoTracking();

            AssertAllOptions(result);
            left.IgnoreQueryFilters.Should().BeFalse();
            right.AsSplitQuery.Should().BeFalse();
        }

        [Fact]
        public void InvertAndOrderBy_PreserveQueryOptions()
        {
            var specification = new NameStartsWithSpec("A").AsSplitQuery().IgnoreQueryFilters().AsNoTracking();

            var inverted = specification.ToInvert();
            var ordered = inverted.OrderBy(new[] { "Name,asc" });

            AssertAllOptions(inverted);
            AssertAllOptions(ordered);
            ordered.OrderBy.Should().ContainSingle();
            ordered.Criteria.Compile()(new Sample { Name = "Beta" }).Should().BeTrue();
            ordered.Criteria.Compile()(new Sample { Name = "Alpha" }).Should().BeFalse();
        }

        private static IDbUtilsSpecification<Sample> ApplyOption(IDbUtilsSpecification<Sample> specification, string option)
            => option switch
            {
                "AsSplitQuery" => specification.AsSplitQuery(),
                "IgnoreQueryFilters" => specification.IgnoreQueryFilters(),
                "AsNoTracking" => specification.AsNoTracking(),
                _ => throw new ArgumentOutOfRangeException(nameof(option))
            };

        private static void AssertAllOptions(IDbUtilsSpecification<Sample> specification)
        {
            specification.AsSplitQuery.Should().BeTrue();
            specification.IgnoreQueryFilters.Should().BeTrue();
            specification.AsNoTracking.Should().BeTrue();
        }
    }
}
