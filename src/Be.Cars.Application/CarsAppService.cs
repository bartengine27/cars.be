using System;
using System.Collections.Generic;
using System.Text;
using Be.Cars.Localization;
using Volo.Abp.Application.Services;

namespace Be.Cars;

/* Inherit your application services from this class.
 */
public abstract class CarsAppService : ApplicationService
{
    protected CarsAppService()
    {
        LocalizationResource = typeof(CarsResource);
    }
}
