
//TODO: Mover espeficiación de paginación a otra general
namespace Rene.Utils.Db.Builder
{
    using System;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;
    using Rene.Utils.Db;

    internal class SpecificationEvaluator<TEntity>
        where TEntity : class //, IEntity<Tk>
    {


        public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> query, IDbUtilsSpecification<TEntity> specification)
        {
            _ = query ?? throw new ArgumentNullException(nameof(query));
            _ = specification ?? throw new ArgumentNullException(nameof(specification));


            // modify the IQueryable using the specification's criteria expression
            if (specification.Criteria != null)
            {
                query = query.Where(specification.Criteria);
            }
            if (specification.Criterias != null)
            {
                query = specification.Criterias.Aggregate(query, (current, criteria) => current.Where(criteria));
            }


            // Includes all expression-based includes
            if (specification.Includes != null)
            {
                query = specification.Includes.Aggregate(query,
                    (current, include) => current.Include(include));
            }

            // Include any string-based include statements
            if (specification.IncludeStrings != null)
            {
                query = specification.IncludeStrings.Aggregate(query,
                    (current, include) => current.Include(include));
            }

          
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

            if (specification.GroupBy != null)
            {
                query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
            }

            // Apply paging if enabled
            if (specification.IsPagingEnabled)
            {
                query = query
                    .Skip(specification.Skip)
                    .Take(specification.Take);
            }

            if (specification.IgnoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            if (specification.AsNoTracking)
            {
                query = query.AsNoTracking();
            }


            ////TODO: Hacer con .net 10 ++
            //if (specification.AsSplitQuery)
            //{
                
            //        query = query.AsSplitQuery();
    
            //}

            return query;
        }
    }


}
