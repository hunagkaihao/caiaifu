using Ecs.Localization;
using Volo.Abp.Application.Services;

namespace Ecs;

public abstract class EcsAppService : ApplicationService
{
    protected EcsAppService()
    {
        LocalizationResource = typeof(EcsResource);
    }
}