
using Microsoft.EntityFrameworkCore;

namespace Rene.Utils.Db.Sample.App1
{
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

            builder.Services.AddDbContext<SampleDbContext>(options =>
            {
                options.UseSqlite("Data Source=Database.db");
            });

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

            await  app.RunAsync();
        }
    }
}
