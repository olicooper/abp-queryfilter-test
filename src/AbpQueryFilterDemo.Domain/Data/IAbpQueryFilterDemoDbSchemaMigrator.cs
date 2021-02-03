using System.Threading.Tasks;

namespace AbpQueryFilterDemo.Data
{
    public interface IAbpQueryFilterDemoDbSchemaMigrator
    {
        Task MigrateAsync();
    }
}
