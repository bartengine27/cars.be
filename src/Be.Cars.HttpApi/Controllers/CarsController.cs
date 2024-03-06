using Be.Cars.Localization;
using Be.Cars.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace Be.Cars.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class CarsController : AbpControllerBase
{
    protected CarsController(CustomMetrics customMetrics, ILogger<CarsController> logger)
    {
        LocalizationResource = typeof(CarsResource);
        CustomMetrics = customMetrics;
        Logger = logger;
    }

    [HttpPost("api/car/increment")]
    public Task PostIncrementCar()
    {
        CustomMetrics.IncrementCarsCounter();
        Logger.LogInformation("Incremented cars counter");
        return Task.CompletedTask;
    }

    public ILogger<CarsController> Logger
    {

        get;
        set;
    }

    CustomMetrics CustomMetrics
    {
        get;
        set;
    }
}
