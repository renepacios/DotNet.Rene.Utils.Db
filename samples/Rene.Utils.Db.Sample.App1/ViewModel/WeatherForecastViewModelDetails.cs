using Rene.Utils.Db.Sample.App1.Model;

namespace Rene.Utils.Db.Sample.App1.ViewModel;

public class WeatherForecastViewModelDetails : WeatherForecastViewModel
, IWithGenericHandler<WeatherForecast>
{

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}