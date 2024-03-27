using Volo.Abp.Modularity;

namespace Be.Cars;

[DependsOn(
    typeof(CarsApplicationModule),
    typeof(CarsDomainTestModule)
)]
public class CarsApplicationTestModule : AbpModule
{

}
