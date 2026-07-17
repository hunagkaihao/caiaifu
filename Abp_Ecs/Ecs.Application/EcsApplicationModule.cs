using System;
using System.Net.Http;
using Ecs.AgvPlc;
using Ecs.AgvPlcTcp;
using Ecs.Rcs;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Application;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Ecs;

[DependsOn(
    typeof(EcsDomainModule),
    typeof(EcsApplicationContractsModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpDddApplicationModule)
    )]
public class EcsApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<EcsApplicationModule>();
        });

        var configuration = context.Services.GetConfiguration();
        var rcsSection = configuration.GetSection("Ecs:Rcs");
        context.Services.Configure<RcsOptions>(rcsSection);
        // 未显式配置 SkipSslValidation 时默认跳过（内网 IP 自签证书）
        var skipSslValidation = !bool.TryParse(rcsSection["SkipSslValidation"], out var skipSsl) || skipSsl;

        context.Services.AddHttpClient<IRcsApiClient, RcsApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (skipSslValidation)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            })
            .ConfigureHttpClient((_, client) =>
            {
                var rcsOptions = new RcsOptions
                {
                    Host = configuration["Ecs:Rcs:Host"] ?? string.Empty,
                    Port = int.TryParse(configuration["Ecs:Rcs:Port"], out var port) ? port : 0,
                    UseHttps = !bool.TryParse(configuration["Ecs:Rcs:UseHttps"], out var useHttps) || useHttps
                };
                var baseUrl = rcsOptions.GetBaseUrl();
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                }
            });

        context.Services.AddTransient<IRcsTaskFeedbackPlcDispatcher, RcsTaskFeedbackPlcDispatcher>();
        context.Services.AddTransient<IRcsTaskFeedbackQuendHandler, RcsTaskFeedbackQuendHandler>();
        context.Services.AddTransient<IAgvPlcSendReadPollSender, AgvPlcSendReadPollSender>();
        context.Services.AddTransient<IAgvPlcHardwareFaultHandler, AgvPlcHardwareFaultHandler>();
        context.Services.AddSingleton<IAgvTaskZoneOperationLock, AgvTaskZoneOperationLock>();

        base.ConfigureServices(context);
    }
}
