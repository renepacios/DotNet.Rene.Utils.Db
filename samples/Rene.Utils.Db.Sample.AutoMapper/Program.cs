


namespace Rene.Utils.Db.Sample.AutoMapper
{
    using Microsoft.EntityFrameworkCore;


    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services
                .AddMagicAutoMapper()
                .AddAutoMapper(typeof(Program).Assembly);

            builder.Services.AddDbContext<SampleDbContext>(options =>
            {
                options.UseSqlite("Data Source=Database.db");
            });

            builder.Services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<Program>());

            builder.AddDbUtils<SampleDbContext>(options => options
                .UseAutoMapper()
                .UseCommandLogging(logging =>
                {
                    logging.Before = (services, context, cancellationToken) =>
                    {
                        services.GetRequiredService<ILogger<Program>>().LogInformation(
                            "Starting {CommandType} at {StartedAt}, execution {ExecutionId}",
                            context.CommandType, context.StartedAt, context.ExecutionId);
                        return Task.CompletedTask;
                    };
                    logging.Completed = (services, context, cancellationToken) =>
                    {
                        services.GetRequiredService<ILogger<Program>>().LogInformation(
                            "Completed {CommandType}: {Outcome} in {ElapsedMs} ms at {CompletedAt}, execution {ExecutionId}",
                            context.CommandType, context.Outcome, context.Elapsed.TotalMilliseconds,
                            context.CompletedAt, context.ExecutionId);
                        return Task.CompletedTask;
                    };
                }), typeof(Program).Assembly);

            var app = builder.Build();

            await app.InitializeDbAsync();//.Wait();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            //app.Run();

            await app.RunAsync();
        }
    }
}
