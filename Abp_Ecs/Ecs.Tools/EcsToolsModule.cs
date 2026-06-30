using Ecs.ConfigTool;
using Ecs.PlcTool;
using Ecs.RedisTool;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Volo.Abp.Modularity;

namespace Ecs;

[DependsOn(
)]
public class EcsToolsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<ConfigOptions>(option =>
        {
            IConfigurationRoot root = new ConfigurationBuilder()
                .AddJsonFile($@"{AppDomain.CurrentDomain.BaseDirectory}appsettings.json", optional: false).Build();

            root.GetSection("Ecs").Bind(option);
        });
        context.Services.AddTransient<IRedisClient, RedisClientByStaEx>();
    }
}
