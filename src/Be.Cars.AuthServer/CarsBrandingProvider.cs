using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace Be.Cars;

[Dependency(ReplaceServices = true)]
public class CarsBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "Cars";
}
