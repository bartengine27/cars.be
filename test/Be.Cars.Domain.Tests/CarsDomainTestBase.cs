using Volo.Abp.Modularity;

namespace Be.Cars;

/* Inherit from this class for your domain layer tests. */
public abstract class CarsDomainTestBase<TStartupModule> : CarsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
