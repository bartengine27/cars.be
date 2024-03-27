using Volo.Abp.Modularity;

namespace Be.Cars;

[DependsOn(
    typeof(CarsDomainModule),
    typeof(CarsTestBaseModule)
)]
public class CarsDomainTestModule : AbpModule
{

}
