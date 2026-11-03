using System.Collections.Generic;
using Rene.Utils.Db.Commands;

namespace Rene.Utils.Db.Authorization
{
    /// <summary>
    /// Builds the requirements for a command. All returned requirements must succeed.
    /// Implementations must not retain mutable per-request state.
    /// </summary>
    public interface ICommandAuthorization<TCommand>
    {
        IEnumerable<IAuthorizationRequirement> GetRequirements(TCommand command);
    }

    /// <summary>Defines authorization for adding a view model.</summary>
    public interface IAuthorizationAdd<TViewModel> : ICommandAuthorization<AddCommand<TViewModel>> { }

    /// <summary>Defines authorization for updating a view model.</summary>
    public interface IAuthorizationUpdate<TViewModel> : ICommandAuthorization<UpdateCommand<TViewModel>> { }

    /// <summary>Defines authorization for deleting a view model.</summary>
    public interface IAuthorizationDelete<TViewModel> : ICommandAuthorization<DeleteCommand<TViewModel>> { }

    /// <summary>Defines authorization for getting a view model by identifier.</summary>
    public interface IAuthorizationGet<TViewModel> : ICommandAuthorization<GetCommand<TViewModel>> { }

    /// <summary>Defines authorization for getting all view models.</summary>
    public interface IAuthorizationGetAll<TViewModel> : ICommandAuthorization<GetAllCommand<TViewModel>> { }

    /// <summary>Defines authorization for specification-based queries.</summary>
    public interface IAuthorizationGetBySpec<TViewModel, TModel>
        : ICommandAuthorization<GetBySpecCommand<TViewModel, TModel>> { }

    /// <summary>Defines authorization for paginated queries, with or without a specification.</summary>
    public interface IAuthorizationGetPaginated<TViewModel, TModel>
        : ICommandAuthorization<GetPaginatedCommand<TViewModel, TModel>> { }
}
