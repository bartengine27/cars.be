using Be.Cars.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Be.Cars.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class CarsController : AbpControllerBase
{
    protected CarsController()
    {
        LocalizationResource = typeof(CarsResource);
    }
}
