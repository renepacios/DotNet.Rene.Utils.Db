
using System.Collections.Generic;
using MediatR;

namespace Rene.Utils.Db.Commands
{
    public class GetCommand<TViewModel> : IRequest<TViewModel>
    {
        public object Id { get; }

        public GetCommand(object id)
        {
            Id = id;
        }
    }

    public class GetAllCommand<TViewModel> : IRequest<IEnumerable<TViewModel>>
    {

    }

    public class GetBySpecCommand<TViewModel, TModel> : IRequest<IReadOnlyList<TViewModel>>
    {
        //TODO:

        public IDbUtilsSpecification<TModel> Specification { get; }

        public GetBySpecCommand(IDbUtilsSpecification<TModel> specification)
        {
            Specification = specification;
        }
    }

    public class PaginationOptions
    {
        public int Size { get; set; }
        public int Page { get; set; }

        public static PaginationOptions AsDefault => new() { Size = 10, Page = 0 };
    }

    public class GetPaginatedCommand<TViewModel, TModel> : IRequest<IDbUtilsPaginatedData<TViewModel>>
    {
        public IDbUtilsSpecification<TModel> Specification { get; }
        public int Size { get; }
        public int PageNumber { get; }

        public GetPaginatedCommand(int size, int pageNumber = 0)
        {
            Size = size;
            PageNumber = pageNumber;
        }

        public GetPaginatedCommand(IDbUtilsSpecification<TModel> specification, int size, int pageNumber = 0)
        {
            Specification = specification;
            Size = size;
            PageNumber = pageNumber;
        }

        public GetPaginatedCommand(IDbUtilsSpecification<TModel> specification, PaginationOptions paginationOptions) :
        this(specification, paginationOptions.Size, paginationOptions.Page)
        {
        }
    }
}


