namespace Rene.Utils.Db.Sample.ManualMapper.Controllers
{
    using Authorization;
    using MediatR;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastAuthorizationController(ISender sender) : ControllerBase
    {
        [HttpGet("count")]
        public async Task<IActionResult> GetCount(CancellationToken cancellationToken)
            => Ok(await sender.Send(new GetForecastCountCommand(), cancellationToken));
    }
}
