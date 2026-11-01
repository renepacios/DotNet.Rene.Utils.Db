namespace Rene.Utils.Db.Sample.ManualMapper.ViewModel
{
    using Model;

    public class WeatherForecastViewModelDetails : WeatherForecastViewModel, IWithGenericHandler<WeatherForecast>
    {
        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }
}