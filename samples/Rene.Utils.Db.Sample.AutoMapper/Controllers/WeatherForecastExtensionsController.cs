namespace Rene.Utils.Db.Sample.AutoMapper.Controllers
{
    using ExtensionHelpers;
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using Model;
    using ViewModel;

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastExtensionsController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
            => Ok(await mediator.GetAll<WeatherForecastViewModel>());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                return Ok(await mediator.Get<WeatherForecastViewModelDetails>(id));
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpGet("paginated")]
        public async Task<IActionResult> GetPaginated([FromQuery] int page = 0, [FromQuery] int size = 10)
            => Ok(await mediator.GetPaginated<WeatherForecastViewModelDetails, WeatherForecast>(new DbUtilsAllSpecification<WeatherForecast>(), size, page));

        [HttpPost]
        public async Task<IActionResult> Post(WeatherForecastViewModelDetails viewModel)
            => Created(string.Empty, await mediator.Add(viewModel));

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, WeatherForecastViewModelDetails viewModel)
            => Ok(await mediator.Update(viewModel, id));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
            => Ok(await mediator.Delete<WeatherForecastViewModelDetails>(id));
    }
}
