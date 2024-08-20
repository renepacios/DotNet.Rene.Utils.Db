namespace Rene.Utils.Db.Sample.App1.ViewModel
{
    using AutoMapper;
    using Magic.AutoMapper;
    using Model;

    [AutoMap(typeof(WeatherForecast))]
    public class WeatherForecastViewModel
        : IWithGenericHandler<WeatherForecast>,
            IMapFrom<WeatherForecast>
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        
    }
}
