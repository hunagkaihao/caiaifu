using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.Rcs;

public interface IRcsTransportTaskStatusUpdater : IApplicationService
{
    Task TryUpdateFromRcsCallbackAsync(
        string? robotTaskCode,
        string? rcsMethod,
        CancellationToken cancellationToken = default);
}
