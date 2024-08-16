using Microsoft.EntityFrameworkCore;
using Rene.Utils.Db.Sample.App1.Model;

namespace Rene.Utils.Db.Sample.App1;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {
    }

    public DbSet<WeatherForecast> WeatherForecast { get; set; }
}