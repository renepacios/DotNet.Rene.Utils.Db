namespace Rene.Utils.Db.Sample.ManualMapper.Controllers
{
    using Commands;
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using ViewModel;

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
            => Ok(await sender.Send(new GetAllCommand<WeatherForecastViewModel>()));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                return Ok(await sender.Send(new GetCommand<WeatherForecastViewModelDetails>(id)));
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpGet("paginated")]
        public async Task<IActionResult> GetPaginated([FromQuery] int page = 0, [FromQuery] int size = 10)
            => Ok(await sender.Send(new GetPaginatedCommand<WeatherForecastViewModelDetails, WeatherForecast>(new DbUtilsAllSpecification<WeatherForecast>(), size, page)));

        [HttpPost]
        public async Task<IActionResult> Post(WeatherForecastViewModelDetails viewModel)
            => Created(string.Empty, await sender.Send(new AddCommand<WeatherForecastViewModelDetails>(viewModel)));

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, WeatherForecastViewModelDetails viewModel)
            => Ok(await sender.Send(new UpdateCommand<WeatherForecastViewModelDetails>(viewModel, id)));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => Ok(await sender.Send(new DeleteCommand<WeatherForecastViewModelDetails>(id)));
    }
}