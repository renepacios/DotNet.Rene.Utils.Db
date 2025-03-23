


namespace Rene.Utils.Db.Sample.App1
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

            builder.AddDbUtils<SampleDbContext>(options =>
            {

            }, typeof(Program).Assembly);

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
