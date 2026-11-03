namespace Rene.Utils.Db.Sample.ManualMapper.Authorization
{
    using Microsoft.AspNetCore.Http;
    using Rene.Utils.Db.Authorization;
    using Rene.Utils.Db.Commands;
    using ViewModel;

    public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

    public sealed class PermissionRequirementHandler(IHttpContextAccessor accessor)
        : IRequirementHandler<PermissionRequirement>
    {
        public Task<AuthorizationResult> Handle(PermissionRequirement requirement, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = accessor.HttpContext?.User;
            var allowed = user?.Identity?.IsAuthenticated == true && user.HasClaim("permission", requirement.Permission);
            return Task.FromResult(allowed
                ? AuthorizationResult.Allow()
                : AuthorizationResult.Deny("The current user does not have the required forecast permission."));
        }
    }

    public sealed class ForecastWriteAuthorization :
        IAuthorizationAdd<WeatherForecastViewModelDetails>,
        IAuthorizationUpdate<WeatherForecastViewModelDetails>,
        IAuthorizationDelete<WeatherForecastViewModelDetails>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(AddCommand<WeatherForecastViewModelDetails> command)
            => [new PermissionRequirement("forecast.write")];

        public IEnumerable<IAuthorizationRequirement> GetRequirements(UpdateCommand<WeatherForecastViewModelDetails> command)
            => [new PermissionRequirement("forecast.write")];

        public IEnumerable<IAuthorizationRequirement> GetRequirements(DeleteCommand<WeatherForecastViewModelDetails> command)
            => [new PermissionRequirement("forecast.write")];
    }

    public sealed class ForecastCountAuthorization : ICommandAuthorization<GetForecastCountCommand>
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements(GetForecastCountCommand command)
            => [new PermissionRequirement("forecast.read")];
    }
}
