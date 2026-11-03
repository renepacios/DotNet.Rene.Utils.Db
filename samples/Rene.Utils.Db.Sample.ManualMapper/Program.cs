namespace Rene.Utils.Db.Sample.ManualMapper
{
    using Microsoft.EntityFrameworkCore;
    using Logging;
    using Rene.Utils.Db.Authorization;
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
            builder.Services.AddScoped<ISampleCommandEventWriter, SampleCommandEventWriter>();

            builder.Services.AddDbContext<SampleDbContext>(options => options.UseSqlite("Data Source=Database.db"));

            builder.Services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<Program>());

            builder.AddDbUtils<SampleDbContext>(options => options
                .UseCommandLogging(logging =>
                {
                    logging.Before = (services, context, cancellationToken) =>
                        services.GetRequiredService<ISampleCommandEventWriter>().Starting(context, cancellationToken);
                    logging.Completed = (services, context, cancellationToken) =>
                        services.GetRequiredService<ISampleCommandEventWriter>().Completed(context, cancellationToken);
                })
                .UseMapper(m => m
            if (builder.Configuration.GetValue<bool>("Authorization:Enabled"))
            {
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddDbUtilsAuthorization(typeof(Program).Assembly);
            }

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

            app.Use(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (CommandAuthorizationException) when (!context.Response.HasStarted)
                {
                    var statusCode = context.User.Identity?.IsAuthenticated == true
                        ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
                    await Results.Problem(statusCode: statusCode, title: "Command authorization denied")
                        .ExecuteAsync(context);
                }
            });

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
