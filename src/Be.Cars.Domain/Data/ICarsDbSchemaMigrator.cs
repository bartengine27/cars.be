using System.Threading.Tasks;

namespace Be.Cars.Data;

public interface ICarsDbSchemaMigrator
{
    Task MigrateAsync();
}
