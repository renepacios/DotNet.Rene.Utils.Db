using MediatR;
using Rene.Utils.Db.Commands;

namespace Rene.Utils.Db.ExtensionHelpers
{
    /// <summary>
    /// Sends the generic database commands through MediatR without constructing them explicitly.
    /// </summary>
    public static class IMediatorExtensions
    {
        /// <summary>
        /// Gets a view model by its identifier.
        /// </summary>
        public static Task<TViewModel> Get<TViewModel>(this IMediator mediator, object id,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new GetCommand<TViewModel>(id), cancellationToken);
        }

        /// <summary>
        /// Gets all view models.
        /// </summary>
        public static Task<IEnumerable<TViewModel>> GetAll<TViewModel>(this IMediator mediator,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new GetAllCommand<TViewModel>(), cancellationToken);
        }

        /// <summary>
        /// Gets the view models matching the specification.
        /// </summary>
        public static Task<IReadOnlyList<TViewModel>> GetBySpec<TViewModel, TModel>(this IMediator mediator,
            IDbUtilsSpecification<TModel> specification, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new GetBySpecCommand<TViewModel, TModel>(specification), cancellationToken);
        }

        /// <summary>
        /// Gets a page of view models using a zero-based page number.
        /// </summary>
        public static Task<IDbUtilsPaginatedData<TViewModel>> GetPaginated<TViewModel, TModel>(this IMediator mediator,
            int size, int pageNumber = 0, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new GetPaginatedCommand<TViewModel, TModel>(size, pageNumber), cancellationToken);
        }

        /// <summary>
        /// Gets a page of view models matching the specification using a zero-based page number.
        /// </summary>
        public static Task<IDbUtilsPaginatedData<TViewModel>> GetPaginated<TViewModel, TModel>(this IMediator mediator,
            IDbUtilsSpecification<TModel> specification, int size, int pageNumber = 0,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new GetPaginatedCommand<TViewModel, TModel>(specification, size, pageNumber), cancellationToken);
        }

        /// <summary>
        /// Gets a page of view models matching the specification using the supplied pagination options.
        /// </summary>
        public static Task<IDbUtilsPaginatedData<TViewModel>> GetPaginated<TViewModel, TModel>(this IMediator mediator,
            IDbUtilsSpecification<TModel> specification, PaginationOptions paginationOptions,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            ArgumentNullException.ThrowIfNull(paginationOptions);
            return mediator.Send(new GetPaginatedCommand<TViewModel, TModel>(specification, paginationOptions), cancellationToken);
        }

        /// <summary>
        /// Adds an entity from the view model and returns the created view model.
        /// </summary>
        public static Task<TViewModel> Add<TViewModel>(this IMediator mediator, TViewModel viewModel,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new AddCommand<TViewModel>(viewModel), cancellationToken);
        }

        /// <summary>
        /// Updates an entity by its identifier using the view model.
        /// </summary>
        public static Task<TViewModel> Update<TViewModel>(this IMediator mediator, TViewModel viewModel, object id,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new UpdateCommand<TViewModel>(viewModel, id), cancellationToken);
        }

        /// <summary>
        /// Deletes an entity by its identifier.
        /// </summary>
        public static Task<bool> Delete<TViewModel>(this IMediator mediator, object id,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediator);
            return mediator.Send(new DeleteCommand<TViewModel>(id), cancellationToken);
        }
    }
}
