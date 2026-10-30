using Microsoft.EntityFrameworkCore;
using Rene.Utils.Db.Sample.AutoMapper.Model;

namespace Rene.Utils.Db.Sample.AutoMapper;

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {
    }

    public DbSet<WeatherForecast> WeatherForecast { get; set; }
}