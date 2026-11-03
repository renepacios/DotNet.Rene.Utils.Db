using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rene.Utils.Db.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Opt-in, assembly-based command authorization registration.</summary>
    public static class AuthorizationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers command authorization definitions and closed requirement handlers.
        /// Commands without definitions are allowed. Multiple definitions and requirements are combined with AND.
        /// Open generic definitions must be registered explicitly as closed types.
        /// Register this behavior before caching and other behaviors that can short-circuit command execution.
        /// </summary>
        public static IServiceCollection AddDbUtilsAuthorization(this IServiceCollection services, params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(assemblies);
            if (assemblies.Length == 0 || assemblies.Any(assembly => assembly == null))
                throw new ArgumentException("Provide at least one non-null authorization assembly.", nameof(assemblies));

            services.AddLogging();
            services.TryAddTransient(typeof(RequirementExecutor<>), typeof(RequirementExecutor<>));
            services.TryAddEnumerable(ServiceDescriptor.Transient(
                typeof(IPipelineBehavior<,>), typeof(CommandAuthorizationBehavior<,>)));

            foreach (var type in assemblies.Distinct().SelectMany(assembly => assembly.DefinedTypes)
                         .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters))
            {
                foreach (var contract in type.ImplementedInterfaces.Where(contract => contract.IsGenericType))
                {
                    var definition = contract.GetGenericTypeDefinition();
                    if (definition == typeof(ICommandAuthorization<>) || definition == typeof(IRequirementHandler<>))
                        services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, type.AsType()));
                }
            }

            return services;
        }

        /// <summary>Registers a closed handler, including a closed instance of a generic requirement handler.</summary>
        public static IServiceCollection AddAuthorizationRequirementHandler<TRequirement, THandler>(
            this IServiceCollection services)
            where TRequirement : IAuthorizationRequirement
            where THandler : class, IRequirementHandler<TRequirement>
        {
            ArgumentNullException.ThrowIfNull(services);
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IRequirementHandler<TRequirement>, THandler>());
            return services;
        }
    }
}
