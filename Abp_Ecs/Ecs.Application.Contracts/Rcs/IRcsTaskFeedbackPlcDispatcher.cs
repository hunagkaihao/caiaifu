#nullable disable
using System.Threading;
using System.Threading.Tasks;

namespace Ecs.Rcs;

/// <summary>
/// 2.2.1 任务过程回馈：按 <c>method</c> 向 O1A 下发 PLC 指令；取货/放货侧八步在 O1A 应答匹配后后台调用 RCS 继续执行。
/// </summary>
public interface IRcsTaskFeedbackPlcDispatcher
{
    Task DispatchAsync(RcsTaskExecutionFeedbackRequest request, CancellationToken cancellationToken = default);
}
