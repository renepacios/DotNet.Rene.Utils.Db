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
            => Ok(await _sender.Send(new GetAllCommand<WeatherForecastViewModel>()));

        [HttpGet("{id}", Name = "GetWeatherForecastById")]
        public async Task<IActionResult> GetById(int id) 
            => Ok(await _sender.Send(new GetCommand<WeatherForecastViewModelDetails>(id)));

        [HttpGet("paginated", Name = "GetWeatherForecastPaginated")]
        public async Task<IActionResult> GetPaginated([FromQuery] int page = 0, [FromQuery] int size = 10)
            => Ok(await _sender.Send(new GetPaginatedCommand<WeatherForecastViewModelDetails, WeatherForecast>(new DbUtilsAllSpecification<WeatherForecast>(), size, page)));

        [HttpPost(Name = "PostWeatherForecast")]
        public async Task<IActionResult> Post(WeatherForecastViewModelDetails viewModel)
        {
            var result = await _sender.Send(new AddCommand<WeatherForecastViewModelDetails>(viewModel));
            return Created(string.Empty, result);
        }

        [HttpPut("{id}", Name = "PutWeatherForecast")]
        public async Task<IActionResult> Put(int id, WeatherForecastViewModelDetails viewModel)
        {
            var result = await _sender.Send(new UpdateCommand<WeatherForecastViewModelDetails>(viewModel, id));
            return Ok(result);
        }

        [HttpDelete("{id}", Name = "DeleteWeatherForecast")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _sender.Send(new DeleteCommand<WeatherForecastViewModelDetails>(id));
            return Ok(result);
        }

    }
}
