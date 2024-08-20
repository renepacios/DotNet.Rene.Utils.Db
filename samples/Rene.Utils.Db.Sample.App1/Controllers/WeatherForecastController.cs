using Microsoft.AspNetCore.Mvc;
using Rene.Utils.Db.Sample.App1.Model;

namespace Rene.Utils.Db.Sample.App1.Controllers
{
    using Commands;
    using MediatR;
    using ViewModel;

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ISender _sender;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        public WeatherForecastController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost(Name = "PostWeatherForecast")]
        public async Task<IActionResult> Post(WeatherForecastViewModel viewModel)
        {
            var result = await _sender.Send(new AddCommand<WeatherForecastViewModel>(viewModel));
            return Created(string.Empty, result);
        }
    }
}
