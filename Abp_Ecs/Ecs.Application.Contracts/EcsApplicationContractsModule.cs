using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace Ecs;

[DependsOn(
    typeof(EcsDomainSharedModule),
    typeof(AbpDddApplicationContractsModule)
)]
public class EcsApplicationContractsModule : AbpModule
{

}
