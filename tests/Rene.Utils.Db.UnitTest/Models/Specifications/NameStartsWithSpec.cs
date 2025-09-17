namespace Rene.Utils.Db.UnitTest.Models.Specifications
{
    using System.Linq.Expressions;

    internal class NameStartsWithSpec(string value) : IDbUtilsSpecification<Sample>
    {
        public Expression<Func<Sample, bool>> Criteria => x => x.Name.StartsWith(value);
        public ICollection<Expression<Func<Sample, bool>>> Criterias { get; }
        public List<Expression<Func<Sample, object>>> Includes { get; }
        public List<string> IncludeStrings { get; }
        public List<KeyValuePair<bool, Expression<Func<Sample, object>>>> OrderBy { get; }
        public Expression<Func<Sample, object>> GroupBy { get; }
        public bool IgnoreQueryFilters { get; }
        public bool AsNoTracking { get; }
        public bool AsSplitQuery { get; }
        public int Take { get; }
        public int Skip { get; }
        public bool IsPagingEnabled { get; }
    }
}