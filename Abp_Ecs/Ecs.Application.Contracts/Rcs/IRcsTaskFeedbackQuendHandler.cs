using System.Threading;
using System.Threading.Tasks;

namespace Ecs.Rcs;

/// <summary>
/// 2.2.1 回馈 <c>method=quend</c> 或 <c>fanend</c>：搬运任务完成并释放 O1A 运行态锁定。
/// </summary>
public interface IRcsTaskFeedbackQuendHandler
{
    Task HandleAsync(RcsTaskExecutionFeedbackRequest request, CancellationToken cancellationToken = default);
}
