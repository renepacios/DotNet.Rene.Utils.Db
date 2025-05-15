namespace Rene.Utils.Db.UnitTest.Models.Specifications
{
    using System.Linq.Expressions;
    using Tests.Common;

    internal class ProductNameContainsSpec(string value) : IDbUtilsSpecification<Product>
    {
        public Expression<Func<Product, bool>> Criteria => x => x.Name.Contains(value);
        public ICollection<Expression<Func<Product, bool>>> Criterias { get; }
        public List<Expression<Func<Product, object>>> Includes { get; }
        public List<string> IncludeStrings { get; }
        public List<KeyValuePair<bool, Expression<Func<Product, object>>>> OrderBy { get; }
        public Expression<Func<Product, object>> GroupBy { get; }
        public bool IgnoreQueryFilters { get; }
        public bool AsNoTracking { get; }
        public bool AsSplitQuery { get; }
        public int Take { get; }
        public int Skip { get; }
        public bool IsPagingEnabled { get; }
    }
}