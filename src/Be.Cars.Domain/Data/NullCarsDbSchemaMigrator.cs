using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Be.Cars.Data;

/* This is used if database provider does't define
 * ICarsDbSchemaMigrator implementation.
 */
public class NullCarsDbSchemaMigrator : ICarsDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
