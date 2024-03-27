using Be.Cars.Samples;
using Xunit;

namespace Be.Cars.EntityFrameworkCore.Applications;

[Collection(CarsTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<CarsEntityFrameworkCoreTestModule>
{

}
