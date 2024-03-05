using Be.Cars.Localization;
using Be.Cars.Metrics;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace Be.Cars.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class CarsController : AbpControllerBase
{
    protected CarsController(CustomMetrics customMetrics)
    {
        LocalizationResource = typeof(CarsResource);
        CustomMetrics = customMetrics;
    }

    public Task PostIncrementCar()
    {
        CustomMetrics.IncrementCarsCounter();
        return Task.CompletedTask;
    }

    CustomMetrics CustomMetrics
    {
        get;
        set;
    }
}
