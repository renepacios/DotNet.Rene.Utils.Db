namespace Rene.Utils.Db.Sample.Mapster
{
    using global::Mapster;
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

            // Mapster maps properties with the same name by convention. Explicit configuration is only
            // needed for custom rules. Registering the TypeAdapterConfig in the container makes UseMapster() use it
            // (otherwise TypeAdapterConfig.GlobalSettings is used).
            var mapsterConfig = new TypeAdapterConfig();
            mapsterConfig.NewConfig<WeatherForecastViewModelDetails, WeatherForecast>()
                .Ignore(dest => dest.Id);
            builder.Services.AddSingleton(mapsterConfig);

            builder.Services.AddDbContext<SampleDbContext>(options => options.UseSqlite("Data Source=Database.db"));

            builder.Services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<Program>());

            builder.AddDbUtils<SampleDbContext>(options => options.UseMapster(), typeof(Program).Assembly);

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
