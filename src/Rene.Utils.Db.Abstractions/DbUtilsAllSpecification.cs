using System;
using System.Collections.Generic;
using System.Linq;

namespace Rene.Utils.Db
{
    using System.Linq.Expressions;

    public sealed class DbUtilsAllSpecification<T> : IDbUtilsSpecification<T>
    {
        public DbUtilsAllSpecification()
        {

        }

        public DbUtilsAllSpecification(ICollection<KeyValuePair<bool, Expression<Func<T, object>>>> sortSequence)
        {
            OrderBy = sortSequence
                .Select(s => new KeyValuePair<bool, Expression<Func<T, object>>>(s.Key, s.Value))
                .ToList();
        }


        public Expression<Func<T, bool>> Criteria { get; }
        public ICollection<Expression<Func<T, bool>>> Criterias { get; }
        public List<Expression<Func<T, object>>> Includes { get; }
        public List<string> IncludeStrings { get; }
        public List<KeyValuePair<bool, Expression<Func<T, object>>>> OrderBy { get; }
        public Expression<Func<T, object>> GroupBy { get; }
        public bool IgnoreQueryFilters { get; }
        public bool AsNoTracking { get; }
        public bool AsSplitQuery { get; }
        public int Take { get; }
        public int Skip { get; }
        public bool IsPagingEnabled { get; }
    }
}
