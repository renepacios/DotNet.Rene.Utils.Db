using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rene.Utils.Db.Sample.App1.Controllers;

public class MetaController : ControllerBase
{
    [HttpGet, Route("")]
    public IActionResult Get()
    {
        var assembly = typeof(MetaController).Assembly;

        var modificado = System.IO.File.GetLastWriteTime(assembly.Location);
        var creationDate = System.IO.File.GetCreationTime(assembly.Location);
        var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);




        return Ok(new
        {
            fvi.ProductName,
            fvi.ProductVersion,
            fvi.FileVersion, // $"{fvi.ProductMajorPart}.{fvi.ProductMinorPart}.{fvi.ProductBuildPart}.-{fvi.PrivateBuild}",
            Last_Update = modificado,
            Last_Build = creationDate,
            OS = RuntimeInformation.OSDescription
            // Configuration = _configuration.GetValue<string>("AppSettings:ConfigurationName") // _appSettings.ConfigurationName

        });
    }
}