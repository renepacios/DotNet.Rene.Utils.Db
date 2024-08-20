using AutoMapper;
using Magic.AutoMapper;
using Rene.Utils.Db.Sample.App1.Model;

namespace Rene.Utils.Db.Sample.App1.ViewModel;

[AutoMap(typeof(WeatherForecast))]
public class WeatherForecastViewModelDetails : WeatherForecastViewModel
, IWithGenericHandler<WeatherForecast>
, IMapFrom<WeatherForecast>
{

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}