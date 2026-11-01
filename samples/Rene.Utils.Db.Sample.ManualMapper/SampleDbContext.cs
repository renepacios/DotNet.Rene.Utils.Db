namespace Rene.Utils.Db.Sample.ManualMapper
{
    using Microsoft.EntityFrameworkCore;
    using Model;

    public class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
    {
        public DbSet<WeatherForecast> WeatherForecast { get; set; } = null!;
    }
}