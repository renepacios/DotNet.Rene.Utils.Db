namespace Rene.Utils.Db.Sample.AutoMapper.ViewModel
{
    using global::AutoMapper;
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
