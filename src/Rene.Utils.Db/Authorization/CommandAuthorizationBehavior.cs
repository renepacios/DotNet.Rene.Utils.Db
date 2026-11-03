using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rene.Utils.Db.Authorization
{
    internal sealed class CommandAuthorizationBehavior<TRequest, TResponse>(
        IEnumerable<ICommandAuthorization<TRequest>> authorizations,
        IServiceProvider services,
        ILogger<CommandAuthorizationBehavior<TRequest, TResponse>> logger)

        : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var authorization in authorizations)
            {
                var requirements = authorization.GetRequirements(request)
                    ?? throw new InvalidOperationException(
                        $"Authorization '{authorization.GetType()}' returned a null requirement collection.");
                var hasRequirements = false;
                foreach (var requirement in requirements)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (requirement == null)
                        throw new InvalidOperationException(
                            $"Authorization '{authorization.GetType()}' returned a null requirement.");

                    hasRequirements = true;
                    var executorType = typeof(RequirementExecutor<>).MakeGenericType(requirement.GetType());
                    var executor = (IRequirementExecutor)services.GetRequiredService(executorType);
                    var result = await executor.Execute(requirement, cancellationToken).ConfigureAwait(false);
                    if (!result.Succeeded)
                    {
                        logger.LogWarning("Authorization denied for command {CommandType} by requirement {RequirementType}.",
                            typeof(TRequest).FullName, requirement.GetType().FullName);
                        throw new CommandAuthorizationException(typeof(TRequest), requirement.GetType(), result.FailureReason);
                    }
                }

                if (!hasRequirements)
                    throw new InvalidOperationException(
                        $"Authorization '{authorization.GetType()}' returned no requirements. Remove the definition to leave a command unprotected.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await next().ConfigureAwait(false);
        }
    }
}
