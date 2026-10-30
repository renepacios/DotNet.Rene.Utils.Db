namespace Rene.Utils.Db.Sample.Mapster.ViewModel
{
    using Model;

    public class WeatherForecastViewModel : IWithGenericHandler<WeatherForecast>
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }
}