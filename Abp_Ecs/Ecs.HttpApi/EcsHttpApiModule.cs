using Ecs.Localization;
using Localization.Resources.AbpUi;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;

namespace Ecs;

[DependsOn(
    typeof(EcsApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule)
    )]
public class EcsHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<EcsResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}
