using Be.Cars.Samples;
using Xunit;

namespace Be.Cars.EntityFrameworkCore.Domains;

[Collection(CarsTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<CarsEntityFrameworkCoreTestModule>
{

}
