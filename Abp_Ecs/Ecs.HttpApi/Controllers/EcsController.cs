using Ecs.Localization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Ecs.Controllers;

/* Inherit your controllers from this class.
 */
[IgnoreAntiforgeryToken]
public abstract class EcsController : AbpControllerBase
{
    protected EcsController()
    {
        LocalizationResource = typeof(EcsResource);
    }
}
