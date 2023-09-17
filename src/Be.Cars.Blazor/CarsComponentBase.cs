using Be.Cars.Localization;
using Volo.Abp.AspNetCore.Components;

namespace Be.Cars.Blazor;

public abstract class CarsComponentBase : AbpComponentBase
{
    protected CarsComponentBase()
    {
        LocalizationResource = typeof(CarsResource);
    }
}
