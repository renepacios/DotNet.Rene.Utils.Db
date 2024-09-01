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
        public async Task<IActionResult> Get()
        {
            //return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            //{
            //    Date = DateTime.Now.AddDays(index),
            //    TemperatureC = Random.Shared.Next(-20, 55),
            //    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            //})
            //.ToArray();

            return Ok(await _sender.Send(new GetAllCommand<WeatherForecastViewModel>()));
        }

        [HttpGet("paginated", Name = "GetWeatherForecastPaginated")]
        public async Task<IDbUtilsPaginatedData<WeatherForecastViewModel>> GetPaginated([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
           return await _sender.Send(new GetPaginatedCommand<WeatherForecastViewModel, WeatherForecast>(new DbUtilsAllSpecification<WeatherForecast>(), size, page));
        }

        [HttpPost(Name = "PostWeatherForecast")]
        public async Task<IActionResult> Post(WeatherForecastViewModel viewModel)
        {
            var result = await _sender.Send(new AddCommand<WeatherForecastViewModel>(viewModel));
            return Created(string.Empty, result);
        }

        [HttpPut("{id}", Name = "PutWeatherForecast")]
        public async Task<IActionResult> Put(int id, WeatherForecastViewModel viewModel)
        {
            var result = await _sender.Send(new UpdateCommand<WeatherForecastViewModel>(viewModel, id));
            return Ok(result);
        }

        [HttpDelete("{id}", Name = "DeleteWeatherForecast")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _sender.Send(new DeleteCommand<WeatherForecastViewModel>(id));
            return Ok(result);
        }

    }
}
