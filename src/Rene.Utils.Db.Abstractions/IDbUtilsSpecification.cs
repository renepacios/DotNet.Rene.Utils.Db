using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Rene.Utils.Db
{
    //public enum Ordering
    //{
    //    Desc,
    //    Asc
    //}

    //public interface IDbUtilsSpecification<T, TOrdering> where TOrdering : struct
    //{
    //    Expression<Func<T, bool>> Criteria { get; }
    //    ICollection<Expression<Func<T, bool>>> Criterias { get; }
    //    List<Expression<Func<T, object>>> Includes { get; }
    //    List<string> IncludeStrings { get; }
    //    List<KeyValuePair<bool, Expression<Func<T, object>>>> OrderBy { get; }
    //    Expression<Func<T, object>> GroupBy { get; }

    //    int Take { get; }
    //    int Skip { get; }
    //    bool IsPagingEnabled { get; }
    //}


    public interface IDbUtilsSpecification<T> //: IDbUtilsSpecification<T,int>
    {
        Expression<Func<T, bool>> Criteria { get; }
        ICollection<Expression<Func<T, bool>>> Criterias { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        List<string> IncludeStrings { get; }
        List<KeyValuePair<bool, Expression<Func<T, object>>>> OrderBy { get; }
        Expression<Func<T, object>> GroupBy { get; }

        int Take { get; }
        int Skip { get; }
        bool IsPagingEnabled { get; }
    }
}
