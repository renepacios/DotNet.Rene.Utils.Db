namespace Rene.Utils.Db.Sample.Mapster.Events
{
    using MediatR;
    using Model;
    using Rene.Utils.Db.Events;
    using ViewModel;

    public sealed class WeatherForecastEventHandler(ILogger<WeatherForecastEventHandler> logger) :
        INotificationHandler<EntityAdded<WeatherForecast, WeatherForecastViewModelDetails>>,
        INotificationHandler<EntityUpdated<WeatherForecast, WeatherForecastViewModelDetails>>,
        INotificationHandler<EntityDeleted<WeatherForecast, WeatherForecastViewModelDetails>>
    {
        public Task Handle(EntityAdded<WeatherForecast, WeatherForecastViewModelDetails> notification,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Forecast {Id} added; input temperature {InputTemperature}, saved temperature {Temperature}",
                notification.Entity.Id, notification.ViewModel.TemperatureC, notification.Response.TemperatureC);
            return Task.CompletedTask;
        }

        public Task Handle(EntityUpdated<WeatherForecast, WeatherForecastViewModelDetails> notification,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Forecast {Id} updated; saved temperature {Temperature}",
                notification.Id, notification.Entity.TemperatureC);
            return Task.CompletedTask;
        }

        public Task Handle(EntityDeleted<WeatherForecast, WeatherForecastViewModelDetails> notification,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Forecast {Id} deleted; previous temperature {Temperature}",
                notification.Id, notification.Entity.TemperatureC);
            return Task.CompletedTask;
        }
    }
}
