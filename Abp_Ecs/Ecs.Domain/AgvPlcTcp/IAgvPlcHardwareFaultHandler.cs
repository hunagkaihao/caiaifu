using System.Threading;
using System.Threading.Tasks;

namespace Ecs.AgvPlcTcp;

public interface IAgvPlcHardwareFaultHandler
{
    /// <summary>
    /// 处理点位硬件异常。所有相关任务的起终端区域均暂停成功时返回 true。
    /// </summary>
    Task<bool> HandleAsync(
        string pointCode,
        AgvPlcHardwareStatus status,
        CancellationToken cancellationToken = default);
}
