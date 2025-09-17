using System.Linq.Expressions;

namespace Rene.Utils.Db.CompositeSpecifications
{
    internal class CompositeSpecification<T> : IDbUtilsSpecification<T>, IDbUtilsCompositeSpecification<T>
    {
        internal CompositeSpecification() { }

        public Expression<Func<T, bool>> Criteria { get; internal set; }
        public ICollection<Expression<Func<T, bool>>> Criterias { get; internal set; }
        public List<Expression<Func<T, object>>> Includes { get; internal set; }
        public List<string> IncludeStrings { get; internal set; }

        public List<KeyValuePair<bool, Expression<Func<T, object>>>> OrderBy { get; internal set; }
        public Expression<Func<T, object>> GroupBy { get; internal set; }
        public int Take { get; internal set; }
        public int Skip { get; internal set; }
        public bool IsPagingEnabled { get; internal set; }


        public bool IgnoreQueryFilters { get; internal set; } = false;

        public bool AsNoTracking { get; internal set; } = false;

        // public bool AsSplitQuery => throw new NotImplementedException();
    }
}