using Ecs.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Ecs.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class EcsController : AbpControllerBase
{
    protected EcsController()
    {
        LocalizationResource = typeof(EcsResource);
    }
}
