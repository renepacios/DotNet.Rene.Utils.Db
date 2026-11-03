namespace Rene.Utils.Db.Sample.ManualMapper.Authorization
{
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Model;

    public sealed record GetForecastCountCommand : IRequest<int>;

    public sealed class GetForecastCountHandler(SampleDbContext context) : IRequestHandler<GetForecastCountCommand, int>
    {
        public Task<int> Handle(GetForecastCountCommand request, CancellationToken cancellationToken)
            => context.Set<WeatherForecast>().CountAsync(cancellationToken);
    }
}
