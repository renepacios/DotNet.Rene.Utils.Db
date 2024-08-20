using Rene.Utils.Db.Sample.App1.Model;

namespace Rene.Utils.Db.Sample.App1;

public static class SampleDbContextUtils
{
    public static async Task InitializeDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var rng = new Random();
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"

        };
        var weatherForecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index * -1),
            TemperatureC = rng.Next(-20, 55),
            Summary = summaries[rng.Next(summaries.Length)]
        });

        
        dbContext.WeatherForecast.AddRange(weatherForecasts);
        await dbContext.SaveChangesAsync();
    }
}