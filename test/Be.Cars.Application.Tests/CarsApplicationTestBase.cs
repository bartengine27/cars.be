using Volo.Abp.Modularity;

namespace Be.Cars;

public abstract class CarsApplicationTestBase<TStartupModule> : CarsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
