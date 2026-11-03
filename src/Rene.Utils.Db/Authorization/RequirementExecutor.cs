using Microsoft.Extensions.DependencyInjection;

namespace Rene.Utils.Db.Authorization
{
    internal interface IRequirementExecutor
    {
        Task<AuthorizationResult> Execute(IAuthorizationRequirement requirement, CancellationToken cancellationToken);
    }

    internal sealed class RequirementExecutor<TRequirement>(IServiceProvider services) : IRequirementExecutor
        where TRequirement : IAuthorizationRequirement
    {
        public async Task<AuthorizationResult> Execute(
            IAuthorizationRequirement requirement, CancellationToken cancellationToken)
        {
            var handlers = services.GetServices<IRequirementHandler<TRequirement>>().ToArray();
            if (handlers.Length != 1)
                throw new InvalidOperationException(
                    $"Requirement '{typeof(TRequirement)}' must have exactly one registered handler; found {handlers.Length}.");

            return await handlers[0].Handle((TRequirement)requirement, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Handler for '{typeof(TRequirement)}' returned a null result.");
        }
    }
}
