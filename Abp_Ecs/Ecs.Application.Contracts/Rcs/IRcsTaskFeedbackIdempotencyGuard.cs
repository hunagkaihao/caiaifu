using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Ecs.Rcs;

/// <summary>
/// RCS 2.2.1 回调幂等：同一 robotTaskCode + method 仅处理一次。
/// </summary>
public interface IRcsTaskFeedbackIdempotencyGuard : IApplicationService
{
    /// <summary>
    /// 尝试占用回调处理权。返回 false 表示重复回调，应跳过后续业务。
    /// </summary>
    Task<bool> TryAcquireAsync(
        string? robotTaskCode,
        string? rcsMethod,
        CancellationToken cancellationToken = default);
}
