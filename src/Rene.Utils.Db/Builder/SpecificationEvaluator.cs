//TODO: Mover espeficiación de paginación a otra general

namespace Rene.Utils.Db.Builder
{
    using Microsoft.EntityFrameworkCore;

    internal class SpecificationEvaluator<TEntity>
        where TEntity : class //, IEntity<Tk>
    {
        /// <summary>
        /// Evaluates the specification against the data source
        /// Used for in-memory evaluation, in order to test the specification in unit tests
        /// Include operations are not supported in this implementation (as they are only relevant to data providers)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<TEntity> Evaluate(IEnumerable<TEntity> data, IDbUtilsSpecification<TEntity> specification)
        {
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            ArgumentNullException.ThrowIfNull(specification, nameof(specification));

            if (specification.Criteria != null) data = data.Where(specification.Criteria.Compile());

            if (specification.Criterias != null) data = specification.Criterias.Aggregate(data, (current, criteria) => data.Where(criteria.Compile()));

            if (specification.OrderBy != null && specification.OrderBy.Count > 0)
            {
                var (ordering, function) = specification.OrderBy[0];
                var orderBy = ordering ? data.OrderBy(function.Compile()) : data.OrderByDescending(function.Compile());

                for (var idx = 1; idx < specification.OrderBy.Count; idx++)
                {
                    (ordering, function) = specification.OrderBy[idx];
                    orderBy = ordering ? orderBy.ThenBy(function.Compile()) : orderBy.ThenByDescending(function.Compile());
                }

                data = orderBy;
            }

            if (specification.GroupBy != null) data = data.GroupBy(specification.GroupBy.Compile()).SelectMany(x => x);

            if (specification.IsPagingEnabled)
                data = data
                    .Skip(specification.Skip)
                    .Take(specification.Take);

            return data;
        }

        public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> query, IDbUtilsSpecification<TEntity> specification)
        {
            _ = query ?? throw new ArgumentNullException(nameof(query));
            _ = specification ?? throw new ArgumentNullException(nameof(specification));


            // modify the IQueryable using the specification's criteria expression
            if (specification.Criteria != null) query = query.Where(specification.Criteria);
            if (specification.Criterias != null) query = specification.Criterias.Aggregate(query, (current, criteria) => current.Where(criteria));


            // Includes all expression-based includes
            if (specification.Includes != null)
                query = specification.Includes.Aggregate(query,
                    (current, include) => current.Include(include));

            // Include any string-based include statements
            if (specification.IncludeStrings != null)
                query = specification.IncludeStrings.Aggregate(query,
                    (current, include) => current.Include(include));

            //if (specification.IgnoreQueryFilters)
            //{
            //    query = query.IgnoreQueryFilters();
            //}

            // Apply ordering if expressions are set
            if (specification.OrderBy != null && specification.OrderBy.Count > 0)
            {
                var (ordering, function) = specification.OrderBy[0];
                var orderBy = ordering ? query.OrderBy(function) : query.OrderByDescending(function);

                for (var idx = 1; idx < specification.OrderBy.Count; idx++)
                {
                    (ordering, function) = specification.OrderBy[idx];
                    orderBy = ordering ? orderBy.ThenBy(function) : orderBy.ThenByDescending(function);
                }

                query = orderBy;
            }

            if (specification.GroupBy != null) query = query.GroupBy(specification.GroupBy).SelectMany(x => x);

            // Apply paging if enabled
            if (specification.IsPagingEnabled)
                query = query
                    .Skip(specification.Skip)
                    .Take(specification.Take);
            return query;
        }
    }
}