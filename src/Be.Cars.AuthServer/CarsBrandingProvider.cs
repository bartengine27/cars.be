using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Be.Cars;

[Dependency(ReplaceServices = true)]
public class CarsBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "Cars";
}
