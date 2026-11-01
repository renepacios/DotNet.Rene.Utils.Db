using AutoMapper;
using Magic.AutoMapper;
using Rene.Utils.Db.Sample.AutoMapper.Model;

namespace Rene.Utils.Db.Sample.AutoMapper.ViewModel;

[AutoMap(typeof(WeatherForecast))]
public class WeatherForecastViewModelDetails : WeatherForecastViewModel
, IWithGenericHandler<WeatherForecast>
, IMapFrom<WeatherForecast>
{

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}