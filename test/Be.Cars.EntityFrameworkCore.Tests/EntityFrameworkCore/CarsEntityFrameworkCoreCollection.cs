using Xunit;

namespace Be.Cars.EntityFrameworkCore;

[CollectionDefinition(CarsTestConsts.CollectionDefinitionName)]
public class CarsEntityFrameworkCoreCollection : ICollectionFixture<CarsEntityFrameworkCoreFixture>
{

}
