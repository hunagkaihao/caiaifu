using Ecs.AgvPlcTcp;
using Ecs.ConfigTool;
using Ecs.Log;
using Ecs.PlcMonitor;
using Ecs.Stations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Ecs;

[DependsOn(
    typeof(EcsDomainSharedModule),
    typeof(EcsToolsModule),
    typeof(AbpDddDomainModule)
)]
public class EcsDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        context.Services.Configure<AgvPlcTcpOptions>(
            context.Services.GetConfiguration().GetSection("Ecs:AgvPlcTcp"));
        context.Services.AddHostedService<PlcMonitorJob>();
        context.Services.AddHostedService<LogCntManageJob>();
        context.Services.AddHostedService<StationJob>();
        context.Services.AddHostedService<AgvPlcTcpHostedService>();
        context.Services.AddHostedService<AgvPlcEdgeDispatchHostedService>();
    }
}
