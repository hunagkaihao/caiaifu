using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.MySQL;
using Volo.Abp.Modularity;

namespace Ecs.EntityFrameworkCore;

[DependsOn(
    typeof(EcsDomainModule),
    typeof(AbpEntityFrameworkCoreMySQLModule)
    )]
public class EcsEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        context.Services.AddAbpDbContext<EcsDbContext>(options =>
        {
                /* Remove "includeAllEntities: true" to create
                 * default repositories only for aggregate roots */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        Configure<AbpDbContextOptions>(options =>
        {
                /* The main point to change your DBMS.
                 * See also EcsMigrationsDbContextFactory for EF Core tooling. */
            options.UseMySQL();
        });

    }
}
