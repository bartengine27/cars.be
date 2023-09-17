using Be.Cars.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Be.Cars;

[DependsOn(
    typeof(CarsEntityFrameworkCoreTestModule)
    )]
public class CarsDomainTestModule : AbpModule
{

}
