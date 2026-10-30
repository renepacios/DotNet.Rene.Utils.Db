namespace Rene.Utils.Db.Sample.ManualMapper
{
    using Microsoft.EntityFrameworkCore;
    using Model;
    using ViewModel;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton(TimeProvider.System);

            builder.Services.AddDbContext<SampleDbContext>(options => options.UseSqlite("Data Source=Database.db"));

            builder.Services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<Program>());

            builder.AddDbUtils<SampleDbContext>(options => options.UseMapper(m => m
                // Expressions: used for in-memory mapping and translated to SQL in projections (GetAll, GetPaginated...)
                .Map<WeatherForecast, WeatherForecastViewModel>(e => new WeatherForecastViewModel
                {
                    Id = e.Id,
                    Date = e.Date
                })
                .Map<WeatherForecast, WeatherForecastViewModelDetails>(e => new WeatherForecastViewModelDetails
                {
                    Id = e.Id,
                    Date = e.Date,
                    TemperatureC = e.TemperatureC,
                    Summary = e.Summary
                })
                // Function with access to the dependency injection container (in-memory only, used by AddCommand)
                .Map<WeatherForecastViewModelDetails, WeatherForecast>((sp, vm) => new WeatherForecast
                {
                    Date = vm.Date == default ? sp.GetRequiredService<TimeProvider>().GetLocalNow().DateTime : vm.Date,
                    TemperatureC = vm.TemperatureC,
                    Summary = vm.Summary
                })
                // Update of an existing entity (used by UpdateCommand)
                .MapInto<WeatherForecastViewModelDetails, WeatherForecast>((vm, entity) =>
                {
                    entity.Date = vm.Date;
                    entity.TemperatureC = vm.TemperatureC;
                    entity.Summary = vm.Summary;
                })), typeof(Program).Assembly);

            var app = builder.Build();

            await app.InitializeDbAsync();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
